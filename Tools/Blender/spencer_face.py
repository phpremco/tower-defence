"""Spencer pass 4: the face behind the visor (Cole Spencer, a rugged field scout in his late thirties), built on MPFB 2
like Dr. Voss (voss3_human.py). REQUIRES the MPFB extension and the CC0 pack makehuman_system_assets (install:
Docs/COMMANDS.md, "Blender: MPFB 2"); faceunits01 gives the Blink shape key.

Pipeline:
1. capture(): voss3_human's capture with a male phenotype, a squarer jaw, heavier brow and a broken nose bridge;
   skin `middleage_caucasian_male`, high-poly eyes, eyebrows (painted in, not exported), eyelashes (a painted lash
   line), no hair (he wears the helmet). Cached as .npz so later runs need no MPFB.
2. build(): only what the visor shows: the front of the head (forehead to chin, cut in front of the ears) and the two
   eyeballs, scaled and placed so the eyes sit behind the visor glass. One mesh `Mercer_Face`, rigid on the Head
   bone, its own 1024 atlas (skin left, eye top right): MPFB's skin and eye textures resampled by a Cycles bake,
   then painted: brows from the brow cards, a lash line, stubble, a scar over the left brow, weathered skin, a
   catchlight in the iris. Maps Albedo / Normal / Emission / Mask in the hero's channel layout (Mask R = AO,
   G = smoothness, B and A = 0: no detail families), plus Low (512) copies.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

import common as C
import portrait_kit as pk
import voss3_human as VH

NAME = "Mercer_Face"
TEX, LOW_TEX = 1024, 512

PHENOTYPE = dict(gender=1.0, age=0.6, muscle=0.72, weight=0.55, proportions=0.6, height=0.6, cupsize=0.5, firmness=0.5)
RACE = {"asian": 0.12, "caucasian": 0.78, "african": 0.1}
# Rugged, not a model: a square jaw and chin, cheek bones, a low heavy brow angled down (stern at rest), a nose with
# a hump (broken once), slightly narrower eyes.
TARGETS = [
    ("head-square", 0.45), ("chin-width-incr", 0.3), ("chin-prominent-incr", 0.22), ("chin-bones-incr", 0.3),
    ("l-cheek-bones-incr", 0.32), ("r-cheek-bones-incr", 0.32), ("l-cheek-volume-decr", 0.2), ("r-cheek-volume-decr", 0.2),
    ("eyebrows-trans-down", 0.3), ("eyebrows-angle-down", 0.25), ("eyebrows-trans-forward", 0.25),
    ("nose-hump-incr", 0.35), ("nose-scale-horiz-incr", 0.08), ("nose-point-width-incr", 0.1),
    ("mouth-scale-horiz-incr", 0.05), ("mouth-upperlip-volume-decr", 0.2),
]
ASSETS = {"eyes": "high-poly/high-poly.mhclo", "eyebrows": "eyebrow001/eyebrow001.mhclo",
          "eyelashes": "eyelashes01/eyelashes01.mhclo"}
SKIN = "middleage_caucasian_male/middleage_caucasian_male.mhmat"
EYE_COLOUR = "bluegreen_eye.png"
EXPRESSIONS = {"Blink": {"eyeBlinkLeft": 1.0, "eyeBlinkRight": 1.0}}


def capture(cache):
    """{key: Captured}, info from MPFB (or the cache)."""
    if cache and os.path.exists(cache):
        C.log("face capture from cache", cache)
        return VH.load_cache(cache)
    VH.PHENOTYPE = PHENOTYPE
    VH.RACE = RACE
    VH.TARGETS = TARGETS
    VH.ASSETS = ASSETS
    VH.SKIN = SKIN
    VH.EXPRESSIONS = EXPRESSIONS
    caps, info = VH.capture()
    if cache:
        os.makedirs(os.path.dirname(cache), exist_ok=True)
        VH.save_cache(cache, caps, info)
    return caps, info


def mpfb_data(*parts):
    return os.path.join(os.environ.get("APPDATA", ""), "Blender Foundation", "Blender", "5.2", "extensions", ".user",
                        "blender_org", "mpfb", "data", *parts)


# ------------------------------------------------------------------------------------------------------------
# Mesh: the visible front of the head and the eyeballs
# ------------------------------------------------------------------------------------------------------------

class Piece:
    def __init__(self, co, faces, cuv, blink=None):
        self.co = np.asarray(co, float)
        self.faces = [tuple(f) for f in faces]
        self.cuv = cuv
        self.blink = blink if blink is not None else np.zeros_like(self.co)

    def subset(self, keep_v):
        keep_v = np.asarray(keep_v, bool)
        fi = [i for i, f in enumerate(self.faces) if all(keep_v[j] for j in f)]
        used = np.zeros(len(self.co), bool)
        for i in fi:
            used[list(self.faces[i])] = True
        remap = -np.ones(len(self.co), int)
        remap[used] = np.arange(used.sum())
        return Piece(self.co[used], [tuple(remap[j] for j in self.faces[i]) for i in fi], [self.cuv[i] for i in fi],
                     self.blink[used])


def piece_of(cap):
    return Piece(cap.co, cap.faces(), cap.corner_uv(), cap.deltas.get("Blink"))


def components(faces, n):
    parent = np.arange(n)

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    for f in faces:
        r = find(f[0])
        for v in f[1:]:
            rv = find(v)
            if rv != r:
                parent[rv] = r
    return np.array([find(i) for i in range(n)])


def landmarks(caps, info):
    b = info["bones"]
    eye_l, eye_r = np.asarray(b["eye.L"], float), np.asarray(b["eye.R"], float)
    return {"eye_l": eye_l, "eye_r": eye_r, "eye_c": 0.5 * (eye_l + eye_r)}


def face_piece(caps, info, back=0.045, top=0.075, bottom=0.135, side=0.072):
    """The skin in front of a plane `back` behind the eye centres, from `top` above to `bottom` below them and within
    `side` of the mid-line; ears and the scalp go."""
    body = piece_of(caps["body"])
    lm = landmarks(caps, info)
    e = lm["eye_c"]
    q = body.co - e
    groups = caps["body"].groups
    ears = groups.get("ears", np.zeros(len(body.co))) > 0.3
    head = (caps["body"].weights["Head"] + caps["body"].weights["Jaw"]) > 0.3
    keep = (q[:, 1] < back) & (q[:, 2] < top) & (q[:, 2] > -bottom) & (np.abs(q[:, 0]) < side) & ~ears & head
    return body.subset(keep), lm


def eye_pieces(caps):
    """The two eyeballs (the cornea shells are dropped, as for Voss: the game shader is opaque)."""
    eyes = piece_of(caps["eyes"])
    comp = components(eyes.faces, len(eyes.co))
    keep = np.zeros(len(eyes.co), bool)
    for s in (1, -1):
        cs = [c for c in np.unique(comp) if np.mean(eyes.co[comp == c][:, 0]) * s > 0]
        fwd = {c: -eyes.co[comp == c][:, 1].min() for c in cs}
        keep |= comp == min(cs, key=lambda c: fwd[c])
    return eyes.subset(keep)


def place(pieces, lm, eye_target, scale):
    """Scales every piece about the eye centre by `scale` and moves the eye centre to `eye_target` (Blender space)."""
    e = lm["eye_c"]
    t = np.asarray(eye_target, float)
    for p in pieces:
        p.co = (p.co - e) * scale + t
        p.blink = p.blink * scale
    out = {k: (np.asarray(v) - e) * scale + t for k, v in lm.items()}
    return out


# ------------------------------------------------------------------------------------------------------------
# UVs: skin on the left 0.78 of the atlas (MPFB's own layout, rescaled), the eye island top right
# ------------------------------------------------------------------------------------------------------------

SKIN_BOX = (0.01, 0.01, 0.8, 0.99)
EYE_BOX = (0.82, 0.8, 0.99, 0.97)
FOLD_BOX = (0.84, 0.03, 0.9, 0.09)
FOLD = (0.16, 0.07, 0.06)            # linear: deep skin shadow


def planar_uv(piece, box, centre=None):
    """Front projection (Blender x, z) into `box`, aspect kept: only the front of the face shows behind the visor, so a
    planar map has no seams and no stretch where it matters. `centre` (x, z) centres the box on a point (the eyes)."""
    pts = piece.co[:, [0, 2]]
    lo, hi = pts.min(axis=0), pts.max(axis=0)
    if centre is not None:
        half = np.maximum(np.abs(lo - centre), np.abs(hi - centre))
        lo, hi = np.asarray(centre) - half, np.asarray(centre) + half
    span = hi - lo
    s = min((box[2] - box[0]) / span[0], (box[3] - box[1]) / span[1])
    off = np.array(box[:2]) + (np.array([box[2] - box[0], box[3] - box[1]]) - span * s) / 2
    uv = (pts - lo) * s + off
    return [[tuple(uv[j]) for j in f] for f in piece.faces]


def mesh_from(pieces, name):
    """One Blender mesh from (Piece, atlas uv, source uv, texture id) parts; UV maps `UVMap` (atlas) and `Src`.
    Returns the object and the per-vertex Blink deltas."""
    co, faces, uv, src, tid, blink = [], [], [], [], [], []
    k = 0
    for p, auv, suv, t in pieces:
        co.append(p.co)
        blink.append(p.blink)
        for f, a, s in zip(p.faces, auv, suv):
            faces.append(tuple(j + k for j in f))
            uv.extend(a)
            src.extend(s)
            tid.append(t)
        k += len(p.co)
    co = np.concatenate(co)
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in co], [], faces)
    me.update()
    for nm, data in (("UVMap", uv), ("Src", src)):
        layer = me.uv_layers.new(name=nm)
        layer.data.foreach_set("uv", np.asarray(data, np.float32).ravel())
    me.uv_layers.active = me.uv_layers["UVMap"]
    me.uv_layers["UVMap"].active_render = True
    obj = C.link(bpy.data.objects.new(name, me))
    for poly, t in zip(me.polygons, tid):
        poly.material_index = t
    return obj, np.concatenate(blink)


# ------------------------------------------------------------------------------------------------------------
# Textures
# ------------------------------------------------------------------------------------------------------------

def _src_material(name, image_path):
    mat = C.new_material(name)
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = "Src"
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(image_path, check_existing=True)
    tex.interpolation = "Cubic"
    nt.links.new(uvn.outputs["UV"], tex.inputs["Vector"])
    nt.links.new(tex.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return mat


def _bake_one_source(obj, image_path, size):
    scene = bpy.context.scene
    img = bpy.data.images.new("FaceSrc", size, size, alpha=False, float_buffer=True)
    img.colorspace_settings.name = "Linear Rec.709"
    mat = _src_material("FaceSrcMat", image_path)
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = img
    mat.node_tree.nodes.active = node
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.material_index = 0
    others = [(o, o.hide_render) for o in scene.objects if o != obj]
    for o, _ in others:
        o.hide_render = True
    C.activate(obj)
    bpy.ops.object.bake(type="EMIT")
    for o, h in others:
        o.hide_render = h
    arr = np.array(img.pixels[:], np.float32).reshape(size, size, 4)[..., :3].copy()
    bpy.data.images.remove(img)
    obj.data.materials.clear()
    bpy.data.materials.remove(mat)
    return arr


def bake_sources(obj, skin_path, eye_path, size, eye_mask):
    """MPFB's skin and eye textures resampled into the atlas (linear colour, (size, size, 3)): each texture is baked
    over the whole mesh through the `Src` UVs and the eye texels are taken from the eye bake."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 4
    bake = scene.render.bake
    bake.use_selected_to_active = False
    bake.target = "IMAGE_TEXTURES"
    bake.margin = 8
    bake.margin_type = "EXTEND"
    bake.use_clear = True
    skin = _bake_one_source(obj, skin_path, size)
    eye = _bake_one_source(obj, eye_path, size)
    return np.where(eye_mask[..., None], eye, skin)


def gbuffer(obj, size, extra):
    """Position, normal and the part id per texel (pk.bake_gbuffer), plus the coverage mask."""
    me = obj.data
    n = len(me.vertices)
    co = np.zeros(n * 3)
    me.vertices.foreach_get("co", co)
    co = co.reshape(n, 3)
    lo = co.min(axis=0) - 0.01
    span = (co.max(axis=0) + 0.01 - lo).max()
    nor = np.zeros(n * 3)
    me.vertices.foreach_get("normal", nor)
    nor = nor.reshape(n, 3)
    pk.set_attr(obj, "GP", (co - lo) / span, kind="FLOAT_VECTOR")
    pk.set_attr(obj, "GN", nor * 0.5 + 0.5, kind="FLOAT_VECTOR")
    pk.set_attr(obj, "GX", extra, kind="FLOAT_VECTOR")
    maps, cover = pk.bake_gbuffer(obj, size, ["GP", "GN", "GX"])
    P = maps["GP"] * span + lo
    N = maps["GN"] * 2 - 1
    return P, N, maps["GX"], cover


def lin(c):
    return C.srgb_to_linear(np.asarray(c, float))


BROW = lin((0.2, 0.14, 0.1))
STUBBLE = lin((0.2, 0.15, 0.12))
SCAR = lin((0.78, 0.55, 0.5))
FACE_GLOW = 0.26


def paint(src, P, N, X, cover, lm, brows, lashes):
    """Overlays on the resampled textures; X[..., 0] = 1 on the eyes. Returns albedo, smoothness, AO-ish, emission."""
    h, w = cover.shape
    col = src.copy()
    eye = X[..., 0] > 0.5
    skin = cover & ~eye
    idx = np.nonzero(skin.ravel())[0]
    p = P.reshape(-1, 3)[idx]
    c = col.reshape(-1, 3)[idx]
    e = lm["eye_c"]
    q = p - e                                   # Blender: x left, -y forward, z up
    # A tan and less pink (a field scout, wind and sun), slightly more contrast.
    lum = c.mean(axis=1, keepdims=True)
    c = c * np.array((1.0, 0.93, 0.86)) * 0.95
    c = lum + (c - lum) * 0.9
    # Stubble: jaw, chin and upper lip; fine speckle over a soft shadow, fading up the cheeks.
    jaw = C.smoothstep(-0.035, -0.06, q[:, 2]) * (1 - C.smoothstep(0.055, 0.07, np.abs(q[:, 0])))
    speck = C.smoothstep(0.45, 0.75, C.value_noise(p, 2600.0, 5))
    shadow = jaw * (0.45 + 0.55 * speck)
    c = C.lerp3(c, STUBBLE, (shadow * 0.55)[:, None])
    # Scar: a pale line through his left brow down onto the cheek bone (Blender +x is his left).
    a = np.array((e[0] + 0.03 * 1.0, e[1], e[2] + 0.03))
    b = np.array((e[0] + 0.045, e[1], e[2] - 0.028))
    ab = b - a
    t = np.clip(((p - a) @ ab) / (ab @ ab), 0, 1)
    d = np.linalg.norm(p - (a + t[:, None] * ab), axis=1)
    scar = (1 - C.smoothstep(0.0009, 0.0018, d)) * C.smoothstep(0.0, 0.15, t) * (1 - C.smoothstep(0.85, 1.0, t))
    c = C.lerp3(c, SCAR, (scar * 0.75)[:, None])
    # Weathering: a few freckles / sun spots and darker crease shading around the eyes.
    spots = C.smoothstep(0.78, 0.86, C.value_noise(p, 700.0, 9))
    c = c * (1 - 0.12 * spots)[:, None]
    # Brows from the brow cards (nearest card point within 5 mm), and the lash line from the upper lash roots.
    c = _near_mesh(c, p, brows, 0.0038, BROW, 0.6 + 0.2 * C.value_noise(p, 500.0, 31))
    c = _near_mesh(c, p, lashes, 0.0018, lin((0.06, 0.045, 0.04)), 0.85)
    flat = col.reshape(-1, 3)
    flat[idx] = c
    col = flat.reshape(h, w, 3)
    # Smoothness: skin 0.38, oilier on the nose and forehead, eyes 0.88; AO from MPFB is baked in the skin already.
    smooth = np.zeros((h, w), np.float32)
    sm = 0.36 + 0.1 * C.smoothstep(0.03, 0.0, np.abs(q[:, 0])) * C.smoothstep(-0.03, 0.0, q[:, 2])
    sflat = smooth.ravel()
    sflat[idx] = sm
    sflat[np.nonzero(eye.ravel())[0]] = 0.88
    smooth = sflat.reshape(h, w)
    # The closed helmet shades the face from every key light: a faint self-light (the visor HUD's glow, cool from below)
    # keeps the face readable behind the tinted glass in a level and in the bust.
    emit = (col * FACE_GLOW * np.array((0.95, 1.0, 1.05))).astype(np.float32)
    col, emit = catchlight(col, emit, X, cover)
    return col, smooth, emit, scar


def catchlight(col, emit, X, cover):
    """A small window catchlight on each iris (both eyes share the eye island): upper left of the pupil."""
    eye = (X[..., 0] > 0.5) & cover
    ys, xs = np.nonzero(eye)
    if not len(xs):
        return col, emit
    h, w = eye.shape
    cx, cy = xs.mean(), ys.mean()
    r = max(xs.max() - xs.min(), ys.max() - ys.min()) / 2.0
    yy, xx = np.mgrid[0:h, 0:w]
    spot = np.exp(-(((xx - (cx - 0.075 * r)) ** 2 + (yy - (cy + 0.09 * r)) ** 2) / (2 * (0.026 * r) ** 2)))
    spot = spot * eye
    col = col + (np.ones(3) * 0.95 - col) * np.clip(spot * 1.4, 0, 1)[..., None]
    emit = np.maximum(emit, (spot * 0.55)[..., None] * np.ones(3))
    return col, emit


def _near_mesh(c, p, piece, radius, colour, strength):
    if piece is None or not len(piece.faces):
        return c
    tree = BVHTree.FromPolygons([tuple(map(float, v)) for v in piece.co], piece.faces)
    lo, hi = piece.co.min(axis=0) - radius, piece.co.max(axis=0) + radius
    near = np.nonzero(np.all((p >= lo) & (p <= hi), axis=1))[0]
    strength = np.broadcast_to(np.asarray(strength, float), (len(p),))
    for k in near:
        hit = tree.find_nearest(Vector(p[k]), radius)
        if hit[0] is None:
            continue
        w = (1 - C.smoothstep(radius * 0.35, radius, hit[3])) * strength[k]
        c[k] = c[k] + (colour - c[k]) * w
    return c


def normal_map(P, cover, size, scar):
    """Pores and fine wrinkles as a tangent-ish normal from a height field in texture space."""
    h = (C.value_noise(P.reshape(-1, 3), 3200.0, 21) * 0.6 + C.value_noise(P.reshape(-1, 3), 900.0, 22) * 0.4).reshape(size, size)
    h = h * cover
    texel = 0.25 / size
    return pk.height_to_normal(h * 0.00012, cover, texel, strength=1.0)


def save(arr, path, srgb):
    pk.save_png(np.clip(arr, 0, 1), path, srgb)


def textures(obj, out_dir, lm, brows, lashes, eye_flag, low_dir=None):
    skin_tex = mpfb_data("skins", "middleage_caucasian_male", "middleage_lightskinned_male_diffuse.png")
    eye_tex = mpfb_data("eyes", "materials", EYE_COLOUR)
    P, N, X, cover = gbuffer(obj, TEX, eye_flag)
    src = bake_sources(obj, skin_tex, eye_tex, TEX, X[..., 0] > 0.5)
    col, smooth, emit, scar = paint(src, P, N, X, cover, lm, brows, lashes)
    fy0, fy1 = int(FOLD_BOX[1] * TEX) - 4, int(FOLD_BOX[3] * TEX) + 4
    fx0, fx1 = int(FOLD_BOX[0] * TEX) - 4, int(FOLD_BOX[2] * TEX) + 4
    col[fy0:fy1, fx0:fx1] = FOLD
    emit[fy0:fy1, fx0:fx1] = 0.0
    col = pk.dilate(col, cover, 8)
    nmap = normal_map(P, cover, TEX, scar)
    ao = np.ones((TEX, TEX), np.float32)
    mask = np.dstack([ao, pk.dilate(smooth[..., None], cover, 8)[..., 0], np.zeros_like(ao), np.zeros_like(ao)])
    os.makedirs(out_dir, exist_ok=True)
    paths = {}
    for key, arr, srgb in (("Albedo", col, True), ("Normal", nmap, False), ("Emission", emit, True), ("Mask", mask, False)):
        path = os.path.join(out_dir, f"{NAME}_{key}.png")
        pk.save_png(np.clip(arr, 0, 1), path, srgb)
        paths[key] = path
        if low_dir:
            os.makedirs(low_dir, exist_ok=True)
            pk.save_png(np.clip(pk.downsample(arr, TEX // LOW_TEX, normal=key == "Normal"), 0, 1),
                        os.path.join(low_dir, f"{NAME}_{key}.png"), srgb)
    return paths
