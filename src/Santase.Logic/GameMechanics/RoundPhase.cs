namespace Santase.Logic.GameMechanics
{
    /// <summary>
    /// The phase of a round (the engine's round states), as seen in a <see cref="SantaseSeatView"/>.
    /// </summary>
    public enum RoundPhase
    {
        /// <summary>
        /// The first trick: no announces, no closing, no trump change.
        /// </summary>
        Start = 0,

        /// <summary>
        /// More than two cards in the talon: everything is allowed, the follower may play any card.
        /// </summary>
        MoreThanTwoCardsLeft = 1,

        /// <summary>
        /// The last draw is next: announces allowed, no closing or trump change.
        /// </summary>
        TwoCardsLeft = 2,

        /// <summary>
        /// The talon is exhausted or closed: the follower must follow suit (and beat, and trump)
        /// when possible.
        /// </summary>
        Final = 3,
    }
}
