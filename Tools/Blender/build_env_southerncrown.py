"""Southern Crown environment set (round 17, continent 5: ice to the south pole, everything mixed; the seal is
cracking). See ArtSource/DESIGN_NOTES.md round 17.

Tiling sets (Exports/Env/SouthernCrown/Tiling/South_<Set>/): Road (pale cool-grey slabs, a few long cracks with
magenta pinpoints), Ground (dark glassy black ice over rock, a network of fine charged veins), GroundAlt (mixed
rubble: stone, ash and ice), CrackedIce (pale shattered pack ice, magenta light in the widest cracks), Cliff
(heaved plates of ice and rock, V up, glowing seams).
Kit: RoadEdge_* (pale granite with charged cracks), Bank_* (broken ice plates tilted up at the shore), Cliff_*
(a pressure ridge of heaved ice and rock plates).
Decor: UnstableCrystal (splinters float free), CrackedVein (flat, any yaw), PressureRidge, ShatteredPillar,
MixedBoulder (half frozen, half scorched), Scatter_Shards.

Run: blender --background --factory-startup --python Tools/Blender/build_env_southerncrown.py -- --preview <dir>
     [--part materials|kit|decor|diorama|all] [--only Road,Cliff_Straight] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
from env_kit import bn, ss, mix, rgb  # noqa: E402

CONT = K.Continent("SouthernCrown", "South", {"Road": 2.0, "Ground": 3.0, "GroundAlt": 2.0, "CrackedIce": 3.0,
                                              "Cliff": 3.0})

CHARGE = np.array((1.0, 0.3, 0.8))
CHARGE_HOT = np.array((1.0, 0.8, 0.95))
EMBER = np.array((1.0, 0.5, 0.18))
ICE = np.array((0.62, 0.7, 0.8))
FROST = np.array((0.8, 0.86, 0.94))

GRANITE = K.pal((0.5, 0.52, 0.56), (0.28, 0.3, 0.34), (0.06, 0.07, 0.09), (0.06, 0.06, 0.08))
BLACKICE = K.pal((0.16, 0.19, 0.25), (0.08, 0.1, 0.14), (0.02, 0.03, 0.05), (0.02, 0.02, 0.04))
PACK = K.pal((0.7, 0.76, 0.84), (0.44, 0.52, 0.62), (0.12, 0.16, 0.22), (0.18, 0.2, 0.3))
SCORCH = K.pal((0.24, 0.2, 0.2), (0.13, 0.1, 0.1), (0.04, 0.03, 0.03), (0.03, 0.02, 0.02))


# ------------------------------------------------------------------------------------------------ tiling sets

def road(n):
    """Pale cool-grey slabs, big and angular; a few long cracks run across them with magenta pinpoints."""
    d1, edge, val, _ = E.voronoi(n, 3, 711, jitter=0.6, warp=0.05)
    joint = 1 - ss(0.0, 0.025, edge)
    bevel = ss(0.012, 0.09, edge)
    _, e2, _, _ = E.voronoi(n, 5, 712, warp=0.4)
    crack = (1 - ss(0.0, 0.012, e2)) * (bn(n, 1, 3, 713) > 0.15) * bevel
    pin = crack * (bn(n, 8, 20, 714) > 0.55)
    scuff = E.scratches(n, 80, 715)
    pits = E.D.pits(n, 60, 716, 0.25, radius=0.3)
    frost, _ = E.grains(n, 300, 717, 0.6, radius=0.36)
    frost = frost * (1 - ss(0.01, 0.06, edge))
    h = (0.01 * bevel + 0.004 * (val - 0.5) - 0.012 * joint - 0.004 * crack + 0.0012 * bn(n, 40, 160, 718)
         + 0.0015 * bn(n, 6, 20, 719) - 0.0015 * pits + 0.002 * frost)
    tone = 0.9 + 0.2 * (val - 0.5) + 0.05 * bn(n, 3, 10, 720)
    col = rgb((0.56, 0.58, 0.62), n) * tone[..., None]
    col = mix(col, rgb((0.24, 0.25, 0.3), n), 0.45 * (1 - ss(0.03, 0.15, edge)))
    col = mix(col, rgb((0.07, 0.07, 0.1), n), joint)
    col = mix(col, rgb((0.14, 0.12, 0.17), n), 0.8 * crack)
    col = mix(col, rgb(CHARGE * 0.7, n), pin)
    col = mix(col, rgb((0.33, 0.34, 0.37), n), 0.5 * pits)
    col = mix(col, rgb(FROST, n), frost * 0.8)
    col = mix(col, rgb((0.76, 0.78, 0.82), n), 0.5 * scuff)
    smooth = np.clip(0.38 - 0.3 * joint - 0.15 * crack + 0.1 * scuff + 0.15 * frost, 0.05, 1)
    emission = rgb(CHARGE, n) * (pin * 0.8)[..., None]
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=pin, ao=ao, emission=emission)


def ground(n):
    """Dark glassy black ice over rock: smooth, with trapped bubbles, cut by a network of fine charged veins."""
    _, e, val, _ = E.voronoi(n, 5, 731, jitter=0.9, warp=0.35)
    vein = 1 - ss(0.0, 0.012, e)
    _, e2, _, _ = E.voronoi(n, 14, 732, warp=0.35)
    fine = (1 - ss(0.0, 0.01, e2)) * (bn(n, 1, 4, 733) > 0.1)
    charge = ss(0.0, 0.5, bn(n, 2, 5, 734))
    bubbles, bv = E.grains(n, 260, 735, 0.35, radius=0.3)
    rockshow = ss(-0.1, 0.5, bn(n, 2, 8, 736))
    h = 0.01 * bn(n, 1, 4, 737) - 0.005 * vein - 0.002 * fine + 0.004 * rockshow + 0.001 * bn(n, 40, 200, 738)
    col = rgb((0.1, 0.12, 0.17), n) * (0.85 + 0.3 * (val - 0.5))[..., None]
    col = mix(col, rgb((0.2, 0.19, 0.2), n), rockshow * 0.5)
    col = mix(col, rgb((0.3, 0.38, 0.48), n), bubbles * 0.6)
    lit = np.clip(vein * (0.15 + 0.85 * charge) * ss(-0.2, 0.2, bn(n, 1, 3, 739)) + fine * charge * 0.35, 0, 1)
    col = mix(col, rgb(CHARGE * 0.6, n), lit)
    smooth = np.clip(0.75 - 0.4 * rockshow + 0.1 * bubbles, 0.05, 0.95)
    emission = rgb(CHARGE, n) * (lit * 0.5)[..., None]
    ao = E.cavity_ao(h, 6, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=lit, ao=ao, emission=emission)


def ground_alt(n):
    """Mixed rubble of every element: dark stone, scorched ash pebbles, ice chips, a few rust grains."""
    g1, v1 = E.grains(n, 80, 741, 0.7, radius=0.45)
    g2, v2 = E.grains(n, 200, 742, 0.8, radius=0.4)
    g3, v3 = E.grains(n, 420, 743, 0.6, radius=0.35)
    h = 0.012 * g1 + 0.006 * g2 + 0.002 * g3 + 0.008 * bn(n, 1, 4, 744) + 0.001 * bn(n, 40, 200, 745)
    col = rgb((0.1, 0.1, 0.12), n)
    palette = [np.array(c) for c in ((0.26, 0.26, 0.29), (0.18, 0.14, 0.13), (0.55, 0.62, 0.7), (0.36, 0.22, 0.14))]
    for g, v in ((g3, v3), (g2, v2), (g1, v1)):
        kind = np.floor(((v * 37) % 1.0) * 4).astype(int)
        tint = np.choose(kind[..., None], palette)
        col = mix(col, tint * (0.8 + 0.4 * ((v * 13) % 1))[..., None], np.clip(g * 1.5, 0, 1))
    ice = np.clip((g1 + g2) * 1.5, 0, 1) * (np.floor(((v1 * 37) % 1.0) * 4) == 2)
    smooth = np.clip(0.12 + 0.5 * ice, 0.03, 1)
    ao = E.cavity_ao(h, 4, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=ice, ao=ao)


def cracked_ice(n):
    """Shattered pack ice, blue and glossy: small plates at slightly different heights, dark fracture lines and
    hairlines, frost on the plate rims, magenta light in the widest cracks. Value capped below the road (about 0.38
    against 0.49), and busier and bluer, so it never reads as paving."""
    d1, e, val, _ = E.voronoi(n, 8, 751, jitter=0.95, warp=0.25)
    gap = 1 - ss(0.0, 0.035, e)
    wide = 1 - ss(0.0, 0.014, e)
    _, e2, _, _ = E.voronoi(n, 22, 752, warp=0.3)
    hair = (1 - ss(0.0, 0.01, e2)) * (1 - gap)
    rim = (1 - ss(0.02, 0.09, e)) * (1 - gap)
    h = 0.012 * (val - 0.5) - 0.015 * gap - 0.002 * hair + 0.002 * rim + 0.001 * bn(n, 40, 200, 753)
    charge = ss(0.2, 0.6, bn(n, 2, 5, 754))
    col = rgb((0.34, 0.42, 0.54), n) * (0.8 + 0.3 * val)[..., None]
    col = mix(col, rgb((0.52, 0.58, 0.66), n), 0.6 * rim)
    col = mix(col, rgb((0.07, 0.09, 0.15), n), gap)
    col = mix(col, rgb((0.2, 0.26, 0.36), n), hair * 0.8)
    lit = wide * charge
    col = mix(col, rgb(CHARGE * 0.7, n), lit)
    col = np.minimum(col, 0.62)
    smooth = np.clip(0.7 + 0.1 * val - 0.4 * gap - 0.3 * rim, 0.05, 0.95)
    emission = rgb(CHARGE, n) * (lit * 0.8)[..., None]
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=lit, ao=ao, emission=emission)


def cliff(n):
    """Heaved plates (V up): angular facets of ice and rock leaning at different angles, charged seams."""
    d1, e, val, _ = E.voronoi(n, 3, 761, jitter=0.7, warp=0.08, stretch=(2, 1))
    seam = 1 - ss(0.0, 0.03, e)
    u, v = E.coords(n)
    ang = val * 6.28
    facet = (np.cos(ang) * (u * 6 % 1.0) + np.sin(ang) * (v * 3 % 1.0)) * 0.5
    ice = ((val * 11.3) % 1.0) > 0.5
    charge = ss(0.1, 0.5, bn(n, 1, 3, 762))
    h = 0.03 * facet - 0.03 * seam + 0.004 * bn(n, 30, 120, 763) + 0.006 * bn(n, 3, 9, 764)
    nrm = E.normal_from_height(h, CONT.tiles["Cliff"])
    up = nrm[..., 1]
    col = np.where(ice[..., None], rgb((0.4, 0.47, 0.57), n), rgb((0.22, 0.22, 0.26), n))
    col = col * (0.8 + 0.3 * ((val * 5.7) % 1.0) + 0.06 * bn(n, 4, 12, 765))[..., None]
    col = mix(col, rgb(FROST, n), 0.5 * ss(0.2, 0.6, up))
    col = mix(col, rgb((0.04, 0.04, 0.07), n), seam * 0.9)
    lit = seam * charge
    col = mix(col, rgb(CHARGE * 0.6, n), lit * 0.7)
    smooth = np.clip(np.where(ice, 0.6, 0.15) - 0.1 * seam, 0.03, 1)
    emission = rgb(CHARGE, n) * (lit * 0.75)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=lit, ao=ao, emission=emission)


SETS = {"Road": road, "Ground": ground, "GroundAlt": ground_alt, "CrackedIce": cracked_ice, "Cliff": cliff}


# ------------------------------------------------------------------------------------------------ kit

def kerb_style(seed):
    return K.rock(GRANITE, seed, strata=30, crack=0.55, crack_glow=0.55, crack_color=CHARGE, frost=0.3, rough=0.7,
                  smooth_top=0.1)


def pack_style(seed, glow=0.6):
    base = K.rock(PACK, seed, strata=0.0, crack=0.9, rough=0.3, crack_glow=glow, crack_color=CHARGE, frost=0.25)

    def style(s, idx, part):
        base(s, idx, part)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def rock_style(seed, glow=0.6):
    return K.rock(BLACKICE, seed, strata=18, crack=0.5, crack_glow=glow, crack_color=CHARGE, frost=0.2, rough=0.6)


def plate(b, centre, size, yaw, pitch, seed, style, chips=6):
    rot = K.rz(yaw) @ Matrix.Rotation(math.radians(pitch), 3, "X")
    b.ellipsoid("Plate", Vector(centre), size, rot, style=style, segments=(28, 14), deform=E.boxy(seed, 0.18, chips, 0.18))


def bank_accent(b, rng, kind):
    """Broken ice plates tilted up along the shore."""
    count = {"straight": 3, "outer": 1, "inner": 1}[kind]
    for i in range(count):
        if kind == "straight":
            c = (-0.32 + i * 0.32 + rng.uniform(-0.04, 0.04), -0.06, 0.03)
            yaw = rng.uniform(-15, 15)
        else:
            c = (-0.06, -0.06, 0.03)
            yaw = 45
        plate(b, c, (rng.uniform(0.11, 0.15), rng.uniform(0.07, 0.09), 0.025), yaw, rng.uniform(18, 32), 7450 + i,
              pack_style(7450 + i))


def shard_plate(b, centre, size, yaw, pitch, roll, seed, style):
    """A broken, angular slab (a flattened chiselled polyhedron: jagged outline, sharp facets)."""
    rot = K.rz(yaw) @ Matrix.Rotation(math.radians(pitch), 3, "X") @ Matrix.Rotation(math.radians(roll), 3, "Y")
    b.ellipsoid("Plate", Vector(centre), size, rot, style=style, segments=(24, 12),
                deform=C.chiselled(11, seed, 0.3, squash=(0.85, 1.0)))


def ridge_piece(kind):
    """A pressure ridge: broken angular plates of pale pack ice and dark rock heaved up at odd angles and leaning on
    each other, charged seams between them, dark broken blocks behind."""
    b = C.PartBuilder()
    rng = np.random.default_rng({"straight": 7610, "outer": 7620, "inner": 7630}[kind])
    if kind == "straight":
        path = K.straight_path(1.0)
    elif kind == "outer":
        path = K.cliff_outer_path(0.6)
    else:
        path = K.cliff_inner_path(0.4)
    length = K.path_length(path)
    rows = ((0.02, (0.5, 0.8), (25, 50)), (-0.3, (0.9, 1.25), (10, 30)), (-0.62, (1.15, 1.55), (-10, 20)))
    for r, (depth, (h0, h1), (p0, p1)) in enumerate(rows):
        count = max(2, int(round(length / 0.38)))
        for i in range(count):
            t = (i + 0.5 + rng.uniform(-0.2, 0.2)) / count
            c = K.along(path, t, depth + rng.uniform(-0.05, 0.05), 0.0)
            hgt = rng.uniform(h0, h1)
            yaw = math.degrees(K.facing(path, t)) + 90 + rng.uniform(-25, 25)
            ice = (i + r) % 3 != 0
            style = pack_style(7640 + r * 20 + i) if ice else rock_style(7640 + r * 20 + i)
            c.z = hgt * 0.4
            shard_plate(b, c, (rng.uniform(0.2, 0.3), 0.055, hgt * 0.55), yaw, -rng.uniform(p0, p1),
                        rng.uniform(-18, 18), 7640 + r * 20 + i, style)
    for i in range(3 if kind == "straight" else 2):
        c = K.along(path, rng.uniform(0.1, 0.9), rng.uniform(0.2, 0.4), 0.0)
        K.chip(b, c, (0.08, 0.06, 0.05), 7700 + i, pack_style(7700 + i))
    backs = [(t, -1.0) for t in np.linspace(0.2, 0.8, 2)]
    for i, (t, d) in enumerate(backs):
        b.ellipsoid("Block", K.along(path, t, d, 0.75), (0.6, 0.42, 0.75), K.rz(rng.uniform(0, 90)),
                    style=rock_style(7720 + i, 0.5), segments=(24, 12), deform=C.chiselled(12, 7720 + i, 0.25))
    if kind == "outer":
        b.ellipsoid("Block", Vector((0.95, 0.95, 0.75)), (0.62, 0.62, 0.75), K.rz(30), style=rock_style(7730, 0.5),
                    segments=(24, 12), deform=C.chiselled(12, 7730, 0.25))
    return b


# ------------------------------------------------------------------------------------------------ decor

def unstable_crystal():
    """An overcharged Crysium cluster: cracked through, white-hot inside, splinters floating free above it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(8001)
    b.ellipsoid("Base", Vector((0, 0, 0.04)), (0.32, 0.28, 0.12), style=rock_style(8010, 1.0), segments=(28, 14),
                deform=C.chiselled(12, 8010, 0.2))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        length = rng.uniform(0.4, 0.65) * (1.4 if i == 0 else 1.0)
        b.shard("Shard", out * (0.03 if i == 0 else 0.12) + Vector((0, 0, 0.08)), out * (0.1 if i == 0 else 0.5) + Vector((0, 0, 1)),
                rng.uniform(0.07, 0.1) * (1.3 if i == 0 else 1.0), length, sides=7, twist=rng.uniform(1.2, 2.2),
                flute=0.35, style=Y.shard_style("overcharge", 1.1, seed=8020 + i))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        c = Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, 0.85 + 0.12 * (i % 2)))
        b.shard("Float", c, Vector((math.cos(a) * 0.4, math.sin(a) * 0.4, 1)), 0.03, rng.uniform(0.1, 0.16), sides=6,
                twist=1.0, flute=0.3, style=Y.shard_style("overcharge", 1.2, seed=8040 + i))
    return b


def cracked_vein():
    """The ground split open along a charged vein: two heaved lips with crystal packed in the fissure (2 x 0.8,
    flat, any yaw)."""
    b = C.PartBuilder()
    L = 2.0

    def lip(sign, seed):
        def out(t, s):
            pts = [(-0.38, -0.03), (-0.2, 0.03), (-0.06, 0.07), (-0.02, 0.04), (0.0, -0.05)]
            d, z = K.polyline(pts, s)
            w = math.sin(math.pi * t) ** 0.6
            return d, z * w - 0.01 * (1 - w)

        def inn(t, s):
            return K.polyline([(-0.36, -0.06), (-0.02, -0.07)], s)
        x0 = -L / 2
        path = E.line_path(x0, -x0, 0.0, (0.0, sign))

        def path2(t):
            (x, y), nrm = path(t)
            return (x, y - sign * (0.05 + 0.025 * math.sin(t * 9 + seed))), nrm
        E.sweep(b, "Lip", path2, out, inn, 90, 14, rock_style(seed, 0.4),
                lambda t, s, d, z: (0.02 * E.periodic(t * L, L, 4, seed), 0.0))
    lip(1.0, 8110)
    lip(-1.0, 8111)
    rng = np.random.default_rng(8120)
    for i in range(7):
        x = -0.85 + i * 0.28 + rng.uniform(-0.05, 0.05)
        b.shard("Vein", Vector((x, rng.uniform(-0.02, 0.02), -0.04)), Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), 1)),
                0.04, rng.uniform(0.08, 0.15), sides=6, twist=1.0, flute=0.3, style=Y.shard_style("overcharge", 1.0, seed=8130 + i))
    b.ellipsoid("Glow", Vector((0, 0, -0.02)), (0.92, 0.05, 0.03), style=K.glow_seam(CHARGE, 0.9, CHARGE * 0.6), segments=(40, 8))
    return b


def pressure_ridge():
    """A short ridge of broken angular ice plates shoved up against each other, one charged seam."""
    b = C.PartBuilder()
    rng = np.random.default_rng(8201)
    for i in range(6):
        x = -0.6 + i * 0.24 + rng.uniform(-0.04, 0.04)
        h = rng.uniform(0.35, 0.6) * (1.25 if i in (2, 3) else 1.0)
        side = 1 if i % 2 else -1
        shard_plate(b, (x, side * 0.05, h * 0.36), (0.16, 0.05, h * 0.55), rng.uniform(-25, 25),
                    side * rng.uniform(15, 35), rng.uniform(-20, 20), 8210 + i,
                    pack_style(8210 + i, 0.9 if i == 2 else 0.4) if i != 4 else rock_style(8210 + i))
    for i in range(3):
        K.chip(b, (rng.uniform(-0.6, 0.6), rng.uniform(-0.3, 0.3), 0.0), (0.06, 0.05, 0.04), 8230 + i, pack_style(8230 + i))
    return b


def shattered_pillar():
    """A banded ice-and-rock pillar snapped off: a jagged stump with charged splinters bursting from the break, the
    top lying beside it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(8301)
    b.prism("Stump", Vector((0, 0, -0.05)), Vector((0.05, 0.02, 1)), 0.2, 0.55, 0.02, sides=6, twist=0.3,
            style=rock_style(8310, 0.9), taper=0.92)
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        out = Vector((math.cos(a), math.sin(a), 0))
        b.ellipsoid("Jag", out * 0.1 + Vector((0.03, 0.01, 0.52 + 0.05 * (i % 2))), (0.08, 0.06, 0.1),
                    K.rz(math.degrees(a)), style=rock_style(8315 + i, 0.6), segments=(14, 8),
                    deform=C.chiselled(8, 8315 + i, 0.35))
    for i in range(3):
        a = i / 3 * 2 * math.pi + 1.0
        out = Vector((math.cos(a), math.sin(a), 0))
        b.shard("Burst", Vector((0.03, 0.01, 0.5)) + out * 0.04, out * 0.8 + Vector((0, 0, 1)), 0.035, rng.uniform(0.14, 0.22),
                sides=6, twist=1.2, flute=0.3, style=Y.shard_style("overcharge", 0.9, seed=8312 + i))
    b.prism("Top", Vector((0.2, -0.28, 0.12)), Vector((1, -0.35, 0.15)), 0.17, 0.6, 0.12, sides=6, twist=0.8,
            style=pack_style(8311, 0.9), taper=0.85)
    for i in range(3):
        K.chip(b, (rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.1), 0.0), (0.06, 0.05, 0.04), 8320 + i, pack_style(8320 + i))
    return b


def mixed_style(seed):
    """Half frozen (+X side: ice crust and frost), half scorched (-X side: ash and ember cracks)."""
    cold = pack_style(seed, 0.3)
    hot = K.rock(SCORCH, seed + 1, crack_glow=1.0, crack_color=EMBER, crack=0.6, strata=30)

    def style(s, idx, part):
        cold(s, idx, part)
        a_alb, a_emit, a_rough = s.albedo[idx].copy(), s.emit[idx].copy(), s.rough[idx].copy()
        hot(s, idx, part)
        L = C.local_points(part, s.p[idx])
        w = C.smoothstep(-0.25, 0.25, L[:, 0] + 0.25 * K.vn(s.p[idx], 4, seed + 2))
        s.albedo[idx] = C.lerp3(s.albedo[idx], a_alb, w)
        s.emit[idx] = C.lerp3(s.emit[idx], a_emit, w)
        s.rough[idx] = s.rough[idx] * (1 - w) + a_rough * w
    return style


def mixed_boulder():
    b = C.PartBuilder()
    b.ellipsoid("Boulder", Vector((0, 0, 0.22)), (0.4, 0.32, 0.28), K.rz(10), style=mixed_style(8410), segments=(36, 18),
                deform=C.chiselled(14, 8410, 0.18))
    for i, (y, z, r) in enumerate(((0.0, 0.38, 0.0), (-0.15, 0.22, 0.5), (0.17, 0.2, -0.5))):
        b.ellipsoid("Crust", Vector((0.24, y, z)), (0.18, 0.16, 0.09), K.rz(10) @ Matrix.Rotation(0.6 + r * 0.4, 3, "Y"),
                    style=pack_style(8411 + i, 0.2), segments=(20, 10), deform=C.chiselled(10, 8411 + i, 0.25))
    for i in range(3):
        b.tube("Icicle", [Vector((0.3, -0.12 + 0.12 * i, 0.32)), Vector((0.36, -0.12 + 0.12 * i, 0.12))], [0.025, 0.003],
               sides=6, style=pack_style(8420 + i, 0.0))
    return b


def shards():
    b = C.PartBuilder()
    rng = np.random.default_rng(8501)
    for i in range(4):
        c = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), -0.01))
        b.shard("Shard", c, Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), 1)), 0.025, rng.uniform(0.06, 0.12),
                sides=5, twist=0.6, flute=0.2, style=Y.shard_style("overcharge" if i == 0 else "ice", 0.7, seed=8510 + i))
    return b


KIT = {
    "RoadEdge_Straight": dict(fn=lambda: K.kerb_straight(kerb_style(7110), 7110), tris=700, voxel=0.007),
    "RoadEdge_OuterCorner": dict(fn=lambda: K.kerb_outer(kerb_style(7210), 7210), tris=500, voxel=0.007),
    "RoadEdge_InnerCorner": dict(fn=lambda: K.kerb_inner(kerb_style(7310), 7310), tris=400, voxel=0.007),
    "Bank_Straight": dict(fn=lambda: K.bank_straight(pack_style(7410), 7410, bank_accent), tris=600, voxel=0.008,
                          families=("crystal", "rock")),
    "Bank_OuterCorner": dict(fn=lambda: K.bank_outer(pack_style(7510), 7510, bank_accent), tris=400, voxel=0.008,
                             families=("crystal", "rock")),
    "Bank_InnerCorner": dict(fn=lambda: K.bank_inner(pack_style(7560), 7560), tris=300, voxel=0.008,
                             families=("crystal", "rock")),
    "Cliff_Straight": dict(fn=lambda: ridge_piece("straight"), tris=1500, voxel=0.013, smoothing=1),
    "Cliff_OuterCorner": dict(fn=lambda: ridge_piece("outer"), tris=1500, voxel=0.013, smoothing=1),
    "Cliff_InnerCorner": dict(fn=lambda: ridge_piece("inner"), tris=1500, voxel=0.013, smoothing=1),
}

DECOR = {
    "Decor_UnstableCrystal": dict(fn=unstable_crystal, tris=1300, voxel=0.008, smoothing=1, sharp=40,
                                  families=("crystal", "rock")),
    "Decor_CrackedVein": dict(fn=cracked_vein, tris=600, voxel=0.008, families=("rock", "crystal")),
    "Decor_PressureRidge": dict(fn=pressure_ridge, tris=1200, voxel=0.009, smoothing=1, families=("crystal", "rock")),
    "Decor_ShatteredPillar": dict(fn=shattered_pillar, tris=1200, voxel=0.009, smoothing=1),
    "Decor_MixedBoulder": dict(fn=mixed_boulder, tris=1100, voxel=0.009),
    "Scatter_Shards": dict(fn=shards, tris=200, voxel=0.005, sharp=40, families=("crystal", "rock")),
}

DIORAMA = dict(
    sets=dict(ground="Ground", alt="GroundAlt", element="CrackedIce", road="Road"),
    alt_areas=[((1.0, 1.0), 1.8), ((8.0, 0.6), 1.6), ((10.6, 5.6), 1.6)],
    element_areas=[((2.4, 7.3), 2.4), ((6.0, 6.6), 2.0), ((0.3, 2.6), 1.6), ((11.3, 0.4), 1.4)],
    liquid=dict(color=(0.03, 0.08, 0.13), rough=0.03, emit=(0.15, 0.35, 0.5), strength=0.3, z=-0.03, origin="Origin_Water"),
    world=(0.012, 0.012, 0.022),
    mist=dict(density=0.3, color=(0.5, 0.52, 0.66)),
    lights=dict(key=2.2, rim=1.1, key_color=(0.9, 0.92, 1.0), rim_color=(0.7, 0.62, 1.0),
                points=[((1.3, 1.4), (1.0, 0.35, 0.85), 22), ((4.2, 0.5), (1.0, 0.35, 0.85), 16),
                        ((10, 2.5), (0.5, 0.75, 1.0), 25), ((7.0, 6.8), (1.0, 0.35, 0.85), 14)]),
    decor=[("Decor_UnstableCrystal", (1.2, 1.3), 0, 1.0), ("Decor_UnstableCrystal", (11.2, 4.6), 70, 0.8),
           ("Decor_CrackedVein", (5.6, 0.2), 8, 1.0), ("Decor_CrackedVein", (6.2, 6.1), 100, 0.8),
           ("Decor_PressureRidge", (0.4, 2.7), 80, 1.0), ("Decor_PressureRidge", (3.0, 7.0), 10, 0.9),
           ("Decor_ShatteredPillar", (8.0, 0.4), 0, 1.0), ("Decor_ShatteredPillar", (10.7, 5.9), 140, 0.8),
           ("Decor_MixedBoulder", (0.2, 0.2), 0, 1.0), ("Decor_MixedBoulder", (8.0, 4.7), 120, 0.6),
           ("Decor_MixedBoulder", (4.3, 1.6), 0, 0.7), ("@Crystal_A", (7.4, 6.9), 0, 0.8)],
    scatter=["Scatter_Shards"],
    scatter_count=30,
    towers=[("Tower_Railgun", "T2", (2, 4)), ("Tower_Tesla", "T2", (6, 4)), ("Tower_Cryo", "T1", (9, 4)),
            ("Tower_Artillery", "T1", (6, 1)), ("Tower_Gun", "T2", (10, 1))],
    enemies=[("Shardback", 0.85, None, [(1.3, 5.4, -90)]), ("Slagstrider", 0.8, None, [(3.6, 4.0, 180)]),
             ("Mireleech", 0.75, None, [(2.4, 5.8, -95)]), ("Skitter", 0.45, None, [(0.6, 5.7, -90), (4.0, 3.2, 175)]),
             ("Siftjaw", 0.75, None, [(6.8, 2.6, -90)])],
    close_target=(7.0, 4.2),
)


if __name__ == "__main__":
    K.run(CONT, SETS, KIT, DECOR, DIORAMA, world=(0.025, 0.025, 0.04))
