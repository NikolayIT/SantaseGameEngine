namespace Santase.Logic.RoundStates
{
    public class StartRoundState : FirstGamePhaseRoundState
    {
        public StartRoundState(IHaveState round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => false;

        public override bool CanClose => false;

        public override bool CanChangeTrump => false;

        internal override void PlayHand(int cardsLeftInDeck)
        {
            this.Round.SetState(new MoreThanTwoCardsLeftRoundState(this.Round));
        }
    }
}
