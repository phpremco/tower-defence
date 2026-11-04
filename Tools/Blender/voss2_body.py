"""Dr. Voss round 19: the figure below the head. Game space (x across, +x her left; y up; z forward), metres; feet on
y = 0. Every piece is a lofted or swept surface with its own UVs in metres (u round, v along), so the cloth painters
get clean coordinates and the islands keep one texel density.

Figure: a field-lab coat (A-line, buttoned to the chest, notched lapels folded back, a rolled stand collar, set-in
sleeves, hip pockets she keeps her hands in), the navy mock-neck top in the V, slim dark trousers, boots.
"""

import math

import numpy as np

import portrait_kit as pk

TWO_PI = 2 * math.pi

# Cross-sections of the body under the clothes: height -> (half width, front depth, back depth, centre z).
BODY = np.array([
    # y      hw     front  back   cz
    (0.56, 0.152, 0.085, 0.09, -0.005),
    (0.72, 0.158, 0.088, 0.098, -0.008),
    (0.84, 0.156, 0.086, 0.098, -0.01),
    (0.93, 0.145, 0.082, 0.09, -0.01),
    (1.0, 0.128, 0.082, 0.082, -0.01),
    (1.08, 0.138, 0.096, 0.08, -0.008),
    (1.15, 0.152, 0.11, 0.084, -0.006),
    (1.2, 0.15, 0.104, 0.084, -0.008),
    (1.24, 0.153, 0.088, 0.082, -0.012),
    (1.27, 0.118, 0.072, 0.072, -0.016),
    (1.295, 0.07, 0.056, 0.058, -0.02),
])


def section(y):
    return [float(np.interp(y, BODY[:, 0], BODY[:, k])) for k in range(1, 5)]


def superellipse(theta, hw, front, back, cz, n=2.6):
    """Point on a rounded cross-section; theta 0 = front centre, + towards her left (+x)."""
    s, c = math.sin(theta), math.cos(theta)
    depth = front if c > 0 else back
    k = (abs(s) ** n + abs(c) ** n) ** (-1.0 / n)
    return (hw * s * k, cz + depth * c * k)


def body_point(theta, y, off=0.0, n=2.6):
    hw, fr, bk, cz = section(y)
    x, z = superellipse(theta, hw + off, fr + off, bk + off, cz, n)
    return np.array((x, y, z))


# -------------------------------------------------------------------------------------------------------------
# Generic lofts
# -------------------------------------------------------------------------------------------------------------

def grid_faces(nu, nv, closed_u=False):
    faces = []
    cols = nu if closed_u else nu - 1
    for j in range(nv - 1):
        for i in range(cols):
            i2 = (i + 1) % nu
            a = j * nu + i
            faces.append((a, j * nu + i2, (j + 1) * nu + i2, (j + 1) * nu + i))
    return faces


def grid_uv(P, nu, nv, closed_u=False):
    """Per-corner UVs in metres: u = arc length along rows (averaged over the grid), v = along columns."""
    G = P.reshape(nv, nu, 3)
    if closed_u:
        Gc = np.concatenate([G, G[:, :1]], 1)
    else:
        Gc = G
    du = np.linalg.norm(np.diff(Gc, axis=1), axis=2).mean(axis=0)
    dv = np.linalg.norm(np.diff(G, axis=0), axis=2).mean(axis=1)
    U = np.concatenate([[0], np.cumsum(du)])
    V = np.concatenate([[0], np.cumsum(dv)])
    faces = grid_faces(nu, nv, closed_u)
    uv = []
    cols = nu if closed_u else nu - 1
    for j in range(nv - 1):
        for i in range(cols):
            uv.append([(U[i], V[j]), (U[i + 1], V[j]), (U[i + 1], V[j + 1]), (U[i], V[j + 1])])
    return faces, uv, U, V


class Piece:
    def __init__(self, verts, faces, uv, params=None):
        self.verts = np.asarray(verts, float)
        self.faces = faces
        self.uv = uv
        self.params = params

    def merge(self, other):
        base = len(self.verts)
        self.verts = np.vstack([self.verts, other.verts])
        self.faces = self.faces + [tuple(base + i for i in f) for f in other.faces]
        self.uv = self.uv + other.uv
        if self.params is not None or other.params is not None:
            a = self.params if self.params is not None else np.zeros((base, 3))
            b = other.params if other.params is not None else np.zeros((len(other.verts), 3))
            self.params = np.vstack([a, b])
        return self


def loft(point_fn, us, vs, closed_u=False, params_fn=None, flip=False):
    P = np.array([point_fn(u, v) for v in vs for u in us])
    faces, uv, U, V = grid_uv(P, len(us), len(vs), closed_u)
    if flip:
        faces = [f[::-1] for f in faces]
        uv = [c[::-1] for c in uv]
    prm = None
    if params_fn is not None:
        prm = np.array([params_fn(u, v) for v in vs for u in us])
    return Piece(P, faces, uv, prm)


def frames(points):
    """Parallel-transport frames along a polyline."""
    pts = np.asarray(points, float)
    t = np.gradient(pts, axis=0)
    t /= np.linalg.norm(t, axis=1, keepdims=True)
    ref = np.array((0.0, 0.0, 1.0)) if abs(t[0][2]) < 0.9 else np.array((1.0, 0.0, 0.0))
    n = np.cross(t[0], ref)
    n /= np.linalg.norm(n)
    ns = [n]
    for i in range(1, len(pts)):
        n = ns[-1] - t[i] * (ns[-1] @ t[i])
        n /= np.linalg.norm(n)
        ns.append(n)
    ns = np.array(ns)
    bs = np.cross(t, ns)
    return t, ns, bs


def sweep(points, radius_fn, sides, cap_start=False, cap_end=False, squash=None, angle0=0.0):
    """A tube along `points` (resampled), radius_fn(t, a) -> radius (t 0..1 along, a angle). UVs in metres."""
    pts = np.asarray(points, float)
    t_, n_, b_ = frames(pts)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    T = np.concatenate([[0], np.cumsum(seg)]) / max(seg.sum(), 1e-9)
    ring = []
    prm = []
    for i, p in enumerate(pts):
        for k in range(sides):
            a = angle0 + TWO_PI * k / sides
            r = radius_fn(T[i], a)
            sx, sy = (1.0, 1.0) if squash is None else squash(T[i])
            ring.append(p + (n_[i] * math.cos(a) * sx + b_[i] * math.sin(a) * sy) * r)
            prm.append((T[i], (a % TWO_PI) / TWO_PI, 0.0))
    P = np.array(ring)
    faces, uv, U, V = grid_uv(P, sides, len(pts), closed_u=True)
    piece = Piece(P, faces, uv, np.array(prm))
    for end, ok in ((0, cap_start), (len(pts) - 1, cap_end)):
        if not ok:
            continue
        c = len(piece.verts)
        piece.verts = np.vstack([piece.verts, pts[end][None]])
        piece.params = np.vstack([piece.params, [(T[end], 0.0, 1.0)]])
        row = [end * sides + k for k in range(sides)]
        for k in range(sides):
            a, b = row[k], row[(k + 1) % sides]
            f = (b, a, c) if end == 0 else (a, b, c)
            piece.faces.append(f)
            ang = [TWO_PI * k / sides, TWO_PI * (k + 1) / sides]
            rr = 0.02
            uvs = {a: (rr * math.cos(ang[0]), rr * math.sin(ang[0])), b: (rr * math.cos(ang[1]), rr * math.sin(ang[1])), c: (0.0, 0.0)}
            piece.uv.append([uvs[i] for i in f])
    return piece


def catmull(points, n):
    """Catmull-Rom resample of control points to n points."""
    P = np.asarray(points, float)
    P = np.vstack([P[0] * 2 - P[1], P, P[-1] * 2 - P[-2]])
    out = []
    segs = len(P) - 3
    for i in range(n):
        s = i / (n - 1) * segs
        k = min(int(s), segs - 1)
        t = s - k
        p0, p1, p2, p3 = P[k], P[k + 1], P[k + 2], P[k + 3]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return np.array(out)


# -------------------------------------------------------------------------------------------------------------
# The coat
# -------------------------------------------------------------------------------------------------------------

HEM = 0.5
BUTTON_TOP = 1.075                    # the V opens above this
COLLAR_Y = 1.272
SHOULDER = np.array((0.152, 1.198, -0.014))
ELBOW = np.array((0.19, 0.99, -0.03))
WRIST = np.array((0.145, 0.872, 0.066))
POCKET_Y = (0.8, 0.905)


def coat_ease(y):
    """How far the coat stands off the body: close at the chest, an A-line flare below the waist."""
    return 0.012 + 0.006 * pk.sstep(1.0, 0.8, y) + 0.03 * pk.sstep(0.85, 0.5, y) ** 1.3


def coat_open(y):
    """Half-angle of the front opening: overlapped (closed) below the top button, a V up to the collar, and a small
    inverted V at the hem."""
    v = 0.42 * pk.sstep(BUTTON_TOP, COLLAR_Y, y) ** 0.85
    hem = 0.09 * pk.sstep(0.64, HEM, y)
    return 0.004 + v + hem


def coat_point(theta, y):
    p = body_point(theta, y, coat_ease(y), n=2.4)
    # Soft drape folds on the skirt, deeper towards the hem and at the sides/back.
    fold = math.sin(theta * 7.0 + 0.6) * 0.006 * pk.sstep(0.9, 0.5, y)
    p[0] += math.sin(theta) * fold
    p[2] += math.cos(theta) * fold * 0.7
    return p


def coat_body():
    ys = np.concatenate([np.linspace(HEM, 1.0, 14), np.linspace(1.0, COLLAR_Y, 13)[1:]])
    nu = 50

    def pt(u, y):
        a0 = coat_open(y)
        th = a0 + (TWO_PI - 2 * a0) * u
        return coat_point(th, y)

    us = np.linspace(0, 1, nu)
    outer = loft(pt, us, ys, params_fn=lambda u, y: (u, (y - HEM) / (COLLAR_Y - HEM), 0.0))
    # A short facing inside the hem and the front edges, so the coat has thickness where you can see in.
    def inner(u, y):
        a0 = coat_open(y)
        th = a0 + (TWO_PI - 2 * a0) * u
        p = body_point(th, y, coat_ease(y) - 0.004, n=2.4)
        return p
    hem_ys = np.linspace(HEM + 0.002, HEM + 0.05, 3)
    facing = loft(inner, us, hem_ys, flip=True, params_fn=lambda u, y: (u, 0.0, 1.0))
    # Hem band joining outer and inner at the bottom edge.
    band_rows = [np.array([pt(u, HEM) for u in us]), np.array([inner(u, HEM + 0.002) for u in us])]
    P = np.vstack(band_rows)
    faces, uv, _, _ = grid_uv(P, nu, 2)
    faces = [f[::-1] for f in faces]
    uv = [c[::-1] for c in uv]
    band = Piece(P, faces, uv, np.array([(u, 0.0, 1.0) for _ in range(2) for u in us]))
    return outer.merge(facing).merge(band)


def lapel(s):
    """A notched lapel folded back over the chest: from the top button to the collar, widening, with thickness."""
    nv, nu = 22, 7

    def width(t):
        w = 0.008 + 0.058 * pk.sstep(0.0, 0.78, t)
        notch = pk.sstep(0.8, 0.86, t)
        return w * (1 - 0.45 * notch)

    def pt(u, t, off):
        y = BUTTON_TOP + (COLLAR_Y + 0.006 - BUTTON_TOP) * t
        a0 = coat_open(y)
        hw = section(y)[0] + coat_ease(y)
        th = a0 - 0.02 + u * width(t) / hw
        p = body_point(th * s if s > 0 else -th, y, coat_ease(y) + off, n=2.4)
        return p
    us = np.linspace(0, 1, nu)
    ts = np.linspace(0, 1, nv)
    top = loft(lambda u, t: pt(u, t, 0.0085), us, ts, flip=s < 0, params_fn=lambda u, t: (u, t, 0.0))
    bottom = loft(lambda u, t: pt(u, t, 0.0035), us, ts, flip=s > 0, params_fn=lambda u, t: (u, t, 1.0))
    piece = top.merge(bottom)
    # Edges: the outer (folded) edge and the inner edge, closing the slab.
    n = nu * nv
    for col in (0, nu - 1):
        for j in range(nv - 1):
            a, b = j * nu + col, (j + 1) * nu + col
            f = (a, b, n + b, n + a) if (col == 0) == (s > 0) else (a, n + a, n + b, b)
            piece.faces.append(f)
            piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    for j in (nv - 1,):
        for i in range(nu - 1):
            a, b = j * nu + i, j * nu + i + 1
            f = (a, n + a, n + b, b) if s > 0 else (a, b, n + b, n + a)
            piece.faces.append(f)
            piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    return piece


def collar():
    """The rolled stand collar round the back of the neck; its ends run into the lapel tops."""
    nu, nv = 40, 5

    def pt(u, v, off):
        a = math.radians(50 + (360 - 100) * u)
        y = COLLAR_Y - 0.004 + 0.056 * v
        r = 0.066 + 0.014 * v ** 1.4 + off
        front = pk.sstep(0.35, 0.8, math.cos(a))
        dip = 0.024 * front + 0.03 * front * v
        return np.array((math.sin(a) * r, y - dip, -0.02 + math.cos(a) * r * 0.95))
    us = np.linspace(0, 1, nu)
    vs = np.linspace(0, 1, nv)
    outer = loft(lambda u, v: pt(u, v, 0.007), us, vs, params_fn=lambda u, v: (u, v, 0.0))
    inner = loft(lambda u, v: pt(u, v, 0.0), us, vs, flip=True, params_fn=lambda u, v: (u, v, 1.0))
    piece = outer.merge(inner)
    n = nu * nv
    j = nv - 1
    for i in range(nu - 1):
        a, b = j * nu + i, j * nu + i + 1
        piece.faces.append((b, a, n + a, n + b))
        piece.uv.append([(0, 0), (0.004, 0), (0.004, 0.01), (0, 0.01)])
    return piece


def sleeve(s):
    ctrl = [SHOULDER + np.array((-0.008, 0.004, 0.0)), SHOULDER + np.array((0.012, -0.04, -0.004)),
            (SHOULDER * 0.45 + ELBOW * 0.55) + np.array((0.008, 0, 0)), ELBOW, ELBOW * 0.55 + WRIST * 0.45 + np.array((0.012, 0, 0)), WRIST]
    pts = catmull(ctrl, 24)
    pts[:, 0] *= s

    def radius(t, a):
        r = np.interp(t, [0.0, 0.12, 0.45, 0.6, 1.0], [0.05, 0.05, 0.047, 0.046, 0.042])
        # Compression folds inside the elbow, a couple of soft rings at the cuff.
        fold = 0.0025 * math.sin(t * 60.0 + a * 2.0) * pk.sstep(0.4, 0.55, t) * (1 - pk.sstep(0.65, 0.8, t))
        return r + fold
    return sweep(pts, radius, 18)


def mock_top():
    """The navy undersuit: the chest in the V and the mock collar round the neck."""
    ys = np.linspace(1.04, 1.288, 10)
    nu = 36
    chest = loft(lambda u, y: body_point(TWO_PI * u, y, 0.002, n=2.4), np.linspace(0, 1, nu, endpoint=False), ys,
                 closed_u=True, params_fn=lambda u, y: (u, y, 0.0))
    neck_ys = np.linspace(1.282, 1.336, 5)

    def neck(u, y):
        a = TWO_PI * u
        r = 0.0525 + 0.005 * pk.sstep(1.315, 1.282, y)
        return np.array((math.sin(a) * r, y, -0.019 + math.cos(a) * r))
    collar_ = loft(neck, np.linspace(0, 1, 32, endpoint=False), neck_ys, closed_u=True, params_fn=lambda u, y: (u, y, 1.0))
    # A rolled top edge so the collar has thickness.
    top = loft(lambda u, v: np.array((math.sin(TWO_PI * u) * (0.0525 - 0.006 * v), 1.336 + 0.002 * math.sin(math.pi * v),
                                      -0.019 + math.cos(TWO_PI * u) * (0.0525 - 0.006 * v))),
               np.linspace(0, 1, 32, endpoint=False), np.linspace(0, 1, 3), closed_u=True, params_fn=lambda u, v: (u, 1.34, 1.0))
    return chest.merge(collar_).merge(top)


def trousers(s):
    ctrl = [np.array((0.085, 0.74, -0.01)), np.array((0.088, 0.5, 0.0)), np.array((0.086, 0.28, -0.005)), np.array((0.084, 0.11, -0.012))]
    pts = catmull(ctrl, 16)
    pts[:, 0] *= s

    def radius(t, a):
        return float(np.interp(t, [0.0, 0.4, 0.7, 1.0], [0.07, 0.058, 0.05, 0.052]))
    return sweep(pts, radius, 16)


def boot(s):
    """A rounded work boot: a lofted shell from the sole up to the shaft."""
    nu = 20
    ys = [0.0, 0.012, 0.035, 0.07, 0.1, 0.13]

    def pt(u, y):
        a = TWO_PI * u
        # Foot footprint: long to the front, round heel; the shaft round at the top.
        t = pk.sstep(0.04, 0.12, y)
        front = 0.165 * (1 - t) + 0.055 * t
        back = 0.07 * (1 - t) + 0.055 * t
        hw = 0.052 * (1 - t) + 0.055 * t
        if y == 0.0:
            front *= 0.97
            back *= 0.97
            hw *= 0.95
        c, sn = math.cos(a), math.sin(a)
        depth = front if c > 0 else back
        k = (abs(sn) ** 2.3 + abs(c) ** 2.3) ** (-1 / 2.3)
        toe_lift = 0.012 * pk.sstep(0.6, 1.0, c) * (1 - t) if y < 0.02 else 0.0
        return np.array((s * 0.085 + hw * sn * k, y + toe_lift, -0.01 + depth * c * k))
    piece = loft(pt, np.linspace(0, 1, nu, endpoint=False), ys, closed_u=True, params_fn=lambda u, y: (u, y, 0.0))
    # Sole cap.
    c = len(piece.verts)
    piece.verts = np.vstack([piece.verts, [(s * 0.085, 0.0, 0.03)]])
    piece.params = np.vstack([piece.params, [(0, 0, 1)]])
    for k in range(nu):
        a, b = k, (k + 1) % nu
        piece.faces.append((b, a, c))
        piece.uv.append([(0, 0), (0.01, 0), (0.005, 0.01)])
    return piece


def pocket(s):
    """A patch pocket on the coat's hip with a flap line; the hand goes in at its top."""
    nu, nv = 8, 6

    def pt(u, v, off):
        y = POCKET_Y[0] + (POCKET_Y[1] - POCKET_Y[0]) * v
        hw = section(y)[0] + coat_ease(y)
        x = 0.07 + 0.075 * u
        th = math.asin(min(0.98, x / hw))
        p = coat_point(th, y)
        n = p - np.array((0, y, -0.01))
        n /= np.linalg.norm(n)
        q = p + n * off
        q[0] *= s
        return q
    us = np.linspace(0, 1, nu)
    vs = np.linspace(0, 1, nv)
    top = loft(lambda u, v: pt(u, v, 0.004), us, vs, flip=s < 0, params_fn=lambda u, v: (u, v, 0.0))
    return top
