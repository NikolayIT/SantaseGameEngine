namespace Santase.Logic.Tests.GameMechanics
{
    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class RoundResultTests
    {
        [Fact]
        public void ConstructorShouldSetProperties()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.Equal(firstRoundPlayerInfo, roundResult.FirstPlayer);
            Assert.Equal(secondRoundPlayerInfo, roundResult.SecondPlayer);
        }

        [Fact]
        public void GameClosedByShouldReturnNoOnePlayerWhenNoneOfThePlayersIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.Equal(PlayerPosition.NoOne, roundResult.GameClosedBy);
        }

        [Fact]
        public void GameClosedByShouldReturnFirstPlayerWhenTheFirstPlayerIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object) { GameCloser = true };
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.Equal(PlayerPosition.FirstPlayer, roundResult.GameClosedBy);
        }

        [Fact]
        public void GameClosedByShouldReturnSecondPlayerWhenTheSecondPlayerIsGameCloser()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object) { GameCloser = true };
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);
            Assert.Equal(PlayerPosition.SecondPlayer, roundResult.GameClosedBy);
        }

        [Fact]
        public void NoTricksPlayerShouldReturnFirstPlayerWhenOnlySecondPlayerHasTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            secondRoundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ace));

            Assert.Equal(PlayerPosition.FirstPlayer, roundResult.NoTricksPlayer);
        }

        [Fact]
        public void NoTricksPlayerShouldReturnSecondPlayerWhenOnlyFirstPlayerHasTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            firstRoundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ace));

            Assert.Equal(PlayerPosition.SecondPlayer, roundResult.NoTricksPlayer);
        }

        [Fact]
        public void NoTricksPlayerShouldReturnNoOnePlayerWhenBothPlayerHaveTricks()
        {
            var firstRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var secondRoundPlayerInfo = new RoundPlayerInfo(new Mock<IPlayer>().Object);
            var roundResult = new RoundResult(firstRoundPlayerInfo, secondRoundPlayerInfo);

            firstRoundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ace));
            secondRoundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ten));

            Assert.Equal(PlayerPosition.NoOne, roundResult.NoTricksPlayer);
        }
    }
}
