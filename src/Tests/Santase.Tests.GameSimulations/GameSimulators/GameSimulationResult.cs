namespace Santase.Tests.GameSimulations.GameSimulators
{
    using System;

    public class GameSimulationResult
    {
        public int FirstPlayerWins { get; set; }

        public int FirstPlayerTotalRoundPoints { get; set; }

        public int SecondPlayerWins { get; set; }

        public int SecondPlayerTotalRoundPoints { get; set; }

        public int RoundsPlayed { get; set; }

        public TimeSpan SimulationDuration { get; set; }
    }
}
