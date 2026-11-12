"""PackAlpha: the Prowler boss, the pack leader whose howl sends the whole pack running (round 6, see
ArtSource/DESIGN_NOTES.md).

The Prowler grown old and front-heavy: massive armoured shoulders and a deep rib cage over narrow hips, the head held
low; longer, broader muzzle shears with teeth and a short inner pair of hooks around a hot throat; a collar of long
glowing spines in two fans on their own bones (it flares on the howl) and a double row of spines down the back;
healed claw scars glowing dull orange across the ribs and shoulders; a pale bone chevron on the saddle that points
forward; heavy legs with hooked toes and a forked whip tail.

Clips: Run (loop), Idle (loop), Hit, Death, Howl (the power). Also exports PackAlpha_LOD1.

Run: blender --background --python Tools/Blender/build_packalpha.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "PackAlpha"
LOD_NAME = "PackAlpha_LOD1"
# Built in Prowler units times M: at Unity scale 1.3 x 0.6 it stands about 2.1 times a Prowler (as the factory boss).
M = 1.65
K = M
DETAIL_K = 1.4
GAME_TRIS = 14500
LOD_TRIS = 6000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 1.3 * 0.6
PROWLER_SCALE = 0.62

# The PackAlpha's data colours: a deeper blood red than the Prowler (0.6, 0.15, 0.1), red-orange sensors, orange glow.
PAL = H.Palette(top=(0.62, 0.13, 0.07), side=(0.17, 0.035, 0.025), belly=(0.05, 0.02, 0.016), seam=(0.035, 0.012, 0.01),
                dark=(0.1, 0.028, 0.02), glow=(1.0, 0.52, 0.14), eye=(1.0, 0.3, 0.12), membrane=(0.3, 0.06, 0.025),
                bone=(0.92, 0.8, 0.62))
LEGP = H.Palette(top=(0.48, 0.1, 0.055), side=(0.2, 0.045, 0.03), belly=(0.045, 0.018, 0.014), seam=(0.03, 0.01, 0.008),
                 dark=(0.08, 0.025, 0.018), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def P(x, up, fwd):
    """A point in Prowler units, scaled to the boss."""
    return G(x * M, up * M, fwd * M)


def R(*r):
    return tuple(v * M for v in r) if len(r) > 1 else r[0] * M


FORE = dict(hip=(0.12, 0.37, 0.19), foot=(0.17, 0.0, 0.3), lengths=(0.17, 0.18, 0.09), pole=(0, 0.2, -1.0),
            foot_dir=(0, -0.75, 0.66), parent="Chest")
HIND = dict(hip=(0.1, 0.38, -0.27), foot=(0.14, 0.0, -0.31), lengths=(0.19, 0.2, 0.12), pole=(0, 0.2, 1.0),
            foot_dir=(0, -0.93, 0.36), parent="Pelvis")
LEG_KINDS = {"F": FORE, "H": HIND}


def legs():
    out = {}
    for side, sx in SIDES:
        for kind, d in LEG_KINDS.items():
            names = [f"Leg_{side}{kind}_{s}" for s in ("Upper", "Lower", "Foot")]
            out[(side, kind)] = C.Limb(names, P(d["hip"][0] * sx, d["hip"][1], d["hip"][2]),
                                       P(d["foot"][0] * sx, d["foot"][1], d["foot"][2]), R(*d["lengths"]), G(*d["pole"]),
                                       foot_dir=G(*d["foot_dir"]))
    return out


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def jaw_points(sx):
    return [P(0.05 * sx, 0.375, 0.55), P(0.1 * sx, 0.36, 0.69), P(0.072 * sx, 0.35, 0.82), P(0.014 * sx, 0.355, 0.9)]


# Back spines in pairs (forward position, root height, length, bone), longest over the shoulders.
SPINES = [(0.24, 0.6, 0.16, "Chest"), (0.15, 0.62, 0.2, "Chest"), (0.06, 0.6, 0.17, "Chest"),
          (-0.06, 0.52, 0.13, "Pelvis"), (-0.17, 0.52, 0.11, "Pelvis"), (-0.27, 0.5, 0.08, "Pelvis")]
TAIL = [P(0, 0.4, -0.36), P(0, 0.37, -0.56), P(0, 0.34, -0.76), P(0, 0.31, -0.96)]
# The mane collar: per side five spines fanning out and back from behind the head (angle round the neck, length).
MANE = [(0.25, 0.34), (0.55, 0.4), (0.85, 0.38), (1.15, 0.32), (1.45, 0.24)]
MANE_ROOT = (0.05, 0.53, 0.31)


def chest_point(angle, fwd, out=0.0):
    """A point on the chest surface at `angle` round it (0 the spine, pi/2 the flank) at `fwd` (Prowler units)."""
    cy, cz, rx, ry, rz = 0.44, 0.14, 0.16, 0.18, 0.23
    q = max(0.0, 1 - ((fwd - cz) / rz) ** 2) ** 0.5
    return P(math.sin(angle) * (rx * q + out), cy + math.cos(angle) * (ry * q + out), fwd)


def mane_spine(sx, angle, length):
    """Root and two points of one mane spine: out round the neck, sweeping back and up."""
    root = P(MANE_ROOT[0] * sx + math.sin(angle) * 0.06 * sx, MANE_ROOT[1] + math.cos(angle) * 0.05 - 0.04, MANE_ROOT[2])
    out = G(math.sin(angle) * sx, math.cos(angle) * 0.9 + 0.25, 0).normalized()
    back = G(0, 0, -1)
    mid = root + (out * 0.75 + back * 0.3).normalized() * R(length * 0.55)
    tip = root + (out * 0.75 + back * 0.6 + G(0, 0.15, 0)).normalized() * R(length)
    return root, mid, tip


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Chest", P(0, 0.44, 0.14), R(0.16, 0.23, 0.18), pitch(-5), bone="Chest",
                style=H.glow_flesh(PAL, k, glow=1.0, flank=(0.4, 0.9)), segments=(48, 24))
    b.ellipsoid("Waist", P(0, 0.4, -0.09), R(0.08, 0.15, 0.1), bone="Pelvis",
                style=H.glow_flesh(PAL, k, glow=0.45, flank=(0.5, 0.95)), segments=(36, 18)).chain = ["Pelvis", "Chest"]
    b.ellipsoid("Haunch", P(0, 0.41, -0.26), R(0.11, 0.13, 0.12), pitch(6), bone="Pelvis",
                style=H.band_plates(PAL, k, 2, height=0.004), segments=(40, 20))
    b.ellipsoid("Saddle", P(0, 0.56, 0.16), R(0.13, 0.2, 0.065), pitch(-6), bone="Chest",
                style=H.band_plates(PAL, k, 3, height=0.004, glow=0.3), segments=(40, 20))
    for sx in (-1, 1):
        # Armoured shoulders: the alpha's mass is all in front.
        b.ellipsoid("Shoulder", P(0.135 * sx, 0.47, 0.18), R(0.085, 0.12, 0.11), pitch(-10), bone="Chest",
                    style=H.band_plates(PAL, k, 3, height=0.005, glow=0.3, shingle=True), segments=(32, 16))
        # The pale forward chevron on the saddle (seen from above).
        b.tube("Chevron", [P(0.085 * sx, 0.6, 0.02), P(0.05 * sx, 0.615, 0.14), P(0.008 * sx, 0.62, 0.27)],
               [R(0.026), R(0.022), R(0.012)], sides=8, chain=["Chest"],
               style=H.tube(PAL, k, PAL.bone, PAL.bone, tip_glow=1.0, glow_from=0.25, rough=0.5), flatten=0.5)
    for i, fwd in enumerate((0.3, 0.225, 0.15, 0.075, 0.0, -0.07)):
        for sx in (-1, 1):
            pts = [chest_point(0.28 * sx, fwd + 0.02, 0.014), chest_point(0.95 * sx, fwd, 0.018),
                   chest_point(1.6 * sx, fwd - 0.03, 0.014), chest_point(2.1 * sx, fwd - 0.05, 0.0)]
            b.tube("Rib", pts, [R(0.026), R(0.028), R(0.022), R(0.009)], sides=10, chain=["Chest"],
                   style=H.tube(PAL, k, PAL.top * 0.95, PAL.side * 0.8, rough=0.64), flatten=0.6)
    b.tube("Neck", [P(0, 0.47, 0.27), P(0, 0.46, 0.39), P(0, 0.43, 0.48)], [R(0.1), R(0.085), R(0.072)], sides=16,
           chain=["Neck"], style=H.tube(PAL, k, PAL.top * 0.9, PAL.top * 0.8, rings=(0.3, 0.65), rough=0.64))

    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.28 * front), v.y, v.z * (1 - 0.35 * front)))
    b.ellipsoid("Head", P(0, 0.42, 0.58), R(0.092, 0.16, 0.064), pitch(5), bone="Head",
                style=H.wedge_head(PAL, k, slits=4, slit_width=0.055), segments=(40, 20), deform=head_deform)
    b.ellipsoid("Throat", P(0, 0.37, 0.69), R(0.032, 0.06, 0.024), bone="Head", style=H.inner_glow(PAL, 1.0))
    for side, sx in SIDES:
        jp = jaw_points(sx)
        b.tube("Jaw", jp, [R(0.046), R(0.043), R(0.028), R(0.002)], sides=12, chain=[f"Jaw_{side}"],
               style=H.blade(PAL, k, glow=0.9, teeth=6), flatten=0.55)
        # A short inner pair of hooks on each shear.
        base = jp[1] + P(-0.03 * sx, -0.004, -0.01)
        b.tube("InnerHook", [base, base + P(-0.03 * sx, -0.004, 0.03), base + P(-0.045 * sx, -0.01, 0.065)],
               [R(0.014), R(0.009), R(0.001)], sides=8, chain=[f"Jaw_{side}"], style=H.hook(PAL, k, glow=0.8))
        # Brow horns, notched like old battle damage.
        root = P(0.042 * sx, 0.465, 0.5)
        b.tube("Horn", [root, root + P(0.025 * sx, 0.05, -0.07), root + P(0.05 * sx, 0.065, -0.17)],
               [R(0.02), R(0.013), R(0.001)], sides=8, chain=["Head"], style=H.blade(PAL, k, glow=0.3, teeth=3), flatten=0.8)
        for angle, length in MANE:
            root, mid, tip = mane_spine(sx, angle, length)
            b.tube("Mane", [root, mid, tip], [R(0.02 + length * 0.05), R(0.012), R(0.0012)], sides=10,
                   chain=[f"Mane_{side}"], style=H.hook(PAL, k, glow=1.0, glow_from=0.62))
    for fwd, up, length, bone in SPINES:
        for sx in (-1, 1):
            base = P(0.032 * sx, up - 0.012, fwd)
            b.tube("Spine", [base, base + P(0.012 * sx, length * 0.65, -length * 0.25),
                             base + P(0.02 * sx, length * 0.8, -length * 0.75)],
                   [R(0.016 + length * 0.07), R(0.009), R(0.0012)], sides=10, chain=[bone],
                   style=H.hook(PAL, k, glow=1.0, glow_from=0.7))
    tail_bones = ["Tail_1", "Tail_2", "Tail_3"]
    for i in range(3):
        a, c = TAIL[i], TAIL[i + 1]
        b.tube("Tail", [a, a.lerp(c, 0.5), c], [R(0.064 - i * 0.016), R(0.054 - i * 0.015), R(0.048 - i * 0.015)], sides=12,
               chain=[tail_bones[i]], style=H.tube(PAL, k, PAL.top * 0.9, PAL.side, rings=(0.5,), rough=0.64))
        for sx in (-1, 1):
            base = a.lerp(c, 0.5) + P(0.02 * sx, 0.035 - i * 0.008, 0)
            b.tube("TailHook", [base, base + P(0.02 * sx, 0.04, -0.025), base + P(0.03 * sx, 0.05, -0.07)],
                   [R(0.013), R(0.007), R(0.001)], sides=8, chain=[tail_bones[i]], style=H.hook(PAL, k, glow=0.7))
    for sx in (-1, 1):
        tip = TAIL[-1]
        b.tube("TailBlade", [tip, tip + P(0.045 * sx, 0.0, -0.07), tip + P(0.085 * sx, -0.01, -0.17)],
               [R(0.034), R(0.027), R(0.002)], sides=10, chain=["Tail_3"], style=H.blade(PAL, k, glow=1.0), flatten=0.35)

    for (side, kind), lg in legs().items():
        hip, knee, ankle, toe = lg.rest_points
        sx = -1 if side == "L" else 1
        thick = 1.35 if kind == "F" else 1.15
        b.tube("Upper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, R(0.01))), knee], [R(0.064 * thick), R(0.054 * thick), R(0.036)],
               sides=12, chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=2))
        b.tube("Lower", [knee, knee.lerp(ankle, 0.5), ankle], [R(0.037 * thick), R(0.032), R(0.026)], sides=10,
               chain=[lg.names[1]], style=H.leg(LEGP, k, "Femur"))
        b.ellipsoid("Joint", knee, (R(0.042 * thick),) * 3, bone=lg.names[0], style=H.band_plates(LEGP, k, 1, height=0.0),
                    segments=(16, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.ellipsoid("Joint", ankle, (R(0.03),) * 3, bone=lg.names[1], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(16, 8))
        b.parts[-1].chain = [lg.names[1], lg.names[2]]
        b.tube("Foot", [ankle, ankle.lerp(toe, 0.5), toe], [R(0.024), R(0.02), R(0.017)], sides=10, chain=[lg.names[2]],
               style=H.leg(LEGP, k, "Tibia"))
        for a in (-0.4, 0.0, 0.4):
            fwd = G(math.sin(a) * 0.7, 0, 1).normalized()
            b.tube("Toe", [toe, toe + fwd * R(0.045) + Vector((0, 0, R(0.01))), toe + fwd * R(0.08) - Vector((0, 0, R(0.012)))],
                   [R(0.015), R(0.011), R(0.001)], sides=8, chain=[lg.names[2]], style=H.hook(PAL, k))
        if kind == "F":
            b.tube("Spur", [ankle, ankle + P(0.014 * sx, 0.06, -0.045), ankle + P(0.022 * sx, 0.085, -0.12)],
                   [R(0.016), R(0.009), R(0.001)], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k, glow=0.9))
            # An elbow blade on the heavy forelegs.
            b.tube("ElbowSpur", [knee, knee + P(0.01 * sx, 0.02, -0.06), knee + P(0.02 * sx, 0.04, -0.13)],
                   [R(0.018), R(0.01), R(0.001)], sides=8, chain=[lg.names[0]], style=H.hook(PAL, k, glow=0.6))
        else:
            b.tube("Spur", [ankle, ankle + P(0, 0.0, -0.05), ankle + P(0, 0.02, -0.1)], [R(0.015), R(0.009), R(0.001)],
                   sides=8, chain=[lg.names[2]], style=H.hook(PAL, k))
    return b


def scar_cuts():
    """Three healed claw slashes across each flank's ribs and one across each shoulder (Blender space)."""
    cuts = []
    for sx in (-1, 1):
        out = Vector((sx, 0, 0.25))
        for i in range(3):
            o = i * 0.045
            cuts.append((P(0.175 * sx, 0.56 - o * 0.4, 0.27 - o), P(0.165 * sx, 0.33 - o * 0.4, 0.04 - o), out))
        cuts.append((P(0.17 * sx, 0.56, 0.27), P(0.2 * sx, 0.44, 0.12), Vector((sx, 0, 0.6))))
    return cuts


def bones():
    out = [
        ("Root", P(0, 0, 0), P(0, 0, 0.2), None, True),
        ("Pelvis", P(0, 0.39, -0.3), P(0, 0.41, -0.04), "Root", True),
        ("Chest", P(0, 0.41, -0.04), P(0, 0.44, 0.27), "Pelvis", True),
        ("Neck", P(0, 0.46, 0.27), P(0, 0.43, 0.46), "Chest", True),
        ("Head", P(0, 0.43, 0.46), P(0, 0.39, 0.8), "Neck", True),
    ]
    for side, sx in SIDES:
        pts = jaw_points(sx)
        out.append((f"Jaw_{side}", pts[0], pts[-1], "Head", True))
        root = P(MANE_ROOT[0] * sx, MANE_ROOT[1], MANE_ROOT[2])
        out.append((f"Mane_{side}", root, root + P(0.18 * sx, 0.08, -0.12), "Chest", True))
    out += [("Tail_1", TAIL[0], TAIL[1], "Pelvis", True), ("Tail_2", TAIL[1], TAIL[2], "Tail_1", True),
            ("Tail_3", TAIL[2], TAIL[3], "Tail_2", True)]
    for (side, kind), lg in legs().items():
        out += lg.rest_bones(LEG_KINDS[kind]["parent"])
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, flex=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0), tail_lift=0.0, mane=0.0, mane_lift=0.0):
    """As the Prowler's, plus mane: how far the collar fans flare out (rad), mane_lift: how far they rise."""
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
        specs[f"Mane_{side}"] = {"rot": [(Yv, -sx * mane), (X, mane_lift)]}
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


def s_(a, b, t):
    return C.smoothstep(a, b, np.array([t]))[0]


RUN_FRAMES = 20
STRIDE, LIFT, STANCE = 0.52 * M, 0.08 * M, 0.45
TOUCHDOWN = {("L", "H"): 0.0, ("R", "H"): 0.1, ("R", "F"): 0.45, ("L", "F"): 0.55}
CROUCH = -0.045 * M


def swing_fold(phase):
    if phase < STANCE:
        return 0.0
    return math.sin(math.pi * (phase - STANCE) / (1 - STANCE))


def run_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Heavier than the Prowler: more bounce and roll, the shoulders working, the collar bouncing on each stride.
        flex = d(6) * math.sin(w + 0.4)
        specs = body_specs(offset=Vector((0, 0, -R(0.055) + R(0.016) * math.cos(2 * w))), pitch_=d(5) + d(3) * math.sin(w),
                           roll=d(2.5) * math.sin(w), flex=flex, neck=-d(12) - flex * 0.6, head=-d(4) * math.sin(w) + d(8),
                           jaw=d(8) * (0.5 + 0.5 * math.sin(2 * w)), tail=(d(5), w), tail_lift=d(5) + d(4) * math.sin(w + 1.0),
                           mane=d(4) * math.sin(2 * w + 0.6), mane_lift=d(3) * math.sin(2 * w + 0.6))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t - TOUCHDOWN[key]) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            dirs[key] = Matrix.Rotation(d(-55 if key[1] == "F" else 45) * swing_fold(phase), 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return RUN_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(3 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, CROUCH + R(0.006) * math.sin(2 * w))), pitch_=d(3),
                           flex=d(2) * math.sin(2 * w), neck=-d(16) + d(4) * math.sin(w),
                           head=d(6) + d(3) * math.sin(2 * w + 1), head_yaw=d(20) * math.sin(w) * math.sin(0.5 * w + 0.3),
                           jaw=d(5) + d(24) * snap, tail=(d(9), w * 2), tail_lift=d(4),
                           mane=d(6) * snap + d(2) * math.sin(w), mane_lift=d(4) * snap)
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((R(0.025) * k, R(0.025) * k, CROUCH + R(0.01) * k)), roll=d(6) * k, yaw=-d(6) * k,
                           flex=-d(5) * k, neck=-d(8) - d(12) * k, head=-d(10) * k, jaw=d(32) * max(k, 0.0),
                           tail=(d(10) * k, 1.0), tail_lift=d(12) * k, mane=d(14) * max(k, 0.0), mane_lift=d(8) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


HOWL_FRAMES = 54
# The howl's peak: the rally VFX and sound start here (frames 20 to 42 hold the howl).
HOWL_PEAK_FRAME = 22


def howl_spec(poser):
    d = math.radians

    def spec(t, f):
        rise = s_(0.05, 0.38, t) * (1 - s_(0.78, 1.0, t))
        gather = math.sin(math.pi * s_(0.0, 0.2, t)) * (1 - s_(0.15, 0.3, t))
        shake = math.sin(t * 2 * math.pi * 11) * s_(0.38, 0.45, t) * (1 - s_(0.72, 0.8, t))
        # It sits back on its haunches, the chest rises, the head is thrown up with the shears wide open and the
        # collar flares like a frill; the tail lifts and shakes.
        specs = body_specs(offset=Vector((0, R(0.05) * rise, CROUCH - R(0.04) * gather + R(0.03) * rise)),
                           pitch_=-d(9) * rise + d(4) * gather, flex=-d(6) * rise, neck=-d(30) * rise + d(10) * gather,
                           head=-d(16) * rise + d(2) * shake, head_yaw=d(3) * shake, jaw=d(52) * rise + d(4) * shake,
                           tail=(d(6) * rise, t * 30), tail_lift=d(30) * rise,
                           mane=d(48) * rise + d(4) * shake, mane_lift=d(30) * rise)
        feet = {key: lg.foot for key, lg in legs().items()}
        for key in feet:
            if key[1] == "F":
                feet[key] = feet[key] + Vector((0, -R(0.04) * gather, R(0.06) * gather))
        return place_legs(poser, specs, feet)
    return HOWL_FRAMES, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = s_(0.1, 0.55, t)
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 50) * (1 if t > 0.6 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH + R(0.03) * jerk)), neck=d(25) * fall, head=d(15) * fall,
                           jaw=d(35) * fall + d(8) * twitch, tail=(d(14) * fall, 0.5), tail_lift=-d(14) * fall,
                           mane=-d(20) * fall, mane_lift=d(10) * fall)
        pivot = P(0.17, 0, 0)
        angle = d(84) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -R(0.19) * fall))}
        pose = poser.solve(specs)
        for key, lg in legs().items():
            move = pose[LEG_KINDS[key[1]]["parent"]] @ poser.rest[LEG_KINDS[key[1]]["parent"]].inverted()
            hip = move @ lg.root
            rest_vec = lg.foot - lg.root
            curl = Vector((0, R(0.08) if key[1] == "H" else -R(0.08), R(0.1))) + rest_vec * 0.6
            kick = Vector((0, R(0.03) * twitch, 0))
            foot = hip + move.to_3x3() @ (rest_vec.lerp(curl, fall) + kick)
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 40, spec, False


CLIPS = {"Run": run_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Howl": howl_spec}
# LOD1: the tail is one bone, each leg two (lower aimed from knee to toe); jaws and mane fans keep their bones (the
# howl needs them). 18 bones.
LOD_DROP = {"Tail_2": "Tail_1", "Tail_3": "Tail_1", **{f"Leg_{s}{k}_Foot": f"Leg_{s}{k}_Lower" for s, _ in SIDES for k in LEG_KINDS}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3"),
            **{f"Leg_{s}{k}_Lower": (f"Leg_{s}{k}_Lower", f"Leg_{s}{k}_Foot") for s, _ in SIDES for k in LEG_KINDS}}
TARGET = (0.0, -0.12, 0.5)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    extra = B.chain_extras(H.weathering(K, markings=PAL.dark, marking_amount=0.7, seed=63),
                           B.scars(scar_cuts(), PAL.glow, 1.0, width=R(0.02), depth=R(0.004)))
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0048, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.13,
                               extrusion=0.02, ray=0.06, emission_strength=2.5, extra=extra, specular=0.3,
                               mirror=True, k=DETAIL_K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder)
    poser = B.ScalePoser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "base_species": "Prowler", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
                          "low_texture_size": LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "howl_peak_frame": HOWL_PEAK_FRAME,
                          "run_distance_per_loop": round(STRIDE / STANCE, 3), "unity_scale": GAME_SCALE,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        detail(norender=True)
        return
    B.boss_previews(
        "packalpha", arm, asset.low, actions, lod, out, asset, target=TARGET, distance=4.4,
        game_scale=GAME_SCALE, base=dict(asset="Prowler", scale=PROWLER_SCALE, clip="Run", frame=5),
        move=("Run", 2), power=("Howl", 30),
        strips=[("Run", "side", range(0, RUN_FRAMES, 3)[:8]), ("Howl", "side", range(0, HOWL_FRAMES, 7)), ("Howl", "close", range(0, HOWL_FRAMES, 7)),
                ("Death", "close", range(0, 41, 6))],
        macros=[("ribs", P(0.2, 0.45, 0.14), 1.6, 80, 15), ("head", P(0.06, 0.45, 0.62), 1.4, -35, 18),
                ("mane", P(-0.12, 0.6, 0.28), 1.6, -60, 30)],
        rig_scale=2.0, minions=[(-1.5, 0.8, 0.15, 0), (-0.9, -0.7, 0.1, 5), (1.2, 0.8, 0.2, 11), (1.5, -0.5, 0.0, 3),
                                (-1.9, -0.1, -0.1, 8), (0.3, 1.4, 0.25, 14)])
    detail()


def detail(norender=False):
    """Round 45: the close-up detail (eyes, teeth, claws) as extra parts, exported again with the body."""
    import packalpha_detail
    packalpha_detail.run(norender)


if __name__ == "__main__":
    main()
