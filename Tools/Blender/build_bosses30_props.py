"""Round 30: Gravemaw's crack slabs (MODEL_TODO "Ronde 5 Unity" GW1).

`Gravemaw_CrackSlabs` (Exports/Gravemaw_CrackSlabs/): four tilted plates of heaved ground (`CrackSlab_A..D`, 150-300
tris each, one shared 256 material, Low 128) for the lips of Gravemaw's crack line. Same basalt as the
`BossFX_GroundCrack` strip: dark cracked basalt on top with a dusty ground film, raw dark rock on the broken edges and
underside, and molten light seeping out of the edge that faced the fissure (and its cracks). Each slab is modelled
already tilted (the lip heaved up 22-38 degrees) with its pivot on the hinge: the low edge on the ground at the
origin, the raised, glowing edge towards +X (Blender, i.e. towards the fissure). Unity yaws them to face the fissure,
sinks them a few cm and shows them as the crack opens (they are static; scale 0.8-1.3 for variety).

Run: blender --background --factory-startup --python Tools/Blender/build_bosses30_props.py -- [--preview <dir>]
     [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_bosses29_props as BP  # noqa: E402
import common as C  # noqa: E402
from env_kit import ss  # noqa: E402

ROUND = 30
SLABS = "Gravemaw_CrackSlabs"
TOP = np.array((0.12, 0.1, 0.085))
SIDE = np.array((0.07, 0.06, 0.052))
DUST = np.array((0.2, 0.17, 0.14))
SEAM = np.array((0.015, 0.01, 0.01))
HOT = np.array((1.0, 0.42, 0.1))
DEEP = np.array((0.4, 0.12, 0.03))
# (key, x offset in the source, half length, half width, half thickness, tilt in degrees, seed)
TABLE = [("A", -3.0, 0.38, 0.3, 0.075, 30, 3001), ("B", -1.0, 0.3, 0.25, 0.06, 22, 3011),
         ("C", 1.0, 0.46, 0.34, 0.09, 38, 3021), ("D", 3.0, 0.26, 0.21, 0.055, 26, 3031)]


def cells2d(u, v, size, seed):
    """F2 - F1 of a jittered-grid Voronoi in the plane (u, v): 0 on the seams between polygonal plates (the
    strip's cracked basalt)."""
    gu, gv = np.floor(u / size), np.floor(v / size)
    f1 = np.full(len(u), 9.0)
    f2 = np.full(len(u), 9.0)
    for du in (-1, 0, 1):
        for dv in (-1, 0, 1):
            cu, cv = gu + du, gv + dv
            h = np.sin(cu * 127.1 + cv * 311.7 + seed * 0.37) * 43758.5453
            j1 = h - np.floor(h)
            h2 = np.sin(cu * 269.5 + cv * 183.3 + seed * 0.71) * 43758.5453
            j2 = h2 - np.floor(h2)
            d = np.hypot(u - (cu + 0.15 + 0.7 * j1) * size, v - (cv + 0.15 + 0.7 * j2) * size)
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    return (f2 - f1) / size


def basalt(seed, tilt, half_len, x0):
    """Top: cracked basalt with a ground film and dark seams; broken faces: raw dark rock; the raised edge that
    faced the fissure (+X), its underside and the seams near it carry the molten glow."""
    t = math.radians(tilt)
    up_local = np.array((-math.sin(t), 0.0, math.cos(t)))      # the slab's own top normal after the tilt
    axis = np.array((math.cos(t), 0.0, math.sin(t)))           # hinge -> raised edge (also that edge's normal)

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        top = ss(0.55, 0.85, n @ up_local)
        edge = ss(0.3, 0.75, n @ axis)
        under = ss(0.3, 0.8, -(n @ up_local))
        along = ((p - np.array((x0, 0.0, 0.0))) @ axis) / (2 * half_len)   # 0 at the hinge, ~1 at the raised edge
        rel = p - np.array((x0, 0.0, 0.0))
        wob = 0.02 * C.value_noise(p, 9.0, seed + 4)
        gap = cells2d(rel @ axis + wob, rel[:, 1] - wob, 0.13, seed)
        seam = (1 - ss(0.04, 0.12, gap)) * (0.4 + 0.6 * top)
        grit = C.value_noise(p, 30.0, seed + 1)
        film = ss(-0.2, 0.4, C.value_noise(p, 3.0, seed + 2)) * top
        col = C.lerp3(SIDE, TOP, top) * (0.85 + 0.15 * grit)[:, None]
        col = C.lerp3(col, DUST, film * 0.45)
        col = C.lerp3(col, SEAM, seam * (0.5 + 0.5 * top))
        far = ss(0.5, 1.0, along)
        heat = np.clip(edge * 0.85 + under * far * 0.9 + seam * far * 0.7, 0, 1)
        col = C.lerp3(col, DEEP, heat * 0.7)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(HOT[None, :] * (heat * (0.55 + 0.45 * seam))[:, None], 0, 1)
        s.rough[idx] = 0.88 - 0.25 * heat
        s.disp[idx] = -0.006 * seam - 0.002 * ss(0.5, 0.7, grit)
        s.fam[idx] = (0.0, 1.0, 0.0, 0.0)
        s.flow[idx] = 0.0
    return style


def slab_parts():
    b = C.PartBuilder()
    for key, x0, hl, hw, ht, tilt, seed in TABLE:
        rot = Matrix.Rotation(-math.radians(tilt), 3, "Y")
        # The hinge edge sits on the ground at (x0, 0, 0); the slab rises towards +X.
        centre = Vector((x0, 0, 0)) + rot @ Vector((hl, 0, ht * 0.4))
        b.ellipsoid("Slab", centre, (hl, hw, ht), rot, bone="Root", style=basalt(seed, tilt, hl, x0), segments=(20, 10),
                    deform=C.chiselled(12, seed, 0.22, squash=(0.7, 1.0)))
    return b


def split_slabs(obj):
    """One object per slab: the faces from 0.4 behind its hinge to 1.6 ahead, moved so the hinge is the origin."""
    import bmesh
    out = []
    for key, x0, *_ in TABLE:
        me = obj.data.copy()
        bm = bmesh.new()
        bm.from_mesh(me)
        kill = [f for f in bm.faces if not (x0 - 0.4 < f.calc_center_median().x < x0 + 1.6)]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        for v in bm.verts:
            v.co.x -= x0
        bm.to_mesh(me)
        bm.free()
        me.name = f"CrackSlab_{key}"
        out.append(C.link(bpy.data.objects.new(me.name, me)))
    return out


def build_slabs():
    asset = C.build_mesh_asset(SLABS, slab_parts(), voxel=0.008, target_tris=900, tex_size=256, ao_distance=0.1,
                               extrusion=0.02, ray=0.05, emission_strength=3.0, smooth_iterations=1, symmetric=False,
                               sharp_angle=35, specular=0.2, mirror=False, k=1.0, families=("rock", "chitin"),
                               low_size=128)
    objs = split_slabs(asset.low)
    bpy.data.objects.remove(asset.low)
    M.export_static(os.path.join(C.EXPORTS, SLABS, SLABS + ".fbx"), objs)
    C.write_report(SLABS, {
        "asset": SLABS, "round": ROUND, "objects": {o.name: C.tri_count(o) for o in objs},
        "size_m": {o.name: [round(d, 3) for d in o.dimensions] for o in objs},
        "tilt_deg": {f"CrackSlab_{k}": tilt for k, _, _, _, _, tilt, _ in TABLE},
        "texture_size": 256, "low_texture_size": 128, "material": f"{SLABS}_Mat (shared by the four)",
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "detail": asset.details, "unity_scale": 1.0,
        "pivot": "the hinge: the low edge on the ground at the origin; the slab rises and glows towards Blender +X "
                 "(the fissure side)",
        "emission": "baked warm (1, 0.42, 0.1) on the fissure-facing edge and the seams near it: emission strength "
                    "~3 while the crack is hot, cool it with the strip's 10 s cooling (glow first)",
        "use": "GW1: 3-6 per opened crack along both lips (yaw so +X faces the fissure, random pick and scale 0.8-1.3, "
               "sink 0.03-0.08), shown as the crack opens next to the BossFX_GroundCrack strip; they replace the "
               "flat lips of the strip where they stand"})
    C.save_blend(SLABS)
    return objs


def ground_strip():
    """Preview only: the GroundCrack strip decal (8 x 2 m) on a quad, so the slabs are judged next to it."""
    folder = os.path.join(C.EXPORTS, "Boss_FX")
    me = bpy.data.meshes.new("CrackStrip")
    me.from_pydata([(-4, -1, 0.004), (4, -1, 0.004), (4, 1, 0.004), (-4, 1, 0.004)], [], [(0, 1, 2, 3)])
    me.uv_layers.new(name="UV")
    for i, uv in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))):
        me.uv_layers[0].data[i].uv = uv
    obj = C.link(bpy.data.objects.new("CrackStrip", me))
    mat = bpy.data.materials.new("CrackStripMat")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    alb = nt.nodes.new("ShaderNodeTexImage")
    alb.image = bpy.data.images.load(os.path.join(folder, "BossFX_GroundCrack_Albedo.png"))
    em = nt.nodes.new("ShaderNodeTexImage")
    em.image = bpy.data.images.load(os.path.join(folder, "BossFX_GroundCrack_Emission.png"))
    em.image.colorspace_settings.name = "Non-Color"
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(alb.outputs["Alpha"], bsdf.inputs["Alpha"])
    nt.links.new(em.outputs["Color"], sep.inputs[0])
    nt.links.new(sep.outputs[0], bsdf.inputs["Emission Strength"])
    bsdf.inputs["Emission Color"].default_value = (1.0, 0.42, 0.1, 1.0)
    for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
        try:
            setattr(mat, attr, val)
        except (AttributeError, TypeError):
            pass
    obj.data.materials.append(mat)
    return obj


def stage(objs):
    """Lays the slabs along both lips of the strip (the strip's fissure runs along X through y = 0)."""
    place = [("A", -2.6, -0.62, 90, 1.0), ("C", -0.6, 0.6, -90, 1.0), ("B", 1.2, -0.55, 80, 1.1), ("D", 2.8, 0.5, -100, 1.2),
             ("A", 0.4, -0.6, 100, 0.85), ("B", -1.8, 0.55, -80, 0.9)]
    out = []
    by = {o.name[-1]: o for o in objs}
    for key, x, y, yaw, sc in place:
        src = by[key]
        o = C.link(bpy.data.objects.new(f"Stage_{key}_{x}", src.data))
        o.location = (x, y, -0.03)
        o.rotation_euler = (0, 0, math.radians(yaw))
        o.scale = (sc, sc, sc)
        out.append(o)
    return out


def main():
    args = C.cli_args()
    out = C.preview_dir()
    C.reset_scene()
    objs = build_slabs()
    if args.get("norender"):
        return
    for i, o in enumerate(objs):
        o.location.x = (i - 1.5) * 1.4
    BP.preview_static(objs, out, SLABS, (0, 0, 0.3), 5.0, game_zoom=2.4)
    for o in objs:
        o.hide_render = True
    strip = ground_strip()
    staged = stage(objs)
    C.preview_world()
    C.ground(color=(0.05, 0.05, 0.07))
    lights = C.game_lights()
    paths = [BP.game_view(os.path.join(out, "_slabs_game_1.png"), (0, 0, 0.2), 900, 560, zoom=1.6),
             BP.game_view(os.path.join(out, "_slabs_game_2.png"), (0, 0, 0.2), 900, 560, zoom=3.0)]
    strip.hide_render = True
    for o in staged:
        o.hide_render = True
    C.contact_sheet(paths, os.path.join(out, f"{SLABS}_game.png"), 1, 900)
    for p in paths:
        os.remove(p)
    for o in lights:
        bpy.data.objects.remove(o)


if __name__ == "__main__":
    main()
