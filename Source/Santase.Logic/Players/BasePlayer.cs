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

        public virtual void StartGame()
        {
        }

        public virtual void StartRound(IEnumerable<Card> playerCards, Card trumpCard)
        {
            this.Cards.Clear();
            foreach (var playerCard in playerCards)
            {
                this.Cards.Add(playerCard);
            }
        }

        public virtual void StartTurn(Card newCard)
        {
            this.Cards.Add(newCard);
        }

        public abstract PlayerAction GetTurn(PlayerTurnContext context);

        public virtual void EndTurn(PlayerTurnContext context)
        {
        }

        public virtual void EndRound()
        {
        }

        public virtual void EndGame(bool amIWinner)
        {
        }

        protected PlayerAction ChangeTrump(Card trumpCard)
        {
            this.Cards.Remove(new Card(trumpCard.Suit, CardType.Nine));
            this.Cards.Add(trumpCard);
            return PlayerAction.ChangeTrump();
        }

        protected PlayerAction PlayCard(Card card)
        {
            this.Cards.Remove(card);
            return PlayerAction.PlayCard(card);
        }
    }
}
