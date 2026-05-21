using Microsoft.Extensions.DependencyInjection;

namespace Santase.UI
{
    public partial class App : Application
    {
        public App()
        {
            // Resolve the device/saved language and set the thread culture before any page builds.
            _ = Localization.LocalizationManager.Instance;

            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}