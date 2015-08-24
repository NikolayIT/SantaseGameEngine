namespace Santase.Logic
{
    using Santase.Logic.RoundStates;

    public interface IHaveState
    {
        void SetState(BaseRoundState newState);
    }
}