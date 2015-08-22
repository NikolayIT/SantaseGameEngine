using Santase.Logic.Cards;

namespace Santase.Logic
{
    public interface ICardWinner
    {
        PlayerPosition Winner(
            Card firstPlayerCard,
            Card secondPlayerCard,
            CardSuit trumpSuit);
    }
}
