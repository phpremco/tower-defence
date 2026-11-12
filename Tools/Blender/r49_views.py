"""Round 49A QA views: one bright sheet per creature to judge its shape (Remco: the in-game shots were too dark).

Opens ArtSource/Blend/<Name>.blend (body plus the round-45/46 Face/Claws parts, LOD hidden) and renders four
panels, written as ArtSource/Previews/QA/r49_<name>_<tag>.png (sheet) and r49_<name>_<tag>_<panel>.png:
  game   the play camera's angle (orthographic, 44 degrees down, yaw 12), framed on the creature, mid-move pose
  close  a 3/4 perspective close-up of the head, standing clip
  move   a low 3/4 side view of the walk/fly/crawl mid-pose
  front  head-on, slightly from below for flyers (the ray's mouth), from above for the walkers

Run: blender -b --factory-startup --python Tools/Blender/r49_views.py -- --only Prowler,Skyray --tag before
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")

# name: (move clip, move frame, stand clip, flyer)
VIEWS = {
    "Prowler": ("Run", 5, "Idle", False),
    "PackAlpha": ("Run", 6, "Idle", False),
    "Skyray": ("Fly", 10, "Idle", True),
    "StormMatriarch": ("Fly", 14, "Idle", True),
    "Oozer": ("Crawl", 10, "Idle", False),
    "SlimeMonarch": ("Crawl", 10, "Idle", False),
}


def bright_scene(size, ground_z):
    for o in list(bpy.data.objects):
        if o.type in ("LIGHT", "CAMERA") or o.name.startswith("Ground"):
            bpy.data.objects.remove(o)
    C.preview_world(color=(0.16, 0.17, 0.2), strength=1.0)
    key = bpy.data.lights.new("Key", "SUN")
    key.energy, key.angle, key.color = 4.2, math.radians(8), (1.0, 0.97, 0.92)
    o = C.link(bpy.data.objects.new("Key", key))
    o.rotation_euler = (math.radians(48), 0, math.radians(-30))
    fill = bpy.data.lights.new("Fill", "SUN")
    fill.energy, fill.color, fill.use_shadow = 1.6, (0.75, 0.82, 1.0), False
    o = C.link(bpy.data.objects.new("Fill", fill))
    o.rotation_euler = (math.radians(60), 0, math.radians(150))
    rim = bpy.data.lights.new("Rim", "AREA")
    rim.energy, rim.size, rim.color = 160 * size * size, 2.5 * size, (0.7, 0.85, 1.0)
    o = C.link(bpy.data.objects.new("Rim", rim))
    o.location = Vector((1.2, 3.0, 2.2)) * size + Vector((0, 0, ground_z))
    C.look_at(o, Vector((0, 0, ground_z + 0.3 * size)))
    C.ground(color=(0.2, 0.21, 0.24), z=ground_z)
    for m in bpy.data.materials:
        m.use_backface_culling = True


def bounds(objs):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for ob in objs:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            p = ev.matrix_world @ v.co
            lo = Vector(map(min, lo, p))
            hi = Vector(map(max, hi, p))
        ev.to_mesh_clear()
    return lo, hi


def cam(kind, target, size, azimuth=-35, elevation=22, distance=None):
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    if kind == "game":
        return C.game_camera(target, size)
    return C.closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=50)


def run(name, tag):
    move, mframe, stand, flyer = VIEWS[name]
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        elif o.type == "MESH" and o not in meshes:
            o.hide_render = True
    C.set_action(arm, bpy.data.actions[stand], 15)
    lo, hi = bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y)
    ground_z = lo.z - (0.35 * size if flyer else 0.0)
    bright_scene(size, ground_z)
    centre = (lo + hi) * 0.5
    head = arm.matrix_world @ arm.pose.bones["Head"].head
    head_tail = arm.matrix_world @ arm.pose.bones["Head"].tail
    face = head.lerp(head_tail, 0.6)
    out = []
    os.makedirs(QA, exist_ok=True)
    low = name.lower()

    def shot(panel, act, frame, camera):
        C.set_action(arm, bpy.data.actions[act], frame)
        path = os.path.join(QA, f"r49_{low}_{tag}_{panel}.png")
        out.append(C.render(camera, path, 1280, 720))

    shot("game", move, mframe, cam("game", centre, size * 1.25))
    shot("close", stand, 15, cam("close", face.lerp(centre, 0.35), size, -38, 20, size * 1.05))
    shot("move", move, mframe, cam("close", centre, size, -68, 8, size * 1.55))
    if flyer:
        shot("front", stand, 15, cam("close", face, size, -8, -10, size * 1.0))
    else:
        shot("front", stand, 15, cam("close", face.lerp(centre, 0.3), size, -6, 14, size * 1.0))
    # A close look at the mouth from the play camera's side of the head (front, from above), standing clip.
    tip = arm.matrix_world @ arm.pose.bones["Head"].tail
    shot("mouth", stand, 15, cam("close", face.lerp(tip, 0.6), size, -25, 30 if not flyer else 20, size * 0.42))
    C.contact_sheet(out, os.path.join(QA, f"r49_{low}_{tag}.png"), 2, 640)
    C.log(name, tag, "views", len(out))


def clip_steps(arm):
    """Per clip: the largest per-frame turn of any bone (armature space, degrees), where it happens, and how it
    compares with that bone's median step in the clip (a one-frame snap stands out as a large ratio)."""
    import json
    out = {}
    for act in bpy.data.actions:
        if act.name.endswith(("_LOD", "_Full", "_Away")):
            continue
        start, end = int(act.frame_range[0]), int(act.frame_range[1])
        prev, steps = {}, {}
        for f in range(start, end + 1):
            C.set_action(arm, act, f)
            for pb in arm.pose.bones:
                q = pb.matrix.to_quaternion()
                if pb.name in prev:
                    ang = math.degrees(prev[pb.name].rotation_difference(q).angle)
                    steps.setdefault(pb.name, []).append((min(ang, 360.0 - ang), f))
                prev[pb.name] = q
        worst = (0.0, None, None, 0.0)
        for bone, st in steps.items():
            vals = sorted(v for v, _ in st)
            med = max(vals[len(vals) // 2], 0.5)
            for v, f in st:
                if v > worst[0]:
                    worst = (v, bone, f, v / med)
        out[act.name] = {"frames": end - start, "max_step_deg": round(worst[0], 1), "bone": worst[1],
                         "frame": worst[2], "ratio_to_median": round(worst[3], 1)}
    return out


def clips_sheet(name, tag):
    """A side-view strip of every clip (four frames each) plus the per-frame step table."""
    import json
    move, mframe, stand, flyer = VIEWS[name]
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if lod_arm is not None and (o == lod_arm or o.parent == lod_arm):
            o.hide_render = True
        elif o.type == "MESH" and o not in meshes:
            o.hide_render = True
    steps = clip_steps(arm)
    with open(os.path.join(QA, f"r49_{name.lower()}_{tag}_clip_steps.json"), "w", encoding="utf-8") as fh:
        json.dump(steps, fh, indent=2)
    C.set_action(arm, bpy.data.actions[stand], 0)
    lo, hi = bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y)
    ground_z = lo.z - (0.35 * size if flyer else 0.0)
    bright_scene(size, ground_z)
    centre = (lo + hi) * 0.5
    paths = []
    for clip in steps:
        act = bpy.data.actions[clip]
        n = int(act.frame_range[1])
        for i, f in enumerate((0, n // 3, (2 * n) // 3, n)):
            C.set_action(arm, act, f)
            camera = cam("close", centre, size, -60, 14, size * 1.7)
            paths.append(C.render(camera, os.path.join(QA, "_r49_tmp", f"{name}_{clip}_{i}.png"), 640, 360, samples=16))
    C.contact_sheet(paths, os.path.join(QA, f"r49_{name.lower()}_{tag}_clips.png"), 4, 400)
    C.log(name, json.dumps(steps))


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    for name in (args.get("only") or ",".join(VIEWS)).split(","):
        if name:
            if args.get("clips"):
                clips_sheet(name, tag)
            else:
                run(name, tag)


if __name__ == "__main__":
    main()
