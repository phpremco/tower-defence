"""Round 38: Gravemaw's toothed maw (Remco: "cool alien-like teeth in the mouth, multiple rows like a worm with teeth;
it looks quite basic when you see it so big").

The round-14 maw was five jaw petals round a glowing plug with ten small Crysium teeth. This adds a worm gullet behind
the petals: a deep, ribbed throat that narrows into the head with a hot core at the bottom, lined with five rows of
hooked, inward- and backward-pointing teeth (lamprey / sandworm): two rows on the inner face of every jaw petal (they
spread with the petals when the maw opens) and three concentric rings down the throat (the lip ring flexes a little
with the nearest petals, the deeper rings sit on the head). The teeth are bone-white at the tip, amber and translucent
towards the root (a faint warm glow there, as if the core shines through), wet but streaked and cracked.

The maw is its own skinned mesh `Gravemaw_Maw` with its own material `Gravemaw_Maw_Mat` (Gravemaw_Maw_*.png, 1024,
Low 512) in Gravemaw.fbx, and `Gravemaw_Maw_LOD1` in Gravemaw_LOD1.fbx: Unity dresses it as an extra part
(BlenderArtEntry.ExtraParts "Maw"). The body loses the old glowing plug and its crystal teeth (faces cut out of the
existing game mesh and LOD, UVs and weights untouched), so the throat opens into the head. No bone, clip or event
changes: the existing actions are exported again as they are.

Run after build_gravemaw_mech.py (which calls attach() itself on a rerun):
  blender -b --factory-startup --python Tools/Blender/gravemaw_maw.py -- [--before | --qa | --blockout] [--norender]
  --before: only render the QA close-ups of the current export (ArtSource/Previews/QA/gravemaw_maw_before_*.png)
  --qa: only render the after close-ups from the saved .blend; --blockout: raw parts, nothing saved
NOTE: run it on the round-37 .blend and body maps (build_gravemaw.py + build_gravemaw_mech.py output); the body cut is
idempotent and the petal recolour is guarded by the report key `petal_insides_recoloured`.
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_gravemaw as BG  # noqa: E402
import common as C  # noqa: E402
import deep_kit as DK  # noqa: E402

NAME = BG.NAME
MAW = NAME + "_Maw"
MAW_LOD = MAW + "_LOD1"
MAW_TRIS, MAW_LOD_TRIS = 4200, 1700
MAW_DECIMATE = 4800         # before the hidden outside of the throat is pruned (lands near MAW_TRIS)
TEETH_TEXEL = 2.4
TEX, LOW_TEX = 1024, 512
# Unity gives an extra part the second slot's detail block (crystal on B, rock on A).
FAMILIES = BG.CRYSTAL_FAMILIES
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")

A, UP, RIGHT = BG.maw_frame()
BASE = BG.MAW_BASE
PETAL_ANGLES = {name: math.radians(ang) for name, ang in BG.PETALS}

# The body's round-14 plug and the faces round it, cut out so the throat opens into the head: Head-weighted body faces
# within CUT_R of the maw axis between CUT_D (depth behind the petal base, metres; negative is in front of it).
CUT_R = 0.335
CUT_D = (-0.34, 0.2)

# Throat profile (depth behind the petal base -> lumen and shell radius). The shell is thick at the lip so it closes
# the cut in the body all round; the lumen narrows to the core.
THROAT_D = [-0.14, -0.08, 0.0, 0.08, 0.17, 0.26, 0.36, 0.46, 0.54, 0.62]
THROAT_IN = [0.31, 0.3, 0.285, 0.26, 0.23, 0.195, 0.155, 0.11, 0.07, 0.035]
THROAT_OUT = [0.37, 0.4, 0.41, 0.4, 0.37, 0.33, 0.27, 0.21, 0.14, 0.05]
FOLDS = [-0.085, 0.085, 0.245, 0.4]      # peristaltic ring folds between the tooth rows
CORE_D = 0.55

# Throat rows: (name, depth, count, angle offset, length, base radius, core glow on the tooth). The lip ring sits a
# little behind the petal base, so the petal fangs (whose tips reach d -0.1 when the maw is shut) stay clear of it.
RINGS = [("ToothLip", 0.0, 15, 0.0, 0.17, 0.034, 0.3),
         ("ToothRing", 0.165, 12, math.pi / 12, 0.13, 0.028, 0.5),
         ("ToothDeep", 0.315, 9, 0.0, 0.095, 0.022, 0.75)]
# Petal rows: (name, v along the petal, u across it, length, base radius).
PETAL_ROWS = [("PetalFang", 0.3, (0.24, 0.5, 0.76), 0.16, 0.034),
              ("PetalHook", 0.54, (0.33, 0.67), 0.115, 0.026)]
LIP_FLEX = 0.38   # share of the lip that follows the nearest petals (the rows spread a little as the maw opens)

FLESH = np.array((0.15, 0.035, 0.05))
FLESH_DEEP = np.array((0.32, 0.06, 0.11))
FOLD_CREST = np.array((0.5, 0.2, 0.22))
GUM = np.array((0.42, 0.1, 0.15))
GLOW = np.array(BG.PAL.glow)
HOT = np.array((1.0, 0.8, 0.68))
T_ROOT = np.array((0.34, 0.15, 0.06))
T_AMBER = np.array((0.8, 0.55, 0.26))
T_BONE = np.array((0.93, 0.88, 0.77))
T_TIP = np.array((0.97, 0.95, 0.9))
T_STAIN = np.array((0.28, 0.05, 0.06))
T_GLOW = np.array((1.0, 0.62, 0.38))


def radial(phi):
    return UP * math.cos(phi) + RIGHT * math.sin(phi)


def at(d, r, phi):
    """Point `d` behind the petal base (along -A) at radius `r` and angle `phi` (0 up, positive to the right)."""
    return BASE - A * d + radial(phi) * r


def frame_coords(p):
    """(depth, radius, angle, radial unit) of points p (n, 3) in the maw frame."""
    a, up, right, base = (np.array(v) for v in (A, UP, RIGHT, BASE))
    rel = p - base
    d = -(rel @ a)
    rad = rel + np.outer(d, a)
    r = np.linalg.norm(rad, axis=1)
    phi = np.arctan2(rad @ right, rad @ up)
    return d, r, phi, rad / np.maximum(r, 1e-6)[:, None]


def lumen_radius(d, phi=0.0):
    r = float(np.interp(d, THROAT_D, THROAT_IN))
    fold = sum(math.exp(-((d - f) / 0.03) ** 2) for f in FOLDS)
    return r - 0.022 * fold * (r / 0.3) + 0.006 * math.cos(18 * phi)


def hash01(*xs):
    """Deterministic 0..1 from numbers (symmetric callers pass |angle| so both sides match)."""
    h = 0.0
    for i, x in enumerate(xs):
        h += math.sin(x * (12.9898 + 7.1 * i) + 78.233 * (i + 1)) * 43758.5453
    return h - math.floor(h)


# ------------------------------------------------------------------------------------------------------------
# Styles
# ------------------------------------------------------------------------------------------------------------

def throat_style(seed=0):
    """Wet, dark maroon gullet, ribbed by ring folds with fine grooves along it; the light rises with depth into a
    pink glow and a white-hot core. Only the lumen glows; the hidden outside of the shell stays dark."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        d, r, phi, ru = frame_coords(p)
        # Everything but the outside of the shell deep in the head (facing away from the axis) is gullet; at the front
        # the outside is the gum that shows between the petal bases when the maw opens.
        lumen = 1 - C.smoothstep(0.3, 0.7, np.einsum("ij,ij->i", n, ru)) * C.smoothstep(0.0, 0.08, d)
        gum = 1 - C.smoothstep(-0.11, -0.03, d)
        depth = C.smoothstep(-0.08, 0.5, d)
        folds = np.zeros(len(idx))
        for f in FOLDS:
            folds = np.maximum(folds, np.exp(-((d - f) / 0.03) ** 2))
        grooves = 0.5 + 0.5 * np.cos(18 * phi + 2.5 * C.value_noise(p, 8, seed))
        mott = 0.5 + 0.5 * C.value_noise(p, 28, seed + 1)
        col = C.lerp3(FLESH, FLESH_DEEP, depth) * (0.72 + 0.4 * mott)[:, None]
        col = C.lerp3(col, FLESH * 0.4, (1 - folds) * grooves * 0.45)
        col = C.lerp3(col, FOLD_CREST, folds * 0.35 * (1 - depth))
        col = C.lerp3(col, GUM * (0.75 + 0.4 * mott)[:, None], gum * 0.85)
        core = C.smoothstep(0.38, 0.58, d)
        glow = C.lerp3(GLOW, HOT, core)
        amount = lumen * (0.05 + 1.15 * depth ** 1.5 + 0.3 * (1 - folds) * grooves * depth + 0.6 * core + 0.1 * gum)
        s.albedo[idx] = C.lerp3(col * (0.4 + 0.6 * lumen)[:, None], glow * 0.7, (0.3 * depth + 0.5 * core) * lumen)
        s.emit[idx] = np.clip(glow * amount[:, None], 0, 1)
        s.rough[idx] = 0.2 + 0.2 * (1 - mott) + 0.12 * folds + 0.3 * (1 - lumen)
        s.disp[idx] = lumen * (0.003 * folds - 0.0025 * grooves * (1 - folds))
        s.fam[idx] = (0.0, 0.0, 0.0, 0.0)
        s.flow[idx] = d
    return style


def core_style():
    def style(s, idx, part):
        s.albedo[idx] = HOT * 0.85
        s.emit[idx] = HOT
        s.rough[idx] = 0.3
        s.disp[idx] = 0.0
        s.fam[idx] = (0.0, 0.0, 0.0, 0.0)
    return style


def tooth_style(glow, seed=0):
    """Hooked tooth: amber, translucent-looking root (a warm glow band), bone body and a pale worn tip; striations
    along it, growth bands across, dark fracture lines and a flesh stain at the root, a wet but streaky sheen."""
    def style(s, idx, part):
        p = s.p[idx]
        z, radius, dist = C.tube_param(part, p)
        L = C.local_points(part, p)
        striae = 0.5 + 0.5 * C.value_noise(np.c_[L[:, 0] * 3, L[:, 1] * 3, z * 0.5], 6, seed)
        bands = 1 - C.smoothstep(0.0, 0.05, np.abs(((z * 7) % 1.0) - 0.5) - 0.42)
        col = C.lerp3(C.lerp3(T_ROOT, T_AMBER, C.smoothstep(0.0, 0.16, z)), T_BONE, C.smoothstep(0.14, 0.45, z))
        col = C.lerp3(col, T_TIP, C.smoothstep(0.6, 0.95, z))
        col = col * (0.86 + 0.18 * striae)[:, None] * (1 - 0.1 * bands)[:, None]
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 22, seed + 3)))
        crack *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 5, seed + 4))
        col = C.lerp3(col, T_ROOT * 0.5, crack * 0.55)
        stain = (1 - C.smoothstep(0.0, 0.1, z)) * (0.6 + 0.4 * striae)
        col = C.lerp3(col, T_STAIN, stain * 0.65)
        root_band = C.smoothstep(0.02, 0.1, z) * (1 - C.smoothstep(0.18, 0.4, z))
        s.albedo[idx] = col
        s.emit[idx] = np.clip(T_GLOW * (glow * (0.04 + 0.4 * root_band) * (1 - 0.7 * crack))[:, None], 0, 1)
        # Wet enamel, not glass or chrome: a sheen on the ridges, duller in the striae, cracks and the worn tip.
        s.rough[idx] = 0.32 + 0.2 * striae + 0.18 * crack + 0.12 * C.smoothstep(0.85, 1.0, z)
        s.disp[idx] = -0.0015 * crack - 0.0008 * bands
        s.fam[idx] = (0.0, 0.0, 0.55, 0.0)
        s.flow[idx] = z * C.axis_length(part)
    return style


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def tooth_profile(t):
    """A thick-rooted cone that tapers fast into a needle tip (not a plain cone)."""
    return (1 - t) ** 1.15 * (1.0 + 0.3 * math.exp(-((t - 0.1) / 0.12) ** 2))


def add_tooth(b, name, base, direction, hook, radius, length, bone, glow, seed, info):
    part = b.shard(name, base, direction, radius, length, sides=7, flute=0.14, profile=tooth_profile,
                   bend=hook * length * 0.32, rings=10, bone=bone, style=tooth_style(glow, seed))
    part.maw = info
    return part


def maw_parts():
    b = C.PartBuilder()
    nu, nv = 56, 36

    def shell(radii):
        def fn(u, v):
            d = THROAT_D[0] + (THROAT_D[-1] - THROAT_D[0]) * v
            phi = 2 * math.pi * u
            r = lumen_radius(d, phi) if radii is THROAT_IN else float(np.interp(d, THROAT_D, radii))
            return at(d, r, phi)
        return fn
    throat = b.slab("Throat", shell(THROAT_OUT), shell(THROAT_IN), nu, nv, bone="Head", style=throat_style(3))
    throat.maw = ("flesh", None)
    core = b.ellipsoid("Core", at(CORE_D, 0.0, 0.0), (0.095, 0.095, 0.095), bone="Head", segments=(20, 10),
                       style=core_style())
    core.maw = ("flesh", None)
    deeper = -A
    for name, d, count, offset, length, radius, glow in RINGS:
        for k in range(count):
            phi = offset + 2 * math.pi * k / count
            phi = math.atan2(math.sin(phi), math.cos(phi))
            j = hash01(round(abs(phi), 4), d)
            ln = length * (0.88 + 0.24 * j)
            inward = -radial(phi)
            direction = (inward * 0.6 + deeper * 0.8).normalized()
            base = at(d, lumen_radius(d, phi) + 0.018, phi)
            add_tooth(b, name, base, direction, deeper, radius * (0.92 + 0.16 * j), ln, "Head", glow,
                      500 + k, ("ring", (d, phi)))
    a_len = (BG.MAW_TIP - BG.MAW_BASE).length
    for pname, ang in BG.PETALS:
        for name, v, us, length, radius in PETAL_ROWS:
            for u in us:
                inner = Vector(BG.petal_point(ang, u, v, inner=True))
                axis_pt = BASE + A * (a_len * v)
                c = (inner - axis_pt - A * (inner - axis_pt).dot(A)).normalized()
                base = inner + c * 0.012
                direction = (-c * 0.82 + deeper * 0.57).normalized()
                j = hash01(abs(u - 0.5), v, abs(ang))
                add_tooth(b, name, base, direction, deeper, radius * (0.92 + 0.16 * j), length * (0.9 + 0.2 * j),
                          pname, 0.25 if v < 0.4 else 0.18, 600 + int(u * 10), ("petal", pname))
    return b


def clearance(b):
    """Smallest gap between a petal tooth and a throat tooth or the throat at rest (closed maw). The voxel remesh
    fuses anything closer than a couple of voxels, and a petal tooth fused to the head would tear when the maw opens."""
    petal = [p for p in b.parts if getattr(p, "maw", (None,))[0] == "petal"]
    other = [p for p in b.parts if getattr(p, "maw", (None,))[0] == "ring"]
    worst = (9.0, None)
    for p in petal:
        for q in other:
            for i, a in enumerate(p.axis):
                for k, c in enumerate(q.axis):
                    gap = np.linalg.norm(a - c) - p.radii[i] - q.radii[k]
                    if gap < worst[0]:
                        worst = (gap, (p.name, q.name))
        # Against the throat shell: in front of the lip the gap is the depth to it, behind it the room to the lumen
        # wall (an axis point inside the wall gives a negative gap).
        d, r, phi, _ = frame_coords(np.array(p.axis))
        d0 = THROAT_D[0]
        for di, ri, ph, rad in zip(d, r, phi, p.radii):
            if di < d0:
                if THROAT_IN[0] - rad < ri < THROAT_OUT[0] + rad:
                    worst = min(worst, (d0 - di - rad, (p.name, "ThroatLip")))
            elif ri < float(np.interp(di, THROAT_D, THROAT_OUT)):
                worst = min(worst, (lumen_radius(di, ph) - ri - rad, (p.name, "ThroatWall")))
    return worst


# ------------------------------------------------------------------------------------------------------------
# Weights
# ------------------------------------------------------------------------------------------------------------

def petal_pair(phi):
    """The two petals either side of angle `phi`, with linear weights by angle."""
    angs = sorted(PETAL_ANGLES.items(), key=lambda kv: kv[1])
    for i in range(len(angs)):
        n0, a0 = angs[i]
        n1, a1 = angs[(i + 1) % len(angs)]
        span = (a1 - a0) % (2 * math.pi)
        off = (phi - a0) % (2 * math.pi)
        if off <= span:
            t = off / span
            return [(n0, 1 - t), (n1, t)]
    return [(angs[0][0], 1.0)]


def lip_share(d):
    return LIP_FLEX * (1 - float(C.smoothstep(-0.12, 0.24, np.array([d]))[0]))


def skin_maw(obj, arm, builder):
    """Petal teeth follow their petal rigidly; the throat and its tooth rings follow the head, and near the lip a share
    of the nearest two petals so the rings spread a little as the maw opens (a tooth takes the weight of its root)."""
    pts, _ = C.mesh_arrays(obj.data)
    pid = C.assign_parts(pts, builder)
    for vg in list(obj.vertex_groups):
        obj.vertex_groups.remove(vg)
    names = ["Head"] + [n for n, _ in BG.PETALS]
    groups = {n: obj.vertex_groups.new(name=n) for n in names}
    d, r, phi, _ = frame_coords(pts)
    for i in range(len(pts)):
        part = builder.parts[pid[i]]
        kind, info = getattr(part, "maw", ("flesh", None))
        if kind == "petal":
            weights = {info: 1.0}
        else:
            dd, ph = info if kind == "ring" else (d[i], phi[i])
            share = lip_share(dd)
            weights = {"Head": 1.0 - share}
            for n, w in petal_pair(ph):
                weights[n] = weights.get(n, 0.0) + share * w
        for n, w in weights.items():
            if w > 0.005:
                groups[n].add([i], float(w), "REPLACE")
    for m in list(obj.modifiers):
        if m.type == "ARMATURE":
            obj.modifiers.remove(m)
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    obj.matrix_parent_inverse.identity()


# ------------------------------------------------------------------------------------------------------------
# Body cut
# ------------------------------------------------------------------------------------------------------------

def cut_body(obj):
    """Removes the round-14 plug (Head-weighted body faces round the maw axis) and the crystal teeth (crystal islands
    on the petals, and the Head ring round the plug) from a joined body + crystal mesh. Idempotent. Returns the
    number of triangles removed."""
    before = C.tri_count(obj)
    me = obj.data
    names = {vg.index: vg.name for vg in obj.vertex_groups}
    dom = []
    for v in me.vertices:
        best = max(v.groups, key=lambda g: g.weight, default=None)
        dom.append(names.get(best.group, "-") if best else "-")
    co = np.array([tuple(v.co) for v in me.vertices])
    d, r, phi, _ = frame_coords(co)
    jaw = np.array([n.startswith("Jaw_") for n in dom])
    head = np.array([n == "Head" for n in dom])
    # The plug (Head faces round the axis), and anything of the body inside the throat further down (the belly
    # tube's front cap reaches into it); the hole edges then lie inside the throat wall.
    lumen = np.array([lumen_radius(float(x), float(y)) for x, y in zip(d, phi)])
    plug = (r < CUT_R) & (d > CUT_D[0]) & (d < CUT_D[1]) & head
    inside = (r < lumen + 0.03) & (d >= 0.0) & (d < 0.75) & ~jaw
    region = plug | inside
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    doomed = set()
    crystal = [f for f in bm.faces if f.material_index == 1]
    for f in bm.faces:
        if f.material_index != 0:
            continue
        if all(region[v.index] for v in f.verts):
            doomed.add(f.index)
    # Crystal islands (connected through shared vertices).
    seen = set()
    for f in crystal:
        if f.index in seen:
            continue
        stack, island = [f], []
        seen.add(f.index)
        while stack:
            g = stack.pop()
            island.append(g)
            for v in g.verts:
                for h in v.link_faces:
                    if h.material_index == 1 and h.index not in seen:
                        seen.add(h.index)
                        stack.append(h)
        verts = list({v.index for g in island for v in g.verts})
        jaw_share = float(np.mean(jaw[verts]))
        centre = co[verts].mean(axis=0)
        cd, cr, _, _ = frame_coords(centre[None, :])
        ring = float(np.mean(head[verts])) > 0.5 and cr[0] < 0.34 and -0.3 < cd[0] < 0.1
        if jaw_share > 0.5 or ring:
            doomed.update(g.index for g in island)
    bmesh.ops.delete(bm, geom=[bm.faces[i] for i in sorted(doomed)], context="FACES")
    bm.to_mesh(me)
    bm.free()
    me.update()
    removed = before - C.tri_count(obj)
    C.log(obj.name, "cut", len(doomed), "faces,", removed, "tris")
    return removed


def cut_posed(obj, arm, poses):
    """Body faces that swing into the throat when the maw opens: the petal heels are fused to the head and blended
    between Head and Jaw weights, so wide open they stretch across the mouth as a membrane (the old plug hid it).
    For each (action, frame) the posed faces are taken back into head space and any face whose centre lies inside
    the lumen is cut. At rest those faces sit inside the throat's lip, so nothing opens up there."""
    before = C.tri_count(obj)
    doomed = set()
    for act, frame in poses:
        if act not in bpy.data.actions:
            continue
        C.set_action(arm, bpy.data.actions[act], frame)
        bpy.context.view_layer.update()
        inv = head_space(arm).inverted()
        dg = bpy.context.evaluated_depsgraph_get()
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        world = obj.matrix_world
        for p in me.polygons:
            c = inv @ (world @ p.center)
            d, r, phi, _ = frame_coords(np.array(tuple(c))[None, :])
            if THROAT_D[0] < d[0] < 0.75 and r[0] < lumen_radius(float(d[0]), float(phi[0])) - 0.005:
                doomed.add(p.index)
        ev.to_mesh_clear()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[i] for i in sorted(doomed)], context="FACES")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    removed = before - C.tri_count(obj)
    C.log(obj.name, "posed cut", len(doomed), "faces,", removed, "tris")
    return removed


# ------------------------------------------------------------------------------------------------------------
# Petal insides (body maps)
# ------------------------------------------------------------------------------------------------------------

PETAL_FLESH_DARK = np.array((0.16, 0.03, 0.06))    # stored sRGB values, like the PNGs
PETAL_FLESH = np.array((0.4, 0.07, 0.13))
RECOLOUR_KEY = "petal_insides_recoloured"


def petal_inside_tris(obj):
    """UV triangles (with their position along the petal) of the body faces that form the inside of the jaw petals:
    Jaw-weighted, material 0, facing the maw axis."""
    me = obj.data
    me.calc_loop_triangles()
    names = {vg.index: vg.name for vg in obj.vertex_groups}
    dom = []
    for v in me.vertices:
        best = max(v.groups, key=lambda g: g.weight, default=None)
        dom.append(names.get(best.group, "-") if best else "-")
    uv = me.uv_layers.active.data
    length = (BG.MAW_TIP - BG.MAW_BASE).length
    inside, other = [], []
    for lt in me.loop_triangles:
        if lt.material_index != 0:
            continue
        uvs = np.array([tuple(uv[i].uv) for i in lt.loops])
        if not all(dom[v].startswith("Jaw_") for v in lt.vertices):
            other.append(uvs)
            continue
        co = np.array([tuple(me.vertices[v].co) for v in lt.vertices])
        d, r, phi, ru = frame_coords(co.mean(axis=0)[None, :])
        if float(np.dot(np.array(tuple(lt.normal)), ru[0])) > -0.35:
            other.append(uvs)
            continue
        dv, _, _, _ = frame_coords(co)
        inside.append((uvs, np.clip(-dv / length, 0, 1)))
    return inside, other


def raster(tris, size, values=False):
    """Coverage mask (and the interpolated per-vertex value) of UV triangles on a size x size map (row 0 = v 0)."""
    mask = np.zeros((size, size), dtype=np.float32)
    val = np.zeros((size, size), dtype=np.float32)
    for item in tris:
        uvs, vv = item if values else (item, None)
        p = uvs * size - 0.5
        x0, y0 = np.floor(p.min(axis=0)).astype(int)
        x1, y1 = np.ceil(p.max(axis=0)).astype(int)
        x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, size - 1), min(y1, size - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        (ax, ay), (bx, by), (cx, cy) = p
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12:
            continue
        w0 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / den
        w1 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / den
        w2 = 1 - w0 - w1
        hit = (w0 >= -0.02) & (w1 >= -0.02) & (w2 >= -0.02)
        mask[ys[hit], xs[hit]] = 1.0
        if values:
            val[ys[hit], xs[hit]] = (w0 * vv[0] + w1 * vv[1] + w2 * vv[2])[hit]
    return mask, val


def grow(mask, val, free, steps):
    """Dilates mask (and its value) by `steps` texels into texels no other face uses (the bake margin)."""
    for _ in range(steps):
        m2, v2 = mask.copy(), val.copy()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sm = np.roll(np.roll(mask, dy, 0), dx, 1)
            sv = np.roll(np.roll(val, dy, 0), dx, 1)
            take = (sm > m2) & free
            m2[take], v2[take] = sm[take], sv[take]
        mask, val = m2, v2
    return mask, val


def recolour_petal_insides(obj, folder, report):
    """The petals' insides were candy pink with a flat glow (round 14), which made the teeth read as decals. They
    become dark wet flesh in the body maps (Gravemaw_Albedo/Emission/Mask and the Low set): the old pattern stays as
    shading, the glow keeps a third of its strength and rises towards the throat, the surface turns wet. Once per
    body bake: build_gravemaw.py writes a fresh report without the marker."""
    if report.get(RECOLOUR_KEY):
        C.log("petal insides already recoloured")
        return False
    inside, other = petal_inside_tris(obj)
    for sub, size in (("", TEX), ("Low", LOW_TEX)):
        mask, v = raster(inside, size, values=True)
        used, _ = raster(other, size)
        mask, v = grow(mask, v, used < 0.5, max(2, size // 256))
        m = mask[..., None]
        hot = (1 - C.smoothstep(0.05, 0.55, v))[..., None]
        base = os.path.join(folder, sub, NAME)
        alb = C.image_to_array(base + "_Albedo.png")
        lum = alb[..., :3].mean(axis=2, keepdims=True)
        flesh = PETAL_FLESH_DARK + (PETAL_FLESH - PETAL_FLESH_DARK) * np.clip(lum * 1.6, 0, 1)
        flesh = flesh * (0.85 + 0.3 * hot)
        alb[..., :3] = alb[..., :3] * (1 - 0.85 * m) + flesh * 0.85 * m
        C.array_to_image(alb, base + "_Albedo.png", "sRGB")
        emi = C.image_to_array(base + "_Emission.png")
        keep = 0.18 + 0.32 * hot
        emi[..., :3] = emi[..., :3] * (1 - m + m * keep)
        C.array_to_image(emi, base + "_Emission.png", "sRGB")
        msk = C.image_to_array(base + "_Mask.png")
        msk[..., 1] = msk[..., 1] * (1 - 0.6 * mask) + 0.6 * 0.66 * mask
        C.array_to_image(msk, base + "_Mask.png", "Non-Color")
        C.log("recoloured petal insides", base, int(mask.sum()), "texels")
    for img in bpy.data.images:
        if img.filepath and os.path.basename(img.filepath).startswith(NAME + "_") and "Maw" not in img.filepath:
            img.reload()
    report[RECOLOUR_KEY] = True
    return True


# The widest-open frames of the clips (jaw 42-58 degrees): Roar, Emerge, Dive, Surface_Throw, Surface_Burst, Death.
OPEN_POSES = [("Roar", 30), ("Roar", 40), ("Emerge", 33), ("Dive", 16), ("Surface_Throw", 24), ("Surface_Burst", 18),
              ("Death", 20)]


# ------------------------------------------------------------------------------------------------------------
# Build, attach, export
# ------------------------------------------------------------------------------------------------------------

def prune_hidden(obj):
    """Deletes the outside of the throat shell deep in the head (always inside the body), so its tris and atlas
    space go to the teeth. The lip's outside stays: it shows as gums between the petal bases when the maw opens."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    doomed = []
    for f in bm.faces:
        c = np.array(tuple(f.calc_center_median()))[None, :]
        d, r, phi, ru = frame_coords(c)
        outward = float(np.dot(np.array(tuple(f.normal)), ru[0]))
        if d[0] > 0.06 and outward > 0.3 and r[0] > lumen_radius(float(d[0]), float(phi[0])) + 0.025:
            doomed.append(f)
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    # Sliver faces left by the decimation get blown up by the minimum-stretch unwrap (two of them took half the
    # atlas): collapse them first.
    slivers = len(bm.faces)
    bmesh.ops.dissolve_degenerate(bm, dist=0.003, edges=bm.edges[:])
    slivers -= len(bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    C.log(obj.name, "pruned", len(doomed), "hidden faces,", slivers, "slivers")


def island_scale(builder, bm, island, face_pid):
    """Teeth get 2.4x the texel density of the gullet (they are the hero detail; the flesh is mostly glow)."""
    names = [builder.parts[face_pid[fi]].name for fi in island]
    teeth = sum(1 for n in names if n.startswith(("Tooth", "Petal")))
    return TEETH_TEXEL if teeth * 2 > len(names) else 1.0


def build(arm, lod_arm, folder):
    """Bakes the maw (Gravemaw_Maw_*.png, Low set) and returns (maw, maw_lod, facts)."""
    for n in (MAW, MAW_LOD, MAW + "_High"):
        o = bpy.data.objects.get(n)
        if o is not None:
            bpy.data.objects.remove(o)
    b = maw_parts()
    gap = clearance(b)
    C.log("maw clearance", gap)
    asset = DK.build_mesh_asset_dk(MAW, b, voxel=0.0042, target_tris=MAW_DECIMATE, tex_size=TEX, ao_distance=0.12,
                                   extrusion=0.02, ray=0.06, emission_strength=2.5, specular=0.5, k=BG.K * 0.35,
                                   families=FAMILIES, low_size=LOW_TEX, out_dir=folder, smooth_iterations=2,
                                   prune=prune_hidden, island_scale=lambda bm, isl, fp: island_scale(b, bm, isl, fp))
    bpy.data.objects.remove(bpy.data.objects[MAW + "_High"])
    maw = asset.low
    skin_maw(maw, arm, b)
    lod_mesh = C.decimate(maw.data.copy(), MAW_LOD_TRIS, MAW_LOD, symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(MAW_LOD, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(maw.data.materials[0])
    skin_maw(lod, lod_arm, b)
    facts = {"object": MAW, "material": MAW + "_Mat", "maps": f"{MAW}_*.png ({TEX}, Low {LOW_TEX})",
             "tris": C.tri_count(maw), "lod1_object": MAW_LOD, "lod1_tris": C.tri_count(lod),
             "source_tris": asset.high_tris, "uv": asset.uv_stats, "detail": asset.details,
             "rows": {"petal": [f"{n}: {len(us)} per petal at v {v}" for n, v, us, _, _ in PETAL_ROWS],
                      "throat": [f"{n}: {c} at depth {d} m" for n, d, c, *_ in RINGS]},
             "teeth": sum(len(us) for _, _, us, _, _ in PETAL_ROWS) * len(BG.PETALS) + sum(r[2] for r in RINGS),
             "bones": ["Head"] + [n for n, _ in BG.PETALS],
             "petal_clearance_m": round(float(gap[0]), 4)}
    return maw, lod, facts


def cut_all(arm, mesh, lod_arm, lod_mesh):
    """Both body meshes: the plug and crystal teeth, then whatever swings into the throat in the open poses."""
    cut = {mesh.name: cut_body(mesh) + cut_posed(mesh, arm, OPEN_POSES)}
    if lod_mesh is not None:
        cut[lod_mesh.name] = cut_body(lod_mesh) + cut_posed(lod_mesh, lod_arm, [(a + "_LOD", f) for a, f in OPEN_POSES])
    return cut


def attach(boss):
    """build_gravemaw_mech hook: cut the body, (re)build the maw and export it with the boss."""
    folder = os.path.join(C.EXPORTS, NAME)
    cut = cut_all(boss.arm, boss.mesh, boss.lod_arm, boss.lod_mesh)
    recoloured = recolour_petal_insides(boss.mesh, folder, load_report())
    maw, lod, facts = build(boss.arm, boss.lod_arm, folder)
    boss.extras.append(maw)
    boss.lod_extras.append(lod)
    return dict(facts, body_cut_tris=cut, petal_insides=recoloured or load_report().get(RECOLOUR_KEY, False))


def round28_clips():
    with open(os.path.join(C.EXPORTS, NAME, NAME + "_report.json"), encoding="utf-8") as fh:
        return list(json.load(fh).get("round28", {}).get("clips", {}))


def export(arm, mesh, maw, lod_arm, lod_mesh, maw_lod):
    """Gravemaw.fbx / _LOD1.fbx again: same rig and actions (original round-14 clips, then the round-28 ones)."""
    folder = os.path.join(C.EXPORTS, NAME)
    clips = list(BG.CLIPS) + [c for c in round28_clips() if c not in BG.CLIPS]
    full = {n: bpy.data.actions[n] for n in clips}
    lod = {n: bpy.data.actions[n + "_LOD"] for n in clips if (n + "_LOD") in bpy.data.actions}
    M.export_rig(os.path.join(folder, NAME + ".fbx"), arm, [mesh, maw], full)
    M.export_rig(os.path.join(folder, NAME + "_LOD1.fbx"), lod_arm, [lod_mesh, maw_lod], lod)
    return clips


def load_report():
    with open(os.path.join(C.EXPORTS, NAME, NAME + "_report.json"), encoding="utf-8") as fh:
        return json.load(fh)


def update_report(facts, tris):
    path = os.path.join(C.EXPORTS, NAME, NAME + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    if facts.get("petal_insides"):
        rep[RECOLOUR_KEY] = True
    rep["game_tris"] = tris["body"]
    rep["lod1"]["tris"] = tris["body_lod1"]
    rep["maw"] = dict(facts, game_tris_total=tris["body"] + facts["tris"],
                      lod1_tris_total=tris["body_lod1"] + facts["lod1_tris"],
                      unity="extra part 'Maw' (BlenderArtEntry.ExtraParts): object Gravemaw_Maw / Gravemaw_Maw_LOD1, "
                            "maps Gravemaw_Maw_*.png and Low/Gravemaw_Maw_*.png",
                      note="round 38: worm gullet with five rows of hooked teeth; the body lost the round-14 plug and "
                           "crystal teeth; bones, clips and events unchanged")
    r28 = rep.setdefault("round28", {})
    extra = [n for n in r28.get("extra_objects", []) if n not in (MAW, MAW_LOD)]
    r28["extra_objects"] = extra + [MAW, MAW_LOD]
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", path)


# ------------------------------------------------------------------------------------------------------------
# QA renders
# ------------------------------------------------------------------------------------------------------------

def qa_renders(tag, arm, hide=()):
    """Head close-ups (front, three-quarter, the game's top-down view) with the maw open (Roar) and at rest (Idle),
    backface culling on like Unity, so a hole in the cut would show."""
    os.makedirs(QA, exist_ok=True)
    for o in bpy.data.objects:
        if o.type in ("LIGHT", "CAMERA"):
            bpy.data.objects.remove(o)
    C.preview_world()
    C.light_rig(scale=2.0)
    for m in bpy.data.materials:
        m.use_backface_culling = True
    for o in hide:
        o.hide_render = True
    shots = [("front_open", "front", "Roar", 34), ("threequarter_open", "threequarter", "Roar", 34),
             ("game_open", "game", "Roar", 34), ("front_rest", "front", "Idle", 20),
             ("threequarter_rest", "threequarter", "Idle", 20), ("game_play", "game_play", "Roar", 34)]
    paths = []
    for label, cam, clip, frame in shots:
        C.set_action(arm, bpy.data.actions[clip], frame)
        paths.append(C.render(pose_camera(arm, cam), os.path.join(QA, f"gravemaw_maw_{tag}_{label}.png"), 1280, 720))
    C.contact_sheet(paths, os.path.join(QA, f"gravemaw_maw_{tag}.png"), 3, 640)
    return paths


def head_space(arm):
    """Rest-to-posed matrix of the Head bone (world), for cameras that follow the maw."""
    bpy.context.view_layer.update()
    pb = arm.pose.bones["Head"]
    return arm.matrix_world @ pb.matrix @ arm.data.bones["Head"].matrix_local.inverted()


def pose_camera(arm, kind):
    """`front`: on the posed maw axis, a little above; `threequarter`: 42 degrees round; `game`: the play view's
    pitch and yaw, zoomed on the head; `game_play`: the play view at play zoom."""
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    m = head_space(arm)
    mouth = m @ (BASE + A * 0.2)
    axis = (m.to_3x3() @ A).normalized()
    if kind == "game":
        return C.game_camera(mouth, 3.4)
    if kind == "game_play":
        return C.game_camera(Vector((0, 0, 0.3)), 9.0)
    turn = {"front": math.radians(8), "threequarter": math.radians(42)}[kind]
    lift = {"front": math.radians(10), "threequarter": math.radians(18)}[kind]
    flat = Vector((axis.x, axis.y, 0)).normalized()
    side = Vector((0, 0, 1)).cross(flat)
    view = (flat * math.cos(turn) + side * math.sin(turn)) * math.cos(lift) + Vector((0, 0, math.sin(lift)))
    cam_data = bpy.data.cameras.new("QACam")
    cam_data.lens = 55
    cam_data.clip_start = 0.01
    cam = C.link(bpy.data.objects.new("QACam", cam_data))
    cam.location = mouth + view * (3.8 if kind == "front" else 4.1)
    C.look_at(cam, mouth)
    return cam


def main():
    args = C.cli_args()
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm = bpy.data.objects[NAME + "_Rig"]
    mesh = bpy.data.objects[NAME]
    lod_arm = bpy.data.objects[NAME + "_LOD1_Rig"]
    lod_mesh = bpy.data.objects[NAME + "_LOD1"]
    lod_arm.hide_render = lod_mesh.hide_render = True
    if args.get("before"):
        qa_renders("before", arm)
        return
    if args.get("qa"):
        after_renders(arm, mesh, bpy.data.objects[MAW], lod_arm, lod_mesh, bpy.data.objects[MAW_LOD])
        return
    if args.get("blockout"):
        # Fast geometry check: raw parts in flat colours on the cut body, nothing saved or exported.
        cut_body(mesh)
        b = maw_parts()
        C.log("maw clearance", clearance(b))
        raw = b.to_object(MAW + "_Block")
        raw.data.materials.append(C.principled("BlockMaw", (0.8, 0.7, 0.55), rough=0.35, emission=(1.0, 0.4, 0.6),
                                               strength=0.3))
        skin_maw(raw, arm, b)
        qa_renders("blockout", arm)
        return
    folder = os.path.join(C.EXPORTS, NAME)
    tris_before = {"body": C.tri_count(mesh), "body_lod1": C.tri_count(lod_mesh)}
    cut = cut_all(arm, mesh, lod_arm, lod_mesh)
    recoloured = recolour_petal_insides(mesh, folder, load_report())
    maw, maw_lod, facts = build(arm, lod_arm, folder)
    facts["body_cut_tris"] = cut
    facts["petal_insides"] = recoloured or load_report().get(RECOLOUR_KEY, False)
    facts["tris_before"] = tris_before
    clips = export(arm, mesh, maw, lod_arm, lod_mesh, maw_lod)
    update_report(facts, {"body": C.tri_count(mesh), "body_lod1": C.tri_count(lod_mesh)})
    C.log("maw facts", json.dumps(facts), "clips", clips)
    maw_lod.hide_render = True
    C.save_blend(NAME)
    if args.get("norender"):
        return
    after_renders(arm, mesh, maw, lod_arm, lod_mesh, maw_lod)


def after_renders(arm, mesh, maw, lod_arm, lod_mesh, maw_lod):
    qa_renders("after", arm)
    # LOD1 with its maw, open, at the three-quarter view.
    arm.hide_render = mesh.hide_render = maw.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = maw_lod.hide_render = False
    C.set_action(lod_arm, bpy.data.actions["Roar_LOD"], 34)
    C.render(pose_camera(lod_arm, "threequarter"), os.path.join(QA, "gravemaw_maw_after_lod1_open.png"), 1280, 720)


if __name__ == "__main__":
    main()
