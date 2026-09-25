namespace Santase.Logic.GameMechanics
{
    /// <summary>
    /// How a finished round was scored.
    /// </summary>
    public sealed class SantaseRoundSummary
    {
        /// <summary>
        /// Gets the first player's round points: cards won plus announces, without the last-trick bonus.
        /// </summary>
        public int FirstPlayerRoundPoints { get; init; }

        /// <summary>
        /// Gets the second player's round points: cards won plus announces, without the last-trick bonus.
        /// </summary>
        public int SecondPlayerRoundPoints { get; init; }

        /// <summary>
        /// Gets who won the last trick with both hands played out (worth +10 unless the talon was
        /// closed); <see cref="PlayerPosition.NoOne"/> when the round ended early.
        /// </summary>
        public PlayerPosition LastTrickWinner { get; init; }

        /// <summary>
        /// Gets who closed the talon, if anyone.
        /// </summary>
        public PlayerPosition ClosedBy { get; init; }

        /// <summary>
        /// Gets the round winner, or <see cref="PlayerPosition.NoOne"/> for a drawn round.
        /// </summary>
        public PlayerPosition Winner { get; init; }

        /// <summary>
        /// Gets the game points the winner received (1 to 3; 0 for a drawn round).
        /// </summary>
        public int GamePoints { get; init; }
    }
}
