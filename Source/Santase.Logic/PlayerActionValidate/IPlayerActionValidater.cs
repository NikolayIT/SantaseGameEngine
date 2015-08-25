namespace Santase.Logic.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public interface IPlayerActionValidater
    {
        bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards);
    }
}
