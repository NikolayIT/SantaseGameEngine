namespace Santase.Tests.GameSimulations.GameSimulators
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;

    public abstract class BaseGameSimulator : IGameSimulator
    {
        public GameSimulationResult Simulate(int numberOfGames)
        {
            var stopwatch = Stopwatch.StartNew();

            GlobalStats.GamesClosedByPlayer = 0;
            var pointsLock = new object();
            var firstPlayerWins = 0;
            var firstPlayerRoundPoints = 0;
            var secondPlayerWins = 0;
            var secondPlayerRoundPoints = 0;
            var roundsPlayed = 0;

            Parallel.For(1, numberOfGames + 1, i =>
                {
                    if (i % 1000 == 0)
                    {
                        Console.Write(".");
                    }

                    var game = this.CreateGame();

                    var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);

                    lock (pointsLock)
                    {
                        if (winner == PlayerPosition.FirstPlayer)
                        {
                            firstPlayerWins++;
                        }
                        else
                        {
                            secondPlayerWins++;
                        }

                        firstPlayerRoundPoints += game.FirstPlayerTotalPoints;
                        secondPlayerRoundPoints += game.SecondPlayerTotalPoints;
                        roundsPlayed += game.RoundsPlayed;
                    }

                    // Console.WriteLine($"{i:00000} Games: {firstPlayerWins} - {secondPlayerWins} == Rounds: {game.FirstPlayerTotalPoints} - {game.SecondPlayerTotalPoints} ({game.RoundsPlayed} rounds)");
                });
            var simulationDuration = stopwatch.Elapsed;

            return new GameSimulationResult
                       {
                           FirstPlayerWins = firstPlayerWins,
                           FirstPlayerTotalRoundPoints = firstPlayerRoundPoints,
                           SecondPlayerWins = secondPlayerWins,
                           SecondPlayerTotalRoundPoints = secondPlayerRoundPoints,
                           RoundsPlayed = roundsPlayed,
                           SimulationDuration = simulationDuration
                       };
        }

        protected abstract ISantaseGame CreateGame();
    }
}
