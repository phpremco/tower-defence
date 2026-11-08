"""Spencer's face inside the helmet: an automated clearance check and close-up sheets (build_mercer5.py, pass-4 fix).

check():  for every clip (or a chosen few) and every `step`-th frame, the evaluated face (Mercer_Face) against the
          evaluated body (Mercer: helmet, guards, gasket, neck, collar) and the visor glass (Mercer_Visor): BVH
          triangle overlap pairs and the smallest vertex clearance. Writes a JSON report; zero overlaps in every
          sampled frame is a pass. (The face is one connected sheet, mostly in the open: any part of it inside a solid
          crosses that solid's surface, so the overlap test also catches buried skin.)
sheet():  close-ups with the camera fixed to the head bone (so every cell frames the visor the same way), one row per
          clip frame, one column per angle (front, front-left, front-right, the game camera's three-quarter above).

Run on a saved hero:
  blender --background --factory-startup --python Tools/Blender/spencer_face_check.py -- --blend <Mercer.blend>
          [--out <dir>] [--prefix pass4fix_after] [--report <json>] [--step 2] [--nosheet] [--nocheck]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import common as C  # noqa: E402

FACE, VISOR, BODY, RIG = "Mercer_Face", "Mercer_Visor", "Mercer", "Mercer_Rig"
NEAR_HEAD = 0.32                # body polygons within this of the head bone's head are tested
SHEET_FRAMES = (("Idle", 10), ("Run", 4), ("Slash", 11), ("Shoot", 0), ("Hit", 6), ("Down", 20), ("Jet_Fly", 20),
                ("Point_Shout", 21), ("Startled", 12))
# (name, azimuth, elevation) in the head's frame: 0 = straight at the visor; the game camera looks down ~44 degrees.
ANGLES = (("front", 0, 0), ("front_left", -30, 4), ("front_right", 30, 4), ("game_above", -35, 44))


def set_frame(arm, act, frame):
    C.set_action(arm, act, frame)
    bpy.context.view_layer.update()


def world_mesh(obj, dg, centre=None):
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    mw = obj.matrix_world
    co = [mw @ v.co for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    if centre is not None:
        polys = [p for p in polys if (co[p[0]] - centre).length < NEAR_HEAD]
    return co, polys


def check_frame(arm, face, body, visor):
    dg = bpy.context.evaluated_depsgraph_get()
    head = arm.matrix_world @ arm.pose.bones["Head"].head
    fc, fp = world_mesh(face, dg)
    bc, bp = world_mesh(body, dg, head)
    vc, vp = world_mesh(visor, dg)
    tf = BVHTree.FromPolygons(fc, fp)
    tb = BVHTree.FromPolygons(bc, bp)
    tv = BVHTree.FromPolygons(vc, vp)
    pairs = len(tf.overlap(tb))
    visor_pairs = len(tf.overlap(tv))
    clearance = 1.0
    for p in fc:
        for tree in (tb, tv):
            loc, _, _, d = tree.find_nearest(p, 0.05)
            if loc is not None:
                clearance = min(clearance, d)
    return {"overlap": pairs, "visor_overlap": visor_pairs, "clearance_mm": round(clearance * 1000, 2)}


def check(clips=None, step=2):
    arm, face, body, visor = (bpy.data.objects[n] for n in (RIG, FACE, BODY, VISOR))
    acts = sorted(bpy.data.actions, key=lambda a: a.name)
    out = {"frames": 0, "fails": [], "worst_clearance_mm": 1000.0, "clips": {}}
    for act in acts:
        if clips and act.name not in clips:
            continue
        f0, f1 = (int(round(x)) for x in act.frame_range)
        worst = None
        for f in list(range(f0, f1 + 1, step)) + ([f1] if (f1 - f0) % step else []):
            set_frame(arm, act, f)
            r = check_frame(arm, face, body, visor)
            out["frames"] += 1
            if r["overlap"] or r["visor_overlap"]:
                out["fails"].append({"clip": act.name, "frame": f, **r})
            if worst is None or r["clearance_mm"] < worst["clearance_mm"]:
                worst = {"frame": f, **r}
        out["clips"][act.name] = worst
        out["worst_clearance_mm"] = min(out["worst_clearance_mm"], worst["clearance_mm"])
        C.log("face check", act.name, worst)
    out["pass"] = not out["fails"]
    return out


def head_camera(arm, az, el, dist, lens=85):
    """A camera in the head bone's frame: the visor points along the bone's -Z... resolved from the rest pose: in rest
    the head bone points up (+Y bone = world +Z) and the face looks at world -Y."""
    pb = arm.pose.bones["Head"]
    rest = arm.data.bones["Head"].matrix_local
    pose = arm.matrix_world @ pb.matrix
    to_world = pose @ rest.inverted()          # rest (armature space) -> posed world
    target = Vector((0.0, -0.03, 1.47))
    a, e = math.radians(az), math.radians(el)
    eye = target + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * dist
    up = Vector((0, 0, 1))
    cam_data = bpy.data.cameras.new("HeadCam")
    cam_data.lens = lens
    cam_data.clip_start = 0.01
    cam = C.link(bpy.data.objects.new("HeadCam", cam_data))
    fwd = (target - eye).normalized()
    right = fwd.cross(up).normalized()
    up2 = right.cross(fwd)
    m = Matrix.Identity(4)
    m.col[0][:3], m.col[1][:3], m.col[2][:3], m.col[3][:3] = right, up2, -fwd, eye
    cam.matrix_world = to_world @ m
    return cam


def sheet(blend, out, prefix, frames=SHEET_FRAMES, size=300):
    import spencer_previews5 as SP5
    import mercer_previews as MP
    rig, acts, _ = SP5.open_scene(blend)
    cells = []
    for clip, f in frames:
        if clip not in acts:
            continue
        MP.pose(rig, acts[clip], f, "none")
        bpy.context.view_layer.update()
        for name, az, el in ANGLES:
            cam = head_camera(rig.arm, az, el, 0.62)
            cells.append(C.render(cam, os.path.join(out, f"_cell_{clip}_{f}_{name}.png"), size, size, samples=20))
            bpy.data.objects.remove(cam)
    path = os.path.join(out, f"{prefix}_sheet.png")
    C.contact_sheet(cells, path, len(ANGLES), size)
    for p in cells:
        os.remove(p)
    # Large single close-ups (the same framings as spencer_previews5's face shots, at the Idle frame).
    MP.pose(rig, acts["Idle"], 10, "none")
    bpy.context.view_layer.update()
    for name, az, el in ANGLES:
        cam = head_camera(rig.arm, az, el, 0.6)
        C.render(cam, os.path.join(out, f"{prefix}_{name}.png"), 900, 900)
        bpy.data.objects.remove(cam)
    return path


def main():
    args = C.cli_args()
    blend = args.get("blend") or os.path.join(C.BLEND, "Mercer.blend")
    out = args.get("out") or os.path.join(C.REPO, "ArtSource", "Previews", "Spencer")
    prefix = args.get("prefix", "pass4fix_after")
    os.makedirs(out, exist_ok=True)
    if not args.get("nocheck"):
        bpy.ops.wm.open_mainfile(filepath=blend)
        rep = check(step=int(args.get("step", 2)))
        path = args.get("report") or os.path.join(out, prefix + "_check.json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=1)
        C.log("face check:", "PASS" if rep["pass"] else "FAIL", "frames", rep["frames"], "fails", len(rep["fails"]),
              "worst clearance mm", rep["worst_clearance_mm"], "->", path)
    if not args.get("nosheet"):
        sheet(blend, out, prefix)


if __name__ == "__main__":
    main()
