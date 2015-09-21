namespace Santase.AI.SmartPlayer.Tests
{
    using System;

    using NUnit.Framework;

    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;

    [TestFixture]
    public class SmartPlayerVsDummyBotTests
    {
        [Test]
        public void SmartPlayerShouldWinAtLeastHalfOfTheGamesVsDummy()
        {
            var smartPlayerWins = this.SimulateGamesAndGetSmartPlayerWins(100);
            Assert.Greater(smartPlayerWins, 50);
        }

        [Test]
        public void SmartPlayerShouldWinIn99PercentOfTheGamesVsDummy()
        {
            const int GamesToPlay = 4000;
            var smartPlayerWins = this.SimulateGamesAndGetSmartPlayerWins(GamesToPlay);
            Console.WriteLine(smartPlayerWins);
            Assert.GreaterOrEqual(smartPlayerWins, 0.99 * GamesToPlay);
        }

        private int SimulateGamesAndGetSmartPlayerWins(int gamesToSimulate)
        {
            var smartPlayer = new SmartPlayer();
            var smartPlayerWins = 0;

            var dummyPlayer = new DummyPlayer();

            var game = new SantaseGame(smartPlayer, dummyPlayer);

            for (var i = 0; i < gamesToSimulate; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                if (winner == PlayerPosition.FirstPlayer)
                {
                    smartPlayerWins++;
                }
            }

            // Console.WriteLine(smartPlayerWins);
            return smartPlayerWins;
        }
    }
}
