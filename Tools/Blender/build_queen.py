"""Hive Queen: the swarm boss (round 2 design, see ArtSource/DESIGN_NOTES.md).

The forebody rears up almost vertically above four armoured stilt legs, like a centaur. The head splits into four
petal jaws around a glowing throat, with clusters of small eyes at the petal roots; a crown of twisted Crysium grows
out of the back of the head and down the ridge of the forebody. Two barbed scythes are held high. The swollen egg
sac hangs low behind, a glowing membrane under armoured ribs that grow into rows of hooks.

Run: blender --background --python Tools/Blender/build_queen.py -- [--preview <dir>] [--compare <round-1 dir>] [--norender]
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

NAME = "Queen"
K = 4.0  # size factor against the Skitter for displacement depths and noise scales
GAME_TRIS = 14500
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
# Micro-detail size factor: a little below K, so the boss's pores are relatively finer than the Skitter's (a
# bigger animal, not a scaled-up one) while staying above the texel size of its 1024 map.
DETAIL_K = 3.0
# Species colours from Data/Enemies/HiveQueen.asset: deep crimson body, pink-red glow and eyes; darker than round 1
# so the crown, eggs and throat carry the eye.
PAL = H.Palette(top=(0.52, 0.08, 0.1), side=(0.24, 0.03, 0.05), belly=(0.05, 0.01, 0.015), seam=(0.035, 0.008, 0.012),
                dark=(0.09, 0.02, 0.03), glow=(1.0, 0.34, 0.46), eye=(1.0, 0.55, 0.45), membrane=(0.42, 0.06, 0.13),
                bone=(0.82, 0.58, 0.52))
LEG = H.Palette(top=(0.36, 0.05, 0.07), side=(0.17, 0.025, 0.035), belly=(0.04, 0.01, 0.012), seam=(0.03, 0.006, 0.01),
                dark=(0.07, 0.015, 0.02), glow=PAL.glow, eye=PAL.eye, bone=(0.78, 0.55, 0.5))

LEG_SPECS = {
    1: ((0.33, 0.9, 0.18), (1.5, 0.0, 0.95)),
    2: ((0.36, 0.88, -0.2), (1.65, 0.0, -0.95)),
}
COXA, FEMUR, TIBIA = 0.2, 1.05, 1.55
COXA_LIFT = math.radians(42)
SIDES = (("L", -1), ("R", 1))
PETALS = ("UL", "UR", "LL", "LR")
# Footfall order of a slow, heavy four-legged walk: one leg in the air at a time.
STEP_PHASE = {("L", 1): 0.0, ("R", 2): 0.25, ("R", 1): 0.5, ("L", 2): 0.75}
SAC_CENTRE, SAC_RADII = (0.0, 0.72, -1.25), (0.68, 1.1, 0.52)


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


def petal_points(name):
    sx = -1 if name[1] == "L" else 1
    sy = 1 if name[0] == "U" else -1
    return [G(0.13 * sx, 1.86 + 0.1 * sy, 0.84), G(0.24 * sx, 1.82 + 0.17 * sy, 1.06),
            G(0.13 * sx, 1.7 + 0.09 * sy, 1.28), G(0.025 * sx, 1.62 + 0.02 * sy, 1.4)]


def arm_points(sx):
    shoulder = G(0.27 * sx, 1.55, 0.5)
    elbow = G(0.8 * sx, 2.35, 0.55)
    # A broad sickle: out and down from the raised elbow, the point hooking back in towards the face.
    return shoulder, elbow, [elbow, G(0.92 * sx, 2.0, 0.95), G(0.78 * sx, 1.4, 1.4), G(0.45 * sx, 1.05, 1.55),
                             G(0.25 * sx, 1.12, 1.4)]


def sac_top(x, fwd):
    cx, cy, cz = SAC_CENTRE
    rx, ry, rz = SAC_RADII
    q = 1 - ((fwd - cz) / ry) ** 2 - (x / rx) ** 2
    return cy + rz * math.sqrt(max(0.0, q))


def chest_back(t, out=0.22):
    """A point on the back ridge of the raised forebody, t 0 (low) .. 1 (neck)."""
    low, high = Vector((0.0, 1.15, 0.2)), Vector((0.0, 1.9, 0.55))  # (x, up, forward)
    axis = (high - low).normalized()
    back = Vector((0.0, axis.z, -axis.y))  # perpendicular, pointing up-backwards
    p = low.lerp(high, t) + back * out
    return G(p.x, p.y, p.z), G(0, back.y, back.z)


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Thorax", G(0, 0.98, -0.05), (0.48, 0.5, 0.36), pitch(-10), bone="Thorax",
                style=H.band_plates(PAL, k, 3, height=0.005), segments=(48, 24))
    b.ellipsoid("Chest", G(0, 1.5, 0.38), (0.36, 0.5, 0.3), pitch(-60), bone="Chest",
                style=H.band_plates(PAL, k, 4, height=0.005, glow=0.35), segments=(48, 24))

    def head_deform(v):
        back = max(0.0, v.y)
        return Vector((v.x * (1 + 0.25 * back * max(0.0, v.z + 0.3)), v.y, v.z * (1 + 0.2 * back)))
    b.ellipsoid("Head", G(0, 2.0, 0.68), (0.25, 0.28, 0.2), pitch(30), bone="Head", style=H.helmet_plates(PAL, k),
                segments=(48, 24), deform=head_deform)
    b.ellipsoid("Throat", G(0, 1.83, 0.86), (0.075, 0.075, 0.075), bone="Head", style=H.inner_glow(PAL, 1.0))
    for name in PETALS:
        b.tube("Petal", petal_points(name), [0.11, 0.13, 0.08, 0.01], sides=14, chain=[f"Petal_{name}"],
               style=H.blade(PAL, k, glow=0.9), flatten=0.42)
    for side, sx in SIDES:
        # A V-shaped brow of tiny eyes over the petals: no pair of "eyes", so no face to read as cute.
        for i in range(5):
            x = 0.03 + 0.055 * i
            b.ellipsoid("Eye", G(x * sx, 2.02 + 0.045 * i, 0.86 - 0.035 * i), (0.028, 0.028, 0.028), bone="Head",
                        style=H.eye(PAL, k, 0.8))
        shoulder, elbow, blade = arm_points(sx)
        b.tube("ArmUpper", [shoulder, shoulder.lerp(elbow, 0.5) + G(0.04 * sx, 0.04, -0.06), elbow], [0.14, 0.12, 0.1],
               sides=14, chain=[f"Arm_{side}_1"], style=H.leg(LEG, k, "Femur", spines=4))
        b.tube("Scythe", blade, [0.09, 0.22, 0.2, 0.12, 0.008], sides=16, chain=[f"Arm_{side}_2"],
               style=H.blade(PAL, k, glow=1.0, teeth=7), flatten=0.28)
        curve = C.catmull_rom([Vector(p) for p in blade], 6)
        for t in (0.25, 0.38, 0.5, 0.62, 0.74):
            root = curve[int(t * (len(curve) - 1))] + G(-0.12 * sx, 0, 0)
            b.tube("Barb", [root, root + G(-0.14 * sx, 0.04, -0.08)], [0.045, 0.003], sides=8,
                   chain=[f"Arm_{side}_2"], style=H.hook(PAL, k, glow=0.6))

    # Crown: twisted Crysium fused into the back of the head and down the ridge of the forebody.
    for i in range(5):
        a = (i - 2) / 2.0
        base = G(0.13 * a, 2.1 - 0.03 * abs(a), 0.58 - 0.04 * abs(a))
        b.shard("Crown", base, G(0.5 * a, 1.0, -0.45), 0.075 - 0.012 * abs(a), 0.85 - 0.25 * abs(a), sides=8,
                twist=2.6 * (1 if a >= 0 else -1), flute=0.45, bone="Head", style=Y.shard_style("pink", 1.1, seed=i))
    for j, (t, length) in enumerate(((0.75, 0.55), (0.45, 0.45), (0.15, 0.36))):
        for sx in (-1, 1):
            base, back = chest_back(t)
            base = base + G(0.09 * sx, 0, 0)
            b.shard("Ridge", base, back + G(0.35 * sx, 0.25, 0), 0.055, length, sides=8, twist=2.2 * sx, flute=0.45,
                    bone="Chest", style=Y.shard_style("pink", 1.0, seed=10 + j))

    def sac_deform(v):
        # Knobbly egg lobes break the smooth spider-abdomen ellipse.
        rear = max(0.0, v.y)
        lobes = 1 + 0.08 * math.sin(5 * math.atan2(v.z, v.x)) * math.sin(4.5 * v.y + 0.5) * (1 - abs(v.y))
        return Vector((v.x * (1 - 0.25 * rear) * lobes, v.y, (v.z * (1 - 0.2 * rear) - 0.08 * rear * rear) * lobes))
    b.ellipsoid("Abdomen", G(*SAC_CENTRE), SAC_RADII, pitch(6), bone="Abdomen_1",
                style=H.membrane_sac(PAL, k, bands=5, plate_line=0.3, cells=55, cell_size=0.11, glow=1.0, plate_glow=0.3),
                segments=(64, 32), deform=sac_deform).chain = ["Abdomen_1", "Abdomen_2"]
    for fwd, bone in ((-0.6, "Abdomen_1"), (-0.95, "Abdomen_1"), (-1.3, "Abdomen_2"), (-1.65, "Abdomen_2"), (-1.95, "Abdomen_2")):
        for sx in (-1, 1):
            base = G(0.3 * sx, sac_top(0.3, fwd) - 0.04, fwd)
            size = 1.0 - 0.25 * abs(fwd + 1.1)
            b.tube("SacHook", [base, base + G(0.05 * sx, 0.24 * size, -0.07), base + G(0.1 * sx, 0.3 * size, -0.26 * size)],
                   [0.07 * size, 0.04 * size, 0.004], sides=10, chain=[bone], style=H.hook(PAL, k, glow=0.8, glow_from=0.75))
    b.tube("Tail", [G(0, 0.5, -2.25), G(0, 0.38, -2.5), G(0, 0.45, -2.7)], [0.13, 0.06, 0.006], sides=12,
           chain=["Abdomen_2"], style=H.hook(PAL, k, glow=1.0))

    for (side, i), lg in legs().items():
        root, c, kn, f, yaw = lg.joints(lg.root, lg.foot)
        tip = f + (f - kn).normalized() * 0.04
        b.tube("Coxa", [root, c], [0.17, 0.15], sides=14, chain=[lg.names[0]], style=H.leg(LEG, k, "Coxa"))
        b.tube("Femur", [c, c.lerp(kn, 0.5) + Vector((0, 0, 0.06)), kn], [0.16, 0.19, 0.12], sides=14,
               chain=[lg.names[1]], style=H.leg(LEG, k, "Femur", spines=5))
        kink = kn.lerp(f, 0.42) + yaw * 0.12 + Vector((0, 0, 0.04))
        b.tube("Tibia", [kn, kink, tip], [0.12, 0.09, 0.008], sides=14, chain=[lg.names[2]],
               style=H.leg(LEG, k, "Tibia", spines=3))
        b.tube("Spur", [kink, kink - yaw * 0.06 + Vector((0, 0, 0.22))], [0.04, 0.003], sides=8, chain=[lg.names[2]],
               style=H.hook(LEG, k))
        # A hooked Crysium spur at each knee repeats the crown down the legs.
        b.shard("KneeCrysium", kn - (kn - c).normalized() * 0.04, Vector((0, 0, 1)) + (kn - c).normalized() * 0.6,
                0.05, 0.34, sides=8, twist=2.0, flute=0.4, bone=lg.names[1], style=Y.shard_style("pink", 0.9, seed=20 + i))
    return b


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.4), None, True),
        ("Thorax", G(0, 0.95, -0.35), G(0, 1.05, 0.3), "Root", True),
        ("Chest", G(0, 1.15, 0.2), G(0, 1.9, 0.55), "Thorax", True),
        ("Head", G(0, 1.9, 0.55), G(0, 1.85, 1.0), "Chest", True),
        ("Abdomen_1", G(0, 0.95, -0.35), G(0, 0.85, -1.2), "Thorax", True),
        ("Abdomen_2", G(0, 0.85, -1.2), G(0, 0.45, -2.4), "Abdomen_1", True),
    ]
    for name in PETALS:
        pts = petal_points(name)
        out.append((f"Petal_{name}", pts[0], pts[-1], "Head", True))
    for side, sx in SIDES:
        shoulder, elbow, blade = arm_points(sx)
        out.append((f"Arm_{side}_1", shoulder, elbow, "Chest", True))
        out.append((f"Arm_{side}_2", elbow, blade[-1], f"Arm_{side}_1", True))
    for lg in legs().values():
        out += lg.rest_bones("Thorax")
    return out


X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def body(bob=0.0, roll=0.0, sway=0.0, thorax_pitch=0.0, chest=0.0, head_pitch=0.0, head_yaw=0.0, petals=0.0,
         arm=0.0, blade=0.0, spread=0.0, belly=0.0, offset=None):
    """chest < 0 rears the forebody back; petals: how far the four jaws open (rad); arm raises the scythes, blade
    swings the scythe blade, spread swings both arms outward."""
    specs = {
        "Thorax": {"rot": [(Yv, roll), (X, thorax_pitch)], "loc": offset if offset is not None else Vector((0, 0, bob))},
        "Chest": {"rot": [(X, chest), (Z, head_yaw * 0.4)]},
        "Head": {"rot": [(X, head_pitch), (Z, head_yaw)]},
        "Abdomen_1": {"rot": [(Z, sway), (X, belly - thorax_pitch * 0.5)]},
        "Abdomen_2": {"rot": [(Z, sway * 0.6), (X, belly * 0.6)]},
    }
    for name in PETALS:
        sx = -1 if name[1] == "L" else 1
        up = -1 if name[0] == "U" else 1
        specs[f"Petal_{name}"] = {"rot": [(X, up * petals * 0.85), (Z, sx * petals * 0.7)]}
    for side, sx in SIDES:
        specs[f"Arm_{side}_1"] = {"rot": [(X, -arm), (Z, sx * spread)]}
        specs[f"Arm_{side}_2"] = {"rot": [(X, blade)]}
    return specs


def place_legs(poser, specs, feet):
    pose = poser.solve(specs)
    thorax = pose["Thorax"] @ poser.rest["Thorax"].inverted()
    for key, lg in legs().items():
        specs.update(lg.pose(poser, thorax @ lg.root, feet[key]))
    return specs


def walk(poser):
    stride, lift, swing = 0.6, 0.32, 0.25

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body(bob=0.035 * math.cos(4 * w), roll=math.radians(3) * math.sin(w), sway=math.radians(7) * math.sin(w + 0.8),
                     chest=math.radians(3) * math.sin(2 * w), head_yaw=math.radians(5) * math.sin(w),
                     head_pitch=math.radians(3) * math.sin(2 * w + 1), petals=math.radians(5) * (0.5 + 0.5 * math.sin(2 * w)),
                     arm=math.radians(5) * math.sin(w), blade=math.radians(6) * math.sin(w + 1),
                     belly=math.radians(2.5) * math.sin(4 * w + 0.5))
        feet = {}
        for key, lg in legs().items():
            phase = (t + STEP_PHASE[key]) % 1.0
            if phase < 1 - swing:  # stance: the planted foot slides back under the body
                s = phase / (1 - swing)
                fwd, up = stride * (0.5 - s), 0.0
            else:
                s = (phase - (1 - swing)) / swing
                fwd = stride * (-0.5 + (1 - math.cos(math.pi * s)) * 0.5)
                up = lift * math.sin(math.pi * s)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet)
    return poser.action("Walk", 40, spec, loop=True)


def idle(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        breathe = 0.5 + 0.5 * math.sin(2 * w)
        specs = body(bob=0.02 * math.sin(w), roll=math.radians(1.5) * math.sin(w), sway=math.radians(4) * math.sin(w + 1),
                     belly=math.radians(3) * math.sin(2 * w), chest=math.radians(3) * math.sin(w + 0.6),
                     head_yaw=math.radians(12) * math.sin(w) * math.sin(0.5 * w + 0.3),
                     head_pitch=math.radians(4) * math.sin(2 * w + 0.4), petals=math.radians(14) * breathe ** 2,
                     arm=math.radians(7) * math.sin(w + 0.5), blade=math.radians(10) * math.sin(2 * w),
                     spread=math.radians(4) * math.sin(w))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return poser.action("Idle", 80, spec, loop=True)


def scream(poser):
    def spec(t, f):
        rise = C.smoothstep(0.0, 0.25, np.array([t]))[0] * (1 - C.smoothstep(0.8, 1.0, np.array([t]))[0])
        shake = math.sin(t * 90) * C.smoothstep(0.25, 0.35, np.array([t]))[0] * (1 - C.smoothstep(0.7, 0.8, np.array([t]))[0])
        specs = body(offset=Vector((0, 0.06 * rise, 0.1 * rise)), thorax_pitch=-math.radians(8) * rise,
                     chest=-math.radians(22) * rise, head_pitch=-math.radians(18) * rise + math.radians(2) * shake,
                     head_yaw=math.radians(3) * shake, petals=math.radians(48) * rise, arm=math.radians(50) * rise,
                     blade=-math.radians(55) * rise, spread=math.radians(14) * rise, belly=math.radians(8) * rise,
                     sway=math.radians(2) * shake)
        feet = {}
        for key, lg in legs().items():
            foot = lg.foot.copy()
            if key[1] == 1:  # front legs lift and stamp
                foot += Vector((0, -0.12, 0.4)) * rise
            feet[key] = foot
        return place_legs(poser, specs, feet)
    return poser.action("Scream", 50, spec)


def set_action(arm, act, frame):
    arm.animation_data.action = act
    bpy.context.scene.frame_set(frame)


def previews(arm, mesh, actions, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=3.5)
    C.ground()
    target = Vector((0, -0.1, 1.0))
    paths = []
    close = C.closeup_camera(target, 9.0, azimuth=-38, elevation=24, lens=55)
    set_action(arm, actions["Idle"], 20)
    paths.append(C.render(close, os.path.join(out, "queen_close.png"), 1280, 720))
    side = C.closeup_camera(target + Vector((0, 0.5, 0)), 10.0, azimuth=-90, elevation=8, lens=55)
    set_action(arm, actions["Walk"], 0)
    paths.append(C.render(side, os.path.join(out, "queen_side.png"), 1280, 720))
    front = C.closeup_camera(target + Vector((0, 0, 0.5)), 7.5, azimuth=-12, elevation=14, lens=55)
    set_action(arm, actions["Scream"], 22)
    paths.append(C.render(front, os.path.join(out, "queen_scream.png"), 1280, 720))
    top = C.closeup_camera(target, 9.5, azimuth=-160, elevation=58, lens=55)
    set_action(arm, actions["Idle"], 0)
    paths.append(C.render(top, os.path.join(out, "queen_top.png"), 1280, 720))
    strip = []
    for i, frame in enumerate(range(0, 40, 5)):
        set_action(arm, actions["Walk"], frame)
        strip.append(C.render(side, os.path.join(out, f"queen_walk_{i}.png"), 640, 360, samples=24))
    C.contact_sheet(strip, os.path.join(out, "queen_walk_strip.png"), 4, 480)
    # Macro close-ups (round 3): egg sac membrane and armour, the head and petals, a leg; plus the sac with the
    # baked maps only (no tiling detail normals).
    set_action(arm, actions["Idle"], 20)
    macros = [("queen_macro_sac.png", G(-0.6, 0.75, -1.2), 1.9, -115, 22),
              ("queen_macro_head.png", G(0.12, 1.95, 0.85), 1.1, -30, 12),
              ("queen_macro_leg.png", G(-1.25, 0.75, 0.8), 1.2, -60, 12)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))
    C.set_detail_preview(asset, False)
    m = macros[0]
    paths.append(C.render(C.macro_camera(m[1], m[2], m[3], m[4]), os.path.join(out, "queen_macro_sac_bakeonly.png"), 1280, 720))
    C.set_detail_preview(asset, True)

    set_action(arm, actions["Walk"], 10)
    # In-game the boss is drawn at enemy scale 2.7 times the factory's boss factor 0.6.
    paths.append(C.game_view(arm, mesh, os.path.join(out, "queen_game.png"), 1.62, 1, (0, 0), close_lights=rig))
    paths.append(C.game_view(arm, mesh, os.path.join(out, "queen_game_zoomed.png"), 1.62, 1, (0, 0), zoom=1.5,
                             close_lights=rig))
    C.contact_sheet(paths + [os.path.join(out, "queen_walk_strip.png")], os.path.join(out, "sheet_queen.png"), 2, 640)
    C.compare_sheet("queen", ["queen_close.png", "queen_side.png", "queen_scream.png", "queen_top.png",
                              "queen_game_zoomed.png", "queen_game.png"], out)
    C.uv_layout(mesh, os.path.join(out, "queen_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.012, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.35,
                               extrusion=0.05, ray=0.15, emission_strength=2.5,
                               extra=H.weathering(K, markings=PAL.dark, marking_amount=0.6), specular=0.3,
                               mirror=True, k=DETAIL_K, families=FAMILIES, low_size=LOW_TEX)
    arm = C.build_armature(NAME + "_Rig", bones())
    C.skin(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = {"Walk": walk(poser), "Idle": idle(poser), "Scream": scream(poser)}
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats,
                          "detail": asset.details, "actions": list(actions)})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, out, asset)


main()
