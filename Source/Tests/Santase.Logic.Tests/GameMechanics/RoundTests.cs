namespace Santase.Logic.Tests.GameMechanics
{
    using NUnit.Framework;

    using Santase.Logic.GameMechanics;

    // These tests can be improved. When bug is found regression should be added.
    [TestFixture]
    public class RoundTestsForSantase
    {
        [Test]
        public void PlayersShouldReceiveEqualNumberOfCardsAtLeast6AndNoMoreThan12()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play();

            Assert.IsTrue(firstPlayer.AddCardCalledCount == secondPlayer.AddCardCalledCount);

            Assert.IsTrue(firstPlayer.AddCardCalledCount >= 6);
            Assert.IsTrue(secondPlayer.AddCardCalledCount >= 6);

            Assert.IsTrue(firstPlayer.AddCardCalledCount <= 12);
            Assert.IsTrue(secondPlayer.AddCardCalledCount <= 12);
        }

        [Test]
        public void PlayShouldReturnValidRoundResultObject()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            var result = round.Play();

            Assert.IsTrue(result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick);
            Assert.IsTrue(result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0);
            Assert.IsTrue(result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0);
            Assert.IsTrue(
                result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66
                || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120);
        }

        [Test]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int NumberOfRounds = 10000;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            for (var i = 0; i < NumberOfRounds; i++)
            {
                Round round;
                if (i % 2 == 0)
                {
                    round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);
                }
                else
                {
                    round = new Round(secondPlayer, firstPlayer, GameRulesProvider.Santase);
                }

                round.Play();
            }

            Assert.IsTrue(firstPlayer.EndRoundCalledCount == NumberOfRounds);
            Assert.IsTrue(secondPlayer.EndRoundCalledCount == NumberOfRounds);

            Assert.IsTrue(firstPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.IsTrue(firstPlayer.GetTurnWhenSecond > NumberOfRounds);
            Assert.IsTrue(secondPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.IsTrue(secondPlayer.GetTurnWhenSecond > NumberOfRounds);

            Assert.IsTrue(firstPlayer.AddCardCalledCount >= firstPlayer.GetTurnCalledCount);
            Assert.IsTrue(secondPlayer.AddCardCalledCount >= secondPlayer.GetTurnCalledCount);

            Assert.IsTrue(firstPlayer.GetTurnWhenFirst >= secondPlayer.GetTurnWhenSecond);
            Assert.IsTrue(secondPlayer.GetTurnWhenFirst >= firstPlayer.GetTurnWhenSecond);
        }
    }
}
