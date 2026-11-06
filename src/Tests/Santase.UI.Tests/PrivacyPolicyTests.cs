namespace Santase.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    using Xunit;

    // The App Store wants the privacy policy reachable inside the app: Settings opens it in the
    // browser. The launch shares the page's gate with its navigation, and a failed launch shows its
    // notice only on the page visit that opened it.
    public class PrivacyPolicyTests
    {
        [Fact]
        public async Task SuccessfulLaunchShouldOpenThePrivacyPolicyWithoutAnError()
        {
            var page = new PageActions();
            page.Activate();
            string? opened = null;

            await PrivacyPolicy.OpenAsync(
                page,
                url =>
                {
                    opened = url;
                    return Task.FromResult(true);
                },
                () => throw new InvalidOperationException("A successful launch showed an error."));

            Assert.Equal("https://nksolutions.com/santase/privacy-policy.html", opened);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedBrowserLaunchShouldShowARecoverableError(bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var errors = 0;

            await PrivacyPolicy.OpenAsync(
                page,
                _ => throws ? throw new InvalidOperationException("No browser") : Task.FromResult(false),
                () =>
                {
                    errors++;
                    return Task.CompletedTask;
                });

            Assert.Equal(1, errors);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task LateFailuresShouldNotShowAnErrorAfterLeavingTheOriginalVisit(bool returnToPage, bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var pending = PrivacyPolicy.OpenAsync(
                page,
                _ => browser.Task,
                () => throw new InvalidOperationException("A browser failure reached a stale page."));

            page.Deactivate();
            if (returnToPage)
            {
                page.Activate();
            }

            if (throws)
            {
                browser.SetException(new InvalidOperationException("Late browser failure"));
            }
            else
            {
                browser.SetResult(false);
            }

            await pending;
        }

        [Theory]
        [InlineData(LocalizationManager.English, "Privacy policy")]
        [InlineData(LocalizationManager.Bulgarian, "Политика за поверителност")]
        public void PrivacyPolicyShouldExplainItsDestinationAndRecoveryInBothLanguages(string language, string title)
        {
            var text = AppStrings.Table(language);
            Assert.Equal(title, text["Settings_PrivacyPolicy"]);
            Assert.NotEmpty(text["Settings_PrivacyPolicyHint"]);
            Assert.Contains(PrivacyPolicy.Url, text["Settings_PrivacyPolicyUnavailable"]);
        }

        [Fact]
        public void TheSettingsPageShouldOfferThePolicyThroughItsPageGate()
        {
            var pages = PagesDirectory();
            var page = XDocument.Load(Path.Combine(pages, "SettingsPage.xaml"));
            var button = page.Descendants().Single(e => (string?)e.Attribute("Clicked") == "OnOpenPrivacyPolicy");
            Assert.Equal("Button", button.Name.LocalName);
            Assert.Equal("{loc:Tr Settings_PrivacyPolicy}", (string?)button.Attribute("Text"));
            Assert.Equal("{loc:Tr Settings_PrivacyPolicyHint}", (string?)button.Attribute("SemanticProperties.Hint"));

            var code = File.ReadAllText(Path.Combine(pages, "SettingsPage.xaml.cs"));
            var start = code.IndexOf("private async void OnOpenPrivacyPolicy", StringComparison.Ordinal);
            var end = code.IndexOf("private async void OnOpenGitHub", start, StringComparison.Ordinal);
            var handler = code[start..end];
            Assert.Contains("PrivacyPolicy.OpenAsync(", handler);
            Assert.Contains("this.actions,", handler);
            Assert.Contains("ExternalBrowser.OpenAsync,", handler);
            Assert.Contains("text[\"Settings_PrivacyPolicyUnavailable\"]", handler);
            Assert.Contains("this.actions.Activate();", code);
            Assert.Contains("this.actions.Deactivate();", code);
        }

        private static string PagesDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Santase.slnx")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("The src folder was not found."), "UI", "Santase.UI", "Pages");
        }
    }
}
