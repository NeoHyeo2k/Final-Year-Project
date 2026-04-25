import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

import matplotlib.pyplot as plt
import numpy as np

try:
    from tensorboard.backend.event_processing.event_accumulator import EventAccumulator
except ImportError:
    EventAccumulator = None


WIN_RATE_TAG_CANDIDATES = (
    "Combat/WinRate",
    "FTGAgent/Combat/WinRate",
)

REWARD_TAG_CANDIDATES = (
    "Environment/Cumulative Reward",
    "FTGAgent/Environment/Cumulative Reward",
    "Policy/Extrinsic Reward",
    "FTGAgent/Policy/Extrinsic Reward",
)

LOG_SUMMARY_RE = re.compile(
    r"^\[RoundStats Summary\] Last (?P<window>\d+) rounds \| .* WinRate=(?P<win_rate>[0-9.]+)% "
)
SEED_RE = re.compile(r"seed(?P<seed>\d+)$")


@dataclass
class Curve:
    steps: np.ndarray
    values: np.ndarray


@dataclass
class RunCurves:
    run_name: str
    seed: Optional[int]
    win_rate: Optional[Curve]
    reward: Optional[Curve]


@dataclass
class LineSpec:
    label: str
    subdir: str
    run_prefix_template: str
    shift_steps: int
    color: str


LINE_SPECS = (
    LineSpec("Line 1 Scratch", "line1_scratch_ppo", "transfer_v1_line1_scratch_{env}_seed", 0, "#1f77b4"),
    LineSpec("Line 2 Transfer PPO", "line2_transfer_ppo", "transfer_v1_line2_finetune_{env}_seed", 1_000_000, "#ff7f0e"),
    LineSpec("Line 3 Transfer PPO-LSTM", "line3_transfer_ppo_lstm", "transfer_v1_line3_lstm_finetune_{env}_seed", 1_000_000, "#2ca02c"),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Plot transfer-learning win rate and reward curves.")
    parser.add_argument(
        "--results-root",
        default="results/transfer_learning",
        help="Root directory that contains line1_scratch_ppo, line2_transfer_ppo, and line3_transfer_ppo_lstm.",
    )
    parser.add_argument(
        "--output-dir",
        default="results/transfer_learning/plots",
        help="Directory where PNG plots will be written.",
    )
    parser.add_argument(
        "--envs",
        nargs="+",
        default=["env2", "env3"],
        help="Environment suffixes to plot.",
    )
    parser.add_argument(
        "--pretrain-steps",
        type=int,
        default=1_000_000,
        help="How many pretraining steps to add when creating aligned transfer plots.",
    )
    parser.add_argument(
        "--grid-points",
        type=int,
        default=400,
        help="Interpolation points used for mean/std aggregation.",
    )
    return parser.parse_args()


def try_load_event_curve(run_dir: Path, tag_candidates: Sequence[str]) -> Optional[Curve]:
    if EventAccumulator is None:
        return None

    event_files = sorted((run_dir / "FTGAgent").glob("events.out.tfevents.*"))
    if not event_files:
        return None

    latest_event = event_files[-1]
    accumulator = EventAccumulator(str(latest_event), size_guidance={"scalars": 0})
    accumulator.Reload()
    available_tags = set(accumulator.Tags().get("scalars", []))

    for tag in tag_candidates:
        if tag not in available_tags:
            continue
        events = accumulator.Scalars(tag)
        if not events:
            continue
        steps = np.array([event.step for event in events], dtype=float)
        values = np.array([event.value for event in events], dtype=float)
        return Curve(steps=steps, values=values)

    return None


def as_finite_float(value: object) -> Optional[float]:
    if value is None:
        return None
    try:
        parsed = float(value)
    except (TypeError, ValueError):
        return None
    if not np.isfinite(parsed):
        return None
    return parsed


def load_training_status(run_dir: Path) -> Optional[dict]:
    status_path = run_dir / "run_logs" / "training_status.json"
    if not status_path.exists():
        return None

    data = json.loads(status_path.read_text(encoding="utf-8"))
    ftg = data.get("FTGAgent")
    if not ftg:
        return None
    return ftg


def get_training_status_checkpoints(ftg: dict) -> List[dict]:
    checkpoints = ftg.get("checkpoints", [])
    if checkpoints:
        return checkpoints

    final_checkpoint = ftg.get("final_checkpoint")
    if not final_checkpoint:
        return []
    return [final_checkpoint]


def load_training_status_reward(run_dir: Path) -> Optional[Curve]:
    ftg = load_training_status(run_dir)
    if not ftg:
        return None

    steps = []
    rewards = []
    for checkpoint in get_training_status_checkpoints(ftg):
        step = as_finite_float(checkpoint.get("steps"))
        reward = as_finite_float(checkpoint.get("reward"))
        if step is None or reward is None:
            continue
        steps.append(step)
        rewards.append(reward)

    if not steps:
        return None

    return Curve(steps=np.array(steps, dtype=float), values=np.array(rewards, dtype=float))


def estimate_final_steps(run_dir: Path) -> Optional[float]:
    ftg = load_training_status(run_dir)
    if not ftg:
        return None

    steps = [
        step
        for checkpoint in get_training_status_checkpoints(ftg)
        for step in [as_finite_float(checkpoint.get("steps"))]
        if step is not None
    ]
    if steps:
        return max(steps)
    return None


def load_log_win_rate(run_dir: Path) -> Optional[Curve]:
    log_path = run_dir / "run_logs" / "Player-0.log"
    if not log_path.exists():
        return None

    win_rates: List[float] = []
    for line in log_path.read_text(encoding="utf-8", errors="ignore").splitlines():
        match = LOG_SUMMARY_RE.match(line.strip())
        if match:
            win_rates.append(float(match.group("win_rate")) / 100.0)

    if not win_rates:
        return None

    final_steps = estimate_final_steps(run_dir)
    if final_steps is None:
        # Fallback to a synthetic summary index axis when no step data is available.
        steps = np.arange(1, len(win_rates) + 1, dtype=float)
    else:
        steps = np.linspace(final_steps / len(win_rates), final_steps, len(win_rates), dtype=float)

    return Curve(steps=steps, values=np.array(win_rates, dtype=float))


def load_run_curves(run_dir: Path) -> RunCurves:
    run_name = run_dir.name
    seed_match = SEED_RE.search(run_name)
    seed = int(seed_match.group("seed")) if seed_match else None

    win_rate = try_load_event_curve(run_dir, WIN_RATE_TAG_CANDIDATES)
    if win_rate is None:
        win_rate = load_log_win_rate(run_dir)

    reward = try_load_event_curve(run_dir, REWARD_TAG_CANDIDATES)
    if reward is None:
        reward = load_training_status_reward(run_dir)

    return RunCurves(run_name=run_name, seed=seed, win_rate=win_rate, reward=reward)


def find_runs(results_root: Path, spec: LineSpec, env_name: str) -> List[Path]:
    line_root = results_root / spec.subdir
    if not line_root.exists():
        return []

    prefix = spec.run_prefix_template.format(env=env_name)
    return sorted(path for path in line_root.iterdir() if path.is_dir() and path.name.startswith(prefix))


def shift_curve(curve: Optional[Curve], shift_steps: float) -> Optional[Curve]:
    if curve is None:
        return None
    return Curve(steps=curve.steps + shift_steps, values=curve.values.copy())


def collect_line_curves(results_root: Path, spec: LineSpec, env_name: str, shift_override: Optional[int] = None) -> Dict[str, List[Curve]]:
    runs = [load_run_curves(path) for path in find_runs(results_root, spec, env_name)]
    shift_steps = spec.shift_steps if shift_override is None else shift_override

    win_rate_curves = []
    reward_curves = []
    for run in runs:
        if run.win_rate is not None:
            win_rate_curves.append(shift_curve(run.win_rate, shift_steps))
        if run.reward is not None:
            reward_curves.append(shift_curve(run.reward, shift_steps))

    return {"win_rate": win_rate_curves, "reward": reward_curves}


def interpolate_group(curves: Sequence[Curve], grid_points: int, x_min: Optional[float] = None, x_max: Optional[float] = None) -> Optional[Tuple[np.ndarray, np.ndarray, np.ndarray]]:
    if not curves:
        return None

    curve_min = min(float(curve.steps.min()) for curve in curves if curve.steps.size > 0)
    curve_max = max(float(curve.steps.max()) for curve in curves if curve.steps.size > 0)
    x_min = curve_min if x_min is None else max(curve_min, x_min)
    x_max = curve_max if x_max is None else min(curve_max, x_max)
    if x_max <= x_min:
        return None

    grid = np.linspace(x_min, x_max, grid_points, dtype=float)
    rows = []
    for curve in curves:
        left = float(curve.steps.min())
        right = float(curve.steps.max())
        mask = (grid >= left) & (grid <= right)
        if not mask.any():
            continue
        row = np.full_like(grid, np.nan, dtype=float)
        row[mask] = np.interp(grid[mask], curve.steps, curve.values)
        rows.append(row)

    if not rows:
        return None

    data = np.vstack(rows)
    mean = np.nanmean(data, axis=0)
    std = np.nanstd(data, axis=0)
    valid = ~np.isnan(mean)
    return grid[valid], mean[valid], std[valid]


def format_steps(x: float, _pos: object) -> str:
    if abs(x) >= 1_000_000:
        return f"{x / 1_000_000:.1f}M"
    if abs(x) >= 1_000:
        return f"{x / 1_000:.0f}k"
    return f"{int(x)}"


def plot_metric(
    output_path: Path,
    metric_name: str,
    title: str,
    line_curves: Dict[str, Dict[str, object]],
    grid_points: int,
    x_min: Optional[float] = None,
    x_max: Optional[float] = None,
) -> None:
    fig, ax = plt.subplots(figsize=(10, 6))

    any_series = False
    for label, payload in line_curves.items():
        curves = payload["curves"]
        color = payload["color"]
        aggregated = interpolate_group(curves, grid_points=grid_points, x_min=x_min, x_max=x_max)
        if aggregated is None:
            continue
        steps, mean, std = aggregated
        any_series = True
        ax.plot(steps, mean, label=label, color=color, linewidth=2)
        if len(curves) > 1:
            ax.fill_between(steps, mean - std, mean + std, color=color, alpha=0.2)

    if not any_series:
        plt.close(fig)
        return

    ax.set_title(title)
    ax.set_xlabel("Training Steps")
    ax.set_ylabel("Win Rate" if metric_name == "win_rate" else "Reward")
    ax.grid(True, alpha=0.3)
    ax.legend()
    ax.xaxis.set_major_formatter(format_steps)

    if metric_name == "win_rate":
        ax.set_ylim(0.0, 1.0)

    fig.tight_layout()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(output_path, dpi=200)
    plt.close(fig)


def build_payload(
    results_root: Path,
    env_name: str,
    metric_name: str,
    shift_transfer: bool,
    pretrain_steps: int,
) -> Dict[str, Dict[str, object]]:
    payload = {}
    for spec in LINE_SPECS:
        shift_override = None
        if spec.shift_steps != 0:
            shift_override = pretrain_steps if shift_transfer else 0
        curves = collect_line_curves(results_root, spec, env_name, shift_override=shift_override)[metric_name]
        payload[spec.label] = {"curves": curves, "color": spec.color}
    return payload


def main() -> None:
    args = parse_args()
    results_root = Path(args.results_root)
    output_dir = Path(args.output_dir)

    for env_name in args.envs:
        raw_win_payload = build_payload(results_root, env_name, "win_rate", shift_transfer=False, pretrain_steps=args.pretrain_steps)
        raw_reward_payload = build_payload(results_root, env_name, "reward", shift_transfer=False, pretrain_steps=args.pretrain_steps)
        aligned_win_payload = build_payload(results_root, env_name, "win_rate", shift_transfer=True, pretrain_steps=args.pretrain_steps)
        aligned_reward_payload = build_payload(results_root, env_name, "reward", shift_transfer=True, pretrain_steps=args.pretrain_steps)

        plot_metric(
            output_dir / f"{env_name}_win_rate_raw.png",
            "win_rate",
            f"{env_name.upper()} Win Rate Comparison (Raw Steps)",
            raw_win_payload,
            grid_points=args.grid_points,
        )
        plot_metric(
            output_dir / f"{env_name}_reward_raw.png",
            "reward",
            f"{env_name.upper()} Reward Comparison (Raw Steps)",
            raw_reward_payload,
            grid_points=args.grid_points,
        )
        plot_metric(
            output_dir / f"{env_name}_win_rate_aligned_1000k.png",
            "win_rate",
            f"{env_name.upper()} Win Rate Comparison (Transfer Shifted by {args.pretrain_steps:,} Steps)",
            aligned_win_payload,
            grid_points=args.grid_points,
            x_min=float(args.pretrain_steps),
        )
        plot_metric(
            output_dir / f"{env_name}_reward_aligned_1000k.png",
            "reward",
            f"{env_name.upper()} Reward Comparison (Transfer Shifted by {args.pretrain_steps:,} Steps)",
            aligned_reward_payload,
            grid_points=args.grid_points,
            x_min=float(args.pretrain_steps),
        )

    print(f"Plots written to: {output_dir}")


if __name__ == "__main__":
    main()
