namespace Santase.Logic.Tests.WinnerLogic
{
    using Santase.Logic.Cards;
    using Santase.Logic.WinnerLogic;

    using Xunit;

    public class CardWinnerLogicTests
    {
        private static readonly object[] FirstPlayerWins =
            {
                new object[] { Card.GetCard(CardSuit.Club, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Ten), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Club, CardType.Ten), Card.GetCard(CardSuit.Club, CardType.King), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Club, CardType.King), Card.GetCard(CardSuit.Club, CardType.Queen), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Club, CardType.Queen), Card.GetCard(CardSuit.Club, CardType.Jack), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Club, CardType.Jack), Card.GetCard(CardSuit.Club, CardType.Nine), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Club, CardType.Ace), Card.GetCard(CardSuit.Diamond, CardType.Jack), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Ace), CardSuit.Spade },
                new object[] { Card.GetCard(CardSuit.Heart, CardType.Ten), Card.GetCard(CardSuit.Heart, CardType.Nine), CardSuit.Heart },
                new object[] { Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Diamond, CardType.King), CardSuit.Heart },
            };

        private static readonly object[] SecondPlayerWins =
            {
                new object[] { Card.GetCard(CardSuit.Club, CardType.Ten), Card.GetCard(CardSuit.Club, CardType.Ace), CardSuit.Diamond },
                new object[] { Card.GetCard(CardSuit.Club, CardType.Ten), Card.GetCard(CardSuit.Club, CardType.Ace), CardSuit.Club },
                new object[] { Card.GetCard(CardSuit.Spade, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Nine), CardSuit.Club },
            };

        [TestCaseSource(nameof(FirstPlayerWins))]
        public void FirstPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.Equal(PlayerPosition.FirstPlayer, result);
        }

        [TestCaseSource(nameof(SecondPlayerWins))]
        public void SecondPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.Equal(PlayerPosition.SecondPlayer, result);
        }
    }
}
