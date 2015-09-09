namespace Santase.Logic.Tests.Players
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class PlayerWithLoggerDecoratorTests
    {
        [Test]
        public void AddCardShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.AddCard(new Card(CardSuit.Club, CardType.Ace));
            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.AddCard(It.IsAny<Card>()), Times.Once());
        }

        [Test]
        public void GetTurnShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.GetTurn(new PlayerTurnContext(new StartRoundState(new StateManager()), new Card(CardSuit.Club, CardType.Ace), 0, 0, 0));
            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.GetTurn(It.IsAny<PlayerTurnContext>()), Times.Once());
        }

        [Test]
        public void EndTurnShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.EndTurn(new PlayerTurnContext(new StartRoundState(new StateManager()), new Card(CardSuit.Club, CardType.Ace), 0, 0, 0));
            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndTurn(It.IsAny<PlayerTurnContext>()), Times.Once());
        }

        [Test]
        public void EndRoundShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.EndRound();
            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndRound(), Times.Once());
        }

        [Test]
        public void EndGameShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.EndGame(true);
            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndGame(It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void NameShouldReturnBaseName()
        {
            const string PlayerName = "тест";

            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            playerMock.SetupGet(x => x.Name).Returns(PlayerName);

            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            Assert.AreEqual(PlayerName, playerWithLogger.Name);
        }
    }
}
