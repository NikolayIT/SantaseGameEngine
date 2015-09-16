namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    [TestFixture]
    public class PlayCardActionValidatorTests
    {
        private static readonly Card JackOfHeart = new Card(CardSuit.Heart, CardType.Jack);

        private static readonly Card PlayerCard = new Card(CardSuit.Heart, CardType.Ace);

        private static readonly Card NonExistingCard = new Card(CardSuit.Diamond, CardType.Ace);

        private static readonly ICollection<Card> PlayerCards = new List<Card>
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
                        new Card(
                            CardSuit.Diamond,
                            CardType.Jack),
                        new Card(
                            CardSuit.Diamond,
                            CardType.Nine)
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
                        new Card(
                            CardSuit.Diamond,
                            CardType.King),
                        new Card(CardSuit.Diamond, CardType.Ten),
                        new Card(
                            CardSuit.Diamond,
                            CardType.Nine)
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
                        new Card(
                            CardSuit.Diamond,
                            CardType.Ace),
                        new Card(
                            CardSuit.Heart,
                            CardType.Queen),
                        new Card(
                            CardSuit.Heart,
                            CardType.Jack)
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
                        new Card(
                            CardSuit.Spade,
                            CardType.King),
                        new Card(
                            CardSuit.Diamond,
                            CardType.Nine)
                    },
            };

        [Test]
        public void CanPlayCardShouldReturnFalseWhenCardIsNotPresentInThePlayerCards()
        {
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, NonExistingCard, null, JackOfHeart, PlayerCards, true);
            Assert.IsFalse(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenRulesShouldNotBeObservedAndThePlayerIsNotFirst()
        {
            const bool ObserveRules = false;
            const bool First = false;
            var canPlayCard = PlayCardActionValidator.CanPlayCard(
                First,
                PlayerCard,
                NonExistingCard,
                JackOfHeart,
                PlayerCards,
                ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstButTheRulesShouldNotBeObserved()
        {
            const bool ObserveRules = false;
            const bool First = true;
            var canPlayCard = PlayCardActionValidator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [Test]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstAndRulesShouldBeObserved()
        {
            const bool ObserveRules = true;
            const bool First = true;
            var canPlayCard = PlayCardActionValidator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.IsTrue(canPlayCard);
        }

        [TestCaseSource(nameof(ValidCardToPlay))]
        public void CanPlayCardShouldReturnTrue(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
            Assert.IsTrue(canPlayCard);
        }

        [TestCaseSource(nameof(InvalidCardToPlay))]
        public void CanPlayCardShouldReturnFalse(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            Assert.IsTrue(PlayerCards.Contains(playerCard), "Invalid play card selected for the test!");
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
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

            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, playerCards, true);
            Assert.IsTrue(canPlayCard);
        }

        [Test]
        public void
            CanPlayCardShouldReturnTrueWhenPlayerHasBiggerCardFromDifferentSuitButDoNotHaveBiggerCardFromTheSameSuit()
        {
            var playerCards = new List<Card>
                                  {
                                      new Card(CardSuit.Spade, CardType.Nine),
                                      new Card(CardSuit.Heart, CardType.King),
                                      new Card(CardSuit.Heart, CardType.Ace),
                                      new Card(CardSuit.Club, CardType.Jack),
                                      new Card(CardSuit.Diamond, CardType.Queen),
                                      new Card(CardSuit.Heart, CardType.Nine)
                                  };
            var playerCard = new Card(CardSuit.Spade, CardType.Nine);
            var otherPlayerCard = new Card(CardSuit.Spade, CardType.Ten);
            var trumpCard = new Card(CardSuit.Diamond, CardType.Nine);

            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, playerCards, true);
            Assert.IsTrue(canPlayCard);
        }
    }
}
