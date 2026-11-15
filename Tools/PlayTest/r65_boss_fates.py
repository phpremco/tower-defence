# Round 65: where every level boss fell, from the probe traces the worker clones keep (ProbeTrace writes a "boss ..."
# line per boss: the share of the main road it had walked when it died, and whether it reached the first gate).
# Reads C:\Users\remco\TowerDefenseWorkers\w1..w5\Logs\probe_trace.txt as the last measure or replay left them.
# Usage: python -I Tools/PlayTest/r65_boss_fates.py [substring of the level name] [difficulty 0-3] > table
import os
import re
import sys

root = os.environ.get("TD_WORKERS_ROOT", r"C:\Users\remco\TowerDefenseWorkers")
want_level = sys.argv[1] if len(sys.argv) > 1 else ""
want_difficulty = sys.argv[2] if len(sys.argv) > 2 else ""
header = re.compile(r"^(.*) on difficulty (\d), seed (\d+), strategy (.*)$")
boss = re.compile(r"^boss w(\d+) (\w+) (\d+) hp (died|still standing) at ([\d.,]+) of the road after ([\d.,]+) s; gates ([\d., ]+); first gate (reached|not reached)")
names = ["Easy", "Medium", "Hard", "Psycho"]
rows = []
for n in range(1, 7):
    path = os.path.join(root, "w%d" % n, "Logs", "probe_trace.txt")
    if not os.path.exists(path):
        continue
    run = None
    for line in open(path, encoding="utf-8", errors="replace"):
        line = line.strip()
        m = header.match(line)
        if m:
            run = m.groups()
            continue
        m = boss.match(line)
        if m and run and want_level in run[0] and (not want_difficulty or run[1] == want_difficulty):
            rows.append((run[0], int(run[1]), run[3], int(run[2]), int(m.group(1)), m.group(2), int(m.group(3)), m.group(4), float(m.group(5).replace(",", ".")),
                         float(m.group(6).replace(",", ".")), m.group(7).strip().split(" ")[0].replace(",", "."), m.group(8)))
rows.sort()
print("| Level | Difficulty | Strategy | Seed | Boss (wave) | Health | Fate | Road walked | Seconds | First gate at | First gate |")
print("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
for r in rows:
    print("| %s | %s | %s | %d | %s (w%d) | %d | %s | %.2f | %.1f | %s | %s |" % (r[0], names[r[1]], r[2], r[3], r[5], r[4], r[6], r[7], r[8], r[9], r[10], r[11]))
reached = sum(1 for r in rows if r[11] == "reached")
print("\n%d boss fates, %d reached the first gate" % (len(rows), reached))
