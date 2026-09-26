namespace Santase.UI.Tests
{
    using System;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    using Xunit;

    [Collection(AppState.Name)]
    public class MatchHistoryTests
    {
        public MatchHistoryTests() => AppState.Reset();

        // A game is kept with the opponent's name in the language it was played in, and its id.
        // The history lists (start page, statistics) must name the opponent in the current
        // language: after switching, a game against "Късметлия" used to stay "Късметлия".
        [Fact]
        public void HistoryShouldNameOpponentsInTheCurrentLanguage() => UiThread.Run(async () =>
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Bulgarian);
            var (session, table, _) = GameTableTests.VsComputerTable("dummy", seed: 2);
            var tester = new TableTester(session, table, 2);
            table.StartGame();
            await tester.PlayToTheEndAsync();
            table.Dispose();

            var bulgarianName = AiOpponents.ById("dummy").DisplayName;
            var entry = Assert.Single(MatchHistoryStore.All());
            Assert.Equal(bulgarianName, entry.OpponentName);
            Assert.Equal(bulgarianName, entry.OpponentDisplayName);

            LocalizationManager.Instance.SetLanguage(LocalizationManager.English);
            var englishName = AiOpponents.ById("dummy").DisplayName;
            Assert.NotEqual(bulgarianName, englishName);
            Assert.Equal(englishName, MatchHistoryStore.All()[0].OpponentDisplayName);
        });

        // Records from app v1.0 have no opponent id, and an id may be unknown: they keep the name
        // they were stored with (never another opponent's).
        [Theory]
        [InlineData("")]
        [InlineData("retired")]
        public void RecordsWithoutAKnownOpponentShouldKeepTheirName(string opponentId)
        {
            MatchHistoryStore.Add(new MatchHistoryEntry("Old Bot", 11, 4, true, DateTime.UtcNow, opponentId));
            Assert.Equal("Old Bot", Assert.Single(MatchHistoryStore.All()).OpponentDisplayName);
            Assert.Null(AiOpponents.Find(opponentId));
            Assert.Same(AiOpponents.All[0], AiOpponents.ById(opponentId));
        }
    }
}
