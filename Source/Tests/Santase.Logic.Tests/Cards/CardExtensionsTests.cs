namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    using Xunit;

    public class CardExtensionsTests
    {
        [Fact]
        public void CardSuitToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                var stringValue = cardSuitValue.ToFriendlyString();
                Assert.False(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardSuitValue}\"");
                values.Add(stringValue);
            }
        }

        [Fact]
        public void CardSuitToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardSuit.ToFriendlyString());
        }

        [Fact]
        public void CardSuitMapAsSortableByColorShouldOrderByByColorInOrderSpadeHeartClubAndDiamonds()
        {
            var cardSuits =
                Enum.GetValues(typeof(CardSuit)).OfType<CardSuit>().OrderBy(x => x.MapAsSortableByColor()).ToList();

            Assert.Equal(CardSuit.Spade, cardSuits[0]);
            Assert.Equal(CardSuit.Heart, cardSuits[1]);
            Assert.Equal(CardSuit.Club, cardSuits[2]);
            Assert.Equal(CardSuit.Diamond, cardSuits[3]);
        }

        [Fact]
        public void CardSuitMapAsSortableByColorShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardSuits = Enum.GetValues(typeof(CardSuit));
            var cardSuit = cardSuits.OfType<CardSuit>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardSuit.MapAsSortableByColor());
        }

        [Fact]
        public void CardTypeToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var stringValue = cardTypeValue.ToFriendlyString();
                Assert.False(values.Contains(stringValue), $"Duplicate string value \"{stringValue}\" for card suit \"{cardTypeValue}\"");
                values.Add(stringValue);
            }
        }

        [Fact]
        public void CardTypeToFriendlyStringShouldThrowAnExceptionWhenCalledOnAnInvalidValue()
        {
            var cardTypes = Enum.GetValues(typeof(CardType));
            var cardType = cardTypes.OfType<CardType>().Max() + 1;
            Assert.Throws<ArgumentException>(() => cardType.ToFriendlyString());
        }
    }
}
