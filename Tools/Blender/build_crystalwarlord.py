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
import crystal_growth as G49  # noqa: E402
import uvlayout  # noqa: E402
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


def crystal(seed, strength=1.0):
    """Crysium whose light comes in pockets and differs per shard (round 49D, crystal_growth.uneven_glow)."""
    return G49.uneven_glow(Y.shard_style("pink", strength, seed=seed), seed, 0.35, 1.3)


def seated(base, normal, depth):
    """Round 49F (Remco: "watch out for floating crystals"): a cluster's base pushed `depth` into the body along its
    normal, so the satellites spread round the dominant shard (which stand off a curved helm or plate) still start
    inside it."""
    return Vector(base) - Vector(normal).normalized() * depth


def build_parts():
    """Round 49D (Remco: "same as the normal one" + "crystals too perfect"): the Bulwark's round-49D body (chipped
    shield, hewn head, serrated sword) as the king, and its crystal as irregular growth: a lopsided crown of
    fractured, bent and spiralling shards bursting from the helm with fragments hovering over it, uneven clusters
    tearing through the shoulder plates (the left one larger), and a broken ridge down the back."""
    b = BW.build_parts(CFG)
    # Crown: one cluster from the back of the helm, leaning back and to the left, plus a smaller one on the right.
    # Round 49F: no hovering fragments (Remco read them as loose parts); every cluster sits a little into the body on
    # a lumpy root mound, so every shard grows out of it.
    n = G(0.08, 1.0, -0.45)
    G49.growth_cluster(b, seated(G(0.0, 0.955, 0.29), n, 0.03), n, 0.22, 4901, bone="Head", style=crystal(70, 1.0),
                       count=7, spread=0.3, lean=(0.15, 0.75), sideways=G(-0.25, 0.0, -0.3), floating=0, spiral=1,
                       bent=2, radius_ratio=0.16, name="Crown", root=(crystal(75, 0.8), 0.06, 0.025))
    n = G(0.5, 1.0, -0.5)
    G49.growth_cluster(b, seated(G(0.06, 0.95, 0.22), n, 0.025), n, 0.12, 4907, bone="Head", style=crystal(72, 0.9),
                       count=4, spread=0.3, lean=(0.2, 0.7), floating=0, spiral=0, bent=1, radius_ratio=0.17, name="Crown",
                       root=(crystal(76, 0.8), 0.035, 0.018))
    # Crystal tearing through the shoulder plates, uneven: the shield side carries the larger growth.
    for side, sx in BW.SIDES:
        sh = gp(BW.SHOULDER, sx)
        size, count = (0.15, 6) if sx < 0 else (0.11, 4)
        n = gp((0.55, 1.0, -0.3), sx)
        G49.growth_cluster(b, seated(sh + gp((0.06, 0.055, -0.01), sx), n, 0.035), n, size * 1.1, 4911 + (sx > 0),
                           bone=f"UpperArm_{side}", style=crystal(80 + (sx > 0) * 3, 0.9), count=count, spread=0.28,
                           lean=(0.2, 0.8), floating=0, spiral=1, bent=1, radius_ratio=0.17,
                           name="ShoulderCrystal", root=(crystal(84 + (sx > 0), 0.8), size * 0.38, size * 0.16))
    # A broken ridge down the back: three clusters of falling size, the middle one with a spiral horn.
    for i, (f, size) in enumerate(((0.06, 0.12), (-0.06, 0.15), (-0.17, 0.09))):
        n = G(0.1 * (i - 1), 1.0, -0.7)
        # The back slopes away under the lower clusters: they sit deeper (the chest's surface, not the bone line).
        G49.growth_cluster(b, seated(G(0.01 * (i - 1), 0.8 - 0.05 * i, f), n, 0.03 + 0.04 * i), n, size * 1.1,
                           4921 + i, bone="Spine", style=crystal(90 + i, 0.9), count=3 + (i == 1), spread=0.25,
                           lean=(0.1, 0.6), floating=0, spiral=int(i == 1), bent=1, radius_ratio=0.16,
                           name="BackCrystal", root=(crystal(94 + i, 0.8), size * 0.35, size * 0.15))
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
        # Round 49F: the head leads the swing: it holds the target while the chest winds up, then turns into the
        # sweep a few frames ahead of the blade and dips with it; the tail swings out against the twist.
        lead = K2.ease(0.3, hit + 0.02, t) * up
        specs = BW.body_specs(offset=Vector((0, 0, BW.CROUCH - 0.05 * up)), lean=d(8) + d(6) * swing * up,
                              twist=twist, roll=d(4) * cocked - d(5) * swing * up, neck=-d(4) - d(4) * cocked + d(6) * lead,
                              head=-d(6) * cocked + d(9) * lead, head_yaw=-twist * 0.6 - d(14) * lead + d(6) * swing * up,
                              head_roll=d(5) * cocked - d(6) * lead, jaw=d(20) * swing * up,
                              tail=(-d(12) * cocked + d(18) * swing * up, 1.0, d(6) * up),
                              sword=(lerp(d(5), lerp(d(40), d(15), swing), up), lerp(d(6), d(80), up),
                                     lerp(d(10), lerp(d(60), -d(35), swing), up)),
                              shield=(d(10) + d(10) * up, d(4) + d(8) * up, d(15) + d(15) * up))
        return BW.place_legs(poser, specs, {key: lg.foot for key, lg in BW.legs().items()})
    return SWEEP_FRAMES, spec, False


CLIPS = dict(BW.CLIPS)
CLIPS["Sweep"] = BW.follow(sweep_spec)
LOD_DROP = dict(BW.LOD_DROP)
LOD_AIMS = dict(BW.LOD_AIMS)
RIGID = BW.RIGID | {n + suffix for n in ("Crown", "ShoulderCrystal", "BackCrystal") for suffix in ("", "Fragment", "Root")}


def main():
    BW.asymmetric_decimate()
    uvlayout.SPARSE_FIX = True  # round 49D: crystal clusters cut into many small islands
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = BW.build_asset(NAME, builder, GAME_TRIS, voxel=0.0026)
    specks = BW.drop_specks(asset.low)
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
                          "model_scale_from_bulwark_units": M, "round49f_specks_dropped": specks,
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
    # Round 45: eyes, teeth and claws (crystalwarlord_detail.py) on the saved .blend, exported again with the body.
    import crystalwarlord_detail as CWD
    CWD.main()
