#!/usr/bin/env python3
"""The skilled rung's strategy per cell, picked from today's data (round 78; ranked on break-even since round 83b).

    python Tools/PlayTest/skilled_picks.py <pick records> [<pick records> ...] [--write]

The records are a pressure batch that played every searched strategy of every cell as the skilled rung plays one
(policies strategy1..strategyN, seeds 1-3; Docs/COMMANDS.md "Round 83b"), on the standard ladder of health multiples
(x0.8 / 0.9 / 1 / 1.12 / 1.25). Per cell the pick is the strategy with the highest break-even: the geometric middle
of the last multiple it holds from the bottom up (two of three seeds) and the first one it loses, exactly as
closed_loop.py reads a rung. The closed loop sets a cell's scalar on the skilled rung's break-even, so the rung must
be the strategy that carries furthest, not the one that happens to keep the most base health at the data (round 82b
picked on x1 alone, and six picks broke before the sensible rung did on the next waves). Ties go to the most seeds
won at the data, then the most seeds won over the whole ladder, then the most base health kept at the data (lost
runs count zero), then the most stars, then the lowest number. Records with the data's multiple alone rank as before
round 83b. Prints every candidate so the pick can be checked; with --write it writes Tools/PlayTest/skilled_picks.txt,
which the skilled rung reads (HeadroomProbeTests.BestRecordedStrategy) and which is part of the bot stamp: a new
pick file is a new bot.
"""
import collections
import math
import re
import sys

OUT = "Tools/PlayTest/skilled_picks.txt"
PICK_MULTIPLE = 1.0
SEEDS_HELD = 2


def break_even(row):
    """row: sorted [(multiple, seeds won)]. The value and 'low' / 'high' when the ladder does not reach it."""
    multiples = [m for m, _ in row]
    held = None
    for m, won in row:
        if won >= SEEDS_HELD:
            held = m
            continue
        if held is None:
            return (m / math.sqrt(multiples[1] / multiples[0]) if len(multiples) > 1 else m * 0.5), "low"
        return math.sqrt(held * m), ""
    step = multiples[-1] / multiples[-2] if len(multiples) > 1 else 1.0
    return held * math.sqrt(step), "high"


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if not args:
        sys.exit(__doc__)
    runs = collections.defaultdict(dict)   # (level, difficulty, policy) -> (multiple, seed) -> (won, base share, stars)
    stamps = set()
    for path in args:
        for line in open(path, encoding="utf-8"):
            f = line.rstrip("\n").split("|")
            if f[0] != "prun" or not re.fullmatch(r"strategy\d+", f[3]):
                continue
            won = f[6] == "won"
            share = float(f[9]) / max(1.0, float(f[10])) if won else 0.0
            runs[(f[1], int(f[2]), f[3])][(float(f[5]), int(f[4]))] = (won, share, int(f[11]) if won else 0)
            stamps.update(x for x in f if x.startswith("bot="))
    if not runs:
        sys.exit("no strategy runs in " + ", ".join(args))
    if len(stamps) != 1:
        sys.exit("the records carry %d stamps; a pick comes from one bot and one data state: %s" % (len(stamps), sorted(stamps)))

    cells = collections.defaultdict(list)
    for (level, difficulty, policy), played in runs.items():
        multiples = sorted({m for m, _ in played})
        row = [(m, sum(1 for (mm, _), run in played.items() if mm == m and run[0])) for m in multiples]
        value, end = break_even(row)
        at_data = [run for (m, _), run in played.items() if abs(m - PICK_MULTIPLE) < 1e-6]
        wins = sum(1 for run in at_data if run[0])
        base = sum(run[1] for run in at_data)
        stars = sum(run[2] for run in at_data)
        ladder = sum(won for _, won in row)
        cells[(level, difficulty)].append((-round(value, 4), -wins, -ladder, -round(base, 4), -stars, int(policy[len("strategy"):]),
                                           len(at_data), " ".join(str(won) for _, won in row), end))

    lines = ["# The skilled rung's strategy per cell (round 78; ranked on break-even since round 83b): of the cell's searched strategies",
             "# (Tools/PlayTest/Plans), the one that breaks even highest on the ladder of health multiples, played as the skilled rung",
             "# plays it on seeds 1-3. Written by Tools/PlayTest/skilled_picks.py; refresh it after a wave or tower change",
             "# (Docs/COMMANDS.md, \"Round 83b\"). It is part of the bot stamp.",
             "# measured with " + sorted(stamps)[0],
             "# pick|level|difficulty|strategy|seeds won at the data|base kept over those seeds|stars|candidates|break-even|seeds won per multiple"]
    for (level, difficulty) in sorted(cells):
        ranked = sorted(cells[(level, difficulty)])
        print("%s %d" % (level, difficulty))
        for i, (value, wins, ladder, base, stars, number, seeds, row, end) in enumerate(ranked):
            mark = "under " if end == "low" else "over " if end == "high" else ""
            print("  %s strategy%d: `%s` breaks even %sx%.2f; at the data won %d of %d, base %.2f, stars %d"
                  % ("*" if i == 0 else " ", number, row, mark, -value, -wins, seeds, -base, -stars))
        value, wins, ladder, base, stars, number, seeds, row, end = ranked[0]
        lines.append("pick|%s|%d|%s_%d_strategy%d|%d/%d|%.4f|%d|%d|%s%.2f|%s"
                     % (level, difficulty, level, difficulty, number, -wins, seeds, -base, -stars, len(ranked),
                        "<" if end == "low" else ">" if end == "high" else "", -value, row))
    if "--write" in sys.argv:
        with open(OUT, "w", encoding="utf-8", newline="\n") as out:
            out.write("\n".join(lines) + "\n")
        print("wrote %s (%d cells)" % (OUT, len(cells)))


if __name__ == "__main__":
    main()
