namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;

    internal class Trick
    {
        private static readonly ICardWinnerLogic CardWinner = new CardWinnerLogic();

        private readonly IStateManager stateManager;

        private readonly IDeck deck;

        private readonly IGameRules gameRules;

        public Trick(
            IStateManager stateManager,
            IDeck deck,
            IGameRules gameRules)
        {
            this.stateManager = stateManager;
            this.deck = deck;
            this.gameRules = gameRules;
        }

        public RoundPlayerInfo Play(RoundPlayerInfo firstToPlay, RoundPlayerInfo secondToPlay)
        {
            var context = new PlayerTurnContext(
                this.stateManager.State,
                this.deck.TrumpCard,
                this.deck.CardsLeft,
                firstToPlay.RoundPoints,
                secondToPlay.RoundPoints);

            // First player
            var firstPlayerAction = this.GetFirstPlayerAction(firstToPlay, context);
            context.FirstPlayedCard = firstPlayerAction.Card;
            context.FirstPlayerAnnounce = firstPlayerAction.Announce;
            var firstToPlayRoundPoints = firstToPlay.RoundPoints;
            context.FirstPlayerRoundPoints = firstToPlayRoundPoints;

            firstToPlay.Cards.Remove(firstPlayerAction.Card);

            // When player announces something he may immediately become round winner
            if (firstToPlayRoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                // Inform players for end turn
                firstToPlay.Player.EndTurn(context);
                secondToPlay.Player.EndTurn(context);
                return firstToPlay;
            }

            // Second player
            var secondPlayerAction = GetPlayerAction(secondToPlay, context);
            context.SecondPlayedCard = secondPlayerAction.Card;
            secondToPlay.Cards.Remove(secondPlayerAction.Card);

            // Determine winner
            var winnerPosition = CardWinner.Winner(
                firstPlayerAction.Card,
                secondPlayerAction.Card,
                this.deck.TrumpCard.Suit);

            var winner = winnerPosition == PlayerPosition.FirstPlayer ? firstToPlay : secondToPlay;
            winner.WinCard(firstPlayerAction.Card);
            winner.WinCard(secondPlayerAction.Card);

            // Inform players for end turn
            context.FirstPlayerRoundPoints = firstToPlay.RoundPoints;
            context.SecondPlayerRoundPoints = secondToPlay.RoundPoints;
            firstToPlay.Player.EndTurn(context);
            secondToPlay.Player.EndTurn(context);

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
                            var nineOfTrump = Card.GetCard(oldTrumpCard.Suit, CardType.Nine);

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
                                playerInfo.AddAnnounce(action.Announce);
                            }

                            return action;
                        }
                }
            }
        }
    }
}
