namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.IO;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Reads a soft-target policy-distillation dataset (magic "STSP") produced by the simulator's
    /// --gen-distill-data mode. Each sample is (128 float32 features) + (24 float32 target
    /// distribution). Format spec is in PolicyTrainingDataCollector.cs. Split into two parallel flat
    /// arrays for fast random access during mini-batch shuffling, mirroring <see cref="TrainingDataset"/>.
    /// </summary>
    public sealed class SoftTrainingDataset
    {
        public const int ExpectedMagic = 0x50535453; // "STSP"
        public const int ExpectedVersion = 1;

        public SoftTrainingDataset(float[] features, float[] targets, int featureDim, int targetDim)
        {
            this.Features = features;
            this.Targets = targets;
            this.FeatureDim = featureDim;
            this.TargetDim = targetDim;
            this.SampleCount = targetDim > 0 ? targets.Length / targetDim : 0;
        }

        public float[] Features { get; }

        public float[] Targets { get; }

        public int FeatureDim { get; }

        public int TargetDim { get; }

        public int SampleCount { get; }

        public static SoftTrainingDataset Load(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException($"Bad magic 0x{magic:X8} (expected 0x{ExpectedMagic:X8})");
            }

            var version = reader.ReadInt32();
            if (version != ExpectedVersion)
            {
                throw new InvalidDataException($"Unsupported version {version} (expected {ExpectedVersion})");
            }

            var featureDim = reader.ReadInt32();
            if (featureDim != NeuralFeatureEncoder.FeatureCount)
            {
                throw new InvalidDataException(
                    $"Feature dim mismatch: file has {featureDim}, code expects {NeuralFeatureEncoder.FeatureCount}");
            }

            var targetDim = reader.ReadInt32();
            if (targetDim != NeuralFeatureEncoder.CardCount)
            {
                throw new InvalidDataException(
                    $"Target dim mismatch: file has {targetDim}, code expects {NeuralFeatureEncoder.CardCount}");
            }

            var count = reader.ReadInt32();
            if (count <= 0)
            {
                throw new InvalidDataException($"Bad sample count {count}");
            }

            var features = new float[(long)count * featureDim];
            var targets = new float[(long)count * targetDim];
            var featBytes = featureDim * sizeof(float);
            var tgtBytes = targetDim * sizeof(float);
            var rowBuffer = new byte[featBytes + tgtBytes];

            for (var i = 0; i < count; i++)
            {
                ReadExactly(stream, rowBuffer, rowBuffer.Length);
                Buffer.BlockCopy(rowBuffer, 0, features, i * featBytes, featBytes);
                Buffer.BlockCopy(rowBuffer, featBytes, targets, i * tgtBytes, tgtBytes);
            }

            return new SoftTrainingDataset(features, targets, featureDim, targetDim);
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int count)
        {
            var read = 0;
            while (read < count)
            {
                var n = stream.Read(buffer, read, count - read);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                read += n;
            }
        }
    }
}
