"""Crystal Plains environment kit (round 8): rock decor, flora, egg clusters and far background silhouettes.

Rock decor (PropKind Boulder, Column, Spike, Spire), consistent with Rock_A/B/C (blue-grey basalt, Crysium veins):
- Decor_Boulder: a split mass of leaning shingled plates with veins and two shards in a seam.
- Decor_Column: a natural stack of flattened lenses with glowing seams between them (not a ruin pillar).
- Decor_Spike: a fan of hooked obsidian horns from a broken base, pink light at their roots.
- Decor_Spire: a tall twisted fluted rock needle with a Crysium crown (map-edge height).

Flora (PropKind Frond, Tendril, GlowTree, Mushroom): plum-dark flesh with Crysium pink and acid lime light. Each
carries a vertex colour 'Sway' (R bend weight, G phase, B flutter; see env_kit.write_sway).
- Flora_Frond: a fan of ribbed blades curling out of a bulb, lit midribs and tips.
- Flora_Tendril: a tuft of segmented feelers ending in eye-like bulbs with a dark slit.
- Flora_GlowTree: three strands winding into a trunk under a flat umbrella canopy; lit gills underneath,
  glowing filaments with beads hang from it.
- Flora_Mushroom: lantern caps whose ribs leak light, lit gills, a crown of spines.

Nest: Egg_Cluster (leathery Burrower eggs on a resin mat, veins glowing through).
Background: Silhouette_CrystalTower, Silhouette_Ridge, Silhouette_Mesa (far, low, for mist depth; in world
units, use at scale 1).

Run: blender --background --factory-startup --python Tools/Blender/build_env_decor.py -- [--preview <dir>]
     [--norender] [--only Flora_Frond]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402

PLUM = np.array((0.26, 0.09, 0.27))
PLUM_DARK = np.array((0.09, 0.03, 0.1))
PLUM_PALE = np.array((0.62, 0.36, 0.6))
LIME = np.array((0.72, 1.0, 0.3))
PINK = np.array((1.0, 0.45, 0.82))
EGG = (np.array((0.88, 0.5, 0.26)), np.array((0.45, 0.08, 0.1)), np.array((0.16, 0.03, 0.04)))
EGG_GLOW = np.array((1.0, 0.5, 0.3))


def rz(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


# ------------------------------------------------------------------------------------------------- rock decor

def plate(b, rng, centre, facing, size, seed, lean, veins=0.8, pal=E.BASALT):
    rot = rz(math.degrees(facing) + 90) @ Matrix.Rotation(math.radians(lean), 3, "X") @ rz(rng.uniform(-8, 8))
    b.ellipsoid("Plate", Vector(centre), size, rot, style=E.stone(pal, seed, veins=veins, strata=16, dust=0.4),
                segments=(32, 16), deform=C.chiselled(14, seed, 0.16, squash=(0.8, 1.0)))


def boulder():
    b = C.PartBuilder()
    rng = np.random.default_rng(4001)
    b.ellipsoid("Core", Vector((0, 0, 0.14)), (0.3, 0.26, 0.24), style=E.stone(E.BASALT, 4010, veins=0.5, dust=0.5),
                segments=(32, 16), deform=C.chiselled(16, 4010, 0.18))
    for i, a in enumerate((0.3, 1.9, 3.4, 4.9)):
        c = Vector((math.cos(a) * 0.3, math.sin(a) * 0.26, 0.28 + 0.05 * (i % 2)))
        plate(b, rng, c, a, (0.27, 0.08, 0.38 - 0.04 * (i % 2)), 4020 + i, lean=-28 - rng.uniform(0, 12))
    for i, (x, y, dx, dy, length) in enumerate(((0.06, 0.0, 0.2, -0.4, 0.3), (0.0, 0.08, -0.4, 0.1, 0.22))):
        b.shard("Crysium", Vector((x, y, 0.45)), Vector((dx, dy, 1)), 0.05, length, sides=8, twist=1.6, flute=0.4,
                style=Y.shard_style("pink" if i == 0 else "ice", 1.0, seed=4030 + i))
    return b


def column():
    b = C.PartBuilder()
    rng = np.random.default_rng(4101)
    z = 0.0
    seams = []
    for i, (r, h) in enumerate(((0.24, 0.1), (0.2, 0.11), (0.23, 0.1), (0.16, 0.1), (0.19, 0.09))):
        z += h
        off = Vector((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), z))
        b.ellipsoid("Lens", off, (r, r * rng.uniform(0.85, 1.0), h), rz(rng.uniform(0, 90)),
                    style=E.stone(E.BASALT, 4110 + i, veins=0.4, strata=0.0, dust=0.6), segments=(32, 16),
                    deform=C.chiselled(14, 4110 + i, 0.12, squash=(0.9, 1.0)))
        z += h * 0.85
        seams.append((off, r))
    for i, (c, r) in enumerate(seams[:-1]):
        b.ellipsoid("Seam", c + Vector((0, 0, 0.1)), (r * 0.72, r * 0.68, 0.03), style=E.glowing(E.VEIN_GLOW, 0.6),
                    segments=(24, 8))
    for i in range(3):
        a = rng.uniform(0, 2 * math.pi)
        b.ellipsoid("Rubble", Vector((math.cos(a) * 0.33, math.sin(a) * 0.33, 0.02)), (0.07, 0.06, 0.05),
                    style=E.stone(E.BASALT, 4130 + i), segments=(14, 7), deform=C.chiselled(8, 4130 + i, 0.3))
    return b


def horn_style(seed):
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, E.OBSIDIAN["top"] * 1.4, E.OBSIDIAN["side"], E.OBSIDIAN["bottom"])
        col = C.lerp3(col, np.array((0.55, 0.5, 0.6)), C.smoothstep(0.7, 1.0, t) * 0.6)
        band = 1 - C.smoothstep(0.0, 0.05, np.abs(((t * 6) % 1.0) - 0.5) - 0.4)
        s.albedo[idx] = C.lerp3(col, E.OBSIDIAN["seam"], band * 0.6) * (1 + 0.1 * C.value_noise(s.p[idx], 25, seed))[:, None]
        root = 1 - C.smoothstep(0.0, 0.22, t)
        s.emit[idx] = E.VEIN_GLOW[None, :] * (root * 0.8)[:, None]
        s.disp[idx] = -0.003 * band
        s.rough[idx] = 0.3 + 0.2 * band
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = t * C.axis_length(part)
    return style


def spike():
    b = C.PartBuilder()
    rng = np.random.default_rng(4201)
    b.ellipsoid("Base", Vector((0, 0, 0.0)), (0.3, 0.26, 0.12), style=E.stone(E.OBSIDIAN, 4210, veins=0.6, dust=0.3),
                segments=(28, 14), deform=C.chiselled(12, 4210, 0.2))
    for i in range(6):
        a = i / 6 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        h = rng.uniform(0.45, 0.85)
        side = out.cross(Vector((0, 0, 1)))
        pts = [out * 0.08 + Vector((0, 0, 0.04)), out * 0.2 + Vector((0, 0, h * 0.45)),
               out * 0.22 + side * 0.04 + Vector((0, 0, h * 0.8)), out * 0.1 + side * 0.08 + Vector((0, 0, h))]
        b.tube("Horn", pts, [0.075, 0.055, 0.03, 0.004], sides=10, style=horn_style(4220 + i))
    return b


def spire():
    b = C.PartBuilder()
    rng = np.random.default_rng(4301)
    b.shard("Needle", Vector((0, 0, -0.05)), Vector((0.08, -0.05, 1)), 0.46, 2.9, sides=7, twist=1.8, flute=0.22,
            rings=16, style=E.stone(E.BASALT, 4310, veins=0.7, strata=9, dust=0.3),
            profile=lambda t: (1.0 - t) ** 0.9 * (0.85 + 0.15 * math.cos(t * 9)))
    for i in range(3):
        a = i / 3 * 2 * math.pi + 0.4
        out = Vector((math.cos(a), math.sin(a), 0))
        base = out * 0.16 + Vector((0.1, -0.06, 1.75 + 0.18 * i))
        b.shard("Crown", base, out * 0.7 + Vector((0, 0, 1)), 0.08, rng.uniform(0.45, 0.7), sides=8, twist=1.8,
                flute=0.45, style=Y.shard_style("pink" if i != 1 else "ice", 1.0, seed=4320 + i))
    for i in range(3):
        a = i / 3 * 2 * math.pi + 1.2
        b.ellipsoid("Foot", Vector((math.cos(a) * 0.45, math.sin(a) * 0.4, 0.06)), (0.24, 0.2, 0.16),
                    style=E.stone(E.BASALT, 4330 + i, veins=0.3, dust=0.5), segments=(24, 12),
                    deform=C.chiselled(12, 4330 + i, 0.2))
    return b


# ------------------------------------------------------------------------------------------------------ flora

def flesh(seed, glow=PINK, tip_glow=0.9, rib=0.6, bands=0.0):
    """Plum flesh along a tube or blade: dark root, paler body, a lit midrib and a glowing tip."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        if part.axis is not None:
            t, radius, dist = C.tube_param(part, p)
            rho = dist / np.maximum(radius, 1e-4)
        else:
            t = C.smoothstep(0.0, 0.6, p[:, 2])
            rho = np.ones(len(idx))
        col = C.lerp3(PLUM_DARK, PLUM, C.smoothstep(0.0, 0.4, t))
        col = C.lerp3(col, PLUM_PALE, C.smoothstep(0.5, 0.9, n[:, 2]) * 0.35)
        mid = (1 - C.smoothstep(0.15, 0.4, rho)) * rib * C.smoothstep(0.1, 0.3, t)
        tip = C.smoothstep(0.75, 0.97, t) * tip_glow
        band = np.zeros(len(idx))
        if bands:
            band = 1 - C.smoothstep(0.0, 0.08, np.abs(((t * bands) % 1.0) - 0.5) - 0.38)
            col = C.lerp3(col, PLUM_DARK, band * 0.7)
        col = col * (1 + 0.12 * C.value_noise(p, 30, seed))[:, None]
        light = np.clip(mid * 0.6 + tip, 0, 1)
        s.albedo[idx] = C.lerp3(col, glow * 0.75, light)
        s.emit[idx] = np.asarray(glow)[None, :] * light[:, None]
        s.disp[idx] = -0.002 * band
        s.rough[idx] = 0.45 - 0.15 * light
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t * (C.axis_length(part) if part.axis is not None else 0.3)
    return style


def bulb_style(seed, glow=PINK, strength=0.9, slit=False):
    def style(s, idx, part):
        p = s.p[idx]
        L = C.local_points(part, p)
        col = C.lerp3(PLUM, glow * 0.8, C.smoothstep(-0.2, 0.8, L[:, 2]))
        emit = np.asarray(glow)[None, :] * (strength * C.smoothstep(-0.3, 0.6, L[:, 2]))[:, None]
        if slit:
            pupil = 1 - C.smoothstep(0.05, 0.16, np.abs(L[:, 0]))
            front = C.smoothstep(0.2, 0.6, -L[:, 1])
            k = pupil * front
            col = C.lerp3(col, PLUM_DARK * 0.4, k)
            emit = emit * (1 - k)[:, None]
        s.albedo[idx] = col
        s.emit[idx] = np.clip(emit, 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.3
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = L[:, 2]
    return style


def frond():
    b = C.PartBuilder()
    rng = np.random.default_rng(4401)
    b.ellipsoid("Bulb", Vector((0, 0, 0.06)), (0.13, 0.13, 0.1), style=bulb_style(4410, LIME, 0.5), segments=(24, 12))
    count = 7
    for i in range(count):
        a = i / count * 2 * math.pi + rng.uniform(-0.25, 0.25)
        out = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.35, 0.5)
        h = rng.uniform(0.45, 0.7)
        pts = [out * 0.05 + Vector((0, 0, 0.1)), out * reach * 0.35 + Vector((0, 0, h * 0.75)),
               out * reach * 0.75 + Vector((0, 0, h)), out * reach + Vector((0, 0, h * 0.8)),
               out * reach * 1.05 + Vector((0, 0, h * 0.62))]
        glow = LIME if i % 3 == 0 else PINK
        b.tube("Blade", pts, [0.03, 0.075, 0.07, 0.045, 0.006], sides=10, flatten=0.3,
               style=flesh(4420 + i, glow, tip_glow=0.9, rib=0.8, bands=7))
    return b


def tendril():
    b = C.PartBuilder()
    rng = np.random.default_rng(4501)
    b.ellipsoid("Root", Vector((0, 0, 0.0)), (0.16, 0.15, 0.06), style=flesh(4510, PINK, tip_glow=0.0, rib=0.0),
                segments=(20, 10))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        h = rng.uniform(0.45, 0.95)
        side = out.cross(Vector((0, 0, 1))) * rng.uniform(-0.1, 0.1)
        pts = [out * 0.04 + Vector((0, 0, 0.02)), out * 0.12 + side + Vector((0, 0, h * 0.35)),
               out * 0.06 - side + Vector((0, 0, h * 0.7)), out * 0.16 + Vector((0, 0, h * 0.93))]
        b.tube("Feeler", pts, [0.045, 0.035, 0.026, 0.02], sides=10, style=flesh(4520 + i, PINK, tip_glow=0.0,
                                                                                    rib=0.0, bands=9))
        tip = pts[-1] + out * 0.03 + Vector((0, 0, 0.03))
        look = (out + Vector((0, 0, 0.4))).normalized()
        rot = look.to_track_quat("-Y", "Z").to_matrix()
        b.ellipsoid("Eye", tip, (0.05, 0.055, 0.06), rot, style=bulb_style(4530 + i, PINK if i % 2 else LIME, 0.95, slit=True),
                    segments=(20, 10))
        hook = pts[-1] + out * 0.02
        b.tube("Hook", [hook, hook + out * 0.06 + Vector((0, 0, -0.01)), hook + out * 0.08 + Vector((0, 0, -0.06))],
               [0.014, 0.01, 0.002], sides=6, style=horn_style(4540 + i))
    return b


def gill_style(seed, glow=LIME):
    """Canopy and caps: plum on top with paler rims; lit radial gills underneath, light leaking through ribs."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        L = C.local_points(part, p)
        ang = np.arctan2(L[:, 1], L[:, 0])
        r = np.hypot(L[:, 0], L[:, 1])
        under = C.smoothstep(-0.1, -0.6, n[:, 2])
        gills = (np.abs(np.sin(ang * 14)) > 0.75) * under * C.smoothstep(0.2, 0.5, r)
        ribs = (np.abs(np.sin(ang * 8)) > 0.9) * C.smoothstep(-0.3, 0.3, -np.abs(n[:, 2]) + 0.2)
        col = C.lerp3(PLUM, PLUM_PALE, C.smoothstep(0.75, 1.0, r) * 0.6)
        col = C.lerp3(col, PLUM_DARK, under * 0.8)
        light = np.clip(gills * 0.85 + ribs * 0.7 + under * 0.15, 0, 1)
        col = C.lerp3(col, glow * 0.7, light)
        s.albedo[idx] = col * (1 + 0.1 * C.value_noise(p, 25, seed))[:, None]
        s.emit[idx] = np.asarray(glow)[None, :] * light[:, None]
        s.disp[idx] = -0.003 * gills
        s.rough[idx] = 0.45
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = r * 0.3
    return style


def dome(depth):
    """Deform: a flat disc whose top domes up and whose underside is nearly flat (an umbrella or lantern cap)."""
    def deform(v):
        r2 = v.x * v.x + v.y * v.y
        z = v.z if v.z > 0 else v.z * depth
        return Vector((v.x, v.y, z + 0.4 * (1 - r2)))
    return deform


def glow_tree():
    b = C.PartBuilder()
    rng = np.random.default_rng(4601)
    height = 1.35
    for k in range(3):
        pts = []
        for j in range(7):
            t = j / 6
            a = k * 2 * math.pi / 3 + t * 2.2
            r = 0.06 + 0.1 * (1 - t) ** 2
            pts.append(Vector((math.cos(a) * r, math.sin(a) * r, t * height)))
        b.tube("Strand", pts, [0.06, 0.045, 0.04, 0.035, 0.035, 0.035, 0.03], sides=10,
               style=flesh(4610 + k, PINK, tip_glow=0.0, rib=0.25, bands=11))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.5
        out = Vector((math.cos(a), math.sin(a), 0))
        b.tube("Root", [Vector((0, 0, 0.15)), out * 0.15 + Vector((0, 0, 0.05)), out * 0.32 + Vector((0, 0, -0.02))],
               [0.05, 0.035, 0.01], sides=8, style=flesh(4620 + i, PINK, tip_glow=0.0, rib=0.0))
    b.ellipsoid("Canopy", Vector((0, 0, height)), (0.62, 0.58, 0.07), style=gill_style(4630, LIME), segments=(48, 16),
                deform=dome(0.3))
    for i in range(9):
        a = i / 9 * 2 * math.pi + rng.uniform(-0.2, 0.2)
        r = rng.uniform(0.3, 0.52)
        top = Vector((math.cos(a) * r, math.sin(a) * r, height - 0.02))
        drop = rng.uniform(0.22, 0.45)
        bot = top + Vector((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), -drop))
        b.tube("Filament", [top, top.lerp(bot, 0.5) + Vector((0.02, 0, 0)), bot], [0.022, 0.016, 0.014], sides=6,
               style=flesh(4640 + i, PINK if i % 2 else LIME, tip_glow=0.6, rib=0.0))
        b.ellipsoid("Bead", bot + Vector((0, 0, -0.03)), (0.04, 0.04, 0.05), style=bulb_style(4650 + i, PINK if i % 2 else LIME, 1.0),
                    segments=(12, 6))
    return b


def mushroom():
    b = C.PartBuilder()
    rng = np.random.default_rng(4701)
    for i, (x, y, h, cap) in enumerate(((0, 0, 0.78, 0.27), (0.3, 0.12, 0.45, 0.17), (-0.2, 0.22, 0.3, 0.12))):
        root = Vector((x, y, 0))
        lean = Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0))
        top = root + lean + Vector((0, 0, h))
        b.tube("Stalk", [root, root + lean * 0.4 + Vector((0, 0, h * 0.5)), top], [cap * 0.3, cap * 0.22, cap * 0.2],
               sides=10, style=flesh(4710 + i, LIME, tip_glow=0.0, rib=0.15, bands=5))
        b.ellipsoid("Cap", top, (cap, cap, cap * 0.45), rz(rng.uniform(0, 90)), style=gill_style(4720 + i, PINK if i else LIME),
                    segments=(40, 16), deform=dome(0.25))
        if i == 0:
            for k in range(5):
                a = k / 5 * 2 * math.pi
                base = top + Vector((math.cos(a) * cap * 0.4, math.sin(a) * cap * 0.4, cap * 0.55))
                b.tube("Spine", [base, base + Vector((math.cos(a) * 0.05, math.sin(a) * 0.05, 0.1))], [0.02, 0.002],
                       sides=6, style=horn_style(4730 + k))
    b.ellipsoid("Mound", Vector((0.05, 0.08, 0.0)), (0.3, 0.26, 0.05), style=flesh(4740, LIME, tip_glow=0.0, rib=0.0),
                segments=(20, 10))
    return b


# ------------------------------------------------------------------------------------------------------- eggs

def egg_style(seed):
    top, mid, dark = EGG

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        L = C.local_points(part, p)
        col = C.lerp3(dark, mid, C.smoothstep(-0.9, 0.0, L[:, 2]))
        col = C.lerp3(col, top, C.smoothstep(0.2, 1.0, L[:, 2]) * 0.7)
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        e = C.voronoi_edge(u, C.fibonacci_sphere(26, seed=seed))
        vein = 1 - C.smoothstep(0.0, 0.05, e)
        glow = C.smoothstep(-0.3, 0.6, L[:, 2])
        col = C.lerp3(col, EGG_GLOW * 0.8, vein * 0.7)
        s.albedo[idx] = col * (1 + 0.1 * C.value_noise(p, 30, seed))[:, None]
        s.emit[idx] = EGG_GLOW[None, :] * (vein * (0.35 + 0.6 * glow))[:, None] + PINK[None, :] * (glow * 0.12)[:, None]
        s.emit[idx] = np.clip(s.emit[idx], 0, 1)
        s.disp[idx] = 0.003 * vein
        s.rough[idx] = 0.4
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = L[:, 2]
    return style


def resin_style(seed):
    def style(s, idx, part):
        p = s.p[idx]
        s.albedo[idx] = np.array((0.18, 0.06, 0.06)) * (1 + 0.2 * C.value_noise(p, 20, seed))[:, None]
        s.emit[idx] = EGG_GLOW[None, :] * (0.08 * (1 + C.value_noise(p, 6, seed + 1)))[:, None].clip(0, 1)
        s.disp[idx] = 0.002 * C.value_noise(p, 30, seed + 2)
        s.rough[idx] = 0.25
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = p[:, 2]
    return style


def egg_cluster():
    b = C.PartBuilder()
    rng = np.random.default_rng(4801)
    b.ellipsoid("Resin", Vector((0, 0, 0.0)), (0.42, 0.38, 0.06), style=resin_style(4810), segments=(32, 12))
    eggs = []
    for i in range(6):
        a = i / 6 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        r = 0.0 if i == 0 else rng.uniform(0.17, 0.26)
        size = rng.uniform(0.1, 0.13) * (1.15 if i == 0 else 1.0)
        c = Vector((math.cos(a) * r, math.sin(a) * r, size * 1.2))
        tilt = Matrix.Rotation(rng.uniform(-0.35, 0.35), 3, "X") @ Matrix.Rotation(rng.uniform(-0.35, 0.35), 3, "Y")
        b.ellipsoid("Egg", c, (size, size, size * 1.45), tilt, style=egg_style(4820 + i), segments=(28, 14))
        eggs.append(c)
    for i in range(4):
        p0, p1 = eggs[i], eggs[i + 1]
        mid = p0.lerp(p1, 0.5) + Vector((0, 0, -0.04))
        b.tube("Strand", [p0 + Vector((0, 0, -0.05)), mid, p1 + Vector((0, 0, -0.05))], [0.018, 0.012, 0.018], sides=6,
               style=resin_style(4840 + i))
    return b


# ------------------------------------------------------------------------------------------------ silhouettes

def silhouette_crystal_tower():
    b = C.PartBuilder()
    b.ellipsoid("Base", Vector((0, 0, 0.4)), (2.6, 2.2, 1.4), style=E.stone(E.BASALT, 4910, veins=0.4, strata=3),
                segments=(32, 16), deform=C.chiselled(18, 4910, 0.2))
    for i, (x, y, h, r, lean) in enumerate(((0, 0, 9.0, 0.95, (0.08, 0.02)), (1.3, -0.6, 6.0, 0.6, (0.35, -0.1)),
                                            (-1.1, 0.5, 5.0, 0.55, (-0.3, 0.15)))):
        b.shard("Tower", Vector((x, y, 0.8)), Vector((lean[0], lean[1], 1)), r, h, sides=7, twist=1.4, flute=0.35,
                rings=14, style=Y.shard_style("pink" if i != 1 else "ice", 0.55, seed=4920 + i))
    return b


def silhouette_ridge():
    b = C.PartBuilder()
    L, W = 24.0, 5.0

    def height(x):
        peaks = 3.2 + 1.6 * E.periodic(x, L, 5, 5001) + 1.2 * abs(E.periodic(x * 3, L * 3, 3, 5002))
        return max(peaks, 1.0)

    def top(u, v):
        x = -L / 2 + L * u
        y = -W / 2 + W * v
        prof = max(0.0, 1 - abs(y) / (W / 2)) ** 1.2
        edge = float(C.smoothstep(0.0, 0.08, np.array(min(u, 1 - u))))
        return Vector((x, y, height(x) * prof * edge + 0.05))

    def bottom(u, v):
        return Vector((-L / 2 + L * u, -W / 2 + W * v, -0.3))

    b.slab("Ridge", top, bottom, 160, 30, style=E.stone(E.BASALT, 5010, veins=0.25, strata=2.5, crack=0.4))
    for i, x in enumerate((-6.0, 2.5, 7.5)):
        b.shard("Needle", Vector((x, 0.2, height(x) * 0.7)), Vector((0.1 * i - 0.1, 0.05, 1)), 0.5, 3.0, sides=6,
                twist=1.2, flute=0.2, style=E.stone(E.BASALT, 5020 + i, veins=0.5, strata=3))
        b.shard("Glint", Vector((x + 0.2, 0.0, height(x) * 0.7 + 1.0)), Vector((0.5, -0.3, 1)), 0.2, 1.2, sides=6,
                twist=1.0, flute=0.3, style=Y.shard_style("pink", 0.6, seed=5030 + i))
    return b


def silhouette_mesa():
    b = C.PartBuilder()
    rng = np.random.default_rng(5101)
    b.ellipsoid("Butte", Vector((0, 0, 2.2)), (3.6, 3.0, 2.8), style=E.stone(E.BASALT, 5110, veins=0.3, strata=2.5, dust=0.5),
                segments=(40, 20), deform=C.chiselled(14, 5110, 0.12, squash=(0.55, 1.0)))
    for i in range(5):
        a = i / 5 * 2 * math.pi + 0.3
        c = Vector((math.cos(a) * 3.2, math.sin(a) * 2.7, 1.6))
        plate(b, rng, c, a, (1.4, 0.5, 1.9), 5120 + i, lean=-15, veins=0.4)
    b.shard("Needle", Vector((1.2, 0.5, 4.2)), Vector((0.2, 0.1, 1)), 0.45, 2.6, sides=6, twist=1.3, flute=0.2,
            style=E.stone(E.BASALT, 5130, veins=0.6, strata=3))
    b.shard("Glint", Vector((-1.0, -0.4, 4.3)), Vector((-0.3, -0.2, 1)), 0.3, 1.6, sides=7, twist=1.4, flute=0.35,
            style=Y.shard_style("ice", 0.6, seed=5140))
    return b


FLORA = {"Flora_Frond": 0.75, "Flora_Tendril": 1.0, "Flora_GlowTree": 1.75, "Flora_Mushroom": 1.0}

ASSETS = {
    # name: (builder, target tris, voxel, smoothing, sharp angle, families, k)
    "Decor_Boulder": (boulder, 1200, 0.011, 2, None, ("rock", "crystal"), 1.0),
    "Decor_Column": (column, 1000, 0.01, 2, None, ("rock", "crystal"), 1.0),
    "Decor_Spike": (spike, 1200, 0.008, 1, None, ("rock", "crystal"), 1.0),
    "Decor_Spire": (spire, 1500, 0.016, 2, None, ("rock", "crystal"), 1.5),
    "Flora_Frond": (frond, 1300, 0.006, 1, None, ("membrane", "chitin"), 1.0),
    "Flora_Tendril": (tendril, 1300, 0.006, 1, None, ("membrane", "chitin"), 1.0),
    "Flora_GlowTree": (glow_tree, 1500, 0.008, 1, None, ("membrane", "chitin"), 1.0),
    "Flora_Mushroom": (mushroom, 1400, 0.007, 1, None, ("membrane", "chitin"), 1.0),
    "Egg_Cluster": (egg_cluster, 1300, 0.007, 2, None, ("membrane", "chitin"), 1.0),
    "Silhouette_CrystalTower": (silhouette_crystal_tower, 900, 0.06, 1, 30, ("rock", "crystal"), 10.0),
    "Silhouette_Ridge": (silhouette_ridge, 1200, 0.08, 1, None, ("rock", "crystal"), 10.0),
    "Silhouette_Mesa": (silhouette_mesa, 1000, 0.07, 1, None, ("rock", "crystal"), 10.0),
}


def previews(built, out):
    C.preview_world((0.02, 0.02, 0.035))
    C.ground(color=(0.05, 0.05, 0.07))
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, asset in built.items():
        for other in built.values():
            other.low.hide_render = other is not asset
        dims = asset.low.dimensions
        size = max(dims.x, dims.y, dims.z)
        cam = C.closeup_camera(Vector((0, 0, dims.z * 0.42)), size * 2.2, azimuth=-30, elevation=22, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 640, 480))
        bpy.data.objects.remove(cam)
    if len(paths) > 1:
        C.contact_sheet(paths, os.path.join(out, "sheet_env_decor.png"), 4, 400)
    C.hide_lights(rig, True)


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    only = C.cli_args().get("only")
    built = {}
    for name, (fn, tris, voxel, smoothing, sharp, families, k) in ASSETS.items():
        if only and name not in only.split(","):
            continue
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, sharp=sharp, families=families,
                        emission=2.2, k=k, ao=0.3 * max(1.0, k / 3), extrusion=0.03 * max(1.0, k / 2),
                        ray=0.1 * max(1.0, k / 2))
        extra = None
        if name in FLORA:
            E.write_sway(asset.low, FLORA[name], seed=len(built))
            extra = {"sway": {"attribute": "Sway", "height": FLORA[name],
                              "channels": "R bend weight (root 0, top 1), G phase 0..1, B flutter"}}
        E.finish(asset, extra)
        built[name] = asset
    C.save_blend("Env_Decor" if not only else "Env_Decor_" + only.replace(",", "_"))
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
