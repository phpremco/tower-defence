"""Round 49F: the floating-part check for the five creatures of this round, on top of round 49E's
check_floating_parts.py (same island split, same attachment test), with three additions:

- islands a clip hides by bone scale (shed shards, phase crystals, the Shattered's split) are skipped in the poses
  where they are scaled down (their posed size under 25 % of the rest size): a collapsed island is not a loose part;
- `--clips` limits the check to some clips (the Shattered's Split and the Shardback's Shatter throw parts off on
  purpose);
- specks: islands of fewer than `--min-verts` vertices (single triangles left by the remesh at needle tips) are
  counted per object instead of ignored silently.
`--allow <prefix,...>` names islands by dominant bone that are separate by design (the Shardback's Shard_n
clusters, the Sealed One's seal stones and crown rings): they are listed apart with their gap, not as floating, and
every island is checked to be pure: an island that holds vertices led by an allowed bone and by any other bone
(a shed cluster fused to the body by the remesh) is listed under "mixed" (Unity could not hide it by scale).

Run: blender -b --factory-startup --python Tools/Blender/check_floating_49f.py -- --only Shardback
     [--clips Walk,Idle,Hit,Death,Strike] [--allow Shard_] [--tag after] [--frames 6]
Writes ArtSource/Previews/QA/r49f_floating_<name>_<tag>.json and prints a summary line.
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402

import check_floating_parts as F  # noqa: E402
import common as C  # noqa: E402


def island_sizes(objs, topo):
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    out = {}
    for o in objs:
        pts = F.evaluated_points(o, dg)
        for i, isl in enumerate(topo[o.name]):
            p = pts[isl]
            out[(o.name, i)] = float(np.linalg.norm(p.max(axis=0) - p.min(axis=0)))
    return out


def mixed_islands(obj, topo, allow):
    """Islands of `obj` whose vertices are led partly by an allowed bone and partly by another bone."""
    names = {g.index: g.name for g in obj.vertex_groups}
    lead = []
    for v in obj.data.vertices:
        best = max(v.groups, key=lambda g: g.weight, default=None)
        lead.append(names.get(best.group, "") if best is not None else "")
    out = []
    for i, isl in enumerate(topo):
        kinds = {any(lead[int(j)].startswith(a) for a in allow) for j in isl}
        if len(kinds) > 1:
            bones = sorted({lead[int(j)] for j in isl})
            allowed = [b_ for b_ in bones if any(b_.startswith(a) for a in allow)]
            out.append({"object": obj.name, "island": i, "verts": len(isl), "allowed_bones": allowed,
                        "other_bones": len(bones) - len(allowed)})
    return out


def run(name, thr, frames, min_verts, clips, allow):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    objs = [o for o in bpy.data.objects
            if o.type == "MESH" and o.parent == arm and not o.name.endswith(F.SKIP_SUFFIXES)]
    body = bpy.data.objects.get(name) or max(objs, key=lambda o: len(o.data.vertices))
    topo = {o.name: F.islands(o.data) for o in objs}
    bones = {(o.name, i): F.dominant_bone(o, isl) for o in objs for i, isl in enumerate(topo[o.name])}
    specks = {o.name: sum(1 for isl in topo[o.name] if len(isl) < min_verts) for o in objs}
    if thr is None:
        co = np.array([v.co for v in body.data.vertices])
        thr = float(max(co.max(axis=0) - co.min(axis=0))) * 0.004
    saved = arm.data.pose_position
    arm.data.pose_position = "REST"
    rest_size = island_sizes(objs, topo)
    arm.data.pose_position = saved
    poses = [("rest", None, 0)]
    for act in bpy.data.actions:
        if act.name.endswith(("_LOD", "_Full", "_Away")) or act.name.startswith(name + "_LOD"):
            continue
        if clips and act.name not in clips:
            continue
        start, end = int(act.frame_range[0]), int(act.frame_range[1])
        for f in sorted({int(round(v)) for v in np.linspace(start, end, frames)}):
            poses.append((act.name, act, f))
    worst, designed = {}, {}
    for label, act, f in poses:
        if act is None:
            arm.data.pose_position = "REST"
            found = F.check_pose(objs, topo, body, thr, min_verts)
            sizes = rest_size
            arm.data.pose_position = saved
        else:
            C.set_action(arm, act, f)
            found = F.check_pose(objs, topo, body, thr, min_verts)
            sizes = island_sizes(objs, topo)
        for o, i, n, ctr, g in found:
            if sizes[(o, i)] < 0.25 * max(rest_size[(o, i)], 1e-9):
                continue  # scaled away by the clip
            bone = bones[(o, i)] or ""
            store = designed if any(bone.startswith(a) for a in allow) else worst
            key = f"{o}#{i}"
            if key not in store or g > store[key]["gap"]:
                store[key] = {"object": o, "island": i, "verts": n, "gap": round(float(g), 5), "pose": f"{label}@{f}",
                              "bone": bone, "centre": [round(float(c), 4) for c in ctr]}
    mixed = [m for o in objs for m in mixed_islands(o, topo[o.name], allow)] if allow else []
    report = {"creature": name, "threshold": round(thr, 5), "poses_checked": len(poses), "mixed": mixed,
              "clips": clips or "all", "islands_per_object": {o.name: len(topo[o.name]) for o in objs},
              "specks_under_min_verts": specks, "floating": sorted(worst.values(), key=lambda w: -w["gap"]),
              "separate_by_design": sorted(designed.values(), key=lambda w: -w["gap"])}
    C.log("FLOATING49F", name, "thr", round(thr, 5), "poses", len(poses), "floating", len(worst), "designed",
          len(designed), "mixed", len(mixed), "specks", json.dumps(specks))
    return report


def main():
    args = C.cli_args()
    thr = float(args["thr"]) if args.get("thr") else None
    frames = int(args.get("frames", 6))
    min_verts = int(args.get("min-verts", 4))
    clips = [c for c in (args.get("clips") or "").split(",") if c]
    allow = [a for a in (args.get("allow") or "").split(",") if a]
    tag = args.get("tag") or "after"
    for name in args["only"].split(","):
        rep = run(name, thr, frames, min_verts, clips, allow)
        path = os.path.join(F.QA, f"r49f_floating_{name.lower()}_{tag}.json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=1)


if __name__ == "__main__":
    main()
