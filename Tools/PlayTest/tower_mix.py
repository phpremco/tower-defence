# Round 85: which towers the bot rungs build, per difficulty: each tower's share of the tower levels bought (a build is
# one level, every upgrade one more, the upgrade counted for the tower standing on its cell) and the share of runs that
# build it at all. Reads the 'build:<Tower>:x:y:wave' and 'upgrade::x:y:wave' actions of every prun line.
# Usage: python -I Tools/PlayTest/tower_mix.py <records> [<records> ...] [--multiple 1] [--rungs laser,cannon]
# --rungs: only these rungs (default sensible,skilled, read as one); round 87: one call per archetype rung.
import sys
from collections import Counter, defaultdict

args = [a for a in sys.argv[1:] if not a.startswith("--")]
mult = None
if "--multiple" in sys.argv:
    mult = sys.argv[sys.argv.index("--multiple") + 1]
    args = [a for a in args if a != mult]
rungs = ("sensible", "skilled")
if "--rungs" in sys.argv:
    chosen = sys.argv[sys.argv.index("--rungs") + 1]
    args = [a for a in args if a != chosen]
    rungs = tuple(chosen.split(","))
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]
builds = defaultdict(Counter)
runs = Counter()
users = defaultdict(Counter)
for path in args:
    for line in open(path, encoding="utf-8"):
        f = line.rstrip("\n").split("|")
        if f[0] != "prun" or len(f) < 24 or not f[3] in rungs:
            continue
        if mult is not None and f[5] != mult:
            continue
        d = int(f[2])
        runs[d] += 1
        seen = set()
        on = {}
        for action in f[23].split(";"):
            p = action.split(":")
            if p[0] == "build" and p[1]:
                on[(p[2], p[3])] = p[1]
                builds[d][p[1]] += 1
                seen.add(p[1])
            elif p[0] == "upgrade" and (p[2], p[3]) in on:
                builds[d][on[(p[2], p[3])]] += 1
        for t in seen:
            users[d][t] += 1
towers = sorted({t for c in builds.values() for t in c})
print("| Difficulty | runs | " + " | ".join("%s levels (runs)" % t for t in towers) + " |")
print("| --- | --- | " + " | ".join("---" for _ in towers) + " |")
for d in sorted(runs):
    total = sum(builds[d].values())
    print("| %s | %d | %s |" % (DIFFS[d], runs[d], " | ".join(
        "%.0f%% (%.0f%%)" % (100.0 * builds[d][t] / total, 100.0 * users[d][t] / runs[d]) for t in towers)))
