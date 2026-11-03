"""Shared tiling detail normal maps, one per material family (round 3).

Written with numpy instead of a bake so they are seamless by construction: noise is band-limited random noise made
in the frequency domain (periodic on the tile), voronoi cells wrap around the tile edges and the normal comes from
wrapped central differences. Only isotropic detail goes in here (pores, pits, frost, grain, veinlets), because
packed UV islands are rotated: anything directional (growth lines, striations) lives in the per-asset bake instead.

Output: ArtSource/Exports/Detail/Detail_<Family>_Normal.png, 512 x 512, tangent space, +Y up (OpenGL, as Blender
and Unity expect). Run on its own:  blender --background --python Tools/Blender/detailmaps.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402

import common as C  # noqa: E402

SIZE = 512
OUT = os.path.join(C.EXPORTS, "Detail")
# World size one tile covers on a k = 1 asset (Skitter units); the README turns this into a UV tiling per asset.
TILE_WORLD = {"chitin": 0.06, "membrane": 0.08, "crystal": 0.07, "rock": 0.12,
              # Round 6 (our side, hero units): a tile of ripstop weave is 2.5 cm, a tile of plate scratches 6 cm.
              "weave": 0.025, "ceramic": 0.06}


def band_noise(n, lo, hi, seed):
    """Periodic noise with energy between `lo` and `hi` cycles per tile, normalised to [-1, 1]."""
    rng = np.random.default_rng(seed)
    f = np.fft.fft2(rng.normal(size=(n, n)))
    fy = np.fft.fftfreq(n)[:, None] * n
    fx = np.fft.fftfreq(n)[None, :] * n
    r = np.hypot(fx, fy)
    mask = np.exp(-((np.log(np.maximum(r, 1e-6)) - np.log(np.sqrt(lo * hi))) / (0.5 * np.log(hi / lo) + 1e-6)) ** 2)
    mask[0, 0] = 0.0
    out = np.real(np.fft.ifft2(f * mask))
    return out / np.abs(out).max()


def voronoi(n, cells, seed, jitter=0.9, warp=0.0):
    """Wrapped voronoi on a jittered grid: (F1, F2 - F1, random value per nearest cell). `warp` (in cells) bends
    the cell borders with periodic noise so they meander like veins or cracks."""
    rng = np.random.default_rng(seed)
    offset = rng.uniform(0.5 - jitter / 2, 0.5 + jitter / 2, (cells, cells, 2))
    value = rng.uniform(0, 1, (cells, cells))
    coords = (np.arange(n) + 0.5) / n * cells
    x, y = np.meshgrid(coords, coords)
    if warp:
        x = x + warp * band_noise(n, 2, 6, seed + 101)
        y = y + warp * band_noise(n, 2, 6, seed + 202)
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    d1 = np.full((n, n), np.inf)
    d2 = np.full((n, n), np.inf)
    v1 = np.zeros((n, n))
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            wx, wy = cx % cells, cy % cells
            px = cx + offset[wy, wx, 0]
            py = cy + offset[wy, wx, 1]
            d = np.hypot(px - x, py - y)
            closer = d < d1
            d2 = np.where(closer, d1, np.minimum(d2, d))
            v1 = np.where(closer, value[wy, wx], v1)
            d1 = np.where(closer, d, d1)
    return d1, d2 - d1, v1


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def pits(n, cells, seed, keep, radius=0.28):
    """Conical pits (no flat floor, no rim) of varied radius in a random subset of cells: punctures, not craters."""
    d1, _, v = voronoi(n, cells, seed)
    r = radius * (0.5 + 1.0 * ((v * 7.13) % 1.0))
    return np.clip(1 - d1 / r, 0, 1) ** 1.6 * (v < keep)


def network(n, cells, seed, width):
    """A shallow polygonal groove network (cell borders)."""
    _, edge, _ = voronoi(n, cells, seed)
    return 1 - smoothstep(0.0, width, edge)


def chitin(n):
    # Dense fine punctures in patches, a micro-reticulation of polygonal cells, fine grain and a few tubercles.
    patch = smoothstep(-0.3, 0.4, band_noise(n, 2, 5, 5))
    h = -0.75 * pits(n, 34, 1, 0.55, radius=0.3) * (0.4 + 0.6 * patch) - 0.35 * pits(n, 70, 2, 0.5, radius=0.35)
    h -= 0.22 * network(n, 16, 6, 0.06)
    d1, _, v = voronoi(n, 12, 3)
    h += 0.3 * (1 - smoothstep(0.0, 0.35, d1)) ** 2 * (v < 0.25)
    h += 0.2 * band_noise(n, 50, 160, 4)
    return h


def membrane(n):
    _, edge, _ = voronoi(n, 5, 11, warp=0.35)
    _, fine, _ = voronoi(n, 13, 12, warp=0.3)
    h = 0.45 * (1 - smoothstep(0.0, 0.05, edge)) + 0.18 * (1 - smoothstep(0.0, 0.04, fine))
    h += 0.6 * band_noise(n, 6, 20, 13) + 0.2 * band_noise(n, 40, 120, 14)
    return h


def crystal(n):
    h = -0.6 * pits(n, 44, 21, 0.85, radius=0.32) - 0.3 * pits(n, 90, 22, 0.8, radius=0.35)
    h += 0.12 * band_noise(n, 80, 200, 23)
    _, edge, _ = voronoi(n, 5, 24, warp=0.2)
    h -= 0.5 * (1 - smoothstep(0.0, 0.02, edge)) * (band_noise(n, 1, 3, 25) > 0)
    return h


def rock(n):
    h = 0.5 * band_noise(n, 40, 160, 31) + 0.35 * band_noise(n, 10, 40, 32)
    h -= 0.9 * pits(n, 22, 33, 0.5)
    _, edge, _ = voronoi(n, 4, 34, warp=0.3)
    h -= 0.8 * (1 - smoothstep(0.0, 0.025, edge)) * (band_noise(n, 1, 3, 35) > -0.1)
    return h


def weave(n):
    """Ripstop: a plain weave (over-under threads, 32 per tile) with a heavier thread every 8, plus irregularity."""
    threads, rip = 32, 8
    c = (np.arange(n) + 0.5) / n * threads
    u, v = np.meshgrid(c, c)
    warp = np.sqrt(np.clip(1 - np.abs(2 * (v % 1.0) - 1), 0, 1))
    weft = np.sqrt(np.clip(1 - np.abs(2 * (u % 1.0) - 1), 0, 1))
    over = ((np.floor(u) + np.floor(v)) % 2) == 0
    # The thread on top bulges along its length between crossings.
    h = np.where(over, warp * (0.75 + 0.25 * np.sin(np.pi * (u % 1.0))), weft * (0.75 + 0.25 * np.sin(np.pi * (v % 1.0))))
    heavy = np.maximum(1 - smoothstep(0.0, 0.55, np.abs(((v + 0.5) % rip) - 0.5)),
                       1 - smoothstep(0.0, 0.55, np.abs(((u + 0.5) % rip) - 0.5)))
    h = h * (1 + 0.5 * heavy) + 0.25 * heavy
    h += 0.12 * band_noise(n, 3, 12, 71) + 0.08 * band_noise(n, 60, 200, 72)
    return h


def ceramic(n):
    """Micro-scratches in all directions over a faint orange peel, with a few tiny pits."""
    rng = np.random.default_rng(81)
    c = (np.arange(n) + 0.5) / n
    x, y = np.meshgrid(c, c)
    h = 0.15 * band_noise(n, 8, 30, 82)
    for _ in range(70):
        p0 = rng.uniform(0, 1, 2)
        a = rng.uniform(0, np.pi)
        length = rng.uniform(0.08, 0.45)
        d = np.array((np.cos(a), np.sin(a)))
        # Wrapped offsets so a scratch crossing the tile edge continues on the other side.
        dx = (x - p0[0] + 0.5) % 1.0 - 0.5
        dy = (y - p0[1] + 0.5) % 1.0 - 0.5
        t = np.clip(dx * d[0] + dy * d[1], -length / 2, length / 2)
        dist = np.hypot(dx - t * d[0], dy - t * d[1])
        width = rng.uniform(0.0012, 0.003)
        h -= rng.uniform(0.3, 1.0) * (1 - smoothstep(0.0, width, dist))
    h -= 0.6 * pits(n, 40, 83, 0.12, radius=0.25)
    return h


FAMILIES = {"chitin": (chitin, 3.0), "membrane": (membrane, 2.5), "crystal": (crystal, 2.5), "rock": (rock, 4.0),
            "weave": (weave, 2.2), "ceramic": (ceramic, 2.0)}


def normal_from_height(h, strength):
    """Wrapped central differences, so the tile edges match."""
    h = h - h.mean()
    h = h / max(np.abs(h).max(), 1e-6)
    du = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dv = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    nrm = np.dstack([-du * strength, -dv * strength, np.ones_like(h)])
    return nrm / np.linalg.norm(nrm, axis=2, keepdims=True)


def build(size=SIZE, names=None):
    os.makedirs(OUT, exist_ok=True)
    paths = {}
    for name, (fn, strength) in FAMILIES.items():
        if names is not None and name not in names:
            continue
        n = normal_from_height(fn(size), strength)
        # Rows are written bottom-up (Blender image order), so row index grows with V and +Y is up.
        rgba = np.dstack([n * 0.5 + 0.5, np.ones((size, size))])
        path = os.path.join(OUT, f"Detail_{name.capitalize()}_Normal.png")
        C.array_to_image(rgba, path, colorspace="Non-Color")
        paths[name] = path
        C.log("detail map", path)
    return paths


def ensure():
    """Paths of the detail maps, building them when one is missing."""
    paths = {name: os.path.join(OUT, f"Detail_{name.capitalize()}_Normal.png") for name in FAMILIES}
    missing = [name for name, p in paths.items() if not os.path.exists(p)]
    if missing:
        build(names=missing)
    return paths


if __name__ == "__main__":
    build()
