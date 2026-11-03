"""Preview renders for Mercer (build_mercer.py): game camera views (with Skitters for scale and colour contrast),
a full-body close-up, a portrait bust, the back, material macros, clip strips and a compare sheet against a stand-in
of the old procedural hero. Weapons follow their socket bones in every render."""

import math
import os

import bpy
from mathutils import Matrix, Vector

import common as C
from common import G

SOCKET_OF = {"hand_pistol": ("Weapon_R", ["Pistol", "Pistol_Mag"]), "hand_sword": ("Weapon_R", ["Sword_Hilt", "Sword_Blade"]),
             "holster_pistol": ("Holster_R", ["Pistol", "Pistol_Mag"]), "back_sword": ("Back", ["Sword_Hilt"])}
MODES = {"pistol": ("hand_pistol", "back_sword"), "sword": ("hand_sword", "holster_pistol"), "none": ("holster_pistol", "back_sword")}
WEAPON_OF = {"Shoot": "pistol", "Reload": "pistol", "Slash": "sword", "Repair": "none"}


class Rig:
    """One rendered hero: an armature, its mesh and its own copies of the weapons."""

    def __init__(self, arm, mesh, weapons, copy=False):
        if copy:
            arm = C.link(arm.copy())
            m = C.link(mesh.copy())
            m.parent = arm
            m.modifiers["Armature"].object = arm
            mesh = m
            weapons = {n: C.link(o.copy()) for n, o in weapons.items()}
        self.arm, self.mesh, self.weapons, self.copied = arm, mesh, weapons, copy

    def place(self, mode):
        bpy.context.view_layer.update()
        for o in self.weapons.values():
            o.hide_render = True
        for slot in MODES[mode]:
            bone, names = SOCKET_OF[slot]
            m = self.arm.matrix_world @ self.arm.pose.bones[bone].matrix
            for n in names:
                o = self.weapons[n]
                o.matrix_world = m
                o.hide_render = False

    def hide(self, hidden=True):
        self.arm.hide_render = self.mesh.hide_render = hidden
        for o in self.weapons.values():
            o.hide_render = True if hidden else o.hide_render

    def remove(self):
        if self.copied:
            for o in [self.mesh, *self.weapons.values(), self.arm]:
                bpy.data.objects.remove(o)


def pose(rig, act, frame, mode=None):
    C.set_action(rig.arm, act, frame)
    rig.place(mode or WEAPON_OF.get(act.name, "none"))


def nla_rig(base_rig, act, frame, pos, yaw, scale=1.0, at=1000):
    r = Rig(base_rig.arm, base_rig.mesh, base_rig.weapons, copy=True)
    r.arm.animation_data.action = None
    track = r.arm.animation_data.nla_tracks.new()
    strip = track.strips.new(act.name + "_strip", int(at - frame), act)
    strip.extrapolation = "HOLD"
    if hasattr(strip, "action_slot") and len(getattr(act, "slots", ())):
        strip.action_slot = act.slots[0]
    r.arm.location = Vector(pos)
    r.arm.rotation_euler.z = yaw
    r.arm.scale = (scale, scale, scale)
    r.act = act
    return r


def skitters():
    """Appends the Skitter from its .blend (for scale and colour contrast in the game views); None if missing."""
    path = os.path.join(C.BLEND, "Skitter.blend")
    if not os.path.exists(path):
        return None
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in ("Skitter", "Skitter_Rig")]
        dst.actions = [n for n in src.actions if n in ("Walk",)]
    objs = {o.name.split(".")[0]: o for o in dst.objects if o is not None}
    if "Skitter" not in objs or "Skitter_Rig" not in objs:
        return None
    for o in objs.values():
        C.link(o)
        o.hide_render = True
    walk = next((a for a in dst.actions if a is not None), None)
    return objs["Skitter_Rig"], objs["Skitter"], walk


def game_scene(base, layout, path, zoom, skitter=None, close_lights=(), width=640, height=360, target=(0, 0, 0.5)):
    scene = bpy.context.scene
    rigs = [nla_rig(base, act, frame, pos, yaw) for act, frame, pos, yaw in layout]
    extra = []
    if skitter:
        s_arm, s_mesh, s_walk = skitter
        for i, (pos, yaw) in enumerate(((Vector((1.5, -1.2, 0)), 2.6), (Vector((2.1, -0.6, 0)), 2.9), (Vector((2.5, -1.6, 0)), 2.5))):
            a2 = C.link(s_arm.copy())
            m2 = C.link(s_mesh.copy())
            m2.parent = a2
            m2.modifiers["Armature"].object = a2
            a2.scale = (0.45, 0.45, 0.45)
            a2.location = pos
            a2.rotation_euler.z = yaw
            a2.hide_render = m2.hide_render = False
            if s_walk and a2.animation_data:
                a2.animation_data.action = s_walk
            extra += [m2, a2]
    base.hide(True)
    C.hide_lights(close_lights, True)
    lights = C.game_lights()
    cam = C.game_camera(Vector(target), width / C.GAME_PIXELS_PER_UNIT / zoom)
    keep = scene.frame_current
    scene.frame_set(1000)
    for r in rigs:
        r.place(WEAPON_OF.get(r.act.name, "none"))
    C.render(cam, path, width, height)
    scene.frame_set(keep)
    for o in lights + [cam] + extra:
        bpy.data.objects.remove(o)
    for r in rigs:
        r.remove()
    C.hide_lights(close_lights, False)
    base.hide(False)
    return path


def strip(rig, act, cam, out, prefix, frames, columns=4, cell=480, mode=None):
    paths = []
    for i, f in enumerate(frames):
        pose(rig, act, f, mode)
        paths.append(C.render(cam, os.path.join(out, f"{prefix}_{i}.png"), 640, 360, samples=24))
    path = os.path.join(out, f"{prefix}_strip.png")
    C.contact_sheet(paths, path, columns, cell)
    for p in paths:
        os.remove(p)
    return path


def old_proxy():
    """HeroModelFactory's figure in primitives and flat colours (white plates, steel-blue suit, gunmetal, gold trim,
    cyan visor, short cloak), standing with its arms down: the look the new model replaces."""
    armour, suit, metal, trim, visor = (0.93, 0.94, 0.92), (0.3, 0.38, 0.54), (0.17, 0.19, 0.23), (1.0, 0.85, 0.3), (0.3, 0.95, 1.0)
    g = {c: C.PartBuilder() for c in (armour, suit, metal, trim, visor)}
    s = 0.95
    P = lambda x, y, z: G(x * s, y * s, z * s)  # noqa: E731
    g[suit].ellipsoid("Pelvis", P(0, 0.74, 0), (0.19 * s, 0.13 * s, 0.09 * s))
    g[suit].tube("Waist", [P(0, 0.8, 0), P(0, 1.02, 0.03)], [0.15 * s, 0.2 * s])
    g[armour].ellipsoid("Chest", P(0, 1.1, 0.04), (0.25 * s, 0.17 * s, 0.2 * s))
    g[metal].ellipsoid("Pack", P(0, 1.06, -0.2), (0.16 * s, 0.07 * s, 0.17 * s))
    g[metal].tube("Collar", [P(0, 1.2, 0.04), P(0, 1.3, 0.04)], [0.13 * s, 0.12 * s])
    g[armour].ellipsoid("Helmet", P(0, 1.44, 0.05), (0.145 * s, 0.15 * s, 0.155 * s))
    g[visor].ellipsoid("Visor", P(0, 1.45, 0.16), (0.105 * s, 0.06 * s, 0.03 * s))
    g[metal].ellipsoid("Jaw", P(0, 1.36, 0.12), (0.115 * s, 0.105 * s, 0.065 * s))
    g[trim].ellipsoid("Ridge", P(0, 1.1, 0.18), (0.025 * s, 0.03 * s, 0.14 * s))
    g[suit].tube("Cloak", [P(0, 1.26, -0.12), P(0, 0.85, -0.22), P(0, 0.55, -0.25)], [0.2 * s, 0.17 * s, 0.13 * s], flatten=0.15)
    for sx in (-1, 1):
        g[suit].ellipsoid("Pauldron", P(0.38 * sx, 1.27, 0.03), (0.14 * s, 0.15 * s, 0.1 * s))
        g[trim].ellipsoid("Rim", P(0.38 * sx, 1.3, 0.03), (0.13 * s, 0.13 * s, 0.02 * s))
        g[suit].tube("Arm", [P(0.31 * sx, 1.24, 0.03), P(0.34 * sx, 0.98, 0.06)], [0.075 * s, 0.065 * s])
        g[armour].tube("Forearm", [P(0.34 * sx, 0.98, 0.06), P(0.34 * sx, 0.86, 0.26)], [0.085 * s, 0.075 * s])
        g[metal].ellipsoid("Glove", P(0.34 * sx, 0.84, 0.29), (0.07 * s, 0.055 * s, 0.085 * s))
        g[suit].tube("Thigh", [P(0.14 * sx, 0.72, 0), P(0.14 * sx, 0.37, 0.02)], [0.095 * s, 0.08 * s])
        g[armour].ellipsoid("ThighPlate", P(0.14 * sx, 0.56, 0.05), (0.1 * s, 0.07 * s, 0.15 * s))
        g[armour].tube("Shin", [P(0.14 * sx, 0.37, 0.02), P(0.14 * sx, 0.06, 0.0)], [0.08 * s, 0.065 * s])
        g[metal].ellipsoid("Boot", P(0.14 * sx, 0.04, 0.06), (0.095 * s, 0.065 * s, 0.17 * s))
    return C.flat_proxy(g, "OldHero")


def render_all(arm, asset, lod, actions, weapons, out):
    mesh = asset.low
    C.preview_world()
    rig_lights = C.light_rig(scale=1.7)
    C.ground()
    base = Rig(arm, mesh, weapons)
    lod.hide_render = True
    for o in weapons.values():
        o.hide_render = True
    target = Vector((0, 0, 0.85))
    paths = []

    # Full body 3/4, front and back, at a portrait aspect.
    close = C.closeup_camera(target, 4.2, azimuth=-32, elevation=12, lens=55)
    pose(base, actions["Idle"], 10, "none")
    paths.append(C.render(close, os.path.join(out, "mercer_close.png"), 960, 1200))
    pose(base, actions["Shoot"], 0)
    paths.append(C.render(close, os.path.join(out, "mercer_close_aim.png"), 960, 1200))
    back = C.closeup_camera(target, 4.2, azimuth=150, elevation=18, lens=55)
    pose(base, actions["Idle"], 10, "none")
    paths.append(C.render(back, os.path.join(out, "mercer_back.png"), 960, 1200))
    top = C.closeup_camera(target, 4.4, azimuth=-20, elevation=50, lens=55)
    pose(base, actions["Slash"], 9)
    paths.append(C.render(top, os.path.join(out, "mercer_slash_top.png"), 1280, 960))

    # Portrait bust: head and shoulders, 3/4, a little below eye level, for dialogue portraits.
    bust = C.closeup_camera(Vector((0, -0.01, 1.33)), 1.45, azimuth=-28, elevation=5, lens=70)
    pose(base, actions["Idle"], 10, "none")
    paths.append(C.render(bust, os.path.join(out, "mercer_bust.png"), 1024, 1024))

    # Material macros.
    macros = [("mercer_macro_suit.png", G(0.1, 0.62, 0.04), 0.42, -40, 10),
              ("mercer_macro_plates.png", G(0.18, 1.3, 0.06), 0.42, -30, 25),
              ("mercer_macro_pack.png", G(0.04, 1.16, -0.22), 0.45, 160, 18),
              ("mercer_macro_boots.png", G(0.12, 0.08, 0.1), 0.42, -35, 18)]
    for name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, name), 1280, 720))
    C.set_detail_preview(asset, False)
    paths.append(C.render(C.macro_camera(macros[0][1], macros[0][2], macros[0][3], macros[0][4]),
                          os.path.join(out, "mercer_macro_suit_bakeonly.png"), 1280, 720))
    C.set_detail_preview(asset, True)

    # LOD1 next to LOD0.
    mesh.hide_render = True
    lod.hide_render = False
    pose(base, actions["Idle"], 10, "none")
    paths.append(C.render(close, os.path.join(out, "mercer_lod1_close.png"), 960, 1200))
    lod.hide_render = True
    mesh.hide_render = False

    # The Shockwave's impact from the side: the fist must meet the ground.
    side_close = C.closeup_camera(Vector((0, -0.2, 0.45)), 3.0, azimuth=-80, elevation=8, lens=50)
    pose(base, actions["Shockwave"], 13, "none")
    paths.append(C.render(side_close, os.path.join(out, "mercer_shockwave_impact.png"), 1280, 720))

    # Strips.
    side = C.closeup_camera(target, 4.6, azimuth=-90, elevation=6, lens=50)
    front3 = C.closeup_camera(target + Vector((0, -0.1, 0)), 4.4, azimuth=-40, elevation=10, lens=50)
    strips = [strip(base, actions["Walk"], side, out, "mercer_walk", range(0, 32, 4)),
              strip(base, actions["Run"], side, out, "mercer_run", range(0, 16, 2)),
              strip(base, actions["Shoot"], front3, out, "mercer_shoot", [0, 1, 2, 3, 5, 7, 10, 14]),
              strip(base, actions["Slash"], front3, out, "mercer_slash", [0, 4, 6, 8, 10, 12, 16, 22]),
              strip(base, actions["Reload"], front3, out, "mercer_reload", [0, 6, 11, 16, 20, 23, 28, 35]),
              strip(base, actions["Shockwave"], front3, out, "mercer_shockwave", [0, 5, 9, 11, 13, 17, 23, 30]),
              strip(base, actions["Repair"], front3, out, "mercer_repair", [0, 12, 24, 36], columns=4),
              strip(base, actions["Down"], front3, out, "mercer_down", [0, 6, 12, 18, 22, 26, 32, 40]),
              strip(base, actions["Revive"], front3, out, "mercer_revive", [0, 6, 12, 17, 22, 27, 31, 36]),
              strip(base, actions["Hit"], front3, out, "mercer_hit", [0, 2, 4, 6, 9, 12])]

    # Game camera: the hero idle, running, aiming and slashing among Skitters, at the play zoom, at ~30 px and zoomed.
    sk = skitters()
    layout = [(actions["Idle"], 10, (-1.4, 0.4, 0), 0.4), (actions["Run"], 3, (-0.2, 0.9, 0), -1.2),
              (actions["Shoot"], 0, (0.8, -0.3, 0), -1.9), (actions["Slash"], 10, (0.0, -1.2, 0), -2.3)]
    games = [game_scene(base, layout, os.path.join(out, "mercer_game.png"), 1.0, sk, rig_lights),
             game_scene(base, layout, os.path.join(out, "mercer_game_30px.png"), 0.55, sk, rig_lights),
             game_scene(base, layout, os.path.join(out, "mercer_game_zoomed.png"), 2.6, sk, rig_lights)]

    # Compare with the old procedural figure.
    base.hide(True)
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "mercer_old_close.png"), 960, 1200),
            C.game_view_static(old, os.path.join(out, "mercer_old_game_zoomed.png"), 1.0, zoom=2.6, close_lights=rig_lights,
                               target=(0, 0, 0.5))]
    C.remove_objects(old)
    base.hide(False)
    single = game_scene(base, [(actions["Idle"], 10, (0, 0, 0), 0.0)], os.path.join(out, "mercer_single_zoomed.png"), 2.6, None, rig_lights)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare6_mercer.png"), 2, 640)
    C.contact_sheet(paths + games, os.path.join(out, "sheet_mercer.png"), 3, 560)
    C.uv_layout(mesh, os.path.join(out, "mercer_uv.png"), background=asset.textures["albedo"])
    C.log("previews", paths + strips + games)
    import build_mercer_weapons as W
    C.hide_lights(rig_lights, True)
    base.hide(True)
    W.previews(weapons, out)
