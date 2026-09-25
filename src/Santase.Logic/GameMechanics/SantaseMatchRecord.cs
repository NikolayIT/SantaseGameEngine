namespace Santase.Logic.GameMechanics
{
    using System.Collections.Generic;

    /// <summary>
    /// A finished match in full, hidden cards included: every deal, trick, trump exchange, close
    /// and round result. Only available once the match is over.
    /// </summary>
    public sealed class SantaseMatchRecord
    {
        /// <summary>
        /// Gets the rounds, in order.
        /// </summary>
        public IReadOnlyList<SantaseRoundRecord> Rounds { get; init; }

        /// <summary>
        /// Gets the match winner.
        /// </summary>
        public PlayerPosition Winner { get; init; }

        /// <summary>
        /// Gets the first player's game points.
        /// </summary>
        public int FirstPlayerTotalPoints { get; init; }

        /// <summary>
        /// Gets the second player's game points.
        /// </summary>
        public int SecondPlayerTotalPoints { get; init; }
    }
}
