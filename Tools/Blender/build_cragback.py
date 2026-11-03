"""Cragback: the Stone-element crawler, heavy, slow and very tough (round 4, see ArtSource/DESIGN_NOTES.md).

A living rock: five hewn tectonic plates on their own bones over a hot core whose fissures glow orange in the faults
between them; a huge brow slab (the ram it slams gates with) over a maw whose stone lower jaw drops to show a glowing
throat and stone teeth; three amber eyes in a vertical slit in the brow; fluted "ember" Crysium pushing out of the
faults on the back; stalactite spikes under the belly; six short columnar basalt legs with splayed stone toes.

Clips: Walk (heavy tripod with a body dip on every footfall), Idle, Hit, Death. The veins fade on death in Unity by
animating the emission (see ArtSource/README.md). Also exports Cragback_LOD1.

Run: blender --background --python Tools/Blender/build_cragback.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import stone as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Cragback"
LOD_NAME = "Cragback_LOD1"
K = 1.2
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("rock", "crystal", "chitin")
GAME_SCALE = 0.85

# Species colours from ProjectGenerator.Bestiary: grey-brown stone (0.4, 0.36, 0.32), amber eyes, orange accent and
# veins. Lighter on top and near-black in the faults so the plates read as separate slabs at game size.
PAL = H.Palette(top=(0.5, 0.44, 0.37), side=(0.25, 0.21, 0.18), belly=(0.07, 0.06, 0.055), seam=(0.05, 0.035, 0.03),
                dark=(0.12, 0.1, 0.09), glow=(1.0, 0.45, 0.12), eye=(1.0, 0.75, 0.3), bone=(0.72, 0.66, 0.58))
LEG = H.Palette(top=(0.44, 0.4, 0.36), side=(0.22, 0.2, 0.18), belly=(0.06, 0.05, 0.045), seam=(0.04, 0.03, 0.025),
                dark=(0.1, 0.085, 0.075), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
PAIRS = {1: 0.22, 2: -0.03, 3: -0.27}          # leg pair: forward position of the hip
FEMUR, TIBIA = 0.2, 0.24
TRIPOD_A = {("L", 1), ("R", 2), ("L", 3)}

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def legs():
    out = {}
    for side, sx in SIDES:
        for i, z in PAIRS.items():
            names = [f"Leg_{side}{i}_Upper", f"Leg_{side}{i}_Lower"]
            out[(side, i)] = C.Limb(names, G(0.24 * sx, 0.25, z), G(0.47 * sx, 0.0, z * 1.15 + 0.02), (FEMUR, TIBIA),
                                    G(sx * 1.0, 0.9, 0.0))
    return out


def rot(pitch=0.0, roll=0.0, yaw=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(roll), 3, "Y")
            @ Matrix.Rotation(math.radians(pitch), 3, "X"))


def slab(b, name, centre, radii, r, bone, seed, count=20, depth=0.18, rim=-0.35, veins=0.6, fault=1.0, pal=PAL):
    b.ellipsoid(name, centre, radii, r, bone=bone, segments=(40, 20),
                deform=C.chiselled(count, seed, depth, squash=(0.55, 1.0)),
                style=S.slab(pal, K, seed=seed, rim=rim, veins=veins, fault=fault))


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Core", G(0, 0.32, -0.02), (0.29, 0.43, 0.2), bone="Body", style=S.core(PAL, k, seed=3),
                segments=(40, 20))
    # Tectonic plates: a crest slab down the back, upper and lower flank slabs, a shoulder slab, a rump slab and the
    # brow. They overlap so only thin faults show the core; their edges tilt up like broken floes.
    slab(b, "PlateC", G(0, 0.55, -0.15), (0.23, 0.3, 0.11), rot(pitch=-10), "Plate_C", 11)
    for side, sx in SIDES:
        slab(b, "PlateSide", G(0.21 * sx, 0.47, -0.04), (0.17, 0.36, 0.11), rot(roll=34 * sx, yaw=-5 * sx), f"Plate_{side}", 12)
        slab(b, "Skirt", G(0.29 * sx, 0.3, 0.0), (0.11, 0.34, 0.1), rot(roll=66 * sx, yaw=4 * sx), f"Plate_{side}", 17,
             count=18, rim=-0.5, veins=0.3)
    slab(b, "Shoulder", G(0, 0.56, 0.14), (0.23, 0.17, 0.11), rot(pitch=14), "Front", 13)
    slab(b, "Rump", G(0, 0.38, -0.41), (0.23, 0.15, 0.15), rot(pitch=40), "Body", 14)
    slab(b, "Brow", G(0, 0.4, 0.36), (0.29, 0.19, 0.14), rot(pitch=-30), "Front", 15, count=16, depth=0.22, rim=-0.6,
         veins=0.3, fault=0.5)
    # The maw: a heavy stone lower jaw under the brow, a narrow glowing slit of throat and stone tusks.
    slab(b, "Jaw", G(0, 0.17, 0.36), (0.2, 0.14, 0.075), rot(pitch=6), "Jaw", 16, count=14, depth=0.2, rim=0.2,
         veins=0.0, fault=0.0)
    b.ellipsoid("Throat", G(0, 0.245, 0.37), (0.12, 0.022, 0.06), bone="Front", style=H.inner_glow(PAL, 0.9))
    for i, x in enumerate((-0.13, 0.13)):
        root = G(x, 0.23, 0.43)
        b.tube("Tusk", [root, root + G(0.01 * np.sign(x), -0.03, 0.05), root + G(0.0, 0.01, 0.1)], [0.026, 0.016, 0.002],
               sides=6, chain=["Jaw"], style=S.spike(PAL, k, seed=5 + i))
    # Three eyes in a row along the top of the brow (seen from the game camera, and no face below them).
    for i in range(3):
        b.ellipsoid("Eye", G(0, 0.55 - i * 0.008, 0.38 - i * 0.055), (0.02 - i * 0.003, 0.024 - i * 0.003, 0.014),
                    bone="Front", style=H.eye(PAL, k, 0.8))

    # Ember Crysium growing out of the faults, leaning back like a crest.
    shards = [(G(0, 0.6, 0.02), G(0, 1.0, -0.7), 0.04, 0.34, "Plate_C"),
              (G(0.13, 0.58, -0.12), G(0.5, 1.0, -0.75), 0.034, 0.28, "Plate_C"),
              (G(-0.13, 0.58, -0.12), G(-0.5, 1.0, -0.75), 0.034, 0.28, "Plate_C"),
              (G(0.08, 0.52, -0.33), G(0.35, 1.0, -0.9), 0.026, 0.2, "Body"),
              (G(-0.08, 0.52, -0.33), G(-0.35, 1.0, -0.9), 0.026, 0.2, "Body")]
    for i, (base, direction, radius, length, bone) in enumerate(shards):
        b.shard("Crysium", base, direction, radius, length, sides=8, twist=2.2 * (1 if i % 2 else -1), flute=0.42,
                bone=bone, style=Y.shard_style("ember", 0.8, seed=40 + i))
    # Stalactites under the belly.
    for x, z, length in ((0.12, -0.12, 0.06), (-0.12, -0.12, 0.06), (0.0, -0.28, 0.07)):
        root = G(x, 0.15, z)
        b.tube("Stalactite", [root, root + G(0, -length, -0.01)], [0.022, 0.002], sides=6, chain=["Body"],
               style=S.spike(PAL, k, seed=20))

    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        # Hexagonal basalt drums: six-sided tubes that the remesh rounds a little, with a boulder on the knee.
        b.tube("Upper", [hip, hip.lerp(knee, 0.5), knee], [0.085, 0.078, 0.07], sides=6, chain=[lg.names[0]],
               style=S.pillar(LEG, k, drums=2, seed=30 + i), cap=0.5)
        b.tube("Lower", [knee, knee.lerp(foot, 0.5), foot + Vector((0, 0, 0.02))], [0.07, 0.068, 0.09], sides=6,
               chain=[lg.names[1]], style=S.pillar(LEG, k, drums=3, seed=33 + i), cap=0.3)
        slab(b, "Knee", knee + Vector((0, 0, 0.02)), (0.085, 0.08, 0.07), rot(roll=20 * sx), lg.names[0], 60 + i,
             count=12, depth=0.25, rim=-0.7, veins=0.0, fault=0.4, pal=LEG)
        for a in (-0.65, 0.0, 0.65):
            out = G(sx * math.sin(a + 0.3 * sx), 0, math.cos(a + 0.3 * sx)).normalized()
            toe = foot + Vector((0, 0, 0.018))
            b.tube("Toe", [toe, toe + out * 0.06 + Vector((0, 0, 0.004)), toe + out * 0.1 - Vector((0, 0, 0.012))],
                   [0.034, 0.024, 0.006], sides=6, chain=[lg.names[1]], style=S.spike(LEG, k, seed=50 + i))
    return b


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Body", G(0, 0.32, -0.08), G(0, 0.34, 0.2), "Root", True),
        ("Front", G(0, 0.4, 0.12), G(0, 0.38, 0.46), "Body", True),
        ("Jaw", G(0, 0.21, 0.22), G(0, 0.15, 0.47), "Front", True),
        ("Plate_C", G(0, 0.5, 0.06), G(0, 0.54, -0.36), "Body", True),
    ]
    for side, sx in SIDES:
        out.append((f"Plate_{side}", G(0.17 * sx, 0.42, 0.28), G(0.2 * sx, 0.44, -0.32), "Body", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch=0.0, roll=0.0, yaw=0.0, front=0.0, front_yaw=0.0, jaw=0.0, plates=(0.0, 0.0, 0.0)):
    """plates: (left roll, right roll, crest pitch) in radians: the slabs grinding against each other."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": offset if offset is not None else Vector()},
        "Front": {"rot": [(X, front), (Z, front_yaw)]},
        "Jaw": {"rot": [(X, -jaw)]},
        "Plate_L": {"rot": [(Yv, -plates[0])]},
        "Plate_R": {"rot": [(Yv, plates[1])]},
        "Plate_C": {"rot": [(X, plates[2])]},
    }
    return specs


def place_legs(poser, specs, feet):
    pose = poser.solve(specs)
    body = pose["Body"] @ poser.rest["Body"].inverted()
    for key, lg in legs().items():
        specs.update(lg.pose(poser, body @ lg.root, feet[key]))
    return specs


STRIDE, LIFT, STANCE = 0.32, 0.08, 0.65
WALK_FRAMES = 32


def impact(t, lands, rise=0.05, fall=9.0):
    """0..1 jolt after each footfall time in `lands`: a quick drop, then a slow recovery."""
    out = 0.0
    for land in lands:
        s = (t - land) % 1.0
        out = max(out, s / rise if s < rise else math.exp(-(s - rise) * fall))
    return out


def walk_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        hit = impact(t, (0.0, 0.5))
        side = 1 if (t % 1.0) < 0.5 else -1  # which tripod carries the weight
        specs = body_specs(offset=Vector((0, 0, -0.035 * hit + 0.01)), pitch=math.radians(2.5) * hit,
                           roll=math.radians(2.5) * math.sin(w), yaw=math.radians(2) * math.sin(w),
                           front=math.radians(5) * hit - math.radians(2), front_yaw=math.radians(3) * math.sin(w + 0.5),
                           jaw=math.radians(6) * hit,
                           plates=(math.radians(4) * hit * (side > 0) + math.radians(1.5) * math.sin(w),
                                   math.radians(4) * hit * (side < 0) - math.radians(1.5) * math.sin(w),
                                   math.radians(3) * hit))
        feet = {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key in TRIPOD_A else 0.5)) % 1.0
            # Phase 0 = the foot lands at the front: shift so the swing ends exactly on the footfall.
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        breathe = 0.5 + 0.5 * math.sin(w)
        grind = max(0.0, math.sin(2 * w + 0.7)) ** 3
        specs = body_specs(offset=Vector((0, 0, 0.008 * breathe)), roll=math.radians(1.2) * math.sin(w),
                           front=math.radians(3) * math.sin(w + 1.0), front_yaw=math.radians(10) * math.sin(w),
                           jaw=math.radians(4) + math.radians(10) * grind,
                           plates=(math.radians(2.5) * breathe, math.radians(2.5) * breathe, math.radians(2) * breathe))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch=-math.radians(5) * k, front=-math.radians(10) * k,
                           jaw=math.radians(18) * max(k, 0.0), plates=(math.radians(6) * k, math.radians(6) * k,
                                                                         math.radians(5) * k))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


def death_spec(poser):
    def spec(t, f):
        buckle = C.smoothstep(0.05, 0.55, np.array([t]))[0]
        slump = C.smoothstep(0.35, 0.85, np.array([t]))[0]
        settle = math.sin(math.pi * C.smoothstep(0.5, 0.7, np.array([t]))[0]) * 0.012
        specs = body_specs(offset=Vector((0, 0, -0.17 * buckle + settle)), roll=math.radians(6) * slump,
                           pitch=math.radians(6) * buckle, front=math.radians(18) * slump, jaw=math.radians(22) * slump,
                           plates=(math.radians(14) * slump, math.radians(10) * slump, -math.radians(8) * slump))
        feet = {}
        for key, lg in legs().items():
            sx = -1 if key[0] == "L" else 1
            # The legs splay out sideways as the weight comes down.
            feet[key] = lg.foot + Vector((sx * 0.12, 0, 0)) * buckle
        return place_legs(poser, specs, feet)
    return 40, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
# LOD1: plates fold into the body, each leg is one bone from hip to foot. 10 bones.
LOD_DROP = {"Plate_L": "Body", "Plate_R": "Body", "Plate_C": "Body",
            **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in PAIRS}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in PAIRS}
# Footfalls (frame of the Walk clip) for dust puffs and a camera-free "thud" sound in Unity.
FOOTFALL_FRAMES = (0, WALK_FRAMES // 2)


def old_proxy():
    """EnemyModelFactory.Cragback in primitives, flat colours (for the compare sheet)."""
    body, dark, accent, eyes = (0.4, 0.36, 0.32), (0.16, 0.14, 0.13), (0.85, 0.42, 0.14), (1.0, 0.75, 0.3)
    groups = {c: C.PartBuilder() for c in (body, dark, accent, eyes)}
    groups[dark].ellipsoid("Base", G(0, 0.14, 0), (0.3, 0.36, 0.08))
    groups[body].ellipsoid("Dome", G(0, 0.3, -0.04), (0.34, 0.4, 0.25))
    for p in ((0, 0.53, -0.06), (0.2, 0.46, 0.1), (-0.21, 0.45, 0.06), (0.23, 0.43, -0.22), (-0.2, 0.44, -0.24),
              (0.02, 0.48, 0.22), (-0.03, 0.46, -0.33)):
        groups[dark].ellipsoid("Plate", G(*p), (0.13, 0.12, 0.05))
    groups[dark].ellipsoid("Brow", G(0, 0.31, 0.36), (0.23, 0.11, 0.06))
    groups[body].ellipsoid("Head", G(0, 0.2, 0.4), (0.14, 0.1, 0.09))
    for i in range(3):
        groups[eyes].ellipsoid("Eye", G(0, 0.25 - i * 0.045, 0.49), (0.02,) * 3)
    for tip, base in ((G(0.1, 0.78, -0.2), G(0.06, 0.55, -0.12)), (G(-0.15, 0.7, 0.02), G(-0.08, 0.52, -0.02))):
        groups[accent].tube("Spire", [base, tip], [0.04, 0.003])
    for sx in (-1, 1):
        for z in (0.22, 0.0, -0.22):
            groups[dark].tube("Leg", [G(0.22 * sx, 0.16, z), G(0.34 * sx, 0.1, z), G(0.33 * sx, 0.0, z + 0.03)], [0.05, 0.04, 0.02])
    return C.flat_proxy(groups, "OldCragback")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    target = Vector((0, -0.05, 0.3))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 2.4, azimuth=-38, elevation=26, lens=55)
    C.set_action(arm, actions["Idle"], 20)
    paths.append(C.render(close, os.path.join(out, "cragback_close.png"), 1280, 720))
    side = C.closeup_camera(target, 2.5, azimuth=-90, elevation=8, lens=55)
    C.set_action(arm, actions["Walk"], 0)
    paths.append(C.render(side, os.path.join(out, "cragback_side.png"), 1280, 720))
    front = C.closeup_camera(target, 1.9, azimuth=-14, elevation=12, lens=55)
    C.set_action(arm, actions["Hit"], 4)
    paths.append(C.render(front, os.path.join(out, "cragback_front.png"), 1280, 720))
    top = C.closeup_camera(target, 2.5, azimuth=-160, elevation=58, lens=55)
    C.set_action(arm, actions["Walk"], 8)
    paths.append(C.render(top, os.path.join(out, "cragback_top.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 40)
    paths.append(C.render(close, os.path.join(out, "cragback_dead.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Walk"], side, out, "cragback_walk", range(0, WALK_FRAMES, 4))]

    C.set_action(lod_arm, lod_actions["Walk"], 8)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "cragback_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 20)
    macros = [("cragback_macro_back.png", G(0.12, 0.58, -0.08), 0.45, -50, 35),
              ("cragback_macro_maw.png", G(0.0, 0.3, 0.42), 0.5, -25, 8),
              ("cragback_macro_leg.png", G(-0.42, 0.12, 0.25), 0.45, -70, 12)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    walk = actions["Walk"]
    layout = [(walk, 0, (-1.2, 0.5, 0), 0.2), (walk, 10, (0.2, 0.7, 0), -0.1), (walk, 20, (1.3, 0.3, 0), 0.3),
              (walk, 5, (-0.5, -0.6, 0), -0.3), (walk, 27, (0.8, -0.6, 0), 0.0)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "cragback_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "cragback_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "cragback_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "cragback_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Idle"], 20)
    single = C.game_view(arm, mesh, os.path.join(out, "cragback_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare4_cragback.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_cragback.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "cragback_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.004, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.1,
                               extrusion=0.016, ray=0.05, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.3, dust=0.7, seed=61), specular=0.25,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "walk_distance_per_loop": round(STRIDE / STANCE, 3), "footfall_frames": list(FOOTFALL_FRAMES),
                          "death_emission_fade": {"from_frame": 8, "to_frame": 40, "curve": "ease-out to 0"},
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)


# Slabs and the jaw are stone: they move as one piece and meet their neighbours in a narrow soft crease.
RIGID = {"PlateC", "PlateSide", "Skirt", "Shoulder", "Rump", "Brow", "Jaw", "Crysium", "Tusk", "Stalactite", "Knee"}
QUICK_TARGET = (0, 0, 0.3)

if __name__ == "__main__":
    main()
