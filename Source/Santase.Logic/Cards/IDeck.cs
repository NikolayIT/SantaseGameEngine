namespace Santase.Logic.Cards
{
    public interface IDeck
    {
        Card GetNextCard();

        Card GetTrumpCard { get; }

        void ChangeTrumpCard(Card newCard);

        int CardsLeft { get; }
    }
}
