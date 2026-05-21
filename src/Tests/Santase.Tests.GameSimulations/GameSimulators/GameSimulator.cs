namespace Santase.Tests.GameSimulations.GameSimulators
{
    using System;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// A reusable head-to-head simulator: the two player factories are invoked once per game
    /// (players are stateful across rounds, so each game needs fresh instances), then the pair
    /// plays out via <see cref="BaseGameSimulator"/>. This replaces the former one-class-per-matchup
    /// explosion under this folder — matchups are now declared as data in <c>Program.cs</c>.
    /// </summary>
    public sealed class GameSimulator : BaseGameSimulator
    {
        private readonly Func<IPlayer> createFirstPlayer;
        private readonly Func<IPlayer> createSecondPlayer;
        private string name;

        public GameSimulator(Func<IPlayer> createFirstPlayer, Func<IPlayer> createSecondPlayer)
        {
            this.createFirstPlayer = createFirstPlayer;
            this.createSecondPlayer = createSecondPlayer;
        }

        public string Name =>
            this.name ??= $"{this.createFirstPlayer().GetType().Name} vs {this.createSecondPlayer().GetType().Name}";

        protected override ISantaseGame CreateGame()
        {
            return new SantaseGame(this.createFirstPlayer(), this.createSecondPlayer());
        }
    }
}
