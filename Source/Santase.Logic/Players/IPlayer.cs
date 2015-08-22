using Santase.Logic.Cards;

namespace Santase.Logic.Players
{
    public interface IPlayer
    {
        void AddCard(Card card);

        PlayerAction GetTurn(
            PlayerTurnContext context,
            IPlayerActionValidater actionValidator);

        void EndTurn(PlayerTurnContext context);
    }
}
