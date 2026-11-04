"""Surface styles for our own side (round 6: the hero; reusable for towers and drones later).

Same contract as chitin.py: a style(s, idx, part) colours, displaces and lights the vertices a part owns. The
micro-detail families are surface.SUIT_FAMILIES (Fam R weave, G ceramic, B metal, A rubber); surface.py turns them
into stitched seams, scratched and chipped plates, brushed metal and moulded rubber at bake time.

Look rules (CLAUDE.md, look feedback): cool technical colours (navy blue, white, yellow, cyan light), three clearly
separated values (white plates, mid-blue suit, gunmetal gear), a light top and a darker underside, and visible wear.
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

import common as C

FAMILY_SET = ("weave", "ceramic", "metal", "rubber")
# Stitched quilting seams: the per-pixel stitch lines in surface.weave_detail repeat every 1 / STITCH_FREQ units, so
# the geometric quilt bands use the same spacing and both line up.
STITCH_FREQ = 20.0


class Palette:
    def __init__(self, **kw):
        for key, value in kw.items():
            setattr(self, key, np.array(value, dtype=float))


# sRGB, as in a paint program (common.detail converts to linear).
PAL = Palette(
    plate_top=(0.9, 0.91, 0.89), plate_side=(0.74, 0.76, 0.76), plate_under=(0.42, 0.45, 0.5),
    suit_top=(0.24, 0.37, 0.62), suit_side=(0.16, 0.26, 0.46), suit_under=(0.07, 0.11, 0.2), suit_seam=(0.05, 0.08, 0.15),
    metal_top=(0.27, 0.29, 0.33), metal_side=(0.17, 0.18, 0.21), metal_under=(0.08, 0.085, 0.1),
    rubber=(0.075, 0.08, 0.09),
    yellow=(0.98, 0.74, 0.14), cyan=(0.32, 0.93, 1.0), glass=(0.02, 0.035, 0.055), glass_sheen=(0.25, 0.42, 0.55),
    dust=(0.43, 0.4, 0.37), crysium=(0.86, 0.72, 0.84), crysium_glint=(1.0, 0.75, 0.95), substrate=(0.35, 0.37, 0.4),
)


def family(s, idx, weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0, flow=None):
    w = np.c_[np.broadcast_to(weave, len(idx)), np.broadcast_to(ceramic, len(idx)),
              np.broadcast_to(metal, len(idx)), np.broadcast_to(rubber, len(idx))]
    s.fam[idx] = w
    if flow is not None:
        s.flow[idx] = flow


def mottle(s, idx, amount, scale, seed):
    return 1.0 + C.value_noise(s.p[idx], scale, seed)[:, None] * amount


class Ctx:
    """What a feature mask may look at: world points and normals, the part's local frame (unit sphere for
    ellipsoids) and, for tubes, the position along the tube, its radius and the distance from its axis."""

    def __init__(self, s, idx, part):
        self.p = s.p[idx]
        self.n = s.n[idx]
        self.L = C.local_points(part, self.p)
        norm = np.linalg.norm(self.L, axis=1, keepdims=True)
        self.u = self.L / np.maximum(norm, 1e-9)
        self.part = part
        if part.axis is not None and part.kind in ("tube", "shard"):
            self.t, self.radius, self.dist = C.tube_param(part, self.p)
            self.length = C.axis_length(part)
        else:
            self.t = None


BASES = {
    # name: (top, side, under, roughness, family weights)
    "suit": ("suit_top", "suit_side", "suit_under", 0.8, dict(weave=1.0)),
    "plate": ("plate_top", "plate_side", "plate_under", 0.42, dict(ceramic=1.0)),
    "metal": ("metal_top", "metal_side", "metal_under", 0.46, dict(metal=1.0)),
    "rubber": ("rubber", "rubber", "rubber", 0.86, dict(rubber=1.0)),
}


def styled(base, features=(), quilt=None, flow=None, disp=None, seed=0):
    """A surface of one base material with painted, grooved or lit features.
    `features`: (mask_fn(ctx) -> 0..1, effect) with effect keys: paint (sRGB colour), paint_amount, groove (depth),
    emit (sRGB colour), glow (strength), rough (absolute), raise (height), family (dict, replaces the base family).
    `quilt`: ("tube" | "height", spacing) padded bands with seams. `flow(ctx)`: growth / brush coordinate."""
    top_key, side_key, under_key, rough0, fam0 = BASES[base]

    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        n = ctx.n
        col = C.chitin_gradient(n, getattr(PAL, top_key), getattr(PAL, side_key), getattr(PAL, under_key))
        col = col * mottle(s, idx, 0.05 if base == "plate" else 0.08, 40.0, 3 + seed)
        d = np.zeros(len(idx))
        rough = np.full(len(idx), rough0)
        emit = np.zeros((len(idx), 3))
        fw = {k: np.full(len(idx), v) for k, v in {**dict(weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0), **fam0}.items()}
        fl = flow(ctx) if flow else (ctx.t * ctx.length if ctx.t is not None else ctx.p[:, 2])
        if quilt:
            mode, spacing = quilt
            coord = ctx.t * ctx.length if mode == "tube" else ctx.p[:, 2]
            frac = (coord / spacing) % 1.0
            edge = np.minimum(frac, 1 - frac)
            pillow = C.smoothstep(0.0, 0.22, edge)
            d += 0.003 * (pillow - 0.7)
            seam = 1 - C.smoothstep(0.0, 0.07, edge)
            col = C.lerp3(col, PAL.suit_seam, seam * 0.3)
            # Stitch lines in the bake sit on the seams: sin(2 pi F flow) peaks where coord / spacing is whole.
            fl = coord * (1.0 / (spacing * STITCH_FREQ)) + 0.25 / STITCH_FREQ
        elif base == "suit" and flow is None:
            fl = np.zeros(len(idx))  # unquilted fabric: no stitch rows
        for mask_fn, fx in features:
            m = np.clip(mask_fn(ctx), 0, 1)
            if not m.any():
                continue
            if "paint" in fx:
                # Paint chips off in patches, so stripes look worn instead of printed.
                worn = C.smoothstep(0.25, 0.55, C.value_noise(ctx.p, 90.0, 11 + seed)) * fx.get("wear", 0.0)
                amount = m * fx.get("paint_amount", 1.0) * (1 - worn)
                paint = np.asarray(fx["paint"]) * (0.8 + 0.2 * C.smoothstep(-0.4, 0.8, n[:, 2]))[:, None]
                col = C.lerp3(col, paint, amount)
            if "groove" in fx:
                d -= fx["groove"] * m
                col = col * (1 - 0.45 * m)[:, None]
            if "raise" in fx:
                d += fx["raise"] * m
            if "emit" in fx:
                e = np.asarray(fx["emit"])
                emit = np.maximum(emit, e[None, :] * (m * fx.get("glow", 1.0))[:, None])
                col = C.lerp3(col, e * 0.55, m)
            if "rough" in fx:
                rough = rough + (fx["rough"] - rough) * m
            if "family" in fx:
                for k in fw:
                    fw[k] = fw[k] * (1 - m) + fx["family"].get(k, 0.0) * m
        if disp:
            d += disp(ctx)
        s.albedo[idx] = col
        s.disp[idx] = d
        s.rough[idx] = rough
        s.emit[idx] = emit
        family(s, idx, fw["weave"], fw["ceramic"], fw["metal"], fw["rubber"], flow=fl)
    return style


def glass(hud_mask=None, glow=0.22):
    """The visor: smoked glass, very smooth, a cool sheen band across the upper half (reads as glass even in a dark
    level), a faint inner glow and a bright HUD line where `hud_mask` says."""
    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        u = ctx.u
        sheen = np.exp(-((u[:, 2] - 0.35) / 0.18) ** 2) * C.smoothstep(-0.2, 0.6, -u[:, 1])
        col = C.lerp3(np.broadcast_to(PAL.glass, (len(idx), 3)), PAL.glass_sheen, sheen * 0.55)
        e = np.broadcast_to(PAL.cyan * glow, (len(idx), 3)).copy()
        if hud_mask is not None:
            h = np.clip(hud_mask(ctx), 0, 1)
            e = np.maximum(e, PAL.cyan[None, :] * h[:, None])
            col = C.lerp3(col, PAL.cyan * 0.6, h)
        s.albedo[idx] = col
        s.emit[idx] = e
        s.rough[idx] = 0.06
        s.fam[idx] = 0.0
    return style


def light(color=None, strength=1.0, bands=None, band_dark=0.85):
    """A lit part (power cells, lamps): glowing, with optional dark ring bands along a tube (`bands` per unit)."""
    color = np.asarray(color if color is not None else PAL.cyan)

    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        m = np.ones(len(idx))
        if bands and ctx.t is not None:
            frac = (ctx.t * ctx.length * bands) % 1.0
            m = 1 - band_dark * (1 - C.smoothstep(0.0, 0.18, np.minimum(frac, 1 - frac)))
        s.albedo[idx] = C.lerp3(PAL.metal_side, color * 0.7, m)
        s.emit[idx] = color[None, :] * (m * strength)[:, None]
        s.rough[idx] = 0.3
        s.fam[idx] = np.c_[np.zeros(len(idx)), np.zeros(len(idx)), 1 - m, np.zeros(len(idx))]
    return style


def weathering(seed=7, ground_dust=0.36, crysium_boot=0.13):
    """Over the whole figure: scuffed patches on the plates, grime in the folds below, dust rising up the shins and
    boots, and pale pink Crysium dust with faint glints on the boots (he walks the crystal plains)."""
    def extra(s, pid):
        p, n = s.p, s.n
        lit = s.emit.max(axis=1) < 0.05
        h = p[:, 2]
        plate = s.fam[:, 1]
        scuff = C.smoothstep(0.2, 0.5, C.value_noise(p, 18.0, seed)) * C.smoothstep(0.1, 0.5, C.value_noise(p, 70.0, seed + 1))
        s.albedo = C.lerp3(s.albedo, s.albedo * 0.72, (scuff * plate * 0.6)[:, None] * lit[:, None])
        below = C.smoothstep(0.0, -0.8, n[:, 2])
        s.albedo = C.lerp3(s.albedo, s.albedo * 0.6, (below * 0.35)[:, None])
        dust = (1 - C.smoothstep(0.02, ground_dust, h)) * (0.55 + 0.45 * C.smoothstep(-0.3, 0.6, C.value_noise(p, 12.0, seed + 2)))
        s.albedo = np.where(lit[:, None], C.lerp3(s.albedo, PAL.dust, (dust * 0.5)[:, None]), s.albedo)
        boot = (1 - C.smoothstep(0.03, crysium_boot, h)) * C.smoothstep(0.1, 0.45, C.value_noise(p, 30.0, seed + 3))
        s.albedo = np.where(lit[:, None], C.lerp3(s.albedo, PAL.crysium, (boot * 0.55)[:, None]), s.albedo)
        glint = boot * C.smoothstep(0.62, 0.72, C.value_noise(p, 400.0, seed + 4))
        s.emit = np.maximum(s.emit, PAL.crysium_glint[None, :] * (glint * 0.55)[:, None])
        s.rough = np.clip(s.rough + dust * 0.12 + scuff * plate * 0.15, 0.04, 0.95)
    return extra


# -------------------------------------------------------------------------------------------------------------
# Feature masks (ctx -> 0..1)
# -------------------------------------------------------------------------------------------------------------

def band(values, centre, width, soft=0.35):
    """1 inside |values - centre| < width, with a soft edge of `soft` * width."""
    d = np.abs(values - centre)
    return 1 - C.smoothstep(width * (1 - soft), width, d)


def line(values, centre, width):
    return 1 - C.smoothstep(0.0, width, np.abs(values - centre))


def angle_around(ctx, ref):
    """Angle of each point around a tube's axis, 0 towards `ref` (a world direction)."""
    axis = ctx.part.axis
    tangent = axis[-1] - axis[0]
    tangent = tangent / np.linalg.norm(tangent)
    ref = np.asarray(ref, dtype=float)
    ref = ref - tangent * (ref @ tangent)
    ref /= np.linalg.norm(ref)
    side = np.cross(tangent, ref)
    centre = axis[0] + np.outer(ctx.t * np.linalg.norm(axis[-1] - axis[0]), tangent)
    radial = ctx.p - centre
    return np.arctan2(radial @ side, radial @ ref)


def rivets(ctx, points, radius):
    """Small domes at given world points."""
    out = np.zeros(len(ctx.p))
    for q in points:
        d = np.linalg.norm(ctx.p - np.asarray(q), axis=1)
        out = np.maximum(out, np.clip(1 - (d / radius) ** 2, 0, 1) ** 0.5)
    return out



def wrap(plate_style, under_style, mask_fn, depth=0.012, soft=0.0):
    """A plate that wraps part of a limb: the part is a tube a little thicker than the limb; where `mask_fn(ctx)` is
    1 it is the plate (`plate_style`), elsewhere it sinks by `depth` back to the limb and wears `under_style` (the
    suit). The step between the two is the plate's rim."""
    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        m = np.clip(mask_fn(ctx), 0, 1)
        under_style(s, idx, part)
        saved = (s.albedo[idx].copy(), s.disp[idx].copy(), s.rough[idx].copy(), s.emit[idx].copy(), s.fam[idx].copy(), s.flow[idx].copy())
        plate_style(s, idx, part)
        a, d, r, e, f, fl = saved
        s.albedo[idx] = C.lerp3(a, s.albedo[idx], m)
        s.disp[idx] = d * (1 - m) + s.disp[idx] * m - depth * (1 - m)
        s.rough[idx] = r * (1 - m) + s.rough[idx] * m
        s.emit[idx] = C.lerp3(e, s.emit[idx], m)
        s.fam[idx] = C.lerp3(f, s.fam[idx], m)
        s.flow[idx] = np.where(m > 0.5, s.flow[idx], fl)
        # A dark seam where the plate meets the cloth.
        rim = np.clip(1 - np.abs(m - 0.5) * 2, 0, 1) ** 2
        s.albedo[idx] = s.albedo[idx] * (1 - 0.5 * rim)[:, None]
    return style


def sector(ctx, front, half_angle, t0=0.0, t1=1.0, soft_deg=6.0, soft_t=0.04):
    """1 on the part of a tube within `half_angle` degrees of `front` and between t0 and t1 along it."""
    a = np.degrees(np.abs(angle_around(ctx, front)))
    m = 1 - C.smoothstep(half_angle - soft_deg, half_angle, a)
    m = m * C.smoothstep(t0, t0 + soft_t, ctx.t) * (1 - C.smoothstep(t1 - soft_t, t1, ctx.t))
    return m


# -------------------------------------------------------------------------------------------------------------
# Round 16: layered assets (separate shells instead of one fused blob)
# -------------------------------------------------------------------------------------------------------------
#
# common.build_mesh_asset fuses every part into one voxel surface. That suits creatures, but hard plates over a
# padded suit, coat panels over an undersuit, locks of hair and separate fingers all melt into soft "pillows" that
# way. build_layered_asset fuses each Layer on its own (its own voxel size and smoothing, so plates keep crisp
# bevelled edges and fingers keep their gaps), decimates each layer to its own share of the triangle budget (the
# face can be denser than the back of the coat), and then runs the same detail, bake, unwrap and material steps as
# common.build_mesh_asset. Part lookups (styles, skin weights, UV seams) go through a layer-aware replacement of
# common.assign_parts, so a vertex of the suit under a plate is never styled or weighted as the plate above it.

# Hard shells: plates (Mercer) and garment panels (Voss) as thickened surface patches with a bevelled rim.


PLATE_EMBED = 0.004     # how far a shell's inner face sinks into the suit under it
CHAMFER = 0.045         # share of a plate's width taken by its bevelled rim
BEVEL = 0.55            # how much of the plate's thickness the rim bevel takes off


def _edge_param(s, n):
    """Grid coordinate i / n remapped so the first and last rows sit a chamfer's width from the edge (they carry
    the bevel); the rows in between are spread evenly."""
    i = round(s * n)
    if i <= 0:
        return 0.0, 0.0
    if i >= n:
        return 1.0, 0.0
    inner = CHAMFER + (1 - 2 * CHAMFER) * (i - 1) / max(n - 2, 1)
    return inner, 1.0


def shell_frame(centre, radii, rot):
    """World -> unit-sphere space of an ellipsoid (like PartBuilder.ellipsoid), so the ellipsoid feature masks
    (c.u) work on a shell patch too."""
    return (Matrix.Translation(centre) @ rot.to_4x4() @ Matrix.Diagonal((radii[0], radii[1], radii[2], 1.0))).inverted()


def dome_patch(b, name, centre, radii, rot, dir_fn, thick, bone, style, nu=22, nv=14, chain=None, outline=None):
    """A hard plate: a patch of the ellipsoid (centre, radii in Blender order, rot) thickened outwards by `thick`,
    its inner face sunk PLATE_EMBED into what lies below, with a bevelled rim. `dir_fn(a, b)` (a, b in -1..1) gives
    the local direction of each patch point (normalised onto the ellipsoid); `outline(a, b)` may reshape the patch
    (e.g. narrower at the bottom)."""
    rot = rot or Matrix.Identity(3)
    centre = Vector(centre)

    def surf(offset, bevel):
        def fn(u, v):
            uu, eu = _edge_param(u, nu)
            vv, ev = _edge_param(v, nv)
            a, bb = 2 * uu - 1, 2 * vv - 1
            if outline:
                a, bb = outline(a, bb)
            d = Vector(dir_fn(a, bb)).normalized()
            scale = 1 + offset * (1 - bevel * (1 - min(eu, ev))) / max(min(radii), 1e-6)
            local = Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * scale
            return centre + rot @ local
        return fn
    b.slab(name, surf(thick, BEVEL), surf(-PLATE_EMBED, 0.0), nu, nv, frame=shell_frame(centre, radii, rot), bone=bone,
           chain=chain, style=style)
    return b.parts[-1]


def limb_shell(b, name, a, c, r_a, r_c, front, half_angle, t0, t1, thick, chain, style, nu=18, nv=10):
    """A hard plate wrapping the `front` side of a straight limb from a to c (radii r_a, r_c) between t0 and t1,
    with a bevelled rim (shin guards, thigh plates, forearm bracers)."""
    a, c = Vector(a), Vector(c)
    axis = c - a
    length = axis.length
    d = axis / length
    ref = (Vector(front) - d * Vector(front).dot(d)).normalized()
    side = d.cross(ref)

    def surf(offset, bevel):
        def fn(u, v):
            uu, eu = _edge_param(u, nu)
            vv, ev = _edge_param(v, nv)
            ang = math.radians(-half_angle + 2 * half_angle * uu)
            t = t0 + (t1 - t0) * vv
            r = r_a + (r_c - r_a) * t + offset * (1 - bevel * (1 - min(eu, ev)))
            return a + d * (length * t) + (ref * math.cos(ang) + side * math.sin(ang)) * r
        return fn
    b.slab(name, surf(thick, BEVEL), surf(-PLATE_EMBED, 0.0), nu, nv, chain=chain, style=style)
    part = b.parts[-1]
    part.limb = (a, d, length, ref, side)
    return part


class Layer:
    """Parts fused together into one shell group. `parts`: part names (None: every part no other layer names);
    `voxel` / `smooth`: remesh size and smoothing passes (None: the asset's); `density`: triangles per area relative
    to the other layers; `tris`: a fixed budget instead (eyes, lids); `uv_scale`: relative texel density."""

    def __init__(self, name, parts=None, voxel=None, smooth=None, density=1.0, tris=None, uv_scale=1.0):
        self.name = name
        self.parts = set(parts) if parts is not None else None
        self.voxel = voxel
        self.smooth = smooth
        self.density = density
        self.tris = tris              # a fixed triangle budget instead of the area share (small, important parts)
        self.uv_scale = uv_scale      # texel density relative to the other layers (unwrap="smart" only)


class LayerIndex:
    """Which layer each point belongs to, and the nearest source part within that layer."""

    def __init__(self, builder, layers):
        default = next((i for i, layer in enumerate(layers) if layer.parts is None), None)
        self.layer_of_pid = np.zeros(len(builder.parts), dtype=np.int32)
        for part in builder.parts:
            li = next((i for i, layer in enumerate(layers) if layer.parts is not None and part.name in layer.parts), default)
            if li is None:
                raise ValueError(f"part {part.name} is in no layer")
            self.layer_of_pid[part.pid] = li
        face_pid = np.asarray(builder.face_pid)
        self.views = []
        for li in range(len(layers)):
            bm = builder.bm.copy()
            bm.faces.ensure_lookup_table()
            keep = self.layer_of_pid[face_pid] == li
            bmesh.ops.delete(bm, geom=[f for f, k in zip(bm.faces, keep) if not k], context="FACES")
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
            bm.faces.ensure_lookup_table()
            self.views.append((bm, BVHTree.FromBMesh(bm), face_pid[keep]))
        self.surfaces = [None] * len(layers)   # BVH of each layer's fused surface (set after fusing)
        self.known = {}                        # point count -> layer id per point, for meshes whose layers are known

    def layer_mesh(self, li):
        mesh = bpy.data.meshes.new(f"Layer{li}")
        self.views[li][0].to_mesh(mesh)
        return mesh

    def set_surface(self, li, mesh):
        verts = [v.co.copy() for v in mesh.vertices]
        polys = [tuple(p.vertices) for p in mesh.polygons]
        self.surfaces[li] = BVHTree.FromPolygons(verts, polys)

    def layers_of(self, points):
        known = self.known.get(len(points))
        if known is not None:
            return known
        out = np.zeros(len(points), dtype=np.int32)
        for i, p in enumerate(points):
            v = Vector(p)
            best, best_d = 0, float("inf")
            for li, tree in enumerate(self.surfaces):
                hit = tree.find_nearest(v)
                if hit[0] is not None and hit[3] < best_d:
                    best, best_d = li, hit[3]
            out[i] = best
        return out

    def assign(self, points):
        layer = self.layers_of(points)
        pid = np.zeros(len(points), dtype=np.int32)
        for i, p in enumerate(points):
            _, tree, fpid = self.views[layer[i]]
            hit = tree.find_nearest(Vector(p))
            pid[i] = fpid[hit[2]] if hit[2] is not None else 0
        return pid

    def register(self, mesh):
        """Records the layer of each vertex and face of `mesh` (from its Layer attribute), so lookups on it (styles,
        skin weights, UV seams) need no distance test."""
        attr = mesh.attributes.get("Layer")
        if attr is None:
            return
        vals = np.empty(len(mesh.vertices), dtype=np.float32)
        attr.data.foreach_get("value", vals)
        lv = np.rint(vals).astype(np.int32)
        self.known[len(mesh.vertices)] = lv
        self.known[len(mesh.polygons)] = np.array([lv[p.vertices[0]] for p in mesh.polygons], dtype=np.int32)

    def vertex_layers(self, mesh):
        return self.known.get(len(mesh.vertices))


_ORIGINAL_ASSIGN = C.assign_parts


def _install(builder, index):
    def assign(points, b):
        if b is builder:
            return index.assign(np.asarray(points))
        return _ORIGINAL_ASSIGN(points, b)
    C.assign_parts = assign


def _tag_layer(mesh, li):
    attr = mesh.attributes.new("Layer", "FLOAT", "POINT")
    attr.data.foreach_set("value", np.full(len(mesh.vertices), float(li), dtype=np.float32))


def _join(meshes, name):
    bm = bmesh.new()
    for m in meshes:
        bm.from_mesh(m)
    out = bpy.data.meshes.new(name)
    bm.to_mesh(out)
    bm.free()
    return out


def _uv_mask(mesh, faces, size):
    """Pixels (row 0 = bottom, as Blender stores them) covered by the UV triangles of `faces`."""
    mesh.calc_loop_triangles()
    uv = mesh.uv_layers.active.data
    keep = np.zeros(len(mesh.polygons), dtype=bool)
    keep[list(faces)] = True
    mask = np.zeros((size, size), dtype=bool)
    for tri in mesh.loop_triangles:
        if not keep[tri.polygon_index]:
            continue
        p = np.array([tuple(uv[li].uv) for li in tri.loops]) * size - 0.5
        x0, y0 = np.maximum(np.floor(p.min(axis=0)).astype(int), 0)
        x1, y1 = np.minimum(np.ceil(p.max(axis=0)).astype(int), size - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        (ax, ay), (bx, by), (cx, cy) = p
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12:
            continue
        w0 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / den
        w1 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / den
        inside = (w0 >= -1e-3) & (w1 >= -1e-3) & (1 - w0 - w1 >= -1e-3)
        mask[ys[inside], xs[inside]] = True
    return mask


def _grow_owner(owner, steps):
    """Spreads layer ownership into unowned pixels, one pixel per step (the bake margins between islands)."""
    for _ in range(steps):
        free = owner < 0
        if not free.any():
            break
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            shifted = np.roll(np.roll(owner, dy, axis=0), dx, axis=1)
            take = free & (shifted >= 0) & (owner < 0)
            owner[take] = shifted[take]
    return owner


def bake_layered_maps(highs, low, face_layer, out_dir, prefix, size, extrusion, ray, bake_faces=None, low_size=None,
                      samples=8):
    """common.bake_maps, but each layer of the game mesh is baked from its own layer of the source only. With one
    shared source a ray from the suit could hit a plate hovering above it (a pauldron rim, the inside of a shell) and
    print white specks and jagged plate edges into the suit. The layers' images are combined by UV coverage."""
    os.makedirs(out_dir, exist_ok=True)
    C._cycles(samples)
    faces_all = list(bake_faces) if bake_faces is not None else list(range(len(low.data.polygons)))
    names = ("albedo", "emission", "normal", "ao", "rough", "fam")
    result = {n: np.zeros((size, size, 4), dtype=np.float32) for n in names}
    owner = np.full((size, size), -1, dtype=np.int32)
    per_layer = []
    bake = bpy.context.scene.render.bake
    for li, high in enumerate(highs):
        faces = [f for f in faces_all if face_layer[f] == li]
        if not faces or high is None:
            per_layer.append(None)
            continue
        target, _ = C._bake_target(low, faces)
        mat_high = high.data.materials[0]
        bake_mat = C.new_material(f"{prefix}_BakeTarget{li}")
        target.data.materials.clear()
        target.data.materials.append(bake_mat)
        node = bake_mat.node_tree.nodes.new("ShaderNodeTexImage")
        bake_mat.node_tree.nodes.active = node
        bake.use_selected_to_active = True
        bake.cage_extrusion = extrusion
        bake.max_ray_distance = ray
        bake.margin = max(4, size // 128)
        bake.use_clear = True
        bake.target = "IMAGE_TEXTURES"
        for o in bpy.context.scene.objects:
            o.select_set(False)
        high.hide_render = False
        high.select_set(True)
        target.select_set(True)
        bpy.context.view_layer.objects.active = target
        maps = {
            "albedo": C._bake_one("EMIT", size, "Linear Rec.709", prefix, "Albedo", node, mat_high, "albedo"),
            "emission": C._bake_one("EMIT", size, "Linear Rec.709", prefix, "Emission", node, mat_high, "emission"),
            "normal": C._bake_one("NORMAL", size, "Non-Color", prefix, "Normal", node, mat_high),
            "ao": C._bake_one("EMIT", size, "Non-Color", prefix, "AO", node, mat_high, "ao"),
            "rough": C._bake_one("EMIT", size, "Non-Color", prefix, "Rough", node, mat_high, "rough"),
            "fam": C._bake_one("EMIT", size, "Non-Color", prefix, "Fam", node, mat_high, "fam"),
        }
        bpy.data.objects.remove(target)
        mask = _uv_mask(low.data, faces, size)
        owner[mask & (owner < 0)] = li
        per_layer.append(maps)
        C.log(prefix, "baked layer", li, "faces", len(faces))
    owner = _grow_owner(owner, max(4, size // 128) + 2)
    first = next(i for i, m in enumerate(per_layer) if m is not None)
    owner[owner < 0] = first
    for li, maps in enumerate(per_layer):
        if maps is None:
            continue
        sel = owner == li
        for n in names:
            result[n][sel] = maps[n][sel]
    mask = np.dstack([result["ao"][..., 0], 1.0 - result["rough"][..., 0], result["fam"][..., 0], result["fam"][..., 1]])
    final = {"albedo": result["albedo"], "emission": result["emission"], "normal": result["normal"],
             "mask": np.clip(mask, 0, 1).astype(np.float32)}
    spaces = {"albedo": "sRGB", "emission": "sRGB", "normal": "Non-Color", "mask": "Non-Color"}
    suffixes = {"albedo": "Albedo", "emission": "Emission", "normal": "Normal", "mask": "Mask"}
    paths = {}
    for key, arr in final.items():
        paths[key] = C._save(arr, os.path.join(out_dir, f"{prefix}_{suffixes[key]}.png"), spaces[key])
    if low_size:
        low_dir = os.path.join(out_dir, "Low")
        os.makedirs(low_dir, exist_ok=True)
        for key, arr in final.items():
            C._save(C.downsample(arr, size // low_size, normal=key == "normal"), os.path.join(low_dir, f"{prefix}_{suffixes[key]}.png"),
                    spaces[key])
    return paths


def _split_high(high, layer_count):
    """One source object per layer (by the Layer attribute), sharing the bake material."""
    attr = high.data.attributes["Layer"]
    vals = np.empty(len(high.data.vertices), dtype=np.float32)
    attr.data.foreach_get("value", vals)
    lv = np.rint(vals).astype(np.int32)
    out = []
    for li in range(layer_count):
        bm = bmesh.new()
        bm.from_mesh(high.data)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if lv[v.index] != li], context="VERTS")
        if not bm.verts:
            bm.free()
            out.append(None)
            continue
        mesh = bpy.data.meshes.new(f"{high.name}_L{li}")
        bm.to_mesh(mesh)
        bm.free()
        obj = C.link(bpy.data.objects.new(f"{high.name}_L{li}", mesh))
        obj.data.materials.append(high.data.materials[0])
        obj.hide_render = True
        out.append(obj)
    return out


def fuse_layers(name, builder, layers, voxel, smooth_iterations=6, extra=None, border_smooth=0):
    """Fuses each layer on its own, installs the layer-aware part lookup and styles the joined source.
    Returns (source mesh, fused layer meshes without detail, LayerIndex)."""
    index = LayerIndex(builder, layers)
    fused = []
    for li, layer in enumerate(layers):
        src = index.layer_mesh(li)
        obj = C.link(bpy.data.objects.new(f"{name}_{layer.name}_Parts", src))
        mesh = C.fuse(obj, layer.voxel or voxel, layer.smooth if layer.smooth is not None else smooth_iterations)
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(src)
        _tag_layer(mesh, li)
        index.set_surface(li, mesh)
        fused.append(mesh)
        C.log(name, "layer", layer.name, "verts", len(mesh.vertices))
    _install(builder, index)
    high_mesh = _join(fused, name + "_High")
    index.register(high_mesh)
    C.detail(high_mesh, builder, extra, border_smooth)
    return high_mesh, fused, index


def source_preview_material(name="SourcePreview", specular=0.4):
    """Lit vertex colours of a styled source (Albedo, Rough, Emit attributes): shape checks without a bake."""
    mat = C.new_material(name)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    for attr, socket in (("Albedo", "Base Color"), ("Emit", "Emission Color")):
        node = nt.nodes.new("ShaderNodeVertexColor")
        node.layer_name = attr
        nt.links.new(node.outputs["Color"], bsdf.inputs[socket])
    rough = nt.nodes.new("ShaderNodeVertexColor")
    rough.layer_name = "Rough"
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(rough.outputs["Color"], sep.inputs["Color"])
    nt.links.new(sep.outputs[0], bsdf.inputs["Roughness"])
    bsdf.inputs["Emission Strength"].default_value = 2.0
    try:
        bsdf.inputs["Specular IOR Level"].default_value = specular
    except KeyError:
        pass
    return mat


def unwrap_weighted(obj, face_layer, layers, margin=0.004, angle=62.0):
    """Unwrap for unmirrored layered assets: a smart projection of the whole mesh (closed shells such as coat
    panels, lids and locks unfold without the long folded strips the part-seam unwrap gives them), every island at
    one texel density times its layer's `uv_scale` (the face sharper than the hidden undersuit), then packed."""
    C.activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.average_islands_scale()
    bpy.ops.uv.seams_from_islands(mark_seams=True, mark_sharp=False)
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    active = np.ones(len(bm.faces), dtype=bool)
    islands = C.uvlayout._islands(bm, active)
    for island in islands:
        scale = layers[face_layer[island[0]]].uv_scale
        if abs(scale - 1.0) < 1e-6:
            continue
        loops = [lp for fi in island for lp in bm.faces[fi].loops]
        cx = sum(lp[uv].uv.x for lp in loops) / len(loops)
        cy = sum(lp[uv].uv.y for lp in loops) / len(loops)
        for lp in loops:
            lp[uv].uv = ((lp[uv].uv.x - cx) * scale + cx, (lp[uv].uv.y - cy) * scale + cy)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    area3d = sum(f.calc_area() for f in bm.faces)
    area_uv = sum(abs(C.uvlayout._uv_signed_area(f, uv)) for f in bm.faces)
    stats = {"islands": len(islands), "unwrap": "smart, layer-weighted", "uv_coverage": round(area_uv, 3),
             "world_per_uv": math.sqrt(area3d / max(area_uv, 1e-9))}
    bpy.ops.object.mode_set(mode="OBJECT")
    C.log(obj.name, "uv", stats)
    return stats, None


def build_layered_asset(name, builder, layers, voxel, target_tris, tex_size, ao_distance, extrusion, ray,
                        emission_strength=2.0, smooth_iterations=6, extra=None, specular=0.4, mirror=False, k=1.0,
                        families=C.FAMILY_ORDER, low_size=None, preview_detail=(1.0, 0.6), family_set=None,
                        edge_gain=0.35, border_smooth=0, symmetric=True, unwrap="parts"):
    """common.build_mesh_asset with per-layer fusing and decimation (see Layer). Returns (MeshAsset, LayerIndex);
    the index stays installed in common.assign_parts, so skin weights made afterwards are layer-aware too."""
    high_mesh, fused, index = fuse_layers(name, builder, layers, voxel, smooth_iterations, extra, border_smooth)
    C.shade_smooth(high_mesh)
    high = C.link(bpy.data.objects.new(name + "_High", high_mesh))
    high.data.materials.append(C.high_bake_material(edge_gain=edge_gain, k=k, families=families, family_set=family_set))
    C.bake_vertex_ao(high, ao_distance)

    weights = np.array([0.0 if layer.tris else sum(p.area for p in m.polygons) * layer.density for m, layer in zip(fused, layers)])
    fixed = sum(layer.tris or 0 for layer in layers)
    budget = float(target_tris - fixed)
    scale_fixed = 1.0
    for _ in range(5):
        pieces = [C.decimate(m.copy(), int(layer.tris * scale_fixed) if layer.tris else max(24, int(budget * w / weights.sum())),
                             f"{name}_L{li}", symmetric)
                  for li, (m, w, layer) in enumerate(zip(fused, weights, layers))]
        low_mesh = _join(pieces, name)
        for m in pieces:
            bpy.data.meshes.remove(m)
        if mirror:
            C.symmetrize(low_mesh)
        low_mesh.calc_loop_triangles()
        count = len(low_mesh.loop_triangles)
        if count <= target_tris:
            break
        budget *= target_tris / count * 0.995
        scale_fixed *= target_tris / count * 0.995
        bpy.data.meshes.remove(low_mesh)
    for m in fused:
        bpy.data.meshes.remove(m)
    C.shade_smooth(low_mesh)
    low = C.link(bpy.data.objects.new(name, low_mesh))
    index.register(low_mesh)
    if unwrap == "smart":
        uv_stats, bake_faces = unwrap_weighted(low, index.known[len(low_mesh.polygons)], layers)
    else:
        uv_stats, bake_faces = C.uvlayout.unwrap(low, builder, mirror=mirror)
    out_dir = os.path.join(C.EXPORTS, name)
    highs = _split_high(high, len(layers))
    face_layer = index.known[len(low_mesh.polygons)]
    textures = bake_layered_maps(highs, low, face_layer, out_dir, name, tex_size, extrusion, ray, bake_faces=bake_faces,
                                 low_size=low_size)
    for o in highs:
        if o is not None:
            mesh = o.data
            bpy.data.objects.remove(o)
            bpy.data.meshes.remove(mesh)
    tiles = C.detailmaps.ensure()
    details = {f: {"map": os.path.basename(tiles[f]), "tiling": C.detail_tiling(uv_stats["world_per_uv"], f, k),
                   "mask": ch} for f, ch in zip(families[:2], ("B", "A"))}
    preview = [(tiles[f], details[f]["tiling"], s, details[f]["mask"]) for f, s in zip(families[:2], preview_detail)]
    low.data.materials.clear()
    low.data.materials.append(C.game_material(name + "_Mat", textures, emission_strength, specular, preview))
    high.hide_render = True
    high.hide_viewport = True
    high_mesh.calc_loop_triangles()
    asset = C.MeshAsset(name, high, low, textures, len(high_mesh.loop_triangles), C.tri_count(low), uv_stats, details)
    C.log(name, "high tris", asset.high_tris, "low tris", asset.low_tris, "layers", [layer.name for layer in layers])
    return asset, index


def drop_layer_attr(obj):
    """The Layer attribute only steers the build; the FBX does not need it."""
    attr = obj.data.attributes.get("Layer")
    if attr is not None:
        obj.data.attributes.remove(attr)
