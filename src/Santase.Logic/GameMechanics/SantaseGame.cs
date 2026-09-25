namespace Santase.Logic.GameMechanics
{
    using System;

    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    /// <summary>
    /// Plays whole matches between two <see cref="IPlayer"/>s: a thin loop over
    /// <see cref="SantaseMatch"/> that asks the player to move for an action and applies it. The
    /// rules live in <see cref="SantaseMatch"/>; use it directly when moves arrive from outside
    /// (e.g. over the network) instead of from an <see cref="IPlayer"/>.
    /// </summary>
    public class SantaseGame : ISantaseGame
    {
        private readonly IGameRules gameRules;

        private readonly IPlayer firstPlayer;

        private readonly IPlayer secondPlayer;

        private readonly ILogger logger;

        private readonly Func<int, int> shuffle;

        private SantaseMatch match;

        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer)
            : this(firstPlayer, secondPlayer, GameRulesProvider.Santase, new NoLogger())
        {
        }

        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer, IGameRules gameRules, ILogger logger)
            : this(firstPlayer, secondPlayer, gameRules, logger, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SantaseGame"/> class.
        /// </summary>
        /// <param name="firstPlayer">The first player.</param>
        /// <param name="secondPlayer">The second player.</param>
        /// <param name="gameRules">The rules.</param>
        /// <param name="logger">Receives one line per round.</param>
        /// <param name="shuffle">Random source for the deals (see <see cref="SantaseMatchOptions.Shuffle"/>);
        /// null means <see cref="Random.Shared"/>.</param>
        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer, IGameRules gameRules, ILogger logger, Func<int, int> shuffle)
        {
            this.firstPlayer = firstPlayer;
            this.secondPlayer = secondPlayer;
            this.gameRules = gameRules;
            this.logger = logger;
            this.shuffle = shuffle;
        }

        public int FirstPlayerTotalPoints => this.match?.FirstPlayerTotalPoints ?? 0;

        public int SecondPlayerTotalPoints => this.match?.SecondPlayerTotalPoints ?? 0;

        public int RoundsPlayed => this.match?.RoundsPlayed ?? 0;

        public PlayerPosition Start(PlayerPosition firstToPlayInFirstRound = PlayerPosition.FirstPlayer)
        {
            var options = new SantaseMatchOptions
            {
                FirstToPlay = firstToPlayInFirstRound,
                Rules = this.gameRules,
                Logger = this.logger,
                Shuffle = this.shuffle,

                // Whole-match loops never ask for views; skip the bookkeeping.
                RecordHistory = false,
            };

            var currentMatch = new SantaseMatch(this.firstPlayer, this.secondPlayer, options);
            this.match = currentMatch;
            currentMatch.Start();

            while (!currentMatch.IsFinished)
            {
                var toMove = currentMatch.ToMove;
                var player = toMove == PlayerPosition.FirstPlayer ? this.firstPlayer : this.secondPlayer;
                var action = player.GetTurn(currentMatch.CreateTurnContext());
                if (currentMatch.Act(toMove, action) != SantaseActResult.Ok)
                {
                    // A bot is code: an illegal move from it is a bug, not a game situation.
                    throw new InternalGameException($"Invalid action played from {player.Name}");
                }
            }

            return currentMatch.Winner;
        }
    }
}
