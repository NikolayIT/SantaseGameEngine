namespace Santase.Logic
{
    using Santase.Logic.Cards;

    public interface ICardWinnerLogic
    {
        PlayerPosition Winner(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit);
    }
}
