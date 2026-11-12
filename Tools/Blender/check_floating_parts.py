"""Round 49E: finds loose geometry on a skinned creature (Remco: "some spikes are no longer attached to the body",
"the head has loose floating parts").

Opens ArtSource/Blend/<Name>.blend and, for the full-detail rig only (LOD hidden), splits every skinned mesh (the body
and its round-45/46 extra parts such as Face, Claws, Eyes, Teeth) into connected islands. The body's largest island is
the anchor. An island counts as attached when its nearest vertex lies within `--thr` (model units; default scaled from
the body size) of the anchor or of another attached island; the rest are reported as floating, with the part name of
the object, the island's size, centre, and the gap. This runs in the rest pose and at several frames of every clip, so
a spike that sits fine at rest but drifts off on its own bone during a clip is caught too.

Run: blender -b --factory-startup --python Tools/Blender/check_floating_parts.py -- --only CinderMatriarch,Knitback
     [--thr 0.004] [--frames 6] [--json <path>] [--min-verts 4]
Writes ArtSource/Previews/QA/r49e_floating_<name>.json (or --json) and prints a summary line per creature.
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import common as C  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
SKIP_SUFFIXES = ("_High", "_Parts", "_Block")


def islands(mesh):
    """Connected vertex islands of a mesh (union-find over its edges), as a list of index arrays."""
    n = len(mesh.vertices)
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", edges)
    edges = edges.reshape(-1, 2)
    parent = np.arange(n)

    def find(i):
        root = i
        while parent[root] != root:
            root = parent[root]
        while parent[i] != root:
            parent[i], i = root, parent[i]
        return root

    for a, b in edges:
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb
    roots = np.array([find(i) for i in range(n)])
    order = np.argsort(roots, kind="stable")
    split = np.nonzero(np.diff(roots[order]))[0] + 1
    return [grp for grp in np.split(order, split) if len(grp)]


def tri_bvh(points, mesh, verts):
    """A BVH of the triangles whose vertices all belong to `verts` (positions from `points`)."""
    keep = np.zeros(len(points), dtype=bool)
    keep[verts] = True
    polys = [tuple(p.vertices) for p in mesh.polygons if all(keep[i] for i in p.vertices)]
    if not polys:
        return None
    return BVHTree.FromPolygons([Vector(p) for p in points], polys)


def evaluated_points(obj, dg):
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    co = np.empty(len(me.vertices) * 3)
    me.vertices.foreach_get("co", co)
    ev.to_mesh_clear()
    co = co.reshape(-1, 3)
    m = np.array(obj.matrix_world)
    return co @ m[:3, :3].T + m[:3, 3]


def gap(points, idx, tree, sample=160):
    """The smallest signed distance from an island's vertices to a closed surface (a BVH): 0 when a vertex lies inside
    it (sampled for big islands)."""
    if len(idx) > sample:
        idx = idx[np.linspace(0, len(idx) - 1, sample).astype(int)]
    best = 1e9
    for i in idx:
        p = Vector(points[i])
        hit = tree.find_nearest(p)
        if hit[0] is None:
            continue
        if hit[3] > thr_inside_probe and (p - hit[0]).dot(hit[1]) < 0 and inside(tree, p):
            return 0.0
        best = min(best, hit[3])
    return best


thr_inside_probe = 0.0
RAY_DIRS = (Vector((0.31, 0.17, 0.935)).normalized(), Vector((-0.6, 0.42, -0.68)).normalized())


def inside(tree, p):
    """Ray parity in two directions (both odd): the point lies inside the closed surface."""
    for d in RAY_DIRS:
        hits, origin = 0, Vector(p)
        for _ in range(64):
            loc, _, _, _ = tree.ray_cast(origin, d)
            if loc is None:
                break
            hits += 1
            origin = loc + d * 1e-5
        if hits % 2 == 0:
            return False
    return True


def sheathed(island_tree, lo, hi, anchors, thr):
    """True when an anchor's vertices poke into (or touch) the island: a cap or claw sheath that encloses the end of
    a spike has all its own vertices outside the spike, yet it is attached."""
    for _, apts in anchors:
        near = apts[np.all((apts >= lo - thr) & (apts <= hi + thr), axis=1)]
        if len(near) and gap(near, np.arange(len(near)), island_tree) <= thr:
            return True
    return False


def check_pose(objs, topo, body, thr, min_verts):
    """Floating islands in the current pose: [(object, island index, verts, centre, gap)]."""
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    pts = {o.name: evaluated_points(o, dg) for o in objs}
    body_isl = topo[body.name]
    main = max(range(len(body_isl)), key=lambda i: len(body_isl[i]))
    anchors = [(tri_bvh(pts[body.name], body.data, body_isl[main]), pts[body.name][body_isl[main]])]
    pending = [(o, i) for o in objs for i, isl in enumerate(topo[o.name])
               if not (o == body and i == main) and len(isl) >= min_verts]
    attached_any = True
    gaps = {}
    while pending and attached_any:
        attached_any = False
        rest = []
        for o, i in pending:
            isl = topo[o.name][i]
            d = min(gap(pts[o.name], isl, t) for t, _ in anchors if t is not None)
            gaps[(o.name, i)] = d
            tree = None
            if d > thr:
                tree = tri_bvh(pts[o.name], o.data, isl)
                ip = pts[o.name][isl]
                if tree is not None and sheathed(tree, ip.min(axis=0), ip.max(axis=0), anchors, thr):
                    d = 0.0
            if d <= thr:
                tree = tree or tri_bvh(pts[o.name], o.data, isl)
                anchors.append((tree, pts[o.name][isl]))
                attached_any = True
            else:
                rest.append((o, i))
        pending = rest
    out = []
    for o, i in pending:
        isl = topo[o.name][i]
        out.append((o.name, i, len(isl), pts[o.name][isl].mean(axis=0), gaps[(o.name, i)]))
    return out


def dominant_bone(obj, idx):
    """The vertex group with the most weight over an island (which bone carries it)."""
    acc = {}
    names = {g.index: g.name for g in obj.vertex_groups}
    for i in idx[:: max(1, len(idx) // 40)]:
        for g in obj.data.vertices[int(i)].groups:
            acc[g.group] = acc.get(g.group, 0.0) + g.weight
    return names.get(max(acc, key=acc.get)) if acc else None


def run(name, thr, frames, min_verts):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    objs = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.name.endswith(SKIP_SUFFIXES)]
    body = bpy.data.objects.get(name) or max(objs, key=lambda o: len(o.data.vertices))
    topo = {o.name: islands(o.data) for o in objs}
    if thr is None:
        lo = np.min(np.array([v.co for v in body.data.vertices]), axis=0)
        hi = np.max(np.array([v.co for v in body.data.vertices]), axis=0)
        thr = float(max(hi - lo)) * 0.004
    report = {"creature": name, "threshold": round(thr, 5), "objects": {o.name: len(topo[o.name]) for o in objs},
              "poses": {}}
    poses = [("rest", None, 0)]
    for act in bpy.data.actions:
        if act.name.endswith(("_LOD", "_Full", "_Away")) or act.name.startswith(name + "_LOD"):
            continue
        start, end = int(act.frame_range[0]), int(act.frame_range[1])
        for f in sorted({int(round(v)) for v in np.linspace(start, end, frames)}):
            poses.append((act.name, act, f))
    worst = {}
    for label, act, f in poses:
        if act is None:
            saved = arm.data.pose_position
            arm.data.pose_position = "REST"
            found = check_pose(objs, topo, body, thr, min_verts)
            arm.data.pose_position = saved
        else:
            C.set_action(arm, act, f)
            found = check_pose(objs, topo, body, thr, min_verts)
        if found:
            report["poses"][f"{label}@{f}"] = [{"object": o, "island": i, "verts": n, "centre": [round(float(c), 4) for c in ctr],
                                               "gap": round(float(g), 5)} for o, i, n, ctr, g in found]
        for o, i, n, ctr, g in found:
            key = f"{o}#{i}"
            if key not in worst or g > worst[key]["gap"]:
                worst[key] = {"object": o, "island": i, "verts": n, "gap": round(float(g), 5), "pose": f"{label}@{f}",
                              "bone": dominant_bone(bpy.data.objects[o], topo[o][i]),
                              "centre": [round(float(c), 4) for c in ctr]}
    report["floating"] = sorted(worst.values(), key=lambda w: -w["gap"])
    report["poses_checked"] = len(poses)
    report["rest_floating"] = report["poses"].get("rest@0", [])
    C.log("FLOATING", name, "thr", round(thr, 5), "poses", len(poses), "islands", len(worst),
          json.dumps(report["floating"][:12]))
    return report


def main():
    args = C.cli_args()
    thr = float(args["thr"]) if args.get("thr") else None
    frames = int(args.get("frames", 6))
    min_verts = int(args.get("min-verts", 4))
    for name in args["only"].split(","):
        rep = run(name, thr, frames, min_verts)
        path = args.get("json") or os.path.join(QA, f"r49e_floating_{name.lower()}.json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=1)


if __name__ == "__main__":
    main()
