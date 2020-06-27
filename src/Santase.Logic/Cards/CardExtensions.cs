﻿namespace Santase.Logic.Cards
{
    using System;

    public static class CardExtensions
    {
        public static string ToFriendlyString(this CardSuit cardSuit)
        {
            switch (cardSuit)
            {
                case CardSuit.Club:
                    return "\u2663"; // ♣
                case CardSuit.Diamond:
                    return "\u2666"; // ♦
                case CardSuit.Heart:
                    return "\u2665"; // ♥
                case CardSuit.Spade:
                    return "\u2660"; // ♠
                default:
                    throw new ArgumentException("cardSuit");
            }
        }

        public static int MapAsSortableByColor(this CardSuit cardSuit)
        {
            switch (cardSuit)
            {
                case CardSuit.Club:
                    return 3;
                case CardSuit.Diamond:
                    return 4;
                case CardSuit.Heart:
                    return 2;
                case CardSuit.Spade:
                    return 1;
                default:
                    throw new ArgumentException("cardSuit");
            }
        }

        public static string ToFriendlyString(this CardType cardType)
        {
            switch (cardType)
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
