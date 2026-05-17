namespace Santase.AI.ClaudePlayer.Tests
{
    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class ClaudePlayerNeuralTests
    {
        [Fact]
        public void NeuralPlayerShouldPlayFullGamesWithoutThrowingAgainstDummy()
        {
            // Random Xavier-init weights make this a near-random policy; we don't care who wins.
            // The point is to drive every code path (lead/follow, phase 1/2, trump swap, close)
            // through the network without producing illegal moves or crashes.
            IPlayer neural = new ClaudePlayerNeural();
            IPlayer dummy = new DummyPlayer();
            var game = new SantaseGame(neural, dummy);

            const int Games = 50;
            for (var i = 0; i < Games; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                Assert.True(
                    winner == PlayerPosition.FirstPlayer || winner == PlayerPosition.SecondPlayer,
                    $"Game {i} ended with no winner.");
            }
        }

        [Fact]
        public void NeuralPlayerShouldPlayFullGamesAgainstClaudePlayer()
        {
            // Same shape, but against the trained heuristic-based ClaudePlayer rather than a
            // random bot. Catches any regression in the engine's interaction with two stateful
            // bookkeepers (UnknownCards, etc.) running in the same game.
            IPlayer neural = new ClaudePlayerNeural();
            IPlayer heuristic = new ClaudePlayer();
            var game = new SantaseGame(neural, heuristic);

            const int Games = 20;
            for (var i = 0; i < Games; i++)
            {
                var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
                Assert.True(winner == PlayerPosition.FirstPlayer || winner == PlayerPosition.SecondPlayer);
            }
        }
    }
}
