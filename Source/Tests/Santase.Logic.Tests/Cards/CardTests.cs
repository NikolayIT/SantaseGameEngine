namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        [Test]
        public void GetValueShouldThrowAnExceptionWhenGivenInvalidCardType()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardTypeValue = cardTypes.OfType<CardType>().Max() + 1;
            var card = new Card(CardSuit.Club, cardTypeValue);
            Assert.Throws<InternalGameException>(() => card.GetValue());
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
                    Assert.IsFalse(
                        values.Contains(cardHashCode),
                        $"Duplicate hash code \"{cardHashCode}\" for card \"{card}\"");
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
                    Assert.IsFalse(
                        values.Contains(cardToString),
                        $"Duplicate string value \"{cardToString}\" for card \"{card}\"");
                    values.Add(cardToString);
                }
            }
        }

        [Test]
        public void CloneShouldReturnDifferentReference()
        {
            var card = new Card(CardSuit.Diamond, CardType.Queen);
            var newCard = card.DeepClone();
            Assert.AreNotSame(card, newCard);
        }

        [Test]
        public void CloneShouldReturnObjectOfTypeCard()
        {
            var card = new Card(CardSuit.Diamond, CardType.Queen);
            var newCard = card.DeepClone();
            Assert.IsInstanceOf<Card>(newCard);
        }

        [Test]
        public void CloneShouldReturnEqualObjectWithEqualProperties()
        {
            var card = new Card(CardSuit.Club, CardType.Ace);
            var newCard = card.DeepClone();
            Assert.IsNotNull(newCard);
            Assert.IsTrue(card.Equals(newCard));
            Assert.AreEqual(card.Suit, newCard.Suit);
            Assert.AreEqual(card.Type, newCard.Type);
        }

        [Test]
        public void CloneShouldReturnObjectWithTheSameHashCode()
        {
            var card = new Card(CardSuit.Spade, CardType.Nine);
            var newCard = card.DeepClone();
            Assert.IsNotNull(newCard);
            Assert.AreEqual(card.GetHashCode(), newCard.GetHashCode());
        }

        [Test]
        public void FromHashCodeShouldCreateCardsWithTheGivenHashCode()
        {
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    var hashCode = card.GetHashCode();
                    var newCard = Card.FromHashCode(hashCode);
                    Assert.AreEqual(card, newCard);
                }
            }
        }

        [Test]
        public void GetHashCodeShouldReturn1ForAceOfClubs()
        {
            var card = new Card(CardSuit.Club, CardType.Ace);
            var hashCode = card.GetHashCode();
            Assert.AreEqual(1, hashCode);
        }

        [Test]
        public void GetHashCodeShouldReturn52ForKingOfSpades()
        {
            var card = new Card(CardSuit.Spade, CardType.King);
            var hashCode = card.GetHashCode();
            Assert.AreEqual(52, hashCode);
        }
    }
}
