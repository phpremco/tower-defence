"""Bulwark: the upright shield bearer with a crystal sword, armoured, from level 11 to 12 (round 10, see
ArtSource/DESIGN_NOTES.md).

A heavy saurian biped leaning into its walk on thick digitigrade legs with hooked toes; a natural shield (a curved
bone plate with growth ridges, a pale rim and a glowing boss) grown from the left forearm; a Crysium blade grown out
of the right forearm, held low; shoulder plates, a chest plate and a ridge of back spines over dark olive scales with
lime goo in the deep seams; a long low head under a bony visor with amber eye slits, tusked mandibles and a crest of
backward horns; a heavy tail with keel plates.

Clips: Walk (loop), Idle (loop), Hit, Death, Block (shield up by BLOCK_UP_FRAME, then held). Also exports
Bulwark_LOD1. Geometry and clips take a `cfg` so the CrystalWarlord (build_crystalwarlord.py) can reuse them.

Run: blender --background --python Tools/Blender/build_bulwark.py -- [--preview <dir>] [--norender]
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
from common import G  # noqa: E402

NAME = "Bulwark"
LOD_NAME = "Bulwark_LOD1"
K = 1.0
GAME_TRIS = 3950
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "crystal", "membrane")
GAME_SCALE = 0.9

# Species colours from ProjectGenerator.Bestiary: green body (0.3, 0.6, 0.35), amber eyes, goo-green blood; the data
# accent is cyan (our side's colour), so the sword is the planet's own pink Crysium instead.
PAL = H.Palette(top=(0.3, 0.42, 0.16), side=(0.1, 0.16, 0.06), belly=(0.035, 0.045, 0.025), seam=(0.02, 0.03, 0.012),
                dark=(0.06, 0.085, 0.035), glow=(0.62, 1.0, 0.22), eye=(1.0, 0.72, 0.22), membrane=(0.25, 0.4, 0.08),
                bone=(0.86, 0.82, 0.66))
PLATE = H.Palette(top=(0.4, 0.42, 0.26), side=(0.15, 0.17, 0.08), belly=(0.05, 0.06, 0.03), seam=(0.025, 0.03, 0.015),
                  dark=(0.08, 0.1, 0.05), glow=PAL.glow, eye=PAL.eye, bone=(0.9, 0.86, 0.7))
EYEP = H.Palette(top=PLATE.top, side=PLATE.side, belly=PLATE.belly, seam=PLATE.seam, dark=PLATE.dark,
                 glow=(1.0, 0.65, 0.2), eye=(1.0, 0.72, 0.22), bone=PLATE.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

CFG = {
    "sword_len": 0.6,
    "sword_width": 0.05,
    "shield": (0.035, 0.2, 0.26),
    "horns": 3,
}

SHOULDER = (0.2, 0.79, 0.04)
ELBOW = (0.3, 0.6, 0.0)
WRIST_R = (0.31, 0.48, 0.17)
WRIST_L = (0.24, 0.52, 0.18)
HIP = (0.11, 0.46, -0.03)
FOOT = (0.16, 0.0, 0.04)
TAIL = [G(0, 0.47, -0.14), G(0, 0.4, -0.38), G(0, 0.31, -0.62), G(0, 0.23, -0.86)]


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def legs():
    out = {}
    for side, sx in SIDES:
        names = [f"Leg_{side}_{s}" for s in ("Thigh", "Shin", "Foot")]
        out[side] = C.Limb(names, gp(HIP, sx), gp(FOOT, sx), (0.23, 0.23, 0.11), G(0.1 * sx, 0.1, 1.0),
                           foot_dir=G(0, -0.9, 0.42))
    return out


def rot(axis, deg):
    return Matrix.Rotation(math.radians(deg), 3, axis)


def sword_points(cfg=CFG):
    w = gp(WRIST_R, 1)
    d = G(0.02, -0.55, 0.84).normalized()
    L = cfg["sword_len"]
    return [w, w + d * L * 0.35 + G(0.01, 0, 0), w + d * L * 0.75, w + d * L]


# --------------------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------------------

def build_parts(cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    hide = K2.scale_hide(PAL, k, cells=70, seed=3, joint_glow=0.55)
    b.ellipsoid("Pelvis", G(0, 0.47, -0.04), (0.15, 0.13, 0.11), bone="Pelvis", style=hide, segments=(32, 16))
    b.ellipsoid("Torso", G(0, 0.64, 0.04), (0.17, 0.15, 0.2), rot("X", -18), bone="Spine", style=hide, segments=(36, 18))
    b.parts[-1].chain = ["Spine", "Pelvis"]
    b.ellipsoid("ChestPlate", G(0, 0.66, 0.17), (0.13, 0.15, 0.035), rot("X", 70), bone="Spine",
                style=K2.armour_plate(PLATE, k, rim_glow=0.7, chevron=0.6, chevron_angle=1.1, cells=10, seed=5), segments=(32, 14))
    b.ellipsoid("Belly", G(0, 0.52, 0.07), (0.12, 0.08, 0.08), bone="Pelvis", segments=(24, 12),
                style=H.glow_flesh(PAL, k, glow=0.45, flank=(0.0, 0.5), folds=10.0))
    for i, f in enumerate((0.0, -0.1, -0.2)):
        base = G(0, 0.8 - 0.06 * i, f)
        b.tube("BackSpine", [base, base + G(0, 0.07, -0.03), base + G(0, 0.09, -0.1)], [0.022, 0.012, 0.0015], sides=8,
               chain=["Spine"], style=H.hook(PLATE, k, glow=0.6, glow_from=0.7))
    b.tube("Neck", [G(0, 0.76, 0.1), G(0, 0.83, 0.18), G(0, 0.87, 0.24)], [0.08, 0.07, 0.06], sides=12, chain=["Neck"],
           style=H.tube(PAL, k, PAL.top * 0.9, PAL.side, rings=(0.3, 0.6)))

    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.35 * front), v.y, v.z * (1 - 0.3 * front)))
    b.ellipsoid("Head", G(0, 0.89, 0.32), (0.08, 0.15, 0.065), rot("X", 6), bone="Head", segments=(32, 16), deform=head_deform,
                style=H.wedge_head(EYEP, k, slits=3, slit_width=0.05))
    b.ellipsoid("Visor", G(0, 0.935, 0.29), (0.082, 0.135, 0.024), rot("X", 4), bone="Head", segments=(32, 14),
                style=K2.armour_plate(PLATE, k, rim_glow=0.5, chevron=0.9, chevron_angle=1.3, lip=0.8, seed=7))
    b.ellipsoid("Jaw", G(0, 0.835, 0.33), (0.065, 0.12, 0.028), rot("X", -6), bone="Jaw", segments=(24, 12),
                style=H.band_plates(PAL, k, 3, height=0.002))
    b.ellipsoid("Throat", G(0, 0.86, 0.38), (0.035, 0.05, 0.016), bone="Head", style=H.inner_glow(PAL, 0.9), segments=(16, 8))
    for i in range(cfg["horns"]):
        base = G(0, 0.965 - 0.005 * i, 0.32 - 0.075 * i)
        size = 1.0 - 0.15 * i
        b.tube("Horn", [base, base + G(0, 0.06 * size, -0.04), base + G(0, 0.08 * size, -0.13 * size)],
               [0.022 * size, 0.013 * size, 0.0015], sides=8, chain=["Head"], style=H.hook(PLATE, k))
    for side, sx in SIDES:
        root = G(0.045 * sx, 0.84, 0.4)
        b.tube("Tusk", [root, root + G(0.03 * sx, -0.01, 0.06), root + G(0.04 * sx, 0.03, 0.1)], [0.016, 0.011, 0.0015],
               sides=8, chain=["Jaw"], style=H.hook(PLATE, k, glow=0.4))
        root = G(0.075 * sx, 0.93, 0.25)
        b.tube("Cheek", [root, root + G(0.04 * sx, 0.01, -0.05), root + G(0.06 * sx, 0.0, -0.11)], [0.016, 0.01, 0.0012],
               sides=8, chain=["Head"], style=H.hook(PLATE, k))

        # Arms; the shoulder plate rides the upper arm.
        sh, el = gp(SHOULDER, sx), gp(ELBOW, sx)
        wr = gp(WRIST_R if sx > 0 else WRIST_L, sx)
        up, fore = f"UpperArm_{side}", f"ForeArm_{side}"
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5) + gp((0.02, 0, 0), sx), el], [0.07, 0.06, 0.05], sides=12, chain=[up],
               style=hide)
        b.ellipsoid("Elbow", el, (0.05,) * 3, bone=up, style=hide, segments=(16, 8))
        b.parts[-1].chain = [up, fore]
        b.tube("ForeArm", [el, el.lerp(wr, 0.5), wr], [0.05, 0.05, 0.045], sides=12, chain=[fore], style=hide)
        b.ellipsoid("ShoulderPlate", sh + gp((0.03, 0.03, 0.0), sx), (0.085, 0.11, 0.03), rot("Y", 48 * sx), bone=up,
                    style=K2.armour_plate(PLATE, k, rim_glow=0.8, chevron=0.0, lip=0.7, cells=8, seed=9 + (sx > 0)),
                    segments=(28, 12))
        for j in range(2):
            base = sh + gp((0.07 + 0.03 * j, 0.06, 0.03 - 0.05 * j), sx)
            b.tube("ShoulderSpike", [base, base + gp((0.03, 0.05, -0.02), sx), base + gp((0.05, 0.06, -0.07), sx)],
                   [0.016, 0.009, 0.001], sides=8, chain=[up], style=H.hook(PLATE, k, glow=0.5, glow_from=0.75))
        if sx < 0:
            # The natural shield on the left forearm.
            centre = el.lerp(wr, 0.55) + G(-0.075, 0.02, 0.0)
            b.ellipsoid("Shield", centre, cfg["shield"], rot("Z", 22) @ rot("X", -8), bone=fore, segments=(36, 20),
                        style=K2.bone_shield(PLATE, k, rings=4, boss_glow=1.0, rim_glow=0.5, seed=12))
            # Hooks grown along the shield's outer rim.
            sh_rot = rot("Z", 22) @ rot("X", -8)
            sy, sz = cfg["shield"][1], cfg["shield"][2]
            for j, a in enumerate((-2.2, -1.2, -0.2, 0.8, 1.8, 2.6)):
                local = Vector((0.0, math.cos(a) * sy * 0.97, math.sin(a) * sz * 0.97))
                base = centre + sh_rot @ local
                out_dir = (sh_rot @ Vector((0.0, math.cos(a), math.sin(a)))).normalized()
                b.tube("ShieldHook", [base, base + out_dir * 0.03 + G(-0.01, 0, 0), base + out_dir * 0.05 + G(-0.025, -0.01, 0)],
                       [0.014, 0.008, 0.001], sides=8, chain=[fore], style=H.hook(PLATE, k, glow=0.4))
            b.ellipsoid("Fist", wr, (0.045, 0.05, 0.04), bone=fore, style=hide, segments=(16, 8))
        else:
            sw = sword_points(cfg)
            sw_w = cfg["sword_width"]
            b.tube("Sword", sw, [sw_w * 0.9, sw_w, sw_w * 0.75, 0.002], sides=10, chain=[fore],
                   style=K2.crysium_blade(k), flatten=0.22)
            for j, (a, d) in enumerate(((0.3, (0.6, 1.0, -0.3)), (0.5, (1.0, 0.6, 0.2)), (0.7, (0.4, 1.0, 0.5)))):
                base = el.lerp(wr, a) + gp((0.03, 0.02, 0.0), sx)
                b.shard("Bracer", base, gp(d, sx), 0.017, 0.07, sides=6, twist=1.5, flute=0.35, bone=fore,
                        style=Y.shard_style("pink", 0.9, seed=30 + j))
            b.ellipsoid("Fist", wr, (0.05, 0.055, 0.045), bone=fore, style=hide, segments=(16, 8))

    for i in range(3):
        a, c = TAIL[i], TAIL[i + 1]
        b.tube("Tail", [a, a.lerp(c, 0.5), c], [0.1 - 0.025 * i, 0.085 - 0.025 * i, 0.07 - 0.025 * i], sides=12,
               chain=[f"Tail_{i + 1}"], style=hide)
        top = a.lerp(c, 0.5) + G(0, 0.08 - 0.022 * i, 0)
        b.tube("TailKeel", [top - G(0, 0.03, -0.03), top + G(0, 0.03, -0.05), top + G(0, 0.03, -0.12)],
               [0.018, 0.01, 0.0015], sides=8, chain=[f"Tail_{i + 1}"], style=H.hook(PLATE, k, glow=0.5))

    for side, lg in legs().items():
        hip, knee, ankle, toe = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.01)), knee], [0.085, 0.075, 0.05], sides=12,
               chain=[lg.names[0]], style=hide)
        b.ellipsoid("Knee", knee, (0.05,) * 3, bone=lg.names[0], style=hide, segments=(16, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("Shin", [knee, knee.lerp(ankle, 0.5), ankle], [0.05, 0.042, 0.035], sides=10, chain=[lg.names[1]], style=hide)
        b.ellipsoid("Ankle", ankle, (0.036,) * 3, bone=lg.names[1], style=hide, segments=(14, 7))
        b.parts[-1].chain = [lg.names[1], lg.names[2]]
        b.tube("Foot", [ankle, ankle.lerp(toe, 0.5), toe], [0.035, 0.032, 0.03], sides=10, chain=[lg.names[2]], style=hide)
        for a in (-0.45, 0.0, 0.45):
            fwd = G(math.sin(a) * 0.8, 0, 1).normalized()
            b.tube("Toe", [toe, toe + fwd * 0.05 + Vector((0, 0, 0.008)), toe + fwd * 0.085 - Vector((0, 0, 0.015))],
                   [0.02, 0.013, 0.0015], sides=8, chain=[lg.names[2]], style=H.hook(PLATE, k))
        b.tube("Spur", [ankle, ankle + G(0, 0.0, -0.05), ankle + G(0, 0.02, -0.09)], [0.016, 0.009, 0.001], sides=8,
               chain=[lg.names[2]], style=H.hook(PLATE, k))
    return b


def bones(cfg=CFG):
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Pelvis", G(0, 0.42, -0.06), G(0, 0.55, -0.02), "Root", True),
        ("Spine", G(0, 0.55, -0.02), G(0, 0.78, 0.1), "Pelvis", True),
        ("Neck", G(0, 0.78, 0.1), G(0, 0.88, 0.24), "Spine", True),
        ("Head", G(0, 0.88, 0.24), G(0, 0.88, 0.44), "Neck", True),
        ("Jaw", G(0, 0.85, 0.25), G(0, 0.82, 0.44), "Head", True),
        ("Tail_1", TAIL[0], TAIL[1], "Pelvis", True),
        ("Tail_2", TAIL[1], TAIL[2], "Tail_1", True),
        ("Tail_3", TAIL[2], TAIL[3], "Tail_2", True),
    ]
    for side, sx in SIDES:
        sh, el = gp(SHOULDER, sx), gp(ELBOW, sx)
        wr = gp(WRIST_R if sx > 0 else WRIST_L, sx)
        out.append((f"UpperArm_{side}", sh, el, "Spine", True))
        out.append((f"ForeArm_{side}", el, wr, f"UpperArm_{side}", True))
    for lg in legs().values():
        out += lg.rest_bones("Pelvis")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, lean=0.0, roll=0.0, yaw=0.0, twist=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0, 0.0), sword=(0.0, 0.0, 0.0), shield=(0.0, 0.0, 0.0)):
    """lean: spine pitch forward (+); twist: chest yaw against the hips; tail: (sway, phase, lift);
    sword / shield: (upper arm forward swing, upper arm out, forearm bend) in radians, + forward/up."""
    sway, phase, lift = tail
    specs = {
        "Root": {"rot": []},
        "Pelvis": {"rot": [(Z, yaw), (Yv, roll)], "loc": offset if offset is not None else Vector()},
        "Spine": {"rot": [(Z, twist), (X, lean)]},
        "Neck": {"rot": [(X, neck), (Z, head_yaw * 0.4)]},
        "Head": {"rot": [(X, head), (Z, head_yaw * 0.6)]},
        "Jaw": {"rot": [(X, -jaw)]},
    }
    for i in range(3):
        specs[f"Tail_{i + 1}"] = {"rot": [(Z, sway * math.sin(phase - i * 0.9) * (0.6 + 0.3 * i)), (X, lift * (1 - 0.3 * i))]}
    for side, sx, (fwd, out, bend) in (("R", 1, sword), ("L", -1, shield)):
        specs[f"UpperArm_{side}"] = {"rot": [(Yv, sx * out), (X, -fwd)]}
        specs[f"ForeArm_{side}"] = {"rot": [(X, -bend)]}
    return specs


def place_legs(poser, specs, feet, foot_dirs=None):
    pose = poser.solve(specs)
    move = pose["Pelvis"] @ poser.rest["Pelvis"].inverted()
    for key, lg in legs().items():
        fd = foot_dirs[key] if foot_dirs else lg.foot_dir
        specs.update(lg.pose(poser, move @ lg.root, feet[key], move.to_3x3() @ lg.pole, fd))
    return specs


CROUCH = -0.03
WALK_FRAMES = 32
STRIDE, LIFT, STANCE = 0.3, 0.07, 0.6


def walk_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, CROUCH - 0.015 * abs(math.cos(w)))), lean=d(8), roll=d(3) * math.sin(w),
                           yaw=d(5) * math.sin(w), twist=-d(6) * math.sin(w), neck=-d(4), head=d(3) * math.sin(2 * w),
                           tail=(d(9), w, -d(4) + d(2) * math.sin(2 * w)),
                           sword=(d(8) * math.sin(w + math.pi), d(6), d(10)), shield=(d(10), d(4), d(15) + d(3) * math.sin(w)))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key == "L" else 0.5)) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            fold = math.sin(math.pi * (phase - STANCE) / (1 - STANCE)) if phase >= STANCE else 0.0
            dirs[key] = Matrix.Rotation(d(-40) * fold, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, CROUCH + 0.006 * math.sin(2 * w))), lean=d(6) + d(1.5) * math.sin(2 * w),
                           roll=d(1.5) * math.sin(w), neck=-d(3), head=d(3) * math.sin(2 * w + 1),
                           head_yaw=d(20) * math.sin(w) * math.sin(0.5 * w + 0.3), jaw=d(6) * max(0.0, math.sin(3 * w)) ** 4,
                           tail=(d(8), w, -d(4)), sword=(d(5) + d(4) * math.sin(w), d(6), d(10)),
                           shield=(d(10), d(4), d(15) + d(2) * math.sin(w + 1)))
        return place_legs(poser, specs, {k_: lg.foot for k_, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.03 * k, CROUCH - 0.01 * k)), lean=d(6) - d(14) * k, roll=d(5) * k,
                           twist=d(8) * k, neck=-d(3) - d(10) * k, head=-d(10) * k, jaw=d(25) * max(k, 0.0),
                           tail=(d(14) * k, 1.0, d(10) * k), sword=(d(5) - d(10) * k, d(6) + d(10) * k, d(10)),
                           shield=(d(10) + d(10) * k, d(4), d(15)))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


BLOCK_FRAMES = 24
BLOCK_UP_FRAME = 7


def block_spec(poser):
    d = math.radians
    up_t = BLOCK_UP_FRAME / BLOCK_FRAMES

    def spec(t, f):
        up = K2.ease(0.0, up_t, t)
        brace = K2.pulse(t, up_t, up_t + 0.08, 0.45) * 0.6
        specs = body_specs(offset=Vector((0, 0.02 * up, CROUCH - 0.05 * up - 0.01 * brace)), lean=d(6) + d(6) * up,
                           twist=d(16) * up, neck=-d(3) - d(6) * up, head=-d(4) * up, jaw=d(14) * up,
                           tail=(d(5), 1.0, d(8) * up), sword=(d(5) - d(12) * up, d(6) + d(12) * up, d(10) - d(12) * up),
                           shield=(d(10) + d(58) * up, d(4) - d(14) * up, d(15) + d(45) * up))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return BLOCK_FRAMES, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        buckle = K2.ease(0.05, 0.4, t)
        fall = K2.ease(0.3, 0.8, t)
        twitch = math.exp(-max(0.0, t - 0.8) * 8) * math.sin(t * 50) * (1 if t > 0.8 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH - 0.12 * buckle)), lean=d(10) + d(25) * buckle, neck=d(20) * fall,
                           head=d(15) * fall, jaw=d(30) * fall + d(6) * twitch, tail=(d(10) * fall, 0.4, -d(8) * fall),
                           sword=(d(25) * fall, d(30) * fall, -d(10) * fall), shield=(d(10) - d(20) * fall, d(35) * fall, d(5)))
        # It topples onto its right side (the sword side), pivoting on the right foot.
        pivot = Vector((0.2, 0, 0))
        angle = d(80) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -0.06 * fall))}
        pose = poser.solve(specs)
        move = pose["Pelvis"] @ poser.rest["Pelvis"].inverted()
        for key, lg in legs().items():
            hip = move @ lg.root
            rest_vec = lg.foot - lg.root
            bent = Vector((rest_vec.x, rest_vec.y - 0.12, rest_vec.z * 0.7))
            local = rest_vec.lerp(bent, max(buckle, fall))
            foot = hip + move.to_3x3() @ local if fall > 0 else lg.foot
            if fall <= 0:
                foot = lg.foot
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 44, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Block": block_spec}
# LOD1: jaw into the head, one tail bone. 16 bones.
LOD_DROP = {"Jaw": "Head", "Tail_2": "Tail_1", "Tail_3": "Tail_1"}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3")}
RIGID = {"ChestPlate", "Visor", "ShoulderPlate", "Shield", "ShieldHook", "Sword", "Bracer", "Horn", "Jaw", "Head"}


def build_asset(name, builder, tris, voxel=0.0042, k=K):
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.1,
                              extrusion=0.014, ray=0.045, emission_strength=2.5,
                              extra=H.weathering(k, markings=PAL.dark, marking_amount=0.5, seed=33), specular=0.3,
                              mirror=False, symmetric=False, k=k, families=FAMILIES, low_size=LOW_TEX)


def asymmetric_decimate():
    """The Bulwark is not mirror-symmetric (shield left, sword right): common.build_lod always asks for a symmetric
    decimation, so this run swaps in a plain one (only inside this Blender process)."""
    plain = C.decimate

    def decimate(mesh, target_tris, name, symmetric=True):
        return plain(mesh, target_tris, name, symmetric=False)
    C.decimate = decimate


def main():
    asymmetric_decimate()
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = build_asset(NAME, builder, GAME_TRIS)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "block_up_frame": BLOCK_UP_FRAME,
                          "walk_distance_per_loop": round(STRIDE / STANCE, 3), "unity_scale": GAME_SCALE,
                          "pivot": "ground, between the feet",
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 10, (0.3, 0.9), -0.15), ("Block", 20, (1.3, 0.3), 0.3),
              ("Walk", 6, (-0.4, -0.5), 0.0), ("Walk", 22, (0.9, -0.7), -0.3)]
    K2.enemy_previews("bulwark", arm, asset.low, actions, lod, out, asset, target=(0, 0.0, 0.55), distance=3.2,
                      game_scale=GAME_SCALE, move=("Walk", 4), layout=layout,
                      strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Block", "front", range(0, BLOCK_FRAMES, 3)),
                              ("Death", "close", range(0, 45, 6))],
                      macros=[("shield", G(-0.38, 0.58, 0.12), 0.5, -80, 10), ("head", G(0.0, 0.92, 0.32), 0.45, -35, 15),
                              ("sword", G(0.32, 0.3, 0.45), 0.5, -10, 20)],
                      rig_scale=1.4, power=("Block", 20))
    # Round 46: the close-up detail (eyes, fangs, talons) as extra parts, exported again with the body.
    import bulwark_detail
    bulwark_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
