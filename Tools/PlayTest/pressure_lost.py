#!/usr/bin/env python3
"""Where the lost runs of a pressure records file fell: per cell and rung the wave each lost run ended in, the share of
the base it still had when that wave began, and the base health the last waves took.
  python Tools/PlayTest/pressure_lost.py <records> [--multiple 1]"""
import sys
from collections import defaultdict

import pressure_endings

LEVELS = {"Level_C1_01": "Landing Site", "Level_C1_02": "Glass Meadows", "Level_C1_03": "Shard Ridge", "Level_C1_04": "Fault Line", "Level_C1_05": "Quartz Flats", "Level_C1_06": "Crysium Vein"}
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]


def main():
    path = sys.argv[1]
    only = float(sys.argv[sys.argv.index("--multiple") + 1]) if "--multiple" in sys.argv else None
    rows = defaultdict(list)
    for run in pressure_endings.load(path, None, None):
        if run["won"] or (only is not None and run["m"] != only):
            continue
        before = pressure_endings.base_before(run, run["wave"])
        rows[(run["level"], run["d"], run["policy"])].append(f"x{run['m']:g} s{run['seed']}: w{run['wave']}/{run['waves']} from {before * 100:.0f} %")
    for (level, diff, policy), runs in sorted(rows.items()):
        print(f"{LEVELS.get(level, level):<14} {DIFFS[diff]:<7} {policy:<9} " + "; ".join(sorted(runs)))


if __name__ == "__main__":
    main()
