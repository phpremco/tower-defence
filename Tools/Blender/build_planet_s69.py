"""Round 20/21: the S-69-CRYS world-map planet as an equirectangular texture set (procedural, numpy, no downloads).

Matches Unity's PlanetCoordinates exactly: u = (longitude + 180) / 360 with east (positive longitude) to the right,
v = (latitude + 90) / 180, so the top row of every PNG is the north pole (Unity imports PNG with v = 1 at the top).
The continents are hand-placed lobes around the CampaignCatalog MapRegion centres, domain-warped and broken up with
fractal coast noise; every Crystal Plains level node sits at least a few degrees inland.

Outputs ArtSource/Exports/Planet_S69/ (4096 x 2048) and Low/ (2048 x 1024):
  Planet_S69_Albedo.png    sRGB colour, no lighting, no clouds
  Planet_S69_Height.png    16-bit grey, 0.5 = sea level, 1 = highest peak, 0 = deepest trench
  Planet_S69_Normal.png    tangent space from the LAND relief only (sea surface flat): R = east, G = north (OpenGL/Unity)
  Planet_S69_Mask.png      R land, G specular/wetness, B glow strength, A region id (see report)
  Planet_S69_Regions.png   R region id on land, G region id incl. its sea territory, B soft region membership
  Planet_S69_Emission.png  sRGB glow for the night side (crystal fields, lava rifts, veins, the Core)
  Planet_S69_Clouds.png    RGBA, cover in all four channels; wraps in longitude
  Planet_S69_report.json

Run:  blender --background --factory-startup --python Tools/Blender/build_planet_s69.py -- [--res 4096]
        [--out <dir>] [--renders <dir>] [--compare-dir <old export dir>] [--layout <layout.json>] [--layoutonly]
        [--norender]
bpy is only used to read the comparison screenshot; everything else is numpy.
"""

import json
import math
import os
import struct
import sys
import time
import zlib
from concurrent.futures import ThreadPoolExecutor

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
F32 = np.float32

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def _arg(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith("--") else True
    return default


RES = int(_arg("--res", 4096))
OUT = _arg("--out", os.path.join(ROOT, "ArtSource", "Exports", "Planet_S69"))
RENDERS = _arg("--renders", None)
COMPARE = _arg("--compare", None)
NORENDER = bool(_arg("--norender", False))
COMPARE_DIR = _arg("--compare-dir", None)

POOL = ThreadPoolExecutor(max(4, (os.cpu_count() or 8) - 2))
CHUNK = 1 << 17
T0 = time.time()


def log(msg):
    print(f"[planet] {time.time() - T0:6.1f}s {msg}", flush=True)


# ----------------------------------------------------------------------------------------------------------------
# World layout: read from layout.json (round 21) so the texture and the Unity campaign data stay in sync.
# The region centres match Assets/_Project/Data/Levels/CampaignCatalog.asset; the node positions are a proposal a
# Unity agent applies to the CampaignCatalog MapNodes.
# ----------------------------------------------------------------------------------------------------------------

LAYOUT_PATH = _arg("--layout", os.path.join(ROOT, "ArtSource", "Exports", "Planet_S69", "layout.json"))
with open(LAYOUT_PATH, encoding="utf-8") as _f:
    LAYOUT = json.load(_f)

REGIONS = [(r["key"], r["name"], float(r["longitude"]), float(r["latitude"]), float(r["radius_degrees"]), int(r["value"]))
           for r in LAYOUT["regions"]]  # key, name, lon, lat, radius, id value (8-bit)
TARGETS = {r["key"]: float(r["target_land_percent"]) for r in LAYOUT["regions"]}
ROUGH = {r["key"]: float(r.get("coast_roughness", 1.0)) for r in LAYOUT["regions"]}
LAKE_REGIONS = set(LAYOUT.get("river_lake_regions", []))
NODES = [(float(n["longitude"]), float(n["latitude"])) for n in LAYOUT["nodes"]]
NODE_NAMES = [n["name"] for n in LAYOUT["nodes"]]
LOBES = {k: [tuple(l) for l in v] for k, v in LAYOUT["lobes"].items()}
ARCHIPELAGOS = [([tuple(p) for p in a["poly"]], float(a["radius"]), int(a["count"]), a["region"]) for a in LAYOUT["archipelagos"]]
MOUNTAINS = {k: [([tuple(p) for p in poly], float(w), float(amp)) for poly, w, amp in v] for k, v in LAYOUT["mountains"].items()}
RIFTS = [[tuple(p) for p in r] for r in LAYOUT["rifts"]]
TRENCHES = [[tuple(p) for p in t] for t in LAYOUT["trenches"]]
RIVER_COUNTS = LAYOUT.get("rivers", {})
SUBBIOMES = LAYOUT.get("crystal_plains_subbiomes", [])
CORE_LON, CORE_LAT = float(LAYOUT["core"]["longitude"]), float(LAYOUT["core"]["latitude"])
CORE_RADIUS = float(LAYOUT["core"]["crater_radius_degrees"])
CORE_ID_RADIUS = float(LAYOUT["core"]["id_radius_degrees"])
NODE_COAST_MIN = 3.0
NODE_SPACING_MIN = float(LAYOUT.get("node_min_spacing_degrees", 8.0))
GRAIN = float(LAYOUT.get("coast_grain", 0.22))


# ----------------------------------------------------------------------------------------------------------------
# Sphere helpers
# ----------------------------------------------------------------------------------------------------------------

def vec(lon, lat):
    lo, la = math.radians(lon), math.radians(lat)
    return np.array([math.cos(la) * math.cos(lo), math.cos(la) * math.sin(lo), math.sin(la)], F32)


def frame(lon, lat):
    lo, la = math.radians(lon), math.radians(lat)
    c = vec(lon, lat)
    e = np.array([-math.sin(lo), math.cos(lo), 0.0], F32)
    n = np.array([-math.sin(la) * math.cos(lo), -math.sin(la) * math.sin(lo), math.cos(la)], F32)
    return c, e, n


class Grid:
    def __init__(self, w, h):
        self.w, self.h = w, h
        lat = ((np.arange(h, dtype=F32) + 0.5) / h * 180.0 - 90.0)          # row 0 = south (Unity v = 0)
        lon = ((np.arange(w, dtype=F32) + 0.5) / w * 360.0 - 180.0)
        self.lat1, self.lon1 = lat, lon
        la, lo = np.radians(lat)[:, None], np.radians(lon)[None, :]
        self.x = (np.cos(la) * np.cos(lo)).astype(F32).ravel()
        self.y = (np.cos(la) * np.sin(lo)).astype(F32).ravel()
        self.z = np.broadcast_to(np.sin(la), (h, w)).astype(F32).ravel()
        self.latf = np.broadcast_to(lat[:, None], (h, w)).ravel().copy()

    @property
    def n(self):
        return self.w * self.h

    def p(self):
        return self.x, self.y, self.z


def chunked(fn, n, *arrays, channels=1):
    out = np.empty((channels, n) if channels > 1 else n, F32)

    def work(s):
        e = min(s + CHUNK, n)
        r = fn(*[a[s:e] for a in arrays])
        if channels > 1:
            for c in range(channels):
                out[c, s:e] = r[c]
        else:
            out[s:e] = r
    list(POOL.map(work, range(0, n, CHUNK)))
    return out


def angle_to(P, lon, lat):
    c = vec(lon, lat)
    return chunked(lambda x, y, z: np.degrees(np.arccos(np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1))), len(P[0]), *P)


def densify(poly, step=0.5):
    pts = []
    for (a, b) in zip(poly[:-1], poly[1:]):
        va, vb = vec(*a), vec(*b)
        ang = math.degrees(math.acos(float(np.clip(np.dot(va, vb), -1, 1))))
        k = max(2, int(ang / step))
        for t in np.linspace(0, 1, k, endpoint=False):
            s = math.sin(math.radians(ang))
            wa = math.sin((1 - t) * math.radians(ang)) / s
            wb = math.sin(t * math.radians(ang)) / s
            pts.append(wa * va + wb * vb)
    pts.append(vec(*poly[-1]))
    return np.array(pts, F32)


def poly_distance(P, polys):
    """Angular distance (deg) from every point to the nearest of the polylines."""
    pts = np.concatenate([densify(p) for p in polys])

    def fn(x, y, z):
        best = np.full(len(x), -1.0, F32)
        for i in range(0, len(pts), 64):
            q = pts[i:i + 64]
            d = x[:, None] * q[None, :, 0] + y[:, None] * q[None, :, 1] + z[:, None] * q[None, :, 2]
            best = np.maximum(best, d.max(axis=1))
        return np.degrees(np.arccos(np.clip(best, -1, 1)))
    return chunked(fn, len(P[0]), *P)


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    return a + (b - a) * t


def col(*rgb):
    return np.array(rgb, F32)


# ----------------------------------------------------------------------------------------------------------------
# Noise: vectorised 3D Perlin, fbm and ridged multifractal on the unit sphere (seamless, no pole pinch)
# ----------------------------------------------------------------------------------------------------------------

_GR = np.array([[1, 1, 0], [-1, 1, 0], [1, -1, 0], [-1, -1, 0], [1, 0, 1], [-1, 0, 1], [1, 0, -1], [-1, 0, -1],
                [0, 1, 1], [0, -1, 1], [0, 1, -1], [0, -1, -1]], F32)
_RNG = np.random.default_rng(6969)
_PERM = _RNG.permutation(256).astype(np.int32)
_P = np.concatenate([_PERM, _PERM])
_H = _RNG.integers(0, 12, 256)
_GX, _GY, _GZ = _GR[_H, 0].copy(), _GR[_H, 1].copy(), _GR[_H, 2].copy()


def _rot(rng):
    q, r = np.linalg.qr(rng.normal(size=(3, 3)))
    q = q * np.sign(np.diag(r))
    if np.linalg.det(q) < 0:
        q[:, 0] *= -1
    return q.astype(F32)


_ROTS = [_rot(_RNG) for _ in range(64)]
_OFFS = [(_RNG.uniform(-500, 500, 3)).astype(F32) for _ in range(64)]


def perlin(x, y, z):
    xi, yi, zi = np.floor(x), np.floor(y), np.floor(z)
    xf, yf, zf = x - xi, y - yi, z - zi
    X, Y, Z = xi.astype(np.int32) & 255, yi.astype(np.int32) & 255, zi.astype(np.int32) & 255
    u = xf * xf * xf * (xf * (xf * 6 - 15) + 10)
    v = yf * yf * yf * (yf * (yf * 6 - 15) + 10)
    w = zf * zf * zf * (zf * (zf * 6 - 15) + 10)
    A = _P[X] + Y
    B = _P[X + 1] + Y
    AA, AB, BA, BB = _P[A] + Z, _P[A + 1] + Z, _P[B] + Z, _P[B + 1] + Z

    def g(hi, dx, dy, dz):
        h = _P[hi]
        return _GX[h] * dx + _GY[h] * dy + _GZ[h] * dz
    x1, y1, z1 = xf - 1, yf - 1, zf - 1
    a = g(AA, xf, yf, zf) + u * (g(BA, x1, yf, zf) - g(AA, xf, yf, zf))
    b = g(AB, xf, y1, zf) + u * (g(BB, x1, y1, zf) - g(AB, xf, y1, zf))
    c = g(AA + 1, xf, yf, z1) + u * (g(BA + 1, x1, yf, z1) - g(AA + 1, xf, yf, z1))
    d = g(AB + 1, xf, y1, z1) + u * (g(BB + 1, x1, y1, z1) - g(AB + 1, xf, y1, z1))
    ab = a + v * (b - a)
    cd = c + v * (d - c)
    return (ab + w * (cd - ab)).astype(F32)


def _octave_point(x, y, z, seed, o, f, stretch):
    R = _ROTS[(seed * 7 + o) % 64]
    off = _OFFS[(seed * 13 + o * 5) % 64]
    zz = z * stretch[2]
    xx, yy = x * stretch[0], y * stretch[1]
    return ((R[0, 0] * xx + R[0, 1] * yy + R[0, 2] * zz) * f + off[0],
            (R[1, 0] * xx + R[1, 1] * yy + R[1, 2] * zz) * f + off[1],
            (R[2, 0] * xx + R[2, 1] * yy + R[2, 2] * zz) * f + off[2])


def fbm(P, freq, octaves, seed, gain=0.5, lac=2.03, stretch=(1, 1, 1)):
    """Roughly -1..1 (std ~0.3)."""
    def fn(x, y, z):
        acc = np.zeros(len(x), F32)
        amp, f, norm = 1.0, freq, 0.0
        for o in range(octaves):
            acc += amp * perlin(*_octave_point(x, y, z, seed, o, f, stretch))
            norm += amp
            amp *= gain
            f *= lac
        return acc * (1.6 / norm)
    return chunked(fn, len(P[0]), *P)


def ridged(P, freq, octaves, seed, gain=0.5, lac=2.07, stretch=(1, 1, 1)):
    """Musgrave ridged multifractal, 0..1, sharp crests."""
    def fn(x, y, z):
        acc = np.zeros(len(x), F32)
        weight = np.ones(len(x), F32)
        amp, f, norm = 1.0, freq, 0.0
        for o in range(octaves):
            s = 1.0 - np.abs(perlin(*_octave_point(x, y, z, seed, o, f, stretch)))
            s = s * s * weight
            weight = np.clip(s * 2.0, 0, 1)
            acc += amp * s
            norm += amp
            amp *= gain
            f *= lac
        return acc / norm
    return chunked(fn, len(P[0]), *P)


def warp(P, amount, freq, octaves, seed):
    wx = fbm(P, freq, octaves, seed)
    wy = fbm(P, freq, octaves, seed + 1)
    wz = fbm(P, freq, octaves, seed + 2)
    x, y, z = P[0] + amount * wx, P[1] + amount * wy, P[2] + amount * wz
    inv = 1.0 / np.sqrt(x * x + y * y + z * z)
    return (x * inv).astype(F32), (y * inv).astype(F32), (z * inv).astype(F32)


def worley(P, freq, seed):
    """Cellular noise on the sphere: distances (in cells) to the nearest and second-nearest feature point."""
    off = _OFFS[seed % 64]

    def fn(x, y, z):
        X, Y, Z = x * freq + off[0], y * freq + off[1], z * freq + off[2]
        xi, yi, zi = np.floor(X).astype(np.int32), np.floor(Y).astype(np.int32), np.floor(Z).astype(np.int32)
        f1 = np.full(len(x), 9.0, F32)
        f2 = np.full(len(x), 9.0, F32)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    cx, cy, cz = xi + dx, yi + dy, zi + dz
                    hs = _P[(_P[(_P[(cx + seed) & 255] + cy) & 255] + cz) & 255]
                    jx = _P[(hs + 17) & 255] / 255.0
                    jy = _P[(hs + 101) & 255] / 255.0
                    jz = _P[(hs + 199) & 255] / 255.0
                    d = (cx + jx - X) ** 2 + (cy + jy - Y) ** 2 + (cz + jz - Z) ** 2
                    f2 = np.where(d < f1, f1, np.minimum(f2, d))
                    f1 = np.minimum(f1, d)
        return np.sqrt(f1), np.sqrt(f2)
    return chunked(fn, len(P[0]), *P, channels=2)


SUBBIOME_TYPES = ["glass_meadow", "bluefern", "amethyst_upland", "shard_field", "salt_basin"]


def subbiome_weights(Pm, elev):
    """Crystal Plains sub-biomes: soft, warped nearest-seed partition of the layout's seeds, uplands follow height."""
    PWm = warp(warp(Pm, 0.10, 2.2, 3, 177), 0.03, 7.0, 3, 179)
    acc = np.zeros((len(SUBBIOME_TYPES), len(Pm[0])), F32)
    for sd in SUBBIOMES:
        c = vec(sd["longitude"], sd["latitude"])
        d = np.degrees(np.arccos(np.clip(PWm[0] * c[0] + PWm[1] * c[1] + PWm[2] * c[2], -1, 1)))
        i = SUBBIOME_TYPES.index(sd["type"])
        acc[i] = np.maximum(acc[i], np.exp(-d / 3.2))
    acc[2] += 0.6 * smooth(0.16, 0.40, elev) * acc.max(axis=0)
    acc = np.power(acc + 1e-6, 1.5)
    return acc / acc.sum(axis=0, keepdims=True)


# ----------------------------------------------------------------------------------------------------------------
# Image helpers
# ----------------------------------------------------------------------------------------------------------------

def upsample(a, H, W):
    """Bilinear, pixel-centre aligned, wraps in longitude."""
    h, w = a.shape[-2:]
    ys = (np.arange(H, dtype=F32) + 0.5) * h / H - 0.5
    xs = (np.arange(W, dtype=F32) + 0.5) * w / W - 0.5
    y0 = np.floor(ys).astype(int)
    fy = (ys - y0).astype(F32)[:, None]
    y1 = np.clip(y0 + 1, 0, h - 1)
    y0 = np.clip(y0, 0, h - 1)
    x0 = np.floor(xs).astype(int)
    fx = (xs - x0).astype(F32)[None, :]
    x1 = (x0 + 1) % w
    x0 = x0 % w
    r0, r1 = a[..., y0, :], a[..., y1, :]
    top = r0[..., x0] * (1 - fx) + r0[..., x1] * fx
    bot = r1[..., x0] * (1 - fx) + r1[..., x1] * fx
    return (top * (1 - fy) + bot * fy).astype(F32)


def blur_sphere(a, radius_deg, lat1, passes=3):
    """Approximately isotropic blur on the sphere for an equirect image (rows south->north)."""
    h, w = a.shape
    ry = max(1, int(round(radius_deg / 180.0 * h / math.sqrt(passes))))
    cosl = np.maximum(np.cos(np.radians(lat1)), 0.04)
    kx = np.clip(np.round(ry / cosl), 1, w // 2 - 1).astype(int)[:, None]
    out = a.astype(F32)
    J = np.arange(w)[None, :] + w
    for _ in range(passes):
        pad = np.concatenate([out, out, out], axis=1)
        cs = np.concatenate([np.zeros((h, 1), F32), np.cumsum(pad, axis=1, dtype=np.float64).astype(F32)], axis=1)
        out = (np.take_along_axis(cs, J + kx + 1, 1) - np.take_along_axis(cs, J - kx, 1)) / (2 * kx + 1)
        pad = np.concatenate([np.repeat(out[:1], ry + 1, 0), out, np.repeat(out[-1:], ry + 1, 0)], axis=0)
        cs = np.concatenate([np.zeros((1, w), F32), np.cumsum(pad, axis=0, dtype=np.float64).astype(F32)], axis=0)
        I = np.arange(h)[:, None] + ry + 1
        out = (cs[I + ry + 1, np.arange(w)[None, :]] - cs[I - ry, np.arange(w)[None, :]]) / (2 * ry + 1)
    return out.astype(F32)


def downsample2(a):
    if a.ndim == 2:
        return ((a[0::2, 0::2] + a[1::2, 0::2] + a[0::2, 1::2] + a[1::2, 1::2]) * 0.25).astype(a.dtype)
    return ((a[0::2, 0::2].astype(F32) + a[1::2, 0::2] + a[0::2, 1::2] + a[1::2, 1::2]) * 0.25).astype(a.dtype)


def write_png(path, img, bits=8):
    """img: float 0..1, (H, W) or (H, W, C), row 0 = BOTTOM (south); written top-down like every PNG."""
    img = np.asarray(img)
    if img.ndim == 2:
        img = img[:, :, None]
    h, w, c = img.shape
    img = img[::-1]
    if bits == 16:
        data = (np.clip(img, 0, 1) * 65535 + 0.5).astype(">u2")
    else:
        data = (np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)
    ctype = {1: 0, 2: 4, 3: 2, 4: 6}[c]
    raw = np.concatenate([np.zeros((h, 1), np.uint8), data.reshape(h, -1).view(np.uint8)], axis=1).tobytes()

    def chunk(tag, payload):
        return struct.pack(">I", len(payload)) + tag + payload + struct.pack(">I", zlib.crc32(tag + payload) & 0xffffffff)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, bits, ctype, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(raw, 6)))
        f.write(chunk(b"IEND", b""))


def srgb_to_lin(c):
    return np.power(np.clip(c, 0, 1), 2.2)


def lin_to_srgb(c):
    return np.power(np.clip(c, 0, 1), 1 / 2.2)


# ----------------------------------------------------------------------------------------------------------------
# Low-resolution layout: continents, regions, belts, ocean depth
# ----------------------------------------------------------------------------------------------------------------

def lobe_field(P, lobes):
    """1 at a lobe's middle, 0 on its rim, negative outside; union by smooth max, bays carved by smooth min."""
    x, y, z = P
    out = np.full(len(x), -4.0, F32)
    carve = []
    for (lon, lat, rx, ry, rot, sign) in lobes:
        c, e, n = frame(lon, lat)
        cd = np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1)
        a = x * e[0] + y * e[1] + z * e[2]
        b = x * n[0] + y * n[1] + z * n[2]
        th = np.degrees(np.arccos(cd))
        rr = np.sqrt(a * a + b * b) + 1e-6
        u, v = th * a / rr, th * b / rr
        cr, sr = math.cos(math.radians(rot)), math.sin(math.radians(rot))
        u, v = u * cr + v * sr, -u * sr + v * cr
        s = np.sqrt((u / rx) ** 2 + (v / ry) ** 2)
        f = (1.0 - s).astype(F32)
        if sign > 0:
            k = 0.25
            hmix = np.clip(0.5 + 0.5 * (f - out) / k, 0, 1)
            out = mix(out, f, hmix) + k * hmix * (1 - hmix)
        else:
            carve.append(f)
    for f in carve:
        out = np.minimum(out, -f * 1.4)
    return out.astype(F32)


def archipelago_field(P, poly, radius, count, seed):
    pts = densify(poly, 0.25)
    rng = np.random.default_rng(seed)
    idx = np.linspace(0, len(pts) - 1, count).astype(int)
    out = np.full(len(P[0]), -4.0, F32)
    for i in idx:
        q = pts[i] + rng.normal(0, 0.02, 3).astype(F32)
        q /= np.linalg.norm(q)
        r = radius * rng.uniform(0.6, 1.25)
        d = np.degrees(np.arccos(np.clip(P[0] * q[0] + P[1] * q[1] + P[2] * q[2], -1, 1)))
        out = np.maximum(out, 1.0 - d / r)
    return out


def area_weights(G):
    wgt = np.broadcast_to(np.cos(np.radians(G.lat1))[:, None], (G.h, G.w)).ravel().astype(np.float64)
    return wgt / wgt.sum()


def solve_bias(field, target, wgt, extra=None, exclude=None):
    """Coast bias so the region's land covers target percent of the planet (bisection; fields are monotone in it)."""
    lo, hi = -0.8, 0.8
    for _ in range(26):
        b = 0.5 * (lo + hi)
        f = field + b
        if extra is not None:
            f = np.maximum(f, extra)
        m = f > 0
        if exclude is not None:
            m &= ~exclude
        if wgt[m].sum() * 100.0 < target:
            lo = b
        else:
            hi = b
    return 0.5 * (lo + hi)


def trace_rivers(G, land_f, owner, beltkey, noise):
    """Rivers by steepest descent on a height proxy from mountain sources to the sea (or to a lake in a basin)."""
    h, w = G.h, G.w
    hgt = (land_f + 0.65 * np.max(np.stack(list(beltkey.values())), axis=0) + 0.06 * noise).reshape(h, w)
    landm = (land_f > 0).reshape(h, w)
    own = owner.reshape(h, w)
    taken = np.zeros((h, w), bool)
    keys = [r[0] for r in REGIONS[:5]]
    rivers, lakes = [], []
    nb = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]
    lat1 = G.lat1
    for ki, key in enumerate(keys):
        count = int(RIVER_COUNTS.get(key, 0))
        if count <= 0:
            continue
        bk = beltkey.get(key, np.zeros(G.n, F32)).reshape(h, w)
        score = np.where(landm & (own == ki), bk + 0.25 * np.clip(hgt, 0, 2), -9)
        cand = np.argsort(score.ravel())[::-1][:40000]
        chosen = []
        made = 0
        for idx in cand:
            if made >= count:
                break
            y, x = divmod(int(idx), w)
            if score[y, x] < 0.15:
                break
            lon, lat = G.lon1[x], lat1[y]
            if abs(lat) > 80:
                continue
            p = vec(lon, lat)
            if any(float(np.dot(p, q)) > math.cos(math.radians(6.5)) for q in chosen):
                continue
            chosen.append(p)
            path = [(y, x)]
            ended = None
            for _ in range(900):
                cy, cx = path[-1]
                if not landm[cy, cx]:
                    ended = "sea"
                    break
                if taken[cy, cx] and len(path) > 1:
                    ended = "join"
                    break
                best, bv = None, hgt[cy, cx]
                for dy, dx in nb:
                    ny, nx = cy + dy, (cx + dx) % w
                    if 0 <= ny < h and hgt[ny, nx] < bv:
                        bv, best = hgt[ny, nx], (ny, nx)
                if best is None:
                    ended = "lake"
                    break
                path.append(best)
            if ended is None or len(path) < 14 or (ended == "lake" and key not in LAKE_REGIONS):
                continue
            for (py, px) in path[:-1]:
                taken[py, px] = True
            pts = np.array([vec(G.lon1[px], lat1[py]) for (py, px) in path], F32)
            k = 4
            if len(pts) > 2 * k + 2:
                sm = pts.copy()
                for i in range(k, len(pts) - k):
                    sm[i] = pts[i - k:i + k + 1].mean(axis=0)
                pts = sm / np.linalg.norm(sm, axis=1, keepdims=True)
            rivers.append((key, pts))
            made += 1
            if ended == "lake":
                lakes.append((key, pts[-1]))
    log(f"rivers: {len(rivers)} traced, {len(lakes)} end in lakes")
    return rivers, lakes


def river_points(rivers):
    """All river samples densified to ~0.12 deg, with progress t (0 source .. 1 mouth) and a width scale."""
    Q, T, S = [], [], []
    for key, pts in rivers:
        seg = []
        for a, b in zip(pts[:-1], pts[1:]):
            ang = math.degrees(math.acos(float(np.clip(np.dot(a, b), -1, 1))))
            k = max(1, int(ang / 0.12))
            for t in np.linspace(0, 1, k, endpoint=False):
                v = a * (1 - t) + b * t
                seg.append(v / np.linalg.norm(v))
        seg.append(pts[-1])
        seg = np.array(seg, F32)
        n = len(seg)
        Q.append(seg)
        # Long rivers grow to full width; short ones stay narrow.
        T.append((np.linspace(0, 1, n) * min(1.0, n / 140.0)).astype(F32))
        S.append(np.full(n, {"C4": 1.35, "C3": 1.1}.get(key, 1.0), F32))
    if not Q:
        return np.zeros((0, 3), F32), np.zeros(0, F32), np.zeros(0, F32)
    return np.concatenate(Q), np.concatenate(T), np.concatenate(S)


def nearest_points(P, Q):
    """Angular distance (deg) to the nearest point of Q and that point's index."""
    def fn(x, y, z):
        best = np.full(len(x), -2.0, F32)
        arg = np.zeros(len(x), np.int64)
        for i in range(0, len(Q), 96):
            q = Q[i:i + 96]
            d = x[:, None] * q[None, :, 0] + y[:, None] * q[None, :, 1] + z[:, None] * q[None, :, 2]
            j = d.argmax(axis=1)
            v = d[np.arange(len(x)), j]
            better = v > best
            best = np.where(better, v, best)
            arg = np.where(better, j + i, arg)
        return np.degrees(np.arccos(np.clip(best, -1, 1))), arg.astype(F32)
    out = chunked(fn, len(P[0]), *P, channels=2)
    return out[0], out[1].astype(np.int64)


def build_layout(w, h):
    log(f"layout grid {w}x{h}")
    G = Grid(w, h)
    P = G.p()
    wgt = area_weights(G)
    PW = warp(P, 0.16, 1.3, 4, 11)
    PW = warp(PW, 0.05, 4.0, 3, 21)
    coast_mid = fbm(P, 2.2, 6, 31, gain=0.55)
    # Tectonic grain: ridged noise turns part of the coast into long capes and channels instead of round lobes.
    grain = ridged(warp(P, 0.08, 2.0, 3, 33), 2.8, 5, 37) - 0.38
    core_ang = angle_to(P, CORE_LON, CORE_LAT)
    core_cap = core_ang < CORE_ID_RADIUS
    guard = np.full(G.n, -4.0, F32)
    for lon, lat in NODES:
        guard = np.maximum(guard, 0.34 * (1.0 - angle_to(P, lon, lat) / 4.6))
    fields, biases = {}, {}
    for key, *_ in REGIONS[:5]:
        f = lobe_field(PW, LOBES[key]) + ROUGH[key] * (0.30 * coast_mid + GRAIN * grain)
        for (poly, rad, cnt, k2) in ARCHIPELAGOS:
            if k2 == key:
                a = archipelago_field(PW, poly, rad, cnt, zlib.crc32(str(poly).encode()) % 1000)
                f = np.maximum(f, a + 0.25 * coast_mid)
        extra = guard if key == "C1" else None
        if key == "C5":
            extra = 0.22 * (1.0 - core_ang / (CORE_RADIUS + 3))
        b = solve_bias(f, TARGETS[key], wgt, extra, core_cap if key == "C5" else None)
        f = f + b
        if extra is not None:
            f = np.maximum(f, extra)
        biases[key] = round(float(b), 4)
        fields[key] = f.astype(F32)
    log("coast biases " + json.dumps(biases))
    fields["C6"] = (1.0 - core_ang / CORE_RADIUS).astype(F32)

    land_f = np.max(np.stack([fields[k] for k in ("C1", "C2", "C3", "C4", "C5")]), axis=0)
    keys = [r[0] for r in REGIONS]
    stack = np.stack([fields[k] for k in keys])
    stack[5] = np.where(core_ang < CORE_ID_RADIUS, 9.0, -9.0)
    terr = np.argmax(stack, axis=0)
    terr_valid = np.max(stack, axis=0) > -0.55
    soft = np.exp(np.clip((stack[:5] - stack[:5].max(axis=0, keepdims=True)) * 14.0, -30, 0))
    soft /= soft.sum(axis=0, keepdims=True)
    belts = np.zeros(G.n, F32)
    beltkey = {}
    for key, ranges in MOUNTAINS.items():
        bk = np.zeros(G.n, F32)
        for poly, width, amp in ranges:
            d = poly_distance(PW, [poly])
            bk = np.maximum(bk, amp * np.exp(-(d / width) ** 2))
        beltkey[key] = bk
        belts = np.maximum(belts, bk)
    rift_d = poly_distance(PW, RIFTS)
    trench_d = poly_distance(PW, TRENCHES)
    node_d = np.full(G.n, 180.0, F32)
    for lon, lat in NODES:
        node_d = np.minimum(node_d, angle_to(P, lon, lat))
    owner = np.argmax(stack[:5], axis=0)
    rivers, lakes = trace_rivers(G, land_f, owner, beltkey, 2.6 * fbm(P, 5.0, 3, 39) + 1.2 * fbm(P, 13.0, 3, 41))
    land_lo = (land_f > 0).astype(F32).reshape(h, w)
    b1 = blur_sphere(land_lo, 2.0, G.lat1)
    b2 = blur_sphere(land_lo, 4.5, G.lat1)
    b3 = blur_sphere(land_lo, 13.0, G.lat1)
    L = {k: v.reshape(h, w) for k, v in fields.items()}
    out = dict(fields=L, land=land_f.reshape(h, w), soft=soft.reshape(5, h, w), terr=terr.reshape(h, w),
               terr_valid=terr_valid.reshape(h, w), belts=belts.reshape(h, w),
               beltkey={k: v.reshape(h, w) for k, v in beltkey.items()},
               rift=rift_d.reshape(h, w), trench=trench_d.reshape(h, w), node=node_d.reshape(h, w),
               core=core_ang.reshape(h, w), b1=b1, b2=b2, b3=b3, rivers=rivers, lakes=lakes, biases=biases)
    log("layout done")
    return out


def preview_layout(lay, path):
    """Flat map of land by region with the level nodes and rivers: a fast check of the layout alone."""
    h, w = lay["land"].shape
    palette = np.array([[0.45, 0.85, 0.95], [0.95, 0.45, 0.15], [0.9, 0.95, 1.0], [0.2, 0.7, 0.4], [0.9, 0.2, 0.7],
                        [0.6, 0.3, 1.0]], F32)
    owner = np.argmax(lay["soft"], axis=0)
    owner = np.where(lay["core"] < CORE_ID_RADIUS, 5, owner)
    img = np.where((lay["land"] > 0)[..., None], palette[owner] * (0.6 + 0.4 * np.clip(lay["land"], 0, 1))[..., None],
                   np.array([0.03, 0.06, 0.12], F32) + 0.12 * lay["b2"][..., None])
    img = img + 0.35 * lay["belts"][..., None]
    for key, pts in lay["rivers"]:
        for p in pts:
            lon = math.degrees(math.atan2(p[1], p[0]))
            lat = math.degrees(math.asin(float(np.clip(p[2], -1, 1))))
            x, y = int((lon + 180) / 360 * w) % w, min(h - 1, int((lat + 90) / 180 * h))
            img[y, x] = (0.1, 0.2, 1.0)
    for lon, lat in NODES:
        x, y = int((lon + 180) / 360 * w) % w, int((lat + 90) / 180 * h)
        img[y - 2:y + 3, x - 2:x + 3] = (1.0, 1.0, 0.2)
    for lon in range(-180, 180, 30):
        img[:, int((lon + 180) / 360 * w)] *= 0.6
    for lat in range(-60, 90, 30):
        img[int((lat + 90) / 180 * h)] *= 0.6
    write_png(path, np.clip(img, 0, 1))


# ----------------------------------------------------------------------------------------------------------------
# Full-resolution painting
# ----------------------------------------------------------------------------------------------------------------

def paint(W, H, lay):
    G = Grid(W, H)
    P = G.p()
    N = G.n
    up = lambda a: upsample(a, H, W).ravel()
    lat = G.latf
    alat = np.abs(lat)
    log("upsampling layout")
    soft = upsample(lay["soft"], H, W).reshape(5, N)
    wCP, wAR, wNC, wMW, wSC = soft
    belts, rift_d, trench_d, node_d, core_d = (up(lay[k]) for k in ("belts", "rift", "trench", "node", "core"))
    b1, b2, b3 = up(lay["b1"]), up(lay["b2"]), up(lay["b3"])
    land_coarse = up(lay["land"])

    log("coast detail")
    fine = fbm(P, 14.0, 7, 41, gain=0.56)
    F = land_coarse + 0.11 * fine
    land = smooth(-0.004, 0.004, F)
    landb = F > 0
    inland = smooth(0.0, 0.35, F)
    inland_m = smooth(0.04, 0.3, F)

    log("relief noise")
    PW = warp(P, 0.03, 6.0, 3, 51)
    rm = ridged(PW, 4.2, 8, 61, gain=0.52)
    hills = fbm(P, 6.0, 6, 71)
    upl = smooth(0.05, 0.6, fbm(P, 1.7, 4, 81))
    n_mid = fbm(P, 3.5, 5, 91)
    n_fine = fbm(P, 40.0, 4, 101)
    n_low2 = fbm(P, 2.4, 4, 111)

    mamp = wCP * 0.85 + wAR * 0.9 + wNC * 1.25 + wMW * 0.35 + wSC * 1.15
    hamp = wCP * 1.0 + wAR * 0.8 + wNC * 1.0 + wMW * 0.5 + wSC * 0.9
    site_flat = smooth(2.0, 5.0, node_d)
    elev = 0.03 + inland * (0.09 + 0.07 * hills * hamp) \
        + inland_m * (belts * 0.95 + upl * 0.35 * mamp) * np.power(rm, 1.4) * mix(0.25, 1.0, site_flat)
    # Ashen Reach: mesas, softly terraced.
    terr8 = np.floor(elev * 9.0) / 9.0 + smooth(0.75, 1.0, (elev * 9.0) % 1.0) / 9.0
    elev = mix(elev, terr8, wAR * 0.6 * smooth(0.12, 0.25, elev))
    # The Core crater: raised ragged rim, sunken floor, a central spire; radial fault lines.
    cn = core_d + 1.6 * fbm(P, 7.0, 4, 121) + 0.5 * fbm(P, 30.0, 3, 125)
    rim = np.exp(-((cn - CORE_RADIUS) / 1.4) ** 2) * 0.55 + np.exp(-((cn - CORE_RADIUS - 3.5) / 2.0) ** 2) * 0.15
    floor_ = smooth(CORE_RADIUS + 0.5, CORE_RADIUS - 1.5, cn)
    spire = np.exp(-(core_d / 1.2) ** 2) * 0.35
    core_w = smooth(CORE_RADIUS + 6.0, CORE_RADIUS + 2.0, core_d)
    elev = mix(elev, 0.05 + rim + spire + 0.04 * hills, core_w * 0.85)
    elev = mix(elev, 0.02 + spire + 0.03 * n_mid, floor_ * 0.9)
    elev = np.clip(elev, 0, 1.2) * landb

    # Rivers are traced downhill in the layout (mountain source -> coast or basin lake); Mirewood adds a braided
    # swamp channel network on top.
    log("water")
    PR = warp(P, 0.06, 3.0, 3, 131)
    rv2 = 1.0 - np.abs(fbm(PR, 9.5, 3, 151))
    lowland = smooth(0.35, 0.12, elev) * smooth(0.02, 0.10, F)
    braid = smooth(0.982, 0.990, rv2) * wMW * lowland * 0.75 * (1 - smooth(55, 70, alat))
    Q, T, S = river_points(lay["rivers"])
    river = np.zeros(N, F32)
    valley = np.zeros(N, F32)
    if len(Q):
        Gc = Grid(1024, 512)
        dc, _ = nearest_points(Gc.p(), Q)
        approx = up(dc.reshape(512, 1024))
        sel = np.nonzero(approx < 1.6)[0]
        Ps = (P[0][sel], P[1][sel], P[2][sel])
        d_s, i_s = nearest_points(Ps, Q)
        d_s = d_s + 0.05 * fbm(Ps, 70.0, 3, 157)
        width = (0.06 + 0.20 * np.power(T[i_s], 1.3)) * S[i_s]
        river[sel] = smooth(width, width * 0.4, d_s)
        valley[sel] = smooth(width * 9.0 + 0.4, width, d_s)
    lake_end = np.zeros(N, F32)
    for key, q in lay["lakes"]:
        r = 1.2 if key == "C4" else 0.8
        sel = np.nonzero((P[0] * q[0] + P[1] * q[1] + P[2] * q[2]) > math.cos(math.radians(r + 1.0)))[0]
        if len(sel):
            d = np.degrees(np.arccos(np.clip(P[0][sel] * q[0] + P[1][sel] * q[1] + P[2][sel] * q[2], -1, 1)))
            d = d + 0.45 * fbm((P[0][sel], P[1][sel], P[2][sel]), 20.0, 3, 159)
            lake_end[sel] = np.maximum(lake_end[sel], smooth(r, r - 0.25, d))
    river *= smooth(0.0, 0.02, F)
    lakes = smooth(0.30, 0.36, fbm(P, 11.0, 4, 161)) * wMW * smooth(0.04, 0.15, F)
    water = np.clip(np.maximum(np.maximum(river, braid), np.maximum(lakes, lake_end)), 0, 1)
    # Valleys: rivers cut a little into the relief so the normal map shows them.
    elev = elev * (1 - 0.45 * valley * smooth(0.0, 0.05, F)) * (1 - 0.25 * lake_end)

    alb = np.zeros((3, N), F32)
    em = np.zeros((3, N), F32)
    spec = np.zeros(N, F32)

    def put(target, rgb, wgt):
        for c in range(3):
            target[c] += rgb[c] * wgt

    # slope for exposing rock (land only)
    eh = elev.reshape(H, W)
    gx = (np.roll(eh, -1, 1) - np.roll(eh, 1, 1)) / np.maximum(np.cos(np.radians(lat.reshape(H, W))), 0.05)
    gy = np.zeros_like(eh)
    gy[1:-1] = eh[2:] - eh[:-2]
    slope = np.clip(np.sqrt(gx * gx + gy * gy).ravel() * (W / 512.0) * 1.6, 0, 1)

    log("biome: Crystal Plains")
    m = wCP > 0.002
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        nm, nf, e, s_ = n_mid[m], n_fine[m], elev[m], slope[m]
        sb = subbiome_weights(Pm, e)
        w_meadow, w_fern, w_upland, w_shard, w_salt = sb

        def tint(a, b, t):
            return mix(col(*a)[:, None], col(*b)[:, None], t[None])
        tm = smooth(-0.4, 0.4, nm)
        base = (tint((0.33, 0.56, 0.52), (0.50, 0.68, 0.60), tm) * w_meadow
                + tint((0.08, 0.22, 0.30), (0.15, 0.33, 0.40), smooth(-0.5, 0.5, fbm(Pm, 70.0, 3, 241))) * w_fern
                + tint((0.40, 0.32, 0.54), (0.58, 0.48, 0.70), smooth(-0.3, 0.5, nm + 0.5 * nf)) * w_upland
                + tint((0.22, 0.22, 0.38), (0.32, 0.30, 0.48), tm) * w_shard
                + tint((0.56, 0.54, 0.62), (0.70, 0.66, 0.70), smooth(-0.3, 0.4, nf)) * w_salt)
        sF1, sF2 = worley(Pm, 260.0, 7)
        base *= (1 - smooth(0.06, 0.0, sF2 - sF1) * w_salt * smooth(-0.1, 0.4, nm) * 0.18)[None]
        base *= (0.88 + 0.16 * nf)[None]
        rock = mix(col(0.36, 0.32, 0.46)[:, None], col(0.50, 0.46, 0.60)[:, None], smooth(-0.3, 0.4, nf)[None])
        base = mix(base, rock, smooth(0.18, 0.34, e + 0.25 * s_)[None])
        base = mix(base, col(0.70, 0.68, 0.84)[:, None], smooth(0.55, 0.75, e + 0.05 * nf)[None] * 0.8)
        # Crystal fields: clustered outcrops (blobs of cells), denser in the shard fields, not vein networks.
        density = 0.28 * w_meadow + 0.08 * w_fern + 0.45 * w_upland + 1.0 * w_shard + 0.18 * w_salt
        cluster = smooth(0.08, 0.38, fbm(Pm, 6.0, 4, 171) + 0.45 * (density - 0.45)) * np.clip(density * 1.3, 0, 1)
        cluster *= smooth(0.5, 0.2, e)
        F1s, _ = worley(Pm, 105.0, 11)
        F1b, _ = worley(Pm, 36.0, 13)
        spots = smooth(0.45, 0.15, F1s)
        mounds = smooth(0.6, 0.2, F1b)
        crystals = cluster * np.maximum(spots * (0.45 + 0.55 * mounds), 0.7 * smooth(0.3, 0.1, F1b))
        base *= (1 - 0.30 * cluster * mounds)[None]
        # The field itself reads from orbit as a pale frosted sheen around its outcrops.
        base = mix(base, col(0.62, 0.78, 0.88)[:, None], (cluster * 0.22)[None])
        base = mix(base, col(0.72, 0.94, 1.0)[:, None], (np.clip(crystals, 0, 1) * 0.75)[None])
        veins = smooth(0.86, 0.96, rm[m]) * smooth(0.18, 0.35, e) * w_upland * 0.3
        glow_col = mix(col(0.30, 0.88, 1.0)[:, None], col(0.62, 0.52, 1.0)[:, None], w_upland[None])
        put_em = glow_col * (crystals * 1.0 + cluster * 0.10 + veins)[None]
        wgt = wCP[m]
        alb[:, m] += base * wgt
        em[:, m] += put_em * wgt
        spec[m] += (0.12 + 0.2 * w_salt + crystals * 0.5) * wgt

    log("biome: Ashen Reach")
    m = wAR > 0.002
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        nm, nf, e, s = n_mid[m], n_fine[m], elev[m], slope[m]
        base = mix(col(0.12, 0.10, 0.10)[:, None], col(0.30, 0.27, 0.25)[:, None], smooth(-0.4, 0.4, nm)[None])
        sand_m = smooth(-0.05, 0.3, n_low2[m]) * smooth(0.3, 0.12, e)
        lonr = np.arctan2(Pm[1], Pm[0])
        dn = fbm(Pm, 7.0, 3, 191)
        dunes = ridged(warp(Pm, 0.012, 18.0, 2, 192), 55.0, 3, 194, stretch=(1.0, 1.0, 0.45))
        dunes = smooth(0.35, 0.9, dunes) * smooth(-0.5, 0.3, dn)
        sand = mix(col(0.48, 0.25, 0.13)[:, None], col(0.68, 0.42, 0.23)[:, None], smooth(-0.3, 0.5, nf + 0.4 * dn)[None])
        sand = sand * (0.90 + 0.14 * dunes)[None]
        base = mix(base, sand, sand_m[None])
        rd = rift_d[m] + 2.2 * fbm(Pm, 4.0, 5, 195) + 0.6 * dn
        rift_on = smooth(-0.35, 0.1, fbm(Pm, 3.0, 3, 197))
        flows = smooth(7.0, 2.5, rd) * smooth(-0.2, 0.3, nm)
        base = mix(base, col(0.06, 0.05, 0.05)[:, None], (flows * 0.85)[None])
        cracks = smooth(0.88, 0.97, ridged(Pm, 22.0, 3, 201)) * smooth(7.0, 2.0, rd) * rift_on
        core_rift = smooth(0.75, 0.1, rd + 0.35 * nf) * rift_on
        lava = np.clip(core_rift + cracks * 0.8, 0, 1)
        vents = smooth(0.48, 0.62, e) * smooth(0.75, 0.9, rm[m])
        base = mix(base, col(0.86, 0.36, 0.08)[:, None], (lava * 0.85)[None])
        base = mix(base, col(0.48, 0.45, 0.43)[:, None], smooth(0.42, 0.6, e)[None] * 0.6)
        glow = col(1.0, 0.42, 0.08)[:, None] * (lava + vents * 0.8)[None] + col(1.0, 0.25, 0.05)[:, None] * (flows * 0.06)[None]
        wgt = wAR[m]
        alb[:, m] += base * wgt
        em[:, m] += glow * wgt
        spec[m] += 0.06 * wgt

    log("biome: Northern Crown")
    m = wNC > 0.002
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        nm, nf, e, s, la = n_mid[m], n_fine[m], elev[m], slope[m], lat[m]
        slate = mix(col(0.22, 0.24, 0.28)[:, None], col(0.36, 0.38, 0.42)[:, None], smooth(-0.4, 0.4, nf)[None])
        tundra = mix(col(0.36, 0.40, 0.42)[:, None], col(0.48, 0.52, 0.56)[:, None], smooth(-0.3, 0.3, nm)[None])
        base = mix(tundra, slate, smooth(0.2, 0.5, e + 0.3 * s)[None])
        gl = 1.0 - np.abs(mix(fbm(Pm, 6.0, 4, 211, stretch=(1, 1, 0.6)), fbm(Pm, 6.0, 4, 213), smooth(68.0, 82.0, la)))
        glacier = smooth(0.9, 0.98, gl) * smooth(0.1, 0.3, e)
        snow_cov = smooth(42.0, 56.0, la + 9.0 * nm - 12.0 * smooth(0.35, 0.7, s)) * (1 - glacier * 0.6)
        snow_cov = np.maximum(snow_cov, smooth(0.45, 0.6, e - 0.3 * s))
        snow = mix(col(0.86, 0.90, 0.96)[:, None], col(0.95, 0.97, 1.0)[:, None], smooth(-0.3, 0.5, nf)[None])
        base = mix(base, snow, np.clip(snow_cov, 0, 1)[None])
        base = mix(base, col(0.58, 0.74, 0.88)[:, None], (glacier * 0.8)[None])
        crev = smooth(0.9, 0.98, ridged(Pm, 30.0, 2, 221)) * snow_cov * 0.18
        base *= (1 - crev)[None]
        veins = smooth(0.9, 0.98, ridged(Pm, 16.0, 3, 231)) * smooth(0.6, 0.2, snow_cov) * 0.35
        wgt = wNC[m]
        alb[:, m] += base * wgt
        em[:, m] += col(0.35, 0.85, 1.0)[:, None] * veins[None] * wgt
        spec[m] += (0.12 + 0.4 * snow_cov) * wgt

    log("biome: Mirewood")
    m = wMW > 0.002
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        nm, nf, e, wa = n_mid[m], n_fine[m], elev[m], water[m]
        base = mix(col(0.06, 0.17, 0.14)[:, None], col(0.11, 0.29, 0.26)[:, None], smooth(-0.4, 0.4, nm)[None])
        bog = smooth(0.1, 0.45, n_low2[m])
        base = mix(base, col(0.21, 0.25, 0.12)[:, None], (bog * 0.55)[None])
        canopy = fbm(Pm, 70.0, 3, 241)
        base *= (0.8 + 0.3 * smooth(-0.5, 0.5, canopy) + 0.1 * nf)[None]
        base = mix(base, col(0.30, 0.31, 0.27)[:, None], smooth(0.25, 0.45, e)[None])
        near = smooth(0.0, 0.5, wa)
        specks = smooth(0.62, 0.8, fbm(Pm, 90.0, 2, 251)) * smooth(0.0, 0.1, nm + 0.2) * 0.5
        wgt = wMW[m]
        alb[:, m] += base * wgt
        em[:, m] += col(0.25, 1.0, 0.55)[:, None] * (specks * (0.3 + near))[None] * wgt
        spec[m] += 0.3 * wgt

    log("biome: Southern Crown")
    m = wSC > 0.002
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        nm, nf, e, s, la = n_mid[m], n_fine[m], elev[m], slope[m], lat[m]
        base = mix(col(0.15, 0.17, 0.24)[:, None], col(0.28, 0.31, 0.41)[:, None], smooth(-0.4, 0.5, nm + 0.3 * nf)[None])
        frost = smooth(0.35, 0.6, e + 0.2 * nf) * 0.8 + smooth(-70.0, -80.0, la + 5.0 * nm + 2.0 * nf) * 0.85
        base = mix(base, col(0.52, 0.56, 0.68)[:, None], np.clip(frost, 0, 1)[None])
        # The polar ice sheet: same white as the sea ice next to it, so the pole reads as one frozen cap.
        sheet = smooth(-72.0, -82.0, la + 5.0 * nm + 2.0 * nf)
        base = mix(base, col(0.80, 0.85, 0.93)[:, None] * (0.92 + 0.1 * nf)[None], (sheet * 0.85)[None])
        polar = smooth(-66.0, -80.0, la)
        sheen = smooth(0.7, 0.95, mix(ridged(Pm, 12.0, 3, 261, stretch=(1, 1, 0.5)), ridged(Pm, 12.0, 3, 263), polar))
        base = mix(base, col(0.38, 0.42, 0.54)[:, None], (sheen * 0.35)[None])
        vr = ridged(warp(Pm, 0.04, 5.0, 2, 271), 7.0, 4, 281)
        veins = smooth(0.82, 0.94, vr) * (0.4 + 0.6 * smooth(-0.2, 0.4, nm))
        base = mix(base, col(0.62, 0.14, 0.46)[:, None], (veins * 0.75)[None])
        wgt = wSC[m]
        alb[:, m] += base * wgt
        em[:, m] += col(1.0, 0.2, 0.72)[:, None] * veins[None] * 0.8 * wgt
        spec[m] += 0.62 * wgt

    log("the Core crater")
    m = core_w > 0.001
    if m.any():
        Pm = (P[0][m], P[1][m], P[2][m])
        cd, cw, fl, nf = core_d[m], core_w[m], floor_[m], n_fine[m]
        lonr = np.arctan2(Pm[1], Pm[0])
        c0, e0, n0 = frame(CORE_LON, CORE_LAT)
        az = np.arctan2(Pm[0] * n0[0] + Pm[1] * n0[1] + Pm[2] * n0[2], Pm[0] * e0[0] + Pm[1] * e0[1] + Pm[2] * e0[2])
        radial = np.abs(np.sin(az * 9.0 + 1.5 * fbm(Pm, 6.0, 3, 291)))
        fract = smooth(0.07, 0.0, radial) * smooth(CORE_RADIUS + 3.0, 2.0, cd) * smooth(-0.1, 0.35, fbm(Pm, 5.0, 3, 301))
        rings = smooth(0.5, 1.0, fl) * smooth(0.2, 0.6, fbm(Pm, 9.0, 3, 305)) * 0.6
        crack = smooth(0.88, 0.97, ridged(Pm, 26.0, 3, 311)) * fl
        glow_v = np.clip(fract * 0.8 + rings * 0.5 + crack * 0.7 + np.exp(-(cd / 2.2) ** 2) * 1.2, 0, 1.5)
        rimrock = mix(col(0.20, 0.16, 0.24)[:, None], col(0.34, 0.29, 0.38)[:, None], smooth(-0.4, 0.4, nf)[None])
        floorc = mix(col(0.15, 0.10, 0.20)[:, None], col(0.27, 0.19, 0.33)[:, None], smooth(-0.3, 0.5, fbm(Pm, 14.0, 4, 315))[None]) * (0.85 + 0.3 * nf)[None]
        base = mix(rimrock, floorc, fl[None])
        base = mix(base, col(0.62, 0.34, 0.95)[:, None], np.clip(glow_v * 0.6, 0, 1)[None])
        cur = alb[:, m]
        alb[:, m] = mix(cur, base, cw[None])
        em[:, m] = mix(em[:, m], col(0.62, 0.28, 1.0)[:, None] * glow_v[None], cw[None])

    # Rivers and lakes on land.
    wmask = water * landb
    for c in range(3):
        tgt = (wCP * 0.07 + wMW * 0.035 + wAR * 0.07 + wNC * 0.5 + wSC * 0.12) if c == 0 else \
              (wCP * 0.24 + wMW * 0.12 + wAR * 0.07 + wNC * 0.62 + wSC * 0.14) if c == 1 else \
              (wCP * 0.30 + wMW * 0.12 + wAR * 0.07 + wNC * 0.75 + wSC * 0.22)
        alb[c] = mix(alb[c], tgt, wmask * 0.9)
    spec = mix(spec, 0.85, wmask)
    em *= (1 - wmask * 0.7)[None]
    # Bioluminescent plankton: Crystal Plains rivers glow faintly cyan, Mirewood channels green.
    em += (col(0.25, 0.75, 1.0)[:, None] * (wCP * 0.35)[None] + col(0.25, 1.0, 0.55)[:, None] * (wMW * 0.25)[None]) * (river * landb)[None]

    # A light baked relief cue: the map view is lit from the camera, so the normal map alone barely shows the
    # ranges. Highlands brighten, valleys and ridge flanks darken a little.
    relief = 0.84 + 0.34 * smooth(0.05, 0.7, elev) - 0.12 * smooth(0.2, 0.9, slope)
    alb *= np.clip(relief, 0.6, 1.2)[None]

    log("ocean")
    shelf = smooth(0.03, 0.4, b1 + 0.06 * fine)
    mid = smooth(0.0, 0.35, b2)
    far = smooth(0.0, 0.25, b3)
    depth = 1.0 - np.clip(0.55 * shelf + 0.3 * mid + 0.15 * far, 0, 1)
    tn = trench_d + 1.2 * fbm(P, 9.0, 3, 321)
    trench = np.exp(-(tn / 1.6) ** 2)
    abyss = fbm(P, 5.0, 5, 331)
    depth = np.clip(depth + 0.25 * trench + 0.06 * abyss * smooth(0.3, 0.7, depth), 0, 1)
    sea = np.stack([
        np.interp(depth, [0.0, 0.12, 0.3, 0.6, 1.0], [0.11, 0.06, 0.035, 0.02, 0.015]),
        np.interp(depth, [0.0, 0.12, 0.3, 0.6, 1.0], [0.40, 0.27, 0.14, 0.07, 0.035]),
        np.interp(depth, [0.0, 0.12, 0.3, 0.6, 1.0], [0.46, 0.38, 0.28, 0.18, 0.10]),
    ]).astype(F32)
    sea = mix(sea, col(0.035, 0.02, 0.10)[:, None], (trench * smooth(0.5, 0.9, depth) * 0.7)[None])
    tints = [(col(0.04, 0.13, 0.26), wCP), (col(0.06, 0.08, 0.09), wAR), (col(0.07, 0.14, 0.20), wNC),
             (col(0.02, 0.16, 0.13), wMW), (col(0.05, 0.06, 0.16), wSC)]
    for tc, tw in tints:
        sea = mix(sea, tc[:, None] * (0.6 + 0.8 * (1 - depth))[None], (tw * 0.28 * smooth(0.1, 0.5, depth))[None])
    curr = mix(fbm(P, 3.0, 4, 341, stretch=(1, 1, 3.0)), fbm(P, 3.0, 4, 343), smooth(50.0, 72.0, alat))
    sea *= (0.92 + 0.12 * curr)[None]
    seaice = smooth(73.0, 79.0, alat + 6.0 * n_mid + 2.0 * n_fine + 7.0 * fbm(P, 1.6, 3, 353))
    floes = smooth(0.0, 0.25, fbm(P, 25.0, 3, 351) + (seaice - 0.5) * 1.6)
    ice_w = np.clip(seaice * 0.35 + floes * smooth(70.0, 76.0, alat), 0, 1)
    sea = mix(sea, col(0.74, 0.83, 0.92)[:, None] * (0.9 + 0.12 * n_fine)[None], ice_w[None])
    ocean_spec = mix(1.0, 0.45, ice_w)
    # Pale surf right at the shore.
    surf = smooth(0.42, 0.62, b1 + 0.08 * fine) * (1 - landb) * (1 - ice_w) * 0.14 * smooth(-0.2, 0.4, n_mid)
    sea = mix(sea, col(0.30, 0.62, 0.62)[:, None], surf[None])

    alb = mix(sea, alb, land[None])
    spec = mix(ocean_spec, spec, land)
    em *= land[None]

    # height: ocean -depth, land elev
    height = np.where(landb, elev / 1.2, -depth) * 0.5 + 0.5

    terr_n = lay["terr"]
    terr_full = terr_n[np.clip(((np.arange(H) + 0.5) * terr_n.shape[0] / H).astype(int), 0, terr_n.shape[0] - 1)][:, np.clip(((np.arange(W) + 0.5) * terr_n.shape[1] / W).astype(int), 0, terr_n.shape[1] - 1)].ravel()
    valid_full = lay["terr_valid"][np.clip(((np.arange(H) + 0.5) * terr_n.shape[0] / H).astype(int), 0, terr_n.shape[0] - 1)][:, np.clip(((np.arange(W) + 0.5) * terr_n.shape[1] / W).astype(int), 0, terr_n.shape[1] - 1)].ravel()
    ids = np.array([r[5] for r in REGIONS], F32) / 255.0
    terr_id = ids[terr_full]
    # Land pixels: nearest continent (by field), the Core inside its crater rim.
    land_ids = np.where(core_d < CORE_ID_RADIUS, ids[5], ids[np.argmax(soft, axis=0)])
    regions_land = np.where(landb, land_ids, 0.0).astype(F32)
    regions_terr = np.where(valid_full | landb, np.where(landb, land_ids, terr_id), 0.0).astype(F32)
    member = np.clip(soft.max(axis=0), 0, 1) * smooth(-0.5, 0.15, land_coarse)

    glowmax = np.clip(em.max(axis=0), 0, 1)
    log("assembling maps")
    maps = dict(
        albedo=np.clip(alb.T.reshape(H, W, 3), 0, 1),
        emission=np.clip(em.T.reshape(H, W, 3), 0, 1),
        height=height.reshape(H, W),
        elev_land=(elev * landb).reshape(H, W),
        mask=np.stack([land, np.clip(spec, 0, 1), glowmax, regions_land], axis=1).reshape(H, W, 4),
        regions=np.stack([regions_land, regions_terr, member], axis=1).reshape(H, W, 3),
    )
    return maps, G


def normal_map(elev, strength):
    H, W = elev.shape
    lat = (np.arange(H) + 0.5) / H * 180 - 90
    cosl = np.maximum(np.cos(np.radians(lat)), 0.05)[:, None]
    px = 2 * math.pi / W  # radians per pixel
    dx = (np.roll(elev, -1, 1) - np.roll(elev, 1, 1)) / (2 * px * cosl)
    dy = np.zeros_like(elev)
    dy[1:-1] = (elev[2:] - elev[:-2]) / (2 * px)
    dx *= strength
    dy *= strength
    n = np.stack([-dx, -dy, np.ones_like(elev)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n.astype(F32)


def clouds(W, H):
    log("clouds")
    G = Grid(W, H)
    P = G.p()
    lat = G.latf
    rng = np.random.default_rng(77)
    # Cyclones: twist the sampling point around each storm's axis (counter-clockwise north, clockwise south).
    x, y, z = [a.copy() for a in P]
    storms = [(-70, 42, 9), (20, 52, 11), (120, 38, 8), (-150, 50, 10), (170, -45, 10), (-20, -50, 12),
              (80, -42, 9), (-110, -38, 8), (-40, 14, 5), (100, 12, 4), (40, 60, 8), (-120, 62, 9)]
    for lon, lat0, rad in storms:
        c = vec(lon, lat0)
        d = np.degrees(np.arccos(np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1)))
        ang = (3.2 if lat0 > 0 else -3.2) * np.exp(-(d / rad) ** 2) * (1 - np.exp(-(d / (rad * 0.18)) ** 2) * 0.5)
        ca, sa = np.cos(ang), np.sin(ang)
        dot = x * c[0] + y * c[1] + z * c[2]
        cx, cy, cz = c[1] * z - c[2] * y, c[2] * x - c[0] * z, c[0] * y - c[1] * x
        x, y, z = (x * ca + cx * sa + c[0] * dot * (1 - ca), y * ca + cy * sa + c[1] * dot * (1 - ca),
                   z * ca + cz * sa + c[2] * dot * (1 - ca))
    Pq = (x.astype(F32), y.astype(F32), z.astype(F32))
    Pq = warp(Pq, 0.18, 1.5, 4, 401)
    Pq = warp(Pq, 0.05, 6.0, 3, 411)
    al = np.abs(lat)
    # Latitude-stretched noise gives the zonal bands; near the poles it would pinch into streaks, so it fades to isotropic.
    iso = smooth(58.0, 78.0, al)
    base = mix(fbm(Pq, 2.2, 8, 421, gain=0.55, stretch=(1, 1, 2.2)), fbm(Pq, 2.2, 8, 423, gain=0.55), iso)
    wisp = mix(ridged(Pq, 7.0, 4, 431, stretch=(1, 1, 3.0)), ridged(Pq, 7.0, 4, 433), iso)
    belt = (0.20 * np.exp(-(lat / 7.0) ** 2) - 0.22 * np.exp(-((al - 24.0) / 8.0) ** 2)
            + 0.16 * np.exp(-((al - 52.0) / 11.0) ** 2) + 0.04 * smooth(65, 85, al))
    dens = base + belt + 0.08
    cover = smooth(0.2, 0.62, dens)
    cover = np.maximum(cover, smooth(0.8, 0.95, wisp) * smooth(0.05, 0.25, dens) * 0.5)
    # Storm eyes clear.
    for lon, lat0, rad in storms:
        d = angle_to(P, lon, lat0)
        cover *= 1 - np.exp(-(d / (rad * 0.06)) ** 2) * 0.6
    cover = np.clip(cover * 0.85, 0, 1)
    return cover.reshape(H, W)


# ----------------------------------------------------------------------------------------------------------------
# Preview renderer (orthographic, Unity's convention: east to the right, north up)
# ----------------------------------------------------------------------------------------------------------------

def sample(img, u, v):
    H, W = img.shape[:2]
    x = u * W - 0.5
    y = v * H - 0.5
    x0 = np.floor(x).astype(int)
    y0 = np.floor(y).astype(int)
    fx, fy = (x - x0)[..., None], (y - y0)[..., None]
    x1 = (x0 + 1) % W
    x0 %= W
    y1 = np.clip(y0 + 1, 0, H - 1)
    y0 = np.clip(y0, 0, H - 1)
    im = img if img.ndim == 3 else img[..., None]
    r = (im[y0, x0] * (1 - fx) + im[y0, x1] * fx) * (1 - fy) + (im[y1, x0] * (1 - fx) + im[y1, x1] * fx) * fy
    return r if img.ndim == 3 else r[..., 0]


def render(maps, cloud, lon0, lat0, size=1100, zoom=1.0, light=(-0.35, 0.45, 0.82), night=0.0, markers=None,
           clouds_on=True, shift=(0.0, 0.0), cloud_k=0.7):
    alb = maps["albedo_lin"]
    nrm = maps["normal"]
    msk = maps["mask"]
    emi = maps["emission"]
    span = 1.12 / zoom
    s = (np.arange(size) + 0.5) / size * 2 - 1
    sx, sy = np.meshgrid(s * span + shift[0], -s * span + shift[1])
    r2 = sx * sx + sy * sy
    hit = r2 < 1
    sz = np.sqrt(np.clip(1 - r2, 0, 1))
    c, e, n = frame(lon0, lat0)
    d = c[None, None] * sz[..., None] + e[None, None] * sx[..., None] + n[None, None] * sy[..., None]
    lat = np.degrees(np.arcsin(np.clip(d[..., 2], -1, 1)))
    lon = np.degrees(np.arctan2(d[..., 1], d[..., 0]))
    u, v = (lon + 180) / 360, (lat + 90) / 180
    A = sample(alb, u, v)
    Nt = sample(nrm, u, v)
    M = sample(msk, u, v)
    Em = sample(emi, u, v)
    lo, la = np.radians(lon), np.radians(lat)
    E = np.stack([-np.sin(lo), np.cos(lo), np.zeros_like(lo)], -1)
    Nn = np.stack([-np.sin(la) * np.cos(lo), -np.sin(la) * np.sin(lo), np.cos(la)], -1)
    wn = E * Nt[..., :1] + Nn * Nt[..., 1:2] + d * Nt[..., 2:3]
    wn /= np.linalg.norm(wn, axis=-1, keepdims=True) + 1e-6
    basis = np.stack([e, n, c])  # view x, y, z
    vn = wn @ basis.T
    gn = np.stack([sx, sy, sz], -1)
    L = np.array(light, F32)
    L /= np.linalg.norm(L)
    ndl_g = (gn * L).sum(-1)
    day = smooth(-0.12, 0.25, ndl_g)
    ndl = np.clip((vn * L).sum(-1), 0, 1)
    diffuse = ndl * day
    Hh = L + np.array([0, 0, 1], F32)
    Hh /= np.linalg.norm(Hh)
    spec = np.power(np.clip((gn * Hh).sum(-1), 0, 1), 2400) * M[..., 1] * (1 - M[..., 0]) * 1.2 * day
    col_ = A * (diffuse[..., None] * np.array([1.0, 0.97, 0.92]) * 1.25 + 0.006 + 0.03 * (night == 0)) + spec[..., None]
    if clouds_on:
        C = sample(cloud, u, v)
        # shadow: sample clouds offset toward the sun along the surface
        off = 0.004
        Cs = sample(cloud, u - L[0] * off, v - L[1] * off)
        col_ *= (1 - 0.45 * cloud_k * Cs[..., None] * day[..., None])
        cl = np.clip(ndl_g * 0.9 + 0.1, 0, 1) * day
        col_ = mix(col_, np.array([0.92, 0.95, 1.0]) * cl[..., None] * 1.15 + 0.02, C[..., None] * cloud_k)
        Em = Em * (1 - 0.6 * C[..., None])
    glow_k = mix(0.45, 1.6, 1 - day) * (0.35 + 0.65 * night) if night > 0 else np.full(day.shape, 0.45, F32)
    col_ += srgb_to_lin(Em) * glow_k[..., None] * 1.5
    fres = np.power(1 - sz, 2.6)
    atmo = np.array([0.30, 0.62, 1.0]) * (fres * (0.03 + 0.97 * smooth(-0.15, 0.4, ndl_g)))[..., None] * 0.9
    twil = np.array([1.0, 0.42, 0.30]) * (np.exp(-((ndl_g - 0.03) / 0.05) ** 2) * np.power(1 - sz, 3.0) * 0.5)[..., None] if night > 0 else 0
    col_ = col_ + atmo + twil
    out = np.zeros((size, size, 3), F32)
    # background: space + halo
    r = np.sqrt(r2)
    halo = np.exp(-np.clip(r - 1, 0, None) / 0.025) * (r >= 1)
    hdl = smooth(-0.4, 0.5, (np.stack([sx, sy, np.zeros_like(sx)], -1) / np.maximum(r, 1e-6)[..., None] * L).sum(-1))
    if night == 0:
        hdl = np.ones_like(hdl) * 0.8
    bg = np.array([0.004, 0.005, 0.012]) + np.array([0.30, 0.62, 1.0]) * (halo * (0.2 + 0.8 * hdl))[..., None] * 0.7
    rng = np.random.default_rng(3)
    stars = (rng.random((size, size)) > 0.9985) * rng.random((size, size)) * 0.6
    bg = bg + stars[..., None]
    out = np.where(hit[..., None], col_, bg)
    out = out / (1 + out * 0.35) * 1.18  # gentle shoulder
    img = lin_to_srgb(out)
    if markers:
        for (mlon, mlat, rgb) in markers:
            p = vec(mlon, mlat)
            if np.dot(p, c) <= 0:
                continue
            px = (np.dot(p, e) - shift[0]) / span
            py = (np.dot(p, n) - shift[1]) / span
            cxp, cyp = (px + 1) / 2 * size, (1 - (py + 1) / 2) * size
            yy, xx = np.ogrid[:size, :size]
            rr = np.sqrt((xx - cxp) ** 2 + (yy - cyp) ** 2)
            rad = max(3.0, 5.5 * zoom)
            ring = (np.abs(rr - rad) < 1.3)
            img[ring] = rgb
    return img[::-1]  # bottom-up for write_png


def load_png_bpy(path):
    try:
        import bpy
    except ImportError:
        return None
    im = bpy.data.images.load(path)
    im.colorspace_settings.name = "Non-Color"
    w, h = im.size
    px = np.empty(w * h * 4, F32)
    im.pixels.foreach_get(px)
    return px.reshape(h, w, 4)[..., :3]  # bottom-up


def resize(img, H, W):
    h, w = img.shape[:2]
    ys = np.clip(((np.arange(H) + 0.5) * h / H).astype(int), 0, h - 1)
    xs = np.clip(((np.arange(W) + 0.5) * w / W).astype(int), 0, w - 1)
    return img[ys][:, xs]


def box_resize(img, factor):
    h, w = img.shape[:2]
    h2, w2 = h // factor, w // factor
    return img[:h2 * factor, :w2 * factor].reshape(h2, factor, w2, factor, -1).mean(axis=(1, 3))


# ----------------------------------------------------------------------------------------------------------------

def main():
    W, H = RES, RES // 2
    lay = build_layout(1024 if RES >= 2048 else 512, 512 if RES >= 2048 else 256)
    if _arg("--layoutonly", False):
        preview_layout(lay, os.path.join(RENDERS or OUT, "layout_preview.png"))
        G0 = Grid(*lay["land"].shape[::-1])
        wg = area_weights(G0).reshape(G0.h, G0.w)
        owner = np.where(lay["core"] < CORE_ID_RADIUS, 5, np.argmax(lay["soft"], axis=0))
        land = lay["land"] > 0
        log("areas %: " + ", ".join(f"{r[0]} {wg[land & (owner == i)].sum() * 100:.2f}" for i, r in enumerate(REGIONS)) +
            f", ocean {wg[~land].sum() * 100:.1f}")
        return
    maps, G = paint(W, H, lay)
    cover = clouds(W, H)
    log("normal map")
    nrm = normal_map(maps["elev_land"], 0.05 * (W / 4096.0) ** 0 * 1.0)
    maps["normal"] = nrm
    os.makedirs(OUT, exist_ok=True)
    low = os.path.join(OUT, "Low")
    os.makedirs(low, exist_ok=True)

    def save(name, img, bits=8, nearest=False):
        write_png(os.path.join(OUT, f"Planet_S69_{name}.png"), img, bits)
        lo = img[0::2, 0::2] if nearest else downsample2(img.astype(F32))
        write_png(os.path.join(low, f"Planet_S69_{name}.png"), lo, bits)
        log(f"wrote {name}")
    if not _arg("--skipwrite", False):
        save("Albedo", maps["albedo"])
        save("Height", maps["height"], bits=16)
        save("Normal", nrm * 0.5 + 0.5)
        mask = maps["mask"].copy()
        save("Mask", mask, nearest=False)
        save("Regions", maps["regions"], nearest=True)
        save("Emission", maps["emission"])
        save("Clouds", np.repeat(cover[..., None], 4, axis=2))
    # Node check: every Crystal Plains level on land and how far inland.
    land = maps["mask"][..., 0]
    checks = []
    for (lon, lat), name in zip(NODES, NODE_NAMES):
        x = int((lon + 180) / 360 * W) % W
        y = int((lat + 90) / 180 * H)
        rid = int(round(maps["regions"][y, x, 0] * 255))
        # distance to the nearest sea pixel within 15 degrees
        r = int(15 / 180 * H)
        ys, xs = np.mgrid[-r:r + 1, -r:r + 1]
        yy = np.clip(y + ys, 0, H - 1)
        xx = (x + xs) % W
        sea = land[yy, xx] < 0.5
        dd = np.sqrt((ys * 180 / H) ** 2 + (xs * 360 / W * math.cos(math.radians(lat))) ** 2)
        inland = float(dd[sea].min()) if sea.any() else 15.0
        checks.append(dict(name=name, lon=lon, lat=lat, land=bool(land[y, x] > 0.5), region_value=rid,
                           degrees_to_coast=round(inland, 1)))
    spacing = min(math.degrees(math.acos(float(np.clip(np.dot(vec(*NODES[i]), vec(*NODES[k])), -1, 1))))
                  for i in range(len(NODES)) for k in range(i))
    node_ok = (all(c["land"] for c in checks) and all(c["region_value"] == REGIONS[0][5] for c in checks)
               and min(c["degrees_to_coast"] for c in checks) >= NODE_COAST_MIN and spacing >= NODE_SPACING_MIN)
    log("node check: min inland %.1f deg, min spacing %.2f deg, all on land: %s, all in C1: %s -> %s" % (
        min(c["degrees_to_coast"] for c in checks), spacing, all(c["land"] for c in checks),
        all(c["region_value"] == REGIONS[0][5] for c in checks), "OK" if node_ok else "FAILED"))
    # Surface share and extent per region, measured on the written region map (land only).
    wg = np.broadcast_to(np.cos(np.radians(G.lat1))[:, None], (H, W)).astype(np.float64)
    wg = wg / wg.sum()
    rid_map = np.round(maps["regions"][..., 0] * 255).astype(int)
    surface = {}
    for (k, nm, lo, la, rd, v) in REGIONS:
        msk = rid_map == v
        c = vec(lo, la)
        ys, xs = np.nonzero(msk[::4, ::4])
        latr, lonr = np.radians(G.lat1[ys * 4]), np.radians(G.lon1[xs * 4])
        ang = np.degrees(np.arccos(np.clip(np.cos(latr) * np.cos(lonr) * c[0] + np.cos(latr) * np.sin(lonr) * c[1]
                                           + np.sin(latr) * c[2], -1, 1)))
        surface[k] = dict(name=nm, land_percent=round(float(wg[msk].sum() * 100), 2),
                          extent_p95_degrees=round(float(np.percentile(ang, 95)), 1) if len(ang) else 0.0)
    surface["ocean"] = dict(land_percent=round(float(wg[rid_map == 0].sum() * 100), 2))
    log("surface %: " + ", ".join(f"{k} {v['land_percent']}" for k, v in surface.items()))
    report = dict(
        name="Planet_S69", size=[W, H], low_size=[W // 2, H // 2],
        uv_convention="Equirectangular. u = (longitude + 180) / 360 with east (positive longitude) to the right; "
                      "v = (latitude + 90) / 180; PNG top row = north pole (v = 1). Identical to "
                      "Game.Levels.PlanetCoordinates.ToUv; the globe mesh in PlanetGlobe already uses it.",
        files={
            "Albedo": "sRGB colour, unlit, no clouds. Import sRGB, wrap U Repeat / V Clamp, aniso 8.",
            "Height": "16-bit grey, linear. 0.5 = sea level, 0.5..1 land (1 = highest peak), 0..0.5 sea floor.",
            "Normal": "Tangent space from land relief (sea surface flat). R = +east, G = +north (OpenGL / Unity "
                      "convention, same as GlobePainter.Normals). Import as Normal map.",
            "Mask": "Linear. R land 0/1 (antialiased coast), G specular/wetness (sea 1, lakes 0.85, black ice 0.6, "
                    "snow ~0.5, dry land ~0.1), B glow strength, A region id (same values as Regions.R).",
            "Regions": "Linear, sample with POINT filtering. R region id on land only, G region id including its "
                       "sea territory, B soft membership 0..1 for a gentle highlight falloff. id = round(value*255/40).",
            "Emission": "sRGB glow, multiply by an HDR colour/intensity; strongest on the night side.",
            "Clouds": "Linear; cover in R, G, B and A (the existing shaders read .a). Wraps in longitude.",
        },
        regions=[dict(key=k, name=nm, longitude=lo, latitude=la, radius_degrees=rd, value=v, value01=round(v / 255, 4),
                      id=v // 40) for (k, nm, lo, la, rd, v) in REGIONS] + [dict(key="-", name="ocean / none", value=0)],
        region_note="The Core (C6) is the crater on Southern Crown's northern arm at its MapRegion centre "
                    f"({CORE_LON:g}, {CORE_LAT:g}); its surface id covers the crater to {CORE_ID_RADIUS:g} deg.",
        layout="layout.json (round 21): region centres, the Crystal Plains node proposal, lobes, ranges, rivers",
        coast_biases=lay["biases"],
        surface=surface,
        node_min_spacing_degrees=round(spacing, 2),
        nodes_ok=node_ok,
        nodes=checks,
        shader_hints=dict(
            Planet_shader="Feed Albedo -> _AlbedoMap with _TextureTint = 1 (the map already carries the biomes; "
                          "set _Alien = 0 or bypass the procedural land palette), Normal -> _NormalMap, "
                          "Emission -> _LightsMap with _CityColor ~ (1.6, 1.6, 1.6), Clouds -> _CloudMap. "
                          "_GlossMap expects sea in .a: use a gloss texture with A = Mask.G, or change the shader "
                          "to read Mask.r (land) and Mask.g (specular).",
            relief="_BumpStrength 1.0-1.5 for the map view, 2 for the cutscene; Height can drive a parallax or "
                   "a sea-depth tint if wanted.",
            map_view="Lit from the camera side: ambient 0.2, emission x0.4 by day so crystal fields still sparkle.",
            highlight="Highlight a region by comparing round(Regions.G*255/40) with the selected id; "
                      "use Regions.B for a soft edge.",
        ),
    )
    with open(os.path.join(OUT, "Planet_S69_report.json"), "w") as f:
        json.dump(report, f, indent=2)

    if NORENDER or not RENDERS:
        log("done (no renders)")
        return
    os.makedirs(RENDERS, exist_ok=True)
    maps["albedo_lin"] = srgb_to_lin(maps["albedo"])
    marks = [(lon, lat, (1.0, 0.85, 0.35) if i < 6 else (0.75, 0.85, 1.0)) for i, (lon, lat) in enumerate(NODES)]
    log("renders")
    LG = (-0.3, 0.35, 0.89)
    globe_views = {
        "globe_crystal_plains": (-40, 6), "globe_ashen_reach": (60, -10), "globe_core_side": (150, -25),
        "globe_mirewood": (-140, 12), "globe_north": (40, 62), "globe_south": (160, -62),
    }
    olds = []
    for cdir in (COMPARE_DIR.split(";") if COMPARE_DIR else []):
        if not os.path.isdir(cdir):
            continue

        def ld(name):
            return load_png_bpy(os.path.join(cdir, f"Planet_S69_{name}.png"))
        oa, on, om, oe, oc = ld("Albedo"), ld("Normal"), ld("Mask"), ld("Emission"), ld("Clouds")
        if oa is not None:
            olds.append((dict(albedo_lin=srgb_to_lin(oa), normal=on * 2 - 1, mask=om, emission=oe), oc[..., 0]))
    for name, (lo, la) in globe_views.items():
        img = render(maps, cover, lo, la, 900, 1.0, LG, 0.0)
        write_png(os.path.join(RENDERS, f"{name}.png"), img)
        if olds:
            panels = [render(o[0], o[1], lo, la, 900, 1.0, LG, 0.0) for o in olds] + [img]
            write_png(os.path.join(RENDERS, f"compare_{name}.png"), np.concatenate(panels, axis=1))
        log(f"render {name}")
    img = render(maps, cover, -10, 10, 1000, 1.0, (0.95, 0.2, 0.25), 1.0)
    write_png(os.path.join(RENDERS, "globe_terminator.png"), img)
    img = render(maps, cover, -39, 3, 1400, 2.3, LG, 0.0, marks, clouds_on=False)
    write_png(os.path.join(RENDERS, "closeup_crystal_plains_nodes.png"), img)
    img = render(maps, cover, -39, 3, 1400, 2.3, LG, 0.0, None, cloud_k=0.5)
    write_png(os.path.join(RENDERS, "closeup_crystal_plains.png"), img)
    img = render(maps, cover, -39, 3, 1200, 1.7, (0.9, 0.25, 0.35), 1.0, None, clouds_on=False)
    write_png(os.path.join(RENDERS, "closeup_crystal_plains_night.png"), img)
    tiles = []
    for (k, nm, lo, la, rd, v) in REGIONS:
        tiles.append(render(maps, cover, lo, la if abs(la) < 50 else la * 0.8, 600, 1.6, LG, 0.0, cloud_k=0.45))
    sheet = np.concatenate([np.concatenate(tiles[3:], axis=1), np.concatenate(tiles[:3], axis=1)], axis=0)
    write_png(os.path.join(RENDERS, "regions_sheet.png"), sheet)
    f = max(1, W // 2048)
    flat = [box_resize(maps["albedo"], f), box_resize(maps["emission"], f),
            box_resize(np.repeat(maps["height"][..., None], 3, 2), f),
            box_resize(np.repeat(cover[..., None], 3, 2), f)]
    reg = maps["regions"][::f, ::f, :1]
    palette = np.array([[0.05, 0.05, 0.08], [0.45, 0.85, 0.95], [0.95, 0.45, 0.15], [0.9, 0.95, 1.0], [0.2, 0.7, 0.4],
                        [0.9, 0.2, 0.7], [0.6, 0.3, 1.0]], F32)
    regc = palette[np.clip(np.round(reg[..., 0] * 255 / 40).astype(int), 0, 6)]
    regt = palette[np.clip(np.round(maps["regions"][::f, ::f, 1] * 255 / 40).astype(int), 0, 6)] * 0.45
    regc = np.where(reg > 0, regc, regt)
    flat.append(regc)
    flat.append(box_resize(nrm * 0.5 + 0.5, f))
    write_png(os.path.join(RENDERS, "flat_albedo.png"), flat[0])
    write_png(os.path.join(RENDERS, "flat_emission.png"), flat[1])
    write_png(os.path.join(RENDERS, "flat_regions.png"), flat[4])
    sm = [resize(x, 512, 1024) for x in flat]
    sheet = np.concatenate([np.concatenate(sm[4:6], 1), np.concatenate(sm[2:4], 1), np.concatenate(sm[0:2], 1)], 0)
    write_png(os.path.join(RENDERS, "flat_sheet.png"), sheet)
    log("done")


main()
