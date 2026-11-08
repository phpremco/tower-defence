"""Spencer, pass 4 (Remco 2026-11-08 on pass 3: "ik vind die blauwe V in zijn visier niet mooi, het zou vetter zijn als
je wel ziet dat er een gezicht achter zit, dus een menselijk gezicht met ogen, en het is nu niet duidelijk dat de
nachtvisie op zijn helm zit ... ipv door het veld rennen kan hij een jetpack op zijn rug gebruiken om naar een andere
gate te vliegen, de loopanimatie mag blijven").

Overrides build_mercer4.py's export on the SAME rig, bone names, sockets, all 20 clip names and export names
(ArtSource/Exports/Mercer/Mercer.fbx, Mercer_<Map>.png, Mercer_report.json); everything not named here is pass 3.

1. Visor and face: the helmet is now a shell with a real visor opening (dark padded liner inside, a gunmetal gasket
   round the opening); the V slit is gone. Behind it Cole Spencer's face (MPFB, spencer_face.py): mesh
   `Mercer_Face` with its own maps (`Mercer_Face_<Map>.png`, same channel layout as the body) and a `Blink` shape
   key. In front of it the glass `Mercer_Visor`: a tinted, see-through faceplate (`Mercer_Visor_Albedo.png`, alpha =
   opacity: clearer in the middle, denser at the rim; `Mercer_Visor_Emission.png`: faint HUD ticks along the edge).
   Both are rigid on the Head bone; far copies `Mercer_Face_LOD1` and `Mercer_Visor_LOD1` ride with Mercer_LOD1.
   Fix 2026-11-08 (face and helmet ran through each other): the brow plate, the cheek guards and the chin guard are
   kept out of the cavity (keep_out_of_cavity), and the face is fitted against the built body (Cavity): only skin
   seen through the opening (plus two rings) is kept, visible skin keeps FIT_GAP from the rigid helmet parts and
   NECK_GAP from the neck and collar. spencer_face_check.py verifies every clip (BVH overlap, zero = pass).
2. Night vision: a helmet-mounted quad-tube NVG flipped up: a shroud bolted to the brow, a mount block with a
   transverse hinge (yellow knobs), a pivot arm, the housing with four tubes (phosphor-green lit objectives up,
   rubber eyecups down) and a cable over the right side of the helmet to the battery pack at the back.
3. Jetpack: two thruster pods behind the shoulders with bell nozzles lit inside, fuel cells beside them, a cell bar
   across the top, a frame plate and a harness (shoulder and waist straps). The sword sheath now runs steeper down
   the middle of the back between the pods (hilt still over the right shoulder; the `Back` socket stays at the
   mouth, its axis follows the sheath). New socket bones `Jet_L`, `Jet_R` at the nozzle exits (Y = thrust
   direction, down). New clips Jet_Takeoff, Jet_Fly (loop), Jet_Land: in place, no root motion; the hips carry the
   lift (JET_LIFT); Unity moves the root along the flight path between `liftoff` and `touchdown`.

Run: blender --background --factory-startup --python Tools/Blender/build_mercer5.py --
     [--preview <dir>] [--norender] [--fast] [--stage <dir>] [--prefix pass4] [--face-cache <npz>]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import build_brand as BR  # noqa: E402
import build_mercer as BM  # noqa: E402
import build_mercer2 as M2  # noqa: E402
import build_mercer3 as M3  # noqa: E402
import build_mercer4 as M4  # noqa: E402
import build_mercer_reactions as RX  # noqa: E402
import build_mercer_weapons as W  # noqa: E402
import common as C  # noqa: E402
import portrait_kit as pk  # noqa: E402
import spencer_face as F  # noqa: E402
import suit as S  # noqa: E402
from build_mercer import FWD, SIDE, R, STANCE, dome_patch, euler, foot_at, frame, nrm  # noqa: E402
from build_mercer2 import DARK, DEEP, GROOVE, YELLOW_CLEAN, hazard, metal, plate, ribs, rubber  # noqa: E402
from build_mercer3 import HELM_C, HELM_R, LIGHT, YELLOW_PAINT, light  # noqa: E402
from build_mercer_reactions import KeyClip, idle0  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
ROUND = "spencer-pass4-2026-11-08 (build_mercer5.py)"
FACE, VISOR = "Mercer_Face", "Mercer_Visor"
FACE_LOD, VISOR_LOD = FACE + "_LOD1", VISOR + "_LOD1"
FACE_LOD_RATIO = 0.22
FACE_CACHE = os.path.join(C.REPO, "ArtSource", "Blend", "SpencerFace_capture.npz")

PHOSPHOR = np.array((0.36, 1.0, 0.62))
LINER = dict(paint=np.array((0.055, 0.06, 0.075)), paint_amount=1.0, wear=0.0, rough=0.88, family=dict(weave=1.0))
HEAT = dict(paint=np.array((0.42, 0.33, 0.5)), paint_amount=0.55, wear=0.0, rough=0.35)

# ------------------------------------------------------------------------------------------------------------
# Helmet: a shell with a visor opening
# ------------------------------------------------------------------------------------------------------------

HELM_E = 0.74                     # the helmet's boxy exponent
BOXY = BM.boxy(HELM_E)
SHELL = 0.013                     # shell thickness
WIN_W = 50.0                      # half-width of the opening (degrees of azimuth from the front)
PHI_BOT = -66.0                   # where the shell ends under the jaw (the collar and neck close it)


def win_top(th):
    """Upper edge of the opening (elevation, degrees): straight under the brow, dipping a little at the sides."""
    return 23.0 - 5.0 * (th / WIN_W) ** 2


def win_bot(th):
    return -31.0 + 8.0 * (th / WIN_W) ** 2


def unit_dir(th, ph):
    t, p = math.radians(th), math.radians(ph)
    return Vector((math.cos(p) * math.sin(t), -math.cos(p) * math.cos(t), math.sin(p)))


def helm_local(th, ph):
    d = BOXY(unit_dir(th, ph))
    return Vector((d.x * HELM_R[0], d.y * HELM_R[1], d.z * HELM_R[2]))


def helm_surf(th, ph, out=0.0, scale=1.0):
    """A point `out` above the helmet's (boxy) surface at azimuth `th` (0 front, + to his left) and elevation `ph`,
    and the outward normal there (the ellipsoid normal of that point: close enough for placing parts)."""
    p = helm_local(th, ph)
    n = nrm(Vector((p.x / HELM_R[0] ** 2, p.y / HELM_R[1] ** 2, p.z / HELM_R[2] ** 2)))
    return Vector(HELM_C) + p * scale + n * out, n


def helm_up(th, ph):
    a, _ = helm_surf(th, ph - 2.0)
    b, _ = helm_surf(th, ph + 2.0)
    return nrm(b - a)


def shell_slab(b, th_fn, ph_fn, nu, nv, style):
    inner = 1.0 - SHELL / 0.13

    def outer(u, v):
        th = th_fn(u)
        return helm_surf(th, ph_fn(th, v))[0]

    def inside(u, v):
        th = th_fn(u)
        return Vector(HELM_C) + helm_local(th, ph_fn(th, v)) * inner
    b.slab("Helmet", outer, inside, nu, nv, frame=S.shell_frame(Vector(HELM_C), HELM_R, Matrix.Identity(3)), bone="Head",
           chain=["Head"], style=style)


def inward(c):
    rel = c.p - np.array(HELM_C)
    rel /= np.maximum(np.linalg.norm(rel, axis=1, keepdims=True), 1e-9)
    return C.smoothstep(0.05, -0.35, np.einsum("ij,ij->i", c.n, rel))


def helmet_style():
    """build_mercer4's two-tone helmet, plus the dark padded liner on every inward face (what the opening shows)."""
    base = M4.helmet_style()

    def style(s, idx, part):
        base(s, idx, part)
        ctx = S.Ctx(s, idx, part)
        m = np.clip(inward(ctx), 0, 1)
        col = s.albedo[idx]
        s.albedo[idx] = C.lerp3(col, LINER["paint"], m)
        s.rough[idx] = s.rough[idx] * (1 - m) + LINER["rough"] * m
        s.fam[idx] = s.fam[idx] * (1 - m)[:, None] + np.array((1.0, 0.0, 0.0, 0.0)) * m[:, None]
        s.emit[idx] = s.emit[idx] * (1 - m)[:, None]
    return style


def helmet_shell(b):
    style = helmet_style()
    span = 360.0 - 2 * WIN_W
    top = 89.0
    # Back and sides (round the back from the left edge of the opening to its right edge).
    shell_slab(b, lambda u: WIN_W + span * u, lambda th, v: PHI_BOT + (top - PHI_BOT) * v, 44, 24, style)
    w = WIN_W + 4.0
    # Over the opening (forehead to crown) and under it (jaw).
    shell_slab(b, lambda u: -w + 2 * w * u, lambda th, v: win_top(th) + (top - win_top(th)) * v, 20, 12, style)
    shell_slab(b, lambda u: -w + 2 * w * u, lambda th, v: PHI_BOT + (win_bot(th) - PHI_BOT) * v, 20, 10, style)


def visor_outline(n=48):
    """The opening's rim as a closed loop of (th, ph)."""
    pts = [(th, win_top(th)) for th in np.linspace(-WIN_W, WIN_W, n)]
    pts += [(WIN_W, ph) for ph in np.linspace(win_top(WIN_W), win_bot(WIN_W), 8)[1:-1]]
    pts += [(th, win_bot(th)) for th in np.linspace(WIN_W, -WIN_W, n)]
    pts += [(-WIN_W, ph) for ph in np.linspace(win_bot(-WIN_W), win_top(-WIN_W), 8)[1:-1]]
    return pts


def gasket(b):
    """A gunmetal gasket on the rim of the opening (four overlapping runs; the remesh fuses the corners)."""
    loop = visor_outline(24)
    n = len(loop)
    runs = [loop[0:24], loop[23:31], loop[30:54], loop[53:n] + [loop[0]]]
    for run in runs:
        pts = [helm_surf(th, ph, -SHELL * 0.35)[0] for th, ph in run]
        b.tube("VisorFrame", pts, [0.0062] * len(pts), sides=8, chain=["Head"], cap=0.6,
               style=metal([(lambda c: S.line(c.t * C.axis_length(c.part), 0.0, 0.002), GROOVE)], seed=93))


def face_guards(b):
    """Brow plate (pass 3), cheek guards that narrow the opening towards the jaw, a lower chin guard and filters."""
    dome_patch(b, "Brow", HELM_C, HELM_R, None, lambda a, v: (a * 0.92, -1.0, 0.4 + 0.62 * (v + 1) / 2), 0.016, "Head",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), GROOVE)], seed=32),
               nu=22, nv=10, outline=lambda a, v: (a * (1.0 - 0.3 * (v + 1) / 2), v))
    for name, sd in SIDE.items():
        sx = sd.sx
        dome_patch(b, "Cheek", HELM_C, HELM_R, None,
                   lambda a, v, sx=sx: (sx * (1.0 - 0.36 * (a + 1) / 2), -(0.12 + 0.88 * (a + 1) / 2), -0.12 - 0.85 * (v + 1) / 2),
                   0.016, "Head", M2.bplate([(lambda c: S.line(c.u[:, 2], -0.72, 0.03), GROOVE)] + M4.hex_features(False), seed=33 + (sx > 0)),
                   nu=16, nv=10)
        b.tube("Ear", [HELM_C + G(0.112 * sx, -0.02, -0.005), HELM_C + G(0.148 * sx, -0.02, -0.005)], [0.044, 0.038], sides=18,
               chain=["Head"], style=metal([(lambda c: S.band(c.t, 0.95, 0.12), GROOVE)], seed=6), cap=0.35)
    b.ellipsoid("Chin", G(0, 1.35, 0.07), R(0.064, 0.034, 0.05), euler(pitch=14), bone="Head", deform=BM.boxy(0.55), segments=(28, 14),
                style=metal([(lambda c: ribs(c.u[:, 0], 0.22, 0.25) * (c.u[:, 1] < -0.3) * S.band(c.u[:, 2], 0.0, 0.5), DEEP),
                             (lambda c: S.band(c.u[:, 2], 0.75, 0.2), YELLOW_CLEAN)], seed=5))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Filter", [G(0.05 * sx, 1.346, 0.08), G(0.088 * sx, 1.33, 0.048)], [0.02, 0.018], sides=14, chain=["Head"], cap=0.5,
               style=metal([(lambda c: ribs(c.t, 0.25, 0.2), GROOVE), (lambda c: S.band(c.t, 0.92, 0.1), YELLOW_CLEAN)], seed=34))


# ------------------------------------------------------------------------------------------------------------
# Helmet-mounted NVG (quad tube, flipped up)
# ------------------------------------------------------------------------------------------------------------

NVG_E = 29.0                         # elevation of the shroud's centre on the brow
X_AX = Vector((1, 0, 0))


def nvg_frame():
    s, n = helm_surf(0.0, NVG_E, 0.016)
    t = helm_up(0.0, NVG_E)
    return s, n, t


def tube_with_glow(b, root, tip, r, style_seed):
    d = nrm(tip - root)
    b.tube("NvgLens", [root, root.lerp(tip, 0.55), tip], [r, r, r * 1.1], sides=16, chain=["Head"], cap=0.12,
           style=metal([(lambda c: ribs(c.t, 0.16, 0.2) * (c.t > 0.25) * (c.t < 0.75), GROOVE),
                        (lambda c: S.band(c.t, 0.9, 0.05), YELLOW_CLEAN)], seed=style_seed))
    side = nrm(d.cross(Vector((0, 0, 1)))) if abs(d.z) < 0.95 else X_AX
    b.ellipsoid("NvgGlow", tip + d * 0.0012, (r * 0.82, r * 0.82, 0.0032), frame(side.cross(d), d), bone="Head",
                style=S.light(PHOSPHOR, strength=3.0), segments=(16, 8))
    b.ellipsoid("NvgEyecup", root - d * 0.004, (r * 1.18, r * 1.18, 0.009), frame(side.cross(d), d), bone="Head",
                deform=BM.boxy(0.7), style=rubber([(lambda c: S.band(c.u[:, 2], -0.9, 0.2), dict(groove=0.0015))]), segments=(16, 8))


def nvg(b):
    s, n, t = nvg_frame()
    # Shroud: a bolted plate on the brow, a mount block with the hinge on it.
    b.ellipsoid("NvgShroud", s + n * 0.003, (0.036, 0.03, 0.0075), frame(t, n), bone="Head", deform=BM.boxy(0.35), segments=(20, 12),
                style=metal([(lambda c: S.line(np.abs(c.u[:, 0]), 0.62, 0.05), GROOVE),
                             (lambda c: hazard(c, 0.01, (1, 0, 1)) * S.band(c.u[:, 1], -0.75, 0.15), YELLOW_PAINT)], seed=80))
    for bx, by in ((-0.026, -0.018), (0.026, -0.018), (0.0, 0.022)):
        b.ellipsoid("NvgBolt", s + n * 0.01 + X_AX * bx + t * by, (0.0045, 0.0045, 0.003), frame(t, n), bone="Head", segments=(10, 6),
                    style=metal(seed=81))
    mount = s + n * 0.016 + t * 0.004
    b.ellipsoid("NvgMount", mount, (0.02, 0.022, 0.012), frame(t, n), bone="Head", deform=BM.boxy(0.5), segments=(16, 10),
                style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.08), GROOVE)], seed=82))
    hinge = s + n * 0.022 + t * 0.012
    b.tube("NvgHinge", [hinge - X_AX * 0.03, hinge, hinge + X_AX * 0.03], [0.0075, 0.0075, 0.0075], sides=12, chain=["Head"], cap=0.3,
           style=metal([(lambda c: ribs(c.t, 0.2, 0.15), GROOVE)], seed=83))
    for sx in (-1, 1):
        b.ellipsoid("NvgKnob", hinge + X_AX * (0.032 * sx), (0.006, 0.0095, 0.0095), None, bone="Head", deform=BM.boxy(0.6),
                    segments=(12, 8), style=metal([(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.yellow, wear=0.2, rough=0.5))], seed=84))
    # Pivot arm from the hinge up the forehead to the housing.
    t2 = helm_up(0.0, NVG_E + 16)
    body = hinge + t * 0.038 + n * 0.011
    arm_c = hinge.lerp(body, 0.5) + n * 0.002
    b.ellipsoid("NvgArm", arm_c, (0.016, (body - hinge).length * 0.62, 0.0065), frame(nrm(body - hinge), n), bone="Head",
                deform=BM.boxy(0.45), segments=(14, 12), style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.1), GROOVE)], seed=85))
    # Housing and the four tubes: objectives up (lit), eyecups down towards the brow.
    d = nrm(t2 * 0.74 + n * 0.67)          # objectives up and forward: they face the game camera
    b.ellipsoid("NvgBody", body - n * 0.012, (0.058, 0.016, 0.011), frame(d, n), bone="Head", deform=BM.boxy(0.5), segments=(24, 12),
                style=metal([(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.plate_side * 0.62, wear=0.1, rough=0.5)),
                             (lambda c: S.line(c.u[:, 0], 0.0, 0.04), GROOVE),
                             (lambda c: S.band(c.u[:, 1], 0.8, 0.12), YELLOW_CLEAN)], seed=86))
    for x, r, splay, drop, length in ((-0.0165, 0.0125, 0.0, 0.0, 0.046), (0.0165, 0.0125, 0.0, 0.0, 0.046),
                                      (-0.044, 0.0112, -12.0, 0.008, 0.04), (0.044, 0.0112, 12.0, 0.008, 0.04)):
        dd = Matrix.Rotation(math.radians(splay), 3, n) @ d
        root = body + X_AX * x - dd * (0.016 + drop) + n * 0.008
        tube_with_glow(b, root, root + dd * length, r, 87)
    # Cable from the housing's right end over the right side of the helmet to the battery pack at the back.
    start = body + X_AX * -0.058 + n * 0.002
    path = [start, helm_surf(-30, 50, 0.014)[0], helm_surf(-42, 66, 0.013)[0], helm_surf(-70, 74, 0.013)[0],
            helm_surf(-115, 66, 0.013)[0], helm_surf(-150, 48, 0.014)[0], helm_surf(-172, 22, 0.02)[0]]
    b.tube("NvgCable", path, [0.0045] * len(path), sides=8, chain=["Head"], cap=0.6,
           style=rubber([(lambda c: ribs(c.t * C.axis_length(c.part), 0.012, 0.25), dict(groove=0.0008))]))
    for p, q in ((path[0], path[1]), (path[-1], path[-2])):
        b.ellipsoid("NvgJack", p, (0.0075, 0.0075, 0.011), frame(Vector((1, 0, 0)), nrm(q - p)), bone="Head", segments=(12, 8),
                    style=metal([(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.yellow, wear=0.2, rough=0.5))], seed=88))


INNER = 1.0 - SHELL / 0.13           # the shell's inner surface, as a fraction of the helmet radius
BROW_MARGIN = 0.5                    # degrees the brow plate's lower edge stays above the opening's top edge
CAVITY_MARGIN = 0.005                # the chin guard's inside stays this fraction of the radius outside the liner


def helm_angles(p):
    """(azimuth, elevation, radius ratio) of a point round the helmet centre: the exact inverse of helm_surf (the boxy
    deform bends directions, so the angles are those of the unit-sphere point it came from) and |p - centre| over the
    helmet surface distance in the same direction."""
    d = Vector(p) - Vector(HELM_C)
    if d.length < 1e-9:
        return 0.0, 0.0, 0.0
    w = [d[i] / HELM_R[i] for i in range(3)]
    k = sum(abs(c) ** (2.0 / HELM_E) for c in w) ** (-HELM_E / 2.0)
    u = Vector(tuple(math.copysign(abs(k * c) ** (1.0 / HELM_E), c) for c in w))
    th = math.degrees(math.atan2(u.x, -u.y))
    ph = math.degrees(math.asin(max(-1.0, min(1.0, u.z))))
    return th, ph, 1.0 / k


def keep_out_of_cavity(b):
    """Outer parts must not reach into the space the face lives in (pass 4 fix, Remco 2026-11-08: the face and the
    helmet ran through each other at the opening's corners). The brow plate's lower corners dipped a few degrees under
    the opening's top edge (a white shard over the forehead), the chin guard, centred on the shell, put half its depth
    inside the cavity in front of the mouth and the jaw, and the brow plate and the cheek guards reached ~7 mm under
    the liner. Their vertices are clamped: the brow up to the opening's top edge, all three out to the liner."""
    verts = {"Brow": set(), "Chin": set(), "Cheek": set()}
    for f, pid in zip(b.bm.faces, b.face_pid):
        name = b.parts[pid].name
        if name in verts:
            verts[name].update(f.verts)
    for v in verts["Brow"]:
        th, ph, r = helm_angles(v.co)
        lo = win_top(max(-WIN_W, min(WIN_W, th))) + BROW_MARGIN
        if ph < lo:
            v.co = Vector(HELM_C) + helm_local(th, lo) * r
    for v in verts["Brow"] | verts["Chin"] | verts["Cheek"]:
        th, ph, r = helm_angles(v.co)
        if r < INNER + CAVITY_MARGIN:
            v.co = Vector(HELM_C) + helm_local(th, ph) * (INNER + CAVITY_MARGIN)


def head(b):
    helmet_shell(b)
    gasket(b)
    face_guards(b)
    nvg(b)
    M4.helmet_kit(b)
    keep_out_of_cavity(b)


# ------------------------------------------------------------------------------------------------------------
# Jetpack, sheath and harness
# ------------------------------------------------------------------------------------------------------------

POD_X, POD_FWD, POD_TOP, POD_BOT, POD_R = 0.155, -0.262, 1.30, 1.075, 0.05
NOZZLE_LEN, NOZZLE_R = (0.022, 0.07), (0.04, 0.058)
SLEEVE_MOUTH = M3.SLEEVE_MOUTH
SLEEVE_DIR = nrm(G(0.2, -0.97, -0.1))
SHEATH_LEN = 0.52
BACK_GRIP = SLEEVE_MOUTH - SLEEVE_DIR * 0.06
BACK_EDGE = nrm(G(-0.35, 0.0, -1.0))
JET_SOCKETS = {}
for _name, _sd in SIDE.items():
    JET_SOCKETS["Jet_" + _name] = G(_sd.sx * (POD_X + 0.006), POD_BOT - NOZZLE_LEN[1] - 0.004, POD_FWD)


def nozzle_style():
    def glow_face(c):
        return C.smoothstep(0.55, 0.85, -c.n[:, 2]) * (1 - C.smoothstep(0.036, 0.044, c.dist))
    return metal([(lambda c: ribs(c.t, 0.22, 0.2) * (c.t < 0.6), GROOVE),
                  (lambda c: C.smoothstep(0.55, 0.95, c.t) * (1 - glow_face(c)), HEAT),
                  (lambda c: S.band(c.t, 0.08, 0.06), YELLOW_CLEAN),
                  (glow_face, dict(emit=np.array((0.62, 0.95, 1.0)), glow=2.6, rough=0.3, family={}))], seed=95)


def pod_style(sx):
    out_back = np.array(nrm(Vector((sx * 0.6, 1.0, 0.0))))

    def window(c):
        return S.band(c.t, 0.56, 0.2) * C.smoothstep(0.75, 0.9, c.n @ out_back)
    return M2.bplate([(lambda c: C.smoothstep(0.24, 0.2, c.t), dict(paint=S.PAL.plate_top, wear=0.25, rough=0.45)),
                      (lambda c: S.band(c.t, 0.3, 0.035), GROOVE),
                      (lambda c: hazard(c, 0.016, (1, 0, 1)) * S.band(c.t, 0.88, 0.05), YELLOW_PAINT),
                      (lambda c: S.band(c.t, 0.88, 0.07) * (1 - hazard(c, 0.016, (1, 0, 1))), dict(paint=S.PAL.metal_under, wear=0.0)),
                      (window, LIGHT),
                      (lambda c: S.line(c.t, 0.34, 0.012) + S.line(c.t, 0.78, 0.012), GROOVE)], seed=96 + (sx > 0))


def jetpack(b):
    # Frame plate on the back.
    b.ellipsoid("Pack", G(0, 1.165, -0.198), R(0.112, 0.17, 0.052), bone="Spine2", deform=BM.boxy(0.3), segments=(32, 18),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.5, 0.03) + S.line(c.u[:, 2], -0.55, 0.03), GROOVE),
                             (lambda c: S.band(c.u[:, 2], -0.78, 0.07) * hazard(c, 0.02, (1, 0, -1)) * (c.u[:, 1] > 0.3), YELLOW_PAINT),
                             (lambda c: (c.u[:, 1] > 0.4) * S.band(np.abs(c.u[:, 0]), 0.0, 0.62), dict(paint=S.PAL.bplate_side, wear=0.1))],
                            seed=7))
    for name, sd in SIDE.items():
        sx = sd.sx
        top, bot = G(sx * POD_X, POD_TOP, POD_FWD), G(sx * (POD_X + 0.006), POD_BOT, POD_FWD)
        b.tube("JetPod", [top, top.lerp(bot, 0.5), bot], [0.046, POD_R, POD_R], sides=22, chain=["Spine2"], cap=0.85, style=pod_style(sx))
        n0, n1 = bot + G(0, NOZZLE_LEN[0], 0), bot - G(0, NOZZLE_LEN[1], 0)
        b.tube("JetNozzle", [n0, n0.lerp(n1, 0.5), n1], [NOZZLE_R[0], 0.047, NOZZLE_R[1]], sides=22, chain=["Spine2"], cap=0.04,
               style=nozzle_style())
        # A fuel cell between pod and frame, lit in bands.
        c0, c1 = G(sx * 0.098, 1.255, -0.238), G(sx * 0.098, 1.085, -0.238)
        b.tube("FuelCell", [c0, c0.lerp(c1, 0.5), c1], [0.02, 0.021, 0.02], sides=14, chain=["Spine2"], cap=0.5,
               style=S.light(strength=1.8, bands=28.0))
        for y in (1.262, 1.078):
            b.ellipsoid("CoreCap", G(sx * 0.098, y, -0.238), R(0.024, 0.009, 0.024), bone="Spine2", deform=BM.boxy(0.5), segments=(14, 8),
                        style=metal([(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.yellow, wear=0.25, rough=0.5))], seed=97))
        # Harness: a shoulder strap over the collar down to the chest, a waist strap to the belt.
        b.tube("Strap", [G(sx * 0.082, 1.3, -0.18), G(sx * 0.098, 1.352, -0.09), G(sx * 0.108, 1.35, 0.02), G(sx * 0.105, 1.27, 0.12),
                         G(sx * 0.095, 1.2, 0.152)], [0.017] * 5, sides=10, chain=["Spine2"], cap=0.4, flatten=0.3,
               style=M2.fabric([(lambda c: S.line(c.dist, 0.015, 0.002), dict(groove=0.0008)),
                                (lambda c: np.ones(len(c.p)), dict(paint=S.PAL.metal_side, paint_amount=0.6, wear=0.0))]))
        b.ellipsoid("StrapBuckle", G(sx * 0.1, 1.24, 0.145), R(0.02, 0.026, 0.008), euler(pitch=-20), bone="Spine2",
                    deform=BM.boxy(0.4), segments=(12, 8), style=metal([(lambda c: S.line(c.u[:, 2], 0.0, 0.12), GROOVE)], seed=98))
        b.tube("Strap", [G(sx * 0.1, 1.04, -0.2), G(sx * 0.148, 0.99, -0.13), G(sx * 0.162, 0.968, -0.05)], [0.016] * 3, sides=10,
               chain=["Spine1", "Spine2"], cap=0.4, flatten=0.3, style=M2.fabric([(lambda c: np.ones(len(c.p)),
                                                                                   dict(paint=S.PAL.metal_side, paint_amount=0.6, wear=0.0))]))
    # Cell bar across the top between the pods.
    a, z = G(0.105, 1.318, -0.215), G(-0.105, 1.318, -0.215)
    b.tube("CoreBar", [a, z], [0.024, 0.024], sides=18, chain=["Spine2"], cap=0.2,
           style=metal([(lambda c: (ribs(c.t, 0.2, 0.32) < 0.5) * S.band(c.t, 0.5, 0.4) * C.smoothstep(-0.1, 0.4, c.n[:, 2] + c.n[:, 1] * 0.5),
                         LIGHT), (lambda c: S.band(c.t, 0.05, 0.03) + S.band(c.t, 0.95, 0.03), GROOVE)], seed=46))
    sheath(b)


def sheath(b):
    """build_mercer3's sheath, steeper: from the hilt over the right shoulder down the middle of the back."""
    tip = SLEEVE_MOUTH + SLEEVE_DIR * SHEATH_LEN
    b.tube("Sleeve", [SLEEVE_MOUTH - SLEEVE_DIR * 0.012, SLEEVE_MOUTH + SLEEVE_DIR * 0.2, SLEEVE_MOUTH + SLEEVE_DIR * 0.4, tip],
           [0.034, 0.031, 0.027, 0.018], sides=16, chain=["Spine2"], cap=0.9,
           style=plate([(lambda c: S.band(c.t, 0.04, 0.05) + S.band(c.t, 0.5, 0.025) + S.band(c.t, 0.93, 0.04), YELLOW_PAINT),
                        (lambda c: C.smoothstep(0.1, 0.35, -c.n[:, 1]) * (c.t > 0.1), dict(paint=S.PAL.metal_top, wear=0.0)),
                        (lambda c: ribs(c.t * 12, 1.0, 0.08) * (c.t > 0.1), GROOVE)], seed=64))
    side = nrm(SLEEVE_DIR.cross(Vector((0, 1, 0))))
    slit = [SLEEVE_MOUTH + SLEEVE_DIR * d + Vector((0, 0.031, 0)) - side * 0.0 for d in np.linspace(0.07, SHEATH_LEN - 0.08, 8)]
    b.tube("SheathLight", slit, [0.0072] * len(slit), sides=8, chain=["Spine2"], style=light(2.0), cap=0.6)
    for d in (0.0, 0.28):
        b.ring("SheathClamp", SLEEVE_MOUTH + SLEEVE_DIR * d, SLEEVE_DIR, 0.036, 0.008, arcs=3, sides=6, chain=["Spine2"], style=metal(seed=67))
    b.ring("SleeveGlow", SLEEVE_MOUTH - SLEEVE_DIR * 0.01, SLEEVE_DIR, 0.024, 0.006, arcs=3, sides=6, chain=["Spine2"], style=light(1.8))


# ------------------------------------------------------------------------------------------------------------
# Parts, layers, rig
# ------------------------------------------------------------------------------------------------------------

def build_parts():
    b = C.PartBuilder()
    head(b)
    keep_plate, keep_bplate = M3.plate, M3.bplate
    M3.plate = M4.with_camo(M2.plate, {1}, True)
    M3.bplate = M4.with_camo(M2.bplate, {12, 24, 25}, False)
    try:
        M3.torso(b)
        M3.arms(b)
    finally:
        M3.plate, M3.bplate = keep_plate, keep_bplate
    M4.tabard(b, 0.146, 0.15, 0.145, 0.3, 0.012, 71, 0.105)
    M4.tabard(b, -0.155, 0.16, 0.15, 0.3, 0.014, 72, 0.1)
    jetpack(b)
    M4.logo_patch(b)
    M2.legs(b)
    return b


NVG_PARTS = {"NvgShroud", "NvgBolt", "NvgMount", "NvgHinge", "NvgKnob", "NvgArm", "NvgBody", "NvgLens", "NvgGlow", "NvgEyecup",
             "NvgCable", "NvgJack"}
HEAD_KIT = NVG_PARTS | {"Rail", "HelmCam", "CamLens", "CamGlow", "CamTally", "CommUnit", "CommAntenna", "HelmPack", "VisorFrame"}
JET = {"JetPod", "JetNozzle", "FuelCell", "CoreCap", "CoreBar", "Pack", "StrapBuckle", "Sleeve", "SheathLight", "SheathClamp", "SleeveGlow"}
OLD = {"Visor", "Nozzle", "NozzleGlow", "PackPouch", "Hose"} | M4.HEAD_KIT
RIGID = (M4.RIGID - OLD) | HEAD_KIT | JET
PLATES = (M4.PLATES - OLD) | {"JetPod"}
GEAR = (M4.GEAR - OLD - {"JetPod"}) | HEAD_KIT | (JET - {"JetPod", "Sleeve"}) | {"Strap"}
LAYERS = [S.Layer("suit", None, voxel=0.0034, smooth=6, density=0.7),
          S.Layer("plates", PLATES, voxel=0.0018, smooth=2, density=1.0),
          S.Layer("gear", GEAR, voxel=0.0019, smooth=3, density=1.0),
          S.Layer("insignia", M4.INSIGNIA, voxel=0.0013, smooth=1, density=1.4, uv_scale=1.8),
          S.Layer("hands", {"Palm", "Knuckles", "Finger", "Thumb"}, voxel=0.0014, smooth=3, density=2.0)]
M3.RIGID = RIGID

# The hilt's socket stays at the sleeve mouth; its axis follows the steeper sheath.
BM.BACK_SOCKET = BACK_GRIP
BM.BACK_Y = SLEEVE_DIR
BM.BACK_Z = BACK_EDGE
_bones = BM.bones


def bones():
    """build_mercer's bones plus the two thrust sockets (Y = thrust direction, straight down; Z backwards)."""
    out = _bones()
    for name, head_ in JET_SOCKETS.items():
        out.append((name, head_, head_ + Vector((0, 0, -0.06)), "Spine2", True, Vector((0, 1, 0))))
    return out


BM.bones = bones
BM.SOCKETS = set(BM.SOCKETS) | set(JET_SOCKETS)


# ------------------------------------------------------------------------------------------------------------
# Jet clips (in place; the hips carry the lift; Unity moves the root along the flight path)
# ------------------------------------------------------------------------------------------------------------

JET_LIFT = 0.3
TAKEOFF_FRAMES, FLY_FRAMES, LAND_FRAMES = 30, 40, 28
TAKEOFF_EVENTS = {"crouch": 7, "ignite": 8, "liftoff": 12, "airborne": 30}
LAND_EVENTS = {"flare": 8, "touchdown": 13, "thrust_off": 15, "rest": 28}


def lifted(dy, dz=0.0):
    return STANCE["hips.loc"] + G(0, dy, dz)


def stance_p(**over):
    p = dict(idle0())
    p.update(over)
    return p


def fly_p(t, f):
    w = 2 * math.pi * t
    bob = math.sin(w)
    return stance_p(**{
        "hips.loc": lifted(JET_LIFT + 0.015 * bob), "hips.pitch": 38.0, "hips.yaw": 0.0, "hips.roll": 2.5 * math.sin(w + 0.5),
        "spine1.pitch": 8.0, "spine1.yaw": 0.0, "spine1.roll": -1.5 * math.sin(w + 0.5), "spine2.pitch": 5.0, "spine2.yaw": 0.0,
        "neck.pitch": -20.0, "neck.yaw": 0.0, "head.pitch": -22.0 + 1.5 * math.sin(w + 1.0), "head.yaw": 3.0 * math.sin(w * 0.5 + 0.3),
        "clav.L.raise": 3.0, "clav.R.raise": 3.0,
        "arm.L.swing": -16.0 + 4.0 * math.sin(w + 0.4), "arm.R.swing": -16.0 + 4.0 * math.sin(w + 2.0),
        "arm.L.out": 26.0 + 3.0 * math.sin(w), "arm.R.out": 26.0 + 3.0 * math.sin(w + 1.6),
        "arm.L.elbow": 34.0, "arm.R.elbow": 36.0, "arm.L.twist": 22.0, "arm.R.twist": 22.0, "arm.L.wrist": -12.0, "arm.R.wrist": -12.0,
        "arm.L.wrist_side": 10.0, "arm.R.wrist_side": 10.0,
        "grip.L.fingers": 0.3, "grip.R.fingers": 0.34, "grip.L.thumb": 0.2, "grip.R.thumb": 0.2,
        "foot.L.pos": foot_at("L", fwd=-0.3 + 0.025 * math.sin(w), side=0.02, up=JET_LIFT + 0.13 + 0.02 * math.sin(w + 1.0)),
        "foot.R.pos": foot_at("R", fwd=-0.2 + 0.025 * math.sin(w + math.pi), side=0.03, up=JET_LIFT + 0.2 + 0.02 * math.sin(w + 2.5)),
        "foot.L.pitch": -48.0, "foot.R.pitch": -42.0, "foot.L.toe": 12.0, "foot.R.toe": 10.0, "foot.L.yaw": 4.0, "foot.R.yaw": 6.0,
    })


def crouch_p():
    return stance_p(**{
        "hips.loc": lifted(-0.13, 0.02), "hips.pitch": 16.0, "spine1.pitch": 9.0, "spine2.pitch": 6.0, "neck.pitch": -6.0,
        "head.pitch": -12.0, "head.yaw": 0.0, "arm.L.swing": -28.0, "arm.R.swing": -28.0, "arm.L.out": -6.0, "arm.R.out": -6.0,
        "arm.L.elbow": 28.0, "arm.R.elbow": 30.0, "grip.L.fingers": 0.55, "grip.R.fingers": 0.55})


def takeoff_keys(poser):
    start = idle0()
    crouch = crouch_p()
    push = stance_p(**{
        "hips.loc": lifted(0.04, 0.01), "hips.pitch": 6.0, "spine1.pitch": 2.0, "spine2.pitch": 0.0, "neck.pitch": -8.0, "head.pitch": -10.0,
        "arm.L.swing": 12.0, "arm.R.swing": 12.0, "arm.L.out": 18.0, "arm.R.out": 18.0, "arm.L.elbow": 22.0, "arm.R.elbow": 22.0,
        "foot.L.pitch": -34.0, "foot.R.pitch": -34.0, "foot.L.toe": 34.0, "foot.R.toe": 34.0,
        "foot.L.pos": STANCE["foot.L.pos"] + G(0, 0.07, 0), "foot.R.pos": STANCE["foot.R.pos"] + G(0, 0.07, 0)})
    lift = stance_p(**{
        "hips.loc": lifted(0.13, 0.0), "hips.pitch": 8.0, "neck.pitch": -10.0, "head.pitch": -12.0,
        "arm.L.swing": 4.0, "arm.R.swing": 4.0, "arm.L.out": 24.0, "arm.R.out": 24.0, "arm.L.elbow": 30.0, "arm.R.elbow": 30.0,
        "foot.L.pitch": -46.0, "foot.R.pitch": -40.0, "foot.L.toe": 14.0, "foot.R.toe": 14.0,
        "foot.L.pos": foot_at("L", fwd=-0.04, side=0.03, up=0.2), "foot.R.pos": foot_at("R", fwd=-0.12, side=0.03, up=0.22)})
    rise = fly_p(0.0, 0)
    rise = dict(rise)
    rise.update({"hips.loc": lifted(JET_LIFT * 0.85), "hips.pitch": 18.0, "foot.L.pos": foot_at("L", fwd=-0.22, side=0.02, up=JET_LIFT + 0.16),
                 "foot.R.pos": foot_at("R", fwd=-0.12, side=0.03, up=JET_LIFT + 0.26)})
    return [(0, start, "ease"), (TAKEOFF_EVENTS["crouch"], crouch, "ease"), (10, push, "snap"), (TAKEOFF_EVENTS["liftoff"], lift, "linear"),
            (20, rise, "ease"), (TAKEOFF_FRAMES, fly_p(0.0, 0), "ease")]


def land_keys(poser):
    fly = fly_p(0.0, 0)
    flare = stance_p(**{
        "hips.loc": lifted(JET_LIFT * 0.55, -0.02), "hips.pitch": -6.0, "spine1.pitch": -4.0, "spine2.pitch": -2.0, "neck.pitch": 2.0,
        "head.pitch": 6.0, "arm.L.swing": 14.0, "arm.R.swing": 14.0, "arm.L.out": 36.0, "arm.R.out": 36.0, "arm.L.elbow": 26.0,
        "arm.R.elbow": 26.0, "grip.L.fingers": 0.3, "grip.R.fingers": 0.3,
        "foot.L.pos": foot_at("L", fwd=0.12, side=0.06, up=JET_LIFT * 0.55 - 0.02), "foot.R.pos": foot_at("R", fwd=-0.05, side=0.05,
                                                                                                         up=JET_LIFT * 0.55 + 0.03),
        "foot.L.pitch": -14.0, "foot.R.pitch": -20.0, "foot.L.toe": 6.0, "foot.R.toe": 6.0})
    touch = stance_p(**{"hips.loc": lifted(-0.02, 0.0), "hips.pitch": 6.0, "spine1.pitch": 3.0, "arm.L.out": 24.0, "arm.R.out": 24.0,
                        "arm.L.swing": 10.0, "arm.R.swing": 10.0, "arm.L.elbow": 30.0, "arm.R.elbow": 30.0})
    absorb = crouch_p()
    absorb.update({"arm.L.swing": 18.0, "arm.R.swing": 14.0, "arm.L.out": 16.0, "arm.R.out": 14.0, "head.pitch": -6.0})
    return [(0, fly, "ease"), (LAND_EVENTS["flare"], flare, "ease"), (LAND_EVENTS["touchdown"], touch, "in"), (18, absorb, "snap"),
            (LAND_FRAMES, idle0(), "ease")]


def jet_clip_fns():
    return {"Jet_Takeoff": lambda poser: KeyClip(poser, takeoff_keys(poser)).make(TAKEOFF_FRAMES, False)(poser),
            "Jet_Fly": BM.clip(FLY_FRAMES, fly_p, True),
            "Jet_Land": lambda poser: KeyClip(poser, land_keys(poser)).make(LAND_FRAMES, False)(poser)}


def jet_report():
    def norm(ev, frames):
        return {k: round(v / frames, 3) for k, v in ev.items()}
    return {
        "clips": {
            "Jet_Takeoff": {"frames": TAKEOFF_FRAMES, "seconds": round(TAKEOFF_FRAMES / 30, 3), "loop": False, "starts": "Idle frame 0",
                            "ends": "Jet_Fly frame 0", "events": TAKEOFF_EVENTS, "events_normalized": norm(TAKEOFF_EVENTS, TAKEOFF_FRAMES)},
            "Jet_Fly": {"frames": FLY_FRAMES, "seconds": round(FLY_FRAMES / 30, 3), "loop": True, "events": {}},
            "Jet_Land": {"frames": LAND_FRAMES, "seconds": round(LAND_FRAMES / 30, 3), "loop": False, "starts": "Jet_Fly frame 0",
                         "ends": "Idle frame 0", "events": LAND_EVENTS, "events_normalized": norm(LAND_EVENTS, LAND_FRAMES)}},
        "root_motion": "none: the Root bone is never keyed; Unity moves the character root along the flight path",
        "hips_lift_m": JET_LIFT,
        "hips_lift_note": "the hips (and so the whole body) ride JET_LIFT above the stance at the end of Jet_Takeoff, all through "
                          "Jet_Fly and at the start of Jet_Land, so the feet are off the ground with the root on the ground; "
                          "extra altitude is the root's job in code",
        "thrust": "ignite (Jet_Takeoff) to thrust_off (Jet_Land): VFX on Jet_L / Jet_R (socket Y axis = exhaust direction)",
        "sockets": {n: {"parent": "Spine2", "head_blender": [round(x, 4) for x in p], "y_axis": "down (exhaust)"}
                    for n, p in JET_SOCKETS.items()},
        "unity": "Jet_Takeoff (root still) until liftoff, then move the root along the path while Jet_Fly loops; start Jet_Land "
                 "so that its touchdown frame meets the ground at the target gate; the root stays on the ground plane at "
                 "touchdown, so place it there before or at that frame."}


# ------------------------------------------------------------------------------------------------------------
# Face and visor objects
# ------------------------------------------------------------------------------------------------------------

FACE_SCALE = 1.22
EYE_Z = 1.480                    # pass-4 fix: 12 mm higher, so the chin clears the liner under the opening (was 1.468)
CLEARANCE = 0.93                 # the face stays inside 93 % of the helmet's radius in every direction (glass ~99 %)


FIT_GAP = 0.0025                 # visible skin keeps 2.5 mm from every body surface (liner, guards, gasket, neck)
FIT_STEP = 0.001                 # the face moves back this far per fitting step
FIT_RINGS = 2                    # polygon rings kept round the visible skin, so cut edges stay behind the helmet
VIEW_AZ = (-90.0, 90.0, 7.5)     # view directions the visible skin is found from (degrees, head frame): every yaw in
VIEW_EL = (-15.0, 75.0, 7.5)     # front, from a little below (cutscenes) to steeply above (the game camera, ~44)
NEAR = 0.3
NECK_GAP = 0.004                 # skin keeps 4 mm from the neck, collar and shoulders in every sampled pose
POSE_STEP = 1                    # every frame of every clip is sampled (the Down clip hits the shoulder on one odd frame)
OPENING_SLACK = 3.0              # degrees round the opening's edge a ray may still leave through


def surface_ratio(points):
    """|p - centre| over the helmet surface distance in the same direction, per point."""
    return np.array([helm_angles(p)[2] for p in points])


class Cavity:
    """The body round the head in the rest pose, for fitting the face. The parts rigid on the Head bone (shell, liner,
    guards, gasket) hide skin and must never be touched by visible skin; the neck and the collar move under the
    helmet, so they never count as cover and skin near them goes (with a wider gap, as they move)."""

    def __init__(self, body, arm=None, actions=()):
        from mathutils.bvhtree import BVHTree
        me = body.data
        head = body.vertex_groups["Head"].index
        self.co = [v.co.copy() for v in me.vertices]
        rigid = np.array([any(g.group == head and g.weight > 0.999 for g in v.groups) for v in me.vertices])
        c = Vector(HELM_C)
        near = [tuple(p.vertices) for p in me.polygons if (self.co[p.vertices[0]] - c).length < NEAR]
        self.cover = [pv for pv in near if rigid[list(pv)].all()]
        self.helmet = BVHTree.FromPolygons(self.co, self.cover)
        moving = [tuple(p.vertices) for p in me.polygons if not rigid[list(p.vertices)].all()]
        near_set = set(near)
        co, polys = self.moving_poses(body, arm, actions, moving) if arm else (self.co, [p for p in moving if p in near_set])
        self.neck = BVHTree.FromPolygons(co, polys)

    @staticmethod
    def moving_poses(body, arm, actions, moving):
        """Everything not rigid on the head (neck, collar, shoulder plates), posed in every POSE_STEP-th frame of every
        clip and carried into the head's rest frame: the skin must clear all of it (the Down clip throws a shoulder
        plate into the helmet's side at the ground hit)."""
        rest = np.array(arm.data.bones["Head"].matrix_local)
        hc = np.array(HELM_C)
        poly_v = [np.array(p) for p in moving]
        first = np.array([p[0] for p in moving])
        co_all, polys_all, base = [np.array([v.co[:] for v in body.data.vertices])], [], 0
        polys_all += [tuple(p) for p, f in zip(moving, first) if np.linalg.norm(co_all[0][f] - hc) < NEAR]
        base = len(co_all[0])
        for act in actions:
            f0, f1 = (int(round(x)) for x in act.frame_range)
            for f in range(f0, f1 + 1, POSE_STEP):
                C.set_action(arm, act, f)
                bpy.context.view_layer.update()
                ev = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
                me = ev.to_mesh()
                co = np.zeros(len(me.vertices) * 3)
                me.vertices.foreach_get("co", co)
                ev.to_mesh_clear()
                pose = np.array(arm.matrix_world @ arm.pose.bones["Head"].matrix)
                m = rest @ np.linalg.inv(pose) @ np.array(body.matrix_world)
                co = co.reshape(-1, 3) @ m[:3, :3].T + m[:3, 3]
                close = np.linalg.norm(co[first] - hc, axis=1) < NEAR
                polys_all += [tuple(int(j) + base for j in pv) for pv, k in zip(poly_v, close) if k]
                co_all.append(co)
                base += len(co)
        C.log("face fit: moving body sampled in", len(co_all) - 1, "poses,", len(polys_all), "polygons")
        return [Vector(v) for v in np.concatenate(co_all)], polys_all

    @staticmethod
    def through_opening(c, d):
        """Whether a ray that missed the cover leaves the helmet through the visor opening (not through the neck hole
        or the shell's lower rim, which the collar closes: those are not cover because they move)."""
        lo, hi = 0.0, 0.4
        for _ in range(18):
            mid = 0.5 * (lo + hi)
            if helm_angles(c + d * mid)[2] < 1.0:
                lo = mid
            else:
                hi = mid
        th, ph, _ = helm_angles(c + d * hi)
        edge = max(-WIN_W, min(WIN_W, th))
        return abs(th) < WIN_W + OPENING_SLACK and win_bot(edge) - OPENING_SLACK < ph < win_top(edge) + OPENING_SLACK

    def visible(self, face, eyes):
        """Per face polygon: its front seen from some view direction (cover: the rigid helmet parts, the face and the
        eyes; the ray must leave through the opening)."""
        from mathutils.bvhtree import BVHTree
        fco = [Vector(v) for v in face.co]
        n, nf = len(self.co), len(fco)
        occ = BVHTree.FromPolygons(self.co + fco + [Vector(v) for v in eyes.co],
                                   self.cover + [tuple(j + n for j in f) for f in face.faces]
                                   + [tuple(j + n + nf for j in f) for f in eyes.faces])
        dirs = [unit_dir(az, el) for az in np.arange(VIEW_AZ[0], VIEW_AZ[1] + 1e-6, VIEW_AZ[2])
                for el in np.arange(VIEW_EL[0], VIEW_EL[1] + 1e-6, VIEW_EL[2])]   # out of the visor, to the viewer
        out = np.zeros(len(face.faces), bool)
        for i, f in enumerate(face.faces):
            c = sum((fco[j] for j in f), Vector()) / len(f)
            nf_ = (fco[f[1]] - fco[f[0]]).cross(fco[f[2]] - fco[f[0]])
            for d in dirs:
                if nf_.dot(d) > 0.0 and occ.ray_cast(c + d * 0.0005, d, 0.6)[0] is None and self.through_opening(c, d):
                    out[i] = True
                    break
        return out

    @staticmethod
    def touching(face, tree, gap):
        """Per face polygon: a triangle overlap with `tree` or a corner closer than `gap`. No inside-a-solid test: the
        face is one connected sheet that is mostly in the open, so any part of it inside a solid crosses its surface."""
        from mathutils.bvhtree import BVHTree
        fco = [Vector(v) for v in face.co]
        near = np.array([tree.find_nearest(p, gap)[0] is not None for p in fco])
        bad = np.array([near[list(f)].any() for f in face.faces])
        bad[[a for a, _ in BVHTree.FromPolygons(fco, face.faces).overlap(tree)]] = True
        return bad

    def contacts(self, face, eyes):
        """(visible, touching the helmet, touching the neck or collar) per face polygon."""
        return (self.visible(face, eyes), self.touching(face, self.helmet, FIT_GAP),
                self.touching(face, self.neck, NECK_GAP))


def grow(face, keep, rings):
    for _ in range(rings):
        on = np.zeros(len(face.co), bool)
        for f, k in zip(face.faces, keep):
            if k:
                on[list(f)] = True
        keep = keep | np.array([on[list(f)].any() for f in face.faces])
    return keep


def keep_faces(piece, keep):
    faces = [f for f, k in zip(piece.faces, keep) if k]
    used = np.zeros(len(piece.co), bool)
    for f in faces:
        used[list(f)] = True
    remap = -np.ones(len(piece.co), int)
    remap[used] = np.arange(used.sum())
    return F.Piece(piece.co[used], [tuple(int(remap[j]) for j in f) for f in faces],
                   [c for c, k in zip(piece.cuv, keep) if k], piece.blink[used])


def build_face(cache, tex_dir, body, arm=None, actions=()):
    caps, info = F.capture(cache)
    face, lm = F.face_piece(caps, info)
    eyes = F.eye_pieces(caps)
    # Eye centres from the eyeballs themselves.
    comp = F.components(eyes.faces, len(eyes.co))
    centres = [eyes.co[comp == c].mean(axis=0) for c in np.unique(comp)]
    lm["eye_c"] = np.mean(centres, axis=0)
    brows = F.piece_of(caps["eyebrows"])
    lashes = F.piece_of(caps["eyelashes"])

    def placed(y):
        out = [F.Piece(p.co.copy(), p.faces, p.cuv, p.blink.copy()) for p in (face, eyes, brows, lashes)]
        return out, F.place(out, dict(lm), Vector((0.0, y, EYE_Z)), FACE_SCALE)

    y = -0.07
    for _ in range(12):
        pieces, lm2 = placed(y)
        r = surface_ratio(pieces[0].co[pieces[0].co[:, 1] < y])
        worst = float(r.max())
        if worst <= CLEARANCE + 0.004:
            break
        y += (worst - CLEARANCE) * 0.13
    C.log("face placed: eye centre y", round(y, 4), "worst radius ratio", round(worst, 3))
    # Fit against the real helmet (pass-4 fix): move back until no visible skin touches the body, then drop the skin
    # nobody can see (scalp sides, under the chin, the back of the jaw: it ran into the neck and the chin guard).
    cavity = Cavity(body, arm, actions)
    for step in range(25):
        pieces, lm2 = placed(y)
        visible, helmet, neck = cavity.contacts(pieces[0], pieces[1])
        if not (visible & helmet).any():
            break
        C.log("face fit: step", step, "y", round(y, 4), "visible polygons touching the helmet", int((visible & helmet).sum()))
        y += FIT_STEP
    else:
        raise RuntimeError("face fit: visible skin still touches the helmet")
    keep = grow(pieces[0], visible, FIT_RINGS) & ~helmet & ~neck
    C.log("face fit: eye centre y", round(y, 4), "visible", int(visible.sum()), "kept", int(keep.sum()), "of", len(keep),
          "visible skin dropped at the neck", int((visible & neck).sum()))
    pieces[0] = keep_faces(pieces[0], keep)
    face_p, eyes_p, brows_p, lashes_p = pieces
    skin_uv = F.planar_uv(face_p, F.SKIN_BOX)
    # Folds that face away from the visor (inner lids, nostrils, the inner lips) would overlap the front projection and
    # print their texels over the skin in front of them: they get a small island of their own, painted as shadow.
    co = face_p.co
    from mathutils.bvhtree import BVHTree
    occluders = BVHTree.FromPolygons([tuple(map(float, v)) for v in np.concatenate([co, eyes_p.co])],
                                     face_p.faces + [tuple(j + len(co) for j in f) for f in eyes_p.faces])
    for i, f in enumerate(face_p.faces):
        a, b_, c = (Vector(co[j]) for j in f[:3])
        centre = Vector(co[list(f)].mean(axis=0))
        hidden = occluders.ray_cast(centre + Vector((0, -0.0006, 0)), Vector((0, -1, 0)), 0.2)[0] is not None
        if hidden or (b_ - a).cross(c - a).normalized().y > -0.2:
            skin_uv[i] = [(F.FOLD_BOX[0] + (F.FOLD_BOX[2] - F.FOLD_BOX[0]) * (k % 2), F.FOLD_BOX[1] + (F.FOLD_BOX[3] - F.FOLD_BOX[1]) * (k // 2 % 2))
                          for k in range(len(f))]
    # Both eyeballs share one island (each projected from the front round its own centre: the iris in the middle).
    comp = F.components(eyes_p.faces, len(eyes_p.co))
    eye_uv = [None] * len(eyes_p.faces)
    for c in np.unique(comp):
        keep = comp == c
        sub = eyes_p.subset(keep)
        uv = F.planar_uv(sub, F.EYE_BOX, centre=sub.co[:, [0, 2]].mean(axis=0))
        idx = [i for i, f in enumerate(eyes_p.faces) if keep[f[0]]]
        for i, u in zip(idx, uv):
            eye_uv[i] = u
    obj, blink = F.mesh_from([(face_p, skin_uv, face_p.cuv, 0), (eyes_p, eye_uv, eyes_p.cuv, 1)], FACE)
    flag = np.concatenate([np.zeros((len(face_p.co), 3)), np.tile((1.0, 0.0, 0.0), (len(eyes_p.co), 1))])
    paths = F.textures(obj, tex_dir, lm2, brows_p, lashes_p, flag, os.path.join(tex_dir, "Low"))
    C.shade_smooth(obj.data)
    # Shape key Blink (the upper lids close).
    blink = np.where(np.linalg.norm(blink, axis=1, keepdims=True) < 0.03, blink, 0.0)
    C.log("face blink: max lid travel", round(float(np.linalg.norm(blink, axis=1).max()), 4), "verts", len(blink),
          "mesh verts", len(obj.data.vertices))
    obj.shape_key_add(name="Basis", from_mix=False)
    key = obj.shape_key_add(name="Blink", from_mix=False)
    co = np.zeros(len(obj.data.vertices) * 3)
    obj.data.vertices.foreach_get("co", co)
    key.data.foreach_set("co", (co.reshape(-1, 3) + blink).ravel())
    key.value = 0.0
    for a in ("GP", "GN", "GX"):
        if a in obj.data.attributes:
            obj.data.attributes.remove(obj.data.attributes[a])
    obj.data.uv_layers.remove(obj.data.uv_layers["Src"])
    tex = {k.lower(): v for k, v in paths.items()}
    obj.data.materials.clear()
    obj.data.materials.append(C.game_material(FACE + "_Mat", tex, emission_strength=1.0, specular=0.5))  # emission: F.FACE_GLOW self-light + catchlight
    return obj, paths


VISOR_TEX = (512, 256)


def visor_textures(out_dir):
    """Albedo RGBA (tint; alpha = opacity, clear in the middle, denser at the rim) and Emission (faint HUD ticks)."""
    w, h = VISOR_TEX
    v, u = np.mgrid[0:h, 0:w]
    u = (u + 0.5) / w
    v = (v + 0.5) / h
    edge = np.minimum(np.minimum(u, 1 - u) * 2.0, np.minimum(v, 1 - v))       # 0 at the rim
    tint = np.array((0.16, 0.34, 0.42))
    alpha = 0.12 + 0.5 * (1 - C.smoothstep(0.015, 0.14, edge))
    sheen = np.exp(-((v - 0.78 + 0.25 * (u - 0.5) ** 2) / 0.05) ** 2) * 0.35
    albedo = np.dstack([np.broadcast_to(tint[i], (h, w)) + sheen for i in range(3)] + [alpha + sheen * 0.4])
    emit = np.zeros((h, w, 3))
    cyan = np.array((0.32, 0.93, 1.0))
    line = (1 - C.smoothstep(0.0, 0.006, np.abs(edge - 0.045))) * 0.35
    ticks = ((np.abs(v - 0.86) < 0.006) & (u > 0.12) & (u < 0.3)) | ((np.abs(v - 0.86) < 0.006) & (u > 0.7) & (u < 0.88))
    ticks |= ((np.abs(u - 0.12) < 0.0035) & (v > 0.7) & (v < 0.86)) | ((np.abs(u - 0.88) < 0.0035) & (v > 0.7) & (v < 0.86))
    marks = ((v > 0.12) & (v < 0.16) & (((u * 40) % 1.0) < 0.12) & (u > 0.38) & (u < 0.62))
    glow = np.maximum(line, ticks * 0.6 + marks * 0.45)
    emit = glow[..., None] * cyan
    os.makedirs(out_dir, exist_ok=True)
    paths = {}
    for key, arr in (("Albedo", albedo), ("Emission", emit)):
        p = os.path.join(out_dir, f"{VISOR}_{key}.png")
        rgba = arr if arr.shape[-1] == 4 else np.dstack([arr, np.ones((h, w))])
        C.array_to_image(rgba.astype(np.float32), p, colorspace="sRGB")   # sRGB values, row 0 = v 0 (Blender order)
        paths[key] = p
    return paths


def build_visor(paths, nu=28, nv=14):
    """The glass: a lens over the opening, its rim tucked 2 mm inside the shell, its middle 2 mm proud of it."""
    verts, uvs, faces = [], [], []
    m = 3.0
    for j in range(nv + 1):
        for i in range(nu + 1):
            u, v = i / nu, j / nv
            th = -(WIN_W + m) + 2 * (WIN_W + m) * u
            ph = (win_bot(th) - m) + ((win_top(th) + m) - (win_bot(th) - m)) * v
            bump = max(0.0, 1 - (2 * u - 1) ** 2) * math.sin(math.pi * v)
            p = Vector(HELM_C) + helm_local(th, ph) * (1.0 - 0.022 + 0.034 * bump)
            verts.append(tuple(p))
            uvs.append((u, v))
    for j in range(nv):
        for i in range(nu):
            a = j * (nu + 1) + i
            faces.append((a, a + 1, a + nu + 2, a + nu + 1))
    me = bpy.data.meshes.new(VISOR)
    me.from_pydata(verts, [], faces)
    me.update()
    layer = me.uv_layers.new(name="UVMap")
    layer.data.foreach_set("uv", np.array([uvs[li] for p in me.polygons for li in p.vertices], np.float32).ravel())
    # Normals out (towards the viewer in front of him).
    if me.polygons[len(me.polygons) // 2].normal.y > 0:
        for p in me.polygons:
            p.flip()
        me.update()
    obj = C.link(bpy.data.objects.new(VISOR, me))
    C.shade_smooth(me)
    obj.data.materials.append(visor_material(paths))
    obj.visible_shadow = False
    return obj


def visor_material(paths):
    mat = C.new_material(VISOR + "_Mat")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    alb = nt.nodes.new("ShaderNodeTexImage")
    alb.image = bpy.data.images.load(paths["Albedo"], check_existing=True)
    em = nt.nodes.new("ShaderNodeTexImage")
    em.image = bpy.data.images.load(paths["Emission"], check_existing=True)
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.2
    mix = nt.nodes.new("ShaderNodeMath")
    mix.operation = "MAXIMUM"
    nt.links.new(alb.outputs["Alpha"], mix.inputs[0])
    nt.links.new(lw.outputs["Fresnel"], mix.inputs[1])
    nt.links.new(mix.outputs[0], bsdf.inputs["Alpha"])
    nt.links.new(em.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 2.0
    bsdf.inputs["Roughness"].default_value = 0.04
    bsdf.inputs["Specular IOR Level"].default_value = 0.8
    try:
        mat.surface_render_method = "BLENDED"
    except AttributeError:
        mat.blend_method = "BLEND"
    try:
        mat.use_transparency_overlap = True
    except AttributeError:
        pass
    return mat


def rigid_on_head(obj, arm):
    for vg in list(obj.vertex_groups):
        obj.vertex_groups.remove(vg)
    g = obj.vertex_groups.new(name="Head")
    g.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm


def lod_copy(obj, name, ratio):
    me = obj.data.copy()
    o = C.link(bpy.data.objects.new(name, me))
    if o.data.shape_keys:
        o.shape_key_clear()
    if ratio < 1.0:
        mod = o.modifiers.new("Dec", "DECIMATE")
        mod.ratio = ratio
        C.activate(o)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.visible_shadow = obj.visible_shadow
    return o


# ------------------------------------------------------------------------------------------------------------
# Build, export, previews
# ------------------------------------------------------------------------------------------------------------

EXTRAS = (FACE, VISOR)


def extra_objects():
    return [bpy.data.objects[n] for n in (FACE, VISOR, FACE_LOD, VISOR_LOD) if n in bpy.data.objects]


def export_all():
    arm = bpy.data.objects[NAME + "_Rig"]
    objs = [arm, bpy.data.objects[NAME]] + ([bpy.data.objects[BM.LOD_NAME]] if BM.LOD_NAME in bpy.data.objects else []) + extra_objects()
    for o in objs[1:]:
        S.drop_layer_attr(o)
    arm.animation_data.action = bpy.data.actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), objs, animated=True)


def patch_report(path, face_paths, visor_paths, face_obj, visor_obj):
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["round"] = ROUND
    rep["sockets"] = sorted(BM.SOCKETS)
    rep["brand"] = {"logo": os.path.relpath(M4.LOGO, C.REPO).replace("\\", "/"), "default_variant": BR.DEFAULT,
                    "where": "tabard front and back (painted), raised patch on the left pauldron (star lit)"}
    rep["jet"] = jet_report()
    rep.setdefault("events", {}).update({"Jet_Takeoff": TAKEOFF_EVENTS, "Jet_Land": LAND_EVENTS})
    rel = lambda p: os.path.basename(p)  # noqa: E731
    rep["face"] = {
        "mesh": FACE, "far_mesh": FACE_LOD, "bone": "Head (rigid, weight 1)", "tris": C.tri_count(face_obj),
        "maps": {k: rel(p) for k, p in face_paths.items()}, "low_maps": "Low/" + FACE + "_<Map>.png (512)",
        "mask": "R AO (1), G smoothness (skin ~0.38, eyes 0.88), B/A 0 (no detail normals)",
        "shape_keys": {"Blink": "upper lids closed (0..1); optional: a blink every 3-6 s for 0.15 s"},
        "material": "the hero's lit shader (Game/UnitLit) with these maps; opaque; emission 1 (iris catchlight only)",
        "source": "MPFB 2 (CC0 assets: middleage_caucasian_male skin, high-poly eyes, bluegreen iris), spencer_face.py",
        "fit": "only skin seen through the opening is kept; it keeps %.1f mm from the helmet and %.0f mm from the neck, "
               "collar and shoulders in every keyed clip; spencer_face_check.py: no overlap in any clip" % (FIT_GAP * 1000, NECK_GAP * 1000)}
    rep["visor"] = {
        "mesh": VISOR, "far_mesh": VISOR_LOD, "bone": "Head (rigid, weight 1)", "tris": C.tri_count(visor_obj),
        "maps": {k: rel(p) for k, p in visor_paths.items()},
        "material": "URP Lit (or Game/UnitLit) TRANSPARENT, alpha blend, base map = Mercer_Visor_Albedo (alpha = opacity "
                    "0.12 in the middle .. 0.62 at the rim), smoothness 0.95, metallic 0, emission map Mercer_Visor_Emission "
                    "x2 (faint HUD ticks), cast shadows off, receive shadows on, render queue after the opaque body; "
                    "optional fresnel: more opaque at grazing angles",
        "single_sided": "normals face out; back faces culled"}
    rep["helmet"] = "shell with a visor opening (dark liner inside, gasket on the rim); quad-tube NVG flipped up on a brow mount"
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)


def main():
    args = C.cli_args()
    if not os.path.exists(M4.LOGO):
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
    clips.update(jet_clip_fns())
    actions = C.key_clips(poser, clips)
    switch = M3.switch_check(poser)
    # The face is fitted after the clips exist: the neck, collar and shoulders are sampled in every clip.
    tex_dir = os.path.join(C.EXPORTS, NAME)
    face, face_paths = build_face(args.get("face-cache") or FACE_CACHE, tex_dir, asset.low, arm, list(actions.values()))
    visor_paths = visor_textures(tex_dir)
    visor = build_visor(visor_paths)
    for o in (face, visor):
        rigid_on_head(o, arm)
    C.set_action(arm, actions["Idle"], 0)
    objs = [arm, asset.low]
    lod = None
    if not fast:
        lod = M3.build_lod(asset, arm, builder, index)
        objs.append(lod)
        for src, name in ((face, FACE_LOD), (visor, VISOR_LOD)):
            rigid_on_head(lod_copy(src, name, FACE_LOD_RATIO if src is face else 1.0), arm)
    for o in objs[1:]:
        S.drop_layer_attr(o)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), objs + extra_objects(), animated=True)
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
        # The reactions step re-exports the body, rig and LOD only: export again with the face and the visor.
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
        export_all()
    path = os.path.join(C.EXPORTS, NAME, NAME + "_report.json")
    M3.patch_report(path, switch)
    patch_report(path, face_paths, visor_paths, bpy.data.objects[FACE], bpy.data.objects[VISOR])
    if args.get("norender"):
        return
    import spencer_previews5 as SP5
    SP5.render_all(os.path.join(C.BLEND, NAME + ".blend"), out, args.get("prefix", "pass4"), M3.ORIGINAL_BLEND, fast=fast,
                   draw_clips=list(M3.DRAW_CLIPS.items()))


if __name__ == "__main__":
    main()
