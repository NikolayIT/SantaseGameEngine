namespace Santase.Tests.GameSimulations
{
    using System;

    using Santase.AI.SmartPlayer;
    using Santase.Tests.GameSimulations.GameSimulators;

    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine(new string('=', 75));
            Console.Write(DateTime.Now);
#if DEBUG
            Console.Write(", Mode=Debug");
#elif RELEASE
            Console.Write(", Mode=Release"); 
#endif
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
            Console.WriteLine($"Total games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0}");
            Console.WriteLine($"Rounds played: {simulationResult.RoundsPlayed:0,0}");
            Console.WriteLine($"Total round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0}");
            Console.WriteLine($"Closed games: {GlobalStats.GamesClosedByPlayer}");
            Console.WriteLine($"Global counters: {string.Join(", ", GlobalStats.GlobalCounterValues)}");
            Console.WriteLine(new string('=', 75));
        }
    }
}
