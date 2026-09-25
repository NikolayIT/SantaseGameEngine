namespace Santase.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.UI.Game;

    using Xunit;

    // Sits at a GameSession the way the game page does: listens to every event, plays a random
    // legal move (from the seat's own view) whenever a person is to move, and continues after
    // every round. Records everything in order, with the thread each event came on and the round
    // points before each move.
    internal sealed class TableDriver
    {
        private readonly GameSession session;

        private readonly Random random;

        private (int First, int Second) pointsBeforeMove;

        public TableDriver(GameSession session, int seed)
        {
            this.session = session;
            this.random = new Random(seed);

            session.RoundStarted += () =>
            {
                this.Record("round");
                this.RoundTrumps.Add(session.GetView(PlayerSlot.First)!.TrumpCard.Suit);
            };
            session.TurnStarted += this.OnTurnStarted;
            session.MovePlayed += move =>
            {
                this.Record(move);
                this.Moves.Add((move, this.pointsBeforeMove.First, this.pointsBeforeMove.Second));
            };
            session.TrickCollected += trick => this.Record(trick);
            session.RoundFinished += round =>
            {
                this.Record(round);
                if (this.AutoContinue)
                {
                    session.Continue();
                }
            };
            session.GameOver += (winner, round) =>
            {
                this.Record(round);
                this.Record("game over");
                this.Winner = winner;
            };
            session.GameError += error =>
            {
                this.Record("error");
                this.Errors.Add(error);
            };
        }

        public bool AutoPlay { get; set; } = true;

        public bool AutoContinue { get; set; } = true;

        // Every event, in order: "round", ("turn", slot, isHuman), MoveInfo, TrickInfo,
        // RoundEndInfo, "game over", "error".
        public List<object> Events { get; } = new List<object>();

        public HashSet<int> EventThreads { get; } = new HashSet<int>();

        // The trump suit of each deal, in order (an exchange never changes the suit).
        public List<CardSuit> RoundTrumps { get; } = new List<CardSuit>();

        public List<(MoveInfo Move, int FirstBefore, int SecondBefore)> Moves { get; } = new();

        public IEnumerable<TrickInfo> Tricks => this.Events.OfType<TrickInfo>();

        public IEnumerable<RoundEndInfo> Rounds => this.Events.OfType<RoundEndInfo>();

        public List<Exception> Errors { get; } = new List<Exception>();

        public PlayerSlot? Winner { get; private set; }

        public event Action<PlayerSlot, bool>? Turn;

        public async Task PlayToTheEndAsync()
        {
            this.session.Start();
            await this.session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            Assert.Empty(this.Errors);
            Assert.NotNull(this.Winner);
        }

        // A random legal move for the person in slot, chosen from what their view offers.
        public PlayerAction ChooseMove(PlayerSlot slot)
        {
            var view = this.session.GetView(slot)!;
            if (view.CanChangeTrump && this.random.Next(2) == 0)
            {
                return PlayerAction.ChangeTrump();
            }

            if (view.CanClose && this.random.Next(8) == 0)
            {
                return PlayerAction.CloseGame();
            }

            return PlayerAction.PlayCard(view.PlayableCards[this.random.Next(view.PlayableCards.Count)]);
        }

        private void OnTurnStarted(PlayerSlot slot, bool isHuman)
        {
            this.Record(("turn", slot, isHuman));
            var view = this.session.GetView(slot)!;
            this.pointsBeforeMove = (view.FirstPlayerRoundPoints, view.SecondPlayerRoundPoints);
            this.Turn?.Invoke(slot, isHuman);
            if (isHuman && this.AutoPlay)
            {
                // The session must already be waiting for this person when it says it is their turn.
                Assert.True(this.session.IsAwaitingMove(slot), $"{slot}'s turn started, but no move is awaited");
                Assert.True(this.session.TryPlay(slot, this.ChooseMove(slot)));
            }
        }

        private void Record(object item)
        {
            this.Events.Add(item);
            this.EventThreads.Add(Environment.CurrentManagedThreadId);
        }
    }
}
