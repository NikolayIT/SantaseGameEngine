namespace Santase.Logic.Tests.GameMechanics
{
    using System;

    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    using Xunit;

    // IGameRules is the seam for rule variants (see SantaseGameRules for the standard ones).
    public class CustomRulesTests
    {
        // Dealing more cards leaves a smaller talon: 2 cards at 11 each. The phases must follow
        // the talon; the first trick used to lead to "more than two cards left" whatever was left,
        // so with 10 or 11 cards each the round drew from an empty talon and threw.
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        public void MatchesShouldPlayToTheEndWithEveryDealThatLeavesATalon(int cardsEach)
        {
            var rules = new VariantRules { CardsAtStartOfTheRound = cardsEach };
            for (var seed = 0; seed < 20; seed++)
            {
                var random = new Random(seed);
                var match = new SantaseMatch(new SantaseMatchOptions { Rules = rules, Shuffle = random.Next });
                match.Start();
                while (!match.IsFinished)
                {
                    var view = match.GetView(match.ToMove);
                    if (view.Tricks.Count == 0 && view.CurrentTrickLeadCard == null)
                    {
                        Assert.Equal(cardsEach, view.Hand.Count);
                        Assert.Equal(24 - (2 * cardsEach), view.CardsLeftInDeck);
                    }

                    Assert.Equal(ExpectedPhase(view), view.Phase);
                    if (view.CanClose || view.CanChangeTrump)
                    {
                        Assert.True(view.Tricks.Count > 0 && view.CardsLeftInDeck > 2);
                    }

                    Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, RandomMove(view, random)));
                }

                Assert.NotEqual(PlayerPosition.NoOne, match.Winner);
            }
        }

        // Rules no match can be played by. A deal of 12 or more leaves no talon (the face-up trump
        // card is its last card), none at all ends every round 0-0 before a card is played (and
        // deals again forever), and so does a round target of 0; a game target of 0 is "won"
        // before the first deal.
        [Theory]
        [InlineData(0, 11, 66)]
        [InlineData(-1, 11, 66)]
        [InlineData(12, 11, 66)]
        [InlineData(13, 11, 66)]
        [InlineData(6, 0, 66)]
        [InlineData(6, -3, 66)]
        [InlineData(6, 11, 0)]
        public void RulesNoMatchCanBePlayedByShouldBeRefused(int cardsEach, int gamePoints, int roundPoints)
        {
            var rules = new VariantRules { CardsAtStartOfTheRound = cardsEach, GamePointsNeededForWin = gamePoints, RoundPointsForGoingOut = roundPoints };
            Assert.Throws<ArgumentOutOfRangeException>(() => new SantaseMatch(new SantaseMatchOptions { Rules = rules }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SantaseGame(new NoPlayer(), new NoPlayer(), rules, new NoLogger()).Start());
        }

        [Fact]
        public void TheSmallestTargetsShouldStillPlayARound()
        {
            var random = new Random(5);
            var rules = new VariantRules { GamePointsNeededForWin = 1, RoundPointsForGoingOut = 1 };
            var match = new SantaseMatch(new SantaseMatchOptions { Rules = rules, Shuffle = random.Next });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
            }

            Assert.Equal(1, match.RoundsPlayed);
            Assert.NotEqual(PlayerPosition.NoOne, match.Winner);
        }

        private static RoundPhase ExpectedPhase(SantaseSeatView view)
        {
            if (view.ClosedBy != PlayerPosition.NoOne)
            {
                return RoundPhase.Final;
            }

            if (view.Tricks.Count == 0)
            {
                return RoundPhase.Start;
            }

            return view.CardsLeftInDeck switch
            {
                0 => RoundPhase.Final,
                2 => RoundPhase.TwoCardsLeft,
                _ => RoundPhase.MoreThanTwoCardsLeft,
            };
        }

        private static PlayerAction RandomMove(SantaseSeatView view, Random random)
        {
            if (view.CanChangeTrump && random.Next(2) == 0)
            {
                return PlayerAction.ChangeTrump();
            }

            if (view.CanClose && random.Next(6) == 0)
            {
                return PlayerAction.CloseGame();
            }

            return PlayerAction.PlayCard(view.PlayableCards[random.Next(view.PlayableCards.Count)]);
        }

        private sealed class VariantRules : IGameRules
        {
            public int RoundPointsForGoingOut { get; init; } = 66;

            public int HalfRoundPoints { get; init; } = 33;

            public int GamePointsNeededForWin { get; init; } = 11;

            public int CardsAtStartOfTheRound { get; init; } = 6;
        }

        private sealed class NoPlayer : BasePlayer
        {
            public override string Name => "none";

            public override PlayerAction GetTurn(PlayerTurnContext context) => throw new InvalidOperationException();
        }
    }
}
