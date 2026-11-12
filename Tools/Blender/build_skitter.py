"""Skitter: the small swarm Burrower (round 2 design, see ArtSource/DESIGN_NOTES.md).

Four spiked walking legs and two raised fore-limbs ending in serrated blades held forward past the head; an eyeless
wedge head under a serrated shovel crest, with glowing sensor slits and a mouth of four hooked jaws in an X; no waist,
a low rising body with hooks along the spine and flanks, lime venom glowing through the membrane of the abdomen and
two Crysium shards grown into the back. Rust-orange plates on a near-black body so it reads at ~20 pixels.

Also exports Skitter_LOD1: about half the triangles, the same textures, 16 bones (2 per leg, no jaw bones).

Run: blender --background --python Tools/Blender/build_skitter.py -- [--preview <dir>] [--compare <round-1 dir>] [--norender]
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
from common import G  # noqa: E402

NAME = "Skitter"
LOD_NAME = "Skitter_LOD1"
K = 1.0
GAME_TRIS = 3800
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
MAPS = ["Albedo", "Normal", "Emission", "Mask"]
LOD_TRIS = 1700
# Species colours from Data/Enemies/Skitter.asset (orange body, pale-yellow eyes, lime venom), pushed darker so the
# glow carries: rust plates over a near-black body.
PAL = H.Palette(top=(0.8, 0.33, 0.07), side=(0.3, 0.09, 0.025), belly=(0.05, 0.03, 0.02), seam=(0.035, 0.018, 0.01),
                dark=(0.07, 0.035, 0.02), glow=(0.68, 1.0, 0.18), eye=(0.9, 1.0, 0.35), membrane=(0.32, 0.4, 0.06),
                bone=(0.95, 0.8, 0.55))

LEG_SPECS = {
    # pair: root (x, up, forward), foot (x, up, forward)
    1: ((0.062, 0.17, 0.07), (0.31, 0.0, 0.2)),
    2: ((0.068, 0.17, -0.045), (0.33, 0.0, -0.29)),
}
COXA, FEMUR, TIBIA = 0.05, 0.21, 0.28
COXA_LIFT = math.radians(35)
TROT_A = {("L", 1), ("R", 2)}
SIDES = (("L", -1), ("R", 1))
JAWS = ("UL", "UR", "LL", "LR")


def legs():
    out = {}
    for side, sx in SIDES:
        for i, (root, foot) in LEG_SPECS.items():
            names = [f"Leg_{side}{i}_{s}" for s in ("Coxa", "Femur", "Tibia")]
            out[(side, i)] = C.Leg(names, G(root[0] * sx, root[1], root[2]), G(foot[0] * sx, foot[1], foot[2]),
                                   COXA, FEMUR, TIBIA, COXA_LIFT)
    return out


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def jaw_points(name):
    sx = -1 if name[1] == "L" else 1
    if name[0] == "U":
        return ([G(0.03 * sx, 0.14, 0.3), G(0.075 * sx, 0.165, 0.37), G(0.062 * sx, 0.155, 0.44), G(0.018 * sx, 0.135, 0.478)])
    return ([G(0.03 * sx, 0.1, 0.3), G(0.072 * sx, 0.075, 0.365), G(0.056 * sx, 0.08, 0.432), G(0.015 * sx, 0.104, 0.468)])


def blade_points(sx):
    shoulder = G(0.065 * sx, 0.2, 0.11)
    elbow = G(0.155 * sx, 0.31, 0.17)
    return shoulder, elbow, [elbow, G(0.15 * sx, 0.25, 0.33), G(0.105 * sx, 0.155, 0.5), G(0.068 * sx, 0.105, 0.6)]


# --------------------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------------------

def crest_deform(v):
    """A flat arrowhead hood: pointed at the front, its side rims cut into backward-pointing teeth."""
    front = max(0.0, -v.y)
    x = v.x * (1 - 0.6 * front ** 1.5)
    saw = ((v.y + 1) * 3.0) % 1.0
    x *= 1 + 0.3 * saw * (1 - abs(v.z)) * (1 - 0.5 * front)
    return Vector((x, v.y * (1 + 0.2 * front), v.z))


def head_deform(v):
    front = max(0.0, -v.y)
    return Vector((v.x * (1 - 0.38 * front), v.y, v.z * (1 - 0.3 * front)))


def abdomen_deform(v):
    rear = max(0.0, v.y)
    return Vector((v.x * (1 - 0.4 * rear), v.y, v.z * (1 - 0.3 * rear) * (0.85 if v.z > 0 else 1.0)))


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Thorax", G(0, 0.195, 0.0), (0.092, 0.14, 0.07), pitch(8), bone="Thorax",
                style=H.band_plates(PAL, k, 2, height=0.004), segments=(40, 20))
    b.ellipsoid("Head", G(0, 0.15, 0.24), (0.07, 0.095, 0.05), pitch(18), bone="Head",
                style=H.wedge_head(PAL, k), segments=(40, 20), deform=head_deform)
    b.ellipsoid("Crest", G(0, 0.222, 0.17), (0.088, 0.135, 0.022), pitch(20), bone="Head", style=H.crest(PAL, k),
                segments=(48, 20), deform=crest_deform)
    for name in JAWS:
        b.tube("Jaw", jaw_points(name), [0.018, 0.016, 0.011, 0.002], sides=10, chain=[f"Jaw_{name}"],
               style=H.tube(PAL, k, PAL.dark, PAL.bone * 0.8, tip_glow=1.0, glow_from=0.75), flatten=0.6)
    b.ellipsoid("Throat", G(0, 0.12, 0.31), (0.022, 0.02, 0.02), bone="Head", style=H.inner_glow(PAL, 0.9))

    for side, sx in SIDES:
        shoulder, elbow, blade = blade_points(sx)
        b.tube("BladeArm", [shoulder, shoulder.lerp(elbow, 0.5) + G(0, 0.015, -0.01), elbow], [0.022, 0.02, 0.017],
               sides=10, chain=[f"Blade_{side}_1"], style=H.leg(PAL, k, "Femur"))
        b.tube("Blade", blade, [0.018, 0.038, 0.03, 0.0015], sides=12, chain=[f"Blade_{side}_2"],
               style=H.blade(PAL, k, glow=0.9, teeth=5), flatten=0.32)
        curve = C.catmull_rom([Vector(p) for p in blade], 6)
        for t in (0.35, 0.55, 0.75):
            root = curve[int(t * (len(curve) - 1))]
            b.tube("Barb", [root, root + G(-0.028 * sx, 0.004, -0.026)], [0.008, 0.001], sides=8,
                   chain=[f"Blade_{side}_2"], style=H.hook(PAL, k))

    b.ellipsoid("Abdomen", G(0, 0.235, -0.2), (0.118, 0.2, 0.085), pitch(14), bone="Abdomen_1",
                style=H.membrane_sac(PAL, k, bands=3, cells=40, cell_size=0.14, glow=1.0, plate_glow=0.12),
                segments=(48, 24), deform=abdomen_deform).chain = ["Abdomen_1", "Abdomen_2"]
    b.tube("Tail", [G(0, 0.265, -0.38), G(0, 0.305, -0.47), G(0, 0.285, -0.535)], [0.03, 0.014, 0.0015], sides=10,
           chain=["Abdomen_2"], style=H.hook(PAL, k, glow=1.2))
    # Hooks along the spine, growing towards the rear, and pairs on the flanks: a serrated outline from above.
    for fwd, up, size, bone in ((0.0, 0.245, 0.8, "Thorax"), (-0.13, 0.305, 1.0, "Abdomen_1"),
                                (-0.23, 0.325, 1.15, "Abdomen_1"), (-0.32, 0.315, 0.9, "Abdomen_2")):
        base = G(0, up, fwd)
        b.tube("Spine", [base, base + G(0, 0.05 * size, -0.025 * size), base + G(0, 0.062 * size, -0.07 * size)],
               [0.017 * size, 0.009 * size, 0.0012], sides=10, chain=[bone], style=H.hook(PAL, k, glow=1.0, glow_from=0.7))
    for side, sx in SIDES:
        for fwd, bone in ((-0.15, "Abdomen_1"), (-0.27, "Abdomen_2")):
            base = G(0.095 * sx, 0.245, fwd)
            b.tube("FlankHook", [base, base + G(0.065 * sx, 0.03, -0.02), base + G(0.09 * sx, 0.02, -0.075)],
                   [0.015, 0.008, 0.001], sides=8, chain=[bone], style=H.hook(PAL, k))
        # Crysium grown into the chitin: the Burrowers live in the crystal layer.
        b.shard("Crysium", G(0.05 * sx, 0.225, 0.06), G(0.6 * sx, 1.0, -0.35), 0.017, 0.1, sides=8, twist=2.0,
                flute=0.4, bone="Thorax", style=Y.shard_style("pink", 1.0, seed=3))

    for (side, i), lg in legs().items():
        root, c, kn, f, yaw = lg.joints(lg.root, lg.foot)
        tip = f + (f - kn).normalized() * 0.012
        b.tube("Coxa", [root, c], [0.032, 0.028], sides=10, chain=[lg.names[0]], style=H.leg(PAL, k, "Coxa"))
        b.tube("Femur", [c, c.lerp(kn, 0.5) + Vector((0, 0, 0.012)), kn], [0.024, 0.026, 0.019], sides=10,
               chain=[lg.names[1]], style=H.leg(PAL, k, "Femur", spines=3))
        # The shin bows outward like an extra reversed joint and thins to a spike.
        kink = kn.lerp(f, 0.42) + yaw * 0.03 + Vector((0, 0, 0.01))
        b.tube("Tibia", [kn, kink, tip], [0.018, 0.014, 0.0015], sides=10, chain=[lg.names[2]],
               style=H.leg(PAL, k, "Tibia"))
        b.tube("Spur", [kn, kn + Vector((0, 0, 0.055)) - yaw * 0.012], [0.012, 0.001], sides=8, chain=[lg.names[1]],
               style=H.hook(PAL, k))
    return b


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
        ("Thorax", G(0, 0.19, -0.08), G(0, 0.2, 0.12), "Root", True),
        ("Head", G(0, 0.19, 0.13), G(0, 0.13, 0.33), "Thorax", True),
        ("Abdomen_1", G(0, 0.21, -0.08), G(0, 0.25, -0.26), "Thorax", True),
        ("Abdomen_2", G(0, 0.25, -0.26), G(0, 0.29, -0.52), "Abdomen_1", True),
    ]
    for name in JAWS:
        pts = jaw_points(name)
        out.append((f"Jaw_{name}", pts[0], pts[-1], "Head", True))
    for side, sx in SIDES:
        shoulder, elbow, blade = blade_points(sx)
        out.append((f"Blade_{side}_1", shoulder, elbow, "Thorax", True))
        out.append((f"Blade_{side}_2", elbow, blade[-1], f"Blade_{side}_1", True))
    for lg in legs().values():
        out += lg.rest_bones("Thorax")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation (spec functions are shared by the full rig and the LOD rig)
# --------------------------------------------------------------------------------------------------------------

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def body_specs(bob=0.0, roll=0.0, sway=0.0, head_pitch=0.0, head_yaw=0.0, jaw=0.0, blade=0.0, blade_spread=0.0,
               reach=0.0, belly=0.0, thorax_pitch=0.0, offset=None):
    """jaw: how far the four jaws splay open (rad). blade: fore-limb raise (+ lifts the blade tips), reach: lowers
    the blade itself forward, blade_spread: swings both blades outward."""
    specs = {
        "Root": {"rot": []},
        "Thorax": {"rot": [(Yv, roll), (X, thorax_pitch)], "loc": offset if offset is not None else Vector((0, 0, bob))},
        "Head": {"rot": [(X, head_pitch), (Z, head_yaw)]},
        "Abdomen_1": {"rot": [(Z, sway), (X, belly)]},
        "Abdomen_2": {"rot": [(Z, sway * 0.8), (X, belly * 0.6)]},
    }
    for name in JAWS:
        sx = -1 if name[1] == "L" else 1
        up = -1 if name[0] == "U" else 1
        specs[f"Jaw_{name}"] = {"rot": [(X, up * jaw * 0.8), (Z, sx * jaw * 0.6)]}
    for side, sx in SIDES:
        specs[f"Blade_{side}_1"] = {"rot": [(X, -blade), (Z, sx * blade_spread)]}
        specs[f"Blade_{side}_2"] = {"rot": [(X, reach)]}
    return specs


def place_legs(poser, specs, feet):
    pose = poser.solve(specs)
    thorax = pose["Thorax"] @ poser.rest["Thorax"].inverted()
    for key, lg in legs().items():
        specs.update(lg.pose(poser, thorax @ lg.root, feet[key]))
    return specs


def walk_spec(poser):
    stride, lift = 0.15, 0.06

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(bob=0.007 * math.cos(2 * w), roll=math.radians(3) * math.sin(w),
                           sway=math.radians(6) * math.sin(w + 0.6), head_pitch=math.radians(3) * math.sin(2 * w + 1.0),
                           head_yaw=math.radians(4) * math.sin(w), jaw=math.radians(8) * (0.5 + 0.5 * math.sin(2 * w)),
                           blade=math.radians(6) * math.sin(w), blade_spread=math.radians(3) * math.sin(w + 1.2),
                           reach=math.radians(5) * math.sin(2 * w), belly=math.radians(3) * math.sin(2 * w + 0.4),
                           thorax_pitch=math.radians(2) * math.sin(2 * w))
        feet = {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key in TROT_A else 0.5)) % 1.0
            if phase < 0.5:
                s = phase / 0.5
                fwd, up = stride * (0.5 - s), 0.0
            else:
                s = (phase - 0.5) / 0.5
                fwd = stride * (-0.5 + (1 - math.cos(math.pi * s)) * 0.5)
                up = lift * math.sin(math.pi * s)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet)
    return 24, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        # The jaws snap open twice per loop, the blades twitch like a mantis waiting.
        snap = max(0.0, math.sin(2 * w)) ** 3
        specs = body_specs(bob=0.004 * math.sin(w), roll=math.radians(1.5) * math.sin(w),
                           sway=math.radians(4) * math.sin(w + 1.0), belly=math.radians(4) * math.sin(2 * w),
                           head_yaw=math.radians(9) * math.sin(w) * math.sin(w * 0.5 + 0.3),
                           head_pitch=math.radians(4) * math.sin(2 * w + 0.5), jaw=math.radians(26) * snap,
                           blade=math.radians(10) * math.sin(w + 0.4) + math.radians(6) * snap,
                           blade_spread=math.radians(5) * math.sin(3 * w), reach=math.radians(-8) * snap)
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 60, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((0, 0.03 * k, 0.022 * k)), thorax_pitch=math.radians(-12) * k,
                           head_pitch=math.radians(-16) * k, belly=math.radians(10) * k, jaw=math.radians(35) * max(k, 0),
                           blade=math.radians(25) * k, blade_spread=math.radians(12) * k, reach=math.radians(-15) * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 12, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        flip = C.smoothstep(0.15, 0.62, np.array([t]))[0]
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 60) * (1 if t > 0.6 else 0)
        angle = math.pi * flip
        pivot = Vector((0, 0, 0.15))
        rot = Matrix.Rotation(angle, 3, "Y")
        hop = 0.06 * math.sin(math.pi * flip)
        specs = body_specs(offset=Vector((0, 0, 0.03 * jerk + hop)), head_pitch=math.radians(25) * flip,
                           belly=math.radians(-12) * flip, jaw=math.radians(40) * flip + math.radians(6) * twitch,
                           blade=math.radians(-35) * flip, reach=math.radians(50) * flip + math.radians(8) * twitch)
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - rot @ pivot}
        curl = C.smoothstep(0.1, 0.7, np.array([t]))[0]
        for (side, i), lg in legs().items():
            sx = -1 if side == "L" else 1
            _, _, _, _, yaw_dir = lg.joints(lg.root, lg.foot)
            hinge = Z.cross(yaw_dir).normalized()
            kick = twitch * (0.25 if (side, i) in TROT_A else -0.25)
            specs[lg.names[0]] = {"rot": [(Yv, -sx * math.radians(20) * curl)]}
            specs[lg.names[1]] = {"rot": [(hinge, math.radians(60) * curl + kick)]}
            specs[lg.names[2]] = {"rot": [(hinge, math.radians(75) * curl - kick)]}
        return specs
    return 36, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}


# --------------------------------------------------------------------------------------------------------------
# LOD1: 2-bone legs (coxa folded into the femur), no jaw bones, one abdomen bone: 16 bones
# --------------------------------------------------------------------------------------------------------------

LOD_DROP = {"Abdomen_2": "Abdomen_1", **{f"Jaw_{n}": "Head" for n in JAWS}}


def lod_bones():
    out = []
    full = {b[0]: b for b in bones()}
    for name, head, tail, parent, deform in bones():
        if name in LOD_DROP or name.endswith("_Coxa"):
            continue
        if name.endswith("_Femur"):
            coxa = full[name.replace("_Femur", "_Coxa")]
            out.append((name, coxa[1], tail, "Thorax", deform))
        else:
            out.append((name, head, tail, parent, deform))
    return out


def lod_remap(name):
    if name in LOD_DROP:
        return LOD_DROP[name]
    if name.endswith("_Coxa"):
        return name.replace("_Coxa", "_Femur")
    return name


def lod_action(full_poser, lod_poser, name, frames, spec_fn, loop):
    """Keys the LOD rig from the full rig's pose: shared bones copy their armature-space pose, each 2-bone femur
    is aimed from the posed leg root to the posed knee."""
    lod_poser.arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    lod_poser.arm.animation_data.action = act
    lod_poser.last = {}
    for f in range(frames + 1):
        t = (f % frames) / frames if loop else f / frames
        pose = full_poser.solve(spec_fn(t, f))
        specs = {}
        for bname in lod_poser.order:
            if bname.endswith("_Femur"):
                coxa = bname.replace("_Femur", "_Coxa")
                head = pose[coxa].translation
                knee = pose[bname.replace("_Femur", "_Tibia")].translation
                rest = lod_poser.rest[bname].to_3x3()
                swing = rest.col[1].rotation_difference((knee - head).normalized()).to_matrix()
                # Keep the femur's roll from the posed coxa so it yaws with the leg.
                m = (swing @ rest).to_4x4()
                m.translation = head
                specs[bname] = {"abs": m}
            else:
                specs[bname] = {"abs": pose[bname]}
        lod_poser.key(lod_poser.solve(specs), f)
    return act


def build_lod(asset, builder, actions_full, full_poser):
    lod_mesh = C.decimate(asset.low.data.copy(), LOD_TRIS, LOD_NAME, symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(LOD_NAME, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    arm = C.build_armature(LOD_NAME + "_Rig", lod_bones())
    saved = [(p, list(p.chain), p.bone) for p in builder.parts]
    for p in builder.parts:
        p.chain = list(dict.fromkeys(lod_remap(c) for c in p.chain))
        p.bone = p.chain[0] if p.chain else p.bone
    C.skin(lod, arm, builder)
    for p, chain, bone in saved:
        p.chain, p.bone = chain, bone
    poser = C.Poser(arm)
    actions = {}
    for clip, fn in CLIPS.items():
        frames, spec, loop = fn(full_poser)
        actions[clip] = lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop)
    arm.animation_data.action = actions["Idle"]
    # Action names are unique per .blend: lend the plain clip names to the LOD actions for its export, so Unity
    # sees the same take names (Walk, Idle, Hit, Death) on both FBX files.
    for clip, act in actions_full.items():
        act.name = clip + "_Full"
    for clip, act in actions.items():
        act.name = clip
    C.export_fbx(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), [arm, lod], animated=True)
    for clip, act in actions.items():
        act.name = clip + "_LOD"
    for clip, act in actions_full.items():
        act.name = clip
    return arm, lod, actions


# --------------------------------------------------------------------------------------------------------------
# Previews
# --------------------------------------------------------------------------------------------------------------

def set_action(arm, act, frame):
    arm.animation_data.action = act
    bpy.context.scene.frame_set(frame)


def previews(arm, mesh, actions, lod, out, asset_ref):
    C.preview_world()
    rig = C.light_rig(scale=1.0)
    C.ground()
    target = Vector((0, -0.04, 0.17))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    set_action(arm, actions["Idle"], 15)
    close = C.closeup_camera(target, 1.9, azimuth=-38, elevation=26, lens=55)
    paths.append(C.render(close, os.path.join(out, "skitter_close.png"), 1280, 720))
    side = C.closeup_camera(target, 1.9, azimuth=-90, elevation=8, lens=55)
    set_action(arm, actions["Walk"], 0)
    paths.append(C.render(side, os.path.join(out, "skitter_side.png"), 1280, 720))
    front = C.closeup_camera(target, 1.5, azimuth=-8, elevation=16, lens=55)
    set_action(arm, actions["Hit"], 4)
    paths.append(C.render(front, os.path.join(out, "skitter_front.png"), 1280, 720))
    top = C.closeup_camera(target + Vector((0, 0.05, 0)), 2.0, azimuth=-160, elevation=60, lens=55)
    set_action(arm, actions["Walk"], 6)
    paths.append(C.render(top, os.path.join(out, "skitter_top.png"), 1280, 720))
    set_action(arm, actions["Death"], 36)
    paths.append(C.render(close, os.path.join(out, "skitter_dead.png"), 1280, 720))

    strip = []
    for i, frame in enumerate(range(0, 24, 3)):
        set_action(arm, actions["Walk"], frame)
        strip.append(C.render(side, os.path.join(out, f"skitter_walk_{i}.png"), 640, 360, samples=24))
    C.contact_sheet(strip, os.path.join(out, "skitter_walk_strip.png"), 4, 480)

    # LOD1 next to LOD0 in the close-up framing.
    set_action(lod_arm, lod_actions["Idle"], 15)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "skitter_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    # Macro close-ups (round 3): abdomen plates and membrane, the crest and blade, a leg; with the tiling detail
    # normals as Unity will add them, and once without them (the baked maps alone).
    set_action(arm, actions["Idle"], 15)
    macros = [("skitter_macro_back.png", G(0.05, 0.3, -0.16), 0.3, -55, 40),
              ("skitter_macro_head.png", G(0.04, 0.2, 0.2), 0.32, -30, 22),
              ("skitter_macro_leg.png", G(-0.19, 0.27, 0.12), 0.34, -80, 28)]
    for file_name, aim, dist, az, el in macros:
        cam = C.macro_camera(aim, dist, az, el)
        paths.append(C.render(cam, os.path.join(out, file_name), 1280, 720))
    C.set_detail_preview(asset_ref[0], False)
    cam = C.macro_camera(macros[0][1], macros[0][2], macros[0][3], macros[0][4])
    paths.append(C.render(cam, os.path.join(out, "skitter_macro_back_bakeonly.png"), 1280, 720))
    C.set_detail_preview(asset_ref[0], True)

    set_action(arm, actions["Walk"], 6)
    paths.append(C.game_view(arm, mesh, os.path.join(out, "skitter_game.png"), 0.45, 9, (1.4, 1.0), close_lights=rig))
    paths.append(C.game_view(arm, mesh, os.path.join(out, "skitter_game_zoomed.png"), 0.45, 9, (1.4, 1.0), zoom=2.5,
                             close_lights=rig))
    C.contact_sheet(paths + [os.path.join(out, "skitter_walk_strip.png")], os.path.join(out, "sheet_skitter.png"), 2, 640)
    C.compare_sheet("skitter", ["skitter_close.png", "skitter_side.png", "skitter_front.png", "skitter_top.png",
                                "skitter_game_zoomed.png", "skitter_game.png"], out)
    C.uv_layout(mesh, os.path.join(out, "skitter_uv.png"), background=asset_ref[0].textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0032, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.08,
                               extrusion=0.012, ray=0.04, emission_strength=2.5, extra=H.weathering(K, markings=PAL.dark),
                               specular=0.3, mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    arm = C.build_armature(NAME + "_Rig", bones())
    C.skin(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = {}
    for clip, fn in CLIPS.items():
        frames, spec, loop = fn(poser)
        actions[clip] = poser.action(clip, frames, spec, loop=loop)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = build_lod(asset, builder, actions, poser)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": MAPS, "uv": asset.uv_stats, "detail": asset.details, "actions": list(actions),
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, [asset])
    # Round 46: the close-up detail (eyes, jaw teeth, blade saws, talons) as extra parts, exported again with the body.
    import skitter_detail
    skitter_detail.run(norender=bool(C.cli_args().get("norender")))


main()
