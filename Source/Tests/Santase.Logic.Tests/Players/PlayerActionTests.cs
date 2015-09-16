namespace Santase.Logic.Tests.Players
{
    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    [TestFixture]
    public class PlayerActionTests
    {
        [Test]
        public void PlayCardShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.PlayCard(new Card(CardSuit.Club, CardType.Ace));
            Assert.AreEqual(PlayerActionType.PlayCard, playerAction.Type);
        }

        [Test]
        public void PlayCardShouldPassCorrectCard()
        {
            var card = new Card(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            Assert.AreEqual(card, playerAction.Card);
        }

        [Test]
        public void PlayCardShouldNotAffectAnnounceValue()
        {
            var card = new Card(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            Assert.AreEqual(Announce.None, playerAction.Announce);
        }

        [Test]
        public void ChangeTrumpShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.AreEqual(PlayerActionType.ChangeTrump, playerAction.Type);
        }

        [Test]
        public void ChangeTrumpShouldNotPassAnyCard()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.AreEqual(null, playerAction.Card);
        }

        [Test]
        public void ChangeTrumpShouldNotPassAnyAnnounce()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.AreEqual(Announce.None, playerAction.Announce);
        }

        [Test]
        public void CloseGameShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.AreEqual(PlayerActionType.CloseGame, playerAction.Type);
        }

        [Test]
        public void CloseGameShouldNotPassAnyCard()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.AreEqual(null, playerAction.Card);
        }

        [Test]
        public void CloseGameShouldNotPassAnyAnnounce()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.AreEqual(Announce.None, playerAction.Announce);
        }

        [Test]
        public void ToStringShouldReturnValidValueWhenCloseGame()
        {
            var playerAction = PlayerAction.CloseGame();
            var toStringValue = playerAction.ToString();

            Assert.IsNotNull(toStringValue);
            Assert.IsTrue(toStringValue.Contains("CloseGame"));
        }

        [Test]
        public void ToStringShouldReturnValidValueWhenChangeTrump()
        {
            var playerAction = PlayerAction.ChangeTrump();
            var toStringValue = playerAction.ToString();

            Assert.IsNotNull(toStringValue);
            Assert.IsTrue(toStringValue.Contains("ChangeTrump"));
        }

        [Test]
        public void ToStringShouldReturnValidValueWhenPlayCard()
        {
            var card = new Card(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            var toStringValue = playerAction.ToString();

            Assert.IsNotNull(toStringValue);
            Assert.IsTrue(toStringValue.Contains("PlayCard"));
            Assert.IsTrue(toStringValue.Contains(card.ToString()));
        }
    }
}
