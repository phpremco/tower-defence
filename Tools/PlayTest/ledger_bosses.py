# Round 85: the boss waves of ledger lab runs side by side (Tools/PlayTest/wave_ledger_<label>.txt, r84_lab.sh).
# Per cell and boss wave: the crowd's multiplier, the boss's health multiplier and what the wave asks of the player
# with the hero against its cap, for every label given.
# Usage: python -I Tools/PlayTest/ledger_bosses.py <label> [<label> ...]
import re
import sys


def read(label):
    rows, cell, cap = {}, None, 0.0
    for line in open("Tools/PlayTest/wave_ledger_%s.txt" % label, encoding="utf-8"):
        m = re.match(r"(C\d_\d+) (\w+) \(the human lens.*cap ([\d.]+)", line)
        if m:
            cell, cap = m.group(1) + " " + m.group(2), float(m.group(3))
            continue
        m = re.match(r"\s+w(\d+)\s+\S+ B\S*\s+x([\d.]+)\s+\(stored.*hero ([\d.]+),.*boss x([\d.]+) stored", line)
        if m and cell:
            rows[(cell, int(m.group(1)))] = (float(m.group(2)), float(m.group(4)), float(m.group(3)), cap)
    return rows


def main():
    labels = sys.argv[1:]
    runs = [read(label) for label in labels]
    print("| Cell | Wave | " + " | ".join("%s: crowd, boss, hero/cap" % label for label in labels) + " |")
    print("| --- | --- |" + " --- |" * len(labels))
    for key in sorted(runs[0], key=lambda k: (k[0].split()[0], ["Easy", "Medium", "Hard", "Psycho"].index(k[0].split()[1]), k[1])):
        cells = []
        for run in runs:
            if key in run:
                crowd, boss, hero, cap = run[key]
                cells.append("x%.2f, x%.2f, %.2f/%.2f" % (crowd, boss, hero, cap))
            else:
                cells.append("-")
        print("| %s | %d | %s |" % (key[0], key[1], " | ".join(cells)))


if __name__ == "__main__":
    main()
