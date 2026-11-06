"""Diagnostic (round 19): solves Gravemaw's gate clips (Gate_Mount, Gate_Straddle_Loop, Gate_Dismount) on the saved
rig without keying and prints, per limb bone, the largest per-frame step of its local rotation (what Unity's seam sweep
measures), with the frame, plus the two-bone solve at that frame (reach share and |w|: how far the pole stands off the
hip-target line) and, for a spade, the angle between the elbow-plane normal and the spade.

Run: blender --background --factory-startup --python Tools/Blender/diag_gate_steps.py -- [--min 40]
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_gravemaw as BG  # noqa: E402
import common as C  # noqa: E402
import gravemaw_gate as GG  # noqa: E402

LIMB_BONES = [n for lg in list(BG.ARMS.values()) + list(BG.LEGS.values()) for n in lg.names]
solves = []
orig = C.two_bone


def traced(root, target, l1, l2, pole):
    to = target - root
    u = to.normalized()
    w = Vector(pole) - u * Vector(pole).dot(u)
    solves.append((round(w.length, 3), round(to.length / (l1 + l2), 3)))
    return orig(root, target, l1, l2, pole)


C.two_bone = traced


def leg_key(bone):
    """'Leg_L3_Lower' -> ('L', 3); 'Arm_L_Spade' -> 'L'."""
    part = bone.split("_")[1]
    return part if bone.startswith("Arm") else (part[0], int(part[1]))


def local_rotations(poser, pose):
    out = {}
    for n in LIMB_BONES:
        basis = poser.inherited(n, pose).inverted() @ pose[n]
        out[n] = basis.to_quaternion()
    return out


def main():
    floor = float(C.cli_args().get("min", 40))
    boss = M.Boss(BG.NAME, list(BG.CLIPS), scale_bones=BG.DEBRIS_NAMES, lod_aims=BG.LOD_AIMS)
    poser = boss.poser
    for name, make in (("Gate_Mount", GG.mount), ("Gate_Straddle_Loop", GG.straddle), ("Gate_Dismount", GG.dismount)):
        frames, spec, loop = make()[0](poser)
        prev, worst, info, twist = None, {}, {}, {}
        for f in range(frames + 1):
            solves.clear()
            pose = poser.solve(spec(f / frames, f))
            cur = local_rotations(poser, pose)
            info[f] = list(solves)
            for n in ("Arm_L_Spade", "Arm_R_Spade"):
                tw = abs(math.degrees(cur[n].to_swing_twist("Y")[1]))
                if tw > twist.get(n, (0, 0))[0]:
                    twist[n] = (tw, f)
            if prev:
                for n in LIMB_BONES:
                    a = math.degrees(prev[n].rotation_difference(cur[n]).angle)
                    a = min(a, 360 - a)
                    if a > worst.get(n, (0, 0))[0]:
                        worst[n] = (a, f)
            prev = cur
        log = GG.MOUNT_LOG if name == "Gate_Mount" else GG.DISMOUNT_LOG if name == "Gate_Dismount" else []
        for n, (a, f) in sorted(worst.items(), key=lambda kv: -kv[1][0]):
            if a >= floor:
                print(f"GATESTEP {name} {n} {a:.1f} deg at frame {f}")
                key = leg_key(n)
                for e in log:
                    if e[0] == key and abs(e[1] - f) <= 10:
                        print(f"    plan {e}")
        print(f"GATETWIST {name} {twist}")
        print(f"GATEMAX {name} {max(a for a, _ in worst.values()):.1f}")


main()
