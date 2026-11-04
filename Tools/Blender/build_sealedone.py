"""The Sealed One: the continent-6 final boss (The Core), the ancient being under the seal (round 16, see
ArtSource/DESIGN_NOTES.md).

Only its upper body has broken out: a ribcage torso of ash-ivory bone-stone with violet light between the ribs and
violet corruption crystals through shoulders and spine, a tall eyeless head whose two face plates (bones Plate_L/R)
part to show a column of violet light, a crown of three broken rings carved with seal glyphs (bones Ring_1..3), two
vast lower arms planted on the ground and two thin upper arms folded in prayer, palms carrying the glyph. Round it
seven broken seal stones (bones Seal_1..7), carved with the glyph in amber-gold, lean outwards where the seal burst;
in Death they close over it again. A ring of heaved ground rubble marks the fissure.

Two materials (1024 + 512 Low each): `SealedOne_Mat` (bone-stone body, rubble) and `SealedOne_Seal_Mat` (seal
stones, crown rings, violet crystals). Clips: Idle (loop), Rise, Summon, Hit, Death. Also exports SealedOne_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_sealedone.py -- [--preview <dir>]
     [--norender] [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "SealedOne"
LOD_NAME = NAME + "_LOD1"
K = 4.5
UNITY_SCALE = 1.0

# Ash-ivory bone-stone with deep violet light (the entity); dark basalt seal stones with amber-gold glyphs (the seal).
PAL = H.Palette(top=(0.86, 0.81, 0.72), side=(0.5, 0.46, 0.44), belly=(0.12, 0.1, 0.13), seam=(0.08, 0.03, 0.1),
                dark=(0.2, 0.17, 0.2), glow=(0.62, 0.26, 1.0), eye=(0.8, 0.5, 1.0), membrane=(0.28, 0.07, 0.4),
                bone=(0.9, 0.87, 0.8))
STONE = H.Palette(top=(0.3, 0.28, 0.3), side=(0.14, 0.13, 0.15), belly=(0.05, 0.05, 0.06), seam=(0.03, 0.03, 0.04),
                  dark=(0.08, 0.075, 0.09), glow=(1.0, 0.68, 0.26), eye=(1.0, 0.75, 0.35), bone=(0.55, 0.52, 0.5))
RUBBLE = H.Palette(top=(0.24, 0.2, 0.24), side=(0.11, 0.09, 0.12), belly=(0.04, 0.03, 0.05), seam=(0.03, 0.02, 0.04),
                   dark=(0.07, 0.06, 0.08), glow=PAL.glow, eye=PAL.eye, bone=(0.5, 0.46, 0.5))
GOLD = (1.0, 0.7, 0.28)
VIOLET = (0.62, 0.26, 1.0)
Y.PALETTES.setdefault("void", ((np.array((0.12, 0.02, 0.2)), np.array((0.55, 0.22, 0.95)), np.array((0.92, 0.82, 1.0))),
                               (np.array((0.68, 0.3, 1.0)), np.array((0.95, 0.85, 1.0)))))

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

STONE_R = 2.75
STONE_ANGLES = [180.0, 180 - 51.43, 180 + 51.43, 180 - 102.86, 180 + 102.86, 180 - 154.29, 180 + 154.29]
STONE_SIZE = [(0.7, 0.26, 1.45), (0.62, 0.25, 1.35), (0.62, 0.25, 1.35), (0.6, 0.25, 1.25), (0.6, 0.25, 1.25),
              (0.5, 0.24, 1.0), (0.5, 0.24, 1.0)]
STONE_TILT = 18.0
HEAD_C = G(0, 4.05, 0.25)
HEAD_R = (0.42, 0.62, 1.05)
HEAD_ROT = T.rot(pitch=18)
CROWN_C = G(0, 4.35, -0.25)
RINGS = [("Ring_1", 1.0, G(0, 0.5, -1).normalized(), 0.07), ("Ring_2", 1.35, G(0, 1, 0.12).normalized(), 0.08),
         ("Ring_3", 1.75, G(0, 0.75, 1).normalized(), 0.09)]
RING_NAMES = [r[0] for r in RINGS]
LOWER = {"shoulder": (1.05, 2.6, 0.15), "hand": (1.75, 0.12, 1.75), "lengths": (1.6, 1.75)}
UPPER = {"shoulder": (0.62, 3.05, 0.3), "elbow": (1.05, 3.05, 0.85), "wrist": (0.16, 3.25, 1.05)}
SCALE_BONES = RING_NAMES


def stone_dir(i):
    a = math.radians(STONE_ANGLES[i])
    return G(math.sin(a), 0, math.cos(a))


def stone_base(i):
    return stone_dir(i) * STONE_R


def stone_rot(i):
    """Stands on its base, leaning outwards by STONE_TILT, its broad face towards the centre."""
    d = stone_dir(i)
    yaw = math.atan2(d.x, -d.y)
    return (Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(math.radians(STONE_TILT), 3, "X"))


def lower_arms():
    out = {}
    for side, sx in SIDES:
        out[side] = C.Limb([f"LArm_{side}_Upper", f"LArm_{side}_Fore"], T.gp(LOWER["shoulder"], sx),
                           T.gp(LOWER["hand"], sx) + G(0, 0.3, 0), LOWER["lengths"], G(0.8 * sx, 0.4, -0.6))
    return out


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def body_parts():
    b = C.PartBuilder()
    k = K
    # Torso rising from below the ground: a dark violet-lit core inside a ribcage of bone-stone.
    b.ellipsoid("Waist", G(0, 0.6, -0.05), (0.85, 0.7, 1.3), bone="Body", segments=(40, 20),
                style=H.glow_flesh(PAL, k * 0.4, glow=0.8, folds=8.0))
    b.ellipsoid("Core", G(0, 2.35, 0.0), (0.95, 0.72, 1.05), bone="Chest", segments=(48, 24),
                style=T.flesh_glow(PAL, k * 0.5, glow=1.0, folds=9.0, seed=2, axis=2))
    b.parts[-1].chain = ["Chest", "Body"]
    for i, u in enumerate((1.55, 1.9, 2.25, 2.6, 2.95)):
        w = 1.0 - 0.1 * abs(i - 2)
        for side, sx in SIDES:
            pts = [G(0.12 * sx, u + 0.05, -0.62), G(0.85 * w * sx, u + 0.1, -0.35), G(1.02 * w * sx, u, 0.15),
                   G(0.72 * w * sx, u - 0.12, 0.6), G(0.16 * sx, u - 0.2, 0.74)]
            b.tube("Rib", pts, [0.1, 0.12, 0.12, 0.1, 0.07], sides=10, chain=["Chest" if u > 1.8 else "Body"],
                   style=S.pillar(PAL, k * 0.4, drums=6, seed=10 + i, glow=0.5))
    b.ellipsoid("Sternum", G(0, 2.35, 0.78), (0.18, 0.12, 0.85), bone="Chest", segments=(24, 12),
                deform=C.chiselled(10, 20, 0.08), style=S.slab(PAL, k * 0.4, strata=30, veins=0.6, fault=0.4, seed=21))
    b.ellipsoid("Spine", G(0, 2.4, -0.7), (0.28, 0.2, 1.1), bone="Chest", segments=(28, 14),
                deform=C.chiselled(12, 22, 0.06), style=S.slab(PAL, k * 0.4, strata=34, veins=0.4, seed=23))
    b.parts[-1].chain = ["Chest", "Body"]
    for side, sx in SIDES:
        b.ellipsoid("Shoulder", G(0.95 * sx, 3.05, 0.0), (0.5, 0.5, 0.38), T.rot(roll=-25 * sx), bone="Chest",
                    segments=(32, 16), deform=C.chiselled(16, 24, 0.07, squash=(0.8, 1.0)),
                    style=S.slab(PAL, k * 0.5, strata=24, veins=0.5, fault=0.6, seed=25))
    # Neck and head: an eyeless crest of two face plates over a column of violet light.
    b.tube("Neck", [G(0, 3.1, 0.05), G(0, 3.35, 0.12), G(0, 3.6, 0.2)], [0.32, 0.28, 0.26], sides=14, chain=["Neck"],
           style=S.pillar(PAL, k * 0.4, drums=4, seed=30, glow=0.6))
    b.ellipsoid("HeadCore", HEAD_C, (HEAD_R[0] * 0.8, HEAD_R[1] * 0.86, HEAD_R[2] * 0.9), HEAD_ROT, bone="Head",
                segments=(32, 16), style=T.solid_glow(VIOLET, 1.0, 0.5))
    for side, sx in SIDES:
        T.ellipsoid_patch(b, "FacePlate", HEAD_C, HEAD_R, HEAD_ROT, (1, 2), (-0.84, 0.97), (-0.95, 0.97), sx, 0.04, 0.09,
                          f"Plate_{side}", T.shell_plate(PAL, k * 0.4, (1, 2), (-0.84, 0.97), (-0.95, 0.97), crack=0.5,
                                                         rim_glow=1.0, seed=31), nu=16, nv=16, bulge=0.05, keel=0.0)
    # Lower arms (planted), with three long-clawed fingers on each hand.
    for side, lg in lower_arms().items():
        sx = -1 if side == "L" else 1
        sh, el, wr = lg.rest_points
        b.tube("LUpper", [sh, sh.lerp(el, 0.5) + G(0, 0.1, 0), el], [0.52, 0.44, 0.36], sides=16, chain=[lg.names[0]],
               style=S.pillar(PAL, k * 0.5, drums=4, seed=40, glow=0.4))
        b.tube("LFore", [el, el.lerp(wr, 0.5), wr], [0.36, 0.33, 0.27], sides=16, chain=[lg.names[1]],
               style=S.pillar(PAL, k * 0.5, drums=5, seed=41, glow=0.4))
        b.ellipsoid("Elbow", el, (0.42, 0.42, 0.38), bone=lg.names[0], segments=(20, 10), deform=C.chiselled(10, 42, 0.1),
                    style=S.slab(PAL, k * 0.4, seed=43))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        hand = wr + G(0, -0.1, 0.15)
        b.ellipsoid("Hand", hand, (0.48, 0.18, 0.5), T.rot(yaw=-15 * sx), bone=f"LArm_{side}_Hand", segments=(24, 12),
                    deform=C.chiselled(10, 44, 0.08), style=S.slab(PAL, k * 0.4, seed=45))
        for j, spread in enumerate((-0.45, 0.0, 0.45)):
            d = (G(spread * 0.8 * sx + 0.15 * sx, 0, 1.0)).normalized()
            r0 = hand + d * 0.3
            pts = [r0, r0 + d * 0.4 + G(0, 0.08, 0), r0 + d * 0.8 - G(0, 0.05, 0), r0 + d * 1.1 - G(0, 0.15, 0)]
            b.tube("Finger", pts, [0.14, 0.11, 0.07, 0.008], sides=10, chain=[f"LArm_{side}_Hand"],
                   style=H.blade(PAL, k * 0.35, glow=0.8, teeth=0))
        # Upper arms folded in prayer.
        us, ue, uw = (T.gp(UPPER[k_], sx) for k_ in ("shoulder", "elbow", "wrist"))
        b.tube("UUpper", [us, us.lerp(ue, 0.5), ue], [0.16, 0.13, 0.11], sides=12, chain=[f"UArm_{side}_Upper"],
               style=S.pillar(PAL, k * 0.4, drums=3, seed=50, glow=0.4))
        b.tube("UFore", [ue, ue.lerp(uw, 0.5), uw], [0.11, 0.1, 0.09], sides=12, chain=[f"UArm_{side}_Fore"],
               style=S.pillar(PAL, k * 0.4, drums=3, seed=51, glow=0.4))
        palm_c = uw + G(0.03 * sx, 0.28, 0.06)
        b.ellipsoid("Palm", palm_c, (0.06, 0.2, 0.32), T.rot(pitch=-15), bone=f"UArm_{side}_Hand", segments=(20, 12),
                    style=S.slab(PAL, k * 0.3, strata=20, veins=0.0, fault=0.0, seed=52))
    # Rubble heaved up round the fissure (static, on Root).
    rng = np.random.default_rng(7)
    for j in range(7):
        a = math.radians(-150 + 300 * j / 6)
        d = G(math.sin(a), 0, math.cos(a))
        r0 = G(0, 0.7, 0) + d * 0.75
        b.tube("Tendril", [r0, r0 + d * 0.5 - G(0, 0.35, 0), r0 + d * 1.0 - G(0, 0.75, 0)], [0.3, 0.2, 0.1], sides=12,
               chain=["Body"], style=S.pillar(PAL, k * 0.4, drums=4, seed=55 + j, glow=0.9))
    for j in range(13):
        a = math.radians(-165 + 330 * j / 12)
        r = 1.35 + 0.25 * rng.uniform()
        c = G(math.sin(a) * r, 0.1, math.cos(a) * r * 0.95)
        b.ellipsoid("Rubble", c, (0.52, 0.4, 0.26), T.rot(yaw=math.degrees(a), roll=20), bone="Root", segments=(16, 8),
                    deform=C.chiselled(9, 60 + j, 0.2), style=S.slab(RUBBLE, k * 0.4, strata=20, veins=0.9, fault=1.0, seed=61))
    return b


def seal_parts():
    b = C.PartBuilder()
    k = K
    # The seal stones (the left ones mirror the right ones; Seal_1 stands at the back on the midline).
    for i in range(7):
        base = stone_base(i)
        sz = STONE_SIZE[i]
        b.ellipsoid("SealStone", base + stone_rot(i) @ Vector((0, 0, sz[2] * 0.85)), sz, stone_rot(i), bone=f"Seal_{i + 1}",
                    segments=(28, 18), deform=C.chiselled(14, 70 + i, 0.12, squash=(0.6, 1.0)),
                    style=S.slab(STONE, k * 0.4, strata=26, veins=0.3, fault=0.5, seed=71 + i))
    # The crown rings: broken arcs, gaps at the bottom and both sides.
    for name, radius, normal, tube in RINGS:
        a_ref = (Z - normal * normal.dot(Z)).normalized()
        b_ref = normal.cross(a_ref)
        for lo, hi in ((-55, 55), (75, 160), (-160, -75)):
            pts = [CROWN_C + (a_ref * math.cos(math.radians(t)) + b_ref * math.sin(math.radians(t))) * radius
                   for t in range(lo, hi + 1, 8)]
            b.tube("Ring", pts, [tube * 0.5, tube, tube, tube, tube * 0.5], sides=10, samples=2, chain=[name],
                   style=T.scale_band(STONE, k * 0.3, bands=5.0, glow=0.0, seed=80))
        # Glyph knots at the arc ends' midpoints.
        for t in (0, 117.5, -117.5):
            c = CROWN_C + (a_ref * math.cos(math.radians(t)) + b_ref * math.sin(math.radians(t))) * radius
            b.ellipsoid("RingKnot", c, (tube * 2.2,) * 3, bone=name, segments=(14, 8), deform=C.chiselled(8, 81, 0.1),
                        style=S.slab(STONE, k * 0.3, seed=82))
    # Violet corruption crystals: shoulders, spine, the crest of the head.
    for side, sx in SIDES:
        for j, (p, d, r, l) in enumerate((((1.0, 3.35, -0.1), (0.5, 1.0, -0.3), 0.14, 1.1),
                                          ((1.2, 3.15, 0.2), (0.9, 0.6, 0.2), 0.1, 0.8),
                                          ((0.75, 3.4, -0.35), (0.2, 1.0, -0.6), 0.09, 0.7))):
            b.shard("Corruption", T.gp(p, sx), T.gp(d, sx).normalized(), r, l, sides=8, flute=0.45, twist=1.0 * sx,
                    bone="Chest", style=Y.shard_style("void", 1.0, seed=90 + j))
    for j, (u, f, l) in enumerate(((2.9, -0.8, 1.0), (2.4, -0.85, 0.8), (1.9, -0.8, 0.6))):
        b.shard("Corruption", G(0, u, f), G(0, 0.6, -1.0).normalized(), 0.12, l, sides=8, flute=0.45, bone="Chest",
                style=Y.shard_style("void", 1.0, seed=95 + j))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.4, 0), None, True),
           ("Body", G(0, -0.6, 0.0), G(0, 1.7, 0.0), "Root", True),
           ("Chest", G(0, 1.7, 0.0), G(0, 3.1, 0.05), "Body", True),
           ("Neck", G(0, 3.1, 0.05), G(0, 3.6, 0.2), "Chest", True),
           ("Head", G(0, 3.6, 0.2), G(0, 4.7, 0.4), "Neck", True)]
    for side, sx in SIDES:
        out.append((f"Plate_{side}", G(0.12 * sx, 3.75, -0.45), G(0.12 * sx, 4.6, -0.25), "Head", True))
    for name, radius, normal, tube in RINGS:
        out.append((name, CROWN_C, CROWN_C + normal * 0.5, "Head", True))
    for side, lg in lower_arms().items():
        out += lg.rest_bones("Chest")
        wr = lg.rest_points[2]
        out.append((f"LArm_{side}_Hand", wr, wr + G(0, -0.1, 0.6), lg.names[1], True))
    for side, sx in SIDES:
        us, ue, uw = (T.gp(UPPER[k_], sx) for k_ in ("shoulder", "elbow", "wrist"))
        out += [(f"UArm_{side}_Upper", us, ue, "Chest", True), (f"UArm_{side}_Fore", ue, uw, f"UArm_{side}_Upper", True),
                (f"UArm_{side}_Hand", uw, uw + G(0, 0.5, 0.1), f"UArm_{side}_Fore", True)]
    for i in range(7):
        base = stone_base(i)
        out.append((f"Seal_{i + 1}", base, base + stone_rot(i) @ Vector((0, 0, 1.0)), "Root", True))
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def stone_axis(i):
    """Positive turn tilts the stone's top outwards (more broken), negative closes it over the centre."""
    return Z.cross(stone_dir(i)).normalized()


def specs(sink=0.0, lean=0.0, roll=0.0, yaw=0.0, chest=0.0, head=0.0, head_yaw=0.0, plates=0.0, rings=1.0,
          spin=(0.0, 0.0, 0.0), uarm_up=0.0, uarm_out=0.0, uelbow=0.0, seal=0.0, seal_lift=0.0):
    """Hinge specs (radians). `sink` lowers the whole body (metres, positive down); `plates` > 0 parts the face plates;
    `seal` > 0 closes the seal stones (inward tilt), `seal_lift` raises them."""
    out = {"Root": {"rot": []},
           "Body": {"rot": [(Z, yaw), (Yv, roll), (X, lean * 0.4)], "loc": Vector((0, 0, -sink))},
           "Chest": {"rot": [(X, lean * 0.6 + chest)]},
           "Neck": {"rot": [(Z, head_yaw * 0.5), (X, head * 0.5)]},
           "Head": {"rot": [(Z, head_yaw * 0.5), (X, head * 0.5)]}}
    for side, sx in SIDES:
        out[f"Plate_{side}"] = {"rot": [(Z, -plates * sx), (Yv, plates * 0.4 * sx)]}
    rs = rings if isinstance(rings, (list, tuple)) else (rings,) * 3
    for (name, radius, normal, tube), s, a in zip(RINGS, rs, spin):
        out[name] = {"rot": [(normal, a)], "scale": max(s, 0.001)}
    for side, sx in SIDES:
        out[f"UArm_{side}_Upper"] = {"rot": [(Yv, -uarm_out * sx), (X, -uarm_up)]}
        out[f"UArm_{side}_Fore"] = {"rot": [(X, -uelbow)]}
        out[f"UArm_{side}_Hand"] = {"rot": []}
        out[f"LArm_{side}_Hand"] = {"rot": []}
    for i in range(7):
        out[f"Seal_{i + 1}"] = {"rot": [(stone_axis(i), -seal)], "loc": Vector((0, 0, seal_lift))}
    return out


def place_hands(poser, sp, hands=None, curl=0.0):
    """Lower arms: IK from the posed shoulders to `hands` ({side: world target}, default planted)."""
    pose = poser.solve(sp)
    move = pose["Chest"] @ poser.rest["Chest"].inverted()
    for side, lg in lower_arms().items():
        sx = -1 if side == "L" else 1
        target = hands[side] if hands else lg.foot
        sp.update(lg.pose(poser, move @ lg.root, target, move.to_3x3() @ lg.pole))
        sp[f"LArm_{side}_Hand"] = {"rot": [(X, curl)]}
    return sp


def planted_hands():
    return {s: lg.foot.copy() for s, lg in lower_arms().items()}


IDLE_FRAMES = 120


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(sink=-0.04 * math.sin(w), lean=math.radians(2) * math.sin(w), head_yaw=math.radians(10) * math.sin(w + 0.6),
                   head=math.radians(3) * math.sin(2 * w), plates=math.radians(2) * (0.5 + 0.5 * math.sin(2 * w)),
                   spin=(w, -w, w), uarm_up=math.radians(2) * math.sin(w), rings=1.0)
        return place_hands(poser, sp)
    return IDLE_FRAMES, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(sink=-0.05 * k, lean=-math.radians(6) * k, head=-math.radians(10) * k, plates=math.radians(10) * max(k, 0),
                   rings=1.0 + 0.1 * max(k, 0), spin=(0.3 * k, -0.3 * k, 0.2 * k))
        return place_hands(poser, sp)
    return 18, spec, False


SUMMON_FRAMES, SUMMON_PEAK = 90, 44


def summon_spec(poser):
    """The upper arms rise and open (palms forward, the glyphs shown), the face plates part on the violet column,
    the crown rings flare (1.3) and spin; the corrupted enemies appear at SUMMON_PEAK."""
    def spec(t, f):
        up = T.ease(6, 34, f) * (1 - T.ease(64, 86, f))
        peak = T.pulse(f, SUMMON_PEAK - 10, SUMMON_PEAK, SUMMON_PEAK + 18)
        spin = 3.0 * T.ease(10, 80, f)
        sp = specs(sink=-0.25 * up, lean=-math.radians(12) * up, head=-math.radians(14) * up,
                   plates=math.radians(30) * up + math.radians(10) * peak, rings=1.0 + 0.3 * up + 0.12 * peak,
                   spin=(spin, -spin * 1.3, spin * 0.7), uarm_up=math.radians(120) * up, uarm_out=math.radians(55) * up,
                   uelbow=-math.radians(40) * up)
        return place_hands(poser, sp, curl=math.radians(10) * up)
    return SUMMON_FRAMES, spec, False


RISE_FRAMES, RISE_BREAK, RISE_SLAM = 150, 22, 92
DEPTH = 5.2


def rise_spec(poser):
    """From under the closed seal: the stones burst open at RISE_BREAK, the body rises out of the fissure, the lower
    arms reach out and slam down on the ground at RISE_SLAM, the crown rings unfold and the plates part in a roar."""
    def spec(t, f):
        burst = T.ease(RISE_BREAK - 6, RISE_BREAK + 4, f)
        rise = 1 - (1 - T.ease(RISE_BREAK, 112, f)) ** 2
        reach = T.pulse(f, 60, 80, RISE_SLAM)
        unfold = T.ease(104, 132, f)
        roar = T.pulse(f, 110, 124, 148)
        sp = specs(sink=DEPTH * (1 - rise), lean=math.radians(10) * (1 - rise) - math.radians(8) * roar,
                   head=-math.radians(16) * roar, plates=math.radians(34) * roar, rings=0.05 + 0.95 * unfold,
                   spin=(2.0 * unfold, -2.0 * unfold, 1.0 * unfold), uarm_up=math.radians(10) * roar,
                   seal=math.radians(58) * (1 - burst) - math.radians(10) * T.pulse(f, RISE_BREAK, RISE_BREAK + 4, RISE_BREAK + 16),
                   seal_lift=0.25 * T.pulse(f, RISE_BREAK - 4, RISE_BREAK + 2, RISE_BREAK + 14))
        pose = poser.solve(sp)
        move = pose["Chest"] @ poser.rest["Chest"].inverted()
        hands = {}
        for side, lg in lower_arms().items():
            sx = -1 if side == "L" else 1
            carried = move @ lg.foot
            raised = carried + G(0.3 * sx, 1.2, 0.2)
            planted = lg.foot.copy()
            follow = 1 - T.ease(70, RISE_SLAM, f)
            hands[side] = planted.lerp(raised, follow) + G(0, 0.9, 0.0) * reach * (1 - follow)
        return place_hands(poser, sp, hands, curl=math.radians(25) * T.pulse(f, RISE_SLAM - 4, RISE_SLAM, 120))
    return RISE_FRAMES, spec, False


DEATH_FRAMES, DEATH_SEAL = 150, 128


def death_spec(poser):
    """The plates close, the rings shrink away, the hands claw the ground and are dragged under as it sinks, the seal
    stones lean in and close over the fissure (shut at DEATH_SEAL)."""
    def spec(t, f):
        shudder = math.sin(f * 1.9) * T.pulse(f, 0, 12, 40)
        rings = 1 - T.ease(16, 56, f)
        sink = DEPTH * T.ease(36, 124, f) ** 1.4
        close = T.ease(80, DEATH_SEAL, f)
        sp = specs(sink=sink, lean=math.radians(14) * T.ease(10, 60, f), roll=math.radians(4) * shudder,
                   head=math.radians(18) * T.ease(10, 60, f) - math.radians(18) * T.pulse(f, 0, 8, 30),
                   plates=math.radians(30) * T.pulse(f, 0, 8, 30), rings=rings, spin=(0.4 * f / 30, -0.4 * f / 30, 0.2 * f / 30),
                   uarm_up=math.radians(40) * T.pulse(f, 10, 40, 90), uarm_out=math.radians(20) * T.pulse(f, 10, 40, 90),
                   seal=math.radians(58) * close + math.radians(4) * math.sin(f * 0.8) * close * (1 - T.ease(DEATH_SEAL - 6, DEATH_SEAL, f)))
        pose = poser.solve(sp)
        move = pose["Chest"] @ poser.rest["Chest"].inverted()
        drag = T.ease(60, 120, f)
        hands = {s: lg.foot.lerp(move @ lg.foot, drag) for s, lg in lower_arms().items()}
        return place_hands(poser, sp, hands, curl=math.radians(30) * T.ease(30, 60, f))
    return DEATH_FRAMES, spec, False


CLIPS = {"Idle": idle_spec, "Rise": rise_spec, "Summon": summon_spec, "Hit": hit_spec, "Death": death_spec}
BODY_RIGID = {"Tendril", "Sternum", "Shoulder", "FacePlate", "HeadCore", "Hand", "Finger", "Palm", "Rubble", "Elbow"}
SEAL_RIGID = {"SealStone", "Ring", "RingKnot", "Corruption"}
LOD_DROP = {"Neck": "Chest", **{f"UArm_{s}_Hand": f"UArm_{s}_Fore" for s, _ in SIDES}}


def seal_extra():
    """The glyph on both broad faces of every seal stone (right side; the bake mirrors the left) and a band of small
    glyphs round each crown ring."""
    out = []
    for i in (0, 1, 3, 5):
        base = stone_base(i)
        rot_m = stone_rot(i)
        sz = STONE_SIZE[i]
        c = base + rot_m @ Vector((0, 0, sz[2] * 0.9))
        n_in = rot_m @ Vector((0, 1, 0))
        n_in = n_in if n_in.dot(-stone_dir(i)) > 0 else -n_in
        up = rot_m @ Vector((0, 0, 1))
        for n in (n_in, -n_in):
            out.append(T.seal_rune(c + n * sz[1] * 0.85, n, up, sz[0] * 0.85, GOLD, glow=0.9, depth=0.012,
                                   line_col=(0.9, 0.6, 0.25), facing=0.4, thickness=sz[1] * 0.6, line=1.2, wear=0.35,
                                   seed=i))
    for name, radius, normal, tube in RINGS:
        out.append(T.rune_band(CROWN_C, normal, radius + tube * 0.9, GOLD, glow=0.6, height=tube * 1.1, count=7))
    return T.B.chain_extras(*out)


def palm_extra():
    out = []
    sx = 1
    uw = T.gp(UPPER["wrist"], sx)
    palm_c = uw + G(0.03 * sx, 0.28, 0.06)
    out.append(T.seal_rune(palm_c + G(-0.06, 0, 0), G(-1, 0, 0), T.rot(pitch=-15) @ Vector((0, -0.2, 1)).normalized(), 0.26,
                           GOLD, glow=1.0, depth=0.008, line_col=(0.9, 0.6, 0.25), facing=0.3, thickness=0.1, line=1.4))
    return T.B.chain_extras(H.weathering(K, scratches=0.5, dust=0.4, seed=201, markings=PAL.dark, marking_amount=0.2), *out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("sealed", [(body_parts(), (0.65, 0.6, 0.55), None), (seal_parts(), (0.25, 0.22, 0.28), (0.6, 0.3, 1.0))],
                   bones(), CLIPS, out, (0, 0, 2.5), 19.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Idle", 8), ("top", "top", "Idle", 8),
                    ("summon", "close", "Summon", SUMMON_PEAK), ("rise", "close", "Rise", 60),
                    ("dead", "close", "Death", 110)],
                   [("Rise", "close", range(0, RISE_FRAMES + 1, 12)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12)),
                    ("Summon", "close", range(0, SUMMON_FRAMES + 1, 10))],
                   scale_bones=SCALE_BONES)
        return
    C.reset_scene()
    mats = [T.Mat(body_parts(), 25500, ("rock", "membrane", "chitin"), voxel=0.014, ao=0.4, k=K, emission=2.6,
                  specular=0.3, extra=palm_extra(), rigid=BODY_RIGID, blend=2, lod_share=0.72,
                  islands=["Plate_L", "Plate_R"]),
            T.Mat(seal_parts(), 9000, ("rock", "crystal"), suffix="Seal", voxel=0.011, ao=0.3, extrusion=0.05, ray=0.14,
                  k=K * 0.6, emission=2.6, specular=0.35, extra=seal_extra(), rigid=SEAL_RIGID, blend=0, smooth=3,
                  lod_share=0.28, islands=[f"Seal_{i + 1}" for i in range(7)])]
    report = {"pivot": "ground at the centre of the seal; the body extends ~0.7 below the ground (hidden by terrain)",
              "rise": {"frames": RISE_FRAMES, "seal_breaks_frame": RISE_BREAK, "hands_slam_frame": RISE_SLAM,
                       "start_depth_m": DEPTH, "note": "starts fully under the ground plane: terrain must occlude"},
              "summon": {"peak_frame": SUMMON_PEAK, "socket": "Head (the violet column between the face plates)"},
              "death": {"seal_shut_frame": DEATH_SEAL, "below_ground_from_frame": 124},
              "seal_stone_radius_m": STONE_R, "glyph_colour": GOLD}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=14000, lod_drop=LOD_DROP, scale_bones=SCALE_BONES,
        report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("sealed", arm, mesh, actions, lod, out, assets, (0, 0, 2.5), 19.0, ("Idle", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Idle", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Idle", 12), ("back", "back", "Idle", 40), ("summon", "close", "Summon", SUMMON_PEAK),
                      ("rise", "close", "Rise", 70), ("dying", "close", "Death", 100), ("sealed", "close", "Death", DEATH_FRAMES)],
               strips=[("Rise", "close", range(0, RISE_FRAMES + 1, 12)), ("Summon", "close", range(0, SUMMON_FRAMES + 1, 10)),
                       ("Death", "close", range(0, DEATH_FRAMES + 1, 12)), ("Idle", "front", range(0, IDLE_FRAMES, 15))],
               macros=[("head", G(0, 4.1, 0.6), 7.0, -25, 10, "Summon", SUMMON_PEAK),
                       ("stone", stone_base(5) + G(0, 0.9, 0), 6.0, -20, 15, "Idle", 20),
                       ("chest", G(0.3, 2.4, 0.8), 7.0, -35, 15, "Idle", 20)],
               game_clip=("Idle", 20))


if __name__ == "__main__":
    main()
