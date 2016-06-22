namespace Santase.Tests.GameSimulations
{
    using System;

    using Santase.AI.SmartPlayer;
    using Santase.Tests.GameSimulations.GameSimulators;

    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine(DateTime.Now);

            // For easier debugging start a single game:
            //// new SantaseGame(new SmartPlayer(), new SmartPlayerOld()).Start();

            SimulateGames(new SmartAndBestExternalPlayerGameSimulator());

            SimulateGames(new SmartPlayersGameSimulator());

            SimulateGames(new SmartAndDummyPlayerChangingTrumpSimulator());

            SimulateGames(new SmartAndDummyPlayersSimulator());
        }

        private static void SimulateGames(IGameSimulator gameSimulator)
        {
            Console.WriteLine($"Running {gameSimulator.GetType().Name}...");

            var simulationResult = gameSimulator.Simulate(100000);

            Console.WriteLine(simulationResult.SimulationDuration);
            Console.WriteLine($"Total games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0}");
            Console.WriteLine($"Rounds played: {simulationResult.RoundsPlayed:0,0}");
            Console.WriteLine(
                $"Total round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0}");
            Console.WriteLine("Closed games: {0}", GlobalStats.GamesClosedByPlayer);
            Console.WriteLine(new string('=', 75));
        }
    }
}
