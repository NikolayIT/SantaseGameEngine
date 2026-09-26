namespace Santase.UI.Game
{
    using System;

    /// <summary>
    /// The human player's ELO rating, persisted on the device (see <see cref="SettingsStore"/>). A
    /// new player starts at <see cref="DefaultElo"/> and earns rating upward; the AI opponents are treated as
    /// fixed-rating anchors (see <see cref="AiOpponent.Elo"/>), so only the human's number moves.
    /// </summary>
    public static class PlayerRatingStore
    {
        public const int DefaultElo = 1000;

        // Standard ELO step size. 32 gives noticeable but not wild swings over a handful of games.
        private const int KFactor = 32;

        private const string EloKey = "player.elo";
        private const string GamesKey = "player.games";
        private const string WinsKey = "player.wins";
        private const string PeakEloKey = "player.peakElo";
        private const string StreakKey = "player.streak";
        private const string BestStreakKey = "player.bestStreak";

        public static int CurrentElo => SettingsStore.Current.Get(EloKey, DefaultElo);

        public static int GamesPlayed => SettingsStore.Current.Get(GamesKey, 0);

        public static int Wins => SettingsStore.Current.Get(WinsKey, 0);

        public static int Losses => Math.Max(0, GamesPlayed - Wins);

        /// <summary>Highest rating ever reached (never below the current or starting rating).</summary>
        public static int PeakElo => Math.Max(CurrentElo, SettingsStore.Current.Get(PeakEloKey, DefaultElo));

        /// <summary>Signed run of results: +n = n wins in a row, -n = n losses in a row.</summary>
        public static int CurrentStreak => SettingsStore.Current.Get(StreakKey, 0);

        public static int BestWinStreak => SettingsStore.Current.Get(BestStreakKey, 0);

        public static RatingChange RecordResult(int opponentElo, bool won)
        {
            var oldElo = CurrentElo;
            var expected = 1.0 / (1.0 + Math.Pow(10.0, (opponentElo - oldElo) / 400.0));
            var score = won ? 1.0 : 0.0;
            var newElo = (int)Math.Round(oldElo + (KFactor * (score - expected)));

            SettingsStore.Current.Set(EloKey, newElo);
            SettingsStore.Current.Set(GamesKey, GamesPlayed + 1);
            SettingsStore.Current.Set(PeakEloKey, Math.Max(PeakElo, newElo));

            var streak = CurrentStreak;
            streak = won ? (streak > 0 ? streak + 1 : 1) : (streak < 0 ? streak - 1 : -1);
            SettingsStore.Current.Set(StreakKey, streak);
            if (won)
            {
                SettingsStore.Current.Set(WinsKey, Wins + 1);
                SettingsStore.Current.Set(BestStreakKey, Math.Max(BestWinStreak, streak));
            }

            return new RatingChange(oldElo, newElo, won);
        }

        public static void Reset()
        {
            SettingsStore.Current.Remove(EloKey);
            SettingsStore.Current.Remove(GamesKey);
            SettingsStore.Current.Remove(WinsKey);
            SettingsStore.Current.Remove(PeakEloKey);
            SettingsStore.Current.Remove(StreakKey);
            SettingsStore.Current.Remove(BestStreakKey);
        }
    }

    public sealed class RatingChange
    {
        public RatingChange(int oldElo, int newElo, bool won)
        {
            this.OldElo = oldElo;
            this.NewElo = newElo;
            this.Won = won;
        }

        public int OldElo { get; }

        public int NewElo { get; }

        public bool Won { get; }

        public int Delta => this.NewElo - this.OldElo;
    }
}
