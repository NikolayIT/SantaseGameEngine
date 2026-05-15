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
            // Note: when ClaudePlayer.cs and ClaudePlayerBaseline.cs are identical, the expected
            // win-count is ~500/1000 (50%). The threshold of 470 (47%) allows ~5% noise + small
            // natural variance from the random shuffle. A change that loses by more than this is
            // almost certainly a regression.
            const int GamesToPlay = 1000;
            const int RegressionThreshold = 470;

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
