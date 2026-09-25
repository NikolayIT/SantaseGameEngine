namespace Santase.Logic.GameMechanics
{
    /// <summary>
    /// The outcome of <see cref="SantaseMatch.Act"/>.
    /// </summary>
    public enum SantaseActResult
    {
        /// <summary>
        /// The action was applied.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// The action is not legal for this player now (an unplayable card, a trump change or
        /// close that is not allowed, or a null action). Nothing changed.
        /// </summary>
        InvalidAction = 1,

        /// <summary>
        /// It is the other player's turn. Nothing changed.
        /// </summary>
        NotYourTurn = 2,

        /// <summary>
        /// The match is over. Nothing changed.
        /// </summary>
        MatchFinished = 3,
    }
}
