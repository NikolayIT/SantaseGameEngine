namespace Santase.Logic.RoundStates
{
    // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#the-play
    public abstract class FirstGamePhaseRoundState : BaseRoundState
    {
        protected FirstGamePhaseRoundState(IGameRound round)
            : base(round)
        {
        }

        public override bool ShouldObserveRules => false;

        public override bool ShouldDrawCard => true;
    }
}
