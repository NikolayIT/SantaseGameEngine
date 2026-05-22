namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Tests.GameSimulations.Training;

    /// <summary>
    /// Generates soft-target policy-distillation data. Both players are <see cref="ClaudePlayerIsmcts"/>
    /// (the strongest player in the repo) with their <c>PolicyRecorder</c> wired to a shared
    /// <see cref="PolicyTrainingDataCollector"/>, so every searched card decision is captured as a
    /// (features, root-visit-distribution) pair — the AlphaZero-style soft label. The exact deck-empty
    /// endgame solve and forced single moves route elsewhere in the base and are not recorded, matching
    /// the decision set <see cref="ClaudePlayerNeural"/> asks its net at inference.
    /// The per-move time budget trades search strength for throughput; the root distribution converges
    /// fast (root fan-out is at most the hand size), so a reduced budget still yields strong targets.
    /// </summary>
    public class ClaudeIsmctsDistillationSimulator : BaseGameSimulator
    {
        private readonly PolicyTrainingDataCollector collector;
        private readonly int timeLimitMilliseconds;

        public ClaudeIsmctsDistillationSimulator(PolicyTrainingDataCollector collector, int timeLimitMilliseconds)
        {
            this.collector = collector;
            this.timeLimitMilliseconds = timeLimitMilliseconds;
        }

        protected override ISantaseGame CreateGame()
        {
            var first = new ClaudePlayerIsmcts
            {
                TimeLimitMilliseconds = this.timeLimitMilliseconds,
                PolicyRecorder = this.collector.Record,
            };
            var second = new ClaudePlayerIsmcts
            {
                TimeLimitMilliseconds = this.timeLimitMilliseconds,
                PolicyRecorder = this.collector.Record,
            };
            return new SantaseGame(first, second);
        }
    }
}
