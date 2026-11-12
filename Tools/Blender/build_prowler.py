"""Prowler: the fast pack hunter on four legs (round 4, see ArtSource/DESIGN_NOTES.md).

Round 49A redesign (Remco: "looks rather reptile-like, it must be more alien-WOLF-like"): the anatomy is wolf_kit.py's
alien wolf, shared with the PackAlpha: a deep narrow chest over a tucked-up waist, long forelegs and digitigrade hind
legs on four-toed paws, a thick neck under a ruff of fur-like quill plates that runs down the spine, a wolf skull
with a long muzzle, a split lower jaw (two mandible halves) and two pairs of tall swept ear blades, a drooping brush
tail. Orange glow in the plate seams (tiger stripes) and the throat. Same bone names and clips as before; the jaw
halves now open downward and the clips' neck angles are taken relative to the raised neck (NECK_BIAS).

Clips: Run (loop, low and flat rotary gallop), Idle, Hit, Death, Pounce. Also exports Prowler_LOD1.

Run: blender --background --python Tools/Blender/build_prowler.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import wolf_kit as W  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "Prowler"
LOD_NAME = "Prowler_LOD1"
K = 1.0
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 0.62

# Species colours from ProjectGenerator.Bestiary: red-orange body (0.8, 0.28, 0.15), red sensors, orange accent.
PAL = H.Palette(top=(0.8, 0.27, 0.12), side=(0.2, 0.05, 0.03), belly=(0.06, 0.025, 0.02), seam=(0.04, 0.014, 0.01),
                dark=(0.13, 0.04, 0.025), glow=(1.0, 0.55, 0.17), eye=(1.0, 0.28, 0.14), membrane=(0.32, 0.07, 0.03),
                bone=(0.93, 0.8, 0.62))
LEGP = H.Palette(top=(0.6, 0.18, 0.08), side=(0.26, 0.065, 0.035), belly=(0.05, 0.02, 0.016), seam=(0.035, 0.012, 0.01),
                 dark=(0.1, 0.035, 0.022), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

# Round 49A: the anatomy is the alien wolf of wolf_kit.py (shared with the PackAlpha).
WOLF = W.Wolf(1.0)
LEG_KINDS = W.LEG_KINDS


def legs():
    return WOLF.legs()


def jaw_points(sx):
    return WOLF.jaw_points(sx)


def build_parts():
    return WOLF.build_parts(PAL, LEGP, K)


def bones():
    return WOLF.bones()


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

# Round 49A: the wolf's neck rises from the withers in the bind pose, so the clips' neck angles (written for the old
# low reptile neck, about -10 to -14 degrees) are taken relative to this bias: they now hold the head near the bind pose.
NECK_BIAS = math.radians(12)


def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, flex=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0), tail_lift=0.0):
    """flex: chest pitch against the pelvis (gather < 0 < stretch); neck/head pitch; jaw: how far the shears open;
    tail: (sway, wave phase)."""
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


RUN_FRAMES = 16
STRIDE, LIFT, STANCE = 0.6, 0.09, 0.42
# Rotary gallop: hind left, hind right, then fore right, fore left (fraction of the cycle at touchdown).
TOUCHDOWN = {("L", "H"): 0.0, ("R", "H"): 0.1, ("R", "F"): 0.45, ("L", "F"): 0.55}


def swing_fold(phase):
    """0 planted .. 1 mid-swing: how much a foot folds up on its way forward."""
    if phase < STANCE:
        return 0.0
    return math.sin(math.pi * (phase - STANCE) / (1 - STANCE))


def run_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        # The spine gathers when the hind feet reach forward and stretches when the fore feet do; the head stays
        # level and low (the neck takes the body's pitch out), the body barely bobs.
        flex = math.radians(7) * math.sin(w + 0.4)
        specs = body_specs(offset=Vector((0, 0, -0.06 + 0.01 * math.cos(2 * w))), pitch_=math.radians(5) + math.radians(2.5) * math.sin(w),
                           roll=math.radians(1.5) * math.sin(w), flex=flex,
                           neck=-math.radians(10) - flex * 0.6, head=-math.radians(4) * math.sin(w) + math.radians(6),
                           jaw=math.radians(6) * (0.5 + 0.5 * math.sin(2 * w)), tail=(math.radians(4), w),
                           tail_lift=math.radians(6) + math.radians(4) * math.sin(w + 1.0))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t - TOUCHDOWN[key]) % 1.0
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            fold = swing_fold(phase)
            dirs[key] = Matrix.Rotation(math.radians(-55 if key[1] == "F" else 45) * fold, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return RUN_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(3 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, CROUCH + 0.006 * math.sin(2 * w))), pitch_=math.radians(3),
                           flex=math.radians(2) * math.sin(2 * w), neck=-math.radians(14) + math.radians(4) * math.sin(w),
                           head=math.radians(6) + math.radians(3) * math.sin(2 * w + 1),
                           head_yaw=math.radians(22) * math.sin(w) * math.sin(0.5 * w + 0.3),
                           jaw=math.radians(4) + math.radians(26) * snap, tail=(math.radians(10), w * 2), tail_lift=math.radians(4))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 60, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((0.03 * k, 0.03 * k, CROUCH + 0.01 * k)), roll=math.radians(8) * k,
                           yaw=math.radians(-8) * k, flex=-math.radians(6) * k, neck=-math.radians(5) - math.radians(14) * k,
                           head=math.radians(-10) * k, jaw=math.radians(30) * max(k, 0.0), tail=(math.radians(12) * k, 1.0),
                           tail_lift=math.radians(15) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 12, spec, False


POUNCE_FRAMES = 26
CROUCH = -0.045  # the hunter's standing crouch: every clip holds the body this much below the bind pose


def pounce_spec(poser):
    def spec(t, f):
        crouch = C.smoothstep(0.0, 0.3, np.array([t]))[0] * (1 - C.smoothstep(0.3, 0.42, np.array([t]))[0])
        leap = C.smoothstep(0.3, 0.55, np.array([t]))[0] * (1 - C.smoothstep(0.62, 0.9, np.array([t]))[0])
        land = C.smoothstep(0.75, 0.85, np.array([t]))[0] * (1 - C.smoothstep(0.85, 1.0, np.array([t]))[0])
        arc = math.sin(math.pi * C.smoothstep(0.3, 0.78, np.array([t]))[0])
        offset = Vector((0, -0.32 * leap, CROUCH - 0.08 * crouch + 0.14 * arc - 0.04 * land))
        specs = body_specs(offset=offset, pitch_=math.radians(8) * crouch - math.radians(10) * leap * (1 - arc * 0.3),
                           flex=-math.radians(8) * crouch + math.radians(8) * leap, neck=-math.radians(6) * crouch + math.radians(10) * leap,
                           head=math.radians(8) * crouch - math.radians(12) * leap, jaw=math.radians(45) * leap,
                           tail=(math.radians(5), t * 6), tail_lift=math.radians(18) * leap)
        feet, dirs = {}, {}
        for key, lg in legs().items():
            if key[1] == "F":
                # The fore feet tuck, then reach far forward and up with the claws out, and land at the end.
                reach = Vector((0, -0.42, 0.22)) * leap
                feet[key] = lg.foot + reach + Vector((0, 0, 0.05 * crouch))
                dirs[key] = Matrix.Rotation(math.radians(-35) * leap, 3, "X") @ lg.foot_dir
            else:
                # The hind feet push off, trail behind in the air and swing back in under the body.
                push = C.smoothstep(0.42, 0.6, np.array([t]))[0] * (1 - C.smoothstep(0.7, 0.92, np.array([t]))[0])
                feet[key] = lg.foot + Vector((0, 0.08 * push, 0.1 * push)) + Vector((0, -0.32 * leap * (1 - push * 0.3), 0))
                dirs[key] = Matrix.Rotation(math.radians(60) * push, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return POUNCE_FRAMES, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = C.smoothstep(0.1, 0.55, np.array([t]))[0]
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 50) * (1 if t > 0.6 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH + 0.03 * jerk)), neck=math.radians(25) * fall, head=math.radians(15) * fall,
                           jaw=math.radians(35) * fall + math.radians(8) * twitch, tail=(math.radians(14) * fall, 0.5),
                           tail_lift=-math.radians(14) * fall)
        # Falls onto its side: the root rolls over the right feet and settles.
        pivot = Vector((0.15, 0, 0))
        angle = math.radians(84) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -0.17 * fall))}
        pose = poser.solve(specs)
        for key, lg in legs().items():
            move = pose[LEG_KINDS[key[1]]["parent"]] @ poser.rest[LEG_KINDS[key[1]]["parent"]].inverted()
            hip = move @ lg.root
            # Limp legs: they fall in a loose curl in the body's frame, with a last kick.
            rest_vec = lg.foot - lg.root
            curl = Vector((0, 0.08 if key[1] == "H" else -0.08, 0.1)) + rest_vec * 0.6
            kick = Vector((0, 0.03 * twitch, 0))
            foot = hip + move.to_3x3() @ (rest_vec.lerp(curl, fall) + kick)
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 36, spec, False


CLIPS = {"Run": run_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Pounce": pounce_spec}
# LOD1: jaws fold into the head, the tail is one bone, each leg two (the lower bone aimed from knee to toe). 14 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", "Tail_2": "Tail_1", "Tail_3": "Tail_1",
            **{f"Leg_{s}{k}_Foot": f"Leg_{s}{k}_Lower" for s, _ in SIDES for k in LEG_KINDS}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3"),
            **{f"Leg_{s}{k}_Lower": (f"Leg_{s}{k}_Lower", f"Leg_{s}{k}_Foot") for s, _ in SIDES for k in LEG_KINDS}}
RIGID = ()
QUICK_TARGET = (0, 0, 0.3)


def old_proxy():
    """EnemyModelFactory.Prowler in primitives (wolf with a mane and flank stripes), flat colours."""
    body, dark, glow, eyes = (0.8, 0.28, 0.15), (0.25, 0.08, 0.05), (1.0, 0.6, 0.2), (1.0, 0.35, 0.2)
    groups = {c: C.PartBuilder() for c in (body, dark, glow, eyes)}
    g = groups[body]
    g.ellipsoid("Chest", G(0, 0.4, 0.12), (0.165, 0.21, 0.17))
    g.ellipsoid("Waist", G(0, 0.37, -0.1), (0.12, 0.2, 0.13))
    g.ellipsoid("Haunch", G(0, 0.39, -0.28), (0.15, 0.15, 0.15))
    groups[dark].ellipsoid("Saddle", G(0, 0.47, 0.08), (0.15, 0.3, 0.1))
    g.tube("Neck", [G(0, 0.47, 0.24), G(0, 0.5, 0.36), G(0, 0.49, 0.44)], [0.11, 0.095, 0.085])
    g.ellipsoid("Head", G(0, 0.49, 0.47), (0.1, 0.12, 0.095))
    g.tube("Muzzle", [G(0, 0.485, 0.52), G(0, 0.475, 0.63), G(0, 0.465, 0.72)], [0.072, 0.058, 0.042])
    for sx in (-1, 1):
        groups[eyes].ellipsoid("Eye", G(0.055 * sx, 0.515, 0.54), (0.03, 0.022, 0.016))
        groups[glow].tube("Stripe", [G(0.15 * sx, 0.42, 0.2), G(0.125 * sx, 0.4, -0.05), G(0.14 * sx, 0.41, -0.28)], [0.012, 0.014, 0.008])
        groups[dark].tube("Ear", [G(0.06 * sx, 0.56, 0.45), G(0.08 * sx, 0.68, 0.4)], [0.035, 0.003])
        for z in (0.15, -0.27):
            groups[body].tube("Leg", [G(0.1 * sx, 0.36, z), G(0.1 * sx, 0.18, z + 0.03), G(0.1 * sx, 0.0, z)], [0.04, 0.03, 0.025])
    for i in range(8):
        tt = i / 7
        groups[dark].tube("Mane", [G(0, 0.55, 0.42 - 0.42 * tt), G(0, 0.68, 0.32 - 0.42 * tt)], [0.04, 0.003])
    g.tube("Tail", [G(0, 0.4, -0.38), G(0, 0.36, -0.6), G(0, 0.3, -0.8)], [0.06, 0.05, 0.02])
    return C.flat_proxy(groups, "OldProwler")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    target = Vector((0, -0.08, 0.3))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 2.4, azimuth=-38, elevation=24, lens=55)
    C.set_action(arm, actions["Idle"], 20)
    paths.append(C.render(close, os.path.join(out, "prowler_close.png"), 1280, 720))
    side = C.closeup_camera(target, 2.6, azimuth=-90, elevation=6, lens=55)
    C.set_action(arm, actions["Run"], 0)
    paths.append(C.render(side, os.path.join(out, "prowler_side.png"), 1280, 720))
    front = C.closeup_camera(target + Vector((0, -0.3, 0.05)), 1.5, azimuth=-20, elevation=10, lens=55)
    C.set_action(arm, actions["Pounce"], 13)
    paths.append(C.render(front, os.path.join(out, "prowler_pounce.png"), 1280, 720))
    top = C.closeup_camera(target, 2.6, azimuth=-160, elevation=58, lens=55)
    C.set_action(arm, actions["Run"], 6)
    paths.append(C.render(top, os.path.join(out, "prowler_top.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 36)
    paths.append(C.render(close, os.path.join(out, "prowler_dead.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Run"], side, out, "prowler_run", range(0, RUN_FRAMES, 2)),
              C.strip_sheet(arm, actions["Pounce"], side, out, "prowler_pounce", range(0, POUNCE_FRAMES, 3)[:8])]

    C.set_action(lod_arm, lod_actions["Run"], 6)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "prowler_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 20)
    macros = [("prowler_macro_ribs.png", G(-0.14, 0.42, 0.12), 0.45, -80, 15),
              ("prowler_macro_head.png", G(0.05, 0.42, 0.62), 0.4, -35, 18),
              ("prowler_macro_leg.png", G(-0.13, 0.15, -0.3), 0.4, -60, 10)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    run = actions["Run"]
    layout = [(run, 0, (-1.0, 0.55, 0), 0.15), (run, 5, (-0.2, 0.75, 0), 0.1), (run, 11, (0.7, 0.45, 0), 0.2),
              (run, 3, (-0.6, -0.3, 0), -0.1), (run, 8, (0.3, -0.5, 0), 0.0), (run, 14, (1.2, -0.25, 0), 0.25)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "prowler_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "prowler_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "prowler_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "prowler_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Idle"], 20)
    single = C.game_view(arm, mesh, os.path.join(out, "prowler_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare4_prowler.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_prowler.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "prowler_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0032, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.08,
                               extrusion=0.012, ray=0.04, emission_strength=2.5,
                               extra=H.weathering(K, markings=PAL.dark, marking_amount=0.7), specular=0.3,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "run_distance_per_loop": round(STRIDE / STANCE, 3),
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)
    # Round 46: the close-up detail (eyes, jaw fangs, toe talons) as extra parts, exported again with the body.
    # `--nodetail` (round 49A) stops after the body, for quick looks while the anatomy changes.
    if C.cli_args().get("nodetail"):
        return
    import prowler_detail
    prowler_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
