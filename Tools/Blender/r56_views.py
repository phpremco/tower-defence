"""Round 56 cleanup: before/after QA views of the small leftovers (bright light, 3x crops).

Opens ArtSource/Blend/<Name>.blend read-only (never saved), or with --from the round-56 backup
(ArtSource/Exports/_backup_r56/<Name>/<Name>.blend, its maps relinked to the backed-up copies), LOD1 hidden, and writes
ArtSource/Previews/QA/r56_<key>_<tag>_<panel>.png plus 3x crops (<panel>_x3.png) and a sheet r56_<key>_<tag>.png.

Keys (one per item):
  sealedone   the cracked right arm and shoulder (round 52b's arms shot); x3 crops of the top-left (the old smear) and
              the centre; the play camera
  skyray      Dive frame 8 on both fin tips, the spade, the mouth from the play camera and from straight above
  rayboss     (export StormMatriarch) the last Death frame on both fin tips, the spade, the mouth from above
  packalpha   the Howl peak (frame 22), the neck from four angles
  bulwark, crystalwarlord   the play camera framed and at play zoom, the mouth close-up; x3 crops on the head
  firelarva   the Crawl from the side (frames 0..20 every 4) on a grid floor
  bat         --skin Moss|Mire: the body maps swapped to Skins/<skin>/ (game view and 3/4 close)

Run: blender -b --factory-startup --python Tools/Blender/r56_views.py -- --only skyray,rayboss --tag before [--from]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from bpy_extras.object_utils import world_to_camera_view  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import r49_views as V  # noqa: E402
import r49c_views as V49C  # noqa: E402

QA = V.QA
BACKUP = os.path.join(C.EXPORTS, "_backup_r56")
NAMES = {"sealedone": "SealedOne", "skyray": "Skyray", "rayboss": "StormMatriarch", "packalpha": "PackAlpha",
         "bulwark": "Bulwark", "crystalwarlord": "CrystalWarlord", "firelarva": "FireLarva", "bat": "Bat"}
W, H = 1280, 720


def open_creature(name, backup):
    path = os.path.join(BACKUP, name, name + ".blend") if backup else os.path.join(C.BLEND, name + ".blend")
    bpy.ops.wm.open_mainfile(filepath=path)
    if backup:
        V49C.relink_backup_maps(os.path.join(BACKUP, name))
    arm = bpy.data.objects[name + "_Rig"]
    lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm
              and not o.name.endswith(("_High", "_Parts", "_Block"))]
    for o in bpy.data.objects:
        if o.type == "MESH" and o not in meshes:
            o.hide_render = True
        if lod_arm is not None and o == lod_arm:
            o.hide_render = True
    for o in meshes:
        o.hide_render = False
    return arm, meshes


def act(name):
    return bpy.data.actions[name]


def posed(arm, bone, share=1.0):
    pb = arm.pose.bones[bone]
    return (arm.matrix_world @ pb.head).lerp(arm.matrix_world @ pb.tail, share)


def crop(src, dst, zoom, u=0.5, v=0.5):
    """The 1/zoom window round (u, v) (0..1, v from the top) of a render, scaled back up `zoom` times."""
    img = bpy.data.images.load(src)
    w, h = img.size
    px = np.array(img.pixels[:]).reshape(h, w, 4)
    cw, ch = w // zoom, h // zoom
    x0 = int(min(max(u * w - cw / 2, 0), w - cw))
    y0 = int(min(max((1 - v) * h - ch / 2, 0), h - ch))
    sub = px[y0:y0 + ch, x0:x0 + cw]
    big = np.repeat(np.repeat(sub, zoom, axis=0), zoom, axis=1)
    out = bpy.data.images.new("crop", big.shape[1], big.shape[0], alpha=True)
    out.pixels = big.ravel()
    out.filepath_raw = dst
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(img)
    bpy.data.images.remove(out)
    return dst


def screen_uv(cam, p):
    co = world_to_camera_view(bpy.context.scene, cam, p)
    return co.x, 1.0 - co.y


class Shots:
    def __init__(self, key, tag, arm, meshes):
        self.key, self.tag, self.arm, self.meshes = key, tag, arm, meshes
        self.paths = []

    def path(self, panel):
        return os.path.join(QA, f"r56_{self.key}_{self.tag}_{panel}.png")

    def shot(self, panel, clip, frame, camera_fn, crops=(), w=W, h=H):
        """camera_fn(centre) -> camera; crops: [(suffix, point or (u, v))] rendered as 3x crops round that point."""
        C.set_action(self.arm, act(clip), frame)
        lo, hi = V.bounds(self.meshes)
        cam = camera_fn((lo + hi) * 0.5)
        bpy.context.scene.render.resolution_x, bpy.context.scene.render.resolution_y = w, h
        out = C.render(cam, self.path(panel), w, h)
        self.paths.append(out)
        for suffix, at in crops:
            uv = at if isinstance(at, tuple) else screen_uv(cam, at)
            self.paths.append(crop(out, self.path(f"{panel}{suffix}_x3"), 3, *uv))
        return cam

    def sheet(self):
        C.contact_sheet(self.paths, os.path.join(QA, f"r56_{self.key}_{self.tag}.png"), 2, 640)


def scene_for(arm, meshes, clip, frame, flyer=False):
    C.set_action(arm, act(clip), frame)
    lo, hi = V.bounds(meshes)
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    V.bright_scene(size, lo.z - (0.35 * size if flyer else 0.0))
    return size


def top_camera(target, ortho):
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    data = bpy.data.cameras.new("TopCam")
    data.type, data.ortho_scale, data.clip_end = "ORTHO", ortho, 200
    cam = C.link(bpy.data.objects.new("TopCam", data))
    cam.location = Vector(target) + Vector((0, -0.001, 30))
    C.look_at(cam, target)
    return cam


# ------------------------------------------------------------------------------------------------------------

def sealedone(s, arm, meshes):
    size = scene_for(arm, meshes, "Idle", 30)
    el = posed(arm, "LArm_R_Fore", 0.0)
    sh = posed(arm, "LArm_R_Upper", 0.0)
    s.shot("arms", "Idle", 30, lambda c: V.cam("close", el.lerp(sh, 0.35), size, -35, 20, size * 0.5),
           crops=[("_topleft", (0.2, 0.25)), ("_centre", (0.5, 0.5))])
    # The left shoulder and mantle too (the smear sat on the near shoulder in the round-52b shot).
    shl = posed(arm, "LArm_L_Upper", 0.0)
    s.shot("shoulder_l", "Idle", 30, lambda c: V.cam("close", shl, size, 40, 25, size * 0.45),
           crops=[("_centre", (0.5, 0.5))])
    s.shot("game", "Idle", 30, lambda c: V.cam("game", c, size * 1.9), crops=[("_arm", sh)])


def fins(s, arm, meshes, clip, frame, size):
    for side, az in (("L", 60), ("R", -60)):
        tip = posed(arm, f"Fin_{side}_A3")
        s.shot(f"fin_{side.lower()}_{clip.lower()}{frame}", clip, frame,
               lambda c, tip=tip, az=az: V.cam("close", tip, size, az, 12, size * 0.32), crops=[("", (0.5, 0.5))])


def mouth_views(s, arm, meshes, size):
    C.set_action(arm, act("Idle"), 15)
    head = posed(arm, "Head", 0.0)
    tip = posed(arm, "Head", 1.0)
    s.shot("game", "Fly", 10, lambda c: V.cam("game", c, size * 1.25), crops=[("_mouth", tip)])
    s.shot("top_mouth", "Idle", 15, lambda c: top_camera(head.lerp(tip, 0.7), size * 0.5),
           crops=[("", tip)])
    s.shot("game_mouth", "Idle", 15, lambda c: V.cam("game", head.lerp(tip, 0.7), size * 0.5), crops=[("", tip)])


def spade(s, arm, size):
    end = posed(arm, "Tail_5", 1.0)
    s.shot("spade", "Idle", 15, lambda c: V.cam("close", end, size, -140, 30, size * 0.12), crops=[("", (0.5, 0.5))])
    s.shot("spade_top", "Idle", 15, lambda c: V.cam("close", end, size, 10, 70, size * 0.12), crops=[("", (0.5, 0.5))])


def skyray(s, arm, meshes):
    size = scene_for(arm, meshes, "Idle", 15, flyer=True)
    C.set_action(arm, act("Dive"), 8)
    fins(s, arm, meshes, "Dive", 8, size)
    C.set_action(arm, act("Idle"), 15)
    spade(s, arm, size)
    mouth_views(s, arm, meshes, size)


def rayboss(s, arm, meshes):
    size = scene_for(arm, meshes, "Idle", 15, flyer=True)
    last = int(act("Death").frame_range[1])
    C.set_action(arm, act("Death"), last)
    fins(s, arm, meshes, "Death", last, size)
    C.set_action(arm, act("Idle"), 15)
    spade(s, arm, size)
    mouth_views(s, arm, meshes, size)


# The two 4-vertex fragments by the neck (rest, model units; r56_drop_specks.py found them 1.5 cm off the skin).
PACK_SPECK = Vector((0.067, -0.49, 0.602))


def carried(arm, bone, p):
    pb = arm.pose.bones[bone]
    return arm.matrix_world @ pb.matrix @ arm.data.bones[bone].matrix_local.inverted() @ Vector(p)


def packalpha(s, arm, meshes):
    size = scene_for(arm, meshes, "Idle", 15)
    C.set_action(arm, act("Howl"), 22)
    for side in (1, -1):
        spot = carried(arm, "Neck", Vector((PACK_SPECK.x * side, PACK_SPECK.y, PACK_SPECK.z)))
        for az, el in ((70 * side, 10), (150 * side, -15)):
            s.shot(f"howl22_{'r' if side > 0 else 'l'}_{abs(az)}_{el}", "Howl", 22,
                   lambda c, az=az, el=el, spot=spot: V.cam("close", spot, size, az, el, size * 0.16),
                   crops=[("", (0.5, 0.5))])


def walker(s, arm, meshes):
    size = scene_for(arm, meshes, "Idle", 15)
    C.set_action(arm, act("Idle"), 15)
    head = posed(arm, "Head", 0.75)
    s.shot("game", "Idle", 15, lambda c: V.cam("game", c, size * 1.15), crops=[("_head", head)])
    s.shot("play", "Idle", 15, lambda c: V.cam("game", c, size * 3.0), crops=[("_head", head)])
    s.shot("mouth", "Idle", 15, lambda c: V.cam("close", head, size, -25, 12, size * 0.42), crops=[("", (0.5, 0.5))])
    s.shot("mouth_front", "Idle", 15, lambda c: V.cam("close", head, size, -5, 30, size * 0.42))


def firelarva(s, arm, meshes):
    size = scene_for(arm, meshes, "Crawl", 0)
    n = int(act("Crawl").frame_range[1])
    for f in range(0, n + 1, 4):
        s.shot(f"crawl_{f:02d}", "Crawl", f, lambda c: V.cam("close", Vector((c.x, c.y, size * 0.15)), size, -90, 4,
                                                              size * 1.6), w=640, h=360)


def bat(s, arm, meshes, skin):
    folder = os.path.join(C.EXPORTS, "Bat", "Skins", skin)
    for img in bpy.data.images:
        base = os.path.basename(bpy.path.abspath(img.filepath)) if img.filepath else ""
        for kind in ("Albedo", "Normal", "Emission", "Mask"):
            if base == f"Bat_{kind}.png":
                low = "low" in img.filepath.replace("\\", "/").lower().split("/")[-2:-1]
                img.filepath = os.path.join(folder, "Low" if low else "", f"Bat_{skin}_{kind}.png")
                img.reload()
    size = scene_for(arm, meshes, "Glide", 10, flyer=True)
    s.shot("close", "Glide", 10, lambda c: V.cam("close", c, size, -38, 30, size * 1.3), crops=[("", (0.5, 0.5))])
    s.shot("game", "Fly", 4, lambda c: V.cam("game", c, size * 1.6), crops=[("", (0.5, 0.5))])


def main():
    args = C.cli_args()
    tag = args.get("tag") or "after"
    backup = bool(args.get("from"))
    os.makedirs(QA, exist_ok=True)
    for key in (args.get("only") or "").split(","):
        if not key:
            continue
        arm, meshes = open_creature(NAMES[key], backup)
        s = Shots(key if key != "bat" else "bat_mire", tag, arm, meshes)
        if key == "sealedone":
            sealedone(s, arm, meshes)
        elif key == "skyray":
            skyray(s, arm, meshes)
        elif key == "rayboss":
            rayboss(s, arm, meshes)
        elif key == "packalpha":
            packalpha(s, arm, meshes)
        elif key in ("bulwark", "crystalwarlord"):
            walker(s, arm, meshes)
        elif key == "firelarva":
            firelarva(s, arm, meshes)
        elif key == "bat":
            bat(s, arm, meshes, args.get("skin") or "Moss")
        s.sheet()
        C.log("r56 views", key, tag, len(s.paths))


if __name__ == "__main__":
    main()
