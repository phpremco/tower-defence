"""Round 26: numpy helpers for equirectangular texture sets (the dying Earth, the theme skies).

A read-only copy of the small toolbox in build_planet_s69.py (that script runs its whole build on import, so it
cannot be imported): seamless 3D Perlin / fbm / ridged noise sampled on the unit sphere, domain warp, polyline
distances on the sphere, a spherical blur, a 16-bit capable PNG writer and an orthographic globe renderer.

Frame (same as Planet_S69): x = cos(lat) cos(lon), y = cos(lat) sin(lon), z = sin(lat); image rows run south ->
north in memory and write_png flips them, so the PNG top row is the north pole (Unity v = 1). u = (lon + 180) / 360.
"""

import math
import os
import struct
import time
import zlib
from concurrent.futures import ThreadPoolExecutor

import numpy as np

F32 = np.float32
POOL = ThreadPoolExecutor(max(4, (os.cpu_count() or 8) - 2))
CHUNK = 1 << 17
T0 = time.time()


def log(tag, msg):
    print(f"[{tag}] {time.time() - T0:6.1f}s {msg}", flush=True)


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
        lat = ((np.arange(h, dtype=F32) + 0.5) / h * 180.0 - 90.0)
        lon = ((np.arange(w, dtype=F32) + 0.5) / w * 360.0 - 180.0)
        self.lat1, self.lon1 = lat, lon
        la, lo = np.radians(lat)[:, None], np.radians(lon)[None, :]
        self.x = (np.cos(la) * np.cos(lo)).astype(F32).ravel()
        self.y = (np.cos(la) * np.sin(lo)).astype(F32).ravel()
        self.z = np.broadcast_to(np.sin(la), (h, w)).astype(F32).ravel()
        self.latf = np.broadcast_to(lat[:, None], (h, w)).ravel().copy()
        self.lonf = np.broadcast_to(lon[None, :], (h, w)).ravel().copy()

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


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    return a + (b - a) * t


def col(*rgb):
    return np.array(rgb, F32)


def srgb_to_lin(c):
    return np.power(np.clip(c, 0, 1), 2.2)


def lin_to_srgb(c):
    return np.power(np.clip(c, 0, 1), 1 / 2.2)


# ---------------------------------------------------------------------------------------------------------------
# Noise
# ---------------------------------------------------------------------------------------------------------------

_GR = np.array([[1, 1, 0], [-1, 1, 0], [1, -1, 0], [-1, -1, 0], [1, 0, 1], [-1, 0, 1], [1, 0, -1], [-1, 0, -1],
                [0, 1, 1], [0, -1, 1], [0, 1, -1], [0, -1, -1]], F32)
_RNG = np.random.default_rng(2053)
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
    xx, yy, zz = x * stretch[0], y * stretch[1], z * stretch[2]
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


def angle_to(P, lon, lat):
    c = vec(lon, lat)
    return chunked(lambda x, y, z: np.degrees(np.arccos(np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1))), len(P[0]), *P)


def densify(poly, step=0.5):
    pts = []
    for (a, b) in zip(poly[:-1], poly[1:]):
        va, vb = vec(*a), vec(*b)
        ang = math.degrees(math.acos(float(np.clip(np.dot(va, vb), -1, 1))))
        k = max(2, int(ang / step))
        s = math.sin(math.radians(ang)) or 1e-6
        for t in np.linspace(0, 1, k, endpoint=False):
            wa = math.sin((1 - t) * math.radians(ang)) / s
            wb = math.sin(t * math.radians(ang)) / s
            pts.append(wa * va + wb * vb)
    pts.append(vec(*poly[-1]))
    return np.array(pts, F32)


def poly_distance(P, polys, step=0.5):
    """Angular distance (deg) from every point to the nearest of the polylines."""
    pts = np.concatenate([densify(p, step) for p in polys])

    def fn(x, y, z):
        best = np.full(len(x), -1.0, F32)
        for i in range(0, len(pts), 64):
            q = pts[i:i + 64]
            d = x[:, None] * q[None, :, 0] + y[:, None] * q[None, :, 1] + z[:, None] * q[None, :, 2]
            best = np.maximum(best, d.max(axis=1))
        return np.degrees(np.arccos(np.clip(best, -1, 1)))
    return chunked(fn, len(P[0]), *P)


# ---------------------------------------------------------------------------------------------------------------
# Images
# ---------------------------------------------------------------------------------------------------------------

def upsample(a, H, W):
    """Bilinear, pixel-centre aligned, wraps in longitude (works on (h, w) or (c, h, w))."""
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


def box_down(img, factor):
    """Box downsample by an integer factor ((h, w) or (h, w, c))."""
    h, w = img.shape[:2]
    s = img.reshape(h // factor, factor, w // factor, factor, *img.shape[2:]).astype(F32)
    return s.mean(axis=(1, 3))


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


def normal_map(elev, strength):
    """Tangent-space normals of an equirect height field: R = +east, G = +north (OpenGL)."""
    H, W = elev.shape
    lat = (np.arange(H) + 0.5) / H * 180 - 90
    cosl = np.maximum(np.cos(np.radians(lat)), 0.05)[:, None]
    px = 2 * math.pi / W
    dx = (np.roll(elev, -1, 1) - np.roll(elev, 1, 1)) / (2 * px * cosl)
    dy = np.zeros_like(elev)
    dy[1:-1] = (elev[2:] - elev[:-2]) / (2 * px)
    n = np.stack([-dx * strength, -dy * strength, np.ones_like(elev)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n.astype(F32)


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


def contact(images, columns, pad=8, bg=(0.03, 0.03, 0.04)):
    """Images (H, W, 3, top row first) of equal size into one sheet (top row first)."""
    h, w = images[0].shape[:2]
    rows = (len(images) + columns - 1) // columns
    out = np.ones((rows * (h + pad) + pad, columns * (w + pad) + pad, 3), F32) * np.array(bg, F32)
    for i, im in enumerate(images):
        r, c = divmod(i, columns)
        out[pad + r * (h + pad):pad + r * (h + pad) + h, pad + c * (w + pad):pad + c * (w + pad) + w] = im[..., :3]
    return out
