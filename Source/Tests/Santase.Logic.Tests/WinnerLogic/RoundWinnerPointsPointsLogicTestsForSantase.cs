namespace Santase.Logic.Tests.WinnerLogic
{
    using NUnit.Framework;

    using Santase.Logic.WinnerLogic;

    [TestFixture]
    public class RoundWinnerPointsPointsLogicTestsForSantase
    {
        [TestCase(65, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(66, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(67, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(68, 67, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(34, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(61, 59, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(66, 33, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [TestCase(68, 67, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithOnePoint(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.FirstPlayer, result.Winner);
            Assert.AreEqual(1, result.Points);
        }

        [TestCase(70, 20, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(66, 32, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(66, 0, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(70, 20, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [TestCase(66, 32, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [TestCase(66, 0, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithTwoPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.FirstPlayer, result.Winner);
            Assert.AreEqual(2, result.Points);
        }

        [TestCase(66, 0, PlayerPosition.NoOne, PlayerPosition.SecondPlayer)]
        [TestCase(66, 0, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer)]
        [TestCase(60, 65, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [TestCase(65, 65, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [TestCase(0, 65, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithThreePoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.FirstPlayer, result.Winner);
            Assert.AreEqual(3, result.Points);
        }

        [TestCase(33, 65, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(33, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(33, 67, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(67, 68, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(33, 34, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(59, 61, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(33, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [TestCase(67, 68, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithOnePoint(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.SecondPlayer, result.Winner);
            Assert.AreEqual(1, result.Points);
        }

        [TestCase(20, 70, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(32, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(0, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(20, 70, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [TestCase(32, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [TestCase(0, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithTwoPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.SecondPlayer, result.Winner);
            Assert.AreEqual(2, result.Points);
        }

        [TestCase(0, 66, PlayerPosition.NoOne, PlayerPosition.FirstPlayer)]
        [TestCase(0, 66, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer)]
        [TestCase(65, 60, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [TestCase(65, 65, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [TestCase(65, 0, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithThreePoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.SecondPlayer, result.Winner);
            Assert.AreEqual(3, result.Points);
        }

        [TestCase(60, 60, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [TestCase(80, 80, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnDrawResult(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                GameRulesProvider.Santase);
            Assert.AreEqual(PlayerPosition.NoOne, result.Winner);
            Assert.AreEqual(0, result.Points);
        }
    }
}
