namespace Santase.Logic.RoundStates
{
    // TODO: Unit test this class
    public class StateManagerManager : IStateManager
    {
        public StateManagerManager()
        {
            this.State = new StartRoundState(this);
        }

        public BaseRoundState State { get; private set; }

        public void SetState(BaseRoundState newState)
        {
            this.State = newState;
        }
    }
}
