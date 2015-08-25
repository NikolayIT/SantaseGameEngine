namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class ChangeTrumpActionValidator
    {
        public bool CanChangeTrump(PlayerTurnContext context, IList<Card> playerCards)
        {
            if (!context.State.CanChangeTrump || !context.AmITheFirstPlayer)
            {
                return false;
            }

            if (!playerCards.Contains(new Card(context.TrumpCard.Suit, CardType.Nine)))
            {
                return false;
            }

            return true;
        }
    }
}