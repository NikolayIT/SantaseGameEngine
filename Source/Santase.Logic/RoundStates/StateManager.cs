namespace Santase.Logic.RoundStates
{
    public class StateManager : IStateManager
    {
        public StateManager()
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
