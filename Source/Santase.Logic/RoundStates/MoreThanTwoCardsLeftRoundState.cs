using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.RoundStates
{
    public class MoreThanTwoCardsLeftRoundState : BaseRoundState
    {
        public MoreThanTwoCardsLeftRoundState(IGameRound round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40
        {
            get { return true; }
        }

        public override bool CanClose
        {
            get { return true; }
        }

        public override bool CanChangeTrump
        {
            get { return true; }
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
            if (cardsLeftInDeck == 2)
            {
                this.round.SetState(new TwoCardsLeftRoundState(this.round));
            }
        }
    }
}
