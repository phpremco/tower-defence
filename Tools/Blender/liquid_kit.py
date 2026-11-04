"""Round 24 helpers for the liquid and mist texture set (`build_liquids.py`): periodic (seamless) noise, a periodic
Worley field, an FFT ocean with a loopable time axis, photon-splat caustics, height-to-normal conversion, drawing
helpers, flipbook atlases and a small PNG writer.

Pure numpy, no bpy, so the texture half of the build also runs in Blender's bundled Python without Blender.
Every field here is periodic over its image size: spectral noise lives on integer frequencies, the Worley points wrap,
warps sample with wrap-around and the ocean uses integer wave vectors. That is what makes each texture tile.
Image convention: row 0 = top of the PNG, x = column (right), y = up = -row (OpenGL / Unity normal maps).
"""

import math
import os
import struct
import zlib

import numpy as np

F32 = np.float32


# ------------------------------------------------------------------------------------------------------------ PNG

def _chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def save_png(path, arr):
    """Float image (H x W, or H x W x 1..4, values 0..1, row 0 = top) to an 8-bit PNG. Grey, grey+alpha, RGB or RGBA
    follows the channel count. Uses the Up filter, which suits smooth masks and normals."""
    a = np.clip(np.nan_to_num(np.asarray(arr, dtype=F32)), 0.0, 1.0)
    if a.ndim == 2:
        a = a[..., None]
    h, w, c = a.shape
    color_type = {1: 0, 2: 4, 3: 2, 4: 6}[c]
    q = (a * 255.0 + 0.5).astype(np.uint8).reshape(h, w * c)
    up = q.astype(np.int16)
    up[1:] -= q[:-1].astype(np.int16)
    up = (up & 0xFF).astype(np.uint8)
    raw = np.concatenate([np.full((h, 1), 2, np.uint8), up], axis=1).tobytes()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(_chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, color_type, 0, 0, 0)))
        f.write(_chunk(b"IDAT", zlib.compress(raw, 7)))
        f.write(_chunk(b"IEND", b""))
    return path


# ---------------------------------------------------------------------------------------------------- basic maths

def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def norm01(x, lo=0.0, hi=100.0):
    """Rescales to 0..1 between two percentiles (robust against a few extreme pixels)."""
    a, b = np.percentile(x, [lo, hi])
    return np.clip((x - a) / max(b - a, 1e-8), 0.0, 1.0).astype(F32)


def standardise(x):
    x = x - x.mean()
    return (x / max(float(x.std()), 1e-8)).astype(F32)


def pixel_grid(h, w=None):
    w = w or h
    y, x = np.mgrid[0:h, 0:w].astype(F32)
    return x, y


def int_freqs(n):
    return np.fft.fftfreq(n, 1.0 / n).astype(F32)


# ------------------------------------------------------------------------------------------------- spectral noise

def spectral(n, seed, k_lo, k_hi, beta=2.0, stretch=(1.0, 1.0), angle=0.0, m=None):
    """Seamless band-limited fractal noise (n x m), zero mean, unit std. k_lo/k_hi are in cycles per tile; beta is
    the spectral slope (2 = soft fbm, 1 = rough). `stretch` squashes frequencies along a rotated axis, so (1, 3) makes
    features three times longer along `angle` (radians)."""
    m = m or n
    rng = np.random.default_rng(seed)
    white = np.fft.fft2(rng.standard_normal((n, m)).astype(F32))
    ky = int_freqs(n)[:, None]
    kx = int_freqs(m)[None, :]
    ca, sa = math.cos(angle), math.sin(angle)
    u = (kx * ca + ky * sa) * stretch[1]
    v = (-kx * sa + ky * ca) * stretch[0]
    k = np.sqrt(u * u + v * v)
    k[0, 0] = 1.0
    amp = k ** (-beta / 2.0) * np.exp(-(k / k_hi) ** 4) * (1.0 - np.exp(-(k / k_lo) ** 4))
    amp[0, 0] = 0.0
    return standardise(np.real(np.fft.ifft2(white * amp)))


def spectral3(n, frames, seed, k_lo, k_hi, kt_hi, beta=2.0):
    """Seamless noise over (t, y, x), periodic in all three: a loopable animated noise. kt_hi limits how fast it
    changes per loop (in cycles per loop)."""
    rng = np.random.default_rng(seed)
    white = np.fft.fftn(rng.standard_normal((frames, n, n)).astype(F32))
    kt = int_freqs(frames)[:, None, None]
    ky = int_freqs(n)[None, :, None]
    kx = int_freqs(n)[None, None, :]
    k = np.sqrt(kx * kx + ky * ky)
    ks = k.copy()
    ks[ks == 0] = 1.0
    amp = ks ** (-beta / 2.0) * np.exp(-(k / k_hi) ** 4) * (1.0 - np.exp(-(k / k_lo) ** 4)) * np.exp(-(kt / kt_hi) ** 2)
    amp[:, 0, 0] = 0.0
    out = np.real(np.fft.ifftn(white * amp))
    return standardise(out)


def ridged(x):
    """1 - |noise|: thin bright crests where the noise crosses zero (veins, cracks, crest lines)."""
    return (1.0 - np.abs(np.tanh(x * 0.9))).astype(F32)


# ----------------------------------------------------------------------------------------------- sampling, warps

def sample_wrap(img, x, y):
    """Bilinear sample of a periodic image at float pixel coordinates (wraps both axes). img: H x W (x C)."""
    h, w = img.shape[:2]
    x0 = np.floor(x).astype(np.int64)
    y0 = np.floor(y).astype(np.int64)
    fx = (x - x0).astype(F32)
    fy = (y - y0).astype(F32)
    if img.ndim == 3:
        fx = fx[..., None]
        fy = fy[..., None]
    x0 %= w
    y0 %= h
    x1 = (x0 + 1) % w
    y1 = (y0 + 1) % h
    a = img[y0, x0] * (1 - fx) + img[y0, x1] * fx
    b = img[y1, x0] * (1 - fx) + img[y1, x1] * fx
    return (a * (1 - fy) + b * fy).astype(F32)


def warp(img, dx, dy):
    """Samples img displaced by (dx, dy) pixels per pixel, wrapping, so a periodic image stays periodic."""
    x, y = pixel_grid(*img.shape[:2])
    return sample_wrap(img, x + dx, y + dy)


def blur_wrap(img, sigma):
    """Gaussian blur on the torus (FFT), sigma in pixels. Works on H x W or H x W x C."""
    h, w = img.shape[:2]
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    kern = np.exp(-2.0 * math.pi ** 2 * sigma ** 2 * (fx * fx + fy * fy))
    if img.ndim == 2:
        return np.real(np.fft.ifft2(np.fft.fft2(img) * kern)).astype(F32)
    return np.stack([blur_wrap(img[..., c], sigma) for c in range(img.shape[2])], axis=-1)


def resize_box(img, factor=2):
    h, w = img.shape[:2]
    a = img[: h // factor * factor, : w // factor * factor]
    shape = (h // factor, factor, w // factor, factor) + a.shape[2:]
    return a.reshape(shape).mean(axis=(1, 3)).astype(F32)


# ------------------------------------------------------------------------------------------------------- worley

def worley(x, y, period, cells, seed, jitter=0.85):
    """Periodic Worley field at pixel coordinates (x, y) on a tile of `period` pixels with cells x cells feature
    points. Returns F1, F2 (in cell units) and the id (0..1 random value) of the nearest cell. x, y may be warped
    coordinates; the field wraps every `period` pixels."""
    rng = np.random.default_rng(seed)
    jit = (0.5 + (rng.random((cells, cells, 2)) - 0.5) * jitter).astype(F32)
    ids = rng.random((cells, cells)).astype(F32)
    u = x * (cells / period)
    v = y * (cells / period)
    iu = np.floor(u).astype(np.int64)
    iv = np.floor(v).astype(np.int64)
    f1 = np.full(u.shape, 9.0, F32)
    f2 = np.full(u.shape, 9.0, F32)
    idv = np.zeros(u.shape, F32)
    for dv in (-1, 0, 1):
        for du in (-1, 0, 1):
            cu = iu + du
            cv = iv + dv
            wu = cu % cells
            wv = cv % cells
            px = cu + jit[wv, wu, 0]
            py = cv + jit[wv, wu, 1]
            d = np.sqrt((px - u) ** 2 + (py - v) ** 2).astype(F32)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            idv = np.where(closer, ids[wv, wu], idv)
            f1 = np.where(closer, d, f1)
    return f1, f2, idv


# ------------------------------------------------------------------------------------------------------- normals

def height_to_normal(h, strength):
    """Tangent-space normal (OpenGL / Unity: R = +x right, G = +y up) from a periodic height field. strength =
    height units per pixel of slope; returns float RGB in 0..1."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy_up = (np.roll(h, 1, axis=0) - np.roll(h, -1, axis=0)) * 0.5
    nx = -dx * strength
    ny = -dy_up * strength
    nz = np.ones_like(nx)
    inv = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx * inv * 0.5 + 0.5, ny * inv * 0.5 + 0.5, nz * inv * 0.5 + 0.5], axis=-1).astype(F32)


def normal_flat_rate(n):
    """Mean z of an encoded normal map: 1 = flat. Used in the report to say how strong a map is."""
    return float((n[..., 2] * 2 - 1).mean())


def resize_normal(n, factor=2):
    v = resize_box(n * 2.0 - 1.0, factor)
    v /= np.maximum(np.linalg.norm(v, axis=-1, keepdims=True), 1e-6)
    return (v * 0.5 + 0.5).astype(F32)


# ---------------------------------------------------------------------------------------------------------- ocean

def ocean(n, seed, frames=1, wind_dir=0.3, k_peak=6.0, k_hi=60.0, directional=2.0, iso=0.35, loop=1.0):
    """FFT ocean on a seamless tile with integer wave vectors. Returns (frames, n, n) height plus the spectrum
    function so derivatives can be taken spectrally. The dispersion w = sqrt(k) is quantised to whole cycles per
    loop, so the animation loops exactly after `frames`. k_peak sets the dominant wavelength (cycles per tile)."""
    rng = np.random.default_rng(seed)
    ky = int_freqs(n)[:, None]
    kx = int_freqs(n)[None, :]
    k = np.sqrt(kx * kx + ky * ky)
    ks = np.where(k == 0, 1.0, k)
    cosw = (kx * math.cos(wind_dir) + ky * math.sin(wind_dir)) / ks
    spread = iso + (1.0 - iso) * np.abs(cosw) ** directional
    # A JONSWAP-like peak: little energy below k_peak, k^-3.5 above it, cut at k_hi.
    phil = np.exp(-1.25 * (k_peak / ks) ** 2) * ks ** -3.5 * spread * np.exp(-(k / k_hi) ** 2)
    phil[0, 0] = 0.0
    h0 = (rng.standard_normal((n, n)) + 1j * rng.standard_normal((n, n))) * np.sqrt(phil / 2.0)
    h0m = np.conj(np.roll(np.flip(h0), 1, axis=(0, 1)))
    w = np.round(np.sqrt(ks) * 1.6 * loop) * 2.0 * math.pi
    spectra = []
    for f in range(frames):
        t = f / frames
        spectra.append(h0 * np.exp(1j * w * t) + h0m * np.exp(-1j * w * t))
    return np.stack(spectra), kx, ky


def from_spectrum(spec, kx=None, ky=None, d=""):
    """Height or a derivative of it ('x', 'y', 'xx', 'yy', 'xy'; x = column, y = row) from a spectrum."""
    s = spec
    n = spec.shape[-1]
    tau = 2.0 * math.pi / n
    for axis in d:
        s = s * (1j * (kx if axis == "x" else ky) * tau)
    return np.real(np.fft.ifft2(s)).astype(F32)


def caustics(spec, kx, ky, out_n, depth, supersample=2):
    """Photon-splat caustics on a seamless tile: light falls straight down, refracts at the surface and lands on a
    flat bottom `depth` (pixels x slope gain) below; each photon is deposited where its ray lands, wrapping. Sharp
    filaments where rays focus, dark cells where they spread; mean intensity 1."""
    n = spec.shape[-1]
    gx = from_spectrum(spec, kx, ky, "x")
    gy = from_spectrum(spec, kx, ky, "y")
    xs, ys = pixel_grid(n * supersample)
    xs = (xs + 0.5) / supersample
    ys = (ys + 0.5) / supersample
    sx = sample_wrap(gx, xs - 0.5, ys - 0.5)
    sy = sample_wrap(gy, xs - 0.5, ys - 0.5)
    lx = (xs - depth * sx) * (out_n / n)
    ly = (ys - depth * sy) * (out_n / n)
    # Bilinear splat so the filaments stay smooth instead of speckled.
    x0 = np.floor(lx).astype(np.int64)
    y0 = np.floor(ly).astype(np.int64)
    fx = lx - x0
    fy = ly - y0
    acc = np.zeros(out_n * out_n, np.float64)
    for oy, wy in ((0, 1 - fy), (1, fy)):
        for ox, wx in ((0, 1 - fx), (1, fx)):
            idx = ((y0 + oy) % out_n) * out_n + ((x0 + ox) % out_n)
            acc += np.bincount(idx.ravel(), (wx * wy).ravel(), minlength=out_n * out_n)
    img = acc.reshape(out_n, out_n).astype(F32)
    return img / img.mean()


# ------------------------------------------------------------------------------------------------------- drawing

def stamp_discs(n, centres, radii, profile, m=None):
    """Draws radial sprites on a seamless n x m tile. profile(r_norm) -> value for r_norm in 0..1.5 (r / radius),
    combined with max. Wraps sprites across the edges."""
    m = m or n
    out = np.zeros((n, m), F32)
    for (cx, cy), r in zip(centres, radii):
        ext = int(math.ceil(r * 1.6)) + 1
        xs = np.arange(int(cx) - ext, int(cx) + ext + 1)
        ys = np.arange(int(cy) - ext, int(cy) + ext + 1)
        dx = xs[None, :] + 0.5 - cx
        dy = ys[:, None] + 0.5 - cy
        val = profile(np.sqrt(dx * dx + dy * dy) / r).astype(F32)
        yy = (ys % n)[:, None]
        xx = (xs % m)[None, :]
        out[yy, xx] = np.maximum(out[yy, xx], val)
    return out


def atlas(frames, cols):
    """Frames (list of H x W x C) into a flipbook sheet: frame 0 top-left, then row by row (Unity order)."""
    rows = int(math.ceil(len(frames) / cols))
    h, w = frames[0].shape[:2]
    c = frames[0].shape[2] if frames[0].ndim == 3 else 1
    sheet = np.zeros((rows * h, cols * w, c), F32)
    for i, fr in enumerate(frames):
        r, q = divmod(i, cols)
        sheet[r * h:(r + 1) * h, q * w:(q + 1) * w] = fr.reshape(h, w, c)
    return sheet if c > 1 else sheet[..., 0]


def radial(n):
    """Centred radius (0 at the centre, 1 at the edge midpoint) and angle grids for an n x n sprite."""
    x, y = pixel_grid(n)
    dx = (x + 0.5) / n * 2 - 1
    dy = (y + 0.5) / n * 2 - 1
    return np.sqrt(dx * dx + dy * dy).astype(F32), np.arctan2(dy, dx).astype(F32), dx.astype(F32), dy.astype(F32)


def seam_error(img):
    """Mean absolute step across the wrap seam relative to the mean step inside the image; ~1 means seamless."""
    a = img.astype(np.float64)
    inner = np.abs(np.diff(a, axis=1)).mean() + np.abs(np.diff(a, axis=0)).mean()
    seam = np.abs(a[:, 0] - a[:, -1]).mean() + np.abs(a[0] - a[-1]).mean()
    return float(seam / max(inner, 1e-9))
