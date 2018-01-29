namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    using Xunit;

    public class AnnounceValidatorTests
    {
        private readonly ICollection<Card> playerCards = new List<Card>
                                                             {
                                                                 Card.GetCard(CardSuit.Club, CardType.Queen),
                                                                 Card.GetCard(CardSuit.Club, CardType.King),
                                                                 Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                                                 Card.GetCard(CardSuit.Diamond, CardType.King),
                                                                 Card.GetCard(CardSuit.Heart, CardType.Queen),
                                                                 Card.GetCard(CardSuit.Heart, CardType.King),
                                                             };

        [Fact]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenGivenNullCard()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(this.playerCards, null, Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(Announce.None, announce);
        }

        [Theory]
        [InlineData(CardType.Nine)]
        [InlineData(CardType.Ten)]
        [InlineData(CardType.Jack)]
        [InlineData(CardType.Ace)]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenNoKingOrQueenIsPlayed(CardType cardType)
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Club, cardType),
                Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.Equal(Announce.None, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenQueenIsPlayedButTheRespectiveKingIsMissing()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Spade, CardType.Queen),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(Announce.None, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenKingIsPlayedButTheRespectiveQueenIsMissing()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Spade, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(Announce.None, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnTwentyWhenQueenIsPlayedTheKingIsPresentAndTheTrumpIsDifferentSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Club, CardType.Queen),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(Announce.Twenty, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnTwentyWhenKingIsPlayedTheQueenIsPresentAndTheTrumpIsDifferentSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Diamond, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(Announce.Twenty, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnFortyWhenQueenIsPlayedTheKingIsPresentAndTheTrumpIsTheSameSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Diamond, CardType.Queen),
                Card.GetCard(CardSuit.Diamond, CardType.Ace));
            Assert.Equal(Announce.Forty, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnFortyWhenKingIsPlayedTheQueenIsPresentAndTheTrumpIsTheSameSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Heart, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Nine));
            Assert.Equal(Announce.Forty, announce);
        }

        [Fact]
        public void GetPossibleAnnounceShouldReturnFalseWhenThePlayerIsNotFirst()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Heart, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Nine),
                false);
            Assert.Equal(Announce.None, announce);
        }
    }
}
