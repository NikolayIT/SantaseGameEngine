namespace Santase.UI.Game
{
    public enum GameSpeed
    {
        Relaxed = 0,
        Normal = 1,
        Fast = 2,
    }

    /// <summary>
    /// Device-persisted app options (see <see cref="SettingsStore"/>). All values have sensible
    /// defaults so a fresh install needs no setup screen. The speed presets translate into the
    /// game's <see cref="GamePace"/> (AI think delay + trick settle time).
    /// </summary>
    public static class AppSettings
    {
        private const string SpeedKey = "settings.speed";
        private const string HapticsKey = "settings.haptics";
        private const string AssistsKey = "settings.assists";
        private const string PlayerNameKey = "settings.playerName";

        public static GameSpeed Speed
        {
            get => (GameSpeed)SettingsStore.Current.Get(SpeedKey, (int)GameSpeed.Normal);
            set => SettingsStore.Current.Set(SpeedKey, (int)value);
        }

        public static bool HapticsEnabled
        {
            get => SettingsStore.Current.Get(HapticsKey, true);
            set => SettingsStore.Current.Set(HapticsKey, value);
        }

        /// <summary>Beginner assists: 20/40 badges on own cards + the in-game hint button.</summary>
        public static bool AssistsEnabled
        {
            get => SettingsStore.Current.Get(AssistsKey, true);
            set => SettingsStore.Current.Set(AssistsKey, value);
        }

        public static string PlayerName
        {
            get => SettingsStore.Current.Get(PlayerNameKey, string.Empty);
            set => SettingsStore.Current.Set(PlayerNameKey, value ?? string.Empty);
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
