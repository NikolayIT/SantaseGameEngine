namespace Santase.Tests.GameSimulations.GameSimulators
{
    using Santase.AI.ClaudePlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Tests.GameSimulations.Training;

    /// <summary>
    /// Generates supervised-cloning training data for the neural policy. Both players are
    /// <see cref="ClaudePlayer"/>, both have their <c>TrainingRecorder</c> wired to a shared
    /// <see cref="TrainingDataCollector"/>, so every heuristic-path decision (~70% of all turns
    /// in a Santase game) is captured as a (features, chosen_card_idx) pair. Minimax-decided
    /// turns are skipped — at inference time, the neural player still uses minimax for those.
    /// </summary>
    public class ClaudeSelfPlayTrainingSimulator : BaseGameSimulator
    {
        private readonly TrainingDataCollector collector;

        public ClaudeSelfPlayTrainingSimulator(TrainingDataCollector collector)
        {
            this.collector = collector;
        }

        protected override ISantaseGame CreateGame()
        {
            var first = new ClaudePlayer { TrainingRecorder = this.collector.Record };
            var second = new ClaudePlayer { TrainingRecorder = this.collector.Record };
            return new SantaseGame(first, second);
        }
    }
}
