namespace Santase.Logic.Cards
{
    public interface IDeck
    {
        Card TrumpCard { get; }

        int CardsLeft { get; }

        Card GetNextCard();

        void ChangeTrumpCard(Card newCard);
    }
}
