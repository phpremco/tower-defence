"""Shared helpers for the VFX texture set (round 20): PNG writing, periodic noise, resampling, 2D distance-field
drawing, flipbook atlases, the axis-aligned volume renderer that turns Mantaflow grids into six-way lightmaps, and
preview shading that imitates the intended Unity shaders so contact sheets show what the game will show.

Pure numpy (Blender's bundled copy); no bpy here, so the 2D builders also run outside Blender.
"""

import os
import struct
import zlib

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "ArtSource", "Exports", "VFX")
OUT_LOW = os.path.join(OUT, "Low")


# --------------------------------------------------------------------------------------------------------- PNG I/O

def _chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def save_png(path, arr):
    """Writes a float image (H x W or H x W x 1..4, 0..1, row 0 = top) as an 8-bit PNG. Tries the Up and Sub
    filters and keeps the smaller file: smooth masks compress far better than with no filter."""
    a = np.clip(np.nan_to_num(np.asarray(arr, dtype=np.float32)), 0.0, 1.0)
    a = (a * 255.0 + 0.5).astype(np.uint8)
    if a.ndim == 2:
        a = a[..., None]
    h, w, c = a.shape
    colour_type = {1: 0, 2: 4, 3: 2, 4: 6}[c]
    up = a.copy()
    up[1:] = a[1:] - a[:-1]
    sub = a.copy()
    sub[:, 1:] = a[:, 1:] - a[:, :-1]
    best = None
    for kind, f in ((2, up), (1, sub)):
        raw = np.concatenate([np.full((h, 1), kind, np.uint8), f.reshape(h, w * c)], axis=1).tobytes()
        z = zlib.compress(raw, 7)
        if best is None or len(z) < len(best):
            best = z
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n")
        fh.write(_chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, colour_type, 0, 0, 0)))
        fh.write(_chunk(b"IDAT", best))
        fh.write(_chunk(b"IEND", b""))
    return os.path.getsize(path)


# ------------------------------------------------------------------------------------------------- periodic noise

class Perlin:
    """Gradient noise on a lattice that wraps per axis, so tiles and time loops are seamless by construction."""

    def __init__(self, seed):
        rng = np.random.default_rng(seed)
        self.perm = rng.permutation(256).astype(np.int64)
        g = rng.normal(size=(256, 3))
        self.grad = g / np.linalg.norm(g, axis=1, keepdims=True)

    def _hash(self, ix, iy, iz):
        p = self.perm
        return p[(p[(p[ix & 255] + iy) & 255] + iz) & 255]

    def noise(self, x, y, z=0.0, period=(256, 256, 256)):
        x, y, z = np.broadcast_arrays(np.asarray(x, float), np.asarray(y, float), np.asarray(z, float))
        x0, y0, z0 = np.floor(x), np.floor(y), np.floor(z)
        fx, fy, fz = x - x0, y - y0, z - z0
        x0, y0, z0 = x0.astype(np.int64), y0.astype(np.int64), z0.astype(np.int64)
        px, py, pz = period
        u, v, w = (f * f * f * (f * (f * 6 - 15) + 10) for f in (fx, fy, fz))
        out = 0.0
        for dz in (0, 1):
            for dy in (0, 1):
                for dx in (0, 1):
                    h = self._hash((x0 + dx) % px, (y0 + dy) % py, (z0 + dz) % pz)
                    g = self.grad[h]
                    dot = g[..., 0] * (fx - dx) + g[..., 1] * (fy - dy) + g[..., 2] * (fz - dz)
                    wt = (u if dx else 1 - u) * (v if dy else 1 - v) * (w if dz else 1 - w)
                    out = out + wt * dot
        return out * 1.6

    def fbm(self, x, y, z=0.0, octaves=4, period=(256, 256, 256), lac=2, gain=0.5):
        total, amp, norm = 0.0, 1.0, 0.0
        px, py, pz = period
        for o in range(octaves):
            f = lac ** o
            total = total + amp * self.noise(np.asarray(x) * f, np.asarray(y) * f, np.asarray(z) * f,
                                             (px * f, py * f, pz * f))
            norm += amp
            amp *= gain
        return total / norm


def grid(h, w):
    """Pixel-centre coordinates: x in [0, 1) left to right, y in [0, 1) top to bottom."""
    y, x = np.mgrid[0:h, 0:w].astype(np.float64)
    return (x + 0.5) / w, (y + 0.5) / h


# ----------------------------------------------------------------------------------------------------- resampling

def _weights(n_in, n_out, kind):
    """Row-stochastic resampling matrix (n_out x n_in): Catmull-Rom for upscaling, a tent of the output footprint
    for downscaling (no aliasing in the Low set)."""
    scale = n_in / n_out
    centres = (np.arange(n_out) + 0.5) * scale - 0.5
    src = np.arange(n_in)[None, :]
    d = src - centres[:, None]
    if kind == "up" or scale <= 1.0:
        a = np.abs(d)
        w = np.where(a < 1, 1.5 * a ** 3 - 2.5 * a ** 2 + 1,
                     np.where(a < 2, -0.5 * a ** 3 + 2.5 * a ** 2 - 4 * a + 2, 0.0))
    else:
        w = np.maximum(0.0, 1.0 - np.abs(d) / scale)
    w /= np.maximum(w.sum(axis=1, keepdims=True), 1e-9)
    return w


def resize(img, h, w):
    img = np.asarray(img, np.float64)
    fy, fx = img.shape[0] / h, img.shape[1] / w
    if fy == fx and fy == int(fy) and fy > 1:
        # Exact integer shrink (the Low set): a box average is exact and far cheaper than big matrix products.
        f = int(fy)
        return img.reshape(h, f, w, f, *img.shape[2:]).mean(axis=(1, 3))
    wy = _weights(img.shape[0], h, "auto")
    wx = _weights(img.shape[1], w, "auto")
    if img.ndim == 2:
        return wy @ img @ wx.T
    return np.stack([wy @ img[..., c] @ wx.T for c in range(img.shape[2])], axis=-1)


def blur(img, radius, wrap=False):
    """Separable gaussian; `wrap` keeps tiles seamless."""
    if radius <= 0:
        return img
    r = int(np.ceil(radius * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / radius) ** 2)
    k /= k.sum()
    out = np.asarray(img, np.float64)
    for axis in (0, 1):
        if wrap:
            acc = np.zeros_like(out)
            for i, kv in enumerate(k):
                acc += kv * np.roll(out, i - r, axis=axis)
            out = acc
        else:
            pad = [(0, 0)] * out.ndim
            pad[axis] = (r, r)
            p = np.pad(out, pad, mode="edge")
            acc = np.zeros_like(out)
            n = out.shape[axis]
            for i, kv in enumerate(k):
                acc += kv * np.take(p, np.arange(i, i + n), axis=axis)
            out = acc
    return out


def smoothstep(a, b, x):
    """Hermite step from a to b (b < a gives a falling edge); a zero-width edge degrades to a hard step."""
    d = np.asarray(b - a, np.float64)
    d = np.where(np.abs(d) < 1e-9, np.where(d < 0, -1e-9, 1e-9), d)
    t = np.clip((x - a) / d, 0.0, 1.0)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------------------------- 2D distance fields

def seg_dist(px, py, ax, ay, bx, by):
    """Distance from pixel coordinates to segment a-b, plus the parameter t along it (0 at a, 1 at b)."""
    dx, dy = bx - ax, by - ay
    ll = dx * dx + dy * dy + 1e-12
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / ll, 0.0, 1.0)
    return np.hypot(px - ax - t * dx, py - ay - t * dy), t


def polyline_field(px, py, pts, widths, glows, intens):
    """Core and glow of a polyline: per segment widths/glow radii/intensity (arrays of len(pts) - 1). Returns
    (core, glow) maxed over segments, each 0..1 before intensity."""
    core = np.zeros(px.shape)
    glow = np.zeros(px.shape)
    for i in range(len(pts) - 1):
        (ax, ay), (bx, by) = pts[i], pts[i + 1]
        m = max(glows[i] * 3.0, widths[i] * 2.0)
        x0, x1 = min(ax, bx) - m, max(ax, bx) + m
        y0, y1 = min(ay, by) - m, max(ay, by) + m
        sel = (px[0] >= x0) & (px[0] <= x1)
        rows = (py[:, 0] >= y0) & (py[:, 0] <= y1)
        if not sel.any() or not rows.any():
            continue
        sx, sy = np.ix_(rows, sel)
        d, _ = seg_dist(px[sx, sy], py[sx, sy], ax, ay, bx, by)
        c = intens[i] * smoothstep(widths[i], widths[i] * 0.35, d)
        g = intens[i] * np.exp(-(d / max(glows[i], 1e-6)) ** 2)
        core[sx, sy] = np.maximum(core[sx, sy], c)
        glow[sx, sy] = np.maximum(glow[sx, sy], g)
    return core, glow


# --------------------------------------------------------------------------------------------------- flipbooks

def atlas(frames, rows, cols):
    """Frames (each H x W x C) to a grid, frame 0 at the top-left, then row by row (Unity's Texture Sheet order)."""
    h, w = frames[0].shape[:2]
    c = 1 if frames[0].ndim == 2 else frames[0].shape[2]
    out = np.zeros((rows * h, cols * w, c), np.float32)
    for i, f in enumerate(frames[:rows * cols]):
        r, q = divmod(i, cols)
        out[r * h:(r + 1) * h, q * w:(q + 1) * w] = f.reshape(h, w, c)
    return out if c > 1 else out[..., 0]


def save_set(name, img, report, meta):
    """Writes the full map and its half-size Low copy, records size and meta in the report."""
    path = os.path.join(OUT, name + ".png")
    size = save_png(path, img)
    h, w = img.shape[:2]
    low = resize(img, h // 2, w // 2)
    size_low = save_png(os.path.join(OUT_LOW, name + ".png"), low)
    entry = dict(meta)
    entry.update({"file": name + ".png", "size": [w, h], "bytes": size, "low_bytes": size_low})
    report[name] = entry
    print(f"[vfx] {name}: {w}x{h} {size / 1e6:.2f} MB (low {size_low / 1e6:.2f} MB)", flush=True)
    return path


# ------------------------------------------------------------------------------------------------ volume render

def _excl_cumsum(a, axis, reverse):
    if reverse:
        a = np.flip(a, axis)
    c = np.cumsum(a, axis=axis) - 0.5 * a
    return np.flip(c, axis) if reverse else c


def render_volume(dens, emit, dx, sigma, emit_gain, ms=(0.72, 0.28, 0.2), emit_dens=None):
    """Orthographic render of a (Z, Y, X) grid seen from -Y (camera in front, looking +Y; image up = +Z).

    Returns a dict of 2D maps (row 0 = top): alpha, emissive (0..1 after exposure), and six single-light maps
    (straight colour, not premultiplied) for light arriving from +X right, -X left, +Z top, -Z bottom, +Y back
    (behind the smoke) and -Y front (camera side). Lighting is single scatter with a cheap multi-scatter lobe:
    T = a*exp(-tau) + b*exp(-tau*c), which keeps thick cores from going black the way real smoke never does.
    """
    s = np.maximum(dens, 0.0).astype(np.float32) * np.float32(sigma)
    tau_view = _excl_cumsum(s, 1, False) * dx
    t_view = np.exp(-tau_view)
    wsc = t_view * (1.0 - np.exp(-s * dx))
    total = s.sum(axis=1) * dx
    alpha = 1.0 - np.exp(-total)
    # Fire is seen through its own thin glowing gas: emission is only occluded by `emit_dens` (the soot that is
    # not burning), so a long path through the fireball's centre reads hotter than its rim.
    if emit_dens is None:
        t_emit = t_view
    else:
        se = np.maximum(emit_dens, 0.0).astype(np.float32) * np.float32(sigma)
        t_emit = np.exp(-_excl_cumsum(se, 1, False) * dx)
    em = (t_emit * np.maximum(emit, 0.0)).sum(axis=1) * dx
    emissive = 1.0 - np.exp(-em * emit_gain)
    a, b, c = ms
    maps = {"alpha": alpha, "emissive": emissive}
    for key, axis, rev in (("right", 2, True), ("left", 2, False), ("top", 0, True), ("bottom", 0, False),
                           ("back", 1, True), ("front", 1, False)):
        tau = _excl_cumsum(s, axis, rev) * dx
        tl = a * np.exp(-tau) + b * np.exp(-tau * c)
        lit = (wsc * tl).sum(axis=1)
        maps[key] = np.clip(lit / np.maximum(alpha, 1e-4), 0.0, 1.0)
    return {k: np.flipud(v) for k, v in maps.items()}


def six_way(maps):
    """Packs a render into the two Unity six-way lightmaps (VFX Graph 'Six-way Smoke Lit' layout):
    Pos = (Right, Top, Back, Alpha), Neg = (Left, Bottom, Front, Emissive)."""
    pos = np.stack([maps["right"], maps["top"], maps["back"], maps["alpha"]], axis=-1)
    neg = np.stack([maps["left"], maps["bottom"], maps["front"], maps["emissive"]], axis=-1)
    return pos, neg


# ---------------------------------------------------------------------------------------------- preview shading

def fire_ramp(t):
    """Temperature 0..1 to linear colour: deep red, orange, yellow, white core. The intended Unity gradient."""
    keys = np.array([0.0, 0.18, 0.4, 0.62, 0.82, 1.0])
    cols = np.array([[0, 0, 0], [0.35, 0.03, 0.01], [1.0, 0.22, 0.03], [1.6, 0.75, 0.18],
                     [2.2, 1.6, 0.7], [3.0, 2.8, 2.4]])
    t = np.clip(t, 0, 1)
    return np.stack([np.interp(t, keys, cols[:, i]) for i in range(3)], axis=-1)


def shade_six_way(pos, neg, light=(-0.45, 0.75, -0.48), light_col=(1.0, 0.95, 0.88), ambient=(0.10, 0.11, 0.14),
                  albedo=(0.17, 0.16, 0.155), emissive_ramp=fire_ramp, emissive_gain=1.0, bg=None):
    """What a six-way smoke shader would show: light weights are the squared components of the light direction
    (they sum to 1), ambient is the mean of the six maps."""
    lx, ly, lz = np.array(light) / np.linalg.norm(light)
    lit = (pos[..., 0] * max(lx, 0) ** 2 + neg[..., 0] * max(-lx, 0) ** 2 +
           pos[..., 1] * max(ly, 0) ** 2 + neg[..., 1] * max(-ly, 0) ** 2 +
           pos[..., 2] * max(lz, 0) ** 2 + neg[..., 2] * max(-lz, 0) ** 2)
    amb = (pos[..., :3].sum(-1) + neg[..., :3].sum(-1)) / 6.0
    col = (lit[..., None] * np.array(light_col) + amb[..., None] * np.array(ambient) * 3.0) * np.array(albedo)
    a = pos[..., 3:4]
    em = emissive_ramp(neg[..., 3]) * emissive_gain
    return composite(col, a, em, bg)


def composite(col, alpha, emissive, bg=None):
    h, w = alpha.shape[:2]
    if bg is None:
        bg = backdrop(h, w)
    out = bg * (1 - alpha) + col * alpha + emissive
    return tonemap(out)


def backdrop(h, w):
    """Dark game-ground checker so alpha and additive glow are both readable."""
    x, y = grid(h, w)
    chk = ((np.floor(x * 8) + np.floor(y * 8)) % 2)[..., None]
    return np.array([0.035, 0.04, 0.05]) + chk * np.array([0.02, 0.02, 0.025])


def tonemap(c):
    """ACES-ish filmic curve then sRGB, so previews burn out like the game's bloom does instead of clipping."""
    c = np.maximum(c, 0)
    a, b, cc, d, e = 2.51, 0.03, 2.43, 0.59, 0.14
    m = np.clip((c * (a * c + b)) / (c * (cc * c + d) + e), 0, 1)
    return np.where(m <= 0.0031308, m * 12.92, 1.055 * np.power(m, 1 / 2.4) - 0.055)


def shade_additive(core, glow, tint, core_col=(3.0, 2.9, 2.7), gain=1.0, bg=None):
    """CG-pack preview: tint * glow plus a white-hot core."""
    h, w = core.shape
    if bg is None:
        bg = backdrop(h, w)
    em = (glow[..., None] * np.array(tint) * 1.3 + core[..., None] * np.array(core_col) * 0.6) * gain
    return tonemap(bg + em)


def sheet(tiles, cols, pad=4, bgval=0.0):
    """Lays out preview tiles (equal size, RGB 0..1) in a grid with a thin gap."""
    h, w = tiles[0].shape[:2]
    rows = (len(tiles) + cols - 1) // cols
    out = np.full((rows * (h + pad) + pad, cols * (w + pad) + pad, 3), bgval, np.float32)
    for i, t in enumerate(tiles):
        r, q = divmod(i, cols)
        y, x = pad + r * (h + pad), pad + q * (w + pad)
        out[y:y + h, x:x + w] = t[..., :3]
    return out
