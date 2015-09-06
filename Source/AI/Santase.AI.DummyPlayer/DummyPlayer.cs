namespace Santase.AI.DummyPlayer
{
    using Santase.Logic;
    using Santase.Logic.Extensions;
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// Dummy never changes the trump or closes the game.
    /// </summary>
    public class DummyPlayer : BasePlayer
    {
        // ReSharper disable once UnusedMember.Global
        public DummyPlayer()
            : this("Dummy Player")
        {
        }

        // ReSharper disable once UnusedMember.Global
        public DummyPlayer(string name)
        {
            this.Name = name;
        }

        public override string Name { get; }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var shuffledCards = this.Cards.Shuffle();
            foreach (var card in shuffledCards)
            {
                var action = PlayerAction.PlayCard(card);
                if (this.PlayerActionValidator.IsValid(action, context, this.Cards))
                {
                    return this.PlayCard(card);
                }
            }

            // Should never happen
            throw new InternalGameException("Out of possible cards to play!");
        }

        public override void EndTurn(PlayerTurnContext context)
        {
        }
    }
}
