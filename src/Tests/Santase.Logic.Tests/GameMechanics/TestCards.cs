namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    // Readable card literals for tests: "9C 10D JH QS KC AD" (rank 9/10/J/Q/K/A, suit C/D/H/S).
    internal static class TestCards
    {
        public static Card Parse(string code)
        {
            var suit = code[^1] switch
            {
                'C' => CardSuit.Club,
                'D' => CardSuit.Diamond,
                'H' => CardSuit.Heart,
                'S' => CardSuit.Spade,
                _ => throw new ArgumentException($"Unknown suit in '{code}'.", nameof(code)),
            };
            var type = code[..^1] switch
            {
                "9" => CardType.Nine,
                "10" => CardType.Ten,
                "J" => CardType.Jack,
                "Q" => CardType.Queen,
                "K" => CardType.King,
                "A" => CardType.Ace,
                _ => throw new ArgumentException($"Unknown rank in '{code}'.", nameof(code)),
            };
            return Card.GetCard(suit, type);
        }

        public static List<Card> List(string codes)
        {
            var cards = new List<Card>();
            foreach (var code in codes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                cards.Add(Parse(code));
            }

            return cards;
        }
    }
}
