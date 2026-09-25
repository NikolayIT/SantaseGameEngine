namespace Santase.Logic.GameMechanics
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    /// <summary>
    /// Everything that happened in one round, hidden cards included: for replays and for checking a
    /// deal after the match.
    /// </summary>
    public sealed class SantaseRoundRecord
    {
        /// <summary>
        /// Gets who led the first trick.
        /// </summary>
        public PlayerPosition FirstToPlay { get; init; }

        /// <summary>
        /// Gets the 24 cards in the order they came off the shuffled deck: 0-5 the first player's
        /// hand, 6-11 the second player's, 12-22 the talon in draw order, 23 the face-up trump
        /// card. Empty when the round was not dealt from a <see cref="Deck"/>.
        /// </summary>
        public IReadOnlyList<Card> Deal { get; init; }

        /// <summary>
        /// Gets the finished tricks, in order.
        /// </summary>
        public IReadOnlyList<SantaseTrick> Tricks { get; init; }

        /// <summary>
        /// Gets who exchanged the Nine of trumps for the face-up trump card, if anyone.
        /// </summary>
        public PlayerPosition TrumpSwappedBy { get; init; }

        /// <summary>
        /// Gets the face-up card taken in that exchange, or null.
        /// </summary>
        public Card SwappedTrumpCard { get; init; }

        /// <summary>
        /// Gets who closed the talon, if anyone.
        /// </summary>
        public PlayerPosition ClosedBy { get; init; }

        /// <summary>
        /// Gets how the round was scored; null for the round a match was stopped in
        /// (see <see cref="SantaseMatch.Stop"/>).
        /// </summary>
        public SantaseRoundSummary Result { get; init; }
    }
}
