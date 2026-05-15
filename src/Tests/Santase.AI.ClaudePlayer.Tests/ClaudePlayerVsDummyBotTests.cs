namespace Santase.AI.ClaudePlayer.Tests
{
    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;

    using Xunit;

    public class ClaudePlayerVsDummyBotTests
    {
        [Fact]
        public void ClaudePlayerShouldWinAtLeastHalfOfTheGamesVsDummy()
        {
            var wins = SimulateAndCountFirstPlayerWins(new ClaudePlayer(), new DummyPlayer(), 100);
            Assert.True(wins > 50, $"ClaudePlayer won {wins}/100 vs DummyPlayer; expected > 50");
        }

        [Fact]
        public void ClaudePlayerShouldWinIn99PercentOfTheGamesVsDummy()
        {
            const int GamesToPlay = 4000;
            var wins = SimulateAndCountFirstPlayerWins(new ClaudePlayer(), new DummyPlayer(), GamesToPlay);
            Assert.True(wins >= 0.99 * GamesToPlay, $"ClaudePlayer won {wins}/{GamesToPlay} vs DummyPlayer; expected >= 99%");
        }

        [Fact]
        public void ClaudePlayerShouldWinAtLeastHalfOfTheGamesVsDummyChangingTrump()
        {
            var wins = SimulateAndCountFirstPlayerWins(new ClaudePlayer(), new DummyPlayerChangingTrump(), 100);
            Assert.True(wins > 50, $"ClaudePlayer won {wins}/100 vs DummyPlayerChangingTrump; expected > 50");
        }

        private static int SimulateAndCountFirstPlayerWins(
            Logic.Players.IPlayer firstPlayer,
            Logic.Players.IPlayer secondPlayer,
            int gamesToSimulate)
        {
            var firstPlayerWins = 0;
            var game = new SantaseGame(firstPlayer, secondPlayer);

            for (var i = 0; i < gamesToSimulate; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                if (winner == PlayerPosition.FirstPlayer)
                {
                    firstPlayerWins++;
                }
            }

            return firstPlayerWins;
        }
    }
}
