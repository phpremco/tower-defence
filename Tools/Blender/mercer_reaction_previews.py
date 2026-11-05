"""Round 35 preview strips for Mercer's reaction clips (build_mercer_reactions.py): every clip from a front, eye-level
cutscene camera and from the 44-degree game camera (zoomed and at play zoom), a visor close-up for Disbelief and a
silhouette sheet of the key poses side by side with Idle."""

import os

import bpy
from mathutils import Vector

import common as C
import mercer_previews as MP

STRIP_FRAMES = {
    "Startled": [0, 2, 4, 6, 9, 11, 15, 24],
    "Startled_Hold": [0, 6, 12, 18, 24, 30, 36, 42],
    "Disbelief": [0, 12, 22, 32, 38, 46, 52, 84],
    "Point_Shout": [0, 5, 9, 12, 14, 18, 28, 42],
    "Lower_Weapon": [0, 8, 16, 22, 30, 40, 52, 70],
}
KEY_FRAMES = {"Startled": 24, "Disbelief": 38, "Point_Shout": 9, "Lower_Weapon": 52}
GAME_YAW = -0.55   # turned a little to the figure's right, so the game camera sees him three-quarter
AWAY_YAW = 2.5     # facing up the screen, the usual case when a boss enters at the top of the field


def front_cam():
    return C.closeup_camera(Vector((0, 0, 0.84)), 3.5, azimuth=-14, elevation=2, lens=35)


def game_strip(base, act, frames, out, prefix, zoom, lights, yaw=GAME_YAW):
    paths = [MP.game_scene(base, [(act, f, (0, 0, 0), yaw)], os.path.join(out, f"{prefix}_{i}.png"), zoom,
                           close_lights=lights, target=(0, 0, 0.75))
             for i, f in enumerate(frames)]
    path = os.path.join(out, f"{prefix}_strip.png")
    C.contact_sheet(paths, path, 4, 480)
    for p in paths:
        os.remove(p)
    return path


def render(arm, mesh, lod, actions, out, weapon_modes, events):
    MP.WEAPON_OF.update(weapon_modes)
    weapons = {n: bpy.data.objects[n] for n in ("Pistol", "Pistol_Mag", "Pistol_Mag_Quick", "Sword_Hilt", "Sword_Blade",
                                                "Sword_Hilt_Wide", "Sword_Blade_Wide")}
    for o in bpy.data.objects:
        if o.type == "MESH" and o not in (mesh,) and o.name not in weapons:
            o.hide_render = True
    lod.hide_render = True
    C.preview_world()
    lights = C.light_rig(scale=1.7)
    C.ground()
    base = MP.Rig(arm, mesh, weapons)
    for o in weapons.values():
        o.hide_render = True
    front = front_cam()
    paths = []
    for name, act in actions.items():
        frames = STRIP_FRAMES[name]
        paths.append(MP.strip(base, act, front, out, f"r35_{name}_front", frames, mode=weapon_modes[name]))
        paths.append(game_strip(base, act, frames, out, f"r35_{name}_game", 2.6, lights))
        paths.append(game_strip(base, act, frames, out, f"r35_{name}_game_away", 2.6, lights, AWAY_YAW))
    # Disbelief up close: the hand must stay below the visor while the head shakes.
    visor = C.closeup_camera(Vector((0, 0, 1.3)), 1.6, azimuth=-12, elevation=3, lens=50)
    paths.append(MP.strip(base, actions["Disbelief"], visor, out, "r35_Disbelief_visor", [22, 32, 38, 46, 52, 62, 74, 84],
                          mode="pistol"))
    # Key poses next to Idle: front, and at the real play zoom.
    idle = bpy.data.actions["Idle"]
    layout = [(idle, 10, (-2.4, 0, 0), 0.0)] + [(actions[n], f, (-1.2 + 1.2 * i, 0, 0), 0.0)
                                                 for i, (n, f) in enumerate(KEY_FRAMES.items())]
    paths.append(MP.game_scene(base, [(a, f, p, GAME_YAW) for a, f, p, _ in layout], os.path.join(out, "r35_keys_game.png"),
                               1.0, close_lights=lights, target=(0, 0, 0.6)))
    paths.append(MP.game_scene(base, [(a, f, p, GAME_YAW) for a, f, p, _ in layout],
                               os.path.join(out, "r35_keys_game_zoomed.png"), 1.6, close_lights=lights, target=(0, 0, 0.6)))
    rigs = [MP.nla_rig(base, a, f, p, y) for a, f, p, y in layout]
    base.hide(True)
    scene = bpy.context.scene
    scene.frame_set(1000)
    for r in rigs:
        r.place(MP.WEAPON_OF.get(r.act.name, "none"))
    wide = C.closeup_camera(Vector((0, 0, 0.85)), 7.5, azimuth=0, elevation=2, lens=35)
    paths.append(C.render(wide, os.path.join(out, "r35_keys_front.png"), 1600, 720, samples=24))
    for r in rigs:
        r.remove()
    base.hide(False)
    C.log("round 35 previews", paths)
    C.log("events", events)
    return paths
