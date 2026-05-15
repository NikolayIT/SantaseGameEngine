namespace Santase.Logic.Tests.WinnerLogic
{
    using Santase.Logic.WinnerLogic;

    using Xunit;

    public class RoundWinnerPointsPointsLogicTestsForSantase
    {
        [Theory]
        [InlineData(65, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(66, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(67, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(68, 67, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(34, 33, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(61, 59, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(66, 33, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [InlineData(68, 67, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithOnePoint(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.FirstPlayer, result.Winner);
            Assert.Equal(1, result.Points);
        }

        [Theory]
        [InlineData(70, 20, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(66, 32, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(66, 0, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(70, 20, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [InlineData(66, 32, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [InlineData(66, 0, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithTwoPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.FirstPlayer, result.Winner);
            Assert.Equal(2, result.Points);
        }

        [Theory]
        [InlineData(66, 0, PlayerPosition.NoOne, PlayerPosition.SecondPlayer)]
        [InlineData(66, 0, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer)]
        [InlineData(60, 65, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [InlineData(65, 65, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [InlineData(0, 65, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer)]
        public void GetWinnerPointsShouldReturnFirstPlayerAsWinnerWithThreePoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.FirstPlayer, result.Winner);
            Assert.Equal(3, result.Points);
        }

        [Theory]
        [InlineData(33, 65, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(33, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(33, 67, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(67, 68, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(33, 34, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(59, 61, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(33, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [InlineData(67, 68, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithOnePoint(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.SecondPlayer, result.Winner);
            Assert.Equal(1, result.Points);
        }

        [Theory]
        [InlineData(20, 70, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(32, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(0, 66, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(20, 70, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [InlineData(32, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        [InlineData(0, 66, PlayerPosition.SecondPlayer, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithTwoPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.SecondPlayer, result.Winner);
            Assert.Equal(2, result.Points);
        }

        [Theory]
        [InlineData(0, 66, PlayerPosition.NoOne, PlayerPosition.FirstPlayer)]
        [InlineData(0, 66, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer)]
        [InlineData(65, 60, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [InlineData(65, 65, PlayerPosition.FirstPlayer, PlayerPosition.NoOne)]
        [InlineData(65, 0, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer)]
        public void GetWinnerPointsShouldReturnSecondPlayerAsWinnerWithThreePoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.SecondPlayer, result.Winner);
            Assert.Equal(3, result.Points);
        }

        [Theory]
        [InlineData(60, 60, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        [InlineData(80, 80, PlayerPosition.NoOne, PlayerPosition.NoOne)]
        public void GetWinnerPointsShouldReturnDrawResult(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.NoOne, result.Winner);
            Assert.Equal(0, result.Points);
        }

        // 65-55 last trick to first: 75 vs 55 -> 1 pt to first (loser >= 33).
        // 60-60 last trick to first: 70 vs 60 -> 1 pt to first.
        // 60-60 last trick to second: 60 vs 70 -> 1 pt to second.
        // 60-25 last trick to second: pushes second to 35 (past 33) -> 1 pt to first instead of 2.
        [Theory]
        [InlineData(65, 55, PlayerPosition.NoOne, PlayerPosition.NoOne, PlayerPosition.FirstPlayer, PlayerPosition.FirstPlayer, 1)]
        [InlineData(60, 60, PlayerPosition.NoOne, PlayerPosition.NoOne, PlayerPosition.FirstPlayer, PlayerPosition.FirstPlayer, 1)]
        [InlineData(60, 60, PlayerPosition.NoOne, PlayerPosition.NoOne, PlayerPosition.SecondPlayer, PlayerPosition.SecondPlayer, 1)]
        [InlineData(60, 25, PlayerPosition.NoOne, PlayerPosition.NoOne, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer, 1)]
        public void GetWinnerPointsShouldApplyLastTrickBonusWhenGameNotClosed(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner,
            PlayerPosition expectedWinner,
            int expectedPoints)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(expectedWinner, result.Winner);
            Assert.Equal(expectedPoints, result.Points);
        }

        // 70-25 closed by first, last trick to second: bonus suspended, so second stays at 25 (< 33)
        // -> first wins 2 pt. If bonus applied, second would become 35 (>= 33) -> only 1 pt.
        // Mirror with second as closer.
        // 60-60 closed by first: closer failed (< 66) -> second wins 3 pt regardless of bonus.
        [Theory]
        [InlineData(70, 25, PlayerPosition.FirstPlayer, PlayerPosition.NoOne, PlayerPosition.SecondPlayer, PlayerPosition.FirstPlayer, 2)]
        [InlineData(25, 70, PlayerPosition.SecondPlayer, PlayerPosition.NoOne, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer, 2)]
        [InlineData(60, 60, PlayerPosition.FirstPlayer, PlayerPosition.NoOne, PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer, 3)]
        public void GetWinnerPointsShouldNotApplyLastTrickBonusWhenGameClosed(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner,
            PlayerPosition expectedWinner,
            int expectedPoints)
        {
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                firstPlayerPoints,
                secondPlayerPoints,
                gameClosedBy,
                noTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
            Assert.Equal(expectedWinner, result.Winner);
            Assert.Equal(expectedPoints, result.Points);
        }

        [Fact]
        public void GetWinnerPointsShouldYieldDrawWhenScoresStillEqualAfterBonus()
        {
            // 70-60 with last trick to second -> 70-70 after bonus -> draw.
            IRoundWinnerPointsLogic roundWinnerPointsLogic = new RoundWinnerPointsPointsLogic();
            var result = roundWinnerPointsLogic.GetWinnerPoints(
                70,
                60,
                PlayerPosition.NoOne,
                PlayerPosition.NoOne,
                PlayerPosition.SecondPlayer,
                GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.NoOne, result.Winner);
            Assert.Equal(0, result.Points);
        }
    }
}
