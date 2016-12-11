namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    [TestFixture]
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

        [Test]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenGivenNullCard()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(this.playerCards, null, Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(Announce.None, announce);
        }

        [TestCase(CardType.Nine)]
        [TestCase(CardType.Ten)]
        [TestCase(CardType.Jack)]
        [TestCase(CardType.Ace)]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenNoKingOrQueenIsPlayed(CardType cardType)
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Club, cardType),
                Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.AreEqual(Announce.None, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenQueenIsPlayedButTheRespectiveKingIsMissing()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Spade, CardType.Queen),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(Announce.None, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnNoAnnounceWhenKingIsPlayedButTheRespectiveQueenIsMissing()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Spade, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(Announce.None, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnTwentyWhenQueenIsPlayedTheKingIsPresentAndTheTrumpIsDifferentSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Club, CardType.Queen),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(Announce.Twenty, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnTwentyWhenKingIsPlayedTheQueenIsPresentAndTheTrumpIsDifferentSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Diamond, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(Announce.Twenty, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnFortyWhenQueenIsPlayedTheKingIsPresentAndTheTrumpIsTheSameSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Diamond, CardType.Queen),
                Card.GetCard(CardSuit.Diamond, CardType.Ace));
            Assert.AreEqual(Announce.Forty, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnFortyWhenKingIsPlayedTheQueenIsPresentAndTheTrumpIsTheSameSuit()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Heart, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Nine));
            Assert.AreEqual(Announce.Forty, announce);
        }

        [Test]
        public void GetPossibleAnnounceShouldReturnFalseWhenThePlayerIsNotFirst()
        {
            IAnnounceValidator validator = new AnnounceValidator();
            var announce = validator.GetPossibleAnnounce(
                this.playerCards,
                Card.GetCard(CardSuit.Heart, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Nine),
                false);
            Assert.AreEqual(Announce.None, announce);
        }
    }
}
