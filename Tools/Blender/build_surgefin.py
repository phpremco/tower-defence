"""Surgefin: the water-element amphibian that surfaces out of water and swims faster on wet road (round 15, name
proposed; see ArtSource/DESIGN_NOTES.md).

A low newt-like body on four splayed paddle limbs, a crest of four translucent fin sails along the back with
coral glowing edges, a long tail with a vertical fin; a wide flat head with a lure organ on a stalk (bone Lure),
needle teeth along the lip, four small coral eyes and glowing gill slits. Wet petrol-teal skin with a paler belly
and fine spots: the element is in the body, the light is warm.

Round 49B (Remco: "somewhat more finished"): a finishing pass on the same body, rig and clips: five taller, ragged
sails with spines running on past the rim, a ridge of bony scutes down the spine, a pale ribbed belly, a lateral
line of glowing photophores, brow ridges and back-swept cheek spines, coral-lit external gill fronds behind the jaw,
a hooded lure bulb, a split double tail fin, and a less glossy skin (roughness 0.5, more pitting).

Clips: Walk (loop, sprawling with a side-to-side spine wave), Swim (loop, limbs tucked, strong wave), Idle (loop),
Hit, Death, Emerge (rises from under the water surface; the head breaks it at EMERGE_SURFACE_FRAME). Also exports
Surgefin_LOD1.

Run: blender --background --python Tools/Blender/build_surgefin.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import numpy as np  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
from common import G  # noqa: E402

NAME = "Surgefin"
K = 1.0
GAME_SCALE = 0.75  # proposal

# Water element in the body (deep petrol-teal, pale warm belly), coral-orange glow (warm).
PAL = H.Palette(top=(0.07, 0.24, 0.25), side=(0.03, 0.1, 0.11), belly=(0.02, 0.04, 0.045), seam=(0.01, 0.022, 0.026),
                dark=(0.02, 0.06, 0.07), glow=(1.0, 0.45, 0.24), eye=(1.0, 0.62, 0.3), membrane=(0.12, 0.3, 0.3),
                bone=(0.72, 0.6, 0.48))
BELLY = H.Palette(top=(0.42, 0.36, 0.3), side=(0.3, 0.26, 0.22), belly=(0.2, 0.17, 0.14), seam=(0.06, 0.08, 0.08),
                  dark=(0.1, 0.1, 0.09), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)
FIN = H.Palette(top=PAL.top, side=(0.03, 0.09, 0.1), belly=PAL.belly, seam=PAL.seam, dark=PAL.dark, glow=PAL.glow,
                eye=PAL.eye, membrane=(0.05, 0.15, 0.16), bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

SPINE = [G(0, 0.11, -0.12), G(0, 0.12, 0.08), G(0, 0.12, 0.27), G(0, 0.12, 0.5)]
TAIL = [G(0, 0.11, -0.12), G(0, 0.095, -0.3), G(0, 0.08, -0.48), G(0, 0.065, -0.7)]
SPINE_BONES = ["Spine_1", "Spine_2", "Head"]
TAIL_BONES = ["Tail_1", "Tail_2", "Tail_3"]
LEG_TABLE = [((0.085, 0.1, 0.2), (0.23, 0.0, 0.29), (0.11, 0.13)),
             ((0.085, 0.1, -0.07), (0.23, 0.0, -0.15), (0.11, 0.13))]
LEG_PARENT = {1: "Spine_2", 2: "Spine_1"}
# Round 49B: five taller sails, the middle ones highest, each with spines that run on past its ragged rim.
SAILS = [(0.2, 0.085, "Spine_2"), (0.09, 0.13, "Spine_2"), (-0.03, 0.14, "Spine_1"), (-0.15, 0.115, "Spine_1"),
         (-0.27, 0.08, "Tail_1")]
# External gill fronds behind the jaw (right side; mirrored): root, direction, length. Coral-lit feathery stalks
# that flare from the head like a crown seen from above.
FRONDS = [((0.1, 0.15, 0.3), (0.9, 0.55, -0.45), 0.13), ((0.105, 0.13, 0.27), (1.0, 0.25, -0.6), 0.15),
          ((0.1, 0.11, 0.24), (0.9, 0.0, -0.75), 0.13)]
# Photophores along the flank (lateral line): (forward, height) on the right side, mirrored.
PHOTOPHORES = [0.24, 0.17, 0.1, 0.03, -0.04, -0.11, -0.18, -0.25]  # forward positions along the lateral line


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.6, 1.0, 0.0))


def lateral_line(f):
    """A point on the right flank a little below the side's widest line at forward position f (torso or tail)."""
    if f > -0.12:
        x = 0.115 * math.sqrt(max(0.0, 1 - ((f - 0.07) / 0.23) ** 2)) * 0.95
        up = 0.11
    else:
        x = float(np.interp(-f, (0.12, 0.22, 0.3), (0.075, 0.062, 0.045))) * 0.95
        up = float(np.interp(-f, (0.12, 0.3), (0.105, 0.09)))
    return G(x, up, f), G(1, 0, 0)


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    b.ellipsoid("Torso", G(0, 0.12, 0.07), (0.115, 0.23, 0.075), bone="Spine_1", segments=(40, 20),
                style=K3.wet_skin(PAL, k, spots=70, seed=1, glow_spots=0.3, gloss=0.5))
    b.parts[-1].chain = ["Spine_1", "Spine_2"]
    # A pale, ribbed belly (finished underside), a ridge of bony scutes down the spine, glowing photophores along
    # the lateral line: surface break-up that reads at play size.
    b.ellipsoid("BellyPlate", G(0, 0.085, 0.07), (0.09, 0.2, 0.035), bone="Spine_1", segments=(32, 12),
                style=H.band_plates(BELLY, k, 9, height=0.002, glow=0.0, axis=1))
    b.parts[-1].chain = ["Spine_1", "Spine_2"]
    for j, f in enumerate((0.27, 0.22, 0.15, 0.1, 0.03, -0.02, -0.09, -0.14, -0.21, -0.27, -0.34, -0.41)):
        bone = "Spine_2" if f > 0.08 else ("Spine_1" if f > -0.12 else "Tail_1")
        up = 0.19 if -0.12 < f < 0.24 else (0.175 if f >= 0.24 else 0.165 - 0.12 * (-0.12 - f))
        b.ellipsoid("Scute", G(0, up, f), (0.018, 0.024, 0.012), rot(pitch=-12), bone=bone, segments=(12, 6),
                    deform=C.chiselled(7, 70 + j, depth=0.2),
                    style=K2.armour_plate(PAL, k, rim_glow=0.3, chevron=0.0, lip=0.7, seed=70 + j))
    for side, sx in SIDES:
        for j, f in enumerate(PHOTOPHORES):
            bone = "Spine_2" if f > 0.08 else ("Spine_1" if f > -0.12 else "Tail_1")
            p, n = lateral_line(f)
            b.ellipsoid("Photophore", K3.gp((p.x + 0.003, p.z, -p.y), sx), (0.009, 0.009, 0.007), bone=bone, segments=(10, 6),
                        style=H.eye(PAL, k, 0.9))
    b.tube("Tail", TAIL[:1] + [G(0, 0.1, -0.22)] + TAIL[1:], [0.08, 0.065, 0.045, 0.025, 0.004], sides=12,
           chain=TAIL_BONES, style=K3.wet_skin(PAL, k, spots=70, seed=2, glow_spots=0.25, gloss=0.5))
    for name, c0, r0, seed in (("TailFin", G(0, 0.1, -0.52), (0.009, 0.19, 0.06), 3),
                               ("TailFin", G(0, 0.055, -0.5), (0.008, 0.15, 0.035), 4)):
        b.ellipsoid(name, c0, r0, bone="Tail_2", deform=C.chiselled(12, 80 + seed, depth=0.12),
                    style=K3.fin_sail(FIN, k, rays=7, edge_glow=0.9, axis=1, height_axis=2, seed=seed), segments=(32, 12))
        b.parts[-1].chain = ["Tail_2", "Tail_3"]
    # Head: wide and flat, a dark lip line with needle teeth and a glowing mouth seam, eyes, gills, the lure.
    b.ellipsoid("Head", G(0, 0.125, 0.38), (0.12, 0.13, 0.055), bone="Head", segments=(36, 16),
                style=K3.wet_skin(PAL, k, spots=80, seed=4, glow_spots=0.4, gloss=0.5))
    b.ellipsoid("Jaw", G(0, 0.09, 0.38), (0.108, 0.12, 0.03), bone="Head", segments=(32, 12),
                style=K3.wet_skin(PAL, k, spots=80, seed=5, belly_light=0.8, gloss=0.5))
    # A bony brow ridge over each eye pair and a back-swept cheek spine at each jaw corner.
    for side, sx in SIDES:
        b.ellipsoid("Brow", K3.gp((0.07, 0.172, 0.385), sx), (0.04, 0.06, 0.014), rot(yaw=-18 * sx, roll=18 * sx),
                    bone="Head", segments=(16, 8), deform=C.chiselled(8, 91, depth=0.15),
                    style=K2.armour_plate(PAL, k, rim_glow=0.2, chevron=0.0, lip=0.6, seed=91))
        cheek = K3.gp((0.115, 0.1, 0.33), sx)
        b.tube("CheekSpine", [cheek, cheek + K3.gp((0.04, 0.01, -0.04), sx), cheek + K3.gp((0.07, 0.02, -0.1), sx)],
               [0.013, 0.008, 0.001], sides=6, chain=["Head"], style=H.hook(PAL, k, glow=0.8, glow_from=0.7))
        # External gill fronds: a stalk with short feathery filaments, coral-lit towards the tip.
        for j, (root, d, ln) in enumerate(FRONDS):
            r0 = K3.gp(root, sx)
            dd = K3.gp(d, sx).normalized()
            pts = [r0, r0 + dd * ln * 0.45 + G(0, 0.015, 0), r0 + dd * ln + G(0, -0.01, -0.01)]
            b.tube("Frond", pts, [0.012, 0.008, 0.002], sides=8, chain=["Head"],
                   style=H.tube(FIN, k, FIN.side, FIN.glow * 0.8, tip_glow=1.0, glow_from=0.4))
            for m in range(4):
                t = 0.35 + 0.15 * m
                p0 = r0.lerp(pts[-1], t)
                for side_off in (1, -1):
                    fd = (dd.cross(Vector((0, 0, 1))).normalized() * side_off * 0.8 + dd * 0.6).normalized()
                    b.tube("Frond", [p0, p0 + fd * 0.022, p0 + fd * 0.034 + Vector((0, 0, -0.004))],
                           [0.004, 0.003, 0.0008], sides=5, chain=["Head"],
                           style=H.tube(FIN, k, FIN.side, FIN.glow, tip_glow=1.0, glow_from=0.2))
    b.ellipsoid("MouthSeam", G(0, 0.104, 0.46), (0.085, 0.03, 0.009), bone="Head", style=H.inner_glow(PAL, 0.9), segments=(20, 6))
    for side, sx in SIDES:
        for j in range(3):
            base = K3.gp((0.02 + 0.03 * j, 0.104, 0.49 - 0.012 * j * j), sx)
            b.tube("Tooth", [base, base + G(0, -0.012, 0.004), base + G(0, -0.024, 0.0)], [0.006, 0.004, 0.0006],
                   sides=6, chain=["Head"], style=H.hook(PAL, k))
        for p, r in (((0.055, 0.172, 0.4), 0.017), ((0.085, 0.16, 0.35), 0.012)):
            b.ellipsoid("Eye", K3.gp(p, sx), (r, r, r * 0.8), bone="Head", style=H.eye(PAL, k, 1.0), segments=(12, 6))
        for j in range(3):
            c = K3.gp((0.105 - 0.005 * j, 0.125, 0.27 - 0.03 * j), sx)
            b.ellipsoid("Gill", c, (0.012, 0.005, 0.03), bone="Spine_2", style=H.inner_glow(PAL, 0.9), segments=(10, 6))
    lure = [G(0, 0.17, 0.33), G(0, 0.24, 0.38), G(0, 0.255, 0.46), G(0, 0.235, 0.52)]
    b.tube("LureStalk", lure, [0.014, 0.011, 0.008, 0.007], sides=8, chain=["Lure"],
           style=H.tube(PAL, k, PAL.side, PAL.top, rings=(0.3, 0.6), ring_dark=0.5))
    b.ellipsoid("LureBulb", lure[-1] + G(0, -0.012, 0.012), (0.017, 0.017, 0.022), bone="Lure", style=H.eye(PAL, k, 1.0),
                segments=(14, 8))
    # A hood of two thin membranes cupped round the bulb (it reads as an organ, not a ball).
    for sx in (-1, 1):
        b.ellipsoid("LureHood", lure[-1] + G(0.012 * sx, -0.002, 0.004), (0.004, 0.026, 0.024), rot(yaw=-25 * sx),
                    bone="Lure", segments=(16, 8), style=K3.fin_sail(FIN, k, rays=4, edge_glow=1.0, axis=1, height_axis=2))
    for i, (f, h, bone) in enumerate(SAILS):
        c = G(0, 0.18 + h * 0.55, f)
        b.ellipsoid("Sail", c, (0.008, 0.065, h), rot(pitch=-14), bone=bone, segments=(28, 12),
                    deform=C.chiselled(11, 110 + i, depth=0.1),
                    style=K3.fin_sail(FIN, k, rays=5, edge_glow=1.0, axis=1, height_axis=2, seed=10 + i))
        for j, (df, lean) in enumerate(((0.025, 0.0), (-0.03, 0.03))):
            b.tube("SailSpine", [G(0, 0.16, f + df), G(0, 0.18 + h * (1.25 - 0.2 * j), f + df - 0.04 - lean)],
                   [0.009, 0.0015], sides=6, chain=[bone], style=H.hook(PAL, k, glow=0.8, glow_from=0.7))
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5), knee], [0.034, 0.028, 0.022], sides=10, chain=[lg.names[0]],
               style=K3.wet_skin(PAL, k, spots=90, seed=20 + i))
        b.tube("LegLower", [knee, knee.lerp(foot, 0.5), foot + Vector((0, 0, 0.012))], [0.022, 0.019, 0.016], sides=10,
               chain=[lg.names[1]], style=K3.wet_skin(PAL, k, spots=90, seed=22 + i))
        pad = foot + K3.gp((0.015, 0.01, 0.025), sx)
        b.ellipsoid("Paddle", pad, (0.045, 0.05, 0.01), bone=lg.names[1], segments=(20, 8),
                    style=K3.fin_sail(FIN, k, rays=3, edge_glow=0.6, axis=1, height_axis=0, seed=24 + i))
        for j, a in enumerate((-0.6, 0.0, 0.6)):
            d = K3.gp((math.sin(a) + 0.3, 0.0, math.cos(a)), sx).normalized()
            b.tube("Toe", [pad, pad + d * 0.045, pad + d * 0.065 + Vector((0, 0, -0.006))], [0.008, 0.006, 0.001], sides=6,
                   chain=[lg.names[1]], style=H.hook(PAL, k, glow=0.5))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True)]
    parent = "Root"
    for i, name in enumerate(SPINE_BONES):
        out.append((name, SPINE[i], SPINE[i + 1], parent, True))
        parent = name
    parent = "Spine_1"
    for i, name in enumerate(TAIL_BONES):
        out.append((name, TAIL[i], TAIL[i + 1], parent, True))
        parent = name
    out.append(("Lure", G(0, 0.17, 0.33), G(0, 0.235, 0.52), "Head", True))
    for (side, i), lg in legs().items():
        out += lg.rest_bones(LEG_PARENT[i])
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, wave=(0.0, 0.0), pitch_=0.0, roll=0.0, head=0.0, lure=(0.0, 0.0), tail_lift=0.0):
    """wave: (amplitude, phase) of the side-to-side wave from the head to the tail tip."""
    amp, ph = wave
    specs = {"Root": {"rot": []},
             "Spine_1": {"rot": [(Z, amp * 0.6 * math.sin(ph)), (Yv, roll), (X, pitch_)],
                         "loc": offset if offset is not None else Vector()},
             "Spine_2": {"rot": [(Z, -amp * 0.9 * math.sin(ph + 0.6))]},
             "Head": {"rot": [(Z, amp * 0.5 * math.sin(ph + 1.2)), (X, head)]}}
    for i, name in enumerate(TAIL_BONES):
        specs[name] = {"rot": [(Z, amp * (1.0 + 0.5 * i) * math.sin(ph - 1.0 - 0.9 * i)), (X, tail_lift * (i == 0))]}
    specs["Lure"] = {"rot": [(Z, lure[0]), (X, lure[1])]}
    return specs


def place(poser, specs, lg, feet):
    pose = poser.solve(specs)
    for (side, i), l in lg.items():
        p = LEG_PARENT[i]
        move = pose[p] @ poser.rest[p].inverted()
        foot = feet[(side, i)] if feet is not None else move @ l.foot
        specs.update(l.pose(poser, move @ l.root, foot, move.to_3x3() @ l.pole))
    return specs


def tucked(poser, specs, lg, amount):
    """Swimming: the paddles fold back along the flanks (riding with the body)."""
    pose = poser.solve(specs)
    for (side, i), l in lg.items():
        p = LEG_PARENT[i]
        move = pose[p] @ poser.rest[p].inverted()
        sx = -1 if side == "L" else 1
        back = l.root + K3.gp((0.06, 0.02, -0.2), sx)
        foot = move @ l.foot.lerp(back, amount)
        specs.update(l.pose(poser, move @ l.root, foot, move.to_3x3() @ l.pole))
    return specs


WALK_FRAMES = 28
STRIDE, LIFT, STANCE = 0.12, 0.035, 0.6
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.004 * math.cos(2 * w))), wave=(math.radians(11), w),
                           roll=math.radians(2) * math.sin(w), head=math.radians(2) * math.sin(2 * w),
                           lure=(math.radians(10) * math.sin(w + 1.5), math.radians(6) * math.sin(2 * w)))
        return place(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


SWIM_FRAMES = 24
SWIM_DISTANCE = 0.5


def swim_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, -0.03 + 0.006 * math.sin(2 * w))), wave=(math.radians(22), w),
                           roll=math.radians(4) * math.sin(w), head=math.radians(-4),
                           lure=(math.radians(14) * math.sin(w + 1.0), math.radians(10)))
        return tucked(poser, specs, lg, 0.85)
    return SWIM_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.004 * math.sin(w))), wave=(math.radians(5), w),
                           head=math.radians(5) * math.sin(w) - math.radians(3),
                           lure=(math.radians(18) * math.sin(w), math.radians(12) * math.sin(2 * w + 0.5)))
        return place(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), wave=(math.radians(14) * k, 0.8),
                           pitch_=math.radians(-6) * k, head=math.radians(-10) * k, lure=(0.0, math.radians(30) * k))
        return place(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        flip = K2.ease(0.1, 0.55, t)
        writhe = math.exp(-t * 4) * math.sin(t * 30)
        specs = body_specs(offset=Vector((0, 0, 0.06 * K2.pulse(t, 0.05, 0.25, 0.5) - 0.03 * flip)),
                           wave=(math.radians(25) * writhe + math.radians(12) * flip, 1.2 + t * 6 * (1 - flip)),
                           roll=math.radians(150) * flip, head=math.radians(10) * flip, lure=(math.radians(30) * flip, math.radians(40) * flip))
        return place(poser, specs, lg, None)
    return 40, spec, False


EMERGE_FRAMES = 48
EMERGE_SURFACE_FRAME = 10
EMERGE_DEPTH = 0.45


def emerge_spec(poser):
    lg = legs()

    def spec(t, f):
        rise = K2.ease(0.0, 0.5, t)
        level = K2.ease(0.4, 0.62, t)
        shake = math.exp(-max(0.0, t - 0.62) * 7) * math.sin(t * 60) * (t > 0.62)
        specs = body_specs(offset=Vector((0, 0.15 * (1 - rise), -EMERGE_DEPTH * (1 - rise) + 0.04 * K2.pulse(t, 0.35, 0.48, 0.62))),
                           pitch_=-math.radians(40) * (1 - level), roll=math.radians(14) * shake,
                           wave=(math.radians(18) * (1 - level) + math.radians(12) * abs(shake), t * 18),
                           head=math.radians(10) * (1 - level), tail_lift=math.radians(15) * (1 - level),
                           lure=(math.radians(25) * shake, math.radians(30) * (1 - level)))
        if level <= 0.0:
            return tucked(poser, specs, lg, 0.85)
        pose = poser.solve(specs)
        for (side, i), l in lg.items():
            p = LEG_PARENT[i]
            move = pose[p] @ poser.rest[p].inverted()
            foot = (move @ l.foot).lerp(l.foot, level)
            specs.update(l.pose(poser, move @ l.root, foot, move.to_3x3() @ l.pole))
        return specs
    return EMERGE_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Swim": swim_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Emerge": emerge_spec}
RIGID = {"Scute", "Brow"}
# LOD1: each leg one bone, the tail two bones, the lure on the head: 10 bones.
LOD_DROP = {"Lure": "Head", "Tail_3": "Tail_2", **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2)}}
LOD_AIMS = {"Tail_2": ("Tail_2", "Tail_3"),
            **{f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2)}}


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Swim", 8, (0.2, 0.9), -0.15), ("Emerge", 22, (1.2, 0.4), 0.3),
              ("Walk", 10, (-0.5, -0.4), 0.0), ("Swim", 16, (0.8, -0.6), -0.3)]
    previews = dict(target=(0, 0.0, 0.12), distance=2.0, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Swim", "top", range(0, SWIM_FRAMES, 3)),
                            ("Emerge", "side", range(0, EMERGE_FRAMES, 4)), ("Death", "close", range(0, 41, 6))],
                    macros=[("head", G(0.0, 0.14, 0.42), 0.4, -30, 20), ("sails", G(0.0, 0.25, 0.0), 0.45, -80, 15),
                            ("paddle", G(0.23, 0.02, 0.3), 0.3, -40, 30)],
                    rig_scale=1.0, power=("Emerge", 20))
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("membrane", "chitin", "crystal"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=0, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
                    extra=H.weathering(K, scratches=0.3, dust=0.1, pits=0.6, seed=41), specular=0.35,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "swim_distance_per_loop": SWIM_DISTANCE,
                            "pivot": "ground (dry) or water surface (swimming: Swim keeps the body 0.03 lower)",
                            "emerge": {"surface_frame": EMERGE_SURFACE_FRAME, "start_depth": EMERGE_DEPTH, "start_back": 0.15}},
                    previews=previews)
    # Round 46: the close-up detail (eyes, needle teeth, toe claws) as extra parts, exported again with the body.
    import surgefin_detail
    surgefin_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
