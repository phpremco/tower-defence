"""Round 49F: do teeth cut through lips? (Remco on the Shattered: "the teeth when the mouth is closed seem to go
through the lips"; the Sealed One's new maw.)

Opens ArtSource/Blend/<Name>.blend read-only. Every island of the teeth part (<Name>_Teeth) is a tooth: its root
end is the end that sits inside the body at rest, its tip end the other one. For each clip, at the frames where the
jaw is most closed (the three smallest jaw turns) and at the rest pose, the body is evaluated, and every tooth's tip
third (the outer 35 % of its length; roots are sunk deep, the crystal teeth up to 60 %) is tested against the body surface: a vertex is inside when the nearest body
surface faces away from it (its normal points away; ray parity is unreliable round the Sealed One's open face-plate
shells). A tip vertex inside the body is a tooth through a lip or a gum. `--crystal-teeth <part prefix>` adds the body's
own crystal teeth islands (the Shattered's Jaw `Tooth` shards; they are islands of the body mesh, tested against the
body without themselves).

Run: blender -b --factory-startup --python Tools/Blender/check_teeth_49f.py -- --only Shattered,SealedOne [--tag after]
     [--crystal-teeth Jaw] [--from ArtSource/Exports/_backup_r49]   (the round-49D .blend files, for the before)
Writes ArtSource/Previews/QA/r49f_teeth_<name>_<tag>.json and prints a summary line per creature.
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import check_floating_parts as F  # noqa: E402
import common as C  # noqa: E402

TIP_SHARE = 0.35


def inside(tree, p):
    """The nearest surface of the body faces away from p (p lies under it)."""
    loc, normal, _, _ = tree.find_nearest(p)
    return loc is not None and (p - loc).dot(normal) < 0


def body_tree(obj, pts, skip=None):
    keep = np.ones(len(pts), dtype=bool)
    if skip is not None:
        keep[skip] = False
    polys = [tuple(p.vertices) for p in obj.data.polygons if all(keep[i] for i in p.vertices)]
    return BVHTree.FromPolygons([Vector(p) for p in pts], polys)


def tooth_axes(pts, isl, tree):
    """(root, tip) points of a tooth island: the principal axis' two ends; the root is the end with more vertices
    inside the body."""
    p = pts[isl]
    c = p.mean(axis=0)
    u, s, vt = np.linalg.svd(p - c, full_matrices=False)
    ax = vt[0]
    t = (p - c) @ ax
    lo, hi = p[t <= np.percentile(t, 15)], p[t >= np.percentile(t, 85)]
    ins_lo = sum(inside(tree, Vector(q)) for q in lo)
    ins_hi = sum(inside(tree, Vector(q)) for q in hi)
    if ins_hi > ins_lo:
        ax = -ax
    t = (p - c) @ ax
    return c + ax * t.min(), c + ax * t.max()


def crystal_island(obj, isl):
    """True when most faces of the island use a crystal material (the body's other material is stone/chitin)."""
    verts = set(int(i) for i in isl)
    mats = [obj.material_slots[p.material_index].name if obj.material_slots else ""
            for p in obj.data.polygons if p.vertices[0] in verts]
    return bool(mats) and sum("Crystal" in m for m in mats) > len(mats) / 2


def jaw_frames(arm, act, bone, count=3):
    turns = []
    for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
        C.set_action(arm, act, f)
        turns.append((arm.pose.bones[bone].matrix_basis.to_quaternion().angle, f))
    return [f for _, f in sorted(turns)[:count]]


def blend_path(name, backup):
    return os.path.join(backup, name + "_49F", name + ".blend") if backup else os.path.join(C.BLEND, name + ".blend")


def run(name, crystal_prefix, backup=None):
    bpy.ops.wm.open_mainfile(filepath=blend_path(name, backup))
    arm = bpy.data.objects[name + "_Rig"]
    body = bpy.data.objects[name]
    teeth = bpy.data.objects.get(name + "_Teeth")
    jaw = "Jaw" if "Jaw" in arm.pose.bones else None
    subjects = []
    if teeth is not None:
        subjects += [(teeth, isl) for isl in F.islands(teeth.data) if len(isl) >= 8]
    body_isl = F.islands(body.data)
    crystal = []
    if crystal_prefix:
        crystal = [isl for isl in body_isl if (F.dominant_bone(body, isl) or "") == crystal_prefix
                   and crystal_island(body, isl)]
        subjects += [(body, isl) for isl in crystal]
    skip = np.concatenate(crystal) if crystal else None
    # Root and tip of every tooth at rest.
    arm.data.pose_position = "REST"
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    pts = {o.name: F.evaluated_points(o, dg) for o in {body, teeth} if o is not None}
    tree = body_tree(body, pts[body.name], skip)
    ends = [tooth_axes(pts[o.name], isl, tree) for o, isl in subjects]
    arm.data.pose_position = "POSE"
    poses = [("rest", None, 0)]
    for act in bpy.data.actions:
        if act.name.endswith(("_LOD", "_Full", "_Away")):
            continue
        for f in (jaw_frames(arm, act, jaw) if jaw else [0]):
            poses.append((act.name, act, f))
    hits, worst = {}, 0
    for label, act, f in poses:
        if act is None:
            arm.data.pose_position = "REST"
        else:
            C.set_action(arm, act, f)
        dg = bpy.context.evaluated_depsgraph_get()
        dg.update()
        pts = {o.name: F.evaluated_points(o, dg) for o in {body, teeth} if o is not None}
        tree = body_tree(body, pts[body.name], skip)
        arm.data.pose_position = "POSE"
        for k, ((o, isl), (root, tip)) in enumerate(zip(subjects, ends)):
            # A vertex's place along the tooth comes from the rest pose (the tooth is rigid on its bone).
            p = pts[o.name][isl]
            ax = tip - root
            t = (rest_positions[o.name][isl] - root) @ ax / max(ax @ ax, 1e-12)
            tip_half = p[t >= 1.0 - TIP_SHARE]
            n_in = sum(inside(tree, Vector(q)) for q in tip_half[:: max(1, len(tip_half) // 24)])
            if n_in:
                key = f"{o.name}#{k}"
                hits.setdefault(key, []).append(f"{label}@{f}:{n_in}")
                worst = max(worst, n_in)
    report = {"creature": name, "teeth_checked": len(subjects), "poses": len(poses),
              "teeth_through_lips": len(hits), "where": hits}
    C.log("TEETH49F", name, "teeth", len(subjects), "poses", len(poses), "through", len(hits),
          json.dumps(dict(list(hits.items())[:6])))
    return report


rest_positions = {}


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    backup = args.get("from")
    if backup and not os.path.isabs(backup):
        backup = os.path.join(C.REPO, backup)
    for name in args["only"].split(","):
        bpy.ops.wm.open_mainfile(filepath=blend_path(name, backup))
        arm = bpy.data.objects[name + "_Rig"]
        arm.data.pose_position = "REST"
        dg = bpy.context.evaluated_depsgraph_get()
        dg.update()
        rest_positions.clear()
        for o in (bpy.data.objects.get(name), bpy.data.objects.get(name + "_Teeth")):
            if o is not None:
                rest_positions[o.name] = F.evaluated_points(o, dg)
        rep = run(name, args.get("crystal-teeth") if name == "Shattered" else None, backup)
        with open(os.path.join(F.QA, f"r49f_teeth_{name.lower()}_{tag}.json"), "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=1)


if __name__ == "__main__":
    main()
