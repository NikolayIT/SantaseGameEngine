namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public interface IAnnounceValidator
    {
        Announce GetPossibleAnnounce(IEnumerable<Card> playerCards, Card cardToBePlayed, Card trumpCard);
    }
}
