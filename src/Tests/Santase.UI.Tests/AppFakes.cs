namespace Santase.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    using Xunit;

    // Tests that use the app's static state (the settings store, the language, the rating and
    // history stores) run one at a time, each on a fresh in-memory store in English.
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class AppState
    {
        public const string Name = "App state";

        public static MemorySettingsStore Reset(string language = LocalizationManager.English)
        {
            var store = new MemorySettingsStore();
            SettingsStore.Current = store;
            LocalizationManager.Instance.SetLanguage(language);
            return store;
        }
    }

    public sealed class MemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object?> values = new();

        public T Get<T>(string key, T defaultValue) => this.values.TryGetValue(key, out var value) ? (T)value! : defaultValue;

        public void Set<T>(string key, T value) => this.values[key] = value;

        public void Remove(string key) => this.values.Remove(key);
    }

    // The page around the table: timers are kept until the test runs them, like a clock the test
    // moves forward.
    internal sealed class FakeTableHost : IGameTableHost
    {
        public List<(TimeSpan Delay, Action Action)> Timers { get; } = new();

        public int Leaves { get; private set; }

        public int Vibrations { get; private set; }

        public void After(TimeSpan delay, Action action) => this.Timers.Add((delay, action));

        public void Vibrate(bool isLong) => this.Vibrations++;

        public void Leave() => this.Leaves++;

        // Runs the oldest timer still waiting.
        public void RunOldestTimer()
        {
            var (_, action) = this.Timers[0];
            this.Timers.RemoveAt(0);
            action();
        }

        // Runs the timers set so far (not the ones they set).
        public void RunTimers()
        {
            var due = this.Timers.ToList();
            this.Timers.Clear();
            foreach (var (_, action) in due)
            {
                action();
            }
        }
    }
}
