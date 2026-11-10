"""Deepwake logo, round 2 (2026-11-10): three title directions as real 3D lettering in a composited key-art scene.

The first round (logo_concepts.py) was rejected as plain: flat front-on text, one material, an orthographic camera and a
numpy glow. This round renders everything in Cycles with a perspective camera, depth of field, volumetric haze, embers,
layered materials (brushed metal face, polished chamfer, dark sides, raw metal in chips, emissive crystal light leaking
through fractures) and the Blender compositor (bloom, fog glow, lift/gamma/gain grade, slight chromatic dispersion).

Directions (all original):
- FAULTLINE: custom chamfered block letters (six glyphs drawn here, no font), sheared along one jagged crystal fault
  that runs through the whole word; the upper halves slip up and right, the gap glows with crystal light and veins
  spread from it. Emblem: a fractured steel hex core with a crystal cluster bursting out of the break.
- DEEP / WAKE: two words, two sides. "DEEP" small, wide-tracked white steel with a blue energy seam (the colony);
  "WAKE" huge dark obsidian-metal with molten crystal veins rising from below (the planet waking). Emblem: an eye-like
  rift opening in rock with a slit pupil, inside a colony targeting reticle.
- OVERGROWTH: the colony's lettering as painted armour plate (panel seams, chipped paint, a hazard band along the
  foot, set in the game's UI face Chakra Petch) with alien crystal spears growing up through the letters.
  Emblem: the colony's landing chevrons split by a crystal spear.

No tagline (Remco rejected it). The series lockup puts the series line "FAR HARVEST" above the best direction.

Out (ArtSource/Previews/Logo/v2/): <Dir>_header_920x430.png, <Dir>_capsule_616x353.png, <Dir>_library_logo.png
(1280x720, alpha), <Dir>_emblem_512.png, <Dir>_emblem_256.png, Faultline_series_header_920x430.png and _library_logo.png (series line FAR HARVEST),
Deepwake_v2_contact_sheet.png.

Run (one direction per Blender process, then the sheet; Cycles renders on the HIP GPU, --cpu forces the CPU). When
Unity agents share the machine, cap Blender with --threads 4 and start it at below-normal priority:
  blender -b --factory-startup --threads 4 --python Tools/Blender/logo_v2.py -- --dir faultline|deepwake|overgrowth
          [--only header,capsule,library,emblem] [--samples 96] [--series]
  blender -b --factory-startup --threads 4 --python Tools/Blender/logo_v2.py -- --sheet
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Euler, Matrix, Vector, noise as mnoise  # noqa: E402

import common as C  # noqa: E402

OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Logo", "v2")
FONTS = os.path.join(OUT, "_fonts")
SERIES_LINE = "FAR HARVEST"

HEADER = (920, 430)
CAPSULE = (616, 353)
LIBRARY = (1280, 720)
SUPER = 2          # every still renders at 2x and is box-filtered down
EMBLEM_RENDER = 1024

COOL = (0.22, 0.55, 1.0)
COOL_HOT = (0.55, 0.82, 1.0)
CYAN = (0.15, 0.85, 1.0)
WARM = (1.0, 0.30, 0.05)
WARM_HOT = (1.0, 0.62, 0.20)
WARM_DEEP = (0.85, 0.06, 0.03)


# =============================================================================================================
# Node helpers
# =============================================================================================================

def new_mat(name):
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except AttributeError:
        pass
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    return mat, nt, out


def sock(sockets, name):
    for s in sockets:
        if s.name == name and s.enabled:
            return s
    raise KeyError(name)


def setv(nt, node, name, value):
    target = sock(node.inputs, name)
    if isinstance(value, bpy.types.NodeSocket):
        nt.links.new(value, target)
    else:
        if isinstance(value, tuple) and len(value) == 3 and target.type == "RGBA":
            value = (*value, 1.0)
        target.default_value = value


def N(nt, kind, inputs=None, **props):
    n = nt.nodes.new(kind)
    for k, v in props.items():
        setattr(n, k, v)
    for k, v in (inputs or {}).items():
        setv(nt, n, k, v)
    return n


def o(node, name=None):
    if name is None:
        return next(s for s in node.outputs if s.enabled)
    return sock(node.outputs, name)


def M(nt, op, a, b=None, clamp=False):
    n = N(nt, "ShaderNodeMath", operation=op, use_clamp=clamp)
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, bpy.types.NodeSocket):
            nt.links.new(v, n.inputs[i])
        else:
            n.inputs[i].default_value = v
    return n.outputs[0]


def MR(nt, value, a, b, c=0.0, d=1.0, smooth=True):
    return o(N(nt, "ShaderNodeMapRange", {"Value": value, "From Min": a, "From Max": b, "To Min": c, "To Max": d},
               interpolation_type="SMOOTHSTEP" if smooth else "LINEAR"), "Result")


def MIXC(nt, fac, a, b):
    n = N(nt, "ShaderNodeMix", data_type="RGBA")
    setv(nt, n, "Factor", fac)
    setv(nt, n, "A", a)
    setv(nt, n, "B", b)
    return o(n, "Result")


def SEP(nt, vec):
    n = N(nt, "ShaderNodeSeparateXYZ")
    nt.links.new(vec, n.inputs[0])
    return n.outputs[0], n.outputs[1], n.outputs[2]


def objco(nt):
    return o(N(nt, "ShaderNodeTexCoord"), "Object")


def noise(nt, vec, scale, detail=4.0, rough=0.55, stretch=None, dist=0.0):
    if stretch:
        vec = o(N(nt, "ShaderNodeMapping", {"Vector": vec, "Scale": stretch}))
    return N(nt, "ShaderNodeTexNoise", {"Vector": vec, "Scale": scale, "Detail": detail, "Roughness": rough,
                                         "Distortion": dist})


def crack_lines(nt, vec, scale, width, warp=0.25, seed_offset=(0, 0, 0)):
    """Thin network lines (voronoi cell edges, warped by noise so they read as cracks, not cells)."""
    vm = N(nt, "ShaderNodeVectorMath", operation="ADD")
    off = vm.outputs[0]
    nt.links.new(vec, vm.inputs[0])
    vm.inputs[1].default_value = seed_offset
    wn = noise(nt, off, scale * 0.8, 3.0, 0.5)
    warped = N(nt, "ShaderNodeMix", data_type="VECTOR")
    warped.inputs[0].default_value = warp
    nt.links.new(off, sock(warped.inputs, "A"))
    nt.links.new(o(wn, "Color"), sock(warped.inputs, "B"))
    vor = N(nt, "ShaderNodeTexVoronoi", {"Vector": o(warped, "Result"), "Scale": scale}, feature="DISTANCE_TO_EDGE")
    return MR(nt, o(vor, "Distance"), 0.0, width, 1.0, 0.0)


def principled(nt, out, **inputs):
    bsdf = N(nt, "ShaderNodeBsdfPrincipled")
    names = {"base": "Base Color", "metal": "Metallic", "rough": "Roughness", "aniso": "Anisotropic",
             "normal": "Normal", "emit": "Emission Color", "emit_s": "Emission Strength", "coat": "Coat Weight",
             "coat_rough": "Coat Roughness", "spec": "Specular IOR Level", "trans": "Transmission Weight",
             "ior": "IOR", "sss": "Subsurface Weight"}
    for k, v in inputs.items():
        setv(nt, bsdf, names[k], v)
    nt.links.new(o(bsdf, "BSDF"), out.inputs["Surface"])
    return bsdf


def bump(nt, height, strength, distance=0.01, normal=None):
    b = N(nt, "ShaderNodeBump", {"Height": height, "Strength": strength, "Distance": distance})
    if normal is not None:
        nt.links.new(normal, sock(b.inputs, "Normal"))
    return o(b, "Normal")


def bevel_normal(nt, radius):
    return o(N(nt, "ShaderNodeBevel", {"Radius": radius}, samples=8), "Normal")


# =============================================================================================================
# Materials
# =============================================================================================================

def vein_mask(nt, kind, args):
    """Where the crystal light is allowed to leak through a surface (object space of the lettering)."""
    x, y, z = SEP(nt, objco(nt))
    if kind == "fault":
        y0, slope, reach = args
        line = M(nt, "ADD", M(nt, "MULTIPLY", x, slope), y0)
        d = M(nt, "ABSOLUTE", M(nt, "SUBTRACT", y, line))
        return MR(nt, d, 0.0, reach, 1.0, 0.0)
    if kind == "bottom":
        top, bottom = args
        return MR(nt, y, top, bottom, 0.0, 1.0)
    if kind == "radial":
        cx, cy, r0, r1 = args
        d = M(nt, "SQRT", M(nt, "ADD", M(nt, "POWER", M(nt, "SUBTRACT", x, cx), 2.0),
                            M(nt, "POWER", M(nt, "SUBTRACT", y, cy), 2.0)))
        return MR(nt, d, r0, r1, 1.0, 0.0)
    return 1.0


def mat_metal_face(name, base, rough=0.3, brushed=True, grime=0.55, veins=None, bevel_r=0.012, coat=0.0,
                   gradient=None):
    """Brushed metal face; optional crystal veins: dict(color, strength, scale, width, mask=(kind, args), density)."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    streak = noise(nt, co, 3.0, 6.0, 0.6, stretch=(2.0, 90.0, 2.0) if brushed else (8, 8, 8))
    rough_s = MR(nt, o(streak, "Factor"), 0.3, 0.7, rough - 0.07, rough + 0.09, smooth=False)
    g = noise(nt, co, 2.2, 8.0, 0.62)
    gmask = MR(nt, o(g, "Factor"), 0.45, 0.72, 0.0, grime)
    col = MIXC(nt, gmask, base, tuple(c * 0.35 for c in base))
    if gradient:
        # Vertical value gradient over the cap height (y0, y1, bottom multiplier, top multiplier).
        gy0, gy1, gb, gt = gradient
        _, gy, _ = SEP(nt, co)
        mul = MR(nt, gy, gy0, gy1, gb, gt)
        mixn = N(nt, "ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
        setv(nt, mixn, "Factor", 1.0)
        setv(nt, mixn, "A", col)
        cm = N(nt, "ShaderNodeCombineXYZ")
        for i in range(3):
            nt.links.new(mul, cm.inputs[i])
        nt.links.new(o(cm), sock(mixn.inputs, "B"))
        col = o(mixn, "Result")
    height = M(nt, "MULTIPLY", o(streak, "Factor"), 0.25)
    nrm = bevel_normal(nt, bevel_r)
    emit_s = 0.0
    emit_c = (0, 0, 0)
    if veins:
        mask = vein_mask(nt, *veins["mask"])
        lines = crack_lines(nt, co, veins["scale"], veins["width"], seed_offset=veins.get("seed", (3.1, 7.7, 1.3)))
        breakup = MR(nt, o(noise(nt, co, veins.get("breakup", 1.6), 3.0), "Factor"), 0.42, 0.58, 0.0, 1.0)
        dens = M(nt, "MULTIPLY", M(nt, "MULTIPLY", lines, mask), breakup)
        # Crack interior is black-hot: the face darkens and dips where the light leaks out.
        col = MIXC(nt, M(nt, "MINIMUM", M(nt, "MULTIPLY", dens, 1.6), 1.0), col, (0.01, 0.006, 0.004))
        glow_mask = M(nt, "MULTIPLY", M(nt, "ADD", dens, M(nt, "MULTIPLY", M(nt, "MULTIPLY", mask, breakup),
                                                                 veins.get("halo", 0.04))), veins["strength"])
        emit_s = glow_mask
        emit_c = veins["color"]
        height = M(nt, "SUBTRACT", height, M(nt, "MULTIPLY", dens, 1.0))
    nrm = bump(nt, height, 0.12, 0.01, nrm)
    principled(nt, out, base=col, metal=1.0, rough=rough_s, aniso=0.35 if brushed else 0.0, normal=nrm,
               emit=emit_c, emit_s=emit_s, coat=coat)
    return mat


def mat_polished(name, base, rough=0.14, wear=0.4):
    """The chamfer: bright, nearly polished, with a little wear so it doesn't read as a plastic outline."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    w = MR(nt, o(noise(nt, co, 9.0, 6.0, 0.6), "Factor"), 0.5, 0.72, 0.0, wear)
    col = MIXC(nt, w, base, tuple(c * 0.55 for c in base))
    r = M(nt, "ADD", M(nt, "MULTIPLY", w, 0.35), rough)
    principled(nt, out, base=col, metal=1.0, rough=r, normal=bevel_normal(nt, 0.008))
    return mat


def mat_side(name, base=(0.05, 0.055, 0.065), rough=0.42, seam=None):
    """Dark sides; optional emissive seam band around the letter's depth: seam=(color, strength, z_centre, half)."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    nz = noise(nt, co, 14.0, 4.0, 0.6, stretch=(1, 1, 30))
    r = MR(nt, o(nz, "Factor"), 0.3, 0.7, rough - 0.08, rough + 0.12, smooth=False)
    emit_s = 0.0
    emit_c = (0, 0, 0)
    if seam:
        colr, strength, zc, half = seam
        _, _, z = SEP(nt, co)
        d = M(nt, "ABSOLUTE", M(nt, "SUBTRACT", z, zc))
        band = MR(nt, d, half * 0.6, half, 1.0, 0.0)
        emit_s = M(nt, "MULTIPLY", band, strength)
        emit_c = colr
    principled(nt, out, base=base, metal=0.9, rough=r, normal=bevel_normal(nt, 0.008), emit=emit_c, emit_s=emit_s)
    return mat


def mat_fracture(name, glow=WARM, strength=1.5):
    """Torn metal inside a fault or chip: rough, granular, lit warm from the crystal behind it."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    gr = noise(nt, co, 60.0, 6.0, 0.8)
    col = MIXC(nt, o(gr, "Factor"), (0.12, 0.11, 0.1), (0.42, 0.40, 0.38))
    nrm = bump(nt, o(gr, "Factor"), 0.5, 0.01)
    e = M(nt, "MULTIPLY", MR(nt, o(noise(nt, co, 5.0, 3.0), "Factor"), 0.4, 0.65, 0.2, 1.0), strength)
    principled(nt, out, base=col, metal=0.85, rough=0.55, normal=nrm, emit=glow, emit_s=e)
    return mat


def mat_chip(name):
    mat, nt, out = new_mat(name)
    co = objco(nt)
    gr = noise(nt, co, 80.0, 5.0, 0.8)
    nrm = bump(nt, o(gr, "Factor"), 0.6, 0.006)
    principled(nt, out, base=(0.62, 0.62, 0.64), metal=1.0, rough=0.38, normal=nrm)
    return mat


def mat_emit(name, colour, strength, core=None, fresnel_power=0.25):
    """Glowing crystal: hot core where it faces the camera, saturated rim (layer weight)."""
    mat, nt, out = new_mat(name)
    lw = N(nt, "ShaderNodeLayerWeight", {"Blend": fresnel_power})
    col = MIXC(nt, o(lw, "Facing"), core or colour, colour)
    em = N(nt, "ShaderNodeEmission", {"Color": col, "Strength": strength})
    gl = N(nt, "ShaderNodeBsdfGlossy", {"Color": (1, 1, 1), "Roughness": 0.05})
    mix = N(nt, "ShaderNodeAddShader")
    nt.links.new(o(em), mix.inputs[0])
    nt.links.new(o(gl), mix.inputs[1])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    return mat


def mat_crystal(name, colour, core, strength, coat=1.0, spec=0.5):
    """Crystal lit from inside: hot at the root where it grows out of the host, cooling to dark glass at the tip,
    with brighter facet edges and a per-facet variation (attributes "t" and "facet" written by shard())."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    lw = N(nt, "ShaderNodeLayerWeight", {"Blend": 0.45})
    facing = o(lw, "Facing")
    t = o(N(nt, "ShaderNodeAttribute", attribute_name="t", attribute_type="GEOMETRY"), "Factor")
    f = o(N(nt, "ShaderNodeAttribute", attribute_name="facet", attribute_type="GEOMETRY"), "Factor")
    f = M(nt, "ADD", f, M(nt, "LESS_THAN", f, 0.01))      # meshes without the attribute glow evenly
    inc = MR(nt, o(noise(nt, co, 1.8, 4.0, 0.5, dist=0.4), "Factor"), 0.35, 0.65, 0.55, 1.0)
    along = MR(nt, t, 0.0, 1.0, 1.0, 0.12)
    col = MIXC(nt, MR(nt, t, 0.0, 0.8), core, colour)
    rim = M(nt, "ADD", M(nt, "POWER", facing, 1.8), 0.3)
    s = M(nt, "MULTIPLY", M(nt, "MULTIPLY", M(nt, "MULTIPLY", inc, rim), M(nt, "MULTIPLY", along, f)), strength)
    principled(nt, out, base=tuple(c * 0.04 for c in colour), metal=0.0, rough=0.06, emit=col, emit_s=s,
               coat=coat, coat_rough=0.02, spec=spec)
    return mat


def mat_rock(name, base=(0.022, 0.022, 0.026), glow=WARM, glow_strength=6.0, crack_scale=1.4, tint2=None):
    """Basalt-like ground with glowing fissure rims and a vein network fading out from the rift (vertex attribute
    "Glow" written by the terrain builder)."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    n1 = noise(nt, co, 1.2, 10.0, 0.62)
    n2 = noise(nt, co, 9.0, 8.0, 0.6)
    col = MIXC(nt, o(n1, "Factor"), tuple(c * 0.5 for c in base), tint2 or tuple(c * 1.8 for c in base))
    h = M(nt, "ADD", o(n1, "Factor"), M(nt, "MULTIPLY", o(n2, "Factor"), 0.35))
    attr = o(N(nt, "ShaderNodeAttribute", attribute_name="Glow", attribute_type="GEOMETRY"), "Factor")
    lines = crack_lines(nt, co, crack_scale, 0.035, warp=0.35)
    veins = M(nt, "MULTIPLY", lines, MR(nt, attr, 0.05, 0.6, 0.0, 1.0))
    rim = M(nt, "POWER", attr, 3.0)
    e = M(nt, "MULTIPLY", M(nt, "ADD", veins, M(nt, "MULTIPLY", rim, 0.5)), glow_strength)
    col = MIXC(nt, M(nt, "MINIMUM", veins, 1.0), col, (0.0, 0.0, 0.0))
    nrm = bump(nt, M(nt, "SUBTRACT", h, M(nt, "MULTIPLY", veins, 0.4)), 0.7, 0.2)
    principled(nt, out, base=col, metal=0.0, rough=0.82, normal=nrm, emit=glow, emit_s=e, spec=0.4)
    return mat


def mat_haze(name, colour, density, low_z, high_z):
    mat, nt, out = new_mat(name)
    tc = o(N(nt, "ShaderNodeTexCoord"), "Object")
    _, _, z = SEP(nt, tc)
    patch = MR(nt, o(noise(nt, tc, 0.08, 3.0, 0.5), "Factor"), 0.35, 0.65, 0.35, 1.4)
    height = MR(nt, z, low_z, high_z, 1.0, 0.18)
    dens = M(nt, "MULTIPLY", M(nt, "MULTIPLY", patch, height), density)
    vol = N(nt, "ShaderNodeVolumePrincipled", {"Color": colour, "Density": dens, "Anisotropy": 0.15})
    nt.links.new(o(vol), out.inputs["Volume"])
    return mat


def mat_reflector(name, strength=3.0, band=0.62):
    """Large studio card behind the camera: what the metal faces reflect (a soft horizontal band)."""
    mat, nt, out = new_mat(name)
    tc = o(N(nt, "ShaderNodeTexCoord"), "Generated")
    _, y, _ = SEP(nt, tc)
    b = MR(nt, M(nt, "ABSOLUTE", M(nt, "SUBTRACT", y, band)), 0.0, 0.3, 1.0, 0.04)
    em = N(nt, "ShaderNodeEmission", {"Color": (0.78, 0.86, 1.0), "Strength": M(nt, "MULTIPLY", b, strength)})
    nt.links.new(o(em), out.inputs["Surface"])
    return mat


# =============================================================================================================
# Geometry
# =============================================================================================================

def coll(name):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(c)
    return c


def put(ob, collection):
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    collection.objects.link(ob)
    return ob


def weld(me, dist=1e-5):
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=dist)
    bm.to_mesh(me)
    bm.free()


def to_mesh(curve_ob):
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(curve_ob.evaluated_get(dg))
    weld(me)
    ob = bpy.data.objects.new(curve_ob.name, me)
    for c in curve_ob.users_collection:
        c.objects.link(ob)
    data = curve_ob.data
    bpy.data.objects.remove(curve_ob)
    if data.users == 0:
        bpy.data.curves.remove(data)
    return ob


def poly_curve(name, polys, extrude, bevel, res=0, z0=0.0):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    for poly in polys:
        sp = cu.splines.new("POLY")
        sp.points.add(len(poly) - 1)
        for i, (x, y) in enumerate(poly):
            sp.points[i].co = (x, y, 0.0, 1.0)
        sp.use_cyclic_u = True
    cu.extrude = extrude
    cu.bevel_depth = bevel
    cu.bevel_resolution = res
    ob = to_mesh(C.link(bpy.data.objects.new(name, cu)))
    if z0:
        ob.data.transform(Matrix.Translation((0, 0, z0)))
        ob.location.z = 0
    return ob


def prism(name, poly, z0, z1, mat_index=0):
    bm = bmesh.new()
    lo = [bm.verts.new((x, y, z0)) for x, y in poly]
    hi = [bm.verts.new((x, y, z1)) for x, y in poly]
    bm.faces.new(list(reversed(lo)))
    bm.faces.new(hi)
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((lo[i], lo[j], hi[j], hi[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        f.material_index = mat_index
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return C.link(bpy.data.objects.new(name, me))


def text_mesh(name, body, font_file, size, extrude, bevel, spacing=1.0, res=0, offset=0.0):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = body
    cu.font = bpy.data.fonts.load(font_file, check_existing=True)
    cu.size = size
    cu.space_character = spacing
    cu.extrude = extrude
    cu.bevel_depth = bevel
    cu.bevel_resolution = res
    cu.offset = offset
    cu.resolution_u = 6
    ob = C.link(bpy.data.objects.new(name, cu))
    return to_mesh(ob)


def assign_by_normal(ob, mats, slots=None):
    """Slot 0 front face, 1 chamfer, 2 side and back. Extra slots (fracture, chip) are filled by booleans."""
    me = ob.data
    me.materials.clear()
    for m in mats:
        me.materials.append(m)
    for p in me.polygons:
        nz = p.normal.z
        p.material_index = 0 if nz > 0.97 else (1 if nz > 0.12 else 2)


def boolean(ob, cutter, op, solver="MANIFOLD"):
    m = ob.modifiers.new("b", "BOOLEAN")
    m.operation = op
    m.object = cutter
    m.solver = solver
    try:
        m.material_mode = "INDEX"
    except AttributeError:
        pass
    C.activate(ob)
    bpy.ops.object.modifier_apply(modifier=m.name)


def duplicate(ob, name):
    cp = ob.copy()
    cp.data = ob.data.copy()
    cp.name = name
    for c in ob.users_collection:
        c.objects.link(cp)
    return cp


def remove(ob):
    me = ob.data
    bpy.data.objects.remove(ob)
    if me and me.users == 0:
        bpy.data.meshes.remove(me)


def join(obs, name):
    C.activate(obs[0])
    for x in obs[1:]:
        x.select_set(True)
    bpy.ops.object.join()
    obs[0].name = name
    return obs[0]


def chip_cutter(points, rng, size=(0.03, 0.07), mat_index=4, name="Chips"):
    """Irregular rock-like nuggets at the given 3D points, joined into one cutter mesh."""
    bm = bmesh.new()
    for p in points:
        s = rng.uniform(*size)
        res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
        vs = res["verts"]
        sx, sy, sz = rng.uniform(0.6, 1.4), rng.uniform(0.6, 1.4), rng.uniform(0.6, 1.3)
        rot = Vector((rng.uniform(0, 6.28), rng.uniform(0, 6.28), rng.uniform(0, 6.28)))
        R = Euler(rot).to_matrix()
        for v in vs:
            j = 1.0 + rng.uniform(-0.25, 0.25)
            v.co = R @ Vector((v.co.x * sx * j, v.co.y * sy * j, v.co.z * sz * j)) * s + Vector(p)
    for f in bm.faces:
        f.material_index = mat_index
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return C.link(bpy.data.objects.new(name, me))


def shard(bm, base, direction, length, radius, sides=6, tip=0.38, rng=None, flat=1.0, mat_index=0):
    """One crystal: an irregular prism with a faceted point."""
    # Layers first: adding a layer later invalidates the vertices already made.
    lt = bm.verts.layers.float.get("t") or bm.verts.layers.float.new("t")
    lf = bm.faces.layers.float.get("facet") or bm.faces.layers.float.new("facet")
    d = Vector(direction).normalized()
    up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    a = d.cross(up).normalized()
    b = d.cross(a).normalized()
    rng = rng or random.Random(1)
    ring0, ring1 = [], []
    for i in range(sides):
        t = i / sides * math.tau + rng.uniform(-0.15, 0.15)
        r = radius * rng.uniform(0.8, 1.15)
        off = a * math.cos(t) * r + b * math.sin(t) * r * flat
        ring0.append(bm.verts.new(Vector(base) + off * 0.8))
        ring1.append(bm.verts.new(Vector(base) + d * length * (1 - tip) + off))
    apex = bm.verts.new(Vector(base) + d * length + a * radius * rng.uniform(-0.2, 0.2))
    faces = [bm.faces.new(list(reversed(ring0)))]
    for i in range(sides):
        j = (i + 1) % sides
        faces.append(bm.faces.new((ring0[i], ring0[j], ring1[j], ring1[i])))
        faces.append(bm.faces.new((ring1[i], ring1[j], apex)))
    for v in ring0:
        v[lt] = 0.0
    for v in ring1:
        v[lt] = 1.0 - tip
    apex[lt] = 1.0
    for f in faces:
        f.material_index = mat_index
        f[lf] = rng.uniform(0.45, 1.25)
    return faces


def bm_object(name, bm, mats):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    return C.link(bpy.data.objects.new(name, me))


def chamfer_poly(pts, c, min_turn=0.35):
    """Cut every corner sharper than min_turn radians with a 45-ish chamfer of size c (keeps short edges)."""
    out = []
    n = len(pts)
    for i in range(n):
        p = Vector(pts[i])
        a = Vector(pts[i - 1])
        b = Vector(pts[(i + 1) % n])
        da, db = (a - p), (b - p)
        la, lb = da.length, db.length
        turn = math.pi - da.angle(db)
        cc = min(c, la * 0.3, lb * 0.3)
        if turn < min_turn or cc < 1e-4:
            out.append((p.x, p.y))
            continue
        q1 = p + da.normalized() * cc
        q2 = p + db.normalized() * cc
        out += [(q1.x, q1.y), (q2.x, q2.y)]
    return out


# =============================================================================================================
# The FAULTLINE glyphs (cap height 1). Outer outline first, holes after.
# =============================================================================================================

def _mirror(pts, axis):
    return [(2 * axis - x, y) for x, y in pts]


GLYPHS = {
    "D": (0.88, [[(0, 0), (0.62, 0), (0.88, 0.26), (0.88, 0.74), (0.62, 1), (0, 1)],
                 [(0.27, 0.21), (0.53, 0.21), (0.61, 0.31), (0.61, 0.69), (0.53, 0.79), (0.27, 0.79)]]),
    "E": (0.72, [[(0, 0), (0.72, 0), (0.67, 0.21), (0.27, 0.21), (0.27, 0.40), (0.62, 0.40), (0.58, 0.60),
                  (0.27, 0.60), (0.27, 0.79), (0.72, 0.79), (0.67, 1), (0, 1)]]),
    "P": (0.84, [[(0, 0), (0.27, 0), (0.27, 0.36), (0.62, 0.36), (0.84, 0.56), (0.84, 0.80), (0.64, 1), (0, 1)],
                 [(0.27, 0.55), (0.55, 0.55), (0.61, 0.61), (0.61, 0.75), (0.55, 0.81), (0.27, 0.81)]]),
    "W": (1.24, [[(0, 1), (0.20, 0), (0.44, 0), (0.62, 0.48), (0.80, 0), (1.04, 0), (1.24, 1), (0.99, 1),
                  (0.862, 0.36), (0.72, 0.78), (0.52, 0.78), (0.378, 0.36), (0.25, 1)]]),
    "A": (0.96, [[(0, 0), (0.25, 0), (0.325, 0.22), (0.635, 0.22), (0.71, 0), (0.96, 0), (0.62, 1), (0.34, 1)],
                 [(0.386, 0.40), (0.574, 0.40), (0.48, 0.676)]]),
    "K": (0.88, [[(0, 0), (0.27, 0), (0.27, 0.30), (0.58, 0), (0.88, 0), (0.50, 0.50), (0.88, 1), (0.58, 1),
                  (0.27, 0.68), (0.27, 1), (0, 1)]]),
}


def custom_word(word, gap=0.10, chamfer=0.045):
    """Polygons of the whole word, left aligned at x=0, plus the outer outline of each glyph (for chips)."""
    polys, outlines, x = [], [], 0.0
    for ch in word:
        w, gl = GLYPHS[ch]
        for k, poly in enumerate(gl):
            cp = chamfer_poly(poly, chamfer if k == 0 else chamfer * 0.7)
            moved = [(px + x, py) for px, py in cp]
            polys.append(moved)
            outlines.append(moved)
        x += w + gap
    return polys, outlines, x - gap


def jagged_line(x0, x1, y0, slope, step, amp, rng):
    pts = []
    x = x0
    while x < x1:
        pts.append((x, y0 + slope * x + rng.uniform(-amp, amp)))
        x += step * rng.uniform(0.6, 1.4)
    pts.append((x1, y0 + slope * x1))
    return pts


def split_along(word, line, gap, z_span=(-1.0, 2.0), name="W"):
    """Cut a word mesh along a polyline: (upper, lower, band) where band is the word inside the gap."""
    top = 9.0
    bot = -9.0
    up_poly = [(x, y + gap * 0.5) for x, y in line] + [(line[-1][0], top), (line[0][0], top)]
    lo_poly = [(line[0][0], bot), (line[-1][0], bot)] + [(x, y - gap * 0.5) for x, y in reversed(line)]
    band_poly = [(x, y + gap * 0.5) for x, y in line] + [(x, y - gap * 0.5) for x, y in reversed(line)]
    up_c = prism(name + "_upc", up_poly, *z_span, mat_index=3)
    lo_c = prism(name + "_loc", lo_poly, *z_span, mat_index=3)
    band_c = prism(name + "_bandc", band_poly, *z_span, mat_index=3)
    upper = duplicate(word, name + "_upper")
    lower = duplicate(word, name + "_lower")
    band = duplicate(word, name + "_band")
    boolean(upper, up_c, "INTERSECT")
    boolean(lower, lo_c, "INTERSECT")
    boolean(band, band_c, "INTERSECT")
    for c in (up_c, lo_c, band_c):
        remove(c)
    remove(word)
    return upper, lower, band


def outline_chip_points(outlines, z, rng, prob, count_edges=1):
    pts = []
    for poly in outlines:
        n = len(poly)
        for i in range(n):
            if rng.random() < prob:
                pts.append((poly[i][0], poly[i][1], z))
            for _ in range(count_edges):
                if rng.random() < prob * 0.6:
                    a, b = Vector(poly[i]), Vector(poly[(i + 1) % n])
                    q = a.lerp(b, rng.uniform(0.2, 0.8))
                    pts.append((q.x, q.y, z))
    return pts


# =============================================================================================================
# Scene: environment, lights, haze, embers
# =============================================================================================================

def world(colour, strength=1.0):
    w = bpy.data.worlds.new("World")
    bpy.context.scene.world = w
    try:
        w.use_nodes = True
    except AttributeError:
        pass
    nt = w.node_tree
    bg = nt.nodes.get("Background") or nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (*colour, 1)
    bg.inputs["Strength"].default_value = strength


def area(name, loc, target, colour, energy, size=(2, 2), collection=None, spread=180.0):
    ld = bpy.data.lights.new(name, "AREA")
    ld.spread = math.radians(spread)
    ld.shape = "RECTANGLE"
    ld.size, ld.size_y = size
    ld.color = colour
    ld.energy = energy
    ob = bpy.data.objects.new(name, ld)
    (collection or bpy.context.scene.collection).objects.link(ob)
    ob.location = loc
    C.look_at(ob, Vector(target))
    return ob


def point(name, loc, colour, energy, radius=0.3, collection=None):
    ld = bpy.data.lights.new(name, "POINT")
    ld.color = colour
    ld.energy = energy
    ld.shadow_soft_size = radius
    ob = bpy.data.objects.new(name, ld)
    (collection or bpy.context.scene.collection).objects.link(ob)
    ob.location = loc
    return ob


def seg_dist(px, py, path):
    """Distance of points to a polyline, plus the polyline parameter (y of the closest point)."""
    best = np.full(px.shape, 1e9, np.float32)
    for (ax, ay), (bx, by) in zip(path[:-1], path[1:]):
        dx, dy = bx - ax, by - ay
        L2 = dx * dx + dy * dy
        t = np.clip(((px - ax) * dx + (py - ay) * dy) / L2, 0, 1)
        d = np.hypot(px - (ax + t * dx), py - (ay + t * dy))
        best = np.minimum(best, d)
    return best


def terrain(cfg, collection):
    """Canyon floor with a fissure carved along cfg['rift'] and cliffs rising at the sides."""
    nx, ny = 260, 300
    x0, x1, y0, y1 = -34.0, 34.0, -4.0, 80.0
    xs = np.linspace(x0, x1, nx, dtype=np.float32)
    ys = np.linspace(y0, y1, ny, dtype=np.float32)
    X, Y = np.meshgrid(xs, ys)
    base = cfg.get("floor", -1.7)
    Z = np.full(X.shape, base, np.float32)
    seed = cfg.get("seed", 3)
    fb = np.vectorize(lambda x, y: mnoise.fractal(Vector((x * 0.18 + seed, y * 0.18, 0.0)), 0.55, 2.1, 6))
    rd = np.vectorize(lambda x, y: mnoise.ridged_multi_fractal(Vector((x * 0.09, y * 0.09 + seed, 1.3)),
                                                               0.9, 2.2, 6, 1.0, 2.0))
    Z += fb(X, Y).astype(np.float32) * 0.35
    half = cfg.get("canyon", 6.0) + cfg.get("canyon_flare", 0.28) * np.maximum(Y, 0)
    wall = np.maximum(np.abs(X) - half, 0)
    Z += (wall ** 1.25) * cfg.get("wall", 1.1) * (0.6 + 0.6 * rd(X, Y).astype(np.float32) / 2.0)
    glow = np.zeros_like(Z)
    if cfg.get("rift"):
        path = cfg["rift"]
        d = seg_dist(X, Y, path)
        w = cfg.get("rift_w", 0.45) + cfg.get("rift_grow", 0.025) * np.maximum(Y, 0)
        Z -= cfg.get("rift_depth", 1.1) * np.exp(-(d / w) ** 2)
        Z += 0.18 * np.exp(-((d - w * 1.3) / (w * 0.6)) ** 2)   # raised lips
        glow = np.exp(-(d / (w * cfg.get("glow_reach", 3.0))) ** 2).astype(np.float32)
    bm = bmesh.new()
    verts = [bm.verts.new((float(X[j, i]), float(Y[j, i]), float(Z[j, i]))) for j in range(ny) for i in range(nx)]
    for j in range(ny - 1):
        for i in range(nx - 1):
            a = j * nx + i
            bm.faces.new((verts[a], verts[a + 1], verts[a + nx + 1], verts[a + nx]))
    me = bpy.data.meshes.new("Terrain")
    bm.to_mesh(me)
    bm.free()
    attr = me.attributes.new("Glow", "FLOAT", "POINT")
    attr.data.foreach_set("value", glow.ravel())
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new("Terrain", me)
    collection.objects.link(ob)
    me.materials.append(mat_rock("Rock", base=cfg.get("rock", (0.022, 0.022, 0.026)), glow=cfg.get("glow", WARM),
                                 glow_strength=cfg.get("vein_glow", 5.0)))
    return ob, (X, Y, Z)


def rift_fill(cfg, collection):
    """Molten crystal light at the bottom of the fissure, shards along its lips and point lights for the haze."""
    path = cfg["rift"]
    rng = random.Random(cfg.get("seed", 3))
    pts = []
    for (ax, ay), (bx, by) in zip(path[:-1], path[1:]):
        n = max(2, int(math.hypot(bx - ax, by - ay) / 0.4))
        for k in range(n):
            t = k / n
            pts.append(Vector((ax + (bx - ax) * t, ay + (by - ay) * t, 0)))
    pts.append(Vector((*path[-1], 0)))
    bm = bmesh.new()
    left, right = [], []
    floor = cfg.get("floor", -1.7)
    depth = cfg.get("rift_depth", 1.1)
    for i, p in enumerate(pts):
        q = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        nrm = Vector((-q.y, q.x, 0)).normalized()
        w = (cfg.get("rift_w", 0.45) + cfg.get("rift_grow", 0.025) * max(p.y, 0)) * 0.75
        z = floor - depth * 0.82
        left.append(bm.verts.new(p + nrm * w * rng.uniform(0.7, 1.1) + Vector((0, 0, z))))
        right.append(bm.verts.new(p - nrm * w * rng.uniform(0.7, 1.1) + Vector((0, 0, z))))
    for i in range(len(pts) - 1):
        bm.faces.new((left[i], left[i + 1], right[i + 1], right[i]))
    ob = bm_object("RiftCore", bm, [mat_emit("RiftCore", cfg.get("glow", WARM), cfg.get("core_strength", 30.0),
                                             core=cfg.get("glow_hot", WARM_HOT))])
    put(ob, collection)
    # Crystal shards growing out of the lips.
    bm = bmesh.new()
    for i in range(cfg.get("rift_shards", 70)):
        p = pts[rng.randrange(len(pts))]
        if p.y < 1.0:
            continue
        w = cfg.get("rift_w", 0.45) + cfg.get("rift_grow", 0.025) * p.y
        side = rng.choice((-1, 1))
        base = p + Vector((side * w * rng.uniform(0.6, 1.3), rng.uniform(-0.3, 0.3), floor - depth * 0.6))
        dirv = Vector((side * rng.uniform(0.1, 0.7), rng.uniform(-0.3, 0.3), 1.0))
        sc = rng.uniform(0.4, 1.0) * (1 + p.y * 0.02)
        shard(bm, base, dirv, 1.1 * sc, 0.14 * sc, rng=rng)
    ob = bm_object("RiftShards", bm, [mat_crystal("RiftShard", cfg.get("glow", WARM), cfg.get("glow_hot", WARM_HOT),
                                                  cfg.get("shard_strength", 6.0))])
    put(ob, collection)
    for k, (x, y) in enumerate(path):
        e = cfg.get("rift_light", 450.0) * (1 + y * 0.05)
        point(f"RiftLight{k}", (x, y, floor - 0.4), cfg.get("glow", WARM), e, 1.0, collection)


def spires(cfg, collection):
    """Huge crystal formation far down the canyon: the waking thing, mostly silhouette with a hot heart."""
    if cfg.get("spire_count", 14) == 0:
        return
    rng = random.Random(cfg.get("seed", 3) + 11)
    cx, cy = cfg.get("spire_at", (2.0, 60.0))
    bm = bmesh.new()
    for i in range(cfg.get("spire_count", 14)):
        base = Vector((cx + rng.uniform(-6, 6), cy + rng.uniform(-3, 4), cfg.get("floor", -1.7) - 1.0))
        dirv = Vector((rng.uniform(-0.45, 0.45), rng.uniform(-0.2, 0.2), 1.0))
        h = rng.uniform(6, 16) * (1.4 if i == 0 else 1.0)
        shard(bm, base, dirv, h, h * 0.09, rng=rng, sides=5, tip=0.3)
    ob = bm_object("Spires", bm, [mat_crystal("Spire", cfg.get("spire_col", WARM_DEEP), cfg.get("glow_hot", WARM_HOT),
                                              cfg.get("spire_strength", 2.2))])
    put(ob, collection)
    point("SpireLight", (cx, cy - 4, 4), cfg.get("glow", WARM), cfg.get("spire_light", 6000.0), 3.0, collection)


def embers(cfg, collection, region, count, size=(0.008, 0.022), colour=(1.0, 0.4, 0.1), strength=40.0, stretch=3.0,
           seed=9, name="Embers"):
    rng = random.Random(seed)
    bm = bmesh.new()
    (x0, x1), (y0, y1), (z0, z1) = region
    for _ in range(count):
        r = rng.uniform(*size)
        y = y0 + (y1 - y0) * rng.random() ** 1.6
        spread = 1.0 + max(y, 0) * 0.06
        p = Vector((rng.uniform(x0, x1) * spread, y, rng.uniform(z0, z1)))
        res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r)
        for v in res["verts"]:
            v.co = Vector((v.co.x, v.co.y, v.co.z * stretch)) + p
    ob = bm_object(name, bm, [mat_emit(name, colour, strength, core=tuple(min(1.0, c * 1.4 + 0.1) for c in colour))])
    ob.visible_shadow = False
    put(ob, collection)
    return ob


def haze(cfg, collection):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = "Haze"
    ob.scale = (80, 90, 18)
    ob.location = (0, 38, 4)
    ob.data.materials.append(mat_haze("Haze", cfg.get("haze_col", (0.55, 0.62, 0.8)), cfg.get("haze", 0.010),
                                      -0.5 / 18 - 0.0, 0.25))
    put(ob, collection)
    return ob


def reflector(collection, strength=3.0, y=-16.0, band=0.62):
    bpy.ops.mesh.primitive_plane_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = "Reflector"
    ob.scale = (40, 14, 1)
    ob.location = (0, y, 3.5)
    ob.rotation_euler = (math.radians(-90), 0, 0)
    ob.data.materials.append(mat_reflector("Reflector", strength, band))
    ob.visible_camera = False
    ob.visible_shadow = False
    ob.visible_volume_scatter = False
    ob.visible_diffuse = False
    put(ob, collection)
    return ob


# =============================================================================================================
# Render
# =============================================================================================================

def enable_gpu():
    scene = bpy.context.scene
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "HIP"
        prefs.refresh_devices()
        ok = False
        for d in prefs.devices:
            d.use = d.type == "HIP"
            ok = ok or d.use
        scene.cycles.device = "GPU" if ok and not C.cli_args().get("cpu") else "CPU"
    except Exception as e:  # noqa: BLE001
        C.log("gpu", e)
        scene.cycles.device = "CPU"
    C.log("device", scene.cycles.device)


def setup_render(w, h, samples, transparent=False):
    s = bpy.context.scene
    s.render.engine = "CYCLES"
    s.cycles.samples = samples
    s.cycles.use_denoising = True
    s.cycles.max_bounces = 8
    s.cycles.volume_bounces = 1
    s.cycles.sample_clamp_indirect = 8.0
    s.cycles.use_adaptive_sampling = True
    s.render.resolution_x = w
    s.render.resolution_y = h
    s.render.resolution_percentage = 100
    s.render.film_transparent = transparent
    s.view_settings.view_transform = "AgX"
    for look in ("AgX - Medium High Contrast", "Medium High Contrast"):
        try:
            s.view_settings.look = look
            break
        except TypeError:
            pass
    s.view_settings.exposure = 0.0


def compositor(grade, use=True):
    s = bpy.context.scene
    s.render.use_compositing = use
    if not use:
        return
    ng = bpy.data.node_groups.new("Comp", "CompositorNodeTree")
    ng.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    rl = ng.nodes.new("CompositorNodeRLayers")
    g1 = ng.nodes.new("CompositorNodeGlare")
    g1.inputs["Type"].default_value = "Bloom"
    g1.inputs["Quality"].default_value = "High"
    g1.inputs["Threshold"].default_value = grade.get("bloom_threshold", 1.0)
    g1.inputs["Smoothness"].default_value = 0.4
    g1.inputs["Strength"].default_value = grade.get("bloom", 0.55)
    g1.inputs["Size"].default_value = grade.get("bloom_size", 0.75)
    g2 = ng.nodes.new("CompositorNodeGlare")
    g2.inputs["Type"].default_value = "Fog Glow"
    g2.inputs["Quality"].default_value = "High"
    g2.inputs["Threshold"].default_value = grade.get("fog_threshold", 2.5)
    g2.inputs["Strength"].default_value = grade.get("fog", 0.25)
    g2.inputs["Size"].default_value = grade.get("fog_size", 0.6)
    cb = ng.nodes.new("CompositorNodeColorBalance")
    cb.inputs["Type"].default_value = "Lift/Gamma/Gain"
    for name, val in (("Lift", grade.get("lift", (1.0, 1.0, 1.02))), ("Gamma", grade.get("gamma", (1, 1, 1))),
                      ("Gain", grade.get("gain", (1, 1, 1)))):
        next(i for i in cb.inputs if i.name == name and i.bl_idname == "NodeSocketColor").default_value = (*val, 1)
    ld = ng.nodes.new("CompositorNodeLensdist")
    ld.inputs["Dispersion"].default_value = grade.get("dispersion", 0.012)
    ld.inputs["Distortion"].default_value = grade.get("distortion", 0.0)
    out = ng.nodes.new("NodeGroupOutput")
    L = ng.links.new
    L(rl.outputs["Image"], g1.inputs["Image"])
    L(g1.outputs["Image"], g2.inputs["Image"])
    L(g2.outputs["Image"], cb.inputs["Image"])
    L(cb.outputs["Image"], ld.inputs["Image"])
    L(ld.outputs["Image"], out.inputs[0])
    s.compositing_node_group = ng


def camera(name, target, distance, lens=40.0, rise=0.6, yaw=0.0, fstop=2.8, dof=True, shift=(0, 0)):
    cd = bpy.data.cameras.new(name)
    cd.lens = lens
    cd.sensor_fit = "HORIZONTAL"
    cd.sensor_width = 36
    cd.shift_x, cd.shift_y = shift
    cd.clip_end = 300
    cam = C.link(bpy.data.objects.new(name, cd))
    t = Vector(target)
    cam.location = t + Vector((math.sin(yaw) * distance, -math.cos(yaw) * distance, rise))
    C.look_at(cam, t)
    cd.dof.use_dof = dof
    cd.dof.focus_distance = (cam.location - t).length
    cd.dof.aperture_fstop = fstop
    return cam


def fit_distance(width, frac, lens=40.0, sensor=36.0):
    """Distance at which an object `width` wide fills `frac` of the frame width."""
    return (width / frac) * lens / sensor


def render_png(cam, w, h, path, samples, grade, transparent=False):
    setup_render(w * SUPER, h * SUPER, samples, transparent)
    compositor(grade, use=not transparent)
    s = bpy.context.scene
    s.camera = cam
    tmp = path + (".exr" if transparent else ".tmp.png")
    s.render.image_settings.file_format = "OPEN_EXR" if transparent else "PNG"
    s.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    if transparent:
        s.render.image_settings.color_depth = "32"
    s.render.filepath = tmp
    bpy.ops.render.render(write_still=True)
    arr = C.image_to_array(tmp)
    os.remove(tmp)
    if transparent:
        return finish_alpha(arr, w, h, path, grade)
    arr = box_down(arr, SUPER)
    C.array_to_image(arr, path)
    C.log("wrote", path)
    return path


def box_down(arr, f):
    h, w = arr.shape[0] // f, arr.shape[1] // f
    return arr[:h * f, :w * f].reshape(h, f, w, f, arr.shape[2]).mean(axis=(1, 3))


def finish_alpha(arr, w, h, path, grade):
    """Linear premultiplied RGBA -> bloom whose light also becomes alpha -> straight colour, saved through the
    scene's AgX view so it matches the opaque stills."""
    rgb, a = arr[..., :3], arr[..., 3:4]
    bright = np.clip(rgb - grade.get("bloom_threshold", 1.0), 0, None)
    glow = np.zeros_like(rgb)
    sc = arr.shape[0] / 720.0
    for radius, weight in ((3.0, 0.5), (10.0, 0.35), (30.0, 0.22), (70.0, 0.1)):
        for c in range(3):
            glow[..., c] += C._blur(bright[..., c], radius * sc) * weight
    glow *= grade.get("bloom", 0.55) * 1.2
    rgb = rgb + glow
    ga = np.clip(glow.max(axis=2, keepdims=True) * 0.9, 0, 1)
    alpha = np.clip(a + ga * (1 - a), 0, 1)
    straight = rgb / np.maximum(alpha, 1e-4)
    out = np.dstack([straight, alpha])
    out = box_down(out, SUPER)
    img = bpy.data.images.new("alpha_out", w, h, alpha=True, float_buffer=True)
    img.pixels.foreach_set(out.astype(np.float32).ravel())
    s = bpy.context.scene
    s.render.image_settings.file_format = "PNG"
    s.render.image_settings.color_mode = "RGBA"
    s.render.image_settings.color_depth = "8"
    img.save_render(path, scene=s)
    bpy.data.images.remove(img)
    C.log("wrote", path)
    return path


def downscale_file(src, dst, factor):
    arr = C.image_to_array(src)
    C.array_to_image(box_down(arr, factor), dst)
    C.log("wrote", dst)


# =============================================================================================================
# Shared title material sets
# =============================================================================================================

def steel_set(prefix, face_base, veins=None, edge=(0.86, 0.88, 0.92), side=(0.045, 0.05, 0.06), seam=None,
              fracture_glow=WARM, rough=0.3):
    return [mat_metal_face(prefix + "Face", face_base, rough=rough, veins=veins),
            mat_polished(prefix + "Edge", edge),
            mat_side(prefix + "Side", side, seam=seam),
            mat_fracture(prefix + "Fracture", fracture_glow),
            mat_chip(prefix + "Chip")]


def stand(obs, location=(0, 0, 0), scale=1.0):
    """Lettering is modelled in the XY plane facing +Z; stand it up facing the camera (-Y)."""
    for ob in obs:
        ob.rotation_euler = (math.pi / 2, 0, 0)
        ob.location = Vector(location)
        ob.scale = (scale, scale, scale)


def chip(ob, outlines, z, rng, prob, size):
    pts = outline_chip_points(outlines, z, rng, prob)
    if not pts:
        return
    cutter = chip_cutter(pts, rng, size)
    boolean(ob, cutter, "DIFFERENCE")
    remove(cutter)


# =============================================================================================================
# Direction 1: FAULTLINE
# =============================================================================================================

FAULT = dict(y0=0.50, slope=-0.012, gap=0.05, shift=(0.05, 0.05, 0.04))


def faultline_title(name="Faultline", word="DEEPWAKE", seed=4):
    rng = random.Random(seed)
    polys, outlines, width = custom_word(word)
    ext, bev = 0.13, 0.04
    front = ext + bev
    mats = steel_set(name, (0.16, 0.17, 0.19),
                     veins=dict(color=WARM, strength=10.0, scale=3.2, width=0.018,
                                mask=("fault", (FAULT["y0"], FAULT["slope"], 0.42)), breakup=1.4, halo=0.02))
    mats[0] = mat_metal_face(name + "Face", (0.2, 0.21, 0.24), rough=0.3, gradient=(0.0, 1.0, 0.35, 1.25),
                             veins=dict(color=WARM, strength=10.0, scale=3.2, width=0.018,
                                        mask=("fault", (FAULT["y0"], FAULT["slope"], 0.42)), breakup=1.4, halo=0.02))
    wordm = poly_curve(name + "Word", polys, ext, bev)
    assign_by_normal(wordm, mats)
    chip(wordm, outlines, front, rng, 0.22, (0.025, 0.06))
    line = jagged_line(-0.5, width + 0.5, FAULT["y0"], FAULT["slope"], 0.16, 0.055, rng)
    upper, lower, band = split_along(wordm, line, FAULT["gap"], name=name)
    # The gap glows: the word inside the gap becomes crystal, pushed back so the edges of the break catch its light.
    band.data.materials.clear()
    band.data.materials.append(mat_crystal(name + "Gap", WARM, (1.0, 0.5, 0.12), 7.0))
    for p in band.data.polygons:
        p.material_index = 0
    band.data.transform(Matrix.Translation((0, 0, -0.025)))
    sx, sy, sz = FAULT["shift"]
    upper.data.transform(Matrix.Translation((sx, sy, sz)))
    # Crystal shards breaking out of the fault, towards the viewer.
    bm = bmesh.new()
    for i in range(9):
        x = rng.uniform(0.2, width - 0.2)
        y = FAULT["y0"] + FAULT["slope"] * x
        shard(bm, (x, y, front * 0.4), (rng.uniform(-0.5, 0.5), rng.uniform(0.1, 0.9), 1.0),
              rng.uniform(0.12, 0.26), rng.uniform(0.025, 0.045), rng=rng)
    shards = bm_object(name + "Shards", bm, [mat_crystal(name + "Shard", WARM, WARM_HOT, 9.0)])
    obs = [upper, lower, band, shards]
    for ob in obs:
        ob.data.transform(Matrix.Translation((-width / 2, 0, 0)))
    return obs, width, 1.0


def hex_pts(r, rot=90.0, n=6):
    return [(r * math.cos(math.radians(rot + i * 360 / n)), r * math.sin(math.radians(rot + i * 360 / n)))
            for i in range(n)]


def faultline_emblem(name="FaultEmblem", seed=8):
    """Fractured steel hex core; crystal bursts out of the break."""
    rng = random.Random(seed)
    mats = steel_set(name, (0.30, 0.32, 0.36),
                     veins=dict(color=WARM, strength=12.0, scale=2.6, width=0.02, mask=("radial", (0, 0, 0.3, 1.0)),
                                breakup=1.1), seam=(COOL, 18.0, 0.09, 0.022))
    outer = chamfer_poly(hex_pts(1.0), 0.09)
    inner = chamfer_poly(hex_pts(0.64), 0.05)
    ring = poly_curve(name + "Ring", [outer, inner], 0.16, 0.045)
    assign_by_normal(ring, mats)
    core = poly_curve(name + "Core", [chamfer_poly(hex_pts(0.46), 0.04)], 0.11, 0.035, z0=-0.02)
    assign_by_normal(core, mats)
    chip(ring, [outer], 0.205, rng, 0.5, (0.04, 0.08))
    plate = join([ring, core], name + "Plate")
    # Fault through the plate, diagonal.
    ang = math.radians(62)
    d = Vector((math.cos(ang), math.sin(ang)))
    line = []
    for k in range(15):
        t = -1.4 + 2.8 * k / 14
        jag = rng.uniform(-0.07, 0.07) if 0 < k < 14 else 0
        line.append((d.x * t - d.y * jag, d.y * t + d.x * jag))
    # split_along expects a polyline running left to right; rotate the frame so the fault is horizontal.
    R = Matrix.Rotation(-ang, 4, "Z")
    plate.data.transform(R)
    hline = [((R @ Vector((x, y, 0))).x, (R @ Vector((x, y, 0))).y) for x, y in line]
    upper, lower, band = split_along(plate, hline, 0.09, name=name)
    remove(band)
    upper.data.transform(Matrix.Translation((0.02, 0.10, 0.03)))
    lower.data.transform(Matrix.Translation((-0.02, -0.06, 0.0)))
    Ri = R.inverted()
    for ob in (upper, lower):
        ob.data.transform(Ri)
    # Crystal burst from the centre and along the break.
    bm = bmesh.new()
    shard(bm, (0, -0.1, 0.05), (0.25, 1.0, 0.55), 1.05, 0.16, rng=rng, sides=6, tip=0.42)
    for i in range(9):
        t = rng.uniform(-0.75, 0.75)
        base = (d.x * t, d.y * t, 0.0)
        dirv = (rng.uniform(-0.8, 0.8) + d.y * 0.6, rng.uniform(0.2, 1.0), 0.6)
        shard(bm, base, dirv, rng.uniform(0.25, 0.6) * (1.2 - abs(t)), rng.uniform(0.05, 0.09), rng=rng)
    crystal = bm_object(name + "Crystal", bm, [mat_crystal(name + "Crys", WARM, (1.0, 0.6, 0.2), 6.0)])
    # Glow plane deep in the break.
    gl = poly_curve(name + "Glow", [hex_pts(0.9)], 0.01, 0.0, z0=-0.06)
    gl.data.materials.append(mat_emit(name + "GlowM", WARM, 6.0, core=WARM_HOT))
    return [upper, lower, crystal, gl], 2.0, 2.0


FAULTLINE_ENV = dict(
    rift=[(0.3, 2.5), (-0.6, 6), (0.8, 10), (-0.5, 15), (1.6, 21), (0.4, 28), (2.4, 38), (1.0, 52), (2.0, 66)],
    rift_w=0.35, rift_grow=0.03, rift_depth=1.2, floor=-1.8, canyon=3.5, canyon_flare=0.2, wall=1.2, moon=3.0,
    spire_count=0,
    glow=WARM, glow_hot=WARM_HOT, core_strength=35.0, rift_light=380.0, vein_glow=5.0,
    spire_at=(1.5, 64.0), spire_strength=0.25, spire_light=400.0, spire_col=(0.5, 0.04, 0.03), haze=0.0018, haze_col=(0.5, 0.58, 0.75), seed=3)

FAULTLINE_GRADE = dict(bloom=0.5, bloom_size=0.8, fog=0.22, lift=(0.99, 1.0, 1.04), gamma=(1.0, 1.0, 1.02),
                       gain=(1.04, 1.0, 0.97), dispersion=0.01)


# =============================================================================================================
# Direction 2: DEEP / WAKE
# =============================================================================================================

def deepwake_title(name="DeepWake", seed=5):
    rng = random.Random(seed)
    heavy = os.path.join(FONTS, "russoone_RussoOne-Regular.ttf")
    wide = os.path.join(FONTS, "michroma_Michroma-Regular.ttf")
    # WAKE: obsidian metal, veins of crystal light rising from below.
    wake_mats = [mat_metal_face(name + "WakeFace", (0.06, 0.05, 0.05), rough=0.34, brushed=False, grime=0.3,
                                gradient=(0.0, 1.3, 0.5, 1.2),
                                veins=dict(color=WARM, strength=16.0, scale=2.6, width=0.02,
                                           mask=("bottom", (1.25, -0.05)), breakup=1.2, halo=0.05)),
                 mat_polished(name + "WakeEdge", (0.55, 0.45, 0.40), rough=0.2, wear=0.6),
                 mat_side(name + "WakeSide", (0.03, 0.025, 0.025)),
                 mat_fracture(name + "WakeFracture"),
                 mat_chip(name + "WakeChip")]
    wake = text_mesh(name + "Wake", "WAKE", heavy, 1.75, 0.16, 0.04, spacing=1.02)
    assign_by_normal(wake, wake_mats)
    bb = [v.co for v in wake.data.vertices]
    wx0, wx1 = min(v.x for v in bb), max(v.x for v in bb)
    wy0, wy1 = min(v.y for v in bb), max(v.y for v in bb)
    # Chips along the bottom where the planet bites into the word.
    pts = [(rng.uniform(wx0, wx1), rng.uniform(wy0, wy0 + 0.25), 0.2) for _ in range(26)]
    pts += [(rng.uniform(wx0, wx1), wy1 - rng.uniform(0, 0.05), 0.2) for _ in range(8)]
    cut = chip_cutter(pts, rng, (0.03, 0.08))
    boolean(wake, cut, "DIFFERENCE")
    remove(cut)
    # Crystal growing up from the bottom of the letters.
    bm = bmesh.new()
    for i in range(14):
        x = rng.uniform(wx0 + 0.1, wx1 - 0.1)
        shard(bm, (x, wy0 + rng.uniform(-0.05, 0.12), rng.uniform(0.0, 0.2)),
              (rng.uniform(-0.4, 0.4), 1.0, rng.uniform(0.2, 0.8)), rng.uniform(0.15, 0.4), rng.uniform(0.03, 0.06),
              rng=rng)
    crys = bm_object(name + "Crys", bm, [mat_crystal(name + "CrysM", WARM, WARM_HOT, 10.0)])
    # DEEP: wide-tracked white steel with a blue seam, the colony's voice.
    deep_mats = [mat_metal_face(name + "DeepFace", (0.78, 0.80, 0.84), rough=0.22, grime=0.25),
                 mat_polished(name + "DeepEdge", (0.95, 0.96, 0.98), rough=0.1, wear=0.2),
                 mat_side(name + "DeepSide", (0.06, 0.07, 0.09), seam=((0.02, 0.2, 1.0), 5.0, 0.06, 0.022)),
                 mat_fracture(name + "DeepFracture", COOL),
                 mat_chip(name + "DeepChip")]
    deep = text_mesh(name + "Deep", "DEEP", wide, 0.62, 0.08, 0.018, spacing=1.9)
    assign_by_normal(deep, deep_mats)
    db = [v.co for v in deep.data.vertices]
    dx0, dx1 = min(v.x for v in db), max(v.x for v in db)
    dy0 = min(v.y for v in db)
    deep.data.transform(Matrix.Translation(((wx0 + wx1) / 2 - (dx0 + dx1) / 2,
                                                                      wy1 + 0.16 - dy0, 0.05)))
    # Two thin blue rules either side of DEEP.
    ddb = [v.co for v in deep.data.vertices]
    dx0, dx1 = min(v.x for v in ddb), max(v.x for v in ddb)
    dy0, dy1 = min(v.y for v in ddb), max(v.y for v in ddb)
    ym = (dy0 + dy1) / 2
    rules = []
    for side in (-1, 1):
        a = dx0 - 0.15 if side < 0 else dx1 + 0.15
        b = wx0 + 0.02 if side < 0 else wx1 - 0.02
        poly = [(min(a, b), ym - 0.018), (max(a, b), ym - 0.018), (max(a, b), ym + 0.018), (min(a, b), ym + 0.018)]
        if side < 0:
            poly = [(b, ym), (b + 0.05, ym - 0.018), (a, ym - 0.018), (a, ym + 0.018), (b + 0.05, ym + 0.018)]
        else:
            poly = [(a, ym - 0.018), (b - 0.05, ym - 0.018), (b, ym), (b - 0.05, ym + 0.018), (a, ym + 0.018)]
        r = poly_curve(name + f"Rule{side}", [poly], 0.02, 0.0, z0=0.06)
        r.data.materials.append(mat_emit(name + "RuleM", (0.02, 0.2, 1.0), 2.2, core=(0.2, 0.55, 1.0)))
        rules.append(r)
    obs = [wake, crys, deep] + rules
    total_h = dy1 - wy0
    for ob in obs:
        ob.data.transform(Matrix.Translation((-(wx0 + wx1) / 2, -wy0, 0)))
    return obs, wx1 - wx0, total_h


def almond(w, h, n=40, pinch=1.0):
    pts = []
    for i in range(n):
        t = i / n * math.tau
        x = w * math.cos(t)
        y = h * math.sin(t)
        y *= (1 - abs(math.cos(t))) ** (0.35 * pinch) if pinch else 1
        pts.append((x, y))
    return pts


def rock_mat(name):
    mat, nt, out = new_mat(name)
    co = objco(nt)
    n1 = noise(nt, co, 3.0, 10.0, 0.65)
    n2 = noise(nt, co, 14.0, 6.0, 0.6)
    col = MIXC(nt, o(n1, "Factor"), (0.012, 0.011, 0.012), (0.06, 0.055, 0.055))
    lines = crack_lines(nt, co, 2.2, 0.02)
    radial = vein_mask(nt, "radial", (0, 0, 0.45, 1.05))
    v = M(nt, "MULTIPLY", lines, radial)
    col = MIXC(nt, v, col, (0, 0, 0))
    nrm = bump(nt, M(nt, "SUBTRACT", M(nt, "ADD", o(n1, "Factor"), M(nt, "MULTIPLY", o(n2, "Factor"), 0.4)), v), 0.8, 0.08)
    principled(nt, out, base=col, rough=0.75, normal=nrm, emit=WARM, emit_s=M(nt, "MULTIPLY", v, 6.0))
    return mat


def deepwake_emblem(name="RiftEye", seed=12):
    """An eye-like rift opening in rock: a hot iris with a slit pupil, held in a colony targeting reticle."""
    rng = random.Random(seed)
    # Rock disc, lumpy.
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=5, radius=1.0)
    rock = bpy.context.active_object
    rock.name = name + "Rock"
    for v in rock.data.vertices:
        p = v.co.copy()
        n = mnoise.fractal(p * 2.2 + Vector((seed, 0, 0)), 0.6, 2.0, 5)
        r = 1.0 + n * 0.12
        v.co = Vector((p.x * r, p.y * r, p.z * r * 0.32))
    rock.data.materials.append(rock_mat(name + "RockM"))
    for p in rock.data.polygons:
        p.use_smooth = True
    # The opening: an almond hole through the rock.
    hole = prism(name + "Hole", almond(0.72, 0.36, pinch=1.0), -1.0, 1.0)
    boolean(rock, hole, "DIFFERENCE")
    remove(hole)
    # Lids: thick lips around the opening, slightly raised.
    lid = poly_curve(name + "Lid", [almond(0.86, 0.48), almond(0.70, 0.34)], 0.05, 0.05, res=2, z0=0.26)
    lid.data.materials.append(rock_mat(name + "LidM"))
    # Iris: a hot domed crystal disc behind the opening; pupil: a dark vertical slit in front of it.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=0.42)
    iris = bpy.context.active_object
    iris.name = name + "Iris"
    iris.scale = (1.0, 1.0, 0.45)
    iris.location = (0, 0, 0.04)
    iris.data.materials.append(mat_iris(name + "IrisM"))
    for p in iris.data.polygons:
        p.use_smooth = True
    bpy.ops.object.transform_apply(location=True, scale=True)
    pupil = poly_curve(name + "Pupil", [almond(0.07, 0.36, n=32, pinch=0.0)], 0.03, 0.02, z0=0.22)
    pm, nt, out = new_mat(name + "PupilM")
    principled(nt, out, base=(0.002, 0.0, 0.0), rough=0.15, coat=1.0)
    pupil.data.materials.append(pm)
    # Reticle: three steel arc segments with a blue seam, and four tick marks.
    mats = [mat_metal_face(name + "RetFace", (0.75, 0.78, 0.82), rough=0.25, grime=0.2),
            mat_polished(name + "RetEdge", (0.95, 0.96, 0.98), rough=0.1),
            mat_side(name + "RetSide", (0.05, 0.06, 0.08), seam=((0.02, 0.2, 1.0), 5.0, 0.05, 0.02))]
    arcs = []
    for k in range(3):
        a0 = math.radians(k * 120 + 18)
        a1 = math.radians(k * 120 + 102)
        n = 24
        outer = [(1.22 * math.cos(a0 + (a1 - a0) * i / n), 1.22 * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]
        inner = [(1.12 * math.cos(a1 - (a1 - a0) * i / n), 1.12 * math.sin(a1 - (a1 - a0) * i / n)) for i in range(n + 1)]
        arc = poly_curve(name + f"Arc{k}", [outer + inner], 0.06, 0.02, z0=0.25)
        assign_by_normal(arc, mats)
        arcs.append(arc)
    ticks = []
    for k in range(3):
        a = math.radians(k * 120 + 60 + 180)
        c, s = math.cos(a), math.sin(a)
        r0, r1 = 1.02, 1.34
        tri = [(c * r0, s * r0), (c * r1 - s * 0.07, s * r1 + c * 0.07), (c * r1 + s * 0.07, s * r1 - c * 0.07)]
        t = poly_curve(name + f"Tick{k}", [tri], 0.04, 0.0, z0=0.27)
        t.data.materials.append(mat_emit(name + "TickM", (0.02, 0.2, 1.0), 2.5, core=(0.2, 0.55, 1.0)))
        ticks.append(t)
    return [rock, lid, iris, pupil] + arcs + ticks, 2.7, 2.7


def mat_iris(name):
    mat, nt, out = new_mat(name)
    co = objco(nt)
    x, y, z = SEP(nt, co)
    r = M(nt, "SQRT", M(nt, "ADD", M(nt, "POWER", x, 2.0), M(nt, "POWER", y, 2.0)))
    # Radial fibres: noise stretched along the radius (polar via atan2).
    at = M(nt, "ARCTAN2", y, x)
    comb = N(nt, "ShaderNodeCombineXYZ")
    nt.links.new(M(nt, "MULTIPLY", at, 6.0), comb.inputs[0])
    nt.links.new(M(nt, "MULTIPLY", r, 1.5), comb.inputs[1])
    fib = noise(nt, o(comb), 3.0, 6.0, 0.6, stretch=(8.0, 1.0, 1.0))
    t = MR(nt, r, 0.05, 0.42, 0.0, 1.0)
    col = MIXC(nt, t, (1.0, 0.9, 0.55), WARM_DEEP)
    col = MIXC(nt, M(nt, "MULTIPLY", MR(nt, o(fib, "Factor"), 0.4, 0.7), 0.6), col, WARM)
    s = M(nt, "MULTIPLY", MR(nt, o(fib, "Factor"), 0.3, 0.7, 0.5, 1.4), MR(nt, r, 0.1, 0.42, 26.0, 4.0))
    principled(nt, out, base=(0.05, 0.01, 0.0), rough=0.1, coat=1.0, emit=col, emit_s=s)
    return mat


DEEPWAKE_ENV = dict(
    rift=[(0.0, 3.0), (0.6, 7), (-0.4, 12), (0.9, 19), (-0.6, 27), (0.5, 38), (-0.2, 55)],
    rift_w=0.42, rift_grow=0.04, rift_depth=1.5, floor=-2.0, canyon=3.5, canyon_flare=0.2, wall=1.3, moon=2.5,
    moon_col=(0.55, 0.5, 0.75), spire_count=0, glow_reach=2.0,
    glow=(1.0, 0.22, 0.04), glow_hot=(1.0, 0.55, 0.15), core_strength=22.0, rift_light=260.0, vein_glow=3.0,
    spire_at=(-1.0, 58.0), spire_col=(0.7, 0.05, 0.03), spire_strength=3.0, haze=0.0018, haze_col=(0.7, 0.6, 0.62),
    rock=(0.025, 0.02, 0.02), seed=7)

DEEPWAKE_GRADE = dict(bloom=0.6, bloom_size=0.85, fog=0.3, lift=(1.01, 0.99, 1.02), gamma=(1.0, 0.99, 1.0),
                      gain=(1.06, 0.99, 0.95), dispersion=0.014)


# =============================================================================================================
# Direction 3: OVERGROWTH
# =============================================================================================================

PINK = (0.9, 0.05, 0.3)
PINK_HOT = (1.0, 0.25, 0.5)
HAZARD = (0.95, 0.62, 0.04)


def mat_paint(name, paint, stripe_top=None, chip=0.6, panel_scale=0.55):
    """Colony armour plate: painted metal with panel seams, chipped to bare steel, a hazard band along the foot."""
    mat, nt, out = new_mat(name)
    co = objco(nt)
    x, y, _ = SEP(nt, co)
    col = paint
    if stripe_top is not None:
        diag = M(nt, "MULTIPLY", M(nt, "ADD", x, y), 5.0)
        stripes = M(nt, "GREATER_THAN", M(nt, "SUBTRACT", diag, M(nt, "FLOOR", diag)), 0.5)
        hz = MIXC(nt, stripes, (0.015, 0.015, 0.016), HAZARD)
        band = M(nt, "LESS_THAN", y, stripe_top)
        col = MIXC(nt, band, paint, hz)
    brick = N(nt, "ShaderNodeTexBrick", {"Vector": co, "Scale": panel_scale, "Mortar Size": 0.007,
                                          "Mortar Smooth": 0.2, "Bias": 0.0},
              offset=0.5, squash=1.0)
    seam = M(nt, "SUBTRACT", 1.0, o(brick, "Factor"))
    col = MIXC(nt, M(nt, "MULTIPLY", seam, 0.85), col, (0.02, 0.02, 0.022))
    n = noise(nt, co, 6.0, 9.0, 0.7)
    edge_n = noise(nt, co, 22.0, 6.0, 0.6)
    bare = MR(nt, M(nt, "ADD", o(n, "Factor"), M(nt, "MULTIPLY", o(edge_n, "Factor"), 0.25)),
              0.77 - (chip - 0.5) * 0.2, 0.80 - (chip - 0.5) * 0.2, 0.0, 1.0)
    grime = MR(nt, o(noise(nt, co, 1.5, 6.0), "Factor"), 0.45, 0.7, 1.0, 0.55)
    gm = N(nt, "ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
    setv(nt, gm, "Factor", 1.0)
    setv(nt, gm, "A", col)
    cg = N(nt, "ShaderNodeCombineXYZ")
    for i in range(3):
        nt.links.new(grime, cg.inputs[i])
    nt.links.new(o(cg), sock(gm.inputs, "B"))
    col = MIXC(nt, bare, o(gm, "Result"), (0.5, 0.5, 0.52))
    rough = MR(nt, bare, 0.0, 1.0, 0.48, 0.3, smooth=False)
    h = M(nt, "ADD", M(nt, "MULTIPLY", seam, -0.6), M(nt, "MULTIPLY", bare, -0.3))
    nrm = bump(nt, h, 0.35, 0.01, bevel_normal(nt, 0.012))
    principled(nt, out, base=col, metal=bare, rough=rough, normal=nrm, spec=0.5)
    return mat


def overgrowth_set(prefix, stripe_top):
    return [mat_paint(prefix + "Paint", (0.3, 0.33, 0.38), stripe_top=stripe_top),
            mat_polished(prefix + "Edge", (0.72, 0.73, 0.76), rough=0.22, wear=0.5),
            mat_side(prefix + "Side", (0.05, 0.055, 0.06)),
            mat_fracture(prefix + "Fracture", PINK, 4.0),
            mat_chip(prefix + "Chip")]


def piercing_crystals(specs, rng, colour=PINK, core=PINK_HOT, strength=4.0, hole=1.15, name="Pierce"):
    """Crystals given as (base, direction, length, radius) plus a cutter mesh that is a little fatter."""
    bm = bmesh.new()
    cut = bmesh.new()
    for base, dirv, length, radius in specs:
        seed = rng.random()
        shard(bm, base, dirv, length, radius, rng=random.Random(seed), sides=6, tip=0.36)
        shard(cut, base, dirv, length * 1.02, radius * hole, rng=random.Random(seed), sides=6, tip=0.36,
              mat_index=3)
    crystal = bm_object(name, bm, [mat_crystal(name + "M", colour, core, strength, coat=0.15, spec=0.25)])
    cutter = bm_object(name + "Cut", cut, [])
    return crystal, cutter


def overgrowth_title(name="Overgrowth", seed=6):
    rng = random.Random(seed)
    font = os.path.join(C.REPO, "Assets", "_Project", "Art", "UI", "Fonts", "ChakraPetch-Bold.ttf")
    word = text_mesh(name + "Word", "DEEPWAKE", font, 1.6, 0.15, 0.03, spacing=1.04)
    bb = [v.co for v in word.data.vertices]
    x0, x1 = min(v.x for v in bb), max(v.x for v in bb)
    y0, y1 = min(v.y for v in bb), max(v.y for v in bb)
    word.data.transform(Matrix.Translation((-(x0 + x1) / 2, -y0, 0)))
    width, height = x1 - x0, y1 - y0
    assign_by_normal(word, overgrowth_set(name, height * 0.2))
    pts = [(rng.uniform(-width / 2, width / 2), rng.choice((0.0, height)) + rng.uniform(-0.03, 0.03), 0.18)
           for _ in range(28)]
    cut = chip_cutter(pts, rng, (0.03, 0.07))
    boolean(word, cut, "DIFFERENCE")
    remove(cut)
    # Alien crystal growing up through the colony's letters: a few big spears and a crust along the foot.
    specs = []
    for fx, lean, length in ((-0.40, -0.25, 1.9), (-0.08, 0.12, 1.35), (0.21, -0.1, 1.7), (0.44, 0.3, 1.25)):
        specs.append(((fx * width, -0.25, 0.09), (lean, 1.0, 0.18), length * height, 0.12 * height / 1.15))
    for _ in range(26):
        fx = rng.uniform(-0.5, 0.5)
        specs.append(((fx * width, rng.uniform(-0.1, 0.05), rng.uniform(-0.05, 0.25)),
                      (rng.uniform(-0.8, 0.8), 1.0, rng.uniform(0.0, 0.9)), rng.uniform(0.12, 0.42) * height,
                      rng.uniform(0.045, 0.08)))
    crystal, cutter = piercing_crystals(specs, rng)
    boolean(word, cutter, "DIFFERENCE")
    remove(cutter)
    return [word, crystal], width, height


def overgrowth_emblem(name="OvergrowthMark", seed=14):
    """The colony's landing chevrons, painted armour plate, split by a crystal spear growing up through them."""
    rng = random.Random(seed)
    mats = overgrowth_set(name, None)
    mats[0] = mat_paint(name + "Paint", (0.3, 0.33, 0.38), stripe_top=None, panel_scale=0.45)
    stripe = [mat_paint(name + "Hz", HAZARD, stripe_top=9.0, chip=0.45)] + mats[1:]
    plates = []
    for k, (L, t) in enumerate(((0.9, 0.30), (1.3, 0.26), (1.7, 0.22))):
        a = math.radians(34)
        tip_y = 0.55 - k * 0.52
        c, s = math.cos(a), math.sin(a)
        outer = [(-L * c, tip_y - L * s), (0, tip_y), (L * c, tip_y - L * s)]
        poly = [outer[0], outer[1], outer[2], (outer[2][0] - t * 0.55, outer[2][1] - t * 0.5 - 0.02),
                (0, tip_y - t / c), (outer[0][0] + t * 0.55, outer[0][1] - t * 0.5 - 0.02)]
        p = poly_curve(name + f"Chev{k}", [chamfer_poly(poly, 0.035)], 0.1, 0.03)
        assign_by_normal(p, stripe if k == 1 else mats)
        plates.append(p)
    plates = join(plates, name + "Plates")
    specs = [((0.0, -1.55, 0.06), (0.0, 1.0, 0.12), 2.6, 0.26),
             ((-0.14, -1.3, 0.05), (-0.45, 1.0, 0.2), 1.3, 0.13),
             ((0.15, -1.35, 0.05), (0.5, 1.0, 0.2), 1.1, 0.11)]
    for _ in range(8):
        specs.append(((rng.uniform(-0.35, 0.35), -1.5, rng.uniform(-0.05, 0.2)),
                      (rng.uniform(-1, 1), 1.0, rng.uniform(0, 0.8)), rng.uniform(0.25, 0.6), rng.uniform(0.04, 0.06)))
    crystal, cutter = piercing_crystals(specs, rng, strength=5.0, name=name + "Crys")
    boolean(plates, cutter, "DIFFERENCE")
    remove(cutter)
    return [plates, crystal], 2.5, 2.5


OVERGROWTH_ENV = dict(
    rift=[(-0.4, 2.5), (0.5, 6), (-0.7, 11), (0.9, 17), (-0.2, 25), (1.5, 36), (0.2, 50)],
    rift_w=0.3, rift_grow=0.028, rift_depth=1.1, floor=-1.7, canyon=3.8, canyon_flare=0.22, wall=1.25, moon=4.0,
    moon_col=(0.42, 0.6, 1.0), spire_count=0, glow=PINK, glow_hot=PINK_HOT, core_strength=18.0, rift_light=220.0,
    vein_glow=3.0, haze=0.0022, haze_col=(0.5, 0.6, 0.8), rock=(0.02, 0.022, 0.028), seed=21)

OVERGROWTH_GRADE = dict(bloom=0.5, bloom_size=0.8, fog=0.22, lift=(0.99, 1.0, 1.04), gamma=(1.0, 1.0, 1.02),
                        gain=(1.02, 1.0, 1.0), dispersion=0.012)


# =============================================================================================================
# Directions: scene assembly and outputs
# =============================================================================================================

DIRECTIONS = {
    "faultline": dict(label="Faultline", title=faultline_title, emblem=faultline_emblem, env=FAULTLINE_ENV,
                      grade=FAULTLINE_GRADE),
    "deepwake": dict(label="DeepWake", title=deepwake_title, emblem=deepwake_emblem, env=DEEPWAKE_ENV,
                     grade=DEEPWAKE_GRADE),
    "overgrowth": dict(label="Overgrowth", title=overgrowth_title, emblem=overgrowth_emblem, env=OVERGROWTH_ENV,
                       grade=OVERGROWTH_GRADE),
}


def build_env(d, col_env):
    env = d["env"]
    world((0.0008, 0.0012, 0.0026), 1.0)
    terrain(env, col_env)
    rift_fill(env, col_env)
    spires(env, col_env)
    haze(env, col_env)
    ember_col = tuple(min(1.0, c) for c in env.get("glow", WARM))
    embers(env, col_env, ((-7, 7), (1.5, 40), (-1.6, 6)), 150, size=(0.005, 0.016), colour=ember_col, strength=7.0,
           seed=env.get("seed", 3))
    embers(env, col_env, ((-4, 4), (1.5, 6), (-1.0, 3.5)), 30, size=(0.006, 0.012), colour=COOL_HOT, strength=10.0,
           stretch=1.0, seed=env.get("seed", 3) + 1, name="Motes")


def lights(col, title_w, receivers, warm_up=WARM, rim=COOL, key=1.0):
    """Studio lights for the lettering only (light linking): the environment is lit by its own rift and crystals,
    so the rim and key never turn the rock into a grey sheen."""
    area("Rim", (-title_w * 0.4, 7.0, 5.5), (0, 0, 0.5), (0.55, 0.72, 1.0), 2500 * key, (6, 1.5), col, 30)
    area("RimR", (title_w * 0.55, 6.0, 3.0), (0, 0, 0.5), (0.55, 0.72, 1.0), 1200 * key, (3, 1.5), col, 30)
    area("Key", (title_w * 0.25, -9.0, 6.0), (0, 0, 0.4), (0.85, 0.9, 1.0), 700 * key, (8, 3), col, 35)
    area("Under", (0, -1.5, -2.2), (0, 0, 0.6), warm_up, 500 * key, (title_w, 0.6), col, 50)
    for ob in col.objects:
        if ob.type == "LIGHT":
            ob.light_linking.receiver_collection = receivers
    reflector(col, 0.9)


def moon(col, col_env, env):
    """A cold sky light from far behind, linked to the environment only: rims the cliffs and crystal silhouettes."""
    ld = bpy.data.lights.new("Moon", "SUN")
    ld.color = env.get("moon_col", (0.45, 0.6, 1.0))
    ld.energy = env.get("moon", 0.8)
    ld.angle = math.radians(3)
    ob = bpy.data.objects.new("Moon", ld)
    col.objects.link(ob)
    ob.rotation_euler = (math.radians(env.get("moon_pitch", -68)), 0, math.radians(env.get("moon_yaw", 160)))
    ob.light_linking.receiver_collection = col_env
    return ob


def place(obs, collection, at, scale=1.0):
    for ob in obs:
        put(ob, collection)
    stand(obs, at, scale)


def build(key, series=False):
    C.reset_scene()
    d = DIRECTIONS[key]
    col_title, col_emb, col_env, col_lit = coll("Title"), coll("Emblem"), coll("Env"), coll("Lights")
    tobs, tw, th = d["title"]()
    tscale = 1.0
    place(tobs, col_title, (0, 0, 0), tscale)
    eobs, ew, eh = d["emblem"]()
    place(eobs, col_emb, (0, 0.05, 0), 1.0)
    # Fit the emblem to the measured bounds: as tall as the lettering block plus a little, left of the title.
    (x0, _, z0), (x1, _, z1) = world_bbox(eobs)
    e_scale = (th * 1.32) / max(z1 - z0, (x1 - x0) * 0.8)
    for ob in eobs:
        ob.scale = (e_scale,) * 3
    (x0, _, z0), (x1, _, z1) = world_bbox(eobs)
    dx = -(tw / 2) - th * 0.35 - x1
    dz = th * 0.5 - (z0 + z1) / 2
    for ob in eobs:
        ob.location.x += dx
        ob.location.z += dz
    (x0, _, z0), (x1, _, z1) = world_bbox(eobs)
    emb_x, emb_z, emb_w, emb_h = (x0 + x1) / 2, (z0 + z1) / 2, x1 - x0, z1 - z0
    if series:
        series_line(tw, th, col_title)
    build_env(d, col_env)
    receivers = bpy.data.collections.new("StudioReceivers")
    receivers.children.link(col_title)
    receivers.children.link(col_emb)
    lights(col_lit, tw, receivers)
    moon(col_lit, col_env, d["env"])
    return dict(d=d, tw=tw, th=th, emb_x=emb_x, emb_z=emb_z, emb_w=emb_w, emb_h=emb_h,
                cols=(col_title, col_emb, col_env, col_lit))


def world_bbox(obs):
    bpy.context.view_layer.update()
    pts = [ob.matrix_world @ Vector(c) for ob in obs if ob.type == "MESH" for c in ob.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def series_line(tw, th, col):
    font = os.path.join(FONTS, "michroma_Michroma-Regular.ttf")
    t = text_mesh("Series", SERIES_LINE, font, 0.30, 0.03, 0.008, spacing=1.7)
    bb = [v.co for v in t.data.vertices]
    x0, x1 = min(v.x for v in bb), max(v.x for v in bb)
    y0 = min(v.y for v in bb)
    t.data.transform(Matrix.Translation((-(x0 + x1) / 2, -y0, 0)))
    mats = [mat_metal_face("SerFace", (0.82, 0.84, 0.88), rough=0.2, grime=0.1),
            mat_polished("SerEdge", (0.95, 0.96, 0.98)), mat_side("SerSide", (0.05, 0.06, 0.08))]
    assign_by_normal(t, mats)
    w = x1 - x0
    rules = []
    for side in (-1, 1):
        a = side * (w / 2 + 0.2)
        b = side * (tw / 2)
        lo, hi = sorted((a, b))
        poly = [(lo, 0.13), (hi, 0.13), (hi, 0.16), (lo, 0.16)]
        r = poly_curve(f"SerRule{side}", [poly], 0.01, 0.0)
        r.data.materials.append(mat_emit("SerRuleM", COOL, 12.0, core=COOL_HOT))
        rules.append(r)
    obs = [t] + rules
    for ob in obs:
        put(ob, col)
    stand(obs, (0, 0.02, th + 0.22))
    return obs


def set_visible(cols, title=True, emblem=True, env=True):
    col_title, col_emb, col_env, _ = cols
    col_title.hide_render = not title
    col_emb.hide_render = not emblem
    col_env.hide_render = not env


def outputs(key, only=None, samples=160, series=False):
    st = build(key, series=series)
    d, tw, th = st["d"], st["tw"], st["th"]
    label = d["label"] + ("_series" if series else "")
    grade = d["grade"]
    res = {}
    title_h = th + (0.55 if series else 0)
    want = lambda k: only is None or k in only  # noqa: E731
    if want("header"):
        set_visible(st["cols"], True, True, True)
        left = st["emb_x"] - st["emb_w"] / 2
        right = tw / 2
        cx = (left + right) / 2
        width = right - left
        dist = fit_distance(width, 0.80)
        cam = camera("CamHeader", (cx, 0, title_h * 0.5), dist, rise=d["env"].get("cam_rise", 0.25), fstop=2.0, shift=(0, -0.02))
        res["header"] = render_png(cam, *HEADER, os.path.join(OUT, f"{label}_header_920x430.png"), samples, grade)
    if want("capsule"):
        set_visible(st["cols"], True, False, True)
        dist = fit_distance(tw, 0.80)
        cam = camera("CamCapsule", (0, 0, title_h * 0.5), dist, rise=d["env"].get("cam_rise", 0.25), fstop=2.0, shift=(0, -0.03))
        res["capsule"] = render_png(cam, *CAPSULE, os.path.join(OUT, f"{label}_capsule_616x353.png"), samples, grade)
    if want("library"):
        set_visible(st["cols"], True, False, False)
        dist = fit_distance(tw, 0.86)
        cam = camera("CamLibrary", (0, 0, title_h * 0.5), dist, rise=0.2, dof=False)
        res["library"] = render_png(cam, *LIBRARY, os.path.join(OUT, f"{label}_library_logo.png"), samples, grade,
                                    transparent=True)
    if want("emblem") and not series:
        set_visible(st["cols"], False, True, True)
        dist = fit_distance(max(st["emb_w"], st["emb_h"]), 0.84, lens=50)
        cam = camera("CamEmblem", (st["emb_x"], 0.05, st["emb_z"]), dist, lens=50, rise=0.1, fstop=1.8)
        global SUPER
        keep = SUPER
        SUPER = 1
        big = os.path.join(OUT, f"_{label}_emblem_1024.png")
        render_png(cam, EMBLEM_RENDER, EMBLEM_RENDER, big, samples, grade)
        SUPER = keep
        downscale_file(big, os.path.join(OUT, f"{label}_emblem_512.png"), 2)
        downscale_file(big, os.path.join(OUT, f"{label}_emblem_256.png"), 4)
        os.remove(big)
        res["emblem"] = os.path.join(OUT, f"{label}_emblem_512.png")
    return res


# =============================================================================================================
# Contact sheet
# =============================================================================================================

def paste(sheet, img, x, y_top):
    """Paste with the top-left corner at (x, y_top) in a top-down layout (Blender images are bottom-up)."""
    H = sheet.shape[0]
    h, w = img.shape[:2]
    y0 = H - y_top - h
    if img.shape[2] == 4:
        a = img[..., 3:4]
        sheet[y0:y0 + h, x:x + w, :3] = sheet[y0:y0 + h, x:x + w, :3] * (1 - a) + img[..., :3] * a
    else:
        sheet[y0:y0 + h, x:x + w, :3] = img[..., :3]


def checker(w, h, a=0.06, b=0.09, cell=16):
    ys, xs = np.mgrid[0:h, 0:w]
    c = np.where(((xs // cell) + (ys // cell)) % 2 == 0, a, b).astype(np.float32)
    return np.dstack([c, c, c, np.ones_like(c)])


def contact_sheet():
    pad = 24
    rows = []
    for key in DIRECTIONS:
        lab = DIRECTIONS[key]["label"]
        rows.append([os.path.join(OUT, f"{lab}_{s}") for s in
                     ("header_920x430.png", "capsule_616x353.png", "library_logo.png", "emblem_256.png")])
    series = os.path.join(OUT, "Faultline_series_header_920x430.png")
    lib_w, lib_h = 640, 360
    W = pad + 920 + pad + 616 + pad + 256 + pad
    row_h = max(430, 353) + pad + lib_h
    H = pad + len(rows) * (row_h + pad) + (430 + pad if os.path.exists(series) else 0)
    sheet = np.zeros((H, W, 4), np.float32)
    sheet[..., :3] = 0.03
    sheet[..., 3] = 1
    y = pad
    for header, capsule, library, emb in rows:
        if not os.path.exists(header):
            y += row_h + pad
            continue
        paste(sheet, C.image_to_array(header), pad, y)
        paste(sheet, C.image_to_array(capsule), pad + 920 + pad, y)
        e = C.image_to_array(emb)
        paste(sheet, e, pad + 920 + pad + 616 + pad, y)
        e64 = box_down(e, 4)
        paste(sheet, e64, pad + 920 + pad + 616 + pad, y + 256 + pad)
        lib = C.image_to_array(library)
        lib = box_down(lib, 2)
        bg = checker(lib.shape[1], lib.shape[0])
        x = pad
        paste(sheet, bg, x, y + 430 + pad)
        paste(sheet, lib, x, y + 430 + pad)
        small = box_down(C.image_to_array(capsule), 2)   # 308x176: small-size legibility check
        paste(sheet, small, pad + lib_w + pad, y + 430 + pad)
        y += row_h + pad
    if os.path.exists(series):
        paste(sheet, C.image_to_array(series), pad, y)
        sl = os.path.join(OUT, "Faultline_series_library_logo.png")
        if os.path.exists(sl):
            lib = box_down(C.image_to_array(sl), 2)
            paste(sheet, checker(lib.shape[1], lib.shape[0]), pad + 920 + pad, y)
            paste(sheet, lib, pad + 920 + pad, y)
    path = os.path.join(OUT, "Deepwake_v2_contact_sheet.png")
    C.array_to_image(sheet, path)
    C.log("sheet", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    args = C.cli_args()
    if args.get("sheet"):
        contact_sheet()
        return
    enable_gpu()
    only = set(args["only"].split(",")) if args.get("only") else None
    samples = int(args.get("samples", 160))
    outputs(args.get("dir", "faultline"), only, samples, series=bool(args.get("series")))


if __name__ == "__main__":
    main()
