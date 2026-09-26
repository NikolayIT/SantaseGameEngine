namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;

    using Xunit;

    public class CardCollectionTests
    {
        // The ICollection<Card> contract for "no card": Contains(null) is false and Remove(null)
        // removes nothing, as with List and HashSet; Add(null) is refused. All three threw
        // NullReferenceException.
        [Fact]
        public void NoCardShouldBeInNoCollection()
        {
            var cards = new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Ace) };
            Assert.False(cards.Contains(null));
            Assert.False(cards.Remove(null));
            Assert.Throws<ArgumentNullException>(() => cards.Add(null));
            Assert.Single(cards);
            Assert.False(new CardCollection().Contains(null));
        }

        // A mask may only hold the 24 cards' bits: bit 0, the bits of ranks 2-8 and bits 53-63 are
        // no card, and enumerating them gave null or threw IndexOutOfRangeException.
        [Theory]
        [InlineData(1L)]
        [InlineData(1L << 2)]
        [InlineData(1L << 53)]
        [InlineData(1L << 63)]
        [InlineData(-1L)]
        public void AMaskShouldHoldOnlySantaseCards(long mask)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardCollection(mask));
        }

        [Fact]
        public void AMaskOfAllSantaseCardsShouldHoldThe24Cards()
        {
            var all = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            Assert.Equal(24, all.Count);
            Assert.Equal(24, all.Distinct().Count());
            Assert.All(all, Assert.NotNull);
        }

        [Fact]
        public void CopyToShouldRefuseAnArrayTheCardsDoNotFitIn()
        {
            var cards = new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Nine) };
            Assert.Throws<ArgumentNullException>(() => cards.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => cards.CopyTo(new Card[2], -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => cards.CopyTo(new Card[2], 1));
            var array = new Card[3];
            cards.CopyTo(array, 1);
            Assert.Null(array[0]);
            Assert.Equal(2, array.Count(c => c != null));
        }

        [Fact]
        public void IsReadOnlyShouldReturnFalse()
        {
            Assert.False(new CardCollection().IsReadOnly);
        }

        [Fact]
        public void CountShouldReturn0WhenCardCollectionIsInitialized()
        {
            Assert.Empty(new CardCollection());
        }

        [Fact]
        public void CountShouldReturn1WhenOneCardIsAdded()
        {
            var collection = new CardCollection { Card.GetCard(CardSuit.Club, CardType.Ace) };
            Assert.Single(collection);
        }

        [Fact]
        public void CountShouldReturn1WhenOneCardIsAddedAndThenRemoved()
        {
            var collection = new CardCollection();
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            collection.Add(card);
            collection.Remove(card);
            Assert.Empty(collection);
        }

        [Fact]
        public void CountShouldReturnCorrectValueAfterFewCardAdds()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Heart, CardType.King),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Spade, CardType.Jack),
                                     Card.GetCard(CardSuit.Spade, CardType.Nine),
                                 };

            Assert.Equal(6, collection.Count);
        }

        [Fact]
        public void CountShouldReturnCorrectValueAfterAddingAllCards()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                }
            }

            Assert.Equal(24, collection.Count);
        }

        [Fact]
        public void ContainsShouldReturnTrueForAllCardsAfterAddingThem()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                }
            }

            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    Assert.Contains(card, collection);
                }
            }
        }

        [Fact]
        public void ClearShouldReturn0Cards()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                     Card.GetCard(CardSuit.Heart, CardType.Jack),
                                     Card.GetCard(CardSuit.Spade, CardType.Nine),
                                 };
            collection.Clear();
            Assert.Empty(collection);
            Assert.Empty(collection.ToList());
        }

        [Fact]
        public void RemoveNonExistingCardsShouldNotRemoveThem()
        {
            var collection = new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ace) };
            collection.Remove(Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.Single(collection);
        }

        [Fact]
        public void RemoveShouldWorkProperly()
        {
            var card1 = Card.GetCard(CardSuit.Club, CardType.Ace); // 1
            var card2 = Card.GetCard(CardSuit.Spade, CardType.King); // 52
            var collection = new CardCollection { card1, card2 };
            collection.Remove(card1);
            collection.Remove(card2);
            Assert.Empty(collection);
        }

        [Fact]
        public void EnumerableGetEnumeratorShouldReturnNonNullEnumeratorWhichWorksCorrectly()
        {
            var card = Card.GetCard(CardSuit.Spade, CardType.King); // 52
            IEnumerable collection = new CardCollection { card };
            var enumerator = collection.GetEnumerator();
            Assert.NotNull(enumerator);
            enumerator.MoveNext();
            Assert.Equal(card, enumerator.Current);
        }

        [Fact]
        public void GetEnumeratorShouldReturnAllElementsInCollection()
        {
            var cards = new List<Card>
                            {
                                Card.GetCard(CardSuit.Club, CardType.Ace),
                                Card.GetCard(CardSuit.Spade, CardType.Ace),
                                Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                Card.GetCard(CardSuit.Heart, CardType.Jack),
                                Card.GetCard(CardSuit.Club, CardType.Nine),
                                Card.GetCard(CardSuit.Spade, CardType.Nine),
                            };

            var collection = new CardCollection();
            foreach (var card in cards)
            {
                collection.Add(card);
            }

            foreach (var card in collection)
            {
                Assert.True(cards.Contains(card), $"Card {card} not found in collection!");
            }

            // Second enumeration
            var count = 0;
            foreach (var card in collection)
            {
                Assert.True(cards.Contains(card), $"Card {card} not found in collection!");
                count++;
            }

            Assert.Equal(cards.Count, count);
        }

        [Fact]
        public void CopyToShouldWorkProperly()
        {
            var card1 = Card.GetCard(CardSuit.Club, CardType.Ace); // 1
            var card2 = Card.GetCard(CardSuit.Spade, CardType.King); // 52
            var collection = new CardCollection { card1, card2 };
            var array = new Card[2];
            collection.CopyTo(array, 0);
            Assert.Contains(card1, array);
            Assert.Contains(card2, array);
        }

        [Fact]
        //// [Timeout(100)]
        public void GetEnumeratorShouldWorkProperlyInNestedLoops()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace), // 1
                                     Card.GetCard(CardSuit.Spade, CardType.King), // 52
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Club, CardType.Jack),
                                     Card.GetCard(CardSuit.Heart, CardType.Nine),
                                 };
            foreach (var firstCard in collection)
            {
                Assert.NotNull(firstCard);
                var found = collection.Any(x => x.Equals(Card.GetCard(CardSuit.Diamond, CardType.Queen)));
                Assert.True(found);
            }
        }

        [Fact]
        public void CloneShouldReturnExactSameCollectionOfCards()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace), // 1
                                     Card.GetCard(CardSuit.Spade, CardType.King), // 52
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Club, CardType.Jack),
                                     Card.GetCard(CardSuit.Heart, CardType.Nine),
                                 };
            var clonedCollection = collection.DeepClone();
            Assert.NotNull(clonedCollection);
            Assert.Equal(collection.Count, clonedCollection.Count);
            foreach (var card in clonedCollection)
            {
                Assert.Contains(card, collection);
            }
        }

        [Fact]
        public void InternalEnumeratorResetMethodShouldAllowNewEnumerating()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace), // 1
                                     Card.GetCard(CardSuit.Spade, CardType.King), // 52
                                 };
            int count;
            using (var enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                }

                enumerator.Reset();
                count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }
            }

            Assert.Equal(collection.Count, count);
        }
    }
}
