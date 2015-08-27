﻿namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;
    using Santase.Logic.RoundStates;

    public class PlayerTurnContext
    {
        public PlayerTurnContext(BaseRoundState state, Card trumpCard, int cardsLeftInDeck)
        {
            this.State = state;
            this.TrumpCard = trumpCard;
            this.CardsLeftInDeck = cardsLeftInDeck;
        }

        public BaseRoundState State { get; internal set; }

        public Card TrumpCard { get; internal set; }

        public int CardsLeftInDeck { get; }

        public Card FirstPlayedCard { get; internal set; }

        public Card SecondPlayedCard { get; internal set; }

        public bool IsFirstPlayerTurn => this.FirstPlayedCard == null;
    }
}
