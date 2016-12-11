namespace Santase.Logic.Cards
{
    /// <summary>
    /// Immutable object to represent game card with suit and type.
    /// </summary>
    public class Card
    {
        private static readonly int[] CardValues = { 0, 11, 0, 0, 0, 0, 0, 0, 0, 0, 10, 2, 3, 4 };

        private readonly int value;

        public Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
            this.value = CardValues[(int)this.Type];
        }

        public CardSuit Suit { get; }

        public CardType Type { get; }

        public static Card FromHashCode(int hashCode)
        {
            var suitId = (hashCode - 1) / 13;
            return new Card((CardSuit)suitId, (CardType)(hashCode - (suitId * 13)));
        }

        public int GetValue()
        {
            return this.value;
        }

        public override bool Equals(object obj)
        {
            var anotherCard = obj as Card;
            return anotherCard != null && this.Equals(anotherCard);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)this.Suit * 13) + (int)this.Type;
            }
        }

        public override string ToString()
        {
            return $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";
        }

        private bool Equals(Card other)
        {
            return this.Suit == other.Suit && this.Type == other.Type;
        }
    }
}
