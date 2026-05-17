namespace Santase.AI.ClaudePlayer.Neural
{
    using System;
    using System.IO;

    /// <summary>
    /// Pure-managed multilayer perceptron used as the policy network for
    /// <see cref="ClaudePlayerNeural"/>. No external dependencies and no P/Invoke,
    /// so it runs anywhere net10.0 runs (Windows, Linux, Android via .NET MAUI).
    /// Architecture: <see cref="InputSize"/> -> <see cref="Hidden1Size"/> (ReLU)
    /// -> <see cref="Hidden2Size"/> (ReLU) -> <see cref="OutputSize"/> (linear).
    /// Instances are NOT thread-safe (shared scratch buffers); give each player its own.
    /// </summary>
    public sealed class NeuralNetwork
    {
        public const int InputSize = 128;
        public const int Hidden1Size = 128;
        public const int Hidden2Size = 128;
        public const int OutputSize = 24;

        public const int TotalWeightCount =
            (InputSize * Hidden1Size) + Hidden1Size
            + (Hidden1Size * Hidden2Size) + Hidden2Size
            + (Hidden2Size * OutputSize) + OutputSize;

        public const int ExpectedWeightFileBytes = TotalWeightCount * sizeof(float);

        // Layer weights, row-major: W[outIdx, inIdx] = w[(outIdx * inSize) + inIdx].
        private readonly float[] w1;
        private readonly float[] b1;
        private readonly float[] w2;
        private readonly float[] b2;
        private readonly float[] w3;
        private readonly float[] b3;

        private readonly float[] hidden1;
        private readonly float[] hidden2;

        private NeuralNetwork()
        {
            this.w1 = new float[InputSize * Hidden1Size];
            this.b1 = new float[Hidden1Size];
            this.w2 = new float[Hidden1Size * Hidden2Size];
            this.b2 = new float[Hidden2Size];
            this.w3 = new float[Hidden2Size * OutputSize];
            this.b3 = new float[OutputSize];

            this.hidden1 = new float[Hidden1Size];
            this.hidden2 = new float[Hidden2Size];
        }

        public static NeuralNetwork LoadFromStream(Stream stream)
        {
            var nn = new NeuralNetwork();
            ReadFloats(stream, nn.w1);
            ReadFloats(stream, nn.b1);
            ReadFloats(stream, nn.w2);
            ReadFloats(stream, nn.b2);
            ReadFloats(stream, nn.w3);
            ReadFloats(stream, nn.b3);
            return nn;
        }

        public static NeuralNetwork FromWeights(
            float[] weights1,
            float[] bias1,
            float[] weights2,
            float[] bias2,
            float[] weights3,
            float[] bias3)
        {
            var nn = new NeuralNetwork();
            CopyOrThrow(weights1, nn.w1, nameof(weights1));
            CopyOrThrow(bias1, nn.b1, nameof(bias1));
            CopyOrThrow(weights2, nn.w2, nameof(weights2));
            CopyOrThrow(bias2, nn.b2, nameof(bias2));
            CopyOrThrow(weights3, nn.w3, nameof(weights3));
            CopyOrThrow(bias3, nn.b3, nameof(bias3));
            return nn;
        }

        public void Forward(float[] input, float[] output)
        {
            if (input == null || input.Length != InputSize)
            {
                throw new ArgumentException($"Expected input of length {InputSize}.", nameof(input));
            }

            if (output == null || output.Length != OutputSize)
            {
                throw new ArgumentException($"Expected output of length {OutputSize}.", nameof(output));
            }

            for (var i = 0; i < Hidden1Size; i++)
            {
                var sum = this.b1[i];
                var row = i * InputSize;
                for (var j = 0; j < InputSize; j++)
                {
                    sum += this.w1[row + j] * input[j];
                }

                this.hidden1[i] = sum > 0f ? sum : 0f;
            }

            for (var i = 0; i < Hidden2Size; i++)
            {
                var sum = this.b2[i];
                var row = i * Hidden1Size;
                for (var j = 0; j < Hidden1Size; j++)
                {
                    sum += this.w2[row + j] * this.hidden1[j];
                }

                this.hidden2[i] = sum > 0f ? sum : 0f;
            }

            for (var i = 0; i < OutputSize; i++)
            {
                var sum = this.b3[i];
                var row = i * Hidden2Size;
                for (var j = 0; j < Hidden2Size; j++)
                {
                    sum += this.w3[row + j] * this.hidden2[j];
                }

                output[i] = sum;
            }
        }

        // Reads dest.Length little-endian float32 values into dest. All currently
        // supported runtimes are little-endian; we don't byte-swap.
        private static void ReadFloats(Stream stream, float[] dest)
        {
            var bytes = new byte[dest.Length * sizeof(float)];
            var read = 0;
            while (read < bytes.Length)
            {
                var n = stream.Read(bytes, read, bytes.Length - read);
                if (n == 0)
                {
                    throw new EndOfStreamException("Truncated neural network weights stream.");
                }

                read += n;
            }

            Buffer.BlockCopy(bytes, 0, dest, 0, bytes.Length);
        }

        private static void CopyOrThrow(float[] src, float[] dest, string name)
        {
            if (src == null || src.Length != dest.Length)
            {
                throw new ArgumentException($"Expected {name} of length {dest.Length}.", name);
            }

            Buffer.BlockCopy(src, 0, dest, 0, dest.Length * sizeof(float));
        }
    }
}
