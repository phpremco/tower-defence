"""Round 56: delete the loose decimation specks from a creature's saved body meshes and export <Name>.fbx /
<Name>_LOD1.fbx again with the same rigs, parts and clips (PackAlpha: 3-vertex triangles near the neck that the strict
check finds 0.8-3 cm off the body in every pose, worst at the Howl peak).

A speck is an island of the body (or its LOD1) under --min vertices (default 8) that stands more than --gap (default
3 mm, model units) off the body's main island in the rest pose. Hundreds of other small islands lie ON the skin
(coplanar crumbs of the decimation, fur-lock tips): they are kept, so the silhouette does not change. Only the specks'
vertices go: nothing else moves, the UV layout and every map stay valid, no re-bake. The detail parts are untouched.
The clips are exported in the report's `actions` order (LOD1: the `<clip>_LOD` actions). A `round56_specks` block is
added to the report.

Run: blender -b --factory-startup --python Tools/Blender/r56_drop_specks.py -- --only PackAlpha [--min 8] [--gap 0.003]
     [--parts]   (the detail parts' specks, measured against the body, instead of the body's)
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import boss_mech_kit as BMK  # noqa: E402
import check_floating_parts as F  # noqa: E402
import common as C  # noqa: E402

PARTS = ("Face", "Claws", "Eyes", "Teeth")


def drop_loose(obj, min_verts, gap, body=None):
    """Deletes the islands of `obj` under `min_verts` vertices that stand over `gap` off its largest island (rest
    pose), or, for a detail part, off the `body` mesh (`--parts`: the Skyray's Face kept two 3-vertex tooth crumbs
    1.2 cm under the head). Returns [(vertices, gap, centre)] of what went."""
    me = obj.data
    isl = sorted(F.islands(me), key=len, reverse=True)
    verts = [v.co.copy() for v in me.vertices]
    if body is None:
        main = set(int(i) for i in isl[0])
        polys = [list(p.vertices) for p in me.polygons if all(i in main for i in p.vertices)]
        tree = BVHTree.FromPolygons(verts, polys)
        isl = isl[1:]
    else:
        tree = BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],
                                    [list(p.vertices) for p in body.data.polygons])
    doomed, out = set(), []
    for island in isl:
        if len(island) >= min_verts:
            continue
        d = min((tree.find_nearest(verts[int(i)])[3] for i in island), default=0.0)
        if d > gap:
            doomed.update(int(i) for i in island)
            c = sum((verts[int(i)] for i in island), Vector()) / len(island)
            out.append((len(island), round(d, 4), [round(v, 3) for v in c]))
    if doomed:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in sorted(doomed)], context="VERTS")
        bm.to_mesh(me)
        bm.free()
    C.log(obj.name, "dropped loose specks", out)
    return out


def clean(name, min_verts, gap, parts=False):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm, lod_arm = bpy.data.objects[name + "_Rig"], bpy.data.objects[name + "_LOD1_Rig"]
    full = [bpy.data.objects[name]] + [o for o in (bpy.data.objects.get(f"{name}_{p}") for p in PARTS) if o]
    lod = [bpy.data.objects[name + "_LOD1"]] + [o for o in (bpy.data.objects.get(f"{name}_{p}_LOD1") for p in PARTS) if o]
    if parts:
        dropped = {o.name: drop_loose(o, min_verts, gap, full[0]) for o in full[1:]}
        dropped.update({o.name: drop_loose(o, min_verts, gap, lod[0]) for o in lod[1:]})
    else:
        dropped = {o.name: drop_loose(o, min_verts, gap) for o in (full[0], lod[0])}
    path = os.path.join(C.EXPORTS, name, name + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    order = list(rep["actions"])
    folder = os.path.join(C.EXPORTS, name)
    BMK.export_rig(os.path.join(folder, name + ".fbx"), arm, full, {n: bpy.data.actions[n] for n in order})
    BMK.export_rig(os.path.join(folder, name + "_LOD1.fbx"), lod_arm, lod, {n: bpy.data.actions[n + "_LOD"] for n in order})
    rep["round56_specks" + ("_parts" if parts else "")] = {"min_vertices": min_verts, "min_gap_model_units": gap,
                             "islands_dropped": {k: len(v) for k, v in dropped.items()},
                             "note": "loose decimation specks deleted from the body; nothing else changed (maps, UVs)"}
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.save_blend(name)
    C.log("SPECKS", name, json.dumps(dropped))


def main():
    args = C.cli_args()
    for name in (args.get("only") or "").split(","):
        if name:
            clean(name, int(args.get("min") or 8), float(args.get("gap") or 0.003), bool(args.get("parts")))


if __name__ == "__main__":
    main()
