using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.Cards
{
    public static class CardExtensions
    {
        public static string ToFriendlyString(this CardSuit cardSuit)
        {
            switch(cardSuit)
            {
                case CardSuit.Club:
                    return "♣";
                case CardSuit.Diamond:
                    return "♦";
                case CardSuit.Heart:
                    return "♥";
                case CardSuit.Spade:
                    return "♠";
                default:
                    throw new ArgumentException("cardSuit");
            }
        }

        public static string ToFriendlyString(this CardType cardType)
        {
            switch(cardType)
            {
                case CardType.Nine:
                    return "9";
                case CardType.Ten:
                    return "10";
                case CardType.Jack:
                    return "J";
                case CardType.Queen:
                    return "Q";
                case CardType.King:
                    return "K";
                case CardType.Ace:
                    return "A";
                default:
                    throw new ArgumentException("cardType");
            }
        }
    }
}
