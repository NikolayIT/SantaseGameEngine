namespace Santase.Logic.RoundStates
{
    public class TwoCardsLeftRoundState : FirstGamePhaseRoundState
    {
        public TwoCardsLeftRoundState(IHaveState round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => true;

        public override bool CanClose => false;

        public override bool CanChangeTrump => false;

        internal override void PlayHand(int cardsLeftInDeck)
        {
            this.Round.SetState(new FinalRoundState(this.Round));
        }
    }
}
