namespace Santase.AI.DummyPlayer
{
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Extensions;
    using Santase.Logic.Logger;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class DummyPlayer : BasePlayer
    {
        private readonly ILogger logger;

        public DummyPlayer()
            : this(new NoLogger())
        {
        }

        public DummyPlayer(ILogger logger)
        {
            this.logger = logger;
        }

        public override void AddCard(Card card)
        {
            this.logger.LogLine($"Got card {card}");
            base.AddCard(card);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context, IPlayerActionValidator actionValidator)
        {
            if (actionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                this.logger.LogLine("Changing trump.");
                this.Cards.Remove(new Card(context.TrumpCard.Suit, CardType.Nine));
                return PlayerAction.ChangeTrump();
            }

            foreach (var card in this.Cards.Shuffle())
            {
                var action = PlayerAction.PlayCard(card, Announce.None);
                if (actionValidator.IsValid(action, context, this.Cards))
                {
                    this.logger.LogLine($"Playing {card}");
                    this.Cards.Remove(card);
                    return action;
                }
            }

            // Should never happen
            this.logger.LogLine($"Out of possible cards to play! This should never happen!");
            return PlayerAction.PlayCard(this.Cards[0], Announce.None);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.logger.LogLine($"End of turn {context.FirstPlayedCard} - {context.SecondPlayedCard}");
        }
    }
}
