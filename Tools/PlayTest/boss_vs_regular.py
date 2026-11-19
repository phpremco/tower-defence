# Round 85: every boss as spawned against the toughest regular creature of its level and difficulty.
# Health as spawned = book x the difficulty's health factor x the level's health scale x the wave's multiplier
# (x the wave's boss scale for a level boss; a continent boss: x the difficulty's boss share x its own share).
# "body+armour" is what the bars show; "light fire" is what a gun of the light-fire row has to shoot through
# (armour at the rules' share, 0.2), "shells" what an explosive gun has to (armour point for point).
# Usage: python -I Tools/PlayTest/boss_vs_regular.py [--root <project root>] [--waves]   (prints a markdown table)
import glob, os, re, sys

LIGHT = 0.2


def read(path):
    return open(path, encoding="utf-8-sig").read()


def field(text, name, default=0.0):
    m = re.search(r"^\s*%s: ([-\d.eE]+)" % name, text, re.M)
    return float(m.group(1)) if m else default


def load(root):
    data = os.path.join(root, "Assets", "_Project", "Data")
    enemies, difficulties = {}, {}
    for meta in glob.glob(os.path.join(data, "Enemies", "*.asset.meta")):
        guid = re.search(r"guid: (\w+)", read(meta)).group(1)
        text = read(meta[:-5])
        enemies[guid] = {
            "name": os.path.basename(meta)[: -len(".asset.meta")],
            "health": field(text, "_maxHealth"), "armor": field(text, "_armor"),
            "boss": "_isBoss: 1" in text, "flies": "_flies: 1" in text,
            "continent": re.search(r"_boss: \{fileID: 0\}", text) is None and "_isBoss: 1" in text,
        }
    for meta in glob.glob(os.path.join(data, "Difficulty", "*.asset.meta")):
        guid = re.search(r"guid: (\w+)", read(meta)).group(1)
        text = read(meta[:-5])
        difficulties[guid] = {"name": os.path.basename(meta)[: -len(".asset.meta")], "health": field(text, "_healthMultiplier", 1.0), "bossShare": field(text, "_bossHealthShare", 1.0)}
    return data, enemies, difficulties


def waves_of(text, enemies, difficulties):
    """{difficulty name: [ {multiplier, bossScale, groups:[(enemy, count)]} ]}"""
    out, current, wave = {}, None, None
    for line in text.split("\n"):
        m = re.match(r"\s*- Difficulty: .*guid: (\w+)", line)
        if m:
            current = out.setdefault(difficulties[m.group(1)]["name"], [])
            continue
        if current is None:
            continue
        if re.match(r"\s*- _startDelay", line):
            wave = {"multiplier": 1.0, "bossScale": 1.0, "groups": []}
            current.append(wave)
            continue
        m = re.match(r"\s*- Enemy: .*guid: (\w+)", line)
        if m and wave is not None:
            wave["groups"].append([enemies.get(m.group(1)), 0])
            continue
        m = re.match(r"\s*Count: (\d+)", line)
        if m and wave is not None and wave["groups"]:
            wave["groups"][-1][1] = int(m.group(1))
            continue
        m = re.match(r"\s*_healthMultiplier: ([-\d.eE]+)", line)
        if m and wave is not None:
            wave["multiplier"] = float(m.group(1))
        m = re.match(r"\s*_bossHealthScale: ([-\d.eE]+)", line)
        if m and wave is not None:
            wave["bossScale"] = float(m.group(1))
    return out


def rows(root):
    data, enemies, difficulties = load(root)
    factor = {d["name"]: d for d in difficulties.values()}
    result = []
    for path in sorted(glob.glob(os.path.join(data, "Waves", "Waves_*.asset"))):
        key = os.path.basename(path)[len("Waves_"): -len(".asset")]
        level = glob.glob(os.path.join(data, "Levels", "*%s*.asset" % key))
        scale = field(read(level[0]), "_enemyHealthScale", 1.0) if level else 1.0
        per = waves_of(read(path), enemies, difficulties)
        for name in ("Easy", "Medium", "Hard", "Psycho"):
            waves = per.get(name, [])
            base = factor[name]["health"] * scale
            best = None  # the toughest regular creature of the cell, as spawned
            for number, wave in enumerate(waves, 1):
                for enemy, count in wave["groups"]:
                    if enemy is None or enemy["boss"]:
                        continue
                    k = base * wave["multiplier"]
                    body, armor = enemy["health"] * k, enemy["armor"] * k
                    if best is None or body + armor > best[1] + best[2]:
                        best = (enemy["name"], body, armor, number)
            before = 0.0  # the bar of the toughest regular creature of the waves before (the solver's floor reads this)
            for number, wave in enumerate(waves, 1):
                for enemy, count in wave["groups"]:
                    if enemy is None or not enemy["boss"]:
                        continue
                    k = base * wave["multiplier"] * (1.0 if enemy["continent"] else wave["bossScale"])
                    if enemy["continent"]:
                        k *= factor[name]["bossShare"]
                    result.append({"level": key, "difficulty": name, "wave": number, "boss": enemy["name"], "flies": enemy["flies"],
                                   "body": enemy["health"] * k, "armor": enemy["armor"] * k, "regular": best, "continent": enemy["continent"], "before": before})
                for enemy, count in wave["groups"]:
                    if enemy is not None and not enemy["boss"]:
                        before = max(before, (enemy["health"] + enemy["armor"]) * base * wave["multiplier"])
    return result


def main():
    root = "."
    if "--root" in sys.argv:
        root = sys.argv[sys.argv.index("--root") + 1]
    print("| Level | Difficulty | Wave | Boss | Body | Armour | Body+armour | Toughest regular (wave) | Its body+armour | Boss / regular (bars) | Boss / regular (light fire) | Boss / regular (shells) | Boss / toughest before it (bars) |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    for r in rows(root):
        name, body, armor, number = r["regular"] if r["regular"] else ("-", 0.0, 0.0, 0)
        bars = (r["body"] + r["armor"]) / max(1e-6, body + armor)
        light = (r["body"] + r["armor"] / LIGHT) / max(1e-6, body + armor / LIGHT)
        print("| %s | %s | %d | %s%s | %.0f | %.0f | %.0f | %s (%d) | %.0f | x%.1f | x%.1f | x%.1f | %s |" % (
            r["level"], r["difficulty"], r["wave"], r["boss"], " (flies)" if r["flies"] else "", r["body"], r["armor"], r["body"] + r["armor"],
            name, number, body + armor, bars, light, bars, ("x%.2f" % ((r["body"] + r["armor"]) / r["before"])) if r["before"] > 0 else "-"))


if __name__ == "__main__":
    main()
