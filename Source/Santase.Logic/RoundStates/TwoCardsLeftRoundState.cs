namespace Santase.Logic.RoundStates
{
    public class TwoCardsLeftRoundState : BaseRoundState
    {
        public TwoCardsLeftRoundState(IStateManager round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => true;

        public override bool CanClose => false;

        public override bool CanChangeTrump => false;

        public override bool ShouldObserveRules => false;

        public override bool ShouldDrawCard => true;

        internal override void PlayHand(int cardsLeftInDeck)
        {
            this.Round.SetState(new FinalRoundState(this.Round));
        }
    }
}
