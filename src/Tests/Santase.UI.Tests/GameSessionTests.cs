namespace Santase.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;
    using Santase.UI.Game;

    using Xunit;

    // The app's game flow, played for real: whole games on a UI-like thread, checked event by
    // event against the engine's rules and scoring.
    public class GameSessionTests
    {
        public static IEnumerable<object[]> Computers => new[]
        {
            new object[] { "dummy" },
            new object[] { "smart" },
            new object[] { "claude" },
            new object[] { "neural" },
            new object[] { "ismcts" },
        };

        [Fact]
        public void HotSeatGamesShouldPlayToTheEndWithEveryEventInPlayOrder() => UiThread.Run(async () =>
        {
            for (var seed = 0; seed < 15; seed++)
            {
                var session = HotSeat(seed);
                var table = new TableDriver(session, seed);
                await table.PlayToTheEndAsync();

                AssertPlayOrder(table.Events);
                AssertScoring(table, session);
                Assert.Equal(new[] { UiThread.Id }, table.EventThreads);
                Assert.False(session.IsRunning);
                Assert.All(Turns(table), turn => Assert.True(turn.IsHuman));
            }
        });

        [Theory]
        [MemberData(nameof(Computers))]
        public void EveryComputerOpponentShouldPlayWholeGames(string computer) => UiThread.Run(async () =>
        {
            for (var seed = 0; seed < 2; seed++)
            {
                var session = VsComputer(computer, seed);
                var table = new TableDriver(session, seed);
                await table.PlayToTheEndAsync();

                AssertPlayOrder(table.Events);
                AssertScoring(table, session);
                Assert.Equal(new[] { UiThread.Id }, table.EventThreads);

                // The person plays the first seat, the computer the second.
                var turns = Turns(table).ToList();
                Assert.All(turns, turn => Assert.Equal(turn.Slot == PlayerSlot.First, turn.IsHuman));
                Assert.Contains(turns, turn => !turn.IsHuman);
            }
        });

        [Fact]
        public void TricksShouldHoldTheCardsJustPlayedAndTheirWinnerShouldLeadNext() => UiThread.Run(async () =>
        {
            for (var seed = 0; seed < 10; seed++)
            {
                var table = new TableDriver(seed % 2 == 0 ? HotSeat(seed) : VsComputer("claude", seed), seed);
                await table.PlayToTheEndAsync();

                var round = -1;
                var played = new List<(PlayerSlot Slot, Card Card)>();
                TrickInfo? previous = null;
                foreach (var item in table.Events)
                {
                    switch (item)
                    {
                        case "round":
                            round++;
                            previous = null;
                            break;
                        case MoveInfo { Action.Type: PlayerActionType.PlayCard } move:
                            if (played.Count == 0 && previous != null)
                            {
                                Assert.Equal(previous.Winner, move.Slot);
                            }

                            played.Add((move.Slot, move.Action.Card));
                            break;
                        case TrickInfo trick:
                            Assert.Equal(played[0], (trick.Leader, trick.LeadCard));
                            Assert.Equal(trick.LeadCard, trick.CardOf(trick.Leader));
                            if (played.Count == 2)
                            {
                                var follower = played[1].Slot;
                                Assert.NotEqual(trick.Leader, follower);
                                Assert.Equal(played[1].Card, trick.FollowCard);
                                Assert.Equal(played[1].Card, trick.CardOf(follower));
                                var leaderWins = CardWinnerLogic.GetWinner(trick.LeadCard, played[1].Card, table.RoundTrumps[round]) == PlayerPosition.FirstPlayer;
                                Assert.Equal(leaderWins ? trick.Leader : follower, trick.Winner);
                            }
                            else
                            {
                                // A 20/40 that reached 66 ends the round before the follower plays.
                                Assert.Single(played);
                                Assert.Null(trick.FollowCard);
                                Assert.True(trick.RoundOver);
                            }

                            played.Clear();
                            previous = trick;
                            break;
                    }
                }

                Assert.Empty(played);
            }
        });

        [Fact]
        public void AnnouncesShouldScoreTheMomentTheyAreMadeAndBeListedWithTheRound() => UiThread.Run(async () =>
        {
            var announces = 0;
            for (var seed = 0; seed < 25; seed++)
            {
                var table = new TableDriver(seed % 2 == 0 ? HotSeat(seed) : VsComputer("smart", seed), seed);
                await table.PlayToTheEndAsync();

                var first = new List<Announce>();
                var second = new List<Announce>();
                var moves = table.Moves.GetEnumerator();
                foreach (var item in table.Events)
                {
                    if (item is MoveInfo)
                    {
                        Assert.True(moves.MoveNext());
                        var (move, firstBefore, secondBefore) = moves.Current;
                        if (move.Announce == Announce.None)
                        {
                            continue;
                        }

                        // Only a led king or queen announces, and it counts at once.
                        Assert.Equal(PlayerActionType.PlayCard, move.Action.Type);
                        Assert.Contains(move.Action.Card.Type, new[] { CardType.King, CardType.Queen });
                        var before = move.Slot == PlayerSlot.First ? firstBefore : secondBefore;
                        var after = move.Slot == PlayerSlot.First ? move.FirstRoundPoints : move.SecondRoundPoints;
                        Assert.Equal(before + (int)move.Announce, after);
                        (move.Slot == PlayerSlot.First ? first : second).Add(move.Announce);
                        announces++;
                    }
                    else if (item is RoundEndInfo round)
                    {
                        Assert.Equal(first, round.FirstAnnounces);
                        Assert.Equal(second, round.SecondAnnounces);
                        first.Clear();
                        second.Clear();
                    }
                }
            }

            Assert.True(announces > 20, $"only {announces} announces in 25 games");
        });

        [Fact]
        public void APersonShouldOnlyBeAbleToMakeTheirOwnLegalMoves() => UiThread.Run(async () =>
        {
            var session = HotSeat(3);
            var table = new TableDriver(session, 3) { AutoPlay = false };
            var turn = new TaskCompletionSource<PlayerSlot>();
            table.Turn += (slot, _) => turn.TrySetResult(slot);
            session.Start();

            var mover = await turn.Task;
            var other = GameSession.Other(mover);
            var view = session.GetView(mover)!;
            var notInHand = Deck.UnshuffledOrder.First(card => !view.Hand.Contains(card));

            Assert.True(session.IsAwaitingMove(mover));
            Assert.False(session.IsAwaitingMove(other));
            Assert.False(session.TryPlay(other, PlayerAction.PlayCard(session.GetView(other)!.Hand[0])));
            Assert.False(session.TryPlay(mover, PlayerAction.PlayCard(notInHand)));
            Assert.False(session.TryPlay(mover, PlayerAction.PlayCard(null)));
            Assert.False(session.TryPlay(mover, PlayerAction.CloseGame()));
            Assert.Empty(table.Events.OfType<MoveInfo>());

            Assert.True(session.TryPlay(mover, PlayerAction.PlayCard(view.PlayableCards[0])));
            Assert.False(session.TryPlay(mover, PlayerAction.PlayCard(view.PlayableCards[^1])));
            Assert.False(session.IsAwaitingMove(mover));

            // The game goes on from the UI thread's queue, not inside TryPlay.
            Assert.Empty(table.Events.OfType<MoveInfo>());
            turn = new TaskCompletionSource<PlayerSlot>();
            Assert.Equal(other, await turn.Task);
            Assert.Single(table.Events.OfType<MoveInfo>());
            session.Stop();
        });

        [Fact]
        public void StoppingShouldEndTheGameQuietlyWhateverItIsWaitingFor() => UiThread.Run(async () =>
        {
            // Waiting for a person.
            var session = HotSeat(5);
            var table = new TableDriver(session, 5) { AutoPlay = false };
            var turn = new TaskCompletionSource<bool>();
            table.Turn += (_, _) => turn.TrySetResult(true);
            session.Start();
            await turn.Task;
            await AssertStopsQuietly(session, table);

            // The computer about to think (a long pause, stopped at its start).
            session = VsComputer("claude", 6, new GamePace(10_000, 0));
            table = new TableDriver(session, 6);
            var thinking = new TaskCompletionSource<bool>();
            table.Turn += (_, isHuman) =>
            {
                if (!isHuman)
                {
                    thinking.TrySetResult(true);
                }
            };
            session.Start();
            await thinking.Task;
            await AssertStopsQuietly(session, table);
            Assert.DoesNotContain(table.Events.OfType<MoveInfo>(), move => move.Slot == PlayerSlot.Second);

            // A finished trick on the table.
            session = HotSeat(7);
            session = new GameSession(GameMode.HotSeat, "Ann", "Bob", null, new GamePace(0, 10_000), new Random(7).Next);
            table = new TableDriver(session, 7);
            var onTable = new TaskCompletionSource<bool>();
            session.MovePlayed += _ =>
            {
                if (table.Moves.Count(m => m.Move.Action.Type == PlayerActionType.PlayCard) == 2)
                {
                    onTable.TrySetResult(true);
                }
            };
            session.Start();
            await onTable.Task;
            await AssertStopsQuietly(session, table);
            Assert.Empty(table.Tricks);

            // Waiting for Continue after a round.
            session = HotSeat(8);
            table = new TableDriver(session, 8) { AutoContinue = false };
            var roundOver = new TaskCompletionSource<bool>();
            session.RoundFinished += _ => roundOver.TrySetResult(true);
            session.Start();
            await roundOver.Task;
            await AssertStopsQuietly(session, table);
        });

        // Leaving the table can happen inside an event handler (a person taps "leave" while the
        // screen updates, or the page goes away): nothing more may be raised or awaited then. The
        // flow used to check for a stop only after its awaits, so a stop from RoundStarted,
        // MovePlayed or TrickCollected still armed the next person's move and raised TurnStarted.
        [Theory]
        [InlineData("deal")]
        [InlineData("turn")]
        [InlineData("lead")]
        [InlineData("trick")]
        [InlineData("round result")]
        public void StoppingFromAnEventHandlerShouldEndTheGameRightThere(string stopOn) => UiThread.Run(async () =>
        {
            var session = HotSeat(11);
            var table = new TableDriver(session, 11);
            var eventsAtStop = -1;
            void StopHere()
            {
                if (eventsAtStop < 0)
                {
                    eventsAtStop = table.Events.Count;
                    session.Stop();
                }
            }

            switch (stopOn)
            {
                case "deal":
                    session.RoundStarted += StopHere;
                    break;
                case "turn":
                    session.TurnStarted += (_, _) => StopHere();
                    break;
                case "lead":
                    session.MovePlayed += move => StopHere();
                    break;
                case "trick":
                    session.TrickCollected += trick => StopHere();
                    break;
                default:
                    session.RoundFinished += round => StopHere();
                    break;
            }

            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            await Task.Delay(30);

            Assert.True(eventsAtStop > 0);
            Assert.Equal(eventsAtStop, table.Events.Count);
            Assert.False(session.IsRunning);
            Assert.False(session.IsAwaitingMove(PlayerSlot.First));
            Assert.False(session.IsAwaitingMove(PlayerSlot.Second));
            Assert.False(session.TryPlay(PlayerSlot.First, PlayerAction.CloseGame()));
            Assert.False(session.TryPlay(PlayerSlot.Second, PlayerAction.CloseGame()));
            Assert.Empty(table.Errors);
        });

        // A wait that ended just before the stop has already queued its continuation on the UI
        // thread: the table pause (its timer fired) or the Continue after a round (completed on
        // the thread pool). That continuation must not raise the trick or the next deal. Here the
        // UI thread is kept busy until the wait is over, then the game is stopped.
        [Theory]
        [InlineData("table pause")]
        [InlineData("continue")]
        public void AWaitThatEndedJustBeforeAStopShouldRaiseNothing(string wait) => UiThread.Run(async () =>
        {
            var session = new GameSession(GameMode.HotSeat, "Ann", "Bob", null, new GamePace(0, 1), new Random(13).Next);
            var table = new TableDriver(session, 13);
            var eventsAtStop = -1;
            void StopOnceTheWaitIsOver()
            {
                SynchronizationContext.Current!.Post(
                    _ =>
                    {
                        Thread.Sleep(100);
                        eventsAtStop = table.Events.Count;
                        session.Stop();
                    },
                    null);
            }

            if (wait == "table pause")
            {
                session.MovePlayed += move =>
                {
                    if (eventsAtStop < 0 && table.Moves.Count(m => m.Move.Action.Type == PlayerActionType.PlayCard) == 2)
                    {
                        StopOnceTheWaitIsOver();
                    }
                };
            }
            else
            {
                // The driver continues first (it subscribed first), then the stop is queued.
                session.RoundFinished += _ => StopOnceTheWaitIsOver();
            }

            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            await Task.Delay(30);

            Assert.True(eventsAtStop > 0);
            Assert.Equal(eventsAtStop, table.Events.Count);
            Assert.False(session.IsAwaitingMove(PlayerSlot.First));
            Assert.False(session.IsAwaitingMove(PlayerSlot.Second));
            Assert.Empty(table.Errors);
        });

        // "Play again" from inside an event handler: the stopped game must not go on to its next
        // turn (it used to raise TurnStarted for the old game, whose handlers then read the new,
        // not yet dealt one); the new game starts with its deal and plays cleanly to the end.
        [Fact]
        public void RestartingFromAnEventHandlerShouldStartOneCleanGame() => UiThread.Run(async () =>
        {
            var session = HotSeat(12);
            var table = new TableDriver(session, 12);
            var eventsAtRestart = -1;
            session.MovePlayed += _ =>
            {
                if (eventsAtRestart < 0)
                {
                    eventsAtRestart = table.Events.Count;
                    session.Restart();
                }
            };

            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));

            Assert.Empty(table.Errors);
            Assert.NotNull(table.Winner);
            Assert.Equal("round", table.Events[eventsAtRestart]);
            AssertPlayOrder(table.Events.Skip(eventsAtRestart).ToList());
        });

        [Fact]
        public void RestartShouldStartANewGameAndTheStoppedOneShouldStaySilent() => UiThread.Run(async () =>
        {
            var session = VsComputer("smart", 8, new GamePace(50, 0));
            var table = new TableDriver(session, 8);
            var restarted = false;
            table.Turn += (_, isHuman) =>
            {
                // Restart just as the computer starts thinking: its pause and move must not leak
                // into the new game.
                if (!isHuman && !restarted)
                {
                    restarted = true;
                    session.Restart();
                }
            };

            session.Start();
            var stoppedGame = session.Completion;
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(60));

            Assert.True(restarted);
            Assert.NotSame(stoppedGame, session.Completion);
            Assert.True(stoppedGame.IsCompletedSuccessfully);
            Assert.Empty(table.Errors);
            Assert.NotNull(table.Winner);

            // The stopped game got as far as the computer's turn; from the second deal on, the
            // events are one clean game.
            var secondDeal = table.Events.FindIndex(1, e => e is "round");
            Assert.Equal(new object[] { "round", ("turn", PlayerSlot.First, true) }, table.Events.Take(2));
            Assert.IsType<MoveInfo>(table.Events[2]);
            Assert.Equal(("turn", PlayerSlot.Second, false), table.Events[3]);
            Assert.Equal(4, secondDeal);
            AssertPlayOrder(table.Events.Skip(secondDeal).ToList());
        });

        [Fact]
        public void HintsShouldComeFromThePersonsOwnView() => UiThread.Run(async () =>
        {
            var session = VsComputer("smart", 9);
            var table = new TableDriver(session, 9) { AutoPlay = false };
            var hints = 0;
            table.Turn += (slot, isHuman) =>
            {
                if (!isHuman)
                {
                    Assert.Null(session.GetHint());
                    return;
                }

                var hint = session.GetHint();
                Assert.NotNull(hint);
                var expected = new ClaudePlayer().ChooseMove(session.GetView(slot)!);
                Assert.Equal(expected.Type, hint!.Type);
                Assert.Equal(expected.Card, hint.Card);
                Assert.True(session.TryPlay(slot, hint));
                hints++;
            };

            await table.PlayToTheEndAsync();
            Assert.True(hints > 20);
            Assert.True(session.SupportsHints);

            var hotSeat = HotSeat(9);
            Assert.False(hotSeat.SupportsHints);
            Assert.Null(hotSeat.GetHint());
        });

        [Fact]
        public void TheComputerShouldThinkAndFinishedTricksShouldStayBeforeTheyAreCollected() => UiThread.Run(async () =>
        {
            const int Think = 40;
            const int Settle = 60;
            var session = VsComputer("dummy", 10, new GamePace(Think, Settle));
            var table = new TableDriver(session, 10);
            var clock = Stopwatch.StartNew();
            var times = new List<(object Item, long At)>();
            table.Turn += (_, isHuman) => times.Add((isHuman ? "person" : "computer", clock.ElapsedMilliseconds));
            session.MovePlayed += move => times.Add((move, clock.ElapsedMilliseconds));
            session.TrickCollected += trick => times.Add((trick, clock.ElapsedMilliseconds));
            session.RoundFinished += _ => session.Stop();
            session.Start();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(30));

            var thinks = 0;
            var settles = 0;
            for (var i = 1; i < times.Count; i++)
            {
                var waited = times[i].At - times[i - 1].At;
                if (times[i].Item is MoveInfo { Slot: PlayerSlot.Second } && times[i - 1].Item is "computer")
                {
                    Assert.True(waited >= Think - 2, $"thought {waited} ms");
                    thinks++;
                }

                if (times[i].Item is TrickInfo && times[i - 1].Item is MoveInfo)
                {
                    Assert.True(waited >= Settle - 2, $"trick stayed {waited} ms");
                    settles++;
                }
            }

            Assert.True(thinks >= 3 && settles >= 3, $"{thinks} thinks, {settles} settles");
        });

        [Fact]
        public void AGameAgainstTheComputerNeedsAComputer()
        {
            Assert.Throws<ArgumentNullException>(() => new GameSession(GameMode.VsAi, "a", "b", null, GamePace.Instant));
        }

        private static GameSession HotSeat(int seed) =>
            new GameSession(GameMode.HotSeat, "Ann", "Bob", null, GamePace.Instant, new Random(seed).Next);

        private static GameSession VsComputer(string computer, int seed, GamePace? pace = null)
        {
            IRestorablePlayer player = computer switch
            {
                "dummy" => new DummyPlayerChangingTrump { Rng = new Random(seed) },
                "smart" => new SmartPlayer(),
                "claude" => new ClaudePlayer(),
                "neural" => new ClaudePlayerNeural(),
                _ => new ClaudePlayerIsmcts { TimeLimitMilliseconds = 5, Rng = new Random(seed) },
            };
            return new GameSession(GameMode.VsAi, "Ann", computer, player, pace ?? GamePace.Instant, new Random(seed).Next);
        }

        private static IEnumerable<(string Name, PlayerSlot Slot, bool IsHuman)> Turns(TableDriver table) =>
            table.Events.OfType<(string, PlayerSlot, bool)>();

        private static async Task AssertStopsQuietly(GameSession session, TableDriver table)
        {
            var events = table.Events.Count;
            session.Stop();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(30);

            Assert.True(session.Completion.IsCompletedSuccessfully);
            Assert.False(session.IsRunning);
            Assert.False(session.IsAwaitingMove(PlayerSlot.First));
            Assert.False(session.IsAwaitingMove(PlayerSlot.Second));
            Assert.False(session.TryPlay(PlayerSlot.First, PlayerAction.CloseGame()));
            Assert.Null(session.GetHint());
            session.Continue();
            Assert.Equal(events, table.Events.Count);
            Assert.Empty(table.Errors);
        }

        // The grammar of a game: a deal, then turns each answered by one move; a finished trick
        // right after its last move; a finished round right after its last trick, then the next
        // deal; the game over last.
        private static void AssertPlayOrder(IReadOnlyList<object> events)
        {
            Assert.Equal("round", events[0]);
            Assert.Equal("game over", events[^1]);
            for (var i = 1; i < events.Count; i++)
            {
                var previous = events[i - 1];
                switch (events[i])
                {
                    case "round":
                        Assert.IsType<RoundEndInfo>(previous);
                        break;
                    case ValueTuple<string, PlayerSlot, bool>:
                        Assert.True(previous is "round" or MoveInfo or TrickInfo { RoundOver: false }, $"a turn after {previous}");
                        break;
                    case MoveInfo move:
                        Assert.True(previous is ValueTuple<string, PlayerSlot, bool> turn && turn.Item2 == move.Slot, $"{move} after {previous}");
                        break;
                    case TrickInfo:
                        Assert.IsType<MoveInfo>(previous);
                        break;
                    case RoundEndInfo:
                        Assert.True(previous is TrickInfo { RoundOver: true }, $"a round result after {previous}");
                        break;
                    case "game over":
                        Assert.True(previous is RoundEndInfo { IsGameOver: true });
                        break;
                    default:
                        Assert.Fail($"unexpected {events[i]}");
                        break;
                }
            }
        }

        // The rounds add up: each award is the change in the game score, a draw awards nothing,
        // and the game ends the first time a player reaches the target.
        private static void AssertScoring(TableDriver table, GameSession session)
        {
            var rounds = table.Rounds.ToList();
            var (first, second) = (0, 0);
            for (var r = 0; r < rounds.Count; r++)
            {
                var round = rounds[r];
                Assert.Equal(first + round.FirstAwardedGamePoints, round.FirstGamePoints);
                Assert.Equal(second + round.SecondAwardedGamePoints, round.SecondGamePoints);
                if (round.IsDraw)
                {
                    Assert.Equal(0, round.FirstAwardedGamePoints + round.SecondAwardedGamePoints);
                }
                else
                {
                    var (won, lost) = round.WinnerSlot == PlayerSlot.First
                        ? (round.FirstAwardedGamePoints, round.SecondAwardedGamePoints)
                        : (round.SecondAwardedGamePoints, round.FirstAwardedGamePoints);
                    Assert.InRange(won, 1, 3);
                    Assert.Equal(0, lost);
                }

                (first, second) = (round.FirstGamePoints, round.SecondGamePoints);
                Assert.Equal(r == rounds.Count - 1, round.IsGameOver);
                if (!round.IsGameOver)
                {
                    Assert.True(first < session.GamePointsTarget && second < session.GamePointsTarget);
                }
            }

            Assert.True((table.Winner == PlayerSlot.First ? first : second) >= session.GamePointsTarget);
            Assert.Single(table.Events, e => e is "game over");
        }
    }
}
