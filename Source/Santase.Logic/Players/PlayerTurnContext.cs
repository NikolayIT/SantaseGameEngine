using Santase.Logic.Cards;
using Santase.Logic.RoundStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic.Players
{
    public class PlayerTurnContext
    {
        public PlayerTurnContext(
            BaseRoundState state,
            Card trumpCard,
            int cardsLeftInDeck)
        {
            this.State = state;
            this.TrumpCard = trumpCard;
            this.CardsLeftInDeck = cardsLeftInDeck;
        }

        public BaseRoundState State { get; internal set; }

        public Card TrumpCard { get; internal set; }

        public int CardsLeftInDeck { get; private set; }

        public Card FirstPlayedCard { get; internal set; }

        public Card SecondPlayedCard { get; internal set; }

        public bool AmITheFirstPlayer
        {
            get
            {
                return this.FirstPlayedCard == null;
            }
        }
    }
}
