namespace Santase.AI.ClaudePlayer.Tests
{
    using System;
    using System.Text;

    using Santase.AI.ClaudePlayer.Tests.TestHelpers;
    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    // Bots on a server keep nothing between moves: they are restored from the seat view. These
    // tests hold every shipped bot to deciding exactly as it does when it plays through callbacks.
    public class RestoreFromViewTests
    {
        [Fact]
        public void ARestoredClaudePlayerShouldDecideAndRememberExactlyAsTheLiveOne()
        {
            var decisions = RestoreEquivalence.Check(() => new ClaudePlayer(), () => new ClaudePlayerNeural(), 60, 100);
            decisions += RestoreEquivalence.Check(() => new ClaudePlayer(), NewSwappingDummy, 60, 200, Reseed);
            Assert.True(decisions > 2500);
        }

        [Fact]
        public void ARestoredNeuralPlayerShouldDecideAndRememberExactlyAsTheLiveOne()
        {
            var decisions = RestoreEquivalence.Check(() => new ClaudePlayerNeural(), () => new ClaudePlayer(), 60, 300);
            decisions += RestoreEquivalence.Check(() => new ClaudePlayerNeural(), NewSwappingDummy, 60, 400, Reseed);
            Assert.True(decisions > 2500);
        }

        [Fact]
        public void ARestoredIsmctsPlayerShouldDecideAndRememberExactlyAsTheLiveOne()
        {
            // A fixed iteration count and a reseeded RNG make the search repeatable. The opponents
            // swap trumps, announce and close, so every card inference gets rebuilt.
            var decisions = RestoreEquivalence.Check(NewRepeatableIsmcts, () => new ClaudePlayer(), 12, 500, Reseed);
            decisions += RestoreEquivalence.Check(NewRepeatableIsmcts, NewSwappingDummy, 12, 600, Reseed);
            Assert.True(decisions > 400);
        }

        [Fact]
        public void RestoredDummiesShouldDecideExactlyAsTheLiveOnes()
        {
            var decisions = RestoreEquivalence.Check(() => new DummyPlayer(), NewSwappingDummy, 40, 700, Reseed);
            decisions += RestoreEquivalence.Check(NewSwappingDummy, () => new ClaudePlayer(), 40, 800, Reseed);
            Assert.True(decisions > 2000);
        }

        [Fact]
        public void BotsShouldPlayWholeMatchesFromViewsAlone()
        {
            // Every move from a new bot restored from a copy of the view; the strong bot must still win.
            var neuralWins = RestoreEquivalence.PlayStateless(() => new ClaudePlayerNeural(), () => new DummyPlayerChangingTrump(), 20, 900);
            Assert.True(neuralWins >= 18, $"ClaudePlayerNeural won {neuralWins}/20 from views");

            RestoreEquivalence.PlayStateless(() => new ClaudePlayer(), () => new ClaudePlayerNeural(), 10, 950);
            RestoreEquivalence.PlayStateless(() => new ClaudePlayerIsmcts { TimeLimitMilliseconds = 2 }, () => new DummyPlayer(), 3, 990);
        }

        [Fact]
        public void DummiesShouldBeReproducibleWithASeededRng()
        {
            Assert.Equal(PlayDummies(42), PlayDummies(42));
            Assert.NotEqual(PlayDummies(42), PlayDummies(43));
        }

        [Fact]
        public void RestoreShouldRefuseAViewWithoutASeat()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = new Random(1).Next });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, PlayerAction.PlayCard(match.GetView(match.ToMove).PlayableCards[0]));
            }

            IRestorablePlayer[] bots = { new ClaudePlayer(), new ClaudePlayerNeural(), new ClaudePlayerIsmcts(), new DummyPlayer(), new DummyPlayerChangingTrump() };
            foreach (var bot in bots)
            {
                Assert.Throws<ArgumentException>(() => bot.Restore(match.GetFinalView()));
                Assert.Throws<ArgumentNullException>(() => bot.Restore(null));
            }
        }

        private static IPlayer NewSwappingDummy()
        {
            return new DummyPlayerChangingTrump();
        }

        private static IPlayer NewRepeatableIsmcts()
        {
            return new ClaudePlayerIsmcts { TimeLimitMilliseconds = 1_000_000, MaxIterations = 80 };
        }

        private static void Reseed(IPlayer player, int seed)
        {
            switch (player)
            {
                case DummyPlayer dummy:
                    dummy.Rng = new Random(seed);
                    break;
                case DummyPlayerChangingTrump dummy:
                    dummy.Rng = new Random(seed);
                    break;
                case ClaudePlayerIsmcts search:
                    search.Rng = new Random(seed);
                    break;
            }
        }

        // The whole move sequence of a match between the two dummies, seeded from one number.
        private static string PlayDummies(int seed)
        {
            var random = new Random(seed);
            var first = new DummyPlayer { Rng = new Random(random.Next()) };
            var second = new DummyPlayerChangingTrump { Rng = new Random(random.Next()) };
            var match = new SantaseMatch(first, second, new SantaseMatchOptions { Shuffle = new Random(random.Next()).Next });
            match.Start();
            var moves = new StringBuilder();
            while (!match.IsFinished)
            {
                var player = match.ToMove == PlayerPosition.FirstPlayer ? (IPlayer)first : second;
                var action = player.GetTurn(match.CreateTurnContext());
                moves.Append(action.Type).Append(action.Card).Append(' ');
                Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, action));
            }

            return moves.ToString();
        }
    }
}
