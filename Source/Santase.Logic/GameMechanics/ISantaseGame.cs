namespace Santase.Logic.GameMechanics
{
    public interface ISantaseGame
    {
        int FirstPlayerTotalPoints { get; }

        int SecondPlayerTotalPoints { get; }

        int RoundsPlayed { get; }

        PlayerPosition Start(PlayerPosition firstToPlayInFirstRound);
    }
}
