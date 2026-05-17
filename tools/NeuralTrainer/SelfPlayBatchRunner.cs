namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.AI.ClaudePlayer;
    using Santase.AI.ClaudePlayer.Neural;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// Runs a batch of self-play games (trainee NN with sampling vs heuristic ClaudePlayer)
    /// in parallel and returns flat per-decision arrays ready for a policy-gradient step.
    /// The trainee plays the first position in even-indexed games and the second position in
    /// odd-indexed games — keeps the dataset balanced under any positional asymmetry of the
    /// engine (starting hand, deal order, etc.).
    /// </summary>
    public sealed class SelfPlayBatchRunner
    {
        private readonly byte[] weightSnapshot;
        private readonly float temperature;

        public SelfPlayBatchRunner(byte[] weightSnapshot, float temperature)
        {
            this.weightSnapshot = weightSnapshot;
            this.temperature = temperature;
        }

        /// <summary>
        /// Deterministic evaluation: plays <paramref name="gameCount"/> games with the trainee
        /// at <see cref="ClaudePlayerNeural.Temperature"/> = 0 (argmax — the production policy),
        /// no trajectory recording, no array flattening. Returns just the trainee win count.
        /// This is the TRUE quality metric; the training-loop EMA is over the sampled policy.
        /// </summary>
        public int EvaluateWins(int gameCount)
        {
            var traineeWins = 0;

            Parallel.For(
                0,
                gameCount,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                () => 0,
                (gameIdx, _, localWins) =>
            {
                NeuralNetwork perGameNet;
                using (var ms = new MemoryStream(this.weightSnapshot, writable: false))
                {
                    perGameNet = NeuralNetwork.LoadFromStream(ms);
                }

                var neural = new ClaudePlayerNeural(perGameNet) { Temperature = 0f };
                var heuristic = new ClaudePlayer();

                IPlayer first;
                IPlayer second;
                PlayerPosition traineePosition;
                if (gameIdx % 2 == 0)
                {
                    first = neural;
                    second = heuristic;
                    traineePosition = PlayerPosition.FirstPlayer;
                }
                else
                {
                    first = heuristic;
                    second = neural;
                    traineePosition = PlayerPosition.SecondPlayer;
                }

                var game = new SantaseGame(first, second);
                var startingPosition = gameIdx % 2 == 0
                    ? PlayerPosition.FirstPlayer
                    : PlayerPosition.SecondPlayer;
                var winner = game.Start(startingPosition);

                if (winner == traineePosition)
                {
                    localWins++;
                }

                return localWins;
            },
                localWins => Interlocked.Add(ref traineeWins, localWins));

            return traineeWins;
        }

        /// <summary>
        /// Collects PPO trajectories: per game, the trainee plays at Temperature = 1 (on-policy)
        /// and every NN decision records (features, action, legalMask, oldLogProb). Reward is
        /// potential-based shaped — r'_t = gamma*phi(s_{t+1}) - phi(s_t) with the round-point
        /// potential phi = myPts - oppPts (already normalized in the feature vector), plus the
        /// terminal +/-1 game outcome at the last decision (terminal phi treated as 0). This
        /// preserves the optimal policy while densifying credit. GAE is computed by the caller
        /// (it needs the critic), so we emit per-game segment boundaries.
        /// </summary>
        public PpoBatch CollectPpo(int gameCount, float gamma)
        {
            var fragments = new List<PpoStep>[gameCount];
            var traineeWon = new bool[gameCount];
            var traineeWins = 0;
            var winsLock = new object();

            Parallel.For(
                0,
                gameCount,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                gameIdx =>
            {
                var steps = new List<PpoStep>(64);

                NeuralNetwork perGameNet;
                using (var ms = new MemoryStream(this.weightSnapshot, writable: false))
                {
                    perGameNet = NeuralNetwork.LoadFromStream(ms);
                }

                var neural = new ClaudePlayerNeural(perGameNet)
                {
                    Temperature = 1f,
                    PpoRecorder = (features, action, mask, oldLp) =>
                    {
                        var copy = new float[features.Length];
                        Array.Copy(features, copy, features.Length);
                        var phi = features[NeuralFeatureEncoder.MyPointsOffset]
                                  - features[NeuralFeatureEncoder.OppPointsOffset];
                        steps.Add(new PpoStep
                        {
                            Features = copy,
                            Action = (byte)action,
                            Mask = mask,
                            OldLogProb = oldLp,
                            Phi = phi,
                        });
                    },
                };

                var heuristic = new ClaudePlayer();

                IPlayer first;
                IPlayer second;
                PlayerPosition traineePosition;
                if (gameIdx % 2 == 0)
                {
                    first = neural;
                    second = heuristic;
                    traineePosition = PlayerPosition.FirstPlayer;
                }
                else
                {
                    first = heuristic;
                    second = neural;
                    traineePosition = PlayerPosition.SecondPlayer;
                }

                var game = new SantaseGame(first, second);
                var startingPosition = gameIdx % 2 == 0
                    ? PlayerPosition.FirstPlayer
                    : PlayerPosition.SecondPlayer;
                var winner = game.Start(startingPosition);

                var won = winner == traineePosition;
                traineeWon[gameIdx] = won;
                fragments[gameIdx] = steps;

                if (won)
                {
                    lock (winsLock)
                    {
                        traineeWins++;
                    }
                }
            });

            var total = 0;
            for (var g = 0; g < gameCount; g++)
            {
                if (fragments[g] != null)
                {
                    total += fragments[g].Count;
                }
            }

            var features = new float[(long)total * NeuralFeatureEncoder.FeatureCount];
            var actions = new byte[total];
            var masks = new int[total];
            var oldLogProbs = new float[total];
            var rewards = new float[total];
            var segStart = new System.Collections.Generic.List<int>();
            var segLen = new System.Collections.Generic.List<int>();

            var w = 0;
            for (var g = 0; g < gameCount; g++)
            {
                var steps = fragments[g];
                if (steps == null || steps.Count == 0)
                {
                    continue;
                }

                var n = steps.Count;
                segStart.Add(w);
                segLen.Add(n);
                var rTerminal = traineeWon[g] ? 1f : -1f;

                for (var t = 0; t < n; t++)
                {
                    var st = steps[t];
                    Buffer.BlockCopy(
                        st.Features,
                        0,
                        features,
                        w * NeuralFeatureEncoder.FeatureCount * sizeof(float),
                        st.Features.Length * sizeof(float));
                    actions[w] = st.Action;
                    masks[w] = st.Mask;
                    oldLogProbs[w] = st.OldLogProb;

                    // Potential-based shaping. Terminal step uses phi(s')=0 and adds R_terminal.
                    if (t < n - 1)
                    {
                        rewards[w] = (gamma * steps[t + 1].Phi) - st.Phi;
                    }
                    else
                    {
                        rewards[w] = rTerminal - st.Phi;
                    }

                    w++;
                }
            }

            return new PpoBatch(
                features,
                actions,
                masks,
                oldLogProbs,
                rewards,
                segStart.ToArray(),
                segLen.ToArray(),
                traineeWins,
                gameCount - traineeWins);
        }

        private struct PpoStep
        {
            public float[] Features;
            public byte Action;
            public int Mask;
            public float OldLogProb;
            public float Phi;
        }

        /// <summary>
        /// Flat PPO rollout. Samples are grouped by game: game k occupies indices
        /// [SegStart[k], SegStart[k]+SegLen[k]); the last index of each segment is terminal
        /// (the caller treats V(s') = 0 there when computing GAE).
        /// </summary>
        public sealed class PpoBatch
        {
            public PpoBatch(
                float[] features,
                byte[] actions,
                int[] legalMasks,
                float[] oldLogProbs,
                float[] stepRewards,
                int[] segStart,
                int[] segLen,
                int wins,
                int losses)
            {
                this.Features = features;
                this.Actions = actions;
                this.LegalMasks = legalMasks;
                this.OldLogProbs = oldLogProbs;
                this.StepRewards = stepRewards;
                this.SegStart = segStart;
                this.SegLen = segLen;
                this.TraineeWins = wins;
                this.TraineeLosses = losses;
            }

            public float[] Features { get; }

            public byte[] Actions { get; }

            public int[] LegalMasks { get; }

            public float[] OldLogProbs { get; }

            public float[] StepRewards { get; }

            public int[] SegStart { get; }

            public int[] SegLen { get; }

            public int TraineeWins { get; }

            public int TraineeLosses { get; }

            public int SampleCount => this.Actions.Length;
        }
    }
}
