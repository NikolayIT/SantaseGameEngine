namespace Santase.Logic.Cards
{
    using System;

    public class Deck : IDeck
    {
        private static readonly Card[] AllCards = CreateAllCards();

        // The shuffled deck. Index 0 is the trump (drawn last); cards are drawn from the
        // top (index remaining-1) downwards. A single array + index pointer replaces the
        // previous List + LINQ Shuffle().ToList() (which allocated a buffer, an iterator
        // and a backing list every round).
        private readonly Card[] cards;

        private int remaining;

        public Deck()
        {
            this.cards = new Card[AllCards.Length];
            Array.Copy(AllCards, this.cards, AllCards.Length);

            // In-place Fisher-Yates. Random.Shared is thread-safe, matching the previous
            // Enumerable.Shuffle() so the parallel simulator stays correct.
            var rng = Random.Shared;
            for (var i = this.cards.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (this.cards[i], this.cards[j]) = (this.cards[j], this.cards[i]);
            }

            this.remaining = this.cards.Length;
            this.TrumpCard = this.cards[0];
        }

        public Card TrumpCard { get; private set; }

        public int CardsLeft => this.remaining;

        public Card GetNextCard()
        {
            if (this.remaining == 0)
            {
                throw new InternalGameException("Deck is empty!");
            }

            return this.cards[--this.remaining];
        }

        public void ChangeTrumpCard(Card newCard)
        {
            this.TrumpCard = newCard;
            if (this.remaining > 0)
            {
                this.cards[0] = newCard;
            }
        }

        private static Card[] CreateAllCards()
        {
            var allTypes = new[] { CardType.Nine, CardType.Ten, CardType.Jack, CardType.Queen, CardType.King, CardType.Ace };
            var allSuits = new[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade };
            var result = new Card[allSuits.Length * allTypes.Length];
            var index = 0;
            foreach (var suit in allSuits)
            {
                foreach (var type in allTypes)
                {
                    result[index++] = Card.GetCard(suit, type);
                }
            }

            return result;
        }
    }
}
