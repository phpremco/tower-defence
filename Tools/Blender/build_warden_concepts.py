"""Round 31: three Frost Warden body-plan concepts (blockouts, QA only, no export).

Remco on the round-30 Warden (2026-11-05): "ik snap de verhoudingen niet, ik weet niet waar ik naar kijk" (a humanoid
torso on six legs read as an 'ice scorpion'). These are quick blockouts with rough materials, one body plan each, to
let him pick a direction before a real model is built:

- A  Rime Colossus: a hunched knuckle-walker; two huge ice-gauntlet forearms are the weapon, a low wedge head with a
     breath maw sits between them.
- B  Crown Floe-beast: a broad, low glacier quadruped; a tilted ice crown-shield and two tusks are the ram, a glowing
     throat breathes the frost.
- C  Lantern Stilt-warden: a high carapace on three stilt legs (a tripod), one great glaive arm in front, an ice lantern
     (the cast) swinging under the belly.

Everything is metaball bodies (fused, smooth) plus convex-hull ice masses (hard facets), procedural materials, EEVEE.
Run: blender --background --factory-startup --python Tools/Blender/build_warden_concepts.py -- --preview <dir>
     [--only A,B,C] [--nosheet] [--save]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Euler, Vector  # noqa: E402

import common as C  # noqa: E402

# ---------------------------------------------------------------------------------------------------------------
# Materials (rough, procedural)
# ---------------------------------------------------------------------------------------------------------------

ICE_GLOW = (0.35, 0.85, 1.0)
AMBER = (1.0, 0.55, 0.12)
EYE = 3.0
_MATS = {}


def _nodes(name):
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except Exception:
        pass
    nt = mat.node_tree
    return mat, nt, nt.nodes.get("Principled BSDF")


def mat_body():
    """Slate blue-grey hide: light on top, dark underneath (the towers' rule, so the mass stands off the ground),
    mottled, with cavity darkening and a fine bump."""
    if "body" in _MATS:
        return _MATS["body"]
    mat, nt, bsdf = _nodes("Concept_Body")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])
    up = nt.nodes.new("ShaderNodeMapRange")
    up.inputs["From Min"].default_value = -0.6
    up.inputs["From Max"].default_value = 0.9
    nt.links.new(sep.outputs["Z"], up.inputs["Value"])
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 2.2
    noise.inputs["Detail"].default_value = 8
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.018, 0.024, 0.034, 1)
    ramp.color_ramp.elements[1].color = (0.15, 0.19, 0.25, 1)
    mix = nt.nodes.new("ShaderNodeMath")
    mix.operation = "MULTIPLY_ADD"
    nt.links.new(up.outputs[0], mix.inputs[0])
    mix.inputs[1].default_value = 0.75
    nt.links.new(noise.outputs["Fac"], mix.inputs[2])
    nt.links.new(mix.outputs[0], ramp.inputs["Fac"])
    ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
    ao.inputs["Distance"].default_value = 0.6
    nt.links.new(ramp.outputs["Color"], ao.inputs["Color"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 0.8
    nt.links.new(ramp.outputs["Color"], mul.inputs["A"])
    nt.links.new(ao.outputs["AO"], mul.inputs["B"])
    nt.links.new(mul.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.62
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 9
    vor.feature = "DISTANCE_TO_EDGE"
    nt.links.new(vor.outputs["Distance"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    _MATS["body"] = mat
    return mat


def mat_ice(name="Concept_Ice", bright=1.0, glow=0.9):
    """Grown ice: deep teal where you look straight in, pale frost on glancing faces and rims, thin cyan light in
    crack lines (voronoi edges), low roughness so the facets flash."""
    key = (name, bright, glow)
    if key in _MATS:
        return _MATS[key]
    mat, nt, bsdf = _nodes(name)
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.45
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.02 * bright, 0.1 * bright, 0.16 * bright, 1)
    ramp.color_ramp.elements[1].color = (0.55, 0.8, 0.9, 1)
    ramp.color_ramp.elements[1].position = 0.85
    nt.links.new(lw.outputs["Facing"], ramp.inputs["Fac"])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])
    frost = nt.nodes.new("ShaderNodeMapRange")
    frost.inputs["From Min"].default_value = 0.55
    frost.inputs["From Max"].default_value = 1.0
    frost.inputs["To Max"].default_value = 0.55
    nt.links.new(sep.outputs["Z"], frost.inputs["Value"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    nt.links.new(frost.outputs[0], mix.inputs["Factor"])
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    mix.inputs["B"].default_value = (0.78, 0.9, 0.97, 1)
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.12
    try:
        bsdf.inputs["Coat Weight"].default_value = 0.6
    except KeyError:
        pass
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.feature = "DISTANCE_TO_EDGE"
    vor.inputs["Scale"].default_value = 1.6
    vor.inputs["Randomness"].default_value = 1.0
    warp = nt.nodes.new("ShaderNodeTexNoise")
    warp.inputs["Scale"].default_value = 1.5
    warp.inputs["Detail"].default_value = 4
    wmix = nt.nodes.new("ShaderNodeMix")
    wmix.data_type = "VECTOR"
    wmix.inputs["Factor"].default_value = 0.45
    tc = nt.nodes.new("ShaderNodeTexCoord")
    nt.links.new(tc.outputs["Object"], wmix.inputs[4])
    nt.links.new(warp.outputs["Color"], wmix.inputs[5])
    nt.links.new(wmix.outputs[1], vor.inputs["Vector"])
    crack = nt.nodes.new("ShaderNodeMapRange")
    crack.inputs["From Min"].default_value = 0.0
    crack.inputs["From Max"].default_value = 0.014
    crack.inputs["To Min"].default_value = 1.0
    crack.inputs["To Max"].default_value = 0.0
    nt.links.new(vor.outputs["Distance"], crack.inputs["Value"])
    rim = nt.nodes.new("ShaderNodeMath")
    rim.operation = "MULTIPLY_ADD"
    nt.links.new(lw.outputs["Fresnel"], rim.inputs[0])
    rim.inputs[1].default_value = 0.35
    nt.links.new(crack.outputs[0], rim.inputs[2])
    bsdf.inputs["Emission Color"].default_value = (*ICE_GLOW, 1)
    em = nt.nodes.new("ShaderNodeMath")
    em.operation = "MULTIPLY"
    em.inputs[1].default_value = glow
    nt.links.new(rim.outputs[0], em.inputs[0])
    nt.links.new(em.outputs[0], bsdf.inputs["Emission Strength"])
    _MATS[key] = mat
    return mat


def mat_glow(color=ICE_GLOW, strength=7.0, name="Concept_Glow"):
    key = (name, color, strength)
    if key in _MATS:
        return _MATS[key]
    mat, nt, bsdf = _nodes(name)
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Strength"].default_value = strength
    _MATS[key] = mat
    return mat


def mat_breath():
    """A frost cone: emissive, fading out along its length (Generated Z: 0 at the source, 1 far) and at its rim."""
    if "breath" in _MATS:
        return _MATS["breath"]
    mat = bpy.data.materials.new("Concept_Breath")
    try:
        mat.use_nodes = True
    except Exception:
        pass
    try:
        mat.surface_render_method = "BLENDED"
    except AttributeError:
        mat.blend_method = "BLEND"
    nt = mat.node_tree
    for n in list(nt.nodes):
        if n.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(n)
    out = [n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"][0]
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    fade = nt.nodes.new("ShaderNodeMapRange")
    fade.inputs["To Min"].default_value = 0.85
    fade.inputs["To Max"].default_value = 0.0
    nt.links.new(sep.outputs["Z"], fade.inputs["Value"])
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 4.0
    nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    a = nt.nodes.new("ShaderNodeMath"); a.operation = "MULTIPLY"
    nt.links.new(fade.outputs[0], a.inputs[0]); nt.links.new(lw.outputs["Facing"], a.inputs[1])
    b = nt.nodes.new("ShaderNodeMath"); b.operation = "MULTIPLY"
    nt.links.new(a.outputs[0], b.inputs[0]); nt.links.new(noise.outputs["Fac"], b.inputs[1])
    c = nt.nodes.new("ShaderNodeMath"); c.operation = "MULTIPLY"; c.inputs[1].default_value = 1.6
    nt.links.new(b.outputs[0], c.inputs[0])
    emis = nt.nodes.new("ShaderNodeEmission")
    emis.inputs["Color"].default_value = (0.6, 0.92, 1.0, 1)
    emis.inputs["Strength"].default_value = 4.0
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(c.outputs[0], mix.inputs["Fac"])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    _MATS["breath"] = mat
    return mat


# ---------------------------------------------------------------------------------------------------------------
# Shape kit
# ---------------------------------------------------------------------------------------------------------------

class Concept:
    """Collects one concept's objects in its own collection."""

    def __init__(self, name):
        self.name = name
        self.col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(self.col)
        self.n = 0

    def add(self, obj):
        self.col.objects.link(obj)
        return obj

    def uid(self, tag):
        self.n += 1
        return f"{self.name}_{tag}_{self.n}"

    def show(self, on):
        for o in self.col.objects:
            o.hide_render = not on
            o.hide_viewport = not on


class Skin:
    """A creature body as one Skin-modifier graph: spine and limbs are vertex chains with exact radii, branches fuse
    at their joints, a subdivision rounds it and a fine noise displacement breaks the surface."""

    def __init__(self, cp, tag):
        self.cp = cp
        self.tag = tag
        self.co = []
        self.rad = []
        self.edges = []

    def node(self, co, r, link=None):
        self.co.append(Vector(co))
        self.rad.append(r if isinstance(r, tuple) else (r, r))
        i = len(self.co) - 1
        if link is not None:
            self.edges.append((link, i))
        return i

    def chain(self, pts, radii, link=None):
        out = []
        for p, r in zip(pts, radii):
            link = self.node(p, r, link)
            out.append(link)
        return out

    def finish(self, mat, disp=0.04, seed=0, levels=2):
        me = bpy.data.meshes.new(self.cp.uid(self.tag))
        me.from_pydata([tuple(c) for c in self.co], self.edges, [])
        o = self.cp.add(bpy.data.objects.new(me.name, me))
        sk = o.modifiers.new("Skin", "SKIN")
        sk.use_smooth_shade = True
        sk.branch_smoothing = 0.6
        for i, r in enumerate(self.rad):
            me.skin_vertices[0].data[i].radius = r
        me.skin_vertices[0].data[0].use_root = True
        sub = o.modifiers.new("Sub", "SUBSURF")
        sub.levels = levels
        sub.render_levels = levels
        me.materials.append(mat)
        if disp:
            tex = bpy.data.textures.new(me.name + "_n", "CLOUDS")
            tex.noise_scale = 0.35
            tex.noise_depth = 2
            m = o.modifiers.new("Disp", "DISPLACE")
            m.texture = tex
            m.strength = disp
            m.mid_level = 0.5
        return o


def ell(cp, centre, size, rot=(0, 0, 0), mat=None):
    """A smooth ellipsoid mass (half-axes `size`)."""
    me = bpy.data.meshes.new(cp.uid("Ell"))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=1.0)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(mat or mat_body())
    o = cp.add(bpy.data.objects.new(me.name, me))
    o.location = Vector(centre)
    o.scale = size
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    tex = bpy.data.textures.get("EllNoise") or bpy.data.textures.new("EllNoise", "CLOUDS")
    tex.noise_scale = 0.35
    m = o.modifiers.new("Disp", "DISPLACE")
    m.texture = tex
    m.texture_coords = "GLOBAL"
    m.strength = 0.04
    return o


def hull(cp, tag, pts, mat, smooth=False):
    bm = bmesh.new()
    vs = [bm.verts.new(Vector(p)) for p in pts]
    bmesh.ops.convex_hull(bm, input=vs)
    me = bpy.data.meshes.new(cp.uid(tag))
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    me.materials.append(mat)
    return cp.add(bpy.data.objects.new(me.name, me))


def ice_chunk(cp, centre, size, rot=(0, 0, 0), seed=0, n=16, mat=None, flat_bottom=False):
    """A hewn ice mass: the convex hull of random points in an ellipsoid (hard facets, no two alike)."""
    rng = np.random.default_rng(seed)
    d = rng.normal(size=(n, 3))
    d /= np.linalg.norm(d, axis=1, keepdims=True)
    d *= rng.uniform(0.78, 1.0, (n, 1))
    if flat_bottom:
        d[:, 2] = np.maximum(d[:, 2], -0.35)
    R = Euler([math.radians(a) for a in rot]).to_matrix()
    pts = [Vector(centre) + R @ Vector((p[0] * size[0], p[1] * size[1], p[2] * size[2])) for p in d]
    return hull(cp, "Ice", pts, mat or mat_ice())


def shard(cp, base, tip, r, seed=0, sides=5, mat=None, mid=0.35):
    """A crystal spike from `base` to `tip`: a jittered ring, a fatter ring a third of the way, a point."""
    rng = np.random.default_rng(seed)
    base, tip = Vector(base), Vector(tip)
    axis = (tip - base).normalized()
    q = Vector((0, 0, 1)).rotation_difference(axis)
    pts = [tip]
    for ring, (t, rr) in enumerate(((0.0, 0.85), (mid, 1.0))):
        c = base.lerp(tip, t)
        for i in range(sides):
            a = 2 * math.pi * (i + rng.uniform(-0.2, 0.2) + ring * 0.5) / sides
            pts.append(c + q @ Vector((math.cos(a), math.sin(a), 0)) * r * rr * rng.uniform(0.8, 1.1))
    return hull(cp, "Shard", pts, mat or mat_ice("Concept_IceBright", bright=1.6, glow=1.4))


def blade(cp, root, tip, edge_dir, width, thick, mat=None, seed=0, curve=0.0):
    """A faceted blade (two hull segments so it can curve): edge towards `edge_dir`, spine opposite."""
    root, tip = Vector(root), Vector(tip)
    e = Vector(edge_dir).normalized()
    ax = (tip - root)
    side = ax.normalized().cross(e).normalized()
    objs = []
    mid = root.lerp(tip, 0.5) + e * curve
    for a, b, w0, w1 in ((root, mid, 0.55, 1.0), (mid, tip, 1.0, 0.05)):
        pts = []
        for p, w in ((a, w0), (b, w1)):
            W = width * w
            pts += [p + e * W, p - e * W * 0.5 + side * thick, p - e * W * 0.5 - side * thick, p - e * W * 0.8]
        objs.append(hull(cp, "Blade", pts, mat or mat_ice("Concept_IceBright", bright=1.6, glow=1.4)))
    return objs


def glow_ball(cp, centre, r, color=ICE_GLOW, strength=7.0):
    me = bpy.data.meshes.new(cp.uid("Glow"))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=r)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(mat_glow(color, strength))
    o = cp.add(bpy.data.objects.new(me.name, me))
    o.location = Vector(centre)
    return o


def glow_slit(cp, centre, size, rot=(0, 0, 0), color=ICE_GLOW, strength=7.0):
    o = glow_ball(cp, centre, 1.0, color, strength)
    o.scale = size
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    return o


def breath(cp, src, direction, length, r0, r1):
    """The frost breath / cast: a fading cone from `src` along `direction`."""
    bm = bmesh.new()
    seg = 20
    rings = []
    for z, r in ((0.0, r0), (length * 0.5, (r0 + r1) * 0.55), (length, r1)):
        rings.append([bm.verts.new((math.cos(2 * math.pi * i / seg) * r, math.sin(2 * math.pi * i / seg) * r, z))
                      for i in range(seg)])
    for a, b in zip(rings, rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            bm.faces.new((a[i], a[j], b[j], b[i]))
    me = bpy.data.meshes.new(cp.uid("Breath"))
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(mat_breath())
    o = cp.add(bpy.data.objects.new(me.name, me))
    o.location = Vector(src)
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(Vector(direction).normalized())
    return o


# ---------------------------------------------------------------------------------------------------------------
# Concept A: Rime Colossus (hunched knuckle-walker, ice-gauntlet forearms)
# ---------------------------------------------------------------------------------------------------------------

def concept_a(pose):
    cp = Concept(f"A_{pose}")
    cast = pose == "cast"
    piv = Vector((0, 1.25, 1.8))
    rear = -20 if cast else 0               # the torso rears up about the hips for the cast

    def R(p):
        if not rear:
            return Vector(p)
        return Euler((math.radians(rear), 0, 0)).to_matrix() @ (Vector(p) - piv) + piv

    body = Skin(cp, "Body")
    # Spine from the hips forward: a great hunched mass, the head slung low in front of the shoulders.
    P, M, Ch, N, H, S = body.chain(
        [R(p) for p in ((0, 1.25, 1.8), (0, 0.45, 2.75), (0, -0.3, 3.0), (0, -0.95, 2.6), (0, -1.45, 2.35),
                        (0, -1.95, 2.2))],
        [1.0, 1.35, 1.3, 0.68, 0.62, 0.4])
    head = R((0, -1.55, 2.35))
    arm = {}
    for s in (1, -1):
        sh = R((1.25 * s, -0.35, 3.15))
        if cast:
            el = Vector((2.0 * s, -1.0, 3.6)); wr = Vector((1.5 * s, -1.85, 4.45))
        else:
            el = Vector((1.9 * s, -0.85, 2.0)); wr = Vector((1.7 * s, -1.55, 0.95))
        body.chain([sh, el, wr], [0.85, 0.55, 0.85], link=Ch)
        arm[s] = (sh, el, wr)
        body.chain([R((0.7 * s, 1.4, 1.55)), (1.0 * s, 0.95, 0.85), (0.95 * s, 1.6, 0.35), (0.95 * s, 1.25, 0.15)],
                   [0.65, 0.48, 0.32, 0.28], link=P)
    body.finish(mat_body(), disp=0.05, seed=1)

    ice = mat_ice()
    for s in (1, -1):
        sh, el, wr = arm[s]
        d = (wr - el).normalized()
        fist = wr + d * 0.4
        if not cast:
            fist.z = 0.7
        # The gauntlet: a huge faceted ice maul round each fist (the weapon), plates up the forearm.
        ice_chunk(cp, fist, (0.8, 0.85, 0.72), (0, 0, 15 * s), seed=10 + s, n=24, mat=ice)
        for t, sz, sd in ((0.3, (0.48, 0.42, 0.4), 20), (0.65, (0.55, 0.45, 0.45), 30)):
            ice_chunk(cp, el.lerp(wr, t) + Vector((0.3 * s, 0, 0.2)), sz, (0, 0, 0), seed=sd + s, n=14, mat=ice)
        for k, (dx, dz) in enumerate(((0.4, 0.3), (0.0, 0.5), (-0.35, 0.25))):
            base = fist + Vector((dx * s, -0.45, dz))
            tip = base + (Vector((0.15 * s, -0.8, 0.35))).normalized() * 0.65
            shard(cp, base, tip, 0.14, seed=40 + k + s)
        # Pauldron with a spike: armour.
        sp = sh + Vector((0.2 * s, 0.05, 0.65))
        ice_chunk(cp, sp, (0.85, 0.75, 0.38), (0, -15 * s, 20 * s), seed=60 + s, n=18, mat=ice)
        shard(cp, sp + Vector((0.35 * s, 0.1, 0.2)), sp + Vector((0.95 * s, 0.4, 1.0)), 0.17, seed=70 + s)
    # Back armour: two floes on the hump.
    ice_chunk(cp, R((0, 0.55, 3.75)), (0.95, 0.75, 0.35), (-25, 0, 0), seed=80, n=18, mat=ice)
    ice_chunk(cp, R((0, 1.35, 2.75)), (0.8, 0.55, 0.32), (-50, 0, 0), seed=81, n=16, mat=ice)
    # Rage icicles: a crest raking back from the hump ridge.
    for i, (y, h, x) in enumerate(((0.15, 1.15, 0.0), (0.6, 1.3, 0.38), (0.6, 1.3, -0.38), (1.05, 1.0, 0.6),
                                    (1.05, 1.0, -0.6))):
        base = R((x, y, 4.0 - (y - 0.15) * 0.5))
        shard(cp, base, base + Vector((x * 0.5, 0.5, h)), 0.15, seed=90 + i)
    # Head: an ice crest plate, three amber eyes, the cyan maw framed by four mandible plates.
    ice_chunk(cp, head + Vector((0, 0.1, 0.48)), (0.42, 0.75, 0.2), (-10, 0, 0), seed=100, n=14, mat=ice)
    for x in (-0.24, 0.0, 0.24):
        glow_ball(cp, head + Vector((x, -0.6 + abs(x) * 0.3, 0.22 + (0.05 if x == 0 else 0))), 0.06, AMBER, EYE)
    maw = head + Vector((0, -0.62, -0.2))
    glow_slit(cp, maw, (0.26, 0.1, 0.13))
    for s in (1, -1):
        for z in (0.1, -0.3):
            shard(cp, maw + Vector((0.2 * s, 0.15, z)), maw + Vector((0.3 * s, -0.45, z * 1.4)), 0.09,
                  seed=110 + int(z * 10) + s, mat=mat_body())
    for s in (1, -1):                     # gill slits down each flank charge the breath
        for i in range(3):
            glow_slit(cp, R((1.25 * s, 0.25 + i * 0.4, 2.6 - i * 0.2)), (0.04, 0.16, 0.06), strength=1.6)
    if cast:
        breath(cp, maw + Vector((0, -0.25, 0)), (0, -1, -0.3), 3.2, 0.25, 1.3)
    return cp, dict(height=4.6)


# ---------------------------------------------------------------------------------------------------------------
# Concept B: Crown Floe-beast (broad low glacier quadruped, crown-shield ram)
# ---------------------------------------------------------------------------------------------------------------

def concept_b(pose):
    cp = Concept(f"B_{pose}")
    cast = pose == "cast"
    body = Skin(cp, "Body")
    if cast:
        neck, head, snout = (0, -1.75, 2.25), (0, -2.3, 2.35), (0, -2.85, 2.35)
    else:
        neck, head, snout = (0, -1.7, 1.85), (0, -2.3, 1.55), (0, -2.85, 1.4)
    tt, tl, hips, mid, chest, nk, hd, sn = body.chain(
        [(0, 3.6, 0.6), (0, 2.7, 1.2), (0, 1.45, 1.85), (0, 0.3, 2.0), (0, -0.8, 2.15), neck, head, snout],
        [0.14, 0.42, 1.0, 1.1, 1.05, 0.6, 0.55, 0.4])
    for s in (1, -1):
        body.chain([(1.15 * s, -0.85, 1.95), (2.0 * s, -0.95, 1.25), (1.9 * s, -1.05, 0.3), (1.9 * s, -1.3, 0.16)],
                   [0.6, 0.5, 0.45, 0.35], link=chest)
        body.chain([(1.05 * s, 1.5, 1.75), (1.9 * s, 1.55, 1.15), (1.85 * s, 1.5, 0.3), (1.85 * s, 1.25, 0.15)],
                   [0.58, 0.48, 0.44, 0.34], link=hips)
    body.finish(mat_body(), disp=0.05, seed=2)
    ell(cp, (0, 0.3, 2.0), (1.25, 1.6, 0.62), (4, 0, 0))     # widens the glacier body into a slab
    hv = Vector(head)
    ice = mat_ice()
    # Back: overlapping glacier floes along the spine (the armour), tilted back like shingles.
    for i, (y, z, w) in enumerate(((-0.8, 2.75, 1.15), (0.25, 2.65, 1.3), (1.3, 2.4, 1.05))):
        ice_chunk(cp, (0, y, z), (w, 0.55, 0.3), (-14, 0, 0), seed=200 + i, n=18, mat=ice)
    for s in (1, -1):
        ice_chunk(cp, (1.35 * s, -0.8, 2.4), (0.55, 0.6, 0.35), (0, 25 * s, 0), seed=210 + s, n=16, mat=ice)
        ice_chunk(cp, (1.3 * s, 1.45, 2.25), (0.5, 0.55, 0.32), (0, 25 * s, 0), seed=215 + s, n=16, mat=ice)
    # Rage icicles: rows of spikes between the floes, raking back.
    for i, y in enumerate((-0.42, 0.42, 1.25)):
        for j, x in enumerate((-0.55, 0.0, 0.55)):
            h = 0.9 if x == 0 else 0.65
            base = Vector((x, y, 2.85 - (y + 0.42) * 0.2))
            shard(cp, base, base + Vector((x * 0.4, 0.4, h)), 0.12, seed=230 + i * 3 + j)
    # The crown-shield: a broad faceted ice wall on the head, leaning back, fringed with blades: the ram.
    tilt = -18 if not cast else -5
    crown = hv + Vector((0, 0.25, 0.95))
    ice_chunk(cp, crown, (1.4, 0.25, 0.95), (tilt, 0, 0), seed=250, n=26,
              mat=mat_ice("Concept_IceCrown", bright=1.3, glow=1.2))
    for k, a in enumerate((-62, -35, -12, 12, 35, 62)):
        r = math.radians(a)
        base = crown + Vector((math.sin(r) * 1.15, 0.1 + abs(math.sin(r)) * 0.15, math.cos(r) * 0.7))
        tip = base + Vector((math.sin(r) * 0.6, 0.35, math.cos(r) * 1.0))
        shard(cp, base, tip, 0.14, seed=260 + k)
    for s in (1, -1):                        # two low tusks forward from the jaw
        base = hv + Vector((0.4 * s, -0.45, -0.2))
        shard(cp, base, base + Vector((0.25 * s, -1.0, 0.25)), 0.13, seed=280 + s, sides=6)
    for x in (-0.42, -0.25, -0.09, 0.09, 0.25, 0.42):   # six amber eyes in a row under the crown
        glow_ball(cp, hv + Vector((x, -0.5 + abs(x) * 0.3, 0.2)), 0.05, AMBER, EYE)
    throat = hv + Vector((0, -0.62, -0.18))
    glow_slit(cp, throat, (0.36, 0.1, 0.09))
    glow_slit(cp, hv + Vector((0, 0.2, -0.45)), (0.3, 0.38, 0.08), strength=2)
    for s in (1, -1):                        # gill slots on the neck
        for i in range(3):
            glow_slit(cp, Vector(neck) + Vector((0.58 * s, 0.1 + i * 0.25, 0.0)), (0.04, 0.07, 0.16), strength=1.6)
    for s in (1, -1):                        # knee armour on the front legs
        ice_chunk(cp, (2.2 * s, -1.15, 1.3), (0.42, 0.42, 0.5), seed=300 + s, n=14, mat=ice)
    if cast:
        breath(cp, throat + Vector((0, -0.2, 0)), (0, -1, -0.38), 3.0, 0.3, 1.4)
    return cp, dict(height=3.6)


# ---------------------------------------------------------------------------------------------------------------
# Concept C: Lantern Stilt-warden (high carapace, three stilts, one glaive arm, hanging ice lantern)
# ---------------------------------------------------------------------------------------------------------------

def concept_c(pose):
    cp = Concept(f"C_{pose}")
    cast = pose == "cast"
    cz = 3.55
    body = Skin(cp, "Body")
    if cast:
        nk, hd, sn = (0, -1.2, cz + 0.2), (0, -1.75, cz + 0.1), (0, -2.4, cz - 0.15)
    else:
        nk, hd, sn = (0, -1.2, cz + 0.4), (0, -1.75, cz + 0.6), (0, -2.45, cz + 0.5)
    rear, mid, front, neck, head, snout = body.chain(
        [(0, 1.35, cz - 0.05), (0, 0.35, cz), (0, -0.55, cz + 0.1), nk, hd, sn], [0.5, 0.85, 0.65, 0.26, 0.3, 0.12])
    head = Vector(hd)
    legs = []
    # Three stilts (a tripod: two forward and out, one straight back), knees above the carapace.
    for s, link, hip, knee, foot in ((1, front, (0.6, -0.4, cz), (1.75, -1.15, cz + 1.1), (2.35, -2.0, 0.05)),
                                     (-1, front, (-0.6, -0.4, cz), (-1.75, -1.15, cz + 1.1), (-2.35, -2.0, 0.05)),
                                     (0, rear, (0, 1.3, cz), (0, 2.35, cz + 1.2), (0, 3.3, 0.05))):
        hip, knee, foot = Vector(hip), Vector(knee), Vector(foot)
        body.chain([hip, knee, foot], [0.32, 0.24, 0.12], link=link)
        legs.append((s, hip, knee, foot))
    sh = Vector((0, -0.85, cz - 0.45))
    if cast:
        el = Vector((0, -1.95, cz + 0.55)); wr = Vector((0, -2.5, cz + 1.55))
    else:
        el = Vector((0, -1.8, cz - 0.65)); wr = Vector((0, -2.2, cz - 1.5))
    body.chain([sh, el, wr], [0.28, 0.22, 0.17], link=front)
    lan = Vector((0, -0.9, 2.3)) if cast else Vector((0, 0.25, 1.85))
    body.chain([(0, 0.3, cz - 0.6), lan + Vector((0, 0, 0.5))], [0.15, 0.09], link=mid)
    body.finish(mat_body(), disp=0.03, seed=3)
    ell(cp, (0, 0.2, cz + 0.15), (1.0, 1.4, 0.62), (6, 0, 0))     # the carapace hood
    ice = mat_ice()
    bright = mat_ice("Concept_IceBright", bright=1.6, glow=1.4)
    # The glaive: a long curved ice blade off the single arm, edge forward.
    if cast:
        blade(cp, wr + Vector((0, 0.05, -0.2)), wr + Vector((0, -0.35, 1.8)), (0, -1, 0), 0.45, 0.08, curve=0.3)
    else:
        blade(cp, wr + Vector((0, 0.1, 0.25)), wr + Vector((0, -0.6, -1.85)), (0, -1, 0.15), 0.45, 0.08, curve=0.3)
    ice_chunk(cp, el.lerp(wr, 0.4), (0.3, 0.35, 0.4), seed=400, n=14, mat=ice)
    # Carapace armour: tilted floes; knee caps and greaves on the stilts.
    for i, (x, y, sz) in enumerate(((0, -0.35, (0.72, 0.55, 0.28)), (0, 0.55, (0.85, 0.65, 0.28)),
                                     (0.72, 0.2, (0.4, 0.75, 0.25)), (-0.72, 0.2, (0.4, 0.75, 0.25)))):
        ice_chunk(cp, (x, y, cz + 0.62 - abs(x) * 0.3), sz, (0, 22 if x > 0 else (-22 if x < 0 else 0), 0),
                  seed=410 + i, n=16, mat=ice)
    for s, hip, knee, foot in legs:
        ice_chunk(cp, knee + Vector((0, 0, 0.08)), (0.3, 0.3, 0.35), seed=420 + int(knee.y * 10) + s, n=14, mat=ice)
        ice_chunk(cp, knee.lerp(foot, 0.3), (0.2, 0.2, 0.55),
                  rot=(math.degrees(math.atan2(foot.y - knee.y, knee.z - foot.z)),
                       -math.degrees(math.atan2(foot.x - knee.x, knee.z - foot.z)), 0),
                  seed=430 + int(knee.y * 10) + s, n=12, mat=ice)
    # Rage icicles: hanging from the carapace rim (they grow down in rage); a swept crown on the head.
    for i in range(7):
        a = math.radians(-160 + i * 47)
        base = Vector((math.cos(a) * 0.8, 0.4 + math.sin(a) * 1.05, cz - 0.3))
        shard(cp, base, base + Vector((0, 0, -0.75 - 0.25 * (i % 2))), 0.1, seed=440 + i)
    for k, (x, l) in enumerate(((0, 0.95), (0.22, 0.7), (-0.22, 0.7))):
        base = head + Vector((x, 0.2, 0.22))
        shard(cp, base, base + Vector((x * 1.2, 0.75, l * 0.7)), 0.09, seed=460 + k)
    for x in (-0.14, 0.14):
        glow_ball(cp, head + Vector((x, -0.42, 0.12)), 0.05, AMBER, EYE)
    # The lantern: a cage of ice bars round a bright core (the cast flares from here).
    glow_ball(cp, lan, 0.3, ICE_GLOW, 12.0)
    for i in range(6):
        a = 2 * math.pi * i / 6
        o = Vector((math.cos(a) * 0.42, math.sin(a) * 0.42, 0))
        shard(cp, lan + o * 0.6 + Vector((0, 0, 0.55)), lan + o + Vector((0, 0, -0.55)), 0.07, seed=470 + i,
              mat=bright)
    ice_chunk(cp, lan + Vector((0, 0, 0.55)), (0.38, 0.38, 0.16), seed=480, n=12, mat=ice)
    if cast:
        breath(cp, lan + Vector((0, -0.35, 0)), (0, -1, -0.4), 3.0, 0.35, 1.5)
    return cp, dict(height=5.2)


CONCEPTS = {
    "A": (concept_a, "A  Rime Colossus", "hunched knuckle-walker: two huge ice-gauntlet fists are the weapon, a low "
          "wedge head breathes frost between them"),
    "B": (concept_b, "B  Crown Floe-beast", "broad low glacier quadruped: an ice crown-shield and tusks ram the gate, "
          "a glowing throat breathes frost, floes on the back"),
    "C": (concept_c, "C  Lantern Stilt-warden", "high carapace on a tripod of stilts: one great glaive arm hangs in front, "
          "an ice lantern under the belly casts the frost"),
}


# ---------------------------------------------------------------------------------------------------------------
# Scene props and renders
# ---------------------------------------------------------------------------------------------------------------

def import_fbx(path, keep_suffix=None):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type == "MESH" and keep_suffix and not o.name.endswith(keep_suffix):
            o.hide_render = True
            o.hide_viewport = True
    for o in new:
        if o.type == "MESH" and ("LOD" in o.name or "Overlay" in o.name or "Damage" in o.name):
            o.hide_render = True
    return [o for o in new if o.parent is None], new


def bounds(objs):
    lo = Vector((1e9, 1e9, 1e9)); hi = -lo
    for o in objs:
        if o.type != "MESH" or o.hide_render:
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    return lo, hi


def place(roots, loc, yaw=0.0):
    for o in roots:
        o.location = Vector(loc)
        o.rotation_euler.z += math.radians(yaw)


def concept_bounds(cp):
    return bounds(list(cp.col.objects))


def render_concept(key, rest, cast, props, out):
    gate_roots, tower_roots = props
    paths = []
    rest.show(True); cast.show(False)
    lo, hi = concept_bounds(rest)
    C.log(key, "size w/l/h", tuple(round(v, 2) for v in (hi - lo)))
    front = lo.y
    gate_y = front - 1.0
    place(gate_roots, (0, gate_y, 0))
    place(tower_roots, (3.6, gate_y + 0.6, 0))
    for o in gate_roots + tower_roots:
        o.hide_render = False
    lights = C.game_lights()
    # 1) three-quarter front at play zoom x1.5, 2) walking away (from behind), 3) the cast at the gate,
    # 4) true game scale (play zoom 1) facing the camera as on a road coming down the screen.
    shots = (("rest", -62, 1.1, 620, 440), ("rest", 192, 1.1, 620, 440), ("cast", -62, 1.1, 620, 440),
             ("rest", 12, 1.0, 620, 440))
    for i, (p, yaw, zoom, w, h) in enumerate(shots):
        rest.show(p == "rest"); cast.show(p == "cast")
        target = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2 - 0.6, 1.9))
        cam = C.game_camera(target, w / C.GAME_PIXELS_PER_UNIT / zoom, yaw=yaw)
        paths.append(C.render(cam, os.path.join(out, f"_{key}_game{i}.png"), w, h, samples=32))
        bpy.data.objects.remove(cam)
    game = os.path.join(out, f"Warden_{key}_game.png")
    C.contact_sheet(paths, game, 4, 620)
    for p in paths:
        os.remove(p)
    for o in lights:
        bpy.data.objects.remove(o)
    # Turntable: four views, no props, studio rig.
    for o in gate_roots + tower_roots:
        for c in [o] + list(o.children_recursive):
            c.hide_render = True
    rest.show(True); cast.show(False)
    rig = C.light_rig(scale=4.0)
    tpaths = []
    centre = Vector((0, (lo.y + hi.y) / 2, (hi.z) * 0.45))
    for i, (az, el) in enumerate(((-38, 16), (-90, 6), (180, 16), (0, 62))):
        cam = C.closeup_camera(centre, 15.5, azimuth=az, elevation=el, lens=55)
        tpaths.append(C.render(cam, os.path.join(out, f"_{key}_tt{i}.png"), 620, 440, samples=32))
        bpy.data.objects.remove(cam)
    tt = os.path.join(out, f"Warden_{key}_turntable.png")
    C.contact_sheet(tpaths, tt, 4, 620)
    for p in tpaths:
        os.remove(p)
    for o in rig:
        bpy.data.objects.remove(o)
    for o in gate_roots + tower_roots:
        for c in [o] + list(o.children_recursive):
            if c.type == "MESH" and "LOD" not in c.name and not c.hide_viewport:
                c.hide_render = False
    rest.show(False); cast.show(False)
    return game, tt


def label_image(text, sub, path, width=2530, height=96):
    """A label strip rendered from Blender text objects (no PIL in Blender's Python)."""
    hidden = [o for o in bpy.data.objects if not o.hide_render]
    for o in hidden:
        o.hide_render = True
    font = bpy.data.fonts.load("C:/Windows/Fonts/arialbd.ttf", check_existing=True)
    font2 = bpy.data.fonts.load("C:/Windows/Fonts/arial.ttf", check_existing=True)
    objs = []
    for t, f, size, y, col in ((text, font, 0.42, 0.12, (0.75, 0.92, 1.0)), (sub, font2, 0.26, -0.3, (0.8, 0.82, 0.86))):
        cu = bpy.data.curves.new("Label", "FONT")
        cu.body = t; cu.font = f; cu.size = size
        o = bpy.data.objects.new("Label", cu)
        bpy.context.scene.collection.objects.link(o)
        o.location = (-width / height * 0.5 + 0.15, y, 0)
        cu.materials.append(mat_glow(col, 1.0, name="Concept_Label" + str(size)))
        objs.append(o)
    cd = bpy.data.cameras.new("LabelCam"); cd.type = "ORTHO"; cd.ortho_scale = width / height
    cam = bpy.data.objects.new("LabelCam", cd)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = (0, 0, 5)
    world = bpy.context.scene.world
    old = world.node_tree.nodes["Background"].inputs["Strength"].default_value
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.0
    C.render(cam, path, width, height, bloom=False, samples=8)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = old
    for o in objs + [cam]:
        bpy.data.objects.remove(o)
    for o in hidden:
        o.hide_render = False
    return path


def stack(paths, out, width, background=(0.05, 0.05, 0.07)):
    """Stacks images top to bottom at a common width (nearest-neighbour)."""
    rows = []
    for p in paths:
        img = C.image_to_array(p)[::-1]             # top-down
        h, w = img.shape[:2]
        s = width / w
        nh = max(1, int(round(h * s)))
        ys = np.clip((np.arange(nh) / s).astype(int), 0, h - 1)
        xs = np.clip((np.arange(width) / s).astype(int), 0, w - 1)
        rows.append(img[ys][:, xs])
        gap = np.zeros((8, width, 4), np.float32); gap[..., :3] = background; gap[..., 3] = 1
        rows.append(gap)
    sheet = np.concatenate(rows, axis=0)[::-1]
    C.array_to_image(sheet, out)
    C.log("sheet", out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    only = str(args.get("only", "A,B,C")).split(",")
    C.reset_scene()
    C.preview_world()
    C.ground()
    gate_roots, gate_all = import_fbx(os.path.join(C.EXPORTS, "Gate", "Gate_G2.fbx"), keep_suffix="G2")
    tower_roots, tower_all = import_fbx(os.path.join(C.EXPORTS, "Tower_Gun", "Tower_Gun_T2.fbx"), keep_suffix="T2")
    glo, ghi = bounds(gate_all)
    tlo, thi = bounds(tower_all)
    C.log("gate size", tuple(round(v, 2) for v in (ghi - glo)), "tower size", tuple(round(v, 2) for v in (thi - tlo)))
    results = {}
    for key in only:
        fn = CONCEPTS[key][0]
        rest, info = fn("rest")
        cast, _ = fn("cast")
        rest.show(False); cast.show(False)
        results[key] = render_concept(key, rest, cast, (gate_roots, tower_roots), out)
    if not args.get("nosheet"):
        parts = []
        for key in ("A", "B", "C"):
            game = os.path.join(out, f"Warden_{key}_game.png")
            tt = os.path.join(out, f"Warden_{key}_turntable.png")
            if not (os.path.exists(game) and os.path.exists(tt)):
                continue
            _, title, line = CONCEPTS[key]
            parts += [label_image(title, line, os.path.join(out, f"_label_{key}.png")), game, tt]
        head = label_image("Frost Warden: three body plans",
                           "top rows: game camera (3/4 front, walking away, frost cast at a G2 gate, true play zoom) "
                           "- bottom rows: front 3/4, side, back, top", os.path.join(out, "_label_head.png"))
        stack([head] + parts, os.path.join(out, "warden_concepts_sheet.png"), 2530)
        for p in [head] + [p for p in parts if os.path.basename(p).startswith("_label")]:
            os.remove(p)
    if args.get("save"):
        C.save_blend("FrostWarden_Concepts")


if __name__ == "__main__":
    main()
