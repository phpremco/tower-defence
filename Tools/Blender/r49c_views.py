"""Round 49C QA views: bright sheets to judge heads, faces and silhouettes (Remco's verdicts of 2026-11-12 on the
element bats, the bat boss, the Stinger, the Acid Empress, the Sealed One and the Frost Warden).

Opens ArtSource/Blend/<Blend>.blend (read-only, never saved; body plus its extra parts, LOD hidden) and writes
ArtSource/Previews/QA/r49_<key>_<tag>_<panel>.png and the 2x2 sheet r49_<key>_<tag>.png:
  game   the play camera's angle (orthographic, 44 degrees down, yaw 12), framed on the creature, mid-move pose
  close  a 3/4 perspective close-up of the head, standing clip
  move   a low 3/4 side view of the walk/fly mid-pose
  mouth  a close look at the face from the play camera's side, standing clip
`--side` (the bats) also writes r49_<key>_<tag>_side.png: the Fly clip at four frames from the angle of the round-46
in-game side shots (high 3/4 from the front-right and front-left), the angle where the near wing seemed to grow out
of the head.

Run: blender -b --factory-startup --python Tools/Blender/r49c_views.py -- --only Bat,SkySovereign --tag before [--side]
     [--from ArtSource/Exports/_backup_r49]   (the backed-up .blend files, for the before sheets)
     [--steps]   (instead of the renders: r49_<key>_<tag>_clip_steps.json, the largest per-frame bone turn per clip)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402

QA = V.QA

# blend name: (preview key, move clip, move frame, stand clip, stand frame, flyer, face bone, face share along it)
VIEWS = {
    "Bat": ("bat", "Fly", 0, "Glide", 10, True, "Head", 0.6),
    "SkySovereign": ("batboss", "Fly", 0, "Glide", 10, True, "Head", 0.6),
    "Stinger": ("stinger", "Walk", 7, "Idle", 15, False, "Body", 0.95),
    "AcidEmpress": ("acidempress", "Walk", 7, "Idle", 15, False, "Body", 0.95),
    "SealedOne": ("sealedone", "Walk", 20, "Idle", 30, False, "Head", 0.4),
    "FrostWarden": ("frostwarden", "Walk", 16, "Idle", 30, False, "Head", 0.6),
}

# Mouth close-ups aimed at the mouth itself: blend name -> (bone, share along it) or a rest point G(x, up, forward),
# distance as a share of the creature's size, azimuth, elevation.
MOUTH = {
    "Stinger": (C.G(0, 0.2, 0.46), 0.32, -28, 26),
    "AcidEmpress": (C.G(0, 0.2, 0.46) * 2.2, 0.32, -28, 26),
    "SealedOne": (C.G(0, 3.7, 1.0), 0.42, -20, 6),
    "FrostWarden": (C.G(0, 2.4, 2.75), 0.5, -18, 14),
}


def open_creature(name):
    # `--from <dir>` opens <dir>/<name>/<name>.blend instead (the before sheets from the round's backups).
    src = C.cli_args().get("from")
    path = os.path.join(src, name, name + ".blend") if src else os.path.join(C.BLEND, name + ".blend")
    bpy.ops.wm.open_mainfile(filepath=path)
    if src:
        relink_backup_maps(os.path.join(src, name))
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


def relink_backup_maps(folder):
    """A backed-up .blend points at its maps relative to ArtSource/Blend; point every image at the backed-up copy of
    the same file (same name, Low/ for the Low set), so the before sheet shows the old maps, not the new ones."""
    found = {}
    for root, _, files in os.walk(folder):
        for f in files:
            found.setdefault((f.lower(), "low" in os.path.relpath(root, folder).lower()), os.path.join(root, f))
    for img in bpy.data.images:
        if not img.filepath:
            continue
        name = os.path.basename(bpy.path.abspath(img.filepath))
        parts = img.filepath.replace("\\", "/").lower().split("/")
        low = len(parts) > 1 and parts[-2] == "low"
        hit = found.get((name.lower(), low)) or found.get((name.lower(), not low))
        if hit:
            img.filepath = hit
            img.reload()


def face_point(arm, bone, share):
    pb = arm.pose.bones[bone]
    return (arm.matrix_world @ pb.head).lerp(arm.matrix_world @ pb.tail, share)


def run(name, tag, side):
    key, move, mframe, stand, sframe, flyer, fbone, fshare = VIEWS[name]
    arm, meshes = open_creature(name)
    C.set_action(arm, bpy.data.actions[stand], sframe)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y)
    ground_z = lo.z - (0.35 * size if flyer else 0.0)
    V.bright_scene(size, ground_z)
    centre = (lo + hi) * 0.5
    face = face_point(arm, fbone, fshare)
    tip = face_point(arm, fbone, 1.0)
    out = []
    os.makedirs(QA, exist_ok=True)

    def shot(panel, act, frame, camera, store=True):
        C.set_action(arm, bpy.data.actions[act], frame)
        path = os.path.join(QA, f"r49_{key}_{tag}_{panel}.png")
        C.render(camera, path, 1280, 720)
        if store:
            out.append(path)
        return path

    shot("game", move, mframe, V.cam("game", centre, size * 1.25))
    shot("close", stand, sframe, V.cam("close", face.lerp(centre, 0.3), size, -38, 20, size * 0.95))
    shot("move", move, mframe, V.cam("close", centre, size, -68, 8, size * 1.55))
    if name in MOUTH:
        aim, share, az, el = MOUTH[name]
        target = face_point(arm, *aim) if isinstance(aim, tuple) else aim
        shot("mouth", stand, sframe, V.cam("close", target, size, az, el, size * share))
    else:
        shot("mouth", stand, sframe, V.cam("close", face.lerp(tip, 0.4), size, -25, 30 if not flyer else 20, size * 0.42))
    C.contact_sheet(out, os.path.join(QA, f"r49_{key}_{tag}.png"), 2, 640)
    if side:
        side_sheet(arm, key, tag, centre, size)
    C.log(name, tag, "views", len(out))


def side_sheet(arm, key, tag, centre, size):
    """The Fly clip at four frames, high 3/4 from the front-left and the front-right (the round-46 side shots)."""
    act = bpy.data.actions["Fly"]
    n = int(act.frame_range[1])
    paths = []
    for az in (-50, 50):
        for f in (0, n // 4, n // 2, (3 * n) // 4):
            C.set_action(arm, act, f)
            camera = V.cam("close", centre, size, az, 40, size * 1.25)
            paths.append(C.render(camera, os.path.join(QA, "_r49_tmp", f"{key}_side_{az}_{f}.png"), 800, 600, samples=24))
    C.contact_sheet(paths, os.path.join(QA, f"r49_{key}_{tag}_side.png"), 4, 480)
    for p in paths:
        os.remove(p)


def steps(name, tag):
    """Per clip: the largest per-frame bone turn (r49_views.clip_steps) in r49_<key>_<tag>_clip_steps.json."""
    import json
    arm, _ = open_creature(name)
    out = V.clip_steps(arm)
    path = os.path.join(QA, f"r49_{VIEWS[name][0]}_{tag}_clip_steps.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=2)
    C.log(name, "clip steps", path)


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    for name in (args.get("only") or ",".join(VIEWS)).split(","):
        if name:
            if args.get("steps"):
                steps(name, tag)
            else:
                run(name, tag, bool(args.get("side")))


if __name__ == "__main__":
    main()
