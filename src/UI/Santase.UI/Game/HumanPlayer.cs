namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class HumanPlayer : BasePlayer
    {
        private TaskCompletionSource<PlayerAction>? pendingTurn;

        public HumanPlayer(string name)
        {
            this.Name = name;
        }

        public override string Name { get; }

        public IReadOnlyList<Card> CardsSnapshot => this.Cards.ToList();

        public IPlayerActionValidator ActionValidator => this.PlayerActionValidator;

        public PlayerTurnContext? CurrentContext { get; private set; }

        public bool IsAwaitingInput => Volatile.Read(ref this.pendingTurn) != null;

        public event Action<HumanPlayer, PlayerTurnContext>? TurnRequested;

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.CurrentContext = context;
            var tcs = new TaskCompletionSource<PlayerAction>(TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref this.pendingTurn, tcs);

            this.TurnRequested?.Invoke(this, context);

            var action = tcs.Task.GetAwaiter().GetResult();

            // Mirror BasePlayer's PlayCard / ChangeTrump helpers - keep our hand in sync
            // before returning the action to the engine.
            switch (action.Type)
            {
                case PlayerActionType.PlayCard:
                    this.Cards.Remove(action.Card);
                    break;
                case PlayerActionType.ChangeTrump:
                    var nineOfTrump = Card.GetCard(context.TrumpCard.Suit, CardType.Nine);
                    this.Cards.Remove(nineOfTrump);
                    this.Cards.Add(context.TrumpCard);
                    break;
            }

            return action;
        }

        public bool TrySubmit(PlayerAction action)
        {
            var tcs = Interlocked.Exchange(ref this.pendingTurn, null);
            return tcs != null && tcs.TrySetResult(action);
        }

        public bool CancelPendingTurn()
        {
            var tcs = Interlocked.Exchange(ref this.pendingTurn, null);
            return tcs != null && tcs.TrySetCanceled();
        }
    }
}
