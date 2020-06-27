namespace Santase.Logic.Tests.GameMechanics
{
    using System;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;

    using Xunit;

    public class SantaseGameTests
    {
        [Fact]
        public void StartGameShouldReturnOneOfThePlayersAsWinner()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var game = new SantaseGame(firstPlayer, secondPlayer);
            var winner = game.Start();
            Assert.True(winner != PlayerPosition.NoOne);
        }

        [Fact]
        public void WinnersShouldBeEquallyDistributed()
        {
            const int GamesToPlay = 200;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            var firstPlayerWinner = 0;
            var secondPlayerWinner = 0;

            for (var i = 0; i < GamesToPlay; i++)
            {
                var firstToPlay = i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer;
                var game = new SantaseGame(firstPlayer, secondPlayer);
                var winner = game.Start(firstToPlay);
                if (winner == PlayerPosition.FirstPlayer)
                {
                    firstPlayerWinner++;
                }
                else if (winner == PlayerPosition.SecondPlayer)
                {
                    secondPlayerWinner++;
                }
            }

            Assert.Equal(GamesToPlay, firstPlayerWinner + secondPlayerWinner);
            Assert.True(Math.Abs(firstPlayerWinner - secondPlayerWinner) < 150);
        }

        [Fact]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int GamesToPlay = 200;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            for (var i = 0; i < GamesToPlay; i++)
            {
                var firstToPlay = i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer;
                var game = new SantaseGame(firstPlayer, secondPlayer);
                game.Start(firstToPlay);
            }

            // StartGame()
            Assert.Equal(GamesToPlay, firstPlayer.StartGameCalledCount);
            Assert.Equal(GamesToPlay, secondPlayer.StartGameCalledCount);

            // EndGame()
            Assert.Equal(GamesToPlay, firstPlayer.EndGameCalledCount);
            Assert.Equal(GamesToPlay, secondPlayer.EndGameCalledCount);

            // StartRound()
            Assert.True(
                firstPlayer.StartRoundCalledCount >= 4 * GamesToPlay,
                "Not started at least 4 rounds per game for the first player");
            Assert.True(
                secondPlayer.StartRoundCalledCount >= 4 * GamesToPlay,
                "Not started at least 4 rounds per game for the second player");

            // EndRound()
            Assert.True(
                firstPlayer.EndRoundCalledCount >= GamesToPlay * 4,
                "Not ended at least 4 rounds per game for the first player");
            Assert.True(
                secondPlayer.EndRoundCalledCount >= GamesToPlay * 4,
                "Not ended at least 4 rounds per game for the second player");

            // GetTurn() and EndTurn()
            Assert.True(firstPlayer.GetTurnWhenFirst > GamesToPlay * 10);
            Assert.True(firstPlayer.GetTurnWhenSecond > GamesToPlay * 10);
            Assert.True(firstPlayer.EndTurnCalledCount > GamesToPlay * 10);

            Assert.True(secondPlayer.GetTurnWhenFirst > GamesToPlay * 10);
            Assert.True(secondPlayer.GetTurnWhenSecond > GamesToPlay * 10);
            Assert.True(secondPlayer.EndTurnCalledCount > GamesToPlay * 10);
        }

        [Fact]
        public void StartingGameShouldRestartTheGameToReuseGameInstance()
        {
            const int GamesToPlay = 20;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var game = new SantaseGame(firstPlayer, secondPlayer, GameRulesProvider.Santase, new NoLogger());

            for (var i = 0; i < GamesToPlay; i++)
            {
                game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
            }

            Assert.True(
                firstPlayer.StartRoundCalledCount >= 4 * GamesToPlay,
                "Not started at least 4 rounds per game for the first player");
            Assert.True(
                secondPlayer.StartRoundCalledCount >= 4 * GamesToPlay,
                "Not started at least 4 rounds per game for the second player");
        }
    }
}
