"""The closed loop of the balance (round 87): one scalar per LEVEL from a pressure measurement.

A wave's health is a formula (difficulty x level scalar x ramp x kind x peak; Docs/WAVE_GRAMMAR.md, "The solver"). How
tough a level is as played is measured: the probe plays each of its four cells at a ladder of health multiples, and the
break-even of the cell's reference rung (the geometric middle of the last multiple two of three seeds hold and the
first they lose) says how far the cell is from its target. The level's scalar (Tools/PlayTest/wave_tuning.txt,
"KEY scalar") is a factor on the health of every ordinary creature of the level, as the probe's multiple is, so

    new scalar = old scalar x the geometric mean over the level's four cells of (measured break-even / target)

Rounds 83 to 86 kept a scalar per cell; the critic of round 86 read those 24 numbers as the level's effect plus noise.
What the four cells of a level still disagree on after the level's factor is the difficulty's (--centre proposes
BalanceRules.DifficultyScale from the mean residual per difficulty) or a question about the level.

THE THRESHOLDS ARE FROZEN in Tools/PlayTest/closed_loop_thresholds.txt (2026-11-20, before any measurement of round
87; ClosedLoopAcceptanceTests reads the same file). They are loaded below as named constants: targets Easy weaker
rung x1.66, Medium sensible x1.32, Hard sensible x0.95, Psycho skilled x1.06; a cell is accepted when its reference
rung reads within one ladder step of its target on both sides (Easy too); on Medium and Hard the best of the better
rungs (skilled, laser, cannon) must itself read x1.06; the laser and the cannon archetype read within one ladder step
of each other; of a level's lost runs at most 40 % fall in a boss wave.

    python -I Tools/PlayTest/closed_loop.py <records> [<records> ...]     the table, the gates and the proposed scalars
    ... --write          also writes Tools/PlayTest/wave_tuning.txt (then the solve stage, then measure again)
    ... --centre         also proposes BalanceRules.DifficultyScale (the factor in use x the difficulty's mean residual)
    ... --levels 0,1     only these campaign nodes        --step 1.6   the most one pass moves a scalar
    ... --mixed-data     records of more than one data state are read as one measurement

Ladders: Tools/PlayTest/r87_measure.sh plays Easy on x1.25 to x2.21, Medium on x0.9 to x1.57, Hard and Psycho on the
standard ladder, with the rungs skilled, sensible, laser and cannon. Run from the folder the measurement ran in.
"""
import math
import os
import re
import sys
from collections import defaultdict

# python -I leaves the script's folder off the path.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import boss_share  # noqa: E402

TUNING = "Tools/PlayTest/wave_tuning.txt"
RULES = "Assets/_Project/Scripts/Balance/BalanceRules.cs"
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]
LEVELS = {"Level_C1_01": "Landing Site", "Level_C1_02": "Glass Meadows", "Level_C1_03": "Shard Ridge", "Level_C1_04": "Fault Line",
          "Level_C1_05": "Quartz Flats", "Level_C1_06": "Crysium Vein"}

# ---- Frozen 2026-11-20 (closed_loop_thresholds.txt is the one place they are written) ---------------------------------
FROZEN = boss_share.thresholds()
TARGET = [FROZEN["target_easy"], FROZEN["target_medium"], FROZEN["target_hard"], FROZEN["target_psycho"]]
ACCEPT_WITHIN_STEPS = FROZEN["accept_within_steps"]
LADDER_STEP = FROZEN["ladder_step"]
ROUNDING = FROZEN["rounding"]
BETTER_RUNG_LEAST = FROZEN["better_rung_least"]
ARCHETYPE_SPREAD_STEPS = FROZEN["archetype_spread_steps"]
BOSS_WAVE_SHARE_MOST = FROZEN["boss_wave_share_most"]
BOSS_WAVE_SHARE_TARGET = FROZEN["boss_wave_share_target"]
SCALAR_LEAST, SCALAR_MOST = FROZEN["scalar_least"], FROZEN["scalar_most"]
# -----------------------------------------------------------------------------------------------------------------------

EASY, MEDIUM, HARD, PSYCHO = range(4)
# The reference rung per difficulty; on Easy the weaker of the two.
REFERENCE = {EASY: ("sensible", "skilled"), MEDIUM: ("sensible",), HARD: ("sensible",), PSYCHO: ("skilled",)}
BETTER_RUNGS = ("skilled", "laser", "cannon")
ARCHETYPES = ("laser", "cannon")
SEEDS_HELD = 2


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
        # Two of three seeds hold a multiple; a coarse pass on one seed (PR_SEEDS=1) holds it with that seed.
        if wins[(level, d, rung, m)] >= min(SEEDS_HELD, played[(level, d, rung, m)]):
            held = m
        else:
            if held is None:
                return m / math.sqrt(multiples[1] / multiples[0]) if len(multiples) > 1 else m, row, "low"
            return math.sqrt(held * m), row, ""
    step = multiples[-1] / multiples[-2] if len(multiples) > 1 else 1.0
    return held * math.sqrt(step), row, "high"


def steps(ratio):
    """A ratio of two break-evens in ladder steps."""
    return math.log(ratio) / math.log(LADDER_STEP)


def text(reading):
    value, row, end = reading
    if value is None:
        return "not played"
    return f"`{row}` {'under ' if end == 'low' else 'over ' if end == 'high' else ''}x{value:.2f}"


def reference(wins, played, level, d):
    """The reference rung's reading of a cell: (rung, value, row, open end); on Easy the weaker of the two rungs."""
    readings = [(rung,) + break_even(wins, played, level, d, rung) for rung in REFERENCE[d]]
    readings = [r for r in readings if r[1] is not None]
    return min(readings, key=lambda r: r[1]) if readings else None


def read_scalars():
    scalars = {}
    try:
        for line in open(TUNING, encoding="utf-8"):
            parts = line.split()
            if len(parts) == 2:
                scalars[parts[0]] = float(parts[1])
    except FileNotFoundError:
        pass
    return scalars


def write_scalars(scalars):
    lines = [f"{key} {value:g}" for key, value in sorted(scalars.items()) if abs(value - 1.0) > 1e-6]
    open(TUNING, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")


def difficulty_scale():
    match = re.search(r"float\[\] DifficultyScale = \{([^}]*)\}", open(RULES, encoding="utf-8").read())
    return [float(v.strip().rstrip("f")) for v in match.group(1).split(",")] if match else [1.0] * len(DIFFS)


def main():
    flags = ("--levels", "--step")
    paths = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and sys.argv[i - 1] not in flags]
    if not paths:
        sys.exit(__doc__)
    wins, played = load(paths)
    most_step = float(option("--step", "1.6"))
    nodes = option("--levels", "")
    levels = sorted(LEVELS) if not nodes else [sorted(LEVELS)[int(n)] for n in nodes.split(",")]
    scalars = read_scalars()
    proposed = dict(scalars)
    within = LADDER_STEP ** ACCEPT_WITHIN_STEPS * ROUNDING
    findings = []
    factors = {}
    print("| Cell | Reference rung: seeds won per multiple, break-even (target) | Off, ladder steps | Verdict | Better rungs | Laser against cannon |")
    print("| --- | --- | --- | --- | --- | --- |")
    for level in levels:
        key = level.replace("Level_", "")
        for d in range(len(DIFFS)):
            name = f"{LEVELS[level]} {DIFFS[d]}"
            ref = reference(wins, played, level, d)
            if ref is None:
                print(f"| {name} | not measured | | | | |")
                continue
            rung, value, row, end = ref
            ratio = value / TARGET[d]
            # An open end is a bound, not a reading: it moves the level only when the bound is already past the target.
            usable = not ((end == "high" and ratio < 1.0) or (end == "low" and ratio > 1.0))
            factors[(key, d)] = ratio if usable else 1.0
            verdict = "accepted"
            if end:
                verdict = f"{'holds' if end == 'high' else 'loses'} its whole ladder: no reading"
            elif ratio > within or ratio < 1.0 / within:
                verdict = "more than a step off"
            if verdict != "accepted":
                findings.append(f"{name}: the {rung} rung {text((value, row, end))}, target x{TARGET[d]:g}: {verdict}")
            better_text = ""
            if d in (MEDIUM, HARD):
                better = [(r,) + break_even(wins, played, level, d, r) for r in BETTER_RUNGS]
                better = [b for b in better if b[1] is not None]
                best = max(better, key=lambda b: b[1]) if better else None
                better_text = "not played" if best is None else f"best {best[0]} {text(best[1:])}"
                if best is None or (best[3] != "high" and best[1] < BETTER_RUNG_LEAST / ROUNDING):
                    findings.append(f"{name}: no better rung reads x{BETTER_RUNG_LEAST:g} ({better_text})")
                    better_text += " UNDER"
            laser, cannon = (break_even(wins, played, level, d, a) for a in ARCHETYPES)
            spread_text = "not played"
            if laser[0] is None or cannon[0] is None:
                findings.append(f"{name}: the laser and cannon archetypes were not both played")
            if laser[0] is not None and cannon[0] is not None:
                spread = abs(steps(laser[0] / cannon[0]))
                spread_text = f"laser {text(laser)}, cannon {text(cannon)}: {spread:.1f} steps"
                if laser[0] / cannon[0] > LADDER_STEP ** ARCHETYPE_SPREAD_STEPS * ROUNDING or cannon[0] / laser[0] > LADDER_STEP ** ARCHETYPE_SPREAD_STEPS * ROUNDING:
                    findings.append(f"{name}: the archetypes are {spread:.1f} ladder steps apart ({spread_text})")
                    spread_text += " APART"
            print(f"| {name} | {rung} {text((value, row, end))} (x{TARGET[d]:g}) | {steps(ratio):+.1f}{' (bound)' if end else ''} | {verdict} | {better_text} | {spread_text} |")

    print("\n| Level | Scalar | Cells measured/target (Easy Medium Hard Psycho) | Factor | New scalar |")
    print("| --- | --- | --- | --- | --- |")
    level_factor = {}
    for level in levels:
        key = level.replace("Level_", "")
        cells = [factors[(key, d)] for d in range(len(DIFFS)) if (key, d) in factors]
        if not cells:
            continue
        old = scalars.get(key, 1.0)
        mean = math.exp(sum(math.log(v) for v in cells) / len(cells))
        factor = max(1.0 / most_step, min(most_step, mean))
        new = round(max(SCALAR_LEAST, min(SCALAR_MOST, old * factor)), 3)
        level_factor[key] = mean
        proposed[key] = new
        note = " (on its bound: look at the level)" if not SCALAR_LEAST < old * factor < SCALAR_MOST else ""
        row = " ".join(f"{factors[(key, d)]:.2f}" if (key, d) in factors else "-" for d in range(len(DIFFS)))
        print(f"| {LEVELS[level]} | {old:g} | {row} | x{factor:.3f} | {new:g}{note} |")

    try:
        lost = boss_share.lost_runs(paths)
        for key, (runs, boss) in sorted(boss_share.shares(lost, boss_share.marks()).items()):
            if boss > runs * BOSS_WAVE_SHARE_MOST:
                findings.append(f"{LEVELS.get('Level_' + key, key)}: {boss} of {runs} lost runs fell in a boss wave ({boss / runs:.0%}, at most {BOSS_WAVE_SHARE_MOST:.0%}, aim {BOSS_WAVE_SHARE_TARGET:.0%})")
    except FileNotFoundError:
        findings.append(f"the boss-wave share was not read: {boss_share.WAVES} is missing (stage.sh run Round87Lab.cs Round87Lab.Run)")

    print(f"\nGates (frozen 2026-11-20, {boss_share.THRESHOLDS}): " + ("all met" if not findings else f"{len(findings)} findings"))
    for finding in findings:
        print("  " + finding)

    if "--centre" in sys.argv:
        in_use = difficulty_scale()
        print("\nPer difficulty: the mean residual of its cells once the level's factor is taken off, and BalanceRules.DifficultyScale with it:")
        for d in range(len(DIFFS)):
            residuals = [factors[(k, dd)] / level_factor[k] for (k, dd) in factors if dd == d and k in level_factor]
            if not residuals:
                continue
            mean = math.exp(sum(math.log(v) for v in residuals) / len(residuals))
            print(f"  {DIFFS[d]}: x{mean:.3f} ({steps(mean):+.1f} steps) over {len(residuals)} levels; DifficultyScale {in_use[min(d, len(in_use) - 1)]:g} -> {in_use[min(d, len(in_use) - 1)] * mean:.3f}")

    if "--write" in sys.argv:
        write_scalars(proposed)
        print(f"\nwritten {TUNING}; now the solve stage, then measure again")


if __name__ == "__main__":
    main()
