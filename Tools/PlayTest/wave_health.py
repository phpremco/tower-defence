#!/usr/bin/env python3
"""What a builder scale did to a level's waves, from the wave assets (round 76): per difficulty and wave the health
multiplier, the boss health scale and their product (what a wave boss without a continent-boss definition carries), now and
at a git revision.

    python Tools/PlayTest/wave_health.py C1_04 [revision]        # revision defaults to HEAD
"""
import re
import subprocess
import sys

DIFFICULTIES = ["Easy", "Medium", "Hard", "Psycho"]


def waves(text):
    """Per difficulty block, in file order: (health multiplier, boss health scale) per wave."""
    blocks = re.split(r"\n  - Difficulty: ", text)[1:]
    out = []
    for block in blocks:
        health = [float(v) for v in re.findall(r"_healthMultiplier: ([\d.eE+-]+)", block)]
        boss = [float(v) for v in re.findall(r"_bossHealthScale: ([\d.eE+-]+)", block)]
        out.append(list(zip(health, boss)))
    return out


def main():
    key = sys.argv[1]
    revision = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
    path = f"Assets/_Project/Data/Waves/Waves_{key}.asset"
    now = waves(open(path, encoding="utf-8").read())
    before = waves(subprocess.run(["git", "show", f"{revision}:{path}"], capture_output=True, text=True, encoding="utf-8", check=True).stdout)
    for d, (a, b) in enumerate(zip(before, now)):
        moved = [w for w in range(min(len(a), len(b))) if a[w] != b[w]]
        total_a = sum(h for h, _ in a)
        total_b = sum(h for h, _ in b)
        print(f"{key} {DIFFICULTIES[d]}: {len(moved)} of {len(b)} waves moved since {revision}; sum of wave multipliers {total_a:.3f} -> {total_b:.3f} (x{total_b / total_a:.3f})")
        for w in moved:
            (h0, s0), (h1, s1) = a[w], b[w]
            boss = f"  boss scale {s0:g} -> {s1:g}, wave x boss {h0 * s0:.3f} -> {h1 * s1:.3f} (x{(h1 * s1) / (h0 * s0):.3f})" if s0 != 1 or s1 != 1 else ""
            print(f"  wave {w + 1:>2}: health x{h0:g} -> x{h1:g} (x{h1 / h0:.3f}){boss}")


if __name__ == "__main__":
    main()
