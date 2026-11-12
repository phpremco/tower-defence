"""Round 49G: the strict loose-part check (Remco on the 49F Shardback: "you can see the spikes floating!!" while
check_floating_49f.py reported 0 floating).

Why the 49F check passed floating spikes (see Docs/STATUS.md, round 49G):
- it chained attachment: an island counted as attached when it touched the body OR any island already attached, so a
  spike resting on a lip shard, a crystal cluster or an eye lid passed even when it hovered over the carapace;
- the threshold scaled with the body (0.4 % of its largest size, 3..9 mm) and the "separate by design" list (the
  Shard_n clusters, 1..2 cm over their craters) was never counted as floating;
- it sampled 6 frames per clip of the .blend, not every frame of what Unity imports.

This check, per creature, imports the exported <Name>.fbx and <Name>_LOD1.fbx (what Unity gets) and, in the rest pose
and at every frame of every clip (`--step` to thin), measures for every mesh island other than the body's main island
the true surface-to-surface distance to that main island only (no chaining through other islands):
- the nearest point on the body's triangles from every island vertex, and from every body vertex near the island the
  nearest point on the island's triangles;
- 0 when the two surfaces cross (an island vertex inside the closed body, or a body vertex inside the island).
An island whose distance is over `--thr` (default 0.5 cm in game metres, never looser than 0.5 cm in model units) is
floating. Islands a clip scales below 25 % of their rest size (shedding, phases, the split) are skipped in that pose.
`--exempt <prefix,...>` lists islands by dominant bone that may hover by design (none by default; reported apart).
`--burst <clip,...>` names clips that throw parts off on purpose (the Shardback's Shatter, the Shattered's Split,
Rejoin, PhaseBreak1/2, Death): an island floating only in those clips is listed under "burst_only", not "floating".
Every entry carries the clips it floats in with the worst gap per clip.

Run: blender -b --factory-startup --python Tools/Blender/check_floating_49g.py -- --only Shardback,Bulwark
     [--tag after] [--step 1] [--thr 0.005] [--lods 0,1] [--dir ArtSource/Exports/_backup_r49/{name}_49G]
     [--burst Shatter] [--exempt Shell_]
Writes ArtSource/Previews/QA/r49g_floating_<name>_<tag>.json and prints one summary line per creature and LOD.
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

QA = F.QA


def unity_scale(folder, name):
    try:
        with open(os.path.join(folder, name + "_report.json"), encoding="utf-8") as fh:
            rep = json.load(fh)
    except OSError:
        return 1.0
    for v in rep.values():
        if isinstance(v, dict) and "unity_model_scale" in v:
            return float(v["unity_model_scale"])
    return float(rep.get("unity_scale", 1.0))


def import_fbx(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    return arm, meshes


def bvh(points, polys):
    return BVHTree.FromPolygons([Vector(p) for p in points], polys) if polys else None


def island_polys(mesh, verts):
    keep = np.zeros(len(mesh.vertices), dtype=bool)
    keep[verts] = True
    return [tuple(p.vertices) for p in mesh.polygons if keep[p.vertices[0]] and all(keep[i] for i in p.vertices)]


RAYS = [Vector(d).normalized() for d in ((0.31, 0.17, 0.935), (-0.6, 0.42, -0.68), (0.83, -0.51, 0.22),
                                          (-0.2, -0.9, 0.38), (0.45, 0.72, -0.53), (-0.77, -0.12, -0.62),
                                          (0.05, 0.6, 0.8))]


def _parity(tree, p, d):
    hits, origin = 0, Vector(p)
    for _ in range(64):
        loc = tree.ray_cast(origin, d)[0]
        if loc is None:
            break
        hits += 1
        origin = loc + d * 1e-5
    return hits % 2 == 1


def inside(tree, p, normal_hit):
    """`p` lies inside the surface `tree`: the nearest face looks away from it and most of seven rays cross the
    surface an odd number of times. A majority vote (not "all rays") because a body can have small holes (the
    Shattered's eye dots are cut out of its core): a point deep in the core must not read as outside through one."""
    loc, nrm, _, _ = normal_hit
    if (p - loc).dot(nrm) >= 0:
        return False
    return sum(_parity(tree, p, d) for d in RAYS) >= 4


def surface_gap(ipts, itree, bpts_near, btree, early=0.0):
    """Surface distance between an island and the body (0 when they cross or when the island lies inside the body).
    Insideness is a ray-parity majority on the island's centre and a few of its vertices: the nearest-face normal is
    no guide deep inside a concave body (the Shattered's phase-1 crystals sit 0.6 m deep in its core)."""
    best = 1e9
    for q in ipts:
        hit = btree.find_nearest(Vector(q))
        if hit[0] is not None and hit[3] < best:
            best = hit[3]
    probes = [ipts.mean(axis=0)] + list(ipts[np.linspace(0, len(ipts) - 1, min(8, len(ipts))).astype(int)])
    for q in probes:
        if sum(_parity(btree, Vector(q), d) for d in RAYS) >= 4:
            return 0.0
    if itree is not None:
        for q in bpts_near:
            p = Vector(q)
            hit = itree.find_nearest(p)
            if hit[0] is None:
                continue
            if hit[3] < best:
                best = hit[3]
            if inside(itree, p, hit):
                return 0.0
    return best


def check_lod(path, name, thr, step, exempt, burst=()):
    arm, meshes = import_fbx(path)
    meshes = [o for o in meshes if o.find_armature() == arm or o.parent == arm]
    body = max((o for o in meshes if o.name.split(".")[0] in (name, name + "_LOD1")), default=None,
               key=lambda o: len(o.data.vertices)) or max(meshes, key=lambda o: len(o.data.vertices))
    topo = {o.name: F.islands(o.data) for o in meshes}
    main = max(range(len(topo[body.name])), key=lambda i: len(topo[body.name][i]))
    body_idx = topo[body.name][main]
    body_polys = island_polys(body.data, body_idx)
    polys = {(o.name, i): island_polys(o.data, isl) for o in meshes for i, isl in enumerate(topo[o.name])
             if not (o == body and i == main)}
    bones = {(o.name, i): F.dominant_bone(o, isl) or "" for o in meshes for i, isl in enumerate(topo[o.name])}
    poses = [("rest", None, 0)]
    for act in bpy.data.actions:
        s, e = int(round(act.frame_range[0])), int(round(act.frame_range[1]))
        for f in range(s, e + 1, step):
            poses.append((act.name.split("|")[-1], act, f))
        if (e - s) % step:
            poses.append((act.name.split("|")[-1], act, e))
    rest_size = None
    worst, exempted = {}, {}
    for label, act, f in poses:
        if act is None:
            arm.data.pose_position = "REST"
        else:
            arm.data.pose_position = "POSE"
            C.set_action(arm, act, f)
        dg = bpy.context.evaluated_depsgraph_get()
        dg.update()
        pts = {o.name: F.evaluated_points(o, dg) for o in meshes}
        bp = pts[body.name]
        btree = bvh(bp, body_polys)
        bmain = bp[body_idx]
        sizes = {}
        for (oname, i), pl in polys.items():
            isl = topo[oname][i]
            ip = pts[oname][isl]
            lo, hi = ip.min(axis=0), ip.max(axis=0)
            sizes[(oname, i)] = float(np.linalg.norm(hi - lo))
        if rest_size is None:
            rest_size = dict(sizes)
        for (oname, i), pl in polys.items():
            if sizes[(oname, i)] < 0.25 * max(rest_size[(oname, i)], 1e-9):
                continue
            isl = topo[oname][i]
            ip = pts[oname][isl]
            lo, hi = ip.min(axis=0) - 0.05, ip.max(axis=0) + 0.05
            near = bmain[np.all((bmain >= lo) & (bmain <= hi), axis=1)]
            itree = bvh(pts[oname], pl)
            g = surface_gap(ip, itree, near, btree)
            if g <= thr:
                continue
            bone = bones[(oname, i)]
            store = exempted if any(bone.startswith(x) for x in exempt) else worst
            key = f"{oname}#{i}"
            if key not in store:
                store[key] = {"object": oname, "island": i, "verts": len(isl), "gap": 0.0, "bone": bone,
                              "poses_floating": 0, "clips": {}}
            e = store[key]
            if g > e["gap"]:
                e.update(gap=round(float(g), 5), pose=f"{label}@{f}", size=round(sizes[(oname, i)], 4),
                         centre=[round(float(c), 4) for c in ip.mean(axis=0)])
            e["poses_floating"] += 1
            e["clips"][label] = max(e["clips"].get(label, 0.0), round(float(g), 4))
    arm.data.pose_position = "POSE"
    normal = [w for w in worst.values() if any(c not in burst for c in w["clips"])]
    burst_only = [w for w in worst.values() if all(c in burst for c in w["clips"])]
    return {"file": os.path.relpath(path, C.REPO), "body": body.name, "poses": len(poses),
            "islands": {o.name: len(topo[o.name]) for o in meshes},
            "floating": sorted(normal, key=lambda w: -w["gap"]),
            "burst_only": sorted(burst_only, key=lambda w: -w["gap"]),
            "exempted": sorted(exempted.values(), key=lambda w: -w["gap"])}


def main():
    args = C.cli_args()
    step = int(args.get("step", 1))
    tag = args.get("tag") or "after"
    exempt = [x for x in (args.get("exempt") or "").split(",") if x]
    lods = [int(x) for x in (args.get("lods") or "0,1").split(",")]
    burst = [x for x in (args.get("burst") or "").split(",") if x]
    for name in args["only"].split(","):
        if not name:
            continue
        folder = os.path.join(C.REPO, args["dir"].format(name=name)) if args.get("dir") else os.path.join(C.EXPORTS, name)
        scale = unity_scale(folder, name)
        thr = float(args["thr"]) if args.get("thr") else 0.005 * min(1.0, 1.0 / scale)
        rep = {"creature": name, "threshold_model_units": round(thr, 5), "unity_scale": scale, "step": step,
               "burst_clips": burst, "exempt_bones": exempt, "lods": {}}
        for lod in lods:
            path = os.path.join(folder, name + (".fbx" if lod == 0 else "_LOD1.fbx"))
            if not os.path.exists(path):
                continue
            r = check_lod(path, name, thr, step, exempt, burst)
            rep["lods"][f"LOD{lod}"] = r
            C.log("FLOATING49G", name, f"LOD{lod}", "thr", round(thr, 5), "poses", r["poses"], "floating",
                  len(r["floating"]), "worst", r["floating"][0]["gap"] if r["floating"] else 0.0,
                  "burst_only", len(r["burst_only"]), "exempted", len(r["exempted"]))
        with open(os.path.join(QA, f"r49g_floating_{name.lower()}_{tag}.json"), "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=1)


if __name__ == "__main__":
    main()
