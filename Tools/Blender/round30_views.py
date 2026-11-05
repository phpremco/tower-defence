"""Round 30 QA views: opens a boss .blend and renders a turntable plus game-camera views (ortho, 44 degrees, dark
ground) with a Gate_G1 stand-in for scale. Used for the before/after comparison of the Frost Warden redesign.

Run: blender --background --factory-startup --python Tools/Blender/round30_views.py -- --blend FrostWarden
     --preview <dir> [--label before] [--clip Idle] [--frame 20] [--gate 3.2]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import common as C  # noqa: E402


def main():
    args = C.cli_args()
    name = args.get("blend", "FrostWarden")
    out = C.preview_dir()
    label = args.get("label", "view")
    clip = args.get("clip", "Idle")
    frame = int(args.get("frame", 20))
    gate_f = float(args.get("gate", 3.2))
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    for o in bpy.data.objects:
        if o.name.endswith("_High") or "_LOD1" in o.name:
            o.hide_render = True
    M.preview_scene()
    C.ground()
    act = bpy.data.actions[clip]
    M.turntable(name, arm, act, frame, (0, -0.2, 2.0), 16, out, label=f"{label}_turntable")
    C.set_action(arm, act, frame)
    for i, (az, el, aim, d) in enumerate(((-30, 18, (0, -0.4, 2.9), 8.0), (-150, 35, (0, 0.3, 3.1), 8.5))):
        cam = C.closeup_camera(Vector(aim), d, azimuth=az, elevation=el, lens=55)
        C.render(cam, os.path.join(out, f"{name}_{label}_close{i}.png"), 1280, 720, samples=32)
        bpy.data.objects.remove(cam)
    # Game views with a G1 gate ahead (Blender -Y is forward).
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(C.EXPORTS, "Gate", "Gate_G1.fbx"))
    gate = [o for o in bpy.data.objects if o not in before]
    for o in gate:
        if o.parent is None:
            o.location = (0.0, -gate_f, 0.0)
        if o.type == "MESH" and not o.name.endswith("G1"):
            o.hide_render = True
    C.hide_lights([o for o in bpy.data.objects if o.type == "LIGHT"], True)
    lights = C.game_lights()
    paths = []
    for i, (c, f) in enumerate(((clip, frame), ("Walk", 12), ("WalkEnraged", 10))):
        if c not in bpy.data.actions:
            continue
        C.set_action(arm, bpy.data.actions[c], f)
        for zoom in (1.7,):
            cam = C.game_camera(Vector((0, -gate_f * 0.35, 1.6)), 900 / C.GAME_PIXELS_PER_UNIT / zoom)
            paths.append(C.render(cam, os.path.join(out, f"_{label}_game_{i}.png"), 900, 620, samples=24))
            bpy.data.objects.remove(cam)
    C.contact_sheet(paths, os.path.join(out, f"{name}_{label}_game.png"), 3, 600)
    for p in paths:
        os.remove(p)
    for o in lights:
        bpy.data.objects.remove(o)


if __name__ == "__main__":
    main()
