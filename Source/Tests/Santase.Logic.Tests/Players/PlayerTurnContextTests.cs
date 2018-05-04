namespace Santase.Logic.Tests.Players
{
    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class PlayerTurnContextTests
    {
        [Fact]
        public void ConstructorShouldSetProperties()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new FinalRoundState(haveStateMock.Object);
            var trumpCard = Card.GetCard(CardSuit.Heart, CardType.Ten);
            const int CardsLeftInDeck = 10;

            var playerTurnContext = new PlayerTurnContext(state, trumpCard, CardsLeftInDeck, 0, 0);

            Assert.Equal(state, playerTurnContext.State);
            Assert.Equal(trumpCard, playerTurnContext.TrumpCard);
            Assert.Equal(CardsLeftInDeck, playerTurnContext.CardsLeftInDeck);
        }

        [Fact]
        public void FirstPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = Card.GetCard(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayedCard = card };
            Assert.Equal(card, playerTurnContext.FirstPlayedCard);
        }

        [Fact]
        public void FirstPlayerAnnouncePropertyShouldWorkCorrectly()
        {
            const Announce Announce = Announce.Twenty;
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayerAnnounce = Announce };
            Assert.Equal(Announce, playerTurnContext.FirstPlayerAnnounce);
        }

        [Fact]
        public void SecondPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = Card.GetCard(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { SecondPlayedCard = card };
            Assert.Equal(card, playerTurnContext.SecondPlayedCard);
        }

        [Fact]
        public void AmITheFirstPlayerShouldReturnTrueWhenCardIsNotPlayedYet()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0);
            Assert.True(playerTurnContext.IsFirstPlayerTurn);
        }

        [Fact]
        public void AmITheFirstPlayerShouldReturnFalseWhenCardIsAlreadyPlayed()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayedCard = Card.GetCard(CardSuit.Diamond, CardType.Ten) };
            Assert.False(playerTurnContext.IsFirstPlayerTurn);
        }

        [Fact]
        public void CloneShouldReturnDifferentReference()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new StartRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, Card.GetCard(CardSuit.Heart, CardType.Queen), 12, 0, 0);
            var clonedPlayerTurnContext = playerTurnContext.DeepClone();
            Assert.NotSame(playerTurnContext, clonedPlayerTurnContext);
        }

        [Fact]
        public void CloneShouldReturnObjectOfTypePlayerTurnContext()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new TwoCardsLeftRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, Card.GetCard(CardSuit.Diamond, CardType.Ace), 2, 0, 0);
            var clonedPlayerTurnContext = playerTurnContext.DeepClone();
            Assert.IsType<PlayerTurnContext>(clonedPlayerTurnContext);
        }

        [Fact]
        public void CloneShouldReturnObjectWithTheSameProperties()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new StartRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, Card.GetCard(CardSuit.Club, CardType.Ten), 12, 0, 0)
                                        {
                                            FirstPlayedCard = Card.GetCard(CardSuit.Spade, CardType.King),
                                            FirstPlayerAnnounce = Announce.Forty,
                                            SecondPlayedCard = Card.GetCard(CardSuit.Heart, CardType.Nine),
                                        };

            var clonedPlayerTurnContext = playerTurnContext.DeepClone();

            Assert.NotNull(clonedPlayerTurnContext);
            Assert.Same(playerTurnContext.State, clonedPlayerTurnContext.State);
            Assert.Equal(playerTurnContext.CardsLeftInDeck, clonedPlayerTurnContext.CardsLeftInDeck);
            Assert.Equal(playerTurnContext.FirstPlayedCard, clonedPlayerTurnContext.FirstPlayedCard);
            Assert.Equal(playerTurnContext.FirstPlayerAnnounce, clonedPlayerTurnContext.FirstPlayerAnnounce);
            Assert.Equal(playerTurnContext.SecondPlayedCard, clonedPlayerTurnContext.SecondPlayedCard);
            Assert.Equal(playerTurnContext.TrumpCard, clonedPlayerTurnContext.TrumpCard);
        }
    }
}
