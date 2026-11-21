"""Spare money at level end and how far the waves get, from a pressure measurement's records (round 88b).

Remco after round 87: "too easy on all difficulties, and the later in a level the easier it feels; maybe the money".
The bots spend what they earn, so their unspent coins say little. What says how much money a level leaves over is the
break-even of a rung (closed_loop.break_even: the geometric middle of the last multiple two of three seeds hold and
the first they lose): a defence that holds the level at x1.32 health needed only 1 / 1.32 of itself at the data, so

    spare = earned x (1 - 1 / break-even)

is the share of the level's money a player of that rung did not need. Read per difficulty, level and rung on the runs
WON at the data (multiple 1; a ladder that starts above 1, as Easy's x1.25, is read at its lowest multiple and the
table says so; there the spare is earned x (1 - multiple / break-even) besides the proxy at the data).

"Earned" in a record (field 17) is the start money plus the BOOK bounty of every kill (ProbeTrace.Earned), before the
difficulty's income multiplier. The table therefore also shows the coins really received: start + (earned - start) x
income multiplier, with the start money and the multipliers read from the level and difficulty assets; the spare is
taken on that.

The second table reads field 22 of the same won runs (wave=health:advanced:leaked; advanced / health is the
health-weighted mean reach of the wave's creatures along the road, 0 the spawn and 1 the base): how far the waves of
the first, the middle and the last third of the level get, bosses' waves aside. A level that grows harder shows a
reach that rises.

    python -I Tools/PlayTest/r88b_spare.py <records> [<records> ...] [--rungs skilled,sensible] [--waves]

--waves also prints the reach wave by wave. Run from the stage (or project) root.
"""
import math
import os
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
if "--mixed-data" not in sys.argv:
    sys.argv.append("--mixed-data")
import boss_share  # noqa: E402
import closed_loop  # noqa: E402

DIFFS = closed_loop.DIFFS
LEVELS = closed_loop.LEVELS
RUNG_ORDER = ["skilled", "sensible", "laser", "cannon"]
DATA = "Assets/_Project/Data"


def pairs(field):
    """'1=a;2=b' -> {1: 'a', 2: 'b'}."""
    out = {}
    for item in field.split(";"):
        if "=" in item:
            wave, value = item.split("=", 1)
            out[int(wave)] = value
    return out


def asset_value(path, field):
    """One serialized number of a ScriptableObject asset; None when the file or the field is missing."""
    try:
        for line in open(path, encoding="utf-8"):
            if line.strip().startswith(field + ":"):
                return float(line.split(":", 1)[1])
    except OSError:
        pass
    return None


def received(level, d, earned):
    """The coins a run really received: its start money plus its book bounties through the income multiplier."""
    income = asset_value(f"{DATA}/Difficulty/{DIFFS[d]}.asset", "_incomeMultiplier")
    share = asset_value(f"{DATA}/Difficulty/{DIFFS[d]}.asset", "_startingMoneyMultiplier")
    start = asset_value(f"{DATA}/Levels/{level}.asset", "_startingMoney")
    if income is None or share is None or start is None:
        return earned
    start = round(start * share)
    return start + (earned - start) * income


def mean(values):
    values = list(values)
    return sum(values) / len(values) if values else float("nan")


def geo(values):
    values = [v for v in values if v > 0]
    return math.exp(sum(math.log(v) for v in values) / len(values)) if values else float("nan")


def main():
    paths = [a for a in sys.argv[1:] if not a.startswith("--") and os.path.exists(a)]
    if not paths:
        sys.exit(__doc__)
    rungs_wanted = closed_loop.option("--rungs", "").split(",") if "--rungs" in sys.argv else None
    wins, played = closed_loop.load(paths)
    runs = defaultdict(list)
    for path in paths:
        for line in open(path, encoding="utf-8"):
            f = line.rstrip("\n").split("|")
            if f[0] != "prun" or len(f) < 26:
                continue
            runs[(f[1], int(f[2]), f[3])].append(f)
    try:
        marked = boss_share.marks()
    except OSError:
        marked = {}

    cells = sorted({(d, level) for (level, d, _) in runs})
    print("## Spare money at level end (runs won at the data)\n")
    print("| Difficulty | Level | Rung | Break-even | Read at | Won | Coins unspent at the end | Earned (book) | Received | Repairs | Spare = received x (1 - 1/break-even) | Spare share |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    by_difficulty = defaultdict(list)
    for d, level in cells:
        for rung in RUNG_ORDER:
            if (level, d, rung) not in runs or (rungs_wanted and rung not in rungs_wanted):
                continue
            value, row, end = closed_loop.break_even(wins, played, level, d, rung)
            multiples = sorted({float(f[5]) for f in runs[(level, d, rung)]})
            at = min((m for m in multiples if m >= 1.0), default=multiples[-1])
            won = [f for f in runs[(level, d, rung)] if float(f[5]) == at and f[6] == "won"]
            reading = f"{'under ' if end == 'low' else 'over ' if end == 'high' else ''}x{value:.2f}"
            if not won:
                print(f"| {DIFFS[d]} | {LEVELS.get(level, level)} | {rung} | {reading} | x{at:g} | 0 | - | - | - | - | - | - |")
                continue
            earned = mean(int(f[17]) for f in won)
            unspent = mean(int(f[16]) for f in won)
            repair = mean(int(f[18]) for f in won)
            share = max(0.0, 1.0 - 1.0 / value)
            paid = received(level, d, earned)
            spare = paid * share
            note = "" if at == 1.0 else f" (at x{at:g}: {paid * max(0.0, 1.0 - at / value):.0f})"
            bound = ">= " if end == "high" else "<= " if end == "low" else ""
            print(f"| {DIFFS[d]} | {LEVELS.get(level, level)} | {rung} | {reading} | x{at:g} | {len(won)} | {unspent:.0f} | {earned:.0f} | {paid:.0f} | {repair:.0f} | {bound}{spare:.0f}{note} | {bound}{share:.0%} |")
            by_difficulty[(d, rung)].append((share, spare, unspent, paid))
    print("\n| Difficulty | Rung | Cells | Mean spare share | Mean spare coins | Mean coins unspent | Mean received |")
    print("| --- | --- | --- | --- | --- | --- | --- |")
    for (d, rung), rows in sorted(by_difficulty.items(), key=lambda kv: (kv[0][0], RUNG_ORDER.index(kv[0][1]))):
        print(f"| {DIFFS[d]} | {rung} | {len(rows)} | {mean(r[0] for r in rows):.0%} | {mean(r[1] for r in rows):.0f} | {mean(r[2] for r in rows):.0f} | {mean(r[3] for r in rows):.0f} |")

    print("\n## How far the waves get, by third of the level (won runs at the data, all rungs; boss waves aside)\n")
    print("Mean reach = advanced / health of the wave (0 the spawn, 1 the base). Purse = coins in hand when the wave starts.\n")
    print("| Difficulty | Level | Runs | Reach first third | Middle | Last third | Last over first | Gate health lost: first / middle / last third | Purse at wave start: first / middle / last third |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    per_wave = {}
    for d, level in cells:
        reach = defaultdict(list)
        gate = defaultdict(list)
        purse = defaultdict(list)
        count = 0
        waves = 0
        for rung in RUNG_ORDER:
            if rungs_wanted and rung not in rungs_wanted:
                continue
            group = runs.get((level, d, rung), [])
            if not group:
                continue
            multiples = sorted({float(f[5]) for f in group})
            at = min((m for m in multiples if m >= 1.0), default=multiples[-1])
            for f in group:
                if float(f[5]) != at or f[6] != "won":
                    continue
                count += 1
                waves = int(f[8])
                lost = pairs(f[21])
                coins = pairs(f[25])
                for wave, value in pairs(f[22]).items():
                    health, advanced, _ = (float(x) for x in value.split(":"))
                    if health > 0:
                        reach[wave].append(advanced / health)
                for wave in range(1, waves + 1):
                    gate[wave].append(float(lost.get(wave, 0)))
                    if wave in coins:
                        purse[wave].append(float(coins[wave]))
        if count == 0:
            continue
        bosses = marked.get((level.replace("Level_", ""), d), {}).get("boss", set())
        thirds = [[], [], []]
        gates = [[], [], []]
        purses = [[], [], []]
        for wave in range(1, waves + 1):
            phase = (wave - 1) / max(1, waves - 1)
            third = 0 if phase < 1 / 3 else 2 if phase > 2 / 3 else 1
            if wave in bosses:
                continue
            if reach[wave]:
                thirds[third].append(mean(reach[wave]))
            gates[third].append(mean(gate[wave]))
            if purse[wave]:
                purses[third].append(mean(purse[wave]))
        r = [mean(t) for t in thirds]
        per_wave[(d, level)] = (waves, reach, bosses)
        print(f"| {DIFFS[d]} | {LEVELS.get(level, level)} | {count} | {r[0]:.2f} | {r[1]:.2f} | {r[2]:.2f} | x{r[2] / r[0] if r[0] > 0 else float('nan'):.2f} | "
              f"{mean(gates[0]):.0f} / {mean(gates[1]):.0f} / {mean(gates[2]):.0f} | {mean(purses[0]):.0f} / {mean(purses[1]):.0f} / {mean(purses[2]):.0f} |")

    if "--waves" in sys.argv:
        print("\n## Mean reach wave by wave (B = boss wave)\n")
        for (d, level), (waves, reach, bosses) in sorted(per_wave.items()):
            row = " ".join(f"{w}{'B' if w in bosses else ''}:{mean(reach[w]):.2f}" for w in range(1, waves + 1) if reach[w])
            print(f"{DIFFS[d]} {LEVELS.get(level, level)}: {row}")


if __name__ == "__main__":
    main()
