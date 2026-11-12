"""Siftjaw: the sand-element digger that burrows for a moment to dodge turret fire (round 15, name proposed;
redesigned from scratch in round 49B, see ArtSource/DESIGN_NOTES.md).

Round 49B (Remco on the round-15 model: "ugly"): an armoured arrowhead hugging the ground. A flat shovel scoop with a
keel carries a cluster of five red eyes on its crown (the Face part) and two great sickle jaws (own bones
Jaw_L/Jaw_R), serrated on the inner edge, that hook in front of it like a pincer ring and flare wide when they open;
a broad thorax shield with a forward chevron whose rim is drawn out into back-raked side spikes; heavy digging arms
ending in toothed spades; a segmented abdomen of shingled plates with side and dorsal spines that tapers into a
fluted, twisted drill cone (it screws itself in). Ochre-tan plates, dark umber bands, red-orange glowing seams.

Clips: Walk (loop), Idle (loop), Hit, Death, Burrow (noses down, spades churn, sinks fully under the ground by
BURROW_UNDER_FRAME), Emerge (bursts up out of the ground at EMERGE_BREACH_FRAME and shakes off). Also exports
Siftjaw_LOD1.

Run: blender --background --python Tools/Blender/build_siftjaw.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import stone as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Siftjaw"
K = 1.0
GAME_SCALE = 0.75  # proposal

# Sand element: ochre-tan plates with dark umber bands, red-orange glow, red eyes.
PAL = H.Palette(top=(0.46, 0.31, 0.15), side=(0.22, 0.13, 0.06), belly=(0.09, 0.06, 0.035), seam=(0.05, 0.03, 0.016),
                dark=(0.17, 0.1, 0.05), glow=(1.0, 0.33, 0.1), eye=(1.0, 0.28, 0.16), membrane=(0.45, 0.14, 0.05),
                bone=(0.86, 0.76, 0.56))
# Round 49B: spikes and serrations in dark umber with red-hot tips (they must read against the tan plates).
SPK = H.Palette(top=(0.2, 0.12, 0.06), side=(0.12, 0.07, 0.035), belly=(0.05, 0.03, 0.015), seam=(0.03, 0.02, 0.01),
                dark=(0.06, 0.035, 0.02), glow=(1.0, 0.33, 0.1), eye=(1.0, 0.28, 0.16), bone=(0.45, 0.16, 0.07))
# Round 49B: limbs a step darker (umber) than the sand plates, so the armour reads as one wedge from above.
LEGP = H.Palette(top=(0.26, 0.16, 0.08), side=(0.13, 0.075, 0.035), belly=PAL.belly, seam=PAL.seam, dark=PAL.dark,
                 glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

LEG_TABLE = [((0.15, 0.13, -0.02), (0.29, 0.0, -0.03), (0.15, 0.17)),
             ((0.13, 0.12, -0.17), (0.27, 0.0, -0.29), (0.15, 0.17))]
ARM = {"shoulder": (0.15, 0.15, 0.2), "elbow": (0.27, 0.14, 0.27), "wrist": (0.25, 0.07, 0.37), "spade": (0.245, 0.055, 0.42)}
SPADE_ROT = (-15, -35)  # (yaw, pitch) of the spade plate; the Claws part puts its digging claws on its front edge
# Round 49B: the sickle jaws (right side, mirrored): a curve that swings out from the jaw root and hooks in towards
# the midline in front of the scoop, so the closed pair is a pincer ring and the open pair flares wide.
JAW = [(0.065, 0.11, 0.35), (0.14, 0.115, 0.44), (0.145, 0.105, 0.54), (0.1, 0.1, 0.62), (0.035, 0.1, 0.66)]
JAW_RADII = [0.032, 0.028, 0.02, 0.011, 0.001]
# Eyes (the Face part puts the lenses here): a cluster of five on the crown of the scoop, the centre one biggest.
EYES = [((0.0, 0.222, 0.335), 0.016), ((0.036, 0.215, 0.322), 0.014), ((0.066, 0.203, 0.3), 0.012)]


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.4, 1.0, 0.0))


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def jaw_points(sx):
    return [K3.gp(q, sx) for q in JAW]


def spike(b, base, d, length, radius, chain, style, name="Spike"):
    d = Vector(d).normalized()
    b.tube(name, [base, base + d * length * 0.55 + Vector((0, 0, 0.004)), base + d * length],
           [radius, radius * 0.55, 0.001], sides=6, chain=chain, style=style)


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    hook = H.hook(SPK, k, glow=0.9, glow_from=0.7)
    b.ellipsoid("Under", G(0, 0.12, 0.04), (0.16, 0.22, 0.06), bone="Body", segments=(32, 14),
                style=H.glow_flesh(PAL, k, glow=0.18, flank=(0.55, 0.95), folds=9.0))
    # The thorax: one broad armour shield with a forward chevron, its rim drawn out into back-raked side spikes (from
    # above an arrowhead, widest at the shoulders).
    b.ellipsoid("Thorax", G(0, 0.175, 0.1), (0.22, 0.18, 0.075), rot(pitch=-4), bone="Body", segments=(40, 18),
                deform=C.chiselled(16, 9, depth=0.06, squash=(0.6, 1.0)),
                style=K2.armour_plate(PAL, k, rim_glow=0.9, chevron=1.0, chevron_angle=1.1, lip=0.5, cells=18, seed=3))
    for j, (f, ln) in enumerate(((0.2, 0.11), (0.1, 0.14), (0.0, 0.12))):
        for side, sx in SIDES:
            spike(b, K3.gp((0.19 - 0.01 * j, 0.17, f), sx), K3.gp((1.0, 0.12, -0.55 - 0.15 * j), sx), ln, 0.026,
                  ["Body"], hook)
    for j, f in enumerate((0.17, 0.08, -0.01)):
        spike(b, G(0, 0.245, f), G(0, 0.55, -1.0), 0.06 - 0.01 * j, 0.016, ["Body"], hook, name="DorsalSpine")
    b.ellipsoid("Abdomen", G(0, 0.15, -0.12), (0.16, 0.11, 0.08), bone="Abdomen_1", segments=(36, 16),
                style=H.band_plates(PAL, k, 3, height=0.004, glow=0.0, shingle=True))
    b.ellipsoid("AbdomenRear", G(0, 0.13, -0.27), (0.12, 0.1, 0.066), bone="Abdomen_2", segments=(32, 14),
                style=H.band_plates(PAL, k, 3, height=0.004, glow=0.0, shingle=True))
    b.parts[-1].chain = ["Abdomen_2", "Abdomen_1"]
    for i, f in enumerate((-0.07, -0.17, -0.25, -0.32)):
        r = 0.165 - 0.028 * i
        bone = "Abdomen_1" if i < 2 else "Abdomen_2"
        b.ellipsoid("SegPlate", G(0, 0.2 - 0.016 * i, f), (r, 0.05, 0.03), rot(pitch=12), bone=bone,
                    deform=C.chiselled(10, 20 + i, depth=0.08, squash=(0.5, 1.0)),
                    style=K2.armour_plate(PAL, k, rim_glow=1.0, chevron=0.0, lip=0.7, seed=10 + i), segments=(28, 10))
        for side, sx in SIDES:
            spike(b, K3.gp((r * 0.92, 0.19 - 0.016 * i, f - 0.01), sx), K3.gp((0.8, 0.1, -0.8), sx), 0.08 - 0.012 * i,
                  0.02, [bone], hook)
        spike(b, G(0, 0.225 - 0.016 * i, f - 0.01), G(0, 0.5, -1.0), 0.05 - 0.008 * i, 0.013, [bone], hook,
              name="DorsalSpine")
    b.shard("Drill", G(0, 0.12, -0.33), G(0, -0.2, -1.0), 0.065, 0.25, sides=8, flute=0.45, twist=5.0, rings=16,
            bone="Abdomen_2", chain=["Abdomen_2"], style=S.pillar(PAL, k, drums=6, seed=14, glow=0.8))
    # Head: a flat shovel scoop with a keel, the eye cluster on its crown (Face part), a glowing gullet under it and
    # two great sickle jaws, serrated on the inner edge, that hook in front of it (own bones Jaw_L/Jaw_R).
    b.ellipsoid("Head", G(0, 0.145, 0.3), (0.15, 0.12, 0.062), rot(pitch=-10), bone="Head", segments=(36, 16),
                deform=C.chiselled(14, 31, depth=0.06, squash=(0.6, 1.0)),
                style=K2.armour_plate(PAL, k, rim_glow=0.7, chevron=0.6, chevron_angle=1.4, lip=1.0, cells=10, seed=32))
    b.ellipsoid("Keel", G(0, 0.2, 0.27), (0.03, 0.11, 0.03), rot(pitch=-6), bone="Head", segments=(20, 10),
                style=H.band_plates(PAL, k, 2, height=0.002))
    b.ellipsoid("Gullet", G(0, 0.1, 0.335), (0.05, 0.02, 0.018), bone="Head", style=H.inner_glow(PAL, 0.6), segments=(14, 8))
    for (x, u, f), r in EYES:
        for sx in ((1,) if x == 0 else (1, -1)):
            b.ellipsoid("EyeSocket", K3.gp((x, u - 0.006, f), sx), (r * 1.2, r * 1.2, r * 0.8), bone="Head",
                        segments=(12, 6), style=H.inner_glow(PAL, 0.45))
    for side, sx in SIDES:
        pts = jaw_points(sx)
        b.tube("Jaw", pts, JAW_RADII, sides=10, chain=[f"Jaw_{side}"], flatten=0.5, style=H.blade(PAL, k, glow=0.9, teeth=6))
        curve = C.catmull_rom(pts, 6)
        for j, t in enumerate((0.3, 0.45, 0.6, 0.74)):
            i = int(t * (len(curve) - 1))
            p, tan = curve[i], (curve[i + 1] - curve[i]).normalized()
            inward = (G(-sx, 0, 0) - tan * tan.dot(G(-sx, 0, 0))).normalized()
            spike(b, p + inward * 0.012, inward + tan * 0.6, 0.03 - 0.004 * j, 0.009, [f"Jaw_{side}"],
                  H.hook(PAL, k, glow=0.7, glow_from=0.6), name="JawTooth")
        sh, el, wr, sp = (K3.gp(ARM[n], sx) for n in ("shoulder", "elbow", "wrist", "spade"))
        b.tube("ArmUpper", [sh, sh.lerp(el, 0.5) + Vector((0, 0, 0.018)), el], [0.046, 0.042, 0.036], sides=8,
               chain=[f"Arm_{side}_Upper"], style=H.leg(LEGP, k, "Femur", spines=3), flatten=0.8)
        spike(b, sh.lerp(el, 0.6) + Vector((0, 0, 0.03)), K3.gp((0.3, 1.0, -0.5), sx), 0.05, 0.014,
              [f"Arm_{side}_Upper"], hook)
        b.tube("ArmFore", [el, el.lerp(wr, 0.5), wr], [0.036, 0.042, 0.046], sides=8, chain=[f"Arm_{side}_Spade"],
               style=H.leg(LEGP, k, "Femur", spines=2), flatten=0.75)
        spike(b, el.lerp(wr, 0.4) + K3.gp((0.03, 0.02, 0.0), sx), K3.gp((1.0, 0.4, -0.3), sx), 0.05, 0.014,
              [f"Arm_{side}_Spade"], hook)
        spade_rot = rot(yaw=SPADE_ROT[0] * sx, pitch=SPADE_ROT[1])
        b.ellipsoid("Spade", sp, (0.07, 0.085, 0.022), spade_rot, bone=f"Arm_{side}_Spade", segments=(24, 10),
                    deform=C.chiselled(9, 24, depth=0.08, squash=(0.6, 1.0)),
                    style=K2.armour_plate(PAL, k, rim_glow=0.6, chevron=0.0, lip=1.0, cells=5, seed=24))
        b.parts[-1].chain = [f"Arm_{side}_Spade"]
        ahead = spade_rot @ Vector((0, -1, 0))
        for j in range(4):
            base = sp + spade_rot @ Vector(((-0.045 + 0.03 * j) * sx, -0.066, 0.0))
            b.tube("SpadeTooth", [base, base + ahead * 0.03, base + ahead * 0.05 + Vector((0, 0, -0.008))],
                   [0.013, 0.008, 0.001], sides=6, chain=[f"Arm_{side}_Spade"], style=H.hook(PAL, k, glow=0.5))
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.01)), knee], [0.034, 0.032, 0.024], sides=10,
               chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=2), flatten=0.75)
        tip = foot + (foot - knee).normalized() * 0.012
        b.tube("LegLower", [knee, knee.lerp(foot, 0.5) + K3.gp((0.01, 0.008, 0), sx), tip], [0.024, 0.018, 0.002], sides=10,
               chain=[lg.names[1]], style=H.leg(LEGP, k, "Tibia"))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.16, -0.04), G(0, 0.18, 0.22), "Root", True),
           ("Head", G(0, 0.14, 0.22), G(0, 0.13, 0.38), "Body", True),
           ("Abdomen_1", G(0, 0.15, -0.03), G(0, 0.14, -0.19), "Body", True),
           ("Abdomen_2", G(0, 0.14, -0.19), G(0, 0.11, -0.5), "Abdomen_1", True)]
    for side, sx in SIDES:
        out.append((f"Jaw_{side}", K3.gp((0.07, 0.11, 0.32), sx), K3.gp((0.09, 0.1, 0.54), sx), "Head", True))
        out.append((f"Arm_{side}_Upper", K3.gp(ARM["shoulder"], sx), K3.gp(ARM["elbow"], sx), "Body", True))
        out.append((f"Arm_{side}_Spade", K3.gp(ARM["elbow"], sx), K3.gp(ARM["spade"], sx), f"Arm_{side}_Upper", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, head=0.0, jaws=0.0, belly=(0.0, 0.0), drill=0.0, arms=None):
    """arms: {side: (swing forward, spade dig)}; belly: (pitch, yaw) of the abdomen; drill: twist of the rear."""
    specs = {"Root": {"rot": []},
             "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
             "Head": {"rot": [(X, head)]},
             "Abdomen_1": {"rot": [(Z, belly[1]), (X, belly[0])]},
             "Abdomen_2": {"rot": [(Yv, drill), (Z, belly[1]), (X, belly[0])]}}
    for side, sx in SIDES:
        swing, dig = (arms or {}).get(side, (0.0, 0.0))
        specs[f"Jaw_{side}"] = {"rot": [(Z, -sx * jaws)]}
        specs[f"Arm_{side}_Upper"] = {"rot": [(Z, -sx * swing * 0.6), (X, -swing * 0.5)]}
        specs[f"Arm_{side}_Spade"] = {"rot": [(X, dig)]}
    return specs


def ride_feet(poser, specs, lg):
    """Feet that ride with the body (burrowing: the whole creature goes down)."""
    pose = poser.solve(specs)
    move = pose["Body"] @ poser.rest["Body"].inverted()
    for key, l in lg.items():
        specs.update(l.pose(poser, move @ l.root, move @ l.foot, move.to_3x3() @ l.pole))
    return specs


WALK_FRAMES = 24
STRIDE, LIFT, STANCE = 0.12, 0.04, 0.6
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        arms = {"L": (math.radians(14) * math.sin(w), math.radians(10) * math.sin(w + 1.2)),
                "R": (math.radians(14) * math.sin(w + math.pi), math.radians(10) * math.sin(w + math.pi + 1.2))}
        specs = body_specs(offset=Vector((0, 0, 0.005 * math.cos(2 * w))), roll=math.radians(2) * math.sin(w),
                           yaw=math.radians(3) * math.sin(w), head=math.radians(2) * math.sin(2 * w),
                           jaws=math.radians(5) * (0.5 + 0.5 * math.sin(2 * w)), belly=(0.0, math.radians(5) * math.sin(w + 1.0)),
                           arms=arms)
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w + 0.6)) ** 6
        arms = {s: (math.radians(5) * math.sin(w + (s == "R")), math.radians(8) * math.sin(2 * w + (s == "R") * 2)) for s in ("L", "R")}
        specs = body_specs(offset=Vector((0, 0, 0.003 * math.sin(w))), roll=math.radians(1.5) * math.sin(w),
                           head=math.radians(3) * math.sin(w), jaws=math.radians(4) + math.radians(26) * snap,
                           belly=(math.radians(2) * math.sin(w), math.radians(5) * math.sin(w)), arms=arms)
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 60, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch_=math.radians(-7) * k, roll=math.radians(5) * k,
                           jaws=math.radians(25) * max(k, 0.0), arms={s: (-math.radians(12) * k, math.radians(15) * k) for s in ("L", "R")})
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = K2.ease(0.08, 0.5, t)
        specs = body_specs(offset=Vector((0, 0, 0.02 * jerk - 0.08 * fall)), roll=math.radians(160) * K2.ease(0.15, 0.7, t),
                           pitch_=math.radians(5) * fall, jaws=math.radians(35) * fall, belly=(math.radians(-15) * fall, 0.0),
                           arms={s: (math.radians(30) * fall, math.radians(40) * fall) for s in ("L", "R")})
        return ride_feet(poser, specs, lg)
    return 40, spec, False


BURROW_FRAMES = 36
BURROW_UNDER_FRAME = 24  # fully under the ground plane from here (the clip runs to 36)
DEPTH = 0.5


def burrow_spec(poser):
    lg = legs()
    under = BURROW_UNDER_FRAME / BURROW_FRAMES

    def spec(t, f):
        tip = K2.ease(0.0, 0.25, t)
        sink = K2.ease(0.15, under, t)
        churn = math.sin(t * 2 * math.pi * 6)
        arms = {"L": (math.radians(25) * churn * tip, math.radians(35) * churn * tip),
                "R": (-math.radians(25) * churn * tip, -math.radians(35) * churn * tip)}
        specs = body_specs(offset=Vector((0, -0.12 * sink, -DEPTH * sink)), pitch_=math.radians(28) * tip,
                           jaws=math.radians(18) * tip, head=math.radians(10) * tip,
                           belly=(math.radians(-14) * tip, math.radians(6) * churn * tip), drill=math.radians(720) * sink, arms=arms)
        return ride_feet(poser, specs, lg)
    return BURROW_FRAMES, spec, False


EMERGE_FRAMES = 36
EMERGE_BREACH_FRAME = 8


def emerge_spec(poser):
    lg = legs()
    breach = EMERGE_BREACH_FRAME / EMERGE_FRAMES

    def spec(t, f):
        rise = K2.ease(0.0, 0.4, t)
        hop = K2.pulse(t, 0.3, 0.42, 0.6)
        level = K2.ease(0.35, 0.65, t)
        shake = math.exp(-max(0.0, t - 0.6) * 9) * math.sin(t * 70) * (t > 0.6)
        arms = {s: (math.radians(30) * (1 - level), -math.radians(30) * (1 - level)) for s in ("L", "R")}
        specs = body_specs(offset=Vector((0, 0.1 * (1 - rise), -DEPTH * (1 - rise) + 0.05 * hop)),
                           pitch_=-math.radians(35) * (1 - level), roll=math.radians(10) * shake,
                           jaws=math.radians(30) * (1 - level) + math.radians(10) * abs(shake),
                           belly=(math.radians(16) * (1 - level), math.radians(10) * shake), arms=arms)
        if t < breach:
            return ride_feet(poser, specs, lg)
        return ride_feet_blend(poser, specs, lg, level)
    return EMERGE_FRAMES, spec, False


def ride_feet_blend(poser, specs, lg, a):
    """Feet between riding with the body (a = 0) and planted on the ground (a = 1)."""
    pose = poser.solve(specs)
    move = pose["Body"] @ poser.rest["Body"].inverted()
    for key, l in lg.items():
        foot = (move @ l.foot).lerp(l.foot, a)
        specs.update(l.pose(poser, move @ l.root, foot, move.to_3x3() @ l.pole))
    return specs


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Burrow": burrow_spec,
         "Emerge": emerge_spec}
RIGID = {"Thorax", "Jaw", "JawTooth", "Spade", "SpadeTooth", "SegPlate", "Keel"}
# LOD1: jaws fold into the head, the rear abdomen into the front one, each rear leg one bone: 12 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", "Abdomen_2": "Abdomen_1",
            **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2)}}
LOD_AIMS = {"Abdomen_1": ("Abdomen_1", "Abdomen_2"),
            **{f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2)}}


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 8, (0.2, 0.9), -0.15), ("Burrow", 20, (1.2, 0.4), 0.3),
              ("Walk", 4, (-0.5, -0.4), 0.0), ("Emerge", 14, (0.8, -0.6), -0.3)]
    previews = dict(target=(0, 0.0, 0.18), distance=2.0, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 3)), ("Burrow", "side", range(0, BURROW_FRAMES, 4)),
                            ("Emerge", "side", range(0, EMERGE_FRAMES, 4)), ("Death", "close", range(0, 41, 6))],
                    macros=[("jaws", G(0.0, 0.12, 0.45), 0.4, -30, 25), ("plates", G(0.05, 0.2, -0.1), 0.4, -60, 35),
                            ("spade", G(0.25, 0.07, 0.4), 0.3, -50, 15)],
                    rig_scale=1.0, power=("Burrow", 12))
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("chitin", "membrane", "rock"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
                    extra=H.weathering(K, scratches=0.8, dust=0.6, seed=37), specular=0.3,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                            "burrow": {"under_frame": BURROW_UNDER_FRAME, "depth": DEPTH, "forward": 0.12},
                            "emerge": {"breach_frame": EMERGE_BREACH_FRAME, "start_depth": DEPTH, "start_back": 0.1}},
                    previews=previews)
    # Round 46: the close-up detail (eyes, jaw and gullet teeth, digging claws, talons) as extra parts, exported again with the body.
    import siftjaw_detail
    siftjaw_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
