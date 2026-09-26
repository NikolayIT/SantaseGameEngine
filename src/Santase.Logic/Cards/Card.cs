namespace Santase.Logic.Cards
{
    using System;

    /// <summary>
    /// Immutable object to represent game card with suit and type.
    /// </summary>
    public sealed class Card
    {
        // The 24 cards by hash code (suit * 13 + rank); the unused ranks 2-8 are empty. Internal: the
        // whole process shares these instances, so no code outside the engine may write into it.
        internal static readonly Card[] Cards = new Card[53];

        private static readonly int[] CardValues = { 0, 11, 0, 0, 0, 0, 0, 0, 0, 0, 10, 2, 3, 4 };

        private readonly int value;

        private readonly int hashCode;

        static Card()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
#pragma warning disable 618
                    var card = new Card(suit, type);
#pragma warning restore 618
                    Cards[card.hashCode] = card;
                }
            }
        }

        [Obsolete("For performance considerations use Card.GetCard instead of Card.ctor()")]
        public Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
            this.value = CardValues[(int)this.Type];
            this.hashCode = ((int)this.Suit * 13) + (int)this.Type;
        }

        public CardSuit Suit { get; }

        public CardType Type { get; }

        public static Card GetCard(CardSuit suit, CardType type)
        {
            // A range check on the code alone let rank 0 or 14 wrap into the neighbouring suit (0
            // was the King of the previous suit); the ranks 2-8 are empty slots (null).
            var inRange = suit >= CardSuit.Club && suit <= CardSuit.Spade && type >= CardType.Ace && type <= CardType.King;
            var card = inRange ? Cards[((int)suit * 13) + (int)type] : null;
            if (card == null)
            {
                throw new IndexOutOfRangeException("Invalid suit and type given.");
            }

            return card;
        }

        /// <summary>
        /// Gets the card whose <see cref="GetHashCode"/> is <paramref name="hashCode"/>, for players that
        /// keep cards as bits of a mask.
        /// </summary>
        /// <param name="hashCode">A card's hash code: suit * 13 + rank (Ace 1, Nine 9 ... King 13).</param>
        /// <returns>The shared instance of that card.</returns>
        /// <exception cref="IndexOutOfRangeException">No Santase card has that hash code.</exception>
        public static Card FromHashCode(int hashCode)
        {
            var card = (uint)hashCode < (uint)Cards.Length ? Cards[hashCode] : null;
            if (card == null)
            {
                throw new IndexOutOfRangeException("Invalid card hash code given.");
            }

            return card;
        }

        public int GetValue()
        {
            return this.value;
        }

        public override bool Equals(object obj)
        {
            return obj is Card anotherCard && this.Suit == anotherCard.Suit && this.Type == anotherCard.Type;
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }

        public override string ToString()
        {
            return $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";
        }
    }
}
