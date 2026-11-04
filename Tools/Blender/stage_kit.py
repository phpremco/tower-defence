"""Round 26 helpers for the menu diorama and the underground cutscene (build_menu_diorama.py, build_underground.py).

Read-only use of the shared modules (common.py, env_kit.py, planet_kit.py). Adds:

- Unity <-> Blender frames: the FBX export (forward -Z, up Y) plus Unity's x flip on import turn Blender (x, y, z)
  into Unity (-x, z, -y). So a Unity world point (x, y, z) is Blender (-x, -z, y), and a Unity yaw of a degrees
  (about +Y) is a Blender rotation of -a about +Z. (common.G keeps x unflipped; that only mirrors symmetric
  creatures, but a placed layout must use the true mapping.)
- 2D Perlin fbm / ridged noise on numpy arrays (planet_kit.perlin underneath), PNG load as linear float arrays,
  wrapped bilinear sampling of tiling sets.
- Importing an exported asset once (hidden source) and placing linked instances of it in Unity terms.
- A perspective camera built exactly like a Unity camera (position, Euler pitch/yaw, vertical field of view).
"""

import math
import os

import bpy
import numpy as np
from mathutils import Euler, Vector

import common as C
import planet_kit as P

F32 = np.float32
EXPORTS = C.EXPORTS
ENV = os.path.join(EXPORTS, "Env")


# --------------------------------------------------------------------------------------------------- frames

def u2b(x, y, z):
    """Unity world point -> Blender point."""
    return Vector((-x, -z, y))


def b2u(v):
    return (-v[0], v[2], -v[1])


def unity_camera(name, position, pitch, yaw, fov_vertical, clip_end=120.0, clip_start=0.1):
    """A Blender camera that sees what a Unity camera with rotation Euler(pitch, yaw, 0) and this vertical field of
    view sees (Unity forward = +Z, pitch > 0 looks down)."""
    data = bpy.data.cameras.new(name)
    data.sensor_fit = "VERTICAL"
    data.angle_y = math.radians(fov_vertical)
    data.clip_start = clip_start
    data.clip_end = clip_end
    cam = C.link(bpy.data.objects.new(name, data))
    cam.location = u2b(*position)
    p, y = math.radians(pitch), math.radians(yaw)
    fwd = (math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p))
    C.look_at(cam, cam.location + u2b(*fwd))
    return cam


def unity_orbit_position(focus, distance, pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    fwd = np.array((math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p)))
    return tuple(np.asarray(focus, float) - fwd * distance)


def unity_direction(pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    return (math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p))


def sun(name, unity_travel, color, energy, angle=3.0, shadow=True):
    """A sun whose light travels along a Unity direction."""
    data = bpy.data.lights.new(name, "SUN")
    data.energy = energy
    data.color = color
    data.angle = math.radians(angle)
    data.use_shadow = shadow
    obj = C.link(bpy.data.objects.new(name, data))
    d = u2b(*unity_travel)
    obj.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    return obj


def point(name, unity_pos, color, energy, radius=0.15):
    data = bpy.data.lights.new(name, "POINT")
    data.energy = energy
    data.color = color
    data.shadow_soft_size = radius
    obj = C.link(bpy.data.objects.new(name, data))
    obj.location = u2b(*unity_pos)
    return obj


# --------------------------------------------------------------------------------------------------- noise

def fbm2(x, z, freq, octaves, seed, gain=0.5, lac=2.03):
    """2D fractal Perlin on arrays, roughly -1..1."""
    x = np.asarray(x, F32)
    z = np.asarray(z, F32)
    acc = np.zeros(np.broadcast(x, z).shape, F32)
    amp, f, norm = 1.0, freq, 0.0
    for o in range(octaves):
        ox, oz, oy = 17.13 * (seed + 1) + o * 31.7, -9.71 * (seed + 1) + o * 17.3, 3.3 * seed + o * 0.71
        acc += amp * P.perlin(x * f + ox, z * f + oz, np.full_like(acc, oy))
        norm += amp
        amp *= gain
        f *= lac
    return acc * (1.6 / norm)


def ridged2(x, z, freq, octaves, seed, gain=0.5, lac=2.07):
    """Sharp crests 0..1 (veins, cracks, strata)."""
    x = np.asarray(x, F32)
    z = np.asarray(z, F32)
    acc = np.zeros(np.broadcast(x, z).shape, F32)
    weight = np.ones_like(acc)
    amp, f, norm = 1.0, freq, 0.0
    for o in range(octaves):
        ox, oz, oy = 11.9 * (seed + 1) + o * 23.1, -5.3 * (seed + 1) + o * 13.7, 1.7 * seed + o * 0.37
        s = 1.0 - np.abs(P.perlin(x * f + ox, z * f + oz, np.full_like(acc, oy)))
        s = s * s * weight
        weight = np.clip(s * 2.0, 0, 1)
        acc += amp * s
        norm += amp
        amp *= gain
        f *= lac
    return acc / norm


def fbm3(x, y, z, freq, octaves, seed, gain=0.5, lac=2.03):
    x, y, z = (np.asarray(a, F32) for a in (x, y, z))
    acc = np.zeros(np.broadcast(x, y, z).shape, F32)
    amp, f, norm = 1.0, freq, 0.0
    for o in range(octaves):
        off = 13.7 * (seed + 1) + o * 29.3
        acc += amp * P.perlin(x * f + off, y * f - off * 0.6, z * f + off * 0.3)
        norm += amp
        amp *= gain
        f *= lac
    return acc * (1.6 / norm)


def ss(a, b, x):
    t = np.clip((np.asarray(x, F32) - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    t = np.asarray(t, F32)
    if np.ndim(a) and np.shape(a)[-1] == 3 or np.ndim(b) and np.shape(b)[-1] == 3:
        t = t[..., None]
    return a + (b - a) * t


def cellular(x, z, cell, seed, jitter=0.85):
    """F1 and F2 - F1 distances of a jittered grid (in cell units) and a value per cell: cracks, honeycomb."""
    gx, gz = x / cell, z / cell
    ix, iz = np.floor(gx).astype(np.int64), np.floor(gz).astype(np.int64)
    best1 = np.full(gx.shape, 9.0, F32)
    best2 = np.full(gx.shape, 9.0, F32)
    val = np.zeros(gx.shape, F32)
    for dx in (-1, 0, 1):
        for dz in (-1, 0, 1):
            cx, cz = ix + dx, iz + dz
            h = (cx * 374761393 + cz * 668265263 + seed * 2147483647) & 0xFFFFFFFF
            h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
            jx = ((h & 0xFFFF) / 65535.0 - 0.5) * jitter + 0.5
            jz = (((h >> 16) & 0xFFFF) / 65535.0 - 0.5) * jitter + 0.5
            d = np.hypot(cx + jx - gx, cz + jz - gz).astype(F32)
            closer = d < best1
            best2 = np.where(closer, best1, np.minimum(best2, d))
            val = np.where(closer, ((h >> 7) & 0xFFFF) / 65535.0, val)
            best1 = np.where(closer, d, best1)
    return best1, best2 - best1, val


# --------------------------------------------------------------------------------------------------- images

def srgb_to_linear(c):
    c = np.asarray(c, F32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4).astype(F32)


def linear_to_srgb(c):
    c = np.clip(np.asarray(c, F32), 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055).astype(F32)


_PNG = {}


def load_png(path, linear_from_srgb=False):
    """PNG as float (H, W, 4), row 0 = bottom (Blender order = Unity v order)."""
    key = (path, linear_from_srgb)
    if key in _PNG:
        return _PNG[key]
    img = bpy.data.images.load(path, check_existing=False)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    arr = np.empty(w * h * 4, F32)
    img.pixels.foreach_get(arr)
    bpy.data.images.remove(img)
    arr = arr.reshape(h, w, 4)
    if linear_from_srgb:
        arr[..., :3] = srgb_to_linear(arr[..., :3])
    _PNG[key] = arr
    return arr


def sample_wrap(img, u, v):
    """Bilinear sample with wrap in both axes; u, v arrays (v = 0 bottom)."""
    H, W = img.shape[:2]
    x = np.asarray(u, F32) * W - 0.5
    y = np.asarray(v, F32) * H - 0.5
    x0 = np.floor(x).astype(np.int64)
    y0 = np.floor(y).astype(np.int64)
    fx = (x - x0)[..., None]
    fy = (y - y0)[..., None]
    x1, y1 = (x0 + 1) % W, (y0 + 1) % H
    x0, y0 = x0 % W, y0 % H
    return ((img[y0, x0] * (1 - fx) + img[y0, x1] * fx) * (1 - fy)
            + (img[y1, x0] * (1 - fx) + img[y1, x1] * fx) * fy)


def write_png(path, img, bits=8):
    """Float image, row 0 = bottom (written top-down)."""
    P.write_png(path, img, bits)
    C.log("wrote", path)


def half(img):
    return P.box_down(img, 2)


def blur(img, radius):
    """Separable gaussian (no wrap) for (H, W) or (H, W, C)."""
    if radius <= 0:
        return img
    r = int(math.ceil(radius * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / radius) ** 2).astype(F32)
    k /= k.sum()
    out = np.asarray(img, F32)
    for axis in (0, 1):
        pad = [(0, 0)] * out.ndim
        pad[axis] = (r, r)
        p = np.pad(out, pad, mode="edge")
        acc = np.zeros_like(out)
        n = out.shape[axis]
        for i, kv in enumerate(k):
            acc += kv * np.take(p, np.arange(i, i + n), axis=axis)
        out = acc
    return out


def encode_normal(n):
    return np.clip(n * 0.5 + 0.5, 0, 1)


def decode_normal(rgb):
    n = rgb[..., :3] * 2 - 1
    return n / np.maximum(np.linalg.norm(n, axis=-1, keepdims=True), 1e-6)


def rnm(base, detail, strength=1.0):
    """Reoriented normal mapping of two tangent normals (unit vectors), detail scaled in xy first."""
    d = detail.copy()
    d[..., :2] *= strength
    d /= np.linalg.norm(d, axis=-1, keepdims=True)
    t = base + np.array((0, 0, 1), F32)
    u = d * np.array((-1, -1, 1), F32)
    r = t * (np.sum(t * u, axis=-1, keepdims=True) / np.maximum(t[..., 2:3], 1e-6)) - u
    return r / np.maximum(np.linalg.norm(r, axis=-1, keepdims=True), 1e-6)


# --------------------------------------------------------------------------------------------------- assets

class Library:
    """Exported assets imported once each (hidden), placed as linked instances."""

    def __init__(self, emission_strength=2.4):
        self.sources = {}
        self.emission = emission_strength
        self.collection = bpy.data.collections.new("Placed")
        bpy.context.scene.collection.children.link(self.collection)

    def source(self, ref):
        """ref: (folder relative to Exports, fbx base name, texture prefix)."""
        if ref in self.sources:
            return self.sources[ref]
        folder, fbx, prefix = ref
        d = os.path.join(EXPORTS, folder)
        path = os.path.join(d, fbx + ".fbx")
        if not os.path.exists(path):
            C.log("missing", path)
            self.sources[ref] = None
            return None
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=path)
        objs = [o for o in bpy.data.objects if o not in before]
        tex = {k.lower(): os.path.join(d, f"{prefix}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        if all(os.path.exists(p) for p in tex.values()):
            mat = C.game_material(prefix + "_Preview", tex, self.emission, 0.4, ())
        else:
            mat = None
        meshes = [o for o in objs if o.type == "MESH"]
        for o in objs:
            o.hide_render = True
            o.hide_viewport = True
        for m in meshes:
            if mat is not None:
                keep = np.zeros(len(m.data.polygons), np.int32)
                m.data.polygons.foreach_get("material_index", keep)
                slots = max(1, len(m.data.materials))
                m.data.materials.clear()
                for _ in range(slots):
                    m.data.materials.append(mat)
                m.data.polygons.foreach_set("material_index", keep)
        src = meshes[0] if meshes else None
        if src is not None:
            # Fold the import transform into a clean data copy so instances only carry the placement.
            src.data = src.data.copy()
            src.data.transform(src.matrix_world)
        self.sources[ref] = src
        return src

    def place(self, ref, unity_pos, yaw=0.0, scale=1.0, name=None, tilt=(0.0, 0.0), flip=False):
        src = self.source(ref)
        if src is None:
            return None
        obj = bpy.data.objects.new(name or ref[1], src.data)
        self.collection.objects.link(obj)
        obj.location = u2b(*unity_pos)
        rx, ry = math.radians(tilt[0]), math.radians(tilt[1])
        rot = Euler((rx + (math.pi if flip else 0.0), ry, -math.radians(yaw)), "XYZ")
        obj.rotation_euler = rot
        obj.scale = (scale, scale, scale)
        return obj


def emission_plane(name, unity_centre, size, image_path, strength=1.0, tint=(1, 1, 1)):
    """A flat emissive plane (starfields, far backdrops) facing up in Unity terms."""
    mesh = bpy.data.meshes.new(name)
    h = size / 2
    mesh.from_pydata([(-h, -h, 0), (h, -h, 0), (h, h, 0), (-h, h, 0)], [], [(0, 1, 2, 3)])
    uv = mesh.uv_layers.new()
    for i, c in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))):
        uv.data[i].uv = c
    obj = C.link(bpy.data.objects.new(name, mesh))
    obj.location = u2b(*unity_centre)
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(image_path, check_existing=True)
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    nt.links.new(img.outputs["Color"], mul.inputs[6])
    mul.inputs[7].default_value = (*tint, 1)
    nt.links.new(mul.outputs[2], em.inputs["Color"])
    em.inputs["Strength"].default_value = strength
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    mesh.materials.append(mat)
    obj.visible_shadow = False
    return obj


def image_material(name, albedo=None, normal=None, emission=None, mask=None, emission_strength=1.0, rough=None,
                   color=None, alpha=None, specular=0.4):
    """A Principled material from optional map paths (mask: R AO, G smoothness)."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = specular

    def tex(path, non_color):
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(path, check_existing=True)
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        return n
    if albedo:
        nt.links.new(tex(albedo, False).outputs["Color"], bsdf.inputs["Base Color"])
    elif color is not None:
        bsdf.inputs["Base Color"].default_value = (*color, 1)
    if mask:
        m = tex(mask, True)
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(m.outputs["Color"], sep.inputs[0])
        inv = nt.nodes.new("ShaderNodeMath")
        inv.operation = "SUBTRACT"
        inv.inputs[0].default_value = 1.0
        nt.links.new(sep.outputs[1], inv.inputs[1])
        nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    elif rough is not None:
        bsdf.inputs["Roughness"].default_value = rough
    if emission:
        nt.links.new(tex(emission, False).outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    if normal:
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(tex(normal, True).outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    if alpha is not None:
        bsdf.inputs["Alpha"].default_value = alpha
    return mat


def eevee(width, height, samples=64, bloom=False):
    C.setup_eevee(width, height, samples)
    scene = bpy.context.scene
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass
    return scene


def render(cam, path, width, height, samples=64, bloom=True, threshold=0.7, strength=0.55):
    scene = bpy.context.scene
    eevee(width, height, samples)
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    if bloom:
        C.add_bloom(path, threshold=threshold, strength=strength)
    C.log("render", path)
    return path
