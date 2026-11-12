"""Round 3 unwrap: fewer, bigger islands at one texel density, and mirrored halves for symmetric creatures.

Round 2 used Smart UV Project, which chops organic shapes into many small planar islands; the margins between
them wasted texture and the islands had uneven density. Here:

1. Seams come from the model itself: part boundaries (a leg meets the thorax), plus one cut along the underside of
   every tube and shard so it unrolls into a strip. Rocks keep a smart projection of their hewn facets as seams.
2. Each island is unwrapped with minimum stretch; an island that folds over itself falls back to smart projection.
3. Islands are scaled to one texel density and packed.
4. Mirror (creatures): only the right half (x >= 0) is unwrapped and packed; every left face copies the UVs of its
   mirror twin. The texture then holds one half at twice the density. Left faces stay in the mesh; the bake only
   writes the right half (see common.bake_maps), and tangent-space normals mirror correctly through the tangent sign.
"""

import math

import bmesh
import bpy
import numpy as np
from mathutils import Vector
from mathutils.kdtree import KDTree

import common as C

MIRROR_EPS = 1e-5


def _face_parts(bm, builder):
    centres = np.array([tuple(f.calc_center_median()) for f in bm.faces])
    return C.assign_parts(centres, builder)


def _mirror_map(bm, tol):
    """Index of each vertex's mirror twin across x = 0, or -1."""
    tree = KDTree(len(bm.verts))
    for v in bm.verts:
        tree.insert(v.co, v.index)
    tree.balance()
    out = np.full(len(bm.verts), -1, dtype=np.int64)
    for v in bm.verts:
        co, idx, dist = tree.find(Vector((-v.co.x, v.co.y, v.co.z)))
        if idx is not None and dist < tol:
            out[v.index] = idx
    return out


def _tube_angle(part, points):
    """Angle of each point around the part's axis, 0 on the underside (towards -Z)."""
    axis = part.axis
    seg_a, seg_b = axis[:-1], axis[1:]
    best = np.full(len(points), np.inf)
    radial = np.zeros_like(points)
    tangent = np.zeros_like(points)
    for a, b in zip(seg_a, seg_b):
        ab = b - a
        length2 = max(ab @ ab, 1e-12)
        s = np.clip(((points - a) @ ab) / length2, 0, 1)
        proj = a + s[:, None] * ab
        d = np.linalg.norm(points - proj, axis=1)
        better = d < best
        best[better] = d[better]
        radial[better] = (points - proj)[better]
        tangent[better] = ab / math.sqrt(length2)
    down = np.array((0.0, 0.0, -1.0))
    ref = down - tangent * (tangent @ down)[:, None]
    weak = np.linalg.norm(ref, axis=1) < 0.2
    alt = np.array((0.0, 1.0, 0.0))
    ref[weak] = (alt - tangent[weak] * (tangent[weak] @ alt)[:, None])
    ref /= np.maximum(np.linalg.norm(ref, axis=1, keepdims=True), 1e-9)
    side = np.cross(tangent, ref)
    return np.arctan2(np.einsum("ij,ij->i", radial, side), np.einsum("ij,ij->i", radial, ref))


def _mark_seams(bm, face_pid, builder, active):
    """Part boundaries plus a lengthwise cut on the underside of tubes. Returns the set of parts that want a
    smart projection (closed blobs such as rocks)."""
    parts = builder.parts
    face_side = {}
    for f in bm.faces:
        if not active[f.index]:
            continue
        part = parts[face_pid[f.index]]
        if part.axis is not None and part.kind in ("tube", "shard"):
            face_side[f.index] = None
    # Angle per tube face, computed per part in one numpy call.
    by_part = {}
    for fi in face_side:
        by_part.setdefault(face_pid[fi], []).append(fi)
    for pid, faces in by_part.items():
        centres = np.array([tuple(bm.faces[fi].calc_center_median()) for fi in faces])
        ang = _tube_angle(parts[pid], centres)
        for fi, a in zip(faces, ang):
            face_side[fi] = a
    for e in bm.edges:
        e.seam = False
        lf = [f for f in e.link_faces if active[f.index]]
        if len(lf) != 2:
            continue
        a, b = lf
        if face_pid[a.index] != face_pid[b.index]:
            e.seam = True
            continue
        sa, sb = face_side.get(a.index), face_side.get(b.index)
        if sa is not None and sb is not None and abs(sa) < math.pi / 2 and abs(sb) < math.pi / 2 and (sa < 0) != (sb < 0):
            e.seam = True
    return {p.pid for p in parts if p.kind == "ellipsoid"}


def _islands(bm, active):
    """Face index lists of the connected regions of active faces, cut by seams."""
    seen = np.zeros(len(bm.faces), dtype=bool)
    out = []
    for f in bm.faces:
        if not active[f.index] or seen[f.index]:
            continue
        stack, island = [f], []
        seen[f.index] = True
        while stack:
            cur = stack.pop()
            island.append(cur.index)
            for e in cur.edges:
                if e.seam:
                    continue
                for nb in e.link_faces:
                    if active[nb.index] and not seen[nb.index]:
                        seen[nb.index] = True
                        stack.append(nb)
        out.append(island)
    return out


def _uv_signed_area(face, uv):
    loops = face.loops
    a = loops[0][uv].uv
    total = 0.0
    for i in range(1, len(loops) - 1):
        b = loops[i][uv].uv
        c = loops[i + 1][uv].uv
        total += (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)
    return total * 0.5


def _clamp_density(bm, uv, islands):
    """Guard against near-degenerate islands that the density averaging blows up: any island whose UV area per
    world area is far above the median is shrunk to the median density around its centre."""
    ratios = []
    for island in islands:
        a3 = sum(bm.faces[fi].calc_area() for fi in island)
        auv = sum(abs(_uv_signed_area(bm.faces[fi], uv)) for fi in island)
        ratios.append((auv / max(a3, 1e-12), island))
    median = float(np.median([r for r, _ in ratios]))
    for ratio, island in ratios:
        if ratio > median * 3.0:
            s = math.sqrt(median / ratio)
            loops = [l for fi in island for l in bm.faces[fi].loops]
            cx = sum(l[uv].uv.x for l in loops) / len(loops)
            cy = sum(l[uv].uv.y for l in loops) / len(loops)
            for l in loops:
                l[uv].uv = ((l[uv].uv.x - cx) * s + cx, (l[uv].uv.y - cy) * s + cy)


# Round 49D, opt-in (the round-49D builders set it; every other asset unwraps exactly as before): bodies crowded with
# small fused parts (crystal clusters, lips, spikes) cut into many small islands, some of which unwrap into rings or
# slivers whose bounding box is mostly empty; the packer then shrinks every island to fit them.
SPARSE_FIX = False


def _uv_islands(bm, uv, active):
    """Face index lists of the regions connected in UV space (shared vertices with the same UV)."""
    parent = list(range(len(bm.faces)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for e in bm.edges:
        lf = [f for f in e.link_faces if active[f.index]]
        if len(lf) != 2:
            continue
        f1, f2 = lf
        same = True
        for v in e.verts:
            u1 = next(l[uv].uv for l in f1.loops if l.vert == v)
            u2 = next(l[uv].uv for l in f2.loops if l.vert == v)
            if (u1 - u2).length > 1e-6:
                same = False
                break
        if same:
            parent[find(f1.index)] = find(f2.index)
    groups = {}
    for f in bm.faces:
        if active[f.index]:
            groups.setdefault(find(f.index), []).append(f.index)
    return list(groups.values())


def _fix_sparse(obj, bm, uv, master, margin, fill=0.12, line=0.02):
    """Islands that fill less than `fill` of their UV box (rings round a fused root, curled strips), that fold over
    themselves or collapse towards a line are re-cut by a smart projection; what still lies on a line afterwards (UV area under `line` x the
    square of the longer side: needle triangles of the decimation) is shrunk round their centre to that limit, so the packer no longer
    scales every island down to fit them."""
    def rows():
        out = []
        for island in _uv_islands(bm, uv, master):
            auv = sum(abs(_uv_signed_area(bm.faces[fi], uv)) for fi in island)
            us = [l[uv].uv for fi in island for l in bm.faces[fi].loops]
            w = max(u.x for u in us) - min(u.x for u in us)
            h = max(u.y for u in us) - min(u.y for u in us)
            out.append((island, auv, w * h, max(w, h) ** 2, us))
        return out

    def finite(*v):
        return all(math.isfinite(x) for x in v)
    # Re-cut: sparse in its box, collapsed towards a line, or folded over itself (more UV area than its box).
    sparse = [fi for island, auv, box, side, _ in rows()
              if not finite(auv, side) or (len(island) > 4 and box > 0 and (auv < fill * box or auv < line * side
                                                                              or auv > box)) for fi in island]
    if sparse:
        _select(bm, sparse)
        bmesh.update_edit_mesh(obj.data)
        bpy.ops.uv.smart_project(angle_limit=math.radians(50), island_margin=margin, scale_to_bounds=False)
        _select(bm, [i for i in range(len(bm.faces)) if master[i]])
        bmesh.update_edit_mesh(obj.data)
        bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.average_islands_scale()
        bm = bmesh.from_edit_mesh(obj.data)
        bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    shrunk = 0
    for island, auv, box, side, us in rows():
        if finite(auv, side) and side > 0 and auv < line * side:
            k = math.sqrt(max(auv, 1e-12) / (line * side))
            cx = sum(u.x for u in us) / len(us)
            cy = sum(u.y for u in us) / len(us)
            for fi in island:
                for l in bm.faces[fi].loops:
                    l[uv].uv = ((l[uv].uv.x - cx) * k + cx, (l[uv].uv.y - cy) * k + cy)
            shrunk += 1
    C.log(obj.name, "uv sparse faces re-cut", len(sparse), "line islands shrunk", shrunk)
    return bm


def _select(bm, faces):
    for f in bm.faces:
        f.select_set(False)
    for fi in faces:
        bm.faces[fi].select_set(True)


def unwrap(obj, builder, mirror=False, margin=0.006, smart_parts_angle=66.0):
    """Unwraps `obj` (object mode in, object mode out). Returns (stats for the report, faces to bake or None)."""
    C.activate(obj)
    bpy.context.scene.tool_settings.use_uv_select_sync = True
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    bm.verts.index_update()
    bm.faces.index_update()
    face_pid = _face_parts(bm, builder)

    n_faces = len(bm.faces)
    master = np.ones(n_faces, dtype=bool)
    twin_of = {}
    if mirror:
        size = max(obj.dimensions)
        mmap = _mirror_map(bm, size * 1e-4)
        by_verts = {}
        for f in bm.faces:
            if f.calc_center_median().x >= -MIRROR_EPS:
                by_verts[frozenset(v.index for v in f.verts)] = f.index
        for f in bm.faces:
            if f.calc_center_median().x >= -MIRROR_EPS:
                continue
            twins = [mmap[v.index] for v in f.verts]
            key = frozenset(twins)
            if min(twins) >= 0 and key in by_verts:
                twin_of[f.index] = by_verts[key]
                master[f.index] = False

    smart_parts = _mark_seams(bm, face_pid, builder, master)
    # Closed blobs (rock parts, chiselled props, unmirrored ellipsoids) get their seams from a smart projection
    # of their facets; mirrored creature bodies are open half-shells and unroll without it.
    if not mirror:
        blob_faces = [f.index for f in bm.faces if master[f.index] and face_pid[f.index] in smart_parts]
        if blob_faces:
            _select(bm, blob_faces)
            bmesh.update_edit_mesh(obj.data)
            bpy.ops.uv.smart_project(angle_limit=math.radians(smart_parts_angle), island_margin=margin,
                                     scale_to_bounds=False)
            bpy.ops.uv.seams_from_islands(mark_seams=True, mark_sharp=False)
            bm = bmesh.from_edit_mesh(obj.data)
            bm.faces.ensure_lookup_table()

    master_faces = [i for i in range(n_faces) if master[i]]
    _select(bm, master_faces)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.unwrap(method="MINIMUM_STRETCH", fill_holes=True, correct_aspect=True, margin=margin)
    bm = bmesh.from_edit_mesh(obj.data)
    if SPARSE_FIX:
        # Round 49D: on bodies crowded with small fused parts the minimum-stretch solver can fail as a whole (every
        # island collapses to a point); angle-based unwrapping does not.
        bm.faces.ensure_lookup_table()
        uv0 = bm.loops.layers.uv.active
        if sum(abs(_uv_signed_area(bm.faces[i], uv0)) for i in master_faces) < 1e-6:
            C.log(obj.name, "uv minimum-stretch collapsed: angle-based unwrap")
            bpy.ops.uv.unwrap(method="ANGLE_BASED", fill_holes=True, correct_aspect=True, margin=margin)
            bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active

    # Islands that fold over themselves (a ring the seams did not open) go back to smart projection.
    islands = _islands(bm, master)
    bad = []
    for island in islands:
        areas = np.array([_uv_signed_area(bm.faces[fi], uv) for fi in island])
        major = np.sign(areas.sum()) or 1.0
        if np.mean(np.sign(areas) != major) > 0.01:
            bad += island
    if bad:
        _select(bm, bad)
        bmesh.update_edit_mesh(obj.data)
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=margin, scale_to_bounds=False)
        bm = bmesh.from_edit_mesh(obj.data)
        bm.faces.ensure_lookup_table()

    _select(bm, master_faces)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.average_islands_scale()
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    _clamp_density(bm, uv, _islands(bm, master))
    if SPARSE_FIX:
        bm = _fix_sparse(obj, bm, uv, master, margin)
        uv = bm.loops.layers.uv.active
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active

    for fi, ti in twin_of.items():
        f, t = bm.faces[fi], bm.faces[ti]
        twin_uv = {l.vert.index: l[uv].uv.copy() for l in t.loops}
        for l in f.loops:
            l[uv].uv = twin_uv[mmap[l.vert.index]]

    # Texel density: world units per UV unit over the unwrapped half.
    area3d = sum(bm.faces[i].calc_area() for i in master_faces)
    area_uv = sum(abs(_uv_signed_area(bm.faces[i], uv)) for i in master_faces)
    stats = {"islands": len(_islands(bm, master)), "folded_fallback_faces": len(bad),
             "mirrored_faces": len(twin_of), "uv_coverage": round(area_uv, 3),
             "world_per_uv": math.sqrt(area3d / max(area_uv, 1e-9))}
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    C.log(obj.name, "uv", stats)
    # Faces the bake writes: with a mirror only the unwrapped half, so the twins do not overwrite it.
    return stats, (master_faces if mirror else None)
