namespace Santase.UI.Tests
{
    using System;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Players;
    using Santase.UI.Game;
    using Santase.UI.Localization;

    using Xunit;

    // The game table (GameViewModel) with a real GameSession, played through its commands on a
    // UI-like thread. See TableTester for what is checked at every decision and result.
    [Collection(AppState.Name)]
    public class GameTableTests
    {
        private static readonly LocalizationManager Loc = LocalizationManager.Instance;

        public GameTableTests() => AppState.Reset();

        [Fact]
        public void HotSeatGamesAtTheTableShouldShowEachPersonTheirOwnSeat() => UiThread.Run(async () =>
        {
            for (var seed = 0; seed < 8; seed++)
            {
                var (session, table, _) = HotSeatTable(seed);
                var tester = new TableTester(session, table, seed);
                table.StartGame();
                await tester.PlayToTheEndAsync();

                Assert.True(tester.Handoffs > 0);
                Assert.True(tester.Decisions > 20);
                table.Dispose();
            }

            // Games between two people on one device are not ranked or kept.
            Assert.Equal(0, PlayerRatingStore.GamesPlayed);
            Assert.Empty(MatchHistoryStore.All());
        });

        // Hot-seat: while the device is passed, the "pass the device" screen covers the table but
        // is not fully opaque, so the person who just played must not leave their cards face up
        // under it (they did: the hand stayed until the next person tapped "Ready").
        [Fact]
        public void HotSeatHandoffShouldHideTheHandOfThePersonWhoJustPlayed() => UiThread.Run(async () =>
        {
            for (var seed = 20; seed < 24; seed++)
            {
                var (session, table, _) = HotSeatTable(seed);
                var tester = new TableTester(session, table, seed);
                tester.OnHandoff = next =>
                {
                    Assert.True(table.IsHandoffOverlayVisible);
                    Assert.DoesNotContain(table.MyHand, card => !card.IsFaceDown);
                };
                table.StartGame();
                await tester.PlayToTheEndAsync();
                Assert.True(tester.Handoffs > 10);
                table.Dispose();
            }
        });

        // Hot-seat: the game-over screen must celebrate the winner, whoever made the last move.
        // It was told from the last mover's side, so when they had lost it said "Defeat" (with
        // the winner's name underneath).
        [Fact]
        public void HotSeatGameOverShouldBeToldFromTheWinnersSide() => UiThread.Run(async () =>
        {
            var lastMoverLost = 0;
            for (var seed = 30; seed < 40; seed++)
            {
                var (session, table, _) = HotSeatTable(seed);
                var tester = new TableTester(session, table, seed);
                PlayerSlot? lastMover = null;
                session.MovePlayed += move => lastMover = move.Slot;
                table.StartGame();
                await tester.PlayToTheEndAsync();

                var winner = tester.Winner!.Value;
                if (lastMover != winner)
                {
                    lastMoverLost++;
                }

                Assert.Equal(Loc["GameOver_Victory"], table.GameOverlayTitle);
                Assert.Equal("\U0001F3C6", table.GameOverlayIcon);
                Assert.Equal(session.GetName(winner), table.MyName);
                Assert.True(table.MyGamePoints >= 11);
                Assert.True(table.OpponentGamePoints < 11);
                Assert.False(table.IsRatingChangeVisible);
                table.Dispose();
            }

            Assert.True(lastMoverLost > 0);
        });

        [Theory]
        [InlineData("dummy")]
        [InlineData("smart")]
        [InlineData("claude")]
        public void GamesAgainstTheComputerAtTheTableShouldShowTheSeatAndBeRecordedOnce(string opponentId) => UiThread.Run(async () =>
        {
            for (var game = 1; game <= 2; game++)
            {
                var (session, table, _) = VsComputerTable(opponentId, seed: game);
                var tester = new TableTester(session, table, game);
                table.StartGame();
                await tester.PlayToTheEndAsync();

                var won = tester.Winner == PlayerSlot.First;
                Assert.Equal(0, tester.Handoffs);
                Assert.Equal(won ? Loc["GameOver_Victory"] : Loc["GameOver_Defeat"], table.GameOverlayTitle);
                Assert.True(table.IsRatingChangeVisible);

                Assert.Equal(game, PlayerRatingStore.GamesPlayed);
                var history = MatchHistoryStore.All();
                Assert.Equal(game, history.Count);
                Assert.Equal(opponentId, history[0].OpponentId);
                Assert.Equal(won, history[0].Won);
                Assert.Equal(table.MyGamePoints, history[0].MyScore);
                Assert.Equal(table.OpponentGamePoints, history[0].OpponentScore);
                Assert.Equal(game, OpponentStatsStore.For(opponentId).Games);
                table.Dispose();
            }
        });

        internal static (GameSession Session, GameViewModel Table, FakeTableHost Host) HotSeatTable(int seed, GamePace? pace = null)
        {
            var session = new GameSession(GameMode.HotSeat, "Ann", "Bob", null, pace ?? GamePace.Instant, new Random(seed).Next);
            var host = new FakeTableHost();
            return (session, new GameViewModel(session, null, host), host);
        }

        internal static (GameSession Session, GameViewModel Table, FakeTableHost Host) VsComputerTable(string opponentId, int seed, GamePace? pace = null)
        {
            var opponent = AiOpponents.ById(opponentId);
            IRestorablePlayer computer = opponentId switch
            {
                "dummy" => new DummyPlayerChangingTrump { Rng = new Random(seed) },
                "smart" => new SmartPlayer(),
                _ => new ClaudePlayer(),
            };
            var session = new GameSession(GameMode.VsAi, "Ann", opponent.DisplayName, computer, pace ?? GamePace.Instant, new Random(seed).Next);
            var host = new FakeTableHost();
            return (session, new GameViewModel(session, opponent, host), host);
        }
    }
}
