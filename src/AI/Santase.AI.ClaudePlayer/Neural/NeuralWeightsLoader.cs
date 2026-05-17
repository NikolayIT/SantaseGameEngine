namespace Santase.AI.ClaudePlayer.Neural
{
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Resolves which <see cref="NeuralNetwork"/> the player should run with.
    /// Preference order:
    ///   1. Trained weights shipped as an embedded resource named
    ///      <see cref="EmbeddedResourceName"/> in this assembly.
    ///   2. Deterministic Xavier-init fallback (Phase 1 placeholder).
    /// </summary>
    public static class NeuralWeightsLoader
    {
        public const string EmbeddedResourceName = "Santase.AI.ClaudePlayer.Neural.weights.bin";

        public const int DefaultXavierSeed = 4242;

        public static NeuralNetwork Load()
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
