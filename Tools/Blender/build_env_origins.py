"""Round 14 environment pieces (see ArtSource/DESIGN_NOTES.md): the Gravemaw's burrow hole, the spawn origins in
the liquids (EnemyOrigin Water, Lava, Quicksand, Ice, Bog) and the resource node the harvest drone mines.

- Gravemaw_Hole: the crater the boss dives into and bursts out of (about 3.4 wide, pivot on the ground, the pit
  drops 1.0): heaved slabs round the opening, a debris ring, Crysium shards cracked loose, pink light rising.
  1024 maps (one per level at most, seen big).
- Origin_Water / _Lava / _Quicksand / _Ice / _Bog: one-cell openings, pivot ON THE LIQUID SURFACE (Unity:
  TerrainLiquidBuilder.SurfaceLevel(kind)); the throat sinks to about -0.5, the rim rises to about +0.1, ribs and
  plates higher. 512 maps; the Emission map is a white 0..1 mask that Unity tints with the element colour.
- ResourceNode: three stage meshes on one 1024 map set in one FBX (ResourceNode_Full, _Half, _Empty), pivot on
  the ground at the cell centre; gold-amber ore Crysium (the game's MineColor).

Run: blender --background --factory-startup --python Tools/Blender/build_env_origins.py -- [--preview <dir>]
     [--norender] [--only Origin_Water,ResourceNode]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import deep_kit as DK  # noqa: E402
import env_kit as E  # noqa: E402

DK.register_palettes()

PINK_GLOW = np.array((1.0, 0.36, 0.62))
WHITE = np.array((1.0, 1.0, 1.0))
# Preview tints of the white masks: the element colours of LevelVisualBuilder.NestColor.
TINT = {"Origin_Water": (0.3, 0.75, 1.0), "Origin_Lava": (1.0, 0.45, 0.12), "Origin_Quicksand": (0.95, 0.8, 0.4),
        "Origin_Ice": (0.8, 0.95, 1.0), "Origin_Bog": (0.55, 0.8, 0.3)}
# Preview liquid sheets (TerrainLiquidBuilder colours, alpha).
LIQUID = {"Origin_Water": ((0.1, 0.4, 0.55), 0.68), "Origin_Lava": ((1.0, 0.45, 0.1), 0.95),
          "Origin_Quicksand": ((0.62, 0.5, 0.3), 0.97), "Origin_Ice": ((0.75, 0.9, 1.0), 0.9),
          "Origin_Bog": ((0.18, 0.24, 0.12), 0.92)}

SAND = dict(top=np.array((0.66, 0.54, 0.34)), side=np.array((0.42, 0.33, 0.2)), bottom=np.array((0.12, 0.09, 0.06)),
            seam=np.array((0.18, 0.13, 0.08)))
ICE = dict(top=np.array((0.8, 0.88, 0.94)), side=np.array((0.52, 0.62, 0.72)), bottom=np.array((0.2, 0.26, 0.34)),
           seam=np.array((0.25, 0.3, 0.4)))
MIRE = dict(top=np.array((0.24, 0.22, 0.13)), side=np.array((0.13, 0.12, 0.07)), bottom=np.array((0.04, 0.04, 0.03)),
            seam=np.array((0.03, 0.03, 0.02)))
FLESH = dict(top=np.array((0.5, 0.26, 0.28)), side=np.array((0.28, 0.12, 0.15)), bottom=np.array((0.08, 0.03, 0.05)),
             seam=np.array((0.06, 0.02, 0.04)))


def polar_slab(b, name, z_top, radius, z_bottom, style, nu=96, nv=36):
    """A round slab: top surface z_top(r, a) over a disc of `radius`, flat bottom; the voxel fuse closes the seam."""
    def top(u, v):
        a = 2 * math.pi * u
        r = radius * v
        return Vector((r * math.cos(a), r * math.sin(a), z_top(r, a)))

    def bottom(u, v):
        a = 2 * math.pi * u
        r = radius * v
        return Vector((r * math.cos(a), r * math.sin(a), z_bottom))
    return b.slab(name, top, bottom, nu, nv, style=style)


def radial(p):
    return np.hypot(p[:, 0], p[:, 1])


def throat_mask(r0, r1, z0=-0.05, z1=-0.35):
    """1 deep in the throat (inside r1 and below z0), fading out above."""
    def fn(s, idx, part):
        p = s.p[idx]
        return C.smoothstep(r1, r0, radial(p)) * C.smoothstep(z0, z1, p[:, 2])
    return fn


def combine(*fns):
    def fn(s, idx, part):
        out = np.zeros(len(idx))
        for f in fns:
            out = np.maximum(out, f(s, idx, part))
        return out
    return fn


def fissures(scale, width, seed, z_max=0.3, r_range=(0.0, 9.0)):
    """Glowing crack lines in the upper surface (white mask)."""
    def fn(s, idx, part):
        p = s.p[idx]
        line = 1 - C.smoothstep(0.0, width, np.abs(C.value_noise(p, scale, seed)))
        line *= C.smoothstep(-0.2, 0.3, C.value_noise(p, scale * 0.35, seed + 1))
        r = radial(p)
        return line * C.smoothstep(r_range[1], r_range[0], r) * C.smoothstep(z_max, -0.1, p[:, 2])
    return fn


# ---------------------------------------------------------------------------------------------------------------
# Gravemaw hole
# ---------------------------------------------------------------------------------------------------------------

def crater_z(r, a):
    wobble = 0.04 * math.sin(3 * a + 0.7) + 0.025 * math.sin(7 * a + 1.3)
    if r < 0.45:
        return -1.0
    if r < 0.82 + wobble:
        t = (r - 0.45) / (0.37 + wobble)
        return -1.0 + 1.06 * float(C.smoothstep(0.0, 1.0, np.array(t))) ** 0.8
    if r < 1.2:
        t = (r - 0.82 - wobble) / (0.38 - wobble)
        return 0.06 + 0.16 * math.sin(math.pi * min(max(t, 0.0), 1.0) * 0.9) + 0.02 * math.sin(11 * a)
    t = (r - 1.2) / 0.55
    return 0.06 * (1 - t) ** 2 + 0.01 * math.sin(9 * a + 2.0) * (1 - t)


def crater_style(seed):
    base = E.stone(E.CRUST, seed, crack=0.7, dust=0.5, strata=30.0, depth_glow=(-0.15, -0.9, PINK_GLOW, 1.0),
                   veins=0.4, glow=E.VEIN_GLOW)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        r = radial(p)
        rng = np.random.default_rng(seed)
        ang = rng.uniform(0, 2 * math.pi, 30)
        rad = np.sqrt(rng.uniform(0.3, 1.0, 30)) * 1.75
        seeds = np.c_[np.cos(ang) * rad, np.sin(ang) * rad, np.zeros(30)]
        e = C.voronoi_edge(np.c_[p[:, :2], np.zeros(len(p))], seeds)
        heat = C.smoothstep(1.75, 0.8, r) * C.smoothstep(-0.2, 0.0, p[:, 2])
        seam = (1 - C.smoothstep(0.0, 0.02 + 0.02 * heat, e)) * heat
        s.albedo[idx] = C.lerp3(s.albedo[idx], E.VEIN * 0.5, seam * 0.7)
        s.emit[idx] = np.clip(s.emit[idx] + E.VEIN_GLOW[None, :] * (seam * 0.9)[:, None], 0, 1)
        s.disp[idx] = s.disp[idx] - 0.02 * seam
        floor = C.smoothstep(-0.8, -0.98, p[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.05, 0.02, 0.04)), floor * 0.8)
    return style


def gravemaw_hole():
    b = C.PartBuilder()
    polar_slab(b, "Crater", crater_z, 1.75, -1.2, crater_style(4101), nu=128, nv=60)
    rng = np.random.default_rng(4102)
    for i in range(11):
        a = i / 11 * 2 * math.pi + rng.uniform(-0.12, 0.12)
        r = 0.92 + rng.uniform(-0.04, 0.1)
        size = (rng.uniform(0.18, 0.3), rng.uniform(0.24, 0.36), rng.uniform(0.06, 0.09))
        tilt = math.radians(rng.uniform(32, 62))
        rot = Matrix.Rotation(a, 3, "Z") @ Matrix.Rotation(-tilt, 3, "Y")
        centre = Vector((math.cos(a) * r, math.sin(a) * r, 0.16 + rng.uniform(0, 0.06)))
        b.ellipsoid("Slab", centre, (size[2], size[1], size[0]), rot, segments=(20, 10),
                    deform=C.chiselled(12, 4110 + i, 0.18), style=E.stone(E.BASALT, 4120 + i, veins=0.5, dust=0.4,
                                                                      glow=E.VEIN_GLOW))
    for i in range(18):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(1.25, 1.72)
        size = rng.uniform(0.05, 0.12)
        c = Vector((math.cos(a) * r, math.sin(a) * r, size * 0.3))
        rot = Matrix.Rotation(rng.uniform(0, 6.3), 3, "Z") @ Matrix.Rotation(rng.uniform(-0.5, 0.5), 3, "X")
        b.ellipsoid("Chunk", c, (size * 1.2, size, size * 0.7), rot, segments=(12, 6),
                    deform=C.chiselled(8, 4140 + i, 0.3), style=E.stone(E.BASALT if i % 3 else E.CRUST, 4150 + i, dust=0.5))
    for i, a in enumerate((0.4, 1.7, 2.9, 4.2, 5.4)):
        r = rng.uniform(1.05, 1.3)
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0.05))
        d = Vector((math.cos(a) * rng.uniform(0.5, 1.1), math.sin(a) * rng.uniform(0.5, 1.1), 1.0))
        b.shard("Shard", base, d, rng.uniform(0.05, 0.08), rng.uniform(0.25, 0.45), sides=6, twist=1.4, flute=0.4,
                style=Y.shard_style("pink", 0.8, seed=4170 + i))
    return b


# ---------------------------------------------------------------------------------------------------------------
# Spawn origins (pivot on the liquid surface)
# ---------------------------------------------------------------------------------------------------------------

def funnel_z(throat=0.16, rim=0.33, depth=-0.5, rim_h=0.06, outer=-0.05, radius=0.55, folds=0, fold_h=0.0):
    def z(r, a):
        f = fold_h * math.cos(folds * a) if folds else 0.0
        if r < throat:
            return depth
        if r < rim:
            t = (r - throat) / (rim - throat)
            return depth + (rim_h + f - depth) * float(C.smoothstep(0.0, 1.0, np.array(t))) ** 0.7
        t = (r - rim) / (radius - rim)
        return rim_h + f + (outer - rim_h - f) * float(C.smoothstep(0.0, 1.0, np.array(t)))
    return z


def flesh_lip(seed):
    """Water opening: a fleshy ridged lip, pale where it is wet and worn, with glowing pores."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        a = np.arctan2(p[:, 1], p[:, 0])
        ridge = 0.5 + 0.5 * np.cos(a * 14 + C.value_noise(p, 6, seed) * 2)
        col = C.chitin_gradient(n, FLESH["top"], FLESH["side"], FLESH["bottom"])
        col = col * (0.8 + 0.3 * ridge)[:, None] * (1 + 0.12 * C.value_noise(p, 20, seed + 1))[:, None]
        col = C.lerp3(col, FLESH["bottom"], C.smoothstep(-0.05, -0.35, p[:, 2]))
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = 0.006 * ridge + 0.002 * C.value_noise(p, 40, seed + 2)
        s.rough[idx] = 0.35 + 0.15 * ridge
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = radial(p)
    return style


def pores(seed, count=40, size=0.022, r_range=(0.18, 0.42), z_min=-0.08):
    rng = np.random.default_rng(seed)
    ang = rng.uniform(0, 2 * math.pi, count)
    rad = rng.uniform(*r_range, count)

    def fn(s, idx, part):
        p = s.p[idx]
        m = np.zeros(len(idx))
        for a, r in zip(ang, rad):
            c = np.array((math.cos(a) * r, math.sin(a) * r))
            d = np.hypot(p[:, 0] - c[0], p[:, 1] - c[1])
            m = np.maximum(m, 1 - C.smoothstep(size * 0.3, size, d))
        return m * C.smoothstep(z_min - 0.05, z_min + 0.05, p[:, 2])
    return fn


def rib_style(col_root, col_tip, glow_from=0.7):
    col_root, col_tip = np.asarray(col_root), np.asarray(col_tip)

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.lerp3(col_root, col_tip, C.smoothstep(0.2, 1.0, t))
        col = col * (0.6 + 0.4 * C.smoothstep(-0.6, 0.7, n[:, 2]))[:, None]
        band = 1 - C.smoothstep(0.0, 0.08, np.abs(((t * 6) % 1.0) - 0.5) - 0.38)
        s.albedo[idx] = C.lerp3(col, col_root * 0.4, band * 0.5)
        s.emit[idx] = 0.0
        s.disp[idx] = -0.003 * band
        s.rough[idx] = 0.45
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = t * C.axis_length(part)
    return style


def tube_tip_mask(start=0.75):
    def fn(s, idx, part):
        t, _, _ = C.tube_param(part, s.p[idx])
        return C.smoothstep(start, 1.0, t)
    return fn


def origin_water():
    b = C.PartBuilder()
    polar_slab(b, "Lip", funnel_z(throat=0.15, rim=0.32, depth=-0.5, rim_h=0.07, folds=7, fold_h=0.02),
               0.56, -0.62, DK.mask_glow(flesh_lip(5101), combine(throat_mask(0.08, 0.2), pores(5102))))
    for i in range(5):
        a = i / 5 * 2 * math.pi + 0.3
        out = Vector((math.cos(a), math.sin(a), 0))
        root = out * 0.36 + Vector((0, 0, 0.0))
        pts = [root, out * 0.38 + Vector((0, 0, 0.18)), out * 0.27 + Vector((0, 0, 0.32)), out * 0.12 + Vector((0, 0, 0.27))]
        b.tube("Rib", pts, [0.045, 0.036, 0.024, 0.004], sides=10,
               style=DK.mask_glow(rib_style((0.32, 0.16, 0.18), (0.86, 0.78, 0.72)), tube_tip_mask(0.8)))
    return b


def origin_lava():
    b = C.PartBuilder()
    crust = E.stone(E.OBSIDIAN, 5201, crack=0.8, dust=0.2, strata=40.0)
    polar_slab(b, "Crust", funnel_z(throat=0.14, rim=0.28, depth=-0.45, rim_h=0.1, outer=-0.04, folds=5, fold_h=0.03),
               0.55, -0.6, DK.mask_glow(crust, combine(throat_mask(0.06, 0.2, 0.0, -0.25), fissures(9, 0.06, 5202, 0.15))))
    rng = np.random.default_rng(5203)
    for i in range(7):
        a = i / 7 * 2 * math.pi + rng.uniform(-0.15, 0.15)
        r = 0.27 + rng.uniform(-0.02, 0.03)
        rot = Matrix.Rotation(a, 3, "Z") @ Matrix.Rotation(-math.radians(rng.uniform(20, 40)), 3, "Y")
        centre = Vector((math.cos(a) * r, math.sin(a) * r, 0.12 + rng.uniform(0, 0.05)))
        b.ellipsoid("Plate", centre, (0.035, 0.1, 0.13), rot, segments=(16, 8), deform=C.chiselled(10, 5210 + i, 0.2),
                    style=DK.mask_glow(E.stone(E.OBSIDIAN, 5220 + i, crack=0.8), fissures(14, 0.08, 5230 + i, 0.4)))
    for i, a in enumerate((1.1, 3.9)):
        base = Vector((math.cos(a) * 0.4, math.sin(a) * 0.4, 0.02))
        b.shard("Ember", base, Vector((math.cos(a) * 0.6, math.sin(a) * 0.6, 1)), 0.035, 0.2, sides=6, twist=1.2,
                flute=0.4, style=DK.mask_glow(Y.shard_style("ember", 1.0, seed=5240 + i), tube_tip_mask(0.2)))
    return b


def sand_style(seed):
    base = E.stone(SAND, seed, crack=0.15, dust=0.0, strata=0.0, rough=0.9)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        r = radial(p)
        ripple = 0.5 + 0.5 * np.sin(r * 55 + C.value_noise(p, 5, seed) * 2.0)
        s.albedo[idx] = s.albedo[idx] * (0.85 + 0.2 * ripple)[:, None]
        s.albedo[idx] = C.lerp3(s.albedo[idx], SAND["bottom"], C.smoothstep(-0.1, -0.4, p[:, 2]))
        s.disp[idx] = 0.006 * ripple
    return style


def shell_style(seed):
    top, dark, bone = (np.array((0.78, 0.36, 0.12)), np.array((0.36, 0.07, 0.06)), np.array((0.92, 0.8, 0.64)))

    def style(s, idx, part):
        n = s.n[idx]
        col = C.chitin_gradient(n, top, dark, dark * 0.5) * (1 + 0.1 * C.value_noise(s.p[idx], 30, seed))[:, None]
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = 0.0
        s.rough[idx] = 0.55
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = s.p[idx][:, 2]
    return style


def origin_quicksand():
    b = C.PartBuilder()

    def z(r, a):
        base = funnel_z(throat=0.13, rim=0.36, depth=-0.48, rim_h=0.09, outer=-0.03, radius=0.56)(r, a)
        terrace = 0.025 * math.sin(r * 38 + 0.5 * math.sin(3 * a)) * float(C.smoothstep(0.12, 0.3, np.array(r)))
        return base + terrace
    polar_slab(b, "Sink", z, 0.56, -0.6, DK.mask_glow(sand_style(5301), combine(throat_mask(0.06, 0.18, -0.1, -0.4),
                                                                                    fissures(10, 0.035, 5302, -0.05, (0.12, 0.3)))))
    for i, (a, tilt) in enumerate(((0.8, 30), (3.6, 45))):
        c = Vector((math.cos(a) * 0.38, math.sin(a) * 0.38, 0.03))
        rot = Matrix.Rotation(a, 3, "Z") @ Matrix.Rotation(math.radians(tilt), 3, "Y")
        b.ellipsoid("Shell", c, (0.16, 0.11, 0.03), rot, segments=(20, 10), deform=C.chiselled(9, 5310 + i, 0.15),
                    style=shell_style(5320 + i))
    for i, a in enumerate((2.0, 5.1)):
        out = Vector((math.cos(a), math.sin(a), 0))
        pts = [out * 0.42 - Vector((0, 0, 0.02)), out * 0.36 + Vector((0, 0, 0.16)), out * 0.22 + Vector((0, 0, 0.2))]
        b.tube("Hook", pts, [0.03, 0.02, 0.003], sides=8,
               style=DK.mask_glow(rib_style((0.4, 0.3, 0.22), (0.92, 0.84, 0.7)), tube_tip_mask(0.85)))
    return b


def ice_style(seed):
    base = E.stone(ICE, seed, crack=0.5, dust=0.0, strata=0.0, rough=0.35)

    def style(s, idx, part):
        base(s, idx, part)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def origin_ice():
    b = C.PartBuilder()
    polar_slab(b, "Shelf", funnel_z(throat=0.16, rim=0.3, depth=-0.42, rim_h=0.05, outer=-0.02, radius=0.55),
               0.55, -0.55, DK.mask_glow(ice_style(5401), combine(throat_mask(0.08, 0.22, -0.02, -0.3),
                                                                  fissures(11, 0.045, 5402, 0.1, (0.15, 0.45)))))
    rng = np.random.default_rng(5403)
    for i in range(8):
        a = i / 8 * 2 * math.pi + rng.uniform(-0.15, 0.15)
        r = 0.3 + rng.uniform(0.0, 0.06)
        rot = Matrix.Rotation(a, 3, "Z") @ Matrix.Rotation(-math.radians(rng.uniform(25, 50)), 3, "Y")
        centre = Vector((math.cos(a) * r, math.sin(a) * r, 0.08 + rng.uniform(0, 0.05)))
        size = rng.uniform(0.85, 1.15)
        b.ellipsoid("Floe", centre, (0.03 * size, 0.11 * size, 0.13 * size), rot, segments=(16, 8),
                    deform=C.chiselled(9, 5410 + i, 0.2),
                    style=DK.mask_glow(ice_style(5420 + i), fissures(18, 0.07, 5430 + i, 0.5)))
    return b


def bog_style(seed):
    base = E.stone(MIRE, seed, crack=0.3, dust=0.0, strata=0.0, rough=0.55)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        a = np.arctan2(p[:, 1], p[:, 0])
        r = radial(p)
        fold = (0.5 + 0.5 * np.cos(a * 9 + r * 6)) * C.smoothstep(0.3, 0.1, r)
        s.albedo[idx] = C.lerp3(s.albedo[idx], FLESH["side"], fold * 0.6 + C.smoothstep(0.2, 0.08, r) * 0.3)
        s.disp[idx] = s.disp[idx] + 0.008 * fold
        s.fam[idx] = E.FAM_MEMBRANE
    return style


def origin_bog():
    b = C.PartBuilder()

    def z(r, a):
        mound = 0.17 * math.exp(-((r - 0.24) / 0.15) ** 2) * (1 + 0.15 * math.sin(4 * a))
        pucker = funnel_z(throat=0.07, rim=0.2, depth=-0.38, rim_h=0.0, outer=-0.04, radius=0.55)(r, a)
        return pucker + mound + 0.02 * math.cos(9 * a) * float(C.smoothstep(0.25, 0.08, np.array(r)))
    polar_slab(b, "Mound", z, 0.55, -0.5, DK.mask_glow(bog_style(5501), throat_mask(0.04, 0.12, 0.0, -0.25)))
    rng = np.random.default_rng(5502)
    for i in range(7):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.2, 0.42)
        size = rng.uniform(0.025, 0.06)
        zc = z(r, a) + size * 0.4
        b.ellipsoid("Blister", Vector((math.cos(a) * r, math.sin(a) * r, zc)), (size, size, size * 0.85),
                    segments=(14, 8), style=DK.mask_glow(bog_style(5510 + i), lambda s, idx, part: 0.35 + 0.65 *
                                                        C.smoothstep(0.2, 0.9, s.n[idx][:, 2])))
    for i, a in enumerate((0.6, 2.7, 4.6)):
        out = Vector((math.cos(a), math.sin(a), 0))
        pts = [out * 0.3 + Vector((0, 0, 0.08)), out * 0.4 + Vector((0, 0, 0.22)), out * 0.55 + Vector((0, 0, 0.12)),
               out * 0.62 - Vector((0, 0, 0.03))]
        b.tube("Tendril", pts, [0.03, 0.024, 0.016, 0.006], sides=8,
               style=DK.mask_glow(rib_style((0.2, 0.16, 0.08), (0.5, 0.3, 0.26)), tube_tip_mask(0.9)))
    return b


# ---------------------------------------------------------------------------------------------------------------
# Resource node (three stages)
# ---------------------------------------------------------------------------------------------------------------

STAGE_X = {"Full": -2.5, "Half": 0.0, "Empty": 2.5}


def cut_shard(style, top=0.93, glow=1.0, colour=(1.0, 0.9, 0.62)):
    """A sawn-off shard: its flat top cap gets a bright smooth cut face (or a dull one with a low `glow`)."""
    colour = np.asarray(colour)

    def wrapped(s, idx, part):
        style(s, idx, part)
        t, _, _ = C.tube_param(part, s.p[idx])
        cap = C.smoothstep(top - 0.02, top + 0.03, t)
        s.albedo[idx] = C.lerp3(s.albedo[idx], colour * (0.5 + 0.4 * glow), cap)
        s.emit[idx] = np.clip(s.emit[idx] * (1 - cap[:, None]) + colour[None, :] * (cap * glow)[:, None], 0, 1)
        s.rough[idx] = s.rough[idx] * (1 - cap) + 0.15 * cap
    return wrapped


def flat_top(t):
    return 1.0 - 0.25 * t


def node_stage(b, stage, x0):
    rng = np.random.default_rng(6101)
    o = Vector((x0, 0, 0))
    vein_glow = {"Full": 0.9, "Half": 0.7, "Empty": 0.25}[stage]
    gold = DK.GOLD[1][0]
    for i, (dx, rx, tilt) in enumerate(((-0.17, 0.3, -18), (0.18, 0.27, 16))):
        rot = Matrix.Rotation(math.radians(tilt), 3, "Y") @ Matrix.Rotation(0.4 * i, 3, "Z")
        b.ellipsoid("Boulder", o + Vector((dx, 0.02 * i, 0.12)), (rx, 0.32, 0.22), rot, segments=(28, 14),
                    deform=C.chiselled(16, 6110 + i, 0.2, squash=(0.6, 1.0)),
                    style=E.stone(E.BASALT, 6120 + i, veins=vein_glow, glow=gold, dust=0.4))
    # Fat fluted ore shards spiralling out of the split, the tallest in the middle.
    shards = [(Vector((0.0, 0.0, 0.08)), Vector((0.05, 0.1, 1.0)), 0.11, 0.78, 0.5),
              (Vector((0.04, -0.12, 0.06)), Vector((0.25, -0.6, 1.0)), 0.075, 0.48, 0.55),
              (Vector((-0.03, 0.13, 0.06)), Vector((-0.3, 0.55, 1.0)), 0.07, 0.44, 0.5),
              (Vector((0.02, 0.2, 0.04)), Vector((0.2, 0.9, 1.0)), 0.05, 0.3, 0.4),
              (Vector((-0.02, -0.22, 0.04)), Vector((-0.15, -1.0, 0.9)), 0.045, 0.26, 0.4),
              (Vector((0.06, 0.05, 0.08)), Vector((0.8, 0.2, 1.0)), 0.04, 0.24, 0.3),
              (Vector((-0.07, -0.02, 0.08)), Vector((-0.9, -0.1, 1.0)), 0.04, 0.22, 0.3)]
    for i, (base, d, radius, length, keep_half) in enumerate(shards):
        twist = 1.6 * (1 if i % 2 else -1)
        style = Y.shard_style("gold", 1.0, seed=6130 + i)
        if stage == "Full":
            b.shard("Ore", o + base, d, radius, length, sides=8, twist=twist, flute=0.42, style=style)
            continue
        cut = length * (keep_half if stage == "Half" else 0.16)
        if stage == "Half" and i >= 3:
            b.shard("Ore", o + base, d, radius, length, sides=8, twist=twist, flute=0.42, style=style)
            continue
        glow = 1.0 if stage == "Half" else 0.12
        b.shard("Stump", o + base, d, radius, cut, sides=8, twist=twist * cut / length, flute=0.42, rings=22,
                profile=flat_top, style=cut_shard(Y.shard_style("gold", 0.7 if stage == "Half" else 0.25, seed=6140 + i),
                                                  glow=glow))
    if stage == "Empty":
        b.ellipsoid("Spoil", o + Vector((0.3, -0.25, 0.02)), (0.09, 0.07, 0.04), segments=(12, 6),
                    deform=C.chiselled(8, 6150, 0.3), style=E.stone(E.BASALT, 6151, dust=0.5))


def resource_node():
    b = C.PartBuilder()
    for stage, x0 in STAGE_X.items():
        node_stage(b, stage, x0)
    return b


def split_stages(asset):
    """Splits the baked game mesh into one object per stage (by x band) and moves each pivot to its centre."""
    objs = []
    for stage, x0 in STAGE_X.items():
        obj = C.link(asset.low.copy())
        obj.data = asset.low.data.copy()
        obj.name = obj.data.name = f"ResourceNode_{stage}"
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        kill = [f for f in bm.faces if abs(f.calc_center_median().x - x0) > 1.2]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector((-x0, 0, 0)))
        bm.to_mesh(obj.data)
        bm.free()
        objs.append(obj)
    asset.low.hide_render = True
    return objs


# ---------------------------------------------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------------------------------------------

ASSETS = {
    # name: (builder, target tris, voxel, smoothing, families, texture size, ao distance)
    "Gravemaw_Hole": (gravemaw_hole, 2400, 0.012, 2, ("rock", "crystal"), 1024, 0.5),
    "Origin_Water": (origin_water, 1800, 0.006, 2, ("membrane", "chitin"), 512, 0.2),
    "Origin_Lava": (origin_lava, 1800, 0.006, 2, ("rock", "crystal"), 512, 0.2),
    "Origin_Quicksand": (origin_quicksand, 1700, 0.006, 2, ("rock", "chitin"), 512, 0.2),
    "Origin_Ice": (origin_ice, 1800, 0.006, 2, ("crystal", "rock"), 512, 0.2),
    "Origin_Bog": (origin_bog, 1800, 0.006, 2, ("membrane", "chitin"), 512, 0.2),
    "ResourceNode": (resource_node, 5400, 0.006, 1, ("rock", "crystal"), 1024, 0.25),
}

PLACEMENT = {
    "Gravemaw_Hole": "pivot on the ground at the hole centre; pit floor at -1.0; place DIVE_HOLE ahead of / "
                     "EMERGE_HOLE behind the boss pivot (see Gravemaw_report.json)",
    "Origin": "pivot on the liquid surface: cell centre at TerrainLiquidBuilder.SurfaceLevel(kind); any yaw; "
              "Emission is a white mask, tint it with LevelVisualBuilder.NestColor(element)",
    "ResourceNode": "pivot on the ground at the mine cell centre (MineCells); swap Full -> Half at 50 % stock, "
                    "Empty on MineDepleted",
}


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    only = C.cli_args().get("only")
    built = {}
    for name, (fn, tris, voxel, smoothing, families, tex, ao) in ASSETS.items():
        if only and name not in only.split(","):
            continue
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, families=families, emission=2.2, ao=ao,
                        tex=tex, extrusion=0.03, ray=0.1)
        placement = PLACEMENT.get(name, PLACEMENT["Origin"])
        if name == "ResourceNode":
            stages = split_stages(asset)
            C.export_fbx(os.path.join(E.ENV, name, name + ".fbx"), stages, animated=False)
            dims = {o.name: [round(d, 3) for d in o.dimensions] for o in stages}
            C.write_report(name, {"asset": name, "stages": {o.name: C.tri_count(o) for o in stages},
                                  "game_tris_total": asset.low_tris, "source_tris": asset.high_tris,
                                  "texture_size": tex, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                                  "uv": asset.uv_stats, "detail": asset.details, "size_m": dims,
                                  "placement": placement, "emission": "baked gold (MineColor), not a mask"})
            built[name] = (asset, stages)
        else:
            extra = {"texture_size": tex, "placement": placement,
                     "emission": "baked pink (Crysium)" if name == "Gravemaw_Hole" else "white mask, tint in Unity"}
            E.finish(asset, extra)
            built[name] = (asset, [asset.low])
    C.save_blend("Env_Origins" if not only else "Env_Origins_" + only.replace(",", "_"))
    if not C.cli_args().get("norender"):
        previews(built, out)


def previews(built, out):
    C.preview_world((0.02, 0.02, 0.035))
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, (asset, objs) in built.items():
        for _, (_, others) in built.items():
            for o in others:
                o.hide_render = True
        mat = objs[0].data.materials[0]
        if name in TINT:
            DK.tint_emission(mat, TINT[name])
        extras = []
        if name in LIQUID:
            colour, alpha = LIQUID[name]
            dark = tuple(c * 0.35 for c in colour)
            extras.append(DK.liquid_sheet(name + "_Liquid", dark, alpha * 0.85, size=3.0, z=0.0, hole=(0, 0, 0.2),
                                          emission=dark if name == "Origin_Lava" else None,
                                          strength=1.5 if name == "Origin_Lava" else 0.0))
            extras.append(C.ground(size=3.0, color=(0.05, 0.05, 0.07), z=-0.65))
        elif name == "Gravemaw_Hole":
            extras.append(DK.ground_with_holes([(0, 0, 1.7)], size=14.0, color=(0.05, 0.05, 0.07)))
        else:
            extras.append(C.ground(size=14.0, color=(0.05, 0.05, 0.07)))
        if name == "ResourceNode":
            for o, x in zip(objs, (-0.9, 0.0, 0.9)):
                o.hide_render = False
                o.location.x = x
            aim, dist = Vector((0, 0, 0.2)), 2.6
        else:
            objs[0].hide_render = False
            size = max(objs[0].dimensions.x, objs[0].dimensions.y)
            aim, dist = Vector((0, 0, -0.05)), size * 1.9 + 0.3
        cam = C.closeup_camera(aim, dist, azimuth=-30, elevation=42, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 960, 640))
        bpy.data.objects.remove(cam)
        cam = C.macro_camera(aim + Vector((0, 0, 0.05)), dist * 0.45, -40, 28)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}_macro.png"), 960, 640))
        bpy.data.objects.remove(cam)
        # Game camera, play zoom x2.5 (an origin is one cell).
        C.hide_lights(rig, True)
        lights = C.game_lights()
        zoom = 1.6 if name == "Gravemaw_Hole" else 2.5
        cam = C.game_camera(Vector((0, 0, 0)), 640 / C.GAME_PIXELS_PER_UNIT / zoom)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}_game.png"), 640, 360))
        for o in lights + [cam]:
            bpy.data.objects.remove(o)
        C.hide_lights(rig, False)
        C.remove_objects(extras)
        C.uv_layout(objs[0], os.path.join(out, f"env_{name.lower()}_uv.png"), 512, background=asset.textures["albedo"])
    C.contact_sheet(paths, os.path.join(out, "sheet_env_origins.png"), 3, 480)


main()
