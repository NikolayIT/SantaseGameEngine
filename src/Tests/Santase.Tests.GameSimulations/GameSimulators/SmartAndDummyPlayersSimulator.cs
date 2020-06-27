namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class SmartAndDummyPlayersSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new SmartPlayer();
            IPlayer secondPlayer = new DummyPlayer();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
