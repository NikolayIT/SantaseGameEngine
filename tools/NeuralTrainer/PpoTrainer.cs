namespace Santase.Tools.NeuralTrainer
{
    using System;
    using System.IO;

    using Santase.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Pure-C# PPO trainer. Actor mirrors the shipped policy net
    /// (128 -> 128 -> 128 -> 24, ReLU hidden, linear logits) so SaveActor() produces a
    /// file NeuralNetwork.LoadFromStream consumes verbatim. Critic is a separate
    /// training-only net (128 -> 128 -> 128 -> 1) and is never shipped.
    ///
    /// Loss = clipped surrogate + value MSE - entropy bonus. Action distribution is the
    /// softmax of the actor logits restricted to the legal-card mask, so log-probs and
    /// entropy are computed over legal cards only — identical to the behavior policy
    /// (which is why PPO must run at sampling Temperature = 1).
    /// </summary>
    public sealed class PpoTrainer
    {
        public const int InSize = NeuralNetwork.InputSize;   // 128
        public const int H1 = NeuralNetwork.Hidden1Size;     // 128
        public const int H2 = NeuralNetwork.Hidden2Size;     // 128
        public const int ActOut = NeuralNetwork.OutputSize;  // 24

        private readonly Mlp actor;
        private readonly Mlp critic;

        public PpoTrainer(int seed)
        {
            this.actor = new Mlp(InSize, H1, H2, ActOut, seed);
            this.critic = new Mlp(InSize, H1, H2, 1, seed + 9973);
        }

        public struct Diag
        {
            public double PolicyLoss;
            public double ValueLoss;
            public double Entropy;
            public double ApproxKl;
            public double ClipFrac;
        }

        /// <summary>Warm-starts the actor from a shipped-layout weights file (supervised clone).</summary>
        public void LoadActor(string path)
        {
            using var s = File.OpenRead(path);
            this.actor.LoadShippedLayout(s);
            this.actor.ResetAdam();
        }

        public void SaveActor(string path)
        {
            using var s = File.Create(path);
            this.actor.SaveShippedLayout(s);
        }

        public void SaveActor(Stream s) => this.actor.SaveShippedLayout(s);

        /// <summary>Critic value estimates V(s) for every sample (used to build GAE pre-update).</summary>
        public void PredictValues(float[] features, int count, float[] valuesOut)
        {
            for (var i = 0; i < count; i++)
            {
                this.critic.Forward(features, i * InSize);
                valuesOut[i] = this.critic.Out[0];
            }
        }

        /// <summary>
        /// One PPO minibatch: forward+backward actor (clipped surrogate + entropy) and critic
        /// (MSE to returns), then an Adam step on each net. Advantages must already be
        /// normalized by the caller. Returns running diagnostics for the minibatch.
        /// </summary>
        public Diag TrainMinibatch(
            float[] features,
            byte[] actions,
            int[] legalMasks,
            float[] oldLogProbs,
            float[] advantages,
            float[] returns,
            int[] indices,
            int mbStart,
            int mbSize,
            float clipEps,
            float entCoef,
            float valueCoef,
            float actorLr,
            float criticLr,
            float gradClip)
        {
            this.actor.ZeroGrad();
            this.critic.ZeroGrad();

            var p = new float[ActOut];
            double sumPolicyLoss = 0, sumValueLoss = 0, sumEntropy = 0, sumKl = 0;
            var clipCount = 0;

            for (var s = 0; s < mbSize; s++)
            {
                var idx = indices[mbStart + s];
                var xOff = idx * InSize;
                int a = actions[idx];
                var mask = legalMasks[idx];
                var oldLp = oldLogProbs[idx];
                var adv = advantages[idx];
                var ret = returns[idx];

                // ---- Actor forward: masked stable softmax over legal cards ----
                this.actor.Forward(features, xOff);
                var logits = this.actor.Out;

                var maxLogit = float.NegativeInfinity;
                for (var k = 0; k < ActOut; k++)
                {
                    if (((mask >> k) & 1) == 1 && logits[k] > maxLogit)
                    {
                        maxLogit = logits[k];
                    }
                }

                float sumExp = 0f;
                for (var k = 0; k < ActOut; k++)
                {
                    if (((mask >> k) & 1) == 1)
                    {
                        var e = MathF.Exp(logits[k] - maxLogit);
                        p[k] = e;
                        sumExp += e;
                    }
                    else
                    {
                        p[k] = 0f;
                    }
                }

                var invSum = 1f / sumExp;
                var entropy = 0.0;
                for (var k = 0; k < ActOut; k++)
                {
                    if (p[k] > 0f)
                    {
                        p[k] *= invSum;
                        entropy -= p[k] * Math.Log(Math.Max(p[k], 1e-12f));
                    }
                }

                var newLp = MathF.Log(Math.Max(p[a], 1e-12f));
                var ratio = MathF.Exp(newLp - oldLp);

                // ---- Clipped surrogate gradient wrt log p[a] ----
                // Loss_policy = -min(ratio*A, clip(ratio,1±eps)*A). d(min)/dlogp piecewise.
                float dPolicy_dLogp;
                var clipped = false;
                if (adv >= 0f)
                {
                    if (ratio <= 1f + clipEps)
                    {
                        dPolicy_dLogp = -(adv * ratio);
                    }
                    else
                    {
                        dPolicy_dLogp = 0f;
                        clipped = true;
                    }
                }
                else
                {
                    if (ratio >= 1f - clipEps)
                    {
                        dPolicy_dLogp = -(adv * ratio);
                    }
                    else
                    {
                        dPolicy_dLogp = 0f;
                        clipped = true;
                    }
                }

                if (clipped)
                {
                    clipCount++;
                }

                // dLogits from policy term: dLoss/dz_j = dPolicy_dLogp * (1[j==a] - p_j)  (legal j).
                // dLogits from entropy bonus (loss has -entCoef*H): += entCoef * p_j*(ln p_j + H).
                var dOut = this.actor.GradOutScratch;
                for (var j = 0; j < ActOut; j++)
                {
                    if (((mask >> j) & 1) == 1)
                    {
                        var indicator = j == a ? 1f : 0f;
                        var gPolicy = dPolicy_dLogp * (indicator - p[j]);
                        var gEntropy = entCoef * p[j] * ((float)Math.Log(Math.Max(p[j], 1e-12f)) + (float)entropy);
                        dOut[j] = gPolicy + gEntropy;
                    }
                    else
                    {
                        dOut[j] = 0f;
                    }
                }

                this.actor.BackwardAccum(features, xOff, dOut);

                // ---- Critic: 0.5*(V - R)^2 ----
                this.critic.Forward(features, xOff);
                var v = this.critic.Out[0];
                var dV = valueCoef * (v - ret);
                this.critic.GradOutScratch[0] = dV;
                this.critic.BackwardAccum(features, xOff, this.critic.GradOutScratch);

                // ---- diagnostics ----
                var unclipped = ratio * adv;
                var clipd = Math.Clamp(ratio, 1f - clipEps, 1f + clipEps) * adv;
                sumPolicyLoss += -Math.Min(unclipped, clipd);
                sumValueLoss += 0.5 * (v - ret) * (v - ret);
                sumEntropy += entropy;
                sumKl += oldLp - newLp; // approx KL(old||new)
            }

            var scale = 1f / mbSize;
            this.actor.ScaleGrad(scale);
            this.critic.ScaleGrad(scale);
            this.actor.AdamStep(actorLr, gradClip);
            this.critic.AdamStep(criticLr, gradClip);

            return new Diag
            {
                PolicyLoss = sumPolicyLoss / mbSize,
                ValueLoss = sumValueLoss / mbSize,
                Entropy = sumEntropy / mbSize,
                ApproxKl = sumKl / mbSize,
                ClipFrac = (double)clipCount / mbSize,
            };
        }

        /// <summary>Minimal 2-hidden-layer MLP with Adam, ReLU hidden, linear output.</summary>
        private sealed class Mlp
        {
            private readonly int inSize;
            private readonly int h1;
            private readonly int h2;
            private readonly int outSize;

            private readonly float[] w1;
            private readonly float[] b1;
            private readonly float[] w2;
            private readonly float[] b2;
            private readonly float[] w3;
            private readonly float[] b3;

            private readonly float[] gw1;
            private readonly float[] gb1;
            private readonly float[] gw2;
            private readonly float[] gb2;
            private readonly float[] gw3;
            private readonly float[] gb3;

            private readonly float[] mw1;
            private readonly float[] vw1;
            private readonly float[] mb1;
            private readonly float[] vb1;
            private readonly float[] mw2;
            private readonly float[] vw2;
            private readonly float[] mb2;
            private readonly float[] vb2;
            private readonly float[] mw3;
            private readonly float[] vw3;
            private readonly float[] mb3;
            private readonly float[] vb3;

            private readonly float[] z1;
            private readonly float[] a1;
            private readonly float[] z2;
            private readonly float[] a2;

            private int adamT;

            public Mlp(int inSize, int h1, int h2, int outSize, int seed)
            {
                this.inSize = inSize;
                this.h1 = h1;
                this.h2 = h2;
                this.outSize = outSize;

                this.w1 = new float[h1 * inSize];
                this.b1 = new float[h1];
                this.w2 = new float[h2 * h1];
                this.b2 = new float[h2];
                this.w3 = new float[outSize * h2];
                this.b3 = new float[outSize];

                this.gw1 = new float[this.w1.Length];
                this.gb1 = new float[h1];
                this.gw2 = new float[this.w2.Length];
                this.gb2 = new float[h2];
                this.gw3 = new float[this.w3.Length];
                this.gb3 = new float[outSize];

                this.mw1 = new float[this.w1.Length];
                this.vw1 = new float[this.w1.Length];
                this.mb1 = new float[h1];
                this.vb1 = new float[h1];
                this.mw2 = new float[this.w2.Length];
                this.vw2 = new float[this.w2.Length];
                this.mb2 = new float[h2];
                this.vb2 = new float[h2];
                this.mw3 = new float[this.w3.Length];
                this.vw3 = new float[this.w3.Length];
                this.mb3 = new float[outSize];
                this.vb3 = new float[outSize];

                this.z1 = new float[h1];
                this.a1 = new float[h1];
                this.z2 = new float[h2];
                this.a2 = new float[h2];
                this.Out = new float[outSize];
                this.GradOutScratch = new float[outSize];

                var rng = new Random(seed);
                InitXavier(rng, this.w1, inSize, h1);
                InitXavier(rng, this.w2, h1, h2);
                InitXavier(rng, this.w3, h2, outSize);
            }

            public float[] Out { get; }

            public float[] GradOutScratch { get; }

            public void Forward(float[] x, int xOff)
            {
                for (var i = 0; i < this.h1; i++)
                {
                    var sum = this.b1[i];
                    var row = i * this.inSize;
                    for (var j = 0; j < this.inSize; j++)
                    {
                        sum += this.w1[row + j] * x[xOff + j];
                    }

                    this.z1[i] = sum;
                    this.a1[i] = sum > 0f ? sum : 0f;
                }

                for (var i = 0; i < this.h2; i++)
                {
                    var sum = this.b2[i];
                    var row = i * this.h1;
                    for (var j = 0; j < this.h1; j++)
                    {
                        sum += this.w2[row + j] * this.a1[j];
                    }

                    this.z2[i] = sum;
                    this.a2[i] = sum > 0f ? sum : 0f;
                }

                for (var i = 0; i < this.outSize; i++)
                {
                    var sum = this.b3[i];
                    var row = i * this.h2;
                    for (var j = 0; j < this.h2; j++)
                    {
                        sum += this.w3[row + j] * this.a2[j];
                    }

                    this.Out[i] = sum;
                }
            }

            public void BackwardAccum(float[] x, int xOff, float[] dOut)
            {
                var da2 = new float[this.h2];
                for (var i = 0; i < this.outSize; i++)
                {
                    var d = dOut[i];
                    var row = i * this.h2;
                    for (var j = 0; j < this.h2; j++)
                    {
                        this.gw3[row + j] += d * this.a2[j];
                        da2[j] += d * this.w3[row + j];
                    }

                    this.gb3[i] += d;
                }

                var dz2 = new float[this.h2];
                for (var j = 0; j < this.h2; j++)
                {
                    dz2[j] = this.z2[j] > 0f ? da2[j] : 0f;
                }

                var da1 = new float[this.h1];
                for (var i = 0; i < this.h2; i++)
                {
                    var d = dz2[i];
                    var row = i * this.h1;
                    for (var j = 0; j < this.h1; j++)
                    {
                        this.gw2[row + j] += d * this.a1[j];
                        da1[j] += d * this.w2[row + j];
                    }

                    this.gb2[i] += d;
                }

                for (var i = 0; i < this.h1; i++)
                {
                    var dz1 = this.z1[i] > 0f ? da1[i] : 0f;
                    if (dz1 == 0f)
                    {
                        continue;
                    }

                    var row = i * this.inSize;
                    for (var j = 0; j < this.inSize; j++)
                    {
                        this.gw1[row + j] += dz1 * x[xOff + j];
                    }

                    this.gb1[i] += dz1;
                }
            }

            public void ZeroGrad()
            {
                Array.Clear(this.gw1);
                Array.Clear(this.gb1);
                Array.Clear(this.gw2);
                Array.Clear(this.gb2);
                Array.Clear(this.gw3);
                Array.Clear(this.gb3);
            }

            public void ScaleGrad(float s)
            {
                Scale(this.gw1, s);
                Scale(this.gb1, s);
                Scale(this.gw2, s);
                Scale(this.gb2, s);
                Scale(this.gw3, s);
                Scale(this.gb3, s);
            }

            public void AdamStep(float lr, float gradClip)
            {
                if (gradClip > 0f)
                {
                    double sq = 0;
                    sq += SumSq(this.gw1);
                    sq += SumSq(this.gb1);
                    sq += SumSq(this.gw2);
                    sq += SumSq(this.gb2);
                    sq += SumSq(this.gw3);
                    sq += SumSq(this.gb3);
                    var norm = Math.Sqrt(sq);
                    if (norm > gradClip)
                    {
                        var cs = (float)(gradClip / norm);
                        Scale(this.gw1, cs);
                        Scale(this.gb1, cs);
                        Scale(this.gw2, cs);
                        Scale(this.gb2, cs);
                        Scale(this.gw3, cs);
                        Scale(this.gb3, cs);
                    }
                }

                this.adamT++;
                Adam(this.w1, this.gw1, this.mw1, this.vw1, lr, this.adamT);
                Adam(this.b1, this.gb1, this.mb1, this.vb1, lr, this.adamT);
                Adam(this.w2, this.gw2, this.mw2, this.vw2, lr, this.adamT);
                Adam(this.b2, this.gb2, this.mb2, this.vb2, lr, this.adamT);
                Adam(this.w3, this.gw3, this.mw3, this.vw3, lr, this.adamT);
                Adam(this.b3, this.gb3, this.mb3, this.vb3, lr, this.adamT);
            }

            public void ResetAdam()
            {
                this.adamT = 0;
                Array.Clear(this.mw1);
                Array.Clear(this.vw1);
                Array.Clear(this.mb1);
                Array.Clear(this.vb1);
                Array.Clear(this.mw2);
                Array.Clear(this.vw2);
                Array.Clear(this.mb2);
                Array.Clear(this.vb2);
                Array.Clear(this.mw3);
                Array.Clear(this.vw3);
                Array.Clear(this.mb3);
                Array.Clear(this.vb3);
            }

            public void SaveShippedLayout(Stream s)
            {
                WriteFloats(s, this.w1);
                WriteFloats(s, this.b1);
                WriteFloats(s, this.w2);
                WriteFloats(s, this.b2);
                WriteFloats(s, this.w3);
                WriteFloats(s, this.b3);
            }

            public void LoadShippedLayout(Stream s)
            {
                ReadFloats(s, this.w1);
                ReadFloats(s, this.b1);
                ReadFloats(s, this.w2);
                ReadFloats(s, this.b2);
                ReadFloats(s, this.w3);
                ReadFloats(s, this.b3);
            }

            private static void InitXavier(Random rng, float[] arr, int inDim, int outDim)
            {
                var limit = (float)Math.Sqrt(6.0 / (inDim + outDim));
                for (var i = 0; i < arr.Length; i++)
                {
                    arr[i] = (float)(((rng.NextDouble() * 2.0) - 1.0) * limit);
                }
            }

            private static void Adam(float[] p, float[] g, float[] m, float[] v, float lr, int t)
            {
                const float B1 = 0.9f, B2 = 0.999f, Eps = 1e-8f;
                var bc1 = 1f - MathF.Pow(B1, t);
                var bc2 = 1f - MathF.Pow(B2, t);
                for (var i = 0; i < p.Length; i++)
                {
                    var gi = g[i];
                    m[i] = (B1 * m[i]) + ((1f - B1) * gi);
                    v[i] = (B2 * v[i]) + ((1f - B2) * gi * gi);
                    p[i] -= lr * (m[i] / bc1) / (MathF.Sqrt(v[i] / bc2) + Eps);
                }
            }

            private static void Scale(float[] a, float s)
            {
                for (var i = 0; i < a.Length; i++)
                {
                    a[i] *= s;
                }
            }

            private static double SumSq(float[] a)
            {
                double s = 0;
                for (var i = 0; i < a.Length; i++)
                {
                    s += (double)a[i] * a[i];
                }

                return s;
            }

            private static void WriteFloats(Stream s, float[] arr)
            {
                var bytes = new byte[arr.Length * sizeof(float)];
                Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
                s.Write(bytes, 0, bytes.Length);
            }

            private static void ReadFloats(Stream s, float[] dest)
            {
                var bytes = new byte[dest.Length * sizeof(float)];
                var read = 0;
                while (read < bytes.Length)
                {
                    var n = s.Read(bytes, read, bytes.Length - read);
                    if (n == 0)
                    {
                        throw new EndOfStreamException("Truncated weights stream.");
                    }

                    read += n;
                }

                Buffer.BlockCopy(bytes, 0, dest, 0, bytes.Length);
            }
        }
    }
}
