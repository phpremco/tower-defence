#!/usr/bin/env python3
"""The win table of a pressure measurement as Markdown (round 76): per cell the seeds won at every multiple, for the skilled
and the sensible rung side by side, and per cell the wave the lost runs ended in.

    python Tools/PlayTest/pressure_table.py <records> [--mixed-bots]
    python Tools/PlayTest/pressure_table.py <records> --cells 3:1,3:2,5:1      # only these node:difficulty cells

One records file, one bot (pressure_stamp.py); the last record of a unit and multiple counts.
"""
import sys
from collections import defaultdict

import pressure_stamp
from pressure_wins import DIFFICULTIES, LEVEL_NAMES, load

LEVELS = sorted(LEVEL_NAMES)
RUNGS = ["skilled", "sensible"]


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    runs = load(args[0])
    pressure_stamp.check_one(runs, args[0], pressure_stamp.ALLOW_FLAG in sys.argv)
    only = None
    if "--cells" in sys.argv:
        only = {(LEVELS[int(c.split(":")[0])], int(c.split(":")[1])) for c in sys.argv[sys.argv.index("--cells") + 1].split(",")}
        args = [a for a in args if ":" not in a]
    multiples = sorted({r["m"] for r in runs})
    wins = defaultdict(lambda: [0, 0])
    fell = defaultdict(list)
    for r in runs:
        key = (r["policy"], r["level"], r["d"], r["m"])
        wins[key][0] += r["won"]
        wins[key][1] += 1
        if not r["won"]:
            fell[(r["policy"], r["level"], r["d"])].append(r["wave"])
    cells = sorted({(r["level"], r["d"]) for r in runs if only is None or (r["level"], r["d"]) in only})
    seeds = sorted({r["seed"] for r in runs})
    print(f"Measured with: {pressure_stamp.describe(runs)}")
    print(f"Seeds won out of {len(seeds)} (seeds {', '.join(str(s) for s in seeds)}) at x" + " / x".join(f"{m:g}" for m in multiples) + " of the creatures' health.")
    print()
    print("| Cell | " + " | ".join(f"{rung.capitalize()}" for rung in RUNGS) + " | " + " | ".join(f"{rung.capitalize()}: lost runs fell in wave" for rung in RUNGS) + " |")
    print("| --- |" + " --- |" * (2 * len(RUNGS)))
    for level, d in cells:
        row = [f"{LEVEL_NAMES.get(level, level)} {DIFFICULTIES[d]}"]
        for rung in RUNGS:
            row.append(" ".join(str(wins[(rung, level, d, m)][0]) if (rung, level, d, m) in wins else "-" for m in multiples))
        for rung in RUNGS:
            waves = fell.get((rung, level, d), [])
            counts = defaultdict(int)
            for w in waves:
                counts[w] += 1
            row.append(", ".join(f"w{w} x{n}" for w, n in sorted(counts.items())) or "-")
        print("| " + " | ".join(row) + " |")


if __name__ == "__main__":
    main()
