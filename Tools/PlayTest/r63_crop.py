# Round 63: nearest-neighbour crop of a still for close inspection.
# Usage: python -I Tools/PlayTest/r63_crop.py <in.png> <out.png> <cx> <cy> <w> <h> <scale>
# (cx, cy) is the crop centre in pixels of the 1920x1080 still.
import sys
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
cx, cy, w, h, scale = (int(float(v)) for v in sys.argv[3:8])
image = Image.open(src)
left = max(0, min(image.width - w, cx - w // 2))
top = max(0, min(image.height - h, cy - h // 2))
crop = image.crop((left, top, left + w, top + h))
crop.resize((w * scale, h * scale), Image.NEAREST).save(dst)
print(f"{dst} from ({left},{top}) {w}x{h} x{scale}")
