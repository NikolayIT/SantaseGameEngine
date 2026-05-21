namespace Santase.Tests.GameSimulations.GameSimulators
{
    using System;
    using System.Diagnostics;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Players;

    /// <summary>
    /// Round-robin ELO tournament for the AI players the MAUI UI offers as opponents.
    /// Every pair plays a head-to-head match (with alternating start, so first-player
    /// advantage cancels), then ratings are fit with the Bradley-Terry model — which is
    /// exactly what ELO's logistic 400-point scale models — and anchored so the weakest
    /// bot (DummyPlayerChangingTrump) sits at <see cref="AnchorElo"/>. The printed numbers
    /// are baked into the UI's opponent catalog.
    /// </summary>
    public static class EloTournament
    {
        // The human starts below this (see the UI's PlayerRatingStore) and earns rating upward.
        private const double AnchorElo = 1200d;

        // Bradley-Terry maps a strength ratio to a win probability; ELO is that on a base-10,
        // 400-point scale: elo_gap = 400 * log10(strength_ratio).
        private const double EloPerDecade = 400d;

        // A uniform 50/50 prior worth this fraction of each pair's games. It keeps a blow-out
        // (e.g. the net beats the dummy ~100%) from producing an infinite rating gap: the
        // implied pairwise rate is shrunk toward 50%, capping any single pair near ~920 ELO,
        // while leaving close matchups essentially untouched.
        private const double PriorFraction = 0.01d;

        public static void Run(int fastGames, int slowGames)
        {
            var competitors = new[]
            {
                new Competitor("DummyPlayerChangingTrump", () => new DummyPlayerChangingTrump(), isSlow: false),
                new Competitor("SmartPlayer", () => new SmartPlayer(), isSlow: false),
                new Competitor("ClaudePlayer", () => new ClaudePlayer(), isSlow: false),
                new Competitor("ClaudePlayerNeural", () => new ClaudePlayerNeural(), isSlow: false),
                new Competitor("ClaudePlayerIsmcts", () => new ClaudePlayerIsmcts(), isSlow: true),
            };

            var n = competitors.Length;
            var wins = new double[n, n];
            var games = new double[n, n];

            Console.WriteLine($"ELO round-robin: {n} players, {fastGames:0,0} games/pair (fast), {slowGames:0,0} games/pair (with ISMCTS).");
            Console.WriteLine(new string('-', 75));

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var slow = competitors[i].IsSlow || competitors[j].IsSlow;
                    var g = slow ? slowGames : fastGames;

                    var simulator = new GameSimulator(competitors[i].Factory, competitors[j].Factory);
                    var result = simulator.Simulate(g);

                    wins[i, j] += result.FirstPlayerWins;
                    wins[j, i] += result.SecondPlayerWins;
                    games[i, j] += g;
                    games[j, i] += g;

                    var pct = 100.0 * result.FirstPlayerWins / g;
                    Console.WriteLine(
                        $"  {competitors[i].Name,-26} {result.FirstPlayerWins,7:0,0} - {result.SecondPlayerWins,-7:0,0} {competitors[j].Name,-26} ({pct,5:0.0}% | {result.SimulationDuration})");
                }
            }

            var ratings = FitElo(wins, games, n);

            // Sort indices by rating, strongest first, for the printed table.
            var order = new int[n];
            for (var i = 0; i < n; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (a, b) => ratings[b].CompareTo(ratings[a]));

            Console.WriteLine(new string('=', 75));
            Console.WriteLine($"ELO ratings (anchor {competitors[0].Name} = {AnchorElo:0}), total time {sw.Elapsed}:");
            Console.WriteLine(new string('-', 75));
            foreach (var i in order)
            {
                Console.WriteLine($"  {competitors[i].Name,-26} {(int)Math.Round(ratings[i]),6}");
            }

            Console.WriteLine(new string('=', 75));
        }

        // Bradley-Terry strengths via the standard minorization-maximization (MM) iteration,
        // then converted to the ELO scale and shifted so the anchor competitor (index 0) sits
        // at AnchorElo.
        private static double[] FitElo(double[,] wins, double[,] games, int n)
        {
            var winEff = new double[n, n];
            var gamesEff = new double[n, n];
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var prior = PriorFraction * games[i, j];
                    winEff[i, j] = wins[i, j] + (0.5 * prior);
                    gamesEff[i, j] = games[i, j] + prior;
                }
            }

            var strength = new double[n];
            for (var i = 0; i < n; i++)
            {
                strength[i] = 1d;
            }

            for (var iter = 0; iter < 10000; iter++)
            {
                var next = new double[n];
                var maxDelta = 0d;
                for (var i = 0; i < n; i++)
                {
                    var totalWins = 0d;
                    var denom = 0d;
                    for (var j = 0; j < n; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        totalWins += winEff[i, j];
                        denom += gamesEff[i, j] / (strength[i] + strength[j]);
                    }

                    next[i] = denom > 0d ? totalWins / denom : strength[i];
                }

                // Normalize to geometric mean 1 to keep the iteration numerically stable.
                var logSum = 0d;
                for (var i = 0; i < n; i++)
                {
                    logSum += Math.Log(next[i]);
                }

                var scale = Math.Exp(-logSum / n);
                for (var i = 0; i < n; i++)
                {
                    next[i] *= scale;
                    maxDelta = Math.Max(maxDelta, Math.Abs(next[i] - strength[i]));
                    strength[i] = next[i];
                }

                if (maxDelta < 1e-12)
                {
                    break;
                }
            }

            var ratings = new double[n];
            for (var i = 0; i < n; i++)
            {
                ratings[i] = EloPerDecade * Math.Log10(strength[i]);
            }

            var shift = AnchorElo - ratings[0];
            for (var i = 0; i < n; i++)
            {
                ratings[i] += shift;
            }

            return ratings;
        }

        private sealed class Competitor
        {
            public Competitor(string name, Func<IPlayer> factory, bool isSlow)
            {
                this.Name = name;
                this.Factory = factory;
                this.IsSlow = isSlow;
            }

            public string Name { get; }

            public Func<IPlayer> Factory { get; }

            public bool IsSlow { get; }
        }
    }
}
