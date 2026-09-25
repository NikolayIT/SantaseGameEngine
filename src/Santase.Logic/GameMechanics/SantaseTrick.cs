namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;

    /// <summary>
    /// One finished trick.
    /// </summary>
    public sealed class SantaseTrick
    {
        /// <summary>
        /// Gets the player who led.
        /// </summary>
        public PlayerPosition Leader { get; init; }

        /// <summary>
        /// Gets the card led.
        /// </summary>
        public Card LeadCard { get; init; }

        /// <summary>
        /// Gets the marriage announced with the lead (20, 40 or none).
        /// </summary>
        public Announce Announce { get; init; }

        /// <summary>
        /// Gets the answer, or null when the announce took the leader to the target and ended the
        /// round before the follower played.
        /// </summary>
        public Card FollowCard { get; init; }

        /// <summary>
        /// Gets the trick winner (the leader when <see cref="FollowCard"/> is null).
        /// </summary>
        public PlayerPosition Winner { get; init; }
    }
}
