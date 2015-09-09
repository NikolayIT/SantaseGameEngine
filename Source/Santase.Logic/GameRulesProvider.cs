namespace Santase.Logic
{
    using System;

    public static class GameRulesProvider
    {
        private static readonly Lazy<SantaseGameRules> SantaseLazy =
            new Lazy<SantaseGameRules>(() => new SantaseGameRules());

        public static IGameRules Santase => SantaseLazy.Value;
    }
}
