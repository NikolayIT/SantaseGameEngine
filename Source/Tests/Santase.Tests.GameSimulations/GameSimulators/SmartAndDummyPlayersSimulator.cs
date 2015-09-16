namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class SmartAndDummyPlayersSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame(PlayerPosition playerPosition)
        {
            IPlayer firstPlayer = new SmartPlayer();
            IPlayer secondPlayer = new DummyPlayerChangingTrump();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, playerPosition); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
