namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;
    using Santase.Tests.Shared;

    using Xunit;

    // What a server hands to players and bots: per-seat views, the final record and the moves.
    public class SantaseSeatViewTests
    {
        [Fact]
        public void EveryViewShouldAgreeWithTheEngineAtEveryDecision()
        {
            var decisions = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var random = new Random(seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    var toMove = match.ToMove;
                    var other = toMove == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
                    var round = match.CurrentRound;
                    var hand = Info(round, toMove).Cards;
                    var engineContext = match.CreateTurnContext();

                    var view = match.GetView(toMove);
                    AssertSameContext(engineContext, view.CreateTurnContext());
                    Assert.Equal(hand, view.Hand);
                    Assert.Equal(PlayerActionValidator.Instance.GetPossibleCardsToPlay(engineContext, hand), view.PlayableCards);
                    Assert.Equal(PlayerActionValidator.Instance.IsValid(PlayerAction.ChangeTrump(), engineContext, hand), view.CanChangeTrump);
                    Assert.Equal(PlayerActionValidator.Instance.IsValid(PlayerAction.CloseGame(), engineContext, hand), view.CanClose);
                    Assert.Equal(round.FirstPlayer.Cards.Count, view.FirstPlayerCardCount);
                    Assert.Equal(round.SecondPlayer.Cards.Count, view.SecondPlayerCardCount);
                    Assert.Equal(round.FirstPlayer.RoundPoints, view.FirstPlayerRoundPoints);
                    Assert.Equal(round.SecondPlayer.RoundPoints, view.SecondPlayerRoundPoints);
                    Assert.Equal(round.Deck.CardsLeft, view.CardsLeftInDeck);
                    Assert.Equal(round.Deck.TrumpCard, view.TrumpCard);
                    Assert.Equal(round.TricksPlayed, view.Tricks.Count);
                    Assert.Equal(match.RoundsPlayed + 1, view.RoundNumber);
                    Assert.Null(view.Record);

                    // The other seat sees the same public state but has no options.
                    var otherView = match.GetView(other);
                    Assert.Empty(otherView.PlayableCards);
                    Assert.False(otherView.CanChangeTrump);
                    Assert.False(otherView.CanClose);
                    Assert.Equal(Info(round, other).Cards, otherView.Hand);
                    Assert.Throws<InvalidOperationException>(() => otherView.CreateTurnContext());

                    Assert.Equal(SantaseActResult.Ok, match.Act(toMove, RandomMove(view, random)));
                    decisions++;
                }
            }

            Assert.True(decisions > 5000);
        }

        [Fact]
        public void AViewShouldNeverShowTheOpponentsHand()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var random = new Random(1000 + seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    foreach (var seat in new[] { PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer })
                    {
                        var view = match.GetView(seat);
                        var shown = ModelCopy.CardsIn(view).ToHashSet();
                        var opponent = seat == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;

                        // The only cards of the opponent's hand anyone may know about: the face-up
                        // trump card they took when the talon ran out, or took in a trump exchange.
                        // A new round deals the same 24 cards again, so the last trick of the
                        // previous round names cards that may be anywhere now.
                        var publiclyKnown = new HashSet<Card> { view.TrumpCard };
                        if (view.SwappedTrumpCard != null)
                        {
                            publiclyKnown.Add(view.SwappedTrumpCard);
                        }

                        if (view.LastTrick != null && view.Tricks.Count == 0)
                        {
                            publiclyKnown.Add(view.LastTrick.LeadCard);
                            publiclyKnown.Add(view.LastTrick.FollowCard);
                        }

                        foreach (var card in Info(match.CurrentRound, opponent).Cards)
                        {
                            if (!publiclyKnown.Contains(card))
                            {
                                Assert.DoesNotContain(card, shown);
                            }
                        }
                    }

                    match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
                }
            }
        }

        [Fact]
        public void AViewShouldBeFullyDescribedByItsProperties()
        {
            // A host maps views to its own models and back; a copy built from the public
            // properties alone must behave exactly like the original.
            var random = new Random(77);
            var match = StartMatch(random);
            var checkedViews = 0;
            while (!match.IsFinished)
            {
                var view = match.GetView(match.ToMove);
                var copy = ModelCopy.Copy(view);

                Assert.NotSame(view, copy);
                Assert.Equal(ModelCopy.Describe(view), ModelCopy.Describe(copy));
                AssertSameContext(view.CreateTurnContext(), copy.CreateTurnContext());
                Assert.Equal(view.GetPlayedCards(), copy.GetPlayedCards());
                checkedViews++;

                match.Act(match.ToMove, RandomMove(view, random));
            }

            var final = match.GetFinalView();
            Assert.Equal(ModelCopy.Describe(final), ModelCopy.Describe(ModelCopy.Copy(final)));
            Assert.True(checkedViews > 50);
        }

        [Fact]
        public void TheTrickHistoryShouldBeConsistent()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var random = new Random(2000 + seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    var view = match.GetView(match.ToMove);
                    var round = match.CurrentRound;

                    // The played cards are exactly the cards both players have won.
                    var won = new CardCollection();
                    foreach (var card in round.FirstPlayer.TrickCards.Concat(round.SecondPlayer.TrickCards))
                    {
                        won.Add(card);
                    }

                    Assert.Equal(won, view.GetPlayedCards());
                    Assert.Equal(round.FirstPlayer.TrickCards.Count / 2, view.FirstPlayerTricksWon);
                    Assert.Equal(round.SecondPlayer.TrickCards.Count / 2, view.SecondPlayerTricksWon);

                    // Every trick was won by the right card and its winner led the next one.
                    for (var t = 0; t < view.Tricks.Count; t++)
                    {
                        var trick = view.Tricks[t];
                        var follower = trick.Leader == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
                        var leaderWins = CardWinnerLogic.GetWinner(trick.LeadCard, trick.FollowCard, view.TrumpCard.Suit) == PlayerPosition.FirstPlayer;
                        Assert.Equal(leaderWins ? trick.Leader : follower, trick.Winner);
                        if (t > 0)
                        {
                            Assert.Equal(view.Tricks[t - 1].Winner, trick.Leader);

                            // The talon shrinks by two a trick until it runs out or is closed,
                            // then stays put.
                            var previous = view.Tricks[t - 1].CardsLeftInDeck;
                            var drawn = previous - trick.CardsLeftInDeck;
                            Assert.True(drawn == 2 || (drawn == 0 && (previous == 0 || view.ClosedBy != PlayerPosition.NoOne)), $"talon {previous} -> {trick.CardsLeftInDeck}");
                            if (t > 1 && view.Tricks[t - 2].CardsLeftInDeck == previous)
                            {
                                Assert.Equal(0, drawn);
                            }
                        }
                        else
                        {
                            Assert.Equal(12, trick.CardsLeftInDeck);
                        }
                    }

                    match.Act(match.ToMove, RandomMove(view, random));
                }
            }
        }

        [Fact]
        public void TheCurrentLeadShouldBeVisibleToBothSeats()
        {
            var match = StartStacked();
            Assert.Equal(PlayerPosition.NoOne, match.GetView(PlayerPosition.FirstPlayer).CurrentTrickLeader);

            match.Act(PlayerPosition.FirstPlayer, PlayerAction.PlayCard(TestCards.Parse("AC")));

            foreach (var seat in new[] { PlayerPosition.FirstPlayer, PlayerPosition.SecondPlayer })
            {
                var view = match.GetView(seat);
                Assert.Equal(PlayerPosition.FirstPlayer, view.CurrentTrickLeader);
                Assert.Equal(TestCards.Parse("AC"), view.CurrentTrickLeadCard);
                Assert.Equal(Announce.None, view.CurrentTrickAnnounce);
                Assert.Equal(PlayerPosition.SecondPlayer, view.ToMove);
            }

            var follower = match.GetView(PlayerPosition.SecondPlayer).CreateTurnContext();
            Assert.Equal(TestCards.Parse("AC"), follower.FirstPlayedCard);
            Assert.False(follower.IsFirstPlayerTurn);
        }

        [Fact]
        public void ATrumpExchangeAndACloseShouldShowInTheViews()
        {
            var match = StartStacked();
            match.Act(PlayerPosition.FirstPlayer, PlayerAction.PlayCard(TestCards.Parse("AC")));
            match.Act(PlayerPosition.SecondPlayer, PlayerAction.PlayCard(TestCards.Parse("9D")));

            var before = match.GetView(PlayerPosition.FirstPlayer);
            Assert.True(before.CanChangeTrump);
            Assert.True(before.CanClose);
            Assert.Equal(RoundPhase.MoreThanTwoCardsLeft, before.Phase);
            Assert.Single(before.Tricks);
            Assert.Equal(PlayerPosition.FirstPlayer, before.LastTrick.Winner);

            match.Act(PlayerPosition.FirstPlayer, PlayerAction.ChangeTrump());
            var swapped = match.GetView(PlayerPosition.SecondPlayer);
            Assert.Equal(PlayerPosition.FirstPlayer, swapped.TrumpSwappedBy);
            Assert.Equal(TestCards.Parse("AS"), swapped.SwappedTrumpCard);
            Assert.Equal(TestCards.Parse("9S"), swapped.TrumpCard);
            Assert.False(match.GetView(PlayerPosition.FirstPlayer).CanChangeTrump);

            match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame());
            var closed = match.GetView(PlayerPosition.SecondPlayer);
            Assert.Equal(PlayerPosition.FirstPlayer, closed.ClosedBy);
            Assert.Equal(RoundPhase.Final, closed.Phase);
            Assert.True(closed.IsTrumpCardOnTable);
        }

        [Fact]
        public void APlayerUsingOnlyViewsAndMovesCanPlayAWholeMatch()
        {
            // Exactly what a server does: views out, plain moves in; every legal option offered by
            // the view must be accepted.
            for (var seed = 0; seed < 20; seed++)
            {
                var random = new Random(3000 + seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    var view = match.GetView(match.ToMove);
                    Assert.NotEmpty(view.PlayableCards);
                    Assert.Equal(SantaseActResult.Ok, match.Act(view.Seat, RandomMove(view, random)));
                }

                Assert.NotEqual(PlayerPosition.NoOne, match.Winner);
            }
        }

        [Fact]
        public void PreviousRoundsShouldAddUpToTheGamePoints()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var random = new Random(4000 + seed);
                var match = StartMatch(random);
                SantaseTrick lastTrickOfPreviousRound = null;
                var rounds = 0;
                while (!match.IsFinished)
                {
                    var view = match.GetView(match.ToMove);
                    if (view.PreviousRounds.Count > rounds)
                    {
                        // A new round was just dealt: its trick list is empty, but the last trick of
                        // the previous round is still shown.
                        Assert.Empty(view.Tricks);
                        Assert.Same(lastTrickOfPreviousRound, view.LastTrick);
                        rounds = view.PreviousRounds.Count;
                    }

                    var roundBefore = match.CurrentRound;
                    match.Act(match.ToMove, RandomMove(view, random));
                    if (match.CurrentRound != roundBefore)
                    {
                        lastTrickOfPreviousRound = roundBefore.Tricks[^1];
                    }
                }

                var final = match.GetFinalView();
                Assert.Equal(match.RoundsPlayed, final.PreviousRounds.Count);
                Assert.Equal(match.FirstPlayerTotalPoints, final.PreviousRounds.Where(r => r.Winner == PlayerPosition.FirstPlayer).Sum(r => r.GamePoints));
                Assert.Equal(match.SecondPlayerTotalPoints, final.PreviousRounds.Where(r => r.Winner == PlayerPosition.SecondPlayer).Sum(r => r.GamePoints));
                Assert.All(final.PreviousRounds.Where(r => r.Winner == PlayerPosition.NoOne), r => Assert.Equal(0, r.GamePoints));
            }
        }

        [Fact]
        public void TheFinalViewShouldCarryTheWholeRecordAndNoHand()
        {
            var log = new List<string>();
            var first = new HandRecordingObserver(log);
            var second = new HandRecordingObserver(log);
            var random = new Random(9);
            var match = new SantaseMatch(first, second, new SantaseMatchOptions { Shuffle = random.Next });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
            }

            var final = match.GetFinalView();
            Assert.Equal(PlayerPosition.NoOne, final.Seat);
            Assert.Equal(PlayerPosition.NoOne, final.ToMove);
            Assert.True(final.IsMatchFinished);
            Assert.Equal(match.Winner, final.MatchWinner);
            Assert.Empty(final.Hand);
            Assert.Empty(final.PlayableCards);
            Assert.Equal(match.RoundsPlayed, final.RoundNumber);

            var record = final.Record;
            Assert.Equal(match.RoundsPlayed, record.Rounds.Count);
            Assert.Equal(match.Winner, record.Winner);
            Assert.Equal(match.FirstPlayerTotalPoints, record.FirstPlayerTotalPoints);
            for (var r = 0; r < record.Rounds.Count; r++)
            {
                var round = record.Rounds[r];

                // The deal is the whole deck in draw order: the hands each player was dealt, then the
                // talon, the trump card last.
                Assert.Equal(24, round.Deal.Distinct().Count());
                Assert.Equal(first.Deals[r], round.Deal.Take(6));
                Assert.Equal(second.Deals[r], round.Deal.Skip(6).Take(6));
                Assert.Equal(first.TrumpCards[r], round.Deal[23]);
                Assert.Same(final.PreviousRounds[r], round.Result);
                Assert.Equal(round.Result.ClosedBy, round.ClosedBy);
            }
        }

        // After the match the view still shows the last round, which may have ended with a 20/40
        // that took the leader to 66 before the answer. That lead won no cards: tricks won count
        // only the tricks the engine scored (the cards each player took), as mid-round views do.
        [Fact]
        public void TricksWonAfterTheMatchShouldNotCountALeadThatEndedTheRound()
        {
            var endedByALead = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var random = new Random(3000 + seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
                }

                var final = match.GetFinalView();
                Assert.Equal(match.CurrentRound.FirstPlayer.TrickCards.Count / 2, final.FirstPlayerTricksWon);
                Assert.Equal(match.CurrentRound.SecondPlayer.TrickCards.Count / 2, final.SecondPlayerTricksWon);
                if (final.Tricks[^1].FollowCard == null)
                {
                    endedByALead++;
                }
            }

            Assert.True(endedByALead > 0, "No match ended with a 20/40 lead.");
        }

        // A record is everything that happened: dealing each recorded deal again and making the
        // recorded moves replays the match exactly, down to an identical record. That needs when
        // the Nine was exchanged and when the talon was closed (the record only said by whom: the
        // exchange could have been made at any of the exchanger's leads).
        [Fact]
        public void AMatchShouldReplayExactlyFromItsRecord()
        {
            var exchangesAndCloses = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var random = new Random(4000 + seed);
                var match = StartMatch(random);
                while (!match.IsFinished)
                {
                    match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
                }

                var record = match.GetRecord();
                var deals = record.Rounds.Select(r => StackedDeal.ForOrder(r.Deal.Reverse().ToList())).ToList();
                var draws = 0;
                var replay = new SantaseMatch(new SantaseMatchOptions
                {
                    FirstToPlay = record.Rounds[0].FirstToPlay,
                    Shuffle = max => deals[draws++ / 23](max),
                });
                replay.Start();
                foreach (var round in record.Rounds)
                {
                    for (var t = 0; t < round.Tricks.Count; t++)
                    {
                        // An exchange comes before a close at the same lead (a closed talon allows none).
                        if (round.TrumpSwappedBy != PlayerPosition.NoOne && round.TrumpSwappedAfterTricks == t)
                        {
                            Assert.Equal(SantaseActResult.Ok, replay.Act(round.TrumpSwappedBy, PlayerAction.ChangeTrump()));
                            exchangesAndCloses++;
                        }

                        if (round.ClosedBy != PlayerPosition.NoOne && round.ClosedAfterTricks == t)
                        {
                            Assert.Equal(SantaseActResult.Ok, replay.Act(round.ClosedBy, PlayerAction.CloseGame()));
                            exchangesAndCloses++;
                        }

                        var trick = round.Tricks[t];
                        Assert.Equal(SantaseActResult.Ok, replay.Act(trick.Leader, PlayerAction.PlayCard(trick.LeadCard)));
                        if (trick.FollowCard != null)
                        {
                            var follower = trick.Leader == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
                            Assert.Equal(SantaseActResult.Ok, replay.Act(follower, PlayerAction.PlayCard(trick.FollowCard)));
                        }
                    }
                }

                Assert.True(replay.IsFinished);
                Assert.Equal(ModelCopy.Describe(record), ModelCopy.Describe(replay.GetRecord()));
            }

            Assert.True(exchangesAndCloses > 20);
        }

        [Fact]
        public void TheRecordedDealShouldBeTheDealInDrawOrder()
        {
            // Stack the first deal, then shuffle at random.
            var random = new Random(6);
            var stacked = StackedDeal.Create("AS", "9S AC KC QC JC 10C", "9D JD QD KD 10D AD", "KS QS JS 10S");
            var calls = 0;
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = max => calls++ < 23 ? stacked(max) : random.Next(max) });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, RandomMove(match.GetView(match.ToMove), random));
            }

            var deal = match.GetRecord().Rounds[0].Deal;
            Assert.Equal(TestCards.List("9S AC KC QC JC 10C"), deal.Take(6));
            Assert.Equal(TestCards.List("9D JD QD KD 10D AD"), deal.Skip(6).Take(6));
            Assert.Equal(TestCards.List("KS QS JS 10S"), deal.Skip(12).Take(4));
            Assert.Equal(TestCards.Parse("AS"), deal[23]);

            // A recorded deal is enough to deal the round again.
            var redeal = new SantaseMatch(new SantaseMatchOptions { Shuffle = StackedDeal.ForOrder(deal.Reverse().ToList()) });
            redeal.Start();
            Assert.True(deal.Take(6).ToHashSet().SetEquals(redeal.GetView(PlayerPosition.FirstPlayer).Hand));
            Assert.True(deal.Skip(6).Take(6).ToHashSet().SetEquals(redeal.GetView(PlayerPosition.SecondPlayer).Hand));
            Assert.Equal(deal[23], redeal.GetView(PlayerPosition.FirstPlayer).TrumpCard);
        }

        [Fact]
        public void ViewsShouldNeedAStartedMatchThatRecordsItsHistory()
        {
            var notStarted = new SantaseMatch();
            Assert.Throws<InvalidOperationException>(() => notStarted.GetView(PlayerPosition.FirstPlayer));

            var match = new SantaseMatch();
            match.Start();
            Assert.Throws<ArgumentOutOfRangeException>(() => match.GetView(PlayerPosition.NoOne));
            Assert.Throws<InvalidOperationException>(() => match.GetFinalView());
            Assert.Throws<InvalidOperationException>(() => match.GetRecord());

            var noHistory = new SantaseMatch(new SantaseMatchOptions { RecordHistory = false });
            noHistory.Start();
            Assert.Throws<InvalidOperationException>(() => noHistory.GetView(PlayerPosition.FirstPlayer));
            Assert.Equal(SantaseActResult.NotYourTurn, noHistory.Act(PlayerPosition.NoOne, PlayerAction.CloseGame()));
        }

        private static SantaseMatch StartMatch(Random random)
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next, FirstToPlay = random.Next(2) == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer });
            match.Start();
            return match;
        }

        private static SantaseMatch StartStacked()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = StackedDeal.Create("AS", "9S AC KC QC JC 10C", "9D JD QD KD 10D AD") });
            match.Start();
            return match;
        }

        private static RoundPlayerInfo Info(Round round, PlayerPosition seat)
        {
            return seat == PlayerPosition.FirstPlayer ? round.FirstPlayer : round.SecondPlayer;
        }

        // A random move chosen only from what the view offers.
        private static PlayerAction RandomMove(SantaseSeatView view, Random random)
        {
            if (view.CanChangeTrump && random.Next(2) == 0)
            {
                return PlayerAction.ChangeTrump();
            }

            if (view.CanClose && random.Next(8) == 0)
            {
                return PlayerAction.CloseGame();
            }

            return PlayerAction.PlayCard(view.PlayableCards[random.Next(view.PlayableCards.Count)]);
        }

        private static void AssertSameContext(PlayerTurnContext expected, PlayerTurnContext actual)
        {
            Assert.Equal(expected.State.GetType(), actual.State.GetType());
            Assert.Equal(expected.TrumpCard, actual.TrumpCard);
            Assert.Equal(expected.CardsLeftInDeck, actual.CardsLeftInDeck);
            Assert.Equal(expected.FirstPlayedCard, actual.FirstPlayedCard);
            Assert.Equal(expected.FirstPlayerAnnounce, actual.FirstPlayerAnnounce);
            Assert.Equal(expected.FirstPlayerRoundPoints, actual.FirstPlayerRoundPoints);
            Assert.Equal(expected.SecondPlayedCard, actual.SecondPlayedCard);
            Assert.Equal(expected.SecondPlayerRoundPoints, actual.SecondPlayerRoundPoints);
            Assert.Equal(expected.IsFirstPlayerTurn, actual.IsFirstPlayerTurn);
        }

        private sealed class HandRecordingObserver : BasePlayer
        {
            private readonly List<string> log;

            public HandRecordingObserver(List<string> log)
            {
                this.log = log;
            }

            public override string Name => "observer";

            public List<List<Card>> Deals { get; } = new List<List<Card>>();

            public List<Card> TrumpCards { get; } = new List<Card>();

            public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
            {
                this.log.Add("StartRound");
                this.Deals.Add(cards.ToList());
                this.TrumpCards.Add(trumpCard);
                base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                throw new InvalidOperationException("Observers are never asked for a move.");
            }
        }
    }
}
