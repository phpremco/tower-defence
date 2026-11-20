"""Round 87: the break-evens of several reference rungs side by side, per cell (the archetype rungs against the others).

A rung's break-even is closed_loop.py's: the geometric middle of the last health multiple at which two of three seeds
win and the first at which they do not ("<" when the lowest multiple is lost already, ">" when the highest is held).
Per cell and rung the table shows the seeds won per multiple and the break-even, and marks the cell when two of the
compared rungs are more than one ladder step apart: by the number of multiples held when both played the same ladder,
else by the ratio of their break-evens (more than STEP to the power 1.5).

    python -I Tools/PlayTest/archetype_table.py <records> [<records> ...] [--rungs laser,cannon,sensible,skilled]
                                                [--compare laser,cannon] [--levels 0,1,2]

--rungs    the rungs shown, in this order (default: every rung found in the records)
--compare  the rungs whose distance marks a cell (default: all rungs shown)
The records may come from several measurements (the archetypes of one batch, the sensible and skilled rungs of
another): the stamps each rung was measured with are listed under the table, and a rung measured on two data states
is called out there. Nothing is written.
"""
import math
import os
import re
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from closed_loop import DIFFS, LEVELS, break_even  # noqa: E402

# One step of the measurement ladders (x0.8, 0.9, 1, 1.12, 1.25, 1.4, 1.57, 1.76).
STEP = 1.12
STEPS_APART = 1
OPTIONS = ("--rungs", "--compare", "--levels")
END_MARK = {"low": "<", "high": ">", "": ""}


def option(name, default):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def load(paths):
    """Wins and plays per (level, difficulty, rung, multiple), and the stamps and notes each rung was recorded with."""
    wins = defaultdict(int)
    played = defaultdict(int)
    stamps = defaultdict(set)
    plain = defaultdict(set)
    for path in paths:
        for line in open(path, encoding="utf-8"):
            f = line.rstrip("\n").split("|")
            if f[0] != "prun":
                continue
            key = (f[1], int(f[2]), f[3], float(f[5]))
            played[key] += 1
            wins[key] += 1 if f[6] == "won" else 0
            stamp = re.search(r"bot=([0-9a-f:]+);data=([0-9a-f]+);game=([0-9a-f]+)", line)
            stamps[f[3]].add(stamp.groups() if stamp else ("?", "?", "?"))
            if len(f) > 24 and "plays as sensible" in f[24]:
                plain[f[3]].add((f[1], int(f[2])))
    return wins, played, stamps, plain


def ladder(played, level, d, rung):
    return sorted(m for (l, dd, r, m) in played if l == level and dd == d and r == rung)


def held_count(wins, played, level, d, rung):
    """How many multiples the rung holds from the bottom of its ladder up (the break-even's place on the ladder)."""
    count = 0
    for m in ladder(played, level, d, rung):
        if wins[(level, d, rung, m)] < min(2, played[(level, d, rung, m)]):
            break
        count += 1
    return count


def apart(wins, played, level, d, a, b, values):
    """Whether two rungs are more than STEPS_APART ladder steps apart in this cell."""
    if ladder(played, level, d, a) == ladder(played, level, d, b):
        return abs(held_count(wins, played, level, d, a) - held_count(wins, played, level, d, b)) > STEPS_APART
    high, low = max(values[a], values[b]), min(values[a], values[b])
    return high / low > STEP ** (STEPS_APART + 0.5)


def main():
    paths = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and sys.argv[i - 1] not in OPTIONS]
    if not paths:
        sys.exit(__doc__)
    wins, played, stamps, plain = load(paths)
    found = sorted({r for (_, _, r, _) in played})
    rungs = option("--rungs", ",".join(found)).split(",")
    compare = [r for r in option("--compare", ",".join(rungs)).split(",") if r in rungs]
    nodes = option("--levels", "")
    levels = sorted(LEVELS) if not nodes else [sorted(LEVELS)[int(n)] for n in nodes.split(",")]

    print("| Cell | " + " | ".join(f"{r}: seeds won, break-even" for r in rungs) + " | More than one step apart |")
    print("| --- | " + " | ".join("---" for _ in rungs) + " | --- |")
    marked = []
    never = []
    for level in levels:
        for d in range(len(DIFFS)):
            cells = []
            values = {}
            for rung in rungs:
                value, row, end = break_even(wins, played, level, d, rung)
                if value is None:
                    cells.append("not played")
                    continue
                values[rung] = value
                same = " (= sensible)" if (level, d) in plain[rung] else ""
                cells.append(f"`{row}` {END_MARK[end]}x{value:.2f}{same}")
                if end == "low":
                    never.append(f"{LEVELS[level]} {DIFFS[d]} {rung} (lowest multiple x{ladder(played, level, d, rung)[0]:g})")
            pairs = []
            judged = [r for r in compare if r in values]
            for i, a in enumerate(judged):
                for b in judged[i + 1:]:
                    if apart(wins, played, level, d, a, b, values):
                        weak, strong = (a, b) if values[a] < values[b] else (b, a)
                        pairs.append(f"{weak} x{values[weak]:.2f} < {strong} x{values[strong]:.2f}")
            if pairs:
                marked.append(f"{LEVELS[level]} {DIFFS[d]}: " + "; ".join(pairs))
            print(f"| {LEVELS[level]} {DIFFS[d]} | " + " | ".join(cells) + " | " + ("**" + "; ".join(pairs) + "**" if pairs else "") + " |")

    print(f"\nCells with two of {', '.join(compare)} more than one ladder step apart: {len(marked)}")
    for line in marked:
        print("  " + line)
    print(f"\nRungs that do not hold the lowest multiple of a cell: {len(never)}")
    for line in never:
        print("  " + line)
    print("\nMeasured with (bot; data; game code):")
    for rung in rungs:
        seen = sorted(stamps.get(rung, []))
        states = len({s[1] for s in seen})
        print(f"  {rung}: " + (", ".join("bot " + s[0] + "; data " + s[1] + "; game " + s[2] for s in seen) or "-")
              + (f"  <-- {states} data states" if states > 1 else ""))
    data = {s[1] for r in rungs for s in stamps.get(r, [])}
    if len(data) > 1:
        print(f"NOTE: the rungs shown were measured on {len(data)} data states ({', '.join(sorted(data))}): a gap between two rungs of different states is not the rungs alone.")


if __name__ == "__main__":
    main()
