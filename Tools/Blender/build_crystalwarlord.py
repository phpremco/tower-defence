"""CrystalWarlord: the Bulwark boss: a sweeping sword and crystal that regrows (round 10, see
ArtSource/DESIGN_NOTES.md).

The Bulwark's body plan (build_bulwark.py) as the shield king: a tower shield of bone on the left arm, a long Crysium
greatsword grown from the right arm, a crown of Crysium shards growing out of the helm in place of the horns, Crysium
clusters breaking through both shoulder plates and a ridge of Crysium along the back (the crystal that regrows).
Built in Bulwark units, then scaled as a whole (creature_kit2.scale_rig). Not mirror-symmetric (one material, no
mirrored UVs).

Clips: Walk, Idle, Hit, Death, Block (from the Bulwark) and Sweep (the power: wind-up, a wide horizontal swing; the
blade passes the front at SWEEP_HIT_FRAME). Also exports CrystalWarlord_LOD1.

Run: blender --background --python Tools/Blender/build_crystalwarlord.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_bulwark as BW  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
from common import G  # noqa: E402

NAME = "CrystalWarlord"
LOD_NAME = "CrystalWarlord_LOD1"
GAME_TRIS = 14000
LOD_TRIS = 6000
M = 2.1
GAME_SCALE = 1.6 * 0.6
BULWARK_SCALE = 0.9

CFG = dict(BW.CFG)
CFG.update({"sword_len": 0.9, "sword_width": 0.068, "shield": (0.04, 0.26, 0.34), "horns": 0})


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def build_parts():
    b = BW.build_parts(CFG)
    k = BW.K
    # Crown: five Crysium shards growing from the helm, the middle tallest, leaning back.
    for i, (x, f, h, lean) in enumerate(((0.0, 0.3, 0.17, -0.25), (0.04, 0.27, 0.13, -0.35), (0.07, 0.23, 0.1, -0.45))):
        for sx in ((1,) if x == 0 else (-1, 1)):
            b.shard("Crown", G(x * sx, 0.95, f), G(0.45 * sx * (x > 0), 1.0, lean), 0.024, h, sides=8, twist=2.2, flute=0.4,
                    bone="Head", style=Y.shard_style("pink", 1.0, seed=70 + i))
    # Crystal breaking through the shoulder plates and along the back.
    for side, sx in BW.SIDES:
        sh = gp(BW.SHOULDER, sx)
        for j, (o, d, h) in enumerate((((0.05, 0.06, 0.02), (0.5, 1.0, -0.2), 0.13), ((0.09, 0.04, -0.03), (0.9, 0.8, -0.3), 0.1),
                                       ((0.03, 0.05, -0.05), (0.2, 1.0, -0.6), 0.09))):
            b.shard("ShoulderCrystal", sh + gp(o, sx), gp(d, sx), 0.02, h, sides=6, twist=1.6, flute=0.35,
                    bone=f"UpperArm_{side}", style=Y.shard_style("pink", 0.9, seed=80 + j + (sx > 0) * 3))
    for i, f in enumerate((0.05, -0.06, -0.16)):
        b.shard("BackCrystal", G(0, 0.8 - 0.05 * i, f), G(0, 1.0, -0.8), 0.022, 0.12 - 0.02 * i, sides=6, twist=1.8,
                flute=0.35, bone="Spine", style=Y.shard_style("pink", 0.9, seed=90 + i))
    return b


SWEEP_FRAMES = 40
SWEEP_HIT_FRAME = 20


def sweep_spec(poser):
    """The arm comes up level (shoulder out), the blade is cocked out to the right at chest height, then swings
    flat across the front to the left (the forearm unbends) while the chest twists after it. Angles from a probe of
    the sword tip (see the README: the arm's `out` turns the right arm inwards on this rig)."""
    d = math.radians
    hit = SWEEP_HIT_FRAME / SWEEP_FRAMES

    def lerp(a, b, x):
        return a + (b - a) * x

    def spec(t, f):
        up = K2.pulse(t, 0.0, 0.3, 0.97)
        swing = K2.ease(0.38, hit + 0.1, t)
        cocked = up * (1 - swing)
        twist = d(30) * cocked - d(40) * swing * up
        specs = BW.body_specs(offset=Vector((0, 0, BW.CROUCH - 0.05 * up)), lean=d(8) + d(6) * swing * up,
                              twist=twist, roll=d(4) * cocked - d(5) * swing * up, neck=-d(4), head=-d(4) * up,
                              head_yaw=-twist * 0.5, jaw=d(20) * swing * up, tail=(d(14) * (swing - 0.5) * up, 1.0, d(6) * up),
                              sword=(lerp(d(5), lerp(d(40), d(15), swing), up), lerp(d(6), d(80), up),
                                     lerp(d(10), lerp(d(60), -d(35), swing), up)),
                              shield=(d(10) + d(10) * up, d(4) + d(8) * up, d(15) + d(15) * up))
        return BW.place_legs(poser, specs, {key: lg.foot for key, lg in BW.legs().items()})
    return SWEEP_FRAMES, spec, False


CLIPS = dict(BW.CLIPS)
CLIPS["Sweep"] = sweep_spec
LOD_DROP = dict(BW.LOD_DROP)
LOD_AIMS = dict(BW.LOD_AIMS)
RIGID = BW.RIGID | {"Crown", "ShoulderCrystal", "BackCrystal"}


def main():
    BW.asymmetric_decimate()
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = BW.build_asset(NAME, builder, GAME_TRIS, voxel=0.0026)
    bone_list = BW.bones(CFG)
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    K2.scale_rig(arm, [asset.low], actions.values(), M)
    K2.scale_rig(lod[0], [lod[1]], lod[2].values(), M)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), arm, asset.low, actions)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    C.write_report(NAME, {"asset": NAME, "base_species": "Bulwark", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": BW.TEX,
                          "low_texture_size": BW.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames, "mirrored_uvs": False,
                          "sweep_hit_frame": SWEEP_HIT_FRAME, "block_up_frame": BW.BLOCK_UP_FRAME,
                          "walk_distance_per_loop": round(BW.STRIDE / BW.STANCE * M, 3), "unity_scale": GAME_SCALE,
                          "model_scale_from_bulwark_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    B.boss_previews(
        "crystalwarlord", arm, asset.low, actions, lod, out, asset, target=(0, 0, 0.6 * M), distance=3.2 * M,
        game_scale=GAME_SCALE, base=dict(asset="Bulwark", scale=BULWARK_SCALE, clip="Walk", frame=6),
        move=("Walk", 4), power=("Sweep", SWEEP_HIT_FRAME),
        strips=[("Walk", "side", range(0, BW.WALK_FRAMES, 4)), ("Sweep", "front", range(0, SWEEP_FRAMES, 5)),
                ("Block", "front", range(0, BW.BLOCK_FRAMES, 4)), ("Death", "close", range(0, 45, 6))],
        macros=[("crown", G(0.0, 1.0, 0.28) * M, 0.45 * M, -30, 20), ("shield", G(-0.38, 0.58, 0.12) * M, 0.55 * M, -80, 10),
                ("sword", G(0.32, 0.25, 0.5) * M, 0.5 * M, -10, 20)],
        rig_scale=2.4, minions=[(-1.5, 0.9, 0.2, 3), (-1.0, -0.7, -0.1, 10), (1.4, 0.8, 0.3, 17)],
        extra_shots=[("block", "front", "Block", 20)])


if __name__ == "__main__":
    main()
