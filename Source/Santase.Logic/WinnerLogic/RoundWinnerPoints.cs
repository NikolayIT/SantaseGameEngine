namespace Santase.Logic.WinnerLogic
{
    public class RoundWinnerPoints
    {
        private RoundWinnerPoints(PlayerPosition winner, int points)
        {
            this.Winner = winner;
            this.Points = points;
        }

        public PlayerPosition Winner { get; }

        public int Points { get; }

        public static RoundWinnerPoints First(int points)
        {
            return new RoundWinnerPoints(PlayerPosition.FirstPlayer, points);
        }

        public static RoundWinnerPoints Second(int points)
        {
            return new RoundWinnerPoints(PlayerPosition.SecondPlayer, points);
        }

        public static RoundWinnerPoints Draw()
        {
            return new RoundWinnerPoints(PlayerPosition.NoOne, 0);
        }
    }
}
