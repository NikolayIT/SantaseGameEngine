namespace Santase.UI
{
    using Microsoft.MobileBlazorBindings;

    using Xamarin.Forms;

    public class App : Application
    {
        public App()
        {
            var host = MobileBlazorBindingsHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Register app-specific services
                    // services.AddSingleton<AppState>();
                })
                .Build();

            this.MainPage = new ContentPage();
            host.AddComponent<GameScreen>(parent: this.MainPage);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
