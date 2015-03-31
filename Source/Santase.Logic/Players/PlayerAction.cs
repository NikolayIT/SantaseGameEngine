using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic.Players
{
    public class PlayerAction
    {
        public PlayerAction(
            PlayerActionType type,
            Card card,
            Announce announce)
        {
            this.Type = type;
            this.Card = card;
            this.Announce = announce;
        }

        public PlayerActionType Type { get; private set; }

        public Card Card { get; private set; }

        public Announce Announce { get; internal set; }
    }
}
