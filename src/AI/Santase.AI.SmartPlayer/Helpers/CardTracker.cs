namespace Santase.AI.SmartPlayer.Helpers
{
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;

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

        // Rebuilds the tracker as SmartPlayer's callbacks leave it by the time it is asked for a
        // turn: unknown = not in the hand, not in a finished trick and not the face-up trump card
        // (the lead of the trick in progress stays unknown until EndTurn).
        public void Restore(SantaseSeatView view)
        {
            this.trumpCard = view.TrumpCard;
            this.PlayedCards = view.GetPlayedCards();
            this.UnknownCards = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            foreach (var card in view.Hand)
            {
                this.UnknownCards.Remove(card);
            }

            foreach (var card in this.PlayedCards)
            {
                this.UnknownCards.Remove(card);
            }

            if (view.IsTrumpCardOnTable)
            {
                this.UnknownCards.Remove(view.TrumpCard);
            }
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
            this.trumpCard = Card.GetCard(card.Suit, CardType.Nine);
            this.UnknownCards.Remove(card);
            this.UnknownCards.Remove(this.trumpCard);
        }

        public void TrumpCardSaw(Card newCard)
        {
            if (Card.Equals(newCard, this.trumpCard))
            {
                return;
            }

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
