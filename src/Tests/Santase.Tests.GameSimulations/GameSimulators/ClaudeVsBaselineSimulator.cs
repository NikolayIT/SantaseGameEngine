namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// Head-to-head between the current <see cref="ClaudePlayer"/> (under iteration) and the
    /// frozen <see cref="ClaudePlayerBaseline"/> snapshot. Use this to measure whether a
    /// candidate change is actually an improvement.
    /// </summary>
    public class ClaudeVsBaselineSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayer();
            IPlayer secondPlayer = new ClaudePlayerBaseline();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
