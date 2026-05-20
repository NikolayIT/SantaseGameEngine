namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    public class ClaudeBaselineAndDummyPlayerChangingTrumpSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayerBaseline();
            IPlayer secondPlayer = new DummyPlayerChangingTrump();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
