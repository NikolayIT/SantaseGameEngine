namespace Santase.Logic.Players
{
    using Santase.Logic.GameMechanics;

    /// <summary>
    /// A player that can pick up a match from a <see cref="SantaseSeatView"/>, with no memory of
    /// the callbacks before it. That lets a server keep no bot objects between moves: build the
    /// bot, <see cref="Restore"/> it from the view of the seat to move and ask it for the turn
    /// (<see cref="RestorablePlayerExtensions.ChooseMove"/> does both). A restored player decides
    /// exactly as the same player would have after playing the match through its callbacks.
    /// </summary>
    public interface IRestorablePlayer : IPlayer
    {
        /// <summary>
        /// Sets the player to where <paramref name="view"/>'s seat is in the current round: its
        /// hand and everything it would have remembered. Earlier rounds are not needed; nothing a
        /// player learns carries over to the next deal.
        /// </summary>
        /// <param name="view">The view of this player's seat.</param>
        void Restore(SantaseSeatView view);
    }
}
