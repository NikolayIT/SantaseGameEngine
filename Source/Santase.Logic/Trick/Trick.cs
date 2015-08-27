namespace Santase.Logic.Trick
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    // TODO: Unit test this class
    public class Trick
    {
        private readonly PlayerInfo firstToPlay;

        private readonly PlayerInfo secondToPlay;

        private readonly IStateManager stateManager;

        private readonly IDeck deck;

        private bool gameClosed;

        public Trick(PlayerInfo firstToPlay, PlayerInfo secondToPlay, IStateManager stateManager, IDeck deck)
        {
            this.firstToPlay = firstToPlay;
            this.secondToPlay = secondToPlay;
            this.stateManager = stateManager;
            this.deck = deck;
        }

        public TrickResult Play()
        {
            var context = new PlayerTurnContext(this.stateManager.State, this.deck.TrumpCard, this.deck.CardsLeft);

            var firstPlayerAction = this.GetFirstPlayerAction(this.firstToPlay, context);
            context.FirstPlayedCard = firstPlayerAction.Card;

            var secondPlayerAction = this.GetPlayerAction(this.secondToPlay, context);
            context.SecondPlayedCard = secondPlayerAction.Card;

            ICardWinnerLogic cardWinnerLogic = new CardWinnerLogic();
            var winnerPosition = cardWinnerLogic.Winner(
                firstPlayerAction.Card,
                secondPlayerAction.Card,
                this.deck.TrumpCard.Suit);

            var winner = winnerPosition == PlayerPosition.FirstPlayer ? this.firstToPlay : this.secondToPlay;

            this.firstToPlay.Player.EndTurn(context);
            this.secondToPlay.Player.EndTurn(context);

            return new TrickResult(winner, firstPlayerAction.Announce, this.gameClosed);
        }

        private PlayerAction GetFirstPlayerAction(PlayerInfo playerInfo, PlayerTurnContext context)
        {
            while (true)
            {
                var action = this.GetPlayerAction(this.firstToPlay, context);

                switch (action.Type)
                {
                    case PlayerActionType.ChangeTrump:
                        var oldTrumpCard = this.deck.TrumpCard;
                        var newTrumpCard = new Card(oldTrumpCard.Suit, CardType.Nine);

                        this.deck.ChangeTrumpCard(newTrumpCard);
                        context.TrumpCard = newTrumpCard;

                        playerInfo.Cards.Remove(newTrumpCard);
                        playerInfo.Cards.Add(oldTrumpCard);
                        playerInfo.Player.AddCard(oldTrumpCard);

                        continue;
                    case PlayerActionType.CloseGame:
                        this.stateManager.State.Close();
                        context.State = this.stateManager.State;
                        this.gameClosed = true;
                        continue;
                    case PlayerActionType.PlayCard:
                        return action;
                    default:
                        throw new InternalGameException("Invalid PlayerActionType");
                }
            }
        }

        private PlayerAction GetPlayerAction(PlayerInfo player, PlayerTurnContext context)
        {
            var playerActionValidator = new PlayerActionValidator();
            var action = player.Player.GetTurn(context, playerActionValidator);
            var isActionValid = playerActionValidator.IsValid(action, context, player.Cards);
            if (!isActionValid)
            {
                throw new InternalGameException($"Invalid turn from {this.firstToPlay.Player.Name}");
            }

            return action;
        }
    }
}
