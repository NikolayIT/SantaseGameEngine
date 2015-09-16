namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;

    public interface IPlayer
    {
        string Name { get; }

        void StartGame(string otherPlayerIdentifier);

        void AddCard(Card card);

        PlayerAction GetTurn(PlayerTurnContext context);

        void EndTurn(PlayerTurnContext context);

        void EndRound();

        void EndGame(bool amIWinner);
    }
}
