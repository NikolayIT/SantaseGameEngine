using Foundation;
using UIKit;

namespace Santase.UI
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
#if STORE_SHOTS
        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            var launched = base.FinishedLaunching(application, launchOptions);
            StoreShotsDriver.Start();
            return launched;
        }

#endif
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
