namespace Santase.Logic.Cards
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Extensions;

    public class Deck : IDeck
    {
        private readonly IList<Card> listOfCards;

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

            this.trumpCard = this.listOfCards[0];
        }

        public Card GetTrumpCard => this.trumpCard;

        public int CardsLeft => this.listOfCards.Count;

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
    }
}
