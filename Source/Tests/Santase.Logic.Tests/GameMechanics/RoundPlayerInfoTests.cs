namespace Santase.Logic.Tests.GameMechanics
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    [TestFixture]
    public class RoundPlayerInfoTests
    {
        [Test]
        public void ConstructorShouldSetDefaultPropertyValues()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            Assert.AreSame(player.Object, roundPlayerInfo.Player);
            Assert.IsNotNull(roundPlayerInfo.Cards);
            Assert.IsNotNull(roundPlayerInfo.TrickCards);
            Assert.IsNotNull(roundPlayerInfo.Announces);
            Assert.IsFalse(roundPlayerInfo.GameCloser);
            Assert.AreEqual(0, roundPlayerInfo.RoundPoints);
            Assert.IsFalse(roundPlayerInfo.HasAtLeastOneTrick);
        }

        [Test]
        public void HasAtLeastOneTrickShouldReturnTrueAfterAddingCardToTricksAndFalseBeforeThat()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            Assert.IsFalse(roundPlayerInfo.HasAtLeastOneTrick);
            roundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ace));
            Assert.IsTrue(roundPlayerInfo.HasAtLeastOneTrick);
            roundPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ten));
            Assert.IsTrue(roundPlayerInfo.HasAtLeastOneTrick);
        }

        [Test]
        public void AddCardShouldCallPlayersAddCardMethod()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            var card = new Card(CardSuit.Club, CardType.Ace);
            roundPlayerInfo.AddCard(card);
            player.Verify(x => x.AddCard(card), Times.Once());
        }

        [Test]
        public void AddCardShouldAddTheCardToTheLocalCardsList()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            var card = new Card(CardSuit.Club, CardType.Ace);
            roundPlayerInfo.AddCard(card);
            Assert.IsTrue(roundPlayerInfo.Cards.Contains(card));
        }

        [Test]
        public void RoundPointsShouldReturn0WhenAnnounceNoneAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            Assert.AreEqual(0, roundPlayerInfo.RoundPoints);

            roundPlayerInfo.Announces.Add(Announce.None);
            Assert.AreEqual(0, roundPlayerInfo.RoundPoints);
        }

        [Test]
        public void RoundPointsShouldReturn20WhenAnnounceOfTwentyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            Assert.AreEqual(20, roundPlayerInfo.RoundPoints);
        }

        [Test]
        public void RoundPointsShouldReturn40WhenAnnounceOfFortyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Forty);
            Assert.AreEqual(40, roundPlayerInfo.RoundPoints);
        }

        [Test]
        public void RoundPointsShouldReturn40WhenAnnouncesOfTwentyAndFortyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            roundPlayerInfo.Announces.Add(Announce.Forty);
            Assert.AreEqual(60, roundPlayerInfo.RoundPoints);
        }

        [Test]
        public void RoundPointsShouldReturnCorrectValueOfCards()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            var card1 = new Card(CardSuit.Diamond, CardType.Jack);
            var card2 = new Card(CardSuit.Diamond, CardType.Ace);
            roundPlayerInfo.TrickCards.Add(card1);
            roundPlayerInfo.TrickCards.Add(card2);
            Assert.AreEqual(card1.GetValue() + card2.GetValue(), roundPlayerInfo.RoundPoints);
        }

        [Test]
        public void RoundPointsShouldReturnCorrectValueOfCardsAndAnnounces()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            var card1 = new Card(CardSuit.Diamond, CardType.Jack);
            var card2 = new Card(CardSuit.Diamond, CardType.Ace);
            roundPlayerInfo.TrickCards.Add(card1);
            roundPlayerInfo.TrickCards.Add(card2);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            Assert.AreEqual(card1.GetValue() + card2.GetValue() + 20, roundPlayerInfo.RoundPoints);
        }
    }
}
