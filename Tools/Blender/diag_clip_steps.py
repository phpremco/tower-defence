"""Diagnostic (round 17): keys the FireLarva clips on a bare armature (no mesh) and prints, per clip and bone, the
largest local-rotation step between two keyed frames and where it happens, plus the knee-plane normal at that frame.

Run: blender --background --factory-startup --python Tools/Blender/diag_clip_steps.py
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402,F401

import build_cindermatriarch as BC  # noqa: E402
import common as C  # noqa: E402
import creature_kit3 as K3  # noqa: E402


def main():
    C.reset_scene()
    arm = C.build_armature("Diag_Rig", BC.larva_bones())
    poser = K3.ClipPoser(arm)
    actions = K3.key_clips(poser, BC.LARVA_CLIPS)
    for clip, act in actions.items():
        arm.animation_data.action = act
        start, end = int(act.frame_range[0]), int(act.frame_range[1])
        prev = {}
        worst = {}
        for f in range(start, end + 1):
            bpy.context.scene.frame_set(f)
            for pb in arm.pose.bones:
                q = pb.rotation_quaternion.copy()
                if pb.name in prev:
                    a = math.degrees(prev[pb.name].rotation_difference(q).angle)
                    a = min(a, 360 - a)
                    if a > worst.get(pb.name, (0, 0))[0]:
                        worst[pb.name] = (a, f)
                prev[pb.name] = q
        for name, (a, f) in sorted(worst.items(), key=lambda kv: -kv[1][0]):
            if a > 20:
                print(f"DIAG {clip} {name} max step {a:.1f} deg at frame {f}")


main()
