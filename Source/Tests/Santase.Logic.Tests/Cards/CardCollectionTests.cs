namespace Santase.Logic.Tests.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Santase.Logic.Cards;

    [TestFixture]
    public class CardCollectionTests
    {
        [Test]
        public void IsReadOnlyShouldReturnFalse()
        {
            Assert.IsFalse(new CardCollection().IsReadOnly);
        }

        [Test]
        public void CountShouldReturn0WhenCardCollectionIsInitialized()
        {
            Assert.AreEqual(0, new CardCollection().Count);
        }

        [Test]
        public void CountShouldReturn1WhenOneCardIsAdded()
        {
            var collection = new CardCollection { new Card(CardSuit.Club, CardType.Ace) };
            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void CountShouldReturn1WhenOneCardIsAddedAndThenRemoved()
        {
            var collection = new CardCollection();
            var card = new Card(CardSuit.Club, CardType.Ace);
            collection.Add(card);
            collection.Remove(card);
            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void CountShouldReturnCorrectValueAfterFewCardAdds()
        {
            var collection = new CardCollection
                                 {
                                     new Card(CardSuit.Club, CardType.Ace),
                                     new Card(CardSuit.Heart, CardType.Ten),
                                     new Card(CardSuit.Heart, CardType.King),
                                     new Card(CardSuit.Diamond, CardType.Queen),
                                     new Card(CardSuit.Spade, CardType.Jack),
                                     new Card(CardSuit.Spade, CardType.Nine)
                                 };

            Assert.AreEqual(6, collection.Count);
        }

        [Test]
        public void CountShouldReturnCorrectValueAfterAddingAllCards()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                }
            }

            Assert.AreEqual(24, collection.Count);
        }

        [Test]
        public void ContainsShouldReturnTrueForAllCardsAfterAddingThem()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                }
            }

            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardSuitValue, cardTypeValue);
                    Assert.IsTrue(collection.Contains(card));
                }
            }
        }

        [Test]
        public void ClearShouldReturn0Cards()
        {
            var collection = new CardCollection
                                 {
                                     new Card(CardSuit.Club, CardType.Ace),
                                     new Card(CardSuit.Diamond, CardType.Ten),
                                     new Card(CardSuit.Heart, CardType.Jack),
                                     new Card(CardSuit.Spade, CardType.Nine)
                                 };
            collection.Clear();
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(0, collection.ToList().Count);
        }

        [Test]
        public void RemoveNonExistingCardsShouldNotRemoveThem()
        {
            var collection = new CardCollection { new Card(CardSuit.Spade, CardType.Ace) };
            collection.Remove(new Card(CardSuit.Club, CardType.Ace));
            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void RemoveShouldWorkProperly()
        {
            var card1 = new Card(CardSuit.Club, CardType.Ace); // 1
            var card2 = new Card(CardSuit.Spade, CardType.King); // 52
            var collection = new CardCollection { card1, card2 };
            collection.Remove(card1);
            collection.Remove(card2);
            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void EnumerableGetEnumeratorShouldReturnNonNullEnumeratorWhichWorksCorrectly()
        {
            var card = new Card(CardSuit.Spade, CardType.King); // 52
            IEnumerable collection = new CardCollection { card };
            var enumerator = collection.GetEnumerator();
            Assert.IsNotNull(enumerator);
            enumerator.MoveNext();
            Assert.AreEqual(card, enumerator.Current);
        }

        [Test]
        public void GetEnumeratorShouldReturnAllElementsInCollection()
        {
            var cards = new List<Card>
                            {
                                new Card(CardSuit.Club, CardType.Ace),
                                new Card(CardSuit.Spade, CardType.Ace),
                                new Card(CardSuit.Diamond, CardType.Ten),
                                new Card(CardSuit.Heart, CardType.Jack),
                                new Card(CardSuit.Club, CardType.Nine),
                                new Card(CardSuit.Spade, CardType.Nine)
                            };

            var collection = new CardCollection();
            foreach (var card in cards)
            {
                collection.Add(card);
            }

            foreach (var card in collection)
            {
                Assert.IsTrue(cards.Contains(card), $"Card {card} not found in collection!");
            }

            // Second enumeration
            var count = 0;
            foreach (var card in collection)
            {
                Assert.IsTrue(cards.Contains(card), $"Card {card} not found in collection!");
                count++;
            }

            Assert.AreEqual(cards.Count, count);
        }

        [Test]
        public void CopyToShouldWorkProperly()
        {
            var card1 = new Card(CardSuit.Club, CardType.Ace); // 1
            var card2 = new Card(CardSuit.Spade, CardType.King); // 52
            var collection = new CardCollection { card1, card2 };
            var array = new Card[2];
            collection.CopyTo(array, 0);
            Assert.IsTrue(array.Contains(card1));
            Assert.IsTrue(array.Contains(card2));
        }

        [Test]
        //// [Timeout(100)]
        public void GetEnumeratorShouldWorkProperlyInNestedLoops()
        {
            var collection = new CardCollection
                                 {
                                     new Card(CardSuit.Club, CardType.Ace), // 1
                                     new Card(CardSuit.Spade, CardType.King), // 52
                                     new Card(CardSuit.Heart, CardType.Ten),
                                     new Card(CardSuit.Diamond, CardType.Queen),
                                     new Card(CardSuit.Club, CardType.Jack),
                                     new Card(CardSuit.Heart, CardType.Nine),
                                 };
            foreach (var firstCard in collection)
            {
                Assert.NotNull(firstCard);
                var found = collection.Any(x => x.Equals(new Card(CardSuit.Diamond, CardType.Queen)));
                Assert.IsTrue(found);
            }
        }

        [Test]
        public void CloneShouldReturnExactSameCollectionOfCards()
        {
            var collection = new CardCollection
                                 {
                                     new Card(CardSuit.Club, CardType.Ace), // 1
                                     new Card(CardSuit.Spade, CardType.King), // 52
                                     new Card(CardSuit.Heart, CardType.Ten),
                                     new Card(CardSuit.Diamond, CardType.Queen),
                                     new Card(CardSuit.Club, CardType.Jack),
                                     new Card(CardSuit.Heart, CardType.Nine),
                                 };
            var clonedCollection = collection.DeepClone();
            Assert.IsNotNull(clonedCollection);
            Assert.AreEqual(collection.Count, clonedCollection.Count);
            foreach (var card in clonedCollection)
            {
                Assert.IsTrue(collection.Contains(card));
            }
        }

        [Test]
        public void InternalEnumeratorResetMethodShouldAllowNewEnumerating()
        {
            var collection = new CardCollection
                                 {
                                     new Card(CardSuit.Club, CardType.Ace), // 1
                                     new Card(CardSuit.Spade, CardType.King) // 52
                                 };
            var enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            enumerator.Reset();
            var count = 0;
            while (enumerator.MoveNext())
            {
                count++;
            }

            Assert.AreEqual(collection.Count, count);
        }
    }
}
