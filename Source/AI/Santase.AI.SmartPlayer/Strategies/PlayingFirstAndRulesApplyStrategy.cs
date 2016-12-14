namespace Santase.AI.SmartPlayer.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class PlayingFirstAndRulesApplyStrategy : BaseChooseCardStrategy
    {
        public PlayingFirstAndRulesApplyStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Find card that will surely win the trick
            var opponentHasTrump = this.Tracker.UnknownCards.Any(x => x.Suit == context.TrumpCard.Suit);

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(context.TrumpCard.Suit, opponentHasTrump);
            if (trumpCard != null)
            {
                return PlayerAction.PlayCard(trumpCard);
            }

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(
                    suit,
                    opponentHasTrump);
                if (possibleCard != null)
                {
                    return PlayerAction.PlayCard(possibleCard);
                }
            }

            // Announce 40 or 20 if possible
            var cardFor20Or40 = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (cardFor20Or40 != null)
            {
                return PlayerAction.PlayCard(cardFor20Or40);
            }

            // Smallest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => this.Tracker.UnknownCards.Count(y => y.Suit == x.Suit))
                    .ThenBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return PlayerAction.PlayCard(cardToPlay);
            }

            // Smallest card
            cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return PlayerAction.PlayCard(cardToPlay);
        }
    }
}
