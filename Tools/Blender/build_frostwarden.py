"""Frost Warden: the continent-3 boss (Northern Crown), "the guardian" (round 16, see ArtSource/DESIGN_NOTES.md).

The only upright boss: four column legs under a skirt of hanging plates, a tall torso with a big chest plate carrying
the seal glyph (the first hint of the seal under The Core), glyph-marked shoulder plates, a hooded crest over a slit
face ringed with amber eye-pits, a broken halo of ice prisms behind the head (bone Halo) and two arms whose forearms
grow into long ice glaives, held point-down in front like a guard at a door.

Two materials (1024 + 512 Low each): `FrostWarden_Mat` (slate stone-chitin, amber-gold glyphs) and
`FrostWarden_Ice_Mat` (halo, glaives, shoulder and back prisms; ice with warm roots). Clips: Walk (loop),
WalkEnraged (loop, hunched and faster), Idle (loop), Hit, Death, FreezePulse, Enrage. Also exports FrostWarden_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_frostwarden.py -- [--preview <dir>]
     [--norender] [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "FrostWarden"
LOD_NAME = NAME + "_LOD1"
K = 4.0
UNITY_SCALE = 1.0

# Dark slate violet stone-chitin (Shardback kin, older and paler on the worn rims), amber-gold glyph light.
PAL = H.Palette(top=(0.4, 0.37, 0.47), side=(0.16, 0.14, 0.21), belly=(0.05, 0.045, 0.07), seam=(0.03, 0.025, 0.045),
                dark=(0.09, 0.08, 0.12), glow=(1.0, 0.62, 0.2), eye=(1.0, 0.76, 0.36), membrane=(0.4, 0.2, 0.12),
                bone=(0.78, 0.8, 0.86))
RUNE = (1.0, 0.7, 0.28)
RUNE_LINE = (0.95, 0.66, 0.3)
ICE = dict(root=(0.14, 0.2, 0.38), body=(0.55, 0.72, 0.92), tip=(0.92, 0.97, 1.0), ice_glow=(0.5, 0.75, 1.0),
           warm=(1.0, 0.6, 0.2))

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

LEG_TABLE = [((0.45, 1.15, 0.4), (1.2, 0.0, 1.05), (0.9, 1.05)),
             ((0.48, 1.02, -0.85), (1.3, 0.0, -1.5), (0.88, 1.0))]
# The armoured train behind the hips (the mantis-centaur's abdomen): nodes (fwd, up) and half widths.
TRAIN = [(-0.15, 1.2), (-1.05, 1.0), (-1.95, 0.62), (-2.6, 0.3)]
TRAIN_W = [0.55, 0.5, 0.36]
SHOULDER = (0.66, 2.62, 0.05)
ELBOW = (0.9, 1.98, 0.32)
WRIST = (0.58, 1.72, 0.82)
BLADE_TIP = (0.78, 0.18, 1.55)
HALO_C = G(0, 3.35, -0.42)
HALO_R = 0.95
HALO_N = 11
CHEST_C = G(0, 2.42, 0.46)


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.3, 0.4, 1.0))


def arm_pts(sx):
    return T.gp(SHOULDER, sx), T.gp(ELBOW, sx), T.gp(WRIST, sx), T.gp(BLADE_TIP, sx)


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def body_parts():
    b = C.PartBuilder()
    k = K
    # Pelvis and the skirt of hanging plates (the robe read).
    b.ellipsoid("Pelvis", G(0, 1.32, 0.0), (0.56, 0.5, 0.36), bone="Hips", segments=(40, 20),
                deform=C.chiselled(20, 1, 0.06, squash=(0.8, 1.0)),
                style=S.slab(PAL, k * 0.6, strata=26, veins=0.4, fault=0.5, seed=2))
    for i, (x, f, yaw) in enumerate(((0.0, 0.42, 0), (0.42, 0.3, 40), (0.58, -0.05, 90), (0.38, -0.38, 140),
                                     (0.0, -0.46, 180))):
        for side, sx in ([(None, 1)] if x == 0 else SIDES):
            c = G(x * sx, 1.05, f)
            b.ellipsoid("Tasset", c, (0.22, 0.06, 0.34), T.rot(yaw=yaw * sx, pitch=-12), bone="Hips", segments=(24, 12),
                        deform=C.chiselled(10, 10 + i, 0.08, squash=(0.8, 1.0)),
                        style=K2.armour_plate(PAL, k * 0.5, rim_glow=0.35, chevron=0.0, lip=0.8, seed=11 + i))
    # The train: two armoured abdomen segments with shingled plates, chevrons pointing forward.
    for i in range(3):
        (f0, u0), (f1, u1) = TRAIN[i], TRAIN[i + 1]
        front, back = G(0, u0, f0), G(0, u1, f1)
        c = front.lerp(back, 0.5)
        bone = ("Hips", "Abdomen_1", "Abdomen_2")[i]
        b.ellipsoid("TrainCore", c - G(0, 0.12, 0), (TRAIN_W[i] * 0.8, (front - back).length * 0.62, 0.26),
                    T.forward_frame(front, back), bone=bone, segments=(32, 16), style=H.glow_flesh(PAL, k * 0.4, glow=0.45))
        b.ellipsoid("TrainPlate", c + G(0, 0.12, 0.08), (TRAIN_W[i], (front - back).length * 0.6, 0.16),
                    T.forward_frame(front, back), bone=bone, segments=(36, 18),
                    deform=C.chiselled(16, 80 + i, 0.06, squash=(0.7, 1.0)),
                    style=K2.armour_plate(PAL, k * 0.5, rim_glow=0.8 - 0.15 * i, chevron=0.9 - 0.2 * i, lip=0.8, cells=8,
                                          seed=81 + i))
        for side, sx in SIDES:
            b.ellipsoid("TrainFlank", c + G(TRAIN_W[i] * 0.75 * sx, -0.05, 0.0), (0.18, (front - back).length * 0.5, 0.1),
                        T.forward_frame(front, back) @ T.rot(roll=-50 * sx), bone=bone, segments=(24, 12),
                        deform=C.chiselled(10, 85 + i, 0.08),
                        style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.6, chevron=0.0, lip=0.7, seed=86 + i))
    # Torso, chest plate (the seal glyph is engraved by an extra pass), ribs on the flanks.
    b.ellipsoid("Torso", G(0, 2.25, 0.04), (0.48, 0.36, 0.62), bone="Spine", segments=(40, 20),
                style=H.glow_flesh(PAL, k * 0.4, glow=0.5, folds=10.0))
    b.parts[-1].chain = ["Spine", "Chest"]
    b.ellipsoid("ChestPlate", CHEST_C - G(0, 0, 0.1), (0.44, 0.13, 0.52), T.rot(pitch=-6), bone="Chest", segments=(40, 20),
                deform=C.chiselled(16, 20, 0.04, squash=(0.9, 1.0)),
                style=S.slab(PAL, k * 0.5, strata=20, veins=0.0, fault=0.0, seed=21))
    for i, u in enumerate((1.75, 1.95, 2.15)):
        for side, sx in SIDES:
            b.ellipsoid("Rib", G(0.4 * sx, u, 0.12), (0.16, 0.26, 0.05), T.rot(roll=-60 * sx, yaw=20 * sx), bone="Spine",
                        segments=(20, 10), style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.6, chevron=0.0, lip=0.7,
                                                                  seed=30 + i))
    b.ellipsoid("Back", G(0, 2.45, -0.3), (0.46, 0.22, 0.55), T.rot(pitch=10), bone="Chest", segments=(36, 18),
                deform=C.chiselled(14, 34, 0.06, squash=(0.8, 1.0)),
                style=K2.armour_plate(PAL, k * 0.5, rim_glow=0.5, chevron=0.0, lip=0.6, cells=8, seed=35))
    for side, sx in SIDES:
        b.ellipsoid("Pauldron", G(0.66 * sx, 2.86, 0.04), (0.34, 0.36, 0.17), T.rot(roll=-28 * sx, pitch=-6),
                    bone="Chest", segments=(36, 18), deform=C.chiselled(16, 40, 0.06, squash=(0.8, 1.0)),
                    style=S.slab(PAL, k * 0.5, strata=18, veins=0.0, fault=0.4, seed=41))
    # Neck, hood and face.
    b.tube("Neck", [G(0, 2.85, 0.08), G(0, 3.0, 0.12), G(0, 3.12, 0.16)], [0.2, 0.17, 0.16], sides=14, chain=["Neck"],
           style=S.pillar(PAL, k * 0.4, drums=3, seed=50, glow=0.5))
    b.ellipsoid("Hood", G(0, 3.38, 0.06), (0.3, 0.46, 0.5), T.rot(pitch=24), bone="Head", segments=(40, 20),
                deform=C.chiselled(18, 51, 0.06, squash=(0.85, 1.0)),
                style=K2.armour_plate(PAL, k * 0.5, rim_glow=0.7, chevron=1.0, lip=0.9, seed=52))
    b.ellipsoid("Crest", G(0, 3.72, -0.12), (0.08, 0.44, 0.3), T.rot(pitch=40), bone="Head", segments=(28, 14),
                deform=C.chiselled(10, 53, 0.08), style=S.slab(PAL, k * 0.5, strata=24, veins=0.3, fault=0.3, seed=54))
    b.ellipsoid("Face", G(0, 3.2, 0.3), (0.19, 0.12, 0.26), T.rot(pitch=-8), bone="Head", segments=(32, 16),
                style=S.slab(PAL, k * 0.4, strata=30, veins=0.0, fault=0.0, seed=55))
    b.ellipsoid("Slit", G(0, 3.17, 0.41), (0.025, 0.03, 0.15), T.rot(pitch=-8), bone="Head", segments=(12, 10),
                style=H.inner_glow(PAL, 1.0))
    for j in range(6):
        a = math.radians(30 + 60 * j)
        p = G(0.13 * math.sin(a), 3.2 + 0.17 * math.cos(a), 0.38 - 0.03 * abs(math.cos(a)))
        b.ellipsoid("Eye", p, (0.032, 0.032, 0.022), bone="Head", segments=(10, 6), style=H.eye(PAL, k * 0.3, 1.0))
    # Arms (the forearms become the glaives, which are ice: see ice_parts).
    for side, sx in SIDES:
        sh, el, wr, tip = arm_pts(sx)
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5), el], [0.15, 0.13, 0.11], sides=14, chain=[f"Arm_{side}_Upper"],
               style=H.leg(PAL, k * 0.4, "Femur", spines=2))
        b.tube("Forearm", [el, el.lerp(wr, 0.5), wr], [0.11, 0.12, 0.1], sides=14, chain=[f"Arm_{side}_Fore"],
               style=H.leg(PAL, k * 0.4, "Femur", spines=3))
        b.ellipsoid("Vambrace", el.lerp(wr, 0.55) + G(0.06 * sx, 0.06, 0), (0.15, 0.3, 0.12),
                    T.frame_from(wr - el) @ T.rot(), bone=f"Arm_{side}_Fore", segments=(24, 12),
                    deform=C.chiselled(10, 60, 0.08), style=S.slab(PAL, k * 0.4, strata=20, veins=0.0, seed=61))
    # Column legs.
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5), knee], [0.2, 0.18, 0.15], sides=14, chain=[lg.names[0]],
               style=S.pillar(PAL, k * 0.5, drums=2, seed=70 + i, glow=0.3))
        b.ellipsoid("KneeCap", knee + (knee - hip.lerp(foot, 0.5)).normalized() * 0.06, (0.17, 0.17, 0.15), bone=lg.names[0],
                    segments=(20, 10), deform=C.chiselled(10, 72, 0.08),
                    style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.4, chevron=0.0, lip=0.8, seed=73))
        b.tube("Shin", [knee, knee.lerp(foot, 0.5), foot + Vector((0, 0, 0.12))], [0.15, 0.17, 0.2], sides=14,
               chain=[lg.names[1]], style=S.pillar(PAL, k * 0.5, drums=4, seed=74 + i, glow=0.25))
        b.tube("Foot", [foot + Vector((0, 0, 0.16)), foot + Vector((0, 0, 0.04)), foot], [0.21, 0.27, 0.22], sides=14,
               chain=[lg.names[1]], style=S.pillar(PAL, k * 0.5, drums=2, seed=76, glow=0.2), cap=0.3)
    return b


def ice_parts():
    b = C.PartBuilder()
    k = K
    # The broken halo: prisms pointing outwards on a circle behind the head, with a gap at the bottom.
    for j in range(HALO_N):
        a = math.radians(-150 + 300 * j / (HALO_N - 1))
        d = G(math.sin(a), math.cos(a), 0.0)
        base = HALO_C + d * HALO_R * 0.78
        length = 0.42 + 0.28 * math.cos(a) ** 2 + (0.12 if j % 2 == 0 else 0.0)
        b.shard("HaloPrism", base, (d + G(0, 0, -0.15)).normalized(), 0.07 + 0.02 * math.cos(a) ** 2, length, sides=6,
                flute=0.3, twist=0.6, bone="Halo", style=K3.ice_prism(k * 0.5, seed=80 + j, **ICE))
    for lo, hi in ((-150, -8), (8, 150)):
        pts = [HALO_C + G(math.sin(math.radians(a)), math.cos(math.radians(a)), 0.0) * HALO_R * 0.74
               for a in range(lo, hi + 1, 10)]
        b.tube("HaloArc", pts, [0.03, 0.06, 0.06, 0.06, 0.03], sides=8, samples=3, chain=["Halo"], flatten=0.6,
               style=K3.ice_prism(k * 0.5, seed=90, **ICE))
    # Glaives: grown from the wrists, long flat blades of ice.
    for side, sx in SIDES:
        sh, el, wr, tip = arm_pts(sx)
        mid = wr.lerp(tip, 0.45) + G(0.05 * sx, 0.06, 0.0)
        b.tube("Glaive", [wr - (tip - wr).normalized() * 0.1, mid, tip], [0.13, 0.12, 0.006], sides=10,
               chain=[f"Arm_{side}_Blade"], flatten=0.28, style=K3.ice_prism(k * 0.5, seed=100, **ICE))
        b.shard("GlaiveSpur", wr + G(0.04 * sx, 0.08, -0.05), G(0.4 * sx, 1.0, -0.5), 0.05, 0.42, sides=6, flute=0.3,
                bone=f"Arm_{side}_Blade", style=K3.ice_prism(k * 0.5, seed=101, **ICE))
        # Shoulder clusters.
        for j, (d, l, r) in enumerate((((0.4, 1.0, -0.2), 0.62, 0.08), ((0.9, 0.6, 0.1), 0.42, 0.06),
                                       ((0.3, 0.9, -0.7), 0.46, 0.06))):
            b.shard("ShoulderIce", G(0.72 * sx, 3.0, 0.0), G(d[0] * sx, d[1], d[2]), r, l, sides=6, flute=0.3,
                    twist=0.6 * sx, bone="Chest", style=K3.ice_prism(k * 0.5, seed=110 + j, **ICE))
    # Back ridge clusters between the shoulder blades.
    for j, (u, f, l) in enumerate(((2.75, -0.42, 0.55), (2.35, -0.48, 0.4))):
        for side, sx in SIDES:
            b.shard("BackIce", G(0.14 * sx, u, f), G(0.35 * sx, 0.8, -0.6), 0.06, l, sides=6, flute=0.3, twist=0.5 * sx,
                    bone="Chest", style=K3.ice_prism(k * 0.5, seed=120 + j, **ICE))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.4, 0), None, False),
           ("Hips", G(0, 1.25, -0.3), G(0, 1.3, 0.3), "Root", True),
           ("Spine", G(0, 1.4, 0.0), G(0, 2.15, 0.04), "Hips", True),
           ("Chest", G(0, 2.15, 0.04), G(0, 2.88, 0.08), "Spine", True),
           ("Neck", G(0, 2.88, 0.08), G(0, 3.12, 0.16), "Chest", True),
           ("Head", G(0, 3.12, 0.16), G(0, 3.5, 0.32), "Neck", True),
           ("Halo", HALO_C - G(0, 0.3, 0), HALO_C + G(0, 0.3, 0), "Chest", True),
           ("Abdomen_1", G(0, TRAIN[1][1], TRAIN[1][0]), G(0, TRAIN[2][1], TRAIN[2][0]), "Hips", True),
           ("Abdomen_2", G(0, TRAIN[2][1], TRAIN[2][0]), G(0, TRAIN[3][1], TRAIN[3][0]), "Abdomen_1", True)]
    for (key, lg) in legs().items():
        out += lg.rest_bones("Hips")
    for side, sx in SIDES:
        sh, el, wr, tip = arm_pts(sx)
        out += [(f"Arm_{side}_Upper", sh, el, "Chest", True), (f"Arm_{side}_Fore", el, wr, f"Arm_{side}_Upper", True),
                (f"Arm_{side}_Blade", wr, tip, f"Arm_{side}_Fore", True)]
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs(off=None, lean=0.0, roll=0.0, yaw=0.0, chest=0.0, chest_yaw=0.0, neck=0.0, head=0.0, head_yaw=0.0,
          halo=1.0, halo_spin=0.0, arm_fwd=0.0, arm_out=0.0, elbow=0.0, blade=0.0, arms=None, tail=0.0,
          tail_lift=0.0):
    """Hinge specs (radians). `lean` > 0 bends the torso forward; `arm_fwd` > 0 swings the arms forward and up;
    `elbow` > 0 bends the elbows up; `blade` > 0 lifts the glaive point. `arms` overrides per side:
    {side: (fwd, out, elbow, blade)}."""
    out = {"Root": {"rot": []},
           "Hips": {"rot": [(Z, yaw), (Yv, roll)], "loc": off if off is not None else Vector()},
           "Spine": {"rot": [(X, lean * 0.5)]},
           "Chest": {"rot": [(Z, chest_yaw), (X, lean * 0.5 + chest)]},
           "Neck": {"rot": [(Z, head_yaw * 0.5), (X, neck)]},
           "Head": {"rot": [(Z, head_yaw * 0.5), (X, head)]},
           "Halo": {"rot": [(Yv, halo_spin)], "scale": halo},
           "Abdomen_1": {"rot": [(Z, -tail * 0.6), (X, tail_lift)]},
           "Abdomen_2": {"rot": [(Z, -tail), (X, tail_lift)]}}
    for side, sx in SIDES:
        f, o, e, bl = arms[side] if arms else (arm_fwd, arm_out, elbow, blade)
        out[f"Arm_{side}_Upper"] = {"rot": [(Yv, -o * sx), (X, -f)]}
        out[f"Arm_{side}_Fore"] = {"rot": [(X, -e)]}
        out[f"Arm_{side}_Blade"] = {"rot": [(X, -bl)]}
    return out


WALK_FRAMES, RAGE_FRAMES = 64, 40
STRIDE, LIFT, STANCE = 0.7, 0.22, 0.68
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}
HUNCH = dict(lean=math.radians(28), neck=-math.radians(8), head=-math.radians(14), halo=1.18)


def walk_spec(poser, rage=False):
    lg = legs()
    frames = RAGE_FRAMES if rage else WALK_FRAMES

    def spec(t, f):
        w = 2 * math.pi * t
        h = 1.0 if rage else 0.0
        swing = math.sin(w)
        sp = specs(off=Vector((0, 0, 0.035 * math.cos(2 * w) - 0.12 * h)), roll=math.radians(2) * swing,
                   yaw=math.radians(2) * swing, lean=HUNCH["lean"] * h + math.radians(1.5) * math.sin(2 * w),
                   chest_yaw=-math.radians(3 + 3 * h) * swing, neck=HUNCH["neck"] * h, head=HUNCH["head"] * h,
                   head_yaw=math.radians(2) * swing, halo=1.0 + 0.18 * h, halo_spin=math.radians(4) * swing,
                   tail=math.radians(5 + 3 * h) * math.sin(w - 1.0), tail_lift=math.radians(2) * math.sin(2 * w - 1.0),
                   arms={s: ((math.radians(6 + 18 * h) - math.radians(8 + 6 * h) * swing * sx),
                             math.radians(4 + 26 * h), math.radians(4 * h), math.radians(10 + 30 * h))
                         for s, sx in SIDES})
        return K3.place_legs(poser, sp, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE * (1.3 if rage else 1.0),
                                                         LIFT * (1.3 if rage else 1.0)), body="Hips")
    return frames, spec, True


def walk_rage_spec(poser):
    return walk_spec(poser, rage=True)


def planted(lg):
    return {key: l.foot.copy() for key, l in lg.items()}


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(off=Vector((0, 0, 0.02 * math.sin(w))), lean=math.radians(1.5) * math.sin(w),
                   head_yaw=math.radians(20) * math.sin(w + 0.5), neck=math.radians(3) * math.sin(2 * w),
                   halo=1.0 + 0.03 * math.sin(2 * w), halo_spin=math.radians(8) * math.sin(w),
                   arm_fwd=math.radians(2) * math.sin(w), blade=math.radians(3) * math.sin(w + 1.0),
                   tail=math.radians(3) * math.sin(w - 1.2))
        return K3.place_legs(poser, sp, lg, planted(lg), body="Hips")
    return 120, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(off=Vector((0, 0.06 * k, 0.0)), lean=-math.radians(8) * k, roll=math.radians(3) * k,
                   neck=-math.radians(10) * k, head=-math.radians(6) * k, halo=1.0 + 0.08 * max(k, 0),
                   arm_fwd=math.radians(10) * k, arm_out=math.radians(8) * k)
        return K3.place_legs(poser, sp, lg, planted(lg), body="Hips")
    return 16, spec, False


PULSE_FRAMES, PULSE_IMPACT = 64, 30


def freeze_spec(poser):
    """Both glaives rise over the head (to frame 20), drive down into the ground in front at PULSE_IMPACT (the frost
    ring starts there), the halo flares to 1.4 and settles."""
    lg = legs()

    def spec(t, f):
        rise = T.ease(2, 20, f) * (1 - T.ease(23, PULSE_IMPACT, f))
        slam = T.ease(23, PULSE_IMPACT, f) * (1 - T.ease(46, 62, f))
        flare = T.pulse(f, 20, PULSE_IMPACT + 2, 58)
        sp = specs(off=Vector((0, 0, 0.06 * rise - 0.22 * slam)), lean=-math.radians(10) * rise + math.radians(24) * slam,
                   neck=-math.radians(10) * rise + math.radians(6) * slam, head=-math.radians(8) * rise,
                   halo=1.0 + 0.4 * flare, halo_spin=math.radians(30) * flare,
                   arm_fwd=math.radians(150) * rise + math.radians(30) * slam, arm_out=math.radians(10) * rise,
                   elbow=math.radians(30) * rise, blade=math.radians(60) * rise - math.radians(30) * slam)
        return K3.place_legs(poser, sp, lg, planted(lg), body="Hips")
    return PULSE_FRAMES, spec, False


ENRAGE_FRAMES = 48


def enrage_spec(poser):
    """From the upright guard into the hunch of WalkEnraged: the glaives sweep out wide (frame 18), the halo flares,
    a shudder, then it settles into the hunched carriage that WalkEnraged loops."""
    lg = legs()

    def spec(t, f):
        h = T.ease(4, 22, f)
        sweep = T.pulse(f, 6, 18, 36)
        shudder = math.sin(f * 2.2) * T.pulse(f, 14, 24, 36)
        sp = specs(off=Vector((0, 0, -0.12 * h)), lean=HUNCH["lean"] * h + math.radians(2) * shudder,
                   neck=HUNCH["neck"] * h, head=HUNCH["head"] * h - math.radians(10) * sweep,
                   halo=1.0 + 0.18 * h + 0.25 * sweep, halo_spin=math.radians(4) * shudder,
                   arm_fwd=math.radians(18) * h + math.radians(30) * sweep, arm_out=math.radians(26) * h + math.radians(40) * sweep,
                   elbow=math.radians(4) * h, blade=math.radians(40) * h + math.radians(20) * sweep)
        return K3.place_legs(poser, sp, lg, planted(lg), body="Hips")
    return ENRAGE_FRAMES, spec, False


DEATH_FRAMES = 84


def death_spec(poser):
    """Kneels: the front legs fold, the body sinks, the torso bows and the glaives are planted in front; the halo
    shrinks and dims (scale 0.7)."""
    lg = legs()

    def spec(t, f):
        sink = T.ease(8, 50, f)
        bow = T.ease(20, 70, f)
        jerk = T.pulse(f, 0, 6, 14)
        sp = specs(off=Vector((0, 0.05 * jerk, -0.75 * sink)), lean=math.radians(28) * bow - math.radians(6) * jerk,
                   neck=math.radians(18) * bow, head=math.radians(22) * bow, halo=1.0 - 0.3 * bow,
                   halo_spin=-math.radians(12) * bow, arm_fwd=math.radians(22) * sink, arm_out=math.radians(6) * sink,
                   elbow=math.radians(10) * sink, blade=-math.radians(12) * sink)
        pose = poser.solve(sp)
        move = pose["Hips"] @ poser.rest["Hips"].inverted()
        feet = planted(lg)
        for (side, i), l in lg.items():
            if i == 1:
                knee_down = (move @ l.root).lerp(l.foot, 0.4)
                feet[(side, i)] = l.foot.lerp(Vector((knee_down.x, knee_down.y + 0.25, 0.0)), sink)
        return K3.place_legs(poser, sp, lg, feet, body="Hips")
    return DEATH_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "WalkEnraged": walk_rage_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec,
         "FreezePulse": freeze_spec, "Enrage": enrage_spec}
BODY_RIGID = {"TrainPlate", "TrainFlank", "Tasset", "ChestPlate", "Rib", "Back", "Pauldron", "Hood", "Crest", "Face", "Vambrace", "KneeCap"}
ICE_RIGID = {"HaloPrism", "HaloArc", "Glaive", "GlaiveSpur", "ShoulderIce", "BackIce"}
LOD_DROP = {"Neck": "Chest"}
LOD_AIMS = {}


def seal_extra():
    """The ancient markings: the glyph on the chest plate, the right shoulder plate (the bake mirrors it to the
    left), and a small one on the hood's brow."""
    ch = T.seal_rune(CHEST_C + G(0, 0, 0.04), G(0, 0, 1) - G(0, 0, 0), Z, 0.4, RUNE, glow=0.85, depth=0.012,
                     line_col=RUNE_LINE, facing=0.35, thickness=0.25, wear=0.6, seed=3)
    sh_n = (G(0.5, 1.0, 0.0)).normalized()
    sh = T.seal_rune(G(0.72, 3.0, 0.04), sh_n, G(0, 0, 1), 0.22, RUNE, glow=0.9, depth=0.008, line_col=RUNE_LINE,
                     facing=0.3, thickness=0.18, line=1.3, wear=0.5, seed=5)
    brow = T.seal_rune(G(0, 3.46, 0.3), G(0, 0.4, 1.0).normalized(), Z, 0.13, RUNE, glow=0.9, depth=0.006,
                       line_col=RUNE_LINE, facing=0.3, thickness=0.12, line=1.5)
    return T.B.chain_extras(H.weathering(K, scratches=0.5, dust=0.25, seed=171, markings=PAL.dark, marking_amount=0.2),
                            ch, sh, brow)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("warden", [(body_parts(), (0.22, 0.2, 0.28), None), (ice_parts(), (0.6, 0.75, 0.95), (0.5, 0.7, 1.0))],
                   bones(), CLIPS, out, (0, -0.4, 1.9), 15.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("top", "top", "Walk", 8),
                    ("pulse", "close", "FreezePulse", PULSE_IMPACT), ("rage", "close", "WalkEnraged", 8),
                    ("dead", "close", "Death", DEATH_FRAMES)],
                   [("FreezePulse", "side", range(0, PULSE_FRAMES, 8)), ("Death", "side", range(0, DEATH_FRAMES + 1, 12))],
                   scale_bones=["Halo"])
        return
    C.reset_scene()
    mats = [T.Mat(body_parts(), 17500, ("rock", "chitin", "membrane"), voxel=0.011, ao=0.35, k=K, emission=2.6,
                  specular=0.3, extra=seal_extra(), rigid=BODY_RIGID, blend=2, lod_share=0.72),
            T.Mat(ice_parts(), 6500, ("crystal", "rock"), suffix="Ice", voxel=0.008, ao=0.2, extrusion=0.04, ray=0.12,
                  k=K * 0.6, emission=2.2, specular=0.45, rigid=ICE_RIGID, blend=0, smooth=2, lod_share=0.28)]
    report = {"walk_distance_per_loop": round(STRIDE / STANCE, 3),
              "walk_enraged_distance_per_loop": round(STRIDE * 1.3 / STANCE, 3), "pivot": "ground, under the hips",
              "freeze_pulse": {"impact_frame": PULSE_IMPACT, "frost_ring_origin": "between the glaive tips, ~1.5 ahead",
                               "halo_peak_frame": PULSE_IMPACT + 2},
              "enrage": {"frames": ENRAGE_FRAMES, "then": "loop WalkEnraged (hunched, 1.3x stride) at a higher speed"},
              "glyph_colour": RUNE, "footfalls": "Walk frames 0 (L1, R2) and 32 (R1, L2)"}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
        scale_bones=["Halo"], report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("warden", arm, mesh, actions, lod, out, assets, (0, -0.4, 1.9), 15.0, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40),
                      ("pulse", "close", "FreezePulse", PULSE_IMPACT), ("rage", "close", "WalkEnraged", 10),
                      ("dead", "close", "Death", DEATH_FRAMES)],
               strips=[("Walk", "side", range(0, WALK_FRAMES, 8)), ("FreezePulse", "close", range(0, PULSE_FRAMES + 1, 8)),
                       ("Enrage", "close", range(0, ENRAGE_FRAMES + 1, 6)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12))],
               macros=[("chest", G(0, 2.45, 0.5), 3.4, -20, 8, "Idle", 20), ("face", G(0, 3.3, 0.35), 3.4, -25, 10, "Idle", 20),
                       ("halo", G(0.5, 3.4, -0.4), 4.5, -150, 20, "Idle", 20)])


if __name__ == "__main__":
    main()
