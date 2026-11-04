"""Stinger: the armoured scorpion whose acid tail stings towers in passing (round 10, see ArtSource/DESIGN_NOTES.md).

Six legs on a low wide stance; two trap claws (a swollen hand with a fixed blade-finger and a hinged hook-finger,
serrated, glowing on the inside edge); a carapace and five shingled dorsal plates, each dark plum with a pale front
lip, a hot magenta rear rim and a forward chevron; a raised eye mound with six magenta eye beads and two hooked palps;
a tail of ringed segments arcing high over the back with acid glowing at every joint, ending in a veined acid bladder
and a long hooked needle that points forward over the head.

Clips: Walk (loop), Idle (loop), Hit, Death, Strike (tail stab, impact at STRIKE_HIT_FRAME). Also exports
Stinger_LOD1. The geometry and clips take a `cfg` so the AcidEmpress (build_acidempress.py) can reuse them.

Run: blender --background --python Tools/Blender/build_stinger.py -- [--preview <dir>] [--norender]
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
from common import G  # noqa: E402

NAME = "Stinger"
LOD_NAME = "Stinger_LOD1"
K = 1.0
GAME_TRIS = 3950
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 0.8  # EnemyDefinition scale

# Species colours from ProjectGenerator.Bestiary: violet body (0.6, 0.2, 0.85), magenta eyes and accent, acid green
# blood. Pushed warmer (plum-magenta) and apart in value: dark plum plates, near-black body, hot magenta plate glow;
# the tail carries the acid colour (lime-yellow).
PAL = H.Palette(top=(0.46, 0.12, 0.4), side=(0.11, 0.03, 0.11), belly=(0.03, 0.014, 0.032), seam=(0.022, 0.01, 0.024),
                dark=(0.08, 0.03, 0.08), glow=(1.0, 0.28, 0.82), eye=(1.0, 0.55, 0.95), membrane=(0.32, 0.06, 0.26),
                bone=(0.92, 0.78, 0.84))
LEGP = H.Palette(top=(0.24, 0.065, 0.23), side=(0.08, 0.025, 0.085), belly=(0.03, 0.014, 0.032), seam=(0.03, 0.012, 0.03),
                 dark=(0.06, 0.025, 0.06), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)
ACID = H.Palette(top=(0.42, 0.13, 0.38), side=(0.16, 0.045, 0.15), belly=(0.05, 0.02, 0.05), seam=(0.03, 0.015, 0.02),
                 dark=(0.08, 0.03, 0.07), glow=(0.78, 1.0, 0.2), eye=PAL.eye, membrane=(0.4, 0.5, 0.06),
                 bone=(0.95, 0.95, 0.7))

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

CFG = {
    "width": 1.0,          # body width factor
    "plates": [(0.05, 0.172), (-0.04, 0.18), (-0.13, 0.172), (-0.21, 0.155), (-0.285, 0.13)],  # (forward, half width)
    "claw": 1.0,           # claw size factor
    "tail": [G(0, 0.235, -0.33), G(0, 0.33, -0.45), G(0, 0.48, -0.5), G(0, 0.62, -0.44), G(0, 0.7, -0.31), G(0, 0.715, -0.17)],
    "tail_radii": [0.058, 0.054, 0.048, 0.043, 0.04, 0.036],
    "bladder": (G(0, 0.69, -0.07), (0.058, 0.08, 0.052)),
    "needle": [G(0, 0.68, -0.01), G(0, 0.66, 0.06), G(0, 0.6, 0.12), G(0, 0.53, 0.14)],
    "legs": [((0.11, 0.17, 0.2), (0.4, 0.0, 0.38)), ((0.125, 0.17, 0.08), (0.45, 0.0, 0.1)),
             ((0.12, 0.17, -0.04), (0.42, 0.0, -0.17))],
    "leg_lengths": [(0.24, 0.27), (0.24, 0.27), (0.23, 0.26)],
    "eyes": 6,
}


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def legs(cfg=CFG):
    out = {}
    for side, sx in SIDES:
        for i, ((hip, foot), lengths) in enumerate(zip(cfg["legs"], cfg["leg_lengths"])):
            names = [f"Leg_{side}{i + 1}_{s}" for s in ("Upper", "Lower")]
            out[(side, i + 1)] = C.Limb(names, gp(hip, sx), gp(foot, sx), lengths, G(0.35 * sx, 1.0, 0.0))
    return out


def claw_points(sx, cfg=CFG):
    """Shoulder, elbow, wrist, hand centre, fixed finger, moving finger (game positions scaled by the claw size)."""
    c = cfg["claw"]

    def P(x, up, f):
        return G(x * sx * c, 0.17 + (up - 0.17) * c, 0.27 + (f - 0.27) * c)
    shoulder = G(0.1 * sx, 0.18, 0.27)
    elbow = P(0.25, 0.21, 0.35)
    wrist = P(0.24, 0.2, 0.47)
    hand = P(0.215, 0.2, 0.53)
    fixed = [P(0.245, 0.2, 0.57), P(0.235, 0.2, 0.67), P(0.19, 0.2, 0.75), P(0.15, 0.2, 0.775)]
    moving = [P(0.18, 0.2, 0.575), P(0.14, 0.2, 0.64), P(0.135, 0.2, 0.7), P(0.155, 0.2, 0.735)]
    return shoulder, elbow, wrist, hand, fixed, moving


def tail_bone_points(cfg=CFG):
    t = cfg["tail"]
    return [(t[0], t[2]), (t[2], t[3]), (t[3], t[4]), (t[4], t[5])]


TAIL_BONES = ["Tail_1", "Tail_2", "Tail_3", "Sting"]
SEG_BONES = ["Tail_1", "Tail_1", "Tail_2", "Tail_3", "Sting"]


# --------------------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------------------

def build_parts(cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    w = cfg["width"]
    b.ellipsoid("Body", G(0, 0.195, 0.17), (0.14 * w, 0.15, 0.07), pitch(-4), bone="Body",
                style=H.band_plates(PAL, k, 2, height=0.003), segments=(40, 20))
    b.ellipsoid("Carapace", G(0, 0.245, 0.19), (0.16 * w, 0.165, 0.035), pitch(-6), bone="Body",
                style=K2.armour_plate(PAL, k, rim_glow=0.9, chevron=0.9, chevron_angle=1.0, cells=14, seed=3), segments=(40, 16))
    b.ellipsoid("Mound", G(0, 0.275, 0.27), (0.06, 0.05, 0.035), bone="Body", style=H.band_plates(PAL, k, 1, height=0.0),
                segments=(20, 10))
    for i in range(cfg["eyes"]):
        a = math.radians(-70 + 140 * i / max(1, cfg["eyes"] - 1))
        b.ellipsoid("Eye", G(0.062 * math.sin(a), 0.292 + 0.006 * math.cos(a * 2), 0.27 + 0.048 * math.cos(a)),
                    (0.017, 0.017, 0.015), bone="Body", style=H.eye(PAL, k, 0.55), segments=(12, 6))
    b.ellipsoid("Mouth", G(0, 0.16, 0.32), (0.045, 0.03, 0.025), bone="Body", style=H.inner_glow(ACID, 0.7), segments=(16, 8))
    for side, sx in SIDES:
        root = G(0.035 * sx, 0.17, 0.32)
        b.tube("Palp", [root, root + G(0.012 * sx, -0.01, 0.05), root + G(0.0, -0.045, 0.075)], [0.016, 0.012, 0.002],
               sides=8, chain=["Body"], style=H.hook(PAL, k))

    b.ellipsoid("Abdomen", G(0, 0.2, -0.12), (0.16 * w, 0.24, 0.075), pitch(3), bone="Abdomen",
                style=H.glow_flesh(PAL, k, glow=0.6, flank=(0.55, 0.95), folds=12.0), segments=(40, 20))
    b.parts[-1].chain = ["Abdomen", "Body"]
    for i, (f, hw) in enumerate(cfg["plates"]):
        b.ellipsoid("Plate", G(0, 0.245 + 0.004 * i, f), (hw * w, 0.058, 0.03), pitch(9), bone="Abdomen",
                    style=K2.armour_plate(PAL, k, rim_glow=1.0, chevron=0.85, cells=10, seed=10 + i), segments=(36, 14))
        base = G(0, 0.27 + 0.004 * i, f - 0.02)
        b.tube("Keel", [base, base + G(0, 0.03, -0.012), base + G(0, 0.04, -0.045)], [0.013, 0.007, 0.001], sides=8,
               chain=["Abdomen"], style=H.hook(PAL, k, glow=1.0, glow_from=0.6))
        # A short hook at each plate's rear corners: a serrated outline from above.
        for sx in (-1, 1):
            base = G((hw * w - 0.012) * sx, 0.245, f - 0.03)
            b.tube("PlateHook", [base, base + G(0.03 * sx, 0.01, -0.02), base + G(0.045 * sx, 0.0, -0.05)],
                   [0.012, 0.007, 0.001], sides=8, chain=["Abdomen"], style=H.hook(PAL, k, glow=0.8, glow_from=0.7))

    # The tail: ringed segments with acid glowing at the joints, the bladder and the needle.
    t, r = cfg["tail"], cfg["tail_radii"]
    for i in range(len(t) - 1):
        a, c = t[i], t[i + 1]
        mid = a.lerp(c, 0.5) + (c - a).cross(X).normalized() * 0.004
        b.tube("TailSeg", [a, mid, c], [r[i] * 0.95, r[i] * 1.08, r[i + 1] * 0.9], sides=12, chain=[SEG_BONES[i]],
               style=K2.ring_segment(ACID, k, joint_glow=0.9, base=PAL.top, tip=PAL.side * 1.3, rings=2, joint_width=0.07))
        # A flanged collar at the front of every segment, with three short hooks: a ringed, armoured tail.
        b.ring("Collar", a.lerp(c, 0.12), c - a, r[i] * 1.12, r[i] * 0.22, arcs=3, sides=6, chain=[SEG_BONES[i]],
               style=H.band_plates(PAL, k, 1, height=0.0), ref=Vector((1, 0, 0)))
        # A backward spine on top of every segment.
        top = mid + (c - a).cross(X).normalized() * -r[i] * 0.9
        back = (a - c).normalized()
        b.tube("TailSpine", [top, top + back * 0.025 + (top - mid).normalized() * 0.03, top + back * 0.06 + (top - mid).normalized() * 0.035],
               [0.011, 0.006, 0.001], sides=8, chain=[SEG_BONES[i]], style=H.hook(PAL, k, glow=0.7, glow_from=0.7))
    centre, radii = cfg["bladder"]
    b.ellipsoid("Bladder", centre, radii, pitch(-25), bone="Sting", style=K2.ribbed_bladder(ACID, k, ribs=7, seed=7, plate=0.35),
                segments=(32, 16))
    b.tube("Needle", cfg["needle"], [0.024, 0.017, 0.009, 0.0015], sides=10, chain=["Sting"],
           style=H.tube(ACID, k, PAL.side, ACID.bone, tip_glow=1.0, glow_from=0.55))
    n0, n1 = cfg["needle"][1], cfg["needle"][2]
    for sx in (-1, 1):
        b.tube("NeedleBarb", [n0, n0 + G(0.03 * sx, 0.0, 0.015), n0 + G(0.05 * sx, -0.03, 0.03)], [0.011, 0.007, 0.001],
               sides=8, chain=["Sting"], style=H.hook(ACID, k, glow=1.0, glow_from=0.6))

    for side, sx in SIDES:
        shoulder, elbow, wrist, hand, fixed, moving = claw_points(sx, cfg)
        c = cfg["claw"]
        b.tube("ClawArm", [shoulder, shoulder.lerp(elbow, 0.5) + G(0, 0.02, 0), elbow], [0.03, 0.028 * c, 0.03 * c],
               sides=10, chain=[f"Arm_{side}"], style=H.leg(LEGP, k, "Femur", spines=2))
        b.tube("ClawWrist", [elbow, elbow.lerp(wrist, 0.5), wrist], [0.03 * c, 0.032 * c, 0.038 * c], sides=10,
               chain=[f"Claw_{side}"], style=H.leg(LEGP, k, "Femur"))
        b.ellipsoid("Hand", hand, (0.055 * c, 0.085 * c, 0.042 * c), Matrix.Rotation(math.radians(14 * sx), 3, "Z"),
                    bone=f"Claw_{side}", style=K2.armour_plate(PAL, k, rim_glow=0.5, chevron=0.0, lip=0.4, cells=8, seed=20 + (sx > 0)),
                    segments=(28, 14))
        b.parts[-1].chain = [f"Claw_{side}"]
        b.tube("FixedFinger", fixed, [0.03 * c, 0.027 * c, 0.018 * c, 0.002], sides=10, chain=[f"Claw_{side}"],
               style=H.blade(PAL, k, glow=1.0, teeth=4), flatten=0.5)
        b.tube("HookFinger", moving, [0.026 * c, 0.022 * c, 0.015 * c, 0.002], sides=10, chain=[f"Finger_{side}"],
               style=H.blade(PAL, k, glow=1.0, teeth=3), flatten=0.5)
        barb = hand + G(0.035 * sx * c, 0.02 * c, -0.04 * c)
        b.tube("HandBarb", [barb, barb + G(0.03 * sx * c, 0.015 * c, -0.03 * c), barb + G(0.035 * sx * c, 0.005, -0.065 * c)],
               [0.014 * c, 0.008 * c, 0.001], sides=8, chain=[f"Claw_{side}"], style=H.hook(PAL, k, glow=0.6))

    for (side, i), lg in legs(cfg).items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.012)), knee], [0.03, 0.03, 0.021], sides=10,
               chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=3), flatten=0.7)
        b.ellipsoid("Knee", knee, (0.024,) * 3, bone=lg.names[0], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(14, 7))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        tip = foot + (foot - knee).normalized() * 0.015
        b.tube("LegLower", [knee, knee.lerp(foot, 0.45) + gp((0.012, 0.01, 0), sx), tip], [0.019, 0.015, 0.0015], sides=10,
               chain=[lg.names[1]], style=H.leg(LEGP, k, "Tibia"))
        b.tube("KneeSpur", [knee, knee + Vector((0, 0, 0.045)) + gp((0.012, 0, 0), sx)], [0.011, 0.001], sides=8,
               chain=[lg.names[0]], style=H.hook(PAL, k))
    return b


def bones(cfg=CFG):
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
        ("Body", G(0, 0.19, 0.02), G(0, 0.21, 0.3), "Root", True),
        ("Abdomen", G(0, 0.2, 0.02), G(0, 0.22, -0.33), "Body", True),
    ]
    parent = "Abdomen"
    for name, (a, c) in zip(TAIL_BONES, tail_bone_points(cfg)):
        out.append((name, a, c, parent, True))
        parent = name
    for side, sx in SIDES:
        shoulder, elbow, wrist, hand, fixed, moving = claw_points(sx, cfg)
        out.append((f"Arm_{side}", shoulder, elbow, "Body", True))
        out.append((f"Claw_{side}", elbow, fixed[-1], f"Arm_{side}", True))
        out.append((f"Finger_{side}", moving[0], moving[-1], f"Claw_{side}", True))
    for lg in legs(cfg).values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, belly=0.0, tail=(0.0, 0.0, 0.0), claws=0.0, claw_spread=0.0,
               pinch=0.0, claw_raise=0.0, sting=0.0, tail_base=0.0):
    """tail: (curl over the back, sway sideways, wave phase); claws: forearm reach forward; claw_spread: arms swing
    out; pinch: how far the hook-fingers open (rad); claw_raise: claws lifted; sting: extra bend of the sting bone."""
    curl, sway, phase = tail
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
        "Abdomen": {"rot": [(X, belly), (Z, -yaw * 0.6)]},
    }
    weights = (0.6, 0.9, 1.1, 1.0)
    for i, name in enumerate(TAIL_BONES):
        specs[name] = {"rot": [(Z, sway * math.sin(phase - i * 0.8) * (0.5 + 0.25 * i)), (X, curl * weights[i])]}
    specs["Sting"]["rot"].append((X, sting))
    specs["Tail_1"]["rot"].append((X, tail_base))
    for side, sx in SIDES:
        specs[f"Arm_{side}"] = {"rot": [(Z, -sx * claw_spread), (X, -claw_raise)]}
        specs[f"Claw_{side}"] = {"rot": [(Z, sx * claws * 0.6), (X, -claws * 0.4)]}
        specs[f"Finger_{side}"] = {"rot": [(Z, -sx * pinch)]}
    return specs


def place_legs(poser, specs, feet, cfg=CFG):
    pose = poser.solve(specs)
    move = pose["Body"] @ poser.rest["Body"].inverted()
    for key, lg in legs(cfg).items():
        specs.update(lg.pose(poser, move @ lg.root, feet[key], move.to_3x3() @ lg.pole))
    return specs


WALK_FRAMES = 28
STRIDE, LIFT, STANCE = 0.13, 0.05, 0.55
TRIPOD_A = {("L", 1), ("R", 2), ("L", 3)}


def walk_spec(poser, cfg=CFG):
    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.005 * math.cos(2 * w))), roll=math.radians(2) * math.sin(w),
                           yaw=math.radians(2.5) * math.sin(w), belly=math.radians(2) * math.sin(2 * w + 0.5),
                           tail=(math.radians(2) * math.sin(2 * w), math.radians(6), w),
                           claws=math.radians(4) * math.sin(w + 0.7), claw_spread=math.radians(3) * math.sin(w),
                           pinch=math.radians(8) * (0.5 + 0.5 * math.sin(2 * w)), claw_raise=math.radians(3) * math.sin(2 * w))
        feet = {}
        for key, lg in legs(cfg).items():
            phase = (t + (0.0 if key in TRIPOD_A else 0.5)) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet, cfg)
    return WALK_FRAMES, spec, True


def idle_spec(poser, cfg=CFG):
    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w + 0.3)) ** 5
        specs = body_specs(offset=Vector((0, 0, 0.003 * math.sin(w))), roll=math.radians(1.2) * math.sin(w),
                           belly=math.radians(2) * math.sin(w), tail=(math.radians(4) * math.sin(w), math.radians(9), w),
                           sting=math.radians(6) * math.sin(2 * w), claws=math.radians(6) * math.sin(w + 1.0),
                           claw_spread=math.radians(6) * math.sin(w * 0.5) ** 2, pinch=math.radians(4) + math.radians(28) * snap,
                           claw_raise=math.radians(4) * math.sin(w + 0.3) + math.radians(5) * snap)
        return place_legs(poser, specs, {k_: lg.foot for k_, lg in legs(cfg).items()}, cfg)
    return 60, spec, True


def hit_spec(poser, cfg=CFG):
    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.025 * k, 0.015 * k)), pitch_=math.radians(-7) * k, roll=math.radians(5) * k,
                           tail=(math.radians(-12) * k, math.radians(8) * k, 1.0), claws=-math.radians(12) * k,
                           claw_spread=math.radians(12) * k, pinch=math.radians(30) * max(k, 0.0), claw_raise=math.radians(10) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs(cfg).items()}, cfg)
    return 12, spec, False


STRIKE_FRAMES = 24
STRIKE_HIT_FRAME = 10


def strike_spec(poser, cfg=CFG):
    hit = STRIKE_HIT_FRAME / STRIKE_FRAMES

    def spec(t, f):
        wind = K2.pulse(t, 0.0, 0.25, hit)
        stab = K2.pulse(t, 0.25, hit, 0.85)
        hold = K2.ease(hit, hit + 0.05, t) * (1 - K2.ease(0.55, 0.95, t))
        specs = body_specs(offset=Vector((0, -0.02 * stab, -0.012 * stab)), pitch_=math.radians(-3) * wind + math.radians(5) * stab,
                           belly=math.radians(-4) * wind + math.radians(4) * stab,
                           tail=(math.radians(-10) * wind - math.radians(24) * stab, 0.0, 0.0),
                           tail_base=math.radians(-14) * wind + math.radians(80) * stab,
                           sting=math.radians(-12) * wind + math.radians(15) * stab + math.radians(4) * hold * math.sin(t * 60),
                           claws=math.radians(8) * stab, claw_spread=math.radians(14) * K2.pulse(t, 0.0, 0.3, 0.9),
                           pinch=math.radians(25) * wind, claw_raise=math.radians(8) * wind)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs(cfg).items()}, cfg)
    return STRIKE_FRAMES, spec, False


def death_spec(poser, cfg=CFG):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = K2.ease(0.08, 0.5, t)
        twitch = math.exp(-max(0.0, t - 0.55) * 6) * math.sin(t * 55) * (1 if t > 0.55 else 0)
        specs = body_specs(offset=Vector((0, 0, 0.025 * jerk - 0.11 * fall)), roll=math.radians(10) * fall,
                           pitch_=math.radians(4) * fall, belly=math.radians(-3) * fall,
                           tail=(math.radians(-48) * fall + math.radians(4) * twitch, math.radians(14) * fall, 0.6),
                           tail_base=math.radians(-22) * fall,
                           sting=math.radians(-25) * fall, claws=math.radians(-15) * fall, claw_spread=math.radians(22) * fall,
                           pinch=math.radians(35) * fall, claw_raise=math.radians(-12) * fall)
        pose = poser.solve(specs)
        move = pose["Body"] @ poser.rest["Body"].inverted()
        for key, lg in legs(cfg).items():
            # The legs splay out flat and give a last kick: the body settles onto the road.
            hip = move @ lg.root
            out = (lg.foot - lg.root)
            out = Vector((out.x * 1.2, out.y * 1.1, 0.0))
            kick = Vector((0, 0, 0.02 * twitch)) if key in TRIPOD_A else Vector((0, 0, -0.01 * twitch))
            foot = lg.foot.lerp(Vector((hip.x + out.x, hip.y + out.y, 0.0)), fall) + kick
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole))
        return specs
    return 40, spec, False


def clips(cfg=CFG):
    def bind(fn):
        return lambda poser: fn(poser, cfg)
    return {"Walk": bind(walk_spec), "Idle": bind(idle_spec), "Hit": bind(hit_spec), "Death": bind(death_spec),
            "Strike": bind(strike_spec)}


CLIPS = clips()
# LOD1: the hook-fingers fold into the claws, the abdomen into the body, the tail is two bones and each leg one. 14.
LOD_DROP = {"Finger_L": "Claw_L", "Finger_R": "Claw_R", "Abdomen": "Body", "Tail_2": "Tail_1", "Tail_3": "Sting",
            **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2, 3)}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_2"), "Sting": ("Tail_3", "Sting"),
            **{f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2, 3)}}


def build_asset(name, builder, tris, voxel=0.0034, k=K):
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.08,
                              extrusion=0.012, ray=0.04, emission_strength=2.5,
                              extra=H.weathering(k, markings=PAL.dark, marking_amount=0.5, seed=91), specular=0.3,
                              mirror=True, k=k, families=FAMILIES, low_size=LOW_TEX)


RIGID = {"Carapace", "Plate", "Hand", "Bladder"}


def main():
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
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "strike_hit_frame": STRIKE_HIT_FRAME,
                          "walk_distance_per_loop": round(STRIDE / STANCE, 3), "unity_scale": GAME_SCALE,
                          "pivot": "ground, body centre",
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 9, (0.2, 0.9), -0.15), ("Strike", STRIKE_HIT_FRAME, (1.2, 0.4), 0.3),
              ("Walk", 5, (-0.5, -0.4), 0.0), ("Walk", 17, (0.8, -0.6), -0.3), ("Idle", 20, (-1.6, -0.2), 0.4)]
    K2.enemy_previews("stinger", arm, asset.low, actions, lod, out, asset, target=(0, 0.0, 0.3), distance=2.6,
                      game_scale=GAME_SCALE, move=("Walk", 4), layout=layout,
                      strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Strike", "side", range(0, STRIKE_FRAMES, 3)),
                              ("Death", "close", range(0, 41, 6))],
                      macros=[("plates", G(0.06, 0.26, -0.1), 0.45, -50, 40), ("claw", G(-0.2, 0.2, 0.6), 0.45, -40, 25),
                              ("tail", G(0.0, 0.69, -0.1), 0.45, -60, 15)],
                      rig_scale=1.3, power=("Strike", STRIKE_HIT_FRAME))


if __name__ == "__main__":
    main()
