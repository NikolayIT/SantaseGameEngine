namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    public abstract class BasePlayer : IPlayer
    {
        protected BasePlayer()
        {
            this.Cards = new CardCollection();
            this.AnnounceValidator = new AnnounceValidator();
            this.PlayerActionValidator = new PlayerActionValidator();
        }

        public abstract string Name { get; }

        protected ICollection<Card> Cards { get; }

        protected IAnnounceValidator AnnounceValidator { get; }

        protected IPlayerActionValidator PlayerActionValidator { get; }

        public virtual void AddCard(Card card)
        {
            this.Cards.Add(card);
        }

        public abstract PlayerAction GetTurn(PlayerTurnContext context);

        public virtual void EndTurn(PlayerTurnContext context)
        {
        }

        public virtual void EndRound()
        {
            this.Cards.Clear();
        }

        public virtual void EndGame(bool amIWinner)
        {
        }

        protected PlayerAction ChangeTrump(CardSuit trumpCardSuit)
        {
            this.Cards.Remove(new Card(trumpCardSuit, CardType.Nine));
            return PlayerAction.ChangeTrump();
        }

        protected PlayerAction PlayCard(Card card)
        {
            this.Cards.Remove(card);
            return PlayerAction.PlayCard(card);
        }
    }
}
