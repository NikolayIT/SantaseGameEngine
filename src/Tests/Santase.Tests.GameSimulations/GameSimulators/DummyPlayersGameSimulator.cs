namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.DummyPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class DummyPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new DummyPlayer("First Dummy Player");
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player");
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
