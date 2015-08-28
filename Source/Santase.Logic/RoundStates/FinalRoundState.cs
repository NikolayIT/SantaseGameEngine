namespace Santase.Logic.RoundStates
{
    // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#the-play
    public class FinalRoundState : BaseRoundState
    {
        public FinalRoundState(IStateManager round)
            : base(round)
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
