namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.RoundStates;

    public class ChangeTrumpActionValidator
    {
        public bool CanChangeTrump(bool isThePlayerFirst, BaseRoundState state, Card trumpCard, IList<Card> playerCards)
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