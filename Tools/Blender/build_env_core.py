"""The Core environment set (round 17, continent 6: an underground crystal plateau; the seal). See
ArtSource/DESIGN_NOTES.md round 17.

Tiling sets (Exports/Env/TheCore/Tiling/Core_<Set>/): Road (pale lilac hexagonal slabs cut from the plateau),
Ground (indigo-black crystal glass in facets, fine gold seams), GroundAlt (crystal grit and shards), Plateau (pale
lilac-grey crystal stone with agate growth rings, the plateau the base stands on), Cliff (crystal-glass faces with
gold veins running down, V up).
Kit: RoadEdge_* (dark crystal-glass kerb with a gold foot line), Bank_* (rim of a violet corruption pool),
Cliff_* (plateau edges: the field breaks off and the faces drop 2.4 into the abyss; field on -Y, void on +Y, as
round 8's Chasm_Edge).
Set pieces: Seal_Intact, Seal_Cracked, Seal_Broken (7.2 across, pivot at the centre on the ground, 1024 maps),
SealVein_Straight (2 x 0.5) and SealVein_Branch (a Y junction), flat, lying on the ground.
Decor: CorruptedCrystal, CrystalPillar, SealShard, HexOutcrop, Scatter_CrystalGrit.

Run: blender --background --factory-startup --python Tools/Blender/build_env_core.py -- --preview <dir>
     [--part materials|kit|decor|seal|diorama|all] [--only Seal_Intact] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
from env_kit import bn, ss, mix, rgb  # noqa: E402

CONT = K.Continent("TheCore", "Core", {"Road": 2.0, "Ground": 3.0, "GroundAlt": 2.0, "Plateau": 3.0, "Cliff": 3.0})

GOLD = np.array((1.0, 0.74, 0.32))
GOLD_HOT = np.array((1.0, 0.92, 0.7))
VIOLET = np.array((0.62, 0.22, 1.0))
LILAC = np.array((0.58, 0.55, 0.66))

GLASS = K.pal((0.24, 0.22, 0.34), (0.11, 0.1, 0.17), (0.03, 0.02, 0.05), (0.02, 0.015, 0.04))
PLATE = K.pal((0.56, 0.53, 0.64), (0.34, 0.31, 0.42), (0.08, 0.07, 0.11), (0.07, 0.06, 0.1))
SEAL_STONE = K.pal((0.34, 0.31, 0.42), (0.17, 0.15, 0.23), (0.04, 0.03, 0.06), (0.03, 0.02, 0.05))


# ------------------------------------------------------------------------------------------------ tiling sets

def hex_cells(n, cols, rows, seed, jitter=0.12):
    """Near-regular hexagonal cells: the voronoi of a staggered lattice, periodic on the tile (rows even). Returns
    (F1, F2 - F1, value per cell) with distances in column units, like E.voronoi."""
    u, v = E.coords(n)
    rng = np.random.default_rng(seed)
    best1 = np.full((n, n), np.inf)
    best2 = np.full((n, n), np.inf)
    vid = np.zeros((n, n))
    vals = rng.uniform(0, 1, (rows, cols))
    jit = rng.uniform(-jitter, jitter, (rows, cols, 2))
    for j in range(rows):
        for i in range(cols):
            cx = (i + 0.5 * (j % 2) + 0.25 + jit[j, i, 0]) / cols
            cy = (j + 0.5 + jit[j, i, 1]) / rows
            dx = (u - cx + 0.5) % 1.0 - 0.5
            dy = (v - cy + 0.5) % 1.0 - 0.5
            d = np.hypot(dx, dy) * cols
            closer = d < best1
            best2 = np.where(closer, best1, np.minimum(best2, d))
            vid = np.where(closer, vals[j, i], vid)
            best1 = np.where(closer, d, best1)
    return best1, best2 - best1, vid


def road(n):
    """Pale lilac hexagonal slabs cut from the plateau; dark joints with a little gold dust in them."""
    d1, edge, val = hex_cells(n, 2, 2, 911)
    joint = 1 - ss(0.0, 0.05, edge)
    bevel = ss(0.03, 0.16, edge)
    _, e2, _, _ = E.voronoi(n, 9, 912, warp=0.25)
    crack = (1 - ss(0.0, 0.016, e2)) * (bn(n, 1, 4, 913) > 0.3) * bevel
    scuff = E.scratches(n, 70, 914)
    pits = E.D.pits(n, 50, 915, 0.25, radius=0.3)
    g, gv = E.grains(n, 300, 916, 0.6, radius=0.36)
    gold = g * (1 - ss(0.02, 0.07, edge)) * (gv < 0.3)
    h = (0.01 * bevel + 0.003 * (val - 0.5) - 0.012 * joint - 0.003 * crack + 0.0012 * bn(n, 40, 160, 917)
         + 0.0015 * bn(n, 6, 20, 918) - 0.0015 * pits + 0.003 * gold)
    tone = 0.9 + 0.18 * (val - 0.5) + 0.05 * bn(n, 3, 10, 919)
    col = rgb((0.57, 0.54, 0.64), n) * tone[..., None]
    col = mix(col, rgb((0.26, 0.23, 0.32), n), 0.45 * (1 - ss(0.04, 0.2, edge)))
    col = mix(col, rgb((0.06, 0.05, 0.09), n), joint)
    col = mix(col, rgb((0.18, 0.16, 0.24), n), 0.7 * crack)
    col = mix(col, rgb((0.34, 0.32, 0.4), n), 0.5 * pits)
    col = mix(col, rgb(GOLD * 0.8, n), gold)
    col = mix(col, rgb((0.76, 0.74, 0.82), n), 0.5 * scuff)
    smooth = np.clip(0.45 - 0.3 * joint - 0.12 * crack + 0.1 * scuff + 0.2 * gold, 0.05, 1)
    emission = rgb(GOLD, n) * (gold * 0.5)[..., None]
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=gold, ao=ao, emission=emission)


def ground(n):
    """Indigo-black crystal glass broken into facets; fine gold seams glow in some joints."""
    d1, e, val = hex_cells(n, 7, 8, 931, jitter=0.3)
    seam = 1 - ss(0.0, 0.035, e)
    _, e2, v2, _ = E.voronoi(n, 13, 932, warp=0.3)
    fine = (1 - ss(0.0, 0.02, e2)) * (1 - seam)
    tilt = (val * 9.3) % 1.0
    lit = ss(0.2, 0.6, bn(n, 2, 6, 933))
    h = 0.01 * tilt - 0.012 * seam - 0.003 * fine + 0.008 * bn(n, 1, 4, 934) + 0.001 * bn(n, 40, 200, 935)
    col = rgb((0.1, 0.09, 0.16), n) * (0.7 + 0.45 * tilt)[..., None]
    col = mix(col, rgb((0.02, 0.02, 0.04), n), seam * 0.9)
    col = mix(col, rgb((0.06, 0.05, 0.1), n), fine * 0.6)
    glow = seam * lit
    col = mix(col, rgb(GOLD * 0.55, n), glow * 0.8)
    smooth = np.clip(0.3 + 0.15 * tilt - 0.2 * seam, 0.05, 0.95)
    emission = rgb(GOLD, n) * (glow * 0.6)[..., None]
    ao = E.cavity_ao(h, 6, 0.008)
    return dict(albedo=col, height=h, smooth=smooth, dust=glow, ao=ao, emission=emission)


def ground_alt(n):
    """Crystal grit and shards: dark grit, lilac and violet splinters, a few gold glints."""
    g1, v1 = E.grains(n, 90, 941, 0.7, radius=0.45, sharp=3)
    g2, v2 = E.grains(n, 260, 942, 0.8, radius=0.4, sharp=3)
    h = 0.01 * g1 + 0.005 * g2 + 0.006 * bn(n, 1, 4, 943) + 0.001 * bn(n, 40, 200, 944)
    col = rgb((0.08, 0.07, 0.12), n)
    col = mix(col, rgb((0.3, 0.27, 0.38), n) * (0.7 + 0.6 * ((v2 * 13) % 1))[..., None], g2)
    col = mix(col, rgb((0.4, 0.36, 0.5), n) * (0.7 + 0.5 * ((v1 * 17) % 1))[..., None], g1 * (v1 >= 0.2))
    vio = g1 * (v1 < 0.12)
    gold = g2 * (v2 < 0.04)
    col = mix(col, rgb(VIOLET * 0.6, n), vio)
    col = mix(col, rgb(GOLD * 0.7, n), gold)
    smooth = np.clip(0.3 + 0.4 * g1, 0.03, 1)
    emission = rgb(VIOLET, n) * (vio * 0.45)[..., None] + rgb(GOLD, n) * (gold * 0.7)[..., None]
    ao = E.cavity_ao(h, 4, 0.005)
    return dict(albedo=col, height=h, smooth=smooth, dust=np.clip(vio + gold, 0, 1), ao=ao,
                emission=np.clip(emission, 0, 1))


def plateau(n):
    """Pale lilac-grey crystal stone with agate growth rings (the plateau the base stands on)."""
    d1, e, val, _ = E.voronoi(n, 3, 951, jitter=0.9, warp=0.2)
    rings = 0.5 + 0.5 * np.sin(d1 * 60 + 2 * bn(n, 2, 6, 952))
    joint = 1 - ss(0.0, 0.025, e)
    _, e2, _, _ = E.voronoi(n, 11, 953, warp=0.3)
    crack = (1 - ss(0.0, 0.014, e2)) * (bn(n, 1, 4, 954) > 0.2)
    h = 0.004 * rings - 0.01 * joint - 0.003 * crack + 0.008 * bn(n, 1, 4, 955) + 0.001 * bn(n, 40, 200, 956)
    col = rgb((0.4, 0.38, 0.47), n) * (0.85 + 0.15 * rings + 0.15 * (val - 0.5))[..., None]
    col = mix(col, rgb((0.3, 0.27, 0.38), n), 0.4 * ss(0.6, 1.0, rings))
    col = mix(col, rgb((0.07, 0.06, 0.1), n), joint)
    col = mix(col, rgb((0.18, 0.16, 0.24), n), crack * 0.7)
    smooth = np.clip(0.45 + 0.1 * rings - 0.3 * joint, 0.05, 1)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=crack, ao=ao)


def cliff(n):
    """Crystal-glass faces (V up): tall facets, gold veins running down them, violet in a few fractures."""
    d1, e, val, _ = E.voronoi(n, 3, 961, jitter=0.4, stretch=(3, 1))
    u, v = E.coords(n)
    facet = 1 - ss(0.0, 0.03, e)
    vein = (1 - ss(0.0, 0.012, np.abs(bn(n, 2, 6, 962) + 0.4 * (u - 0.5) * 0))) * ss(0.2, 0.5, bn(n, 1, 3, 963))
    vio = (1 - ss(0.0, 0.01, np.abs(bn(n, 3, 8, 964)))) * ss(0.4, 0.7, bn(n, 1, 3, 965))
    h = 0.03 * ((val * 7.7) % 1.0) - 0.03 * facet + 0.004 * bn(n, 30, 120, 966)
    col = rgb((0.15, 0.14, 0.23), n) * (0.75 + 0.5 * ((val * 7.7) % 1.0))[..., None]
    col = mix(col, rgb((0.03, 0.02, 0.05), n), facet * 0.9)
    col = mix(col, rgb(GOLD * 0.6, n), vein)
    col = mix(col, rgb(VIOLET * 0.5, n), vio)
    smooth = np.clip(0.6 - 0.3 * facet, 0.05, 0.95)
    emission = rgb(GOLD, n) * (vein * 0.75)[..., None] + rgb(VIOLET, n) * (vio * 0.6)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=vein, ao=ao, emission=np.clip(emission, 0, 1))


SETS = {"Road": road, "Ground": ground, "GroundAlt": ground_alt, "Plateau": plateau, "Cliff": cliff}


# ------------------------------------------------------------------------------------------------ kit

def kerb_style(seed, road_dir=(0.0, -1.0)):
    base = K.rock(GLASS, seed, strata=0.0, crack=0.5, rough=0.35, smooth_top=0.15)
    rd = np.array(road_dir) / np.linalg.norm(road_dir)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        facing = C.smoothstep(0.1, 0.6, n[:, 0] * rd[0] + n[:, 1] * rd[1])
        low = 1 - C.smoothstep(0.0, 0.05, p[:, 2])
        f = facing * low
        top = C.smoothstep(0.75, 0.95, n[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.36, 0.33, 0.46)), top * 0.6)
        s.albedo[idx] = C.lerp3(s.albedo[idx], GOLD * 0.55, f)
        s.emit[idx] = np.clip(s.emit[idx] + GOLD[None, :] * (f * 0.7)[:, None], 0, 1)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def pool_style(seed):
    """Rim of a corruption pool: dark glass with a violet crust at the waterline."""
    base = K.rock(GLASS, seed, strata=40, crack=0.6, rough=0.3, crack_glow=0.4, crack_color=VIOLET)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        crust = 1 - C.smoothstep(-0.02, 0.03, p[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], VIOLET * 0.45, crust)
        s.emit[idx] = np.clip(s.emit[idx] + VIOLET[None, :] * (crust * 0.7)[:, None], 0, 1)
    return style


def edge_style(seed):
    """Plateau edge: crystal glass, lighter at the lip, gold veins running down the face, violet light from below."""
    base = K.rock(GLASS, seed, strata=8, crack=0.4, rough=0.45, veins=0.4, glow=GOLD,
                  depth_glow=(-0.5, -2.2, VIOLET, 0.45))

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        lip = C.smoothstep(-0.25, 0.0, p[:, 2]) * C.smoothstep(0.5, 0.9, n[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], LILAC * 0.8, lip * 0.7)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def edge_profile(t, s):
    """Field on d > 0 (flush with the ground), the lip, then the face dropping 2.4 with facet steps."""
    pts = [(0.45, -0.04), (0.42, 0.0), (0.1, 0.02), (0.02, 0.0), (-0.06, -0.12), (-0.02, -0.5), (-0.14, -0.62),
           (-0.12, -1.2), (-0.26, -1.35), (-0.22, -2.0), (-0.34, -2.4)]
    return K.polyline(pts, s)


def edge_inner(t, s):
    return K.polyline([(0.4, -0.3), (0.2, -0.6), (0.05, -2.3)], s)


def edge_jitter(x, s, d, z):
    if z > -0.05:
        return 0.0, 0.0
    return 0.05 * E.periodic(x * 2, 40.0, 4, 9601 + int(-z * 3)), 0.0


def plateau_edge(kind):
    b = C.PartBuilder()
    rng = np.random.default_rng({"straight": 9610, "outer": 9620, "inner": 9630}[kind])
    path, length = K.cliff_sweep(b, "Edge", kind, edge_profile, edge_inner, edge_style(9611), edge_jitter,
                                 radius=0.6 if kind == "outer" else 0.4, nv=80)
    count = 4 if kind == "straight" else 2
    for i in range(count):
        t = (i + 0.5) / count + rng.uniform(-0.06, 0.06)
        base = K.along(path, t, -0.12, rng.uniform(-1.6, -0.4))
        (_, _), (nx, ny) = path(t)
        out = Vector((-nx, -ny, -0.35)).normalized()
        b.shard("Facet", base, out, rng.uniform(0.07, 0.11), rng.uniform(0.3, 0.55), sides=6, twist=0.6, flute=0.25,
                style=Y.shard_style("seal" if i % 2 == 0 else "corrupt", 0.8, seed=9640 + i))
    for i in range(2):
        t = rng.uniform(0.15, 0.85)
        K.chip(b, K.along(path, t, 0.28, 0.0), (0.06, 0.05, 0.035), 9650 + i, edge_style(9650 + i))
    return b


# ------------------------------------------------------------------------------------------------ the seal

GOLD_AMOUNT = {"Intact": 1.0, "Cracked": 0.75, "Broken": 0.35}
CRACKS = {"Intact": 0.0, "Cracked": 0.85, "Broken": 1.0}


def seal_style(seed, state, band=0):
    """Ancient carved stone: glyph rows engraved along each band and eight radial veins, inlaid with gold Crysium;
    in the cracked and broken states a violet crack network leaks through."""
    gold_k = GOLD_AMOUNT[state]
    crack_k = CRACKS[state]

    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        r = np.hypot(p[:, 0], p[:, 1])
        th = np.arctan2(p[:, 1], p[:, 0])
        top = C.smoothstep(0.6, 0.9, n[:, 2])
        col = C.chitin_gradient(n, SEAL_STONE["top"], SEAL_STONE["side"], SEAL_STONE["bottom"])
        col = col * (0.88 + 0.16 * K.vn(p, 5, seed))[:, None]
        # Glyph rows on every other quarter-unit ring (bigger, sparser marks, so it reads as script, not as
        # perforated metal); the rows between carry only a fine ring line.
        rr = r * 4.0
        row = np.floor(rr)
        fr = rr % 1.0
        cells = np.maximum(np.round(2 * math.pi * r / 0.42), 6)
        ct = (th / (2 * math.pi) % 1.0) * cells
        cell = np.floor(ct)
        ft = ct % 1.0
        hv = (np.sin(cell * 12.9898 + row * 78.233) * 43758.5453) % 1.0
        hv2 = (np.sin(cell * 39.3468 + row * 11.135) * 24634.6345) % 1.0
        on = (row % 2 == 1) & (hv > 0.18)
        bar_t = (np.abs(ft - (0.3 + 0.4 * hv2)) < 0.07) & (fr > 0.18) & (fr < 0.82)
        bar_r = (np.abs(fr - 0.5) < 0.1) & (np.abs(ft - 0.5) < 0.3) & (hv < 0.6)
        hook = (np.abs(fr - 0.25) < 0.08) & (ft > 0.5) & (ft < 0.85) & (hv2 > 0.55)
        glyph = ((bar_t | bar_r | hook) & on).astype(float) * top
        ring = (1 - C.smoothstep(0.0, 0.025, np.abs(fr - 0.03))) * top * 0.8
        radial = (1 - C.smoothstep(0.025, 0.05, np.abs(np.sin(th * 4)) * r)) * top
        inlay = np.clip(glyph + ring + radial, 0, 1)
        col = C.lerp3(col, GOLD * (0.45 + 0.3 * gold_k), inlay)
        emit = GOLD[None, :] * (inlay * gold_k)[:, None]
        cr = np.zeros(len(idx))
        if crack_k:
            cr = (1 - C.smoothstep(0.0, 0.03, np.abs(K.vn(p, 2.6, seed + 11)))) * C.smoothstep(-0.25, 0.35, K.vn(p, 0.9, seed + 12))
            cr = cr * crack_k
            col = C.lerp3(col, VIOLET * 0.45, cr)
            emit = emit * (1 - cr)[:, None] + VIOLET[None, :] * cr[:, None]
        s.albedo[idx] = col
        s.emit[idx] = np.clip(emit, 0, 1)
        s.disp[idx] = -0.008 * glyph - 0.006 * radial - 0.012 * cr
        s.rough[idx] = 0.62 - 0.25 * inlay
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = r
    return style


def band_segment(b, r0, r1, height, a0, a1, style, hinge=None):
    """One curved segment of a seal band between radii r0 and r1 and angles a0..a1 (radians), chamfered top.
    `hinge` = (angle deg, lift) tips the segment up about its outer edge (the broken state)."""
    ch = min(0.06, (r1 - r0) * 0.15)
    prof = [(r0, -0.12), (r0, height - ch), (r0 + ch, height), (r1 - ch, height), (r1, height - ch), (r1, -0.12)]
    gap = 0.012 / max(r1, 0.3)
    a0, a1 = a0 + gap, a1 - gap
    am = (a0 + a1) / 2
    tf = None
    if hinge:
        tip, lift = hinge
        axis = Vector((-math.sin(am), math.cos(am), 0))
        pivot = Vector((math.cos(am) * r1, math.sin(am) * r1, 0))
        R = Matrix.Rotation(math.radians(tip), 3, axis)

        def tf(p):
            return pivot + R @ (p - pivot) + Vector((0, 0, lift))

    def outer(u, v):
        a = a0 + (a1 - a0) * u
        r, z = K.polyline(prof, v)
        return Vector((math.cos(a) * r, math.sin(a) * r, z))

    def inner(u, v):
        a = a0 + (a1 - a0) * u
        r = r0 + 0.02 + (r1 - r0 - 0.04) * v
        return Vector((math.cos(a) * r, math.sin(a) * r, -0.14))

    nu = max(6, int((a1 - a0) * r1 * 22))
    b.slab("Band", outer, inner, nu, 14, style=style, transform=tf)


BANDS = ((2.75, 3.45, 0.26, 12), (1.85, 2.45, 0.22, 8), (1.0, 1.5, 0.18, 6))


def seal(state):
    b = C.PartBuilder()
    rng = np.random.default_rng({"Intact": 9701, "Cracked": 9702, "Broken": 9703}[state])
    # The light floor under the bands: what glows through the joints.
    floor_col = GOLD if state != "Broken" else VIOLET
    floor_k = {"Intact": 0.55, "Cracked": 0.45, "Broken": 0.6}[state]
    b.ellipsoid("Floor", Vector((0, 0, -0.04)), (3.55, 3.55, 0.05), style=K.glow_seam(floor_col, floor_k, floor_col * 0.4),
                segments=(64, 8))
    for k, (r0, r1, h, count) in enumerate(BANDS):
        for i in range(count):
            a0 = 2 * math.pi * i / count + 0.13 * k
            a1 = 2 * math.pi * (i + 1) / count + 0.13 * k
            hinge = None
            hh = h
            if state == "Cracked" and rng.uniform() < 0.25:
                hinge = (rng.uniform(-3, 3), rng.uniform(0.0, 0.03))
            if state == "Broken":
                if rng.uniform() < 0.6:
                    hinge = (rng.uniform(-30, -12) if k > 0 else rng.uniform(-20, 8), rng.uniform(0.02, 0.16))
                if k == 2 and rng.uniform() < 0.5:
                    continue  # the inner band partly fell into the pit
            band_segment(b, r0, r1, hh, a0, a1, seal_style(9710 + k * 20 + i, state, k), hinge)
    # Spokes joining the bands (eight, along the radial veins).
    for i in range(8):
        a = i * math.pi / 4
        if state == "Broken" and i % 3 == 1:
            continue
        for r_a, r_b in ((1.5, 1.85), (2.45, 2.75)):
            c = Vector((math.cos(a) * (r_a + r_b) / 2, math.sin(a) * (r_a + r_b) / 2, 0.06))
            b.ellipsoid("Spoke", c, ((r_b - r_a) / 2 + 0.04, 0.12, 0.12), K.rz(math.degrees(a)),
                        style=seal_style(9790 + i, state), segments=(16, 8), deform=E.boxy(9790 + i, 0.2, 2, 0.05))
    if state != "Broken":
        b.ellipsoid("Boss", Vector((0, 0, 0.0)), (0.82, 0.82, 0.3), style=seal_style(9800, state), segments=(48, 16),
                    deform=K_dome())
        crystal = "seal" if state == "Intact" else "corrupt"
        for i in range(5 if state == "Intact" else 4):
            a = i / 5 * 2 * math.pi
            d = Vector((math.cos(a) * 0.35, math.sin(a) * 0.35, 1)) if i else Vector((0, 0, 1))
            b.shard("Heart", Vector((math.cos(a) * 0.15, math.sin(a) * 0.15, 0.2)) if i else Vector((0, 0, 0.22)), d,
                    0.12 if i == 0 else 0.07, 0.65 if i == 0 else 0.35, sides=8, twist=1.2, flute=0.3,
                    style=Y.shard_style(crystal if i else "seal", 1.0, seed=9810 + i))
    else:
        # The centre collapsed into a pit glowing violet; corrupted crystal grows from it.
        def pit_outer(u, v):
            a = 2 * math.pi * u
            r = 0.95 - 0.85 * v
            z = 0.06 - 0.7 * math.sin(v * math.pi / 2) ** 1.4 + 0.03 * math.sin(a * 5)
            return Vector((math.cos(a) * r, math.sin(a) * r, z))

        def pit_inner(u, v):
            a = 2 * math.pi * u
            r = 1.05 - 0.9 * v
            return Vector((math.cos(a) * r, math.sin(a) * r, -0.16 - 0.7 * v))

        b.slab("Pit", pit_outer, pit_inner, 48, 12, style=pit_style(9820))
        for i in range(5):
            a = i / 5 * 2 * math.pi + 0.3
            b.shard("Corrupt", Vector((math.cos(a) * 0.3, math.sin(a) * 0.3, -0.5)),
                    Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, 1)), 0.08, rng.uniform(0.45, 0.8), sides=7, twist=1.6,
                    flute=0.35, style=Y.shard_style("corrupt", 1.1, seed=9830 + i))
        for i in range(4):
            a = rng.uniform(0, 2 * math.pi)
            r = rng.uniform(1.6, 3.3)
            K.chip(b, (math.cos(a) * r, math.sin(a) * r, 0.0), (0.18, 0.14, 0.1), 9840 + i, seal_style(9840 + i, state))
    return b


def K_dome():
    def deform(v):
        z = v.z if v.z > 0 else v.z * 0.4
        return Vector((v.x, v.y, z))
    return deform


def pit_style(seed):
    def style(s, idx, part):
        p = s.p[idx]
        deep = C.smoothstep(-0.1, -0.6, p[:, 2])
        col = C.lerp3(SEAL_STONE["side"], VIOLET * 0.5, deep)
        s.albedo[idx] = col * (0.85 + 0.2 * K.vn(p, 8, seed))[:, None]
        s.emit[idx] = np.clip(VIOLET[None, :] * (deep * 0.9)[:, None], 0, 1)
        s.disp[idx] = 0.006 * K.vn(p, 10, seed + 1)
        s.rough[idx] = 0.5
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = p[:, 2]
    return style


def vein_style(seed):
    base = K.rock(PLATE, seed, strata=0.0, crack=0.3, rough=0.4)

    def style(s, idx, part):
        base(s, idx, part)
        s.fam[idx] = E.FAM_ROCK
    return style


def vein_run(b, x0, x1, seed, y_wob=0.0):
    """A groove in the plateau along X: two low carved lips and gold crystal packed in the channel."""
    L = x1 - x0
    for sign in (1.0, -1.0):
        def out(t, s):
            d, z = K.polyline([(-0.22, -0.03), (-0.12, 0.025), (-0.05, 0.03), (-0.02, 0.0), (0.0, -0.04)], s)
            w = min(1.0, math.sin(math.pi * t) * 3)
            return d, z * w - 0.02 * (1 - w)

        def inn(t, s):
            return K.polyline([(-0.2, -0.06), (-0.02, -0.06)], s)

        def path(t, sign=sign):
            x = x0 + L * t
            return (x, -sign * (0.05 + y_wob * math.sin(t * 7 + seed))), (0.0, sign)
        E.sweep(b, "Lip", path, out, inn, max(30, int(L * 40)), 10, vein_style(seed + int(sign)), None)
    count = max(2, int(L / 0.22))
    rng = np.random.default_rng(seed)
    for i in range(count):
        x = x0 + (i + 0.5) * L / count
        b.shard("Gold", Vector((x, rng.uniform(-0.015, 0.015), -0.04)), Vector((rng.uniform(-0.4, 0.4), rng.uniform(-0.3, 0.3), 1)),
                0.035, rng.uniform(0.07, 0.11), sides=6, twist=1.0, flute=0.3, style=Y.shard_style("seal", 1.0, seed=seed + 10 + i))
    b.ellipsoid("Glow", Vector(((x0 + x1) / 2, 0, -0.02)), (L / 2 - 0.04, 0.045, 0.03),
                style=K.glow_seam(GOLD, 1.0, GOLD * 0.6), segments=(40, 8))


def seal_vein_straight():
    b = C.PartBuilder()
    vein_run(b, -1.0, 1.0, 9910)
    return b


def seal_vein_branch():
    """A Y junction: the main vein along X and a branch leaving at 40 degrees towards +Y."""
    b = C.PartBuilder()
    vein_run(b, -0.75, 0.75, 9920)
    sub = C.PartBuilder()
    vein_run(sub, 0.24, 0.95, 9930)
    sub.ellipsoid("Join", Vector((0.14, 0.0, -0.02)), (0.16, 0.045, 0.03), style=K.glow_seam(GOLD, 1.0, GOLD * 0.6),
                  segments=(20, 8))
    rot = Matrix.Rotation(math.radians(40), 4, "Z")
    for v in sub.bm.verts:
        v.co = rot @ v.co
    # Merge the branch into the main builder, keeping each part's style.
    offset = len(b.parts)
    import bmesh as _bm  # noqa: F401
    tmp = bpy.data.meshes.new("BranchTmp")
    sub.bm.to_mesh(tmp)
    b.bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
    for part in sub.parts:
        part.pid += offset
        if part.axis is not None:
            part.axis = np.array([tuple(rot.to_3x3() @ Vector(a)) for a in part.axis])
        part.frame = part.frame @ rot.inverted()
        b.parts.append(part)
    b.face_pid.extend([pid + offset for pid in sub.face_pid])
    return b


# ------------------------------------------------------------------------------------------------ decor

def corrupted_crystal():
    """Dark crystal sick with violet light, fractured, oozing a glossy black-violet resin at its foot."""
    b = C.PartBuilder()
    rng = np.random.default_rng(10001)

    def ooze(s, idx, part):
        p = s.p[idx]
        s.albedo[idx] = np.array((0.05, 0.02, 0.08)) * (1 + 0.3 * K.vn(p, 12, 10002))[:, None]
        s.emit[idx] = np.clip(VIOLET[None, :] * (0.25 * C.smoothstep(0.0, 0.5, K.vn(p, 6, 10003)))[:, None], 0, 1)
        s.disp[idx] = 0.004 * K.vn(p, 20, 10004)
        s.rough[idx] = 0.15
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = p[:, 2]

    b.ellipsoid("Ooze", Vector((0, 0, 0.0)), (0.38, 0.32, 0.07), style=ooze, segments=(32, 12))
    for i in range(6):
        a = i / 6 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        length = rng.uniform(0.35, 0.6) * (1.5 if i == 0 else 1.0)
        b.shard("Shard", out * (0.03 if i == 0 else 0.14), (out * (0.1 if i == 0 else 0.6) + Vector((0, 0, 1))),
                rng.uniform(0.06, 0.09) * (1.4 if i == 0 else 1.0), length, sides=6, twist=rng.uniform(-1.5, 1.5),
                flute=0.3, bend=out * 0.1 * (1 if i % 2 else -1), style=Y.shard_style("corrupt", 1.0, seed=10010 + i))
    for i in range(3):
        a = rng.uniform(0, 2 * math.pi)
        b.ellipsoid("Drip", Vector((math.cos(a) * 0.3, math.sin(a) * 0.26, 0.02)), (0.06, 0.05, 0.05), style=ooze,
                    segments=(12, 8))
    return b


def crystal_pillar():
    """A tall pillar of pale plateau crystal, a gold vein spiralling up it, a small crown of seal crystal."""
    b = C.PartBuilder()

    def pillar_style(s, idx, part):
        p = s.p[idx]
        base = K.rock(PLATE, 10110, strata=10, crack=0.3, rough=0.4)
        base(s, idx, part)
        th = np.arctan2(p[:, 1], p[:, 0])
        spiral = 1 - C.smoothstep(0.02, 0.06, np.abs(((th / (2 * math.pi) + p[:, 2] * 0.55) % 1.0) - 0.5))
        s.albedo[idx] = C.lerp3(s.albedo[idx], GOLD * 0.6, spiral)
        s.emit[idx] = np.clip(s.emit[idx] + GOLD[None, :] * (spiral * 0.9)[:, None], 0, 1)
        s.disp[idx] -= 0.006 * spiral
        s.fam[idx] = E.FAM_CRYSTAL

    b.prism("Pillar", Vector((0, 0, -0.05)), Vector((0.03, 0.02, 1)), 0.26, 1.55, 0.35, sides=6, twist=0.5,
            style=pillar_style, taper=0.62)
    for i in range(3):
        a = i / 3 * 2 * math.pi + 0.3
        out = Vector((math.cos(a), math.sin(a), 0))
        b.shard("Crown", out * 0.08 + Vector((0, 0, 1.45)), out * 0.7 + Vector((0, 0, 1)), 0.05, 0.3, sides=6, twist=1.2,
                flute=0.3, style=Y.shard_style("seal", 1.0, seed=10120 + i))
    for i in range(3):
        a = i / 3 * 2 * math.pi + 1.1
        K.chip(b, (math.cos(a) * 0.36, math.sin(a) * 0.32, 0.0), (0.12, 0.1, 0.09), 10130 + i, vein_style(10130 + i))
    return b


def seal_shard():
    """A fallen fragment of a seal band, tipped and half sunk in the plateau, its inlay still faintly gold."""
    b = C.PartBuilder()
    sub_style = seal_style(10210, "Cracked")
    b2 = C.PartBuilder()
    band_segment(b2, 2.75, 3.45, 0.26, 0.0, 0.5, sub_style, hinge=None)
    # Move the fragment from the ring to the origin, tipped.
    mid = Vector((math.cos(0.25) * 3.1, math.sin(0.25) * 3.1, 0.0))
    R = (Matrix.Rotation(math.radians(-25), 4, "X") @ Matrix.Rotation(-0.25 - math.pi / 2, 4, "Z")
         @ Matrix.Translation(-mid))
    for v in b2.bm.verts:
        v.co = R @ v.co
    for part in b2.parts:
        part.frame = part.frame @ R.inverted()
    tmp = bpy.data.meshes.new("ShardTmp")
    b2.bm.to_mesh(tmp)
    b.bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
    b.parts.extend(b2.parts)
    b.face_pid.extend(b2.face_pid)
    # The seal style reads world radius and angle: keep its pattern by styling in the band's own frame.
    for part in b.parts:
        part.style = _framed(sub_style, R.inverted())
    rng = np.random.default_rng(10220)
    for i in range(3):
        K.chip(b, (rng.uniform(-0.4, 0.4), rng.uniform(-0.35, 0.35), 0.0), (0.08, 0.07, 0.05), 10220 + i, vein_style(10220 + i))
    return b


def _framed(style, to_band):
    """Runs a style on points mapped back into another frame (so a moved piece keeps its original pattern)."""
    M = np.array(to_band)

    def wrapped(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        q = (M[:3, :3] @ p.T).T + M[:3, 3]
        nn = (M[:3, :3] @ n.T).T
        saved_p, saved_n = s.p, s.n
        s.p = s.p.copy()
        s.n = s.n.copy()
        s.p[idx] = q
        s.n[idx] = nn
        style(s, idx, part)
        s.p, s.n = saved_p, saved_n
    return wrapped


def hex_outcrop():
    """A knot of short hexagonal crystal columns pushing up through the plateau, gold seams in their tops."""
    b = C.PartBuilder()
    rng = np.random.default_rng(10301)
    for i in range(7):
        a = i / 7 * 2 * math.pi + rng.uniform(-0.2, 0.2)
        r = 0.0 if i == 0 else rng.uniform(0.16, 0.26)
        h = rng.uniform(0.25, 0.55) * (1.4 if i == 0 else 1.0)
        b.prism("Column", Vector((math.cos(a) * r, math.sin(a) * r, -0.05)),
                Vector((math.cos(a) * 0.12 * (r > 0), math.sin(a) * 0.12 * (r > 0), 1)), rng.uniform(0.09, 0.12), h, 0.04,
                sides=6, twist=rng.uniform(0, 1), style=K.rock(PLATE, 10310 + i, strata=0.0, crack=0.4, veins=0.6, glow=GOLD,
                                                                rough=0.4), taper=0.95)
    return b


def crystal_grit():
    b = C.PartBuilder()
    rng = np.random.default_rng(10401)
    for i in range(4):
        c = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), -0.01))
        b.shard("Grit", c, Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), 1)), 0.025, rng.uniform(0.06, 0.11),
                sides=5, twist=0.6, flute=0.2, style=Y.shard_style(("corrupt", "seal", "ice", "corrupt")[i], 0.7, seed=10410 + i))
    return b


KIT = {
    "RoadEdge_Straight": dict(fn=lambda: K.kerb_straight(kerb_style(9110), 9110), tris=700, voxel=0.007,
                              families=("crystal", "rock")),
    "RoadEdge_OuterCorner": dict(fn=lambda: K.kerb_outer(kerb_style(9210, (-0.7, -0.7)), 9210), tris=500, voxel=0.007,
                                 families=("crystal", "rock")),
    "RoadEdge_InnerCorner": dict(fn=lambda: K.kerb_inner(kerb_style(9310, (-0.7, -0.7)), 9310), tris=400, voxel=0.007,
                                 families=("crystal", "rock")),
    "Bank_Straight": dict(fn=lambda: K.bank_straight(pool_style(9410), 9410), tris=600, voxel=0.008),
    "Bank_OuterCorner": dict(fn=lambda: K.bank_outer(pool_style(9510), 9510), tris=400, voxel=0.008),
    "Bank_InnerCorner": dict(fn=lambda: K.bank_inner(pool_style(9560), 9560), tris=300, voxel=0.008),
    "Cliff_Straight": dict(fn=lambda: plateau_edge("straight"), tris=1500, voxel=0.014, families=("crystal", "rock")),
    "Cliff_OuterCorner": dict(fn=lambda: plateau_edge("outer"), tris=1500, voxel=0.014, families=("crystal", "rock")),
    "Cliff_InnerCorner": dict(fn=lambda: plateau_edge("inner"), tris=1500, voxel=0.014, families=("crystal", "rock")),
}

SEAL = {
    "Seal_Intact": dict(fn=lambda: seal("Intact"), tris=8000, voxel=0.02, smoothing=1, tex=1024, k=3.0),
    "Seal_Cracked": dict(fn=lambda: seal("Cracked"), tris=8000, voxel=0.02, smoothing=1, tex=1024, k=3.0),
    "Seal_Broken": dict(fn=lambda: seal("Broken"), tris=8000, voxel=0.02, smoothing=1, tex=1024, k=3.0),
    "SealVein_Straight": dict(fn=seal_vein_straight, tris=600, voxel=0.008, families=("rock", "crystal")),
    "SealVein_Branch": dict(fn=seal_vein_branch, tris=800, voxel=0.008, families=("rock", "crystal")),
}

DECOR = {
    "Decor_CorruptedCrystal": dict(fn=corrupted_crystal, tris=1300, voxel=0.008, smoothing=1, sharp=40,
                                   families=("crystal", "membrane")),
    "Decor_CrystalPillar": dict(fn=crystal_pillar, tris=1200, voxel=0.01, smoothing=1, sharp=35,
                                families=("crystal", "rock")),
    "Decor_SealShard": dict(fn=seal_shard, tris=1200, voxel=0.01, smoothing=1),
    "Decor_HexOutcrop": dict(fn=hex_outcrop, tris=1200, voxel=0.008, smoothing=1, sharp=35, families=("crystal", "rock")),
    "Scatter_CrystalGrit": dict(fn=crystal_grit, tris=200, voxel=0.005, sharp=40, families=("crystal", "rock")),
}


def extras(dio, rng):
    """The seal (cracked) in the upper right of the field, veins leading away from it."""
    dio.place("Seal_Cracked", K.world((8.6, 5.9)), 0, scale=0.5)
    dio.place("SealVein_Straight", K.world((5.6, 7.2)), 10)
    dio.place("SealVein_Branch", K.world((7.6, 0.4)), 180)
    dio.place("SealVein_Straight", K.world((11.3, 4.4)), 80, scale=0.7)


DIORAMA = dict(
    sets=dict(ground="Ground", alt="GroundAlt", element="Plateau", road="Road"),
    alt_areas=[((1.0, 1.0), 1.8), ((8.0, 0.6), 1.4), ((3.0, 7.3), 1.6)],
    element_areas=[((8.6, 5.9), 2.5), ((5.6, 6.6), 1.6), ((0.3, 2.6), 1.3)],
    pool={(0, 1), (1, 1)},
    pads=[(0, 4), (1, 4), (2, 4), (2, 3), (2, 2), (5, 4), (6, 4), (5, 1), (6, 1), (9, 1), (10, 1)],
    liquid=dict(color=(0.06, 0.02, 0.1), rough=0.05, emit=(0.45, 0.12, 0.85), strength=0.5, z=-0.03),
    world=(0.006, 0.004, 0.014),
    mist=dict(density=0.25, color=(0.4, 0.3, 0.6)),
    void=(0.25, 0.1, 0.45),
    front_edge=True,
    cut_outside=True,
    lights=dict(key=1.6, rim=1.3, key_color=(0.82, 0.8, 1.0), rim_color=(0.6, 0.35, 1.0),
                points=[((8.6, 5.9), (1.0, 0.72, 0.3), 70), ((0.5, 1.0), (0.62, 0.22, 1.0), 25),
                        ((10, 2.5), (0.5, 0.75, 1.0), 25), ((4.2, 0.6), (0.62, 0.22, 1.0), 14)]),
    decor=[("Decor_CorruptedCrystal", (3.4, 0.6), 0, 1.0), ("Decor_CorruptedCrystal", (11.2, 0.8), 70, 0.8),
           ("Decor_CrystalPillar", (0.3, 7.0), 0, 1.0), ("Decor_CrystalPillar", (11.4, 6.9), 40, 0.9),
           ("Decor_SealShard", (4.5, 6.9), 30, 0.7), ("Decor_HexOutcrop", (0.2, 2.8), 0, 1.0),
           ("Decor_HexOutcrop", (7.6, 0.3), 70, 0.8), ("Decor_CorruptedCrystal", (2.6, 7.2), 0, 0.7),
           ("Decor_HexOutcrop", (4.3, 1.6), 0, 0.7)],
    scatter=["Scatter_CrystalGrit"],
    scatter_count=30,
    towers=[("Tower_Railgun", "T2", (2, 4)), ("Tower_Tesla", "T2", (6, 4)), ("Tower_Laser", "T2", (5, 1)),
            ("Tower_Cannon", "T2", (10, 1)), ("Tower_Gun", "T2", (9, 1))],
    enemies=[("Skitter", 0.45, "Corrupted", [(0.6, 5.7, -90), (2.4, 5.8, -95), (4.0, 3.2, 175)]),
             ("Cragback", 0.85, "Corrupted", [(1.3, 5.4, -90)]), ("Prowler", 0.7, "Corrupted", [(6.8, 2.6, -90)])],
    close_target=(8.0, 5.0),
    extras=extras,
)


if __name__ == "__main__":
    args = C.cli_args()
    if args.get("part") == "seal":
        C.reset_scene()
        K.run_assets(CONT, SEAL, C.preview_dir(), args.get("only"), not args.get("norender"), "Seal")
    else:
        if args.get("part", "all") == "all":
            C.reset_scene()
            K.run_assets(CONT, SEAL, C.preview_dir(), None, not args.get("norender"), "Seal")
        K.run(CONT, SETS, KIT, DECOR, DIORAMA, world=(0.02, 0.018, 0.035))
