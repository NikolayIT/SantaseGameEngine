# The Santase Neural Player

A from-scratch, pure-C# neural network that plays Santase (66 / Schnapsen). It replaces
the hand-tuned heuristic of `ClaudePlayer` with a learned policy and, after training,
**beats every other player in this repository** — including the bot it learned from.

This document explains what it is, how it is built, how it was trained ("educated"),
how to reproduce or improve it, and how to maintain it.

---

## Table of contents

1. [The idea](#1-the-idea)
2. [Where everything lives](#2-where-everything-lives)
3. [Network architecture](#3-network-architecture)
4. [Input: feature encoding](#4-input-feature-encoding)
5. [Output and action selection](#5-output-and-action-selection)
6. [Inference runtime](#6-inference-runtime)
7. [Integration into the player](#7-integration-into-the-player)
8. [Education: the training pipeline](#8-education-the-training-pipeline)
9. [Results](#9-results)
10. [Reproduction guide](#10-reproduction-guide)
11. [Promotion and validation gate](#11-promotion-and-validation-gate)
12. [Maintenance and support](#12-maintenance-and-support)
13. [FAQ](#13-faq)

---

## 1. The idea

`ClaudePlayer` is a strong Santase AI: a hand-written heuristic for general play plus an
**exact alpha-beta minimax** for the non-closed Phase-2 perfect-information endgame. The
heuristic is ~600 lines of rules (marriage preservation, trump draining, guaranteed-winner
search, etc.).

The goal: **replace that heuristic with a neural network that learns to play at least as
well — and ideally better — purely from self-play**, under three hard constraints:

- **No external libraries at runtime.** No TorchSharp, ONNX, native BLAS, or P/Invoke.
  The inference path is plain managed C# over `float[]`.
- **Runs anywhere `net10.0` runs**, including `net10.0-android` via the MAUI UI. Pure
  managed code with zero native dependencies satisfies this for free.
- **No change to the shipped engine.** `Santase.Logic` (the NuGet package) and the
  `IPlayer` contract are untouched. The net is an AI player like any other.

Training may use whatever compute is available (it is offline and one-off), but it is
also pure C#: the trainer is CPU-only and parallel across cores. There is deliberately
**no GPU path** — adding one would require an external-dependency rewrite that breaks the
constraints above.

The minimax endgame is *kept*. It is already optimal for the small perfect-information
subgame, so the network only has to be good at the part the heuristic was doing — Phase-1
and closed-Phase-2 card choice.

---

## 2. Where everything lives

| Path | Role |
|---|---|
| `src/AI/Santase.AI.ClaudePlayer/Neural/NeuralNetwork.cs` | Inference MLP (forward pass only). Shipped. |
| `src/AI/Santase.AI.ClaudePlayer/Neural/NeuralFeatureEncoder.cs` | Game state → 128-float input vector. Shipped. |
| `src/AI/Santase.AI.ClaudePlayer/Neural/NeuralWeightsLoader.cs` | Loads embedded weights; Xavier fallback. Shipped. |
| `src/AI/Santase.AI.ClaudePlayer/Neural/XavierInitializer.cs` | Deterministic random init (fallback / cold start). Shipped. |
| `src/AI/Santase.AI.ClaudePlayer/Neural/weights.bin` | **The shipped trained policy** — embedded resource (~144 KB). |
| `src/AI/Santase.AI.ClaudePlayer/Neural/weights_supervised.bin` | Supervised-clone baseline; PPO warm-start; revert point. |
| `src/AI/Santase.AI.ClaudePlayer/ClaudePlayerNeural.cs` | The player: minimax endgame + NN policy + rule gates. Shipped. |
| `src/AI/Santase.AI.ClaudePlayer/ClaudePlayer.cs` | The heuristic player; also the supervised "teacher". Shipped. |
| `tools/NeuralTrainer/` | Offline trainer (not in `Santase.sln`). `--supervised` / `--ppo` / `--validate`. |
| `src/Tests/Santase.Tests.GameSimulations/` | Benchmark harness + `--gen-training-data` dataset export. |
| `src/Tests/Santase.AI.ClaudePlayer.Tests/` | 23 unit tests (net, encoder, legal-move, player-vs-bot). |

Everything under `Neural/` is **pure managed C#**. The trainer references
`Santase.AI.ClaudePlayer` but is dev-only tooling kept out of the solution so
`dotnet build src\Santase.sln` never compiles non-product code.

---

## 3. Network architecture

A small fully-connected multilayer perceptron (MLP):

```
 input            hidden 1         hidden 2          output
 128 floats  -->  128 ReLU    -->  128 ReLU     -->  24 linear logits
            W1,b1            W2,b2             W3,b3
```

- **Layers**: `128 → 128 → 128 → 24`. ReLU on the two hidden layers; the output layer is
  linear (raw logits — the softmax is applied at the decision boundary, masked to legal
  moves, see §5).
- **Parameters**: `128·128 + 128 + 128·128 + 128 + 24·128 + 24 = 36,120` float32 values
  = **144,480 bytes** on disk. Constants live in `NeuralNetwork.cs`
  (`InputSize/Hidden1Size/Hidden2Size/OutputSize`, `TotalWeightCount`,
  `ExpectedWeightFileBytes`) and are **load-bearing**: the trainer mirrors them and the
  weights blob is sized to them.
- **Why this shape?** Santase has a small, well-structured state and only 24 distinct
  cards. A wide-enough 2-hidden-layer MLP has ample capacity to represent the heuristic
  and then exceed it, while staying tiny enough that a hand-written CPU forward pass costs
  well under a millisecond per move — fast enough that the 200k-game simulator still runs
  in reasonable time.
- **Weight layout** (row-major, exactly what `NeuralNetwork.LoadFromStream` consumes and
  what the trainer writes): `W1, b1, W2, b2, W3, b3`, contiguous little-endian float32.

Changing the architecture is a **coordinated change** across `NeuralNetwork`,
`NeuralFeatureEncoder` (if the input changes), the weights blob, and `tools/NeuralTrainer`
(which hard-codes the same dimensions). They will silently disagree if you change one
alone.

---

## 4. Input: feature encoding

`NeuralFeatureEncoder.Encode(...)` turns `(PlayerTurnContext, my hand, played cards,
unknown cards)` into a fixed **128-float** vector. Cards use a stable index
`cardIndex = (int)suit * 6 + typeRank[type]` where
`typeRank = { Nine:0, Jack:1, Queen:2, King:3, Ten:4, Ace:5 }` (value order, matching
`ClaudePlayer.AllTypes`). The 24 output logits use the *same* index space, so output slot
`k` corresponds to exactly one card.

| Offset | Size | Meaning |
|---|---|---|
| `[0, 24)` | 24 | My hand, one-hot per card |
| `[24, 48)` | 24 | Cards already played this round, one-hot |
| `[48, 72)` | 24 | Cards still unknown to me (opp could hold / in deck), one-hot |
| `[72, 76)` | 4 | Trump suit, one-hot (Club, Diamond, Heart, Spade) |
| `[76, 82)` | 6 | Trump card type, one-hot |
| `[82, 106)` | 24 | Opponent's lead card this trick, one-hot (zeros if I lead) |
| `[106]` | 1 | `CardsLeftInDeck / 12` |
| `[107]` | 1 | My round points `/ 66` |
| `[108]` | 1 | Opponent round points `/ 66` |
| `[109]` | 1 | `(my − opp)` round points `/ 66` |
| `[110]` | 1 | `ShouldObserveRules` |
| `[111]` | 1 | `CanClose` |
| `[112]` | 1 | `CanChangeTrump` |
| `[113]` | 1 | `CanAnnounce20Or40` |
| `[114]` | 1 | `IsFirstPlayerTurn` (I am leading) |
| `[115]` | 1 | Trump card still visible (`CardsLeftInDeck ≥ 2`) |
| `[116]` | 1 | I hold the 9 of trump |
| `[117]` | 1 | Opponent announced 20 on their lead |
| `[118]` | 1 | Opponent announced 40 on their lead |
| `[119, 128)` | 9 | Reserved (zero) — headroom for new features without resizing |

The reserved tail means you can add up to 9 scalar features without changing the network
input size or invalidating the weights file shape (you still must retrain).

The encoder's "first player = leader of the current trick" convention matches
`ClaudePlayer`'s mapping (`FirstPlayerRoundPoints` is the leader's points), so the points
features are always from the acting player's perspective.

---

## 5. Output and action selection

The 24 logits are **not** softmaxed over all cards. The engine restricts legal moves
(follow suit, beat the led card, etc.), so the policy is a softmax **masked to the legal
set** for the current turn:

- **Production (deterministic)**: `Temperature = 0` → pick `argmax` over the legal cards.
  This is what ships and what `--validate` measures.
- **Training (stochastic)**: `Temperature > 0` → sample from the temperature-scaled
  softmax over legal cards. PPO runs at `Temperature = 1` so the behavior policy is
  exactly the training distribution (a correctness requirement for the importance ratio).

If the masked distribution is degenerate (NaN/Inf/zero sum) the code falls back to a
legal argmax, so a corrupt or untrained net still produces only legal moves.

---

## 6. Inference runtime

`NeuralNetwork`:

- Pure `float[]` matmuls, ReLU, no allocations on the hot path (reusable scratch buffers).
- Loaded by `NeuralWeightsLoader.Load()`: tries the embedded resource
  `Santase.AI.ClaudePlayer.Neural.weights.bin`; if absent, falls back to deterministic
  Xavier init (so the project always runs, even with no trained weights).
- **Not thread-safe** (shared scratch). The simulator runs games in parallel, so each
  game constructs its own `ClaudePlayerNeural` with its own `NeuralNetwork` — never share
  one instance across threads. The trainer serializes weights to a `byte[]` snapshot per
  generation and each parallel self-play game deserializes its own net from it.
- Endianness: weights are little-endian float32; all supported runtimes are little-endian
  so no byte-swapping is done.

`weights.bin` ships as an `<EmbeddedResource>` with an explicit `LogicalName`, so it is
baked into the assembly — there is no external file to deploy. Updating the policy means
replacing the file and **rebuilding `Santase.AI.ClaudePlayer`** to re-embed it.

---

## 7. Integration into the player

`ClaudePlayerNeural` is a sibling of `ClaudePlayer` with the same bookkeeping
(`UnknownCards`, `PlayedCards`, trump-sync). Per turn:

1. **Trump-swap gate** — rule-based (trade 9-of-trump for the face-up trump when legal).
   Kept as-is; it is a discrete tactical rule, not a scoring decision.
2. **Close-game gate** — rule-based (close on a strong trump hand). Kept as-is.
3. **Non-closed Phase-2 endgame** — exact alpha-beta **minimax** to terminal. Kept as-is;
   the opponent's hand is exactly `UnknownCards` here, so this is optimal and the NN would
   only add noise.
4. **Everything else** — the **neural policy**: encode features → forward pass → mask to
   legal moves → argmax. This is the ~600-line heuristic tree, replaced by one MLP.

Training-only seams, both `null` in production (zero cost):

- `ClaudePlayer.TrainingRecorder` — `(features, chosenCard)` for supervised cloning. Fires
  only on heuristic-path decisions (not minimax).
- `ClaudePlayerNeural.PpoRecorder` — `(features, action, legalMask, oldLogProb)` for PPO.
- `ClaudePlayerNeural.Temperature` / `Rng` — exploration controls for self-play.

---

## 8. Education: the training pipeline

```
 ClaudePlayer self-play              supervised clone            PPO self-play
 (--gen-training-data)   ─────────▶  (--supervised)    ───────▶  (--ppo)        ──┐
   binary dataset            ~700k   weights_supervised.bin        weights.best   │
   (features, card)          samples  ≈ 47% vs heuristic           ≈ 71%          │
                                                                                  ▼
                                                              validate ≥50k games
                                                              promote → weights.bin
```

### Stage 0 — Data generation

The simulator's `--gen-training-data <games> <outpath>` plays `ClaudePlayer` against
itself and records `(features, chosen_card)` for every **heuristic-path** decision (skip
minimax decisions — the neural player keeps using minimax there anyway). Output is a
compact binary file (`STSE` header + 513-byte records), written thread-safely by
`TrainingDataCollector`. ~5,000 games yields ~700k samples.

### Stage 1 — Supervised cloning (the warm start)

`tools/NeuralTrainer --supervised` trains the MLP to **imitate the heuristic** with
softmax cross-entropy and Adam. After ~15 epochs it predicts the heuristic's exact card
choice with ~95% accuracy. As a *player* this is ≈47% vs the heuristic — essentially "as
good as the teacher", which is the ceiling of pure imitation.

This stage exists because starting PPO from random weights is hopeless (the reward signal
is far too sparse to discover competent play from scratch in feasible time). The clone is
a competent starting policy PPO can then *improve*.

### Stage 2 — REINFORCE (what we tried, and why it failed)

The first RL attempt was vanilla REINFORCE (policy gradient with game-outcome reward).
**It does not work here.** It plateaued around ~48% and then suffered catastrophic policy
collapse (win rate → ~0%, action log-probabilities exploding), even with conservative
learning rates and gradient clipping. Twice. The collapse is the well-known REINFORCE
instability: high-variance updates, no trust region, off-policy drift within a batch. The
REINFORCE code was removed; it is documented here only so nobody re-derives the dead end.

### Stage 3 — PPO (what works)

`tools/NeuralTrainer --ppo` is Proximal Policy Optimization. This is the step that
actually beats the heuristic. Components:

- **Actor** = the shipped MLP architecture (warm-started from `weights_supervised.bin`).
- **Critic** = a separate, **training-only** `128→128→128→1` value network. Never shipped.
- **Clipped surrogate objective** (ε = 0.2) — the trust region that prevents the
  REINFORCE-style collapse. Hand-derived gradients through the masked softmax.
- **GAE(λ)** advantages (γ = 0.997, λ = 0.95) from the critic, normalized per batch.
- **Entropy bonus** (coef 0.01) — keeps exploration alive; entropy *rose* during the
  successful run instead of collapsing.
- **Potential-based reward shaping** — `r' = γ·Φ(s') − Φ(s)` with the round-point
  potential `Φ = (myPts − oppPts)` (already in the feature vector). This is
  policy-invariant (does not change the optimal policy) but densifies an otherwise
  game-end-only ±1 reward, dramatically lowering variance.
- **Self-supervising harness** — every 25 generations it evaluates the *deterministic*
  policy over 6,000 games, keeps `checkpoints/weights.best.bin` on improvement, and
  **auto-stops** on collapse (eval < 0.7 × best) or stalled progress (20 evals without a
  new best). This makes long unattended runs safe and removes manual checkpoint hunting.

Default PPO hyperparameters (overridable on the CLI), from `PpoProgram.cs`:

| Param | Default | Param | Default |
|---|---|---|---|
| games/generation | 500 | actor lr | 1e-4 |
| minibatch | 1024 | critic lr | 3e-4 |
| epochs/batch | 4 | clip ε | 0.2 |
| γ | 0.997 | entropy coef | 0.01 |
| λ (GAE) | 0.95 | value coef | 0.5 |
| grad clip | 0.5 | eval every | 25 gens |
| eval games | 6000 | patience | 20 evals |
| collapse factor | 0.7 | | |

A single 9-hour run (≈2,161 generations, ≈240M training samples on a 20-core CPU) climbed
**monotonically** from ~47% to ~71% with no collapse and was still slowly improving when
the wall-clock budget ended.

---

## 9. Results

Win rate of the trained `ClaudePlayerNeural` vs each opponent, 200,000 games each
(deterministic argmax policy, the production behavior):

| Opponent | Neural (PPO) | Heuristic `ClaudePlayer` (for reference) |
|---|---|---|
| `ClaudePlayer` (heuristic) | **~71%** | — |
| `SmartPlayer` | **~73%** | ~56% |
| `NinjaPlayer` (best external bot) | **~76%** | ~64% |
| `DummyPlayerChangingTrump` | ~100% | ~100% |
| `DummyPlayer` | ~100% | ~100% |

Progression of the same network through the pipeline (50k-game validation vs the
heuristic, standard error ≈ 0.22%):

| Stage | Win rate vs heuristic |
|---|---|
| Random Xavier init | 0.08% |
| Supervised clone | ~47% |
| REINFORCE (best, then collapsed) | ~48% |
| **PPO (shipped)** | **~71%** |

The decisive result: PPO was trained **only against `ClaudePlayer`**, yet it beats
`SmartPlayer` and the external `NinjaPlayer` by *larger* margins than the heuristic does.
It learned general Santase skill, not a `ClaudePlayer`-specific exploit. The ~100% vs the
dummies confirms no degenerate overfitting.

---

## 10. Reproduction guide

From the repo root, in PowerShell, Release configuration throughout:

```powershell
# 0. Export a supervised dataset (ClaudePlayer self-play).
dotnet run -c Release --project src\Tests\Santase.Tests.GameSimulations\Santase.Tests.GameSimulations.csproj -- --gen-training-data 5000 dataset.bin

# 1. Supervised clone -> the PPO warm-start.
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --supervised --data dataset.bin --out src\AI\Santase.AI.ClaudePlayer\Neural\weights_supervised.bin --epochs 15

# 2. PPO self-play fine-tune (auto early-stops; safe to leave running).
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --ppo --in src\AI\Santase.AI.ClaudePlayer\Neural\weights_supervised.bin --out src\AI\Santase.AI.ClaudePlayer\Neural\checkpoints --hours 9

# 3. Validate the best checkpoint (deterministic, production-equivalent metric).
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --validate src\AI\Santase.AI.ClaudePlayer\Neural\checkpoints\weights.best.bin 50000
```

Training is CPU-bound and parallel across all cores. The PPO run prints per-generation
diagnostics (policy loss, value loss, entropy, approx-KL, clip fraction) and periodic
`EVAL` lines with the true win rate; the final report names the file to promote.

---

## 11. Promotion and validation gate

The trainer **never** overwrites the shipped `weights.bin`. It writes
`checkpoints/weights.best.bin`. Shipping a new policy is a deliberate, gated step:

1. `--validate checkpoints\weights.best.bin 50000` and compare to the current production
   `weights.bin` at the same game count. With SE ≈ 0.22% at 50k games, only promote on a
   difference well beyond noise (the PPO jump from ~47% to ~71% is ~100σ — unambiguous;
   marginal +0.x% differences are not real).
2. Copy the winner over `src\AI\Santase.AI.ClaudePlayer\Neural\weights.bin`.
3. **Rebuild `Santase.AI.ClaudePlayer`** so the new resource is re-embedded.
4. Confirm via the canonical `ClaudeNeuralVsClaudeSimulator` workload (embedded-resource
   path) and the 23 `Santase.AI.ClaudePlayer.Tests`.

`weights_supervised.bin` is the always-available revert point. `Neural/checkpoints/` and
`training_*.bin` are git-ignored by the repo-root `.gitignore`; the two shipped weight
files are explicitly **not** ignored.

---

## 12. Maintenance and support

### Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `ClaudePlayerNeural` plays badly / randomly | `weights.bin` not embedded (loader fell back to Xavier). Confirm the `<EmbeddedResource>` is in the csproj and you rebuilt `Santase.AI.ClaudePlayer`. `--validate` should report ~71%. |
| PPO win rate collapses to ~0 | Lower `--actor-lr`, raise `--ent`, or lower `--clip`. The collapse guard should auto-halt and `weights.best.bin` is preserved from before the collapse. |
| PPO eval flat at ~47% forever | It is not learning past the clone. Check reward shaping is on (it is by default), try a slightly higher `--actor-lr`, more `--games-per-gen` (lower variance), or longer run. |
| "Truncated weights stream" on load | `weights.bin` size ≠ `ExpectedWeightFileBytes` (144,480). The file is corrupt or the architecture changed without retraining. |
| Trainer build fails after engine change | `tools/NeuralTrainer` `ProjectReference`s `Santase.AI.ClaudePlayer`; it is not in the sln so a plain sln build will not catch this. Build the tool to check. |
| Feature dim mismatch in `--supervised` | Dataset was generated with a different `NeuralFeatureEncoder` layout. Regenerate the dataset. |

### Extending it

- **New input feature**: use the reserved `[119, 128)` slots in `NeuralFeatureEncoder`
  (no size change), or grow the vector — which is a coordinated change (see §3) and
  invalidates existing weights (retrain from Stage 0).
- **Bigger network**: change the `NeuralNetwork` size constants; the trainer mirrors them
  automatically via the same constants, but you must retrain — the weights file shape
  changes.
- **Train against more opponents**: PPO self-play currently faces `ClaudePlayer`. An
  opponent pool (frozen self-snapshots + heuristic) is the natural next step to push past
  the current plateau and avoid over-specialization.
- **PPO is still improving at 9h.** Longer runs, an opponent pool, or a learning-rate
  schedule are the most promising avenues for further gains.

### Known limitations

- CPU-only training by design; a 9h run is ~2k generations on a 20-core machine.
- The net only replaces Phase-1 / closed-Phase-2 play; the perfect-information endgame is
  still exact minimax (intentionally — it is already optimal there).
- Strength is measured vs the bots in this repo. It is not benchmarked vs humans.

### Coordinated-change checklist

If you touch the architecture or feature layout, update **all** of:
`NeuralNetwork.cs` constants · `NeuralFeatureEncoder.cs` layout · `weights.bin`
(retrain) · `tools/NeuralTrainer` (hard-codes the same dims) · this document.

---

## 13. FAQ

**Why not just use PyTorch / ONNX?** The runtime must be pure managed C# with no native
dependencies so it deploys to Android via MAUI unchanged. Training could use anything, but
keeping the trainer pure C# too avoids a second toolchain and keeps the whole pipeline in
one language and one repo.

**Why keep the minimax?** It is an exact solver for the perfect-information endgame. A
network can at best match it there and would add variance. The net's job is the part that
was *heuristic*, not the part that was *solved*.

**Why was supervised cloning necessary?** RL from random weights cannot discover competent
Santase play from sparse game-outcome reward in feasible time. The clone gives PPO a
competent starting point to improve from.

**Is the result luck?** No. The PPO climb is monotonic over ~2,000 generations, the
improvement is ~100 standard errors at 50k validation games, and it generalizes to
opponents it never trained against. It is reproducible with the commands in §10.

**Can it get stronger?** Almost certainly — it was still improving when the 9-hour budget
ended. Opponent pools, longer runs, and LR schedules are the open avenues.
