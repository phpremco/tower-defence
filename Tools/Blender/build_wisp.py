"""Wisp: the energy creature that walks the road and can land (round 5, see ArtSource/DESIGN_NOTES.md).

Very little mesh, a lot of light: a bud of four dark chitin petals closed around a white-hot heart (light leaks
out of the seams and the crown slit), two gyroscope rings around it (an inner ring of linked vertebra beads with
glowing joints, an outer band with six outward hooks and three glowing nodes) and three hooked tendrils with
glowing tips that trail under it in flight and plant as a tripod when it lands.

The armature origin is on the ground: Float holds the bud about 0.42 above it, Land brings it down onto the
tendrils (bud 0.24 high) and Grounded idles there. Clips: Float (loop, rings spinning), Land, Grounded (loop),
Hit, Death (petals burst open, rings drop). Also exports Wisp_LOD1.

Run: blender --background --python Tools/Blender/build_wisp.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "Wisp"
LOD_NAME = "Wisp_LOD1"
K = 0.8
GAME_TRIS = 2400
LOD_TRIS = 1000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 0.5

# Species colours from ProjectGenerator.Bestiary: pale ice-blue light (0.7, 0.85, 1), white eyes. The organism
# inside is dark umber-violet flesh with pale rims (warm and organic around the cool light).
PAL = H.Palette(top=(0.36, 0.24, 0.3), side=(0.17, 0.1, 0.14), belly=(0.06, 0.04, 0.05), seam=(0.03, 0.02, 0.025),
                dark=(0.1, 0.06, 0.08), glow=(0.5, 0.74, 1.0), eye=(0.9, 0.97, 1.0), bone=(0.82, 0.76, 0.74))
RING = H.Palette(top=(0.32, 0.3, 0.38), side=(0.14, 0.13, 0.18), belly=(0.05, 0.05, 0.07), seam=(0.02, 0.02, 0.03),
                 dark=(0.08, 0.08, 0.1), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

FLOAT_UP, GROUND_UP = 0.42, 0.24
CENTRE = G(0, FLOAT_UP, 0)
BUD_R = 0.155
PETALS = [math.radians(a) for a in (45, 135, 225, 315)]   # azimuth, 0 = forward, positive towards +x
PETAL_HW = math.radians(40)
RING_A = dict(radius=0.27, tube=0.019, normal=G(0, math.cos(math.radians(20)), math.sin(math.radians(20))))
RING_B = dict(radius=0.35, tube=0.017, normal=G(0, math.sin(math.radians(30)), math.cos(math.radians(30))))
TENDRILS = [math.radians(a) for a in (0, 120, 240)]
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def flat_dir(az):
    return G(math.sin(az), 0, math.cos(az))


def bud_point(az, elev, r, centre=None):
    c = centre if centre is not None else CENTRE
    return c + flat_dir(az) * (r * math.cos(elev)) + Z * (r * math.sin(elev))


PETAL_ELEV = (-1.15, 1.25)


def petal_taper(elev):
    """Pointed petal tips: the petal narrows towards the crown, so the crown slit opens between them."""
    v = (np.asarray(elev) - PETAL_ELEV[0]) / (PETAL_ELEV[1] - PETAL_ELEV[0])
    return 1 - 0.4 * C.smoothstep(0.7, 1.0, v)


def petal_surface(az0, r):
    def fn(u, v):
        elev = PETAL_ELEV[0] + (PETAL_ELEV[1] - PETAL_ELEV[0]) * v
        hw = PETAL_HW * float(petal_taper(elev))
        return bud_point(az0 + hw * (2 * u - 1), elev, r * (1 + 0.06 * math.sin(math.pi * v)))
    return fn


def tendril_limbs():
    out = []
    for i, az in enumerate(TENDRILS):
        d = flat_dir(az)
        root = bud_point(az, -1.05, BUD_R * 0.85)
        foot = root + d * 0.09 - Z * 0.29
        names = [f"Tendril_{i + 1}_1", f"Tendril_{i + 1}_2"]
        out.append(C.Limb(names, root, foot, (0.17, 0.17), d * 1.0 + Z * 0.3))
    return out


def plant_point(i, up=0.0):
    return G(0, up, 0) + flat_dir(TENDRILS[i]) * 0.2


def build_parts():
    b = C.PartBuilder()
    k = K
    # The heart is living tissue, not a light bulb: glowing cells in dark flesh (a membrane sac without armour).
    b.ellipsoid("Heart", CENTRE, (0.128, 0.128, 0.135), bone="Core", segments=(32, 16),
                style=H.membrane_sac(PAL, k, plate_line=2.0, cells=26, cell_size=0.16, glow=1.0, seed=17, plate_glow=0.0))
    for i, az in enumerate(PETALS):
        b.slab("Petal", petal_surface(az, BUD_R), petal_surface(az, BUD_R * 0.8), 10, 18, chain=[f"Petal_{i + 1}"],
               style=H.bud_petal(PAL, k, az, PETAL_HW, glow=0.9, taper=petal_taper))
        # The style measures the petal from the bud's centre: give the part a frame there.
        b.parts[-1].frame = (C.Matrix.Translation(CENTRE) @ C.Matrix.Diagonal((BUD_R, BUD_R, BUD_R, 1.0))).inverted()
        b.parts[-1].radii = (BUD_R, BUD_R, BUD_R)
        # Each petal tip ends in a hook that curls out over the crown: the bud reads as a closed claw.
        tip = bud_point(az, PETAL_ELEV[1] - 0.08, BUD_R * 1.02)
        out = flat_dir(az)
        b.tube("PetalHook", [tip, tip + out * 0.03 + Z * 0.035, tip + out * 0.075 + Z * 0.03], [0.018, 0.01, 0.001],
               sides=7, chain=[f"Petal_{i + 1}"], style=H.hook(PAL, k, glow=0.6, glow_from=0.8))

    ref = Vector((1, 0, 0))
    b.ring("RingA", CENTRE, RING_A["normal"], RING_A["radius"], RING_A["tube"], arcs=3, sides=8, chain=["Ring_A"],
           profile=lambda th: 0.75 + 0.45 * abs(math.sin(6 * th)), ref=ref,
           style=H.ring_band(RING, k, CENTRE, RING_A["normal"], beads=12, joint_glow=0.9, inner_glow=0.5, ref=ref))
    b.ring("RingB", CENTRE, RING_B["normal"], RING_B["radius"], RING_B["tube"], arcs=3, sides=8, chain=["Ring_B"],
           ref=ref, style=H.ring_band(RING, k, CENTRE, RING_B["normal"], nodes=3, node_glow=1.0, inner_glow=0.7,
                                       ref=ref, phase=math.pi / 2))
    n = RING_B["normal"].normalized()
    a = (ref - n * n.dot(ref)).normalized()
    bb = n.cross(a)
    for j in range(6):
        th = math.radians(30 + 60 * j)
        radial = a * math.cos(th) + bb * math.sin(th)
        tangent = n.cross(radial)
        base = CENTRE + radial * (RING_B["radius"] + 0.005)
        b.tube("Hook", [base, base + radial * 0.04 + tangent * 0.01, base + radial * 0.07 + tangent * 0.035],
               [0.016, 0.009, 0.001], sides=7, chain=["Ring_B"], style=H.hook(RING, k, glow=0.9, glow_from=0.7))
    for limb in tendril_limbs():
        root, knee, foot = limb.rest_points
        b.tube("Tendril", [root, root.lerp(knee, 0.5), knee], [0.03, 0.024, 0.02], sides=8, chain=[limb.names[0]],
               style=H.tube(PAL, k, PAL.top, PAL.side, rings=(0.3, 0.6, 0.9), rough=0.55))
        b.tube("Tendril", [knee, knee.lerp(foot, 0.5), foot], [0.02, 0.015, 0.008], sides=8, chain=[limb.names[1]],
               style=H.tube(PAL, k, PAL.side, PAL.dark, rings=(0.3, 0.6), tip_glow=1.0, glow_from=0.6, rough=0.55))
        d = (foot - knee).normalized()
        out = (foot - Vector((CENTRE.x, CENTRE.y, foot.z))).normalized()
        b.tube("Barb", [foot, foot + d * 0.02 + out * 0.02, foot + out * 0.045 - Z * 0.005], [0.01, 0.006, 0.001],
               sides=6, chain=[limb.names[1]], style=H.hook(PAL, k, glow=1.0, glow_from=0.3))
    return b


def petal_bone(i):
    az = PETALS[i]
    return bud_point(az, -0.8, BUD_R * 0.9), bud_point(az, 0.9, BUD_R * 0.9)


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Core", G(0, FLOAT_UP - 0.1, 0), G(0, FLOAT_UP + 0.1, 0), "Root", True),
    ]
    for i in range(4):
        head, tail = petal_bone(i)
        out.append((f"Petal_{i + 1}", head, tail, "Core", True))
    out.append(("Ring_A", CENTRE, CENTRE + RING_A["normal"] * 0.15, "Core", True))
    out.append(("Ring_B", CENTRE, CENTRE + RING_B["normal"] * 0.15, "Core", True))
    for limb in tendril_limbs():
        out += limb.rest_bones("Core")
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def core_specs(lift=0.0, sway=(0.0, 0.0), yaw=0.0, petals=0.0, petal_wave=0.0, ring_a=(0.0, 0.0, None),
               ring_b=(0.0, 0.0, None), w=0.0):
    """lift: bud height change; petals: how far the petals open (radians), petal_wave: each petal a little out of
    step; ring_*: (spin about its own axis, tilt about X, armature-space drop or None)."""
    specs = {
        "Root": {"rot": []},
        "Core": {"rot": [(Z, yaw), (X, sway[0]), (Yv, sway[1])], "loc": Vector((0, 0, lift))},
    }
    for i, az in enumerate(PETALS):
        axis = Z.cross(flat_dir(az)).normalized()
        specs[f"Petal_{i + 1}"] = {"rot": [(axis, petals + petal_wave * math.sin(w + i * 1.7))]}
    for name, ring, (spin, tilt, drop) in (("Ring_A", RING_A, ring_a), ("Ring_B", RING_B, ring_b)):
        spec = {"rot": [(X, tilt), (ring["normal"], spin)]}
        if drop is not None:
            spec["loc"] = drop
        specs[name] = spec
    return specs


def place_tendrils(poser, specs, feet):
    pose = poser.solve(specs)
    move = pose["Core"] @ poser.rest["Core"].inverted()
    for limb, foot in zip(tendril_limbs(), feet):
        specs.update(limb.pose(poser, move @ limb.root, foot, move.to_3x3() @ limb.pole))
    return specs


def hanging_feet(poser, specs, w, amount=0.03):
    """Trailing tendrils: they hang from the moving bud and sway a little, each out of step."""
    pose = poser.solve(specs)
    move = pose["Core"] @ poser.rest["Core"].inverted()
    feet = []
    for i, limb in enumerate(tendril_limbs()):
        d = flat_dir(TENDRILS[i])
        feet.append(move @ limb.foot + d * (amount * math.sin(w + i * 2.1)) + Z * (0.02 * math.sin(2 * w + i)))
    return feet


# The rings lie flat around the bud on the ground: tilt each ring's axis up to vertical.
FLAT_A = -math.radians(20)
FLAT_B = -math.radians(60)


def float_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = core_specs(lift=0.03 * math.sin(w), sway=(d(4) * math.sin(w + 0.4), d(3) * math.cos(w)),
                           petals=d(5) + d(3) * math.sin(2 * w), petal_wave=d(3), w=2 * w,
                           ring_a=(2 * math.pi * t, d(8) * math.sin(w), None),
                           ring_b=(-2 * math.pi * t, d(10) * math.sin(w + 1.3), None))
        return place_tendrils(poser, specs, hanging_feet(poser, specs, w))
    return 60, spec, True


LAND_FRAMES = 24


def land_spec(poser):
    d = math.radians

    def spec(t, f):
        s = lambda a, b: C.smoothstep(a, b, np.array([t]))[0]  # noqa: E731
        down = s(0.15, 0.75)
        squash = math.sin(math.pi * s(0.6, 0.95)) * 0.025
        reach = s(0.0, 0.6)
        flat = s(0.2, 0.9)
        spin = s(0.0, 1.0)
        specs = core_specs(lift=-(FLOAT_UP - GROUND_UP) * down - squash, petals=d(5) - d(5) * reach + d(10) * s(0.75, 1.0),
                           ring_a=(2 * math.pi * spin, FLAT_A * flat, None),
                           ring_b=(-2 * math.pi * spin, FLAT_B * flat, None))
        hang = hanging_feet(poser, specs, 0.0, 0.0)
        feet = [h.lerp(plant_point(i), reach) for i, h in enumerate(hang)]
        return place_tendrils(poser, specs, feet)
    return LAND_FRAMES, spec, False


def grounded_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = core_specs(lift=-(FLOAT_UP - GROUND_UP) + 0.012 * math.sin(2 * w), sway=(d(3) * math.sin(w), d(2) * math.cos(w)),
                           yaw=d(6) * math.sin(w), petals=d(10) + d(4) * math.sin(2 * w), petal_wave=d(2), w=2 * w,
                           ring_a=(2 * math.pi * t, FLAT_A + d(3) * math.sin(w), None),
                           ring_b=(-2 * math.pi * t, FLAT_B + d(4) * math.sin(w + 1), None))
        return place_tendrils(poser, specs, [plant_point(i) for i in range(3)])
    return 60, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        specs = core_specs(lift=0.03 * k, sway=(-d(14) * k, d(8) * k), petals=d(5) + d(28) * max(k, 0.0),
                           ring_a=(d(25) * k, d(15) * k, None), ring_b=(-d(30) * k, -d(12) * k, None))
        return place_tendrils(poser, specs, hanging_feet(poser, specs, k * 3, 0.05))
    return 12, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        s = lambda a, b: C.smoothstep(a, b, np.array([t]))[0]  # noqa: E731
        swell = math.sin(math.pi * s(0.0, 0.35))
        burst = s(0.25, 0.45)
        fall = s(0.35, 0.9)
        specs = core_specs(lift=0.04 * swell - 0.3 * fall, sway=(d(25) * fall, -d(15) * fall),
                           petals=d(5) + d(12) * swell + d(80) * burst,
                           ring_a=(d(160) * s(0.0, 0.6), d(55) * fall, Vector((0.02, 0, -0.32 * fall))),
                           ring_b=(-d(200) * s(0.0, 0.6), -d(40) * fall, Vector((-0.03, 0.02, -0.28 * fall))))
        pose = poser.solve(specs)
        move = pose["Core"] @ poser.rest["Core"].inverted()
        feet = []
        for i, limb in enumerate(tendril_limbs()):
            # The tendrils splay out and go limp on the ground.
            feet.append((move @ limb.foot).lerp(plant_point(i) * 1.6, fall))
        return place_tendrils(poser, specs, feet)
    return 30, spec, False


CLIPS = {"Float": float_spec, "Land": land_spec, "Grounded": grounded_spec, "Hit": hit_spec, "Death": death_spec}
# LOD1: petals fold into the core, each tendril one bone aimed from the root to the tip. 7 bones.
LOD_DROP = {**{f"Petal_{i}": "Core" for i in (1, 2, 3, 4)}, **{f"Tendril_{i}_2": f"Tendril_{i}_1" for i in (1, 2, 3)}}
LOD_AIMS = {f"Tendril_{i}_1": (f"Tendril_{i}_1", f"Tendril_{i}_2") for i in (1, 2, 3)}


def old_proxy():
    """EnemyModelFactory.Wisp in primitives (factory units x 1.4), flat colours: a glowing ball and two rings."""
    f = 1.4
    core, shell, glow, dark = (1.0, 1.0, 1.0), (0.7, 0.85, 1.0), (0.75, 0.9, 1.0), (0.3, 0.35, 0.45)
    groups = {c: C.PartBuilder() for c in (core, shell, glow, dark)}
    c = G(0, 0.45, 0)
    groups[shell].ellipsoid("Shell", c, (0.19 * f,) * 3)
    for i, (radius, color) in enumerate(((0.24, dark), (0.31, glow))):
        groups[color].ring("Ring", c, G(0, math.cos(0.4 + i * 0.5), math.sin(0.4 + i * 0.5)), radius * f, 0.024 * f, arcs=2)
    for i in range(4):
        a = i * math.pi / 2 + 0.4
        root = c + G(math.cos(a) * 0.12 * f, -0.12 * f, math.sin(a) * 0.12 * f)
        groups[glow].tube("Tendril", [root, root + G(0, -0.26 * f, 0)], [0.035 * f, 0.002])
    return C.flat_proxy(groups, "OldWisp")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=0.9)
    C.ground()
    target = Vector((0, 0, 0.36))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 1.9, azimuth=-38, elevation=24, lens=55)
    C.set_action(arm, actions["Float"], 10)
    paths.append(C.render(close, os.path.join(out, "wisp_close.png"), 1280, 720))
    top = C.closeup_camera(target, 2.0, azimuth=-12, elevation=58, lens=55)
    C.set_action(arm, actions["Float"], 30)
    paths.append(C.render(top, os.path.join(out, "wisp_top.png"), 1280, 720))
    side = C.closeup_camera(target - Vector((0, 0, 0.08)), 1.9, azimuth=-80, elevation=10, lens=55)
    C.set_action(arm, actions["Grounded"], 15)
    paths.append(C.render(side, os.path.join(out, "wisp_grounded.png"), 1280, 720))
    C.set_action(arm, actions["Grounded"], 15)
    paths.append(C.render(close, os.path.join(out, "wisp_grounded_close.png"), 1280, 720))
    C.set_action(arm, actions["Hit"], 4)
    paths.append(C.render(close, os.path.join(out, "wisp_hit.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 18)
    paths.append(C.render(close, os.path.join(out, "wisp_death.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Float"], close, out, "wisp_float", range(0, 60, 8)),
              C.strip_sheet(arm, actions["Land"], side, out, "wisp_land", range(0, LAND_FRAMES + 1, 3)[:8]),
              C.strip_sheet(arm, actions["Death"], close, out, "wisp_deathstrip", range(0, 31, 4))]

    C.set_action(lod_arm, lod_actions["Float"], 10)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "wisp_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Float"], 10)
    macros = [("wisp_macro_bud.png", CENTRE + G(0, 0.04, 0.1), 0.42, -35, 25),
              ("wisp_macro_ring.png", CENTRE + G(-0.3, 0.1, 0.05), 0.35, -60, 20)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    fl, gr = actions["Float"], actions["Grounded"]
    layout = [(fl, 0, (-1.0, 0.5, 0), 0.2), (fl, 15, (-0.3, 0.7, 0), 0.0), (fl, 33, (0.5, 0.4, 0), 0.4),
              (gr, 10, (1.2, 0.6, 0), 0.1), (fl, 48, (-0.7, -0.3, 0), -0.3), (actions["Land"], 14, (0.3, -0.4, 0), 0.0),
              (gr, 40, (1.0, -0.2, 0), 0.6)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "wisp_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "wisp_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "wisp_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "wisp_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Float"], 10)
    single = C.game_view(arm, mesh, os.path.join(out, "wisp_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare5_wisp.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_wisp.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "wisp_uv.png"), background=asset.textures["albedo"])


# Petals, rings and their hooks are hard parts: they follow one bone and meet the heart in a soft crease.
RIGID = {"Petal", "PetalHook", "RingA", "RingB", "Hook"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0024, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.06,
                               extrusion=0.01, ray=0.03, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.4, dust=0.2, seed=91), specular=0.35,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Float"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "pivot": "ground", "float_height": FLOAT_UP, "grounded_height": GROUND_UP,
                          "ring_spin_degrees_per_second": round(360 / 2.0),
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)
    # Round 46: the close-up detail (seam eyes, petal teeth, tendril talons) as extra parts, exported again with the body.
    import wisp_detail
    wisp_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
