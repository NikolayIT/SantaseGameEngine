namespace Santase.Tests.GameSimulations
{
    using System;

    using Santase.Tests.GameSimulations.GameSimulators;

    public static class Program
    {
        public static void Main()
        {
            IGameSimulator gameSimulator = new SmartPlayersGameSimulator();
            var simulationResult = gameSimulator.Simulate(100000);

            Console.WriteLine(simulationResult.SimulationDuration);
            Console.WriteLine(
                $"Total games: {simulationResult.FirstPlayerWins:0,0} - {simulationResult.SecondPlayerWins:0,0}");
            Console.WriteLine(
                $"Total round points: {simulationResult.FirstPlayerTotalRoundPoints:0,0} - {simulationResult.SecondPlayerTotalRoundPoints:0,0}");
        }
    }
}
