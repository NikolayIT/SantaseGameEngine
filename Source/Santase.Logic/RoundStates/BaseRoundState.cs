namespace Santase.Logic.RoundStates
{
    public abstract class BaseRoundState
    {
        protected readonly IGameRound round;

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
                this.round.SetState(new FinalRoundState(this.round));
            }
        }
    }
}
