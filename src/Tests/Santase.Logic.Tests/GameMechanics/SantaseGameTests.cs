namespace Santase.Logic.Tests.GameMechanics
{
    using System;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

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

        [Theory]
        [InlineData(PlayerPosition.FirstPlayer)]
        [InlineData(PlayerPosition.SecondPlayer)]
        public void DrawnRoundShouldNotAwardPointsAndShouldKeepTheSameOpener(PlayerPosition openerBefore)
        {
            // Spec: equal final round-points => no winner, no game points awarded,
            // and the opener of the next round must be the same as the drawn round.
            //
            // Deterministic setup: build a RoundResult directly (no shuffle / no random
            // play). Equal RoundPoints + gameClosedBy=NoOne + lastTrickWinner=NoOne is
            // exactly the input shape RoundWinnerPointsPointsLogic resolves to Draw.
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var game = new SantaseGame(firstPlayer, secondPlayer);
            game.FirstToPlay = openerBefore;

            var drawnRound = BuildRoundResult(firstPlayer, secondPlayer, firstPlayerCardPoints: 60, secondPlayerCardPoints: 60);

            game.UpdatePoints(drawnRound);

            Assert.Equal(0, game.FirstPlayerTotalPoints);
            Assert.Equal(0, game.SecondPlayerTotalPoints);
            Assert.Equal(openerBefore, game.FirstToPlay);
        }

        [Theory]
        [InlineData(66, 33, 1, 0, PlayerPosition.SecondPlayer)]
        [InlineData(33, 66, 0, 1, PlayerPosition.FirstPlayer)]
        public void NonDrawnRoundShouldSwitchOpenerToTheLoser(
            int firstPlayerCardPoints,
            int secondPlayerCardPoints,
            int expectedFirstPlayerTotal,
            int expectedSecondPlayerTotal,
            PlayerPosition expectedOpenerAfter)
        {
            // Contrast / sanity: without this, the draw test could pass for the wrong
            // reason (e.g. UpdatePoints silently never touching FirstToPlay). A 66-vs-33
            // round (loser ≥ 33, both have tricks) is a clean 1-pt win, and the opener
            // must flip to the LOSER for the next round — regardless of who opened it.
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var game = new SantaseGame(firstPlayer, secondPlayer);
            game.FirstToPlay = PlayerPosition.FirstPlayer;

            var wonRound = BuildRoundResult(firstPlayer, secondPlayer, firstPlayerCardPoints, secondPlayerCardPoints);

            game.UpdatePoints(wonRound);

            Assert.Equal(expectedFirstPlayerTotal, game.FirstPlayerTotalPoints);
            Assert.Equal(expectedSecondPlayerTotal, game.SecondPlayerTotalPoints);
            Assert.Equal(expectedOpenerAfter, game.FirstToPlay);
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

        private static RoundResult BuildRoundResult(
            IPlayer firstPlayer,
            IPlayer secondPlayer,
            int firstPlayerCardPoints,
            int secondPlayerCardPoints)
        {
            var firstInfo = new RoundPlayerInfo(firstPlayer);
            var secondInfo = new RoundPlayerInfo(secondPlayer);
            AddTrickCardsWorth(firstInfo, firstPlayerCardPoints);
            AddTrickCardsWorth(secondInfo, secondPlayerCardPoints);
            Assert.Equal(firstPlayerCardPoints, firstInfo.RoundPoints);
            Assert.Equal(secondPlayerCardPoints, secondInfo.RoundPoints);
            return new RoundResult(firstInfo, secondInfo, PlayerPosition.NoOne);
        }

        private static void AddTrickCardsWorth(RoundPlayerInfo info, int targetPoints)
        {
            // Card values: A=11, 10=10, K=4, Q=3, J=2, 9=0. Assemble enough distinct cards
            // (within one CardCollection) to reach exactly the target. Two players in a
            // test may share the same card identities — RoundPlayerInfo.TrickCards is a
            // per-player CardCollection, so there is no cross-player conflict.
            var typesByValueDesc = new[]
            {
                CardType.Ace, CardType.Ten, CardType.King, CardType.Queen, CardType.Jack,
            };

            var remaining = targetPoints;
            foreach (var type in typesByValueDesc)
            {
                foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                {
                    var card = Card.GetCard(suit, type);
                    if (remaining < card.GetValue())
                    {
                        break;
                    }

                    info.WinCard(card);
                    remaining -= card.GetValue();
                }

                if (remaining == 0)
                {
                    break;
                }
            }

            Assert.Equal(0, remaining);
        }
    }
}
