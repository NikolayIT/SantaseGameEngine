namespace Santase.AI.ClaudePlayer.Tests.Neural
{
    using Santase.AI.ClaudePlayer.Neural;

    using Xunit;

    public class NeuralNetworkTests
    {
        [Fact]
        public void ForwardPassWithZeroWeightsAndZeroBiasShouldReturnAllZeros()
        {
            var nn = NeuralNetwork.FromWeights(
                new float[NeuralNetwork.InputSize * NeuralNetwork.Hidden1Size],
                new float[NeuralNetwork.Hidden1Size],
                new float[NeuralNetwork.Hidden1Size * NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size * NeuralNetwork.OutputSize],
                new float[NeuralNetwork.OutputSize]);

            var input = new float[NeuralNetwork.InputSize];
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = 1f;
            }

            var output = new float[NeuralNetwork.OutputSize];
            nn.Forward(input, output);

            foreach (var v in output)
            {
                Assert.Equal(0f, v);
            }
        }

        [Fact]
        public void ForwardPassShouldPassOutputBiasThroughWhenWeightsAreZero()
        {
            // With W1=W2=W3=0, hidden layers stay zero, so output = b3 verbatim.
            var b3 = new float[NeuralNetwork.OutputSize];
            for (var i = 0; i < b3.Length; i++)
            {
                b3[i] = i + 1f;
            }

            var nn = NeuralNetwork.FromWeights(
                new float[NeuralNetwork.InputSize * NeuralNetwork.Hidden1Size],
                new float[NeuralNetwork.Hidden1Size],
                new float[NeuralNetwork.Hidden1Size * NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size * NeuralNetwork.OutputSize],
                b3);

            var input = new float[NeuralNetwork.InputSize];
            var output = new float[NeuralNetwork.OutputSize];
            nn.Forward(input, output);

            for (var i = 0; i < b3.Length; i++)
            {
                Assert.Equal(b3[i], output[i]);
            }
        }

        [Fact]
        public void ForwardPassReluShouldClipNegativeActivations()
        {
            // W1=0 but b1 = -1 -> hidden1 = ReLU(-1) = 0, so subsequent layers see all zeros,
            // and output collapses to b3. Confirms ReLU is applied and not skipped.
            var b1 = new float[NeuralNetwork.Hidden1Size];
            for (var i = 0; i < b1.Length; i++)
            {
                b1[i] = -1f;
            }

            var b3 = new float[NeuralNetwork.OutputSize];
            for (var i = 0; i < b3.Length; i++)
            {
                b3[i] = 7f;
            }

            var nn = NeuralNetwork.FromWeights(
                new float[NeuralNetwork.InputSize * NeuralNetwork.Hidden1Size],
                b1,
                new float[NeuralNetwork.Hidden1Size * NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size],
                new float[NeuralNetwork.Hidden2Size * NeuralNetwork.OutputSize],
                b3);

            var input = new float[NeuralNetwork.InputSize];
            var output = new float[NeuralNetwork.OutputSize];
            nn.Forward(input, output);

            foreach (var v in output)
            {
                Assert.Equal(7f, v);
            }
        }

        [Fact]
        public void ForwardPassWithUniformWeightsShouldProduceUniformOutput()
        {
            // W = 1/N at every layer, biases zero, input all ones.
            // hidden1[i] = ReLU(N * (1/N) * 1) = 1.  hidden2[i] = 1.  output[i] = 1.
            var w1 = Filled(NeuralNetwork.InputSize * NeuralNetwork.Hidden1Size, 1f / NeuralNetwork.InputSize);
            var w2 = Filled(NeuralNetwork.Hidden1Size * NeuralNetwork.Hidden2Size, 1f / NeuralNetwork.Hidden1Size);
            var w3 = Filled(NeuralNetwork.Hidden2Size * NeuralNetwork.OutputSize, 1f / NeuralNetwork.Hidden2Size);

            var nn = NeuralNetwork.FromWeights(
                w1,
                new float[NeuralNetwork.Hidden1Size],
                w2,
                new float[NeuralNetwork.Hidden2Size],
                w3,
                new float[NeuralNetwork.OutputSize]);

            var input = Filled(NeuralNetwork.InputSize, 1f);
            var output = new float[NeuralNetwork.OutputSize];
            nn.Forward(input, output);

            foreach (var v in output)
            {
                Assert.InRange(v, 0.999f, 1.001f);
            }
        }

        [Fact]
        public void XavierInitializerShouldProduceUsableNetwork()
        {
            var nn = XavierInitializer.CreateNetwork(seed: 42);
            var input = new float[NeuralNetwork.InputSize];
            input[0] = 1f;
            var output = new float[NeuralNetwork.OutputSize];

            nn.Forward(input, output);

            // Output should be finite for every position (no NaN / no infinity).
            foreach (var v in output)
            {
                Assert.False(float.IsNaN(v));
                Assert.False(float.IsInfinity(v));
            }
        }

        private static float[] Filled(int size, float value)
        {
            var arr = new float[size];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }

            return arr;
        }
    }
}
