namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;

    public interface IPlayer
    {
        string Name { get; }

        void AddCard(Card card);

        // TODO: Reconsider removing IPlayerActionValidator as a parameter
        PlayerAction GetTurn(PlayerTurnContext context, IPlayerActionValidator actionValidator);

        void EndTurn(PlayerTurnContext context);

        void EndRound();
    }
}
