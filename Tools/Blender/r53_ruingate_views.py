"""Round 53 QA views of the RuinGate (bright light; the .blend is opened read-only and never saved).

Writes ArtSource/Previews/QA/r53_ruingate_<tag>_*.png:
  game        the play camera (orthographic, 44 degrees down): shut (Closed_Idle 0), opening (Open 76), open
              (Opened_Idle 0), one panel each
  game_open   the play camera, open, 1920x1080 (for 3x crops)
  close       2x2: low 3/4 front (open), the right jamb's inner face, the keystone crest, 3/4 back (open)
  rise        the Open clip from a 3/4 front view: frames 0, 24, 34, 44, 60, 76, 92, 112, 130 and Opened_Idle 0
  rise_game   the same frames from the play camera
  _r53_seq/   every 5th frame of Open from 40 to 100, play camera (for 3x crops)
--from <dir>: the backed-up .blend (<dir>/RuinGate.blend) with its own maps (tag "before").

Run: blender -b --factory-startup --python Tools/Blender/r53_ruingate_views.py -- --tag after
     [--from ArtSource/Exports/_backup_r53/RuinGate]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402
import r49c_views as V49C  # noqa: E402

QA = V.QA
NAME = "RuinGate"
RISE = [0, 24, 34, 44, 60, 76, 92, 112, 130]


def open_gate(backup):
    path = os.path.join(backup, NAME + ".blend") if backup else os.path.join(C.BLEND, NAME + ".blend")
    bpy.ops.wm.open_mainfile(filepath=path)
    if backup:
        V49C.relink_backup_maps(backup)
    arm = bpy.data.objects[NAME + "_Rig"]
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.hide_render
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if o.type == "MESH" and o not in meshes:
            o.hide_render = True
    return arm, meshes


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    backup = args.get("from")
    if backup and not os.path.isabs(backup):
        backup = os.path.join(C.REPO, backup)
    arm, meshes = open_gate(backup)
    acts = {n: bpy.data.actions[n] for n in ("Closed_Idle", "Open", "Opened_Idle")}
    C.set_action(arm, acts["Opened_Idle"], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.z - lo.z)
    V.bright_scene(size, 0.0)
    centre = (lo + hi) * 0.5
    os.makedirs(QA, exist_ok=True)
    pre = os.path.join(QA, f"r53_ruingate_{tag}")

    def shot(path, act, frame, camera, w=960, h=640):
        C.set_action(arm, acts[act], frame)
        out = C.render(camera, path, w, h, samples=32)
        return out

    def game_cam(scale=1.12):
        return V.cam("game", centre, size * scale)

    # Play camera: shut, opening, open.
    tmp = [shot(f"{pre}_g{i}.png", a, f, game_cam()) for i, (a, f) in
           enumerate((("Closed_Idle", 0), ("Open", 76), ("Opened_Idle", 0)))]
    C.contact_sheet(tmp, f"{pre}_game.png", 3, 640)
    for p in tmp:
        os.remove(p)
    shot(f"{pre}_game_open.png", "Opened_Idle", 0, game_cam(1.0), 1920, 1080)
    # Close views.
    jamb = Vector((1.9, -0.2, 1.3))
    apex = Vector((0, 0, hi.z - (hi.z - lo.z) * 0.22))
    views = [(centre, -32, 9, 1.25), (jamb, -52, 14, 0.42), (apex, -22, 22, 0.42), (centre, 148, 14, 1.25)]
    tmp = [shot(f"{pre}_c{i}.png", "Opened_Idle", 0, V.cam("close", t, size, az, el, size * k))
           for i, (t, az, el, k) in enumerate(views)]
    C.contact_sheet(tmp, f"{pre}_close.png", 2, 960)
    for p in tmp:
        os.remove(p)
    # The opening, frame by frame.
    tmp = [shot(f"{pre}_r{i}.png", "Open", f, V.cam("close", centre, size, -28, 14, size * 1.3), 640, 440)
           for i, f in enumerate(RISE)]
    tmp.append(shot(f"{pre}_r{len(RISE)}.png", "Opened_Idle", 0, V.cam("close", centre, size, -28, 14, size * 1.3), 640, 440))
    C.contact_sheet(tmp, f"{pre}_rise.png", 5, 640)
    for p in tmp:
        os.remove(p)
    tmp = [shot(f"{pre}_rg{i}.png", "Open", f, game_cam(), 640, 440) for i, f in enumerate(RISE)]
    C.contact_sheet(tmp, f"{pre}_rise_game.png", 5, 640)
    for p in tmp:
        os.remove(p)
    # Every 5th frame from 40 to 100 from the play camera, kept apart for 3x crops (r53_ruingate_<tag>_seq_<f>.png).
    seq = os.path.join(QA, "_r53_seq")
    os.makedirs(seq, exist_ok=True)
    for f in range(40, 101, 5):
        shot(os.path.join(seq, f"r53_ruingate_{tag}_seq_{f:03d}.png"), "Open", f, game_cam(), 960, 640)
    C.log("r53 ruingate views", tag, "size", round(size, 2))


if __name__ == "__main__":
    main()
