namespace Santase.Logic.Players
{
    using System;

    using Santase.Logic.GameMechanics;

    public static class RestorablePlayerExtensions
    {
        /// <summary>
        /// Restores <paramref name="player"/> from the view of the seat to move and returns its move.
        /// </summary>
        /// <param name="player">The player; its earlier state is discarded.</param>
        /// <param name="view">The view of the seat to move (see <see cref="SantaseMatch.GetView"/>).</param>
        /// <returns>The move, ready for <see cref="SantaseMatch.Act"/>.</returns>
        public static PlayerAction ChooseMove(this IRestorablePlayer player, SantaseSeatView view)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            // Throws unless the view's seat is the one to move.
            var context = view.CreateTurnContext();
            player.Restore(view);
            return player.GetTurn(context);
        }
    }
}
