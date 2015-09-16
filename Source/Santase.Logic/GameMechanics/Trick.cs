namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;

    internal class Trick
    {
        private readonly RoundPlayerInfo firstToPlay;

        private readonly RoundPlayerInfo secondToPlay;

        private readonly IStateManager stateManager;

        private readonly IDeck deck;

        private readonly IGameRules gameRules;

        public Trick(
            RoundPlayerInfo firstToPlay,
            RoundPlayerInfo secondToPlay,
            IStateManager stateManager,
            IDeck deck,
            IGameRules gameRules)
        {
            this.firstToPlay = firstToPlay;
            this.secondToPlay = secondToPlay;
            this.stateManager = stateManager;
            this.deck = deck;
            this.gameRules = gameRules;
        }

        public RoundPlayerInfo Play()
        {
            var context = new PlayerTurnContext(
                this.stateManager.State,
                this.deck.TrumpCard,
                this.deck.CardsLeft,
                this.firstToPlay.RoundPoints,
                this.secondToPlay.RoundPoints);

            // First player
            var firstPlayerAction = this.GetFirstPlayerAction(this.firstToPlay, context);
            context.FirstPlayedCard = firstPlayerAction.Card;
            context.FirstPlayerAnnounce = firstPlayerAction.Announce;
            context.FirstPlayerRoundPoints = this.firstToPlay.RoundPoints;

            this.firstToPlay.Cards.Remove(firstPlayerAction.Card);

            // When player announces something he may immediately become round winner
            if (this.firstToPlay.RoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                // Inform players for end turn
                this.firstToPlay.Player.EndTurn(context);
                this.secondToPlay.Player.EndTurn(context);
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
            context.FirstPlayerRoundPoints = this.firstToPlay.RoundPoints;
            context.SecondPlayerRoundPoints = this.secondToPlay.RoundPoints;
            this.firstToPlay.Player.EndTurn(context);
            this.secondToPlay.Player.EndTurn(context);

            return winner;
        }

        private static PlayerAction GetPlayerAction(RoundPlayerInfo playerInfo, PlayerTurnContext context)
        {
            var action = playerInfo.Player.GetTurn(context.DeepClone());
            var isActionValid = PlayerActionValidator.Instance.IsValid(action, context, playerInfo.Cards);
            if (!isActionValid)
            {
                throw new InternalGameException($"Invalid action played from {playerInfo.Player.Name}");
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
                            var nineOfTrump = new Card(oldTrumpCard.Suit, CardType.Nine);

                            this.deck.ChangeTrumpCard(nineOfTrump);
                            context.TrumpCard = nineOfTrump;

                            // Only swap cards from the local cards list (player should swap its own cards)
                            playerInfo.Cards.Remove(nineOfTrump);
                            playerInfo.Cards.Add(oldTrumpCard);
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
                }
            }
        }
    }
}
