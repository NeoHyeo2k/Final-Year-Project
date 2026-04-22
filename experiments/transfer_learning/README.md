# Transfer Learning Experiments (Unity ML-Agents PPO)

This folder provides a reproducible setup for 3 experiment lines under a fixed budget:

- `N = 1,000,000` steps
- `2N = 2,000,000` steps

## Experiment Lines

1. `Line1_Scratch_PPO`  
Train PPO directly in env2 for `2N` steps.

2. `Line2_Transfer_PPO`  
Pretrain PPO in env1 for `N`, then transfer-all fine-tune in env2 for `N`.

3. `Line3_Transfer_PPO_LSTM`  
Pretrain PPO+LSTM in env1 for `N`, then transfer-all fine-tune in env2 for `N`.

## Directory Layout

```text
experiments/transfer_learning/
  configs/
    line1_scratch_env2_2N.yaml
    line2_pretrain_env1_N.yaml
    line2_finetune_env2_N.yaml
    line3_lstm_pretrain_env1_N.yaml
    line3_lstm_finetune_env2_N.yaml
  scripts/
    run_line1_scratch.ps1
    run_line2_transfer.ps1
    run_line3_transfer_lstm.ps1
    run_all.ps1
```

## Prerequisites

1. Install `mlagents` CLI in your Python environment.
2. Build Unity executables for each environment variant, for example:
   - `Builds/Env1/FightingEnv1.exe`
   - `Builds/Env2/FightingEnv2.exe`
3. Confirm Behavior Name in Unity is `FTGAgent` (matches YAML).

## Quick Start

Run all lines with multiple seeds:

```powershell
pwsh experiments/transfer_learning/scripts/run_all.ps1 `
  -Env1Path "E:\project\unity\fyp\Builds\Env1\FightingEnv1.exe" `
  -Env2Path "E:\project\unity\fyp\Builds\Env2\FightingEnv2.exe" `
  -Seeds @(0,1,2,3,4) `
  -RunTag "transfer_v1"
```

Existing run IDs are not overwritten by default. Add `-Force` only when you intentionally want to replace previous results.

## Logging and Metrics

From TensorBoard (ML-Agents default):
- `Environment/Cumulative Reward` -> average reward
- `Losses/Policy Loss`, `Losses/Value Loss` -> training stability
- `Policy/Entropy` -> action entropy
- `Environment/Episode Length` -> episode length

From `RoundStatsTracker` via ML-Agents `StatsRecorder`:
- `Combat/WinRate`
- `Combat/HitRate`
- `Combat/WhiffRate`
- `Combat/AvgRoundDuration`
- `Combat/AvgHitsDealt`
- `Combat/AvgHitsTaken`
- `Combat/HitDelta`
- `Combat/AvgAttackAttempts`
- `Combat/AvgWhiffs`

Recommended run grouping:
- `results/transfer_learning/line1_scratch_ppo/...`
- `results/transfer_learning/line2_transfer_ppo/...`
- `results/transfer_learning/line3_transfer_ppo_lstm/...`

For stability analysis across seeds:
- Compute mean/std for final-window win rate and reward
- Plot learning curves with shaded std region per line

## Fairness Notes

- Line1 vs Line2 changes only `Scratch vs Transfer`.
- Line2 vs Line3 changes only `Network Architecture (feedforward vs LSTM)`.
- Transfer uses full fine-tuning (`--initialize-from` only, no freezing).
