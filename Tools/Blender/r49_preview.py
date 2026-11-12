"""Round 49B: before/after preview sheets for the redesigned species (Remco's verdicts of 2026-11-12).

Opens ArtSource/Blend/<Name>.blend (read-only, never saved) with its round-46 extra parts and renders, lit brightly
enough to judge shape:
  1. the play camera angle (orthographic, 44 degrees down) framed on the creature (shape read from above),
  2. a 3/4 close-up (perspective),
  3. a walk mid-pose from the side,
  4. the play camera at in-game size (zoom 2, as the round-46 sheets: real pixels).
Writes ArtSource/Previews/QA/r49_<name>_<tag>.png (2x2 sheet) and, for tag "after", also r49_<name>_walk.png.

Run: blender -b --factory-startup --python Tools/Blender/r49_preview.py -- --name Cragback --tag before
     [--face x,up,forward,distance]   (also r49_<name>_<tag>_face.png: two close-ups of the face)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_small as S  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")


def bright_scene(size):
    for o in list(bpy.data.objects):
        if o.type in ("LIGHT", "CAMERA") or o.name.startswith("Ground"):
            bpy.data.objects.remove(o)
    C.preview_world(color=(0.07, 0.08, 0.1), strength=1.6)
    rig = C.light_rig(scale=size)
    for o in rig:
        o.data.energy *= 1.8 if o.data.type == "SUN" else 1.5
    C.ground(color=(0.09, 0.09, 0.1))
    for m in bpy.data.materials:
        m.use_backface_culling = True
    return rig


def walk_action(name):
    for clip in ("Walk", "Crawl", "Fly", "Glide", "Idle"):
        act = bpy.data.actions.get(clip)
        if act is not None:
            return act
    raise RuntimeError(name + ": no walk clip")


def bounds(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    for o in objs:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            p = ev.matrix_world @ v.co
            lo = Vector(map(min, lo, p))
            hi = Vector(map(max, hi, p))
        ev.to_mesh_clear()
    return lo, hi


def main():
    args = C.cli_args()
    name, tag = args["name"], args.get("tag", "after")
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    full = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.hide_render]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        if o.type == "MESH" and o.parent is None and not o.name.startswith("Ground"):
            o.hide_render = True
    rep = S.report(name)
    scale = float(rep.get("unity_scale") or S.unity_scale(name, rep))
    walk = walk_action(name)
    idle = bpy.data.actions.get("Idle") or walk
    C.set_action(arm, idle, 20)
    lo, hi = bounds(full)
    centre = (lo + hi) / 2
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    bright_scene(max(1.0, size * 0.8))
    os.makedirs(QA, exist_ok=True)
    low = name.lower()
    paths = []
    p = os.path.join(QA, f"r49_{low}_{tag}_game.png")
    C.set_action(arm, walk, int(walk.frame_range[1] * 0.25))
    paths.append(C.render(C.game_camera(centre, size * 1.35), p, 960, 720))
    C.set_action(arm, idle, 20)
    p = os.path.join(QA, f"r49_{low}_{tag}_close.png")
    paths.append(C.render(C.closeup_camera(centre + Vector((0, -size * 0.12, 0)), size * 1.55, azimuth=-36,
                                           elevation=20, lens=50), p, 960, 720))
    mid = int(walk.frame_range[1] * 0.25)
    C.set_action(arm, walk, mid)
    p = os.path.join(QA, f"r49_{low}_{tag}_walk.png")
    paths.append(C.render(C.closeup_camera(centre, size * 1.7, azimuth=-90, elevation=6, lens=50), p, 960, 720))
    p = os.path.join(QA, f"r49_{low}_{tag}_play.png")
    C.set_action(arm, walk, mid)
    paths.append(C.render(C.game_camera(centre, 640 / C.GAME_PIXELS_PER_UNIT / 2.0 / scale), p, 640, 360))
    C.contact_sheet(paths, os.path.join(QA, f"r49_{low}_{tag}.png"), 2, 720)
    if args.get("face"):
        # --face x,up,forward,distance: two face close-ups (3/4 and front) in r49_<name>_<tag>_face.png.
        fx, fu, ff, fd = (float(v) for v in args["face"].split(","))
        C.set_action(arm, idle, 20)
        target = C.G(fx, fu, ff)
        faces = [C.render(C.closeup_camera(target, fd, azimuth=-32, elevation=14, lens=55),
                          os.path.join(QA, f"r49_{low}_{tag}_f1.png"), 960, 720),
                 C.render(C.closeup_camera(target, fd, azimuth=-4, elevation=24, lens=55),
                          os.path.join(QA, f"r49_{low}_{tag}_f2.png"), 960, 720)]
        C.contact_sheet(faces, os.path.join(QA, f"r49_{low}_{tag}_face.png"), 2, 720)
        for p in faces:
            os.remove(p)
    keep = {f"r49_{low}_{tag}_walk.png"} if tag == "after" else set()
    for p in paths:
        if os.path.basename(p) in keep:
            os.replace(p, os.path.join(QA, f"r49_{low}_walk.png"))
        else:
            os.remove(p)
    C.log("r49 preview", name, tag, "size", round(size, 3), "scale", scale)


if __name__ == "__main__":
    main()
