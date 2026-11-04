"""SkySovereign: the bat boss of the air: a tail laser swept across the field (round 10, see
ArtSource/DESIGN_NOTES.md).

The element bat's body plan (build_bat.py) grown into a sovereign: a wider span, a second smaller wing pair behind
the first (its own bones), a crown of five backward horns over the brow, and a long tail that ends in a lens organ
(the laser emitter, on its own bone `Emitter`) between two hooked prongs. Its own warm palette (wine body, magenta
glow, amber-white lens), not an element. Built in Bat units, then scaled as a whole (creature_kit2.scale_rig).

Clips: Fly, Glide, Hit, Death (from the Bat, with the aft wings and the tail), Laser (the power: it rears, the tail
swings forward under the body and sweeps the lens from side to side; beam on from LASER_ON_FRAME to LASER_OFF_FRAME,
along the Emitter bone's +Y). Also exports SkySovereign_LOD1.

Data note: in ProjectGenerator.Bestiary the SkySovereign is still the ray boss (Skyray shape) and the StormMatriarch
the bat boss; the round-6 StormMatriarch model is a Skyray boss, so this bat boss fits the SkySovereign slot once the
data is swapped (curator decision).

Run: blender --background --python Tools/Blender/build_skysovereign.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_bat as BT  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "SkySovereign"
LOD_NAME = "SkySovereign_LOD1"
GAME_TRIS = 13500
LOD_TRIS = 6000
M = 1.5
GAME_SCALE = 1.4 * 0.6
BAT_SCALE = 0.65

X, Yv, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
SIDES = BT.SIDES

SKIN = BT.skin(top=(0.38, 0.08, 0.16), side=(0.13, 0.03, 0.07), belly=(0.04, 0.015, 0.025), glow=(1.0, 0.32, 0.62),
               eye=(1.0, 0.8, 0.45), membrane=(0.36, 0.06, 0.14), under=(0.45, 0.16, 0.22), flush=(0.55, 0.1, 0.2),
               bone=(0.95, 0.85, 0.7))
LENS = H.Palette(top=SKIN["pal"].top, side=SKIN["pal"].side, belly=SKIN["pal"].belly, seam=SKIN["pal"].seam,
                 dark=SKIN["pal"].dark, glow=(1.0, 0.78, 0.4), eye=(1.0, 0.85, 0.5), membrane=(0.6, 0.3, 0.1),
                 bone=SKIN["pal"].bone)

CFG = dict(BT.CFG)
CFG.update({
    "shoulder": (0.08, 0.04, 0.03),
    "wrist": (0.36, 0.1, 0.09),
    "tips": ((0.92, 0.07, 0.02), (0.82, 0.02, -0.22), (0.5, 0.0, -0.36)),
    "flank": (0.075, 0.0, -0.15),
    "tail": [G(0, 0.0, -0.14), G(0, -0.02, -0.4), G(0, -0.04, -0.66)],
})
EMITTER = (G(0, -0.04, -0.66), G(0, -0.05, -0.8))
AFT_ROOT = ((0.06, 0.0, -0.05), (0.05, 0.0, -0.15))
AFT_TIPS = ((0.42, 0.0, -0.2), (0.24, -0.01, -0.36))


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def aft_point(sx, u, v, side):
    r = gp(AFT_ROOT[0], sx).lerp(gp(AFT_ROOT[1], sx), v)
    e = gp(AFT_TIPS[0], sx).lerp(gp(AFT_TIPS[1], sx), v)
    e = e.lerp(r, 0.18 * math.sin(math.pi * v))
    p = r.lerp(e, u) + Vector((0, 0, 0.01 * math.sin(math.pi * u)))
    return p + Vector((0, 0, side * (0.0035 + 0.004 * (1 - u))))


def aft_uv(points):
    x = np.abs(points[:, 0])
    f = -points[:, 1]
    u = np.clip((x - 0.05) / 0.37, 0, 1)
    lead = np.interp(x, [0.06, 0.42], [-0.05, -0.2])
    trail = np.interp(x, [0.05, 0.24, 0.42], [-0.15, -0.36, -0.22])
    v = np.clip((lead - f) / np.maximum(lead - trail, 0.02), 0, 1)
    return u, v


def build_parts():
    b = BT.build_parts(SKIN, CFG)
    k = BT.K
    pal = SKIN["pal"]
    # The crown: five horns over the brow, the middle one tallest, sweeping back.
    for i, (x, h) in enumerate(((0.0, 0.11), (0.045, 0.09))):
        for sx in ((1,) if x == 0 else (-1, 1)):
            root = G(x * sx, 0.08, 0.15 - 0.015 * i)
            b.tube("Crown", [root, root + G(0.02 * sx, h * 0.4, -h * 0.5), root + G(0.05 * sx, h * 0.55, -h * 1.4)],
                   [0.017, 0.011, 0.0015], sides=8, chain=["Head"], style=H.hook(LENS, k, glow=1.0, glow_from=0.4))
    # The aft wings.
    for side, sx in SIDES:
        fin = H.fin_sheet(pal, k, aft_uv, SKIN["under"], rays=2, chevrons=2, chevron_width=0.17, lead_glow=1.0,
                          lead_width=0.13, flush=SKIN["flush"])
        for name, upper, lower in (("AftSkin", 1.0, 0.0), ("AftSkinUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: aft_point(sx, u, v, h),
                   lambda u, v, sx=sx, h=lower: aft_point(sx, u, v, h), 14, 16, chain=[f"Aft_{side}"], style=fin)
        root = gp(AFT_ROOT[0], sx)
        tip = gp(AFT_TIPS[0], sx)
        b.tube("AftSpine", [root, root.lerp(tip, 0.5) + Vector((0, 0, 0.01)), tip, tip + gp((0.03, -0.02, -0.02), sx)],
               [0.014, 0.01, 0.005, 0.001], sides=8, chain=[f"Aft_{side}"], style=H.tube(pal, k, pal.side, pal.bone))
    # The lens organ between two hooked prongs at the tail's end.
    a, c = EMITTER
    b.tube("EmitterStalk", [a, a.lerp(c, 0.5), c], [0.026, 0.034, 0.036], sides=10, chain=["Emitter"],
           style=K2.ring_segment(LENS, k, joint_glow=0.5, base=pal.side, tip=pal.top, rings=3, joint_width=0.08))
    b.ellipsoid("Lens", c + G(0, 0, -0.02), (0.04, 0.03, 0.04), bone="Emitter", style=H.eye(LENS, k, 1.0), segments=(20, 10))
    for sx in (-1, 1):
        b.tube("Prong", [a.lerp(c, 0.6) + G(0.025 * sx, 0, 0), c + G(0.06 * sx, 0.0, -0.04), c + G(0.04 * sx, 0.0, -0.1)],
               [0.016, 0.011, 0.0015], sides=8, chain=["Emitter"], style=H.hook(pal, k, glow=0.8, glow_from=0.6))
    return b


def bones():
    out = BT.bones(CFG)
    a, c = EMITTER
    out.append(("Emitter", a, c, "Tail_2", True))
    for side, sx in SIDES:
        out.append((f"Aft_{side}", gp(AFT_ROOT[0], sx), gp(AFT_TIPS[0], sx), "Body", True))
    return out


def with_extras(fn, aft=lambda t: (0.0, 0.0), emitter=lambda t: (0.0, 0.0)):
    """Wraps a Bat clip: the aft wings flap after the main pair (lift, sweep), the emitter bends (pitch, yaw)."""
    def make(poser):
        frames, spec, loop = fn(poser)

        def full(t, f):
            specs = spec(t, f)
            lift, sweep = aft(t)
            for side, sx in SIDES:
                specs[f"Aft_{side}"] = {"rot": [(Vector((0, 0, sx)), sweep), (Vector((0, -sx, 0)), lift)]}
            p, y = emitter(t)
            specs["Emitter"] = {"rot": [(Z, y), (X, p)]}
            return specs
        return frames, full, loop
    return make


LASER_FRAMES = 72
LASER_ON_FRAME = 20
LASER_OFF_FRAME = 60


def laser_spec(poser):
    d = math.radians
    on, off = LASER_ON_FRAME / LASER_FRAMES, LASER_OFF_FRAME / LASER_FRAMES

    def spec(t, f):
        rear = K2.ease(0.0, on, t) * (1 - K2.ease(off, 1.0, t))
        sweep = math.sin(2 * math.pi * (t - on) / (off - on) * 1.0) * K2.pulse(t, on - 0.02, on + 0.05, off + 0.02)
        w = 2 * math.pi * t * 3
        specs = BT.specs_for(loc=Vector((0, 0, 0.03 * rear - 0.01 * math.cos(w))), pitch=-d(28) * rear, yaw=d(10) * sweep,
                             head=d(18) * rear, jaw=d(4) + d(20) * rear, arm=d(15) + d(22) * math.sin(w), sweep=-d(8) * rear,
                             hand=d(15) * math.sin(w - 0.9), fold=d(10) * max(0.0, math.cos(w)),
                             tail=(-d(40) * rear, d(28) * sweep))
        return specs
    return LASER_FRAMES, spec, False


def flap_aft(t):
    w = 2 * math.pi * t
    return (math.radians(30) * math.sin(w - 0.8), math.radians(6) * math.cos(w))


CLIPS = {
    "Fly": with_extras(BT.fly_spec, flap_aft, lambda t: (math.radians(8) * math.sin(2 * math.pi * t - 2.0), 0.0)),
    "Glide": with_extras(BT.glide_spec, lambda t: (math.radians(4) * math.sin(4 * math.pi * t), 0.0),
                         lambda t: (0.0, math.radians(10) * math.sin(2 * math.pi * t))),
    "Hit": with_extras(BT.hit_spec, lambda t: (math.radians(25) * K2.recoil(t), 0.0), lambda t: (math.radians(15) * K2.recoil(t), 0.0)),
    "Death": with_extras(BT.death_spec, lambda t: (math.radians(50) * K2.ease(0.0, 0.5, t), math.radians(30) * K2.ease(0.0, 0.5, t)),
                         lambda t: (-math.radians(30) * K2.ease(0.1, 0.6, t), 0.0)),
    "Laser": with_extras(laser_spec, lambda t: (math.radians(20) * math.sin(6 * math.pi * t - 0.8), 0.0),
                         lambda t: (-math.radians(25) * K2.ease(0.0, LASER_ON_FRAME / LASER_FRAMES, t) * (1 - K2.ease(LASER_OFF_FRAME / LASER_FRAMES, 1.0, t)), 0.0)),
}
LOD_DROP = dict(BT.LOD_DROP)
LOD_AIMS = dict(BT.LOD_AIMS)
RIGID = BT.RIGID | {"Crown", "Lens", "Prong"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = BT.build_asset(NAME, builder, GAME_TRIS, voxel=0.0019)
    bone_list = bones()
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
    C.write_report(NAME, {"asset": NAME, "base_species": "Bat", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": BT.TEX,
                          "low_texture_size": BT.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames,
                          "laser_on_frame": LASER_ON_FRAME, "laser_off_frame": LASER_OFF_FRAME,
                          "laser_origin_bone": "Emitter (beam along its +Y, from its tail)",
                          "flap_seconds": round(BT.FLY_FRAMES / 30, 2), "pivot": "body centre (flight height)",
                          "unity_scale": GAME_SCALE, "model_scale_from_bat_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    h = BT.AIR_HEIGHT
    B.boss_previews(
        "skysovereign", arm, asset.low, actions, lod, out, asset, target=(0, -0.1 * M, 0.0), distance=2.4 * M,
        game_scale=GAME_SCALE, base=dict(asset="Bat", scale=BAT_SCALE, clip="Fly", frame=4),
        move=("Fly", 3), power=("Laser", 40), idle="Glide",
        strips=[("Fly", "front", range(0, BT.FLY_FRAMES, 2)), ("Laser", "close", range(0, LASER_FRAMES, 9)),
                ("Death", "close", range(0, 37, 6))],
        macros=[("wing", G(-0.5, 0.06, -0.06) * M, 0.5 * M, -40, 45), ("crown", G(0.0, 0.1, 0.16) * M, 0.4 * M, -30, 25),
                ("lens", G(0, -0.05, -0.78) * M, 0.35 * M, -150, 20)],
        rig_scale=1.8, lift=0.9 * M, game_lift=h, game_target=(0, 0.3, 0.9),
        minions=[(-1.6, 1.0, 0.2, 2), (-1.1, -0.8, -0.1, 7), (1.5, 0.9, 0.3, 10), (1.8, -0.5, 0.0, 5)],
        compare_span=1.6)


if __name__ == "__main__":
    main()
