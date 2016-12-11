namespace Santase.AI.SmartPlayer.Strategies
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class PlayingSecondAndRulesDoNotApplyStrategy : BaseChooseCardStrategy
    {
        public PlayingSecondAndRulesDoNotApplyStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // If bigger card is available => play it
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                // If other player wins with this trick => take it
                if (context.FirstPlayedCard.GetValue() + biggerCard.GetValue() + context.FirstPlayerRoundPoints > 65)
                {
                    return PlayerAction.PlayCard(biggerCard);
                }

                // If current player wins with this trick => take it
                if (context.FirstPlayedCard.GetValue() + biggerCard.GetValue() + context.SecondPlayerRoundPoints > 65)
                {
                    return PlayerAction.PlayCard(biggerCard);
                }

                // Don't have Queen and King of the same suit => play it
                if (biggerCard.Type != CardType.Queen || !this.Cards.Contains(Card.GetCard(biggerCard.Suit, CardType.King)))
                {
                    if (biggerCard.Type != CardType.King || !this.Cards.Contains(Card.GetCard(biggerCard.Suit, CardType.Queen)))
                    {
                        return PlayerAction.PlayCard(biggerCard);
                    }
                }
            }

            // Smallest card
            var smallestCard = possibleCardsToPlay.OrderBy(x => x.GetValue()).ThenByDescending(x => this.Tracker.UnknownCards.Count(uc => uc.Suit == x.Suit)).FirstOrDefault();

            if (context.FirstPlayedCard.Suit != context.TrumpCard.Suit)
            {
                var biggestTrump =
                    possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
                        .OrderByDescending(x => x.GetValue()).FirstOrDefault();

                if (biggestTrump != null)
                {
                    var currentPlayerPotentialPoints = context.FirstPlayedCard.GetValue() + biggestTrump.GetValue()
                                                       + context.SecondPlayerRoundPoints;

                    var cardFor20Or40 = this.TryToAnnounce20Or40(context, this.Cards);
                    if (cardFor20Or40?.Type == CardType.Queen && cardFor20Or40.Suit != context.TrumpCard.Suit)
                    {
                        currentPlayerPotentialPoints += 20;
                    }

                    // If the current player wins the round by playing trump => play it
                    if (currentPlayerPotentialPoints > 65)
                    {
                        return PlayerAction.PlayCard(biggestTrump);
                    }

                    // If the other player wins the round by taking this hand => trump it
                    if (context.FirstPlayedCard.GetValue() + smallestCard.GetValue() + context.FirstPlayerRoundPoints > 65)
                    {
                        return PlayerAction.PlayCard(biggestTrump);
                    }
                }
            }

            // When opponent plays Ace or Ten => play trump card
            if (context.FirstPlayedCard.Suit != context.TrumpCard.Suit &&
                (context.FirstPlayedCard.Type == CardType.Ace || context.FirstPlayedCard.Type == CardType.Ten))
            {
                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Nine))
                    && context.TrumpCard.Type == CardType.Jack)
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.Nine));
                }

                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Jack)))
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.Jack));
                }

                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Queen))
                    && this.Tracker.PlayedCards.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.King)))
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.Queen));
                }

                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.King))
                    && this.Tracker.PlayedCards.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Queen)))
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.King));
                }

                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Ten)))
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.Ten));
                }

                if (possibleCardsToPlay.Contains(Card.GetCard(context.TrumpCard.Suit, CardType.Ace)))
                {
                    return PlayerAction.PlayCard(Card.GetCard(context.TrumpCard.Suit, CardType.Ace));
                }
            }

            return PlayerAction.PlayCard(smallestCard);
        }
    }
}
