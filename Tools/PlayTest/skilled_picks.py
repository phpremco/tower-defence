#!/usr/bin/env python3
"""The skilled rung's strategy per cell, picked from today's data (round 78).

    python Tools/PlayTest/skilled_picks.py <pick records> [--write]

The records are a pressure batch that played every searched strategy of every cell as the skilled rung plays one
(policies strategy1..strategyN, health multiple 1, seeds 1-3; Docs/COMMANDS.md "Round 78"). Per cell the pick is the
strategy that won the most seeds, then the one that kept the most base health over its seeds (lost runs count zero),
then the most stars, then the lowest number. Prints every candidate so the pick can be checked; with --write it writes
Tools/PlayTest/skilled_picks.txt, which the skilled rung reads (HeadroomProbeTests.BestRecordedStrategy) and which is part
of the bot stamp: a new pick file is a new bot.
"""
import collections
import re
import sys

OUT = "Tools/PlayTest/skilled_picks.txt"
PICK_MULTIPLE = 1.0


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1:
        sys.exit(__doc__)
    runs = collections.defaultdict(dict)   # (level, difficulty, policy) -> seed -> (won, base share, stars)
    stamps = set()
    for line in open(args[0], encoding="utf-8"):
        f = line.rstrip("\n").split("|")
        if f[0] != "prun" or not re.fullmatch(r"strategy\d+", f[3]) or abs(float(f[5]) - PICK_MULTIPLE) > 1e-6:
            continue
        won = f[6] == "won"
        share = float(f[9]) / max(1.0, float(f[10])) if won else 0.0
        runs[(f[1], int(f[2]), f[3])][int(f[4])] = (won, share, int(f[11]) if won else 0)
        stamps.update(x for x in f if x.startswith("bot="))
    if not runs:
        sys.exit("no strategy runs at multiple 1 in " + args[0])
    if len(stamps) != 1:
        sys.exit("the records carry %d stamps; a pick comes from one bot and one data state: %s" % (len(stamps), sorted(stamps)))

    cells = collections.defaultdict(list)
    for (level, difficulty, policy), seeds in runs.items():
        wins = sum(1 for s in seeds.values() if s[0])
        base = sum(s[1] for s in seeds.values())
        stars = sum(s[2] for s in seeds.values())
        cells[(level, difficulty)].append((-wins, -round(base, 4), -stars, int(policy[len("strategy"):]), len(seeds)))

    lines = ["# The skilled rung's strategy per cell (round 78): of the cell's searched strategies (Tools/PlayTest/Plans), the one that",
             "# did best played as the skilled rung plays it, at the data, on seeds 1-3. Written by Tools/PlayTest/skilled_picks.py;",
             "# refresh it after a wave or tower change (Docs/COMMANDS.md, \"Round 78\"). It is part of the bot stamp.",
             "# measured with " + sorted(stamps)[0],
             "# pick|level|difficulty|strategy|seeds won|base kept over the seeds|stars|candidates"]
    for (level, difficulty) in sorted(cells):
        ranked = sorted(cells[(level, difficulty)])
        print("%s %d" % (level, difficulty))
        for i, (wins, base, stars, number, seeds) in enumerate(ranked):
            print("  %s strategy%d: won %d of %d, base %.2f, stars %d" % ("*" if i == 0 else " ", number, -wins, seeds, -base, -stars))
        wins, base, stars, number, seeds = ranked[0]
        lines.append("pick|%s|%d|%s_%d_strategy%d|%d/%d|%.4f|%d|%d" % (level, difficulty, level, difficulty, number, -wins, seeds, -base, -stars, len(ranked)))
    if "--write" in sys.argv:
        with open(OUT, "w", encoding="utf-8", newline="\n") as out:
            out.write("\n".join(lines) + "\n")
        print("wrote %s (%d cells)" % (OUT, len(cells)))


if __name__ == "__main__":
    main()
