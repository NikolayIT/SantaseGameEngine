namespace Santase.Logic.WinnerLogic
{
    using Santase.Logic.Cards;

    public class CardWinnerLogic : ICardWinnerLogic
    {
        public PlayerPosition Winner(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            if (firstPlayerCard.Suit == secondPlayerCard.Suit)
            {
                // If both players play the same suit, the higher card wins.
                return firstPlayerCard.GetValue() > secondPlayerCard.GetValue()
                           ? PlayerPosition.FirstPlayer
                           : PlayerPosition.SecondPlayer;
            }

            // If just one player plays a trump, the trump wins.
            if (secondPlayerCard.Suit == trumpSuit)
            {
                return PlayerPosition.SecondPlayer;
            }

            // If the players play non-trumps of different suits the card played by the first player wins.
            return PlayerPosition.FirstPlayer;
        }
    }
}
