namespace Santase.AI.SmartPlayer.Strategies
{
    using System.Collections.Generic;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class PlayingFirstAndRulesApplyStrategy : BaseChooseCardStrategy
    {
        // Enum.GetValues(typeof(CardSuit)) order.
        private static readonly CardSuit[] CardSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        public PlayingFirstAndRulesApplyStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Find card that will surely win the trick
            var opponentHasTrump = CountOfSuit(this.Tracker.UnknownCards, context.TrumpCard.Suit) > 0;

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(context.TrumpCard.Suit, opponentHasTrump);
            if (trumpCard != null)
            {
                return PlayerAction.PlayCard(trumpCard);
            }

            foreach (var suit in CardSuits)
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
            var cardToPlay = this.SmallestNonTrumpFromShortestOpponentSuit(possibleCardsToPlay, context.TrumpCard.Suit);
            if (cardToPlay != null)
            {
                return PlayerAction.PlayCard(cardToPlay);
            }

            // Smallest card
            cardToPlay = Lowest(possibleCardsToPlay);
            return PlayerAction.PlayCard(cardToPlay);
        }
    }
}
