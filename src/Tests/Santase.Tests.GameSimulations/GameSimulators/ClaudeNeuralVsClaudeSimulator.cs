namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// Head-to-head between the neural-policy variant <see cref="ClaudePlayerNeural"/> and the
    /// hand-tuned heuristic <see cref="ClaudePlayer"/>. With Phase 1 random-Xavier weights the
    /// neural player is essentially a random-policy bot (its minimax still kicks in for the
    /// perfect-info endgame), so the heuristic should dominate here — useful as a baseline to
    /// measure improvement once trained weights are dropped in.
    /// </summary>
    public class ClaudeNeuralVsClaudeSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayerNeural();
            IPlayer secondPlayer = new ClaudePlayer();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
