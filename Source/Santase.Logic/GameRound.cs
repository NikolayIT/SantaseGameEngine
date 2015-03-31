using Santase.Logic.Cards;
using Santase.Logic.Players;
using Santase.Logic.RoundStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic
{
    public class GameRound : IGameRound
    {
        private IDeck deck;

        private IPlayer firstPlayer;
        private int firstPlayerPoints;
        private IList<Card> firstPlayerCards;
        private bool firstPlayerHasCollectedCards;

        private IPlayer secondPlayer;
        private int secondPlayerPoints;
        private IList<Card> secondPlayerCards;
        private bool secondPlayerHasCollectedCards;

        private PlayerPosition firstToPlay;

        private BaseRoundState state;

        private PlayerPosition gameClosedBy;

        public GameRound(IPlayer firstPlayer, IPlayer secondPlayer, PlayerPosition firstToPlay)
        {
            this.deck = new Deck();
            this.firstPlayer = firstPlayer;
            this.firstPlayerPoints = 0;
            this.firstPlayerCards = new List<Card>();
            this.firstPlayerHasCollectedCards = false;

            this.secondPlayer = secondPlayer;
            this.secondPlayerPoints = 0;
            this.secondPlayerCards = new List<Card>();
            this.secondPlayerHasCollectedCards = false;

            this.firstToPlay = firstToPlay;

            this.SetState(new StartRoundState(this));

            this.gameClosedBy = PlayerPosition.NoOne;
        }

        public void Start()
        {
            this.DealFirstCards();
            while(!this.IsFinished())
            {
                this.PlayHand();
            }
        }

        private void PlayHand()
        {
            IGameHand hand = new GameHand(
                this.firstToPlay,
                this.firstPlayer,
                this.firstPlayerCards,
                this.secondPlayer,
                this.secondPlayerCards,
                this.state,
                this.deck);
            hand.Start();

            this.UpdatePoints(hand);

            if (hand.Winner == PlayerPosition.FirstPlayer)
            {
                this.firstPlayerHasCollectedCards = true;
            }
            else
            {
                this.secondPlayerHasCollectedCards = true;
            }

            this.firstToPlay = hand.Winner;

            this.firstPlayerCards.Remove(hand.FirstPlayerCard);
            this.secondPlayerCards.Remove(hand.SecondPlayerCard);

            if (hand.GameClosedBy == PlayerPosition.FirstPlayer
                || hand.GameClosedBy == PlayerPosition.SecondPlayer)
            {
                this.gameClosedBy = hand.GameClosedBy;
                this.state.Close();
            }

            this.DrawNewCards();
            this.state.PlayHand(this.deck.CardsLeft);
        }

        private void DrawNewCards()
        {
            if (this.state.ShouldDrawCard)
            {
                if (this.firstToPlay == PlayerPosition.FirstPlayer)
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
        }

        private void UpdatePoints(IGameHand hand)
        {
            if (hand.Winner == PlayerPosition.FirstPlayer)
            {
                this.firstPlayerPoints += hand.FirstPlayerCard.GetValue();
                this.firstPlayerPoints += hand.SecondPlayerCard.GetValue();
            }
            else
            {
                this.secondPlayerPoints += hand.FirstPlayerCard.GetValue();
                this.secondPlayerPoints += hand.SecondPlayerCard.GetValue();
            }

            this.firstPlayerPoints += (int)hand.FirstPlayerAnnounce;
            this.secondPlayerPoints += (int)hand.SecondPlayerAnnounce;
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
            if (this.firstPlayerPoints >= 66)
            {
                return true;
            }

            if (this.secondPlayerPoints >= 66)
            {
                return true;
            }

            if (this.firstPlayerCards.Count == 0
                || this.secondPlayerCards.Count == 0)
            {
                return true;
            }

            return false;
        }

        private void DealFirstCards()
        {
            for (int i = 0; i < 3; i++)
            {
                this.GiveCardToFirstPlayer();
            }

            for (int i = 0; i < 3; i++)
            {
                this.GiveCardToSecondPlayer();
            }

            for (int i = 0; i < 3; i++)
            {
                this.GiveCardToFirstPlayer();
            }

            for (int i = 0; i < 3; i++)
            {
                this.GiveCardToSecondPlayer();
            }
        }

        public int FirstPlayerPoints
        {
            get { return this.firstPlayerPoints; }
        }

        public int SecondPlayerPoints
        {
            get { return this.secondPlayerPoints; }
        }

        public bool FirstPlayerHasHand
        {
            get { return this.firstPlayerHasCollectedCards; }
        }

        public bool SecondPlayerHasHand
        {
            get { return this.secondPlayerHasCollectedCards; }
        }

        public PlayerPosition ClosedByPlayer
        {
            get { return this.gameClosedBy; }
        }


        public void SetState(BaseRoundState newState)
        {
            this.state = newState;
        }


        public PlayerPosition LastHandInPlayer
        {
            get { return this.firstToPlay; }
        }
    }
}
