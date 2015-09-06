namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.RoundStates;

    internal static class ChangeTrumpActionValidator
    {
        public static bool CanChangeTrump(bool isThePlayerFirst, BaseRoundState state, Card trumpCard, IList<Card> playerCards)
        {
            if (!isThePlayerFirst)
            {
                return false;
            }

            if (!state.CanChangeTrump)
            {
                return false;
            }

            return playerCards.Contains(new Card(trumpCard.Suit, CardType.Nine));
        }
    }
}