#!/usr/bin/env python3
"""The headroom table (Docs/BALANCE_HEADROOM.md) from the records of Tools/PlayTest/headroom.sh.

    python Tools/PlayTest/headroom_report.py [records] [report]

Reads headroom_records.txt (h, run and batch lines; see HeadroomProbeTests) and writes headroom.txt: per cell the headroom H
of every reference player (median over the seeds, with the range), the mono rows as a share of the sensible player's, the
spread over seeds, the monotonicity of every unit that was scanned, reruns side by side, rescaled cells, the per-wave leak
line and the cost. H is the last multiple won; the first multiple lost is within the precision above it.
"""
import statistics
import sys
from collections import defaultdict

DIFFICULTIES = ["Easy", "Medium", "Hard", "Psycho"]
LADDER = ["careless", "sensible", "skilled"]
MIN_MULTIPLE, MAX_MULTIPLE = 0.1, 16.0


def load(path):
    edges, runs, batches = [], [], []
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("|")
        if p[0] == "h" and len(p) >= 13:
            edges.append(dict(level=p[1], d=int(p[2]), policy=p[3], seed=int(p[4]), base=float(p[5]), h=float(p[6]),
                              loss=float(p[7]), runs=int(p[8]), wall=float(p[9]), note=p[10], worker=p[11], batch=p[12]))
        elif p[0] == "run" and len(p) >= 24:
            runs.append(dict(level=p[1], d=int(p[2]), policy=p[3], seed=int(p[4]), base=float(p[5]), m=float(p[6]), phase=p[7],
                             outcome=p[8], wave=int(p[9]), waves=int(p[10]), health=float(p[11]), max=float(p[12]), stars=int(p[13]),
                             game=float(p[14]), wall=float(p[15]), reach=p[19], base_lost=p[20], gate_lost=p[21], worker=p[22], batch=p[23]))
        elif p[0] == "batch" and len(p) >= 5:
            batches.append(dict(started=p[1], seconds=int(p[2]), workers=int(p[3]), units=int(p[4])))
    return edges, runs, batches


def show(e):
    if e["runs"] == 0:
        return "n/a"
    if e["h"] <= 0:
        return f"<{MIN_MULTIPLE:.2f}"
    if e["loss"] <= 0:
        return f">{MAX_MULTIPLE:.0f}"
    return f"{e['h']:.2f}"


def value(e):
    """H as a number for medians and ratios: the bound itself when the edge lies outside the bounds."""
    if e["runs"] == 0:
        return None
    return e["h"] if e["h"] > 0 else MIN_MULTIPLE


def latest(edges):
    """The last record of every unit (a rerun replaces the first in the table; both are kept for the rerun section)."""
    out = {}
    for e in edges:
        out[(e["level"], e["d"], e["policy"], e["seed"], e["base"])] = e
    return out


def summary(values):
    values = [v for v in values if v is not None]
    if not values:
        return None, "-"
    med = statistics.median(values)
    lo, hi = min(values), max(values)
    text = f"{med:.2f}" if len(values) == 1 or hi == lo else f"{med:.2f} ({lo:.2f}-{hi:.2f})"
    return med, text


def parse_waves(text):
    out = {}
    for item in text.split(";"):
        if "=" in item:
            k, v = item.split("=")
            out[int(k)] = float(v)
    return out


def leak_line(run):
    reach = parse_waves(run["reach"])
    lost = parse_waves(run["base_lost"])
    cells = []
    for w in range(1, run["waves"] + 1):
        if w not in reach:
            cells.append(" . ")
        elif w in lost:
            cells.append(" B ")
        else:
            cells.append(f"{min(99, round(reach[w] * 100)):2d} ")
    return "".join(cells)


def main():
    records = sys.argv[1] if len(sys.argv) > 1 else "Tools/PlayTest/headroom_records.txt"
    report = sys.argv[2] if len(sys.argv) > 2 else "Tools/PlayTest/headroom.txt"
    edges, runs, batches = load(records)
    last = latest(edges)
    out = []
    cells = sorted({(e["level"], e["d"]) for e in edges})
    plain = {k: e for k, e in last.items() if k[4] == 1.0}

    out.append("Headroom H: the largest uniform multiple of the creatures' health at which the reference player still wins")
    out.append("(median over the seeds, range in brackets; the first multiple lost is within the precision above it).")
    out.append("")
    out.append(f"{'cell':<22}{'careless':<20}{'sensible':<20}{'skilled':<20}{'fresh save (sensible)':<22}")
    med = {}
    for level, d in cells:
        row = f"{level + ' ' + DIFFICULTIES[d]:<22}"
        for policy in LADDER + ["fresh"]:
            m, text = summary([value(e) for k, e in plain.items() if k[:3] == (level, d, policy)])
            shown = [show(e) for k, e in plain.items() if k[:3] == (level, d, policy)]
            if shown and all(s.startswith(("<", ">", "n/a")) for s in shown):
                text = shown[0]
            med[(level, d, policy)] = m
            row += f"{text:<{22 if policy == 'fresh' else 20}}"
        out.append(row.rstrip())

    out.append("")
    out.append("Mono rows (one tower type), seed 1: H, and as a share of the sensible player's H (role pressure);")
    out.append("dominance = best mono row / skilled.")
    for level, d in cells:
        rows = sorted((k[2], e) for k, e in plain.items() if k[:2] == (level, d) and k[2].startswith("mono-"))
        if not rows:
            continue
        sensible, skilled = med.get((level, d, "sensible")), med.get((level, d, "skilled"))
        parts = []
        best = 0.0
        for policy, e in rows:
            v = value(e)
            best = max(best, v or 0.0)
            ratio = f"{v / sensible:.2f}" if v and sensible else "-"
            parts.append(f"{policy[5:]} {show(e)} ({ratio})")
        dominance = f"{best / skilled:.2f}" if skilled else "-"
        out.append(f"  {level + ' ' + DIFFICULTIES[d]:<22}{'; '.join(parts)}; dominance {dominance}")

    out.append("")
    out.append("Spread over seeds (max / min - 1) per cell and ladder rung; the stop rule asks 5 % or less:")
    worst = 0.0
    for level, d in cells:
        parts = []
        for policy in LADDER:
            values = [value(e) for k, e in plain.items() if k[:3] == (level, d, policy) and value(e) is not None and e["h"] > 0 and e["loss"] > 0]
            if len(values) >= 2:
                spread = max(values) / min(values) - 1
                worst = max(worst, spread)
                parts.append(f"{policy} {spread * 100:.1f} % (n={len(values)})")
        if parts:
            out.append(f"  {level + ' ' + DIFFICULTIES[d]:<22}{'; '.join(parts)}")
    out.append(f"  worst spread: {worst * 100:.1f} %")

    out.append("")
    out.append("Monotonicity (every unit with a scan: its runs sorted by multiple; one win->lose crossover = monotone):")
    by_unit = defaultdict(list)
    for r in runs:
        by_unit[(r["level"], r["d"], r["policy"], r["seed"], r["base"], r["batch"])].append(r)
    scanned = violators = 0
    for key, unit_runs in sorted(by_unit.items()):
        if not any(r["phase"] == "scan" for r in unit_runs):
            continue
        scanned += 1
        ordered = sorted(unit_runs, key=lambda r: r["m"])
        marks = "".join("W" if r["outcome"] == "won" else "L" for r in ordered)
        flips = sum(1 for a, b in zip(marks, marks[1:]) if a != b)
        violators += flips > 1
        points = " ".join(f"{r['m']:.2f}{'W' if r['outcome'] == 'won' else 'L'}" for r in ordered)
        out.append(f"  {key[0]} {DIFFICULTIES[key[1]]:<7}{key[2]:<16} seed {key[3]}: {'monotone' if flips <= 1 else 'NOT MONOTONE (' + str(flips) + ' crossovers)'}  {points}")
    out.append(f"  {scanned} units scanned, {violators} not monotone")

    out.append("")
    out.append("Outcome band per cell and rung, every run of every seed pooled (bisection and scan): the lowest multiple lost and")
    out.append("the highest multiple won. Lowest loss above highest win = a sharp edge; otherwise the overlap is the band in which")
    out.append("the result depends on the seed or flips back (width = highest win / lowest loss - 1), with the wins and losses in it.")
    widest = 0.0
    for level, d in cells:
        for policy in LADDER:
            pool = [r for r in runs if (r["level"], r["d"], r["policy"], r["base"]) == (level, d, policy, 1.0)]
            wins = [r["m"] for r in pool if r["outcome"] == "won"]
            losses = [r["m"] for r in pool if r["outcome"] != "won"]
            if not wins or not losses:
                continue
            low, high = min(losses), max(wins)
            if low > high:
                out.append(f"  {level + ' ' + DIFFICULTIES[d]:<22}{policy:<9} sharp: won up to {high:.2f}, lost from {low:.2f} ({len(pool)} runs)")
            else:
                inside = [r for r in pool if low <= r["m"] <= high]
                won_inside = sum(1 for r in inside if r["outcome"] == "won")
                widest = max(widest, high / low - 1)
                out.append(f"  {level + ' ' + DIFFICULTIES[d]:<22}{policy:<9} band {low:.2f}-{high:.2f} (width {(high / low - 1) * 100:.0f} %): {won_inside} won, {len(inside) - won_inside} lost inside ({len(pool)} runs)")
    out.append(f"  widest band: {widest * 100:.0f} %")

    out.append("")
    out.append("Reruns (the same unit measured more than once: H per batch and worker):")
    by_key = defaultdict(list)
    for e in edges:
        by_key[(e["level"], e["d"], e["policy"], e["seed"], e["base"])].append(e)
    reruns = differing = 0
    for key, items in sorted(by_key.items()):
        if len(items) < 2:
            continue
        reruns += 1
        hs = {(e["h"], e["loss"]) for e in items}
        differing += len(hs) > 1
        out.append(f"  {key[0]} {DIFFICULTIES[key[1]]:<7}{key[2]:<16} seed {key[3]} base {key[4]:g}: " + ", ".join(f"{show(e)} ({e['worker']})" for e in items) + ("" if len(hs) == 1 else "  DIFFERS"))
    out.append(f"  {reruns} units rerun, {differing} with a different edge")

    rescaled = sorted((k, e) for k, e in last.items() if k[4] != 1.0)
    if rescaled:
        out.append("")
        out.append("Rescaled cells (the cell's health times the factor, then measured again):")
        for k, e in rescaled:
            before = last.get((k[0], k[1], k[2], k[3], 1.0))
            target = before["h"] / k[4] if before else None
            line = f"  {k[0]} {DIFFICULTIES[k[1]]:<7}{k[2]:<10} seed {k[3]}: factor {k[4]:.4f}, H {show(e)}"
            if target:
                line += f"; before {show(before)}, so the target was {target:.3f}: off by {(e['h'] / target - 1) * 100:+.1f} %"
            out.append(line)

    out.append("")
    out.append("Leak line, seed 1 at the data (x1.00) and at the last multiple won: per wave how far its furthest creature got")
    out.append("(percent of its road; B = that wave hit the base; . = no wave record). Result and base health at the end.")
    for level, d in cells:
        for policy in LADDER:
            unit_runs = [r for r in runs if (r["level"], r["d"], r["policy"], r["seed"], r["base"]) == (level, d, policy, 1, 1.0) and r["phase"] == "bisect"]
            if not unit_runs:
                continue
            at_data = [r for r in unit_runs if abs(r["m"] - 1) < 1e-6][-1:]
            wins = sorted((r for r in unit_runs if r["outcome"] == "won"), key=lambda r: r["m"])[-1:]
            for label, picked in (("data", at_data), ("edge", wins)):
                for r in picked:
                    out.append(f"  {level + ' ' + DIFFICULTIES[d]:<22}{policy:<9} {label} x{r['m']:<5.2f} {leak_line(r)} {r['outcome']} w{r['wave']}/{r['waves']} base {r['health']:.0f}/{r['max']:.0f}")

    out.append("")
    total_wall = sum(r["wall"] for r in runs)
    out.append(f"Cost: {len(runs)} runs in {len(edges)} bisections, {total_wall / 60:.0f} machine minutes of play "
               f"({total_wall / max(1, len(runs)):.0f} s a run, {len(runs) / max(1, len([e for e in edges if e['runs'] > 0])):.1f} runs a bisection); "
               f"{len(batches)} batches, {sum(b['seconds'] for b in batches) / 60:.0f} wall-clock minutes in all.")
    for b in batches:
        out.append(f"  batch {b['started']}: {b['units']} units on {b['workers']} workers, {b['seconds'] / 60:.1f} min")
    text = "\n".join(out) + "\n"
    open(report, "w", encoding="utf-8", newline="\n").write(text)
    print(text)


if __name__ == "__main__":
    main()
