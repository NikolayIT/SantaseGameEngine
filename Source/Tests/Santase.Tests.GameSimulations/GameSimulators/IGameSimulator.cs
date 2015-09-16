namespace Santase.Tests.GameSimulations.GameSimulators
{
    public interface IGameSimulator
    {
        GameSimulationResult Simulate(int numberOfGames);
    }
}
