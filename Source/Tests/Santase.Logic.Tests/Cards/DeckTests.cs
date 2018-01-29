namespace Santase.Logic.Tests.Cards
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    using Xunit;

    public class DeckTests
    {
        [Fact]
        public void TrumpCardShouldBeNonNullable()
        {
            IDeck deck = new Deck();
            Assert.NotNull(deck.TrumpCard);
        }

        [Fact]
        public void TrumpCardShouldBeRandom()
        {
            const int NumberOfRandomDecks = 25;
            var lastCard = new Deck().TrumpCard;
            for (var i = 0; i < NumberOfRandomDecks - 1; i++)
            {
                IDeck deck = new Deck();
                if (!deck.TrumpCard.Equals(lastCard))
                {
                    return;
                }

                lastCard = deck.TrumpCard;
            }

            Assert.Fail($"{NumberOfRandomDecks} times generated the same trump card!");
        }

        [Fact]
        public void CardsLeftShouldBe24ForANewDeck()
        {
            IDeck deck = new Deck();
            Assert.Equal(24, deck.CardsLeft);
        }

        [Fact]
        public void CardsLeftShouldBe23AfterDrawingOneCard()
        {
            IDeck deck = new Deck();
            deck.GetNextCard();
            Assert.Equal(23, deck.CardsLeft);
        }

        [Fact]
        public void CardsLeftShouldBe0AfterDrawing24Cards()
        {
            IDeck deck = new Deck();
            for (var i = 0; i < 24; i++)
            {
                deck.GetNextCard();
            }

            Assert.Equal(0, deck.CardsLeft);
        }

        [Fact]
        public void GetNextCardShouldThrowExceptionWhenCalled25Times()
        {
            IDeck deck = new Deck();
            for (var i = 0; i < 24; i++)
            {
                deck.GetNextCard();
            }

            Assert.Throws<InternalGameException>(() => deck.GetNextCard());
        }

        [Fact]
        public void GetNextCardShouldNotChangeTheTrumpCard()
        {
            IDeck deck = new Deck();
            var trumpBefore = deck.TrumpCard;
            deck.GetNextCard();
            var trumpAfter = deck.TrumpCard;
            Assert.Equal(trumpBefore, trumpAfter);
        }

        [Fact]
        public void GetNextCardShouldReturnDifferentNonNullCardEveryTime()
        {
            IDeck deck = new Deck();
            var cards = new HashSet<Card>();
            var cardsCount = deck.CardsLeft;
            for (var i = 0; i < cardsCount; i++)
            {
                var card = deck.GetNextCard();
                Assert.NotNull(card);
                Assert.False(cards.Contains(card), $"Duplicate card drawn \"{card}\"");
                cards.Add(card);
            }
        }

        [Fact]
        public void ChangeTrumpCardShouldWorkProperly()
        {
            IDeck deck = new Deck();
            var card = Card.GetCard(CardSuit.Spade, CardType.Nine);
            deck.ChangeTrumpCard(card);
            var trumpCard = deck.TrumpCard;
            Assert.Equal(card, trumpCard);
        }

        [Fact]
        public void ChangeTrumpCardShouldChangeTheLastCardInTheDeck()
        {
            IDeck deck = new Deck();
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            deck.ChangeTrumpCard(card);
            var cardsCount = deck.CardsLeft;
            for (var i = 0; i < cardsCount - 1; i++)
            {
                deck.GetNextCard();
            }

            var lastCard = deck.GetNextCard();
            Assert.Equal(card, lastCard);
        }
    }
}
