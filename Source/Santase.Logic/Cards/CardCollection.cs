namespace Santase.Logic.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Low memory (only 12 bytes per instance) fast implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>, IDeepCloneable<CardCollection>
    {
        public const long AllSantaseCardsBitMask = 8727889205820930;

        private const int MaxCards = 52;

        private long cards; // 64 bits for 52 possible cards
        private int count;

        public CardCollection()
        {
        }

        public CardCollection(long bitMask)
        {
            this.cards = bitMask;
            this.count = this.CalculateCount();
        }

        public int Count => this.count;

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
                    this.count++;
                    this.cards |= (long)1 << item.GetHashCode();
                }
            }
        }

        public void Clear()
        {
            this.cards = 0;
            this.count = 0;
        }

        public bool Contains(Card item)
        {
            return ((this.cards >> item.GetHashCode()) & 1) == 1;
        }

        public void CopyTo(Card[] array, int arrayIndex)
        {
            foreach (var card in this)
            {
                array.SetValue(card, arrayIndex);
                arrayIndex = arrayIndex + 1;
            }
        }

        public bool Remove(Card item)
        {
            if (this.Contains(item))
            {
                unchecked
                {
                    this.count--;
                    this.cards &= ~((long)1 << item.GetHashCode());
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public CardCollection DeepClone()
        {
            return new CardCollection(this.cards);
        }

        private int CalculateCount()
        {
            var bits = this.cards;
            var cardsCount = 0;
            while (bits > 0)
            {
                var bit = bits & 1;
                if (bit == 1)
                {
                    cardsCount++;
                }

                bits = bits >> 1;
            }

            return cardsCount;
        }

        private class CardCollectionEnumerator : IEnumerator<Card>
        {
            private static readonly Card[] AllCards;

            private readonly long cards;

            private int currentHashCode;

            static CardCollectionEnumerator()
            {
                AllCards = new Card[MaxCards + 1];

                foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
                {
                    foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                    {
                        var card = new Card(cardSuitValue, cardTypeValue);
                        var hashCode = card.GetHashCode();
                        AllCards[hashCode] = card;
                    }
                }
            }

            public CardCollectionEnumerator(long cards)
            {
                this.cards = cards;
                this.currentHashCode = -1;
            }

            public Card Current => AllCards[this.currentHashCode];

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
