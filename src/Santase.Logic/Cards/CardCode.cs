namespace Santase.Logic.Cards
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Compact card codes: the rank (9, 10, J, Q, K, A) followed by the suit (C, D, H, S), e.g.
    /// "9C", "10D", "QH", "AS". <see cref="Parse"/> returns the shared <see cref="Card"/> instances.
    /// </summary>
    public static class CardCode
    {
        /// <summary>
        /// Formats a Santase card as its code.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <returns>The code, e.g. "QH".</returns>
        public static string Format(Card card)
        {
            ArgumentNullException.ThrowIfNull(card);
            var rank = card.Type switch
            {
                CardType.Nine => "9",
                CardType.Ten => "10",
                CardType.Jack => "J",
                CardType.Queen => "Q",
                CardType.King => "K",
                CardType.Ace => "A",
                _ => throw new ArgumentException($"{card.Type} is not a Santase card.", nameof(card)),
            };

            return rank + card.Suit switch
            {
                CardSuit.Club => "C",
                CardSuit.Diamond => "D",
                CardSuit.Heart => "H",
                _ => "S",
            };
        }

        /// <summary>
        /// Parses a card code (case-insensitive).
        /// </summary>
        /// <param name="code">The code, e.g. "QH".</param>
        /// <returns>The card.</returns>
        /// <exception cref="FormatException">The code is not a Santase card.</exception>
        public static Card Parse(string code)
        {
            if (!TryParse(code, out var card))
            {
                throw new FormatException($"'{code}' is not a Santase card code (rank 9, 10, J, Q, K or A followed by suit C, D, H or S).");
            }

            return card;
        }

        /// <summary>
        /// Tries to parse a card code (case-insensitive).
        /// </summary>
        /// <param name="code">The code, e.g. "QH".</param>
        /// <param name="card">The card, or null.</param>
        /// <returns>Whether the code is a Santase card.</returns>
        public static bool TryParse(string code, [NotNullWhen(true)] out Card card)
        {
            card = null;
            if (string.IsNullOrEmpty(code) || code.Length < 2 || code.Length > 3)
            {
                return false;
            }

            CardSuit suit;
            switch (char.ToUpperInvariant(code[^1]))
            {
                case 'C':
                    suit = CardSuit.Club;
                    break;
                case 'D':
                    suit = CardSuit.Diamond;
                    break;
                case 'H':
                    suit = CardSuit.Heart;
                    break;
                case 'S':
                    suit = CardSuit.Spade;
                    break;
                default:
                    return false;
            }

            CardType type;
            switch (code.Substring(0, code.Length - 1).ToUpperInvariant())
            {
                case "9":
                    type = CardType.Nine;
                    break;
                case "10":
                    type = CardType.Ten;
                    break;
                case "J":
                    type = CardType.Jack;
                    break;
                case "Q":
                    type = CardType.Queen;
                    break;
                case "K":
                    type = CardType.King;
                    break;
                case "A":
                    type = CardType.Ace;
                    break;
                default:
                    return false;
            }

            card = Card.GetCard(suit, type);
            return true;
        }
    }
}
