"""Round 84: the before / after tables of a round from two pressure records files, in STATUS's format.

    python Tools/PlayTest/round_tables.py <records before> <records after> [<name before> <name after>]

Per cell the seeds won per multiple for both rungs in both files and the break-even of the difficulty's reference rung
(as closed_loop.py reads it: the geometric middle of the last multiple two of three seeds hold and the first they
lose), the runs won per rung, and the break-even per difficulty and level against its target. Stamps are not checked.
"""
import sys

import closed_loop as cl

REFERENCE = {0: ("weaker", 1.4), 1: ("sensible", 1.18), 2: ("sensible", 0.95), 3: ("skilled", 1.06)}


def reading(wins, played, level, d, rung):
    value, row, end = cl.break_even(wins, played, level, d, rung)
    if value is None:
        return None, "-", ""
    return value, row, {"low": "under ", "high": "over "}.get(end, "") + "x%.2f" % value


def reference(wins, played, level, d):
    rung = REFERENCE[d][0]
    if rung != "weaker":
        return reading(wins, played, level, d, rung)
    both = [reading(wins, played, level, d, r) for r in ("sensible", "skilled")]
    return min(both, key=lambda b: (b[0], b[2]))


def main():
    sys.argv.append("--mixed-data")
    before = cl.load([sys.argv[1]])
    after = cl.load([sys.argv[2]])
    names = sys.argv[3:5] if len(sys.argv) > 5 else ["before", "after"]
    print("| Cell | Sensible, %s | Sensible, %s | Skilled, %s | Skilled, %s | Reference rung's break-even, %s -> %s (target) |" % (names[0], names[1], names[0], names[1], names[0], names[1]))
    print("| --- | --- | --- | --- | --- | --- |")
    won = [0, 0, 0, 0]
    for level, title in cl.LEVELS.items():
        for d, difficulty in enumerate(cl.DIFFS):
            rows = []
            for i, (rung, (wins, played)) in enumerate((("sensible", before), ("sensible", after), ("skilled", before), ("skilled", after))):
                row = reading(wins, played, level, d, rung)[1]
                rows.append(row)
                won[i] += sum(int(x) for x in row.split() if x.isdigit())
            a = reference(before[0], before[1], level, d)[2]
            b = reference(after[0], after[1], level, d)[2]
            print("| %s %s | %s | %s | %s | %s | %s -> %s (x%s) |" % (title, difficulty, rows[0], rows[1], rows[2], rows[3], a, b, REFERENCE[d][1]))
    print("| Runs won of 360 | %d | %d | %d | %d | |" % tuple(won))
    print()
    print("| Difficulty (rung) | Target | " + " | ".join(cl.LEVELS.values()) + " |")
    print("| --- | --- | " + " | ".join("---" for _ in cl.LEVELS) + " |")
    for d, difficulty in enumerate(cl.DIFFS):
        cells = ["%s -> %s" % (reference(before[0], before[1], level, d)[2], reference(after[0], after[1], level, d)[2]) for level in cl.LEVELS]
        print("| %s (%s) | x%s | %s |" % (difficulty, REFERENCE[d][0] + (" rung" if d == 0 else ""), REFERENCE[d][1], " | ".join(cells)))
    for d, label in ((1, "Medium, skilled (guard x1.06)"), (2, "Hard, skilled (guard x1.06)")):
        cells = ["%s -> %s" % (reading(before[0], before[1], level, d, "skilled")[2], reading(after[0], after[1], level, d, "skilled")[2]) for level in cl.LEVELS]
        print("| %s | at least x1.06 for the better rung | %s |" % (label, " | ".join(cells)))


if __name__ == "__main__":
    main()
