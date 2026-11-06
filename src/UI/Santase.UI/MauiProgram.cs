using Microsoft.Extensions.Logging;

namespace Santase.UI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Before anything reads a setting (the language is resolved on first use).
            Game.SettingsStore.Current = new Game.PreferencesSettingsStore();
#if STORE_SHOTS
            StoreShotsDriver.Prepare();
#endif

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
