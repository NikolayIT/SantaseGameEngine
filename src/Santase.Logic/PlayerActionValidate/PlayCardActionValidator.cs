namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

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
                // ReSharper disable once LoopCanBeConvertedToQuery (performance critical)
                foreach (var card in playerCards)
                {
                    if (card.Suit == otherPlayerCard.Suit && card.GetValue() > otherPlayerCard.GetValue())
                    {
                        // Found bigger card which is not played => wrong action
                        return false;
                    }
                }
            }
            else
            {
                // Having no higher card, the second player MUST play a lower card of the suit that was led
                // ReSharper disable once LoopCanBeConvertedToQuery (performance critical)
                foreach (var card in playerCards)
                {
                    if (card.Suit == otherPlayerCard.Suit)
                    {
                        // Found same suit card which is not played => wrong action
                        return false;
                    }
                }

                // Player has no card of the same suit and plays trump - OK
                if (playedCard.Suit == trumpCard.Suit)
                {
                    return true;
                }

                // If the player has no card of the suit played by the first player he must play a trump if possible
                // ReSharper disable once LoopCanBeConvertedToQuery (performance critical)
                foreach (var card in playerCards)
                {
                    // Found trump card which is not played => wrong action
                    if (card.Suit == trumpCard.Suit)
                    {
                        return false;
                    }
                }
            }

            // Having no cards of the suit led and no trumps, the second player may throw any card.
            return true;
        }
    }
}
