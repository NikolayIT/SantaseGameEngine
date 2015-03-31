using Santase.Logic.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic
{
    public class PlayerActionValidater : IPlayerActionValidater
    {
        public bool IsValid(PlayerAction action, Players.PlayerTurnContext context)
        {
            // TODO: Implement
            return false;
        }
    }
}
