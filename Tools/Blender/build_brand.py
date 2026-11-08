"""The Nova Terra Industries (NTI) brand mark for Operation New Dawn (2026-11-08, Remco on Spencer pass 2: the tabard
sign "staat nergens voor"; it should be the space company's logo, also shown at the base on a flag).

Three original flat variants, drawn from geometry in numpy (no fonts, no references), in the human cool palette:
- A "Nova Hex": the flat 2D form of the in-game NTI emblem (story_kit.emblem, DESIGN_NOTES "NTI emblem"): a chamfered
  hexagonal badge, a white horizon arc (Terra), a pale gold four-pointed rising star (Nova) whose long lower ray touches
  the arc, and N T I on a dark bar.
- B "Dawn Gear": a toothed industrial ring (mining) round a navy disc; a yellow sun half risen over a white horizon,
  a small cyan star above it, N T I below the horizon.
- C "Ascent Shield": an angular shield; a yellow double chevron pointing up (the climb to a new dawn) over a white
  horizon arc, stencil N T I in the lower field.

Out: ArtSource/Exports/Brand/NTI_Logo_A.png, _B.png, _C.png (1024 px, RGBA, sRGB) and NTI_Logo.png, a copy of DEFAULT:
the one texture every consumer reads (build_mercer4.py, build_flag.py, Unity decals). To switch variant, change DEFAULT
(or copy another variant over NTI_Logo.png) and re-run build_brand.py, build_mercer4.py and build_flag.py.
Also: NTI_Stencil_PF07.png (Spencer's helmet unit marking) and the comparison sheet
ArtSource/Previews/Brand/NTI_Logo_compare.png (each variant big, on cloth, and at 64/32/16 px).

`classes(path)` turns any logo PNG into soft per-colour masks (nearest palette colour) and `decal(...)` projects them
onto a part in a style mask, so a logo swap needs no code change.

Run: blender --background --factory-startup --python Tools/Blender/build_brand.py
"""

import math
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402

import common as C  # noqa: E402

OUT = os.path.join(C.EXPORTS, "Brand")
PREVIEW = os.path.join(C.REPO, "ArtSource", "Previews", "Brand")
DEFAULT = "A"
LOGO = os.path.join(OUT, "NTI_Logo.png")
STENCIL = os.path.join(OUT, "NTI_Stencil_PF07.png")
SIZE = 1024
SUPER = 4

# sRGB palette (the human side: cobalt, white, yellow accent, cyan; gunmetal and navy for depth).
PALETTE = {
    "gunmetal": (0.17, 0.18, 0.21),
    "navy": (0.05, 0.08, 0.17),
    "cobalt": (0.13, 0.32, 0.76),
    "white": (0.93, 0.94, 0.93),
    "yellow": (1.0, 0.76, 0.12),
    "star": (1.0, 0.93, 0.66),
    "cyan": (0.32, 0.93, 1.0),
}
NAMES = list(PALETTE)


# ------------------------------------------------------------------------------------------------------------
# Raster helpers (y up, coordinates -1..1, row 0 = bottom as Blender stores images)
# ------------------------------------------------------------------------------------------------------------

class Canvas:
    def __init__(self, size=SIZE, aspect=1.0, superscale=SUPER):
        self.w, self.h = int(size * aspect), size
        n = superscale
        xs = (np.arange(self.w * n, dtype=np.float32) + 0.5) / (self.w * n) * 2 * aspect - aspect
        ys = (np.arange(self.h * n, dtype=np.float32) + 0.5) / (self.h * n) * 2 - 1
        self.X, self.Y = np.meshgrid(xs, ys)
        self.n = n
        self.rgb = np.zeros(self.X.shape + (3,), np.float32)
        self.a = np.zeros(self.X.shape, np.float32)

    def paint(self, mask, colour):
        c = np.asarray(PALETTE[colour] if isinstance(colour, str) else colour, np.float32)
        self.rgb[mask] = c
        self.a[mask] = 1.0

    def erase(self, mask):
        self.a[mask] = 0.0

    def result(self):
        """Box-filtered down to the output size, colour unpremultiplied (RGBA float, sRGB)."""
        n = self.n
        h, w = self.h, self.w
        pre = self.rgb * self.a[..., None]
        pre = pre.reshape(h, n, w, n, 3).mean(axis=(1, 3))
        a = self.a.reshape(h, n, w, n).mean(axis=(1, 3))
        rgb = pre / np.maximum(a, 1e-6)[..., None]
        return np.dstack([rgb, a])


def poly(cv, pts):
    """Even-odd fill of a polygon (list of (x, y)), evaluated only inside its bounding box."""
    pts = np.asarray(pts, np.float32)
    x0, y0 = pts.min(axis=0)
    x1, y1 = pts.max(axis=0)
    out = np.zeros(cv.X.shape, bool)
    cols = np.nonzero((cv.X[0] >= x0) & (cv.X[0] <= x1))[0]
    rows = np.nonzero((cv.Y[:, 0] >= y0) & (cv.Y[:, 0] <= y1))[0]
    if not len(cols) or not len(rows):
        return out
    X = cv.X[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1]
    Y = cv.Y[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1]
    inside = np.zeros(X.shape, bool)
    for i in range(len(pts)):
        ax, ay = pts[i]
        bx, by = pts[i - 1]
        if ay == by:
            continue
        cond = (ay > Y) != (by > Y)
        xc = (bx - ax) * (Y - ay) / (by - ay) + ax
        inside ^= cond & (X < xc)
    out[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1] = inside
    return out


def circle(cv, cx, cy, r):
    return (cv.X - cx) ** 2 + (cv.Y - cy) ** 2 <= r * r


def ring(cv, cx, cy, r0, r1):
    d = (cv.X - cx) ** 2 + (cv.Y - cy) ** 2
    return (d >= r0 * r0) & (d <= r1 * r1)


def rect(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def chamfer_rect(x0, y0, x1, y1, c):
    return [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]


def chamfered_hex(r, cut):
    """Flat-top hexagon with cut corners (story_kit.chamfered_hex in 2D)."""
    hexa = [(r * math.cos(math.radians(60 * i)), r * math.sin(math.radians(60 * i))) for i in range(6)]
    out = []
    for i in range(6):
        a, b, c = np.array(hexa[i - 1]), np.array(hexa[i]), np.array(hexa[(i + 1) % 6])
        out.append(tuple(b + (a - b) * cut))
        out.append(tuple(b + (c - b) * cut))
    return out


def four_star(cx, cy, outer, inner, lower=None):
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = outer if i % 2 == 0 else inner
        if i == 4 and lower:
            r = lower
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def scaled(pts, s, dx=0.0, dy=0.0):
    return [(x * s + dx, y * s + dy) for x, y in pts]


# ------------------------------------------------------------------------------------------------------------
# A geometric, industrial letter set (cap height 1, chamfered corners; `stencil` opens bridges like a spray stencil)
# ------------------------------------------------------------------------------------------------------------

W, S = 0.66, 0.22   # glyph width and stroke
GLYPH_W = {"I": S, "-": 0.4, "1": 0.36}


def glyph(ch, stencil=False):
    """Polygons (positive) and holes for one glyph in a box (0..width, 0..1)."""
    g = 0.05 if stencil else 0.0
    w, s = GLYPH_W.get(ch, W), S
    c = 0.12
    if ch == "N":
        return [rect(0, 0, s, 1), rect(w - s, 0, w, 1), [(0, 1), (s + 0.03, 1), (w, 0), (w - s - 0.03, 0)]], []
    if ch == "T":
        return [rect(0, 1 - s, w, 1), rect((w - s) / 2, 0, (w + s) / 2, 1 - s - g)], []
    if ch == "I":
        return [rect(0, 0, s, 1)], []
    if ch == "P":
        return [rect(0, 0, s, 1 - g if stencil else 1), chamfer_rect(s + g, 0.4, w, 1, c)], [chamfer_rect(s + g, 0.4 + s * 0.8, w - s, 1 - s * 0.8, 0.04)]
    if ch == "F":
        return [rect(0, 0, s, 1), rect(s + g, 1 - s, w, 1), rect(s + g, 0.44, w * 0.85, 0.44 + s * 0.9)], []
    if ch == "0":
        outer = [chamfer_rect(0, 0, w, 1, c * 1.4)]
        holes = [chamfer_rect(s, s * 0.9, w - s, 1 - s * 0.9, 0.05)]
        if stencil:
            holes += [rect(-0.01, 0.47, w + 0.01, 0.53)]
        return outer, holes
    if ch == "7":
        return [rect(0, 1 - s, w, 1), [(w - s, 1 - s), (w, 1 - s), (s + 0.04, 0), (0.04, 0)]], []
    if ch == "1":
        return [rect(w - s, 0, w, 1), [(0, 0.72), (w - s, 1), (w - s, 0.78), (0, 0.55)]], []
    if ch == "-":
        return [rect(0, 0.42, w, 0.58)], []
    if ch == " ":
        return [], []
    raise KeyError(ch)


def text_width(text, tracking=0.16):
    return sum(GLYPH_W.get(ch, W) if ch != " " else 0.3 for ch in text) + tracking * (len(text) - 1)


def text(cv, string, cx, cy, height, colour, tracking=0.16, stencil=False, holes_colour=None):
    """Paints `string` centred on (cx, cy) with cap height `height`; holes are erased (or painted `holes_colour`)."""
    x = cx - text_width(string, tracking) * height / 2
    y0 = cy - height / 2
    for ch in string:
        polys, holes = glyph(ch, stencil)
        for p in polys:
            cv.paint(poly(cv, scaled(p, height, x, y0)), colour)
        for p in holes:
            m = poly(cv, scaled(p, height, x, y0))
            if holes_colour:
                cv.paint(m, holes_colour)
            else:
                cv.erase(m)
        x += ((GLYPH_W.get(ch, W) if ch != " " else 0.3) + tracking) * height


# ------------------------------------------------------------------------------------------------------------
# The three variants
# ------------------------------------------------------------------------------------------------------------

def variant_a(cv):
    """Nova Hex: the in-game emblem (story_kit.emblem) as a flat mark."""
    cv.paint(poly(cv, chamfered_hex(0.98, 0.18)), "gunmetal")
    cv.paint(poly(cv, chamfered_hex(0.9, 0.18)), "white")
    field = poly(cv, chamfered_hex(0.85, 0.18))
    cv.paint(field, "cobalt")
    # The new world below the horizon: a navy planet whose white limb is the horizon arc.
    planet = circle(cv, 0.0, -1.42, 1.18)
    cv.paint(field & planet, "navy")
    cv.paint(field & ring(cv, 0.0, -1.42, 1.12, 1.2), "white")
    # The rising Nova: a four-pointed star whose long lower ray touches the horizon.
    cv.paint(poly(cv, four_star(0.0, 0.2, 0.44, 0.1, lower=0.66)) & ~planet, "star")
    cv.paint(poly(cv, four_star(0.0, 0.2, 0.2, 0.05, lower=0.3)) & ~planet, "yellow")
    text(cv, "NTI", 0.0, -0.5, 0.3, "white", tracking=0.2)


def variant_b(cv):
    """Dawn Gear: a toothed industrial ring round a navy disc with a half-risen sun."""
    teeth = 16
    for i in range(teeth):
        a = 2 * math.pi * i / teeth
        ca, sa = math.cos(a), math.sin(a)
        w, r0, r1 = 0.1, 0.8, 0.98
        pts = [(ca * r0 - sa * w, sa * r0 + ca * w), (ca * r1 - sa * w * 0.75, sa * r1 + ca * w * 0.75),
               (ca * r1 + sa * w * 0.75, sa * r1 - ca * w * 0.75), (ca * r0 + sa * w, sa * r0 - ca * w)]
        cv.paint(poly(cv, pts), "gunmetal")
    cv.paint(circle(cv, 0, 0, 0.86), "gunmetal")
    cv.paint(circle(cv, 0, 0, 0.8), "white")
    disc = circle(cv, 0, 0, 0.74)
    cv.paint(disc, "cobalt")
    horizon = -0.12
    cv.paint(disc & circle(cv, 0, horizon - 0.25, 0.22 + 0.25 + 0.2) & (cv.Y > horizon), "yellow")
    cv.paint(disc & circle(cv, 0, horizon - 0.25, 0.4) & (cv.Y > horizon), "star")
    cv.paint(disc & (cv.Y <= horizon), "navy")
    cv.paint(disc & (np.abs(cv.Y - horizon) < 0.035), "white")
    cv.paint(poly(cv, four_star(0.36, 0.42, 0.13, 0.035)), "cyan")
    text(cv, "NTI", 0.0, -0.42, 0.24, "white", tracking=0.22)


def variant_c(cv):
    """Ascent Shield: a yellow double chevron climbing over a white horizon, stencil NTI below."""
    outer = [(-0.86, 0.92), (0.86, 0.92), (0.92, 0.84), (0.92, -0.2), (0.0, -0.98), (-0.92, -0.2), (-0.92, 0.84)]
    inner = [(-0.78, 0.84), (0.78, 0.84), (0.84, 0.76), (0.84, -0.16), (0.0, -0.88), (-0.84, -0.16), (-0.84, 0.76)]
    cv.paint(poly(cv, outer), "gunmetal")
    field = poly(cv, inner)
    cv.paint(field, "cobalt")
    planet = circle(cv, 0.0, -1.55, 1.6)
    cv.paint(field & planet, "navy")
    cv.paint(field & ring(cv, 0.0, -1.55, 1.55, 1.63), "white")
    for dy, colour in ((0.0, "yellow"), (0.27, "star")):
        y = 0.12 + dy
        cv.paint(poly(cv, [(-0.5, y), (0.0, y + 0.36), (0.5, y), (0.5, y + 0.16), (0.0, y + 0.52), (-0.5, y + 0.16)]) & ~planet, colour)
    text(cv, "NTI", 0.0, -0.36, 0.3, "white", tracking=0.18, stencil=True)


VARIANTS = {"A": ("Nova Hex", variant_a), "B": ("Dawn Gear", variant_b), "C": ("Ascent Shield", variant_c)}


def stencil_mark(path):
    """Spencer's unit marking: "PF-07" (Pathfinder, unit 7) as a white spray stencil on a transparent field."""
    cv = Canvas(256, aspect=3.0)
    text(cv, "PF-07", 0.0, 0.0, 1.3, "white", tracking=0.2, stencil=True)
    C.array_to_image(cv.result(), path)


# ------------------------------------------------------------------------------------------------------------
# Logo -> per-colour masks, and a planar decal for suit / kit styles
# ------------------------------------------------------------------------------------------------------------

_CACHE = {}


def classes(path=LOGO):
    """(H, W, K) float masks, one per PALETTE colour (nearest colour, times alpha), cached per path."""
    if path in _CACHE:
        return _CACHE[path]
    arr = C.image_to_array(path)
    rgb, a = arr[..., :3], arr[..., 3]
    pal = np.array([PALETTE[k] for k in NAMES], np.float32)
    d = ((rgb[..., None, :] - pal[None, None]) ** 2).sum(-1)
    idx = d.argmin(-1)
    masks = (idx[..., None] == np.arange(len(NAMES))).astype(np.float32) * a[..., None]
    _CACHE[path] = masks
    return masks


def sample(masks, u, v):
    """Bilinear lookup of (N,) uv in 0..1 (v up) into the masks; outside the image -> 0."""
    h, w, _ = masks.shape
    inside = (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
    x = np.clip(u * (w - 1), 0, w - 1.001)
    y = np.clip(v * (h - 1), 0, h - 1.001)
    x0, y0 = x.astype(int), y.astype(int)
    fx, fy = (x - x0)[:, None], (y - y0)[:, None]
    m = (masks[y0, x0] * (1 - fx) * (1 - fy) + masks[y0, x0 + 1] * fx * (1 - fy) + masks[y0 + 1, x0] * (1 - fx) * fy
         + masks[y0 + 1, x0 + 1] * fx * fy)
    return m * inside[:, None]


def decal(origin, right, up, size, colour, path=LOGO, facing=0.15, height=None):
    """A style mask: the `colour` layer of the image at `path`, projected along right x up onto points within `size`
    wide (and `height` high, default square) of `origin` (Blender world space), only on faces turned towards the
    viewer of the decal."""
    o = np.asarray(origin, float)
    r = np.asarray(right, float)
    r = r / np.linalg.norm(r)
    u_ = np.asarray(up, float)
    u_ = u_ - r * (u_ @ r)
    u_ = u_ / np.linalg.norm(u_)
    n = np.cross(r, u_)   # towards the viewer (right-handed: right x up)
    k = NAMES.index(colour)

    def mask(c):
        rel = c.p - o
        u = rel @ r / size + 0.5
        v = rel @ u_ / (height or size) + 0.5
        m = sample(classes(path), u, v)[:, k]
        return m * C.smoothstep(facing - 0.1, facing + 0.1, c.n @ n)
    return mask


# ------------------------------------------------------------------------------------------------------------
# Comparison sheet
# ------------------------------------------------------------------------------------------------------------

def over(bg, img):
    a = img[..., 3:4]
    out = bg.copy()
    out[..., :3] = bg[..., :3] * (1 - a) + img[..., :3] * a
    return out


def resize(img, size, out=512):
    """Area resample of a square RGBA image to `size` px (premultiplied), then nearest up to `out` px (pixels show)."""
    h = img.shape[0]
    f = h // size
    pre = img[..., :3] * img[..., 3:4]
    small_a = img[..., 3].reshape(size, f, size, f).mean(axis=(1, 3))
    small = pre.reshape(size, f, size, f, 3).mean(axis=(1, 3)) / np.maximum(small_a, 1e-6)[..., None]
    small = np.dstack([small, small_a])
    idx = np.arange(out) * size // out
    return small[idx][:, idx]


def cloth_bg(h, w, seed=3):
    rng = np.random.default_rng(seed)
    base = np.array((0.1, 0.24, 0.6), np.float32)
    y, x = np.mgrid[0:h, 0:w]
    weave = 0.92 + 0.08 * ((x // 3 + y // 3) % 2)
    noise = 1 + 0.06 * rng.standard_normal((h, w))
    rgb = base[None, None] * (weave * noise)[..., None]
    return np.dstack([rgb, np.ones((h, w))]).astype(np.float32)


def compare_sheet(images, path):
    cell = 512
    dark = np.zeros((cell, cell, 4), np.float32)
    dark[..., :3] = (0.05, 0.055, 0.07)
    dark[..., 3] = 1
    rows = []
    for key, img in images:
        big = resize(img, 512)
        cloth = over(cloth_bg(cell, cell), big)
        tiny = [over(dark, resize(img, s)) for s in (64, 32, 16)]
        rows.append([over(dark, big), cloth] + tiny)
    pad = 8
    cols = 5
    H = len(rows) * cell + (len(rows) + 1) * pad
    Wd = cols * cell + (cols + 1) * pad
    sheet = np.zeros((H, Wd, 4), np.float32)
    sheet[..., :3] = 0.02
    sheet[..., 3] = 1
    for r, row in enumerate(reversed(rows)):  # row 0 is the bottom of a Blender image
        y = pad + r * (cell + pad)
        for c, img in enumerate(row):
            x = pad + c * (cell + pad)
            sheet[y:y + cell, x:x + cell] = img
    C.array_to_image(sheet, path)
    C.log("sheet", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREVIEW, exist_ok=True)
    images = []
    for key, (title, fn) in VARIANTS.items():
        cv = Canvas()
        fn(cv)
        img = cv.result()
        p = os.path.join(OUT, f"NTI_Logo_{key}.png")
        C.array_to_image(img, p)
        C.log("logo", key, title, p)
        images.append((key, img))
    shutil.copy(os.path.join(OUT, f"NTI_Logo_{DEFAULT}.png"), LOGO)
    stencil_mark(STENCIL)
    compare_sheet(images, os.path.join(PREVIEW, "NTI_Logo_compare.png"))


if __name__ == "__main__":
    main()
