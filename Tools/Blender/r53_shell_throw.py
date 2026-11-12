"""Round 53: which shell plates each Shattered clip throws, measured on the exported FBX (what Unity imports).

For <Name>.fbx and <Name>_LOD1.fbx: every mesh island that is wholly weighted to one Shell_n bone is a plate. Per clip
and frame the plate's size (largest distance from its centre) and its centre relative to the Pelvis bone are taken;
a plate counts as thrown in a clip when it shrinks below 25 % of its rest size or its centre moves more than 0.4 m (in
model units) from where it rides at the clip's first frame. Prints per rig the plates per bone (count and rest centre)
and per clip the bones thrown, in order, with the frame each one goes; writes
ArtSource/Previews/QA/r53_shell_throw_<tag>.json.

Run: blender -b --factory-startup --python Tools/Blender/r53_shell_throw.py -- [--dir ArtSource/Exports/Shattered]
     [--tag after] [--clips PhaseBreak1,PhaseBreak2,Split,Rejoin,Death]
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402

import check_floating_parts as F  # noqa: E402
import common as C  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
NAME = "Shattered"


def plates(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for isl in F.islands(obj.data):
        bones = set()
        for i in isl:
            for g in obj.data.vertices[int(i)].groups:
                if g.weight > 1e-4:
                    bones.add(names[g.group])
        if len(bones) == 1 and next(iter(bones)).startswith("Shell_"):
            out.append((next(iter(bones)), np.asarray(isl)))
    return out


def deformed(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    co = np.empty(len(me.vertices) * 3)
    me.vertices.foreach_get("co", co)
    ev.to_mesh_clear()
    m = np.array(obj.matrix_world)
    co = co.reshape(-1, 3)
    return co @ m[:3, :3].T + m[:3, 3]


def run(path, clips):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    body = max((o for o in bpy.data.objects if o.type == "MESH" and any(g.name.startswith("Shell_") for g in o.vertex_groups)
                and o.find_armature() == arm), key=lambda o: len(o.data.vertices))
    found = plates(body)
    result = {"mesh": body.name, "plates": {}, "clips": {}}
    for o in bpy.data.objects:
        if o.type == "ARMATURE" and o.animation_data:
            o.animation_data.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis.identity()
    bpy.context.view_layer.update()
    rest = deformed(body)
    rest_size = [float(np.max(np.linalg.norm(rest[isl] - rest[isl].mean(axis=0), axis=1))) for _, isl in found]
    for bone, isl in found:
        result["plates"].setdefault(bone, []).append([round(float(v), 2) for v in rest[isl].mean(axis=0)])
    acts = {a.name.split("|")[-1]: a for a in bpy.data.actions}
    for clip in clips:
        act = acts.get(clip)
        if act is None:
            continue
        f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
        start = None
        gone = {}
        for f in range(f0, f1 + 1):
            C.set_action(arm, act, f)
            bpy.context.view_layer.update()
            co = deformed(body)
            pelvis = np.array(arm.matrix_world @ arm.pose.bones["Pelvis"].head)
            state = []
            for k, (bone, isl) in enumerate(found):
                p = co[isl]
                c = p.mean(axis=0)
                size = float(np.max(np.linalg.norm(p - c, axis=1)))
                state.append((c - pelvis, size))
            if start is None:
                start = state
            for k, (bone, isl) in enumerate(found):
                rel, size = state[k]
                moved = float(np.linalg.norm(rel - start[k][0]))
                if k not in gone and start[k][1] >= 0.25 * rest_size[k] and (size < 0.25 * start[k][1] or moved > 0.4):
                    gone[k] = f
        thrown = sorted(gone.items(), key=lambda kv: kv[1])
        result["clips"][clip] = [{"bone": found[k][0], "frame": f, "centre": result["plates"][found[k][0]][0]}
                                 for k, f in thrown]
        hidden = {k for k in range(len(found)) if start[k][1] < 0.25 * rest_size[k]}
        result["clips"][clip + "_hidden"] = sorted({found[k][0] for k in hidden})
        result["clips"][clip + "_kept"] = sorted({found[k][0] for k in range(len(found)) if k not in gone and k not in hidden})
    return result


def main():
    args = C.cli_args()
    folder = args.get("dir") or os.path.join(C.EXPORTS, NAME)
    if not os.path.isabs(folder):
        folder = os.path.join(C.REPO, folder)
    tag = args.get("tag") or "after"
    clips = (args.get("clips") or "PhaseBreak1,PhaseBreak2,Split,Rejoin,Death").split(",")
    report = {}
    for rig in (NAME, NAME + "_LOD1"):
        r = run(os.path.join(folder, rig + ".fbx"), clips)
        report[rig] = r
        print("R53", rig, "plates:", {b: len(v) for b, v in sorted(r["plates"].items())})
        for clip in clips:
            if clip in r["clips"]:
                seq = ", ".join(f"{t['bone']}@{t['frame']}" for t in r["clips"][clip])
                print("R53", rig, clip, "thrown:", seq or "-", "| kept:", ",".join(r["clips"][clip + "_kept"]) or "-",
                      "| hidden from the start:", ",".join(r["clips"][clip + "_hidden"]) or "-")
    os.makedirs(QA, exist_ok=True)
    with open(os.path.join(QA, f"r53_shell_throw_{tag}.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=1)


if __name__ == "__main__":
    main()
