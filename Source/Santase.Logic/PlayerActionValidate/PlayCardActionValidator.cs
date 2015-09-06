namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    internal static class PlayCardActionValidator
    {
        public static bool CanPlayCard(
            bool isThePlayerFirst,
            Card playedCard,
            Card otherPlayerCard,
            Card trumpCard,
            ICollection<Card> playerCards,
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

            if (otherPlayerCard.Suit == playedCard.Suit)
            {
                // Played bigger card of the same suit - OK
                if (playedCard.GetValue() > otherPlayerCard.GetValue())
                {
                    return true;
                }

                // When a card is led, the opponent must play a higher card of the same suit if possible
                var hasBigger =
                    playerCards.Any(c => c.GetValue() > otherPlayerCard.GetValue() && c.Suit == otherPlayerCard.Suit);
                if (hasBigger)
                {
                    return false;
                }
            }
            else
            {
                // Having no higher card, the second player MUST play a lower card of the suit that was led
                var hasSameSuit = playerCards.Any(c => c.Suit == otherPlayerCard.Suit);
                if (hasSameSuit)
                {
                    return false;
                }

                // Player has no card of the same suit and plays trump - OK
                if (playedCard.Suit == trumpCard.Suit)
                {
                    return true;
                }

                // If the player has no card of the suit played by the first player he must play a trump if possible
                var hasTrump = playerCards.Any(c => c.Suit == trumpCard.Suit);
                if (hasTrump)
                {
                    return false;
                }
            }

            // Having no cards of the suit led and no trumps, the second player may throw any card.
            return true;
        }
    }
}
