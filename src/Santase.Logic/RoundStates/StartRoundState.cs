namespace Santase.Logic.RoundStates
{
    public class StartRoundState : BaseRoundState
    {
        public StartRoundState(IStateManager round)
            : base(round)
        {
        }

        public override bool CanAnnounce20Or40 => false;

        public override bool CanClose => false;

        public override bool CanChangeTrump => false;

        public override bool ShouldObserveRules => false;

        public override bool ShouldDrawCard => true;

        // After the first trick the phase follows the talon. With the standard six cards each the
        // talon then has 10; a variant dealing more cards (IGameRules.CardsAtStartOfTheRound) can
        // leave 2 or none.
        internal override void PlayHand(int cardsLeftInDeck)
        {
            BaseRoundState next = cardsLeftInDeck switch
            {
                0 => new FinalRoundState(this.Round),
                2 => new TwoCardsLeftRoundState(this.Round),
                _ => new MoreThanTwoCardsLeftRoundState(this.Round),
            };
            this.Round.SetState(next);
        }
    }
}
