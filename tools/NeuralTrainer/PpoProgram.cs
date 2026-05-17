namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// PPO training entry (activated by --ppo). Per generation: collect on-policy self-play
    /// (Temperature = 1) vs the heuristic, compute GAE advantages with the training-only
    /// critic, run K clipped-surrogate epochs, then evaluate the deterministic policy and
    /// keep weights.best.bin. Reuses the same early-stop / collapse-guard harness as the
    /// REINFORCE path so an unattended long run is safe.
    /// </summary>
    public static class PpoProgram
    {
        public static int Run(string[] args)
        {
            var inPath = "weights.bin";
            var outDir = "checkpoints";
            var hours = 10.0;
            var gamesPerGen = 500;
            var minibatch = 1024;
            var epochs = 4;
            var actorLr = 1e-4f;
            var criticLr = 3e-4f;
            var clipEps = 0.2f;
            var entCoef = 0.01f;
            var valueCoef = 0.5f;
            var gamma = 0.997f;
            var lambda = 0.95f;
            var gradClip = 0.5f;
            var evalEveryGens = 25;
            var evalGames = 6000;
            var patience = 20;
            var collapseFactor = 0.7;
            var checkpointEveryGens = 50;
            var seed = 1337;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--ppo": break;
                    case "--in": inPath = args[++i]; break;
                    case "--out": outDir = args[++i]; break;
                    case "--hours": hours = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--games-per-gen": gamesPerGen = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--minibatch": minibatch = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--epochs": epochs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--actor-lr": actorLr = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--critic-lr": criticLr = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--clip": clipEps = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--ent": entCoef = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--vf": valueCoef = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--gamma": gamma = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--lambda": lambda = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--grad-clip": gradClip = float.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--eval-every": evalEveryGens = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--eval-games": evalGames = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--patience": patience = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--collapse-factor": collapseFactor = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--checkpoint-every": checkpointEveryGens = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--seed": seed = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        return 2;
                }
            }

            if (!File.Exists(inPath))
            {
                Console.Error.WriteLine($"Input weights not found: {inPath}");
                return 2;
            }

            Directory.CreateDirectory(outDir);

            Console.WriteLine($"PPO trainer starting at {DateTime.Now:O}");
            Console.WriteLine(
                $"in={inPath} out={outDir} hours={hours} games/gen={gamesPerGen} "
                + $"mb={minibatch} epochs={epochs} aLr={actorLr} cLr={criticLr} "
                + $"clip={clipEps} ent={entCoef} vf={valueCoef} g={gamma} lam={lambda} "
                + $"gclip={gradClip} seed={seed}");
            Console.WriteLine(
                $"eval-every={evalEveryGens}gens eval-games={evalGames} "
                + $"patience={patience} collapse={collapseFactor} CPUs={Environment.ProcessorCount}");
            Console.WriteLine(new string('-', 78));

            var ppo = new PpoTrainer(seed);
            ppo.LoadActor(inPath);
            Console.WriteLine($"Loaded actor from {inPath} (critic fresh)");

            var deadline = DateTime.UtcNow.AddHours(hours);
            var rng = new Random(seed);
            var generation = 0;
            var bestEval = -1.0;
            var bestGen = 0;
            var stale = 0;
            var bestPath = Path.Combine(outDir, "weights.best.bin");
            string stopReason = null;
            var sw = Stopwatch.StartNew();

            while (DateTime.UtcNow < deadline)
            {
                generation++;
                var genSw = Stopwatch.StartNew();

                var snapshot = SnapshotActor(ppo);
                var runner = new SelfPlayBatchRunner(snapshot, temperature: 1f);
                var batch = runner.CollectPpo(gamesPerGen, gamma);
                var collectSec = genSw.Elapsed.TotalSeconds;

                var n = batch.SampleCount;
                if (n < minibatch)
                {
                    Console.WriteLine($"gen {generation}: only {n} samples (<mb {minibatch}), skipping");
                    continue;
                }

                var vOld = new float[n];
                ppo.PredictValues(batch.Features, n, vOld);

                var adv = new float[n];
                var ret = new float[n];
                ComputeGae(batch, vOld, gamma, lambda, adv, ret);

                NormalizeInPlace(adv);

                var idx = new int[n];
                for (var k = 0; k < n; k++)
                {
                    idx[k] = k;
                }

                double pl = 0, vl = 0, ent = 0, kl = 0, cf = 0;
                var updates = 0;
                var trainSw = Stopwatch.StartNew();
                for (var e = 0; e < epochs; e++)
                {
                    Shuffle(idx, rng);
                    for (var start = 0; start + minibatch <= n; start += minibatch)
                    {
                        var d = ppo.TrainMinibatch(
                            batch.Features, batch.Actions, batch.LegalMasks, batch.OldLogProbs,
                            adv, ret, idx, start, minibatch,
                            clipEps, entCoef, valueCoef, actorLr, criticLr, gradClip);
                        pl += d.PolicyLoss;
                        vl += d.ValueLoss;
                        ent += d.Entropy;
                        kl += d.ApproxKl;
                        cf += d.ClipFrac;
                        updates++;
                    }
                }

                trainSw.Stop();
                var inv = updates > 0 ? 1.0 / updates : 0;
                var trainWin = (double)batch.TraineeWins / (batch.TraineeWins + batch.TraineeLosses);

                Console.WriteLine(
                    $"gen {generation,4} winTrain {trainWin:P1} n {n,6} "
                    + $"pLoss {pl * inv:F4} vLoss {vl * inv:F4} ent {ent * inv:F3} "
                    + $"kl {kl * inv:F4} clip {cf * inv:P0} "
                    + $"col {collectSec:F1}s tr {trainSw.Elapsed.TotalSeconds:F1}s "
                    + $"el {FormatElapsed(sw.Elapsed)}");

                if (generation % checkpointEveryGens == 0)
                {
                    ppo.SaveActor(Path.Combine(outDir, $"weights.gen{generation:D5}.bin"));
                }

                if (generation % evalEveryGens == 0)
                {
                    var evalSnap = SnapshotActor(ppo);
                    var evalSw = Stopwatch.StartNew();
                    var wins = new SelfPlayBatchRunner(evalSnap, 0f).EvaluateWins(evalGames);
                    evalSw.Stop();
                    var er = (double)wins / evalGames;
                    var improved = er > bestEval;
                    if (improved)
                    {
                        bestEval = er;
                        bestGen = generation;
                        stale = 0;
                        ppo.SaveActor(bestPath);
                    }
                    else
                    {
                        stale++;
                    }

                    Console.WriteLine(
                        $"  EVAL gen {generation}: argmax {er:P2} ({wins}/{evalGames}) "
                        + $"best {bestEval:P2}@{bestGen} stale {stale}/{patience} "
                        + $"{evalSw.Elapsed.TotalSeconds:F1}s"
                        + (improved ? "  <- new best, saved weights.best.bin" : string.Empty));

                    if (bestEval > 0 && er < collapseFactor * bestEval)
                    {
                        stopReason = $"collapse (eval {er:P2} < {collapseFactor:P0} of best {bestEval:P2})";
                        break;
                    }

                    if (stale >= patience)
                    {
                        stopReason = $"early stop (no improvement {patience} evals; best {bestEval:P2}@{bestGen})";
                        break;
                    }
                }
            }

            stopReason ??= "wall-clock budget reached";
            var finalPath = Path.Combine(outDir, $"weights.final.gen{generation:D5}.bin");
            ppo.SaveActor(finalPath);

            Console.WriteLine(new string('-', 78));
            Console.WriteLine($"PPO finished at {DateTime.Now:O}");
            Console.WriteLine($"Stop reason: {stopReason}");
            Console.WriteLine($"Generations: {generation}, elapsed: {sw.Elapsed}");
            if (bestEval >= 0)
            {
                Console.WriteLine($"BEST argmax win rate: {bestEval:P2} at gen {bestGen}");
                Console.WriteLine($"  >>> promote this file: {bestPath} <<<");
            }
            else
            {
                Console.WriteLine("No evaluation ran; nothing to promote.");
            }

            Console.WriteLine($"(final weights, analysis only -> {finalPath})");
            return 0;
        }

        private static void ComputeGae(
            SelfPlayBatchRunner.PpoBatch batch,
            float[] vOld,
            float gamma,
            float lambda,
            float[] adv,
            float[] ret)
        {
            for (var seg = 0; seg < batch.SegStart.Length; seg++)
            {
                var s0 = batch.SegStart[seg];
                var len = batch.SegLen[seg];
                var gae = 0f;
                for (var t = len - 1; t >= 0; t--)
                {
                    var i = s0 + t;
                    float nextV;
                    float nonTerminal;
                    if (t == len - 1)
                    {
                        nextV = 0f;
                        nonTerminal = 0f; // terminal step: no bootstrap, GAE chain resets
                    }
                    else
                    {
                        nextV = vOld[i + 1];
                        nonTerminal = 1f;
                    }

                    var delta = batch.StepRewards[i] + (gamma * nextV * nonTerminal) - vOld[i];
                    gae = delta + (gamma * lambda * nonTerminal * gae);
                    adv[i] = gae;
                    ret[i] = gae + vOld[i];
                }
            }
        }

        private static void NormalizeInPlace(float[] a)
        {
            double mean = 0;
            for (var i = 0; i < a.Length; i++)
            {
                mean += a[i];
            }

            mean /= a.Length;

            double var = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var d = a[i] - mean;
                var += d * d;
            }

            var /= a.Length;
            var std = Math.Sqrt(var) + 1e-8;
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = (float)((a[i] - mean) / std);
            }
        }

        private static byte[] SnapshotActor(PpoTrainer ppo)
        {
            using var ms = new MemoryStream(NeuralNetwork.ExpectedWeightFileBytes);
            ppo.SaveActor(ms);
            return ms.ToArray();
        }

        private static void Shuffle(int[] arr, Random rng)
        {
            for (var i = arr.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        private static string FormatElapsed(TimeSpan t)
            => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
