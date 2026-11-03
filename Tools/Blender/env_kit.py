"""Helpers for the level environment kit (round 8, Crystal Plains): seamless tiling material sets, kit assets built
through the shared pipeline but exported to ArtSource/Exports/Env, vertex-colour sway masks for flora, preview
materials and the dark, misty level lighting for the diorama renders.

Kept apart from the shared modules (common.py and the style modules are owned by other work): this module only
imports them. Conventions it adds:

- Tiling sets live in Exports/Env/Tiling/<Set>/ as <Set>_Albedo (sRGB), <Set>_Normal (tangent, +Y up),
  <Set>_Mask (linear: R AO, G smoothness, B height 0..1 for height blending, A Crysium-dust mask) and, where
  something glows, <Set>_Emission (sRGB). A 512 copy sits in Low/. Each tile covers TILE_WORLD[set] world units.
- Kit meshes live in Exports/Env/<Asset>/ with the same four maps as the props (Albedo, Normal, Emission, Mask with
  R AO, G smoothness, B / A detail masks) and a report. Pivots sit on the ground plane (z = 0).
- Placement frame (game terms): a piece that borders something (a road edge, a bank, a cliff) has that thing on
  its local forward side (Blender -Y, Unity +Z), so Unity orients it with LookRotation(towards the thing).
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Vector

import common as C
import detailmaps as D

ENV = os.path.join(C.EXPORTS, "Env")
TILING = os.path.join(ENV, "Tiling")
SCRATCH_DEFAULT = os.path.join(C.REPO, "ArtSource", "Previews", "Env")

# World size (units, one grid cell = 1) one texture tile covers. Unity: UV = worldXZ / TILE_WORLD.
TILE_WORLD = {"Road": 2.0, "Plaza": 3.0, "Ground": 3.0, "GroundFlora": 2.0, "Cliff": 3.0, "RiftRock": 2.0}


# ---------------------------------------------------------------------------------------------------------------
# Periodic pattern helpers (everything wraps on the tile)
# ---------------------------------------------------------------------------------------------------------------

def coords(n):
    """Tile coordinates u (columns) and v (rows, upwards) in [0, 1)."""
    c = (np.arange(n) + 0.5) / n
    return np.meshgrid(c, c)


def bn(n, lo, hi, seed):
    return D.band_noise(n, lo, hi, seed)


def blur(channel, radius):
    """Wrapped Gaussian blur (FFT), radius in texels."""
    return C._blur(channel, radius)


def ss(e0, e1, x):
    return D.smoothstep(e0, e1, x)


def voronoi(n, cells, seed, jitter=0.9, warp=0.0, stretch=(1, 1)):
    """Wrapped jittered-grid voronoi: (F1, F2 - F1, value per cell, cell index). `stretch` (sx, sy) uses that many
    cells across u and v (anisotropic cells for strata and columns)."""
    sx, sy = (cells * stretch[0], cells * stretch[1]) if stretch != (1, 1) else (cells, cells)
    rng = np.random.default_rng(seed)
    offset = rng.uniform(0.5 - jitter / 2, 0.5 + jitter / 2, (sy, sx, 2))
    value = rng.uniform(0, 1, (sy, sx))
    u, v = coords(n)
    x, y = u * sx, v * sy
    if warp:
        x = x + warp * bn(n, 2, 6, seed + 101)
        y = y + warp * bn(n, 2, 6, seed + 202)
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    d1 = np.full((n, n), np.inf)
    d2 = np.full((n, n), np.inf)
    v1 = np.zeros((n, n))
    id1 = np.zeros((n, n), dtype=np.int64)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            wx, wy = cx % sx, cy % sy
            px = cx + offset[wy, wx, 0]
            py = cy + offset[wy, wx, 1]
            d = np.hypot(px - x, py - y)
            closer = d < d1
            d2 = np.where(closer, d1, np.minimum(d2, d))
            v1 = np.where(closer, value[wy, wx], v1)
            id1 = np.where(closer, wy * sx + wx, id1)
            d1 = np.where(closer, d, d1)
    return d1, d2 - d1, v1, id1


def grains(n, cells, seed, keep, radius=0.3, sharp=6):
    """Small angular grains (gravel, crystal grit): a random subset of voronoi cells holds a faceted bump.
    Returns (height 0..1, value per grain)."""
    d1, _, v, _ = voronoi(n, cells, seed)
    rng_angle = (v * 97.0) % (2 * math.pi)
    u, vv = coords(n)
    # A polygonal (faceted) grain: distance scaled by a lobed function of the angle around the grain centre.
    lobes = 1 + 0.25 * np.cos(sharp * (np.arctan2(np.sin(vv * 7.0 + rng_angle), np.cos(u * 7.0 + rng_angle))))
    r = radius * (0.6 + 0.8 * ((v * 13.7) % 1.0))
    h = np.clip(1 - d1 * lobes / r, 0, 1) ** 0.7 * (v < keep)
    return h, v


def scratches(n, count, seed, length=(0.03, 0.09), width=(0.0012, 0.0025), claws=3, spread=0.006):
    """Claw scuffs: groups of `claws` parallel short strokes in random directions, wrapped. 0..1."""
    rng = np.random.default_rng(seed)
    u, v = coords(n)
    out = np.zeros((n, n))
    for _ in range(count):
        p0 = rng.uniform(0, 1, 2)
        a = rng.uniform(0, math.pi)
        d = np.array((math.cos(a), math.sin(a)))
        side = np.array((-d[1], d[0]))
        half = rng.uniform(*length) / 2
        w = rng.uniform(*width)
        strength = rng.uniform(0.4, 1.0)
        # Only evaluate a window around the stroke group (it is small), wrapped.
        dx = (u - p0[0] + 0.5) % 1.0 - 0.5
        dy = (v - p0[1] + 0.5) % 1.0 - 0.5
        near = (np.abs(dx) < half + 0.03) & (np.abs(dy) < half + 0.03)
        if not near.any():
            continue
        for k in range(claws):
            o = (k - (claws - 1) / 2) * spread
            qx = dx[near] - side[0] * o
            qy = dy[near] - side[1] * o
            t = np.clip(qx * d[0] + qy * d[1], -half, half)
            dist = np.hypot(qx - t * d[0], qy - t * d[1])
            taper = 1 - (np.abs(t) / half) ** 2
            out[near] = np.maximum(out[near], strength * taper * (1 - ss(0.0, w, dist)))
    return out


def normal_from_height(h, tile_world, strength=1.0):
    """Tangent normal from a height field in world units (wrapped differences): slopes are true to scale."""
    n = h.shape[0]
    texel = tile_world / n
    du = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) / (2 * texel)
    dv = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) / (2 * texel)
    nrm = np.dstack([-du * strength, -dv * strength, np.ones_like(h)])
    return nrm / np.linalg.norm(nrm, axis=2, keepdims=True)


def cavity_ao(h, radius_texels, depth, amount=0.65):
    """Occlusion from the height field: points lower than their surroundings get darker."""
    c = np.clip((blur(h, radius_texels) - h) / depth, 0, 1)
    return 1 - amount * c


def mix(a, b, t):
    """Lerp; a 2D weight is broadcast over colour channels when a or b is an image or a colour."""
    t = np.asarray(t, dtype=float)
    a = np.asarray(a, dtype=float)
    b = np.asarray(b, dtype=float)
    if t.ndim == 2 and (a.ndim != 2 or b.ndim != 2) and not (a.ndim == 0 and b.ndim == 0):
        t = t[..., None]
    return a * (1 - t) + b * t


def rgb(col, n):
    return np.broadcast_to(np.asarray(col, dtype=float), (n, n, 3)).copy()


def write_set(name, albedo, height, smooth, dust, ao, emission=None, normal_strength=1.0):
    """Writes a tiling set (1024 and a 512 Low copy). albedo/emission in sRGB 0..1, height in world units."""
    out = os.path.join(TILING, name)
    low = os.path.join(out, "Low")
    os.makedirs(low, exist_ok=True)
    n = albedo.shape[0]
    tile = TILE_WORLD[name]
    nrm = normal_from_height(height, tile, normal_strength)
    hn = (height - height.min()) / max(np.ptp(height), 1e-9)
    maps = {
        "Albedo": (np.dstack([np.clip(albedo, 0, 1), np.ones((n, n))]), "sRGB", False),
        "Normal": (np.dstack([nrm * 0.5 + 0.5, np.ones((n, n))]), "Non-Color", True),
        "Mask": (np.dstack([ao, smooth, hn, dust]).clip(0, 1), "Non-Color", False),
    }
    if emission is not None:
        maps["Emission"] = (np.dstack([np.clip(emission, 0, 1), np.ones((n, n))]), "sRGB", False)
    paths = {}
    for key, (arr, space, is_normal) in maps.items():
        path = os.path.join(out, f"{name}_{key}.png")
        C.array_to_image(arr.astype(np.float32), path, colorspace=space)
        small = C.downsample(arr.astype(np.float32), 2, normal=is_normal)
        C.array_to_image(small, os.path.join(low, f"{name}_{key}.png"), colorspace=space)
        paths[key.lower()] = path
    stats = {"set": name, "size": n, "low_size": n // 2, "tile_world": tile, "maps": sorted(maps),
             "albedo_mean_srgb": [round(float(x), 3) for x in albedo.reshape(-1, 3).mean(axis=0)],
             "smoothness_mean": round(float(smooth.mean()), 3), "height_range_m": round(float(np.ptp(height)), 4)}
    with open(os.path.join(out, f"{name}_report.json"), "w", encoding="utf-8") as f:
        import json
        json.dump(stats, f, indent=2)
    C.log("tiling set", name, stats)
    return paths


def set_paths(name):
    out = os.path.join(TILING, name)
    paths = {k.lower(): os.path.join(out, f"{name}_{k}.png") for k in ("Albedo", "Normal", "Mask", "Emission")}
    if not os.path.exists(paths["emission"]):
        paths.pop("emission")
    return paths


# ---------------------------------------------------------------------------------------------------------------
# Preview materials
# ---------------------------------------------------------------------------------------------------------------

def tiling_material(name, set_name, tile_scale=1.0, emission_strength=2.0, projection="UV", normal_strength=1.0):
    """Preview stand-in for a terrain shader using one tiling set. `projection`: 'UV' (the mesh's UVs times
    `tile_scale`) or 'WORLD' (top-down world XY / TILE_WORLD, what Unity should do for the ground)."""
    paths = set_paths(set_name)
    mat = C.new_material(name)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    if projection == "WORLD":
        tc = nt.nodes.new("ShaderNodeTexCoord")
        mp = nt.nodes.new("ShaderNodeMapping")
        k = 1.0 / TILE_WORLD[set_name]
        mp.inputs["Scale"].default_value = (k, k, k)
        nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
        vec = mp.outputs["Vector"]
    else:
        tc = nt.nodes.new("ShaderNodeTexCoord")
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (tile_scale, tile_scale, 1)
        nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
        vec = mp.outputs["Vector"]

    def tex(path, non_color=False):
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(path, check_existing=True)
        if non_color:
            t.image.colorspace_settings.name = "Non-Color"
            t.image.alpha_mode = "CHANNEL_PACKED"
        nt.links.new(vec, t.inputs["Vector"])
        return t

    alb = tex(paths["albedo"])
    mask = tex(paths["mask"], True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], sep.inputs[0])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(alb.outputs["Color"], mul.inputs[6])
    nt.links.new(sep.outputs[0], mul.inputs[7])
    nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(sep.outputs[1], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nm.inputs["Strength"].default_value = normal_strength
    nt.links.new(tex(paths["normal"], True).outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    if "emission" in paths:
        em = tex(paths["emission"])
        nt.links.new(em.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def emissive(name, color, strength):
    mat = C.new_material(name)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0, 0, 0, 1)
    bsdf.inputs["Emission Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def plane(name, sx, sy, mat, location=(0, 0, 0), uv_scale=1.0, subdiv=0):
    """A flat plane sx by sy (centred) with UVs 0..uv_scale."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    sx_div, sy_div = subdiv if isinstance(subdiv, tuple) else (subdiv, subdiv)
    bmesh.ops.create_grid(bm, x_segments=1 + sx_div, y_segments=1 + sy_div, size=0.5)
    for f in bm.faces:
        for loop in f.loops:
            co = loop.vert.co
            loop[uv].uv = ((co.x + 0.5) * uv_scale, (co.y + 0.5) * uv_scale)
    for v in bm.verts:
        v.co.x *= sx
        v.co.y *= sy
    bm.to_mesh(mesh)
    bm.free()
    obj = C.link(bpy.data.objects.new(name, mesh))
    obj.location = location
    if mat:
        mesh.materials.append(mat)
    return obj


# ---------------------------------------------------------------------------------------------------------------
# Kit assets
# ---------------------------------------------------------------------------------------------------------------

def use_env_exports():
    """Points the shared pipeline's export folder at Exports/Env (only for this process)."""
    C.EXPORTS = ENV
    os.makedirs(ENV, exist_ok=True)


def build(name, builder, tris, voxel=0.011, smoothing=3, sharp=None, families=("rock", "crystal"),
          emission=2.2, ao=0.3, tex=512, extrusion=0.03, ray=0.1, symmetric=False, border_smooth=0, family_set=None,
          k=1.0):
    asset = C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=tex, ao_distance=ao,
                               extrusion=extrusion, ray=ray, emission_strength=emission, smooth_iterations=smoothing,
                               symmetric=symmetric, low_from_detail=True, sharp_angle=sharp, families=families,
                               border_smooth=border_smooth, family_set=family_set, k=k)
    return asset


def finish(asset, extra=None):
    """Exports the asset's FBX and its report (Exports/Env/<Asset>/)."""
    name = asset.name
    C.export_fbx(os.path.join(ENV, name, name + ".fbx"), [asset.low], animated=False)
    dims = asset.low.dimensions
    report = {"asset": name, "game_tris": asset.low_tris, "source_tris": asset.high_tris, "texture_size": 512,
              "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
              "size_m": [round(dims.x, 3), round(dims.y, 3), round(dims.z, 3)]}
    if extra:
        report.update(extra)
    C.write_report(name, report)
    return report


def ground_snap(obj, below=-0.02):
    """Clamps vertices below the ground plane: kit pieces sit on z = 0 with a thin skirt under it."""
    mesh = obj.data
    for v in mesh.vertices:
        if v.co.z < below:
            v.co.z = below
    mesh.update()


def write_sway(obj, height, seed=0, phase_fn=None, flutter_fn=None):
    """Vertex colour 'Sway' on the game mesh (stored as raw bytes, exported as FBX vertex colours):
    R = bend weight (0 at the root, 1 at the top, eased), G = phase 0..1 (each stem moves on its own),
    B = flutter (thin tips and leaf edges shiver faster), A = 1. A Unity vertex shader bends by
    R * sin(time * speed + G * 2pi) and adds B * a fast small wobble."""
    mesh = obj.data
    co = np.empty(len(mesh.vertices) * 3)
    mesh.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    bend = np.clip(co[:, 2] / max(height, 1e-6), 0, 1) ** 1.6
    if phase_fn is None:
        ang = (np.arctan2(co[:, 1], co[:, 0]) / (2 * math.pi)) % 1.0
        phase = (ang + 0.13 * seed) % 1.0
    else:
        phase = phase_fn(co)
    flutter = flutter_fn(co) if flutter_fn else np.clip((co[:, 2] / max(height, 1e-6) - 0.6) / 0.4, 0, 1)
    attr = mesh.color_attributes.get("Sway") or mesh.color_attributes.new("Sway", "BYTE_COLOR", "POINT")
    rgba = np.c_[bend, phase, flutter, np.ones(len(bend))].astype(np.float32)
    attr.data.foreach_set("color_srgb", rgba.ravel())
    mesh.color_attributes.active_color = attr
    return attr


# ---------------------------------------------------------------------------------------------------------------
# Level lighting for previews: dark, moody, a little mist
# ---------------------------------------------------------------------------------------------------------------

def level_world(color=(0.012, 0.012, 0.03), strength=1.0, mist_density=0.0, mist_color=(0.22, 0.2, 0.42)):
    world = C.preview_world(color, strength)
    if mist_density > 0:
        nt = world.node_tree
        vol = nt.nodes.new("ShaderNodeVolumePrincipled")
        vol.inputs["Density"].default_value = mist_density
        vol.inputs["Color"].default_value = (*mist_color, 1)
        vol.inputs["Anisotropy"].default_value = 0.3
        out = nt.nodes.get("World Output")
        nt.links.new(vol.outputs[0], out.inputs["Volume"])
    return world


def level_lights(key=2.2, rim=1.1, key_color=(0.92, 0.9, 1.0), rim_color=(0.55, 0.5, 1.0), yaw=-30):
    """Like the game's ThemeLighting: a near-white key sun at pitch 42 from the camera side and a cool violet
    rim from behind."""
    objs = []
    k = bpy.data.lights.new("EnvKey", "SUN"); k.energy = key; k.color = key_color; k.angle = math.radians(3)
    o = C.link(bpy.data.objects.new("EnvKey", k)); o.rotation_euler = (math.radians(48), 0, math.radians(yaw)); objs.append(o)
    r = bpy.data.lights.new("EnvRim", "SUN"); r.energy = rim; r.color = rim_color; r.use_shadow = False
    o = C.link(bpy.data.objects.new("EnvRim", r)); o.rotation_euler = (math.radians(-62), 0, math.radians(25)); objs.append(o)
    return objs


def point_light(name, location, color, energy, radius=0.1):
    data = bpy.data.lights.new(name, "POINT")
    data.energy = energy
    data.color = color
    data.shadow_soft_size = radius
    obj = C.link(bpy.data.objects.new(name, data))
    obj.location = location
    return obj


def eevee_extras(volumetrics=True):
    scene = bpy.context.scene
    try:
        scene.eevee.use_volumetric_shadows = False
        scene.eevee.volumetric_tile_size = "4"
        scene.eevee.volumetric_end = 80
    except AttributeError:
        pass
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def import_fbx(path, name=None):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    return new


def textured_import(asset_dir, asset, emission_strength=2.0, details=True):
    """Imports an exported asset (Exports/.../<asset>/<asset>.fbx) and dresses it in a game_material from its maps."""
    objs = import_fbx(os.path.join(asset_dir, asset + ".fbx"))
    tex = {k.lower(): os.path.join(asset_dir, f"{asset}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
    mat = C.game_material(asset + "_Preview", tex, emission_strength, 0.4, ())
    for o in objs:
        if o.type == "MESH":
            o.data.materials.clear()
            o.data.materials.append(mat)
    return objs


# ---------------------------------------------------------------------------------------------------------------
# Vertex styles for kit pieces (same contract as build_props / stone.py: colour, glow, roughness, displacement,
# micro-detail family and flow per vertex of a part)
# ---------------------------------------------------------------------------------------------------------------

VEIN = np.array((1.0, 0.55, 0.88))
VEIN_GLOW = np.array((1.0, 0.45, 0.82))
EMBER = np.array((1.0, 0.6, 0.28))
VOID = np.array((0.45, 0.3, 1.0))
COLONY_GLOW = np.array((0.62, 0.86, 1.0))
HAZARD = np.array((0.95, 0.78, 0.18))

# Light top, mid sides, near-black underside; seam = crack colour.
KERB = dict(top=np.array((0.5, 0.5, 0.58)), side=np.array((0.24, 0.24, 0.31)), bottom=np.array((0.06, 0.06, 0.08)),
            seam=np.array((0.08, 0.075, 0.1)))
SLAB = dict(top=np.array((0.6, 0.6, 0.68)), side=np.array((0.36, 0.36, 0.44)), bottom=np.array((0.1, 0.1, 0.13)),
            seam=np.array((0.12, 0.11, 0.15)))
BASALT = dict(top=np.array((0.36, 0.36, 0.45)), side=np.array((0.18, 0.18, 0.25)), bottom=np.array((0.04, 0.04, 0.06)),
              seam=np.array((0.05, 0.05, 0.07)))
CRUST = dict(top=np.array((0.19, 0.19, 0.26)), side=np.array((0.13, 0.13, 0.18)), bottom=np.array((0.04, 0.04, 0.06)),
             seam=np.array((0.04, 0.04, 0.06)))
OBSIDIAN = dict(top=np.array((0.2, 0.17, 0.24)), side=np.array((0.1, 0.08, 0.12)), bottom=np.array((0.03, 0.025, 0.04)),
                seam=np.array((0.02, 0.02, 0.03)))

FAM_ROCK = (0.0, 0.0, 0.0, 1.0)
FAM_CRYSTAL = (0.0, 0.0, 1.0, 0.0)
FAM_CHITIN = (1.0, 0.0, 0.0, 0.0)
FAM_MEMBRANE = (0.0, 1.0, 0.0, 0.0)
# Our own side uses the suit family set (weave, ceramic, metal, rubber).
SUIT_SET = ("weave", "ceramic", "metal", "rubber")
FAM_CERAMIC = (0.0, 1.0, 0.0, 0.0)
FAM_METAL = (0.0, 0.0, 1.0, 0.0)


def nq(p, period=None):
    """Noise coordinates; with `period` the pattern repeats along x (chained pieces meet without a seam)."""
    if not period:
        return p
    a = 2 * math.pi * p[:, 0] / period
    r = period / (2 * math.pi)
    return np.c_[r * np.cos(a), r * np.sin(a) + 0.37 * p[:, 2], p[:, 1] + 0.61 * p[:, 2]]


def vn(q, scale, seed):
    return C.value_noise(q, scale, seed)


def stone(pal, seed=0, veins=0.0, strata=22.0, period=None, crack=0.6, foot=None, dust=0.0, rough=0.8,
          glow=None, depth_glow=None, smooth_top=0.0):
    """Hewn alien stone. `foot` = (zmax, side_sign): a thin Crysium seam glows at the foot of faces pointing
    along side_sign * y (the road side of a kerb). `depth_glow` = (z0, z1, colour, amount): glow rising from below
    (rift and chasm walls). `smooth_top`: worn, smoother tops (road-coloured slab lips)."""
    glow = VEIN if glow is None else glow

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        q = nq(p, period)
        col = C.chitin_gradient(n, pal["top"], pal["side"], pal["bottom"])
        band = 0.5 + 0.5 * np.sin(p[:, 2] * strata + vn(q, 3, seed) * 2.5)
        col = col * (0.84 + 0.16 * band)[:, None] * (1 + 0.14 * vn(q, 8, seed + 1))[:, None]
        cr = (1 - C.smoothstep(0.0, 0.05, np.abs(vn(q, 6, seed + 2)))) * C.smoothstep(-0.2, 0.3, vn(q, 1.7, seed + 3)) * crack
        col = C.lerp3(col, pal["seam"], cr * 0.8)
        emit = np.zeros((len(idx), 3))
        if veins:
            line = (1 - C.smoothstep(0.0, 0.05, np.abs(vn(q, 4.5, seed + 4)))) * C.smoothstep(-0.3, 0.25, vn(q, 1.3, seed + 5))
            line = line * veins
            col = C.lerp3(col, glow * 0.55, line)
            emit += glow[None, :] * (line * 0.9)[:, None]
        if foot:
            zmax, sign = foot
            f = (1 - C.smoothstep(0.0, zmax, p[:, 2])) * C.smoothstep(0.1, 0.6, sign * n[:, 1])
            f = f * (0.6 + 0.4 * C.smoothstep(-0.5, 0.5, vn(q, 5, seed + 6)))
            col = C.lerp3(col, VEIN * 0.6, f)
            emit += VEIN_GLOW[None, :] * (f * 0.55)[:, None]
        if depth_glow:
            z0, z1, gcol, amount = depth_glow
            d = C.smoothstep(z0, z1, p[:, 2]) * (0.55 + 0.45 * C.smoothstep(-0.4, 0.4, vn(q, 3, seed + 7)))
            col = C.lerp3(col, np.asarray(gcol) * 0.45, d * 0.6)
            emit += np.asarray(gcol)[None, :] * (d * amount)[:, None]
        top = C.smoothstep(0.5, 0.9, n[:, 2])
        if dust:
            dd = top * C.smoothstep(-0.3, 0.4, vn(q, 2.5, seed + 8)) * dust
            col = C.lerp3(col, np.array((0.3, 0.29, 0.35)), dd)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(emit, 0, 1)
        s.disp[idx] = 0.005 * vn(q, 14, seed + 9) - 0.004 * cr - 0.003 * band
        s.rough[idx] = np.clip(rough + 0.08 * vn(q, 6, seed + 10) - smooth_top * top, 0.2, 1)
        s.fam[idx] = FAM_ROCK
        s.flow[idx] = p[:, 2] + 0.03 * vn(q, 2, seed + 11)
    return style


def colony(base, seed=0, rough=0.5, fam=FAM_METAL, glow_fn=None, stripes=None, dirt=0.5, top=None):
    """Our side's hard surface: a flat base colour (optionally a lighter `top` on up-facing faces), grime and ground
    dust towards the foot, a few scratches. `glow_fn(p, n)` -> 0..1 colony light; `stripes` = (period, colour):
    diagonal hazard stripes over the whole part."""
    base = np.asarray(base, dtype=float)

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        col = np.repeat(base[None, :], len(idx), axis=0)
        if top is not None:
            col = C.lerp3(col, np.asarray(top), C.smoothstep(0.55, 0.85, n[:, 2]))
        if stripes is not None:
            period, scol = stripes
            st = C.smoothstep(0.45, 0.55, ((p[:, 0] + p[:, 1]) / period) % 1.0)
            col = C.lerp3(col, np.asarray(scol), st)
        col = col * (1 + 0.06 * vn(p, 10, seed))[:, None]
        foot = 1 - C.smoothstep(0.0, 0.06, p[:, 2])
        col = C.lerp3(col, np.array((0.2, 0.19, 0.23)), foot * dirt)
        scratch = (1 - C.smoothstep(0.0, 0.03, np.abs(vn(p, 40, seed + 1)))) * C.smoothstep(0.2, 0.6, vn(p, 4, seed + 2))
        col = C.lerp3(col, np.minimum(col * 1.6 + 0.08, 1), scratch * 0.5)
        emit = np.zeros((len(idx), 3))
        if glow_fn is not None:
            g = np.clip(glow_fn(p, n), 0, 1)
            col = C.lerp3(col, COLONY_GLOW, g)
            emit += COLONY_GLOW[None, :] * g[:, None]
        s.albedo[idx] = col
        s.emit[idx] = emit
        s.disp[idx] = 0.0
        s.rough[idx] = rough - 0.15 * scratch
        s.fam[idx] = fam
        s.flow[idx] = p[:, 2]
    return style


def glowing(color, strength=1.0, body=None, fam=FAM_CRYSTAL):
    """A part that is mostly light (colony lamp strips, egg cores)."""
    color = np.asarray(color, dtype=float)

    def style(s, idx, part):
        s.albedo[idx] = (body if body is not None else color * 0.7)
        s.emit[idx] = np.clip(color * strength, 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.35
        s.fam[idx] = fam
        s.flow[idx] = s.p[idx][:, 2]
    return style


# ---------------------------------------------------------------------------------------------------------------
# Shape helpers
# ---------------------------------------------------------------------------------------------------------------

def boxy(seed, power=0.25, chips=8, chip_depth=0.18):
    """Deform for PartBuilder.ellipsoid: a rounded box (superellipsoid) with a few chipped corners and edges:
    a cut block that has been knocked about."""
    rng = np.random.default_rng(seed)
    dirs = rng.normal(size=(chips, 3))
    dirs[:, 2] = np.abs(dirs[:, 2]) * 0.6 + 0.2  # chips mostly on the upper edges
    dirs /= np.linalg.norm(dirs, axis=1, keepdims=True)
    heights = 1.0 - rng.uniform(0.04, chip_depth, chips)

    def deform(v):
        u = np.array((v.x, v.y, v.z))
        b = np.sign(u) * np.abs(u) ** power
        dots = dirs @ b
        ok = dots > 1e-3
        r = float(np.min(heights[ok] / dots[ok])) if ok.any() else 1.0
        return Vector(b * min(r, 1.0))
    return deform


def sweep(b, name, path, profile_out, profile_in, nu, nv, style, jitter=None):
    """A closed slab swept along a horizontal `path(t)` -> (point, outward normal) for t in 0..1. The profiles map
    (t, s) with s in 0..1 to (d, z): d along the outward normal, z up. `jitter(t, s, d, z)` -> (dd, dz) roughens
    the outer face (keep it periodic in t for chained pieces)."""
    def surf(profile, rough):
        def f(t, s):
            point, normal = path(t)
            d, z = profile(t, s)
            if rough and jitter is not None:
                dd, dz = jitter(t, s, d, z)
                d, z = d + dd, z + dz
            return Vector((point[0] + normal[0] * d, point[1] + normal[1] * d, z))
        return f
    return b.slab(name, surf(profile_out, True), surf(profile_in, False), nu, nv, style=style)


def line_path(x0, x1, y=0.0, outward=(0.0, -1.0)):
    def path(t):
        return (x0 + (x1 - x0) * t, y), outward
    return path


def corner_path(radius, arm, low_inside=False):
    """Path round a corner point at the origin: along y = 0 from x = arm to x = radius, a quarter arc round
    (radius, radius), then along x = 0 up to y = arm. The outward normal points away from the (+x, +y) quadrant
    (the quadrant is the raised or solid side); `low_inside` flips it."""
    straight = max(arm - radius, 0.0)
    arc = math.pi * radius / 2
    total = 2 * straight + arc
    sign = -1.0 if low_inside else 1.0

    def path(t):
        s = t * total
        if s < straight:
            return (arm - s, 0.0), (0.0, -sign)
        s -= straight
        if s < arc:
            a = -math.pi / 2 - s / max(radius, 1e-6)
            return (radius + math.cos(a) * radius, radius + math.sin(a) * radius), (math.cos(a) * sign, math.sin(a) * sign)
        s -= arc
        return (0.0, radius + s), (-sign, 0.0)
    return path


def periodic(x, period, harmonics, seed, amps=None):
    """Smooth random function of x that repeats every `period`: a few integer harmonics, about -1..1."""
    coeffs = _PERIODIC.get((harmonics, seed))
    if coeffs is None:
        rng = np.random.default_rng(seed)
        coeffs = [(rng.uniform(0.3, 1.0) / k, rng.uniform(0, 2 * math.pi)) for k in range(1, harmonics + 1)]
        _PERIODIC[(harmonics, seed)] = coeffs
    out = 0.0
    for k, (a, ph) in enumerate(coeffs, start=1):
        out += a * math.sin(2 * math.pi * k * x / period + ph)
    return out


_PERIODIC = {}
