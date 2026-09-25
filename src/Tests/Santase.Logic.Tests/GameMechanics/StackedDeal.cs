namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    // Shuffle sources that make the real Deck produce an exact deal, so tests can set up any
    // hands while still going through the public shuffle path. Works by running Fisher-Yates
    // backwards: at step i, pick the index of the card that must end up at position i.
    internal static class StackedDeal
    {
        // The deal: face-up trump, the first player's six cards, the second player's six, and
        // the talon in the order it will be drawn. Cards left out go to the end of the talon
        // (drawn last before the trump), in unshuffled order.
        public static Func<int, int> Create(
            string trump,
            string firstHand,
            string secondHand,
            string talonInDrawOrder = "")
        {
            var trumpCard = TestCards.Parse(trump);
            var first = TestCards.List(firstHand);
            var second = TestCards.List(secondHand);
            var talon = TestCards.List(talonInDrawOrder);

            var used = new HashSet<Card>(first.Concat(second).Concat(talon)) { trumpCard };
            talon.AddRange(Deck.UnshuffledOrder.Where(c => !used.Contains(c)));

            // Deck positions: 0 = trump, 23..18 = first hand (drawn first), 17..12 = second
            // hand, 11..1 = talon in draw order.
            var order = new Card[24];
            order[0] = trumpCard;
            for (var k = 0; k < 6; k++)
            {
                order[23 - k] = first[k];
                order[17 - k] = second[k];
            }

            for (var k = 0; k < 11; k++)
            {
                order[11 - k] = talon[k];
            }

            return ForOrder(order);
        }

        public static Func<int, int> ForOrder(IReadOnlyList<Card> order)
        {
            if (order.Count != 24 || order.Distinct().Count() != 24)
            {
                throw new ArgumentException("A deal needs all 24 cards exactly once.", nameof(order));
            }

            var current = Deck.UnshuffledOrder.ToArray();
            var draws = new Queue<int>();
            for (var i = 23; i > 0; i--)
            {
                var j = Array.IndexOf(current, order[i], 0, i + 1);
                draws.Enqueue(j);
                (current[i], current[j]) = (current[j], current[i]);
            }

            return max =>
            {
                var j = draws.Dequeue();
                if (j >= max)
                {
                    throw new InvalidOperationException("The stacked deal was consumed out of order.");
                }

                return j;
            };
        }
    }
}
