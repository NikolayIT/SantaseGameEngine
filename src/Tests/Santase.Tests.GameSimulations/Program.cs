namespace Santase.Tests.GameSimulations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.NinjaPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Players;
    using Santase.Tests.GameSimulations.GameSimulators;
    using Santase.Tests.GameSimulations.Players;
    using Santase.Tests.GameSimulations.Training;

    public static class Program
    {
        private const int DefaultGamesPerMatchup = 200000;

        private const string DefaultSuiteName = "claude";

        // Named matchup suites. Pick one with the first CLI arg (default: "claude"); the
        // optional second arg overrides the game count. This replaces toggling blocks of
        // commented-out simulator calls.
        private static readonly Dictionary<string, Func<GameSimulator[]>> Suites =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultSuiteName] = BuildClaudeSuite,
                ["ismcts"] = BuildIsmctsSuite,
                ["smart"] = BuildSmartSuite,
                ["baseline"] = BuildBaselineSuite,
            };

        public static void Main(string[] args)
        {
            if (args is { Length: > 0 } && args[0] == "--gen-training-data")
            {
                RunTrainingDataGeneration(args);
                return;
            }

            if (args is { Length: > 0 } && args[0] == "--gen-distill-data")
            {
                RunDistillationDataGeneration(args);
                return;
            }

            if (args is { Length: > 0 } && string.Equals(args[0], "elo", StringComparison.OrdinalIgnoreCase))
            {
                RunEloTournament(args);
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

            var suiteName = args is { Length: > 0 } ? args[0] : DefaultSuiteName;
            if (!Suites.TryGetValue(suiteName, out var buildSuite))
            {
                Console.WriteLine($"Unknown suite '{suiteName}'. Available: {string.Join(", ", Suites.Keys)}. Running '{DefaultSuiteName}'.");
                suiteName = DefaultSuiteName;
                buildSuite = Suites[DefaultSuiteName];
            }

            var gamesPerMatchup = args is { Length: > 1 }
                                  && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedGames)
                                  && parsedGames > 0
                                      ? parsedGames
                                      : DefaultGamesPerMatchup;

            Console.WriteLine($"Suite '{suiteName}', {gamesPerMatchup:0,0} games per matchup.");
            Console.WriteLine(new string('=', 75));

            var sw = Stopwatch.StartNew();
            foreach (var simulator in buildSuite())
            {
                SimulateGames(simulator, gamesPerMatchup);
            }

            Console.WriteLine($"Total tests time: {sw.Elapsed}");
        }

        private static void SimulateGames(GameSimulator gameSimulator, int gamesCount)
        {
            Console.Write($"Running {gameSimulator.Name}... ");

            var simulationResult = gameSimulator.Simulate(gamesCount);

            Console.WriteLine(simulationResult.SimulationDuration);
            Console.WriteLine($"Games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0} (total: {gamesCount:0,0}, diff: {simulationResult.FirstPlayerWins - simulationResult.SecondPlayerWins:0,0})");
            Console.WriteLine($"Round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0} (rounds: {simulationResult.RoundsPlayed:0,0})");
            Console.WriteLine($"Global counters: {string.Join(", ", GlobalStats.GlobalCounterValues)} (closed: {GlobalStats.GamesClosedByPlayer:0,0})");
            Console.WriteLine(new string('=', 75));
        }

        // ClaudePlayerNeural (the PPO-trained net) then the heuristic ClaudePlayer, each vs every
        // other player. The ClaudePlayer-vs-ClaudePlayerBaseline matchup measures heuristic changes
        // against the frozen snapshot.
        private static GameSimulator[] BuildClaudeSuite()
        {
            return new[]
            {
                new GameSimulator(() => new ClaudePlayerNeural(), () => new ClaudePlayer()),
                new GameSimulator(() => new ClaudePlayerNeural(), () => new SmartPlayer()),
                new GameSimulator(() => new ClaudePlayerNeural(), () => new NinjaPlayer()),
                new GameSimulator(() => new ClaudePlayerNeural(), () => new DummyPlayerChangingTrump()),
                new GameSimulator(() => new ClaudePlayerNeural(), () => new DummyPlayer()),

                new GameSimulator(() => new ClaudePlayer(), () => new ClaudePlayerBaseline()),
                new GameSimulator(() => new ClaudePlayer(), () => new SmartPlayer()),
                new GameSimulator(() => new ClaudePlayer(), () => new NinjaPlayer()),
                new GameSimulator(() => new ClaudePlayer(), () => new DummyPlayerChangingTrump()),
                new GameSimulator(() => new ClaudePlayer(), () => new DummyPlayer()),
            };
        }

        // ClaudePlayerIsmcts (single information-set tree, the repo's strongest player) vs the panel.
        // Each move spends its full wall-clock budget (default 100ms), so this suite is far slower
        // per game than the others — run it with a small game count, e.g. `-- ismcts 300`.
        private static GameSimulator[] BuildIsmctsSuite()
        {
            return new[]
            {
                new GameSimulator(() => new ClaudePlayerIsmcts(), () => new ClaudePlayerNeural()),
                new GameSimulator(() => new ClaudePlayerIsmcts(), () => new ClaudePlayer()),
                new GameSimulator(() => new ClaudePlayerIsmcts(), () => new SmartPlayer()),
                new GameSimulator(() => new ClaudePlayerIsmcts(), () => new NinjaPlayer()),
                new GameSimulator(() => new ClaudePlayerIsmcts(), () => new DummyPlayer()),
            };
        }

        // SmartPlayer ad-hoc workloads (kept for debugging SmartPlayer in isolation).
        private static GameSimulator[] BuildSmartSuite()
        {
            return new[]
            {
                new GameSimulator(() => new SmartPlayer(), () => new SmartPlayerOld()),
                new GameSimulator(() => new SmartPlayer(), () => new NinjaPlayer()),
                new GameSimulator(() => new SmartPlayer(), () => new DummyPlayerChangingTrump()),
                new GameSimulator(() => new SmartPlayer(), () => new DummyPlayer()),
            };
        }

        // Frozen ClaudePlayerBaseline vs every opponent — absolute reference numbers for the
        // within-run delta workflow when iterating on the ClaudePlayer heuristic.
        private static GameSimulator[] BuildBaselineSuite()
        {
            return new[]
            {
                new GameSimulator(() => new ClaudePlayerBaseline(), () => new SmartPlayer()),
                new GameSimulator(() => new ClaudePlayerBaseline(), () => new NinjaPlayer()),
                new GameSimulator(() => new ClaudePlayerBaseline(), () => new DummyPlayerChangingTrump()),
                new GameSimulator(() => new ClaudePlayerBaseline(), () => new DummyPlayer()),
            };
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

        // ISMCTS self-play soft-target distillation: both players are the strongest search player,
        // and every searched decision is recorded as (features, root-visit-distribution). Feeds
        // NeuralTrainer --supervised --soft. ISMCTS spends a per-move budget (default 35ms, far below
        // play-strength 100ms — the root distribution converges fast), so this is much slower than the
        // heuristic --gen-training-data; size the game count accordingly.
        private static void RunDistillationDataGeneration(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: --gen-distill-data <games> <output-path> [budgetMs]");
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
            var budgetMs = 35;
            if (args.Length > 3
                && int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBudget)
                && parsedBudget > 0)
            {
                budgetMs = parsedBudget;
            }

            Console.WriteLine($"Generating ISMCTS distillation data: {games:0,0} self-play games @ {budgetMs}ms/move -> {outputPath}");
            Console.WriteLine($"CPUs={Environment.ProcessorCount}");

            var collector = new PolicyTrainingDataCollector();
            var simulator = new ClaudeIsmctsDistillationSimulator(collector, budgetMs);

            var sw = Stopwatch.StartNew();
            var result = simulator.Simulate(games);
            var elapsed = sw.Elapsed;

            collector.WriteTo(outputPath);

            Console.WriteLine($"Played {games:0,0} games in {elapsed}");
            Console.WriteLine($"Wins: {result.FirstPlayerWins:0,0} - {result.SecondPlayerWins:0,0}, rounds: {result.RoundsPlayed:0,0}");
            Console.WriteLine($"Recorded {collector.SampleCount:0,0} distillation samples to {outputPath}");
        }

        // `elo [fastGames] [ismctsGames]` — round-robin among the five UI opponents, then a
        // Bradley-Terry/ELO fit. ISMCTS spends its full per-move budget so its pairings are
        // capped at a much smaller game count by default.
        private static void RunEloTournament(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var fastGames = args.Length > 1
                            && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFast)
                            && parsedFast > 0
                                ? parsedFast
                                : 20000;

            var slowGames = args.Length > 2
                            && int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSlow)
                            && parsedSlow > 0
                                ? parsedSlow
                                : 200;

            EloTournament.Run(fastGames, slowGames);
        }
    }
}
