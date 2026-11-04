"""Round 25 helpers: small gameplay markers, decals, bars, projectiles and particle sprites.

Own module (the shared kits are only imported read-only). Three parts:
1. Texture painting in numpy on a pixel grid (rows bottom-up, as Blender stores pixels), saved as PNG with a
   half-size copy in `Low/`. All marker textures are linear data (import with sRGB off): channels are masks that
   the Unity shader tints, so one texture serves every theme colour.
2. Tiny meshes from explicit vertices, faces and per-corner UVs (plus an optional second UV set), with the pivot
   where the game code expects it.
3. Preview materials (additive or alpha-blended emission from a packed texture and a tint) for EEVEE renders.

Axes: Blender is Z-up; with the shared FBX settings Blender -Y arrives as Unity +Z and Blender +Y as Unity -Z (the
camera side). Flat decals lie in Blender XY (Unity XZ, normal up).
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Vector

import common as C
import tower_kit as tk

TAU = 2.0 * math.pi


# ---------------------------------------------------------------------------------------------------------------
# Texture painting
# ---------------------------------------------------------------------------------------------------------------

def grid(width, height):
    """Pixel-centre coordinates u, v in 0..1 (v = 0 is the bottom row, as Blender stores pixels)."""
    u = (np.arange(width) + 0.5) / width
    v = (np.arange(height) + 0.5) / height
    return np.meshgrid(u, v)


def centred(width, height):
    """Coordinates -1..1 around the texture centre, and the radius."""
    u, v = grid(width, height)
    x, y = u * 2 - 1, v * 2 - 1
    return x, y, np.hypot(x, y)


def sstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def gauss(x, width):
    return np.exp(-(x / np.maximum(width, 1e-6)) ** 2)


def line(d, width, soft):
    """A line of half-width `width` around d = 0 with a soft edge `soft` (anti-aliased)."""
    return 1.0 - sstep(width, width + soft, np.abs(d))


def _hash2(ix, iy, seed):
    h = np.sin(ix * 127.1 + iy * 311.7 + seed * 74.7) * 43758.5453
    return h - np.floor(h)


def value_noise(u, v, cells_u, cells_v, seed=0):
    """Tileable value noise (period 1 in u and v) with `cells` lattice cells per tile."""
    x, y = u * cells_u, v * cells_v
    ix, iy = np.floor(x), np.floor(y)
    fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)

    def h(dx, dy):
        return _hash2(np.mod(ix + dx, cells_u), np.mod(iy + dy, cells_v), seed)
    a, b, c, d = h(0, 0), h(1, 0), h(0, 1), h(1, 1)
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(u, v, cells, octaves=4, seed=0, cells_v=None):
    cells_v = cells if cells_v is None else cells_v
    out, amp, total = np.zeros_like(u), 1.0, 0.0
    for o in range(octaves):
        out += amp * value_noise(u, v, cells * 2 ** o, cells_v * 2 ** o, seed + o * 13)
        total += amp
        amp *= 0.5
    return out / total


def rounded_rect(x, y, half_w, half_h, radius):
    """Signed distance to a rounded rectangle centred at 0 (negative inside)."""
    qx = np.abs(x) - (half_w - radius)
    qy = np.abs(y) - (half_h - radius)
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - radius


def pack(*channels):
    """Stacks up to four float channels into an RGBA array (missing ones are 0, alpha defaults to the max)."""
    chans = [np.clip(c, 0, 1).astype(np.float32) for c in channels]
    while len(chans) < 3:
        chans.append(np.zeros_like(chans[0]))
    if len(chans) == 3:
        chans.append(np.max(np.stack(chans), axis=0))
    return np.dstack(chans[:4])


def save_tex(asset, name, arr, low=True, colorspace="Non-Color", normal=False):
    """Writes Exports/<asset>/<name>.png (+ Low/<name>.png at half size). Returns the full-size path."""
    folder = os.path.join(C.EXPORTS, asset)
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name + ".png")
    C.array_to_image(arr, path, colorspace)
    if low:
        os.makedirs(os.path.join(folder, "Low"), exist_ok=True)
        C.array_to_image(C.downsample(arr, 2, normal=normal), os.path.join(folder, "Low", name + ".png"), colorspace)
    return path


def height_to_normal(height, strength, wrap=False):
    """Tangent-space normal map (+Y up, OpenGL) from a height field in pixels."""
    if wrap:
        dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 0.5
        dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 0.5
    else:
        dy, dx = np.gradient(height)
    n = np.dstack([-dx * strength, -dy * strength, np.ones_like(height)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.dstack([n * 0.5 + 0.5, np.ones_like(height)])


# ---------------------------------------------------------------------------------------------------------------
# Meshes
# ---------------------------------------------------------------------------------------------------------------

def mesh_object(name, verts, faces, uvs, uv2=None, smooth=False, sharp=None):
    """A mesh object from vertices, faces and per-face-corner UVs (list per face). `uv2`: optional second set
    (same layout). Applies smooth shading (with sharp edges above `sharp` degrees) when asked."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
    mesh.update()
    flat_uv = [c for f in uvs for c in f]
    layer = mesh.uv_layers.new(name="UVMap")
    layer.data.foreach_set("uv", np.array(flat_uv, dtype=np.float32).ravel())
    if uv2 is not None:
        layer2 = mesh.uv_layers.new(name="UV2")
        layer2.data.foreach_set("uv", np.array([c for f in uv2 for c in f], dtype=np.float32).ravel())
    if smooth:
        mesh.shade_smooth()
        if sharp is not None:
            mesh.set_sharp_from_angle(angle=math.radians(sharp))
    else:
        mesh.shade_flat()
    return C.link(bpy.data.objects.new(name, mesh))


def quad(name, width, height, plane="XY", pivot=(0.5, 0.5), segments=1, uv_rect=(0, 0, 1, 1), uv2_rect=None,
         offset=(0, 0, 0)):
    """A quad (in `segments` strips along its width). plane XY lies flat (Unity XZ, normal up); XZ stands upright
    facing Blender +Y (Unity -Z, the camera). `pivot` is the point in 0..1 of the quad that sits at the origin;
    `uv_rect` (u0, v0, u1, v1) maps the quad into an atlas, `uv2_rect` adds a second set."""
    verts, faces, uvs, uv2 = [], [], [], []
    for i in range(segments + 1):
        s = i / segments
        for t in (0.0, 1.0):
            a, b = (s - pivot[0]) * width, (t - pivot[1]) * height
            p = (a, b, 0.0) if plane == "XY" else (a, 0.0, b)
            verts.append(Vector(p) + Vector(offset))
    for i in range(segments):
        i0, i1, i2, i3 = 2 * i, 2 * i + 2, 2 * i + 3, 2 * i + 1
        # XZ faces must point to Blender +Y: wind them the other way round.
        faces.append((i0, i1, i2, i3) if plane == "XY" else (i0, i3, i2, i1))

        def corner(k, rect):
            s = (k // 2) / segments
            # Upright quads: U starts at Blender +X, which is Unity -X (screen left under the game camera).
            s = s if plane == "XY" else 1.0 - s
            t = k % 2
            return (rect[0] + (rect[2] - rect[0]) * s, rect[1] + (rect[3] - rect[1]) * t)
        order = (i0, i1, i2, i3) if plane == "XY" else (i0, i3, i2, i1)
        uvs.append([corner(k, uv_rect) for k in order])
        if uv2_rect is not None:
            uv2.append([corner(k, uv2_rect) for k in order])
    return mesh_object(name, verts, faces, uvs, uv2 if uv2_rect is not None else None)


def annulus(name, inner, outer, segments, u_tiles=1.0, z=0.0, uv2_band=True):
    """A flat ring in Blender XY: U runs around (u_tiles over the full turn), V from the inner (0) to the outer
    edge (1). UV2.x is the same around-coordinate 0..1, UV2.y 0 inner / 1 outer (for a constant-width shader)."""
    verts, faces, uvs, uv2 = [], [], [], []
    for i in range(segments + 1):
        a = TAU * i / segments
        c, s = math.cos(a), math.sin(a)
        verts += [(c * inner, s * inner, z), (c * outer, s * outer, z)]
    for i in range(segments):
        a0, b0, a1, b1 = 2 * i, 2 * i + 1, 2 * i + 2, 2 * i + 3
        faces.append((a0, a1, b1, b0))
        u0, u1 = i / segments, (i + 1) / segments
        uvs.append([(u0 * u_tiles, 0), (u1 * u_tiles, 0), (u1 * u_tiles, 1), (u0 * u_tiles, 1)])
        uv2.append([(u0, 0), (u1, 0), (u1, 1), (u0, 1)])
    return mesh_object(name, verts, faces, uvs, uv2 if uv2_band else None)


def disc(name, radius, segments, z=0.0):
    """A flat disc (triangle fan in Blender XY) with planar UVs: (0.5, 0.5) at the centre, the rim on the unit
    circle of the texture."""
    verts = [(0.0, 0.0, z)]
    for i in range(segments):
        a = TAU * i / segments
        verts.append((math.cos(a) * radius, math.sin(a) * radius, z))
    faces, uvs = [], []
    for i in range(segments):
        j = 1 + (i + 1) % segments
        faces.append((0, 1 + i, j))
        uvs.append([(0.5 + v[0] / radius * 0.5, 0.5 + v[1] / radius * 0.5) for v in (verts[0], verts[1 + i], verts[j])])
    return mesh_object(name, verts, faces, uvs)


def crossed(name, length, width, axis="Y", planes=2, pivot=0.5, uv_rect=(0, 0, 1, 1)):
    """`planes` quads crossing on a common axis (Blender Y or X), U along the axis, V across: a glow shell or strand
    that reads from any side. `pivot` 0..1 along the axis sits at the origin."""
    verts, faces, uvs = [], [], []
    u0, v0, u1, v1 = uv_rect
    for p in range(planes):
        ang = math.pi * p / planes
        side = Vector((math.cos(ang), 0, math.sin(ang))) if axis == "Y" else Vector((0, math.cos(ang), math.sin(ang)))
        along = Vector((0, -1, 0)) if axis == "Y" else Vector((1, 0, 0))
        base = len(verts)
        for s in (0.0, 1.0):
            for t in (-0.5, 0.5):
                verts.append(along * ((s - pivot) * length) + side * (t * width))
        faces.append((base, base + 2, base + 3, base + 1))
        uvs.append([(u0, v0), (u1, v0), (u1, v1), (u0, v1)])
    return mesh_object(name, verts, faces, uvs)


def lathe(name, profile, sides, u_tiles=1.0, axis="Z", cap=False):
    """A surface of revolution: profile = [(radius, height), ...] along the axis (Blender Z, or Y for a part that
    points along Unity +Z: height runs towards Blender -Y). U around, V along the profile (by arc length)."""
    verts, faces, uvs = [], [], []
    lengths = [0.0]
    for i in range(1, len(profile)):
        lengths.append(lengths[-1] + math.dist(profile[i], profile[i - 1]))
    total = lengths[-1] or 1.0
    rows = len(profile)
    for i in range(sides + 1):
        a = TAU * i / sides
        c, s = math.cos(a), math.sin(a)
        for r, h in profile:
            if axis == "Z":
                verts.append((c * r, s * r, h))
            else:
                verts.append((c * r, -h, s * r))
    for i in range(sides):
        for j in range(rows - 1):
            a0, a1 = i * rows + j, i * rows + j + 1
            b0, b1 = (i + 1) * rows + j, (i + 1) * rows + j + 1
            faces.append((a0, b0, b1, a1) if axis == "Z" else (a0, a1, b1, b0))
            q = [(i / sides * u_tiles, lengths[j] / total), ((i + 1) / sides * u_tiles, lengths[j] / total),
                 ((i + 1) / sides * u_tiles, lengths[j + 1] / total), (i / sides * u_tiles, lengths[j + 1] / total)]
            uvs.append(q if axis == "Z" else [q[0], q[3], q[2], q[1]])
    obj = mesh_object(name, verts, faces, uvs, smooth=True)
    # Merge the seam and the collapsed poles (radius 0 rings) so normals stay smooth across them.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.shade_smooth()
    return obj


def join(objs, name):
    C.activate(objs[0])
    for o in objs[1:]:
        o.select_set(True)
    bpy.ops.object.join()
    objs[0].name = name
    objs[0].data.name = name
    return objs[0]


def tris(obj):
    return C.tri_count(obj)


def export(path, objects, animated=False):
    tk.export_fbx(path, objects, animated)


def remove(objs):
    for o in objs:
        if o is not None and o.name in bpy.data.objects:
            bpy.data.objects.remove(o, do_unlink=True)


# ---------------------------------------------------------------------------------------------------------------
# Preview materials
# ---------------------------------------------------------------------------------------------------------------

def _load(path, colorspace="Non-Color"):
    img = bpy.data.images.load(path, check_existing=True)
    img.colorspace_settings.name = colorspace
    return img


def preview_image(name, arr):
    """A float image in Blender from an RGBA array (straight colour, alpha in A), for preview materials."""
    h, w = arr.shape[:2]
    img = bpy.data.images.new(name, w, h, alpha=True, float_buffer=True)
    img.colorspace_settings.name = "Linear Rec.709"
    img.pixels.foreach_set(np.clip(arr, 0, 64).astype(np.float32).ravel())
    img.pack()
    return img


def tinted(arr, recipe):
    """What the Unity shader will show, from a packed texture: recipe = [(channel index, colour, gain), ...]
    summed (additive), alpha from `recipe`'s optional ("alpha", channel) entry or the summed brightness."""
    rgb = np.zeros(arr.shape[:2] + (3,), dtype=np.float32)
    alpha = None
    for entry in recipe:
        if entry[0] == "alpha":
            alpha = arr[..., entry[1]] * (entry[2] if len(entry) > 2 else 1.0)
            continue
        ch, colour, gain = entry
        rgb += arr[..., ch][..., None] * np.array(colour, dtype=np.float32)[None, None, :] * gain
    if alpha is None:
        # Additive recipes: the colour already carries the coverage.
        alpha = np.ones(arr.shape[:2], dtype=np.float32)
    return np.dstack([rgb, alpha])


def fx_material(name, img, mode="add", strength=1.0, uv="UVMap"):
    """Emission from `img` (rgb, straight alpha). mode "add": added over the scene; "alpha": blended over it."""
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except Exception:
        pass
    try:
        mat.surface_render_method = "BLENDED"
    except Exception:
        pass
    try:
        mat.use_backface_culling = False
    except Exception:
        pass
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = uv
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Linear"
    nt.links.new(uvn.outputs["UV"], tex.inputs["Vector"])
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Strength"].default_value = strength
    nt.links.new(tex.outputs["Color"], emit.inputs["Color"])
    transp = nt.nodes.new("ShaderNodeBsdfTransparent")
    if mode == "add":
        # Additive: colour already carries the coverage, so emission x alpha plus a fully clear surface.
        mul = nt.nodes.new("ShaderNodeMixRGB")
        mul.blend_type = "MULTIPLY"
        mul.inputs["Fac"].default_value = 1.0
        nt.links.new(tex.outputs["Color"], mul.inputs[1])
        nt.links.new(tex.outputs["Alpha"], mul.inputs[2])
        nt.links.new(mul.outputs["Color"], emit.inputs["Color"])
        add = nt.nodes.new("ShaderNodeAddShader")
        nt.links.new(emit.outputs[0], add.inputs[0])
        nt.links.new(transp.outputs[0], add.inputs[1])
        nt.links.new(add.outputs[0], out.inputs["Surface"])
    else:
        mix = nt.nodes.new("ShaderNodeMixShader")
        nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
        nt.links.new(transp.outputs[0], mix.inputs[1])
        nt.links.new(emit.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], out.inputs["Surface"])
    return mat


def lit_material(name, albedo_path, emission_path=None, emission_strength=2.0, rough=0.45, normal_path=None):
    """A simple lit preview material from an albedo (sRGB) and optional emission and normal maps."""
    mat = bpy.data.materials.new(name)
    try:
        mat.use_nodes = True
    except Exception:
        pass
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = _load(albedo_path, "sRGB")
    nt.links.new(t.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    if emission_path:
        e = nt.nodes.new("ShaderNodeTexImage")
        e.image = _load(emission_path, "sRGB")
        nt.links.new(e.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    if normal_path:
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = _load(normal_path, "Non-Color")
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(n.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    obj.visible_shadow = False
    return obj


# ---------------------------------------------------------------------------------------------------------------
# Texture sheets for the review
# ---------------------------------------------------------------------------------------------------------------

def texture_card(arr, recipe, out_path, cell=256, background=(0.03, 0.035, 0.05), mode="add"):
    """Renders a packed texture as the shader would show it, composited over a dark checker, fitted into a
    `cell` x `cell` square (aspect kept, centred)."""
    view = tinted(arr, recipe)
    h, w = view.shape[:2]
    scale = cell / max(w, h)
    hh, ww = max(1, int(round(h * scale))), max(1, int(round(w * scale)))
    ys = np.clip((np.arange(hh) / scale).astype(int), 0, h - 1)
    xs = np.clip((np.arange(ww) / scale).astype(int), 0, w - 1)
    v = np.zeros((cell, cell, 4), dtype=np.float32)
    y0, x0 = (cell - hh) // 2, (cell - ww) // 2
    v[y0:y0 + hh, x0:x0 + ww] = view[ys][:, xs]
    bg = np.ones((cell, cell, 3), dtype=np.float32) * np.array(background, dtype=np.float32)
    # Checker under the card so alpha shows.
    cy, cx = np.meshgrid(np.arange(cell) // 16, np.arange(cell) // 16, indexing="ij")
    bg *= (1.0 + 0.35 * ((cx + cy) % 2))[..., None]
    a = v[..., 3:4]
    if mode == "add":
        rgb = np.clip(bg + v[..., :3] * a, 0, 1)
    else:
        rgb = bg * (1 - a) + np.clip(v[..., :3], 0, 1) * a
    out = np.dstack([rgb, np.ones((cell, cell), dtype=np.float32)])
    C.array_to_image(out, out_path)
    return out_path
