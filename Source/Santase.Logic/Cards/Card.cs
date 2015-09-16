namespace Santase.Logic.Cards
{
    /// <summary>
    /// Immutable object to represent game card with suit and type.
    /// </summary>
    public class Card : IDeepCloneable<Card>
    {
        public Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
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
            switch (this.Type)
            {
                case CardType.Nine:
                    return 0;
                case CardType.Jack:
                    return 2;
                case CardType.Queen:
                    return 3;
                case CardType.King:
                    return 4;
                case CardType.Ten:
                    return 10;
                case CardType.Ace:
                    return 11;
                default:
                    throw new InternalGameException("Invalid card type in GetValue()");
            }
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

        public Card DeepClone()
        {
            return new Card(this.Suit, this.Type);
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
