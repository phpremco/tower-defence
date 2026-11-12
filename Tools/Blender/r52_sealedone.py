"""Round 52: the Sealed One (concept B, "The opening seal") look passes and QA views.

--quick: the unbaked look pass. build_sealedone.py's parts and sealedone_detail.py's eyes, teeth and claws are fused at
  a coarse voxel with their styles as vertex colours, skinned on the real rig and posed with the real clips (no bake,
  nothing saved). Renders the play camera (orthographic, 44 degrees), a 3/4 close-up and a front look at the maw for
  each pose in --poses (default Idle:30, Summon:44) to ArtSource/Previews/QA/_r52_quick/<tag>_*.png and a sheet
  <tag>_sheet.png.

--views: after the full build (ArtSource/Blend/SealedOne.blend, opened read-only, LOD1 hidden, bright light):
  r52_sealedone_after_game|close|move|mouth.png      play camera / 3/4 close / walk mid-pose / maw open wide (Summon
                                                       peak) and their 2x2 sheet r52_sealedone_after.png
  r52_sealedone_after_mouth_clips.png                 per clip the frame where the maw is most closed and most open,
                                                       two angles each (rows: clips)
  r52_sealedone_loose_<clip>.png                      (--loose) per clip 4 frames (rows) x 4 angles, long lens
  --lod: the same panels of the LOD1 (r52_sealedone_after_lod_*.png).

Run: blender -b --factory-startup --python Tools/Blender/r52_sealedone.py -- --quick [--tag q1]
         [--poses Idle:30,Summon:44,Roar:30] [--voxel 0.02]
     blender -b --factory-startup --python Tools/Blender/r52_sealedone.py -- --views [--loose] [--mouth]
         [--panels] [--clips Walk,Idle]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import build_sealedone as BS  # noqa: E402
import common as C  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import detail_kit_b as D  # noqa: E402
import r49_quick as Q  # noqa: E402
import r49_views as V  # noqa: E402
import sealedone_detail as SOD  # noqa: E402

QA = V.QA
QUICK = os.path.join(QA, "_r52_quick")
PREFIX = "r52_sealedone"
LOOSE_ANGLES = ((-60, 6), (120, 6), (-150, 22), (35, 38))
CELL = (720, 450)
LENS = 120.0


# ------------------------------------------------------------------------------------------------------------
# Quick look
# ------------------------------------------------------------------------------------------------------------

def quick_object(name, builder, voxel, smooth, extra=None):
    raw = builder.to_object(name + "_Parts")
    mesh = C.fuse(raw, voxel, smooth)
    bpy.data.objects.remove(raw)
    C.detail(mesh, builder, extra=extra)
    C.shade_smooth(mesh)
    o = C.link(bpy.data.objects.new(name, mesh))
    o.data.materials.append(Q.vertex_material())
    return o


def quick_clips():
    import build_sealedone_mech as MECH
    clips = dict(BS.CLIPS)
    clips.update({"Roar": MECH.roar, "Release": MECH.release, "Walk": lambda p: MECH.walk(p), "Adapt": MECH.adapt,
                  "OnBase_Loop": MECH.on_base_loop})
    return clips


def maw_target(arm):
    """The posed centre of the maw (the throat), carried by the Head bone."""
    m = D.bone_space(arm, "Head")
    return m @ (BS.MAW_O + BS.D * 0.35)


def quick(args):
    tag = args.get("tag") or "q"
    voxel = float(args.get("voxel", 0.02))
    os.makedirs(QUICK, exist_ok=True)
    C.reset_scene()
    arm = C.build_armature("Q_Rig", BS.bones())
    body, seal = BS.body_parts(), BS.seal_parts()
    objs = [(quick_object("Q_Body", body, voxel, 4, BS.body_extra()), body, "body"),
            (quick_object("Q_Seal", seal, voxel * 0.8, 3, BS.seal_extra()), seal, "seal")]
    for suffix, fn, _ in SOD.MAKERS:
        b = fn(None)
        objs.append((quick_object("Q_" + suffix, b, 0.006, 1), b, "detail"))
    for o, b, kind in objs:
        if kind == "body":
            C.skin(o, arm, b, rigid=BS.BODY_RIGID, blend=2)
        elif kind == "seal":
            C.skin(o, arm, b, rigid=BS.SEAL_RIGID, blend=0)
        else:
            D.skin_rigid(o, arm, b)
    poser = K3.ClipPoser(arm, BS.SCALE_BONES + [f"Seal_{i + 1}" for i in range(7)])
    clips = quick_clips()
    poses = [p.split(":") for p in (args.get("poses") or "Idle:30,Summon:44").split(",")]
    wanted = {c for c, _ in poses}
    acts = K3.key_clips(poser, {c: clips[c] for c in wanted})
    meshes = [o for o, _, _ in objs]
    C.set_action(arm, acts[poses[0][0]], int(poses[0][1]))
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, 0.0)
    paths = []
    for clip, frame in poses:
        C.set_action(arm, acts[clip], int(frame))
        plo, phi = V.bounds(meshes)
        centre = (plo + phi) * 0.5
        maw = maw_target(arm)
        key = f"{tag}_{clip}{frame}"
        paths.append(C.render(V.cam("game", centre, size * 1.9), os.path.join(QUICK, key + "_game.png"), 1280, 720,
                              samples=24))
        paths.append(C.render(V.cam("close", maw.lerp(centre, 0.35), size, -38, 18, size * 0.85),
                              os.path.join(QUICK, key + "_close.png"), 1280, 720, samples=24))
        paths.append(C.render(V.cam("close", maw, size, -12, 22, size * 0.42), os.path.join(QUICK, key + "_mouth.png"),
                              1280, 720, samples=24))
    C.contact_sheet(paths, os.path.join(QUICK, f"{tag}_sheet.png"), 3, 640)
    C.log("quick", tag, len(paths), "renders")


# ------------------------------------------------------------------------------------------------------------
# Views of the built boss
# ------------------------------------------------------------------------------------------------------------

def open_boss(lod=False):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, BS.NAME + ".blend"))
    arm = bpy.data.objects[BS.NAME + ("_LOD1_Rig" if lod else "_Rig")]
    other = bpy.data.objects.get(BS.NAME + ("_Rig" if lod else "_LOD1_Rig"))
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if o.type == "MESH" and o not in meshes:
            o.hide_render = True
        if other is not None and o == other:
            o.hide_render = True
    for o in meshes:
        o.hide_render = False
    return arm, meshes


def clip_actions(arm, clips=None, lod=False):
    out = []
    for a in bpy.data.actions:
        if a.name.endswith(("_Full", "_Away")):
            continue
        is_lod = a.name.endswith("_LOD")
        if is_lod != lod:
            continue
        name = a.name[:-4] if is_lod else a.name
        if clips and name not in clips:
            continue
        out.append((name, a))
    return sorted(out)


def maw_opening(arm):
    """How far the maw stands open: the mean turn of the three petal bones from their rest (radians)."""
    return sum(arm.pose.bones[b].matrix_basis.to_quaternion().angle for b in BS.PETAL_NAMES) / 3.0


def frames_of(act):
    return range(int(act.frame_range[0]), int(act.frame_range[1]) + 1)


def panels(lod=False):
    arm, meshes = open_boss(lod)
    acts = dict(clip_actions(arm, lod=lod))
    C.set_action(arm, acts["Idle"], 30)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, 0.0)
    tag = "after_lod" if lod else "after"
    out = []

    def shot(panel, clip, frame, camera_fn):
        C.set_action(arm, acts[clip], frame)
        plo, phi = V.bounds(meshes)
        centre = (plo + phi) * 0.5
        out.append(C.render(camera_fn(centre, maw_target(arm)), os.path.join(QA, f"{PREFIX}_{tag}_{panel}.png"),
                            1280, 720))

    shot("game", "Idle", 30, lambda c, m: V.cam("game", c, size * 1.9))
    shot("close", "Idle", 30, lambda c, m: V.cam("close", m.lerp(c, 0.35), size, -38, 18, size * 0.85))
    shot("move", "Walk", 20, lambda c, m: V.cam("close", c, size, -72, 8, size * 1.45))
    shot("mouth", "Summon", BS.SUMMON_PEAK, lambda c, m: V.cam("close", m, size, -14, 24, size * 0.5))
    C.contact_sheet(out, os.path.join(QA, f"{PREFIX}_{tag}.png"), 2, 640)
    # The open maw from the play camera too (the read that matters in the game).
    C.set_action(arm, acts["Summon"], BS.SUMMON_PEAK)
    plo, phi = V.bounds(meshes)
    C.render(V.cam("game", (plo + phi) * 0.5, size * 1.9), os.path.join(QA, f"{PREFIX}_{tag}_game_summon.png"), 1280, 720)


def mouth_sheet(clips=None):
    arm, meshes = open_boss()
    acts = clip_actions(arm, clips)
    C.set_action(arm, dict(acts)["Idle"], 30)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, 0.0)
    tmp = os.path.join(QA, "_r52_tmp")
    os.makedirs(tmp, exist_ok=True)
    paths, facts = [], {}
    for name, act in acts:
        opening = []
        for f in frames_of(act):
            C.set_action(arm, act, f)
            opening.append((maw_opening(arm), f))
        lo_f, hi_f = min(opening)[1], max(opening)[1]
        facts[name] = (round(math.degrees(min(opening)[0]), 1), lo_f, round(math.degrees(max(opening)[0]), 1), hi_f)
        for f in (lo_f, hi_f):
            C.set_action(arm, act, f)
            m = maw_target(arm)
            for az, el in ((-16, 22), (-60, 30)):
                cam = V.cam("close", m, size, az, el, size * 0.55)
                paths.append(C.render(cam, os.path.join(tmp, f"mouth_{name}_{f}_{az}.png"), 640, 400, samples=24))
    C.contact_sheet(paths, os.path.join(QA, f"{PREFIX}_after_mouth_clips.png"), 4, 640)
    C.log("mouth sheet (clip: min deg, frame, max deg, frame)", facts)


def tele_camera(target, radius, azimuth, elevation, aspect=CELL[0] / CELL[1], margin=0.62):
    half_v = math.atan(18.0 / LENS / aspect)
    distance = radius * margin / math.sin(half_v)
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    return C.closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=LENS)


def loose_sheets(clips=None):
    arm, meshes = open_boss()
    acts = clip_actions(arm, clips)
    C.set_action(arm, dict(acts)["Idle"], 0)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z)
    tmp = os.path.join(QA, "_r52_tmp")
    os.makedirs(tmp, exist_ok=True)
    for name, act in acts:
        n = int(act.frame_range[1])
        frames = sorted({int(round(i * n / 3)) for i in range(4)})
        paths = []
        for f in frames:
            C.set_action(arm, act, f)
            plo, phi = V.bounds(meshes)
            centre = (plo + phi) * 0.5
            radius = (phi - plo).length * 0.5
            for az, el in LOOSE_ANGLES:
                cam = tele_camera(centre, radius, az, el)
                paths.append(C.render(cam, os.path.join(tmp, f"loose_{name}_{f}_{az}.png"), *CELL, samples=24))
        out = os.path.join(QA, f"{PREFIX}_loose_{name}.png")
        C.contact_sheet(paths, out, len(LOOSE_ANGLES), CELL[0])
        C.log("loose sheet", out, "frames", frames)


def main():
    args = C.cli_args()
    if args.get("quick"):
        quick(args)
        return
    clips = [c for c in (args.get("clips") or "").split(",") if c] or None
    every = not (args.get("panels") or args.get("loose") or args.get("mouth"))
    if every or args.get("panels"):
        panels()
        if args.get("lod"):
            panels(lod=True)
    if every or args.get("mouth"):
        mouth_sheet(clips)
    if every or args.get("loose"):
        loose_sheets(clips)


if __name__ == "__main__":
    main()
