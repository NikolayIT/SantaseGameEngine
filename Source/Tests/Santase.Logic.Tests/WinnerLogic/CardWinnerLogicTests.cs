namespace Santase.Logic.Tests.WinnerLogic
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.WinnerLogic;

    using Xunit;

    public class CardWinnerLogicTests
    {
        [Theory]
        [MemberData(nameof(DataSource.FirstPlayerWins), MemberType = typeof(DataSource))]
        public void FirstPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.Equal(PlayerPosition.FirstPlayer, result);
        }

        [Theory]
        [MemberData(nameof(DataSource.SecondPlayerWins), MemberType = typeof(DataSource))]
        public void SecondPlayerShouldWins(Card firstPlayerCard, Card secondPlayerCard, CardSuit trumpSuit)
        {
            var cardWinner = new CardWinnerLogic();
            var result = cardWinner.Winner(firstPlayerCard, secondPlayerCard, trumpSuit);
            Assert.Equal(PlayerPosition.SecondPlayer, result);
        }

        public static class DataSource
        {
            public static readonly IEnumerable<object[]> FirstPlayerWins = new List<object[]>
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

            public static readonly IEnumerable<object[]> SecondPlayerWins = new List<object[]>
                {
                    new object[] { Card.GetCard(CardSuit.Club, CardType.Ten), Card.GetCard(CardSuit.Club, CardType.Ace), CardSuit.Diamond },
                    new object[] { Card.GetCard(CardSuit.Club, CardType.Ten), Card.GetCard(CardSuit.Club, CardType.Ace), CardSuit.Club },
                    new object[] { Card.GetCard(CardSuit.Spade, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Nine), CardSuit.Club },
                };
        }
    }
}
