namespace Santase.Logic.Players
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    public class AnnounceValidator : IAnnounceValidator
    {
        public Announce GetPossibleAnnounce(IEnumerable<Card> playerCards, Card cardToBePlayed, Card trumpCard)
        {
            CardType cardTypeToSearch;
            if (cardToBePlayed.Type == CardType.Queen)
            {
                cardTypeToSearch = CardType.King;
            }
            else if (cardToBePlayed.Type == CardType.King)
            {
                cardTypeToSearch = CardType.Queen;
            }
            else
            {
                return Announce.None;
            }

            var cardToSearch = new Card(
                cardToBePlayed.Suit,
                cardTypeToSearch);

            if (!playerCards.Contains(cardToSearch))
            {
                return Announce.None;
            }

            if (cardToBePlayed.Suit == trumpCard.Suit)
            {
                return Announce.Fourty;
            }
            else
            {
                return Announce.Twenty;
            }
        }
    }
}