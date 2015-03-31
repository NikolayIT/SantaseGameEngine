using Santase.Logic.Cards;
using Santase.Logic.Players;
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
        private IList<Card> firstPlayerCollectedCards;

        private IPlayer secondPlayer;
        private int secondPlayerPoints;
        private IList<Card> secondPlayerCards;
        private IList<Card> secondPlayerCollectedCards;

        private PlayerPosition firstToPlay;

        public GameRound(IPlayer firstPlayer, IPlayer secondPlayer, PlayerPosition firstToPlay)
        {
            this.deck = new Deck();
            this.firstPlayer = firstPlayer;
            this.firstPlayerPoints = 0;
            this.firstPlayerCards = new List<Card>();
            this.firstPlayerCollectedCards = new List<Card>();

            this.secondPlayer = secondPlayer;
            this.secondPlayerPoints = 0;
            this.secondPlayerCards = new List<Card>();
            this.secondPlayerCollectedCards = new List<Card>();

            this.firstToPlay = firstToPlay;
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
            IGameHand hand = new GameHand();
            hand.Start();

            // TODO: Update points
            // TODO: Add one more card to both players
            // TODO: Update firstPlayerCollectedCards and secondPlayerCollectedCards
            
            this.firstToPlay = hand.Winner;
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
                var card = this.deck.GetNextCard();
                this.firstPlayer.AddCard(card);
            }

            for (int i = 0; i < 3; i++)
            {
                var card = this.deck.GetNextCard();
                this.secondPlayer.AddCard(card);
            }

            for (int i = 0; i < 3; i++)
            {
                var card = this.deck.GetNextCard();
                this.firstPlayer.AddCard(card);
            }

            for (int i = 0; i < 3; i++)
            {
                var card = this.deck.GetNextCard();
                this.secondPlayer.AddCard(card);
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
            get { return this.firstPlayerCollectedCards.Count > 0; }
        }

        public bool SecondPlayerHasHand
        {
            get { return this.secondPlayerCollectedCards.Count > 0; }
        }

        public PlayerPosition ClosedByPlayer
        {
            get { throw new NotImplementedException(); }
        }
    }
}
