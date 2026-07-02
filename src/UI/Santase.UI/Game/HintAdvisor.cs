namespace Santase.UI.Game
{
    using System.Collections.Generic;

    using Santase.AI.ClaudePlayer;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// The in-game hint engine: a shadow <see cref="ClaudePlayer"/> that mirrors the human's
    /// round lifecycle (StartRound / AddCard / EndTurn / EndRound, wired in
    /// <see cref="GameSession"/>) so its card tracking knows exactly what the human can know,
    /// and answers "what would Claude play here?" on demand. Claude's heuristic replies in
    /// well under 10 ms and its Phase-2 endgame is an exact solve, so hints are both instant
    /// and strong.
    /// <para>
    /// <see cref="ClaudePlayer.GetTurn"/> mutates the hand through the BasePlayer helpers
    /// (playing removes the card, a trump swap trades the 9 for the trump), so the hint call
    /// syncs the hand from the human's snapshot before asking and restores it afterwards.
    /// </para>
    /// </summary>
    internal sealed class HintAdvisor : ClaudePlayer
    {
        public PlayerAction? ComputeHint(IReadOnlyList<Card> humanCards, PlayerTurnContext context)
        {
            try
            {
                this.SetHand(humanCards);
                var action = this.GetTurn(context.DeepClone());
                this.SetHand(humanCards);
                return action;
            }
            catch
            {
                // A hint must never be able to break the game; fail silently instead.
                return null;
            }
        }

        private void SetHand(IReadOnlyList<Card> cards)
        {
            this.Cards.Clear();
            foreach (var card in cards)
            {
                this.Cards.Add(card);
            }
        }
    }
}
