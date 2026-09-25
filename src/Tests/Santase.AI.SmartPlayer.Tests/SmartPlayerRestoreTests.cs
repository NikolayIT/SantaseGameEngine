namespace Santase.AI.SmartPlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Tests.Shared;

    using Xunit;

    // SmartPlayer restored from a seat view must decide, and track cards, exactly as the one that
    // played the match through its callbacks.
    public class SmartPlayerRestoreTests
    {
        [Fact]
        public void ARestoredSmartPlayerShouldDecideAndTrackCardsExactlyAsTheLiveOne()
        {
            var decisions = 0;
            for (var m = 0; m < 150; m++)
            {
                var random = new Random(m);
                var live = new SmartPlayer();

                // Opponents that swap trumps, announce and close.
                IPlayer opponent = m % 2 == 0 ? new SmartPlayer() : new DummyPlayerChangingTrump { Rng = new Random(m) };
                var match = new SantaseMatch(live, opponent, new SantaseMatchOptions
                {
                    Shuffle = random.Next,
                    FirstToPlay = m % 3 == 0 ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer,
                });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    if (seat == PlayerPosition.SecondPlayer)
                    {
                        Assert.Equal(SantaseActResult.Ok, match.Act(seat, opponent.GetTurn(match.CreateTurnContext())));
                        continue;
                    }

                    var view = ModelCopy.Copy(match.GetView(seat));
                    var restored = new SmartPlayer();
                    var liveAction = live.GetTurn(match.CreateTurnContext());
                    var restoredAction = restored.ChooseMove(view);

                    var where = $"match {m}, round {view.RoundNumber}, trick {view.Tricks.Count + 1}";
                    Assert.True(liveAction.Type == restoredAction.Type && liveAction.Card == restoredAction.Card, where);
                    AssertSameTracker(live, restored, where);

                    Assert.Equal(SantaseActResult.Ok, match.Act(seat, liveAction));
                    decisions++;
                }
            }

            Assert.True(decisions > 8000);
        }

        [Fact]
        public void SmartPlayersRestoredForEveryMoveShouldStillBeatTheDummy()
        {
            var wins = 0;
            for (var m = 0; m < 40; m++)
            {
                var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = new Random(1000 + m).Next });
                match.Start();
                var dummyRng = new Random(m);
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    IRestorablePlayer bot = seat == PlayerPosition.FirstPlayer ? new SmartPlayer() : new DummyPlayerChangingTrump { Rng = dummyRng };
                    Assert.Equal(SantaseActResult.Ok, match.Act(seat, bot.ChooseMove(match.GetView(seat))));
                }

                wins += match.Winner == PlayerPosition.FirstPlayer ? 1 : 0;
            }

            Assert.True(wins >= 36, $"SmartPlayer won {wins}/40 from views");
        }

        private static void AssertSameTracker(SmartPlayer live, SmartPlayer restored, string where)
        {
            var trackerField = typeof(SmartPlayer).GetField("cardTracker", BindingFlags.Instance | BindingFlags.NonPublic);
            var a = (CardTracker)trackerField.GetValue(live);
            var b = (CardTracker)trackerField.GetValue(restored);
            Assert.True(Codes(a.UnknownCards) == Codes(b.UnknownCards), $"{where}: unknown [{Codes(a.UnknownCards)}] vs [{Codes(b.UnknownCards)}]");
            Assert.True(Codes(a.PlayedCards) == Codes(b.PlayedCards), $"{where}: played [{Codes(a.PlayedCards)}] vs [{Codes(b.PlayedCards)}]");

            var trumpField = typeof(CardTracker).GetField("trumpCard", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Equal(trumpField.GetValue(a), trumpField.GetValue(b));

            var handProperty = typeof(BasePlayer).GetProperty("Cards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Equal(Codes((IEnumerable<Card>)handProperty.GetValue(live)), Codes((IEnumerable<Card>)handProperty.GetValue(restored)));
        }

        private static string Codes(IEnumerable<Card> cards)
        {
            return string.Join(" ", cards.Select(CardCode.Format).OrderBy(c => c, StringComparer.Ordinal));
        }
    }
}
