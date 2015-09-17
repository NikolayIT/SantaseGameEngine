namespace Santase.Logic.RoundStates
{
    public class MoreThanTwoCardsLeftRoundState : BaseRoundState
    {
        public MoreThanTwoCardsLeftRoundState(IStateManager round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => true;

        public override bool CanClose => true;

        public override bool CanChangeTrump => true;

        public override bool ShouldObserveRules => false;

        public override bool ShouldDrawCard => true;

        internal override void PlayHand(int cardsLeftInDeck)
        {
            if (cardsLeftInDeck == 2)
            {
                this.Round.SetState(new TwoCardsLeftRoundState(this.Round));
            }
        }
    }
}
