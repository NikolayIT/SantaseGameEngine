namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Stage 1: supervised cloning of the heuristic. Reads a training dataset emitted by the
    /// simulator's --gen-training-data mode and trains the policy MLP to imitate ClaudePlayer.
    /// Output is the warm-start the PPO stage fine-tunes from.
    /// </summary>
    public static class SupervisedProgram
    {
        public static int Run(string[] args)
        {
            var dataPath = "training_smoke.bin";
            var outPath = "weights.bin";
            var epochs = 10;
            var batchSize = 256;
            var learningRate = 1e-3f;
            var seed = 1337;
            var soft = false;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--supervised":
                        break;
                    case "--soft":
                        soft = true;
                        break;
                    case "--data":
                        dataPath = args[++i];
                        break;
                    case "--out":
                        outPath = args[++i];
                        break;
                    case "--epochs":
                        epochs = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    case "--batch":
                        batchSize = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    case "--lr":
                        learningRate = float.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    case "--seed":
                        seed = int.Parse(args[++i], CultureInfo.InvariantCulture);
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        return 2;
                }
            }

            if (!File.Exists(dataPath))
            {
                Console.Error.WriteLine($"Dataset not found: {dataPath}");
                return 2;
            }

            if (soft)
            {
                return RunSoft(dataPath, outPath, epochs, batchSize, learningRate, seed);
            }

            Console.WriteLine($"Loading dataset from {dataPath}");
            var swLoad = Stopwatch.StartNew();
            var dataset = TrainingDataset.Load(dataPath);
            Console.WriteLine(
                $"Loaded {dataset.SampleCount:N0} samples, "
                + $"feature_dim={dataset.FeatureDim} in {swLoad.Elapsed}");

            Console.WriteLine(
                $"Config: epochs={epochs}, batch={batchSize}, lr={learningRate}, seed={seed}");

            var trainer = new MLPTrainer(seed);
            var rng = new Random(seed);
            var indices = new int[dataset.SampleCount];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            var swTrain = Stopwatch.StartNew();
            for (var epoch = 1; epoch <= epochs; epoch++)
            {
                Shuffle(indices, rng);

                double sumLoss = 0;
                double sumAcc = 0;
                var batches = 0;

                var epochSw = Stopwatch.StartNew();
                for (var start = 0; start + batchSize <= dataset.SampleCount; start += batchSize)
                {
                    var (loss, acc) = trainer.TrainBatch(
                        dataset.Features,
                        dataset.Labels,
                        indices,
                        start,
                        batchSize,
                        learningRate,
                        beta1: 0.9f,
                        beta2: 0.999f,
                        eps: 1e-8f);
                    sumLoss += loss;
                    sumAcc += acc;
                    batches++;
                }

                var meanLoss = sumLoss / batches;
                var meanAcc = sumAcc / batches;
                Console.WriteLine(
                    $"Epoch {epoch,3}/{epochs}: loss={meanLoss:F4} acc={meanAcc:P2} "
                    + $"({batches:N0} batches, {epochSw.Elapsed})");
            }

            Console.WriteLine($"Training finished in {swTrain.Elapsed}");

            trainer.SaveWeights(outPath);
            var fi = new FileInfo(outPath);
            Console.WriteLine($"Weights written to {outPath} ({fi.Length:N0} bytes)");
            return 0;
        }

        // Soft-target distillation: same loop as the hard path but reads an STSP dataset (24-float
        // root-visit distributions) and trains with cross-entropy against those soft labels.
        private static int RunSoft(string dataPath, string outPath, int epochs, int batchSize, float learningRate, int seed)
        {
            Console.WriteLine($"Loading soft-target dataset from {dataPath}");
            var swLoad = Stopwatch.StartNew();
            var dataset = SoftTrainingDataset.Load(dataPath);
            Console.WriteLine(
                $"Loaded {dataset.SampleCount:N0} samples, "
                + $"feature_dim={dataset.FeatureDim} target_dim={dataset.TargetDim} in {swLoad.Elapsed}");

            Console.WriteLine(
                $"Config (soft): epochs={epochs}, batch={batchSize}, lr={learningRate}, seed={seed}");

            var trainer = new MLPTrainer(seed);
            var rng = new Random(seed);
            var indices = new int[dataset.SampleCount];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            var swTrain = Stopwatch.StartNew();
            for (var epoch = 1; epoch <= epochs; epoch++)
            {
                Shuffle(indices, rng);

                double sumLoss = 0;
                double sumAcc = 0;
                var batches = 0;

                var epochSw = Stopwatch.StartNew();
                for (var start = 0; start + batchSize <= dataset.SampleCount; start += batchSize)
                {
                    var (loss, acc) = trainer.TrainBatchSoft(
                        dataset.Features,
                        dataset.Targets,
                        indices,
                        start,
                        batchSize,
                        learningRate,
                        beta1: 0.9f,
                        beta2: 0.999f,
                        eps: 1e-8f);
                    sumLoss += loss;
                    sumAcc += acc;
                    batches++;
                }

                var meanLoss = sumLoss / batches;
                var meanAcc = sumAcc / batches;
                Console.WriteLine(
                    $"Epoch {epoch,3}/{epochs}: loss={meanLoss:F4} agree={meanAcc:P2} "
                    + $"({batches:N0} batches, {epochSw.Elapsed})");
            }

            Console.WriteLine($"Training finished in {swTrain.Elapsed}");

            trainer.SaveWeights(outPath);
            var fi = new FileInfo(outPath);
            Console.WriteLine($"Weights written to {outPath} ({fi.Length:N0} bytes)");
            return 0;
        }

        private static void Shuffle(int[] arr, Random rng)
        {
            for (var i = arr.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
