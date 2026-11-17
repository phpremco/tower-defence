#!/usr/bin/env python3
"""Two pressure measurements side by side as Markdown (round 78): per cell and rung the seeds won at every multiple, the
first file's beside the second's, with the cells that moved marked.

    python Tools/PlayTest/pressure_versus.py <records before> <records after> [--mixed-bots]

Meant for a bot comparison on the same data (version 2 against version 3) or a data comparison with the same bot; the stamp
rule of pressure_stamp.py holds: two different bots need --mixed-bots, and the header says what differs.
"""
import sys
from collections import defaultdict

import pressure_stamp
from pressure_wins import DIFFICULTIES, LEVEL_NAMES, load

RUNGS = ["skilled", "sensible"]


def wins(runs):
    table = defaultdict(lambda: [0, 0])
    for r in runs:
        table[(r["policy"], r["level"], r["d"], r["m"])][0] += r["won"]
        table[(r["policy"], r["level"], r["d"], r["m"])][1] += 1
    return table


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 2:
        sys.exit(__doc__)
    before, after = load(args[0]), load(args[1])
    allow = pressure_stamp.ALLOW_FLAG in sys.argv
    pressure_stamp.check_one(before, args[0], allow)
    pressure_stamp.check_one(after, args[1], allow)
    pressure_stamp.check_pair(after, before, allow)
    print(f"Before: {pressure_stamp.describe(before)}")
    print(f"After:  {pressure_stamp.describe(after)}")
    multiples = sorted({r["m"] for r in after})
    old, new = wins(before), wins(after)
    cells = sorted({(r["level"], r["d"]) for r in after})
    print("Seeds won at x" + " / x".join(f"{m:g}" for m in multiples) + " of the creatures' health; ** marks a cell and rung that moved.")
    print()
    print("| Cell | " + " | ".join(f"{rung.capitalize()}, before | after" for rung in RUNGS) + " |")
    print("| --- |" + " --- | --- |" * len(RUNGS))
    totals = {rung: [0, 0, 0] for rung in RUNGS}
    for level, d in cells:
        row = [f"{LEVEL_NAMES.get(level, level)} {DIFFICULTIES[d]}"]
        for rung in RUNGS:
            a = [old.get((rung, level, d, m), [0, 0]) for m in multiples]
            b = [new.get((rung, level, d, m), [0, 0]) for m in multiples]
            totals[rung][0] += sum(x[0] for x in a)
            totals[rung][1] += sum(x[0] for x in b)
            totals[rung][2] += sum(x[1] for x in b)
            left = " ".join(str(x[0]) if x[1] else "-" for x in a)
            right = " ".join(str(x[0]) if x[1] else "-" for x in b)
            row += [left, f"**{right}**" if left != right else right]
        print("| " + " | ".join(row) + " |")
    print("| Runs won | " + " | ".join(f"{totals[rung][0]} of {totals[rung][2]} | {totals[rung][1]} of {totals[rung][2]}" for rung in RUNGS) + " |")


if __name__ == "__main__":
    main()
