"""Dr. Voss round 19: the head (skin and sculpted hair cap as one surface), its eyes, lids and mouth topology.

Local head space (portrait_kit): x across (+x her left), y up, z forward, metres; origin on the eye line at mid-depth
of the skull, placed at game (0, 1.44, 0).
"""

import math

import numpy as np

import portrait_kit as pk
from portrait_kit import sd_ellipsoid as ell, sd_sphere as sph, sd_round_cone as cone, smin, smax, sstep

HEAD_ORIGIN = (0.0, 1.44, 0.0)          # game space (x, up, forward)
RAY_CENTRE = np.array((0.0, -0.004, -0.008))

R_EYE = 0.0145
EYE = np.array((0.0292, 0.0, 0.0592))   # her left eye; the right one mirrors x
LID = R_EYE + 0.0019                    # lid surface radius round the eye centre
LID_MARGIN = R_EYE + 0.00035            # where the lid's inner edge rests on the eyeball

# Eye opening, in eye-local angles (h across, + lateral; v up), degrees.
EYE_HALF = 58.0                          # half the opening's angular width
EYE_SHIFT = 5.0                          # the opening wraps further round the lateral side
EYE_UP = 24.5                            # upper lid height at its peak
EYE_DOWN = 21.5                          # lower lid depth
EYE_TILT = 3.0                           # outer corner higher than the inner (a friendly upward tilt)
EYE_N = 18                               # points along each lid (corners shared): 2 * (EYE_N - 1) per loop
EYE_RINGS = (5.0, 11.0, 19.0, 30.0)      # angular offsets of the loops round the opening

MOUTH_W = 0.0195                         # half width at the corners
MOUTH_Y = -0.0445
MOUTH_N = 15                             # points along each lip line
MOUTH_RINGS = ((0.0018, 0.002, 0.0016), (0.0038, 0.0046, 0.0036), (0.0058, 0.0082, 0.0062), (0.0078, 0.0122, 0.0096))

EL_MIN = math.radians(-78.0)
EL_SPLIT = math.radians(56.0)


def eye_c(s):
    return EYE * np.array((s, 1, 1))


HEAD_SCALE = 1.06                       # the head is drawn a touch large (stylised proportions)


def to_blender(p):
    p = np.asarray(p, float) * HEAD_SCALE
    out = np.empty_like(p)
    out[..., 0] = p[..., 0] + HEAD_ORIGIN[0]
    out[..., 1] = -(p[..., 2] + HEAD_ORIGIN[2])
    out[..., 2] = p[..., 1] + HEAD_ORIGIN[1]
    return out


def from_blender(b):
    b = np.asarray(b, float)
    out = np.empty_like(b)
    out[..., 0] = b[..., 0] - HEAD_ORIGIN[0]
    out[..., 1] = b[..., 2] - HEAD_ORIGIN[1]
    out[..., 2] = -b[..., 1] - HEAD_ORIGIN[2]
    return out / HEAD_SCALE


# -------------------------------------------------------------------------------------------------------------
# Field
# -------------------------------------------------------------------------------------------------------------

def tapered_ell(p, c, r, y0, y1, f1):
    """An ellipsoid whose width narrows from full at height y0 to f1 at y1 (a jaw that tapers to the chin without
    the lumps of separate jaw primitives)."""
    f = 1 - (1 - f1) * sstep(y0, y1, p[:, 1])
    q = (p - np.asarray(c, float))
    q[:, 0] /= f
    return ell(q, (0, 0, 0), r) * np.minimum(f, 1.0)


def skin_sdf(p):
    """Landmarks: crown +0.112, hairline +0.062, brow +0.018, eyes 0, nose base -0.034, mouth -0.048, chin -0.088;
    glabella 8.5 cm in front of the skull centre, nose tip 10.2, cornea 7.4."""
    d = ell(p, (0, 0.022, -0.016), (0.068, 0.089, 0.1))                      # cranium
    d = smin(d, tapered_ell(p, (0, -0.027, 0.009), (0.0545, 0.052, 0.073), -0.006, -0.08, 0.7), 0.026)  # jaw
    d = smin(d, ell(p, (0, -0.068, 0.0635), (0.017, 0.0105, 0.012)), 0.022)  # chin point (rounded, short)
    for s in (1, -1):
        d = smin(d, sph(p, (s * 0.042, -0.052, -0.02), 0.0105), 0.026)    # soft jaw angles
    for s in (1, -1):
        d = smin(d, ell(p, (s * 0.046, -0.006, 0.043), (0.019, 0.011, 0.018)), 0.02)   # cheekbones
        d = smin(d, ell(p, (s * 0.041, -0.014, 0.04), (0.015, 0.012, 0.016)), 0.022)   # cheeks, set high
    for s in (1, -1):
        d = smax(d, -ell(p, (s * 0.029, 0.006, 0.08), (0.016, 0.01, 0.01)), 0.012)    # sockets
    for s in (1, -1):
        d = smin(d, sph(p, eye_c(s), LID), 0.008)                           # lids over the eyeballs
    nose = cone(p, (0, 0.006, 0.077), (0, -0.0155, 0.09), 0.0045, 0.0056)
    nose = smin(nose, sph(p, (0, -0.0188, 0.0902), 0.0079), 0.0045)        # a small rounded tip
    for s in (1, -1):
        nose = smin(nose, sph(p, (s * 0.0092, -0.0243, 0.0815), 0.0052), 0.005)
    nose = smin(nose, ell(p, (0, -0.027, 0.0848), (0.0058, 0.0036, 0.0056)), 0.004)  # columella into the lip
    d = smin(d, nose, 0.008)
    d = smin(d, ell(p, (0, -0.0393, 0.0796), (0.0182, 0.0058, 0.0071)), 0.0038)   # upper lip
    d = smin(d, ell(p, (0, -0.05, 0.078), (0.0155, 0.0066, 0.0077)), 0.0038)      # lower lip
    d = smin(d, cone(p, (0, -0.04, -0.024), (0, -0.3, -0.012), 0.042, 0.049), 0.02)  # neck, slim under the jaw
    return d


CRANIUM_C = np.array((0.0, 0.022, -0.016))
CRANIUM_R = np.array((0.068, 0.089, 0.1))
HAIR_C = np.array((0.0, 0.0, -0.016))        # centre for hairline angles
# Hairline: elevation (degrees, seen from HAIR_C) by azimuth (0 = forehead, 90 = over the ear, 180 = nape).
HAIRLINE = np.array([(0, 34), (20, 34), (35, 31), (50, 26), (62, 17), (72, 5), (78, -6), (84, -2), (92, 10), (105, 12),
                     (120, 6), (140, -8), (160, -22), (180, -30)], float)
BUN = np.array((0.0, 0.042, -0.121))
BUN_AXIS = np.array((0.0, 0.32, -0.95)) / np.linalg.norm((0.0, 0.32, -0.95))
PART_X = 0.024                                 # side parting on her left


def _basis(axis):
    a = np.asarray(axis, float)
    e1 = np.cross(a, (1.0, 0.0, 0.0))
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    return e1, e2


def hairline_dist(p):
    """Positive inside the hair area (above the hairline), roughly in metres."""
    q = p - HAIR_C
    azs = np.degrees(np.arctan2(q[:, 0], q[:, 2]))
    az = np.abs(azs)
    el = np.degrees(np.arctan2(q[:, 1], np.hypot(q[:, 0], q[:, 2])))
    # Under the fringe (her right front) the hair reaches lower, so no skin shows between the lock and the cap.
    line = np.interp(az, HAIRLINE[:, 0], HAIRLINE[:, 1]) - 9.0 * np.exp(-((azs + 32.0) / 20.0) ** 2)
    return np.radians(el - line) * 0.1


def flow_angle(p):
    """Angle round the axis through the bun: hair combed back runs along lines of constant angle."""
    e1, e2 = _basis(BUN - CRANIUM_C)
    v = p - CRANIUM_C
    return np.arctan2(v @ e2, v @ e1)


def cap_point(v, lift):
    """The point on the cranium (plus `lift`) in direction v from its centre."""
    v = np.asarray(v, float)
    v = v / np.linalg.norm(v)
    t = 1.0 / np.linalg.norm(v / CRANIUM_R)
    return CRANIUM_C + v * (t + lift)


def lock_path(points, lift):
    return [cap_point(np.asarray(q) - CRANIUM_C, lift) for q in points]


# The fringe: from the parting across the forehead to her right temple, back over the ear to the bun; a second lock
# under it, and a short one sweeping back from the parting on her left.
FRINGE = [((PART_X, 0.092, 0.04), 0.0045), ((0.008, 0.074, 0.072), 0.0092), ((-0.016, 0.056, 0.082), 0.0108),
          ((-0.04, 0.04, 0.07), 0.0105), ((-0.06, 0.024, 0.04), 0.0095), ((-0.07, 0.012, 0.0), 0.0082),
          ((-0.062, 0.016, -0.05), 0.007), ((-0.035, 0.026, -0.098), 0.006)]
FRINGE2 = [((PART_X - 0.004, 0.096, 0.03), 0.004), ((-0.002, 0.086, 0.06), 0.0075), ((-0.028, 0.072, 0.068), 0.0085),
           ((-0.052, 0.055, 0.048), 0.008), ((-0.068, 0.036, 0.008), 0.0072), ((-0.064, 0.03, -0.048), 0.006)]
SIDE_L = [((PART_X + 0.003, 0.094, 0.035), 0.004), ((0.04, 0.076, 0.056), 0.0075), ((0.058, 0.055, 0.032), 0.0078),
          ((0.068, 0.032, -0.008), 0.0072), ((0.06, 0.026, -0.058), 0.0062), ((0.03, 0.03, -0.1), 0.0055)]


CHUNKS = [
    # Her right temple: a lock from under the fringe back over the ear to the bun.
    ([((-0.05, 0.05, 0.05), 0.004), ((-0.066, 0.036, 0.02), 0.0078), ((-0.074, 0.03, -0.02), 0.0082), ((-0.064, 0.032, -0.065), 0.0072),
      ((-0.035, 0.038, -0.105), 0.0058)], 0.0012),
    # Her left temple.
    ([((0.048, 0.07, 0.045), 0.004), ((0.066, 0.046, 0.02), 0.0075), ((0.073, 0.034, -0.025), 0.0078), ((0.06, 0.036, -0.07), 0.0068),
      ((0.03, 0.04, -0.105), 0.0055)], 0.0012),
    # Two crown locks combed back, lifting the silhouette.
    ([((0.012, 0.1, 0.03), 0.0045), ((0.01, 0.112, -0.01), 0.0085), ((0.006, 0.104, -0.055), 0.0085), ((0.0, 0.075, -0.095), 0.007)], 0.0016),
    ([((-0.03, 0.094, 0.035), 0.004), ((-0.035, 0.104, -0.008), 0.0078), ((-0.03, 0.098, -0.055), 0.008), ((-0.014, 0.07, -0.098), 0.0065)], 0.0016),
    # A loose tip of the fringe falling over her right temple.
    ([((-0.05, 0.046, 0.066), 0.0045), ((-0.062, 0.03, 0.05), 0.0052), ((-0.068, 0.012, 0.036), 0.0035)], -0.0008),
]


def lock_sdf(p, spec, sink):
    pts = lock_path([q for q, r in spec], -sink)
    radii = [r for q, r in spec]
    return pk.sd_polyline(p, pts, radii)


def bun_sdf(p):
    e1, e2 = _basis(BUN_AXIS)
    d = sph(p, BUN + BUN_AXIS * 0.003, 0.024)
    # Two strands wrapped round the core, offset by half a turn: the twist reads from any side.
    for phase, r0 in ((0.0, 0.0125), (math.pi, 0.0105)):
        pts, radii = [], []
        n = 30
        for i in range(n):
            t = i / (n - 1)
            th = phase + 2 * math.pi * 2.1 * t
            rr = 0.026 * (1 - 0.55 * t)
            pts.append(BUN + BUN_AXIS * (-0.008 + 0.02 * t) + (math.cos(th) * e1 + math.sin(th) * e2) * rr)
            radii.append(r0 * (1 - 0.35 * t))
        d = smin(d, pk.sd_polyline(p, pts, radii), 0.0022)
    return d


def hair_sdf(p):
    base = ell(p, CRANIUM_C, CRANIUM_R)
    y = p[:, 1]
    th = (0.0036 + 0.0064 * sstep(-0.01, 0.09, y) + 0.0032 * sstep(-0.02, -0.1, p[:, 2]) * sstep(-0.03, 0.05, y)
          + 0.0024 * sstep(0.04, 0.065, np.abs(p[:, 0])) * sstep(-0.01, 0.03, y))          # fuller at the temples
    cap = base - th
    # Sculpted locks: rounded ridges along the comb lines to the bun, waving a little; shallow at the hairline.
    phi = flow_angle(p) + 0.12 * np.sin(p[:, 1] * 60.0 + p[:, 0] * 40.0)
    ridge = 0.5 - 0.5 * np.cos(phi * 14.0)
    hl = hairline_dist(p)
    # The cap thins to nothing at the hairline: no ledge, so the coarse triangles there cannot show a zigzag step.
    cap = base - th * (0.25 + 0.75 * sstep(0.0, 0.009, hl))
    # Big locks (14 round the bun axis) with a narrow deep parting between them, plus a finer second set.
    fine = 0.5 - 0.5 * np.cos(phi * 42.0 + 1.3)
    cap += (0.0026 * ridge ** 3 + 0.0006 * fine ** 2) * sstep(0.0, 0.01, hl)
    hair = smax(cap, -hl, 0.0016)
    for spec, sink in ((FRINGE, 0.0016), (FRINGE2, 0.0024), (SIDE_L, 0.0024)):
        hair = smin(hair, lock_sdf(p, spec, sink), 0.0055)
    # Chunky locks: they sit proud of the cap (small blend), so the outline breaks at the temples and the crown.
    for spec, sink in CHUNKS:
        hair = smin(hair, lock_sdf(p, spec, sink), 0.0025)
    hair = smin(hair, bun_sdf(p), 0.005)
    return hair


def ear_sdf(p, s):
    """Ears: a flattened shell with a rolled rim and a bowl, flaring out at the back."""
    c = np.array((s * 0.0688, -0.009, -0.016))
    ang = math.radians(22.0)
    q = p - c
    # Ear frame: u outwards (normal), v up, w forwards; the back edge flares away from the head.
    u = np.array((s * math.cos(ang), 0.0, -math.sin(ang)))
    v = np.array((0.0, 1.0, 0.0))
    w = np.cross(u, v) * s
    lq = np.stack([q @ u, q @ v, q @ w], 1)
    shell = ell(lq, (0.0, 0.0, -0.002), (0.0055, 0.0275, 0.0165))
    bowl = ell(lq, (0.0042, -0.004, 0.001), (0.0035, 0.0135, 0.0085))
    d = smax(shell, -bowl, 0.002)
    d = smin(d, ell(lq, (0.0, -0.022, -0.001), (0.005, 0.008, 0.008)), 0.003)   # lobe
    return d


def head_sdf(p):
    skin = skin_sdf(p)
    for s in (1, -1):
        skin = smin(skin, ear_sdf(p, s), 0.004)
    return np.minimum(skin, hair_sdf(p))


def is_hair(p):
    return hair_sdf(p) < head_skin(p) + 1e-5


def head_skin(p):
    skin = skin_sdf(p)
    for s in (1, -1):
        skin = smin(skin, ear_sdf(p, s), 0.004)
    return skin


# -------------------------------------------------------------------------------------------------------------
# Feature loops (3D), projected to the parameter space
# -------------------------------------------------------------------------------------------------------------

def to_param(p):
    q = np.asarray(p, float) - RAY_CENTRE
    az = np.arctan2(q[:, 0], q[:, 2])
    el = np.arctan2(q[:, 1], np.hypot(q[:, 0], q[:, 2]))
    return np.stack([az, el], 1)


def eye_dir(s, h, v):
    """Unit direction from the eye centre for eye-local angles (degrees), mirrored for the right eye."""
    h = np.radians(h)
    v = np.radians(v)
    return np.stack([s * np.sin(h) * np.cos(v), np.sin(v), np.cos(h) * np.cos(v)], -1)


def lid_curves(delta=0.0):
    """(h, v) of the upper and lower lid lines from the inner corner (t=-1) to the outer (t=+1), widened by delta."""
    i = np.arange(EYE_N)
    t = -np.cos(np.pi * i / (EYE_N - 1))
    w = EYE_HALF + delta
    h = t * w + EYE_SHIFT
    vc = 0.5 * EYE_TILT * t - 0.5
    bulge = (1 - t * t)
    up = vc + (EYE_UP * bulge ** 0.75 * (1 - 0.16 * t)) + delta * bulge ** 0.3
    lo = vc - (EYE_DOWN * bulge ** 0.9 * (1 + 0.14 * t)) - delta * 0.9 * bulge ** 0.3
    return h, up, lo


def eye_loop(s, delta=0.0, radius=LID):
    """A closed loop round the eye (upper lid medial->lateral, then lower lid lateral->medial, corners once)."""
    h, up, lo = lid_curves(delta)
    upper = eye_dir(s, h, up)
    lower = eye_dir(s, h[::-1], lo[::-1])[1:-1]
    d = np.vstack([upper, lower])
    return eye_c(s) + d * radius


def mouth_curves(dx=0.0, du=0.0, dl=0.0):
    i = np.arange(MOUTH_N)
    t = -np.cos(np.pi * i / (MOUTH_N - 1))
    x = t * (MOUTH_W + dx)
    yc = MOUTH_Y + 0.0009 * t * t
    gap = 0.00045 * (1 - t * t)
    b = (1 - t * t) ** 0.5
    up = yc + gap + du * (0.35 + 0.65 * b)
    lo = yc - gap - dl * (0.35 + 0.65 * b)
    if dx == 0.0:
        up = yc + gap
        lo = yc - gap
    z = 0.086 - 26.0 * x * x
    return x, up, lo, z


def mouth_loop(ring=None):
    dx, du, dl = (0.0, 0.0, 0.0) if ring is None else (ring[2], ring[0], ring[1])
    x, up, lo, z = mouth_curves(dx, du, dl)
    upper = np.stack([x, up, z], 1)
    lower = np.stack([x[::-1], lo[::-1], z[::-1]], 1)[1:-1]
    return np.vstack([upper, lower])


# -------------------------------------------------------------------------------------------------------------
# Size field (target edge length in metres)
# -------------------------------------------------------------------------------------------------------------

def spacing_3d(p):
    h = np.full(len(p), 0.0084)
    front = sstep(-0.01, 0.05, p[:, 2]) * sstep(-0.11, -0.07, p[:, 1]) * (1 - sstep(0.05, 0.075, p[:, 1]))
    h = np.minimum(h, 0.0084 - 0.0046 * front)
    for s in (1, -1):
        d = np.linalg.norm(p - eye_c(s), axis=1)
        h = np.minimum(h, 0.0016 + 0.28 * np.maximum(d - 0.016, 0))
    dm = np.linalg.norm((p - np.array((0, MOUTH_Y, 0.084))) * np.array((0.8, 1.2, 1.0)), axis=1)
    h = np.minimum(h, 0.0017 + 0.3 * np.maximum(dm - 0.014, 0))
    for s in (1, -1):
        de = np.linalg.norm(p - np.array((s * 0.072, -0.009, -0.016)), axis=1)
        h = np.minimum(h, 0.0026 + 0.3 * np.maximum(de - 0.03, 0))
    db = np.linalg.norm(p - BUN, axis=1)
    h = np.minimum(h, 0.0034 + 0.3 * np.maximum(db - 0.03, 0))
    dc = np.linalg.norm(p - np.array((0, -0.062, 0.07)), axis=1)
    h = np.minimum(h, 0.0024 + 0.3 * np.maximum(dc - 0.016, 0))      # chin and jaw corner: no shading facets
    dn = np.linalg.norm(p - np.array((0, -0.02, 0.09)), axis=1)
    h = np.minimum(h, 0.0022 + 0.25 * np.maximum(dn - 0.012, 0))
    return h


class RadiusLookup:
    """Coarse traced radius r(az, el) for the parameter metric."""

    def __init__(self, sdf, n_az=120, n_el=90):
        self.az = np.linspace(-math.pi, math.pi, n_az)
        self.el = np.linspace(-math.pi / 2 + 0.01, math.pi / 2 - 0.01, n_el)
        A, E = np.meshgrid(self.az, self.el)
        dirs = pk.directions(A.ravel(), E.ravel())
        self.r = pk.trace_outermost(sdf, RAY_CENTRE, dirs, 0.35).reshape(n_el, n_az)
        P = (RAY_CENTRE + dirs * self.r.ravel()[:, None]).reshape(n_el, n_az, 3)
        # True metres per radian along each parameter axis (rays that graze the neck stretch a lot).
        self.ma = np.linalg.norm(np.gradient(P, self.az, axis=1), axis=2)
        self.me = np.linalg.norm(np.gradient(P, self.el, axis=0), axis=2)
        # Elevation remap S(el): stretched where the surface runs away from the rays (neck, crown) so the 2D layout is
        # about isotropic per row; CDT and relax then make well-shaped triangles in 3D.
        front = np.abs(self.az) < 2.6
        k = np.median(np.where(front[None, :], self.me / np.maximum(self.ma, 1e-4), np.nan)[:, front], axis=1)
        self.k = np.clip(k, 0.3, 30.0)
        self.S = np.concatenate([[0], np.cumsum(0.5 * (self.k[1:] + self.k[:-1]) * np.diff(self.el))])
        self.S -= np.interp(0.0, self.el, self.S)

    def el_to_s(self, el):
        return np.interp(el, self.el, self.S)

    def s_to_el(self, sv):
        return np.interp(sv, self.S, self.el)

    def _bilinear(self, grid, az, el):
        fa = np.interp(az, self.az, np.arange(len(self.az)))
        fe = np.interp(el, self.el, np.arange(len(self.el)))
        i0 = np.clip(np.floor(fa).astype(int), 0, len(self.az) - 2)
        j0 = np.clip(np.floor(fe).astype(int), 0, len(self.el) - 2)
        u = fa - i0
        v = fe - j0
        g = grid
        return ((g[j0, i0] * (1 - u) + g[j0, i0 + 1] * u) * (1 - v) + (g[j0 + 1, i0] * (1 - u) + g[j0 + 1, i0 + 1] * u) * v)

    def metric(self, q):
        return np.maximum(self._bilinear(self.ma, q[:, 0], q[:, 1]), 1e-4), np.maximum(self._bilinear(self.me, q[:, 0], q[:, 1]), 1e-4)

    def __call__(self, az, el):
        fa = np.interp(az, self.az, np.arange(len(self.az)))
        fe = np.interp(el, self.el, np.arange(len(self.el)))
        i0 = np.clip(np.floor(fa).astype(int), 0, len(self.az) - 2)
        j0 = np.clip(np.floor(fe).astype(int), 0, len(self.el) - 2)
        u = fa - i0
        v = fe - j0
        r = self.r
        return ((r[j0, i0] * (1 - u) + r[j0, i0 + 1] * u) * (1 - v) + (r[j0 + 1, i0] * (1 - u) + r[j0 + 1, i0 + 1] * u) * v)

    def point(self, az, el):
        return RAY_CENTRE + pk.directions(az, el) * self(az, el)[:, None]


# -------------------------------------------------------------------------------------------------------------
# Topology
# -------------------------------------------------------------------------------------------------------------

def resample_loop(pts2d, metric_fn, spacing_fn, closed=True):
    """Points along a 2D polyline at the local spacing (metric-aware)."""
    pts = np.asarray(pts2d, float)
    if closed:
        pts = np.vstack([pts, pts[:1]])
    seg = pts[1:] - pts[:-1]
    mid = (pts[1:] + pts[:-1]) * 0.5
    mx, my = metric_fn(mid)
    length = np.hypot(seg[:, 0] * mx, seg[:, 1] * my)
    sp = spacing_fn(mid)
    steps = length / sp
    cum = np.concatenate([[0], np.cumsum(steps)])
    n = max(3, int(round(cum[-1])))
    targets = np.linspace(0, cum[-1], n + (0 if closed else 1), endpoint=not closed)
    out = np.stack([np.interp(targets, cum, pts[:, k]) for k in range(2)], 1)
    return out


class HeadMesh:
    """Result of build_head: Blender-space verts, faces, per-vertex tags and loops for rigging/texturing."""

    def __init__(self):
        self.p = None            # local positions (n, 3)
        self.faces = []
        self.tag = None          # 0 surface, 1 lid margin, 2 lid tuck, 3 mouth roll, 4 mouth bag
        self.eye_rings = {}      # side -> [ring0 idx, ring1 idx, ...]
        self.mouth_rings = []
        self.seam = set()        # vertex ids on the back seam (az = +-pi)
        self.eye_margin = {}     # side -> (margin idx, tuck idx)
        self.mouth_inner = []    # loops behind the slit


def build_head():
    """The head surface as one mesh: domain A (azimuth x remapped elevation, from the neck to 56 degrees up), domain B
    (the crown, an azimuthal projection round the top), welded along the back seam and the crown seam; then the eye
    openings get their lid margins and tucks, and the mouth slit its rolled lips and mouth bag."""
    look = RadiusLookup(head_sdf)

    def q_of(pe):            # (az, el) -> layout space (az, S)
        pe = np.asarray(pe, float)
        return np.stack([pe[:, 0], look.el_to_s(pe[:, 1])], 1)

    def el_of(q):
        return q[:, 0], look.s_to_el(q[:, 1])

    def metric_q(q):
        az, el = el_of(q)
        ma, me = look.metric(np.stack([az, el], 1))
        k = np.interp(el, look.el, look.k)
        return ma, me / k

    def spacing_q(q):
        az, el = el_of(q)
        return spacing_3d(look.point(az, el))

    pm = pk.ParamMesh()
    eye_rings = {}
    for s, side in ((1, "L"), (-1, "R")):
        rings = []
        for k, delta in enumerate((0.0,) + EYE_RINGS):
            loop = q_of(to_param(eye_loop(s, delta)))
            rings.append(pm.add_loop(loop))
            if k == 0:
                pm.add_hole(loop)
        eye_rings[side] = rings
    mouth_rings = []
    for k, ring in enumerate((None,) + MOUTH_RINGS):
        loop = q_of(to_param(mouth_loop(ring)))
        mouth_rings.append(pm.add_loop(loop))
        if k == 0:
            pm.add_hole(loop)
    s_min, s_split = look.el_to_s(EL_MIN), look.el_to_s(EL_SPLIT)
    seam_s = resample_open(np.array([[math.pi, s_min], [math.pi, s_split]]), metric_q, spacing_q)[:, 1]
    top_a = resample_open(np.array([[-math.pi, s_split], [math.pi, s_split]]), metric_q, spacing_q)[:, 0]
    bot_a = resample_open(np.array([[-math.pi, s_min], [math.pi, s_min]]), metric_q, spacing_q)[:, 0]
    left = [(-math.pi, v) for v in seam_s]
    topb = [(a, s_split) for a in top_a[1:-1]]
    right = [(math.pi, v) for v in seam_s[::-1]]
    botb = [(a, s_min) for a in bot_a[::-1][1:-1]]
    border_ids = pm.add_loop(np.array(left + topb + right + botb))
    n_seam = len(seam_s)
    n_top = len(topb)
    left_ids = border_ids[:n_seam]
    right_ids = border_ids[n_seam + n_top:n_seam + n_top + n_seam][::-1]
    top_ids = [left_ids[-1]] + border_ids[n_seam:n_seam + n_top] + [right_ids[-1]]
    bottom_ids = [left_ids[0]] + border_ids[n_seam + n_top + n_seam:][::-1] + [right_ids[0]]
    fixed = np.asarray(pm.fixed)
    outers = [fixed[rings[-1]] for rings in list(eye_rings.values()) + [mouth_rings]]

    def keep_a(q):
        ok = (q[:, 0] > -math.pi + 0.01) & (q[:, 0] < math.pi - 0.01) & (q[:, 1] > s_min + 0.004) & (q[:, 1] < s_split - 0.004)
        for outer in outers:
            ok &= ~pk.point_in_polygon(q, outer)
        return ok
    free = mirror_points(pk.dart_points(((-math.pi, math.pi), (s_min, s_split)), spacing_q, metric_q, keep_a, seed=3, fixed=fixed))
    pts_a, tris_a, nf_a = pk.triangulate(pm, free, metric_q, relax=4)
    az_a, el_a = el_of(pts_a)

    # Domain B: the crown. Its rim is A's top border (same azimuths), so the two weld exactly.
    rim_az = np.array([pts_a[i, 0] for i in top_ids[:-1]])
    rho0 = math.pi / 2 - EL_SPLIT
    rim = np.stack([rho0 * np.sin(rim_az), rho0 * np.cos(rim_az)], 1)
    pmb = pk.ParamMesh()
    rim_ids_b = pmb.add_loop(rim)

    def b_to_azel(q):
        rho = np.hypot(q[:, 0], q[:, 1])
        return np.arctan2(q[:, 0], q[:, 1]), math.pi / 2 - rho

    def metric_b(q):
        az, el = b_to_azel(q)
        r = look(az, el)
        return r, r

    def spacing_b(q):
        az, el = b_to_azel(q)
        return spacing_3d(look.point(az, el))

    def keep_b(q):
        return np.hypot(q[:, 0], q[:, 1]) < rho0 - 0.01
    free_b = pk.dart_points(((-rho0, rho0), (-rho0, rho0)), spacing_b, metric_b, keep_b, seed=5,
                            fixed=np.asarray(pmb.fixed), cells=(60, 60))
    free_b = mirror_points(free_b)
    pts_b, tris_b, nf_b = pk.triangulate(pmb, free_b, metric_b, relax=4)
    az_b, el_b = b_to_azel(pts_b)

    # One vertex list: A (its right seam column welded onto the left one), then B's inner points (its rim maps onto
    # A's top border).
    remap_a = np.arange(len(pts_a))
    for l, r in zip(left_ids, right_ids):
        remap_a[r] = l
    keep_mask = np.ones(len(pts_a), bool)
    keep_mask[right_ids] = False
    order = np.nonzero(keep_mask)[0]
    new_index = -np.ones(len(pts_a), int)
    new_index[order] = np.arange(len(order))
    final_a = new_index[remap_a]
    az = list(az_a[order])
    el = list(el_a[order])
    tris = [final_a[t] for t in tris_a]
    b_index = np.zeros(len(pts_b), int)
    for j, i in enumerate(rim_ids_b):
        b_index[i] = final_a[top_ids[j]]
    base = len(az)
    for k, i in enumerate(range(nf_b, len(pts_b))):
        b_index[i] = base + k
        az.append(az_b[i])
        el.append(el_b[i])
    tris += [b_index[t][::-1] for t in tris_b]     # the azimuthal projection mirrors the winding
    az = np.array(az)
    el = np.array(el)
    tris = np.array(tris, dtype=np.int64)
    tris = tris[(tris[:, 0] != tris[:, 1]) & (tris[:, 1] != tris[:, 2]) & (tris[:, 2] != tris[:, 0])]
    fixed_mask = np.zeros(len(az), bool)
    fixed_mask[final_a[np.arange(nf_a)]] = True

    dirs = pk.directions(az, el)
    t = pk.trace_outermost(head_sdf, RAY_CENTRE, dirs, 0.35)
    p = RAY_CENTRE + dirs * t[:, None]
    # Relax on the field so triangles stretched over undercuts (under the nose) even out; loops stay put.
    p = pk.relax_project(head_sdf, p, tris, iters=6, fixed=fixed_mask)

    hm = HeadMesh()
    hm.eye_rings = {side: [[int(final_a[i]) for i in r] for r in rings] for side, rings in eye_rings.items()}
    hm.mouth_rings = [[int(final_a[i]) for i in r] for r in mouth_rings]
    hm.seam = set(int(final_a[i]) for i in left_ids)
    hm.bottom = [int(final_a[i]) for i in bottom_ids]
    tags = [0] * len(p)
    verts = list(p)
    faces = [tuple(int(i) for i in f) for f in tris]
    # Eye openings: rim -> margin resting on the eyeball -> tuck behind it.
    for side, s in (("L", 1), ("R", -1)):
        rim_ids = hm.eye_rings[side][0]
        c = eye_c(s)
        margin, tuck = [], []
        for i in rim_ids:
            d = verts[i] - c
            d = d / np.linalg.norm(d)
            margin.append(len(verts))
            verts.append(c + d * LID_MARGIN)
            tags.append(1)
        for i in rim_ids:
            d = verts[i] - c
            d = d / np.linalg.norm(d)
            d2 = d - np.array((0, 0, 0.45))
            d2 /= np.linalg.norm(d2)
            tuck.append(len(verts))
            verts.append(c + d2 * (R_EYE - 0.0012))
            tags.append(2)
        faces += ring_strip(rim_ids, margin) + ring_strip(margin, tuck)
        hm.eye_margin[side] = (margin, tuck)
    # Mouth: the slit's rim rolls into the lips (two rows) and closes in a bag behind the teeth.
    rim_ids = hm.mouth_rings[0]
    rows = [rim_ids]
    for depth, spread, shrink in ((0.0022, 0.0011, 0.97), (0.0065, 0.0042, 0.9), (0.0125, 0.0068, 0.78)):
        row = []
        for i in rim_ids:
            q = verts[i]
            up = 1.0 if _is_upper(q) else -1.0
            corner = min(1.0, abs(q[0]) / MOUTH_W)
            v = np.array((q[0] * shrink, q[1] + up * spread * (1 - corner ** 3), q[2] - depth))
            row.append(len(verts))
            verts.append(v)
            tags.append(3 if depth < 0.01 else 4)
        faces += ring_strip(rows[-1], row)
        rows.append(row)
    back = len(verts)
    verts.append(np.array((0.0, MOUTH_Y - 0.001, 0.058)))
    tags.append(4)
    last = rows[-1]
    for a, b in zip(last, last[1:] + last[:1]):
        faces.append((b, a, back))
    hm.mouth_inner = rows[1:]
    hm.p = np.array(verts)
    hm.faces = faces
    hm.tag = np.array(tags)
    hm.fixed = np.concatenate([fixed_mask, np.ones(len(verts) - len(fixed_mask), bool)])
    return hm


def mirror_points(pts, eps=0.003):
    """Keeps the +x half of a scatter and mirrors it (points on the centre line once): a symmetric face layout."""
    half = pts[pts[:, 0] > eps]
    mid = pts[np.abs(pts[:, 0]) <= eps].copy()
    mid[:, 0] = 0.0
    return np.vstack([half, half * np.array((-1.0, 1.0)), mid])


def _is_upper(q):
    t = np.clip(q[0] / MOUTH_W, -1, 1)
    return q[1] >= MOUTH_Y + 0.0009 * t * t


def ring_strip(a, b, flip=False):
    """Quads between two closed loops with the same vertex count."""
    out = []
    n = len(a)
    for i in range(n):
        j = (i + 1) % n
        f = (a[i], a[j], b[j], b[i])
        out.append(f[::-1] if flip else f)
    return out


def resample_open(seg, metric_fn, spacing_fn):
    return resample_loop(seg, metric_fn, spacing_fn, closed=False)


# -------------------------------------------------------------------------------------------------------------
# Eyeballs
# -------------------------------------------------------------------------------------------------------------

IRIS_ANGLE = 0.66           # angular radius of the iris (radians from the eye's forward axis)
CORNEA = 0.0007             # how far the cornea bulges in front of the sphere


def eyeball(s, segments=24, rings=14):
    """A UV sphere with its pole on the gaze (forward) and a cornea bulge over the iris: the highlight then slides
    across the eye as it should. Returns local verts, quad/tri faces and per-vertex angle from the gaze axis."""
    c = eye_c(s)
    verts = []
    ang = []
    # Rings concentrated at the front (iris, pupil edge) where the detail is.
    thetas = np.concatenate([np.linspace(0.0, IRIS_ANGLE * 1.15, rings // 2 + 1)[1:],
                             np.linspace(IRIS_ANGLE * 1.15, math.pi * 0.92, rings - rings // 2 + 1)[1:]])
    verts.append(c + np.array((0, 0, R_EYE + CORNEA)))
    ang.append(0.0)
    for th in thetas:
        bulge = CORNEA * max(0.0, 1 - (th / (IRIS_ANGLE * 1.08)) ** 2) ** 1.5
        for j in range(segments):
            ph = 2 * math.pi * j / segments
            d = np.array((math.sin(th) * math.cos(ph), math.sin(th) * math.sin(ph), math.cos(th)))
            verts.append(c + d * (R_EYE + bulge))
            ang.append(th)
    faces = []
    for j in range(segments):
        faces.append((0, 1 + j, 1 + (j + 1) % segments))
    for r in range(len(thetas) - 1):
        a0 = 1 + r * segments
        a1 = a0 + segments
        for j in range(segments):
            k = (j + 1) % segments
            faces.append((a0 + j, a1 + j, a1 + k, a0 + k))
    return np.array(verts), faces, np.array(ang)


def lip_mask(p):
    """1 on the red of the lips, fading over a millimetre at the vermilion border."""
    x = np.abs(p[:, 0])
    t = np.clip(x / (MOUTH_W * 1.02), 0, 1)
    yc = MOUTH_Y + 0.0009 * t * t
    up = yc + 0.0062 * (1 - t ** 2.2) ** 0.6 + 0.0011 * np.exp(-((x - 0.0045) / 0.003) ** 2)   # cupid's bow peaks
    up -= 0.0008 * np.exp(-(x / 0.0022) ** 2)                                                  # the bow's dip
    lo = yc - 0.0078 * (1 - t ** 2.0) ** 0.7
    inside = (p[:, 1] < up) & (p[:, 1] > lo) & (x < MOUTH_W * 1.02) & (p[:, 2] > 0.06)
    soft = np.minimum(np.clip((up - p[:, 1]) / 0.0008, 0, 1), np.clip((p[:, 1] - lo) / 0.0009, 0, 1))
    return np.where(inside, soft, 0.0) * np.clip((MOUTH_W * 1.02 - x) / 0.0015, 0, 1)
