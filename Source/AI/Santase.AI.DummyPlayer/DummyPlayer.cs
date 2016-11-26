namespace Santase.AI.DummyPlayer
{
    using System.Linq;

    using Santase.Logic.Extensions;
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// Dummy never changes the trump or closes the game.
    /// </summary>
    internal class DummyPlayer : BasePlayer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DummyPlayer"/> class.
        ///  </summary>
        /// <param name="name">The name of the player</param>
        public DummyPlayer(string name = "Dummy Player Lvl. 1")
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the player
        /// </summary>
        /// <value>The name of the player</value>
        public override string Name { get; }

        /// <summary>
        /// Gets the player action by given player turn context
        /// </summary>
        /// <param name="context">The player turn context information</param>
        /// <returns>The player action</returns>
        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            var shuffledCards = possibleCardsToPlay.Shuffle();
            var cardToPlay = shuffledCards.First();
            return this.PlayCard(cardToPlay);
        }
    }
}
