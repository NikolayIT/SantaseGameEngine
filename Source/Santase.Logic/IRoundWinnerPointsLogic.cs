namespace Santase.Logic
{
    using Santase.Logic.GameMechanics;

    public interface IRoundWinnerPointsLogic
    {
        RoundWinnerPoints GetWinnerPoints(RoundResult round);
    }
}
