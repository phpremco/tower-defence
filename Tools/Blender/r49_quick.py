"""Round 49B: a fast look at a body redesign without the bake: the build script's parts fused at a coarse voxel,
its styles run per vertex (colour + glow as attributes), rendered in the rest pose from the play-camera angle and a
3/4 close-up. Nothing is saved or exported.

Run: blender -b --factory-startup --python Tools/Blender/r49_quick.py -- --script build_cragback.py [--fn build_parts]
     [--voxel 0.006] [--out <png>] [--focus x,up,forward --dist d]
"""

import os
import re
import sys
import types

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402


def load(script):
    path = os.path.join(HERE, script)
    src = open(path, encoding="utf-8").read()
    src = re.sub(r"^main\(\)\s*$", "", src, flags=re.M)
    mod = types.ModuleType("r49q_" + os.path.splitext(script)[0])
    mod.__file__ = path
    exec(compile(src, path, "exec"), mod.__dict__)
    return mod


def vertex_material():
    mat = C.new_material("Quick")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    alb = nt.nodes.new("ShaderNodeVertexColor")
    alb.layer_name = "Albedo"
    emi = nt.nodes.new("ShaderNodeVertexColor")
    emi.layer_name = "Emit"
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(emi.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 2.5
    bsdf.inputs["Roughness"].default_value = 0.75
    return mat


def main():
    args = C.cli_args()
    mod = load(args["script"])
    C.reset_scene()
    voxel = float(args.get("voxel", 0.006))
    builders = [getattr(mod, fn)() for fn in args.get("fn", "build_parts").split(",")]
    objs = []
    for i, b in enumerate(builders):
        parts = b.to_object(f"Q{i}_Parts")
        mesh = C.fuse(parts, voxel, 4)
        bpy.data.objects.remove(parts)
        C.detail(mesh, b)
        C.shade_smooth(mesh)
        o = C.link(bpy.data.objects.new(f"Q{i}", mesh))
        o.data.materials.append(vertex_material())
        objs.append(o)
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    for o in objs:
        for v in o.data.vertices:
            lo = Vector(map(min, lo, v.co))
            hi = Vector(map(max, hi, v.co))
    centre = (lo + hi) / 2
    size = max(hi - lo)
    C.preview_world(color=(0.07, 0.08, 0.1), strength=1.6)
    for o in C.light_rig(scale=max(1.0, size * 0.8)):
        o.data.energy *= 1.8 if o.data.type == "SUN" else 1.5
    C.ground(color=(0.09, 0.09, 0.1))
    if args.get("focus"):
        # --focus x,up,forward --dist d: the close-ups look at one region (a head) instead of the whole body.
        fx, fu, ff = (float(v) for v in args["focus"].split(","))
        centre = C.G(fx, fu, ff)
        size = float(args.get("dist", 1.0)) / 1.5
    out = os.path.join(C.REPO, args.get("out") or os.path.join("Logs", "r49b", "quick.png"))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    a = C.render(C.game_camera(centre, size * 1.35), out.replace(".png", "_a.png"), 800, 600)
    b = C.render(C.closeup_camera(centre + Vector((0, -size * 0.1, 0)), size * 1.55, azimuth=-36, elevation=20, lens=50),
                 out.replace(".png", "_b.png"), 800, 600)
    c = C.render(C.closeup_camera(centre, size * 1.7, azimuth=-90, elevation=6, lens=50), out.replace(".png", "_c.png"),
                 800, 600)
    d = C.render(C.closeup_camera(centre, size * 1.4, azimuth=-150, elevation=50, lens=50), out.replace(".png", "_d.png"),
                 800, 600)
    C.contact_sheet([a, b, c, d], out, 2, 640)
    for p in (a, b, c, d):
        os.remove(p)


if __name__ == "__main__":
    main()
