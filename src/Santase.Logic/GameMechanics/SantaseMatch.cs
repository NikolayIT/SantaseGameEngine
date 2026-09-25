namespace Santase.Logic.GameMechanics
{
    using System;

    using Santase.Logic.Logger;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

    /// <summary>
    /// A whole Santase match (rounds until a player reaches
    /// <see cref="IGameRules.GamePointsNeededForWin"/>) that is driven from outside, one action at
    /// a time. Nothing inside ever waits for a player: after <see cref="Start"/>, read
    /// <see cref="ToMove"/>, give that player <see cref="CreateTurnContext"/> and pass their answer
    /// to <see cref="Act"/>; the match then plays forward (resolves the trick, draws, scores the
    /// round, deals the next one) up to the next decision. Between decisions it is just an object
    /// in memory, which is what a game server needs: no thread is held while a person thinks.
    /// <para>
    /// Optional observers (one per seat) receive every <see cref="IPlayer"/> callback except
    /// <see cref="IPlayer.GetTurn"/>: StartGame, StartRound, AddCard, EndTurn, EndRound and
    /// EndGame, in the same order <see cref="SantaseGame"/> delivers them. Deciding stays with the
    /// caller. <see cref="SantaseGame.Start"/> is this loop with two <see cref="IPlayer"/>s.
    /// </para>
    /// <para>Not thread-safe: drive a match from one thread at a time (e.g. a table actor).</para>
    /// </summary>
    public sealed class SantaseMatch
    {
        private static readonly IRoundWinnerPointsLogic RoundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();

        private readonly IPlayer firstObserver;

        private readonly IPlayer secondObserver;

        private readonly IGameRules gameRules;

        private readonly ILogger logger;

        private readonly Func<int, int> shuffle;

        private Round round;

        private bool started;

        /// <summary>
        /// Initializes a new instance of the <see cref="SantaseMatch"/> class without observers.
        /// </summary>
        /// <param name="options">The match settings; null uses the defaults.</param>
        public SantaseMatch(SantaseMatchOptions options = null)
            : this(null, null, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SantaseMatch"/> class.
        /// </summary>
        /// <param name="firstPlayerObserver">Receives the first player's callbacks (not GetTurn); may be null.</param>
        /// <param name="secondPlayerObserver">Receives the second player's callbacks (not GetTurn); may be null.</param>
        /// <param name="options">The match settings; null uses the defaults.</param>
        public SantaseMatch(IPlayer firstPlayerObserver, IPlayer secondPlayerObserver, SantaseMatchOptions options = null)
        {
            options ??= new SantaseMatchOptions();
            if (options.FirstToPlay != PlayerPosition.FirstPlayer && options.FirstToPlay != PlayerPosition.SecondPlayer)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.FirstToPlay, "FirstToPlay must be the first or the second player.");
            }

            this.firstObserver = firstPlayerObserver;
            this.secondObserver = secondPlayerObserver;
            this.gameRules = options.Rules ?? GameRulesProvider.Santase;
            this.logger = options.Logger ?? new NoLogger();
            this.shuffle = options.Shuffle;
            this.FirstToPlay = options.FirstToPlay;
        }

        /// <summary>
        /// Gets the first player's game points.
        /// </summary>
        public int FirstPlayerTotalPoints { get; private set; }

        /// <summary>
        /// Gets the second player's game points.
        /// </summary>
        public int SecondPlayerTotalPoints { get; private set; }

        /// <summary>
        /// Gets the number of finished rounds.
        /// </summary>
        public int RoundsPlayed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the match is over.
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Gets the match winner once <see cref="IsFinished"/>; otherwise <see cref="PlayerPosition.NoOne"/>.
        /// </summary>
        public PlayerPosition Winner { get; private set; }

        /// <summary>
        /// Gets the player who must act now, or <see cref="PlayerPosition.NoOne"/> before
        /// <see cref="Start"/> and after the match.
        /// </summary>
        public PlayerPosition ToMove => this.IsFinished || this.round == null ? PlayerPosition.NoOne : this.round.ToMove;

        // Who leads the next round. Test seam, as it was on SantaseGame.
        internal PlayerPosition FirstToPlay { get; set; }

        internal Round CurrentRound => this.round;

        /// <summary>
        /// Starts the match: StartGame to the observers, then the first deal.
        /// </summary>
        public void Start()
        {
            if (this.started)
            {
                throw new InvalidOperationException("The match has already been started.");
            }

            this.started = true;
            this.firstObserver?.StartGame(this.secondObserver?.Name);
            this.secondObserver?.StartGame(this.firstObserver?.Name);
            this.StartNextRoundOrFinish();
        }

        /// <summary>
        /// Creates the context for the player to move: what <see cref="IPlayer.GetTurn"/> receives.
        /// It is a copy; changing it does not affect the match.
        /// </summary>
        /// <returns>A new <see cref="PlayerTurnContext"/>.</returns>
        public PlayerTurnContext CreateTurnContext()
        {
            if (this.ToMove == PlayerPosition.NoOne)
            {
                throw new InvalidOperationException("Nobody is to move: the match has not started or is over.");
            }

            return this.round.CreateTurnContext();
        }

        /// <summary>
        /// Applies one action. Only <see cref="SantaseActResult.Ok"/> changes anything; the other
        /// results leave the match exactly as it was.
        /// <para>
        /// A trump change or a close keeps the same player to move (they still have to lead a
        /// card). Playing a card may finish the trick, the round and the match.
        /// </para>
        /// </summary>
        /// <param name="player">The player acting.</param>
        /// <param name="action">A <see cref="PlayerAction.PlayCard"/>, <see cref="PlayerAction.ChangeTrump"/>
        /// or <see cref="PlayerAction.CloseGame"/> action. A card lead with its marriage partner
        /// in hand always announces 20/40; the engine decides that, not the action.</param>
        /// <returns>What happened.</returns>
        public SantaseActResult Act(PlayerPosition player, PlayerAction action)
        {
            if (!this.started)
            {
                throw new InvalidOperationException("Start the match before acting.");
            }

            if (this.IsFinished)
            {
                return SantaseActResult.MatchFinished;
            }

            if (player != this.round.ToMove)
            {
                return SantaseActResult.NotYourTurn;
            }

            if (!this.round.TryAct(action))
            {
                return SantaseActResult.InvalidAction;
            }

            if (this.round.IsFinished)
            {
                this.OnRoundFinished();
            }

            return SantaseActResult.Ok;
        }

        internal void UpdatePoints(RoundResult roundResult)
        {
            var roundWinnerPoints = RoundWinnerPointsLogic.GetWinnerPoints(
                roundResult.FirstPlayer.RoundPoints,
                roundResult.SecondPlayer.RoundPoints,
                roundResult.GameClosedBy,
                roundResult.NoTricksPlayer,
                roundResult.LastTrickWinner,
                this.gameRules);

            // The round's loser leads the next round; a drawn round keeps the same opener.
            switch (roundWinnerPoints.Winner)
            {
                case PlayerPosition.FirstPlayer:
                    this.FirstPlayerTotalPoints += roundWinnerPoints.Points;
                    this.FirstToPlay = PlayerPosition.SecondPlayer;
                    break;
                case PlayerPosition.SecondPlayer:
                    this.SecondPlayerTotalPoints += roundWinnerPoints.Points;
                    this.FirstToPlay = PlayerPosition.FirstPlayer;
                    break;
            }
        }

        private void OnRoundFinished()
        {
            var roundResult = this.round.Result;
            this.UpdatePoints(roundResult);
            this.logger.LogLine($"{roundResult.FirstPlayer.RoundPoints} - {roundResult.SecondPlayer.RoundPoints}");
            this.RoundsPlayed++;
            this.StartNextRoundOrFinish();
        }

        private void StartNextRoundOrFinish()
        {
            var matchWinner = this.GetMatchWinner();
            if (matchWinner != PlayerPosition.NoOne)
            {
                this.IsFinished = true;
                this.Winner = matchWinner;
                this.firstObserver?.EndGame(matchWinner == PlayerPosition.FirstPlayer);
                this.secondObserver?.EndGame(matchWinner == PlayerPosition.SecondPlayer);
                return;
            }

            this.round = new Round(this.firstObserver, this.secondObserver, this.gameRules, this.FirstToPlay, this.shuffle);
            this.round.Start(this.FirstPlayerTotalPoints, this.SecondPlayerTotalPoints);
            if (this.round.IsFinished)
            {
                this.OnRoundFinished();
            }
        }

        private PlayerPosition GetMatchWinner()
        {
            if (this.FirstPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin)
            {
                return PlayerPosition.FirstPlayer;
            }

            if (this.SecondPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin)
            {
                return PlayerPosition.SecondPlayer;
            }

            return PlayerPosition.NoOne;
        }
    }
}
