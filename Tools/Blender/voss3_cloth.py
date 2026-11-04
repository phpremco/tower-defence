"""Dr. Voss round 20: the clothes and gear, fitted to the MPFB body. Game space (x across, +x her left; y up; z forward),
metres, feet on y = 0. Same design as round 19 (voss2_body / voss2_gear): a knee-length lab coat (A-line, buttoned to
the chest, notched lapels, a rolled stand collar, set-in sleeves, hip patch pockets she keeps her hands in, a breast
pocket with pens), the navy ribbed mock-neck top in the V, slim slate trousers, MPFB shoes04 as ankle boots, the
headset with its cyan earpiece and the yellow NTI badge.

The difference: every piece is measured off her body. `Field` takes horizontal slabs of the body's vertices, builds
the 2D convex hull of each slab (mirrored for symmetry) and stores the ray distance from a centre line per angle, so
the coat follows her shoulders, bust, waist and hips; sleeves and trousers are swept along her posed limbs with radii
from the support function of the limb's vertices. Lofts, sweeps and UVs (metres) come from voss2_body.
"""

import math

import numpy as np

import portrait_kit as pk
import voss2_body as B

TWO_PI = 2 * math.pi


# -------------------------------------------------------------------------------------------------------------
# Measuring
# -------------------------------------------------------------------------------------------------------------

def hull2d(P):
    """Convex hull (monotone chain), counter-clockwise."""
    P = np.unique(np.round(P, 6), axis=0)
    if len(P) < 3:
        return P
    P = P[np.lexsort((P[:, 1], P[:, 0]))]

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in P:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in P[::-1]:
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return np.array(lower[:-1] + upper[:-1])


def ray_hull(c, dirs, poly):
    """Distance from c along each direction to the polygon's boundary (c inside)."""
    a = poly
    b = np.roll(poly, -1, axis=0)
    e = b - a
    out = np.zeros(len(dirs))
    for k, d in enumerate(dirs):
        den = d[0] * e[:, 1] - d[1] * e[:, 0]
        ok = np.abs(den) > 1e-12
        w = a - c
        t = np.where(ok, (w[:, 0] * e[:, 1] - w[:, 1] * e[:, 0]) / np.where(ok, den, 1), -1)
        s = np.where(ok, (w[:, 0] * d[1] - w[:, 1] * d[0]) / np.where(ok, den, 1), -1)
        hit = ok & (t > 0) & (s >= -1e-6) & (s <= 1 + 1e-6)
        out[k] = t[hit].max() if hit.any() else 0.0
    return out


def smooth_rows(R, sigma_y, sigma_t):
    """Gaussian smoothing of R (rows = heights, columns = angles, periodic)."""
    def kernel(s):
        n = max(1, int(3 * s))
        x = np.arange(-n, n + 1)
        k = np.exp(-0.5 * (x / max(s, 1e-6)) ** 2)
        return x, k / k.sum()
    if sigma_t > 0:
        x, k = kernel(sigma_t)
        R = sum(np.roll(R, -int(i), axis=1) * w for i, w in zip(x, k))
    if sigma_y > 0:
        x, k = kernel(sigma_y)
        pad = np.pad(R, ((len(x), len(x)), (0, 0)), mode="edge")
        R = sum(pad[len(x) + i:len(x) + i + len(R)] * w for i, w in zip(x, k))
    return R


class Field:
    """Cylindrical radius field r(theta, y) of a point cloud: theta 0 = front (+z), + towards her left (+x)."""

    def __init__(self, pts, y0, y1, dy=0.008, slab=0.01, n=96, sigma_y=1.2, sigma_t=1.0):
        self.ys = np.arange(y0, y1 + 1e-9, dy)
        self.th = np.arange(n) / n * TWO_PI
        dirs = np.stack([np.sin(self.th), np.cos(self.th)], 1)
        R = np.zeros((len(self.ys), n))
        CZ = np.zeros(len(self.ys))
        for i, y in enumerate(self.ys):
            S = pts[np.abs(pts[:, 1] - y) < slab][:, [0, 2]]
            if len(S) < 4:
                R[i] = R[i - 1] if i else 0.0
                CZ[i] = CZ[i - 1] if i else 0.0
                continue
            S = np.vstack([S, S * np.array((-1, 1))])
            poly = hull2d(S)
            cz = 0.5 * (S[:, 1].min() + S[:, 1].max())
            CZ[i] = cz
            R[i] = ray_hull(np.array((0.0, cz)), dirs, poly)
        self.R = smooth_rows(R, sigma_y, sigma_t)
        self.CZ = smooth_rows(CZ[:, None], sigma_y * 2, 0)[:, 0]

    def _interp(self, theta, y, arr2d):
        fi = np.clip((y - self.ys[0]) / (self.ys[1] - self.ys[0]), 0, len(self.ys) - 1.0001)
        i = int(fi)
        u = fi - i
        n = len(self.th)
        ft = (theta % TWO_PI) / TWO_PI * n
        k = int(ft) % n
        v = ft - int(ft)
        k2 = (k + 1) % n
        a = arr2d[i, k] * (1 - v) + arr2d[i, k2] * v
        b = arr2d[i + 1, k] * (1 - v) + arr2d[i + 1, k2] * v
        return a * (1 - u) + b * u

    def r(self, theta, y):
        return self._interp(theta, y, self.R)

    def cz(self, y):
        return float(np.interp(y, self.ys, self.CZ))

    def point(self, theta, y, off=0.0):
        r = self.r(theta, y) + off
        return np.array((r * math.sin(theta), y, self.cz(y) + r * math.cos(theta)))

    def row(self, y):
        return int(np.clip(round((y - self.ys[0]) / (self.ys[1] - self.ys[0])), 0, len(self.ys) - 1))


def support_sweep(path, cloud, sides, ease, slab=0.014, rmin=0.02, smooth=(1.5, 1.0), extra=None, angle0=0.0, rmax=1.0):
    """A tube along `path` (game space) whose radius per ring and angle is the support distance of `cloud` (points
    near that ring's plane) plus `ease`; `extra(t, a)` adds shaping. Returns a voss2_body.Piece (params t, a, 0)."""
    pts = np.asarray(path, float)
    t_, n_, b_ = B.frames(pts)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    T = np.concatenate([[0], np.cumsum(seg)]) / max(seg.sum(), 1e-9)
    angles = angle0 + TWO_PI * np.arange(sides) / sides
    R = np.zeros((len(pts), sides))
    for i, p in enumerate(pts):
        q = cloud - p
        along = q @ t_[i]
        sel = q[np.abs(along) < slab]
        if len(sel) < 6:
            R[i] = R[i - 1] if i else rmin
            continue
        for k, a in enumerate(angles):
            d = n_[i] * math.cos(a) + b_[i] * math.sin(a)
            R[i, k] = min(rmax, max(rmin, float((sel @ d).max())))
    R = smooth_rows(R, *smooth) + ease
    ring, prm = [], []
    for i, p in enumerate(pts):
        for k, a in enumerate(angles):
            r = R[i, k] + (extra(T[i], a) if extra else 0.0)
            ring.append(p + (n_[i] * math.cos(a) + b_[i] * math.sin(a)) * r)
            prm.append((T[i], (a % TWO_PI) / TWO_PI, 0.0))
    P = np.array(ring)
    faces, uv, _, _ = B.grid_uv(P, sides, len(pts), closed_u=True)
    return B.Piece(P, faces, uv, np.array(prm))


# -------------------------------------------------------------------------------------------------------------
# The figure: measurements of the posed body
# -------------------------------------------------------------------------------------------------------------

class Figure:
    def __init__(self, g, masks, bones):
        """g: (n, 3) body vertices in game space; masks: {'torso','arm_L','arm_R','leg_L','leg_R','neck'} -> bool;
        bones: game-space joints (shoulder/elbow/wrist per side, clavicle, neck01, jaw, jaw_tail, eye_L, eye_R)."""
        self.g = g
        self.m = masks
        self.b = bones
        torso = g[masks["torso"]]
        self.collar_y = float(bones["clavicle_L"][1]) + 0.032
        self.torso = Field(torso, 0.3, self.collar_y + 0.09)
        self._landmarks()
        self.coat = self._coat_field()
        self.neck = Field(g[masks["neck"] | masks["torso"]], self.collar_y - 0.12, float(bones["jaw_tail"][1]) + 0.01,
                          sigma_y=1.0)

    def _landmarks(self):
        f = self.torso
        front = [f.r(0.0, y) for y in f.ys]
        girth = f.R.mean(axis=1)
        ys = f.ys
        band = (ys > 1.0) & (ys < 1.22)
        self.bust_y = float(ys[band][np.argmax(np.array(front)[band])])
        band = (ys > 0.88) & (ys < self.bust_y - 0.05)
        self.waist_y = float(ys[band][np.argmin(girth[band])])
        band = (ys > 0.7) & (ys < self.waist_y)
        self.hip_y = float(ys[band][np.argmax(girth[band])])
        self.hem_y = 0.465
        self.button_top = self.bust_y - 0.045
        self.pocket = {}
        for side, s in (("L", 1), ("R", -1)):
            w = self.b["wrist_" + side]
            self.pocket[side] = (math.atan2(w[0], w[2] - f.cz(w[1])), float(w[1]) + 0.035)

    def _coat_field(self):
        """The coat's radius: the body hull plus ease; no concavity from the bust down to the waist (the cloth spans
        it), never narrower below the hips than at the hips (the skirt falls straight, then flares)."""
        f = self.torso
        R = f.R.copy()
        ys = f.ys
        iw, ib, ih = f.row(self.waist_y), f.row(self.bust_y), f.row(self.hip_y)
        # Bust to waist: per angle, the upper envelope between the two rows (a straight drape for the front).
        for k in range(R.shape[1]):
            a, b = R[iw, k], R[ib, k]
            for i in range(iw, ib + 1):
                t = (i - iw) / max(ib - iw, 1)
                R[i, k] = max(R[i, k], a + (b - a) * t ** 1.2)
        # Below the hips: monotone (never tucks in round the thighs or knees).
        for i in range(ih - 1, -1, -1):
            R[i] = np.maximum(R[i], R[i + 1])
        ease = np.array([coat_ease(y, self) for y in ys])
        out = Field.__new__(Field)
        out.ys, out.th, out.CZ = f.ys, f.th, f.CZ
        out.R = smooth_rows(R, 1.5, 1.5) + ease[:, None]
        return out


def coat_ease(y, fig):
    """How far the coat stands off the body: close at the chest, an A-line flare below the hips."""
    return 0.011 + 0.006 * pk.sstep(fig.waist_y, fig.hip_y, y) + 0.034 * pk.sstep(fig.hip_y, fig.hem_y, y) ** 1.25


def coat_open(y, fig):
    """Half-angle of the front opening: overlapped below the top button, a V up to the collar, a small inverted V at
    the hem."""
    v = 0.40 * pk.sstep(fig.button_top, fig.collar_y, y) ** 0.85
    hem = 0.08 * pk.sstep(fig.hem_y + 0.14, fig.hem_y, y)
    return 0.004 + v + hem


def coat_point(fig, theta, y, off=0.0):
    p = fig.coat.point(theta, y, off)
    fold = math.sin(theta * 7.0 + 0.6) * 0.006 * pk.sstep(fig.hip_y, fig.hem_y, y)
    p[0] += math.sin(theta) * fold
    p[2] += math.cos(theta) * fold * 0.7
    return p


# -------------------------------------------------------------------------------------------------------------
# Coat
# -------------------------------------------------------------------------------------------------------------

def coat_body(fig):
    top = fig.collar_y + 0.028
    ys = np.concatenate([np.linspace(fig.hem_y, fig.hip_y, 8), np.linspace(fig.hip_y, fig.waist_y, 5)[1:],
                         np.linspace(fig.waist_y, fig.collar_y - 0.06, 9)[1:], np.linspace(fig.collar_y - 0.06, top, 8)[1:]])
    nu = 48

    def pt(u, y):
        a0 = coat_open(y, fig)
        return coat_point(fig, a0 + (TWO_PI - 2 * a0) * u, y)

    us = np.linspace(0, 1, nu)
    span = top - fig.hem_y
    outer = B.loft(pt, us, ys, params_fn=lambda u, y: (u, (y - fig.hem_y) / span, 0.0))

    def inner(u, y):
        a0 = coat_open(y, fig)
        return coat_point(fig, a0 + (TWO_PI - 2 * a0) * u, y, -0.004)
    facing = B.loft(inner, us, np.linspace(fig.hem_y + 0.002, fig.hem_y + 0.05, 3), flip=True, params_fn=lambda u, y: (u, 0.0, 1.0))
    P = np.vstack([np.array([pt(u, fig.hem_y) for u in us]), np.array([inner(u, fig.hem_y + 0.002) for u in us])])
    faces, uv, _, _ = B.grid_uv(P, nu, 2)
    band = B.Piece(P, [f[::-1] for f in faces], [c[::-1] for c in uv], np.array([(u, 0.0, 1.0) for _ in range(2) for u in us]))
    # Front edge facing: the inside of each front edge in the V shows a strip of the coat's inside.
    return outer.merge(facing).merge(band)


def lapel(fig, s):
    """A notched lapel folded back over the chest: from the top button to the collar, widening, with thickness."""
    nv, nu = 22, 7
    y0, y1 = fig.button_top, fig.collar_y + 0.006

    def width(t):
        w = 0.008 + 0.056 * pk.sstep(0.0, 0.78, t)
        return w * (1 - 0.45 * pk.sstep(0.8, 0.86, t))

    def pt(u, t, off):
        y = y0 + (y1 - y0) * t
        a0 = coat_open(y, fig)
        r = fig.coat.r(a0, y)
        th = a0 - 0.02 + u * width(t) / max(r, 0.05)
        return coat_point(fig, th if s > 0 else -th, y, off)
    us, ts = np.linspace(0, 1, nu), np.linspace(0, 1, nv)
    top = B.loft(lambda u, t: pt(u, t, 0.0085), us, ts, flip=s < 0, params_fn=lambda u, t: (u, t, 0.0))
    bottom = B.loft(lambda u, t: pt(u, t, 0.0035), us, ts, flip=s > 0, params_fn=lambda u, t: (u, t, 1.0))
    piece = top.merge(bottom)
    n = nu * nv
    for col in (0, nu - 1):
        for j in range(nv - 1):
            a, b = j * nu + col, (j + 1) * nu + col
            piece.faces.append((a, b, n + b, n + a) if (col == 0) == (s > 0) else (a, n + a, n + b, b))
            piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    j = nv - 1
    for i in range(nu - 1):
        a, b = j * nu + i, j * nu + i + 1
        piece.faces.append((a, n + a, n + b, b) if s > 0 else (a, b, n + b, n + a))
        piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    return piece


def collar(fig):
    """The rolled stand collar round the back of the neck; its ends run into the lapel tops."""
    nu, nv = 40, 5
    y0 = fig.collar_y + 0.008

    def pt(u, v, off):
        a = math.radians(50 + 260 * u)
        y = y0 + 0.042 * v
        r = fig.neck.r(a, min(y, fig.neck.ys[-1])) + 0.009 + 0.011 * v ** 1.6 + off
        front = pk.sstep(0.35, 0.8, math.cos(a))
        dip = 0.024 * front + 0.03 * front * v
        cz = fig.neck.cz(y)
        return np.array((math.sin(a) * r, y - dip, cz + math.cos(a) * r * 0.97))
    us, vs = np.linspace(0, 1, nu), np.linspace(0, 1, nv)
    outer = B.loft(lambda u, v: pt(u, v, 0.007), us, vs, params_fn=lambda u, v: (u, v, 0.0))
    inner = B.loft(lambda u, v: pt(u, v, 0.0), us, vs, flip=True, params_fn=lambda u, v: (u, v, 1.0))
    piece = outer.merge(inner)
    n = nu * nv
    j = nv - 1
    for i in range(nu - 1):
        a, b = j * nu + i, j * nu + i + 1
        piece.faces.append((b, a, n + a, n + b))
        piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    return piece


def arm_path(fig, side, n=26):
    s = 1 if side == "L" else -1
    sh, el, wr = fig.b["shoulder_" + side], fig.b["elbow_" + side], fig.b["wrist_" + side]
    start = sh + np.array((-0.042 * s, -0.012, 0.0))
    ctrl = [start, sh + np.array((0.002 * s, -0.04, 0.0)), sh * 0.45 + el * 0.55, el, el * 0.5 + wr * 0.5, wr - (wr - el) * 0.02]
    return B.catmull(ctrl, n)


def sleeve(fig, side):
    cloud = fig.g[fig.m["arm_" + side]]
    path = arm_path(fig, side)

    def extra(t, a):
        # A bit of fullness at the cap, compression folds inside the elbow, a flared cuff.
        cap = 0.002 * (1 - pk.sstep(0.0, 0.15, t))
        fold = 0.0025 * math.sin(t * 60.0 + a * 2.0) * pk.sstep(0.4, 0.55, t) * (1 - pk.sstep(0.65, 0.8, t))
        cuff = 0.0015 * pk.sstep(0.85, 1.0, t)
        return cap + fold + cuff
    return support_sweep(path, cloud, 18, 0.008, rmin=0.032, rmax=0.062, extra=extra)


def pocket(fig, side):
    """A patch pocket on the coat's hip, centred on her hand; the sleeve runs into its opening."""
    s = 1 if side == "L" else -1
    th0, ytop = fig.pocket[side]
    nu, nv = 8, 6
    w = 0.072

    def pt(u, v, off):
        y = ytop - 0.105 + 0.105 * v
        r = fig.coat.r(th0, y)
        th = th0 + (u - 0.5) * w * 2 / max(r, 0.05)
        p = coat_point(fig, th, y, off)
        return p
    us, vs = np.linspace(0, 1, nu), np.linspace(0, 1, nv)
    return B.loft(lambda u, v: pt(u, v, 0.004), us, vs, flip=s < 0, params_fn=lambda u, v: (u, v, 0.0))


def mock_top(fig):
    """The navy undersuit: the chest in the V and the ribbed mock collar to under the chin."""
    y0 = fig.button_top - 0.01
    neck_top = float(fig.b["jaw_tail"][1]) - 0.028
    ys = np.concatenate([np.linspace(y0, fig.collar_y, 6), np.linspace(fig.collar_y, neck_top, 9)[1:]])
    nu = 34
    f = fig.neck

    def pt(u, y):
        a = TWO_PI * u
        lift = 0.016 * (0.5 - 0.5 * math.cos(a))           # higher at the back
        yy = min(y + lift * pk.sstep(neck_top - 0.05, neck_top, y), f.ys[-1])
        if y <= fig.collar_y:
            return fig.torso.point(a, yy, 0.0025)
        return f.point(a, yy, 0.004 + 0.003 * pk.sstep(neck_top - 0.04, neck_top, y))
    body = B.loft(pt, np.linspace(0, 1, nu, endpoint=False), ys, closed_u=True, params_fn=lambda u, y: (u, y, 0.0))

    def roll(u, v):
        a = TWO_PI * u
        p = pt(u, neck_top)
        c = np.array((0.0, p[1], f.cz(p[1])))
        d = p - c
        d[1] = 0
        d /= max(np.linalg.norm(d), 1e-9)
        return p + d * (0.0022 * math.sin(math.pi * v)) + np.array((0, 0.0035 * math.sin(0.5 * math.pi * v), 0)) - d * 0.005 * v
    top = B.loft(roll, np.linspace(0, 1, nu, endpoint=False), np.linspace(0, 1, 4), closed_u=True,
                 params_fn=lambda u, v: (u, v, 1.0))
    return body.merge(top), neck_top


def trousers(fig, side):
    s = 1 if side == "L" else -1
    cloud = fig.g[fig.m["leg_" + side]]
    top, bottom = fig.hem_y + 0.14, 0.072
    ys = np.linspace(top, bottom, 18)
    centres = []
    for y in ys:
        sl = cloud[np.abs(cloud[:, 1] - y) < 0.02]
        c = sl.mean(axis=0) if len(sl) else np.array((0.09 * s, y, 0.0))
        centres.append((c[0], y, c[2]))
    path = B.catmull(np.array(centres)[::3].tolist() + [centres[-1]], 18)
    if fig.m.get("shoe") is not None:
        sh = fig.m["shoe"]
        cloud = np.vstack([cloud, sh[(sh[:, 0] * s > 0) & (sh[:, 1] > 0.06) & (sh[:, 1] < 0.1)]])

    def extra(t, a):
        return 0.003 * pk.sstep(0.8, 1.0, t)                # the hem breaks a little over the boot
    return support_sweep(path, cloud, 16, 0.006, slab=0.012, rmin=0.034, rmax=0.075, extra=extra)


# -------------------------------------------------------------------------------------------------------------
# Gear
# -------------------------------------------------------------------------------------------------------------

def surface_frame(fig, x, y, off):
    """Point on the coat's front at (x, y) pushed out by `off`, with (right, up, out) axes."""
    r = fig.coat.r(0.0, y)
    th = math.asin(max(-0.98, min(0.98, x / max(r, 0.05))))
    for _ in range(4):
        p = coat_point(fig, th, y)
        th += (x - p[0]) / max(fig.coat.r(th, y), 0.05)
    p = coat_point(fig, th, y)
    p2 = coat_point(fig, th + 0.01, y)
    right = p2 - p
    right /= np.linalg.norm(right)
    up = np.array((0.0, 1.0, 0.0))
    out = np.cross(right, up)
    out /= np.linalg.norm(out)
    if out[2] < 0:
        out = -out
    return p + out * off, right, up, out


def disc(centre, axis, radius, thickness, sides=20, bevel=0.25):
    axis = np.asarray(axis, float) / np.linalg.norm(axis)
    c = np.asarray(centre, float)
    pts = [c - axis * thickness * 0.5, c - axis * thickness * 0.15, c + axis * thickness * 0.15, c + axis * thickness * 0.5]
    prof = [1 - bevel, 1.0, 1.0, 1 - bevel]
    return B.sweep(pts, lambda t, a: radius * float(np.interp(t, [0, 0.35, 0.65, 1], prof)), sides, cap_start=True, cap_end=True)


def buttons(fig):
    out = None
    ys = np.linspace(fig.button_top - 0.012, fig.hem_y + 0.2, 5)
    for y in ys:
        c, right, up, n = surface_frame(fig, -0.006, y, 0.0028)
        d = disc(c, n, 0.0068, 0.0032, sides=14)
        out = d if out is None else out.merge(d)
    return out, ys


def breast_pocket(fig):
    nu, nv = 7, 5
    y0 = fig.bust_y - 0.005
    x0 = 0.058

    def pt(u, v):
        return surface_frame(fig, x0 + 0.05 * u, y0 + 0.06 * v, 0.0035)[0]
    fig.breast = (x0, x0 + 0.05, y0, y0 + 0.06)
    return B.loft(pt, np.linspace(0, 1, nu), np.linspace(0, 1, nv), params_fn=lambda u, v: (u, v, 0.0))


def pens(fig):
    out = None
    x0, x1, y0, y1 = fig.breast
    for x, h in ((x0 + 0.01, 0.05), (x0 + 0.022, 0.044)):
        base, right, up, n = surface_frame(fig, x, y1 - 0.004, 0.0045)
        p = B.sweep([base, base + up * h], lambda t, a: 0.0036, 8, cap_start=True, cap_end=True)
        clip = B.sweep([base + up * (h - 0.002) + n * 0.003, base + up * (h - 0.02) + n * 0.0036], lambda t, a: 0.0012, 4,
                       cap_start=True, cap_end=True)
        p = p.merge(clip)
        out = p if out is None else out.merge(p)
    return out


def card(fig):
    """The NTI ID badge clipped to her left lapel (params u, v, face)."""
    y = fig.button_top + 0.07
    a0 = coat_open(y, fig)
    p = coat_point(fig, a0 + 0.035 / max(fig.coat.r(a0, y), 0.05), y, 0.0)
    c, right, up, out = surface_frame(fig, float(p[0]), y, 0.0145)
    tilt = math.radians(-8)
    r2 = right * math.cos(tilt) + up * math.sin(tilt)
    u2 = -right * math.sin(tilt) + up * math.cos(tilt)
    w, h, t = 0.021, 0.029, 0.0011
    nu, nv = 6, 8
    pieces = None
    for side in (1, -1):
        def pt(u, v, side=side):
            x = u * 2 - 1
            yy = v * 2 - 1
            k = (abs(x) ** 6 + abs(yy) ** 6) ** (1 / 6)
            m = max(abs(x), abs(yy))
            sc = m / max(k, 1e-9) if k > 0 else 1.0
            return c + r2 * x * w * sc + u2 * yy * h * sc + out * t * side
        piece = B.loft(pt, np.linspace(0, 1, nu), np.linspace(0, 1, nv), flip=side < 0,
                       params_fn=lambda u, v, side=side: (u, v, 0.0 if side > 0 else 1.0))
        pieces = piece if pieces is None else pieces.merge(piece)
    n = nu * nv
    ring = [(0, i) for i in range(nu)] + [(j, nu - 1) for j in range(1, nv)] + [(nv - 1, i) for i in range(nu - 2, -1, -1)] + \
        [(j, 0) for j in range(nv - 2, 0, -1)]
    for k in range(len(ring)):
        j0, i0 = ring[k]
        j1, i1 = ring[(k + 1) % len(ring)]
        a, b = j0 * nu + i0, j1 * nu + i1
        pieces.faces.append((a, n + a, n + b, b))
        pieces.uv.append([(0, 0), (0.002, 0), (0.002, 0.003), (0, 0.003)])
    clip = B.sweep([c + u2 * (h + 0.004), c + u2 * (h - 0.004)], lambda tt, a: 0.0035, 8, cap_start=True, cap_end=True,
                   squash=lambda tt: (1.0, 0.45))
    return pieces, clip


def headset(hair_pts, ears, mouth_corner):
    """(band, earpiece, ring, pad, boom, mic) in game space. hair_pts: the hair mesh's vertices (game space); ears:
    {'L','R'} ear centres; mouth_corner: her right mouth corner."""
    ear_r, ear_l = ears["R"] + np.array((-0.012, 0.0, 0.0)), ears["L"] + np.array((0.011, 0.0, 0.0))
    mid = 0.5 * (ear_r + ear_l)
    # The band runs over the hair in the plane of the two earpieces, slightly behind the crown.
    arc = []
    for k in range(17):
        f = -1.25 + 2.5 * k / 16
        d = np.array((math.sin(f), math.cos(f), -0.12))
        d /= np.linalg.norm(d)
        q = hair_pts - mid
        cosang = (q @ d) / np.maximum(np.linalg.norm(q, axis=1), 1e-9)
        near = q[cosang > 0.995]
        t = float((near @ d).max()) if len(near) else 0.11
        arc.append(mid + d * (t + 0.0028))
    pts = B.catmull(np.vstack([ear_r + np.array((0.0, 0.016, 0.0)), np.array(arc), ear_l + np.array((0.0, 0.014, 0.0))]), 30)
    band = B.sweep(pts, lambda t, a: 0.0042, 8, squash=lambda t: (0.45, 1.0))
    ear = disc(ear_r, (-1, 0, 0.05), 0.019, 0.013, sides=22)
    ring = disc(ear_r + np.array((-0.0072, 0, 0.0)), (-1, 0, 0.05), 0.0135, 0.0022, sides=22, bevel=0.1)
    pad = disc(ear_l, (1, 0, 0.05), 0.012, 0.009, sides=16)
    mc = np.asarray(mouth_corner, float)
    tip = mc + np.array((-0.016, -0.004, 0.026))
    boom_pts = B.catmull([ear_r + np.array((-0.008, -0.012, 0.012)), ear_r * 0.55 + tip * 0.45 + np.array((-0.034, -0.012, 0.012)),
                          ear_r * 0.2 + tip * 0.8 + np.array((-0.022, -0.003, 0.008)), tip], 14)
    boom = B.sweep(boom_pts, lambda t, a: 0.0017, 6)
    d = boom_pts[-1] - boom_pts[-2]
    d /= np.linalg.norm(d)
    mic = B.sweep(B.catmull([boom_pts[-1] - d * 0.003, boom_pts[-1] + d * 0.006], 4),
                  lambda t, a: 0.0028 * math.sin(math.pi * (0.15 + 0.7 * t)) ** 0.5, 10, cap_start=True, cap_end=True)
    return band, ear, ring, pad, boom, mic


def hair_pin(bun_c, bun_axis):
    """A pin through the bun with a cyan tip."""
    ax = np.asarray(bun_axis, float)
    ax /= np.linalg.norm(ax)
    side = np.cross(ax, (0, 1, 0))
    side /= np.linalg.norm(side)
    a = bun_c - side * 0.05 + np.array((0, 0.014, 0))
    b = bun_c + side * 0.052 - np.array((0, 0.006, 0))
    rod = B.sweep([a, a * 0.5 + b * 0.5, b], lambda t, a_: 0.0021, 8, cap_start=True, cap_end=True)
    tip = B.sweep([b, b + (b - a) / np.linalg.norm(b - a) * 0.006], lambda t, a_: 0.0034 * (1 - 0.6 * t), 8, cap_start=True, cap_end=True)
    return rod, tip
