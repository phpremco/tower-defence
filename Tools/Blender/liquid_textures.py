"""Round 24: the texture builders of the liquid and mist set, one function per kind. Each returns a list of
Tex records (name, image, meta); build_liquids.py writes them, the Low/ copies and the report.

Pure numpy (liquid_kit.py). Sizes: tiling maps 1024 (TILE), flipbooks 4 x 4 frames of 256 px (FRAME).
Normal maps are OpenGL / Unity (G = up), linear. Masks and flow maps are linear. Only the *_Albedo maps are sRGB.
Values are kept neutral (grey or near-grey) wherever Unity tints per continent; the report carries the tints.
"""

import math

import numpy as np

import liquid_kit as K

F32 = np.float32
TILE = 1024
FRAME = 256
FRAMES = 16
COLS = 4


class Tex:
    def __init__(self, name, img, **meta):
        self.name = name
        self.img = img.astype(F32)
        self.meta = meta


def slope_normal(h, slope):
    """Normal map whose mean slope is `slope` (tan of the tilt): keeps every map's strength comparable."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy = (np.roll(h, 1, axis=0) - np.roll(h, -1, axis=0)) * 0.5
    g = float(np.sqrt(dx * dx + dy * dy).mean())
    return K.height_to_normal(h, slope / max(g, 1e-8))


def grid(n=TILE):
    return K.pixel_grid(n)


def curl_flow(psi, gain_pct=99.0):
    """Divergence-free flow (RG around 0.5) from a periodic stream function, plus speed in B."""
    vx = (np.roll(psi, 1, axis=0) - np.roll(psi, -1, axis=0)) * 0.5
    vy = -(np.roll(psi, -1, axis=1) - np.roll(psi, 1, axis=1)) * 0.5
    # vx along +x (right), vy along +y (up = -row): image rows are y-down, so the row difference already flips.
    sp = np.sqrt(vx * vx + vy * vy)
    s = max(float(np.percentile(sp, gain_pct)), 1e-8)
    vx, vy, sp = vx / s, vy / s, sp / s
    return np.stack([np.clip(vx * 0.5 + 0.5, 0, 1), np.clip(vy * 0.5 + 0.5, 0, 1), np.clip(sp, 0, 1)], axis=-1)


# -------------------------------------------------------------------------------------------------------- water

def water():
    out = []
    # Large swell: a directional wind sea, a few long crests per tile.
    spec, kx, ky = K.ocean(TILE, 101, wind_dir=0.45, k_peak=4.0, k_hi=34.0, directional=2.0, iso=0.35)
    swell = K.from_spectrum(spec[0])
    out.append(Tex("Water_Normal_Swell", slope_normal(swell, 0.085), kind="normal", wrap="Repeat",
                   note="large swell, directional (wind ~26 deg); scroll slowly along +x"))
    # Fine ripples: short, near-isotropic capillary chop plus a little wind texture.
    spec_r, _, _ = K.ocean(TILE, 102, wind_dir=-0.6, k_peak=22.0, k_hi=150.0, directional=1.0, iso=0.75)
    rip = K.standardise(K.from_spectrum(spec_r[0])) + 0.35 * K.spectral(TILE, 103, 60, 260, beta=1.6)
    out.append(Tex("Water_Normal_Ripple", slope_normal(rip, 0.12), kind="normal", wrap="Repeat",
                   note="fine ripples, near isotropic; scroll faster, opposite direction to the swell"))

    # Caustics flipbook: an animated ocean, 16 frames that loop, photons splatted onto a flat bottom.
    n = FRAME
    spec_c, kxc, kyc = K.ocean(n, 104, frames=FRAMES, wind_dir=0.2, k_peak=3.0, k_hi=14.0, directional=1.0,
                               iso=0.8, loop=1.0)
    hxx = K.from_spectrum(spec_c[0], kxc, kyc, "xx")
    depth = 1.15 / float(np.percentile(np.abs(hxx), 99.0))
    frames = []
    for f in range(FRAMES):
        c = K.caustics(spec_c[f], kxc, kyc, n, depth, supersample=3)
        frames.append(caustic_tone(K.blur_wrap(c, 0.7)))
    out.append(Tex("Water_Caustics_Flip", K.atlas(frames, COLS), kind="mask", wrap="Clamp (sample per frame with "
                   "frac(uv) inside the cell)", flipbook=[COLS, COLS], fps=8,
                   note="grey caustic light; each frame tiles; loops after 16 frames"))
    # A sharper single caustic tile for the Low profile (two scrolled samples, min() of both).
    spec_s, kxs, kys = K.ocean(512, 105, wind_dir=0.2, k_peak=3.5, k_hi=18.0, directional=1.0, iso=0.8)
    hxx = K.from_spectrum(spec_s[0], kxs, kys, "xx")
    depth = 1.15 / float(np.percentile(np.abs(hxx), 99.0))
    cs = K.caustics(spec_s[0], kxs, kys, TILE, depth * 2.0, supersample=2)
    out.append(Tex("Water_Caustics", caustic_tone(K.blur_wrap(cs, 1.0)), kind="mask", wrap="Repeat",
                   note="static caustic tile; animate by two scrolled samples combined with min()"))

    out.append(Tex("Water_Foam", water_foam(), kind="mask", wrap="Repeat",
                   note="R lace foam, G bubbles, B dissolve noise, A combined preview"))
    out.append(Tex("Water_ShoreProfile", shore_profile(), kind="ramp", wrap="Repeat U, Clamp V",
                   note="crest profile over one wave period (U); sample at frac(dist * freq + time)"))
    return out


def caustic_tone(c):
    """Photon density (mean 1) to a 0..1 light mask: dark cells stay near 0.05, filaments saturate softly."""
    return np.clip(1.0 - np.exp(-np.maximum(c - 0.35, 0.0) * 0.55), 0.0, 1.0) ** 1.15


def water_foam():
    x, y = grid()
    wx = K.spectral(TILE, 111, 3, 14) * 10.0
    wy = K.spectral(TILE, 112, 3, 14) * 10.0
    cover = K.smoothstep(-0.9, 1.2, K.spectral(TILE, 113, 2, 9, beta=2.5))
    f1, f2, _ = K.worley(x + wx, y + wy, TILE, 22, 114)
    g1, g2, _ = K.worley(x + wx * 0.5, y + wy * 0.5, TILE, 54, 115)
    # Foam is packed bubbles: round holes (near the cell points) that shrink where the foam is dense, so thin
    # areas read as lace and dense areas as a nearly closed sheet. F1 holes stay round; F2-F1 borders went spiky.
    hole1 = 0.62 - 0.45 * cover
    hole2 = 0.58 - 0.40 * cover
    big = K.smoothstep(hole1 - 0.06, hole1 + 0.08, f1)
    small = K.smoothstep(hole2 - 0.08, hole2 + 0.1, g1)
    lace = big * (0.55 + 0.45 * small)
    grain = 0.78 + 0.22 * K.norm01(K.spectral(TILE, 116, 80, 400, beta=1.0))
    lace = K.blur_wrap(lace, 0.8) * grain * K.smoothstep(0.05, 0.45, cover)
    rng = np.random.default_rng(117)
    pts, radii = [], []
    while len(pts) < 2600:
        p = rng.random(2) * TILE
        if rng.random() < cover[int(p[1]), int(p[0])] * 0.9 + 0.1:
            pts.append(p)
            radii.append(1.6 + 6.0 * rng.random() ** 3)

    def bubble(r):
        rim = np.exp(-((r - 0.82) / 0.16) ** 2)
        inner = 0.22 * (r < 0.8)
        return np.where(r < 1.15, np.maximum(rim, inner), 0.0)
    bub = K.stamp_discs(TILE, pts, radii, bubble)
    diss = K.norm01(K.spectral(TILE, 118, 2, 16, beta=1.8), 1, 99)
    comb = np.clip(np.maximum(lace, bub * 0.85), 0, 1)
    return np.stack([lace, bub, diss, comb], axis=-1)


def shore_profile():
    s = (np.arange(256) + 0.5) / 256.0
    # A sharp breaking front at s = 0.12 (towards the bank is s -> 0), a frothy tail fading towards s = 0.7.
    front = K.smoothstep(0.04, 0.12, s) * (1.0 - K.smoothstep(0.12, 0.2, s))
    tail = (1.0 - K.smoothstep(0.12, 0.72, s)) * K.smoothstep(0.05, 0.13, s) * 0.55
    p = np.maximum(front, tail)
    return np.repeat(p[None, :], 16, axis=0).astype(F32)


# --------------------------------------------------------------------------------------------------------- lava

def lava():
    out = []
    for layer, (seed, cells) in (("A", (201, 7)), ("B", (231, 10))):
        alb, nrm, heat = lava_crust(seed, cells)
        out.append(Tex(f"Lava_Crust{layer}_Albedo", alb, kind="albedo", wrap="Repeat",
                       note="RGB sRGB basalt crust, A crust coverage (1 plate, 0 molten seam)"))
        out.append(Tex(f"Lava_Crust{layer}_Normal", nrm, kind="normal", wrap="Repeat", note="domed plates, ropes, cracks"))
        out.append(Tex(f"Lava_Crust{layer}_Heat", heat, kind="mask", wrap="Repeat",
                       note="R seam heat (sharp), G halo glow (wide), B molten underflow pattern"))
    out.append(Tex("Lava_Flow", curl_flow(K.spectral(TILE, 251, 1.5, 6, beta=3.0)), kind="flow", wrap="Repeat",
                   note="RG divergence-free distortion vector (0.5 = none), B speed"))
    out.append(Tex("Lava_Bubble_Flip", lava_bubbles(), kind="mask", wrap="Clamp", flipbook=[COLS, COLS], fps=10,
                   note="R emission, G lit skin shade, B height, A alpha; swell 0-9, burst 10, crater fades 11-15"))
    return out


def lava_crust(seed, cells):
    x, y = grid()
    cell_px = TILE / cells
    wx = K.spectral(TILE, seed + 1, 2, 10) * cell_px * 0.14 + K.spectral(TILE, seed + 2, 10, 40) * cell_px * 0.03
    wy = K.spectral(TILE, seed + 3, 2, 10) * cell_px * 0.14 + K.spectral(TILE, seed + 4, 10, 40) * cell_px * 0.03
    f1, f2, pid = K.worley(x + wx, y + wy, TILE, cells, seed)
    edge = f2 - f1
    width = 0.035 + 0.075 * K.norm01(K.spectral(TILE, seed + 5, 3, 12), 2, 98)
    seam = 1.0 - K.smoothstep(width * 0.35, width, edge)
    # Fine fractures across the plates (crust shrinking as it cools); only some of them crack through.
    s1, s2, _ = K.worley(x + wx * 0.7, y + wy * 0.7, TILE, cells * 3, seed + 6)
    fine = (1.0 - K.smoothstep(0.0, 0.035, s2 - s1)) * K.smoothstep(0.08, 0.2, edge)
    fine *= K.smoothstep(0.1, 0.6, K.spectral(TILE, seed + 7, 4, 20))
    dome = K.smoothstep(0.0, 0.42, edge)
    ropes_a = K.ridged(K.spectral(TILE, seed + 8, 14, 60, stretch=(1.0, 3.5), angle=0.6))
    ropes_b = K.ridged(K.spectral(TILE, seed + 9, 14, 60, stretch=(1.0, 3.5), angle=2.1))
    ropes = np.where(pid > 0.5, ropes_a, ropes_b) ** 4
    grain = K.spectral(TILE, seed + 10, 120, 500, beta=0.8)
    height = dome * 0.9 + ropes * 0.22 * dome + grain * 0.03 - seam * 0.7 - fine * 0.25
    nrm = slope_normal(height, 0.32)

    tone = 0.85 + 0.3 * pid
    lum = (0.03 + 0.022 * ropes + 0.008 * grain + 0.012 * dome) * tone
    rim = (1.0 - K.smoothstep(width, width * 3.5, edge)) * (1.0 - seam)
    r = lum + rim * 0.07 + seam * 0.32
    g = lum * 0.93 + rim * 0.02 + seam * 0.06
    b = lum * 0.9 + seam * 0.02
    cover = 1.0 - seam
    alb = np.stack([r, g, b, cover], axis=-1)
    alb[..., :3] = np.clip(alb[..., :3], 0, 1) ** (1 / 2.2)

    wide = width / 0.11
    heat_r = np.clip(seam ** 0.8 * (0.35 + 0.65 * wide) + fine * 0.15, 0, 1)
    halo = np.exp(-edge / (width * 2.2)) * 0.8 + (1.0 - dome) * 0.1
    m = K.spectral(TILE, seed + 11, 3, 18, beta=2.2)
    under = K.norm01(K.ridged(K.warp(m, K.spectral(TILE, seed + 12, 2, 8) * 28, K.spectral(TILE, seed + 13, 2, 8) * 28))
                     ** 2.0, 1, 99)
    heat = np.stack([heat_r, np.clip(halo, 0, 1), under], axis=-1)
    return alb, nrm, heat


def lava_bubbles():
    r, ang, dx, dy = K.radial(FRAME)
    skin_noise = K.ridged(K.spectral(FRAME, 261, 6, 30)) ** 4
    rng = np.random.default_rng(262)
    drops = [(rng.random() * 2 * math.pi, 0.6 + 0.8 * rng.random(), 0.02 + 0.03 * rng.random()) for _ in range(14)]
    frames = []
    for f in range(FRAMES):
        emit = np.zeros_like(r)
        shade = np.zeros_like(r)
        hgt = np.zeros_like(r)
        alpha = np.zeros_like(r)
        if f <= 9:
            t = f / 9.0
            R = 0.18 + 0.5 * t ** 0.8
            q = np.clip(r / R, 0, 1)
            inside = r < R
            dome = np.sqrt(np.clip(1 - q * q, 0, 1))
            hgt = dome * R * (0.45 + 0.55 * t)
            # Lambert from the upper left (light dir in sprite space: -x, up = -row).
            nx = dx / R * dome
            ny = dy / R * dome
            nz = np.sqrt(np.clip(1 - q * q, 0.02, 1))
            lam = np.clip((-0.45 * nx - 0.55 * ny + 0.7 * nz), 0, 1)
            shade = np.where(inside, 0.25 + 0.75 * lam, 0)
            # The skin stretches and thins: hotter on top and along fresh cracks as it swells.
            emit = np.where(inside, dome ** 2 * t ** 1.6 * 0.85 + skin_noise * t * 0.7 * dome, 0)
            emit += np.exp(-((r - R) / 0.025) ** 2) * 0.55 * (0.4 + 0.6 * t)
            alpha = K.smoothstep(R + 0.03, R - 0.02, r) + np.exp(-((r - R) / 0.03) ** 2) * 0.6
        else:
            t = (f - 10) / 5.0
            R0 = 0.68
            crater = K.smoothstep(R0 * (0.95 - 0.35 * t), R0 * (0.55 - 0.35 * t), r)
            emit = crater * (1.0 - t) ** 1.5 * (0.75 + 0.25 * skin_noise)
            ring_r = R0 * (1.0 + 0.4 * t)
            ring = np.exp(-((r - ring_r) / (0.04 + 0.05 * t)) ** 2) * (1.0 - t)
            hgt = ring * 0.12 - crater * 0.08 * (1 - t)
            shade = np.clip(0.5 + ring * 0.5, 0, 1) * (ring > 0.02)
            emit += ring * 0.25 * (1 - t)
            alpha = np.clip(crater + ring, 0, 1)
            for a, sp, size in drops:
                d = R0 + sp * (t + 0.15) * 0.45
                if d > 0.95:
                    continue
                cx, cy = math.cos(a) * d, math.sin(a) * d
                dd = np.sqrt((dx - cx) ** 2 + (dy - cy) ** 2)
                blob = K.smoothstep(size, size * 0.4, dd)
                emit = np.maximum(emit, blob * (1.0 - t) ** 0.7)
                shade = np.maximum(shade, blob * 0.6)
                alpha = np.maximum(alpha, blob)
        alpha *= K.smoothstep(1.0, 0.92, r)
        frames.append(np.stack([np.clip(emit, 0, 1), np.clip(shade, 0, 1), np.clip(hgt / 0.7, 0, 1),
                                np.clip(alpha, 0, 1)], axis=-1))
    return K.atlas(frames, COLS)


# ---------------------------------------------------------------------------------------------------------- bog

def bog():
    out = []
    h = K.spectral(TILE, 301, 3, 26, beta=2.8) + 0.18 * K.spectral(TILE, 302, 30, 120, beta=1.6)
    out.append(Tex("Bog_Normal", slope_normal(h, 0.05), kind="normal", wrap="Repeat",
                   note="murky, viscous swell; scroll very slowly"))
    out.append(Tex("Bog_Scum", bog_scum(), kind="mask", wrap="Repeat",
                   note="R film lightness, G glow spots, B wetness/gloss, A alpha (coverage)"))
    out.append(Tex("Bog_Bubble_Flip", film_bubbles(311), kind="mask", wrap="Clamp", flipbook=[COLS, COLS], fps=10,
                   note="R specular highlight, G film shade (0.5 neutral), B height, A alpha; swell 0-8, pop 9, "
                   "rings 10-15"))
    return out


def bog_scum():
    x, y = grid()
    w = 40.0
    cover = K.warp(K.spectral(TILE, 321, 2, 10, beta=2.6), K.spectral(TILE, 322, 3, 12) * w,
                   K.spectral(TILE, 323, 3, 12) * w)
    patch = K.smoothstep(0.3, 1.1, cover)
    f1, f2, pid = K.worley(x + K.spectral(TILE, 324, 8, 30) * 3, y + K.spectral(TILE, 325, 8, 30) * 3, TILE, 96, 326)
    size = 0.28 + 0.14 * pid
    leaf = K.smoothstep(size + 0.06, size - 0.04, f1) * K.smoothstep(0.02, 0.1, f2 - f1)
    present = pid < patch * 1.25 - 0.05
    leaf = leaf * present
    film = K.smoothstep(-0.1, 0.9, cover) * (0.55 + 0.45 * K.norm01(K.spectral(TILE, 327, 40, 200, beta=1.2)))
    alpha = np.clip(np.maximum(leaf, film * 0.62), 0, 1)
    light = np.where(leaf > 0.3, 0.42 + 0.3 * pid + 0.12 * (1 - f1 / size), 0.26 * film + 0.06)
    glow = K.smoothstep(0.13, 0.04, f1) * (pid > 0.72) * present
    wet = np.where(leaf > 0.3, 0.75, 0.35 * film)
    return np.stack([np.clip(light, 0, 1), glow.astype(F32), np.clip(wet, 0, 1), alpha], axis=-1)


def film_bubbles(seed):
    r, ang, dx, dy = K.radial(FRAME)
    frames = []
    for f in range(FRAMES):
        spec = np.zeros_like(r)
        shade = np.full_like(r, 0.5)
        hgt = np.zeros_like(r)
        alpha = np.zeros_like(r)
        if f <= 8:
            t = f / 8.0
            R = 0.15 + 0.42 * t ** 0.7
            q = np.clip(r / R, 0, 1)
            dome = np.sqrt(np.clip(1 - q * q, 0, 1))
            hgt = dome * R
            # A thin film: dark rim (fresnel), bright window top-left, a sharp sun spot.
            rim = K.smoothstep(0.55, 1.0, q)
            shade = np.where(r < R, 0.5 - 0.22 * rim + 0.18 * dome * K.smoothstep(0.3, -0.4, (dx + dy) / R), 0.5)
            hx, hy = -0.35 * R, -0.4 * R
            spec = np.exp(-(((dx - hx) ** 2 + (dy - hy) ** 2) / (0.12 * R) ** 2)) * (r < R)
            alpha = K.smoothstep(R + 0.025, R - 0.015, r)
        elif f == 9:
            R = 0.57
            ring = np.exp(-((r - R) / 0.035) ** 2)
            shade = 0.5 + ring * 0.35
            hgt = ring * 0.4
            rng = np.random.default_rng(seed)
            for _ in range(10):
                a = rng.random() * 2 * math.pi
                d = R * (1.05 + 0.25 * rng.random())
                dd = np.sqrt((dx - math.cos(a) * d) ** 2 + (dy - math.sin(a) * d) ** 2)
                spec = np.maximum(spec, K.smoothstep(0.03, 0.01, dd))
            alpha = np.clip(ring + spec, 0, 1)
        else:
            t = (f - 10) / 5.0
            alpha = np.zeros_like(r)
            for k, lag in enumerate((0.0, 0.22)):
                tt = np.clip(t - lag, 0, 1)
                rr = 0.5 + 0.45 * tt
                wv = np.exp(-((r - rr) / (0.03 + 0.04 * tt)) ** 2) * (1 - tt) * (0.8 - 0.35 * k)
                hgt += wv * 0.3
                shade += wv * 0.3
                alpha = np.maximum(alpha, wv)
        alpha *= K.smoothstep(1.0, 0.9, r)
        frames.append(np.stack([np.clip(spec, 0, 1), np.clip(shade, 0, 1), np.clip(hgt, 0, 1),
                                np.clip(alpha, 0, 1)], axis=-1))
    return K.atlas(frames, COLS)


# ---------------------------------------------------------------------------------------------------- quicksand

def quicksand():
    out = []
    psi = K.spectral(TILE, 401, 1.2, 4.5, beta=3.2)
    w = 26.0
    psi_w = K.warp(psi, K.spectral(TILE, 402, 4, 16) * w, K.spectral(TILE, 403, 4, 16) * w)
    bands = np.sin(K.norm01(psi_w, 0.5, 99.5) * 2 * math.pi * 7.0)
    crest = (0.5 + 0.5 * bands) ** 1.6
    fine = K.spectral(TILE, 404, 150, 520, beta=0.6)
    clump = K.spectral(TILE, 405, 40, 160, beta=1.4)
    low = K.spectral(TILE, 406, 2, 10, beta=2.5)
    height = crest * 0.55 + clump * 0.06 + fine * 0.04 + low * 0.25
    out.append(Tex("Quicksand_Normal", slope_normal(height, 0.2), kind="normal", wrap="Repeat",
                   note="swirled ripple bands + grains"))
    rng = np.random.default_rng(407)
    speck = rng.random((TILE, TILE)).astype(F32)
    lum = 0.36 + 0.2 * crest + 0.04 * clump + 0.03 * fine - 0.05 * (speck > 0.985) + 0.08 * (speck < 0.004)
    trough = 1.0 - crest
    r = lum * 1.04
    g = lum * 1.0
    b = lum * 0.93
    alb = np.stack([r, g, b], axis=-1)
    alb = np.clip(alb, 0, 1) ** (1 / 2.2)
    wet = np.clip(trough * 0.6 * K.smoothstep(-0.6, 0.8, low), 0, 1)
    out.append(Tex("Quicksand_Albedo", np.concatenate([alb, wet[..., None]], axis=-1), kind="albedo", wrap="Repeat",
                   note="RGB sRGB near-neutral sand, A wetness in the troughs (gloss)"))
    sparkle = ((speck < 0.0025) | (speck > 0.9985)).astype(F32)
    out.append(Tex("Quicksand_Flow", curl_flow(psi_w), kind="flow", wrap="Repeat",
                   note="RG flow along the swirl bands (0.5 = still), B speed; flow-map two-phase blend"))
    out.append(Tex("Quicksand_Vortex", vortex_flow(), kind="flow", wrap="Clamp",
                   note="one pool-centred vortex: RG inward spiral, B speed, A sink mask (centre)"))
    out.append(Tex("Quicksand_Ring_Flip", sand_rings(), kind="mask", wrap="Clamp", flipbook=[COLS, COLS], fps=8,
                   note="R,G normal xy (0.5 flat), B height, A alpha; dimple sinks 0-3, rings run out 4-15"))
    out.append(Tex("Quicksand_Sparkle", sparkle, kind="mask", wrap="Repeat",
                   note="grey: rare crystal grains that glint (Crysium dust); multiply by N.H^high"))
    return out


def vortex_flow(n=512):
    r, ang, dx, dy = K.radial(n)
    # Tangential speed peaks at r ~ 0.35 and falls off; a gentle inward pull makes it a spiral towards the sink.
    tang = (r / 0.35) * np.exp(1 - r / 0.35)
    vx = (-dy) * tang / np.maximum(r, 1e-3)
    vy_img = dx * tang / np.maximum(r, 1e-3)
    pull = 0.35 * K.smoothstep(1.0, 0.1, r) * K.smoothstep(0.0, 0.12, r)
    vx += -dx / np.maximum(r, 1e-3) * pull
    vy_img += -dy / np.maximum(r, 1e-3) * pull
    fade = K.smoothstep(1.0, 0.8, r)
    vx *= fade
    vy_img *= fade
    vy = -vy_img
    sp = np.sqrt(vx * vx + vy * vy)
    s = float(sp.max())
    sink = K.smoothstep(0.18, 0.0, r)
    return np.stack([vx / s * 0.5 + 0.5, vy / s * 0.5 + 0.5, sp / s, sink], axis=-1)


def sand_rings():
    r, ang, dx, dy = K.radial(FRAME)
    wob = 1.0 + 0.04 * np.sin(ang * 3 + 1.0) + 0.03 * np.sin(ang * 5 + 2.0)
    rw = r * wob
    frames = []
    for f in range(FRAMES):
        t = f / (FRAMES - 1)
        dimple = -np.exp(-(rw / (0.12 + 0.1 * t)) ** 2) * 0.6 * K.smoothstep(0.0, 0.2, t) * (1 - t) ** 0.5
        h = dimple
        for k, lag in enumerate((0.0, 0.18, 0.36)):
            tt = np.clip((t - lag) / (1.0 - lag), 0, 1)
            if tt <= 0:
                continue
            rr = 0.12 + 0.78 * tt ** 0.8
            h += np.exp(-((rw - rr) / (0.035 + 0.05 * tt)) ** 2) * (1 - tt) ** 1.3 * (0.5 - 0.12 * k)
        env = K.smoothstep(0.98, 0.8, r)
        h *= env
        nrm = K.height_to_normal(h, 9.0)
        alpha = np.clip(np.abs(h) * 3.0, 0, 1) * env
        frames.append(np.stack([nrm[..., 0], nrm[..., 1], np.clip(h * 0.5 + 0.5, 0, 1), alpha], axis=-1))
    return K.atlas(frames, COLS)


# ---------------------------------------------------------------------------------------------------------- ice

def ice():
    out = []
    x, y = grid()
    milk_n = K.spectral(TILE, 501, 1.5, 7, beta=3.0)
    layers = 0.5 + 0.5 * np.sin(K.warp(milk_n, K.spectral(TILE, 502, 3, 10) * 50, K.spectral(TILE, 503, 3, 10) * 50)
                                 * 2.5)
    milky = np.clip(K.smoothstep(-0.6, 1.4, milk_n) * 0.62 + layers * 0.07, 0, 1)

    # Cracks: a partial cell fracture network (only some borders break) plus a few long through-cracks.
    wx = K.spectral(TILE, 504, 2, 12) * 22
    wy = K.spectral(TILE, 505, 2, 12) * 22
    f1, f2, pid = K.worley(x + wx, y + wy, TILE, 6, 506)
    e = f2 - f1
    partial = K.smoothstep(-0.2, 0.5, K.spectral(TILE, 507, 2, 8))
    cell_crack = (1.0 - K.smoothstep(0.004, 0.014, e)) * partial
    g1, g2, _ = K.worley(x + wx * 1.4, y + wy * 1.4, TILE, 15, 508)
    fine = (1.0 - K.smoothstep(0.003, 0.012, g2 - g1)) * K.smoothstep(0.3, 0.8, K.spectral(TILE, 509, 3, 12))
    long_n = K.warp(K.spectral(TILE, 510, 1, 3.5, beta=3), K.spectral(TILE, 511, 4, 20) * 8, K.spectral(TILE, 512, 4, 20) * 8)
    long_c = K.smoothstep(0.985, 0.998, K.ridged(long_n * 2.5)) * K.smoothstep(-0.4, 0.4, K.spectral(TILE, 513, 1, 4))
    crack = np.clip(np.maximum(np.maximum(cell_crack, fine * 0.6), long_c), 0, 1)
    plane = np.clip(np.maximum(np.exp(-e / 0.06) * partial, np.exp(-(1 - K.ridged(long_n * 2.5)) / 0.03) * 0.8)
                    * 0.85, 0, 1)

    rng = np.random.default_rng(514)
    clump = K.smoothstep(0.4, 1.6, K.spectral(TILE, 515, 3, 14))
    pts, radii = [], []
    while len(pts) < 1500:
        p = rng.random(2) * TILE
        if rng.random() < clump[int(p[1]), int(p[0])]:
            # Bubbles in short vertical chains (frozen in as the ice grew down); top-down they overlap as dots.
            for k in range(1 + int(rng.integers(0, 4))):
                pts.append(p + np.array([rng.normal(0, 0.8), k * 3.5 + rng.normal(0, 0.6)]))
                radii.append(1.2 + 3.2 * rng.random() ** 2)

    def bub(rr):
        return np.where(rr < 1.0, 0.55 + 0.45 * np.exp(-((rr - 0.75) / 0.2) ** 2), 0.0)
    bubbles = K.stamp_discs(TILE, pts, radii, bub)

    white = np.clip(milky * 0.75 + plane * 0.35 + bubbles * 0.55 + crack * 0.3, 0, 1)
    r = 0.52 + 0.38 * white
    g = 0.6 + 0.34 * white
    b = 0.66 + 0.3 * white
    alb = np.stack([r, g, b], axis=-1)
    out.append(Tex("Ice_Albedo", np.concatenate([alb, np.clip(milky * 0.8 + plane * 0.3 + bubbles * 0.4, 0, 1)[..., None]],
                                                axis=-1), kind="albedo", wrap="Repeat",
                   note="RGB sRGB pale ice (near neutral cool), A milkiness/opacity (0 clear: show the depth colour)"))
    undul = K.spectral(TILE, 516, 2, 14, beta=2.8) * 0.35 + K.spectral(TILE, 517, 60, 300, beta=1.2) * 0.04
    hgt = undul - crack * 0.5 - fine * 0.1
    out.append(Tex("Ice_Normal", slope_normal(hgt, 0.06), kind="normal", wrap="Repeat",
                   note="gentle undulation + crack grooves"))
    out.append(Tex("Ice_Cracks", np.stack([crack, plane, bubbles], axis=-1), kind="mask", wrap="Repeat",
                   note="R crack lines (glint/emission), G fracture planes (soft white under the surface), B bubbles"))
    out.append(Tex("Ice_Frost", ice_frost(), kind="mask", wrap="Repeat",
                   note="R frost density (threshold against distance to bank), G sparkle points, B frost height"))
    return out


def ice_frost():
    # Feathery dendrites: ridged noise stretched in two crossing directions, so a distance threshold leaves fern-
    # like fingers reaching out from the bank.
    a = K.ridged(K.spectral(TILE, 521, 8, 50, beta=1.6, stretch=(1, 2.5), angle=0.5))
    b = K.ridged(K.spectral(TILE, 522, 8, 50, beta=1.6, stretch=(1, 2.5), angle=2.0))
    base = K.norm01(K.spectral(TILE, 523, 2, 12, beta=2.0), 1, 99)
    dens = np.clip(base * 0.6 + np.maximum(a, b) ** 3 * 0.55 + K.spectral(TILE, 524, 60, 300) * 0.04, 0, 1)
    rng = np.random.default_rng(525)
    spark = (rng.random((TILE, TILE)) < 0.0018).astype(F32)
    spark = np.clip(K.blur_wrap(spark, 0.6) * 6, 0, 1)
    return np.stack([K.norm01(dens, 0.5, 99.5), spark, K.norm01(K.blur_wrap(dens, 1.5), 1, 99)], axis=-1)


# --------------------------------------------------------------------------------------------------- corruption

def corruption():
    out = []
    x, y = grid()
    f1, f2, pid = K.worley(x + K.spectral(TILE, 601, 2, 8) * 30, y + K.spectral(TILE, 602, 2, 8) * 30, TILE, 9, 603)
    blobs = K.smoothstep(0.75, 0.0, f1) ** 1.5
    w = 30.0
    v1n = K.spectral(TILE, 604, 2, 9, beta=2.4)
    vx = K.spectral(TILE, 605, 3, 14) * w
    vy = K.spectral(TILE, 606, 3, 14) * w
    a1, a2, _ = K.worley(x + vx, y + vy, TILE, 5, 607)
    b1, b2, _ = K.worley(x + vx * 0.6, y + vy * 0.6, TILE, 13, 608)
    # Vein networks: thick trunks along big cell borders, thin capillaries on some small borders.
    trunk = (1.0 - K.smoothstep(0.0, 0.05, a2 - a1)) * (0.6 + 0.4 * K.norm01(v1n))
    cap = (1.0 - K.smoothstep(0.0, 0.025, b2 - b1)) * K.smoothstep(0.3, 1.0, K.spectral(TILE, 610, 2, 7))
    v1 = trunk
    veins = np.clip(np.maximum(trunk, cap * 0.6), 0, 1) ** 1.3
    phase = K.norm01(K.spectral(TILE, 611, 1, 3.5, beta=3.0) + 0.15 * v1n, 0.5, 99.5)
    mottle = K.norm01(K.spectral(TILE, 612, 3, 30, beta=1.8), 1, 99)
    h = blobs * 0.6 + K.spectral(TILE, 613, 2, 10, beta=3.0) * 0.25 + veins * 0.18 + K.spectral(TILE, 614, 40, 160) * 0.02
    out.append(Tex("Corruption_Normal", slope_normal(h, 0.07), kind="normal", wrap="Repeat",
                   note="heavy viscous blobs, raised veins"))
    out.append(Tex("Corruption_Veins", np.stack([veins, phase, mottle], axis=-1), kind="mask", wrap="Repeat",
                   note="R vein emission, G pulse phase (0..1), B mottling (darker clots)"))
    return out


# --------------------------------------------------------------------------------------------------------- mist

def mist():
    out = []
    a = K.spectral(TILE, 701, 1.5, 7, beta=3.2, stretch=(1.0, 2.2), angle=0.0)
    a = K.smoothstep(-0.35, 1.9, a) ** 1.6 * (0.8 + 0.2 * K.norm01(K.spectral(TILE, 702, 10, 40, beta=2.0)))
    out.append(Tex("Mist_Layer_A", a, kind="mask", wrap="Repeat",
                   note="soft banks with wide gaps; the lowest, slowest layer"))
    bw = 60.0
    b = K.warp(K.spectral(TILE, 703, 2, 12, beta=2.6, stretch=(1.0, 4.5), angle=0.15),
               K.spectral(TILE, 704, 2, 8) * bw, K.spectral(TILE, 705, 2, 8) * bw * 0.4)
    b = K.smoothstep(0.0, 2.0, b) ** 1.4 * (0.75 + 0.25 * K.norm01(K.spectral(TILE, 706, 10, 50)))
    out.append(Tex("Mist_Layer_B", b, kind="mask", wrap="Repeat", note="long wispy streaks along +x (wind)"))
    c = K.spectral(TILE, 707, 3, 16, beta=2.8, stretch=(1.0, 1.6)) + 0.6 * K.spectral(TILE, 708, 1.5, 5, beta=3)
    out.append(Tex("Mist_Layer_C", K.smoothstep(-0.2, 2.2, c) ** 1.8, kind="mask", wrap="Repeat",
                   note="fine broken haze; the highest, fastest layer"))
    out.append(Tex("Mist_Wisp_Flip", wisp_cards(), kind="mask", wrap="Clamp", flipbook=[COLS, COLS], fps=6,
                   note="R top-lit shade, A density; rolling wisp card, loops after 16 frames"))
    out.append(Tex("Steam_Flip", steam_cards(), kind="mask", wrap="Clamp", flipbook=[COLS, COLS], fps=10,
                   note="R top-lit shade, A density; rising lava steam column, loops"))
    hx = K.spectral(TILE, 721, 6, 30, beta=2.0, stretch=(2.5, 1.0))
    hy = K.spectral(TILE, 722, 6, 30, beta=2.0, stretch=(2.5, 1.0))
    out.append(Tex("Heat_Distortion", np.stack([K.norm01(hx, 0.5, 99.5), K.norm01(hy, 0.5, 99.5),
                                                np.full_like(hx, 1.0)], axis=-1), kind="flow", wrap="Repeat",
                   note="RG screen-space offset (0.5 = none), vertically stretched; scroll upwards"))
    return out


def self_shade(d, k=4.0):
    """Top-lit shade for a card: light enters from the top row and is absorbed by the density above."""
    acc = np.cumsum(K.blur_wrap(d, 7.0), axis=0) / d.shape[0]
    return np.clip(np.exp(-acc * k) * 0.75 + 0.25, 0, 1)


def wisp_cards():
    n = FRAME
    noise = K.spectral3(n, FRAMES, 731, 2.0, 12.0, 2.0, beta=2.2)
    r, ang, dx, dy = K.radial(n)
    env = K.smoothstep(1.0, 0.25, np.sqrt(dx * dx * 0.8 + (dy * 1.5) ** 2))
    frames = []
    for f in range(FRAMES):
        # Rolls one full card width over the loop, so the shift wraps back to frame 0 exactly.
        fr = np.roll(noise[f], int(round(f * n / FRAMES)), axis=1)
        d = K.smoothstep(-1.0, 1.8, fr * 0.8 + env * 1.4 - 0.7) ** 1.3 * env
        frames.append(np.stack([self_shade(d), d], axis=-1))
    return K.atlas(frames, COLS)


def steam_cards():
    n = FRAME
    noise = K.spectral3(n, FRAMES, 741, 2.0, 14.0, 2.0, beta=2.0)
    x, y = K.pixel_grid(n)
    u = (x + 0.5) / n * 2 - 1
    v = 1.0 - (y + 0.5) / n
    width = 0.12 + 0.5 * v
    env = np.exp(-(u / width) ** 2) * K.smoothstep(0.0, 0.12, v) * K.smoothstep(1.0, 0.55, v)
    frames = []
    for f in range(FRAMES):
        # Rises one full card height per loop.
        fr = np.roll(noise[f], -int(round(f * n / FRAMES)), axis=0)
        d = K.smoothstep(-1.0, 1.6, fr * 0.8 + env * 1.2 - 0.5) * env * (1.0 - v * 0.5)
        frames.append(np.stack([self_shade(d, 3.0), np.clip(d, 0, 1)], axis=-1))
    return K.atlas(frames, COLS)


KINDS = {"Water": water, "Lava": lava, "Bog": bog, "Quicksand": quicksand, "Ice": ice, "Corruption": corruption,
         "Mist": mist}
