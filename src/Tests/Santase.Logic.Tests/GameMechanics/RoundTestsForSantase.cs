namespace Santase.Logic.Tests.GameMechanics
{
    using Santase.Logic.GameMechanics;

    using Xunit;

    public class RoundTestsForSantase
    {
        [Fact]
        public void PlayersStartRoundAndEndRoundShouldBeCalledAndShouldReceiveEqualNumberOfCards()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play(0, 0);

            Assert.Equal(firstPlayer.AddCardCalledCount, secondPlayer.AddCardCalledCount);

            Assert.Equal(1, firstPlayer.StartRoundCalledCount);
            Assert.Equal(1, secondPlayer.StartRoundCalledCount);

            Assert.Equal(1, firstPlayer.EndRoundCalledCount);
            Assert.Equal(1, secondPlayer.EndRoundCalledCount);

            Assert.True(firstPlayer.AddCardCalledCount >= 2);
            Assert.True(secondPlayer.AddCardCalledCount >= 2);
            Assert.True(firstPlayer.AddCardCalledCount <= GameRulesProvider.Santase.CardsAtStartOfTheRound);
            Assert.True(secondPlayer.AddCardCalledCount <= GameRulesProvider.Santase.CardsAtStartOfTheRound);
        }

        [Fact]
        public void PlayersStartRoundShouldBeCalledWithCorrectScoreValues()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            round.Play(9, 4);

            Assert.Equal(9, firstPlayer.MyTotalPoints);
            Assert.Equal(4, firstPlayer.OpponentTotalPoints);
            Assert.Equal(4, secondPlayer.MyTotalPoints);
            Assert.Equal(9, secondPlayer.OpponentTotalPoints);
        }

        [Fact]
        public void PlayShouldReturnValidRoundResultObject()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            var result = round.Play(0, 0);

            Assert.True(
                result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick,
                "result.FirstPlayer.HasAtLeastOneTrick || result.SecondPlayer.HasAtLeastOneTrick");

            Assert.True(
                result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0,
                "result.FirstPlayer.RoundPoints > 0 || result.SecondPlayer.RoundPoints > 0");

            Assert.True(
                result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0,
                "result.FirstPlayer.TrickCards.Count > 0 || result.SecondPlayer.TrickCards.Count > 0");

            Assert.True(
                result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66
                || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120,
                "result.FirstPlayer.RoundPoints >= 66 || result.SecondPlayer.RoundPoints >= 66 || result.FirstPlayer.RoundPoints + result.SecondPlayer.RoundPoints >= 120");
        }

        [Fact]
        public void PlayShouldSetLastTrickWinnerOnlyWhenBothHandsEndUpEmpty()
        {
            // LastTrickWinner is the gate Round.cs uses to tell scoring whether the +10
            // bonus is in play. The invariant: it must be a real player when both hands
            // are empty at end of round, and NoOne otherwise. Run enough rounds with
            // random-ish play to exercise both natural-exhaustion and 66-reached-mid-round.
            const int NumberOfRounds = 1000;

            var naturalEndCount = 0;
            var earlyEndCount = 0;

            for (var i = 0; i < NumberOfRounds; i++)
            {
                var firstPlayer = new ValidPlayerWithMethodsCallCounting();
                var secondPlayer = new ValidPlayerWithMethodsCallCounting();
                var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

                var result = round.Play(0, 0);

                var bothHandsEmpty =
                    result.FirstPlayer.Cards.Count == 0 && result.SecondPlayer.Cards.Count == 0;

                if (bothHandsEmpty)
                {
                    Assert.True(
                        result.LastTrickWinner == PlayerPosition.FirstPlayer
                            || result.LastTrickWinner == PlayerPosition.SecondPlayer,
                        "When both hands are empty, LastTrickWinner must be a real player so scoring can award the +10 bonus");
                    naturalEndCount++;
                }
                else
                {
                    Assert.Equal(PlayerPosition.NoOne, result.LastTrickWinner);
                    earlyEndCount++;
                }
            }

            // Both code paths must be hit, otherwise the test isn't actually covering the gate.
            Assert.True(naturalEndCount > 0, "expected some rounds to end via natural talon exhaustion");
            Assert.True(earlyEndCount > 0, "expected some rounds to end early via a player reaching 66");
        }

        [Fact]
        public void PlayersMethodsShouldBeCalledCorrectNumberOfTimes()
        {
            const int NumberOfRounds = 10000;

            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();

            for (var i = 0; i < NumberOfRounds; i++)
            {
                var round = i % 2 == 0
                                ? new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase)
                                : new Round(secondPlayer, firstPlayer, GameRulesProvider.Santase);

                round.Play(0, 0);
            }

            Assert.Equal(firstPlayer.StartRoundCalledCount, NumberOfRounds);
            Assert.Equal(secondPlayer.StartRoundCalledCount, NumberOfRounds);

            Assert.Equal(firstPlayer.EndRoundCalledCount, NumberOfRounds);
            Assert.Equal(secondPlayer.EndRoundCalledCount, NumberOfRounds);

            Assert.True(firstPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.True(firstPlayer.GetTurnWhenSecond > NumberOfRounds);
            Assert.True(secondPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.True(secondPlayer.GetTurnWhenSecond > NumberOfRounds);

            Assert.True(firstPlayer.GetTurnWhenFirst >= secondPlayer.GetTurnWhenSecond);
            Assert.True(secondPlayer.GetTurnWhenFirst >= firstPlayer.GetTurnWhenSecond);
        }
    }
}
