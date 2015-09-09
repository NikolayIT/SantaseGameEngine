namespace Santase.Logic.WinnerLogic
{
    public interface IRoundWinnerPointsLogic
    {
        RoundWinnerPoints GetWinnerPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            IGameRules gameRules);
    }
}
