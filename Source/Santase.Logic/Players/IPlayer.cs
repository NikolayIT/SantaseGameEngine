namespace Santase.Logic.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public interface IPlayer
    {
        string Name { get; }

        void StartGame(string otherPlayerIdentifier);

        void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints);

        void AddCard(Card card);

        PlayerAction GetTurn(PlayerTurnContext context);

        void EndTurn(PlayerTurnContext context);

        void EndRound();

        void EndGame(bool amIWinner);
    }
}
