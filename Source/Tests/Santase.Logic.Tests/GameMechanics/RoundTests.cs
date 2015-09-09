namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // These tests can be improved. When bug is found regression should be added.
    [TestFixture]
    public class RoundTestsForSantase
    {
        [Test]
        public void PlayersShouldReceiveEqualNumberOfCardsAtLeast6AndNoMoreThan12()
        {
            var firstPlayer = new ValidPlayer();
            var secondPlayer = new ValidPlayer();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play();

            Assert.IsTrue(firstPlayer.AddCardCalledCount == secondPlayer.AddCardCalledCount);

            Assert.IsTrue(firstPlayer.AddCardCalledCount >= 6);
            Assert.IsTrue(secondPlayer.AddCardCalledCount >= 6);

            Assert.IsTrue(firstPlayer.AddCardCalledCount <= 12);
            Assert.IsTrue(secondPlayer.AddCardCalledCount <= 12);
        }

        [Test]
        public void PlayShouldReturnValidRoundResultObject()
        {
            var firstPlayer = new ValidPlayer();
            var secondPlayer = new ValidPlayer();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            var result = round.Play();

            Assert.IsTrue(result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick);
            Assert.IsTrue(result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0);
            Assert.IsTrue(result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0);
            Assert.IsTrue(
                result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66
                || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120);
        }

        [Test]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int NumberOfRounds = 10000;

            var firstPlayer = new ValidPlayer();
            var secondPlayer = new ValidPlayer();

            for (var i = 0; i < NumberOfRounds; i++)
            {
                Round round;
                if (i % 2 == 0)
                {
                    round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);
                }
                else
                {
                    round = new Round(secondPlayer, firstPlayer, GameRulesProvider.Santase);
                }

                round.Play();
            }

            Assert.IsTrue(firstPlayer.EndRoundCalledCount == NumberOfRounds);
            Assert.IsTrue(secondPlayer.EndRoundCalledCount == NumberOfRounds);

            Assert.IsTrue(firstPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.IsTrue(firstPlayer.GetTurnWhenSecond > NumberOfRounds);
            Assert.IsTrue(secondPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.IsTrue(secondPlayer.GetTurnWhenSecond > NumberOfRounds);

            Assert.IsTrue(firstPlayer.AddCardCalledCount >= firstPlayer.GetTurnCalledCount);
            Assert.IsTrue(secondPlayer.AddCardCalledCount >= secondPlayer.GetTurnCalledCount);

            Assert.IsTrue(firstPlayer.GetTurnWhenFirst >= secondPlayer.GetTurnWhenSecond);
            Assert.IsTrue(secondPlayer.GetTurnWhenFirst >= firstPlayer.GetTurnWhenSecond);
        }

        private class ValidPlayer : BasePlayer
        {
            public override string Name => "Valid player";

            public int GetTurnCalledCount { get; private set; }

            public int GetTurnWhenFirst { get; private set; }

            public int GetTurnWhenSecond { get; private set; }

            public int AddCardCalledCount { get; private set; }

            public int EndTurnCalledCount { get; private set; }

            public int EndRoundCalledCount { get; private set; }

            public override void AddCard(Card card)
            {
                this.AddCardCalledCount++;
                base.AddCard(card);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.GetTurnCalledCount++;
                if (context.IsFirstPlayerTurn)
                {
                    this.GetTurnWhenFirst++;
                }
                else
                {
                    this.GetTurnWhenSecond++;
                }

                var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(possibleCardsToPlay.First());
            }

            public override void EndTurn(PlayerTurnContext context)
            {
                this.EndTurnCalledCount++;
                base.EndTurn(context);
            }

            public override void EndRound()
            {
                this.EndRoundCalledCount++;
                base.EndRound();
            }
        }
    }
}
