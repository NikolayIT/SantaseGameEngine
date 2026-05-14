# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository overview

C# implementation of the **Santase / 66 / Schnapsen / Sechsundsechzig** two-player trick-taking card game. The engine (`Santase.Logic`) is shipped as the [SantaseGameEngine NuGet package](https://www.nuget.org/packages/SantaseGameEngine); the rest of the repo is AI players, UIs, and a large simulator harness used to benchmark AI strength.

Solution lives in `src/Santase.sln`. All NuGet/dotnet commands below assume you `cd src` first.

## Build, test, run

```powershell
# Restore + build the whole solution (Release recommended for the simulator)
dotnet build src\Santase.sln -c Release

# Unit tests (xUnit). Two test projects, both .NET Standard 2.0:
dotnet test src\Tests\Santase.Logic.Tests\Santase.Logic.Tests.csproj
dotnet test src\Tests\Santase.AI.SmartPlayer.Tests\Santase.AI.SmartPlayer.Tests.csproj

# Run a single test
dotnet test src\Tests\Santase.Logic.Tests\Santase.Logic.Tests.csproj --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run the AI benchmark / game simulator (netcoreapp3.1 console app — runs 4 x 200,000 games)
dotnet run -c Release --project src\Tests\Santase.Tests.GameSimulations\Santase.Tests.GameSimulations.csproj

# Run the human-playable Console UI (net5.0)
dotnet run --project src\UI\Santase.UI.Console\Santase.UI.Console.csproj
```

Notes on platform-specific projects:

- `Santase.UI.WindowsUniversal` (UWP) and `Santase.UI.Android` (Mobile Blazor Bindings + Xamarin.Forms) only build on Windows with the matching workloads installed. CI (`azure-pipelines.yml`) builds the whole solution via `VSBuild` on `windows-latest`; `dotnet build` of the .sln will fail to build these two projects on plain SDK installs — build the specific projects you need instead of the whole solution if you hit that.
- `Santase.Logic`, `Santase.AI.DummyPlayer`, `Santase.AI.SmartPlayer`, and the test projects all target `netstandard2.0`; the simulator is `netcoreapp3.1`; the console UI is `net5.0`. Don't "upgrade" a target framework without a reason — `Santase.Logic` is intentionally `netstandard2.0` because it is a published NuGet package, and the simulator pins `netcoreapp3.1` because that's the framework the AI players are compiled and benchmarked against.

## Architecture

### Layering (dependencies flow downward)

```
UIs (Console / WindowsUniversal / Android+Blazor)   Tests / GameSimulations
                       │                                      │
                       ▼                                      ▼
              AI players (SmartPlayer, DummyPlayer, External *.dll)
                       │
                       ▼
                  Santase.Logic   ← the engine, the NuGet package
```

`Santase.Logic` has zero dependencies beyond StyleCop. AI players depend only on `Santase.Logic`. Anything UI-shaped or simulator-shaped lives above the AI layer. **Never let `Santase.Logic` take a dependency on an AI or UI project** — it would break the NuGet package's surface.

### Engine core (`src/Santase.Logic`)

The engine is driven by a single public entry point: `SantaseGame` in `GameMechanics/SantaseGame.cs`. You hand it two `IPlayer` instances and call `Start()`; it runs rounds until someone reaches `GamePointsNeededForWin` (default 11, from `SantaseGameRules`).

Key concepts to keep in mind when editing the engine:

- **`IPlayer` is the only seam for AI.** `Players/IPlayer.cs` defines the player contract: `StartGame` / `StartRound` / `AddCard` / `GetTurn(PlayerTurnContext)` / `EndTurn` / `EndRound` / `EndGame`. Every AI inherits from `BasePlayer`, which provides `ChangeTrump`, `PlayCard`, and `CloseGame` helpers and holds the `Cards` collection. Adding new player capabilities means changing this interface and rippling through every AI implementation — do that intentionally, not casually.
- **Round state machine** (`RoundStates/`). The two phases of Santase (talon still has cards / closed or empty) are modeled with the State pattern: `StartRoundState` → `MoreThanTwoCardsLeftRoundState` → `TwoCardsLeftRoundState` → `FinalRoundState`, coordinated by `StateManager`. `BaseRoundState` exposes booleans like `ShouldObserveRules`, `CanClose`, `CanChangeTrump`, `CanAnnounce20Or40`, `ShouldDrawCard` — these flags are how the rest of the engine and AIs know which phase they're in. If you add a phase-dependent rule, add the flag here rather than scattering `if (cardsLeftInDeck == ...)` checks.
- **`PlayerActionValidate/`** is the single source of truth for what moves are legal in a given `PlayerTurnContext`. AIs that propose illegal moves should be caught here. Both `IAnnounceValidator` and `IPlayerActionValidator` are exposed as singletons via `.Instance`.
- **`GameMechanics/Round.cs`** and **`Trick.cs`** are `internal` — they're implementation detail of `SantaseGame`. They are made visible to the test projects via `InternalsVisibleToContainer.cs` + `[assembly: InternalsVisibleTo]`. Don't make them public to "test something easily"; add the assembly to the InternalsVisibleTo list instead.
- **`PlayerTurnContext`** is what the engine passes to AIs each turn. It implements `IDeepCloneable<PlayerTurnContext>` because AIs (notably `SmartPlayer`) do lookahead / simulation and need to mutate copies safely. Preserve `DeepClone` if you add fields.
- **Game rules are pluggable** via `IGameRules` (`SantaseGameRules` is the default, exposed through `GameRulesProvider.Santase`). To prototype rule variants (different winning score, different round-points threshold), implement `IGameRules` rather than editing `SantaseGameRules`.

### AI architecture (`src/AI`)

- **`Santase.AI.DummyPlayer`** — baseline random/legal players used as a punching bag in simulations.
- **`Santase.AI.SmartPlayer`** — the real AI. The interesting structure is in `Strategies/`: `ChooseBestCardToPlayStrategy` dispatches into one of four sub-strategies based on `(IsFirstPlayerTurn, ShouldObserveRules)`:
  - `PlayingFirstAndRulesDoNotApplyStrategy` / `PlayingFirstAndRulesApplyStrategy`
  - `PlayingSecondAndRulesDoNotApplyStrategy` / `PlayingSecondAndRulesApplyStrategy`

  This 2×2 split mirrors the game's two phases × leader-vs-follower and is the right place to add new heuristics. `Helpers/CardTracker.cs` maintains the SmartPlayer's belief about which cards remain unseen — keep it consistent with `StartRound` / `AddCard` / `EndTurn` whenever you touch card flow.
- **`AI/External/*.dll`** — third-party AI implementations (Botsko, Ninja, Pro) used by the simulator as opponents. Source is not in this repo; they are referenced as binary `<Reference>` items.

### Simulator (`src/Tests/Santase.Tests.GameSimulations`)

A console app, not an xUnit project (despite living under `Tests/`). `Program.cs` runs four `IGameSimulator` workloads (each plays 200,000 games) and prints win/round-point totals plus the counters from `Santase.AI.SmartPlayer.GlobalStats`. This is the primary tool for evaluating whether an AI change is an improvement — when modifying `SmartPlayer` or its strategies, run the simulator in **Release** and compare game/point deltas against the previous numbers in the latest commit message (commits embed simulator output as the regression baseline).

### UIs

- `Santase.UI.Console` — straightforward .NET 5 console game; `ConsolePlayer` is an `IPlayer` that prompts on stdin.
- `Santase.UI` + `Santase.UI.Android` — Mobile Blazor Bindings + Xamarin.Forms shared/Android pair. Razor components (`GameScreen.razor`, `CardImage.razor`) live in `Santase.UI`; the Android head wires them up.
- `Santase.UI.WindowsUniversal` — UWP app published to the Microsoft Store. Old-style `.csproj` (not SDK-style); pinned to UAP `10.0.19041.0`. Only builds inside Visual Studio with the UWP workload.

## Code style

StyleCop.Analyzers is enforced via `src/Rules.ruleset` + `src/stylecop.json`, applied to every C# project. The non-default rules that matter when editing code:

- `using` directives go **inside** the namespace, `System.*` first, with a blank line between groups (`orderingRules` in `stylecop.json`).
- Files must end with a newline.
- `companyName` is `Santase`; documentation rules are relaxed (interfaces and internals do not need XML docs).
- Hungarian-style two-letter field prefixes are explicitly allowed (`db`, `at`, `or`, `up`, `it`, `un`, `x`, `y`, `id`, `ip`, `bg`, `am`, `my`). Don't fight existing names that use them.

## Useful pointers

- Game rules reference: `docs/Rules.md` (full rules text, including announce/close mechanics).
- The CI build is the Azure DevOps pipeline at `nikolayit.visualstudio.com/SantaseGameEngine` (definition in `azure-pipelines.yml`). It targets the UWP packaging path, so it builds the whole solution.
- The Logic library claims 100% test coverage and ~250 tests in `Santase.Logic.Tests`. Changes to `Santase.Logic` should keep that intact — add tests alongside any new branch in engine code.
