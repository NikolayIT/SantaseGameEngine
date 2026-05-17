namespace Santase.AI.ClaudePlayer.Neural
{
    using System;

    /// <summary>
    /// Deterministic Xavier (Glorot) initialization used as the Phase 1 placeholder for
    /// <see cref="NeuralNetwork"/>. Produces a "random but legal" policy — verifies the
    /// inference plumbing end-to-end before a trained weights blob exists. When Phase 2
    /// produces trained weights, we ship them as an <c>EmbeddedResource</c> on the csproj
    /// and <see cref="NeuralWeightsLoader"/> will prefer the resource over this fallback.
    /// </summary>
    public static class XavierInitializer
    {
        public static NeuralNetwork CreateNetwork(int seed)
        {
            var rng = new Random(seed);
            var w1 = SampleXavier(rng, NeuralNetwork.Hidden1Size, NeuralNetwork.InputSize);
            var b1 = new float[NeuralNetwork.Hidden1Size];
            var w2 = SampleXavier(rng, NeuralNetwork.Hidden2Size, NeuralNetwork.Hidden1Size);
            var b2 = new float[NeuralNetwork.Hidden2Size];
            var w3 = SampleXavier(rng, NeuralNetwork.OutputSize, NeuralNetwork.Hidden2Size);
            var b3 = new float[NeuralNetwork.OutputSize];
            return NeuralNetwork.FromWeights(w1, b1, w2, b2, w3, b3);
        }

        private static float[] SampleXavier(Random rng, int outDim, int inDim)
        {
            var arr = new float[outDim * inDim];
            var limit = (float)Math.Sqrt(6.0 / (inDim + outDim));
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (float)(((rng.NextDouble() * 2.0) - 1.0) * limit);
            }

            return arr;
        }
    }
}
