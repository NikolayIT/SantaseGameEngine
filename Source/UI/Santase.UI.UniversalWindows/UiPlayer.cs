namespace Santase.UI.UniversalWindows
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class UiPlayer : BasePlayer
    {
        private bool played = false;

        private Card playedCard = null;

        public event EventHandler<IEnumerable<Card>> RedrawCards;

        public event EventHandler<int> CardsLeftChanged;

        public override string Name => "UI";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            while (this.played == false)
            {
                Task.Delay(100).Wait();
            }

            this.played = false;
            var action = this.PlayCard(this.playedCard);
            this.RedrawCards?.Invoke(this, this.Cards);
            this.CardsLeftChanged?.Invoke(this, context.CardsLeftInDeck);
            return action;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.RedrawCards?.Invoke(this, this.Cards);
        }

        public void Action(Card card)
        {
            this.played = true;
            this.playedCard = card;
        }
    }
}
