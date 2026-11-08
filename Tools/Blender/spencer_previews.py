"""Before/after previews for Spencer (the hero; build_mercer.py, build_mercer2.py): front, 3/4, back, bust and the
game camera at play zoom and zoomed in, with a Cragback and Skitters beside him for the "holds up next to the
creatures" check.

Run on a saved hero (any version of Mercer.blend):
  blender --background --factory-startup --python Tools/Blender/spencer_previews.py -- --blend <Mercer.blend>
          --out <dir>[;<dir>...] --prefix <name>
"""

import math
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import mercer_previews as MP  # noqa: E402

WEAPONS = ("Pistol", "Pistol_Mag", "Sword_Hilt", "Sword_Blade")
CRAG_SCALE = 0.85
SKITTER_SCALE = 0.45


CREATURE_BLEND = C.BLEND


def append_creature(name, clip):
    """The creature's mesh, rig and one clip from its .blend (hidden; copies are placed per render)."""
    path = os.path.join(CREATURE_BLEND, name + ".blend")
    if not os.path.exists(path):
        return None
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in (name, name + "_Rig")]
        dst.actions = [n for n in src.actions if n == clip]
    objs = {o.name.split(".")[0]: o for o in dst.objects if o is not None}
    if name not in objs or name + "_Rig" not in objs:
        return None
    for o in objs.values():
        C.link(o)
        o.hide_render = True
    act = next((a for a in dst.actions if a is not None), None)
    return objs[name + "_Rig"], objs[name], act


def place_creature(creature, pos, yaw, scale, frame):
    arm, mesh, act = creature
    a2 = C.link(arm.copy())
    m2 = C.link(mesh.copy())
    m2.parent = a2
    m2.modifiers["Armature"].object = a2
    a2.scale = (scale, scale, scale)
    a2.location = Vector(pos)
    a2.rotation_euler.z = yaw
    a2.hide_render = m2.hide_render = False
    if act and a2.animation_data is None:
        a2.animation_data_create()
    if act:
        a2.animation_data.action = None
        track = a2.animation_data.nla_tracks.new()
        strip = track.strips.new(act.name + "_s", int(1000 - frame), act)
        strip.extrapolation = "HOLD"
        if hasattr(strip, "action_slot") and len(getattr(act, "slots", ())):
            strip.action_slot = act.slots[0]
    return [m2, a2]


def game_shot(base, heroes, creatures, path, zoom, close_lights, width, height, target):
    """Game camera (44 degree ortho, like a level): `heroes` = (clip, frame, pos, yaw, weapon mode),
    `creatures` = (creature, pos, yaw, scale, frame)."""
    rigs = []
    for act, frame, pos, yaw, mode in heroes:
        r = MP.nla_rig(base, act, frame, pos, yaw)
        r.mode = mode
        rigs.append(r)
    extra = []
    for creature, pos, yaw, scale, frame in creatures:
        if creature:
            extra += place_creature(creature, pos, yaw, scale, frame)
    base.hide(True)
    C.hide_lights(close_lights, True)
    lights = C.game_lights()
    cam = C.game_camera(Vector(target), width / C.GAME_PIXELS_PER_UNIT / zoom)
    scene = bpy.context.scene
    keep = scene.frame_current
    scene.frame_set(1000)
    for r in rigs:
        r.place(r.mode)
    C.render(cam, path, width, height)
    scene.frame_set(keep)
    for o in lights + [cam] + extra:
        bpy.data.objects.remove(o)
    for r in rigs:
        r.remove()
    C.hide_lights(close_lights, False)
    base.hide(False)
    return path


def render_set(arm, mesh, weapons, actions, outs, prefix):
    out = outs[0]
    os.makedirs(out, exist_ok=True)
    C.preview_world()
    rig_lights = C.light_rig(scale=1.7)
    C.ground()
    base = MP.Rig(arm, mesh, weapons)
    for o in weapons.values():
        o.hide_render = True
    crag = append_creature("Cragback", "Walk")
    skit = append_creature("Skitter", "Walk")
    target = Vector((0, 0, 0.82))
    paths = []
    MP.pose(base, actions["Idle"], 10, "none")
    for name, az, el in (("front", 0, 6), ("34", -35, 12), ("back34", 150, 16)):
        cam = C.closeup_camera(target, 4.2, azimuth=az, elevation=el, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"{prefix}_{name}.png"), 960, 1200))
    bust = C.closeup_camera(Vector((0, -0.01, 1.33)), 1.45, azimuth=-28, elevation=5, lens=70)
    paths.append(C.render(bust, os.path.join(out, f"{prefix}_bust.png"), 1024, 1024))
    MP.pose(base, actions["Shoot"], 0, "pistol")
    cam = C.closeup_camera(target, 4.2, azimuth=-35, elevation=12, lens=55)
    paths.append(C.render(cam, os.path.join(out, f"{prefix}_34_aim.png"), 960, 1200))
    # Game camera at play zoom (about 51 px per unit at 720p) and zoomed in, next to a Cragback and Skitters.
    heroes = [(actions["Idle"], 10, (0.0, 0.0, 0), 0.3, "none"), (actions["Run"], 3, (-1.3, 0.7, 0), -1.2, "none"),
              (actions["Shoot"], 0, (1.0, 0.5, 0), -2.2, "pistol")]
    creatures = [(crag, (1.6, -1.1, 0), 2.6, CRAG_SCALE, 6), (skit, (2.6, -0.4, 0), 2.9, SKITTER_SCALE, 4),
                 (skit, (-0.6, -1.4, 0), 2.4, SKITTER_SCALE, 9)]
    games = [game_shot(base, heroes, creatures, os.path.join(out, f"{prefix}_game.png"), 1.0, rig_lights, 1280, 720, (0.5, -0.3, 0.4)),
             game_shot(base, heroes, creatures, os.path.join(out, f"{prefix}_game_zoom.png"), 3.2, rig_lights, 1280, 720,
                       (0.3, -0.25, 0.5))]
    C.contact_sheet(paths[:3] + paths[4:5], os.path.join(out, f"{prefix}_sheet.png"), 4, 480)
    for extra in outs[1:]:
        os.makedirs(extra, exist_ok=True)
        for p in paths + games + [os.path.join(out, f"{prefix}_sheet.png")]:
            shutil.copy(p, extra)
    return paths + games


def main():
    args = C.cli_args()
    render_file(args["blend"], args["out"].split(";"), args.get("prefix", "spencer"))


def render_file(blend, outs, prefix, creature_blend=None):
    """Opens a saved hero .blend and renders the set (creatures from `creature_blend`, default ArtSource/Blend)."""
    global CREATURE_BLEND
    CREATURE_BLEND = creature_blend or CREATURE_BLEND
    bpy.ops.wm.open_mainfile(filepath=blend)
    arm = bpy.data.objects["Mercer_Rig"]
    mesh = bpy.data.objects["Mercer"]
    for n in ("Mercer_LOD1", "Mercer_High", "MercerWeapons_High"):
        if n in bpy.data.objects:
            bpy.data.objects[n].hide_render = True
    weapons = {n: bpy.data.objects[n] for n in WEAPONS}
    for o in bpy.data.objects:
        if o.type == "MESH" and o.name not in ("Mercer",) and o.name not in weapons:
            o.hide_render = True
    actions = {a.name: a for a in bpy.data.actions}
    return render_set(arm, mesh, weapons, actions, outs, prefix)


if __name__ == "__main__":
    main()
