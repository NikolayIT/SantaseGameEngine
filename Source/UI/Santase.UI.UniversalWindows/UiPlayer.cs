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

        public event EventHandler<Card> PlayerPlayedCardChanged;

        public event EventHandler<Card> OtherPlayerPlayedCardChanged;

        public override string Name => "UI";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            if (context.FirstPlayedCard != null)
            {
                this.OtherPlayerPlayedCardChanged?.Invoke(this, context.FirstPlayedCard);
            }

            while (this.played == false)
            {
            }

            this.played = false;
            var action = PlayerAction.PlayCard(this.playedCard);

            if (!this.PlayerActionValidator.IsValid(action, context, this.Cards))
            {
                // TODO: Consider converting this to while loop
                return this.GetTurn(context);
            }

            action = this.PlayCard(this.playedCard);
            this.PlayerPlayedCardChanged?.Invoke(this, this.playedCard);
            this.RedrawCards?.Invoke(this, this.Cards);
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

        public override void EndTurn(PlayerTurnContext context)
        {
            if (context.FirstPlayedCard == this.playedCard)
            {
                this.OtherPlayerPlayedCardChanged?.Invoke(this, context.SecondPlayedCard);
            }
            else
            {
                this.OtherPlayerPlayedCardChanged?.Invoke(this, context.FirstPlayedCard);
            }

            this.CardsLeftChanged?.Invoke(this, context.CardsLeftInDeck);
        }

        public override void EndRound()
        {
            this.CardsLeftChanged?.Invoke(this, 12);
        }
    }
}
