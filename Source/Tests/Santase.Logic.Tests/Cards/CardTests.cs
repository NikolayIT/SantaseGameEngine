namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    using Santase.Logic.Cards;

    [TestFixture]
    public class CardTests
    {
        [Test]
        public void ConstructorShouldUpdatePropertyValues()
        {
            var card = new Card(CardSuit.Spade, CardType.Queen);
            Assert.AreEqual(CardSuit.Spade, card.Suit);
            Assert.AreEqual(CardType.Queen, card.Type);
        }

        [Test]
        public void GetValueShouldReturnPositiveValueForEveryCardType()
        {
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var card = new Card(CardSuit.Diamond, cardTypeValue);
                var value = card.GetValue(); // Not expecting exceptions here
                Assert.IsTrue(value >= 0);
            }
        }

        [TestCase(true, CardSuit.Spade, CardType.Ace, CardSuit.Spade, CardType.Ace)]
        [TestCase(false, CardSuit.Heart, CardType.Jack, CardSuit.Heart, CardType.Queen)]
        [TestCase(false, CardSuit.Heart, CardType.King, CardSuit.Spade, CardType.King)]
        [TestCase(false, CardSuit.Heart, CardType.Nine, CardSuit.Spade, CardType.Ten)]
        public void EqualsShouldWorkCorrectly(
            bool expectedValue,
            CardSuit firstCardSuit,
            CardType firstCardType,
            CardSuit secondCardSuit,
            CardType secondCardType)
        {
            var firstCard = new Card(firstCardSuit, firstCardType);
            var secondCard = new Card(secondCardSuit, secondCardType);
            Assert.AreEqual(expectedValue, firstCard.Equals(secondCard));
            Assert.AreEqual(expectedValue, secondCard.Equals(firstCard));
        }

        [Test]
        public void EqualsShouldReturnFalseWhenGivenNullValue()
        {
            var card = new Card(CardSuit.Club, CardType.Nine);
            var areEqual = card.Equals(null);
            Assert.IsFalse(areEqual);
        }

        [Test]
        public void EqualsShouldReturnFalseWhenGivenNonCardObject()
        {
            var card = new Card(CardSuit.Club, CardType.Nine);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var areEqual = card.Equals(new CardTests());
            Assert.IsFalse(areEqual);
        }

        [Test]
        public void GetHashCodeShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<int>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    var cardHashCode = card.GetHashCode();
                    if (values.Contains(cardHashCode))
                    {
                        Assert.Fail($"Duplicate hash code \"{cardHashCode}\" for card \"{card}\"");
                    }

                    values.Add(cardHashCode);
                }
            }
        }

        [Test]
        public void ToStringShouldReturnDifferentValidValueForEachCardCombination()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    var cardToString = card.ToString();
                    if (values.Contains(cardToString))
                    {
                        Assert.Fail($"Duplicate string value \"{cardToString}\" for card \"{card}\"");
                    }

                    values.Add(cardToString);
                }
            }
        }
    }
}
