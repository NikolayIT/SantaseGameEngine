namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.BotskoPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    public class SmartAndBestExternalPlayerGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new SmartPlayer(); // new PlayerWithLoggerDecorator(new SmartPlayer(), new ConsoleLogger("[-]"))
            IPlayer secondPlayer = new BotskoPlayer();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
