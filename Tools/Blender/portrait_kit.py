"""Helpers for hand-shaped character heads and their texel-space textures (round 19, Dr. Voss rebuild).

The creature pipeline (common.py, suit.py) fuses primitives with a voxel remesh, decimates the result and bakes the
detail back. That is right for chitin and armour, but a face made that way loses its edge loops: the eyes and the mouth
become holes in a random triangle soup, and the blink and the jaw stretch it. This module builds a head the way a
modeller would, in three steps:

1. **Shape** is a signed distance field (`sd_*`, `smin`, `smax`): soft volumes (cranium, jaw, cheekbones, nose, lips,
   lids round the eyeballs, hair chunks) blended into one surface.
2. **Topology** is laid out in a 2D parameter space (azimuth, elevation round a centre inside the head): concentric
   constraint loops round each eye and round the mouth, a size field that is dense at the eyes and lips and coarse at
   the back, a constrained Delaunay triangulation and a relax. Every vertex is then shot outwards along its ray and
   lands on the outermost surface of the field (`trace_outermost`). The eye openings and the mouth slit are real holes
   whose rims roll inwards (lid margins onto the eyeball, lips into a mouth bag), so a lid bone or a jaw bone moves
   whole loops.
3. **Textures** are painted in texel space: Cycles bakes the game mesh's own position, normal and part id into float
   images (a G-buffer), and numpy evaluates the colour, height (into a tangent normal map), roughness and glow
   functions per texel. Detail is then as sharp as the texture, without a million-polygon source.

Coordinates: everything here works in the character's local game space (x across with +x her left, y up, z forward);
`Frame.to_blender` maps it to Blender (x, -z, y) plus the character's offset.
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Vector
from mathutils.geometry import delaunay_2d_cdt

import common as C


# -------------------------------------------------------------------------------------------------------------
# Signed distance field toolkit (numpy, points as (n, 3) arrays)
# -------------------------------------------------------------------------------------------------------------

def sd_sphere(p, c, r):
    return np.linalg.norm(p - np.asarray(c, float), axis=1) - r


def sd_ellipsoid(p, c, r):
    """Approximate (bound-friendly) ellipsoid distance (Quilez)."""
    q = (p - np.asarray(c, float)) / np.asarray(r, float)
    k0 = np.linalg.norm(q, axis=1)
    k1 = np.linalg.norm(q / np.asarray(r, float), axis=1)
    return k0 * (k0 - 1.0) / np.maximum(k1, 1e-9)


def sd_round_cone(p, a, b, ra, rb):
    """Capsule whose radius runs from ra at a to rb at b (a tapered stroke)."""
    a = np.asarray(a, float)
    b = np.asarray(b, float)
    ab = b - a
    l2 = max(float(ab @ ab), 1e-12)
    t = np.clip(((p - a) @ ab) / l2, 0.0, 1.0)
    q = a + t[:, None] * ab
    return np.linalg.norm(p - q, axis=1) - (ra + (rb - ra) * t)


def sd_polyline(p, pts, radii):
    """Union of tapered strokes along a polyline (a lock of hair, a fold)."""
    d = np.full(len(p), 1e9)
    for i in range(len(pts) - 1):
        d = np.minimum(d, sd_round_cone(p, pts[i], pts[i + 1], radii[i], radii[i + 1]))
    return d


def smin(a, b, k):
    if k <= 0:
        return np.minimum(a, b)
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


def smax(a, b, k):
    return -smin(-a, -b, k)


def sstep(e0, e1, x):
    t = np.clip((np.asarray(x, float) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def gauss(p, c, r):
    """A soft blob of influence (1 at c, ~0 beyond 2r); r may be a 3-vector for an anisotropic blob."""
    q = (p - np.asarray(c, float)) / np.asarray(r, float)
    return np.exp(-np.sum(q * q, axis=1))


# -------------------------------------------------------------------------------------------------------------
# Ray landing: every parameter vertex lands on the outermost surface along its ray
# -------------------------------------------------------------------------------------------------------------

def directions(az, el):
    """Unit rays for azimuth (0 forward, + towards +x) and elevation, local game space."""
    ce = np.cos(el)
    return np.stack([np.sin(az) * ce, np.sin(el), np.cos(az) * ce], axis=1)


def trace_outermost(sdf, origin, dirs, rmax, iters=160, eps=2e-5):
    """Sphere tracing inwards from rmax along each ray; returns the distance from the origin of the first (outermost)
    surface met. Rays that find nothing keep rmax."""
    o = np.asarray(origin, float)
    t = np.full(len(dirs), float(rmax))
    done = np.zeros(len(dirs), bool)
    for _ in range(iters):
        idx = np.nonzero(~done)[0]
        if len(idx) == 0:
            break
        d = sdf(o + dirs[idx] * t[idx, None])
        hit = d < eps
        done[idx[hit]] = True
        step = np.maximum(d * 0.8, 5e-5)
        t[idx[~hit]] -= step[~hit]
        neg = t < 0
        t[neg] = 0
        done[neg] = True
    # Polish: a few Newton-like steps so the vertex sits on the zero set, not up to eps outside it.
    for _ in range(4):
        d = sdf(o + dirs * t[:, None])
        t = t - d
    return t


def numeric_normals(sdf, p, h=2e-4):
    ex = np.array((h, 0, 0))
    ey = np.array((0, h, 0))
    ez = np.array((0, 0, h))
    n = np.stack([sdf(p + ex) - sdf(p - ex), sdf(p + ey) - sdf(p - ey), sdf(p + ez) - sdf(p - ez)], axis=1)
    return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)


def project(sdf, p, steps=5):
    """Moves points onto the field's zero set along the gradient."""
    for _ in range(steps):
        p = p - sdf(p)[:, None] * numeric_normals(sdf, p)
    return p


def edges_of(faces):
    e = set()
    for f in faces:
        n = len(f)
        for i in range(n):
            a, b = int(f[i]), int(f[(i + 1) % n])
            e.add((min(a, b), max(a, b)))
    return np.array(sorted(e), dtype=np.int64)


def relax_project(sdf, p, faces, iters=10, fixed=None, amount=0.5):
    """Uniform Laplacian relax with re-projection onto the field: evens out triangles stretched over undercuts (the
    nose's underside seen from the ray centre) while keeping the surface. `fixed` vertices do not move."""
    e = edges_of(faces)
    n = len(p)
    move = np.ones(n, bool) if fixed is None else ~np.asarray(fixed, bool)
    for _ in range(iters):
        acc = np.zeros_like(p)
        cnt = np.zeros(n)
        np.add.at(acc, e[:, 0], p[e[:, 1]])
        np.add.at(acc, e[:, 1], p[e[:, 0]])
        np.add.at(cnt, e[:, 0], 1)
        np.add.at(cnt, e[:, 1], 1)
        avg = acc / np.maximum(cnt, 1)[:, None]
        q = p + (avg - p) * amount
        q = project(sdf, q, 3)
        p = np.where(move[:, None], q, p)
    return p


# -------------------------------------------------------------------------------------------------------------
# 2D parameter meshing: constraint loops + a size field + CDT + relax
# -------------------------------------------------------------------------------------------------------------

class ParamMesh:
    """A 2D triangulation with fixed (constraint) vertices and free vertices.

    `loops`: list of (points (k, 2), closed) constraint polylines; their vertices never move. Holes are given as
    closed polygons; triangles whose centroid lies inside any hole are dropped."""

    def __init__(self):
        self.fixed = []          # list of (2,) points
        self.edges = []          # constraint edges (indices into fixed)
        self.holes = []          # polygons (k, 2)
        self.loop_ids = []       # index ranges of each loop in `fixed`

    def add_loop(self, pts, closed=True):
        start = len(self.fixed)
        for p in pts:
            self.fixed.append((float(p[0]), float(p[1])))
        n = len(pts)
        for i in range(n - 1 + (1 if closed else 0)):
            self.edges.append((start + i, start + (i + 1) % n))
        self.loop_ids.append(list(range(start, start + n)))
        return self.loop_ids[-1]

    def add_hole(self, pts):
        self.holes.append(np.asarray(pts, float))


def point_in_polygon(pts, poly):
    """Even-odd rule, vectorised over pts (n, 2)."""
    x, y = pts[:, 0], pts[:, 1]
    inside = np.zeros(len(pts), bool)
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        cond = ((y0 > y) != (y1 > y))
        xint = x0 + (y - y0) * (x1 - x0) / ((y1 - y0) if y1 != y0 else 1e-12)
        inside ^= cond & (x < xint)
    return inside


def dart_points(bounds, spacing, metric, keep, seed=1, fixed=None, cells=(240, 160), oversample=4.0):
    """Poisson-disk-like scatter in a 2D parameter box. `spacing(pts)` -> local target spacing (metres),
    `metric(pts)` -> (mx, my): metres per parameter unit along each axis there, `keep(pts)` -> bool mask.
    `fixed` points (n, 2) are seeded first so the scatter keeps its distance from the constraint loops."""
    rng = np.random.default_rng(seed)
    (x0, x1), (y0, y1) = bounds
    na, ne = cells
    ca = (x1 - x0) / na
    ce = (y1 - y0) / ne
    # Candidates: per cell, as many as its metric area holds at the local spacing (times oversample).
    gx = x0 + (np.arange(na) + 0.5) * ca
    gy = y0 + (np.arange(ne) + 0.5) * ce
    centres = np.stack(np.meshgrid(gx, gy), -1).reshape(-1, 2)
    mx, my = metric(centres)
    sp = spacing(centres)
    counts = np.ceil(oversample * (ca * mx) * (ce * my) / (0.866 * sp * sp)).astype(int)
    cand = np.repeat(centres, counts, axis=0)
    cand += (rng.random(cand.shape) - 0.5) * np.array((ca, ce))
    cand = cand[keep(cand)]
    rng.shuffle(cand)
    sp = spacing(cand)
    mx, my = metric(cand)
    grid = {}
    acc_x, acc_y, acc_r = [], [], []

    def insert(px, py, r):
        k = (int((px - x0) // ca), int((py - y0) // ce))
        grid.setdefault(k, []).append(len(acc_x))
        acc_x.append(px)
        acc_y.append(py)
        acc_r.append(r)
    if fixed is not None and len(fixed):
        fsp = spacing(np.asarray(fixed))
        for (px, py), r in zip(np.asarray(fixed), fsp):
            insert(px, py, r)
    n_fixed = len(acc_x)
    for i in range(len(cand)):
        px, py = cand[i]
        r = sp[i]
        kx, ky = int((px - x0) // ca), int((py - y0) // ce)
        ra = int(math.ceil(r / (mx[i] * ca))) + 1
        re = int(math.ceil(r / (my[i] * ce))) + 1
        ok = True
        for gxi in range(kx - ra, kx + ra + 1):
            for gyi in range(ky - re, ky + re + 1):
                for j in grid.get((gxi, gyi), ()):
                    dx = (px - acc_x[j]) * mx[i]
                    dy = (py - acc_y[j]) * my[i]
                    lim = 0.5 * (r + acc_r[j])
                    if dx * dx + dy * dy < lim * lim:
                        ok = False
                        break
                if not ok:
                    break
            if not ok:
                break
        if ok:
            insert(px, py, r)
    pts = np.stack([acc_x[n_fixed:], acc_y[n_fixed:]], 1) if len(acc_x) > n_fixed else np.zeros((0, 2))
    return pts


def triangulate(pm, free, metric, relax=4):
    """CDT of fixed + free points with the constraint edges; free points are relaxed towards the (metric-weighted)
    centroid of their neighbours between rounds. Returns (points (n, 2), triangles (m, 3), n_fixed)."""
    fixed = np.asarray(pm.fixed, float)
    nf = len(fixed)
    pts = np.vstack([fixed, free]) if len(free) else fixed
    tris = None
    for it in range(relax + 1):
        res = delaunay_2d_cdt([Vector(p) for p in pts], pm.edges, [], 0, 1e-9)
        out_v = np.array([tuple(v) for v in res[0]])
        tris = np.array(res[2], dtype=np.int64)
        # CDT may merge or add vertices; map back by order (it keeps input order and appends new ones).
        if len(out_v) != len(pts):
            pts = out_v
        cent = pts[tris].mean(axis=1)
        keep = np.ones(len(tris), bool)
        for h in pm.holes:
            keep &= ~point_in_polygon(cent, h)
        tris = tris[keep]
        if it == relax:
            break
        # Neighbour averaging for free points.
        acc = np.zeros_like(pts)
        cnt = np.zeros(len(pts))
        for a, b in ((0, 1), (1, 2), (2, 0)):
            np.add.at(acc, tris[:, a], pts[tris[:, b]])
            np.add.at(cnt, tris[:, a], 1)
            np.add.at(acc, tris[:, b], pts[tris[:, a]])
            np.add.at(cnt, tris[:, b], 1)
        new = acc / np.maximum(cnt, 1)[:, None]
        move = np.arange(len(pts)) >= nf
        move &= cnt > 0
        pts[move] = pts[move] * 0.4 + new[move] * 0.6
    return pts, tris, nf


# -------------------------------------------------------------------------------------------------------------
# Mesh objects
# -------------------------------------------------------------------------------------------------------------

def mesh_object(name, verts, faces, uvs=None, uv_name="UVMap"):
    """Object from Blender-space verts and face index lists; `uvs` per face-corner list matching `faces`."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], [tuple(int(i) for i in f) for f in faces])
    me.validate(clean_customdata=False)
    if uvs is not None:
        layer = me.uv_layers.new(name=uv_name)
        flat = [uv for f in uvs for uv in f]
        if len(flat) == len(layer.data):
            layer.data.foreach_set("uv", np.asarray(flat, np.float32).ravel())
    me.update()
    obj = C.link(bpy.data.objects.new(name, me))
    return obj


def consistent_normals(obj):
    """Orients every face like its neighbours (the eye and mouth roll-ins continue the outer side of the skin)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def set_attr(obj, name, values, domain="POINT", kind="FLOAT"):
    me = obj.data
    if name in me.attributes:
        me.attributes.remove(me.attributes[name])
    a = me.attributes.new(name, kind, domain)
    key = "value" if kind == "FLOAT" else "vector" if kind == "FLOAT_VECTOR" else "color"
    a.data.foreach_set(key, np.asarray(values, np.float32).ravel())
    return a


def get_verts(obj):
    n = len(obj.data.vertices)
    co = np.zeros(n * 3, np.float32)
    obj.data.vertices.foreach_get("co", co)
    return co.reshape(n, 3).astype(float)


def set_verts(obj, co):
    obj.data.vertices.foreach_set("co", np.asarray(co, np.float32).ravel())
    obj.data.update()


# -------------------------------------------------------------------------------------------------------------
# Texel-space painting
# -------------------------------------------------------------------------------------------------------------

def _gbuffer_material(name, attr, mode):
    """Emission of an attribute (or position/normal) so an EMIT bake writes it into a float image."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 1.0
    if mode == "attr":
        a = nt.nodes.new("ShaderNodeAttribute")
        a.attribute_name = attr
        nt.links.new(a.outputs["Vector"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return mat


def bake_gbuffer(obj, size, channels, margin=6):
    """Bakes per-vertex float3 attributes of `obj` (already on the mesh, encoded to be >= 0) into (size, size, 3)
    arrays through its active UV map. Returns {channel: array} and a coverage mask (texels inside islands)."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    scene.cycles.use_denoising = False
    try:
        scene.cycles.pixel_filter_width = 0.01
    except AttributeError:
        pass
    bake = scene.render.bake
    bake.use_selected_to_active = False
    bake.target = "IMAGE_TEXTURES"
    bake.margin = margin
    bake.margin_type = "EXTEND"
    bake.use_clear = True
    saved = list(obj.data.materials)
    others = [(o, o.hide_render) for o in scene.objects if o != obj]
    for o, _ in others:
        o.hide_render = True
    C.activate(obj)
    out = {}
    for ch in list(channels) + ["__cover"]:
        mat = _gbuffer_material("GB_" + ch, ch if ch != "__cover" else "__one", "attr")
        if ch == "__cover":
            # Coverage: constant white emission, baked with no margin.
            nt = mat.node_tree
            em = [n for n in nt.nodes if n.type == "EMISSION"][0]
            for l in list(em.inputs["Color"].links):
                nt.links.remove(l)
            em.inputs["Color"].default_value = (1, 1, 1, 1)
            bake.margin = 0
        img = bpy.data.images.new("GB_" + ch, size, size, alpha=False, float_buffer=True)
        img.colorspace_settings.name = "Non-Color"
        node = mat.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = img
        mat.node_tree.nodes.active = node
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        bpy.ops.object.bake(type="EMIT")
        arr = np.array(img.pixels[:], np.float32).reshape(size, size, 4)[..., :3].copy()
        out[ch] = arr
        bpy.data.images.remove(img)
        bpy.data.materials.remove(mat)
        bake.margin = margin
    obj.data.materials.clear()
    for m in saved:
        obj.data.materials.append(m)
    for o, h in others:
        o.hide_render = h
    cover = out.pop("__cover")[..., 0] > 0.5
    return out, cover


def height_to_normal(height, cover, texel_world, strength=1.0):
    """Tangent-space normal map from a height field painted in texel space (metres). Gradients are taken in UV
    space and scaled by the local world size of a texel, so a 1 mm groove is a 1 mm groove whatever the island
    scale. Assumes UVs without mirroring and roughly conformal islands (true for the unwraps here)."""
    h = height.copy()
    # Outside islands: hold the nearest value so seams do not create cliffs (the bake margin already extends).
    gx = np.zeros_like(h)
    gy = np.zeros_like(h)
    gx[:, 1:-1] = (h[:, 2:] - h[:, :-2]) * 0.5
    gy[1:-1, :] = (h[2:, :] - h[:-2, :]) * 0.5
    tw = np.maximum(texel_world, 1e-7)
    nx = -gx / tw * strength
    ny = -gy / tw * strength
    nz = np.ones_like(h)
    n = np.stack([nx, ny, nz], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def dilate(arr, cover, steps=8):
    """Pushes island colours outwards into empty texels (padding against mip bleeding)."""
    a = arr.copy()
    m = cover.copy()
    for _ in range(steps):
        acc = np.zeros_like(a)
        cnt = np.zeros(m.shape, float)
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sm = np.roll(m, (dy, dx), (0, 1))
            sa = np.roll(a, (dy, dx), (0, 1))
            acc += sa * sm[..., None]
            cnt += sm
        grow = (~m) & (cnt > 0)
        a[grow] = acc[grow] / cnt[grow][:, None]
        m = m | grow
    return a


def save_png(arr, path, srgb):
    out = np.clip(arr, 0, 1).astype(np.float32)
    if out.shape[-1] == 3:
        out = np.concatenate([out, np.ones(out.shape[:2] + (1,), np.float32)], -1)
    if srgb:
        out[..., :3] = C.linear_to_srgb(out[..., :3])
    C.array_to_image(out, path, colorspace="sRGB" if srgb else "Non-Color")
    C.log("map", path)
    return path


def downsample(arr, factor, normal=False):
    return C.downsample(arr, factor, normal=normal)


# -------------------------------------------------------------------------------------------------------------
# Assembly: parts -> one object with packed, density-weighted UVs
# -------------------------------------------------------------------------------------------------------------

class Part:
    def __init__(self, name, verts, faces, pid, weight=1.0, uv=None, seams=(), params=None, smooth=True, face_weight=None):
        self.name = name
        self.verts = np.asarray(verts, float)       # Blender space
        self.faces = [tuple(int(i) for i in f) for f in faces]
        self.pid = pid
        self.weight = weight                        # texel density relative to the rest
        self.uv = uv                                # per-face list of per-corner (u, v) in metres, or None (LSCM)
        self.seams = set((min(a, b), max(a, b)) for a, b in seams)
        self.params = params                        # optional (n, 3) per-vertex extra data for the painters
        self.smooth = smooth
        self.face_weight = face_weight              # optional per-face density weights (one welded mesh, several islands)


class Assembly:
    def __init__(self, name):
        self.name = name
        self.parts = []

    def add(self, part):
        self.parts.append(part)
        return part

    def build(self, margin=0.004):
        """One object: vertices of all parts, attribute `Pid` (part id) and `Prm` (extra params), UVs unwrapped
        (LSCM with the parts' seams, or the parts' own UVs), every island at the same texel density times its part's
        weight, packed into 0..1."""
        verts, faces, pids, prms, smooth, owner, fw = [], [], [], [], [], [], []
        offsets = []
        for k, part in enumerate(self.parts):
            base = len(verts)
            offsets.append(base)
            verts.extend(part.verts)
            faces.extend(tuple(base + i for i in f) for f in part.faces)
            pids.extend([part.pid] * len(part.verts))
            prms.extend(part.params if part.params is not None else np.zeros((len(part.verts), 3)))
            smooth.extend([part.smooth] * len(part.faces))
            owner.extend([k] * len(part.faces))
            fw.extend(part.face_weight if part.face_weight is not None else [part.weight] * len(part.faces))
        me = bpy.data.meshes.new(self.name)
        me.from_pydata([tuple(v) for v in verts], [], faces)
        me.update()
        obj = C.link(bpy.data.objects.new(self.name, me))
        me.polygons.foreach_set("use_smooth", np.asarray(smooth, bool))
        set_attr(obj, "Pid", np.asarray(pids, float))
        set_attr(obj, "Prm", np.asarray(prms, float), kind="FLOAT_VECTOR")
        uv = me.uv_layers.new(name="UVMap")
        owner = np.asarray(owner)
        # Seams for the LSCM parts.
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.edges.ensure_lookup_table()
        bm.faces.ensure_lookup_table()
        seam_set = set()
        for k, part in enumerate(self.parts):
            base = offsets[k]
            for a, b in part.seams:
                seam_set.add((base + a, base + b))
        for e in bm.edges:
            a, b = e.verts[0].index, e.verts[1].index
            if (min(a, b), max(a, b)) in seam_set:
                e.seam = True
        lscm = [k for k, part in enumerate(self.parts) if part.uv is None]
        for f in bm.faces:
            f.select = owner[f.index] in lscm
        bm.to_mesh(me)
        bm.free()
        C.activate(obj)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_mode(type="FACE")
        bpy.ops.uv.unwrap(method="CONFORMAL", margin=0.0)
        bpy.ops.object.mode_set(mode="OBJECT")
        # The parts' own UVs.
        loops_uv = np.zeros((len(me.loops), 2))
        me.uv_layers.active.data.foreach_get("uv", loops_uv.ravel())
        for k, part in enumerate(self.parts):
            if part.uv is None:
                continue
            fids = np.nonzero(owner == k)[0]
            for fi, corner_uvs in zip(fids, part.uv):
                poly = me.polygons[fi]
                for li, uvc in zip(poly.loop_indices, corner_uvs):
                    loops_uv[li] = uvc
        me.uv_layers.active.data.foreach_set("uv", loops_uv.ravel())
        # One density for everything, then each part scaled by its weight; pack.
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.average_islands_scale()
        bpy.ops.object.mode_set(mode="OBJECT")
        me.uv_layers.active.data.foreach_get("uv", loops_uv.ravel())
        loop_owner = np.zeros(len(me.loops), int)
        for poly in me.polygons:
            loop_owner[poly.loop_start:poly.loop_start + poly.loop_total] = owner[poly.index]
        loop_face = np.zeros(len(me.loops), int)
        for poly in me.polygons:
            loop_face[poly.loop_start:poly.loop_start + poly.loop_total] = poly.index
        w = np.asarray(fw, float)[loop_face]
        loops_uv *= w[:, None]
        me.uv_layers.active.data.foreach_set("uv", loops_uv.ravel())
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.pack_islands(rotate=True, scale=True, margin=margin)
        bpy.ops.object.mode_set(mode="OBJECT")
        self.obj = obj
        self.offsets = offsets
        self.owner = owner
        return obj


def uv_stats(obj):
    """World metres per UV unit (sqrt of area ratio) and UV coverage."""
    me = obj.data
    me.calc_loop_triangles()
    uv = np.zeros((len(me.loops), 2))
    me.uv_layers.active.data.foreach_get("uv", uv.ravel())
    co = get_verts(obj)
    a3 = 0.0
    a2 = 0.0
    for t in me.loop_triangles:
        p = co[list(t.vertices)]
        q = uv[list(t.loops)]
        a3 += 0.5 * np.linalg.norm(np.cross(p[1] - p[0], p[2] - p[0]))
        a2 += 0.5 * abs((q[1, 0] - q[0, 0]) * (q[2, 1] - q[0, 1]) - (q[2, 0] - q[0, 0]) * (q[1, 1] - q[0, 1]))
    return {"world_per_uv": math.sqrt(a3 / max(a2, 1e-12)), "uv_coverage": round(a2, 3)}


def gbuffer_attrs(obj, offset=2.0):
    """Writes GPos (Blender position + offset) and GNrm (corner normal * 0.5 + 0.5) for the G-buffer bake."""
    me = obj.data
    co = get_verts(obj)
    set_attr(obj, "GPos", co + offset, kind="FLOAT_VECTOR")
    cn = np.zeros(len(me.loops) * 3, np.float32)
    me.corner_normals.foreach_get("vector", cn)
    set_attr(obj, "GNrm", cn.reshape(-1, 3) * 0.5 + 0.5, domain="CORNER", kind="FLOAT_VECTOR")


def texel_world(P, cover):
    """World size of one texel along u and v (metres), from the baked positions; island borders fall back to the
    median so seams do not distort the normal map."""
    du = np.zeros(P.shape[:2])
    dv = np.zeros(P.shape[:2])
    du[:, 1:-1] = np.linalg.norm(P[:, 2:] - P[:, :-2], axis=2) * 0.5
    dv[1:-1, :] = np.linalg.norm(P[2:, :] - P[:-2, :], axis=2) * 0.5
    med = np.median(du[cover]) if cover.any() else 1e-3
    for d in (du, dv):
        bad = (d <= 0) | (d > med * 6)
        d[bad] = med
    return du, dv


def height_normal(height, du, dv, strength=1.0):
    """Tangent-space normal from a texel-space height field (metres), with per-texel world sizes along u and v."""
    gx = np.zeros_like(height)
    gy = np.zeros_like(height)
    gx[:, 1:-1] = (height[:, 2:] - height[:, :-2]) * 0.5
    gy[1:-1, :] = (height[2:, :] - height[:-2, :]) * 0.5
    nx = -gx / du * strength
    ny = -gy / dv * strength
    n = np.stack([nx, ny, np.ones_like(height)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def bake_ao(obj, size, distance=0.03, samples=48):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = samples
    world = scene.world or bpy.data.worlds.new("BakeWorld")
    scene.world = world
    world.light_settings.distance = distance
    bake = scene.render.bake
    bake.use_selected_to_active = False
    bake.target = "IMAGE_TEXTURES"
    bake.margin = 8
    saved = list(obj.data.materials)
    mat = bpy.data.materials.new("AOBake")
    img = bpy.data.images.new("AO", size, size, alpha=False, float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = img
    mat.node_tree.nodes.active = node
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    C.activate(obj)
    bpy.ops.object.bake(type="AO")
    arr = np.array(img.pixels[:], np.float32).reshape(size, size, 4)[..., 0].copy()
    bpy.data.images.remove(img)
    obj.data.materials.clear()
    for m in saved:
        obj.data.materials.append(m)
    return arr
