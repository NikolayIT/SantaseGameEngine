namespace Santase.AI.ClaudePlayer.Neural
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Resolves which <see cref="NeuralNetwork"/> the player should run with.
    /// Preference order:
    ///   1. Trained weights shipped as an embedded resource named
    ///      <see cref="EmbeddedResourceName"/> in this assembly.
    ///   2. Deterministic Xavier-init fallback (Phase 1 placeholder).
    /// The network is immutable and thread-safe, so it is resolved once per process and shared:
    /// the simulator builds a fresh <see cref="ClaudePlayerNeural"/> per game, and re-reading the
    /// ~144 KB resource each time used to dominate the player's construction cost.
    /// </summary>
    public static class NeuralWeightsLoader
    {
        public const string EmbeddedResourceName = "Santase.AI.ClaudePlayer.Neural.weights.bin";

        public const int DefaultXavierSeed = 4242;

        private static readonly Lazy<NeuralNetwork> SharedNetwork = new Lazy<NeuralNetwork>(LoadNetwork);

        public static NeuralNetwork Load()
        {
            return SharedNetwork.Value;
        }

        private static NeuralNetwork LoadNetwork()
        {
            var assembly = typeof(NeuralWeightsLoader).Assembly;
            using (var stream = OpenEmbeddedWeights(assembly))
            {
                if (stream != null)
                {
                    return NeuralNetwork.LoadFromStream(stream);
                }
            }

            return XavierInitializer.CreateNetwork(DefaultXavierSeed);
        }

        private static Stream OpenEmbeddedWeights(Assembly assembly)
        {
            return assembly.GetManifestResourceStream(EmbeddedResourceName);
        }
    }
}
