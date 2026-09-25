namespace Santase.Logic.Players
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;

    public abstract class BasePlayer : IPlayer
    {
        protected BasePlayer()
        {
            this.Cards = new CardCollection();
            this.AnnounceValidator = PlayerActionValidate.AnnounceValidator.Instance;
            this.PlayerActionValidator = PlayerActionValidate.PlayerActionValidator.Instance;
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
            this.Cards.Remove(Card.GetCard(trumpCard.Suit, CardType.Nine));
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

        /// <summary>
        /// Replaces the hand with the one in <paramref name="view"/>. For <see cref="IRestorablePlayer"/>
        /// implementations; the rest of a player's memory is theirs to rebuild.
        /// </summary>
        /// <param name="view">The seat view to take the hand from.</param>
        protected void RestoreHand(SantaseSeatView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (view.Seat != PlayerPosition.FirstPlayer && view.Seat != PlayerPosition.SecondPlayer)
            {
                throw new ArgumentException("A player is restored from the view of its own seat.", nameof(view));
            }

            this.Cards.Clear();
            foreach (var card in view.Hand)
            {
                this.Cards.Add(card);
            }
        }
    }
}
