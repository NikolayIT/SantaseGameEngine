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

# Run the unit tests via CLI (xunit, 255 + 2 tests).
dotnet test src\Santase.sln -c Release
```

### Unit tests

`Santase.Logic.Tests` and `Santase.AI.SmartPlayer.Tests` are xUnit test projects targeting `net10.0` with `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` referenced — they run via both `dotnet test` and Visual Studio's Test Explorer. Logic.Tests has 255 cases; SmartPlayer.Tests has 2.

Tests in `Santase.Tests.GameSimulations/Tests/` (the `*LoggerTests.cs` files) live inside the simulator's `Exe` project and are not invoked by the simulator's `Main` or by `dotnet test` (the simulator csproj is `OutputType=Exe`, not a test SDK project) — they're VS-Test-Explorer artifacts.

### Platform notes

- Every project targets `net10.0`. Before the .NET 10 migration the library + AI projects were `netstandard2.0`, the simulator was `netcoreapp3.1`, and the console UI was `net5.0` — recent commits in `git log` still reference those frameworks if you need to compare.
- `Santase.Logic` is the published [SantaseGameEngine](https://www.nuget.org/packages/SantaseGameEngine) NuGet package — bumping its TFM is a breaking change for downstream consumers (current package version is `3.0.0`, post-migration).
- The third-party AI players in `src/AI/External/*.dll` (`BotskoPlayer`, `NinjaPlayer`, `ProPlayer`) are binary references — no source. They're `.NETPortable` (PCL) assemblies, which load fine from `net10.0`. Treat their `IPlayer` contract as load-bearing for the simulator.

## Architecture

### Layering (dependencies flow downward)

```
        Santase.UI.Console            Santase.Tests.GameSimulations (+ unit tests)
                  │                                        │
                  └──────────────┬─────────────────────────┘
                                 ▼
              AI players (SmartPlayer, DummyPlayer, External *.dll)
                                 │
                                 ▼
                          Santase.Logic   ← the engine, the NuGet package
```

`Santase.Logic` has zero non-StyleCop dependencies. AI projects depend only on `Santase.Logic`. **Never let `Santase.Logic` take a dependency on an AI or UI project** — it would corrupt the NuGet package's public surface.

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

### Simulator (`src/Tests/Santase.Tests.GameSimulations`)

A `net10.0` console app (not an xUnit project, despite its location). `Program.cs` runs four `IGameSimulator` workloads — each plays 200,000 games — via `Parallel.For(MaxDegreeOfParallelism = Environment.ProcessorCount)` in `BaseGameSimulator`, then prints win counts, round-point totals, and `GlobalStats` counters.

**This is the canonical regression check for AI changes.** Commit messages in this repo embed the simulator output as a baseline (e.g., commit `8c5084c`). Workflow for AI work:

1. Run the simulator on `master` in Release; record the four blocks of output.
2. Make the AI change.
3. Re-run in Release; compare game/round-point deltas against the recorded baseline.
4. Paste the new output into the commit message (matching prior style) so the next person has a baseline too.

## Code style

StyleCop.Analyzers is enforced via `src/Rules.ruleset` + `src/stylecop.json`, applied through every csproj. The non-default rules that actually matter when editing:

- `using` directives go **inside** the namespace; `System.*` first; blank line between groups.
- Files must end with a newline.
- `companyName` is `Santase`. Documentation is **not** required on interfaces or internal elements.
- Two-letter Hungarian prefixes are explicitly whitelisted: `db at or up it un x y id ip bg am my`. Don't rename fields that use them.

## Known-stale artifacts in the repo

A few files in the repo describe an older state and were not updated when the UWP / Mobile Blazor Bindings / Android UI projects were removed (commit `262e270`):

- `README.md` still advertises the Windows Universal App (Microsoft Store) and the Mobile Blazor Bindings Android UI, and says **Visual Studio 2017** is required. The current solution file is tagged Visual Studio 18, and only the Console UI remains.
- `azure-pipelines.yml` still builds the solution via `VSBuild` with UWP-specific MSBuild args (`AppxBundlePlatforms`, `AppxBundle=Always`, `UapAppxPackageBuildMode=StoreUpload`). With the UWP project gone, the pipeline is effectively dead until rewritten — assume CI is not currently green.

When working on related areas (CI, packaging, docs), check these against current reality before trusting them.
