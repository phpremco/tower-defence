"""Round 32 QA views for the Frost Warden (Rime Colossus): a studio turntable, game-camera views next to a Gate_G2 and a
Tower_Gun_T2 at Unity scale 1 (ortho, 44 degrees, level lighting), and game-angle clip strips.

`stage(...)` is imported by build_frostwarden.py --blockout. Run on its own after build_frostwarden_mech.py:
blender --background --factory-startup --python Tools/Blender/round32_views.py -- --preview <dir> [--label final]
        [--clips Walk,Freeze_Gate,...] [--gate 3.6]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402

NAME = "FrostWarden"


def import_fbx(path, keep_suffix):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type == "MESH" and (not o.name.endswith(keep_suffix) or "LOD" in o.name or "Overlay" in o.name
                                 or "Damage" in o.name):
            o.hide_render = True
            o.hide_viewport = True
    return [o for o in new if o.parent is None], new


def props(gate_f):
    gate, _ = import_fbx(os.path.join(C.EXPORTS, "Gate", "Gate_G2.fbx"), "G2")
    tower, _ = import_fbx(os.path.join(C.EXPORTS, "Tower_Gun", "Tower_Gun_T2.fbx"), "T2")
    for o in gate:
        o.location = Vector((0.0, -gate_f, 0.0))
    for o in tower:
        o.location = Vector((3.9, -gate_f + 1.2, 0.0))
    return gate, tower


def show(objs, on):
    for o in objs:
        for c in [o] + list(o.children_recursive):
            if c.type == "MESH" and not c.hide_viewport:
                c.hide_render = not on


def render_cells(cams, out, tag, w, h, samples):
    paths = []
    for i, cam in enumerate(cams):
        paths.append(C.render(cam, os.path.join(out, f"_{tag}_{i}.png"), w, h, samples=samples))
        bpy.data.objects.remove(cam)
    return paths


def sheet(paths, path, cols, cell):
    C.contact_sheet(paths, path, cols, cell)
    for p in paths:
        os.remove(p)
    return path


def stage(arm, meshes, actions, out, label, shots, strips=(), gate_f=3.6, tt=("Idle", 20), samples=24):
    """`shots`: [(clip, frame)] rendered from three game angles next to the gate and tower; `strips`: [(clip,
    frames)] rendered from the 3/4-front game angle. Writes `<label>_turntable.png`, `<label>_game.png`,
    `<label>_game_<clip>.png`."""
    C.preview_world()
    ground = C.ground()
    gate, tower = props(gate_f)
    written = []
    # Studio turntable (no props).
    show(gate + tower, False)
    rig = C.light_rig(scale=4.0)
    C.set_action(arm, actions[tt[0]], tt[1])
    centre = Vector((0, -0.2, 2.1))
    cams = [C.closeup_camera(centre, 15.5, azimuth=az, elevation=el, lens=55)
            for az, el in ((-38, 16), (-90, 6), (-150, 20), (180, 14), (38, 16), (0, 64))]
    written.append(sheet(render_cells(cams, out, "tt", 620, 440, samples), os.path.join(out, f"{label}_turntable.png"), 3, 620))
    for o in rig:
        bpy.data.objects.remove(o)
    # Game views: (yaw, zoom): 3/4 front, coming down the screen, walking away; play zoom x1.25.
    show(gate + tower, True)
    lights = C.game_lights()
    angles = ((-62, 1.25), (12, 1.25), (192, 1.25))
    paths = []
    for clip, frame in shots:
        if clip not in actions:
            continue
        C.set_action(arm, actions[clip], frame)
        cams = [C.game_camera(Vector((0.4, -gate_f * 0.35, 1.6)), 620 / C.GAME_PIXELS_PER_UNIT / z, yaw=yaw)
                for yaw, z in angles]
        paths += render_cells(cams, out, f"g_{clip}_{frame}", 620, 440, samples)
    if paths:
        written.append(sheet(paths, os.path.join(out, f"{label}_game.png"), 3, 620))
    # True play zoom (1.0), coming down the screen, beside the gate.
    C.set_action(arm, actions[shots[0][0]], shots[0][1])
    cam = C.game_camera(Vector((1.0, -gate_f * 0.4, 0.8)), 1280 / C.GAME_PIXELS_PER_UNIT, yaw=12)
    written.append(C.render(cam, os.path.join(out, f"{label}_game_playzoom.png"), 1280, 720, samples=samples))
    bpy.data.objects.remove(cam)
    for clip, frames in strips:
        if clip not in actions:
            continue
        cams, fl = [], list(frames)
        cells = []
        for i, f in enumerate(fl):
            C.set_action(arm, actions[clip], f)
            cam = C.game_camera(Vector((0.3, -gate_f * 0.45, 1.4)), 620 / C.GAME_PIXELS_PER_UNIT / 1.05, yaw=-62)
            cells.append(C.render(cam, os.path.join(out, f"_s_{clip}_{i}.png"), 620, 440, samples=16))
            bpy.data.objects.remove(cam)
        written.append(sheet(cells, os.path.join(out, f"{label}_game_{clip}.png"), 4, 480))
    for o in lights:
        bpy.data.objects.remove(o)
    C.log("round32 views", written)
    return written


def main():
    args = C.cli_args()
    out = C.preview_dir()
    label = args.get("label", "final")
    gate_f = float(args.get("gate", 3.6))
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm = bpy.data.objects[NAME + "_Rig"]
    for o in bpy.data.objects:
        if o.name.endswith("_High") or "_LOD1" in o.name:
            o.hide_render = True
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm]
    actions = {a.name: a for a in bpy.data.actions if not a.name.endswith("_LOD")}
    clips = str(args.get("clips", "")).split(",") if args.get("clips") else []
    shots = [("Idle", 20), ("Walk", 16), ("WalkEnraged", 10), ("Freeze_Gate", 30)]
    strips = [(c, [int(round(actions[c].frame_range[1] * i / 7)) for i in range(8)]) for c in clips if c in actions]
    stage(arm, meshes, actions, out, label, shots, strips, gate_f=gate_f)


if __name__ == "__main__":
    main()
