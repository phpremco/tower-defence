"""Spencer, pass 2 (Remco 2026-11-08 on pass 1: "Spencer mag nog wel cooler hoor", plus better weapon draw/holster
animations and natural hands at rest).

Overrides build_mercer2.py's export on the SAME rig, bone names, socket names, clip names and export names
(ArtSource/Exports/Mercer/Mercer.fbx, Mercer_<Map>.png, Mercer_report.json; the asset id stays "Mercer").

Look (a hero silhouette from the high game camera, not a generic trooper):
- Helmet: a tall yellow crest fin from the brow over the top to a swept tail, a cyan light line along its edge, a
  V-shaped (angry brow) cyan visor slit; the whip antenna is gone.
- Shoulders: broad three-tier pauldrons (white cap, cobalt lames, yellow rims) with a cyan light strip along the outer
  edge; rank chevrons on the left one.
- Chest: a bold yellow chevron over the cuirass with a cyan line under it and a bigger lit core.
- Pack: a cobalt reactor pack with a glowing cyan ring on its back, two thruster nozzles (lit), two swept vanes from its
  top corners (white, yellow tips, cyan leading edge) and a sword sleeve on its top right: the hilt now rides pommel-up
  over the right shoulder (signature silhouette, and a natural over-the-shoulder draw).
Hands: curved finger groups (baked joint bends), a slimmer palm and knuckle plate; the Fingers bone curls a little less
to make up for the baked bend. Rest poses: elbows less bent, wrists relaxed (hands hang along the thigh, palm in).

Rig changes: none in names or hierarchy. The `Back` socket moved to the sleeve on the pack's top right (pommel up,
blade axis down into the sleeve), so the hilt sits there when Unity seats it at identity.

New clips (in place, 30 fps; see the "draw_stow" section of Mercer_report.json for every event):
- Draw_Pistol (18): Idle frame 0 -> Shoot frame 0 (two-handed aim). grab = 7: pistol Holster_R -> Weapon_R.
- Stow_Pistol (22): Shoot frame 0 -> Idle frame 0. release = 11: pistol Weapon_R -> Holster_R.
- Draw_Blade (22): Idle frame 0 -> Slash frame 0 (guard). grab = 9: hilt Back -> Weapon_R; ignite = 16: blade on.
- Stow_Blade (24): Slash frame 0 -> Idle frame 0. blade_off = 4: blade off; release = 15: hilt Weapon_R -> Back.
At each switch frame the hand socket and the target socket coincide (checked; the residual is in the report), so a
reparent with an identity local transform does not pop.

Run: blender --background --factory-startup --python Tools/Blender/build_mercer3.py --
     [--preview <dir>] [--norender] [--fast] [--stage <dir>] [--prefix pass2]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

import build_mercer as BM  # noqa: E402
import build_mercer2 as M2  # noqa: E402
import build_mercer_reactions as RX  # noqa: E402
import build_mercer_weapons as W  # noqa: E402
import common as C  # noqa: E402
import suit as S  # noqa: E402
from build_mercer import FWD, SIDE, R, STANCE, UP, along, around, boxy, dome_patch, euler, frame, limb_shell, nrm, rad  # noqa: E402
from build_mercer2 import (CYAN, CYAN_SOFT, DARK, DEEP, GROOVE, YELLOW, YELLOW_CLEAN, YELLOW_GLOW, bplate, fabric, front,  # noqa: E402
                           hazard, metal, plate, quilted, ribs, rubber)
from build_mercer_reactions import KeyClip, idle0  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
LOD_NAME = BM.LOD_NAME
GAME_TRIS = 17500
LOD_TRIS = 6000
TEX, LOW_TEX = 2048, 1024
ORIGINAL_BLEND = C.BLEND

# --- Palette: a touch more saturated cobalt and a warmer, brighter yellow so the colour blocks read at play zoom ---
S.PAL.bplate_top = np.array((0.2, 0.45, 0.95))
S.PAL.bplate_side = np.array((0.13, 0.32, 0.76))
S.PAL.yellow = np.array((1.0, 0.76, 0.12))
LIGHT = dict(emit=S.PAL.cyan, glow=1.5, rough=0.3, family={})
YELLOW_PAINT = dict(paint=S.PAL.yellow, wear=0.15, rough=0.45)


def battle_wear(seed=13):
    """On top of suit.weathering: chipped paint and burn marks on the plates (a veteran's armour, not a parade suit)."""
    base = S.weathering()

    def extra(s, pid):
        base(s, pid)
        lit = s.emit.max(axis=1) < 0.05
        plate_ = np.maximum(s.fam[:, 1], 0.0)
        chips = C.smoothstep(0.58, 0.66, C.value_noise(s.p, 55.0, seed)) * plate_ * lit
        s.albedo = C.lerp3(s.albedo, S.PAL.metal_side, (chips * 0.85)[:, None])
        burn = C.smoothstep(0.5, 0.75, C.value_noise(s.p, 9.0, seed + 1)) * C.smoothstep(0.3, 0.7, C.value_noise(s.p, 60.0, seed + 2))
        s.albedo = C.lerp3(s.albedo, s.albedo * 0.45, (burn * 0.55 * lit)[:, None])
        s.rough = np.clip(s.rough + chips * 0.2, 0.04, 0.95)
    return extra


def light(strength=1.6):
    return S.light(strength=strength)


def shell_point(centre, radii, rot, d, offset=0.0):
    """A point on (or `offset` above) the ellipsoid of a dome patch, in the patch's own local direction `d`."""
    d = Vector(d).normalized()
    local = Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * (1 + offset / min(radii))
    return Vector(centre) + (rot or Matrix.Identity(3)) @ local


def thin_plate(b, name, root, span, chord, chord_taper, thick, chain, style, nu=12, nv=6, bow=0.0):
    """A flat plate (vane, fin) from `root` along `span` with a chord vector that shrinks by `chord_taper` at the tip;
    `bow` bends the span (a fraction of its length) towards the chord's normal."""
    span, chord = Vector(span), Vector(chord)
    n = span.cross(chord).normalized()

    def surf(side):
        def fn(u, v):
            c = chord * (1 - chord_taper * u)
            t = thick * (1 - 0.5 * u) * (1 - 0.4 * abs(2 * v - 1))
            return Vector(root) + span * u + c * v + n * (bow * span.length * math.sin(math.pi * u) + side * t)
        return fn
    b.slab(name, surf(1.0), surf(-1.0), nu, nv, chain=chain, style=style)
    return b.parts[-1]


SCAR = dict(groove=0.0022, paint=S.PAL.metal_side, wear=0.0, rough=0.55)
TALLY = dict(paint=S.PAL.plate_top, wear=0.25, rough=0.5)


def claw(origin, along_dir, across_dir, length, count=3, spacing=0.013, width=0.0028):
    """Battle damage: `count` parallel claw gouges (world space, Blender axes) that taper at both ends."""
    o, a, n = np.array(origin), np.array(nrm(along_dir)), np.array(nrm(across_dir))

    def mask(c):
        rel = c.p - o
        s_, q = rel @ a, rel @ n
        taper = C.smoothstep(0.0, 0.15, s_ / length) * (1 - C.smoothstep(0.7, 1.0, s_ / length))
        m = np.zeros(len(c.p))
        for k in range(count):
            m = np.maximum(m, S.line(q - (k - (count - 1) / 2) * spacing, 0.0, width))
        return m * taper
    return mask


def tallies(origin, across_dir, up_dir, count=5, spacing=0.012, height=0.034, width=0.0028):
    """Kill markings: `count` painted strokes in a row, the first five struck through."""
    o, a, u = np.array(origin), np.array(nrm(across_dir)), np.array(nrm(up_dir))

    def mask(c):
        rel = c.p - o
        x, y = rel @ a, rel @ u
        inside = S.band(y, 0.0, height / 2, soft=0.1)
        m = np.zeros(len(c.p))
        for k in range(count):
            m = np.maximum(m, S.line(x - k * spacing, 0.0, width) * inside)
        if count >= 5:
            m = np.maximum(m, S.line(y - (x - 2 * spacing) * 0.6, 0.0, width) * S.band(x, 2 * spacing, 2.6 * spacing, soft=0.1))
        return m
    return mask


# ------------------------------------------------------------------------------------------------------------
# Head: crest fin, chevron visor
# ------------------------------------------------------------------------------------------------------------

HELM_C = M2.HELM_C
HELM_R = M2.HELM_R  # Blender order (x, fwd, up)
FIN_E0, FIN_E1 = 32.0, 178.0  # elevation of the fin's root from the front (0) over the top (90) to the back (180)


def fin_root(e):
    """A point on the helmet's mid-plane at elevation `e` (radians) and the outward direction there."""
    up, fwd = math.sin(e), math.cos(e)
    p = HELM_C + G(0, HELM_R[2] * up * 0.96, HELM_R[1] * fwd * 0.96)
    out = nrm(G(0, up / HELM_R[2], fwd / HELM_R[1]))
    return p, out


def fin_height(u):
    """Low over the brow, tallest behind the crown, a swept tail past the back."""
    return 0.014 + 0.08 * math.sin(math.pi * min(1.0, u / 0.82) * 0.5) ** 1.4 * (1 - 0.35 * max(0.0, (u - 0.82) / 0.18))


def fin_point(u, v, side, half=0.0075):
    e = math.radians(FIN_E0 + (FIN_E1 - FIN_E0) * u)
    p, out = fin_root(e)
    sweep = G(0, -0.01, -0.035) * (u ** 2) * v  # the top edge leans back
    t = half * (1 - 0.55 * v)
    return p + out * fin_height(u) * v + sweep + G(side * t, 0, 0)


def head(b):
    # A heavy, angular helm (a rounded box, not a ball).
    b.ellipsoid("Helmet", HELM_C, HELM_R, bone="Head", segments=(48, 26), deform=boxy(0.74),
                style=plate([(lambda c: S.line(np.abs(c.u[:, 0]), 0.36, 0.02) * C.smoothstep(0.2, 0.5, c.u[:, 2]), GROOVE),
                             (lambda c: S.line(c.u[:, 2], -0.25, 0.025) * C.smoothstep(-0.2, 0.4, c.u[:, 1]), GROOVE),
                             (lambda c: S.line(c.u[:, 1], 0.55, 0.02) * C.smoothstep(-0.3, 0.1, c.u[:, 2]), GROOVE),
                             # Two yellow stripes either side of the fin.
                             (lambda c: S.band(np.abs(c.u[:, 0]), 0.2, 0.05) * C.smoothstep(0.35, 0.6, c.u[:, 2]), YELLOW_PAINT),
                             (lambda c: ribs(c.u[:, 0], 0.1, 0.3) * S.band(c.u[:, 2], -0.48, 0.07) * (c.u[:, 1] > 0.5), DEEP),
                             (claw(HELM_C + G(0.05, 0.12, 0.05), G(0.5, -0.45, -0.6), G(0.6, 0.8, 0.0), 0.13), SCAR)],
                            seed=4))
    # Crest fin: a thick yellow blade plate on the helmet's mid-line.
    b.slab("Fin", lambda u, v: fin_point(u, v, 1.0), lambda u, v: fin_point(u, v, -1.0), 24, 5, chain=["Head"],
           style=plate([(lambda c: np.ones(len(c.p)), YELLOW_PAINT)], seed=61))
    edge = [fin_point(u, 1.0, 0.0) + G(0, 0.002, 0) for u in np.linspace(0.08, 0.97, 9)]
    b.tube("FinLight", edge, [0.0045] * len(edge), sides=8, chain=["Head"], style=light(1.8), cap=0.6)
    # Brow plate over the visor (yellow glints at its corners as in pass 1).
    dome_patch(b, "Brow", HELM_C, HELM_R, None, lambda a, v: (a * 0.92, -1.0, 0.4 + 0.62 * (v + 1) / 2), 0.016, "Head",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), GROOVE),
                      (lambda c: S.band(np.abs(c.u[:, 0]), 0.42, 0.06) * S.band(c.u[:, 2], 0.42, 0.06), YELLOW_GLOW)], seed=32),
               nu=22, nv=10, outline=lambda a, v: (a * (1.0 - 0.3 * (v + 1) / 2), v))
    for name, sd in SIDE.items():
        sx = sd.sx
        dome_patch(b, "Cheek", HELM_C, HELM_R, None,
                   lambda a, v, sx=sx: (sx * (1.0 - 0.55 * (a + 1) / 2), -(0.05 + 0.95 * (a + 1) / 2), -0.15 - 0.8 * (v + 1) / 2),
                   0.016, "Head", bplate([(lambda c: S.line(c.u[:, 2], -0.72, 0.03), GROOVE)], seed=33 + (sx > 0)),
                   nu=16, nv=10)
        b.tube("Ear", [HELM_C + G(0.112 * sx, -0.02, -0.005), HELM_C + G(0.148 * sx, -0.02, -0.005)], [0.044, 0.038], sides=18,
               chain=["Head"], style=metal([(lambda c: S.band(c.t, 0.95, 0.12), GROOVE),
                                            (lambda c: S.band(c.t, 1.0, 0.06) * C.smoothstep(0.3, 0.8, np.abs(c.n[:, 0])), CYAN_SOFT)],
                                           seed=6), cap=0.35)
        # Swept ear vane: a short white blade from the ear pod back along the helmet (wings of the crest).
        thin_plate(b, "EarVane", HELM_C + G(0.15 * sx, 0.0, -0.03), G(0.018 * sx, 0.045, -0.1), G(0, -0.04, -0.012), 0.55, 0.005,
                   ["Head"], plate(seed=62 + (sx > 0)), nu=8, nv=4)

    # Visor: smoked glass with a V-shaped cyan slit (an angry brow) and a faint inner glow.
    def chevron_slit(c):
        x = np.abs(c.u[:, 0])
        return S.band(c.u[:, 2], -0.02 + 0.5 * x, 0.15) * C.smoothstep(0.3, 0.75, -c.u[:, 1]) * (1 - C.smoothstep(0.66, 0.82, x))
    b.ellipsoid("Visor", G(0, 1.455, 0.02), R(0.126, 0.06, 0.13), bone="Head", segments=(48, 24), style=S.glass(chevron_slit, glow=0.14))
    b.ellipsoid("Chin", G(0, 1.378, 0.072), R(0.062, 0.036, 0.052), euler(pitch=14), bone="Head", deform=boxy(0.55), segments=(28, 14),
                style=metal([(lambda c: ribs(c.u[:, 0], 0.22, 0.25) * (c.u[:, 1] < -0.3) * S.band(c.u[:, 2], 0.0, 0.5), DEEP),
                             (lambda c: S.band(c.u[:, 2], 0.75, 0.2), YELLOW)], seed=5))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Filter", [G(0.05 * sx, 1.372, 0.084), G(0.086 * sx, 1.356, 0.052)], [0.021, 0.019], sides=14, chain=["Head"], cap=0.5,
               style=metal([(lambda c: ribs(c.t, 0.25, 0.2), GROOVE), (lambda c: S.band(c.t, 0.92, 0.1), YELLOW)], seed=34))
    b.ellipsoid("Comms", HELM_C + G(-0.146, -0.005, -0.03), R(0.03, 0.05, 0.062), bone="Head", deform=boxy(0.4), segments=(18, 10),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.2, 0.06), GROOVE),
                             (lambda c: S.band(c.u[:, 1], -0.75, 0.12) * S.band(c.u[:, 2], -0.3, 0.2), CYAN)], seed=35))
    b.tube("HelmLamp", [HELM_C + G(0.142, 0.01, -0.01), HELM_C + G(0.144, 0.012, 0.06)], [0.019, 0.021], sides=14, chain=["Head"], cap=0.3,
           style=metal([(lambda c: S.band(c.t, 1.0, 0.1), dict(emit=np.array((1.0, 0.93, 0.72)), glow=1.4, rough=0.2, family={})),
                        (lambda c: S.band(c.t, 0.2, 0.08), YELLOW)], seed=37))


# ------------------------------------------------------------------------------------------------------------
# Torso: as pass 1, with a chest chevron and a bigger core
# ------------------------------------------------------------------------------------------------------------

CHEST_C, CHEST_R = M2.CHEST_C, M2.CHEST_R
ABS_C, ABS_R = M2.ABS_C, M2.ABS_R


def chest_chevron(c):
    x = np.abs(c.u[:, 0])
    return S.band(c.u[:, 2], 0.14 + 0.72 * x, 0.085) * (x < 0.74) * front(c, -0.2)


def chest_line(c):
    x = np.abs(c.u[:, 0])
    return S.band(c.u[:, 2], -0.02 + 0.72 * x, 0.024) * (x > 0.2) * (x < 0.7) * front(c, -0.3)


def torso(b):
    b.ellipsoid("Pelvis", G(0, 0.9, 0.0), R(0.15, 0.105, 0.112), bone="Hips", style=quilted(), segments=(36, 18))
    b.ellipsoid("Abdomen", ABS_C, ABS_R, bone="Spine1", style=quilted(), segments=(36, 18))
    b.parts[-1].chain = ["Spine1", "Hips"]
    b.ellipsoid("Chest", CHEST_C, CHEST_R, bone="Spine2", style=quilted(), segments=(40, 20))
    b.parts[-1].chain = ["Spine2", "Spine1"]
    dome_patch(b, "ChestPlate", CHEST_C, CHEST_R, euler(pitch=-6), lambda a, v: (a * 2.0, -1.0, v * 0.95 + 0.2), 0.022, "Spine2",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.025) * front(c) * (c.u[:, 2] < 0.1), DEEP),
                      (lambda c: S.line(c.u[:, 2], -0.4, 0.022) * front(c), GROOVE),
                      (chest_chevron, YELLOW_PAINT),
                      (chest_line, LIGHT),
                      (claw(CHEST_C + G(-0.13, 0.07, 0.13), G(0.7, -0.7, 0.0), G(0.7, 0.7, 0.0), 0.18, spacing=0.016), SCAR)],
                     seed=1),
               outline=lambda a, v: (a * (0.72 + 0.28 * (v + 1) / 2), v), nu=32, nv=20)
    core = G(0, 1.168, 0.156)
    b.ellipsoid("ChestCore", core, R(0.032, 0.032, 0.013), bone="Spine2", style=S.light(strength=2.0), segments=(20, 10))
    b.ring("CoreRing", core + G(0, 0, -0.004), FWD, 0.037, 0.01, arcs=3, sides=8, chain=["Spine2"],
           style=metal([(lambda c: ribs(c.p[:, 0] + c.p[:, 2], 0.012, 0.25), GROOVE),
                        (lambda c: C.smoothstep(0.3, 0.8, -c.n[:, 1]), YELLOW)], seed=38))
    for z0, seed in ((0.3, 40), (-0.08, 41)):
        dome_patch(b, "AbPlate", ABS_C, R(0.134 * 1.03, 0.1 * 1.03, 0.106 * 1.03), None,
                   lambda a, v, z0=z0: (a * 1.45, -1.0, z0 + v * 0.16), 0.012, None,
                   bplate([(lambda c: S.line(c.u[:, 0], 0.0, 0.02), GROOVE)], seed=seed), chain=["Spine1"], nu=20, nv=8,
                   outline=lambda a, v: (a * 0.92, v))
    dome_patch(b, "BackPlate", ABS_C, R(0.134 * 1.03, 0.1 * 1.03, 0.106 * 1.03), None, lambda a, v: (a * 1.3, 1.0, 0.05 + v * 0.42), 0.013,
               None, bplate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), DEEP), (lambda c: S.line(c.u[:, 2], 0.0, 0.03), GROOVE),
                             (lambda c: S.band(c.u[:, 2], -0.4, 0.06), YELLOW)], seed=60),
               chain=["Spine1"], nu=20, nv=10, outline=lambda a, v: (a * (0.75 + 0.25 * (v + 1) / 2), v))
    b.ring("Collar", G(0, 1.312, -0.004), Vector((0, 0, 1)), 0.09, 0.033, arcs=3, sides=10, chain=["Spine2"],
           style=metal([(lambda c: C.smoothstep(0.5, 0.9, c.n[:, 2]) * S.band(c.p[:, 2], 1.343, 0.008), CYAN),
                        (lambda c: ribs(np.arctan2(c.p[:, 0], c.p[:, 1]), 0.35, 0.12), GROOVE)], seed=39))
    b.tube("Neck", [G(0, 1.29, -0.004), G(0, 1.35, 0.0), G(0, 1.405, 0.004)], [0.062, 0.058, 0.06], sides=14,
           chain=["Neck"], style=rubber([(lambda c: (np.abs(np.sin(c.t * math.pi * 5)) < 0.2).astype(float), dict(groove=0.002))]))
    b.ellipsoid("Belt", G(0, 0.958, 0.0), R(0.158, 0.034, 0.12), bone="Hips", deform=boxy(0.6), segments=(40, 12),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.45, 0.06) + S.line(c.u[:, 2], -0.45, 0.06), GROOVE)], seed=42))
    b.ellipsoid("Buckle", G(0, 0.958, 0.122), R(0.044, 0.032, 0.017), bone="Hips", deform=boxy(0.35), segments=(16, 8),
                style=plate([(lambda c: S.band(c.u[:, 2], 0.0, 0.4), YELLOW_CLEAN), (lambda c: S.line(c.u[:, 0], 0.0, 0.08), DEEP),
                             (lambda c: S.band(c.u[:, 0], 0.0, 0.18) * S.band(c.u[:, 2], 0.0, 0.25) * (c.u[:, 1] < -0.5), LIGHT)]))
    pouches = [(G(0.088, 0.94, 0.108), R(0.034, 0.044, 0.026), 0), (G(-0.088, 0.94, 0.108), R(0.034, 0.044, 0.026), 0),
               (G(0.148, 0.94, 0.03), R(0.026, 0.046, 0.04), 30), (G(0.0, 0.95, -0.125), R(0.06, 0.042, 0.028), 0)]
    for centre, radii, yaw in pouches:
        b.ellipsoid("Pouch", centre, radii, euler(yaw=yaw), bone="Hips", deform=boxy(0.3), segments=(16, 10),
                    style=fabric([(lambda c: S.band(c.u[:, 2], 0.62, 0.14), dict(groove=0.002)),
                                  (lambda c: S.band(c.u[:, 2], 0.85, 0.15) * S.band(c.u[:, 0], 0.0, 0.25), DARK)]))
    for k, z in enumerate((0.075, 0.02)):
        p0 = G(-0.152, 0.92, z)
        b.tube("Grenade", [p0, p0 + G(0, 0.07, 0)], [0.019, 0.019], sides=12, chain=["Hips"], cap=0.6,
               style=metal([(lambda c: S.band(c.t, 0.78, 0.07), YELLOW_CLEAN), (lambda c: S.band(c.t, 0.35, 0.05), GROOVE)], seed=43 + k))
    for name, sd in SIDE.items():
        sx = sd.sx
        out_front = nrm(FWD * 0.55 + Vector((sx, 0, 0)))
        limb_shell(b, "Tasset", sd.hip + G(0, 0.04, 0), sd.knee, 0.118, 0.1, out_front, 62, 0.0, 0.32, 0.012, [f"Thigh_{name}"],
                   bplate([(lambda c: S.band(along(c), 0.95, 0.07), YELLOW_PAINT), (lambda c: S.line(around(c), 0.0, 3.0), GROOVE)],
                          seed=44 + (sx > 0)))


TABARD_TOP = 0.935


def tabard(b, z_front, width_top, width_bottom, length, bulge, seed):
    """A heavy cloth strip hanging from the belt with a torn, ragged hem; `z_front` > 0 in front, < 0 behind."""
    sgn = 1.0 if z_front > 0 else -1.0
    tears = np.random.default_rng(seed).uniform(0.0, 1.0, 8)

    def hem(u):
        i = u * 6
        k = int(min(5, i))
        f = i - k
        jag = tears[k] * (1 - f) + tears[k + 1] * f
        notch = 0.13 * max(0.0, 1 - abs((u - 0.68) / 0.05)) + 0.1 * max(0.0, 1 - abs((u - 0.27) / 0.04))
        return length * (1 - 0.12 * jag - notch)

    def surf(side):
        def fn(u, v):
            w = width_top + (width_bottom - width_top) * v
            y = TABARD_TOP - hem(u) * v
            z = z_front + sgn * (bulge * math.sin(math.pi * min(1.0, v * 1.6)) - 0.012 * v) + 0.0035 * side
            return G((u - 0.5) * w, y, z)
        return fn

    def trim(c):
        return C.smoothstep(width_top * 0.36, width_top * 0.42, np.abs(c.p[:, 0]))

    def emblem(c):
        x = np.abs(c.p[:, 0])
        return S.band(c.p[:, 2], 0.84 - 1.2 * x, 0.014) * (x < 0.045)
    cloth = S.styled("suit", [(lambda c: np.ones(len(c.p)), dict(paint=S.PAL.bplate_side * 0.8, wear=0.0, rough=0.85)),
                              (trim, dict(paint=S.PAL.yellow, wear=0.45, rough=0.7)),
                              (lambda c: S.band(c.p[:, 2], TABARD_TOP - 0.018, 0.014), dict(paint=S.PAL.yellow, wear=0.3)),
                              (emblem, dict(paint=S.PAL.plate_top, wear=0.35)),
                              (lambda c: C.smoothstep(0.66, 0.52, c.p[:, 2]) * 0.65, dict(paint=S.PAL.suit_under, wear=0.0))], seed=seed)
    b.slab("Tabard", surf(1.0), surf(-1.0), 14, 12, chain=["Hips"], style=cloth)


# ------------------------------------------------------------------------------------------------------------
# Reactor pack, vanes and the sword sleeve
# ------------------------------------------------------------------------------------------------------------

PACK_C, PACK_R = G(0, 1.15, -0.2), R(0.152, 0.172, 0.088)
# The hilt rides pommel-up in a sleeve on the pack's top right (figure's right = -x): blade axis down into the sleeve.
SLEEVE_MOUTH = G(-0.112, 1.372, -0.262)
SLEEVE_DIR = nrm(G(0.45, -0.88, -0.12))
SHEATH_LEN = 0.56
BACK_GRIP = SLEEVE_MOUTH - SLEEVE_DIR * 0.06
BACK_EDGE = nrm(G(-0.35, 0.0, -1.0))
VANES = False  # tried: the swept vanes read as broken plates at play zoom; the sheath carries the back


def pack(b):
    b.ellipsoid("Pack", PACK_C, PACK_R, bone="Spine2", deform=boxy(0.3), segments=(36, 18),
                style=bplate([(lambda c: S.line(c.u[:, 2], 0.42, 0.02) + S.line(c.u[:, 2], -0.5, 0.02), GROOVE),
                              (lambda c: S.line(np.abs(c.u[:, 0]), 0.82, 0.03) * (c.u[:, 1] > 0.3), GROOVE),
                              (lambda c: hazard(c, 0.024, (1, 0, -1)) * S.band(c.u[:, 2], -0.75, 0.08) * (c.u[:, 1] > 0.4), YELLOW_PAINT),
                              (lambda c: C.smoothstep(0.88, 0.95, np.abs(c.u[:, 0])) * S.band(c.u[:, 2], 0.0, 0.55), YELLOW_PAINT),
                              (lambda c: (c.u[:, 1] < 0.1).astype(float), dict(paint=S.PAL.metal_top, wear=0.0))], seed=7))
    # The cell bar across the top (shorter: the sleeve takes the right end).
    a, z = G(0.138, 1.338, -0.2), G(-0.035, 1.338, -0.2)
    b.tube("CoreBar", [a, z], [0.04, 0.04], sides=20, chain=["Spine2"], cap=0.2,
           style=metal([(lambda c: (ribs(c.t, 0.25, 0.32) < 0.5) * S.band(c.t, 0.5, 0.42) * C.smoothstep(-0.1, 0.4, c.n[:, 2] - c.n[:, 1] * 0.5),
                         LIGHT), (lambda c: S.band(c.t, 0.06, 0.03) + S.band(c.t, 0.94, 0.03), GROOVE)], seed=46))
    b.ellipsoid("CoreCap", a, R(0.02, 0.048, 0.048), bone="Spine2", deform=boxy(0.5), segments=(16, 10),
                style=metal([(lambda c: (np.abs(c.u[:, 0]) > 0.3).astype(float), YELLOW)], seed=47))
    # The sword sheath: a long armoured housing diagonally across the back, from the hilt over the right shoulder down
    # to the left hip, a cyan energy slit along its back (the blade's charge) and yellow bands: the hero's big weapon
    # silhouette from the game camera.
    tip = SLEEVE_MOUTH + SLEEVE_DIR * SHEATH_LEN
    b.tube("Sleeve", [SLEEVE_MOUTH - SLEEVE_DIR * 0.012, SLEEVE_MOUTH + SLEEVE_DIR * 0.2, SLEEVE_MOUTH + SLEEVE_DIR * 0.42, tip],
           [0.034, 0.031, 0.027, 0.018], sides=16, chain=["Spine2"], cap=0.9,
           style=plate([(lambda c: S.band(c.t, 0.04, 0.05) + S.band(c.t, 0.5, 0.025) + S.band(c.t, 0.93, 0.04), YELLOW_PAINT),
                        (lambda c: C.smoothstep(0.1, 0.35, -c.n[:, 1]) * (c.t > 0.1), dict(paint=S.PAL.metal_top, wear=0.0)),
                        (lambda c: ribs(c.t * 12, 1.0, 0.08) * (c.t > 0.1), GROOVE)], seed=64))
    slit = [SLEEVE_MOUTH + SLEEVE_DIR * d + G(0, 0, -0.03 + 0.012 * d) for d in np.linspace(0.07, SHEATH_LEN - 0.08, 8)]
    b.tube("SheathLight", slit, [0.0075] * len(slit), sides=8, chain=["Spine2"], style=light(2.0), cap=0.6)
    for d in (0.0, 0.3):
        b.ring("SheathClamp", SLEEVE_MOUTH + SLEEVE_DIR * d, SLEEVE_DIR, 0.036, 0.008, arcs=3, sides=6, chain=["Spine2"], style=metal(seed=67))
    b.ring("SleeveGlow", SLEEVE_MOUTH - SLEEVE_DIR * 0.01, SLEEVE_DIR, 0.024, 0.006, arcs=3, sides=6, chain=["Spine2"], style=light(1.8))
    # Thrusters: two nozzles under the pack, lit inside.
    for name, sd in SIDE.items():
        sx = sd.sx
        top, bot = G(0.078 * sx, 1.005, -0.248), G(0.086 * sx, 0.925, -0.262)
        b.tube("Nozzle", [top, top.lerp(bot, 0.5), bot], [0.026, 0.03, 0.036], sides=16, chain=["Spine2"], cap=0.2,
               style=metal([(lambda c: ribs(c.t, 0.25, 0.2) * (c.t < 0.7), GROOVE), (lambda c: S.band(c.t, 0.95, 0.06), YELLOW_CLEAN)], seed=65))
        b.ellipsoid("NozzleGlow", bot + G(0, -0.004, 0), R(0.028, 0.006, 0.028), bone="Spine2", style=light(2.0), segments=(16, 8))
        b.ellipsoid("PackPouch", G(0.13 * sx, 1.05, -0.21), R(0.034, 0.05, 0.05), bone="Spine2", deform=boxy(0.3), segments=(16, 10),
                    style=fabric([(lambda c: S.band(c.u[:, 2], 0.6, 0.14), dict(groove=0.002))]))
        if VANES:
            # Swept vane from the top corner: white, yellow tip, a cyan leading edge.
            root = G(0.115 * sx, 1.3, -0.262)
            span = G(0.13 * sx, 0.15, -0.09)
            chord = G(0.0, -0.1, -0.03)
            thin_plate(b, "Vane", root, span, chord, 0.55, 0.007, ["Spine2"],
                       plate([(lambda c, r=root, s=span: C.smoothstep(0.72, 0.8, ((c.p - np.array(r)) @ np.array(s)) / (s.length ** 2)),
                               YELLOW_PAINT),
                              (lambda c, r=root, s=span: S.line(((c.p - np.array(r)) @ np.array(s)) / (s.length ** 2), 0.4, 0.02), GROOVE)],
                             seed=66 + (sx > 0)), nu=14, nv=6, bow=0.06)
            edge = [root + span * u + G(0, 0.004, 0.003) for u in np.linspace(0.05, 0.92, 6)]
            b.tube("VaneLight", edge, [0.005] * len(edge), sides=8, chain=["Spine2"], style=light(1.8), cap=0.6)
    b.tube("Hose", [G(0.14, 1.02, -0.2), G(0.158, 0.99, -0.16), G(0.16, 0.965, -0.1)], [0.013, 0.013, 0.013], sides=10,
           chain=["Spine2", "Spine1", "Hips"], cap=0.3,
           style=rubber([(lambda c: ribs(c.t * C.axis_length(c.part), 0.012, 0.3), dict(groove=0.0015))]))


# ------------------------------------------------------------------------------------------------------------
# Shoulders, arms and hands
# ------------------------------------------------------------------------------------------------------------

FINGER_BEND = (16.0, 12.0)    # baked bends at the middle and end joints (degrees)
FINGER_COMP = 14.0            # degrees the Fingers bone curls less at full grip, for the baked bend
PHALANX = (0.46, 0.3, 0.24)


def finger_points(sd, base, length, lateral):
    """Base, then three phalanges, each bent a little further towards the palm; the tip drifts `lateral`."""
    pts = [base]
    p = base.copy()
    ang = 0.0
    for i, share in enumerate(PHALANX):
        d = Quaternion(sd.hand_x, rad(ang)) @ sd.finger_dir
        p = p + d * (length * share) + lateral * (share / 1.0)
        pts.append(p.copy())
        if i < len(FINGER_BEND):
            ang += FINGER_BEND[i]
    return pts


def pauldron(b, name, sd, centre, radii, roll, thick, style, chain, scale=1.0, nu=22, nv=14):
    sx = sd.sx
    rot = euler(roll=roll * sx)

    def dir_fn(a, v):
        return (a * 0.98 * sx, v * 0.95, 1.0)
    dome_patch(b, name, centre, radii, rot, dir_fn, thick, None, style, chain=chain, nu=nu, nv=nv)
    return rot, dir_fn


def arms(b):
    for name, sd in SIDE.items():
        sx = sd.sx
        b.ellipsoid("Yoke", G(0.168 * sx, 1.25, -0.006), R(0.084, 0.07, 0.086), bone=f"Clavicle_{name}", style=fabric(),
                    segments=(24, 12))
        b.parts[-1].chain = [f"Clavicle_{name}", "Spine2"]
        chain = [f"Clavicle_{name}", f"UpperArm_{name}"]
        # Tier 1: a broad white cap with a yellow rim. Asymmetric like a warrior's harness: the left (shield) side is
        # huge, spiked and carries kill tallies; the right stays lower so the sword arm can reach the hilt.
        big = sx > 0
        k = 1.18 if big else 0.92
        cap_c = G((0.222 if big else 0.205) * sx, 1.24 + (0.012 if big else 0.0), -0.008)
        cap_r = R(0.168 * k, 0.15 * k, 0.17 * k)
        feats = [(lambda c, sx=sx: S.line(c.u[:, 0] * sx, 0.3, 0.03), GROOVE),
                 (lambda c, sx=sx: C.smoothstep(0.62, 0.7, c.u[:, 0] * sx), YELLOW_PAINT),
                 (lambda c: C.smoothstep(0.55, 0.65, np.abs(c.u[:, 1])) * C.smoothstep(0.4, 0.6, c.u[:, 2]), YELLOW_PAINT)]
        if big:
            feats += [(tallies(cap_c + G(0.02, 0.07, 0.11), G(1, 0, 0), G(0, 0.55, 0.85), count=6), TALLY),
                      (claw(cap_c + G(-0.07, 0.15, -0.06), G(1, -0.25, 0.4), G(0, 0.9, -0.5), 0.2), SCAR)]
        else:
            feats += [(claw(cap_c + G(0.03, 0.12, 0.08), G(-1, -0.4, -0.2), G(0.2, 0.3, -1), 0.13, count=2), SCAR)]
        rot, dir_fn = pauldron(b, "Pauldron", sd, cap_c, cap_r, 24 if big else 20, 0.03 if big else 0.024, bplate(feats, seed=12), chain,
                               nu=26, nv=16)
        rim = [shell_point(cap_c, cap_r, rot, dir_fn(0.8, v), 0.032) for v in np.linspace(-0.85, 0.85, 7)]
        b.tube("PauldronLight", rim, [0.0065] * len(rim), sides=8, chain=chain, style=light(1.8), cap=0.6)
        if big:
            # A raised yellow ridge along the cap and three swept spikes off it (angular, aggressive from above).
            ridge = [shell_point(cap_c, cap_r, rot, dir_fn(0.15, v), 0.026) for v in np.linspace(-0.8, 0.8, 7)]
            b.tube("Ridge", ridge, [0.012, 0.015, 0.017, 0.018, 0.017, 0.015, 0.012], sides=10, chain=chain, cap=0.6,
                   style=plate([(lambda c: np.ones(len(c.p)), YELLOW_PAINT)], seed=68))
            for i, v in enumerate((-0.5, 0.0, 0.5)):
                base = shell_point(cap_c, cap_r, rot, dir_fn(0.15, v), 0.03)
                tipd = nrm(rot @ Vector((0.55 * sx, 0.35 * v + 0.25, 1.0)))
                length = (0.075, 0.1, 0.07)[i]
                b.tube("Spike", [base - tipd * 0.01, base + tipd * length * 0.55, base + tipd * length], [0.02, 0.011, 0.002], sides=10,
                       chain=chain, cap=0.4,
                       style=metal([(lambda c: C.smoothstep(0.65, 0.9, c.t), dict(paint=S.PAL.plate_top, wear=0.3))], seed=69 + i))
        # Tiers 2 and 3: cobalt lames under the outer edge, the lowest with a yellow hem.
        lk = 1.12 if big else 0.92
        dome_patch(b, "Lame", G((0.25 if big else 0.236) * sx, 1.198, -0.008), R(0.11 * lk, 0.096 * lk, 0.13 * lk), euler(roll=56 * sx),
                   lambda a, v, sx=sx: (a * 0.95 * sx, v * 1.1, 1.0), 0.016, None,
                   bplate([(lambda c, sx=sx: S.line(c.u[:, 0] * sx, 0.3, 0.03), GROOVE)], seed=24),
                   chain=[f"UpperArm_{name}", f"Clavicle_{name}"], nu=16, nv=12)
        dome_patch(b, "Lame", G((0.274 if big else 0.255) * sx, 1.165, -0.008), R(0.088 * lk, 0.082 * lk, 0.112 * lk), euler(roll=78 * sx),
                   lambda a, v, sx=sx: (a * 0.95 * sx, v * 1.1, 1.0), 0.012, None,
                   bplate([(lambda c, sx=sx: C.smoothstep(0.4, 0.55, c.u[:, 0] * sx), YELLOW_PAINT)], seed=25),
                   chain=[f"UpperArm_{name}"], nu=14, nv=10)
        s_, e_, w_ = sd.shoulder, sd.elbow, sd.wrist
        out_arm = nrm(Vector((sx, 0, 0)) + FWD * 0.2)
        b.tube("UpperArm", [s_, s_.lerp(e_, 0.5), e_], [0.06, 0.056, 0.05], sides=16, chain=[f"UpperArm_{name}"],
               style=fabric([(lambda c: S.line(c.t, 0.85, 0.03), GROOVE)]))
        limb_shell(b, "Rerebrace", s_, e_, 0.061, 0.052, out_arm, 75, 0.3, 0.8, 0.011, [f"UpperArm_{name}"],
                   bplate([(lambda c: S.line(along(c), 0.55, 0.012), GROOVE)], seed=26))
        out = nrm(-sd.hand_z)
        b.ellipsoid("ElbowCop", e_ + (e_ - s_).normalized() * -0.004 + G(0, 0, -0.03), R(0.05, 0.05, 0.044), bone=f"UpperArm_{name}",
                    deform=boxy(0.7), segments=(18, 10), style=plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.08), GROOVE)], seed=27))
        b.parts[-1].chain = [f"UpperArm_{name}", f"Forearm_{name}"]
        b.tube("Forearm", [e_, e_.lerp(w_, 0.5), w_], [0.054, 0.051, 0.045], sides=16, chain=[f"Forearm_{name}"], style=fabric())
        limb_shell(b, "Gauntlet", e_, w_, 0.058, 0.05, out, 105, 0.16, 0.96, 0.014, [f"Forearm_{name}"],
                   plate([(lambda c: (np.abs(around(c)) < 9) * S.band(along(c), 0.55, 0.3), LIGHT),
                          (lambda c: S.line(np.abs(around(c)), 30.0, 3.0), GROOVE),
                          (lambda c: S.band(along(c), 0.9, 0.05), YELLOW_PAINT),
                          (lambda c: S.line(along(c), 0.2, 0.012), GROOVE)], seed=13))
        if sx > 0:
            b.ellipsoid("WristPad", e_.lerp(w_, 0.55) + sd.hand_z * 0.052, R(0.026, 0.05, 0.012), frame(sd.hand_y, sd.hand_z),
                        bone=f"Forearm_{name}", deform=boxy(0.45), segments=(16, 10),
                        style=metal([(lambda c: S.band(c.u[:, 0], 0.0, 0.6) * S.band(c.u[:, 1], 0.0, 0.7) * (c.u[:, 2] > 0.5), CYAN)], seed=28))
        b.ellipsoid("Emitter", w_ + sd.hand_y * -0.02 + out * 0.064, R(0.014, 0.01, 0.014), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=S.light(strength=1.4), segments=(12, 8))
        b.ellipsoid("Cuff", w_ + sd.hand_y * 0.006, (0.046, 0.02, 0.046), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=metal(seed=14), segments=(16, 8))
        b.parts[-1].chain = [f"Forearm_{name}", f"Hand_{name}"]
        hand(b, name, sd)


def hand(b, name, sd):
    """A glove that reads as a hand: a slimmer palm, a low knuckle guard, four curved finger groups and a thumb."""
    pc = sd.wrist + sd.hand_y * 0.044
    b.ellipsoid("Palm", pc, (0.041, 0.05, 0.024), frame(sd.hand_y, sd.hand_z), bone=f"Hand_{name}",
                style=rubber([(lambda c: C.smoothstep(0.3, 0.7, -c.u[:, 2]), dict(family=dict(metal=1.0)))]), deform=boxy(0.6),
                segments=(20, 10))
    b.ellipsoid("Knuckles", pc + sd.hand_y * 0.02 - sd.hand_z * 0.02, (0.038, 0.022, 0.009), frame(sd.hand_y, -sd.hand_z),
                bone=f"Hand_{name}", style=metal([(lambda c: S.line(c.u[:, 1], 0.0, 0.1), GROOVE)], seed=15), deform=boxy(0.55),
                segments=(16, 8))
    k_ = sd.knuckle
    for off, r0, length in BM.FINGERS:
        o = sd.thumb * off
        base = k_ - sd.hand_y * 0.014 + o + sd.hand_z * 0.002
        pts = finger_points(sd, base, length + 0.006, -o * 0.1)
        b.tube("Finger", pts, [r0, r0 * 0.97, r0 * 0.9, r0 * 0.8], sides=10, chain=[f"Fingers_{name}"],
               style=rubber([(lambda c: S.band(c.t, 0.42, 0.04) + S.band(c.t, 0.72, 0.04), dict(groove=0.0012))]), cap=0.8)
    th, tt = sd.thumb_head, sd.thumb_tip
    bend = Quaternion(nrm(sd.thumb_dir.cross(sd.hand_z)), rad(-14)) @ sd.thumb_dir
    mid = th + sd.thumb_dir * 0.03
    b.tube("Thumb", [th - sd.thumb_dir * 0.01, mid, mid + bend * 0.03], [0.02, 0.018, 0.0145], sides=10, chain=[f"Thumb_{name}"],
           style=rubber([(lambda c: S.band(c.t, 0.55, 0.05), dict(groove=0.0012))]), cap=0.8)


def build_parts():
    b = C.PartBuilder()
    head(b)
    torso(b)
    tabard(b, 0.146, 0.15, 0.12, 0.44, 0.012, 71)
    tabard(b, -0.155, 0.17, 0.13, 0.4, 0.014, 72)
    pack(b)
    arms(b)
    M2.legs(b)
    return b


RIGID = (M2.RIGID - {"Crest", "HelmAntenna", "Cell", "CellCap", "HiltClip", "Fin", "Antenna", "AntennaTip", "AntennaBase", "Stub"}) | {
    "Fin", "FinLight", "EarVane", "Sleeve", "SheathLight", "SheathClamp", "SleeveGlow", "Nozzle", "NozzleGlow", "Vane", "VaneLight"}

PLATES = {"Helmet", "Fin", "EarVane", "Brow", "Cheek", "ChestPlate", "AbPlate", "BackPlate", "Tasset", "Pauldron", "Lame", "Rerebrace",
          "ElbowCop", "Gauntlet", "ThighPlate", "KneePad", "ShinPlate", "ToeCap", "Vane", "Ridge", "Spike", "Tabard", "Sleeve"}
GEAR = {"Visor", "Chin", "Filter", "Ear", "Comms", "HelmLamp", "FinLight", "ChestCore", "CoreRing", "Collar", "Belt", "Buckle", "Pouch",
        "Grenade", "Pack", "CoreBar", "CoreCap", "SheathLight", "SheathClamp", "SleeveGlow", "Nozzle", "NozzleGlow",
        "PackPouch", "VaneLight", "PauldronLight", "Hose", "WristPad", "Emitter", "Cuff", "Holster", "LegPouch", "BootCuff",
        "AnkleRing", "Boot", "Sole"}
LAYERS = [S.Layer("suit", None, voxel=0.0034, smooth=6, density=0.7),
          S.Layer("plates", PLATES, voxel=0.0018, smooth=2, density=1.0),
          S.Layer("gear", GEAR, voxel=0.0021, smooth=3, density=0.9),
          S.Layer("hands", {"Palm", "Knuckles", "Finger", "Thumb"}, voxel=0.0014, smooth=3, density=2.0)]


# ------------------------------------------------------------------------------------------------------------
# Rig tweaks: the Back socket at the sleeve; fingers; rest arms
# ------------------------------------------------------------------------------------------------------------

BM.BACK_SOCKET = BACK_GRIP
BM.BACK_Y = SLEEVE_DIR
BM.BACK_Z = BACK_EDGE

_compose = BM.compose


def compose(poser, P):
    """build_mercer.compose, with the Fingers bone curling FINGER_COMP degrees less at full grip (the mesh is bent)."""
    specs = _compose(poser, P)
    rest = poser.rest
    for name in SIDE:
        curl = P.get(f"grip.{name}.fingers", 0.4)
        specs[f"Fingers_{name}"] = {"rot": [(rest[f"Fingers_{name}"].to_3x3().col[0], rad(95) * curl - rad(10) - rad(FINGER_COMP) * curl)]}
    return specs


BM.compose = compose
RX.compose = compose

# Rest arms (Remco: hands at rest must look natural): elbows less bent, the wrist turned so the hand hangs along the
# thigh instead of pointing forward, fingers loosely curled, thumbs relaxed.
REST_ARMS = {"arm.L.elbow": 6.0, "arm.R.elbow": 8.0, "arm.L.wrist": 4.0, "arm.R.wrist": 4.0, "arm.L.swing": -2.0, "arm.R.swing": -4.0,
             "arm.L.wrist_side": 32.0, "arm.R.wrist_side": 32.0,
             "grip.L.fingers": 0.42, "grip.R.fingers": 0.46, "grip.L.thumb": 0.28, "grip.R.thumb": 0.3}
STANCE.update(REST_ARMS)
# A warrior's stance (Remco: "komt niet echt over als een warrior"): feet wider and further apart front to back,
# weight lower, the chest a touch forward.
STANCE.update({"hips.loc": G(0, -0.05, 0.0), "foot.L.pos": BM.foot_at("L", fwd=0.1, side=0.075), "foot.L.yaw": 12.0,
               "foot.R.pos": BM.foot_at("R", fwd=-0.13, side=0.06), "foot.R.yaw": 24.0, "spine2.pitch": 2.0, "neck.pitch": 0.0,
               "head.pitch": -1.0})
_idle = BM.idle_p


def idle_p(t, f):
    w = 2 * math.pi * t
    breath = math.sin(w)
    p = _idle(t, f)
    p["arm.L.elbow"] = REST_ARMS["arm.L.elbow"] + 3 * breath
    p["arm.R.elbow"] = REST_ARMS["arm.R.elbow"] + 2 * math.sin(w + 1.3)
    p["arm.L.swing"] = REST_ARMS["arm.L.swing"] + 1.5 * breath
    p["arm.R.swing"] = REST_ARMS["arm.R.swing"] + 1.5 * math.sin(w + 1.3)
    # Fingers breathe a little out of phase (a living hand never holds still).
    p["grip.L.fingers"] = REST_ARMS["grip.L.fingers"] + 0.06 * math.sin(w + 0.8)
    p["grip.R.fingers"] = REST_ARMS["grip.R.fingers"] + 0.05 * math.sin(w + 2.1)
    return p


BM.idle_p = idle_p
BM.CLIPS["Idle"] = BM.clip(BM.IDLE_FRAMES, idle_p, True)


# ------------------------------------------------------------------------------------------------------------
# Draw and stow clips
# ------------------------------------------------------------------------------------------------------------

def solve(poser, P):
    return poser.solve(compose(poser, dict(P)))


def shifted(m, offset=Vector(), local=None):
    """Frame `m` moved by `offset` (armature space) and by `local` = (axis index, distance) along its own axis."""
    r = m.copy()
    t = m.translation + Vector(offset)
    if local:
        t = t + m.to_3x3().col[local[0]].normalized() * local[1]
    r.translation = t
    return r


def with_hand(P, name, sock, pole, fingers, thumb, space="world"):
    out = dict(P)
    out.update({f"arm.{name}.mode": "ik", f"arm.{name}.space": space, f"arm.{name}.sock": sock, f"arm.{name}.pole": pole,
                f"grip.{name}.fingers": fingers, f"grip.{name}.thumb": thumb})
    return out


def over(base, d):
    out = dict(base)
    out.update(d)
    return out


HOLSTER_POLE = G(-0.45, 0.0, -1.0)
BACK_POLE = G(-0.95, 0.65, 0.3)


def draw_pistol_keys(poser):
    start = idle0()
    # Anticipation: the right shoulder lifts and the hand cocks back a little before the fast move down.
    cock = over(start, {"clav.R.raise": 5.0, "arm.R.swing": -12.0, "arm.R.elbow": 30.0, "arm.R.wrist": -8.0,
                        "grip.R.fingers": 0.2, "grip.R.thumb": 0.15, "head.yaw": -6.0, "head.pitch": 4.0})
    lean = {"hips.loc": STANCE["hips.loc"] + G(0, -0.022, 0), "hips.roll": -2.0, "spine1.roll": -3.0, "spine2.roll": -3.5,
            "head.yaw": -12.0, "head.pitch": 10.0, "clav.R.raise": -6.0}
    reach = over(start, lean)
    hol = solve(poser, reach)["Holster_R"]
    reach = with_hand(reach, "R", shifted(hol, G(-0.02, 0, 0), (2, -0.05)), HOLSTER_POLE, 0.3, 0.2)
    grab = over(start, lean)
    grab = with_hand(grab, "R", solve(poser, grab)["Holster_R"], HOLSTER_POLE, 0.92, 0.55)
    up = over(start, {"hips.roll": -0.5, "spine2.roll": -1.0, "head.yaw": -4.0, "head.pitch": 4.0, "clav.R.raise": 2.0})
    clear = with_hand(up, "R", shifted(solve(poser, up)["Holster_R"], local=(2, -0.17)), HOLSTER_POLE, 0.92, 0.55)
    aim = BM.shoot_p(0.0, 0)
    turn = over(aim, {"head.pitch": 3.0})
    turn.update({"arm.R.mode": "ik", "arm.R.space": "chest", "arm.R.sock": BM.sock_frame(G(-0.13, 1.08, 0.22), G(0.04, -0.12, 1.0), UP),
                 "arm.R.pole": G(-0.6, -0.4, -0.8), "arm.L.mode": "fk", "arm.L.swing": 40.0, "arm.L.out": -10.0, "arm.L.elbow": 70.0,
                 "arm.L.twist": 25.0, "grip.L.fingers": 0.3, "grip.L.thumb": 0.2})
    overshoot = over(aim, RX.aim_at(elev=9.0, back=-0.03))
    overshoot["spine2.pitch"] = 1.5
    return [(0, start, "ease"), (3, cock, "ease"), (5, reach, "in"), (7, grab, "snap"), (10, clear, "ease"), (13, turn, "ease"),
            (15, overshoot, "snap"), (DRAW_PISTOL_FRAMES, aim, "ease")]


def stow_pistol_keys(poser):
    aim = BM.shoot_p(0.0, 0)
    lower = over(BM.stance(), {"head.pitch": 8.0, "head.yaw": -5.0, "spine2.yaw": 2.0,
                               "arm.L.mode": "fk", "arm.L.swing": 12.0, "arm.L.out": -18.0, "arm.L.elbow": 32.0, "grip.L.fingers": 0.4})
    lower.update({"arm.R.mode": "ik", "arm.R.space": "chest", "arm.R.sock": BM.sock_frame(G(-0.15, 1.02, 0.2), G(0.05, -0.6, 0.8), UP),
                  "arm.R.pole": G(-0.6, -0.4, -0.8), "grip.R.fingers": 0.92, "grip.R.thumb": 0.55})
    lean = {"hips.loc": STANCE["hips.loc"] + G(0, -0.02, 0), "hips.roll": -2.0, "spine1.roll": -3.0, "spine2.roll": -3.5,
            "head.yaw": -12.0, "head.pitch": 11.0, "clav.R.raise": -6.0}
    body = over(BM.stance(), lean)
    hol = solve(poser, body)["Holster_R"]
    align = with_hand(body, "R", shifted(hol, local=(2, -0.15)), HOLSTER_POLE, 0.92, 0.55)
    seat = with_hand(body, "R", hol, HOLSTER_POLE, 0.92, 0.55)
    let_go = with_hand(body, "R", shifted(hol, G(-0.03, 0, 0), (2, -0.05)), HOLSTER_POLE, 0.25, 0.2)
    let_go["head.pitch"] = 4.0
    return [(0, aim, "ease"), (4, lower, "ease"), (8, align, "ease"), (11, seat, "in"), (13, seat, "linear"), (16, let_go, "ease"),
            (STOW_PISTOL_FRAMES, idle0(), "ease")]


def over_shoulder(body):
    """The hilt just pulled out, swung up over the right shoulder (pommel forward, emitter back)."""
    return with_hand(body, "R", BM.blade_frame(G(-0.2, 1.6, 0.04), G(0.12, 0.4, -0.9), G(-0.95, 0.1, 0.1)), G(-0.6, 0.7, 0.3), 0.95, 0.6,
                     space="chest")


def draw_blade_keys(poser):
    start = idle0()
    cock = over(start, {"hips.loc": STANCE["hips.loc"] + G(0, -0.02, 0), "spine1.yaw": 8.0, "spine2.yaw": 10.0, "head.yaw": -10.0,
                        "clav.R.raise": -4.0, "arm.R.swing": -10.0, "arm.R.elbow": 30.0, "grip.R.fingers": 0.3})
    reach_body = over(start, {"spine2.yaw": 2.0, "spine2.roll": 3.0, "spine1.roll": 1.5, "clav.R.raise": 16.0, "clav.R.fwd": -4.0,
                              "head.yaw": -18.0, "head.pitch": -2.0, "arm.L.swing": 14.0, "arm.L.elbow": 30.0})
    back = solve(poser, reach_body)["Back"]
    reach = with_hand(reach_body, "R", shifted(back, G(0, 0.02, 0.05), (1, -0.05)), BACK_POLE, 0.15, 0.15)
    grab = with_hand(reach_body, "R", back, BACK_POLE, 0.95, 0.6)
    pull_body = over(reach_body, {"spine2.yaw": -2.0, "clav.R.raise": 20.0, "head.yaw": -12.0})
    pull = with_hand(pull_body, "R", shifted(solve(poser, pull_body)["Back"], local=(1, -0.15)), BACK_POLE, 0.95, 0.6)
    swing = over_shoulder(over(start, {"spine2.yaw": -6.0, "spine1.yaw": -2.0, "clav.R.raise": 10.0, "head.yaw": -6.0}))
    guard = BM.slash_p(0.0, 0)
    overshoot = over(guard, {"hips.loc": guard["hips.loc"] + G(0, -0.035, 0.01), "spine2.pitch": guard["spine2.pitch"] + 4.0})
    overshoot.update({"arm.R.mode": "ik", "arm.R.space": "chest", "arm.R.pole": G(-0.8, -0.6, -0.3), "grip.R.fingers": 0.95,
                      "arm.R.sock": BM.blade_frame(G(-0.15, 0.99, 0.34), G(0.24, 0.45, 0.86), G(0.0, 0.0, 1.0))})
    return [(0, start, "ease"), (3, cock, "ease"), (7, reach, "ease"), (9, grab, "snap"), (12, pull, "ease"), (15, swing, "ease"),
            (18, overshoot, "snap"), (DRAW_BLADE_FRAMES, guard, "ease")]


def stow_blade_keys(poser):
    guard = BM.slash_p(0.0, 0)
    flick = dict(guard)
    flick.update({"arm.R.mode": "ik", "arm.R.space": "chest", "arm.R.pole": G(-0.8, -0.6, -0.3), "grip.R.fingers": 0.95,
                  "arm.R.sock": BM.blade_frame(G(-0.2, 1.1, 0.27), G(0.22, 0.97, 0.1), G(0.0, 0.0, 1.0))})
    swing = over_shoulder(over(BM.stance(), {"spine2.yaw": -4.0, "clav.R.raise": 12.0, "head.yaw": -8.0}))
    body = over(BM.stance(), {"spine2.yaw": 2.0, "spine2.roll": 3.0, "spine1.roll": 1.5, "clav.R.raise": 18.0, "clav.R.fwd": -4.0,
                              "head.yaw": -16.0, "head.pitch": -2.0})
    back = solve(poser, body)["Back"]
    align = with_hand(body, "R", shifted(back, local=(1, -0.13)), BACK_POLE, 0.95, 0.6)
    seat = with_hand(body, "R", back, BACK_POLE, 0.95, 0.6)
    let_go = with_hand(body, "R", shifted(back, G(-0.02, 0.03, 0.06), (1, -0.04)), BACK_POLE, 0.2, 0.15)
    return [(0, guard, "ease"), (4, flick, "snap"), (9, swing, "ease"), (12, align, "ease"), (15, seat, "in"), (17, seat, "linear"),
            (19, let_go, "ease"), (STOW_BLADE_FRAMES, idle0(), "ease")]


DRAW_PISTOL_FRAMES, STOW_PISTOL_FRAMES, DRAW_BLADE_FRAMES, STOW_BLADE_FRAMES = 18, 22, 22, 24
# name: (frames, keys, events, primary event, weapon, socket from, socket to, start, end)
DRAW_CLIPS = {
    "Draw_Pistol": (DRAW_PISTOL_FRAMES, draw_pistol_keys, {"reach": 5, "grab": 7, "clear": 10, "aimed": 18}, "grab",
                    "Pistol (+ Pistol_Mag)", "Holster_R", "Weapon_R", "Idle frame 0", "Shoot frame 0"),
    "Stow_Pistol": (STOW_PISTOL_FRAMES, stow_pistol_keys, {"lower": 4, "align": 8, "release": 11, "let_go": 16, "rest": 22}, "release",
                    "Pistol (+ Pistol_Mag)", "Weapon_R", "Holster_R", "Shoot frame 0", "Idle frame 0"),
    "Draw_Blade": (DRAW_BLADE_FRAMES, draw_blade_keys, {"reach": 7, "grab": 9, "clear": 12, "ignite": 16, "guard": 22}, "grab",
                   "Sword_Hilt (Sword_Blade from ignite)", "Back", "Weapon_R", "Idle frame 0", "Slash frame 0"),
    "Stow_Blade": (STOW_BLADE_FRAMES, stow_blade_keys, {"blade_off": 4, "align": 12, "release": 15, "rest": 24}, "release",
                   "Sword_Hilt (Sword_Blade until blade_off)", "Weapon_R", "Back", "Slash frame 0", "Idle frame 0"),
}
SWITCH = {"Draw_Pistol": ("grab", "Holster_R"), "Stow_Pistol": ("release", "Holster_R"), "Draw_Blade": ("grab", "Back"),
          "Stow_Blade": ("release", "Back")}


def draw_clip_fns():
    out = {}
    for name, (frames, keys, *_rest) in DRAW_CLIPS.items():
        out[name] = (lambda k, n: (lambda poser: KeyClip(poser, k(poser)).make(n, False)(poser)))(keys, frames)
    return out


def switch_check(poser):
    """How far the hand socket is from the holster / back socket at each switch frame (millimetres, degrees)."""
    out = {}
    for name, (event, socket) in SWITCH.items():
        frames, keys, events = DRAW_CLIPS[name][:3]
        clip = KeyClip(poser, keys(poser))
        pose = solve(poser, clip.pose(events[event]))
        a, b = pose["Weapon_R"], pose[socket]
        dist = (a.translation - b.translation).length * 1000
        ang = math.degrees(a.to_quaternion().rotation_difference(b.to_quaternion()).angle)
        out[name] = {"event": event, "frame": events[event], "offset_mm": round(dist, 2), "angle_deg": round(ang, 2)}
        C.log("switch", name, out[name])
    return out


# ------------------------------------------------------------------------------------------------------------
# Build, LOD, export
# ------------------------------------------------------------------------------------------------------------

def tabard_weights(mesh_obj, builder):
    """The cloth hangs from the hips and, lower down, follows the average of both thighs (no tearing between the legs)."""
    pts, _ = C.mesh_arrays(mesh_obj.data)
    pid = C.assign_parts(pts, builder)
    ids = [p.pid for p in builder.parts if p.name == "Tabard"]
    idx = np.nonzero(np.isin(pid, ids))[0]
    if not len(idx):
        return
    s = np.clip((TABARD_TOP - 0.03 - pts[idx, 2]) / 0.26, 0.0, 1.0) ** 1.3
    keep = [int(i) for i in idx]
    for vg in mesh_obj.vertex_groups:
        vg.remove(keep)
    groups = mesh_obj.vertex_groups
    for i, w in zip(keep, s):
        groups["Hips"].add([i], float(1 - 0.85 * w), "REPLACE")
        groups["Thigh_L"].add([i], float(0.425 * w), "REPLACE")
        groups["Thigh_R"].add([i], float(0.425 * w), "REPLACE")


def skin_all(mesh_obj, arm, builder):
    C.skin(mesh_obj, arm, builder, rigid=RIGID, blend=2)
    tabard_weights(mesh_obj, builder)
    for n in BM.SOCKETS:
        vg = mesh_obj.vertex_groups.get(n)
        if vg:
            mesh_obj.vertex_groups.remove(vg)


def build_lod(asset, arm, builder, index):
    lod_mesh = C.decimate(asset.low.data.copy(), LOD_TRIS, LOD_NAME, symmetric=False)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(LOD_NAME, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    index.register(lod_mesh)
    skin_all(lod, arm, builder)
    return lod


def run_reactions():
    keep = sys.argv
    sys.argv = list(keep) + (["--"] if "--" not in keep else []) + ["--norender"]
    try:
        RX.main()
    finally:
        sys.argv = keep


def patch_report(path, switch):
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    events = rep.setdefault("events", {})
    clips = {}
    for name, (frames, _, ev, primary, weapon, src, dst, start, end) in DRAW_CLIPS.items():
        events[name] = {primary: ev[primary]}
        clips[name] = {"frames": frames, "seconds": round(frames / 30.0, 3), "loop": False, "layer": "full (feet stay in the stance)",
                       "weapon": weapon, "from_socket": src, "to_socket": dst, "starts": start, "ends": end,
                       "events": ev, "events_normalized": {k: round(v / frames, 3) for k, v in ev.items()}}
    rep["draw_stow"] = {
        "clips": clips, "switch_check": switch,
        "back_socket": "moved to the sword sleeve on the pack's top right (pommel up over the right shoulder)",
        "unity": "reparent the weapon (identity local transform) at the primary event; Draw_Blade shows Sword_Blade from 'ignite', "
                 "Stow_Blade hides it at 'blade_off'"}
    rep["round"] = "spencer-pass2-2026-11-08 (build_mercer3.py)"
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", path)


def main():
    args = C.cli_args()
    fast = bool(args.get("fast"))
    C.reset_scene()
    BM.stage_exports()
    out = os.path.join(C.REPO, "ArtSource", "Previews", "Spencer") if not args.get("preview") else C.preview_dir()
    tex, low_tex = (512, None) if fast else (TEX, LOW_TEX)
    W.build()
    builder = build_parts()
    asset, index = S.build_layered_asset(NAME, builder, LAYERS, voxel=0.0034, target_tris=GAME_TRIS, tex_size=tex, ao_distance=0.06,
                                         extrusion=0.01, ray=0.03, emission_strength=2.5, extra=battle_wear(), specular=0.4,
                                         mirror=False, symmetric=False, k=1.0, families=BM.FAMILIES, family_set=S.FAMILY_SET,
                                         low_size=low_tex, preview_detail=(0.8, 0.6), edge_gain=0.5, border_smooth=4,
                                         unwrap="smart")
    arm = BM.build_armature(NAME + "_Rig", BM.bones())
    skin_all(asset.low, arm, builder)
    poser = C.Poser(arm)
    clips = {n: BM.CLIPS[n] for n in (("Idle", "Walk", "Run", "Shoot", "Slash") if fast else BM.CLIPS)}
    clips.update(draw_clip_fns())
    actions = C.key_clips(poser, clips)
    switch = switch_check(poser)
    arm.animation_data.action = actions["Idle"]
    objs = [arm, asset.low]
    lod = None
    if not fast:
        lod = build_lod(asset, arm, builder, index)
        objs.append(lod)
    for o in objs[1:]:
        S.drop_layer_attr(o)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), objs, animated=True)
    C.write_report(NAME, {
        "asset": NAME, "character": "Spencer", "round": "spencer-pass2-2026-11-08 (build_mercer3.py)",
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
        run_reactions()
    patch_report(os.path.join(C.EXPORTS, NAME, NAME + "_report.json"), switch)
    if args.get("norender"):
        return
    import spencer_previews as SP
    SP.render_pass2(os.path.join(C.BLEND, NAME + ".blend"), out, args.get("prefix", "pass2"), ORIGINAL_BLEND,
                    args.get("before"), list(DRAW_CLIPS.items()))


if __name__ == "__main__":
    main()
