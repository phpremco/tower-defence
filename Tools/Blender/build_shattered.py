"""The Shattered: the continent-5 boss (Southern Crown), a Burrower overcharged by our superweapons (round 16, see
ArtSource/DESIGN_NOTES.md).

A humped knuckle-walker: massive front arms with spade fists, shorter hind legs, a high hump, a small low head with a
cracked jaw. Its armour is eight separate shell plates (bones Shell_1..8, each a separate island lifted off the core,
so a plate scaled to 0 leaves the core showing) over a core of overcharged flesh; six crystal clusters inside
(bones Crys_1..6) grow out through the gaps.

Three states on one rig, all by bone scale:
  phase 1 (intact):   Shell_1..8 = 1, Crys_1..6 = 0.25 (hidden inside the core)
  phase 2 (cracked):  Shell_1..4 = 0 (the hump), Crys_1, 2, 3, 6 = 1.0, Crys_4, 5 = 0.55
  phase 3 (bursting): all shells 0, all crystals 1.35
Walk, Idle and Hit are keyed in phase 1; PhaseBreak1 goes 1 -> 2, PhaseBreak2 2 -> 3, Death from 3 to the final
explosion. In Unity, LateUpdate writes the phase-2 or phase-3 scales over Walk/Idle/Hit (see README round 16).

Two materials (1024 + 512 Low each): `Shattered_Mat` (armour and core) and `Shattered_Crystal_Mat` (overcharged
Crysium). Clips: Walk (loop), Idle (loop), Hit, PhaseBreak1, PhaseBreak2, Death. Also exports Shattered_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_shattered.py -- [--preview <dir>]
     [--norender] [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import crysium as Y  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "Shattered"
LOD_NAME = NAME + "_LOD1"
K = 4.0
UNITY_SCALE = 1.0

# The old Burrower armour (plum-bronze, as Gravemaw's kin) with hot pink light; the overcharged core and crystal.
PAL = H.Palette(top=(0.44, 0.29, 0.3), side=(0.18, 0.1, 0.12), belly=(0.05, 0.03, 0.04), seam=(0.03, 0.018, 0.025),
                dark=(0.1, 0.055, 0.07), glow=(1.0, 0.42, 0.72), eye=(1.0, 0.72, 0.32), membrane=(0.45, 0.1, 0.22),
                bone=(0.86, 0.76, 0.74))
CORE = H.Palette(top=(0.3, 0.12, 0.2), side=(0.16, 0.05, 0.11), belly=(0.08, 0.02, 0.05), seam=(0.06, 0.01, 0.04),
                 dark=PAL.dark, glow=(1.0, 0.82, 0.96), eye=PAL.eye, membrane=(0.6, 0.12, 0.38), bone=PAL.bone)
Y.PALETTES.setdefault("overcharge", ((np.array((0.36, 0.05, 0.32)), np.array((1.0, 0.45, 0.8)), np.array((1.0, 0.95, 1.0))),
                                     (np.array((1.0, 0.55, 0.88)), np.array((1.0, 0.96, 1.0)))))

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

TORSO_C = G(0, 1.78, -0.05)
TORSO_R = (1.02, 1.5, 0.86)
TORSO_ROT = T.rot(pitch=-12)
PELVIS_C = G(0, 1.3, -1.2)
PELVIS_R = (0.78, 0.68, 0.62)
PELVIS_ROT = T.rot(pitch=6)
HEAD_C = G(0, 1.32, 1.42)
HEAD_R = (0.5, 0.5, 0.3)
HEAD_ROT = T.rot(pitch=10)
# Round 49F (Remco: "the teeth when the mouth is closed seem to go through the lips"): the lower jaw sat wedged into
# the head core (no gap even with the jaw shut), so every tooth crossed a lip. It now hangs JAW_DROP lower: a real
# slot under the head's front rim for the short upper fangs, and the jaw's front lip still juts out past the head
# (an underbite) so the long lower fangs rise outside the upper lip. Jaw ellipsoid: centre, radii, pitch.
JAW_DROP = 0.2  # round 49G: 0.15 -> 0.2, so the gums of the new mouth leave a slot between them
JAW_C = G(0, 1.05 - JAW_DROP, 1.62)
JAW_R = (0.34, 0.36, 0.14)
JAW_ROT = T.rot(pitch=-10)
GAP, THICK = 0.05, 0.1

# Round 49G (Remco on 49F: "the jaw seems not attached, and the teeth are glued against the head"): the jaw plate hung
# under the round head core with nothing joining them, and the upper fangs grew from inside the dark sphere along its
# curve. Now the head has an upper jaw (a plated muzzle jutting from the lower front of the core, its flat underside the
# upper lip), the jaw hinges visibly (on each side a jaw branch, JawRamus, rises from the back of the jaw plate under a
# cheek plate on the head, right at the jaw bone's pivot, so it stays tucked in at every opening), and a gum runs along
# both lips (mouth49g.py) with the teeth (shattered_detail.py) rooted in it, pointing into the mouth.
MUZZLE_C = G(0, 1.2, 1.66)
MUZZLE_R = (0.42, 0.36, 0.1)
MUZZLE_ROT = T.rot(pitch=-4)


def muzzle_shape(b, style=None, k=1.0):
    b.ellipsoid("HeadCore", HEAD_C, HEAD_R, HEAD_ROT, bone="Head", segments=(36, 18), style=style)
    return b.ellipsoid("Muzzle", MUZZLE_C, MUZZLE_R, MUZZLE_ROT, bone="Head", segments=(40, 16),
                       deform=C.chiselled(12, 33, 0.08), style=style)


def jaw_shape(b, style=None):
    return b.ellipsoid("Jaw", JAW_C, JAW_R, JAW_ROT, bone="Jaw", segments=(32, 16), deform=C.chiselled(12, 30, 0.07),
                       style=style)


def _mouth():
    import mouth49g as MO
    return MO.Mouth(u=[(-0.27, 1.5), (-0.25, 1.62), (-0.2, 1.74), (-0.12, 1.84), (0.0, 1.89), (0.12, 1.84),
                       (0.2, 1.74), (0.25, 1.62), (0.27, 1.5)],
                    upper_fn=muzzle_shape, lower_fn=jaw_shape, gum_r=0.035, gum_sink=0.015, ray_from=(0.3, 2.2),
                    lower_inset=0.92, clearance=0.02, upper_teeth=7, lower_teeth=6, tooth_r=0.045, canine=1.35,
                    out_upper=0.15, out_lower=-0.1)


MOUTH = _mouth()

ARM_TABLE = [((0.82, 1.95, 0.72), (1.35, 0.0, 1.5), (1.25, 1.25))]
LEG_TABLE = [((0.6, 1.15, -1.25), (1.0, 0.0, -1.55), (0.72, 0.78))]

# Shell plates: (bone, ellipsoid, axes, a range, b range, sign, mirrored, chevron). Ellipsoid unit coordinates:
# 0 = x (right), 1 = y (back, Blender -forward), 2 = z (up).
SHELLS = [("Shell_1", "torso", (0, 1), (-0.62, 0.62), (-0.82, -0.36), 1, False, 0.9),
          ("Shell_2", "torso", (0, 1), (-0.66, 0.66), (-0.3, 0.16), 1, False, 0.7),
          ("Shell_3", "torso", (0, 1), (-0.62, 0.62), (0.22, 0.66), 1, False, 0.5),
          ("Shell_4", "pelvis", (0, 1), (-0.62, 0.62), (-0.3, 0.62), 1, False, 0.0),
          ("Shell_5", "torso", (1, 2), (-0.86, -0.38), (-0.05, 0.62), 1, True, 0.0),
          ("Shell_6", "torso", (1, 2), (-0.3, 0.42), (-0.42, 0.55), 1, True, 0.0),
          ("Shell_7", "pelvis", (1, 2), (-0.35, 0.55), (-0.25, 0.6), 1, True, 0.0),
          ("Shell_8", "head", (0, 1), (-0.6, 0.6), (-0.55, 0.45), 1, False, 1.0)]
SHELL_NAMES = [s[0] for s in SHELLS]
# Crystal clusters: (bone, bone head (inside the core), [(direction, length, radius)], mirrored).
CRYS = [("Crys_1", G(0, 1.95, 0.55), [((0, 1.0, 0.45), 1.1, 0.13), ((0.4, 0.9, 0.5), 0.8, 0.09), ((-0.4, 0.9, 0.5), 0.8, 0.09)], False),
        ("Crys_2", G(0, 2.1, -0.05), [((0, 1.0, 0.0), 1.35, 0.15), ((0.45, 0.9, -0.1), 0.95, 0.1), ((-0.45, 0.9, -0.1), 0.95, 0.1),
                                       ((0.0, 0.9, -0.45), 0.85, 0.09)], False),
        ("Crys_3", G(0, 1.9, -0.65), [((0, 1.0, -0.5), 1.05, 0.12), ((0.45, 0.8, -0.5), 0.75, 0.08), ((-0.45, 0.8, -0.5), 0.75, 0.08)], False),
        ("Crys_4", G(0, 1.9, 0.5), [((1.0, 0.45, 0.25), 1.35, 0.12), ((1.0, 0.75, 0.0), 0.95, 0.08)], True),
        ("Crys_5", G(0, 1.55, -0.1), [((1.0, 0.0, 0.1), 1.35, 0.12), ((1.0, -0.3, -0.35), 1.0, 0.08)], True),
        ("Crys_6", G(0, 1.35, -1.2), [((0, 1.0, -0.35), 0.95, 0.11), ((0.6, 0.7, -0.4), 0.85, 0.08), ((-0.6, 0.7, -0.4), 0.85, 0.08)], False)]
CRYS_NAMES = [c[0] for c in CRYS]
SCALE_BONES = SHELL_NAMES + CRYS_NAMES
PHASE = {1: ({n: 1.0 for n in SHELL_NAMES}, {n: 0.25 for n in CRYS_NAMES}),
         2: ({n: (0.0 if i < 4 else 1.0) for i, n in enumerate(SHELL_NAMES)},
             {n: (0.55 if n in ("Crys_4", "Crys_5") else 1.0) for n in CRYS_NAMES}),
         3: ({n: 0.0 for n in SHELL_NAMES}, {n: 1.35 for n in CRYS_NAMES})}


def ellipsoid(which):
    return {"torso": (TORSO_C, TORSO_R, TORSO_ROT), "pelvis": (PELVIS_C, PELVIS_R, PELVIS_ROT),
            "head": (HEAD_C, HEAD_R, HEAD_ROT)}[which]


def arms():
    return K3.limbs(ARM_TABLE, pole=(0.4, 0.2, -1.0), prefix="Arm")


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.3, 0.3, 1.0))


def shell_anchor(entry):
    """Bone head of a shell: the middle of the plate pulled onto the midline (mirrored pairs share one bone)."""
    name, which, axes, ar, br, sign, mirrored, _ = entry
    c, r, rot_m = ellipsoid(which)
    w = [0.0, 0.0, 0.0]
    w[axes[0]], w[axes[1]] = (ar[0] + ar[1]) / 2, (br[0] + br[1]) / 2
    w[3 - axes[0] - axes[1]] = sign * math.sqrt(max(1 - w[axes[0]] ** 2 - w[axes[1]] ** 2, 0.01))
    p = Vector(c) + rot_m @ Vector((w[0] * r[0], w[1] * r[1], w[2] * r[2]))
    if mirrored:
        p.x = 0.0
    return p


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def patch_point(entry, a, bb, off, sx=None):
    name, which, axes, ar, br, sign, mirrored, _ = entry
    c, r, rot_m = ellipsoid(which)
    w = [0.0, 0.0, 0.0]
    w[axes[0]], w[axes[1]] = ar[0] + (ar[1] - ar[0]) * a, br[0] + (br[1] - br[0]) * bb
    w[3 - axes[0] - axes[1]] = (sx if sx is not None else sign) * math.sqrt(max(1 - w[axes[0]] ** 2 - w[axes[1]] ** 2, 0.01))
    unit = Vector(w).normalized()
    n = rot_m @ Vector((unit.x / r[0], unit.y / r[1], unit.z / r[2])).normalized()
    return Vector(c) + rot_m @ Vector((unit.x * r[0], unit.y * r[1], unit.z * r[2])) + n * off, n


# Round 49G (curator, strict loose-part check: "Shell_2 ... a 5-6 cm armour gap"): every shell plate is tied to the core
# by sinew straps that cross the gap (on the plate's own bone, so they fly off with it). The plates are remeshed apart
# from the core (build_shattered2.separate_shell_fuse) so each stays its own island for the scale-0 phase breaks.
STRAP_SPOTS = ((0.22, 0.28), (0.78, 0.28), (0.22, 0.72), (0.78, 0.72), (0.5, 0.5))
SHELL_PARTS = ("Shell", "Spike", "ShellStrap")


def add_straps(b, entry, k):
    name, mirrored = entry[0], entry[6]
    style = H.glow_flesh(CORE, k * 0.4, glow=0.5, flank=(0.3, 0.9), folds=8.0)
    for sx in ((-1, 1) if mirrored else (None,)):
        for a, bb in STRAP_SPOTS:
            outer, n = patch_point(entry, a, bb, GAP + THICK * 0.5, sx)
            inner, _ = patch_point(entry, a, bb, -0.12, sx)
            b.tube("ShellStrap", [outer, outer.lerp(inner, 0.5), inner], [0.075, 0.06, 0.1], sides=10, chain=[name],
                   style=style)


def add_spikes(b, entry, k):
    """Hooked spikes on the rear edge of the hump plates (they fly off with the plate)."""
    name = entry[0]
    if name not in ("Shell_1", "Shell_2", "Shell_3", "Shell_4"):
        return
    for a in (0.2, 0.5, 0.8):
        base, n = patch_point(entry, a, 0.82, GAP + THICK + (0.18 if a == 0.5 else 0.0))
        back = G(0, 0, -1)
        tip = base + n * (0.42 if a == 0.5 else 0.28) + back * 0.25
        b.tube("Spike", [base - n * 0.05, base.lerp(tip, 0.5) + n * 0.04, tip], [0.09 if a == 0.5 else 0.065, 0.05, 0.006],
               sides=10, chain=[name], style=H.hook(PAL, k * 0.4, glow=0.6, glow_from=0.75))


def mouth_parts(b, k):
    """Round 49G: the muzzle (upper jaw), the jaw hinge (cheek plates and jaw branches), the gums, a dark palate and
    tongue and the overcharge glow deep in the throat."""
    import mouth49g as MO
    b.ellipsoid("Muzzle", MUZZLE_C, MUZZLE_R, MUZZLE_ROT, bone="Head", segments=(40, 16),
                deform=C.chiselled(12, 33, 0.08),
                style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.7, chevron=0.6, chevron_angle=1.1, lip=0.8, cells=8,
                                      seed=35))
    hinge = G(0, 1.12, 1.3)  # the Jaw bone's head: points near it barely move when the jaw opens
    for side, sx in SIDES:
        # Round 49G follow-up (curator: "the jaw still reads as a separate grey slab"): bigger cheek plates standing
        # proud of the head's sides and thick jaw branches running up the outside of the face into them.
        b.ellipsoid("Cheek", hinge + G(0.4 * sx, 0.02, 0.04), (0.22, 0.26, 0.21), T.rot(yaw=12 * sx), bone="Head",
                    segments=(24, 12), deform=C.chiselled(10, 37 + (sx > 0), 0.1),
                    style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.6, chevron=0.0, lip=0.8, cells=6, seed=37))
        b.tube("JawRamus", [G(0.27 * sx, 0.84, 1.55), G(0.36 * sx, 0.95, 1.4), hinge + G(0.4 * sx, 0.0, 0.02)],
               [0.17, 0.16, 0.14], sides=12, chain=["Jaw"], flatten=0.75,
               style=K2.armour_plate(PAL, k * 0.4, rim_glow=0.8, chevron=0.0, lip=0.6, cells=6, seed=39))
    up, lo = MO.gum_path(MOUTH, True), MO.gum_path(MOUTH, False)
    radii = [MOUTH.gum_r * 0.8] + [MOUTH.gum_r] * (len(up) - 2) + [MOUTH.gum_r * 0.8]
    gum = MO.gum_style(1, flesh=(0.3, 0.06, 0.2), deep=(0.06, 0.01, 0.06), glow=(1.0, 0.42, 0.78), glow_k=0.15)
    b.tube("UpperGum", up, radii, sides=12, chain=["Head"], style=gum)
    b.tube("LowerGum", lo, radii, sides=12, chain=["Jaw"], style=gum)
    mid_up = sum((v.z for v in up), 0.0) / len(up)
    mid_lo = sum((v.z for v in lo), 0.0) / len(lo)
    flesh = MO.gum_style(7, flesh=(0.2, 0.04, 0.14), deep=(0.04, 0.01, 0.04))
    b.ellipsoid("Palate", G(0, mid_up + 0.01, 1.68), (0.24, 0.2, 0.03), bone="Head", segments=(24, 10), style=flesh)
    b.ellipsoid("Tongue", G(0, mid_lo - 0.01, 1.66), (0.2, 0.2, 0.035), bone="Jaw", segments=(24, 10), style=flesh)
    b.ellipsoid("Throat", G(0, (mid_up + mid_lo) * 0.5, 1.5), (0.16, 0.12, 0.05), bone="Head", segments=(16, 8),
                style=H.inner_glow(CORE, 0.3))


def body_parts():
    b = C.PartBuilder()
    k = K
    core = T.kiln_crust(CORE, k * 0.6, cells=22, seed=1, glow=0.6, warp=0.4, under=0.3, deep=(0.95, 0.2, 0.62))
    b.ellipsoid("Core", TORSO_C, TORSO_R, TORSO_ROT, bone="Chest", segments=(56, 28), style=core)
    b.ellipsoid("CorePelvis", PELVIS_C, PELVIS_R, PELVIS_ROT, bone="Pelvis", segments=(40, 20),
                style=T.kiln_crust(CORE, k * 0.6, cells=12, seed=2, glow=0.6, warp=0.4, under=0.3, deep=(0.95, 0.2, 0.62)))
    b.ellipsoid("HeadCore", HEAD_C, HEAD_R, HEAD_ROT, bone="Head", segments=(36, 18),
                style=T.kiln_crust(CORE, k * 0.4, cells=8, seed=3, glow=0.9, deep=(0.95, 0.2, 0.62)))
    for i, entry in enumerate(SHELLS):
        name, which, axes, ar, br, sign, mirrored, chev = entry
        c, r, rot_m = ellipsoid(which)
        style = T.shell_plate(PAL, k * 0.5, axes, ar, br, crack=0.55, rim_glow=0.3, seed=10 + i, chevron=chev * 0.6)
        if mirrored:
            # The third axis is x: one plate per side, both on the same (midline) bone.
            for side, sx in SIDES:
                T.ellipsoid_patch(b, "Shell", c, r, rot_m, axes, ar, br, sx, GAP, THICK, name, style, bulge=0.04,
                                  keel=0.12)
        else:
            T.ellipsoid_patch(b, "Shell", c, r, rot_m, axes, ar, br, sign, GAP, THICK, name, style, bulge=0.06,
                              keel=0.0 if name == "Shell_8" else 0.28)
        add_spikes(b, entry, k)
        add_straps(b, entry, k)
    # Jaw: a cracked lower jaw with the core showing; eyes.
    plate = K2.armour_plate(PAL, k * 0.4, rim_glow=0.8, chevron=0.0, lip=0.8, cells=10, seed=31)
    jaw_shape(b, plate)
    mouth_parts(b, k)
    for side, sx in SIDES:
        root = G(0.28 * sx, 1.02 - JAW_DROP, 1.7)
        b.tube("Tusk", [root, root + G(0.18 * sx, 0.12, 0.28), root + G(0.22 * sx, 0.45, 0.42)], [0.08, 0.06, 0.008],
               sides=10, chain=["Jaw"], style=H.hook(PAL, k * 0.4, glow=0.5, glow_from=0.8))
    for sx in (-1, 1):
        for j, (x, u, f) in enumerate(((0.2, 1.42, 1.78), (0.3, 1.32, 1.7))):
            b.ellipsoid("Eye", G(x * sx, u, f), (0.05,) * 2 + (0.035,), bone="Head", segments=(12, 8),
                        style=H.eye(PAL, k * 0.3, 1.0))
    # Arms with spade fists, hind legs.
    for (side, i), lg in arms().items():
        sh, el, wr = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5), el], [0.36, 0.3, 0.24], sides=16, chain=[lg.names[0]],
               style=H.leg(PAL, k * 0.5, "Femur", spines=3))
        b.tube("Forearm", [el, el.lerp(wr, 0.5), wr + Vector((0, 0, 0.25))], [0.24, 0.3, 0.32], sides=16,
               chain=[lg.names[1]], style=H.leg(PAL, k * 0.5, "Femur", spines=2))
        b.ellipsoid("Fist", wr + Vector((0, 0, 0.2)), (0.36, 0.4, 0.24), T.rot(yaw=-10 * sx), bone=lg.names[1],
                    segments=(32, 16), deform=C.chiselled(14, 40, 0.08, squash=(0.8, 1.0)),
                    style=K2.armour_plate(PAL, k * 0.5, rim_glow=0.7, chevron=0.0, lip=0.8, cells=10, seed=41))
        for j, spread in enumerate((-0.2, 0.0, 0.2)):
            root = wr + G(spread * sx + 0.05 * sx, 0.12, 0.25)
            b.tube("Knuckle", [root, root + G(0.02 * sx, -0.02, 0.18), root + G(0.0, -0.13, 0.28)], [0.09, 0.06, 0.01],
                   sides=10, chain=[lg.names[1]], flatten=0.6, style=H.blade(PAL, k * 0.4, glow=0.6, teeth=2))
    for (side, i), lg in legs().items():
        hp, kn, ft = lg.rest_points
        b.tube("Thigh", [hp, hp.lerp(kn, 0.5), kn], [0.3, 0.26, 0.2], sides=14, chain=[lg.names[0]],
               style=H.leg(PAL, k * 0.5, "Femur", spines=2))
        b.tube("Shin", [kn, kn.lerp(ft, 0.5), ft + Vector((0, 0, 0.06))], [0.2, 0.18, 0.2], sides=14, chain=[lg.names[1]],
               style=H.leg(PAL, k * 0.5, "Tibia"))
    return b


def crystal_parts():
    b = C.PartBuilder()
    for name, head, shards, mirrored in CRYS:
        for j, (d, length, radius) in enumerate(shards):
            for sx in ((-1, 1) if mirrored else (1,)):
                direction = G(d[0] * sx, d[1], d[2]).normalized()
                b.shard("Cluster", head + direction * 0.05, direction, radius * 1.3, length * 1.45, sides=8, flute=0.45,
                        twist=1.1 * sx, bone=name, style=Y.shard_style("overcharge", 1.0, seed=60 + j))
    # Crystal teeth in the cracked jaw (always visible: the first sign of what is inside).
    for j, x in enumerate((-0.2, -0.07, 0.07, 0.2)):
        b.shard("Tooth", G(x, 1.12, 1.78), G(0.2 * x, 1.0, 0.5), 0.04, 0.22 - 0.04 * abs(x) * 3, sides=6, flute=0.3,
                bone="Jaw", style=Y.shard_style("overcharge", 0.9, seed=70 + j))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.4, 0), None, False),
           ("Pelvis", G(0, 1.25, -1.5), G(0, 1.5, -0.55), "Root", True),
           ("Chest", G(0, 1.5, -0.55), G(0, 1.95, 0.9), "Pelvis", True),
           ("Head", G(0, 1.45, 1.05), G(0, 1.25, 1.85), "Chest", True),
           ("Jaw", G(0, 1.12, 1.3), G(0, 0.98, 1.9), "Head", True)]
    for entry in SHELLS:
        name, which = entry[0], entry[1]
        parent = {"torso": "Chest", "pelvis": "Pelvis", "head": "Head"}[which]
        a = shell_anchor(entry)
        c = ellipsoid(which)[0]
        d = (a - Vector(c)).normalized()
        out.append((name, a - d * 0.2, a + d * 0.2, parent, True))
    for name, head, shards, mirrored in CRYS:
        parent = "Pelvis" if name == "Crys_6" else "Chest"
        out.append((name, head, head + G(0, 0.3, 0), parent, True))
    for lg in arms().values():
        out += lg.rest_bones("Chest")
    for lg in legs().values():
        out += lg.rest_bones("Pelvis")
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs(off=None, pitch=0.0, roll=0.0, yaw=0.0, chest=0.0, chest_roll=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
          shells=None, crys=None, blow=None):
    """Hinge specs (radians); `shells`/`crys`: {bone: scale}; `blow`: {shell bone: (offset vector, spin)}."""
    shells = shells or PHASE[1][0]
    crys = crys or PHASE[1][1]
    out = {"Root": {"rot": []},
           "Pelvis": {"rot": [(Z, yaw), (Yv, roll), (X, -pitch)], "loc": off if off is not None else Vector()},
           "Chest": {"rot": [(Yv, chest_roll), (X, -chest)]},
           "Head": {"rot": [(Z, head_yaw), (X, -head)]},
           "Jaw": {"rot": [(X, jaw)]}}
    for n in SHELL_NAMES:
        spec = {"rot": [], "scale": max(shells[n], 0.001)}
        if blow and n in blow:
            off_v, spin = blow[n]
            spec["loc"] = off_v
            spec["rot"] = [(Vector((0.3, 0.2, 1.0)).normalized(), spin)]
        out[n] = spec
    for n in CRYS_NAMES:
        out[n] = {"rot": [], "scale": max(crys[n], 0.001)}
    return out


def place(poser, sp, arm_feet, leg_feet):
    K3.place_legs(poser, sp, arms(), arm_feet, body="Chest")
    return K3.place_legs(poser, sp, legs(), leg_feet, body="Pelvis")


def planted():
    return {k_: l.foot.copy() for k_, l in arms().items()}, {k_: l.foot.copy() for k_, l in legs().items()}


WALK_FRAMES = 56
STRIDE, LIFT, STANCE = 0.9, 0.3, 0.62


def walk_spec(poser):
    am, lg = arms(), legs()

    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(off=Vector((0, 0, 0.06 * math.cos(2 * w))), roll=math.radians(4) * math.sin(w),
                   yaw=math.radians(3) * math.sin(w), chest_roll=-math.radians(4) * math.sin(w),
                   chest=math.radians(2) * math.sin(2 * w), head=-math.radians(3) * math.sin(2 * w + 0.6),
                   head_yaw=math.radians(4) * math.sin(w), jaw=math.radians(6) + math.radians(3) * math.sin(2 * w))
        a_feet = K3.gait_feet(am, t, {("L", 1): 0.0, ("R", 1): 0.5}, STANCE, STRIDE, LIFT)
        l_feet = K3.gait_feet(lg, t, {("R", 1): 0.1, ("L", 1): 0.6}, STANCE, STRIDE * 0.9, LIFT * 0.7)
        return place(poser, sp, a_feet, l_feet)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(off=Vector((0, 0, 0.04 * math.sin(w))), chest=math.radians(2) * math.sin(w),
                   head_yaw=math.radians(12) * math.sin(w + 0.5), head=math.radians(3) * math.sin(2 * w),
                   jaw=math.radians(6) + math.radians(8) * max(0.0, math.sin(2 * w + 1.0)) ** 2,
                   chest_roll=math.radians(1.5) * math.sin(3 * w))
        return place(poser, sp, *planted())
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(off=Vector((0, 0.08 * k, 0.03 * k)), chest=-math.radians(8) * k, roll=math.radians(3) * k,
                   head=-math.radians(10) * k, jaw=math.radians(6 + 16 * max(k, 0)))
        return place(poser, sp, *planted())
    return 14, spec, False


def lerp_phase(a, b, t, names):
    return {n: a[n] + (b[n] - a[n]) * t for n in names}


def blow_off(f, start, names):
    """Shells thrown off from frame `start` (one every 2 frames): flung outward and up, spinning, shrinking away
    by 8 frames after their start."""
    blow, scale = {}, {}
    for i, n in enumerate(names):
        s0 = start + 2 * i
        t = T.ease(s0, s0 + 8, f)
        entry = next(e for e in SHELLS if e[0] == n)
        c = ellipsoid(entry[1])[0]
        d = (shell_anchor(entry) - Vector(c)).normalized()
        blow[n] = (d * 1.2 * t + Vector((0, 0, 0.6)) * math.sin(math.pi * min(t, 1.0)), math.radians(140) * t)
        scale[n] = 1.0 - t
    return blow, scale


BREAK1_FRAMES, BREAK1_BURST = 40, 12
BREAK2_FRAMES, BREAK2_BURST = 44, 14


def break_spec(poser, frames, burst, before, after, names):
    def spec(t, f):
        brace = T.pulse(f, 0, burst - 2, burst + 2)
        rear = T.pulse(f, burst - 2, burst + 6, frames - 4)
        blow, sh = blow_off(f, burst, names)
        shells = dict(PHASE[before][0])
        shells.update(sh)
        grow = T.ease(burst, burst + 12, f)
        over = math.sin(math.pi * T.ease(burst + 6, burst + 20, f)) * 0.15
        crys = lerp_phase(PHASE[before][1], PHASE[after][1], grow, CRYS_NAMES)
        crys = {n: v + over * (PHASE[after][1][n] - PHASE[before][1][n] > 0) for n, v in crys.items()}
        shake = math.sin(f * 2.6) * T.pulse(f, 2, burst, burst + 14)
        sp = specs(off=Vector((0, 0, -0.15 * brace + 0.12 * rear)), chest=-math.radians(6) * brace + math.radians(-14) * rear,
                   chest_roll=math.radians(3) * shake, head=-math.radians(20) * rear, jaw=math.radians(6 + 30 * rear),
                   shells=shells, crys=crys, blow=blow)
        return place(poser, sp, *planted())
    return frames, spec, False


def break1_spec(poser):
    return break_spec(poser, BREAK1_FRAMES, BREAK1_BURST, 1, 2, SHELL_NAMES[:4])


def break2_spec(poser):
    return break_spec(poser, BREAK2_FRAMES, BREAK2_BURST, 2, 3, SHELL_NAMES[4:])


DEATH_FRAMES, DEATH_BLAST = 80, 46


def death_spec(poser):
    """Phase 3 to the end: the crystals swell and shudder past breaking (to 1.9), the brute sags onto its fists, the
    final explosion at DEATH_BLAST (the crystals flash to 2.3 and are gone 8 frames later: VFX shards there), the
    husk falls."""
    am, lg = arms(), legs()

    def spec(t, f):
        swell = T.ease(0, DEATH_BLAST - 4, f)
        shudder = math.sin(f * 3.1) * swell * (1 - T.ease(DEATH_BLAST, DEATH_BLAST + 2, f))
        flash = T.pulse(f, DEATH_BLAST - 4, DEATH_BLAST, DEATH_BLAST + 8)
        gone = T.ease(DEATH_BLAST + 2, DEATH_BLAST + 8, f)
        fall = T.ease(DEATH_BLAST - 2, DEATH_BLAST + 20, f)
        crys = {n: (1.35 + 0.55 * swell + 0.4 * flash) * (1 - gone) for n in CRYS_NAMES}
        sp = specs(off=Vector((0, -0.1 * fall, -0.2 * swell - 0.7 * fall)), pitch=-math.radians(6) * fall,
                   roll=math.radians(8) * fall, chest=math.radians(10) * swell - math.radians(4) * fall,
                   chest_roll=math.radians(2) * shudder, head=-math.radians(22) * fall + math.radians(10) * flash,
                   jaw=math.radians(6 + 34 * swell * (1 - fall) + 20 * fall), shells=PHASE[3][0], crys=crys)
        sp = K3.splay_feet(poser, sp, am, fall, twitch=shudder, out=1.15, body="Chest")
        return K3.splay_feet(poser, sp, lg, fall, out=1.1, body="Pelvis")
    return DEATH_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "PhaseBreak1": break1_spec,
         "PhaseBreak2": break2_spec, "Death": death_spec}
BODY_RIGID = {"Shell", "Spike", "Jaw", "Tusk", "Fist", "Muzzle", "Cheek", "JawRamus", "UpperGum", "LowerGum",
              "Palate", "Tongue", "Throat"}
CRYSTAL_RIGID = {"Cluster", "Tooth"}
LOD_DROP = {"Jaw": "Head"}


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("shattered", [(body_parts(), (0.3, 0.18, 0.2), None), (crystal_parts(), (1.0, 0.5, 0.85), (1.0, 0.5, 0.85))],
                   bones(), CLIPS, out, (0, 0, 1.5), 13.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("p2", "close", "PhaseBreak1", BREAK1_FRAMES),
                    ("p3", "close", "PhaseBreak2", BREAK2_FRAMES), ("burst", "close", "PhaseBreak1", BREAK1_BURST + 5),
                    ("dead", "close", "Death", DEATH_FRAMES)],
                   [("PhaseBreak1", "close", range(0, BREAK1_FRAMES + 1, 5)), ("Death", "close", range(0, DEATH_FRAMES + 1, 10)),
                    ("Walk", "side", range(0, WALK_FRAMES, 7))],
                   scale_bones=SCALE_BONES, rigid=BODY_RIGID | CRYSTAL_RIGID)
        return
    C.reset_scene()
    mats = [T.Mat(body_parts(), 17500, ("chitin", "membrane", "rock"), voxel=0.012, ao=0.35, k=K, emission=2.6,
                  specular=0.3, extra=H.weathering(K, scratches=0.6, dust=0.3, seed=191, markings=PAL.dark,
                                                   marking_amount=0.15), rigid=BODY_RIGID, blend=0, lod_share=0.7,
                  islands=SHELL_NAMES),
            T.Mat(crystal_parts(), 6500, ("crystal", "rock"), suffix="Crystal", voxel=0.009, ao=0.2, extrusion=0.04,
                  ray=0.12, k=K * 0.6, emission=2.8, specular=0.45, rigid=CRYSTAL_RIGID, blend=0, smooth=2, lod_share=0.3)]
    report = {"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, under the chest",
              "phases": {str(p): {"shells": v[0], "crystals": v[1]} for p, v in PHASE.items()},
              "phase_break_1": {"burst_frame": BREAK1_BURST, "shells_off": SHELL_NAMES[:4], "frames": BREAK1_FRAMES},
              "phase_break_2": {"burst_frame": BREAK2_BURST, "shells_off": SHELL_NAMES[4:], "frames": BREAK2_FRAMES},
              "death": {"explosion_frame": DEATH_BLAST, "crystals_gone_frame": DEATH_BLAST + 8},
              "emission_per_phase": {"1": 2.6, "2": 3.4, "3": 4.5}}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=LOD_DROP, scale_bones=SCALE_BONES,
        report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("shattered", arm, mesh, actions, lod, out, assets, (0, 0, 1.5), 13.0, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40),
                      ("phase2", "close", "PhaseBreak1", BREAK1_FRAMES), ("phase3", "close", "PhaseBreak2", BREAK2_FRAMES),
                      ("burst", "close", "PhaseBreak2", BREAK2_BURST + 6), ("blast", "close", "Death", DEATH_BLAST),
                      ("dead", "close", "Death", DEATH_FRAMES)],
               strips=[("Walk", "side", range(0, WALK_FRAMES, 7)), ("PhaseBreak1", "close", range(0, BREAK1_FRAMES + 1, 5)),
                       ("PhaseBreak2", "close", range(0, BREAK2_FRAMES + 1, 5)), ("Death", "close", range(0, DEATH_FRAMES + 1, 10))],
               macros=[("shell", G(0.3, 2.5, 0.2), 3.0, -40, 35, "Idle", 20), ("core", G(0.3, 2.3, 0.0), 3.2, -40, 35, "PhaseBreak2", BREAK2_FRAMES),
                       ("head", G(0, 1.3, 1.6), 2.6, -30, 10, "Idle", 20)])


if __name__ == "__main__":
    main()
