namespace Santase.Tests.GameSimulations
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Santase.AI.SmartPlayer;
    using Santase.Tests.GameSimulations.GameSimulators;
    using Santase.Tests.GameSimulations.Training;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "--gen-training-data")
            {
                RunTrainingDataGeneration(args);
                return;
            }

            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine(new string('=', 75));
#if DEBUG
            Console.Write("Mode=Debug");
#elif RELEASE
            Console.Write("Mode=Release");
#endif
            Console.Write($", CPUs={Environment.ProcessorCount}, OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', 75));

            // For easier debugging start a single game:
            //// new SantaseGame(new SmartPlayer(), new SmartPlayerOld()).Start();

            var sw = Stopwatch.StartNew();

            //// SimulateGames(new SmartPlayersGameSimulator(), 200000);

            //// SimulateGames(new SmartAndBestExternalPlayerGameSimulator(), 200000);

            //// SimulateGames(new SmartAndDummyPlayerChangingTrumpSimulator(), 200000);

            //// SimulateGames(new SmartAndDummyPlayersSimulator(), 200000);

            // Iteration-#1 panel: modified heuristic Claude vs every opponent, paired with
            // the frozen ClaudePlayerBaseline vs the same opponents so the delta is visible
            // in one invocation. Neural matchups kept at the bottom for context.
            SimulateGames(new ClaudeVsBaselineSimulator(), 200000);

            SimulateGames(new ClaudeAndSmartPlayerSimulator(), 200000);
            SimulateGames(new ClaudeBaselineAndSmartPlayerSimulator(), 200000);

            SimulateGames(new ClaudeAndBestExternalPlayerGameSimulator(), 200000);
            SimulateGames(new ClaudeBaselineAndBestExternalPlayerGameSimulator(), 200000);

            SimulateGames(new ClaudeAndDummyPlayerChangingTrumpSimulator(), 200000);
            SimulateGames(new ClaudeBaselineAndDummyPlayerChangingTrumpSimulator(), 200000);

            SimulateGames(new ClaudeAndDummyPlayersSimulator(), 200000);
            SimulateGames(new ClaudeBaselineAndDummyPlayersSimulator(), 200000);

            SimulateGames(new ClaudeNeuralVsClaudeSimulator(), 200000);

            Console.WriteLine($"Total tests time: {sw.Elapsed}");
        }

        private static void SimulateGames(IGameSimulator gameSimulator, int gamesCount = 100000)
        {
            Console.Write($"Running {gameSimulator.GetType().Name}... ");

            var simulationResult = gameSimulator.Simulate(gamesCount);

            Console.WriteLine(simulationResult.SimulationDuration);
            Console.WriteLine($"Games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0} (total: {gamesCount:0,0}, diff: {simulationResult.FirstPlayerWins - simulationResult.SecondPlayerWins:0,0})");
            Console.WriteLine($"Round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0} (rounds: {simulationResult.RoundsPlayed:0,0})");
            Console.WriteLine($"Global counters: {string.Join(", ", GlobalStats.GlobalCounterValues)} (closed: {GlobalStats.GamesClosedByPlayer:0,0})");
            Console.WriteLine(new string('=', 75));
        }

        private static void RunTrainingDataGeneration(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: --gen-training-data <games> <output-path>");
                Environment.Exit(2);
                return;
            }

            if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var games) || games <= 0)
            {
                Console.Error.WriteLine($"Invalid game count: {args[1]}");
                Environment.Exit(2);
                return;
            }

            var outputPath = args[2];

            Console.WriteLine($"Generating training data: {games:0,0} self-play games -> {outputPath}");

            var collector = new TrainingDataCollector();
            var simulator = new ClaudeSelfPlayTrainingSimulator(collector);

            var sw = Stopwatch.StartNew();
            var result = simulator.Simulate(games);
            var elapsed = sw.Elapsed;

            collector.WriteTo(outputPath);

            Console.WriteLine($"Played {games:0,0} games in {elapsed}");
            Console.WriteLine($"Wins: {result.FirstPlayerWins:0,0} - {result.SecondPlayerWins:0,0}, rounds: {result.RoundsPlayed:0,0}");
            Console.WriteLine($"Recorded {collector.SampleCount:0,0} training samples to {outputPath}");
        }
    }
}
