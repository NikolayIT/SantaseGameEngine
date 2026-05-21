# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository overview

C# implementation of the **Santase / 66 / Schnapsen / Sechsundsechzig** two-player trick-taking card game. The engine in `src/Santase.Logic` is shipped as the [SantaseGameEngine](https://www.nuget.org/packages/SantaseGameEngine) NuGet package; everything else is AI players, a console UI, and a parallel simulator harness used to benchmark AI strength.

Solution: `src/Santase.sln`. The commands below assume the repo root is the working directory.

## Build, test, run

```powershell
# Restore + build the whole solution (Release recommended; the simulator is benchmark-grade).
dotnet build src\Santase.sln -c Release

# Run the AI benchmark / game simulator (net10.0 console app, parallelized to all cores).
# This is the primary regression test for AI changes. Always run in Release.
dotnet run -c Release --project src\Tests\Santase.Tests.GameSimulations\Santase.Tests.GameSimulations.csproj

# Run the human-playable Console UI (net10.0).
dotnet run --project src\UI\Santase.UI.Console\Santase.UI.Console.csproj

# Run the cross-platform MAUI desktop/mobile UI (Santase.UI).
dotnet build src\UI\Santase.UI\Santase.UI.csproj -t:Run

# Run the unit tests via CLI (xunit, ~293 tests across 3 projects).
dotnet test src\Santase.sln -c Release
```

### Unit tests

`Santase.Logic.Tests`, `Santase.AI.SmartPlayer.Tests`, and `Santase.AI.ClaudePlayer.Tests` are xUnit test projects targeting `net10.0` with `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` referenced — they run via both `dotnet test` and Visual Studio's Test Explorer. Approx. counts: Logic.Tests ~268, ClaudePlayer.Tests 26 (neural net / feature encoder / legal-move + player-vs-bot smoke, incl. 3 `ClaudePlayerIsmcts` smoke tests), SmartPlayer.Tests 2.

Tests in `Santase.Tests.GameSimulations/Tests/` (the `*LoggerTests.cs` files) live inside the simulator's `Exe` project and are not invoked by the simulator's `Main` or by `dotnet test` (the simulator csproj is `OutputType=Exe`, not a test SDK project) — they're VS-Test-Explorer artifacts.

### Platform notes

- Every project targets `net10.0`. Before the .NET 10 migration the library + AI projects were `netstandard2.0`, the simulator was `netcoreapp3.1`, and the console UI was `net5.0` — recent commits in `git log` still reference those frameworks if you need to compare.
- `Santase.Logic` is the published [SantaseGameEngine](https://www.nuget.org/packages/SantaseGameEngine) NuGet package — bumping its TFM is a breaking change for downstream consumers (current package version is `3.0.0`, post-migration).
- The third-party AI players in `src/AI/External/*.dll` (`BotskoPlayer`, `NinjaPlayer`, `ProPlayer`) are binary references — no source. They're `.NETPortable` (PCL) assemblies, which load fine from `net10.0`. Treat their `IPlayer` contract as load-bearing for the simulator.

## Architecture

### Layering (dependencies flow downward)

```
   Santase.UI.Console   Santase.UI (MAUI)   Santase.Tests.GameSimulations (+ unit tests)
            │                 │                              │
            └─────────────────┴──────────────┬───────────────┘
                                 ▼
     AI players (ClaudePlayer/ClaudePlayerNeural, SmartPlayer, DummyPlayer, External *.dll)
                                 │
                                 ▼
                          Santase.Logic   ← the engine, the NuGet package
```

`Santase.Logic` has zero non-StyleCop dependencies. AI projects depend only on `Santase.Logic`. **Never let `Santase.Logic` take a dependency on an AI or UI project** — it would corrupt the NuGet package's public surface.

The **MAUI UI (`Santase.UI`) references `Santase.AI.ClaudePlayer`** (alongside `SmartPlayer`/`DummyPlayer`) so it can offer all five AIs as selectable opponents — an `IPlayer`-surface break in ClaudePlayer now also breaks the app build, not just the simulator. The UI ranks the human with an on-device ELO (`Game/PlayerRatingStore.cs`, via MAUI `Preferences`; new players start at 1000 and earn upward) against the fixed AI ratings baked into `Game/AiOpponent.cs` — those numbers come from the simulator's `elo` mode (below) and must be re-pasted if the AIs change. Completed vs-AI games are kept in `Game/MatchHistoryStore.cs` (Preferences, newest-first) and shown on the start page.

Two load-bearing UI facts:
- **Localization is in-code, not `.resx`.** English + Bulgarian live in plain dictionaries under `Localization/` (`AppStrings.cs`), looked up by `LocalizationManager` — no satellite assemblies, so it's trimming/AOT-safe and identical on every platform. XAML uses `{loc:Tr Key}` (a binding to the manager's indexer, so it updates live); C# uses `LocalizationManager.Instance[...]` / `.Format(...)`. Default is the device locale; an in-app toggle (start page) overrides and persists it. Add a string ⇒ add the key to **both** dictionaries.
- **Round results come from the game-point delta, not round points.** The engine never passes `RoundResult` to an `IPlayer`, and `UpdatePoints` runs *after* `EndRound`, so `GameSession` defers the round outcome: it reads the engine's public total at the next `StartRound` (or at game over) and the delta vs the round's starting total is the authoritative award/winner. A naive `myRoundPoints > oppRoundPoints` is wrong when a player closes and fails to reach 66 (they have more points but lose) — don't reintroduce it.

### Engine core (`src/Santase.Logic`)

Public entry point: `SantaseGame` in `GameMechanics/SantaseGame.cs`. Hand it two `IPlayer` instances and call `Start()`; it plays rounds until someone reaches `IGameRules.GamePointsNeededForWin` (default 11, from `SantaseGameRules`).

Load-bearing concepts when editing the engine:

- **`IPlayer` is the only seam for AI.** `Players/IPlayer.cs` defines the player contract (`StartGame` / `StartRound` / `AddCard` / `GetTurn` / `EndTurn` / `EndRound` / `EndGame`). Every AI inherits from `BasePlayer`, which owns the `Cards` collection and exposes `ChangeTrump`, `PlayCard`, `CloseGame` helpers. **Adding members to `IPlayer` breaks the external `AI.External/*.dll` players** — those are binary references with no source, so a signature change there is effectively unfixable without losing them as simulator opponents. Prefer extending `PlayerTurnContext` or adding to `BasePlayer` over changing `IPlayer`.
- **Round state machine** (`RoundStates/`). The two phases of Santase (talon still has cards / talon closed or empty) are modeled with the State pattern: `StartRoundState` → `MoreThanTwoCardsLeftRoundState` → `TwoCardsLeftRoundState` → `FinalRoundState`, coordinated by `StateManager`. `BaseRoundState` exposes booleans (`ShouldObserveRules`, `CanClose`, `CanChangeTrump`, `CanAnnounce20Or40`, `ShouldDrawCard`) — these flags are how AIs and validators know which phase they're in. If you add a phase-dependent rule, add the flag here rather than scattering `if (cardsLeftInDeck == ...)` checks.
- **`PlayerActionValidate/`** is the single source of truth for legal moves in a given `PlayerTurnContext`. Both `IAnnounceValidator` and `IPlayerActionValidator` are exposed as singletons via a static `.Instance`. AIs use these to filter their move generation; the engine uses them to reject invalid `PlayerAction`s.
- **Engine `internal` surface + `InternalsVisibleTo`.** `GameMechanics/Round.cs`, `Trick.cs`, `RoundResult.cs`, etc. are `internal` and exposed to tests via per-project files named `InternalsVisibleToContainer.cs` containing `[assembly: InternalsVisibleTo(...)]`. Don't make engine internals `public` to "test something easily" — add the test assembly to that file instead.
- **`PlayerTurnContext` implements `IDeepCloneable<PlayerTurnContext>`** because `SmartPlayer` (and any future lookahead AI) clones contexts to do simulation. If you add a field to `PlayerTurnContext`, also wire it into `DeepClone()`, or lookahead silently loses information.
- **`Card` is a flyweight.** `Cards/Card.cs` pre-allocates all 52 (+1 dummy) cards into a static array, and the public constructor is `[Obsolete]`. Always use `Card.GetCard(suit, type)` — never `new Card(...)`. `Card.Equals`/`GetHashCode` are based on `(Suit, Type)`, so two `GetCard` calls return the same instance and equality is a pointer compare in practice. Hot simulator loops depend on this.
- **Game rules are pluggable** via `IGameRules` (`SantaseGameRules` is the default, exposed through `GameRulesProvider.Santase`). To prototype rule variants, implement a new `IGameRules` rather than editing `SantaseGameRules`.

### AI architecture (`src/AI`)

- **`Santase.AI.DummyPlayer`** — baseline legal-random players used as a punching bag in simulations. Two flavors: `DummyPlayer` and `DummyPlayerChangingTrump`.
- **`Santase.AI.SmartPlayer`** — the real AI. The interesting structure is in `Strategies/`: `ChooseBestCardToPlayStrategy` dispatches into one of four sub-strategies based on `(IsFirstPlayerTurn, ShouldObserveRules)`:
  - `PlayingFirstAndRulesDoNotApplyStrategy` / `PlayingFirstAndRulesApplyStrategy`
  - `PlayingSecondAndRulesDoNotApplyStrategy` / `PlayingSecondAndRulesApplyStrategy`

  This 2×2 split mirrors phase × leader-vs-follower and is where new heuristics go. `Helpers/CardTracker.cs` maintains SmartPlayer's belief over which cards remain unseen — it must stay in lockstep with `StartRound` / `AddCard` / `EndTurn` flow.
- **`GlobalStats`** in `Santase.AI.SmartPlayer` is a static counter bag the simulator reads to print per-batch diagnostics (`GamesClosedByPlayer`, `GlobalCounterValues[4]`). It is not thread-safe; the simulator runs games in parallel, so small absolute counts have inherent jitter — don't read significance into single-digit changes between runs.
- **`Santase.AI.ClaudePlayer`** — the repo's strongest AIs. Three players: the self-contained hand-tuned `ClaudePlayer` and MLP-policy `ClaudePlayerNeural`, plus the search player `ClaudePlayerIsmcts` (true ISMCTS), which sits on the `ClaudeSearchPlayerBase` scaffolding (round bookkeeping, the rule-based trump-swap/close gates, the bitmask simulator + strong perfect-information rollout, the exact alpha-beta endgame solve, and determinization — the base is abstract so a future search variant can reuse it). **`ClaudePlayerIsmcts` is the single strongest player in the repo** — it beats `ClaudePlayerNeural` head-to-head. The three:
  - **`ClaudePlayer`** — hand-tuned heuristic + exact alpha-beta minimax for the non-closed Phase-2 perfect-info endgame. Also the supervised *teacher*: it exposes an opt-in `Action<float[],int> TrainingRecorder` that, when set, emits `(features, chosen_card)` for every heuristic-path decision (minimax decisions are not recorded). Null by default → zero production cost.
  - **`ClaudePlayerNeural`** — same minimax endgame and rule-based trump-swap/close gates, but the heuristic card-choice tree is replaced by an MLP policy. `Temperature` (0 = argmax/production, >0 = softmax sampling) and `PpoRecorder` `(features, action, legalMask, oldLogProb)` are training-only seams, null in production.
  - **`ClaudePlayerIsmcts`** — single-observer Information Set MCTS (Cowling/Powley/Whitehouse). Builds **one** tree keyed by the public play history and re-samples a fresh determinization *every iteration*, so a node pools statistics across many opponent hands and the search can never commit to a line that only works in one world — removing the strategy fusion that caps PIMC. Technical core: UCB with **availability counts** (`value + C·sqrt(ln(n′)/n)`, where `n′` = times the child was *legal* in a sampled world), variable per-node fan-out via a linked-list child store (an opponent node accumulates every card it could hold), most-visited root selection. `ExplorationConstant` defaults to **0.2** (set in its ctor) — far below a typical UCT ~1.4: the deep shared tree + re-determinization make exploitation pay (tuned vs `ClaudePlayer`: 0.2→~89%, 0.7→~81%, 1.4→~75%, and **C=0 collapses to ~52%**). `TimeLimitMilliseconds` defaults to 100; granularity is sub-ms so worst case ≈ budget + one iteration. **It is the strongest player in the repo** — see the strength block below.
  - **`Neural/`** is pure managed C# — no native deps, no P/Invoke — so it runs anywhere `net10.0` runs (incl. `net10.0-android` via the MAUI UI). `NeuralNetwork` is a fixed **128→128→128→24** MLP (ReLU hidden, linear logits); the constants `InputSize/Hidden1Size/Hidden2Size/OutputSize` are load-bearing — the trainer mirrors them and the embedded weights file is sized to them, so changing the architecture is a coordinated change across `NeuralNetwork`, `NeuralFeatureEncoder`, the weights blob, and `tools/NeuralTrainer`. `NeuralFeatureEncoder` produces the 128-float input; its layout is documented in-file and **must stay in lockstep** (card index = `suit*6 + typeRank`, matching `AllTypes` order). `NeuralWeightsLoader` loads the embedded `weights.bin`, falling back to deterministic Xavier-init if the resource is absent.
  - **Weights are committed binaries.** `Neural/weights.bin` is an `<EmbeddedResource>` (LogicalName `Santase.AI.ClaudePlayer.Neural.weights.bin`, ~144 KB, 36,120 float32) — the shipped PPO-trained policy. `Neural/weights_supervised.bin` is the supervised-clone baseline kept for comparison and as the PPO warm-start. Both are intentionally **not** git-ignored; `Neural/checkpoints/` (training scratch) is. Regenerate via `tools/NeuralTrainer` (see *Retraining the neural net*).
  - `ClaudePlayerNeural` strength (200k-game benchmark, as P1): ~71% vs `ClaudePlayer`, ~73% vs `SmartPlayer`, ~76% vs `NinjaPlayer` (best external), ~100% vs the dummies. It was trained only against `ClaudePlayer` yet generalizes. It was the strongest player until `ClaudePlayerIsmcts` (below), which beats it head-to-head.
  - `ClaudePlayerIsmcts` strength (100ms, tuned `ExplorationConstant=0.2`, 200-game benchmark, as P1): **~78% vs `ClaudePlayerNeural`**, ~88% vs `ClaudePlayer`, ~84% vs `SmartPlayer`, ~94% vs `NinjaPlayer`, 100% vs `DummyPlayer` — the strongest player in the repo by a clear margin. An earlier PIMC build of this same simulator/rollout (independent per-determinization trees, since removed) only reached ~`ClaudePlayer`-parity and lost to this ~86–14; the jump is the payoff for killing strategy fusion — the single shared information-set tree models the opponent's *uncertainty* instead of solving each world as if the cards were visible. (Benchmarks are 150–200 games, so ±~5pp noise; the gaps are far larger than that.)

### Simulator (`src/Tests/Santase.Tests.GameSimulations`)

A `net10.0` console app (not an xUnit project, despite its location). `Main` runs a named **suite** of head-to-head matchups — each plays `DefaultGamesPerMatchup` (200,000) games — via `Parallel.For(MaxDegreeOfParallelism = Environment.ProcessorCount)` in `BaseGameSimulator`, then prints win counts, round-point totals, and `GlobalStats` counters. Matchups are declared as **data**, not code: one `GameSimulator` is built from two `Func<IPlayer>` factories (there is no longer a class-per-matchup — the ~19 trivial subclasses were collapsed into `GameSimulator.cs`). Select a suite with the first CLI arg and optionally override the game count with the second: `dotnet run -c Release --project ... -- <suite> [games]`. Suites (in `Program.cs`): **`claude`** (default — `ClaudePlayerNeural`-vs-all then heuristic `ClaudePlayer`-vs-all, where the opponents are `ClaudePlayer`/`ClaudePlayerBaseline`, `SmartPlayer`, `NinjaPlayer`, `DummyPlayerChangingTrump`, `DummyPlayer`); **`ismcts`** (`ClaudePlayerIsmcts`-vs-all — **run with a small game count, e.g. `-- ismcts 300`**, since each search move spends its full ~100ms budget and 200k games would take days); **`smart`** (SmartPlayer ad-hoc workloads); **`baseline`** (frozen `ClaudePlayerBaseline`-vs-all, for within-run heuristic-iteration deltas).

`Program.cs` also has a **training-data export mode**: `dotnet run -c Release --project ... -- --gen-training-data <games> <outpath>` plays `ClaudePlayer` self-play and writes a binary `(features, chosen_card)` dataset (header `STSE`, 513-byte records) via `Training/TrainingDataCollector.cs` + `ClaudeSelfPlayTrainingSimulator`. This feeds `tools/NeuralTrainer --supervised`. Without that flag the default benchmark runs.

`Program.cs` also has an **ELO tournament mode**: `dotnet run -c Release --project ... -- elo [fastGames] [ismctsGames]` runs a round-robin among the five UI opponents (`DummyPlayerChangingTrump`, `SmartPlayer`, `ClaudePlayer`, `ClaudePlayerNeural`, `ClaudePlayerIsmcts`), fits Bradley-Terry/ELO ratings (anchored so `DummyPlayerChangingTrump` = 1200, with a 1%-of-games uniform prior so a 100%–0% blow-out can't produce an infinite gap), and prints the table (`GameSimulators/EloTournament.cs`). Defaults: 20,000 games/pair for the fast players, **200** for any pairing involving the slow `ClaudePlayerIsmcts` (≈5 min total). The printed ratings are pasted into the MAUI UI's `Game/AiOpponent.cs` and into this doc — re-run and update both if the AIs change. Latest run: **ISMCTS 2441, Neural 2261, ClaudePlayer 2103, SmartPlayer 2056, Dummy 1200**.

**This is the canonical regression check for AI changes.** Commit messages in this repo embed the simulator output as a baseline (e.g., commit `8c5084c`). Workflow for AI work:

1. Run the simulator on `master` in Release; record the output blocks.
2. Make the AI change.
3. Re-run in Release; compare game/round-point deltas against the recorded baseline.
4. Paste the new output into the commit message (matching prior style) so the next person has a baseline too.

## Retraining the neural net (`tools/NeuralTrainer`)

`ClaudePlayerNeural`'s weights are produced by `tools/NeuralTrainer` — a `net10.0` console app that **is deliberately not in `src/Santase.sln`**. It's dev-only offline tooling (its only output is the already-committed `weights.bin`), so keeping it out of the solution stops `dotnet build src\Santase.sln` from compiling non-product code and keeps "what ships" unambiguous. It `ProjectReference`s `Santase.AI.ClaudePlayer`, so an API break there is caught the next time you build the tool, not by a plain sln build. Pure CPU (parallel self-play across all cores) — there is no GPU path; "use the GPU" would require an external-dependency rewrite (TorchSharp/ONNX) that contradicts the pure-managed/Android constraint.

Three subcommands mirror the pipeline that produced the shipped net:

```powershell
# 0. Export a supervised dataset by ClaudePlayer self-play (the "teacher").
dotnet run -c Release --project src\Tests\Santase.Tests.GameSimulations\Santase.Tests.GameSimulations.csproj -- --gen-training-data 5000 dataset.bin

# 1. Supervised clone: train the MLP to imitate ClaudePlayer -> the PPO warm-start.
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --supervised --data dataset.bin --out src\AI\Santase.AI.ClaudePlayer\Neural\weights_supervised.bin --epochs 15

# 2. PPO self-play fine-tune from the supervised checkpoint (the step that actually beats the heuristic).
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --ppo --in src\AI\Santase.AI.ClaudePlayer\Neural\weights_supervised.bin --out src\AI\Santase.AI.ClaudePlayer\Neural\checkpoints --hours 9

# 3. Deterministic (argmax) win rate vs the heuristic — the production-equivalent metric.
dotnet run -c Release --project tools\NeuralTrainer\NeuralTrainer.csproj -- --validate src\AI\Santase.AI.ClaudePlayer\Neural\weights.bin 50000
```

Load-bearing facts:

- **PPO is the method that works; plain REINFORCE was removed.** REINFORCE plateaued at ~48% then collapsed; PPO (clipped surrogate + a training-only critic + GAE + entropy bonus + potential-based reward shaping) climbed monotonically to ~71% over a 9-hour run. The trainer self-supervises: it evaluates the deterministic policy every N generations, keeps `checkpoints/weights.best.bin`, and early-stops on collapse or stalled progress, so a long run is safe unattended.
- **Promotion is manual and gated.** The trainer never overwrites `weights.bin`; it writes `checkpoints/weights.best.bin`. To ship a new net: validate it at ≥50k games vs the current production `weights.bin`, and only if it's a statistically real improvement copy it over `Neural/weights.bin`, **rebuild `Santase.AI.ClaudePlayer`** (re-embeds the resource), then confirm via the default `claude` simulator suite (its first matchup, `ClaudePlayerNeural` vs `ClaudePlayer`, exercises the embedded-resource path) + the 23 ClaudePlayer tests. `weights_supervised.bin` is the always-available revert point.
- `Neural/checkpoints/` and `training_*.bin` are git-ignored by the **repo-root `.gitignore`** (added for this; the only `.gitignore` outside `src/`). The two shipped weight files are explicitly not ignored.

## Code style

StyleCop.Analyzers is enforced via `src/Rules.ruleset` + `src/stylecop.json`, applied through every csproj. The non-default rules that actually matter when editing:

- `using` directives go **inside** the namespace; `System.*` first; blank line between groups.
- Files must end with a newline.
- `companyName` is `Santase`. Documentation is **not** required on interfaces or internal elements.
- Two-letter Hungarian prefixes are explicitly whitelisted: `db at or up it un x y id ip bg am my`. Don't rename fields that use them.

## Known-stale artifacts in the repo

A few files in the repo describe an older state and were not updated when the UWP / Mobile Blazor Bindings / Android UI projects were removed (commit `262e270`):

- `README.md` still advertises the Windows Universal App (Microsoft Store) and the Mobile Blazor Bindings Android UI, and says **Visual Studio 2017** is required. The current solution file is tagged Visual Studio 18. The old UWP/Android UIs were removed in `262e270`; the UIs that remain are the Console UI (`Santase.UI.Console`) and a newer cross-platform **MAUI** desktop/mobile UI (`Santase.UI`, added after `262e270` in `2dec948`→`c5a2163`), both in `src/Santase.sln`.
- `azure-pipelines.yml` still builds the solution via `VSBuild` with UWP-specific MSBuild args (`AppxBundlePlatforms`, `AppxBundle=Always`, `UapAppxPackageBuildMode=StoreUpload`). With the UWP project gone, the pipeline is effectively dead until rewritten — assume CI is not currently green.

When working on related areas (CI, packaging, docs), check these against current reality before trusting them.
