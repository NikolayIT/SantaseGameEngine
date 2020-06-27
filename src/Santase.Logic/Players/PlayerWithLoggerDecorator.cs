namespace Santase.Logic.Players
{
    using System.Collections.Generic;
    using System.Text;

    using Santase.Logic.Cards;
    using Santase.Logic.Logger;

    public class PlayerWithLoggerDecorator : PlayerDecorator
    {
        private readonly ILogger logger;

        public PlayerWithLoggerDecorator(IPlayer player, ILogger logger)
            : base(player)
        {
            this.logger = logger;
        }

        public override void StartGame(string otherPlayerIdentifier)
        {
            this.logger.LogLine($"New game vs {otherPlayerIdentifier}");
            base.StartGame(otherPlayerIdentifier);
        }

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            var cardsAsString = new StringBuilder();
            foreach (var card in cards)
            {
                cardsAsString.Append(card);
            }

            this.logger.LogLine($"New round ({myTotalPoints}-{opponentTotalPoints}). Cards: {cardsAsString}. Trump card: {trumpCard}");
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
        }

        public override void AddCard(Card card)
        {
            this.logger.LogLine($"New card {card}");
            base.AddCard(card);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.logger.LogLine("--GetTurn; "
                                + $"Trump: {context.TrumpCard}({context.CardsLeftInDeck}); "
                                + $"State: {context.State.GetType().Name.Replace("RoundState", string.Empty)}; "
                                + $"First: {context.FirstPlayedCard}({context.FirstPlayerAnnounce}); "
                                + $"I am first: {context.IsFirstPlayerTurn}");
            var action = base.GetTurn(context);
            this.logger.LogLine($"Playing {action}");
            return action;
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.logger.LogLine(
                $"End of turn {context.FirstPlayedCard}({context.FirstPlayerAnnounce}) - {context.SecondPlayedCard}");
            base.EndTurn(context);
        }

        public override void EndRound()
        {
            base.EndRound();
            this.logger.LogLine("EndRound();");
            this.logger.LogLine(new string('-', 40));
        }

        public override void EndGame(bool amIWinner)
        {
            base.EndGame(amIWinner);
            this.logger.LogLine($"EndGame({amIWinner});");
        }
    }
}
