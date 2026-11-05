"""Re-imports an exported rigged FBX and lists, per clip, the bones whose scale is not 1 (at the first, middle and
last frame). Catches bones a clip does not key: the FBX exporter bakes them with whatever pose the previous clip
left behind (round 29: the Frost Warden's old clips had the armour plates at scale 0).

Run: blender --background --factory-startup --python Tools/Blender/check_clip_scales.py -- --fbx <path>
     [--bones Armour_,Rubble,Seal_]   (only bones whose name starts with one of these)
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import common as C  # noqa: E402

args = C.cli_args()
C.reset_scene()
bpy.ops.import_scene.fbx(filepath=args["fbx"])
prefixes = [p for p in (args.get("bones") or "").split(",") if p]
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
arm.animation_data_create()
for act in bpy.data.actions:
    arm.animation_data.action = act
    if hasattr(arm.animation_data, "action_slot") and act.slots:
        arm.animation_data.action_slot = act.slots[0]
    a, b = act.frame_range
    small = {}
    for f in (a, (a + b) / 2, b):
        bpy.context.scene.frame_set(int(round(f)))
        for pb in arm.pose.bones:
            if prefixes and not any(pb.name.startswith(p) for p in prefixes):
                continue
            s = min(pb.scale)
            if abs(s - 1) > 0.05 or abs(max(pb.scale) - 1) > 0.05:
                small.setdefault(pb.name, []).append(round(s, 2))
    C.log("clip", act.name, "frames", (a, b), "off-scale bones", len(small),
          "" if not small else ", ".join(f"{n}:{v}" for n, v in sorted(small.items())))
