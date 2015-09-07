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

        public Card FirstPlayedCard { get; set; }

        public Announce FirstPlayerAnnounce { get; set; }

        public Card SecondPlayedCard { get; set; }

        public bool IsFirstPlayerTurn => this.FirstPlayedCard == null;

        public object Clone()
        {
            // Creating new instance here seems to be faster than calling MemberwiseClone()
            var newPlayerTurnContext = new PlayerTurnContext(this.State, this.TrumpCard, this.CardsLeftInDeck)
                                           {
                                               FirstPlayedCard = this.FirstPlayedCard,
                                               SecondPlayedCard = this.SecondPlayedCard,
                                               FirstPlayerAnnounce = this.FirstPlayerAnnounce
                                           };

            return newPlayerTurnContext;
        }
    }
}
