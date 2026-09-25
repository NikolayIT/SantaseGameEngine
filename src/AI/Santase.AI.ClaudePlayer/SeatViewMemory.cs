namespace Santase.AI.ClaudePlayer
{
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;

    // What the Claude players remember about a round, rebuilt from a seat view (IRestorablePlayer).
    // Mirrors their callbacks exactly, so a restored player decides as the live one would.
    internal static class SeatViewMemory
    {
        // UnknownCards as StartRound / AddCard / EndTurn and the trump sync at the start of
        // GetTurn leave it: not in the hand, not in a finished trick and not the face-up trump card
        // while it is on the table. The lead of the trick in progress is still unknown: the
        // players only file played cards at EndTurn.
        public static CardCollection UnknownCards(SantaseSeatView view)
        {
            var unknown = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            foreach (var card in view.Hand)
            {
                unknown.Remove(card);
            }

            foreach (var card in view.GetPlayedCards())
            {
                unknown.Remove(card);
            }

            if (view.IsTrumpCardOnTable)
            {
                unknown.Remove(view.TrumpCard);
            }

            return unknown;
        }

        public static int TricksWonBy(SantaseSeatView view, PlayerPosition player)
        {
            return player == PlayerPosition.FirstPlayer ? view.FirstPlayerTricksWon : view.SecondPlayerTricksWon;
        }

        public static PlayerPosition Opponent(PlayerPosition player)
        {
            return player == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
        }
    }
}
