namespace Santase.Logic.Tests.Players
{
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    using Xunit;

    public class PlayerActionTests
    {
        [Fact]
        public void PlayCardShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.PlayCard(Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.Equal(PlayerActionType.PlayCard, playerAction.Type);
        }

        [Fact]
        public void PlayCardShouldPassCorrectCard()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            Assert.Equal(card, playerAction.Card);
        }

        [Fact]
        public void PlayCardShouldNotAffectAnnounceValue()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            Assert.Equal(Announce.None, playerAction.Announce);
        }

        [Fact]
        public void ChangeTrumpShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.Equal(PlayerActionType.ChangeTrump, playerAction.Type);
        }

        [Fact]
        public void ChangeTrumpShouldNotPassAnyCard()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.Null(playerAction.Card);
        }

        [Fact]
        public void ChangeTrumpShouldNotPassAnyAnnounce()
        {
            var playerAction = PlayerAction.ChangeTrump();
            Assert.Equal(Announce.None, playerAction.Announce);
        }

        [Fact]
        public void CloseGameShouldPassCorrectActionType()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.Equal(PlayerActionType.CloseGame, playerAction.Type);
        }

        [Fact]
        public void CloseGameShouldNotPassAnyCard()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.Null(playerAction.Card);
        }

        [Fact]
        public void CloseGameShouldNotPassAnyAnnounce()
        {
            var playerAction = PlayerAction.CloseGame();
            Assert.Equal(Announce.None, playerAction.Announce);
        }

        [Fact]
        public void ToStringShouldReturnValidValueWhenCloseGame()
        {
            var playerAction = PlayerAction.CloseGame();
            var toStringValue = playerAction.ToString();

            Assert.NotNull(toStringValue);
            Assert.Contains("CloseGame", toStringValue);
        }

        [Fact]
        public void ToStringShouldReturnValidValueWhenChangeTrump()
        {
            var playerAction = PlayerAction.ChangeTrump();
            var toStringValue = playerAction.ToString();

            Assert.NotNull(toStringValue);
            Assert.Contains("ChangeTrump", toStringValue);
        }

        [Fact]
        public void ToStringShouldReturnValidValueWhenPlayCard()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            var playerAction = PlayerAction.PlayCard(card);
            var toStringValue = playerAction.ToString();

            Assert.NotNull(toStringValue);
            Assert.Contains("PlayCard", toStringValue);
            Assert.Contains(card.ToString(), toStringValue);
        }
    }
}
