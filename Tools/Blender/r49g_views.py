"""Round 49G QA views (Remco: "you can see the spikes floating!!"; "those teeth are strange with the new mouth").

Opens ArtSource/Blend/<Name>.blend read-only (never saved), LOD1 hidden, bright lighting, and writes to
ArtSource/Previews/QA/:
  r49g_<key>_<tag>_game|close|move|mouth.png   the four r49f panels (r49f_views.run under the r49g prefix)
  r49g_<key>_<tag>.png                          their 2x2 sheet
  r49g_<key>_loose_<clip>.png                   (--loose) per clip: 4 frames (rows) x 4 angles (columns), long lens,
                                                tight on the body, grazing light: anything off the body shows
                                                against the pale ground and sky
  r49g_<key>_mouth_clips.png                    (--mouth) per clip the most-closed and the most-open jaw frame, a
                                                close 3/4 look at the mouth (rows: clips)
--from <dir>: the backed-up .blend (<dir>/<Name>_49G/<Name>.blend) with its own maps (the before panels; the tag
should then be "before").

Run: blender -b --factory-startup --python Tools/Blender/r49g_views.py -- --only shardback --tag after
     [--panels] [--loose] [--mouth] [--clips Walk,Idle] [--from ArtSource/Exports/_backup_r49]
(no --panels/--loose/--mouth: all three)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402
import r49c_views as V49C  # noqa: E402
import r49f_views as R  # noqa: E402

QA = V.QA
R.PREFIX = "r49g"
R.BACKUP_SUFFIX = "_49G"
LOOSE_ANGLES = ((-60, 6), (120, 6), (-150, 22), (35, 38))
CELL = (720, 450)
LENS = 120.0
SKIP = ("_LOD", "_Full", "_Away")


def clip_actions(clips=None):
    out = [a for a in bpy.data.actions if not a.name.endswith(SKIP) and "_LOD" not in a.name]
    if clips:
        out = [a for a in out if a.name in clips]
    return sorted(out, key=lambda a: a.name)


def tele_camera(target, radius, azimuth, elevation, aspect=CELL[0] / CELL[1], margin=0.62):
    """A long-lens camera whose frame holds a sphere of `radius` round `target` (vertical fit)."""
    half_v = math.atan(18.0 / LENS / aspect)
    distance = radius * margin / math.sin(half_v)
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    return C.closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=LENS)


def loose_sheets(key, tag, backup, clips):
    name = R.VIEWS[key][0]
    arm, meshes = R.open_creature(name, backup)
    C.set_action(arm, bpy.data.actions[R.VIEWS[key][3]], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    tmp = os.path.join(QA, "_r49g_tmp")
    os.makedirs(tmp, exist_ok=True)
    for act in clip_actions(clips):
        n = int(act.frame_range[1])
        frames = sorted({int(round(i * n / 3)) for i in range(4)})
        paths = []
        for f in frames:
            C.set_action(arm, act, f)
            plo, phi = V.bounds(meshes)
            centre = (plo + phi) * 0.5
            radius = (phi - plo).length * 0.5
            for az, el in LOOSE_ANGLES:
                cam = tele_camera(centre, radius, az, el)
                paths.append(C.render(cam, os.path.join(tmp, f"{key}_{act.name}_{f}_{az}.png"), *CELL, samples=24))
        label = "loose" if tag == "after" else f"{tag}_loose"
        out = os.path.join(QA, f"r49g_{key}_{label}_{act.name}.png")
        C.contact_sheet(paths, out, len(LOOSE_ANGLES), CELL[0])
        C.log("loose sheet", out, "frames", frames)


def mouth_sheet(key, tag, backup, clips):
    name, _, _, stand, _, fbone, fshare, maim, mshare, maz, mel, _ = R.VIEWS[key]
    arm, meshes = R.open_creature(name, backup)
    C.set_action(arm, bpy.data.actions[stand], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    tmp = os.path.join(QA, "_r49g_tmp")
    os.makedirs(tmp, exist_ok=True)
    paths = []
    for act in clip_actions(clips):
        angles = []
        for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
            C.set_action(arm, act, f)
            angles.append((arm.pose.bones["Jaw"].matrix_basis.to_quaternion().angle, f))
        for f in (min(angles)[1], max(angles)[1]):
            C.set_action(arm, act, f)
            target = V49C.face_point(arm, *maim) if isinstance(maim, tuple) else maim
            for az in (maz, maz - 50):
                cam = V.cam("close", target, size, az, mel, size * mshare)
                paths.append(C.render(cam, os.path.join(tmp, f"{key}_mouth_{act.name}_{f}_{az}.png"), 640, 400,
                                      samples=24))
    out = os.path.join(QA, f"r49g_{key}_{tag}_mouth_clips.png")
    C.contact_sheet(paths, out, 4, 640)
    C.log("mouth sheet", out)


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    backup = args.get("from")
    if backup and not os.path.isabs(backup):
        backup = os.path.join(C.REPO, backup)
    clips = [c for c in (args.get("clips") or "").split(",") if c]
    every = not (args.get("panels") or args.get("loose") or args.get("mouth"))
    for key in (args.get("only") or "").split(","):
        if not key:
            continue
        if every or args.get("panels"):
            R.run(key, tag, backup)
        if every or args.get("loose"):
            loose_sheets(key, tag, backup, clips)
        if (every or args.get("mouth")) and key != "shardback":
            mouth_sheet(key, tag, backup, clips)


if __name__ == "__main__":
    main()
