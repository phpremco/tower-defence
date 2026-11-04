"""Helpers for the continent bosses 2-6 (round 16: build_cindermatriarch.py, build_frostwarden.py, build_rotbloom.py,
build_shattered.py, build_sealedone.py). Kept apart from the shared modules, which this module only imports.

- Seal rune: the one glyph of the seal under The Core (a ring of seven broken arcs, seven spokes, inward chevrons
  and a central eye), engraved and lit on any surface by `seal_rune` (an `extra` pass). The Frost Warden wears it
  as ancient markings (the first hint of the seal), the Sealed One carries it on its crown rings and palms.
- Styles: `flesh_glow` (hot flesh between plates), `leaf_petal` (a bloom petal), `scale_band` (wrapped bands round
  a tube: roots, stalks), `wrap` (run a style, then post-process).
- `build_titan`: the whole pipeline of a one- or two-material boss: bake each material with deep_kit's unwrap, skin
  everything to one rig, key the clips with a per-clip scale poser (creature_kit3.ClipPoser), build a matching LOD1
  (per-part rigid sets, per-clip scale keys), join, export, report and save the .blend.
- `previews`: the round-16 render set: 3/4, side, front, top, back, power poses, death, clip strips, LOD1, macros, and
  the game view at play size (1280x720, 51 px per unit, the game camera) next to Gravemaw and Skitters, plus zoomed.
- `blockout`: raw parts in flat colours on the rig, a few stills and clip strips, in about a minute (no bakes).
"""

import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Vector

import boss_kit as B
import chitin as H
import common as C
import creature_kit2 as K2
import creature_kit3 as K3
import deep_kit as DK

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
SIDES = (("L", -1), ("R", 1))


def rot(pitch=0.0, roll=0.0, yaw=0.0):
    """Rotation (degrees): pitch about X (nose up for negative in Blender's -Y-forward frame), roll about Y, yaw
    about Z, applied pitch first."""
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(roll), 3, "Y")
            @ Matrix.Rotation(math.radians(pitch), 3, "X"))


def gp(p, sx):
    return C.G(p[0] * sx, p[1], p[2])


def forward_frame(front, back):
    """Rotation whose local -Y points from `back` to `front` (armour_plate's forward) with local Z up."""
    d = (Vector(front) - Vector(back)).normalized()
    y = -d
    x = Vector((1, 0, 0))
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return Matrix((x, y, z)).transposed()


def frame_from(direction, up=Z):
    """Rotation whose local +Z points along `direction` (for ellipsoids stretched along z)."""
    d = Vector(direction).normalized()
    return d.to_track_quat("Z", "Y").to_matrix()


def ease(a, b, t):
    return K2.ease(a, b, t)


def pulse(t, a, p, b):
    return K2.pulse(t, a, p, b)


# ---------------------------------------------------------------------------------------------------------------
# The seal rune
# ---------------------------------------------------------------------------------------------------------------

SEAL_ARMS = 7


def seal_mask(r, th, arms=SEAL_ARMS, line=1.0):
    """The seal glyph on a unit disc (r 0..1, th from the 'up' axis): outer ring, an inner ring, broken arcs between
    them, spokes, inward chevrons and a ringed eye. 1 on a line. Symmetric about th = 0 (mirror-safe)."""
    w = 0.03 * line

    def ring(r0, width):
        return 1 - C.smoothstep(width * 0.5, width, np.abs(r - r0))
    sector = (th * arms / (2 * math.pi)) % 1.0
    outer = ring(0.93, w * 1.4)
    inner = ring(0.58, w)
    gap = C.smoothstep(0.1, 0.15, sector) * (1 - C.smoothstep(0.85, 0.9, sector))
    arc = ring(0.76, w * 1.1) * gap
    d = np.minimum(sector, 1 - sector) * 2 * math.pi * r / arms
    spoke = (1 - C.smoothstep(w * 0.45, w * 0.9, d)) * C.smoothstep(0.56, 0.6, r) * (1 - C.smoothstep(0.9, 0.94, r))
    half = np.abs(sector - 0.5) * 2 * math.pi * r / arms
    tri_w = np.clip(0.5 - r, 0, 1) * 0.36 + 1e-4
    chevron = (1 - C.smoothstep(tri_w * 0.6, tri_w, half)) * C.smoothstep(0.3, 0.32, r) * (1 - C.smoothstep(0.49, 0.51, r))
    eye = ring(0.18, w) + (1 - C.smoothstep(0.05, 0.07, r))
    mask = np.maximum.reduce([outer, inner, arc, spoke, chevron, np.clip(eye, 0, 1)])
    return mask * (1 - C.smoothstep(0.97, 1.0, r))


def seal_rune(centre, normal, up, radius, glow_col, glow=1.0, depth=0.006, line_col=None, facing=0.35,
              thickness=None, line=1.0, arms=SEAL_ARMS, wear=0.0, seed=0):
    """An `extra` pass: projects the seal glyph along `normal` onto every vertex within `radius` of `centre`
    whose normal faces the same way (dot > `facing`) and within `thickness` of the plane, engraves it (`depth`),
    darkens or tints the line (`line_col`) and lights it in `glow_col`. `up` sets th = 0 (keep it in the mirror
    plane for symmetric creatures). `wear` (0..1) breaks the lines up in patches (old, chipped, frosted over)."""
    c = np.asarray(centre, float)
    n = np.asarray(normal, float)
    n = n / np.linalg.norm(n)
    a = np.asarray(up, float)
    a = a - n * (n @ a)
    a /= np.linalg.norm(a)
    b = np.cross(n, a)
    col = np.asarray(glow_col, float)
    thick = thickness if thickness is not None else radius * 0.6

    def extra(s, pid):
        rel = s.p - c
        z = rel @ n
        x, y = rel @ b, rel @ a
        r = np.hypot(x, y) / radius
        th = np.arctan2(x, y)
        face = C.smoothstep(facing, facing + 0.25, s.n @ n) * (1 - C.smoothstep(thick * 0.8, thick, np.abs(z)))
        m = seal_mask(r, th, arms, line) * face
        if wear:
            keep = C.smoothstep(-0.25, 0.15, C.value_noise(s.p, 3.0 / radius, seed + 11) + 0.35 - wear * 0.7)
            m = m * (0.25 + 0.75 * keep)
        s.disp -= depth * m
        target = np.asarray(line_col, float) if line_col is not None else col * 0.7
        s.albedo = C.lerp3(s.albedo, target, m * 0.85)
        s.emit = np.maximum(s.emit, col[None, :] * (m * glow)[:, None])
        s.rough = np.clip(s.rough - 0.25 * m, 0.08, 0.95)
    return extra


def rune_band(centre, axis, radius, glow_col, glow=1.0, height=0.08, depth=0.004, count=7, phase=0.0, line_col=None):
    """An `extra` pass: a band of small seal glyphs round a vertical-ish `axis` through `centre` (a crown ring, a
    column): `count` glyphs on the cylinder of `radius` between heights -height..height along the axis."""
    c = np.asarray(centre, float)
    ax = np.asarray(axis, float)
    ax = ax / np.linalg.norm(ax)
    ref = np.array((0.0, -1.0, 0.0)) if abs(ax[1]) < 0.9 else np.array((1.0, 0.0, 0.0))
    e1 = ref - ax * (ax @ ref)
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(ax, e1)
    col = np.asarray(glow_col, float)

    def extra(s, pid):
        rel = s.p - c
        h = rel @ ax
        ang = np.arctan2(rel @ e2, rel @ e1) - phase
        rad = np.linalg.norm(rel - np.outer(h, ax), axis=1)
        cell = 2 * math.pi / count
        local = ((ang + cell / 2) % cell) - cell / 2
        x = local * radius / height
        y = h / height
        r = np.hypot(x, y)
        th = np.arctan2(x, y)
        band = (1 - C.smoothstep(radius * 0.12, radius * 0.2, np.abs(rad - radius)))
        m = seal_mask(r, th, line=1.4) * band
        s.disp -= depth * m
        target = np.asarray(line_col, float) if line_col is not None else col * 0.7
        s.albedo = C.lerp3(s.albedo, target, m * 0.85)
        s.emit = np.maximum(s.emit, col[None, :] * (m * glow)[:, None])
    return extra


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def _unit(L):
    return L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)


def wrap(style, post):
    """Runs `style`, then `post(s, idx, part)` (tint, extra glow)."""
    def wrapped(s, idx, part):
        style(s, idx, part)
        post(s, idx, part)
    return wrapped


def flesh_glow(pal, k, glow=1.0, folds=7.0, seed=0, axis=1):
    """Hot flesh exposed between plates (a swollen core): folded, veined, glowing brightest in the creases."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        p = s.p[idx]
        fold = np.abs(np.sin(u[:, axis] * folds + 0.6 * C.value_noise(p, 6 / k, seed)))
        crease = 1 - C.smoothstep(0.0, 0.35, fold)
        vein = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 18 / k, seed + 1)))
        col = C.lerp3(pal.membrane, pal.glow * 0.75, crease * 0.8)
        col = C.lerp3(col, pal.seam, vein * 0.5)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip((0.35 + 0.65 * crease) * glow * (1 - 0.5 * vein), 0, 1)[:, None]
        s.disp[idx] = -0.003 * k * crease
        s.rough[idx] = 0.4
        H.family(s, idx, membrane=1.0, flow=u[:, axis] * part.radii[axis])
    return style


def kiln_crust(pal, k, cells=16, seed=0, glow=1.0, warp=0.35, under=0.35, deep=(0.9, 0.22, 0.04), rifts=0.0):
    """A molten organ under a chilled crust (ellipsoids): large floes of uneven size (warped cells, never a regular
    honeycomb), cracks of varying width that run red-orange where thin and yellow-white where they gape, a darker
    crust with slag grain, a dull glow on the underside. `rifts` (0..1) widens more of the cracks into hot rifts."""
    deep = np.asarray(deep, float)

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        p = s.p[idx]
        rng = np.random.default_rng(seed)
        seeds = C.fibonacci_sphere(cells, seed=seed) * rng.uniform(0.6, 1.4, (cells, 1))
        wu = _unit(u + warp * np.c_[C.value_noise(p, 2.5 / k, seed + 1), C.value_noise(p, 2.5 / k, seed + 2),
                                     C.value_noise(p, 2.5 / k, seed + 3)])
        e = C.voronoi_edge(wu, seeds / np.linalg.norm(seeds, axis=1, keepdims=True))
        width = 0.012 + 0.05 * C.smoothstep(-0.1 - 0.5 * rifts, 0.6 - 0.4 * rifts, C.value_noise(p, 1.8 / k, seed + 4))
        crack = 1 - C.smoothstep(width * 0.4, width, e)
        gape = C.smoothstep(0.035, 0.06, width) * crack
        fine = (1 - C.smoothstep(0.0, 0.03, np.abs(C.value_noise(p, 9 / k, seed + 5)))) * 0.5
        bottom = C.smoothstep(0.2, -0.7, u[:, 2]) * under
        crust = C.chitin_gradient(s.n[idx], pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.18, 30 / k, seed + 6)
        hot_col = C.lerp3(deep[None, :].repeat(len(idx), 0), pal.glow[None, :].repeat(len(idx), 0), gape)
        col = C.lerp3(crust, pal.seam, fine * (1 - crack))
        col = C.lerp3(col, hot_col * 0.75, np.clip(crack + bottom, 0, 1))
        amount = np.clip(crack * (0.55 + 0.45 * gape) + fine * 0.12 + bottom, 0, 1) * glow
        s.albedo[idx] = col
        s.emit[idx] = np.clip(hot_col * amount[:, None], 0, 1)
        s.disp[idx] = -0.006 * k * crack - 0.002 * k * fine
        s.rough[idx] = 0.88 - 0.4 * crack
        H.family(s, idx, rock=1.0 - crack, membrane=crack * 0.5, flow=u[:, 2] * part.radii[2])
    return style


def patch_frame(centre, radii, rot_m):
    """World -> unit-sphere space of an ellipsoid (the frame an ellipsoid part gets), for patch plates."""
    return (Matrix.Translation(Vector(centre)) @ rot_m.to_4x4() @ Matrix.Diagonal((radii[0], radii[1], radii[2], 1.0))).inverted()


def ellipsoid_patch(b, name, centre, radii, rot_m, axes, a_rng, b_rng, sign, gap, thick, bone, style, nu=14, nv=14,
                    bulge=0.0, keel=0.0):
    """A shell plate conforming to an ellipsoid (centre, radii, rotation): the patch where unit-sphere coordinate
    `axes[0]` runs over a_rng and `axes[1]` over b_rng on the `sign` side of the third axis, lifted `gap` off the
    surface along the normal and `thick` deep. With a gap of a few voxels the remesh keeps it a separate island, so a
    bone can scale it away cleanly. `bulge` thickens the middle (a domed plate), `keel` raises a sharp ridge along the
    middle of the a range (rising towards the high-b edge)."""
    ia, ib = axes
    ic = 3 - ia - ib
    R = rot_m
    c = Vector(centre)
    rad = Vector(radii)

    def point(u, v, off):
        a = a_rng[0] + (a_rng[1] - a_rng[0]) * u
        bb = b_rng[0] + (b_rng[1] - b_rng[0]) * v
        w = [0.0, 0.0, 0.0]
        w[ia], w[ib] = a, bb
        w[ic] = sign * math.sqrt(max(1.0 - a * a - bb * bb, 0.01))
        unit = Vector(w).normalized()
        p = Vector((unit.x * rad.x, unit.y * rad.y, unit.z * rad.z))
        n = Vector((unit.x / rad.x, unit.y / rad.y, unit.z / rad.z)).normalized()
        dome = bulge * math.sin(math.pi * u) * math.sin(math.pi * v)
        dome += keel * max(0.0, 1 - abs(2 * u - 1) * 2.2) ** 1.4 * math.sin(math.pi * min(v * 0.85 + 0.1, 1.0)) * (0.5 + 0.5 * v)
        return c + R @ (p + n * (off + dome * (1 if off > gap else 0)))
    return b.slab(name, lambda u, v: point(u, v, gap + thick), lambda u, v: point(u, v, gap), nu, nv,
                  frame=patch_frame(centre, radii, R), bone=bone, style=style)


def shell_plate(pal, k, axes, a_rng, b_rng, crack=0.7, rim_glow=0.8, seed=0, chevron=0.0):
    """Armour plate style for ellipsoid_patch plates: dark plate with growth lines, a pale worn rim, hairline cracks
    leaking hot light, a hot underside (the core shows when it lifts), an optional forward chevron."""
    ia, ib = axes

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u_ = _unit(L)
        p = s.p[idx]
        n = s.n[idx]
        a = (u_[:, ia] - a_rng[0]) / (a_rng[1] - a_rng[0])
        bb = (u_[:, ib] - b_rng[0]) / (b_rng[1] - b_rng[0])
        edge = np.clip(np.minimum.reduce([a, 1 - a, bb, 1 - bb]), 0, 1)
        rim = 1 - C.smoothstep(0.02, 0.12, edge)
        outward = C.smoothstep(-0.1, 0.3, np.einsum("ij,ij->i", n, _unit(p - np.array(part.frame.inverted().translation))))
        growth = 1 - C.smoothstep(0.0, 0.18, np.abs(np.sin(edge * 22 + 0.6 * C.value_noise(p, 8 / k, seed))))
        cr = (1 - C.smoothstep(0.0, 0.035, np.abs(C.value_noise(p, 7 / k, seed + 1)))) * crack
        cr *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 2.5 / k, seed + 2)) * C.smoothstep(0.04, 0.12, edge)
        chev = np.zeros(len(idx))
        if chevron:
            v_line = np.abs(np.abs(a - 0.5) * 1.6 - (bb - 0.25))
            chev = (1 - C.smoothstep(0.04, 0.09, v_line)) * C.smoothstep(0.15, 0.3, bb) * (1 - C.smoothstep(0.7, 0.85, bb))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.12, 40 / k, seed + 3)
        col = C.lerp3(col, pal.seam, growth * 0.35)
        col = C.lerp3(col, pal.bone, rim * 0.6)
        hot = np.clip(cr + chev * chevron + (1 - outward) * 0.9 + rim * rim_glow * 0.35, 0, 1)
        col = C.lerp3(col, pal.glow * 0.75, np.clip(cr + chev * chevron + (1 - outward), 0, 1))
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = -0.004 * k * cr - 0.002 * k * growth * outward
        s.rough[idx] = 0.66 - 0.25 * rim - 0.25 * hot
        H.family(s, idx, chitin=1.0 - 0.6 * hot, flow=edge)
    return style


def leaf_petal(pal, k, glow=1.0, veins=7, seed=0):
    """A bloom petal (a slab, u across 0..1, v along 0..1 from the base): dark veined back, the inner face
    glowing from the base towards a pale frilled rim, dark vein lines fanning out, a hot throat at the base."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        p = s.p[idx]
        n = s.n[idx]
        up = C.smoothstep(-0.2, 0.3, n[:, 2])
        v = np.clip(L[:, 1], 0, 1)
        u = np.clip(L[:, 0], -1, 1)
        vein = 1 - C.smoothstep(0.0, 0.12, np.abs(np.sin((u * veins * 0.5 + 0.15 * C.value_noise(p, 8 / k, seed))
                                                         * math.pi)))
        mid = 1 - C.smoothstep(0.0, 0.08, np.abs(u))
        rim = C.smoothstep(0.82, 1.0, np.maximum(np.abs(u), v))
        inner = C.lerp3(pal.glow * 0.65, pal.bone, C.smoothstep(0.4, 1.0, v) * 0.6)
        inner = C.lerp3(inner, pal.seam, np.maximum(vein, mid) * 0.55)
        back = C.chitin_gradient(n, pal.side, pal.dark, pal.belly) * H.mottle(s, idx, 0.12, 40 / k, seed + 3)
        back = C.lerp3(back, pal.seam, vein * 0.4)
        col = C.lerp3(back, inner, up)
        col = C.lerp3(col, pal.bone, rim * 0.5)
        throat = 1 - C.smoothstep(0.0, 0.35, v)
        hot = up * np.clip((0.25 + 0.75 * throat) * (1 - 0.6 * np.maximum(vein, mid)) + rim * 0.4, 0, 1) * glow
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = 0.002 * k * vein * (1 - up) - 0.002 * k * vein * up
        s.rough[idx] = 0.55 - 0.2 * up
        H.family(s, idx, membrane=up, chitin=1 - up, flow=v)
    return style


def scale_band(pal, k, bands=9.0, glow=0.0, seed=0, rough=0.7):
    """Wrapped bands round a tube (roots, stalks, a tail): lit on top, dark grooves between the bands, the grooves
    glowing faintly when `glow` > 0."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        along = t * C.axis_length(part)
        wob = 0.3 * C.value_noise(s.p[idx], 6 / k, seed)
        groove = 1 - C.smoothstep(0.0, 0.25, np.abs(np.sin((along / max(C.axis_length(part), 1e-6) * bands + wob)
                                                             * math.pi)))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.12, 60 / k, seed + 1)
        col = C.lerp3(col, pal.seam, groove * 0.7)
        s.albedo[idx] = C.lerp3(col, pal.glow * 0.6, groove * glow * 0.6)
        s.emit[idx] = pal.glow[None, :] * (groove * glow)[:, None]
        s.disp[idx] = -0.003 * k * groove
        s.rough[idx] = rough - 0.15 * groove * glow
        H.family(s, idx, chitin=1.0, flow=along)
    return style


def solid_glow(color, strength=1.0, albedo_scale=0.6):
    """A plain glowing part (a core, a slit, a vent mouth) in any colour."""
    color = np.asarray(color, float)

    def style(s, idx, part):
        s.albedo[idx] = color * albedo_scale
        s.emit[idx] = color[None, :] * strength
        s.rough[idx] = 0.35
        H.family(s, idx, membrane=1.0, flow=s.p[idx][:, 2])
    return style


# ---------------------------------------------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------------------------------------------

def island_rigid(obj, bones, share=0.35):
    """Welds every loose island of `obj` whose weight is dominated by one of `bones` fully onto that bone. Plates that
    must vanish by bone scale (shells, seal stones) are separate islands, but after decimation a few of their vertices
    land nearer another part and pick up its weight, so at scale 0 they would only shrink, not disappear."""
    mesh = obj.data
    n = len(mesh.vertices)
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", edges)
    edges = edges.reshape(-1, 2)
    parent = np.arange(n)

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for a, b in edges:
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb
    roots = np.array([find(i) for i in range(n)])
    names = {vg.index: vg.name for vg in obj.vertex_groups}
    wanted = {obj.vertex_groups[b].index for b in bones if b in obj.vertex_groups}
    totals = {}
    for v in mesh.vertices:
        for g in v.groups:
            if g.group in wanted:
                key = (roots[v.index], g.group)
                totals[key] = totals.get(key, 0.0) + g.weight
    sizes = np.bincount(roots, minlength=n)
    best = {}
    for (root, gi), w in totals.items():
        if w > share * sizes[root] and w > best.get(root, (None, 0.0))[1]:
            best[root] = (gi, w)
    welded = 0
    for root, (gi, _) in best.items():
        ids = [int(i) for i in np.nonzero(roots == root)[0]]
        for vg in obj.vertex_groups:
            vg.remove(ids)
        obj.vertex_groups[names[gi]].add(ids, 1.0, "REPLACE")
        welded += 1
    C.log(obj.name, "island_rigid welded", welded, "islands")


class Mat:
    """One material of a boss: its parts, budget and bake settings. `islands`: bones whose loose islands are welded
    fully onto them after skinning (island_rigid)."""

    def __init__(self, builder, tris, families, suffix="", voxel=0.012, ao=0.3, extrusion=0.05, ray=0.15,
                 emission=2.5, specular=0.3, k=4.0, extra=None, rigid=(), blend=2, smooth=6, lod_share=1.0, islands=()):
        self.builder = builder
        self.tris = tris
        self.families = families
        self.suffix = suffix
        self.voxel = voxel
        self.ao = ao
        self.extrusion = extrusion
        self.ray = ray
        self.emission = emission
        self.specular = specular
        self.k = k
        self.extra = extra
        self.rigid = set(rigid)
        self.blend = blend
        self.smooth = smooth
        self.lod_share = lod_share
        self.islands = list(islands)


def build_lod_multi(lod_name, parts, bones, clips, full_poser, lod_tris, drop, aims, scale_bones=(), clip_scales=None):
    """LOD1 of a one- or two-material boss: each game mesh decimated on its own (keeps its material and UVs), skinned
    on the reduced rig (rigid parts stay rigid), joined; clips keyed from the full rig's pose with per-clip scale
    keys. `parts`: [(asset, Mat)]. Returns (arm, mesh, actions)."""
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
    for i, (asset, m) in enumerate(parts):
        mesh = C.decimate(asset.low.data.copy(), int(lod_tris * m.lod_share), f"{lod_name}_{i}", symmetric=True)
        C.shade_smooth(mesh)
        obj = C.link(bpy.data.objects.new(f"{lod_name}_{i}", mesh))
        obj.data.materials.clear()
        obj.data.materials.append(asset.low.data.materials[0])
        saved = [(p, list(p.chain), p.bone) for p in m.builder.parts]
        for p in m.builder.parts:
            p.chain = list(dict.fromkeys(remap(c) for c in p.chain))
            p.bone = p.chain[0] if p.chain else p.bone
        C.skin(obj, arm, m.builder, rigid=m.rigid, blend=min(m.blend, 1))
        if m.islands:
            island_rigid(obj, [b for b in m.islands if b not in drop])
        for p, chain, bone in saved:
            p.chain, p.bone = chain, bone
        pieces.append(obj)
    lod = pieces[0]
    for other in pieces[1:]:
        DK.join_skinned(lod, other)
    lod.name = lod_name
    lod.data.name = lod_name
    poser = K3.ClipPoser(arm, [b for b in scale_bones if b not in drop], clip_scales)
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(full_poser)
        poser.use(clip)
        actions[clip] = C.lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop, aims)
    arm.animation_data.action = actions.get("Idle") or next(iter(actions.values()))
    return arm, lod, actions


def build_titan(name, mats, bone_list, clips, lod_name=None, lod_tris=None, lod_drop=None, lod_aims=None,
                scale_bones=(), clip_scales=None, report=None, unity_scale=1.0):
    """Bakes, rigs, animates, exports and reports a boss. Returns (assets, arm, mesh, actions, poser, lod)."""
    folder = os.path.join(C.EXPORTS, name)
    assets = []
    for m in mats:
        nm = name + ("_" + m.suffix if m.suffix else "")
        a = DK.build_mesh_asset_dk(nm, m.builder, voxel=m.voxel, target_tris=m.tris, tex_size=1024, ao_distance=m.ao,
                                   extrusion=m.extrusion, ray=m.ray, emission_strength=m.emission, extra=m.extra,
                                   specular=m.specular, k=m.k, families=m.families, low_size=512, out_dir=folder,
                                   smooth_iterations=m.smooth)
        assets.append(a)
    arm = C.build_armature(name + "_Rig", bone_list)
    for a, m in zip(assets, mats):
        C.skin(a.low, arm, m.builder, rigid=m.rigid, blend=m.blend)
        if m.islands:
            island_rigid(a.low, m.islands)
    poser = K3.ClipPoser(arm, scale_bones, clip_scales)
    actions = K3.key_clips(poser, clips)
    lod = None
    if lod_name:
        lod = build_lod_multi(lod_name, list(zip(assets, mats)), bone_list, clips, poser, lod_tris, lod_drop or {},
                              lod_aims or {}, scale_bones, clip_scales)
    tris = [a.low_tris for a in assets]
    mesh = assets[0].low
    for a in assets[1:]:
        DK.join_skinned(mesh, a.low)
    mesh.name = name
    arm.animation_data.action = actions.get("Idle") or next(iter(actions.values()))
    K2.export_rig(os.path.join(folder, name + ".fbx"), arm, mesh, actions)
    if lod:
        K2.export_rig(os.path.join(folder, lod_name + ".fbx"), lod[0], lod[1], lod[2])
    data = {"asset": name, "game_tris": C.tri_count(mesh), "tris_per_material": tris,
            "source_tris": sum(a.high_tris for a in assets), "bones": len(arm.data.bones), "texture_size": 1024,
            "low_texture_size": 512,
            "materials": {f"{a.name}_Mat": f"{a.name}_*.png" for a in assets},
            "maps": ["Albedo", "Normal", "Emission", "Mask"],
            "uv": {a.name: a.uv_stats for a in assets}, "detail": {a.name: a.details for a in assets},
            "actions": {n: clips[n](poser)[0] for n in clips}, "size_m": [round(d, 3) for d in mesh.dimensions],
            "unity_scale": unity_scale, "scale_keyed_bones": list(scale_bones)}
    if lod:
        data["lod1"] = {"fbx": lod_name + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}
    data.update(report or {})
    C.write_report(name, data)
    C.save_blend(name)
    return assets, arm, mesh, actions, poser, lod


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

def cameras(target, distance):
    t = Vector(target)
    return {
        "close": C.closeup_camera(t, distance, azimuth=-38, elevation=24, lens=55),
        "side": C.closeup_camera(t, distance * 1.05, azimuth=-90, elevation=8, lens=55),
        "front": C.closeup_camera(t, distance * 0.85, azimuth=-15, elevation=12, lens=55),
        "top": C.closeup_camera(t, distance * 1.05, azimuth=-160, elevation=58, lens=55),
        "back": C.closeup_camera(t, distance, azimuth=-150, elevation=22, lens=55),
        "low": C.closeup_camera(t, distance * 0.9, azimuth=-28, elevation=6, lens=50),
    }


def previews(prefix, arm, mesh, actions, lod, out, assets, target, distance, move, shots, strips, macros,
             game_scale=1.0, rig_scale=4.0, minions=(), game_clip=None, game_target=(0.0, 0.0, 0.1), scale_cast=True,
             extra_cams=None):
    """The round-16 set. `shots`: [(file suffix, camera key, clip, frame)]; `strips`: [(clip, camera key, frames)];
    `macros`: [(suffix, aim, distance, azimuth, elevation, clip, frame)]; `minions`: [(arm, mesh, action, frame,
    (x, y, z), yaw, scale)] extra staged rigs in the game view (larvae). The scale cast is Gravemaw (crawling, to the
    right) and six Skitters at their game scales."""
    C.preview_world()
    rig = C.light_rig(scale=rig_scale)
    C.ground()
    cams = cameras(target, distance)
    cams.update(extra_cams or {})
    if lod:
        # The LOD rig sits on the same spot: keep it out of every render but its own shot.
        lod[0].hide_render = lod[1].hide_render = True
    paths = []

    def shot(name, cam, clip, frame):
        C.set_action(arm, actions[clip], frame)
        paths.append(C.render(cams[cam], os.path.join(out, f"{prefix}_{name}.png"), 1280, 720))

    for name, cam, clip, frame in shots:
        shot(name, cam, clip, frame)
    strip_paths = [C.strip_sheet(arm, actions[clip], cams[cam], out, f"{prefix}_{clip.lower()}_{cam}", frames)
                   for clip, cam, frames in strips]
    if lod:
        lod_arm, lod_mesh, lod_actions = lod
        C.set_action(lod_arm, lod_actions[move[0]], move[1])
        arm.hide_render = mesh.hide_render = True
        lod_arm.hide_render = lod_mesh.hide_render = False
        paths.append(C.render(cams["close"], os.path.join(out, f"{prefix}_lod1_close.png"), 1280, 720))
        lod_arm.hide_render = lod_mesh.hide_render = True
        arm.hide_render = mesh.hide_render = False
    for suffix, aim, dist, az, el, clip, frame in macros:
        C.set_action(arm, actions[clip], frame)
        paths.append(C.render(C.macro_camera(Vector(aim), dist, az, el), os.path.join(out, f"{prefix}_macro_{suffix}.png"),
                              1280, 720))
    if scale_cast:
        clip, frame = game_clip or move
        entries = [(arm, mesh, actions[clip], frame, (0, 0, 0), 0.0, game_scale)]
        gm = B.append_base("Gravemaw")
        sk = B.append_base("Skitter")
        entries.append((gm[0], gm[1], gm[2]["Crawl"], 10, (5.6, 0.8, 0), 0.1, 1.0))
        for x, y, yaw, fr in ((-3.2, 2.2, 0.2, 3), (-2.6, -2.4, -0.1, 10), (2.4, -2.6, 0.3, 14), (3.0, 3.0, 0.0, 18),
                              (-3.8, 0.0, 0.25, 7), (2.2, 2.4, -0.2, 21)):
            entries.append((sk[0], sk[1], sk[2]["Walk"], fr, (x, y, 0), yaw, 0.45))
        entries += list(minions)
        hide = [arm, mesh]
        paths.append(B.game_view_multi(entries, os.path.join(out, f"{prefix}_game.png"), 1.0, rig, (1.2, 0.0, 0.1),
                                       width=1280, height=720, hide=hide))
        paths.append(B.game_view_multi(entries, os.path.join(out, f"{prefix}_game_zoomed.png"), 2.0, rig, game_target,
                                       width=1280, height=720, hide=hide))
        C.remove_objects([gm[1], gm[0], sk[1], sk[0]])
    C.contact_sheet(paths, os.path.join(out, f"sheet_{prefix}.png"), 2, 640)
    if strip_paths:
        C.contact_sheet(strip_paths, os.path.join(out, f"strips_{prefix}.png"), 1, 960)
    C.uv_layout(mesh, os.path.join(out, f"{prefix}_uv.png"), background=assets[0].textures["albedo"])
    return paths + strip_paths


def blockout(prefix, mats_colors, bone_list, clips, out, target, distance, stills, strips, scale_bones=(),
             clip_scales=None, rigid=()):
    """Raw parts in flat colours on the rig (no bakes). `mats_colors`: [(builder, (r, g, b), glow or None)];
    `stills`: [(suffix, camera key, clip, frame)]; `strips`: [(clip, camera key, frames)]."""
    objs = []
    for i, (b, col, glow) in enumerate(mats_colors):
        o = b.to_object(f"{prefix}_Block{i}")
        o.data.materials.append(C.principled(f"Block{i}", col, rough=0.6, emission=glow, strength=0.6 if glow else 0.0))
        objs.append((o, b))
    arm = C.build_armature(prefix + "_BlockRig", bone_list)
    for o, b in objs:
        C.skin(o, arm, b, rigid=rigid)
    poser = K3.ClipPoser(arm, scale_bones, clip_scales)
    actions = K3.key_clips(poser, clips)
    C.preview_world()
    C.light_rig(scale=4.0)
    C.ground()
    cams = cameras(target, distance)
    still_paths = []
    for suffix, cam, clip, frame in stills:
        C.set_action(arm, actions[clip], frame)
        still_paths.append(C.render(cams[cam], os.path.join(out, f"block_{prefix}_{suffix}.png"), 960, 540, bloom=False,
                                    samples=8))
    C.set_action(arm, actions[stills[0][2]], stills[0][3])
    game = C.game_camera(Vector((0, 0, 0)), 1280 / C.GAME_PIXELS_PER_UNIT)
    still_paths.append(C.render(game, os.path.join(out, f"block_{prefix}_game.png"), 1280, 720, bloom=False, samples=8))
    C.contact_sheet(still_paths, os.path.join(out, f"block_{prefix}_sheet.png"), 2, 640)
    for clip, cam, frames in strips:
        paths = []
        for i, f in enumerate(frames):
            C.set_action(arm, actions[clip], f)
            paths.append(C.render(cams[cam], os.path.join(out, f"block_{prefix}_{clip.lower()}_{i}.png"), 640, 360,
                                  bloom=False, samples=4))
        C.contact_sheet(paths, os.path.join(out, f"block_{prefix}_strip_{clip.lower()}.png"), 4, 480)
        for p in paths:
            os.remove(p)
    for b in [m[0] for m in mats_colors]:
        C.log(prefix, "parts", len(b.parts))
    C.log(prefix, "bones", len(arm.data.bones))
    return arm, actions
