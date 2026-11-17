#!/usr/bin/env python3
"""Seeds won per cell and multiple, from pressure records (round 74; the reading of Docs/BALANCE_CRITIC_2026-11-17.md).

    python Tools/PlayTest/pressure_wins.py <records> [<records before>]      # with two files: before -> after, side by side
    python Tools/PlayTest/pressure_wins.py <records> --bot                   # the sensible bot's counter purchases and purse
    python Tools/PlayTest/pressure_wins.py <records> --sales                 # the sensible bot's runs with a sale, per cell

Records carry the stamp of the bot, the data and the game code they were measured with (pressure_stamp.py, round 76). A file
that mixes bots, or two files measured with different (or unknown) bots, are refused unless --mixed-bots is given: the probe
is deterministic, so a difference between two bots is a bot effect, not noise, and says nothing about the data.

The table: per rung and cell, the seeds won at every multiple played (the last record of a unit and multiple counts).
--bot lists, per cell and seed at the data (x1) for the sensible rung: the wave of the first sky gun (Rocket), the sky guns
and anti-armour guns (Cannon) built by wave 14, the purse at the start of waves 10 to 14, the coins left, and the
idle-counter note (a counter trait alive at the end with its counter's price unspent).
"""
import sys
from collections import defaultdict

import pressure_stamp

DIFFICULTIES = ["Easy", "Medium", "Hard", "Psycho"]
LEVEL_NAMES = {"Level_C1_01": "Landing Site", "Level_C1_02": "Glass Meadows", "Level_C1_03": "Shard Ridge", "Level_C1_04": "Fault Line",
               "Level_C1_05": "Quartz Flats", "Level_C1_06": "Crysium Vein"}
SKY_GUN = "Rocket"
ARMOUR_GUN = "Cannon"
BY_WAVE = 14
PURSE_WAVES = range(10, 15)


def pairs(text, parse):
    out = {}
    for item in text.split(";"):
        if "=" in item:
            k, v = item.split("=")
            out[int(k)] = parse(v)
    return out


def load(path):
    last = {}
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("|")
        if p[0] != "prun" or len(p) < 27:
            continue
        new = len(p) >= 29
        r = dict(level=p[1], d=int(p[2]), policy=p[3], seed=int(p[4]), m=float(p[5]), won=p[6] == "won", wave=int(p[7]), health=float(p[9]),
                 max=float(p[10]), coins=int(p[16]), base_lost=pairs(p[20], float), moves=[s.split(":") for s in p[23].split(";") if s],
                 reach=pairs(p[19], float), purse=pairs(p[25], int) if new else {}, idle=p[26] if new else "")
        r["bot"], r["data"], r["game"] = pressure_stamp.parse(p)
        last[(r["level"], r["d"], r["policy"], r["seed"], r["m"])] = r
    return list(last.values())


def cell(level, d):
    return f"{LEVEL_NAMES.get(level, level)} {DIFFICULTIES[d]}"


def wins(runs):
    table = defaultdict(lambda: [0, 0])
    for r in runs:
        key = (r["policy"], r["level"], r["d"], r["m"])
        table[key][0] += r["won"]
        table[key][1] += 1
    return table


def print_wins(runs, before=None):
    now = wins(runs)
    old = wins(before) if before else {}
    multiples = sorted({k[3] for k in now} | {k[3] for k in old if k[:3] in {n[:3] for n in now}})
    rows = sorted({k[:3] for k in now}, key=lambda k: (k[0], k[1], k[2]))
    head = "".join(f"x{m:<12g}" if before else f"x{m:<7g}" for m in multiples)
    print(f"{'':<34}{head}" + ("   (before -> after)" if before else ""))
    for policy, level, d in rows:
        line = f"{policy:<10}{cell(level, d):<24}"
        for m in multiples:
            a = now.get((policy, level, d, m))
            b = old.get((policy, level, d, m))
            text = f"{a[0]}/{a[1]}" if a else "-"
            if before:
                text = (f"{b[0]}/{b[1]}" if b else "-") + " -> " + text
            line += f"{text:<13}" if before else f"{text:<8}"
        print(line.rstrip())


def built(run, item, by_wave):
    return [int(m[4]) for m in run["moves"] if len(m) == 5 and m[0] == "build" and m[1] == item and int(m[4]) <= by_wave]


def print_bot(runs):
    print(f"sensible bot at the data (x1): first {SKY_GUN} wave, {SKY_GUN}s and {ARMOUR_GUN}s built by wave {BY_WAVE}, purse at the start of waves "
          f"{PURSE_WAVES[0]}-{PURSE_WAVES[-1]}, coins left, base hit in, idle counter")
    at = sorted((r for r in runs if r["policy"] == "sensible" and abs(r["m"] - 1) < 1e-6), key=lambda r: (r["level"], r["d"], r["seed"]))
    for r in at:
        sky = built(r, SKY_GUN, 99)
        purse = " ".join(str(r["purse"].get(w, "-")) for w in PURSE_WAVES)
        hits = ",".join(f"w{w}" for w in sorted(r["base_lost"])) or "never"
        print(f"  {cell(r['level'], r['d']):<22} seed {r['seed']}: {'won ' if r['won'] else 'lost'} w{r['wave']:<3} first {SKY_GUN} {('w' + str(sky[0])) if sky else 'never':<6}"
              f" {SKY_GUN}s<=w{BY_WAVE} {len(built(r, SKY_GUN, BY_WAVE))}  {ARMOUR_GUN}s {len(built(r, ARMOUR_GUN, BY_WAVE))}  purse [{purse}]  left {r['coins']:<5}"
              f" base hit {hits:<16} idle {r['idle'] or '-'}")
    idle = [r for r in runs if r["policy"] == "sensible" and r["idle"]]
    total = sum(r["policy"] == "sensible" for r in runs)
    print(f"runs of the sensible bot that ended with a counter's price unspent while its target was alive: {len(idle)} of {total}")
    for r in idle:
        print(f"  {cell(r['level'], r['d'])} seed {r['seed']} x{r['m']:g}: {r['idle']} ({'won' if r['won'] else 'lost'} in wave {r['wave']})")


def print_sales(runs):
    print("sensible bot: runs in which a gun was sold (every multiple played), per cell")
    table = defaultdict(lambda: [0, 0])
    for r in runs:
        if r["policy"] == "sensible":
            key = (r["level"], r["d"])
            table[key][0] += any(m[0] == "sell" for m in r["moves"])
            table[key][1] += 1
    for (level, d), (sold, total) in sorted(table.items()):
        print(f"  {cell(level, d):<24}{sold}/{total}")
    print(f"  all cells: {sum(v[0] for v in table.values())} of {sum(v[1] for v in table.values())} runs")


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    allow = pressure_stamp.ALLOW_FLAG in sys.argv
    runs = load(args[0])
    pressure_stamp.check_one(runs, args[0], allow)
    print(f"{args[0]}: {pressure_stamp.describe(runs)}")
    if "--bot" in sys.argv:
        print_bot(runs)
    elif "--sales" in sys.argv:
        print_sales(runs)
    else:
        before = load(args[1]) if len(args) > 1 else None
        if before is not None:
            pressure_stamp.check_one(before, args[1], allow)
            print(f"{args[1]}: {pressure_stamp.describe(before)}")
            pressure_stamp.check_pair(runs, before, allow)
        print_wins(runs, before)


if __name__ == "__main__":
    main()
