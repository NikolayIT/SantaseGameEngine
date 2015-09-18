namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class SmartPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new SmartPlayer(); // new PlayerWithLoggerDecorator(new SmartPlayer(), new ConsoleLogger("[-]"))
            IPlayer secondPlayer = new SmartPlayerOld();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
