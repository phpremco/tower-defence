#!/usr/bin/env python3
"""Why runs are lost, from pressure records (round 82b; the critic's "killer wave and standing guns").

    python Tools/PlayTest/pressure_killers.py lost <records> [--multiple 1]
        every lost run at that multiple: the wave it fell in, the base damage per wave (field 21), the wave that took
        the most, the gate damage of the last waves (field 22) and the guns standing at that wave, by type and level
        (the builds, upgrades and sales of field 24 up to and including that wave).
    python Tools/PlayTest/pressure_killers.py boss <records> <records before> [--difficulty 1]
        per level and rung, at x1 and x1.25: seeds won, the last wave's mean gate and base damage of the runs that
        reached it, and the mean base left, before -> after (how a level's boss bites as played).
    python Tools/PlayTest/pressure_killers.py breakeven <records> [--policies skilled,sensible]
        per cell and rung the seeds won at every multiple, the break-even (the highest multiple at which two of three
        seeds win) and "fragile" where the row is not monotone.

Reads the records as they are; it does not check stamps (compare files with pressure_wins.py first).
"""
import statistics
import sys
from collections import defaultdict

LEVELS = {"Level_C1_01": "Landing Site", "Level_C1_02": "Glass Meadows", "Level_C1_03": "Shard Ridge",
          "Level_C1_04": "Fault Line", "Level_C1_05": "Quartz Flats", "Level_C1_06": "Crysium Vein"}
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]
MULTIPLES = [0.8, 0.9, 1.0, 1.12, 1.25]
SEEDS = (1, 2, 3)
LAST_WAVES = 3


def per_wave(text):
    out = {}
    for part in text.split(";"):
        if "=" in part:
            wave, value = part.split("=", 1)
            out[int(wave)] = value
    return out


def load(path):
    runs = {}
    for line in open(path, encoding="utf-8"):
        f = line.rstrip("\n").split("|")
        if f[0] != "prun" or len(f) < 25:
            continue
        run = dict(level=f[1], d=int(f[2]), policy=f[3], seed=int(f[4]), m=float(f[5]), won=f[6] == "won",
                   wave=int(f[7]), waves=int(f[8]), base=float(f[9]), max=float(f[10]),
                   base_lost={w: float(v) for w, v in per_wave(f[20]).items()},
                   gate_lost={w: float(v) for w, v in per_wave(f[21]).items()}, builds=f[23])
        runs[(run["level"], run["d"], run["policy"], run["seed"], run["m"])] = run
    return runs


def guns(run, wave):
    """The guns standing after the buys of `wave`, as 'Tower 3/2/1' per type."""
    towers = {}
    for entry in run["builds"].split(";"):
        q = entry.split(":")
        if len(q) < 5 or int(q[4]) > wave:
            continue
        spot = (q[2], q[3])
        if q[0] == "build":
            towers[spot] = [q[1], 1]
        elif q[0] == "upgrade" and spot in towers:
            towers[spot][1] += 1
        elif q[0] == "sell":
            towers.pop(spot, None)
    by_type = defaultdict(list)
    for name, level in towers.values():
        by_type[name].append(level)
    return ", ".join(f"{name} {'/'.join(str(v) for v in sorted(levels, reverse=True))}" for name, levels in sorted(by_type.items()))


def option(name, default):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def lost(runs):
    multiple = float(option("--multiple", "1"))
    for key in sorted(runs):
        run = runs[key]
        if run["won"] or run["m"] != multiple:
            continue
        fell = run["wave"]
        killer = max(run["base_lost"], key=lambda w: run["base_lost"][w]) if run["base_lost"] else fell
        base = " ".join(f"w{w}:{v:.0f}" for w, v in sorted(run["base_lost"].items()))
        gates = " ".join(f"w{w}:{v:.0f}" for w, v in sorted(run["gate_lost"].items()) if w >= fell - LAST_WAVES)
        print(f"{LEVELS.get(run['level'], run['level'])} {DIFFS[run['d']]} {run['policy']} s{run['seed']}: fell w{fell}/{run['waves']}; "
              f"base damage {base} (of {run['max']:.0f}); most in w{killer}; gate damage, last waves: {gates}; "
              f"guns at w{killer}: {guns(run, killer)}" + ("" if killer == fell else f"; at w{fell}: {guns(run, fell)}"))


def boss(after, before):
    difficulty = int(option("--difficulty", "1"))
    for level in sorted(LEVELS):
        for policy in ("skilled", "sensible"):
            for multiple in (1.0, 1.25):
                cells = []
                for source in (before, after):
                    runs = [source[(level, difficulty, policy, s, multiple)] for s in SEEDS if (level, difficulty, policy, s, multiple) in source]
                    if not runs:
                        cells.append("no runs")
                        continue
                    reached = [r for r in runs if r["wave"] >= r["waves"]]
                    gate = statistics.mean(r["gate_lost"].get(r["waves"], 0.0) for r in reached) if reached else float("nan")
                    base = statistics.mean(r["base_lost"].get(r["waves"], 0.0) for r in reached) if reached else float("nan")
                    left = statistics.mean(r["base"] for r in runs)
                    cells.append(f"won {sum(r['won'] for r in runs)}/{len(runs)}, last wave gate {gate:.0f} base {base:.0f}, base left {left:.0f}/{runs[0]['max']:.0f}")
                print(f"{LEVELS[level]} {DIFFS[difficulty]} {policy} x{multiple:g}: {cells[0]} -> {cells[1]}")


def breakeven(runs):
    policies = option("--policies", "skilled,sensible").split(",")
    for level in sorted(LEVELS):
        for d in range(len(DIFFS)):
            parts = []
            for policy in policies:
                wins = [sum(runs[(level, d, policy, s, m)]["won"] for s in SEEDS if (level, d, policy, s, m) in runs) for m in MULTIPLES]
                held = [m for m, w in zip(MULTIPLES, wins) if w >= 2]
                even = "under x0.8" if not held else f"x{max(held):g}" + ("+" if max(held) == MULTIPLES[-1] else "")
                monotone = all(wins[i] >= wins[i + 1] for i in range(len(wins) - 1))
                parts.append(f"{policy} {' '.join(map(str, wins))} break-even {even}{'' if monotone else ' fragile'}")
            print(f"{LEVELS[level]} {DIFFS[d]}: " + " | ".join(parts))


def main():
    args = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and not sys.argv[i - 1].startswith("--")]
    if len(args) < 2 or args[0] not in ("lost", "boss", "breakeven") or (args[0] == "boss" and len(args) < 3):
        sys.exit(__doc__)
    if args[0] == "lost":
        lost(load(args[1]))
    elif args[0] == "boss":
        boss(load(args[1]), load(args[2]))
    else:
        breakeven(load(args[1]))


if __name__ == "__main__":
    main()
