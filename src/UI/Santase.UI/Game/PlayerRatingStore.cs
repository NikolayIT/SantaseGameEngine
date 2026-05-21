namespace Santase.UI.Game
{
    using System;

    using Microsoft.Maui.Storage;

    /// <summary>
    /// The human player's ELO rating, persisted on the device via MAUI <see cref="Preferences"/>
    /// (Windows registry / Android SharedPreferences / iOS NSUserDefaults). A new player starts
    /// at <see cref="DefaultElo"/> and earns rating upward; the AI opponents are treated as
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

        public static int CurrentElo => Preferences.Default.Get(EloKey, DefaultElo);

        public static int GamesPlayed => Preferences.Default.Get(GamesKey, 0);

        public static int Wins => Preferences.Default.Get(WinsKey, 0);

        public static int Losses => Math.Max(0, GamesPlayed - Wins);

        public static RatingChange RecordResult(int opponentElo, bool won)
        {
            var oldElo = CurrentElo;
            var expected = 1.0 / (1.0 + Math.Pow(10.0, (opponentElo - oldElo) / 400.0));
            var score = won ? 1.0 : 0.0;
            var newElo = (int)Math.Round(oldElo + (KFactor * (score - expected)));

            Preferences.Default.Set(EloKey, newElo);
            Preferences.Default.Set(GamesKey, GamesPlayed + 1);
            if (won)
            {
                Preferences.Default.Set(WinsKey, Wins + 1);
            }

            return new RatingChange(oldElo, newElo, won);
        }

        public static void Reset()
        {
            Preferences.Default.Remove(EloKey);
            Preferences.Default.Remove(GamesKey);
            Preferences.Default.Remove(WinsKey);
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
