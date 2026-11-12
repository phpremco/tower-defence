"""PackAlpha: the Prowler boss, the pack leader whose howl sends the whole pack running (round 6, see
ArtSource/DESIGN_NOTES.md).

Round 49A redesign (Remco: "I don't know what I'm looking at, it looks like a porcupine or hippo"): the Prowler's
alien wolf (wolf_kit.py) at 1.65 times its size, clearly the same species: heavier shoulders and chest, a collar of
long glowing plates on the Mane bones that flares on the howl, two pale bone horns swept back from the brow, a pale
bone chevron over the withers, healed claw scars glowing on the flanks, three eyes per side (Face part). Same bones,
clips and the howl peak frame.

Clips: Run (loop), Idle (loop), Hit, Death, Howl (the power). Also exports PackAlpha_LOD1.

Run: blender --background --python Tools/Blender/build_packalpha.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import wolf_kit as W  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "PackAlpha"
LOD_NAME = "PackAlpha_LOD1"
# Built in Prowler units times M: at Unity scale 1.3 x 0.6 it stands about 2.1 times a Prowler (as the factory boss).
M = 1.65
K = M
DETAIL_K = 1.4
GAME_TRIS = 14500
LOD_TRIS = 6000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 1.3 * 0.6
PROWLER_SCALE = 0.62

# The PackAlpha's data colours: a deeper blood red than the Prowler (0.6, 0.15, 0.1), red-orange sensors, orange glow.
PAL = H.Palette(top=(0.62, 0.13, 0.07), side=(0.17, 0.035, 0.025), belly=(0.05, 0.02, 0.016), seam=(0.035, 0.012, 0.01),
                dark=(0.1, 0.028, 0.02), glow=(1.0, 0.52, 0.14), eye=(1.0, 0.3, 0.12), membrane=(0.3, 0.06, 0.025),
                bone=(0.92, 0.8, 0.62))
LEGP = H.Palette(top=(0.48, 0.1, 0.055), side=(0.2, 0.045, 0.03), belly=(0.045, 0.018, 0.014), seam=(0.03, 0.01, 0.008),
                 dark=(0.08, 0.025, 0.018), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def P(x, up, fwd):
    """A point in Prowler units, scaled to the boss."""
    return G(x * M, up * M, fwd * M)


def R(*r):
    return tuple(v * M for v in r) if len(r) > 1 else r[0] * M


# Round 49A: the anatomy is wolf_kit.py's alien wolf at M times the Prowler, with the leader's collar, horns and
# chevron (alpha=True). Bones as before (the Mane_L/R collar bones flare on the howl).
WOLF = W.Wolf(M, alpha=True)
LEG_KINDS = W.LEG_KINDS


def legs():
    return WOLF.legs()


def jaw_points(sx):
    return WOLF.jaw_points(sx)


def build_parts():
    return WOLF.build_parts(PAL, LEGP, K)


def scar_cuts():
    """Three healed claw slashes across each flank and one across each shoulder (Blender space)."""
    cuts = []
    for sx in (-1, 1):
        out = Vector((sx, 0, 0.25))
        for i in range(3):
            o = i * 0.05
            cuts.append((P(0.125 * sx, 0.6 - o * 0.4, 0.2 - o), P(0.105 * sx, 0.42 - o * 0.4, 0.02 - o), out))
        cuts.append((P(0.12 * sx, 0.63, 0.25), P(0.135 * sx, 0.5, 0.12), Vector((sx, 0, 0.6))))
    return cuts


def bones():
    return WOLF.bones()


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

# Round 49A: as the Prowler's (build_prowler.NECK_BIAS): the clips' neck angles are relative to the wolf's raised neck.
NECK_BIAS = math.radians(12)


def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, flex=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0), tail_lift=0.0, mane=0.0, mane_lift=0.0):
    """As the Prowler's, plus mane: how far the collar fans flare out (rad), mane_lift: how far they rise."""
    sway, phase = tail
    specs = {
        "Root": {"rot": []},
        "Pelvis": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
        "Chest": {"rot": [(X, flex), (Z, -yaw * 0.5)]},
        "Neck": {"rot": [(X, neck + NECK_BIAS), (Z, head_yaw * 0.4)]},
        "Head": {"rot": [(X, head), (Z, head_yaw * 0.6)]},
    }
    for side, sx in SIDES:
        specs[f"Jaw_{side}"] = {"rot": W.jaw_rot(sx, jaw)}
        specs[f"Mane_{side}"] = {"rot": [(Yv, -sx * mane), (X, mane_lift)]}
    for i in range(3):
        specs[f"Tail_{i + 1}"] = {"rot": [(Z, sway * math.sin(phase - i * 0.9) * (0.6 + 0.3 * i)), (X, tail_lift * (1 - 0.3 * i))]}
    return specs


def place_legs(poser, specs, feet, foot_dirs=None):
    pose = poser.solve(specs)
    moves = {name: pose[name] @ poser.rest[name].inverted() for name in ("Chest", "Pelvis")}
    for key, lg in legs().items():
        move = moves[LEG_KINDS[key[1]]["parent"]]
        fd = foot_dirs[key] if foot_dirs else lg.foot_dir
        pole = move.to_3x3() @ lg.pole
        specs.update(lg.pose(poser, move @ lg.root, feet[key], pole, fd))
    return specs


def s_(a, b, t):
    return C.smoothstep(a, b, np.array([t]))[0]


RUN_FRAMES = 20
STRIDE, LIFT, STANCE = 0.6 * M, 0.09 * M, 0.45
TOUCHDOWN = {("L", "H"): 0.0, ("R", "H"): 0.1, ("R", "F"): 0.45, ("L", "F"): 0.55}
CROUCH = -0.045 * M


def swing_fold(phase):
    if phase < STANCE:
        return 0.0
    return math.sin(math.pi * (phase - STANCE) / (1 - STANCE))


def run_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Heavier than the Prowler: more bounce and roll, the shoulders working, the collar bouncing on each stride.
        flex = d(6) * math.sin(w + 0.4)
        specs = body_specs(offset=Vector((0, 0, -R(0.055) + R(0.016) * math.cos(2 * w))), pitch_=d(5) + d(3) * math.sin(w),
                           roll=d(2.5) * math.sin(w), flex=flex, neck=-d(12) - flex * 0.6, head=-d(4) * math.sin(w) + d(8),
                           jaw=d(8) * (0.5 + 0.5 * math.sin(2 * w)), tail=(d(5), w), tail_lift=d(5) + d(4) * math.sin(w + 1.0),
                           mane=d(4) * math.sin(2 * w + 0.6), mane_lift=d(3) * math.sin(2 * w + 0.6))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t - TOUCHDOWN[key]) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            dirs[key] = Matrix.Rotation(d(-55 if key[1] == "F" else 45) * swing_fold(phase), 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return RUN_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(3 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, CROUCH + R(0.006) * math.sin(2 * w))), pitch_=d(3),
                           flex=d(2) * math.sin(2 * w), neck=-d(16) + d(4) * math.sin(w),
                           head=d(6) + d(3) * math.sin(2 * w + 1), head_yaw=d(20) * math.sin(w) * math.sin(0.5 * w + 0.3),
                           jaw=d(5) + d(24) * snap, tail=(d(9), w * 2), tail_lift=d(4),
                           mane=d(6) * snap + d(2) * math.sin(w), mane_lift=d(4) * snap)
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((R(0.025) * k, R(0.025) * k, CROUCH + R(0.01) * k)), roll=d(6) * k, yaw=-d(6) * k,
                           flex=-d(5) * k, neck=-d(8) - d(12) * k, head=-d(10) * k, jaw=d(32) * max(k, 0.0),
                           tail=(d(10) * k, 1.0), tail_lift=d(12) * k, mane=d(14) * max(k, 0.0), mane_lift=d(8) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


HOWL_FRAMES = 54
# The howl's peak: the rally VFX and sound start here (frames 20 to 42 hold the howl).
HOWL_PEAK_FRAME = 22


def howl_spec(poser):
    d = math.radians

    def spec(t, f):
        rise = s_(0.05, 0.38, t) * (1 - s_(0.78, 1.0, t))
        gather = math.sin(math.pi * s_(0.0, 0.2, t)) * (1 - s_(0.15, 0.3, t))
        shake = math.sin(t * 2 * math.pi * 11) * s_(0.38, 0.45, t) * (1 - s_(0.72, 0.8, t))
        # It sits back on its haunches, the chest rises, the head is thrown up with the shears wide open and the
        # collar flares like a frill; the tail lifts and shakes.
        specs = body_specs(offset=Vector((0, R(0.05) * rise, CROUCH - R(0.04) * gather + R(0.03) * rise)),
                           pitch_=-d(9) * rise + d(4) * gather, flex=-d(6) * rise, neck=-d(30) * rise + d(10) * gather,
                           head=-d(16) * rise + d(2) * shake, head_yaw=d(3) * shake, jaw=d(52) * rise + d(4) * shake,
                           tail=(d(6) * rise, t * 30), tail_lift=d(30) * rise,
                           mane=d(48) * rise + d(4) * shake, mane_lift=d(30) * rise)
        feet = {key: lg.foot for key, lg in legs().items()}
        for key in feet:
            if key[1] == "F":
                feet[key] = feet[key] + Vector((0, -R(0.04) * gather, R(0.06) * gather))
        return place_legs(poser, specs, feet)
    return HOWL_FRAMES, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = s_(0.1, 0.55, t)
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 50) * (1 if t > 0.6 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH + R(0.03) * jerk)), neck=d(25) * fall, head=d(15) * fall,
                           jaw=d(35) * fall + d(8) * twitch, tail=(d(14) * fall, 0.5), tail_lift=-d(14) * fall,
                           mane=-d(20) * fall, mane_lift=d(10) * fall)
        pivot = P(0.17, 0, 0)
        angle = d(84) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -R(0.19) * fall))}
        pose = poser.solve(specs)
        for key, lg in legs().items():
            move = pose[LEG_KINDS[key[1]]["parent"]] @ poser.rest[LEG_KINDS[key[1]]["parent"]].inverted()
            hip = move @ lg.root
            rest_vec = lg.foot - lg.root
            curl = Vector((0, R(0.08) if key[1] == "H" else -R(0.08), R(0.1))) + rest_vec * 0.6
            kick = Vector((0, R(0.03) * twitch, 0))
            foot = hip + move.to_3x3() @ (rest_vec.lerp(curl, fall) + kick)
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 40, spec, False


CLIPS = {"Run": run_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Howl": howl_spec}
# LOD1: the tail is one bone, each leg two (lower aimed from knee to toe); jaws and mane fans keep their bones (the
# howl needs them). 18 bones.
LOD_DROP = {"Tail_2": "Tail_1", "Tail_3": "Tail_1", **{f"Leg_{s}{k}_Foot": f"Leg_{s}{k}_Lower" for s, _ in SIDES for k in LEG_KINDS}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3"),
            **{f"Leg_{s}{k}_Lower": (f"Leg_{s}{k}_Lower", f"Leg_{s}{k}_Foot") for s, _ in SIDES for k in LEG_KINDS}}
TARGET = (0.0, -0.12, 0.5)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    extra = B.chain_extras(H.weathering(K, markings=PAL.dark, marking_amount=0.7, seed=63),
                           B.scars(scar_cuts(), PAL.glow, 1.0, width=R(0.02), depth=R(0.004)))
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0048, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.13,
                               extrusion=0.02, ray=0.06, emission_strength=2.5, extra=extra, specular=0.3,
                               mirror=True, k=DETAIL_K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder)
    poser = B.ScalePoser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "base_species": "Prowler", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
                          "low_texture_size": LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "howl_peak_frame": HOWL_PEAK_FRAME,
                          "run_distance_per_loop": round(STRIDE / STANCE, 3), "unity_scale": GAME_SCALE,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("nodetail"):
        return
    if C.cli_args().get("norender"):
        detail(norender=True)
        return
    B.boss_previews(
        "packalpha", arm, asset.low, actions, lod, out, asset, target=TARGET, distance=4.4,
        game_scale=GAME_SCALE, base=dict(asset="Prowler", scale=PROWLER_SCALE, clip="Run", frame=5),
        move=("Run", 2), power=("Howl", 30),
        strips=[("Run", "side", range(0, RUN_FRAMES, 3)[:8]), ("Howl", "side", range(0, HOWL_FRAMES, 7)), ("Howl", "close", range(0, HOWL_FRAMES, 7)),
                ("Death", "close", range(0, 41, 6))],
        macros=[("ribs", P(0.2, 0.45, 0.14), 1.6, 80, 15), ("head", P(0.06, 0.45, 0.62), 1.4, -35, 18),
                ("mane", P(-0.12, 0.6, 0.28), 1.6, -60, 30)],
        rig_scale=2.0, minions=[(-1.5, 0.8, 0.15, 0), (-0.9, -0.7, 0.1, 5), (1.2, 0.8, 0.2, 11), (1.5, -0.5, 0.0, 3),
                                (-1.9, -0.1, -0.1, 8), (0.3, 1.4, 0.25, 14)])
    detail()


def detail(norender=False):
    """Round 45: the close-up detail (eyes, teeth, claws) as extra parts, exported again with the body."""
    import packalpha_detail
    packalpha_detail.run(norender)


if __name__ == "__main__":
    main()
