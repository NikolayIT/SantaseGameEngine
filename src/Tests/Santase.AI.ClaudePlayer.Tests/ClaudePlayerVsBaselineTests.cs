namespace Santase.AI.ClaudePlayer.Tests
{
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    /// <summary>
    /// Fast regression check. Runs the current <see cref="ClaudePlayer"/> head-to-head against
    /// the frozen <see cref="ClaudePlayerBaseline"/>. Fails if the current code regresses
    /// noticeably below the baseline - i.e., if an "improvement" actually made things worse.
    /// </summary>
    public class ClaudePlayerVsBaselineTests
    {
        [Fact]
        public void ClaudePlayerShouldNotRegressNoticeablyBelowBaseline()
        {
            // Note: when ClaudePlayer.cs and ClaudePlayerBaseline.cs are identical (right after
            // a baseline refresh), the expected win-count is ~1000/2000 (50%) with sigma ~22, so
            // the 46% threshold sits ~3.6 sigma below parity: false-failure odds ~0.02% per run
            // (the old 1000-game/47% version flaked ~3% of runs at parity). A change that loses
            // by more than this is almost certainly a regression - the ab simulator suite, not
            // this tripwire, is the precise measure.
            const int GamesToPlay = 2000;
            const int RegressionThreshold = 920;

            IPlayer current = new ClaudePlayer();
            IPlayer baseline = new ClaudePlayerBaseline();
            var game = new SantaseGame(current, baseline);

            var currentWins = 0;
            for (var i = 0; i < GamesToPlay; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                if (winner == PlayerPosition.FirstPlayer)
                {
                    currentWins++;
                }
            }

            Assert.True(
                currentWins >= RegressionThreshold,
                $"ClaudePlayer won {currentWins}/{GamesToPlay} vs baseline; "
                + $"threshold for non-regression is {RegressionThreshold} ({RegressionThreshold * 100.0 / GamesToPlay:0.0}%).");
        }
    }
}
