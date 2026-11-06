namespace Santase.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Opens the privacy policy in the browser from the settings page, keeping a failed launch's
    /// notice tied to the current page visit.
    /// </summary>
    public static class PrivacyPolicy
    {
        public const string Url = "https://nksolutions.com/santase/privacy-policy.html";

        public static Task OpenAsync(PageActions actions, Func<string, Task<bool>> openBrowser, Func<Task> showFailure) => actions.ConfirmAsync(
            async () =>
            {
                try
                {
                    return !await openBrowser(Url);
                }
                catch
                {
                    return true;
                }
            },
            showFailure);
    }
}
