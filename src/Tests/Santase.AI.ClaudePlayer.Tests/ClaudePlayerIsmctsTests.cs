namespace Santase.AI.ClaudePlayer.Tests
{
    using System;

    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class ClaudePlayerIsmctsTests
    {
        // A tiny per-move budget and deterministic RNG keep these tests fast and reproducible while
        // still exercising the full ISMCTS path (per-iteration determinization, availability-count
        // UCB, expansion of the shared tree, rollout, endgame solve).
        private static ClaudePlayerIsmcts NewFastIsmcts()
        {
            return new ClaudePlayerIsmcts
            {
                TimeLimitMilliseconds = 3,
                Rng = new Random(2024),
            };
        }

        [Fact]
        public void IsmctsPlayerShouldPlayFullGamesWithoutThrowingAgainstDummy()
        {
            IPlayer ismcts = NewFastIsmcts();
            IPlayer dummy = new DummyPlayer();
            var game = new SantaseGame(ismcts, dummy);

            const int Games = 30;
            for (var i = 0; i < Games; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                Assert.True(
                    winner == PlayerPosition.FirstPlayer || winner == PlayerPosition.SecondPlayer,
                    $"Game {i} ended with no winner.");
            }
        }

        [Fact]
        public void IsmctsPlayerShouldBeatDummyInTheVastMajorityOfGames()
        {
            const int Games = 60;
            var ismcts = NewFastIsmcts();
            var dummy = new DummyPlayer();
            var game = new SantaseGame(ismcts, dummy);

            var ismctsWins = 0;
            for (var i = 0; i < Games; i++)
            {
                if (game.Start(PlayerPosition.FirstPlayer) == PlayerPosition.FirstPlayer)
                {
                    ismctsWins++;
                }
            }

            Assert.True(ismctsWins >= 0.8 * Games, $"IsmctsPlayer won {ismctsWins}/{Games} vs DummyPlayer; expected >= 80%");
        }

        [Fact]
        public void IsmctsPlayerShouldPlayFullGamesAgainstClaudePlayer()
        {
            // Two stateful bookkeepers (the ISMCTS belief and ClaudePlayer's UnknownCards) in the
            // same game — catches any desync between the shared belief bookkeeping and the engine.
            IPlayer ismcts = NewFastIsmcts();
            IPlayer heuristic = new ClaudePlayer();
            var game = new SantaseGame(ismcts, heuristic);

            const int Games = 10;
            for (var i = 0; i < Games; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                Assert.True(winner == PlayerPosition.FirstPlayer || winner == PlayerPosition.SecondPlayer);
            }
        }
    }
}
