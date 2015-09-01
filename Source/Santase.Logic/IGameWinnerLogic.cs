namespace Santase.Logic
{
    using Santase.Logic.GameMechanics;

    public interface IGameWinnerLogic
    {
        PlayerPosition UpdatePointsAndGetFirstToPlay(
            RoundResult round,
            ref int firstPlayerPoints,
            ref int secondPlayerPoints);
    }
}
