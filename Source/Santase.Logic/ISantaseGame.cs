namespace Santase.Logic
{
    public interface ISantaseGame
    {
        void Start();

        int FirstPlayerTotalPoints { get; }

        int SecondPlayerTotalPoints { get; }

        int RoundsPlayed { get; }
    }
}
