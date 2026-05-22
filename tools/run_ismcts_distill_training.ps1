#Requires -Version 7
<#
.SYNOPSIS
  ISMCTS-distillation training pipeline for ClaudePlayerNeural.

  Replaces the heuristic teacher with ClaudePlayerIsmcts (the strongest player in the repo) and
  distills its *root visit distribution* (an AlphaZero-style soft target) into the MLP, then PPO
  fine-tunes from that warm-start. Stages:
    0. ISMCTS self-play -> soft-target dataset (--gen-distill-data)
    1. supervised soft-target clone -> distilled warm-start (--supervised --soft)
    1b. early read: validate the warm-start vs the heuristic
    2. PPO fine-tune from the warm-start (the headline ~9h run)
    3. final validation of {production, distilled warm-start, PPO best} vs the heuristic

  Promotion is MANUAL (matching the repo workflow): this script never overwrites the shipped
  Neural/weights.bin. It only produces candidates + validation numbers under tools/NeuralTrainer/_scratch.
#>
param(
    [int]$Games = 5000,
    [int]$BudgetMs = 60,
    [int]$SupervisedEpochs = 20,
    [double]$PpoHours = 9.0,
    [int]$ValidateGames = 50000
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

$sim     = 'src/Tests/Santase.Tests.GameSimulations/Santase.Tests.GameSimulations.csproj'
$trainer = 'tools/NeuralTrainer/NeuralTrainer.csproj'
$scratch = 'tools/NeuralTrainer/_scratch'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null

$dataset   = "$scratch/ismcts_distill.bin"
$warmstart = "$scratch/weights_distilled_ismcts.bin"
$ckpt      = "$scratch/checkpoints_ismcts"
$best      = "$ckpt/weights.best.bin"
$prod      = 'src/AI/Santase.AI.ClaudePlayer/Neural/weights.bin'

function Step($msg) { Write-Host "`n===== $msg  [$(Get-Date -Format o)] =====" }
function Die($msg)  { Write-Error $msg; exit 1 }

Step "Pipeline start: Games=$Games BudgetMs=$BudgetMs SupervisedEpochs=$SupervisedEpochs PpoHours=$PpoHours ValidateGames=$ValidateGames"

Step 'Build (Release)'
dotnet build src/Santase.sln -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Die 'solution build failed' }
dotnet build $trainer -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Die 'trainer build failed' }

Step "Stage 0: ISMCTS distillation data-gen ($Games games @ ${BudgetMs}ms/move)"
dotnet run -c Release --no-build --project $sim -- --gen-distill-data $Games $dataset $BudgetMs
if ($LASTEXITCODE -ne 0) { Die 'data-gen failed' }

Step "Stage 1: supervised soft-target clone ($SupervisedEpochs epochs) -> $warmstart"
dotnet run -c Release --no-build --project $trainer -- --supervised --soft --data $dataset --out $warmstart --epochs $SupervisedEpochs
if ($LASTEXITCODE -ne 0) { Die 'supervised clone failed' }

Step "Stage 1b: validate distilled warm-start vs heuristic ($ValidateGames games) -- early read before the 9h PPO"
dotnet run -c Release --no-build --project $trainer -- --validate $warmstart $ValidateGames

Step "Stage 2: PPO fine-tune ($PpoHours h) from distilled warm-start -> $ckpt"
dotnet run -c Release --no-build --project $trainer -- --ppo --in $warmstart --out $ckpt --hours $PpoHours
if ($LASTEXITCODE -ne 0) { Die 'PPO failed' }

Step "Stage 3: final validation @ $ValidateGames games vs heuristic"
Write-Host '--- current production weights.bin ---'
dotnet run -c Release --no-build --project $trainer -- --validate $prod $ValidateGames
Write-Host '--- distilled warm-start (pre-PPO) ---'
dotnet run -c Release --no-build --project $trainer -- --validate $warmstart $ValidateGames
if (Test-Path $best) {
    Write-Host '--- PPO best (post-fine-tune) ---'
    dotnet run -c Release --no-build --project $trainer -- --validate $best $ValidateGames
} else {
    Write-Host "No PPO best checkpoint at $best (PPO may have early-stopped before first eval)."
}

Step 'DONE'
Write-Host "Candidates:"
Write-Host "  distilled warm-start : $warmstart"
Write-Host "  PPO best             : $best"
Write-Host "Promotion is MANUAL: if a candidate beats production by a real margin, copy it over"
Write-Host "  $prod"
Write-Host "then rebuild Santase.AI.ClaudePlayer (re-embeds the resource) and re-run the 'claude' sim suite."
