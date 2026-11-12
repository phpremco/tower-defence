"""Round 49D QA views: bright before/after panels for the six crystal and shield creatures (Remco's verdicts of
2026-11-12: Shardback circles too perfect; Brood Mother, Hive Queen, Shattered crystals too perfect; Crystal
Warlord = Bulwark fixes plus crystals; Bulwark shield, head, sword and smoother clips).

Opens ArtSource/Blend/<Name>.blend read-only (never saved) with its extra parts (LOD1 hidden) and writes
ArtSource/Previews/QA/r49_<key>_<tag>_<panel>.png:
  game   the play camera's angle (orthographic, 44 degrees down), framed on the creature, walk mid-pose
  close  a 3/4 perspective close-up of the head and front, standing clip
  move   a low 3/4 side view of the walk mid-pose
With --clips also r49_<key>_<tag>_clips.png (every clip, four frames, side view) and r49_<key>_<tag>_clip_steps.json
(per clip: the largest per-frame bone turn in armature space, its bone and frame, and its ratio to that bone's
median step; see r49_views.clip_steps). --steps writes only the JSON.

Run: blender -b --factory-startup --python Tools/Blender/r49d_views.py -- --only shardback,bulwark --tag before
     [--clips] [--steps]
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402

QA = V.QA

# key: (blend name, move clip, move frame, stand clip, head bone, close azimuth)
VIEWS = {
    "shardback": ("Shardback", "Walk", 12, "Idle", "Head", -38),
    "broodmother": ("BroodMother", "Walk", 12, "Idle", "Head", -38),
    "queen": ("Queen", "Walk", 10, "Idle", "Head", -38),
    "shattered": ("Shattered", "Walk", 14, "Idle", "Head", -38),
    "crystalwarlord": ("CrystalWarlord", "Walk", 8, "Idle", "Head", -30),
    "bulwark": ("Bulwark", "Walk", 8, "Idle", "Head", -30),
}


def open_creature(name):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        elif o.type == "MESH" and o not in meshes:
            o.hide_render = True
    for o in meshes:
        o.hide_render = False
    return arm, meshes


def run(key, tag):
    name, move, mframe, stand, head_bone, az = VIEWS[key]
    arm, meshes = open_creature(name)
    C.set_action(arm, bpy.data.actions[stand], 15)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    centre = (lo + hi) * 0.5
    pb = arm.pose.bones[head_bone]
    face = (arm.matrix_world @ pb.head).lerp(arm.matrix_world @ pb.tail, 0.5)
    os.makedirs(QA, exist_ok=True)

    def shot(panel, act, frame, camera):
        C.set_action(arm, bpy.data.actions[act], frame)
        return C.render(camera, os.path.join(QA, f"r49_{key}_{tag}_{panel}.png"), 1280, 720)

    paths = [shot("game", move, mframe, V.cam("game", centre, size * 1.15)),
             shot("close", stand, 15, V.cam("close", face.lerp(centre, 0.45), size, az, 18, size * 1.0)),
             shot("move", move, mframe, V.cam("close", centre, size, -72, 8, size * 1.5))]
    C.contact_sheet(paths, os.path.join(QA, "_r49_tmp", f"r49_{key}_{tag}_sheet.png"), 3, 640)
    C.log(key, tag, "views", len(paths))


def clips(key, tag, render=True):
    name, move, mframe, stand, head_bone, az = VIEWS[key]
    arm, meshes = open_creature(name)
    steps = V.clip_steps(arm)
    with open(os.path.join(QA, f"r49_{key}_{tag}_clip_steps.json"), "w", encoding="utf-8") as fh:
        json.dump(steps, fh, indent=2)
    C.log(key, json.dumps(steps))
    if not render:
        return
    C.set_action(arm, bpy.data.actions[stand], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    centre = (lo + hi) * 0.5
    paths = []
    for clip in steps:
        act = bpy.data.actions[clip]
        n = int(act.frame_range[1])
        for i, f in enumerate((0, n // 3, (2 * n) // 3, n)):
            C.set_action(arm, act, f)
            camera = V.cam("close", centre + Vector((0, 0, 0)), size, -60, 12, size * 1.8)
            paths.append(C.render(camera, os.path.join(QA, "_r49_tmp", f"{key}_{clip}_{i}.png"), 640, 360, samples=16))
    C.contact_sheet(paths, os.path.join(QA, f"r49_{key}_{tag}_clips.png"), 4, 400)


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    for key in (args.get("only") or ",".join(VIEWS)).split(","):
        if not key:
            continue
        if args.get("clips") or args.get("steps"):
            clips(key, tag, render=not args.get("steps"))
        else:
            run(key, tag)


if __name__ == "__main__":
    main()
