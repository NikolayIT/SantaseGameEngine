namespace Santase.Logic.Tests.Players
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class PlayerTurnContextTests
    {
        [Test]
        public static void ConstructorShouldSetProperties()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new FinalRoundState(haveStateMock.Object);
            var trumpCard = new Card(CardSuit.Heart, CardType.Ten);
            const int CardsLeftInDeck = 10;

            var playerTurnContext = new PlayerTurnContext(state, trumpCard, CardsLeftInDeck);

            Assert.AreEqual(state, playerTurnContext.State);
            Assert.AreEqual(trumpCard, playerTurnContext.TrumpCard);
            Assert.AreEqual(CardsLeftInDeck, playerTurnContext.CardsLeftInDeck);
        }

        [Test]
        public static void FirstPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = new Card(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0) { FirstPlayedCard = card };
            Assert.AreEqual(card, playerTurnContext.FirstPlayedCard);
        }

        [Test]
        public static void SecondPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = new Card(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0) { SecondPlayedCard = card };
            Assert.AreEqual(card, playerTurnContext.SecondPlayedCard);
        }

        [Test]
        public static void AmITheFirstPlayerShouldReturnTrueWhenCardIsNotPlayedYet()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0);
            Assert.IsTrue(playerTurnContext.IsFirstPlayerTurn);
        }

        [Test]
        public static void AmITheFirstPlayerShouldReturnFalseWhenCardIsAlreadyPlayed()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0) { FirstPlayedCard = new Card(CardSuit.Diamond, CardType.Ten) };
            Assert.IsFalse(playerTurnContext.IsFirstPlayerTurn);
        }
    }
}
