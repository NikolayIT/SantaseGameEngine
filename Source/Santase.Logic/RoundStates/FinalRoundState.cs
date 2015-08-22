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

        public override bool CanAnnounce20Or40 => true;

        public override bool CanClose => false;

        public override bool CanChangeTrump => false;

        public override bool ShouldObserveRules => true;

        public override bool ShouldDrawCard => false;

        internal override void PlayHand(int cardsLeftInDeck)
        {
        }
    }
}
