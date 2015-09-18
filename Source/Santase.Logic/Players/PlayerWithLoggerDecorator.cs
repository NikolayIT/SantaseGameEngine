namespace Santase.Logic.Players
{
    using System.Collections.Generic;
    using System.Text;

    using Santase.Logic.Cards;
    using Santase.Logic.Logger;

    public class PlayerWithLoggerDecorator : IPlayer
    {
        private readonly IPlayer player;

        private readonly ILogger logger;

        public PlayerWithLoggerDecorator(IPlayer player, ILogger logger)
        {
            this.player = player;
            this.logger = logger;
        }

        public string Name => this.player.Name;

        public void StartGame(string otherPlayerIdentifier)
        {
            this.logger.LogLine($"New game vs {otherPlayerIdentifier}");
            this.player.StartGame(otherPlayerIdentifier);
        }

        public void StartRound(ICollection<Card> cards, Card trumpCard)
        {
            var cardsAsString = new StringBuilder();
            foreach (var card in cards)
            {
                cardsAsString.Append(card);
            }

            this.logger.LogLine($"New round. Cards: {cardsAsString}. Trump card: {trumpCard}");
            this.player.StartRound(cards, trumpCard);
        }

        public void AddCard(Card card)
        {
            this.logger.LogLine($"New card {card}");
            this.player.AddCard(card);
        }

        public PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.logger.LogLine("--GetTurn; "
                                + $"Trump: {context.TrumpCard}({context.CardsLeftInDeck}); "
                                + $"State: {context.State.GetType().Name.Replace("RoundState", string.Empty)}; "
                                + $"First: {context.FirstPlayedCard}({context.FirstPlayerAnnounce}); "
                                + $"I am first: {context.IsFirstPlayerTurn}");
            var action = this.player.GetTurn(context);
            this.logger.LogLine($"Playing {action}");
            return action;
        }

        public void EndTurn(PlayerTurnContext context)
        {
            this.logger.LogLine(
                $"End of turn {context.FirstPlayedCard}({context.FirstPlayerAnnounce}) - {context.SecondPlayedCard}");
            this.player.EndTurn(context);
        }

        public void EndRound()
        {
            this.player.EndRound();
            this.logger.LogLine("EndRound();");
            this.logger.LogLine(new string('-', 40));
        }

        public void EndGame(bool amIWinner)
        {
            this.player.EndGame(amIWinner);
            this.logger.LogLine($"EndGame({amIWinner});");
        }
    }
}
