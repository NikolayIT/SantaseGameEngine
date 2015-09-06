namespace Santase.Logic
{
    using Santase.Logic.GameMechanics;

    public interface IRoundWinnerPointsLogic
    {
        RoundWinnerPoints GetWinnerPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer);
    }
}
