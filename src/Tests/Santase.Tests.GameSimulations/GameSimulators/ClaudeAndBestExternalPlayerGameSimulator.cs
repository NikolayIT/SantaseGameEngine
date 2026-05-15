namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.AI.NinjaPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    public class ClaudeAndBestExternalPlayerGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new ClaudePlayer();
            IPlayer secondPlayer = new NinjaPlayer();
            return new SantaseGame(firstPlayer, secondPlayer);
        }
    }
}
