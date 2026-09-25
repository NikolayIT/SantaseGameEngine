namespace Santase.Logic.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;

    // A talon with exactly the given cards, for rounds prepared by hand: the cards are drawn in
    // the given order and the last one is the face-up trump (drawn last), like Deck.
    internal sealed class TestDeck : IDeck
    {
        private readonly List<Card> cards;

        private int next;

        public TestDeck(string cardsInDrawOrderTrumpLast)
        {
            this.cards = TestCards.List(cardsInDrawOrderTrumpLast);
            this.TrumpCard = this.cards[^1];
        }

        public Card TrumpCard { get; private set; }

        public int CardsLeft => this.cards.Count - this.next;

        public Card GetNextCard()
        {
            if (this.CardsLeft == 0)
            {
                throw new InternalGameException("Deck is empty!");
            }

            return this.cards[this.next++];
        }

        public void ChangeTrumpCard(Card newCard)
        {
            this.TrumpCard = newCard;
            if (this.CardsLeft > 0)
            {
                this.cards[^1] = newCard;
            }
        }
    }
}
