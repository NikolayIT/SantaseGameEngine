namespace Santase.Logic.Players
{
    using System;

    using Santase.Logic.Cards;
    using Santase.Logic.RoundStates;

    public class PlayerTurnContext : ICloneable
    {
        public PlayerTurnContext(BaseRoundState state, Card trumpCard, int cardsLeftInDeck)
        {
            this.State = state;
            this.TrumpCard = trumpCard;
            this.CardsLeftInDeck = cardsLeftInDeck;
        }

        public BaseRoundState State { get; set; }

        public Card TrumpCard { get; set; }

        public int CardsLeftInDeck { get; }

        // TODO: Add FirstPlayerAnnounce?
        public Card FirstPlayedCard { get; set; }

        public Card SecondPlayedCard { get; set; }

        public bool IsFirstPlayerTurn => this.FirstPlayedCard == null;

        public object Clone()
        {
            var newPlayerTurnContext = new PlayerTurnContext(this.State, this.TrumpCard, this.CardsLeftInDeck);
            newPlayerTurnContext.FirstPlayedCard = this.FirstPlayedCard?.Clone() as Card;
            newPlayerTurnContext.SecondPlayedCard = this.SecondPlayedCard?.Clone() as Card;
            return newPlayerTurnContext;
        }
    }
}
