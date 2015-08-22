using System.Collections.Generic;

using Santase.Logic.Players;
using Santase.Logic.Cards;
namespace Santase.Logic
{
    public interface IPlayerActionValidater
    {
        bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards);
    }
}
