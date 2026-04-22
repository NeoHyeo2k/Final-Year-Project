param(
    [Parameter(Mandatory = $true)]
    [string]$Env1Path,

    [Parameter(Mandatory = $true)]
    [string]$Env2Path,

    [int[]]$Seeds = @(0, 1, 2, 3, 4),
    [string]$RunTag = "transfer_v1",
    [string]$ResultsDir = "results/transfer_learning",
    [switch]$NoGraphics = $true,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "../../..")
$pretrainConfig = Join-Path $repoRoot "experiments/transfer_learning/configs/line2_pretrain_env1_N.yaml"
$finetuneConfig = Join-Path $repoRoot "experiments/transfer_learning/configs/line2_finetune_env2_N.yaml"
$baseResultsDir = if ([System.IO.Path]::IsPathRooted($ResultsDir)) { $ResultsDir } else { Join-Path $repoRoot $ResultsDir }
$lineResultsDir = Join-Path $baseResultsDir "line2_transfer_ppo"

foreach ($seed in $Seeds) {
    $pretrainRunId = "${RunTag}_line2_pretrain_env1_seed${seed}"
    $finetuneRunId = "${RunTag}_line2_finetune_env2_seed${seed}"

    $pretrainArgs = @(
        $pretrainConfig,
        "--run-id", $pretrainRunId,
        "--env", $Env1Path,
        "--seed", "$seed",
        "--results-dir", $lineResultsDir
    )
    if ($NoGraphics) {
        $pretrainArgs += "--no-graphics"
    }
    if ($Force) {
        $pretrainArgs += "--force"
    }

    Write-Host "=== Line 2 PRETRAIN | Seed $seed | RunId: $pretrainRunId ==="
    & mlagents-learn @pretrainArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Pretrain failed for run-id: $pretrainRunId"
    }

    $finetuneArgs = @(
        $finetuneConfig,
        "--run-id", $finetuneRunId,
        "--env", $Env2Path,
        "--seed", "$seed",
        "--results-dir", $lineResultsDir,
        "--initialize-from", $pretrainRunId
    )
    if ($NoGraphics) {
        $finetuneArgs += "--no-graphics"
    }
    if ($Force) {
        $finetuneArgs += "--force"
    }

    Write-Host "=== Line 2 FINETUNE | Seed $seed | RunId: $finetuneRunId | init: $pretrainRunId ==="
    & mlagents-learn @finetuneArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Finetune failed for run-id: $finetuneRunId"
    }
}
