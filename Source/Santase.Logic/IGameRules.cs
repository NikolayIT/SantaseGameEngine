namespace Santase.Logic
{
    public interface IGameRules
    {
        int RoundPointsForGoingOut { get; }

        int HalfRoundPoints { get; }

        int GamePointsNeededForWin { get; }
    }
}
