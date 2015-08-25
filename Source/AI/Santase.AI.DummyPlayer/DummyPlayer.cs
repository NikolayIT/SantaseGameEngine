namespace Santase.AI.DummyPlayer
{
    using Santase.Logic;
    using Santase.Logic.Extensions;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class DummyPlayer : BasePlayer
    {
        public override PlayerAction GetTurn(PlayerTurnContext context, IPlayerActionValidator actionValidator)
        {
            if (actionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return PlayerAction.ChangeTrump();
            }

            foreach (var card in this.Cards.Shuffle())
            {
                var action = PlayerAction.PlayCard(card, Announce.None);
                if (actionValidator.IsValid(action, context, this.Cards))
                {
                    return action;
                }
            }

            // Should never happen
            return PlayerAction.PlayCard(this.Cards[0], Announce.None);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
        }
    }
}
