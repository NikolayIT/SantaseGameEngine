namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    public abstract class BasePlayer : IPlayer
    {
        protected BasePlayer()
        {
            this.Cards = new List<Card>();
            this.AnnounceValidator = new AnnounceValidator();
            this.PlayerActionValidator = new PlayerActionValidator();
        }

        public abstract string Name { get; }

        protected IList<Card> Cards { get; }

        protected IAnnounceValidator AnnounceValidator { get; }

        protected IPlayerActionValidator PlayerActionValidator { get; }

        public virtual void AddCard(Card card)
        {
            this.Cards.Add(card);
        }

        public abstract PlayerAction GetTurn(PlayerTurnContext context);

        public abstract void EndTurn(PlayerTurnContext context);

        public virtual void EndRound()
        {
            this.Cards.Clear();
        }
    }
}
