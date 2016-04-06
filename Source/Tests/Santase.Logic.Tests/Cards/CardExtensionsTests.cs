namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Santase.Logic.Cards;

    [TestFixture]
    public class CardExtensionsTests
    {
        [Test]
        public void CardSuitToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                var stringValue = cardSuitValue.ToFriendlyString();
                Assert.IsFalse(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardSuitValue}\"");
                values.Add(stringValue);
            }
        }

        [Test]
        public void CardSuitToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardSuit.ToFriendlyString());
        }

        [Test]
        public void CardSuitMapAsSortableByColorShouldOrderByByColorInOrderSpadeHeartClubAndDiamonds()
        {
            var cardSuits =
                Enum.GetValues(typeof(CardSuit)).OfType<CardSuit>().OrderBy(x => x.MapAsSortableByColor()).ToList();

            Assert.AreEqual(CardSuit.Spade, cardSuits[0]);
            Assert.AreEqual(CardSuit.Heart, cardSuits[1]);
            Assert.AreEqual(CardSuit.Club, cardSuits[2]);
            Assert.AreEqual(CardSuit.Diamond, cardSuits[3]);
        }

        [Test]
        public void CardSuitMapAsSortableByColorShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardSuit.MapAsSortableByColor());
        }

        [Test]
        public void CardTypeToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var stringValue = cardTypeValue.ToFriendlyString();
                Assert.IsFalse(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardTypeValue}\"");
                values.Add(stringValue);
            }
        }

        [Test]
        public void CardTypeToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardType = cardTypes.OfType<CardType>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardType.ToFriendlyString());
        }
    }
}
