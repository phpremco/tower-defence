"""Rotbloom: the continent-4 boss (Mirewood), fought at the Pioneer-1 wreck (round 16, see ArtSource/DESIGN_NOTES.md).

A wide low mound of rotting bark plates and moss on six short root legs, dragging a translucent slime skirt (bone
Trail: the hasting slime trail), wearing a giant five-petalled carrion flower on its back (bones Petal_F, Petal_FL/FR,
Petal_BL/BR; a stamen crown on bone Bloom round a lime pit-maw). Four sap pods on the flanks swell when it regrows
(bones Pod_L1/R1/L2/R2). At the front a low bark-skull head with a broad dripping shovel jaw, amber eyes and
dead-branch antlers.

Two materials (1024 + 512 Low each): `Rotbloom_Mat` (bark, moss, slime, pods) and `Rotbloom_Bloom_Mat` (petals,
stamens, throat). Clips: Crawl (loop), Idle (loop), Hit, Death, Regrow, Bloom. Also exports Rotbloom_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_rotbloom.py -- [--preview <dir>]
     [--norender] [--blockout]
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
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "Rotbloom"
LOD_NAME = NAME + "_LOD1"
K = 4.0
UNITY_SCALE = 1.0

# Bark brown and moss olive (close values, as the Knitback), lime rot glow, amber eyes.
PAL = H.Palette(top=(0.33, 0.26, 0.17), side=(0.14, 0.1, 0.07), belly=(0.05, 0.04, 0.03), seam=(0.03, 0.022, 0.015),
                dark=(0.08, 0.06, 0.04), glow=(0.72, 1.0, 0.18), eye=(1.0, 0.7, 0.2), membrane=(0.24, 0.27, 0.1),
                bone=(0.6, 0.55, 0.42))
SLIME = H.Palette(top=PAL.top, side=(0.14, 0.16, 0.06), belly=(0.05, 0.06, 0.02), seam=PAL.seam, dark=PAL.dark,
                  glow=(0.7, 1.0, 0.2), eye=PAL.eye, membrane=(0.4, 0.55, 0.12), bone=PAL.bone)
POD = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=(0.1, 0.12, 0.03),
                glow=(0.85, 1.0, 0.3), eye=PAL.eye, membrane=(0.45, 0.55, 0.15), bone=PAL.bone)
# The bloom: wine-dark backs, coral inner faces fading to a pale frilled rim, a lime throat.
PETAL = H.Palette(top=(0.36, 0.07, 0.12), side=(0.2, 0.035, 0.07), belly=(0.08, 0.015, 0.03), seam=(0.12, 0.02, 0.05),
                  dark=(0.1, 0.02, 0.04), glow=(1.0, 0.42, 0.24), eye=PAL.eye, membrane=(0.5, 0.1, 0.12),
                  bone=(0.95, 0.75, 0.6))
STAMEN = H.Palette(top=(0.3, 0.1, 0.12), side=(0.16, 0.05, 0.07), belly=(0.06, 0.02, 0.03), seam=PETAL.seam,
                   dark=PETAL.dark, glow=(0.78, 1.0, 0.25), eye=PAL.eye, membrane=PETAL.membrane, bone=(0.9, 0.95, 0.5))
THROAT = (0.75, 1.0, 0.25)

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

BODY_C = G(0, 0.92, -0.1)
BODY_R = (1.3, 1.55, 0.72)
BLOOM_C = G(0, 1.52, -0.25)
PETALS = [("Petal_F", 0.0), ("Petal_FR", 72.0), ("Petal_FL", -72.0), ("Petal_BR", 144.0), ("Petal_BL", -144.0)]
PODS = [("Pod_R1", (1.12, 1.0, 0.45)), ("Pod_L1", (-1.12, 1.0, 0.45)), ("Pod_R2", (1.08, 0.95, -0.75)),
        ("Pod_L2", (-1.08, 0.95, -0.75))]
POD_NAMES = [p[0] for p in PODS]
LEG_TABLE = [((0.85, 0.55, 0.85), (1.45, 0.0, 1.15), (0.42, 0.5)),
             ((0.95, 0.5, -0.1), (1.6, 0.0, -0.1), (0.42, 0.5)),
             ((0.85, 0.5, -0.95), (1.45, 0.0, -1.3), (0.42, 0.5))]
HEAD_C = G(0, 0.78, 1.62)
JAW_PIVOT = G(0, 0.62, 1.42)
SCALE_BONES = POD_NAMES + ["Bloom"]


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.5, 1.0, 0.0))


def petal_dir(ang):
    a = math.radians(ang)
    return G(math.sin(a), 0, math.cos(a))


def petal_axis(ang):
    """Hinge of a petal: a positive turn closes it (tip up), negative opens it flat."""
    return petal_dir(ang).cross(Z).normalized()


def petal_frame(ang):
    d = petal_dir(ang)
    side = Z.cross(d).normalized()
    m = Matrix((side * 0.5, d * 1.2, Z)).transposed().to_4x4()
    m.translation = BLOOM_C + d * 0.3
    return m.inverted()


def petal_point(ang, u, v, inner=False):
    d = petal_dir(ang)
    side = Z.cross(d).normalized()
    w = 0.5 * math.sin(math.pi * (0.12 + 0.8 * v)) + 0.06
    x = (2 * u - 1)
    r = 0.3 + 1.2 * v
    h = 0.62 * v - 0.42 * v ** 3 + 0.16 * x * x * math.sin(math.pi * v) + 0.03 * math.sin(9 * v + 3 * x) * v
    p = BLOOM_C + d * r + side * (x * w) + Z * h
    if inner:
        p = p - Z * (0.055 * (1 - 0.7 * v) + 0.012)
    return p


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def body_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Mound", BODY_C, BODY_R, bone="Body", segments=(56, 28), deform=C.chiselled(40, 1, 0.05, squash=(0.8, 1.0)),
                style=K3.bark(PAL, k * 0.6, grain=30, splits=0.9, moss=0.65, glow=0.55, seed=2))
    # Bark plates round the rim, overlapping like old shingles, moss on top.
    for i, (ang, u, sz) in enumerate(((20, 1.0, 0.55), (55, 0.95, 0.6), (95, 0.9, 0.62), (130, 0.92, 0.58),
                                      (165, 0.95, 0.5))):
        a = math.radians(ang)
        for side, sx in SIDES:
            c = BODY_C + G(math.sin(a) * BODY_R[0] * 0.92 * sx, u - 0.92, math.cos(a) * BODY_R[1] * 0.92)
            yaw = -math.degrees(a) * sx
            b.ellipsoid("BarkPlate", c, (sz, 0.32, 0.12), T.rot(roll=-55 * sx, yaw=yaw + 90 * sx), bone="Body",
                        segments=(28, 14), deform=C.chiselled(12, 10 + i, 0.1, squash=(0.8, 1.0)),
                        style=K3.bark(PAL, k * 0.5, grain=44, splits=1.0, moss=0.5, glow=0.4, seed=11 + i))
    # Slime skirt under the whole mound.
    b.ellipsoid("Skirt", G(0, 0.2, -0.2), (1.6, 1.95, 0.2), bone="Body", segments=(56, 20),
                style=K2.slime_foot(SLIME, k * 0.5, waves=8.0, edge_glow=1.0))
    b.ellipsoid("TrailLobe", G(0, 0.14, -1.95), (0.8, 0.7, 0.12), bone="Trail", segments=(32, 12),
                style=K2.slime_foot(SLIME, k * 0.5, waves=6.0, edge_glow=1.0))
    # Sap pods on the flanks.
    for name, p in PODS:
        b.ellipsoid("Pod", G(*p), (0.3, 0.3, 0.36), bone=name, segments=(28, 14), style=K3.sap_pod(POD, k * 0.5, glow=1.0,
                                                                                               seed=20))
        b.ellipsoid("PodCollar", G(*p) - G(0.1 * (1 if p[0] > 0 else -1), 0.12, 0), (0.24, 0.24, 0.12),
                    T.rot(roll=-50 * (1 if p[0] > 0 else -1)), bone="Body", segments=(20, 10),
                    style=K3.bark(PAL, k * 0.4, seed=21))
    # Head: a bark skull with a broad shovel jaw, amber eyes, dead-branch antlers.
    b.ellipsoid("Skull", HEAD_C, (0.55, 0.5, 0.42), T.rot(pitch=8), bone="Head", segments=(40, 20),
                deform=C.chiselled(18, 30, 0.07, squash=(0.8, 1.0)),
                style=K3.bark(PAL, k * 0.5, grain=36, splits=0.8, moss=0.35, glow=0.5, seed=31))
    b.ellipsoid("Jaw", G(0, 0.5, 1.98), (0.58, 0.42, 0.12), T.rot(pitch=-6), bone="Jaw", segments=(36, 18),
                deform=C.chiselled(14, 32, 0.06, squash=(0.7, 1.0)),
                style=K3.bark(PAL, k * 0.5, grain=40, splits=0.6, moss=0.2, glow=0.3, seed=33))
    b.ellipsoid("Maw", G(0, 0.62, 1.95), (0.42, 0.3, 0.12), bone="Head", segments=(28, 12), style=H.inner_glow(SLIME, 0.9))
    for side, sx in SIDES:
        for j, (x, u, f) in enumerate(((0.3, 0.95, 1.92), (0.42, 0.85, 1.86))):
            b.ellipsoid("Eye", G(x * sx, u, f), (0.06 - 0.012 * j,) * 2 + (0.045,), bone="Head", segments=(12, 8),
                        style=H.eye(PAL, k * 0.3, 1.0))
        base = G(0.3 * sx, 1.1, 1.5)
        main = [base, base + G(0.15 * sx, 0.35, -0.2), base + G(0.35 * sx, 0.6, -0.55), base + G(0.45 * sx, 0.72, -0.95)]
        b.tube("Antler", main, [0.08, 0.06, 0.04, 0.01], sides=10, chain=["Head"],
               style=K3.bark(PAL, k * 0.4, grain=30, splits=0.5, moss=0.3, seed=34))
        for fork, (d, l) in enumerate((((0.35, 0.4, 0.1), 0.35), ((0.1, 0.45, -0.25), 0.3))):
            root = main[fork + 1]
            b.tube("AntlerFork", [root, root + G(d[0] * sx, d[1], d[2]) * l, root + G(d[0] * sx * 1.6, d[1] * 1.3, d[2]) * l],
                   [0.04, 0.025, 0.006], sides=8, chain=["Head"], style=K3.bark(PAL, k * 0.4, seed=35 + fork))
        # Drooping slime strands from the jaw corners.
        root = G(0.45 * sx, 0.5, 2.0)
        b.tube("Drool", [root, root + G(0.02 * sx, -0.18, 0.04), root + G(0.0, -0.36, 0.06)], [0.04, 0.03, 0.035],
               sides=8, chain=["Jaw"], style=K2.slime_foot(SLIME, k * 0.3, waves=3.0, edge_glow=0.8))
    # Root legs with two root toes each.
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("RootLeg", [hip, knee, foot + Vector((0, 0, 0.08))], [0.17, 0.13, 0.1], sides=12, chain=list(lg.names),
               style=K3.bark(PAL, k * 0.4, grain=34, splits=0.6, moss=0.3, seed=40 + i))
        for t, d in enumerate((G(0.3 * sx, 0, 0.25), G(0.3 * sx, 0, -0.2))):
            r0 = foot + Vector((0, 0, 0.06))
            b.tube("Toe", [r0, r0 + d * 0.6 + Vector((0, 0, 0.02)), r0 + d - Vector((0, 0, 0.05))], [0.06, 0.04, 0.008],
                   sides=8, chain=[lg.names[1]], style=K3.bark(PAL, k * 0.4, seed=44 + t))
    return b


def bloom_parts():
    b = C.PartBuilder()
    k = K
    for name, ang in PETALS:
        b.slab("Petal", lambda u, v, a=ang: petal_point(a, u, v), lambda u, v, a=ang: petal_point(a, u, v, True), 14, 18,
               frame=petal_frame(ang), bone=name, style=T.leaf_petal(PETAL, k * 0.5, glow=1.0, veins=7, seed=50))
    # The pit-maw: a lime throat ringed by a lip, stamens rising round it.
    b.ellipsoid("Throat", BLOOM_C + G(0, 0.02, 0), (0.36, 0.36, 0.14), bone="Bloom", segments=(28, 14),
                style=T.solid_glow(THROAT, 1.0, 0.55))
    b.ellipsoid("Lip", BLOOM_C + G(0, 0.06, 0), (0.46, 0.46, 0.1), bone="Bloom", segments=(32, 12),
                style=T.leaf_petal(STAMEN, k * 0.4, glow=0.6, veins=10, seed=51))
    for j in range(9):
        a = math.radians(20 + 40 * j)
        d = G(math.sin(a), 0, math.cos(a))
        base = BLOOM_C + d * 0.24 + G(0, 0.08, 0)
        tip = base + d * 0.22 + G(0, 0.55 + 0.12 * (j % 2), 0)
        b.tube("Stamen", [base, base.lerp(tip, 0.5) + d * 0.04, tip], [0.035, 0.025, 0.02], sides=8, chain=["Bloom"],
               style=H.hook(STAMEN, k * 0.3, glow=0.8, glow_from=0.7))
        b.ellipsoid("Anther", tip, (0.06, 0.06, 0.07), bone="Bloom", segments=(12, 8), style=T.solid_glow(THROAT, 0.9, 0.6))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.3, 0), None, False),
           ("Body", G(0, 0.9, -1.0), G(0, 0.95, 0.8), "Root", True),
           ("Head", G(0, 0.85, 1.2), G(0, 0.8, 2.0), "Body", True),
           ("Jaw", JAW_PIVOT, G(0, 0.5, 2.2), "Head", True),
           ("Trail", G(0, 0.2, -1.4), G(0, 0.15, -2.4), "Body", True),
           ("Bloom", BLOOM_C - G(0, 0.15, 0), BLOOM_C + G(0, 0.5, 0), "Body", True)]
    for name, ang in PETALS:
        d = petal_dir(ang)
        out.append((name, BLOOM_C + d * 0.3, petal_point(ang, 0.5, 0.6), "Body", True))
    for name, p in PODS:
        c = G(*p)
        out.append((name, c - G(0.15 * (1 if p[0] > 0 else -1), 0.15, 0), c + G(0.15 * (1 if p[0] > 0 else -1), 0.15, 0),
                    "Body", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs(off=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, head_yaw=0.0, jaw=0.0, petals=0.0, bloom=1.0,
          bloom_lift=0.0, pods=1.0, trail=0.0):
    """Hinge specs (radians). `petals` > 0 closes the petals (tips up), < 0 opens them flat; a dict per petal works."""
    out = {"Root": {"rot": []},
           "Body": {"rot": [(Z, yaw), (Yv, roll), (X, -pitch)], "loc": off if off is not None else Vector()},
           "Head": {"rot": [(Z, head_yaw), (X, -head)]},
           "Jaw": {"rot": [(X, jaw)]},
           "Trail": {"rot": [(Z, trail)]},
           "Bloom": {"rot": [], "loc": G(0, bloom_lift, 0), "scale": bloom}}
    pt = petals if isinstance(petals, dict) else {n: petals for n, _ in PETALS}
    for name, ang in PETALS:
        out[name] = {"rot": [(petal_axis(ang), pt[name])]}
    pd = pods if isinstance(pods, dict) else {n: pods for n in POD_NAMES}
    for n in POD_NAMES:
        out[n] = {"rot": [], "scale": pd[n]}
    return out


CRAWL_FRAMES = 64
STRIDE, LIFT, STANCE = 0.5, 0.16, 0.7
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("L", 3): 0.0, ("R", 1): 0.5, ("L", 2): 0.5, ("R", 3): 0.5}


def planted(lg):
    return {key: l.foot.copy() for key, l in lg.items()}


def crawl_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(off=Vector((0, 0, 0.035 * math.cos(2 * w))), roll=math.radians(2.5) * math.sin(w),
                   yaw=math.radians(2) * math.sin(w), pitch=math.radians(1.5) * math.sin(2 * w),
                   head=math.radians(3) * math.sin(2 * w + 0.6), head_yaw=math.radians(4) * math.sin(w),
                   jaw=math.radians(4) + math.radians(3) * math.sin(2 * w),
                   petals={n: math.radians(4) * math.sin(2 * w - i * 0.9) for i, (n, _) in enumerate(PETALS)},
                   bloom=1.0 + 0.03 * math.sin(2 * w), trail=math.radians(6) * math.sin(w - 1.5))
        return K3.place_legs(poser, sp, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return CRAWL_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        breathe = math.sin(w)
        sp = specs(off=Vector((0, 0, 0.03 * breathe)), pitch=math.radians(1) * breathe,
                   head_yaw=math.radians(10) * math.sin(w + 0.7), head=math.radians(3) * math.sin(2 * w),
                   jaw=math.radians(5) + math.radians(8) * max(0.0, math.sin(2 * w + 1.0)) ** 2,
                   petals={n: -math.radians(5) * breathe + math.radians(2) * math.sin(3 * w - i) for i, (n, _) in
                           enumerate(PETALS)}, bloom=1.0 + 0.05 * breathe,
                   pods={n: 1.0 + 0.05 * math.sin(2 * w + i * 1.3) for i, n in enumerate(POD_NAMES)})
        return K3.place_legs(poser, sp, lg, planted(lg))
    return 96, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(off=Vector((0, 0.06 * k, -0.04 * k)), pitch=-math.radians(4) * k, roll=math.radians(3) * k,
                   head=-math.radians(10) * k, jaw=math.radians(4 + 16 * max(k, 0)), petals=math.radians(14) * k,
                   bloom=1.0 - 0.08 * k)
        return K3.place_legs(poser, sp, lg, planted(lg))
    return 16, spec, False


REGROW_FRAMES, REGROW_PEAK = 60, 28


def regrow_spec(poser):
    """The pods swell to 1.5 in a wave from front to back (peak at REGROW_PEAK), the body heaves, petals pulse."""
    lg = legs()

    def spec(t, f):
        heave = T.pulse(f, 4, REGROW_PEAK, 54)
        pods = {n: 1.0 + 0.5 * T.pulse(f, 4 + 4 * (i // 2), REGROW_PEAK + 4 * (i // 2), 56) for i, n in enumerate(POD_NAMES)}
        sp = specs(off=Vector((0, 0, 0.08 * heave)), pitch=math.radians(2) * heave, head=math.radians(6) * heave,
                   jaw=math.radians(4 + 10 * heave), petals=-math.radians(10) * heave, bloom=1.0 + 0.1 * heave, pods=pods)
        return K3.place_legs(poser, sp, lg, planted(lg))
    return REGROW_FRAMES, spec, False


BLOOM_FRAMES, BLOOM_BURST = 72, 34


def bloom_spec(poser):
    """The petals open flat (frames 6-26), the stamen crown rises and swells, the spore burst leaves the throat at
    BLOOM_BURST, the flower closes back to rest by the end."""
    lg = legs()

    def spec(t, f):
        open_ = T.ease(6, 26, f) * (1 - T.ease(52, 70, f))
        burst = T.pulse(f, BLOOM_BURST - 4, BLOOM_BURST, BLOOM_BURST + 12)
        sp = specs(off=Vector((0, 0, -0.06 * open_)), pitch=-math.radians(2) * open_,
                   head=-math.radians(8) * open_, jaw=math.radians(4 + 22 * burst),
                   petals={n: -math.radians(34) * open_ - math.radians(6) * burst for n, _ in PETALS},
                   bloom=1.0 + 0.3 * open_ + 0.15 * burst, bloom_lift=0.12 * open_)
        return K3.place_legs(poser, sp, lg, planted(lg))
    return BLOOM_FRAMES, spec, False


DEATH_FRAMES = 84


def death_spec(poser):
    """The bloom wilts shut (petals close, then droop), the stamens shrink, the mound sinks onto its splayed roots."""
    lg = legs()

    def spec(t, f):
        sink = T.ease(10, 56, f)
        wilt = T.ease(4, 40, f)
        droop = T.ease(40, 80, f)
        jerk = T.pulse(f, 0, 6, 14)
        sp = specs(off=Vector((0, 0.05 * jerk, -0.42 * sink)), roll=math.radians(6) * sink, pitch=-math.radians(4) * sink,
                   head=-math.radians(16) * sink, jaw=math.radians(4 + 20 * T.pulse(f, 2, 10, 30) + 14 * sink),
                   petals={n: math.radians(40) * wilt - math.radians(70) * droop for n, _ in PETALS},
                   bloom=1.0 - 0.45 * wilt, bloom_lift=-0.12 * wilt, pods=1.0 - 0.35 * sink)
        return K3.splay_feet(poser, sp, lg, sink, twitch=math.sin(f * 1.1) * (1 - sink), out=1.2)
    return DEATH_FRAMES, spec, False


CLIPS = {"Crawl": crawl_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Regrow": regrow_spec,
         "Bloom": bloom_spec}
BODY_RIGID = {"BarkPlate", "Skull", "Jaw", "Pod", "Antler", "AntlerFork", "Eye", "Maw"}
BLOOM_RIGID = {"Petal", "Throat", "Lip", "Stamen", "Anther"}
LOD_DROP = {"Jaw": "Head", "Trail": "Body"}


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("rotbloom", [(body_parts(), (0.22, 0.18, 0.1), None), (bloom_parts(), (0.6, 0.15, 0.2), (1.0, 0.4, 0.25))],
                   bones(), CLIPS, out, (0, 0, 1.0), 12.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Crawl", 8), ("top", "top", "Crawl", 8),
                    ("bloom", "close", "Bloom", BLOOM_BURST), ("regrow", "close", "Regrow", REGROW_PEAK),
                    ("dead", "close", "Death", DEATH_FRAMES)],
                   [("Bloom", "close", range(0, BLOOM_FRAMES, 9)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12))],
                   scale_bones=SCALE_BONES)
        return
    C.reset_scene()
    mats = [T.Mat(body_parts(), 17000, ("chitin", "membrane", "rock"), voxel=0.012, ao=0.35, k=K, emission=2.4,
                  specular=0.3, extra=H.weathering(K, scratches=0.2, dust=0.15, seed=181), rigid=BODY_RIGID, blend=2,
                  lod_share=0.7),
            T.Mat(bloom_parts(), 7000, ("membrane", "chitin"), suffix="Bloom", voxel=0.009, ao=0.25, extrusion=0.04,
                  ray=0.12, k=K * 0.6, emission=2.4, specular=0.3, rigid=BLOOM_RIGID, blend=0, smooth=3, lod_share=0.3)]
    report = {"crawl_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, under the mound",
              "trail_origin": "bone Trail (the rear slime lobe, on the ground)",
              "regrow": {"pod_peak_frame": REGROW_PEAK, "pods": POD_NAMES},
              "bloom": {"spore_burst_frame": BLOOM_BURST, "socket": "bone Bloom (throat centre)",
                        "petals_open_frames": [6, 26]},
              "footfalls": "Crawl frames 0 (L1, R2, L3) and 32 (R1, L2, R3)"}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=LOD_DROP, scale_bones=SCALE_BONES,
        report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("rotbloom", arm, mesh, actions, lod, out, assets, (0, 0, 1.0), 12.0, ("Crawl", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Crawl", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Crawl", 12), ("back", "back", "Idle", 40), ("bloom", "close", "Bloom", BLOOM_BURST),
                      ("regrow", "close", "Regrow", REGROW_PEAK), ("dead", "close", "Death", DEATH_FRAMES)],
               strips=[("Crawl", "side", range(0, CRAWL_FRAMES, 8)), ("Bloom", "close", range(0, BLOOM_FRAMES + 1, 9)),
                       ("Regrow", "close", range(0, REGROW_FRAMES + 1, 8)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12))],
               macros=[("bloom", G(0, 1.7, -0.2), 2.6, -30, 40, "Bloom", BLOOM_BURST),
                       ("head", G(0, 0.8, 1.8), 2.2, -30, 12, "Idle", 20), ("pod", G(1.1, 1.0, 0.45), 1.4, -70, 20, "Regrow", 28)])


if __name__ == "__main__":
    main()
