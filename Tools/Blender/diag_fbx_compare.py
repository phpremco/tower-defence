"""Diagnostic (round 17): imports two rigged FBX files and reports how they differ: mesh counts and bounds, bone
names, actions and frame ranges, and the largest per-bone difference (location, rotation, scale) over every frame.

Run: blender --background --factory-startup --python Tools/Blender/diag_fbx_compare.py -- --a <fbx> --b <fbx>
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import common as C  # noqa: E402


def load(path):
    before = set(bpy.data.objects)
    acts_before = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=path)
    objs = [o for o in bpy.data.objects if o not in before]
    acts = [a for a in bpy.data.actions if a not in acts_before]
    arm = next(o for o in objs if o.type == "ARMATURE")
    meshes = [o for o in objs if o.type == "MESH"]
    return arm, meshes, {a.name.split("|")[-1]: a for a in acts}


def sample(arm, act, frame):
    arm.animation_data_create()
    arm.animation_data.action = act
    if hasattr(arm.animation_data, "action_slot") and act.slots:
        arm.animation_data.action_slot = act.slots[0]
    bpy.context.scene.frame_set(frame)
    return {pb.name: pb.matrix.copy() for pb in arm.pose.bones}


def main():
    args = C.cli_args()
    C.reset_scene()
    a_arm, a_mesh, a_acts = load(args["a"])
    b_arm, b_mesh, b_acts = load(args["b"])
    for tag, meshes in (("A", a_mesh), ("B", b_mesh)):
        for m in meshes:
            print(f"CMP {tag} mesh {m.name} verts {len(m.data.vertices)} polys {len(m.data.polygons)} "
                  f"dims {tuple(round(d, 4) for d in m.dimensions)} groups {len(m.vertex_groups)}")
    ab = [b.name for b in a_arm.data.bones]
    bb = [b.name for b in b_arm.data.bones]
    print(f"CMP bones same {ab == bb}: A {ab} B {bb}")
    print(f"CMP actions A {sorted((n, tuple(a.frame_range)) for n, a in a_acts.items())}")
    print(f"CMP actions B {sorted((n, tuple(a.frame_range)) for n, a in b_acts.items())}")
    for name in sorted(set(a_acts) & set(b_acts)):
        a0, a1 = (int(x) for x in a_acts[name].frame_range)
        worst_l = worst_r = worst_s = 0.0
        where = ""
        for f in range(a0, a1 + 1):
            pa, pb = sample(a_arm, a_acts[name], f), sample(b_arm, b_acts[name], f)
            for bone in pa:
                if bone not in pb:
                    continue
                la, ra, sa = pa[bone].decompose()
                lb, rb, sb = pb[bone].decompose()
                dl = (la - lb).length
                dr = math.degrees(ra.rotation_difference(rb).angle)
                dr = min(dr, 360 - dr)
                ds = (sa - sb).length
                if dl > worst_l or dr > worst_r or ds > worst_s:
                    where = f"{bone}@{f}"
                worst_l, worst_r, worst_s = max(worst_l, dl), max(worst_r, dr), max(worst_s, ds)
        print(f"CMP clip {name}: max loc {worst_l:.4f} rot {worst_r:.2f} deg scale {worst_s:.4f} ({where})")
    va = [v.co.copy() for m in a_mesh for v in m.data.vertices]
    vb = [v.co.copy() for m in b_mesh for v in m.data.vertices]
    if len(va) == len(vb):
        print(f"CMP mesh max vertex delta {max((x - y).length for x, y in zip(va, vb)):.5f}")


main()
