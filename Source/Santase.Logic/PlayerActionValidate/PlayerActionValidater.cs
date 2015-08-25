namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayerActionValidater : IPlayerActionValidater
    {
        public bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards)
        {
            if (!context.AmITheFirstPlayer)
            {
                action.Announce = Announce.None;
            }

            if (action.Type == PlayerActionType.PlayCard)
            {
                if (!CanPlayCard(action, context, playerCards))
                {
                    return false;
                }
            }

            if (action.Type == PlayerActionType.CloseGame)
            {
                if (!CanCloseGame(context))
                {
                    return false;
                }
            }

            if (action.Type == PlayerActionType.ChangeTrump)
            {
                if (!CanChangeTrump(context, playerCards))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanCloseGame(PlayerTurnContext context)
        {
            if (!context.State.CanClose || !context.AmITheFirstPlayer)
            {
                return false;
            }

            return true;
        }

        private static bool CanPlayCard(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards)
        {
            if (!playerCards.Contains(action.Card))
            {
                return false;
            }

            if (action.Announce != Announce.None)
            {
                if (action.Card.Type != CardType.Queen && action.Card.Type != CardType.King)
                {
                    action.Announce = Announce.None;
                }

                // TODO: Check for another card
            }

            if (context.State.ShouldObserveRules)
            {
                if (!context.AmITheFirstPlayer)
                {
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
                }
            }

            return true;
        }

        private static bool CanChangeTrump(PlayerTurnContext context, IList<Card> playerCards)
        {
            if (!context.State.CanChangeTrump || !context.AmITheFirstPlayer)
            {
                return false;
            }

            if (!playerCards.Contains(new Card(context.TrumpCard.Suit, CardType.Nine)))
            {
                return false;
            }

            return true;
        }
    }
}
