"""SlimeMonarch: the Oozer boss: floods of slime and more oozers (round 10, see ArtSource/DESIGN_NOTES.md).

The Oozer's body plan (build_oozer.py) as a monarch: a crown of six eye stalks (three pairs, the front ones long),
a broader mantle crowded with toxin glands, a crest of glowing spines, a wide slime foot and two brood sacs on the
back (on their own bones, bone scale) where young oozers come out. Built in Oozer units, then scaled as a whole
(creature_kit2.scale_rig).

Clips: Crawl, Idle, Hit, Death (from the Oozer, with the sacs), Flood (the power: it rears up and slams the front
down; slime bursts out at FLOOD_SLAM_FRAME) and Spawn (the sacs squeeze; young oozers leave at SPAWN_RELEASE_FRAME
from the Sac bones). Also exports SlimeMonarch_LOD1.

Run: blender --background --python Tools/Blender/build_slimemonarch.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_oozer as OZ  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "SlimeMonarch"
LOD_NAME = "SlimeMonarch_LOD1"
GAME_TRIS = 13000
LOD_TRIS = 6000
M = 2.0
GAME_SCALE = 1.5 * 0.6
OOZER_SCALE = 0.7

X, Yv, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
SIDES = OZ.SIDES

STALKS = {1: ((0.04, 0.21, 0.37), (0.09, 0.44, 0.47)), 2: ((0.07, 0.22, 0.31), (0.16, 0.4, 0.33)),
          3: ((0.085, 0.22, 0.25), (0.2, 0.33, 0.2))}
CFG = dict(OZ.CFG)
CFG.update({"glands": 56, "gland_size": 0.17, "stalks": STALKS, "spines": 6})
# The base module's stalk list drives its clip specs: point it at the monarch's six stalks.
OZ.STALK_NAMES = [f"Stalk_{s}{n}" for s, _ in SIDES for n in STALKS]
SAC = (0.1, 0.27, -0.2)
SCALE_BONES = OZ.STALK_NAMES + ["Sac_L", "Sac_R"]


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def build_parts():
    b = OZ.build_parts(CFG)
    k = OZ.K
    for side, sx in SIDES:
        b.ellipsoid("Sac", gp(SAC, sx), (0.075, 0.11, 0.07), bone=f"Sac_{side}", segments=(32, 16),
                    style=K2.ribbed_bladder(OZ.PAL, k, ribs=8, seed=21 + (sx > 0), glow=0.75, plate=0.45))
    # A crest of glowing spines round the back of the head: the crown of the slugs.
    for j in range(5):
        a = math.radians(-60 + 30 * j)
        base = G(0.08 * math.sin(a), 0.22, 0.3 - 0.05 * math.cos(a))
        out = G(math.sin(a) * 0.5, 1.0, -0.6).normalized()
        b.tube("CrownSpine", [base, base + out * 0.05, base + out * 0.09 + G(0, 0, -0.03)], [0.018, 0.01, 0.0015], sides=8,
               chain=["Head"], style=H.hook(OZ.PAL, k, glow=1.0, glow_from=0.5))
    return b


def bones():
    out = OZ.bones(CFG)
    for side, sx in SIDES:
        c = gp(SAC, sx)
        out.append((f"Sac_{side}", c - G(0, 0.0, -0.08), c + G(0, 0.0, -0.1), "Body_2", True))
    return out


def with_sacs(fn, sac=lambda t: 1.0):
    def make(poser):
        frames, spec, loop = fn(poser)

        def full(t, f):
            specs = spec(t, f)
            for side, _ in SIDES:
                specs[f"Sac_{side}"] = {"rot": [], "scale": sac(t)}
            return specs
        return frames, full, loop
    return make


FLOOD_FRAMES = 44
FLOOD_SLAM_FRAME = 24
SPAWN_FRAMES = 48
SPAWN_RELEASE_FRAME = 30


def flood_spec(poser):
    slam = FLOOD_SLAM_FRAME / FLOOD_FRAMES

    def spec(t, f):
        rise = K2.ease(0.0, slam - 0.12, t) * (1 - K2.ease(slam - 0.1, slam, t))
        impact = K2.pulse(t, slam - 0.04, slam + 0.02, 0.9)
        ripple = math.exp(-max(0.0, t - slam) * 6) * math.sin((t - slam) * 50) * (t > slam)
        return OZ.body_specs(offset=Vector((0, 0, -0.02 * impact)), wave=(math.radians(8) * ripple, t * 30),
                             lift=math.radians(55) * rise - math.radians(10) * impact, head=-math.radians(15) * rise + math.radians(10) * impact,
                             stalks={n: (-math.radians(25) * rise + math.radians(30) * impact, 0.0) for n in OZ.STALK_NAMES},
                             retract=1.0 - 0.3 * impact)
    return FLOOD_FRAMES, spec, False


def spawn_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        brace = K2.pulse(t, 0.05, 0.3, 0.95)
        return OZ.body_specs(wave=(math.radians(3) * brace, 3 * w), sway=(math.radians(4) * brace, 2 * w),
                             lift=-math.radians(4) * brace, stalks=OZ.stalk_wander(t, 1.5, 3), retract=1.0)
    return SPAWN_FRAMES, spec, False


def spawn_sac(t):
    rel = SPAWN_RELEASE_FRAME / SPAWN_FRAMES
    swell = 0.35 * K2.pulse(t, 0.05, rel - 0.06, rel) + 0.12 * math.sin(t * 40) * K2.pulse(t, 0.2, 0.4, rel)
    squeeze = -0.3 * K2.pulse(t, rel - 0.03, rel + 0.05, 0.95)
    return 1.0 + swell + squeeze


CLIPS = {
    "Crawl": with_sacs(OZ.crawl_spec, lambda t: 1.0 + 0.05 * math.sin(4 * math.pi * t)),
    "Idle": with_sacs(OZ.idle_spec, lambda t: 1.0 + 0.07 * math.sin(2 * math.pi * t)),
    "Hit": with_sacs(OZ.hit_spec, lambda t: 1.0 - 0.12 * max(0.0, K2.recoil(t))),
    "Death": with_sacs(OZ.death_spec, lambda t: 1.0 - 0.45 * K2.ease(0.1, 0.8, t)),
    "Flood": with_sacs(flood_spec, lambda t: 1.0 + 0.2 * K2.pulse(t, 0.1, 0.45, 0.6)),
    "Spawn": with_sacs(spawn_spec, spawn_sac),
}
LOD_DROP = {f"Stalk_{s}3": "Head" for s, _ in SIDES}
LOD_AIMS = {}
RIGID = OZ.RIGID | {"CrownSpine"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = OZ.build_asset(NAME, builder, GAME_TRIS, voxel=0.0024)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm, SCALE_BONES)
    actions = C.key_clips(poser, CLIPS)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      SCALE_BONES)
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    K2.scale_rig(arm, [asset.low], actions.values(), M)
    K2.scale_rig(lod[0], [lod[1]], lod[2].values(), M)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), arm, asset.low, actions)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    C.write_report(NAME, {"asset": NAME, "base_species": "Oozer", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": OZ.TEX,
                          "low_texture_size": OZ.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames,
                          "scale_keyed_bones": SCALE_BONES, "flood_slam_frame": FLOOD_SLAM_FRAME,
                          "spawn_release_frame": SPAWN_RELEASE_FRAME, "spawn_point_bones": ["Sac_L", "Sac_R"],
                          "crawl_distance_per_loop": round(OZ.CRAWL_DISTANCE * M, 3), "unity_scale": GAME_SCALE,
                          "model_scale_from_oozer_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    B.boss_previews(
        "slimemonarch", arm, asset.low, actions, lod, out, asset, target=(0, 0, 0.15 * M), distance=2.3 * M,
        game_scale=GAME_SCALE, base=dict(asset="Oozer", scale=OOZER_SCALE, clip="Crawl", frame=6),
        move=("Crawl", 4), power=("Flood", 16),
        strips=[("Crawl", "side", range(0, OZ.CRAWL_FRAMES, 5)), ("Flood", "side", range(0, FLOOD_FRAMES, 5)),
                ("Spawn", "close", range(0, SPAWN_FRAMES, 6)), ("Death", "close", range(0, 41, 6))],
        macros=[("glands", G(0.06, 0.27, -0.1) * M, 0.45 * M, -50, 40), ("crown", G(0.0, 0.3, 0.33) * M, 0.45 * M, -30, 20),
                ("sac", gp(SAC, 1) * M, 0.4 * M, -70, 30)],
        rig_scale=2.0, minions=[(-1.5, 0.9, 0.2, 3), (-1.0, -0.7, -0.1, 10), (1.4, 0.8, 0.3, 17), (1.6, -0.5, 0.0, 21)],
        extra_shots=[("spawn", "close", "Spawn", SPAWN_RELEASE_FRAME - 4)])


if __name__ == "__main__":
    main()
