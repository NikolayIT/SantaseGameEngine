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

        public void StartGame()
        {
            this.logger.LogLine("New game");
            this.player.StartGame();
        }

        public void StartRound(IEnumerable<Card> playerCards, Card trumpCard)
        {
            var cardsAsString = new StringBuilder();
            foreach (var playerCard in playerCards)
            {
                cardsAsString.Append($"{playerCard} ");
            }

            this.logger.LogLine($"New round {cardsAsString}");
            this.player.StartRound(playerCards, trumpCard);
        }

        public void StartTurn(Card newCard)
        {
            this.logger.LogLine($"New turn {newCard}");
            this.player.StartTurn(newCard);
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
