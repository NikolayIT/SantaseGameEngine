namespace Santase.Logic
{
    using System;

    public sealed class GameRulesProvider
    {
        private static readonly Lazy<SantaseGameRules> SantaseLazy =
            new Lazy<SantaseGameRules>(() => new SantaseGameRules());

        public static IGameRules Santase => SantaseLazy.Value;

        private class SantaseGameRules : IGameRules
        {
            public int RoundPointsForGoingOut => 66;
        }
    }
}
