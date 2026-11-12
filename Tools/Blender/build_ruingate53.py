"""Round 53: the RuinGate rebuilt as one ancient alien arch (Remco on the round-52 stills: "this doesn't look right, that
gate").

What was wrong with the round-29 gate (build_bosses29_props.py), seen at 3x in r52_ingame_Shattered_walk_ruingate_*:
- eleven separate voussoir slabs with a 7 cm gap between each, every one with its own depth and width: hard stepped
  edges and a dark seam that showed light at the keystone;
- the faces towards the opening had no light of their own and read as black voids;
- the opened iris petals stood in the opening (each swung 98 degrees back on its rim, the floor petal lay in the
  threshold), and the two buttress boulders with their crystal spikes stood apart from the pillars like loose rocks.

The round-53 gate:
- **One band of stone** (a single slab swept from foot to foot: straight jambs and a slightly pointed head of two
  concentric arcs, so the joints run true to the arc centres). Its section is a rounded, chamfered rectangle whose
  edges are worn back by noise and chipped in a few places; the pillars flare and deepen towards their feet. The
  joints are tight interlocking (zig-zag) hairline grooves, dark and closed; the light lives in a few long cracks and
  in the worn glyph bands, in the Shattered's magenta-white overcharge.
- **The faces towards the opening** carry a glowing slot down the middle (where the seal withdraws into the stone) and
  two rows of glyph dashes, on a paler stone: they never read black.
- **A keystone crest**: the band itself rises into a pointed crest over the apex, three overcharged crystal
  horns rooted in it.
- **Root buttresses**: three stone roots grow out of each pillar's outer face and run down into the plinth; crystal
  clusters break out of cracks in the pillars and shoulders, rooted inside the stone (crystal_growth, `sink`).
- **A worn plinth** (a flat-topped, round-edged dais) carries the whole arch and meets the ground with a rounded rim;
  the seal sigil is carved into its top under the opening.
- **The seal**: the same five black-glass iris petals and crystal seal knot, now rooted in the jambs (their rims run
  into the stone). Opening, each petal withdraws into a slot in the arch as one solid piece: it folds flat along its
  rim's normal, its leading edge running back to the rim, onto a line inside the stone, and only there goes to nothing.
  The seal knot flares and draws its four shards in as it collapses (nothing flies, nothing is left in the air).

Unity contract kept: asset name, bones (Root, Seal, Seal_1..4, Petal_1..5), clips Closed_Idle 72 / Open 160 /
Opened_Idle 72 with the same moments (seal crack 30, first light 38, petals start 52, fully open 130), pivot (the
centre of the threshold on the ground, facing -Y), one material with the four maps, 6400 tris, 1024 (Low 512). New:
the petal bones are scale-keyed too (`scale_keyed_bones`).

Each moving piece is remeshed on its own (the arch, every petal, the seal knot, every seal shard), so the iris and the
seal never weld to the stone; everything on Root is one island.

Run: blender -b --factory-startup --python Tools/Blender/build_ruingate53.py -- [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_bosses29_props as P  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import crystal_growth as G49  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
from boss_mech_kit import ease  # noqa: E402
from env_kit import ss  # noqa: E402

R = math.radians
GATE = P.GATE
CHARGE = P.CHARGE
GRANITE = P.GRANITE
BLACKGLASS = P.BLACKGLASS
PALE = np.array((0.6, 0.62, 0.67))

# Opening: straight jambs +-HALF_W up to SPRING, a head of two arcs of ARC_R whose centres sit either side of the
# middle (a slightly pointed head; the Shattered still fits).
HALF_W, SPRING, ARC_R = 2.0, 2.0, 3.0
ARC_X0 = HALF_W - ARC_R                      # centre of the right half's arc (left of the middle)
APEX_T = math.acos(-ARC_X0 / ARC_R)          # arc angle of the apex
APEX_Z = SPRING + ARC_R * math.sin(APEX_T)   # 4.83
FOOT_Z = -0.3                                # the band runs down into the plinth
BAND_W = 0.9                                 # band width (inner to outer edge)
BAND_D = 0.58                                # half depth of the band
PLINTH_TOP = 0.2
OZ = 2.25                                    # iris centre
PETALS = 5
TWIST = 0.55
PETAL_R0 = 0.02                              # the tips meet in the middle, behind the seal knot (no hole)
PETAL_EMBED = 0.09                           # the rims run this far into the stone
SEAL_C = Vector((0, -0.36, OZ))
PIVOT_IN = 0.3                              # petal pivots: this far inside the stone, past the opening outline
OPEN_FRAMES, SEAL_CRACK, FIRST_LIGHT, SWING_START, FULLY_OPEN = 160, 30, 38, 52, 130
IDLE_FRAMES = 72
SWING_ORDER = [0, 2, 4, 1, 3]

_THETA_O = {}


def theta_out(width):
    """Arc angle where an outer arc of ARC_R + width (same centre) reaches the middle: both halves meet in a point."""
    key = round(width, 4)
    if key not in _THETA_O:
        _THETA_O[key] = math.acos(-ARC_X0 / (ARC_R + width))
    return _THETA_O[key]


_LA, _LJ = ARC_R * APEX_T, SPRING - FOOT_Z
QA = _LA / (_LA + _LJ)                       # share of a half-band that is arc


class Weather:
    """Per-side wear of the band's outer edge and faces: slow width waves and a few chipped notches."""

    def __init__(self, seed):
        rng = np.random.default_rng(seed)
        self.waves = [(rng.uniform(4, 13), rng.uniform(0, 2 * math.pi), rng.uniform(0.01, 0.025)) for _ in range(4)]
        self.chips = [(rng.uniform(0.15, 0.95), rng.uniform(0.035, 0.07), rng.uniform(0.04, 0.09)) for _ in range(4)]
        self.bevel = [(rng.uniform(5, 17), rng.uniform(0, 2 * math.pi)) for _ in range(3)]

    def width(self, q):
        w = BAND_W * (1 + sum(a * math.sin(f * q + p) for f, p, a in self.waves))
        for c, half, depth in self.chips:
            w -= depth * math.exp(-((q - c) / half) ** 2)
        return w

    def bevel_k(self, q):
        """0.6..1.4: how far this stretch of edge is worn back."""
        return 1.0 + 0.4 * sum(math.sin(f * q + p) for f, p in self.bevel) / 3


WEATHER = {1: Weather(5301), -1: Weather(5302)}
APEX_W = BAND_W  # both halves meet at the apex with this width (the waves are faded out there)
CREST, CREST_SPAN = 0.85, 0.09  # the keystone crest: extra width at the apex and how far down the head it reaches


def half_band(q, side, v):
    """(x, z) on a half band: q 0 (apex) .. 1 (foot), v 0 (inner edge, the opening) .. 1 (outer edge)."""
    wq = WEATHER[side].width(q)
    w = APEX_W + (wq - APEX_W) * ss(0.0, 0.12, q)          # no wear at the apex itself: the halves meet cleanly
    w += CREST * math.exp(-(q / CREST_SPAN) ** 2)          # the band rises into a pointed crest over the apex
    if q <= QA:
        th = APEX_T * (1 - q / QA)
        to = theta_out(w) * (th / APEX_T)
        xi, zi = ARC_X0 + ARC_R * math.cos(th), SPRING + ARC_R * math.sin(th)
        xo, zo = ARC_X0 + (ARC_R + w) * math.cos(to), SPRING + (ARC_R + w) * math.sin(to)
    else:
        z = SPRING - (q - QA) / (1 - QA) * (SPRING - FOOT_Z)
        flare = 0.5 * max(0.0, 1 - (z - FOOT_Z) / 1.7) ** 2
        xi, zi, xo, zo = HALF_W, z, HALF_W + w + flare, z
    return side * (xi + (xo - xi) * v), zi + (zo - zi) * v


def band_depth(q, z, v, side, front):
    """Half depth with the pillars deepening towards their feet and the edges worn back (a chamfer that varies)."""
    d = BAND_D + 0.24 * max(0.0, 1 - (z - FOOT_Z) / 1.8) ** 2 + 0.05 * ss(0.0, 0.1, -q + 0.1)  # a little deeper at the apex
    k = WEATHER[side].bevel_k(q * 3 + (0 if front else 1.7))
    edge = max(0.0, 1 - v / 0.16) ** 2 + max(0.0, 1 - (1 - v) / 0.16) ** 2
    return d - 0.14 * k * edge


def band_surface(front):
    def fn(u, v):
        v = 0.5 - 0.5 * math.cos(math.pi * v)                   # denser at the edges (the chamfers)
        side = -1 if u < 0.5 else 1
        q = abs(u - 0.5) * 2
        x, z = half_band(q, side, v)
        d = band_depth(q, z, v, side, front)
        wob = 0.025 * math.sin(1.7 * x + 2.3 * z + (0.0 if front else 1.1)) * math.sin(0.9 * z - 1.3 * x)
        return Vector((x, -d - wob if front else d + wob, z))
    return fn


def outer_apex_z():
    return SPRING + (ARC_R + APEX_W + CREST) * math.sin(theta_out(APEX_W + CREST))


# -------------------------------------------------------------------------------------------------------------
# The opening outline (iris geometry)
# -------------------------------------------------------------------------------------------------------------

def inside(x, z, grow=0.0):
    if z < -0.1 - grow:
        return False
    if z <= SPRING:
        return abs(x) <= HALF_W + grow
    return math.hypot(abs(x) - ARC_X0, z - SPRING) <= ARC_R + grow


def ray_radius(phi, grow=0.0, floor=True):
    d = (math.cos(phi), math.sin(phi))
    lo, hi = 0.0, 8.0
    for _ in range(40):
        m = (lo + hi) / 2
        x, z = d[0] * m, OZ + d[1] * m
        ok = inside(x, z, grow) and (z >= 0.0 or not floor)
        lo, hi = (m, hi) if ok else (lo, m)
    return lo


def at_iris(phi, r, y, grow=0.0):
    rr = ray_radius(phi, grow) * r
    return Vector((math.cos(phi) * rr, y, OZ + math.sin(phi) * rr))


_SMOOTH = {}


def smooth_radius(phi):
    """The inflated outline's radius blurred over +-0.35 rad (a rounder iris inside the pointed opening)."""
    if "g" not in _SMOOTH:
        grid = np.linspace(-math.pi, math.pi, 721)
        raw = np.array([ray_radius(float(a), grow=PETAL_EMBED) for a in grid[:-1]])
        k = np.exp(-0.5 * (np.linspace(-0.7, 0.7, 141) / 0.35) ** 2)
        k /= k.sum()
        wrap = np.concatenate([raw[-70:], raw, raw[:70]])
        blur = np.convolve(wrap, k, mode="valid")
        _SMOOTH["g"], _SMOOTH["r"] = grid, np.append(blur, blur[0])
    a = (phi + math.pi) % (2 * math.pi) - math.pi
    return float(np.interp(a, _SMOOTH["g"], _SMOOTH["r"]))


def petal_bounds(i):
    mid = -math.pi / 2 + 2 * math.pi * i / PETALS - TWIST
    return mid - math.pi / PETALS, mid + math.pi / PETALS


def petal_pivot(i):
    """Pivot of petal i inside the stone behind the middle of its rim (in the plinth for the floor petals), and the
    outline's inward normal there. The petal withdraws by folding flat along that normal onto the line through the
    pivot along the outline's tangent: the opening is convex, so that line lies in the stone all along the rim."""
    a0, a1 = petal_bounds(i)
    phi = (a0 + a1) / 2 + TWIST
    p = at_iris(phi, 1.0, 0.0, grow=PIVOT_IN)
    if p.z < 0.05:
        p.z = -PIVOT_IN  # the floor petals fold down into the plinth (top 0.2): out of sight before they lie flat
    t = at_iris(phi + 0.02, 1.0, 0.0) - at_iris(phi - 0.02, 1.0, 0.0)
    t.y = 0
    inward = Vector((-t.z, 0.0, t.x)).normalized()
    if inward.dot(Vector((0, 0, OZ)) - p) < 0:
        inward = -inward
    return p, inward


def petal_surface(i, front):
    a0, a1 = petal_bounds(i)

    def fn(u, v):
        r = PETAL_R0 + (1 - PETAL_R0) * v
        rr = max(ray_radius(0.5 * (a0 + a1) + TWIST * r), 0.5) * r
        gap = 0.006 / max(rr, 0.2)  # hairline seams: no daylight through the shut iris
        phi = a0 + gap + (a1 - a0 - 2 * gap) * u + TWIST * r
        dome = 1 - r * r
        y = -0.11 - 0.24 * dome if front else 0.11 - 0.08 * dome
        # The rim runs PETAL_EMBED into the stone, the foot of the floor petals down into the plinth (an inflated
        # outline, not a clamp: clamping the floor petals to the threshold folded them into a bulging fan). Inside the
        # iris the radius follows a blurred outline, so the pointed apex does not crease the petal from the centre.
        sharp = ray_radius(phi, grow=PETAL_EMBED)
        soft = smooth_radius(phi)
        rad = soft + (sharp - soft) * r ** 4
        return Vector((math.cos(phi) * rad * r, y, OZ + math.sin(phi) * rad * r))
    return fn


# -------------------------------------------------------------------------------------------------------------
# Styles
# -------------------------------------------------------------------------------------------------------------

def arch_frame(p):
    """Per point: distance outside the opening outline (m), the along-arch coordinate (arc angle on the head, height on
    the jambs) and the unit direction from the point into the opening (x, z)."""
    x, z = p[:, 0], p[:, 2]
    ax = np.abs(x)
    sx = np.where(x < 0, -1.0, 1.0)
    head = z > SPRING
    dx, dz = ax - ARC_X0, z - SPRING
    rad = np.hypot(dx, dz)
    d = np.where(head, rad - ARC_R, ax - HALF_W)
    theta = np.where(head, np.arctan2(dz, dx), 0.0)
    inx = np.where(head, -dx / np.maximum(rad, 1e-4), -1.0) * sx
    inz = np.where(head, -dz / np.maximum(rad, 1e-4), 0.0)
    return d, theta, head, sx, inx, inz


# Voussoir joints (arc angles per half, the keystone between the last pair) and pillar courses (heights).
JOINTS_T = [APEX_T * f for f in (0.2, 0.41, 0.62, 0.83)]
COURSES_Z = [0.62, 1.3]
# Long cracks with the overcharge in them: (side, centre along the band (arc angle, or height when < 0 the jamb
# value is -z), wiggle seed).
CRACKS = [(-1, APEX_T * 0.52, 1), (1, -1.55, 2), (1, APEX_T * 0.93, 3)]


def zigzag(t):
    return np.abs(((t * 5.0) % 2.0) - 1.0) - 0.5


def arch_stone(seed):
    # Few small lit cracks (the noise network's closed loops read as doodles); the long cracks below carry the light.
    base = K.rock(GRANITE, seed, strata=6, crack=0.55, crack_glow=0.2, crack_color=CHARGE, frost=0.25, rough=0.65,
                  smooth_top=0.1)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        d, theta, head, sx, inx, inz = arch_frame(p)
        band_v = np.clip(d / BAND_W, 0, 1.6)
        near = (d > -0.15) & (d < BAND_W + 0.8)
        front = ss(0.35, 0.75, -n[:, 1])
        # Tight interlocking joints: a zig-zag hairline across the band, dark, no light through.
        joint = np.zeros(len(idx))
        for t in JOINTS_T:
            off = 0.05 * zigzag(d)
            joint = np.maximum(joint, head * (1 - ss(0.0, 0.012, np.abs((theta - t) * ARC_R - off))))
        for zc in COURSES_Z:
            off = 0.05 * zigzag(d)
            joint = np.maximum(joint, (~head) * (1 - ss(0.0, 0.012, np.abs(p[:, 2] - zc - off))))
        joint *= near
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.06, 0.055, 0.07)), joint * 0.75)
        s.disp[idx] -= 0.01 * joint
        # Glyph bands on the front, worn in runs (old, not a neon sign).
        along = np.where(head, theta * ARC_R, -p[:, 2])
        band = np.zeros(len(idx))
        for c, w in ((0.2, 0.03), (0.74, 0.026)):
            band = np.maximum(band, 1 - ss(0.0, w, np.abs(band_v - c)))
        dash = ss(-0.2, 0.2, np.sin(along * 7.5 + 1.7 * np.sin(along * 1.6 + seed)))
        worn = ss(-0.15, 0.25, C.value_noise(p, 0.9, seed + 3)) * (0.55 + 0.45 * ss(-0.5, 0.5, C.value_noise(p, 4.0, seed + 4)))
        carved = band * dash * front * near
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.05, 0.04, 0.06)), carved * 0.55)
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.3, carved * worn * 0.5)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (carved * worn * 0.62)[:, None], 0, 1)
        s.disp[idx] -= 0.012 * carved
        # Long cracks through the stone, the overcharge burning inside.
        crack = np.zeros(len(idx))
        for side, centre, k_ in CRACKS:
            on_side = sx == side
            if centre > 0:
                coord, extent = theta * ARC_R, head
                c0 = centre * ARC_R
            else:
                coord, extent = p[:, 2], ~head
                c0 = -centre
            wiggle = 0.09 * np.sin(d * 9.0 + k_) + 0.05 * np.sin(d * 23.0 + 2 * k_)
            reach = 1 - ss(0.75, 1.05, band_v + 0.25 * np.sin(k_ * 3.1))
            crack = np.maximum(crack, on_side * extent * reach * (1 - ss(0.0, 0.016, np.abs(coord - c0 - wiggle))))
        crack *= near
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.35, crack * 0.8)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (crack * 0.95)[:, None], 0, 1)
        s.disp[idx] -= 0.02 * crack
        # The faces towards the opening: paler stone, a glowing slot down the middle (the seal's seat) and two rows of
        # glyph dashes, so they never read as a black void.
        soffit = ss(0.45, 0.8, n[:, 0] * inx + n[:, 2] * inz) * (1 - ss(0.04, 0.16, d)) * (p[:, 2] > PLINTH_TOP - 0.02)
        s.albedo[idx] = C.lerp3(s.albedo[idx], PALE, soffit * 0.45)
        slot = (1 - ss(0.0, 0.04, np.abs(p[:, 1]))) * soffit
        rows = (1 - ss(0.0, 0.022, np.abs(np.abs(p[:, 1]) - 0.3))) * soffit
        ticks = ss(-0.1, 0.3, np.sin(along * 11.0 + 0.8)) * worn
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.07, 0.03, 0.08)), np.clip(slot + rows * 0.6, 0, 1) * 0.7)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (0.9 * slot + 0.5 * rows * ticks + 0.06 * soffit)[:, None],
                              0, 1)
        s.disp[idx] -= 0.02 * slot + 0.008 * rows
    return style


def plinth_stone(seed):
    base = K.rock(GRANITE, seed, strata=4, crack=0.6, crack_glow=0.35, crack_color=CHARGE, frost=0.0, rough=0.75,
                  smooth_top=0.15)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        up = ss(0.6, 0.85, n[:, 2])
        r = np.hypot(p[:, 0] / 1.15, p[:, 1])
        ring = np.zeros(len(idx))
        for c in (0.85, 1.45):
            ring = np.maximum(ring, 1 - ss(0.0, 0.03, np.abs(r - c)))
        phi = np.arctan2(p[:, 1], p[:, 0])
        spoke = (1 - ss(0.0, 0.06, np.abs(((phi / (2 * math.pi) * 14) % 1.0) - 0.5))) * ss(0.9, 1.0, r) * (1 - ss(1.38, 1.45, r))
        worn = ss(-0.2, 0.25, C.value_noise(p, 1.1, seed + 5))
        sig = np.clip(ring + spoke * 0.7, 0, 1) * up * worn
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.3, sig * 0.5)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (0.55 * sig)[:, None], 0, 1)
        s.disp[idx] -= 0.012 * sig
        # The floor petal's slot across the threshold, an organic groove (Remco: no square-cut ends on the ground): a
        # lens that tapers to nothing at both ends, its edge wavering and worn, a dark-violet floor and lit lips that
        # fade out with the taper.
        x = p[:, 0]
        u = np.clip(np.abs(x) / PETAL_SLOT_X, 0, 1)
        shape = np.sqrt(np.clip(1 - u ** 2, 0, 1)) ** 0.8 * (1 + 0.18 * C.value_noise(p * np.array((1, 0, 0)), 2.5, seed + 9))
        half = 0.27 * np.clip(shape, 0, None)
        d = np.abs(p[:, 1] + 0.12 - 0.035 * np.sin(2.7 * x + 0.6))
        inside = up * (1 - ss(np.maximum(half - 0.035, 0.0), half + 1e-4, d)) * (half > 0.01)
        lips = up * (1 - ss(0.0, 0.025, np.abs(d - half))) * ss(0.05, 0.4, shape)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.11, 0.035, 0.13)), inside * 0.9)
        s.emit[idx] = np.clip(s.emit[idx] * (1 - inside)[:, None]
                              + CHARGE[None, :] * (0.7 * lips * (1 - inside) + 0.12 * inside)[:, None], 0, 1)
        s.disp[idx] -= 0.025 * inside
    return style


PETAL_SLOT_X = 1.85


def petal_style(seed):
    """Black volcanic glass with the seal sigil across all five petals, the rims glowing (round 29's petals, centred
    on the new iris)."""
    base = K.rock(BLACKGLASS, seed, strata=4, crack=0.4, crack_glow=0.25, crack_color=CHARGE, rough=0.35)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        x, z = p[:, 0], p[:, 2] - OZ
        r = np.hypot(x, z)
        phi = np.arctan2(z, x)
        front = ss(0.3, 0.7, -n[:, 1])
        ring = np.zeros(len(idx))
        for c in (0.55, 1.05, 1.55):
            ring = np.maximum(ring, 1 - ss(0.0, 0.03, np.abs(r - c)))
        spoke = (1 - ss(0.0, 0.05, np.abs(((phi / (2 * math.pi) * 15) % 1.0) - 0.5))) * ss(0.6, 0.7, r) * (1 - ss(1.0, 1.05, r))
        sig = np.clip(ring + spoke * 0.8, 0, 1) * front
        wall = 1 - ss(0.35, 0.6, np.abs(n[:, 1]))                    # the petal's edges (seam and rim walls)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.03, 0.026, 0.045)), wall)
        rim = wall * (1 - ss(0.0, 0.045, np.abs(p[:, 1] + 0.02)))
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.3, sig * 0.6)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (0.72 * sig + 0.5 * rim)[:, None], 0, 1)
        s.disp[idx] -= 0.01 * sig
        s.rough[idx] = np.clip(s.rough[idx] * (1 - 0.4 * front), 0.15, 1)
        s.rough[idx] = np.maximum(s.rough[idx], 0.85 * wall)            # matte edges: no pale sheen on the walls
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def overcharge(seed, strength=1.1):
    glow = G49.uneven_glow(Y.shard_style("overcharge_w", strength, seed=seed), seed, 0.5, 1.3)

    def style(s, idx, part):
        glow(s, idx, part)
        # uneven_glow raises a slightly negative noise to a power now and then (NaN): no dead pixels in the bake.
        s.emit[idx] = np.nan_to_num(s.emit[idx], nan=0.0)
    return style


# -------------------------------------------------------------------------------------------------------------
# Parts
# -------------------------------------------------------------------------------------------------------------

def slab_plinth(v):
    """Ellipsoid deform: a flat-topped dais with a rounded, slightly uneven rim."""
    def sp(c, e):
        return math.copysign(abs(c) ** e, c)
    rim = 1 + 0.035 * math.sin(5.0 * math.atan2(v.y, v.x) + 0.7) + 0.02 * math.sin(11.0 * math.atan2(v.y, v.x))
    return Vector((sp(v.x, 0.55) * rim, sp(v.y, 0.6) * rim, sp(v.z, 0.45)))


def gate_parts():
    b = C.PartBuilder()
    # The band: one slab from foot to foot.
    arch = b.slab("Arch", band_surface(True), band_surface(False), 260, 12, bone="Root", style=arch_stone(5310))
    # One closed band does not unroll in one piece (the minimum-stretch unwrap collapsed half of it): the UV layout
    # cuts it by facet angle like a closed rock (uvlayout smart-projects "ellipsoid" parts).
    arch.kind = "ellipsoid"
    # Crown of three overcharged crystal horns rooted in the crest (the band's own pointed top).
    top = outer_apex_z()
    for k_, (x, ln, lean, rad) in enumerate(((0.0, 1.5, 0.0, 0.17), (-0.32, 1.05, -0.6, 0.12), (0.34, 1.15, 0.65, 0.12))):
        d = Vector((lean, -0.18, 1.0)).normalized()
        base = Vector((x, -0.1, top - 0.1 - 0.45 * abs(lean))) - d * 0.45
        G49.fractured_shard(b, "Horn", base, d, rad, ln + 0.45, 5330 + k_, sides=5 + (k_ == 0), flat=0.7,
                            tip="point", steps=1, chips=2, bend=Vector((lean * 0.2, -0.1, 0)), bone="Root",
                            style=overcharge(5335 + k_, 1.15))
    # Root buttresses: two stone roots out of each pillar's outer face (one raking forward, one back), swelling where
    # they leave the pillar and running down into the plinth like the roots of an old tree.
    for sx in (-1, 1):
        for k_, (y0, z0, reach, yr) in enumerate(((-0.22, 1.6, 0.6, -0.55), (0.3, 1.35, 0.5, 0.5))):
            out = HALF_W + BAND_W
            pts = [Vector((sx * (HALF_W + BAND_W * 0.5), y0 * 0.6, z0 + 0.35)),
                   Vector((sx * (out + 0.15), y0 + yr * 0.2, z0 * 0.75)),
                   Vector((sx * (out + 0.2 + 0.7 * reach), y0 + yr * 0.6, z0 * 0.3)),
                   Vector((sx * (out + 0.3 + reach), y0 + yr, -0.12))]
            b.tube("Buttress", pts, [0.42, 0.33, 0.3, 0.36], sides=14, chain=["Root"],
                   style=K.rock(GRANITE, 5340 + 3 * k_ + (sx > 0), strata=5, crack=0.7, crack_glow=0.55,
                                crack_color=CHARGE, frost=0.3, rough=0.7))
    # Crystal clusters breaking out of cracks in the pillars and a shoulder (rooted inside the stone).
    for k_, (sx, z, size, count) in enumerate(((-1, 2.35, 1.2, 5), (1, 1.95, 0.9, 4))):
        x = sx * (HALF_W + BAND_W - 0.12)
        G49.growth_cluster(b, Vector((x, -0.25, z)), Vector((sx * 1.0, -0.55, 0.45)), size, 5350 + k_, bone="Root",
                           style=overcharge(5355 + k_), count=count, spread=0.28, lean=(0.1, 0.5), floating=0,
                           spiral=1, bent=1, radius_ratio=0.13, name="Crystal", sink=0.3)
    th = theta_out(APEX_W) * 0.55
    sh = Vector((ARC_X0 + (ARC_R + BAND_W * 0.92) * math.cos(th), -0.25, SPRING + (ARC_R + BAND_W * 0.92) * math.sin(th)))
    G49.growth_cluster(b, sh, Vector((math.cos(th), -0.5, math.sin(th))), 0.75, 5358, bone="Root",
                       style=overcharge(5359), count=4, spread=0.25, lean=(0.1, 0.45), floating=0, spiral=1, bent=1,
                       radius_ratio=0.13, name="Crystal", sink=0.3)
    # The plinth: a worn, flat-topped dais under everything, its rim rounding into the ground.
    b.ellipsoid("Plinth", Vector((0, 0.0, PLINTH_TOP - 0.5)), (4.35, 1.75, 0.5), bone="Root", segments=(64, 24),
                deform=slab_plinth, style=plinth_stone(5360))
    # The iris and the seal knot.
    for i in range(PETALS):
        petal = b.slab("Petal", petal_surface(i, True), petal_surface(i, False), 10, 12, bone=f"Petal_{i + 1}",
                       style=petal_style(5370 + i))
        petal.kind = "ellipsoid"  # cut by facet angle: a closed slab's one-piece unwrap collapsed its edges (pale grey)
    b.ellipsoid("SealCore", SEAL_C, (0.3, 0.3, 0.3), bone="Seal", style=K.glow_seam((1.0, 0.75, 0.95), 1.0, CHARGE * 0.7),
                segments=(24, 14), deform=C.chiselled(12, 5380, 0.22))
    for k_ in range(6):
        a = 2 * math.pi * k_ / 6 + 0.3
        d = Vector((math.cos(a) * 0.55, -1.0, math.sin(a) * 0.55))
        b.shard("SealSpike", SEAL_C + Vector((0, -0.12, 0)), d, 0.06, 0.4 if k_ % 2 else 0.3, sides=5, twist=0.8,
                flute=0.3, bone="Seal", style=Y.shard_style("overcharge_w", 1.5, seed=5381 + k_))
    for k_ in range(4):
        b.shard("SealShard", seal_shard_base(k_), seal_shard_dir(k_), 0.05, 0.26, sides=5, twist=0.6, flute=0.3,
                bone=f"Seal_{k_ + 1}", style=Y.shard_style("overcharge_w", 1.3, seed=5390 + k_))
    return b


def seal_shard_out(k_):
    a = 2 * math.pi * k_ / 4 + math.pi / 4
    return Vector((math.cos(a), 0, math.sin(a)))


def seal_shard_base(k_):
    return SEAL_C + seal_shard_out(k_) * 0.24 + Vector((0, -0.1, 0))


def seal_shard_dir(k_):
    return seal_shard_out(k_) + Vector((0, -0.6, 0))


def gate_bones():
    out = [("Root", Vector((0, 0, 0)), Vector((0, 0, 0.4)), None, True),
           ("Seal", SEAL_C, SEAL_C + Vector((0, -0.35, 0)), "Root", True)]
    for k_ in range(4):
        h = seal_shard_base(k_)
        out.append((f"Seal_{k_ + 1}", h, h + seal_shard_dir(k_).normalized() * 0.2, "Root", True))
    for i in range(PETALS):
        h, inward = petal_pivot(i)
        out.append((f"Petal_{i + 1}", h, h + inward * 0.4, "Root", True))
    return out


PETAL_NAMES = [f"Petal_{i + 1}" for i in range(PETALS)]
GATE_SCALE = ["Seal"] + [f"Seal_{k_ + 1}" for k_ in range(4)] + PETAL_NAMES
YAXIS = Vector((0, 1, 0))


def gate_specs(withdraw=None, unlock=0.0, jitter=0.0, seal=1.0, shards=0.0, shard_scale=1.0, f=0.0):
    """`withdraw[i]` 0..1 (1 = petal i gone into the stone), `unlock` 0..1 (the seams part at first light), `jitter`
    rumble in radians, `seal` the knot's scale, `shards` how far the four seal shards have drawn into the knot (0..1;
    round 53: they no longer fly, nothing is left in the air)."""
    sp = {"Root": {"rot": []}, "Seal": {"rot": [], "scale": max(seal, 0.001)}}
    for k_ in range(4):
        into = (SEAL_C - seal_shard_base(k_)) * shards
        sp[f"Seal_{k_ + 1}"] = {"rot": [], "loc": into, "scale": max(shard_scale, 0.001)}
    for i in range(PETALS):
        h, inward = petal_pivot(i)
        k_ = (withdraw or [0.0] * PETALS)[i]
        # The petal folds flat along its rim's normal (bone Y) into the slot, one solid piece whose leading edge runs
        # back to the rim; it never shrinks in the air. Only once it lies flat inside the stone does it go to nothing.
        # `unlock` no longer parts the seams (daylight through them read as grey strips); first light is the rim glow.
        fold = 1 - 0.985 * k_
        across = 1 - 0.999 * ss(0.97, 1.0, k_)
        loc = Vector()
        ang = jitter * math.sin(f * 1.9 + i * 1.3)
        sp[f"Petal_{i + 1}"] = {"rot": [(YAXIS, ang)], "loc": loc,
                                "scale": (max(across, 0.001), max(fold * across, 0.001), max(across, 0.001))}
    return sp


def gate_closed(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return gate_specs(seal=1.0 + 0.04 * math.sin(w), shards=0.01 * (0.5 + 0.5 * math.sin(w + 1)))
    return IDLE_FRAMES, spec, True


def gate_open(poser):
    """Rumble (0-30, the knot swells), the seal cracks at SEAL_CRACK (the knot collapses and draws its shards in), the
    seams part at FIRST_LIGHT, then the petals fold into the stone one after another from SWING_START, the last gone
    at FULLY_OPEN."""
    def spec(t, f):
        rumble = ease(0, SEAL_CRACK, f) * (1 - ease(SEAL_CRACK + 4, FIRST_LIGHT + 8, f))
        seal = (1.0 + 0.22 * ease(4, SEAL_CRACK, f)) * (1 - ease(SEAL_CRACK, SEAL_CRACK + 8, f))
        shards = ease(SEAL_CRACK, SEAL_CRACK + 8, f)
        unlock = ease(FIRST_LIGHT, FIRST_LIGHT + 8, f)
        dur = FULLY_OPEN - SWING_START - 9 * (PETALS - 1)
        # Ease-out: each petal folds most of the way in quickly and seats slowly, so no half-folded petal lingers.
        withdraw = [1 - (1 - min(max((f - SWING_START - 9 * SWING_ORDER.index(i)) / dur, 0.0), 1.0)) ** 2.2
                    for i in range(PETALS)]
        return gate_specs(withdraw=withdraw, unlock=unlock, jitter=R(0.7) * rumble, seal=seal, shards=shards,
                          shard_scale=1 - ease(SEAL_CRACK, SEAL_CRACK + 8, f), f=f)
    return OPEN_FRAMES, spec, False


def gate_opened(poser):
    def spec(t, f):
        return gate_specs(withdraw=[1.0] * PETALS, unlock=1.0, seal=0.0, shards=1.0, shard_scale=0.0)
    return IDLE_FRAMES, spec, True


# -------------------------------------------------------------------------------------------------------------
# Build
# -------------------------------------------------------------------------------------------------------------

def separate_fuse(builder):
    """C.fuse for this build only: the parts of each bone are remeshed on their own and joined (the iris petals and
    the seal pieces stay islands of their own, never welded to the stone or to each other)."""
    import bmesh
    plain = C.fuse
    state = {"root_verts": None}
    owner = np.array([builder.parts[p].chain[0] if builder.parts[p].chain else "Root" for p in builder.face_pid])
    faces_total = len(builder.face_pid)

    def split(obj, keep):
        o2 = C.link(bpy.data.objects.new(obj.name + "_split", obj.data.copy()))
        bm = bmesh.new()
        bm.from_mesh(o2.data)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in np.nonzero(~keep)[0]], context="FACES")
        bm.to_mesh(o2.data)
        bm.free()
        return o2

    def fuse(obj, voxel, smooth_iterations=6):
        if len(obj.data.polygons) != faces_total:
            return plain(obj, voxel, smooth_iterations)
        verts, faces = [], []
        for bone in dict.fromkeys(owner):
            o2 = split(obj, owner == bone)
            me = plain(o2, voxel, smooth_iterations)
            data = o2.data
            bpy.data.objects.remove(o2)
            bpy.data.meshes.remove(data)
            co = np.empty(len(me.vertices) * 3)
            me.vertices.foreach_get("co", co)
            if bone == "Root":
                state["root_verts"] = len(me.vertices)
            base = sum(len(v) for v in verts)
            verts.append(co.reshape(-1, 3))
            faces += [[base + i for i in p.vertices] for p in me.polygons]
            bpy.data.meshes.remove(me)
        mesh = bpy.data.meshes.new(obj.name + "_fused")
        mesh.from_pydata(np.concatenate(verts).tolist(), [], faces)
        mesh.update()
        C.log("RuinGate: remeshed per bone:", len(set(owner)), "groups,", len(faces), "faces")
        return mesh

    plain_ao = C.bake_vertex_ao

    def ao(high, distance, samples=48):
        # The stone's occlusion without the closed iris: the petals fill the opening only until the gate opens, and
        # their shadow baked into the faces towards the opening made those read black once it stood open.
        n = state["root_verts"]
        if n is None or n >= len(high.data.vertices):
            return plain_ao(high, distance, samples)
        co = np.empty(len(high.data.vertices) * 3)
        high.data.vertices.foreach_get("co", co)
        moved = co.reshape(-1, 3).copy()
        moved[n:, 2] += 60.0
        high.data.vertices.foreach_set("co", moved.ravel())
        high.data.update()
        try:
            plain_ao(high, distance, samples)
        finally:
            high.data.vertices.foreach_set("co", co)
            high.data.update()
    plain_bake = C.bake_maps

    def shift(obj, ids, dz):
        co = np.empty(len(obj.data.vertices) * 3)
        obj.data.vertices.foreach_get("co", co)
        co = co.reshape(-1, 3)
        co[ids, 2] += dz
        obj.data.vertices.foreach_set("co", co.ravel())
        obj.data.update()

    def bake(high, low, *a, **kw):
        # The stone bakes without the iris and the seal (and they without the stone): where a petal's foot pierced the
        # plinth its outline was baked into the threshold as dark dashes. Every piece but the stone (the largest
        # island of the game mesh, everything past the stone's vertices in the source) is lifted out of the way, in
        # both meshes by the same amount, for the bake only.
        import check_floating_parts as F
        n = state["root_verts"]
        isl = F.islands(low.data)
        main = max(isl, key=len)
        low_ids = np.setdiff1d(np.arange(len(low.data.vertices)), main)
        high_ids = np.arange(n, len(high.data.vertices)) if n is not None else np.array([], dtype=int)
        shift(high, high_ids, 60.0)
        shift(low, low_ids, 60.0)
        try:
            return plain_bake(high, low, *a, **kw)
        finally:
            shift(high, high_ids, -60.0)
            shift(low, low_ids, -60.0)

    plain_skin = C.skin

    def skin(mesh_obj, arm, builder_, *a, **kw):
        # Each island wholly on the bone most of it follows: a petal's foot sits inside the plinth and its rim inside
        # the jambs, where the nearest source surface is the stone, so those vertices took Root and stayed behind as
        # slivers when the petal withdrew.
        import build_bulwark as BW
        import check_floating_parts as F
        BW.drop_specks(mesh_obj, min_verts=8)  # single triangles the decimation leaves near the crystal tips
        out = plain_skin(mesh_obj, arm, builder_, *a, **kw)
        names = {g.index: g.name for g in mesh_obj.vertex_groups}
        fixed = 0
        for isl in F.islands(mesh_obj.data):
            votes = {}
            for i in isl:
                v = mesh_obj.data.vertices[int(i)]
                if v.groups:
                    best = max(v.groups, key=lambda g: g.weight)
                    votes[names[best.group]] = votes.get(names[best.group], 0) + 1
            if not votes:
                continue
            lead = max(votes, key=votes.get)
            ids = [int(i) for i in isl]
            for vg in mesh_obj.vertex_groups:
                vg.remove(ids)
            mesh_obj.vertex_groups[lead].add(ids, 1.0, "REPLACE")
            fixed += 1
        C.log("RuinGate: islands made pure:", fixed)
        return out
    return plain, fuse, plain_ao, ao, plain_skin, skin, plain_bake, bake


def build_gate():
    rigid = {"Arch", "Crest", "Horn", "Buttress", "Crystal", "CrystalFragment", "CrystalRoot", "Plinth", "Petal",
             "SealCore", "SealSpike", "SealShard"}
    clips = {"Closed_Idle": gate_closed, "Open": gate_open, "Opened_Idle": gate_opened}
    report = {
        "pivot": "centre of the threshold on the ground; the gate faces -Y in Blender (Unity +Z after the FBX axis "
                 "conversion): the giant walks out towards -Y",
        "opening_m": {"width": HALF_W * 2, "height": round(APEX_Z, 3), "note": "The Shattered (3.4 x 5.3 x 3.6) fits"},
        "unity_scale": 1.0,
        "clips_info": {
            "Closed_Idle": {"frames": IDLE_FRAMES, "loop": True, "note": "dormant: the seal knot breathes"},
            "Open": {"frames": OPEN_FRAMES, "loop": False, "seal_crack_frame": SEAL_CRACK, "first_light_frame": FIRST_LIGHT,
                     "swing_start_frame": SWING_START, "fully_open_frame": FULLY_OPEN,
                     "note": "the first and only opening; rumble 0-30 (camera shake, dust off the arch), crack: "
                             "the knot flares and collapses, drawing its four shards in (flash + shard burst VFX at "
                             "Seal), first light: the seams part (raise the emission and a point light inside), the "
                             "petals fold into slots in the arch from swing_start, bottom first (round 53: each folds "
                             "flat along its rim's normal, as one piece, and goes to nothing inside the stone); spawn the giant in the flash at first_light or when fully open"},
            "Opened_Idle": {"frames": IDLE_FRAMES, "loop": True, "note": "stays open for the rest of the level; the "
                                                                          "opening is clear"}},
        "emission": "magenta crack lines, worn glyph bands, the soffit slot, petal rims and sigil, plinth sigil, "
                    "crystals; suggested _EmissionColor strength 0.6 while closed (dormant), ramp to 2.5 from "
                    "first_light to fully_open, then 1.6",
        "round53": "rebuilt as one band of stone on a plinth (build_ruingate53.py); same bones, clips and moments; the "
                   "petal bones are scale-keyed (withdraw into the stone)",
    }
    builder = gate_parts()
    plain, fuse, plain_ao, ao, plain_skin, skin, plain_bake, bake = separate_fuse(builder)
    C.fuse, C.bake_vertex_ao, C.skin, C.bake_maps = fuse, ao, skin, bake
    try:
        asset, arm, actions, poser = M.small_rig(GATE, builder, gate_bones(), clips, 6400, families=("rock", "crystal"),
                                                 rigid=rigid, blend=0, scale_bones=GATE_SCALE, voxel=0.013, ao=0.3,
                                                 extrusion=0.04, ray=0.12, emission=2.6, specular=0.3, k=3.0,
                                                 mirror=False, tex=1024, low=512, report=report, symmetric=False)
    finally:
        C.fuse, C.bake_vertex_ao, C.skin, C.bake_maps = plain, plain_ao, plain_skin, plain_bake
    return asset, arm, actions


def blockout():
    """Raw parts in flat colours (no bake, no rig) and three quick renders, to judge the shape in a minute."""
    import r49_views as V
    C.reset_scene()
    b = gate_parts()
    obj = b.to_object("RuinGate_Block")
    mat = bpy.data.materials.new("Block")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.3, 0.31, 0.34, 1)
    obj.data.materials.append(mat)
    V.bright_scene(8.0, 0.0)
    out = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
    for name, cam in (("game", C.game_camera(Vector((0, 0, 2.4)), 11.0)),
                      ("front", C.closeup_camera(Vector((0, 0, 2.6)), 15, azimuth=-25, elevation=12, lens=50))):
        C.render(cam, os.path.join(out, f"r53_ruingate_block_{name}.png"), 960, 640, samples=16)
        bpy.data.objects.remove(cam)


def main():
    args = C.cli_args()
    if args.get("blockout"):
        blockout()
        return
    build_gate()


if __name__ == "__main__":
    main()
