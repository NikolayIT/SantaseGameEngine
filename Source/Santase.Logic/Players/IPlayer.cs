namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    public interface IPlayer
    {
        string Name { get; }

        void AddCard(Card card);

        PlayerAction GetTurn(PlayerTurnContext context);

        void EndTurn(PlayerTurnContext context);

        void EndRound();
    }
}
