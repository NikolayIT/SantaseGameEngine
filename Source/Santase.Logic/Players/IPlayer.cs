namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public interface IPlayer
    {
        string Name { get; }

        void StartGame();

        void StartRound(IEnumerable<Card> playerCards, Card trumpCard);

        void StartTurn(Card newCard);

        PlayerAction GetTurn(PlayerTurnContext context);

        void EndTurn(PlayerTurnContext context);

        void EndRound();

        void EndGame(bool amIWinner);
    }
}
