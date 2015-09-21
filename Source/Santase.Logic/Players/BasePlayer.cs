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

        public virtual void StartGame(string otherPlayerIdentifier)
        {
        }

        public virtual void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.Cards.Clear();
            foreach (var card in cards)
            {
                this.Cards.Add(card);
            }
        }

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

        protected PlayerAction CloseGame()
        {
            return PlayerAction.CloseGame();
        }
    }
}
