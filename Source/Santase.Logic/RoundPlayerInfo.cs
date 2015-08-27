namespace Santase.Logic
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class RoundPlayerInfo
    {
        public RoundPlayerInfo(IPlayer player, IList<Card> cards)
        {
            this.Player = player;
            this.Cards = cards;
            this.Announces = new List<Announce>();
            this.GameCloser = false;
            this.RoundPoints = 0;
        }

        public IPlayer Player { get; }

        public IList<Card> Cards { get; }

        public IList<Announce> Announces { get; }

        public bool GameCloser { get; set; }

        public int RoundPoints { get; set; }
    }
}
