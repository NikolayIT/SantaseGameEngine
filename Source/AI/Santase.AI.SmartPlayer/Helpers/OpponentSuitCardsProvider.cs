namespace Santase.AI.SmartPlayer.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    // TODO: Unit test this class
    public class OpponentSuitCardsProvider
    {
        public ICollection<Card> GetOpponentCards(ICollection<Card> myCards, ICollection<Card> playedCards, Card activeTrumpCard, CardSuit suit)
        {
            var playerCards = new CardCollection
                                  {
                                      Card.GetCard(suit, CardType.Nine),
                                      Card.GetCard(suit, CardType.Jack),
                                      Card.GetCard(suit, CardType.Queen),
                                      Card.GetCard(suit, CardType.King),
                                      Card.GetCard(suit, CardType.Ten),
                                      Card.GetCard(suit, CardType.Ace),
                                  };

            foreach (var card in myCards.Where(x => x.Suit == suit))
            {
                playerCards.Remove(card);
            }

            foreach (var card in playedCards.Where(x => x.Suit == suit))
            {
                playerCards.Remove(card);
            }

            if (activeTrumpCard != null)
            {
                playerCards.Remove(activeTrumpCard);
            }

            return playerCards;
        }
    }
}
