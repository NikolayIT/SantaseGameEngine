namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;

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
                if (values.Contains(stringValue))
                {
                    Assert.Fail($"Duplicate string value \"{stringValue}\" for card suit \"{cardSuitValue}\"");
                }

                values.Add(stringValue);
            }
        }

        [Test]
        public void CardTypeToFriendlyStringShouldReturnDifferentValidValueForEachPossibleParameter()
        {
            var values = new HashSet<string>();
            foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
            {
                var stringValue = cardTypeValue.ToFriendlyString();
                if (values.Contains(stringValue))
                {
                    Assert.Fail($"Duplicate string value \"{stringValue}\" for card suit \"{cardTypeValue}\"");
                }

                values.Add(stringValue);
            }
        }
    }
}
