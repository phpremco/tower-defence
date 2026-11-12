"""Knitback: the wood-element healer that regrows while it walks and hides until it is close (round 15, name
proposed; redesigned from scratch in round 49B, see ArtSource/DESIGN_NOTES.md).

Round 49B (Remco on the round-15 tortoise: "ugly"): a low, long stalker on four tall root legs whose burled knees
stand above its back. Its back is a cage of heartwood ribs that rise from the flanks and grip three glowing sap pods
(the regrowth organs, own bones Pod_1..3, new shoots curling out of each), so from above it reads as a star of
thorns round a glowing heart. A long low skull under a split bark mask, brow ridges over two pairs of eyes (the Face
part), thorn tusks hooked in like pincers, branching antlers swept back over the neck; bark shingles on the flanks,
two thorned root tendrils trailing behind. The skin keeps the two-value bark-and-moss camouflage: the pods, the sap
in the rib splits and the eyes are the only light.

Clips: Walk (loop, four-beat), Idle (loop), Hit, Death, Regrow (the pods swell and pulse; every clip keys the pod
bones' scale, 1 outside Regrow). Also exports Knitback_LOD1.

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

PODS = [("Pod_1", -0.2, 0.47, 0.066), ("Pod_2", -0.03, 0.52, 0.078), ("Pod_3", 0.14, 0.48, 0.064)]  # (bone, fwd, up, radius)
POD_NAMES = [p[0] for p in PODS]
# Round 49B: the thorn cage. Ribs of heartwood rise from the flanks (right side; mirrored) and grip the pods like
# curled fingers, tips hooked back out: from above a star of thorns round a glowing heart. (fwd, height, reach in,
# length scale).
RIBS = [(0.24, 0.55, 0.1, 0.8), (0.1, 0.66, 0.095, 1.0), (-0.05, 0.7, 0.095, 1.05), (-0.2, 0.64, 0.095, 0.95),
        (-0.33, 0.5, 0.1, 0.75)]
# Bark shingles low on the flanks (x, up, fwd), radii, yaw.
SHINGLES = [((0.17, 0.3, 0.18), (0.08, 0.1, 0.05), 12), ((0.19, 0.29, 0.02), (0.085, 0.11, 0.05), -4),
            ((0.18, 0.28, -0.15), (0.08, 0.1, 0.05), 8), ((0.15, 0.27, -0.3), (0.07, 0.09, 0.045), -14)]
LEG_TABLE = [((0.15, 0.3, 0.18), (0.42, 0.0, 0.4), (0.3, 0.34)),
             ((0.15, 0.3, -0.18), (0.42, 0.0, -0.38), (0.3, 0.34))]
TENDRILS = [(0.06, 0.3, -0.38), (0.12, 0.2, -0.58), (0.16, 0.05, -0.74)]  # right side root, mid, tip
# Eyes (the Face part puts the lenses here): a forward pair on the snout under the brow ridge, a rear pair wider
# and higher, near the antler roots.
EYES = (((0.074, 0.29, 0.565), 0.021), ((0.1, 0.305, 0.48), 0.016))


def legs():
    return K3.limbs(LEG_TABLE, pole=(1.0, 1.1, 0.0))


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def thorn_row(b, pts, chain, style, count, length, side_dir, seed=0):
    """Short thorns along a curve (on its outer side): hooked a little towards the tip of the curve."""
    curve = C.catmull_rom([Vector(p) for p in pts], 6)
    for j in range(count):
        t = (j + 0.6) / (count + 0.4)
        i = min(int(t * (len(curve) - 1)), len(curve) - 2)
        p, tan = curve[i], (curve[i + 1] - curve[i]).normalized()
        out = (Vector(side_dir) - tan * tan.dot(Vector(side_dir))).normalized()
        ln = length * (1.0 - 0.4 * t) * (0.8 + 0.4 * ((j * 7 + seed) % 3) / 2)
        b.tube("Thorn", [p, p + out * ln * 0.6 + tan * ln * 0.15, p + out * ln + tan * ln * 0.45],
               [ln * 0.22, ln * 0.12, 0.001], sides=6, chain=chain, style=style)


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    bark = K3.bark(PAL, k, grain=30, splits=0.6, moss=0.6, glow=0.35, seed=1)
    thorn = H.hook(PAL, k, glow=0.3, glow_from=0.85)
    # A low, long torso: bark over the top, a soft sap-lit underside.
    b.ellipsoid("Body", G(0, 0.3, -0.03), (0.17, 0.33, 0.12), bone="Body", segments=(40, 20), style=bark)
    b.ellipsoid("Belly", G(0, 0.22, -0.02), (0.14, 0.28, 0.07), bone="Body", segments=(32, 12),
                style=H.glow_flesh(PAL, k, glow=0.4, flank=(0.6, 0.95), folds=8.0))
    # The pod bed: a sunken glowing socket along the spine under the pods.
    b.ellipsoid("PodBed", G(0, 0.4, -0.03), (0.07, 0.26, 0.05), bone="Body", segments=(24, 12),
                style=K3.socket_floor(POD, k, glow=0.8, seed=2))
    for name, f, up, r in PODS:
        c = G(0, up, f)
        b.ellipsoid("Pod", c, (r * 0.8, r * 0.9, r * 1.75), bone=name, segments=(28, 14),
                    style=K3.sap_pod(POD, k, glow=0.8, seed=3, veins=30))
        b.parts[-1].chain = [name]
        # New shoots curling out of the pod top (a mirrored pair), lit at the tips.
        for sx in (-1, 1):
            root = c + G(0.02 * sx, r * 1.6, 0.005)
            pts = [root, root + G(0.03 * sx, 0.06, -0.01), root + G(0.07 * sx, 0.08, -0.03), root + G(0.075 * sx, 0.05, -0.05)]
            b.tube("Shoot", pts, [0.011, 0.008, 0.005, 0.001], sides=8, chain=[name],
                   style=H.tube(POD, k, POD.membrane * 0.8, POD.glow * 0.7, tip_glow=1.0, glow_from=0.6))
    # The thorn cage: heartwood ribs from the flanks, curled over the pods, hooked out at the tips; sap glows in
    # their splits near the roots.
    for i, (f, h, reach, s) in enumerate(RIBS):
        for side, sx in SIDES:
            root = G(0.15 * sx, 0.33, f)
            pts = [root, G(0.23 * sx, 0.4 * s + 0.02, f - 0.01), G(0.22 * sx, (h - 0.1) * s + 0.1, f - 0.04),
                   G((reach + 0.04) * sx, h, f - 0.08), G((reach + 0.06) * sx, h + 0.05 * s, f - 0.12),
                   G((reach + 0.12) * sx, h + 0.04 * s, f - 0.15)]
            b.tube("Rib", pts, [0.036, 0.03, 0.024, 0.017, 0.01, 0.002], sides=8, chain=["Body"],
                   style=K3.bark(PAL, k, grain=24, splits=0.8, moss=0.35, glow=0.6, seed=40 + i))
            thorn_row(b, pts[1:4], ["Body"], thorn, 2, 0.05, G(sx, 0.4, 0.0), seed=i)
    for i, (p, r, yaw) in enumerate(SHINGLES):
        for side, sx in SIDES:
            b.ellipsoid("Shingle", K3.gp(p, sx), r, rot(yaw * sx, 6, 40 * sx), bone="Body", segments=(20, 10),
                        deform=C.chiselled(8, 30 + i, depth=0.2),
                        style=K3.bark(PAL, k, grain=44, splits=0.9, moss=0.7, glow=0.4, seed=10 + i))
    # Head: a long, low skull under a split bark mask; thorn tusks hooked in like pincers at the jaw corners; a
    # crown of branching antlers swept back over the neck.
    b.ellipsoid("Head", G(0, 0.27, 0.44), (0.1, 0.18, 0.078), rot(pitch=-6), bone="Head", segments=(32, 16),
                style=K3.bark(PAL, k, grain=30, splits=0.4, moss=0.3, glow=0.3, seed=5))
    b.ellipsoid("Mask", G(0, 0.335, 0.46), (0.07, 0.16, 0.035), rot(pitch=-10), bone="Head", segments=(32, 12),
                deform=C.chiselled(9, 77, depth=0.15), style=K3.bark(PAL, k, grain=50, splits=0.7, moss=0.2, glow=0.4, seed=7))
    b.ellipsoid("Mouth", G(0, 0.225, 0.56), (0.035, 0.045, 0.012), bone="Head", style=H.inner_glow(POD, 0.25), segments=(14, 8))
    # A bark collar behind the skull, its rim splintered into thorns: the neck of a predator, not a tortoise.
    b.ellipsoid("Collar", G(0, 0.36, 0.3), (0.16, 0.08, 0.07), rot(pitch=-30), bone="Body", segments=(28, 12),
                deform=C.chiselled(10, 95, depth=0.18), style=K3.bark(PAL, k, grain=40, splits=0.8, moss=0.5, glow=0.4, seed=12))
    for side, sx in SIDES:
        # Brow ridges over the eyes: they make the face read as a face from above.
        b.ellipsoid("BrowRidge", K3.gp((0.082, 0.322, 0.53), sx), (0.022, 0.075, 0.013), rot(yaw=-12 * sx, roll=30 * sx, pitch=8),
                    bone="Head", segments=(16, 8), deform=C.chiselled(7, 90, depth=0.15),
                    style=K3.bark(PAL, k, grain=40, splits=0.3, moss=0.1, seed=8))
        tusk = [K3.gp(q, sx) for q in ((0.07, 0.21, 0.58), (0.1, 0.2, 0.66), (0.075, 0.19, 0.74), (0.025, 0.2, 0.78))]
        b.tube("Tusk", tusk, [0.022, 0.016, 0.009, 0.001], sides=8, chain=["Head"], style=thorn)
        for f in (0.33, 0.27):
            base = K3.gp((0.12, 0.38, f), sx)
            b.tube("CollarThorn", [base, base + K3.gp((0.05, 0.06, -0.04), sx), base + K3.gp((0.08, 0.09, -0.1), sx)],
                   [0.016, 0.009, 0.001], sides=6, chain=["Body"], style=thorn)
        antler = [K3.gp(q, sx) for q in ((0.06, 0.35, 0.4), (0.11, 0.46, 0.34), (0.18, 0.54, 0.22), (0.23, 0.57, 0.08))]
        b.tube("Antler", antler, [0.02, 0.016, 0.01, 0.002], sides=8, chain=["Head"],
               style=K3.bark(PAL, k, grain=20, splits=0.3, moss=0.2, glow=0.25, seed=9))
        for base_t, d, ln in ((0.35, (0.4, 1.0, 0.25), 0.1), (0.65, (0.7, 0.8, -0.1), 0.08)):
            c0 = C.catmull_rom([Vector(p) for p in antler], 6)
            p0 = c0[int(base_t * (len(c0) - 1))]
            dd = K3.gp(d, sx).normalized()
            b.tube("Antler", [p0, p0 + dd * ln * 0.6, p0 + dd * ln + G(0, 0, -0.02)], [0.011, 0.007, 0.001], sides=6,
                   chain=["Head"], style=thorn)
        # Root tendrils trailing from the rear, thorned.
        pts = [K3.gp(q, sx) for q in TENDRILS]
        b.tube("Tendril", pts, [0.026, 0.016, 0.003], sides=8, chain=[f"Tendril_{side}1", f"Tendril_{side}2"],
               style=K3.bark(PAL, k, grain=16, splits=0.3, moss=0.5, glow=0.4, seed=11))
        thorn_row(b, pts, [f"Tendril_{side}1", f"Tendril_{side}2"], thorn, 3, 0.035, G(sx, 0.6, 0.0), seed=3)
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.02)), knee], [0.062, 0.05, 0.038], sides=10,
               chain=[lg.names[0]], style=K3.bark(PAL, k, grain=18, splits=0.5, moss=0.5, glow=0.3, seed=20 + i))
        # A knotted burl on the knee with a thorn: the knees stand above the back (a stalker, not a tortoise).
        b.ellipsoid("Burl", knee, (0.045, 0.045, 0.045), bone=lg.names[0], segments=(16, 8),
                    deform=C.chiselled(8, 60 + i, depth=0.2), style=K3.bark(PAL, k, grain=40, splits=0.6, moss=0.6, seed=22))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("KneeThorn", [knee + Vector((0, 0, 0.02)), knee + K3.gp((0.03, 0.08, -0.03), sx),
                             knee + K3.gp((0.04, 0.13, -0.08), sx)], [0.014, 0.008, 0.001], sides=6,
               chain=[lg.names[0]], style=thorn)
        b.tube("LegLower", [knee, knee.lerp(foot, 0.5) + K3.gp((0.015, 0.0, 0.0), sx), foot + Vector((0, 0, 0.02))],
               [0.034, 0.028, 0.03], sides=10, chain=[lg.names[1]],
               style=K3.bark(PAL, k, grain=18, splits=0.4, moss=0.4, seed=24 + i))
        # Three root claws splayed over the ground (the Claws part puts a thorn point on each).
        for j, a in enumerate((-40, 0, 40)):
            d = Matrix.Rotation(math.radians(a + (25 if i == 1 else -10) * sx), 3, "Z") @ K3.gp((0.25, 0.0, 1.0), sx).normalized()
            base = foot + Vector((0, 0, 0.02))
            b.tube("RootClaw", [base, base + d * 0.05 + Vector((0, 0, -0.008)), base + d * 0.1 + Vector((0, 0, -0.02))],
                   [0.016, 0.011, 0.002], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.32, -0.15), G(0, 0.34, 0.22), "Root", True),
           ("Head", G(0, 0.27, 0.3), G(0, 0.28, 0.56), "Body", True)]
    for name, f, up, r in PODS:
        out.append((name, G(0, up - r * 1.0, f), G(0, up + r * 1.35, f), "Body", True))
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
STRIDE, LIFT, STANCE = 0.18, 0.06, 0.72
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
RIGID = {"Shingle", "Pod", "Mask", "BrowRidge", "Collar"}
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
                    macros=[("pods", G(0.0, 0.6, -0.05), 0.6, -50, 40), ("head", G(0.0, 0.3, 0.48), 0.45, -25, 12),
                            ("bark", G(0.15, 0.48, -0.1), 0.35, -70, 30)],
                    rig_scale=1.2, power=("Regrow", 18))
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("chitin", "membrane", "rock"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS, scale_bones=POD_NAMES, clip_scales=CLIP_SCALES,
                    extra=H.weathering(K, scratches=0.3, dust=0.35, seed=23), specular=0.3,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                            "camouflage": "two-value bark/moss albedo; fade albedo towards the ground tint and the emission down",
                            "regrow_pod_bones": POD_NAMES},
                    previews=previews)
    # Round 46: the close-up detail (eyes, thorn teeth, root-claw thorns) as extra parts, exported again with the body.
    import knitback_detail
    knitback_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
