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

$line1 = Join-Path $PSScriptRoot "run_line1_scratch.ps1"
$line2 = Join-Path $PSScriptRoot "run_line2_transfer.ps1"
$line3 = Join-Path $PSScriptRoot "run_line3_transfer_lstm.ps1"

Write-Host "===== Running Line 1: Scratch PPO ====="
& $line1 -Env2Path $Env2Path -Seeds $Seeds -RunTag $RunTag -ResultsDir $ResultsDir -NoGraphics:$NoGraphics -Force:$Force

Write-Host "===== Running Line 2: Transfer PPO ====="
& $line2 -Env1Path $Env1Path -Env2Path $Env2Path -Seeds $Seeds -RunTag $RunTag -ResultsDir $ResultsDir -NoGraphics:$NoGraphics -Force:$Force

Write-Host "===== Running Line 3: Transfer PPO+LSTM ====="
& $line3 -Env1Path $Env1Path -Env2Path $Env2Path -Seeds $Seeds -RunTag $RunTag -ResultsDir $ResultsDir -NoGraphics:$NoGraphics -Force:$Force

Write-Host "All experiment lines completed."
