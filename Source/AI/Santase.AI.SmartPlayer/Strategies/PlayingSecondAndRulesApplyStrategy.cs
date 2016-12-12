namespace Santase.AI.SmartPlayer.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class PlayingSecondAndRulesApplyStrategy : BaseChooseCardStrategy
    {
        public PlayingSecondAndRulesApplyStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // If bigger card is available => play it
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();

            if (biggerCard != null)
            {
                var typeToTry = this.GetNextBiggerCardType(biggerCard.Type);
                while (possibleCardsToPlay.Any(x => x.Suit == biggerCard.Suit && x.Type == typeToTry)
                           || this.Tracker.PlayedCards.Any(x => x.Suit == biggerCard.Suit && x.Type == typeToTry))
                {
                    if (possibleCardsToPlay.Any(x => x.Suit == biggerCard.Suit && x.Type == typeToTry))
                    {
                        biggerCard = Card.GetCard(biggerCard.Suit, typeToTry);
                    }

                    if (typeToTry == CardType.Ace)
                    {
                        break;
                    }

                    typeToTry = this.GetNextBiggerCardType(typeToTry);
                }

                return PlayerAction.PlayCard(biggerCard);
            }

            // Play smallest trump card?
            var smallestTrumpCard =
                possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (smallestTrumpCard != null)
            {
                return PlayerAction.PlayCard(smallestTrumpCard);
            }

            // Smallest card
            var cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return PlayerAction.PlayCard(cardToPlay);
        }

        private CardType GetNextBiggerCardType(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Nine:
                    return CardType.Jack;
                case CardType.Ten:
                    return CardType.Ace;
                case CardType.Jack:
                    return CardType.Queen;
                case CardType.Queen:
                    return CardType.King;
                case CardType.King:
                    return CardType.Ten;
                case CardType.Ace:
                    return CardType.Ace;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null);
            }
        }
    }
}
