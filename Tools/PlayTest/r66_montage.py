"""Round 66: pastes stills side by side (optionally cropped and enlarged) into one picture for a critical look.
python -I Tools/PlayTest/r66_montage.py <out.png> <scale> <crop x0,y0,x1,y1 as shares or -> <in.png> [...]"""
import sys
from PIL import Image

out, scale, crop = sys.argv[1], float(sys.argv[2]), sys.argv[3]
tiles = []
for path in sys.argv[4:]:
    image = Image.open(path).convert("RGB")
    if crop != "-":
        x0, y0, x1, y1 = [float(v) for v in crop.split(",")]
        image = image.crop((int(x0 * image.width), int(y0 * image.height), int(x1 * image.width), int(y1 * image.height)))
    if scale != 1:
        image = image.resize((int(image.width * scale), int(image.height * scale)), Image.LANCZOS)
    tiles.append(image)
sheet = Image.new("RGB", (sum(t.width for t in tiles), max(t.height for t in tiles)), (20, 20, 24))
x = 0
for tile in tiles:
    sheet.paste(tile, (x, 0))
    x += tile.width
sheet.save(out)
print(out, sheet.size)
