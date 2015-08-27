namespace Santase.Logic.Trick
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    public class GameTrick : IGameTrick
    {
        private readonly PlayerPosition whoWillPlayFirst;
        private readonly IDeck deck;
        private readonly IPlayerActionValidator actionValidator;

        private readonly IPlayer firstPlayer;
        private readonly IList<Card> firstPlayerCards;

        private readonly IPlayer secondPlayer;
        private readonly IList<Card> secondPlayerCards;

        private BaseRoundState state;

        public GameTrick(
            PlayerPosition whoWillPlayFirst,
            IPlayer firstPlayer,
            IList<Card> firstPlayerCards,
            IPlayer secondPlayer,
            IList<Card> secondPlayerCards,
            BaseRoundState state,
            IDeck deck)
        {
            this.whoWillPlayFirst = whoWillPlayFirst;
            this.firstPlayer = firstPlayer;
            this.firstPlayerCards = firstPlayerCards;
            this.secondPlayer = secondPlayer;
            this.secondPlayerCards = secondPlayerCards;
            this.state = state;
            this.deck = deck;
            this.actionValidator = new PlayerActionValidator();
            this.GameClosedBy = PlayerPosition.NoOne;
        }

        public PlayerPosition Winner { get; private set; }

        public Card FirstPlayerCard { get; private set; }

        public Announce FirstPlayerAnnounce { get; private set; }

        public Card SecondPlayerCard { get; private set; }

        public Announce SecondPlayerAnnounce { get; private set; }

        public PlayerPosition GameClosedBy { get; private set; }

        public void Start()
        {
            if (this.whoWillPlayFirst == PlayerPosition.FirstPlayer)
            {
                this.PlayTrick(this.firstPlayer, this.firstPlayerCards, this.secondPlayer, this.secondPlayerCards);
            }
            else
            {
                this.PlayTrick(this.secondPlayer, this.secondPlayerCards, this.firstPlayer, this.firstPlayerCards);
            }
        }

        public void PlayTrick(
            IPlayer firstToPlay,
            IList<Card> firstToPlayCards,
            IPlayer secondToPlay,
            IList<Card> secondToPlayCards)
        {
            var context = new PlayerTurnContext(this.state, this.deck.TrumpCard, this.deck.CardsLeft);

            PlayerAction firstPlayerAction;
            do
            {
                firstPlayerAction = this.FirstPlayerTurn(firstToPlay, context, firstToPlayCards);
            }
            while (firstPlayerAction.Type != PlayerActionType.PlayCard);

            context.FirstPlayedCard = firstPlayerAction.Card;

            var secondPlayerAction = secondToPlay.GetTurn(context, this.actionValidator);

            if (!this.actionValidator.IsValid(secondPlayerAction, context, secondToPlayCards))
            {
                // TODO: Do something more graceful?
                throw new InternalGameException("Invalid turn!");
            }

            context.SecondPlayedCard = secondPlayerAction.Card;

            if (firstToPlay == this.firstPlayer)
            {
                this.FirstPlayerCard = firstPlayerAction.Card;
                this.FirstPlayerAnnounce = firstPlayerAction.Announce;
                this.SecondPlayerCard = secondPlayerAction.Card;
                this.SecondPlayerAnnounce = secondPlayerAction.Announce;
            }
            else
            {
                this.FirstPlayerCard = secondPlayerAction.Card;
                this.FirstPlayerAnnounce = secondPlayerAction.Announce;
                this.SecondPlayerCard = firstPlayerAction.Card;
                this.SecondPlayerAnnounce = firstPlayerAction.Announce;
            }

            firstToPlay.EndTurn(context);
            secondToPlay.EndTurn(context);

            ICardWinnerLogic cardWinnerLogic = new CardWinnerLogic();
            if (firstToPlay == this.firstPlayer)
            {
                this.Winner = cardWinnerLogic.Winner(
                    firstPlayerAction.Card,
                    secondPlayerAction.Card,
                    this.deck.TrumpCard.Suit);
            }
            else
            {
                this.Winner = cardWinnerLogic.Winner(
                    secondPlayerAction.Card,
                    firstPlayerAction.Card,
                    this.deck.TrumpCard.Suit);
            }
        }

        private PlayerAction FirstPlayerTurn(IPlayer firstToPlay, PlayerTurnContext context, IList<Card> firstToPlayCards)
        {
            var firstToPlayTurn = firstToPlay.GetTurn(context, this.actionValidator);
            if (!this.actionValidator.IsValid(firstToPlayTurn, context, firstToPlayCards))
            {
                // TODO: Do something more graceful?
                throw new InternalGameException("Invalid turn!");
            }

            if (firstToPlayTurn.Type == PlayerActionType.CloseGame)
            {
                this.state.Close();
                context.State = new FinalRoundState();
                this.state = new FinalRoundState();
                this.GameClosedBy = firstToPlay == this.firstPlayer
                                        ? PlayerPosition.FirstPlayer
                                        : PlayerPosition.SecondPlayer;
            }

            if (firstToPlayTurn.Type == PlayerActionType.ChangeTrump)
            {
                var changeTrump = new Card(this.deck.TrumpCard.Suit, CardType.Nine);
                var oldTrump = this.deck.TrumpCard;
                context.TrumpCard = oldTrump;
                this.deck.ChangeTrumpCard(changeTrump);

                if (firstToPlay == this.firstPlayer)
                {
                    this.firstPlayerCards.Remove(changeTrump);
                    this.firstPlayerCards.Add(oldTrump);
                    this.firstPlayer.AddCard(oldTrump);
                }
                else
                {
                    this.secondPlayerCards.Remove(changeTrump);
                    this.secondPlayerCards.Add(oldTrump);
                    this.secondPlayer.AddCard(oldTrump);
                }
            }

            return firstToPlayTurn;
        }
    }
}
