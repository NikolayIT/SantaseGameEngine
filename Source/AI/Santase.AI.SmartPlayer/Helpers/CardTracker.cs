namespace Santase.AI.SmartPlayer.Helpers
{
    using Santase.Logic.Cards;

    // TODO: Unit test this class
    public class CardTracker
    {
        private Card trumpCard;

        public CardTracker()
        {
            this.Clear();
        }

        public CardCollection UnknownCards { get; private set; }

        public CardCollection PlayedCards { get; private set; }

        public void Clear()
        {
            this.trumpCard = null;
            this.UnknownCards = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            this.PlayedCards = new CardCollection();
        }

        public void CardPlayed(Card card)
        {
            if (card == null)
            {
                return;
            }

            this.UnknownCards.Remove(card);
            this.PlayedCards.Add(card);
        }

        public void ChangeTrumpCard(Card card)
        {
            // Current player changed the trump card
            this.trumpCard = new Card(card.Suit, CardType.Nine);
            this.UnknownCards.Remove(card);
            this.UnknownCards.Remove(this.trumpCard);
        }

        public void TrumpCardSaw(Card newCard)
        {
            if (!Card.Equals(newCard, this.trumpCard))
            {
                // The other player changed the trump card
                this.UnknownCards.Remove(newCard);
                if (this.trumpCard != null)
                {
                    this.UnknownCards.Add(this.trumpCard);
                }

                this.trumpCard = newCard;
            }
        }
    }
}
