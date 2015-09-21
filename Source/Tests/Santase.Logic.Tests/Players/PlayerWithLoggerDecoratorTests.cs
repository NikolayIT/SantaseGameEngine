namespace Santase.Logic.Tests.Players
{
    using System.Collections.Generic;

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
        public void StartGameShouldAddToLoggerAndCallBaseMethod()
        {
            const string OtherPlayerIdentifier = "тест";
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.StartGame(OtherPlayerIdentifier);

            Assert.IsTrue(logger.ToString().Length > 0);
            Assert.IsTrue(logger.ToString().Contains(OtherPlayerIdentifier));
            playerMock.Verify(x => x.StartGame(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void StartRoundShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            var card = new Card(CardSuit.Diamond, CardType.King);
            var cards = new List<Card> { card };
            var trumpCard = new Card(CardSuit.Club, CardType.Ace);
            playerWithLogger.StartRound(cards, trumpCard, 1, 4);

            Assert.IsTrue(logger.ToString().Length > 0);
            Assert.IsTrue(logger.ToString().Contains(card.ToString()));
            Assert.IsTrue(logger.ToString().Contains(trumpCard.ToString()));
            playerMock.Verify(x => x.StartRound(cards, trumpCard, 1, 4), Times.Once());
        }

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

            playerWithLogger.GetTurn(
                new PlayerTurnContext(
                    new StartRoundState(new StateManager()),
                    new Card(CardSuit.Club, CardType.Ace),
                    0,
                    0,
                    0));

            Assert.IsTrue(logger.ToString().Length > 0);
            playerMock.Verify(x => x.GetTurn(It.IsAny<PlayerTurnContext>()), Times.Once());
        }

        [Test]
        public void EndTurnShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.EndTurn(
                new PlayerTurnContext(
                    new StartRoundState(new StateManager()),
                    new Card(CardSuit.Club, CardType.Ace),
                    0,
                    0,
                    0));

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
