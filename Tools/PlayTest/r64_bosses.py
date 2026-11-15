# Round 64: every level boss's health as spawned, before (git HEAD) and after (the working tree), per level and difficulty.
# Health = the creature's book health x the difficulty's health factor x the wave's multiplier x the wave's boss scale.
# Usage: python -I Tools/PlayTest/r64_bosses.py   (from the project root; prints a markdown table)
import glob, os, re, subprocess

ROOT = "Assets/_Project/Data"
DIFFICULTY = {"Easy": 0.68, "Medium": 1.0, "Hard": 1.15, "Psycho": 1.65}


def assets():
    names, health, boss = {}, {}, {}
    for meta in glob.glob(os.path.join(ROOT, "Enemies", "*.asset.meta")) + glob.glob(os.path.join(ROOT, "Difficulty", "*.asset.meta")):
        guid = re.search(r"guid: (\w+)", open(meta).read()).group(1)
        name = os.path.basename(meta)[: -len(".asset.meta")]
        names[guid] = name
        text = open(meta[:-5], encoding="utf-8-sig").read()
        m = re.search(r"^\s*_maxHealth: ([-\d.eE]+)", text, re.M)
        health[name] = float(m.group(1)) if m else 0.0
        boss[name] = "_isBoss: 1" in text
        m = re.search(r"^\s*_healthMultiplier: ([-\d.eE]+)", text, re.M)
        if "Difficulty" in meta and m:
            DIFFICULTY[name] = float(m.group(1))
    return names, health, boss


def bosses(text, names, health, boss):
    found = {}
    difficulty, wave, pending = None, 0, []
    scale = 1.0
    for line in text.split("\n"):
        m = re.match(r"\s*- Difficulty: .*guid: (\w+)", line)
        if m:
            difficulty, wave = names.get(m.group(1)), 0
            continue
        if re.match(r"\s*- _startDelay", line):
            wave += 1
            pending, multiplier, scale = [], None, 1.0
            continue
        m = re.match(r"\s*- Enemy: .*guid: (\w+)", line)
        if m and boss.get(names.get(m.group(1))):
            pending.append(names[m.group(1)])
            continue
        m = re.match(r"\s*_healthMultiplier: ([\d.eE-]+)", line)
        if m:
            multiplier = float(m.group(1))
            for name in pending:
                found.setdefault(difficulty, []).append([wave, name, health[name] * multiplier * DIFFICULTY[difficulty], multiplier, 1.0])
            continue
        m = re.match(r"\s*_bossHealthScale: ([\d.eE-]+)", line)
        if m and pending:
            for entry in found[difficulty][-len(pending):]:
                entry[2] *= float(m.group(1))
                entry[4] = float(m.group(1))
    return found


names, health, boss = assets()
print("| Level | Difficulty | Boss (wave) | Before | After | x |")
print("| --- | --- | --- | --- | --- | --- |")
for key in ["C1_01", "C1_02", "C1_03", "C1_04", "C1_05", "C1_06"]:
    path = f"{ROOT}/Waves/Waves_{key}.asset"
    old = bosses(subprocess.run(["git", "show", f"HEAD:{path}"], capture_output=True, text=True, encoding="utf-8").stdout, names, health, boss)
    new = bosses(open(path, encoding="utf-8-sig").read(), names, health, boss)
    for difficulty in ["Easy", "Medium", "Hard", "Psycho"]:
        for before, after in zip(old.get(difficulty, []), new.get(difficulty, [])):
            print(f"| {key} | {difficulty} | {after[1]} (w{after[0]}) | {before[2]:.0f} | {after[2]:.0f} | {after[2] / max(1, before[2]):.2f} |")
