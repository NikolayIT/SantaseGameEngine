using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.RoundStates
{
    public abstract class BaseRoundState
    {
        protected IGameRound round;

        protected BaseRoundState(IGameRound round)
        {
            this.round = round;
        }

        public abstract bool CanAnnounce20Or40 { get; }

        public abstract bool CanClose { get; }

        public abstract bool CanChangeTrump { get; }

        public abstract bool ShouldObserveRules { get; }

        public abstract bool ShouldDrawCard { get; }

        internal abstract void PlayHand(int cardsLeftInDeck);

        internal void Close()
        {
            if (this.CanClose)
            {
                round.SetState(new FinalRoundState(this.round));
            }
        }
    }
}
