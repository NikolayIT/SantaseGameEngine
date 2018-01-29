namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    using Xunit;

    public class PlayCardActionValidatorTests
    {
        private static readonly Card JackOfHeart = Card.GetCard(CardSuit.Heart, CardType.Jack);

        private static readonly Card PlayerCard = Card.GetCard(CardSuit.Heart, CardType.Ace);

        private static readonly Card NonExistingCard = Card.GetCard(CardSuit.Diamond, CardType.Ace);

        private static readonly ICollection<Card> PlayerCards = new List<Card>
                                                              {
                                                                  Card.GetCard(CardSuit.Heart, CardType.Nine),
                                                                  Card.GetCard(CardSuit.Diamond, CardType.Jack),
                                                                  Card.GetCard(CardSuit.Heart, CardType.Queen),
                                                                  Card.GetCard(CardSuit.Spade, CardType.King),
                                                                  Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                                                  Card.GetCard(CardSuit.Heart, CardType.Ace),
                                                              };

        [Fact]
        public void CanPlayCardShouldReturnFalseWhenCardIsNotPresentInThePlayerCards()
        {
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, NonExistingCard, null, JackOfHeart, PlayerCards, true);
            Assert.False(canPlayCard);
        }

        [Fact]
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
            Assert.True(canPlayCard);
        }

        [Fact]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstButTheRulesShouldNotBeObserved()
        {
            const bool ObserveRules = false;
            const bool First = true;
            var canPlayCard = PlayCardActionValidator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.True(canPlayCard);
        }

        [Fact]
        public void CanPlayCardShouldReturnTrueWhenThePlayerIsFirstAndRulesShouldBeObserved()
        {
            const bool ObserveRules = true;
            const bool First = true;
            var canPlayCard = PlayCardActionValidator.CanPlayCard(First, PlayerCard, null, JackOfHeart, PlayerCards, ObserveRules);
            Assert.True(canPlayCard);
        }

        [Theory]
        [MemberData(nameof(DataSource.ValidCardToPlay), MemberType = typeof(DataSource))]
        public void CanPlayCardShouldReturnTrue(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
            Assert.True(canPlayCard);
        }

        [Theory]
        [MemberData(nameof(DataSource.InvalidCardToPlay), MemberType = typeof(DataSource))]
        public void CanPlayCardShouldReturnFalse(Card otherPlayerCard, Card playerCard, Card trumpCard)
        {
            Assert.True(PlayerCards.Contains(playerCard), "Invalid play card selected for the test!");
            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, PlayerCards, true);
            Assert.False(canPlayCard);
        }

        [Fact]
        public void CanPlayCardShouldReturnTrueWhenPlayerHasNoTrumpCardAndNoCardsOfThePlayedSuit()
        {
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Club, CardType.Ace),
                                      Card.GetCard(CardSuit.Club, CardType.Ten),
                                      Card.GetCard(CardSuit.Club, CardType.King),
                                      Card.GetCard(CardSuit.Spade, CardType.Queen),
                                      Card.GetCard(CardSuit.Spade, CardType.Jack),
                                      Card.GetCard(CardSuit.Spade, CardType.Nine),
                                  };
            var playerCard = Card.GetCard(CardSuit.Spade, CardType.Nine);
            var otherPlayerCard = Card.GetCard(CardSuit.Heart, CardType.Nine);
            var trumpCard = Card.GetCard(CardSuit.Diamond, CardType.Nine);

            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, playerCards, true);
            Assert.True(canPlayCard);
        }

        [Fact]
        public void CanPlayCardShouldReturnTrueWhenPlayerHasBiggerCardFromDifferentSuitButDoNotHaveBiggerCardFromTheSameSuit()
        {
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Spade, CardType.Nine),
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Heart, CardType.Ace),
                                      Card.GetCard(CardSuit.Club, CardType.Jack),
                                      Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                      Card.GetCard(CardSuit.Heart, CardType.Nine),
                                  };
            var playerCard = Card.GetCard(CardSuit.Spade, CardType.Nine);
            var otherPlayerCard = Card.GetCard(CardSuit.Spade, CardType.Ten);
            var trumpCard = Card.GetCard(CardSuit.Diamond, CardType.Nine);

            var canPlayCard = PlayCardActionValidator.CanPlayCard(false, playerCard, otherPlayerCard, trumpCard, playerCards, true);
            Assert.True(canPlayCard);
        }

        public static class DataSource
        {
            public static readonly IEnumerable<object[]> ValidCardToPlay = new List<object[]>
            {
                // otherPlayerCard, playerCard, trumpCard
                new object[]
                    {
                        // When required play trump card
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
                new object[]
                    {
                        // When required play smaller non-trump card
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                    },
                new object[]
                    {
                        // Play bigger trump card when available
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
                new object[]
                    {
                        // Play bigger non-trump card when available
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                    },
                new object[]
                    {
                        // Play trump when no card of the same suit is available
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
            };

            public static readonly IEnumerable<object[]> InvalidCardToPlay = new List<object[]>
            {
                // otherPlayerCard, playerCard, trumpCard
                new object[]
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
                new object[]
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                    },
                new object[]
                    {
                        // Player has Diamond but plays trump.
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Heart, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                    },
                new object[]
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
                new object[]
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                    },
                new object[]
                    {
                        // Player has trump but plays other suit
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                    },
            };
        }
    }
}
