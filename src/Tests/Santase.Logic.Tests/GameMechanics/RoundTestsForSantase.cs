namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class RoundTestsForSantase
    {
        [Fact]
        public void PlayersStartRoundAndEndRoundShouldBeCalledAndShouldReceiveEqualNumberOfCards()
        {
            var firstPlayer = new ValidPlayerWithMethodsCallCounting();
            var secondPlayer = new ValidPlayerWithMethodsCallCounting();
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

            RoundTestDriver.Play(round, 0, 0);

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

            RoundTestDriver.Play(round, 9, 4);

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

            var result = RoundTestDriver.Play(round, 0, 0);

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
            // LastTrickWinner is the gate the round uses to tell scoring whether the +10
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

                var result = RoundTestDriver.Play(round, 0, 0);

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

                RoundTestDriver.Play(round, 0, 0);
            }

            Assert.Equal(NumberOfRounds, firstPlayer.StartRoundCalledCount);
            Assert.Equal(NumberOfRounds, secondPlayer.StartRoundCalledCount);

            Assert.Equal(NumberOfRounds, firstPlayer.EndRoundCalledCount);
            Assert.Equal(NumberOfRounds, secondPlayer.EndRoundCalledCount);

            Assert.True(firstPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.True(firstPlayer.GetTurnWhenSecond > NumberOfRounds);
            Assert.True(secondPlayer.GetTurnWhenFirst > NumberOfRounds);
            Assert.True(secondPlayer.GetTurnWhenSecond > NumberOfRounds);

            Assert.True(firstPlayer.GetTurnWhenFirst >= secondPlayer.GetTurnWhenSecond);
            Assert.True(secondPlayer.GetTurnWhenFirst >= firstPlayer.GetTurnWhenSecond);
        }

        [Fact]
        public void StartShouldDealFromTheTopFirstPlayerFirstAndShowBothTheSameTrump()
        {
            // The golden deal vector (see DeckTests): the first player receives the six top cards
            // in draw order, then the second player the next six.
            var draws = new[] { 17, 3, 21, 0, 12, 16, 5, 8, 14, 2, 11, 7, 10, 4, 9, 1, 6, 3, 0, 4, 2, 1, 1 };
            var next = 0;
            var log = new List<string>();
            var firstPlayer = new DealRecordingPlayer("first", log);
            var secondPlayer = new DealRecordingPlayer("second", log);
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase, PlayerPosition.FirstPlayer, _ => draws[next++]);

            round.Start(0, 0);

            Assert.Equal(TestCards.List("AH QC QS 9C 9H KH"), firstPlayer.Hand);
            Assert.Equal(TestCards.List("AC JD JH JC AD 10D"), secondPlayer.Hand);
            Assert.Equal(TestCards.Parse("AS"), firstPlayer.TrumpCard);
            Assert.Equal(TestCards.Parse("AS"), secondPlayer.TrumpCard);
            Assert.Equal(new[] { "first", "second" }, log);
            Assert.Equal(12, round.Deck.CardsLeft);
        }

        [Theory]
        [InlineData(PlayerPosition.FirstPlayer)]
        [InlineData(PlayerPosition.SecondPlayer)]
        public void TheFirstToPlayShouldLeadTheFirstTrick(PlayerPosition firstToPlay)
        {
            var round = new Round(new ValidPlayerWithMethodsCallCounting(), new ValidPlayerWithMethodsCallCounting(), GameRulesProvider.Santase, firstToPlay);

            round.Start(0, 0);

            Assert.Equal(firstToPlay, round.ToMove);
            Assert.True(round.CreateTurnContext().IsFirstPlayerTurn);
        }

        [Fact]
        public void ConstructorShouldRejectNoOneAsTheFirstToPlay()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Round(null, null, GameRulesProvider.Santase, PlayerPosition.NoOne));
        }

        [Fact]
        public void NobodyShouldBeToMoveBeforeTheDealOrAfterTheRound()
        {
            var round = new Round(new ValidPlayerWithMethodsCallCounting(), new ValidPlayerWithMethodsCallCounting(), GameRulesProvider.Santase);
            Assert.Equal(PlayerPosition.NoOne, round.ToMove);
            Assert.Throws<InvalidOperationException>(() => round.CreateTurnContext());

            RoundTestDriver.Play(round, 0, 0);

            Assert.True(round.IsFinished);
            Assert.NotNull(round.Result);
            Assert.Equal(PlayerPosition.NoOne, round.ToMove);
            Assert.Throws<InvalidOperationException>(() => round.CreateTurnContext());
            Assert.Throws<InvalidOperationException>(() => round.TryAct(PlayerAction.CloseGame()));
        }

        [Fact]
        public void TryActShouldRejectAnIllegalActionAndChangeNothing()
        {
            var round = new Round(null, null, GameRulesProvider.Santase, PlayerPosition.FirstPlayer, StackedDeal.Create("AS", "AH QC QS 9C 9H KH", "AC JD JH JC AD 10D"));
            round.Start(0, 0);
            var contextBefore = round.CreateTurnContext();

            Assert.False(round.TryAct(PlayerAction.PlayCard(TestCards.Parse("AC")))); // the opponent's card
            Assert.False(round.TryAct(null));
            Assert.False(round.TryAct(PlayerAction.CloseGame())); // not in the first trick
            Assert.False(round.TryAct(PlayerAction.ChangeTrump())); // no nine of trumps, first trick

            Assert.Equal(PlayerPosition.FirstPlayer, round.ToMove);
            Assert.Equal(TestCards.List("AH QC QS 9C 9H KH").OrderBy(c => c.GetHashCode()), round.FirstPlayer.Cards);
            var contextAfter = round.CreateTurnContext();
            Assert.Null(contextAfter.FirstPlayedCard);
            Assert.Equal(contextBefore.TrumpCard, contextAfter.TrumpCard);
            Assert.Same(contextBefore.State, contextAfter.State);

            Assert.True(round.TryAct(PlayerAction.PlayCard(TestCards.Parse("9C"))));
            Assert.Equal(PlayerPosition.SecondPlayer, round.ToMove);
        }

        [Fact]
        public void TricksPlayedShouldCountEveryFinishedTrick()
        {
            var round = new Round(new ValidPlayerWithMethodsCallCounting(), new ValidPlayerWithMethodsCallCounting(), GameRulesProvider.Santase);
            round.Start(0, 0);
            Assert.Equal(0, round.TricksPlayed);

            RoundTestDriver.PlayTrick(round);
            Assert.Equal(1, round.TricksPlayed);

            while (!round.IsFinished)
            {
                RoundTestDriver.Step(round);
            }

            var cardsWon = round.FirstPlayer.TrickCards.Count + round.SecondPlayer.TrickCards.Count;
            Assert.InRange(round.TricksPlayed, cardsWon / 2, (cardsWon / 2) + 1);
        }

        private sealed class DealRecordingPlayer : BasePlayer
        {
            private readonly string name;

            private readonly List<string> log;

            public DealRecordingPlayer(string name, List<string> log)
            {
                this.name = name;
                this.log = log;
            }

            public override string Name => this.name;

            public List<Card> Hand { get; } = new List<Card>();

            public Card TrumpCard { get; private set; }

            public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
            {
                this.log.Add(this.name);
                this.Hand.AddRange(cards);
                this.TrumpCard = trumpCard;
                base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                var cards = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(cards.First());
            }
        }
    }
}
