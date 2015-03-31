using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.Cards
{
    public class Card
    {
        public Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
        }

        public CardSuit Suit { get; private set; }

        public CardType Type { get; private set; }

        public int GetValue()
        {
            switch(this.Type)
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
            if (anotherCard == null)
            {
                return false;
            }

            return this.Suit == anotherCard.Suit
                && this.Type == anotherCard.Type;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}",
                this.Type.ToFriendlyString(),
                this.Suit.ToFriendlyString());
        }
    }
}
