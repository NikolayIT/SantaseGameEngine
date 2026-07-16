namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    internal static class PlayCardActionValidator
    {
        // Precomputed masks over the CardCollection bit layout (bit index =
        // Card.GetHashCode() = suit*13 + type). SuitMasks[suit] holds every card of that
        // suit; HigherSameSuitMasks[card] holds the same-suit cards with a strictly higher
        // value. They turn the "does the hand contain ..." scans below into single AND
        // tests instead of hand enumerations.
        private static readonly long[] SuitMasks = CreateSuitMasks();

        private static readonly long[] HigherSameSuitMasks = CreateHigherSameSuitMasks();

        // Dispatcher kept for the public ICollection<Card> contract. The engine always
        // passes the concrete CardCollection, so it takes the no-boxing fast path below;
        // any other ICollection is copied once into a CardCollection (slow path, never hit
        // in the engine) so the rule logic lives in exactly one place.
        public static bool CanPlayCard(
            bool isThePlayerFirst,
            Card playedCard,
            Card otherPlayerCard,
            Card trumpCard,
            ICollection<Card> playerCards,
            bool shouldObserveRules)
        {
            if (playerCards is CardCollection cardCollection)
            {
                return CanPlayCard(isThePlayerFirst, playedCard, otherPlayerCard, trumpCard, cardCollection, shouldObserveRules);
            }

            var copy = new CardCollection();
            foreach (var card in playerCards)
            {
                copy.Add(card);
            }

            return CanPlayCard(isThePlayerFirst, playedCard, otherPlayerCard, trumpCard, copy, shouldObserveRules);
        }

        // Real implementation. The played card is still in playerCards at validation time,
        // but it can never satisfy the masks it is tested against (it is not higher than a
        // card it failed to beat, and it is excluded from the led/trump suit checks by the
        // surrounding branches), so no exclusion is needed.
        public static bool CanPlayCard(
            bool isThePlayerFirst,
            Card playedCard,
            Card otherPlayerCard,
            Card trumpCard,
            CardCollection playerCards,
            bool shouldObserveRules)
        {
            if (!playerCards.Contains(playedCard))
            {
                return false;
            }

            if (!shouldObserveRules)
            {
                // When rules does not apply every card is valid
                return true;
            }

            if (isThePlayerFirst)
            {
                // When the player is first he can play every card
                return true;
            }

            var handBits = playerCards.BitMask;
            if (otherPlayerCard.Suit == playedCard.Suit)
            {
                // Played bigger card of the same suit - OK
                if (playedCard.GetValue() > otherPlayerCard.GetValue())
                {
                    return true;
                }

                // When a card is led, the opponent must play a higher card of the same suit if possible
                return (handBits & HigherSameSuitMasks[otherPlayerCard.GetHashCode()]) == 0;
            }

            // Having no higher card, the second player MUST play a lower card of the suit that was led
            if ((handBits & SuitMasks[(int)otherPlayerCard.Suit]) != 0)
            {
                return false;
            }

            // Player has no card of the same suit and plays trump - OK
            if (playedCard.Suit == trumpCard.Suit)
            {
                return true;
            }

            // If the player has no card of the suit played by the first player he must play a trump if possible
            return (handBits & SuitMasks[(int)trumpCard.Suit]) == 0;
        }

        private static long[] CreateSuitMasks()
        {
            var masks = new long[4];
            foreach (var card in Card.Cards)
            {
                if (card != null)
                {
                    masks[(int)card.Suit] |= 1L << card.GetHashCode();
                }
            }

            return masks;
        }

        private static long[] CreateHigherSameSuitMasks()
        {
            var masks = new long[Card.Cards.Length];
            foreach (var card in Card.Cards)
            {
                if (card == null)
                {
                    continue;
                }

                long mask = 0;
                foreach (var other in Card.Cards)
                {
                    if (other != null && other.Suit == card.Suit && other.GetValue() > card.GetValue())
                    {
                        mask |= 1L << other.GetHashCode();
                    }
                }

                masks[card.GetHashCode()] = mask;
            }

            return masks;
        }
    }
}
