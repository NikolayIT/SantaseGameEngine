namespace Santase.Logic.Cards
{
    using System;

    /// <summary>
    /// Immutable object to represent game card with suit and type.
    /// </summary>
    public class Card
    {
        private static readonly Card[] Cards = new Card[53];

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
            var code = ((int)suit * 13) + (int)type;
            if (code < 0 || code > 52)
            {
                throw new IndexOutOfRangeException("Invalid suit and type given.");
            }

            return Cards[code];
        }

        public static Card FromHashCode(int hashCode)
        {
            var suitId = (hashCode - 1) / 13;
            return GetCard((CardSuit)suitId, (CardType)(hashCode - (suitId * 13)));
        }

        public int GetValue()
        {
            return this.value;
        }

        public override bool Equals(object obj)
        {
            var anotherCard = obj as Card;
            return anotherCard != null && this.Suit == anotherCard.Suit && this.Type == anotherCard.Type;
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
