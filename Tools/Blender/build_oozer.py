"""Oozer: the slug on a toxic slime foot whose trail hastes other species and burns the hero and gates (round 10,
see ArtSource/DESIGN_NOTES.md).

A long low body on a wide translucent slime foot (lime rim, pedal-wave stripes on the sole); a ridged mantle with
glowing toxin glands of mixed size and a row of soft dorsal spines; a round lamprey mouth with a ring of hooked teeth
around a glowing gullet; four eye stalks (a long front pair, a short rear pair), each with a glowing bulb, on their
own bones. Dark olive-brown mottled hide so the lime glow carries.

Clips: Crawl (loop: a pedal wave runs from tail to head), Idle (loop), Hit, Death. The stalks retract by bone scale
(Hit, Death). Also exports Oozer_LOD1. Geometry and clips take a `cfg` so the SlimeMonarch can reuse them.

Run: blender --background --python Tools/Blender/build_oozer.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "Oozer"
LOD_NAME = "Oozer_LOD1"
K = 1.0
GAME_TRIS = 3800
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("membrane", "chitin", "crystal")
GAME_SCALE = 0.7

# Species colours from ProjectGenerator.Bestiary: lime body (0.55, 0.8, 0.15), yellow eyes, lime accent and slime.
# The hide is pushed to a dark olive-brown so the lime glands, foot and eyes carry the species colour.
PAL = H.Palette(top=(0.3, 0.3, 0.1), side=(0.13, 0.12, 0.04), belly=(0.05, 0.045, 0.02), seam=(0.03, 0.025, 0.01),
                dark=(0.09, 0.07, 0.03), glow=(0.66, 1.0, 0.16), eye=(1.0, 0.98, 0.5), membrane=(0.4, 0.55, 0.08),
                bone=(0.85, 0.82, 0.6))

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

SPINE = [G(0, 0.12, -0.44), G(0, 0.13, -0.18), G(0, 0.14, 0.08), G(0, 0.14, 0.29), G(0, 0.13, 0.46)]
BODY_BONES = ["Body_1", "Body_2", "Body_3", "Head"]
# Eye stalks: (root, tip) on the right side; 1 = long front pair, 2 = short rear pair.
STALKS = {1: ((0.045, 0.2, 0.37), (0.1, 0.4, 0.46)), 2: ((0.075, 0.21, 0.29), (0.15, 0.31, 0.27))}

CFG = {"glands": 40, "gland_size": 0.2, "stalks": STALKS, "spines": 5}


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def build_parts(cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    chain = list(BODY_BONES)
    b.ellipsoid("Foot", G(0, 0.04, 0.0), (0.21, 0.47, 0.045), bone="Body_2", style=K2.slime_foot(PAL, k, waves=10.0, edge_glow=0.75),
                segments=(48, 20))
    b.parts[-1].chain = chain
    b.ellipsoid("Body", G(0, 0.12, -0.03), (0.15, 0.44, 0.1), bone="Body_2", segments=(48, 24),
                style=K2.slug_hide(PAL, k, glands=12, seed=3, gland_size=0.12, gland_zone=0.5))
    b.parts[-1].chain = chain
    b.ellipsoid("Mantle", G(0, 0.185, -0.07), (0.168, 0.3, 0.085), Matrix.Rotation(math.radians(3), 3, "X"), bone="Body_2",
                segments=(48, 24), style=K2.slug_hide(PAL, k, glands=cfg["glands"], seed=5, gland_size=cfg["gland_size"],
                                                     gland_zone=0.05, grooves=7.0))
    b.parts[-1].chain = ["Body_1", "Body_2", "Body_3"]
    b.ellipsoid("Head", G(0, 0.15, 0.36), (0.1, 0.12, 0.085), bone="Head", segments=(32, 16),
                style=K2.slug_hide(PAL, k, glands=8, seed=7, gland_size=0.12, gland_zone=0.6))
    b.parts[-1].chain = ["Head", "Body_3"]
    # The ring mouth: a lip ring, hooked teeth pointing in, a glowing gullet.
    mouth = G(0, 0.12, 0.465)
    b.ring("Lip", mouth, G(0, 0.15, 1.0), 0.05, 0.016, arcs=3, sides=8, chain=["Head"],
           style=H.band_plates(PAL, k, 1, height=0.0), ref=Vector((1, 0, 0)))
    b.ellipsoid("Gullet", mouth + G(0, 0.0, -0.012), (0.04, 0.02, 0.04), bone="Head", style=H.inner_glow(PAL, 1.0), segments=(16, 8))
    for i in range(8):
        a = 2 * math.pi * i / 8
        root = mouth + G(math.cos(a) * 0.05, math.sin(a) * 0.05, 0.004)
        tip = mouth + G(math.cos(a) * 0.018, math.sin(a) * 0.018, 0.03)
        b.tube("Tooth", [root, root.lerp(tip, 0.5) + G(0, 0, 0.012), tip], [0.009, 0.006, 0.001], sides=6, chain=["Head"],
               style=H.hook(PAL, k, glow=0.5))
    for i in range(cfg["spines"]):
        f = 0.1 - 0.11 * i
        base = G(0, 0.29 - 0.004 * i * i, f)
        b.tube("DorsalSpine", [base, base + G(0, 0.04, -0.02), base + G(0, 0.05, -0.06)], [0.016, 0.009, 0.0015], sides=8,
               chain=["Body_2" if f < -0.05 else "Body_3"], style=H.hook(PAL, k, glow=0.8, glow_from=0.6))
    for side, sx in SIDES:
        for n, (root, tip) in cfg["stalks"].items():
            r, t = gp(root, sx), gp(tip, sx)
            mid = r.lerp(t, 0.5) + gp((0.015, 0.0, -0.02), sx)
            b.tube("Stalk", [r, mid, t], [0.022, 0.013, 0.011], sides=8, chain=[f"Stalk_{side}{n}"],
                   style=H.tube(PAL, k, PAL.side, PAL.top, rings=(0.3, 0.5, 0.7), ring_dark=0.5))
            b.ellipsoid("EyeBulb", t + (t - mid).normalized() * 0.012, (0.024, 0.024, 0.022), bone=f"Stalk_{side}{n}",
                        style=H.eye(PAL, k, 0.9), segments=(14, 8))
    return b


def bones(cfg=CFG):
    out = [("Root", G(0, 0, 0), G(0, 0, 0.2), None, True)]
    parent = "Root"
    for i, name in enumerate(BODY_BONES):
        out.append((name, SPINE[i], SPINE[i + 1], parent, True))
        parent = name
    for side, sx in SIDES:
        for n, (root, tip) in cfg["stalks"].items():
            out.append((f"Stalk_{side}{n}", gp(root, sx), gp(tip, sx), "Head", True))
    return out


STALK_NAMES = [f"Stalk_{s}{n}" for s, _ in SIDES for n in (1, 2)]


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, wave=(0.0, 0.0), sway=(0.0, 0.0), lift=0.0, head=0.0, stalks=None, retract=1.0):
    """wave: (amplitude, phase) of the vertical pedal wave along the body; sway: (amplitude, phase) sideways;
    lift: head end raised; stalks: {name: (pitch, yaw)}; retract: stalk scale (1 out, small = pulled in)."""
    amp, ph = wave
    samp, sph = sway
    specs = {"Root": {"rot": []}}
    for i, name in enumerate(BODY_BONES):
        a = amp * math.sin(ph - i * 1.3)
        s_ = samp * math.sin(sph - i * 0.9)
        rots = [(Z, s_), (X, -a)]
        if name == "Body_1":
            specs[name] = {"rot": rots, "loc": offset if offset is not None else Vector()}
        else:
            specs[name] = {"rot": rots}
    specs["Body_3"]["rot"].append((X, -lift * 0.5))
    specs["Head"]["rot"].append((X, -lift * 0.5 + head))
    for name in STALK_NAMES:
        p, y = (stalks or {}).get(name, (0.0, 0.0))
        specs[name] = {"rot": [(Z, y), (X, p)], "scale": retract}
    return specs


def stalk_wander(t, amount=1.0, speed=1):
    w = 2 * math.pi * t * speed
    out = {}
    for j, name in enumerate(STALK_NAMES):
        out[name] = (math.radians(10) * amount * math.sin(w + j * 1.7), math.radians(16) * amount * math.sin(w * (1 + j % 2) + j * 2.3))
    return out


CRAWL_FRAMES = 40
CRAWL_DISTANCE = 0.3  # model units per loop for playback speed (a slug glides; the wave is the cue)


def crawl_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return body_specs(offset=Vector((0, 0, 0.004 * math.sin(2 * w))), wave=(math.radians(3.5), 2 * w),
                          sway=(math.radians(4), w), lift=math.radians(3) + math.radians(2) * math.sin(w),
                          head=math.radians(3) * math.sin(2 * w + 1), stalks=stalk_wander(t, 0.8))
    return CRAWL_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return body_specs(wave=(math.radians(1.5), w), sway=(math.radians(3), w), lift=math.radians(5) * (0.5 + 0.5 * math.sin(w)),
                          head=math.radians(4) * math.sin(w + 1), stalks=stalk_wander(t, 1.3, 2))
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t, 0.3, 6.0, 12.0)
        return body_specs(offset=Vector((0, 0.02 * k, -0.01 * k)), wave=(math.radians(6) * k, 1.0), lift=-math.radians(8) * k,
                          head=-math.radians(10) * k, stalks={n: (math.radians(30) * k, 0.0) for n in STALK_NAMES},
                          retract=1.0 - 0.45 * max(k, 0.0))
    return 12, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.7, t)
        writhe = math.exp(-t * 4) * math.sin(t * 30)
        return body_specs(offset=Vector((0, 0, 0.015 * jerk - 0.05 * fall)), wave=(math.radians(10) * writhe, t * 20),
                          sway=(math.radians(14) * fall, 1.2), lift=math.radians(15) * jerk - math.radians(10) * fall,
                          head=-math.radians(15) * fall, stalks={n: (math.radians(70) * fall, math.radians(20) * fall) for n in STALK_NAMES},
                          retract=1.0 - 0.6 * fall)
    return 40, spec, False


CLIPS = {"Crawl": crawl_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
LOD_DROP = {"Stalk_L2": "Head", "Stalk_R2": "Head"}
LOD_AIMS = {}
RIGID = {"Lip", "Tooth"}


def build_asset(name, builder, tris, voxel=0.0034, k=K):
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.08,
                              extrusion=0.012, ray=0.04, emission_strength=2.5,
                              extra=H.weathering(k, scratches=0.2, dust=0.25, seed=17), specular=0.45,
                              mirror=True, k=k, families=FAMILIES, low_size=LOW_TEX)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = build_asset(NAME, builder, GAME_TRIS)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm, STALK_NAMES)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      STALK_NAMES)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "scale_keyed_bones": STALK_NAMES,
                          "crawl_distance_per_loop": CRAWL_DISTANCE, "unity_scale": GAME_SCALE,
                          "pivot": "ground, body centre", "trail_origin": "under Body_1 (rear), on the ground",
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    layout = [("Crawl", 0, (-1.0, 0.6), 0.2), ("Crawl", 12, (0.3, 0.9), -0.15), ("Crawl", 25, (1.3, 0.3), 0.3),
              ("Idle", 30, (-0.4, -0.5), 0.0), ("Crawl", 33, (0.9, -0.7), -0.3)]
    K2.enemy_previews("oozer", arm, asset.low, actions, lod, out, asset, target=(0, 0.0, 0.15), distance=2.2,
                      game_scale=GAME_SCALE, move=("Crawl", 4), layout=layout,
                      strips=[("Crawl", "side", range(0, CRAWL_FRAMES, 5)), ("Death", "close", range(0, 41, 6))],
                      macros=[("glands", G(0.05, 0.28, -0.1), 0.45, -50, 40), ("mouth", G(0.0, 0.12, 0.46), 0.4, -20, 10),
                              ("foot", G(-0.18, 0.04, -0.1), 0.4, -80, 15)],
                      rig_scale=1.1)


if __name__ == "__main__":
    main()
