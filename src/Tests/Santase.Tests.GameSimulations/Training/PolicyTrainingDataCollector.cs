namespace Santase.Tests.GameSimulations.Training
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Thread-safe accumulator for soft-target policy-distillation samples produced by a search
    /// player's <c>PolicyRecorder</c>. Each sample is (128 float32 features) + (24 float32 visit
    /// distribution) — the AlphaZero-style soft label for cloning the search into the MLP, far richer
    /// than the one-hot label <see cref="TrainingDataCollector"/> records for heuristic cloning.
    /// Each thread writes into its own private <see cref="MemoryStream"/>; <see cref="WriteTo"/>
    /// concatenates them in pointer order behind a small header.
    /// File format (little-endian):
    ///   bytes 0..3   magic "STSP"
    ///   bytes 4..7   version (currently 1)
    ///   bytes 8..11  feature dim (currently 128)
    ///   bytes 12..15 target dim (currently 24)
    ///   bytes 16..19 sample count
    ///   then sample_count records of (128 float32 features) + (24 float32 target distribution).
    /// </summary>
    public sealed class PolicyTrainingDataCollector
    {
        public const int Magic = 0x50535453; // "STSP" little-endian
        public const int Version = 1;

        private static readonly int FeatureBytes = NeuralFeatureEncoder.FeatureCount * sizeof(float);
        private static readonly int TargetBytes = NeuralFeatureEncoder.CardCount * sizeof(float);
        private static readonly int RecordBytes = FeatureBytes + TargetBytes;

        private readonly ThreadLocal<byte[]> recordBuffer =
            new(() => new byte[RecordBytes]);

        private readonly ThreadLocal<MemoryStream> threadBuffers =
            new(() => new MemoryStream(capacity: 1 << 16), trackAllValues: true);

        private long sampleCount;

        public long SampleCount => Interlocked.Read(ref this.sampleCount);

        public void Record(float[] features, float[] target)
        {
            if (features.Length != NeuralFeatureEncoder.FeatureCount)
            {
                throw new ArgumentException($"Expected {NeuralFeatureEncoder.FeatureCount} features.", nameof(features));
            }

            if (target.Length != NeuralFeatureEncoder.CardCount)
            {
                throw new ArgumentException($"Expected {NeuralFeatureEncoder.CardCount} targets.", nameof(target));
            }

            var bytes = this.recordBuffer.Value;
            Buffer.BlockCopy(features, 0, bytes, 0, FeatureBytes);
            Buffer.BlockCopy(target, 0, bytes, FeatureBytes, TargetBytes);
            this.threadBuffers.Value.Write(bytes, 0, RecordBytes);
            Interlocked.Increment(ref this.sampleCount);
        }

        public void WriteTo(string path)
        {
            using var output = File.Create(path);
            using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(NeuralFeatureEncoder.FeatureCount);
            writer.Write(NeuralFeatureEncoder.CardCount);
            writer.Write((int)this.SampleCount);

            foreach (var ms in this.threadBuffers.Values)
            {
                ms.Position = 0;
                ms.CopyTo(output);
            }
        }
    }
}
