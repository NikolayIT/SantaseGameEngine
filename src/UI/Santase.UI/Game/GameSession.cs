namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.AI.ClaudePlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// One game of Santase on the device: a <see cref="SantaseMatch"/> played by a single async
    /// flow. Nothing waits on a thread. A person's turn is an awaited tap
    /// (<see cref="TryPlay"/>), the computer's turn an awaited thinking pause and move, a finished
    /// trick an awaited table pause, and a finished round an awaited <see cref="Continue"/>.
    /// <para>
    /// Start it on the UI thread: the flow then resumes there after every await, so every event
    /// is raised on the UI thread, in play order, and handlers can update the screen directly.
    /// Only the computer's move is computed on the thread pool (the search player thinks for
    /// ~100 ms), from a snapshot view of its seat.
    /// </para>
    /// <para>No MAUI types here: the UI tests compile this file and play whole games with it.</para>
    /// </summary>
    public sealed class GameSession
    {
        // The computer (vs-AI games): it plays the second seat and is restored from its view
        // before every move, so the session keeps no callbacks wired to it.
        private readonly IRestorablePlayer? computer;

        // Answers the human's "what would you play?" from the human's own view (vs-AI games).
        private readonly ClaudePlayer? hintPlayer;

        private readonly Func<int, int>? shuffle;

        private SantaseMatch? match;

        private CancellationTokenSource? stopping;

        private Task running = Task.CompletedTask;

        // Each Start begins a new run; a stopped run that wakes up late leaves the new one alone.
        private int runId;

        // Counts Start calls (runId also moves on Stop).
        private int starts;

        private TaskCompletionSource<PlayerAction>? pendingMove;

        private PlayerSlot pendingMoveSlot;

        private TaskCompletionSource? pendingContinue;

        /// <param name="mode">Against the computer (it plays the second seat) or two people on one device.</param>
        /// <param name="firstPlayerName">The first seat's name.</param>
        /// <param name="secondPlayerName">The second seat's name.</param>
        /// <param name="computer">The computer player for <see cref="GameMode.VsAi"/>; null otherwise.</param>
        /// <param name="pace">The thinking and table pauses.</param>
        /// <param name="shuffle">The random source for the deals; null shuffles with <see cref="Random.Shared"/>.</param>
        public GameSession(GameMode mode, string firstPlayerName, string secondPlayerName, IRestorablePlayer? computer, GamePace pace, Func<int, int>? shuffle = null)
        {
            if (mode == GameMode.VsAi && computer == null)
            {
                throw new ArgumentNullException(nameof(computer), "A game against the computer needs a computer player.");
            }

            this.Mode = mode;
            this.FirstPlayerName = firstPlayerName;
            this.SecondPlayerName = secondPlayerName;
            this.Pace = pace;
            this.shuffle = shuffle;
            if (mode == GameMode.VsAi)
            {
                this.computer = computer;
                this.hintPlayer = new ClaudePlayer();
            }
        }

        /// <summary>Fires when a round has been dealt (read the seats' views for the cards).</summary>
        public event Action? RoundStarted;

        /// <summary>Fires when a player is to move; the flag says whether it is a person.</summary>
        public event Action<PlayerSlot, bool>? TurnStarted;

        /// <summary>Fires after every move: a card played, a trump exchange or a close.</summary>
        public event Action<MoveInfo>? MovePlayed;

        /// <summary>Fires when a finished trick leaves the table, after the table pause.</summary>
        public event Action<TrickInfo>? TrickCollected;

        /// <summary>Fires after a round that did not end the game; the game waits for <see cref="Continue"/>.</summary>
        public event Action<RoundEndInfo>? RoundFinished;

        /// <summary>Fires once when the game is won, with the last round's result.</summary>
        public event Action<PlayerSlot, RoundEndInfo>? GameOver;

        /// <summary>Fires if the game stops on an unexpected error.</summary>
        public event Action<Exception>? GameError;

        public GameMode Mode { get; }

        public string FirstPlayerName { get; }

        public string SecondPlayerName { get; }

        public GamePace Pace { get; }

        /// <summary>Gets a value indicating whether a game is in progress (started, not over, not stopped).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Gets the game in progress, for callers that want to await its end.</summary>
        public Task Completion => this.running;

        /// <summary>Gets the game points needed to win (11 under standard Santase rules).</summary>
        public int GamePointsTarget => GameRulesProvider.Santase.GamePointsNeededForWin;

        /// <summary>Gets a value indicating whether <see cref="GetHint"/> can answer (vs-AI games).</summary>
        public bool SupportsHints => this.hintPlayer != null;

        public static PlayerPosition Position(PlayerSlot slot) =>
            slot == PlayerSlot.First ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer;

        public static PlayerSlot Slot(PlayerPosition position) =>
            position == PlayerPosition.SecondPlayer ? PlayerSlot.Second : PlayerSlot.First;

        public static PlayerSlot Other(PlayerSlot slot) =>
            slot == PlayerSlot.First ? PlayerSlot.Second : PlayerSlot.First;

        public bool IsHumanSlot(PlayerSlot slot) => slot == PlayerSlot.First || this.Mode == GameMode.HotSeat;

        public string GetName(PlayerSlot slot) => slot == PlayerSlot.First ? this.FirstPlayerName : this.SecondPlayerName;

        /// <summary>What <paramref name="slot"/> may see now, or null before the first deal.</summary>
        public SantaseSeatView? GetView(PlayerSlot slot) => this.match?.GetView(Position(slot));

        /// <summary>Whether the game is waiting for a move from the person in <paramref name="slot"/>.</summary>
        public bool IsAwaitingMove(PlayerSlot slot) => this.pendingMove != null && this.pendingMoveSlot == slot;

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            var game = new SantaseMatch(new SantaseMatchOptions { FirstToPlay = PlayerPosition.FirstPlayer, Shuffle = this.shuffle });

            // No views until the new game is dealt (after a restart, the stopped game may still
            // be finishing the computer's move).
            this.match = null;
            this.stopping = new CancellationTokenSource();
            this.IsRunning = true;
            var start = ++this.starts;
            var run = this.RunAsync(this.running, game, ++this.runId, this.stopping.Token);

            // The run raises the deal and the first turn before it returns here. If a handler of
            // those restarted the game, the new game's run is already in place: keep it. (The run
            // started here stops at its next check without touching the computer, so the new one
            // does not need to wait for it.)
            if (start == this.starts)
            {
                this.running = run;
            }
        }

        /// <summary>Stops the game in progress (leaving the table). Nothing more is raised for it.</summary>
        public void Stop()
        {
            this.runId++;
            this.stopping?.Cancel();
            this.stopping = null;
            this.pendingMove = null;
            this.pendingContinue = null;
            this.IsRunning = false;
        }

        public void Restart()
        {
            this.Stop();
            this.Start();
        }

        /// <summary>
        /// The person in <paramref name="slot"/> makes a move. Returns false, changing nothing, when
        /// it is not their turn or the move is not allowed.
        /// </summary>
        public bool TryPlay(PlayerSlot slot, PlayerAction action)
        {
            var move = this.pendingMove;
            if (move == null || this.pendingMoveSlot != slot || this.match == null)
            {
                return false;
            }

            if (this.match.Validate(Position(slot), action) != SantaseActResult.Ok)
            {
                return false;
            }

            this.pendingMove = null;
            return move.TrySetResult(action);
        }

        /// <summary>Deals the next round after <see cref="RoundFinished"/>.</summary>
        public void Continue()
        {
            var next = this.pendingContinue;
            this.pendingContinue = null;
            next?.TrySetResult();
        }

        /// <summary>The move the hint player would make for the person to move, or null when none is asked.</summary>
        public PlayerAction? GetHint()
        {
            if (this.hintPlayer == null || this.match == null || this.pendingMove == null)
            {
                return null;
            }

            return this.hintPlayer.ChooseMove(this.match.GetView(Position(this.pendingMoveSlot)));
        }

        private static RoundEndInfo EndOfRound(SantaseMatch game, List<Announce> firstAnnounces, List<Announce> secondAnnounces)
        {
            var summary = game.GetView(PlayerPosition.FirstPlayer).PreviousRounds[^1];
            PlayerSlot? winner = summary.Winner == PlayerPosition.NoOne ? null : Slot(summary.Winner);
            return new RoundEndInfo(
                summary.FirstPlayerRoundPoints,
                summary.SecondPlayerRoundPoints,
                game.FirstPlayerTotalPoints,
                game.SecondPlayerTotalPoints,
                winner == PlayerSlot.First ? summary.GamePoints : 0,
                winner == PlayerSlot.Second ? summary.GamePoints : 0,
                winner,
                firstAnnounces.ToArray(),
                secondAnnounces.ToArray(),
                game.IsFinished);
        }

        private async Task RunAsync(Task previous, SantaseMatch game, int id, CancellationToken stop)
        {
            var firstAnnounces = new List<Announce>();
            var secondAnnounces = new List<Announce>();
            try
            {
                // A stopped game ends at its next await, but the computer may be mid-move on the
                // thread pool; the computer player is one object, so let that move finish first.
                // (Already complete in the usual case, so the first deal is raised synchronously.)
                await previous;
                stop.ThrowIfCancellationRequested();

                this.match = game;
                game.Start();
                this.RoundStarted?.Invoke();
                while (true)
                {
                    // A handler of the last event may have stopped the game (or restarted it). The
                    // same check follows every await: a wait that ended just before a stop has
                    // already queued its continuation, which must not raise anything.
                    stop.ThrowIfCancellationRequested();

                    var slot = Slot(game.ToMove);
                    PlayerAction action;
                    if (this.IsHumanSlot(slot))
                    {
                        // Waiting for the tap already when TurnStarted fires, so its handlers can
                        // ask for a hint or play at once.
                        var tap = this.ExpectMove(slot, stop);
                        this.TurnStarted?.Invoke(slot, true);
                        action = await tap;
                    }
                    else
                    {
                        this.TurnStarted?.Invoke(slot, false);
                        action = await this.ThinkAsync(game, slot, stop);
                    }

                    stop.ThrowIfCancellationRequested();

                    var (move, trick) = this.Apply(game, slot, action);
                    if (move.Announce != Announce.None)
                    {
                        (slot == PlayerSlot.First ? firstAnnounces : secondAnnounces).Add(move.Announce);
                    }

                    this.MovePlayed?.Invoke(move);
                    if (trick == null)
                    {
                        continue;
                    }

                    await Task.Delay(this.Pace.TrickSettleMs, stop);
                    stop.ThrowIfCancellationRequested();
                    this.TrickCollected?.Invoke(trick);
                    stop.ThrowIfCancellationRequested();
                    if (!trick.RoundOver)
                    {
                        continue;
                    }

                    var round = EndOfRound(game, firstAnnounces, secondAnnounces);
                    firstAnnounces.Clear();
                    secondAnnounces.Clear();
                    if (game.IsFinished)
                    {
                        this.IsRunning = false;
                        this.GameOver?.Invoke(Slot(game.Winner), round);
                        return;
                    }

                    var next = this.ExpectContinue(stop);
                    this.RoundFinished?.Invoke(round);
                    await next;
                    stop.ThrowIfCancellationRequested();
                    this.RoundStarted?.Invoke();
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // Stopped: leaving the table is not an error.
            }
            catch (Exception ex)
            {
                // A stopped run has nobody left to tell.
                if (id == this.runId)
                {
                    this.IsRunning = false;
                    this.GameError?.Invoke(ex);
                }
            }
        }

        // The move TryPlay will deliver for slot (cancelled when the game is stopped).
        private Task<PlayerAction> ExpectMove(PlayerSlot slot, CancellationToken stop)
        {
            var move = new TaskCompletionSource<PlayerAction>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pendingMoveSlot = slot;
            this.pendingMove = move;
            return move.Task.WaitAsync(stop);
        }

        // The Continue after a finished round (cancelled when the game is stopped).
        private Task ExpectContinue(CancellationToken stop)
        {
            var next = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pendingContinue = next;
            return next.Task.WaitAsync(stop);
        }

        private async Task<PlayerAction> ThinkAsync(SantaseMatch game, PlayerSlot slot, CancellationToken stop)
        {
            await Task.Delay(this.Pace.ThinkDelayMs, stop);
            var view = game.GetView(Position(slot));
            var player = this.computer!;
            return await Task.Run(() => player.ChooseMove(view), stop);
        }

        // Makes the move and reads what it did from the match: the announce it made, the round
        // points after it and, if it finished a trick, that trick (the last trick of the view;
        // when the trick ended the round, the view is already the next deal's or the final one).
        private (MoveInfo Move, TrickInfo? Trick) Apply(SantaseMatch game, PlayerSlot slot, PlayerAction action)
        {
            var seat = Position(slot);
            var tricksBefore = game.GetView(seat).Tricks.Count;
            var roundsBefore = game.RoundsPlayed;
            var result = game.Act(seat, action);
            if (result != SantaseActResult.Ok)
            {
                throw new InvalidOperationException($"{this.GetName(slot)}'s move ({action}) was refused: {result}.");
            }

            var view = game.GetView(seat);
            var roundOver = game.RoundsPlayed > roundsBefore;
            var finished = roundOver || view.Tricks.Count > tricksBefore ? view.LastTrick : null;

            var announce = Announce.None;
            if (action.Type == PlayerActionType.PlayCard)
            {
                if (finished == null && view.CurrentTrickLeader == seat)
                {
                    announce = view.CurrentTrickAnnounce;
                }
                else if (finished != null && finished.Leader == seat && finished.FollowCard == null)
                {
                    announce = finished.Announce;
                }
            }

            var firstRoundPoints = view.FirstPlayerRoundPoints;
            var secondRoundPoints = view.SecondPlayerRoundPoints;
            if (roundOver)
            {
                var summary = view.PreviousRounds[^1];
                firstRoundPoints = summary.FirstPlayerRoundPoints;
                secondRoundPoints = summary.SecondPlayerRoundPoints;
            }

            var move = new MoveInfo(slot, action, announce, firstRoundPoints, secondRoundPoints);
            var trick = finished == null
                ? null
                : new TrickInfo(Slot(finished.Leader), finished.LeadCard, finished.FollowCard, Slot(finished.Winner), roundOver);
            return (move, trick);
        }
    }
}
