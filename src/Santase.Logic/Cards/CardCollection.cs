namespace Santase.Logic.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <inheritdoc cref="ICollection" />
    /// <summary>
    /// Low memory (only 12 bytes per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>, IDeepCloneable<CardCollection>
    {
        public const long AllSantaseCardsBitMask = 8727889205820930;

        private const int MaxCards = 52;

        private long cards; // 64 bits for 52 possible cards

        public CardCollection()
        {
        }

        public CardCollection(long bitMask)
        {
            this.cards = bitMask;
            this.Count = this.CalculateCount();
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public IEnumerator<Card> GetEnumerator()
        {
            return new CardCollectionEnumerator(this.cards);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(Card item)
        {
            if (!this.Contains(item))
            {
                unchecked
                {
                    this.Count++;
                    this.cards |= 1L << item.GetHashCode();
                }
            }
        }

        public void Clear()
        {
            this.cards = 0;
            this.Count = 0;
        }

        public bool Contains(Card item)
        {
            return ((this.cards >> item.GetHashCode()) & 1) == 1;
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            for (var currentHashCode = 0; currentHashCode < MaxCards; currentHashCode++)
            {
                if (((this.cards >> currentHashCode) & 1) == 1)
                {
                    array[arrayIndex++] = Card.Cards[currentHashCode];
                }
            }
        }

        public bool Remove(Card item)
        {
            if (this.Contains(item))
            {
                unchecked
                {
                    this.Count--;
                    this.cards &= ~(1L << item.GetHashCode());
                    return true;
                }
            }

            return false;
        }

        public CardCollection DeepClone()
        {
            return new CardCollection(this.cards);
        }

        private int CalculateCount()
        {
            var bits = this.cards;
            var cardsCount = 0;
            while (bits != 0)
            {
                cardsCount++;
                bits &= bits - 1;
            }

            return cardsCount;
        }

        private class CardCollectionEnumerator : IEnumerator<Card>
        {
            private readonly long cards;

            private int currentHashCode;

            public CardCollectionEnumerator(long cards)
            {
                this.cards = cards;
                this.currentHashCode = -1;
            }

            public Card Current => Card.Cards[this.currentHashCode];

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (this.currentHashCode <= MaxCards)
                {
                    unchecked
                    {
                        this.currentHashCode++;
                        if (((this.cards >> this.currentHashCode) & 1) == 1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                this.currentHashCode = -1;
            }
        }
    }
}
