namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Single training tool for the ClaudePlayerNeural policy net. Three subcommands mirror the
    /// pipeline that produced the shipped weights:
    ///   --supervised  clone the heuristic into the net          (-> weights_supervised.bin)
    ///   --ppo         PPO self-play fine-tune from that          (-> shipped weights.bin)
    ///   --validate    deterministic win rate vs the heuristic    (production-equivalent metric)
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "--validate")
            {
                return Validate(args);
            }

            if (Array.IndexOf(args, "--supervised") >= 0)
            {
                return SupervisedProgram.Run(args);
            }

            if (Array.IndexOf(args, "--ppo") >= 0)
            {
                return PpoProgram.Run(args);
            }

            Console.Error.WriteLine(
                "Usage:\n"
                + "  --supervised --data <dataset.bin> --out <weights.bin> [--epochs N] [--batch B] [--lr R]\n"
                + "  --ppo --in <weights.bin> --out <dir> [--hours N] [...]\n"
                + "  --validate <weights.bin> <games>");
            return 2;
        }

        // Deterministic (argmax) win rate vs the heuristic — the production-equivalent metric.
        private static int Validate(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: --validate <weights.bin> <games>");
                return 2;
            }

            var weightsPath = args[1];
            var games = int.Parse(args[2], CultureInfo.InvariantCulture);

            if (!File.Exists(weightsPath))
            {
                Console.Error.WriteLine($"Not found: {weightsPath}");
                return 2;
            }

            var snapshot = File.ReadAllBytes(weightsPath);
            var runner = new SelfPlayBatchRunner(snapshot, temperature: 0f);
            var sw = Stopwatch.StartNew();
            var wins = runner.EvaluateWins(games);
            sw.Stop();

            var winRate = (double)wins / games;
            Console.WriteLine(
                $"{Path.GetFileName(weightsPath)}: "
                + $"{wins}/{games} = {winRate:P2} vs heuristic "
                + $"({sw.Elapsed.TotalSeconds:F1}s)");
            return 0;
        }
    }
}
