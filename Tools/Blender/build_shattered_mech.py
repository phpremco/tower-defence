"""Round 28: The Shattered's split mechanic: the three shard creatures and the giant's clips (Docs/BOSSES.md
"The Shattered"). Run after build_shattered2.py (the improved giant).

Shards (own FBX, rig, one 512 material each, Unity scale 1, the giant's palette):
- `ShardRunner` (the second road): a fast two-legged strider, a spear head and a crest of blades. Walk (run loop),
  Idle, Hit, Death, Spawn (unfolds out of the split), Rejoin (crouches and springs back to the giant).
- `ShardFlyer` (the air route): a kite of crystal feather-blades round a core. Fly (loop), Idle (hover), Hit, Death
  (the blades shatter, it drops), Spawn, Rejoin (folds and darts back).
- `ShardCrawler` (the ground route): a six-legged blade-legged crawler under a pale shell. Walk, Idle, Hit, Death,
  Spawn, Rejoin.

The giant (opens ArtSource/Blend/Shattered.blend) gets Split (the light cracks open, the body bursts at the frame
in the report: the three shards spawn at the three spawn points, the giant collapses into the flash), Rejoin (the
shards fly in, it reassembles in the phase-3 "rejoined" look and roars), Roar, Climb_Up / Climb_Loop / Climb_Down
(its shards scale the ruins: the giant climbs too), Dive_Into_Liquid / Submerged_Loop / Surface_Burst.

Run: blender --background --factory-startup --python Tools/Blender/build_shattered_mech.py --
     [--preview <dir>] [--norender] [--skip-shards] [--only-shards]

Round 29: ShardRunner and ShardCrawler also get Climb_Up / Climb_Loop / Climb_Down over a 1.0 m ruin ledge
(round-28 boss conventions); `--only-shards` rebuilds the three shards without touching the giant.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_shattered as BS  # noqa: E402
import build_shattered2 as B2  # noqa: E402
import shattered_detail as SHD  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import crysium as Y  # noqa: E402
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BS.NAME
R = math.radians
SIDES = T.SIDES
SPAL = BS.PAL      # the round-28 palette (build_shattered2 sets it on import)
SCORE = BS.CORE
GLOW = "overcharge_w"
RUNNER, FLYER, CRAWLER = "ShardRunner", "ShardFlyer", "ShardCrawler"
CLIMB_H = 1.5
SPLIT_FRAMES, SPLIT_BURST = 64, 28
REJOIN_FRAMES, REJOIN_FORM = 80, 30
# Where each shard appears at SPLIT_BURST (game x right, up, forward from the giant's pivot) and leaves from in Rejoin.
SPAWN = {RUNNER: (1.6, 0.0, 2.2), FLYER: (0.0, 3.2, -0.4), CRAWLER: (-1.6, 0.0, -1.8)}
CORE_OF = {RUNNER: "Core_Road", FLYER: "Core_Air", CRAWLER: "Core_Land"}


def crystal(seed, strength=1.0):
    return Y.shard_style(GLOW, strength, seed=seed)


# ------------------------------------------------------------------------------------------------------------
# ShardRunner
# ------------------------------------------------------------------------------------------------------------

RUN_LEGS = [((0.2, 0.95, -0.05), (0.32, 0.0, 0.1), (0.55, 0.66))]


def runner_legs():
    return K3.limbs(RUN_LEGS, pole=(0.1, 0.25, -1.0))


def runner_parts():
    b = C.PartBuilder()
    k = 0.7
    b.ellipsoid("Hip", G(0, 1.0, -0.05), (0.24, 0.3, 0.2), T.rot(pitch=-10), bone="Hips", segments=(28, 14),
                style=T.kiln_crust(SCORE, k * 0.5, cells=8, seed=11, glow=0.8, deep=(0.95, 0.2, 0.62)))
    b.ellipsoid("Torso", G(0, 1.22, 0.32), (0.22, 0.5, 0.2), T.rot(pitch=-14), bone="Torso", segments=(28, 14),
                style=T.kiln_crust(SCORE, k * 0.5, cells=10, seed=12, glow=0.8, deep=(0.95, 0.2, 0.62)))
    T.ellipsoid_patch(b, "Plate", G(0, 1.22, 0.32), (0.22, 0.5, 0.2), T.rot(pitch=-14), (0, 1), (-0.7, 0.7), (-0.85, 0.75),
                      1, 0.02, 0.05, "Torso", T.shell_plate(SPAL, k * 0.4, (0, 1), (-0.7, 0.7), (-0.85, 0.75), crack=0.7,
                                                            rim_glow=0.8, seed=13, chevron=0.8), nu=10, nv=14, bulge=0.02, keel=0.08)
    # The spear head and its crest.
    b.shard("Spear", G(0, 1.32, 0.72), G(0, -0.12, 1.0).normalized(), 0.12, 0.95, sides=6, flute=0.4, twist=0.6,
            bone="Head", style=crystal(20))
    for j, sx in enumerate((-1, 1)):
        b.shard("Fin", G(0.08 * sx, 1.38, 0.62), G(0.6 * sx, 0.5, -0.2).normalized(), 0.05, 0.4, sides=5, flute=0.3,
                bone="Head", style=crystal(21 + j))
        b.ellipsoid("Eye", G(0.08 * sx, 1.3, 0.86), (0.03, 0.03, 0.025), bone="Head", segments=(10, 6),
                    style=H.eye(SPAL, k * 0.3, 1.0))
    for j, (u, f, l) in enumerate(((1.42, 0.25, 0.5), (1.38, 0.0, 0.42), (1.3, -0.25, 0.34))):
        b.shard("Crest", G(0, u, f), G(0, 0.8, -0.6).normalized(), 0.06, l, sides=5, flute=0.4, bone="Torso",
                style=crystal(30 + j))
    b.shard("Tail", G(0, 1.0, -0.3), G(0, 0.1, -1.0).normalized(), 0.08, 0.7, sides=5, flute=0.3, bone="Tail",
            style=crystal(35))
    for (side, i), lg in runner_legs().items():
        hip, knee, foot = lg.rest_points
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5), knee], [0.11, 0.09, 0.07], sides=10, chain=[lg.names[0]],
               style=H.leg(SPAL, k * 0.3, "Femur", spines=1))
        b.shard("Shin", knee, (foot - knee), 0.075, (foot - knee).length * 1.04, sides=5, flute=0.3, bone=lg.names[1],
                style=crystal(40 + i))
    return b


def runner_bones():
    out = [("Root", G(0, 0, 0), G(0, 0.3, 0), None, True),
           ("Hips", G(0, 1.0, -0.25), G(0, 1.05, 0.1), "Root", True),
           ("Torso", G(0, 1.05, 0.1), G(0, 1.3, 0.62), "Hips", True),
           ("Head", G(0, 1.3, 0.62), G(0, 1.25, 1.2), "Torso", True),
           ("Tail", G(0, 1.0, -0.25), G(0, 1.02, -0.9), "Hips", True)]
    for lg in runner_legs().values():
        out += lg.rest_bones("Hips")
    return out


def runner_specs(off=None, lean=0.0, roll=0.0, yaw=0.0, head=0.0, tail=0.0, curl=0.0, scale=1.0):
    return {"Root": {"rot": [], "scale": max(scale, 0.001)},
            "Hips": {"rot": [(M.Z, yaw), (M.Yv, roll), (M.X, -lean * 0.4 + curl * 0.3)], "loc": off if off is not None else Vector()},
            "Torso": {"rot": [(M.X, -lean * 0.6 + curl)]},
            "Head": {"rot": [(M.X, -head + curl * 0.6)]},
            "Tail": {"rot": [(M.Z, tail), (M.X, curl * 0.8)]}}


RUN_PH = {("L", 1): 0.0, ("R", 1): 0.5}


def runner_walk(poser):
    lg = runner_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = runner_specs(off=Vector((0, 0, 0.04 * math.cos(2 * w) - 0.04)), lean=R(14), roll=R(4) * math.sin(w),
                          yaw=R(4) * math.sin(w), head=-R(8) + R(3) * math.sin(2 * w), tail=R(10) * math.sin(w - 1.0))
        return K3.place_legs(poser, sp, lg, K3.gait_feet(lg, t, RUN_PH, 0.42, 1.1, 0.32), body="Hips")
    return 20, spec, True


def runner_idle(poser):
    lg = runner_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = runner_specs(off=Vector((0, 0, 0.02 * math.sin(w) - 0.05)), lean=R(6) + R(2) * math.sin(w),
                          head=R(4) * math.sin(2 * w), yaw=R(10) * math.sin(w), tail=R(8) * math.sin(w + 1))
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()}, body="Hips")
    return 48, spec, True


def runner_hit(poser):
    lg = runner_legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = runner_specs(off=Vector((0, 0.08 * k, 0)), lean=-R(10) * k, head=R(12) * k, roll=R(6) * k)
        return K3.place_legs(poser, sp, lg, {k_: l.foot.copy() for k_, l in lg.items()}, body="Hips")
    return 12, spec, False


def runner_death(poser):
    lg = runner_legs()

    def spec(t, f):
        fall = ease(4, 24, f)
        burst = ease(26, 36, f)
        sc = 1.0 + 0.2 * pulse(f, 22, 26, 30) - burst
        sp = runner_specs(off=Vector((0, -0.2 * fall, -0.7 * fall)), lean=R(40) * fall, roll=R(20) * fall,
                          head=-R(20) * fall, scale=sc)
        return scale_limbs(K3.splay_feet(poser, sp, lg, fall, out=1.3, body="Hips"), lg, sc)
    return 40, spec, False


def scale_limbs(sp, legs, s):
    """Limb bones are posed with absolute (IK) matrices, so they do not inherit Root's scale: give them their own."""
    for lg in legs.values():
        for n in lg.names:
            if n in sp:
                sp[n]["scale"] = max(s, 0.001)
    return sp


def unfold_spec(specs_fn, legs, place, frames, curl_amt, body, flip=False):
    """Spawn (flip False): from a tight curled ball at scale 0.3 to the stance; Rejoin (flip True): the reverse,
    ending with a spring upwards (the body leaves along +up; Unity flies it to the giant)."""
    def spec(t, f):
        u = ease(0, frames * 0.8, f) if not flip else 1 - ease(frames * 0.15, frames * 0.75, f)
        spring = pulse(f, frames * 0.7, frames * 0.85, frames) if flip else 0.0
        scale = 0.3 + 0.7 * u if not flip else 0.35 + 0.65 * u
        sp = specs_fn(off=Vector((0, 0, -0.3 * (1 - u) + 0.6 * spring)), curl=curl_amt * (1 - u), scale=scale)
        feet = {}
        for key, l in legs.items():
            tucked = l.root.lerp(l.foot, 0.4) + Vector((0, 0, 0.1))
            feet[key] = tucked.lerp(l.foot, u)
        return scale_limbs(place(sp, feet), legs, scale)
    return frames, spec, False


def runner_spawn(poser):
    lg = runner_legs()
    return unfold_spec(runner_specs, lg, lambda sp, ft: K3.place_legs(poser, sp, lg, ft, body="Hips"), 30, R(70), "Hips")


def runner_rejoin(poser):
    lg = runner_legs()
    return unfold_spec(runner_specs, lg, lambda sp, ft: K3.place_legs(poser, sp, lg, ft, body="Hips"), 30, R(70), "Hips",
                       flip=True)


# ------------------------------------------------------------------------------------------------------------
# ShardFlyer
# ------------------------------------------------------------------------------------------------------------

FEATHERS = [(0.0, 1.0, 0.1), (0.25, 0.95, 0.09), (0.5, 0.85, 0.08)]  # (sweep back, length scale, radius)


def flyer_parts():
    b = C.PartBuilder()
    k = 0.6
    b.ellipsoid("Core", G(0, 0, 0), (0.2, 0.42, 0.16), bone="Body", segments=(28, 14),
                style=T.kiln_crust(SCORE, k * 0.4, cells=9, seed=51, glow=0.9, deep=(0.95, 0.2, 0.62)))
    T.ellipsoid_patch(b, "Plate", G(0, 0, 0), (0.2, 0.42, 0.16), T.rot(), (0, 1), (-0.75, 0.75), (-0.85, 0.85), 1, 0.02,
                      0.04, "Body", T.shell_plate(SPAL, k * 0.4, (0, 1), (-0.75, 0.75), (-0.85, 0.85), crack=0.7,
                                                   rim_glow=0.8, seed=52, chevron=0.9), nu=10, nv=12, bulge=0.02, keel=0.06)
    for side, sx in SIDES:
        for seg, (bone, x0, span) in enumerate(((f"Wing_{side}_1", 0.16, 0.55), (f"Wing_{side}_2", 0.7, 0.6))):
            for j, (sweep, ls, r) in enumerate(FEATHERS):
                base = G(x0 * sx, 0.02, 0.1 - 0.12 * j)
                d = G(sx * 1.0, 0.05, -sweep - 0.25 * seg).normalized()
                b.shard("Feather", base, d, r * (0.8 if seg else 1.0), span * ls * (1.1 if seg else 1.0), sides=4,
                        flute=0.2, twist=0.3 * sx, bone=bone, style=crystal(60 + 3 * seg + j))
    for j, sx in enumerate((-1, 0, 1)):
        b.shard("Prong", G(0.07 * sx, 0.02, 0.35), G(0.25 * sx, 0.0, 1.0).normalized(), 0.05, 0.35 if sx else 0.45, sides=5,
                flute=0.3, bone="Body", style=crystal(70 + j))
    for j, sx in enumerate((-1, 1)):
        b.ellipsoid("Eye", G(0.08 * sx, 0.08, 0.32), (0.03, 0.03, 0.025), bone="Body", segments=(10, 6),
                    style=H.eye(SPAL, k * 0.3, 1.0))
    b.shard("Tail", G(0, 0, -0.35), G(0, 0.0, -1.0), 0.07, 0.75, sides=5, flute=0.3, twist=0.8, bone="Tail",
            style=crystal(75))
    return b


def flyer_bones():
    out = [("Root", G(0, -0.2, 0), G(0, 0.1, 0), None, True),
           ("Body", G(0, 0, -0.3), G(0, 0, 0.3), "Root", True),
           ("Tail", G(0, 0, -0.32), G(0, 0, -1.0), "Body", True)]
    for side, sx in SIDES:
        out += [(f"Wing_{side}_1", G(0.16 * sx, 0.02, 0.0), G(0.7 * sx, 0.02, -0.1), "Body", True),
                (f"Wing_{side}_2", G(0.7 * sx, 0.02, -0.1), G(1.3 * sx, 0.02, -0.35), f"Wing_{side}_1", True)]
    return out


def flyer_specs(off=None, pitch=0.0, roll=0.0, flap=0.0, fold=0.0, tail=0.0, curl=0.0, scale=1.0, shatter=0.0):
    sp = {"Root": {"rot": [], "scale": max(scale, 0.001)},
          "Body": {"rot": [(M.Yv, roll), (M.X, -pitch + curl)], "loc": off if off is not None else Vector()},
          "Tail": {"rot": [(M.Z, tail), (M.X, curl)]}}
    for side, sx in SIDES:
        sp[f"Wing_{side}_1"] = {"rot": [(M.Yv, -flap * sx), (M.Z, fold * sx * 1.2)], "scale": max(1 - shatter, 0.001)}
        sp[f"Wing_{side}_2"] = {"rot": [(M.Yv, -flap * 0.6 * sx), (M.Z, fold * sx * 0.9)]}
    return sp


def flyer_fly(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return flyer_specs(off=Vector((0, 0, -0.06 * math.sin(w))), pitch=-R(4), roll=R(3) * math.sin(w),
                           flap=R(32) * math.sin(w), tail=R(10) * math.sin(w - 1.2))
    return 24, spec, True


def flyer_idle(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return flyer_specs(off=Vector((0, 0, 0.08 * math.sin(w))), pitch=R(6), roll=R(4) * math.sin(w * 2),
                           flap=R(22) * math.sin(3 * w), tail=R(14) * math.sin(w))
    return 32, spec, True


def flyer_hit(poser):
    def spec(t, f):
        k = K2.recoil(t)
        return flyer_specs(off=Vector((0, 0.1 * k, 0.05 * k)), pitch=R(14) * k, roll=R(12) * k, flap=R(20) * k)
    return 12, spec, False


def flyer_death(poser):
    """The feather-blades shatter off (scale 0 by frame 14: shard VFX), the core tumbles and drops 3 m."""
    def spec(t, f):
        drop = ease(6, 36, f) ** 1.6
        return flyer_specs(off=Vector((0, 0, -3.0 * drop)), pitch=R(200) * drop, roll=R(160) * drop,
                           flap=R(40) * pulse(f, 0, 4, 10), shatter=ease(4, 14, f),
                           scale=1.0 - ease(34, 44, f))
    return 44, spec, False


def flyer_spawn(poser):
    def spec(t, f):
        u = ease(0, 26, f)
        return flyer_specs(off=Vector((0, 0, 0.3 * u)), fold=R(70) * (1 - u), curl=R(30) * (1 - u), scale=0.3 + 0.7 * u,
                           flap=R(30) * math.sin(f * 0.8) * u)
    return 30, spec, False


def flyer_rejoin(poser):
    def spec(t, f):
        u = ease(4, 24, f)
        return flyer_specs(off=Vector((0, -0.4 * u, 0.5 * u)), pitch=R(40) * u, fold=R(70) * u,
                           curl=R(20) * u, scale=1.0 - 0.6 * ease(20, 30, f))
    return 30, spec, False


# ------------------------------------------------------------------------------------------------------------
# ShardCrawler
# ------------------------------------------------------------------------------------------------------------

CRAWL_LEGS = [((0.3, 0.42, 0.42), (0.95, 0.0, 0.8), (0.5, 0.62)),
              ((0.36, 0.42, 0.0), (1.05, 0.0, 0.05), (0.52, 0.64)),
              ((0.3, 0.42, -0.42), (0.95, 0.0, -0.75), (0.5, 0.62))]
CRAWL_PH = {("L", 1): 0.0, ("R", 2): 0.0, ("L", 3): 0.0, ("R", 1): 0.5, ("L", 2): 0.5, ("R", 3): 0.5}
SHELL_C, SHELL_R = G(0, 0.48, 0.0), (0.46, 0.72, 0.24)


def crawler_legs():
    return K3.limbs(CRAWL_LEGS, pole=(0.3, 1.0, 0.0))


def crawler_parts():
    b = C.PartBuilder()
    k = 0.7
    b.ellipsoid("Core", SHELL_C - G(0, 0.04, 0), (0.42, 0.68, 0.2), bone="Body", segments=(32, 16),
                style=T.kiln_crust(SCORE, k * 0.5, cells=12, seed=81, glow=0.9, deep=(0.95, 0.2, 0.62)))
    for i, (b0, b1) in enumerate(((-0.95, -0.35), (-0.3, 0.25), (0.3, 0.92))):
        T.ellipsoid_patch(b, "Plate", SHELL_C, SHELL_R, T.rot(), (0, 1), (-0.8, 0.8), (b0, b1), 1, 0.02, 0.05, "Body",
                          T.shell_plate(SPAL, k * 0.4, (0, 1), (-0.8, 0.8), (b0, b1), crack=0.7, rim_glow=0.8,
                                        seed=82 + i, chevron=0.7), nu=12, nv=8, bulge=0.02, keel=0.1)
    for j, (f, l) in enumerate(((0.35, 0.5), (0.0, 0.62), (-0.35, 0.45))):
        for sx in ((-1, 1) if j != 1 else (0,)):
            b.shard("Spike", G(0.18 * sx, 0.66, f), G(0.5 * sx, 1.0, -0.4).normalized(), 0.06, l, sides=5, flute=0.4,
                    twist=0.5, bone="Body", style=crystal(90 + j))
    b.ellipsoid("Head", G(0, 0.42, 0.75), (0.24, 0.2, 0.15), bone="Head", segments=(24, 12),
                style=T.kiln_crust(SCORE, k * 0.4, cells=6, seed=95, glow=1.0, deep=(0.95, 0.2, 0.62)))
    for side, sx in SIDES:
        b.shard("Mandible", G(0.12 * sx, 0.38, 0.86), G(0.35 * sx, -0.1, 1.0).normalized(), 0.06, 0.5, sides=5, flute=0.3,
                bone=f"Mand_{side}", style=crystal(96))
        for j, x in enumerate((0.08, 0.16)):
            b.ellipsoid("Eye", G(x * sx, 0.5, 0.86 - 0.04 * j), (0.03, 0.03, 0.025), bone="Head", segments=(10, 6),
                        style=H.eye(SPAL, k * 0.3, 1.0))
    for (side, i), lg in crawler_legs().items():
        hip, knee, foot = lg.rest_points
        b.tube("Femur", [hip, hip.lerp(knee, 0.5), knee], [0.07, 0.06, 0.05], sides=8, chain=[lg.names[0]],
               style=H.leg(SPAL, k * 0.3, "Femur", spines=1))
        b.shard("Blade", knee, (foot - knee), 0.06, (foot - knee).length * 1.05, sides=5, flute=0.3, bone=lg.names[1],
                style=crystal(100 + i))
    return b


def crawler_bones():
    out = [("Root", G(0, 0, 0), G(0, 0.3, 0), None, True),
           ("Body", G(0, 0.45, -0.6), G(0, 0.45, 0.6), "Root", True),
           ("Head", G(0, 0.45, 0.6), G(0, 0.42, 0.95), "Body", True)]
    for side, sx in SIDES:
        out.append((f"Mand_{side}", G(0.12 * sx, 0.38, 0.84), G(0.2 * sx, 0.34, 1.3), "Head", True))
    for lg in crawler_legs().values():
        out += lg.rest_bones("Body")
    return out


def crawler_specs(off=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, mand=0.0, curl=0.0, scale=1.0):
    sp = {"Root": {"rot": [], "scale": max(scale, 0.001)},
          "Body": {"rot": [(M.Z, yaw), (M.Yv, roll), (M.X, -pitch + curl)], "loc": off if off is not None else Vector()},
          "Head": {"rot": [(M.X, -head + curl)]}}
    for side, sx in SIDES:
        sp[f"Mand_{side}"] = {"rot": [(M.Z, -mand * sx)]}
    return sp


def crawler_walk(poser):
    lg = crawler_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = crawler_specs(off=Vector((0, 0, 0.025 * math.cos(2 * w))), roll=R(3) * math.sin(w), yaw=R(3) * math.sin(w),
                           head=R(3) * math.sin(2 * w), mand=R(8) + R(6) * math.sin(2 * w))
        return K3.place_legs(poser, sp, lg, K3.gait_feet(lg, t, CRAWL_PH, 0.6, 0.6, 0.2))
    return 26, spec, True


def crawler_idle(poser):
    lg = crawler_legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = crawler_specs(off=Vector((0, 0, 0.02 * math.sin(w))), head=R(5) * math.sin(w), yaw=R(6) * math.sin(w),
                           mand=R(6) + R(14) * max(0, math.sin(2 * w)))
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()})
    return 48, spec, True


def crawler_hit(poser):
    lg = crawler_legs()

    def spec(t, f):
        k = K2.recoil(t)
        sp = crawler_specs(off=Vector((0, 0.06 * k, 0.04 * k)), pitch=R(8) * k, roll=R(6) * k, mand=R(24) * max(k, 0))
        return K3.place_legs(poser, sp, lg, {k_: l.foot.copy() for k_, l in lg.items()})
    return 12, spec, False


def crawler_death(poser):
    lg = crawler_legs()

    def spec(t, f):
        fall = ease(2, 20, f)
        sc = 1.0 + 0.15 * pulse(f, 20, 24, 28) - ease(26, 36, f)
        sp = crawler_specs(off=Vector((0, 0, -0.32 * fall)), roll=R(10) * fall, mand=R(30) * fall, scale=sc)
        return scale_limbs(K3.splay_feet(poser, sp, lg, fall, out=1.25), lg, sc)
    return 40, spec, False


def crawler_spawn(poser):
    lg = crawler_legs()
    return unfold_spec(crawler_specs, lg, lambda sp, ft: K3.place_legs(poser, sp, lg, ft), 30, R(50), "Body")


def crawler_rejoin(poser):
    lg = crawler_legs()
    return unfold_spec(crawler_specs, lg, lambda sp, ft: K3.place_legs(poser, sp, lg, ft), 30, R(50), "Body", flip=True)


# ------------------------------------------------------------------------------------------------------------
# Round 29: the runner and the crawler scale the ruins (BOSSES.md "The Shattered's shards scale the ruins")
# ------------------------------------------------------------------------------------------------------------

SHARD_CLIMB_H = 1.0     # one ruin ledge for both shards (the giant's is 1.5); other heights: tilt + blend in Unity


def runner_body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0):
    """The generic climb amounts on the runner: it crouches into the spring, the spear head stays on the target."""
    sp = runner_specs(off=at(0, 0, -0.22 * crouch), lean=R(10) + R(16) * crouch + R(8) * reach, roll=roll,
                      yaw=yaw, head=-R(6) + look - pitch * 0.3, tail=-pitch * 0.4 + R(12) * crouch,
                      curl=R(10) * tuck)
    # A biped's two hips sit side by side, so the climb pitch over its short span is steep: it takes half of it.
    sp["Hips"]["rot"].append((M.X, -pitch * 0.45))
    sp["Root"]["loc"] = off
    return sp


def crawler_body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0):
    sp = crawler_specs(off=at(0, 0, -0.14 * crouch), pitch=pitch, roll=roll, yaw=yaw,
                       head=look - pitch * 0.4 + R(6) * reach, mand=R(8) + R(22) * max(mouth, 0.6 * rage, 0.5 * reach),
                       curl=R(8) * tuck)
    # The travel goes on Root: a few fused-in vertices are weighted to Root and would stay behind at the pivot.
    sp["Root"]["loc"] = off
    return sp


def runner_adapter():
    return M.Legged(runner_body, [(runner_legs(), "Hips")], [RUN_PH], 0.42, 1.1, 0.32, 20, hip_span=(0.35, -0.35))


def crawler_adapter():
    return M.Legged(crawler_body, [(crawler_legs(), "Body")], [CRAWL_PH], 0.6, 0.6, 0.2, 26, hip_span=(0.45, -0.45))


def shard_climbs(ad, slow, stride_k, lift_k=1.0, lead=0.5, reach=0.0):
    """Climb_Up / Climb_Loop / Climb_Down over a SHARD_CLIMB_H ledge with the round-28 conventions (pivot fixed,
    end offsets in the report, Climb_Loop in place on the top). Returns (clips, facts)."""
    lift, front = ad.lift, ad.foot_front
    ad.lift = lift * lift_k
    # The ledge face sits 0.45 ahead of the front foot's rest contact; a long stride swings the foot past that, so the
    # face moves out by `reach` and frame 0 stays the plain gait pose on flat ground.
    ad.foot_front = front + reach
    up, fu = M.climb_up(ad, SHARD_CLIMB_H, slow=slow, stride_k=stride_k, crouch=0.5, look=0.3, lead=lead, trail=0.6)
    loop, fl = M.climb_loop(ad, slow=1.3, crouch=0.5, width=1.1)
    down, fd = M.climb_down(ad, SHARD_CLIMB_H, slow=slow, stride_k=stride_k, crouch=0.4, trail=0.3, lead=0.8)
    ad.lift, ad.foot_front = lift, front

    def frames_of(fn):
        return fn(None)[0]
    clips = {"Climb_Up": up, "Climb_Loop": loop, "Climb_Down": down}
    facts = {"Climb_Up": fu, "Climb_Loop": fl, "Climb_Down": fd}
    for name, fn in clips.items():
        facts[name] = dict(facts[name], frames=frames_of(fn), loop=name == "Climb_Loop")
    return clips, facts


RUNNER_CLIMB = shard_climbs(runner_adapter(), slow=1.6, stride_k=1.0, lift_k=1.3, lead=0.3, reach=0.6)
CRAWLER_CLIMB = shard_climbs(crawler_adapter(), slow=1.2, stride_k=1.2, lift_k=1.6)


SHARDS = {
    RUNNER: (runner_parts, runner_bones, {"Walk": runner_walk, "Idle": runner_idle, "Hit": runner_hit,
                                          "Death": runner_death, "Spawn": runner_spawn, "Rejoin": runner_rejoin,
                                          **RUNNER_CLIMB[0]},
             2200, {"Spear", "Fin", "Crest", "Tail", "Shin", "Plate"},
             {"route": "the second road", "walk_distance_per_loop": round(1.1 / 0.42, 3), "move": "ground"}),
    FLYER: (flyer_parts, flyer_bones, {"Fly": flyer_fly, "Idle": flyer_idle, "Hit": flyer_hit, "Death": flyer_death,
                                       "Spawn": flyer_spawn, "Rejoin": flyer_rejoin},
            2000, {"Feather", "Prong", "Tail", "Plate"},
            {"route": "the air route", "move": "air (pivot = body centre; a ground shadow like every flyer)",
             "death": "drops 3.0 m; hide at frame 44"}),
    CRAWLER: (crawler_parts, crawler_bones, {"Walk": crawler_walk, "Idle": crawler_idle, "Hit": crawler_hit,
                                             "Death": crawler_death, "Spawn": crawler_spawn, "Rejoin": crawler_rejoin,
                                             **CRAWLER_CLIMB[0]},
              2400, {"Plate", "Spike", "Mandible", "Blade"},
              {"route": "the ground route (the giant's road)", "walk_distance_per_loop": round(0.6 / 0.6, 3),
               "move": "ground"}),
}


LIMBS = {RUNNER: runner_legs, CRAWLER: crawler_legs}
CLIMBS = {RUNNER: RUNNER_CLIMB, CRAWLER: CRAWLER_CLIMB}


def build_shards():
    for name, (parts, bones, clips, tris, rigid, report) in SHARDS.items():
        report = dict(report, shard_of=NAME, spawn_offset=SPAWN[name], giant_spire=CORE_OF[name],
                      hp_share="a third of the giant's remaining HP (BOSSES.md)",
                      spawn="play Spawn at the spawn offset at the giant's Split burst frame",
                      rejoin="play Rejoin, then fly the shard to the giant over ~0.6 s; the giant's Rejoin forms at "
                             f"frame {REJOIN_FORM}")
        if name in CLIMBS:
            report["round29"] = {"climb_height_m": SHARD_CLIMB_H, "clips": CLIMBS[name][1],
                                 "foot_markers": "none: no IK markers on the shards (baked clips plus a slope tilt, "
                                                 "as Unity climbs the bosses)",
                                 "note": "round-28 boss climb conventions: Climb_Up/Climb_Down authored with the pivot "
                                         "fixed, move the transform by end_offset_forward_m / end_offset_up_m on the "
                                         "last frame; Climb_Loop in place on the top surface"}
        M.small_rig(name, parts(), bones(), clips, tris, families=("crystal", "chitin", "rock"), rigid=rigid, blend=1,
                    scale_bones=["Root"] + ([f"Wing_{s}_1" for s, _ in SIDES] if name == FLYER else
                                            [n for lg in LIMBS[name]().values() for n in lg.names]),
                    voxel=0.0045, ao=0.06, extrusion=0.012, ray=0.03, emission=2.8, k=0.6, mirror=True,
                    extra=H.weathering(0.6, scratches=0.4, dust=0.2, seed=7), report=report)


# ------------------------------------------------------------------------------------------------------------
# The giant
# ------------------------------------------------------------------------------------------------------------

GIANT_LIMBS = [n for lg in list(BS.arms().values()) + list(BS.legs().values()) for n in lg.names]
GIANT_SCALE = BS.SCALE_BONES + B2.CORE_NAMES + ["Pelvis"] + GIANT_LIMBS
SPLIT_REJOIN = {"Split": GIANT_SCALE, "Rejoin": GIANT_SCALE}


def giant_limb_scale(sp, s):
    for n in GIANT_LIMBS:
        sp[n]["scale"] = max(s, 0.001)
    return sp


def giant_body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0):
    return BS.specs(off=off + at(0, 0, -0.3 * crouch), pitch=pitch, roll=roll, yaw=yaw,
                    chest=-pitch * 0.25 + R(6) * crouch + R(12) * tuck, head=look + R(10) * rage - pitch * 0.4,
                    jaw=R(6 + 30 * max(mouth, rage * 0.6)))


def adapter():
    return M.Legged(giant_body, [(BS.arms(), "Chest"), (BS.legs(), "Pelvis")],
                    [{("L", 1): 0.0, ("R", 1): 0.5}, {("R", 1): 0.1, ("L", 1): 0.6}], BS.STANCE, BS.STRIDE, BS.LIFT,
                    BS.WALK_FRAMES, hip_span=(1.5, -1.5))


def split(poser):
    """Cracks of light run over it (to 20: the crystals swell, the plates lift on light, it shudders and rises),
    at SPLIT_BURST it bursts: every plate is thrown off, the three shard spires fly out towards the three spawn
    points and the body collapses into the flash (Pelvis scale 0 by SPLIT_BURST + 8). Spawn the shards at
    SPLIT_BURST; hide the giant after the clip."""
    am, lg = BS.arms(), BS.legs()

    def spec(t, f):
        crack = ease(0, SPLIT_BURST - 4, f)
        shudder = math.sin(f * 3.0) * crack * (1 - ease(SPLIT_BURST, SPLIT_BURST + 2, f))
        rise = pulse(f, 4, SPLIT_BURST - 2, SPLIT_BURST + 4)
        blow, sh = BS.blow_off(f, SPLIT_BURST, BS.SHELL_NAMES)
        shells = {n: (1.0 if f < SPLIT_BURST else sh[n]) for n in BS.SHELL_NAMES}
        crys = {n: 0.25 + 1.1 * crack for n in BS.CRYS_NAMES}
        sp = BS.specs(off=Vector((0, 0, 0.25 * rise)), chest=-R(14) * rise, chest_roll=R(2) * shudder,
                      head=-R(24) * rise, jaw=R(6 + 36 * crack), shells=shells, crys=crys, blow=blow if f >= SPLIT_BURST else None)
        gone = ease(SPLIT_BURST, SPLIT_BURST + 8, f)
        sp["Pelvis"]["scale"] = max(1 - gone, 0.001)
        for name, core in CORE_OF.items():
            x, u, fw = SPAWN[name]
            fly = ease(SPLIT_BURST - 1, SPLIT_BURST + 6, f)
            rest = poser.rest[core].translation
            target = G(x, u + 1.2, fw)
            m = Matrix.Translation(rest.lerp(target, fly)) @ poser.rest[core].to_3x3().to_4x4()
            sp[core] = {"abs": m, "scale": max((1.0 + 0.5 * crack) * (1 - ease(SPLIT_BURST + 4, SPLIT_BURST + 10, f)), 0.001)}
        return giant_limb_scale(BS.place(poser, sp, *BS.planted()), 1 - gone)
    return SPLIT_FRAMES, spec, False


def rejoin(poser):
    """Starts empty (everything at scale 0 at the pivot); the shards arrive (Unity flies them in during frames
    0-24), the three spires appear where they land, the core grows out of them (REJOIN_FORM, with an overshoot) in
    the rejoined look (phase 3: no plates, swollen crystals), and it roars. A dead shard's spire: set its Core_*
    scale to 0.35 in LateUpdate after the clip (each dead shard takes 30% off the giant)."""
    def spec(t, f):
        form = ease(REJOIN_FORM - 10, REJOIN_FORM + 12, f)
        over = math.sin(math.pi * ease(REJOIN_FORM, REJOIN_FORM + 18, f)) * 0.12
        roar_ = pulse(f, REJOIN_FORM + 16, REJOIN_FORM + 30, REJOIN_FRAMES - 4)
        crys = {n: 1.35 * form + over for n in BS.CRYS_NAMES}
        sp = BS.specs(off=Vector((0, 0.1 * roar_, 0.2 * roar_)), chest=-R(16) * roar_, head=-R(26) * roar_,
                      jaw=R(6 + 40 * roar_), shells=BS.PHASE[3][0], crys=crys)
        sp["Pelvis"]["scale"] = max(form + over, 0.001)
        for name, core in CORE_OF.items():
            x, u, fw = SPAWN[name]
            arrive = ease(14, REJOIN_FORM - 4, f)
            rest = poser.rest[core].translation
            m = Matrix.Translation(G(x, u + 1.2, fw).lerp(rest, arrive)) @ poser.rest[core].to_3x3().to_4x4()
            sp[core] = {"abs": m, "scale": max(ease(10, 18, f) * (1.0 + 0.3 * pulse(f, REJOIN_FORM - 6, REJOIN_FORM, REJOIN_FORM + 10)), 0.001)}
        return giant_limb_scale(BS.place(poser, sp, *BS.planted()), form + over)
    return REJOIN_FRAMES, spec, False


def roar(poser):
    am, lg = BS.arms(), BS.legs()

    def spec(t, f):
        lift = ease(4, 22, f) * (1 - ease(52, 68, f))
        jaw = ease(14, 24, f) * (1 - ease(48, 60, f))
        tremble = math.sin(f * 2.4) * pulse(f, 20, 32, 50)
        sp = BS.specs(off=Vector((0, 0.3 * lift, 0.45 * lift)), pitch=R(24) * lift, chest=-R(10) * lift,
                      chest_roll=R(2) * tremble, head=-R(20) * lift, jaw=R(6 + 40 * jaw))
        pose = poser.solve(sp)
        move = pose["Chest"] @ poser.rest["Chest"].inverted()
        a_feet, l_feet = BS.planted()
        for key, l in am.items():
            sx = -1 if key[0] == "L" else 1
            a_feet[key] = l.foot.lerp(move @ l.foot, lift) + G(0.35 * sx, 0.6 + 0.1 * tremble, 0.1) * lift
        return BS.place(poser, sp, a_feet, l_feet)
    return 70, spec, False


def fit_depth(boss, make, name, start, margin=0.2):
    depth = start
    for _ in range(2):
        fn, facts = make(depth)
        act = boss.key(name, fn, facts)
        hi = M.mesh_z_range(boss.mesh, boss.arm, act, M.frames_of(boss.frames[name], 6))[1]
        if hi < -margin + 0.02:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    ad = adapter()
    boss.key("Split", split, {"burst_frame": SPLIT_BURST, "spawn_offsets": SPAWN, "giant_gone_frame": SPLIT_BURST + 8,
                              "note": "at 66% (BOSSES.md); spawn the three shards at burst_frame at their offsets "
                                      "(x right, up, forward) and hide the giant after the clip"})
    boss.key("Rejoin", rejoin, {"form_frame": REJOIN_FORM, "spires": CORE_OF,
                                "note": "at 33%: fly the surviving shards to the giant during frames 0-24 and remove "
                                        "them at form_frame; ends in the phase-3 (rejoined) look"})
    boss.key("Roar", roar, {"jaw_wide_frames": [18, 54]})
    fn, facts = M.climb_up(ad, CLIMB_H, trail=1.3)
    boss.key("Climb_Up", fn, facts)
    fn, facts = M.climb_loop(ad)
    boss.key("Climb_Loop", fn, facts)
    fn, facts = M.climb_down(ad, CLIMB_H, trail=0.3, lead=1.8)
    boss.key("Climb_Down", fn, facts)
    depth, top = fit_depth(boss, lambda d: M.submerged_clip(ad, d, frames=80), "Submerged_Loop", 3.0)
    boss.info["Submerged_Loop"]["mesh_top_m"] = top
    fn, facts = M.dive_clip(ad, depth, 2.8, frames=66)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = M.burst_clip(ad, depth, 2.8, frames=64, breach=16, land=32)
    boss.key("Surface_Burst", fn, facts)
    return ad, depth


def stage_shards(out):
    """The three shards side by side (Idle / Fly) at game scale next to each other, and strips of their clips."""
    paths = {}
    for name in SHARDS:
        C.reset_scene()
        with bpy.data.libraries.load(os.path.join(C.BLEND, name + ".blend"), link=False) as (src, dst):
            dst.objects = [n for n in src.objects if n in (name, name + "_Rig")]
            dst.actions = list(src.actions)
        arm = next(o for o in dst.objects if o.type == "ARMATURE")
        for o in dst.objects:
            C.link(o)
        acts = {a.name: a for a in bpy.data.actions}
        C.preview_world()
        C.light_rig(scale=1.2)
        z = 0.0 if name != FLYER else -1.2
        C.ground(z=z)
        target = Vector((0, 0, 0.7 if name != FLYER else 0.0))
        cam = C.closeup_camera(target, 4.5, azimuth=-38, elevation=22, lens=50)
        move = "Fly" if name == FLYER else "Walk"
        M.strip(name, arm, acts[move], cam, out, move, M.frames_of(acts[move].frame_range[1] - 1, 4))
        M.strip(name, arm, acts["Spawn"], cam, out, "Spawn", [0, 6, 12, 18, 24, 30])
        M.strip(name, arm, acts["Rejoin"], cam, out, "Rejoin", [0, 6, 12, 18, 24, 30])
        M.strip(name, arm, acts["Death"], cam, out, "Death", M.frames_of(acts["Death"].frame_range[1], 6))
        if name in CLIMBS:
            stage_climb(name, arm, acts, out, ground=bpy.data.objects.get("Ground"))
        paths[name] = M.turntable(name, arm, acts["Idle"], 8, target, 4.5, out)
    return paths


def stage_climb(name, arm, acts, out, ground=None):
    """Round 29: side strips of the shard climbing a SHARD_CLIMB_H ruin ledge, up and down, plus the crawl on top
    and one game-camera frame mid climb."""
    facts = CLIMBS[name][1]
    up, down = facts["Climb_Up"], facts["Climb_Down"]
    props = M.ledge_props(up["edge_forward_m"], SHARD_CLIMB_H, half=2.0)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 1.2), 9.0, elevation=8)
    M.strip(name, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(up["frames"]))
    game = C.game_camera(Vector((0, -up["edge_forward_m"], 0.8)), 640 / C.GAME_PIXELS_PER_UNIT * 0.45)
    M.strip(name, arm, acts["Climb_Up"], game, out, "Climb_Up_game", M.frames_of(up["frames"], 4))
    M.remove(props + [cam, game])
    if ground is not None:
        ground.location.z = -SHARD_CLIMB_H
    props = M.ledge_props(down["edge_forward_m"], SHARD_CLIMB_H, half=2.0, down=True)
    cam = M.side_cam((0, -down["end_offset_forward_m"] * 0.5, 0.3), 9.0, elevation=8)
    M.strip(name, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(down["frames"]))
    M.remove(props + [cam])
    if ground is not None:
        ground.location.z = 0
    cam = C.closeup_camera(Vector((0, 0, 0.5)), 4.5, azimuth=-60, elevation=18, lens=50)
    M.strip(name, arm, acts["Climb_Loop"], cam, out, "Climb_Loop", M.frames_of(facts["Climb_Loop"]["frames"] - 1, 4))
    M.remove([cam])


def previews(boss, ad, depth, out):
    arm = boss.arm
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    M.preview_scene()
    acts = boss.new
    ground = C.ground()
    cam = C.closeup_camera(Vector((0, 0, 1.6)), 15, azimuth=-38, elevation=20, lens=50)
    M.strip(NAME, arm, acts["Split"], cam, out, "Split", [0, 10, 18, 24, SPLIT_BURST, 30, 33, 40])
    M.strip(NAME, arm, acts["Rejoin"], cam, out, "Rejoin", [12, 20, 26, REJOIN_FORM, 38, 46, 58, REJOIN_FRAMES])
    M.strip(NAME, arm, acts["Roar"], cam, out, "Roar", [0, 12, 22, 34, 46, 70])
    M.remove([cam])
    up = boss.info["Climb_Up"]
    props = M.ledge_props(up["edge_forward_m"], CLIMB_H)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 1.8), 16)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    ground.location.z = -CLIMB_H
    props = M.ledge_props(boss.info["Climb_Down"]["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -boss.info["Climb_Down"]["end_offset_forward_m"] * 0.5, 0.6), 16)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    M.remove(props + [cam])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("water", ad.foot_front + 0.3, length=10)
    cam = M.side_cam((0, -1.8, 0.0), 17, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("water", -10, length=10 + 2.8 + ad.foot_back - 0.4)
    cam = M.side_cam((0, -1.4, 0.0), 17, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    M.remove(props + [cam])
    C.ground()
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, 0, 1.6), 13.5, out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if not args.get("skip-shards"):
        build_shards()
        if not args.get("norender"):
            stage_shards(out)
    if args.get("only-shards"):
        return
    clip_scales = {c: BS.SCALE_BONES for c in BS.CLIPS}
    clip_scales.update(SPLIT_REJOIN)
    boss = M.Boss(NAME, list(BS.CLIPS), scale_bones=GIANT_SCALE, clip_scales=clip_scales)
    ad = adapter()
    boss.add_foot_markers(ad.limbs())
    ad, depth = key_all(boss)
    # Round 45: eyes, teeth and claws (shattered_detail.py) are rebuilt and exported with the boss.
    SHD.attach(boss)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "water (any pool)", "submerged_depth_m": depth,
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "shards": {n: f"../{n}/{n}.fbx" for n in SHARDS}, "spires": CORE_OF,
                 "phases_round28": {"1 whole": "phase-1 table, clips Walk/Idle/Hit/Climb_*/Dive...",
                                    "2 split": "the giant is gone; three shards on their routes",
                                    "3 rejoined": "phase-3 table (Rejoin ends in it); dead shards' Core_* at 0.35"},
                 "superseded": "PhaseBreak1/PhaseBreak2 (round 16) are not used by the split design; keep them for "
                               "a damage stagger or drop them",
                 "mechanic": "Splitting: Split at 66%, three shards (runner, flyer, crawler) on three routes, Rejoin "
                             "at 33% where they are"})
    boss.save()
    if not args.get("norender"):
        previews(boss, ad, depth, out)


if __name__ == "__main__":
    main()
