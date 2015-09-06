namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public interface IAnnounceValidator
    {
        Announce GetPossibleAnnounce(
            IEnumerable<Card> playerCards,
            Card cardToBePlayed,
            Card trumpCard,
            bool amITheFirstPlayer = true);
    }
}
