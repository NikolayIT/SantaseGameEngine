namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class DummyPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame(PlayerPosition playerPosition)
        {
            IPlayer firstPlayer = new DummyPlayer("First Dummy Player");
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player");
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, playerPosition); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
