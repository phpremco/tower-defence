"""Dr. Elena Voss, chief scientist: a bust for dialogue portraits and cutscene close-ups (round 13, see
ArtSource/DESIGN_NOTES.md).

Creature pipeline (common.py): overlapping parts fused by a voxel remesh, styles paint colour, roughness, glow and a
displacement that ends up in the normal map, a decimated game mesh (not mirrored: the headset and the fringe are on
one side), one material with four 1024 maps (+ a 512 Low set). Stylised in Mercer's family: soft volumes, painted
brows and lash lines, no pores.

Rig (10 bones): Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R (the upper eyelids are their own
shapes on the lid bones, so a blink closes them over the eyes). Clips: Idle, Talk, Blink. No LOD1 (portrait and
cutscene only).

Out: `Exports/Voss/Voss.fbx`, maps, `Voss_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_voss.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import suit as S  # noqa: E402
import tower_kit as tk  # noqa: E402
from common import G  # noqa: E402

NAME = "Voss"
GAME_TRIS = 6000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("weave", "ceramic")   # Mask B = weave (coat and undersuit), A = ceramic (the ID card); detail maps exist only for these
XAX = Vector((1, 0, 0))
UPV = Vector((0, 0, 1))

# Colours (sRGB).
SKIN = np.array((0.9, 0.72, 0.62))
SKIN_SHADE = np.array((0.74, 0.52, 0.47))
BLUSH = np.array((0.9, 0.58, 0.55))
LIP = np.array((0.74, 0.44, 0.44))
MOUTH = np.array((0.16, 0.05, 0.06))
HAIR = np.array((0.22, 0.11, 0.085))
HAIR_SHEEN = np.array((0.45, 0.27, 0.2))
LASH = np.array((0.07, 0.04, 0.04))
SCLERA = np.array((0.93, 0.93, 0.91))
IRIS = np.array((0.3, 0.5, 0.64))
IRIS_RIM = np.array((0.12, 0.2, 0.3))
PUPIL = np.array((0.025, 0.025, 0.03))
COAT = np.array((0.88, 0.89, 0.9))
COAT_SHADE = np.array((0.55, 0.58, 0.64))
NAVY = np.array((0.12, 0.19, 0.36))
COLONY = np.array((0.22, 0.38, 0.66))
YELLOW = np.array((0.98, 0.74, 0.14))
CYAN = np.array((0.32, 0.93, 1.0))
NOVA = np.array((1.0, 0.9, 0.62))

EYE_R = 0.0152
HEAD_C = G(0, 1.47, 0.0)
EYES = {"L": G(0.031, 1.464, 0.056), "R": G(-0.031, 1.464, 0.056)}
LID_EDGE = math.radians(24)       # the upper lid's edge sits this far above the eye's centre line
LOWER_EDGE = math.radians(-30)
BLINK = math.radians(54)          # lid rotation that closes the eye
MOUTH_Z = 1.403


def R(x, up, fwd):
    return (x, fwd, up)


def lin(c):
    return np.asarray(c, dtype=float)


def fam(weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0):
    return np.array((weave, ceramic, metal, rubber))


def style(color_fn, rough=0.5, family=fam(), disp_fn=None, emit_fn=None, grad=(1.04, 1.0, 0.8)):
    """A free style: colour_fn(ctx) -> (n, 3) sRGB, with a soft top-light value ramp, optional displacement
    (normal map only) and glow. Flow is 0: no stitch rows."""
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


def fwd_of(ctx):
    """How much each normal faces the viewer (game forward is Blender -Y)."""
    return -ctx.n[:, 1]


# -------------------------------------------------------------------------------------------------------------
# Styles
# -------------------------------------------------------------------------------------------------------------

def skin_color(ctx):
    col = np.broadcast_to(SKIN, (len(ctx.p), 3)).copy()
    for sx in (1, -1):
        col = mix(col, BLUSH, near(ctx, G(0.042 * sx, 1.438, 0.062), 0.028, 0.9) * 0.4)
    # Darker in the eye sockets and under the jaw, so the face reads without a strong light.
    col = mix(col, SKIN_SHADE, C.smoothstep(-0.2, -0.8, ctx.n[:, 2]) * 0.35)
    # The mouth line: dark where the lips meet; it stretches into an open mouth when the jaw drops.
    m = (1 - C.smoothstep(0.0016, 0.0034, np.abs(ctx.p[:, 2] - MOUTH_Z))) * (1 - C.smoothstep(0.015, 0.02, np.abs(ctx.p[:, 0])))
    m = m * C.smoothstep(0.045, 0.065, -ctx.p[:, 1])
    return mix(col, MOUTH, m)


SKIN_STYLE = style(skin_color, rough=0.48)


def lip_color(ctx):
    col = mix(SKIN, LIP, np.ones(len(ctx.p)) * 0.85)
    m = 1 - C.smoothstep(0.0014, 0.0032, np.abs(ctx.p[:, 2] - MOUTH_Z))
    return mix(col, MOUTH, m)


LIP_STYLE = style(lip_color, rough=0.32)


def eye_style(centre):
    c = np.asarray(centre)

    def color(ctx):
        d = ctx.p - c
        d = d / np.maximum(np.linalg.norm(d, axis=1, keepdims=True), 1e-9)
        ang = np.arccos(np.clip(-d[:, 1], -1, 1))
        col = mix(SCLERA, IRIS_RIM, 1 - C.smoothstep(0.5, 0.56, ang))
        col = mix(col, IRIS, 1 - C.smoothstep(0.38, 0.46, ang))
        col = mix(col, IRIS * 1.35, (1 - C.smoothstep(0.1, 0.4, ang)) * C.smoothstep(0.0, -0.6, d[:, 2]) * 0.5)
        col = mix(col, PUPIL, 1 - C.smoothstep(0.17, 0.21, ang))
        # Shade the top of the eye under the lid.
        return mix(col, col * 0.55, C.smoothstep(0.2, 0.5, d[:, 2]))
    return style(color, rough=0.08, grad=(1.0, 1.0, 0.9))


def lid_color(ctx, centre, upper=True):
    d = ctx.p - np.asarray(centre)
    el = np.arcsin(np.clip(d[:, 2] / np.maximum(np.linalg.norm(d, axis=1), 1e-9), -1, 1))
    col = np.broadcast_to(SKIN * 0.97, (len(ctx.p), 3)).copy()
    if upper:
        lash = 1 - C.smoothstep(LID_EDGE + 0.06, LID_EDGE + 0.2, el)
        col = mix(col, np.array((0.8, 0.6, 0.6)), C.smoothstep(LID_EDGE + 0.2, LID_EDGE + 0.6, el) * 0.3)   # a hint of shadow on the lid
    else:
        lash = (1 - C.smoothstep(LOWER_EDGE - 0.12, LOWER_EDGE - 0.02, -el + 2 * LOWER_EDGE)) * 0.0 + \
               C.smoothstep(LOWER_EDGE - 0.15, LOWER_EDGE - 0.02, el) * 0.45
    return mix(col, LASH, lash)


def hair_color(ctx):
    sheen = C.smoothstep(0.35, 0.65, ctx.n[:, 2]) * (1 - C.smoothstep(0.75, 0.95, ctx.n[:, 2]))
    var = 0.85 + 0.3 * C.value_noise(ctx.p, 60.0, 5)[:, None]
    return mix(HAIR * var, HAIR_SHEEN, sheen * 0.5)


def hair_disp(ctx):
    # Strand grooves running from the hairline back to the bun (planes of constant x), broken up a little.
    w = ctx.p[:, 0] * 520 + 3 * C.value_noise(ctx.p, 25.0, 9)
    return 0.0011 * (np.abs(np.sin(w)) - 0.6)


HAIR_STYLE = style(hair_color, rough=0.42, disp_fn=hair_disp, grad=(1.1, 1.0, 0.7))


def strand_disp(ctx):
    if ctx.t is None:
        return hair_disp(ctx)
    a = S.angle_around(ctx, UPV if abs(ctx.part.axis[-1][2] - ctx.part.axis[0][2]) < 0.05 else XAX)
    return 0.0012 * (np.abs(np.sin(a * 9 + 2 * C.value_noise(ctx.p, 30.0, 4))) - 0.6)


STRAND_STYLE = style(hair_color, rough=0.42, disp_fn=strand_disp, grad=(1.1, 1.0, 0.7))


def coat_color(ctx):
    col = np.broadcast_to(COAT, (len(ctx.p), 3)).copy()
    return mix(col, COAT_SHADE, C.smoothstep(-0.1, -0.9, ctx.n[:, 2]) * 0.4)


def coat_disp(ctx):
    # Soft vertical folds on the coat front and sides.
    return 0.0018 * np.sin(ctx.p[:, 0] * 55 + 4 * C.value_noise(ctx.p, 6.0, 2)) * C.smoothstep(1.24, 1.05, ctx.p[:, 2])


def chest_color(ctx):
    col = coat_color(ctx)
    # The navy undersuit shows in a V between the lapels.
    half = 0.02 + np.clip(1.25 - ctx.p[:, 2], 0, None) * 0.28
    v = (1 - C.smoothstep(half - 0.004, half, np.abs(ctx.p[:, 0]))) * C.smoothstep(0.02, 0.06, -ctx.p[:, 1])
    col = mix(col, NAVY, v)
    edge = (1 - C.smoothstep(0.0, 0.006, np.abs(np.abs(ctx.p[:, 0]) - half))) * C.smoothstep(0.02, 0.06, -ctx.p[:, 1])
    return mix(col, COAT_SHADE * 0.7, edge * 0.6)


def shoulder_color(ctx):
    col = coat_color(ctx)
    # The colony-blue yoke stripe over the top of each shoulder.
    yoke = C.smoothstep(0.62, 0.72, ctx.n[:, 2]) * (1 - C.smoothstep(0.9, 0.97, ctx.n[:, 2]))
    col = mix(col, COLONY, yoke)
    # The NTI patch on the left shoulder: a navy disc with a gold nova point.
    patch = near(ctx, G(0.175, 1.15, 0.035), 0.022, 0.15)
    col = mix(col, NAVY, patch)
    return mix(col, NOVA, near(ctx, G(0.175, 1.153, 0.038), 0.008, 0.4))


def shoulder_emit(ctx):
    return NOVA[None, :] * near(ctx, G(0.175, 1.153, 0.038), 0.008, 0.4)[:, None] * 0.7


COAT_FAM = fam(weave=1.0)
CHEST_STYLE = style(chest_color, rough=0.78, family=COAT_FAM, disp_fn=coat_disp)
COAT_STYLE = style(coat_color, rough=0.78, family=COAT_FAM, disp_fn=coat_disp)
SHOULDER_STYLE = style(shoulder_color, rough=0.78, family=COAT_FAM, disp_fn=coat_disp, emit_fn=shoulder_emit)


def collar_seam_emit(ctx):
    rim = 1 - C.smoothstep(0.0, 0.005, np.abs(ctx.p[:, 2] - 1.306))
    return CYAN[None, :] * rim[:, None]


UNDERSUIT_STYLE = style(lambda ctx: mix(NAVY, CYAN * 0.55, 1 - C.smoothstep(0.0, 0.005, np.abs(ctx.p[:, 2] - 1.306))), rough=0.6,
                        family=fam(weave=1.0), emit_fn=collar_seam_emit)


def metal_style(glow_mask=None):
    def color(ctx):
        col = np.broadcast_to(np.array((0.22, 0.24, 0.28)), (len(ctx.p), 3)).copy()
        return mix(col, CYAN * 0.6, glow_mask(ctx)) if glow_mask else col

    def emit(ctx):
        return CYAN[None, :] * glow_mask(ctx)[:, None] if glow_mask else 0.0
    return style(color, rough=0.32, family=fam(metal=1.0), emit_fn=emit if glow_mask else None)


def card_color(ctx):
    col = np.broadcast_to(YELLOW, (len(ctx.p), 3)).copy()
    photo = (np.abs(ctx.u[:, 0] + 0.25) < 0.35) * (np.abs(ctx.u[:, 2] - 0.15) < 0.4) * (ctx.u[:, 1] < -0.3)
    return mix(col, np.array((0.15, 0.2, 0.3)), photo.astype(float))


# -------------------------------------------------------------------------------------------------------------
# Parts
# -------------------------------------------------------------------------------------------------------------

def cap_slab(b, name, centre, r_out, r_in, el0, el1, az, bone, style_fn, nu=16, nv=6):
    """A spherical shell band round `centre`: elevation el0..el1 (radians, 0 = the eye's centre line), azimuth
    -az..az round game forward. Upper and lower eyelids."""
    c = Vector(centre)

    def at(r):
        def fn(u, v):
            a = -az + 2 * az * u
            e = el0 + (el1 - el0) * v
            d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
            return c + d * r
        return fn
    b.slab(name, at(r_out), at(r_in), nu, nv, bone=bone, style=style_fn)


def build_parts():
    b = C.PartBuilder()
    # --- Torso and coat ------------------------------------------------------------------------------
    def flat_bottom(v):
        return Vector((v.x, v.y, max(v.z, -0.55)))
    b.ellipsoid("Chest", G(0, 1.1, -0.01), R(0.148, 0.15, 0.088), bone="Chest", style=CHEST_STYLE, segments=(40, 20), deform=flat_bottom)
    b.parts[-1].chain = ["Chest", "Spine"]
    # One smooth shoulder line across the back, so the shoulders slope instead of reading as two balls.
    b.ellipsoid("Shoulders", G(0, 1.2, -0.016), R(0.178, 0.058, 0.078), bone="Chest", style=SHOULDER_STYLE, segments=(36, 14))
    for side, sx in (("L", 1), ("R", -1)):
        b.tube("Arm", [G(0.158 * sx, 1.2, -0.014), G(0.172 * sx, 1.1, -0.01), G(0.18 * sx, 0.98, -0.004)], [0.05, 0.05, 0.048], sides=16,
               chain=[f"Clavicle_{side}"], style=SHOULDER_STYLE, cap=0.2)
        b.tube("Lapel", [G(0.034 * sx, 1.245, 0.05), G(0.054 * sx, 1.17, 0.07), G(0.07 * sx, 1.05, 0.078)], [0.008, 0.009, 0.007],
               sides=8, chain=["Chest"], style=COAT_STYLE, cap=0.4)

    def collar_at(r_add):
        def fn(u, v):
            a = math.radians(58 + (360 - 116) * u)
            z = 1.228 + 0.075 * v
            r = 0.06 + 0.014 * v + r_add
            return G(math.sin(a) * r, z, -math.cos(a) * r - 0.012)
        return fn
    b.slab("Collar", collar_at(0.0), collar_at(-0.008), 20, 3, bone="Chest", style=COAT_STYLE)
    b.parts[-1].chain = ["Chest", "Neck"]
    b.tube("Undersuit", [G(0, 1.225, -0.01), G(0, 1.27, -0.007), G(0, 1.31, -0.004)], [0.047, 0.043, 0.04], sides=18, chain=["Neck", "Chest"],
           style=UNDERSUIT_STYLE, cap=0.2)
    b.ellipsoid("Card", G(0.072, 1.1, 0.078), R(0.019, 0.026, 0.0045), rot=Matrix.Rotation(math.radians(-8), 3, "X"), bone="Chest",
                style=style(card_color, rough=0.4, family=fam(ceramic=1.0)),
                deform=lambda v: Vector(tuple(math.copysign(abs(c) ** 0.35, c) for c in v)), segments=(12, 8))
    b.ellipsoid("CardClip", G(0.072, 1.129, 0.08), R(0.007, 0.005, 0.005), bone="Chest", style=metal_style(), segments=(8, 6))
    # --- Neck and head ---------------------------------------------------------------------------------
    b.tube("Neck", [G(0, 1.26, -0.012), G(0, 1.32, -0.006), G(0, 1.385, 0.0)], [0.035, 0.034, 0.036], sides=14, chain=["Neck"], style=SKIN_STYLE)

    def head_shape(v):
        down = max(0.0, -v.z)
        x = v.x * (1 - 0.34 * down ** 1.6)
        y = v.y
        if y < -0.55:   # a flatter face plane
            y = -0.55 - (abs(y) - 0.55) * 0.55
        y *= (1 - 0.06 * down)
        return Vector((x, y, v.z))
    b.ellipsoid("Head", HEAD_C, R(0.074, 0.108, 0.092), bone="Head", style=SKIN_STYLE, segments=(48, 28), deform=head_shape)
    for sx in (1, -1):
        b.ellipsoid("Ear", G(0.072 * sx, 1.458, -0.008), R(0.01, 0.024, 0.017), bone="Head", style=SKIN_STYLE, segments=(12, 8))
        b.ellipsoid("Brow", G(0.031 * sx, 1.49, 0.07), R(0.022, 0.0055, 0.009), rot=Matrix.Rotation(math.radians(-10 * sx), 3, "Y"), bone="Head",
                    style=style(lambda ctx: mix(SKIN, HAIR * 0.8, C.smoothstep(-0.2, 0.3, ctx.u[:, 2]) * C.smoothstep(0.0, 0.5, -ctx.u[:, 1])),
                                rough=0.5), segments=(14, 8))
    b.ellipsoid("Chin", G(0, 1.38, 0.048), R(0.026, 0.022, 0.026), bone="Jaw", style=SKIN_STYLE, segments=(16, 10))
    b.parts[-1].chain = ["Jaw", "Head"]
    b.tube("Nose", [G(0, 1.466, 0.068), G(0, 1.448, 0.08), G(0, 1.432, 0.088)], [0.0075, 0.0085, 0.0098], sides=10, chain=["Head"],
           style=SKIN_STYLE, cap=0.6)
    b.ellipsoid("Nostrils", G(0, 1.429, 0.082), R(0.013, 0.0065, 0.008), bone="Head", style=SKIN_STYLE, segments=(14, 8))
    b.ellipsoid("LipUpper", G(0, MOUTH_Z + 0.0052, 0.0705), R(0.018, 0.0048, 0.0075), bone="Head", style=LIP_STYLE, segments=(16, 8))
    b.ellipsoid("LipLower", G(0, MOUTH_Z - 0.0058, 0.0685), R(0.016, 0.0055, 0.0075), bone="Jaw", style=LIP_STYLE, segments=(16, 8))
    for side, c in EYES.items():
        b.ellipsoid("Eye", c, (EYE_R, EYE_R, EYE_R), bone="Head", style=eye_style(c), segments=(20, 12))
        cap_slab(b, "Lid", c, EYE_R + 0.0024, EYE_R - 0.004, LID_EDGE, math.radians(80), math.radians(74), f"Lid_{side}",
                 style(lambda ctx, c=c: lid_color(ctx, c, True), rough=0.45))
        cap_slab(b, "LowerLid", c, EYE_R + 0.0016, EYE_R - 0.004, math.radians(-74), LOWER_EDGE, math.radians(68), "Head",
                 style(lambda ctx, c=c: lid_color(ctx, c, False), rough=0.45))
    # --- Hair -----------------------------------------------------------------------------------------
    def hair_cap(v):
        # Pull the front-lower part of the cap into the head, leaving a hairline over the forehead and temples.
        front = max(0.0, -v.y)
        low = max(0.0, 0.45 - v.z)
        k = min(1.0, front * 1.6) * min(1.0, low * 2.2)
        return v * (1 - 0.3 * k)
    b.ellipsoid("Hair", G(0, 1.49, -0.012), R(0.083, 0.104, 0.1), bone="Head", style=HAIR_STYLE, segments=(40, 22), deform=hair_cap)
    b.tube("Fringe", [G(-0.05, 1.57, 0.045), G(-0.01, 1.568, 0.076), G(0.04, 1.545, 0.078), G(0.074, 1.5, 0.054)],
           [0.015, 0.017, 0.014, 0.008], sides=12, chain=["Head"], style=STRAND_STYLE, cap=0.5)
    for sx in (1, -1):
        b.tube("Lock", [G(0.068 * sx, 1.53, 0.032), G(0.078 * sx, 1.475, 0.016), G(0.074 * sx, 1.425, -0.004)], [0.013, 0.012, 0.007],
               sides=10, chain=["Head"], style=STRAND_STYLE, cap=0.5)
    b.ellipsoid("Bun", G(0, 1.525, -0.104), R(0.042, 0.04, 0.033), bone="Head", style=HAIR_STYLE, segments=(20, 12))
    b.tube("BunTwist", [G(-0.036, 1.512, -0.112), G(0, 1.552, -0.126), G(0.036, 1.512, -0.112)], [0.011, 0.013, 0.011], sides=8, chain=["Head"],
           style=STRAND_STYLE, cap=0.5)
    b.tube("Pin", [G(-0.055, 1.538, -0.094), G(0.06, 1.522, -0.108)], [0.0032, 0.0032], sides=6, chain=["Head"],
           style=metal_style(lambda ctx: (ctx.t > 0.92).astype(float)), cap=0.8)
    # --- Headset: band over the crown, an earpiece with a lit ring on her right ear, a mic boom ---------
    b.tube("Band", [G(-0.084, 1.465, -0.004), G(-0.07, 1.555, -0.01), G(0.0, 1.598, -0.014), G(0.07, 1.555, -0.01), G(0.084, 1.465, -0.004)],
           [0.005] * 5, sides=8, chain=["Head"], style=metal_style(), cap=0.5)
    b.ellipsoid("Earpiece", G(-0.086, 1.458, -0.004), R(0.01, 0.023, 0.02), bone="Head",
                style=metal_style(lambda ctx: (1 - C.smoothstep(0.0, 0.08, np.abs(np.linalg.norm(ctx.u[:, 1:], axis=1) - 0.75))) * (ctx.u[:, 0] < -0.3)),
                segments=(16, 10))
    b.ellipsoid("EarpieceL", G(0.082, 1.458, -0.004), R(0.007, 0.014, 0.012), bone="Head", style=metal_style(), segments=(12, 8))
    b.tube("Mic", [G(-0.088, 1.444, 0.012), G(-0.076, 1.416, 0.048), G(-0.04, 1.4, 0.072)], [0.003, 0.0028, 0.0026], sides=6, chain=["Head"],
           style=metal_style(), cap=0.5)
    b.ellipsoid("MicTip", G(-0.037, 1.4, 0.074), R(0.005, 0.0045, 0.005), bone="Head",
                style=metal_style(lambda ctx: np.ones(len(ctx.p)) * 0.8), segments=(10, 6))
    return b


RIGID = {"Lid", "Eye", "Earpiece", "EarpieceL", "Mic", "MicTip", "Band", "Pin", "Card", "CardClip"}


def bones():
    return [tk.Bone("Root", (0, 0, 0), UPV, length=0.1),
            tk.Bone("Spine", G(0, 0.98, 0), UPV, "Root", length=0.1),
            tk.Bone("Chest", G(0, 1.12, 0), UPV, "Spine", length=0.1),
            tk.Bone("Clavicle_L", G(0.02, 1.24, -0.01), XAX, "Chest", length=0.12),
            tk.Bone("Clavicle_R", G(-0.02, 1.24, -0.01), -XAX, "Chest", length=0.12),
            tk.Bone("Neck", G(0, 1.26, -0.012), UPV, "Chest", length=0.08),
            tk.Bone("Head", G(0, 1.39, 0.0), UPV, "Neck", length=0.12),
            tk.Bone("Jaw", G(0, 1.43, -0.02), (G(0, 1.37, 0.06) - G(0, 1.43, -0.02)), "Head", length=0.08),
            tk.Bone("Lid_L", EYES["L"], Vector((0, -1, 0)), "Head", length=0.02),
            tk.Bone("Lid_R", EYES["R"], Vector((0, -1, 0)), "Head", length=0.02)]


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


def talk(t, f):
    spec = breath(t, 1, 0.7)
    x = t * len(SYLL)
    i = int(x) % len(SYLL)
    u = x - int(x)
    v = SYLL[i] + (SYLL[(i + 1) % len(SYLL)] - SYLL[i]) * (u * u * (3 - 2 * u))
    nod = 0.05 * math.sin(2 * math.pi * 2 * t) * (0.5 + 0.5 * math.sin(2 * math.pi * t))
    spec["Head"] = {"rot": (XAX, nod + 0.02 * v)}
    spec["Jaw"] = {"rot": (XAX, math.radians(11) * v)}
    spec.update(lids(blink_curve(f, 44)))
    return spec


def blink(t, f):
    return lids(blink_curve(f, 0, 8))


CLIPS = {"Idle": (120, idle, True), "Talk": (90, talk, True), "Blink": (8, blink, False)}


# -------------------------------------------------------------------------------------------------------------
# Build
# -------------------------------------------------------------------------------------------------------------

def skin_mesh(obj, arm, builder):
    C.skin(obj, arm, builder, rigid=RIGID, blend=1)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=float(args.get("voxel", 0.0021)), target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.04,
                               extrusion=0.006, ray=0.02, emission_strength=2.2, symmetric=False, specular=0.4, mirror=False, k=0.6,
                               families=FAMILIES, family_set=S.FAMILY_SET, low_size=LOW_TEX, preview_detail=(0.5, 0.5), edge_gain=0.2,
                               border_smooth=2)
    arm = tk.build_armature(NAME + "_Rig", bones())
    skin_mesh(asset.low, arm, builder)
    asset.low.parent = arm
    mod = asset.low.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    acts = {n: bk.clip(arm, n, fr, fn, loop=lp) for n, (fr, fn, lp) in CLIPS.items()}
    bk.rest_pose(arm)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    C.write_report(NAME, {
        "asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris, "lod1": "none: portrait and cutscene only",
        "bones": [b.name for b in arm.data.bones], "texture_size": TEX, "low_texture_size": LOW_TEX,
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
        "clips": {n: fr for n, (fr, fn, lp) in CLIPS.items()}, "events": {"Idle": {"blink": 70}, "Talk": {"blink": 44}},
        "blink": "Lid_L/Lid_R rotate about +X by %.0f degrees to close" % math.degrees(BLINK),
        "portrait": {"target": [0, 1.36, 0.0], "distance": 1.45, "azimuth": -28, "elevation": 5, "lens_mm": 70,
                     "note": "game space (x, up, forward); same camera as mercer_bust.png, aimed 3 cm higher"}})
    C.save_blend(NAME)
    if not args.get("norender"):
        previews(arm, asset, acts, out)


def previews(arm, asset, acts, out):
    rig = sk.portrait_lights(0.6)
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    cam = sk.portrait_camera(G(0, 1.36, 0.0))
    C.render(cam, os.path.join(out, "voss_portrait.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.44, 0.03), 0.62, azimuth=-25, elevation=4, lens=70)
    C.render(cam, os.path.join(out, "voss_face.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.4, 0.0), 0.9, azimuth=-80, elevation=4, lens=70)
    C.render(cam, os.path.join(out, "voss_profile.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.4, -0.02), 1.0, azimuth=150, elevation=10, lens=70)
    C.render(cam, os.path.join(out, "voss_back.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.25, 0.0), 1.9, azimuth=-20, elevation=10, lens=60)
    C.render(cam, os.path.join(out, "voss_bust_full.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(G(0, 1.43, 0.03), 0.55, azimuth=-15, elevation=2, lens=70)
    sk.strip(arm, acts["Talk"], cam, (0, 5, 10, 14, 22, 44), os.path.join(out, "voss_talk_strip.png"), width=400, height=400)
    sk.strip(arm, acts["Blink"], cam, (0, 2, 4, 6, 8), os.path.join(out, "voss_blink_strip.png"), width=400, height=400)
    sk.strip(arm, acts["Idle"], C.closeup_camera(G(0, 1.3, 0.0), 1.3, azimuth=-60, elevation=3, lens=60), (0, 15, 30, 72, 90),
             os.path.join(out, "voss_idle_strip.png"), width=400, height=400)
    arm.animation_data.action = None
    bk.rest_pose(arm)
    cam = C.macro_camera(G(0.0, 1.47, 0.07), 0.22, -20, 5, lens=70)
    C.render(cam, os.path.join(out, "voss_macro_eyes.png"), 1280, 720)
    bk.remove([cam])
    cam = C.macro_camera(G(-0.08, 1.2, 0.05), 0.4, -40, 15, lens=60)
    C.render(cam, os.path.join(out, "voss_macro_coat.png"), 1280, 720)
    bk.remove([cam] + rig)
    C.uv_layout(asset.low, os.path.join(out, "voss_uv.png"), 1024, asset.textures["albedo"])


if __name__ == "__main__":
    main()
