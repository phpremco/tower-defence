"""Helpers for the UI art set (round 25, `build_ui.py`): a pixel-unit orthographic Blender scene, flat 2D curve
shapes with a small bevel, emission materials whose brightness follows the bevel normal (so every icon and frame
gets the same top-left light without any lamp), Cycles renders read back as float arrays, and the numpy compositing
that follows (glow, dark rim, premultiplied over, packing, 9-slice drawing for the mockups).

Coordinates: one Blender unit is one pixel of the 2x master; x right, y UP (Blender). Arrays handed back by
`render` are row 0 = top, like the PNGs. Shapes are given in a cell's unit square (u right, v up) through `Cell`.
"""

import math
import os
import tempfile

import bpy
import numpy as np

import vfx_kit as VK

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "ArtSource", "Exports", "UI")
FONT = os.path.join(REPO, "Assets", "_Project", "Art", "UI", "Fonts", "KenneyFuture.ttf")
FONT_NARROW = os.path.join(REPO, "Assets", "_Project", "Art", "UI", "Fonts", "KenneyFutureNarrow.ttf")

# The light every bevel is shaded with: from the top left, 45 degrees up.
LIGHT = (-0.5, 0.5, 0.7071)
TMP = os.path.join(tempfile.gettempdir(), "td_ui_render")

save_png = VK.save_png
blur = VK.blur
resize = VK.resize


# ------------------------------------------------------------------------------------------------------- scene

def reset():
    _layers.clear()
    for collection in (bpy.data.objects, bpy.data.curves, bpy.data.meshes, bpy.data.materials, bpy.data.cameras,
                       bpy.data.images):
        for item in list(collection):
            collection.remove(item)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 0
    scene.cycles.transparent_max_bounces = 16
    scene.cycles.filter_width = 1.2
    scene.render.film_transparent = True
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "OPEN_EXR"
    scene.render.image_settings.color_depth = "32"
    scene.view_settings.view_transform = "Standard"
    scene.world = scene.world or bpy.data.worlds.new("World")
    return scene


_layers = {}


def layer(name):
    """A collection that can be rendered on its own (render() hides the others)."""
    if name in _layers and _layers[name].name in bpy.data.collections:
        return _layers[name]
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    _layers[name] = c
    return c


def _materials():
    """Two shared materials driven by the object colour: 'shaded' (bevel light) and 'flat' (pure colour)."""
    if "UI_Shaded" in bpy.data.materials:
        return bpy.data.materials["UI_Shaded"], bpy.data.materials["UI_Flat"]
    mats = []
    for name, shaded in (("UI_Shaded", True), ("UI_Flat", False)):
        m = bpy.data.materials.new(name)
        nt = m.node_tree if m.node_tree else None
        if nt is None:
            m.use_nodes = True
            nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission")
        info = nt.nodes.new("ShaderNodeObjectInfo")
        if shaded:
            geo = nt.nodes.new("ShaderNodeNewGeometry")
            dot = nt.nodes.new("ShaderNodeVectorMath")
            dot.operation = "DOT_PRODUCT"
            dot.inputs[1].default_value = LIGHT
            nt.links.new(geo.outputs["Normal"], dot.inputs[0])
            # 1 on the flat top; brighter on bevels facing the light, darker on the far side.
            rng = nt.nodes.new("ShaderNodeMapRange")
            rng.clamp = True
            rng.inputs["From Min"].default_value = 0.0
            rng.inputs["From Max"].default_value = LIGHT[2]
            rng.inputs["To Min"].default_value = 0.42
            rng.inputs["To Max"].default_value = 1.0
            hi = nt.nodes.new("ShaderNodeMapRange")
            hi.clamp = True
            hi.inputs["From Min"].default_value = LIGHT[2]
            hi.inputs["From Max"].default_value = 1.0
            hi.inputs["To Min"].default_value = 0.0
            hi.inputs["To Max"].default_value = 0.45
            nt.links.new(dot.outputs["Value"], rng.inputs["Value"])
            nt.links.new(dot.outputs["Value"], hi.inputs["Value"])
            add = nt.nodes.new("ShaderNodeMath")
            add.operation = "ADD"
            nt.links.new(rng.outputs[0], add.inputs[0])
            nt.links.new(hi.outputs[0], add.inputs[1])
            nt.links.new(add.outputs[0], em.inputs["Strength"])
        else:
            em.inputs["Strength"].default_value = 1.0
        nt.links.new(info.outputs["Color"], em.inputs["Color"])
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        mats.append(m)
    return mats[0], mats[1]


def gradient_material(name, build):
    """A material whose node tree `build(nt, out)` wires up; used for glows and backdrops."""
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    build(nt, out)
    return m


def camera(w, h):
    scene = bpy.context.scene
    cam = bpy.data.cameras.new("UICam")
    cam.type = "ORTHO"
    cam.ortho_scale = float(max(w, h))
    cam.clip_start = 0.1
    cam.clip_end = 2000.0
    ob = bpy.data.objects.new("UICam", cam)
    ob.location = (w / 2.0, h / 2.0, 500.0)
    scene.collection.objects.link(ob)
    scene.camera = ob
    scene.render.resolution_x = int(w)
    scene.render.resolution_y = int(h)
    return ob


def render(w, h, only=None, samples=24):
    """Renders the scene (or only the named layers) and returns an H x W x 4 float array, straight alpha, top row
    first."""
    scene = bpy.context.scene
    scene.cycles.samples = samples
    for c in scene.collection.children:
        c.hide_render = only is not None and c.name not in only
    os.makedirs(TMP, exist_ok=True)
    path = os.path.join(TMP, f"ui_{os.getpid()}.exr")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path, check_existing=False)
    buf = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    a = buf.reshape(h, w, 4)[::-1].astype(np.float64)
    a = np.nan_to_num(a)
    a[..., 3] = np.clip(a[..., 3], 0, 1)
    # Cycles writes premultiplied colour; hand back straight alpha.
    a[..., :3] = np.where(a[..., 3:4] > 1e-5, a[..., :3] / np.maximum(a[..., 3:4], 1e-5), 0.0)
    return a


# ------------------------------------------------------------------------------------------------- 2D shapes

def _curve_object(name, loops, coll, z, colour, bevel, shaded, depth=1.0):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    cu.resolution_u = 1
    cu.extrude = depth
    if bevel > 0:
        cu.bevel_depth = bevel
        cu.bevel_resolution = 4
        # Pull the outline in by the bevel so the silhouette keeps the size it was drawn at.
        cu.offset = -bevel
    for loop in loops:
        sp = cu.splines.new("POLY")
        sp.points.add(len(loop) - 1)
        for p, (x, y) in zip(sp.points, loop):
            p.co = (x, y, 0.0, 1.0)
        sp.use_cyclic_u = True
        sp.use_smooth = True
    ob = bpy.data.objects.new(name, cu)
    ob.location.z = z
    ob.color = (colour[0], colour[1], colour[2], 1.0)
    shaded_mat, flat_mat = _materials()
    cu.materials.append(shaded_mat if shaded else flat_mat)
    coll.objects.link(ob)
    return ob


def circle_pts(cx, cy, r, n=96, a0=0.0, a1=None, ccw=True):
    if a1 is None:
        return [(cx + r * math.cos(a0 + i * 2 * math.pi / n), cy + r * math.sin(a0 + i * 2 * math.pi / n)) for i in range(n)]
    steps = max(3, int(abs(a1 - a0) / (2 * math.pi) * n))
    return [(cx + r * math.cos(a0 + (a1 - a0) * i / steps), cy + r * math.sin(a0 + (a1 - a0) * i / steps)) for i in range(steps + 1)]


def capsule_pts(ax, ay, bx, by, hw, n=24):
    ang = math.atan2(by - ay, bx - ax)
    pts = []
    for i in range(n + 1):
        a = ang + math.pi / 2 - math.pi * i / n
        pts.append((bx + hw * math.cos(a), by + hw * math.sin(a)))
    for i in range(n + 1):
        a = ang - math.pi / 2 - math.pi * i / n
        pts.append((ax + hw * math.cos(a), ay + hw * math.sin(a)))
    return pts


def sector_pts(cx, cy, r0, r1, a0, a1, n=96):
    outer = circle_pts(cx, cy, r1, n, a0, a1)
    inner = circle_pts(cx, cy, r0, n, a1, a0)
    return outer + inner


def star_pts(cx, cy, ro, ri, points=5, rot=math.pi / 2):
    out = []
    for i in range(points * 2):
        r = ro if i % 2 == 0 else ri
        a = rot + i * math.pi / points
        out.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return out


def hex_pts(cx, cy, r, flat_top=True):
    off = 0.0 if flat_top else math.pi / 6
    return [(cx + r * math.cos(off + i * math.pi / 3), cy + r * math.sin(off + i * math.pi / 3)) for i in range(6)]


def offset_convex(pts, d):
    """Insets (d > 0) or grows (d < 0) a convex polygon given counter-clockwise."""
    n = len(pts)
    lines = []
    for i in range(n):
        (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
        dx, dy = x1 - x0, y1 - y0
        ln = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / ln, dx / ln
        lines.append(((x0 + nx * d, y0 + ny * d), (dx, dy)))
    out = []
    for i in range(n):
        (p, r), (q, s) = lines[i - 1], lines[i]
        cross = r[0] * s[1] - r[1] * s[0]
        if abs(cross) < 1e-9:
            out.append(q)
            continue
        t = ((q[0] - p[0]) * s[1] - (q[1] - p[1]) * s[0]) / cross
        out.append((p[0] + r[0] * t, p[1] + r[1] * t))
    return out


def chamfer_rect(x0, y0, x1, y1, tl, tr, br, bl):
    """A rectangle (y up) with each corner cut at 45 degrees by the given size; counter-clockwise."""
    pts = []
    pts += [(x0 + bl, y0)] if bl else [(x0, y0)]
    pts += [(x1 - br, y0), (x1, y0 + br)] if br else [(x1, y0)]
    pts += [(x1, y1 - tr), (x1 - tr, y1)] if tr else [(x1, y1)]
    pts += [(x0 + tl, y1), (x0, y1 - tl)] if tl else [(x0, y1)]
    if bl:
        pts.append((x0, y0 + bl))
    return pts


class Cell:
    """Draws in a cell's unit square: (u, v) in 0..1, v up, mapped onto a pixel square of side `size` whose
    bottom-left corner is (ox, oy) in Blender pixel space."""

    def __init__(self, ox, oy, size, coll, colour=(1, 1, 1), bevel=6.0, z=0.0, shaded=True):
        self.ox, self.oy, self.s = ox, oy, size
        self.coll = coll
        self.colour = colour
        self.bevel = bevel
        self.z = z
        self.shaded = shaded
        self.count = 0

    def _map(self, pts):
        return [(self.ox + u * self.s, self.oy + v * self.s) for u, v in pts]

    def poly(self, pts, holes=(), colour=None, z=None, bevel=None, shaded=None):
        self.count += 1
        loops = [self._map(pts)] + [self._map(h) for h in holes]
        return _curve_object(f"{self.coll.name}_{self.count}", loops, self.coll, self.z if z is None else z,
                             colour or self.colour, self.bevel if bevel is None else bevel,
                             self.shaded if shaded is None else shaded)

    def circle(self, cx, cy, r, holes=(), **kw):
        return self.poly(circle_pts(cx, cy, r), holes, **kw)

    def ring(self, cx, cy, r0, r1, **kw):
        return self.poly(circle_pts(cx, cy, r1), [circle_pts(cx, cy, r0)], **kw)

    def capsule(self, ax, ay, bx, by, hw, **kw):
        return self.poly(capsule_pts(ax, ay, bx, by, hw), **kw)

    def polyline(self, pts, hw, **kw):
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            self.capsule(ax, ay, bx, by, hw, **kw)

    def arc(self, cx, cy, r, a0, a1, hw, caps=True, **kw):
        self.poly(sector_pts(cx, cy, r - hw, r + hw, a0, a1), **kw)
        if caps:
            for a in (a0, a1):
                self.circle(cx + r * math.cos(a), cy + r * math.sin(a), hw, **kw)


def px_poly(coll, pts, holes=(), colour=(1, 1, 1), z=0.0, bevel=0.0, shaded=False, depth=1.0, name="P"):
    """A polygon given straight in pixel space (y up)."""
    return _curve_object(name, [pts] + list(holes), coll, z, colour, bevel, shaded, depth)


def plane(coll, x0, y0, x1, y1, material, z=0.0, name="Plane"):
    """A quad in pixel space with 0..1 UVs, for gradient and noise materials."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x0, y0, 0), (x1, y0, 0), (x1, y1, 0), (x0, y1, 0)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new(name="UVMap")
    for loop, co in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop.uv = co
    me.materials.append(material)
    ob = bpy.data.objects.new(name, me)
    ob.location.z = z
    coll.objects.link(ob)
    return ob


# ------------------------------------------------------------------------------------------------- compositing

def premul(img):
    out = np.array(img, np.float64)
    out[..., :3] *= out[..., 3:4]
    return out


def unpremul(img):
    out = np.array(img, np.float64)
    a = out[..., 3:4]
    out[..., :3] = np.where(a > 1e-5, out[..., :3] / np.maximum(a, 1e-5), 0.0)
    return out


def over(dst, src):
    """Premultiplied src over premultiplied dst."""
    return src + dst * (1.0 - src[..., 3:4])


def solid(alpha, colour):
    """A premultiplied layer of one colour with the given coverage."""
    a = np.clip(alpha, 0, 1)
    out = np.zeros(a.shape + (4,))
    out[..., 0] = colour[0] * a
    out[..., 1] = colour[1] * a
    out[..., 2] = colour[2] * a
    out[..., 3] = a
    return out


def dilate(a, r):
    """Grey dilation by a disc of radius r (pixels)."""
    out = a.copy()
    ri = int(math.ceil(r))
    for dy in range(-ri, ri + 1):
        for dx in range(-ri, ri + 1):
            if dx * dx + dy * dy <= r * r:
                out = np.maximum(out, np.roll(np.roll(a, dy, 0), dx, 1))
    return out


def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


# ------------------------------------------------------------------------------------------------- packing

def shelf_pack(sizes, width, pad):
    """Places (name, w, h) boxes left to right on shelves; returns {name: (x, y_top, w, h)} and the used height."""
    order = sorted(sizes, key=lambda s: -s[2])
    x = y = pad
    shelf = 0
    out = {}
    for name, w, h in order:
        if x + w + pad > width:
            x = pad
            y += shelf + pad
            shelf = 0
        out[name] = (x, y, w, h)
        x += w + pad
        shelf = max(shelf, h)
    return out, y + shelf + pad


def pow2(n):
    p = 1
    while p < n:
        p *= 2
    return p


# ------------------------------------------------------------------------------------------------- 9-slice (mockups)

def nine_slice(img, border, w, h):
    """Draws a straight-alpha sprite at w x h keeping its borders (l, b, r, t in pixels, Unity order)."""
    l, b, r, t = border
    H, W = img.shape[:2]
    xs = [0, l, W - r, W]
    ys = [0, t, H - b, H]
    dx = [0, l, w - r, w]
    dy = [0, t, h - b, h]
    out = np.zeros((h, w, 4))
    src = premul(img)
    for j in range(3):
        for i in range(3):
            sh, sw = ys[j + 1] - ys[j], xs[i + 1] - xs[i]
            th, tw = dy[j + 1] - dy[j], dx[i + 1] - dx[i]
            if sh <= 0 or sw <= 0 or th <= 0 or tw <= 0:
                continue
            piece = src[ys[j]:ys[j + 1], xs[i]:xs[i + 1]]
            out[dy[j]:dy[j + 1], dx[i]:dx[i + 1]] = _stretch(piece, th, tw)
    return unpremul(out)


def _stretch(piece, h, w):
    sh, sw = piece.shape[:2]
    yi = np.clip(((np.arange(h) + 0.5) * sh / h).astype(int), 0, sh - 1)
    xi = np.clip(((np.arange(w) + 0.5) * sw / w).astype(int), 0, sw - 1)
    return piece[yi][:, xi]


def paste(canvas, img, x, y, tint=(1, 1, 1), alpha=1.0):
    """Composites a straight-alpha sprite (tinted by multiplication) onto a premultiplied canvas at (x, y) top-left."""
    h, w = img.shape[:2]
    H, W = canvas.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0:
        return
    part = np.array(img[y0 - y:y1 - y, x0 - x:x1 - x], np.float64)
    part[..., :3] *= np.asarray(tint)[:3]
    part[..., 3] *= alpha
    canvas[y0:y1, x0:x1] = over(canvas[y0:y1, x0:x1], premul(part))


def scale_img(img, f):
    h, w = img.shape[:2]
    nh, nw = max(1, int(round(h * f))), max(1, int(round(w * f)))
    p = premul(img)
    return unpremul(resize(p, nh, nw))
