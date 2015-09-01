namespace Santase.AI.DummyPlayer
{
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Extensions;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    public class DummyPlayer : BasePlayer
    {
        private readonly ILogger logger;

        // ReSharper disable once UnusedMember.Global
        public DummyPlayer(string name)
            : this(name, new NoLogger())
        {
        }

        public DummyPlayer(string name, ILogger logger)
        {
            this.Name = name;
            this.logger = logger;
        }

        public override string Name { get; }

        public override void AddCard(Card card)
        {
            this.logger.LogLine($"Got card {card}");
            base.AddCard(card);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var shuffledCards = this.Cards.Shuffle();
            foreach (var card in shuffledCards)
            {
                var action = PlayerAction.PlayCard(card);
                if (this.PlayerActionValidator.IsValid(action, context, this.Cards))
                {
                    this.logger.LogLine($"Playing {card}");
                    return this.PlayCard(card);
                }
            }

            // Should never happen
            throw new InternalGameException("Out of possible cards to play!");
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.logger.LogLine($"End of turn {context.FirstPlayedCard} - {context.SecondPlayedCard}");
        }
    }
}
