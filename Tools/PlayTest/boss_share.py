"""Where the lost runs of a pressure measurement fell (round 87): the boss wall against the peaks.

Per level and difficulty: the lost runs, the share of them lost in a boss wave and in the last wave, and the waves the
other losses fell in (wave: runs; P marks a peak wave), so one sees whether the mid-level peaks hurt or the level is a
safe corridor with a pass-or-fail boss at its end. Per level over all difficulties the boss-wave share is held against
the gate of Tools/PlayTest/closed_loop_thresholds.txt (boss_wave_share_most).

The boss waves and the peaks are read from the paper report of the stored waves (Tools/PlayTest/r87_waves.txt, written
by `stage.sh run Round87Lab.cs Round87Lab.Run`); run that first when the waves moved. A run's losing wave is field 8
of its `prun` record (the wave it reached), its level's wave count field 9.

    python -I Tools/PlayTest/boss_share.py <records> [<records> ...] [--rungs sensible,skilled] [--waves <r87_waves.txt>]
"""
import re
import sys
from collections import defaultdict

WAVES = "Tools/PlayTest/r87_waves.txt"
THRESHOLDS = "Tools/PlayTest/closed_loop_thresholds.txt"
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]
LEVELS = {"C1_01": "Landing Site", "C1_02": "Glass Meadows", "C1_03": "Shard Ridge", "C1_04": "Fault Line", "C1_05": "Quartz Flats", "C1_06": "Crysium Vein"}


def thresholds(path=THRESHOLDS):
    values = {}
    for line in open(path, encoding="utf-8"):
        parts = line.split()
        if len(parts) == 2 and not line.startswith("#"):
            values[parts[0]] = float(parts[1])
    return values


def marks(path=WAVES):
    """{(level key, difficulty): {"boss": set of waves, "peak": set of waves}} from the paper report."""
    out = {}
    current = None
    for line in open(path, encoding="utf-8"):
        head = re.match(r"(\S+) (Easy|Medium|Hard|Psycho) \(", line)
        if head:
            current = out.setdefault((head.group(1), DIFFS.index(head.group(2))), {"boss": set(), "peak": set()})
            continue
        wave = re.match(r"\s+wave\s+(\d+): \S+\s+(.*)", line)
        if wave and current is not None:
            if re.search(r"\bboss \w", wave.group(2)):
                current["boss"].add(int(wave.group(1)))
            if "PEAK" in wave.group(2):
                current["peak"].add(int(wave.group(1)))
    return out


def lost_runs(paths, rungs=None):
    """[(level key, difficulty, rung, wave lost in, waves of the level)] of every lost run."""
    runs = []
    for path in paths:
        for line in open(path, encoding="utf-8"):
            f = line.rstrip("\n").split("|")
            if f[0] == "prun" and len(f) > 8 and f[6] == "lost" and (not rungs or f[3] in rungs):
                runs.append((f[1].replace("Level_", ""), int(f[2]), f[3], int(f[7]), int(f[8])))
    return runs


def shares(runs, marked):
    """{level key: (lost runs, lost in a boss wave)} over every difficulty and rung."""
    per_level = defaultdict(lambda: [0, 0])
    for level, d, _, wave, _ in runs:
        per_level[level][0] += 1
        per_level[level][1] += 1 if wave in marked.get((level, d), {"boss": set()})["boss"] else 0
    return per_level


def main():
    args = sys.argv[1:]
    option = lambda name, default: args[args.index(name) + 1] if name in args else default
    paths = [a for i, a in enumerate(args) if not a.startswith("--") and (i == 0 or args[i - 1] not in ("--rungs", "--waves"))]
    if not paths:
        sys.exit(__doc__)
    rungs = set(option("--rungs", "").split(",")) - {""}
    marked = marks(option("--waves", WAVES))
    runs = lost_runs(paths, rungs)
    most = thresholds()["boss_wave_share_most"]
    print("| Cell | Lost runs | In a boss wave | In the last wave | The other losses (wave: runs; P = peak wave) |")
    print("| --- | --- | --- | --- | --- |")
    for level in sorted({r[0] for r in runs}):
        for d in range(len(DIFFS)):
            cell = [r for r in runs if r[0] == level and r[1] == d]
            if not cell:
                continue
            mark = marked.get((level, d), {"boss": set(), "peak": set()})
            boss = sum(1 for r in cell if r[3] in mark["boss"])
            last = sum(1 for r in cell if r[3] == r[4])
            other = defaultdict(int)
            for r in cell:
                if r[3] not in mark["boss"]:
                    other[r[3]] += 1
            rest = ", ".join(f"{w}{'P' if w in mark['peak'] else ''}: {n}" for w, n in sorted(other.items()))
            print(f"| {LEVELS.get(level, level)} {DIFFS[d]} | {len(cell)} | {boss} ({boss / len(cell):.0%}) | {last} ({last / len(cell):.0%}) | {rest or '-'} |")
    print("\nPer level, every difficulty and rung together (gate: at most %.0f %% of the lost runs in a boss wave):" % (most * 100))
    for level, (lost, boss) in sorted(shares(runs, marked).items()):
        peak = sum(1 for r in runs if r[0] == level and r[3] in marked.get((level, r[1]), {"peak": set()})["peak"])
        verdict = "ok" if boss <= lost * most else "OVER"
        print(f"  {LEVELS.get(level, level)}: {lost} lost, {boss} in a boss wave ({boss / lost:.0%}) {verdict}; {peak} in a peak wave ({peak / lost:.0%})")


if __name__ == "__main__":
    main()
