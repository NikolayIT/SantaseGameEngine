namespace Santase.Logic.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    // TODO: Unit test this class
    internal class RoundPlayerInfo
    {
        public RoundPlayerInfo(IPlayer player)
        {
            this.Player = player;
            this.Cards = new HashSet<Card>();
            this.TrickCards = new List<Card>();
            this.Announces = new List<Announce>();
            this.GameCloser = false;
        }

        public IPlayer Player { get; }

        public ICollection<Card> Cards { get; }

        public ICollection<Card> TrickCards { get; }

        public ICollection<Announce> Announces { get; }

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

        public void AddCard(Card card)
        {
            // We are adding the card in two different places to control what an AI player can play
            this.Cards.Add(card);
            this.Player.AddCard(card);
        }
    }
}
