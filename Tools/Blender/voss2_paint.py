"""Dr. Voss round 19: texel-space painters. Each takes the baked G-buffer samples of one part (positions in the head's
local space, normals, extra params) and returns linear albedo, height (metres, for the normal map), roughness, glow
and the two detail-family weights of the Mask (B weave, A ceramic).

Stylised-believable skin: no pores, but warmth where skin is thin (cheeks, nose, ears, lids), a cool touch under the
eyes, painted brows with stroke direction, a lash line, lips with a vermilion border and soft vertical lines. Hair:
strands along the comb lines, clumps, a darker root zone and a soft sheen band.
"""

import math

import numpy as np

import common as C
import voss2_body as B
import voss2_head as H
from portrait_kit import gauss, sstep


def lin(*rgb):
    return C.srgb_to_linear(np.array(rgb, float))


def mix(a, b, t):
    t = np.clip(np.asarray(t, float), 0, 1)
    if t.ndim == 1:
        t = t[:, None]
    return a + (b - a) * t


def hash1(i):
    return np.modf(np.sin(i * 12.9898 + 78.233) * 43758.5453)[0] % 1.0


def noise1(x, seed=0.0):
    """Smooth 1D value noise in [0, 1]."""
    i = np.floor(x)
    f = x - i
    a = hash1(i + seed * 17.0)
    b = hash1(i + 1 + seed * 17.0)
    u = f * f * (3 - 2 * f)
    return a + (b - a) * u


class Out:
    def __init__(self, n):
        self.albedo = np.zeros((n, 3))
        self.height = np.zeros(n)
        self.rough = np.full(n, 0.6)
        self.emit = np.zeros((n, 3))
        self.fam = np.zeros((n, 2))


# -------------------------------------------------------------------------------------------------------------
# Skin and hair (the head mesh)
# -------------------------------------------------------------------------------------------------------------

SKIN = lin(0.89, 0.7, 0.6)
SKIN_WARM = lin(0.88, 0.56, 0.5)
SKIN_LID = lin(0.78, 0.55, 0.49)
SKIN_COOL = lin(0.76, 0.66, 0.66)
LIP = lin(0.76, 0.44, 0.44)
LIP_DARK = lin(0.56, 0.27, 0.29)
MOUTH = lin(0.2, 0.06, 0.07)
LASH = lin(0.09, 0.05, 0.045)
WATERLINE = lin(0.7, 0.45, 0.42)
BROW = lin(0.22, 0.12, 0.085)
HAIR = lin(0.2, 0.1, 0.072)
HAIR_DEEP = lin(0.08, 0.04, 0.03)
HAIR_LIGHT = lin(0.38, 0.22, 0.15)
NOSTRIL = lin(0.32, 0.15, 0.13)

BROW_PTS = np.array([(0.0085, 0.018), (0.018, 0.0222), (0.03, 0.0258), (0.042, 0.025), (0.053, 0.0195)])
BROW_W = np.array([0.0052, 0.0055, 0.0048, 0.0036, 0.0016])


def brow_mask(p):
    """Painted brows: thick at the inner end, an arch two thirds out, a tapering tail. Returns (mask, stroke angle
    coordinate across the brow) so strokes can run along it."""
    x = np.abs(p[:, 0])
    y = p[:, 1]
    front = sstep(0.02, 0.05, p[:, 2])
    xs, ys = BROW_PTS[:, 0], BROW_PTS[:, 1]
    yc = np.interp(x, xs, ys)
    w = np.interp(x, xs, BROW_W)
    inside = (x > xs[0] - 0.0015) & (x < xs[-1] + 0.001)
    d = np.abs(y - yc) / np.maximum(w * 0.5, 1e-5)
    soft_end = sstep(xs[0] - 0.0015, xs[0] + 0.002, x) * (1 - sstep(xs[-1] - 0.003, xs[-1] + 0.001, x))
    m = (1 - sstep(0.35, 1.25, d)) * soft_end * front * inside
    return m, (y - yc), x


def skin_paint(o, idx, p, n, prm=None):
    x, y, z = p[:, 0], p[:, 1], p[:, 2]
    ax = np.abs(x)
    col = np.tile(SKIN, (len(p), 1))
    # Low-frequency value variation (living skin is never one flat colour).
    col *= (1 + 0.035 * C.value_noise(p, 90.0, 11))[:, None]
    # Warmth: cheeks, nose, ears, chin.
    for s in (1, -1):
        col = mix(col, SKIN_WARM, gauss(p, (s * 0.041, -0.022, 0.062), (0.015, 0.012, 0.03)) * 0.42)
        col = mix(col, SKIN_WARM, gauss(p, (s * 0.07, -0.009, -0.016), (0.012, 0.03, 0.022)) * 0.5)
    col = mix(col, SKIN_WARM, gauss(p, (0, -0.018, 0.093), (0.007, 0.007, 0.012)) * 0.35)
    col = mix(col, SKIN_WARM, gauss(p, (0, -0.071, 0.065), (0.013, 0.009, 0.02)) * 0.12)
    # Eyes: warm-tinted lids up to the crease, a soft crease shadow, a cool touch underneath.
    for s in (1, -1):
        e = H.eye_c(s)
        q = p - e
        r = np.linalg.norm(q, axis=1)
        up = q[:, 1] / np.maximum(r, 1e-9)
        lid_zone = sstep(0.05, 0.35, up) * (1 - sstep(H.LID + 0.004, H.LID + 0.009, r)) * sstep(0.0, 0.01, q[:, 2] + 0.004)
        col = mix(col, SKIN_LID, lid_zone * 0.5)
        crease = np.exp(-((r - (H.LID + 0.0052)) / 0.0018) ** 2) * sstep(0.25, 0.6, up) * sstep(-0.004, 0.004, q[:, 2])
        col = mix(col, SKIN_LID * 0.72, crease * 0.45)
        under = gauss(p, e + np.array((s * 0.002, -0.0145, 0.008)), (0.012, 0.005, 0.012))
        col = mix(col, SKIN_COOL, under * 0.25)
        # Outer corner: a little darker (gives the eye its shape at portrait size).
        corner = gauss(p, e + np.array((s * 0.0152, 0.002, 0.0)), (0.0035, 0.003, 0.006))
        col = mix(col, SKIN_LID * 0.7, corner * 0.35)
    # Brows.
    bm, across, bx = brow_mask(p)
    # Soft painted brows: denser along the lower edge and at the inner end, feathering out at the top and the tail;
    # a fine slanted grain so they read as hair, not as a stroke.
    w = np.interp(bx, BROW_PTS[:, 0], BROW_W)
    lower_edge = 1 - sstep(-0.2, 0.9, across / np.maximum(w * 0.5, 1e-5))
    grain = 0.5 + 0.5 * np.sin(bx * 5200.0 + across * 2600.0 + 2.5 * C.value_noise(p, 600.0, 8))
    density = bm * (0.55 + 0.35 * lower_edge) * (0.8 + 0.2 * grain)
    col = mix(col, BROW * (0.85 + 0.3 * grain)[:, None], density)
    o.height[idx] += 0.00002 * bm * grain
    # Nostrils (the dark underside of the nose wings).
    for s in (1, -1):
        nos = gauss(p, (s * 0.0056, -0.0262, 0.0862), (0.0026, 0.0017, 0.003)) * sstep(-0.2, -0.7, n[:, 1])
        col = mix(col, NOSTRIL, nos * 0.55)
    # Lips.
    lm = H.lip_mask(p)
    lower = (y < H.MOUTH_Y + 0.0009 * np.clip(ax / H.MOUTH_W, 0, 1) ** 2).astype(float)
    lip_col = mix(np.tile(LIP, (len(p), 1)), LIP_DARK, sstep(0.55, 1.0, ax / H.MOUTH_W) * 0.6)
    lip_col = mix(lip_col, LIP * 1.12, lower * gauss(p, (0, -0.05, 0.085), (0.009, 0.003, 0.01)) * 0.6)
    col = mix(col, lip_col, lm * 0.92)
    # Soft vertical lines on the lips (height only), stronger on the lower lip.
    lines = np.abs(np.sin(x * 2200.0 + 3 * C.value_noise(p, 400.0, 4)))
    o.height[idx] -= 0.00003 * lm * (1 - lines) * (0.6 + 0.4 * lower)
    # The inside of the mouth and the lid margins come from the mesh's own region tags (Prm.x: 0 surface, 0..1 lid
    # margin, 1..2 lid tuck, 0..3 lip roll, 3..4 mouth bag), not from the field: texels on coarse curved triangles sit
    # a little inside the surface and would test as "inside".
    tag = prm[:, 0] if prm is not None else np.zeros(len(p))
    near_mouth = (np.abs(y - H.MOUTH_Y) < 0.016) & (ax < H.MOUTH_W + 0.004)
    inner_mouth = near_mouth & (tag > 0.6)
    depth = np.clip((tag - 0.6) / 2.4, 0, 1)
    col[inner_mouth] = mix(np.tile(LIP_DARK * 0.6, (inner_mouth.sum(), 1)), MOUTH * 0.55, depth[inner_mouth])
    for s in (1, -1):
        cm = gauss(p, (s * H.MOUTH_W, H.MOUTH_Y + 0.0009, 0.0765), (0.0014, 0.0011, 0.004))
        col = mix(col, LIP_DARK, cm * 0.3)
    # Lid margins: the dark lash line on the upper lid, a wet pink waterline below.
    for s in (1, -1):
        e = H.eye_c(s)
        q = p - e
        r = np.linalg.norm(q, axis=1)
        near_eye = (r < H.LID + 0.003) & ~near_mouth
        margin = near_eye & (tag > 0.35)
        upper = q[:, 1] > -0.001
        col[margin & upper] = LASH
        col[margin & ~upper] = WATERLINE
        o.rough[idx[margin]] = 0.25
        # Lash line continuing a hair onto the lid skin, thicker at the outer corner.
        rim = np.exp(-((r - H.LID) / 0.0013) ** 2) * (q[:, 1] > 0.0) * sstep(-0.005, 0.0, q[:, 2]) * (tag < 0.35)
        col = mix(col, LASH, rim * 0.9)
    o.albedo[idx] = col
    o.rough[idx] = np.where(o.rough[idx] < 0.3, o.rough[idx], 0.62 - 0.2 * lm - 0.12 * gauss(p, (0, -0.019, 0.093), (0.006, 0.006, 0.01)))
    o.height[idx] += 0.000015 * C.value_noise(p, 1400.0, 5)


def hair_paint(o, idx, p, n, prm=None):
    """Strands along the comb lines (cap) or along the lock (fringe, bun), clumps, deep roots, a sheen band."""
    phi = H.flow_angle(p)
    strand_coord = phi * 70.0
    # Locks near the fringe flow sideways: use the fringe's own direction there.
    fr = np.zeros(len(p))
    for spec, sink in ((H.FRINGE, 0.0016), (H.FRINGE2, 0.0024), (H.SIDE_L, 0.0024)):
        pts = H.lock_path([q for q, r in spec], -sink)
        d, across = _nearest_polyline(p, pts)
        w = 1 - sstep(0.008, 0.013, d)
        strand_coord = strand_coord * (1 - w) + across * 520.0 * w
        fr = np.maximum(fr, w)
    # Bun: strands run round the coil.
    db = np.linalg.norm(p - H.BUN, axis=1)
    wb = 1 - sstep(0.026, 0.034, db)
    e1, e2 = H._basis(H.BUN_AXIS)
    q = p - H.BUN
    ring = np.hypot(q @ e1, q @ e2)
    strand_coord = strand_coord * (1 - wb) + (ring * 420.0 + (q @ H.BUN_AXIS) * 300.0) * wb
    s1 = noise1(strand_coord, 1.0)
    s2 = noise1(strand_coord * 0.37, 2.0)
    clump = noise1(phi * 14.0 / (2 * math.pi) * 2.0, 5.0)
    # Root to tip: dark at the hairline and the parting, lighter where the hair gathers into the bun.
    tip = sstep(0.11, 0.03, np.linalg.norm(p - H.BUN, axis=1))
    col = mix(np.tile(HAIR, (len(p), 1)), HAIR_DEEP, (1 - s2) * 0.22)
    col = mix(col, HAIR_LIGHT, s1 * 0.1 + clump * 0.1 + tip * 0.28)
    # Sheen: a soft lighter band on top-front-facing parts (hair reads glossy without a hair shader).
    up = n[:, 1]
    sheen = sstep(0.25, 0.55, up) * (1 - sstep(0.8, 0.98, up)) * (0.6 + 0.4 * s1)
    col = mix(col, HAIR_LIGHT * 1.2, sheen * 0.22)
    # Roots at the hairline and in the grooves between locks are darker.
    hl = H.hairline_dist(p)
    col = mix(col, HAIR_DEEP, (1 - sstep(0.0, 0.006, hl)) * (1 - fr) * 0.4)
    ridge = 0.5 - 0.5 * np.cos((H.flow_angle(p) + 0.12 * np.sin(p[:, 1] * 60.0 + p[:, 0] * 40.0)) * 14.0)
    col = mix(col, HAIR_DEEP, ridge ** 4 * (1 - fr) * (1 - wb) * 0.55)
    o.albedo[idx] = col
    # Strand ridges in the normal map: sharp crests, so the highlight breaks up along the hair, not across it.
    o.height[idx] += 0.00022 * (np.abs(np.sin(strand_coord * math.pi)) ** 0.5 - 0.6) + 0.00012 * (s2 - 0.5)
    o.rough[idx] = 0.58 - 0.1 * sheen


def _nearest_polyline(p, pts):
    best = np.full(len(p), 1e9)
    across = np.zeros(len(p))
    for a, b in zip(pts[:-1], pts[1:]):
        ab = b - a
        t = np.clip(((p - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
        q = a + t[:, None] * ab
        d = np.linalg.norm(p - q, axis=1)
        side = np.cross(ab / np.linalg.norm(ab), p - q)
        sgn = np.sign(side @ np.array((0.0, 1.0, 0.3)) + 1e-9)
        closer = d < best
        best = np.where(closer, d, best)
        across = np.where(closer, d * sgn, across)
    return best, across


def head_paint(o, idx, p, n, prm=None):
    hair = H.is_hair(p)
    if (~hair).any():
        skin_paint(o, idx[~hair], p[~hair], n[~hair], None if prm is None else prm[~hair])
    if hair.any():
        hair_paint(o, idx[hair], p[hair], n[hair])


# -------------------------------------------------------------------------------------------------------------
# Eyes
# -------------------------------------------------------------------------------------------------------------

SCLERA = lin(0.88, 0.87, 0.85)
IRIS = lin(0.36, 0.55, 0.7)
IRIS_DARK = lin(0.12, 0.22, 0.34)
IRIS_WARM = lin(0.55, 0.5, 0.35)
PUPIL = lin(0.015, 0.015, 0.02)


def lash_paint(o, idx, p, n, prm=None):
    o.albedo[idx] = LASH
    o.rough[idx] = 0.6


def eye_paint(o, idx, p, n, prm=None):
    col = np.zeros((len(p), 3))
    for s in (1, -1):
        m = (p[:, 0] * s) > 0
        e = H.eye_c(s)
        q = p[m] - e
        r = np.linalg.norm(q, axis=1)
        th = np.arccos(np.clip(q[:, 2] / np.maximum(r, 1e-9), -1, 1))
        ph = np.arctan2(q[:, 1], q[:, 0] * s)
        u = th / H.IRIS_ANGLE                     # 0 centre, 1 limbus
        fib = 0.5 + 0.5 * np.sin(ph * 46.0 + 4.0 * noise1(ph * 9.0, 2.0)) * (0.6 + 0.4 * noise1(ph * 31.0, 4.0))
        iris = mix(np.tile(IRIS, (len(q), 1)), IRIS * 1.35, fib * sstep(0.35, 0.9, u) * 0.5)
        iris = mix(iris, IRIS_WARM, (1 - sstep(0.38, 0.6, u)) * 0.45)          # warm collarette round the pupil
        iris = mix(iris, IRIS_DARK, sstep(0.78, 1.0, u))                         # limbal ring
        iris = mix(iris, IRIS_DARK, sstep(0.0, 0.7, q[:, 1] / H.R_EYE) * 0.35)    # the lid's shadow on top
        sclera = mix(np.tile(SCLERA, (len(q), 1)), lin(0.9, 0.78, 0.76), sstep(0.9, 1.6, th) * 0.6)
        c = mix(sclera, iris, 1 - sstep(0.98, 1.06, u))
        c = mix(c, PUPIL, 1 - sstep(0.33, 0.39, u))
        # The upper lid's shadow on the top of the eyeball, and a little at the bottom.
        c = mix(c, c * 0.55, sstep(0.15, 0.6, q[:, 1] / H.R_EYE))
        c = mix(c, c * 0.8, sstep(-0.3, -0.75, q[:, 1] / H.R_EYE))
        d = q / np.maximum(r, 1e-9)[:, None]
        catch = np.exp(-(((d[:, 0] - (-0.26)) ** 2 + (d[:, 1] - 0.3) ** 2) / 0.006))
        c = mix(c, np.array((1.0, 1.0, 1.0)), catch * 0.85)
        o.emit[idx[m]] += catch[:, None] * 0.35
        col[m] = c
    o.albedo[idx] = col
    o.rough[idx] = 0.05


# -------------------------------------------------------------------------------------------------------------
# Clothes and gear (positions converted to game space)
# -------------------------------------------------------------------------------------------------------------

COAT = lin(0.8, 0.81, 0.83)            # a touch below white: the portrait light must not burn it out
COAT_SEAM = lin(0.6, 0.62, 0.66)
COLONY = lin(0.22, 0.38, 0.66)
NAVY = lin(0.12, 0.19, 0.36)
NAVY_DARK = lin(0.07, 0.11, 0.21)
SLATE = lin(0.2, 0.22, 0.27)
BOOT = lin(0.14, 0.13, 0.13)
SOLE = lin(0.06, 0.06, 0.065)
GUNMETAL = lin(0.22, 0.24, 0.28)
CYAN = lin(0.32, 0.93, 1.0)
YELLOW = lin(0.98, 0.76, 0.16)
GOLD = lin(1.0, 0.84, 0.45)
BUTTON = lin(0.84, 0.85, 0.87)
PATCH_C = np.array((0.205, 1.12, 0.0))     # NTI patch on her left upper sleeve, game space


def game(p):
    g = p * H.HEAD_SCALE
    g[:, 1] += H.HEAD_ORIGIN[1]
    return g


def line(d, width):
    return 1 - sstep(0.0, width, np.abs(d))


def stitch(d, along, width=0.0007, period=0.004):
    """A dashed stitch line at distance d (metres), dashes along `along` (metres)."""
    dash = (np.modf(along / period)[0] % 1.0) < 0.6
    return line(d, width) * dash


def hex_mask(u, v, r):
    """Flat-topped hexagon of circumradius r in a 2D frame."""
    a = np.abs(u)
    b = np.abs(v)
    return ((b < r * 0.866) & (a * 0.866 + b * 0.5 < r * 0.866)).astype(float)


def star4(u, v, r):
    """A four-pointed star (the NTI nova) of radius r."""
    a = np.abs(u) / r
    b = np.abs(v) / r
    return ((np.sqrt(a) + np.sqrt(b)) < 1.0).astype(float)


def cloth(o, idx, g, n, col, fold=1.0):
    """Shared cloth finish: soft value break-up, the weave detail family, matte."""
    col = col * (1 + 0.02 * C.value_noise(g, 40.0, 3))[:, None]
    o.albedo[idx] = col
    o.rough[idx] = 0.82
    o.fam[idx, 0] = 1.0


def coat_paint(o, idx, p, n, prm=None):
    g = game(p)
    x, y, z = g[:, 0], g[:, 1], g[:, 2]
    th = np.arctan2(x, z + 0.01)
    col = np.tile(COAT, (len(g), 1))
    inner = prm[:, 2] > 0.5 if prm is not None else np.zeros(len(g), bool)
    col[inner] *= 0.82
    seams = np.zeros(len(g))
    r = 0.15
    for a0 in (math.radians(100), math.radians(-100), math.pi):
        d = np.angle(np.exp(1j * (th - a0))) * r
        seams = np.maximum(seams, stitch(d + 0.002, y) * 0.8)
        seams = np.maximum(seams, line(d, 0.0012) * 0.6)
    # Shoulder seam and the colony-blue yoke band along the top of the shoulders.
    # The stripe runs along the ridge of each shoulder, neck to sleeve, on the top-facing cloth only.
    top = sstep(0.078, 0.092, np.abs(x)) * sstep(0.55, 0.8, n[:, 1]) * (y > 1.17)
    dz = z + 0.013
    band = (1 - sstep(0.009, 0.0105, np.abs(dz))) * top
    col = mix(col, COLONY, band)
    seams = np.maximum(seams, line(np.abs(dz) - 0.0118, 0.0008) * top * 0.7)
    # Front edge stitch, the overlap's shadow below the top button, the hem stitch.
    front = np.abs(th) < 0.5
    seams = np.maximum(seams, stitch(np.abs(th) * 0.15 - 0.012, y) * front * (y < 1.07) * 0.9)
    shadow = line(th * 0.15 + 0.001, 0.0025) * (y < B.BUTTON_TOP - 0.01) * (y > 0.6)
    col = mix(col, COAT * 0.55, shadow * 0.8)
    seams = np.maximum(seams, stitch(y - (B.HEM + 0.018), th * 0.15) * 0.8)
    # Breast pocket on her left chest.
    inside = (x > 0.054) & (x < 0.108) & (y > 1.028) & (y < 1.094)
    edge = np.minimum.reduce([np.abs(x - 0.054), np.abs(x - 0.108), np.abs(y - 1.028), np.abs(y - 1.094)])
    seams = np.maximum(seams, stitch(edge - 0.002, x + y) * inside)
    col = mix(col, COAT_SEAM, seams)
    o.height[idx] -= 0.0003 * seams
    # Folds: long soft drapes on the skirt, a few pull folds from the pockets, buttons' small pulls.
    drape = np.sin(th * 23.0 + 3.0 * C.value_noise(g, 6.0, 2)) * sstep(0.98, 0.6, y)
    o.height[idx] += 0.0007 * drape + 0.00025 * C.value_noise(g, 70.0, 6)
    cloth(o, idx, g, n, col)


def sleeve_paint(o, idx, p, n, prm):
    g = game(p)
    t = prm[:, 0]
    col = np.tile(COAT, (len(g), 1))
    seams = np.maximum(line(t - 0.07, 0.006) * 0.6, stitch(t - 0.09, prm[:, 1] * 0.3, width=0.004) * 0.6)
    seams = np.maximum(seams, line(t - 0.9, 0.005) * 0.7)
    col = mix(col, COAT_SEAM, seams)
    # The cuff is turned back: a band at the end with its own shade.
    col = mix(col, COAT * 0.93, sstep(0.9, 0.91, t))
    # NTI patch on her left sleeve: a navy hexagon with a gold nova and a cyan rim.
    left = g[:, 0] > 0
    q = g - PATCH_C
    u = q[:, 2]
    v = q[:, 1]
    facing = sstep(0.3, 0.6, n[:, 0])
    hx = hex_mask(u, v, 0.019) * facing * left
    rim = (hex_mask(u, v, 0.019) - hex_mask(u, v, 0.016)).clip(0, 1) * facing * left
    col = mix(col, NAVY, hx)
    col = mix(col, CYAN * 0.6, rim)
    st = star4(u, v + 0.0005, 0.011) * facing * left
    col = mix(col, GOLD, st)
    o.emit[idx] += (CYAN[None, :] * rim[:, None] * 0.6) + (GOLD[None, :] * st[:, None] * 0.25)
    o.height[idx] += 0.0003 * hx
    # Folds: compression rings inside the elbow, a few soft ones at the cuff.
    o.height[idx] += 0.0008 * np.sin(t * 90.0 + 2 * np.sin(prm[:, 1] * 6.28)) * sstep(0.38, 0.5, t) * (1 - sstep(0.62, 0.72, t))
    o.height[idx] -= 0.0003 * seams
    cloth(o, idx, g, n, col)


def lapel_paint(o, idx, p, n, prm):
    g = game(p)
    col = np.tile(COAT * 1.02, (len(g), 1))
    under = prm[:, 2] > 0.5
    col[under] = COAT * 0.85
    edge = stitch(prm[:, 0] - 0.86, prm[:, 1] * 0.2, width=0.03) * (~under)
    col = mix(col, COAT_SEAM, edge * 0.8)
    o.height[idx] -= 0.0002 * edge
    cloth(o, idx, g, n, col)


def collar_paint(o, idx, p, n, prm):
    g = game(p)
    col = np.tile(COAT, (len(g), 1))
    inner = prm[:, 2] > 0.5
    col[inner] = COAT * 0.82
    edge = stitch(prm[:, 1] - 0.85, prm[:, 0] * 0.3, width=0.04) * (~inner)
    col = mix(col, COAT_SEAM, edge * 0.8)
    cloth(o, idx, g, n, col)


def pocket_paint(o, idx, p, n, prm):
    g = game(p)
    u, v = prm[:, 0], prm[:, 1]
    col = np.tile(COAT, (len(g), 1))
    e = np.minimum.reduce([u, 1 - u, v * 1.5])
    s = stitch(e - 0.08, g[:, 0] + g[:, 1], width=0.02)
    col = mix(col, COAT_SEAM, s * 0.8)
    # The opening at the top (her hand is in it): a dark lip.
    col = mix(col, COAT * 0.35, sstep(0.86, 0.97, v))
    o.height[idx] -= 0.0002 * s
    cloth(o, idx, g, n, col)


def top_paint(o, idx, p, n, prm):
    g = game(p)
    a = np.arctan2(g[:, 0], g[:, 2] + 0.019)
    col = mix(np.tile(NAVY, (len(g), 1)), NAVY_DARK, sstep(-0.2, -0.8, n[:, 1]) * 0.5)
    rib = 0.5 + 0.5 * np.sin(a * 90.0)
    o.height[idx] += 0.00035 * rib
    col = col * (0.92 + 0.12 * rib)[:, None]
    # The lit seam on the mock collar's rim.
    seam = line(g[:, 1] - 1.332, 0.0025) * (g[:, 1] > 1.3)
    col = mix(col, CYAN * 0.55, seam)
    o.emit[idx] += CYAN[None, :] * seam[:, None] * 0.9
    o.albedo[idx] = col
    o.rough[idx] = 0.7
    o.fam[idx, 0] = 1.0


def trousers_paint(o, idx, p, n, prm):
    g = game(p)
    col = np.tile(SLATE, (len(g), 1))
    a = prm[:, 1] * 2 * math.pi
    crease = line(np.abs(n[:, 2] - 1.0), 0.03) * sstep(0.1, 0.2, prm[:, 0])
    col = mix(col, SLATE * 1.25, crease * 0.5)
    o.height[idx] += 0.0006 * np.sin(prm[:, 0] * 40.0 + a) * sstep(0.75, 0.95, prm[:, 0])
    cloth(o, idx, g, n, col)
    o.rough[idx] = 0.75


def boot_paint(o, idx, p, n, prm):
    g = game(p)
    col = np.tile(BOOT, (len(g), 1))
    col = mix(col, SOLE, 1 - sstep(0.016, 0.022, g[:, 1]))
    col = mix(col, BOOT * 1.35, sstep(0.6, 0.9, n[:, 2]) * (g[:, 1] < 0.07) * 0.5)
    col = col * (1 + 0.05 * C.value_noise(g, 120.0, 9))[:, None]
    o.albedo[idx] = col
    o.rough[idx] = 0.5
    o.height[idx] += 0.0001 * C.value_noise(g, 500.0, 2)


def metal_paint(o, idx, p, n, prm):
    o.albedo[idx] = GUNMETAL * (1 + 0.04 * C.value_noise(p, 300.0, 1))[:, None]
    o.rough[idx] = 0.34
    o.fam[idx, 1] = 1.0


def glow_paint(o, idx, p, n, prm):
    o.albedo[idx] = CYAN * 0.7
    o.emit[idx] = CYAN
    o.rough[idx] = 0.3


def button_paint(o, idx, p, n, prm):
    o.albedo[idx] = BUTTON
    o.rough[idx] = 0.32
    o.fam[idx, 1] = 1.0


def pen_paint(o, idx, p, n, prm):
    g = game(p)
    col = np.where((g[:, 0] < 0.072)[:, None], NAVY[None, :], GUNMETAL[None, :] * 0.8)
    o.albedo[idx] = col
    o.rough[idx] = 0.3


def card_paint(o, idx, p, n, prm):
    u, v, back = prm[:, 0], prm[:, 1], prm[:, 2] > 0.5
    col = np.tile(YELLOW, (len(p), 1))
    # Front: a photo box, a navy NTI hexagon with the gold nova, text lines and a cyan chip.
    photo = (np.abs(u - 0.3) < 0.2) & (np.abs(v - 0.55) < 0.2)
    col[photo] = lin(0.25, 0.3, 0.38)
    face = photo & (((u - 0.3) / 0.11) ** 2 + ((v - 0.6) / 0.09) ** 2 < 1)
    col[face] = lin(0.72, 0.56, 0.48)
    hx = hex_mask((u - 0.74) * 0.042, (v - 0.76) * 0.058, 0.0075) > 0
    col[hx] = NAVY
    st = star4((u - 0.74) * 0.042, (v - 0.76) * 0.058, 0.0048) > 0
    col[st] = GOLD
    lines = (np.abs(np.sin(v * 40.0)) < 0.35) & (v < 0.3) & (v > 0.08) & (np.abs(u - 0.5) < 0.38)
    col[lines] = lin(0.35, 0.26, 0.08)
    col[back] = lin(0.9, 0.7, 0.18)
    o.albedo[idx] = col
    o.emit[idx] += GOLD[None, :] * (st & ~back)[:, None] * 0.25
    o.rough[idx] = 0.35
    o.fam[idx, 1] = 1.0
