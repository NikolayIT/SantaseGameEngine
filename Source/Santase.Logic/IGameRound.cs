namespace Santase.Logic
{
    using Santase.Logic.RoundStates;

    public interface IGameRound
    {
        void Start();

        void SetState(BaseRoundState newState);

        int FirstPlayerPoints { get; }

        int SecondPlayerPoints { get; }

        bool FirstPlayerHasHand { get; }

        bool SecondPlayerHasHand { get; }

        PlayerPosition ClosedByPlayer { get; }

        PlayerPosition LastHandInPlayer { get; }
    }
}
