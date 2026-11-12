"""The Sealed One: the continent-6 final boss (The Core), the ancient being under the seal (round 16, see
ArtSource/DESIGN_NOTES.md).

Round 52 (Remco picked concept B of round 49G, "The opening seal": "continue with direction B ... it may be much more
epic, as long as it's clear that there is a head"). Two earlier builds failed because parts were stacked on parts
until nobody could tell what the head was, so this build has ONE head and ONE mouth and nothing passes through either:

- a massive hood of dark sealed stone sits on a hunched bone-stone body; its face IS the mouth: a deep funnel ringed
  by a band of amber-gold seal glyphs, out of which three pale bone petals (two upper, one lower) meet in a pointed
  snout. The petals are the face plates of the old rig: Plate_R / Plate_L (upper right / left) and Jaw (lower). Each
  hinges at its root on the funnel lip and opens outwards, so every clip that parted the face plates now opens the
  maw. At rest they stand a little apart (REST_OPEN): dark slits with teeth and violet light between them.
- inside: rows of ivory teeth rooted in wet gums along both edges of every petal, two inner rings of teeth on the
  funnel wall, a hot violet gullet at the bottom; gold seal glyphs on the petals' inner faces show only when the maw
  opens (sealedone_detail.py builds the teeth, the brow eyes and the claws).
- behind the hood (never in front of it): a broken seal halo, three concentric broken stone rings carved with glyph
  bands (bones Ring_1..3), each carried on three or four bone tines from its own hub on an axle that grows out of the
  back of the hood, so a ring stays attached while it turns and flares.
- two massive planted arms with dark stone shackles and broken chain stubs at the wrists, three-fingered hands;
  dark sealed-stone mantles on the shoulders; a few violet corruption crystals low on the back and shoulders; seven
  broken seal stones round the fissure (bones Seal_1..7) and a ring of heaved rubble.
- the thin praying upper arms are gone (they cluttered the front of the maw); their bones UArm_* stay keyed for
  Unity but deform nothing.

Two materials (1024 + 512 Low each): `SealedOne_Mat` (body, hood, petals, rubble) and `SealedOne_Seal_Mat` (seal
stones, halo rings, violet crystals). Clips: Idle (loop), Rise, Summon, Hit, Death. Also exports SealedOne_LOD1.

Run: blender --background --factory-startup --python Tools/Blender/build_sealedone.py -- [--preview <dir>]
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
import crysium as Y  # noqa: E402
import mouth49g as MO  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "SealedOne"
LOD_NAME = NAME + "_LOD1"
K = 4.5
UNITY_SCALE = 1.0

# Ash-ivory bone-stone body (mid value), bone-white petals (the brightest thing on it), a near-black hood of sealed
# stone, deep violet light (the entity) and amber-gold glyphs (the seal).
PAL = H.Palette(top=(0.68, 0.64, 0.58), side=(0.4, 0.37, 0.36), belly=(0.1, 0.09, 0.11), seam=(0.08, 0.03, 0.1),
                dark=(0.18, 0.15, 0.18), glow=(0.62, 0.26, 1.0), eye=(0.8, 0.5, 1.0), membrane=(0.28, 0.07, 0.4),
                bone=(0.84, 0.8, 0.73))
PETAL = H.Palette(top=(0.93, 0.9, 0.83), side=(0.8, 0.76, 0.68), belly=(0.5, 0.45, 0.43), seam=(0.36, 0.3, 0.28),
                  dark=(0.25, 0.2, 0.2), glow=(0.62, 0.26, 1.0), eye=(0.8, 0.5, 1.0), membrane=(0.3, 0.06, 0.14),
                  bone=(0.97, 0.95, 0.9))
HOOD = H.Palette(top=(0.21, 0.19, 0.22), side=(0.12, 0.11, 0.13), belly=(0.05, 0.045, 0.055), seam=(0.02, 0.015, 0.03),
                 dark=(0.07, 0.06, 0.08), glow=(0.62, 0.26, 1.0), eye=(0.8, 0.5, 1.0), membrane=(0.2, 0.05, 0.25),
                 bone=(0.42, 0.38, 0.37))
STONE = H.Palette(top=(0.3, 0.28, 0.3), side=(0.14, 0.13, 0.15), belly=(0.05, 0.05, 0.06), seam=(0.03, 0.03, 0.04),
                  dark=(0.08, 0.075, 0.09), glow=(1.0, 0.68, 0.26), eye=(1.0, 0.75, 0.35), bone=(0.55, 0.52, 0.5))
HALO = H.Palette(top=(0.58, 0.54, 0.52), side=(0.32, 0.29, 0.3), belly=(0.07, 0.065, 0.075), seam=(0.04, 0.035, 0.045),
                 dark=(0.1, 0.09, 0.11), glow=(1.0, 0.68, 0.26), eye=(1.0, 0.75, 0.35), bone=(0.6, 0.56, 0.52))
RUBBLE = H.Palette(top=(0.24, 0.2, 0.24), side=(0.11, 0.09, 0.12), belly=(0.04, 0.03, 0.05), seam=(0.03, 0.02, 0.04),
                   dark=(0.07, 0.06, 0.08), glow=PAL.glow, eye=PAL.eye, bone=(0.5, 0.46, 0.5))
GOLD = (1.0, 0.7, 0.28)
VIOLET = (0.62, 0.26, 1.0)
Y.PALETTES.setdefault("void", ((np.array((0.12, 0.02, 0.2)), np.array((0.55, 0.22, 0.95)), np.array((0.92, 0.82, 1.0))),
                               (np.array((0.68, 0.3, 1.0)), np.array((0.95, 0.85, 1.0)))))

SIDES = T.SIDES
X, Yv, Z = T.X, T.Yv, T.Z

STONE_R = 2.75
STONE_ANGLES = [180.0, 180 - 51.43, 180 + 51.43, 180 - 102.86, 180 + 102.86, 180 - 154.29, 180 + 154.29]
STONE_SIZE = [(0.7, 0.26, 1.45), (0.62, 0.25, 1.35), (0.62, 0.25, 1.35), (0.6, 0.25, 1.25), (0.6, 0.25, 1.25),
              (0.5, 0.24, 1.0), (0.5, 0.24, 1.0)]
STONE_TILT = 18.0

# ------------------------------------------------------------------------------------------------------------
# The maw frame: everything of the head is laid out round the maw axis D (forward, tipped MAW_PITCH down) through
# MAW_O, the centre of the funnel lip. maw_point(r, phi, z): radius r from the axis at angle phi (0 = up, + to the
# creature's right = +x), z along the axis (0 = the lip plane, + forward).
# ------------------------------------------------------------------------------------------------------------
MAW_O = G(0, 3.95, 0.82)
MAW_PITCH = math.radians(6.0)
D = G(0, -math.sin(MAW_PITCH), math.cos(MAW_PITCH)).normalized()
E_UP = G(0, math.cos(MAW_PITCH), math.sin(MAW_PITCH)).normalized()
E_X = Vector((1.0, 0.0, 0.0))
MAW_ROT = Matrix((E_X, E_UP, D)).transposed()
MAW_FRAME = Matrix.Translation(MAW_O) @ MAW_ROT.to_4x4()


def radial(phi):
    return (E_UP * math.cos(phi) + E_X * math.sin(phi)).normalized()


def maw_point(r, phi, z):
    return MAW_O + D * z + radial(phi) * r


# The three petals: (bone, centre angle). Plate_R is the creature's upper right (+x), Plate_L its mirror, Jaw below.
PETALS = [("Plate_R", math.radians(60.0)), ("Plate_L", math.radians(-60.0)), ("Jaw", math.radians(180.0))]
PETAL_NAMES = [p[0] for p in PETALS]
PETAL_HW = math.radians(57.0)       # half the angular width of a petal at its root (6 degree seams between them)
PETAL_LEN = 1.3                     # root to tip along the axis
ROOT_R = 0.6                        # inner radius of the petals at the root
ROOT_BACK = 0.1                     # how far each petal's root reaches back into the hood wall
HINGE_R = 0.66                      # the hinge line (tangent to the lip) runs at this radius
REST_OPEN = math.radians(4.0)       # the petals' outward turn at rest (the clips only ever open them further)
PETAL_K = 1.0                       # petal opening (radians) per radian of the clips' `plates`
PREVIEW_OPEN = 0.0                  # r52_sealedone.py --quick: extra opening baked into the geometry (radians)

# The hood: one shell of revolution round the maw axis. Outside, a long rounded hood (HOOD_RMAX at HOOD_ZC, back pole
# at HOOD_ZB); inside, the face: a cup (the funnel) from the lip at LIP_Z down to its bottom at FUNNEL_BACK, where the
# violet gullet sits.
FUNNEL_BACK, LIP_Z = -0.42, 0.05
FUNNEL_R = 0.66
HOOD_ZB, HOOD_ZC, HOOD_RMAX = -2.0, -0.85, 1.02
GULLET_Z = -0.38


def hood_ri(z):
    t = min(1.0, max(0.0, (z - FUNNEL_BACK) / (LIP_Z - FUNNEL_BACK)))
    return FUNNEL_R * t ** 0.55


def hood_ro(z, phi):
    """The hood's outer radius: an ellipse of revolution cut by the lip, a little taller on top, lumpy."""
    e = (z - HOOD_ZC) / (HOOD_ZC - HOOD_ZB)
    r = HOOD_RMAX * math.sqrt(max(0.0, 1.0 - e * e))
    r = max(r, FUNNEL_R + 0.05) if z > HOOD_ZC else r
    return r * (1.0 + 0.06 * math.cos(phi) + 0.018 * math.sin(5 * phi + 3.0 * z))


# The halo: an axle out of the back of the hood, three hubs on it, one broken ring per hub on bone tines.
HALO_UP = 0.85
AXLE_Z = (-1.4, -2.62)
# (bone, ring radius, hub position along the axis, arc phase in degrees from up, arcs, half the band's width)
RINGS = [("Ring_1", 1.18, -2.14, 0.0, 3, 0.13), ("Ring_2", 1.5, -2.32, 60.0, 3, 0.15),
         ("Ring_3", 1.84, -2.5, 45.0, 4, 0.17)]
RING_NAMES = [r[0] for r in RINGS]
RING_NORMAL = -D
RING_GROW = 0.5                     # a ring the clips flare past 1.0 grows only half as much (it stays behind the head)

LOWER = {"shoulder": (1.22, 2.72, 0.22), "hand": (1.82, 0.12, 1.86), "lengths": (1.72, 1.9)}
UPPER = {"shoulder": (0.62, 3.05, 0.3), "elbow": (1.05, 2.8, 0.8), "wrist": (0.16, 2.72, 1.08)}
SCALE_BONES = RING_NAMES


def stone_dir(i):
    a = math.radians(STONE_ANGLES[i])
    return G(math.sin(a), 0, math.cos(a))


def stone_base(i):
    return stone_dir(i) * STONE_R


def stone_rot(i):
    """Stands on its base, leaning outwards by STONE_TILT, its broad face towards the centre."""
    d = stone_dir(i)
    yaw = math.atan2(d.x, -d.y)
    return (Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(math.radians(STONE_TILT), 3, "X"))


# ------------------------------------------------------------------------------------------------------------
# Petal geometry
# ------------------------------------------------------------------------------------------------------------

def hinge(phi):
    return maw_point(HINGE_R, phi, 0.0)


def hinge_axis(phi):
    """A positive turn about this axis (through hinge(phi)) swings the petal's tip away from the maw axis."""
    return D.cross(radial(phi)).normalized()


def petal_matrix(phi, extra=0.0):
    """World from the closed petal: the turn about the hinge by REST_OPEN (+ `extra`, + PREVIEW_OPEN)."""
    h = hinge(phi)
    rot = Matrix.Rotation(REST_OPEN + PREVIEW_OPEN + extra, 4, hinge_axis(phi))
    return Matrix.Translation(h) @ rot @ Matrix.Translation(-h)


def petal_s(z):
    return min(1.0, max(0.0, z / PETAL_LEN))


def petal_r(s):
    """Inner radius of a closed petal at s (0 root .. 1 tip): a swelling bud drawn to a point."""
    return ROOT_R * (1.0 + 0.3 * math.sin(math.pi * s * 0.9)) * (1.0 - s ** 1.8) + 0.035 * s


def petal_hw(s):
    return PETAL_HW * (1.0 - 0.6 * s ** 1.6)


def petal_thick(s, w):
    """Thickness at s across w (-1..1): thick at the root, a raised keel down the middle, thinner at the edges."""
    base = 0.13 * (1.0 - 0.5 * s) * (0.62 + 0.38 * (1.0 - w * w))
    keel = 0.075 * max(0.0, 1.0 - abs(w) * 2.4) ** 1.3 * math.sin(math.pi * min(1.0, s * 1.15))
    return base + keel


def petal_closed(phi, u, v, outer):
    """A point of the closed petal at (u across, v along) on its inner or outer surface."""
    z = -ROOT_BACK + v * (PETAL_LEN + ROOT_BACK)
    s = petal_s(z)
    w = 2.0 * u - 1.0
    a = phi + w * petal_hw(s)
    r = petal_r(s) + (petal_thick(s, w) if outer else 0.0)
    return maw_point(r, a, z)


def petal_inner_point(phi, s, w, extra=0.0):
    """World point on the inner face of a petal (rest pose + `extra`) at s along, w across."""
    z = s * PETAL_LEN
    return petal_matrix(phi, extra) @ maw_point(petal_r(s), phi + w * petal_hw(s), z)


def petal_dirs(phi, s, w, extra=0.0):
    """(inward unit, along unit) on a petal's inner face at (s, w), rest pose."""
    rot = petal_matrix(phi, extra).to_3x3()
    a = phi + w * petal_hw(s)
    return (rot @ -radial(a)).normalized(), (rot @ D).normalized()


def petal_frame(phi):
    """World -> the closed maw frame of a petal (its style reads radius and depth there)."""
    return (petal_matrix(phi) @ MAW_FRAME).inverted()


# ------------------------------------------------------------------------------------------------------------
# Styles
# ------------------------------------------------------------------------------------------------------------

def _local(part, s, idx):
    """Points and normals of a slab part in its own frame (the closed maw frame)."""
    L = C.local_points(part, s.p[idx])
    rot = part.frame.to_3x3()
    n = np.asarray(s.n[idx]) @ np.array(rot).T
    return L, n


def petal_style(seed=0):
    """Outer face: bone-white, worn, with dark growth bands across it and hairline cracks (pale against the dark hood).
    Inner face: wet wine-dark flesh in transverse folds, a violet blush deep towards the root (the throat's light)."""
    pal = PETAL

    def style(s, idx, part):
        L, nl = _local(part, s, idx)
        p = s.p[idx]
        rad = np.hypot(L[:, 0], L[:, 1])
        rdir = L[:, :2] / np.maximum(rad[:, None], 1e-6)
        out = np.einsum("ij,ij->i", nl[:, :2], rdir)
        outer = C.smoothstep(-0.25, 0.25, out)
        z = L[:, 2]
        col = C.chitin_gradient(s.n[idx], pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.1, 30 / K, seed)
        band = 1 - C.smoothstep(0.0, 0.22, np.abs(np.sin(z * 15.0 + 1.2 * C.value_noise(p, 3.0 / K, seed + 1))))
        crack = (1 - C.smoothstep(0.0, 0.035, np.abs(C.value_noise(p, 6.0 / K, seed + 2)))) * \
            C.smoothstep(-0.1, 0.4, C.value_noise(p, 2.0 / K, seed + 3))
        col = C.lerp3(col, pal.seam, band * 0.45 + crack * 0.6)
        col = C.lerp3(col, pal.bone, C.smoothstep(0.85, 1.25, z) * 0.5)
        fold = 0.5 + 0.5 * np.sin(z * 26.0 + 2.0 * C.value_noise(p, 5.0 / K, seed + 4))
        deep = 1 - C.smoothstep(-0.1, 0.7, z)
        flesh = C.lerp3(np.tile(np.array((0.07, 0.015, 0.04)), (len(idx), 1)), np.array((0.3, 0.06, 0.12)), fold * 0.7)
        flesh = C.lerp3(flesh, np.array(VIOLET) * 0.5, deep * 0.5)
        s.albedo[idx] = C.lerp3(flesh, col, outer)
        glow = (1 - outer) * (0.08 + 0.45 * deep) * (0.6 + 0.4 * fold) + outer * crack * 0.06
        s.emit[idx] = np.array(VIOLET)[None, :] * glow[:, None]
        s.rough[idx] = 0.42 + 0.4 * outer - 0.1 * crack
        s.disp[idx] = outer * (-0.003 * K * band - 0.004 * K * crack) + (1 - outer) * 0.002 * K * (fold - 0.5)
        H.family(s, idx, chitin=outer * 0.7, membrane=(1 - outer), flow=z)
    return style


def hood_face_style(seed=0):
    """The hood wall round the maw: outside, dark sealed stone with thin violet veins; inside (the funnel), wet dark
    flesh that warms to violet towards the gullet."""
    def style(s, idx, part):
        L, nl = _local(part, s, idx)
        p = s.p[idx]
        rad = np.hypot(L[:, 0], L[:, 1])
        rdir = L[:, :2] / np.maximum(rad[:, None], 1e-6)
        inward = C.smoothstep(0.1, -0.3, np.einsum("ij,ij->i", nl[:, :2], rdir) - 0.4 * nl[:, 2]) *             (1 - C.smoothstep(FUNNEL_R, FUNNEL_R + 0.08, rad))
        z = L[:, 2]
        col = C.chitin_gradient(s.n[idx], HOOD.top, HOOD.side, HOOD.belly) * \
            (1 + 0.16 * C.value_noise(p, 9 / K, seed))[:, None]
        crack = (1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 6 / K, seed + 1)))) * \
            C.smoothstep(-0.2, 0.3, C.value_noise(p, 2 / K, seed + 2))
        col = C.lerp3(col, HOOD.seam, crack * 0.8)
        fold = 0.5 + 0.5 * np.sin(np.arctan2(L[:, 0], L[:, 1]) * 18 + 3 * C.value_noise(p, 6 / K, seed + 3))
        depth = C.smoothstep(LIP_Z, FUNNEL_BACK, z)
        flesh = C.lerp3(np.tile(np.array((0.2, 0.04, 0.09)), (len(idx), 1)), np.array((0.04, 0.01, 0.05)), fold * 0.6)
        flesh = C.lerp3(flesh, np.array(VIOLET) * 0.55, depth * 0.6)
        s.albedo[idx] = C.lerp3(col, flesh, inward)
        glow = inward * (0.05 + 0.75 * depth ** 1.5) * (0.6 + 0.4 * fold) + (1 - inward) * crack * 0.35
        s.emit[idx] = np.array(VIOLET)[None, :] * glow[:, None]
        s.rough[idx] = 0.86 - 0.5 * inward
        s.disp[idx] = (1 - inward) * (-0.005 * K * crack + 0.002 * K * C.value_noise(p, 18 / K, seed + 4)) + \
            inward * 0.002 * K * (fold - 0.5)
        H.family(s, idx, rock=1 - inward, membrane=inward, flow=z)
    return style


def hood_style(seed=0):
    return S.slab(HOOD, K * 0.5, strata=18, veins=0.35, fault=0.2, seed=seed, rim=-2.0)


def _crack_seeds(count, seed, lo, hi):
    rng = np.random.default_rng(seed)
    return rng.uniform(np.array(lo), np.array(hi), size=(count, 3))


# Round 52b (Remco on the arms: "the stone arms could use cracks with a purple glow"): one world-space fracture field
# shared by every arm part, so the cracks run on across the joins between upper arm, elbow, forearm and hand.
_CRACK_LO, _CRACK_HI = (-2.7, -2.9, -0.4), (2.7, 1.0, 3.8)          # Blender space (x, -fwd, up) round the arms
CRACK_SEEDS = {"big": _crack_seeds(650, 521, _CRACK_LO, _CRACK_HI), "fine": _crack_seeds(7000, 522, _CRACK_LO, _CRACK_HI)}


def _edge(points, seeds, pad):
    """voronoi_edge over the seeds near `points` only (the field is global; a part needs just its neighbourhood)."""
    lo, hi = points.min(axis=0) - pad, points.max(axis=0) + pad
    near = seeds[np.all((seeds >= lo) & (seeds <= hi), axis=1)]
    if len(near) < 2:
        return np.full(len(points), 1.0)
    return C.voronoi_edge(points, near)


def _edge_on_surface(points, normals, seeds, pad):
    """Round 56: the fissure distance measured ON the surface, and how squarely the surface cuts the crack.

    voronoi_edge is a distance in space to the border plane between the two nearest seeds. Where a surface runs
    nearly along that plane (the round shoulder and mantle), every point of a broad patch is close to it, so a 2 cm
    fissure became a 10-20 cm violet smear. Measured along the surface the distance is the space distance divided by
    the sine of the angle between the surface and the plane; `cut` is that sine (1 = the crack cuts straight across,
    0 = the surface lies in the crack plane), so a fissure keeps its width wherever it runs and fades where the stone
    would be split along its face instead of across it."""
    lo, hi = points.min(axis=0) - pad, points.max(axis=0) + pad
    near = seeds[np.all((seeds >= lo) & (seeds <= hi), axis=1)]
    if len(near) < 2:
        return np.full(len(points), 1.0), np.ones(len(points))
    d1 = np.full(len(points), np.inf)
    d2 = np.full(len(points), np.inf)
    i1 = np.zeros(len(points), dtype=int)
    i2 = np.zeros(len(points), dtype=int)
    for i, s in enumerate(near):
        d = np.linalg.norm(points - s, axis=1)
        closer = d < d1
        second = ~closer & (d < d2)
        d2 = np.where(closer, d1, np.where(second, d, d2))
        i2 = np.where(closer, i1, np.where(second, i, i2))
        d1 = np.where(closer, d, d1)
        i1 = np.where(closer, i, i1)
    plane = near[i2] - near[i1]
    plane /= np.maximum(np.linalg.norm(plane, axis=1), 1e-9)[:, None]
    n = normals / np.maximum(np.linalg.norm(normals, axis=1), 1e-9)[:, None]
    cos = np.abs(np.einsum("ij,ij->i", n, plane))
    cut = np.sqrt(np.clip(1.0 - cos * cos, 0.0, 1.0))
    return (d2 - d1) * 0.5 / np.maximum(cut, 0.2), cut


def crack_hot_spots():
    """Where the seal strains most (wider fissures): the elbows and shoulders of the planted arms."""
    out = []
    for side, lg in lower_arms().items():
        sh, el, wr = lg.rest_points
        out += [(np.array(el), 0.6), (np.array(sh), 0.75)]
    return out


def cracked(base, seed=0, scale=1.0, fine=1.0):
    """Wraps a stone style with a fracture network leaking violet core light (round 52b):
    - deep fissures: borders of a noise-warped Voronoi field (~0.3 m cells), opened in some places and closed in
      others so they branch and stop instead of drawing closed cells; wider and hotter near elbows and shoulders;
    - hairlines: a finer warped field, faint and patchy;
    - chipped edges: pale fresh stone and small breaks along the deep fissures.
    Glow varies along every crack (brightest deep in the fissures, faint in the hairlines). The light is the
    throat's violet, so the phase emission sets (Phase2..4, Release) brighten it with the rest of the entity."""
    hot = crack_hot_spots()
    violet = np.array(VIOLET)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        warp = np.stack([C.value_noise(p, 2.2 / scale, seed + 30 + i) for i in range(3)], axis=1)
        q = p + warp * 0.07 * scale
        # Round 56: measured along the surface (no broad smear where a round shoulder runs along a crack plane).
        e_big, cut = _edge_on_surface(q, s.n[idx], CRACK_SEEDS["big"], 0.8)
        across = C.smoothstep(0.12, 0.35, cut)
        e_fine = _edge(q + warp * 0.05, CRACK_SEEDS["fine"], 0.5)
        heat = np.zeros(len(idx))
        for c, r in hot:
            heat = np.maximum(heat, 1 - C.smoothstep(0.2 * r, r, np.linalg.norm(p - c, axis=1)))
        # Width varies along a crack; some stretches close up (the network branches and stops).
        open_ = C.smoothstep(-0.12, 0.28, C.value_noise(p, 1.3 / scale, seed + 41))
        width = (0.004 + 0.018 * C.smoothstep(-0.2, 0.9, C.value_noise(p, 2.6 / scale, seed + 42)) ** 1.5
                 + 0.02 * heat)
        gate = np.maximum(open_, 0.7 * heat * C.smoothstep(-0.3, 0.2, C.value_noise(p, 1.9 / scale, seed + 46)))
        gate = gate * across
        fissure = (1 - C.smoothstep(width * 0.35, width, e_big)) * gate
        # Round 56 (the glow bled into a broad blurry band on the upper arm by the shoulder): the light sits in a
        # narrow core inside the fissure. Near the hot spots the fissure widened by up to 2 cm and the glow filled all
        # of it, soft edge included; the core widens by 4 mm at most, so the open crack stays dark round its light.
        glow_w = width - 0.016 * heat
        core = (1 - C.smoothstep(glow_w * 0.2, glow_w * 0.65, e_big)) * gate
        chip = (1 - C.smoothstep(width, width * 2.6, e_big)) * gate * (1 - fissure)
        chip *= C.smoothstep(-0.1, 0.4, C.value_noise(p, 14.0 / scale, seed + 43))
        hair = (1 - C.smoothstep(0.0015, 0.005, e_fine)) * C.smoothstep(0.0, 0.5, C.value_noise(p, 2.4 / scale, seed + 44))
        hair *= (1 - fissure) * fine
        pulse = 0.2 + 0.8 * C.smoothstep(-0.5, 0.8, C.value_noise(p, 2.0 / scale, seed + 45))
        # Hairlines glow only where they branch off an open fissure; elsewhere they are dark dry cracks.
        branch = hair * (1 - C.smoothstep(0.03, 0.16, e_big)) * gate
        glow = core * pulse * (0.62 + 0.3 * heat) + branch * 0.45 * pulse + hair * 0.04
        col = s.albedo[idx]
        col = C.lerp3(col, PAL.bone * 0.95, chip * 0.5)
        col = C.lerp3(col, PAL.seam, hair * 0.6)
        col = C.lerp3(col, violet * 0.45, fissure)
        s.albedo[idx] = col
        s.emit[idx] = np.maximum(s.emit[idx], violet[None, :] * np.clip(glow, 0, 1)[:, None])
        s.disp[idx] = s.disp[idx] - 0.006 * K * fissure * (0.6 + 0.6 * heat) - 0.0012 * K * hair + 0.001 * K * chip
        s.rough[idx] = np.clip(s.rough[idx] - 0.3 * fissure, 0.1, 0.95)
    return style


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def lower_arms():
    out = {}
    for side, sx in SIDES:
        out[side] = C.Limb([f"LArm_{side}_Upper", f"LArm_{side}_Fore"], T.gp(LOWER["shoulder"], sx),
                           T.gp(LOWER["hand"], sx) + G(0, 0.3, 0), LOWER["lengths"], G(0.8 * sx, 0.4, -0.6))
    return out


def hand_centre(side):
    lg = lower_arms()[side]
    return lg.rest_points[2] + G(0, -0.1, 0.15)


def finger_dirs(sx):
    return [(G(spread * 0.8 * sx + 0.15 * sx, 0, 1.0)).normalized() for spread in (-0.45, 0.0, 0.45)]


def body_parts():
    b = C.PartBuilder()
    k = K
    torso_parts(b, k)
    head_parts(b, k)
    arm_parts(b, k)
    ground_parts(b, k)
    return b


def torso_parts(b, k):
    """A hunched ribcage of bone-stone round a violet core, rising out of the fissure; dark sealed-stone mantles on
    the shoulders (it was sealed in stone and bone)."""
    b.ellipsoid("Waist", G(0, 0.55, -0.05), (0.98, 0.78, 1.35), bone="Body", segments=(40, 20),
                style=H.glow_flesh(PAL, k * 0.4, glow=0.8, folds=8.0))
    b.ellipsoid("Core", G(0, 2.25, -0.02), (1.05, 0.8, 1.0), bone="Chest", segments=(48, 24),
                style=T.flesh_glow(PAL, k * 0.5, glow=1.0, folds=9.0, seed=2, axis=2))
    b.parts[-1].chain = ["Chest", "Body"]
    for i, u in enumerate((1.45, 1.8, 2.15, 2.5, 2.82)):
        w = 1.08 - 0.1 * abs(i - 2.4)
        for side, sx in SIDES:
            pts = [G(0.12 * sx, u + 0.05, -0.7), G(0.92 * w * sx, u + 0.1, -0.4), G(1.1 * w * sx, u, 0.12),
                   G(0.8 * w * sx, u - 0.12, 0.62), G(0.18 * sx, u - 0.2, 0.8)]
            b.tube("Rib", pts, [0.11, 0.13, 0.13, 0.11, 0.08], sides=10, chain=["Chest" if u > 1.7 else "Body"],
                   style=S.pillar(PAL, k * 0.4, drums=6, seed=10 + i, glow=0.5))
    b.ellipsoid("Sternum", G(0, 2.15, 0.85), (0.2, 0.13, 0.72), bone="Chest", segments=(24, 12),
                deform=C.chiselled(10, 20, 0.08), style=S.slab(PAL, k * 0.4, strata=30, veins=0.6, fault=0.4, seed=21))
    b.ellipsoid("Spine", G(0, 2.3, -0.78), (0.3, 0.22, 1.15), bone="Chest", segments=(28, 14),
                deform=C.chiselled(12, 22, 0.06), style=S.slab(PAL, k * 0.4, strata=34, veins=0.4, seed=23))
    b.parts[-1].chain = ["Chest", "Body"]
    for side, sx in SIDES:
        b.ellipsoid("Shoulder", G(1.12 * sx, 2.85, 0.05), (0.62, 0.55, 0.46), T.rot(roll=-25 * sx), bone="Chest",
                    segments=(32, 16), deform=C.chiselled(16, 24, 0.07, squash=(0.8, 1.0)),
                    style=cracked(S.slab(PAL, k * 0.5, strata=24, veins=0.15, fault=0.3, seed=25), seed=1))
        # The mantle: a broken slab of sealed stone over each shoulder (dark, a gold seal glyph on top: seal_extra).
        b.ellipsoid("Mantle", G(1.2 * sx, 3.18, -0.02), (0.6, 0.5, 0.17), T.rot(roll=-32 * sx, pitch=-8), bone="Chest",
                    segments=(28, 14), deform=C.chiselled(11, 26 + (sx > 0), 0.14, squash=(0.7, 1.0)),
                    style=cracked(S.slab(HOOD, k * 0.5, strata=20, veins=0.2, fault=0.3, seed=27, rim=-0.5), seed=2,
                                  fine=0.6))
    b.tube("Neck", [G(0, 2.65, 0.1), G(0, 2.98, 0.2), G(0, 3.25, 0.25)], [0.55, 0.5, 0.42], sides=16,
           chain=["Neck", "Chest"], style=S.pillar(PAL, k * 0.4, drums=4, seed=30, glow=0.6))


def head_parts(b, k):
    """The hood (dome + the funnel wall round the maw), the gullet, the three petals with their gums, the halo axle."""
    # The hood: three overlapping arcs of one shell of revolution (UV seams at the arc borders); outer surface the
    # hood from its back pole to the lip, inner surface the face cup from its bottom to the lip.
    for i in range(3):
        a0 = math.radians(120 * i - 64)
        a1 = math.radians(120 * i + 64)

        def shell(u, v, inner, a0=a0, a1=a1):
            phi = a0 + (a1 - a0) * u
            if inner:
                z = FUNNEL_BACK + (LIP_Z - FUNNEL_BACK) * v
                return maw_point(hood_ri(z), phi, z)
            z = HOOD_ZB + (LIP_Z - HOOD_ZB) * (1.0 - (1.0 - v) ** 1.35)
            return maw_point(hood_ro(z, phi), phi, z)
        b.slab("Hood", lambda u, v, f=shell: f(u, v, False), lambda u, v, f=shell: f(u, v, True), 26, 34,
               frame=MAW_FRAME.inverted(), chain=["Head"], style=hood_face_style(54 + i))
    # Two cowl folds sweep back along the sides of the hood (break its outline), lying on the surface.
    for side, sx in SIDES:
        zs = np.linspace(-0.3, -1.75, 6)
        pts = [maw_point(hood_ro(z, sx * math.radians(82)) * 0.97, sx * math.radians(82 + 10 * i), z)
               for i, z in enumerate(zs)]
        b.tube("HoodFold", pts, [0.08, 0.13, 0.14, 0.12, 0.09, 0.05], sides=12, chain=["Head"], style=hood_style(53))
    # A crest along the top of the hood from the brow back to the halo axle (a cowl, not a ball).
    zs = np.linspace(-0.5, -1.45, 6)
    pts = [maw_point(hood_ro(z, 0.0) * 0.985, 0.0, z) for z in zs]
    b.tube("HoodCrest", pts, [0.06, 0.12, 0.15, 0.16, 0.15, 0.13], sides=12, chain=["Head"], flatten=0.7,
           style=hood_style(59))
    # The gullet: the hot violet bottom of the funnel.
    b.ellipsoid("Gullet", maw_point(0.0, 0.0, GULLET_Z), (0.28, 0.28, 0.1), MAW_ROT, bone="Head",
                segments=(24, 12), style=T.solid_glow(VIOLET, 1.25, 0.7))
    # The inner gum rings (the inner rings of teeth are rooted in them: sealedone_detail.py).
    for j, (z, r) in enumerate(INNER_GUMS):
        pts = [maw_point(r, math.radians(a), z) for a in range(0, 361, 20)]
        b.tube("ThroatGum", pts, [GUM_R] * len(pts), sides=12, chain=["Head"], style=MO.gum_style(60 + j, glow=VIOLET,
                                                                                                    glow_k=0.25))
    petal_parts(b, k)
    # The halo axle: out of the back of the hood, carrying the three ring hubs (seal_parts).
    a0, a1 = AXLE_Z
    b.tube("Axle", [maw_point(HALO_UP, 0, a0), maw_point(HALO_UP, 0, (a0 + a1) * 0.5), maw_point(HALO_UP, 0, a1)],
           [0.16, 0.12, 0.13], sides=14, chain=["Head"], style=hood_style(57))
    b.ellipsoid("AxleKnob", maw_point(HALO_UP, 0, a1 - 0.03), (0.17, 0.17, 0.11), MAW_ROT, bone="Head",
                segments=(16, 8), deform=C.chiselled(8, 58, 0.12), style=hood_style(58))


GUM_R = 0.045
INNER_GUMS = [(-0.12, 0.51), (-0.24, 0.385)]    # (z along the axis, radius) of the two throat gum rings
PETAL_GUM_INSET = 0.16                        # the petal gums run this share of the half width inside each edge
PETAL_GUM_S = (0.06, 0.74)


def petal_gum_path(phi, edge, extra=0.0):
    """The gum line along one edge (+1 / -1) of a petal's inner face (rest pose + `extra`), root to near the tip."""
    w = edge * (1.0 - PETAL_GUM_INSET)
    pts = []
    for s in np.linspace(PETAL_GUM_S[0], PETAL_GUM_S[1], 8):
        p = petal_inner_point(phi, s, w, extra)
        inward, _ = petal_dirs(phi, s, w, extra)
        pts.append(p - inward * GUM_R * 0.25)
    return pts


def petal_parts(b, k):
    for bone, phi in PETALS:
        b.slab("Petal", lambda u, v, ph=phi: petal_closed(ph, u, v, True),
               lambda u, v, ph=phi: petal_closed(ph, u, v, False), 22, 30, frame=petal_frame(phi), chain=[bone],
               style=petal_style(70 + int(math.degrees(phi)) % 7), transform=lambda p, ph=phi: petal_matrix(ph) @ p)
        for edge in (-1, 1):
            pts = petal_gum_path(phi, edge)
            radii = [GUM_R * 0.7] + [GUM_R] * (len(pts) - 2) + [GUM_R * 0.6]
            b.tube("PetalGum", pts, radii, sides=12, chain=[bone], style=MO.gum_style(80, glow=VIOLET, glow_k=0.2))


def arm_parts(b, k):
    """Two massive planted arms: bone-stone, dark stone shackles with a gold glyph band and a broken chain stub at
    the wrists, three long-clawed fingers (talons in sealedone_detail.py)."""
    for side, lg in lower_arms().items():
        sx = -1 if side == "L" else 1
        sh, el, wr = lg.rest_points
        b.tube("LUpper", [sh, sh.lerp(el, 0.5) + G(0.05 * sx, 0.12, 0), el], [0.62, 0.52, 0.42], sides=18,
               chain=[lg.names[0]], style=cracked(S.pillar(PAL, k * 0.5, drums=2, seed=40, glow=0.05), seed=3))
        b.tube("LFore", [el, el.lerp(wr, 0.5), wr], [0.44, 0.4, 0.33], sides=18, chain=[lg.names[1]],
               style=cracked(S.pillar(PAL, k * 0.5, drums=2, seed=41, glow=0.05), seed=4))
        b.ellipsoid("Elbow", el, (0.5, 0.5, 0.45), bone=lg.names[0], segments=(20, 10), deform=C.chiselled(10, 42, 0.1),
                    style=cracked(S.slab(PAL, k * 0.4, veins=0.1, fault=0.2, seed=43), seed=5))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        # The shackle: a heavy cuff of sealed stone round the forearm above the wrist.
        axis = (wr - el).normalized()
        cuff = el.lerp(wr, 0.74)
        b.ring("Shackle", cuff, axis, 0.42, 0.11, arcs=3, sides=10, chain=[lg.names[1]],
               style=S.slab(HOOD, k * 0.4, strata=18, veins=0.2, fault=0.2, seed=44), ref=G(0, 1, 0),
               profile=lambda th: 1.0 + 0.18 * math.cos(3 * th) + 0.08 * math.sin(7 * th))
        chain_stub(b, cuff, axis, sx, lg.names[1])
        hand = hand_centre(side)
        b.ellipsoid("Hand", hand, (0.56, 0.2, 0.56), T.rot(yaw=-15 * sx), bone=f"LArm_{side}_Hand", segments=(24, 12),
                    deform=C.chiselled(10, 44, 0.08),
                    style=cracked(S.slab(PAL, k * 0.4, veins=0.1, fault=0.2, seed=45), seed=6, scale=0.7))
        for d in finger_dirs(sx):
            r0 = hand + d * 0.34
            pts = [r0, r0 + d * 0.45 + G(0, 0.1, 0), r0 + d * 0.88 - G(0, 0.05, 0), r0 + d * 1.2 - G(0, 0.16, 0)]
            b.tube("Finger", pts, [0.16, 0.125, 0.08, 0.01], sides=10, chain=[f"LArm_{side}_Hand"],
                   style=cracked(H.blade(PAL, k * 0.35, glow=0.4, teeth=0), seed=7, scale=0.5, fine=0.5))


def chain_stub(b, cuff, axis, sx, bone):
    """Three heavy chiselled links hanging from the outer side of the cuff along the forearm, the last one broken
    open; each link passes through the one before it (the remesh fuses them to the cuff: nothing hangs loose)."""
    out = (G(sx, 0, 0) - axis * axis.dot(G(sx, 0, 0))).normalized()
    side_dir = axis.cross(out).normalized()
    start = cuff + out * 0.5
    for j in range(3):
        c = start + axis * (0.24 * j + 0.06) + out * 0.03 * j
        normal = side_dir if j % 2 == 0 else out
        prof = (lambda th, j=j: 1.0 + 0.2 * math.sin(2 * th + j) + (0.0 if j < 2 else -0.6 * (abs(math.cos(th)) > 0.92)))
        b.ring("Chain", c, normal, 0.15, 0.05, arcs=3, sides=8, chain=[bone],
               style=S.slab(HOOD, K * 0.3, strata=12, veins=0.0, fault=0.0, seed=46 + j), ref=axis, profile=prof)


def ground_parts(b, k):
    """Tendrils of the waist sinking into the fissure (on Body) and the heaved rubble ring (Root; moved to its own
    bone `Rubble` by build_sealedone_mech.py). The rubble keeps clear of the tendrils, so it is its own island and
    never stretches after the body when the body rises or sinks."""
    for j in range(7):
        a = math.radians(-150 + 300 * j / 6)
        d = G(math.sin(a), 0, math.cos(a))
        r0 = G(0, 0.6, 0) + d * 0.8
        b.tube("Tendril", [r0, r0 + d * 0.35 - G(0, 0.4, 0), r0 + d * 0.55 - G(0, 0.95, 0)], [0.32, 0.22, 0.1],
               sides=12, chain=["Body"], style=S.pillar(PAL, k * 0.4, drums=4, seed=55 + j, glow=0.9))
    for j in range(13):
        a = math.radians(-165 + 330 * j / 12)
        if 18 < abs(math.degrees(a)) < 66:
            continue  # the planted arms, hands and fingers come down here (a touching stone would fuse to a hand)
        r = 2.15 + 0.15 * math.sin(j * 2.3)
        c = G(math.sin(a) * r, 0.08, math.cos(a) * r * 0.95)
        b.ellipsoid("Rubble", c, (0.5, 0.36, 0.24), T.rot(yaw=math.degrees(a), roll=20), bone="Root", segments=(16, 8),
                    deform=C.chiselled(9, 60 + j, 0.2), style=S.slab(RUBBLE, k * 0.4, strata=20, veins=0.9, fault=1.0, seed=61))


def halo_centre(z):
    return maw_point(HALO_UP, 0.0, z)


def halo_point(z, radius, theta):
    """A point of the halo plane at axis position z, `radius` from the axle, theta from up (+ to the right)."""
    return halo_centre(z) + radial(theta) * radius


def seal_parts():
    b = C.PartBuilder()
    k = K
    # The seal stones (the left ones mirror the right ones; Seal_1 stands at the back on the midline).
    for i in range(7):
        base = stone_base(i)
        sz = STONE_SIZE[i]
        b.ellipsoid("SealStone", base + stone_rot(i) @ Vector((0, 0, sz[2] * 0.85)), sz, stone_rot(i), bone=f"Seal_{i + 1}",
                    segments=(28, 18), deform=C.chiselled(14, 70 + i, 0.12, squash=(0.6, 1.0)),
                    style=S.slab(STONE, k * 0.4, strata=26, veins=0.3, fault=0.5, seed=71 + i))
    halo_parts(b, k)
    # Violet corruption crystals: low on the shoulders' backs and down the lower spine (clear of the halo).
    for side, sx in SIDES:
        for j, (p, d, r, l) in enumerate((((1.2, 2.95, -0.3), (0.75, 0.8, -0.45), 0.13, 0.85),
                                          ((1.45, 2.75, -0.05), (1.0, 0.45, -0.2), 0.09, 0.6),
                                          ((0.95, 2.8, -0.35), (0.45, 0.6, -0.8), 0.08, 0.55))):
            b.shard("Corruption", T.gp(p, sx), T.gp(d, sx).normalized(), r, l, sides=8, flute=0.45, twist=1.0 * sx,
                    bone="Chest", style=Y.shard_style("void", 1.0, seed=90 + j))
    for j, (u, f, l) in enumerate(((2.05, -0.92, 0.5), (1.6, -0.9, 0.42))):
        b.shard("Corruption", G(0.06, u, f), G(0.15, 0.25, -1.0).normalized(), 0.11, l, sides=8, flute=0.45, bone="Chest",
                style=Y.shard_style("void", 1.0, seed=95 + j))
    return b


def halo_parts(b, k):
    """Three broken flat stone rings behind the hood (seal rings, glyphs on both faces: halo_glyphs), each on its own
    hub round the axle, carried by tapered stone tines. The arcs are symmetric about the creature's midline (the bake
    mirrors the left half); every arc carries a tine, so no arc ever hangs loose while its ring turns."""
    stone = S.slab(HALO, k * 0.3, strata=14, veins=0.0, fault=0.0, seed=82)
    tine = S.pillar(HALO, k * 0.3, drums=2, seed=83, glow=0.0)
    for name, radius, z, phase, arcs, width in RINGS:
        b.ellipsoid("HaloHub", halo_centre(z), (0.24, 0.24, 0.08), MAW_ROT, bone=name, segments=(20, 10),
                    deform=C.chiselled(10, 84, 0.1), style=stone)
        span = 360.0 / arcs
        gap = 24.0 if arcs == 3 else 18.0
        for i in range(arcs):
            mid = phase + span * i
            lo, hi = mid - (span - gap) * 0.5, mid + (span - gap) * 0.5
            seed = 7 * i + arcs

            def band(u, v, front, lo=lo, hi=hi, z=z, radius=radius, width=width, seed=seed):
                t = math.radians(lo + (hi - lo) * u)
                # Broken ends: the band narrows and chips towards each end; worn, uneven edges along it.
                end = min(u, 1.0 - u)
                taper = 0.35 + 0.65 * min(1.0, end / 0.12) ** 0.6
                rough = 1.0 + 0.08 * math.sin(7.3 * t + seed) + 0.05 * math.sin(17.0 * t + 2 * seed)
                rr = radius + width * rough * taper * (2.0 * v - 1.0)
                th = 0.045 * (0.7 + 0.3 * taper)
                return halo_point(z + (th if front else -th), rr, t)
            # Four overlapping segments per arc: part borders become UV seams, so each arc unrolls in short charts
            # (one chart per arc stretched the glyphs into streaks).
            for j in range(4):
                u0, u1 = max(0.0, j / 4 - 0.02), min(1.0, (j + 1) / 4 + 0.02)
                b.slab("Ring", lambda u, v, f=band, u0=u0, u1=u1: f(u0 + (u1 - u0) * u, v, True),
                       lambda u, v, f=band, u0=u0, u1=u1: f(u0 + (u1 - u0) * u, v, False), 12, 4, chain=[name],
                       style=stone)
            # The tine: from the hub out to the middle of the arc, bowed back a little, tapering out.
            th = math.radians(mid)
            tpts = [halo_point(z, 0.12, th), halo_point(z, radius * 0.5, th) + RING_NORMAL * 0.05,
                    halo_point(z, radius - width * 0.5, th)]
            b.tube("HaloTine", tpts, [0.085, 0.05, 0.075], sides=10, chain=[name], style=tine)


def halo_glyphs():
    """Seal glyphs on both faces of the flat halo rings (a cylinder band would put them on the rings' edges)."""
    col = np.array(GOLD)
    line = np.array((0.95, 0.62, 0.25))
    rings = []
    for name, radius, z, phase, arcs, width in RINGS:
        rings.append((np.array(halo_centre(z)), radius, width, 2 * math.pi / (5 * arcs)))
    d = np.array(D)
    up, xx = np.array(E_UP), np.array(E_X)

    def extra(s, pid):
        for c, radius, width, cell in rings:
            rel = s.p - c
            h = rel @ d
            ang = np.arctan2(rel @ xx, rel @ up)
            rad = np.linalg.norm(rel - np.outer(h, d), axis=1)
            near = (np.abs(h) < 0.08) & (np.abs(rad - radius) < width * 1.05)
            if not near.any():
                continue
            local = ((ang + cell / 2) % cell) - cell / 2
            x = local * radius / (width * 0.8)
            y = (rad - radius) / (width * 0.8)
            m = T.seal_mask(np.hypot(x, y), np.arctan2(x, y), line=1.6) * near
            edge = (1 - C.smoothstep(0.0, 0.02, np.abs(np.abs(rad - radius) - width * 0.82))) * near
            m = np.maximum(m, edge * 0.6)
            s.disp -= 0.004 * m
            s.albedo = C.lerp3(s.albedo, line, m * 0.85)
            s.emit = np.maximum(s.emit, col[None, :] * (m * 0.72)[:, None])
    return extra


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0.4, 0), None, True),
           ("Body", G(0, -0.6, 0.0), G(0, 1.7, 0.0), "Root", True),
           ("Chest", G(0, 1.7, 0.0), G(0, 2.75, 0.12), "Body", True),
           ("Neck", G(0, 2.75, 0.12), G(0, 3.4, 0.42), "Chest", True),
           ("Head", G(0, 3.4, 0.42), G(0, 4.6, 0.3), "Neck", True)]
    for bone, phi in PETALS:
        h = hinge(phi)
        out.append((bone, h, h + petal_matrix(phi).to_3x3() @ D * 0.8, "Head", True))
    for name, radius, z, phase, arcs, width in RINGS:
        c = halo_centre(z)
        out.append((name, c, c + RING_NORMAL * 0.5, "Head", True))
    for side, lg in lower_arms().items():
        out += lg.rest_bones("Chest")
        wr = lg.rest_points[2]
        out.append((f"LArm_{side}_Hand", wr, wr + G(0, -0.1, 0.6), lg.names[1], True))
    for side, sx in SIDES:
        us, ue, uw = (T.gp(UPPER[k_], sx) for k_ in ("shoulder", "elbow", "wrist"))
        out += [(f"UArm_{side}_Upper", us, ue, "Chest", True), (f"UArm_{side}_Fore", ue, uw, f"UArm_{side}_Upper", True),
                (f"UArm_{side}_Hand", uw, uw + G(0, 0.5, 0.1), f"UArm_{side}_Fore", True)]
    for i in range(7):
        base = stone_base(i)
        out.append((f"Seal_{i + 1}", base, base + stone_rot(i) @ Vector((0, 0, 1.0)), "Root", True))
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def stone_axis(i):
    """Positive turn tilts the stone's top outwards (more broken), negative closes it over the centre."""
    return Z.cross(stone_dir(i)).normalized()


def ring_scale(s):
    """The clips flare the rings past 1.0 (summon, release); the halo grows only RING_GROW of that so it stays
    behind the hood and clear of the back."""
    return s if s <= 1.0 else 1.0 + RING_GROW * (s - 1.0)


def specs(sink=0.0, lean=0.0, roll=0.0, yaw=0.0, chest=0.0, head=0.0, head_yaw=0.0, plates=0.0, rings=1.0,
          spin=(0.0, 0.0, 0.0), uarm_up=0.0, uarm_out=0.0, uelbow=0.0, seal=0.0, seal_lift=0.0, jaw=None):
    """Hinge specs (radians). `sink` lowers the whole body (metres, positive down); `plates` > 0 opens the maw: the
    three petals (Plate_R, Plate_L, Jaw) turn outwards about their hinges by PETAL_K x plates on top of their rest
    opening (round 52; `jaw` overrides the lower petal). They never close past rest (the teeth would meet).
    `seal` > 0 closes the seal stones (inward tilt), `seal_lift` raises them."""
    out = {"Root": {"rot": []},
           "Body": {"rot": [(Z, yaw), (Yv, roll), (X, lean * 0.4)], "loc": Vector((0, 0, -sink))},
           "Chest": {"rot": [(X, lean * 0.6 + chest)]},
           "Neck": {"rot": [(Z, head_yaw * 0.5), (X, head * 0.5)]},
           "Head": {"rot": [(Z, head_yaw * 0.5), (X, head * 0.5)]}}
    for bone, phi in PETALS:
        a = PETAL_K * plates if (bone != "Jaw" or jaw is None) else jaw
        out[bone] = {"rot": [(hinge_axis(phi), max(0.0, a))]}
    rs = rings if isinstance(rings, (list, tuple)) else (rings,) * 3
    for name, s, a in zip(RING_NAMES, rs, spin):
        out[name] = {"rot": [(RING_NORMAL, a)], "scale": max(ring_scale(s), 0.001)}
    for side, sx in SIDES:
        out[f"UArm_{side}_Upper"] = {"rot": [(Yv, -uarm_out * sx), (X, -uarm_up)]}
        out[f"UArm_{side}_Fore"] = {"rot": [(X, -uelbow)]}
        out[f"UArm_{side}_Hand"] = {"rot": []}
        out[f"LArm_{side}_Hand"] = {"rot": []}
    for i in range(7):
        out[f"Seal_{i + 1}"] = {"rot": [(stone_axis(i), -seal)], "loc": Vector((0, 0, seal_lift))}
    return out


def place_hands(poser, sp, hands=None, curl=0.0):
    """Lower arms: IK from the posed shoulders to `hands` ({side: world target}, default planted)."""
    pose = poser.solve(sp)
    move = pose["Chest"] @ poser.rest["Chest"].inverted()
    for side, lg in lower_arms().items():
        target = hands[side] if hands else lg.foot
        sp.update(lg.pose(poser, move @ lg.root, target, move.to_3x3() @ lg.pole))
        sp[f"LArm_{side}_Hand"] = {"rot": [(X, curl)]}
    return sp


def planted_hands():
    return {s: lg.foot.copy() for s, lg in lower_arms().items()}


IDLE_FRAMES = 120


def idle_spec(poser):
    """Breathing: the maw parts and closes twice per loop (round 52: 4 degrees, was 2), the head sways."""
    def spec(t, f):
        w = 2 * math.pi * t
        sp = specs(sink=-0.04 * math.sin(w), lean=math.radians(2) * math.sin(w), head_yaw=math.radians(10) * math.sin(w + 0.6),
                   head=math.radians(3) * math.sin(2 * w), plates=math.radians(4) * (0.5 + 0.5 * math.sin(2 * w)),
                   spin=(w, -w, w), uarm_up=math.radians(2) * math.sin(w), rings=1.0)
        return place_hands(poser, sp)
    return IDLE_FRAMES, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t)
        sp = specs(sink=-0.05 * k, lean=-math.radians(6) * k, head=-math.radians(10) * k, plates=math.radians(14) * max(k, 0),
                   rings=1.0 + 0.1 * max(k, 0), spin=(0.3 * k, -0.3 * k, 0.2 * k))
        return place_hands(poser, sp)
    return 18, spec, False


SUMMON_FRAMES, SUMMON_PEAK = 90, 44


def summon_spec(poser):
    """The head rears, the maw opens wide on the violet throat (the inner glyphs and teeth rings show), the halo
    flares and spins; the corrupted enemies appear at SUMMON_PEAK."""
    def spec(t, f):
        up = T.ease(6, 34, f) * (1 - T.ease(64, 86, f))
        peak = T.pulse(f, SUMMON_PEAK - 10, SUMMON_PEAK, SUMMON_PEAK + 18)
        spin = 3.0 * T.ease(10, 80, f)
        sp = specs(sink=-0.25 * up, lean=-math.radians(12) * up, head=-math.radians(14) * up,
                   plates=math.radians(32) * up + math.radians(16) * peak, rings=1.0 + 0.3 * up + 0.12 * peak,
                   spin=(spin, -spin * 1.3, spin * 0.7), uarm_up=math.radians(120) * up, uarm_out=math.radians(55) * up,
                   uelbow=-math.radians(40) * up)
        return place_hands(poser, sp, curl=math.radians(10) * up)
    return SUMMON_FRAMES, spec, False


RISE_FRAMES, RISE_BREAK, RISE_SLAM = 150, 22, 92
DEPTH = 5.2


def rise_spec(poser):
    """From under the closed seal: the stones burst open at RISE_BREAK, the body rises out of the fissure, the lower
    arms reach out and slam down on the ground at RISE_SLAM, the halo unfolds and the maw opens in a roar."""
    def spec(t, f):
        burst = T.ease(RISE_BREAK - 6, RISE_BREAK + 4, f)
        rise = 1 - (1 - T.ease(RISE_BREAK, 112, f)) ** 2
        reach = T.pulse(f, 60, 80, RISE_SLAM)
        unfold = T.ease(104, 132, f)
        roar = T.pulse(f, 110, 124, 148)
        sp = specs(sink=DEPTH * (1 - rise), lean=math.radians(10) * (1 - rise) - math.radians(8) * roar,
                   head=-math.radians(16) * roar, plates=math.radians(40) * roar, rings=0.05 + 0.95 * unfold,
                   spin=(2.0 * unfold, -2.0 * unfold, 1.0 * unfold), uarm_up=math.radians(10) * roar,
                   seal=math.radians(58) * (1 - burst) - math.radians(10) * T.pulse(f, RISE_BREAK, RISE_BREAK + 4, RISE_BREAK + 16),
                   seal_lift=0.25 * T.pulse(f, RISE_BREAK - 4, RISE_BREAK + 2, RISE_BREAK + 14))
        pose = poser.solve(sp)
        move = pose["Chest"] @ poser.rest["Chest"].inverted()
        hands = {}
        for side, lg in lower_arms().items():
            sx = -1 if side == "L" else 1
            carried = move @ lg.foot
            raised = carried + G(0.3 * sx, 1.2, 0.2)
            planted = lg.foot.copy()
            follow = 1 - T.ease(70, RISE_SLAM, f)
            hands[side] = planted.lerp(raised, follow) + G(0, 0.9, 0.0) * reach * (1 - follow)
        return place_hands(poser, sp, hands, curl=math.radians(25) * T.pulse(f, RISE_SLAM - 4, RISE_SLAM, 120))
    return RISE_FRAMES, spec, False


DEATH_FRAMES, DEATH_SEAL = 150, 128


def death_spec(poser):
    """A last open of the maw, then it closes, the halo shrinks away, the hands claw the ground and are dragged
    under as it sinks, the seal stones lean in and close over the fissure (shut at DEATH_SEAL)."""
    def spec(t, f):
        shudder = math.sin(f * 1.9) * T.pulse(f, 0, 12, 40)
        rings = 1 - T.ease(16, 56, f)
        sink = DEPTH * T.ease(36, 124, f) ** 1.4
        close = T.ease(80, DEATH_SEAL, f)
        sp = specs(sink=sink, lean=math.radians(14) * T.ease(10, 60, f), roll=math.radians(4) * shudder,
                   head=math.radians(18) * T.ease(10, 60, f) - math.radians(18) * T.pulse(f, 0, 8, 30),
                   plates=math.radians(34) * T.pulse(f, 0, 8, 30), rings=rings, spin=(0.4 * f / 30, -0.4 * f / 30, 0.2 * f / 30),
                   uarm_up=math.radians(40) * T.pulse(f, 10, 40, 90), uarm_out=math.radians(20) * T.pulse(f, 10, 40, 90),
                   seal=math.radians(58) * close + math.radians(4) * math.sin(f * 0.8) * close * (1 - T.ease(DEATH_SEAL - 6, DEATH_SEAL, f)))
        pose = poser.solve(sp)
        move = pose["Chest"] @ poser.rest["Chest"].inverted()
        drag = T.ease(60, 120, f)
        hands = {s: lg.foot.lerp(move @ lg.foot, drag) for s, lg in lower_arms().items()}
        return place_hands(poser, sp, hands, curl=math.radians(30) * T.ease(30, 60, f))
    return DEATH_FRAMES, spec, False


CLIPS = {"Idle": idle_spec, "Rise": rise_spec, "Summon": summon_spec, "Hit": hit_spec, "Death": death_spec}
BODY_RIGID = {"Tendril", "Sternum", "Shoulder", "Mantle", "Hood", "HoodFold", "HoodCrest", "Gullet", "ThroatGum",
              "Petal", "PetalGum", "Axle", "AxleKnob", "Hand", "Finger", "Rubble", "Elbow", "Shackle", "Chain"}
SEAL_RIGID = {"SealStone", "Ring", "HaloHub", "HaloTine", "Corruption"}
LOD_DROP = {"Neck": "Chest", **{f"UArm_{s}_Hand": f"UArm_{s}_Fore" for s, _ in SIDES}}


def seal_extra():
    """The glyph on both broad faces of every seal stone (right side; the bake mirrors the left) and a band of small
    glyphs round each halo ring."""
    out = []
    for i in (0, 1, 3, 5):
        base = stone_base(i)
        rot_m = stone_rot(i)
        sz = STONE_SIZE[i]
        c = base + rot_m @ Vector((0, 0, sz[2] * 0.9))
        n_in = rot_m @ Vector((0, 1, 0))
        n_in = n_in if n_in.dot(-stone_dir(i)) > 0 else -n_in
        up = rot_m @ Vector((0, 0, 1))
        for n in (n_in, -n_in):
            out.append(T.seal_rune(c + n * sz[1] * 0.85, n, up, sz[0] * 0.85, GOLD, glow=0.9, depth=0.012,
                                   line_col=(0.9, 0.6, 0.25), facing=0.4, thickness=sz[1] * 0.6, line=1.2, wear=0.35,
                                   seed=i))
    out.append(halo_glyphs())
    return T.B.chain_extras(*out)


def petal_glyph_extras():
    """The seal glyph carved into every petal: on its inner face (lit gold, hidden while the maw is shut, shown as it
    opens) and, faint and dark, on its outer face. Right petal and the lower one only: the bake mirrors the left."""
    out = []
    for bone, phi in PETALS:
        if bone == "Plate_L":
            continue
        # The lower petal's glyph sits further back: from above, the top seam would show it with the maw shut.
        s, w = (0.34 if bone != "Jaw" else 0.24), 0.0
        p = petal_inner_point(phi, s, w)
        inward, along = petal_dirs(phi, s, w)
        out.append(T.seal_rune(p + inward * 0.004, inward, along, 0.21, GOLD, glow=1.1, depth=0.008,
                               line_col=(0.95, 0.62, 0.25), facing=0.35, thickness=0.05, line=1.3, wear=0.15,
                               seed=7 + len(out)))
        so = 0.42
        po = petal_matrix(phi) @ maw_point(petal_r(so) + petal_thick(so, 0.0), phi, so * PETAL_LEN)
        out.append(T.seal_rune(po, -petal_dirs(phi, so, 0.0)[0], along, 0.27, GOLD, glow=0.06, depth=0.012,
                               line_col=(0.45, 0.39, 0.36), facing=0.35, thickness=0.07, line=1.6, wear=0.1,
                               seed=17 + len(out)))
    return out


LIP_LINES = (-0.035, -0.235)


def lip_seal():
    """The seal round the face: two thin gold rings on the hood's front slope round the lip with a band of small seal
    glyphs between them (continuous lines, so it reads as one ring framing the maw, not as loose spots)."""
    band = T.rune_band(maw_point(0.0, 0.0, sum(LIP_LINES) * 0.5), D, hood_ro(sum(LIP_LINES) * 0.5, 0.0), GOLD,
                       glow=0.9, height=0.07, count=14, line_col=(0.95, 0.62, 0.25))
    col = np.array(GOLD)
    o, d = np.array(MAW_O), np.array(D)

    def extra(s, pid):
        rel = s.p - o
        z = rel @ d
        rad = np.linalg.norm(rel - np.outer(z, d), axis=1)
        outside = C.smoothstep(FUNNEL_R + 0.03, FUNNEL_R + 0.08, rad) * (1 - C.smoothstep(1.1, 1.16, rad))
        m = np.zeros(len(z))
        for zl in LIP_LINES:
            m = np.maximum(m, 1 - C.smoothstep(0.007, 0.016, np.abs(z - zl)))
        m *= outside
        s.disp -= 0.006 * m
        s.albedo = C.lerp3(s.albedo, np.array((0.95, 0.62, 0.25)), m * 0.9)
        s.emit = np.maximum(s.emit, col[None, :] * (m * 1.0)[:, None])
        band(s, pid)
    return extra


def body_extra():
    """Weathering, the petal glyphs, a ring of gold glyphs round the maw (the seal frames the face), a glyph band
    on each shackle and the seal glyph on each stone mantle."""
    out = [H.weathering(K, scratches=0.5, dust=0.4, seed=201, markings=PAL.dark, marking_amount=0.2)]
    out += petal_glyph_extras()
    out.append(lip_seal())
    lg = lower_arms()["R"]
    sh, el, wr = lg.rest_points
    axis = (wr - el).normalized()
    out.append(T.rune_band(el.lerp(wr, 0.74), axis, 0.53, GOLD, glow=0.7, height=0.07, count=6))
    m = G(1.2, 3.18, -0.02)
    up = (T.rot(roll=-32, pitch=-8) @ Vector((0, 0, 1))).normalized()
    out.append(T.seal_rune(m + up * 0.17, up, G(0, 0, 1), 0.34, GOLD, glow=0.75, depth=0.01, line_col=(0.9, 0.6, 0.25),
                           facing=0.4, thickness=0.12, line=1.3, wear=0.4, seed=31))
    return T.B.chain_extras(*out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("sealed", [(body_parts(), (0.65, 0.6, 0.55), None), (seal_parts(), (0.25, 0.22, 0.28), (0.6, 0.3, 1.0))],
                   bones(), CLIPS, out, (0, 0, 2.5), 19.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Idle", 8), ("top", "top", "Idle", 8),
                    ("summon", "close", "Summon", SUMMON_PEAK), ("rise", "close", "Rise", 60),
                    ("dead", "close", "Death", 110)],
                   [("Rise", "close", range(0, RISE_FRAMES + 1, 12)), ("Death", "close", range(0, DEATH_FRAMES + 1, 12)),
                    ("Summon", "close", range(0, SUMMON_FRAMES + 1, 10))],
                   scale_bones=SCALE_BONES)
        return
    C.reset_scene()
    mats = [T.Mat(body_parts(), 25500, ("rock", "membrane", "chitin"), voxel=0.014, ao=0.4, k=K, emission=2.6,
                  specular=0.3, extra=body_extra(), rigid=BODY_RIGID, blend=2, lod_share=0.72),
            T.Mat(seal_parts(), 9000, ("rock", "crystal"), suffix="Seal", voxel=0.011, ao=0.3, extrusion=0.05, ray=0.14,
                  k=K * 0.6, emission=2.6, specular=0.35, extra=seal_extra(), rigid=SEAL_RIGID, blend=0, smooth=3,
                  lod_share=0.28, islands=[f"Seal_{i + 1}" for i in range(7)] + RING_NAMES)]
    report = {"pivot": "ground at the centre of the seal; the body extends ~0.7 below the ground (hidden by terrain)",
              "rise": {"frames": RISE_FRAMES, "seal_breaks_frame": RISE_BREAK, "hands_slam_frame": RISE_SLAM,
                       "start_depth_m": DEPTH, "note": "starts fully under the ground plane: terrain must occlude"},
              "summon": {"peak_frame": SUMMON_PEAK, "socket": "Head (the maw's throat lies 0.4 m forward and 0.55 m "
                                                              "up of the Head bone's head at rest)"},
              "death": {"seal_shut_frame": DEATH_SEAL, "below_ground_from_frame": 124},
              "seal_stone_radius_m": STONE_R, "glyph_colour": GOLD,
              "round52": {"maw": "three petals Plate_R / Plate_L / Jaw hinge on the funnel lip and open outwards with "
                                 "`plates` (rest opening %.0f deg)" % math.degrees(REST_OPEN),
                          "throat_centre_m": [round(v, 3) for v in MAW_O],
                          "halo": "Ring_1..3 on hubs round an axle behind the hood (bone heads at the hubs)",
                          "upper_arms": "UArm_* keyed, deform nothing (no geometry)"}}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), CLIPS, lod_name=LOD_NAME, lod_tris=14000, lod_drop=LOD_DROP, scale_bones=SCALE_BONES,
        report=report, unity_scale=UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("sealed", arm, mesh, actions, lod, out, assets, (0, 0, 2.5), 19.0, ("Idle", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Idle", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Idle", 12), ("back", "back", "Idle", 40), ("summon", "close", "Summon", SUMMON_PEAK),
                      ("rise", "close", "Rise", 70), ("dying", "close", "Death", 100), ("sealed", "close", "Death", DEATH_FRAMES)],
               strips=[("Rise", "close", range(0, RISE_FRAMES + 1, 12)), ("Summon", "close", range(0, SUMMON_FRAMES + 1, 10)),
                       ("Death", "close", range(0, DEATH_FRAMES + 1, 12)), ("Idle", "front", range(0, IDLE_FRAMES, 15))],
               macros=[("head", maw_point(0, 0, 0.6), 7.0, -25, 10, "Summon", SUMMON_PEAK),
                       ("stone", stone_base(5) + G(0, 0.9, 0), 6.0, -20, 15, "Idle", 20),
                       ("chest", G(0.3, 2.4, 0.8), 7.0, -35, 15, "Idle", 20)],
               game_clip=("Idle", 20))


if __name__ == "__main__":
    main()
