namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Maui.Storage;

    public sealed class MatchHistoryEntry
    {
        public MatchHistoryEntry(string opponentName, int myScore, int opponentScore, bool won, DateTime whenUtc, string opponentId = "")
        {
            this.OpponentName = opponentName;
            this.MyScore = myScore;
            this.OpponentScore = opponentScore;
            this.Won = won;
            this.WhenUtc = whenUtc;
            this.OpponentId = opponentId;
        }

        public string OpponentName { get; }

        /// <summary>Stable, language-independent opponent id (empty on records from app v1.0).</summary>
        public string OpponentId { get; }

        public int MyScore { get; }

        public int OpponentScore { get; }

        public bool Won { get; }

        public DateTime WhenUtc { get; }

        public string ScoreText => $"{this.MyScore} – {this.OpponentScore}";
    }

    /// <summary>
    /// Completed vs-AI games, persisted on the device via MAUI <see cref="Preferences"/>. Stored
    /// as one delimited line per game (pipe-separated fields, newline-separated records) — no JSON,
    /// so it's trimming/AOT-safe on every platform. Newest first, capped at <see cref="MaxEntries"/>.
    /// </summary>
    public static class MatchHistoryStore
    {
        private const string Key = "match.history";
        private const int MaxEntries = 100;
        private const char FieldSeparator = '|';
        private const char RecordSeparator = '\n';

        public static IReadOnlyList<MatchHistoryEntry> All()
        {
            var raw = Preferences.Default.Get(Key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return Array.Empty<MatchHistoryEntry>();
            }

            var list = new List<MatchHistoryEntry>();
            foreach (var record in raw.Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = record.Split(FieldSeparator);
                if (fields.Length < 5)
                {
                    continue;
                }

                list.Add(new MatchHistoryEntry(
                    fields[3],
                    ParseInt(fields[1]),
                    ParseInt(fields[2]),
                    fields[0] == "1",
                    ParseDate(fields[4]),
                    fields.Length >= 6 ? fields[5] : string.Empty));
            }

            return list;
        }

        public static void Add(MatchHistoryEntry entry)
        {
            var list = All().ToList();
            list.Insert(0, entry);
            if (list.Count > MaxEntries)
            {
                list = list.GetRange(0, MaxEntries);
            }

            Preferences.Default.Set(Key, string.Join(RecordSeparator.ToString(), list.Select(Encode)));
        }

        public static void Clear() => Preferences.Default.Remove(Key);

        private static string Encode(MatchHistoryEntry e) => string.Join(
            FieldSeparator.ToString(),
            e.Won ? "1" : "0",
            e.MyScore.ToString(CultureInfo.InvariantCulture),
            e.OpponentScore.ToString(CultureInfo.InvariantCulture),
            Sanitize(e.OpponentName),
            e.WhenUtc.ToString("o", CultureInfo.InvariantCulture),
            Sanitize(e.OpponentId));

        private static string Sanitize(string name) =>
            name.Replace(FieldSeparator, ' ').Replace(RecordSeparator, ' ');

        private static int ParseInt(string s) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static DateTime ParseDate(string s) =>
            DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var v) ? v : DateTime.UtcNow;
    }
}
