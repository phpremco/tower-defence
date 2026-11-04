"""AcidEmpress: the Stinger boss: acid rain on the towers and plates that grow back (round 10, see
ArtSource/DESIGN_NOTES.md).

The Stinger's body plan (build_stinger.py) grown wide, low and old: seven glowing armour plates, bigger trap claws,
a crown of magenta Crysium spikes on the carapace, eight eye beads, two short side tails flanking the main one, and
the tail ends in a big ribbed acid spray gland on its own bone (bone scale: it swells and squeezes) with three
nozzles that point up and forward. Built in Stinger units, then scaled as a whole (creature_kit2.scale_rig).

Clips: Walk, Idle, Hit, Death, Strike (from the Stinger), Spray (the power: the tail rears up, the gland squeezes,
acid leaves the nozzles at SPRAY_RELEASE_FRAME). Also exports AcidEmpress_LOD1.

Run: blender --background --python Tools/Blender/build_acidempress.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_stinger as S  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
from common import G  # noqa: E402

NAME = "AcidEmpress"
LOD_NAME = "AcidEmpress_LOD1"
GAME_TRIS = 14500
LOD_TRIS = 6000
# Built in Stinger units; M scales the finished model so that at Unity scale 1.5 x 0.6 = 0.9 she stands about
# 2.7 times a Stinger (as the factory boss does).
M = 2.2
GAME_SCALE = 1.5 * 0.6
STINGER_SCALE = 0.8

X, Yv, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
SIDES = S.SIDES

CFG = dict(S.CFG)
CFG.update({
    "width": 1.22,
    "plates": [(0.06, 0.18), (-0.015, 0.19), (-0.09, 0.195), (-0.16, 0.19), (-0.225, 0.175), (-0.285, 0.155), (-0.335, 0.13)],
    "claw": 1.3,
    "tail": [G(0, 0.235, -0.36), G(0, 0.34, -0.5), G(0, 0.5, -0.56), G(0, 0.66, -0.5), G(0, 0.76, -0.36), G(0, 0.78, -0.2)],
    "tail_radii": [0.07, 0.066, 0.06, 0.054, 0.05, 0.046],
    "bladder": (G(0, 0.76, -0.08), (0.04, 0.05, 0.04)),
    "needle": [G(0, 0.73, 0.0), G(0, 0.7, 0.07), G(0, 0.63, 0.13), G(0, 0.55, 0.15)],
    "legs": [((0.13, 0.17, 0.2), (0.46, 0.0, 0.4)), ((0.15, 0.17, 0.07), (0.52, 0.0, 0.08)),
             ((0.14, 0.17, -0.06), (0.48, 0.0, -0.22))],
    "leg_lengths": [(0.27, 0.3), (0.27, 0.3), (0.26, 0.29)],
    "eyes": 8,
})
GLAND = (G(0, 0.8, -0.13), (0.085, 0.11, 0.075))
SIDE_TAIL = [(0.13, 0.22, -0.3), (0.2, 0.32, -0.42), (0.22, 0.42, -0.4)]

PINK = H.Palette(top=S.PAL.top, side=S.PAL.side, belly=S.PAL.belly, seam=S.PAL.seam, dark=S.PAL.dark, glow=S.PAL.glow,
                 eye=S.PAL.eye, membrane=S.PAL.membrane, bone=S.PAL.bone)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def build_parts():
    b = S.build_parts(CFG)
    k = S.K
    # The crown: magenta Crysium spikes along the carapace rim, tallest at the back, leaning back (barbs of an
    # arrowhead from above).
    for i, (x, f, h) in enumerate(((0.0, 0.3, 0.09), (0.07, 0.26, 0.11), (0.12, 0.2, 0.13), (0.15, 0.13, 0.12))):
        for sx in ((1,) if x == 0 else (-1, 1)):
            b.shard("Crown", G(x * sx, 0.255, f), G(0.35 * sx * (x > 0), 1.0, -0.45), 0.02, h, sides=8, twist=2.0, flute=0.4,
                    bone="Body", style=Y.shard_style("rose", 1.0, seed=40 + i))
    # The spray gland on the sting, with three nozzles pointing up and forward.
    centre, radii = GLAND
    b.ellipsoid("Gland", centre, radii, Matrix.Rotation(math.radians(-35), 3, "X"), bone="Gland",
                style=K2.ribbed_bladder(S.ACID, k, ribs=9, seed=19, plate=0.25), segments=(36, 18))
    for j, (x, d) in enumerate(((0.0, (0.0, 1.0, 0.25)), (0.04, (0.35, 1.0, 0.15)), (-0.04, (-0.35, 1.0, 0.15)))):
        base = centre + G(x, 0.06, -0.01)
        dv = G(*d).normalized()
        b.tube("Nozzle", [base, base + dv * 0.04, base + dv * 0.07], [0.022, 0.016, 0.013], sides=8, chain=["Gland"],
               style=H.tube(S.ACID, k, S.PAL.side, S.ACID.bone, tip_glow=1.0, glow_from=0.6))
    # Two short side tails, each ending in a small acid sting.
    for side, sx in SIDES:
        pts = [gp(p, sx) for p in SIDE_TAIL]
        b.tube("SideTail", pts, [0.042, 0.034, 0.024], sides=10, chain=[f"SideTail_{side}"],
               style=K2.ring_segment(S.ACID, k, joint_glow=0.6, base=S.PAL.top, tip=S.PAL.side * 1.3, rings=3, joint_width=0.06))
        tip = pts[-1]
        b.tube("SideSting", [tip, tip + gp((0.01, 0.04, 0.04), sx), tip + gp((0.0, 0.02, 0.1), sx)], [0.02, 0.012, 0.0015],
               sides=8, chain=[f"SideTail_{side}"], style=H.tube(S.ACID, k, S.PAL.side, S.ACID.bone, tip_glow=1.0, glow_from=0.5))
    return b


def bones():
    out = S.bones(CFG)
    centre, _ = GLAND
    out.append(("Gland", centre + G(0, -0.04, -0.03), centre + G(0, 0.08, 0.02), "Sting", True))
    for side, sx in SIDES:
        out.append((f"SideTail_{side}", gp(SIDE_TAIL[0], sx), gp(SIDE_TAIL[-1], sx), "Abdomen", True))
    return out


SCALE_BONES = ["Gland"]


def with_extras(fn, gland=lambda t: 1.0, side=lambda t: (0.0, 0.0)):
    """Wraps a Stinger clip: adds the gland's scale and the side tails' motion (curl, sway)."""
    def make(poser):
        frames, spec, loop = fn(poser, CFG)

        def full(t, f):
            specs = spec(t, f)
            specs["Gland"] = {"rot": [], "scale": gland(t)}
            curl, sway = side(t)
            for s, sx in SIDES:
                specs[f"SideTail_{s}"] = {"rot": [(Z, -sx * sway), (X, curl)]}
            return specs
        return frames, full, loop
    return make


def breathe(t):
    return 1.0 + 0.04 * math.sin(2 * math.pi * t)


SPRAY_FRAMES = 48
SPRAY_RELEASE_FRAME = 26


def spray_spec(poser, cfg=CFG):
    rel = SPRAY_RELEASE_FRAME / SPRAY_FRAMES

    def spec(t, f):
        rear = K2.ease(0.0, 0.4, t) * (1 - K2.ease(0.8, 1.0, t))
        squeeze = K2.pulse(t, rel - 0.08, rel, rel + 0.25)
        pump = K2.pulse(t, 0.1, rel - 0.1, rel)
        shake = math.sin(t * 90) * K2.pulse(t, rel - 0.05, rel + 0.02, rel + 0.3)
        specs = S.body_specs(offset=Vector((0, 0.015 * rear, -0.02 * rear)), pitch_=-math.radians(5) * rear,
                             belly=-math.radians(4) * rear, tail=(-math.radians(8) * rear, math.radians(3) * shake, 0.0),
                             tail_base=-math.radians(10) * rear, sting=-math.radians(35) * rear + math.radians(10) * squeeze,
                             claws=math.radians(10) * rear, claw_spread=math.radians(20) * rear, pinch=math.radians(30) * rear,
                             claw_raise=math.radians(15) * rear)
        return S.place_legs(poser, specs, {key: lg.foot for key, lg in S.legs(cfg).items()}, cfg)
    return SPRAY_FRAMES, spec, False


CLIPS = {
    "Walk": with_extras(S.walk_spec, breathe, lambda t: (math.radians(4) * math.sin(4 * math.pi * t), math.radians(6) * math.sin(2 * math.pi * t))),
    "Idle": with_extras(S.idle_spec, breathe, lambda t: (math.radians(6) * math.sin(2 * math.pi * t), math.radians(8) * math.sin(4 * math.pi * t))),
    "Hit": with_extras(S.hit_spec, lambda t: 1.0 - 0.1 * max(0.0, K2.recoil(t)), lambda t: (-math.radians(15) * K2.recoil(t), 0.0)),
    "Death": with_extras(S.death_spec, lambda t: 1.0 - 0.3 * K2.ease(0.2, 0.9, t), lambda t: (-math.radians(40) * K2.ease(0.1, 0.6, t), math.radians(20) * K2.ease(0.1, 0.6, t))),
    "Strike": with_extras(S.strike_spec, lambda t: 1.0 + 0.1 * K2.pulse(t, 0.2, 0.4, 0.7), lambda t: (math.radians(20) * K2.pulse(t, 0.0, 0.4, 0.9), 0.0)),
    "Spray": with_extras(spray_spec,
                         lambda t: 1.0 + 0.3 * K2.pulse(t, 0.1, SPRAY_RELEASE_FRAME / SPRAY_FRAMES - 0.06, SPRAY_RELEASE_FRAME / SPRAY_FRAMES) - 0.22 * K2.pulse(t, SPRAY_RELEASE_FRAME / SPRAY_FRAMES - 0.04, SPRAY_RELEASE_FRAME / SPRAY_FRAMES + 0.05, 0.9),
                         lambda t: (math.radians(30) * K2.pulse(t, 0.1, 0.5, 0.95), math.radians(12) * K2.pulse(t, 0.1, 0.5, 0.95))),
}
LOD_DROP = dict(S.LOD_DROP)
LOD_AIMS = dict(S.LOD_AIMS)
RIGID = S.RIGID | {"Crown", "Gland", "Nozzle"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = S.build_asset(NAME, builder, GAME_TRIS, voxel=0.0024)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = B.ScalePoser(arm, SCALE_BONES)
    actions = C.key_clips(poser, CLIPS)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      SCALE_BONES)
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    K2.scale_rig(arm, [asset.low], actions.values(), M)
    K2.scale_rig(lod[0], [lod[1]], lod[2].values(), M)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), arm, asset.low, actions)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    arm.animation_data.action = actions["Idle"]
    C.write_report(NAME, {"asset": NAME, "base_species": "Stinger", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": S.TEX,
                          "low_texture_size": S.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames,
                          "scale_keyed_bones": SCALE_BONES, "spray_release_frame": SPRAY_RELEASE_FRAME,
                          "spray_origin_bone": "Gland (nozzles about 0.15 x scale above it)",
                          "strike_hit_frame": S.STRIKE_HIT_FRAME,
                          "walk_distance_per_loop": round(S.STRIDE / S.STANCE * M, 3), "unity_scale": GAME_SCALE,
                          "model_scale_from_stinger_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    B.boss_previews(
        "acidempress", arm, asset.low, actions, lod, out, asset, target=(0, 0, 0.3 * M), distance=2.6 * M,
        game_scale=GAME_SCALE, base=dict(asset="Stinger", scale=STINGER_SCALE, clip="Walk", frame=6),
        move=("Walk", 4), power=("Spray", SPRAY_RELEASE_FRAME),
        strips=[("Walk", "side", range(0, S.WALK_FRAMES, 4)), ("Spray", "side", range(0, SPRAY_FRAMES, 6)),
                ("Death", "close", range(0, 41, 6))],
        macros=[("plates", G(0.08, 0.27, -0.12) * M, 0.5 * M, -50, 40), ("crown", G(0.0, 0.3, 0.2) * M, 0.45 * M, -30, 25),
                ("gland", G(0, 0.8, -0.13) * M, 0.45 * M, -60, 20)],
        rig_scale=2.2, minions=[(-1.5, 0.9, 0.2, 3), (-1.0, -0.7, -0.1, 10), (1.4, 0.8, 0.3, 17), (1.6, -0.5, 0.0, 21)])


if __name__ == "__main__":
    main()
