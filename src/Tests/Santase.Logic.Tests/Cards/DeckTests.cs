namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

            Assert.True(false, $"{NumberOfRandomDecks} times generated the same trump card!");
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

        [Fact]
        public void UnshuffledOrderShouldBeSuitMajorFromNineToAce()
        {
            var expected = new List<Card>();
            foreach (var suit in new[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade })
            {
                foreach (var type in new[] { CardType.Nine, CardType.Ten, CardType.Jack, CardType.Queen, CardType.King, CardType.Ace })
                {
                    expected.Add(Card.GetCard(suit, type));
                }
            }

            Assert.Equal(expected, Deck.UnshuffledOrder);
            Assert.Throws<NotSupportedException>(() => ((IList<Card>)Deck.UnshuffledOrder)[0] = null);
        }

        [Fact]
        public void ShuffleShouldDraw23NumbersWithExclusiveMaximaFrom24DownTo2()
        {
            var maxima = new List<int>();
            _ = new Deck(max =>
            {
                maxima.Add(max);
                return 0;
            });

            Assert.Equal(Enumerable.Range(2, 23).Reverse(), maxima);
        }

        [Fact]
        public void ShuffleThatAlwaysKeepsTheCardInPlaceShouldLeaveTheUnshuffledOrder()
        {
            // j = i swaps every card with itself: no change. The trump is the first card of the
            // unshuffled order and the first card drawn is the last one.
            var deck = new Deck(max => max - 1);

            Assert.Equal(Card.GetCard(CardSuit.Club, CardType.Nine), deck.TrumpCard);
            var drawn = DrawAll(deck);
            Assert.Equal(Deck.UnshuffledOrder.Reverse(), drawn);
        }

        [Fact]
        public void ShuffleThatAlwaysDrawsZeroShouldRotateTheDeck()
        {
            // j = 0 repeatedly swaps into position 0, which ends as the rotation
            // [c1, c2, ..., c23, c0]: the Ten of Clubs is the trump and the Nine of Clubs is drawn first.
            var deck = new Deck(_ => 0);

            Assert.Equal(Card.GetCard(CardSuit.Club, CardType.Ten), deck.TrumpCard);
            var drawn = DrawAll(deck);
            Assert.Equal(Card.GetCard(CardSuit.Club, CardType.Nine), drawn[0]);
            Assert.Equal(Card.GetCard(CardSuit.Club, CardType.Ten), drawn[23]);
        }

        [Fact]
        public void ShuffleShouldMatchTheGoldenDealVector()
        {
            // Known-answer vector, computed independently from the documented algorithm (not
            // from this code), for anyone re-implementing the deal (e.g. a browser verifier).
            // Draw k uses exclusive maximum 24 - k.
            var draws = new[] { 17, 3, 21, 0, 12, 16, 5, 8, 14, 2, 11, 7, 10, 4, 9, 1, 6, 3, 0, 4, 2, 1, 1 };
            var expectedOrder = Codes(
                "AS 10S 9S QH 10H JS KS 9D 10C QD KC KD 10D AD JC JH JD AC KH 9H 9C QS QC AH");

            var next = 0;
            var deck = new Deck(_ => draws[next++]);

            Assert.Equal(23, next);
            Assert.Equal(expectedOrder[0], deck.TrumpCard);
            Assert.Equal(Enumerable.Reverse(expectedOrder), DrawAll(deck));
        }

        [Fact]
        public void ShuffleShouldRejectAValueOutsideTheRequestedRange()
        {
            Assert.Throws<InternalGameException>(() => new Deck(max => max));
            Assert.Throws<InternalGameException>(() => new Deck(_ => -1));
        }

        [Fact]
        public void TheSameRandomSequenceShouldProduceTheSameDeck()
        {
            var first = DrawAll(new Deck(new Random(2026).Next));
            var second = DrawAll(new Deck(new Random(2026).Next));
            var other = DrawAll(new Deck(new Random(2027).Next));

            Assert.Equal(first, second);
            Assert.NotEqual(first, other);
            Assert.Equal(24, first.Distinct().Count());
        }

        private static List<Card> DrawAll(IDeck deck)
        {
            var cards = new List<Card>();
            while (deck.CardsLeft > 0)
            {
                cards.Add(deck.GetNextCard());
            }

            return cards;
        }

        private static List<Card> Codes(string codes)
        {
            var cards = new List<Card>();
            foreach (var code in codes.Split(' '))
            {
                var suit = code[^1] switch
                {
                    'C' => CardSuit.Club,
                    'D' => CardSuit.Diamond,
                    'H' => CardSuit.Heart,
                    _ => CardSuit.Spade,
                };
                var type = code[..^1] switch
                {
                    "9" => CardType.Nine,
                    "10" => CardType.Ten,
                    "J" => CardType.Jack,
                    "Q" => CardType.Queen,
                    "K" => CardType.King,
                    _ => CardType.Ace,
                };
                cards.Add(Card.GetCard(suit, type));
            }

            return cards;
        }
    }
}
