namespace Santase.Logic.GameMechanics
{
    using System;

    using Santase.Logic.Logger;

    /// <summary>
    /// Settings for a <see cref="SantaseMatch"/>. Every property has a usable default.
    /// </summary>
    public sealed class SantaseMatchOptions
    {
        /// <summary>
        /// Gets or sets who leads the first round. Later rounds are led by the loser of the
        /// previous round (after a drawn round, by the same player).
        /// </summary>
        public PlayerPosition FirstToPlay { get; set; } = PlayerPosition.FirstPlayer;

        /// <summary>
        /// Gets or sets the rules; null means <see cref="GameRulesProvider.Santase"/>.
        /// </summary>
        public IGameRules Rules { get; set; }

        /// <summary>
        /// Gets or sets the random source for every deal of the match: it returns a uniformly
        /// distributed integer in [0, exclusiveMax). See <see cref="Cards.Deck"/> for the exact
        /// draw order. Null means <see cref="Random.Shared"/>.
        /// </summary>
        public Func<int, int> Shuffle { get; set; }

        /// <summary>
        /// Gets or sets the logger that receives one line per round ("firstPoints - secondPoints");
        /// null means none.
        /// </summary>
        public ILogger Logger { get; set; }
    }
}
