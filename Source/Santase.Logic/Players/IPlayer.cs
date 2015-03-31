using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic.Players
{
    public interface IPlayer
    {
        void AddCard(Card card);

        PlayerAction GetTurn(
            PlayerTurnContext context,
            IPlayerActionValidater actionValidator);

        void EndTurn(PlayerTurnContext context);
    }
}
