namespace Santase.AI.DummyPlayer
{
    using System;
    using System.Linq;

    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// When possible Dummy changes the trump.
    /// </summary>
    public class DummyPlayerChangingTrump : BasePlayer
    {
        public DummyPlayerChangingTrump()
        {
            this.Name = "Dummy Player Lvl. 2";
        }

        public override string Name { get; }

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
            var cardToPlay = possibleCardsToPlay.ElementAt(Random.Shared.Next(possibleCardsToPlay.Count));
            return this.PlayCard(cardToPlay);
        }
    }
}
