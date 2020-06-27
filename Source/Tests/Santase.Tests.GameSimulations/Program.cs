namespace Santase.Tests.GameSimulations
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Santase.AI.SmartPlayer;
    using Santase.Tests.GameSimulations.GameSimulators;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine(new string('=', 75));
            Console.Write(DateTime.Now);
#if DEBUG
            Console.Write(", Mode=Debug");
#elif RELEASE
            Console.Write(", Mode=Release");
#endif
            Console.Write($", CPUs={Environment.ProcessorCount}, OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', 75));

            // For easier debugging start a single game:
            //// new SantaseGame(new SmartPlayer(), new SmartPlayerOld()).Start();

            SimulateGames(new SmartPlayersGameSimulator(), 200000);

            SimulateGames(new SmartAndBestExternalPlayerGameSimulator());

            SimulateGames(new SmartAndDummyPlayerChangingTrumpSimulator());

            SimulateGames(new SmartAndDummyPlayersSimulator());
        }

        private static void SimulateGames(IGameSimulator gameSimulator, int gamesCount = 100000)
        {
            Console.WriteLine($"Running {gameSimulator.GetType().Name}...");

            var simulationResult = gameSimulator.Simulate(gamesCount);

            Console.WriteLine(simulationResult.SimulationDuration);
            Console.WriteLine($"Games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0} (total: {gamesCount:0,0}, diff: {simulationResult.FirstPlayerWins - simulationResult.SecondPlayerWins})");
            Console.WriteLine($"Round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0} (rounds: {simulationResult.RoundsPlayed:0,0})");
            Console.WriteLine($"Global counters: {string.Join(", ", GlobalStats.GlobalCounterValues)} (closed: {GlobalStats.GamesClosedByPlayer:0,0})");
            Console.WriteLine(new string('=', 75));
        }
    }
}
