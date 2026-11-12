# Round 51: zoomed crops of a still for close inspection of beam ends.
# Usage: python -I r51_crop.py <image> <out> <centre x> <centre y> <half size> <zoom>
# Cuts a square of 2 * half size around the centre (still pixels, top-left origin) and scales it up with nearest-neighbour
# sampling, so hard edges stay visible as hard edges instead of being smoothed by the resize.
import sys
from PIL import Image

image, out = sys.argv[1], sys.argv[2]
cx, cy, half, zoom = (int(float(v)) for v in sys.argv[3:7])
picture = Image.open(image)
box = (max(0, cx - half), max(0, cy - half), min(picture.width, cx + half), min(picture.height, cy + half))
crop = picture.crop(box)
crop.resize((crop.width * zoom, crop.height * zoom), Image.NEAREST).save(out)
print(out, box)
