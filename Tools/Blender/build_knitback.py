"""Knitback: the wood-element healer that regrows while it walks and hides until it is close (round 15, name
proposed; see ArtSource/DESIGN_NOTES.md).

A tortoise-like body under a high arched back of split bark plates; three translucent sap pods on the midline (the
regrowth organs, own bones Pod_1..3, each with new shoots curling out of its top) glow amber-green between the
plates. Four twisted root legs with splayed root claws, a low head under a bark mask with amber eye slits and two
curled horn shoots, two hanging root tendrils behind. The skin is a two-value bark-and-moss camouflage: the pods and
eyes are the only light.

Clips: Walk (loop, four-beat), Idle (loop), Hit, Death, Regrow (the pods swell and pulse; only this clip keys the pod
bones' scale). Also exports Knitback_LOD1.

Run: blender --background --python Tools/Blender/build_knitback.py -- [--preview <dir>] [--norender]
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
from common import G  # noqa: E402

NAME = "Knitback"
K = 1.0
GAME_SCALE = 0.85  # proposal: a little larger than a Stinger

# Wood element: bark-brown and moss-olive (two close values for the camouflage), amber-green sap glow (warm).
PAL = H.Palette(top=(0.25, 0.19, 0.11), side=(0.1, 0.075, 0.045), belly=(0.04, 0.032, 0.02), seam=(0.02, 0.015, 0.01),
                dark=(0.06, 0.05, 0.03), glow=(0.95, 0.82, 0.22), eye=(1.0, 0.68, 0.22), membrane=(0.17, 0.2, 0.07),
                bone=(0.58, 0.52, 0.38))
POD = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=(0.04, 0.05, 0.012), glow=(0.85, 0.62, 0.12),
                eye=PAL.eye, membrane=(0.16, 0.22, 0.04), bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

PODS = [("Pod_1", -0.22, 0.48, 0.058), ("Pod_2", -0.05, 0.53, 0.068), ("Pod_3", 0.12, 0.51, 0.06)]  # (bone, fwd, up, radius)
POD_NAMES = [p[0] for p in PODS]
# Bark plates on the right side (x, up, fwd), radii, yaw: two rows that leave the midline open for the pods.
PLATES = [((0.15, 0.46, 0.2), (0.09, 0.1, 0.07), 10), ((0.17, 0.48, 0.03), (0.095, 0.11, 0.08), -5),
          ((0.165, 0.47, -0.14), (0.095, 0.1, 0.075), 8), ((0.13, 0.43, -0.3), (0.085, 0.09, 0.065), -12),
          ((0.23, 0.38, 0.1), (0.07, 0.1, 0.07), 20), ((0.24, 0.37, -0.12), (0.07, 0.11, 0.07), -15)]
LEG_TABLE = [((0.17, 0.3, 0.2), (0.35, 0.0, 0.33), (0.24, 0.26)),
             ((0.17, 0.3, -0.2), (0.34, 0.0, -0.33), (0.24, 0.26))]
TENDRILS = [(0.06, 0.3, -0.36), (0.1, 0.18, -0.5), (0.13, 0.06, -0.58)]  # right side root, mid, tip


def legs():
    return K3.limbs(LEG_TABLE, pole=(1.0, 0.7, 0.0))


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    b.ellipsoid("Body", G(0, 0.33, -0.03), (0.23, 0.34, 0.15), bone="Body", segments=(40, 20),
                style=K3.bark(PAL, k, grain=30, splits=0.5, moss=0.6, glow=0.3, seed=1))
    b.ellipsoid("Belly", G(0, 0.24, -0.02), (0.19, 0.3, 0.08), bone="Body", segments=(32, 12),
                style=H.glow_flesh(PAL, k, glow=0.35, flank=(0.6, 0.95), folds=8.0))
    for i, (p, r, yaw) in enumerate(PLATES):
        for side, sx in SIDES:
            b.ellipsoid("BarkPlate", K3.gp(p, sx), r, rot(yaw * sx, 8, 18 * sx), bone="Body", segments=(24, 12),
                        deform=C.chiselled(7, 30 + i, depth=0.16), style=K3.bark(PAL, k, grain=44, splits=0.9, moss=0.7,
                                                                             glow=0.55, seed=10 + i))
    for name, f, up, r in PODS:
        c = G(0, up, f)
        b.ellipsoid("Pod", c, (r * 0.9, r * 0.95, r * 1.55), bone=name, segments=(28, 14), style=K3.sap_pod(POD, k, glow=0.55, seed=3))
        b.parts[-1].chain = [name]
        # New shoots curling out of the pod top (a mirrored pair).
        for sx in (-1, 1):
            root = c + G(0.02 * sx, r * 1.4, 0.005)
            pts = [root, root + G(0.03 * sx, 0.06, -0.01), root + G(0.07 * sx, 0.08, -0.03), root + G(0.075 * sx, 0.05, -0.05)]
            b.tube("Shoot", pts, [0.011, 0.008, 0.005, 0.001], sides=8, chain=[name],
                   style=H.tube(POD, k, POD.membrane * 0.8, POD.glow * 0.7, tip_glow=1.0, glow_from=0.6))
    # Head: a low skull under a bark mask with amber slits, curled horn shoots, a dim mouth.
    b.ellipsoid("Head", G(0, 0.26, 0.37), (0.11, 0.12, 0.085), bone="Head", segments=(32, 16),
                style=K3.bark(PAL, k, grain=30, splits=0.3, moss=0.4, seed=5))
    b.ellipsoid("Mask", G(0, 0.29, 0.44), (0.125, 0.045, 0.085), rot(pitch=-12), bone="Head", segments=(32, 12),
                deform=C.chiselled(6, 77, depth=0.1), style=K3.bark(PAL, k, grain=50, splits=0.6, moss=0.2, glow=0.3, seed=7))
    b.ellipsoid("Mouth", G(0, 0.21, 0.45), (0.05, 0.02, 0.022), bone="Head", style=H.inner_glow(POD, 0.6), segments=(14, 6))
    for side, sx in SIDES:
        for p, r in (((0.045, 0.305, 0.485), (0.024, 0.008, 0.009)), ((0.088, 0.29, 0.468), (0.018, 0.007, 0.008))):
            b.ellipsoid("Eye", K3.gp(p, sx), r, rot(yaw=-20 * sx), bone="Head", style=H.eye(PAL, k, 1.0), segments=(12, 6))
        horn = [K3.gp(q, sx) for q in ((0.06, 0.34, 0.44), (0.1, 0.44, 0.41), (0.15, 0.49, 0.31), (0.14, 0.45, 0.24))]
        b.tube("HornShoot", horn, [0.022, 0.016, 0.01, 0.002], sides=8, chain=["Head"],
               style=K3.bark(PAL, k, grain=20, splits=0.2, moss=0.3, seed=9))
        # Root tendrils hanging from the rear.
        pts = [K3.gp(q, sx) for q in TENDRILS]
        b.tube("Tendril", pts, [0.026, 0.016, 0.003], sides=8, chain=[f"Tendril_{side}1", f"Tendril_{side}2"],
               style=K3.bark(PAL, k, grain=16, splits=0.3, moss=0.5, glow=0.4, seed=11))
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.015)), knee], [0.068, 0.058, 0.046], sides=10,
               chain=[lg.names[0]], style=K3.bark(PAL, k, grain=18, splits=0.4, moss=0.5, seed=20 + i))
        b.tube("LegLower", [knee, knee.lerp(foot, 0.5) + K3.gp((0.012, 0.0, 0.0), sx), foot + Vector((0, 0, 0.02))],
               [0.046, 0.04, 0.036], sides=10, chain=[lg.names[1]], style=K3.bark(PAL, k, grain=18, splits=0.4, moss=0.4, seed=24 + i))
        # Three root claws splayed over the ground.
        for j, a in enumerate((-40, 0, 40)):
            d = Matrix.Rotation(math.radians(a + (25 if i == 1 else -10) * sx), 3, "Z") @ K3.gp((0.25, 0.0, 1.0), sx).normalized()
            base = foot + Vector((0, 0, 0.02))
            b.tube("RootClaw", [base, base + d * 0.05 + Vector((0, 0, -0.008)), base + d * 0.1 + Vector((0, 0, -0.02))],
                   [0.016, 0.011, 0.002], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.32, -0.15), G(0, 0.34, 0.22), "Root", True),
           ("Head", G(0, 0.27, 0.28), G(0, 0.28, 0.46), "Body", True)]
    for name, f, up, r in PODS:
        out.append((name, G(0, up - r * 0.9, f), G(0, up + r * 1.2, f), "Body", True))
    for side, sx in SIDES:
        a, m, t = (K3.gp(q, sx) for q in TENDRILS)
        out.append((f"Tendril_{side}1", a, m, "Body", True))
        out.append((f"Tendril_{side}2", m, t, f"Tendril_{side}1", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, head=0.0, head_yaw=0.0, tendril=(0.0, 0.0), pods=None):
    amp, ph = tendril
    specs = {"Root": {"rot": []},
             "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
             "Head": {"rot": [(Z, head_yaw), (X, head)]}}
    for side, sx in SIDES:
        specs[f"Tendril_{side}1"] = {"rot": [(Z, amp * math.sin(ph + (sx > 0))), (X, amp * 0.5 * math.cos(ph))]}
        specs[f"Tendril_{side}2"] = {"rot": [(Z, amp * 1.4 * math.sin(ph - 0.9 + (sx > 0)))]}
    for name in POD_NAMES:
        specs[name] = {"rot": [], "scale": (pods or {}).get(name, 1.0)}
    return specs


WALK_FRAMES = 40
STRIDE, LIFT, STANCE = 0.14, 0.05, 0.72
# Four-beat walk: left fore, right hind, right fore, left hind.
PHASES = {("L", 1): 0.0, ("R", 2): 0.25, ("R", 1): 0.5, ("L", 2): 0.75}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.008 * math.cos(4 * w))), roll=math.radians(3) * math.sin(w),
                           yaw=math.radians(3) * math.sin(w), pitch_=math.radians(1.5) * math.sin(2 * w),
                           head=math.radians(3) * math.sin(2 * w + 0.8), head_yaw=math.radians(5) * math.sin(w + 0.5),
                           tendril=(math.radians(10), w))
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.006 * math.sin(w))), roll=math.radians(1.5) * math.sin(w),
                           pitch_=math.radians(2) * math.sin(w + 0.4), head=math.radians(5) * math.sin(w),
                           head_yaw=math.radians(12) * math.sin(w + 1.2), tendril=(math.radians(8), w))
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch_=math.radians(-6) * k, roll=math.radians(4) * k,
                           head=math.radians(-12) * k, tendril=(math.radians(14) * k, 1.0))
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 14, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.6, t)
        specs = body_specs(offset=Vector((0, 0, 0.02 * jerk - 0.16 * fall)), roll=math.radians(12) * fall,
                           pitch_=math.radians(6) * fall, head=math.radians(22) * fall, tendril=(math.radians(20) * fall, 0.5))
        return K3.splay_feet(poser, specs, lg, fall, out=1.35)
    return 44, spec, False


REGROW_FRAMES = 48


def regrow_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        swell = K2.pulse(t, 0.0, 0.35, 1.0)
        pods = {}
        for j, name in enumerate(POD_NAMES):
            beat = 0.5 + 0.5 * math.sin(3 * w - j * 1.2)
            pods[name] = 1.0 + 0.28 * swell + 0.08 * swell * beat
        specs = body_specs(offset=Vector((0, 0, 0.012 * swell)), pitch_=math.radians(-3) * swell,
                           head=math.radians(-6) * swell + math.radians(3) * math.sin(2 * w), tendril=(math.radians(9), w),
                           roll=math.radians(1.5) * math.sin(w), pods=pods)
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return REGROW_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Regrow": regrow_spec}
CLIP_SCALES = None  # every clip keys the pod scale (1 outside Regrow)
RIGID = {"BarkPlate", "Pod", "Mask"}
# LOD1: each leg one bone, each tendril one bone: 12 bones.
LOD_DROP = {**{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2)},
            **{f"Tendril_{s}2": f"Tendril_{s}1" for s, _ in SIDES}}
LOD_AIMS = {**{f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2)},
            **{f"Tendril_{s}1": (f"Tendril_{s}1", f"Tendril_{s}2") for s, _ in SIDES}}


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 12, (0.2, 0.9), -0.15), ("Regrow", 18, (1.2, 0.4), 0.3),
              ("Walk", 6, (-0.5, -0.4), 0.0), ("Walk", 26, (0.8, -0.6), -0.3)]
    previews = dict(target=(0, 0.0, 0.3), distance=2.4, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 5)), ("Regrow", "close", range(0, REGROW_FRAMES, 6)),
                            ("Death", "close", range(0, 45, 6))],
                    macros=[("pods", G(0.0, 0.55, -0.05), 0.45, -50, 35), ("head", G(0.0, 0.3, 0.45), 0.4, -25, 12),
                            ("bark", G(0.15, 0.48, -0.1), 0.35, -70, 30)],
                    rig_scale=1.2, power=("Regrow", 18))
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("chitin", "membrane", "rock"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS, scale_bones=POD_NAMES, clip_scales=CLIP_SCALES,
                    extra=H.weathering(K, scratches=0.3, dust=0.35, seed=23), specular=0.3,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                            "camouflage": "two-value bark/moss albedo; fade albedo towards the ground tint and the emission down",
                            "regrow_pod_bones": POD_NAMES},
                    previews=previews)


if __name__ == "__main__":
    main()
