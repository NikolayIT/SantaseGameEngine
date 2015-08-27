namespace Santase.Logic.Round
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class RoundPlayerInfo
    {
        public RoundPlayerInfo(IPlayer player)
        {
            this.Player = player;
            this.Cards = new List<Card>();
            this.TrickCards = new List<Card>();
            this.Announces = new List<Announce>();
            this.GameCloser = false;
        }

        public IPlayer Player { get; }

        public IList<Card> Cards { get; }

        public IList<Card> TrickCards { get; }

        public IList<Announce> Announces { get; }

        public bool GameCloser { get; set; }

        public int RoundPoints
        {
            get
            {
                var points = 0;
                points += this.TrickCards.Sum(card => card.GetValue());
                points += this.Announces.Sum(announce => (int)announce);
                return points;
            }
        }

        public bool HasAtLeastOneTrick => this.TrickCards.Count > 0;
    }
}
