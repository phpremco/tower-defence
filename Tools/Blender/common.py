"""Shared helpers for the headless Blender art pipeline.

Every asset script builds its model from Python only: a detailed "high" source made of overlapping parts that a
voxel remesh fuses into one continuous surface, a decimated "low" game mesh, textures baked from high to low, an
optional armature with generated weights and keyed actions, an FBX for Unity and preview renders.

Coordinates: Blender is Z-up. Creatures face -Y, which arrives as +Z (forward) in Unity with the export settings
below. Use G(x, up, forward) to write positions in the game's own terms.
"""

import json
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EXPORTS = os.path.join(REPO, "ArtSource", "Exports")
BLEND = os.path.join(REPO, "ArtSource", "Blend")
# Default detail-family order of an asset's Mask (B = first family, A = second): see surface.FAMILIES.
FAMILY_ORDER = ("chitin", "membrane", "crystal")

# Round 3 helpers import this module back; they only touch it at call time (EXPORTS is defined above).
import detailmaps  # noqa: E402
import surface  # noqa: E402
import uvlayout  # noqa: E402


def cli_args():
    """Arguments after `--` on the Blender command line, as a dict of --key value pairs."""
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = {}
    i = 0
    while i < len(argv):
        if argv[i].startswith("--"):
            key = argv[i][2:]
            if i + 1 < len(argv) and not argv[i + 1].startswith("--"):
                out[key] = argv[i + 1]
                i += 2
                continue
            out[key] = True
        i += 1
    return out


def preview_dir():
    path = cli_args().get("preview") or os.environ.get("TD_PREVIEW_DIR") or os.path.join(REPO, "ArtSource", "Previews")
    os.makedirs(path, exist_ok=True)
    return path


def G(x, up, forward):
    """Game-space position (x, up, forward) to Blender space."""
    return Vector((x, -forward, up))


def log(*parts):
    print("[td]", *parts, flush=True)


# ---------------------------------------------------------------------------------------------------------------
# Scene
# ---------------------------------------------------------------------------------------------------------------

def reset_scene(fps=30):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = fps
    return scene


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def activate(obj, select_only=True):
    if select_only:
        for o in bpy.context.scene.objects:
            if o is not None:
                o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def tri_count(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh = obj.evaluated_get(depsgraph).to_mesh()
    mesh.calc_loop_triangles()
    count = len(mesh.loop_triangles)
    obj.evaluated_get(depsgraph).to_mesh_clear()
    return count


# ---------------------------------------------------------------------------------------------------------------
# Part-based sculpting: parts are written into one bmesh, each face tagged with a part id
# ---------------------------------------------------------------------------------------------------------------

class Part:
    """One shape of a model. `frame` maps world points into the part's own space (for surface patterns);
    `axis` is the polyline of a tube, used to find how far along the tube a point lies."""

    def __init__(self, pid, name, kind, frame, bone=None, chain=None, axis=None, radii=None, style=None):
        self.pid = pid
        self.name = name
        self.kind = kind
        self.frame = frame
        self.bone = bone
        self.chain = chain or ([bone] if bone else [])
        self.axis = axis
        self.radii = radii
        self.style = style


class PartBuilder:
    def __init__(self):
        self.bm = bmesh.new()
        self.parts = []
        self.face_pid = []

    def _tag(self, faces, part):
        self.parts.append(part)
        self.face_pid.extend([part.pid] * len(faces))
        return part

    def ellipsoid(self, name, centre, radii, rot=None, bone=None, style=None, segments=(32, 16), deform=None):
        """An ellipsoid; `deform(local)` may move its unit-sphere vertices (in local space) before scaling."""
        rot = rot or Matrix.Identity(3)
        result = bmesh.ops.create_uvsphere(self.bm, u_segments=segments[0], v_segments=segments[1], radius=1.0)
        verts = result["verts"]
        for v in verts:
            local = Vector(v.co)
            if deform:
                local = deform(local)
            v.co = Vector(centre) + rot @ Vector((local.x * radii[0], local.y * radii[1], local.z * radii[2]))
        faces = list({f for v in verts for f in v.link_faces})
        frame = (Matrix.Translation(centre) @ rot.to_4x4() @ Matrix.Diagonal((radii[0], radii[1], radii[2], 1.0))).inverted()
        part = Part(len(self.parts), name, "ellipsoid", frame, bone=bone, radii=radii, style=style)
        return self._tag(faces, part)

    def tube(self, name, points, radii, sides=14, samples=6, chain=None, style=None, cap=0.7, flatten=1.0):
        """A tapered tube along a Catmull-Rom curve through `points`, radius interpolated from `radii`.
        `flatten` < 1 squashes the cross-section (flat mandibles, blade legs)."""
        curve = catmull_rom([Vector(p) for p in points], samples)
        rad = np.interp(np.linspace(0, 1, len(curve)), np.linspace(0, 1, len(radii)), radii)
        frames = parallel_frames(curve)
        rings = []
        for c, r, (t, n, b) in zip(curve, rad, frames):
            ring = []
            for s in range(sides):
                a = 2 * math.pi * s / sides
                offset = n * (math.cos(a) * r) + b * (math.sin(a) * r * flatten)
                ring.append(self.bm.verts.new(c + offset))
            rings.append(ring)
        faces = []
        for i in range(len(rings) - 1):
            for s in range(sides):
                s2 = (s + 1) % sides
                faces.append(self.bm.faces.new((rings[i][s], rings[i][s2], rings[i + 1][s2], rings[i + 1][s])))
        start_tip = self.bm.verts.new(curve[0] - frames[0][0] * rad[0] * cap)
        end_tip = self.bm.verts.new(curve[-1] + frames[-1][0] * rad[-1] * cap)
        for s in range(sides):
            s2 = (s + 1) % sides
            faces.append(self.bm.faces.new((rings[0][s2], rings[0][s], start_tip)))
            faces.append(self.bm.faces.new((rings[-1][s], rings[-1][s2], end_tip)))
        part = Part(len(self.parts), name, "tube", Matrix.Identity(4), chain=chain, axis=np.array([tuple(c) for c in curve]),
                    radii=rad, style=style)
        if chain:
            part.bone = chain[0]
        return self._tag(faces, part)

    def prism(self, name, base, direction, radius, length, tip, sides=6, twist=0.0, bone=None, style=None, taper=0.85):
        """A crystal: an n-sided prism along `direction` ending in a pyramid point. Local frame: z runs 0..1 from
        base to tip, x/y are the cross-section in units of `radius`."""
        d = Vector(direction).normalized()
        rot = d.to_track_quat("Z", "Y").to_matrix()
        base = Vector(base)
        bottom, top = [], []
        for s in range(sides):
            a = 2 * math.pi * s / sides + twist
            offset = Vector((math.cos(a), math.sin(a), 0))
            bottom.append(self.bm.verts.new(base + rot @ (offset * radius)))
            top.append(self.bm.verts.new(base + rot @ (offset * radius * taper + Vector((0, 0, length)))))
        apex = self.bm.verts.new(base + rot @ Vector((0, 0, length + tip)))
        faces = [self.bm.faces.new(list(reversed(bottom)))]
        for s in range(sides):
            s2 = (s + 1) % sides
            faces.append(self.bm.faces.new((bottom[s], bottom[s2], top[s2], top[s])))
            faces.append(self.bm.faces.new((top[s], top[s2], apex)))
        total = length + tip
        frame = (Matrix.Translation(base) @ rot.to_4x4() @ Matrix.Diagonal((radius, radius, total, 1.0))).inverted()
        part = Part(len(self.parts), name, "prism", frame, bone=bone, radii=(radius, length, tip), style=style)
        return self._tag(faces, part)

    def shard(self, name, base, direction, radius, length, sides=8, twist=0.0, flute=0.0, profile=None, bend=None,
              rings=10, bone=None, style=None, phase=0.0, chain=None, helix=None):
        """A Crysium shard: rings along `direction` whose radius follows `profile(t)` (t 0..1, default a needle),
        twisted by `twist` radians over the length; `flute` (0..1) pulls every second ring vertex in, giving a
        star cross-section whose ridges spiral. `bend` is an offset added with t^2 (a curving horn); `helix`
        (radius, turns, start angle) winds the axis around the base line, shrinking towards the tip. Ends in a point.
        Local frame like prism: z runs 0..1 from base to tip, x/y in units of `radius`."""
        d = Vector(direction).normalized()
        rot = d.to_track_quat("Z", "Y").to_matrix()
        base = Vector(base)
        bend = Vector(bend) if bend is not None else Vector((0, 0, 0))
        profile = profile or (lambda t: (1.0 - t) ** 0.8 * (0.75 + 0.25 * min(1.0, t * 8)))

        def centre(t):
            c = base + rot @ Vector((0, 0, length * t)) + bend * (t * t)
            if helix:
                hr, turns, start = helix
                a = start + 2 * math.pi * turns * t
                c = c + rot @ Vector((math.cos(a), math.sin(a), 0)) * hr * (1 - 0.7 * t)
            return c

        ring_verts = []
        for i in range(rings):
            t = i / rings
            r = radius * profile(t)
            c = centre(t)
            ring = []
            for s in range(sides):
                a = 2 * math.pi * s / sides + twist * t + phase
                k = 1.0 - flute * (s % 2)
                ring.append(self.bm.verts.new(c + rot @ Vector((math.cos(a) * r * k, math.sin(a) * r * k, 0))))
            ring_verts.append(ring)
        bottom = self.bm.verts.new(centre(0.0))
        apex = self.bm.verts.new(centre(1.0))
        faces = []
        for s in range(sides):
            s2 = (s + 1) % sides
            faces.append(self.bm.faces.new((ring_verts[0][s2], ring_verts[0][s], bottom)))
            for i in range(rings - 1):
                faces.append(self.bm.faces.new((ring_verts[i][s], ring_verts[i][s2], ring_verts[i + 1][s2], ring_verts[i + 1][s])))
            faces.append(self.bm.faces.new((ring_verts[-1][s], ring_verts[-1][s2], apex)))
        frame = (Matrix.Translation(base) @ rot.to_4x4() @ Matrix.Diagonal((radius, radius, length, 1.0))).inverted()
        part = Part(len(self.parts), name, "shard", frame, bone=bone, chain=chain, radii=(radius, length, 0.0), style=style)
        # Tube-style axis as well, so leg and tube helpers (tube_param) also work on a shard.
        part.axis = np.array([tuple(centre(t)) for t in np.linspace(0, 1, 12)])
        part.radii = np.array([radius * profile(t) for t in np.linspace(0, 1, 12)])
        return self._tag(faces, part)

    def slab(self, name, outer, inner, nu, nv, frame=None, bone=None, chain=None, style=None, transform=None):
        """A closed thick plate between two parametric surfaces outer(u, v) and inner(u, v), u and v in 0..1
        (shell bands, armour plates of any outline). `transform(p)` maps each point afterwards (e.g. from the
        rolled-up pose a plate was designed in to the rest pose). Local frame: `frame` (world to the space the
        style wants; default identity)."""
        tf = transform or (lambda p: p)
        grids = []
        for fn in (outer, inner):
            grids.append([[self.bm.verts.new(tf(Vector(fn(i / nu, j / nv)))) for j in range(nv + 1)] for i in range(nu + 1)])
        out_g, in_g = grids
        faces = []
        for i in range(nu):
            for j in range(nv):
                faces.append(self.bm.faces.new((out_g[i][j], out_g[i + 1][j], out_g[i + 1][j + 1], out_g[i][j + 1])))
                faces.append(self.bm.faces.new((in_g[i][j + 1], in_g[i + 1][j + 1], in_g[i + 1][j], in_g[i][j])))
        for i in range(nu):
            faces.append(self.bm.faces.new((out_g[i + 1][0], out_g[i][0], in_g[i][0], in_g[i + 1][0])))
            faces.append(self.bm.faces.new((out_g[i][nv], out_g[i + 1][nv], in_g[i + 1][nv], in_g[i][nv])))
        for j in range(nv):
            faces.append(self.bm.faces.new((out_g[0][j], out_g[0][j + 1], in_g[0][j + 1], in_g[0][j])))
            faces.append(self.bm.faces.new((out_g[nu][j + 1], out_g[nu][j], in_g[nu][j], in_g[nu][j + 1])))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        part = Part(len(self.parts), name, "slab", frame or Matrix.Identity(4), bone=bone, chain=chain, style=style)
        if chain:
            part.bone = chain[0]
        return self._tag(faces, part)

    def ring(self, name, centre, normal, radius, tube_radius, arcs=3, sides=8, samples=4, chain=None, style=None,
             profile=None, start=0.0, overlap=0.06, ref=None):
        """A closed ring (a torus along the circle of `radius` around `normal`), built as `arcs` overlapping tube
        arcs that the remesh fuses: the arc boundaries become UV seams, so the ring unrolls without folding.
        `profile(theta)` (0..2pi) scales the tube radius (beads, nodes); `start` turns where the arcs begin; theta 0
        lies along `ref` (projected into the ring plane; default any axis), so patterns can be placed symmetrically."""
        n = Vector(normal).normalized()
        a = (Vector(ref) - n * n.dot(Vector(ref))).normalized() if ref is not None else n.orthogonal().normalized()
        b = n.cross(a)
        centre = Vector(centre)
        profile = profile or (lambda th: 1.0)
        parts = []
        span = 2 * math.pi / arcs
        steps = 8
        for i in range(arcs):
            th0 = start + i * span - overlap
            ths = [th0 + (span + 2 * overlap) * j / steps for j in range(steps + 1)]
            pts = [centre + (a * math.cos(th) + b * math.sin(th)) * radius for th in ths]
            radii = [tube_radius * profile(th % (2 * math.pi)) for th in ths]
            parts.append(self.tube(name, pts, radii, sides=sides, samples=samples, chain=chain, style=style, cap=0.3))
        return parts

    def to_object(self, name):
        mesh = bpy.data.meshes.new(name)
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        obj = link(bpy.data.objects.new(name, mesh))
        return obj

    def bvh(self):
        self.bm.faces.ensure_lookup_table()
        return BVHTree.FromBMesh(self.bm)


def chiselled(count, seed, depth=0.15, squash=None):
    """Deform for PartBuilder.ellipsoid: turns the sphere into a random convex polyhedron (the intersection of
    `count` planes), i.e. a hewn low-poly rock. The remesh afterwards rounds the edges into small bevels."""
    rng = np.random.default_rng(seed)
    dirs = fibonacci_sphere(count, seed)
    dirs /= np.linalg.norm(dirs, axis=1, keepdims=True)
    heights = 1.0 - rng.uniform(0, depth, count)
    if squash is not None:
        heights = heights * np.interp(dirs[:, 2], (-1, 1), squash)

    def deform(v):
        u = np.array((v.x, v.y, v.z))
        dots = dirs @ u
        ok = dots > 1e-3
        r = np.min(heights[ok] / dots[ok]) if ok.any() else 1.0
        return Vector(u * min(r, 1.6))
    return deform


def catmull_rom(points, samples):
    if len(points) == 2:
        return [points[0].lerp(points[1], i / samples) for i in range(samples + 1)]
    pts = [points[0] * 2 - points[1]] + points + [points[-1] * 2 - points[-2]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(samples):
            t = s / samples
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(points[-1])
    return out


def parallel_frames(curve):
    tangents = []
    for i in range(len(curve)):
        a = curve[max(i - 1, 0)]
        b = curve[min(i + 1, len(curve) - 1)]
        tangents.append((b - a).normalized())
    up = Vector((0, 0, 1)) if abs(tangents[0].z) < 0.9 else Vector((1, 0, 0))
    n = tangents[0].cross(up).normalized()
    frames = []
    for t in tangents:
        n = (n - t * n.dot(t)).normalized()
        frames.append((t, n, t.cross(n).normalized()))
    return frames


# ---------------------------------------------------------------------------------------------------------------
# Remesh, surface detail, decimation
# ---------------------------------------------------------------------------------------------------------------

def fuse(obj, voxel, smooth_iterations=6):
    """Voxel remesh `obj` into one closed continuous surface and smooth out the voxel steps."""
    remesh = obj.modifiers.new("Remesh", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = voxel
    remesh.adaptivity = 0.0
    smooth = obj.modifiers.new("Smooth", "SMOOTH")
    smooth.factor = 0.6
    smooth.iterations = smooth_iterations
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph))
    obj.modifiers.clear()
    return mesh


def mesh_arrays(mesh):
    n = len(mesh.vertices)
    co = np.empty(n * 3, dtype=np.float64)
    mesh.vertices.foreach_get("co", co)
    nor = np.empty(n * 3, dtype=np.float64)
    mesh.vertex_normals.foreach_get("vector", nor)
    return co.reshape(-1, 3), nor.reshape(-1, 3)


def assign_parts(points, builder):
    """Part id of each point: the part whose source surface is nearest."""
    tree = builder.bvh()
    face_pid = builder.face_pid
    pid = np.empty(len(points), dtype=np.int32)
    for i, p in enumerate(points):
        hit = tree.find_nearest(Vector(p))
        pid[i] = face_pid[hit[2]] if hit[2] is not None else 0
    return pid


def tube_param(part, points):
    """For points near a tube: (t along the tube 0..1, radius there, distance from the axis)."""
    axis = part.axis
    seg_a = axis[:-1]
    seg_b = axis[1:]
    ab = seg_b - seg_a
    lengths = np.linalg.norm(ab, axis=1)
    cum = np.concatenate([[0], np.cumsum(lengths)])
    total = cum[-1]
    best_d = np.full(len(points), np.inf)
    best_t = np.zeros(len(points))
    for i in range(len(seg_a)):
        ap = points - seg_a[i]
        s = np.clip((ap @ ab[i]) / max(lengths[i] ** 2, 1e-12), 0, 1)
        proj = seg_a[i] + s[:, None] * ab[i]
        d = np.linalg.norm(points - proj, axis=1)
        better = d < best_d
        best_d[better] = d[better]
        best_t[better] = (cum[i] + s[better] * lengths[i]) / total
    radius = np.interp(best_t, np.linspace(0, 1, len(part.radii)), part.radii)
    return best_t, radius, best_d


def local_points(part, points):
    homo = np.c_[points, np.ones(len(points))]
    m = np.array(part.frame)
    return (homo @ m.T)[:, :3]


class Surface:
    """Per-vertex outputs a style writes into: displacement along the normal, albedo, emission and a ramp of
    roughness. Styles are functions style(ctx, idx) that read ctx.p / ctx.n / ctx.local and fill these arrays."""

    def __init__(self, points, normals):
        n = len(points)
        self.p = points
        self.n = normals
        self.disp = np.zeros(n)
        self.albedo = np.ones((n, 3)) * 0.5
        self.emit = np.zeros((n, 3))
        self.rough = np.full(n, 0.5)
        # Round 3: material family weights (chitin, membrane, crystal, rock) and the growth-line coordinate in
        # world units; surface.py turns them into per-pixel micro-detail at bake time.
        self.fam = np.zeros((n, 4))
        self.flow = np.zeros(n)


def smooth_borders(mesh, pid, surface, iterations):
    """Averages the style outputs across part borders (a band two edge rings wide) `iterations` times. On a fused
    surface the nearest-part assignment zigzags along a crease by a voxel; hard-surface assets (plates on cloth) show
    that as a jagged fringe in close-ups, this turns it into a clean line."""
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", edges)
    edges = edges.reshape(-1, 2)
    band = np.zeros(len(pid), dtype=bool)
    cross = pid[edges[:, 0]] != pid[edges[:, 1]]
    band[edges[cross].ravel()] = True
    for _ in range(2):
        grow = band.copy()
        grow[edges[band[edges[:, 0]], 1]] = True
        grow[edges[band[edges[:, 1]], 0]] = True
        band = grow
    count = np.bincount(edges.ravel(), minlength=len(pid)).astype(float)
    for name in ("albedo", "emit", "fam"):
        arr = getattr(surface, name)
        for _ in range(iterations):
            acc = np.zeros_like(arr)
            np.add.at(acc, edges[:, 0], arr[edges[:, 1]])
            np.add.at(acc, edges[:, 1], arr[edges[:, 0]])
            mean = acc / np.maximum(count, 1)[:, None]
            arr = np.where(band[:, None], 0.5 * arr + 0.5 * mean, arr)
        setattr(surface, name, arr)
    for name in ("rough", "disp"):
        arr = getattr(surface, name)
        for _ in range(iterations):
            acc = np.zeros_like(arr)
            np.add.at(acc, edges[:, 0], arr[edges[:, 1]])
            np.add.at(acc, edges[:, 1], arr[edges[:, 0]])
            arr = np.where(band, 0.5 * arr + 0.5 * acc / np.maximum(count, 1), arr)
        setattr(surface, name, arr)


def detail(high_mesh, builder, extra=None, border_smooth=0):
    """Runs each part's style over the vertices it owns, displaces the mesh and writes colour attributes.
    `border_smooth` > 0 blends the styles across part borders (see smooth_borders); 0 keeps the round-3 behaviour."""
    points, normals = mesh_arrays(high_mesh)
    pid = assign_parts(points, builder)
    surface = Surface(points, normals)
    for part in builder.parts:
        idx = np.nonzero(pid == part.pid)[0]
        if len(idx) == 0 or part.style is None:
            continue
        part.style(surface, idx, part)
    if border_smooth:
        smooth_borders(high_mesh, pid, surface, border_smooth)
    if extra:
        extra(surface, pid)
    moved = points + normals * surface.disp[:, None]
    high_mesh.vertices.foreach_set("co", moved.ravel())
    high_mesh.update()
    # Styles pick colours as in a paint program (sRGB); the bake works in linear light. The emission map is a
    # 0..1 mask in the glow colour, the material's emission strength sets how hot it burns.
    write_color(high_mesh, "Albedo", srgb_to_linear(surface.albedo))
    write_color(high_mesh, "Emit", srgb_to_linear(np.clip(surface.emit, 0, 1)))
    write_color(high_mesh, "Rough", np.repeat(surface.rough[:, None], 3, axis=1))
    write_rgba(high_mesh, "Fam", np.clip(surface.fam, 0, 1))
    flow = high_mesh.attributes.get("Flow") or high_mesh.attributes.new("Flow", "FLOAT", "POINT")
    flow.data.foreach_set("value", surface.flow.astype(np.float32))
    return pid


def srgb_to_linear(c):
    c = np.clip(c, 0, None)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def write_color(mesh, name, rgb):
    attr = mesh.color_attributes.get(name) or mesh.color_attributes.new(name, "FLOAT_COLOR", "POINT")
    rgba = np.c_[rgb, np.ones(len(rgb))].astype(np.float32)
    attr.data.foreach_set("color", rgba.ravel())


def write_rgba(mesh, name, rgba):
    attr = mesh.color_attributes.get(name) or mesh.color_attributes.new(name, "FLOAT_COLOR", "POINT")
    attr.data.foreach_set("color", rgba.astype(np.float32).ravel())


def axis_length(part):
    return float(np.linalg.norm(np.diff(part.axis, axis=0), axis=1).sum())


def decimate(mesh, target_tris, name, symmetric=True):
    obj = link(bpy.data.objects.new(name + "_tmp", mesh))
    mesh.calc_loop_triangles()
    ratio = min(1.0, target_tris / max(len(mesh.loop_triangles), 1))
    mod = obj.modifiers.new("Decimate", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = ratio
    mod.use_symmetry = symmetric
    mod.symmetry_axis = "X"
    mod.use_collapse_triangulate = True
    depsgraph = bpy.context.evaluated_depsgraph_get()
    low = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph))
    low.name = name
    bpy.data.objects.remove(obj)
    return low


def symmetrize(mesh):
    """Makes the game mesh exactly mirror-symmetric across x = 0 (the +X half is mirrored over), so every left face
    has a twin for the mirrored UVs. The symmetric decimation alone leaves about 15 % of the vertices off by a
    fraction of a millimetre."""
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.symmetrize(bm, input=bm.verts[:] + bm.edges[:] + bm.faces[:], direction="X", dist=1e-5)
    # The cut along x = 0 leaves slivers and doubled faces: weld, collapse edges below a fraction of a
    # millimetre of model size, and drop faces that end up sharing all their vertices with another.
    size = max(max(v.co[i] for v in bm.verts) - min(v.co[i] for v in bm.verts) for i in range(3))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=size * 1e-4)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=size * 1.5e-3)
    seen, doubles = set(), []
    for f in bm.faces:
        key = frozenset(v.index for v in f.verts)
        if key in seen:
            doubles.append(f)
        seen.add(key)
    bmesh.ops.delete(bm, geom=doubles, context="FACES_ONLY")
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3])
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def shade_smooth(mesh):
    mesh.shade_smooth()


# ---------------------------------------------------------------------------------------------------------------
# Numpy pattern helpers for styles
# ---------------------------------------------------------------------------------------------------------------

def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def fibonacci_sphere(count, seed=0):
    rng = np.random.default_rng(seed)
    i = np.arange(count) + 0.5
    phi = np.arccos(1 - 2 * i / count)
    theta = math.pi * (1 + 5 ** 0.5) * i + rng.uniform(0, 0.4, count)
    pts = np.c_[np.cos(theta) * np.sin(phi), np.sin(theta) * np.sin(phi), np.cos(phi)]
    return pts + rng.normal(0, 0.06, pts.shape)


def voronoi_edge(points, seeds):
    """Half the gap between nearest and second-nearest seed: 0 on a cell border, growing towards the centre."""
    d1 = np.full(len(points), np.inf)
    d2 = np.full(len(points), np.inf)
    for s in seeds:
        d = np.linalg.norm(points - s, axis=1)
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d))
        d1 = np.where(closer, d, d1)
    return (d2 - d1) * 0.5


def value_noise(points, scale, seed=0):
    """Cheap smooth 3D noise in [-1, 1] (sum of random sines) for colour mottling and grain."""
    rng = np.random.default_rng(seed)
    out = np.zeros(len(points))
    for octave in range(4):
        freq = scale * (2.0 ** octave)
        for _ in range(3):
            direction = rng.normal(size=3)
            direction /= np.linalg.norm(direction)
            phase = rng.uniform(0, 2 * math.pi)
            out += np.sin(points @ direction * freq + phase) / (2.0 ** octave)
    return out / 3.5


def lerp3(a, b, t):
    a = np.asarray(a, dtype=float)
    b = np.asarray(b, dtype=float)
    t = np.asarray(t, dtype=float)
    if t.ndim == 1:
        t = t[:, None]
    return a + (b - a) * t


def chitin_gradient(n, top, side, belly):
    """Light top, mid sides, dark underside: the look that lifts creatures off a dark floor."""
    up = n[:, 2]
    upper = smoothstep(-0.1, 0.8, up)
    lower = smoothstep(-0.9, -0.1, up)
    col = lerp3(belly, side, lower)
    return lerp3(col, top, upper)


# ---------------------------------------------------------------------------------------------------------------
# Materials
# ---------------------------------------------------------------------------------------------------------------

def new_material(name):
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except Exception:
        pass
    return mat


def principled(name, color=(0.5, 0.5, 0.5), rough=0.5, metal=0.0, emission=None, strength=0.0):
    mat = new_material(name)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1)
        bsdf.inputs["Emission Strength"].default_value = strength
    return mat


_CHANNELS = {}

# Albedo response to the micro-detail: grime darkens pores and crevices, worn/chipped spots get lighter.
CAVITY_DARKEN = 0.38
WEAR_LIGHTEN = 0.5
CAVITY_AO = 0.45
DUST = (0.15, 0.145, 0.14)


def high_bake_material(edge_gain=0.35, k=1.0, detail_params=None, detail_strength=1.0, families=FAMILY_ORDER,
                       family_set=None):
    """Material on the detailed source. Its output is switched per bake (set_high_channel): emission of the final
    albedo (vertex albedo x baked AO x edge wear x micro cavity/wear), the glow, AO, roughness and the family mask,
    or a diffuse BSDF whose normal carries the micro-detail bump for the normal bake."""
    mat = new_material("HighBake")
    nt = mat.node_tree
    nt.nodes.clear()
    g = surface.Graph(nt)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emission = nt.nodes.new("ShaderNodeEmission")
    albedo = g.attr("Albedo")
    glow = g.attr("Emit")
    ao = g.attr("AO")
    rough_attr = g.attr("Rough")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    # Edge wear from mesh convexity: rims and ridges get lighter, which reads as worn chitin at a glance.
    edge = g.map(geo.outputs["Pointiness"], 0.5, 0.58, 1.0, 1.0 + edge_gain, smooth=False)
    ao_curve = g.map(ao.outputs["Color"], 0.0, 1.0, 0.25, 1.0, smooth=False)
    base = g.vscale(g.vscale(albedo.outputs["Color"], ao_curve), edge)

    family_set = family_set or surface.FAMILIES
    d = surface.build(g, k, detail_params, detail_strength, family_set)
    tone = g.mul(g.inv(g.mul(d.cavity, CAVITY_DARKEN)), g.add(1.0, g.mul(d.wear, WEAR_LIGHTEN)))
    col = g.vscale(base, tone)
    # Rock: dust settles in the crevices (families without rock get none here; they paint their own dust).
    rock_w = d.families[family_set.index("rock")] if "rock" in family_set else 0.0
    dust_f = g.mul(g.mul(d.cavity, rock_w), 0.55)
    mix = g.node("ShaderNodeMix", data_type="RGBA", clamp_factor=True)
    g.set(mix.inputs["Factor"], dust_f)
    g.set(mix.inputs[6], col)
    mix.inputs[7].default_value = (*DUST, 1.0)
    final_albedo = mix.outputs[2]

    final_emit = g.vscale(glow.outputs["Color"], d.emit)
    final_ao = g.vscale(ao.outputs["Color"], g.inv(g.mul(d.cavity, CAVITY_AO)))
    # Sheen only on the edges: raised rims and ridges get a lower roughness than the matte plates around them.
    sheen = g.map(geo.outputs["Pointiness"], 0.5, 0.6, 1.0, 0.55, smooth=False)
    rough = g.math("ADD", g.mul(rough_attr.outputs["Factor"], sheen), d.rough, clamp=True)
    rough = g.math("MAXIMUM", rough, 0.08)
    fam = g.node("ShaderNodeCombineColor")
    index = {name: i for i, name in enumerate(family_set)}
    for slot, name in enumerate(families[:3]):
        g.set(fam.inputs[slot], d.families[index[name]])

    bump = g.node("ShaderNodeBump")
    g.set(bump.inputs["Height"], d.height)
    bump.inputs["Distance"].default_value = 1.0
    bump.inputs["Strength"].default_value = 1.0
    diffuse = nt.nodes.new("ShaderNodeBsdfDiffuse")
    nt.links.new(bump.outputs["Normal"], diffuse.inputs["Normal"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])
    _CHANNELS[mat.name] = {"albedo": final_albedo, "emission": final_emit, "ao": final_ao, "rough": rough,
                           "fam": fam.outputs[0], "normal": diffuse.outputs[0], "_emission": emission, "_out": out}
    return mat


def set_high_channel(mat, channel):
    ch = _CHANNELS[mat.name]
    nt = mat.node_tree
    out, emission = ch["_out"], ch["_emission"]
    for link_ in list(out.inputs["Surface"].links) + list(emission.inputs["Color"].links):
        nt.links.remove(link_)
    if channel == "normal":
        nt.links.new(ch["normal"], out.inputs["Surface"])
        return
    nt.links.new(ch[channel], emission.inputs["Color"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])


def _image_node(nt, path, non_color=False, packed=False):
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path, check_existing=True)
    if non_color:
        tex.image.colorspace_settings.name = "Non-Color"
    if packed:
        tex.image.alpha_mode = "CHANNEL_PACKED"
    return tex


def _decode_normal(g, color, strength=None, mask=None):
    """Tangent normal from a texture colour; with `strength`/`mask` it is faded towards flat (0, 0, 1)."""
    n = g.vec("MULTIPLY_ADD", color, (2.0, 2.0, 2.0))
    g.set(n.inputs[2], (-1.0, -1.0, -1.0))
    v = n.outputs[0]
    if strength is None:
        return v
    s = g.mul(mask, strength) if mask is not None else strength
    flat = (0.0, 0.0, 1.0)
    delta = g.vec("SUBTRACT", v, flat).outputs[0]
    return g.vec("ADD", flat, g.vscale(delta, s)).outputs[0]


def game_material(name, textures, emission_strength=2.0, specular=0.4, details=()):
    """Preview stand-in for Game/UnitLit with the round-3 slots: Albedo, Normal, Emission and the packed Mask
    (R AO, G smoothness, B primary detail mask, A secondary detail mask), plus up to two tiling detail normals
    combined with whiteout blending. `details`: [(detail normal path, tiling, strength, mask channel 'B' or 'A')]."""
    mat = new_material(name)
    nt = mat.node_tree
    g = surface.Graph(nt)
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = specular
    albedo = _image_node(nt, textures["albedo"])
    nt.links.new(albedo.outputs["Color"], bsdf.inputs["Base Color"])
    mask = _image_node(nt, textures["mask"], non_color=True, packed=True)
    channels = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], channels.inputs[0])
    nt.links.new(g.inv(channels.outputs[1]), bsdf.inputs["Roughness"])
    emit = _image_node(nt, textures["emission"])
    nt.links.new(emit.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = emission_strength
    base = _decode_normal(g, _image_node(nt, textures["normal"], non_color=True).outputs["Color"])
    uv = nt.nodes.new("ShaderNodeTexCoord").outputs["UV"]
    for path, tiling, strength, channel in details:
        tex = _image_node(nt, path, non_color=True)
        tex["detail_path"] = path
        g.set(tex.inputs["Vector"], g.mapping(uv, scale=(tiling, tiling, 1.0)))
        weight = channels.outputs[2] if channel == "B" else mask.outputs["Alpha"]
        dn = _decode_normal(g, tex.outputs["Color"], strength, weight)
        bx = g.node("ShaderNodeSeparateXYZ"); g.set(bx.inputs[0], base)
        dx = g.node("ShaderNodeSeparateXYZ"); g.set(dx.inputs[0], dn)
        comb = g.node("ShaderNodeCombineXYZ")
        g.set(comb.inputs[0], g.add(bx.outputs[0], dx.outputs[0]))
        g.set(comb.inputs[1], g.add(bx.outputs[1], dx.outputs[1]))
        g.set(comb.inputs[2], g.mul(bx.outputs[2], dx.outputs[2]))
        base = g.vec("NORMALIZE", comb.outputs[0]).outputs[0]
    enc = g.vec("MULTIPLY_ADD", base, (0.5, 0.5, 0.5))
    g.set(enc.inputs[2], (0.5, 0.5, 0.5))
    nmap = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(enc.outputs[0], nmap.inputs["Color"])
    nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


# ---------------------------------------------------------------------------------------------------------------
# Baking
# ---------------------------------------------------------------------------------------------------------------

def _cycles(samples):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False


def bake_vertex_ao(high, distance, samples=48):
    """Ambient occlusion of the source on its own, stored in its vertices (cheap, and crisp in the cavities)."""
    _cycles(samples)
    world = bpy.context.scene.world or bpy.data.worlds.new("BakeWorld")
    bpy.context.scene.world = world
    world.light_settings.distance = distance
    if not high.data.color_attributes.get("AO"):
        high.data.color_attributes.new("AO", "FLOAT_COLOR", "POINT")
    high.data.color_attributes.active_color = high.data.color_attributes["AO"]
    others = [(o, o.hide_render) for o in bpy.context.scene.objects if o != high]
    for o, _ in others:
        o.hide_render = True
    activate(high)
    bake = bpy.context.scene.render.bake
    bake.target = "VERTEX_COLORS"
    bake.use_selected_to_active = False
    bpy.ops.object.bake(type="AO")
    bake.target = "IMAGE_TEXTURES"
    for o, was_hidden in others:
        o.hide_render = was_hidden


def _bake_target(low, bake_faces):
    """The object the bake writes through. With mirrored UVs it is a copy of the game mesh holding only the
    unwrapped half, so the mirror twins (same UVs) cannot overwrite it with the other side's detail."""
    if bake_faces is None:
        return low, False
    target = link(low.copy())
    target.data = low.data.copy()
    target.modifiers.clear()
    keep = set(bake_faces)
    bm = bmesh.new()
    bm.from_mesh(target.data)
    bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index not in keep], context="FACES")
    bm.to_mesh(target.data)
    bm.free()
    return target, True


def _bake_one(kind, size, space, prefix, suffix, node, mat_high=None, channel=None):
    image = bpy.data.images.new(f"{prefix}_{suffix}", size, size, alpha=False, float_buffer=True)
    image.colorspace_settings.name = space
    node.image = image
    if kind == "NORMAL":
        set_high_channel(mat_high, "normal")
        bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")
    else:
        set_high_channel(mat_high, channel)
        bpy.ops.object.bake(type="EMIT")
    w, h = image.size
    arr = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(image)
    return arr


def _save(arr, path, colorspace):
    """8-bit PNG. Colour maps are baked in linear light and stored sRGB like a paint program would."""
    out = arr.copy()
    if colorspace == "sRGB":
        out[..., :3] = linear_to_srgb(out[..., :3])
    array_to_image(out, path, colorspace=colorspace)
    log("baked", path)
    return path


def linear_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def downsample(arr, factor, normal=False):
    """Box filter by an integer factor (Low profile maps); normals are renormalised."""
    h, w = arr.shape[:2]
    out = arr.reshape(h // factor, factor, w // factor, factor, arr.shape[2]).mean(axis=(1, 3))
    if normal:
        n = out[..., :3] * 2 - 1
        n /= np.maximum(np.linalg.norm(n, axis=2, keepdims=True), 1e-6)
        out[..., :3] = n * 0.5 + 0.5
    return out


def bake_maps(high, low, out_dir, prefix, size=1024, extrusion=0.02, ray=0.05, samples=8, bake_faces=None,
              low_size=None):
    """Bakes the detailed source onto the game mesh and writes four maps per asset:
    Albedo (sRGB), Normal (tangent), Emission (sRGB) and Mask (linear: R AO, G smoothness, B primary detail mask,
    A secondary detail mask). `low_size` also writes the same four maps at that size into out_dir/Low."""
    os.makedirs(out_dir, exist_ok=True)
    for stale in ("AO", "Roughness"):  # round 2 files, now packed into the Mask
        old = os.path.join(out_dir, f"{prefix}_{stale}.png")
        if os.path.exists(old):
            os.remove(old)
    _cycles(samples)
    mat_high = high.data.materials[0]
    target, temporary = _bake_target(low, bake_faces)
    bake_mat = new_material(prefix + "_BakeTarget")
    saved_mats = list(target.data.materials)
    target.data.materials.clear()
    target.data.materials.append(bake_mat)
    node = bake_mat.node_tree.nodes.new("ShaderNodeTexImage")
    bake_mat.node_tree.nodes.active = node
    bake = bpy.context.scene.render.bake
    bake.use_selected_to_active = True
    bake.cage_extrusion = extrusion
    bake.max_ray_distance = ray
    bake.margin = max(4, size // 128)
    bake.use_clear = True
    bake.target = "IMAGE_TEXTURES"
    for o in bpy.context.scene.objects:
        if o is not None:
            o.select_set(False)
    high.select_set(True)
    target.select_set(True)
    bpy.context.view_layer.objects.active = target

    maps = {
        "albedo": _bake_one("EMIT", size, "Linear Rec.709", prefix, "Albedo", node, mat_high, "albedo"),
        "emission": _bake_one("EMIT", size, "Linear Rec.709", prefix, "Emission", node, mat_high, "emission"),
        "normal": _bake_one("NORMAL", size, "Non-Color", prefix, "Normal", node, mat_high),
    }
    ao = _bake_one("EMIT", size, "Non-Color", prefix, "AO", node, mat_high, "ao")
    rough = _bake_one("EMIT", size, "Non-Color", prefix, "Rough", node, mat_high, "rough")
    fam = _bake_one("EMIT", size, "Non-Color", prefix, "Fam", node, mat_high, "fam")
    mask = np.dstack([ao[..., 0], 1.0 - rough[..., 0], fam[..., 0], fam[..., 1]]).astype(np.float32)
    maps["mask"] = np.clip(mask, 0, 1)

    spaces = {"albedo": "sRGB", "emission": "sRGB", "normal": "Non-Color", "mask": "Non-Color"}
    suffixes = {"albedo": "Albedo", "emission": "Emission", "normal": "Normal", "mask": "Mask"}
    paths = {}
    for key, arr in maps.items():
        paths[key] = _save(arr, os.path.join(out_dir, f"{prefix}_{suffixes[key]}.png"), spaces[key])
    if low_size:
        low_dir = os.path.join(out_dir, "Low")
        os.makedirs(low_dir, exist_ok=True)
        for key, arr in maps.items():
            small = downsample(arr, size // low_size, normal=key == "normal")
            _save(small, os.path.join(low_dir, f"{prefix}_{suffixes[key]}.png"), spaces[key])

    if temporary:
        bpy.data.objects.remove(target)
    else:
        target.data.materials.clear()
        for m in saved_mats:
            target.data.materials.append(m)
    return paths


# ---------------------------------------------------------------------------------------------------------------
# Armature, weights and animation
# ---------------------------------------------------------------------------------------------------------------

def build_armature(name, bones):
    """bones: list of (name, head, tail, parent or None, deform)."""
    data = bpy.data.armatures.new(name)
    arm = link(bpy.data.objects.new(name, data))
    activate(arm)
    bpy.ops.object.mode_set(mode="EDIT")
    for bname, head, tail, parent, deform in bones:
        eb = data.edit_bones.new(bname)
        eb.head = head
        eb.tail = tail
        eb.use_deform = deform
        eb.roll = 0.0
        if parent:
            eb.parent = data.edit_bones[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return arm


def segment_distance(points, a, b):
    ab = b - a
    t = np.clip(((points - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
    return np.linalg.norm(points - (a + t[:, None] * ab), axis=1)


def blend_weights(mesh, weights, select, iterations, max_influences=4):
    """Averages the bone weights of the `select`ed vertices with their mesh neighbours `iterations` times, so rigid
    parts meet in a narrow soft crease instead of a hard tear (the stretch then happens only there)."""
    if iterations <= 0 or not select.any():
        return weights
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", edges)
    edges = edges.reshape(-1, 2)
    names = list(weights)
    w = np.stack([weights[n] for n in names], axis=1)
    count = np.bincount(edges.ravel(), minlength=len(w)).astype(float)[:, None]
    for _ in range(iterations):
        acc = np.zeros_like(w)
        np.add.at(acc, edges[:, 0], w[edges[:, 1]])
        np.add.at(acc, edges[:, 1], w[edges[:, 0]])
        mean = acc / np.maximum(count, 1)
        w = np.where(select[:, None], 0.5 * w + 0.5 * mean, w)
    if w.shape[1] > max_influences:
        cut = -np.sort(-w, axis=1)[:, max_influences - 1:max_influences]
        w = np.where(w >= cut, w, 0.0)
    w /= np.maximum(w.sum(axis=1, keepdims=True), 1e-9)
    return {n: w[:, i] for i, n in enumerate(names)}


def skin(mesh_obj, arm, builder, extra_candidates=None, power=4.0, max_influences=4, rigid=(), blend=0):
    """Part-aware distance weights: a vertex can only follow the bones of the part it belongs to (plus that chain's
    parent, so joints blend into the body). This keeps legs from dragging the belly, which bone heat often gets wrong
    on fused meshes. Parts named in `rigid` follow their first bone only (armour plates that must not bend); their
    vertices are then blended `blend` edge rings into their neighbours, so the plates meet in a soft crease."""
    mesh = mesh_obj.data
    points, _ = mesh_arrays(mesh)
    pid = assign_parts(points, builder)
    bones = {b.name: b for b in arm.data.bones}
    heads = {n: np.array(b.head_local) for n, b in bones.items()}
    tails = {n: np.array(b.tail_local) for n, b in bones.items()}
    for vg in list(mesh_obj.vertex_groups):
        mesh_obj.vertex_groups.remove(vg)
    groups = {n: mesh_obj.vertex_groups.new(name=n) for n, b in bones.items() if b.use_deform}
    weights = {n: np.zeros(len(points)) for n in groups}
    rigid_select = np.zeros(len(points), dtype=bool)
    for part in builder.parts:
        idx = np.nonzero(pid == part.pid)[0]
        if len(idx) == 0:
            continue
        if part.name in rigid:
            weights[part.chain[0]][idx] = 1.0
            rigid_select[idx] = True
            continue
        candidates = list(part.chain)
        first = bones[candidates[0]]
        if first.parent and first.parent.name in groups:
            candidates.append(first.parent.name)
        if extra_candidates and part.name in extra_candidates:
            candidates += extra_candidates[part.name]
        sub = points[idx]
        dist = np.stack([segment_distance(sub, heads[c], tails[c]) for c in candidates], axis=1)
        own = 1.0 / np.power(dist + 1e-3, power)
        # The part's own bones dominate; the parent only takes over right at the joint.
        if len(candidates) > len(part.chain):
            own[:, len(part.chain):] *= 0.35
        order = np.argsort(-own, axis=1)[:, :max_influences]
        top = np.take_along_axis(own, order, axis=1)
        top /= top.sum(axis=1, keepdims=True)
        for k in range(order.shape[1]):
            for c_index, c in enumerate(candidates):
                sel = order[:, k] == c_index
                weights[c][idx[sel]] += top[sel, k]
    weights = blend_weights(mesh, weights, rigid_select, blend, max_influences)
    for n, w in weights.items():
        nz = np.nonzero(w > 0.01)[0]
        for i in nz:
            groups[n].add([int(i)], float(w[i]), "REPLACE")
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    mesh_obj.parent = arm


class Poser:
    """Turns per-frame pose specs into keyed quaternions without touching the depsgraph.
    spec per bone: {"rot": [(axis_in_armature_rest_space, angle), ...]} composes local hinge rotations;
    {"abs": Matrix} sets the bone's armature-space pose matrix; {"loc": Vector} adds a translation (armature space)."""

    def __init__(self, arm):
        self.arm = arm
        self.rest = {b.name: b.matrix_local.copy() for b in arm.data.bones}
        self.tail = {b.name: b.tail_local.copy() for b in arm.data.bones}
        self.parent = {b.name: (b.parent.name if b.parent else None) for b in arm.data.bones}
        order = []
        pending = [b.name for b in arm.data.bones]
        while pending:
            for n in list(pending):
                if self.parent[n] is None or self.parent[n] in order:
                    order.append(n)
                    pending.remove(n)
        self.order = order
        self.last = {}

    def inherited(self, name, pose):
        parent = self.parent[name]
        if parent is None:
            return self.rest[name].copy()
        return pose[parent] @ self.rest[parent].inverted() @ self.rest[name]

    def solve(self, specs):
        pose = {}
        for name in self.order:
            inh = self.inherited(name, pose)
            spec = specs.get(name, {})
            if "abs" in spec:
                m = spec["abs"]
            else:
                rest_rot = self.rest[name].to_3x3()
                basis_rot = Quaternion()
                for axis, angle in spec.get("rot", []):
                    local_axis = (rest_rot.inverted() @ Vector(axis)).normalized()
                    basis_rot = basis_rot @ Quaternion(local_axis, angle)
                m = inh @ basis_rot.to_matrix().to_4x4()
                if "loc" in spec:
                    m = Matrix.Translation(spec["loc"]) @ m
            pose[name] = m
        return pose

    def key(self, pose, frame):
        for name in self.order:
            inh = self.inherited(name, pose)
            basis = inh.inverted() @ pose[name]
            loc, rot, _ = basis.decompose()
            prev = self.last.get(name)
            if prev is not None and prev.dot(rot) < 0:
                rot.negate()
            self.last[name] = rot
            pb = self.arm.pose.bones[name]
            pb.rotation_quaternion = rot
            pb.location = loc
            pb.keyframe_insert("rotation_quaternion", frame=frame, group=name)
            pb.keyframe_insert("location", frame=frame, group=name)

    def action(self, name, frames, spec_fn, loop=False):
        """Keys frames 0..frames (inclusive) from spec_fn(t in 0..1, frame); a loop repeats frame 0 at the end."""
        self.arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        self.arm.animation_data.action = act
        self.last = {}
        for f in range(frames + 1):
            t = (f % frames) / frames if loop else f / frames
            self.key(self.solve(spec_fn(t, f)), f)
        return act


def bone_matrix(head, direction, rest_matrix, yaw_delta, plane_normal):
    """Armature-space pose for a leg bone lying in a vertical plane: yaw the rest frame, then swing it about the
    plane normal until its Y axis points along `direction`."""
    rest_rot = rest_matrix.to_3x3()
    yaw = Matrix.Rotation(yaw_delta, 3, "Z")
    yawed_dir = (yaw @ rest_rot.col[1]).normalized()
    direction = direction.normalized()
    angle = yawed_dir.angle(direction, 0.0)
    axis = yawed_dir.cross(direction)
    if axis.length < 1e-8:
        swing = Matrix.Identity(3)
    else:
        swing = Matrix.Rotation(angle, 3, axis.normalized())
    rot = swing @ yaw @ rest_rot
    m = rot.to_4x4()
    m.translation = head
    return m


class Leg:
    """A three-bone insect leg (coxa, femur, tibia) solved analytically: the coxa yaws towards the foot with a fixed
    lift, femur and tibia are a two-bone IK in the vertical plane with the knee up."""

    def __init__(self, names, root, foot, coxa_len, femur_len, tibia_len, coxa_lift, knee_lift=1.0):
        self.names = names
        self.root = Vector(root)
        self.foot = Vector(foot)
        self.lengths = (coxa_len, femur_len, tibia_len)
        self.coxa_lift = coxa_lift
        self.knee_lift = knee_lift

    def joints(self, root, foot):
        lc, lf, lt = self.lengths
        flat = Vector((foot.x - root.x, foot.y - root.y, 0))
        yaw_dir = flat.normalized() if flat.length > 1e-6 else Vector((1, 0, 0))
        coxa_end = root + (yaw_dir * math.cos(self.coxa_lift) + Vector((0, 0, math.sin(self.coxa_lift)))) * lc
        to_foot = foot - coxa_end
        d = min(to_foot.length, (lf + lt) * 0.995)
        u = to_foot.normalized()
        w = Vector((0, 0, 1)) - u * u.z
        w = w.normalized() if w.length > 1e-6 else yaw_dir
        cos_a = max(-1.0, min(1.0, (lf * lf + d * d - lt * lt) / (2 * lf * d)))
        alpha = math.acos(cos_a) * self.knee_lift
        knee = coxa_end + (u * math.cos(alpha) + w * math.sin(alpha)) * lf
        end = coxa_end + u * d
        return root, coxa_end, knee, end, yaw_dir

    def rest_bones(self, parent):
        root, c, k, f, _ = self.joints(self.root, self.foot)
        return [(self.names[0], root, c, parent, True), (self.names[1], c, k, self.names[0], True),
                (self.names[2], k, f, self.names[1], True)]

    def pose(self, poser, root_now, foot_now):
        _, _, _, _, rest_yaw = self.joints(self.root, self.foot)
        root, c, k, f, yaw_dir = self.joints(root_now, foot_now)
        yaw_delta = math.atan2(yaw_dir.y, yaw_dir.x) - math.atan2(rest_yaw.y, rest_yaw.x)
        normal = Vector((0, 0, 1)).cross(yaw_dir)
        out = {}
        for name, head, tail in zip(self.names, (root, c, k), (c, k, f)):
            out[name] = {"abs": bone_matrix(head, tail - head, poser.rest[name], yaw_delta, normal)}
        return out


def aim_matrix(head, direction, normal, rest_matrix, rest_normal):
    """Armature-space pose of a bone whose Y axis points along `direction`, turned so that the rest bend-plane
    normal `rest_normal` maps onto `normal` (no roll drift on limbs that swing in any plane)."""
    def frame(y, n):
        a = Vector(y).normalized()
        b = Vector(n) - a * Vector(n).dot(a)
        if b.length < 1e-6:
            b = a.orthogonal()
        b.normalize()
        return Matrix((a, b, a.cross(b))).transposed()
    rest_rot = rest_matrix.to_3x3()
    rot = frame(direction, normal) @ frame(rest_rot.col[1], rest_normal).transposed() @ rest_rot
    m = rot.to_4x4()
    m.translation = head
    return m


def two_bone(root, target, l1, l2, pole):
    """Knee of a two-bone chain from `root` towards `target`, bending towards `pole` (a direction)."""
    to = target - root
    d = min(max(to.length, abs(l1 - l2) * 1.01 + 1e-4), (l1 + l2) * 0.999)
    u = to.normalized()
    w = Vector(pole) - u * Vector(pole).dot(u)
    w = w.normalized() if w.length > 1e-6 else u.orthogonal().normalized()
    cos_a = max(-1.0, min(1.0, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)))
    a = math.acos(cos_a)
    return root + (u * math.cos(a) + w * math.sin(a)) * l1, root + u * d


class Limb:
    """A two- or three-bone limb with a pole: hip -> knee -> (ankle ->) foot. The knee bends towards `pole`; a third
    bone (a raised foot, digitigrade) hangs from the ankle along `foot_dir(...)` so the chain stays planted.
    Works for any plane (sprawling stone legs, a runner's legs swinging fore and aft)."""

    def __init__(self, names, root, foot, lengths, pole, foot_dir=None):
        self.names = names
        self.root = Vector(root)
        self.foot = Vector(foot)
        self.lengths = lengths
        self.pole = Vector(pole)
        self.foot_dir = Vector(foot_dir) if foot_dir is not None else None
        self.rest_points = self.points(self.root, self.foot, self.pole, self.foot_dir)
        self.rest_normals = self._normals(self.rest_points, self.pole)

    def points(self, root, foot, pole, foot_dir=None):
        if len(self.lengths) == 2:
            knee, end = two_bone(root, foot, self.lengths[0], self.lengths[1], pole)
            return [root, knee, end]
        fd = Vector(foot_dir if foot_dir is not None else self.foot_dir).normalized()
        ankle = foot - fd * self.lengths[2]
        knee, end = two_bone(root, ankle, self.lengths[0], self.lengths[1], pole)
        return [root, knee, end, end + fd * self.lengths[2]]

    @staticmethod
    def _normals(pts, pole):
        n = (pts[1] - pts[0]).cross(pts[2] - pts[1])
        if n.length < 1e-7:
            n = (pts[-1] - pts[0]).cross(Vector(pole))
        n.normalize()
        return [n] * (len(pts) - 1)

    def rest_bones(self, parent):
        pts = self.rest_points
        out, prev = [], parent
        for i, name in enumerate(self.names):
            out.append((name, pts[i], pts[i + 1], prev, True))
            prev = name
        return out

    def pose(self, poser, root_now, foot_now, pole=None, foot_dir=None, foot_roll=None):
        """`foot_roll="swing"` (opt-in, three-bone limbs; round 19): the foot bone keeps its rest roll relative to the
        shin and only swings off it the shortest way. By default its roll comes from the knee plane's normal, which is
        ill-conditioned when the foot bone lies close to that normal: the foot then spins about itself within a few
        frames (Gravemaw's spades reaching from a gate crown to the far ground: 115 degrees in three frames)."""
        pole = Vector(pole) if pole is not None else self.pole
        pts = self.points(root_now, foot_now, pole, foot_dir)
        normals = self._normals(pts, pole)
        if foot_roll == "swing" and len(self.names) == 3:
            normals = normals[:2] + [self._swung_foot_normal(pts, normals[1])]
        return {name: {"abs": aim_matrix(pts[i], pts[i + 1] - pts[i], normals[i], poser.rest[name], self.rest_normals[i])}
                for i, name in enumerate(self.names)}

    def _swung_foot_normal(self, pts, shin_normal):
        """The rest foot normal carried rest foot -> rest shin (shortest swing), rest shin -> shin now (the shin's own
        turn, roll included) and shin now -> foot now (shortest swing): the identity at rest."""
        rp = self.rest_points
        rest_shin, rest_foot = rp[2] - rp[1], rp[3] - rp[2]
        shin, foot = pts[2] - pts[1], pts[3] - pts[2]
        shin_turn = (aim_matrix(Vector(), shin, shin_normal, Matrix.Identity(4), self.rest_normals[1]).to_3x3()
                     @ aim_matrix(Vector(), rest_shin, self.rest_normals[1], Matrix.Identity(4),
                                  self.rest_normals[1]).to_3x3().inverted())
        n = rest_foot.rotation_difference(rest_shin) @ self.rest_normals[2]
        return shin.rotation_difference(foot) @ (shin_turn @ n)


def step_curve(phase, stance, stride, lift):
    """Foot offset (forward, up) over one gait cycle: planted and sliding back for `stance` of the cycle, then a
    lifted swing forward. phase 0 = the foot touches down at the front."""
    if phase < stance:
        s = phase / stance
        return stride * (0.5 - s), 0.0
    s = (phase - stance) / (1 - stance)
    return stride * (-0.5 + (1 - math.cos(math.pi * s)) * 0.5), lift * math.sin(math.pi * s)


def follow(poser, pose_specs, bone, point):
    """Where a rest-space point attached to `bone` is after the specs so far (used to move leg roots with the body)."""
    pose = poser.solve(pose_specs)
    return pose[bone] @ poser.rest[bone].inverted() @ Vector(point)


# ---------------------------------------------------------------------------------------------------------------
# Export
# ---------------------------------------------------------------------------------------------------------------

def export_fbx(path, objects, animated):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in bpy.context.view_layer.objects:
        o.select_set(o in objects)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        # One rule for every asset: the axis conversion stays on the root node (safe for armatures), and Unity's
        # "Bake Axis Conversion" import option folds it into the mesh so the prefab root has no rotation.
        bake_space_transform=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=animated,
        bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        path_mode="STRIP",
        embed_textures=False,
    )
    log("exported", path)


def save_blend(name):
    os.makedirs(BLEND, exist_ok=True)
    path = os.path.join(BLEND, name + ".blend")
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backups: the scripts are the source
    bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)
    log("saved", path)


def write_report(asset, data):
    path = os.path.join(EXPORTS, asset, asset + "_report.json")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    log("report", json.dumps(data))


# ---------------------------------------------------------------------------------------------------------------
# Preview renders
# ---------------------------------------------------------------------------------------------------------------

ROUND = 3
GAME_PITCH = 44.0
GAME_YAW = 12.0
# The game's play view: orthographic size 7 (half height) on 720 lines, so about 51 pixels per world unit.
GAME_PIXELS_PER_UNIT = 720 / 14.0


def preview_world(color=(0.022, 0.026, 0.036), strength=1.0):
    world = bpy.data.worlds.new("Preview")
    bpy.context.scene.world = world
    try:
        world.use_nodes = True
    except Exception:
        pass
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (*color, 1)
    bg.inputs["Strength"].default_value = strength
    return world


def light_rig(scale=1.0, key_color=(1.0, 0.96, 0.9), rim_color=(0.55, 0.75, 1.0)):
    """Key sun from the camera side, a soft cool fill, and a strong rim from behind for the silhouette."""
    objs = []
    key = bpy.data.lights.new("Key", "SUN"); key.energy = 3.2; key.color = key_color; key.angle = math.radians(6)
    o = link(bpy.data.objects.new("Key", key)); o.rotation_euler = (math.radians(50), 0, math.radians(-35)); objs.append(o)
    fill = bpy.data.lights.new("Fill", "AREA"); fill.energy = 25 * scale * scale; fill.size = 3 * scale; fill.color = (0.6, 0.7, 1.0)
    o = link(bpy.data.objects.new("Fill", fill)); o.location = Vector((-2.5, -1.5, 1.5)) * scale
    o.rotation_euler = Vector((0, 0, 0)); look_at(o, Vector((0, 0, 0.2 * scale))); objs.append(o)
    rim = bpy.data.lights.new("Rim", "AREA"); rim.energy = 90 * scale * scale; rim.size = 2 * scale; rim.color = rim_color
    o = link(bpy.data.objects.new("Rim", rim)); o.location = Vector((1.5, 3.0, 2.0)) * scale
    look_at(o, Vector((0, 0, 0.2 * scale))); objs.append(o)
    return objs


def game_lights(key_energy=3.0, rim_energy=1.2):
    """Lighting like a level: one shadowing key sun from the camera side plus a weak cool back sun for rims.
    No local lights, so a field-wide view has no light pools."""
    objs = []
    key = bpy.data.lights.new("GameKey", "SUN"); key.energy = key_energy; key.angle = math.radians(4)
    key.color = (1.0, 0.95, 0.88)
    o = link(bpy.data.objects.new("GameKey", key)); o.rotation_euler = (math.radians(42), 0, math.radians(-30)); objs.append(o)
    back = bpy.data.lights.new("GameBack", "SUN"); back.energy = rim_energy; back.color = (0.5, 0.7, 1.0)
    back.use_shadow = False
    o = link(bpy.data.objects.new("GameBack", back)); o.rotation_euler = (math.radians(-60), 0, math.radians(20)); objs.append(o)
    return objs


def hide_lights(objs, hidden):
    for o in objs:
        o.hide_render = hidden


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def ground(size=400.0, color=(0.03, 0.035, 0.045), z=0.0):
    mesh = bpy.data.meshes.new("Ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size / 2)
    bm.to_mesh(mesh)
    obj = link(bpy.data.objects.new("Ground", mesh))
    obj.location.z = z
    mat = new_material("Ground")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = 0.85
    noise = nt.nodes.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 3.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (color[0] * 0.7, color[1] * 0.7, color[2] * 0.7, 1)
    ramp.color_ramp.elements[1].color = (color[0] * 1.3, color[1] * 1.3, color[2] * 1.3, 1)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    mesh.materials.append(mat)
    return obj


def game_camera(target, ortho_scale, pitch=GAME_PITCH, yaw=GAME_YAW, distance=30.0):
    cam_data = bpy.data.cameras.new("GameCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam_data.clip_end = 200
    cam = link(bpy.data.objects.new("GameCam", cam_data))
    p = math.radians(pitch)
    y = math.radians(yaw)
    # Looking towards +Y (the creatures walk towards -Y, i.e. towards the camera's lower edge) from above.
    offset = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p))) * distance
    cam.location = Vector(target) + offset
    look_at(cam, target)
    return cam


def closeup_camera(target, distance, azimuth=-35.0, elevation=24.0, lens=60.0):
    cam_data = bpy.data.cameras.new("CloseCam")
    cam_data.lens = lens
    cam_data.clip_start = 0.01
    cam = link(bpy.data.objects.new("CloseCam", cam_data))
    a = math.radians(azimuth)
    e = math.radians(elevation)
    cam.location = Vector(target) + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * distance
    look_at(cam, target)
    return cam


def setup_eevee(width, height, samples=48):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.eevee.taa_render_samples = samples
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    try:
        scene.eevee.use_shadows = True
    except AttributeError:
        pass


def render(camera, path, width, height, bloom=True, samples=48):
    scene = bpy.context.scene
    setup_eevee(width, height, samples)
    scene.camera = camera
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    if bloom:
        add_bloom(path)
    log("render", path)
    return path


def image_to_array(path):
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    arr = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return arr


def array_to_image(arr, path, colorspace=None):
    h, w = arr.shape[:2]
    img = bpy.data.images.new("tmp_out", w, h, alpha=True)
    if colorspace:
        img.colorspace_settings.name = colorspace
    img.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def _blur(channel, radius):
    """Gaussian blur through the FFT; a stand-in for the game's bloom on preview stills."""
    h, w = channel.shape
    y = np.fft.fftfreq(h)[:, None]
    x = np.fft.fftfreq(w)[None, :]
    sigma = radius
    kernel = np.exp(-2 * (math.pi ** 2) * (sigma ** 2) * (x * x + y * y))
    return np.real(np.fft.ifft2(np.fft.fft2(channel) * kernel))


def add_bloom(path, threshold=0.72, strength=0.55):
    arr = image_to_array(path)
    rgb = arr[..., :3]
    lum = rgb.max(axis=2)
    bright = rgb * smoothstep(threshold, 1.0, lum)[..., None]
    scale = max(arr.shape[0], arr.shape[1]) / 1280.0
    glow = np.zeros_like(rgb)
    for radius, weight in ((3 * scale, 0.5), (10 * scale, 0.35), (28 * scale, 0.25)):
        for c in range(3):
            glow[..., c] += _blur(bright[..., c], radius) * weight
    arr[..., :3] = 1 - (1 - rgb) * (1 - np.clip(glow * strength * 2, 0, 1))
    array_to_image(arr, path)


def contact_sheet(paths, out_path, columns, cell_width, background=(0.05, 0.05, 0.07)):
    """Lays renders out on a grid (each scaled to the cell width, nearest-neighbour)."""
    images = [image_to_array(p) for p in paths]
    cells = []
    for img in images:
        h, w = img.shape[:2]
        scale = cell_width / w
        nh = max(1, int(round(h * scale)))
        ys = np.clip((np.arange(nh) / scale).astype(int), 0, h - 1)
        xs = np.clip((np.arange(cell_width) / scale).astype(int), 0, w - 1)
        cells.append(img[ys][:, xs])
    rows = [cells[i:i + columns] for i in range(0, len(cells), columns)]
    pad = 6
    row_heights = [max(c.shape[0] for c in r) for r in rows]
    total_h = sum(row_heights) + pad * (len(rows) + 1)
    total_w = columns * cell_width + pad * (columns + 1)
    sheet = np.zeros((total_h, total_w, 4), dtype=np.float32)
    sheet[..., :3] = background
    sheet[..., 3] = 1
    # Blender images are stored bottom-up, so lay rows out from the top of the array's last index downwards.
    y = total_h - pad
    for r, rh in zip(rows, row_heights):
        x = pad
        for c in r:
            ch = c.shape[0]
            sheet[y - ch:y, x:x + cell_width] = c
            x += cell_width + pad
        y -= rh + pad
    array_to_image(sheet, out_path)
    log("sheet", out_path)


def uv_layout(obj, path, size=1024, background=None):
    """The UV wireframe of `obj` drawn over a map (or black), for checking islands and texel density."""
    if background:
        img = image_to_array(background)
        if img.shape[0] != size:
            ys = (np.arange(size) * img.shape[0] / size).astype(int)
            img = img[ys][:, ys]
        img = img.copy()
        img[..., :3] *= 0.6
    else:
        img = np.zeros((size, size, 4), dtype=np.float32)
    img[..., 3] = 1
    mesh = obj.data
    uv = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        pts = [uv[i].uv for i in poly.loop_indices]
        for a, b in zip(pts, pts[1:] + pts[:1]):
            n = int(max(abs(b.x - a.x), abs(b.y - a.y)) * size) + 2
            t = np.linspace(0, 1, n)
            xs = np.clip(((a.x + (b.x - a.x) * t) * size).astype(int), 0, size - 1)
            ys = np.clip(((a.y + (b.y - a.y) * t) * size).astype(int), 0, size - 1)
            img[ys, xs, :3] = (0.3, 1.0, 0.6)
    array_to_image(img, path)
    log("uv layout", path)
    return path


def macro_camera(target, distance, azimuth, elevation, lens=80.0):
    """A very close camera for pores and scratches (cutscene distance and closer)."""
    return closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=lens)


def compare_sheet(asset, names, out_dir):
    """Side-by-side sheet `compare_<asset>.png`: left column the previous round's render (from --compare <dir>),
    right column this round's render of the same name. Skipped when --compare is not given."""
    old_dir = cli_args().get("compare")
    if not old_dir:
        return None
    paths = []
    for name in names:
        old = os.path.join(old_dir, name)
        new = os.path.join(out_dir, name)
        if os.path.exists(old) and os.path.exists(new):
            paths += [old, new]
    if not paths:
        return None
    out = os.path.join(out_dir, f"compare{ROUND}_{asset}.png")
    contact_sheet(paths, out, 2, 640)
    return out


# ---------------------------------------------------------------------------------------------------------------
# One-call pipelines
# ---------------------------------------------------------------------------------------------------------------

class MeshAsset:
    """Result of build_mesh_asset: the hidden detailed source, the game mesh, its textures and UV stats."""

    def __init__(self, name, high, low, textures, high_tris, low_tris, uv_stats, details):
        self.name = name
        self.high = high
        self.low = low
        self.textures = textures
        self.high_tris = high_tris
        self.low_tris = low_tris
        self.uv_stats = uv_stats
        self.details = details


def detail_tiling(world_per_uv, family, k):
    """UV tiling of a shared detail map on this asset: how many tiles fit in one UV unit."""
    return round(world_per_uv / (detailmaps.TILE_WORLD[family] * k), 1)


def build_mesh_asset(name, builder, voxel, target_tris, tex_size, ao_distance, extrusion, ray,
                     emission_strength=2.0, smooth_iterations=6, extra=None, symmetric=True,
                     low_from_detail=False, sharp_angle=None, specular=0.4, mirror=False, k=1.0,
                     families=FAMILY_ORDER, detail_params=None, detail_strength=1.0, low_size=None,
                     preview_detail=(1.0, 0.6), family_set=None, edge_gain=0.35, border_smooth=0):
    """Parts -> fused source with detail and vertex colours -> baked AO -> decimated, unwrapped game mesh with maps.
    `families`: the asset's detail families in mask order (B = first, A = second); `k`: size factor for the
    micro-detail; `mirror`: mirrored UV halves (symmetric creatures); `low_size`: also write a Low map set."""
    parts_obj = builder.to_object(name + "_Parts")
    high_mesh = fuse(parts_obj, voxel, smooth_iterations)
    bpy.data.objects.remove(parts_obj)
    smooth_copy = high_mesh.copy()
    detail(high_mesh, builder, extra, border_smooth)
    shade_smooth(high_mesh)
    high = link(bpy.data.objects.new(name + "_High", high_mesh))
    high.data.materials.append(high_bake_material(edge_gain=edge_gain, k=k, detail_params=detail_params,
                                                  detail_strength=detail_strength, families=families,
                                                  family_set=family_set))
    bake_vertex_ao(high, ao_distance)

    # Creatures decimate the undisplaced surface (cleaner deforming silhouette, detail lives in the normal map);
    # props with deep carved shapes (a pit, a crack) need those shapes in the game mesh itself.
    if low_from_detail:
        bpy.data.meshes.remove(smooth_copy)
        smooth_copy = high_mesh.copy()
    low_mesh = decimate(smooth_copy.copy(), target_tris, name, symmetric)
    if mirror:
        # The symmetry cut adds triangles: decimate once more, a little lower, so the budget holds.
        symmetrize(low_mesh)
        low_mesh.calc_loop_triangles()
        over = len(low_mesh.loop_triangles)
        if over > target_tris:
            bpy.data.meshes.remove(low_mesh)
            low_mesh = decimate(smooth_copy.copy(), int(target_tris * target_tris / over), name, symmetric)
            symmetrize(low_mesh)
    bpy.data.meshes.remove(smooth_copy)
    shade_smooth(low_mesh)
    if sharp_angle is not None:
        # Hard facets (crystals): split normals where faces meet at more than `sharp_angle` degrees.
        low_mesh.set_sharp_from_angle(angle=math.radians(sharp_angle))
    low = link(bpy.data.objects.new(name, low_mesh))
    uv_stats, bake_faces = uvlayout.unwrap(low, builder, mirror=mirror)
    out_dir = os.path.join(EXPORTS, name)
    textures = bake_maps(high, low, out_dir, name, tex_size, extrusion, ray, bake_faces=bake_faces, low_size=low_size)
    tiles = detailmaps.ensure()
    details = {f: {"map": os.path.basename(tiles[f]), "tiling": detail_tiling(uv_stats["world_per_uv"], f, k),
                   "mask": ch} for f, ch in zip(families[:2], ("B", "A"))}
    preview = [(tiles[f], details[f]["tiling"], s, details[f]["mask"]) for f, s in zip(families[:2], preview_detail)]
    low.data.materials.clear()
    low.data.materials.append(game_material(name + "_Mat", textures, emission_strength, specular, preview))
    high.hide_render = True
    high.hide_viewport = True
    high_mesh.calc_loop_triangles()
    asset = MeshAsset(name, high, low, textures, len(high_mesh.loop_triangles), tri_count(low), uv_stats, details)
    log(name, "high tris", asset.high_tris, "low tris", asset.low_tris, "details", details)
    return asset


def set_detail_preview(asset, on):
    """Turns the tiling detail normals in the preview material on or off (renders with and without them)."""
    flat = bpy.data.images.get("FlatNormal")
    if flat is None:
        flat = bpy.data.images.new("FlatNormal", 4, 4, alpha=False)
        flat.colorspace_settings.name = "Non-Color"
        flat.pixels.foreach_set(np.tile(np.array((0.5, 0.5, 1.0, 1.0), dtype=np.float32), 16))
    nt = asset.low.data.materials[0].node_tree
    for n in nt.nodes:
        if n.bl_idname == "ShaderNodeTexImage" and n.image is not None and "detail_path" in n:
            n.image = bpy.data.images.load(n["detail_path"], check_existing=True) if on else flat


def game_view(arm, mesh, out_path, scale, count, spread, zoom=1.0, seed=3, close_lights=(), width=640, height=360,
              target=(0, 0, 0.1), lift=0.0):
    """Renders `count` copies of a rigged model at in-game scale under the game camera and game lighting.
    zoom 1 is the default play zoom (about 51 px per unit at 720p); larger values zoom in. `lift` raises the
    copies (flyers cruise above the road); `target` is where the camera looks."""
    rng = np.random.default_rng(seed)
    copies = []
    for i in range(count):
        a2 = link(arm.copy())
        m2 = link(mesh.copy())
        m2.parent = a2
        m2.modifiers["Armature"].object = a2
        a2.scale = (scale, scale, scale)
        a2.location = Vector((rng.uniform(-spread[0], spread[0]), rng.uniform(-spread[1], spread[1]), lift)) if count > 1 else Vector((0, 0, lift))
        a2.rotation_euler.z = rng.uniform(-0.5, 0.5) if count > 1 else 0.0
        copies.append((a2, m2))
    arm.hide_render = True
    mesh.hide_render = True
    hide_lights(close_lights, True)
    lights = game_lights()
    cam = game_camera(Vector(target), width / GAME_PIXELS_PER_UNIT / zoom)
    render(cam, out_path, width, height)
    for o in lights + [cam]:
        bpy.data.objects.remove(o)
    for a2, m2 in copies:
        bpy.data.objects.remove(m2)
        bpy.data.objects.remove(a2)
    hide_lights(close_lights, False)
    arm.hide_render = False
    mesh.hide_render = False
    return out_path


def set_action(arm, act, frame):
    """Assigns `act` to `arm` (Blender 5 layered actions: also its slot) and moves the scene to `frame`."""
    arm.animation_data.action = act
    if hasattr(arm.animation_data, "action_slot") and len(getattr(act, "slots", ())) and arm.animation_data.action_slot is None:
        arm.animation_data.action_slot = act.slots[0]
    bpy.context.scene.frame_set(frame)


def game_view_mixed(arm, mesh, out_path, scale, layout, zoom=1.0, close_lights=(), width=640, height=360,
                    target=(0, 0, 0.1)):
    """Like game_view, but each copy gets its own (action, action frame, position, yaw) from `layout`, e.g. walking
    and rolling Rollers side by side. Each copy plays its action from an NLA strip shifted so the scene frame shows
    the requested action frame."""
    scene = bpy.context.scene
    base = 1000
    copies = []
    for act, frame, pos, yaw in layout:
        a2 = link(arm.copy())
        a2.animation_data.action = None
        track = a2.animation_data.nla_tracks.new()
        strip = track.strips.new(act.name + "_strip", int(base - frame), act)
        strip.extrapolation = "HOLD"
        if hasattr(strip, "action_slot") and len(getattr(act, "slots", ())):
            strip.action_slot = act.slots[0]
        m2 = link(mesh.copy())
        m2.parent = a2
        m2.modifiers["Armature"].object = a2
        a2.scale = (scale, scale, scale)
        a2.location = Vector(pos)
        a2.rotation_euler.z = yaw
        copies.append((a2, m2))
    arm.hide_render = True
    mesh.hide_render = True
    hide_lights(close_lights, True)
    lights = game_lights()
    cam = game_camera(Vector(target), width / GAME_PIXELS_PER_UNIT / zoom)
    keep = scene.frame_current
    scene.frame_set(base)
    render(cam, out_path, width, height)
    scene.frame_set(keep)
    for o in lights + [cam]:
        bpy.data.objects.remove(o)
    for a2, m2 in copies:
        bpy.data.objects.remove(m2)
        bpy.data.objects.remove(a2)
    hide_lights(close_lights, False)
    arm.hide_render = False
    mesh.hide_render = False
    return out_path


def strip_sheet(arm, act, camera, out_dir, prefix, frames, columns=4, cell=480):
    """Renders `frames` of `act` from `camera` and lays them out as `<prefix>_strip.png`."""
    paths = []
    for i, f in enumerate(frames):
        set_action(arm, act, f)
        paths.append(render(camera, os.path.join(out_dir, f"{prefix}_{i}.png"), 640, 360, samples=24))
    out = os.path.join(out_dir, f"{prefix}_strip.png")
    contact_sheet(paths, out, columns, cell)
    for p in paths:
        os.remove(p)
    return out


# ---------------------------------------------------------------------------------------------------------------
# LOD1: a reduced rig keyed from the full rig's pose
# ---------------------------------------------------------------------------------------------------------------

def lod_action(full_poser, lod_poser, name, frames, spec_fn, loop, aims):
    """Keys the LOD rig from the full rig's pose every frame. Shared bones copy their armature-space pose; a bone in
    `aims` ({lod bone: (full bone whose head it starts at, full bone whose tail it points to)}) replaces a chain and
    is aimed from the posed head to the posed tail, keeping the first bone's roll."""
    lod_poser.arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    lod_poser.arm.animation_data.action = act
    lod_poser.last = {}
    for f in range(frames + 1):
        t = (f % frames) / frames if loop else f / frames
        pose = full_poser.solve(spec_fn(t, f))
        specs = {}
        for bname in lod_poser.order:
            if bname in aims:
                first, last = aims[bname]
                head = pose[first].translation
                tail = pose[last] @ full_poser.rest[last].inverted() @ full_poser.tail[last]
                delta = pose[first].to_3x3() @ full_poser.rest[first].to_3x3().inverted()
                guess = delta @ lod_poser.rest[bname].to_3x3()
                swing = guess.col[1].rotation_difference((tail - head).normalized()).to_matrix()
                m = (swing @ guess).to_4x4()
                m.translation = head
                specs[bname] = {"abs": m}
            else:
                specs[bname] = {"abs": pose[bname]}
        lod_poser.key(lod_poser.solve(specs), f)
    return act


def build_lod(name, lod_name, asset, builder, bones, clips, full_poser, actions_full, lod_tris, drop, aims):
    """LOD1 for a creature: the game mesh decimated again (same UVs, shares the maps), a reduced rig and the same
    clips. `bones`: the full bone list; `drop`: {full bone: lod bone it folds into}; `aims`: lod bones that replace
    a chain (see lod_action). Exports `<lod_name>.fbx` next to the full FBX with the same take names."""
    full = {b[0]: b for b in bones}
    lod_bones = []
    for bname, head, tail, parent, deform in bones:
        if bname in drop:
            continue
        if bname in aims:
            first, last = aims[bname]
            head, tail = full[first][1], full[last][2]
        while parent in drop:
            parent = drop[parent]
        lod_bones.append((bname, head, tail, parent, deform))
    lod_mesh = decimate(asset.low.data.copy(), lod_tris, lod_name, symmetric=True)
    shade_smooth(lod_mesh)
    lod = link(bpy.data.objects.new(lod_name, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    arm = build_armature(lod_name + "_Rig", lod_bones)

    def remap(b):
        while b in drop:
            b = drop[b]
        return b
    saved = [(p, list(p.chain), p.bone) for p in builder.parts]
    for p in builder.parts:
        p.chain = list(dict.fromkeys(remap(c) for c in p.chain))
        p.bone = p.chain[0] if p.chain else p.bone
    skin(lod, arm, builder)
    for p, chain, bone in saved:
        p.chain, p.bone = chain, bone
    poser = Poser(arm)
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(full_poser)
        actions[clip] = lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop, aims)
    arm.animation_data.action = actions.get("Idle") or next(iter(actions.values()))
    for clip, act in actions_full.items():
        act.name = clip + "_Full"
    for clip, act in actions.items():
        act.name = clip
    export_fbx(os.path.join(EXPORTS, name, lod_name + ".fbx"), [arm, lod], animated=True)
    for clip, act in actions.items():
        act.name = clip + "_LOD"
    for clip, act in actions_full.items():
        act.name = clip
    return arm, lod, actions


def key_clips(poser, clips):
    """Keys every clip {name: fn(poser) -> (frames, spec, loop)}."""
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(poser)
        actions[clip] = poser.action(clip, frames, spec, loop=loop)
    return actions


# ---------------------------------------------------------------------------------------------------------------
# Old procedural stand-ins for compare sheets
# ---------------------------------------------------------------------------------------------------------------

def flat_proxy(groups, name):
    """Objects from {sRGB colour: PartBuilder}: unfused primitives in flat matte colours, roughly what
    EnemyModelFactory builds in Unity (used only to put the old look next to the new model)."""
    objs = []
    for i, (color, builder) in enumerate(groups.items()):
        if not builder.parts:
            continue
        obj = builder.to_object(f"{name}_{i}")
        obj.data.shade_smooth()
        lin = tuple(float(x) for x in srgb_to_linear(np.array(color)))
        obj.data.materials.append(principled(f"{name}_{i}_Mat", lin, rough=0.55))
        objs.append(obj)
    return objs


def game_view_static(objs, out_path, scale, zoom=1.0, close_lights=(), width=640, height=360, target=(0, 0, 0.1),
                     lift=0.0):
    """game_view for unrigged objects at the origin (one copy), raised by `lift`."""
    holder = link(bpy.data.objects.new("ProxyHolder", None))
    for o in objs:
        o.parent = holder
    holder.scale = (scale, scale, scale)
    holder.location.z = lift
    hide_lights(close_lights, True)
    lights = game_lights()
    cam = game_camera(Vector(target), width / GAME_PIXELS_PER_UNIT / zoom)
    render(cam, out_path, width, height)
    for o in lights + [cam]:
        bpy.data.objects.remove(o)
    hide_lights(close_lights, False)
    for o in objs:
        o.parent = None
    bpy.data.objects.remove(holder)
    return out_path


def remove_objects(objs):
    for o in objs:
        bpy.data.objects.remove(o)
