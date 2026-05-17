namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    public class ClaudeNeuralAndDummyPlayersSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayerNeural();
            IPlayer secondPlayer = new DummyPlayer();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
