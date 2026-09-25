namespace Santase.AI.DummyPlayer
{
    using System;
    using System.Linq;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// When possible Dummy changes the trump.
    /// </summary>
    public class DummyPlayerChangingTrump : BasePlayer, IRestorablePlayer
    {
        public DummyPlayerChangingTrump()
        {
            this.Name = "Dummy Player Lvl. 2";
        }

        public override string Name { get; }

        /// <summary>
        /// Gets or sets the random source for the card choice. Defaults to <see cref="Random.Shared"/>;
        /// set a seeded one for reproducible games.
        /// </summary>
        public Random Rng { get; set; } = Random.Shared;

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

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
