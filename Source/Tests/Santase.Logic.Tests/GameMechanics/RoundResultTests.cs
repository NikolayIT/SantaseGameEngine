namespace Santase.Logic.Tests.GameMechanics
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    [TestFixture]
    public class RoundResultTests
    {
        [Test]
        public void ConstructorShouldSetProperties()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.AreEqual(firstRoundPlayerInfo, roundResult.FirstPlayer);
            Assert.AreEqual(secondRoundPlayerInfo, roundResult.SecondPlayer);
        }
    }
}
