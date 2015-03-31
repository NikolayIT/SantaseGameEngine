using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santase.Logic.Extensions;

namespace Santase.Logic.Cards
{
    public class Deck : IDeck
    {
        private IList<Card> listOfCards;

        private Card trumpCard;

        public Deck()
        {
            this.listOfCards = new List<Card>();
            foreach (var cardSuit in this.GetAllCardSuits())
            {
                foreach (var cardType in this.GetAllCardTypes())
                {
                    this.listOfCards.Add(new Card(cardSuit, cardType));
                }
            }

            this.listOfCards = this.listOfCards.Shuffle().ToList();

            this.trumpCard = listOfCards[0];
        }

        public Card GetNextCard()
        {
            if (this.listOfCards.Count == 0)
            {
                throw new InternalGameException("Deck is empty!");
            }

            var card = this.listOfCards[this.listOfCards.Count - 1];
            this.listOfCards.RemoveAt(this.listOfCards.Count - 1);
            return card;
        }

        public Card GetTrumpCard
        {
            get { return this.trumpCard; }
        }

        public void ChangeTrumpCard(Card newCard)
        {
            this.trumpCard = newCard;
            if (this.listOfCards.Count > 0)
            {
                this.listOfCards[0] = newCard;
            }
        }

        private IEnumerable<CardType> GetAllCardTypes()
        {
            return new List<CardType>
            {
                CardType.Nine,
                CardType.Ten,
                CardType.Jack,
                CardType.Queen,
                CardType.King,
                CardType.Ace,
            };
        }

        private IEnumerable<CardSuit> GetAllCardSuits()
        {
            return new List<CardSuit>
            {
                CardSuit.Club,
                CardSuit.Diamond,
                CardSuit.Heart,
                CardSuit.Spade,
            };
        }


        public int CardsLeft
        {
            get { return this.listOfCards.Count; }
        }
    }
}
