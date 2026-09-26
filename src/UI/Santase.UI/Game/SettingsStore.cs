namespace Santase.UI.Game
{
    using System;

    /// <summary>
    /// Where the app keeps its settings, the player's rating and the game history: MAUI
    /// Preferences in the app (set in <c>MauiProgram</c>), memory in the UI tests.
    /// </summary>
    public interface ISettingsStore
    {
        T Get<T>(string key, T defaultValue);

        void Set<T>(string key, T value);

        void Remove(string key);
    }

    /// <summary>The app's <see cref="ISettingsStore"/>. No MAUI types here: the UI tests compile this file.</summary>
    public static class SettingsStore
    {
        private static ISettingsStore? current;

        public static ISettingsStore Current
        {
            get => current ?? throw new InvalidOperationException("SettingsStore.Current is set when the app starts (MauiProgram).");
            set => current = value;
        }
    }
}
