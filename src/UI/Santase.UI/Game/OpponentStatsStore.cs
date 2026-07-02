namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Maui.Storage;

    /// <summary>
    /// Lifetime win/loss tallies per AI opponent, persisted via MAUI <see cref="Preferences"/>
    /// (one pair of counters per opponent id). Unlike <see cref="MatchHistoryStore"/>, which is
    /// capped, these run forever — they feed the "Your record: 3W – 1L" line on the start page
    /// and the per-opponent table on the statistics page.
    /// </summary>
    public static class OpponentStatsStore
    {
        private const string GamesKeyPrefix = "opp.games.";
        private const string WinsKeyPrefix = "opp.wins.";

        /// <summary>Raised after a result is recorded or the store is cleared, so views can refresh.</summary>
        public static event Action? Changed;

        public static (int Games, int Wins) For(string opponentId)
        {
            var games = Preferences.Default.Get(GamesKeyPrefix + opponentId, 0);
            var wins = Preferences.Default.Get(WinsKeyPrefix + opponentId, 0);
            return (games, Math.Min(wins, games));
        }

        public static void Record(string opponentId, bool won)
        {
            if (string.IsNullOrEmpty(opponentId))
            {
                return;
            }

            var (games, wins) = For(opponentId);
            Preferences.Default.Set(GamesKeyPrefix + opponentId, games + 1);
            if (won)
            {
                Preferences.Default.Set(WinsKeyPrefix + opponentId, wins + 1);
            }

            Changed?.Invoke();
        }

        public static void Clear(IEnumerable<string> opponentIds)
        {
            foreach (var id in opponentIds)
            {
                Preferences.Default.Remove(GamesKeyPrefix + id);
                Preferences.Default.Remove(WinsKeyPrefix + id);
            }

            Changed?.Invoke();
        }
    }
}
