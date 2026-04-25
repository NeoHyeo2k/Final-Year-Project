param(
    [string]$PythonExe = "python",
    [string]$RunTag = "transfer_v1",
    [int[]]$Seeds = @(0, 1, 2),
    [string]$ResultsRoot = "results/transfer_learning",
    [string]$Env1Exe = "E:\project\unity\fyp\Builds\Env1\fyp.exe",
    [string]$Env2Exe = "E:\project\unity\fyp\Builds\Env2\fyp.exe",
    [string]$Env3Exe = "E:\project\unity\fyp\Builds\Env3\fyp.exe",
    [switch]$NoGraphics = $true,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "../../..")
$configRoot = Join-Path $repoRoot "experiments/transfer_learning/configs"

function Invoke-Train {
    param(
        [string]$ConfigPath,
        [string]$RunId,
        [string]$EnvExe,
        [string]$ResultsDir,
        [int]$Seed,
        [string]$InitializeFrom = $null
    )

    $args = @(
        "-m", "mlagents.trainers.learn",
        $ConfigPath,
        "--run-id", $RunId,
        "--env", $EnvExe,
        "--seed", "$Seed",
        "--results-dir", $ResultsDir
    )

    if ($InitializeFrom) {
        $args += @("--initialize-from", $InitializeFrom)
    }
    if ($NoGraphics) {
        $args += "--no-graphics"
    }
    if ($Force) {
        $args += "--force"
    }

    Write-Host ">>> $RunId"
    & $PythonExe @args
    if ($LASTEXITCODE -ne 0) {
        throw "Training failed: $RunId"
    }
}

foreach ($seed in $Seeds) {
    Write-Host "===== Seed $seed ====="

    $line1Dir = Join-Path $repoRoot (Join-Path $ResultsRoot "line1_scratch_ppo")
    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line1_scratch_env2_2N.yaml") `
        -RunId "${RunTag}_line1_scratch_env2_seed${seed}" `
        -EnvExe $Env2Exe `
        -ResultsDir $line1Dir `
        -Seed $seed

    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line1_scratch_env3_2N.yaml") `
        -RunId "${RunTag}_line1_scratch_env3_seed${seed}" `
        -EnvExe $Env3Exe `
        -ResultsDir $line1Dir `
        -Seed $seed

    $line2Dir = Join-Path $repoRoot (Join-Path $ResultsRoot "line2_transfer_ppo")
    $line2PretrainRunId = "${RunTag}_line2_pretrain_env1_seed${seed}"
    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line2_pretrain_env1_N.yaml") `
        -RunId $line2PretrainRunId `
        -EnvExe $Env1Exe `
        -ResultsDir $line2Dir `
        -Seed $seed

    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line2_finetune_env2_N.yaml") `
        -RunId "${RunTag}_line2_finetune_env2_seed${seed}" `
        -EnvExe $Env2Exe `
        -ResultsDir $line2Dir `
        -Seed $seed `
        -InitializeFrom $line2PretrainRunId

    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line2_finetune_env3_N.yaml") `
        -RunId "${RunTag}_line2_finetune_env3_seed${seed}" `
        -EnvExe $Env3Exe `
        -ResultsDir $line2Dir `
        -Seed $seed `
        -InitializeFrom $line2PretrainRunId

    $line3Dir = Join-Path $repoRoot (Join-Path $ResultsRoot "line3_transfer_ppo_lstm")
    $line3PretrainRunId = "${RunTag}_line3_lstm_pretrain_env1_seed${seed}"
    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line3_lstm_pretrain_env1_N.yaml") `
        -RunId $line3PretrainRunId `
        -EnvExe $Env1Exe `
        -ResultsDir $line3Dir `
        -Seed $seed

    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line3_lstm_finetune_env2_N.yaml") `
        -RunId "${RunTag}_line3_lstm_finetune_env2_seed${seed}" `
        -EnvExe $Env2Exe `
        -ResultsDir $line3Dir `
        -Seed $seed `
        -InitializeFrom $line3PretrainRunId

    Invoke-Train `
        -ConfigPath (Join-Path $configRoot "line3_lstm_finetune_env3_N.yaml") `
        -RunId "${RunTag}_line3_lstm_finetune_env3_seed${seed}" `
        -EnvExe $Env3Exe `
        -ResultsDir $line3Dir `
        -Seed $seed `
        -InitializeFrom $line3PretrainRunId
}

Write-Host "All requested seeds completed."
