"""Cinder Matriarch: the continent-2 boss (Ashen Reach) and her fire larva (round 16, see ArtSource/DESIGN_NOTES.md).

A tall four-stilt walker, mother of the Slagstrider line: a slag-shingled thorax, a long wedge head whose jaw splits
into four glowing mandibles under back-swept ember-Crysium horns, two ember scythes folded under the head, and a huge
low-slung kiln-sac of slag floes and molten cracks behind, with three larva chimneys (bones Vent_1..3) and a
dripping tip (bone Drip: the burning trail).

Two materials (1024 + 512 Low each): `CinderMatriarch_Mat` (slag body) and `CinderMatriarch_Crystal_Mat` (ember
Crysium horns, spines and knee spikes). Clips: Walk (loop), Idle (loop), Hit, Death, Spawn, Roar. Also exports
CinderMatriarch_LOD1 and the minion `FireLarva` (+ LOD1; clips Crawl, Idle, Hit, Death).

Run: blender --background --factory-startup --python Tools/Blender/build_cindermatriarch.py -- [--preview <dir>]
     [--norender] [--blockout] [--skip-larva]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402,F401
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import crysium as Y  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "CinderMatriarch"
LOD_NAME = NAME + "_LOD1"
LARVA = "FireLarva"
K = 4.0
UNITY_SCALE = 1.0
LARVA_SCALE = 0.5

# Charcoal slag a step lighter than the Slagstrider (she must separate from dark ash ground), orange-yellow molten.
PAL = H.Palette(top=(0.24, 0.2, 0.18), side=(0.09, 0.075, 0.068), belly=(0.035, 0.028, 0.025), seam=(0.025, 0.014, 0.01),
                dark=(0.055, 0.042, 0.036), glow=(1.0, 0.48, 0.1), eye=(1.0, 0.86, 0.45), membrane=(0.5, 0.13, 0.03),
                bone=(0.58, 0.5, 0.42))
HOT = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=PAL.dark, glow=(1.0, 0.68, 0.24),
                eye=PAL.eye, membrane=PAL.membrane, bone=PAL.bone)
WHITE_HOT = (1.0, 0.86, 0.55)

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

# Stilt legs (right side): hip, foot, (thigh, shin). The sum is far above the hip-foot distance, so the knees rise
# above the back.
LEG_TABLE = [((0.42, 1.5, 0.55), (2.0, 0.0, 1.55), (1.5, 2.5)),
             ((0.42, 1.45, -0.35), (2.15, 0.0, -1.45), (1.6, 2.62))]
MOUTH = G(0, 1.24, 2.12)
HEAD_AXIS = (G(0, 1.24, 2.12) - G(0, 1.5, 1.25)).normalized()
MANDS = [("Mand_UL", -45.0), ("Mand_UR", 45.0), ("Mand_LL", -135.0), ("Mand_LR", 135.0)]
VENTS = [("Vent_1", "Sac_1", G(0, 1.72, -1.0), G(0, 1.0, -0.25)),
         ("Vent_2", "Sac_2", G(0.42, 1.5, -1.75), G(0.45, 1.0, -0.4)),
         ("Vent_3", "Sac_2", G(-0.42, 1.5, -1.75), G(-0.45, 1.0, -0.4))]
VENT_NAMES = [v[0] for v in VENTS]
SCYTHE = {"L": (G(-0.36, 1.3, 0.9), G(-0.78, 0.78, 1.5), G(-0.62, 1.45, 2.2)),
          "R": (G(0.36, 1.3, 0.9), G(0.78, 0.78, 1.5), G(0.62, 1.45, 2.2))}


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.12, 1.0, 0.0))


def mouth_frame():
    a = HEAD_AXIS
    up = (Z - a * a.dot(Z)).normalized()
    right = up.cross(a).normalized()
    return a, up, right


def mand_dir(angle):
    a, up, right = mouth_frame()
    phi = math.radians(angle)
    return (up * math.cos(phi) + right * math.sin(phi)).normalized()


def mand_axis(angle):
    return HEAD_AXIS.cross(mand_dir(angle)).normalized()


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def body_parts():
    b = C.PartBuilder()
    k = K
    # Thorax: dark rock with a fissure net, under a mantle of slag shingles.
    b.ellipsoid("Thorax", G(0, 1.5, 0.15), (0.55, 0.78, 0.45), T.forward_frame(G(0, 1.5, 1), G(0, 1.5, -1)),
                bone="Body", segments=(48, 24), style=S.core(PAL, k * 0.7, seed=1, cells=60, width=0.016, glow=0.8))
    for i, (f, h, w, l) in enumerate(((0.55, 1.88, 0.42, 0.36), (0.12, 1.92, 0.5, 0.4), (-0.32, 1.86, 0.5, 0.38))):
        b.ellipsoid("Mantle", G(0, h, f), (w, l, 0.13), T.rot(pitch=-8), bone="Body", segments=(36, 18),
                    deform=C.chiselled(18, 20 + i, 0.08, squash=(0.7, 1.0)),
                    style=K2.armour_plate(PAL, k * 0.55, rim_glow=1.0, chevron=0.9, lip=0.7, cells=8, seed=21 + i))
        for side, sx in SIDES:
            b.ellipsoid("Mantle", G(0.42 * sx, h - 0.2, f - 0.02), (w * 0.55, l * 0.85, 0.11), T.rot(pitch=-8, roll=-34 * sx),
                        bone="Body", segments=(28, 14), deform=C.chiselled(14, 30 + i, 0.1, squash=(0.7, 1.0)),
                        style=K2.armour_plate(PAL, k * 0.55, rim_glow=0.9, chevron=0.0, lip=0.6, cells=6, seed=31 + i))
    # Neck and head: a long slag wedge, a glowing mouth under four mandibles.
    b.tube("Neck", [G(0, 1.55, 0.7), G(0, 1.58, 1.0), G(0, 1.52, 1.3)], [0.3, 0.26, 0.24], sides=16, chain=["Neck"],
           style=S.pillar(PAL, k * 0.5, drums=3, seed=40, glow=0.7))
    head_c = G(0, 1.42, 1.66)
    b.ellipsoid("Head", head_c, (0.38, 0.33, 0.66), T.frame_from(HEAD_AXIS), bone="Head", segments=(40, 20),
                deform=C.chiselled(14, 41, 0.07, squash=(0.8, 1.0)),
                style=S.slab(PAL, k * 0.6, strata=30, veins=0.9, fault=0.8, seed=42))
    b.ellipsoid("Brow", G(0, 1.62, 1.6), (0.36, 0.42, 0.12), T.rot(pitch=12), bone="Head", segments=(32, 16),
                deform=C.chiselled(12, 43, 0.08, squash=(0.7, 1.0)),
                style=K2.armour_plate(PAL, k * 0.55, rim_glow=0.6, chevron=1.0, lip=0.8, seed=44))
    b.ellipsoid("Mouth", MOUTH - HEAD_AXIS * 0.08, (0.17, 0.17, 0.12), T.frame_from(HEAD_AXIS), bone="Head",
                segments=(24, 12), style=H.inner_glow(HOT, 1.0))
    for sx in (-1, 1):
        for j, (x, u, f) in enumerate(((0.2, 1.52, 1.86), (0.25, 1.42, 1.72))):
            b.ellipsoid("Eye", G(x * sx, u, f), (0.045, 0.045, 0.03), bone="Head", segments=(12, 8),
                        style=H.eye(PAL, k * 0.3, 0.95))
    a, up, right = mouth_frame()
    for name, ang in MANDS:
        d = mand_dir(ang)
        root = MOUTH - a * 0.12 + d * 0.13
        pts = [root, root + a * 0.3 + d * 0.16, root + a * 0.62 + d * 0.06, root + a * 0.8 - d * 0.14]
        b.tube("Mandible", pts, [0.1, 0.085, 0.05, 0.006], sides=10, chain=[name], flatten=0.55,
               style=H.blade(HOT, k * 0.35, glow=0.9, teeth=4))
    # The kiln-sac: two overlapping molten ellipsoids, a crust of slag floes on top, three chimneys, a dripping tip.
    b.ellipsoid("SacFront", G(0, 1.18, -0.95), (0.78, 0.7, 0.62), T.rot(pitch=14), bone="Sac_1", segments=(48, 24),
                style=K3.molten_sac(HOT, k * 0.6, cells=38, seed=50, glow=1.0))
    b.ellipsoid("SacRear", G(0, 0.95, -1.85), (0.92, 0.85, 0.7), T.rot(pitch=22), bone="Sac_2", segments=(48, 24),
                style=K3.molten_sac(HOT, k * 0.6, cells=44, seed=51, glow=1.0))
    for i, (x, u, f, r, bone) in enumerate(((0.0, 1.78, -0.62, (0.4, 0.34, 0.1), "Sac_1"),
                                             (0.42, 1.62, -1.3, (0.36, 0.32, 0.1), "Sac_2"),
                                             (0.0, 1.66, -1.62, (0.34, 0.36, 0.1), "Sac_2"),
                                             (0.5, 1.28, -2.2, (0.32, 0.3, 0.1), "Sac_2"),
                                             (0.0, 1.2, -2.48, (0.3, 0.28, 0.1), "Sac_2"))):
            for side, sx in ([(None, 1)] if x == 0 else SIDES):
                b.ellipsoid("Floe", G(x * sx, u, f), r, T.rot(pitch=-20 - 12 * i, roll=-30 * sx * (x != 0)), bone=bone,
                            segments=(24, 12), deform=C.chiselled(10, 60 + i, 0.15),
                            style=S.slab(PAL, k * 0.6, strata=26, veins=0.9, fault=1.0, seed=61 + i))
    for name, parent, base, dirn in VENTS:
        d = dirn.normalized()
        top = base + d * 0.42
        b.tube("Chimney", [base - d * 0.1, base + d * 0.2, top], [0.2, 0.15, 0.17], sides=14, chain=[name],
               style=S.pillar(PAL, k * 0.5, drums=3, seed=70, glow=0.8), cap=0.15)
        b.ellipsoid("ChimneyFire", top + d * 0.01, (0.12, 0.12, 0.05), T.frame_from(d), bone=name, segments=(16, 8),
                    style=T.solid_glow(WHITE_HOT, 1.0))
    b.ellipsoid("DripTip", G(0, 0.58, -2.5), (0.2, 0.2, 0.28), bone="Drip", segments=(20, 10),
                style=K3.molten_sac(HOT, k * 0.4, cells=10, seed=80, glow=1.0))
    # Stilt legs: basalt drums, glowing knees, heat-proof foot cones.
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        b.ellipsoid("Hip", hip, (0.16,) * 3, bone=lg.names[0], segments=(20, 10),
                    style=S.slab(PAL, k * 0.5, seed=90 + i))
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.05)), knee], [0.14, 0.12, 0.09], sides=12,
               chain=[lg.names[0]], style=S.pillar(PAL, k * 0.5, drums=3, seed=92 + i, glow=0.35))
        b.ellipsoid("Knee", knee, (0.13,) * 3, bone=lg.names[0], segments=(16, 8),
                    style=S.core(PAL, k * 0.4, seed=94 + i, cells=12, glow=0.95))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("Shin", [knee, knee.lerp(foot, 0.6), foot + Vector((0, 0, 0.32))], [0.09, 0.075, 0.1], sides=12,
               chain=[lg.names[1]], style=S.pillar(PAL, k * 0.5, drums=4, seed=96 + i, glow=0.35))
        b.tube("FootCone", [foot + Vector((0, 0, 0.4)), foot + Vector((0, 0, 0.16)), foot],
               [0.11, 0.18, 0.12], sides=12, chain=[lg.names[1]], style=S.pillar(PAL, k * 0.5, drums=3, seed=98 + i,
                                                                               glow=0.8), cap=0.3)
    # Ember scythes folded under the head.
    for side, sx in SIDES:
        sh, el, tip = SCYTHE[side]
        b.tube("ScytheArm", [sh, sh.lerp(el, 0.5), el], [0.13, 0.11, 0.09], sides=12, chain=[f"Scythe_{side}_Arm"],
               style=H.leg(PAL, k * 0.4, "Femur", spines=3))
        mid = el.lerp(tip, 0.5) + G(0.0, 0.05, 0.1)
        b.tube("ScytheBlade", [el, mid, tip], [0.13, 0.1, 0.006], sides=10, chain=[f"Scythe_{side}_Blade"],
               flatten=0.35, style=H.blade(HOT, k * 0.35, glow=1.0, teeth=6))
    return b


def crystal_parts():
    b = C.PartBuilder()
    # Horn crown: two great back-swept horns and two smaller, plus a pair over the brow.
    for side, sx in SIDES:
        b.shard("Horn", G(0.2 * sx, 1.68, 1.5), G(0.45 * sx, 0.55, -1.0), 0.11, 1.15, sides=8, flute=0.42,
                twist=1.1 * sx, bend=G(0, 0.25, 0), bone="Head", style=Y.shard_style("ember", 0.9, seed=100))
        b.shard("HornSmall", G(0.3 * sx, 1.55, 1.75), G(0.8 * sx, 0.45, -0.6), 0.06, 0.5, sides=6, flute=0.4,
                twist=0.9 * sx, bone="Head", style=Y.shard_style("ember", 0.85, seed=101))
    # Spine ridge on the mantle, tallest at the front, swept back.
    for i, (f, h, r, l) in enumerate(((0.62, 1.98, 0.09, 0.6), (0.2, 2.02, 0.085, 0.52), (-0.24, 1.96, 0.075, 0.44))):
        b.shard("Spine", G(0, h, f), G(0, 1.0, -0.75), r, l, sides=8, flute=0.45, bone="Body",
                style=Y.shard_style("ember", 0.8, seed=110 + i))
    # Shards breaking out of the sac crust round the chimneys.
    for i, (x, u, f, d, l, bone) in enumerate(((0.32, 1.6, -0.8, (0.6, 0.8, -0.2), 0.36, "Sac_1"),
                                                (0.7, 1.32, -1.55, (0.9, 0.6, -0.2), 0.32, "Sac_2"),
                                                (0.28, 1.5, -2.15, (0.4, 0.8, -0.6), 0.3, "Sac_2"))):
        for side, sx in SIDES:
            b.shard("SacShard", G(x * sx, u, f), G(d[0] * sx, d[1], d[2]), 0.06, l, sides=6, flute=0.4,
                    twist=0.8 * sx, bone=bone, style=Y.shard_style("ember", 0.85, seed=120 + i))
    # Knee spikes: one per stilt knee, pointing up and out (the knees are the highest points from the camera).
    for (side, i), lg in legs().items():
        sx = -1 if side == "L" else 1
        hip, knee, foot = lg.rest_points
        b.shard("KneeSpike", knee + Vector((0, 0, 0.06)), G(0.35 * sx, 1.0, -0.3), 0.07, 0.45, sides=6, flute=0.4,
                twist=0.8 * sx, bone=lg.names[0], style=Y.shard_style("ember", 0.85, seed=130 + i))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.4, 0), None, False),
           ("Body", G(0, 1.5, -0.4), G(0, 1.55, 0.7), "Root", True),
           ("Neck", G(0, 1.55, 0.7), G(0, 1.52, 1.3), "Body", True),
           ("Head", G(0, 1.52, 1.3), MOUTH, "Neck", True)]
    for name, ang in MANDS:
        d = mand_dir(ang)
        root = MOUTH - HEAD_AXIS * 0.12 + d * 0.13
        out.append((name, root, root + HEAD_AXIS * 0.5, "Head", True))
    out += [("Sac_1", G(0, 1.4, -0.45), G(0, 1.12, -1.35), "Body", True),
            ("Sac_2", G(0, 1.12, -1.35), G(0, 0.8, -2.45), "Sac_1", True),
            ("Drip", G(0, 0.7, -2.45), G(0, 0.4, -2.55), "Sac_2", True)]
    for name, parent, base, d in VENTS:
        out.append((name, base - d.normalized() * 0.1, base + d.normalized() * 0.35, parent, True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    for side, _ in SIDES:
        sh, el, tip = SCYTHE[side]
        out += [(f"Scythe_{side}_Arm", sh, el, "Neck", True), (f"Scythe_{side}_Blade", el, tip, f"Scythe_{side}_Arm", True)]
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs(off=None, pitch=0.0, roll=0.0, yaw=0.0, neck=0.0, head=0.0, head_yaw=0.0, mand=0.0, sac=0.0, sac2=0.0,
          sac_roll=0.0, vent=1.0, scythe_up=0.0, scythe_out=0.0, blade=0.0, drip=0.0):
    """Hinge specs; angles in radians. Positive pitch raises the front (nose up), neck/head likewise."""
    out = {"Root": {"rot": []},
           "Body": {"rot": [(Z, yaw), (Yv, roll), (X, -pitch)], "loc": off if off is not None else Vector()},
           "Neck": {"rot": [(Z, head_yaw * 0.5), (X, -neck)]},
           "Head": {"rot": [(Z, head_yaw * 0.5), (X, -head)]},
           "Sac_1": {"rot": [(X, -sac), (Yv, sac_roll)]},
           "Sac_2": {"rot": [(X, -sac2), (Yv, sac_roll * 0.6)]},
           "Drip": {"rot": [(X, drip)]}}
    m = mand if isinstance(mand, dict) else {n: mand for n, _ in MANDS}
    for name, ang in MANDS:
        out[name] = {"rot": [(mand_axis(ang), m[name])]}
    v = vent if isinstance(vent, dict) else {n: vent for n in VENT_NAMES}
    for n in VENT_NAMES:
        out[n] = {"rot": [], "scale": v[n]}
    for side, sx in SIDES:
        out[f"Scythe_{side}_Arm"] = {"rot": [(Z, -scythe_out * sx), (X, -scythe_up)]}
        out[f"Scythe_{side}_Blade"] = {"rot": [(X, -blade)]}
    return out


WALK_FRAMES = 56
STRIDE, LIFT, STANCE = 0.85, 0.32, 0.66
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(off=Vector((0, 0, 0.04 * math.cos(2 * w))), roll=math.radians(3) * math.sin(w),
                   yaw=math.radians(2.5) * math.sin(w), pitch=math.radians(1.5) * math.sin(2 * w + 0.4),
                   neck=math.radians(3) * math.sin(2 * w + 1.0), head=-math.radians(3) * math.sin(2 * w + 1.0),
                   head_yaw=math.radians(5) * math.sin(w), mand=math.radians(4) + math.radians(3) * math.sin(2 * w),
                   sac=math.radians(3) * math.sin(2 * w - 1.2), sac2=math.radians(4) * math.sin(2 * w - 1.8),
                   sac_roll=math.radians(5) * math.sin(w - 0.8), scythe_up=math.radians(4) * math.sin(w),
                   drip=math.radians(6) * math.sin(2 * w - 2.0))
        return K3.place_legs(poser, sp, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def planted(lg):
    return {key: l.foot.copy() for key, l in lg.items()}


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        breathe = 0.5 + 0.5 * math.sin(w)
        sp = specs(off=Vector((0, 0, 0.03 * math.sin(w))), roll=math.radians(1.5) * math.sin(w),
                   pitch=math.radians(1.5) * math.sin(w + 0.3), neck=math.radians(4) * math.sin(w),
                   head=math.radians(5) * math.sin(2 * w), head_yaw=math.radians(14) * math.sin(w + 1.0),
                   mand=math.radians(5) + math.radians(10) * max(0.0, math.sin(2 * w + 0.5)) ** 2,
                   sac=math.radians(2) * breathe, sac2=math.radians(3) * breathe,
                   vent={n: 1.0 + 0.06 * math.sin(w * 2 + i * 2.1) for i, n in enumerate(VENT_NAMES)},
                   scythe_up=math.radians(3) * math.sin(w + 0.6), blade=math.radians(4) * math.sin(w + 0.6))
        return K3.place_legs(poser, sp, lg, planted(lg))
    return 90, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(off=Vector((0, 0.1 * k, 0.05 * k)), pitch=math.radians(6) * k, roll=math.radians(4) * k,
                   neck=math.radians(10) * k, head=math.radians(8) * k, mand=math.radians(4 + 18 * max(k, 0)),
                   sac=-math.radians(5) * k, sac2=-math.radians(6) * k, scythe_up=math.radians(12) * k,
                   scythe_out=math.radians(8) * k)
        return K3.place_legs(poser, sp, lg, planted(lg))
    return 16, spec, False


SPAWN_FRAMES, SPAWN_RELEASE = 64, 30


def spawn_spec(poser):
    """The sac heaves, the chimneys swell and open (bone scale) and the larvae leave the chimney mouths at
    SPAWN_RELEASE (one per chimney, staggered by 3 frames: Vent_1 first)."""
    lg = legs()

    def spec(t, f):
        heave = T.pulse(f, 6, 26, 50)
        crouch = T.pulse(f, 0, 18, 40)
        vents = {}
        for i, n in enumerate(VENT_NAMES):
            vents[n] = 1.0 + 0.7 * T.pulse(f, 14 + 3 * i, SPAWN_RELEASE + 3 * i, 48 + 3 * i)
        sp = specs(off=Vector((0, 0.05 * heave, -0.18 * crouch + 0.08 * heave)), pitch=-math.radians(5) * crouch,
                   neck=math.radians(14) * heave, head=math.radians(6) * heave,
                   mand=math.radians(6 + 26 * T.pulse(f, 18, 30, 46)),
                   sac=-math.radians(10) * heave, sac2=-math.radians(12) * heave + math.radians(6) * crouch,
                   sac_roll=math.radians(3) * math.sin(f * 0.9) * heave, vent=vents,
                   scythe_up=math.radians(10) * heave, scythe_out=math.radians(6) * heave)
        return K3.place_legs(poser, sp, lg, planted(lg))
    return SPAWN_FRAMES, spec, False


ROAR_FRAMES = 70


def roar_spec(poser):
    """Rears on the hind stilts: the front feet lift off and paw the air, scythes spread high, mandibles wide."""
    lg = legs()

    def spec(t, f):
        lift = T.ease(4, 22, f) * (1 - T.ease(54, 68, f))
        jaw = T.ease(14, 24, f) * (1 - T.ease(50, 62, f))
        tremble = math.sin(f * 2.4) * T.pulse(f, 20, 34, 52)
        sp = specs(off=Vector((0, 0.25 * lift, 0.35 * lift)), pitch=math.radians(22) * lift,
                   neck=math.radians(14) * lift + math.radians(2) * tremble, head=math.radians(8) * lift,
                   head_yaw=math.radians(4) * tremble, mand=math.radians(4 + 40 * jaw),
                   sac=-math.radians(14) * lift, sac2=-math.radians(6) * lift,
                   scythe_up=math.radians(70) * lift, scythe_out=math.radians(28) * lift,
                   blade=math.radians(-40) * lift + math.radians(6) * tremble)
        pose = poser.solve(sp)
        move = pose["Body"] @ poser.rest["Body"].inverted()
        feet = planted(lg)
        for (side, i), l in lg.items():
            if i == 1:
                sx = -1 if side == "L" else 1
                carried = move @ l.foot
                paw = math.sin(f * 0.35 + (0 if sx < 0 else 1.6)) * 0.12 * lift
                feet[(side, i)] = l.foot.lerp(carried, lift * 0.85) + G(0.15 * sx, 0.15 + paw, -0.3) * lift
        return K3.place_legs(poser, sp, lg, feet)
    return ROAR_FRAMES, spec, False


DEATH_FRAMES = 72


def death_spec(poser):
    """The stilts buckle outwards, she drops onto the sac, the chimneys burst wide (bone scale) and the head falls."""
    lg = legs()

    def spec(t, f):
        fall = T.ease(6, 40, f)
        jerk = T.pulse(f, 0, 5, 12)
        crack = T.ease(30, 48, f)
        sp = specs(off=Vector((0, 0.05 * jerk, 0.08 * jerk - 0.95 * fall)), pitch=-math.radians(12) * fall,
                   roll=math.radians(10) * fall, neck=-math.radians(22) * fall, head=-math.radians(10) * fall,
                   mand=math.radians(4 + 34 * T.pulse(f, 2, 10, 30) + 18 * crack),
                   sac=math.radians(14) * fall, sac2=math.radians(10) * fall, vent=1.0 + 0.55 * crack,
                   scythe_up=-math.radians(20) * fall, scythe_out=math.radians(30) * fall, blade=math.radians(30) * fall,
                   drip=math.radians(20) * fall)
        return K3.splay_feet(poser, sp, lg, fall, twitch=math.sin(f * 1.3) * (1 - fall), out=1.25)
    return DEATH_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Spawn": spawn_spec,
         "Roar": roar_spec}
BODY_RIGID = {"Mantle", "Head", "Brow", "Floe", "Mandible", "ScytheBlade", "Chimney", "ChimneyFire"}
CRYSTAL_RIGID = {"Horn", "HornSmall", "Spine", "SacShard", "KneeSpike"}
LOD_DROP = {"Drip": "Sac_2", "Mand_LL": "Head", "Mand_LR": "Head",
            **{f"Scythe_{s}_Blade": f"Scythe_{s}_Arm" for s, _ in SIDES}}
LOD_AIMS = {}


# ------------------------------------------------------------------------------------------------------------
# Fire larva (the minion)
# ------------------------------------------------------------------------------------------------------------

LARVA_SEGS = [(0.32, 0.075, 0.095, 0.075), (0.16, 0.095, 0.115, 0.09), (0.0, 0.1, 0.12, 0.095),
              (-0.16, 0.09, 0.11, 0.085), (-0.3, 0.07, 0.09, 0.07)]  # (fwd, up, half width, half height)
LARVA_LEGS = [((0.09, 0.06, 0.16), (0.15, 0.0, 0.19), (0.05, 0.06)),
              ((0.1, 0.06, 0.0), (0.16, 0.0, 0.0), (0.05, 0.06)),
              ((0.09, 0.055, -0.16), (0.15, 0.0, -0.19), (0.05, 0.06))]
LPAL = H.Palette(top=(0.28, 0.22, 0.19), side=(0.1, 0.075, 0.065), belly=(0.04, 0.03, 0.025), seam=(0.03, 0.015, 0.01),
                 dark=(0.06, 0.045, 0.04), glow=(1.0, 0.55, 0.14), eye=(1.0, 0.9, 0.55), membrane=(0.55, 0.16, 0.04),
                 bone=(0.62, 0.52, 0.42))


def larva_legs():
    return K3.limbs(LARVA_LEGS, pole=(0.4, 1.0, 0.0))


def larva_parts():
    b = C.PartBuilder()
    k = 1.0
    for i, (f, u, w, h) in enumerate(LARVA_SEGS):
        b.ellipsoid("Seg", G(0, u + 0.02, f), (w, 0.1, h), bone=f"Seg_{i + 1}", segments=(28, 14),
                    deform=C.chiselled(12, 5 + i, 0.06, squash=(0.8, 1.0)),
                    style=K2.armour_plate(LPAL, k * 0.6, rim_glow=1.0, chevron=0.8 if i < 2 else 0.4, lip=0.6, seed=9 + i))
    b.ellipsoid("Belly", G(0, 0.06, 0.0), (0.08, 0.3, 0.05), bone="Seg_3", segments=(24, 12),
                style=K3.molten_sac(LPAL, k * 0.5, cells=14, seed=30, glow=1.0))
    b.parts[-1].chain = ["Seg_3", "Seg_2", "Seg_4"]
    b.ellipsoid("Head", G(0, 0.09, 0.44), (0.07, 0.075, 0.06), bone="Head", segments=(24, 12),
                style=K3.molten_sac(LPAL, k * 0.4, cells=8, seed=31, glow=0.9))
    for side, sx in SIDES:
        root = G(0.035 * sx, 0.07, 0.5)
        b.tube("Mandible", [root, root + G(0.04 * sx, 0.0, 0.06), root + G(0.0, -0.01, 0.11)], [0.018, 0.013, 0.002],
               sides=8, chain=[f"Mand_{side}"], flatten=0.6, style=H.blade(LPAL, k * 0.3, glow=1.0, teeth=2))
    for (side, i), lg in larva_legs().items():
        hip, knee, foot = lg.rest_points
        b.tube("Leg", [hip, knee, foot + Vector((0, 0, 0.01))], [0.022, 0.016, 0.006], sides=8, chain=list(lg.names),
               style=H.leg(LPAL, k * 0.3, "Tibia"))
    return b


def larva_bones():
    out = [("Root", G(0, 0, 0), G(0, 0.1, 0), None, True)]
    prev = "Root"
    for i, (f, u, w, h) in enumerate(LARVA_SEGS):
        out.append((f"Seg_{i + 1}", G(0, u, f + 0.08), G(0, u, f - 0.08), prev if i else "Root", True))
        prev = f"Seg_{i + 1}"
    out.append(("Head", G(0, 0.09, 0.38), G(0, 0.09, 0.5), "Seg_1", True))
    for side, sx in SIDES:
        out.append((f"Mand_{side}", G(0.035 * sx, 0.07, 0.5), G(0.04 * sx, 0.06, 0.61), "Head", True))
    for (side, i), lg in larva_legs().items():
        seg = ("Seg_2", "Seg_3", "Seg_4")[i - 1]
        out += lg.rest_bones(seg)
    return out


def larva_specs(wave=0.0, phase=0.0, lift=0.0, curl=0.0, mand=0.0, roll=0.0, drop=0.0):
    out = {"Root": {"rot": []}}
    for i in range(len(LARVA_SEGS)):
        a = wave * math.sin(phase - i * 1.1)
        v = lift * math.sin(phase - i * 1.1 + 1.0)
        out[f"Seg_{i + 1}"] = {"rot": [(Z, a), (X, -v + curl), (Yv, roll if i == 0 else 0.0)]}
    out["Seg_1"]["loc"] = Vector((0, 0, -drop))
    out["Head"] = {"rot": [(X, -lift * 0.6 * math.sin(phase + 1.5))]}
    for side, sx in SIDES:
        out[f"Mand_{side}"] = {"rot": [(Z, mand * sx)]}
    return out


def larva_place(poser, sp, feet):
    lg = larva_legs()
    pose = poser.solve(sp)
    for key, l in lg.items():
        seg = ("Seg_2", "Seg_3", "Seg_4")[key[1] - 1]
        move = pose[seg] @ poser.rest[seg].inverted()
        sp.update(l.pose(poser, move @ l.root, feet[key], move.to_3x3() @ l.pole))
    return sp


LARVA_PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("L", 3): 0.0, ("R", 1): 0.5, ("L", 2): 0.5, ("R", 3): 0.5}


def larva_crawl(poser):
    lg = larva_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = larva_specs(wave=math.radians(9), phase=w, lift=math.radians(5), mand=math.radians(8) * math.sin(2 * w))
        return larva_place(poser, sp, K3.gait_feet(lg, t, LARVA_PHASES, 0.6, 0.12, 0.04))
    return 20, spec, True


def larva_idle(poser):
    lg = larva_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = larva_specs(wave=math.radians(4), phase=w, lift=math.radians(3), mand=math.radians(14) * max(0, math.sin(2 * w)))
        return larva_place(poser, sp, {k_: l.foot for k_, l in lg.items()})
    return 48, spec, True


def larva_hit(poser):
    lg = larva_legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = larva_specs(wave=math.radians(10) * k, phase=1.0, lift=math.radians(12) * k, mand=math.radians(20) * max(k, 0))
        return larva_place(poser, sp, {k_: l.foot for k_, l in lg.items()})
    return 10, spec, False


def larva_death(poser):
    lg = larva_legs()

    def spec(t, f):
        c = T.ease(0.1, 0.7, t)
        sp = larva_specs(wave=math.radians(6) * (1 - c), phase=f * 0.8, lift=0.0, curl=math.radians(16) * c,
                         mand=math.radians(30) * c, roll=math.radians(70) * c, drop=0.04 * c)
        return larva_place(poser, sp, {k_: l.foot + G(0.05 * (1 if k_[0] == "R" else -1), 0.04 * c, 0) for k_, l in lg.items()})
    return 30, spec, False


LARVA_CLIPS = {"Crawl": larva_crawl, "Idle": larva_idle, "Hit": larva_hit, "Death": larva_death}


def build_larva():
    layout = [("Crawl", 0, (-0.6, 0.3), 0.2), ("Crawl", 7, (0.2, 0.5), -0.15), ("Idle", 20, (0.7, 0.0), 0.3),
              ("Crawl", 12, (-0.2, -0.3), 0.0), ("Crawl", 4, (0.5, -0.5), -0.3)]
    previews = dict(target=(0, 0.0, 0.1), distance=1.6, move=("Crawl", 4), layout=layout,
                    strips=[("Crawl", "side", range(0, 20, 3)), ("Death", "close", range(0, 31, 5))],
                    macros=[("head", G(0.0, 0.1, 0.42), 0.35, -35, 15)], rig_scale=0.8)
    K3.run_creature(LARVA, larva_parts(), larva_bones(), LARVA_CLIPS, tris=1450, lod_tris=700,
                    families=("rock", "membrane", "chitin"), game_scale=LARVA_SCALE, rigid={"Seg", "Mandible"}, blend=2,
                    lod_drop={**{f"Mand_{s}": "Head" for s, _ in SIDES},
                              **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2, 3)}},
                    lod_aims={f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES
                              for i in (1, 2, 3)},
                    extra=H.weathering(1.0, scratches=0.3, dust=0.2, seed=7), voxel=0.004, specular=0.25,
                    emission_strength=3.0,
                    report={"crawl_distance_per_loop": round(0.12 / 0.6, 3), "minion_of": NAME,
                            "spawn": "leaves the Matriarch's chimney mouths (bones Vent_1..3) at Spawn frame 30-36"},
                    previews=previews)


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("cinder", [(body_parts(), (0.2, 0.12, 0.08), None), (crystal_parts(), (1.0, 0.55, 0.15), (1.0, 0.5, 0.1))],
                   bones(), CLIPS, out, (0, 0, 1.2), 9.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("top", "top", "Walk", 8),
                    ("roar", "close", "Roar", 36), ("spawn", "close", "Spawn", 32), ("dead", "close", "Death", 72)],
                   [("Walk", "side", range(0, WALK_FRAMES, 7)), ("Roar", "side", range(0, ROAR_FRAMES, 9))],
                   scale_bones=VENT_NAMES)
        return
    if not args.get("skip-larva"):
        build_larva()
    C.reset_scene()
    mats = [T.Mat(body_parts(), 18000, ("rock", "membrane", "chitin"), voxel=0.012, ao=0.35, k=K, emission=3.0,
                  specular=0.25, extra=H.weathering(K, scratches=0.4, dust=0.3, seed=161), rigid=BODY_RIGID,
                  blend=2, lod_share=0.75),
            T.Mat(crystal_parts(), 5500, ("crystal", "rock"), suffix="Crystal", voxel=0.009, ao=0.2, extrusion=0.04,
                  ray=0.12, k=K * 0.6, emission=2.4, specular=0.4, rigid=CRYSTAL_RIGID, blend=0, smooth=2,
                  lod_share=0.25)]
    report = {"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, under the thorax",
              "spawn": {"release_frame": SPAWN_RELEASE, "stagger_frames": 3, "sockets": VENT_NAMES,
                        "socket_note": "larvae leave the chimney mouths: bone tail of Vent_n, chimney swells by scale"},
              "trail_origin": "bone Drip (sac tip, 0.4-0.7 above the ground)",
              "footfalls": "Walk frames 0 (L1, R2) and 28 (R1, L2)",
              "minion": {"fbx": f"../{LARVA}/{LARVA}.fbx", "unity_scale": LARVA_SCALE}}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=9000, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
        scale_bones=VENT_NAMES, report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    minions = []
    larva_path = os.path.join(C.BLEND, LARVA + ".blend")
    if os.path.exists(larva_path):
        la = B.append_base(LARVA)
        for x, y, yaw, fr in ((-0.9, -2.9, 0.4, 3), (0.6, -3.2, -0.3, 9), (-1.8, -3.6, 0.1, 14)):
            minions.append((la[0], la[1], la[2]["Crawl"], fr, (x, y, 0), yaw, LARVA_SCALE))
    T.previews("cinder", arm, mesh, actions, lod, out, assets, (0, 0, 1.2), 9.0, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40), ("roar", "close", "Roar", 38),
                      ("spawn", "close", "Spawn", SPAWN_RELEASE), ("dead", "close", "Death", DEATH_FRAMES)],
               strips=[("Walk", "side", range(0, WALK_FRAMES, 7)), ("Spawn", "close", range(0, SPAWN_FRAMES + 1, 8)),
                       ("Roar", "close", range(0, ROAR_FRAMES, 9)), ("Death", "close", range(0, DEATH_FRAMES + 1, 9))],
               macros=[("head", G(0, 1.45, 1.9), 2.2, -35, 14, "Roar", 38), ("sac", G(0.3, 1.3, -1.4), 2.4, -60, 30, "Idle", 20),
                       ("leg", G(1.1, 1.6, 1.1), 2.0, -50, 20, "Idle", 20)],
               minions=minions)


if __name__ == "__main__":
    main()
