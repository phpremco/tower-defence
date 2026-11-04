"""Dr. Voss round 20: the atlas painters. The G-buffer bake (portrait_kit) gives every texel its position, normal, part
id, part params and, for the MPFB parts, the UV into that part's own MPFB texture (`SrcUV`, texture id in z).

- Skin: MPFB's young_caucasian_female texture, simplified for the game's look: the colour is taken from a heavily
  blurred copy (no pores, no scalp stubble, no chin shadow), only a damped trace of the mid-frequency detail stays;
  then painted warmth on cheeks, nose and ears, the lips from the lips group, a soft lid tone, a lash line along the
  upper lid rim, and the brows resampled from MPFB's eyebrow010 cards (alpha) onto the skin.
- Eyes, hair, teeth, boots: their MPFB textures resampled (eyes steel-blue, hair recoloured to her dark warm brown with
  strand variation kept, the bun's red tie navy, boots dark leather).
- Clothes and gear: the round-19 painters (voss2_paint palette and helpers) driven by voss3_cloth's measurements.
"""

import math
import os
import time

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

import common as C
import portrait_kit as pk
import voss2_paint as V2
from portrait_kit import gauss, sstep

lin, mix, line, stitch, hex_mask, star4 = V2.lin, V2.mix, V2.line, V2.stitch, V2.hex_mask, V2.star4

SKIN = lin(0.9, 0.72, 0.62)
SKIN_WARM = lin(0.9, 0.58, 0.52)
SKIN_LID = lin(0.8, 0.6, 0.55)
LIP = lin(0.78, 0.45, 0.45)
MOUTH = lin(0.22, 0.07, 0.08)
LASH = lin(0.08, 0.045, 0.04)
BROW = lin(0.2, 0.11, 0.075)
HAIR = lin(0.21, 0.105, 0.07)
IRIS_TINT = lin(0.42, 0.56, 0.68)
EYE_TEXTURE = "blue_eye.png"


def blender_to_game(b):
    return np.stack([b[..., 0], b[..., 2], -b[..., 1]], -1)


# -------------------------------------------------------------------------------------------------------------
# Textures
# -------------------------------------------------------------------------------------------------------------

class Tex:
    """A source texture in linear RGB(A) with bilinear sampling at UVs."""

    def __init__(self, path, blur=()):
        arr = C.image_to_array(path)
        self.rgb = C.srgb_to_linear(np.clip(arr[..., :3], 0, 1))
        self.alpha = arr[..., 3]
        self.blurred = {s: _gauss_blur(self.rgb, s) for s in blur}
        self.h, self.w = self.rgb.shape[:2]

    def sample(self, uv, arr=None):
        arr = self.rgb if arr is None else arr
        x = np.clip(uv[:, 0] % 1.0 * self.w - 0.5, 0, self.w - 1.001)
        y = np.clip(uv[:, 1] % 1.0 * self.h - 0.5, 0, self.h - 1.001)
        x0, y0 = x.astype(int), y.astype(int)
        fx, fy = (x - x0), (y - y0)
        if arr.ndim == 2:
            arr = arr[..., None]
        a = arr[y0, x0] * (1 - fx)[:, None] + arr[y0, x0 + 1] * fx[:, None]
        b = arr[y0 + 1, x0] * (1 - fx)[:, None] + arr[y0 + 1, x0 + 1] * fx[:, None]
        out = a * (1 - fy)[:, None] + b * fy[:, None]
        return out[:, 0] if out.shape[1] == 1 else out


def _gauss_blur(img, sigma):
    """Separable Gaussian through the FFT (periodic edges are fine: the texture borders are padding)."""
    h, w = img.shape[:2]
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    k = np.exp(-2 * (math.pi * sigma) ** 2 * (fx ** 2 + fy ** 2))
    out = np.empty_like(img)
    for c in range(img.shape[2]):
        out[..., c] = np.real(np.fft.ifft2(np.fft.fft2(img[..., c]) * k))
    return out


def lum(c):
    return c @ np.array((0.2126, 0.7152, 0.0722))


# -------------------------------------------------------------------------------------------------------------
# MPFB parts
# -------------------------------------------------------------------------------------------------------------

class Ctx:
    """What the painters need from the build: landmarks (game space), textures, the brow cards, the lid rims."""

    def __init__(self, build_ctx):
        caps = build_ctx["caps"]
        self.fig = build_ctx["fig"]
        self.tex = {}
        self.paths = {1: caps["body"].texture, 2: os.path.join(os.path.dirname(caps["eyes"].texture), EYE_TEXTURE),
                      3: caps["eyelashes"].texture, 4: caps["hair"].texture, 5: caps["teeth"].texture,
                      6: caps["shoes"].texture}
        b = self.fig.b
        self.eye = {1: b["eye_L"], -1: b["eye_R"]}
        g = blender_to_game(caps["body"].co)
        lips = caps["body"].groups.get("lips", np.zeros(len(g))) > 0.5
        lp = g[lips]
        self.mouth = np.array((0.0, float(np.median(lp[:, 1])), float(lp[:, 2].max())))
        self.mouth_w = float(lp[:, 0].max())
        # Lid margins: the skin that the blink target moves most (the upper lid edge) and where it lands (the lower).
        body = build_ctx["parts"]["body"]
        bg = blender_to_game(body.co)
        d = blender_to_game(body.deltas["Blink"])
        mag = np.linalg.norm(d, axis=1)
        rim = []
        self.upper_rim = []
        for s in (1, -1):
            side = (bg[:, 0] * s > 0) & (np.linalg.norm(bg - self.eye[s], axis=1) < 0.03)
            if not side.any():
                continue
            m = side & (mag > 0.8 * mag[side].max())
            for pts, upper in ((bg[m], True), (bg[m] + d[m], False)):
                # Resample each rim as a dense curve over x, so the painted line is continuous, not dotted.
                # The margin vertices mix the lid's outer and inner rows: per x bin keep the one on the edge
                # (lowest for the upper lid, highest for the lower), then smooth.
                edges = np.linspace(pts[:, 0].min(), pts[:, 0].max(), 13)
                keep = []
                for b0, b1 in zip(edges[:-1], edges[1:]):
                    sel = pts[(pts[:, 0] >= b0) & (pts[:, 0] <= b1)]
                    if len(sel):
                        keep.append(sel[np.argmin(sel[:, 1])] if upper else sel[np.argmax(sel[:, 1])])
                keep = np.array(keep)
                if len(keep) >= 3:
                    keep[1:-1, 1:] = 0.25 * keep[:-2, 1:] + 0.5 * keep[1:-1, 1:] + 0.25 * keep[2:, 1:]
                xs = np.linspace(keep[0, 0], keep[-1, 0], 160)
                dense = np.stack([xs, np.interp(xs, keep[:, 0], keep[:, 1]), np.interp(xs, keep[:, 0], keep[:, 2])], 1)
                rim.append(dense)
                self.upper_rim.append(np.full(len(dense), upper))
        self.rim = np.vstack(rim) if rim else np.zeros((1, 3))
        self.upper_rim = np.concatenate(self.upper_rim) if self.upper_rim else np.zeros(1, bool)
        self.rim_kd = KDTree(len(self.rim))
        for i, p in enumerate(self.rim):
            self.rim_kd.insert(p, i)
        self.rim_kd.balance()
        self.skin_bvh = BVHTree.FromPolygons([tuple(map(float, p)) for p in body.co], body.faces)
        hair = build_ctx["parts"]["hair"]
        self.hair_bvh = BVHTree.FromPolygons([tuple(map(float, p)) for p in hair.co], hair.faces)
        brows = build_ctx["brows"]
        self.brows = brows
        self.brow_bvh = BVHTree.FromPolygons([tuple(map(float, p)) for p in brows.co], brows.faces)
        C.log("paint ctx: mouth", self.mouth.round(3), "rim verts", len(self.rim))

    def texture(self, tid, blur=()):
        if tid not in self.tex:
            self.tex[tid] = Tex(self.paths[tid], blur)
            C.log("texture", self.paths[tid])
        return self.tex[tid]


def skin_paint(o, idx, g, n, prm, src, ctx, blender_p):
    t = ctx.texture(1, blur=(3.0, 26.0))
    fine = t.sample(src, t.blurred[3.0])
    broad = t.sample(src, t.blurred[26.0])
    ratio = np.clip(lum(fine) / np.maximum(lum(broad), 1e-4), 0.975, 1.02)
    shade = np.clip(lum(broad) / lum(SKIN), 0.9, 1.04)
    col = SKIN[None, :] * shade[:, None] * ratio[:, None]
    x, y, z = g[:, 0], g[:, 1], g[:, 2]
    # Warmth where the skin is thin: cheeks, nose, ears, chin.
    warm = np.zeros(len(g))
    for s in (1, -1):
        e = ctx.eye[s]
        warm += 0.55 * gauss(g, e + np.array((s * 0.012, -0.03, 0.0)), (0.017, 0.013, 0.03))
    warm += 0.3 * gauss(g, ctx.mouth + np.array((0, 0.034, 0.006)), (0.008, 0.008, 0.02))
    warm += 0.2 * gauss(g, ctx.mouth + np.array((0, -0.03, -0.006)), (0.014, 0.01, 0.02))
    warm += 0.55 * sstep(0.3, 0.8, prm[:, 1])
    col = mix(col, SKIN_WARM, np.clip(warm, 0, 1) * 0.5)
    # Lids: a soft rosy tone above the eye, a shade in the crease.
    for s in (1, -1):
        e = ctx.eye[s]
        q = g - e
        r = np.linalg.norm(q[:, :2] * np.array((0.8, 1.0)), axis=1)
        lid = sstep(0.006, 0.0, np.abs(q[:, 1] - 0.008)) * sstep(0.02, 0.012, r) * (q[:, 2] > -0.01)
        col = mix(col, SKIN_LID, lid * 0.4)
    # Lips: the lips group, a darker corner and line; the mouth inside dark.
    lips = sstep(0.25, 0.75, prm[:, 0])
    lipcol = mix(np.tile(LIP, (len(g), 1)), LIP * 0.7, sstep(0.55, 1.0, np.abs(x) / max(ctx.mouth_w, 1e-3)) * 0.6)
    col = mix(col, lipcol, lips * 0.6)
    # The mouth inside: texels behind the lips whose forward ray meets the face again (the lips occlude them).
    cand = np.nonzero((np.abs(x) < ctx.mouth_w + 0.004) & (np.abs(y - ctx.mouth[1]) < 0.011) & (z < ctx.mouth[2] - 0.004))[0]
    fwd = Vector((0.0, -1.0, 0.0))
    for k in cand:
        hit = ctx.skin_bvh.ray_cast(Vector(blender_p[k]) + fwd * 0.0015, fwd, 0.06)
        if hit[0] is not None:
            col[k] = MOUTH * (0.7 + 0.3 * sstep(0.03, 0.005, ctx.mouth[2] - z[k]))
    # Brows from the MPFB brow cards and the lash line along the upper rim.
    col = _paint_brows(col, blender_p, ctx)
    col = _hair_shadow(col, blender_p, ctx)
    # MakeHuman's scalp group: where hair grows. Skin there that the hair shell leaves bare (the temples next to the
    # bun style's side cut) is painted as close-combed hair.
    scalp = sstep(0.25, 0.75, prm[:, 2]) * sstep(float(ctx.eye[1][1]) - 0.01, float(ctx.eye[1][1]) + 0.01, y)
    strands = 0.85 + 0.25 * C.value_noise(g * np.array((1.0, 6.0, 1.0)), 300.0, 4)
    col = mix(col, HAIR[None, :] * strands[:, None], scalp * 0.92)
    near = np.linalg.norm(g - ctx.eye[1], axis=1) < 0.028
    near |= np.linalg.norm(g - ctx.eye[-1], axis=1) < 0.028
    for k in np.nonzero(near)[0]:
        _, j, d = ctx.rim_kd.find(g[k])
        s = 1 if x[k] > 0 else -1
        e = ctx.eye[s]
        upper = bool(ctx.upper_rim[j])
        outer = sstep(0.004, 0.015, (x[k] - e[0]) * s)
        w = 0.0021 + 0.0014 * outer if upper else 0.0009
        a = (1 - sstep(w * 0.45, w, d)) * (0.95 if upper else 0.4)
        col[k] = col[k] + (LASH - col[k]) * a
    o.albedo[idx] = col
    o.rough[idx] = 0.62 - 0.15 * lips


def _paint_brows(col, P, ctx):
    """Each skin texel near a brow card takes the card's alpha at the nearest point on it (barycentric UV)."""
    bt = ctx.brows_tex_obj
    co = ctx.brows.co
    centre = co.mean(axis=0)
    near = np.nonzero(np.linalg.norm(P - centre, axis=1) < 0.08)[0]
    faces = ctx.brows.faces
    for k in near:
        hit = ctx.brow_bvh.find_nearest(Vector(P[k]), 0.006)
        if hit[0] is None:
            continue
        loc, _, fi, dist = hit
        f = faces[fi]
        poly = [Vector(co[j]) for j in f]
        w = _poly_weights(poly, loc)
        uv = sum(np.array(ctx.brows.cuv[fi][i]) * w[i] for i in range(len(f)))
        a = float(bt.sample(uv[None], bt.alpha_soft)[0])
        a *= 1 - sstep(0.003, 0.006, dist)
        col[k] = col[k] + (BROW - col[k]) * min(0.92, a * 1.9)
    return col


def _hair_shadow(col, P, ctx):
    """Skin right under the hair's edge takes the hair colour, fading out over 6 mm: the cut edge of the MPFB hair
    shell then reads as a soft hairline instead of a step with bare scalp next to it."""
    near = np.nonzero(P[:, 2] > float(ctx.eye[1][1]) - 0.06)[0]
    for k in near:
        hit = ctx.hair_bvh.find_nearest(Vector(P[k]), 0.012)
        if hit[0] is None:
            continue
        w = 1 - sstep(0.002, 0.012, hit[3])
        col[k] = col[k] + (HAIR * 0.85 - col[k]) * w * 0.9
    return col


def _poly_weights(poly, p):
    from mathutils.interpolate import poly_3d_calc
    return poly_3d_calc(poly, p)


def eye_paint(o, idx, g, n, prm, src, ctx, blender_p):
    t = ctx.texture(2)
    col = t.sample(src)
    l = lum(col)
    sclera = l > 0.45
    tint = IRIS_TINT[None, :] * (l / lum(IRIS_TINT))[:, None]
    col = np.where(sclera[:, None], col * np.array((0.97, 0.96, 0.95)), mix(col, tint, 0.45))
    for s in (1, -1):
        e = ctx.eye[s]
        m = (g[:, 0] * s > 0)
        up = (g[:, 1] - e[1]) / 0.012
        col[m] = col[m] * (1 - 0.45 * sstep(0.15, 0.75, up[m]))[:, None]
    o.albedo[idx] = col
    o.rough[idx] = 0.06


def lash_paint(o, idx, g, n, prm, src, ctx, blender_p):
    o.albedo[idx] = LASH
    o.rough[idx] = 0.7


def hair_paint(o, idx, g, n, prm, src, ctx, blender_p):
    t = ctx.texture(4)
    if 2.0 not in t.blurred:
        t.blurred[2.0] = _gauss_blur(t.rgb, 2.0)
    col = t.sample(src, t.blurred[2.0])
    a = t.sample(src, t.alpha)
    l = lum(col)
    ok = a > 0.35
    mean = float(np.median(l[ok])) if ok.any() else 0.2
    tie = (col[:, 0] > 5.0 * col[:, 1]) & (col[:, 0] > 0.2)
    v = np.where(ok, np.clip(l / mean, 0.55, 1.5), 0.85) ** 0.5
    out = HAIR[None, :] * v[:, None] ** 0.9
    out[tie] = V2.NAVY_DARK
    o.albedo[idx] = out
    o.rough[idx] = 0.52
    o.height[idx] += 0.00025 * (v - 1.0)


def teeth_paint(o, idx, g, n, prm, src, ctx, blender_p):
    t = ctx.texture(5)
    col = t.sample(src)
    depth = sstep(ctx.mouth[2] - 0.01, ctx.mouth[2] - 0.045, g[:, 2])
    o.albedo[idx] = col * (1 - 0.6 * depth)[:, None]
    o.rough[idx] = 0.3


def shoe_paint(o, idx, g, n, prm, src, ctx, blender_p):
    t = ctx.texture(6)
    l = lum(t.sample(src))
    v = np.clip(l / max(float(np.median(l)), 1e-3), 0.6, 1.5) ** 0.5
    col = V2.BOOT[None, :] * v[:, None]
    col = mix(col, V2.SOLE, 1 - sstep(0.014, 0.02, g[:, 1]))
    o.albedo[idx] = col
    o.rough[idx] = 0.45
    o.fam[idx, 1] = 0.5


# -------------------------------------------------------------------------------------------------------------
# Clothes and gear (round-19 painters on the measured figure)
# -------------------------------------------------------------------------------------------------------------

def cloth(o, idx, g, col):
    col = col * (1 + 0.02 * C.value_noise(g, 40.0, 3))[:, None]
    o.albedo[idx] = col
    o.rough[idx] = 0.82
    o.fam[idx, 0] = 1.0


def coat_paint(o, idx, g, n, prm, src, ctx, blender_p):
    fig = ctx.fig
    x, y, z = g[:, 0], g[:, 1], g[:, 2]
    th = np.arctan2(x, z - np.array([fig.coat.cz(v) for v in y]))
    col = np.tile(V2.COAT, (len(g), 1))
    inner = prm[:, 2] > 0.5
    col[inner] *= 0.82
    seams = np.zeros(len(g))
    r = 0.15
    for a0 in (math.radians(100), math.radians(-100), math.pi):
        d = np.angle(np.exp(1j * (th - a0))) * r
        seams = np.maximum(seams, stitch(d + 0.002, y) * 0.8)
        seams = np.maximum(seams, line(d, 0.0012) * 0.6)
    # The colony-blue band along each shoulder ridge (top-facing cloth only), with its seam.
    top = sstep(0.07, 0.085, np.abs(x)) * sstep(0.55, 0.8, n[:, 1]) * (y > fig.collar_y - 0.1)
    dz = z - fig.coat.cz(fig.collar_y - 0.03)
    band = (1 - sstep(0.009, 0.0105, np.abs(dz))) * top
    col = mix(col, V2.COLONY, band)
    seams = np.maximum(seams, line(np.abs(dz) - 0.0118, 0.0008) * top * 0.7)
    front = np.abs(th) < 0.5
    seams = np.maximum(seams, stitch(np.abs(th) * 0.15 - 0.012, y) * front * (y < fig.button_top) * 0.9)
    shadow = line(th * 0.15 + 0.001, 0.0025) * (y < fig.button_top - 0.01) * (y > fig.hem_y + 0.1)
    col = mix(col, V2.COAT * 0.55, shadow * 0.8)
    seams = np.maximum(seams, stitch(y - (fig.hem_y + 0.018), th * 0.15) * 0.8)
    col = mix(col, V2.COAT_SEAM, seams)
    o.height[idx] -= 0.0003 * seams
    drape = np.sin(th * 23.0 + 3.0 * C.value_noise(g, 6.0, 2)) * sstep(fig.waist_y, fig.hem_y + 0.1, y)
    # Soft pull folds from the waist to the bust and from the pockets.
    pull = np.sin(th * 15.0 + y * 40.0) * sstep(fig.waist_y - 0.03, fig.waist_y + 0.04, y) * sstep(fig.bust_y + 0.02, fig.waist_y + 0.06, y)
    o.height[idx] += 0.0007 * drape + 0.0003 * pull + 0.00025 * C.value_noise(g, 70.0, 6)
    cloth(o, idx, g, col)


def sleeve_paint(o, idx, g, n, prm, src, ctx, blender_p):
    fig = ctx.fig
    t = prm[:, 0]
    col = np.tile(V2.COAT, (len(g), 1))
    seams = np.maximum(line(t - 0.07, 0.006) * 0.6, stitch(t - 0.09, prm[:, 1] * 0.3, width=0.004) * 0.6)
    seams = np.maximum(seams, line(t - 0.9, 0.005) * 0.7)
    col = mix(col, V2.COAT_SEAM, seams)
    col = mix(col, V2.COAT * 0.93, sstep(0.9, 0.91, t))
    # The NTI patch on her left sleeve: navy hexagon, gold nova, cyan rim.
    sh, el = fig.b["shoulder_L"], fig.b["elbow_L"]
    pc = sh * 0.55 + el * 0.45 + np.array((0.05, 0.0, 0.0))
    left = g[:, 0] > 0
    q = g - pc
    u, v = q[:, 2], q[:, 1]
    facing = sstep(0.3, 0.6, n[:, 0])
    hx = hex_mask(u, v, 0.019) * facing * left
    rim = (hex_mask(u, v, 0.019) - hex_mask(u, v, 0.016)).clip(0, 1) * facing * left
    col = mix(col, V2.NAVY, hx)
    col = mix(col, V2.CYAN * 0.6, rim)
    st = star4(u, v + 0.0005, 0.011) * facing * left
    col = mix(col, V2.GOLD, st)
    o.emit[idx] += (V2.CYAN[None, :] * rim[:, None] * 0.6) + (V2.GOLD[None, :] * st[:, None] * 0.25)
    o.height[idx] += 0.0003 * hx
    o.height[idx] += 0.0008 * np.sin(t * 90.0 + 2 * np.sin(prm[:, 1] * 6.28)) * sstep(0.38, 0.5, t) * (1 - sstep(0.62, 0.72, t))
    o.height[idx] -= 0.0003 * seams
    cloth(o, idx, g, col)


def lapel_paint(o, idx, g, n, prm, src, ctx, blender_p):
    col = np.tile(V2.COAT * 1.02, (len(g), 1))
    under = prm[:, 2] > 0.5
    col[under] = V2.COAT * 0.85
    edge = stitch(prm[:, 0] - 0.86, prm[:, 1] * 0.2, width=0.03) * (~under)
    col = mix(col, V2.COAT_SEAM, edge * 0.8)
    o.height[idx] -= 0.0002 * edge
    cloth(o, idx, g, col)


def collar_paint(o, idx, g, n, prm, src, ctx, blender_p):
    col = np.tile(V2.COAT, (len(g), 1))
    inner = prm[:, 2] > 0.5
    col[inner] = V2.COAT * 0.82
    edge = stitch(prm[:, 1] - 0.85, prm[:, 0] * 0.3, width=0.04) * (~inner)
    col = mix(col, V2.COAT_SEAM, edge * 0.8)
    cloth(o, idx, g, col)


def pocket_paint(o, idx, g, n, prm, src, ctx, blender_p):
    u, v = prm[:, 0], prm[:, 1]
    col = np.tile(V2.COAT, (len(g), 1))
    e = np.minimum.reduce([u, 1 - u, v * 1.5])
    s = stitch(e - 0.08, g[:, 0] + g[:, 1], width=0.02)
    col = mix(col, V2.COAT_SEAM, s * 0.8)
    col = mix(col, V2.COAT * 0.45, sstep(0.88, 0.98, v))
    o.height[idx] -= 0.0002 * s
    cloth(o, idx, g, col)


def top_paint(o, idx, g, n, prm, src, ctx, blender_p):
    fig = ctx.fig
    a = np.arctan2(g[:, 0], g[:, 2] - np.array([fig.neck.cz(v) for v in g[:, 1]]))
    col = mix(np.tile(V2.NAVY, (len(g), 1)), V2.NAVY_DARK, sstep(-0.2, -0.8, n[:, 1]) * 0.5)
    rib = 0.5 + 0.5 * np.sin(a * 90.0)
    o.height[idx] += 0.00035 * rib
    col = col * (0.92 + 0.12 * rib)[:, None]
    seam = (prm[:, 2] > 0.5) * line(prm[:, 1] - 0.55, 0.22)
    col = mix(col, V2.CYAN * 0.5, seam)
    o.emit[idx] += V2.CYAN[None, :] * seam[:, None] * 0.55
    o.albedo[idx] = col
    o.rough[idx] = 0.7
    o.fam[idx, 0] = 1.0


def trousers_paint(o, idx, g, n, prm, src, ctx, blender_p):
    col = np.tile(V2.SLATE, (len(g), 1))
    a = prm[:, 1] * 2 * math.pi
    crease = line(np.abs(n[:, 2] - 1.0), 0.03) * sstep(0.1, 0.2, prm[:, 0])
    col = mix(col, V2.SLATE * 1.25, crease * 0.5)
    o.height[idx] += 0.0006 * np.sin(prm[:, 0] * 40.0 + a) * sstep(0.75, 0.95, prm[:, 0])
    cloth(o, idx, g, col)
    o.rough[idx] = 0.75


def metal_paint(o, idx, g, n, prm, src, ctx, blender_p):
    o.albedo[idx] = V2.GUNMETAL * (1 + 0.04 * C.value_noise(g, 300.0, 1))[:, None]
    o.rough[idx] = 0.34
    o.fam[idx, 1] = 1.0


def glow_paint(o, idx, g, n, prm, src, ctx, blender_p):
    o.albedo[idx] = V2.CYAN * 0.7
    o.emit[idx] = V2.CYAN
    o.rough[idx] = 0.3


def button_paint(o, idx, g, n, prm, src, ctx, blender_p):
    o.albedo[idx] = V2.BUTTON
    o.rough[idx] = 0.32
    o.fam[idx, 1] = 1.0


def pen_paint(o, idx, g, n, prm, src, ctx, blender_p):
    x0 = ctx.fig.breast[0] + 0.016
    o.albedo[idx] = np.where((g[:, 0] < x0)[:, None], V2.NAVY[None, :], V2.GUNMETAL[None, :] * 0.8)
    o.rough[idx] = 0.3


def card_paint(o, idx, g, n, prm, src, ctx, blender_p):
    V2.card_paint(o, idx, g, n, prm)


PAINTERS = {1: skin_paint, 2: eye_paint, 3: lash_paint, 4: hair_paint, 5: teeth_paint, 6: shoe_paint,
            10: coat_paint, 11: sleeve_paint, 12: top_paint, 13: trousers_paint, 15: collar_paint, 16: lapel_paint,
            17: pocket_paint, 20: metal_paint, 21: card_paint, 22: glow_paint, 23: button_paint, 24: pen_paint}


def paint_maps(obj, size, out_dir, prefix, build_ctx, low_size=None):
    t0 = time.time()
    ctx = Ctx(build_ctx)
    ctx.brows_tex_obj = Tex(build_ctx["caps"]["eyebrows"].texture)
    ctx.brows_tex_obj.alpha_soft = _gauss_blur(ctx.brows_tex_obj.alpha[..., None], 1.5)[..., 0]
    pk.gbuffer_attrs(obj)
    gb, cover = pk.bake_gbuffer(obj, size, ["GPos", "GNrm", "Pid", "Prm", "SrcUV"])
    C.log("gbuffer", round(time.time() - t0, 1))
    P = (gb["GPos"] - 2.0).reshape(-1, 3)
    N = (gb["GNrm"] * 2 - 1).reshape(-1, 3)
    pid = np.rint(gb["Pid"][..., 0]).astype(int).ravel()
    flat = cover.ravel()
    G = blender_to_game(P)
    Ng = blender_to_game(N)
    Ng /= np.maximum(np.linalg.norm(Ng, axis=1, keepdims=True), 1e-9)
    prm = gb["Prm"].reshape(-1, 3)
    src = gb["SrcUV"].reshape(-1, 3)[:, :2]
    o = V2.Out(size * size)
    for k, fn in PAINTERS.items():
        idx = np.nonzero(flat & (pid == k))[0]
        if len(idx):
            fn(o, idx, G[idx], Ng[idx], prm[idx], src[idx], ctx, P[idx])
            C.log("painted", fn.__name__, len(idx), round(time.time() - t0, 1))
    # AO without the headset and badge clip: their contact shadows must not be painted onto the cheek for good.
    co = pk.get_verts(obj)
    vp = np.zeros(len(co))
    obj.data.attributes["Pid"].data.foreach_get("value", vp)
    gear = np.isin(np.rint(vp).astype(int), (20, 22))
    moved = co.copy()
    moved[gear] += np.array((0.0, 0.0, 50.0))
    pk.set_verts(obj, moved)
    ao = pk.bake_ao(obj, size, distance=0.035, samples=40)
    pk.set_verts(obj, co)
    C.log("ao", round(time.time() - t0, 1))
    P2 = P.reshape(size, size, 3)
    du, dv = pk.texel_world(P2, cover)
    albedo = pk.dilate(o.albedo.reshape(size, size, 3), cover)
    normal = pk.height_normal(o.height.reshape(size, size), du, dv)
    emit = pk.dilate(o.emit.reshape(size, size, 3), cover)
    rough = o.rough.reshape(size, size)
    fam = o.fam.reshape(size, size, 2)
    skinlike = np.isin(pid, (1, 2, 3)).reshape(size, size)
    ao_floor = np.where(skinlike, 0.6, 0.3)
    mask = np.dstack([np.clip(ao_floor + (1 - ao_floor) * ao, 0, 1), 1 - rough, fam[..., 0], fam[..., 1]])
    mask = pk.dilate(mask, cover)
    os.makedirs(out_dir, exist_ok=True)
    maps = {"Albedo": (albedo, True), "Normal": (normal, False), "Emission": (emit, True), "Mask": (mask, False)}
    paths = {}
    for key, (arr, srgb) in maps.items():
        paths[key.lower()] = pk.save_png(arr, os.path.join(out_dir, f"{prefix}_{key}.png"), srgb)
    if low_size:
        low_dir = os.path.join(out_dir, "Low")
        os.makedirs(low_dir, exist_ok=True)
        f = size // low_size
        for key, (arr, srgb) in maps.items():
            pk.save_png(pk.downsample(arr, f, normal=key == "Normal"), os.path.join(low_dir, f"{prefix}_{key}.png"), srgb)
    C.log("maps", round(time.time() - t0, 1))
    return paths
