namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayCardActionValidator
    {
        public bool CanPlayCard(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards)
        {
            if (!playerCards.Contains(action.Card))
            {
                return false;
            }

            if (!context.State.ShouldObserveRules)
            {
                // When rules does not apply every card is valid
                return true;
            }

            if (context.AmITheFirstPlayer)
            {
                // When the player is first he can play every card
                return true;
            }

            var firstCard = context.FirstPlayedCard;
            var ourCard = action.Card;

            if (firstCard.Suit != ourCard.Suit)
            {
                if (ourCard.Suit != context.TrumpCard.Suit)
                {
                    var hasTrump = playerCards.Any(c => c.Suit == context.TrumpCard.Suit);
                    if (hasTrump)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (ourCard.GetValue() < firstCard.GetValue())
                {
                    var hasBigger = playerCards.Any(c => c.GetValue() > firstCard.GetValue());
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