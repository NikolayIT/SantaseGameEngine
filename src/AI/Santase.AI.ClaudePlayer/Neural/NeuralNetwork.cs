namespace Santase.AI.ClaudePlayer.Neural
{
    using System;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Pure-managed multilayer perceptron used as the policy network for
    /// <see cref="ClaudePlayerNeural"/>. No external dependencies and no P/Invoke,
    /// so it runs anywhere net10.0 runs (Windows, Linux, Android via .NET MAUI).
    /// Architecture: <see cref="InputSize"/> -> <see cref="Hidden1Size"/> (ReLU)
    /// -> <see cref="Hidden2Size"/> (ReLU) -> <see cref="OutputSize"/> (linear).
    /// Instances are immutable and thread-safe (<see cref="Forward"/> keeps its scratch on the
    /// stack), so one loaded network can be shared by any number of players and threads.
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

        // Layer weights, stored TRANSPOSED (column-major): W[outIdx, inIdx] = wT[(inIdx * outSize) + outIdx].
        // The weight file and FromWeights stay row-major; the transpose happens once at load. With
        // this layout the outputs sharing one input are contiguous, so Forward vectorizes across
        // output neurons: every SIMD lane owns one output and accumulates bias + sum(w * x) in
        // ascending input order with a separate multiply and add per term — exactly the scalar
        // row-major dot product's operation order, so the logits are bit-identical to it.
        private readonly float[] w1T;
        private readonly float[] b1;
        private readonly float[] w2T;
        private readonly float[] b2;
        private readonly float[] w3T;
        private readonly float[] b3;

        private NeuralNetwork(float[] w1, float[] b1, float[] w2, float[] b2, float[] w3, float[] b3)
        {
            this.w1T = Transpose(w1, Hidden1Size, InputSize);
            this.b1 = b1;
            this.w2T = Transpose(w2, Hidden2Size, Hidden1Size);
            this.b2 = b2;
            this.w3T = Transpose(w3, OutputSize, Hidden2Size);
            this.b3 = b3;
        }

        public static NeuralNetwork LoadFromStream(Stream stream)
        {
            var w1 = ReadFloats(stream, InputSize * Hidden1Size);
            var b1 = ReadFloats(stream, Hidden1Size);
            var w2 = ReadFloats(stream, Hidden1Size * Hidden2Size);
            var b2 = ReadFloats(stream, Hidden2Size);
            var w3 = ReadFloats(stream, Hidden2Size * OutputSize);
            var b3 = ReadFloats(stream, OutputSize);
            return new NeuralNetwork(w1, b1, w2, b2, w3, b3);
        }

        public static NeuralNetwork FromWeights(
            float[] weights1,
            float[] bias1,
            float[] weights2,
            float[] bias2,
            float[] weights3,
            float[] bias3)
        {
            return new NeuralNetwork(
                CopyOrThrow(weights1, InputSize * Hidden1Size, nameof(weights1)),
                CopyOrThrow(bias1, Hidden1Size, nameof(bias1)),
                CopyOrThrow(weights2, Hidden1Size * Hidden2Size, nameof(weights2)),
                CopyOrThrow(bias2, Hidden2Size, nameof(bias2)),
                CopyOrThrow(weights3, Hidden2Size * OutputSize, nameof(weights3)),
                CopyOrThrow(bias3, OutputSize, nameof(bias3)));
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

            Span<float> hidden1 = stackalloc float[Hidden1Size];
            Span<float> hidden2 = stackalloc float[Hidden2Size];
            Span<int> active = stackalloc int[Math.Max(InputSize, Math.Max(Hidden1Size, Hidden2Size))];

            Layer(input, this.w1T, this.b1, hidden1, active, relu: true);
            Layer(hidden1, this.w2T, this.b2, hidden2, active, relu: true);
            Layer(hidden2, this.w3T, this.b3, output, active, relu: false);
        }

        // y = act(b + W x) with W stored transposed (see the field comment). Zero inputs are skipped:
        // their terms are +/-0, which can only change the sign of an exactly-zero sum — erased by the
        // ReLU, and invisible to every consumer of the linear output layer (argmax / softmax treat
        // -0 and +0 alike). The input is sparse in practice (one-hot card planes, ~half the hidden
        // units ReLU-clipped), so this also skips most of the work.
        private static void Layer(ReadOnlySpan<float> x, float[] weightsT, float[] bias, Span<float> y, Span<int> active, bool relu)
        {
            var outSize = bias.Length;
            var activeCount = 0;
            for (var j = 0; j < x.Length; j++)
            {
                if (x[j] != 0f)
                {
                    active[activeCount++] = j;
                }
            }

            ref var w = ref MemoryMarshal.GetArrayDataReference(weightsT);
            var i = 0;
            if (Vector.IsHardwareAccelerated)
            {
                var width = Vector<float>.Count;

                // Four accumulators in flight per pass over the active inputs.
                for (; i + (4 * width) <= outSize; i += 4 * width)
                {
                    var acc0 = new Vector<float>(bias, i);
                    var acc1 = new Vector<float>(bias, i + width);
                    var acc2 = new Vector<float>(bias, i + (2 * width));
                    var acc3 = new Vector<float>(bias, i + (3 * width));
                    for (var k = 0; k < activeCount; k++)
                    {
                        var j = active[k];
                        var xj = new Vector<float>(x[j]);
                        var offset = (nuint)((j * outSize) + i);
                        acc0 += Vector.LoadUnsafe(ref w, offset) * xj;
                        acc1 += Vector.LoadUnsafe(ref w, offset + (nuint)width) * xj;
                        acc2 += Vector.LoadUnsafe(ref w, offset + (nuint)(2 * width)) * xj;
                        acc3 += Vector.LoadUnsafe(ref w, offset + (nuint)(3 * width)) * xj;
                    }

                    acc0.CopyTo(y.Slice(i));
                    acc1.CopyTo(y.Slice(i + width));
                    acc2.CopyTo(y.Slice(i + (2 * width)));
                    acc3.CopyTo(y.Slice(i + (3 * width)));
                }

                for (; i + width <= outSize; i += width)
                {
                    var acc = new Vector<float>(bias, i);
                    for (var k = 0; k < activeCount; k++)
                    {
                        var j = active[k];
                        acc += Vector.LoadUnsafe(ref w, (nuint)((j * outSize) + i)) * new Vector<float>(x[j]);
                    }

                    acc.CopyTo(y.Slice(i));
                }
            }

            for (; i < outSize; i++)
            {
                var sum = bias[i];
                for (var k = 0; k < activeCount; k++)
                {
                    var j = active[k];
                    sum += weightsT[(j * outSize) + i] * x[j];
                }

                y[i] = sum;
            }

            if (relu)
            {
                for (var o = 0; o < outSize; o++)
                {
                    y[o] = y[o] > 0f ? y[o] : 0f;
                }
            }
        }

        private static float[] Transpose(float[] rowMajor, int rows, int columns)
        {
            var transposed = new float[rowMajor.Length];
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    transposed[(c * rows) + r] = rowMajor[(r * columns) + c];
                }
            }

            return transposed;
        }

        // Reads count little-endian float32 values. All currently supported runtimes are
        // little-endian; we don't byte-swap.
        private static float[] ReadFloats(Stream stream, int count)
        {
            var dest = new float[count];
            var bytes = MemoryMarshal.AsBytes(dest.AsSpan());
            var read = 0;
            while (read < bytes.Length)
            {
                var n = stream.Read(bytes.Slice(read));
                if (n == 0)
                {
                    throw new EndOfStreamException("Truncated neural network weights stream.");
                }

                read += n;
            }

            return dest;
        }

        private static float[] CopyOrThrow(float[] src, int expectedLength, string name)
        {
            if (src == null || src.Length != expectedLength)
            {
                throw new ArgumentException($"Expected {name} of length {expectedLength}.", name);
            }

            return (float[])src.Clone();
        }
    }
}
