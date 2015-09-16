namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class SmartPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame(PlayerPosition playerPosition)
        {
            IPlayer firstPlayer = new SmartPlayer(); // new PlayerWithLoggerDecorator(new SmartPlayer(), new ConsoleLogger("[-]"))
            IPlayer secondPlayer = new SmartPlayerOld();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, playerPosition); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
