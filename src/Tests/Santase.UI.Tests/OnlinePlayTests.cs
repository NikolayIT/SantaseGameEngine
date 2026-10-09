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

    // "Play people online" on the start page opens ednaigra.com's Santase lobby in the browser.
    // The launch shares the page's gate with its navigation, and a failed launch shows its notice
    // only on the page visit that opened it.
    public class OnlinePlayTests
    {
        [Fact]
        public async Task SuccessfulLaunchShouldOpenTheSantaseLobbyWithoutAnError()
        {
            var page = new PageActions();
            page.Activate();
            string? opened = null;

            await OnlinePlay.OpenAsync(
                page,
                url =>
                {
                    opened = url;
                    return Task.FromResult(true);
                },
                () => throw new InvalidOperationException("Successful browser launch showed an error."));

            var destination = new Uri(Assert.IsType<string>(opened));
            Assert.Equal("https", destination.Scheme);
            Assert.Equal("ednaigra.com", destination.Host);
            Assert.Equal("/play", destination.AbsolutePath);
            Assert.Equal("?game=santase", destination.Query);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedBrowserLaunchShouldShowARecoverableError(bool throws)
        {
            var page = new PageActions();
            page.Activate();
            var errors = 0;

            await OnlinePlay.OpenAsync(
                page,
                _ => throws ? throw new InvalidOperationException("No browser") : Task.FromResult(false),
                () =>
                {
                    errors++;
                    return Task.CompletedTask;
                });

            Assert.Equal(1, errors);
        }

        [Fact]
        public async Task RepeatedTapsShouldShareThePageGateAndAllowRetryAfterTheErrorCloses()
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var dialog = new TaskCompletionSource();
            var launches = 0;
            var errors = 0;
            Task<bool> Open(string url)
            {
                launches++;
                return browser.Task;
            }

            Task ShowFailure()
            {
                errors++;
                return dialog.Task;
            }

            var first = OnlinePlay.OpenAsync(page, Open, ShowFailure);
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            await page.RunAsync(() => throw new InvalidOperationException("Navigation during browser launch"));
            Assert.Equal(1, launches);

            browser.SetResult(false);
            Assert.Equal(1, errors);
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(1, launches);

            dialog.SetResult();
            await first;
            await OnlinePlay.OpenAsync(page, Open, ShowFailure);
            Assert.Equal(2, launches);
            Assert.Equal(2, errors);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AFailedLaunchShouldNotShowAnErrorAfterLeavingTheOriginalPageVisit(bool returnToPage)
        {
            var page = new PageActions();
            page.Activate();
            var browser = new TaskCompletionSource<bool>();
            var pending = OnlinePlay.OpenAsync(
                page,
                _ => browser.Task,
                () => throw new InvalidOperationException("Stale browser error"));

            page.Deactivate();
            if (returnToPage)
            {
                page.Activate();
            }

            browser.SetResult(false);
            await pending;
        }

        [Fact]
        public async Task AHiddenPageShouldIgnoreTaps()
        {
            var page = new PageActions();
            var runs = 0;
            Task Run()
            {
                runs++;
                return Task.CompletedTask;
            }

            await page.RunAsync(Run);
            page.Activate();
            await page.RunAsync(Run);
            page.Deactivate();
            await page.RunAsync(Run);

            Assert.Equal(1, runs);
        }

        [Theory]
        [InlineData(LocalizationManager.English, "Play people online", "Santase")]
        [InlineData(LocalizationManager.Bulgarian, "Играй с хора онлайн", "сантасе")]
        public void OnlinePlayShouldExplainItsDestinationAndRecoveryInBothLanguages(string language, string title, string game)
        {
            var text = AppStrings.Table(language);
            Assert.Equal(title, text["Start_PlayOnline"]);
            Assert.Contains("ednaigra.com", text["Start_OnlineHint"]);
            Assert.Contains("ednaigra.com", text["Start_OnlineUnavailable"]);
            Assert.Contains(game, text["Start_OnlineUnavailable"]);
        }

        [Fact]
        public void TheStartPageShouldOfferOnlinePlayAsASecondaryButtonAfterTheOpponents()
        {
            var page = XDocument.Load(Path.Combine(PagesDirectory(), "StartPage.xaml"));
            var button = page.Descendants().Single(e => (string?)e.Attribute("Clicked") == "OnPlayOnline");
            var named = page.Descendants().SelectMany(e => e.Attributes().Where(a => a.Name.LocalName == "Name").Select(a => a.Value)).ToArray();
            Assert.Equal("Button", button.Name.LocalName);
            Assert.Equal("{loc:Tr Start_PlayOnline}", (string?)button.Attribute("Text"));
            Assert.Equal("{loc:Tr Start_OnlineHint}", (string?)button.Attribute("SemanticProperties.Hint"));
            Assert.Equal("Transparent", (string?)button.Attribute("BackgroundColor"));
            Assert.Equal("1", (string?)button.Attribute("BorderWidth"));
            Assert.True(Array.IndexOf(named, "OpponentList") < Array.IndexOf(named, "OnlinePlayButton"));
            Assert.True(Array.IndexOf(named, "OnlinePlayButton") < Array.IndexOf(named, "SeeAllLabel"));
            Assert.Contains(button.Parent!.Elements(), e => (string?)e.Attribute("Text") == "ednaigra.com");

            var code = File.ReadAllText(Path.Combine(PagesDirectory(), "StartPage.xaml.cs"));
            var start = code.IndexOf("private async void OnPlayOnline", StringComparison.Ordinal);
            var end = code.IndexOf("private void OnPlayerNameChanged", start, StringComparison.Ordinal);
            var handler = code[start..end];
            Assert.Contains("OnlinePlay.OpenAsync(", handler);
            Assert.Contains("this.actions,", handler);
            Assert.Contains("Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred)", handler);
            Assert.Contains("text[\"Start_OnlineUnavailable\"]", handler);
            Assert.Contains("this.DisplayAlertAsync", handler);

            // Navigation and the launch share one gate that knows the page visit.
            Assert.DoesNotContain("OneAtATime", code);
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
