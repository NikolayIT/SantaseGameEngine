namespace Santase.UI.Tests
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Santase.Logic;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;
    using Santase.UI.Localization;

    using Xunit;

    // The app's two string tables, and the rules page against the engine's rules.
    public class LocalizationTests
    {
        private static readonly string[] Languages = { LocalizationManager.English, LocalizationManager.Bulgarian };

        [Fact]
        public void BothLanguagesShouldHaveTheSameStrings()
        {
            var english = AppStrings.Table(LocalizationManager.English).Keys.OrderBy(k => k, StringComparer.Ordinal);
            var bulgarian = AppStrings.Table(LocalizationManager.Bulgarian).Keys.OrderBy(k => k, StringComparer.Ordinal);
            Assert.Equal(english, bulgarian);
        }

        // A translation takes the same arguments as the English text, so Format never throws or
        // drops a value in either language.
        [Fact]
        public void TranslationsShouldTakeTheSameArguments()
        {
            foreach (var (key, english) in AppStrings.Table(LocalizationManager.English))
            {
                var bulgarian = AppStrings.Table(LocalizationManager.Bulgarian)[key];
                Assert.True(Placeholders(english) == Placeholders(bulgarian), $"{key}: \"{english}\" vs \"{bulgarian}\"");
                _ = string.Format(bulgarian, "a", "b", "c", "d");
            }
        }

        // "Девятка" is Russian; the Bulgarian nine is "деветка".
        [Fact]
        public void BulgarianShouldNameTheNineInBulgarian()
        {
            foreach (var (key, text) in AppStrings.Table(LocalizationManager.Bulgarian))
            {
                Assert.DoesNotContain("девятк", text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains("деветка", AppStrings.Get(LocalizationManager.Bulgarian, "Rules_Nine_Title"), StringComparison.OrdinalIgnoreCase);
        }

        // The engine allows no 20/40, trump exchange or close on a round's first trick; the rules
        // page stated only "your lead" (and "more than two cards in the deck"), so the disabled
        // buttons on the first lead contradicted it.
        [Fact]
        public void TheRulesPageShouldSayWhatTheFirstTrickDoesNotAllow()
        {
            var firstTrick = new StartRoundState(new StateManager());
            Assert.False(firstTrick.CanAnnounce20Or40);
            Assert.False(firstTrick.CanChangeTrump);
            Assert.False(firstTrick.CanClose);

            foreach (var language in Languages)
            {
                var mentionsFirstTrick = language == LocalizationManager.English
                    ? new Regex("(first|second) trick")
                    : new Regex("(първата|втората) взятка");
                foreach (var key in new[] { "Rules_Marriages_Body", "Rules_Nine_Body", "Rules_Closing_Body" })
                {
                    Assert.Matches(mentionsFirstTrick, AppStrings.Get(language, key));
                }
            }
        }

        // A round played out without anyone reaching 66 can end 65-65 (the last trick's +10
        // included), which the engine scores as a draw; the page only said the higher total wins.
        [Fact]
        public void TheRulesPageShouldMentionTheDraw()
        {
            var draw = new RoundWinnerPointsPointsLogic().GetWinnerPoints(55, 65, PlayerPosition.NoOne, PlayerPosition.NoOne, PlayerPosition.FirstPlayer, GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.NoOne, draw.Winner);
            Assert.Equal(0, draw.Points);

            foreach (var language in Languages)
            {
                Assert.Contains("65", AppStrings.Get(language, "Rules_Scoring_Body"));
            }
        }

        private static string Placeholders(string text) =>
            string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(v => v, StringComparer.Ordinal));
    }
}
