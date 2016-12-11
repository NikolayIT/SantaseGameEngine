namespace Santase.AI.SmartPlayer.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public abstract class BaseChooseCardStrategy : IChooseCardStrategy
    {
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

        protected Card GetCardWhichWillSurelyWinTheGame(
            CardSuit trumpSuit,
            int playerRoundPoints,
            ICollection<Card> possibleCardsToPlay)
        {
            var opponentSuitCards =
                this.Tracker.UnknownCards.Where(x => x.Suit == trumpSuit)
                    .OrderByDescending(x => x.GetValue())
                    .ToList();
            var opponentBiggestTrumpCard = opponentSuitCards.FirstOrDefault();

            if (opponentBiggestTrumpCard == null ||
                (opponentSuitCards.Count == 2 && opponentSuitCards[0].Type == CardType.King && opponentSuitCards[1].Type == CardType.Queen))
            {
                foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                {
                    var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(suit, false);
                    if (possibleCard != null)
                    {
                        return possibleCard;
                    }
                }
            }

            var myBiggestTrumpCards =
                possibleCardsToPlay.Where(x => x.Suit == trumpSuit).OrderByDescending(x => x.GetValue());

            var sumOfPoints = 0;
            foreach (var myTrumpCard in myBiggestTrumpCards)
            {
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
            var myBiggestCard =
                this.Cards.Where(x => x.Suit == suit).OrderByDescending(x => x.GetValue()).FirstOrDefault();
            if (myBiggestCard == null)
            {
                return null;
            }

            var opponentBiggestCard =
                this.Tracker.UnknownCards.Where(x => x.Suit == suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();

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
