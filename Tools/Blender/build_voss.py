"""Dr. Elena Voss, chief scientist: a bust for dialogue portraits and cutscene close-ups (round 13, rebuilt in
round 16; see ArtSource/DESIGN_NOTES.md).

Round 16 builds her from separate shells (suit.build_layered_asset) instead of one fused blob: the head and neck,
the eyes, the eyelids, the hair (a cap under sculpted locks, a side-swept fringe and a twisted bun), the navy utility
top, the field-lab coat (panels, yoke, sleeves, a rolled collar, notched lapels, a breast pocket) and the gear
(headset, ID card, pens, buttons). Each layer has its own remesh resolution and its own share of the triangle budget,
so the face and the eyelids get dense geometry and the back of the coat little. The head is one sculpted surface
(eye sockets, cheekbones, a tapered jaw, a small chin) with a real mouth slit, so the Talk jaw parts the lips instead
of stretching the lower face.

Rig (10 bones, unchanged): Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R. The upper eyelids are
shells concentric with the eyes on the lid bones, so a blink slides them over the eyeballs. Clips: Idle, Talk, Blink.
No LOD1 (portrait and cutscene only).

Out: `Exports/Voss/Voss.fbx`, maps, `Voss_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_voss.py -- [--preview <dir>] [--norender]
     [--fast] (styled source only, no bake or export: shape checks) [--stage <dir>] (exports there instead)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import suit as S  # noqa: E402
import tower_kit as tk  # noqa: E402
from common import G  # noqa: E402

NAME = "Voss"
GAME_TRIS = 10000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("weave", "ceramic")   # Mask B = weave (coat and undersuit), A = ceramic (the ID card)
XAX = Vector((1, 0, 0))
UPV = Vector((0, 0, 1))

# Colours (sRGB).
SKIN = np.array((0.88, 0.69, 0.585))
SKIN_WARM = np.array((0.86, 0.55, 0.5))      # ears, nose tip, cheeks: where light scatters red through skin
SKIN_SHADE = np.array((0.7, 0.5, 0.46))
LIP = np.array((0.72, 0.43, 0.42))
MOUTH = np.array((0.2, 0.07, 0.08))
HAIR = np.array((0.2, 0.1, 0.075))
HAIR_SHEEN = np.array((0.46, 0.28, 0.2))
HAIR_DEEP = np.array((0.09, 0.045, 0.035))
BROW = np.array((0.16, 0.085, 0.065))
LASH = np.array((0.06, 0.035, 0.035))
SCLERA = np.array((0.92, 0.91, 0.89))
IRIS = np.array((0.3, 0.5, 0.64))
IRIS_RIM = np.array((0.12, 0.2, 0.3))
PUPIL = np.array((0.025, 0.025, 0.03))
COAT = np.array((0.9, 0.9, 0.9))
COAT_SHADE = np.array((0.6, 0.63, 0.69))
COAT_SEAM = np.array((0.68, 0.7, 0.74))
NAVY = np.array((0.12, 0.19, 0.36))
NAVY_DARK = np.array((0.07, 0.11, 0.21))
COLONY = np.array((0.22, 0.38, 0.66))
YELLOW = np.array((0.98, 0.74, 0.14))
CYAN = np.array((0.32, 0.93, 1.0))
NOVA = np.array((1.0, 0.9, 0.62))
GUNMETAL = np.array((0.22, 0.24, 0.28))

# Round 16: the head sits 3 cm lower than in round 13 (a shorter neck: the "bobblehead" fix).
DROP = 0.03
HEAD_C = G(0, 1.468 - DROP, -0.006)
HEAD_R = (0.0735, 0.104, 0.091)              # across, up, forward
EYE_R = 0.0142
EYES = {"L": G(0.0305, 1.463 - DROP, 0.052), "R": G(-0.0305, 1.463 - DROP, 0.052)}
LID_GAP = 0.0007                            # between the eyeball and the inside of a lid
LID_THICK = 0.0026
LID_EDGE = math.radians(22)                 # the upper lid's edge sits this far above the eye's centre line
LOWER_EDGE = math.radians(-27)
BLINK = math.radians(52)                    # lid rotation that closes the eye onto the lower lid
MOUTH_Z = 1.402 - DROP
MOUTH_HALF = 0.0175                         # half the mouth's width
PART_X = 0.022                              # the hair parting, on her left


def R(x, up, fwd):
    return (x, fwd, up)


def fam(weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0):
    return np.array((weave, ceramic, metal, rubber))


def style(color_fn, rough=0.5, family=fam(), disp_fn=None, emit_fn=None, grad=(1.04, 1.0, 0.82)):
    """A free style: colour_fn(ctx) -> (n, 3) sRGB, a soft top-light value ramp, optional displacement (normal map
    only) and glow. Flow is 0: no stitch rows."""
    def fn(s, idx, part):
        ctx = S.Ctx(s, idx, part)
        col = np.asarray(color_fn(ctx), dtype=float)
        if col.ndim == 1:
            col = np.broadcast_to(col, (len(idx), 3)).copy()
        ramp = C.chitin_gradient(ctx.n, np.full(3, grad[0]), np.full(3, grad[1]), np.full(3, grad[2]))
        s.albedo[idx] = np.clip(col * ramp, 0, 1)
        s.disp[idx] = disp_fn(ctx) if disp_fn else 0.0
        s.rough[idx] = rough(ctx) if callable(rough) else rough
        s.emit[idx] = emit_fn(ctx) if emit_fn else 0.0
        s.fam[idx] = family
        s.flow[idx] = 0.0
    return fn


def mix(a, b, m):
    return C.lerp3(np.broadcast_to(a, (len(m), 3)), np.broadcast_to(b, (len(m), 3)), np.clip(m, 0, 1))


def near(ctx, point, radius, soft=0.5):
    d = np.linalg.norm(ctx.p - np.asarray(point), axis=1)
    return 1 - C.smoothstep(radius * (1 - soft), radius, d)


def ss(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# -------------------------------------------------------------------------------------------------------------
# Styles
# -------------------------------------------------------------------------------------------------------------

def skin_color(ctx):
    p = ctx.p
    col = np.broadcast_to(SKIN, (len(p), 3)).copy()
    # Warmth where skin is thin or blood-rich: cheeks, nose, ears, the eyelid creases. Painted, not lit: a matte
    # stand-in for subsurface scattering.
    for sx in (1, -1):
        col = mix(col, SKIN_WARM, near(ctx, G(0.04 * sx, 1.437 - DROP, 0.06), 0.03, 0.95) * 0.32)
        col = mix(col, SKIN_WARM, near(ctx, G(0.071 * sx, 1.458 - DROP, -0.008), 0.03, 0.6) * 0.45)
    col = mix(col, SKIN_WARM, near(ctx, G(0, 1.434 - DROP, 0.086), 0.014, 0.8) * 0.3)
    # Soft shade under the jaw and in the eye sockets, so the face reads without a strong light.
    col = mix(col, SKIN_SHADE, C.smoothstep(-0.25, -0.85, ctx.n[:, 2]) * 0.4)
    for sx in (1, -1):
        e = np.asarray(EYES["L" if sx > 0 else "R"])
        col = mix(col, SKIN_SHADE * 0.95, near(ctx, e + np.array((0.0, -0.004, 0.006)), 0.024, 0.8) * 0.28)
    # The crease between the lips is dark: it opens into the mouth when the jaw drops.
    inside = C.smoothstep(0.0016, 0.0, np.abs(p[:, 2] - MOUTH_Z)) * (1 - C.smoothstep(MOUTH_HALF * 0.75, MOUTH_HALF, np.abs(p[:, 0])))
    return mix(col, MOUTH, inside * C.smoothstep(0.03, 0.06, -p[:, 1]))


def skin_disp(ctx):
    # A whisper of skin texture (no pores: stylised), only so highlights break up a little.
    return 0.00012 * C.value_noise(ctx.p, 300.0, 21)


SKIN_STYLE = style(skin_color, rough=0.66, disp_fn=skin_disp, grad=(1.03, 1.0, 0.86))


def lip_color(ctx):
    col = mix(SKIN, LIP, np.ones(len(ctx.p)) * 0.8)
    # The lips' inner faces (towards the slit, not the viewer) darken into the mouth.
    inner = C.smoothstep(0.55, 0.0, -ctx.n[:, 1]) * C.smoothstep(0.0055, 0.0015, np.abs(ctx.p[:, 2] - MOUTH_Z))
    col = mix(col, MOUTH * 1.4, inner)
    corner = C.smoothstep(MOUTH_HALF * 0.55, MOUTH_HALF * 1.1, np.abs(ctx.p[:, 0]))
    col = mix(col, SKIN, corner * 0.6)
    return mix(col, MOUTH, C.smoothstep(0.0022, 0.0, np.abs(ctx.p[:, 2] - MOUTH_Z)) * 0.7)


LIP_STYLE = style(lip_color, rough=0.45, grad=(1.04, 1.0, 0.85))


def eye_style(centre):
    c = np.asarray(centre)

    def color(ctx):
        d = ctx.p - c
        d = d / np.maximum(np.linalg.norm(d, axis=1, keepdims=True), 1e-9)
        ang = np.arccos(np.clip(-d[:, 1], -1, 1))
        col = mix(SCLERA, SCLERA * np.array((0.95, 0.88, 0.86)), C.smoothstep(0.7, 1.2, ang))
        col = mix(col, IRIS_RIM, 1 - C.smoothstep(0.5, 0.56, ang))
        col = mix(col, IRIS, 1 - C.smoothstep(0.38, 0.47, ang))
        col = mix(col, IRIS * 1.4, (1 - C.smoothstep(0.1, 0.42, ang)) * C.smoothstep(0.0, -0.6, d[:, 2]) * 0.55)
        col = mix(col, PUPIL, 1 - C.smoothstep(0.16, 0.2, ang))
        # The upper lid shades the top of the eye.
        return mix(col, col * 0.5, C.smoothstep(0.15, 0.45, d[:, 2]))
    return style(color, rough=0.06, grad=(1.0, 1.0, 0.9))


def lid_color(centre, upper):
    c = np.asarray(centre)

    def color(ctx):
        d = ctx.p - c
        r = np.maximum(np.linalg.norm(d, axis=1), 1e-9)
        el = np.arcsin(np.clip(d[:, 2] / r, -1, 1))
        col = np.broadcast_to(SKIN * 0.97, (len(ctx.p), 3)).copy()
        if upper:
            # A dark lash line along the rim (it also caps the lid's thickness), a little warmth on the lid.
            lash = 1 - C.smoothstep(LID_EDGE + 0.05, LID_EDGE + 0.17, el)
            col = mix(col, SKIN_WARM, C.smoothstep(LID_EDGE + 0.3, LID_EDGE + 0.8, el) * 0.25)
            # The outer corner's lashes sweep a little further: the wing that makes the eye read at a distance.
            wing = C.smoothstep(0.35, 0.8, np.abs(d[:, 0]) / EYE_R) * (1 - C.smoothstep(LID_EDGE + 0.1, LID_EDGE + 0.35, el))
            return mix(col, LASH, np.maximum(lash, wing))
        lash = C.smoothstep(LOWER_EDGE - 0.12, LOWER_EDGE - 0.02, el) * 0.5
        return mix(col, LASH, lash)
    return color


def hair_color(ctx):
    sheen = C.smoothstep(0.3, 0.6, ctx.n[:, 2]) * (1 - C.smoothstep(0.75, 0.95, ctx.n[:, 2]))
    var = 0.85 + 0.3 * C.value_noise(ctx.p, 70.0, 5)[:, None]
    col = mix(HAIR * var, HAIR_SHEEN, sheen * 0.45)
    # Dark in the crevices between locks and underneath.
    return mix(col, HAIR_DEEP, C.smoothstep(-0.1, -0.7, ctx.n[:, 2]) * 0.5)


def lock_disp(ctx):
    """Fine strands running along each lock (tubes): grooves round the lock's axis, broken up a little."""
    if ctx.t is None:
        return cap_disp(ctx)
    a = S.angle_around(ctx, UPV)
    return 0.00055 * (np.abs(np.sin(a * 11 + 2.5 * C.value_noise(ctx.p, 40.0, 4))) - 0.6)


def cap_disp(ctx):
    # Strands on the cap run from the front to the bun (about along the head's forward axis).
    w = ctx.p[:, 0] * 900 + 3 * C.value_noise(ctx.p, 30.0, 9)
    return 0.0005 * (np.abs(np.sin(w)) - 0.6)


LOCK_STYLE = style(hair_color, rough=0.44, disp_fn=lock_disp, grad=(1.1, 1.0, 0.65))
CAP_STYLE = style(hair_color, rough=0.48, disp_fn=cap_disp, grad=(1.05, 1.0, 0.6))
BROW_STYLE = style(lambda ctx: BROW, rough=0.6, grad=(1.0, 1.0, 0.9))


COAT_FAM = fam(weave=1.0)


def coat_color(ctx):
    col = np.broadcast_to(COAT, (len(ctx.p), 3)).copy()
    return mix(col, COAT_SHADE, C.smoothstep(-0.1, -0.9, ctx.n[:, 2]) * 0.45)


def coat_folds(ctx):
    """Soft cloth folds: long vertical drapes on the body, rings round the sleeves, all broken up by noise."""
    p = ctx.p
    drape = np.sin(p[:, 0] * 60 + 5 * C.value_noise(p, 5.0, 2)) * C.smoothstep(1.2, 1.0, p[:, 2])
    return 0.0016 * drape + 0.0006 * C.value_noise(p, 60.0, 3)


def sleeve_folds(ctx):
    if ctx.t is None:
        return coat_folds(ctx)
    # Compression folds: rings that wander, deeper at the inside of the elbow end.
    ring = np.sin(ctx.t * 34 + 3 * C.value_noise(ctx.p, 12.0, 7))
    return 0.0005 * ring * (0.3 + 0.7 * C.smoothstep(0.3, 1.0, ctx.t)) + 0.0004 * C.value_noise(ctx.p, 50.0, 8)


def seam(values, centre, width=0.0016):
    return 1 - C.smoothstep(0.0, width, np.abs(values - centre))


def coat_panel_color(ctx):
    col = coat_color(ctx)
    p = ctx.p
    # Side seams under the arms and a centre-back seam, as darker stitched lines.
    side = seam(np.arctan2(np.abs(p[:, 0]), -p[:, 1] - TORSO_FWD), math.radians(100), 0.03)
    back = seam(p[:, 0], 0.0, 0.0015) * C.smoothstep(0.0, 0.05, p[:, 1])
    return mix(col, COAT_SEAM, np.maximum(side, back) * 0.7)


def shoulder_line(x):
    """Height of the shoulder seam: from the neck down to the shoulder point."""
    return 1.288 - np.clip(np.abs(x) - 0.06, 0, None) * 0.42


def yoke_color(ctx):
    col = coat_color(ctx)
    p = ctx.p
    # The colony-blue yoke stripe along the top of each shoulder, edged by stitched seams; drawn from the shoulder
    # line, so it stays a clean band over the lumps of the cloth.
    dz = p[:, 2] - shoulder_line(p[:, 0])
    top = C.smoothstep(0.1, 0.35, ctx.n[:, 2]) * C.smoothstep(0.05, 0.075, np.abs(p[:, 0]))
    band = C.smoothstep(-0.03, -0.026, dz) * (1 - C.smoothstep(-0.009, -0.005, dz)) * top
    col = mix(col, COLONY, band)
    col = mix(col, COAT_SEAM * 0.8, np.maximum(seam(dz, -0.032, 0.0012), seam(dz, -0.003, 0.0012)) * top)
    # The NTI patch on the left upper arm: a navy disc with a gold nova point.
    patch = near(ctx, PATCH, 0.02, 0.15)
    col = mix(col, NAVY, patch)
    return mix(col, NOVA, near(ctx, PATCH + np.array((0, -0.003, 0.0)), 0.007, 0.4))


PATCH = np.array(G(0.198, 1.15, 0.012))


def patch_emit(ctx):
    return NOVA[None, :] * near(ctx, PATCH + np.array((0, -0.003, 0.0)), 0.007, 0.4)[:, None] * 0.7


COAT_STYLE = style(coat_panel_color, rough=0.8, family=COAT_FAM, disp_fn=coat_folds)
def sleeve_color(ctx):
    col = coat_color(ctx)
    if ctx.t is None:
        return col
    # The set-in sleeve's armhole seam and a seam down the back of the arm.
    ring = seam(ctx.t, 0.12, 0.012)
    back = seam(S.angle_around(ctx, Vector((0, 1, 0))), 0.0, 0.05)
    return mix(col, COAT_SEAM, np.maximum(ring, back) * 0.75)


SLEEVE_STYLE = style(sleeve_color, rough=0.8, family=COAT_FAM, disp_fn=sleeve_folds)
YOKE_STYLE = style(yoke_color, rough=0.8, family=COAT_FAM, disp_fn=lambda c: 0.0005 * C.value_noise(c.p, 50.0, 3), emit_fn=patch_emit)


LAPEL_STYLE = style(coat_color, rough=0.76, family=COAT_FAM, disp_fn=lambda c: 0.0004 * C.value_noise(c.p, 70.0, 5))


def navy_color(ctx):
    col = mix(NAVY, NAVY_DARK, C.smoothstep(-0.1, -0.8, ctx.n[:, 2]) * 0.6)
    return col


def rib_disp(ctx):
    return 0.0004 * np.sin(ctx.p[:, 0] * 900)


UNDERSUIT_STYLE = style(navy_color, rough=0.62, family=fam(weave=1.0), disp_fn=rib_disp)
COLLAR_TOP = 1.33


def mock_collar_color(ctx):
    rim = seam(ctx.p[:, 2], COLLAR_TOP - 0.004, 0.0028)
    return mix(navy_color(ctx), CYAN * 0.55, rim)


def mock_collar_emit(ctx):
    return CYAN[None, :] * seam(ctx.p[:, 2], COLLAR_TOP - 0.004, 0.0028)[:, None]


MOCK_COLLAR_STYLE = style(mock_collar_color, rough=0.55, family=fam(weave=1.0), emit_fn=mock_collar_emit)


def metal_style(glow_mask=None, base=GUNMETAL, rough=0.32):
    def color(ctx):
        col = np.broadcast_to(base, (len(ctx.p), 3)).copy()
        return mix(col, CYAN * 0.6, glow_mask(ctx)) if glow_mask else col

    def emit(ctx):
        return CYAN[None, :] * glow_mask(ctx)[:, None] if glow_mask else 0.0
    return style(color, rough=rough, family=fam(metal=1.0), emit_fn=emit if glow_mask else None)


def card_color(ctx):
    col = np.broadcast_to(YELLOW, (len(ctx.p), 3)).copy()
    u = ctx.u
    photo = (np.abs(u[:, 0] + 0.25) < 0.38) * (np.abs(u[:, 2] - 0.12) < 0.42) * (u[:, 1] < -0.2)
    lines = (np.abs(u[:, 0] - 0.45) < 0.32) * (np.abs(np.sin(u[:, 2] * 9)) < 0.35) * (np.abs(u[:, 2]) < 0.6) * (u[:, 1] < -0.2)
    col = mix(col, np.array((0.15, 0.2, 0.3)), photo.astype(float))
    return mix(col, np.array((0.35, 0.28, 0.1)), lines.astype(float) * 0.7)


# -------------------------------------------------------------------------------------------------------------
# Shapes
# -------------------------------------------------------------------------------------------------------------

def gb(X, U, x0, u0, sx, su):
    return math.exp(-((X - x0) / sx) ** 2 - ((U - u0) / su) ** 2)


def head_deform(v):
    """The sculpted head on the unit sphere (Blender local: x across, -y forward, z up): a cranium fuller at the
    back, a flatter face plane, eye sockets, cheekbones, a tapered jaw to a small chin, a slight muzzle, the skull
    under the occiput drawn in towards the neck."""
    X, U, F = v.x, v.z, -v.y
    front = ss(-0.05, 0.45, F)
    lower = ss(0.12, -0.95, U)
    X2 = X * (1 - 0.17 * lower ** 1.5 * (0.45 + 0.55 * front))
    # A shorter lower face: the jaw and chin come up a little.
    U = U if U > -0.25 else -0.25 + (U + 0.25) * 0.9
    F2 = F
    if F < 0:
        F2 = F * (1 + 0.1 * ss(-0.3, 0.5, U) - 0.22 * ss(-0.2, -0.9, U) * ss(0.0, -0.6, F))
    if F2 > 0.5:
        F2 = 0.5 + (F2 - 0.5) * 0.72
    r = 1.0
    if F > -0.1:
        bumps = 0.0
        for sx in (1, -1):
            bumps += -0.13 * gb(X, U, 0.43 * sx, -0.02, 0.17, 0.11)     # eye sockets
            bumps += 0.05 * gb(X, U, 0.6 * sx, -0.2, 0.16, 0.14)        # cheekbones
            bumps += 0.035 * gb(X, U, 0.4 * sx, 0.2, 0.2, 0.06)         # brow
            bumps += -0.035 * gb(X, U, 0.56 * sx, -0.54, 0.14, 0.16)    # under the cheekbones
            bumps += -0.02 * gb(X, U, 0.82 * sx, 0.3, 0.12, 0.2)        # temples
            bumps += 0.03 * gb(X, U, 0.62 * sx, -0.62, 0.16, 0.14)      # a soft jaw line
        bumps += 0.04 * gb(X, U, 0.0, -0.5, 0.3, 0.2)                   # muzzle
        bumps += 0.07 * gb(X, U, 0.0, -0.86, 0.22, 0.11)                # chin
        bumps += 0.03 * gb(X, U, 0.0, -0.02, 0.1, 0.16)                 # nose bridge between the sockets
        r += bumps * ss(-0.1, 0.4, F)
    return Vector((X2 * r, -F2 * r, U * r))


def ray_hit(tree, origin, direction):
    hit = tree.ray_cast(Vector(origin), Vector(direction).normalized())
    return hit[0]


def head_point(tree, direction, lift=0.0):
    """Where a ray from the head's centre leaves the head (or the nearest point), lifted outwards."""
    d = Vector(direction).normalized()
    far = HEAD_C + d * 0.3
    hit = tree.ray_cast(far, -d)
    p = hit[0] if hit[0] is not None else HEAD_C + d * 0.09
    return p + d * lift


def face_point(tree, x, up, lift=0.0):
    """The face surface straight in front of (x, up), lifted along +forward."""
    hit = tree.ray_cast(G(x, up, 0.3), Vector((0, 1, 0)))
    p = hit[0] if hit[0] is not None else G(x, up, 0.07)
    return p + G(0, 0, lift)


def sph(az, el):
    """A direction from the head's centre: azimuth round up (0 forward, + towards her left), elevation."""
    return G(math.sin(az) * math.cos(el), math.sin(el), math.cos(az) * math.cos(el))


def torso_r(z):
    """Half-widths of the torso at height z: (across, front, back)."""
    zs = [0.9, 1.0, 1.08, 1.14, 1.2, 1.25, 1.285]
    return (float(np.interp(z, zs, [0.142, 0.143, 0.147, 0.152, 0.158, 0.16, 0.13])),
            float(np.interp(z, zs, [0.084, 0.086, 0.094, 0.1, 0.09, 0.078, 0.066])),
            float(np.interp(z, zs, [0.08, 0.08, 0.082, 0.084, 0.086, 0.084, 0.072])))


TORSO_FWD = -0.014


def torso_pt(z, a, off):
    rx, rf, rb = torso_r(z)
    c = math.cos(a)
    depth = rf if c > 0 else rb
    return G((rx + off) * math.sin(a), z, TORSO_FWD + (depth + off) * c)


BUTTON_Z = 1.035
TOP_Z = 1.27


def a_open(z):
    """Half-angle of the coat's front opening at height z (closed below the top button, a V above it)."""
    return 0.012 + 0.45 * ss(BUTTON_Z, TOP_Z, z) ** 0.9


def lapel_width(t):
    """Lapel width along it (0 at the button, 1 at the collar), with the notch where the collar meets it."""
    w = 0.006 + 0.05 * ss(0.0, 0.8, t)
    if t > 0.8:
        w = 0.056 - 0.028 * ss(0.8, 0.84, t)
    return w


def build_parts():
    b = C.PartBuilder()
    # --- Head and neck (one sculpted surface) ---------------------------------------------------------------
    b.ellipsoid("Head", HEAD_C, R(*HEAD_R), bone="Head", style=SKIN_STYLE, segments=(128, 80), deform=head_deform)
    head_tree = b.bvh()
    b.tube("Neck", [G(0, 1.25, -0.024), G(0, 1.33, -0.02), G(0, 1.41 - DROP, -0.014)], [0.051, 0.05, 0.049], sides=20, chain=["Neck"],
           style=SKIN_STYLE, cap=0.2)
    for sx in (1, -1):
        b.ellipsoid("Ear", G(0.069 * sx, 1.457 - DROP, -0.012), R(0.009, 0.024, 0.015), rot=Matrix.Rotation(math.radians(12 * sx), 3, "Z"),
                    bone="Head", style=SKIN_STYLE, segments=(16, 12),
                    deform=lambda v, sx=sx: Vector((v.x * (1 - 0.5 * ss(-0.2, 0.6, v.x * sx)), v.y, v.z)))
    # A small straight nose: the bridge from between the eyes, a rounded tip and two soft nostril wings.
    bridge = face_point(head_tree, 0, 1.46 - DROP, -0.003)
    tip = G(0, 1.436 - DROP, 0.0)
    tip.y = bridge.y - 0.0115
    b.tube("Nose", [bridge, bridge.lerp(tip, 0.6) + G(0, 0, 0.001), tip], [0.0036, 0.0046, 0.006], sides=12, chain=["Head"],
           style=SKIN_STYLE, cap=0.7)
    # Lips: separate soft volumes either side of the mouth slit. The upper lip has a gentle bow.
    front = face_point(head_tree, 0, MOUTH_Z, 0.0)
    lf = front.y   # Blender y of the face at the mouth (forward is -y)

    def bow(v):
        return Vector((v.x, v.y, v.z + 0.35 * math.exp(-(v.x / 0.35) ** 2) * max(0.0, v.z) - 0.25 * abs(v.x) ** 2))
    b.ellipsoid("LipUpper", Vector((0, lf + 0.0022, MOUTH_Z + 0.0032)), (MOUTH_HALF * 0.95, 0.0045, 0.0038), bone="Head", style=LIP_STYLE,
                segments=(24, 12), deform=bow)
    b.ellipsoid("LipLower", Vector((0, lf + 0.0026, MOUTH_Z - 0.0041)), (MOUTH_HALF * 0.8, 0.0048, 0.0045), bone="Jaw", style=LIP_STYLE,
                segments=(24, 12))
    # The mouth's dark inside (its own shell behind the lips): what shows when the jaw parts them.
    b.ellipsoid("MouthInside", Vector((0, lf + 0.009, MOUTH_Z - 0.001)), (MOUTH_HALF * 0.9, 0.006, 0.0075), bone="Head",
                style=style(lambda ctx: MOUTH, rough=0.5), segments=(16, 10))
    # --- Eyes and lids ---------------------------------------------------------------------------------------
    for side, c in EYES.items():
        b.ellipsoid("Eye", c, (EYE_R, EYE_R, EYE_R), bone="Head", style=eye_style(c), segments=(28, 18))
        # The lid band runs on over the top of the eyeball, so a closed lid still covers the socket above it.
        lid_shell(b, "Lid", c, EYE_R + LID_GAP, EYE_R + LID_GAP + LID_THICK, LID_EDGE, math.radians(128), math.radians(84), f"Lid_{side}",
                  style(lid_color(c, True), rough=0.55))
        lid_shell(b, "LowerLid", c, EYE_R + LID_GAP, EYE_R + LID_GAP + LID_THICK * 0.8, math.radians(-70), LOWER_EDGE, math.radians(70),
                  "Head", style(lid_color(c, False), rough=0.55))
    # --- Brows: dark tapered strokes lying on the brow ridge -------------------------------------------------
    for sx in (1, -1):
        pts = [face_point(head_tree, x * sx, up, 0.0015) for x, up in ((0.011, 1.4855 - DROP), (0.026, 1.492 - DROP), (0.042, 1.4915 - DROP), (0.055, 1.4835 - DROP))]
        b.tube("Brow", pts, [0.0026, 0.0029, 0.0022, 0.0011], sides=8, chain=["Head"], style=BROW_STYLE, cap=0.6, flatten=0.42)
    hair(b, head_tree)
    body(b)
    coat(b)
    gear(b, head_tree)
    return b


def lid_shell(b, name, centre, r_in, r_out, el0, el1, az, bone, style_fn, nu=26, nv=8):
    """A spherical band round the eye (concentric with it, so rotating it about the eye keeps it on the eyeball):
    elevation el0..el1, azimuth -az..az round forward. The rim at el0 is rounded by a thinner outer row."""
    c = Vector(centre)

    def at(radius, rim):
        def fn(u, v):
            a = -az + 2 * az * u
            e = el0 + (el1 - el0) * v
            # Lids follow the eye's almond: the band narrows towards the corners.
            corner = abs(2 * u - 1) ** 2.2
            if el0 > 0:
                e = e - corner * math.radians(16) * (1 - v)
            else:
                e = e + corner * math.radians(12) * v
            d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
            rr = radius
            if rim and ((el0 > 0 and v < 0.13) or (el0 < 0 and v > 0.87)):
                rr = r_in + (radius - r_in) * 0.55
            return c + d * rr
        return fn
    b.slab(name, at(r_out, True), at(r_in, False), nu, nv, bone=bone, style=style_fn)


def hair(b, head_tree):
    """A cap of hair over the cranium with a clean hairline, broad sculpted locks half sunk into it (they read as
    locks, not as tubes) swept from the hairline and the side parting back to a twisted bun, and a side-swept
    fringe."""
    def cap(v):
        X, U, F = v.x, v.z, -v.y
        # The hairline: pull the cap well inside the head on the forehead, the temples, round the ears, the nape.
        k = 0.0
        if F > -0.2:
            k = ss(0.5, 0.33, U) * ss(-0.2, 0.3, F)
        side = ss(0.12, -0.12, U) * ss(-0.5, 0.0, F)
        nape = ss(-0.3, -0.55, U) * ss(0.1, -0.3, F)
        k = max(k, side, nape)
        h = head_deform(v)
        return h * (1.05 + 0.03 * ss(-0.2, 0.7, U) * (1 - k) - 0.3 * k)
    b.ellipsoid("HairCap", HEAD_C, R(*HEAD_R), bone="Head", style=CAP_STYLE, segments=(96, 60), deform=cap)
    bun = G(0, 1.488 - DROP, -0.104)

    def lock(start_dir, r0, lift=0.004, end=None, samples=7, name="Lock", flatten=0.4):
        """A lock from a hairline direction to `end` (default: the bun), following the head surface."""
        target = end or bun
        d0 = Vector(start_dir).normalized()
        d1 = (target - HEAD_C).normalized()
        pts, radii = [], []
        for i in range(samples):
            t = i / (samples - 1)
            d = d0.slerp(d1, t)
            pts.append(head_point(head_tree, d, lift * math.sin(math.pi * min(1.0, 0.2 + t)) + 0.001))
            radii.append(r0 * (0.25 + 0.75 * ss(0.0, 0.3, t)) * (1 - 0.35 * ss(0.7, 1.0, t)))
        pts[-1] = pts[-1].lerp(target, 0.6)
        b.tube(name, pts, radii, sides=12, chain=["Head"], style=LOCK_STYLE, cap=0.5, flatten=flatten)
    # Locks off the hairline either side of the parting, close enough to overlap into one swept mass.
    part_az = math.atan2(PART_X, 0.06)
    for i, az in enumerate(np.linspace(-1.4, part_az - 0.1, 10)):
        el = 0.62 - 0.3 * abs(az) ** 1.5
        lock(sph(az, el), 0.0098 - 0.001 * (i % 2), lift=0.0005 * (i % 3), flatten=1.0)
    for i, az in enumerate(np.linspace(part_az + 0.12, 1.4, 7)):
        el = 0.62 - 0.3 * abs(az) ** 1.5
        lock(sph(az, el), 0.0098 - 0.001 * (i % 2), lift=0.0005 * (i % 3), flatten=1.0)
    # Side and back locks gathered up into the bun.
    for az in (1.8, 2.25, 2.7, -1.8, -2.25, -2.7):
        lock(sph(az, -0.28), 0.0095, lift=0.0, samples=6, flatten=1.0)
    # The fringe: a broad lock from the parting sweeping across the forehead to her right temple and back over the
    # ear into the bun, a narrower one tucked under it.
    for k, (el0, r0) in enumerate(((0.8, 0.0135), (0.7, 0.011))):
        p0 = head_point(head_tree, sph(part_az, el0), 0.006)
        p1 = face_point(head_tree, -0.008, 1.546 - DROP - 0.01 * k, 0.009)
        p2 = face_point(head_tree, -0.044, 1.528 - DROP - 0.012 * k, 0.007)
        p3 = head_point(head_tree, sph(-1.3, 0.24 - 0.08 * k), 0.006)
        p4 = head_point(head_tree, sph(-2.0, 0.08), 0.005)
        b.tube("Fringe", [p0, p1, p2, p3, p4, bun + G(-0.02, 0.0, 0.012)], [r0 * 0.6, r0, r0, r0 * 0.85, r0 * 0.7, 0.008], sides=14,
               chain=["Head"], style=LOCK_STYLE, cap=0.5, flatten=0.6)
    # The bun: a core and a lock twisted round it one and a half times.
    b.ellipsoid("BunCore", bun + G(0, 0, -0.006), R(0.028, 0.027, 0.022), bone="Head", style=CAP_STYLE, segments=(20, 14))
    pts = []
    for i in range(22):
        t = i / 21
        a = 2 * math.pi * 1.4 * t
        rr = 0.026 * (1 - 0.55 * t)
        pts.append(bun + G(math.cos(a) * rr, math.sin(a) * rr * 0.95, -0.008 - 0.012 * t))
    b.tube("Bun", pts, [0.0125, 0.012, 0.0105, 0.009], sides=12, samples=3, chain=["Head"], style=LOCK_STYLE, cap=0.6, flatten=0.8)


def body(b):
    """The navy utility top under the coat (it shows in the V) and its low mock collar with the lit seam."""
    def surf(off):
        def fn(u, v):
            return torso_pt(0.92 + (1.262 - 0.92) * v, 2 * math.pi * u, off)
        return fn
    b.slab("Torso", surf(-0.009), surf(-0.035), 40, 16, chain=["Chest", "Spine"], style=UNDERSUIT_STYLE)
    b.tube("MockCollar", [G(0, 1.258, -0.02), G(0, 1.287, -0.019), G(0, COLLAR_TOP, -0.018)], [0.062, 0.0595, 0.0575], sides=24,
           chain=["Neck", "Chest"], style=MOCK_COLLAR_STYLE, cap=0.15)


def coat(b):
    """The field-lab coat: body panels open in a V above the top button, a yoke over each shoulder, sleeves, a
    rolled collar round the back of the neck, notched lapels folded back on the chest and a breast pocket."""
    zb = 0.92

    def panel(off):
        def fn(u, v):
            z = zb + (TOP_Z - zb) * v
            a0 = a_open(z)
            return torso_pt(z, a0 + (2 * math.pi - 2 * a0) * u, off)
        return fn
    b.slab("Coat", panel(0.011), panel(-0.002), 56, 16, chain=["Chest", "Spine"], style=COAT_STYLE)
    for side, sx in (("L", 1), ("R", -1)):
        b.ellipsoid("Yoke", G(0.088 * sx, 1.232, -0.018), R(0.088, 0.05, 0.078), rot=Matrix.Rotation(math.radians(-18 * sx), 3, "Y"),
                    bone=f"Clavicle_{side}", style=YOKE_STYLE, segments=(28, 14))
        b.parts[-1].chain = [f"Clavicle_{side}", "Chest"]
        b.tube("Sleeve", [G(0.158 * sx, 1.228, -0.016), G(0.18 * sx, 1.1, -0.01), G(0.186 * sx, 0.95, -0.002)], [0.05, 0.045, 0.043], sides=18,
               chain=[f"Clavicle_{side}"], style=SLEEVE_STYLE, cap=0.8)

    # Collar: rises from round the neck at the back and rolls outwards a little; its ends run into the lapel tops.
    def collar(off):
        def fn(u, v):
            a = math.radians(48 + (360 - 96) * u)
            z = 1.266 + 0.058 * v
            r = 0.067 + 0.012 * v ** 1.5 + off
            # The front ends dip to meet the lapel tops.
            return G(math.sin(a) * r, z - 0.016 * ss(0.5, 0.75, math.cos(a)), -0.018 + math.cos(a) * r)
        return fn
    b.slab("Collar", collar(0.0075), collar(0.0), 36, 5, chain=["Chest", "Neck"], style=LAPEL_STYLE)
    for side, sx in (("L", 1), ("R", -1)):
        def lapel(off, sx=sx):
            def fn(u, v):
                t = v
                z = BUTTON_Z + (TOP_Z + 0.008 - BUTTON_Z) * t
                rx = torso_r(z)[0]
                a = a_open(z) - 0.035 + u * lapel_width(t) / rx
                return torso_pt(z, a * sx, off)
            return fn
        b.slab("Lapel", lapel(0.0175), lapel(0.008), 8, 24, chain=["Chest"], style=LAPEL_STYLE)
    # Breast pocket on her left, with two pens clipped in it.
    def pocket(off):
        def fn(u, v):
            z = 1.05 + 0.058 * v
            rx = torso_r(z)[0]
            a = math.asin(min(0.99, (0.05 + 0.048 * u) / rx))
            return torso_pt(z, a, off)
        return fn
    b.slab("Pocket", pocket(0.0145), pocket(0.008), 10, 8, chain=["Chest"], style=LAPEL_STYLE)

    # The buttoned placket below the V, so the front reads closed rather than as a slit.
    def placket(off):
        def fn(u, v):
            z = zb + (BUTTON_Z + 0.012 - zb) * v
            return torso_pt(z, -0.05 + 0.1 * u, off)
        return fn
    b.slab("Placket", placket(0.0135), placket(0.004), 6, 10, chain=["Chest", "Spine"], style=LAPEL_STYLE)


def gear(b, head_tree):
    # Headset: a slim band over the crown (on the hair), an earpiece with a lit ring over her right ear, a small
    # pad on the left, a mic boom along the cheek.
    band = [head_point(head_tree, sph(az, el), 0.016) for az, el in ((-1.57, 0.05), (-1.5, 0.6), (0.0, 1.35), (1.5, 0.6), (1.57, 0.05))]
    b.tube("Band", band, [0.0042] * 5, sides=8, chain=["Head"], style=metal_style(), cap=0.5)
    ear_r = G(-0.081, 1.457 - DROP, -0.01)
    b.ellipsoid("Earpiece", ear_r, R(0.011, 0.022, 0.02), bone="Head",
                style=metal_style(lambda ctx: (1 - C.smoothstep(0.0, 0.08, np.abs(np.linalg.norm(ctx.u[:, 1:], axis=1) - 0.72))) * (ctx.u[:, 0] < -0.3)),
                segments=(20, 12))
    b.ellipsoid("EarpieceL", G(0.079, 1.457 - DROP, -0.01), R(0.007, 0.014, 0.012), bone="Head", style=metal_style(), segments=(12, 8))
    mic_end = face_point(head_tree, -0.03, 1.405 - DROP, 0.012)
    b.tube("Mic", [ear_r + G(-0.004, -0.012, 0.012), G(-0.074, 1.42 - DROP, 0.05), mic_end + G(-0.006, 0, -0.004)], [0.0026, 0.0024, 0.0022],
           sides=6, chain=["Head"], style=metal_style(), cap=0.5)
    b.ellipsoid("MicTip", mic_end, R(0.0045, 0.004, 0.0045), bone="Head", style=metal_style(lambda ctx: np.ones(len(ctx.p)) * 0.8), segments=(10, 6))
    # Hair pin through the bun.
    b.tube("Pin", [G(-0.05, 1.5 - DROP, -0.092), G(0.055, 1.484 - DROP, -0.108)], [0.0028, 0.0028], sides=6, chain=["Head"],
           style=metal_style(lambda ctx: (ctx.t > 0.92).astype(float)), cap=0.8)
    # ID card on the left lapel (the only yellow), its clip, the pens and the coat's buttons.
    card_c = torso_pt(1.165, 0.42, 0.024)
    b.ellipsoid("Card", card_c, R(0.017, 0.025, 0.003), rot=Matrix.Rotation(math.radians(24), 3, "Z") @ Matrix.Rotation(math.radians(-10), 3, "X"),
                bone="Chest", style=style(card_color, rough=0.4, family=fam(ceramic=1.0)),
                deform=lambda v: Vector(tuple(math.copysign(abs(c) ** 0.3, c) for c in v)), segments=(14, 10))
    b.ellipsoid("CardClip", card_c + G(0.0, 0.027, 0.0), R(0.006, 0.006, 0.0045), bone="Chest", style=metal_style(), segments=(8, 6))
    for x, col in ((0.064, NAVY), (0.077, GUNMETAL * 0.7)):
        a = math.asin(x / torso_r(1.12)[0])
        p0 = torso_pt(1.075, a, 0.011)
        p1 = torso_pt(1.128, a, 0.0125)
        b.tube("Pen", [p0, p1], [0.0035, 0.0035], sides=8, chain=["Chest"], style=metal_style(base=col, rough=0.3), cap=0.6)
    for z in (0.975, BUTTON_Z):
        b.ellipsoid("Button", torso_pt(z, 0.0, 0.015), R(0.0055, 0.0055, 0.0022), bone="Chest", style=metal_style(base=np.array((0.8, 0.82, 0.85)), rough=0.35),
                    segments=(12, 6))


RIGID = {"Lid", "Eye", "MouthInside", "Earpiece", "EarpieceL", "Mic", "MicTip", "Band", "Pin", "Card", "CardClip", "Pen", "Button"}

LAYERS = [S.Layer("head", {"Head", "Neck", "Ear", "Nose", "LipUpper", "LipLower"}, voxel=0.0011, smooth=4, tris=3300, uv_scale=1.7),
          S.Layer("eyes", {"Eye"}, voxel=0.001, smooth=1, tris=440, uv_scale=2.2),
          S.Layer("lids", {"Lid", "LowerLid"}, voxel=0.00055, smooth=1, tris=760, uv_scale=1.6),
          S.Layer("hair", {"HairCap", "Lock", "Fringe", "BunCore", "Bun", "Brow"}, voxel=0.0011, smooth=2, tris=1500, uv_scale=1.1),
          S.Layer("body", {"Torso", "MockCollar"}, voxel=0.0024, smooth=4, tris=420, uv_scale=0.5),
          S.Layer("coat", {"Coat", "Yoke", "Sleeve", "Collar", "Lapel", "Pocket", "Placket"}, voxel=0.0018, smooth=3, density=1.0),
          S.Layer("gear", None, voxel=0.0009, smooth=2, tris=750, uv_scale=1.3)]


def bones():
    return [tk.Bone("Root", (0, 0, 0), UPV, length=0.1),
            tk.Bone("Spine", G(0, 0.98, 0), UPV, "Root", length=0.1),
            tk.Bone("Chest", G(0, 1.12, 0), UPV, "Spine", length=0.1),
            tk.Bone("Clavicle_L", G(0.02, 1.24, -0.01), XAX, "Chest", length=0.12),
            tk.Bone("Clavicle_R", G(-0.02, 1.24, -0.01), -XAX, "Chest", length=0.12),
            tk.Bone("Neck", G(0, 1.26, -0.02), UPV, "Chest", length=0.08),
            tk.Bone("Head", G(0, 1.39 - DROP, -0.012), UPV, "Neck", length=0.12),
            tk.Bone("Jaw", JAW_HINGE, (G(0, 1.37 - DROP, 0.06) - JAW_HINGE), "Head", length=0.08),
            tk.Bone("Lid_L", EYES["L"], Vector((0, -1, 0)), "Head", length=0.02),
            tk.Bone("Lid_R", EYES["R"], Vector((0, -1, 0)), "Head", length=0.02)]


JAW_HINGE = G(0, 1.432 - DROP, -0.018)


# -------------------------------------------------------------------------------------------------------------
# Clips (bk.clip: rotations in armature space about each bone's head)
# -------------------------------------------------------------------------------------------------------------

def blink_curve(f, start, length=7):
    u = (f - start) / length
    if u <= 0 or u >= 1:
        return 0.0
    return math.sin(math.pi * u) ** 0.6


def lids(k):
    return {"Lid_L": {"rot": (XAX, BLINK * k)}, "Lid_R": {"rot": (XAX, BLINK * k)}}


def breath(t, cycles, amount=1.0):
    b = 0.5 - 0.5 * math.cos(2 * math.pi * cycles * t)
    return {"Chest": {"loc": Vector((0, 0, 0.0035 * b * amount)), "rot": (XAX, -0.012 * b * amount)},
            "Clavicle_L": {"rot": (Vector((0, 1, 0)), -0.025 * b * amount)},
            "Clavicle_R": {"rot": (Vector((0, 1, 0)), 0.025 * b * amount)},
            "Neck": {"rot": (XAX, 0.008 * b * amount)}}


def idle(t, f):
    spec = breath(t, 2)
    spec["Head"] = {"rot": (Vector((0.15, 0.3, 1.0)).normalized(), 0.035 * math.sin(2 * math.pi * t))}
    spec.update(lids(blink_curve(f, 70)))
    return spec


SYLL = [0.0, 0.8, 0.35, 1.0, 0.2, 0.7, 0.0, 0.0, 0.9, 0.5, 0.85, 0.3, 0.6, 0.0, 0.75, 0.25, 1.0, 0.4, 0.0, 0.0]
JAW_OPEN = math.radians(7.0)


def talk(t, f):
    spec = breath(t, 1, 0.7)
    x = t * len(SYLL)
    i = int(x) % len(SYLL)
    u = x - int(x)
    v = SYLL[i] + (SYLL[(i + 1) % len(SYLL)] - SYLL[i]) * (u * u * (3 - 2 * u))
    nod = 0.05 * math.sin(2 * math.pi * 2 * t) * (0.5 + 0.5 * math.sin(2 * math.pi * t))
    spec["Head"] = {"rot": (XAX, nod + 0.015 * v)}
    spec["Jaw"] = {"rot": (XAX, JAW_OPEN * v)}
    spec.update(lids(blink_curve(f, 44)))
    return spec


def blink(t, f):
    return lids(blink_curve(f, 0, 8))


CLIPS = {"Idle": (120, idle, True), "Talk": (90, talk, True), "Blink": (8, blink, False)}


# -------------------------------------------------------------------------------------------------------------
# Weights
# -------------------------------------------------------------------------------------------------------------

def skin_mesh(obj, arm, builder, index):
    C.skin(obj, arm, builder, rigid=RIGID, blend=1)
    jaw_weights(obj, index)


def jaw_weights(obj, index):
    """The lower face follows the Jaw by position, not by part: everything below the mouth slit in front of the
    jaw hinge, fading out towards the ears and down the throat. Across the slit the weight changes in one step
    (the lips part cleanly); round the mouth corners and on the cheeks it fades, so the face folds instead of
    stretching."""
    layers = index.vertex_layers(obj.data)
    head_layer = [layer.name for layer in LAYERS].index("head")
    groups = {g.name: g for g in obj.vertex_groups}
    jaw, head, neck = groups["Jaw"], groups["Head"], groups["Neck"]
    for v in obj.data.vertices:
        if layers is not None and layers[v.index] != head_layer:
            continue
        p = v.co
        fwd = -p.y
        below = p.z < MOUTH_Z
        dz = MOUTH_Z - p.z
        x = abs(p.x)
        if fwd > 0.055 and x < MOUTH_HALF + 0.004:
            # At the lips: a hard split across the slit, softened only right at the corners.
            corner = ss(MOUTH_HALF - 0.004, MOUTH_HALF + 0.004, x)
            w = (1.0 if below else 0.0) * (1 - corner) + corner * ss(-0.004, 0.02, dz) * 0.7
        else:
            w = ss(-0.006, 0.026, dz) * 0.9
            w *= ss(-0.03, 0.035, fwd)                        # nothing behind the jaw line
            w *= 1 - ss(0.045, 0.068, x) * ss(-0.02, 0.03, dz)  # cheeks towards the ears move less
        w *= 1 - ss(1.375 - DROP, 1.35 - DROP, p.z) * ss(0.04, -0.01, fwd)  # the throat stays with the neck
        if w <= 0.01:
            jaw.remove([v.index])
            continue
        rest = 1 - w
        # Whatever was not jaw before is scaled into the remaining share (head or neck).
        old = {g.group: g.weight for g in v.groups if g.group != jaw.index}
        total = sum(old.values()) or 1.0
        for gi, wt in old.items():
            obj.vertex_groups[gi].add([v.index], wt / total * rest, "REPLACE")
        if not old:
            (head if p.z > 1.37 - DROP else neck).add([v.index], rest, "REPLACE")
        jaw.add([v.index], w, "REPLACE")


# -------------------------------------------------------------------------------------------------------------
# Build
# -------------------------------------------------------------------------------------------------------------

def stage_exports(args):
    if args.get("stage"):
        C.EXPORTS = os.path.join(args["stage"], "Exports")
        C.BLEND = os.path.join(args["stage"], "Blend")


def main():
    C.reset_scene()
    args = C.cli_args()
    stage_exports(args)
    out = C.preview_dir()
    builder = build_parts()
    if args.get("fast"):
        fast_preview(builder, out)
        return
    asset, index = S.build_layered_asset(NAME, builder, LAYERS, voxel=0.0018, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.02,
                                         extrusion=0.004, ray=0.012, emission_strength=2.2, specular=0.35, mirror=False, k=0.6,
                                         families=FAMILIES, family_set=S.FAMILY_SET, low_size=LOW_TEX, preview_detail=(0.5, 0.5),
                                         edge_gain=0.15, border_smooth=2, symmetric=False,
                                         unwrap="smart")
    arm = tk.build_armature(NAME + "_Rig", bones())
    skin_mesh(asset.low, arm, builder, index)
    S.drop_layer_attr(asset.low)
    acts = {n: bk.clip(arm, n, fr, fn, loop=lp) for n, (fr, fn, lp) in CLIPS.items()}
    bk.rest_pose(arm)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    C.write_report(NAME, {
        "asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris, "lod1": "none: portrait and cutscene only",
        "bones": [b.name for b in arm.data.bones], "texture_size": TEX, "low_texture_size": LOW_TEX,
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
        "layers": [layer.name for layer in LAYERS],
        "clips": {n: fr for n, (fr, fn, lp) in CLIPS.items()}, "events": {"Idle": {"blink": 70}, "Talk": {"blink": 44}},
        "blink": "Lid_L/Lid_R rotate about +X by %.0f degrees to close" % math.degrees(BLINK),
        "jaw": "Jaw rotates about +X up to %.0f degrees in Talk; the mouth is a real slit" % math.degrees(JAW_OPEN),
        "portrait": {"target": [0, 1.36, 0.0], "distance": 1.45, "azimuth": -28, "elevation": 5, "lens_mm": 70,
                     "note": "game space (x, up, forward); same camera as mercer_bust.png, aimed 3 cm higher"}})
    C.save_blend(NAME)
    if not args.get("norender"):
        previews(arm, asset, acts, out)


def fast_preview(builder, out):
    """Shape check: the styled source with lit vertex colours, no bake."""
    high_mesh, fused, _ = S.fuse_layers(NAME, builder, LAYERS, 0.0018, border_smooth=2)
    for m in fused:
        bpy.data.meshes.remove(m)
    C.shade_smooth(high_mesh)
    obj = C.link(bpy.data.objects.new(NAME + "_Source", high_mesh))
    obj.data.materials.append(S.source_preview_material(specular=0.35))
    rig = sk.portrait_lights(0.6)
    shots = portrait_shots()
    for name, cam in shots:
        C.render(cam, os.path.join(out, f"fast_{name}.png"), 900, 900)
        bk.remove([cam])
    bk.remove(rig)


def portrait_shots():
    return [("portrait", sk.portrait_camera(G(0, 1.36, 0.0))),
            ("face", C.closeup_camera(G(0, 1.415, 0.03), 0.62, azimuth=-25, elevation=4, lens=70)),
            ("front", C.closeup_camera(G(0, 1.415, 0.03), 0.62, azimuth=0, elevation=2, lens=70)),
            ("profile", C.closeup_camera(G(0, 1.42, 0.0), 0.95, azimuth=-80, elevation=4, lens=70)),
            ("back", C.closeup_camera(G(0, 1.42, -0.02), 1.0, azimuth=150, elevation=10, lens=70)),
            ("bust_full", C.closeup_camera(G(0, 1.25, 0.0), 1.9, azimuth=-20, elevation=10, lens=60))]


def previews(arm, asset, acts, out):
    rig = sk.portrait_lights(0.6)
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    names = {"portrait": "voss_portrait", "face": "voss_face", "front": "voss_front", "profile": "voss_profile", "back": "voss_back",
             "bust_full": "voss_bust_full"}
    for name, cam in portrait_shots():
        C.render(cam, os.path.join(out, names[name] + ".png"), 1024, 1024)
        bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.4, 0.03), 0.55, azimuth=-15, elevation=2, lens=70)
    sk.strip(arm, acts["Talk"], cam, (0, 5, 10, 14, 22, 44), os.path.join(out, "voss_talk_strip.png"), width=400, height=400)
    sk.strip(arm, acts["Blink"], cam, (0, 2, 4, 6, 8), os.path.join(out, "voss_blink_strip.png"), width=400, height=400)
    close = C.closeup_camera(G(0, 1.381, 0.06), 0.3, azimuth=-20, elevation=3, lens=70)
    sk.strip(arm, acts["Talk"], close, (0, 10, 14, 22), os.path.join(out, "voss_mouth_strip.png"), width=500, height=400)
    eye = C.closeup_camera(G(-0.03, 1.434, 0.06), 0.2, azimuth=-25, elevation=3, lens=70)
    sk.strip(arm, acts["Blink"], eye, (0, 2, 3, 4, 6), os.path.join(out, "voss_blink_macro_strip.png"), width=500, height=400)
    sk.strip(arm, acts["Idle"], C.closeup_camera(G(0, 1.3, 0.0), 1.3, azimuth=-60, elevation=3, lens=60), (0, 15, 30, 72, 90),
             os.path.join(out, "voss_idle_strip.png"), width=400, height=400)
    arm.animation_data.action = None
    bk.rest_pose(arm)
    macros = [("voss_macro_eyes", G(0.0, 1.434, 0.07), 0.24, -20, 5), ("voss_macro_hair", G(-0.02, 1.52, 0.0), 0.42, -40, 25),
              ("voss_macro_hair_back", G(0.0, 1.5, -0.08), 0.4, 160, 15), ("voss_macro_coat", G(0.0, 1.17, 0.06), 0.5, -30, 12),
              ("voss_macro_coat_side", G(-0.12, 1.15, 0.0), 0.55, -75, 10)]
    for name, aim, dist, az, el in macros:
        cam = C.macro_camera(aim, dist, az, el, lens=70)
        C.render(cam, os.path.join(out, name + ".png"), 1280, 720)
        bk.remove([cam])
    bk.remove(rig)
    C.uv_layout(asset.low, os.path.join(out, "voss_uv.png"), 1024, asset.textures["albedo"])


if __name__ == "__main__":
    main()
