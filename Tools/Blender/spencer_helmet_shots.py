"""Helmet close-ups for Spencer (helmet rebuild, build_mercer5.py): the camera rides on the head bone and frames the
visor from every side the helmet is seen from (front, Remco's low front-left, front-left, front-right, side, the
game camera's three-quarter above, below like the bust), plus high-zoom shots of the opening's lower corners and a
game-camera shot (44 degree ortho) zoomed in.

Run on a saved hero:
  blender --background --factory-startup --python Tools/Blender/spencer_helmet_shots.py -- --blend <Mercer.blend>
          --out <dir> --prefix helmet2_after [--fast]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402

TARGET = Vector((0.0, -0.03, 1.47))           # the helmet's middle, head rest frame
# (name, azimuth, elevation, distance, lens, target offset in the head rest frame)
SHOTS = (("front", 0, 0, 0.62, 85, (0, 0, 0)),
         ("remco", -24, -6, 0.5, 85, (0, 0, -0.01)),
         ("front_left", -32, 4, 0.62, 85, (0, 0, 0)),
         ("front_right", 32, 4, 0.62, 85, (0, 0, 0)),
         ("side_left", -78, 6, 0.66, 85, (0, 0, 0)),
         ("side_right", 78, 6, 0.66, 85, (0, 0, 0)),
         ("game_above", -35, 44, 0.66, 85, (0, 0, 0)),
         ("below", -14, -32, 0.62, 85, (0, 0, -0.02)),
         ("corner_left", 28, -8, 0.36, 135, (0.08, -0.06, -0.055)),
         ("corner_right", -28, -8, 0.36, 135, (-0.08, -0.06, -0.055)),
         ("chin", 0, -14, 0.38, 135, (0.0, -0.1, -0.07)))


def head_camera(arm, az, el, dist, lens, offset):
    pb = arm.pose.bones["Head"]
    rest = arm.data.bones["Head"].matrix_local
    to_world = arm.matrix_world @ pb.matrix @ rest.inverted()
    target = TARGET + Vector(offset)
    a, e = math.radians(az), math.radians(el)
    eye = target + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * dist
    data = bpy.data.cameras.new("HelmCam")
    data.lens = lens
    data.clip_start = 0.01
    cam = C.link(bpy.data.objects.new("HelmCam", data))
    fwd = (target - eye).normalized()
    right = fwd.cross(Vector((0, 0, 1))).normalized()
    up = right.cross(fwd)
    m = Matrix.Identity(4)
    m.col[0][:3], m.col[1][:3], m.col[2][:3], m.col[3][:3] = right, up, -fwd, eye
    cam.matrix_world = to_world @ m
    return cam


def args_extra():
    """--probe name,az,el,dist,lens,ox,oy,oz: one extra framing only (inspecting a spot)."""
    v = C.cli_args().get("probe")
    if not v:
        return None
    p = v.split(",")
    return (p[0], float(p[1]), float(p[2]), float(p[3]), float(p[4]), tuple(float(x) for x in p[5:8]))


def closeups(blend, out, prefix, size=900):
    import mercer_previews as MP
    import spencer_previews5 as SP5
    rig, acts, _ = SP5.open_scene(blend)
    MP.pose(rig, acts["Idle"], 10, "none")
    bpy.context.view_layer.update()
    paths = []
    shots = SHOTS
    if args_extra():
        shots = [args_extra()]
    for name, az, el, dist, lens, off in shots:
        cam = head_camera(rig.arm, az, el, dist, lens, off)
        paths.append(C.render(cam, os.path.join(out, f"{prefix}_{name}.png"), size, size))
        bpy.data.objects.remove(cam)
    if args_extra():
        return paths
    C.contact_sheet(paths[:8], os.path.join(out, f"{prefix}_sheet.png"), 4, 450)
    return paths


def game(blend, out, prefix):
    import mercer_previews as MP
    import spencer_previews as SP
    import spencer_previews5 as SP5
    rig, acts, lights = SP5.open_scene(blend)
    heroes = [(acts["Idle"], 10, (0.0, 0.0, 0), 0.3, "none"), (acts["Run"], 3, (-1.3, 0.7, 0), -1.2, "none"),
              (acts["Shoot"], 0, (1.0, 0.5, 0), -2.2, "pistol")]
    SP.game_shot(rig, heroes, [], os.path.join(out, f"{prefix}_game_zoom.png"), 3.2, lights, 1280, 720, (0.3, -0.25, 0.5))
    SP.game_shot(rig, heroes[:1], [], os.path.join(out, f"{prefix}_game_close.png"), 16.0, lights, 1280, 720, (0.0, 0.0, 1.5))
    MP.pose(rig, acts["Idle"], 10, "none")


def main():
    args = C.cli_args()
    out = args["out"]
    os.makedirs(out, exist_ok=True)
    closeups(args["blend"], out, args.get("prefix", "helmet2_after"))
    if not args.get("nogame"):
        game(args["blend"], out, args.get("prefix", "helmet2_after"))


if __name__ == "__main__":
    main()
