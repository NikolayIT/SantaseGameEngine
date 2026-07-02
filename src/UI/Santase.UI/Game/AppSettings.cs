namespace Santase.UI.Game
{
    using Microsoft.Maui.Storage;

    public enum GameSpeed
    {
        Relaxed = 0,
        Normal = 1,
        Fast = 2,
    }

    /// <summary>
    /// Device-persisted app options (MAUI <see cref="Preferences"/>). All values have sensible
    /// defaults so a fresh install needs no setup screen. The speed presets translate into the
    /// two pacing knobs <see cref="GameSession"/> exposes (AI think delay + trick settle time).
    /// </summary>
    public static class AppSettings
    {
        private const string SpeedKey = "settings.speed";
        private const string HapticsKey = "settings.haptics";
        private const string AssistsKey = "settings.assists";
        private const string PlayerNameKey = "settings.playerName";

        public static GameSpeed Speed
        {
            get => (GameSpeed)Preferences.Default.Get(SpeedKey, (int)GameSpeed.Normal);
            set => Preferences.Default.Set(SpeedKey, (int)value);
        }

        public static bool HapticsEnabled
        {
            get => Preferences.Default.Get(HapticsKey, true);
            set => Preferences.Default.Set(HapticsKey, value);
        }

        /// <summary>Beginner assists: 20/40 badges on own cards + the in-game hint button.</summary>
        public static bool AssistsEnabled
        {
            get => Preferences.Default.Get(AssistsKey, true);
            set => Preferences.Default.Set(AssistsKey, value);
        }

        public static string PlayerName
        {
            get => Preferences.Default.Get(PlayerNameKey, string.Empty);
            set => Preferences.Default.Set(PlayerNameKey, value ?? string.Empty);
        }

        public static int AiThinkDelayMs => Speed switch
        {
            GameSpeed.Relaxed => 800,
            GameSpeed.Fast => 150,
            _ => 400,
        };

        public static int TrickSettleMs => Speed switch
        {
            GameSpeed.Relaxed => 1500,
            GameSpeed.Fast => 500,
            _ => 900,
        };
    }
}
