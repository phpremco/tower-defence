"""Gravemaw: the continent-1 boss, "the first giant from the deep" (round 14, see ArtSource/DESIGN_NOTES.md).

A colossal burrower: a drill-maw of five armoured jaw petals (closed: a glowing digging cone; open: a star round a
white-hot gullet ringed with Crysium teeth), six broad overlapping armour segments carrying slabs and a swept-back
crest of Crysium armour, two spade forelimbs, three pairs of short spiked legs and a spade tail with crystal fins.

Two materials (both 1024, plus a 512 Low set): `Gravemaw_Mat` (body: chitin and basalt) and `Gravemaw_Crystal_Mat`
(the Crysium armour, horns, teeth, fins and the crystal debris). One mesh, one rig.

Clips: Crawl (loop), Idle (loop), Hit, Death, Roar, Dive, Emerge. The spine rides a path in Dive and Emerge
(deep_kit.PathFZ): it dives into a hole DIVE_HOLE ahead of the pivot and breaches out of a hole EMERGE_HOLE behind
it; six debris chunks on their own bones (hidden at rest by bone scale) burst out of the hole. Also exports
Gravemaw_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_gravemaw.py -- [--preview <dir>]
     [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
import deep_kit as DK  # noqa: E402
import stone as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Gravemaw"
CRYSTAL = "Gravemaw_Crystal"
LOD_NAME = "Gravemaw_LOD1"
K = 4.5
BODY_TRIS, CRYSTAL_TRIS = 17200, 7400
LOD_TRIS = 10000
TEX, LOW_TEX = 1024, 512
BODY_FAMILIES = ("chitin", "membrane", "rock")
CRYSTAL_FAMILIES = ("crystal", "rock")
UNITY_SCALE = 1.0

# Warm plum-bronze armour (a step lighter than the violet ground so it separates), magenta-pink Crysium light,
# amber sensor pits, pale bone on worn lips and keels.
PAL = H.Palette(top=(0.46, 0.29, 0.31), side=(0.19, 0.1, 0.12), belly=(0.05, 0.03, 0.04), seam=(0.03, 0.018, 0.025),
                dark=(0.1, 0.055, 0.07), glow=(1.0, 0.36, 0.6), eye=(1.0, 0.72, 0.32), membrane=(0.45, 0.1, 0.22),
                bone=(0.86, 0.76, 0.74))
LEG = H.Palette(top=(0.4, 0.26, 0.28), side=(0.16, 0.09, 0.1), belly=(0.05, 0.03, 0.035), seam=PAL.seam,
                dark=PAL.dark, glow=PAL.glow, eye=PAL.eye, membrane=PAL.membrane, bone=PAL.bone)
ROCK = H.Palette(top=(0.38, 0.35, 0.42), side=(0.17, 0.15, 0.2), belly=(0.05, 0.05, 0.06), seam=(0.04, 0.035, 0.05),
                 dark=(0.1, 0.09, 0.11), glow=(1.0, 0.42, 0.72), eye=PAL.eye, bone=(0.6, 0.58, 0.62))

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

# Spine nodes (forward, height): the head tip node, then the joints from the head collar back to the tail.
NODES = [(2.25, 0.46), (1.6, 0.56), (0.98, 0.64), (0.36, 0.66), (-0.26, 0.62), (-0.88, 0.54), (-1.5, 0.44),
         (-2.12, 0.34), (-2.7, 0.24)]
SPINE = [("Head", 1, 0)] + [(f"Seg_{i}", i, i + 1) for i in range(1, 7)] + [("Tail", 7, 8)]
WIDTH = [0.96, 0.9, 0.82, 0.7, 0.56, 0.42]
HEIGHT = [0.36, 0.37, 0.35, 0.32, 0.28, 0.22]

# The maw: petal base centre, tip centre, base radius.
MAW_BASE = G(0, 0.47, 2.1)
MAW_TIP = G(0, 0.34, 2.88)
MAW_R = 0.44
PETALS = [("Jaw_T", 0.0), ("Jaw_UR", 72.0), ("Jaw_UL", -72.0), ("Jaw_LR", 144.0), ("Jaw_LL", -144.0)]

SHOULDER = {"L": G(-0.72, 0.5, 1.5), "R": G(0.72, 0.5, 1.5)}
LEG_SEGS = {1: ("Seg_2", 0.67), 2: ("Seg_3", 0.05), 3: ("Seg_4", -0.57)}
DEBRIS = [("Debris_L1", -1, "rock", G(-0.3, -0.9, 2.0), 0.15, G(-1.3, 3.0, 0.9), 7.0),
          ("Debris_R1", 1, "rock", G(0.3, -0.9, 2.0), 0.15, G(1.3, 3.0, 0.9), 7.0),
          ("Debris_L2", -1, "rock", G(-0.55, -0.9, 1.6), 0.1, G(-0.7, 3.6, 1.6), 9.0),
          ("Debris_R2", 1, "rock", G(0.55, -0.9, 1.6), 0.1, G(0.7, 3.6, 1.6), 9.0),
          ("Debris_L3", -1, "crystal", G(-0.2, -0.95, 1.25), 0.05, G(-1.8, 3.9, 0.2), 11.0),
          ("Debris_R3", 1, "crystal", G(0.2, -0.95, 1.25), 0.05, G(1.8, 3.9, 0.2), 11.0)]
DEBRIS_NAMES = [d[0] for d in DEBRIS]

# Dive: the hole lies ahead of the jaws; Emerge: the head breaches out of a hole behind the tail's rest position.
DIVE_HOLE = 2.95
DIVE_PATH = DK.PathFZ([(f, 0.0) for f in np.linspace(-12.0, DIVE_HOLE, 30)]
                      + [(DIVE_HOLE + 0.95 * math.sin(a), -0.95 * (1 - math.cos(a))) for a in np.linspace(0.05, math.radians(52), 10)]
                      + [(DIVE_HOLE + 0.95 * math.sin(math.radians(52)) + 6 * math.cos(math.radians(52)),
                          -0.95 * (1 - math.cos(math.radians(52))) - 6 * math.sin(math.radians(52)))],
                      anchor="start", smooth=False)
EMERGE_PATH = DK.PathFZ([(-7.4, -3.6), (-6.55, -2.3), (-5.95, -1.0), (-5.62, 0.1), (-5.25, 0.82), (-4.62, 1.12),
                         (-3.98, 0.82), (-3.45, 0.25), (-3.02, 0.02), (-2.5, 0.0), (-1.5, 0.0), (14.0, 0.0)],
                        anchor="end", samples=16)
DIVE_FRAMES, EMERGE_FRAMES = 120, 112
DIVE_D_END = 8.2


def rot(pitch=0.0, roll=0.0, yaw=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(roll), 3, "Y")
            @ Matrix.Rotation(math.radians(pitch), 3, "X"))


def node(j):
    f, h = NODES[j]
    return G(0, h, f)


def forward_frame(front, back):
    """Rotation whose local -Y points from `back` to `front` (armour_plate's forward) with local Z up."""
    d = (front - back).normalized()
    y = -d
    x = Vector((1, 0, 0))
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return Matrix((x, y, z)).transposed()


def maw_frame():
    a = (MAW_TIP - MAW_BASE).normalized()
    up = (Z - a * a.dot(Z)).normalized()
    right = up.cross(a).normalized()
    return a, up, right


def petal_dir(angle_deg):
    a, up, right = maw_frame()
    phi = math.radians(angle_deg)
    return (up * math.cos(phi) + right * math.sin(phi)).normalized()


def petal_half_width(v):
    return math.radians(32.0) * (1 - v ** 2.2) + math.radians(1.5)


def petal_radius(v, u=0.5):
    keel = 0.03 * math.cos(math.pi * (u - 0.5)) ** 4 * (1 - 0.6 * v)
    return MAW_R * (1 - v) ** 0.7 * (1 + 0.18 * math.sin(math.pi * v)) + 0.035 + keel


def petal_thickness(v):
    return 0.075 * (1 - v) + 0.018


def petal_point(angle_deg, u, v, inner=False):
    a, up, right = maw_frame()
    length = (MAW_TIP - MAW_BASE).length
    phi = math.radians(angle_deg) + (2 * u - 1) * petal_half_width(v)
    c = up * math.cos(phi) + right * math.sin(phi)
    r = petal_radius(v, u) - (petal_thickness(v) + 0.03 * math.cos(math.pi * (u - 0.5)) ** 4 * (1 - 0.6 * v) if inner else 0.0)
    return MAW_BASE + a * length * v + c * r


def limbs():
    arms = {}
    for side, sx in SIDES:
        arms[side] = C.Limb([f"Arm_{side}_Upper", f"Arm_{side}_Fore", f"Arm_{side}_Spade"], SHOULDER[side],
                            G(1.2 * sx, 0.04, 2.3), (0.58, 0.62, 0.32), G(0.7 * sx, 1.0, -0.4),
                            G(0.25 * sx, -0.35, 1.0))
    legs = {}
    for side, sx in SIDES:
        for i, (seg, f) in LEG_SEGS.items():
            legs[(side, i)] = C.Limb([f"Leg_{side}{i}_Upper", f"Leg_{side}{i}_Lower"], G(0.68 * sx, 0.34, f),
                                     G(1.18 * sx, 0.0, f - 0.04), (0.4, 0.44), G(0.4 * sx, 1.0, 0.0))
    return arms, legs


ARMS, LEGS = limbs()


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def body_parts():
    b = C.PartBuilder()
    k = K
    # Segments: broad, low, hewn armour plates, overlapping like shingles; glow fades towards the tail.
    for i in range(1, 7):
        front, back = node(i), node(i + 1)
        centre = (front + back) * 0.5 + (front - back).normalized() * 0.06
        b.ellipsoid("Segment", centre, (WIDTH[i - 1], 0.45, HEIGHT[i - 1]), forward_frame(front, back), bone=f"Seg_{i}",
                    segments=(48, 24), deform=C.chiselled(30, 200 + i, 0.06, squash=(0.7, 1.0)),
                    style=K2.armour_plate(PAL, k * 0.6, rim_glow=1.0 - 0.13 * (i - 1), chevron=0.95 - 0.12 * (i - 1),
                                          lip=0.75, cells=10, seed=210 + i))
        # Pleural hooks: each segment ends at the flanks in a backward hook (a serrated fringe from above, not a grub).
        w, h = WIDTH[i - 1], HEIGHT[i - 1]
        mid = (front + back) * 0.5
        for side, sx in SIDES:
            root = mid + Vector((sx * w * 0.84, 0, h * 0.05))
            out = Vector((sx, 0, 0))
            back_dir = (back - front).normalized()
            pts = [root, root + out * 0.12 + back_dir * 0.1, root + out * 0.17 + back_dir * 0.3 - Vector((0, 0, 0.03))]
            b.tube("Pleura", pts, [0.11 * w, 0.06 * w, 0.005], sides=10, chain=[f"Seg_{i}"], flatten=0.38,
                   style=H.hook(PAL, k * 0.4, glow=0.9 - 0.1 * i, glow_from=0.7))
    # Shoulder guards on the first segment, over the digging arms.
    for side, sx in SIDES:
        b.ellipsoid("Guard", G(0.8 * sx, 0.62, 1.42), (0.24, 0.3, 0.13), rot(roll=-38 * sx, pitch=-10), bone="Seg_1",
                    segments=(32, 16), deform=C.chiselled(16, 230, 0.08, squash=(0.7, 1.0)),
                    style=K2.armour_plate(PAL, k * 0.6, rim_glow=0.8, chevron=0.0, lip=0.8, seed=231))
    # Belly: one tube under the whole spine, ringed at every joint.
    pts = [node(j) + Vector((0, 0, -0.2)) for j in range(1, 9)]
    b.tube("Belly", pts, [0.36, 0.42, 0.42, 0.4, 0.36, 0.3, 0.22, 0.12], sides=24, samples=4,
           chain=[f"Seg_{i}" for i in range(1, 7)] + ["Tail"], style=DK.belly_rings(PAL, k, [n[0] for n in NODES[1:]]))
    # Head collar with the sensor crown.
    collar_c, collar_r = G(0, 0.52, 1.82), (0.6, 0.42, 0.44)
    b.ellipsoid("Collar", collar_c, collar_r, forward_frame(node(0), node(1)), bone="Head", segments=(48, 24),
                deform=C.chiselled(26, 240, 0.05, squash=(0.75, 1.0)),
                style=K2.armour_plate(PAL, k * 0.6, rim_glow=1.0, chevron=1.0, lip=0.85, seed=241))
    for sx in (-1, 1):
        for i, (dx, du, df) in enumerate(((0.22, 0.8, 0.56), (0.45, 0.66, 0.6), (0.66, 0.42, 0.62))):
            d = G(dx * sx, du, df)
            t = 1.0 / math.sqrt((d.x / collar_r[0]) ** 2 + (d.y / collar_r[1]) ** 2 + (d.z / collar_r[2]) ** 2)
            b.ellipsoid("Eye", collar_c + d * t * 0.98, (0.068 - i * 0.008,) * 2 + (0.045,), bone="Head",
                        segments=(14, 8), style=H.eye(PAL, k * 0.3, 0.95))
    # The gullet behind the petals and the five petals.
    b.ellipsoid("Gullet", MAW_BASE + (MAW_TIP - MAW_BASE).normalized() * 0.04, (0.33, 0.2, 0.3), bone="Head",
                segments=(32, 16), style=H.inner_glow(PAL, 1.0))
    a, _, _ = maw_frame()
    length = (MAW_TIP - MAW_BASE).length
    for name, ang in PETALS:
        b.slab("Petal", lambda u, v, ang=ang: petal_point(ang, u, v), lambda u, v, ang=ang: petal_point(ang, u, v, True),
               12, 18, bone=name,
               style=DK.petal(PAL, k * 0.5, MAW_BASE, a, petal_dir(ang), length, petal_half_width))
    # Digging arms: armoured upper arm and forearm, a spade palm with three hooked claws.
    for side, sx in SIDES:
        lg = ARMS[side]
        sh, el, wr, sp = lg.rest_points
        b.tube("ArmUpper", [sh, sh.lerp(el, 0.5) + Vector((0, 0, 0.03)), el], [0.2, 0.17, 0.135], sides=14,
               chain=[lg.names[0]], style=H.leg(LEG, k * 0.5, "Femur", spines=3))
        b.tube("ArmFore", [el, el.lerp(wr, 0.5), wr], [0.135, 0.12, 0.1], sides=14, chain=[lg.names[1]],
               style=H.leg(LEG, k * 0.5, "Femur", spines=2))
        fd = (sp - wr).normalized()
        palm_up = fd.cross(X * sx).normalized() * -sx
        if palm_up.z < 0:
            palm_up = -palm_up
        b.ellipsoid("Palm", wr + fd * 0.06, (0.13, 0.11, 0.07), forward_frame(wr + fd, wr), bone=lg.names[2],
                    segments=(24, 12), style=K2.armour_plate(LEG, k * 0.5, rim_glow=0.5, chevron=0.0, lip=0.6, seed=250))
        side_dir = fd.cross(palm_up).normalized()
        for j, spread in enumerate((-0.11, 0.0, 0.11)):
            root = wr + fd * 0.1 + side_dir * spread
            tip_dir = (fd + side_dir * spread * 2.2).normalized()
            pts = [root, root + tip_dir * 0.18 + palm_up * 0.01, root + tip_dir * 0.3 - palm_up * 0.1]
            b.tube("Claw", pts, [0.05, 0.038, 0.006], sides=10, chain=[lg.names[2]], flatten=0.5,
                   style=H.blade(LEG, k * 0.4, glow=0.7, teeth=3))
    # Legs: short spiked columns.
    for (side, i), lg in LEGS.items():
        hip, knee, foot = lg.rest_points
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.02)), knee], [0.15, 0.14, 0.115], sides=12,
               chain=[lg.names[0]], style=H.leg(LEG, k * 0.5, "Femur", spines=2))
        b.tube("Shin", [knee, knee.lerp(foot, 0.5), foot + Vector((0, 0, 0.015))], [0.115, 0.085, 0.02], sides=12,
               chain=[lg.names[1]], style=H.leg(LEG, k * 0.5, "Tibia"))
    # Tail spade.
    b.ellipsoid("TailSpade", G(0, 0.22, -2.62), (0.36, 0.44, 0.075), forward_frame(node(7), node(8)), bone="Tail",
                segments=(32, 16), deform=C.chiselled(16, 260, 0.06, squash=(0.8, 1.0)),
                style=K2.armour_plate(PAL, k * 0.6, rim_glow=0.35, chevron=0.4, lip=0.7, seed=261))
    # Debris rocks, hidden under the ground at rest (their bones scale them up in Dive and Emerge).
    for name, sx, kind, pos, size, _, _ in DEBRIS:
        if kind != "rock":
            continue
        b.ellipsoid("DebrisRock", pos, (size * 1.1, size, size * 0.8), rot(yaw=30 * sx), bone=name, segments=(16, 8),
                    deform=C.chiselled(10, 270, 0.25), style=S.slab(ROCK, 1.6, seed=271, veins=0.8, fault=0.4))
    return b


def crystal_parts():
    b = C.PartBuilder()
    # Crysium armour: a pair of angled plates per segment and a crest of swept-back shards, tallest at the front.
    crest = [(0.13, 0.8), (0.12, 0.66), (0.105, 0.54), (0.085, 0.42), (0.068, 0.3)]
    for i in range(1, 7):
        front, back = node(i), node(i + 1)
        mid = (front + back) * 0.5
        w, h = WIDTH[i - 1], HEIGHT[i - 1]
        s = w / 0.8
        for side, sx in SIDES:
            c = mid + Vector((0.4 * s * sx, 0, h * 0.7))
            r = forward_frame(front, back) @ rot(roll=-26 * sx, pitch=16)
            b.ellipsoid("Plate", c, (0.27 * s, 0.37, 0.085), r, bone=f"Seg_{i}", segments=(28, 14),
                        deform=C.chiselled(14, 300 + i, 0.25, squash=(0.8, 1.0)),
                        style=DK.crystal_plate("pink", 0.6 - 0.05 * (i - 1), seed=310 + i))
        if i <= 5:
            radius, length = crest[i - 1]
            base = mid + Vector((0, 0.08, h * 0.86))
            b.shard("Crest", base, G(0, 1.0, -0.85), radius, length, sides=8, flute=0.45, bend=G(0, 0.06, -0.04),
                    bone=f"Seg_{i}", style=Y.shard_style("pink", 0.75, seed=320 + i))
        if i in (2, 4):
            for side, sx in SIDES:
                base = mid + Vector((0.6 * s * sx, 0, h * 0.2))
                b.shard("Flank", base, G(sx, 0.55, -0.55), 0.045, 0.26, sides=6, flute=0.4, twist=1.2 * sx,
                        bone=f"Seg_{i}", style=Y.shard_style("pink", 0.7, seed=330 + i))
    # Horns on the head collar, swept back.
    for side, sx in SIDES:
        b.shard("Horn", G(0.28 * sx, 0.84, 1.72), G(0.4 * sx, 0.5, -1.0), 0.1, 1.0, sides=8, flute=0.42,
                twist=1.2 * sx, bend=G(0, 0.25, 0), bone="Head", style=Y.shard_style("pink", 0.85, seed=340))
    # Teeth: a ring round the gullet and two per petal on its inner face.
    a, up, right = maw_frame()
    ring_c = MAW_BASE + a * 0.06
    for j in range(10):
        phi = math.radians(18 + 36 * j)
        c = up * math.cos(phi) + right * math.sin(phi)
        b.shard("Tooth", ring_c + c * 0.29, (a * 0.7 - c * 0.7).normalized(), 0.026, 0.14, sides=6, flute=0.3,
                bone="Head", style=Y.shard_style("rose", 0.9, seed=350 + j))
    length = (MAW_TIP - MAW_BASE).length
    for name, ang in PETALS:
        c = petal_dir(ang)
        for v in (0.32, 0.58):
            base = MAW_BASE + a * length * v + c * (petal_radius(v) - petal_thickness(v) - 0.02)
            b.shard("PetalTooth", base, (-c * 0.85 + a * 0.4).normalized(), 0.024, 0.12 * (1.1 - v), sides=6,
                    flute=0.3, bone=name, style=Y.shard_style("rose", 0.9, seed=360))
    # Spade knuckle shards and tail fins.
    for side, sx in SIDES:
        lg = ARMS[side]
        sh, el, wr, sp = lg.rest_points
        b.shard("Knuckle", wr + Vector((0, 0, 0.05)), (G(0.3 * sx, 1.0, -0.6)).normalized(), 0.05, 0.26, sides=6,
                flute=0.4, twist=1.0 * sx, bone=lg.names[2], style=Y.shard_style("pink", 0.8, seed=370))
        b.shard("Fin", G(0.24 * sx, 0.28, -2.52), G(0.7 * sx, 0.35, -1.0), 0.06, 0.52, sides=8, flute=0.4,
                twist=1.0 * sx, bone="Tail", style=Y.shard_style("pink", 0.7, seed=380))
    for name, sx, kind, pos, size, _, _ in DEBRIS:
        if kind != "crystal":
            continue
        b.shard("DebrisShard", pos - Vector((0, 0, 0.12)), G(0.2 * sx, 1.0, 0.1), size, 0.26, sides=6, flute=0.4,
                bone=name, style=Y.shard_style("pink", 0.9, seed=390))
    return b


def bones():
    # Root does not deform: a part whose chain starts at Seg_1 must not be pulled back to the pivot when the
    # spine rides far away from it (Dive, Emerge).
    out = [("Root", G(0, 0, 0), G(0, 0.3, 0), None, False)]
    parents = {"Head": "Seg_1", "Seg_1": "Root", "Tail": "Seg_6"}
    for i in range(2, 7):
        parents[f"Seg_{i}"] = f"Seg_{i - 1}"
    order = ["Seg_1", "Head"] + [f"Seg_{i}" for i in range(2, 7)] + ["Tail"]
    spine = {n: (a, b) for n, a, b in SPINE}
    for n in order:
        a, b = spine[n]
        out.append((n, node(a), node(b), parents[n], True))
    a_ax, _, _ = maw_frame()
    length = (MAW_TIP - MAW_BASE).length
    for name, ang in PETALS:
        c = petal_dir(ang)
        out.append((name, MAW_BASE + c * MAW_R, MAW_BASE + a_ax * length * 0.8 + c * petal_radius(0.8), "Head", True))
    for side, _ in SIDES:
        out += ARMS[side].rest_bones("Seg_1")
    for (side, i), lg in LEGS.items():
        out += lg.rest_bones(LEG_SEGS[i][0])
    for name, sx, kind, pos, size, _, _ in DEBRIS:
        out.append((name, pos, pos + Vector((0, 0, 0.12)), "Root", True))
    return out


# Hinge axes of the petals (armature rest space): opening is a positive turn about the tangent at the base.
def jaw_axes():
    a, _, _ = maw_frame()
    return {name: a.cross(petal_dir(ang)).normalized() for name, ang in PETALS}


JAW_AXES = jaw_axes()


# ------------------------------------------------------------------------------------------------------------
# Posing
# ------------------------------------------------------------------------------------------------------------

def rest_nodes():
    return [node(j) for j in range(len(NODES))], [Z.copy() for _ in NODES]


def rotate_about(p, centre, axis, angle):
    return centre + Matrix.Rotation(angle, 3, axis) @ (p - centre)


def debris_hidden(poser):
    return {n: {"abs": poser.rest[n].copy(), "scale": 0.001} for n in DEBRIS_NAMES}


def debris_burst(poser, frame, burst, origin_f, sink=(None, None)):
    """Chunks thrown out of a hole at forward `origin_f` from frame `burst`, landing round it; from sink[0] to
    sink[1] they sink back under the ground and are hidden."""
    out = {}
    for name, sx, kind, pos, size, vel, spin in DEBRIS:
        if frame < burst or (sink[1] is not None and frame >= sink[1]):
            out[name] = {"abs": poser.rest[name].copy(), "scale": 0.001}
            continue
        t = (frame - burst) / 30.0
        origin = G(0.18 * sx, -0.05, origin_f + (0.1 if kind == "rock" else -0.1))
        p, tl = DK.ballistic(origin, vel * (0.85 if kind == "rock" else 1.0), t, floor=size * 0.6)
        tumble = Matrix.Rotation(spin * (tl if tl is not None else t), 3, G(0.3 * sx, 0.2, 1.0).normalized())
        if sink[0] is not None and frame > sink[0]:
            p.z -= 0.7 * K2.ease(sink[0], sink[1], frame)
        m = (tumble @ poser.rest[name].to_3x3()).to_4x4()
        m.translation = p
        out[name] = {"abs": m, "scale": 1.0}
    return out


def assemble(poser, pos, ups, jaw=0.0, arm_feet=None, leg_feet=None, debris=None, arm_pole=None, leg_pole=None):
    """Full specs: spine from node positions, jaws, limbs (targets in armature space), debris."""
    specs = {"Root": {"rot": []}}
    specs.update(DK.spine_specs(poser, SPINE, pos, ups))
    jaws = jaw if isinstance(jaw, dict) else {n: jaw for n, _ in PETALS}
    for n, _ in PETALS:
        specs[n] = {"rot": [(JAW_AXES[n], jaws[n])]}
    for side, lg in ARMS.items():
        root = DK.carried(poser, specs, "Seg_1", lg.root)
        foot = arm_feet[side] if arm_feet else DK.carried(poser, specs, "Seg_1", lg.foot)
        pole = DK.carried_dir(poser, specs, "Seg_1", lg.pole if not arm_pole else arm_pole[side])
        fd = DK.carried_dir(poser, specs, "Seg_1", lg.foot_dir)
        specs.update(lg.pose(poser, root, foot, pole, fd))
    for key, lg in LEGS.items():
        seg = LEG_SEGS[key[1]][0]
        root = DK.carried(poser, specs, seg, lg.root)
        foot = leg_feet[key] if leg_feet else DK.carried(poser, specs, seg, lg.foot)
        pole = DK.carried_dir(poser, specs, seg, lg.pole)
        specs.update(lg.pose(poser, root, foot, pole))
    specs.update(debris if debris is not None else debris_hidden(poser))
    return specs


def world_feet():
    return {s: lg.foot.copy() for s, lg in ARMS.items()}, {k: lg.foot.copy() for k, lg in LEGS.items()}


def carried_feet(poser, pos, ups, tuck_arm=None, tuck_leg=None):
    """Feet that ride with their segment (rest offset carried by the bone), folded in by a tuck factor 0..1."""
    tmp = {"Root": {"rot": []}}
    tmp.update(DK.spine_specs(poser, SPINE, pos, ups))
    arms, legs = {}, {}
    for side, lg in ARMS.items():
        sx = -1 if side == "L" else 1
        rest = lg.foot
        tucked = rest.lerp(lg.root, 0.25) + G(-0.25 * sx, 0.05, 0.25)
        arms[side] = DK.carried(poser, tmp, "Seg_1", rest.lerp(tucked, tuck_arm(side) if tuck_arm else 0.0))
    for key, lg in LEGS.items():
        sx = -1 if key[0] == "L" else 1
        seg = LEG_SEGS[key[1]][0]
        tucked = lg.root + G(0.22 * sx, -0.12, -0.32)
        legs[key] = DK.carried(poser, tmp, seg, lg.foot.lerp(tucked, tuck_leg(key) if tuck_leg else 0.0))
    return arms, legs


STRIDE, STANCE, LIFT = 0.45, 0.65, 0.12
CRAWL_FRAMES = 64


def crawl_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        pos, ups = rest_nodes()
        for j in range(len(pos)):
            amp = 0.03 + 0.012 * j
            pos[j] = pos[j] + Vector((amp * math.sin(w - j * 0.55), 0, 0.012 * math.sin(2 * w - j * 0.4)))
        arms, legs = world_feet()
        for side, lg in ARMS.items():
            phase = (t + (0.0 if side == "L" else 0.5)) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT * 1.6)
            arms[side] = lg.foot + G(0, up, fwd)
        for (side, i), lg in LEGS.items():
            phase = (t + 0.22 * i + (0.0 if side == "L" else 0.5)) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            legs[(side, i)] = lg.foot + G(0, up, fwd)
        jaw = math.radians(3 + 3 * math.sin(w * 2))
        return assemble(poser, pos, ups, jaw, arms, legs)
    return CRAWL_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        breathe = 0.5 + 0.5 * math.sin(w)
        pos, ups = rest_nodes()
        for j in range(len(pos)):
            pos[j] = pos[j] + Vector((0, 0, 0.014 * breathe * (1 - j / 10)))
        pos[0] = rotate_about(pos[0], pos[1], Z, math.radians(7) * math.sin(w))
        pos[0] = rotate_about(pos[0], pos[1], X, math.radians(3) * math.sin(2 * w))
        jaw = math.radians(5) + math.radians(9) * max(0.0, math.sin(2 * w + 0.4)) ** 2
        arms, legs = world_feet()
        for side, lg in ARMS.items():
            sx = -1 if side == "L" else 1
            arms[side] = lg.foot + G(0, 0.03 * max(0.0, math.sin(w + (0 if sx < 0 else math.pi))), 0)
        return assemble(poser, pos, ups, jaw, arms, legs)
    return 90, spec, True


def rear(pos, amount, back=0.0):
    weights = [1.0, 0.85, 0.5, 0.18, 0.04, 0, 0, 0, 0]
    for j, wgt in enumerate(weights):
        pos[j] = pos[j] + Vector((0, back * wgt, amount * wgt))
    return pos


def roar_spec(poser):
    def spec(t, f):
        lift = K2.ease(0.0, 0.3, t) * (1 - K2.ease(0.78, 1.0, t))
        pos, ups = rest_nodes()
        pos = rear(pos, 0.8 * lift, 0.1 * lift)
        pos[0] = rotate_about(pos[0], pos[1], X, math.radians(32) * lift)
        tremble = math.radians(3) * math.sin(t * 70) * K2.pulse(t, 0.3, 0.5, 0.8)
        jaw = math.radians(58) * K2.ease(0.18, 0.34, t) * (1 - K2.ease(0.78, 0.95, t)) + tremble
        arms, legs = world_feet()
        tmp = assemble(poser, pos, ups, 0.0)
        for side, lg in ARMS.items():
            sx = -1 if side == "L" else 1
            carried = DK.carried(poser, tmp, "Seg_1", lg.foot)
            arms[side] = lg.foot.lerp(carried, lift) + G(0.3 * sx, 0.3, 0.1) * lift
        for (side, i), lg in LEGS.items():
            legs[(side, i)] = lg.foot + G(0, 0.25 * lift * (1.0 if i == 1 else 0.0), 0.05 * lift)
        return assemble(poser, pos, ups, jaw, arms, legs)
    return 66, spec, False


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t)
        pos, ups = rest_nodes()
        for j, wgt in enumerate([1.0, 0.9, 0.6, 0.35, 0.15, 0.05, 0, 0, 0]):
            pos[j] = pos[j] + Vector((0.02 * k * wgt, 0.07 * k * wgt, 0.05 * k * wgt))
        jaw = math.radians(16) * max(k, 0.0) + math.radians(3)
        arms, legs = world_feet()
        return assemble(poser, pos, ups, jaw, arms, legs)
    return 16, spec, False


def death_spec(poser):
    def spec(t, f):
        c = K2.ease(0.05, 0.6, t)
        settle = math.sin(math.pi * K2.ease(0.55, 0.75, t)) * 0.03
        pos, ups = rest_nodes()
        roll = math.radians(20) * c
        out = []
        for j, p in enumerate(pos):
            q = Vector((p.x + 0.38 * c * (j / 8) ** 2, p.y, p.z * (1 - 0.5 * c) + settle))
            q = Matrix.Rotation(roll, 3, "Y") @ q
            out.append(q)
        out[0] = out[0] + Vector((0, 0, -0.18 * c))
        ups = [Matrix.Rotation(roll, 3, "Y") @ Z for _ in pos]
        jaw = math.radians(48) * K2.pulse(t, 0.1, 0.32, 0.7) + math.radians(28) * K2.ease(0.5, 0.8, t)
        arms, legs = world_feet()
        for side, lg in ARMS.items():
            sx = -1 if side == "L" else 1
            arms[side] = lg.foot + G(0.32 * sx, 0.0, -0.15) * c
        for (side, i), lg in LEGS.items():
            sx = -1 if side == "L" else 1
            legs[(side, i)] = lg.foot + G(0.2 * sx, 0.0, 0.0) * c
        return assemble(poser, out, ups, jaw, arms, legs)
    return 60, spec, False


# Dive timing (frames): wind-up to 24, the drill slams down at DIVE_IMPACT, the body slides in until DIVE_UNDER.
DIVE_IMPACT = 31
DIVE_SLIDE = (28, 112)


def dive_D(f):
    return DIVE_D_END * K2.ease(DIVE_SLIDE[0], DIVE_SLIDE[1], f)


def dive_spec(poser):
    def spec(t, f):
        D = dive_D(f)
        windup = K2.ease(0, 20, f) * (1 - K2.ease(23, 31, f))
        slam = K2.pulse(f, 23, 30, 46)
        pos, ups = DK.ride(DIVE_PATH, NODES, D)
        early = 1 - K2.ease(0.0, 1.2, D)
        pos = rear(pos, 0.5 * windup * early, 0.05 * windup)
        pos[0] = rotate_about(pos[0], pos[1], X, (math.radians(26) * windup - math.radians(30) * slam) * early)
        jaw = math.radians(42) * K2.pulse(f, 6, 16, 28)
        plunge = K2.ease(26, 40, f)

        def leg_tuck(key):
            hip_f = NODES[0][0] + D - (NODES[0][0] - LEG_SEGS[key[1]][1])
            return K2.ease(-1.0, -0.1, hip_f - DIVE_HOLE)

        arms_c, legs_c = carried_feet(poser, pos, ups, tuck_arm=lambda s: plunge, tuck_leg=leg_tuck)
        arms_w, legs_w = world_feet()
        tmp = assemble(poser, pos, ups, 0.0)
        arms, legs = {}, {}
        for side, lg in ARMS.items():
            sx = -1 if side == "L" else 1
            raised = DK.carried(poser, tmp, "Seg_1", lg.foot) + G(0.25 * sx, 0.25, 0.1) * windup
            dig = arms_c[side] + G(-0.15 * sx, -0.15, 0.3) * K2.pulse(f, 28, 36, 60)
            arms[side] = raised.lerp(dig, plunge)
        follow = K2.ease(0.0, 0.5, D)
        for key in LEGS:
            legs[key] = legs_w[key].lerp(legs_c[key], follow)
        debris = debris_burst(poser, f, DIVE_IMPACT, DIVE_HOLE - 0.05, sink=(96, 114))
        return assemble(poser, pos, ups, jaw, arms, legs, debris)
    return DIVE_FRAMES, spec, False


EMERGE_START = 8
EMERGE_END = 98


def emerge_D(f):
    """Fast out of the ground (ease-out), slowing as the body settles into the rest pose."""
    s = 1 - (1 - min(max((f - EMERGE_START) / (EMERGE_END - EMERGE_START), 0.0), 1.0)) ** 2.3
    return EMERGE_D0 * (1 - s)


def emerge_burst_frame():
    for f in range(EMERGE_FRAMES + 1):
        pos, _ = DK.ride(EMERGE_PATH, NODES, emerge_D(f))
        tip = pos[0] + (pos[0] - pos[1]).normalized() * 0.62
        if tip.z > 0.0:
            return f
    return EMERGE_START


def emerge_hole():
    """Forward position where the path breaks the surface going up."""
    for i in range(len(EMERGE_PATH.z) - 1):
        if EMERGE_PATH.z[i] < 0 <= EMERGE_PATH.z[i + 1]:
            a = -EMERGE_PATH.z[i] / (EMERGE_PATH.z[i + 1] - EMERGE_PATH.z[i])
            return float(EMERGE_PATH.f[i] + (EMERGE_PATH.f[i + 1] - EMERGE_PATH.f[i]) * a)
    return -5.6


def emerge_spec(poser):
    burst = EMERGE_BURST
    hole = EMERGE_HOLE
    flat = -3.02

    def spec(t, f):
        D = emerge_D(f)
        pos, ups = DK.ride(EMERGE_PATH, NODES, D)
        out = K2.ease(burst, burst + 6, f)
        jaw = math.radians(54) * out * (1 - K2.ease(burst + 26, burst + 44, f))
        breach = K2.pulse(f, burst, burst + 14, burst + 40)

        def leg_tuck(key):
            hip_f = LEG_SEGS[key[1]][1] + D
            return 1 - K2.ease(0.0, 0.8, hip_f - flat)

        arm_tuck = 1 - K2.ease(0.2, 1.4, NODES[1][0] + D - flat)
        arms, legs = carried_feet(poser, pos, ups, tuck_arm=lambda s: arm_tuck * (1 - breach), tuck_leg=leg_tuck)
        for side in arms:
            sx = -1 if side == "L" else 1
            arms[side] = arms[side] + G(0.35 * sx, 0.1, 0.1) * breach
        debris = debris_burst(poser, f, burst, hole + 0.1, sink=(EMERGE_FRAMES - 16, EMERGE_FRAMES))
        return assemble(poser, pos, ups, jaw, arms, legs, debris)
    return EMERGE_FRAMES, spec, False


# The emerge path's start, so the head begins deep under the ground.
EMERGE_D0 = (EMERGE_PATH.offset - 0.6) - NODES[0][0]
EMERGE_BURST = emerge_burst_frame()
EMERGE_HOLE = emerge_hole()

CLIPS = {"Crawl": crawl_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Roar": roar_spec,
         "Dive": dive_spec, "Emerge": emerge_spec}
LOD_DROP = {**{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in LEG_SEGS},
            **{f"Arm_{s}_Spade": f"Arm_{s}_Fore" for s, _ in SIDES}}
LOD_AIMS = {**{f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in LEG_SEGS},
            **{f"Arm_{s}_Fore": (f"Arm_{s}_Fore", f"Arm_{s}_Spade") for s, _ in SIDES}}

BODY_RIGID = {"Segment", "Pleura", "Guard", "Collar", "Petal", "Palm", "TailSpade", "DebrisRock", "Eye", "Gullet"}
CRYSTAL_RIGID = {"Plate", "Crest", "Flank", "Horn", "Tooth", "PetalTooth", "Knuckle", "Fin", "DebrisShard"}


def dive_under_frame(poser):
    """First Dive frame at which the whole creature is below the ground plane."""
    for f in range(DIVE_IMPACT, DIVE_FRAMES + 1):
        pos, _ = DK.ride(DIVE_PATH, NODES, dive_D(f))
        if max(p.z + 0.75 for p in pos[1:]) < 0.0:
            return f
    return DIVE_FRAMES


# ------------------------------------------------------------------------------------------------------------
# Previews
# ------------------------------------------------------------------------------------------------------------

def hole_props():
    """Two copies of the burrow hole (Exports/Env/Gravemaw_Hole) at the dive and emerge holes, or none."""
    import env_kit as E
    folder = os.path.join(E.ENV, "Gravemaw_Hole")
    if not os.path.exists(os.path.join(folder, "Gravemaw_Hole.fbx")):
        return []
    objs = []
    for f, yaw in ((DIVE_HOLE + 0.2, 0.0), (EMERGE_HOLE - 0.1, math.pi)):
        new = E.textured_import(folder, "Gravemaw_Hole", emission_strength=2.2)
        for o in new:
            if o.type == "MESH":
                o.location = G(0, 0, f)
                o.rotation_euler.z = yaw
                objs.append(o)
    return objs


def previews(arm, mesh, actions, lod, out):
    C.preview_world()
    rig = C.light_rig(scale=4.0)
    plain = C.ground()
    holes = DK.ground_with_holes([(0.0, -(DIVE_HOLE + 0.2), 0.7), (0.0, -(EMERGE_HOLE - 0.1), 0.7)])
    holes.hide_render = True
    props = hole_props()
    for o in props:
        o.hide_render = True

    def holes_on(on):
        plain.hide_render = on
        holes.hide_render = not on
        for o in props:
            o.hide_render = not on

    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    target = G(0, 0.55, 0.0)
    cams = {
        "close": C.closeup_camera(target, 10.0, azimuth=-38, elevation=24, lens=55),
        "side": C.closeup_camera(target, 10.5, azimuth=-90, elevation=8, lens=55),
        "front": C.closeup_camera(G(0, 0.6, 1.4), 7.5, azimuth=-18, elevation=12, lens=55),
        "top": C.closeup_camera(target, 10.5, azimuth=-160, elevation=58, lens=55),
        "back": C.closeup_camera(target, 10.0, azimuth=-150, elevation=22, lens=55),
        "dive": C.closeup_camera(G(0, 0.3, 1.2), 11.5, azimuth=-62, elevation=26, lens=50),
        "emerge": C.closeup_camera(G(0, 0.6, -2.4), 11.5, azimuth=-62, elevation=24, lens=50),
    }
    paths = []

    def shot(name, cam, clip, frame):
        C.set_action(arm, actions[clip], frame)
        paths.append(C.render(cams[cam], os.path.join(out, f"gravemaw_{name}.png"), 1280, 720))

    shot("close", "close", "Idle", 20)
    shot("side", "side", "Crawl", 8)
    shot("front", "front", "Idle", 20)
    shot("top", "top", "Crawl", 12)
    shot("back", "back", "Idle", 40)
    shot("roar", "close", "Roar", 34)
    shot("dead", "close", "Death", 60)
    holes_on(True)
    shot("dive_mid", "dive", "Dive", 52)
    shot("emerge_breach", "emerge", "Emerge", EMERGE_BURST + 14)
    strips = [C.strip_sheet(arm, actions["Dive"], cams["dive"], out, "gravemaw_dive",
                            [0, 12, 20, 26, 31, 36, 44, 54, 66, 80, 96, 119]),
              C.strip_sheet(arm, actions["Emerge"], cams["emerge"], out, "gravemaw_emerge",
                            [EMERGE_BURST - 2, EMERGE_BURST + 2, EMERGE_BURST + 6, EMERGE_BURST + 12, EMERGE_BURST + 20,
                             EMERGE_BURST + 30, EMERGE_BURST + 42, EMERGE_BURST + 56, 80, 92, 100, EMERGE_FRAMES])]
    holes_on(False)
    strips += [C.strip_sheet(arm, actions["Crawl"], cams["side"], out, "gravemaw_crawl", range(0, CRAWL_FRAMES, 8)),
               C.strip_sheet(arm, actions["Roar"], cams["close"], out, "gravemaw_roar", range(0, 66, 8)),
               C.strip_sheet(arm, actions["Death"], cams["close"], out, "gravemaw_death", range(0, 61, 8))]

    C.set_action(lod_arm, lod_actions["Crawl"], 8)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(cams["close"], os.path.join(out, "gravemaw_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 20)
    macros = [("maw", G(0, 0.48, 2.5), 2.0, -32, 14), ("armour", G(0.3, 1.0, 0.5), 2.0, -50, 40),
              ("spade", G(0.95, 0.15, 2.15), 1.4, -70, 18), ("roar_maw", G(0, 1.4, 2.4), 2.2, -20, 10)]
    for suffix, aim, dist, az, el in macros[:3]:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, f"gravemaw_macro_{suffix}.png"), 1280, 720))
    C.set_action(arm, actions["Roar"], 34)
    suffix, aim, dist, az, el = macros[3]
    paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, f"gravemaw_macro_{suffix}.png"), 1280, 720))

    # Game views: Gravemaw with the level-1 boss and a Skitter swarm, at play zoom and zoomed.
    bm_arm, bm_mesh, bm_actions = B.append_base("BroodMother")
    sk_arm, sk_mesh, sk_actions = B.append_base("Skitter")
    entries = [(arm, mesh, actions["Crawl"], 10, (0, 0, 0), 0.0, UNITY_SCALE),
               (bm_arm, bm_mesh, bm_actions["Walk"], 6, (3.6, 1.4, 0), 0.15, 0.78)]
    for x, y, yaw, fr in ((-2.4, 1.6, 0.2, 3), (-1.9, -1.7, -0.1, 10), (2.3, -1.6, 0.3, 14), (3.0, -0.4, 0.0, 18),
                          (-3.2, 0.1, 0.25, 7), (1.6, 2.6, -0.2, 21)):
        entries.append((sk_arm, sk_mesh, sk_actions["Walk"], fr, (x, y, 0), yaw, 0.45))
    hide = [arm, mesh]
    paths.append(B.game_view_multi(entries, os.path.join(out, "gravemaw_game.png"), 1.0, rig, (0.6, 0, 0.1), hide=hide))
    paths.append(B.game_view_multi(entries, os.path.join(out, "gravemaw_game_zoomed.png"), 1.8, rig, (0.6, 0, 0.1),
                                   hide=hide))
    holes_on(True)
    dive_entries = [(arm, mesh, actions["Dive"], 44, (0, 0, 0), 0.0, UNITY_SCALE)] + entries[1:]
    paths.append(B.game_view_multi(dive_entries, os.path.join(out, "gravemaw_game_dive.png"), 1.6, rig, (0.6, -1.5, 0.1),
                                   hide=hide))
    em_entries = [(arm, mesh, actions["Emerge"], EMERGE_BURST + 16, (0, 0, 0), 0.0, UNITY_SCALE)] + entries[1:]
    paths.append(B.game_view_multi(em_entries, os.path.join(out, "gravemaw_game_emerge.png"), 1.6, rig, (0.6, 2.5, 0.1),
                                   hide=hide))
    holes_on(False)
    cam = C.closeup_camera(G(1.2, 0.6, 0.2), 9.5, azimuth=-30, elevation=22, lens=55)
    paths.append(B.closeup_multi(entries[:2] + entries[2:5], cam, os.path.join(out, "gravemaw_compare_close.png"),
                                 hide=hide))
    C.remove_objects([bm_mesh, bm_arm, sk_mesh, sk_arm])
    C.contact_sheet(paths, os.path.join(out, "sheet_gravemaw.png"), 2, 640)
    C.contact_sheet(strips, os.path.join(out, "strips_gravemaw.png"), 1, 960)
    C.uv_layout(mesh, os.path.join(out, "gravemaw_uv_body.png"))


def blockout(out):
    """Fast check without bakes (--blockout): the raw parts in flat colours on the rig, a few clip strips."""
    body_b, crys_b = body_parts(), crystal_parts()
    body = body_b.to_object(NAME + "_Block")
    crys = crys_b.to_object(CRYSTAL + "_Block")
    body.data.materials.append(C.principled("BlockBody", (0.2, 0.1, 0.11), rough=0.6))
    crys.data.materials.append(C.principled("BlockCrystal", (0.9, 0.35, 0.65), rough=0.3, emission=(1.0, 0.4, 0.7),
                                            strength=0.6))
    arm = C.build_armature(NAME + "_Rig", bones())
    C.skin(body, arm, body_b, rigid=BODY_RIGID)
    C.skin(crys, arm, crys_b, rigid=CRYSTAL_RIGID)
    poser = B.ScalePoser(arm, DEBRIS_NAMES)
    actions = C.key_clips(poser, CLIPS)
    C.log("emerge burst", EMERGE_BURST, "hole", EMERGE_HOLE, "dive under", dive_under_frame(poser),
          "bones", len(arm.data.bones))
    C.preview_world()
    C.light_rig(scale=4.0)
    DK.ground_with_holes([(0.0, -(DIVE_HOLE + 0.2), 0.7), (0.0, -(EMERGE_HOLE - 0.1), 0.7)])
    close = C.closeup_camera(G(0, 0.55, 0.0), 10.0, azimuth=-38, elevation=24, lens=55)
    side = C.closeup_camera(G(0, 0.55, 0.0), 11.5, azimuth=-90, elevation=6, lens=45)
    top = C.closeup_camera(G(0, 0.55, 0.0), 10.5, azimuth=-160, elevation=58, lens=55)
    for clip, frame in (("Idle", 20), ("Roar", 34)):
        C.set_action(arm, actions[clip], frame)
        C.render(close, os.path.join(out, f"block_{clip.lower()}.png"), 960, 540, bloom=False, samples=8)
    C.set_action(arm, actions["Crawl"], 8)
    C.render(top, os.path.join(out, "block_top.png"), 960, 540, bloom=False, samples=8)
    game = C.game_camera(G(0, 0, 0), 9.0)
    C.render(game, os.path.join(out, "block_game.png"), 960, 540, bloom=False, samples=8)
    for clip, frames in (("Dive", [0, 16, 26, 31, 40, 52, 66, 80, 96, 119]),
                         ("Emerge", [EMERGE_BURST - 2, EMERGE_BURST + 4, EMERGE_BURST + 12, EMERGE_BURST + 22,
                                     EMERGE_BURST + 34, 60, 72, 86, 98, EMERGE_FRAMES]),
                         ("Crawl", range(0, CRAWL_FRAMES, 8)), ("Death", range(0, 61, 10))):
        paths = []
        for i, f in enumerate(frames):
            C.set_action(arm, actions[clip], f)
            paths.append(C.render(side, os.path.join(out, f"block_{clip.lower()}_{i}.png"), 640, 360, bloom=False,
                                  samples=4))
        C.contact_sheet(paths, os.path.join(out, f"block_strip_{clip.lower()}.png"), 4, 480)
        for p in paths:
            os.remove(p)


def main():
    C.reset_scene()
    out = C.preview_dir()
    if C.cli_args().get("blockout"):
        blockout(out)
        return
    folder = os.path.join(C.EXPORTS, NAME)
    body_b, crys_b = body_parts(), crystal_parts()
    body = K2.build_mesh_asset_ex(NAME, body_b, voxel=0.012, target_tris=BODY_TRIS, tex_size=TEX, ao_distance=0.35,
                                  extrusion=0.05, ray=0.15, emission_strength=2.5,
                                  extra=H.weathering(K, markings=PAL.dark, marking_amount=0.4, dust=0.6, seed=401),
                                  specular=0.3, k=K, families=BODY_FAMILIES, low_size=LOW_TEX, out_dir=folder)
    crystal = K2.build_mesh_asset_ex(CRYSTAL, crys_b, voxel=0.009, target_tris=CRYSTAL_TRIS, tex_size=TEX,
                                     ao_distance=0.2, extrusion=0.04, ray=0.12, emission_strength=2.2, specular=0.4,
                                     k=K * 0.6, families=CRYSTAL_FAMILIES, low_size=LOW_TEX, out_dir=folder,
                                     smooth_iterations=2)
    body_tris, crystal_tris = body.low_tris, crystal.low_tris
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(body.low, arm, body_b, rigid=BODY_RIGID, blend=2)
    C.skin(crystal.low, arm, crys_b, rigid=CRYSTAL_RIGID, blend=0)
    mesh = DK.join_skinned(body.low, crystal.low)
    mesh.name = NAME
    poser = B.ScalePoser(arm, DEBRIS_NAMES)
    actions = C.key_clips(poser, CLIPS)
    under = dive_under_frame(poser)
    lod = DK.build_lod2(LOD_NAME, [(body, body_b, 0.72), (crystal, crys_b, 0.28)], bone_list, CLIPS, poser, LOD_TRIS,
                        LOD_DROP, LOD_AIMS, scale_bones=DEBRIS_NAMES)
    K2.export_rig(os.path.join(folder, NAME + ".fbx"), arm, mesh, actions)
    K2.export_rig(os.path.join(folder, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    C.write_report(NAME, {
        "asset": NAME, "game_tris": C.tri_count(mesh), "body_tris": body_tris, "crystal_tris": crystal_tris,
        "source_tris": body.high_tris + crystal.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
        "low_texture_size": LOW_TEX, "materials": {"Gravemaw_Mat": "Gravemaw_*.png", "Gravemaw_Crystal_Mat": "Gravemaw_Crystal_*.png"},
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv_body": body.uv_stats, "uv_crystal": crystal.uv_stats,
        "detail_body": body.details, "detail_crystal": crystal.details, "actions": frames,
        "size_m": [round(d, 3) for d in mesh.dimensions], "unity_scale": UNITY_SCALE,
        "crawl_distance_per_loop": round(STRIDE / STANCE, 3),
        "dive": {"hole_forward": DIVE_HOLE, "wind_up_end": 24, "impact_frame": DIVE_IMPACT, "debris_frame": DIVE_IMPACT,
                 "all_under_frame": under, "frames": DIVE_FRAMES},
        "emerge": {"hole_forward": round(EMERGE_HOLE, 3), "burst_frame": EMERGE_BURST, "debris_frame": EMERGE_BURST,
                   "rest_pose_frame": EMERGE_END, "frames": EMERGE_FRAMES},
        "scale_bones": DEBRIS_NAMES,
        "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, mesh, actions, lod, out)


if __name__ == "__main__":
    main()
