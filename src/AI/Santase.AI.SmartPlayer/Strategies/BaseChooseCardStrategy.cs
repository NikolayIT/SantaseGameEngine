namespace Santase.AI.SmartPlayer.Strategies
{
    using System.Collections.Generic;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public abstract class BaseChooseCardStrategy : IChooseCardStrategy
    {
        // Enum.GetValues(typeof(CardSuit)) order.
        private static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        // Card types from the highest value to the lowest.
        private static readonly CardType[] TypesByDescendingValue =
        {
            CardType.Ace, CardType.Ten, CardType.King, CardType.Queen, CardType.Jack, CardType.Nine,
        };

        protected BaseChooseCardStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
        {
            this.Tracker = cardTracker;
            this.Validator = announceValidator;
            this.Cards = cards;
        }

        protected CardTracker Tracker { get; }

        protected IAnnounceValidator Validator { get; }

        protected ICollection<Card> Cards { get; }

        public abstract PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay);

        // The card queries below replace LINQ OrderBy/FirstOrDefault chains with single passes. The
        // tie-breaks are the same: OrderBy is stable, so the chains returned the FIRST card (in
        // collection order) among equal keys, which is what the strict comparisons keep. Card values
        // are unique within a suit, so per-suit highest/lowest never tie.

        // The highest card of the suit, or null.
        protected static Card HighestOfSuit(IEnumerable<Card> cards, CardSuit suit)
        {
            Card highest = null;
            foreach (var card in cards)
            {
                if (card.Suit == suit && (highest == null || card.GetValue() > highest.GetValue()))
                {
                    highest = card;
                }
            }

            return highest;
        }

        // The lowest card of the suit, or null.
        protected static Card LowestOfSuit(IEnumerable<Card> cards, CardSuit suit)
        {
            Card lowest = null;
            foreach (var card in cards)
            {
                if (card.Suit == suit && (lowest == null || card.GetValue() < lowest.GetValue()))
                {
                    lowest = card;
                }
            }

            return lowest;
        }

        // The first lowest-value card, or null.
        protected static Card Lowest(IEnumerable<Card> cards)
        {
            Card lowest = null;
            foreach (var card in cards)
            {
                if (lowest == null || card.GetValue() < lowest.GetValue())
                {
                    lowest = card;
                }
            }

            return lowest;
        }

        protected static int CountOfSuit(CardCollection cards, CardSuit suit)
        {
            var count = 0;
            foreach (var card in cards)
            {
                if (card.Suit == suit)
                {
                    count++;
                }
            }

            return count;
        }

        // The first non-trump card from the suit the opponent has the fewest unknown cards of,
        // lowest value first; null when every card is a trump.
        protected Card SmallestNonTrumpFromShortestOpponentSuit(IEnumerable<Card> cards, CardSuit trumpSuit)
        {
            Card best = null;
            var bestSuitCount = 0;
            foreach (var card in cards)
            {
                if (card.Suit == trumpSuit)
                {
                    continue;
                }

                var suitCount = CountOfSuit(this.Tracker.UnknownCards, card.Suit);
                if (best == null || suitCount < bestSuitCount || (suitCount == bestSuitCount && card.GetValue() < best.GetValue()))
                {
                    best = card;
                    bestSuitCount = suitCount;
                }
            }

            return best;
        }

        protected Card GetCardWhichWillSurelyWinTheGame(
            CardSuit trumpSuit,
            int playerRoundPoints,
            ICollection<Card> possibleCardsToPlay)
        {
            var opponentBiggestTrumpCard = HighestOfSuit(this.Tracker.UnknownCards, trumpSuit);

            // The opponent's possible trumps are exactly King and Queen (King is the higher one).
            var opponentHasOnlyKingAndQueen = opponentBiggestTrumpCard != null
                                              && opponentBiggestTrumpCard.Type == CardType.King
                                              && CountOfSuit(this.Tracker.UnknownCards, trumpSuit) == 2
                                              && this.Tracker.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Queen));

            if (opponentBiggestTrumpCard == null || opponentHasOnlyKingAndQueen)
            {
                foreach (var suit in AllSuits)
                {
                    var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(suit, false);
                    if (possibleCard != null)
                    {
                        return possibleCard;
                    }
                }
            }

            // My trumps from the biggest to the smallest.
            var sumOfPoints = 0;
            foreach (var type in TypesByDescendingValue)
            {
                var myTrumpCard = Card.GetCard(trumpSuit, type);
                if (!possibleCardsToPlay.Contains(myTrumpCard))
                {
                    continue;
                }

                sumOfPoints += myTrumpCard.GetValue();
                if (playerRoundPoints >= 66 - sumOfPoints)
                {
                    if (opponentBiggestTrumpCard == null
                        || myTrumpCard.GetValue() > opponentBiggestTrumpCard.GetValue())
                    {
                        return myTrumpCard;
                    }
                }
            }

            return null;
        }

        protected Card GetCardWhichWillSurelyWinTheTrick(CardSuit suit, bool opponentHasTrump)
        {
            var myBiggestCard = HighestOfSuit(this.Cards, suit);
            if (myBiggestCard == null)
            {
                return null;
            }

            var opponentBiggestCard = HighestOfSuit(this.Tracker.UnknownCards, suit);

            if (!opponentHasTrump && opponentBiggestCard == null)
            {
                return myBiggestCard;
            }

            if (opponentBiggestCard != null && opponentBiggestCard.GetValue() < myBiggestCard.GetValue())
            {
                return myBiggestCard;
            }

            return null;
        }

        protected Card TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            if (!context.State.CanAnnounce20Or40)
            {
                return null;
            }

            // Choose card with announce 40 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.Validator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Forty)
                {
                    return card;
                }
            }

            // Choose card with announce 20 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.Validator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Twenty)
                {
                    return card;
                }
            }

            return null;
        }
    }
}
