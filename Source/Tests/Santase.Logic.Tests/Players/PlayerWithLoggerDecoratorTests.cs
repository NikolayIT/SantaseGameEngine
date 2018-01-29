namespace Santase.Logic.Tests.Players
{
    using System.Collections.Generic;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class PlayerWithLoggerDecoratorTests
    {
        [Fact]
        public void StartGameShouldAddToLoggerAndCallBaseMethod()
        {
            const string OtherPlayerIdentifier = "тест";
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.StartGame(OtherPlayerIdentifier);

            Assert.True(logger.ToString().Length > 0);
            Assert.True(logger.ToString().Contains(OtherPlayerIdentifier));
            playerMock.Verify(x => x.StartGame(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void StartRoundShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            var card = Card.GetCard(CardSuit.Diamond, CardType.King);
            var cards = new List<Card> { card };
            var trumpCard = Card.GetCard(CardSuit.Club, CardType.Ace);
            playerWithLogger.StartRound(cards, trumpCard, 1, 4);

            Assert.True(logger.ToString().Length > 0);
            Assert.True(logger.ToString().Contains(card.ToString()));
            Assert.True(logger.ToString().Contains(trumpCard.ToString()));
            playerMock.Verify(x => x.StartRound(cards, trumpCard, 1, 4), Times.Once());
        }

        [Fact]
        public void AddCardShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.AddCard(Card.GetCard(CardSuit.Club, CardType.Ace));

            Assert.True(logger.ToString().Length > 0);
            playerMock.Verify(x => x.AddCard(It.IsAny<Card>()), Times.Once());
        }

        [Fact]
        public void GetTurnShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.GetTurn(
                new PlayerTurnContext(
                    new StartRoundState(new StateManager()),
                    Card.GetCard(CardSuit.Club, CardType.Ace),
                    0,
                    0,
                    0));

            Assert.True(logger.ToString().Length > 0);
            playerMock.Verify(x => x.GetTurn(It.IsAny<PlayerTurnContext>()), Times.Once());
        }

        [Fact]
        public void EndTurnShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.EndTurn(
                new PlayerTurnContext(
                    new StartRoundState(new StateManager()),
                    Card.GetCard(CardSuit.Club, CardType.Ace),
                    0,
                    0,
                    0));

            Assert.True(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndTurn(It.IsAny<PlayerTurnContext>()), Times.Once());
        }

        [Fact]
        public void EndRoundShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);
            playerWithLogger.EndRound();
            Assert.True(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndRound(), Times.Once());
        }

        [Fact]
        public void EndGameShouldAddToLoggerAndCallBaseMethod()
        {
            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            playerWithLogger.EndGame(true);

            Assert.True(logger.ToString().Length > 0);
            playerMock.Verify(x => x.EndGame(It.IsAny<bool>()), Times.Once());
        }

        [Fact]
        public void NameShouldReturnBaseName()
        {
            const string PlayerName = "тест";

            var logger = new MemoryLogger();
            var playerMock = new Mock<IPlayer>();
            playerMock.SetupGet(x => x.Name).Returns(PlayerName);

            var playerWithLogger = new PlayerWithLoggerDecorator(playerMock.Object, logger);

            Assert.Equal(PlayerName, playerWithLogger.Name);
        }
    }
}
