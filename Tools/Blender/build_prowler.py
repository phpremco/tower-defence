"""Prowler: the fast pack hunter on four legs (round 4, see ArtSource/DESIGN_NOTES.md).

A long, low body on a narrow waist; open armour ribs over the chest whose gaps glow orange (the pack's colour code);
a crest of tall backward-curving spines from the neck to the hips; a long eyeless head whose muzzle is two hooked
jaw halves that open sideways like shears around a glowing throat, with red sensor slits along both sides; legs on
long raised feet with two hooked toes, a reverse hock on the hind legs and a blade spur at each wrist; a segmented
whip tail ending in a forked blade.

Clips: Run (loop, low and flat rotary gallop), Idle, Hit, Death, Pounce. Also exports Prowler_LOD1.

Run: blender --background --python Tools/Blender/build_prowler.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "Prowler"
LOD_NAME = "Prowler_LOD1"
K = 1.0
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 0.62

# Species colours from ProjectGenerator.Bestiary: red-orange body (0.8, 0.28, 0.15), red sensors, orange accent.
PAL = H.Palette(top=(0.8, 0.27, 0.12), side=(0.2, 0.05, 0.03), belly=(0.06, 0.025, 0.02), seam=(0.04, 0.014, 0.01),
                dark=(0.13, 0.04, 0.025), glow=(1.0, 0.55, 0.17), eye=(1.0, 0.28, 0.14), membrane=(0.32, 0.07, 0.03),
                bone=(0.93, 0.8, 0.62))
LEGP = H.Palette(top=(0.6, 0.18, 0.08), side=(0.26, 0.065, 0.035), belly=(0.05, 0.02, 0.016), seam=(0.035, 0.012, 0.01),
                 dark=(0.1, 0.035, 0.022), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

# Legs: hip (x, up, forward), foot, bone lengths, knee pole, raised-foot direction (game axes).
FORE = dict(hip=(0.1, 0.35, 0.17), foot=(0.14, 0.0, 0.26), lengths=(0.16, 0.17, 0.09), pole=(0, 0.2, -1.0),
            foot_dir=(0, -0.75, 0.66), parent="Chest")
HIND = dict(hip=(0.1, 0.37, -0.27), foot=(0.14, 0.0, -0.31), lengths=(0.19, 0.2, 0.12), pole=(0, 0.2, 1.0),
            foot_dir=(0, -0.93, 0.36), parent="Pelvis")
LEG_KINDS = {"F": FORE, "H": HIND}


def legs():
    out = {}
    for side, sx in SIDES:
        for kind, d in LEG_KINDS.items():
            names = [f"Leg_{side}{kind}_{s}" for s in ("Upper", "Lower", "Foot")]
            out[(side, kind)] = C.Limb(names, G(d["hip"][0] * sx, d["hip"][1], d["hip"][2]),
                                       G(d["foot"][0] * sx, d["foot"][1], d["foot"][2]), d["lengths"], G(*d["pole"]),
                                       foot_dir=G(*d["foot_dir"]))
    return out


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def jaw_points(sx):
    return [G(0.04 * sx, 0.39, 0.53), G(0.078 * sx, 0.375, 0.65), G(0.055 * sx, 0.365, 0.76), G(0.012 * sx, 0.37, 0.83)]


SPINES = [  # forward position, height of the root, length, bone
    (0.36, 0.48, 0.09, "Neck"), (0.25, 0.53, 0.15, "Chest"), (0.15, 0.555, 0.2, "Chest"), (0.05, 0.55, 0.18, "Chest"),
    (-0.06, 0.49, 0.13, "Pelvis"), (-0.17, 0.5, 0.11, "Pelvis"), (-0.27, 0.49, 0.08, "Pelvis")]
TAIL = [G(0, 0.4, -0.36), G(0, 0.37, -0.55), G(0, 0.34, -0.74), G(0, 0.31, -0.93)]


def chest_point(angle, fwd, out=0.0):
    """A point on the chest surface at `angle` round it (0 the spine, pi/2 the flank) at `fwd`."""
    cy, cz, rx, ry, rz = 0.41, 0.12, 0.13, 0.15, 0.2
    q = max(0.0, 1 - ((fwd - cz) / rz) ** 2) ** 0.5
    return G(math.sin(angle) * (rx * q + out), cy + math.cos(angle) * (ry * q + out), fwd)


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Chest", G(0, 0.41, 0.12), (0.13, 0.2, 0.15), pitch(-4), bone="Chest",
                style=H.glow_flesh(PAL, k, glow=0.55, flank=(0.5, 0.95)), segments=(40, 20))
    b.ellipsoid("Waist", G(0, 0.39, -0.09), (0.085, 0.15, 0.1), bone="Pelvis", style=H.glow_flesh(PAL, k, glow=0.35, flank=(0.5, 0.95)),
                segments=(32, 16)).chain = ["Pelvis", "Chest"]
    b.ellipsoid("Haunch", G(0, 0.4, -0.26), (0.115, 0.13, 0.12), pitch(6), bone="Pelvis",
                style=H.band_plates(PAL, k, 2, height=0.004), segments=(36, 18))
    # A dark armoured saddle over the shoulders.
    b.ellipsoid("Saddle", G(0, 0.5, 0.15), (0.11, 0.17, 0.06), pitch(-6), bone="Chest",
                style=H.band_plates(PAL, k, 3, height=0.004), segments=(36, 18))
    # Open armour ribs around the chest: dark plates with lit gaps between them.
    for i, fwd in enumerate((0.26, 0.185, 0.11, 0.035, -0.04)):
        for sx in (-1, 1):
            pts = [chest_point(0.25 * sx, fwd + 0.02, 0.012), chest_point(0.95 * sx, fwd, 0.016),
                   chest_point(1.6 * sx, fwd - 0.03, 0.012), chest_point(2.1 * sx, fwd - 0.05, 0.0)]
            b.tube("Rib", pts, [0.024, 0.026, 0.02, 0.008], sides=10, chain=["Chest"],
                   style=H.tube(PAL, k, PAL.top * 0.95, PAL.side * 0.8, rough=0.64), flatten=0.6)
    b.tube("Neck", [G(0, 0.44, 0.24), G(0, 0.45, 0.36), G(0, 0.43, 0.46)], [0.085, 0.072, 0.062], sides=14,
           chain=["Neck"], style=H.tube(PAL, k, PAL.top * 0.9, PAL.top * 0.8, rings=(0.3, 0.65), rough=0.64))

    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.3 * front), v.y, v.z * (1 - 0.35 * front)))
    b.ellipsoid("Head", G(0, 0.43, 0.56), (0.075, 0.14, 0.055), pitch(4), bone="Head",
                style=H.wedge_head(PAL, k, slits=3, slit_width=0.06), segments=(36, 18), deform=head_deform)
    b.ellipsoid("Throat", G(0, 0.38, 0.66), (0.025, 0.05, 0.02), bone="Head", style=H.inner_glow(PAL, 0.9))
    for side, sx in SIDES:
        b.tube("Jaw", jaw_points(sx), [0.036, 0.034, 0.022, 0.002], sides=10, chain=[f"Jaw_{side}"],
               style=H.blade(PAL, k, glow=0.8, teeth=4), flatten=0.55)
        # Two brow horns sweeping back over the neck.
        root = G(0.035 * sx, 0.47, 0.5)
        b.tube("Horn", [root, root + G(0.02 * sx, 0.04, -0.06), root + G(0.04 * sx, 0.05, -0.15)], [0.016, 0.01, 0.001],
               sides=8, chain=["Head"], style=H.hook(PAL, k))
    for fwd, up, length, bone in SPINES:
        base = G(0, up - 0.01, fwd)
        b.tube("Spine", [base, base + G(0, length * 0.65, -length * 0.25), base + G(0, length * 0.8, -length * 0.75)],
               [0.018 + length * 0.07, 0.01, 0.0012], sides=10, chain=[bone], style=H.hook(PAL, k, glow=1.0, glow_from=0.75))
    tail_bones = ["Tail_1", "Tail_2", "Tail_3"]
    for i in range(3):
        a, c = TAIL[i], TAIL[i + 1]
        b.tube("Tail", [a, a.lerp(c, 0.5), c], [0.06 - i * 0.015, 0.05 - i * 0.014, 0.045 - i * 0.014], sides=12,
               chain=[tail_bones[i]], style=H.tube(PAL, k, PAL.top * 0.9, PAL.side, rings=(0.5,), rough=0.64))
    for sx in (-1, 1):
        tip = TAIL[-1]
        b.tube("TailBlade", [tip, tip + G(0.04 * sx, 0.0, -0.06), tip + G(0.075 * sx, -0.01, -0.15)], [0.03, 0.024, 0.002],
               sides=10, chain=["Tail_3"], style=H.blade(PAL, k, glow=0.9), flatten=0.35)

    for (side, kind), lg in legs().items():
        hip, knee, ankle, toe = lg.rest_points
        sx = -1 if side == "L" else 1
        thick = 1.2 if kind == "H" else 1.0
        b.tube("Upper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.01)), knee], [0.062 * thick, 0.052 * thick, 0.034], sides=12,
               chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=2))
        b.tube("Lower", [knee, knee.lerp(ankle, 0.5), ankle], [0.034, 0.03, 0.024], sides=10, chain=[lg.names[1]],
               style=H.leg(LEGP, k, "Femur"))
        # A ball at every joint so the limb reads as jointed, not as bent sticks.
        b.ellipsoid("Joint", knee, (0.04 * thick,) * 3, bone=lg.names[0], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(16, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.ellipsoid("Joint", ankle, (0.028,) * 3, bone=lg.names[1], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(16, 8))
        b.parts[-1].chain = [lg.names[1], lg.names[2]]
        b.tube("Foot", [ankle, ankle.lerp(toe, 0.5), toe], [0.022, 0.018, 0.016], sides=10, chain=[lg.names[2]],
               style=H.leg(LEGP, k, "Tibia"))
        for a in (-0.35, 0.35):
            fwd = G(math.sin(a) * 0.7, 0, 1).normalized()
            b.tube("Toe", [toe, toe + fwd * 0.04 + Vector((0, 0, 0.01)), toe + fwd * 0.07 - Vector((0, 0, 0.012))],
                   [0.014, 0.01, 0.001], sides=8, chain=[lg.names[2]], style=H.hook(PAL, k))
        if kind == "F":
            # A blade spur at the wrist, pointing back.
            b.tube("Spur", [ankle, ankle + G(0.012 * sx, 0.05, -0.04), ankle + G(0.02 * sx, 0.07, -0.1)],
                   [0.014, 0.008, 0.001], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k, glow=0.8))
        else:
            b.tube("Spur", [ankle, ankle + G(0, 0.0, -0.05), ankle + G(0, 0.02, -0.09)], [0.014, 0.008, 0.001], sides=8,
                   chain=[lg.names[2]], style=H.hook(PAL, k))
    return b


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Pelvis", G(0, 0.39, -0.3), G(0, 0.4, -0.04), "Root", True),
        ("Chest", G(0, 0.4, -0.04), G(0, 0.42, 0.25), "Pelvis", True),
        ("Neck", G(0, 0.43, 0.25), G(0, 0.43, 0.44), "Chest", True),
        ("Head", G(0, 0.43, 0.44), G(0, 0.4, 0.74), "Neck", True),
    ]
    for side, sx in SIDES:
        pts = jaw_points(sx)
        out.append((f"Jaw_{side}", pts[0], pts[-1], "Head", True))
    out += [("Tail_1", TAIL[0], TAIL[1], "Pelvis", True), ("Tail_2", TAIL[1], TAIL[2], "Tail_1", True),
            ("Tail_3", TAIL[2], TAIL[3], "Tail_2", True)]
    for (side, kind), lg in legs().items():
        out += lg.rest_bones(LEG_KINDS[kind]["parent"])
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, flex=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0), tail_lift=0.0):
    """flex: chest pitch against the pelvis (gather < 0 < stretch); neck/head pitch; jaw: how far the shears open;
    tail: (sway, wave phase)."""
    sway, phase = tail
    specs = {
        "Root": {"rot": []},
        "Pelvis": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
        "Chest": {"rot": [(X, flex), (Z, -yaw * 0.5)]},
        "Neck": {"rot": [(X, neck), (Z, head_yaw * 0.4)]},
        "Head": {"rot": [(X, head), (Z, head_yaw * 0.6)]},
    }
    for side, sx in SIDES:
        specs[f"Jaw_{side}"] = {"rot": [(Z, -sx * jaw), (X, -jaw * 0.2)]}
    for i in range(3):
        specs[f"Tail_{i + 1}"] = {"rot": [(Z, sway * math.sin(phase - i * 0.9) * (0.6 + 0.3 * i)), (X, tail_lift * (1 - 0.3 * i))]}
    return specs


def place_legs(poser, specs, feet, foot_dirs=None):
    pose = poser.solve(specs)
    moves = {name: pose[name] @ poser.rest[name].inverted() for name in ("Chest", "Pelvis")}
    for key, lg in legs().items():
        move = moves[LEG_KINDS[key[1]]["parent"]]
        fd = foot_dirs[key] if foot_dirs else lg.foot_dir
        pole = move.to_3x3() @ lg.pole
        specs.update(lg.pose(poser, move @ lg.root, feet[key], pole, fd))
    return specs


RUN_FRAMES = 16
STRIDE, LIFT, STANCE = 0.5, 0.07, 0.42
# Rotary gallop: hind left, hind right, then fore right, fore left (fraction of the cycle at touchdown).
TOUCHDOWN = {("L", "H"): 0.0, ("R", "H"): 0.1, ("R", "F"): 0.45, ("L", "F"): 0.55}


def swing_fold(phase):
    """0 planted .. 1 mid-swing: how much a foot folds up on its way forward."""
    if phase < STANCE:
        return 0.0
    return math.sin(math.pi * (phase - STANCE) / (1 - STANCE))


def run_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        # The spine gathers when the hind feet reach forward and stretches when the fore feet do; the head stays
        # level and low (the neck takes the body's pitch out), the body barely bobs.
        flex = math.radians(7) * math.sin(w + 0.4)
        specs = body_specs(offset=Vector((0, 0, -0.06 + 0.01 * math.cos(2 * w))), pitch_=math.radians(5) + math.radians(2.5) * math.sin(w),
                           roll=math.radians(1.5) * math.sin(w), flex=flex,
                           neck=-math.radians(10) - flex * 0.6, head=-math.radians(4) * math.sin(w) + math.radians(6),
                           jaw=math.radians(6) * (0.5 + 0.5 * math.sin(2 * w)), tail=(math.radians(4), w),
                           tail_lift=math.radians(6) + math.radians(4) * math.sin(w + 1.0))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t - TOUCHDOWN[key]) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            fold = swing_fold(phase)
            dirs[key] = Matrix.Rotation(math.radians(-55 if key[1] == "F" else 45) * fold, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return RUN_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(3 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, CROUCH + 0.006 * math.sin(2 * w))), pitch_=math.radians(3),
                           flex=math.radians(2) * math.sin(2 * w), neck=-math.radians(14) + math.radians(4) * math.sin(w),
                           head=math.radians(6) + math.radians(3) * math.sin(2 * w + 1),
                           head_yaw=math.radians(22) * math.sin(w) * math.sin(0.5 * w + 0.3),
                           jaw=math.radians(4) + math.radians(26) * snap, tail=(math.radians(10), w * 2), tail_lift=math.radians(4))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 60, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((0.03 * k, 0.03 * k, CROUCH + 0.01 * k)), roll=math.radians(8) * k,
                           yaw=math.radians(-8) * k, flex=-math.radians(6) * k, neck=-math.radians(5) - math.radians(14) * k,
                           head=math.radians(-10) * k, jaw=math.radians(30) * max(k, 0.0), tail=(math.radians(12) * k, 1.0),
                           tail_lift=math.radians(15) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 12, spec, False


POUNCE_FRAMES = 26
CROUCH = -0.045  # the hunter's standing crouch: every clip holds the body this much below the bind pose


def pounce_spec(poser):
    def spec(t, f):
        crouch = C.smoothstep(0.0, 0.3, np.array([t]))[0] * (1 - C.smoothstep(0.3, 0.42, np.array([t]))[0])
        leap = C.smoothstep(0.3, 0.55, np.array([t]))[0] * (1 - C.smoothstep(0.62, 0.9, np.array([t]))[0])
        land = C.smoothstep(0.75, 0.85, np.array([t]))[0] * (1 - C.smoothstep(0.85, 1.0, np.array([t]))[0])
        arc = math.sin(math.pi * C.smoothstep(0.3, 0.78, np.array([t]))[0])
        offset = Vector((0, -0.32 * leap, CROUCH - 0.08 * crouch + 0.14 * arc - 0.04 * land))
        specs = body_specs(offset=offset, pitch_=math.radians(8) * crouch - math.radians(10) * leap * (1 - arc * 0.3),
                           flex=-math.radians(8) * crouch + math.radians(8) * leap, neck=-math.radians(6) * crouch + math.radians(10) * leap,
                           head=math.radians(8) * crouch - math.radians(12) * leap, jaw=math.radians(45) * leap,
                           tail=(math.radians(5), t * 6), tail_lift=math.radians(18) * leap)
        feet, dirs = {}, {}
        for key, lg in legs().items():
            if key[1] == "F":
                # The fore feet tuck, then reach far forward and up with the claws out, and land at the end.
                reach = Vector((0, -0.42, 0.22)) * leap
                feet[key] = lg.foot + reach + Vector((0, 0, 0.05 * crouch))
                dirs[key] = Matrix.Rotation(math.radians(-35) * leap, 3, "X") @ lg.foot_dir
            else:
                # The hind feet push off, trail behind in the air and swing back in under the body.
                push = C.smoothstep(0.42, 0.6, np.array([t]))[0] * (1 - C.smoothstep(0.7, 0.92, np.array([t]))[0])
                feet[key] = lg.foot + Vector((0, 0.08 * push, 0.1 * push)) + Vector((0, -0.32 * leap * (1 - push * 0.3), 0))
                dirs[key] = Matrix.Rotation(math.radians(60) * push, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return POUNCE_FRAMES, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = C.smoothstep(0.1, 0.55, np.array([t]))[0]
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 50) * (1 if t > 0.6 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH + 0.03 * jerk)), neck=math.radians(25) * fall, head=math.radians(15) * fall,
                           jaw=math.radians(35) * fall + math.radians(8) * twitch, tail=(math.radians(14) * fall, 0.5),
                           tail_lift=-math.radians(14) * fall)
        # Falls onto its side: the root rolls over the right feet and settles.
        pivot = Vector((0.15, 0, 0))
        angle = math.radians(84) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -0.17 * fall))}
        pose = poser.solve(specs)
        for key, lg in legs().items():
            move = pose[LEG_KINDS[key[1]]["parent"]] @ poser.rest[LEG_KINDS[key[1]]["parent"]].inverted()
            hip = move @ lg.root
            # Limp legs: they fall in a loose curl in the body's frame, with a last kick.
            rest_vec = lg.foot - lg.root
            curl = Vector((0, 0.08 if key[1] == "H" else -0.08, 0.1)) + rest_vec * 0.6
            kick = Vector((0, 0.03 * twitch, 0))
            foot = hip + move.to_3x3() @ (rest_vec.lerp(curl, fall) + kick)
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 36, spec, False


CLIPS = {"Run": run_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Pounce": pounce_spec}
# LOD1: jaws fold into the head, the tail is one bone, each leg two (the lower bone aimed from knee to toe). 14 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", "Tail_2": "Tail_1", "Tail_3": "Tail_1",
            **{f"Leg_{s}{k}_Foot": f"Leg_{s}{k}_Lower" for s, _ in SIDES for k in LEG_KINDS}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3"),
            **{f"Leg_{s}{k}_Lower": (f"Leg_{s}{k}_Lower", f"Leg_{s}{k}_Foot") for s, _ in SIDES for k in LEG_KINDS}}
RIGID = ()
QUICK_TARGET = (0, 0, 0.3)


def old_proxy():
    """EnemyModelFactory.Prowler in primitives (wolf with a mane and flank stripes), flat colours."""
    body, dark, glow, eyes = (0.8, 0.28, 0.15), (0.25, 0.08, 0.05), (1.0, 0.6, 0.2), (1.0, 0.35, 0.2)
    groups = {c: C.PartBuilder() for c in (body, dark, glow, eyes)}
    g = groups[body]
    g.ellipsoid("Chest", G(0, 0.4, 0.12), (0.165, 0.21, 0.17))
    g.ellipsoid("Waist", G(0, 0.37, -0.1), (0.12, 0.2, 0.13))
    g.ellipsoid("Haunch", G(0, 0.39, -0.28), (0.15, 0.15, 0.15))
    groups[dark].ellipsoid("Saddle", G(0, 0.47, 0.08), (0.15, 0.3, 0.1))
    g.tube("Neck", [G(0, 0.47, 0.24), G(0, 0.5, 0.36), G(0, 0.49, 0.44)], [0.11, 0.095, 0.085])
    g.ellipsoid("Head", G(0, 0.49, 0.47), (0.1, 0.12, 0.095))
    g.tube("Muzzle", [G(0, 0.485, 0.52), G(0, 0.475, 0.63), G(0, 0.465, 0.72)], [0.072, 0.058, 0.042])
    for sx in (-1, 1):
        groups[eyes].ellipsoid("Eye", G(0.055 * sx, 0.515, 0.54), (0.03, 0.022, 0.016))
        groups[glow].tube("Stripe", [G(0.15 * sx, 0.42, 0.2), G(0.125 * sx, 0.4, -0.05), G(0.14 * sx, 0.41, -0.28)], [0.012, 0.014, 0.008])
        groups[dark].tube("Ear", [G(0.06 * sx, 0.56, 0.45), G(0.08 * sx, 0.68, 0.4)], [0.035, 0.003])
        for z in (0.15, -0.27):
            groups[body].tube("Leg", [G(0.1 * sx, 0.36, z), G(0.1 * sx, 0.18, z + 0.03), G(0.1 * sx, 0.0, z)], [0.04, 0.03, 0.025])
    for i in range(8):
        tt = i / 7
        groups[dark].tube("Mane", [G(0, 0.55, 0.42 - 0.42 * tt), G(0, 0.68, 0.32 - 0.42 * tt)], [0.04, 0.003])
    g.tube("Tail", [G(0, 0.4, -0.38), G(0, 0.36, -0.6), G(0, 0.3, -0.8)], [0.06, 0.05, 0.02])
    return C.flat_proxy(groups, "OldProwler")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    target = Vector((0, -0.08, 0.3))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 2.4, azimuth=-38, elevation=24, lens=55)
    C.set_action(arm, actions["Idle"], 20)
    paths.append(C.render(close, os.path.join(out, "prowler_close.png"), 1280, 720))
    side = C.closeup_camera(target, 2.6, azimuth=-90, elevation=6, lens=55)
    C.set_action(arm, actions["Run"], 0)
    paths.append(C.render(side, os.path.join(out, "prowler_side.png"), 1280, 720))
    front = C.closeup_camera(target + Vector((0, -0.3, 0.05)), 1.5, azimuth=-20, elevation=10, lens=55)
    C.set_action(arm, actions["Pounce"], 13)
    paths.append(C.render(front, os.path.join(out, "prowler_pounce.png"), 1280, 720))
    top = C.closeup_camera(target, 2.6, azimuth=-160, elevation=58, lens=55)
    C.set_action(arm, actions["Run"], 6)
    paths.append(C.render(top, os.path.join(out, "prowler_top.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 36)
    paths.append(C.render(close, os.path.join(out, "prowler_dead.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Run"], side, out, "prowler_run", range(0, RUN_FRAMES, 2)),
              C.strip_sheet(arm, actions["Pounce"], side, out, "prowler_pounce", range(0, POUNCE_FRAMES, 3)[:8])]

    C.set_action(lod_arm, lod_actions["Run"], 6)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "prowler_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 20)
    macros = [("prowler_macro_ribs.png", G(-0.14, 0.42, 0.12), 0.45, -80, 15),
              ("prowler_macro_head.png", G(0.05, 0.42, 0.62), 0.4, -35, 18),
              ("prowler_macro_leg.png", G(-0.13, 0.15, -0.3), 0.4, -60, 10)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    run = actions["Run"]
    layout = [(run, 0, (-1.0, 0.55, 0), 0.15), (run, 5, (-0.2, 0.75, 0), 0.1), (run, 11, (0.7, 0.45, 0), 0.2),
              (run, 3, (-0.6, -0.3, 0), -0.1), (run, 8, (0.3, -0.5, 0), 0.0), (run, 14, (1.2, -0.25, 0), 0.25)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "prowler_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "prowler_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "prowler_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "prowler_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Idle"], 20)
    single = C.game_view(arm, mesh, os.path.join(out, "prowler_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare4_prowler.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_prowler.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "prowler_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0032, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.08,
                               extrusion=0.012, ray=0.04, emission_strength=2.5,
                               extra=H.weathering(K, markings=PAL.dark, marking_amount=0.7), specular=0.3,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "run_distance_per_loop": round(STRIDE / STANCE, 3),
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)
    # Round 46: the close-up detail (eyes, jaw fangs, toe talons) as extra parts, exported again with the body.
    import prowler_detail
    prowler_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
