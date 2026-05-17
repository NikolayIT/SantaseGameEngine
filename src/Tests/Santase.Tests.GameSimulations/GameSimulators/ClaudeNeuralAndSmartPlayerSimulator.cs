namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    public class ClaudeNeuralAndSmartPlayerSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayerNeural();
            IPlayer secondPlayer = new SmartPlayer();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
