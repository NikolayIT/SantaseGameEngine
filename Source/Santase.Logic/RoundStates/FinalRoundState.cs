using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic.RoundStates
{
    public class FinalRoundState : BaseRoundState
    {
        public FinalRoundState(IGameRound round)
            : base(round)
        {
        }

        // TODO: Think of somthing smarter
        public FinalRoundState()
            : base(null)
        {
        }

        public override bool CanAnnounce20Or40
        {
            get { return true; }
        }

        public override bool CanClose
        {
            get { return false; }
        }

        public override bool CanChangeTrump
        {
            get { return false; }
        }

        public override bool ShouldObserveRules
        {
            get { return true; }
        }

        public override bool ShouldDrawCard
        {
            get { return false; }
        }

        internal override void PlayHand(int cardsLeftInDeck)
        {
        }
    }
}
