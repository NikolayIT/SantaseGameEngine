namespace Santase.Logic.RoundStates
{
    public class StateManagerManager : IStateManager
    {
        public BaseRoundState State { get; private set; }

        public void SetState(BaseRoundState newState)
        {
            this.State = newState;
        }
    }
}
