# Round 65: the play table of the round from two per-cell tables of r62_cells.py (before and after): the best replayed plan,
# the plans winning on most seeds, the rows of one type and the creative bot, "a -> b" where a cell moved.
# Usage: python -I Tools/PlayTest/r65_cells_table.py Tools/PlayTest/r65_cells_before.txt Tools/PlayTest/r65_cells_after.txt
import sys


def load(path):
    rows = {}
    for line in open(path, encoding="utf-8"):
        cells = [c.strip() for c in line.split("|")]
        if len(cells) > 9 and cells[1].startswith("C1_"):
            rows[cells[1]] = cells
    return rows


def moved(a, b):
    return a if a == b else a + " -> " + b


before, after = load(sys.argv[1]), load(sys.argv[2])
print("| Cell | Best plan before (wins, stars, base) | Best plan after | Plans winning 2+ | Gun row | Cannon row | Laser row | Creative bot |")
print("| --- | --- | --- | --- | --- | --- | --- | --- |")
for cell, b in before.items():
    a = after.get(cell, b)
    print("| %s | %s | %s | %s | %s | %s | %s | %s |" % (cell, b[2], a[2], moved(b[3], a[3]), moved(b[4], a[4]), moved(b[5], a[5]), moved(b[6], a[6]), moved(b[9], a[9])))
