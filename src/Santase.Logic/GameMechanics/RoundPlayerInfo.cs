namespace Santase.Logic.GameMechanics
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    internal class RoundPlayerInfo
    {
        // Lazily created: announces are rare (only when a player declares 20/40), so most
        // rounds never allocate this list.
        private List<Announce> announces;

        public RoundPlayerInfo(IPlayer player)
        {
            this.Player = player;
            this.Cards = new CardCollection();
            this.TrickCards = new CardCollection();
            this.GameCloser = false;
        }

        public IPlayer Player { get; }

        public CardCollection Cards { get; }

        public CardCollection TrickCards { get; }

        public IList<Announce> Announces => this.announces ??= new List<Announce>();

        public bool GameCloser { get; set; }

        // Maintained incrementally as cards/announces are added (see WinCard/AddAnnounce)
        // instead of rescanning TrickCards + Announces on every read. RoundPoints is read
        // several times per trick (PlayerTurnContext build, Trick.Play, Round.IsFinished).
        public int RoundPoints { get; private set; }

        public bool HasAtLeastOneTrick => this.TrickCards.Count > 0;

        public void AddCard(Card card)
        {
            // We are adding the card in two different places to control what an AI player can play
            this.Cards.Add(card);
            this.Player.AddCard(card);
        }

        public void WinCard(Card card)
        {
            this.TrickCards.Add(card);
            this.RoundPoints += card.GetValue();
        }

        public void AddAnnounce(Announce announce)
        {
            this.Announces.Add(announce);
            this.RoundPoints += (int)announce;
        }
    }
}
