namespace Santase.Logic
{
    public interface IGameRound : IHaveState
    {
        int FirstPlayerPoints { get; }

        int SecondPlayerPoints { get; }

        bool FirstPlayerHasHand { get; }

        bool SecondPlayerHasHand { get; }

        PlayerPosition ClosedByPlayer { get; }

        PlayerPosition LastHandInPlayer { get; }

        void Start();
    }
}
