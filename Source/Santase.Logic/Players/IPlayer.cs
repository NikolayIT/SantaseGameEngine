namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;

    public interface IPlayer
    {
        string Name { get; }

        void AddCard(Card card);

        PlayerAction GetTurn(PlayerTurnContext context);

        void EndTurn(PlayerTurnContext context);

        void EndRound();
    }
}
