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
        public void ConstructorShouldSetProperties()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new FinalRoundState(haveStateMock.Object);
            var trumpCard = new Card(CardSuit.Heart, CardType.Ten);
            const int CardsLeftInDeck = 10;

            var playerTurnContext = new PlayerTurnContext(state, trumpCard, CardsLeftInDeck, 0, 0);

            Assert.AreEqual(state, playerTurnContext.State);
            Assert.AreEqual(trumpCard, playerTurnContext.TrumpCard);
            Assert.AreEqual(CardsLeftInDeck, playerTurnContext.CardsLeftInDeck);
        }

        [Test]
        public void FirstPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = new Card(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayedCard = card };
            Assert.AreEqual(card, playerTurnContext.FirstPlayedCard);
        }

        [Test]
        public void FirstPlayerAnnouncePropertyShouldWorkCorrectly()
        {
            const Announce Announce = Announce.Twenty;
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayerAnnounce = Announce };
            Assert.AreEqual(Announce, playerTurnContext.FirstPlayerAnnounce);
        }

        [Test]
        public void SecondPlayedCardPropertyShouldWorkCorrectly()
        {
            var haveStateMock = new Mock<IStateManager>();
            var card = new Card(CardSuit.Spade, CardType.Jack);
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { SecondPlayedCard = card };
            Assert.AreEqual(card, playerTurnContext.SecondPlayedCard);
        }

        [Test]
        public void AmITheFirstPlayerShouldReturnTrueWhenCardIsNotPlayedYet()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0,
                0,
                0);
            Assert.IsTrue(playerTurnContext.IsFirstPlayerTurn);
        }

        [Test]
        public void AmITheFirstPlayerShouldReturnFalseWhenCardIsAlreadyPlayed()
        {
            var haveStateMock = new Mock<IStateManager>();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(haveStateMock.Object),
                new Card(CardSuit.Club, CardType.Ace),
                0,
                0,
                0) { FirstPlayedCard = new Card(CardSuit.Diamond, CardType.Ten) };
            Assert.IsFalse(playerTurnContext.IsFirstPlayerTurn);
        }

        [Test]
        public void CloneShouldReturnDifferentReference()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new StartRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, new Card(CardSuit.Heart, CardType.Queen), 12, 0, 0);
            var clonedPlayerTurnContext = playerTurnContext.DeepClone();
            Assert.AreNotSame(playerTurnContext, clonedPlayerTurnContext);
        }

        [Test]
        public void CloneShouldReturnObjectOfTypePlayerTurnContext()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new TwoCardsLeftRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, new Card(CardSuit.Diamond, CardType.Ace), 2, 0, 0);
            var clonedPlayerTurnContext = playerTurnContext.DeepClone();
            Assert.IsInstanceOf<PlayerTurnContext>(clonedPlayerTurnContext);
        }

        [Test]
        public void CloneShouldReturnObjectWithTheSameProperties()
        {
            var haveStateMock = new Mock<IStateManager>();
            var state = new StartRoundState(haveStateMock.Object);
            var playerTurnContext = new PlayerTurnContext(state, new Card(CardSuit.Club, CardType.Ten), 12, 0, 0)
                                        {
                                            FirstPlayedCard = new Card(CardSuit.Spade, CardType.King),
                                            FirstPlayerAnnounce = Announce.Forty,
                                            SecondPlayedCard = new Card(CardSuit.Heart, CardType.Nine)
                                        };

            var clonedPlayerTurnContext = playerTurnContext.DeepClone();

            Assert.IsNotNull(clonedPlayerTurnContext);
            Assert.AreSame(playerTurnContext.State, clonedPlayerTurnContext.State);
            Assert.AreEqual(playerTurnContext.CardsLeftInDeck, clonedPlayerTurnContext.CardsLeftInDeck);
            Assert.AreEqual(playerTurnContext.FirstPlayedCard, clonedPlayerTurnContext.FirstPlayedCard);
            Assert.AreEqual(playerTurnContext.FirstPlayerAnnounce, clonedPlayerTurnContext.FirstPlayerAnnounce);
            Assert.AreEqual(playerTurnContext.SecondPlayedCard, clonedPlayerTurnContext.SecondPlayedCard);
            Assert.AreEqual(playerTurnContext.TrumpCard, clonedPlayerTurnContext.TrumpCard);
        }
    }
}
