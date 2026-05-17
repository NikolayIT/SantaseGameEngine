namespace Santase.Tests.GameSimulations.Training
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Thread-safe accumulator for training samples produced by <c>ClaudePlayer.TrainingRecorder</c>.
    /// Each thread writes into its own private <see cref="MemoryStream"/>; <see cref="WriteTo"/>
    /// concatenates them in deterministic-by-pointer-order at the end and prepends a small header.
    /// File format (little-endian):
    ///   bytes 0..3   magic "STSE"
    ///   bytes 4..7   version (currently 1)
    ///   bytes 8..11  feature dim (currently 128)
    ///   bytes 12..15 sample count
    ///   then sample_count records of (128 float32 features) + (1 byte card index in [0, 24)).
    /// </summary>
    public sealed class TrainingDataCollector
    {
        public const int Magic = 0x45535453; // "STSE" little-endian
        public const int Version = 1;
        public const int LabelBytes = 1;

        private static readonly int FeatureBytes = NeuralFeatureEncoder.FeatureCount * sizeof(float);
        private static readonly int RecordBytes = FeatureBytes + LabelBytes;

        private readonly ThreadLocal<byte[]> recordBuffer =
            new(() => new byte[RecordBytes]);

        private readonly ThreadLocal<MemoryStream> threadBuffers =
            new(() => new MemoryStream(capacity: 1 << 16), trackAllValues: true);

        private long sampleCount;

        public long SampleCount => Interlocked.Read(ref this.sampleCount);

        public void Record(float[] features, int label)
        {
            if (label < 0 || label >= NeuralFeatureEncoder.CardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(label));
            }

            var bytes = this.recordBuffer.Value;
            Buffer.BlockCopy(features, 0, bytes, 0, FeatureBytes);
            bytes[FeatureBytes] = (byte)label;
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
            writer.Write((int)this.SampleCount);

            foreach (var ms in this.threadBuffers.Values)
            {
                ms.Position = 0;
                ms.CopyTo(output);
            }
        }
    }
}
