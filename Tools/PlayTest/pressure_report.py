#!/usr/bin/env python3
"""The pressure report (Docs/BALANCE_HEADROOM.md, "Stage 1b") from the records of Tools/PlayTest/pressure.sh.

    python Tools/PlayTest/pressure_report.py [records] [report]

Reads pressure_records.txt (prun, pref and batch lines; see PressureProbeTests) and writes pressure.txt.

Pressure of one run, defined before the runs were made:

    P = A + D            (0 = nothing got anywhere, 2 = everything walked in and the base fell)
    A = mean over the level's waves of a_w
        a_w = sum(health_i * furthest_i) / sum(health_i) over the creatures spawned in wave w, furthest_i = the share of its
        road creature i covered before it died (1 = it reached the base); a wave that never came because the base fell = 1
    D = base health lost over the run / base maximum, capped at 1; a lost level = 1

A moves with every creature that gets a step further, so it does not saturate where the win/lose edge does; D adds what
the leaks cost. The stop rules below are fixed here, not fitted to the results.
"""
import math
import statistics
import sys
from collections import defaultdict

DIFFICULTIES = ["Easy", "Medium", "Hard", "Psycho"]
LEVEL_NAMES = {"Level_C1_01": "Landing Site", "Level_C1_03": "Shard Ridge"}
RUNGS = ["skilled", "typical", "sensible", "careless"]  # typical: stage 1b records only (dropped from the default run in round 74)
FIXED = ["skilled", "typical"]

# Stop rule (a): a unit/seed curve is monotone when no step to the next multiple drops P by more than this (1 % of the 0-2 scale).
TOLERANCE = 0.02
MONOTONE_SHARE = 0.95
# Stop rule (b): (max - min) / median of P at the data over the seeds, per cell, for the fixed-build-order rung.
SPREAD_LIMIT = 0.10
RULER = "skilled"
# The derived edge: the multiple at which the base loses this share of its health (interpolated on the log of the multiple).
BREACH = 0.5


def waves(text, parse):
    out = {}
    for item in text.split(";"):
        if "=" in item:
            k, v = item.split("=")
            out[int(k)] = parse(v)
    return out


def load(path):
    runs, refs, batches = [], [], []
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("|")
        if p[0] in ("prun", "pref") and len(p) >= 27:
            r = dict(level=p[1], d=int(p[2]), policy=p[3], seed=int(p[4]), m=float(p[5]), outcome=p[6], wave=int(p[7]), waves=int(p[8]),
                     health=float(p[9]), max=float(p[10]), stars=int(p[11]), game=float(p[12]), wall=float(p[13]), built=int(p[14]),
                     upgrades=int(p[15]), coins=int(p[16]), earned=int(p[17]), repair=int(p[18]), reach=waves(p[19], float),
                     base_lost=waves(p[20], float), gate_lost=waves(p[21], float),
                     advance=waves(p[22], lambda v: tuple(float(x) for x in v.split(":"))), moves=p[23], note=p[24], worker=p[-2], batch=p[-1])
            # Round 74: the purse at each wave start and the idle-counter note sit between the note and the worker.
            r["purse"] = waves(p[25], int) if len(p) >= 29 else {}
            r["idle"] = p[26] if len(p) >= 29 else ""
            score(r)
            (runs if p[0] == "prun" else refs).append(r)
        elif p[0] == "batch" and len(p) >= 5:
            batches.append(dict(started=p[1], seconds=int(p[2]), workers=int(p[3]), units=int(p[4])))
    return runs, refs, batches


def score(r):
    lost = r["outcome"] != "won"
    a, top = [], []
    for w in range(1, r["waves"] + 1):
        health, advanced, _ = r["advance"].get(w, (0.0, 0.0, 0.0))
        if health > 0:
            a.append(advanced / health)
        else:
            a.append(1.0 if lost else 0.0)
        top.append(r["reach"].get(w, 1.0 if lost else 0.0))
    r["A"] = sum(a) / len(a)
    r["R"] = sum(top) / len(top)
    r["D"] = 1.0 if lost else min(1.0, sum(r["base_lost"].values()) / r["max"])
    r["P"] = r["A"] + r["D"]
    r["a"] = a
    total = sum(h for h, _, _ in r["advance"].values())
    r["leak"] = sum(l for _, _, l in r["advance"].values()) / total if total > 0 else 0.0


def cell_name(level, d):
    return f"{LEVEL_NAMES.get(level, level)} {DIFFICULTIES[d]}"


def med_range(values, fmt="{:.3f}"):
    if not values:
        return "-"
    return f"{fmt.format(statistics.median(values))} ({fmt.format(min(values))}-{fmt.format(max(values))})"


def spread(values):
    m = statistics.median(values)
    return (max(values) - min(values)) / m if m > 0 else 0.0


def crossing(points, key, threshold):
    """The multiple at which key first reaches the threshold, on the log of the multiple; None below the grid, inf above it."""
    points = sorted(points, key=lambda r: r["m"])
    if points[0][key] >= threshold:
        return None
    for a, b in zip(points, points[1:]):
        if b[key] >= threshold:
            t = (threshold - a[key]) / (b[key] - a[key])
            return math.exp(math.log(a["m"]) + t * (math.log(b["m"]) - math.log(a["m"])))
    return math.inf


def show_edge(value, lo, hi):
    if value is None:
        return f"<{lo:g}"
    if value == math.inf:
        return f">{hi:g}"
    return f"{value:.2f}"


def main():
    records = sys.argv[1] if len(sys.argv) > 1 else "Tools/PlayTest/pressure_records.txt"
    report = sys.argv[2] if len(sys.argv) > 2 else "Tools/PlayTest/pressure.txt"
    runs, refs, batches = load(records)
    # The last record of every (unit, multiple): a rerun replaces the first.
    last = {}
    for r in runs:
        last[(r["level"], r["d"], r["policy"], r["seed"], r["m"])] = r
    runs = list(last.values())
    curves = defaultdict(list)
    for r in runs:
        curves[(r["level"], r["d"], r["policy"], r["seed"])].append(r)
    for c in curves.values():
        c.sort(key=lambda r: r["m"])
    cells = sorted({(r["level"], r["d"]) for r in runs})
    policies = sorted({r["policy"] for r in runs}, key=lambda p: (RUNGS.index(p) if p in RUNGS else len(RUNGS), p))
    multiples = sorted({r["m"] for r in runs})
    lo, hi = multiples[0], multiples[-1]
    out = []

    out.append("Pressure P = A + D per run (0-2): A = mean over the waves of the health-weighted share of the road the wave's creatures")
    out.append("covered (a wave that never came = 1), D = base health lost / base maximum (a lost level = 1). See the header of")
    out.append("Tools/PlayTest/pressure_report.py. Median over the seeds, range in brackets.")
    out.append("")

    # ---- Stop rule (a) -------------------------------------------------------------------------------------------------
    out.append(f"STOP RULE (a): P is monotone non-decreasing in the health multiple (tolerance {TOLERANCE} per step) for at least {MONOTONE_SHARE:.0%} of the unit/seed curves")
    per_policy = defaultdict(lambda: [0, 0, 0, 0])
    offenders = []
    variants = {"P": [0, 0], "A": [0, 0], "R+D (furthest creature)": [0, 0], "D": [0, 0], "won/lost": [0, 0]}
    for key, c in sorted(curves.items()):
        if len(c) < 2:
            continue
        drops = [(a["m"], b["m"], b["P"] - a["P"]) for a, b in zip(c, c[1:]) if b["P"] < a["P"] - TOLERANCE]
        stats = per_policy[key[2]]
        stats[0] += 1
        stats[1] += not drops
        stats[2] += len(c) - 1
        stats[3] += len(drops)
        if drops:
            offenders.append((key, c, drops))
        for name, values in (("P", [r["P"] for r in c]), ("A", [r["A"] for r in c]), ("R+D (furthest creature)", [r["R"] + r["D"] for r in c]),
                             ("D", [r["D"] for r in c]), ("won/lost", [0.0 if r["outcome"] == "won" else 1.0 for r in c])):
            variants[name][0] += 1
            variants[name][1] += all(b >= a - TOLERANCE for a, b in zip(values, values[1:]))
    total = sum(s[0] for s in per_policy.values())
    good = sum(s[1] for s in per_policy.values())
    share = good / max(1, total)
    out.append(f"  all rungs: {good} of {total} curves monotone = {share:.1%}  ->  {'PASS' if share >= MONOTONE_SHARE else 'FAIL'}")
    for policy in policies:
        s = per_policy[policy]
        if s[0]:
            out.append(f"  {policy:<12} {s[1]:>3} of {s[0]:<3} curves ({s[1] / s[0]:.0%}); {s[3]} of {s[2]} steps drop by more than the tolerance")
    out.append("  the same check on other scores (all rungs): " + "; ".join(f"{name} {v[1]} of {v[0]}" for name, v in variants.items()))
    out.append("  curves that are not monotone (P per multiple, the drops marked):")
    for key, c, drops in offenders:
        marks = {b for _, b, _ in drops}
        points = " ".join(f"{r['m']:g}:{r['P']:.2f}{'L' if r['outcome'] != 'won' else ''}{'!' if r['m'] in marks else ''}" for r in c)
        out.append(f"    {cell_name(key[0], key[1]):<22}{key[2]:<12} seed {key[3]}: {points}  (worst drop {min(d for _, _, d in drops):+.3f})")
    out.append("")

    # ---- Stop rule (b) -------------------------------------------------------------------------------------------------
    out.append(f"STOP RULE (b): spread over the seeds, (max - min) / median, of P at the data (x1) at most {SPREAD_LIMIT:.0%} for the '{RULER}' rung in every cell")
    passed = cells_checked = 0
    spreads = defaultdict(dict)
    for level, d in cells:
        for policy in policies:
            values = [r["P"] for r in runs if (r["level"], r["d"], r["policy"]) == (level, d, policy) and abs(r["m"] - 1) < 1e-6]
            if len(values) >= 2:
                spreads[(level, d)][policy] = (spread(values), max(values) - min(values), len(values))
        if RULER in spreads[(level, d)]:
            cells_checked += 1
            passed += spreads[(level, d)][RULER][0] <= SPREAD_LIMIT
    out.append(f"  {RULER}: {passed} of {cells_checked} cells within the limit  ->  {'PASS' if cells_checked and passed == cells_checked else 'FAIL'}")
    out.append(f"  {'cell':<22}" + "".join(f"{p:<26}" for p in policies if p in RUNGS))
    for level, d in cells:
        row = f"  {cell_name(level, d):<22}"
        for policy in policies:
            if policy not in RUNGS:
                continue
            s = spreads[(level, d)].get(policy)
            row += f"{(f'{s[0]:.1%} (abs {s[1]:.3f}, n={s[2]})' if s else '-'):<26}"
        out.append(row.rstrip())
    out.append("  the same spread at every multiple (relative; absolute in brackets), fixed-build-order rungs:")
    for policy in FIXED:
        for level, d in cells:
            parts = []
            for m in multiples:
                values = [r["P"] for r in runs if (r["level"], r["d"], r["policy"], r["m"]) == (level, d, policy, m)]
                if len(values) >= 2:
                    parts.append(f"x{m:g} {spread(values):.0%} ({max(values) - min(values):.2f})")
            if parts:
                out.append(f"    {policy:<9}{cell_name(level, d):<22}" + "  ".join(parts))
    out.append("")

    # ---- The table -----------------------------------------------------------------------------------------------------
    out.append("THE TABLE: P at the data (x1), median over the seeds (range); wins out of the seeds played")
    out.append(f"  {'cell':<22}" + "".join(f"{p:<34}" for p in policies))
    for level, d in cells:
        row = f"  {cell_name(level, d):<22}"
        for policy in policies:
            at = [r for r in runs if (r["level"], r["d"], r["policy"]) == (level, d, policy) and abs(r["m"] - 1) < 1e-6]
            text = f"{med_range([r['P'] for r in at])} {sum(r['outcome'] == 'won' for r in at)}/{len(at)}" if at else "-"
            row += f"{text:<34}"
        out.append(row.rstrip())
    out.append("")
    out.append("P per multiple: median over the seeds (min-max), wins/seeds")
    for policy in policies:
        out.append(f"  {policy}")
        for level, d in cells:
            parts = []
            for m in multiples:
                at = [r for r in runs if (r["level"], r["d"], r["policy"], r["m"]) == (level, d, policy, m)]
                if at:
                    values = [r["P"] for r in at]
                    parts.append(f"x{m:g} {statistics.median(values):.2f} ({min(values):.2f}-{max(values):.2f}) {sum(r['outcome'] == 'won' for r in at)}/{len(at)}")
            if parts:
                out.append(f"    {cell_name(level, d):<22}" + " | ".join(parts))
    out.append("")
    out.append("The parts at the data (x1), median over the seeds: A (advance), D (base damage), share of the spawned health that reached the base")
    for policy in policies:
        for level, d in cells:
            at = [r for r in runs if (r["level"], r["d"], r["policy"]) == (level, d, policy) and abs(r["m"] - 1) < 1e-6]
            if at:
                out.append(f"  {policy:<12}{cell_name(level, d):<22}A {med_range([r['A'] for r in at])}  D {med_range([r['D'] for r in at], '{:.2f}')}  leaked {med_range([r['leak'] for r in at], '{:.3f}')}")
    out.append("")

    # ---- The derived edge ----------------------------------------------------------------------------------------------
    out.append(f"DERIVED EDGE H*: the multiple at which the base loses {BREACH:.0%} of its health (D crosses {BREACH}), interpolated on the log of the")
    out.append(f"multiple between the grid points; '<{lo:g}' / '>{hi:g}' = outside the grid. Per seed, then the median; 'pooled' = the crossing")
    out.append("of the median-over-seeds D curve. Spread = max / min - 1 over the seeds inside the grid.")
    for policy in policies:
        out.append(f"  {policy}")
        for level, d in cells:
            edges = []
            for seed in sorted({k[3] for k in curves if k[:3] == (level, d, policy)}):
                edges.append(crossing(curves[(level, d, policy, seed)], "D", BREACH))
            if not edges:
                continue
            numeric = [lo if e is None else hi if e == math.inf else e for e in edges]
            inside = [e for e in edges if e is not None and e != math.inf]
            pooled_curve = []
            for m in multiples:
                at = [r["D"] for r in runs if (r["level"], r["d"], r["policy"], r["m"]) == (level, d, policy, m)]
                if at:
                    pooled_curve.append({"m": m, "D": statistics.median(at)})
            pooled = crossing(pooled_curve, "D", BREACH) if len(pooled_curve) > 1 else None
            median = statistics.median(numeric)
            median_text = f"<{lo:g}" if median <= lo and None in edges else f">{hi:g}" if median >= hi and math.inf in edges else f"{median:.2f}"
            spread_text = f"{max(inside) / min(inside) - 1:.0%} (n={len(inside)})" if len(inside) >= 2 else "-"
            out.append(f"    {cell_name(level, d):<22}median {median_text:<6} pooled {show_edge(pooled, lo, hi):<6} per seed: {' '.join(show_edge(e, lo, hi) for e in edges):<36} spread {spread_text}")
    out.append("")

    # ---- The win-rate edge -------------------------------------------------------------------------------------------
    out.append("WIN RATE: seeds won per multiple, and H50 = the multiple at which half the seeds are lost (interpolated on the log of")
    out.append("the multiple). Not a stop rule; it is what the table supports when P turns out two-valued.")
    out.append(f"  {'':<34}" + " ".join(f"x{m:<5g}" for m in multiples) + " H50")
    for policy in policies:
        for level, d in cells:
            points, counts = [], []
            for m in multiples:
                at = [r for r in runs if (r["level"], r["d"], r["policy"], r["m"]) == (level, d, policy, m)]
                if at:
                    won = sum(r["outcome"] == "won" for r in at)
                    points.append({"m": m, "L": 1 - won / len(at)})
                    counts.append(f"{won}/{len(at)}")
            if len(points) > 1:
                out.append(f"  {policy:<12}{cell_name(level, d):<22}" + " ".join(f"{c:<6}" for c in counts) + " " + show_edge(crossing(points, "L", 0.5), lo, hi))
    out.append("")

    # ---- Leak lines ----------------------------------------------------------------------------------------------------
    out.append("ADVANCE PER WAVE at the data (x1): a_w in percent, median over the seeds (. = the wave never came in most seeds);")
    out.append("below it the waves in which the base was hit, with the number of seeds.")
    for policy in policies:
        for level, d in cells:
            at = [r for r in runs if (r["level"], r["d"], r["policy"]) == (level, d, policy) and abs(r["m"] - 1) < 1e-6]
            if not at:
                continue
            count = at[0]["waves"]
            line = []
            for w in range(count):
                came = [r["a"][w] for r in at if (w + 1) in r["advance"]]
                line.append(f"{round(statistics.median(came) * 100):3d}" if len(came) * 2 > len(at) else "  .")
            hits = defaultdict(int)
            for r in at:
                for w in r["base_lost"]:
                    hits[w] += 1
            out.append(f"  {policy:<12}{cell_name(level, d):<22}{' '.join(line)}")
            out.append(f"  {'':<34}base hit: " + (", ".join(f"w{w} {n}/{len(at)}" for w, n in sorted(hits.items())) or "never"))
    out.append("")

    # ---- Build orders --------------------------------------------------------------------------------------------------
    out.append("WHAT WAS BUILT at the data (x1), seed 1: towers built, upgrades, coins left, coins earned, repairs paid; the first twelve moves")
    for policy in policies:
        for level, d in cells:
            at = [r for r in runs if (r["level"], r["d"], r["policy"], r["seed"]) == (level, d, policy, 1) and abs(r["m"] - 1) < 1e-6]
            for r in at:
                moves = []
                for step in r["moves"].split(";")[:12]:
                    q = step.split(":")
                    if len(q) == 5:
                        moves.append(f"w{q[4]} {q[0]}{' ' + q[1] if q[1] else ''}@{q[2]},{q[3]}")
                out.append(f"  {policy:<12}{cell_name(level, d):<22}{r['outcome']} w{r['wave']}/{r['waves']}; built {r['built']}, upgrades {r['upgrades']}, left {r['coins']}, earned {r['earned']}, repairs {r['repair']}")
                out.append(f"  {'':<34}" + "; ".join(moves))
    out.append("")

    wall = sum(r["wall"] for r in runs)
    out.append(f"Cost: {len(runs)} runs, {wall / 60:.0f} machine minutes of play ({wall / max(1, len(runs)):.0f} s a run), {len(refs)} reference runs for the typical scripts; "
               f"{len(batches)} batches, {sum(b['seconds'] for b in batches) / 60:.0f} wall-clock minutes in all.")
    for b in batches:
        out.append(f"  batch {b['started']}: {b['units']} units on {b['workers']} workers, {b['seconds'] / 60:.1f} min")
    text = "\n".join(out) + "\n"
    open(report, "w", encoding="utf-8", newline="\n").write(text)
    print(text)


if __name__ == "__main__":
    main()
