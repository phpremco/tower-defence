"""Spencer, the hero (Remco 2026-11-08: "he needs a bit of a boost look wise, cooler and more detail").

Overrides build_mercer.py's export with a more detailed colonial soldier / engineer on the SAME rig, bone names,
clips, sockets and export names (ArtSource/Exports/Mercer/Mercer.fbx, Mercer_<Map>.png, Mercer_report.json; the asset
id stays "Mercer" so the Unity import keeps working). Runs build_mercer_reactions.py's clips on top afterwards, so the
FBX carries all sixteen clips as before.

What changed against round 16 (build_mercer.py):
- Layered armour in three values plus accents: white ceramic plates, cobalt-blue painted plates, a dark navy undersuit
  (no more quilted "snow suit" limbs), gunmetal gear, yellow trim and hazard marks, cyan light.
- Helmet: a faceted shell with a raised yellow crest, a brow plate, blue cheek guards, a respirator with two filters,
  a narrower visor with a bright cyan eye slit, a comms pod with a whip antenna (right) and a helmet lamp (left).
- Torso: a two-panel cuirass with a cyan core at the sternum, two blue abdominal plates, a thicker collar, a loaded
  utility belt (pouches, two grenades) and blue tassets over the hips.
- Pack: a bigger power pack with a lit cell bar across the top (reads from the high game camera), the two cyan side
  cells, heat-sink fins, a tall whip antenna with a lit tip, a short stub antenna, side pouches and a hose to the belt.
- Shoulders: a white pauldron with a yellow stripe over two blue lames; chevrons on the left one.
- Arms: blue upper-arm plates, elbow cops, bigger white gauntlets with a cyan strip; a wrist display on the left.
- Legs: white thigh plates with a yellow stripe, bigger knee cops, ribbed blue shin guards, armoured boots with blue
  toe caps, ankle rings and lugged soles; a leg pouch on the left thigh (the pistol holster stays on the right).
- Not mirrored: the figure is asymmetric (antennas, lamp, chevrons, wrist display), so the UVs are not mirrored and
  the maps are 2048 (Low 1024), like Voss.

Run: blender --background --factory-startup --python Tools/Blender/build_mercer2.py --
     [--preview <dir>] [--norender] [--fast] [--stage <dir>]
  --fast: 512 maps, no LOD / reactions (look iterations only). --stage: write exports and .blend under <dir>.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import build_mercer as BM  # noqa: E402
import build_mercer_weapons as W  # noqa: E402
import common as C  # noqa: E402
import suit as S  # noqa: E402
from build_mercer import FWD, SIDE, R, along, around, boxy, dome_patch, euler, frame, limb_shell, nrm  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
LOD_NAME = BM.LOD_NAME
GAME_TRIS = 15000
LOD_TRIS = 5200
TEX, LOW_TEX = 2048, 1024
ORIGINAL_BLEND = C.BLEND

# --- Palette: the undersuit goes darker so the plates carry the colour; a second plate colour (cobalt blue) -------
S.PAL.suit_top = np.array((0.19, 0.27, 0.44))
S.PAL.suit_side = np.array((0.12, 0.18, 0.31))
S.PAL.suit_under = np.array((0.05, 0.075, 0.14))
S.PAL.suit_seam = np.array((0.035, 0.05, 0.09))
S.PAL.bplate_top = np.array((0.24, 0.47, 0.88))
S.PAL.bplate_side = np.array((0.16, 0.34, 0.7))
S.PAL.bplate_under = np.array((0.07, 0.15, 0.32))
S.PAL.metal_top = np.array((0.3, 0.32, 0.36))
S.BASES["bplate"] = ("bplate_top", "bplate_side", "bplate_under", 0.4, dict(ceramic=1.0))

CYAN = dict(emit=S.PAL.cyan, glow=1.0, rough=0.3, family={})
CYAN_SOFT = dict(emit=S.PAL.cyan, glow=0.7, rough=0.3, family={})
YELLOW_GLOW = dict(emit=S.PAL.yellow, glow=0.9, rough=0.3, family={})
YELLOW = dict(paint=S.PAL.yellow, wear=0.55, rough=0.5)
YELLOW_CLEAN = dict(paint=S.PAL.yellow, wear=0.2, rough=0.5)
WHITE_PAINT = dict(paint=S.PAL.plate_top, wear=0.4, rough=0.45)
DARK = dict(paint=S.PAL.metal_under, wear=0.0, rough=0.6)
GROOVE = dict(groove=0.0014)
DEEP = dict(groove=0.0026)


def plate(features=(), seed=0):
    return S.styled("plate", features, seed=seed)


def bplate(features=(), seed=0):
    return S.styled("bplate", features, seed=seed)


def metal(features=(), seed=0):
    return S.styled("metal", features, seed=seed)


def rubber(features=()):
    return S.styled("rubber", features)


def fabric(features=()):
    return S.styled("suit", features)


def quilted():
    return S.styled("suit", (), quilt=("height", BM.SPACING))


# --- Masks ----------------------------------------------------------------------------------------------------

def hazard(c, period=0.028, axis=(1.0, 0.0, 1.0)):
    """Diagonal hazard stripes in world space (period in metres)."""
    s = c.p @ np.asarray(axis, dtype=float) / period
    return C.smoothstep(0.35, 0.5, np.abs((s % 1.0) - 0.5) * 2)


def ribs(values, period, width=0.18):
    frac = (values / period) % 1.0
    return 1 - C.smoothstep(0.0, width, np.minimum(frac, 1 - frac))


def front(c, limit=-0.3):
    return (c.u[:, 1] < limit).astype(float)


def build_parts():
    b = C.PartBuilder()
    head(b)
    torso(b)
    pack(b)
    arms(b)
    legs(b)
    return b


# ------------------------------------------------------------------------------------------------------------
# Head
# ------------------------------------------------------------------------------------------------------------

HELM_C = G(0, 1.462, -0.01)
HELM_R = R(0.128, 0.136, 0.14)


def head(b):
    # Shell: slightly faceted (boxy) so it reads as a helmet, not a ball; panel lines and a seam round the back.
    b.ellipsoid("Helmet", HELM_C, HELM_R, bone="Head", segments=(48, 26),
                style=plate([(lambda c: S.line(np.abs(c.u[:, 0]), 0.36, 0.02) * C.smoothstep(0.2, 0.5, c.u[:, 2]), GROOVE),
                             (lambda c: S.line(c.u[:, 2], -0.25, 0.025) * C.smoothstep(-0.2, 0.4, c.u[:, 1]), GROOVE),
                             (lambda c: S.line(c.u[:, 1], 0.55, 0.02) * C.smoothstep(-0.3, 0.1, c.u[:, 2]), GROOVE),
                             # Vent slots low on the back.
                             (lambda c: ribs(c.u[:, 0], 0.1, 0.3) * S.band(c.u[:, 2], -0.48, 0.07) * (c.u[:, 1] > 0.5), DEEP)],
                            seed=4))
    # Crest: a raised ridge front to back, painted yellow; the brightest thing on top of him from the game camera.
    crest = []
    for el in (38, 62, 86, 110, 134, 156):
        e = math.radians(el)
        crest.append(HELM_C + G(0, 0.136 * math.sin(e) * 1.02, 0.14 * math.cos(e) * 1.02))
    b.tube("Crest", crest, [0.014, 0.02, 0.022, 0.022, 0.02, 0.015], sides=12, chain=["Head"], cap=0.5,
           style=plate([(lambda c: (c.t > 0.04).astype(float), YELLOW_CLEAN),
                        (lambda c: S.line(c.t, 0.35, 0.02) + S.line(c.t, 0.7, 0.02), GROOVE)], seed=31))
    # Brow plate over the visor.
    dome_patch(b, "Brow", HELM_C, HELM_R, None, lambda a, v: (a * 0.92, -1.0, 0.4 + 0.62 * (v + 1) / 2), 0.015, "Head",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), GROOVE),
                      (lambda c: S.band(np.abs(c.u[:, 0]), 0.42, 0.06) * S.band(c.u[:, 2], 0.42, 0.06), YELLOW_GLOW)], seed=32),
               nu=22, nv=10, outline=lambda a, v: (a * (1.0 - 0.3 * (v + 1) / 2), v))
    for name, sd in SIDE.items():
        sx = sd.sx
        # Cheek guard: blue, wrapping from the ear to beside the respirator, under the visor.
        dome_patch(b, "Cheek", HELM_C, HELM_R, None,
                   lambda a, v, sx=sx: (sx * (1.0 - 0.55 * (a + 1) / 2), -(0.05 + 0.95 * (a + 1) / 2), -0.15 - 0.8 * (v + 1) / 2),
                   0.015, "Head", bplate([(lambda c: S.line(c.u[:, 2], -0.72, 0.03), GROOVE)], seed=33 + (sx > 0)),
                   nu=16, nv=10)
        b.tube("Ear", [HELM_C + G(0.112 * sx, -0.02, -0.005), HELM_C + G(0.145 * sx, -0.02, -0.005)], [0.042, 0.036], sides=18,
               chain=["Head"], style=metal([(lambda c: S.band(c.t, 0.95, 0.12), GROOVE)], seed=6), cap=0.35)
    # Visor: smoked glass with a bright cyan eye slit (the face of the figure at game zoom).
    b.ellipsoid("Visor", G(0, 1.457, 0.012), R(0.122, 0.054, 0.128), bone="Head", segments=(48, 24),
                style=S.glass(lambda c: S.band(c.u[:, 2], 0.08, 0.2) * C.smoothstep(0.3, 0.75, -c.u[:, 1])
                              * (1 - C.smoothstep(0.62, 0.8, np.abs(c.u[:, 0]))), glow=0.12))
    # Respirator with vent grooves and two side filters.
    b.ellipsoid("Chin", G(0, 1.378, 0.072), R(0.062, 0.036, 0.052), euler(pitch=14), bone="Head", deform=boxy(0.55), segments=(28, 14),
                style=metal([(lambda c: ribs(c.u[:, 0], 0.22, 0.25) * (c.u[:, 1] < -0.3) * S.band(c.u[:, 2], 0.0, 0.5), DEEP),
                             (lambda c: S.band(c.u[:, 2], 0.75, 0.2), YELLOW)], seed=5))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Filter", [G(0.05 * sx, 1.372, 0.084), G(0.086 * sx, 1.356, 0.052)], [0.021, 0.019], sides=14, chain=["Head"], cap=0.5,
               style=metal([(lambda c: ribs(c.t, 0.25, 0.2), GROOVE), (lambda c: S.band(c.t, 0.92, 0.1), YELLOW)], seed=34))
    # Right: comms pod with a whip antenna. Left: helmet lamp.
    b.ellipsoid("Comms", HELM_C + G(-0.142, -0.005, -0.03), R(0.03, 0.05, 0.062), bone="Head", deform=boxy(0.4), segments=(18, 10),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.2, 0.06), GROOVE),
                             (lambda c: S.band(c.u[:, 1], -0.75, 0.12) * S.band(c.u[:, 2], -0.3, 0.2), CYAN)], seed=35))
    b.tube("HelmAntenna", [HELM_C + G(-0.152, 0.035, -0.06), HELM_C + G(-0.16, 0.12, -0.085), HELM_C + G(-0.168, 0.2, -0.11)],
           [0.0075, 0.0065, 0.0055], sides=8, chain=["Head"], cap=0.6,
           style=metal([(lambda c: S.band(c.t, 1.0, 0.12), YELLOW_GLOW)], seed=36))
    b.tube("HelmLamp", [HELM_C + G(0.14, 0.01, -0.01), HELM_C + G(0.142, 0.012, 0.06)], [0.019, 0.021], sides=14, chain=["Head"], cap=0.3,
           style=metal([(lambda c: S.band(c.t, 1.0, 0.1), dict(emit=np.array((1.0, 0.93, 0.72)), glow=1.4, rough=0.2, family={})),
                        (lambda c: S.band(c.t, 0.2, 0.08), YELLOW)], seed=37))


# ------------------------------------------------------------------------------------------------------------
# Torso, belt and hips
# ------------------------------------------------------------------------------------------------------------

CHEST_C, CHEST_R = G(0, 1.19, -0.006), R(0.172, 0.13, 0.122)
ABS_C, ABS_R = G(0, 1.045, 0.006), R(0.134, 0.1, 0.106)


def torso(b):
    b.ellipsoid("Pelvis", G(0, 0.9, 0.0), R(0.15, 0.105, 0.112), bone="Hips", style=quilted(), segments=(36, 18))
    b.ellipsoid("Abdomen", ABS_C, ABS_R, bone="Spine1", style=quilted(), segments=(36, 18))
    b.parts[-1].chain = ["Spine1", "Hips"]
    b.ellipsoid("Chest", CHEST_C, CHEST_R, bone="Spine2", style=quilted(), segments=(40, 20))
    b.parts[-1].chain = ["Spine2", "Spine1"]
    # Cuirass: two pectoral panels split by a deep centre seam, panel lines, yellow hazard corners.
    dome_patch(b, "ChestPlate", CHEST_C, CHEST_R, euler(pitch=-6), lambda a, v: (a * 2.0, -1.0, v * 0.95 + 0.2), 0.02, "Spine2",
               plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.025) * front(c), DEEP),
                      (lambda c: S.line(c.u[:, 2], -0.28, 0.022) * front(c), GROOVE),
                      (lambda c: S.line(np.abs(c.u[:, 0]), 0.55, 0.02) * (c.u[:, 2] > -0.28) * front(c), GROOVE),
                      (lambda c: hazard(c) * C.smoothstep(0.6, 0.7, np.abs(c.u[:, 0])) * C.smoothstep(0.25, 0.4, c.u[:, 2]), YELLOW),
                      (lambda c: S.band(np.abs(c.u[:, 0]), 0.3, 0.08) * S.band(c.u[:, 2], -0.05, 0.035) * (c.u[:, 1] < -0.5), CYAN_SOFT)],
                     seed=1),
               outline=lambda a, v: (a * (0.72 + 0.28 * (v + 1) / 2), v), nu=30, nv=18)
    # Core: a lit disc in a metal ring at the sternum.
    core = G(0, 1.168, 0.152)
    b.ellipsoid("ChestCore", core, R(0.026, 0.026, 0.012), bone="Spine2", style=S.light(strength=1.6), segments=(18, 10))
    b.ring("CoreRing", core + G(0, 0, -0.004), FWD, 0.03, 0.009, arcs=3, sides=8, chain=["Spine2"],
           style=metal([(lambda c: ribs(c.p[:, 0] + c.p[:, 2], 0.012, 0.25), GROOVE)], seed=38))
    # Two blue abdominal plates.
    for i, (z0, seed) in enumerate(((0.3, 40), (-0.08, 41))):
        dome_patch(b, "AbPlate", ABS_C, R(0.134 * 1.03, 0.1 * 1.03, 0.106 * 1.03), None,
                   lambda a, v, z0=z0: (a * 1.45, -1.0, z0 + v * 0.16), 0.012, None,
                   bplate([(lambda c: S.line(c.u[:, 0], 0.0, 0.02), GROOVE)], seed=seed), chain=["Spine1"], nu=20, nv=8,
                   outline=lambda a, v: (a * 0.92, v))
    # Lumbar plate under the pack (the back view was bare suit).
    dome_patch(b, "BackPlate", ABS_C, R(0.134 * 1.03, 0.1 * 1.03, 0.106 * 1.03), None, lambda a, v: (a * 1.3, 1.0, 0.05 + v * 0.42), 0.013,
               None, bplate([(lambda c: S.line(c.u[:, 0], 0.0, 0.03), DEEP), (lambda c: S.line(c.u[:, 2], 0.0, 0.03), GROOVE),
                             (lambda c: S.band(c.u[:, 2], -0.4, 0.06), YELLOW)], seed=60),
               chain=["Spine1"], nu=20, nv=10, outline=lambda a, v: (a * (0.75 + 0.25 * (v + 1) / 2), v))
    # Collar ring, thick, lit along its upper rim.
    b.ring("Collar", G(0, 1.312, -0.004), Vector((0, 0, 1)), 0.088, 0.031, arcs=3,
           sides=10, chain=["Spine2"],
           style=metal([(lambda c: C.smoothstep(0.5, 0.9, c.n[:, 2]) * S.band(c.p[:, 2], 1.34, 0.008), CYAN),
                        (lambda c: ribs(np.arctan2(c.p[:, 0], c.p[:, 1]), 0.35, 0.12), GROOVE)], seed=39))
    b.tube("Neck", [G(0, 1.29, -0.004), G(0, 1.35, 0.0), G(0, 1.405, 0.004)], [0.062, 0.058, 0.06], sides=14,
           chain=["Neck"], style=rubber([(lambda c: (np.abs(np.sin(c.t * math.pi * 5)) < 0.2).astype(float), dict(groove=0.002))]))
    # Belt: a heavy strap with a buckle, two front pouches, two grenades on the right, a pouch at the back.
    b.ellipsoid("Belt", G(0, 0.958, 0.0), R(0.158, 0.034, 0.12), bone="Hips", deform=boxy(0.6), segments=(40, 12),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.45, 0.06) + S.line(c.u[:, 2], -0.45, 0.06), GROOVE)], seed=42))
    b.ellipsoid("Buckle", G(0, 0.958, 0.12), R(0.04, 0.03, 0.016), bone="Hips", deform=boxy(0.35), segments=(16, 8),
                style=plate([(lambda c: S.band(c.u[:, 2], 0.0, 0.4), YELLOW_CLEAN), (lambda c: S.line(c.u[:, 0], 0.0, 0.08), DEEP)]))
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
    # Tassets: blue plates hanging over the hips (on the thighs, so they swing with the legs).
    for name, sd in SIDE.items():
        sx = sd.sx
        out_front = nrm(FWD * 0.55 + Vector((sx, 0, 0)))
        limb_shell(b, "Tasset", sd.hip + G(0, 0.04, 0), sd.knee, 0.118, 0.1, out_front, 62, 0.0, 0.3, 0.011, [f"Thigh_{name}"],
                   bplate([(lambda c: S.band(along(c), 0.95, 0.06), YELLOW), (lambda c: S.line(around(c), 0.0, 3.0), GROOVE)],
                          seed=44 + (sx > 0)))


# ------------------------------------------------------------------------------------------------------------
# Power pack
# ------------------------------------------------------------------------------------------------------------

def pack(b):
    b.ellipsoid("Pack", G(0, 1.15, -0.195), R(0.148, 0.17, 0.084), bone="Spine2", deform=boxy(0.3), segments=(36, 18),
                style=metal([(lambda c: S.line(c.u[:, 2], 0.3, 0.02) + S.line(c.u[:, 2], -0.45, 0.02), GROOVE),
                             (lambda c: S.line(np.abs(c.u[:, 0]), 0.82, 0.03) * (c.u[:, 1] > 0.3), GROOVE),
                             (lambda c: hazard(c, 0.024, (1, 0, -1)) * S.band(c.u[:, 2], -0.72, 0.07) * (c.u[:, 1] > 0.4), YELLOW),
                             (lambda c: C.smoothstep(0.9, 0.97, np.abs(c.u[:, 0])) * S.band(c.u[:, 2], 0.0, 0.5), YELLOW)], seed=7))
    # The cell bar across the top: a housing with five lit windows facing up and back.
    a, z = G(0.138, 1.338, -0.2), G(-0.138, 1.338, -0.2)
    b.tube("CoreBar", [a, z], [0.043, 0.043], sides=20, chain=["Spine2"], cap=0.2,
           style=metal([(lambda c: (ribs(c.t, 0.2, 0.32) < 0.5) * S.band(c.t, 0.5, 0.42) * C.smoothstep(-0.1, 0.4, c.n[:, 2] - c.n[:, 1] * 0.5),
                         dict(emit=S.PAL.cyan, glow=1.4, rough=0.3, family={})),
                        (lambda c: S.band(c.t, 0.06, 0.03) + S.band(c.t, 0.94, 0.03), GROOVE)], seed=46))
    for end in (a, z):
        b.ellipsoid("CoreCap", end, R(0.02, 0.05, 0.05), bone="Spine2", deform=boxy(0.5), segments=(16, 10),
                    style=metal([(lambda c: (np.abs(c.u[:, 0]) > 0.3).astype(float), YELLOW)], seed=47))
    # Side cells (as before) on the back face.
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Cell", [G(0.098 * sx, 1.03, -0.262), G(0.098 * sx, 1.15, -0.268), G(0.098 * sx, 1.27, -0.262)], [0.034, 0.035, 0.034],
               sides=14, chain=["Spine2"], style=S.light(strength=1.0, bands=14, band_dark=0.75), cap=0.4)
        for y in (1.025, 1.275):
            b.ellipsoid("CellCap", G(0.098 * sx, y, -0.262), R(0.04, 0.016, 0.04), bone="Spine2", style=metal(seed=10), segments=(16, 8))
        b.ellipsoid("HiltClip", G(0.158 * sx, 1.17, -0.2), R(0.018, 0.05, 0.028), bone="Spine2", style=metal(seed=11),
                    deform=boxy(0.35), segments=(12, 8))
        b.ellipsoid("PackPouch", G(0.118 * sx, 1.0, -0.215), R(0.036, 0.05, 0.05), bone="Spine2", deform=boxy(0.3), segments=(16, 10),
                    style=fabric([(lambda c: S.band(c.u[:, 2], 0.6, 0.14), dict(groove=0.002))]))
    # Heat-sink fins between the cells.
    for i in range(5):
        x = -0.044 + 0.022 * i
        b.ellipsoid("Fin", G(x, 1.15, -0.27), R(0.0055, 0.085, 0.022), bone="Spine2", deform=boxy(0.4), segments=(10, 10),
                    style=metal([(lambda c: S.band(c.u[:, 2], -0.85, 0.12), YELLOW)], seed=48 + i))
    # Antennas: a tall whip with a lit tip on the right, a stub with a cap on the left.
    b.tube("Antenna", [G(-0.088, 1.36, -0.262), G(-0.093, 1.55, -0.272), G(-0.1, 1.74, -0.29)], [0.0085, 0.0065, 0.0055], sides=8,
           chain=["Spine2"], cap=0.6, style=metal([(lambda c: ribs(c.t, 0.25, 0.1) * (c.t < 0.3), GROOVE)], seed=53))
    b.ellipsoid("AntennaTip", G(-0.1, 1.748, -0.29), R(0.011, 0.011, 0.011), bone="Spine2", style=S.light(S.PAL.yellow, strength=1.5),
                segments=(12, 8))
    b.ellipsoid("AntennaBase", G(-0.088, 1.36, -0.262), R(0.016, 0.014, 0.016), bone="Spine2", style=metal(seed=54), segments=(12, 8))
    b.tube("Stub", [G(0.088, 1.36, -0.262), G(0.09, 1.44, -0.268)], [0.012, 0.01], sides=10, chain=["Spine2"], cap=0.4,
           style=metal([(lambda c: S.band(c.t, 1.0, 0.15), YELLOW_GLOW)], seed=55))
    # A hose from the pack's left corner down to the belt (bends with the back).
    b.tube("Hose", [G(0.14, 1.02, -0.2), G(0.158, 0.99, -0.16), G(0.16, 0.965, -0.1)], [0.013, 0.013, 0.013], sides=10,
           chain=["Spine2", "Spine1", "Hips"], cap=0.3,
           style=rubber([(lambda c: ribs(c.t * C.axis_length(c.part), 0.012, 0.3), dict(groove=0.0015))]))


# ------------------------------------------------------------------------------------------------------------
# Shoulders and arms
# ------------------------------------------------------------------------------------------------------------

def chevrons(c, sx):
    """Two yellow chevrons on the left pauldron's outer face (rank marks)."""
    if sx < 0:
        return np.zeros(len(c.p))
    y = c.u[:, 2] + np.abs(c.u[:, 1]) * 0.6
    return (S.band(y, 0.05, 0.05) + S.band(y, -0.15, 0.05)) * S.band(c.u[:, 1], 0.0, 0.45) * (c.u[:, 0] > 0.3)


def arms(b):
    for name, sd in SIDE.items():
        sx = sd.sx
        b.ellipsoid("Yoke", G(0.168 * sx, 1.25, -0.006), R(0.084, 0.07, 0.086), bone=f"Clavicle_{name}", style=fabric(),
                    segments=(24, 12))
        b.parts[-1].chain = [f"Clavicle_{name}", "Spine2"]
        dome_patch(b, "Pauldron", G(0.2 * sx, 1.218, -0.008), R(0.135, 0.116, 0.136), euler(roll=22 * sx),
                   lambda a, v, sx=sx: (a * 0.98 * sx, v * 0.95, 1.0), 0.02, None,
                   plate([(lambda c, sx=sx: S.line(c.u[:, 0] * sx, 0.42, 0.03), GROOVE),
                          (lambda c, sx=sx: S.band(c.u[:, 0] * sx, 0.12, 0.09) * C.smoothstep(0.4, 0.6, c.u[:, 2]), YELLOW),
                          (lambda c, sx=sx: chevrons(c, sx), YELLOW_CLEAN)], seed=12),
                   chain=[f"Clavicle_{name}", f"UpperArm_{name}"])
        dome_patch(b, "Lame", G(0.232 * sx, 1.205, -0.008), R(0.09, 0.08, 0.112), euler(roll=56 * sx),
                   lambda a, v, sx=sx: (a * 0.95 * sx, v * 1.1, 1.0), 0.013, None,
                   bplate([(lambda c, sx=sx: S.line(c.u[:, 0] * sx, 0.3, 0.03), GROOVE)], seed=24),
                   chain=[f"UpperArm_{name}", f"Clavicle_{name}"], nu=16, nv=12)
        dome_patch(b, "Lame", G(0.25 * sx, 1.18, -0.008), R(0.075, 0.07, 0.098), euler(roll=78 * sx),
                   lambda a, v, sx=sx: (a * 0.95 * sx, v * 1.1, 1.0), 0.011, None,
                   bplate([(lambda c, sx=sx: C.smoothstep(0.4, 0.55, c.u[:, 0] * sx), YELLOW)], seed=25),
                   chain=[f"UpperArm_{name}"], nu=14, nv=10)
        s_, e_, w_ = sd.shoulder, sd.elbow, sd.wrist
        out_arm = nrm(Vector((sx, 0, 0)) + FWD * 0.2)
        b.tube("UpperArm", [s_, s_.lerp(e_, 0.5), e_], [0.06, 0.056, 0.05], sides=16, chain=[f"UpperArm_{name}"],
               style=fabric([(lambda c: S.line(c.t, 0.85, 0.03), GROOVE)]))
        limb_shell(b, "Rerebrace", s_, e_, 0.061, 0.052, out_arm, 75, 0.3, 0.8, 0.011, [f"UpperArm_{name}"],
                   bplate([(lambda c: S.line(along(c), 0.55, 0.012), GROOVE)], seed=26))
        out = nrm(-sd.hand_z)  # back of the forearm
        b.ellipsoid("ElbowCop", e_ + (e_ - s_).normalized() * -0.004 + G(0, 0, -0.03), R(0.05, 0.05, 0.044), bone=f"UpperArm_{name}",
                    deform=boxy(0.7), segments=(18, 10), style=plate([(lambda c: S.line(c.u[:, 0], 0.0, 0.08), GROOVE)], seed=27))
        b.parts[-1].chain = [f"UpperArm_{name}", f"Forearm_{name}"]
        b.tube("Forearm", [e_, e_.lerp(w_, 0.5), w_], [0.054, 0.051, 0.045], sides=16, chain=[f"Forearm_{name}"], style=fabric())
        limb_shell(b, "Gauntlet", e_, w_, 0.058, 0.05, out, 105, 0.16, 0.96, 0.014, [f"Forearm_{name}"],
                   plate([(lambda c: (np.abs(around(c)) < 9) * S.band(along(c), 0.55, 0.3), CYAN),
                          (lambda c: S.line(np.abs(around(c)), 30.0, 3.0), GROOVE),
                          (lambda c: S.band(along(c), 0.9, 0.05), YELLOW),
                          (lambda c: S.line(along(c), 0.2, 0.012), GROOVE)], seed=13))
        if sx > 0:
            # Wrist display on the left forearm, screen up and out.
            b.ellipsoid("WristPad", e_.lerp(w_, 0.55) + sd.hand_z * 0.052, R(0.026, 0.05, 0.012), frame(sd.hand_y, sd.hand_z),
                        bone=f"Forearm_{name}", deform=boxy(0.45), segments=(16, 10),
                        style=metal([(lambda c: S.band(c.u[:, 0], 0.0, 0.6) * S.band(c.u[:, 1], 0.0, 0.7) * (c.u[:, 2] > 0.5), CYAN)], seed=28))
        b.ellipsoid("Emitter", w_ + sd.hand_y * -0.02 + out * 0.064, R(0.014, 0.01, 0.014), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=S.light(strength=1.4), segments=(12, 8))
        b.ellipsoid("Cuff", w_ + sd.hand_y * 0.006, (0.047, 0.02, 0.047), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=metal(seed=14), segments=(16, 8))
        b.parts[-1].chain = [f"Forearm_{name}", f"Hand_{name}"]
        # Gloves (as round 16): palm block, knuckle plate, finger groups and the thumb.
        pc = sd.wrist + sd.hand_y * 0.046
        b.ellipsoid("Palm", pc, (0.045, 0.052, 0.029), frame(sd.hand_y, sd.hand_z), bone=f"Hand_{name}",
                    style=rubber([(lambda c: C.smoothstep(0.3, 0.7, -c.u[:, 2]), dict(family=dict(metal=1.0)))]), deform=boxy(0.75),
                    segments=(20, 10))
        b.ellipsoid("Knuckles", pc + sd.hand_y * 0.014 - sd.hand_z * 0.024, (0.041, 0.031, 0.014), frame(sd.hand_y, -sd.hand_z),
                    bone=f"Hand_{name}", style=metal([(lambda c: S.line(c.u[:, 1], 0.0, 0.08), GROOVE)], seed=15), deform=boxy(0.5),
                    segments=(16, 8))
        k_ = sd.knuckle
        for off, r0, length in BM.FINGERS:
            o = sd.thumb * off
            base = k_ - sd.hand_y * 0.014 + o + sd.hand_z * 0.002
            tip = k_ + sd.finger_dir * length + o * 1.08
            b.tube("Finger", [base, base.lerp(tip, 0.5) + sd.hand_z * 0.003, tip], [r0, r0 * 0.95, r0 * 0.8], sides=10,
                   chain=[f"Fingers_{name}"], style=rubber([(lambda c: S.band(c.t, 0.5, 0.05), dict(groove=0.0012))]), cap=0.8)
        b.tube("Thumb", [sd.thumb_head - sd.thumb_dir * 0.01, sd.thumb_head.lerp(sd.thumb_tip, 0.5), sd.thumb_tip], [0.02, 0.019, 0.015],
               sides=10, chain=[f"Thumb_{name}"], style=rubber())


# ------------------------------------------------------------------------------------------------------------
# Legs and boots
# ------------------------------------------------------------------------------------------------------------

def legs(b):
    for name, sd in SIDE.items():
        sx = sd.sx
        h_, k_, a_ = sd.hip, sd.knee, sd.ankle
        down = nrm(k_ - h_)
        outer_front = nrm(FWD + Vector((sx * 0.55, 0, 0)))
        b.tube("Thigh", [h_ + G(0, 0.03, 0), h_.lerp(k_, 0.5), k_], [0.095, 0.085, 0.069], sides=18, chain=[f"Thigh_{name}"],
               style=fabric([(lambda c: S.line(c.t, 0.92, 0.02), GROOVE)]))
        limb_shell(b, "ThighPlate", h_, k_, 0.097, 0.073, outer_front, 64, 0.3, 0.86, 0.013, [f"Thigh_{name}"],
                   plate([(lambda c: S.line(along(c), 0.5, 0.01), GROOVE),
                          (lambda c: S.line(np.abs(around(c)), 40.0, 2.5), GROOVE),
                          (lambda c: (np.abs(around(c) + 18 * sx) < 7) * S.band(along(c), 0.68, 0.17), YELLOW)], seed=16))
        if sx < 0:
            b.ellipsoid("Holster", sd.holster + down * 0.06 + G(0.004 * sx, 0, 0), (0.026, 0.078, 0.046), frame(-down, FWD),
                        bone=f"Thigh_{name}", deform=boxy(0.3), segments=(16, 10),
                        style=metal([(lambda c: S.line(c.u[:, 1], 0.55, 0.05), GROOVE), (lambda c: S.band(c.u[:, 1], 0.85, 0.08), YELLOW)],
                                    seed=17))
        else:
            # Left thigh: a leg pouch with a strap.
            b.ellipsoid("LegPouch", sd.holster + down * 0.05 + G(0.006, 0, 0.0), (0.03, 0.06, 0.048), frame(-down, FWD),
                        bone=f"Thigh_{name}", deform=boxy(0.3), segments=(16, 10),
                        style=fabric([(lambda c: S.band(c.u[:, 1], 0.6, 0.14), dict(groove=0.002)),
                                      (lambda c: S.band(c.u[:, 1], 0.82, 0.12) * S.band(c.u[:, 2], 0.0, 0.35), DARK)]))
        dome_patch(b, "KneePad", k_ + G(0, 0.004, 0.014), R(0.068, 0.072, 0.064), None, lambda a, v: (a * 1.0, -1.0, v * 1.05), 0.016,
                   f"Shin_{name}", plate([(lambda c: S.line(c.u[:, 2], -0.05, 0.03) * (c.u[:, 1] < 0), GROOVE),
                                          (lambda c: S.line(c.u[:, 0], 0.0, 0.04) * (c.u[:, 2] > 0), GROOVE),
                                          (lambda c: S.band(c.u[:, 2], -0.45, 0.1) * (c.u[:, 1] < 0), YELLOW)], seed=19), nu=16, nv=14)
        b.tube("Shin", [k_, k_.lerp(a_, 0.5), a_ + G(0, 0.03, 0)], [0.065, 0.059, 0.051], sides=16, chain=[f"Shin_{name}"], style=fabric())
        limb_shell(b, "ShinPlate", k_, a_, 0.068, 0.056, FWD, 78, 0.12, 0.8, 0.014, [f"Shin_{name}"],
                   bplate([(lambda c: ribs(along(c), 0.12, 0.12) * (along(c) > 0.45), GROOVE),
                           (lambda c: S.line(around(c), 0.0, 3.0) * (along(c) < 0.45), GROOVE),
                           (lambda c: S.band(along(c), 0.06, 0.05), YELLOW)], seed=20))
        # Boot: cuff, ankle ring, gunmetal shell, blue toe cap, lugged rubber sole, yellow heel tab.
        b.ellipsoid("BootCuff", a_ + G(0, 0.035, 0.0), R(0.068, 0.052, 0.072), bone=f"Shin_{name}", style=metal(seed=21), deform=boxy(0.7),
                    segments=(20, 10))
        b.parts[-1].chain = [f"Shin_{name}", f"Foot_{name}"]
        b.ring("AnkleRing", a_ + G(0, 0.072, 0.0), Vector((0, 0, 1)), 0.064, 0.012, arcs=3, sides=8, chain=[f"Shin_{name}"],
               style=metal([(lambda c: S.band(c.n[:, 2], 0.0, 0.5), YELLOW)], seed=56))
        heel = G(0.105 * sx, 0.062, -0.06)
        b.tube("Boot", [heel, a_ + G(0, -0.025, 0.03), sd.ball + G(0, 0.024, 0), sd.toe + G(0, 0.014, -0.012)], [0.06, 0.064, 0.056, 0.046],
               sides=16, chain=[f"Foot_{name}", f"Toe_{name}"],
               style=metal([(lambda c: S.band(c.t, 0.0, 0.12) * C.smoothstep(-0.2, 0.3, c.n[:, 1]), YELLOW),
                            (lambda c: S.line(c.t, 0.45, 0.02), GROOVE)], seed=22), flatten=0.8)
        b.ellipsoid("ToeCap", sd.toe + G(0, 0.03, -0.035), R(0.056, 0.036, 0.06), bone=f"Toe_{name}", deform=boxy(0.6), segments=(18, 10),
                    style=metal([(lambda c: S.line(c.u[:, 2], 0.3, 0.05), GROOVE),
                                 (lambda c: S.band(c.u[:, 2], 0.62, 0.08) * (c.u[:, 1] < 0), YELLOW)], seed=57))
        b.ellipsoid("Sole", G(0.107 * sx, 0.02, 0.058), R(0.066, 0.022, 0.146), bone=f"Foot_{name}", deform=boxy(0.6), segments=(24, 10),
                    style=rubber([(lambda c: ribs(c.p[:, 1], 0.03, 0.25) * (c.u[:, 2] < 0.3), dict(groove=0.003))]))
        b.parts[-1].chain = [f"Foot_{name}", f"Toe_{name}"]


RIGID = {"Helmet", "Crest", "Brow", "Cheek", "Visor", "Chin", "Filter", "Ear", "Comms", "HelmAntenna", "HelmLamp",
         "ChestPlate", "ChestCore", "CoreRing", "Collar", "AbPlate", "BackPlate", "Belt", "Buckle", "Pouch", "Grenade", "Tasset",
         "Pack", "CoreBar", "CoreCap", "Cell", "CellCap", "HiltClip", "PackPouch", "Fin", "Antenna", "AntennaTip", "AntennaBase", "Stub",
         "Rerebrace", "Gauntlet", "WristPad", "Emitter", "Knuckles", "Finger",
         "ThighPlate", "Holster", "LegPouch", "KneePad", "ShinPlate", "AnkleRing", "ToeCap"}

LAYERS = [S.Layer("suit", None, voxel=0.0034, smooth=6, density=0.7),
          S.Layer("plates", {"Helmet", "Crest", "Brow", "Cheek", "ChestPlate", "AbPlate", "BackPlate", "Tasset", "Pauldron", "Lame", "Rerebrace",
                             "ElbowCop", "Gauntlet", "ThighPlate", "KneePad", "ShinPlate", "ToeCap"},
                  voxel=0.0018, smooth=2, density=1.0),
          S.Layer("gear", {"Visor", "Chin", "Filter", "Ear", "Comms", "HelmAntenna", "HelmLamp", "ChestCore", "CoreRing", "Collar", "Belt",
                           "Buckle", "Pouch", "Grenade", "Pack", "CoreBar", "CoreCap", "Cell", "CellCap", "HiltClip", "PackPouch", "Fin",
                           "Antenna", "AntennaTip", "AntennaBase", "Stub", "Hose", "WristPad", "Emitter", "Cuff", "Holster", "LegPouch",
                           "BootCuff", "AnkleRing", "Boot", "Sole"},
                  voxel=0.0022, smooth=3, density=0.9),
          S.Layer("hands", {"Palm", "Knuckles", "Finger", "Thumb"}, voxel=0.0015, smooth=3, density=2.0)]


# ------------------------------------------------------------------------------------------------------------
# Build, LOD, export
# ------------------------------------------------------------------------------------------------------------

def unwrap_packed(obj, face_layer, layers, margin=0.0025, angle=62.0):
    """suit.unwrap_weighted with a concave island packer: the closed plate shells and thin gear leave a lot of empty
    texture space with the default box packer (about 31 % coverage)."""
    import bmesh
    C.activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.average_islands_scale()
    bpy.ops.uv.seams_from_islands(mark_seams=True, mark_sharp=False)
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    islands = C.uvlayout._islands(bm, np.ones(len(bm.faces), dtype=bool))
    for island in islands:
        scale = layers[face_layer[island[0]]].uv_scale
        if abs(scale - 1.0) < 1e-6:
            continue
        loops = [lp for fi in island for lp in bm.faces[fi].loops]
        cx = sum(lp[uv].uv.x for lp in loops) / len(loops)
        cy = sum(lp[uv].uv.y for lp in loops) / len(loops)
        for lp in loops:
            lp[uv].uv = ((lp[uv].uv.x - cx) * scale + cx, (lp[uv].uv.y - cy) * scale + cy)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.uv.pack_islands(rotate=True, margin=margin, shape_method="CONCAVE")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    uv = bm.loops.layers.uv.active
    area3d = sum(f.calc_area() for f in bm.faces)
    area_uv = sum(abs(C.uvlayout._uv_signed_area(f, uv)) for f in bm.faces)
    stats = {"islands": len(islands), "unwrap": "smart, layer-weighted, concave pack", "uv_coverage": round(area_uv, 3),
             "world_per_uv": math.sqrt(area3d / max(area_uv, 1e-9))}
    bpy.ops.object.mode_set(mode="OBJECT")
    C.log(obj.name, "uv", stats)
    return stats, None


S.unwrap_weighted = unwrap_packed


def skin_all(mesh_obj, arm, builder):
    C.skin(mesh_obj, arm, builder, rigid=RIGID, blend=2)
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
    """build_mercer_reactions on the .blend just saved: adds the five round-35 clips, re-exports the FBX with all
    sixteen and merges its facts into the report."""
    import build_mercer_reactions as RX
    keep = sys.argv
    sys.argv = [a for a in keep] + (["--"] if "--" not in keep else []) + ["--norender"]
    try:
        RX.main()
    finally:
        sys.argv = keep


def main():
    args = C.cli_args()
    fast = bool(args.get("fast"))
    C.reset_scene()
    BM.stage_exports()
    out = C.preview_dir()
    tex, low_tex = (512, None) if fast else (TEX, LOW_TEX)
    W.build()
    builder = build_parts()
    asset, index = S.build_layered_asset(NAME, builder, LAYERS, voxel=0.0034, target_tris=GAME_TRIS, tex_size=tex, ao_distance=0.06,
                                         extrusion=0.01, ray=0.03, emission_strength=2.5, extra=S.weathering(), specular=0.4,
                                         mirror=False, symmetric=False, k=1.0, families=BM.FAMILIES, family_set=S.FAMILY_SET,
                                         low_size=low_tex, preview_detail=(0.8, 0.6), edge_gain=0.5, border_smooth=4,
                                         unwrap="smart")
    arm = BM.build_armature(NAME + "_Rig", BM.bones())
    skin_all(asset.low, arm, builder)
    poser = C.Poser(arm)
    clips = {n: BM.CLIPS[n] for n in (("Idle", "Run", "Shoot") if fast else BM.CLIPS)}
    actions = C.key_clips(poser, clips)
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
        "asset": NAME, "character": "Spencer", "round": "spencer-2026-11-08 (build_mercer2.py)",
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
    if args.get("norender"):
        return
    import spencer_previews as SP
    SP.render_file(os.path.join(C.BLEND, NAME + ".blend"), [out] + ([args["copy"]] if args.get("copy") else []),
                   args.get("prefix", "after"), ORIGINAL_BLEND)


if __name__ == "__main__":
    main()
