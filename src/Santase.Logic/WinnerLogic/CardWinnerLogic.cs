namespace Santase.Logic.WinnerLogic
{
    using Santase.Logic.Cards;

    public class CardWinnerLogic : ICardWinnerLogic
    {
        public PlayerPosition Winner(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            return GetWinner(firstPlayerCard, secondPlayerCard, trumpSuit);
        }

        // The trick winner given the led card (first), the response (second) and the trump suit.
        // This is the engine's authoritative trick rule, exposed as a pure, allocation-free static
        // so card players can reuse it in their search / evaluation loops instead of re-deriving the
        // same comparison. The instance Winner above (and the ICardWinnerLogic seam) delegate here.
        public static PlayerPosition GetWinner(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
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
