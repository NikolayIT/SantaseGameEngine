namespace Santase.Logic.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Low memory (only 8 bytes per instance) implementation of card collection.
    /// </summary>
    public class CardCollection : ICollection<Card>
    {
        private const int MaxCards = 52;

        private long cards; // 64 bits for 52 possible cards

        public int Count
        {
            get
            {
                var bits = this.cards;
                var count = 0;
                while (bits > 0)
                {
                    var bit = bits & 1;
                    if (bit == 1)
                    {
                        count++;
                    }

                    bits = bits >> 1;
                }

                return count;
            }
        }

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
            this.cards |= (long)((long)1 << item.GetHashCode());
        }

        public void Clear()
        {
            this.cards = 0;
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
            if (item == null)
            {
                return false;
            }

            this.cards &= ~((long)1 << item.GetHashCode());

            return true;
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
                this.Reset();
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
                    this.currentHashCode++;
                    var bit = (this.cards >> this.currentHashCode) & 1;
                    if (bit == 1)
                    {
                        return true;
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
