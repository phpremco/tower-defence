"""Round 84: the waves of a ledger lab run that ask more of the player with the hero than the difficulty's cap allows
(the solver could not lower them: the lower edge of the band, a boss on its floor), per cell.

    python Tools/PlayTest/ledger_held.py <label> [<label> ...]

Reads Tools/PlayTest/wave_ledger_<label>.txt. The lab prints the hero pressure of the waves as stored (the difficulty's
factor and the cell's scalar on top), so it is read over that scale against the cap in the cell's header.
"""
import re
import sys

HEAD = re.compile(r"^(C\d_\d\d) (Easy|Medium|Hard|Psycho) \(.*?scale x([\d.]+).*?cap ([\d.]+)")
LINE = re.compile(r"^\s+w(\d+)\s+(\S+)\s+(B\s+)?x([\d.]+).*?hero ([\d.]+)")


def read(label):
    cells = {}
    cell = None
    with open("Tools/PlayTest/wave_ledger_%s.txt" % label, encoding="utf-8") as handle:
        for line in handle:
            head = HEAD.match(line)
            if head:
                cell = (head.group(1), head.group(2))
                cells[cell] = {"scale": float(head.group(3)), "cap": float(head.group(4)), "waves": []}
                continue
            wave = LINE.match(line)
            if wave and cell:
                cells[cell]["waves"].append((int(wave.group(1)), wave.group(2), wave.group(3) is not None, float(wave.group(5))))
    return cells


def main():
    runs = [(label, read(label)) for label in sys.argv[1:]]
    for cell in sorted(runs[0][1], key=lambda c: (c[0], ["Easy", "Medium", "Hard", "Psycho"].index(c[1]))):
        row = "%s %-6s" % cell
        for label, run in runs:
            data = run[cell]
            # The model's pressure is close to linear in health near the cap, so the stored reading over the scale is the solver's.
            held = [(n, p, hero / data["scale"]) for n, p, boss, hero in data["waves"] if hero / data["scale"] > data["cap"] * 1.05]
            worst = max([h for _, _, h in held], default=0)
            row += "  | %s: %d of %d over the cap %.2f%s" % (label, len(held), len(data["waves"]), data["cap"],
                                                           (", worst %.2f (w%s)" % (worst, ",".join(str(n) for n, _, _ in held))) if held else "")
        print(row)


if __name__ == "__main__":
    main()
