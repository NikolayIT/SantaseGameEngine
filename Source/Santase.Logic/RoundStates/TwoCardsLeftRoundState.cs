using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.RoundStates
{
    public class TwoCardsLeftRoundState : BaseRoundState
    {
        public TwoCardsLeftRoundState(IGameRound round)
            : base(round)
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
            get { return false; }
        }

        public override bool ShouldDrawCard
        {
            get { return true; }
        }

        internal override void PlayHand(int cardsLeftInDeck)
        {
            this.round.SetState(new FinalRoundState(this.round));
        }
    }
}
