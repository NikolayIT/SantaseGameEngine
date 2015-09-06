namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;

    // TODO: Unit test this class
    internal class Trick
    {
        private readonly RoundPlayerInfo firstToPlay;

        private readonly RoundPlayerInfo secondToPlay;

        private readonly IStateManager stateManager;

        private readonly IDeck deck;

        public Trick(RoundPlayerInfo firstToPlay, RoundPlayerInfo secondToPlay, IStateManager stateManager, IDeck deck)
        {
            this.firstToPlay = firstToPlay;
            this.secondToPlay = secondToPlay;
            this.stateManager = stateManager;
            this.deck = deck;
        }

        public RoundPlayerInfo Play()
        {
            var context = new PlayerTurnContext(this.stateManager.State, this.deck.TrumpCard, this.deck.CardsLeft);

            // First player
            var firstPlayerAction = this.GetFirstPlayerAction(this.firstToPlay, context);
            context.FirstPlayedCard = firstPlayerAction.Card;
            context.FirstPlayerAnnounce = firstPlayerAction.Announce;
            this.firstToPlay.Cards.Remove(firstPlayerAction.Card);

            // When player announces something he may immediately become round winner
            if (this.firstToPlay.RoundPoints >= 66)
            {
                return this.firstToPlay;
            }

            // Second player
            var secondPlayerAction = GetPlayerAction(this.secondToPlay, context);
            context.SecondPlayedCard = secondPlayerAction.Card;
            this.secondToPlay.Cards.Remove(secondPlayerAction.Card);

            // Determine winner
            ICardWinnerLogic cardWinnerLogic = new CardWinnerLogic();
            var winnerPosition = cardWinnerLogic.Winner(
                firstPlayerAction.Card,
                secondPlayerAction.Card,
                this.deck.TrumpCard.Suit);

            var winner = winnerPosition == PlayerPosition.FirstPlayer ? this.firstToPlay : this.secondToPlay;
            winner.TrickCards.Add(firstPlayerAction.Card);
            winner.TrickCards.Add(secondPlayerAction.Card);

            // Inform players for end turn
            this.firstToPlay.Player.EndTurn(context);
            this.secondToPlay.Player.EndTurn(context);

            return winner;
        }

        private static PlayerAction GetPlayerAction(RoundPlayerInfo playerInfo, PlayerTurnContext context)
        {
            var action = playerInfo.Player.GetTurn(context.Clone() as PlayerTurnContext);
            var isActionValid = PlayerActionValidator.Instance.IsValid(action, context, playerInfo.Cards);
            if (!isActionValid)
            {
                throw new InternalGameException($"Invalid turn from {playerInfo.Player.Name}");
            }

            return action;
        }

        private PlayerAction GetFirstPlayerAction(RoundPlayerInfo playerInfo, PlayerTurnContext context)
        {
            while (true)
            {
                var action = GetPlayerAction(playerInfo, context);
                switch (action.Type)
                {
                    case PlayerActionType.ChangeTrump:
                        {
                            var oldTrumpCard = this.deck.TrumpCard;
                            var newTrumpCard = new Card(oldTrumpCard.Suit, CardType.Nine);

                            this.deck.ChangeTrumpCard(newTrumpCard);
                            context.TrumpCard = newTrumpCard;

                            playerInfo.Cards.Remove(newTrumpCard);
                            playerInfo.AddCard(oldTrumpCard);
                            continue;
                        }

                    case PlayerActionType.CloseGame:
                        {
                            this.stateManager.State.Close();
                            context.State = this.stateManager.State;
                            playerInfo.GameCloser = true;
                            continue;
                        }

                    case PlayerActionType.PlayCard:
                        {
                            if (action.Announce != Announce.None)
                            {
                                playerInfo.Announces.Add(action.Announce);
                            }

                            return action;
                        }

                    default:
                        throw new InternalGameException($"Invalid PlayerActionType from {playerInfo.Player.Name}");
                }
            }
        }
    }
}
