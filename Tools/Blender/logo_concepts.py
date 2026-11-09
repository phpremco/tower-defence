"""Game logo concepts for the three shortlisted names (Docs/NAME_BRAINSTORM.md, 2026-11-09): an emblem plus a wordmark.

Concepts (all original, no references):
- VEINBREAK: a chamfered hexagon plate (the human colony, NTI's Nova Hex) split along a jagged crack; warm Crysium shards
  burst out of the glowing break, a cool blue rim light outlines the plate. A thin warm crack runs under the wordmark.
- SHARDLINE: a diamond frame with a cool blue defence line across it; three warm crystal shards rise through the line.
  The line continues under the wordmark as a light rail.
- DEEPWAKE: a dark planet disc with a cool atmosphere limb and a colony orbit ring; in its lower half a warm eye opens,
  with glowing veins spreading from it.

Look: dark background, emissive glow with a bloom pass, cool tech blue/white (human) against warm red/orange (alien).
Wordmarks use Chakra Petch (SIL OFL 1.1, already the UI font, Docs/CREDITS.md).

Each concept is rendered once as a Steam header (920x430, with a tagline and a planet limb in the background) and once as
a small capsule (231x87, rendered at 4x and area-downsampled so thin strokes stay smooth), plus a contact sheet.

Out: ArtSource/Previews/Logo/<Name>_header.png, <Name>_capsule.png, Logo_contact_sheet.png

Run: blender --background --factory-startup --python Tools/Blender/logo_concepts.py [-- --only veinbreak]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402

OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Logo")
FONT_DIR = os.path.join(C.REPO, "Assets", "_Project", "Art", "UI", "Fonts")
FONT_BOLD = os.path.join(FONT_DIR, "ChakraPetch-Bold.ttf")
FONT_MED = os.path.join(FONT_DIR, "ChakraPetch-Medium.ttf")

HEADER = (920, 430)
CAPSULE = (231, 87)
CAPSULE_SUPER = 4

# Linear colours. Cool = the colony, warm = the living planet.
COOL = (0.25, 0.62, 1.0)
COOL_WHITE = (0.75, 0.9, 1.0)
WARM = (1.0, 0.32, 0.06)
WARM_HOT = (1.0, 0.62, 0.18)
WARM_DEEP = (0.55, 0.03, 0.05)
GUNMETAL = (0.035, 0.042, 0.055)
BG = (0.004, 0.005, 0.009)


# ------------------------------------------------------------------------------------------------------------
# Materials
# ------------------------------------------------------------------------------------------------------------

def _principled(name):
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except AttributeError:
        pass
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    return mat, nt, bsdf


def mat_emit(name, colour, strength):
    mat, nt, bsdf = _principled(name)
    bsdf.inputs["Base Color"].default_value = (0, 0, 0, 1)
    bsdf.inputs["Emission Color"].default_value = (*colour, 1)
    bsdf.inputs["Emission Strength"].default_value = strength
    bsdf.inputs["Roughness"].default_value = 0.5
    return mat


def mat_metal(name, colour=GUNMETAL, rough=0.32, metallic=0.85):
    mat, nt, bsdf = _principled(name)
    bsdf.inputs["Base Color"].default_value = (*colour, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    return mat


def mat_gradient_emit(name, stops, strength, axis="Z", fresnel=0.0):
    """Emission whose colour follows the object's generated coordinate along `axis` (a colour ramp of `stops`)."""
    mat, nt, bsdf = _principled(name)
    bsdf.inputs["Base Color"].default_value = (0.02, 0.01, 0.01, 1)
    bsdf.inputs["Roughness"].default_value = 0.25
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs[axis], ramp.inputs["Fac"])
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = stops[0][0], (*stops[0][1], 1)
    els[1].position, els[1].color = stops[-1][0], (*stops[-1][1], 1)
    for pos, col in stops[1:-1]:
        e = els.new(pos)
        e.color = (*col, 1)
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Emission Color"])
    if fresnel:
        # Facet edges flare: a layer-weight term lifts the emission at grazing angles.
        lw = nt.nodes.new("ShaderNodeLayerWeight")
        lw.inputs["Blend"].default_value = 0.35
        mr = nt.nodes.new("ShaderNodeMapRange")
        mr.inputs["To Min"].default_value = strength
        mr.inputs["To Max"].default_value = strength * (1 + fresnel)
        nt.links.new(lw.outputs["Facing"], mr.inputs["Value"])
        nt.links.new(mr.outputs["Result"], bsdf.inputs["Emission Strength"])
    else:
        bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def mat_crystal(name):
    """Crysium as the planet's warm crystal: lit, glossy facets (so the key light separates them) over an inner glow
    that runs deep red at the root to orange and a pale hot tip."""
    mat, nt, bsdf = _principled(name)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Y"], ramp.inputs["Fac"])
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.1, (0.35, 0.0, 0.04, 1)
    els[1].position, els[1].color = 1.0, (1.0, 0.5, 0.12, 1)
    e = els.new(0.5)
    e.color = (1.0, 0.16, 0.02, 1)
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["To Min"].default_value = 3.0
    mr.inputs["To Max"].default_value = 0.6
    nt.links.new(lw.outputs["Facing"], mr.inputs["Value"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Emission Color"])
    nt.links.new(mr.outputs["Result"], bsdf.inputs["Emission Strength"])
    bsdf.inputs["Base Color"].default_value = (0.12, 0.01, 0.005, 1)
    bsdf.inputs["Roughness"].default_value = 0.18
    bsdf.inputs["Metallic"].default_value = 0.0
    return mat


def mat_wordmark(name, top=(0.95, 0.97, 1.0), bottom=(0.32, 0.5, 0.82), glow=0.55):
    """Lit letters: the front face runs white at the top to steel blue at the bottom (light top, dark base, like the
    towers); bevels stay metal so the light picks out the edges."""
    mat, nt, bsdf = _principled(name)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Y"], ramp.inputs["Fac"])
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.05, (*bottom, 1)
    els[1].position, els[1].color = 0.95, (*top, 1)
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sepn = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sepn.inputs[0])
    front = nt.nodes.new("ShaderNodeMapRange")
    front.inputs["From Min"].default_value = 0.9
    front.inputs["From Max"].default_value = 0.99
    nt.links.new(sepn.outputs["Z"], front.inputs["Value"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    nt.links.new(front.outputs["Result"], mix.inputs["Factor"])
    mix.inputs["A"].default_value = (0.18, 0.22, 0.3, 1)
    nt.links.new(ramp.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    em = nt.nodes.new("ShaderNodeMath")
    em.operation = "MULTIPLY"
    em.inputs[1].default_value = glow
    nt.links.new(front.outputs["Result"], em.inputs[0])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Emission Color"])
    nt.links.new(em.outputs["Value"], bsdf.inputs["Emission Strength"])
    bsdf.inputs["Metallic"].default_value = 0.6
    bsdf.inputs["Roughness"].default_value = 0.28
    return mat


# ------------------------------------------------------------------------------------------------------------
# Geometry helpers (logo plane = XY, the camera looks down -Z)
# ------------------------------------------------------------------------------------------------------------

def obj_from_bmesh(name, bm, mat):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = C.link(bpy.data.objects.new(name, me))
    me.materials.append(mat)
    return ob


def prism(name, pts, z0, z1, mat, bevel=0.0, segments=3):
    """Extrudes a 2D polygon (CCW list of (x, y)) between z0 and z1, with an optional bevel."""
    bm = bmesh.new()
    bot = [bm.verts.new((x, y, z0)) for x, y in pts]
    top = [bm.verts.new((x, y, z1)) for x, y in pts]
    bm.faces.new(top)
    bm.faces.new(list(reversed(bot)))
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    ob = obj_from_bmesh(name, bm, mat)
    if bevel:
        mod = ob.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.limit_method = "ANGLE"
        mod.angle_limit = math.radians(25)
        mod.harden_normals = False
    return ob


def ring_mesh(name, outer, inner, z, mat):
    """A flat band between two closed loops with the same vertex count."""
    bm = bmesh.new()
    vo = [bm.verts.new((x, y, z)) for x, y in outer]
    vi = [bm.verts.new((x, y, z)) for x, y in inner]
    n = len(outer)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((vo[i], vo[j], vi[j], vi[i]))
    return obj_from_bmesh(name, bm, mat)


def tube(name, pts, radius, mat, z=0.0, cyclic=False, taper=None):
    """A round tube along a 2D/3D polyline (curve with bevel); `taper` = (start, end) radius factors."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = radius
    cu.bevel_resolution = 3
    cu.use_fill_caps = True
    sp = cu.splines.new("POLY")
    sp.points.add(len(pts) - 1)
    for i, p in enumerate(pts):
        pz = p[2] if len(p) > 2 else z
        w = 1.0
        if taper:
            t = i / max(1, len(pts) - 1)
            w = taper[0] + (taper[1] - taper[0]) * t
        sp.points[i].co = (p[0], p[1], pz, 1)
        sp.points[i].radius = w
    sp.use_cyclic_u = cyclic
    ob = C.link(bpy.data.objects.new(name, cu))
    cu.materials.append(mat)
    return ob


def shard(name, base, direction, length, radius, mat, z=0.3, sides=6, twist=0.0, flat=0.55):
    """A faceted Crysium shard (bipyramid, long tip, short root) lying in the logo plane, raised to z."""
    bm = bmesh.new()
    tip = bm.verts.new((0, length, 0))
    root = bm.verts.new((0, -length * 0.14, 0))
    ring = []
    for i in range(sides):
        a = 2 * math.pi * i / sides + twist
        ring.append(bm.verts.new((math.cos(a) * radius, length * 0.18, math.sin(a) * radius * flat)))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((ring[i], ring[j], tip))
        bm.faces.new((ring[j], ring[i], root))
    ob = obj_from_bmesh(name, bm, mat)
    ang = math.atan2(direction[1], direction[0]) - math.pi / 2
    ob.rotation_euler = (0, 0, ang)
    ob.location = (base[0], base[1], z)
    return ob


def chamfered_hex(r, cut, rot=90.0):
    hexa = [(r * math.cos(math.radians(rot + 60 * i)), r * math.sin(math.radians(rot + 60 * i))) for i in range(6)]
    out = []
    for i in range(6):
        a, b, c = np.array(hexa[i - 1]), np.array(hexa[i]), np.array(hexa[(i + 1) % 6])
        out.append(tuple(b + (a - b) * cut))
        out.append(tuple(b + (c - b) * cut))
    return out


def jagged(p0, p1, n, amp, seed):
    rng = np.random.default_rng(seed)
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = p1 - p0
    nrm = np.array((-d[1], d[0])) / np.linalg.norm(d)
    pts = []
    for i in range(1, n):
        t = i / n + rng.uniform(-0.25, 0.25) / n
        off = amp * (1 if i % 2 else -1) * rng.uniform(0.5, 1.0)
        pts.append(tuple(p0 + d * t + nrm * off))
    return pts


def offset(pts, d):
    return [(x + d[0], y + d[1]) for x, y in pts]


def scale_pts(pts, s, c=(0.0, 0.0)):
    return [((x - c[0]) * s + c[0], (y - c[1]) * s + c[1]) for x, y in pts]


def text_obj(name, body, font, size, mat, extrude=0.06, bevel=0.018, spacing=1.0, z=0.0):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = body
    cu.font = font
    cu.size = size
    cu.extrude = extrude
    cu.bevel_depth = bevel
    cu.bevel_resolution = 2
    cu.space_character = spacing
    cu.align_x = "LEFT"
    cu.align_y = "BOTTOM_BASELINE"
    ob = C.link(bpy.data.objects.new(name, cu))
    cu.materials.append(mat)
    ob.location = (0, 0, z)
    return ob


def bbox(ob):
    bpy.context.view_layer.update()
    pts = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    xs, ys = [p.x for p in pts], [p.y for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def union_bbox(obs):
    bs = [bbox(o) for o in obs]
    return min(b[0] for b in bs), min(b[1] for b in bs), max(b[2] for b in bs), max(b[3] for b in bs)


# ------------------------------------------------------------------------------------------------------------
# Emblems (each fits roughly a 2.4 x 2.4 box centred on the origin; returns its objects)
# ------------------------------------------------------------------------------------------------------------

def emblem_veinbreak():
    obs = []
    plate = mat_metal("Plate", (0.03, 0.035, 0.045), rough=0.3)
    face = mat_metal("Face", (0.04, 0.055, 0.09), rough=0.45, metallic=0.75)
    rim = mat_emit("RimCool", COOL, 6.0)
    core = mat_gradient_emit("CrackCore", [(0.0, WARM), (0.5, WARM_HOT), (1.0, WARM)], 10.0, axis="X")
    crys = mat_crystal("Crys")

    outer = chamfered_hex(1.18, 0.16)
    # Crack from the upper-left chamfer to the lower-right chamfer (opposite vertices of the 12-gon).
    a, b = 2, 8
    crack = jagged(outer[a], outer[b], 7, 0.13, seed=7)
    half1 = outer[a:b + 1] + list(reversed(crack))
    half2 = outer[b:] + outer[:a + 1] + crack
    d = np.array(outer[b]) - np.array(outer[a])
    nrm = np.array((-d[1], d[0])) / np.linalg.norm(d)
    gap = 0.065
    h1 = offset(half1, -nrm * gap)
    h2 = offset(half2, nrm * gap)
    obs.append(prism("PlateA", h1, -0.1, 0.12, plate, bevel=0.05))
    obs.append(prism("PlateB", h2, -0.1, 0.12, plate, bevel=0.05))
    # Raised inner faces: the same halves pulled in, a second tier with its own bevel.
    c1 = np.mean(np.array(h1), axis=0)
    c2 = np.mean(np.array(h2), axis=0)
    inlay = mat_emit("Inlay", COOL, 3.0)
    for tag, h, c in (("A", h1, c1), ("B", h2, c2)):
        inner = scale_pts(h, 0.8, c)
        obs.append(prism("Face" + tag, inner, 0.12, 0.2, face, bevel=0.025))
        obs.append(tube("Inlay" + tag, [(x, y, 0.215) for x, y in scale_pts(h, 0.68, c)], 0.011, inlay, cyclic=True))
    # Cool rim light behind the plate, peeking out round the edge.
    out_r = chamfered_hex(1.3, 0.16)
    in_r = chamfered_hex(1.21, 0.16)
    obs.append(ring_mesh("Rim", out_r, in_r, -0.15, rim))
    # The hot break: a slab under the gap, plus a glowing seam that follows the crack.
    obs.append(prism("CrackSlab", chamfered_hex(1.12, 0.16), -0.2, -0.16, core))
    # Crysium shards bursting out of the break, along the crack normal (up-right).
    up = nrm if nrm[1] > 0 else -nrm
    specs = [(0.3, 0.55, 0.09, 0.62), (0.4, 0.95, 0.12, 0.3), (0.5, 1.4, 0.15, 0.02), (0.6, 0.85, 0.11, -0.32),
             (0.7, 0.5, 0.08, -0.6)]
    for i, (t, ln, rad, tilt) in enumerate(specs):
        p = np.array(outer[a]) + d * t
        ca, sa = math.cos(tilt), math.sin(tilt)
        dirn = (up[0] * ca - up[1] * sa, up[0] * sa + up[1] * ca)
        obs.append(shard(f"Shard{i}", p - np.array(dirn) * 0.1, dirn, ln, rad, crys, z=0.32, twist=0.5 + 0.4 * i, flat=0.95,
                         sides=5))
    return obs


def emblem_shardline():
    obs = []
    frame = mat_metal("Frame", (0.06, 0.07, 0.09), rough=0.28)
    fill = mat_metal("Fill", (0.012, 0.015, 0.022), rough=0.6, metallic=0.3)
    line = mat_gradient_emit("Line", [(0.0, (0.05, 0.2, 0.9)), (0.5, COOL_WHITE), (1.0, (0.05, 0.2, 0.9))], 7.0,
                             axis="X")
    crys = mat_crystal("Crys")
    r = 1.2
    outer = [(0, r), (-r, 0), (0, -r), (r, 0)]
    outer = chamfer_poly(outer, 0.12)
    inner = chamfer_poly([(0, r - 0.2), (-(r - 0.2), 0), (0, -(r - 0.2)), (r - 0.2, 0)], 0.1)
    obs.append(prism("FrameOuter", outer, -0.1, 0.1, frame, bevel=0.04))
    glow_out = chamfer_poly([(0, r + 0.12), (-(r + 0.12), 0), (0, -(r + 0.12)), (r + 0.12, 0)], 0.13)
    glow_in = chamfer_poly([(0, r + 0.02), (-(r + 0.02), 0), (0, -(r + 0.02)), (r + 0.02, 0)], 0.12)
    obs.append(ring_mesh("FrameRim", glow_out, glow_in, -0.15, mat_emit("FrameRimMat", COOL, 4.0)))
    obs.append(prism("FrameFill", inner, 0.1, 0.12, fill))
    # The defence line: a light rail right across the emblem (and on into the wordmark, see SHARDLINE layout).
    ly = SHARDLINE_Y
    obs.append(prism("Line", [(-1.65, ly - 0.045), (1.65, ly - 0.045), (1.65, ly + 0.045), (-1.65, ly + 0.045)],
                     0.14, 0.2, line, bevel=0.02))
    # Emitter caps where the line meets the frame.
    cap = mat_metal("Cap", (0.5, 0.6, 0.75), rough=0.25)
    for sx in (-1, 1):
        x = sx * (r - 0.18 - abs(ly))
        obs.append(prism(f"Cap{sx}", [(x - 0.07, ly - 0.11), (x + 0.07, ly - 0.11), (x + 0.07, ly + 0.11),
                                      (x - 0.07, ly + 0.11)], 0.12, 0.26, cap, bevel=0.02))
    # Three shards rising from the bottom point through the line.
    for i, (bx, ln, rad, tilt) in enumerate(((0.0, 1.95, 0.22, 0.0), (-0.28, 1.1, 0.15, 0.42), (0.3, 1.25, 0.16, -0.36))):
        dirn = (-math.sin(tilt), math.cos(tilt))
        obs.append(shard(f"Shard{i}", (bx * 0.6, -0.95), dirn, ln, rad, crys, z=0.36 if i == 0 else 0.3, twist=0.4 * i))
    return obs


SHARDLINE_Y = -0.42


def chamfer_poly(pts, c):
    out = []
    n = len(pts)
    for i in range(n):
        a, b, d = np.array(pts[i - 1]), np.array(pts[i]), np.array(pts[(i + 1) % n])
        out.append(tuple(b + (a - b) / np.linalg.norm(a - b) * c))
        out.append(tuple(b + (d - b) / np.linalg.norm(d - b) * c))
    return out


def emblem_deepwake():
    obs = []
    planet = mat_metal("Planet", (0.012, 0.014, 0.024), rough=0.6, metallic=0.0)
    limb = mat_gradient_emit("Limb", [(0.0, (0.02, 0.1, 0.5)), (0.5, COOL_WHITE), (1.0, (0.02, 0.1, 0.5))], 5.0,
                             axis="X")
    orbit = mat_emit("Orbit", COOL, 4.0)
    eye = mat_gradient_emit("Eye", [(0.0, WARM_DEEP), (0.3, WARM), (0.5, (1.0, 0.85, 0.5)), (0.7, WARM), (1.0, WARM_DEEP)],
                            9.0, axis="X")
    lid = mat_metal("Lid", (0.12, 0.03, 0.02), rough=0.35, metallic=0.5)
    pupil = mat_metal("Pupil", (0.0, 0.0, 0.0), rough=0.4, metallic=0.0)
    vein = mat_emit("Vein", (1.0, 0.22, 0.04), 4.0)

    R = 1.1
    disc = [(R * math.cos(2 * math.pi * i / 96), R * math.sin(2 * math.pi * i / 96)) for i in range(96)]
    obs.append(prism("Planet", disc, -0.1, 0.1, planet, bevel=0.06))
    # Atmosphere limb along the upper rim.
    arc = [(R * 0.99 * math.cos(math.radians(a)), R * 0.99 * math.sin(math.radians(a)), 0.14) for a in range(22, 159, 3)]
    obs.append(tube("Limb", arc, 0.026, limb, taper=None))
    # The eye: an almond slit in the lower half, hot centre, dark slit pupil, a raised lid ridge.
    ey, ew, eh = -0.22, 0.78, 0.2
    alm = [(ew * math.cos(t), ey + eh * math.sin(t) * (1 - 0.25 * math.cos(t) ** 2))
           for t in np.linspace(0, 2 * math.pi, 64, endpoint=False)]
    lid_out = [(x * 1.12, ey + (y - ey) * 1.55) for x, y in alm]
    obs.append(ring_mesh("Lid", lid_out, alm, 0.14, lid))
    lid_ob = prism("LidRidge", lid_out, 0.1, 0.16, lid, bevel=0.03)
    obs.append(lid_ob)
    obs.append(prism("Eye", alm, 0.1, 0.17, eye))
    pup = [(0.07 * math.cos(t), ey + 0.17 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 32, endpoint=False)]
    obs.append(prism("Pupil", pup, 0.17, 0.19, pupil))
    # Veins: jagged glowing cracks from the eye corners and lids out towards the rim.
    rng = np.random.default_rng(11)
    for i, ang in enumerate((-170, -140, -110, -70, -40, -10, 200, 160, 20, -90)):
        a0 = math.radians(ang)
        start = (math.cos(a0) * ew * 0.9, ey + math.sin(a0) * eh * 1.4)
        ln = rng.uniform(0.35, 0.62)
        end = (start[0] + math.cos(a0) * ln, start[1] + math.sin(a0) * ln * 0.9)
        if math.hypot(*end) > R * 0.94:
            k = R * 0.94 / math.hypot(*end)
            end = (end[0] * k, end[1] * k)
        pts = [start] + jagged(start, end, 3, 0.022, seed=i) + [end]
        obs.append(tube(f"Vein{i}", [(x, y, 0.105) for x, y in pts], 0.013, vein, taper=(1.0, 0.15)))
    # Colony orbit: a tilted ellipse, the front half over the disc, the back half behind it.
    tilt = math.radians(-14)
    for half, z, rng_t in (("Front", 0.3, (math.pi, 2 * math.pi)), ("Back", -0.3, (0, math.pi))):
        pts = []
        for t in np.linspace(*rng_t, 60):
            x, y = 1.62 * math.cos(t), 0.36 * math.sin(t) - 0.02
            pts.append((x * math.cos(tilt) - y * math.sin(tilt), x * math.sin(tilt) + y * math.cos(tilt), z))
        obs.append(tube(f"Orbit{half}", pts, 0.022, orbit))
    # A small colony beacon riding the orbit (a cool point light).
    t = math.radians(305)
    x, y = 1.62 * math.cos(t), 0.36 * math.sin(t) - 0.02
    bx, by = x * math.cos(tilt) - y * math.sin(tilt), x * math.sin(tilt) + y * math.cos(tilt)
    obs.append(shard("Beacon", (bx, by - 0.08), (0, 1), 0.16, 0.06, mat_emit("BeaconMat", COOL_WHITE, 12.0), z=0.36,
                     sides=4, flat=1.0))
    return obs


# ------------------------------------------------------------------------------------------------------------
# Concepts
# ------------------------------------------------------------------------------------------------------------

CONCEPTS = {
    "veinbreak": dict(name="Veinbreak", word="VEINBREAK", tagline="THE PLANET FIGHTS BACK", emblem=emblem_veinbreak,
                      underline="crack", glow_at=(-0.3, 0.1), glow_col=WARM),
    "shardline": dict(name="Shardline", word="SHARDLINE", tagline="HOLD THE GATES", emblem=emblem_shardline,
                      underline="rail", glow_at=(0.0, 0.0), glow_col=WARM),
    "deepwake": dict(name="Deepwake", word="DEEPWAKE", tagline="SOMETHING BELOW IS AWAKE", emblem=emblem_deepwake,
                     underline=None, glow_at=(0.0, -0.3), glow_col=WARM),
}


def build(concept, header):
    C.reset_scene()
    scene = bpy.context.scene
    world = bpy.data.worlds.new("World")
    scene.world = world
    try:
        world.use_nodes = True
    except AttributeError:
        pass
    bgn = world.node_tree.nodes.get("Background")
    if bgn:
        bgn.inputs[0].default_value = (0.012, 0.016, 0.03, 1)
        bgn.inputs[1].default_value = 1.0

    font_b = bpy.data.fonts.load(FONT_BOLD, check_existing=True)
    font_m = bpy.data.fonts.load(FONT_MED, check_existing=True)
    emblem = concept["emblem"]()
    word_mat = mat_wordmark("Word")
    word = text_obj("Word", concept["word"], font_b, 1.42, word_mat, spacing=1.04)
    ex0, ey0, ex1, ey1 = union_bbox(emblem)
    wx0, wy0, wx1, wy1 = bbox(word)
    cap = wy1 - max(wy0, 0.0)
    gap = 0.45
    word.location.x = ex1 + gap - wx0
    word.location.y = -cap / 2 - 0.05
    if concept["underline"] == "rail":
        # The wordmark stands on the defence line, which runs on from the emblem.
        word.location.y = SHARDLINE_Y + 0.24
    objs = list(emblem) + [word]
    wx0, wy0, wx1, wy1 = bbox(word)

    if concept["underline"] == "rail":
        rail = mat_gradient_emit("Rail", [(0.0, (0.05, 0.2, 0.9)), (0.25, COOL_WHITE), (0.8, COOL),
                                          (1.0, (0.01, 0.04, 0.2))], 7.0, axis="X")
        y = SHARDLINE_Y
        objs.append(prism("Rail", [(1.65, y - 0.045), (wx1, y - 0.045), (wx1, y + 0.045), (1.65, y + 0.045)], 0.14,
                          0.2, rail, bevel=0.02))
    elif concept["underline"] == "crack":
        crack = mat_gradient_emit("UCrack", [(0.0, WARM_HOT), (0.6, WARM), (1.0, WARM_DEEP)], 7.0, axis="X")
        y = wy0 - 0.22
        pts = [(wx0 - 0.05, y)] + jagged((wx0 - 0.05, y), (wx1, y), 14, 0.035, seed=3) + [(wx1, y)]
        objs.append(tube("UCrack", [(x, yy, 0.05) for x, yy in pts], 0.026, crack, taper=(1.0, 0.2)))

    if header:
        tag_mat = mat_emit("Tag", (0.55, 0.68, 0.85), 1.2)
        tag = text_obj("Tag", concept["tagline"], font_m, 0.36, tag_mat, extrude=0.0, bevel=0.0, spacing=1.35)
        tx0, ty0, tx1, ty1 = bbox(tag)
        tag.location.x = wx1 - (tx1 - tx0) - tx0
        tag.location.y = wy0 - 0.75
        objs.append(tag)

    # Lights: a cool key from the upper left, a warm kicker from the lower right (the planet glows from below).
    for name, loc, col, power, size in (("Key", (-6, 6, 7), (0.75, 0.85, 1.0), 2500, 5),
                                        ("Warm", (6, -5, 2.5), (1.0, 0.45, 0.15), 1600, 4),
                                        ("Fill", (0, 0, 10), (0.6, 0.65, 0.8), 150, 12)):
        ld = bpy.data.lights.new(name, "AREA")
        ld.energy = power
        ld.color = col
        ld.size = size
        lo = C.link(bpy.data.objects.new(name, ld))
        lo.location = loc
        C.look_at(lo, Vector((wx0 * 0.4, 0, 0)))

    x0, y0, x1, y1 = union_bbox(objs)
    return (x0, y0, x1, y1)


def camera_for(bounds, size, margin_x, margin_y):
    x0, y0, x1, y1 = bounds
    w, h = size
    aspect = w / h
    bw, bh = (x1 - x0) * (1 + margin_x), (y1 - y0) * (1 + margin_y)
    scale = max(bw, bh * aspect)
    cd = bpy.data.cameras.new("Cam")
    cd.type = "ORTHO"
    cd.ortho_scale = scale
    cam = C.link(bpy.data.objects.new("Cam", cd))
    cam.location = ((x0 + x1) / 2, (y0 + y1) / 2, 20)
    cam.rotation_euler = (0, 0, 0)
    return cam, scale, scale / aspect, ((x0 + x1) / 2, (y0 + y1) / 2)


def render_rgba(cam, w, h, path, samples=64):
    scene = bpy.context.scene
    C.setup_eevee(w, h, samples)
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "OPEN_EXR"
    scene.render.image_settings.color_depth = "32"
    scene.render.image_settings.color_mode = "RGBA"
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    arr = C.image_to_array(path)
    os.remove(path)
    return arr


# ------------------------------------------------------------------------------------------------------------
# Background, bloom, tone map (numpy, linear light, row 0 = bottom)
# ------------------------------------------------------------------------------------------------------------

def background(w, h, view_w, view_h, centre, concept, header, seed=5):
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    X = centre[0] + (xs + 0.5) / w * view_w - view_w / 2
    Y = centre[1] + (ys + 0.5) / h * view_h - view_h / 2
    rgb = np.zeros((h, w, 3), np.float32) + np.array(BG, np.float32)
    # Cool haze from the upper left (the colony's sky), warm haze from below the emblem (the planet).
    t = (Y - Y.min()) / max(1e-6, Y.max() - Y.min())
    rgb += np.array((0.004, 0.009, 0.022))[None, None] * t[..., None]
    gx, gy = concept["glow_at"]
    d2 = (X - gx) ** 2 + (Y - gy) ** 2
    rgb += np.array(concept["glow_col"], np.float32) * (0.05 * np.exp(-d2 / 2.2))[..., None]
    if header:
        # A planet limb low across the frame: dark body, a thin cool atmosphere line, faint warm veins inside.
        pr = view_w * 1.25
        pcy = centre[1] - view_h / 2 - pr + view_h * 0.17
        pd = np.sqrt((X - centre[0]) ** 2 + (Y - pcy) ** 2)
        inside = pd < pr
        rgb[inside] = rgb[inside] * 0.35 + np.array((0.002, 0.003, 0.006))
        edge = np.exp(-((pd - pr) / (view_h * 0.006)) ** 2)
        halo = np.exp(-np.maximum(pd - pr, 0) / (view_h * 0.05)) * (pd >= pr)
        rgb += np.array((0.25, 0.55, 1.0)) * (edge * 0.6 + halo * 0.05)[..., None]
        # Veins on the planet: thin warm lines from a few seeds (sin field on warped coordinates).
        u = (X - centre[0]) * 3.0
        v = (pd - pr) * 9.0
        field = np.abs(np.sin(u * 1.7 + np.sin(v * 1.3 + u * 0.4) * 2.2 + v * 0.6))
        veins = np.clip(1 - field / 0.04, 0, 1) * inside * np.exp((pd - pr) / (view_h * 0.12))
        rgb += np.array(WARM) * (veins * 0.08)[..., None]
    # Vignette and fine grain so the dark gradient doesn't band.
    vx = (xs / w - 0.5) * 2
    vy = (ys / h - 0.5) * 2
    rgb *= (1 - 0.35 * np.clip(vx ** 2 * 0.6 + vy ** 2 * 0.8, 0, 1))[..., None]
    rng = np.random.default_rng(seed)
    rgb *= (1 + 0.04 * rng.standard_normal((h, w, 1))).astype(np.float32)
    return np.clip(rgb, 0, None)


def bloom(rgb, scale):
    bright = np.clip(rgb - 0.9, 0, None)
    glow = np.zeros_like(rgb)
    for radius, weight in ((2.0, 0.55), (7.0, 0.35), (22.0, 0.25), (60.0, 0.12)):
        for c in range(3):
            glow[..., c] += C._blur(bright[..., c], radius * scale) * weight
    return rgb + glow * 0.6


def tonemap(rgb):
    """Soft shoulder (keeps whites white, rolls hot emission off without hue clipping), then sRGB."""
    lum = np.max(rgb, axis=2, keepdims=True)
    mapped = lum * (1 + lum / 9.0) / (1 + lum)
    out = rgb * (mapped / np.maximum(lum, 1e-6))
    # Very hot cores drift towards white, like real glow.
    over = np.clip(lum - 4.0, 0, None) / 16.0
    out = out + (np.clip(mapped, 0, 1) - out) * np.clip(over, 0, 0.35)
    out = np.clip(out, 0, 1)
    return np.where(out <= 0.0031308, out * 12.92, 1.055 * np.power(out, 1 / 2.4) - 0.055)


def compose(layer, w, h, view, centre, concept, header, scale):
    bg = background(w, h, view[0], view[1], centre, concept, header)
    a = layer[..., 3:4]
    rgb = bg * (1 - a) + layer[..., :3]
    rgb = bloom(rgb, scale)
    return np.dstack([tonemap(rgb), np.ones((h, w, 1), np.float32)])


def downsample(img, f):
    h, w = img.shape[0] // f, img.shape[1] // f
    return img[:h * f, :w * f].reshape(h, f, w, f, 4).mean(axis=(1, 3))


def make(key):
    concept = CONCEPTS[key]
    paths = {}
    for header in (True, False):
        bounds = build(concept, header)
        if header:
            w, h = HEADER
            f = 1
            cam, vw, vh, centre = camera_for(bounds, (w, h), 0.2, 0.5)
        else:
            w, h = CAPSULE
            f = CAPSULE_SUPER
            cam, vw, vh, centre = camera_for(bounds, (w, h), 0.08, 0.22)
        tmp = os.path.join(OUT, f"_{key}_{'h' if header else 'c'}.exr")
        layer = render_rgba(cam, w * f, h * f, tmp)
        img = compose(layer, w * f, h * f, (vw, vh), centre, concept, header, (h * f) / 430.0)
        if f > 1:
            img = downsample(img, f)
        path = os.path.join(OUT, f"{concept['name']}_{'header' if header else 'capsule'}.png")
        C.array_to_image(img, path)
        C.log("logo", path)
        paths["header" if header else "capsule"] = path
    return paths


def contact_sheet(all_paths, path):
    """Per concept one row: the header at full size, and the capsule at 1x and nearest-neighbour 2x."""
    pad = 24
    hw, hh = HEADER
    cw, ch = CAPSULE
    col2 = cw * 2
    W = pad + hw + pad + col2 + pad
    rowh = hh
    H = pad + len(all_paths) * (rowh + pad)
    sheet = np.zeros((H, W, 4), np.float32)
    sheet[..., :3] = 0.035
    sheet[..., 3] = 1
    for r, paths in enumerate(reversed(all_paths)):   # row 0 is the bottom of a Blender image
        y = pad + r * (rowh + pad)
        head = C.image_to_array(paths["header"])
        sheet[y:y + hh, pad:pad + hw] = head
        cap = C.image_to_array(paths["capsule"])
        x = pad + hw + pad
        big = cap.repeat(2, axis=0).repeat(2, axis=1)
        sheet[y + hh - ch * 2:y + hh, x:x + col2] = big
        sheet[y + hh - ch * 2 - pad - ch:y + hh - ch * 2 - pad, x:x + cw] = cap
    C.array_to_image(sheet, path)
    C.log("sheet", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    only = C.cli_args().get("only")
    keys = [only] if only else list(CONCEPTS)
    all_paths = [make(k) for k in keys]
    if not only:
        contact_sheet(all_paths, os.path.join(OUT, "Logo_contact_sheet.png"))


if __name__ == "__main__":
    main()
