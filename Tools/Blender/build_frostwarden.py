"""Frost Warden: the continent-3 boss (Northern Crown). Round 32: concept A "Rime Colossus" (Remco's pick of the
round-31 concepts; see ArtSource/DESIGN_NOTES.md round 32). Replaces the round-16/30 upright glaive sentinel.

One anatomy that reads at once from the 44-degree game camera: a hunched knuckle-walker. Almost all of the mass is in
the shoulders and the forearms; two huge fists carry grown-ice mauls (the weapon) and walk on their knuckles. A great
hump rises behind the shoulders with frost vents along its flanks and a crest of rage icicles on its ridge
(`Icicle_1..5`, build_frostwarden_mech.py). The head is a low, flat wedge slung forward between the fists, with three
amber eyes in a triangle and a cyan maw between four mandible plates (the frost breath, `Socket_Breath`). Short,
thick hind legs push from behind. Ice grows out of the body (elbow spurs, vent crystals, forearm ridges, knee spurs,
brow crystals), so the armour reads as part of the creature, not as plates stuck on.

Two materials (1024 + 512 Low each): `FrostWarden_Mat` (cold slate hide, light on top and dark below, irregular
cracks with a faint cyan core, rime on the up faces, amber eyes, cyan maw and vents) and `FrostWarden_Ice_Mat` (the
fist mauls and the grown ice). The ice armour (`FrostWarden_IceArmour`) is added by build_frostwarden_mech.py.

Rig: Root, Hips, Spine (hump), Chest (shoulder girdle), Neck, Head, Jaw; three-bone arms `Arm_<L|R>_Upper/Fore/Fist`
(the fist is the arm's "foot": the knuckle contact) and three-bone hind legs `Leg_<L|R>_Upper/Lower/Foot`. Clips:
Walk (loop), WalkEnraged (loop), Idle (loop), Hit, Death, FreezePulse, Enrage. Also exports FrostWarden_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_frostwarden.py -- [--preview <dir>]
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
import ice_kit as IK  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "FrostWarden"
LOD_NAME = NAME + "_LOD1"
K = 4.0
UNITY_SCALE = 1.0
SC = 1.0  # round 32: designed in final metres (kept for the mech script and old callers)
R = math.radians

# Cold slate hide: light frosted top, mid slate flanks, near-black belly (lifts it off the dark ground). Cyan glow only
# in the crack cores, the vents and the maw; amber only in the three eyes (the enemy colour code).
PAL = H.Palette(top=(0.58, 0.62, 0.68), side=(0.26, 0.29, 0.34), belly=(0.05, 0.065, 0.09), seam=(0.03, 0.04, 0.055),
                dark=(0.07, 0.09, 0.12), glow=(0.42, 0.82, 1.0), eye=(1.0, 0.6, 0.18), membrane=(0.1, 0.17, 0.24),
                bone=(0.78, 0.86, 0.92), dust=(0.7, 0.8, 0.88))
FROST = np.array((0.8, 0.88, 0.94))
PLATE_TOP = np.array((0.4, 0.5, 0.62))     # carapace: bluer and darker than the hide, so the plating reads
PLATE_SIDE = np.array((0.13, 0.18, 0.26))
MARK = np.array((0.1, 0.13, 0.19))          # dark markings across the hide
GLOW = np.array((0.42, 0.82, 1.0))
EYE = (1.0, 0.6, 0.18)
ICE_K = K * 0.55
# Round 32: lighter, frosted ice (more clear blue, rime on ridges and up faces, dim crack light),
# shared by the maul, the grown ice and the armour floes. ice_kit's colours stay as they are (props use them).
ICE_LOOK = dict(crack_glow=0.42, rime=1.45, top_frost=0.85, clear=0.9)

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

# ------------------------------------------------------------------------------------------------------------
# Skeleton (game space G(x, up, forward), metres; right side, mirrored)
# ------------------------------------------------------------------------------------------------------------

HIPS = (G(0, 1.95, -1.3), G(0, 2.35, -0.55))
SPINE = (G(0, 2.35, -0.55), G(0, 3.05, 0.15))
CHEST = (G(0, 3.05, 0.15), G(0, 2.95, 0.95))
NECK = (G(0, 2.85, 1.0), G(0, 2.68, 1.6))
HEAD = (G(0, 2.68, 1.6), G(0, 2.5, 2.5))
JAW = (G(0, 2.36, 1.75), G(0, 2.16, 2.5))

SHOULDER = (1.5, 3.0, 0.8)
FIST_FOOT = (1.8, 0.0, 1.95)            # knuckle contact
ARM_LENGTHS = (1.48, 1.7, 0.5)
FIST_DIR = (0.05, -1.0, 0.22)           # wrist -> knuckles (x, up, fwd)
ARM_POLE = (1.0, 0.1, -0.55)            # the elbow flares out and back
HIP = (0.85, 1.85, -1.3)
HIND_FOOT = (1.1, 0.0, -1.25)
LEG_LENGTHS = (1.05, 0.95, 0.4)
TOE_DIR = (0.0, -0.45, 1.0)             # ankle -> toe contact
LEG_POLE = (0.25, 0.0, 1.0)             # the knee points forward


def gdir(v, sx=1):
    return G(v[0] * sx, v[1], v[2]).normalized()


# The knuckles turn over the front between fists raised overhead and fists hammered down.
FIST_OVER = G(0.0, 0.0, 1.0)


def turn(a, b, k, via=None):
    """Direction turned from `a` towards `b` by k along the arc between them. Round 18: a straight lerp between two
    nearly opposite fist directions passes close to zero, so the normalised direction (and the fist with it) flipped
    110-140 degrees in one frame; along the arc it sweeps evenly.
    `via`: a direction the turn passes through (a hammer blow turns the knuckles over the front, not round the back),
    at an even angular speed over both legs."""
    k = min(max(k, 0.0), 1.0)
    a, b = Vector(a).normalized(), Vector(b).normalized()
    if via is None:
        return a.slerp(b, k, a).normalized()
    via = Vector(via).normalized()
    first, second = a.angle(via, 0.0), via.angle(b, 0.0)
    angle = k * (first + second)
    if angle <= first:
        return a.slerp(via, angle / first if first > 1e-6 else 1.0, a).normalized()
    return via.slerp(b, (angle - first) / second if second > 1e-6 else 1.0, via).normalized()


def make_limbs():
    arms, legs = {}, {}
    for side, sx in SIDES:
        arms[side] = C.Limb([f"Arm_{side}_Upper", f"Arm_{side}_Fore", f"Arm_{side}_Fist"], T.gp(SHOULDER, sx),
                            T.gp(FIST_FOOT, sx), ARM_LENGTHS, gdir(ARM_POLE, sx), foot_dir=gdir(FIST_DIR, sx))
        legs[side] = C.Limb([f"Leg_{side}_Upper", f"Leg_{side}_Lower", f"Leg_{side}_Foot"], T.gp(HIP, sx),
                            T.gp(HIND_FOOT, sx), LEG_LENGTHS, gdir(LEG_POLE, sx), foot_dir=gdir(TOE_DIR, sx))
    return arms, legs


ARMS, LEGS = make_limbs()


def arm_pts(sx):
    """Shoulder, elbow, wrist, knuckle contact of one arm (rest)."""
    return ARMS["L" if sx < 0 else "R"].rest_points


def leg_pts(sx):
    return LEGS["L" if sx < 0 else "R"].rest_points


def legs(s=SC):
    """Compatibility for older callers: {(side, i): Limb} of the ground limbs (arms 1, legs 2)."""
    out = {}
    for side, _ in SIDES:
        out[(side, 1)] = ARMS[side]
        out[(side, 2)] = LEGS[side]
    return out


# ------------------------------------------------------------------------------------------------------------
# Surface styles
# ------------------------------------------------------------------------------------------------------------

def _hash3(c, seed):
    """Per-cell pseudo-random offsets in [0, 1)^3 for integer cell coordinates (vectorised)."""
    a = np.sin(c @ np.array((127.1, 311.7, 74.7)) + seed * 1.37) * 43758.5453
    b = np.sin(c @ np.array((269.5, 183.3, 246.1)) + seed * 2.11) * 43758.5453
    d = np.sin(c @ np.array((113.5, 271.9, 124.6)) + seed * 3.03) * 43758.5453
    return np.c_[a - np.floor(a), b - np.floor(b), d - np.floor(d)]


def worley_edge(p, size, seed, jitter=0.9):
    """World-space Voronoi on a jittered grid: (half the F2-F1 gap in metres, cell id hash). One continuous field over
    the whole body, so a crack runs on across part boundaries instead of following each ellipsoid."""
    q = p / size
    base = np.floor(q)
    d1 = np.full(len(p), np.inf)
    d2 = np.full(len(p), np.inf)
    cid = np.zeros(len(p))
    for ox in (-1, 0, 1):
        for oy in (-1, 0, 1):
            for oz in (-1, 0, 1):
                cell = base + np.array((ox, oy, oz), float)
                h = _hash3(cell, seed)
                pt = cell + 0.5 + (h - 0.5) * jitter
                d = np.linalg.norm(q - pt, axis=1)
                closer = d < d1
                d2 = np.where(closer, d1, np.minimum(d2, d))
                cid = np.where(closer, h[:, 0], cid)
                d1 = np.where(closer, d, d1)
    return (d2 - d1) * 0.5 * size, cid


def hide(pal, k, seed=0, crack=1.0, rime=0.55, glow=0.38, wrinkle=0.6, grain=1.0, plate=0.0):
    """Cold hide and carapace (all body parts). `plate` 0 = the hide: leathery folds, pores and dark irregular
    markings that run across the body (an alien pattern, not stone). `plate` 1 = carapace: darker, bluer chitin with a
    pale frosted rim. Cracks are rare, irregular fractures (a warped world-space Voronoi whose borders mostly stay shut,
    varying in width, a few hairlines branching off), denser on the carapace than on the hide; only the widest
    stretches show a thin cyan core (kept at ~0.4, never clipping). Rime gathers in patches on the up faces."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        warp = np.c_[C.value_noise(p, 1.3, seed + 1), C.value_noise(p, 1.3, seed + 2), C.value_noise(p, 1.3, seed + 3)]
        e, cid = worley_edge(p + warp * 0.3, 0.75, seed)
        width = 0.004 + 0.024 * C.smoothstep(0.0, 0.8, C.value_noise(p, 1.1, seed + 4))
        open_ = C.value_noise(p, 1.4, seed + 5) + (cid - 0.5) * 0.5 + 0.35 * plate
        keep = C.smoothstep(0.12, 0.42, open_)
        big = (1 - C.smoothstep(width * 0.45, width, e)) * keep * crack
        core = (1 - C.smoothstep(0.0, width * 0.35, e)) * keep * C.smoothstep(0.016, 0.026, width) * crack
        e2, _ = worley_edge(p + warp * 0.12, 0.24, seed + 9)
        hair = (1 - C.smoothstep(0.0, 0.004, e2)) * C.smoothstep(0.35, 0.7, open_) * crack * 0.6
        hair *= 1 - big
        fold = 1 - C.smoothstep(0.0, 0.16, np.abs(C.value_noise(p * np.array((1.0, 1.0, 2.2)), 2.6, seed + 7)))
        fold *= wrinkle * (1 - plate)
        pore = C.smoothstep(0.5, 0.8, C.value_noise(p, 22.0, seed + 13)) * (1 - plate)
        # Dark markings: broken bands running across the body (along x), warped, wider on the back.
        band = np.sin(p[:, 1] * 3.4 + 1.6 * C.value_noise(p, 0.9, 77) + 0.8 * np.abs(p[:, 0]))
        mark = C.smoothstep(0.55, 0.85, band) * C.smoothstep(-0.2, 0.3, C.value_noise(p, 1.2, 78)) * (1 - plate)
        skin = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        shell = C.chitin_gradient(n, PLATE_TOP, PLATE_SIDE, pal.belly)
        col = C.lerp3(skin, shell, plate)
        col = col * (1 + 0.14 * C.value_noise(p, 1.8, seed + 8))[:, None] * (1 + 0.06 * C.value_noise(p, 9.0, seed + 10))[:, None]
        col = C.lerp3(col, MARK, mark * 0.6)
        col = C.lerp3(col, pal.seam, np.clip(fold * 0.4 + pore * 0.25 + hair * 0.6 + big * 0.9, 0, 1))
        col = C.lerp3(col, pal.glow * 0.55, core * 0.7)
        up = C.smoothstep(0.35, 0.9, n[:, 2]) * C.smoothstep(-0.15, 0.45, C.value_noise(p, 2.4, seed + 11)) * rime
        up *= 1 - big
        col = C.lerp3(col, FROST, up * 0.5)
        if plate:
            rim = C.smoothstep(0.6, 0.95, np.abs(C.local_points(part, p)).max(axis=1)) * plate
            col = C.lerp3(col, FROST, rim * 0.45)
        s.albedo[idx] = col
        s.emit[idx] = GLOW[None, :] * np.clip(core * glow, 0, 1)[:, None]
        s.disp[idx] = (-0.0075 * k * big - 0.0022 * k * hair - 0.003 * k * fold - 0.0008 * k * pore
                       + 0.0012 * k * grain * C.value_noise(p, 14.0, seed + 12) + 0.001 * k * up)
        s.rough[idx] = np.clip(0.78 - 0.2 * plate + 0.1 * up - 0.3 * core, 0.2, 0.95)
        H.family(s, idx, rock=0.5 - 0.3 * plate, chitin=0.2 + 0.7 * plate, membrane=0.3 * (1 - plate),
                 flow=p[:, 2] + 0.2 * C.value_noise(p, 1.0, seed))
    return style


def claw(pal, k, seed=0):
    """Mandible plates, toe claws, brow horns: dark old bone with a pale worn tip and growth rings."""
    def style(s, idx, part):
        p = s.p[idx]
        t, radius, dist = C.tube_param(part, p)
        rings = 1 - C.smoothstep(0.0, 0.2, np.abs(np.sin(t * 18 + 0.5 * C.value_noise(p, 4.0, seed))))
        col = C.lerp3(pal.dark * 1.4, pal.bone * 0.8, C.smoothstep(0.45, 1.0, t))
        col = C.lerp3(col, pal.seam, rings * 0.35)
        col = col * C.chitin_gradient(s.n[idx], np.ones(3), np.ones(3) * 0.75, np.ones(3) * 0.45)
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = -0.002 * k * rings
        s.rough[idx] = 0.5 - 0.15 * t
        H.family(s, idx, chitin=1.0, flow=t * C.axis_length(part))
    return style


def vent(pal, glow=0.5):
    """The glowing throat of a frost vent: cyan in the middle, dark at the rim (0.5 max: never clips)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        r = np.linalg.norm(L[:, :2] * np.array((1.0, 0.6)), axis=1)
        mid = 1 - C.smoothstep(0.25, 0.95, r)
        s.albedo[idx] = C.lerp3(pal.seam, pal.glow * 0.6, mid)
        s.emit[idx] = GLOW[None, :] * (mid * glow)[:, None]
        s.rough[idx] = 0.35
        s.disp[idx] = 0.0
        H.family(s, idx, membrane=1.0, flow=L[:, 1])
    return style


def ice(seed, **kw):
    kw.update(ICE_LOOK)
    return IK.frost_ice(ICE_K, seed=seed, **kw)


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def along(a, b):
    """Rotation whose local z runs from a to b (ellipsoids stretched along a limb)."""
    return T.frame_from(Vector(b) - Vector(a))


def wedge(v):
    """The head: a low wedge, wide at the back, narrowing and flattening to the front, flat on top with a keel."""
    x, y, z = v.x, v.y, v.z
    f = max(0.0, -y)                     # local -y is forward
    x *= 1.0 - 0.42 * f
    z *= 1.0 - 0.3 * f
    if z > 0:
        z *= 0.72
        z += 0.14 * max(0.0, 1.0 - abs(x) * 3.5) * (0.5 + 0.5 * f)
    return Vector((x, y, z))


def body_parts():
    b = C.PartBuilder()
    k = K
    # The trunk: pelvis, belly, the great hump behind the shoulders, the shoulder girdle and the trapezius masses
    # that tie the hump to the arms (the creature's one dominant mass, seen from above).
    b.ellipsoid("Pelvis", G(0, 2.05, -1.18), (0.8, 0.78, 0.64), T.rot(pitch=-18), bone="Hips", segments=(40, 20),
                style=hide(PAL, k, seed=1))
    b.ellipsoid("Belly", G(0, 2.2, -0.45), (0.92, 0.84, 0.72), bone="Spine", segments=(40, 20), style=hide(PAL, k, seed=2))
    b.parts[-1].chain = ["Hips", "Spine"]
    b.ellipsoid("Hump", G(0, 3.0, -0.12), (1.22, 1.18, 0.98), T.rot(pitch=-8), bone="Spine", segments=(48, 24),
                style=hide(PAL, k, seed=3))
    b.parts[-1].chain = ["Spine", "Chest"]
    b.ellipsoid("Girdle", G(0, 2.72, 0.66), (1.28, 0.72, 0.86), bone="Chest", segments=(44, 22), style=hide(PAL, k, seed=4))
    for side, sx in SIDES:
        b.ellipsoid("Trap", G(0.82 * sx, 3.3, 0.42), (0.66, 0.78, 0.48), T.rot(roll=-22 * sx, pitch=-10), bone="Chest",
                    segments=(32, 16), style=hide(PAL, k, seed=5))
        # Flank ribs under the hump: three heavy folds, so the side view is not one smooth balloon.
        for i, (f, u) in enumerate(((-0.95, 2.35), (-0.45, 2.45), (0.05, 2.5))):
            b.ellipsoid("FlankRib", G(1.0 * sx, u, f), (0.2, 0.26, 0.55), T.rot(roll=-28 * sx, pitch=8 + 6 * i),
                        bone="Spine", segments=(20, 10), style=hide(PAL, k, seed=6 + i, plate=0.5))
    # Frost vents along the upper flanks of the hump: three per side, raised lips round a glowing throat. They
    # face up and out, so the game camera sees them as three cyan slits on each side of the crest.
    for side, sx in SIDES:
        for i, (f, u, x) in enumerate(((-0.62, 3.42, 0.86), (-0.12, 3.58, 0.82), (0.36, 3.52, 0.8))):
            c = G(x * sx, u, f)
            normal = (c - G(0, 3.0, -0.12)).normalized()
            rot_m = normal.to_track_quat("Z", "Y").to_matrix()
            b.ellipsoid("VentThroat", c - normal * 0.02, (0.09, 0.28, 0.06), rot_m, bone="Spine", segments=(16, 8),
                        style=vent(PAL, 0.5))
            for lip in (-1, 1):
                b.ellipsoid("VentLip", c + rot_m @ Vector((0.14 * lip, 0, 0.02)), (0.08, 0.32, 0.09), rot_m,
                            bone="Spine", segments=(16, 8), style=hide(PAL, k, seed=20 + i, plate=0.8, crack=0.0))
    # Neck (short, thick, slung down) and the wedge head, pushed out in front of the shoulders.
    b.tube("Neck", [G(0, 2.95, 0.85), G(0, 2.82, 1.25), G(0, 2.68, 1.62)], [0.64, 0.56, 0.5], sides=16,
           chain=["Neck"], style=hide(PAL, k, seed=30))
    b.ellipsoid("Skull", G(0, 2.58, 1.98), (0.74, 0.82, 0.46), bone="Head", segments=(48, 24), deform=wedge,
                style=hide(PAL, k, seed=31, plate=0.6, wrinkle=0.2))
    b.ellipsoid("JawMass", G(0, 2.28, 2.08), (0.5, 0.56, 0.22), T.rot(pitch=10), bone="Jaw", segments=(32, 16),
                style=hide(PAL, k, seed=32, plate=0.4))
    for side, sx in SIDES:
        # Swept-back brow ridges: the wedge reads as a head with a top and a front.
        b.shard("Brow", G(0.36 * sx, 2.86, 2.32), G(0.28 * sx, 0.35, -1.0), 0.15, 0.95, sides=6, flute=0.2,
                bone="Head", style=claw(PAL, k, seed=33))
    # Three amber eyes in a triangle on the front slope of the wedge (the centre one higher and bigger).
    for i, (x, u, f, r) in enumerate(((0.0, 2.9, 2.42, 0.095), (-0.3, 2.76, 2.44, 0.08), (0.3, 2.76, 2.44, 0.08))):
        b.ellipsoid("Eye", G(x, u, f), (r, r * 0.8, r), T.rot(pitch=-25), bone="Head", segments=(14, 8),
                    style=H.eye(PAL, k * 0.3, 0.55))
    # The maw: a cyan slit under the front of the wedge, framed by four mandible plates (upper pair on the head,
    # lower pair on the jaw), all pointing forward and in.
    b.ellipsoid("Maw", G(0, 2.34, 2.58), (0.28, 0.08, 0.1), bone="Jaw", segments=(18, 10), style=vent(PAL, 0.6))
    for side, sx in SIDES:
        b.shard("Mandible", G(0.3 * sx, 2.46, 2.46), G(-0.25 * sx, -0.35, 1.0), 0.11, 0.5, sides=6, flute=0.2,
                bone="Head", style=claw(PAL, k, seed=34))
        b.shard("Mandible", G(0.32 * sx, 2.2, 2.38), G(-0.3 * sx, 0.1, 1.0), 0.1, 0.48, sides=6, flute=0.2,
                bone="Jaw", style=claw(PAL, k, seed=35))
    # Carapace bands over the hump and the back: five shingled plates of hard hide across the spine (the alien,
    # insect-like read; each band tucks under the one in front of it), so the hump is not one smooth balloon.
    for i, (f, u, ry, pitch) in enumerate(((0.62, 3.55, 0.36, 28), (0.12, 3.86, 0.4, 8), (-0.42, 3.86, 0.4, -10),
                                            (-0.95, 3.58, 0.38, -28), (-1.42, 3.12, 0.34, -46))):
        b.ellipsoid("Carapace", G(0, u, f), (1.18 - 0.06 * abs(i - 2), ry, 0.3), T.rot(pitch=pitch), bone="Spine",
                    segments=(36, 18), deform=C.chiselled(14, 70 + i, 0.06, squash=(0.7, 1.0)),
                    style=hide(PAL, k, seed=70 + i, plate=0.85, wrinkle=0.0))
        b.parts[-1].chain = (["Chest"] if i == 0 else ["Spine"] if i < 3 else ["Spine", "Hips"])
    # Arms: a heavy shoulder ball, a shorter upper arm with a bicep mass, an elbow ball and the dominant forearm
    # that flares towards the wrist, then the fist (the ice maul round it is in ice_parts).
    for side, sx in SIDES:
        sh, el, wr, kn = arm_pts(sx)
        up, fo, fi = (f"Arm_{side}_{n}" for n in ("Upper", "Fore", "Fist"))
        b.ellipsoid("Shoulder", sh, (0.64, 0.62, 0.6), bone=up, segments=(32, 16), style=hide(PAL, k, seed=40))
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5), el], [0.48, 0.4, 0.36], sides=16, chain=[up], style=hide(PAL, k, seed=41))
        b.ellipsoid("Bicep", sh.lerp(el, 0.45) + G(-0.12 * sx, 0, 0.18), (0.32, 0.32, 0.52), along(sh, el), bone=up,
                    segments=(24, 12), style=hide(PAL, k, seed=42))
        b.ellipsoid("Elbow", el, (0.38, 0.38, 0.38), bone=fo, segments=(24, 12), style=hide(PAL, k, seed=43))
        b.parts[-1].chain = [up, fo]
        b.tube("Forearm", [el, el.lerp(wr, 0.45), el.lerp(wr, 0.8), wr], [0.42, 0.64, 0.72, 0.56], sides=18,
               chain=[fo], style=hide(PAL, k, seed=44))
        b.ellipsoid("ForeMass", el.lerp(wr, 0.5) + G(0.14 * sx, 0.02, -0.08), (0.6, 0.56, 0.82), along(el, wr), bone=fo,
                    segments=(28, 14), style=hide(PAL, k, seed=45))
        b.ellipsoid("Fist", wr.lerp(kn, 0.5) + G(0, 0.02, 0.06), (0.5, 0.48, 0.4), along(wr, kn), bone=fi,
                    segments=(28, 14), style=hide(PAL, k, seed=46, plate=0.5))
    # Hind legs: big thigh, knee ball, short shin, a splayed foot with two forward claws.
    for side, sx in SIDES:
        hp, kn, an, toe = leg_pts(sx)
        up, lo, ft = (f"Leg_{side}_{n}" for n in ("Upper", "Lower", "Foot"))
        b.ellipsoid("Thigh", hp.lerp(kn, 0.45) + G(0.08 * sx, 0, 0), (0.5, 0.5, 0.72), along(hp, kn), bone=up,
                    segments=(28, 14), style=hide(PAL, k, seed=50))
        b.ellipsoid("Knee", kn, (0.4, 0.4, 0.4), bone=lo, segments=(20, 10), style=hide(PAL, k, seed=51))
        b.parts[-1].chain = [up, lo]
        b.tube("Shin", [kn, kn.lerp(an, 0.5), an], [0.38, 0.32, 0.3], sides=14, chain=[lo], style=hide(PAL, k, seed=52))
        b.ellipsoid("Foot", an.lerp(toe, 0.5) + G(0, 0.06, 0), (0.36, 0.5, 0.2), along(an, toe), bone=ft,
                    segments=(24, 12), style=hide(PAL, k, seed=53))
        for j, dx in enumerate((-0.15, 0.15)):
            b.shard("Claw", toe + G(dx * sx, 0.08, -0.08), G(dx * sx * 1.5, -0.35, 1.0), 0.08, 0.36, sides=6,
                    bone=ft, style=claw(PAL, k, seed=54 + j))
    return b


def ice_parts():
    b = C.PartBuilder()
    for side, sx in SIDES:
        sh, el, wr, kn = arm_pts(sx)
        fo, fi = f"Arm_{side}_Fore", f"Arm_{side}_Fist"
        d = (kn - wr).normalized()
        fwd = G(0, 0, 1)
        # The maul: a hewn ice block grown round the fist, flat where it meets the ground, with three knuckle
        # spikes driving forward and down and two spurs off the back of the wrist.
        IK.chunk(b, "Maul", wr.lerp(kn, 0.5) + fwd * 0.3 + G(0.06 * sx, 0.12, 0), (0.76, 0.72, 0.58),
                 along(wr, kn), fi, 600, count=22, depth=0.2, squash=(0.5, 1.0), mirror=sx < 0, k=ICE_K,
                 **ICE_LOOK)
        IK.chunk(b, "Maul", wr + fwd * 0.05 + G(0.1 * sx, 0.2, 0), (0.6, 0.5, 0.5), along(wr, kn), fi, 601, count=16,
                 depth=0.2, mirror=sx < 0, k=ICE_K, **ICE_LOOK)
        for j, (dx, du) in enumerate(((-0.28, 0.1), (0.0, 0.22), (0.28, 0.1))):
            base = wr.lerp(kn, 0.55) + fwd * 0.72 + G(dx * sx, du + 0.1, 0)
            b.shard("Knuckle", base, G(dx * sx * 0.6, -0.25 + du, 1.0), 0.15, 0.6 + 0.1 * (j == 1), sides=6, flute=0.25,
                    twist=0.4 * sx, bone=fi, style=ice(610 + j))
        for j, (dx, ln) in enumerate(((0.25, 0.55), (-0.1, 0.4))):
            b.shard("WristSpur", wr + G(dx * sx, 0.12, -0.25), G(dx * sx, 0.7, -1.0), 0.1, ln, sides=6, flute=0.3,
                    bone=fi, style=ice(620 + j))
        # Ice growing out of the elbow (backwards, the olecranon) and a low ridge along the outer forearm (it stays
        # when the gauntlet armour bursts off).
        for j, (dd, ln, r) in enumerate((((0.3, 0.2, -1.0), 0.6, 0.12), ((0.8, 0.5, -0.6), 0.42, 0.09))):
            b.shard("ElbowSpur", el + G(0.1 * sx, 0.05, -0.15), G(dd[0] * sx, dd[1], dd[2]), r, ln, sides=6,
                    flute=0.3, twist=0.5 * sx, bone=fo, style=ice(630 + j))
        for j, t in enumerate((0.3, 0.52, 0.74)):
            c = el.lerp(wr, t) + G(0.5 * sx, 0.05, -0.12)
            b.shard("ForeRidge", c, G(0.9 * sx, 0.3, -0.3), 0.09, 0.3 - 0.04 * j, sides=6, flute=0.3, bone=fo,
                    style=ice(640 + j))
        # Crystals growing round the vents (between the vent lips and the crest).
        for j, (f, u, x, ln) in enumerate(((-0.95, 3.32, 0.95, 0.42), (0.62, 3.4, 0.98, 0.36), (-0.36, 3.72, 0.58, 0.32))):
            c = G(x * sx, u, f)
            n = (c - G(0, 2.9, -0.12)).normalized()
            b.shard("VentIce", c, n + G(0, 0.3, -0.2), 0.1, ln, sides=6, flute=0.3, twist=0.6 * sx, bone="Spine",
                    style=ice(650 + j))
        # Shoulder-blade growth under the pauldron edge and knee spurs on the hind legs.
        b.shard("BladeIce", G(1.05 * sx, 3.62, 0.1), G(0.6 * sx, 0.8, -0.6), 0.11, 0.5, sides=6, flute=0.3, bone="Chest",
                style=ice(660))
        hp, knee, an, toe = leg_pts(sx)
        b.shard("KneeSpur", knee + G(0.1 * sx, 0.1, 0.15), G(0.4 * sx, 0.6, 0.8), 0.09, 0.42, sides=6, flute=0.3,
                bone=f"Leg_{side}_Lower", style=ice(670))
        # Brow crystals: two short prisms raking back off the wedge.
        b.shard("BrowIce", G(0.22 * sx, 2.96, 1.95), G(0.3 * sx, 0.7, -1.0), 0.08, 0.48, sides=6, flute=0.3,
                bone="Head", style=ice(680))
    # Rump: a low cluster of ice where the hind legs meet the pelvis (it reads from behind).
    b.shard("RumpIce", G(0, 2.55, -1.85), G(0, 0.6, -1.0), 0.12, 0.5, sides=6, flute=0.3, twist=0.5, bone="Hips",
            style=ice(690))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.5, 0), None, False),
           ("Hips", *HIPS, "Root", True),
           ("Spine", *SPINE, "Hips", True),
           ("Chest", *CHEST, "Spine", True),
           ("Neck", *NECK, "Chest", True),
           ("Head", *HEAD, "Neck", True),
           ("Jaw", *JAW, "Head", True)]
    for side, _ in SIDES:
        out += ARMS[side].rest_bones("Chest")
        out += LEGS[side].rest_bones("Hips")
    return out


# ------------------------------------------------------------------------------------------------------------
# Posing
# ------------------------------------------------------------------------------------------------------------

def specs(off=None, pitch=0.0, roll=0.0, yaw=0.0, spine=0.0, chest=0.0, chest_yaw=0.0, chest_roll=0.0, neck=0.0,
          head=0.0, head_yaw=0.0, jaw=0.0):
    """Hinge specs (radians). `pitch` > 0 tips the body nose down about the hips; `spine`/`chest` > 0 curl the hump
    and the shoulders forward and down; `neck`/`head` > 0 drop the head; `jaw` > 0 opens the maw."""
    return {"Root": {"rot": []},
            "Hips": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": off if off is not None else Vector()},
            "Spine": {"rot": [(X, spine)]},
            "Chest": {"rot": [(Z, chest_yaw), (Yv, chest_roll), (X, chest)]},
            "Neck": {"rot": [(Z, head_yaw * 0.5), (X, neck)]},
            "Head": {"rot": [(Z, head_yaw * 0.5), (X, head)]},
            "Jaw": {"rot": [(X, jaw)]}}


def place(poser, sp, feet, fists=None, poles=None):
    """Poses the four limbs by IK: arms from the Chest, legs from the Hips. `feet`: {("arm"|"leg", side): world
    target}; `fists`: {side: world direction wrist -> knuckles} (default: the knuckle-walk direction carried by the
    chest); `poles`: {("arm"|"leg", side): pole direction}."""
    pose = poser.solve(sp)
    chest = pose["Chest"] @ poser.rest["Chest"].inverted()
    hips = pose["Hips"] @ poser.rest["Hips"].inverted()
    for side, _ in SIDES:
        a = ARMS[side]
        rest_dir = a.foot_dir
        if fists and side in fists:
            a.foot_dir = Vector(fists[side]).normalized()
        pole = (poles or {}).get(("arm", side), chest.to_3x3() @ a.pole)
        sp.update(a.pose(poser, chest @ a.root, feet[("arm", side)], pole))
        a.foot_dir = rest_dir
        lg = LEGS[side]
        pole = (poles or {}).get(("leg", side), hips.to_3x3() @ lg.pole)
        sp.update(lg.pose(poser, hips @ lg.root, feet[("leg", side)], pole))
    return sp


def planted(travel=0.0):
    out = {}
    for side, _ in SIDES:
        out[("arm", side)] = ARMS[side].foot + G(0, 0, travel)
        out[("leg", side)] = LEGS[side].foot + G(0, 0, travel)
    return out


def arc(a, b, t, lift):
    """A foot moving from a to b along an eased arc `lift` high."""
    e = (1 - math.cos(math.pi * min(max(t, 0.0), 1.0))) * 0.5
    return Vector(a).lerp(Vector(b), e) + G(0, lift * math.sin(math.pi * min(max(t, 0.0), 1.0)), 0)


def overhead(sx, up=5.0, fwd=0.9, spread=0.45):
    """Fist target when both fists are curled up over the head (knuckles up)."""
    return G(spread * sx, up, fwd)


# Gait: a lateral-sequence knuckle walk (left hind, left fist, right hind, right fist), heavy and deliberate.
WALK_FRAMES, RAGE_FRAMES = 64, 40
STRIDE, LIFT, STANCE = 1.0, 0.3, 0.7
ARM_LIFT, LEG_LIFT = 0.42, 0.22
PHASES = {("leg", "L"): 0.0, ("arm", "L"): 0.75, ("leg", "R"): 0.5, ("arm", "R"): 0.25}
RAGE_STRIDE = 1.3


def gait(t, stride=STRIDE, lift_k=1.0, travel=0.0, swing_out=0.18):
    out = {}
    for (kind, side), ph in PHASES.items():
        sx = -1 if side == "L" else 1
        g = (t + ph) % 1.0
        fw, upv = C.step_curve(g, STANCE, stride, (ARM_LIFT if kind == "arm" else LEG_LIFT) * lift_k)
        base = (ARMS if kind == "arm" else LEGS)[side].foot
        out[(kind, side)] = base + G(0, upv, fw + travel)
        if kind == "arm" and g >= STANCE:
            s = (g - STANCE) / (1 - STANCE)
            out[(kind, side)] += G(swing_out * sx * math.sin(math.pi * s), 0, 0)   # the fist swings round, not through
    return out


def walk_spec(poser, rage=False):
    frames = RAGE_FRAMES if rage else WALK_FRAMES
    stride = STRIDE * (RAGE_STRIDE if rage else 1.0)
    h = 1.0 if rage else 0.0

    def spec(t, f):
        w = 2 * math.pi * t
        # The body drops onto each landing fist (t 0.25 and 0.75: twice per cycle) and is pushed up by the hind legs.
        bob = -0.06 * math.cos(2 * (w - 2 * math.pi * 0.3)) * (1 + 0.5 * h)
        sp = specs(off=G(0.05 * math.sin(w - 0.4), bob - 0.18 * h, 0.04 * math.sin(2 * w) - 0.1 * h),
                   pitch=R(6) * h + R(1.5) * math.sin(2 * w + 0.6),
                   roll=R(3 + 2 * h) * math.sin(w - 0.4), yaw=R(3) * math.cos(w - 2 * math.pi * 0.85),
                   spine=R(2) * math.sin(2 * w) + R(4) * h,
                   chest=R(4) * h, chest_yaw=R(7 + 4 * h) * math.cos(w - 2 * math.pi * 0.1),
                   chest_roll=-R(2.5) * math.sin(w - 0.4),
                   neck=R(6) * h, head=-R(4) * h + R(1.5) * math.sin(2 * w),
                   head_yaw=-R(5 + 2 * h) * math.cos(w - 2 * math.pi * 0.1), jaw=R(10) * h)
        return place(poser, sp, gait(t, stride, 1.25 if rage else 1.0))
    return frames, spec, True


def walk_rage_spec(poser):
    return walk_spec(poser, rage=True)


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        breath = math.sin(w * 2)
        regrip = K2.pulse(f, 64, 74, 88)
        sp = specs(off=G(0.04 * math.sin(w), 0.03 * breath - 0.02, 0.0), roll=R(2) * math.sin(w),
                   spine=-R(1.6) * breath, chest=R(1.2) * breath, chest_yaw=R(2.5) * math.sin(w + 0.4),
                   neck=R(3) * math.sin(w * 2 + 1.0), head=R(3) * math.sin(w + 2.0),
                   head_yaw=R(22) * math.sin(w + 0.5), jaw=R(4) * max(0.0, math.sin(w * 2)))
        feet = planted()
        # Re-grips the left fist once per loop (lifts the knuckles, grinds them back in).
        feet[("arm", "L")] = feet[("arm", "L")] + G(-0.05, 0.16 * regrip, 0.06 * regrip)
        return place(poser, sp, feet)
    return 120, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(off=G(0, 0.04 * k, -0.12 * k), pitch=-R(5) * k, roll=R(3) * k, spine=-R(3) * k, chest=-R(6) * k,
                   neck=-R(10) * k, head=-R(8) * k, jaw=R(12) * max(k, 0))
        return place(poser, sp, planted())
    return 16, spec, False


PULSE_FRAMES, PULSE_IMPACT = 64, 30
PULSE_FWD = 2.7


def freeze_spec(poser):
    """Rears back on the hind legs and curls both fists together over the head (frames 4-20), holds, then hammers
    them down into the ground side by side PULSE_FWD ahead at PULSE_IMPACT (the frost ring starts there): the body
    follows the blow forward and down, squashes, and settles back onto the knuckles."""
    def spec(t, f):
        rise = K2.ease(3, 19, f) * (1 - K2.ease(24, PULSE_IMPACT, f))
        slam = K2.ease(24, PULSE_IMPACT, f) * (1 - K2.ease(44, 60, f))
        squash = K2.pulse(f, PULSE_IMPACT - 1, PULSE_IMPACT + 3, PULSE_IMPACT + 16)
        shake = math.sin(f * 2.6) * K2.pulse(f, PULSE_IMPACT, PULSE_IMPACT + 3, PULSE_IMPACT + 14)
        sp = specs(off=G(0, 0.25 * rise - 0.3 * squash, -0.35 * rise + 0.3 * slam),
                   pitch=-R(26) * rise + R(9) * slam + R(1.5) * shake, spine=-R(6) * rise + R(6) * slam,
                   chest=-R(10) * rise + R(8) * slam, neck=R(14) * rise - R(4) * slam, head=-R(16) * rise + R(10) * slam,
                   jaw=R(26) * K2.pulse(f, 14, 22, PULSE_IMPACT + 10))
        feet = planted()
        fists = {}
        for side, sx in SIDES:
            rest = feet[("arm", side)]
            top = overhead(sx, up=5.2, fwd=0.55, spread=0.42)
            hit = G(0.62 * sx, 0.02, PULSE_FWD)
            if f <= 20:
                feet[("arm", side)] = arc(rest, top, K2.ease(3, 19, f), 0.4)
                fists[side] = turn(gdir((0.0, -1.0, 0.2)), gdir((-0.4 * sx, 1.0, -0.4)), K2.ease(3, 17, f), FIST_OVER)
            elif f <= PULSE_IMPACT:
                s = K2.ease(24, PULSE_IMPACT, f)
                feet[("arm", side)] = top.lerp(hit, s) + G(0, 0.9 * math.sin(math.pi * s), 0.6 * math.sin(math.pi * s))
                fists[side] = turn(gdir((-0.4 * sx, 1.0, -0.4)), gdir((0.0, -1.0, 0.25)), K2.ease(24, PULSE_IMPACT - 1, f), FIST_OVER)
            else:
                back = K2.ease(44, 62, f)
                feet[("arm", side)] = arc(hit, rest, back, 0.3)
                fists[side] = turn(gdir((0.0, -1.0, 0.25)), gdir(FIST_DIR, sx), back)
        return place(poser, sp, feet, fists)
    return PULSE_FRAMES, spec, False


ENRAGE_FRAMES, ENRAGE_SLAM = 48, 22


def enrage_spec(poser):
    """Rears up on the hind legs with both fists thrown wide and high (to frame 14), roars, hammers the ground with
    both fists at ENRAGE_SLAM, and settles low into the WalkEnraged hunch."""
    def spec(t, f):
        rear = K2.ease(2, 14, f) * (1 - K2.ease(17, ENRAGE_SLAM, f))
        slam = K2.pulse(f, ENRAGE_SLAM - 2, ENRAGE_SLAM + 1, ENRAGE_SLAM + 12)
        hunch = K2.ease(ENRAGE_SLAM, ENRAGE_FRAMES, f)
        shudder = math.sin(f * 2.3) * K2.pulse(f, ENRAGE_SLAM, ENRAGE_SLAM + 4, 40)
        sp = specs(off=G(0, 0.3 * rear - 0.2 * slam - 0.18 * hunch, -0.3 * rear - 0.1 * hunch),
                   pitch=-R(30) * rear + R(6) * slam + R(6) * hunch + R(1.2) * shudder,
                   spine=-R(8) * rear + R(4) * hunch, chest=-R(12) * rear + R(4) * hunch,
                   neck=R(8) * rear + R(6) * hunch, head=-R(22) * rear - R(4) * hunch,
                   jaw=R(34) * K2.pulse(f, 8, 16, 34) + R(10) * hunch)
        feet = planted()
        fists = {}
        for side, sx in SIDES:
            rest = feet[("arm", side)]
            wide = G(2.5 * sx, 4.2, 1.3)
            if f <= 14:
                feet[("arm", side)] = arc(rest, wide, K2.ease(2, 14, f), 0.3)
                fists[side] = gdir(FIST_DIR, sx).lerp(gdir((0.6 * sx, 1.0, 0.2)), K2.ease(2, 14, f))
            elif f <= ENRAGE_SLAM:
                s = K2.ease(16, ENRAGE_SLAM, f)
                feet[("arm", side)] = wide.lerp(rest + G(0.1 * sx, 0, 0.2), s)
                fists[side] = gdir((0.6 * sx, 1.0, 0.2)).lerp(gdir(FIST_DIR, sx), s)
            else:
                feet[("arm", side)] = (rest + G(0.1 * sx, 0, 0.2)).lerp(rest, K2.ease(32, 46, f))
                fists[side] = gdir(FIST_DIR, sx)
        return place(poser, sp, feet, fists)
    return ENRAGE_FRAMES, spec, False


DEATH_FRAMES = 84


def death_spec(poser):
    """Staggers back, the arms buckle outwards (elbows splay), the chest crashes down between the fists, the head
    hits the ground, the hind legs fold and the hulk rolls a little onto its left side."""
    def spec(t, f):
        jerk = K2.pulse(f, 0, 5, 14)
        drop = K2.ease(10, 40, f) ** 1.6
        settle = K2.ease(40, 70, f)
        sp = specs(off=G(-0.25 * settle, -1.15 * drop - 0.12 * settle, -0.2 * jerk + 0.25 * drop),
                   pitch=-R(6) * jerk + R(14) * drop, roll=-R(9) * settle, spine=R(6) * drop,
                   chest=R(10) * drop, neck=R(10) * drop, head=-R(8) * jerk + R(14) * drop + R(4) * settle,
                   jaw=R(22) * K2.pulse(f, 2, 8, 30) + R(12) * settle)
        feet = planted()
        poles = {}
        for side, sx in SIDES:
            feet[("arm", side)] = feet[("arm", side)] + G((0.75 + (0.2 if sx < 0 else 0.0)) * sx * drop, 0.0, 0.35 * drop)
            feet[("leg", side)] = feet[("leg", side)] + G(0.35 * sx * drop, 0.0, -0.4 * drop)
            poles[("arm", side)] = gdir((1.0, 0.25, -0.35), sx)
        return place(poser, sp, feet, poles=poles)
    return DEATH_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "WalkEnraged": walk_rage_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec,
         "FreezePulse": freeze_spec, "Enrage": enrage_spec}
BODY_RIGID = {"Brow", "Mandible", "Claw", "Eye", "VentLip", "VentThroat", "FlankRib"}
ICE_RIGID = {"Maul", "Knuckle", "WristSpur", "ElbowSpur", "ForeRidge", "VentIce", "BladeIce", "KneeSpur", "BrowIce",
             "RumpIce"}
LOD_DROP = {"Neck": "Chest"}
LOD_AIMS = {}


def armour_preview():
    """The ice armour (build_frostwarden_mech) for the blockout: (builder, bones with the deform flag)."""
    import build_frostwarden_mech as MW
    return MW.armour_parts(), [(n, h, t, p, True) for n, h, t, p in MW.armour_bones()]


def blockout(out):
    """Raw parts on the rig (no bakes) with the stage views next to a Gate_G2 and a tower (round32_views)."""
    import round32_views as V
    C.reset_scene()
    armour, armour_bones = armour_preview()
    objs = []
    for i, (b, col, glow) in enumerate(((body_parts(), (0.34, 0.4, 0.48), None), (ice_parts(), (0.3, 0.55, 0.7), (0.4, 0.8, 1.0)),
                                        (armour, (0.25, 0.5, 0.65), (0.4, 0.8, 1.0)))):
        o = b.to_object(f"Warden_Block{i}")
        o.data.materials.append(C.principled(f"Block{i}", col, rough=0.6, emission=glow, strength=0.4 if glow else 0.0))
        objs.append((o, b))
    arm = C.build_armature(NAME + "_Rig", bones() + armour_bones)
    for o, b in objs:
        C.skin(o, arm, b, rigid=BODY_RIGID | ICE_RIGID | {"IcePlate", "IceSpur", "Icicle"})
    import creature_kit3 as K3
    poser = K3.ClipPoser(arm, [])
    actions = K3.key_clips(poser, CLIPS)
    V.stage(arm, [o for o, _ in objs], actions, out, "block",
            [("Idle", 20), ("Walk", 8), ("WalkEnraged", 10), ("FreezePulse", 18), ("FreezePulse", PULSE_IMPACT),
             ("Enrage", 12), ("Death", DEATH_FRAMES)],
            strips=[("Walk", range(0, WALK_FRAMES, 8)), ("FreezePulse", range(0, PULSE_FRAMES + 1, 8)),
                    ("Enrage", range(0, ENRAGE_FRAMES + 1, 6)), ("Death", range(0, DEATH_FRAMES + 1, 12))])


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        blockout(out)
        return
    target, dist = (0, -0.3, 2.2), 15.0
    C.reset_scene()
    mats = [T.Mat(body_parts(), 17500, ("rock", "chitin", "membrane"), voxel=0.011, ao=0.4, k=K, emission=2.4,
                  specular=0.3, extra=H.weathering(K, scratches=0.4, dust=0.0, seed=171, markings=PAL.dark,
                                                   marking_amount=0.15),
                  rigid=BODY_RIGID, blend=2, lod_share=0.72),
            T.Mat(ice_parts(), 6500, ("crystal", "rock"), suffix="Ice", voxel=0.008, ao=0.2, extrusion=0.04, ray=0.12,
                  k=ICE_K, emission=2.0, specular=0.5, rigid=ICE_RIGID, blend=0, smooth=2, lod_share=0.28)]
    report = {"walk_distance_per_loop": round(STRIDE / STANCE, 3),
              "walk_enraged_distance_per_loop": round(STRIDE * RAGE_STRIDE / STANCE, 3),
              "pivot": "ground, under the middle of the body (fists 1.95 ahead, hind feet 1.25 behind)",
              "freeze_pulse": {"impact_frame": PULSE_IMPACT, "frost_ring_origin": f"between the fists, {PULSE_FWD} ahead",
                               "fists_overhead_frames": "14-24"},
              "enrage": {"frames": ENRAGE_FRAMES, "slam_frame": ENRAGE_SLAM,
                         "then": "loop WalkEnraged (lower, 1.3x stride) at a higher speed"},
              "seam_colour": PAL.glow.tolist(), "eye_colour": EYE,
              "round32": {"design": "Rime Colossus (concept A): hunched knuckle-walker, ice-maul fists, wedge head "
                                    "with three eyes and a frost maw, hump with vents and the rage-icicle crest",
                          "bones": "Root, Hips, Spine, Chest, Neck, Head, Jaw, Arm_<L|R>_Upper/Fore/Fist, "
                                   "Leg_<L|R>_Upper/Lower/Foot (+ armour, icicles, sockets and IK markers from "
                                   "build_frostwarden_mech.py)",
                          "removed": "Halo, Abdomen_1/2, Arm_*_Blade, Leg_*1/2 (round-16/30 rig)"},
              "footfalls": "Walk: left hind 0, left fist 16, right hind 32, right fist 48 (frames)"}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
        scale_bones=[], report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("warden", arm, mesh, actions, lod, out, assets, target, dist, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40),
                      ("pulse", "close", "FreezePulse", PULSE_IMPACT), ("rage", "close", "WalkEnraged", 10),
                      ("dead", "close", "Death", DEATH_FRAMES)],
               strips=[("Walk", "side", range(0, WALK_FRAMES, 8)), ("FreezePulse", "close", range(0, PULSE_FRAMES + 1, 8)),
                       ("Enrage", "close", range(0, ENRAGE_FRAMES + 1, 6)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12))],
               macros=[("head", G(0, 2.5, 1.9), 3.6, -25, 14, "Idle", 20),
                       ("fist", G(1.8, 0.6, 1.9), 3.6, -40, 12, "Idle", 20),
                       ("hump", G(0.7, 3.4, -0.2), 4.2, -120, 35, "Idle", 20)])


if __name__ == "__main__":
    main()
