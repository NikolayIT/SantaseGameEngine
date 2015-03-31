using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santase.Logic.Players;
using Santase.Logic.Cards;
namespace Santase.Logic
{
    public interface IPlayerActionValidater
    {
        bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards);
    }
}
