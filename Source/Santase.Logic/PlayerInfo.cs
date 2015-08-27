namespace Santase.Logic
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayerInfo
    {
        public PlayerInfo(IPlayer player, IList<Card> cards)
        {
            this.Player = player;
            this.Cards = cards;
        }

        public IPlayer Player { get; }

        public IList<Card> Cards { get; }
    }
}
