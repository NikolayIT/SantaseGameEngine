namespace Santase.Logic.RoundStates
{
    public interface IStateManager
    {
        BaseRoundState State { get; }

        void SetState(BaseRoundState newState);
    }
}
