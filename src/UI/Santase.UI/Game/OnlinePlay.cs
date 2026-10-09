namespace Santase.UI.Game
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Opens multiplayer on ednaigra.com in the browser (the app itself has no network play),
    /// keeping a failed launch's notice tied to the current page visit.
    /// </summary>
    public static class OnlinePlay
    {
        /// <summary>The site's lobby, opening its new Santase game window.</summary>
        public const string Url = "https://ednaigra.com/play?game=santase";

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
