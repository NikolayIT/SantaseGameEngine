namespace Santase.Logic
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.Trick;

    public class GameRound : IGameRound
    {
        private readonly IDeck deck;

        private readonly IPlayer firstPlayer;

        private readonly IList<Card> firstPlayerCards;

        private readonly IPlayer secondPlayer;

        private readonly IList<Card> secondPlayerCards;

        private BaseRoundState state;

        public GameRound(IPlayer firstPlayer, IPlayer secondPlayer, PlayerPosition firstToPlay)
        {
            this.deck = new Deck();
            this.firstPlayer = firstPlayer;
            this.FirstPlayerPoints = 0;
            this.firstPlayerCards = new List<Card>();
            this.FirstPlayerHasHand = false;

            this.secondPlayer = secondPlayer;
            this.SecondPlayerPoints = 0;
            this.secondPlayerCards = new List<Card>();
            this.SecondPlayerHasHand = false;

            this.LastHandInPlayer = firstToPlay;

            this.SetState(new StartRoundState(this));

            this.ClosedByPlayer = PlayerPosition.NoOne;
        }

        public int FirstPlayerPoints { get; private set; }

        public int SecondPlayerPoints { get; private set; }

        public bool FirstPlayerHasHand { get; private set; }

        public bool SecondPlayerHasHand { get; private set; }

        public PlayerPosition ClosedByPlayer { get; private set; }

        public PlayerPosition LastHandInPlayer { get; private set; }

        public void Start()
        {
            this.DealFirstCards();
            while (!this.IsFinished())
            {
                this.PlayHand();
            }
        }

        public void SetState(BaseRoundState newState)
        {
            this.state = newState;
        }

        private void PlayHand()
        {
            IGameTrick trick = new GameTrick(
                this.LastHandInPlayer,
                this.firstPlayer,
                this.firstPlayerCards,
                this.secondPlayer,
                this.secondPlayerCards,
                this.state,
                this.deck);
            trick.Start();

            this.UpdatePoints(trick);

            if (trick.Winner == PlayerPosition.FirstPlayer)
            {
                this.FirstPlayerHasHand = true;
            }
            else
            {
                this.SecondPlayerHasHand = true;
            }

            this.LastHandInPlayer = trick.Winner;

            this.firstPlayerCards.Remove(trick.FirstPlayerCard);
            this.secondPlayerCards.Remove(trick.SecondPlayerCard);

            if (trick.GameClosedBy == PlayerPosition.FirstPlayer || trick.GameClosedBy == PlayerPosition.SecondPlayer)
            {
                this.ClosedByPlayer = trick.GameClosedBy;
                this.state.Close();
            }

            this.DrawNewCards();
            this.state.PlayHand(this.deck.CardsLeft);
        }

        private void DrawNewCards()
        {
            if (!this.state.ShouldDrawCard)
            {
                return;
            }

            if (this.LastHandInPlayer == PlayerPosition.FirstPlayer)
            {
                this.GiveCardToFirstPlayer();
                this.GiveCardToSecondPlayer();
            }
            else
            {
                this.GiveCardToSecondPlayer();
                this.GiveCardToFirstPlayer();
            }
        }

        private void UpdatePoints(IGameTrick trick)
        {
            if (trick.Winner == PlayerPosition.FirstPlayer)
            {
                this.FirstPlayerPoints += trick.FirstPlayerCard.GetValue();
                this.FirstPlayerPoints += trick.SecondPlayerCard.GetValue();
            }
            else
            {
                this.SecondPlayerPoints += trick.FirstPlayerCard.GetValue();
                this.SecondPlayerPoints += trick.SecondPlayerCard.GetValue();
            }

            this.FirstPlayerPoints += (int)trick.FirstPlayerAnnounce;
            this.SecondPlayerPoints += (int)trick.SecondPlayerAnnounce;
        }

        private void GiveCardToFirstPlayer()
        {
            var card = this.deck.GetNextCard();
            this.firstPlayer.AddCard(card);
            this.firstPlayerCards.Add(card);
        }

        private void GiveCardToSecondPlayer()
        {
            var card = this.deck.GetNextCard();
            this.secondPlayer.AddCard(card);
            this.secondPlayerCards.Add(card);
        }

        private bool IsFinished()
        {
            if (this.FirstPlayerPoints >= 66)
            {
                return true;
            }

            if (this.SecondPlayerPoints >= 66)
            {
                return true;
            }

            if (this.firstPlayerCards.Count == 0 || this.secondPlayerCards.Count == 0)
            {
                return true;
            }

            return false;
        }

        private void DealFirstCards()
        {
            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToFirstPlayer();
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToSecondPlayer();
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToFirstPlayer();
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToSecondPlayer();
            }
        }
    }
}
