"""Re-imports an exported FBX and prints what Unity will find in it: meshes, triangles, bones, actions.

Run: blender --background --python Tools/Blender/inspect_fbx.py -- --fbx <path>
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import common as C  # noqa: E402

C.reset_scene()
path = C.cli_args()["fbx"]
bpy.ops.import_scene.fbx(filepath=path)
for obj in bpy.context.scene.objects:
    if obj.type == "MESH":
        C.log("mesh", obj.name, "tris", C.tri_count(obj), "groups", len(obj.vertex_groups), "uv", len(obj.data.uv_layers),
              "dims", tuple(round(d, 3) for d in obj.dimensions), "scale", tuple(round(s, 3) for s in obj.scale))
    elif obj.type == "ARMATURE":
        C.log("armature", obj.name, "bones", len(obj.data.bones), "scale", tuple(round(s, 3) for s in obj.scale),
              "rot", tuple(round(r, 3) for r in obj.rotation_euler))
for act in bpy.data.actions:
    C.log("action", act.name, "frames", tuple(act.frame_range))
