namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.IO;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Pure-C# supervised trainer for the policy MLP that <see cref="NeuralNetwork"/> consumes
    /// at runtime. Architecture is hard-coded to match: 128 -> 128 -> 128 -> 24, ReLU on hidden
    /// layers, linear output with fused softmax + cross-entropy. Single-threaded backprop; Adam.
    /// Used to clone the heuristic into the network — this is the warm-start the PPO trainer
    /// fine-tunes from.
    /// </summary>
    public sealed class MLPTrainer
    {
        private const int InSize = NeuralNetwork.InputSize;
        private const int H1 = NeuralNetwork.Hidden1Size;
        private const int H2 = NeuralNetwork.Hidden2Size;
        private const int OutSize = NeuralNetwork.OutputSize;

        private const int W1Size = InSize * H1;
        private const int W2Size = H1 * H2;
        private const int W3Size = H2 * OutSize;

        private readonly float[] w1 = new float[W1Size];
        private readonly float[] b1 = new float[H1];
        private readonly float[] w2 = new float[W2Size];
        private readonly float[] b2 = new float[H2];
        private readonly float[] w3 = new float[W3Size];
        private readonly float[] b3 = new float[OutSize];

        // Adam state.
        private readonly float[] mw1 = new float[W1Size];
        private readonly float[] vw1 = new float[W1Size];
        private readonly float[] mb1 = new float[H1];
        private readonly float[] vb1 = new float[H1];
        private readonly float[] mw2 = new float[W2Size];
        private readonly float[] vw2 = new float[W2Size];
        private readonly float[] mb2 = new float[H2];
        private readonly float[] vb2 = new float[H2];
        private readonly float[] mw3 = new float[W3Size];
        private readonly float[] vw3 = new float[W3Size];
        private readonly float[] mb3 = new float[OutSize];
        private readonly float[] vb3 = new float[OutSize];

        // Per-batch gradient accumulators.
        private readonly float[] gw1 = new float[W1Size];
        private readonly float[] gb1 = new float[H1];
        private readonly float[] gw2 = new float[W2Size];
        private readonly float[] gb2 = new float[H2];
        private readonly float[] gw3 = new float[W3Size];
        private readonly float[] gb3 = new float[OutSize];

        // Per-sample scratch.
        private readonly float[] z1 = new float[H1];
        private readonly float[] a1 = new float[H1];
        private readonly float[] z2 = new float[H2];
        private readonly float[] a2 = new float[H2];
        private readonly float[] z3 = new float[OutSize];
        private readonly float[] probs = new float[OutSize];
        private readonly float[] dz1 = new float[H1];
        private readonly float[] dz2 = new float[H2];
        private readonly float[] dz3 = new float[OutSize];

        private int adamStep;

        public MLPTrainer(int seed)
        {
            var rng = new Random(seed);
            InitXavier(rng, this.w1, InSize, H1);
            InitXavier(rng, this.w2, H1, H2);
            InitXavier(rng, this.w3, H2, OutSize);
        }

        /// <summary>
        /// Forward + backward over a single mini-batch, then one Adam step.
        /// Returns (mean cross-entropy loss, mean top-1 accuracy) for the batch.
        /// </summary>
        public (float Loss, float Accuracy) TrainBatch(
            float[] features,
            byte[] labels,
            int[] indices,
            int batchStart,
            int batchSize,
            float learningRate,
            float beta1,
            float beta2,
            float eps)
        {
            Array.Clear(this.gw1, 0, this.gw1.Length);
            Array.Clear(this.gb1, 0, this.gb1.Length);
            Array.Clear(this.gw2, 0, this.gw2.Length);
            Array.Clear(this.gb2, 0, this.gb2.Length);
            Array.Clear(this.gw3, 0, this.gw3.Length);
            Array.Clear(this.gb3, 0, this.gb3.Length);

            float totalLoss = 0f;
            var correct = 0;

            for (var s = 0; s < batchSize; s++)
            {
                var sampleIdx = indices[batchStart + s];
                var xOffset = sampleIdx * InSize;
                var y = labels[sampleIdx];

                this.Forward(features, xOffset);

                var pY = this.probs[y];
                if (pY < 1e-12f)
                {
                    pY = 1e-12f;
                }

                totalLoss += -MathF.Log(pY);

                var argmax = 0;
                var pMax = this.probs[0];
                for (var i = 1; i < OutSize; i++)
                {
                    if (this.probs[i] > pMax)
                    {
                        pMax = this.probs[i];
                        argmax = i;
                    }
                }

                if (argmax == y)
                {
                    correct++;
                }

                this.Backward(features, xOffset, y);
            }

            var scale = 1f / batchSize;
            ScaleInPlace(this.gw1, scale);
            ScaleInPlace(this.gb1, scale);
            ScaleInPlace(this.gw2, scale);
            ScaleInPlace(this.gb2, scale);
            ScaleInPlace(this.gw3, scale);
            ScaleInPlace(this.gb3, scale);

            this.adamStep++;
            AdamUpdate(this.w1, this.gw1, this.mw1, this.vw1, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b1, this.gb1, this.mb1, this.vb1, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.w2, this.gw2, this.mw2, this.vw2, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b2, this.gb2, this.mb2, this.vb2, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.w3, this.gw3, this.mw3, this.vw3, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b3, this.gb3, this.mb3, this.vb3, learningRate, beta1, beta2, eps, this.adamStep);

            return (totalLoss / batchSize, (float)correct / batchSize);
        }

        /// <summary>
        /// Soft-target variant of <see cref="TrainBatch"/> for policy distillation: each label is a
        /// full probability distribution over the 24 cards (the search's root visit fractions) rather
        /// than a single class index. Loss is cross-entropy H(target, pred) = -sum_i t_i log p_i; the
        /// softmax output gradient reduces to the same clean (pred - target). Returns (mean loss, mean
        /// top-1 agreement where argmax(pred) == argmax(target)).
        /// </summary>
        public (float Loss, float Accuracy) TrainBatchSoft(
            float[] features,
            float[] targets,
            int[] indices,
            int batchStart,
            int batchSize,
            float learningRate,
            float beta1,
            float beta2,
            float eps)
        {
            Array.Clear(this.gw1, 0, this.gw1.Length);
            Array.Clear(this.gb1, 0, this.gb1.Length);
            Array.Clear(this.gw2, 0, this.gw2.Length);
            Array.Clear(this.gb2, 0, this.gb2.Length);
            Array.Clear(this.gw3, 0, this.gw3.Length);
            Array.Clear(this.gb3, 0, this.gb3.Length);

            float totalLoss = 0f;
            var correct = 0;

            for (var s = 0; s < batchSize; s++)
            {
                var sampleIdx = indices[batchStart + s];
                var xOffset = sampleIdx * InSize;
                var tOffset = sampleIdx * OutSize;

                this.Forward(features, xOffset);

                var tArgmax = 0;
                var tMax = -1f;
                var pArgmax = 0;
                var pMax = -1f;
                for (var i = 0; i < OutSize; i++)
                {
                    var ti = targets[tOffset + i];
                    if (ti > 0f)
                    {
                        var pi = this.probs[i] < 1e-12f ? 1e-12f : this.probs[i];
                        totalLoss += -ti * MathF.Log(pi);
                    }

                    if (ti > tMax)
                    {
                        tMax = ti;
                        tArgmax = i;
                    }

                    if (this.probs[i] > pMax)
                    {
                        pMax = this.probs[i];
                        pArgmax = i;
                    }
                }

                if (pArgmax == tArgmax)
                {
                    correct++;
                }

                this.BackwardSoft(features, xOffset, targets, tOffset);
            }

            var scale = 1f / batchSize;
            ScaleInPlace(this.gw1, scale);
            ScaleInPlace(this.gb1, scale);
            ScaleInPlace(this.gw2, scale);
            ScaleInPlace(this.gb2, scale);
            ScaleInPlace(this.gw3, scale);
            ScaleInPlace(this.gb3, scale);

            this.adamStep++;
            AdamUpdate(this.w1, this.gw1, this.mw1, this.vw1, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b1, this.gb1, this.mb1, this.vb1, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.w2, this.gw2, this.mw2, this.vw2, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b2, this.gb2, this.mb2, this.vb2, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.w3, this.gw3, this.mw3, this.vw3, learningRate, beta1, beta2, eps, this.adamStep);
            AdamUpdate(this.b3, this.gb3, this.mb3, this.vb3, learningRate, beta1, beta2, eps, this.adamStep);

            return (totalLoss / batchSize, (float)correct / batchSize);
        }

        /// <summary>
        /// Saves weights in the row-major float32 layout expected by NeuralNetwork.LoadFromStream:
        /// W1, b1, W2, b2, W3, b3.
        /// </summary>
        public void SaveWeights(string path)
        {
            using var stream = File.Create(path);
            this.SaveWeights(stream);
        }

        public void SaveWeights(Stream stream)
        {
            WriteFloats(stream, this.w1);
            WriteFloats(stream, this.b1);
            WriteFloats(stream, this.w2);
            WriteFloats(stream, this.b2);
            WriteFloats(stream, this.w3);
            WriteFloats(stream, this.b3);
        }

        private static void InitXavier(Random rng, float[] arr, int inDim, int outDim)
        {
            var limit = (float)Math.Sqrt(6.0 / (inDim + outDim));
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (float)(((rng.NextDouble() * 2.0) - 1.0) * limit);
            }
        }

        private static void ScaleInPlace(float[] arr, float scale)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] *= scale;
            }
        }

        private static void AdamUpdate(
            float[] param, float[] grad, float[] m, float[] v,
            float lr, float beta1, float beta2, float eps, int t)
        {
            var bc1 = 1f - MathF.Pow(beta1, t);
            var bc2 = 1f - MathF.Pow(beta2, t);
            for (var i = 0; i < param.Length; i++)
            {
                var g = grad[i];
                m[i] = (beta1 * m[i]) + ((1f - beta1) * g);
                v[i] = (beta2 * v[i]) + ((1f - beta2) * g * g);
                var mHat = m[i] / bc1;
                var vHat = v[i] / bc2;
                param[i] -= lr * mHat / (MathF.Sqrt(vHat) + eps);
            }
        }

        private static void WriteFloats(Stream s, float[] arr)
        {
            var bytes = new byte[arr.Length * sizeof(float)];
            Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
            s.Write(bytes, 0, bytes.Length);
        }

        private void Forward(float[] features, int xOffset)
        {
            for (var i = 0; i < H1; i++)
            {
                var sum = this.b1[i];
                var row = i * InSize;
                for (var j = 0; j < InSize; j++)
                {
                    sum += this.w1[row + j] * features[xOffset + j];
                }

                this.z1[i] = sum;
                this.a1[i] = sum > 0f ? sum : 0f;
            }

            for (var i = 0; i < H2; i++)
            {
                var sum = this.b2[i];
                var row = i * H1;
                for (var j = 0; j < H1; j++)
                {
                    sum += this.w2[row + j] * this.a1[j];
                }

                this.z2[i] = sum;
                this.a2[i] = sum > 0f ? sum : 0f;
            }

            for (var i = 0; i < OutSize; i++)
            {
                var sum = this.b3[i];
                var row = i * H2;
                for (var j = 0; j < H2; j++)
                {
                    sum += this.w3[row + j] * this.a2[j];
                }

                this.z3[i] = sum;
            }

            var maxZ = this.z3[0];
            for (var i = 1; i < OutSize; i++)
            {
                if (this.z3[i] > maxZ)
                {
                    maxZ = this.z3[i];
                }
            }

            var sumExp = 0f;
            for (var i = 0; i < OutSize; i++)
            {
                this.probs[i] = MathF.Exp(this.z3[i] - maxZ);
                sumExp += this.probs[i];
            }

            var invSum = 1f / sumExp;
            for (var i = 0; i < OutSize; i++)
            {
                this.probs[i] *= invSum;
            }
        }

        private void Backward(float[] features, int xOffset, int y)
        {
            for (var i = 0; i < OutSize; i++)
            {
                this.dz3[i] = this.probs[i];
            }

            this.dz3[y] -= 1f;
            this.BackwardFromDz3(features, xOffset);
        }

        // Soft-target output gradient: dz3 = pred - target, where target is a full probability
        // distribution over the 24 cards (softmax cross-entropy reduces to the same clean form).
        private void BackwardSoft(float[] features, int xOffset, float[] targets, int tOffset)
        {
            for (var i = 0; i < OutSize; i++)
            {
                this.dz3[i] = this.probs[i] - targets[tOffset + i];
            }

            this.BackwardFromDz3(features, xOffset);
        }

        // Backprop from an already-populated this.dz3 through W3/W2/W1 into the gradient accumulators.
        private void BackwardFromDz3(float[] features, int xOffset)
        {
            for (var i = 0; i < OutSize; i++)
            {
                var row = i * H2;
                var d = this.dz3[i];
                for (var j = 0; j < H2; j++)
                {
                    this.gw3[row + j] += d * this.a2[j];
                }

                this.gb3[i] += d;
            }

            for (var j = 0; j < H2; j++)
            {
                var da = 0f;
                for (var i = 0; i < OutSize; i++)
                {
                    da += this.w3[(i * H2) + j] * this.dz3[i];
                }

                this.dz2[j] = this.z2[j] > 0f ? da : 0f;
            }

            for (var i = 0; i < H2; i++)
            {
                var row = i * H1;
                var d = this.dz2[i];
                for (var j = 0; j < H1; j++)
                {
                    this.gw2[row + j] += d * this.a1[j];
                }

                this.gb2[i] += d;
            }

            for (var j = 0; j < H1; j++)
            {
                var da = 0f;
                for (var i = 0; i < H2; i++)
                {
                    da += this.w2[(i * H1) + j] * this.dz2[i];
                }

                this.dz1[j] = this.z1[j] > 0f ? da : 0f;
            }

            for (var i = 0; i < H1; i++)
            {
                var row = i * InSize;
                var d = this.dz1[i];
                for (var j = 0; j < InSize; j++)
                {
                    this.gw1[row + j] += d * features[xOffset + j];
                }

                this.gb1[i] += d;
            }
        }
    }
}
