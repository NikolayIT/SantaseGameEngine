namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;

    /// <summary>
    /// One finished trick (and, in the record of a stopped match, the card left unanswered on the
    /// table: see <see cref="SantaseRoundRecord.Tricks"/>).
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
        /// round before the follower played (or the match was stopped before the answer).
        /// </summary>
        public Card FollowCard { get; init; }

        /// <summary>
        /// Gets the trick winner (the leader when <see cref="FollowCard"/> is null; nobody for the
        /// unanswered card of a stopped match).
        /// </summary>
        public PlayerPosition Winner { get; init; }

        /// <summary>
        /// Gets the number of cards in the talon (the face-up trump card included) while the trick
        /// was played, before the draws. It stops changing once the talon is closed; the trick
        /// played with 2 is the last one before the talon runs out, and its loser takes the trump
        /// card.
        /// </summary>
        public int CardsLeftInDeck { get; init; }
    }
}
