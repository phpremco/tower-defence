"""The closed loop of the balance (round 83): one scalar per cell from a pressure measurement.

The solver shapes a level (which wave is how tough against the others). How tough a cell is as played is measured:
the probe plays the cell at a ladder of health multiples, and the break-even of the difficulty's reference rung (the
highest multiple at which two of three seeds still win) says how far the cell is from its target. A cell's scalar
(Tools/PlayTest/wave_tuning.txt, "KEY D scale") is a factor on the health of every creature of the cell, exactly as
the probe's multiple is, so

    new scalar = old scalar x measured break-even / target break-even

needs no model and is the same procedure for every level. Targets per difficulty (TARGETS below; a rung's break-even
is the geometric middle of its last held and first lost multiple):

    Easy    the weaker rung breaks even at x1.55 (wins x1.4, loses x1.76)
    Medium  the sensible rung breaks even at x1.32 (wins x1.25, loses x1.4); the skilled rung at x1.06 at least
    Hard    the sensible rung breaks even at x0.95 (wins x0.9, loses x1); the skilled rung at x1.06 at least
    Psycho  the skilled rung breaks even at x1.06 (wins x1, loses x1.12)

Round 85 (Remco after playing level 1: Easy and Medium a tad too hard, Hard right): Easy x1.4 -> x1.55 and Medium
x1.18 -> x1.32, one step of the ladder each. The standard ladder (x0.8 to x1.25) does not reach those, so Easy is
measured on x1 / 1.25 / 1.4 / 1.57 / 1.76 and Medium on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57 (Tools/PlayTest/r85_measure.sh
plays the three ladders into three records files; give all three to this script).

A cell takes the tightest of its difficulty's conditions. The skilled rung stands for the better of the two reference
players: where its picked strategy breaks before the sensible rung does, the pick is stale and the sensible rung's
break-even is read in its place (and the picks want a refresh, as a bot batch of their own). Usage:

    python Tools/PlayTest/closed_loop.py <records> [<records> ...]            the table and the proposed scalars
    python Tools/PlayTest/closed_loop.py <records> --write                    also writes the scale lines of wave_tuning.txt
    python Tools/PlayTest/closed_loop.py <records> --centre                   also prints the per-difficulty factors (the
                                                                             geometric mean of a difficulty's scalars) for
                                                                             BalanceRules.DifficultyScale, and the scalars over them
    --levels 0,1,2   only these campaign nodes (a new level: its own node)    --step 1.4   the most one pass moves a scalar

Several records files are read as one measurement (the same data stamp is required unless --mixed-data).
After --write: the solve stage (Docs/COMMANDS.md, "Round 83"), then measure again; a cell is done when its reference
rung reads its target on the standard ladder.
"""
import math
import re
import sys
from collections import defaultdict

TUNING = "Tools/PlayTest/wave_tuning.txt"
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]
LEVELS = {"Level_C1_01": "Landing Site", "Level_C1_02": "Glass Meadows", "Level_C1_03": "Shard Ridge", "Level_C1_04": "Fault Line",
          "Level_C1_05": "Quartz Flats", "Level_C1_06": "Crysium Vein"}
# Per difficulty: (rung, target break-even, binding). A binding condition is met from both sides; a guard only softens.
TARGETS = {
    0: [("sensible", 1.55, False), ("skilled", 1.55, False)],
    1: [("sensible", 1.32, True), ("skilled", 1.06, False)],
    2: [("sensible", 0.95, True), ("skilled", 1.06, False)],
    3: [("skilled", 1.06, True)],
}
# Easy has no binding rung: the weaker of the two sets the cell.
WEAKEST = {0}
SEEDS_HELD = 2
SCALAR_LEAST, SCALAR_MOST = 0.7, 1.4


def option(name, default):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def load(paths):
    wins = defaultdict(int)
    played = defaultdict(int)
    stamps = set()
    for path in paths:
        for line in open(path, encoding="utf-8"):
            f = line.rstrip("\n").split("|")
            if f[0] != "prun":
                continue
            key = (f[1], int(f[2]), f[3], float(f[5]))
            played[key] += 1
            wins[key] += 1 if f[6] == "won" else 0
            stamp = re.search(r"data=([0-9a-f]+)", line)
            stamps.add(stamp.group(1) if stamp else "?")
    if len(stamps) > 1 and "--mixed-data" not in sys.argv:
        sys.exit(f"records of {len(stamps)} data states ({', '.join(sorted(stamps))}): one measurement per data state, or --mixed-data")
    return wins, played


def break_even(wins, played, level, d, rung):
    """The geometric middle of the last multiple held from the bottom up and the first one lost; None when not played.
    Returns (value, row, open end): 'low' when the lowest multiple is lost already, 'high' when the highest is held."""
    multiples = sorted(m for (l, dd, r, m) in played if l == level and dd == d and r == rung)
    if not multiples:
        return None, "", ""
    row = " ".join(str(wins[(level, d, rung, m)]) for m in multiples)
    held = None
    for m in multiples:
        # Two of three seeds hold a multiple; a coarse pass on one seed (round 85, PR_SEEDS=1) holds it with that seed.
        if wins[(level, d, rung, m)] >= min(SEEDS_HELD, played[(level, d, rung, m)]):
            held = m
        else:
            if held is None:
                return m / math.sqrt(multiples[1] / multiples[0]) if len(multiples) > 1 else m, row, "low"
            return math.sqrt(held * m), row, ""
    step = multiples[-1] / multiples[-2] if len(multiples) > 1 else 1.0
    return held * math.sqrt(step), row, "high"


def read_scales():
    scales = {}
    try:
        for line in open(TUNING, encoding="utf-8"):
            parts = line.split()
            if len(parts) == 3:
                scales[(parts[0], int(parts[1]))] = float(parts[2])
    except FileNotFoundError:
        pass
    return scales


def write_scales(scales):
    try:
        kept = [line.rstrip("\n") for line in open(TUNING, encoding="utf-8") if len(line.split()) != 3 and line.strip()]
    except FileNotFoundError:
        kept = []
    lines = kept + [f"{key} {d} {value:g}" for (key, d), value in scales.items() if abs(value - 1.0) > 1e-6]
    open(TUNING, "w", encoding="utf-8", newline="\n").write("\n".join(sorted(lines)) + "\n")


def main():
    paths = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and sys.argv[i - 1] not in ("--levels", "--step")]
    if not paths:
        sys.exit(__doc__)
    wins, played = load(paths)
    most_step = float(option("--step", "1.6"))
    nodes = option("--levels", "")
    levels = sorted(LEVELS) if not nodes else [sorted(LEVELS)[int(n)] for n in nodes.split(",")]
    scales = read_scales()
    proposed = dict(scales)
    print("| Cell | Scalar | Rung: seeds won per multiple, break-even (target) | Factor | New scalar |")
    print("| --- | --- | --- | --- | --- |")
    for level in levels:
        key = level.replace("Level_", "")
        for d in range(len(DIFFS)):
            old = scales.get((key, d), 1.0)
            parts = []
            factors = []
            for rung, target, binding in TARGETS[d]:
                value, row, end = break_even(wins, played, level, d, rung)
                if value is None:
                    continue
                # The skilled rung stands for the better reference player. Where its picked strategy breaks before the
                # sensible rung does, the pick is stale (refresh the picks, a bot batch), not the cell hard.
                # Not on Easy: there the weaker rung is the measure, whichever it is.
                other = break_even(wins, played, level, d, "sensible") if rung == "skilled" and d not in WEAKEST else (None, "", "")
                if other[0] is not None and other[0] > value:
                    row += " stale pick, sensible `" + other[1] + "`"
                    value, end = other[0], other[2]
                factor = value / target
                # An open end is a bound: "holds the top of the ladder" under the target says nothing against the cell,
                # "loses the bottom" above the target nothing for it.
                if (end == "high" and factor < 1.0) or (end == "low" and factor > 1.0):
                    factor, end = 1.0, ""
                mark = "under " if end == "low" else "over " if end == "high" else ""
                parts.append(f"{rung} `{row}` {mark}x{value:.2f} ({'' if binding else 'at least ' if d else 'weaker rung '}x{target:g})")
                # A guard that is met asks nothing; an open end is a bound, not a value: it still moves the cell that way.
                if binding or d in WEAKEST or factor < 1.0:
                    factors.append((factor, binding or d in WEAKEST, end))
            if not factors:
                print(f"| {LEVELS[level]} {DIFFS[d]} | {old:g} | not measured | | |")
                continue
            factor, _, end = min(factors)
            factor = max(1.0 / most_step, min(most_step, factor))
            new = round(old * factor, 3)
            proposed[(key, d)] = new
            note = " (open end: measure again)" if end else ""
            print(f"| {LEVELS[level]} {DIFFS[d]} | {old:g} | {'; '.join(parts)} | x{factor:.3f} | {new:g}{note} |")

    if "--centre" in sys.argv:
        print("\nPer difficulty: the geometric mean of the scalars (for BalanceRules.DifficultyScale, times the factor in use) and the scalars over it:")
        for d in range(len(DIFFS)):
            values = [proposed.get((l.replace("Level_", ""), d), 1.0) for l in sorted(LEVELS)]
            mean = math.exp(sum(math.log(v) for v in values) / len(values))
            rest = " ".join(f"{v / mean:.3f}" for v in values)
            out = [f"{v / mean:.2f}" for v in values if not SCALAR_LEAST <= v / mean <= SCALAR_MOST]
            print(f"  {DIFFS[d]}: x{mean:.3f}; cells {rest}{'; outside ' + str(SCALAR_LEAST) + '-' + str(SCALAR_MOST) + ': ' + ' '.join(out) if out else ''}")

    if "--write" in sys.argv:
        write_scales(proposed)
        print(f"\nwritten {TUNING}; now the solve stage, then measure again")


if __name__ == "__main__":
    main()
