namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayCardActionValidator
    {
        public bool CanPlayCard(
            bool isThePlayerFirst,
            Card playedCard,
            Card otherPlayerCard,
            Card trumpCard,
            IList<Card> playerCards,
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

            if (otherPlayerCard.Suit != playedCard.Suit)
            {
                if (playedCard.Suit != trumpCard.Suit)
                {
                    var hasTrump = playerCards.Any(c => c.Suit == trumpCard.Suit);
                    if (hasTrump)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (playedCard.GetValue() < otherPlayerCard.GetValue())
                {
                    var hasBigger = playerCards.Any(c => c.GetValue() > otherPlayerCard.GetValue());
                    if (hasBigger)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
