namespace Santase.Logic.Tests.Cards
{
    using System.Collections.Generic;

    using NUnit.Framework;

    using Santase.Logic.Cards;

    [TestFixture]
    public class DeckTests
    {
        [Test]
        public void TrumpCardShouldBeNonNullable()
        {
            IDeck deck = new Deck();
            Assert.IsNotNull(deck.TrumpCard);
        }

        [Test]
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

        [Test]
        public void CardsLeftShouldBe24ForANewDeck()
        {
            IDeck deck = new Deck();
            Assert.AreEqual(24, deck.CardsLeft);
        }

        [Test]
        public void CardsLeftShouldBe23AfterDrawingOneCard()
        {
            IDeck deck = new Deck();
            deck.GetNextCard();
            Assert.AreEqual(23, deck.CardsLeft);
        }

        [Test]
        public void CardsLeftShouldBe0AfterDrawing24Cards()
        {
            IDeck deck = new Deck();
            for (var i = 0; i < 24; i++)
            {
                deck.GetNextCard();
            }

            Assert.AreEqual(0, deck.CardsLeft);
        }

        [Test]
        public void GetNextCardShouldThrowExceptionWhenCalled25Times()
        {
            IDeck deck = new Deck();
            for (var i = 0; i < 24; i++)
            {
                deck.GetNextCard();
            }

            Assert.Throws<InternalGameException>(() => deck.GetNextCard());
        }

        [Test]
        public void GetNextCardShouldNotChangeTheTrumpCard()
        {
            IDeck deck = new Deck();
            var trumpBefore = deck.TrumpCard;
            deck.GetNextCard();
            var trumpAfter = deck.TrumpCard;
            Assert.AreEqual(trumpBefore, trumpAfter);
        }

        [Test]
        public void GetNextCardShouldReturnDifferentNonNullCardEveryTime()
        {
            IDeck deck = new Deck();
            var cards = new HashSet<Card>();
            var cardsCount = deck.CardsLeft;
            for (var i = 0; i < cardsCount; i++)
            {
                var card = deck.GetNextCard();
                Assert.IsNotNull(card);
                Assert.IsFalse(cards.Contains(card), $"Duplicate card drawn \"{card}\"");
                cards.Add(card);
            }
        }

        [Test]
        public void ChangeTrumpCardShouldWorkProperly()
        {
            IDeck deck = new Deck();
            var card = new Card(CardSuit.Spade, CardType.Nine);
            deck.ChangeTrumpCard(card);
            var trumpCard = deck.TrumpCard;
            Assert.AreEqual(card, trumpCard);
        }

        [Test]
        public void ChangeTrumpCardShouldChangeTheLastCardInTheDeck()
        {
            IDeck deck = new Deck();
            var card = new Card(CardSuit.Club, CardType.Ace);
            deck.ChangeTrumpCard(card);
            var cardsCount = deck.CardsLeft;
            for (var i = 0; i < cardsCount - 1; i++)
            {
                deck.GetNextCard();
            }

            var lastCard = deck.GetNextCard();
            Assert.AreEqual(card, lastCard);
        }
    }
}
