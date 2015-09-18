namespace Santase.AI.SmartPlayer.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    public class OpponentSuitCardsProvider
    {
        public ICollection<Card> GetOpponentCards(ICollection<Card> myCards, ICollection<Card> playedCards, CardSuit suit)
        {
            var playerCards = new CardCollection
                                       {
                                           new Card(suit, CardType.Nine),
                                           new Card(suit, CardType.Jack),
                                           new Card(suit, CardType.Queen),
                                           new Card(suit, CardType.King),
                                           new Card(suit, CardType.Ten),
                                           new Card(suit, CardType.Ace),
                                       };
            foreach (var card in myCards.Where(x => x.Suit == suit))
            {
                playerCards.Remove(card);
            }

            foreach (var card in playedCards.Where(x => x.Suit == suit))
            {
                playerCards.Remove(card);
            }

            return playerCards;
        }
    }
}
