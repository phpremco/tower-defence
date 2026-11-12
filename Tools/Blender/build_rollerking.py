"""RollerKing: the Roller boss: a quake with every landing (round 10, see ArtSource/DESIGN_NOTES.md).

The Roller's eight-plate ball (build_roller.py, its geometry and clips reused) in bronze armour: two extra rows of
hooked keels (five saw rings on the ball instead of three), a crown of ember Crysium shards round each flank hub (a
crowned wheel when it rolls), and a pale forward chevron on the scoop plate. Built in Roller units, then scaled as a
whole (creature_kit2.scale_rig).

Clips: Walk, Idle, Hit, Death, Curl, Roll, Uncurl (from the Roller) and Quake (the power: rolled up, it hops and
slams down; impact at QUAKE_IMPACT_FRAME, ends rolled so Uncurl or Roll can follow). Also exports RollerKing_LOD1.

Run: blender --background --python Tools/Blender/build_rollerking.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_roller as R  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
from common import G  # noqa: E402

NAME = "RollerKing"
LOD_NAME = "RollerKing_LOD1"
GAME_TRIS = 14000
LOD_TRIS = 6000
M = 2.1
GAME_SCALE = 1.3 * 0.6
ROLLER_SCALE = 0.7

# Data colour (0.7, 0.4, 0.15): bronze where the Roller is sand; the same amber glow, hotter.
R.PAL = H.Palette(top=(0.66, 0.4, 0.17), side=(0.27, 0.13, 0.05), belly=(0.05, 0.03, 0.02), seam=(0.035, 0.02, 0.01),
                  dark=(0.11, 0.055, 0.025), glow=(1.0, 0.55, 0.12), eye=(1.0, 0.75, 0.3), membrane=(0.42, 0.17, 0.04),
                  bone=(0.96, 0.86, 0.64))
PAL = R.PAL


def build_parts():
    b = R.build_parts()
    k = R.K
    for i in range(1, R.N - 1):
        m = R.plate_transform(i, 0.0)
        for phi in (math.radians(28), -math.radians(28)):
            base = R.keel_point(i, 0.7, phi, -0.008)
            out = base.normalized()
            back = (R.keel_point(i, 1.0, phi) - R.keel_point(i, 0.6, phi)).normalized()
            pts = [m @ base, m @ (base + out * 0.05 + back * 0.01), m @ (base + out * 0.07 + back * 0.05)]
            b.tube("Hook", pts, [0.022, 0.012, 0.0012], sides=10, chain=[f"Band_{i}"],
                   style=H.hook(PAL, k, glow=0.8, glow_from=0.75))
    # Crowned hubs: six ember Crysium shards round each flank hub.
    m = R.plate_transform(3, 0.0)
    for sx in (-1, 1):
        centre = Vector((sx * (R.RB - 0.005), 0, 0))
        for j in range(6):
            a = 2 * math.pi * j / 6 + math.pi / 6
            radial = Vector((0, math.sin(a), math.cos(a)))
            base = centre + radial * 0.06
            direction = (radial * 0.8 + Vector((sx * 0.6, 0, 0))).normalized()
            b.shard("HubCrown", m @ base, m.to_3x3() @ direction, 0.014, 0.06, sides=6, twist=1.5, flute=0.35,
                    bone="Band_3", style=Y.shard_style("ember", 1.0, seed=60 + j))
    return b


QUAKE_FRAMES = 30
QUAKE_IMPACT_FRAME = 16


def quake_spec(poser):
    P = R.Pose(poser)
    imp = QUAKE_IMPACT_FRAME / QUAKE_FRAMES

    def spec(t, f):
        if t < imp:
            s = t / imp
            lift = 0.28 * math.sin(math.pi * min(1.0, s * 1.25)) if s < 0.8 else 0.28 * (1 - (s - 0.8) / 0.2) ** 2 * math.sin(math.pi * 0.8 * 1.25)
            lift = max(lift, 0.0)
            angle = -math.radians(40) * s
            squash = 0.0
        else:
            s = (t - imp) / (1 - imp)
            lift = 0.03 * math.exp(-s * 5) * abs(math.sin(s * 9))
            angle = -math.radians(40) - math.radians(10) * K2.ease(0, 1, s)
            squash = math.exp(-s * 6) * math.cos(s * 14)
        specs = P.specs(1.0 - 0.06 * max(squash, 0.0), R.roll_place(angle, R.BALL_LIFT + lift), head_pitch=math.radians(70),
                        head_pull=0.12, jaw=math.radians(-6))
        return specs
    return QUAKE_FRAMES, spec, False


CLIPS = dict(R.CLIPS)
CLIPS["Quake"] = quake_spec
LOD_DROP = dict(R.LOD_DROP)
LOD_AIMS = dict(R.LOD_AIMS)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0022, target_tris=GAME_TRIS, tex_size=R.TEX, ao_distance=0.08,
                               extrusion=0.012, ray=0.04, emission_strength=2.5,
                               extra=H.weathering(R.K, markings=PAL.dark, marking_amount=0.35, seed=44), specular=0.3,
                               mirror=True, k=R.K, families=R.FAMILIES, low_size=R.LOW_TEX)
    bone_list = R.bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid={"Plate", "Belly", "Hub", "Hook", "Prong", "HubCrown"}, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    centre = R.ball_centre()
    K2.scale_rig(arm, [asset.low], actions.values(), M)
    K2.scale_rig(lod[0], [lod[1]], lod[2].values(), M)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), arm, asset.low, actions)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    C.write_report(NAME, {"asset": NAME, "base_species": "Roller", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": R.TEX,
                          "low_texture_size": R.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames,
                          "quake_impact_frame": QUAKE_IMPACT_FRAME,
                          "walk_distance_per_loop": round(R.STRIDE / 0.5 * M, 3),
                          "ball_radius": round((R.RB + R.EPS) * M, 3), "ball_centre_height": round(centre.z * M, 3),
                          "roll_distance_per_loop": round(2 * math.pi * (R.RB + R.EPS) * M, 3),
                          "unity_scale": GAME_SCALE, "model_scale_from_roller_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        detail(norender=True)
        return
    B.boss_previews(
        "rollerking", arm, asset.low, actions, lod, out, asset, target=(0, -0.05 * M, 0.25 * M), distance=2.5 * M,
        game_scale=GAME_SCALE, base=dict(asset="Roller", scale=ROLLER_SCALE, clip="Walk", frame=6),
        move=("Walk", 4), power=("Quake", 9),
        strips=[("Walk", "side", range(0, 20, 3)), ("Curl", "side", range(0, 15, 2)), ("Quake", "side", range(0, QUAKE_FRAMES, 4)),
                ("Death", "close", range(0, 37, 6))],
        macros=[("plates", G(0.1, 0.4, 0.0) * M, 0.5 * M, -50, 40), ("hub", G(0.3, 0.2, 0.0) * M, 0.4 * M, -80, 10),
                ("head", G(0.0, 0.14, 0.62) * M, 0.4 * M, -30, 15)],
        rig_scale=2.0, minions=[(-1.5, 0.9, 0.2, 3), (-1.0, -0.7, -0.1, 10), (1.4, 0.8, 0.3, 14), (1.6, -0.5, 0.0, 18)],
        extra_shots=[("rolled", "close", "Roll", 6)])
    detail()


def detail(norender=False):
    """Round 45: the close-up detail (eyes, teeth, claws) as extra parts, exported again with the body."""
    import rollerking_detail
    rollerking_detail.run(norender)


if __name__ == "__main__":
    main()
