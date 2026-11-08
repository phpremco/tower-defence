"""Spencer, pass 3 (Remco 2026-11-08 on pass 2: "ik vind dat gele ding op de helm niet mooi, ik zou hier eerder
accessoires op verwachten zoals nachtvisier of een camera ofzo, of coole patronen. die vlag bij zijn kruis is best wel
japans achtig ... het teken staat nergens voor. dit zou dan een logo moeten zijn van het ruimtebedrijf").

Overrides build_mercer3.py's export on the SAME rig, bone names, socket names, all clip names (incl. Draw/Stow and the
reaction clips) and export names (ArtSource/Exports/Mercer/Mercer.fbx, Mercer_<Map>.png, Mercer_report.json).

Changes against pass 2 (everything else, incl. the pauldrons, sheath, battle wear and stance, is build_mercer3.py):
- Helmet: the yellow crest fin, its light line and the swept ear vanes are gone. Functional kit instead: a flip-up
  night-vision module on a brow mount (three lens tubes with lit cyan objectives, stowed up so the visor stays free and
  the lenses read as cyan dots from the high game camera), a helmet camera on a side rail (lit lens, yellow tally
  light), a comm unit with a stub antenna on the left ear, a battery / IR-strobe counterweight at the back, accessory
  rails on both sides. Surface: two-tone plates (white front, cobalt back shell split by a panel line), a subtle hex
  pattern on the white plates, and the unit stencil "PF-07" (Pathfinder, unit 7) on the back shell.
- Hex pattern (a quiet two-tone honeycomb tint + fine hex panel lines) also on the pauldrons, lames and chest plate.
- Tabard: shorter, squared tactical flaps (front and back) with gunmetal binding, a yellow / dark hazard band at the hem,
  clipped to the belt, and the NTI logo (build_brand.py, Exports/Brand/NTI_Logo.png) instead of the chevron.
- A raised NTI logo patch on the big left pauldron.

Run: blender --background --factory-startup --python Tools/Blender/build_mercer4.py --
     [--preview <dir>] [--norender] [--fast] [--stage <dir>] [--prefix pass3] [--logo <png>]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import build_brand as BR  # noqa: E402
import build_mercer as BM  # noqa: E402
import build_mercer2 as M2  # noqa: E402
import build_mercer3 as M3  # noqa: E402
import build_mercer_weapons as W  # noqa: E402
import common as C  # noqa: E402
import suit as S  # noqa: E402
from build_mercer import SIDE, R, dome_patch, euler, frame, nrm  # noqa: E402
from build_mercer2 import CYAN, CYAN_SOFT, DARK, DEEP, GROOVE, YELLOW_CLEAN, YELLOW_GLOW, hazard, metal, plate, ribs  # noqa: E402
from build_mercer3 import HELM_C, HELM_R, LIGHT, SCAR, TABARD_TOP, YELLOW_PAINT, claw, light  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
ROUND = "spencer-pass3-2026-11-08 (build_mercer4.py)"
LOGO = BR.LOGO

WHITE_STENCIL = dict(paint=S.PAL.plate_top, wear=0.35, rough=0.5)
COBALT_PAINT = dict(paint=S.PAL.bplate_top, wear=0.12, rough=0.42)
GUNMETAL = dict(paint=S.PAL.metal_top, wear=0.0, rough=0.55)
HAZARD_DARK = dict(paint=S.PAL.metal_under, wear=0.0, rough=0.7)
HAZARD_YELLOW = dict(paint=S.PAL.yellow, wear=0.25, rough=0.7)
TALLY_LIGHT = dict(emit=S.PAL.yellow, glow=1.6, rough=0.3, family={})


# ------------------------------------------------------------------------------------------------------------
# Patterns: a honeycomb tint and hex panel lines (triplanar), and the logo as paint
# ------------------------------------------------------------------------------------------------------------

SQ3 = math.sqrt(3.0)


def hex_cells(p, n, size):
    """(cell hash 0..1, distance to the cell edge) of a pointy-top hex grid projected along each point's dominant
    normal axis."""
    ax = np.abs(n).argmax(axis=1)
    a = np.where(ax == 0, p[:, 1], p[:, 0])
    b = np.where(ax == 2, p[:, 1], p[:, 2])
    q = (SQ3 / 3 * a - b / 3) / size
    r = (2 / 3 * b) / size
    x, z = q, r
    y = -x - z
    rx, ry, rz = np.round(x), np.round(y), np.round(z)
    dx, dy, dz = np.abs(rx - x), np.abs(ry - y), np.abs(rz - z)
    fix_x = (dx > dy) & (dx > dz)
    fix_y = ~fix_x & (dy > dz)
    rx = np.where(fix_x, -ry - rz, rx)
    rz = np.where(~fix_x & ~fix_y, -rx - ry, rz)
    h = np.sin(rx * 12.9898 + rz * 78.233 + ax * 37.719) * 43758.5453
    h = h - np.floor(h)
    cx = size * SQ3 * (rx + rz / 2)
    cy = size * 1.5 * rz
    ddx, ddy = a - cx, b - cy
    dist = np.maximum(np.abs(ddx), np.maximum(np.abs(0.5 * ddx + 0.866 * ddy), np.abs(-0.5 * ddx + 0.866 * ddy)))
    return h, size * SQ3 / 2 - dist


def hex_tint(size=0.013, share=0.42):
    def mask(c):
        h, edge = hex_cells(c.p, c.n, size)
        return (h < share) * C.smoothstep(0.0006, 0.0014, edge)
    return mask


def hex_lines(size=0.013):
    def mask(c):
        _, edge = hex_cells(c.p, c.n, size)
        return 1 - C.smoothstep(0.0, 0.0007, edge)
    return mask


def hex_features(white=True, size=0.013):
    shade = S.PAL.plate_side * 0.94 if white else S.PAL.bplate_side * 0.82
    return [(hex_tint(size), dict(paint=shade, paint_amount=0.3 if white else 0.4, wear=0.0)), (hex_lines(size), dict(groove=0.0004))]


def logo_features(origin, right, up, size, path=LOGO, wear=0.12, glow=0.0, rough=0.55):
    """The logo as paint layers (one per palette colour); `glow` lights its star and cyan parts."""
    out = []
    for name in BR.NAMES:
        fx = dict(paint=np.array(BR.PALETTE[name]), wear=wear, rough=rough)
        if glow and name in ("star", "cyan"):
            fx.update(emit=np.array(BR.PALETTE[name]), glow=glow)
        out.append((BR.decal(origin, right, up, size, name, path), fx))
    return out


def facing_frame(normal, up_hint=(0, 0, 1)):
    """(right, up) of a decal whose viewer looks along -normal, its up as close to `up_hint` as possible."""
    n = Vector(normal).normalized()
    up = Vector(up_hint)
    up = (up - n * up.dot(n)).normalized()
    right = up.cross(n)
    return right, up


# ------------------------------------------------------------------------------------------------------------
# Head: helmet with NVG, camera, comm unit, rails, counterweight
# ------------------------------------------------------------------------------------------------------------

def helm_point(e_deg, out=0.0, x=0.0):
    """A point `out` above the helmet's mid-plane surface at elevation `e` (0 front, 90 top, 180 back), shifted by
    `x`, and the outward normal there."""
    e = math.radians(e_deg)
    up, fwd = math.sin(e), math.cos(e)
    n = nrm(G(0, up / HELM_R[2], fwd / HELM_R[1]))
    return HELM_C + G(x, HELM_R[2] * up, HELM_R[1] * fwd) + n * out, n


NVG_MOUNT_E = 47.0
NVG_LENS_DIR = nrm(G(0, 0.78, 0.62))      # stowed up: the objectives look up and forward
NVG_LENSES = ((-0.034, 0.0135, 0.036), (0.0, 0.0175, 0.044), (0.034, 0.0135, 0.036))   # x, radius, length


def helmet_style():
    back_shell = lambda c: C.smoothstep(0.02, 0.1, c.u[:, 1]) * C.smoothstep(-0.55, -0.35, c.u[:, 2])  # noqa: E731
    white = lambda c: 1 - back_shell(c)  # noqa: E731
    sp, sn = helm_point(142.0, 0.004)
    right, up = facing_frame(sn)
    feats = [(lambda c: S.line(np.abs(c.u[:, 0]), 0.36, 0.02) * C.smoothstep(0.2, 0.5, c.u[:, 2]) * (c.u[:, 1] < 0.05), GROOVE),
             (lambda c: S.line(c.u[:, 2], -0.25, 0.025) * C.smoothstep(-0.2, 0.4, c.u[:, 1]), GROOVE),
             (back_shell, COBALT_PAINT),
             (lambda c: S.line(c.u[:, 1], 0.06, 0.02) * C.smoothstep(-0.5, -0.3, c.u[:, 2]), DEEP),
             (lambda c: ribs(c.u[:, 0], 0.1, 0.3) * S.band(c.u[:, 2], -0.48, 0.07) * (c.u[:, 1] > 0.5), DEEP)]
    feats += [(lambda c, m=m: m(c) * white(c), fx) for m, fx in hex_features(True)]
    feats += [(BR.decal(sp, right, up, 0.074, "white", BR.STENCIL, height=0.0247), WHITE_STENCIL),
              (claw(HELM_C + G(0.05, 0.12, 0.05), G(0.5, -0.45, -0.6), G(0.6, 0.8, 0.0), 0.13), SCAR)]
    return plate(feats, seed=4)


def nvg(b):
    mount, n = helm_point(NVG_MOUNT_E, 0.012)
    b.ellipsoid("NvgMount", mount, R(0.034, 0.022, 0.016), frame(n.cross(Vector((1, 0, 0))), n), bone="Head", deform=boxy_(0.45),
                segments=(18, 10), style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.08), GROOVE),
                                                (lambda c: hazard(c, 0.012, (1, 0, 1)) * (c.u[:, 2] > 0.3), YELLOW_PAINT)], seed=80))
    body = mount + n * 0.016 + NVG_LENS_DIR * 0.004
    b.tube("NvgArm", [mount, mount + n * 0.014, body], [0.009, 0.008, 0.008], sides=10, chain=["Head"], cap=0.4, style=metal(seed=81))
    b.ellipsoid("NvgBody", body, R(0.066, 0.024, 0.026), frame(NVG_LENS_DIR, -n), bone="Head", deform=boxy_(0.5), segments=(24, 12),
                style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.06) + S.line(np.abs(c.u[:, 0]), 0.6, 0.05), GROOVE),
                             (lambda c: S.band(c.u[:, 0], 0.0, 0.25) * (c.u[:, 2] > 0.5), DARK)], seed=82))
    side = Vector((1, 0, 0))
    for x, r, length in NVG_LENSES:
        root = body + side * x
        tip = root + NVG_LENS_DIR * length
        b.tube("NvgLens", [root, root.lerp(tip, 0.6), tip], [r, r, r * 1.08], sides=16, chain=["Head"], cap=0.15,
               style=metal([(lambda c: ribs(c.t, 0.18, 0.2) * (c.t < 0.7), GROOVE), (lambda c: S.band(c.t, 0.86, 0.05), YELLOW_CLEAN)],
                           seed=83))
        b.ellipsoid("NvgGlow", tip + NVG_LENS_DIR * 0.001, (r * 0.8, 0.0035, r * 0.8), frame(NVG_LENS_DIR, side), bone="Head",
                    style=light(2.2), segments=(16, 8))


def boxy_(k):
    return BM.boxy(k)


def helmet_kit(b):
    # Accessory rails on both sides, ribbed.
    for name, sd in SIDE.items():
        sx = sd.sx
        b.ellipsoid("Rail", HELM_C + G(0.127 * sx, 0.052, 0.006), R(0.008, 0.011, 0.074), bone="Head", deform=boxy_(0.8), segments=(12, 16),
                    style=metal([(lambda c: ribs(c.p[:, 1], 0.011, 0.3) * (np.abs(c.u[:, 1]) < 0.85), GROOVE)], seed=84))
    # Helmet camera on the right rail (figure's right = -x): body, lens barrel, lit lens, yellow tally light.
    cam = HELM_C + G(-0.146, 0.064, 0.018)
    b.ellipsoid("HelmCam", cam, R(0.018, 0.021, 0.042), bone="Head", deform=boxy_(0.55), segments=(16, 14),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.25, 0.06), GROOVE), (lambda c: S.band(c.u[:, 0], 0.0, 0.7) * (c.u[:, 2] > 0.7),
                                                                                    YELLOW_CLEAN)], seed=85))
    b.tube("CamLens", [cam + G(0, -0.002, 0.034), cam + G(0, -0.002, 0.058)], [0.0125, 0.0135], sides=16, chain=["Head"], cap=0.15,
           style=metal([(lambda c: ribs(c.t, 0.3, 0.2), GROOVE)], seed=86))
    b.ellipsoid("CamGlow", cam + G(0, -0.002, 0.059), R(0.0095, 0.0095, 0.0025), bone="Head", style=light(2.0), segments=(14, 8))
    b.ellipsoid("CamTally", cam + G(-0.004, 0.022, -0.026), R(0.0045, 0.0035, 0.0045), bone="Head", style=S.light(S.PAL.yellow, 1.8),
                segments=(10, 6))
    # Comm unit on the left ear pod with a stub antenna sweeping back, a speaker grille and two status lights.
    comm = HELM_C + G(0.163, -0.012, -0.048)
    b.ellipsoid("CommUnit", comm, R(0.019, 0.05, 0.056), bone="Head", deform=boxy_(0.5), segments=(14, 16),
                style=metal([(lambda c: ribs(c.u[:, 2], 0.16, 0.25) * S.band(c.u[:, 1], 0.1, 0.45) * (c.u[:, 0] > 0.5), GROOVE),
                             (lambda c: S.band(c.u[:, 1], -0.55, 0.1) * S.band(c.u[:, 2], 0.55, 0.12) * (c.u[:, 0] > 0.4), CYAN),
                             (lambda c: S.band(c.u[:, 1], -0.3, 0.1) * S.band(c.u[:, 2], 0.55, 0.12) * (c.u[:, 0] > 0.4), YELLOW_GLOW),
                             (lambda c: S.band(c.u[:, 2], -0.8, 0.12), YELLOW_CLEAN)], seed=87))
    a0 = comm + G(0.004, 0.04, -0.03)
    b.tube("CommAntenna", [a0, a0 + G(0.006, 0.05, -0.03), a0 + G(0.01, 0.1, -0.075)], [0.0065, 0.0045, 0.003], sides=8, chain=["Head"],
           cap=0.8, style=metal([(lambda c: S.band(c.t, 0.97, 0.04), YELLOW_CLEAN)], seed=88))
    # Lamp in front of the comm unit (pass 1 position).
    b.tube("HelmLamp", [HELM_C + G(0.142, 0.01, 0.0), HELM_C + G(0.144, 0.012, 0.06)], [0.019, 0.021], sides=14, chain=["Head"], cap=0.3,
           style=metal([(lambda c: S.band(c.t, 1.0, 0.1), dict(emit=np.array((1.0, 0.93, 0.72)), glow=1.4, rough=0.2, family={})),
                        (lambda c: S.band(c.t, 0.2, 0.08), YELLOW_CLEAN)], seed=37))
    # Counterweight / battery with an IR strobe at the back (balances the NVG; a cyan blink light).
    pack, n = helm_point(178.0, 0.016, 0.0)
    pack = pack + G(0, -0.03, 0)
    b.ellipsoid("HelmPack", pack, R(0.05, 0.032, 0.02), bone="Head", deform=boxy_(0.55), segments=(18, 12),
                style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.05) + S.line(c.u[:, 2], 0.0, 0.05), GROOVE),
                             (lambda c: S.band(c.u[:, 0], 0.55, 0.2) * S.band(c.u[:, 2], 0.0, 0.35) * (c.u[:, 1] > 0.5), CYAN),
                             (lambda c: hazard(c, 0.01, (1, 0, 1)) * S.band(c.u[:, 0], -0.55, 0.3) * (c.u[:, 1] > 0.3), YELLOW_PAINT)],
                            seed=89))


def head(b):
    b.ellipsoid("Helmet", HELM_C, HELM_R, bone="Head", segments=(48, 26), deform=boxy_(0.74), style=helmet_style())
    # Brow plate over the visor (yellow glints at its corners as in pass 1/2).
    dome_patch(b, "Brow", HELM_C, HELM_R, None, lambda a, v: (a * 0.92, -1.0, 0.4 + 0.62 * (v + 1) / 2), 0.016, "Head",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), GROOVE),
                      (lambda c: S.band(np.abs(c.u[:, 0]), 0.42, 0.06) * S.band(c.u[:, 2], 0.42, 0.06), YELLOW_GLOW)], seed=32),
               nu=22, nv=10, outline=lambda a, v: (a * (1.0 - 0.3 * (v + 1) / 2), v))
    for name, sd in SIDE.items():
        sx = sd.sx
        dome_patch(b, "Cheek", HELM_C, HELM_R, None,
                   lambda a, v, sx=sx: (sx * (1.0 - 0.55 * (a + 1) / 2), -(0.05 + 0.95 * (a + 1) / 2), -0.15 - 0.8 * (v + 1) / 2),
                   0.016, "Head", M2.bplate([(lambda c: S.line(c.u[:, 2], -0.72, 0.03), GROOVE)] + hex_features(False), seed=33 + (sx > 0)),
                   nu=16, nv=10)
        b.tube("Ear", [HELM_C + G(0.112 * sx, -0.02, -0.005), HELM_C + G(0.148 * sx, -0.02, -0.005)], [0.044, 0.038], sides=18,
               chain=["Head"], style=metal([(lambda c: S.band(c.t, 0.95, 0.12), GROOVE),
                                            (lambda c: S.band(c.t, 1.0, 0.06) * C.smoothstep(0.3, 0.8, np.abs(c.n[:, 0])), CYAN_SOFT)],
                                           seed=6), cap=0.35)

    def chevron_slit(c):
        x = np.abs(c.u[:, 0])
        return S.band(c.u[:, 2], -0.02 + 0.5 * x, 0.15) * C.smoothstep(0.3, 0.75, -c.u[:, 1]) * (1 - C.smoothstep(0.66, 0.82, x))
    b.ellipsoid("Visor", G(0, 1.455, 0.02), R(0.126, 0.06, 0.13), bone="Head", segments=(48, 24), style=S.glass(chevron_slit, glow=0.14))
    b.ellipsoid("Chin", G(0, 1.378, 0.072), R(0.062, 0.036, 0.052), euler(pitch=14), bone="Head", deform=boxy_(0.55), segments=(28, 14),
                style=metal([(lambda c: ribs(c.u[:, 0], 0.22, 0.25) * (c.u[:, 1] < -0.3) * S.band(c.u[:, 2], 0.0, 0.5), DEEP),
                             (lambda c: S.band(c.u[:, 2], 0.75, 0.2), YELLOW_CLEAN)], seed=5))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Filter", [G(0.05 * sx, 1.372, 0.084), G(0.086 * sx, 1.356, 0.052)], [0.021, 0.019], sides=14, chain=["Head"], cap=0.5,
               style=metal([(lambda c: ribs(c.t, 0.25, 0.2), GROOVE), (lambda c: S.band(c.t, 0.92, 0.1), YELLOW_CLEAN)], seed=34))
    nvg(b)
    helmet_kit(b)


# ------------------------------------------------------------------------------------------------------------
# Tactical tabard with the logo
# ------------------------------------------------------------------------------------------------------------

def tabard(b, z_front, width_top, width_bottom, length, bulge, seed, logo_size):
    """A squared cloth flap clipped to the belt: gunmetal binding, a hazard band at the hem, the NTI logo."""
    sgn = 1.0 if z_front > 0 else -1.0

    def hem(u):
        corner = max(0.0, abs(2 * u - 1) - 0.8) / 0.2
        return length * (1 - 0.1 * corner)

    def surf(side):
        def fn(u, v):
            w = width_top + (width_bottom - width_top) * v
            y = TABARD_TOP - hem(u) * v
            z = z_front + sgn * (bulge * math.sin(math.pi * min(1.0, v * 1.6)) - 0.012 * v) + 0.0035 * side
            return G((u - 0.5) * w, y, z)
        return fn

    bottom = TABARD_TOP - length
    binding = lambda c: C.smoothstep(width_top * 0.43, width_top * 0.46, np.abs(c.p[:, 0]))  # noqa: E731
    band = lambda c: S.band(c.p[:, 2], bottom + 0.034, 0.016, soft=0.15)  # noqa: E731
    centre_y = TABARD_TOP - 0.03 - logo_size / 2
    bulge_at = bulge * math.sin(math.pi * min(1.0, (TABARD_TOP - centre_y) / length * 1.6))
    origin = G(0, centre_y, z_front + sgn * (bulge_at + 0.006))
    right, up = facing_frame(G(0, 0, sgn))
    cloth = S.styled("suit", [(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.bplate_side * 0.62, wear=0.0, rough=0.85)),
                              (lambda c: S.band(c.p[:, 2], TABARD_TOP - 0.012, 0.012), GUNMETAL),
                              (lambda c: C.smoothstep(bottom + 0.016, bottom + 0.008, c.p[:, 2]), GUNMETAL),
                              (binding, GUNMETAL),
                              (lambda c: band(c) * (1 - binding(c)), HAZARD_DARK),
                              (lambda c: band(c) * hazard(c, 0.022, (1, 0, 1)) * (1 - binding(c)), HAZARD_YELLOW),
                              (lambda c: S.line(c.p[:, 2], bottom + 0.055, 0.0012), dict(groove=0.0008))]
                     + logo_features(origin, right, up, logo_size, wear=0.08, rough=0.7), seed=seed)
    b.slab("Tabard", surf(1.0), surf(-1.0), 14, 12, chain=["Hips"], style=cloth)
    for sx in (-1, 1):
        b.ellipsoid("TabardClip", G(sx * width_top * 0.33, TABARD_TOP - 0.004, z_front + sgn * 0.008), R(0.011, 0.016, 0.007), bone="Hips",
                    deform=boxy_(0.5), segments=(12, 8), style=metal([(lambda c: S.line(c.u[:, 2], 0.0, 0.1), GROOVE)], seed=90))


# ------------------------------------------------------------------------------------------------------------
# Logo patch on the big (left) pauldron
# ------------------------------------------------------------------------------------------------------------

PATCH_DIR = (0.47, 0.0, 1.0)


def logo_patch(b):
    sd = SIDE["L"]
    sx = sd.sx
    k = 1.18
    cap_c = G(0.222 * sx, 1.252, -0.008)
    cap_r = R(0.168 * k, 0.15 * k, 0.17 * k)
    rot = euler(roll=24 * sx)
    radii = tuple(r + 0.03 for r in cap_r)
    d = nrm(PATCH_DIR)
    centre = M3.shell_point(cap_c, radii, rot, d, 0.004)
    n = nrm(rot @ Vector((d.x / radii[0] ** 2, d.y / radii[1] ** 2, d.z / radii[2] ** 2)))
    right, up = facing_frame(n)
    style = S.styled("metal", [(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.metal_under, wear=0.0, rough=0.7))]
                     + logo_features(centre, right, up, 0.066, wear=0.1, glow=0.6, rough=0.5), seed=91)
    dome_patch(b, "LogoPatch", cap_c, radii, rot, lambda a, v: (0.47 + 0.17 * a, 0.19 * v, 1.0), 0.004, None, style,
               chain=[f"Clavicle_{'L'}", "UpperArm_L"], nu=14, nv=14)


# ------------------------------------------------------------------------------------------------------------
# Parts, layers, rig sets
# ------------------------------------------------------------------------------------------------------------

def with_camo(fn, seeds, white):
    def wrapped(features=(), seed=0):
        extra = hex_features(white) if seed in seeds else []
        return fn(list(features) + extra, seed=seed)
    return wrapped


def build_parts():
    b = C.PartBuilder()
    head(b)
    keep_plate, keep_bplate = M3.plate, M3.bplate
    M3.plate = with_camo(M2.plate, {1}, True)           # chest plate
    M3.bplate = with_camo(M2.bplate, {12, 24, 25}, False)  # pauldron caps and lames
    try:
        M3.torso(b)
        M3.arms(b)
    finally:
        M3.plate, M3.bplate = keep_plate, keep_bplate
    tabard(b, 0.146, 0.15, 0.145, 0.3, 0.012, 71, 0.105)
    tabard(b, -0.155, 0.16, 0.15, 0.3, 0.014, 72, 0.1)
    M3.pack(b)
    logo_patch(b)
    M2.legs(b)
    return b


HEAD_KIT = {"NvgMount", "NvgArm", "NvgBody", "NvgLens", "NvgGlow", "Rail", "HelmCam", "CamLens", "CamGlow", "CamTally", "CommUnit",
            "CommAntenna", "HelmPack"}
RIGID = (M3.RIGID - {"Fin", "FinLight", "EarVane", "Comms"}) | HEAD_KIT | {"TabardClip"}
PLATES = M3.PLATES - {"Fin", "EarVane", "Tabard"}
GEAR = (M3.GEAR - {"FinLight", "Comms"}) | HEAD_KIT | {"TabardClip"}
INSIGNIA = {"Tabard", "LogoPatch"}
LAYERS = [S.Layer("suit", None, voxel=0.0034, smooth=6, density=0.7),
          S.Layer("plates", PLATES, voxel=0.0018, smooth=2, density=1.0),
          S.Layer("gear", GEAR, voxel=0.0019, smooth=3, density=1.0),
          S.Layer("insignia", INSIGNIA, voxel=0.0013, smooth=1, density=1.4, uv_scale=1.8),
          S.Layer("hands", {"Palm", "Knuckles", "Finger", "Thumb"}, voxel=0.0014, smooth=3, density=2.0)]
M3.RIGID = RIGID   # skin_all reads it


# ------------------------------------------------------------------------------------------------------------
# Build, export, previews
# ------------------------------------------------------------------------------------------------------------

def compare(out, prefix, before="pass2"):
    """pass 2 left, pass 3 right, per view."""
    for view, width in (("sheet", 1600), ("game", 1280), ("game_zoom", 1280), ("bust", 1024), ("front", 960), ("34", 960),
                        ("back34", 960)):
        a, b = os.path.join(out, f"{before}_{view}.png"), os.path.join(out, f"{prefix}_{view}.png")
        if os.path.exists(a) and os.path.exists(b):
            C.contact_sheet([a, b], os.path.join(out, f"{prefix}_compare_{view}.png"), 2, width)


def main():
    args = C.cli_args()
    global LOGO
    if args.get("logo"):
        LOGO = args["logo"]
    if not os.path.exists(LOGO):
        BR.main()
    fast = bool(args.get("fast"))
    C.reset_scene()
    BM.stage_exports()
    out = os.path.join(C.REPO, "ArtSource", "Previews", "Spencer") if not args.get("preview") else C.preview_dir()
    tex, low_tex = (512, None) if fast else (M3.TEX, M3.LOW_TEX)
    W.build()
    builder = build_parts()
    asset, index = S.build_layered_asset(NAME, builder, LAYERS, voxel=0.0034, target_tris=M3.GAME_TRIS, tex_size=tex, ao_distance=0.06,
                                         extrusion=0.01, ray=0.03, emission_strength=2.5, extra=M3.battle_wear(), specular=0.4,
                                         mirror=False, symmetric=False, k=1.0, families=BM.FAMILIES, family_set=S.FAMILY_SET,
                                         low_size=low_tex, preview_detail=(0.8, 0.6), edge_gain=0.5, border_smooth=4,
                                         unwrap="smart")
    arm = BM.build_armature(NAME + "_Rig", BM.bones())
    M3.skin_all(asset.low, arm, builder)
    poser = C.Poser(arm)
    clips = {n: BM.CLIPS[n] for n in (("Idle", "Walk", "Run", "Shoot", "Slash") if fast else BM.CLIPS)}
    clips.update(M3.draw_clip_fns())
    actions = C.key_clips(poser, clips)
    switch = M3.switch_check(poser)
    arm.animation_data.action = actions["Idle"]
    objs = [arm, asset.low]
    lod = None
    if not fast:
        lod = M3.build_lod(asset, arm, builder, index)
        objs.append(lod)
    for o in objs[1:]:
        S.drop_layer_attr(o)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), objs, animated=True)
    C.write_report(NAME, {
        "asset": NAME, "character": "Spencer", "round": ROUND,
        "game_tris": asset.low_tris, "lod1_tris": C.tri_count(lod) if lod else None, "source_tris": asset.high_tris,
        "bones": len(arm.data.bones), "sockets": sorted(BM.SOCKETS), "texture_size": tex, "low_texture_size": low_tex,
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
        "layers": [layer.name for layer in LAYERS], "mirrored_uv": False,
        "actions": {n: clips[n](poser)[0] for n in clips},
        "walk_distance_per_loop": round(BM.WALK_STRIDE / BM.WALK_STANCE, 3), "run_distance_per_loop": round(BM.RUN_STRIDE / BM.RUN_STANCE, 3),
        "events": {"Shoot": {"muzzle_flash": 0}, "Reload": {"mag_out": 4, "mag_in": 23}, "Slash": {"hit": 10},
                   "Shockwave": {"impact": BM.SHOCK_IMPACT}, "Down": {"ground": 26}},
        "upper_body_mask": "Spine1 and all its children"})
    C.save_blend(NAME)
    if not fast:
        M3.run_reactions()
    path = os.path.join(C.EXPORTS, NAME, NAME + "_report.json")
    M3.patch_report(path, switch)
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["round"] = ROUND
    rep["brand"] = {"logo": os.path.relpath(LOGO, C.REPO).replace("\\", "/"), "default_variant": BR.DEFAULT,
                    "where": "tabard front and back (painted), raised patch on the left pauldron (star lit)"}
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    if args.get("norender"):
        return
    import spencer_previews as SP
    prefix = args.get("prefix", "pass3")
    SP.render_file(os.path.join(C.BLEND, NAME + ".blend"), [out], prefix, M3.ORIGINAL_BLEND)
    compare(out, prefix)


if __name__ == "__main__":
    main()
