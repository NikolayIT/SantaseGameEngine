namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Players;

    /// <summary>
    /// A selectable computer opponent: a display description plus the factory that builds the
    /// underlying <see cref="IPlayer"/>. The <see cref="Elo"/> values are not guessed — they are
    /// produced by the simulator's round-robin ELO tournament
    /// (<c>dotnet run ... -- elo</c>) and pasted in here, anchored so the Dummy sits at 1200.
    /// </summary>
    public sealed class AiOpponent
    {
        public AiOpponent(string id, string displayName, string tagline, int difficulty, int elo, Func<IPlayer> factory)
        {
            this.Id = id;
            this.DisplayName = displayName;
            this.Tagline = tagline;
            this.Difficulty = difficulty;
            this.Elo = elo;
            this.Factory = factory;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Tagline { get; }

        // 1..5, used only for the star flavour; the ELO badge carries the precise strength.
        public int Difficulty { get; }

        public int Elo { get; }

        public Func<IPlayer> Factory { get; }

        public string DifficultyStars =>
            new string('★', Math.Clamp(this.Difficulty, 0, 5)) +
            new string('☆', 5 - Math.Clamp(this.Difficulty, 0, 5));

        public string EloText => $"ELO {this.Elo}";

        public IPlayer CreatePlayer() => this.Factory();
    }

    public static class AiOpponents
    {
        // Ratings from the `elo` simulator round-robin (anchor: Dummy = 1200). Re-run that suite
        // and update these if the AI players change.
        public static IReadOnlyList<AiOpponent> All { get; } = new[]
        {
            new AiOpponent(
                "dummy",
                "Dummy",
                "Plays random legal cards. A gentle warm-up.",
                1,
                1200,
                () => new DummyPlayerChangingTrump()),
            new AiOpponent(
                "smart",
                "Smart Player",
                "Counts cards and follows solid heuristics.",
                2,
                2056,
                () => new SmartPlayer()),
            new AiOpponent(
                "claude",
                "Claude",
                "Hand-tuned heuristics with an exact endgame solver.",
                3,
                2103,
                () => new ClaudePlayer()),
            new AiOpponent(
                "neural",
                "Claude Neural",
                "A PPO-trained neural-network policy.",
                4,
                2261,
                () => new ClaudePlayerNeural()),
            new AiOpponent(
                "ismcts",
                "Claude MCTS",
                "Information-set Monte-Carlo tree search. The strongest.",
                5,
                2441,
                () => new ClaudePlayerIsmcts()),
        };

        public static AiOpponent ById(string? id) =>
            All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }
}
