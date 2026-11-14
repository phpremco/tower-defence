# Round 62: a per-cell table from the pipeline's record files, without the editor.
# Usage: python -I Tools/PlayTest/r62_cells.py <calibration_records.txt> <plan_records.txt> [level substring ...]
# Per cell (level, difficulty): the best replayed plan (wins over its seeds, mean stars of its wins, mean base left), how many
# plans win on most seeds, and the obvious family from the yardstick (wins/seeds, and the earliest wave a lost run fell at).
# Record line: run|level|difficulty|strategy|seed|step|won/lost|stars|waveReached|waveCount|baseLeft|coins|player
import sys
from collections import defaultdict

NAMES = ["Easy", "Medium", "Hard", "Psycho"]
OBVIOUS = ["mono PulseRifle", "mono Cannon", "mono Laser", "naive", "affordable", "creative"]


def runs(path):
    out = []
    for line in open(path, encoding="utf-8"):
        f = line.rstrip("\n").split("|")
        if f[0] == "run" and (len(f) < 13 or f[12] == "expected"):
            out.append(dict(level=f[1], d=int(f[2]), strategy=f[3], seed=f[4], won=f[6] == "won", stars=int(f[7]),
                            wave=int(f[8]), waves=int(f[9]), base=float(f[10])))
    return out


def cell(rs):
    by = defaultdict(list)
    for r in rs:
        by[r["strategy"]].append(r)
    return by


def obvious(rs):
    if not rs:
        return "-"
    wins = sum(r["won"] for r in rs)
    lost = [r["wave"] for r in rs if not r["won"]]
    return f"{wins}/{len(rs)}" + (f" (w{min(lost)})" if lost and wins == 0 else "")


def main():
    yard, plans = runs(sys.argv[1]), runs(sys.argv[2])
    wanted = sys.argv[3:]
    cells = sorted({(r["level"], r["d"]) for r in yard + plans})
    print("| Cell | Best plan (wins, stars, base) | Plans winning 2+ | Gun row | Cannon row | Laser row | naive | affordable | creative |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    for level, d in cells:
        if wanted and not any(w in level for w in wanted):
            continue
        p = cell([r for r in plans if r["level"] == level and r["d"] == d])
        y = cell([r for r in yard if r["level"] == level and r["d"] == d])
        best, holding = "-", 0
        ranked = []
        for name, rs in p.items():
            wins = [r for r in rs if r["won"]]
            holding += 1 if len(wins) * 2 > len(rs) else 0
            stars = sum(r["stars"] for r in wins) / len(wins) if wins else 0.0
            base = sum(r["base"] for r in wins) / len(wins) if wins else 0.0
            ranked.append((len(wins) / len(rs), stars, base, len(wins), len(rs)))
        if ranked:
            share, stars, base, wins, n = max(ranked)
            best = f"{wins}/{n}, {stars:.1f}, {base:.0f}"
        row = [obvious(y.get(name, [])) for name in OBVIOUS]
        print(f"| {level.replace('Level_', '')} {NAMES[d]} | {best} | {holding}/{len(p)} | " + " | ".join(row) + " |")


main()
