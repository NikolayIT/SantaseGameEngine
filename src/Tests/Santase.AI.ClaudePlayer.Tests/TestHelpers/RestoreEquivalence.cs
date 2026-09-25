namespace Santase.AI.ClaudePlayer.Tests.TestHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Tests.Shared;

    using Xunit;

    // The IRestorablePlayer contract: a bot restored from a seat view decides exactly as the same
    // bot that played the match through its callbacks, and remembers exactly the same things.
    internal static class RestoreEquivalence
    {
        // The search's per-move scratch: written when a search runs, so a live bot keeps the last
        // search's values across forced moves and exact endgame solves while a fresh one has none.
        private static readonly HashSet<string> SearchScratch = new HashSet<string>
        {
            "nodeCount", "worldTrumpSuit", "worldDeckLength", "worldClosed", "isLeader", "ledHash",
            "oppHandCount", "faceDownDeck", "rootPhase", "trumpHash", "rootMyPoints", "rootOppPoints",
            "myHandMask", "poolCount", "poolMask", "dealKnownMask", "dealFreeCount",
        };

        // Plays matches between live bots (they get the usual callbacks as the match's observers).
        // At every decision of the bot under test (the first seat), a fresh one is restored from
        // a copy of the view built from its public properties and must choose the same move and end up with the same
        // memory. Returns the number of decisions checked.
        public static int Check(
            Func<IPlayer> create,
            Func<IPlayer> createOpponent,
            int matches,
            int seed,
            Action<IPlayer, int> reseed = null)
        {
            var decisions = 0;
            for (var m = 0; m < matches; m++)
            {
                var random = new Random(seed + m);
                var live = create();
                var opponent = createOpponent();
                var match = new SantaseMatch(live, opponent, new SantaseMatchOptions
                {
                    Shuffle = random.Next,
                    FirstToPlay = m % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer,
                });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var moveSeed = random.Next();
                    if (seat == PlayerPosition.SecondPlayer)
                    {
                        reseed?.Invoke(opponent, moveSeed);
                        Assert.Equal(SantaseActResult.Ok, match.Act(seat, opponent.GetTurn(match.CreateTurnContext())));
                        continue;
                    }

                    var view = RoundTrip(match.GetView(seat));
                    var restored = create();
                    reseed?.Invoke(live, moveSeed);
                    reseed?.Invoke(restored, moveSeed);

                    var liveAction = live.GetTurn(match.CreateTurnContext());
                    var restoredAction = ((IRestorablePlayer)restored).ChooseMove(view);

                    var where = $"match {m}, round {view.RoundNumber}, trick {view.Tricks.Count + 1}";
                    Assert.True(
                        liveAction.Type == restoredAction.Type && liveAction.Card == restoredAction.Card,
                        $"{where}: live {liveAction}, restored {restoredAction}");
                    AssertSameMemory(live, restored, where);

                    Assert.Equal(SantaseActResult.Ok, match.Act(seat, liveAction));
                    decisions++;
                }
            }

            return decisions;
        }

        // Plays whole matches keeping no bot between moves: every move comes from a new bot
        // restored from a copy of the view of the seat to move. Returns the first seat's wins.
        public static int PlayStateless(Func<IPlayer> createFirst, Func<IPlayer> createSecond, int matches, int seed)
        {
            var firstWins = 0;
            for (var m = 0; m < matches; m++)
            {
                var random = new Random(seed + m);
                var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var bot = (IRestorablePlayer)(seat == PlayerPosition.FirstPlayer ? createFirst() : createSecond());
                    Assert.Equal(SantaseActResult.Ok, match.Act(seat, bot.ChooseMove(RoundTrip(match.GetView(seat)))));
                }

                if (match.Winner == PlayerPosition.FirstPlayer)
                {
                    firstWins++;
                }
            }

            return firstWins;
        }

        // What a host does: map the view to its own model and back (here: a property-by-property copy).
        public static SantaseSeatView RoundTrip(SantaseSeatView view)
        {
            return ModelCopy.Copy(view);
        }

        // Every scalar, card and card-set field, down the class hierarchy.
        public static void AssertSameMemory(object live, object restored, string where)
        {
            for (var type = live.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (SearchScratch.Contains(field.Name))
                    {
                        continue;
                    }

                    var fieldType = field.FieldType;
                    var a = field.GetValue(live);
                    var b = field.GetValue(restored);
                    if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType == typeof(Card))
                    {
                        Assert.True(Equals(a, b), $"{where}: {type.Name}.{field.Name} live {a}, restored {b}");
                    }
                    else if (typeof(IEnumerable<Card>).IsAssignableFrom(fieldType))
                    {
                        Assert.True(
                            Codes(a) == Codes(b),
                            $"{where}: {type.Name}.{field.Name} live [{Codes(a)}], restored [{Codes(b)}]");
                    }
                }
            }
        }

        private static string Codes(object cards)
        {
            return cards == null ? "null" : string.Join(" ", ((IEnumerable<Card>)cards).Select(CardCode.Format).OrderBy(c => c, StringComparer.Ordinal));
        }
    }
}
