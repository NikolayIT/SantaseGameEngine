namespace Santase.AI.SmartPlayer.Strategies
{
    using System;
    using System.Collections.Generic;

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
            // If bigger card is available => play it (the smallest one that is bigger)
            Card biggerCard = null;
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Suit == context.FirstPlayedCard.Suit && card.GetValue() > context.FirstPlayedCard.GetValue()
                    && (biggerCard == null || card.GetValue() < biggerCard.GetValue()))
                {
                    biggerCard = card;
                }
            }

            if (biggerCard != null)
            {
                var typeToTry = this.GetNextBiggerCardType(biggerCard.Type);
                while (possibleCardsToPlay.Contains(Card.GetCard(biggerCard.Suit, typeToTry))
                           || this.Tracker.PlayedCards.Contains(Card.GetCard(biggerCard.Suit, typeToTry)))
                {
                    if (possibleCardsToPlay.Contains(Card.GetCard(biggerCard.Suit, typeToTry)))
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
            var smallestTrumpCard = LowestOfSuit(possibleCardsToPlay, context.TrumpCard.Suit);
            if (smallestTrumpCard != null)
            {
                return PlayerAction.PlayCard(smallestTrumpCard);
            }

            // Smallest card
            var cardToPlay = Lowest(possibleCardsToPlay);
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
