"""Round 39: Gravemaw rears up to roar, so the 44-degree play camera looks into the toothed throat (round 38).

The round-14 Roar lifted the front 0.8 m but turned the head 32 degrees *down* (a rotation about +X pitches a -Y
forward axis down), so the open maw pointed about 31 degrees at the ground and the play camera only saw the top of
the head. The new Roar (same 66 frames, same bones, jaws open on the same frames) is a rear:

- anticipation (frames 0-13): the front draws back and the head tucks, the spades dig in;
- rise (7-23): the front four spine segments curl up in an arc (chest 12/28/45 degrees, head 58 degrees), the neck
  arches back past the chest, and the petals spread to 60 degrees at the peak (frame ~22): the maw axis points
  about 57 degrees up, the spade arms lift and spread;
- held roar (23-49): a shudder through the front, small head shakes (yaw, pitch, roll) and every petal quivering on
  its own phase;
- heavy drop (49-59): the front falls with gravity (ease-in) and slams down at frame ~59, a small rebound, back to the
  rest pose at 66.

The event key Unity reads (Roar 0.4 s = frame 12, the roar cue, as the jaws start to open) keeps its time: the jaws
start opening at frame 12 as before.

Surface_Throw (the power clip that ends in the Roar at the drill rig) also tips the head up while the maw is wide
(frames 14-34, up to +32 degrees on the Head bone, zero again by frame 34; the body path, jaws and debris are
untouched): Head-bone rotation keys only. Every other clip is left as it is.

Run after gravemaw_maw.py (the .blend holds the maw):
  blender -b --factory-startup --threads 8 --python Tools/Blender/gravemaw_roar.py -- [--dry] [--norender]
  --dry: key and render into the scratch copies only (nothing saved or exported); --qa: only the after renders
  --before: only the before renders of the current .blend
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

import build_gravemaw as BG  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import deep_kit as DK  # noqa: E402
import gravemaw_maw as GMW  # noqa: E402

NAME = BG.NAME
R = math.radians
ease, pulse = K2.ease, K2.pulse
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
FRAMES = 66                     # unchanged
FRONT = 4                       # spine nodes 0..3 move; node 4 (Seg_3 / Seg_4 joint) and everything behind stay put
PEAK = [R(58), R(45), R(28), R(12)]   # pitch of segment node j+1 -> node j at the top of the rear (head first)
JAW_PEAK = R(60)
JAW_HOLD = R(57)
QUIVER = R(2.6)
THROW_TIP = R(32)
THROW_TIP_FRAMES = (14, 24, 34)       # Surface_Throw: tip in, peak, back to zero (throw moment is frame 30)
VERSION_NOTE = ("round 39: Roar is a rear (front curls up, head 58 degrees, maw open skyward ~57 degrees at the "
                "44-degree camera, shudder and petal quiver, heavy drop); Surface_Throw tips the Head up to +32 "
                "degrees over frames 14-34; same bones, clip names, frame counts and event frames")


def rest_fh():
    return [(f, h) for f, h in BG.NODES]


REST = rest_fh()
LENGTHS = [math.hypot(REST[j][0] - REST[j + 1][0], REST[j][1] - REST[j + 1][1]) for j in range(FRONT)]
REST_PITCH = [math.atan2(REST[j][1] - REST[j + 1][1], REST[j][0] - REST[j + 1][0]) for j in range(FRONT)]


def direction(pitch, yaw=0.0):
    """Blender-space unit vector: forward is -Y, `yaw` turns it towards +X."""
    return Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))


def rear_curve(t):
    """0 at rest, 1 at the top: rise 0.1-0.34 with a small overshoot, held, gravity drop 0.74-0.9, a small rebound."""
    up = ease(0.1, 0.34, t) * (1 + 0.05 * pulse(t, 0.26, 0.34, 0.46))
    u = min(max((t - 0.74) / 0.16, 0.0), 1.0)
    r = up * (1 - u * u)
    rebound = 0.045 * math.sin(math.pi * min(max((t - 0.9) / 0.1, 0.0), 1.0)) ** 2
    return r + rebound


def roar_nodes(t):
    """Spine nodes and ups at time t of the new Roar."""
    pos, ups = BG.rest_nodes()
    r = rear_curve(t)
    dip = pulse(t, 0.0, 0.08, 0.2)
    hold = pulse(t, 0.3, 0.4, 0.76)
    impact = pulse(t, 0.88, 0.9, 1.0)
    w = 2 * math.pi * t
    pitch = [REST_PITCH[j] + (PEAK[j] - REST_PITCH[j]) * r for j in range(FRONT)]
    # Anticipation: the head tucks while the chest rises a touch (the tip stays off the ground).
    pitch[0] += -R(9) * dip
    pitch[1] += R(5) * dip
    pitch[2] += R(2) * dip
    # The roar: the head pushes further back through the hold and shakes.
    pitch[0] += R(3) * hold * ease(0.34, 0.6, t) + R(2.5) * hold * math.sin(w * 4.5 + 1.0)
    yaw0 = R(5) * hold * math.sin(w * 3.0) + R(1.5) * hold * math.sin(w * 7.0)
    yaw1 = R(1.5) * hold * math.sin(w * 3.0 - 0.6)
    roll0 = R(6) * hold * math.sin(w * 3.0 + 0.5)
    yaws = [yaw0, yaw1, 0.0, 0.0]
    for j in range(FRONT - 1, -1, -1):
        pos[j] = pos[j + 1] + direction(pitch[j], yaws[j]) * LENGTHS[j]
    # Draw back in the anticipation, a shudder through the front in the hold, a jolt on landing (never downwards).
    back = 0.16 * dip
    shudder = 0.016 * hold * math.sin(w * 23.0) + 0.012 * impact * abs(math.sin(w * 19.0))
    side = 0.01 * hold * math.sin(w * 17.0 + 0.3)
    for j, k in enumerate((1.0, 0.85, 0.55, 0.2)):
        pos[j] = pos[j] + Vector((side * k, back * k, shudder * k))
    head_dir = (pos[0] - pos[1]).normalized()
    ups[0] = Matrix.Rotation(roll0, 3, head_dir) @ ups[0]
    return pos, ups, r, dip, hold


def roar_jaws(t, hold):
    """Spread wide at the peak, a little less in the hold with each petal quivering on its own phase, shut by 0.88."""
    shut = 1 - ease(0.74, 0.88, t)
    base = JAW_PEAK * ease(0.18, 0.34, t) - (JAW_PEAK - JAW_HOLD) * ease(0.34, 0.44, t)
    out = {}
    for i, (name, _) in enumerate(BG.PETALS):
        q = QUIVER * hold * math.sin(2 * math.pi * t * 7.0 + 1.3 * i)
        out[name] = max(0.0, (base + q) * shut)
    return out


def roar_spec(poser):
    def spec(t, f):
        pos, ups, r, dip, hold = roar_nodes(t)
        jaws = roar_jaws(t, hold)
        arms, legs = BG.world_feet()
        tmp = BG.assemble(poser, pos, ups, 0.0)
        lift = min(max(r, 0.0), 1.0)
        tremble = 0.02 * hold * math.sin(2 * math.pi * t * 13.0)
        for side, lg in BG.ARMS.items():
            sx = -1 if side == "L" else 1
            carried = DK.carried(poser, tmp, "Seg_1", lg.foot)
            raised = carried + C.G(0.38 * sx, 0.12 + tremble, 0.08)
            dug = lg.foot + C.G(0.04 * sx, 0.0, -0.14) * dip
            arms[side] = dug.lerp(raised, lift)
        for key, lg in BG.LEGS.items():
            if key[1] == 1:
                carried = DK.carried(poser, tmp, BG.LEG_SEGS[1][0], lg.foot)
                legs[key] = lg.foot.lerp(carried, 0.7 * lift)
        return BG.assemble(poser, pos, ups, jaws, arms, legs)
    return FRAMES, spec, False


# ------------------------------------------------------------------------------------------------------------
# Keying
# ------------------------------------------------------------------------------------------------------------

def posers(arm, lod_arm):
    lod_names = {b.name for b in lod_arm.data.bones}
    full = K3.ClipPoser(arm, BG.DEBRIS_NAMES)
    lod = K3.ClipPoser(lod_arm, [b for b in BG.DEBRIS_NAMES if b in lod_names])
    return full, lod


def rekey_roar(arm, lod_arm):
    full, lod = posers(arm, lod_arm)
    for n in ("Roar", "Roar_LOD"):
        act = bpy.data.actions.get(n)
        if act is not None:
            bpy.data.actions.remove(act)
    frames, spec, loop = roar_spec(full)
    full.use("Roar")
    act = full.action("Roar", frames, spec, loop=loop)
    lod.use("Roar")
    lod_act = C.lod_action(full, lod, "Roar_LOD", frames, spec, loop, BG.LOD_AIMS)
    C.log("keyed Roar", frames, act.name, lod_act.name)
    return act, lod_act


def throw_tip(f):
    a, p, b = THROW_TIP_FRAMES
    return THROW_TIP * pulse(f, a, p, b)


def tip_head(act, arm, angle_of_frame):
    """Adds a pitch-up about the Head bone's own lateral axis to every Head rotation key of `act` (children such as
    the jaws and Socket_Throw follow; nothing else changes)."""
    bone = arm.data.bones["Head"]
    lateral = (bone.matrix_local.to_3x3().inverted() @ Vector((1, 0, 0))).normalized()
    path = 'pose.bones["Head"].rotation_quaternion'
    bag = K2._channelbags(act)[0]
    curves = [bag.fcurves.find(path, index=i) for i in range(4)]
    if any(c is None for c in curves):
        raise RuntimeError(f"{act.name}: no Head rotation curves")
    n = len(curves[0].keyframe_points)
    changed = 0
    for k in range(n):
        f = curves[0].keyframe_points[k].co[0]
        ang = angle_of_frame(f)
        if abs(ang) < 1e-6:
            continue
        q = Quaternion([curves[i].keyframe_points[k].co[1] for i in range(4)])
        # A rotation about +X turns the forward (-Y) axis down, so up is negative.
        q2 = q @ Quaternion(lateral, -ang)
        if q2.dot(q) < 0:
            q2.negate()
        for i in range(4):
            curves[i].keyframe_points[k].co[1] = q2[i]
        changed += 1
    for c in curves:
        c.update()
    C.log("tipped head", act.name, changed, "keys")
    return changed


def tip_throw(arm, lod_arm):
    return {"Surface_Throw": tip_head(bpy.data.actions["Surface_Throw"], arm, throw_tip),
            "Surface_Throw_LOD": tip_head(bpy.data.actions["Surface_Throw_LOD"], lod_arm, throw_tip)}


# ------------------------------------------------------------------------------------------------------------
# Checks
# ------------------------------------------------------------------------------------------------------------

def head_space(arm):
    pb = arm.pose.bones["Head"]
    return arm.matrix_world @ pb.matrix @ arm.data.bones["Head"].matrix_local.inverted()


def maw_pitch(arm):
    a, _, _ = BG.maw_frame()
    ax = (head_space(arm).to_3x3() @ a).normalized()
    return math.degrees(math.asin(max(-1.0, min(1.0, ax.z))))


def bend(arm, a, b):
    """Joint bend between two spine bones (the Head points forward from the neck, the segments point backwards)."""
    ya = arm.pose.bones[a].matrix.to_3x3().col[1].normalized()
    yb = arm.pose.bones[b].matrix.to_3x3().col[1].normalized()
    if (a == "Head") != (b == "Head"):
        yb = -yb
    return math.degrees(ya.angle(yb))


def debris_free(obj):
    """Vertex mask without the debris chunks (hidden at the hole under the ground at rest)."""
    names = {vg.index: vg.name for vg in obj.vertex_groups}
    keep = np.ones(len(obj.data.vertices), dtype=bool)
    for v in obj.data.vertices:
        best = max(v.groups, key=lambda g: g.weight, default=None)
        if best is not None and names.get(best.group, "").startswith("Debris"):
            keep[v.index] = False
    return keep


def min_z(objs, masks):
    dg = bpy.context.evaluated_depsgraph_get()
    lo = 1e9
    for o, mask in zip(objs, masks):
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        co = np.empty(len(me.vertices) * 3)
        me.vertices.foreach_get("co", co)
        co = co.reshape(-1, 3)
        if mask is not None and len(mask) == len(co):
            co = co[mask]
        mw = np.array(o.matrix_world)
        z = co @ mw[2, :3] + mw[2, 3]
        lo = min(lo, float(z.min()))
        ev.to_mesh_clear()
    return lo


JOINTS = [("Head", "Seg_1"), ("Seg_1", "Seg_2"), ("Seg_2", "Seg_3"), ("Seg_3", "Seg_4")]


def clip_stats(arm, objs, masks, clip, frames=None):
    act = bpy.data.actions[clip]
    n = int(act.frame_range[1])
    frames = frames if frames is not None else range(0, n + 1, 2)
    rows = []
    for f in frames:
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        rows.append({"f": f, "pitch": round(maw_pitch(arm), 1),
                     "jaw": round(math.degrees(2 * math.acos(min(1.0, abs(arm.pose.bones["Jaw_T"].rotation_quaternion.w)))), 1),
                     "bend": [round(bend(arm, a, b), 1) for a, b in JOINTS],
                     "min_z": round(min_z(objs, masks), 3)})
    return rows


def throat_intrusions(arm, mesh, clip, frames):
    """gravemaw_maw.cut_posed's test without cutting: body faces whose centre swings inside the throat lumen."""
    hits = 0
    for f in frames:
        C.set_action(arm, bpy.data.actions[clip], f)
        bpy.context.view_layer.update()
        inv = head_space(arm).inverted()
        dg = bpy.context.evaluated_depsgraph_get()
        ev = mesh.evaluated_get(dg)
        me = ev.to_mesh()
        for p in me.polygons:
            c = inv @ (mesh.matrix_world @ p.center)
            d, r, phi, _ = GMW.frame_coords(np.array(tuple(c))[None, :])
            if GMW.THROAT_D[0] < d[0] < 0.75 and r[0] < GMW.lumen_radius(float(d[0]), float(phi[0])) - 0.005:
                hits += 1
        ev.to_mesh_clear()
    return hits


def summarise(rows):
    return {"maw_pitch_max": max(r["pitch"] for r in rows), "maw_pitch_min": min(r["pitch"] for r in rows),
            "jaw_max": max(r["jaw"] for r in rows),
            "bend_max": [max(r["bend"][i] for r in rows) for i in range(len(JOINTS))],
            "min_z": min(r["min_z"] for r in rows)}


# ------------------------------------------------------------------------------------------------------------
# Renders
# ------------------------------------------------------------------------------------------------------------

ROAR_FRAMES = [0, 6, 12, 18, 24, 32, 42, 52, 59, 66]
THROW_FRAMES = [12, 16, 20, 24, 28, 30, 34, 40, 48]
HEADINGS = [("toward", 0.0), ("side", 90.0), ("away", 180.0)]


def scene_setup():
    """World, a light rig on a turntable empty (a heading turns the camera and the lights the other way round the
    creature, which equals turning the creature under the fixed play camera and sun) and the ground."""
    for o in list(bpy.data.objects):
        if o.type in ("LIGHT", "CAMERA") or o.name.startswith(("Ground", "LightTurn")):
            bpy.data.objects.remove(o)
    C.preview_world()
    turn = C.link(bpy.data.objects.new("LightTurn", None))
    for o in C.light_rig(scale=2.0) or [o for o in bpy.data.objects if o.type == "LIGHT"]:
        o.parent = turn
    C.ground(color=(0.07, 0.065, 0.08))
    for m in bpy.data.materials:
        m.use_backface_culling = True
    return turn


def heading(turn, yaw_deg):
    turn.rotation_euler.z = -math.radians(yaw_deg)
    bpy.context.view_layer.update()


def strip(arm, clip, frames, cam_fn, tag, out):
    paths = []
    act = bpy.data.actions[clip]
    for f in frames:
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        cam = cam_fn()
        p = os.path.join(out, f"{tag}_{f:03d}.png")
        paths.append(C.render(cam, p, 480, 300, samples=16))
        bpy.data.objects.remove(cam)
    return paths


def renders(arm, tag, out_dir):
    """Roar key frames from the play camera (orthographic, 44 degrees down) at three headings and from a 3/4
    close-up; then Surface_Throw at the side heading. Rows: toward, side, away, close; columns: ROAR_FRAMES."""
    os.makedirs(out_dir, exist_ok=True)
    turn = scene_setup()
    paths = []
    for label, yaw in HEADINGS:
        heading(turn, yaw)
        paths += strip(arm, "Roar", ROAR_FRAMES,
                       lambda yaw=yaw: C.game_camera(Vector((0, -0.6, 1.0)), 7.5, yaw=C.GAME_YAW - yaw),
                       f"roar_{label}", out_dir)
    heading(turn, 0.0)
    paths += strip(arm, "Roar", ROAR_FRAMES,
                   lambda: C.closeup_camera(Vector((0, -1.4, 1.2)), 7.5, azimuth=-40, elevation=20, lens=50),
                   "roar_close", out_dir)
    sheet = os.path.join(QA, f"gravemaw_roar_rear_{tag}.png")
    C.contact_sheet(paths, sheet, len(ROAR_FRAMES), 320)
    throw = []
    for label, yaw in HEADINGS[:2]:
        heading(turn, yaw)
        throw += strip(arm, "Surface_Throw", THROW_FRAMES,
                       lambda yaw=yaw: C.game_camera(Vector((0, -2.0, 1.2)), 8.5, yaw=C.GAME_YAW - yaw),
                       f"throw_{label}", out_dir)
    heading(turn, 0.0)
    C.contact_sheet(throw, os.path.join(QA, f"gravemaw_throw_tip_{tag}.png"), len(THROW_FRAMES), 320)
    return sheet


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def report_path():
    return os.path.join(C.EXPORTS, NAME, NAME + "_report.json")


def update_report(facts):
    with open(report_path(), encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["roar_rear"] = facts
    with open(report_path(), "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", report_path())


def checks(arm, mesh, maw, label, out_dir):
    masks = [debris_free(mesh), None]
    roar = clip_stats(arm, [mesh, maw], masks, "Roar")
    throw = clip_stats(arm, [mesh, maw], masks, "Surface_Throw", range(10, 50, 2))
    idle = clip_stats(arm, [mesh, maw], masks, "Idle", [0])
    out = {"roar": summarise(roar), "throw": summarise(throw), "rest_min_z": idle[0]["min_z"],
           "roar_frames": roar, "throw_frames": throw}
    for clip, frames in (("Emerge", range(20, 60, 2)), ("Dive", range(0, 40, 2)), ("Gate_Mount", range(0, 169, 4))):
        out[f"ref_{clip}"] = summarise(clip_stats(arm, [mesh, maw], masks, clip, frames))
    out["throat_intrusions"] = {"Roar": throat_intrusions(arm, mesh, "Roar", range(14, 56, 4)),
                                "Surface_Throw": throat_intrusions(arm, mesh, "Surface_Throw", range(16, 44, 4))}
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, f"gravemaw_roar_rear_checks_{label}.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=1)
    C.log("checks", label, json.dumps({k: v for k, v in out.items() if not k.endswith("_frames")}))
    return out


def main():
    args = C.cli_args()
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm = bpy.data.objects[NAME + "_Rig"]
    mesh = bpy.data.objects[NAME]
    maw = bpy.data.objects[GMW.MAW]
    lod_arm = bpy.data.objects[NAME + "_LOD1_Rig"]
    lod_mesh = bpy.data.objects[NAME + "_LOD1"]
    maw_lod = bpy.data.objects[GMW.MAW_LOD]
    lod_arm.hide_render = lod_mesh.hide_render = maw_lod.hide_render = True
    scratch = args.get("out") or os.path.join(C.REPO, "ArtSource", "Previews", "QA", "_roar_cells")
    if args.get("qa"):
        renders(arm, "after", os.path.join(scratch, "after"))
        return
    if not args.get("dry"):
        if bpy.data.actions["Roar"].get("round39_rear"):
            raise RuntimeError("the .blend already has the round-39 Roar: restore the round-38 .blend first")
    before = checks(arm, mesh, maw, "before", scratch)
    if not args.get("norender"):
        renders(arm, "before", os.path.join(scratch, "before"))
    if args.get("before"):
        return
    rekey_roar(arm, lod_arm)
    tipped = tip_throw(arm, lod_arm)
    bpy.data.actions["Roar"]["round39_rear"] = True
    after = checks(arm, mesh, maw, "after", scratch)
    if not args.get("dry"):
        clips = GMW.export(arm, mesh, maw, lod_arm, lod_mesh, maw_lod)
        update_report({"note": VERSION_NOTE, "changed_clips": ["Roar", "Surface_Throw"],
                       "roar": {"frames": FRAMES, "event_frames_unchanged": True,
                                "beats": {"anticipation": [0, 13], "rise": [7, 23], "peak_spread": 22,
                                          "hold_shudder": [23, 49], "drop": [49, 59], "impact": 59,
                                          "rest": 66, "jaws_start_opening": 12},
                                "maw_pitch_deg_before": [before["roar"]["maw_pitch_min"], before["roar"]["maw_pitch_max"]],
                                "maw_pitch_deg_after": [after["roar"]["maw_pitch_min"], after["roar"]["maw_pitch_max"]],
                                "lowest_point_m": after["roar"]["min_z"], "rest_lowest_point_m": after["rest_min_z"]},
                       "surface_throw": {"head_tip_deg": round(math.degrees(THROW_TIP), 1),
                                         "frames": list(THROW_TIP_FRAMES), "keys_changed": tipped,
                                         "body_path_jaws_debris_unchanged": True,
                                         "maw_pitch_deg_before": before["throw"]["maw_pitch_max"],
                                         "maw_pitch_deg_after": after["throw"]["maw_pitch_max"]},
                       "untouched": "every other clip (Gate_*, Climb_*, Crawl, Idle, Hit, Death, Dive, Emerge, "
                                    "Burrow_*, Dive_Into_Liquid, Submerged_Loop, Surface_Burst)",
                       "exported_clips": clips})
        C.save_blend(NAME)
    if not args.get("norender"):
        renders(arm, "after", os.path.join(scratch, "after"))


if __name__ == "__main__":
    main()
