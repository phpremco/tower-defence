"""Helpers for the round-10 creatures (build_stinger.py, build_bat.py, build_bulwark.py, build_oozer.py and their
bosses). Kept apart from common.py, chitin.py and boss_kit.py so this round never touches the shared modules.

- Surface styles: armour plates with a glowing rear rim and a forward chevron, ringed tail segments with glowing
  joints, a veined acid bladder, saurian scale hide, a natural bone shield, slug hide with glands of mixed size, a
  translucent slime foot.
- build_mesh_asset_ex: common.build_mesh_asset that also keeps what a second skin needs (bake faces, build args);
  rebake_skin bakes another palette onto the same game mesh (the element bats share one mesh and rig).
- scale_rig / export_rig: build a boss in its species' units, then scale mesh, bones and location keys as a whole.
- enemy_previews: the standard sheet for a common enemy (close-ups, strips, LOD1, macros, game views).
"""

import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Vector

import chitin as H
import common as C
import detailmaps
import uvlayout


# ---------------------------------------------------------------------------------------------------------------
# Surface styles
# ---------------------------------------------------------------------------------------------------------------

def _unit(L):
    return L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)


def armour_plate(pal, k, rim_glow=1.0, chevron=0.8, lip=0.7, chevron_angle=1.2, height=0.004, cells=0, seed=0):
    """A flattened-ellipsoid armour plate seen from above: dark plate, pale worn front lip, a hot rim along the rear
    edge and a forward-pointing chevron on the midline (local -Y is forward), dark seam round the outline.
    `cells` > 0 adds a faint crackle (the plates crack under heavy shots)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        top = C.smoothstep(-0.1, 0.35, u[:, 2])
        outline = 1 - C.smoothstep(0.0, 0.32, np.abs(u[:, 2]))
        rear = C.smoothstep(0.1, 0.7, u[:, 1])
        front = C.smoothstep(0.2, 0.8, -u[:, 1])
        rim = outline * rear * C.smoothstep(-0.4, 0.2, u[:, 2])
        # Chevron: a V on the top whose arms run back from a point near the front.
        v_line = np.abs(np.abs(u[:, 0]) * chevron_angle - (u[:, 1] + 0.25))
        chev = (1 - C.smoothstep(0.05, 0.13, v_line)) * top * C.smoothstep(-0.55, -0.2, u[:, 1]) * (1 - C.smoothstep(0.35, 0.6, u[:, 1]))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.12, 50 / k, seed + 3)
        col = C.lerp3(col, pal.bone, front * outline * lip)
        col = C.lerp3(col, pal.seam, (1 - C.smoothstep(0.0, 0.12, np.abs(u[:, 2]))) * 0.85)
        crack = np.zeros(len(idx))
        if cells:
            e = C.voronoi_edge(u, C.fibonacci_sphere(cells, seed=seed))
            crack = (1 - C.smoothstep(0.0, 0.025, e)) * top * C.smoothstep(-0.2, 0.3, C.value_noise(s.p[idx], 9 / k, seed + 5))
            col = C.lerp3(col, pal.seam, crack * 0.6)
        hot = np.clip(rim * rim_glow + chev * chevron + crack * 0.25 * rim_glow, 0, 1)
        col = C.lerp3(col, pal.glow * 0.7, np.clip(rim * 0.7 + chev * 0.8, 0, 1))
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = height * k * (C.smoothstep(0.0, 0.2, np.abs(u[:, 2])) - 0.5) - 0.0015 * k * chev - 0.001 * k * crack
        s.rough[idx] = 0.66 - 0.2 * front * outline - 0.25 * hot
        H.family(s, idx, chitin=1.0 - 0.6 * hot, flow=(1 + u[:, 1]) * part.radii[1])
    return style


def ring_segment(pal, k, joint_glow=1.0, base=None, tip=None, rings=2, rough=0.6, joint_width=0.14):
    """A ringed tail or limb segment (a tube): plates lit on top, dark bands, and glow leaking at both ends where
    it meets the next segment (the acid in the joints)."""
    base = np.array(base if base is not None else pal.top)
    tip = np.array(tip if tip is not None else pal.side)

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.lerp3(base, tip, C.smoothstep(0.2, 1.0, t))
        col = col * (0.5 + 0.5 * C.smoothstep(-0.6, 0.7, n[:, 2]))[:, None] * H.mottle(s, idx, 0.1, 140 / k, 9)
        frac = (t * rings) % 1.0
        band = 1 - C.smoothstep(0.0, 0.12, np.minimum(frac, 1 - frac))
        joint = 1 - C.smoothstep(0.0, joint_width, np.minimum(t, 1 - t))
        col = C.lerp3(col, pal.seam, band * 0.6)
        col = C.lerp3(col, pal.glow * 0.6, joint * 0.8)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (joint * joint_glow)[:, None]
        s.disp[idx] = -0.002 * k * band - 0.002 * k * joint
        s.rough[idx] = rough - 0.2 * joint
        H.family(s, idx, chitin=1.0 - joint * 0.6, membrane=joint * 0.6, flow=t * C.axis_length(part))
    return style


def bladder(pal, k, cells=26, seed=5, glow=1.0, plate=0.35):
    """A swollen glowing sac (acid bladder, bolt gland, brood sac): a translucent membrane lit from inside, a net of
    dark veins of mixed cell size, and a dark plated cap on top (`plate`: how far the cap reaches down, 0 none)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        seeds = C.fibonacci_sphere(cells, seed=seed)
        rng = np.random.default_rng(seed)
        seeds = seeds * rng.uniform(0.75, 1.25, (len(seeds), 1))
        e = C.voronoi_edge(u, seeds / np.linalg.norm(seeds, axis=1, keepdims=True))
        vein = 1 - C.smoothstep(0.0, 0.035, e)
        size = 0.5 + 0.5 * C.value_noise(u, 2.5, seed + 1)
        blob = C.smoothstep(0.03, 0.12 + 0.1 * size, e)
        cap = C.smoothstep(1 - plate - 0.08, 1 - plate + 0.08, u[:, 2]) if plate else np.zeros(len(idx))
        flesh = C.lerp3(pal.membrane, pal.glow * 0.75, blob * 0.8)
        flesh = C.lerp3(flesh, pal.seam, vein * 0.85)
        capc = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.12, 50 / k, seed + 2)
        s.albedo[idx] = C.lerp3(flesh, capc, cap)
        s.emit[idx] = pal.glow[None, :] * ((blob * (0.55 + 0.45 * size) * (1 - vein) * glow) * (1 - cap))[:, None]
        s.disp[idx] = (0.004 * k * blob - 0.003 * k * vein) * (1 - cap)
        s.rough[idx] = 0.32 + 0.35 * cap + 0.2 * vein
        H.family(s, idx, chitin=cap, membrane=1 - cap, flow=u[:, 2] * part.radii[2])
    return style


def ribbed_bladder(pal, k, ribs=7, seed=5, glow=1.0, plate=0.35, axis=1):
    """A swollen gland like a gourd: dark chitin ribs running along the local `axis` (1 = length) with the glowing
    fluid showing between them, brightest towards the far end, broken by a few dark veins; a dark cap on top."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        others = [i for i in range(3) if i != axis]
        ang = np.arctan2(u[:, others[0]], u[:, others[1]])
        rib = np.abs(np.sin(ang * ribs * 0.5 + 0.3 * C.value_noise(s.p[idx], 8 / k, seed)))
        ridge = C.smoothstep(0.55, 0.95, rib)
        gap = 1 - C.smoothstep(0.1, 0.5, rib)
        vein = (1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(s.p[idx], 22 / k, seed + 1)))) * gap
        far = 0.55 + 0.45 * C.smoothstep(-0.8, 0.8, u[:, axis])
        cap = C.smoothstep(1 - plate - 0.08, 1 - plate + 0.08, u[:, 2]) if plate else np.zeros(len(idx))
        shell = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.12, 50 / k, seed + 2)
        col = C.lerp3(shell, pal.glow * 0.75, gap * (1 - cap) * 0.9)
        col = C.lerp3(col, pal.seam, vein * 0.8)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (gap * far * glow * (1 - vein) * (1 - cap))[:, None]
        s.disp[idx] = 0.004 * k * ridge - 0.002 * k * gap
        s.rough[idx] = 0.55 - 0.25 * gap
        H.family(s, idx, chitin=np.maximum(ridge, cap), membrane=gap * (1 - cap), flow=u[:, axis] * part.radii[axis])
    return style


def scale_hide(pal, k, cells=90, seed=0, width=0.04, joint_glow=0.0, belly_light=0.4):
    """Saurian scaled hide on any part: scales of mixed size with dark seams, lit on top, a lighter underside band,
    and optional goo glow in the deepest seams (`joint_glow`). Scales are seeded from the part's own points, so a
    tube and an ellipsoid both get scales of one world size."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        rng = np.random.default_rng(seed + part.pid)
        count = max(8, min(len(p), int(cells)))
        seeds = p[rng.choice(len(p), size=count, replace=False)]
        e = C.voronoi_edge(p, seeds)
        span = max(float(np.ptp(p, axis=0).max()), 1e-3)
        seam = 1 - C.smoothstep(0.0, width * span / count ** 0.5, e)
        dome = C.smoothstep(0.0, 2.5 * width * span / count ** 0.5, e)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        under = C.smoothstep(0.0, -0.6, n[:, 2]) * belly_light
        col = C.lerp3(col, pal.bone * 0.55, under)
        col = col * (0.85 + 0.15 * dome)[:, None] * H.mottle(s, idx, 0.14, 40 / k, seed + 1)
        col = C.lerp3(col, pal.seam, seam * 0.75)
        s.albedo[idx] = col
        if joint_glow:
            deep = seam * C.smoothstep(0.1, 0.5, C.value_noise(p, 12 / k, seed + 2))
            s.emit[idx] = pal.glow[None, :] * (deep * joint_glow)[:, None]
        s.disp[idx] = 0.0025 * k * dome - 0.0015 * k * seam
        s.rough[idx] = 0.7 - 0.1 * dome
        H.family(s, idx, chitin=1.0, flow=p[:, 2])
    return style


def bone_shield(pal, k, rings=7, boss_glow=1.0, rim_glow=0.6, seed=0, ribs=7):
    """A natural shield grown from bone (a flattened ellipsoid, the face along local -X or +X): warped growth ridges
    that come and go, radial ribs like a shell, a small glowing boss off-centre in a dark socket, a pale worn rim with a
    thin glowing seam behind it. Organic, never a target of even rings."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        cy, cz = u[:, 1] + 0.12, u[:, 2] - 0.1
        r = np.hypot(cy, cz)
        ang = np.arctan2(cz, cy)
        warp = 0.5 * C.value_noise(s.p[idx], 5 / k, seed) + 0.15 * np.sin(ang * 3 + 1.0)
        frac = (r * rings + warp) % 1.0
        broken = C.smoothstep(-0.3, 0.2, C.value_noise(s.p[idx], 7 / k, seed + 3))
        ridge = C.smoothstep(0.0, 0.35, frac) * (1 - C.smoothstep(0.65, 1.0, frac)) * broken
        groove = (1 - C.smoothstep(0.0, 0.12, np.minimum(frac, 1 - frac))) * broken
        rib = 1 - C.smoothstep(0.0, 0.18, np.abs(np.sin(ang * ribs * 0.5 + 0.3 * warp)))
        face = C.smoothstep(0.1, 0.5, np.abs(u[:, 0]))
        boss = 1 - C.smoothstep(0.05, 0.12, r)
        socket = (1 - C.smoothstep(0.12, 0.22, r)) * (1 - boss)
        rim = C.smoothstep(0.85, 0.98, np.hypot(u[:, 1], u[:, 2]))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.18, 30 / k, seed + 1)
        col = C.lerp3(col, pal.bone * 0.8, (0.1 + 0.25 * ridge) * face)
        col = C.lerp3(col, pal.seam, np.maximum(groove * 0.45, rib * 0.55 * C.smoothstep(0.2, 0.4, r)) * face)
        col = C.lerp3(col, pal.seam, socket * 0.9 * face)
        col = C.lerp3(col, pal.bone, rim * 0.8)
        seam = (1 - C.smoothstep(0.0, 0.03, np.abs(np.hypot(u[:, 1], u[:, 2]) - 0.8))) * face
        hot = np.clip(boss * boss_glow * face + seam * rim_glow, 0, 1)
        col = C.lerp3(col, pal.glow * 0.7, hot)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = (0.004 * k * ridge - 0.003 * k * groove - 0.003 * k * rib - 0.004 * k * socket) * face + 0.004 * k * boss
        s.rough[idx] = 0.62 - 0.15 * rim - 0.2 * hot
        H.family(s, idx, chitin=1.0 - hot * 0.7, flow=r * part.radii[1])
    return style


def slug_hide(pal, k, glands=34, seed=0, gland_size=0.16, gland_glow=1.0, grooves=9.0, gland_zone=0.15):
    """Wet leathery slug skin on an ellipsoid: tubercles of mixed size, lengthwise grooves, dark mottling, and on
    the back (local +Z above `gland_zone`) glowing toxin glands of mixed size with dark rims."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        p = s.p[idx]
        tub = 0.5 + 0.5 * C.value_noise(p, 45 / k, seed)
        groove = 1 - C.smoothstep(0.0, 0.25, np.abs(np.sin(np.arctan2(u[:, 0], u[:, 2]) * grooves)))
        seeds = C.fibonacci_sphere(glands, seed=seed + 1)
        rng = np.random.default_rng(seed + 2)
        radii = gland_size * rng.uniform(0.45, 1.25, len(seeds))
        d = np.full(len(idx), 9.0)
        rad = np.ones(len(idx))
        keep = rng.uniform(0, 1, len(seeds)) < 0.55
        for sd, r, kp in zip(seeds, radii, keep):
            if sd[2] < gland_zone or not kp:
                continue
            dist = np.linalg.norm(u - sd, axis=1) / r
            closer = dist < d
            d = np.where(closer, dist, d)
            rad = np.where(closer, r, rad)
        gland = 1 - C.smoothstep(0.55, 0.95, d)
        rimg = (1 - C.smoothstep(0.0, 0.15, np.abs(d - 0.95))) * (d < 1.4)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        blot = C.smoothstep(0.05, 0.4, C.value_noise(p, 18 / k, seed + 3))
        col = C.lerp3(col, pal.dark, blot * 0.55)
        col = col * (0.82 + 0.18 * tub)[:, None]
        col = C.lerp3(col, pal.seam, groove * 0.4)
        col = C.lerp3(col, pal.top * 1.3, rimg * 0.5)
        col = C.lerp3(col, pal.glow * 0.6, gland)
        s.albedo[idx] = col
        core = 1 - C.smoothstep(0.0, 0.55, d)
        s.emit[idx] = pal.glow[None, :] * (gland * gland_glow * (0.25 + 0.75 * core) * (0.7 + 0.3 * tub))[:, None]
        # A raised pustule rim round each gland, the gland itself slightly sunk (a vent, not a painted spot).
        s.disp[idx] = 0.003 * k * tub - 0.002 * k * groove - 0.002 * k * gland + 0.005 * k * rimg
        # Wet: low roughness overall, glossier on the glands.
        s.rough[idx] = 0.38 - 0.12 * gland + 0.1 * groove
        H.family(s, idx, membrane=1.0, flow=(1 + u[:, 1]) * part.radii[1])
    return style


def slime_foot(pal, k, waves=9.0, edge_glow=1.0):
    """A slug's foot skirt (a flat ellipsoid): dark wet top blending into the hide, a translucent glowing rim, and
    pedal-wave stripes across the sole (lit from inside)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        n = s.n[idx]
        r = np.hypot(u[:, 0], u[:, 1])
        edge = C.smoothstep(0.7, 0.98, r) * (1 - C.smoothstep(0.2, 0.7, u[:, 2]))
        sole = C.smoothstep(0.0, -0.5, u[:, 2])
        wave = 0.5 + 0.5 * np.sin(u[:, 1] * waves * math.pi)
        col = C.lerp3(pal.side, pal.membrane, edge)
        col = C.lerp3(col, pal.belly, sole * 0.5)
        col = C.lerp3(col, pal.glow * 0.7, edge * 0.6 + sole * wave * 0.25)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip(edge * edge_glow * (0.65 + 0.35 * wave) + sole * wave * 0.4, 0, 1)[:, None]
        s.disp[idx] = 0.002 * k * wave * sole + 0.0015 * k * C.value_noise(s.p[idx], 50 / k, 7)
        s.rough[idx] = 0.22 + 0.15 * (1 - edge)
        H.family(s, idx, membrane=1.0, flow=u[:, 1] * part.radii[1])
    return style


def crysium_blade(k, body=(0.92, 0.42, 0.72), edge=(1.0, 0.9, 0.96), glow=(1.0, 0.45, 0.8), root=(0.3, 0.07, 0.3)):
    """A Crysium sword grown from a forearm (a flattened tube): dark violet root, pink body with a hot white edge, a
    bright core line down the middle and darker fracture lines."""
    body, edge, glow, root = (np.array(c) for c in (body, edge, glow, root))

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        rel = dist / np.maximum(radius, 1e-5)
        rim = C.smoothstep(0.55, 0.95, rel)
        core = 1 - C.smoothstep(0.0, 0.25, rel)
        col = C.lerp3(root, body, C.smoothstep(0.0, 0.2, t))
        col = C.lerp3(col, edge, rim * C.smoothstep(0.1, 0.4, t))
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(s.p[idx], 18 / k, 9)))
        col = C.lerp3(col, root * 0.5, crack * 0.6)
        s.albedo[idx] = col * 0.8
        amount = 0.25 + 0.5 * rim + 0.4 * core * C.smoothstep(0.1, 0.3, t) + 0.3 * C.smoothstep(0.85, 1.0, t)
        s.emit[idx] = np.clip(glow[None, :] * (amount * (1 - 0.6 * crack) * C.smoothstep(0.02, 0.15, t))[:, None], 0, 1)
        s.rough[idx] = 0.3 + 0.25 * crack
        s.disp[idx] = 0.0
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        s.flow[idx] = t * C.axis_length(part)
    return style


# ---------------------------------------------------------------------------------------------------------------
# Mesh asset with a second skin
# ---------------------------------------------------------------------------------------------------------------

def build_mesh_asset_ex(name, builder, voxel, target_tris, tex_size, ao_distance, extrusion, ray, emission_strength=2.5,
                        extra=None, specular=0.3, k=1.0, families=C.FAMILY_ORDER, low_size=None, out_dir=None,
                        smooth_iterations=6, edge_gain=0.35):
    """common.build_mesh_asset for mirrored creatures (mirror=True, symmetric), keeping the bake faces and the
    settings on the asset so rebake_skin can bake another palette onto the same game mesh. `out_dir`: where the
    maps go (default Exports/<name>)."""
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
    asset.bake_faces = bake_faces
    asset.settings = dict(voxel=voxel, tex_size=tex_size, ao_distance=ao_distance, extrusion=extrusion, ray=ray,
                          k=k, families=families, low_size=low_size, smooth_iterations=smooth_iterations,
                          edge_gain=edge_gain, emission_strength=emission_strength, specular=specular, preview=preview)
    C.log(name, "high tris", asset.high_tris, "low tris", asset.low_tris, "details", details)
    return asset


def rebake_skin(asset, builder, out_dir, prefix, extra=None):
    """Bakes another skin (the same parts with another palette) onto the asset's game mesh into `out_dir`, with
    the same file names as the default maps (`prefix`_Albedo, ...). Returns the texture paths. The game mesh, its UVs
    and the rig stay as they are."""
    st = asset.settings
    parts_obj = builder.to_object(prefix + "_SkinParts")
    high_mesh = C.fuse(parts_obj, st["voxel"], st["smooth_iterations"])
    bpy.data.objects.remove(parts_obj)
    C.detail(high_mesh, builder, extra)
    C.shade_smooth(high_mesh)
    high = C.link(bpy.data.objects.new(prefix + "_SkinHigh", high_mesh))
    high.data.materials.append(C.high_bake_material(edge_gain=st["edge_gain"], k=st["k"], families=st["families"]))
    C.bake_vertex_ao(high, st["ao_distance"])
    textures = C.bake_maps(high, asset.low, out_dir, prefix, st["tex_size"], st["extrusion"], st["ray"],
                           bake_faces=asset.bake_faces, low_size=st["low_size"])
    bpy.data.objects.remove(high)
    return textures


def skin_material(asset, name, textures):
    """A preview material for a second skin (same detail normals as the default)."""
    st = asset.settings
    return C.game_material(name, textures, st["emission_strength"], st["specular"], st["preview"])


# ---------------------------------------------------------------------------------------------------------------
# Whole-rig scaling (bosses built in species units)
# ---------------------------------------------------------------------------------------------------------------

def action_fcurves(act):
    """All F-curves of an action (legacy and Blender 5 layered actions)."""
    curves = []
    if hasattr(act, "layers") and len(act.layers):
        for layer in act.layers:
            for strip in layer.strips:
                for bag in getattr(strip, "channelbags", ()):
                    curves.extend(bag.fcurves)
    elif hasattr(act, "fcurves"):
        curves.extend(act.fcurves)
    return curves


def scale_rig(arm, meshes, actions, m):
    """Scales a skinned creature by `m` as a whole: mesh vertices, bone heads and tails, and every location key
    (rotation and scale keys are unitless). The pose of every frame is the same shape, just bigger."""
    for mesh in meshes:
        mesh.data.transform(Matrix.Scale(m, 4))
        mesh.data.update()
    C.activate(arm)
    bpy.ops.object.mode_set(mode="EDIT")
    for eb in arm.data.edit_bones:
        eb.head = eb.head * m
        eb.tail = eb.tail * m
    bpy.ops.object.mode_set(mode="OBJECT")
    for act in actions:
        for fc in action_fcurves(act):
            if fc.data_path.endswith(".location"):
                for kp in fc.keyframe_points:
                    kp.co.y *= m
                    kp.handle_left.y *= m
                    kp.handle_right.y *= m
                fc.update()


def _channelbags(act):
    bags = []
    for layer in getattr(act, "layers", ()):
        for strip in layer.strips:
            bags.extend(getattr(strip, "channelbags", ()))
    return bags


GUARD_PATH = 'pose.bones["__export_guard__"].location'


def export_rig(path, arm, mesh, names):
    """Exports `arm` + `mesh` with exactly the actions in `names` ({clip: action}) as takes named by clip. The FBX
    exporter takes every action whose F-curves all resolve on the armature (a LOD's actions do on the full rig), so
    every other action gets a guard F-curve that cannot resolve for the time of the export."""
    keep = set(names.values())
    guards = []
    for act in bpy.data.actions:
        if act in keep:
            continue
        for bag in _channelbags(act):
            if bag.fcurves:
                guards.append((bag, bag.fcurves.new(GUARD_PATH, index=0)))
    taken = {}
    for clip in names:
        other = bpy.data.actions.get(clip)
        if other is not None and other is not names[clip]:
            taken[clip] = other
            other.name = clip + "_Away"
    old = {act: act.name for act in names.values()}
    for clip, act in names.items():
        act.name = clip
    C.set_action(arm, next(iter(names.values())), 0)
    C.export_fbx(path, [arm, mesh], animated=True)
    for act, n in old.items():
        act.name = n
    for clip, other in taken.items():
        other.name = clip
    for bag, fc in guards:
        bag.fcurves.remove(fc)


# ---------------------------------------------------------------------------------------------------------------
# Animation helpers
# ---------------------------------------------------------------------------------------------------------------

def ease(e0, e1, t):
    return float(C.smoothstep(e0, e1, np.array([t]))[0])


def pulse(t, start, peak, end):
    """0 before `start`, rises to 1 at `peak`, falls back to 0 at `end` (smooth)."""
    return ease(start, peak, t) * (1 - ease(peak, end, t))


def recoil(t, attack=0.35, decay=7.0, freq=14.0):
    """The shared hit curve: a quick shove that rings out."""
    if t < attack:
        return math.sin(math.pi * t / attack)
    return math.exp(-(t - attack) * decay) * math.cos((t - attack) * freq) * 0.4


# ---------------------------------------------------------------------------------------------------------------
# Previews for a common enemy
# ---------------------------------------------------------------------------------------------------------------

def enemy_previews(prefix, arm, mesh, actions, lod, out, asset, target, distance, game_scale, move, layout, strips,
                   macros, rig_scale=1.0, lift=0.0, game_lift=0.0, game_target=(0, 0, 0.1), idle="Idle", power=None,
                   death_frame=None):
    """Close-up 3/4, side, front, top and death; clip strips; LOD1; macros with and without detail normals; game
    views (a group at play zoom and zoomed, one alone zoomed). `move`: (clip, frame) for the walking shots;
    `layout`: [(clip, frame, (x, y), yaw)] for the game group; `strips`: [(clip, camera key, frames)]; `macros`:
    [(suffix, aim, distance, azimuth, elevation)]; `lift`: close-up height for flyers; `game_lift`: flight height."""
    C.preview_world()
    rig = C.light_rig(scale=rig_scale)
    C.ground()
    arm.location.z = lift
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    lod_arm.location.z = lift
    t = Vector(target) + Vector((0, 0, lift))
    cams = {
        "close": C.closeup_camera(t, distance, azimuth=-38, elevation=24, lens=55),
        "side": C.closeup_camera(t, distance * 1.05, azimuth=-90, elevation=8, lens=55),
        "front": C.closeup_camera(t, distance * 0.85, azimuth=-15, elevation=12, lens=55),
        "top": C.closeup_camera(t, distance * 1.05, azimuth=-160, elevation=58, lens=55),
        "back": C.closeup_camera(t, distance, azimuth=-150, elevation=20, lens=55),
    }
    paths = []

    def shot(name, cam, clip, frame):
        C.set_action(arm, actions[clip], frame)
        paths.append(C.render(cams[cam], os.path.join(out, f"{prefix}_{name}.png"), 1280, 720))

    shot("close", "close", idle, 15)
    shot("side", "side", move[0], move[1])
    shot("front", "front", idle, 15)
    shot("top", "top", move[0], move[1] + 3)
    shot("back", "back", idle, 30)
    if power:
        shot("power", "close", power[0], power[1])
    shot("dead", "close", "Death", death_frame if death_frame is not None else int(actions["Death"].frame_range[1]))
    strip_paths = [C.strip_sheet(arm, actions[clip], cams[cam], out, f"{prefix}_{clip.lower()}_{cam}", frames)
                   for clip, cam, frames in strips]

    C.set_action(lod_arm, lod_actions[move[0]], move[1])
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(cams["close"], os.path.join(out, f"{prefix}_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions[idle], 15)
    for suffix, aim, dist, az, el in macros:
        aim = Vector(aim) + Vector((0, 0, lift))
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, f"{prefix}_macro_{suffix}.png"), 1280, 720))
    if macros:
        C.set_detail_preview(asset, False)
        suffix, aim, dist, az, el = macros[0]
        aim = Vector(aim) + Vector((0, 0, lift))
        paths.append(C.render(C.macro_camera(aim, dist, az, el),
                              os.path.join(out, f"{prefix}_macro_{suffix}_bakeonly.png"), 1280, 720))
        C.set_detail_preview(asset, True)

    arm.location.z = 0
    group = [(actions[clip], frame, (x, y, game_lift), yaw) for clip, frame, (x, y), yaw in layout]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, f"{prefix}_game.png"), game_scale, group,
                                   close_lights=rig, target=game_target))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, f"{prefix}_game_zoomed.png"), game_scale, group,
                                   zoom=2.5, close_lights=rig, target=game_target))
    single = [(actions[move[0]], move[1], (0, 0, game_lift), 0.0)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, f"{prefix}_single_zoomed.png"), game_scale, single,
                                   zoom=4.0, close_lights=rig, target=(0, 0, game_lift + 0.1)))
    C.contact_sheet(paths, os.path.join(out, f"sheet_{prefix}.png"), 2, 640)
    if strip_paths:
        C.contact_sheet(strip_paths, os.path.join(out, f"strips_{prefix}.png"), 1, 960)
    C.uv_layout(mesh, os.path.join(out, f"{prefix}_uv.png"), background=asset.textures["albedo"])
    return paths + strip_paths
