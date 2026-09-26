namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Santase.Logic.Cards;

    using Xunit;

    public class CardTests
    {
        [Fact]
        public void ConstructorShouldUpdatePropertyValues()
        {
            var card = Card.GetCard(CardSuit.Spade, CardType.Queen);
            Assert.Equal(CardSuit.Spade, card.Suit);
            Assert.Equal(CardType.Queen, card.Type);
        }

        [Fact]
        public void GetValueShouldReturnPositiveValueForEveryCardType()
        {
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var card = Card.GetCard(CardSuit.Diamond, cardTypeValue);
                var value = card.GetValue(); // Not expecting exceptions here
                Assert.True(value >= 0);
            }
        }

        [Fact]
        public void GetValueShouldThrowAnExceptionWhenGivenInvalidCardType()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardTypeValue = cardTypes.OfType<CardType>().Max() + 1;
            Assert.Throws<IndexOutOfRangeException>(() => Card.GetCard(CardSuit.Spade, cardTypeValue));
        }

        // A host mapping its own numbers to cards must get an error for anything that is not a
        // Santase card, never another card (rank 0 used to be the previous suit's King, 14 the
        // next suit's Ace) or null (the ranks 2-8, which Santase does not use).
        [Theory]
        [InlineData(1, 0)] // Diamond, rank 0
        [InlineData(0, 14)] // Club, rank 14
        [InlineData(2, 2)] // Heart, rank 2
        [InlineData(0, 8)] // Club, rank 8
        [InlineData(0, 0)] // Club, rank 0
        [InlineData(4, 0)]
        [InlineData(4, 1)] // suit 4, Ace
        [InlineData(-1, 13)] // suit -1, King
        [InlineData(3, -1)] // Spade, rank -1
        public void GetCardShouldRejectAnythingThatIsNotASantaseCard(int suit, int type)
        {
            Assert.Throws<IndexOutOfRangeException>(() => Card.GetCard((CardSuit)suit, (CardType)type));
        }

        [Fact]
        public void GetCardShouldReturnTheCardAskedForForAllTwentyFourCards()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(suit, type);
                    Assert.Equal(suit, card.Suit);
                    Assert.Equal(type, card.Type);
                    Assert.Same(card, Card.GetCard(suit, type));
                }
            }
        }

        [Fact]
        public void FromHashCodeShouldReturnTheSameInstanceForAllTwentyFourCards()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(suit, type);
                    Assert.Same(card, Card.FromHashCode(card.GetHashCode()));
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)] // Club, rank 0
        [InlineData(2)] // Club, rank 2
        [InlineData(8)] // Club, rank 8
        [InlineData(15)] // Diamond, rank 2
        [InlineData(53)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void FromHashCodeShouldRejectAnythingThatIsNotASantaseCard(int hashCode)
        {
            Assert.Throws<IndexOutOfRangeException>(() => Card.FromHashCode(hashCode));
        }

        // Every Card is shared by the whole process, so their table must not be writable from
        // outside the engine: Card.Cards was a public array (never on nuget.org), where one
        // "Card.Cards[1] = null" would have broken every game after it. Bots use Card.FromHashCode.
        [Fact]
        public void CardShouldExposeNoPublicStaticFields()
        {
            Assert.Empty(typeof(Card).GetFields(BindingFlags.Public | BindingFlags.Static));
        }

        [Theory]
        [InlineData(true, CardSuit.Spade, CardType.Ace, CardSuit.Spade, CardType.Ace)]
        [InlineData(false, CardSuit.Heart, CardType.Jack, CardSuit.Heart, CardType.Queen)]
        [InlineData(false, CardSuit.Heart, CardType.King, CardSuit.Spade, CardType.King)]
        [InlineData(false, CardSuit.Heart, CardType.Nine, CardSuit.Spade, CardType.Ten)]
        public void EqualsShouldWorkCorrectly(
            bool expectedValue,
            CardSuit firstCardSuit,
            CardType firstCardType,
            CardSuit secondCardSuit,
            CardType secondCardType)
        {
            var firstCard = Card.GetCard(firstCardSuit, firstCardType);
            var secondCard = Card.GetCard(secondCardSuit, secondCardType);
            Assert.Equal(expectedValue, firstCard.Equals(secondCard));
            Assert.Equal(expectedValue, secondCard.Equals(firstCard));
        }

        [Fact]
        public void EqualsShouldReturnFalseWhenGivenNullValue()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Nine);
            var areEqual = card.Equals(null);
            Assert.False(areEqual);
        }

        [Fact]
        public void EqualsShouldReturnFalseWhenGivenNonCardObject()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Nine);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var areEqual = card.Equals(new CardTests());
            Assert.False(areEqual);
        }

        [Fact]
        public void GetHashCodeShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<int>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    var cardHashCode = card.GetHashCode();
                    Assert.False(
                        values.Contains(cardHashCode),
                        $"Duplicate hash code \"{cardHashCode}\" for card \"{card}\"");
                    values.Add(cardHashCode);
                }
            }
        }

        [Fact]
        public void ToStringShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    var cardToString = card.ToString();
                    Assert.False(
                        values.Contains(cardToString),
                        $"Duplicate string value \"{cardToString}\" for card \"{card}\"");
                    values.Add(cardToString);
                }
            }
        }

        [Fact]
        public void GetHashCodeShouldReturn1ForAceOfClubs()
        {
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            var hashCode = card.GetHashCode();
            Assert.Equal(1, hashCode);
        }

        [Fact]
        public void GetHashCodeShouldReturn52ForKingOfSpades()
        {
            var card = Card.GetCard(CardSuit.Spade, CardType.King);
            var hashCode = card.GetHashCode();
            Assert.Equal(52, hashCode);
        }
    }
}
