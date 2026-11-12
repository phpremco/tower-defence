"""Round 49H: QA for the ray boss's (StormMatriarch export) scorpion tail shot, the reworked `Release` clip.

Opens ArtSource/Blend/StormMatriarch.blend (never saved) and, for the full rig and the LOD1 rig:
  - the per-frame bone turn in every clip (armature space, the round-49A measure) and the local turn of each bone,
    with the ratio to that bone's median step (a one- or two-frame snap stands out);
  - the Emitter bone per Release frame: its head (the beam origin), its +Y (the firing direction), how far below the
    horizon it points, and how close the beam's first 3 model units pass to the body (every vertex not led by a tail
    bone); and how close the curled tail comes to the body and the head (a tail through the back would show here);
  - the Body bone's pitch and roll range in Fly and Idle (the boss must fly level outside Release);
  - renders (bright light, a glowing line drawn from Emitter along its +Y):
      r49h_rayboss_release_sheet.png  eight Release frames, side view (top row) and play camera (bottom row)
      r49h_rayboss_release_game.png   the aim pose at the release frame from the play camera
Writes ArtSource/Previews/QA/r49h_rayboss_release_check.json.

`--proto` instead adds the Emitter bone to the old rig in memory and keys the Release clip from the current build
script, for a fast look before the rebuild (renders and json go to `--out`, default Logs/r49h).

Run: blender -b --factory-startup --python Tools/Blender/r49h_rayboss.py -- [--proto] [--out <dir>] [--norender]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_stormmatriarch as SM  # noqa: E402
import common as C  # noqa: E402
import r49_views as V  # noqa: E402

NAME = "StormMatriarch"
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
SHEET_FRAMES = (0, 8, 14, 22, 30, 38, 46, 50)
BEAM_LENGTH = 3.0
TAIL_PREFIX = ("Tail_", "Emitter")
# The far tail (the part that curls over the back): its gap to the body and head is measured.
FAR_TAIL = ("Tail_3", "Tail_4", "Tail_5", "Emitter")


def add_emitter(arm):
    """--proto: the new bone on the old rig, as build_stormmatriarch.bones() makes it."""
    C.activate(arm)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.data.edit_bones.new("Emitter")
    eb.head, eb.tail = SM.EMITTER
    eb.roll = 0.0
    eb.parent = arm.data.edit_bones["Tail_5"]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"


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


def clip_table(arm, suffix=""):
    table = {}
    for act in bpy.data.actions:
        if not act.name.endswith(suffix) or (suffix == "" and act.name.endswith(("_LOD", "_Full", "_Away"))):
            continue
        if suffix == "" and "." in act.name:
            continue
        s = steps(arm, act)
        worst = max(s.items(), key=lambda kv: kv[1]["max_arm_deg"])
        info = {k: v for k, v in worst[1].items() if k != "spikes"}
        table[act.name] = dict(worst_bone=worst[0], **info,
                               spikes={b: v["spikes"] for b, v in s.items() if v["spikes"]},
                               max_local=max(v["max_local_deg"] for v in s.values()))
    return table


def dominant_masks(mesh_obj, prefixes=TAIL_PREFIX):
    """Per vertex: True where the vertex is led by a bone whose name starts with one of `prefixes`."""
    me = mesh_obj.data
    names = {g.index: g.name for g in mesh_obj.vertex_groups}
    tail = np.zeros(len(me.vertices), dtype=bool)
    for v in me.vertices:
        best, w = None, 0.0
        for g in v.groups:
            if g.weight > w:
                best, w = names[g.group], g.weight
        tail[v.index] = best is not None and best.startswith(prefixes)
    return tail


def posed_points(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    pts = np.empty(len(me.vertices) * 3)
    me.vertices.foreach_get("co", pts)
    ev.to_mesh_clear()
    pts = pts.reshape(-1, 3)
    mw = np.array(obj.matrix_world)
    return pts @ mw[:3, :3].T + mw[:3, 3]


def seg_dist(points, a, b):
    ab = b - a
    t = np.clip(((points - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
    return np.linalg.norm(points - (a + t[:, None] * ab), axis=1)


def min_set_dist(p, q, chunk=600):
    best = 1e9
    for i in range(0, len(p), chunk):
        d = np.linalg.norm(p[i:i + chunk, None, :] - q[None, :, :], axis=2)
        best = min(best, float(d.min()))
    return best


def emitter_world(arm):
    pb = arm.pose.bones["Emitter"]
    head = arm.matrix_world @ pb.head
    tail = arm.matrix_world @ pb.tail
    return head, (tail - head).normalized()


def release_track(arm, body, act, tail_mask, far_mask):
    """Per frame: Emitter head and direction (Blender world: -Y forward, +Z up), the dive angle below the horizon,
    the forward/side heading, the beam's nearest pass to the non-tail body, and the curled tail's nearest pass."""
    out = []
    n = int(act.frame_range[1])
    sub = np.nonzero(~tail_mask)[0][::2]
    for f in range(0, n + 1):
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        h, d = emitter_world(arm)
        pts = posed_points(body)
        rest = pts[sub]
        ha, hb = np.array(h), np.array(h + d * BEAM_LENGTH)
        beam = float(seg_dist(rest, ha, hb).min())
        tail_pts = pts[np.nonzero(far_mask)[0][::3]]
        tail_gap = min_set_dist(tail_pts, rest[::2])
        down = math.degrees(math.asin(max(-1.0, min(1.0, -d.z))))
        out.append(dict(frame=f, origin=[round(v, 3) for v in h], dir=[round(v, 3) for v in d],
                        below_horizon_deg=round(down, 1), forward=round(-d.y, 3), beam_clear=round(beam, 3),
                        tail_body_gap=round(tail_gap, 3)))
    return out


def level_check(arm):
    out = {}
    for clip in ("Fly", "Idle"):
        act = bpy.data.actions.get(clip)
        if act is None:
            continue
        pitches, rolls = [], []
        for f in range(int(act.frame_range[0]), int(act.frame_range[1]) + 1):
            C.set_action(arm, act, f)
            # The Body bone points forward, so its +Y is the nose direction and its +X the right side.
            fwd = (arm.matrix_world.to_3x3() @ (arm.pose.bones["Body"].matrix.to_3x3() @ Vector((0, 1, 0)))).normalized()
            side = (arm.matrix_world.to_3x3() @ (arm.pose.bones["Body"].matrix.to_3x3() @ Vector((1, 0, 0)))).normalized()
            pitches.append(math.degrees(math.asin(fwd.z)))
            rolls.append(math.degrees(math.asin(side.z)))
        out[clip] = dict(pitch_deg=[round(min(pitches), 1), round(max(pitches), 1)],
                         roll_deg=[round(min(rolls), 1), round(max(rolls), 1)])
    return out


def beam_object():
    """A thin glowing rod (unit length along +Z) and a ball at its start: placed per frame on the Emitter."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.025, depth=1.0)
    rod = bpy.context.active_object
    rod.name = "QA_Beam"
    rod.data.transform(C.Matrix.Translation((0, 0, 0.5)))
    mat = C.principled("QA_BeamMat", (0.5, 1.0, 1.0), rough=0.3, emission=(0.45, 1.0, 1.0), strength=6.0)
    rod.data.materials.append(mat)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, segments=16, ring_count=8)
    ball = bpy.context.active_object
    ball.name = "QA_BeamStart"
    ball.data.materials.append(mat)
    return rod, ball


def place_beam(arm, rod, ball, on):
    h, d = emitter_world(arm)
    rod.matrix_world = (C.Matrix.Translation(h) @ Vector((0, 0, 1)).rotation_difference(d).to_matrix().to_4x4()
                        @ C.Matrix.Diagonal((1, 1, BEAM_LENGTH, 1)))
    ball.location = h
    rod.hide_render = not on
    ball.hide_render = False


def renders(arm, meshes, act, out_dir, prefix):
    C.set_action(arm, act, 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y)
    ground_z = lo.z - 0.6 * size
    V.bright_scene(size, ground_z)
    centre = (lo + hi) * 0.5 + Vector((0, 0, 0.15 * size))
    rod, ball = beam_object()
    tmp = os.path.join(out_dir, "_r49h_tmp")
    side, game = [], []
    for f in SHEET_FRAMES:
        C.set_action(arm, act, f)
        bpy.context.view_layer.update()
        on = SM.RELEASE_HOLD[0] <= f <= SM.RELEASE_HOLD[1]
        place_beam(arm, rod, ball, True)
        rod.hide_render = False
        # The aim line is always drawn (thin), so the aim shows in wind-up and recovery too; the hold frames are noted.
        side.append(C.render(V.cam("close", centre, size, -90, 4, size * 1.9), os.path.join(tmp, f"side_{f}.png"),
                             640, 400, samples=16))
        game.append(C.render(V.cam("game", centre + Vector((0, -0.25 * size, 0)), size * 1.7),
                             os.path.join(tmp, f"game_{f}.png"), 640, 400, samples=16))
        C.log("frame", f, "beam on" if on else "beam off")
    sheet = os.path.join(out_dir, f"{prefix}_release_sheet.png")
    C.contact_sheet(side + game, sheet, len(SHEET_FRAMES), 360)
    C.set_action(arm, act, SM.RELEASE_FRAME + 4)
    bpy.context.view_layer.update()
    place_beam(arm, rod, ball, True)
    big = C.render(V.cam("game", centre + Vector((0, -0.25 * size, 0)), size * 1.5),
                   os.path.join(out_dir, f"{prefix}_release_game.png"), 1280, 720)
    side_big = C.render(V.cam("close", centre, size, -62, 10, size * 1.6),
                        os.path.join(out_dir, f"{prefix}_release_aim34.png"), 1280, 720)
    for p in side + game:
        os.remove(p)
    return [sheet, big, side_big]


def main():
    args = C.cli_args()
    proto = bool(args.get("proto"))
    out_dir = os.path.join(C.REPO, args.get("out") or (os.path.join("Logs", "r49h") if proto else QA))
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm = bpy.data.objects[NAME + "_Rig"]
    lod_arm = bpy.data.objects.get(NAME + "_LOD1_Rig")
    body = bpy.data.objects[NAME]
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        elif o.type == "MESH" and o not in meshes:
            o.hide_render = True
    if proto:
        add_emitter(arm)
        poser = B.ScalePoser(arm)
        frames, spec, loop = SM.release_spec(poser)
        act = poser.action("Release_49H", frames, spec, loop=loop)
    else:
        act = bpy.data.actions["Release"]
    report = {"release_frame": SM.RELEASE_FRAME, "release_hold_frames": list(SM.RELEASE_HOLD),
              "release_hold_loop_frames": list(SM.RELEASE_HOLD_LOOP)}
    report["release_steps"] = steps(arm, act)
    report["release_track"] = release_track(arm, body, act, dominant_masks(body), dominant_masks(body, FAR_TAIL))
    if not proto:
        report["clips_full"] = clip_table(arm)
        report["level"] = level_check(arm)
        if lod_arm is not None:
            report["lod_bones"] = [b.name for b in lod_arm.data.bones]
            lod_rel = bpy.data.actions.get("Release_LOD")
            if lod_rel is not None:
                report["release_steps_lod"] = steps(lod_arm, lod_rel)
                track = []
                for f in range(0, int(lod_rel.frame_range[1]) + 1):
                    C.set_action(lod_arm, lod_rel, f)
                    C.set_action(arm, act, f)
                    h1, d1 = emitter_world(lod_arm)
                    h0, d0 = emitter_world(arm)
                    track.append(dict(frame=f, origin_diff=round((h1 - h0).length, 4),
                                      dir_diff_deg=round(math.degrees(d1.angle(d0)), 2)))
                report["lod_emitter_vs_full"] = max(track, key=lambda r: r["origin_diff"])
                report["lod_emitter_max_dir_diff_deg"] = max(r["dir_diff_deg"] for r in track)
                report["clips_lod"] = clip_table(lod_arm, "_LOD")
    path = os.path.join(out_dir, "r49h_rayboss_release_check.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=1)
    worst = max(report["release_steps"].items(), key=lambda kv: kv[1]["max_arm_deg"])
    C.log("R49H worst Release step", worst)
    C.log("R49H spikes", {b: v["spikes"] for b, v in report["release_steps"].items() if v["spikes"]})
    for r in report["release_track"]:
        if r["frame"] % 2 == 0 or r["frame"] == SM.RELEASE_FRAME:
            C.log("R49H", r)
    if "level" in report:
        C.log("R49H level", report["level"])
    if not args.get("norender"):
        renders(arm, meshes, act, out_dir, "r49h_rayboss")


main()
