namespace Santase.Logic.RoundStates
{
    public class MoreThanTwoCardsLeftRoundState : FirstGamePhaseRoundState
    {
        public MoreThanTwoCardsLeftRoundState(IHaveState round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => true;

        public override bool CanClose => true;

        public override bool CanChangeTrump => true;

        internal override void PlayHand(int cardsLeftInDeck)
        {
            if (cardsLeftInDeck == 2)
            {
                this.Round.SetState(new TwoCardsLeftRoundState(this.Round));
            }
        }
    }
}
