namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Players;
    using Santase.UI.Localization;

    /// <summary>
    /// A selectable computer opponent: a display description plus the factory that builds the
    /// underlying <see cref="IPlayer"/>. The <see cref="Elo"/> values are not guessed — they are
    /// produced by the simulator's round-robin ELO tournament
    /// (<c>dotnet run ... -- elo</c>) and pasted in here, anchored so the Dummy sits at 1200.
    /// </summary>
    public sealed class AiOpponent : INotifyPropertyChanged
    {
        private readonly string nameKey;

        private readonly string taglineKey;

        public AiOpponent(string id, string avatar, string nameKey, string taglineKey, int difficulty, int elo, Func<IPlayer> factory)
        {
            this.Id = id;
            this.Avatar = avatar;
            this.nameKey = nameKey;
            this.taglineKey = taglineKey;
            this.Difficulty = difficulty;
            this.Elo = elo;
            this.Factory = factory;

            // Re-raise the localized properties when the language switches so bound labels (the
            // start-page opponent list) update in place. These instances are static singletons and
            // so are the manager + stats store, so the subscriptions live for the app's lifetime —
            // no leak.
            LocalizationManager.Instance.PropertyChanged += (_, _) => this.RaiseDisplayChanged();
            OpponentStatsStore.Changed += () => this.Raise(nameof(this.RecordText), nameof(this.HasRecord));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get; }

        /// <summary>Emoji avatar shown in the opponent list.</summary>
        public string Avatar { get; }

        // Display name + tagline resolve through the active language; PropertyChanged (above) makes
        // bound views refresh on a switch. Id stays the stable, language-independent key.
        public string DisplayName => LocalizationManager.Instance[this.nameKey];

        public string Tagline => LocalizationManager.Instance[this.taglineKey];

        // 1..5, used for the star flavour + the localized difficulty label; the ELO badge carries
        // the precise strength.
        public int Difficulty { get; }

        public int Elo { get; }

        public Func<IPlayer> Factory { get; }

        public string DifficultyStars =>
            new string('★', Math.Clamp(this.Difficulty, 0, 5)) +
            new string('☆', 5 - Math.Clamp(this.Difficulty, 0, 5));

        public string DifficultyLabel => LocalizationManager.Instance[$"Diff_{Math.Clamp(this.Difficulty, 1, 5)}"];

        public string EloText => $"ELO {this.Elo}";

        /// <summary>Lifetime human-vs-this-opponent record ("Your record: 3W – 1L").</summary>
        public string RecordText
        {
            get
            {
                var (games, wins) = OpponentStatsStore.For(this.Id);
                return games > 0
                    ? LocalizationManager.Instance.Format("Opp_RecordFormat", wins, games - wins)
                    : string.Empty;
            }
        }

        public bool HasRecord => OpponentStatsStore.For(this.Id).Games > 0;

        public IPlayer CreatePlayer() => this.Factory();

        private void RaiseDisplayChanged() => this.Raise(
            nameof(this.DisplayName),
            nameof(this.Tagline),
            nameof(this.DifficultyLabel),
            nameof(this.RecordText));

        private void Raise(params string[] names)
        {
            foreach (var name in names)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public static class AiOpponents
    {
        // Ratings from the `elo` simulator round-robin (anchor: Dummy = 1200). Re-run that suite
        // and update these if the AI players change.
        public static IReadOnlyList<AiOpponent> All { get; } = new[]
        {
            new AiOpponent(
                "dummy",
                "🎲",
                "Opp_Dummy_Name",
                "Opp_Dummy_Tag",
                1,
                1200,
                () => new DummyPlayerChangingTrump()),
            new AiOpponent(
                "smart",
                "🃏",
                "Opp_Smart_Name",
                "Opp_Smart_Tag",
                2,
                2030,
                () => new SmartPlayer()),
            new AiOpponent(
                "claude",
                "🧭",
                "Opp_Claude_Name",
                "Opp_Claude_Tag",
                3,
                2131,
                () => new ClaudePlayer()),
            new AiOpponent(
                "neural",
                "🧠",
                "Opp_Neural_Name",
                "Opp_Neural_Tag",
                4,
                2289,
                () => new ClaudePlayerNeural()),
            new AiOpponent(
                "ismcts",
                "👑",
                "Opp_Ismcts_Name",
                "Opp_Ismcts_Tag",
                5,
                2345,
                () => new ClaudePlayerIsmcts()),
        };

        public static AiOpponent ById(string? id) =>
            All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }
}
