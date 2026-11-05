"""Round 34: Gravemaw's modular 3D fissure kit (`Gravemaw_Fissure`).

Remco found the flat crack decal ugly (square ends, hard bends, overlaps) and after two Unity passes it still read as
orange light streaks on the ground. A flat decal cannot show depth from the 44-degree game camera, so this kit is real
geometry that Unity lays along the smoothed crack path:

- Each module is a strip built round a path (straight, or an arc for the curves): two half-strips (left and right of the
  path) with one shared cross-section at both ends, so every module joins every other module without a seam.
- The cross-section: a molten core deep in the gap, dark walls that step down to it, a broken rock face, raised lips of
  fractured ground slabs (each slab its own height and outward tilt, a crack between slabs), a stepped slab edge and a
  thin ground skirt that fades into the terrain. Pebbles lie on the lips and on the ledge in the gap.
- Texture: one 512 strip atlas (U = metres along the path, 1 m per repeat; V = place in the cross-section), so the
  rock, the walls and the molten core run on across joins. Glow only in the core and the lowest part of the walls.
- Reveal: a shape key `Closed` (everything folded flat into the path's centre line: gap shut, lips flat, pebbles sunk)
  and the same offset in UV channels for a vertex shader (see README round 34).
- A second material slot `Gravemaw_Fissure_Punch` holds the gap's opening at ground level: Unity draws it to clear the
  ground's depth so the walls under the ground show (the ground is opaque at y = 0).

Run: blender --background --factory-startup --python Tools/Blender/build_fissure34.py -- [--preview <dir>] [--norender]
     [--notex]
"""

import bisect
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402

ROUND = 34
ASSET = "Gravemaw_Fissure"
TEX = 512
LOW_TEX = 256
U_PERIOD = 1.0          # metres along the path per texture repeat (module lengths are whole metres)
JOIN = 0.025            # the canonical cross-section holds this far in from each joinable end
RAMP = 0.12             # then the noise ramps in over this length
PUNCH_Z = 0.003
EMISSION_STRENGTH = 2.6

# Half cross-section at full width, from the core outwards: (t across from the centre line, z, V in the atlas).
COLS = [
    (-0.02, -0.52, 0.02),   # 0 core floor (crosses the centre line so the two halves overlap)
    (0.085, -0.46, 0.08),   # 1 core edge
    (0.13, -0.35, 0.14),    # 2 lower wall
    (0.18, -0.25, 0.21),    # 3 ledge
    (0.20, -0.14, 0.27),    # 4 mid wall
    (0.25, -0.03, 0.33),    # 5 upper wall
    (0.27, 0.07, 0.40),     # 6 lip edge (top of the broken face)
    (0.30, 0.105, 0.46),    # 7 lip crest
    (0.40, 0.08, 0.58),     # 8 lip top
    (0.49, 0.045, 0.68),    # 9 slab outer edge
    (0.51, 0.01, 0.74),     # 10 slab foot (the step down to the ground)
    (0.72, 0.006, 0.88),    # 11 skirt edge (fades out)
]
LIP_EDGE = 6
GAP_DEPTH = 0.52
LOD_COLS = {0: list(range(12)), 1: [0, 1, 2, 4, 5, 6, 7, 9, 10, 11]}
LOD_STEP = {0: 0.14, 1: 0.28}

HOT = np.array((1.0, 0.42, 0.1))

MODULES = [
    dict(name="Fissure_Straight_A", kind="straight", length=2.0, seed=3401, rubble=6),
    dict(name="Fissure_Straight_B", kind="straight", length=3.0, seed=3402, rubble=8),
    dict(name="Fissure_Straight_C", kind="straight", length=2.0, seed=3403, rubble=11),
    dict(name="Fissure_Curve_L", kind="curve", length=2.0, turn=20.0, seed=3404, rubble=6),
    dict(name="Fissure_Curve_R", kind="curve", length=2.0, turn=-20.0, seed=3405, rubble=6),
    dict(name="Fissure_Start", kind="start", length=2.0, seed=3406, rubble=5),
    dict(name="Fissure_Head", kind="head", length=2.0, seed=3407, rubble=6),
    dict(name="Fissure_Branch_L", kind="branch", length=1.6, side=1, seed=3408, rubble=3),
    dict(name="Fissure_Branch_R", kind="branch", length=1.6, side=-1, seed=3409, rubble=3),
]


def ss(e0, e1, x):
    t = np.clip((np.asarray(x, dtype=float) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def hash01(*keys):
    h = 0.0
    for i, k in enumerate(keys):
        h += (k + 0.123) * (12.9898 + 31.7 * i)
    v = math.sin(h) * 43758.5453
    return v - math.floor(v)


# ---------------------------------------------------------------------------------------------------------------
# Paths: Blender XY plane, forward = -Y (Unity +Z), left = +X (Unity -X)
# ---------------------------------------------------------------------------------------------------------------

class Path2:
    def __init__(self, p0=(0.0, 0.0), heading_deg=-90.0, kappa=0.0):
        self.p0 = np.array(p0, dtype=float)
        self.phi0 = math.radians(heading_deg)
        self.kappa = kappa

    def at(self, s):
        phi = self.phi0 + self.kappa * s
        if abs(self.kappa) < 1e-9:
            p = self.p0 + s * np.array((math.cos(self.phi0), math.sin(self.phi0)))
        else:
            k = self.kappa
            p = self.p0 + np.array((math.sin(phi) - math.sin(self.phi0), -(math.cos(phi) - math.cos(self.phi0)))) / k
        t = np.array((math.cos(phi), math.sin(phi)))
        n = np.array((-t[1], t[0]))
        return p, t, n, phi


class Module:
    """One kit piece: its path, width profile, noise and slab plates, and the functions that place any point of its
    cross-section at any distance along it (so rows, the punch cap and the pebbles all agree)."""

    def __init__(self, spec):
        self.spec = spec
        self.name = spec["name"]
        self.kind = spec["kind"]
        self.L = spec["length"]
        self.rng = np.random.default_rng(spec["seed"])
        if self.kind == "curve":
            self.path = Path2(kappa=math.radians(spec["turn"]) / self.L)
        elif self.kind == "branch":
            side = spec["side"]
            self.path = Path2(p0=(side * 0.2, -0.05), heading_deg=-90.0 + side * 38.0, kappa=-side * 0.2)
        else:
            self.path = Path2()
        self.join_start = self.kind in ("straight", "curve", "head")
        self.join_end = self.kind in ("straight", "curve", "start")
        r = self.rng
        self.mea = (r.uniform(0, 6.3), r.uniform(1.4, 2.2), r.uniform(0.03, 0.06))
        self.side = {}
        for sigma in (1, -1):
            knots = [0.0]
            while knots[-1] < self.L + 0.2:
                knots.append(knots[-1] + r.uniform(0.07, 0.16))
            jag = r.uniform(-1, 1, len(knots)) * 0.042
            bounds = [0.0]
            while bounds[-1] < self.L - 0.45:
                bounds.append(bounds[-1] + r.uniform(0.25, 0.8))
            if self.L - bounds[-1] < 0.25:
                bounds[-1] = self.L
            else:
                bounds.append(self.L)
            plates = [(r.uniform(-0.02, 0.04), r.uniform(0.0, 0.06), r.uniform(-0.06, 0.06))
                      for _ in range(len(bounds) - 1)]
            # Each crack between slabs runs at a slant (0 at the joinable ends so the join profile stays square).
            skews = [0.0] + [r.uniform(-0.9, 0.9) for _ in bounds[1:-1]] + [0.0]
            self.side[sigma] = dict(knots=np.array(knots), jag=jag, bounds=bounds, plates=plates, skews=skews,
                                    breathe=(r.uniform(0, 6.3), r.uniform(0, 6.3)))

    # Width and envelopes ------------------------------------------------------------------------------------
    def width(self, s):
        L = self.L
        if self.kind == "start":
            return float(ss(0.0, 1.4, s)) ** 0.85
        if self.kind == "head":
            return max(0.0, 1.0 - s / L) ** 0.8
        if self.kind == "branch":
            return 0.5 * float(ss(0.0, 0.35, s)) * max(0.0, 1.0 - s / L) ** 0.9
        return 1.0

    def env(self, s):
        e = 1.0
        if self.join_start:
            e *= float(ss(JOIN, JOIN + RAMP, s))
        if self.join_end:
            e *= float(ss(JOIN, JOIN + RAMP, self.L - s))
        return e

    def dip(self, s, sigma):
        b = self.side[sigma]["bounds"]
        d = min(abs(s - x) for x in b)
        return max(0.0, 1.0 - d / JOIN)

    def plate(self, s, sigma):
        sd = self.side[sigma]
        i = min(max(bisect.bisect_right(sd["bounds"], s) - 1, 0), len(sd["plates"]) - 1)
        return sd["plates"][i]

    def head_heave(self, s):
        if self.kind != "head":
            return 1.0
        return 1.0 + 0.7 * float(ss(0.45, 0.75, s / self.L) * (1 - ss(0.85, 1.0, s / self.L)))

    # Cross-section ------------------------------------------------------------------------------------------
    def tz(self, s, sigma, j):
        """(t, z) of column j at distance s on side sigma (t >= 0 outwards from the centre)."""
        sd = self.side[sigma]
        w = self.width(s)
        e = self.env(s)
        tip = 1.0 + (1.6 * (1 - w) if self.kind in ("head", "branch") else 0.0)
        jag = e * float(np.interp(s, sd["knots"], sd["jag"])) * tip
        ph1, ph2 = sd["breathe"]
        b = 1.0 + e * (0.1 * math.sin(2 * math.pi * s / 1.3 + ph1) + 0.06 * math.sin(2 * math.pi * s / 0.55 + ph2))
        h, tilt, out = self.plate(s, sigma)
        h, tilt, out = h * e, tilt * e, out * e
        depth = w ** 0.7
        lipw = w ** 0.4
        lipz = w ** 0.5 * self.head_heave(s)
        t0, z0, _ = COLS[j]
        t6 = (0.27 * b + jag) * w
        if j < LIP_EDGE:
            k = (0.2, 0.5, 1.0, 1.0, 1.0, 1.0)[j]
            t = (t0 * b + jag * k) * w if j > 0 else t0 * w
            jit = e * (hash01(round(s * 1000), j, sigma, self.spec["seed"]) * 2 - 1) if 2 <= j <= 5 else 0.0
            z = z0 * depth + 0.025 * jit
            t += 0.018 * jit * w
            if j == 0 and sigma < 0:
                z -= 0.012
            return t, z
        dip = self.dip(s, sigma)
        if j <= 9:
            t = t6 + (t0 - 0.27) * lipw + out * (j - 6) / 3.0
            z = (z0 + h + tilt * (0.49 - t0) / 0.22) * lipz * (1 - 0.5 * dip)
            if j == 6:
                z += 0.012 * e * (hash01(round(s * 1000), 6, sigma, self.spec["seed"]) * 2 - 1)
            return t, z
        t = t6 + (t0 - 0.27) * lipw + out * (1.0 if j == 10 else 0.3)
        return t, z0

    def skewed(self, s, sigma, j):
        if j < LIP_EDGE:
            return s
        sd = self.side[sigma]
        k = float(np.interp(s, sd["bounds"], sd["skews"])) * self.env(s)
        return float(np.clip(s + k * (COLS[j][0] - 0.27), 0.0, self.L))

    def point(self, s, sigma, j):
        s = self.skewed(s, sigma, j)
        t, z = self.tz(s, sigma, j)
        p, _, n, _ = self.path.at(s)
        ph, period, amp = self.mea
        m = self.env(s) * amp * math.sin(2 * math.pi * s / period + ph) * self.width(s)
        xy = p + n * (sigma * t + m)
        return np.array((xy[0], xy[1], z))

    def closed_point(self, s, sigma, j):
        p, _, n, _ = self.path.at(s)
        return np.array((p[0], p[1], 0.004 if j >= LIP_EDGE else 0.0))

    def point_f(self, s, sigma, jf):
        j0 = int(math.floor(jf))
        j1 = min(j0 + 1, len(COLS) - 1)
        f = jf - j0
        return self.point(s, sigma, j0) * (1 - f) + self.point(s, sigma, j1) * f

    def rows(self, sigma, lod):
        step = LOD_STEP[lod]
        rows = set(np.round(np.arange(0.0, self.L, step), 4).tolist()) | {self.L}
        special = {0.0, JOIN, self.L - JOIN, self.L}
        for x in self.side[sigma]["bounds"][1:-1]:
            special |= ({x - JOIN, x, x + JOIN} if lod == 0 else {x})
        out = sorted(special)
        for r in sorted(rows):
            if all(abs(r - x) > 0.04 for x in out):
                out.append(r)
        return sorted(x for x in out if -1e-9 <= x <= self.L + 1e-9)


# ---------------------------------------------------------------------------------------------------------------
# Mesh assembly
# ---------------------------------------------------------------------------------------------------------------

class MeshData:
    def __init__(self):
        self.v, self.vc, self.uv, self.rev, self.col, self.faces, self.mat = [], [], [], [], [], [], []

    def add(self, p, pc, uv, rev, col):
        self.v.append(tuple(p)); self.vc.append(tuple(pc)); self.uv.append(tuple(uv))
        self.rev.append(tuple(rev)); self.col.append(tuple(col))
        return len(self.v) - 1

    def face(self, idx, mat=0):
        self.faces.append(tuple(idx)); self.mat.append(mat)


def build_half(md, mod, sigma, lod):
    cols = LOD_COLS[lod]
    rows = mod.rows(sigma, lod)
    uoff = 0.0 if sigma > 0 else 0.37
    grid = []
    for s in rows:
        line = []
        for j in cols:
            p = mod.point(s, sigma, j)
            pc = mod.closed_point(s, sigma, j)
            depth = float(np.clip(-p[2] / GAP_DEPTH, 0, 1))
            fade = 0.0 if j == 11 else 1.0
            if mod.kind in ("start", "head", "branch"):
                fade *= float(ss(0.0, 0.15, mod.width(s))) if j >= 10 else 1.0
            line.append(md.add(p, pc, (s / U_PERIOD + uoff, COLS[j][2]), (s, s / mod.L), (depth, 0, 0, fade)))
        grid.append(line)
    for r in range(len(rows) - 1):
        for c in range(len(cols) - 1):
            a, b, cc, d = grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]
            # Columns run outwards (+sigma N), rows forward (T): N x T = -Z, so the left half is wound the other way.
            md.face((a, d, cc, b) if sigma > 0 else (a, b, cc, d))


def build_punch(md, mod, lod):
    step = LOD_STEP[lod] * 0.75
    rows = sorted(set(np.round(np.arange(0.0, mod.L, step), 4).tolist()) | {mod.L})
    line = []
    for s in rows:
        p, _, n, _ = mod.path.at(s)
        pair = []
        for sigma in (1, -1):
            q = mod.point(s, sigma, LIP_EDGE)
            q[:2] += n * sigma * 0.012 * mod.width(s)
            q[2] = PUNCH_Z
            pair.append(md.add(q, (p[0], p[1], 0.0), (s, 0.05), (s, s / mod.L), (0, 0, 0, 1)))
        line.append(pair)
    for r in range(len(rows) - 1):
        a, b = line[r]
        c, d = line[r + 1]
        md.face((b, d, c, a), mat=1)


def rock(md, centre, radius, squash, rng, s, mod, sink=0.2):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
    base = len(md.v)
    yaw = rng.uniform(0, 2 * math.pi)
    tilt = rng.uniform(-0.5, 0.5)
    cy, sy, ct, st = math.cos(yaw), math.sin(yaw), math.cos(tilt), math.sin(tilt)
    uoff = rng.uniform(0, 1)
    for v in bm.verts:
        x, y, z = v.co
        k = 1.0 + rng.uniform(-0.28, 0.28)
        x, y, z = x * k * rng.uniform(0.8, 1.25), y * k, z * k * squash
        y, z = y * ct - z * st, y * st + z * ct
        x, y = x * cy - y * sy, x * sy + y * cy
        p = np.array(centre) + np.array((x, y, z)) * radius
        top = v.co.z > 0.35
        md.add(p, (centre[0], centre[1], -sink), (uoff + x * radius * 2, 0.6 if top else 0.37),
               (s, s / mod.L), (0, 1, 0, 1))
    for f in bm.faces:
        md.face([base + v.index for v in f.verts])
    bm.free()


def build_rubble(md, mod, lod):
    rng = np.random.default_rng(mod.spec["seed"] + 77)
    count = mod.spec["rubble"] if lod == 0 else max(1, mod.spec["rubble"] // 3)
    for i in range(count):
        s = rng.uniform(0.15, mod.L - 0.15)
        if mod.width(s) < 0.2:
            continue
        sigma = 1 if rng.random() < 0.5 else -1
        zone = rng.random()
        if zone < 0.3:
            jf, r = rng.uniform(6.6, 8.4), rng.uniform(0.035, 0.075)
        elif zone < 0.75:
            jf, r = rng.uniform(8.5, 10.8), rng.uniform(0.025, 0.055)
        else:
            jf, r = rng.uniform(2.8, 3.3), rng.uniform(0.02, 0.035)
        r *= max(0.5, mod.width(s))
        c = mod.point_f(s, sigma, jf)
        c[2] += r * 0.35
        rock(md, c, r, 0.6, rng, s, mod)
    if mod.kind == "head":
        # Heaved chunks at the tearing tip: the ground pushed up ahead of the boss.
        for i in range(5 if lod == 0 else 2):
            s = rng.uniform(0.55, 0.85) * mod.L
            sigma = 1 if i % 2 == 0 else -1
            c = mod.point_f(s, sigma, rng.uniform(6.5, 8.0))
            r = rng.uniform(0.06, 0.1)
            c[2] += r * 0.2
            rock(md, c, r, 0.55, rng, s, mod)


def ribbon(md, mod, start, heading, length, width, rng, s_rev, lod, sink_to=None):
    """A hairline crack: a thin jagged ribbon just above the ground, tapering to nothing, alpha fading at its end."""
    seg = max(3, int(length / (0.07 if lod == 0 else 0.14)))
    pts = [np.array(start, dtype=float)]
    h = heading
    for i in range(seg):
        h += rng.uniform(-0.45, 0.45)
        pts.append(pts[-1] + np.array((math.cos(h), math.sin(h))) * length / seg)
    root = pts[0]
    ids = []
    acc = 0.0
    for i, p in enumerate(pts):
        if i > 0:
            acc += float(np.linalg.norm(p - pts[i - 1]))
        a = pts[min(i + 1, seg)] - pts[max(i - 1, 0)]
        n = np.array((-a[1], a[0])) / max(np.linalg.norm(a), 1e-6)
        f = i / seg
        wd = width * (1 - f) ** 0.7 + 0.004
        alpha = float(1 - ss(0.6, 1.0, f))
        pair = []
        for side, v in ((1, 0.915), (-1, 0.995)):
            q = p + n * side * wd * 0.5
            pair.append(md.add((q[0], q[1], 0.008), (root[0], root[1], 0.0), (acc + rng.uniform(0, 0.01), v),
                               (s_rev, s_rev / mod.L), (0, 0, 1, alpha)))
        ids.append(pair)
    for i in range(seg):
        a, b = ids[i]
        c, d = ids[i + 1]
        md.face((a, c, d, b))


def build_hairlines(md, mod, lod):
    rng = np.random.default_rng(mod.spec["seed"] + 191)
    if mod.kind == "start":
        p, t, _, phi = mod.path.at(0.0)
        for k in range(4):
            ang = phi + math.pi + rng.uniform(-1.2, 1.2)
            ribbon(md, mod, p, ang, rng.uniform(0.5, 1.0), 0.05, rng, 0.0, lod)
        for sigma in (1, -1):
            s = rng.uniform(0.6, 1.1)
            q = mod.point(s, sigma, 10)
            ribbon(md, mod, q[:2], phi + sigma * rng.uniform(0.9, 1.4), rng.uniform(0.35, 0.6), 0.035, rng, s, lod)
    elif mod.kind == "head":
        p, t, _, phi = mod.path.at(mod.L)
        for k in range(3):
            ribbon(md, mod, p, phi + rng.uniform(-0.6, 0.6), rng.uniform(0.6, 1.1), 0.05, rng, mod.L, lod)
        s = mod.L * 0.4
        sigma = 1 if rng.random() < 0.5 else -1
        q = mod.point(s, sigma, 10)
        ribbon(md, mod, q[:2], phi + sigma * 0.9, 0.5, 0.035, rng, s, lod)
    elif mod.kind == "branch":
        p, t, _, phi = mod.path.at(mod.L)
        for k in range(2):
            ribbon(md, mod, p, phi + rng.uniform(-0.5, 0.5), rng.uniform(0.4, 0.7), 0.035, rng, mod.L, lod)


def to_object(md, name, materials):
    me = bpy.data.meshes.new(name)
    me.from_pydata(md.v, [], md.faces)
    me.validate(clean_customdata=False)
    nloops = len(me.loops)
    vidx = np.zeros(nloops, dtype=np.int64)
    me.loops.foreach_get("vertex_index", vidx)
    uv = np.array(md.uv)
    rev = np.array(md.rev)
    delta = np.array(md.vc) - np.array(md.v)
    # Unity space (Bake Axis Conversion): X = -Blender X, Y = Blender Z, Z = -Blender Y.
    du = np.stack((-delta[:, 0], delta[:, 2], -delta[:, 1]), axis=1)
    for lname, data in (("UV", uv), ("Reveal", rev), ("CloseXY", du[:, :2]),
                        ("CloseZ", np.stack((du[:, 2], np.zeros(len(du))), axis=1))):
        layer = me.uv_layers.new(name=lname)
        layer.data.foreach_set("uv", data[vidx].astype(np.float32).ravel())
    me.uv_layers.active = me.uv_layers["UV"]
    me.uv_layers["UV"].active_render = True
    attr = me.color_attributes.new("Col", "BYTE_COLOR", "POINT")
    cols = np.array(md.col, dtype=np.float32)
    try:
        attr.data.foreach_set("color_srgb", cols.ravel())
    except (AttributeError, TypeError):
        attr.data.foreach_set("color", cols.ravel())
    me.color_attributes.active_color = attr
    for m in materials:
        me.materials.append(m)
    me.polygons.foreach_set("material_index", np.array(md.mat, dtype=np.int32))
    obj = C.link(bpy.data.objects.new(name, me))
    C.activate(obj)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(38))
    obj.shape_key_add(name="Basis", from_mix=False)
    key = obj.shape_key_add(name="Closed", from_mix=False)
    key.data.foreach_set("co", np.array(md.vc, dtype=np.float32).ravel())
    key.value = 0.0
    return obj


def build_module(spec, lod, materials):
    mod = Module(spec)
    md = MeshData()
    for sigma in (1, -1):
        build_half(md, mod, sigma, lod)
    build_rubble(md, mod, lod)
    build_hairlines(md, mod, lod)
    build_punch(md, mod, lod)
    name = spec["name"] + ("_LOD1" if lod else "")
    obj = to_object(md, name, materials)
    return mod, obj


def module_facts(mod):
    p, t, n, phi = mod.path.at(mod.L)
    # Unity: x = -Blender x, z = -Blender y; yaw positive = clockwise seen from above (towards Unity +X, the right).
    end = (round(-p[0], 4), 0.0, round(-p[1], 4))
    yaw = round(-math.degrees(phi - mod.path.phi0), 3)
    if mod.kind == "branch":
        p0 = mod.path.p0
        start = (round(-p0[0], 4), 0.0, round(-p0[1], 4))
    else:
        start = (0.0, 0.0, 0.0)
    return {"length_m": mod.L, "path_start_unity": start, "end_offset_unity": end, "end_yaw_deg": yaw}


# ---------------------------------------------------------------------------------------------------------------
# Texture atlas (numpy): U along (periodic, 1 m), V across the cross-section
# ---------------------------------------------------------------------------------------------------------------

def pnoise(h, w, fu, fv, seed):
    """Periodic smooth noise (std 1): white noise low-passed in the frequency domain; fu/fv cycles per image."""
    rng = np.random.default_rng(seed)
    white = rng.standard_normal((h, w))
    ky = np.fft.fftfreq(h)[:, None] * h
    kx = np.fft.fftfreq(w)[None, :] * w
    filt = np.exp(-((kx / fu) ** 2 + (ky / fv) ** 2))
    out = np.real(np.fft.ifft2(np.fft.fft2(white) * filt))
    return (out - out.mean()) / max(out.std(), 1e-6)


def fbm(h, w, fu, fv, seed, octaves=4):
    out = np.zeros((h, w))
    amp = 1.0
    for o in range(octaves):
        out += amp * pnoise(h, w, fu * 2 ** o, fv * 2 ** o, seed + o * 17)
        amp *= 0.5
    return out / 1.9


def voronoi(U, V, nu, nv, seed, aspect):
    """F1 and F2 of a jittered grid, periodic in U (nu cells per repeat); distances in U units."""
    cu, cv = np.floor(U * nu), np.floor(V * nv)
    f1 = np.full(U.shape, 9.0)
    f2 = np.full(U.shape, 9.0)
    for du in (-1, 0, 1):
        for dv in (-1, 0, 1):
            iu, iv = cu + du, cv + dv
            iw = np.mod(iu, nu)
            h1 = np.sin(iw * 127.1 + iv * 311.7 + seed * 0.37) * 43758.5453
            h2 = np.sin(iw * 269.5 + iv * 183.3 + seed * 0.71) * 43758.5453
            j1, j2 = h1 - np.floor(h1), h2 - np.floor(h2)
            pu, pv = (iu + 0.1 + 0.8 * j1) / nu, (iv + 0.1 + 0.8 * j2) / nv
            d = np.hypot(U - pu, (V - pv) * aspect)
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    return f1, f2


def lin(*srgb):
    return C.srgb_to_linear(np.array(srgb))


def band(v, a, b, soft=0.008):
    return ss(a - soft, a + soft, v) * (1 - ss(b - soft, b + soft, v))


def make_textures(size):
    H = W = size
    v1 = (np.arange(H) + 0.5) / H
    u1 = (np.arange(W) + 0.5) / W
    U, V = np.meshgrid(u1, v1)
    n_fine = fbm(H, W, 24, 24, 11)
    n_mid = fbm(H, W, 6, 8, 23)
    n_big = pnoise(H, W, 2, 3, 37)
    grit = pnoise(H, W, 120, 120, 41)

    core = 1 - ss(0.095, 0.115, V)
    wall = band(V, 0.105, 0.335)
    face = band(V, 0.335, 0.40)
    rim = band(V, 0.40, 0.465)
    top = band(V, 0.465, 0.70)
    step = band(V, 0.70, 0.74)
    skirt = band(V, 0.74, 0.905)
    hair = ss(0.897, 0.913, V)
    wall_d = np.clip((0.335 - V) / 0.23, 0, 1)            # 0 at the wall top, 1 at the core

    # Height (metres) ------------------------------------------------------------------------------------------
    f1, f2 = voronoi(U, V, 7, 18, 5, 1.4)
    plates = f2 - f1
    seams = 1 - ss(0.01, 0.035, plates + 0.006 * n_fine)
    strata = np.sin(V * 2 * math.pi * 14 + 1.2 * n_mid + 0.3 * n_fine)
    g1, g2 = voronoi(U, V, 9, 6, 9, 3.0)
    joints = 1 - ss(0.008, 0.03, g2 - g1 + 0.004 * n_fine)
    c1, c2 = voronoi(U, V, 8, 64, 13, 8.0)
    crust_cells = c2 - c1
    flow = np.sin(U * 2 * math.pi * 3 + 3.0 * n_mid + V * 40)
    hair_c = 0.955 + 0.012 * pnoise(H, W, 10, 2, 51)
    groove = np.exp(-((V - hair_c) / 0.011) ** 2)
    sk1, sk2 = voronoi(U, V, 5, 22, 17, 2.0)
    skirt_cracks = 1 - ss(0.004, 0.014, sk2 - sk1 + 0.003 * n_fine)

    height = (core * (0.004 * flow + 0.003 * n_fine)
              + wall * (0.002 * strata - 0.01 * joints + 0.008 * n_mid + 0.004 * n_fine)
              + (face + rim) * (0.009 * n_fine + 0.004 * n_mid)
              + top * (-0.007 * seams + 0.003 * n_fine + 0.0015 * grit)
              + step * (0.006 * n_fine)
              + skirt * (-0.003 * skirt_cracks + 0.002 * n_fine + 0.001 * grit)
              + hair * (-0.008 * groove + 0.001 * grit))

    # Albedo (linear) -----------------------------------------------------------------------------------------
    ground = lin(0.19, 0.185, 0.245)
    slab = lin(0.2, 0.19, 0.24)
    seam_c = lin(0.07, 0.065, 0.085)
    rim_c = lin(0.33, 0.30, 0.33)
    face_c = lin(0.2, 0.18, 0.2)
    wall_top = lin(0.075, 0.07, 0.085)
    wall_bot = lin(0.09, 0.04, 0.025)
    core_c = lin(0.5, 0.17, 0.05)
    crust_c = lin(0.09, 0.06, 0.05)
    step_c = lin(0.085, 0.08, 0.095)

    def mix(a, b, t):
        return a + (b - a) * np.asarray(t)[..., None]

    tone = (1 + 0.12 * n_mid + 0.06 * grit)[..., None]
    alb = np.zeros((H, W, 3))
    # Dark cooled crust plates float on the melt; the light comes from the cracks between them.
    crust = ss(-0.9, -0.1, n_mid * 0.6 + 0.5 * pnoise(H, W, 5, 14, 61)) * ss(0.006, 0.018, crust_cells)
    alb += core[..., None] * mix(core_c, crust_c, crust)
    wcol = mix(wall_top, wall_bot, wall_d ** 1.2) * (1 + 0.06 * strata + 0.18 * n_mid)[..., None]
    wcol = mix(wcol, seam_c * 0.5, joints * 0.7)
    alb += wall[..., None] * wcol
    alb += face[..., None] * face_c * (1 + 0.2 * n_fine)[..., None]
    alb += rim[..., None] * mix(face_c, rim_c, ss(0.4, 0.45, V) * (1 - ss(0.455, 0.47, V) * 0.6))
    film = ss(-0.3, 0.6, n_big)
    tcol = mix(slab, ground, film * 0.6) * tone
    tcol = mix(tcol, seam_c, seams * 0.7)
    alb += top[..., None] * tcol
    alb += step[..., None] * step_c * tone
    scol = mix(ground * tone, seam_c, skirt_cracks * 0.6)
    alb += skirt[..., None] * scol
    alb += hair[..., None] * mix(ground * tone, seam_c * 0.4, ss(0.2, 0.8, groove))
    alpha = 1 - hair * (1 - ss(0.05, 0.5, groove))

    # Emission: the molten core, a heat flush on the lowest part of the walls, veins only near the bottom -----
    em = np.zeros((H, W, 3))
    molten = (0.8 + 0.12 * flow + 0.12 * n_fine) * (1 - 0.88 * crust)
    em += core[..., None] * HOT * np.clip(molten, 0, 1.2)[..., None]
    heat = (1 - ss(0.105, 0.19, V)) ** 1.5 * 0.45 + joints * (1 - ss(0.105, 0.22, V)) * 0.4
    em += wall[..., None] * HOT * np.clip(heat, 0, 1)[..., None]
    em += hair[..., None] * HOT * (0.18 * ss(0.5, 0.9, groove))[..., None]
    em = np.clip(em, 0, 1)

    # Mask: R AO, G smoothness, B/A detail (none) ---------------------------------------------------------------
    ao = (core * 1.0 + wall * (0.85 - 0.5 * wall_d) * (1 - 0.3 * joints) + face * 0.8 + rim * 0.95
          + top * (1 - 0.4 * seams) + step * 0.55 + skirt * (1 - 0.3 * skirt_cracks) + hair * (1 - 0.5 * groove))
    smooth = core * (0.55 - 0.35 * crust) + wall * 0.14 + (face + rim) * 0.2 + top * 0.12 + step * 0.1 + skirt * 0.13 \
        + hair * 0.12

    # Normal (OpenGL, +Y up) from the height. U px = 1 m / W; V px ~ 2 m / H across the section.
    du_m = U_PERIOD / W
    dv_m = 2.0 / H
    dhdu = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / (2 * du_m)
    dhdv = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / (2 * dv_m)
    dhdv[0, :] = dhdv[-1, :] = 0
    nrm = np.stack((-dhdu, -dhdv, np.ones_like(height)), axis=2)
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)

    rgba = lambda rgb, a: np.concatenate((rgb, np.asarray(a)[..., None]), axis=2)  # noqa: E731
    return {
        "Albedo": rgba(np.clip(alb, 0, 1), alpha),
        "Normal": rgba(nrm * 0.5 + 0.5, np.ones((H, W))),
        "Emission": rgba(em, np.ones((H, W))),
        "Mask": np.stack((np.clip(ao, 0, 1), np.clip(smooth, 0, 1), np.zeros((H, W)), np.zeros((H, W))), axis=2),
    }


def write_textures(folder):
    os.makedirs(os.path.join(folder, "Low"), exist_ok=True)
    maps = make_textures(TEX)
    paths = {}
    for key, arr in maps.items():
        space = "sRGB" if key in ("Albedo", "Emission") else "Non-Color"
        path = os.path.join(folder, f"{ASSET}_{key}.png")
        C._save(arr, path, space)
        C._save(C.downsample(arr, TEX // LOW_TEX, normal=(key == "Normal")), os.path.join(folder, "Low",
                f"{ASSET}_{key}.png"), space)
        paths[key.lower()] = path
    return paths


def textures_on_disk(folder):
    return {k.lower(): os.path.join(folder, f"{ASSET}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}


# ---------------------------------------------------------------------------------------------------------------
# Materials
# ---------------------------------------------------------------------------------------------------------------

def fissure_material(tex):
    """Preview stand-in for the Unity shader: atlas maps on UV, alpha = Albedo.a x vertex A, emission x 3.5."""
    mat = C.new_material(ASSET + "_Mat")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    uvn = nt.nodes.new("ShaderNodeUVMap"); uvn.uv_map = "UV"
    def img(path, non_color):
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(path, check_existing=True)
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        nt.links.new(uvn.outputs["UV"], n.inputs["Vector"])
        return n
    alb = img(tex["albedo"], False)
    nor = img(tex["normal"], True)
    emi = img(tex["emission"], False)
    msk = img(tex["mask"], True)
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])
    attr = nt.nodes.new("ShaderNodeVertexColor"); attr.layer_name = "Col"
    amul = nt.nodes.new("ShaderNodeMath"); amul.operation = "MULTIPLY"
    nt.links.new(alb.outputs["Alpha"], amul.inputs[0]); nt.links.new(attr.outputs["Alpha"], amul.inputs[1])
    nt.links.new(amul.outputs[0], bsdf.inputs["Alpha"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nm.uv_map = "UV"
    nt.links.new(nor.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    nt.links.new(emi.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = EMISSION_STRENGTH
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(msk.outputs["Color"], sep.inputs[0])
    inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    nt.links.new(sep.outputs[1], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    bsdf.inputs["Specular IOR Level"].default_value = 0.3
    for attr_, val in (("surface_render_method", "DITHERED"), ("blend_method", "HASHED")):
        try:
            setattr(mat, attr_, val)
        except (AttributeError, TypeError):
            pass
    return mat


def punch_material():
    """Placeholder for the depth-punch pass (Unity: ColorMask 0, writes far depth). Invisible in the previews."""
    mat = C.new_material(ASSET + "_Punch")
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    nt.links.new(tr.outputs[0], out.inputs["Surface"])
    try:
        mat.surface_render_method = "DITHERED"
    except (AttributeError, TypeError):
        pass
    return mat


# ---------------------------------------------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------------------------------------------

def build(tex):
    mats = [fissure_material(tex), punch_material()]
    full, lod1, mods = [], [], {}
    for spec in MODULES:
        mod, obj = build_module(spec, 0, mats)
        mods[spec["name"]] = mod
        full.append(obj)
        _, o1 = build_module(spec, 1, mats)
        lod1.append(o1)
    folder = os.path.join(C.EXPORTS, ASSET)
    C.export_fbx(os.path.join(folder, ASSET + ".fbx"), full, animated=False)
    C.export_fbx(os.path.join(folder, ASSET + "_LOD1.fbx"), lod1, animated=False)
    report = {
        "asset": ASSET, "round": ROUND,
        "objects": {o.name: C.tri_count(o) for o in full + lod1},
        "modules": {name: module_facts(m) for name, m in mods.items()},
        "join_profile": {"half_columns_t_z": [[c[0], c[1]] for c in COLS], "lip_dip_at_join": 0.35,
                         "full_width_m": 2 * COLS[-1][0], "gap_top_width_m": 2 * COLS[LIP_EDGE][0],
                         "gap_depth_m": GAP_DEPTH, "canonical_hold_m": JOIN},
        "texture_size": TEX, "low_texture_size": LOW_TEX, "u_period_m": U_PERIOD,
        "materials": [ASSET + "_Mat (slot 0, rock)", ASSET + "_Punch (slot 1, depth punch)"],
        "maps": ["Albedo (RGB + A coverage)", "Normal", "Emission", "Mask (R AO, G smoothness, B/A 0)"],
        "uv": {"UV0": "atlas: U metres along / 1 m repeat, V cross-section", "UV1 (Reveal)": "x metres along the "
               "module, y 0..1 along the module", "UV2 (CloseXY)": "Closed minus open, Unity X and Y",
               "UV3 (CloseZ)": "x = Closed minus open, Unity Z"},
        "vertex_color": "R depth 0..1 (0 at ground, 1 at the core floor), G 1 on pebbles, B 1 on hairlines, "
                        "A footprint fade (0 at the skirt edge and the hairline tips)",
        "shape_keys": ["Closed"], "emission_strength": EMISSION_STRENGTH, "unity_scale": "uniform 1.8 (see README)",
        "pivot": "segment start on the path centre line, at ground level; forward = Blender -Y (Unity +Z)",
    }
    C.write_report(ASSET, report)
    C.save_blend(ASSET)
    return full, lod1, mods, mats


# ---------------------------------------------------------------------------------------------------------------
# Preview
# ---------------------------------------------------------------------------------------------------------------

def tiled_material(name, folder, prefix, tile, road_folder=None, road_prefix=None, road_tile=2.0, road_band=None):
    mat = C.new_material(name)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tc = nt.nodes.new("ShaderNodeTexCoord")

    def tex(path, scale, non_color):
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (1 / scale, 1 / scale, 1)
        nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(path, check_existing=True)
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mp.outputs[0], n.inputs["Vector"])
        return n

    ga = tex(os.path.join(folder, prefix + "_Albedo.png"), tile, False)
    gn = tex(os.path.join(folder, prefix + "_Normal.png"), tile, True)
    col, nrm = ga.outputs["Color"], gn.outputs["Color"]
    if road_folder:
        ra = tex(os.path.join(road_folder, road_prefix + "_Albedo.png"), road_tile, False)
        rn = tex(os.path.join(road_folder, road_prefix + "_Normal.png"), road_tile, True)
        sep = nt.nodes.new("ShaderNodeSeparateXYZ")
        nt.links.new(tc.outputs["Object"], sep.inputs[0])
        d = nt.nodes.new("ShaderNodeMath"); d.operation = "SUBTRACT"; d.inputs[1].default_value = road_band[0]
        nt.links.new(sep.outputs[1], d.inputs[0])
        a = nt.nodes.new("ShaderNodeMath"); a.operation = "ABSOLUTE"
        nt.links.new(d.outputs[0], a.inputs[0])
        lt = nt.nodes.new("ShaderNodeMath"); lt.operation = "LESS_THAN"; lt.inputs[1].default_value = road_band[1]
        nt.links.new(a.outputs[0], lt.inputs[0])
        mc = nt.nodes.new("ShaderNodeMix"); mc.data_type = "RGBA"
        nt.links.new(lt.outputs[0], mc.inputs["Factor"])
        nt.links.new(col, mc.inputs[6]); nt.links.new(ra.outputs["Color"], mc.inputs[7])
        mn = nt.nodes.new("ShaderNodeMix"); mn.data_type = "RGBA"
        nt.links.new(lt.outputs[0], mn.inputs["Factor"])
        nt.links.new(nrm, mn.inputs[6]); nt.links.new(rn.outputs["Color"], mn.inputs[7])
        col, nrm = mc.outputs[2], mn.outputs[2]
    nt.links.new(col, bsdf.inputs["Base Color"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(nrm, nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return mat


def place_chain(objs, chain, start_xy, heading_deg, scale):
    """Lays modules end to end like Unity would along a path; returns the placed objects and their world frames."""
    placed = []
    pos = np.array(start_xy, dtype=float)
    heading = math.radians(heading_deg)            # world direction of travel
    frames = []
    for name in chain:
        src = objs[name]
        mod = MODS[name]
        o = C.link(bpy.data.objects.new(f"P_{name}_{len(placed)}", src.data))
        rot = heading + math.pi / 2                  # local forward (-Y, angle -90) onto the heading
        o.location = (pos[0], pos[1], 0.0)
        o.rotation_euler = (0, 0, rot)
        o.scale = (scale, scale, scale)
        placed.append(o)
        frames.append((pos.copy(), heading))
        p, t, n, phi = mod.path.at(mod.L)
        c, s_ = math.cos(rot), math.sin(rot)
        pos = pos + scale * np.array((c * p[0] - s_ * p[1], s_ * p[0] + c * p[1]))
        heading += phi - mod.path.phi0
    return placed, frames


def place_at(objs, name, frame, scale):
    pos, heading = frame
    o = C.link(bpy.data.objects.new(f"P_{name}_b", objs[name].data))
    o.location = (pos[0], pos[1], 0.0)
    o.rotation_euler = (0, 0, heading + math.pi / 2)
    o.scale = (scale, scale, scale)
    return o


def ground_with_holes(placed, folder_env, res=4096):
    """Preview only: the Crystal Plains ground (and a road band) made see-through over the gap openings, standing in
    for the Unity depth punch. The openings (material slot 1) are rendered top-down into a mask the ground reads."""
    bpy.context.view_layer.update()
    bm = bmesh.new()
    for o in placed:
        mesh = o.data
        sk = mesh.shape_keys.key_blocks["Closed"] if mesh.shape_keys else None
        w = sk.value if sk else 0.0
        mw = o.matrix_world
        for poly in mesh.polygons:
            if poly.material_index != 1:
                continue
            ring = []
            for vi in poly.vertices:
                co = mesh.vertices[vi].co.copy()
                if sk:
                    co = co.lerp(sk.data[vi].co, w)
                p = mw @ co
                ring.append(bm.verts.new((p.x, p.y, 0.0)))
            bm.faces.new(ring)
    me = bpy.data.meshes.new("CapMask")
    bm.to_mesh(me)
    bm.free()
    caps = C.link(bpy.data.objects.new("CapMask", me))
    white = C.new_material("CapWhite")
    nt = white.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.0
    out_ = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out_.inputs["Surface"])
    me.materials.append(white)
    xs = [v.co.x for v in me.vertices] or [0.0]
    ys = [v.co.y for v in me.vertices] or [0.0]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    size = max(max(xs) - min(xs), max(ys) - min(ys)) + 2.0
    hidden = [(o, o.hide_render) for o in bpy.data.objects if o != caps]
    for o, _ in hidden:
        o.hide_render = True
    cam_d = bpy.data.cameras.new("MaskCam"); cam_d.type = "ORTHO"; cam_d.ortho_scale = size
    cam = C.link(bpy.data.objects.new("MaskCam", cam_d))
    cam.location = (cx, cy, 20.0)
    scene = bpy.context.scene
    world = scene.world
    scene.world = None
    C.setup_eevee(res, res, samples=1)
    scene.camera = cam
    mask_path = os.path.join(C.preview_dir(), "_gapmask.png")
    scene.render.filepath = mask_path
    bpy.ops.render.render(write_still=True)
    scene.world = world
    for o, h in hidden:
        o.hide_render = h
    bpy.data.objects.remove(cam)
    bpy.data.objects.remove(caps)
    gm = bpy.data.meshes.new("GroundCut")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60)
    bm.to_mesh(gm)
    bm.free()
    ground = C.link(bpy.data.objects.new("GroundCut", gm))
    tiling = os.path.join(folder_env, "Tiling")
    mat = tiled_material("PlainsGround", os.path.join(tiling, "Ground"), "Ground", 3.0,
                         os.path.join(tiling, "Road"), "Road", 2.0, (ROAD_Y, 2.0))
    nt = mat.node_tree
    tc = [n for n in nt.nodes if n.type == "TEX_COORD"][0]
    mp = nt.nodes.new("ShaderNodeVectorMath")
    mp.operation = "MULTIPLY_ADD"
    mp.inputs[1].default_value = (1 / size, 1 / size, 0.0)
    mp.inputs[2].default_value = (0.5 - cx / size, 0.5 - cy / size, 0.0)
    nt.links.new(tc.outputs["Object"], mp.inputs[0])
    mk = nt.nodes.new("ShaderNodeTexImage")
    mk.image = bpy.data.images.load(mask_path, check_existing=False)
    mk.image.colorspace_settings.name = "Non-Color"
    mk.extension = "CLIP"
    nt.links.new(mp.outputs[0], mk.inputs["Vector"])
    inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mk.outputs["Color"], sep.inputs[0])
    nt.links.new(sep.outputs[0], inv.inputs[1])
    nt.links.new(inv.outputs[0], nt.nodes.get("Principled BSDF").inputs["Alpha"])
    try:
        mat.surface_render_method = "DITHERED"
    except (AttributeError, TypeError):
        pass
    gm.materials.append(mat)
    return ground


def import_prop(path, folder, prefix, location, yaw_deg, keep=None, scale=1.0):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    tex = {k.lower(): os.path.join(folder, f"{prefix}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
    mat = C.game_material(prefix + "_Prev", tex, emission_strength=2.0)
    for o in new:
        o.animation_data_clear()
        if o.type == "MESH" and "LOD1" in o.name:
            o.hide_render = True
        if o.parent is None:
            o.location = location
            o.rotation_euler = (o.rotation_euler[0], o.rotation_euler[1], math.radians(yaw_deg))
            o.scale = (scale, scale, scale)
        if o.type == "MESH":
            if keep and not o.name.endswith(keep):
                o.hide_render = True
            o.data.materials.clear()
            o.data.materials.append(mat)
    return new


ROAD_Y = 3.0
CHAIN = ["Fissure_Start", "Fissure_Straight_A", "Fissure_Curve_L", "Fissure_Straight_B", "Fissure_Curve_R",
         "Fissure_Curve_R", "Fissure_Straight_C", "Fissure_Curve_L", "Fissure_Straight_A", "Fissure_Head"]
SCALE = 1.8
MODS = {}


def compose(objs):
    placed, frames = place_chain(objs, CHAIN, (-17.0, -2.0), 8.0, SCALE)
    placed.append(place_at(objs, "Fissure_Branch_L", frames[3], SCALE))
    placed.append(place_at(objs, "Fissure_Branch_R", frames[6], SCALE))
    return placed, frames


def preview(objs, out):
    for o in bpy.data.objects:
        o.hide_render = True
    C.preview_world(color=(0.02, 0.02, 0.035))
    placed, frames = compose(objs)
    ground = ground_with_holes(placed, os.path.join(C.EXPORTS, "Env"))
    tower = import_prop(os.path.join(C.EXPORTS, "Tower_Gun", "Tower_Gun_T2.fbx"), os.path.join(C.EXPORTS, "Tower_Gun"),
                        "Tower_Gun", (-3.0, -2.2, 0), 200, scale=1.4)
    gate = import_prop(os.path.join(C.EXPORTS, "Gate", "Gate_G1.fbx"), os.path.join(C.EXPORTS, "Gate"), "Gate",
                       (-8.0, ROAD_Y + 0.2, 0), 98, keep="G1")
    lights = C.game_lights()
    paths = []
    centre = Vector((0.0, 1.0, 0.0))
    # Play zoom: the game's ortho size 7 on 720 lines.
    cam = C.game_camera(centre, 1280 / C.GAME_PIXELS_PER_UNIT)
    paths.append(C.render(cam, os.path.join(out, "fissure_game.png"), 1280, 720, samples=32))
    bpy.data.objects.remove(cam)
    cam = C.game_camera(centre + Vector((-1.0, 0.0, 0)), 1280 / C.GAME_PIXELS_PER_UNIT / 0.62)
    paths.append(C.render(cam, os.path.join(out, "fissure_overview.png"), 1280, 720, samples=24))
    bpy.data.objects.remove(cam)
    cam = C.game_camera(Vector((-6.0, -0.5, 0.0)), 1280 / C.GAME_PIXELS_PER_UNIT / 2.2)
    paths.append(C.render(cam, os.path.join(out, "fissure_game_zoom.png"), 1280, 720, samples=32))
    bpy.data.objects.remove(cam)
    cam = C.closeup_camera(Vector((-7.5, -0.6, -0.1)), 5.5, azimuth=-20, elevation=30, lens=40)
    paths.append(C.render(cam, os.path.join(out, "fissure_close.png"), 1280, 720, samples=48))
    bpy.data.objects.remove(cam)
    # Reveal: the front at three places (open behind it, Closed ramps over one module ahead of it).
    keys = [o.data.shape_keys.key_blocks["Closed"] for o in placed]
    shots = []
    for i, front in enumerate((3.0, 6.0, 9.0)):
        for k, o in enumerate(placed[:len(CHAIN)]):
            o.data = o.data.copy() if o.data.users > 1 else o.data
        for k, o in enumerate(placed[:len(CHAIN)]):
            o.data.shape_keys.key_blocks["Closed"].value = float(np.clip(k + 1 - front, 0, 1))
        for o in placed[len(CHAIN):]:
            o.data = o.data.copy() if o.data.users > 1 else o.data
            o.data.shape_keys.key_blocks["Closed"].value = 1.0 if front < 9 else 0.0
        bpy.data.objects.remove(ground)
        ground = ground_with_holes(placed, os.path.join(C.EXPORTS, "Env"))
        cam = C.game_camera(centre + Vector((-1.0, 0.0, 0)), 1280 / C.GAME_PIXELS_PER_UNIT / 0.62)
        shots.append(C.render(cam, os.path.join(out, f"_reveal_{i}.png"), 1280, 720, samples=16))
        bpy.data.objects.remove(cam)
    C.contact_sheet(shots, os.path.join(out, "fissure_reveal.png"), 3, 640)
    for p in shots:
        os.remove(p)
    del keys, tower, gate, lights
    return paths


def module_sheet(objs, out):
    """Each module alone on a plain ground, top-ish view, with its name order left to right."""
    for o in bpy.data.objects:
        o.hide_render = True
    C.preview_world(color=(0.02, 0.02, 0.035))
    placed = []
    for i, spec in enumerate(MODULES):
        o = C.link(bpy.data.objects.new("S_" + spec["name"], objs[spec["name"]].data))
        o.location = ((i - 4) * 2.2, 1.5, 0)
        placed.append(o)
    ground = ground_with_holes(placed, os.path.join(C.EXPORTS, "Env"))
    lights = C.game_lights()
    cam = C.game_camera(Vector((0, 0.2, 0)), 21.0, yaw=0.0)
    path = C.render(cam, os.path.join(out, "fissure_modules.png"), 1600, 620, samples=24)
    bpy.data.objects.remove(cam)
    for o in placed + [ground] + lights:
        bpy.data.objects.remove(o)
    return path


def main():
    args = C.cli_args()
    out = C.preview_dir()
    C.reset_scene()
    folder = os.path.join(C.EXPORTS, ASSET)
    tex = textures_on_disk(folder) if args.get("notex") and os.path.exists(textures_on_disk(folder)["albedo"]) \
        else write_textures(folder)
    full, lod1, mods, mats = build(tex)
    MODS.update(mods)
    if args.get("norender"):
        return
    objs = {o.name: o for o in full}
    module_sheet(objs, out)
    preview(objs, out)


if __name__ == "__main__":
    main()
