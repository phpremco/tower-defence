"""Round 84: two runs of the wave ledger lab side by side, per cell.

    python Tools/PlayTest/ledger_compare.py <base label> <label> [<label> ...] [--cells C1_01:2,C1_02:3] [--waves]

Reads Tools/PlayTest/wave_ledger_<label>.txt (WaveLedgerLab.cs, Docs/COMMANDS.md "Round 79"; r84_lab.sh writes one
with rule overrides) and prints per cell, for every run against the base: the creatures of the level (ground / air), the
ground health as solved, the median multiplier of the ground waves, the softest and toughest ground wave over it and
the spawning seconds. With --waves every wave whose bodies or multiplier moved is listed.
Used to judge what a composition rule does on paper before a solve (what a per-cell lever did, what a rule per
difficulty does to the cells that never had the lever).
"""
import re
import sys

LINE = re.compile(r"^\s+w(\d+)\s+(\S+)\s+(B\s+)?x([\d.]+)\s+\(stored x([\d.]+).*?hero ([\d.]+).*?boss x([\d.]+).*?money\s+(\d+) hp\s+(\d+)/\s*(\d+) span\s+([\d.]+)s sky\s+([\d.]+)s n\s+(\d+)/\s*(\d+)")
HEAD = re.compile(r"^(C\d_\d\d) (Easy|Medium|Hard|Psycho) \(")
NAMES = ["Easy", "Medium", "Hard", "Psycho"]


def read(label):
    cells = {}
    cell = None
    with open("Tools/PlayTest/wave_ledger_%s.txt" % label, encoding="utf-8") as handle:
        for line in handle:
            head = HEAD.match(line)
            if head:
                cell = (head.group(1), NAMES.index(head.group(2)))
                cells[cell] = []
                continue
            wave = LINE.match(line)
            if wave and cell is not None:
                cells[cell].append({
                    "wave": int(wave.group(1)), "pattern": wave.group(2), "boss": wave.group(3) is not None,
                    "x": float(wave.group(4)), "hero": float(wave.group(6)), "bossx": float(wave.group(7)),
                    "money": int(wave.group(8)), "hp": int(wave.group(9)), "airhp": int(wave.group(10)),
                    "span": float(wave.group(11)), "n": int(wave.group(13)), "air": int(wave.group(14)),
                })
            elif line.startswith("Against the stored waves"):
                cell = None
    return cells


def median(values):
    values = sorted(values)
    if not values:
        return 0.0
    half = len(values) // 2
    return values[half] if len(values) % 2 else (values[half - 1] + values[half]) / 2


def summary(waves):
    ground = [w["x"] for w in waves if not w["pattern"].startswith("A")]
    return {
        "n": sum(w["n"] for w in waves), "air": sum(w["air"] for w in waves), "hp": sum(w["hp"] for w in waves),
        "airhp": sum(w["airhp"] for w in waves), "median": median(ground), "span": sum(w["span"] for w in waves),
        "low": min(ground) / median(ground) if ground else 0, "high": max(ground) / median(ground) if ground else 0,
    }


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    waves = "--waves" in sys.argv
    only = None
    if "--cells" in sys.argv:
        only = sys.argv[sys.argv.index("--cells") + 1].split(",")
        args = [a for a in args if a != sys.argv[sys.argv.index("--cells") + 1]]
    base = read(args[0])
    runs = [(label, read(label)) for label in args[1:]]
    for cell in sorted(base):
        name = "%s:%d" % cell
        if only and name not in only:
            continue
        b = summary(base[cell])
        print("%s %-6s %-14s bodies %4d/%3d  ground hp %6d  median x%.2f  low x%.2f high x%.2f  span %4.0fs" % (
            cell[0], NAMES[cell[1]], args[0], b["n"], b["air"], b["hp"], b["median"], b["low"], b["high"], b["span"]))
        for label, run in runs:
            if cell not in run:
                continue
            r = summary(run[cell])
            print("%17s %-14s bodies %4d/%3d (x%.2f)  ground hp %6d (x%.2f)  median x%.2f (x%.2f)  low x%.2f high x%.2f  span %4.0fs (x%.2f)" % (
                "", label, r["n"], r["air"], r["n"] / max(1, b["n"]), r["hp"], r["hp"] / max(1, b["hp"]), r["median"],
                r["median"] / max(0.001, b["median"]), r["low"], r["high"], r["span"], r["span"] / max(0.001, b["span"])))
            if waves:
                for old, new in zip(base[cell], run[cell]):
                    if old["n"] != new["n"] or abs(new["x"] / max(0.001, old["x"]) - 1) > 0.03 or old["air"] != new["air"]:
                        print("%21s w%-2d %-4s%s n %3d -> %3d  air %2d -> %2d  x%.2f -> x%.2f  hp %5d -> %5d  hero %.2f -> %.2f%s" % (
                            "", new["wave"], new["pattern"], " B" if new["boss"] else "  ", old["n"], new["n"], old["air"], new["air"],
                            old["x"], new["x"], old["hp"], new["hp"], old["hero"], new["hero"],
                            "  boss x%.2f -> x%.2f" % (old["bossx"], new["bossx"]) if new["boss"] else ""))


if __name__ == "__main__":
    main()
