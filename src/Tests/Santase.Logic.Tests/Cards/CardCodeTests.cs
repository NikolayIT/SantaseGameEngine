namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Linq;

    using Santase.Logic;
    using Santase.Logic.Cards;

    using Xunit;

    public class CardCodeTests
    {
        [Fact]
        public void FormatAndParseShouldRoundTripAllCards()
        {
            var codes = Deck.UnshuffledOrder.Select(CardCode.Format).ToList();

            Assert.Equal(24, codes.Distinct().Count());
            foreach (var card in Deck.UnshuffledOrder)
            {
                Assert.Same(card, CardCode.Parse(CardCode.Format(card)));
            }
        }

        [Theory]
        [InlineData(CardSuit.Club, CardType.Nine, "9C")]
        [InlineData(CardSuit.Diamond, CardType.Ten, "10D")]
        [InlineData(CardSuit.Heart, CardType.Jack, "JH")]
        [InlineData(CardSuit.Spade, CardType.Queen, "QS")]
        [InlineData(CardSuit.Club, CardType.King, "KC")]
        [InlineData(CardSuit.Heart, CardType.Ace, "AH")]
        public void FormatShouldWriteRankThenSuit(CardSuit suit, CardType type, string expected)
        {
            Assert.Equal(expected, CardCode.Format(Card.GetCard(suit, type)));
        }

        [Fact]
        public void ParseShouldIgnoreCase()
        {
            Assert.Same(Card.GetCard(CardSuit.Heart, CardType.Queen), CardCode.Parse("qh"));
            Assert.Same(Card.GetCard(CardSuit.Spade, CardType.Ten), CardCode.Parse("10s"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Q")]
        [InlineData("1H")]
        [InlineData("11H")]
        [InlineData("QX")]
        [InlineData("QHH")]
        [InlineData("2C")]
        [InlineData(" QH")]
        public void InvalidCodesShouldBeRejected(string code)
        {
            Assert.False(CardCode.TryParse(code, out var card));
            Assert.Null(card);
            Assert.Throws<FormatException>(() => CardCode.Parse(code));
        }

        [Fact]
        public void FormatShouldRejectANull()
        {
            Assert.Throws<ArgumentNullException>(() => CardCode.Format(null));
        }
    }
}
