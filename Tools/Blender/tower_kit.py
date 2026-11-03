"""Hard-surface kit for our own side's machines (round 7: the towers; reusable for gates, drones and the base).

The creature pipeline (common.py) fuses organic parts with a voxel remesh and bakes a dense source onto a decimated
game mesh. Machines need crisp chamfered edges instead, so this kit works the other way round:

1. **Pieces** (`Kit.block`, `lathe`, `cyl`, `torus`, `plate`): chamfered blocks, lathed bodies, tori and extruded
   plates written straight at game resolution. Each piece is rigid on one bone and tagged with the variants (tiers)
   it belongs to, and keeps a recipe so the LOD1 can rebuild it without chamfers.
2. **One atlas per machine:** the union of the pieces of every variant is unwrapped once, so all tiers share one
   texture set (a piece used by several tiers has one island).
3. **Self-bake** (`bake`): a Cycles shader on the game mesh itself paints, per pixel, what a high-poly sculpt would
   give: rounded edges (Bevel node) with chipped paint, panel seams and bolt rows in each piece's own frame, hazard
   stripes, ring grooves, scratches, grime from ambient occlusion, rain streaks and dust near the ground. For the
   bake each variant's pieces are moved apart, so one tier's occlusion never darkens another tier's pieces that
   occupy the same space.
4. **Skinned export** (`build_variant`, `export_fbx`): every piece weighted 1.0 to its bone, LOD0 and LOD1 skinned to
   the same armature in one FBX, actions keyed per bone.

Coordinates: Blender world, Z up, the machine faces -Y (Unity +Z). The fixed game camera looks from +Y (Unity -Z),
44 degrees down and 16 degrees to the side (`game_camera`), so camera-facing details (the rank plate with the stars)
sit on the +Y side.
"""

import json
import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

import common as C
import detailmaps
import surface

# -------------------------------------------------------------------------------------------------------------
# Palette (sRGB, as in a paint program; converted to linear for the bake)
# -------------------------------------------------------------------------------------------------------------

PAL = {
    "white": (0.86, 0.88, 0.9),
    "blue": (0.2, 0.36, 0.62),
    "steel": (0.42, 0.47, 0.54),
    "gunmetal": (0.2, 0.215, 0.24),
    "dark": (0.075, 0.08, 0.09),
    "rubber": (0.06, 0.065, 0.07),
    "yellow": (0.98, 0.74, 0.14),
    "glass": (0.03, 0.05, 0.07),
    "burnt": (0.09, 0.085, 0.08),
}
GLOW = {
    "azure": (0.32, 0.66, 1.0),
    "amber": (1.0, 0.78, 0.3),
    "cyan": (0.35, 0.95, 1.0),
    "ice": (0.82, 0.9, 1.0),
    "gate": (0.55, 0.75, 1.0),
    "star": (1.0, 0.88, 0.35),
    "ember": (1.0, 0.42, 0.12),
}
FAMILIES = ("ceramic", "metal", "rubber", "glass")
# Mask B = metal (Detail_Metal_Normal, made here), A = ceramic (the shared Detail_Ceramic_Normal).
DETAIL_FAMILIES = ("metal", "ceramic")
METAL_TILE_WORLD = 0.05
SHARP_DEGREES = 35.0
BEVEL_EDGE_DEGREES = 40.0


def srgb(c):
    c = np.asarray(c, dtype=float)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


class Look:
    """What a piece is made of and how it is painted. `panel`: seam spacing in the piece's frame (0 = none);
    `bolts`: bolt heads on the seam corners; `stripes`: hazard stripes; `rings`: ring grooves along local z;
    `wear`: chipped edges and scratches; `grad`: strength of the light-top / dark-underside value ramp;
    `lens`: radius of a radial glow (a lens is hottest in its centre)."""

    def __init__(self, paint, fam="ceramic", rough=0.5, panel=0.0, bolts=False, stripes=False, rings=0.0, wear=0.6,
                 emit=None, glow=0.0, lens=0.0, grad=1.0):
        self.paint = paint
        self.fam = fam
        self.rough = rough
        self.panel = panel
        self.bolts = bolts
        self.stripes = stripes
        self.rings = rings
        self.wear = wear
        self.emit = emit
        self.glow = glow
        self.lens = lens
        self.grad = grad

    def but(self, **kw):
        out = Look(self.paint)
        out.__dict__.update(self.__dict__)
        out.__dict__.update(kw)
        return out


WHITE = Look(PAL["white"], "ceramic", rough=0.42, panel=0.1, bolts=True, wear=0.9)
BLUE = Look(PAL["blue"], "ceramic", rough=0.48, panel=0.1, bolts=True, wear=0.8)
STEEL = Look(PAL["steel"], "metal", rough=0.38, wear=0.7, grad=0.8)
GUNMETAL = Look(PAL["gunmetal"], "metal", rough=0.42, wear=0.8, grad=0.7)
DARK = Look(PAL["dark"], "metal", rough=0.55, wear=0.4, grad=0.6)
RUBBER = Look(PAL["rubber"], "rubber", rough=0.85, wear=0.1, grad=0.5)
HAZARD = Look(PAL["yellow"], "ceramic", rough=0.5, stripes=True, wear=1.0)
YELLOW = Look(PAL["yellow"], "ceramic", rough=0.45, wear=0.9)
GLASS = Look(PAL["glass"], "glass", rough=0.06, wear=0.0, grad=0.3)


def glow(color, strength=1.0, lens=0.0):
    c = np.asarray(GLOW.get(color, color) if isinstance(color, str) else color, dtype=float)
    return Look(tuple(c * 0.55), "glow", rough=0.3, wear=0.0, emit=tuple(c), glow=strength, lens=lens, grad=0.3)


# -------------------------------------------------------------------------------------------------------------
# Pieces
# -------------------------------------------------------------------------------------------------------------

def frame(origin=(0, 0, 0), z=None, y=None, rot=None):
    """A rigid local-to-world matrix: origin, and either an Euler rotation `rot` (degrees) or a local +Z axis `z`
    (with `y` as a hint for local +Y)."""
    o = Vector(origin)
    if z is not None:
        zz = Vector(z).normalized()
        hint = Vector(y) if y is not None else (Vector((0, 0, 1)) if abs(zz.z) < 0.95 else Vector((0, 1, 0)))
        xx = hint.cross(zz).normalized()
        yy = zz.cross(xx)
        m = Matrix((xx, yy, zz)).transposed()
    elif rot is not None:
        from mathutils import Euler
        m = Euler([math.radians(a) for a in rot], "XYZ").to_matrix()
    else:
        m = Matrix.Identity(3)
    return Matrix.Translation(o) @ m.to_4x4()


def _bm_box(sx, sy, sz, taper=(1.0, 1.0), shift=(0.0, 0.0)):
    bm = bmesh.new()
    v = []
    for zi, z in enumerate((-sz / 2, sz / 2)):
        tx, ty = (1.0, 1.0) if zi == 0 else taper
        dx, dy = (0.0, 0.0) if zi == 0 else shift
        for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            v.append(bm.verts.new((x * sx / 2 * tx + dx, y * sy / 2 * ty + dy, z)))
    for f in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
        bm.faces.new([v[i] for i in f])
    return bm


def _bm_lathe(profile, sides, start=0.0):
    """Revolve (r, z) around local Z. An end point with r = 0 closes that end: with an n-gon when it lies in the
    plane of the next ring, otherwise with a cone of triangles to a pole (domes, dishes, lens caps)."""
    bm = bmesh.new()
    pts = list(profile)
    rings = []
    for r, z in pts:
        if r <= 1e-9:
            rings.append(None)
            continue
        rings.append([bm.verts.new((math.cos(start + 2 * math.pi * s / sides) * r,
                                    math.sin(start + 2 * math.pi * s / sides) * r, z)) for s in range(sides)])
    body = [i for i, ring in enumerate(rings) if ring is not None]
    for i0, i1 in zip(body, body[1:]):
        a, b = rings[i0], rings[i1]
        for s in range(sides):
            s2 = (s + 1) % sides
            bm.faces.new((a[s], a[s2], b[s2], b[s]))
    for end, nxt in ((0, body[0]), (len(pts) - 1, body[-1])):
        if rings[end] is not None:
            continue
        ring = rings[nxt]
        if abs(pts[end][1] - pts[nxt][1]) < 1e-7:
            bm.faces.new(list(reversed(ring)) if end == 0 else ring)
        else:
            pole = bm.verts.new((0.0, 0.0, pts[end][1]))
            for s in range(sides):
                s2 = (s + 1) % sides
                bm.faces.new((ring[s2], ring[s], pole) if end == 0 else (ring[s], ring[s2], pole))
    return bm


def _bm_torus(radius, tube, seg, sides):
    bm = bmesh.new()
    grid = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        c, s = math.cos(a), math.sin(a)
        ring = []
        for j in range(sides):
            b = 2 * math.pi * j / sides
            r = radius + math.cos(b) * tube
            ring.append(bm.verts.new((c * r, s * r, math.sin(b) * tube)))
        grid.append(ring)
    for i in range(seg):
        i2 = (i + 1) % seg
        for j in range(sides):
            j2 = (j + 1) % sides
            bm.faces.new((grid[i][j], grid[i2][j], grid[i2][j2], grid[i][j2]))
    return bm


def _bm_plate(poly, thickness):
    """An outline in local XY (counter-clockwise) extruded over local Z (-t/2 .. t/2)."""
    bm = bmesh.new()
    bottom = [bm.verts.new((x, y, -thickness / 2)) for x, y in poly]
    top = [bm.verts.new((x, y, thickness / 2)) for x, y in poly]
    bm.faces.new(list(reversed(bottom)))
    bm.faces.new(top)
    n = len(poly)
    for i in range(n):
        i2 = (i + 1) % n
        bm.faces.new((bottom[i], bottom[i2], top[i2], top[i]))
    return bm


def _finish(bm, bevel):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    if bevel > 0:
        edges = [e for e in bm.edges if e.is_manifold and e.calc_face_angle(0.0) > math.radians(BEVEL_EDGE_DEGREES)]
        if edges:
            bmesh.ops.bevel(bm, geom=edges, offset=bevel, offset_type="OFFSET", segments=1, profile=0.5,
                            affect="EDGES", clamp_overlap=True)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    verts = np.array([tuple(v.co) for v in bm.verts], dtype=np.float64)
    bm.verts.index_update()
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return verts, faces


class Piece:
    def __init__(self, name, recipe, bevel, matrix, look, bone, variants, lod=True):
        self.name = name
        self.recipe = recipe          # () -> bmesh in local space
        self.bevel = bevel
        self.matrix = matrix
        self.look = look
        self.bone = bone
        self.variants = set(variants)
        self.lod = lod
        self.local, self.faces = _finish(recipe(), bevel)
        self.uv = None                # per face corner, after the atlas unwrap

    def world(self, local=None):
        local = self.local if local is None else local
        m = np.array(self.matrix)
        return np.c_[local, np.ones(len(local))] @ m.T[:, :3]

    def axes(self):
        m = np.array(self.matrix)[:3, :3]
        return m[:, 0], m[:, 1], m[:, 2]

    def loops(self):
        return sum(len(f) for f in self.faces)


class Kit:
    """All pieces of one machine (every variant). `variants` is the ordered list of variant names."""

    def __init__(self, name, variants):
        self.name = name
        self.variants = list(variants)
        self.pieces = []

    # Primitive helpers; `on` is the variant list (default all), `bone` the rigid bone.
    def _add(self, name, recipe, bevel, m, look, bone, on, lod=True):
        p = Piece(name, recipe, bevel, m, look, bone, on if on is not None else self.variants, lod)
        self.pieces.append(p)
        return p

    def block(self, name, size, m, look, bone, on=None, taper=(1, 1), shift=(0, 0), bevel=0.008, lod=True):
        sx, sy, sz = size
        return self._add(name, lambda: _bm_box(sx, sy, sz, taper, shift), bevel, m, look, bone, on, lod)

    def lathe(self, name, profile, m, look, bone, on=None, sides=16, bevel=0.006, start=None, lod=True):
        st = math.pi / sides if start is None else start
        return self._add(name, lambda: _bm_lathe(profile, sides, st), bevel, m, look, bone, on, lod)

    def cyl(self, name, p0, p1, r0, r1=None, look=None, bone=None, on=None, sides=14, bevel=0.005, lod=True):
        p0, p1 = Vector(p0), Vector(p1)
        h = (p1 - p0).length
        r1 = r0 if r1 is None else r1
        m = frame(p0, z=p1 - p0)
        return self.lathe(name, [(0, 0), (r0, 0), (r1, h), (0, h)], m, look, bone, on, sides, bevel, lod=lod)

    def barrel(self, name, p0, p1, r, bore, look, bone, on=None, sides=14, depth=0.03, lod=True):
        """A gun tube from p0 (breech) to p1 (muzzle) with a dark bore sunk into the muzzle face."""
        p0, p1 = Vector(p0), Vector(p1)
        h = (p1 - p0).length
        m = frame(p0, z=p1 - p0)
        prof = [(0, 0), (r, 0), (r, h), (bore, h), (bore, h - depth), (0, h - depth)]
        return self.lathe(name, prof, m, look, bone, on, sides, 0.004, lod=lod)

    def torus(self, name, centre, axis, radius, tube, look, bone, on=None, seg=20, sides=6, lod=True):
        m = frame(centre, z=axis)
        return self._add(name, lambda: _bm_torus(radius, tube, seg, sides), 0.0, m, look, bone, on, lod)

    def plate(self, name, poly, thickness, m, look, bone, on=None, bevel=0.004, lod=True):
        return self._add(name, lambda: _bm_plate(poly, thickness), bevel, m, look, bone, on, lod)

    def of(self, variant):
        return [p for p in self.pieces if variant in p.variants]

    def home(self, piece):
        return next(v for v in self.variants if v in piece.variants)


def star_poly(outer, inner, points=5):
    pts = []
    for i in range(points * 2):
        a = math.pi / 2 + math.pi * i / points
        r = outer if i % 2 == 0 else inner
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts


# -------------------------------------------------------------------------------------------------------------
# Mesh assembly
# -------------------------------------------------------------------------------------------------------------

def assemble(pieces, name, offsets=None, lod=False, with_uv=True, attributes=False):
    """One mesh object from `pieces` (optionally translated per piece by offsets[piece]). `lod`: rebuild each
    piece without chamfers, drop pieces flagged lod=False and map UVs onto the LOD0 islands."""
    verts, faces, uvs, groups = [], [], [], []
    piece_ranges = []
    base = 0
    for p in pieces:
        if lod and not p.lod:
            continue
        off = np.array(offsets[p] if offsets else (0, 0, 0), dtype=float)
        if lod and p.bevel > 0:
            local, pf = _finish(p.recipe(), 0.0)
            uv = _lod_uvs(p, local, pf) if with_uv and p.uv is not None else None
        else:
            local, pf, uv = p.local, p.faces, p.uv
        w = p.world(local) + off
        verts.extend(map(tuple, w))
        faces.extend(tuple(i + base for i in f) for f in pf)
        if with_uv:
            uvs.extend(uv if uv is not None else [(0.0, 0.0)] * sum(len(f) for f in pf))
        piece_ranges.append((p, base, len(local), local))
        groups.append((p.bone, base, len(local)))
        base += len(local)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    if with_uv:
        layer = mesh.uv_layers.new(name="UVMap")
        layer.data.foreach_set("uv", np.array(uvs, dtype=np.float32).ravel())
    mesh.shade_smooth()
    mesh.set_sharp_from_angle(angle=math.radians(SHARP_DEGREES))
    obj = C.link(bpy.data.objects.new(name, mesh))
    if attributes:
        write_attributes(mesh, piece_ranges)
    for bone, b, n in groups:
        vg = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
        vg.add(list(range(b, b + n)), 1.0, "REPLACE")
    return obj


def _lod_uvs(piece, local, faces):
    """UVs for a chamfer-free rebuild of `piece`: each corner is looked up on the LOD0 face it belongs to (the
    query point is pulled towards the face centre, past the chamfer) and extrapolated with that face's affine
    UV map, so the LOD face covers the same texels as the flat face of LOD0."""
    tris, tri_uv = [], []
    k = 0
    for f in piece.faces:
        uv = piece.uv[k:k + len(f)]
        for i in range(1, len(f) - 1):
            tris.append((f[0], f[i], f[i + 1]))
            tri_uv.append((uv[0], uv[i], uv[i + 1]))
        k += len(f)
    co = [Vector(v) for v in piece.local]
    tree = BVHTree.FromPolygons(co, tris)
    out = []
    inset = 2.5 * piece.bevel + 0.002
    for f in faces:
        pts = [Vector(local[i]) for i in f]
        centre = sum(pts, Vector()) / len(pts)
        normal = (pts[1] - pts[0]).cross(pts[2] - pts[0]).normalized() if len(pts) >= 3 else Vector((0, 0, 1))
        for v in pts:
            d = centre - v
            q = v + d.normalized() * min(inset, d.length * 0.5) if d.length > 1e-9 else v
            hit = tree.find_nearest(q - normal * 1e-4)
            ti = hit[2] if hit[2] is not None else 0
            a, b, c = (co[i] for i in tris[ti])
            ua, ub, uc = (Vector((*tri_uv[ti][j], 0)) for j in range(3))
            out.append(tuple(_affine_uv(v, a, b, c, ua, ub, uc)))
    return out


def _affine_uv(p, a, b, c, ua, ub, uc):
    e1, e2 = b - a, c - a
    n = e1.cross(e2)
    if n.length < 1e-12:
        return (ua.x, ua.y)
    # Barycentric coordinates of p projected into the triangle's plane (extrapolates outside the triangle).
    d = p - a
    d = d - n.normalized() * d.dot(n.normalized())
    m = Matrix(((e1.dot(e1), e1.dot(e2)), (e1.dot(e2), e2.dot(e2))))
    rhs = Vector((d.dot(e1), d.dot(e2)))
    try:
        s, t = m.inverted() @ rhs
    except ValueError:
        return (ua.x, ua.y)
    uv = ua + (ub - ua) * s + (uc - ua) * t
    return (uv.x, uv.y)


def write_attributes(mesh, piece_ranges):
    n = len(mesh.vertices)
    att = {k: np.zeros((n, 4), dtype=np.float32) for k in ("Paint", "Emit", "MatW", "Pan", "Look2")}
    vec = {k: np.zeros((n, 3), dtype=np.float32) for k in ("Loc", "AxX", "AxY", "AxZ")}
    for p, b, cnt, local in piece_ranges:
        s = slice(b, b + cnt)
        L = p.look
        att["Paint"][s] = (*srgb(L.paint), 1.0)
        e = np.asarray(L.emit if L.emit is not None else (0, 0, 0), dtype=float) * L.glow
        att["Emit"][s] = (*srgb(np.clip(e, 0, 1)) * max(1.0, L.glow), 1.0)
        fam = np.zeros(4)
        if L.fam in FAMILIES:
            fam[FAMILIES.index(L.fam)] = 1.0
        att["MatW"][s] = fam
        att["Pan"][s] = (L.panel, 1.0 if L.bolts else 0.0, 1.0 if L.stripes else 0.0, L.wear)
        att["Look2"][s] = (L.rough, L.grad, L.lens, L.rings)
        vec["Loc"][s] = local
        ax, ay, az = p.axes()
        vec["AxX"][s] = ax
        vec["AxY"][s] = ay
        vec["AxZ"][s] = az
    for k, arr in att.items():
        a = mesh.attributes.new(k, "FLOAT_COLOR", "POINT")
        a.data.foreach_set("color", arr.ravel())
    for k, arr in vec.items():
        a = mesh.attributes.new(k, "FLOAT_VECTOR", "POINT")
        a.data.foreach_set("vector", arr.ravel())


# -------------------------------------------------------------------------------------------------------------
# Atlas unwrap
# -------------------------------------------------------------------------------------------------------------

def unwrap_kit(kit, margin=0.0025, angle=66.0):
    """Unwraps the union of every piece of every variant into one atlas and stores the UVs on the pieces."""
    obj = assemble(kit.pieces, kit.name + "_Atlas", with_uv=True)
    C.activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, area_weight=0.0,
                             correct_aspect=True, scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.average_islands_scale()
    bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")
    uv = np.empty(len(obj.data.loops) * 2, dtype=np.float32)
    obj.data.uv_layers.active.data.foreach_get("uv", uv)
    uv = uv.reshape(-1, 2)
    k = 0
    for p in kit.pieces:
        n = p.loops()
        p.uv = [tuple(x) for x in uv[k:k + n]]
        k += n
    stats = uv_stats(obj)
    bpy.data.objects.remove(obj)
    return stats


def uv_stats(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    uvl = mesh.uv_layers.active.data
    area3 = area2 = 0.0
    for t in mesh.loop_triangles:
        a, b, c = (mesh.vertices[i].co for i in t.vertices)
        area3 += (b - a).cross(c - a).length / 2
        ua, ub, uc = (uvl[i].uv for i in t.loops)
        area2 += abs((ub - ua).x * (uc - ua).y - (ub - ua).y * (uc - ua).x) / 2
    return {"coverage": round(area2, 3), "world_per_uv": round(math.sqrt(area3 / max(area2, 1e-9)), 4)}


# -------------------------------------------------------------------------------------------------------------
# The self-bake shader
# -------------------------------------------------------------------------------------------------------------

_CH = {}
TOP, SIDE, UNDER = 1.0, 0.74, 0.36
DUST = (0.13, 0.12, 0.11)
SUBSTRATE = (0.06, 0.065, 0.07)
POLISHED = (0.42, 0.44, 0.47)
HAZ_DARK = (0.012, 0.013, 0.015)
GROOVE_W = 0.0032
BOLT_R = 0.0055
BOLT_INSET = 0.013
STRIPE_PERIOD = 0.06


def _dot(g, a, b):
    return g.vec("DOT_PRODUCT", a, b).outputs["Value"]


def _xyz(g, v):
    n = g.node("ShaderNodeSeparateXYZ")
    g.set(n.inputs[0], v)
    return n.outputs[0], n.outputs[1], n.outputs[2]


def _mix_rgb(g, fac, a, b):
    m = g.node("ShaderNodeMix", data_type="RGBA", clamp_factor=True)
    g.set(m.inputs["Factor"], fac)
    g.set(m.inputs[6], a)
    g.set(m.inputs[7], b)
    return m.outputs[2]


def _cell(g, u, spacing):
    """Distance from u to the nearest seam (cell border), cells centred on u = 0."""
    c = g.sub(g.math("FRACT", g.add(g.math("DIVIDE", u, spacing), 0.5)), 0.5)
    return g.mul(g.sub(0.5, g.math("ABSOLUTE", c)), spacing)


def _plane_features(g, u, v, spacing, period):
    du = _cell(g, u, spacing)
    dv = _cell(g, v, spacing)
    groove = g.math("MAXIMUM", g.map(du, 0.0, GROOVE_W, 1.0, 0.0), g.map(dv, 0.0, GROOVE_W, 1.0, 0.0))
    bx = g.sub(du, BOLT_INSET)
    by = g.sub(dv, BOLT_INSET)
    d = g.math("SQRT", g.add(g.mul(bx, bx), g.mul(by, by)))
    bolt = g.math("SQRT", g.math("MAXIMUM", g.sub(1.0, g.math("POWER", g.math("DIVIDE", d, BOLT_R), 2.0)), 0.0))
    s = g.math("FRACT", g.math("DIVIDE", g.add(u, v), period))
    stripe = g.map(g.math("ABSOLUTE", g.sub(s, 0.5)), 0.23, 0.27, 1.0, 0.0)
    return groove, bolt, stripe


def bake_material(name="TowerBake", seed=0.0):
    mat = C.new_material(name)
    nt = mat.node_tree
    nt.nodes.clear()
    g = surface.Graph(nt)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emission = nt.nodes.new("ShaderNodeEmission")

    loc = g.attr("Loc").outputs["Vector"]
    axes = [g.attr(n).outputs["Vector"] for n in ("AxX", "AxY", "AxZ")]
    paint = g.attr("Paint").outputs["Color"]
    emit = g.attr("Emit").outputs["Color"]
    matw = g.attr("MatW")
    cer, met, rub = g.rgb(matw.outputs["Color"])
    gla = matw.outputs["Alpha"]
    pan = g.attr("Pan")
    spacing, bolts_on, stripes_on = g.rgb(pan.outputs["Color"])
    wear_amt = pan.outputs["Alpha"]
    look = g.attr("Look2")
    rough0, grad, lens = g.rgb(look.outputs["Color"])
    rings = look.outputs["Alpha"]

    geo = g.node("ShaderNodeNewGeometry")
    N = geo.outputs["True Normal"]
    P = geo.outputs["Position"]
    _, _, nz = _xyz(g, N)
    _, _, pz = _xyz(g, P)
    lx, ly, lz = _xyz(g, loc)

    # Which face of the piece's own box this pixel is on: seams run in the two other local axes.
    w = [g.math("POWER", g.math("ABSOLUTE", _dot(g, N, a)), 8.0) for a in axes]
    wsum = g.add(w[0], w[1], w[2], 1e-6)
    w = [g.math("DIVIDE", x, wsum) for x in w]
    sp = g.math("MAXIMUM", spacing, 0.001)
    enable = g.math("GREATER_THAN", spacing, 0.0005)
    groove = bolt = stripe = None
    for wi, (u, v) in zip(w, ((ly, lz), (lx, lz), (lx, ly))):
        gr, bo, st = _plane_features(g, u, v, sp, STRIPE_PERIOD)
        groove = g.mul(gr, wi) if groove is None else g.add(groove, g.mul(gr, wi))
        bolt = g.mul(bo, wi) if bolt is None else g.add(bolt, g.mul(bo, wi))
        stripe = g.mul(st, wi) if stripe is None else g.add(stripe, g.mul(st, wi))
    groove = g.mul(groove, enable)
    bolt = g.mul(g.mul(bolt, enable), bolts_on)
    # Ring grooves along the local axis (barrel jackets, heat sinks, lathed bodies).
    ring_on = g.math("GREATER_THAN", rings, 0.0005)
    rd = _cell(g, lz, g.math("MAXIMUM", rings, 0.001))
    ring = g.mul(g.map(rd, 0.0, GROOVE_W * 1.2, 1.0, 0.0), ring_on)
    groove = g.math("MAXIMUM", groove, ring)

    # Rounded edges and edge wear.
    bev = g.node("ShaderNodeBevel")
    bev.inputs["Radius"].default_value = 0.007
    bev.samples = 8
    nb = bev.outputs["Normal"]
    edge = g.map(g.sub(1.0, _dot(g, nb, N)), 0.004, 0.08)
    noise_chip = g.noise(P, 34.0, detail=3.0, rough=0.6, w=seed + 3.0)
    chip = g.mul(g.map(g.add(noise_chip, g.mul(edge, 0.55)), 0.78, 0.84), g.math("MINIMUM", g.mul(edge, 4.0), 1.0))
    chip = g.mul(chip, wear_amt)
    scr = g.mul(surface.scratches(g, g.vscale(P, 1.0), 90.0, seed + 11.0), wear_amt)
    paint_w = g.add(cer, rub)

    # Value ramp: white tops, mid sides, dark undersides.
    upper = g.map(nz, -0.1, 0.8)
    lower = g.map(nz, -0.9, -0.1)
    val = g.add(UNDER, g.mul(g.sub(SIDE, UNDER), lower))
    val = g.add(val, g.mul(g.sub(TOP, val), upper))
    val = g.add(1.0, g.mul(g.sub(val, 1.0), grad))
    base = g.vscale(paint, val)

    # Hazard stripes: yellow and near-black.
    haz = _mix_rgb(g, stripe, (*HAZ_DARK, 1.0), paint)
    base = _mix_rgb(g, stripes_on, base, g.vscale(haz, val))
    # Glass: dark with a cool sheen picked up from the ramp.
    base = _mix_rgb(g, gla, base, g.vscale(tuple(srgb(PAL["glass"])), g.add(0.6, g.mul(upper, 0.8))))
    # Seams and bolt heads.
    base = g.vscale(base, g.sub(1.0, g.mul(groove, 0.62)))
    bolt_mask = g.map(bolt, 0.02, 0.2)
    base = _mix_rgb(g, g.mul(bolt_mask, 0.85), base, g.vscale(tuple(srgb((0.5, 0.52, 0.56))), g.add(0.55, g.mul(bolt, 0.6))))
    # Chips: painted plates show the dark substrate, metal shows polished edges.
    base = _mix_rgb(g, g.mul(chip, paint_w), base, (*SUBSTRATE, 1.0))
    base = _mix_rgb(g, g.mul(g.math("MAXIMUM", chip, g.mul(edge, 0.25)), met), base, (*POLISHED, 1.0))
    base = _mix_rgb(g, g.mul(scr, g.mul(met, 0.6)), base, (*POLISHED, 1.0))
    base = g.vscale(base, g.sub(1.0, g.mul(scr, g.mul(paint_w, 0.22))))

    # Grime: ambient occlusion, rain streaks on the sides, dust on the foot.
    ao_node = g.node("ShaderNodeAmbientOcclusion")
    ao_node.inputs["Distance"].default_value = 0.1
    ao_node.samples = 16
    ao = ao_node.outputs["AO"]
    grime = g.sub(1.0, ao)
    base = g.vscale(base, g.sub(1.0, g.mul(grime, 0.55)))
    streak_n = g.noise(g.mapping(P, scale=(46.0, 46.0, 5.0)), 1.0, detail=2.0, w=seed + 21.0)
    side = g.sub(1.0, g.math("ABSOLUTE", nz))
    streak = g.mul(g.map(streak_n, 0.55, 0.75), g.mul(side, 0.22))
    base = g.vscale(base, g.sub(1.0, g.mul(streak, g.sub(1.0, gla))))
    dust = g.mul(g.map(pz, 0.09, 0.0), 0.55)
    dust = g.mul(dust, g.map(g.noise(P, 18.0, detail=2.0, w=seed + 31.0), 0.3, 0.6, 0.5, 1.0))
    base = _mix_rgb(g, dust, base, (*DUST, 1.0))

    # Glow: lenses hottest in the centre.
    lens_on = g.math("GREATER_THAN", lens, 0.0005)
    rr = g.math("DIVIDE", g.math("SQRT", g.add(g.mul(lx, lx), g.mul(ly, ly))), g.math("MAXIMUM", lens, 0.001))
    lf = g.map(rr, 0.0, 1.0, 1.35, 0.45)
    lf = g.add(g.mul(lf, lens_on), g.sub(1.0, lens_on))
    final_emit = g.vscale(emit, lf)

    rough = g.add(rough0, g.mul(groove, 0.12), g.mul(grime, 0.1), g.mul(dust, 0.25))
    rough = g.sub(rough, g.mul(g.add(chip, g.mul(scr, 0.6)), g.mul(met, 0.22)))
    rough = g.add(rough, g.mul(scr, g.mul(paint_w, 0.12)))
    rough = g.math("MINIMUM", g.math("MAXIMUM", rough, 0.05), 0.95)

    fam = g.node("ShaderNodeCombineColor")
    g.set(fam.inputs[0], met)
    g.set(fam.inputs[1], cer)
    g.set(fam.inputs[2], 0.0)

    dent = g.sub(g.noise(P, 22.0, detail=1.0, w=seed + 41.0), 0.5)
    height = g.add(g.mul(groove, -0.0011), g.mul(bolt, 0.0016), g.mul(scr, -0.00012),
                   g.mul(g.mul(chip, paint_w), -0.0005), g.mul(g.mul(dent, met), 0.00025))
    bump = g.node("ShaderNodeBump")
    g.set(bump.inputs["Height"], height)
    bump.inputs["Distance"].default_value = 1.0
    bump.inputs["Strength"].default_value = 1.0
    g.set(bump.inputs["Normal"], nb)
    diffuse = nt.nodes.new("ShaderNodeBsdfDiffuse")
    nt.links.new(bump.outputs["Normal"], diffuse.inputs["Normal"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])
    ao_col = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g.set(ao_col.inputs[i], ao)
    rough_col = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g.set(rough_col.inputs[i], rough)
    _CH[mat.name] = {"albedo": base, "emission": final_emit, "ao": ao_col.outputs[0], "rough": rough_col.outputs[0],
                     "fam": fam.outputs[0], "normal": diffuse.outputs[0], "_emission": emission, "_out": out}
    return mat


def _channel(mat, channel):
    ch = _CH[mat.name]
    nt = mat.node_tree
    out, emission = ch["_out"], ch["_emission"]
    for l in list(out.inputs["Surface"].links) + list(emission.inputs["Color"].links):
        nt.links.remove(l)
    if channel == "normal":
        nt.links.new(ch["normal"], out.inputs["Surface"])
        return
    nt.links.new(ch[channel], emission.inputs["Color"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])


def bake(kit, out_dir, size=1024, low_size=512, samples=12, spacing=4.0):
    """Self-bakes the kit's atlas: Albedo, Normal, Emission and the packed Mask (R AO, G smoothness, B metal
    detail, A ceramic detail), plus the Low set. Returns the map paths."""
    os.makedirs(out_dir, exist_ok=True)
    offsets = {}
    index = {v: i for i, v in enumerate(kit.variants)}
    for p in kit.pieces:
        offsets[p] = (index[kit.home(p)] * spacing, 0.0, 0.0)
    target = assemble(kit.pieces, kit.name + "_Bake", offsets, attributes=True)
    occluders = []
    for v in kit.variants:
        others = [p for p in kit.of(v) if kit.home(p) != v]
        if others:
            off = {p: (index[v] * spacing, 0.0, 0.0) for p in others}
            occluders.append(assemble(others, kit.name + "_Occ_" + v, off, with_uv=False))
    mat = bake_material(kit.name + "_BakeMat")
    target.data.materials.append(mat)
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    mat.node_tree.nodes.active = node

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    world = scene.world or bpy.data.worlds.new("BakeWorld")
    scene.world = world
    b = scene.render.bake
    b.use_selected_to_active = False
    b.margin = max(6, size // 96)
    b.use_clear = True
    b.target = "IMAGE_TEXTURES"
    hidden = [(o, o.hide_render) for o in scene.objects if o not in [target] + occluders]
    for o, _ in hidden:
        o.hide_render = True
    C.activate(target)

    def one(kind, channel, space):
        img = bpy.data.images.new(f"{kit.name}_{channel}", size, size, alpha=False, float_buffer=True)
        img.colorspace_settings.name = space
        node.image = img
        _channel(mat, channel)
        if kind == "NORMAL":
            bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")
        else:
            bpy.ops.object.bake(type="EMIT")
        arr = np.array(img.pixels[:], dtype=np.float32).reshape(size, size, 4)
        bpy.data.images.remove(img)
        C.log("baked", kit.name, channel)
        return arr

    maps = {"albedo": one("EMIT", "albedo", "Linear Rec.709"),
            "emission": one("EMIT", "emission", "Linear Rec.709"),
            "normal": one("NORMAL", "normal", "Non-Color")}
    ao = one("EMIT", "ao", "Non-Color")
    rough = one("EMIT", "rough", "Non-Color")
    fam = one("EMIT", "fam", "Non-Color")
    maps["mask"] = np.clip(np.dstack([ao[..., 0], 1.0 - rough[..., 0], fam[..., 0], fam[..., 1]]), 0, 1)
    for o, h in hidden:
        o.hide_render = h
    bpy.data.objects.remove(target)
    for o in occluders:
        bpy.data.objects.remove(o)

    spaces = {"albedo": "sRGB", "emission": "sRGB", "normal": "Non-Color", "mask": "Non-Color"}
    suffix = {"albedo": "Albedo", "emission": "Emission", "normal": "Normal", "mask": "Mask"}
    paths = {}
    for key, arr in maps.items():
        paths[key] = _save(arr, os.path.join(out_dir, f"{kit.name}_{suffix[key]}.png"), spaces[key])
        if low_size:
            small = C.downsample(arr, size // low_size, normal=key == "normal")
            _save(small, os.path.join(out_dir, "Low", f"{kit.name}_{suffix[key]}.png"), spaces[key])
    return paths


def _save(arr, path, space):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out = arr.copy()
    if space == "sRGB":
        out[..., :3] = C.linear_to_srgb(out[..., :3])
    if not path.endswith("_Mask.png"):
        out[..., 3] = 1.0
    C.array_to_image(out, path, colorspace=space)
    return path


# -------------------------------------------------------------------------------------------------------------
# Shared detail normal for metal (new file; the existing detail maps are left alone)
# -------------------------------------------------------------------------------------------------------------

def metal_detail_map(size=512, force=False):
    """Detail_Metal_Normal.png: isotropic micro-dents, fine grain and sparse pits for gunmetal and steel."""
    path = os.path.join(detailmaps.OUT, "Detail_Metal_Normal.png")
    if os.path.exists(path) and not force:
        return path
    n = size
    h = 0.25 * detailmaps.band_noise(n, 40, 160, 301)
    h += 0.35 * detailmaps.band_noise(n, 3, 9, 302)
    h -= 0.7 * detailmaps.pits(n, 36, 303, 0.1, radius=0.2)
    nrm = detailmaps.normal_from_height(h, 1.6)
    rgba = np.dstack([nrm * 0.5 + 0.5, np.ones((n, n))])
    os.makedirs(detailmaps.OUT, exist_ok=True)
    C.array_to_image(rgba, path, colorspace="Non-Color")
    C.log("detail map", path)
    return path


def detail_entries(world_per_uv):
    """[(path, tiling, strength, mask channel)] for the preview material, and the report block."""
    tiles = detailmaps.ensure()
    metal = metal_detail_map()
    t_metal = round(world_per_uv / METAL_TILE_WORLD, 1)
    t_cer = round(world_per_uv / detailmaps.TILE_WORLD["ceramic"], 1)
    entries = [(metal, t_metal, 0.6, "B"), (tiles["ceramic"], t_cer, 0.5, "A")]
    report = {"metal": {"map": "Detail_Metal_Normal.png", "tiling": t_metal, "mask": "B"},
              "ceramic": {"map": "Detail_Ceramic_Normal.png", "tiling": t_cer, "mask": "A"}}
    return entries, report


# -------------------------------------------------------------------------------------------------------------
# Rig, clips and export
# -------------------------------------------------------------------------------------------------------------

class Bone:
    def __init__(self, name, head, direction, parent=None, deform=True, length=0.08):
        self.name = name
        self.head = Vector(head)
        self.dir = Vector(direction).normalized()
        self.parent = parent
        self.deform = deform
        self.length = length


def build_armature(name, bones):
    data = bpy.data.armatures.new(name)
    arm = C.link(bpy.data.objects.new(name, data))
    C.activate(arm)
    bpy.ops.object.mode_set(mode="EDIT")
    for b in bones:
        eb = data.edit_bones.new(b.name)
        eb.head = b.head
        eb.tail = b.head + b.dir * b.length
        # Roll so the bone's local X lies in the horizontal plane where possible: predictable hinge axes.
        eb.align_roll(Vector((0, 0, 1)) if abs(b.dir.z) < 0.9 else Vector((0, -1, 0)))
        eb.use_deform = b.deform
        if b.parent:
            eb.parent = data.edit_bones[b.parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return arm


def local_axis(arm, bone, world_axis):
    rest = arm.data.bones[bone].matrix_local.to_3x3()
    return (rest.inverted() @ Vector(world_axis)).normalized()


def clip(arm, name, frames, keys, loop=False):
    """keys(t, f) -> {bone: {"rot": (world_axis, angle), "loc": world_vector}} for frames 0..frames. Bones not
    named rest. Rotations and offsets are given in armature space and converted to each bone's own frame."""
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    bones = [b.name for b in arm.data.bones]
    for f in range(frames + 1):
        t = (f % frames) / frames if loop else f / frames
        spec = keys(t, f)
        for bname in bones:
            pb = arm.pose.bones[bname]
            s = spec.get(bname, {})
            q = Quaternion()
            if "rot" in s:
                axis, angle = s["rot"]
                q = Quaternion(local_axis(arm, bname, axis), angle)
            loc = Vector()
            if "loc" in s:
                rest = arm.data.bones[bname].matrix_local.to_3x3()
                loc = rest.inverted() @ Vector(s["loc"])
            pb.rotation_quaternion = q
            pb.location = loc
            pb.keyframe_insert("rotation_quaternion", frame=f, group=bname)
            pb.keyframe_insert("location", frame=f, group=bname)
    return act


def skin_to(obj, arm):
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm


def export_fbx(path, objects, animated):
    """common.export_fbx's settings, but keeping non-deforming bones (the Muzzle and Smoke sockets)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in bpy.context.view_layer.objects:
        if o is not None:
            o.select_set(o in objects)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"}, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=False, mesh_smooth_type="FACE", use_tspace=True, add_leaf_bones=False,
        use_armature_deform_only=False, bake_anim=animated, bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        path_mode="STRIP", embed_textures=False)
    C.log("exported", path)


# -------------------------------------------------------------------------------------------------------------
# Preview staging
# -------------------------------------------------------------------------------------------------------------

GAME_PITCH = 44.0
GAME_YAW = 16.0
PX_PER_UNIT = 720 / 14.0


def game_camera(target, ortho_scale, distance=30.0):
    """The game's fixed camera as Unity has it (pitch 44, yaw 16, looking along Unity +Z), in Blender space:
    it sits on the +Y side, a little towards +X."""
    cam_data = bpy.data.cameras.new("GameCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam_data.clip_end = 200
    cam = C.link(bpy.data.objects.new("GameCam", cam_data))
    p, y = math.radians(GAME_PITCH), math.radians(GAME_YAW)
    cam.location = Vector(target) + Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p))) * distance
    C.look_at(cam, Vector(target))
    return cam


def game_lights(key=3.2, back=1.1):
    """Like a level: the key sun from the camera side, upper left (Unity sun pitch 42, yaw -35), a cool back sun."""
    objs = []
    k = bpy.data.lights.new("GameKey", "SUN")
    k.energy = key
    k.angle = math.radians(4)
    k.color = (1.0, 0.95, 0.88)
    o = C.link(bpy.data.objects.new("GameKey", k))
    C.look_at(o, o.location + Vector((0.426, -0.609, -0.669)))
    objs.append(o)
    b = bpy.data.lights.new("GameBack", "SUN")
    b.energy = back
    b.color = (0.5, 0.7, 1.0)
    b.use_shadow = False
    o = C.link(bpy.data.objects.new("GameBack", b))
    C.look_at(o, o.location + Vector((-0.3, 0.7, -0.5)))
    objs.append(o)
    return objs


def closeup_camera(target, distance, azimuth=20.0, elevation=24.0, lens=60.0):
    """Azimuth 0 looks from the camera side (+Y); positive turns towards +X."""
    cam_data = bpy.data.cameras.new("CloseCam")
    cam_data.lens = lens
    cam_data.clip_start = 0.01
    cam = C.link(bpy.data.objects.new("CloseCam", cam_data))
    a, e = math.radians(azimuth), math.radians(elevation)
    cam.location = Vector(target) + Vector((math.sin(a) * math.cos(e), math.cos(a) * math.cos(e), math.sin(e))) * distance
    C.look_at(cam, Vector(target))
    return cam


def close_lights(scale=1.0):
    """Studio rig for close-ups seen from +Y: key from the upper left front, cool fill, strong rim from behind."""
    objs = []
    k = bpy.data.lights.new("Key", "SUN")
    k.energy = 3.0
    k.angle = math.radians(5)
    k.color = (1.0, 0.96, 0.9)
    o = C.link(bpy.data.objects.new("Key", k))
    C.look_at(o, o.location + Vector((0.5, -0.6, -0.65)))
    objs.append(o)
    f = bpy.data.lights.new("Fill", "AREA")
    f.energy = 30 * scale * scale
    f.size = 3 * scale
    f.color = (0.6, 0.7, 1.0)
    o = C.link(bpy.data.objects.new("Fill", f))
    o.location = Vector((2.5, 1.5, 1.2)) * scale
    C.look_at(o, Vector((0, 0, 0.3 * scale)))
    objs.append(o)
    r = bpy.data.lights.new("Rim", "AREA")
    r.energy = 110 * scale * scale
    r.size = 2 * scale
    r.color = (0.55, 0.75, 1.0)
    o = C.link(bpy.data.objects.new("Rim", r))
    o.location = Vector((-1.2, -3.0, 2.0)) * scale
    C.look_at(o, Vector((0, 0, 0.3 * scale)))
    objs.append(o)
    return objs


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    C.log("report", path)
