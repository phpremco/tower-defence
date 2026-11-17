#!/usr/bin/env python3
"""Shape of the stored waves (round 79): per cell the spawn span of every wave, its beats (stretches of spawning
separated by a pause of at least GAP seconds), the creature types and the nest groups.

  python Tools/PlayTest/wave_shape.py [--rev <git revision>] [--waves C1_05:1] [--gap 4]

Without --waves it prints the summary per cell (median and longest spawn span, beats, types, level seconds);
with --waves <key>:<difficulty> it lists every wave of that cell. A span is the time from the wave's start to
its last spawn (delay + (stagger + (n-1) x interval) x the difficulty's interval multiplier), no jitter.
"""
import argparse, glob, math, os, re, statistics, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DATA = "Assets/_Project/Data"
DIFFS = ["Easy", "Medium", "Hard", "Psycho"]


def read(path, rev):
    if rev:
        return subprocess.run(["git", "-C", ROOT, "show", f"{rev}:{path}"], capture_output=True, text=True, check=True).stdout
    with open(os.path.join(ROOT, path), encoding="utf-8-sig") as handle:
        return handle.read()


def field(text, name, default=None):
    match = re.search(r"^\s*" + re.escape(name) + r":\s*(\S+)", text, re.M)
    return match.group(1) if match else default


def enemies():
    names = {}
    for meta in glob.glob(os.path.join(ROOT, DATA, "Enemies", "*.asset.meta")):
        with open(meta, encoding="utf-8-sig") as handle:
            guid = field(handle.read(), "guid")
        with open(meta[:-5], encoding="utf-8-sig") as handle:
            text = handle.read()
        names[guid] = (os.path.basename(meta)[:-11], field(text, "_isBoss", "0") == "1")
    return names


def difficulties(rev):
    result = {}
    for name in DIFFS:
        path = f"{DATA}/Difficulty/{name}.asset"
        with open(os.path.join(ROOT, path + ".meta"), encoding="utf-8-sig") as handle:
            guid = field(handle.read(), "guid")
        text = read(path, rev)
        result[guid] = (DIFFS.index(name), float(field(text, "_countMultiplier", "1")), float(field(text, "_spawnIntervalMultiplier", "1")))
    return result


def parse(text, diffs, names):
    """-> {difficulty index: [wave]}; wave = dict(start, health, boss, groups=[dict])"""
    cells = {}
    waves = None
    wave = None
    group = None
    for line in text.splitlines():
        s = line.strip()
        if s.startswith("- Difficulty:"):
            guid = re.search(r"guid: (\w+)", s).group(1)
            waves = cells.setdefault(diffs[guid][0], [])
            continue
        if waves is None:
            continue
        if s.startswith("- _startDelay:"):
            wave = {"start": float(s.split(":")[1]), "groups": [], "health": 1.0, "boss": 1.0}
            waves.append(wave)
        elif s.startswith("- Enemy:"):
            guid = re.search(r"guid: (\w+)", s).group(1)
            group = {"enemy": names.get(guid, (guid, False))[0], "isboss": names.get(guid, (guid, False))[1]}
            wave["groups"].append(group)
        elif s.startswith("_healthMultiplier:"):
            wave["health"] = float(s.split(":")[1])
        elif s.startswith("_bossHealthScale:"):
            wave["boss"] = float(s.split(":")[1])
        elif group is not None and ":" in s and s.split(":")[0] in ("Count", "PathIndex", "Source", "Interval", "Delay", "Stagger", "Nest"):
            group[s.split(":")[0]] = float(s.split(":")[1])
    return cells


def spawns(wave, count_mul, interval_mul):
    times = []
    for g in wave["groups"]:
        n = int(g["Count"]) if g["isboss"] else max(1, math.ceil(g["Count"] * count_mul))
        for k in range(n):
            times.append((g["Delay"] + (g["Stagger"] + k * g["Interval"]) * interval_mul, g))
    times.sort(key=lambda t: t[0])
    return times


def shape(wave, count_mul, interval_mul, gap):
    times = spawns(wave, count_mul, interval_mul)
    span = times[-1][0] if times else 0.0
    beats = 1
    longest_gap = 0.0
    for a, b in zip(times, times[1:]):
        pause = b[0] - a[0]
        longest_gap = max(longest_gap, pause)
        beats += 1 if pause >= gap else 0
    types = sorted({g["enemy"] for _, g in times})
    nest = sum(1 for _, g in times if g.get("Source", 0) == 4)
    return span, beats, longest_gap, types, len(times), nest


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rev")
    parser.add_argument("--waves")
    parser.add_argument("--gap", type=float, default=4.0)
    args = parser.parse_args()
    names = enemies()
    diffs = difficulties(args.rev)
    by_index = {v[0]: v for v in diffs.values()}
    print(f"# wave shape, {'revision ' + args.rev if args.rev else 'working tree'}; a beat ends at a pause of {args.gap:g} s or more")
    if not args.waves:
        print("| Cell | Waves | Median span s | Longest span s | Mean beats | Max beats | Mean types | Spawn seconds | Creatures |")
        print("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    all_spans = []
    for key in ["C1_01", "C1_02", "C1_03", "C1_04", "C1_05", "C1_06"]:
        cells = parse(read(f"{DATA}/Waves/Waves_{key}.asset", args.rev), diffs, names)
        for d in sorted(cells):
            _, count_mul, interval_mul = by_index[d]
            rows = [shape(w, count_mul, interval_mul, args.gap) for w in cells[d]]
            if args.waves:
                if args.waves != f"{key}:{d}":
                    continue
                for i, (w, r) in enumerate(zip(cells[d], rows)):
                    print(f"w{i + 1:<2} x{w['health']:<6g} boss x{w['boss']:<6g} span {r[0]:5.1f}s beats {r[1]} longest pause {r[2]:4.1f}s n={r[4]:<3} nest={r[5]:<2} {', '.join(r[3])}")
                    for g in w["groups"]:
                        print(f"      {g['enemy']:<14} x{int(g['Count']):<3} every {g['Interval']:<5g} delay {g['Delay']:<5g} stagger {g['Stagger']:<6g} path {int(g['PathIndex'])} src {int(g['Source'])} nest {int(g.get('Nest', 0))}")
                continue
            spans = [r[0] for r in rows]
            all_spans += spans
            total = sum(spans) + sum(w["start"] for w in cells[d])
            print(f"| {key} {DIFFS[d]} | {len(rows)} | {statistics.median(spans):.0f} | {max(spans):.0f} | {statistics.mean(r[1] for r in rows):.1f} | {max(r[1] for r in rows)} | {statistics.mean(len(r[3]) for r in rows):.1f} | {total:.0f} | {sum(r[4] for r in rows)} |")
    if not args.waves and all_spans:
        print(f"\nAll cells: median span {statistics.median(all_spans):.0f} s, mean {statistics.mean(all_spans):.0f} s, longest {max(all_spans):.0f} s")


if __name__ == "__main__":
    main()
