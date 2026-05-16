namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// Decorates any IPlayer to expose lifecycle events for the UI and to insert
    /// "thinking time" before AI players answer GetTurn so the human can follow play.
    /// </summary>
    public class PlayerObserver : PlayerDecorator
    {
        public PlayerObserver(IPlayer inner)
            : base(inner)
        {
        }

        public int ThinkDelayMs { get; set; }

        public int CardsCount { get; private set; }

        public event Action<ICollection<Card>, Card, int, int>? RoundStarted;

        public event Action<Card>? CardAdded;

        public event Action<PlayerTurnContext>? TurnAboutToStart;

        public event Action<PlayerAction>? TurnCompleted;

        public event Action<PlayerTurnContext>? TurnEnded;

        public event Action? RoundEnded;

        public event Action<bool>? GameEnded;

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.CardsCount = cards.Count;
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            this.RoundStarted?.Invoke(cards, trumpCard, myTotalPoints, opponentTotalPoints);
        }

        public override void AddCard(Card card)
        {
            this.CardsCount++;
            base.AddCard(card);
            this.CardAdded?.Invoke(card);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.TurnAboutToStart?.Invoke(context);

            if (this.ThinkDelayMs > 0 && this.Player is not HumanPlayer)
            {
                Thread.Sleep(this.ThinkDelayMs);
            }

            var action = base.GetTurn(context);

            if (action.Type == PlayerActionType.PlayCard)
            {
                this.CardsCount--;
            }

            this.TurnCompleted?.Invoke(action);
            return action;
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            base.EndTurn(context);
            this.TurnEnded?.Invoke(context);
        }

        public override void EndRound()
        {
            base.EndRound();
            this.RoundEnded?.Invoke();
        }

        public override void EndGame(bool amIWinner)
        {
            base.EndGame(amIWinner);
            this.GameEnded?.Invoke(amIWinner);
        }
    }
}
