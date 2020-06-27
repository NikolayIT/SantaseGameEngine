namespace Santase.AI.DummyPlayer
{
    using System.Linq;

    using Santase.Logic.Extensions;
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
            var shuffledCards = possibleCardsToPlay.Shuffle();
            var cardToPlay = shuffledCards.First();
            return this.PlayCard(cardToPlay);
        }
    }
}
