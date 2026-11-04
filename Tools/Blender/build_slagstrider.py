"""Slagstrider: the fire-element stilt walker that wades through lava and leaves a burning trail (round 15, name
proposed; see ArtSource/DESIGN_NOTES.md).

A compact crucible body hung between four long stilt legs whose knees rise above the back; every foot is a
heat-proof cone of chilled slag drums. Charcoal slag plates over the body with molten seams, three furnace vents on
the back, a molten belly sac (the burning trail starts under it: bone `Drip`), a slit-jaw wedge head on a short
neck with a crown of back spikes and no visible eyes.

Clips: Walk (loop, diagonal pairs), Idle (loop), Hit, Death. Also exports Slagstrider_LOD1.

Run: blender --background --python Tools/Blender/build_slagstrider.py -- [--preview <dir>] [--norender]
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

NAME = "Slagstrider"
K = 1.0
GAME_SCALE = 0.8  # proposal

# Fire element: charcoal slag, orange-yellow molten glow.
PAL = H.Palette(top=(0.17, 0.15, 0.14), side=(0.07, 0.06, 0.055), belly=(0.03, 0.025, 0.022), seam=(0.02, 0.012, 0.008),
                dark=(0.045, 0.035, 0.03), glow=(1.0, 0.5, 0.1), eye=(1.0, 0.85, 0.45), membrane=(0.45, 0.12, 0.03),
                bone=(0.5, 0.44, 0.38))
HOT = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=PAL.dark, glow=(1.0, 0.72, 0.28),
                eye=PAL.eye, membrane=PAL.membrane, bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

LEG_TABLE = [((0.13, 0.5, 0.13), (0.4, 0.0, 0.4), (0.56, 0.62)),
             ((0.13, 0.5, -0.13), (0.4, 0.0, -0.38), (0.56, 0.62))]
# Slag plates on the right side and the midline (x, up, fwd), radii, yaw.
PLATES = [((0.0, 0.58, 0.04), (0.11, 0.13, 0.05), 0), ((0.1, 0.55, 0.12), (0.08, 0.09, 0.05), 25),
          ((0.12, 0.54, -0.06), (0.08, 0.1, 0.05), -20), ((0.0, 0.55, -0.17), (0.09, 0.08, 0.05), 0),
          ((0.15, 0.47, 0.03), (0.06, 0.12, 0.07), 5)]
VENTS = [(-0.02, 0.11), (-0.09, 0.09), (-0.16, 0.07)]  # (fwd, radius scale) on the midline


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.1, 1.0, 0.0))


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    b.ellipsoid("Body", G(0, 0.48, 0.0), (0.19, 0.27, 0.13), bone="Body", segments=(40, 20),
                style=S.core(PAL, k, seed=1, cells=50, width=0.02, glow=0.8))
    b.ellipsoid("Belly", G(0, 0.38, -0.02), (0.12, 0.17, 0.08), bone="Body", segments=(32, 16),
                style=K3.molten_sac(HOT, k, cells=26, seed=2, glow=1.0))
    for i, (p, r, yaw) in enumerate(PLATES):
        sides = [(None, 1)] if p[0] == 0.0 else SIDES
        for side, sx in sides:
            b.ellipsoid("Plate", K3.gp(p, sx), r, rot(yaw * sx, 6, 12 * sx * (p[0] != 0)), bone="Body", segments=(24, 12),
                        deform=C.chiselled(7, 50 + i, depth=0.15),
                        style=S.slab(PAL, k, strata=30, veins=0.8, fault=1.0, seed=10 + i))
    for f, s in VENTS:
        base = G(0, 0.58, f) if f > -0.1 else G(0, 0.55, f)
        top = base + G(0, 0.07 * s / 0.1, -0.03)
        b.tube("Vent", [base, base.lerp(top, 0.5), top], [0.03 * s / 0.1, 0.026 * s / 0.1, 0.024 * s / 0.1], sides=10,
               chain=["Body"], style=S.pillar(PAL, k, drums=2, seed=20, glow=0.5), cap=0.2)
        b.ellipsoid("VentFire", top + G(0, 0.004, 0), (0.019 * s / 0.1, 0.019 * s / 0.1, 0.01), bone="Body",
                    style=H.inner_glow(HOT, 1.0), segments=(12, 6))
    # Neck and slit-jaw head.
    b.tube("Neck", [G(0, 0.5, 0.18), G(0, 0.53, 0.26), G(0, 0.52, 0.33)], [0.06, 0.05, 0.045], sides=10,
           chain=["Neck"], style=S.pillar(PAL, k, drums=3, seed=30, glow=0.7))
    b.ellipsoid("Head", G(0, 0.5, 0.42), (0.065, 0.14, 0.06), rot(pitch=12), bone="Head", segments=(32, 16),
                deform=C.chiselled(6, 61, depth=0.08), style=S.slab(PAL, k, strata=40, veins=0.5, fault=0.6, seed=31))
    b.ellipsoid("Slit", G(0, 0.47, 0.5), (0.012, 0.07, 0.035), rot(pitch=12), bone="Head", style=H.inner_glow(HOT, 1.0),
                segments=(12, 10))
    for j, (x, f) in enumerate(((0.0, 0.38), (0.045, 0.36), (0.075, 0.33))):
        for side, sx in ([(None, 1)] if x == 0 else SIDES):
            base = K3.gp((x, 0.54, f), sx)
            b.tube("Crown", [base, base + K3.gp((0.02, 0.05, -0.05), sx), base + K3.gp((0.03, 0.07, -0.12), sx)],
                   [0.016, 0.01, 0.001], sides=8, chain=["Head"], style=S.spike(PAL, k, glow=0.5, seed=40 + j))
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.ellipsoid("Hip", hip, (0.045,) * 3, bone=lg.names[0], style=S.slab(PAL, k, seed=60 + i), segments=(14, 8))
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.02)), knee], [0.04, 0.032, 0.024], sides=10,
               chain=[lg.names[0]], style=S.pillar(PAL, k, drums=2, seed=62 + i, glow=0.3))
        b.ellipsoid("Knee", knee, (0.036,) * 3, bone=lg.names[0], style=S.core(PAL, k, seed=64 + i, cells=12, glow=0.9),
                    segments=(14, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("LegLower", [knee, knee.lerp(foot, 0.6), foot + Vector((0, 0, 0.1))], [0.024, 0.02, 0.03], sides=10,
               chain=[lg.names[1]], style=S.pillar(PAL, k, drums=3, seed=66 + i, glow=0.3))
        # The heat-proof foot: a cone of chilled slag drums.
        b.tube("FootCone", [foot + Vector((0, 0, 0.12)), foot + Vector((0, 0, 0.05)), foot + Vector((0, 0, 0.0))],
               [0.035, 0.05, 0.035], sides=10, chain=[lg.names[1]], style=S.pillar(PAL, k, drums=3, seed=68 + i, glow=0.8),
               cap=0.3)
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.47, -0.2), G(0, 0.49, 0.18), "Root", True),
           ("Neck", G(0, 0.5, 0.18), G(0, 0.52, 0.33), "Body", True),
           ("Head", G(0, 0.52, 0.33), G(0, 0.48, 0.55), "Neck", True),
           ("Drip", G(0, 0.32, -0.02), G(0, 0.22, -0.02), "Body", True)]
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, neck=0.0, head=0.0, head_yaw=0.0):
    return {"Root": {"rot": []},
            "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
            "Neck": {"rot": [(Z, head_yaw * 0.5), (X, neck)]},
            "Head": {"rot": [(Z, head_yaw * 0.5), (X, head)]},
            "Drip": {"rot": []}}


WALK_FRAMES = 36
STRIDE, LIFT, STANCE = 0.24, 0.1, 0.62
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.012 * math.cos(2 * w))), roll=math.radians(3) * math.sin(w),
                           yaw=math.radians(3) * math.sin(w), pitch_=math.radians(1.5) * math.sin(2 * w + 0.4),
                           neck=math.radians(3) * math.sin(2 * w + 1.0), head=-math.radians(3) * math.sin(2 * w + 1.0),
                           head_yaw=math.radians(6) * math.sin(w))
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.01 * math.sin(w))), roll=math.radians(1.5) * math.sin(w),
                           pitch_=math.radians(2) * math.sin(w + 0.3), neck=math.radians(5) * math.sin(w),
                           head=math.radians(6) * math.sin(2 * w), head_yaw=math.radians(16) * math.sin(w + 1.0))
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.03 * k, 0.02 * k)), pitch_=math.radians(-7) * k, roll=math.radians(6) * k,
                           neck=math.radians(-12) * k, head=math.radians(-8) * k)
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.6, t)
        specs = body_specs(offset=Vector((0, 0, 0.03 * jerk - 0.36 * fall)), roll=math.radians(14) * fall,
                           pitch_=math.radians(8) * fall, neck=math.radians(25) * fall, head=math.radians(15) * fall)
        return K3.splay_feet(poser, specs, lg, fall, out=1.15)
    return 42, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
RIGID = {"Plate", "Head"}
# LOD1: each leg one bone, Drip dropped into the body: 8 bones (the trail origin is then the body: 0.33 below it).
LOD_DROP = {"Drip": "Body", **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2)}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2)}


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 12, (0.3, 0.9), -0.15), ("Idle", 20, (1.3, 0.3), 0.3),
              ("Walk", 6, (-0.4, -0.5), 0.0), ("Walk", 24, (0.9, -0.7), -0.3)]
    previews = dict(target=(0, 0.0, 0.35), distance=2.8, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Death", "close", range(0, 43, 6))],
                    macros=[("plates", G(0.05, 0.58, 0.0), 0.45, -50, 35), ("head", G(0.0, 0.5, 0.45), 0.4, -30, 12),
                            ("foot", G(0.42, 0.06, 0.42), 0.35, -60, 15)],
                    rig_scale=1.4)
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("rock", "membrane", "chitin"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
                    extra=H.weathering(K, scratches=0.4, dust=0.2, seed=29), specular=0.25, emission_strength=3.0,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                            "trail_origin": "bone Drip (under the belly, 0.22 above the ground; LOD1: Body)",
                            "footfalls": "Walk frames 0 (L1, R2) and 18 (R1, L2)"},
                    previews=previews)


if __name__ == "__main__":
    main()
