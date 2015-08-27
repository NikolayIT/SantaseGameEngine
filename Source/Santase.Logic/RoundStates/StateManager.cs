namespace Santase.Logic.RoundStates
{
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
