namespace Santase.Logic.Tests.GameMechanics
{
    using System;

    using NUnit.Framework;

    using Santase.Logic.GameMechanics;

    [TestFixture]
    public class SantaseGameTests
    {
        [Test]
        public void StartGameShouldReturnOneOfThePlayersAsWinner()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var game = new SantaseGame(firstPlayer, secondPlayer);
            var winner = game.Start();
            Assert.IsTrue(winner != PlayerPosition.NoOne);
        }

        [Test]
        public void WinnersShouldBeEquallyDistributed()
        {
            const int GamesToPlay = 500;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            var firstPlayerWinner = 0;
            var secondPlayerWinner = 0;

            for (var i = 0; i < GamesToPlay; i++)
            {
                var firstToPlay = i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer;
                var game = new SantaseGame(firstPlayer, secondPlayer, firstToPlay);
                var winner = game.Start();
                if (winner == PlayerPosition.FirstPlayer)
                {
                    firstPlayerWinner++;
                }
                else if (winner == PlayerPosition.SecondPlayer)
                {
                    secondPlayerWinner++;
                }
            }

            Assert.AreEqual(GamesToPlay, firstPlayerWinner + secondPlayerWinner);
            Assert.IsTrue(Math.Abs(firstPlayerWinner - secondPlayerWinner) < 150);
        }

        [Test]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int GamesToPlay = 500;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            for (var i = 0; i < GamesToPlay; i++)
            {
                var firstToPlay = i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer;
                var game = new SantaseGame(firstPlayer, secondPlayer, firstToPlay);
                game.Start();
            }

            Assert.AreEqual(GamesToPlay, firstPlayer.StartGameCalledCount);
            Assert.AreEqual(GamesToPlay, secondPlayer.StartGameCalledCount);

            Assert.AreEqual(GamesToPlay, firstPlayer.EndGameCalledCount);
            Assert.AreEqual(GamesToPlay, secondPlayer.EndGameCalledCount);

            Assert.IsTrue(firstPlayer.AddCardCalledCount >= 3 * 6 * GamesToPlay);
            Assert.IsTrue(secondPlayer.AddCardCalledCount >= 3 * 6 * GamesToPlay);

            Assert.IsTrue(firstPlayer.EndRoundCalledCount > GamesToPlay * 2);
            Assert.IsTrue(firstPlayer.GetTurnWhenFirst > GamesToPlay * 10);
            Assert.IsTrue(firstPlayer.GetTurnWhenSecond > GamesToPlay * 10);
            Assert.IsTrue(firstPlayer.EndTurnCalledCount > GamesToPlay * 10);

            Assert.IsTrue(secondPlayer.EndRoundCalledCount > GamesToPlay * 2);
            Assert.IsTrue(secondPlayer.GetTurnWhenFirst > GamesToPlay * 10);
            Assert.IsTrue(secondPlayer.GetTurnWhenSecond > GamesToPlay * 10);
            Assert.IsTrue(secondPlayer.EndTurnCalledCount > GamesToPlay * 10);
        }
    }
}
