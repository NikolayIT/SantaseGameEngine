namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class PlayCardActionValidatorTests
    {
        private static readonly Card JackOfHeart = new Card(CardSuit.Heart, CardType.Jack);
        private static readonly Card PlayerCard = new Card(CardSuit.Heart, CardType.Ace);
        private static readonly Card NonExistingCard = new Card(CardSuit.Diamond, CardType.Ace);
        private static readonly IList<Card> PlayerCards = new List<Card>
                                                              {
                                                                  new Card(CardSuit.Heart, CardType.Nine),
                                                                  new Card(CardSuit.Diamond, CardType.Jack),
                                                                  new Card(CardSuit.Heart, CardType.Queen),
                                                                  new Card(CardSuit.Spade, CardType.King),
                                                                  new Card(CardSuit.Diamond, CardType.Ten),
                                                                  new Card(CardSuit.Heart, CardType.Ace)
                                                              };

        private static readonly object[] ValidCardToPlay =
            {
                // otherPlayerCard, playerCard, trumpCard
                new object[]
                    {
                        // When required play trump card
                        new Card(CardSuit.Diamond, CardType.Ace),
                        new Card(CardSuit.Diamond, CardType.Jack),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
                new object[]
                    {
                        // When required play smaller non-trump card
                        new Card(CardSuit.Diamond, CardType.Ace),
                        new Card(CardSuit.Diamond, CardType.Jack),
                        new Card(CardSuit.Club, CardType.Nine)
                    },
                new object[]
                    {
                        // Play bigger trump card when available
                        new Card(CardSuit.Diamond, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Ten),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
                new object[]
                    {
                        // Play bigger non-trump card when available
                        new Card(CardSuit.Diamond, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Ten),
                        new Card(CardSuit.Club, CardType.Nine)
                    },
                new object[]
                    {
                        // Play trump when no card of the same suit is available
                        new Card(CardSuit.Club, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Ten),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
            };

        private static readonly object[] InvalidCardToPlay =
            {
                // otherPlayerCard, playerCard, trumpCard
                new object[]
                    {
                        new Card(CardSuit.Diamond, CardType.Ace),
                        new Card(CardSuit.Spade, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
                new object[]
                    {
                        new Card(CardSuit.Diamond, CardType.Ace),
                        new Card(CardSuit.Spade, CardType.King),
                        new Card(CardSuit.Club, CardType.Nine)
                    },
                new object[]
                    {
                        // Player has Diamond but plays trump.
                        new Card(CardSuit.Diamond, CardType.Ace),
                        new Card(CardSuit.Heart, CardType.Queen),
                        new Card(CardSuit.Heart, CardType.Jack)
                    },
                new object[]
                    {
                        new Card(CardSuit.Diamond, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Jack),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
                new object[]
                    {
                        new Card(CardSuit.Diamond, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Jack),
                        new Card(CardSuit.Club, CardType.Nine)
                    },
                new object[]
                    {
                        // Player has trump but plays other suit
                        new Card(CardSuit.Club, CardType.King),
                        new Card(CardSuit.Spade, CardType.King),
                        new Card(CardSuit.Diamond, CardType.Nine)
                    },
            };

        [Test]
        public void CanPlayCardShouldReturnFalseWhenCardIsNotPresentInThePlayerCards()
        {
            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(false, NonExistingCard, null, JackOfHeart, PlayerCards, true);
            Assert.IsFalse(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenRulesShouldNotBeObservedAndThePlayerIsNotFirst()
        {
            const bool ObserveRules = false;
            const bool First = false;
            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(First, PlayerCard, NonExistingCard, JackOfHeart, PlayerCards, ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstButTheRulesShouldNotBeObserved()
        {
            const bool ObserveRules = false;
            const bool First = true;
            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstAndRulesShouldBeObserved()
        {
            const bool ObserveRules = true;
            const bool First = true;
            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [TestCaseSource(nameof(ValidCardToPlay))]
        public void CanPlayCardShouldReturnTrue(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
            Assert.IsTrue(canPlayCard);
        }

        [TestCaseSource(nameof(InvalidCardToPlay))]
        public void CanPlayCardShouldReturnFalse(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            if (!PlayerCards.Contains(playerCard))
            {
                Assert.Fail("Invalid play card!");
            }

            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
            Assert.IsFalse(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenPlayerHasNoTrumpCardAndNoCardsOfThePlayedSuit()
        {
            var playerCards = new List<Card>
                                  {
                                      new Card(CardSuit.Club, CardType.Ace),
                                      new Card(CardSuit.Club, CardType.Ten),
                                      new Card(CardSuit.Club, CardType.King),
                                      new Card(CardSuit.Spade, CardType.Queen),
                                      new Card(CardSuit.Spade, CardType.Jack),
                                      new Card(CardSuit.Spade, CardType.Nine)
                                  };
            var playerCard = new Card(CardSuit.Spade, CardType.Nine);
            var otherPlayerCard = new Card(CardSuit.Heart, CardType.Nine);
            var trumpCard = new Card(CardSuit.Diamond, CardType.Nine);

            var validator = new PlayCardActionValidator();
            var canPlayCard = validator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, playerCards, true);
            Assert.IsTrue(canPlayCard);
        }
    }
}
