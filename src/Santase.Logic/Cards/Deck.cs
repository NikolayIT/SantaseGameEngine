namespace Santase.Logic.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// The 24-card Santase talon.
    /// <para>
    /// How a deal is produced (stable, so a verifier can recreate any deal from the random
    /// numbers it was built from):
    /// </para>
    /// <list type="number">
    /// <item><description>Start from <see cref="UnshuffledOrder"/>: suits Club, Diamond, Heart,
    /// Spade, each as Nine, Ten, Jack, Queen, King, Ace (index = suit * 6 + rank).</description></item>
    /// <item><description>Fisher-Yates from the end: for i = 23 down to 1, draw j = nextInt(i + 1)
    /// (uniform in 0..i) and swap positions i and j. That is 23 draws with exclusive maxima
    /// 24, 23, ..., 2.</description></item>
    /// <item><description>Position 0 of the shuffled array is the face-up trump card; cards are
    /// drawn from the other end (position 23 first), so the trump card is drawn last.</description></item>
    /// </list>
    /// </summary>
    public class Deck : IDeck
    {
        private static readonly Card[] AllCards = CreateAllCards();

        private static readonly ReadOnlyCollection<Card> AllCardsReadOnly = Array.AsReadOnly(AllCards);

        // The shuffled deck. Index 0 is the trump (drawn last); cards are drawn from the
        // top (index remaining-1) downwards. A single array + index pointer replaces the
        // previous List + LINQ Shuffle().ToList() (which allocated a buffer, an iterator
        // and a backing list every round).
        private readonly Card[] cards;

        private int remaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deck"/> class, shuffled with
        /// <see cref="Random.Shared"/>.
        /// </summary>
        public Deck()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deck"/> class, shuffled with the given
        /// random source (see the class remarks for the exact draw order).
        /// </summary>
        /// <param name="nextInt">Returns a uniformly distributed integer in [0, exclusiveMax).
        /// Null uses <see cref="Random.Shared"/>, which is thread-safe.</param>
        public Deck(Func<int, int> nextInt)
        {
            this.cards = new Card[AllCards.Length];
            Array.Copy(AllCards, this.cards, AllCards.Length);

            if (nextInt == null)
            {
                var rng = Random.Shared;
                for (var i = this.cards.Length - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (this.cards[i], this.cards[j]) = (this.cards[j], this.cards[i]);
                }
            }
            else
            {
                for (var i = this.cards.Length - 1; i > 0; i--)
                {
                    var j = nextInt(i + 1);
                    if (j < 0 || j > i)
                    {
                        throw new InternalGameException($"The shuffle source returned {j} for a value in [0, {i + 1}).");
                    }

                    (this.cards[i], this.cards[j]) = (this.cards[j], this.cards[i]);
                }
            }

            this.remaining = this.cards.Length;
            this.TrumpCard = this.cards[0];
        }

        /// <summary>
        /// Gets the 24 cards in the order a shuffle starts from (see the class remarks).
        /// </summary>
        public static IReadOnlyList<Card> UnshuffledOrder => AllCardsReadOnly;

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
