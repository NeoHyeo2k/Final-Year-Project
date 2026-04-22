param(
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
$configPath = Join-Path $repoRoot "experiments/transfer_learning/configs/line1_scratch_env2_2N.yaml"
$baseResultsDir = if ([System.IO.Path]::IsPathRooted($ResultsDir)) { $ResultsDir } else { Join-Path $repoRoot $ResultsDir }
$lineResultsDir = Join-Path $baseResultsDir "line1_scratch_ppo"

foreach ($seed in $Seeds) {
    $runId = "${RunTag}_line1_scratch_env2_seed${seed}"

    $args = @(
        $configPath,
        "--run-id", $runId,
        "--env", $Env2Path,
        "--seed", "$seed",
        "--results-dir", $lineResultsDir
    )

    if ($NoGraphics) {
        $args += "--no-graphics"
    }
    if ($Force) {
        $args += "--force"
    }

    Write-Host "=== Line 1 | Seed $seed | RunId: $runId ==="
    & mlagents-learn @args
    if ($LASTEXITCODE -ne 0) {
        throw "mlagents-learn failed for run-id: $runId"
    }
}
