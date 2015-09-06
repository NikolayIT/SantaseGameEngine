namespace Santase.Logic.Tests.WinnerLogic
{
    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.WinnerLogic;

    [TestFixture]
    public class CardWinnerLogicTests
    {
        private static readonly object[] FirstPlayerWins =
            {
                new object[] { new Card(CardSuit.Club, CardType.Ace), new Card(CardSuit.Club, CardType.Ten), CardSuit.Spade },
                new object[] { new Card(CardSuit.Club, CardType.Ten), new Card(CardSuit.Club, CardType.King), CardSuit.Spade },
                new object[] { new Card(CardSuit.Club, CardType.King), new Card(CardSuit.Club, CardType.Queen), CardSuit.Spade },
                new object[] { new Card(CardSuit.Club, CardType.Queen), new Card(CardSuit.Club, CardType.Jack), CardSuit.Spade },
                new object[] { new Card(CardSuit.Club, CardType.Jack), new Card(CardSuit.Club, CardType.Nine), CardSuit.Spade },
                new object[] { new Card(CardSuit.Club, CardType.Ace), new Card(CardSuit.Diamond, CardType.Jack), CardSuit.Spade },
                new object[] { new Card(CardSuit.Heart, CardType.Jack), new Card(CardSuit.Diamond, CardType.Ace), CardSuit.Spade },
                new object[] { new Card(CardSuit.Heart, CardType.Ten), new Card(CardSuit.Heart, CardType.Nine), CardSuit.Heart },
                new object[] { new Card(CardSuit.Heart, CardType.Nine), new Card(CardSuit.Diamond, CardType.King), CardSuit.Heart },
            };

        private static readonly object[] SecondPlayerWins =
            {
                new object[] { new Card(CardSuit.Club, CardType.Ten), new Card(CardSuit.Club, CardType.Ace), CardSuit.Diamond },
                new object[] { new Card(CardSuit.Club, CardType.Ten), new Card(CardSuit.Club, CardType.Ace), CardSuit.Club },
                new object[] { new Card(CardSuit.Spade, CardType.Ace), new Card(CardSuit.Club, CardType.Nine), CardSuit.Club },
            };

        [TestCaseSource(nameof(FirstPlayerWins))]
        public void FirstPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.AreEqual(PlayerPosition.FirstPlayer, result);
        }

        [TestCaseSource(nameof(SecondPlayerWins))]
        public void SecondPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.AreEqual(PlayerPosition.SecondPlayer, result);
        }
    }
}
