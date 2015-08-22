namespace Santase.Logic
{
    public interface ISantaseGame
    {
        int FirstPlayerTotalPoints { get; }

        int SecondPlayerTotalPoints { get; }

        int RoundsPlayed { get; }

        void Start();
    }
}
