namespace Santase.AI.DummyPlayer
{
    using System;
    using System.Linq;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// Dummy never changes the trump or closes the game.
    /// </summary>
    internal class DummyPlayer : BasePlayer, IRestorablePlayer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DummyPlayer"/> class.
        ///  </summary>
        /// <param name="name">The name of the player.</param>
        public DummyPlayer(string name = "Dummy Player Lvl. 1")
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        /// <value>The name of the player.</value>
        public override string Name { get; }

        /// <summary>
        /// Gets or sets the random source for the card choice. Defaults to <see cref="Random.Shared"/>;
        /// set a seeded one for reproducible games.
        /// </summary>
        public Random Rng { get; set; } = Random.Shared;

        /// <summary>
        /// Gets the player action by given player turn context.
        /// </summary>
        /// <param name="context">The player turn context information.</param>
        /// <returns>The player action.</returns>
        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);

            // Uniform random pick (same distribution as Shuffle().First()) without
            // materializing a shuffle buffer on every turn.
            var cardToPlay = possibleCardsToPlay.ElementAt(this.Rng.Next(possibleCardsToPlay.Count));
            return this.PlayCard(cardToPlay);
        }

        public void Restore(SantaseSeatView view)
        {
            // A dummy remembers nothing but its hand.
            this.RestoreHand(view);
        }
    }
}
