namespace Santase.AI.SmartPlayer.Strategies
{
    using System.Collections.Generic;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class PlayingFirstAndRulesDoNotApplyStrategy : BaseChooseCardStrategy
    {
        public PlayingFirstAndRulesDoNotApplyStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Announce 40 or 20 if possible
            var cardFor20Or40 = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (cardFor20Or40 != null)
            {
                // When playing a trump card and then announcing 40 or 20 will win the round then do it.
                var opponentHasTrump = CountOfSuit(this.Tracker.UnknownCards, context.TrumpCard.Suit) > 0;
                var cardWhichWillSurelyWinTheTrick = this.GetCardWhichWillSurelyWinTheTrick(context.TrumpCard.Suit, opponentHasTrump);
                if (cardWhichWillSurelyWinTheTrick != null)
                {
                    var points = context.FirstPlayerRoundPoints;
                    points += cardWhichWillSurelyWinTheTrick.GetValue();
                    if (cardFor20Or40.Suit == context.TrumpCard.Suit)
                    {
                        points += 40;
                    }
                    else
                    {
                        points += 20;
                    }

                    if (points >= 66)
                    {
                        return PlayerAction.PlayCard(cardWhichWillSurelyWinTheTrick);
                    }
                }

                return PlayerAction.PlayCard(cardFor20Or40);
            }

            // If the player is close to the win => play trump card which will surely win the trick
            var cardToWinTheGame = this.GetCardWhichWillSurelyWinTheGame(
                context.TrumpCard.Suit,
                context.FirstPlayerRoundPoints,
                possibleCardsToPlay);
            if (cardToWinTheGame != null)
            {
                return PlayerAction.PlayCard(cardToWinTheGame);
            }

            // Smallest non-trump card from the shortest opponent suit
            var cardToPlay = this.SmallestNonTrumpFromShortestOpponentSuit(possibleCardsToPlay, context.TrumpCard.Suit);
            if (cardToPlay != null)
            {
                return PlayerAction.PlayCard(cardToPlay);
            }

            // Should never happen
            cardToPlay = Lowest(possibleCardsToPlay);
            return PlayerAction.PlayCard(cardToPlay);
        }
    }
}
