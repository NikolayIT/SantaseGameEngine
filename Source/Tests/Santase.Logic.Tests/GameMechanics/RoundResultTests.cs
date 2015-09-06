namespace Santase.Logic.Tests.GameMechanics
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
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

        [Test]
        public void GameClosedByShouldReturnNoOnePlayerWhenNoneOfThePlayersIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.AreEqual(PlayerPosition.NoOne, roundResult.GameClosedBy);
        }

        [Test]
        public void GameClosedByShouldReturnFirstPlayerWhenTheFirstPlayerIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object) { GameCloser = true };
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.AreEqual(PlayerPosition.FirstPlayer, roundResult.GameClosedBy);
        }

        [Test]
        public void GameClosedByShouldReturnSecondPlayerWhenTheSecondPlayerIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object) { GameCloser = true };
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.AreEqual(PlayerPosition.SecondPlayer, roundResult.GameClosedBy);
        }

        [Test]
        public void NoTricksPlayerShouldReturnFirstPlayerWhenOnlySecondPlayerHasTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            secondRoundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ace));

            Assert.AreEqual(PlayerPosition.FirstPlayer, roundResult.NoTricksPlayer);
        }

        [Test]
        public void NoTricksPlayerShouldReturnSecondPlayerWhenOnlyFirstPlayerHasTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            firstRoundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ace));

            Assert.AreEqual(PlayerPosition.SecondPlayer, roundResult.NoTricksPlayer);
        }

        [Test]
        public void NoTricksPlayerShouldReturnNoOnePlayerWhenBothPlayerHaveTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            firstRoundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ace));
            secondRoundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ten));

            Assert.AreEqual(PlayerPosition.NoOne, roundResult.NoTricksPlayer);
        }
    }
}
