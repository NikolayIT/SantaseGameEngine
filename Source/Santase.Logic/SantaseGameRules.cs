namespace Santase.Logic
{
    public class SantaseGameRules : IGameRules
    {
        public int RoundPointsForGoingOut => 66;

        public int HalfRoundPoints => 33;

        public int GamePointsNeededForWin => 11;
    }
}
