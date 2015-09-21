namespace Santase.Logic.Tests.GameMechanics
{
    using NUnit.Framework;

    using Santase.Logic.GameMechanics;

    [TestFixture]
    public class RoundTestsForSantase
    {
        [Test]
        public void PlayersStartRoundAndEndRoundShouldBeCalledAndShouldReceiveEqualNumberOfCards()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play(0, 0);

            Assert.AreEqual(firstPlayer.AddCardCalledCount, secondPlayer.AddCardCalledCount);

            Assert.AreEqual(firstPlayer.StartRoundCalledCount, 1);
            Assert.AreEqual(secondPlayer.StartRoundCalledCount, 1);

            Assert.AreEqual(firstPlayer.EndRoundCalledCount, 1);
            Assert.AreEqual(secondPlayer.EndRoundCalledCount, 1);

            Assert.GreaterOrEqual(firstPlayer.AddCardCalledCount, 2);
            Assert.GreaterOrEqual(secondPlayer.AddCardCalledCount, 2);
            Assert.LessOrEqual(firstPlayer.AddCardCalledCount, 6);
            Assert.LessOrEqual(secondPlayer.AddCardCalledCount, 6);
        }

        [Test]
        public void PlayersStartRoundShouldBeCalledWithCorrectScoreValues()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play(9, 4);

            Assert.AreEqual(firstPlayer.MyTotalPoints, 9);
            Assert.AreEqual(firstPlayer.OpponentTotalPoints, 4);
            Assert.AreEqual(secondPlayer.MyTotalPoints, 4);
            Assert.AreEqual(secondPlayer.OpponentTotalPoints, 9);
        }

        [Test]
        public void PlayShouldReturnValidRoundResultObject()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            var result = round.Play(0, 0);

            Assert.IsTrue(
                result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick,
                "result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick");

            Assert.IsTrue(
                result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0,
                "result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0");

            Assert.IsTrue(
                result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0,
                "result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0");

            Assert.IsTrue(
                result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66
                || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120,
                "result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66 || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120");
        }

        [Test]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int NumberOfRounds = 10000;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            for (var i = 0; i < NumberOfRounds; i++)
            {
                var round = i % 2 == 0
                                ? new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase)
                                : new Round(secondPlayer, firstPlayer, GameRulesProvider.Santase);

                round.Play(0, 0);
            }

            Assert.AreEqual(firstPlayer.StartRoundCalledCount, NumberOfRounds);
            Assert.AreEqual(secondPlayer.StartRoundCalledCount, NumberOfRounds);

            Assert.AreEqual(firstPlayer.EndRoundCalledCount, NumberOfRounds);
            Assert.AreEqual(secondPlayer.EndRoundCalledCount, NumberOfRounds);

            Assert.Greater(firstPlayer.GetTurnWhenFirst, NumberOfRounds);
            Assert.Greater(firstPlayer.GetTurnWhenSecond, NumberOfRounds);
            Assert.Greater(secondPlayer.GetTurnWhenFirst, NumberOfRounds);
            Assert.Greater(secondPlayer.GetTurnWhenSecond, NumberOfRounds);

            Assert.GreaterOrEqual(firstPlayer.GetTurnWhenFirst, secondPlayer.GetTurnWhenSecond);
            Assert.GreaterOrEqual(secondPlayer.GetTurnWhenFirst, firstPlayer.GetTurnWhenSecond);
        }
    }
}
