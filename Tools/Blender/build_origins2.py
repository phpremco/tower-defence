"""Round 23: living spawn origins (see ArtSource/DESIGN_NOTES.md, round 23).

Every enemy spawn origin is a rigged prop with five clips. It starts hidden, can be marked (the player sees where
creatures will come from while the ground is still closed), opens once with a burst a few seconds before its first
creature, and then lives as an open maw that convulses when a creature climbs out.

- Origin2_Burrow    the land burrow (about 2 cells of maw, ~3.4 with the thrown slabs): 13 ground plates whose tops
                    are painted with the continent's ground set (camo), a ridged chitin maw with hooked teeth, two
                    claws and four tendrils stored under the ground. One mesh, one map set per ground set
                    (`Skins/<Set>/`), the Emission is baked warm (not a mask).
- Origin2_Water / _Lava / _Quicksand / _Ice / _Bog
                    the liquid ones (about 1-1.3 cells), sunk below the liquid surface when closed; their Emission is
                    a white mask (Unity tints it with NestColor(element)), like round 14.

Pivot: the cell centre on the ground (burrow) or ON the liquid surface (TerrainLiquidBuilder.SurfaceLevel(kind)).
Bind pose = Closed: the maw, teeth, tendrils, chunks and markers wait under the ground (z = -D), so a renderer that
plays no clip still shows nothing but flush camo plates (burrow) or nothing at all (liquids).

Clips (30 fps): Closed (1), Marked (90, loop), Open (96, one-shot, starts from Marked frame 0, burst at frame 54),
Idle (105, loop, starts where Open ends), Spawn (30, starts and ends on Idle frame 0).
Sockets (non-deforming bones): Emerge, Glow, Dust, Debris_1..n.

Run: blender --background --factory-startup --python Tools/Blender/build_origins2.py -- [--preview <dir>]
     [--norender] [--only Burrow,Water,Lava,Quicksand,Ice,Bog]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import deep_kit as DK  # noqa: E402
import env_kit as E  # noqa: E402
import tower_kit as tk  # noqa: E402

FPS = 30
OPEN_FRAMES = 96
BURST = 54          # the frame the ground bursts (VFX Dust/Debris + sound)
TREMOR_END = 30
SURGE_END = 66      # maw fully up (overshoot), teeth begin to unfurl
IDLE_FRAMES = 105
MARKED_FRAMES = 90
SPAWN_FRAMES = 30
SPAWN_PEAK = 6
TILE = 3.0          # world size of one ground tile (all continents' ground sets)

# The ground sets a burrow skin is painted from (continent: tiling set folder relative to Exports/Env).
GROUND_SETS = {"CrystalPlains": "Tiling/Ground", "AshenReach": "AshenReach/Tiling/Ash_Ground",
               "NorthernCrown": "NorthernCrown/Tiling/North_Ground", "Mirewood": "Mirewood/Tiling/Mire_Ground",
               "SouthernCrown": "SouthernCrown/Tiling/South_Ground", "TheCore": "TheCore/Tiling/Core_Ground"}

# Preview tints of the white masks (LevelVisualBuilder.NestColor) and liquid sheets (colour, alpha).
TINT = {"Water": (0.3, 0.75, 1.0), "Lava": (1.0, 0.45, 0.12), "Quicksand": (0.95, 0.8, 0.4),
        "Ice": (0.8, 0.95, 1.0), "Bog": (0.55, 0.8, 0.3)}
LIQUID = {"Water": ((0.1, 0.4, 0.55), 0.68), "Lava": ((1.0, 0.45, 0.1), 0.95), "Quicksand": ((0.62, 0.5, 0.3), 0.97),
          "Ice": ((0.75, 0.9, 1.0), 0.9), "Bog": ((0.18, 0.24, 0.12), 0.92)}

# Warm alien light of the land burrow (baked; aliens are warm-coloured).
HOT = np.array((1.0, 0.52, 0.16))
VEIN = np.array((1.0, 0.3, 0.38))

OXBLOOD = dict(top=np.array((0.4, 0.12, 0.09)), side=np.array((0.2, 0.055, 0.05)), bottom=np.array((0.07, 0.02, 0.03)))
FLESH = dict(top=np.array((0.55, 0.2, 0.16)), side=np.array((0.3, 0.08, 0.08)), bottom=np.array((0.1, 0.03, 0.04)))
BONE = (np.array((0.32, 0.12, 0.08)), np.array((0.9, 0.8, 0.62)))
UNDER = dict(top=np.array((0.2, 0.15, 0.17)), side=np.array((0.13, 0.1, 0.12)), bottom=np.array((0.06, 0.05, 0.06)),
             seam=np.array((0.05, 0.03, 0.04)))
SAND = dict(top=np.array((0.66, 0.54, 0.34)), side=np.array((0.45, 0.35, 0.22)), bottom=np.array((0.16, 0.12, 0.08)),
            seam=np.array((0.2, 0.14, 0.08)))
ICEP = dict(top=np.array((0.8, 0.88, 0.94)), side=np.array((0.55, 0.66, 0.78)), bottom=np.array((0.24, 0.3, 0.4)),
            seam=np.array((0.3, 0.35, 0.45)))
FROST = dict(top=np.array((0.62, 0.66, 0.74)), side=np.array((0.32, 0.3, 0.4)), bottom=np.array((0.1, 0.08, 0.12)))
MIRE = dict(top=np.array((0.3, 0.26, 0.15)), side=np.array((0.16, 0.13, 0.08)), bottom=np.array((0.05, 0.05, 0.03)))
ROT = dict(top=np.array((0.5, 0.28, 0.22)), side=np.array((0.28, 0.13, 0.12)), bottom=np.array((0.08, 0.04, 0.04)))
WETFLESH = dict(top=np.array((0.52, 0.28, 0.3)), side=np.array((0.3, 0.13, 0.17)), bottom=np.array((0.08, 0.03, 0.05)))

V = Vector


def radial(a):
    return V((math.cos(a), math.sin(a), 0.0))


def tangent(a):
    return V((-math.sin(a), math.cos(a), 0.0))


def ss(e0, e1, x):
    return C.smoothstep(e0, e1, x)


def ease(e0, e1, x):
    return float(C.smoothstep(e0, e1, np.array([x]))[0])


def rnd(seed):
    return np.random.default_rng(seed)


# ---------------------------------------------------------------------------------------------------------------
# Glow: baked warm colour (burrow) or a white mask (liquids, tinted in Unity)
# ---------------------------------------------------------------------------------------------------------------

class Glow:
    colour = None  # None = white mask

    @classmethod
    def write(cls, s, idx, mask, col=HOT):
        m = np.clip(mask, 0, 1)
        if cls.colour is None:
            s.emit[idx] = np.repeat(m[:, None], 3, axis=1)
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.5, 0.46, 0.44)), m * 0.5)
        else:
            s.emit[idx] = np.clip(np.asarray(col)[None, :] * m[:, None], 0, 1)
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.asarray(col) * 0.6, m * 0.45)
        s.rough[idx] = np.clip(s.rough[idx] - 0.2 * m, 0.1, 1)


# ---------------------------------------------------------------------------------------------------------------
# Ground set sampling (camo of the burrow's plate tops)
# ---------------------------------------------------------------------------------------------------------------

class GroundSet:
    def __init__(self, rel):
        base = os.path.join(E.ENV, rel)
        name = os.path.basename(rel)
        self.name = name
        self.maps = {k: C.image_to_array(os.path.join(base, f"{name}_{k}.png")) for k in ("Albedo", "Normal", "Mask")}

    def sample(self, key, x, y):
        img = self.maps[key]
        h, w = img.shape[:2]
        u = (np.asarray(x) / TILE) % 1.0 * w - 0.5
        v = (np.asarray(y) / TILE) % 1.0 * h - 0.5
        x0 = np.floor(u).astype(int)
        y0 = np.floor(v).astype(int)
        fx = (u - x0)[..., None]
        fy = (v - y0)[..., None]
        x0 %= w
        y0 %= h
        x1 = (x0 + 1) % w
        y1 = (y0 + 1) % h
        a = img[y0, x0] * (1 - fx) + img[y0, x1] * fx
        b = img[y1, x0] * (1 - fx) + img[y1, x1] * fx
        return a * (1 - fy) + b * fy


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def plate_style(top_fn, under, seed, z_top, glow_veins=0.55, hairline=0.35, crack=0.5, bevel_dark=0.4):
    """A plate: its top from `top_fn(p) -> sRGB` (the ground set for a burrow, crust/ice/sand for liquids), flat
    (no displacement on the top, so a closed plate sits flush); the bevel round the top edge is a dark hairline with
    a faint glow (the Marked rings and star); the sides glow just under the top edge (the cracks of the tremor) and
    the underside is root-veined rock with warm light (seen when a plate flips)."""
    rock = E.stone(under, seed, crack=0.6, dust=0.0, strata=18.0, rough=0.85)

    def style(s, idx, part):
        rock(s, idx, part)
        p = s.p[idx]
        n = s.n[idx]
        zr = p[:, 2] - z_top
        top = ss(0.93, 0.985, n[:, 2]) * ss(-0.02, -0.004, zr)
        bevel = ss(0.45, 0.8, n[:, 2]) * (1 - ss(0.93, 0.985, n[:, 2])) * ss(-0.03, -0.006, zr)
        side = (1 - ss(0.35, 0.7, np.abs(n[:, 2]))) * ss(-0.05, -0.03, zr) * ss(0.0, -0.012, zr)
        below = ss(-0.2, -0.7, n[:, 2])
        vein = (1 - ss(0.0, 0.06, np.abs(C.value_noise(p, 7, seed + 3)))) * ss(-0.2, 0.3, C.value_noise(p, 2, seed + 4))
        col = C.lerp3(s.albedo[idx], np.asarray(top_fn(p)), top)
        col = C.lerp3(col, np.array((0.05, 0.03, 0.03)), bevel * bevel_dark)
        s.albedo[idx] = col
        s.disp[idx] = s.disp[idx] * (1 - np.maximum(top, bevel))
        s.rough[idx] = s.rough[idx] * (1 - top) + 0.82 * top
        s.fam[idx] = E.FAM_ROCK
        mask = np.maximum.reduce([bevel * hairline, side * crack, below * vein * glow_veins])
        Glow.write(s, idx, mask, VEIN)
        # the glow write pushes albedo: keep the camo top untouched
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.asarray(top_fn(p)), top * (1 - bevel))
    return style


def ground_top(ground):
    def fn(p):
        return ground.sample("Albedo", p[:, 0], p[:, 1])[:, :3]
    return fn


def flat_top(pal, seed, ripple=0.0, scale=9.0):
    def fn(p):
        col = np.broadcast_to(pal["top"], (len(p), 3)).copy()
        col *= (0.86 + 0.24 * C.value_noise(p, scale, seed))[:, None]
        if ripple:
            r = np.hypot(p[:, 0], p[:, 1])
            col *= (0.9 + ripple * np.sin(r * 60 + 2 * C.value_noise(p, 4, seed + 1)))[:, None]
        return col
    return fn


SOIL = np.array((0.13, 0.11, 0.12))


def flank_rib(a, n):
    """The raised radial ribs of the maw's outer flank (geometry and paint use the same function)."""
    if not n:
        return 0.0 * a
    return (0.5 + 0.5 * np.cos(np.asarray(a) * n + 0.35 * np.sin(np.asarray(a) * 3))) ** 4


def maw_style(pal, seed, zo, gullet, crest, foot, flank_ridges=13, glow_depth=1.0, pores=0.55, fam=E.FAM_CHITIN,
              col=HOT, soil=SOIL, fissure=0.0):
    """The funnel: radial chitin ribs on the outer flank with glowing pores, concentric throat rings on the inner
    wall that glow brighter with depth, and a white-hot gullet."""
    rng = rnd(seed)
    pa = rng.uniform(0, 2 * math.pi, 46)
    pr = rng.uniform(crest * 1.15, foot * 0.85, 46)

    def style(s, idx, part):
        p = s.p[idx].copy()
        p[:, 2] -= zo
        n = s.n[idx]
        r = np.hypot(p[:, 0], p[:, 1])
        a = np.arctan2(p[:, 1], p[:, 0])
        t_flank = np.clip((r - crest) / max(foot - crest, 1e-3), 0, 1)
        rib = flank_rib(a, flank_ridges) * ss(crest * 0.95, crest * 1.2, r)
        groove = (1 - flank_rib(a, flank_ridges)) * ss(crest * 1.05, crest * 1.3, r) if flank_ridges else 0 * r
        ring = (0.5 + 0.5 * np.sin(r * (60 / max(crest, 0.1)) + C.value_noise(p, 6, seed + 1))) * ss(crest, gullet, r)
        colr = C.chitin_gradient(n, pal["top"], pal["side"], pal["bottom"])
        plate = 0.5 + 0.5 * C.value_noise(p, 9, seed + 4)
        colr = colr * (0.55 + 0.6 * rib + 0.15 * plate)[:, None] * (1 + 0.15 * C.value_noise(p, 24, seed + 2))[:, None]
        colr = C.lerp3(colr, np.array((0.75, 0.6, 0.48)), rib * ss(0.6, 0.95, n[:, 2]) * 0.3)
        colr = C.lerp3(colr, pal["bottom"], ss(crest * 0.9, gullet, r) * 0.6)
        # The foot is crusted with the soil it pushed up, so the maw sits in the ground instead of on it.
        colr = C.lerp3(colr, soil * (0.8 + 0.4 * plate)[:, None], ss(0.55, 1.0, t_flank) * 0.85)
        s.albedo[idx] = colr
        s.rough[idx] = 0.55 - 0.2 * rib + 0.3 * ss(0.55, 1.0, t_flank)
        s.disp[idx] = 0.006 * rib - 0.004 * ring + 0.003 * C.value_noise(p, 50, seed + 3) - 0.004 * groove
        s.fam[idx] = fam
        s.flow[idx] = r
        inner = ss(crest * 0.85, gullet * 1.1, r) * (0.55 + 0.45 * ring) * glow_depth
        core = ss(gullet * 1.05, gullet * 0.6, r)
        pm = np.zeros(len(idx))
        for aa, rr in zip(pa, pr):
            d = np.hypot(p[:, 0] - math.cos(aa) * rr, p[:, 1] - math.sin(aa) * rr)
            pm = np.maximum(pm, 1 - ss(0.006, 0.02 * (foot / 0.95), d))
        seam = groove * (1 - ss(0.0, 0.6, t_flank)) * 0.45
        crack = np.zeros(len(idx))
        if fissure:
            # Molten cracks through the chilled crust of the flank (lava).
            line = 1 - ss(0.0, 0.05, np.abs(C.value_noise(p, 11, seed + 8)))
            crack = line * ss(-0.25, 0.25, C.value_noise(p, 3.5, seed + 9)) * ss(crest * 0.9, crest * 1.2, r) * fissure
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.05, 0.03, 0.03)), crack * 0.5)
            s.disp[idx] = s.disp[idx] - 0.006 * crack
        mask = np.maximum.reduce([inner * 0.8, core, pm * pores * (0.4 + 0.6 * groove + 0.3 * (1 - rib)), seam, crack])
        Glow.write(s, idx, mask, col)
    return style


def rim_style(pal, seed, segments=11, joint_glow=0.75):
    """The ridged collar: segmented chitin with worn pale ridges, glowing joints between the segments."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        a = np.arctan2(p[:, 1], p[:, 0])
        seg = np.abs(((a / (2 * math.pi) * segments + 0.13 * C.value_noise(p, 4, seed)) % 1.0) - 0.5) * 2
        joint = 1 - ss(0.0, 0.08, 1 - seg)
        col = C.chitin_gradient(n, pal["top"], pal["side"], pal["bottom"])
        worn = ss(0.6, 0.95, n[:, 2]) * ss(0.3, 0.8, seg)
        col = C.lerp3(col, np.array((0.78, 0.62, 0.5)), worn * 0.45)
        s.albedo[idx] = col * (1 + 0.1 * C.value_noise(p, 30, seed + 1))[:, None]
        s.rough[idx] = 0.45
        s.disp[idx] = -0.006 * joint + 0.002 * C.value_noise(p, 60, seed + 2)
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = a
        Glow.write(s, idx, joint * joint_glow, VEIN)
    return style


def horn_style(root, tip, seed, tip_glow=0.6, nodes=0, node_glow=0.7, fam=E.FAM_CHITIN, col=HOT):
    """Teeth, claws, ribs and tendrils: dark root to pale tip, growth bands, glowing tip; `nodes` glowing beads."""
    root, tip = np.asarray(root), np.asarray(tip)

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        colr = C.lerp3(root, tip, ss(0.25, 0.95, t))
        colr = colr * (0.62 + 0.38 * ss(-0.6, 0.7, n[:, 2]))[:, None]
        band = 1 - ss(0.0, 0.1, np.abs(((t * 7) % 1.0) - 0.5) - 0.36)
        s.albedo[idx] = C.lerp3(colr, root * 0.4, band * 0.4)
        s.rough[idx] = 0.42
        s.disp[idx] = -0.003 * band
        s.fam[idx] = fam
        s.flow[idx] = t * C.axis_length(part)
        mask = ss(0.78, 1.0, t) * tip_glow
        if nodes:
            bead = 1 - ss(0.0, 0.05, np.abs(((t * nodes) % 1.0) - 0.5))
            mask = np.maximum(mask, bead * node_glow * ss(0.1, 0.3, t))
        Glow.write(s, idx, mask, col)
    return style


def chunk_style(pal, seed, glow=0.5, col=VEIN):
    rock = E.stone(pal, seed, crack=0.8, dust=0.2, strata=30.0)

    def style(s, idx, part):
        rock(s, idx, part)
        p = s.p[idx]
        line = (1 - ss(0.0, 0.08, np.abs(C.value_noise(p, 16, seed + 1)))) * glow
        s.fam[idx] = E.FAM_ROCK
        Glow.write(s, idx, line, col)
    return style


def gullet_style(seed, col=HOT):
    def style(s, idx, part):
        p = s.p[idx]
        r = np.hypot(p[:, 0], p[:, 1])
        s.albedo[idx] = np.array((0.6, 0.3, 0.2))
        s.rough[idx] = 0.3
        s.disp[idx] = 0.003 * C.value_noise(p, 40, seed)
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = r
        vein = 1 - ss(0.0, 0.12, np.abs(C.value_noise(p, 25, seed + 1)))
        Glow.write(s, idx, 0.75 + 0.25 * vein, col)
    return style


def marker_style(pal, seed):
    def style(s, idx, part):
        n = s.n[idx]
        s.albedo[idx] = C.chitin_gradient(n, pal["top"], pal["side"], pal["bottom"])
        s.rough[idx] = 0.4
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = s.p[idx][:, 2]
        Glow.write(s, idx, 0.35 + 0.65 * ss(0.2, 0.9, n[:, 2]), HOT)
    return style


# ---------------------------------------------------------------------------------------------------------------
# The rig description: bones plus the animation roles of each group
# ---------------------------------------------------------------------------------------------------------------

class Origin:
    def __init__(self, kind, S, D, sink=0.0):
        self.kind = kind
        self.name = "Origin2_" + kind
        self.S = S          # size factor of positions (1 = burrow)
        self.D = D          # depth the maw waits at in the bind pose
        self.sink = sink    # liquids: plates wait this far under the surface
        self.b = C.PartBuilder()
        self.bones = [("Root", V((0, 0, 0)), V((0, 0.15 * S, 0)), None, True)]
        self.outer, self.inner, self.rims, self.teeth, self.tendrils = [], [], [], [], []
        self.chunks, self.markers, self.sockets = [], [], []
        self.rigid = set()
        self.spin = 0.0
        self.plates_marked = False   # liquids: plates float on the surface while Marked (lava crust, ice floes)
        self.outer_mode = "flip"
        self.maw = None

    def bone(self, name, head, tail, parent="Root", deform=True):
        self.bones.append((name, V(head), V(tail), parent, deform))
        return name

    def socket(self, name, head, parent):
        self.bone(name, head, V(head) + V((0, 0, 0.06 * self.S)), parent, deform=False)
        self.sockets.append((name, parent))


def wobble(R, seed, amp=0.045):
    rng = rnd(seed)
    ph = rng.uniform(0, 6.3, 3)

    def fn(a):
        return R * (1 + amp * math.sin(3 * a + ph[0]) + amp * 0.6 * math.sin(7 * a + ph[1]) + amp * 0.4 * math.sin(13 * a + ph[2]))
    return fn


def wedge(b, name, bone, a0, a1, r0f, r1f, z_top, thick, gap, style, nu=18, nv=8):
    """A polar plate between angles a0..a1 and radii r0f(a)..r1f(a), top flat at z_top; the side gaps keep a
    constant width so the remesh keeps neighbours apart."""
    def at(u, v, z):
        am = a0 + (a1 - a0) * u
        r = r0f(am) + (r1f(am) - r0f(am)) * v
        half = gap / (2 * max(r, 0.03))
        a = a0 + half + (a1 - a0 - 2 * half) * u
        r = r0f(a) + (r1f(a) - r0f(a)) * v
        return V((math.cos(a) * r, math.sin(a) * r, z))
    return b.slab(name, lambda u, v: at(u, v, z_top), lambda u, v: at(u, v, z_top - thick), nu, nv, bone=bone, style=style)


def polar_slab(b, name, z_top, radius, z_bottom, style, bone=None, chain=None, nu=96, nv=34):
    def top(u, v):
        a = 2 * math.pi * u
        r = radius * v
        return V((r * math.cos(a), r * math.sin(a), z_top(r, a)))

    def bottom(u, v):
        a = 2 * math.pi * u
        r = radius * v
        return V((r * math.cos(a), r * math.sin(a), z_bottom))
    return b.slab(name, top, bottom, nu, nv, bone=bone, chain=chain, style=style)


def add_plates(o, inner_n, outer_n, r_in, r_mid, r_out, z_top, thick, gap, top_fn, under, seed,
               finals=(150, 170), glow_veins=0.7, hairline=0.35, throw=True, bevel_dark=0.4):
    """The plate disc: `inner_n` wedges round the centre (thrown at the burst) and `outer_n` ring segments hinged
    at their outer edge (flip outward / stand up / slump, by o.outer_mode)."""
    S = o.S
    rng = rnd(seed)
    mid = wobble(r_mid, seed + 1, 0.06)
    out = wobble(r_out, seed + 2, 0.05)
    style = plate_style(top_fn, under, seed + 3, z_top, glow_veins, hairline, bevel_dark=bevel_dark)
    off_in = rng.uniform(0, 1)
    for i in range(inner_n):
        a0 = (i + off_in) / inner_n * 2 * math.pi
        a1 = (i + 1 + off_in) / inner_n * 2 * math.pi
        bn = f"Shard_{i + 1}"
        am = (a0 + a1) / 2
        rc = (r_in + mid(am)) * 0.55
        head = radial(am) * rc + V((0, 0, z_top - thick / 2))
        o.bone(bn, head, head + radial(am) * 0.1 * S)
        wedge(o.b, "Plate", bn, a0, a1, lambda a: r_in, mid, z_top, thick, gap, style)
        dist = rng.uniform(1.5, 1.75) * (r_out / 1.0) - rc
        o.inner.append(dict(bone=bn, a=am, head=head, axis=tangent(am), dist=dist, vz=rng.uniform(3.0, 3.6) * math.sqrt(S),
                            final=math.pi + rng.uniform(-0.35, 0.35), floor=thick * 0.55, phase=rng.uniform(0, 6.3),
                            spin=rng.uniform(-0.6, 0.6), throw=throw))
    off_out = rng.uniform(0, 1)
    for i in range(outer_n):
        a0 = (i + off_out) / outer_n * 2 * math.pi
        a1 = (i + 1 + off_out) / outer_n * 2 * math.pi
        am = (a0 + a1) / 2
        bn = f"Plate_{i + 1}"
        head = radial(am) * out(am) + V((0, 0, z_top - thick * 0.6))
        o.bone(bn, head, head - radial(am) * 0.1 * S)
        wedge(o.b, "Plate", bn, a0, a1, lambda a, m=mid: m(a) + gap, out, z_top, thick, gap, style)
        o.outer.append(dict(bone=bn, a=am, head=head, axis=tangent(am), final=math.radians(rng.uniform(*finals)),
                            phase=rng.uniform(0, 6.3), delay=rng.uniform(0, 4)))
    o.rigid.add("Plate")


def add_maw(o, pal, seed, gullet, crest, zc, zg, foot, zf=-0.03, rim_tube=0.055, n_rims=8, flank_ridges=13,
            glow_depth=1.0, pores=0.55, fam=E.FAM_CHITIN, rim=True, rim_pal=None, rim_glow=0.45, gullet_fn=None,
            rib_h=None, soil=SOIL, fissure=0.0):
    """The maw in its open layout, stored D under the ground. Profile radii and heights are in units (already
    scaled), z relative to the open state."""
    S, zo = o.S, -o.D
    rib_h = 0.045 * S if rib_h is None else rib_h
    o.maw = dict(gullet=gullet, crest=crest, zc=zc, zg=zg, foot=foot)
    o.bone("Maw", (0, 0, zo), (0, 0, zo + 0.12 * S))
    rims = []
    for i in range(n_rims):
        a = i / n_rims * 2 * math.pi
        h = radial(a) * crest + V((0, 0, zc + zo))
        rims.append(o.bone(f"Rim_{i + 1}", h, h + radial(a) * 0.08 * S, "Maw"))
        o.rims.append(dict(bone=rims[-1], a=a))
    o.bone("Throat", (0, 0, zg + zo), (0, 0, zg + zo + 0.06 * S), "Maw")

    def z(r, a):
        if r < gullet:
            return zg - 0.01 * S * (1 - (r / gullet) ** 2)
        if r < crest:
            t = (r - gullet) / (crest - gullet)
            return zg + (zc - zg) * float(ss(0.0, 1.0, np.array(t))) ** 0.75
        t = (r - crest) / (foot - crest)
        lump = 0.025 * S * math.sin(5 * a + 0.8) * math.sin(math.pi * t)
        rib = rib_h * float(flank_rib(a, flank_ridges)) * math.sin(math.pi * min(1.0, t * 1.15)) ** 0.6
        return zc + (zf - zc) * float(ss(0.0, 1.0, np.array(t))) ** 0.9 + lump + rib

    polar_slab(o.b, "Funnel", lambda r, a: z(r, a) + zo, foot, zo + zf - 0.1 * S,
               maw_style(pal, seed, zo, gullet, crest, foot, flank_ridges, glow_depth, pores, fam, soil=soil, fissure=fissure), chain=rims,
               nu=144, nv=40)
    if rim:
        o.b.ring("Rim", V((0, 0, zc + zo + rim_tube * 0.35)), V((0, 0, 1)), crest, rim_tube, arcs=4, sides=10,
                 chain=rims, style=rim_style(rim_pal or pal, seed + 5, joint_glow=rim_glow),
                 profile=lambda th: 1.0 + 0.22 * max(0.0, math.cos(th * 11)))
    o.b.ellipsoid("Gullet", V((0, 0, zg + zo + 0.006 * S)), (gullet * 0.95, gullet * 0.95, 0.022 * S), bone="Throat",
                  segments=(28, 10), style=gullet_fn or gullet_style(seed + 7))
    o.rigid.add("Gullet")
    o.socket("Emerge", (0, 0, zo - 0.12 * S), "Maw")
    o.socket("Glow", (0, 0, zg + zo + 0.08 * S), "Throat")
    o.socket("Dust", (0, 0, 0.02), "Root")


def nearest_rim(o, a):
    best = min(o.rims, key=lambda r: abs(math.atan2(math.sin(r["a"] - a), math.cos(r["a"] - a))))
    return best["bone"]


def add_teeth(o, n, base_r, base_z, profile, radii, style, seed, kind="Tooth", fold=-70.0, sides=9, offset=0.0,
              jitter=0.25, size_var=(0.8, 1.2)):
    """Hooked teeth on the inner crest, curving up and in. `profile`: [(dr, dz)] offsets from the base (units)."""
    rng = rnd(seed)
    zo = -o.D
    for i in range(n):
        a = (i + offset) / n * 2 * math.pi + rng.uniform(-jitter, jitter) * (2 * math.pi / n)
        k = rng.uniform(*size_var) * (1.25 if i % 3 == 0 else 1.0)
        er = radial(a)
        lean = tangent(a) * rng.uniform(-0.03, 0.03) * o.S
        pts = [er * (base_r + dr * k) + V((0, 0, base_z + dz * k + zo)) + lean * j for j, (dr, dz) in enumerate(profile)]
        bn = f"{kind}_{i + 1}"
        o.bone(bn, pts[0], pts[1], nearest_rim(o, a))
        o.b.tube(kind, pts, [r * k for r in radii], sides=sides, chain=[bn], style=style)
        o.teeth.append(dict(bone=bn, a=a, axis=tangent(a), fold=math.radians(fold * rng.uniform(0.85, 1.1)),
                            phase=rng.uniform(0, 6.3), delay=rng.uniform(0, 5)))
    o.rigid.add(kind)


def add_tendrils(o, angles, points, radii, style, seed, curl=(-70, -45, -40)):
    """Tendrils from the flank outward over the ground, three chained bones each. `points`: [(r, z, lateral)]."""
    rng = rnd(seed)
    zo = -o.D
    for i, a in enumerate(angles):
        er, et = radial(a), tangent(a)
        side = 1 if i % 2 else -1
        pts = [er * r + et * (lat * side) + V((0, 0, z + zo)) for r, z, lat in points]
        names = []
        for j in range(3):
            names.append(o.bone(f"Tendril_{i + 1}_{j + 1}", pts[j], pts[j + 1], names[-1] if names else "Maw"))
        o.b.tube("Tendril", pts, radii, sides=9, chain=names, style=style)
        o.tendrils.append(dict(bones=names, a=a, axis=tangent(a), side=side, phase=rng.uniform(0, 6.3),
                               curl=[math.radians(c) for c in curl]))


def add_chunks(o, n, size, pal, seed, land=(1.15, 1.9), glow=0.5, col=VEIN, style=None):
    """Debris thrown at the burst: stored deep under the maw, launched from the centre, resting where they land."""
    rng = rnd(seed)
    S = o.S
    for i in range(n):
        a = rng.uniform(0, 2 * math.pi)
        rest = radial(a) * 0.3 * S + V((0, 0, -o.D - 0.35 * S))
        r = size * rng.uniform(0.7, 1.3)
        bn = f"Chunk_{i + 1}"
        o.bone(bn, rest, rest + V((0, 0, 0.06 * S)))
        rot = Matrix.Rotation(rng.uniform(0, 6.3), 3, "Z") @ Matrix.Rotation(rng.uniform(-0.6, 0.6), 3, "X")
        o.b.ellipsoid("Chunk", rest, (r * 1.25, r, r * 0.8), rot, bone=bn, segments=(14, 8),
                      deform=C.chiselled(9, seed + 10 + i, 0.3), style=style or chunk_style(pal, seed + 20 + i, glow, col))
        dist = rng.uniform(*land) * S
        launch = radial(a) * 0.08 * S + V((0, 0, -0.06 * S))
        o.chunks.append(dict(bone=bn, rest=rest, launch=launch, a=a, dist=dist, vz=rng.uniform(3.2, 4.4) * math.sqrt(S),
                             delay=int(rng.integers(0, 4)), floor=r * 0.6, spin=rng.uniform(4, 9),
                             axis=V(tuple(rng.normal(0, 1, 3))).normalized()))
        o.socket(f"Debris_{i + 1}", rest, bn)
    o.rigid.add("Chunk")


def add_markers(o, n, radius, depth, style, seed, size=0.04, shape="bead"):
    """Liquids' Marked cue: small glowing organs that wait `depth` under the surface and breach it while Marked."""
    rng = rnd(seed)
    S = o.S
    for i in range(n):
        a = (i + rng.uniform(-0.2, 0.2)) / n * 2 * math.pi
        r = radius * rng.uniform(0.9, 1.1)
        c = radial(a) * r + V((0, 0, -depth))
        bn = f"Marker_{i + 1}"
        o.bone(bn, c, c + V((0, 0, 0.05 * S)))
        k = rng.uniform(0.8, 1.2)
        if shape == "bead":
            o.b.ellipsoid("Marker", c, (size * k, size * k, size * 0.8 * k), bone=bn, segments=(14, 8), style=style)
        else:
            er = radial(a)
            pts = [c - V((0, 0, 0.05 * S)), c + V((0, 0, 0.03 * S)) - er * 0.01 * S, c + V((0, 0, 0.07 * S)) - er * 0.04 * S]
            o.b.tube("Marker", pts, [size * 0.6 * k, size * 0.4 * k, size * 0.05], sides=8, chain=[bn], style=style)
        o.markers.append(dict(bone=bn, a=a, depth=depth, head=c, phase=rng.uniform(0, 6.3), rate=int(rng.integers(1, 3))))
    o.rigid.add("Marker")


# ---------------------------------------------------------------------------------------------------------------
# The six origins
# ---------------------------------------------------------------------------------------------------------------

def burrow(ground):
    Glow.colour = HOT
    o = Origin("Burrow", 1.0, 1.25)
    add_plates(o, 5, 8, 0.035, 0.52, 1.0, 0.008, 0.075, 0.022, ground_top(ground), UNDER, 2301)
    add_maw(o, OXBLOOD, 2310, gullet=0.17, crest=0.42, zc=0.22, zg=0.025, foot=0.99, rim_tube=0.058)
    tooth = horn_style(BONE[0], BONE[1], 2320, tip_glow=0.5)
    add_teeth(o, 9, 0.37, 0.2, [(0.0, 0.0), (0.02, 0.12), (-0.03, 0.23), (-0.12, 0.27), (-0.19, 0.19)],
              [0.046, 0.036, 0.026, 0.014, 0.002], tooth, 2321)
    claw = horn_style((0.25, 0.07, 0.06), (0.92, 0.78, 0.58), 2330, tip_glow=0.7, nodes=4, node_glow=0.5)
    zo = -o.D
    for i, (a, k) in enumerate(((0.55, 1.0), (3.5, 0.82))):
        er = radial(a)
        pts = [er * 0.45 + V((0, 0, 0.18 + zo)), er * 0.56 * k + V((0, 0, 0.46 * k + zo)),
               er * 0.42 * k + V((0, 0, 0.7 * k + zo)), er * 0.2 + V((0, 0, 0.62 * k + zo))]
        bn = f"Claw_{i + 1}"
        o.bone(bn, pts[0], pts[1], nearest_rim(o, a))
        o.b.tube("Claw", pts, [0.07 * k, 0.055 * k, 0.03 * k, 0.004], sides=11, chain=[bn], style=claw)
        o.teeth.append(dict(bone=bn, a=a, axis=tangent(a), fold=math.radians(-85), phase=i * 2.0, delay=6 + 2 * i))
    o.rigid.add("Claw")
    tendril = horn_style((0.3, 0.08, 0.07), (0.7, 0.36, 0.26), 2340, tip_glow=0.6, nodes=5, node_glow=0.85)
    add_tendrils(o, [1.25, 2.4, 4.4, 5.6], [(0.42, 0.24, 0.0), (0.66, 0.2, 0.03), (0.9, 0.08, 0.06), (1.12, 0.1, 0.13)],
                 [0.05, 0.04, 0.03, 0.018, 0.004], tendril, 2341)
    add_chunks(o, 6, 0.07, E.BASALT, 2350, land=(1.15, 1.85))
    o.spin = 0.35
    return o


def water():
    Glow.colour = None
    S = 0.55
    o = Origin("Water", S, 0.9)
    add_maw(o, WETFLESH, 2410, gullet=0.13, crest=0.3, zc=0.075, zg=0.02, foot=0.56, zf=-0.06, rim_tube=0.045,
            n_rims=6, flank_ridges=7, pores=0.8, fam=E.FAM_MEMBRANE,
            soil=np.array((0.1, 0.17, 0.2)))
    rib = horn_style((0.32, 0.16, 0.18), (0.86, 0.78, 0.72), 2420, tip_glow=0.8, nodes=3, node_glow=0.5)
    add_teeth(o, 6, 0.29, 0.06, [(0.02, 0.0), (0.06, 0.13), (0.0, 0.26), (-0.12, 0.3), (-0.21, 0.22)],
              [0.042, 0.034, 0.024, 0.014, 0.003], rib, 2421, kind="Rib", fold=-80, jitter=0.1)
    tendril = horn_style((0.3, 0.12, 0.14), (0.65, 0.4, 0.42), 2440, tip_glow=0.7, nodes=4, node_glow=0.7,
                         fam=E.FAM_MEMBRANE)
    add_tendrils(o, [0.9, 3.0, 5.0], [(0.3, 0.09, 0.0), (0.42, 0.07, 0.02), (0.56, 0.02, 0.05), (0.66, 0.03, 0.1)],
                 [0.03, 0.025, 0.018, 0.01, 0.003], tendril, 2441)
    add_markers(o, 7, 0.3, 0.3, marker_style(WETFLESH, 2450), 2451, size=0.038)
    for i in range(4):
        o.socket(f"Debris_{i + 1}", radial(i * 1.57 + 0.4) * 0.3 + V((0, 0, 0.12 - o.D)), o.rims[i]["bone"])
    o.spin = 2 * math.pi * 1.25
    o.outer_mode = None
    return o


def lava():
    Glow.colour = None
    S = 0.55
    o = Origin("Lava", S, 0.9, sink=0.14)
    o.plates_marked = True
    crust = flat_top(E.OBSIDIAN, 2501, scale=14)
    o.outer_mode = "prop"
    add_plates(o, 3, 6, 0.03, 0.24, 0.58, -0.14 + 0.012, 0.06, 0.016, crust, E.OBSIDIAN, 2502, finals=(105, 135),
               glow_veins=1.0, hairline=0.7)
    add_maw(o, dict(top=np.array((0.22, 0.18, 0.24)), side=np.array((0.11, 0.08, 0.12)), bottom=np.array((0.04, 0.03, 0.04))),
            2510, gullet=0.13, crest=0.24, zc=0.2, zg=0.03, foot=0.55, zf=-0.04, rim_tube=0.045, n_rims=6,
            flank_ridges=6, glow_depth=1.2, pores=0.3, fam=E.FAM_ROCK, rim_glow=0.7,
            soil=np.array((0.07, 0.05, 0.05)), fissure=0.9)
    ember = horn_style((0.12, 0.05, 0.05), (1.0, 0.82, 0.56), 2520, tip_glow=1.0, nodes=3, node_glow=0.8,
                       fam=E.FAM_CRYSTAL)
    add_teeth(o, 7, 0.21, 0.18, [(0.0, 0.0), (0.03, 0.12), (0.0, 0.2), (-0.05, 0.24)], [0.035, 0.026, 0.014, 0.002],
              ember, 2521, kind="Ember", fold=-50)
    add_chunks(o, 5, 0.045, E.OBSIDIAN, 2530, land=(0.9, 1.5), glow=1.0)
    return o


def quicksand():
    Glow.colour = None
    S = 0.55
    o = Origin("Quicksand", S, 0.9, sink=0.16)
    o.outer_mode = "slump"
    add_plates(o, 0, 6, 0.0, 0.3, 0.6, -0.16 + 0.01, 0.06, 0.018, flat_top(SAND, 2601, ripple=0.08), SAND, 2602,
               finals=(-45, -30), glow_veins=0.5, hairline=0.25)
    # No collar: a sinkhole is a slumped funnel, not a ring (round 14's weak point). Concentric slump terraces come
    # from the throat rings; six big bone hooks and a dark eye carry the read.
    add_maw(o, dict(top=SAND["top"] * 0.9, side=SAND["side"], bottom=np.array((0.06, 0.045, 0.03))), 2610,
            gullet=0.09, crest=0.33, zc=0.07, zg=0.02, foot=0.6, zf=-0.04, rim=False, n_rims=6, flank_ridges=0,
            glow_depth=0.9, pores=0.0, fam=E.FAM_ROCK, soil=SAND["top"] * 0.85)
    hook = horn_style((0.3, 0.2, 0.14), (0.93, 0.85, 0.7), 2620, tip_glow=0.9, nodes=3, node_glow=0.5)
    add_teeth(o, 6, 0.3, 0.05, [(0.02, 0.0), (0.04, 0.15), (-0.04, 0.27), (-0.15, 0.3), (-0.23, 0.2)],
              [0.04, 0.032, 0.022, 0.012, 0.002], hook, 2621, kind="Hook", fold=-75)
    shell = dict(top=np.array((0.78, 0.36, 0.12)), side=np.array((0.36, 0.07, 0.06)), bottom=np.array((0.15, 0.04, 0.03)))
    add_markers(o, 6, 0.33, 0.25, marker_style(shell, 2650), 2651, size=0.042)
    for i in range(4):
        o.socket(f"Debris_{i + 1}", radial(i * 1.57 + 0.2) * 0.3 + V((0, 0, 0.1 - o.D)), o.rims[i]["bone"])
    o.spin = math.pi
    return o


def ice():
    Glow.colour = None
    S = 0.55
    o = Origin("Ice", S, 0.9, sink=0.2)
    o.plates_marked = True
    o.outer_mode = "prop"
    # Floe tops a step darker and bluer than the sheet and dark seams, so the (near-white) ice tint of the glow reads.
    floe = dict(ICEP, top=np.array((0.56, 0.68, 0.8)))
    add_plates(o, 4, 7, 0.03, 0.3, 0.62, -0.2 + 0.008, 0.05, 0.016, flat_top(floe, 2701, scale=6), ICEP, 2702,
               finals=(60, 85), glow_veins=0.6, hairline=0.8, bevel_dark=0.85)
    add_maw(o, FROST, 2710, gullet=0.12, crest=0.26, zc=0.13, zg=0.025, foot=0.58, zf=-0.04, rim_tube=0.04,
            n_rims=6, flank_ridges=9, pores=0.5, fam=E.FAM_CHITIN, rim_glow=0.6,
            soil=np.array((0.62, 0.7, 0.78)))
    icicle = horn_style((0.35, 0.4, 0.5), (0.9, 0.97, 1.0), 2720, tip_glow=0.7, fam=E.FAM_CRYSTAL)
    add_teeth(o, 8, 0.23, 0.12, [(0.0, 0.0), (0.0, 0.11), (-0.05, 0.19), (-0.1, 0.16)], [0.03, 0.022, 0.012, 0.002],
              icicle, 2721, kind="Icicle", fold=-65)
    add_chunks(o, 4, 0.04, ICEP, 2730, land=(1.0, 1.5), glow=0.4)
    return o


def bog():
    Glow.colour = None
    S = 0.55
    o = Origin("Bog", S, 0.9)
    add_maw(o, MIRE, 2810, gullet=0.08, crest=0.2, zc=0.17, zg=0.04, foot=0.56, zf=-0.04, rim_tube=0.0, rim=False,
            n_rims=6, flank_ridges=9, pores=0.9, fam=E.FAM_MEMBRANE,
            soil=np.array((0.12, 0.13, 0.07)))
    lobe = horn_style(ROT["side"], ROT["top"] * 1.3, 2820, tip_glow=0.5, fam=E.FAM_MEMBRANE)
    add_teeth(o, 7, 0.18, 0.14, [(0.02, 0.0), (0.0, 0.07), (-0.06, 0.1), (-0.11, 0.08)], [0.05, 0.045, 0.03, 0.012],
              lobe, 2821, kind="Lobe", fold=-40, size_var=(0.85, 1.1))
    tendril = horn_style((0.18, 0.15, 0.08), (0.5, 0.3, 0.26), 2840, tip_glow=0.9, nodes=3, node_glow=0.6,
                         fam=E.FAM_MEMBRANE)
    add_tendrils(o, [0.6, 2.7, 4.6], [(0.24, 0.14, 0.0), (0.38, 0.12, 0.02), (0.52, 0.04, 0.06), (0.62, 0.0, 0.1)],
                 [0.03, 0.025, 0.018, 0.01, 0.004], tendril, 2841)
    add_markers(o, 5, 0.32, 0.3, marker_style(MIRE, 2850), 2851, size=0.045)
    add_chunks(o, 4, 0.04, dict(top=MIRE["top"], side=MIRE["side"], bottom=MIRE["bottom"], seam=MIRE["bottom"]),
               2860, land=(0.9, 1.4), glow=0.3)
    o.spin = math.pi / 2
    o.outer_mode = None
    return o


KINDS = {"Burrow": burrow, "Water": water, "Lava": lava, "Quicksand": quicksand, "Ice": ice, "Bog": bog}
BUDGET = {"Burrow": (3900, 1750, 0.007), "Water": (2400, 1080, 0.005), "Lava": (2400, 1080, 0.005),
          "Quicksand": (2400, 1080, 0.005), "Ice": (2400, 1080, 0.005), "Bog": (2400, 1080, 0.005)}
FAMILIES = {"Burrow": ("rock", "chitin"), "Water": ("membrane", "chitin"), "Lava": ("rock", "crystal"),
            "Quicksand": ("rock", "chitin"), "Ice": ("crystal", "rock"), "Bog": ("membrane", "chitin")}


# ---------------------------------------------------------------------------------------------------------------
# Animation
# ---------------------------------------------------------------------------------------------------------------

def jit(f, phase, rate=1.0):
    return math.sin(f * 0.9 * rate + phase) * 0.6 + math.sin(f * 2.3 * rate + phase * 1.7) * 0.4


def plate_surface(o, f_lift):
    """Liquid plates rise to the surface (loc z) by the share f_lift."""
    return V((0, 0, o.sink * f_lift))


def open_specs(o, f):
    """Open: Stage 1 tremor (0-30), Stage 2 bulge (30-54) and burst (54), Stage 3 the maw settles (54-96)."""
    sp = {}
    S = o.S
    trem = ease(0, TREMOR_END, f)
    bulge = ease(TREMOR_END, BURST, f)
    after = max(0.0, (f - BURST) / FPS)
    # Liquid plates that are not on the surface yet come up in the first half second.
    lift0 = 1.0 if o.plates_marked else ease(0, 16, f)
    for p in o.outer:
        shake = (0.012 * trem + 0.03 * bulge) * S * jit(f, p["phase"], 1.4)
        tilt_j = math.radians(1.5 * trem + 3 * bulge) * jit(f + 7, p["phase"])
        hv = math.radians(4 + 6 * bulge) * bulge * (-0.6 if o.outer_mode == "slump" else 1.0)
        lift = (0.01 * trem + 0.03 * bulge) * S
        if f >= BURST:
            u = ease(BURST + p["delay"], BURST + p["delay"] + 10, f)
            fin = p["final"]
            # Slams to its final angle, rebounds a little, settles.
            reb = math.radians(9) * math.sin(math.pi * ease(BURST + p["delay"] + 10, BURST + p["delay"] + 22, f)) *                 (1 if o.outer_mode == "flip" else 0.6)
            ang = hv + (fin - hv) * u ** 1.6 - reb * math.copysign(1, fin)
            fade = 1 - ease(BURST, BURST + 4, f)
            loc = plate_surface(o, lift0) + V((0, 0, 0.04 * S * (1 - u) + shake * fade))
            sp[p["bone"]] = {"rot": [(p["axis"], ang), (radial(p["a"]), tilt_j * fade)], "loc": loc}
        else:
            loc = plate_surface(o, lift0) + V((0, 0, lift + shake))
            sp[p["bone"]] = {"rot": [(p["axis"], hv), (radial(p["a"]), tilt_j)], "loc": loc}
    for p in o.inner:
        shake = (0.015 * trem + 0.03 * bulge) * S * jit(f, p["phase"], 1.7)
        rise = (0.012 * trem + 0.11 * bulge) * S
        tilt = math.radians(14) * bulge
        base = plate_surface(o, lift0)
        if f < BURST or not p["throw"]:
            sp[p["bone"]] = {"rot": [(p["axis"], tilt + math.radians(2) * jit(f + 3, p["phase"]) * trem)],
                             "loc": base + V((0, 0, rise + shake))}
            continue
        origin = p["head"] + base + V((0, 0, rise))
        pos, angle, spin = flight(origin, radial(p["a"]), p["dist"], p["vz"], p["floor"], after, p["final"], tilt)
        sp[p["bone"]] = {"rot": [(p["axis"], angle), (V((0, 0, 1)), p["spin"] * spin)], "loc": pos - p["head"]}
    # The maw climbs under the ground during the bulge, surges out at the burst, overshoots and settles.
    D = o.D
    z_under = -0.32 * S
    if f < BURST:
        mz = (D + z_under) * bulge
    else:
        u = ease(BURST + 1, SURGE_END - 4, f)
        over = 0.07 * S * math.sin(math.pi * ease(SURGE_END - 8, SURGE_END + 14, f))
        mz = D + z_under * (1 - u) + over
    spin_u = ease(TREMOR_END + 6, SURGE_END + 10, f)
    sp["Maw"] = {"rot": [(V((0, 0, 1)), o.spin * (spin_u ** 0.7))], "loc": V((0, 0, mz))}
    throat = 0.3 + 0.85 * ease(BURST + 2, SURGE_END + 4, f) - 0.15 * ease(SURGE_END + 4, SURGE_END + 20, f)
    sp["Throat"] = {"scale": throat}
    for t in o.teeth:
        u = ease(SURGE_END - 4 + t["delay"], SURGE_END + 10 + t["delay"], f)
        over = math.radians(16) * math.sin(math.pi * ease(SURGE_END + 4 + t["delay"], SURGE_END + 22 + t["delay"], f))
        sp[t["bone"]] = {"rot": [(t["axis"], t["fold"] * (1 - u) + over)]}
    for td in o.tendrils:
        u = ease(SURGE_END - 4, SURGE_END + 12, f)
        lash = math.sin(math.pi * ease(SURGE_END, OPEN_FRAMES, f) * 2.0) * (1 - ease(SURGE_END + 14, OPEN_FRAMES, f))
        for j, bn in enumerate(td["bones"]):
            ang = td["curl"][j] * (1 - u) + math.radians(14 + 8 * j) * lash * td["side"] * 0.5
            yaw = math.radians(10 * (j + 1)) * lash * td["side"]
            sp[bn] = {"rot": [(td["axis"], ang), (V((0, 0, 1)), yaw)]}
    for c in o.chunks:
        sp[c["bone"]] = chunk_spec(c, f)
    for m in o.markers:
        # Marked frame 0 is where Open starts; the markers start to circle (the whirl begins), are drawn in and
        # drop away as the maw rises.
        up = m["depth"] * (1 - ease(TREMOR_END + 8, BURST, f))
        swirl = min(o.spin, 2.0) * 0.5 * (ease(0, BURST, f) ** 1.5)
        pull = 1 - 0.45 * ease(TREMOR_END, BURST, f)
        h = m["head"]
        moved = Matrix.Rotation(swirl, 3, "Z") @ V((h.x * pull, h.y * pull, h.z))
        sp[m["bone"]] = {"loc": moved - h + V((0, 0, up + 0.01 * S * jit(f, m["phase"], 2.0) * trem))}
    return sp


def flight(origin, dirn, dist, vz, floor, t, final, tilt0, g=9.0):
    """A thrown plate: ballistic arc from `origin` to `dist` along `dirn`, tumbling to `final` about its tangent;
    returns (position, angle about the tangent, share of the flight for the spin)."""
    a, b, c = -0.5 * g, vz, origin.z - floor
    tl = (-b - math.sqrt(b * b - 4 * a * c)) / (2 * a)
    vh = dist / tl
    if t < tl:
        p = origin + dirn * vh * t + V((0, 0, vz * t - 0.5 * g * t * t))
        k = t / tl
        return p, tilt0 + (final - tilt0) * k, k
    # Landed: a short skid and a small bounce, then rest.
    dt = t - tl
    skid = 0.04 * (1 - math.exp(-dt * 10))
    bounce = 0.02 * max(0.0, math.sin(min(dt, 0.25) * math.pi / 0.25)) * math.exp(-dt * 4)
    p = origin + dirn * (dist + skid) + V((0, 0, floor - origin.z + bounce))
    return p, final, 1.0


def chunk_spec(c, f):
    t = (f - BURST - c["delay"]) / FPS
    if t < 0:
        return {"loc": V((0, 0, 0))}
    origin = c["launch"]
    a, b, k = -4.5, c["vz"], origin.z - c["floor"]
    tl = (-b - math.sqrt(b * b - 4 * a * k)) / (2 * a)
    vh = c["dist"] / tl
    er = radial(c["a"])
    if t < tl:
        p = origin + er * vh * t + V((0, 0, c["vz"] * t - 4.5 * t * t))
        ang = c["spin"] * t
    else:
        p = origin + er * c["dist"] + V((0, 0, c["floor"] - origin.z))
        ang = c["spin"] * tl
    return {"rot": [(c["axis"], ang)], "loc": p - c["rest"]}


def final_specs(o):
    """The settled open state (Open's last frame, Idle's base)."""
    return open_specs(o, OPEN_FRAMES)


def osc(w, ph):
    """A sine that is 0 at w = 0, so a loop's frame 0 equals the pose it starts from."""
    return math.sin(w + ph) - math.sin(ph)


def idle_specs(o, t):
    """Breathing rim (a wave running round it), pulsing throat, flexing teeth, twitching tendrils."""
    sp = final_specs(o)
    S = o.S
    w = 2 * math.pi * t
    for i, r in enumerate(o.rims):
        ph = r["a"] * 2
        breath = 0.022 * S * osc(w, ph) + 0.008 * S * osc(2 * w, ph * 0.5)
        sp[r["bone"]] = {"loc": radial(r["a"]) * breath + V((0, 0, 0.012 * S * osc(w, ph + 0.8)))}
    sp["Throat"] = {"scale": 1.0 + 0.07 * math.sin(2 * w) + 0.03 * osc(3 * w, 1.0)}
    mz = sp["Maw"]["loc"] + V((0, 0, 0.006 * S * math.sin(w)))
    sp["Maw"] = {"rot": sp["Maw"]["rot"], "loc": mz}
    for tt in o.teeth:
        base = sp[tt["bone"]]["rot"][0][1]
        sp[tt["bone"]] = {"rot": [(tt["axis"], base + math.radians(4) * osc(w * (1 + int(tt["delay"]) % 2), tt["phase"]))]}
    for td in o.tendrils:
        for j, bn in enumerate(td["bones"]):
            ang = math.radians(5 + 4 * j) * osc(w * (j + 1), td["phase"] + j * 0.7)
            yaw = math.radians(7 + 5 * j) * osc(w * 2, td["phase"] * 1.3 + j)
            sp[bn] = {"rot": [(td["axis"], ang), (V((0, 0, 1)), yaw)]}
    for p in o.outer:
        r = sp[p["bone"]]["rot"][0][1]
        sp[p["bone"]] = {"rot": [(p["axis"], r + math.radians(0.6) * osc(w, p["phase"]))], "loc": sp[p["bone"]]["loc"]}
    return sp


def spawn_specs(o, t, f):
    """A convulsion when a creature climbs out: the rim heaves out and up, the throat swells, teeth flare, tendrils
    lash. Starts and ends on Idle frame 0, so it can play on top of Idle (additive) or cross-fade cleanly."""
    sp = idle_specs(o, 0.0)
    S = o.S
    k = ease(0, SPAWN_PEAK, f) * (1 - ease(SPAWN_PEAK, SPAWN_FRAMES, f))
    shiver = math.sin(f * 2.4) * (1 - t) * k
    for r in o.rims:
        base = sp[r["bone"]]["loc"]
        sp[r["bone"]] = {"loc": base + radial(r["a"]) * 0.06 * S * k + V((0, 0, 0.035 * S * k))}
    sp["Throat"] = {"scale": sp["Throat"]["scale"] + 0.3 * k}
    sp["Maw"] = {"rot": sp["Maw"]["rot"], "loc": sp["Maw"]["loc"] + V((0, 0, 0.03 * S * k))}
    for tt in o.teeth:
        base = sp[tt["bone"]]["rot"][0][1]
        sp[tt["bone"]] = {"rot": [(tt["axis"], base + math.radians(20) * k + math.radians(3) * shiver)]}
    for td in o.tendrils:
        for j, bn in enumerate(td["bones"]):
            rot = sp[bn]["rot"]
            sp[bn] = {"rot": [(td["axis"], rot[0][1] - math.radians(18 + 10 * j) * k), rot[1]]}
    for p in o.outer:
        rot = sp[p["bone"]]["rot"][0]
        sp[p["bone"]] = {"rot": [(p["axis"], rot[1] + math.radians(2.5) * shiver)], "loc": sp[p["bone"]]["loc"]}
    return sp


def marked_specs(o, t, f):
    """Marked: the ground is still closed. Burrow: plates flush with a 2 mm breath of the centre wedges (the glow
    pulse itself is Unity's emission strength). Liquids: the floating plates (lava crust, ice floes) and the
    marker organs breach the surface and bob."""
    sp = {}
    S = o.S
    w = 2 * math.pi * t
    for p in o.inner + o.outer:
        lift = plate_surface(o, 1.0 if o.plates_marked else 0.0)
        bob = 0.002 * S * (1 + math.sin(w + p["phase"])) if (o.kind == "Burrow" and p in o.inner) else \
            (0.006 * S * math.sin(w + p["phase"]) if o.plates_marked else 0.0)
        sp[p["bone"]] = {"loc": lift + V((0, 0, bob))}
    for m in o.markers:
        sp[m["bone"]] = {"loc": V((0, 0, m["depth"] + 0.012 * S * osc(w * m["rate"], m["phase"])))}
    sp["Maw"] = {"loc": V((0, 0, 0))}
    sp["Throat"] = {"scale": 0.3}
    for tt in o.teeth:
        sp[tt["bone"]] = {"rot": [(tt["axis"], tt["fold"])]}
    for td in o.tendrils:
        for j, bn in enumerate(td["bones"]):
            sp[bn] = {"rot": [(td["axis"], td["curl"][j])]}
    return sp


def closed_specs(o):
    sp = marked_specs(o, 0.0, 0)
    for p in o.inner + o.outer:
        sp[p["bone"]] = {"loc": V((0, 0, 0))}
    for m in o.markers:
        sp[m["bone"]] = {"loc": V((0, 0, 0))}
    return sp


def clip_table(o):
    return {
        "Closed": lambda poser: (1, lambda t, f: closed_specs(o), False),
        "Marked": lambda poser: (MARKED_FRAMES, lambda t, f: marked_specs(o, t, f), True),
        "Open": lambda poser: (OPEN_FRAMES, lambda t, f: open_specs(o, f), False),
        "Idle": lambda poser: (IDLE_FRAMES, lambda t, f: idle_specs(o, t), True),
        "Spawn": lambda poser: (SPAWN_FRAMES, lambda t, f: spawn_specs(o, t, f), False),
    }


# ---------------------------------------------------------------------------------------------------------------
# Camo pass (burrow): full-resolution ground set on the plate tops, after the bake
# ---------------------------------------------------------------------------------------------------------------

def top_texels(low, size, z_max=0.02, nz_min=0.86):
    """Rasterises the up-facing plate-top triangles of the game mesh (bind pose) into UV space: per texel the
    weight (0..1), world xy and the tangent frame (T, B unit vectors in xy) for converting the ground normal."""
    me = low.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active.data
    co = np.array([v.co[:] for v in me.vertices])
    w = np.zeros((size, size))
    xy = np.zeros((size, size, 2))
    frame = np.zeros((size, size, 6))
    for tri in me.loop_triangles:
        n = tri.normal
        if n.z < nz_min:
            continue
        P = co[list(tri.vertices)]
        if P[:, 2].min() < -0.05 or P[:, 2].max() > z_max:
            continue
        U = np.array([uv[li].uv[:] for li in tri.loops]) * size - 0.5
        weight = float(ss(nz_min, 0.97, np.array([n.z]))[0])
        e1, e2 = P[1] - P[0], P[2] - P[0]
        d1, d2 = U[1] - U[0], U[2] - U[0]
        det = d1[0] * d2[1] - d2[0] * d1[1]
        if abs(det) < 1e-12:
            continue
        T = (e1 * d2[1] - e2 * d1[1]) / det
        Bv = (e2 * d1[0] - e1 * d2[0]) / det
        T /= max(np.linalg.norm(T), 1e-9)
        Bv /= max(np.linalg.norm(Bv), 1e-9)
        x0, y0 = np.floor(U.min(axis=0)).astype(int) - 1
        x1, y1 = np.ceil(U.max(axis=0)).astype(int) + 1
        x0, y0 = max(x0, 0), max(y0, 0)
        x1, y1 = min(x1, size - 1), min(y1, size - 1)
        if x1 < x0 or y1 < y0:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        px = np.stack([gx.ravel(), gy.ravel()], axis=1).astype(float)
        v0 = px - U[0]
        l1 = (v0[:, 0] * d2[1] - d2[0] * v0[:, 1]) / det
        l2 = (d1[0] * v0[:, 1] - v0[:, 0] * d1[1]) / det
        l0 = 1 - l1 - l2
        eps = 0.7 / max(abs(det) ** 0.5, 1.0)
        inside = (l0 >= -eps) & (l1 >= -eps) & (l2 >= -eps)
        if not inside.any():
            continue
        pts = P[0] + l1[inside, None] * e1 + l2[inside, None] * e2
        ix = gx.ravel()[inside]
        iy = gy.ravel()[inside]
        better = weight > w[iy, ix]
        ix, iy, pts = ix[better], iy[better], pts[better]
        w[iy, ix] = weight
        xy[iy, ix] = pts[:, :2]
        frame[iy, ix] = np.r_[T, Bv][None, :].repeat(len(ix), 0)
    return w, xy, frame


def camo_maps(base, ground, texels, emission):
    """The baked maps with the plate tops replaced by the ground set (albedo, normal turned into the texel's
    tangent frame, AO and smoothness; detail mask B = rock). The hairline crack (baked emission) is kept."""
    w, xy, frame = texels
    glow = emission[..., :3].max(axis=2)
    w = w * (1 - ss(0.03, 0.2, glow))
    sel = w > 0.001
    xs, ys = xy[sel, 0], xy[sel, 1]
    k = w[sel][:, None]
    out = {key: arr.copy() for key, arr in base.items()}
    ga = ground.sample("Albedo", xs, ys)
    out["Albedo"][sel, :3] = out["Albedo"][sel, :3] * (1 - k) + ga[:, :3] * k
    gn = ground.sample("Normal", xs, ys)[:, :3] * 2 - 1
    T = frame[sel, 0:3]
    Bv = frame[sel, 3:6]
    nw = gn[:, 0:1] * np.array([[1.0, 0, 0]]) + gn[:, 1:2] * np.array([[0, 1.0, 0]]) + gn[:, 2:3] * np.array([[0, 0, 1.0]])
    nt = np.stack([(nw * T).sum(1), (nw * Bv).sum(1), nw[:, 2]], axis=1)
    nt /= np.maximum(np.linalg.norm(nt, axis=1, keepdims=True), 1e-6)
    old = out["Normal"][sel, :3] * 2 - 1
    mixn = old * (1 - k) + nt * k
    mixn /= np.maximum(np.linalg.norm(mixn, axis=1, keepdims=True), 1e-6)
    out["Normal"][sel, :3] = mixn * 0.5 + 0.5
    gm = ground.sample("Mask", xs, ys)
    m = out["Mask"][sel]
    m[:, 0] = m[:, 0] * (1 - k[:, 0]) + gm[:, 0] * k[:, 0]
    m[:, 1] = m[:, 1] * (1 - k[:, 0]) + gm[:, 1] * k[:, 0]
    m[:, 2] = m[:, 2] * (1 - k[:, 0]) + 1.0 * k[:, 0]
    m[:, 3] = m[:, 3] * (1 - k[:, 0])
    out["Mask"][sel] = m
    return out


def write_maps(maps, out_dir, prefix, low_size=512):
    os.makedirs(os.path.join(out_dir, "Low"), exist_ok=True)
    spaces = {"Albedo": "sRGB", "Normal": "Non-Color", "Mask": "Non-Color", "Emission": "sRGB"}
    for key, arr in maps.items():
        C.array_to_image(arr, os.path.join(out_dir, f"{prefix}_{key}.png"), colorspace=spaces[key])
        small = C.downsample(arr.astype(np.float32), arr.shape[0] // low_size, normal=key == "Normal")
        C.array_to_image(small, os.path.join(out_dir, "Low", f"{prefix}_{key}.png"), colorspace=spaces[key])


def apply_camo(asset, grounds):
    """Default maps = CrystalPlains; `Skins/<Continent>/` holds Albedo/Normal/Mask (+Low) for the other sets."""
    out_dir = os.path.join(C.EXPORTS, asset.name)
    base = {k: C.image_to_array(os.path.join(out_dir, f"{asset.name}_{k}.png")) for k in ("Albedo", "Normal", "Mask", "Emission")}
    size = base["Albedo"].shape[0]
    texels = top_texels(asset.low, size)
    C.log("camo texels", int((texels[0] > 0).sum()))
    skins = {}
    for cont, ground in grounds.items():
        maps = camo_maps({k: base[k] for k in ("Albedo", "Normal", "Mask")}, ground, texels, base["Emission"])
        if cont == "CrystalPlains":
            write_maps(maps, out_dir, asset.name)
        sk = os.path.join(out_dir, "Skins", cont)
        write_maps(maps, sk, asset.name)
        skins[cont] = os.path.relpath(sk, out_dir).replace("\\", "/")
    for img in bpy.data.images:
        if img.filepath and asset.name in img.filepath:
            img.reload()
    return skins


# ---------------------------------------------------------------------------------------------------------------
# Build, rig, export
# ---------------------------------------------------------------------------------------------------------------

def export_rig(path, arm, mesh, actions):
    C.set_action(arm, actions["Closed"], 0)
    tk.export_fbx(path, [arm, mesh], animated=True)


def unwrap_cut(obj, builder, mirror=False):
    """deep_kit's unwrap (closed islands such as the plates are cut across their thinnest axis: top and underside
    become two clean islands instead of a smart-project shred). The origins are not mirrored, so no twins match."""
    return DK.unwrap_dk(obj, builder)


def build(kind, grounds):
    C.uvlayout.unwrap = unwrap_cut
    C.reset_scene(FPS)
    E.use_env_exports()
    o = KINDS[kind](grounds["CrystalPlains"]) if kind == "Burrow" else KINDS[kind]()
    tris, lod_tris, voxel = BUDGET[kind]
    asset = C.build_mesh_asset(o.name, o.b, voxel=voxel, target_tris=tris, tex_size=1024, ao_distance=0.12 * o.S + 0.03,
                               extrusion=0.02, ray=0.06, emission_strength=2.4, smooth_iterations=2, symmetric=False,
                               low_from_detail=True, families=FAMILIES[kind], low_size=512, border_smooth=1)
    skins = apply_camo(asset, grounds) if kind == "Burrow" else None
    arm = C.build_armature(o.name + "_Rig", [(n, h, t, p, d) for n, h, t, p, d in o.bones])
    C.skin(asset.low, arm, o.b, rigid=tuple(o.rigid), blend=2)
    poser = B.ScalePoser(arm, ["Throat"])
    actions = {}
    for clip, fn in clip_table(o).items():
        frames, spec, loop = fn(poser)
        actions[clip] = poser.action(clip, frames, spec, loop=loop)
    out_dir = os.path.join(C.EXPORTS, o.name)
    export_rig(os.path.join(out_dir, o.name + ".fbx"), arm, asset.low, actions)
    lod_mesh = C.decimate(asset.low.data.copy(), lod_tris, o.name + "_LOD1", symmetric=False)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(o.name + "_LOD1", lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    C.skin(lod, arm, o.b, rigid=tuple(o.rigid), blend=1)
    export_rig(os.path.join(out_dir, o.name + "_LOD1.fbx"), arm, lod, actions)
    lod.hide_render = True
    report(o, asset, arm, lod, skins)
    C.save_blend(o.name)
    return o, asset, arm, lod, actions


def socket_positions(o, arm, act, frame):
    C.set_action(arm, act, frame)
    bpy.context.view_layer.update()
    out = {}
    for name, _ in o.sockets:
        pb = arm.pose.bones[name]
        h = arm.matrix_world @ pb.head
        # Blender (x, y, z) -> Unity (x, z, y) mirrored as the FBX axis conversion does (-Y forward -> +Z).
        out[name] = [round(h.x, 3), round(h.z, 3), round(-h.y, 3)]
    return out


def posed_size(obj, arm, act, frame, above=None):
    """Bounding box (x, y, z extent; z from the lowest point, or `above` a height) of the posed mesh."""
    C.set_action(arm, act, frame)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = obj.evaluated_get(dg).to_mesh()
    co = np.array([v.co[:] for v in me.vertices])
    obj.evaluated_get(dg).to_mesh_clear()
    if above is not None:
        co = co[co[:, 2] > above]
        if not len(co):
            return [0.0, 0.0, 0.0]
    ext = co.max(axis=0) - co.min(axis=0)
    return [round(float(e), 3) for e in ext] + [round(float(co[:, 2].max()), 3)]


def report(o, asset, arm, lod, skins):
    acts = {a.name: a for a in bpy.data.actions}
    data = {
        "asset": o.name, "kind": o.kind, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
        "lod1": {"fbx": o.name + "_LOD1.fbx", "tris": C.tri_count(lod), "bones": len(arm.data.bones)},
        "bones": len(arm.data.bones), "deform_bones": sum(1 for b in arm.data.bones if b.use_deform),
        "texture_size": 1024, "low_texture_size": 512, "maps": ["Albedo", "Normal", "Emission", "Mask"],
        "uv": asset.uv_stats, "detail": asset.details,
        "fps": FPS,
        "clips": {"Closed": 1, "Marked": MARKED_FRAMES, "Open": OPEN_FRAMES, "Idle": IDLE_FRAMES, "Spawn": SPAWN_FRAMES},
        "loops": ["Marked", "Idle"],
        "open_keys": {"tremor": [0, TREMOR_END], "bulge": [TREMOR_END, BURST], "burst_frame": BURST,
                      "burst_seconds": round(BURST / FPS, 2), "maw_up_frame": SURGE_END,
                      "first_emerge_from_frame": SURGE_END + 10, "settled_frame": OPEN_FRAMES},
        "spawn_keys": {"peak_frame": SPAWN_PEAK, "start_and_end": "Idle frame 0"},
        "sockets_open": socket_positions(o, arm, acts["Idle"], 0),
        "sockets_closed": socket_positions(o, arm, acts["Closed"], 0),
        "emission": "baked warm (not a mask)" if o.kind == "Burrow" else "white mask, tint with NestColor(element)",
        "pivot": "ground at the nest cell centre" if o.kind == "Burrow" else
                 "on the liquid surface at the cell centre (TerrainLiquidBuilder.SurfaceLevel(kind))",
        "bind_pose": "Closed (maw D under the pivot)", "maw_storage_depth": o.D, "plate_sink": o.sink,
    }
    if skins:
        data["skins"] = skins
        data["camo"] = ("plate tops painted with the ground set at world xy / 3.0 (the sets' tile size) in the "
                        "model's own space; Closed = emission 0, Marked = emission pulse 0.35..1 on the hairlines")
    data["size_open_m"] = posed_size(asset.low, arm, acts["Idle"], 0)
    data["size_closed_above_ground_m"] = posed_size(asset.low, arm, acts["Closed"], 0, above=-0.001)
    C.write_report(o.name, data)


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

def preview_material(asset, kind):
    mat = asset.low.data.materials[0]
    if kind != "Burrow":
        DK.tint_emission(mat, TINT[kind])
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    # Same occlusion as Game/EnvironmentLit (mask R at 0.5) so the camo compares with the ground fairly.
    alb_link = next(l for l in nt.links if l.to_socket == bsdf.inputs["Base Color"])
    mask = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeTexImage" and n.image and "_Mask" in n.image.name)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], sep.inputs[0])
    occ = nt.nodes.new("ShaderNodeMapRange")
    occ.inputs["To Min"].default_value = 0.5
    nt.links.new(sep.outputs[0], occ.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(alb_link.from_socket, mul.inputs[6])
    nt.links.new(occ.outputs[0], mul.inputs[7])
    nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    return bsdf


def ground_material(rel):
    """Preview stand-in for Game/EnvironmentGround: the set in object xy / 3.0, occlusion at 0.6."""
    name = os.path.basename(rel)
    base = os.path.join(E.ENV, rel)
    mat = C.new_material("PreviewGround_" + name)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / TILE, 1 / TILE, 1)
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])

    def tex(k, non_color):
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(os.path.join(base, f"{name}_{k}.png"), check_existing=True)
        if non_color:
            t.image.colorspace_settings.name = "Non-Color"
            t.image.alpha_mode = "CHANNEL_PACKED"
        nt.links.new(mp.outputs["Vector"], t.inputs["Vector"])
        return t
    alb = tex("Albedo", False)
    mask = tex("Mask", True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], sep.inputs[0])
    occ = nt.nodes.new("ShaderNodeMapRange")
    occ.inputs["To Min"].default_value = 0.4
    nt.links.new(sep.outputs[0], occ.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(alb.outputs["Color"], mul.inputs[6])
    nt.links.new(occ.outputs[0], mul.inputs[7])
    nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(sep.outputs[1], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(tex("Normal", True).outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def stage(kind):
    """Terrain for the previews: the ground set plane (burrow) or a liquid sheet over a bed."""
    objs = []
    if kind == "Burrow":
        objs.append(E.plane("PreviewGround", 24, 24, ground_material(GROUND_SETS["CrystalPlains"])))
    else:
        colour, alpha = LIQUID[kind]
        dark = tuple(c * 0.4 for c in colour)
        lava = kind == "Lava"
        objs.append(DK.liquid_sheet(kind + "_Liquid", (0.25, 0.06, 0.02) if lava else dark, alpha, size=10.0, z=0.0,
                                    emission=(0.85, 0.28, 0.05) if lava else None, strength=0.55 if lava else 0.0))
        objs.append(C.ground(size=10.0, color=(0.05, 0.05, 0.07), z=-0.45))
    return objs


def skitters(positions, scale=0.45):
    """Skitters at their game scale (0.45) mid-stride, appended from ArtSource/Blend/Skitter.blend."""
    arm, mesh, acts = B.append_base("Skitter")
    act = next((a for n, a in acts.items() if n == "Walk"), next(iter(acts.values())))
    C.set_action(arm, act, 6)
    objs = [arm, mesh]
    for pos, yaw in positions:
        a2 = C.link(arm.copy())
        m2 = C.link(mesh.copy())
        m2.parent = a2
        m2.modifiers["Armature"].object = a2
        a2.location = Vector(pos)
        a2.rotation_euler.z = yaw
        a2.scale = (scale, scale, scale)
        a2.hide_render = m2.hide_render = False
        objs += [m2, a2]
    return objs


def game_shot(path, zoom, width=960, height=540, target=(0, 0, 0)):
    lights = C.game_lights()
    cam = C.game_camera(Vector(target), width / C.GAME_PIXELS_PER_UNIT / zoom)
    C.render(cam, path, width, height, samples=32)
    for x in lights + [cam]:
        bpy.data.objects.remove(x)
    return path


def previews(o, asset, arm, actions, out):
    kind = o.kind
    d = os.path.join(out, kind.lower())
    os.makedirs(d, exist_ok=True)
    C.preview_world((0.02, 0.02, 0.035))
    bsdf = preview_material(asset, kind)
    full = bsdf.inputs["Emission Strength"].default_value
    world_objs = stage(kind)
    zoom = 2.2 if kind == "Burrow" else 3.6
    paths = []
    # Closed on its terrain (emission off: what Unity shows before Marked) and Marked (emission pulse at its peak).
    C.set_action(arm, actions["Closed"], 0)
    bsdf.inputs["Emission Strength"].default_value = 0.0 if kind == "Burrow" else full
    paths.append(game_shot(os.path.join(d, "closed_game.png"), zoom))
    paths.append(game_shot(os.path.join(d, "closed_play.png"), 1.0, 1280, 720))
    bsdf.inputs["Emission Strength"].default_value = full
    C.set_action(arm, actions["Marked"], MARKED_FRAMES // 4)
    paths.append(game_shot(os.path.join(d, "marked_game.png"), zoom))
    paths.append(game_shot(os.path.join(d, "marked_play.png"), 1.0, 1280, 720))
    # Open strip.
    strip = []
    for i, f in enumerate([0, 15, 30, 42, 50, 54, 57, 60, 64, 70, 80, 96]):
        C.set_action(arm, actions["Open"], f)
        strip.append(game_shot(os.path.join(d, f"open_{i:02d}_f{f}.png"), zoom, 640, 400))
    C.contact_sheet(strip, os.path.join(d, "open_strip.png"), 4, 400)
    # Idle loop and Spawn.
    idle = []
    for i, f in enumerate(range(0, IDLE_FRAMES, IDLE_FRAMES // 4)):
        C.set_action(arm, actions["Idle"], f)
        idle.append(game_shot(os.path.join(d, f"idle_{i}.png"), zoom * 1.4, 640, 400))
    C.contact_sheet(idle, os.path.join(d, "idle_strip.png"), 4, 400)
    spawn = []
    for i, f in enumerate((0, 4, 6, 10, 18, 30)):
        C.set_action(arm, actions["Spawn"], f)
        spawn.append(game_shot(os.path.join(d, f"spawn_{i}.png"), zoom * 1.4, 640, 400))
    C.contact_sheet(spawn, os.path.join(d, "spawn_strip.png"), 3, 400)
    # Close-up of the open maw.
    C.set_action(arm, actions["Idle"], 0)
    rig = C.light_rig(scale=1.2 * o.S + 0.3)
    cam = C.closeup_camera(Vector((0, 0, 0.12 * o.S)), 3.2 * o.S + 0.6, azimuth=-28, elevation=38, lens=50)
    paths.append(C.render(cam, os.path.join(d, "closeup.png"), 1280, 800))
    bpy.data.objects.remove(cam)
    cam = C.closeup_camera(Vector((0, 0, 0.12 * o.S)), 1.4 * o.S + 0.25, azimuth=-50, elevation=30, lens=60)
    paths.append(C.render(cam, os.path.join(d, "macro.png"), 1280, 800))
    bpy.data.objects.remove(cam)
    for x in rig:
        bpy.data.objects.remove(x)
    # Game zoom with Skitters for scale (one climbing out at Emerge).
    sk = skitters([((0.95 * o.S + 0.5, -0.5, 0.0), 0.6), ((-0.9 * o.S - 0.6, 0.3, 0.0), -2.2),
                   ((0.0, 0.0, -0.08), 0.3)])
    paths.append(game_shot(os.path.join(d, "scale_play.png"), 1.0, 1280, 720))
    paths.append(game_shot(os.path.join(d, "scale_zoom.png"), zoom, 960, 540))
    C.remove_objects(sk)
    C.contact_sheet(paths, os.path.join(d, "sheet.png"), 3, 480)
    C.remove_objects(world_objs)
    C.uv_layout(asset.low, os.path.join(d, "uv.png"), 1024, background=asset.textures["albedo"])


def main():
    args = C.cli_args()
    only = args.get("only")
    kinds = only.split(",") if only else list(KINDS)
    out = args.get("preview") or os.path.join(C.REPO, "ArtSource", "Previews", "Origins2")
    os.makedirs(out, exist_ok=True)
    C.reset_scene(FPS)
    E.use_env_exports()
    grounds = {k: GroundSet(rel) for k, rel in GROUND_SETS.items()}
    for kind in kinds:
        o, asset, arm, lod, actions = build(kind, grounds)
        if not args.get("norender"):
            previews(o, asset, arm, actions, out)


main()
