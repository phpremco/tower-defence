"""Round 49F QA views: bright panels for the five creatures of Remco's 49C/49D verdicts (Bulwark and Crystal Warlord:
"tail, neck and head very static", plus floating crystals on the Warlord; Shattered: crystal positions and teeth
through the lips; Sealed One: "I don't understand the mouth"; Shardback: "spikes not attached to the body").

Opens ArtSource/Blend/<Name>.blend read-only (never saved) with its extra parts (LOD1 hidden) and writes
ArtSource/Previews/QA/r49f_<key>_<tag>_<panel>.png:
  game   the play camera's angle (orthographic, 44 degrees down), framed on the creature, walk mid-pose
  close  a 3/4 perspective close-up of the head and front, standing clip
  move   a low 3/4 side view of the walk mid-pose
  mouth  a close look at the mouth (the jaw at its most closed frame of the standing clip, for the teeth)
--clips: r49f_<key>_<tag>_clips.png, every clip at six frames from the side (head, neck and tail in view), and
r49f_<key>_<tag>_clip_steps.json: per clip the largest per-frame bone turn (r49_views.clip_steps) plus the swing of
the head against the chest and of the tail tip against the pelvis (peak-to-peak degrees: how much they move).
--steps: only the JSON. --from <dir>: render the backed-up .blend of round 49D (<dir>/<Name>_49F/<Name>.blend) with
its own maps (the before panels).

Run: blender -b --factory-startup --python Tools/Blender/r49f_views.py -- --only bulwark,shardback --tag after
     [--clips | --steps] [--from ArtSource/Exports/_backup_r49]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import r49_views as V  # noqa: E402
import r49c_views as V49C  # noqa: E402

QA = V.QA
# Round 49G reuses these panels under its own prefix and backup folder (r49g_views.py sets both).
PREFIX = "r49f"
BACKUP_SUFFIX = "_49F"

# key: (blend name, move clip, move frame, stand clip, stand frame, face bone, face share, mouth aim, mouth share of
# size, mouth azimuth, mouth elevation, close azimuth)
VIEWS = {
    "bulwark": ("Bulwark", "Walk", 8, "Idle", 15, "Head", 0.6, ("Head", 0.75), 0.42, -25, 12, -30),
    "crystalwarlord": ("CrystalWarlord", "Walk", 8, "Idle", 15, "Head", 0.6, ("Head", 0.75), 0.4, -25, 12, -30),
    "shattered": ("Shattered", "Walk", 14, "Idle", 15, "Head", 0.6, (C.G(0, 1.15, 1.85)), 0.3, -22, 8, -38),
    "sealedone": ("SealedOne", "Walk", 20, "Idle", 30, "Head", 0.4, (C.G(0, 3.62, 1.05)), 0.36, -20, 10, -38),
    "shardback": ("Shardback", "Walk", 12, "Idle", 15, "Head", 0.6, ("Head", 0.9), 0.5, -25, 25, -38),
}
NECK = {"bulwark": ("Spine", "Head", "Pelvis", "Tail_3"), "crystalwarlord": ("Spine", "Head", "Pelvis", "Tail_3")}


def open_creature(name, backup=None):
    path = os.path.join(backup, name + BACKUP_SUFFIX, name + ".blend") if backup else os.path.join(C.BLEND, name + ".blend")
    bpy.ops.wm.open_mainfile(filepath=path)
    if backup:
        V49C.relink_backup_maps(os.path.join(backup, name + BACKUP_SUFFIX))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.hide_render
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        elif o.type == "MESH" and o not in meshes:
            o.hide_render = True
    return arm, meshes


def closed_jaw_frame(arm, act, bone="Jaw"):
    """The frame of `act` where the jaw bone is turned least from its rest (the mouth most closed)."""
    if bone not in arm.pose.bones:
        return 0
    best = (1e9, 0)
    for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
        C.set_action(arm, act, f)
        ang = arm.pose.bones[bone].matrix_basis.to_quaternion().angle
        best = min(best, (ang, f))
    return best[1]


def run(key, tag, backup):
    name, move, mframe, stand, sframe, fbone, fshare, maim, mshare, maz, mel, caz = VIEWS[key]
    arm, meshes = open_creature(name, backup)
    C.set_action(arm, bpy.data.actions[stand], sframe)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    centre = (lo + hi) * 0.5
    face = V49C.face_point(arm, fbone, fshare)
    os.makedirs(QA, exist_ok=True)
    out = []

    def shot(panel, act, frame, camera):
        C.set_action(arm, bpy.data.actions[act], frame)
        path = os.path.join(QA, f"{PREFIX}_{key}_{tag}_{panel}.png")
        out.append(C.render(camera, path, 1280, 720))

    shot("game", move, mframe, V.cam("game", centre, size * 1.15))
    shot("close", stand, sframe, V.cam("close", face.lerp(centre, 0.4), size, caz, 18, size * 0.95))
    shot("move", move, mframe, V.cam("close", centre, size, -72, 8, size * 1.5))
    act = bpy.data.actions[stand]
    jf = closed_jaw_frame(arm, act)
    C.set_action(arm, act, jf)
    target = V49C.face_point(arm, *maim) if isinstance(maim, tuple) else maim
    shot("mouth", stand, jf, V.cam("close", target, size, maz, mel, size * mshare))
    if key == "shardback":
        shed_panel(arm, key, tag, face.lerp(centre, 0.6), size)
    C.contact_sheet(out, os.path.join(QA, f"{PREFIX}_{key}_{tag}.png"), 2, 640)
    C.log(key, tag, "views", len(out), "mouth frame", jf)


def shed_panel(arm, key, tag, target, size):
    """The Shardback with Shard_1..3 shed as Unity sheds them (bone scale 0 over the Idle pose): the clusters must
    vanish whole and leave their craters (each cluster is its own island on its own bone)."""
    act = bpy.data.actions["Idle"]
    shards = [b.name for b in arm.pose.bones if b.name.startswith("Shard_")]
    muted = [fc for fc in K2.action_fcurves(act)
             if fc.data_path.endswith(".scale") and any(f'"{n}"' in fc.data_path for n in shards)]
    for fc in muted:
        fc.mute = True
    C.set_action(arm, act, 15)
    for n in shards:
        arm.pose.bones[n].scale = (0.001,) * 3 if n in ("Shard_1", "Shard_2", "Shard_3") else (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    camera = V.cam("close", target, size, -38, 34, size * 1.1)
    C.render(camera, os.path.join(QA, f"{PREFIX}_{key}_{tag}_shed.png"), 1280, 720)
    for n in shards:
        arm.pose.bones[n].scale = (1.0, 1.0, 1.0)
    for fc in muted:
        fc.mute = False


def swing(arm, act, parent, bone):
    """Peak-to-peak turn (degrees) of `bone` against `parent` over a clip (armature-space directions)."""
    dirs = []
    for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
        C.set_action(arm, act, f)
        p, b = arm.pose.bones[parent], arm.pose.bones[bone]
        rel = p.matrix.to_3x3().inverted() @ (b.tail - b.head).normalized()
        dirs.append(rel.normalized())
    worst = 0.0
    for i in range(len(dirs)):
        for j in range(i + 1, len(dirs)):
            worst = max(worst, math.degrees(dirs[i].angle(dirs[j], 0.0)))
    return round(worst, 1)


def clips(key, tag, backup, render=True):
    name = VIEWS[key][0]
    arm, meshes = open_creature(name, backup)
    steps = V.clip_steps(arm)
    if key in NECK:
        chest, head, pelvis, tail = NECK[key]
        for clip in steps:
            act = bpy.data.actions[clip]
            steps[clip]["head_vs_chest_deg"] = swing(arm, act, chest, head)
            steps[clip]["tail_tip_vs_pelvis_deg"] = swing(arm, act, pelvis, tail)
    with open(os.path.join(QA, f"{PREFIX}_{key}_{tag}_clip_steps.json"), "w", encoding="utf-8") as fh:
        json.dump(steps, fh, indent=2)
    C.log(key, json.dumps(steps))
    if not render:
        return
    C.set_action(arm, bpy.data.actions[VIEWS[key][3]], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    centre = (lo + hi) * 0.5
    paths = []
    tmp = os.path.join(QA, "_r49f_tmp")
    os.makedirs(tmp, exist_ok=True)
    for clip in steps:
        act = bpy.data.actions[clip]
        n = int(act.frame_range[1])
        for i in range(6):
            f = int(round(i * n / 5))
            C.set_action(arm, act, f)
            camera = V.cam("close", centre, size, -62, 14, size * 1.7)
            paths.append(C.render(camera, os.path.join(tmp, f"{key}_{clip}_{i}.png"), 480, 300, samples=16))
    C.contact_sheet(paths, os.path.join(QA, f"{PREFIX}_{key}_{tag}_clips.png"), 6, 320)


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    backup = args.get("from")
    if backup and not os.path.isabs(backup):
        backup = os.path.join(C.REPO, backup)
    for key in (args.get("only") or ",".join(VIEWS)).split(","):
        if not key:
            continue
        if args.get("clips") or args.get("steps"):
            clips(key, tag, backup, render=not args.get("steps"))
        else:
            run(key, tag, backup)


if __name__ == "__main__":
    main()
