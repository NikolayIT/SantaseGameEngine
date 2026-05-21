namespace Santase.Logic.Cards
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Numerics;

    /// <inheritdoc cref="ICollection" />
    /// <summary>
    /// Low memory (only 12 bytes per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>, IDeepCloneable<CardCollection>
    {
        public const long AllSantaseCardsBitMask = 8727889205820930;

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

        // Returns the struct enumerator by concrete type so that foreach over a
        // CardCollection-typed reference allocates nothing (the BCL List<T> pattern).
        // Interface-typed iteration still works through the explicit implementations below.
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.cards);
        }

        IEnumerator<Card> IEnumerable<Card>.GetEnumerator()
        {
            return this.GetEnumerator();
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
            var bits = this.cards;
            while (bits != 0)
            {
                var index = BitOperations.TrailingZeroCount(bits);
                array[arrayIndex++] = Card.Cards[index];
                bits &= bits - 1; // clear the lowest set bit
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
            return BitOperations.PopCount((ulong)this.cards);
        }

        /// <summary>
        /// Mutable struct enumerator that walks only the set bits via
        /// <see cref="BitOperations.TrailingZeroCount(long)"/>, so iteration cost is
        /// proportional to the number of cards rather than the 53-bit address space.
        /// </summary>
        public struct Enumerator : IEnumerator<Card>
        {
            private readonly long cards;

            private long remaining;

            private int currentHashCode;

            public Enumerator(long cards)
            {
                this.cards = cards;
                this.remaining = cards;
                this.currentHashCode = -1;
            }

            public Card Current => Card.Cards[this.currentHashCode];

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.remaining == 0)
                {
                    return false;
                }

                this.currentHashCode = BitOperations.TrailingZeroCount(this.remaining);
                this.remaining &= this.remaining - 1; // clear the lowest set bit
                return true;
            }

            public void Reset()
            {
                this.remaining = this.cards;
                this.currentHashCode = -1;
            }
        }
    }
}
