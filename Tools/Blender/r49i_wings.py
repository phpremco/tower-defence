"""Round 49I: QA for the flyers' strikes that keep the wing beat (bat `Strike`, Skyray `Dive`).

Remco: "when the bats are attacking they should still be flying, they seem to stop flying as they attack". Opens
<Name>.blend (the current one, or a backup with `--blend`) and never saves it. For the full rig and the LOD1 rig:
  - the wing height per frame: the wing tip (bat `Finger_L2`, ray `Fin_L_A3`) in the Body bone's frame, so the body's
    own dive does not count; the longest run of frames in which the wing hardly moves (a hold), the number of turns
    (top or bottom of a stroke), the peak-to-peak; the same for one Fly cycle as the yardstick;
  - how far every wing bone's local rotation at the clip's first and last frame is from Fly frame 0 (the seam with
    the Fly cycle Unity keeps running underneath), and the largest such gap over all bones;
  - the per-frame bone turn of every clip (the round-49H measure: a snap is a step over 6 degrees that is 1.8 times
    larger than both its neighbours);
  - renders: ArtSource/Previews/QA/r49i_<key>_<clip>_sheet[_before].png, eight frames, side view on top and the play
    camera below.
Writes ArtSource/Previews/QA/r49i_<key>_<tag>.json.

Run: blender -b --factory-startup --python Tools/Blender/r49i_wings.py -- --only Bat,Skyray --tag before
     [--blend ArtSource/Exports/_backup_r49/{name}_49I/{name}.blend] [--norender]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")

# name: (sheet key, strike clip, wing tip bone, wing bone prefixes)
FLYERS = {
    "Bat": ("bat", "Strike", "Finger_L2", ("Arm_", "Hand_", "Finger_")),
    "Skyray": ("skyray", "Dive", "Fin_L_A3", ("Fin_",)),
    "Shalebat": ("shalebat", "Strike", "Finger_L2", ("Arm_", "Hand_", "Finger_")),
}
SHEET_FRAMES = (0, 3, 6, 9, 10, 13, 17, 21)
HOLD_SHARE = 0.03  # a frame in which the tip moves less than 3 % of Fly's stroke counts as held


# The round-49H per-frame bone turn (r49h_rayboss.steps; that module runs on import, so it is repeated here).
def steps(arm, act):
    """Largest armature-space and local turn per frame for every bone: {bone: (arm deg, frame, ratio, local deg)}."""
    start, end = int(act.frame_range[0]), int(act.frame_range[1])
    prev_a, prev_l, st_a, st_l = {}, {}, {}, {}
    for f in range(start, end + 1):
        C.set_action(arm, act, f)
        for pb in arm.pose.bones:
            qa, ql = pb.matrix.to_quaternion(), pb.rotation_quaternion.copy()
            if pb.name in prev_a:
                a = math.degrees(prev_a[pb.name].rotation_difference(qa).angle)
                b = math.degrees(prev_l[pb.name].rotation_difference(ql).angle)
                st_a.setdefault(pb.name, []).append((min(a, 360 - a), f))
                st_l.setdefault(pb.name, []).append(min(b, 360 - b))
            prev_a[pb.name], prev_l[pb.name] = qa, ql
    out = {}
    for bone, st in st_a.items():
        vals = sorted(v for v, _ in st)
        med = max(vals[len(vals) // 2], 0.5)
        v, f = max(st)
        # A snap: one step much larger than both its neighbours (a spike, not a fast but even swing).
        spikes = []
        for i in range(1, len(st) - 1):
            if st[i][0] > 6 and st[i][0] > 1.8 * max(st[i - 1][0], st[i + 1][0]):
                spikes.append(st[i][1])
        out[bone] = dict(max_arm_deg=round(v, 1), frame=f, ratio_to_median=round(v / med, 1),
                         max_local_deg=round(max(st_l[bone]), 1), spikes=spikes)
    return out


def wing_heights(arm, act, tip):
    """The wing tip's height in the Body bone's rest frame, per frame (the body's dive and pitch removed)."""
    body_rest = arm.data.bones["Body"].matrix_local
    out = []
    for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        body = arm.pose.bones["Body"].matrix
        p = body_rest @ body.inverted() @ arm.pose.bones[tip].tail
        out.append(round(p.z, 4))
    return out


def stroke(heights, yardstick):
    """Longest held run, turns and peak-to-peak of a height series; `yardstick` is Fly's peak-to-peak."""
    eps = HOLD_SHARE * yardstick
    run = best = 0
    turns = 0
    for i in range(1, len(heights)):
        if abs(heights[i] - heights[i - 1]) < eps:
            run += 1
            best = max(best, run)
        else:
            run = 0
        if 1 < i and (heights[i] - heights[i - 1]) * (heights[i - 1] - heights[i - 2]) < 0:
            turns += 1
    return dict(longest_hold_frames=best, turns=turns, peak_to_peak=round(max(heights) - min(heights), 4))


def local_rots(arm, act, frame):
    C.set_action(arm, act, frame)
    bpy.context.view_layer.update()
    return {pb.name: pb.rotation_quaternion.copy() for pb in arm.pose.bones}


def seam(arm, clip, fly, prefixes):
    """Gap (degrees) of every bone's local rotation to Fly frame 0 at the clip's first and last frame."""
    ref = local_rots(arm, fly, 0)
    out = {}
    for label, f in (("first", int(clip.frame_range[0])), ("last", int(clip.frame_range[1]))):
        rots = local_rots(arm, clip, f)
        gaps = {n: math.degrees(ref[n].rotation_difference(q).angle) for n, q in rots.items()}
        gaps = {n: round(min(g, 360 - g), 2) for n, g in gaps.items()}
        wing = {n: g for n, g in gaps.items() if n.startswith(prefixes)}
        worst = max(gaps.items(), key=lambda kv: kv[1])
        out[label] = dict(wing_max_deg=max(wing.values()), all_bones_max_deg=worst[1], worst_bone=worst[0])
    return out


def clip_steps(arm, suffix):
    table = {}
    for act in bpy.data.actions:
        is_lod = act.name.endswith("_LOD")
        if (suffix == "_LOD") != is_lod or "." in act.name or act.name.endswith(("_Full", "_Away")):
            continue
        s = steps(arm, act)
        worst = max(s.items(), key=lambda kv: kv[1]["max_arm_deg"])
        table[act.name] = dict(worst_bone=worst[0], max_arm_deg=worst[1]["max_arm_deg"], frame=worst[1]["frame"],
                               spikes={b: v["spikes"] for b, v in s.items() if v["spikes"]})
    return table


def measure(arm, clip_name, tip, prefixes, suffix=""):
    clip = bpy.data.actions[clip_name + suffix]
    fly = bpy.data.actions["Fly" + suffix]
    fly_h = wing_heights(arm, fly, tip)
    clip_h = wing_heights(arm, clip, tip)
    ref = max(fly_h) - min(fly_h)
    return dict(fly=dict(heights=fly_h, **stroke(fly_h, ref)), clip=dict(heights=clip_h, **stroke(clip_h, ref)),
                seam_to_fly0=seam(arm, clip, fly, prefixes), steps=clip_steps(arm, suffix))


def renders(arm, meshes, act, key, clip_name, tag):
    C.set_action(arm, act, 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y)
    V.bright_scene(size, lo.z - 0.6 * size)
    centre = (lo + hi) * 0.5
    tmp = os.path.join(QA, "_r49i_tmp")
    side, game = [], []
    for f in SHEET_FRAMES:
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        side.append(C.render(V.cam("close", centre, size, -90, 6, size * 1.9), os.path.join(tmp, f"side_{f}.png"),
                             560, 400, samples=16))
        game.append(C.render(V.cam("game", centre, size * 1.2), os.path.join(tmp, f"game_{f}.png"), 560, 400,
                             samples=16))
    suffix = "" if tag == "after" else "_" + tag
    sheet = os.path.join(QA, f"r49i_{key}_{clip_name.lower()}_sheet{suffix}.png")
    C.contact_sheet(side + game, sheet, len(SHEET_FRAMES), 300)
    for p in side + game:
        os.remove(p)
    C.log("sheet", sheet)


def run(name, tag, blend, norender):
    key, clip_name, tip, prefixes = FLYERS[name]
    bpy.ops.wm.open_mainfile(filepath=blend)
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects[name + "_LOD1_Rig"]
    report = dict(blend=os.path.relpath(blend, C.REPO), clip=clip_name,
                  parts=[o.name for o in bpy.data.objects if o.type == "MESH" and o.parent == arm],
                  full=measure(arm, clip_name, tip, prefixes))
    # The bat's LOD1 folds Finger_L2 into the hand; its first spine is the tip there.
    lod_tip = tip if tip in lod_arm.pose.bones else "Finger_L1"
    report["lod"] = measure(lod_arm, clip_name, lod_tip, prefixes, "_LOD")
    path = os.path.join(QA, f"r49i_{key}_{tag}.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=1)
    for rig in ("full", "lod"):
        r = report[rig]
        C.log("R49I", name, tag, rig, "clip", {k: v for k, v in r["clip"].items() if k != "heights"},
              "fly", {k: v for k, v in r["fly"].items() if k != "heights"}, "seam", r["seam_to_fly0"])
        C.log("R49I", name, tag, rig, "clip heights", r["clip"]["heights"])
        C.log("R49I", name, tag, rig, "steps", {c: (v["worst_bone"], v["max_arm_deg"], v["spikes"])
                                               for c, v in r["steps"].items()})
    if not norender:
        for o in bpy.data.objects:
            if o == lod_arm or o.parent == lod_arm:
                o.hide_render = True
        meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.hide_render]
        renders(arm, meshes, bpy.data.actions[clip_name], key, clip_name, tag)


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    for name in [n for n in (args.get("only") or ",".join(FLYERS)).split(",") if n]:
        blend = args.get("blend")
        blend = os.path.join(C.REPO, blend.format(name=name)) if blend else os.path.join(C.BLEND, name + ".blend")
        run(name, tag, blend, bool(args.get("norender")))


main()
