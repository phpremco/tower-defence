# Round 87: the fit behind the health formula (Docs/WAVE_GRAMMAR.md, "The solver").
# Reads Tools/PlayTest/r87_dump_r86.txt (Round87Dump.cs run on the waves round 86 stored; the script writes r87_dump.txt) and fits, in the logarithm,
#   walkIn = DifficultyScale[d] x LevelScalar[L] x Ramp(wave)
# on the plain ground waves (no raid, no boss wave, no wave after a boss, no surprise; rush and heavy waves with
# their kind factor taken off), with the ramp (a) geometric in the wave's phase (n-1)/(N-1) and (b) geometric in the
# wave's number, each with one slope for all difficulties and with a slope per difficulty. Prints the residuals and
# the numbers. The ramp is normalised to one at mid-level (start x end = 1), so DifficultyScale is the multiplier of
# a level's middle wave.
# Usage: python -I Tools/PlayTest/r87_fit.py [dump] [--rush 1.1] [--heavy 1.15]
import math
import sys


RUSH, HEAVY = 1.1, 1.15
LEAST, MOST = 0.7, 1.4


def rows(path):
    lines = open(path, encoding="utf-8").read().strip().split("\n")
    head = lines[0].split("\t")
    cells = {}
    for line in lines[1:]:
        r = dict(zip(head, line.split("\t")))
        cells.setdefault((r["key"], int(r["difficulty"])), []).append(r)
    out = []
    for (key, d), waves in sorted(cells.items()):
        for i, r in enumerate(waves):
            after_boss = i > 0 and waves[i - 1]["boss"] != "-"
            if r["raid"] == "1" or r["kind"] == "Air" or r["boss"] != "-" or after_boss or r["surprise"] != "None":
                continue
            kind = RUSH if r["kind"] == "Rush" else HEAVY if r["kind"] == "Heavy" else 1.0
            n, total = int(r["wave"]), int(r["waves"])
            out.append({"key": key, "level": int(r["level"]), "d": d, "n": n, "phase": (n - 1) / (total - 1),
                        "mid": (total - 1) / 2.0, "y": math.log(float(r["walkin"]) / kind)})
    return out


def solve(matrix, vector):
    """Least squares by the normal equations and Gaussian elimination (no numpy on this machine)."""
    n = len(matrix[0])
    ata = [[sum(row[i] * row[j] for row in matrix) for j in range(n)] for i in range(n)]
    atb = [sum(row[i] * v for row, v in zip(matrix, vector)) for i in range(n)]
    for i in range(n):
        pivot = max(range(i, n), key=lambda k: abs(ata[k][i]))
        ata[i], ata[pivot] = ata[pivot], ata[i]
        atb[i], atb[pivot] = atb[pivot], atb[i]
        for k in range(i + 1, n):
            factor = ata[k][i] / ata[i][i]
            atb[k] -= factor * atb[i]
            for j in range(i, n):
                ata[k][j] -= factor * ata[i][j]
    x = [0.0] * n
    for i in reversed(range(n)):
        x[i] = (atb[i] - sum(ata[i][j] * x[j] for j in range(i + 1, n))) / ata[i][i]
    return x


def fit(data, x_of, per_difficulty):
    levels = sorted({r["key"] for r in data})
    slope_at = 4 + len(levels) - 1
    columns = slope_at + (4 if per_difficulty else 1)
    a = []
    y = [r["y"] for r in data]
    for r in data:
        row = [0.0] * columns
        row[r["d"]] = 1.0
        level = levels.index(r["key"])
        # The level scalars average one in the logarithm: the last level is minus the sum of the others.
        for k in range(len(levels) - 1):
            row[4 + k] = 1.0 if k == level else -1.0 if level == len(levels) - 1 else 0.0
        row[slope_at + (r["d"] if per_difficulty else 0)] = x_of(r)
        a.append(row)
    solution = solve(a, y)
    residual = [v - sum(c * s for c, s in zip(row, solution)) for row, v in zip(a, y)]
    scalars = list(solution[4:slope_at])
    scalars.append(-sum(scalars))
    return {"rms": math.sqrt(sum(v * v for v in residual) / len(residual)), "worst": max(abs(v) for v in residual),
            "difficulty": [math.exp(v) for v in solution[:4]], "levels": dict(zip(levels, [math.exp(v) for v in scalars])),
            "slopes": list(solution[slope_at:]), "parameters": columns}


def main():
    path = next((a for a in sys.argv[1:] if not a.startswith("--")), "Tools/PlayTest/r87_dump_r86.txt")
    data = rows(path)
    print("%d plain ground waves of %d cells" % (len(data), len({(r["key"], r["d"]) for r in data})))
    shapes = {"phase": lambda r: r["phase"] - 0.5, "number": lambda r: r["n"] - 1 - r["mid"]}
    for name, x_of in shapes.items():
        for per in (False, True):
            f = fit(data, x_of, per)
            print("\nramp geometric in wave %s, %s (%d numbers): rms residual x%.3f (log %.4f), worst x%.2f" % (
                name, "a slope per difficulty" if per else "one slope", f["parameters"], math.exp(f["rms"]), f["rms"], math.exp(f["worst"])))
            print("  DifficultyScale " + " / ".join("%.3f" % v for v in f["difficulty"]))
            print("  level scalars   " + "  ".join("%s %.3f (clamped %.3f)" % (k, v, min(MOST, max(LEAST, v))) for k, v in f["levels"].items()))
            if name == "phase":
                print("  RampStart / RampEnd " + "  ".join("%.3f / %.3f" % (math.exp(-s / 2), math.exp(s / 2)) for s in f["slopes"]))
            else:
                print("  growth per wave " + "  ".join("x%.4f" % math.exp(s) for s in f["slopes"]))


main()
