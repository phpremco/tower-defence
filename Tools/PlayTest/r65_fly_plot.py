# Round 65: the top-down picture of how flyers fly. Draws a level's map (from the dump_map text), every flight as its
# waypoints give it (thin red) and as it is flown now (white: FlightCurve, from Tools/PlayTest/r65_air_routes.csv), and the
# places real flyers were at in play (dots from Logs/r65/fly_trace.csv, yellow the more they were rolled into a turn).
# Usage: python -I Tools/PlayTest/r65_fly_plot.py <map dump txt> <Level_C1_04> <out png> [trace csv]
import csv
import sys
from PIL import Image, ImageDraw

dump, level, out = sys.argv[1], sys.argv[2], sys.argv[3]
trace = sys.argv[4] if len(sys.argv) > 4 else None
CELL = 44

text = open(dump, encoding="utf-8").read()
block = text.split("== " + level)[1].split("== ")[0]
rows = [line[3:] for line in block.split("\n")[1:] if line[:2].strip().isdigit()]
rows.reverse()
height, width = len(rows), len(rows[0])
colours = {"=": (58, 60, 92), "P": (70, 72, 110), "G": (150, 190, 255), "H": (90, 120, 190), "B": (120, 220, 160), "S": (220, 120, 90),
           "^": (120, 96, 150), "#": (96, 80, 112), "/": (56, 50, 70), "X": (40, 40, 48), "M": (200, 170, 60), ":": (90, 70, 50), "~": (40, 90, 130), "*": (160, 70, 40)}
image = Image.new("RGB", (width * CELL, height * CELL), (22, 22, 30))
draw = ImageDraw.Draw(image)
for y in range(height):
    for x in range(width):
        colour = colours.get(rows[y][x])
        if colour:
            top = (height - 1 - y) * CELL
            draw.rectangle([x * CELL, top, (x + 1) * CELL - 1, top + CELL - 1], fill=colour)


def pixel(wx, wz):
    # World to picture: the grid is centred on the origin, a cell is one unit, row 0 is at the bottom.
    cx = wx + (width - 1) * 0.5
    cy = wz + (height - 1) * 0.5
    return (cx + 0.5) * CELL, (height - 1 - cy + 0.5) * CELL


routes = {}
for row in csv.DictReader(open("Tools/PlayTest/r65_air_routes.csv", encoding="utf-8")):
    if row["level"] == level:
        routes.setdefault((row["path"], row["kind"]), []).append(pixel(float(row["x"]), float(row["z"])))
for (path, kind), points in routes.items():
    if kind == "waypoints":
        draw.line(points, fill=(230, 70, 70), width=2)
for (path, kind), points in routes.items():
    if kind != "waypoints":
        draw.line(points, fill=(245, 245, 245), width=3)

sharpest = 0.0
if trace:
    last = {}
    for row in csv.reader(open(trace, encoding="utf-8")):
        time, ident, name, x, z, heading, body, roll = row
        px, py = pixel(float(x), float(z))
        lean = min(1.0, abs(float(roll)) / 28.0)
        colour = (int(80 + 175 * lean), int(220 - 10 * lean), int(255 - 215 * lean))
        draw.ellipse([px - 4, py - 4, px + 4, py + 4], fill=colour)
        if ident in last:
            turn = abs((float(heading) - last[ident] + 180.0) % 360.0 - 180.0)
            sharpest = max(sharpest, turn)
        last[ident] = float(heading)

image.save(out)
print("wrote", out, image.size, "sharpest change of heading between two samples in play: %.1f degrees" % sharpest)
