namespace Santase.UI.Game
{
    using Santase.Logic.Cards;

    public static class CardImageProvider
    {
        public const string BackImage = "card_back.png";

        public static string For(Card card)
        {
            var rank = card.Type switch
            {
                CardType.Nine => "nine",
                CardType.Ten => "ten",
                CardType.Jack => "jack",
                CardType.Queen => "queen",
                CardType.King => "king",
                CardType.Ace => "ace",
                _ => "back",
            };

            var suit = card.Suit switch
            {
                CardSuit.Club => "club",
                CardSuit.Diamond => "diamond",
                CardSuit.Heart => "heart",
                CardSuit.Spade => "spade",
                _ => "back",
            };

            return $"card_{rank}{suit}.png";
        }
    }
}
