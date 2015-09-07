namespace Santase.AI.DummyPlayer
{
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// When possible Dummy changes the trump.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class DummyPlayerChangingTrump : DummyPlayer
    {
        public DummyPlayerChangingTrump(string name = "Dummy Player Lvl. 2")
            : base(name)
        {
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard.Suit);
            }

            return base.GetTurn(context);
        }
    }
}
