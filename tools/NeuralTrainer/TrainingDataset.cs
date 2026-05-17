namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.IO;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Reads the entire training dataset produced by the simulator's --gen-training-data mode
    /// into RAM. Format spec is in TrainingDataCollector.cs; we just verify the header and
    /// gulp the body. Float features are stored contiguously: [sample0_features, sample0_label,
    /// sample1_features, sample1_label, ...]. We split into two parallel arrays for fast random
    /// access during mini-batch shuffling.
    /// </summary>
    public sealed class TrainingDataset
    {
        public const int ExpectedMagic = 0x45535453; // "STSE"
        public const int ExpectedVersion = 1;

        public TrainingDataset(float[] features, byte[] labels, int featureDim)
        {
            this.Features = features;
            this.Labels = labels;
            this.FeatureDim = featureDim;
            this.SampleCount = labels.Length;
        }

        public float[] Features { get; }

        public byte[] Labels { get; }

        public int FeatureDim { get; }

        public int SampleCount { get; }

        public static TrainingDataset Load(string path)
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

            var count = reader.ReadInt32();
            if (count <= 0)
            {
                throw new InvalidDataException($"Bad sample count {count}");
            }

            var features = new float[(long)count * featureDim];
            var labels = new byte[count];
            var floatBytes = featureDim * sizeof(float);
            var rowBuffer = new byte[floatBytes];

            for (var i = 0; i < count; i++)
            {
                ReadExactly(stream, rowBuffer, floatBytes);
                Buffer.BlockCopy(rowBuffer, 0, features, i * floatBytes, floatBytes);
                var label = stream.ReadByte();
                if (label < 0)
                {
                    throw new EndOfStreamException($"Truncated dataset at sample {i}");
                }

                labels[i] = (byte)label;
            }

            return new TrainingDataset(features, labels, featureDim);
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
