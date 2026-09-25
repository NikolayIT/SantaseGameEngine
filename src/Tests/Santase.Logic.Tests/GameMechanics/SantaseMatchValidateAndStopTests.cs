namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Tests.Shared;

    using Xunit;

    public class SantaseMatchValidateAndStopTests
    {
        [Fact]
        public void ValidateShouldAgreeWithActAndChangeNothing()
        {
            var checkedActions = 0;
            for (var seed = 0; seed < 25; seed++)
            {
                var random = new Random(seed);
                var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
                match.Start();
                while (!match.IsFinished)
                {
                    var toMove = match.ToMove;
                    var other = toMove == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
                    var view = match.GetView(toMove);
                    var before = Snapshot(match);

                    // Exactly the options the view offers are valid, for the mover only.
                    var valid = new List<PlayerAction>();
                    foreach (var move in AllMoves())
                    {
                        var result = match.Validate(toMove, move);
                        Assert.Equal(SantaseActResult.NotYourTurn, match.Validate(other, move));
                        if (result == SantaseActResult.Ok)
                        {
                            valid.Add(move);
                        }
                        else
                        {
                            Assert.Equal(SantaseActResult.InvalidAction, result);
                        }

                        checkedActions++;
                    }

                    var offered = view.PlayableCards.Select(PlayerAction.PlayCard).ToList();
                    if (view.CanChangeTrump)
                    {
                        offered.Add(PlayerAction.ChangeTrump());
                    }

                    if (view.CanClose)
                    {
                        offered.Add(PlayerAction.CloseGame());
                    }

                    Assert.Equal(offered.Select(Describe).OrderBy(x => x), valid.Select(Describe).OrderBy(x => x));
                    Assert.Equal(SantaseActResult.InvalidAction, match.Validate(toMove, null));
                    Assert.Equal(SantaseActResult.InvalidAction, match.Validate(toMove, PlayerAction.PlayCard(null)));
                    Assert.Equal(before, Snapshot(match));

                    // Act returns what Validate said, for an invalid move too.
                    if (random.Next(5) == 0)
                    {
                        var invalid = AllMoves().FirstOrDefault(m => !valid.Any(v => Describe(v) == Describe(m)));
                        if (invalid != null)
                        {
                            Assert.Equal(SantaseActResult.InvalidAction, match.Act(toMove, invalid));
                            Assert.Equal(before, Snapshot(match));
                        }
                    }

                    var chosen = valid[random.Next(valid.Count)];
                    Assert.Equal(SantaseActResult.Ok, match.Act(toMove, chosen));
                }

                Assert.Equal(SantaseActResult.MatchFinished, match.Validate(match.Winner, PlayerAction.CloseGame()));
            }

            Assert.True(checkedActions > 50000);
        }

        [Fact]
        public void ValidateShouldNeedAStartedMatch()
        {
            Assert.Throws<InvalidOperationException>(() => new SantaseMatch().Validate(PlayerPosition.FirstPlayer, PlayerAction.CloseGame()));
        }

        [Fact]
        public void StopShouldEndTheMatchWithoutAWinnerAndRevealTheUnfinishedRound()
        {
            var first = new CountingObserver();
            var second = new CountingObserver();
            var random = new Random(12);
            var match = new SantaseMatch(first, second, new SantaseMatchOptions { Shuffle = random.Next });
            match.Start();
            PlayUntil(match, random, () => match.RoundsPlayed == 1 && match.CurrentRound.TricksPlayed == 3);
            var totals = (match.FirstPlayerTotalPoints, match.SecondPlayerTotalPoints);
            var hand = match.GetView(PlayerPosition.FirstPlayer).Hand;

            match.Stop();

            Assert.True(match.IsFinished);
            Assert.True(match.IsStopped);
            Assert.Equal(PlayerPosition.NoOne, match.Winner);
            Assert.Equal(PlayerPosition.NoOne, match.ToMove);
            Assert.Equal(1, match.RoundsPlayed);
            Assert.Equal(totals, (match.FirstPlayerTotalPoints, match.SecondPlayerTotalPoints));
            Assert.Equal(SantaseActResult.MatchFinished, match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame()));
            Assert.Equal(SantaseActResult.MatchFinished, match.Validate(PlayerPosition.SecondPlayer, PlayerAction.CloseGame()));
            Assert.Equal(0, first.EndGames + second.EndGames);

            // The seats still see their hands, with no options.
            var seatView = match.GetView(PlayerPosition.FirstPlayer);
            Assert.Equal(hand, seatView.Hand);
            Assert.Empty(seatView.PlayableCards);

            var final = match.GetFinalView();
            Assert.True(final.IsMatchFinished);
            Assert.Equal(PlayerPosition.NoOne, final.MatchWinner);
            Assert.Equal(2, final.RoundNumber);
            Assert.Single(final.PreviousRounds);
            Assert.Equal(3, final.Tricks.Count);

            var record = match.GetRecord();
            Assert.Equal(PlayerPosition.NoOne, record.Winner);
            Assert.Equal(2, record.Rounds.Count);
            Assert.NotNull(record.Rounds[0].Result);
            var unfinished = record.Rounds[1];
            Assert.Null(unfinished.Result);
            Assert.Equal(24, unfinished.Deal.Distinct().Count());
            Assert.Equal(final.Tricks.Select(Describe), unfinished.Tricks.Select(Describe));
            Assert.Equal(first.LastDeal, unfinished.Deal.Take(6));

            // A copy built from the public properties keeps the null result.
            var described = ModelCopy.Describe(final);
            Assert.Equal(described, ModelCopy.Describe(ModelCopy.Copy(final)));

            // Stopping again changes nothing.
            match.Stop();
            Assert.Equal(described, ModelCopy.Describe(match.GetFinalView()));
        }

        [Fact]
        public void StopRightAfterADealShouldRecordTheDealWithNoTricks()
        {
            var random = new Random(4);
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
            match.Start();
            PlayUntil(match, random, () => match.RoundsPlayed == 2);

            match.Stop();

            var record = match.GetRecord();
            Assert.Equal(3, record.Rounds.Count);
            Assert.Empty(record.Rounds[2].Tricks);
            Assert.Equal(24, record.Rounds[2].Deal.Count);
            Assert.Equal(PlayerPosition.NoOne, record.Rounds[2].ClosedBy);
        }

        [Fact]
        public void StopShouldNotChangeAFinishedMatch()
        {
            var random = new Random(21);
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
            match.Start();
            PlayUntil(match, random, () => false);
            var winner = match.Winner;
            var before = ModelCopy.Describe(match.GetFinalView());

            match.Stop();

            Assert.False(match.IsStopped);
            Assert.Equal(winner, match.Winner);
            Assert.Equal(before, ModelCopy.Describe(match.GetFinalView()));
        }

        [Fact]
        public void StopShouldNeedAStartedMatchAndWorkWithoutHistory()
        {
            Assert.Throws<InvalidOperationException>(() => new SantaseMatch().Stop());

            var match = new SantaseMatch(new SantaseMatchOptions { RecordHistory = false });
            match.Start();
            match.Stop();
            Assert.True(match.IsFinished);
            Assert.Throws<InvalidOperationException>(() => match.GetRecord());
        }

        private static void PlayUntil(SantaseMatch match, Random random, Func<bool> stop)
        {
            while (!match.IsFinished && !stop())
            {
                var view = match.GetView(match.ToMove);
                Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, PlayerAction.PlayCard(view.PlayableCards[random.Next(view.PlayableCards.Count)])));
            }
        }

        private static IEnumerable<PlayerAction> AllMoves()
        {
            foreach (var card in Deck.UnshuffledOrder)
            {
                yield return PlayerAction.PlayCard(card);
            }

            yield return PlayerAction.ChangeTrump();
            yield return PlayerAction.CloseGame();
        }

        private static string Describe(PlayerAction move) => $"{move.Type} {move.Card}";

        private static string Describe(SantaseTrick trick) => $"{trick.Leader} {trick.LeadCard} {trick.FollowCard} {trick.Winner}";

        // Everything a seat or the engine could observe, as text.
        private static string Snapshot(SantaseMatch match)
        {
            return ModelCopy.Describe(match.GetView(PlayerPosition.FirstPlayer))
                + ModelCopy.Describe(match.GetView(PlayerPosition.SecondPlayer))
                + match.ToMove + match.CurrentRound.TricksPlayed;
        }

        private sealed class CountingObserver : BasePlayer
        {
            public override string Name => "observer";

            public int EndGames { get; private set; }

            public List<Card> LastDeal { get; private set; }

            public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
            {
                this.LastDeal = cards.ToList();
                base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            }

            public override void EndGame(bool amIWinner)
            {
                this.EndGames++;
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                throw new InvalidOperationException("Observers are never asked for a move.");
            }
        }
    }
}
