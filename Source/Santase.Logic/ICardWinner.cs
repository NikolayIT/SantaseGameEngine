namespace Santase.Logic
{
    using Santase.Logic.Cards;

    public interface ICardWinner
    {
        PlayerPosition Winner(
            Card firstPlayerCard,
            Card secondPlayerCard,
            CardSuit trumpSuit);
    }
}
