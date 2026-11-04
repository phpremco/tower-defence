"""Shared helpers for round 13: the story characters and the cutscene models (see ArtSource/DESIGN_NOTES.md).

Builds on the round-7/9 hard-surface kit (`tower_kit.py`, `base_kit.py`): the same pieces, atlas, self-bake and
skinned export, so the capsule, the drill and Pioneer-1 come out of the same workshop as the towers. Only additions
live here:

- extra looks (insulation foil, solar cells, the heat shield with its ember glow, mire mud, alien flora) and glow
  colours of the story speakers (ORA cyan, Pioneer mint, the NTI "nova" gold-white);
- extra shapes: partial tori (gyro rings), icospheres (ORA's core), partial lathes (a broken dish), a noisy mound,
  Crysium shards and clusters, fronds, tendrils and bulbs (alien growth on the Pioneer-1 wreck);
- the NTI emblem as kit pieces (used by the emblem asset and as a plate on Pioneer-1);
- second UV sets: glitch bands (ORA) and planar 0..1 screens (radar console);
- staging: the portrait camera (Mercer's bust framing), clip strips, a space backdrop with a planet, socket empties.

Coordinates as in tower_kit: Blender world, Z up, a model faces -Y (Unity +Z).
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

import base_kit as bk
import common as C
import tower_kit as tk
from base_kit import UP, V

GLOW = {
    "ora": (0.35, 0.85, 1.0),
    "ora_hot": (0.82, 0.97, 1.0),
    "pioneer": (0.55, 1.0, 0.8),
    "nova": (1.0, 0.9, 0.62),
    "base": (0.3, 0.7, 1.0),
    "engine": (0.55, 0.8, 1.0),
    "cyan": (0.35, 0.95, 1.0),
    "ember": (1.0, 0.42, 0.12),
    "crysium": (0.98, 0.5, 0.92),
    "crysium_violet": (0.68, 0.42, 1.0),
    "lime": (0.72, 1.0, 0.32),
    "pink": (1.0, 0.45, 0.75),
}


def glow(name, strength=1.0, lens=0.0):
    return tk.glow(GLOW.get(name, name) if isinstance(name, str) else name, strength, lens)


# Looks beyond tower_kit's (sRGB paint; families from tower_kit.FAMILIES).
FOIL = tk.Look((0.74, 0.55, 0.22), "metal", rough=0.26, panel=0.11, wear=0.35, grad=0.55)
SOLAR = tk.Look((0.07, 0.12, 0.27), "glass", rough=0.16, panel=0.075, wear=0.15, grad=0.4)
NAVY = tk.Look((0.1, 0.17, 0.33), "ceramic", rough=0.45, panel=0.0, wear=0.5)
SHIELD = tk.Look((0.075, 0.066, 0.062), "metal", rough=0.78, panel=0.12, wear=0.5, emit=GLOW["ember"], glow=0.22, grad=0.6)
MUD = tk.Look((0.12, 0.09, 0.095), "rubber", rough=0.62, wear=0.0, grad=0.9)
MIRE = tk.Look((0.03, 0.055, 0.06), "glass", rough=0.05, wear=0.0, grad=0.2)
PLUM = tk.Look((0.3, 0.13, 0.27), "rubber", rough=0.55, wear=0.15, grad=1.0)
MOSS = tk.Look((0.2, 0.17, 0.12), "rubber", rough=0.8, wear=0.0, grad=1.0)
CRYS = tk.Look((0.85, 0.45, 0.82), "glass", rough=0.12, wear=0.0, emit=GLOW["crysium"], glow=0.9, grad=0.5)
CRYS_VIOLET = tk.Look((0.5, 0.32, 0.85), "glass", rough=0.12, wear=0.0, emit=GLOW["crysium_violet"], glow=0.85, grad=0.5)
CRYS_TIP = tk.Look((0.85, 0.88, 1.0), "glass", rough=0.08, wear=0.0, emit=(0.85, 0.9, 1.0), glow=1.3, grad=0.4)


# -------------------------------------------------------------------------------------------------------------
# Shapes (bmesh recipes in local space, as tower_kit's)
# -------------------------------------------------------------------------------------------------------------

def bm_arc_torus(radius, tube, a0, a1, seg, sides):
    """A torus segment from angle a0 to a1 (radians) round local Z, with flat caps at both ends."""
    bm = bmesh.new()
    rings = []
    for i in range(seg + 1):
        a = a0 + (a1 - a0) * i / seg
        c, s = math.cos(a), math.sin(a)
        ring = []
        for j in range(sides):
            b = 2 * math.pi * j / sides + math.pi / sides
            r = radius + math.cos(b) * tube
            ring.append(bm.verts.new((c * r, s * r, math.sin(b) * tube)))
        rings.append(ring)
    for i in range(seg):
        for j in range(sides):
            j2 = (j + 1) % sides
            bm.faces.new((rings[i][j], rings[i + 1][j], rings[i + 1][j2], rings[i][j2]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    return bm


def bm_ico(radius, subdiv=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    return bm


def bm_lathe_arc(profile, sides, a0, a1):
    """tower_kit's lathe over the angle range a0..a1 only (a broken dish), capped on both cut sides."""
    bm = bmesh.new()
    pts = list(profile)
    poles = {i: bm.verts.new((0.0, 0.0, z)) for i, (r, z) in enumerate(pts) if r <= 1e-9}
    cols = []
    for s in range(sides + 1):
        a = a0 + (a1 - a0) * s / sides
        cols.append([poles[i] if i in poles else bm.verts.new((math.cos(a) * r, math.sin(a) * r, z))
                     for i, (r, z) in enumerate(pts)])
    n = len(pts)
    for s in range(sides):
        for i in range(n - 1):
            quad = [cols[s][i], cols[s + 1][i], cols[s + 1][i + 1], cols[s][i + 1]]
            uniq = []
            for v in quad:
                if v not in uniq:
                    uniq.append(v)
            if len(uniq) >= 3:
                bm.faces.new(uniq)
    for col, rev in ((cols[0], True), (cols[-1], False)):
        uniq = []
        for v in col:
            if v not in uniq:
                uniq.append(v)
        if len(uniq) >= 3:
            bm.faces.new(list(reversed(uniq)) if rev else uniq)
    return bm


def bm_mound(radius, height, seed=0, rings=7, sectors=22, rough=0.25):
    """A low irregular mound: a disc whose rim wobbles in and out and whose top rises in noisy terraces."""
    rng = np.random.default_rng(seed)
    wob = rng.uniform(-1, 1, sectors)
    wob = (wob + np.roll(wob, 1) + np.roll(wob, -1)) / 3
    bm = bmesh.new()
    centre = bm.verts.new((0, 0, height))
    grid = []
    for i in range(1, rings + 1):
        t = i / rings
        ring = []
        for s in range(sectors):
            a = 2 * math.pi * s / sectors
            r = radius * t * (1 + rough * wob[s] * t)
            z = height * (1 - t ** 2.2) * (1 + 0.25 * rng.uniform(-1, 1) * t)
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, max(0.0, z) if i < rings else -0.02)))
        grid.append(ring)
    for s in range(sectors):
        s2 = (s + 1) % sectors
        bm.faces.new((centre, grid[0][s], grid[0][s2]))
        for i in range(rings - 1):
            bm.faces.new((grid[i][s], grid[i + 1][s], grid[i + 1][s2], grid[i][s2]))
    bm.faces.new(list(reversed(grid[-1])))
    return bm


def bm_poly_prism(poly, z0, z1):
    """An outline in local XY extruded from z0 to z1 (tower_kit's plate, off-centre)."""
    bm = bmesh.new()
    lo = [bm.verts.new((x, y, z0)) for x, y in poly]
    hi = [bm.verts.new((x, y, z1)) for x, y in poly]
    bm.faces.new(list(reversed(lo)))
    bm.faces.new(hi)
    for i in range(len(poly)):
        i2 = (i + 1) % len(poly)
        bm.faces.new((lo[i], lo[i2], hi[i2], hi[i]))
    return bm


def add(k, name, recipe, m, look, bone, on=None, bevel=0.0, lod=True):
    """A piece from any bmesh recipe (tower_kit's Kit._add made public for the recipes above)."""
    return k._add(name, recipe, bevel, m, look, bone, on, lod)


def chamfered_hex(r, cut):
    """A hexagon (flat top and bottom) with its corners cut by `cut` (0..0.5 of a side): a rounded badge."""
    hexa = bk.hex_poly(r, flat=True)
    out = []
    for i in range(6):
        a, b, c = Vector(hexa[i - 1]), Vector(hexa[i]), Vector(hexa[(i + 1) % 6])
        out.append(tuple(b + (a - b) * cut))
        out.append(tuple(b + (c - b) * cut))
    return out


def four_star(outer, inner, lower=None):
    """A four-pointed star (rays up, right, down, left); `lower` lengthens the lower ray."""
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = outer if i % 2 == 0 else inner
        if i == 4 and lower:
            r = lower
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts


def arc_band(r0, r1, a0, a1, seg=16):
    """A flat annulus segment (outline) between radii r0 < r1 from angle a0 to a1 (radians)."""
    outer = [(math.cos(a0 + (a1 - a0) * i / seg) * r1, math.sin(a0 + (a1 - a0) * i / seg) * r1) for i in range(seg + 1)]
    inner = [(math.cos(a0 + (a1 - a0) * i / seg) * r0, math.sin(a0 + (a1 - a0) * i / seg) * r0) for i in range(seg, -1, -1)]
    return outer + inner


# -------------------------------------------------------------------------------------------------------------
# The NTI emblem (Nova Terra Industries): badge, horizon arc, rising four-point star, letters N T I
# -------------------------------------------------------------------------------------------------------------

def emblem(k, m, bone, on=None, scale=1.0, letters=True, prefix="Emblem", star_glow=1.6, rim=tk.GUNMETAL,
           field=tk.BLUE, mark=tk.WHITE):
    """Kit pieces of the emblem on the plane of frame `m` (local XY, facing local +Z), badge radius 0.3 * scale."""
    s = scale

    def at(x, y, z=0.0):
        return m @ tk.frame((x * s, y * s, z * s))
    k.plate(prefix + "Rim", chamfered_hex(0.3 * s, 0.18), 0.05 * s, at(0, 0, -0.005), rim.but(panel=0, bolts=False),
            bone, on, bevel=0.008 * s)
    k.plate(prefix + "Field", chamfered_hex(0.255 * s, 0.18), 0.05 * s, at(0, 0, 0.004), field.but(panel=0, bolts=False),
            bone, on, bevel=0.005 * s)
    # Horizon arc: a band of a large circle whose centre lies below the badge.
    k.plate(prefix + "Arc", arc_band(0.25 * s, 0.29 * s, math.radians(52), math.radians(128), seg=14), 0.012 * s,
            at(0, -0.33, 0.034), mark.but(panel=0, bolts=False), bone, on, bevel=0.0)
    k.plate(prefix + "Star", four_star(0.115 * s, 0.03 * s, lower=0.175 * s), 0.016 * s, at(0, 0.075, 0.036),
            glow("nova", star_glow), bone, on, bevel=0.0)
    if letters:
        k.block(prefix + "Bar", (0.25 * s, 0.07 * s, 0.014 * s), at(0, -0.15, 0.032), tk.DARK.but(panel=0), bone, on, bevel=0.0)
        h, w = 0.048 * s, 0.011 * s
        z = 0.042
        for x in (-0.085, -0.055):
            k.block(prefix + "N", (w, h, 0.01 * s), at(x, -0.15, z), mark.but(panel=0, bolts=False), bone, on, bevel=0.0)
        k.block(prefix + "N", (w, h * 1.15, 0.01 * s), at(-0.07, -0.15, z) @ tk.frame(rot=(0, 0, 32)), mark.but(panel=0, bolts=False),
                bone, on, bevel=0.0)
        k.block(prefix + "T", (0.04 * s, w, 0.01 * s), at(0, -0.15 + 0.0185, z), mark.but(panel=0, bolts=False), bone, on, bevel=0.0)
        k.block(prefix + "T", (w, h, 0.01 * s), at(0, -0.15, z), mark.but(panel=0, bolts=False), bone, on, bevel=0.0)
        k.block(prefix + "I", (w, h, 0.01 * s), at(0.07, -0.15, z), mark.but(panel=0, bolts=False), bone, on, bevel=0.0)


# -------------------------------------------------------------------------------------------------------------
# Alien growth (Pioneer-1 wreck)
# -------------------------------------------------------------------------------------------------------------

def crystal(k, name, base, direction, r, h, look, bone, on=None, seed=0, lod=True):
    """A hexagonal Crysium shard: a short flared foot, a long faceted body and a sharp point."""
    start = (seed * 0.83) % (math.pi / 3)
    m = tk.frame(base, z=direction)
    body = k.lathe(name, [(0, -0.02 * h), (r * 0.8, 0.0), (r, 0.12 * h), (r * 0.8, 0.7 * h), (0, 0.7 * h)], m, look, bone, on, sides=6,
                   bevel=0.0, start=start, lod=lod)
    # An icy tip: the Crysium look (violet root, pink body, white-hot point).
    k.lathe(name + "Tip", [(0, 0.7 * h), (r * 0.8, 0.7 * h), (0, h)], m, CRYS_TIP, bone, on, sides=6, bevel=0.0, start=start, lod=lod)
    return body


def cluster(k, name, base, normal, count, size, seed, look=CRYS, bone="Root", on=None, spread=0.55, tilt=38.0):
    """`count` shards from one root: the biggest along `normal`, the rest leaning out at up to `tilt` degrees."""
    rng = np.random.default_rng(seed)
    n = Vector(normal).normalized()
    side = n.orthogonal().normalized()
    out = []
    for i in range(count):
        if i == 0:
            d, scale, off = n, 1.0, Vector()
        else:
            a = rng.uniform(0, 2 * math.pi)
            axis = (side * math.cos(a) + n.cross(side) * math.sin(a)).normalized()
            d = (Matrix.Rotation(math.radians(rng.uniform(tilt * 0.4, tilt)), 3, axis) @ n).normalized()
            scale = rng.uniform(0.45, 0.8)
            off = (n.cross(axis)).normalized() * size * spread * rng.uniform(0.2, 0.6)
        r = size * 0.12 * scale
        h = size * scale
        lk = look if (i % 2) else (CRYS_VIOLET if look is CRYS else look)
        out.append(crystal(k, name, Vector(base) + off - d * h * 0.05, d, r, h, lk, bone, on, seed=seed * 7 + i))
    return out


def frond(k, name, base, direction, length, width, curl, look, tip, bone="Root", on=None, segs=4, side=None):
    """A leaf from flat trapezoid segments along a curve that bends by `curl` radians; a glowing tip at the end."""
    d = Vector(direction).normalized()
    s_axis = Vector(side) if side is not None else d.orthogonal()
    s_axis = (s_axis - d * s_axis.dot(d)).normalized()
    p = Vector(base)
    widths = [width * w for w in (0.55, 1.0, 0.85, 0.5, 0.22)]
    for i in range(segs):
        seg_d = (Matrix.Rotation(curl * i / segs, 3, s_axis) @ d).normalized()
        seg_len = length / segs
        w0, w1 = widths[min(i, 4)], widths[min(i + 1, 4)]
        poly = [(-w0 / 2, 0), (w0 / 2, 0), (w1 / 2, seg_len), (-w1 / 2, seg_len)]
        # Local x across the leaf, y along it, z its face normal.
        m = Matrix.Translation(p) @ Matrix((s_axis, seg_d, s_axis.cross(seg_d).normalized())).transposed().to_4x4()
        k.plate(name, poly, 0.012, m, look, bone, on, bevel=0.0)
        p = p + seg_d * seg_len
    k.lathe(name + "Tip", [(0, -0.03), (0.028, 0.0), (0.02, 0.03), (0, 0.05)], tk.frame(p, z=seg_d), tip, bone, on, sides=6, bevel=0.0)
    return p


def tendril(k, name, points, r0, r1, look, bone="Root", on=None, sides=5):
    pts = [Vector(p) for p in points]
    n = len(pts) - 1
    for i in range(n):
        ra = r0 + (r1 - r0) * i / n
        rb = r0 + (r1 - r0) * (i + 1) / n
        k.cyl(name, pts[i], pts[i + 1] + (pts[i + 1] - pts[i]).normalized() * ra * 0.5, ra, rb, look=look, bone=bone, on=on,
              sides=sides, bevel=0.0)


def bulb(k, name, centre, r, look, bone="Root", on=None):
    prof = [(0, -r), (r * 0.7, -r * 0.7), (r, 0), (r * 0.7, r * 0.7), (0, r)]
    k.lathe(name, prof, tk.frame(centre), look, bone, on, sides=8, bevel=0.0)


# -------------------------------------------------------------------------------------------------------------
# Second UV sets
# -------------------------------------------------------------------------------------------------------------

def uv_bands(obj, z0, z1, name="Bands"):
    """UV1 for a glitch shader: U = angle round the vertical axis (0..1, 0.5 faces the viewer at -Y),
    V = (z - z0) / (z1 - z0). Written per loop from the rest-pose vertex positions."""
    mesh = obj.data
    co = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    vi = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get("vertex_index", vi)
    p = co[vi]
    u = (np.arctan2(p[:, 0], -p[:, 1]) / (2 * math.pi) + 0.5) % 1.0
    v = np.clip((p[:, 2] - z0) / max(z1 - z0, 1e-6), 0, 1)
    layer = mesh.uv_layers.new(name=name)
    layer.data.foreach_set("uv", np.c_[u, v].astype(np.float32).ravel())
    mesh.uv_layers.active_index = 0
    for i, l in enumerate(mesh.uv_layers):
        l.active_render = i == 0
    return layer


def planar_uv(obj, centre, axis_u, axis_v, half_u, half_v):
    """Replaces UV0 with a planar 0..1 map over a rectangle (screens): u along axis_u, v along axis_v."""
    mesh = obj.data
    co = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3) - np.asarray(centre, dtype=np.float32)
    vi = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get("vertex_index", vi)
    p = co[vi]
    u = np.clip(p @ np.asarray(axis_u) / (2 * half_u) + 0.5, 0, 1)
    v = np.clip(p @ np.asarray(axis_v) / (2 * half_v) + 0.5, 0, 1)
    mesh.uv_layers[0].data.foreach_set("uv", np.c_[u, v].astype(np.float32).ravel())


# -------------------------------------------------------------------------------------------------------------
# Objects, export, staging
# -------------------------------------------------------------------------------------------------------------

def socket_empties(sockets, parent=None, label="", size=0.05):
    """Empties for sockets {name: position or (position, direction)}; a direction turns the empty's +Z along it."""
    out = []
    for n, spec in sockets.items():
        e = C.link(bpy.data.objects.new(n + label, None))
        e.empty_display_size = size
        if isinstance(spec, tuple) and len(spec) == 2 and hasattr(spec[0], "__len__"):
            pos, d = Vector(spec[0]), Vector(spec[1])
            e.matrix_world = tk.frame(pos, z=d)
        else:
            e.location = Vector(spec)
        e.parent = parent
        out.append(e)
    return out


def static_object(kit, variant, mat, name, lod=False):
    return bk.module_object(kit.of(variant), name, V(0, 0, 0), mat, lod=lod)


def portrait_camera(target, distance=1.45, azimuth=-28.0, elevation=5.0, lens=70.0):
    """Mercer's bust framing (`mercer_bust.png`): 3/4 from the figure's right, a little below eye level, 70 mm."""
    return C.closeup_camera(Vector(target), distance, azimuth=azimuth, elevation=elevation, lens=lens)


def portrait_lights(scale=1.0, rim=(0.55, 0.75, 1.0)):
    return C.light_rig(scale, rim_color=rim)


def strip(arm, action, cam, frames, out_path, cell=420, width=480, height=480, columns=None):
    """Renders `frames` of `action` with `cam` and lays them out in one row (or `columns`)."""
    arm.animation_data.action = action
    tmp = []
    for f in frames:
        bpy.context.scene.frame_set(f)
        p = os.path.join(os.path.dirname(out_path), f"_strip_{len(tmp)}.png")
        C.render(cam, p, width, height, bloom=True, samples=24)
        tmp.append(p)
    C.contact_sheet(tmp, out_path, columns or len(tmp), cell)
    for p in tmp:
        os.remove(p)
    bpy.context.scene.frame_set(0)
    return out_path


def space_world(strength=1.0):
    """A black sky with fine stars (a world shader), for the orbit shots."""
    world = bpy.data.worlds.new("Space")
    bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    bg = nt.nodes.get("Background")
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 420.0
    coord = nt.nodes.new("ShaderNodeTexCoord")
    nt.links.new(coord.outputs["Generated"], vor.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (1, 1, 1, 1)
    ramp.color_ramp.elements[1].position = 0.035
    ramp.color_ramp.elements[1].color = (0.004, 0.005, 0.009, 1)
    nt.links.new(vor.outputs["Distance"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = strength
    return world


def planet(centre, radius, name="Planet"):
    """S-69-CRYS as a backdrop: a purple-blue sphere with a lit atmosphere rim (not an export)."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=96, ring_count=48, radius=radius, location=centre)
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.shade_smooth()
    mat = C.new_material(name + "Mat")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 2.2
    noise.inputs["Detail"].default_value = 8.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.05, 0.05, 0.16, 1)
    ramp.color_ramp.elements[1].color = (0.2, 0.12, 0.32, 1)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.7
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.25
    em = nt.nodes.new("ShaderNodeMath")
    em.operation = "POWER"
    em.inputs[1].default_value = 3.0
    nt.links.new(lw.outputs["Facing"], em.inputs[0])
    bsdf.inputs["Emission Color"].default_value = (0.35, 0.55, 1.0, 1)
    nt.links.new(em.outputs[0], bsdf.inputs["Emission Strength"])
    obj.data.materials.append(mat)
    return obj


def field_ground(color=(0.05, 0.05, 0.075)):
    return C.ground(color=color)


def report(path, data):
    tk.write_json(path, data)


def tris(obj):
    return C.tri_count(obj)


def lifted(objs, z):
    for o in objs:
        o.location.z += z
