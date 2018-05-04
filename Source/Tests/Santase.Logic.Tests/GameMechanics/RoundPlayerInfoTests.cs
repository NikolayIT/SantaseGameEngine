namespace Santase.Logic.Tests.GameMechanics
{
    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class RoundPlayerInfoTests
    {
        [Fact]
        public void ConstructorShouldSetDefaultPropertyValues()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            Assert.Same(player.Object, roundPlayerInfo.Player);
            Assert.NotNull(roundPlayerInfo.Cards);
            Assert.NotNull(roundPlayerInfo.TrickCards);
            Assert.NotNull(roundPlayerInfo.Announces);
            Assert.False(roundPlayerInfo.GameCloser);
            Assert.Equal(0, roundPlayerInfo.RoundPoints);
            Assert.False(roundPlayerInfo.HasAtLeastOneTrick);
        }

        [Fact]
        public void HasAtLeastOneTrickShouldReturnTrueAfterAddingCardToTricksAndFalseBeforeThat()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            Assert.False(roundPlayerInfo.HasAtLeastOneTrick);
            roundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.True(roundPlayerInfo.HasAtLeastOneTrick);
            roundPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ten));
            Assert.True(roundPlayerInfo.HasAtLeastOneTrick);
        }

        [Fact]
        public void AddCardShouldCallPlayersAddCardMethod()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            roundPlayerInfo.AddCard(card);
            player.Verify(x => x.AddCard(card), Times.Once());
        }

        [Fact]
        public void AddCardShouldAddTheCardToTheLocalCardsList()
        {
            var player = new Mock<BasePlayer>();
            var roundPlayerInfo = new RoundPlayerInfo(player.Object);
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            roundPlayerInfo.AddCard(card);
            Assert.True(roundPlayerInfo.Cards.Contains(card));
        }

        [Fact]
        public void RoundPointsShouldReturn0WhenAnnounceNoneAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            Assert.Equal(0, roundPlayerInfo.RoundPoints);

            roundPlayerInfo.Announces.Add(Announce.None);
            Assert.Equal(0, roundPlayerInfo.RoundPoints);
        }

        [Fact]
        public void RoundPointsShouldReturn20WhenAnnounceOfTwentyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            Assert.Equal(20, roundPlayerInfo.RoundPoints);
        }

        [Fact]
        public void RoundPointsShouldReturn40WhenAnnounceOfFortyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Forty);
            Assert.Equal(40, roundPlayerInfo.RoundPoints);
        }

        [Fact]
        public void RoundPointsShouldReturn40WhenAnnouncesOfTwentyAndFortyAdded()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            roundPlayerInfo.Announces.Add(Announce.Forty);
            Assert.Equal(60, roundPlayerInfo.RoundPoints);
        }

        [Fact]
        public void RoundPointsShouldReturnCorrectValueOfCards()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            var card1 = Card.GetCard(CardSuit.Diamond, CardType.Jack);
            var card2 = Card.GetCard(CardSuit.Diamond, CardType.Ace);
            roundPlayerInfo.TrickCards.Add(card1);
            roundPlayerInfo.TrickCards.Add(card2);
            Assert.Equal(card1.GetValue() + card2.GetValue(), roundPlayerInfo.RoundPoints);
        }

        [Fact]
        public void RoundPointsShouldReturnCorrectValueOfCardsAndAnnounces()
        {
            var roundPlayerInfo = new RoundPlayerInfo(new Mock<BasePlayer>().Object);
            var card1 = Card.GetCard(CardSuit.Diamond, CardType.Jack);
            var card2 = Card.GetCard(CardSuit.Diamond, CardType.Ace);
            roundPlayerInfo.TrickCards.Add(card1);
            roundPlayerInfo.TrickCards.Add(card2);
            roundPlayerInfo.Announces.Add(Announce.Twenty);
            Assert.Equal(card1.GetValue() + card2.GetValue() + 20, roundPlayerInfo.RoundPoints);
        }
    }
}
