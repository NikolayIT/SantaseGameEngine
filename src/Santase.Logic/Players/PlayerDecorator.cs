namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public abstract class PlayerDecorator : IPlayer
    {
        protected PlayerDecorator(IPlayer player)
        {
            this.Player = player;
        }

        public virtual string Name => this.Player.Name;

        protected IPlayer Player { get; }

        public virtual void StartGame(string otherPlayerIdentifier)
        {
            this.Player.StartGame(otherPlayerIdentifier);
        }

        public virtual void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.Player.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
        }

        public virtual void AddCard(Card card)
        {
            this.Player.AddCard(card);
        }

        public virtual PlayerAction GetTurn(PlayerTurnContext context)
        {
            return this.Player.GetTurn(context);
        }

        public virtual void EndTurn(PlayerTurnContext context)
        {
            this.Player.EndTurn(context);
        }

        public virtual void EndRound()
        {
            this.Player.EndRound();
        }

        public virtual void EndGame(bool amIWinner)
        {
            this.Player.EndGame(amIWinner);
        }
    }
}
