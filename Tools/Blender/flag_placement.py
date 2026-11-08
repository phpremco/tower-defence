"""Spencer pass 4 / flag fix (Remco 2026-11-08: "de vlag staat in een container"): the NTI flag on a clearly open
spot of the base deck. Measures the round-37 base export (a height map of the deck from above), checks the flag's
foot and its cloth sweep are free, and renders the proof shots: the game camera at play zoom and zoomed in, a close
3/4 and a top-down plan with the old and new spots marked.

The spot itself lives in build_flag.py (SPOT, SPOT_YAW), so the flag report carries it.

Run: blender --background --factory-startup --python Tools/Blender/flag_placement.py -- [--out <dir>]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import base_kit as bk  # noqa: E402
import build_base2 as B2  # noqa: E402
import build_flag as BF  # noqa: E402
import common as C  # noqa: E402

OLD_SPOT = (0.42, 2.16, 0.23)
PREVIEW = os.path.join(C.REPO, "ArtSource", "Previews", "Brand")


def base_tree(placed):
    dg = bpy.context.evaluated_depsgraph_get()
    verts, polys = [], []
    for o in placed:
        if o.type != "MESH" or "LOD1" in o.name or "Damage" in o.name or "Wreck" in o.name:
            continue
        me = o.evaluated_get(dg).to_mesh()
        k = len(verts)
        verts += [o.matrix_world @ v.co for v in me.vertices]
        polys += [tuple(k + i for i in p.vertices) for p in me.polygons]
        o.evaluated_get(dg).to_mesh_clear()
    return BVHTree.FromPolygons(verts, polys)


def clearance(tree, spot, yaw_deg):
    """Highest base surface within 0.22 m of the pole foot, and anything the cloth (its sweep box) would touch."""
    sx, sy, sz = spot
    foot = 0.0
    for r in np.linspace(0.0, 0.22, 5):
        for a in np.linspace(0, 2 * math.pi, 16, endpoint=False):
            hit = tree.ray_cast(Vector((sx + r * math.cos(a), sy + r * math.sin(a), 6)), Vector((0, 0, -1)), 10)
            if hit[0] is not None:
                foot = max(foot, hit[0].z - sz)
    d = Vector((math.cos(math.radians(yaw_deg)), math.sin(math.radians(yaw_deg)), 0))
    cloth = 0.0
    for t in np.linspace(0.0, BF.FLAG_W + 0.15, 12):
        for side in (-0.15, 0.0, 0.15):
            p = Vector((sx, sy, 6)) + d * t + Vector((-d.y, d.x, 0)) * side
            hit = tree.ray_cast(p, Vector((0, 0, -1)), 10)
            if hit[0] is not None:
                cloth = max(cloth, hit[0].z)
    return round(foot, 3), round(cloth, 3)


def marker(name, spot, colour):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.02, location=(spot[0], spot[1], 3.0))
    o = bpy.context.active_object
    o.name = name
    mat = C.principled(name + "_Mat", color=colour, emission=colour, strength=4.0)
    o.data.materials.append(mat)
    return o


def main():
    args = C.cli_args()
    out = args.get("out") or PREVIEW
    os.makedirs(out, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, BF.NAME + ".blend"))
    arm = bpy.data.objects[BF.NAME + "_Rig"]
    C.preview_world()
    base_dir = os.path.join(C.EXPORTS, "Base")
    placed = B2.import_prop(os.path.join(base_dir, "Base.fbx"), base_dir, "Base", (0, 0, 0), 0)
    for o in placed:
        if o.type == "MESH" and ("Damage" in o.name or "Wreck" in o.name):
            o.hide_render = True
    tree = base_tree(placed)
    C.log("old spot clearance (foot, cloth top)", clearance(tree, OLD_SPOT, BF.SPOT_YAW))
    C.log("new spot clearance (foot, cloth top)", clearance(tree, BF.SPOT, BF.SPOT_YAW), "cloth bottom",
          round(BF.SPOT[2] + BF.POLE_H - BF.FLAG_H, 3))
    BF.set_frame(arm, 18)
    arm.location = Vector(BF.SPOT)
    arm.rotation_euler.z = math.radians(BF.SPOT_YAW)
    bk.render_game(Vector((0.0, 0.4, 0.5)), os.path.join(out, "flag_fixed_game.png"), zoom=1.6, width=1280, height=720)
    bk.render_game(Vector((-0.3, 0.8, 0.8)), os.path.join(out, "flag_fixed_zoom.png"), zoom=4.0, width=1280, height=720)
    bk.render_close(Vector((-0.3, 0.8, 0.7)), os.path.join(out, "flag_fixed_close.png"), 6.0, azimuth=200, elevation=26, lens=50,
                    width=1280, height=800, light_scale=2.0)
    # Plan view from straight above: the old spot in red, the new one in green.
    marks = [marker("OldSpot", OLD_SPOT, (1.0, 0.1, 0.05)), marker("NewSpot", BF.SPOT, (0.1, 1.0, 0.2))]
    cam_data = bpy.data.cameras.new("Plan")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 6.4
    cam = C.link(bpy.data.objects.new("Plan", cam_data))
    cam.location = (0.0, 0.5, 12.0)
    lights = C.light_rig(scale=2.0)
    C.render(cam, os.path.join(out, "flag_fixed_plan.png"), 1100, 1000)
    bk.remove(marks + [cam] + lights)
    C.contact_sheet([os.path.join(out, f"flag_fixed_{n}.png") for n in ("game", "zoom", "close", "plan")],
                    os.path.join(out, "flag_fixed_sheet.png"), 2, 900)


if __name__ == "__main__":
    main()
