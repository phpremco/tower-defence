"""Helpers for round 14 (build_gravemaw.py, build_env_origins.py). Kept apart from the shared modules, which this
module only imports.

- Palettes: `register_palettes` adds a gold "ore" Crysium palette to crysium.PALETTES at run time (the resource node).
- Styles: Crysium armour plates (chiselled slabs), the Gravemaw's jaw petals, a ringed belly, and white glow masks
  for the spawn origins (Unity tints the Emission map with the element colour).
- PathFZ and spine helpers: a creature whose spine nodes ride along a path in the (forward, up) plane, offset along
  the path normal by their rest height (dive into the ground, breach out of it like a whale).
- Two-material creatures: skin two baked assets (body and crystal armour) to one rig and join them into one mesh with
  two materials; a matching LOD.
- Preview helpers: a ground plane with holes (so a pit below z = 0 shows), a translucent liquid sheet, and an emission
  tint for mask-only maps.
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

import boss_kit as B
import chitin as H
import common as C
import crysium as Y

# ---------------------------------------------------------------------------------------------------------------
# Palettes
# ---------------------------------------------------------------------------------------------------------------

GOLD = ((np.array((0.2, 0.08, 0.04)), np.array((0.95, 0.62, 0.16)), np.array((1.0, 0.93, 0.7))),
        (np.array((1.0, 0.66, 0.18)), np.array((1.0, 0.9, 0.6))))


def register_palettes():
    """The resource node's Crysium: gold-amber ore, so the harvest vein never reads as decor crystal."""
    Y.PALETTES.setdefault("gold", GOLD)


def _unit(L):
    return L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def crystal_plate(palette="pink", strength=0.7, cells=14, seed=0, cut=None, cut_glow=1.0):
    """A slab of Crysium armour (a chiselled ellipsoid): violet root at the base, pink body, icy rim; the facet edges
    carry the inner light, the faces stay a step darker so the shape reads. `cut` = (point, normal) marks a sawn
    face (resource node): points near that plane get a bright smooth cut surface."""
    (root, body, tip), (glow_body, glow_tip) = Y.PALETTES[palette]

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        e = C.voronoi_edge(u, C.fibonacci_sphere(cells, seed=seed))
        edge = 1 - C.smoothstep(0.0, 0.06, e)
        low = C.smoothstep(-0.9, 0.1, u[:, 2])
        rim = C.smoothstep(0.7, 0.97, np.hypot(u[:, 0], u[:, 1])) * C.smoothstep(-0.3, 0.4, u[:, 2])
        col = C.lerp3(C.lerp3(root, body, low), tip, rim * 0.8)
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(s.p[idx], 9, seed + 3)))
        crack *= C.smoothstep(-0.2, 0.3, C.value_noise(s.p[idx], 2.5, seed + 4))
        col = C.lerp3(col, root * 0.4, crack * 0.7)
        col = col * (0.62 + 0.18 * (0.5 + 0.5 * C.value_noise(s.p[idx], 4, seed + 5)))[:, None]
        glow = C.lerp3(glow_body, glow_tip, rim)
        amount = 0.08 + 0.25 * low + 0.55 * edge + 0.3 * rim
        hot = np.zeros(len(idx))
        if cut is not None:
            cp, cn = np.asarray(cut[0], float), np.asarray(cut[1], float)
            d = (s.p[idx] - cp) @ (cn / np.linalg.norm(cn))
            hot = (1 - C.smoothstep(0.0, 0.015, np.abs(d))) * C.smoothstep(0.6, 0.9, s.n[idx] @ (cn / np.linalg.norm(cn)))
            col = C.lerp3(col, tip, hot * 0.8)
            amount = amount + hot * cut_glow
        s.albedo[idx] = col
        s.emit[idx] = np.clip(glow * (amount * strength * (1 - 0.6 * crack))[:, None], 0, 1)
        s.rough[idx] = 0.3 + 0.3 * (1 - edge) * 0.5 + 0.2 * crack - 0.2 * hot
        s.disp[idx] = -0.004 * crack
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        s.flow[idx] = L[:, 2] * part.radii[2]
    return style


def petal(pal, k, base, axis, centre, length, half_width, seam_glow=1.0, inner_glow=0.9, bands=6):
    """A jaw petal (a slab between an outer and an inner shell round the maw axis): dark plate with a pale worn
    keel, growth bands across, glowing seams along both side edges on the outside (light leaking between the closed
    petals), a hot fleshy inner face and a pale hooked tip. `base`, `axis`, `centre` (unit radial direction of the
    petal) in Blender space; `half_width(v)` the petal's half angle along its length."""
    base = np.asarray(base, float)
    axis = np.asarray(axis, float) / np.linalg.norm(axis)
    centre = np.asarray(centre, float) / np.linalg.norm(centre)
    side = np.cross(axis, centre)

    def style(s, idx, part):
        rel = s.p[idx] - base
        v = np.clip((rel @ axis) / length, 0, 1)
        rad = rel - np.outer(rel @ axis, axis)
        r = np.linalg.norm(rad, axis=1)
        ang = np.arctan2(rad @ side, rad @ centre)
        w = np.array([half_width(x) for x in v])
        edge = np.clip(np.abs(ang) / np.maximum(w, 1e-3), 0, 1)
        n = s.n[idx]
        outward = np.einsum("ij,ij->i", n, _unit(rad + 1e-9))
        outer = C.smoothstep(-0.1, 0.3, outward)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.14, 40 / k, 17)
        band = 1 - C.smoothstep(0.0, 0.12, np.abs(((v * bands) % 1.0) - 0.5) - 0.38)
        keel = 1 - C.smoothstep(0.0, 0.18, edge)
        col = C.lerp3(col, pal.seam, band * 0.55 * outer)
        col = C.lerp3(col, pal.bone, keel * 0.55 * outer * C.smoothstep(0.1, 0.6, v))
        seam = C.smoothstep(0.78, 0.98, edge) * outer
        tip = C.smoothstep(0.8, 1.0, v)
        col = C.lerp3(col, pal.bone, tip * 0.8)
        flesh = C.lerp3(pal.membrane, pal.glow * 0.7, 0.4 + 0.4 * (1 - v))
        col = C.lerp3(col, flesh, (1 - outer))
        col = C.lerp3(col, pal.glow * 0.75, seam * 0.8)
        hot = np.clip(seam * seam_glow + (1 - outer) * inner_glow * (1 - 0.6 * v), 0, 1)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = (-0.003 * k * band + 0.003 * k * keel) * outer
        s.rough[idx] = 0.62 - 0.25 * keel * outer - 0.2 * hot
        H.family(s, idx, chitin=outer, membrane=1 - outer, flow=v * length)
    return style


def belly_rings(pal, k, rings, glow=0.5):
    """The underside of a segmented body: dark armoured rings at the given forward positions (Blender -Y), the soft
    membrane between them dimly lit, lighter towards the flanks."""
    rings = np.asarray(rings, float)

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        f = -p[:, 1]
        d = np.min(np.abs(f[:, None] - rings[None, :]), axis=1)
        gap = 1 - C.smoothstep(0.0, 0.08 * k / 4.5, d)
        col = C.chitin_gradient(n, pal.side, pal.dark, pal.belly) * H.mottle(s, idx, 0.12, 30 / k, 23)
        col = C.lerp3(col, pal.membrane, gap * 0.8)
        s.albedo[idx] = col
        under = C.smoothstep(0.2, -0.6, n[:, 2])
        s.emit[idx] = pal.glow[None, :] * (gap * glow * (0.35 + 0.65 * under))[:, None]
        s.disp[idx] = -0.006 * k / 4.5 * gap
        s.rough[idx] = 0.6 - 0.2 * gap
        H.family(s, idx, chitin=1 - gap, membrane=gap, flow=f)
    return style


def mask_glow(style, mask_fn):
    """Wraps a style: after it runs, the emission becomes a white 0..1 mask from `mask_fn(s, idx, part)` (Unity
    tints it with the element colour), and the albedo near the glow is pushed towards a neutral warm-dark so the
    tint is not fighting a baked colour."""
    def wrapped(s, idx, part):
        style(s, idx, part)
        m = np.clip(mask_fn(s, idx, part), 0, 1)
        s.emit[idx] = np.repeat(m[:, None], 3, axis=1)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.5, 0.46, 0.44)), m * 0.6)
        s.rough[idx] = s.rough[idx] - 0.2 * m
    return wrapped


# ---------------------------------------------------------------------------------------------------------------
# Paths and spines
# ---------------------------------------------------------------------------------------------------------------

class PathFZ:
    """A path in the creature's (forward, up) plane, parameterised by arc length `u`. The flat section is anchored
    so that u equals the forward coordinate there (`anchor` 'start': the first point; 'end': the last point), so a
    node whose u is its rest forward position sits on the ground at rest. Beyond either end the path continues
    along its end tangent."""

    def __init__(self, points, anchor="start", samples=12, smooth=True):
        pts = [Vector((f, 0.0, z)) for f, z in points]
        dense = C.catmull_rom(pts, samples) if smooth else pts
        self.f = np.array([p.x for p in dense])
        self.z = np.array([p.z for p in dense])
        seg = np.hypot(np.diff(self.f), np.diff(self.z))
        self.s = np.concatenate([[0.0], np.cumsum(seg)])
        self.total = float(self.s[-1])
        self.offset = self.f[0] if anchor == "start" else self.f[-1] - self.total

    def at(self, u):
        """(forward, up, tangent_f, tangent_z) at arc parameter u."""
        s = u - self.offset
        if s <= 0:
            tf, tz = self._tangent(0)
            return self.f[0] + tf * s, self.z[0] + tz * s, tf, tz
        if s >= self.total:
            tf, tz = self._tangent(len(self.s) - 2)
            d = s - self.total
            return self.f[-1] + tf * d, self.z[-1] + tz * d, tf, tz
        i = int(np.searchsorted(self.s, s) - 1)
        i = max(0, min(i, len(self.s) - 2))
        a = (s - self.s[i]) / max(self.s[i + 1] - self.s[i], 1e-9)
        tf, tz = self._tangent(i)
        return self.f[i] + (self.f[i + 1] - self.f[i]) * a, self.z[i] + (self.z[i + 1] - self.z[i]) * a, tf, tz

    def _tangent(self, i):
        df, dz = self.f[i + 1] - self.f[i], self.z[i + 1] - self.z[i]
        n = math.hypot(df, dz) or 1.0
        return df / n, dz / n

    def u_at_forward(self, f, z_sign=1):
        """The u where the path crosses z = 0 nearest forward position f (for placing the hole)."""
        sign = np.sign(self.z)
        cross = np.nonzero(np.diff(sign) != 0)[0]
        if not len(cross):
            return None
        best = min(cross, key=lambda i: abs(self.f[i] - f))
        return float(self.s[best] + self.offset)


def ride(path, nodes, D, lateral=None):
    """Positions and up vectors (Blender space) of spine nodes riding `path`: node j with rest (forward, height)
    sits at path(f_j + D) + normal * height_j. `lateral(j)` adds an x offset."""
    pos, ups = [], []
    for j, (f, h) in enumerate(nodes):
        pf, pz, tf, tz = path.at(f + D)
        nf, nz = -tz, tf
        x = lateral(j) if lateral else 0.0
        pos.append(C.G(x, pz + nz * h, pf + nf * h))
        ups.append(Vector((0.0, -nf, nz)))
    return pos, ups


def spine_specs(poser, bones, pos, ups, rest_up=Vector((0, 0, 1))):
    """Abs specs for spine bones given as (bone name, node index of its head, node index of its tail)."""
    out = {}
    for name, a, b in bones:
        up = (ups[a] + ups[b]).normalized()
        out[name] = {"abs": C.aim_matrix(pos[a], pos[b] - pos[a], up, poser.rest[name], rest_up)}
    return out


def carried(poser, specs, bone, point):
    """Where a rest-space point attached to `bone` is under the abs spec of that bone."""
    m = specs[bone]["abs"]
    return m @ poser.rest[bone].inverted() @ Vector(point)


def carried_dir(poser, specs, bone, direction):
    m = specs[bone]["abs"]
    return (m.to_3x3() @ poser.rest[bone].to_3x3().inverted()) @ Vector(direction)


def ballistic(origin, velocity, t, gravity=9.0, floor=0.05):
    """Position of a thrown chunk t seconds after release, resting on the floor once it lands."""
    o = Vector(origin)
    v = Vector(velocity)
    p = o + v * t + Vector((0, 0, -0.5 * gravity * t * t))
    landed = p.z < floor and t > 0.05
    if landed:
        # Time of landing, then rest there.
        a, b, c = -0.5 * gravity, v.z, o.z - floor
        disc = max(b * b - 4 * a * c, 0.0)
        tl = (-b - math.sqrt(disc)) / (2 * a)
        p = o + v * tl + Vector((0, 0, -0.5 * gravity * tl * tl))
        p.z = floor
        return p, tl
    return p, None


# ---------------------------------------------------------------------------------------------------------------
# UV repair and a mesh-asset build that uses it
# ---------------------------------------------------------------------------------------------------------------

def _uv_islands(bm, uv, faces):
    """UV islands (lists of face indices) among `faces`: faces joined by an edge whose two loops agree in UV."""
    allowed = set(faces)
    parent = {f: f for f in faces}

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    for fi in faces:
        f = bm.faces[fi]
        for loop in f.loops:
            other = loop.link_loop_radial_next
            if other == loop or other.face.index not in allowed:
                continue
            nxt, onx = loop.link_loop_next, other.link_loop_next
            # The radial loop runs the other way round: its next loop sits on this loop's vertex.
            if (loop[uv].uv - onx[uv].uv).length < 1e-5 and (nxt[uv].uv - other[uv].uv).length < 1e-5:
                ra, rb = find(fi), find(other.face.index)
                if ra != rb:
                    parent[ra] = rb
    groups = {}
    for fi in faces:
        groups.setdefault(find(fi), []).append(fi)
    return list(groups.values())


def fix_uv_slivers(obj, master_faces, margin=0.006, factor=3.0):
    """After uvlayout.unwrap: islands whose UV extent is far larger than their 3D extent at the median density
    (a sliver the minimum-stretch unwrap blew up) are shrunk to that density, the master half is re-packed and the
    mirror twins copy their UVs again. Returns (stats, number of islands shrunk)."""
    from mathutils import kdtree
    C.activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    islands = _uv_islands(bm, uv, master_faces)
    rows = []
    for island in islands:
        loops = [l for fi in island for l in bm.faces[fi].loops]
        us = np.array([tuple(l[uv].uv) for l in loops])
        ps = np.array([tuple(l.vert.co) for l in loops])
        ext_uv = float((us.max(0) - us.min(0)).max())
        ext_3d = float((ps.max(0) - ps.min(0)).max())
        rows.append((island, ext_uv, max(ext_3d, 1e-6), us.mean(0)))
    ratios = np.array([r[1] / r[2] for r in rows])
    weights = np.array([r[2] for r in rows])
    order = np.argsort(ratios)
    cum = np.cumsum(weights[order])
    median = float(ratios[order][np.searchsorted(cum, cum[-1] / 2)])
    shrunk = 0
    for (island, ext_uv, ext_3d, centre), ratio in zip(rows, ratios):
        if ratio > median * factor:
            s = median / ratio
            for fi in island:
                for l in bm.faces[fi].loops:
                    l[uv].uv = ((l[uv].uv.x - centre[0]) * s + centre[0], (l[uv].uv.y - centre[1]) * s + centre[1])
            shrunk += 1
    for f in bm.faces:
        f.select_set(False)
    for fi in master_faces:
        bm.faces[fi].select_set(True)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    # Mirror twins copy their UVs again (as uvlayout.unwrap does).
    size = max(obj.dimensions)
    tree = kdtree.KDTree(len(bm.verts))
    for v in bm.verts:
        tree.insert(v.co, v.index)
    tree.balance()
    mmap = {}
    for v in bm.verts:
        co, idx, dist = tree.find(Vector((-v.co.x, v.co.y, v.co.z)))
        if idx is not None and dist < size * 1e-4:
            mmap[v.index] = idx
    master = set(master_faces)
    by_verts = {frozenset(v.index for v in bm.faces[fi].verts): fi for fi in master_faces}
    for f in bm.faces:
        if f.index in master:
            continue
        twins = [mmap.get(v.index, -1) for v in f.verts]
        key = frozenset(twins)
        if min(twins) >= 0 and key in by_verts:
            t = bm.faces[by_verts[key]]
            twin_uv = {l.vert.index: l[uv].uv.copy() for l in t.loops}
            for l in f.loops:
                l[uv].uv = twin_uv[mmap[l.vert.index]]
    area3d = sum(bm.faces[i].calc_area() for i in master_faces)
    area_uv = 0.0
    for fi in master_faces:
        loops = bm.faces[fi].loops
        a = loops[0][uv].uv
        for i in range(1, len(loops) - 1):
            b, c = loops[i][uv].uv, loops[i + 1][uv].uv
            area_uv += abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    stats = {"uv_coverage": round(area_uv, 3), "world_per_uv": math.sqrt(area3d / max(area_uv, 1e-9)),
             "sliver_islands_shrunk": shrunk, "uv_islands": len(islands)}
    C.log(obj.name, "uv repaired", stats)
    return stats


def build_mesh_asset_dk(name, builder, voxel, target_tris, tex_size, ao_distance, extrusion, ray, emission_strength=2.5,
                        extra=None, specular=0.3, k=1.0, families=C.FAMILY_ORDER, low_size=None, out_dir=None,
                        smooth_iterations=6, edge_gain=0.35):
    """creature_kit2.build_mesh_asset_ex (mirrored creature) with fix_uv_slivers between the unwrap and the bake."""
    import detailmaps
    import uvlayout
    parts_obj = builder.to_object(name + "_Parts")
    high_mesh = C.fuse(parts_obj, voxel, smooth_iterations)
    bpy.data.objects.remove(parts_obj)
    smooth_copy = high_mesh.copy()
    C.detail(high_mesh, builder, extra)
    C.shade_smooth(high_mesh)
    high = C.link(bpy.data.objects.new(name + "_High", high_mesh))
    high.data.materials.append(C.high_bake_material(edge_gain=edge_gain, k=k, families=families))
    C.bake_vertex_ao(high, ao_distance)
    low_mesh = C.decimate(smooth_copy.copy(), target_tris, name, True)
    C.symmetrize(low_mesh)
    low_mesh.calc_loop_triangles()
    over = len(low_mesh.loop_triangles)
    if over > target_tris:
        bpy.data.meshes.remove(low_mesh)
        low_mesh = C.decimate(smooth_copy.copy(), int(target_tris * target_tris / over), name, True)
        C.symmetrize(low_mesh)
    bpy.data.meshes.remove(smooth_copy)
    C.shade_smooth(low_mesh)
    low = C.link(bpy.data.objects.new(name, low_mesh))
    uv_stats, bake_faces = uvlayout.unwrap(low, builder, mirror=True)
    uv_stats.update(fix_uv_slivers(low, bake_faces))
    out_dir = out_dir or os.path.join(C.EXPORTS, name)
    textures = C.bake_maps(high, low, out_dir, name, tex_size, extrusion, ray, bake_faces=bake_faces, low_size=low_size)
    tiles = detailmaps.ensure()
    details = {f: {"map": os.path.basename(tiles[f]), "tiling": C.detail_tiling(uv_stats["world_per_uv"], f, k),
                   "mask": ch} for f, ch in zip(families[:2], ("B", "A"))}
    preview = [(tiles[f], details[f]["tiling"], s, details[f]["mask"]) for f, s in zip(families[:2], (1.0, 0.6))]
    low.data.materials.clear()
    low.data.materials.append(C.game_material(name + "_Mat", textures, emission_strength, specular, preview))
    high.hide_render = high.hide_viewport = True
    high_mesh.calc_loop_triangles()
    asset = C.MeshAsset(name, high, low, textures, len(high_mesh.loop_triangles), C.tri_count(low), uv_stats, details)
    C.log(name, "high tris", asset.high_tris, "low tris", asset.low_tris, "details", details)
    return asset


# ---------------------------------------------------------------------------------------------------------------
# Two-material creatures
# ---------------------------------------------------------------------------------------------------------------

def join_skinned(main, other):
    """Joins the skinned mesh `other` into `main` (same rig): vertex groups merge by name, materials are appended,
    the UV layers share the name UVMap. Returns `main`."""
    for o in (main, other):
        if o.data.uv_layers:
            o.data.uv_layers[0].name = "UVMap"
    other.modifiers.clear()
    C.activate(main)
    other.select_set(True)
    bpy.ops.object.join()
    return main


def build_lod2(lod_name, parts, bones, clips, full_poser, lod_tris, drop, aims, scale_bones=()):
    """LOD1 of a two-material creature. `parts`: [(asset, builder, share of lod_tris)]; each game mesh is decimated
    on its own (keeps its material and UVs), skinned with its builder on the reduced rig, and the pieces are joined.
    Returns (arm, mesh, actions) keyed from the full rig's pose (ScalePoser, so scaled bones stay)."""
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
    arm = C.build_armature(lod_name + "_Rig", lod_bones)

    def remap(b):
        while b in drop:
            b = drop[b]
        return b
    pieces = []
    for i, (asset, builder, share) in enumerate(parts):
        mesh = C.decimate(asset.low.data.copy(), int(lod_tris * share), f"{lod_name}_{i}", symmetric=True)
        C.shade_smooth(mesh)
        obj = C.link(bpy.data.objects.new(f"{lod_name}_{i}", mesh))
        obj.data.materials.clear()
        obj.data.materials.append(asset.low.data.materials[0])
        saved = [(p, list(p.chain), p.bone) for p in builder.parts]
        for p in builder.parts:
            p.chain = list(dict.fromkeys(remap(c) for c in p.chain))
            p.bone = p.chain[0] if p.chain else p.bone
        C.skin(obj, arm, builder)
        for p, chain, bone in saved:
            p.chain, p.bone = chain, bone
        pieces.append(obj)
    lod = pieces[0]
    for other in pieces[1:]:
        join_skinned(lod, other)
    lod.name = lod_name
    lod.data.name = lod_name
    poser = B.ScalePoser(arm, [b for b in scale_bones if b not in drop])
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(full_poser)
        actions[clip] = C.lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop, aims)
    arm.animation_data.action = next(iter(actions.values()))
    return arm, lod, actions


# ---------------------------------------------------------------------------------------------------------------
# Preview helpers
# ---------------------------------------------------------------------------------------------------------------

def ground_with_holes(holes, size=60.0, color=(0.03, 0.035, 0.045), res=240):
    """C.ground, but a grid with round holes [(x, y, radius)] cut out, so pits below z = 0 stay visible."""
    g = C.ground(size=size, color=color)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=res, y_segments=res, size=size / 2)
    kill = []
    for f in bm.faces:
        c = f.calc_center_median()
        for x, y, r in holes:
            if (c.x - x) ** 2 + (c.y - y) ** 2 < r * r:
                kill.append(f)
                break
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(g.data)
    bm.free()
    return g


def liquid_sheet(name, color, alpha=0.7, size=4.0, z=0.0, emission=None, strength=0.0, hole=None):
    """A translucent liquid surface (preview stand-in for TerrainLiquidBuilder's sheets); `hole` (x, y, r) cuts a
    round opening where the prop's throat is."""
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=80, y_segments=80, size=size / 2)
    if hole:
        x, y, r = hole
        kill = [f for f in bm.faces if (f.calc_center_median().x - x) ** 2 + (f.calc_center_median().y - y) ** 2 < r * r]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = C.link(bpy.data.objects.new(name, mesh))
    obj.location.z = z
    mat = C.new_material(name + "_Mat")
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = 0.15
    bsdf.inputs["Alpha"].default_value = alpha
    if emission is not None:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1)
        bsdf.inputs["Emission Strength"].default_value = strength
    try:
        mat.surface_render_method = "BLENDED"
    except AttributeError:
        mat.blend_method = "BLEND"
    mesh.materials.append(mat)
    return obj


def tint_emission(mat, color):
    """Multiplies the material's emission texture by `color` (preview of a white emission mask tinted in Unity)."""
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    link = next((l for l in nt.links if l.to_socket == bsdf.inputs["Emission Color"]), None)
    if link is None:
        return
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    # The Mix node keeps float, vector and colour sockets side by side; pick the colour ones by type.
    a_in, b_in = [s for s in mix.inputs if s.type == "RGBA"][:2]
    out = next(s for s in mix.outputs if s.type == "RGBA")
    mix.inputs[0].default_value = 1.0
    src = link.from_socket
    nt.links.remove(link)
    nt.links.new(src, a_in)
    b_in.default_value = (*color, 1)
    nt.links.new(out, bsdf.inputs["Emission Color"])
