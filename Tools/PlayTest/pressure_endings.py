#!/usr/bin/env python3
"""How the levels end, from pressure records (round 77): the all-or-nothing share.

    python Tools/PlayTest/pressure_endings.py <records> [<records before>] [--cells 3:1,4:1] [--difficulties 0,1]

Per records file: the runs won by how much of the base they keep, and the runs lost by where they fell. The number the
rounds compare is the all-or-nothing share: of the lost runs, those that fell in the level's last wave from a base that
still had 90 % or more of its health when that wave began. A second, wider reading counts every lost run whose base was
at 90 % or more when the wave it fell in began (a level lost in one wave, whichever wave). With two files both are printed,
the first argument last (before -> after). Reads the same records as pressure_wins.py and holds to the same stamp rule.
"""
import sys

import pressure_stamp
import pressure_wins

WHOLE = 0.9
HURT = 0.5
LEVELS = ["Level_C1_01", "Level_C1_02", "Level_C1_03", "Level_C1_04", "Level_C1_05", "Level_C1_06"]


def load(path, cells, difficulties):
    counts = {}
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("|")
        if p[0] == "prun" and len(p) >= 27:
            counts[(p[1], int(p[2]), p[3], int(p[4]), float(p[5]))] = int(p[8])
    runs = pressure_wins.load(path)
    for r in runs:
        r["waves"] = counts[(r["level"], r["d"], r["policy"], r["seed"], r["m"])]
    return [r for r in runs if (not cells or (r["level"], r["d"]) in cells) and (not difficulties or r["d"] in difficulties)]


def base_before(run, wave):
    """The share of the base left when a wave began."""
    lost = sum(v for w, v in run["base_lost"].items() if w < wave)
    return 1.0 - lost / run["max"] if run["max"] > 0 else 0.0


def summary(runs):
    won = [r for r in runs if r["won"]]
    lost = [r for r in runs if not r["won"]]
    final = [r for r in lost if r["wave"] >= r["waves"]]
    final_whole = [r for r in final if base_before(r, r["wave"]) >= WHOLE]
    any_whole = [r for r in lost if base_before(r, r["wave"]) >= WHOLE]
    return dict(
        runs=len(runs), won=len(won),
        whole=sum(1 for r in won if r["health"] >= WHOLE * r["max"]),
        hurt=sum(1 for r in won if HURT * r["max"] <= r["health"] < WHOLE * r["max"]),
        scraped=sum(1 for r in won if r["health"] < HURT * r["max"]),
        lost=len(lost), final=len(final), final_whole=len(final_whole), any_whole=len(any_whole))


def share(part, whole):
    return f"{part}/{whole} ({100.0 * part / whole:.0f} %)" if whole else f"{part}/0"


def show(name, s):
    print(f"{name}: {s['runs']} runs, {s['won']} won (base kept 90 %+: {s['whole']}, 50-90 %: {s['hurt']}, under 50 %: {s['scraped']}), {s['lost']} lost")
    print(f"  lost in the last wave: {share(s['final'], s['lost'])}")
    print(f"  ALL-OR-NOTHING (lost in the last wave from a base at 90 %+): {share(s['final_whole'], s['lost'])} of the lost runs")
    print(f"  lost in one wave, any wave (base at 90 %+ when the wave began): {share(s['any_whole'], s['lost'])} of the lost runs")
    print(f"  won but hurt (base under 90 %): {share(s['hurt'] + s['scraped'], s['won'])} of the won runs")


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    allow = pressure_stamp.ALLOW_FLAG in sys.argv
    cells, difficulties = set(), set()
    if "--cells" in sys.argv:
        value = sys.argv[sys.argv.index("--cells") + 1]
        args.remove(value)
        cells = {(LEVELS[int(c.split(":")[0])], int(c.split(":")[1])) for c in value.split(",")}
    if "--difficulties" in sys.argv:
        value = sys.argv[sys.argv.index("--difficulties") + 1]
        args.remove(value)
        difficulties = {int(d) for d in value.split(",")}
    files = [(path, load(path, cells, difficulties)) for path in args]
    for path, runs in files:
        pressure_stamp.check_one(runs, path, allow)
    if len(files) == 2:
        pressure_stamp.check_pair(files[0][1], files[1][1], allow)
        files.reverse()
    for path, runs in files:
        print(f"{path}: {pressure_stamp.describe(runs)}")
        show("all", summary(runs))
        for policy in sorted({r["policy"] for r in runs}):
            show(f"  {policy}", summary([r for r in runs if r["policy"] == policy]))
        for d in sorted({r["d"] for r in runs}):
            show(f"  {pressure_wins.DIFFICULTIES[d]}", summary([r for r in runs if r["d"] == d]))
        print()


if __name__ == "__main__":
    main()
