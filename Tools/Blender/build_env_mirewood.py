"""Mirewood environment set (round 17, continent 4: wood, water and bog; Pioneer-1's wreck). See
ArtSource/DESIGN_NOTES.md round 17.

Tiling sets (Exports/Env/Mirewood/Tiling/Mire_<Set>/): Road (a causeway of pale grey-green packed silt with flat
stones), Ground (black-brown peat, root threads, twigs, rare teal spores), GroundAlt (moss mat with teal spore
lights), Mud (glossy wet mud with puddles), Cliff (peat and earth bank with a root mat, V up).
Kit: RoadEdge_* (pale silt-stone kerb, moss on the ground side, roots crawling over), Bank_* (peat lip, roots
dipping into the water), Cliff_* (earth bank held by exposed roots, moss on top, dead stumps).
Decor: Flora_DeadTree, Flora_Reed, Decor_Fungus, Decor_FogVent (report names the fog emitter), Decor_MossStone,
Scatter_Twigs. The Pioneer-1 wreck (round 13, Exports/Pioneer1_Wreck) is the set piece.

Run: blender --background --factory-startup --python Tools/Blender/build_env_mirewood.py -- --preview <dir>
     [--part materials|kit|decor|diorama|all] [--only Road,Cliff_Straight] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
from env_kit import bn, ss, mix, rgb  # noqa: E402

CONT = K.Continent("Mirewood", "Mire", {"Road": 2.0, "Ground": 3.0, "GroundAlt": 2.0, "Mud": 3.0, "Cliff": 3.0})

TEAL = np.array((0.25, 1.0, 0.8))
VIOLET = np.array((0.7, 0.35, 1.0))
MOSS = np.array((0.14, 0.26, 0.2))
PEAT = np.array((0.12, 0.1, 0.08))
BARK = np.array((0.24, 0.22, 0.2))

SILTSTONE = K.pal((0.5, 0.53, 0.47), (0.3, 0.32, 0.29), (0.07, 0.08, 0.07), (0.07, 0.08, 0.07))
PEAT_PAL = K.pal((0.16, 0.13, 0.1), (0.1, 0.08, 0.06), (0.03, 0.025, 0.02), (0.02, 0.02, 0.015))
STONE = K.pal((0.3, 0.32, 0.3), (0.17, 0.18, 0.17), (0.04, 0.05, 0.04), (0.04, 0.04, 0.04))


# ------------------------------------------------------------------------------------------------ tiling sets

def road(n):
    """Causeway: flat pale stones bedded in packed silt; the silt between them is slightly darker, never black, with
    a few moss specks, so the road stays one calm light band."""
    d1, edge, val, _ = E.voronoi(n, 5, 411, jitter=0.9, warp=0.15)
    gap = 1 - ss(0.02, 0.08, edge)
    bevel = ss(0.04, 0.14, edge)
    silt, sv = E.grains(n, 380, 412, 0.9, radius=0.35)
    scuff = E.scratches(n, 60, 413)
    pits = E.D.pits(n, 50, 414, 0.25, radius=0.3)
    moss_g, mv = E.grains(n, 200, 415, 0.4, radius=0.4)
    moss = moss_g * gap * (mv < 0.25)
    h = (0.01 * bevel + 0.003 * (val - 0.5) - 0.006 * gap + 0.0015 * silt * gap + 0.0012 * bn(n, 40, 160, 416)
         + 0.0015 * bn(n, 6, 20, 417) - 0.0012 * pits + 0.003 * moss)
    tone = 0.9 + 0.18 * (val - 0.5) + 0.06 * bn(n, 3, 10, 418)
    col = rgb((0.55, 0.57, 0.5), n) * tone[..., None]
    col = mix(col, rgb((0.37, 0.38, 0.33), n) * (0.9 + 0.2 * sv)[..., None], gap)
    col = mix(col, rgb((0.26, 0.27, 0.24), n), 0.5 * (1 - ss(0.02, 0.045, edge)))
    col = mix(col, rgb((0.33, 0.34, 0.3), n), 0.5 * pits)
    col = mix(col, rgb(MOSS * 1.3, n), moss)
    col = mix(col, rgb((0.7, 0.71, 0.66), n), 0.45 * scuff)
    smooth = np.clip(0.3 + 0.12 * bevel - 0.18 * gap + 0.08 * scuff, 0.05, 1)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=moss, ao=ao)


def ground(n):
    """Black-brown peat: fibrous clumps with lighter tops, pale root threads running through it, a few twigs, wet
    hollows (darker and glossy), rare teal spores."""
    d1, e, val, _ = E.voronoi(n, 10, 421, jitter=0.9, warp=0.3)
    clump = 1 - ss(0.0, 0.7, d1)
    roots = E.scratches(n, 140, 422, length=(0.12, 0.3), width=(0.0015, 0.003), claws=1)
    twigs = E.scratches(n, 25, 423, length=(0.04, 0.08), width=(0.003, 0.005), claws=1)
    fibre = bn(n, 20, 80, 424)
    big = bn(n, 1, 4, 425)
    wet = ss(0.25, 0.6, -big) * (1 - clump * 0.5)
    spore, spv = E.grains(n, 300, 426, 0.03, radius=0.3)
    h = 0.015 * big + 0.006 * clump + 0.002 * fibre + 0.002 * roots + 0.003 * twigs + 0.001 * bn(n, 40, 200, 427)
    col = rgb((0.17, 0.14, 0.11), n) * (0.8 + 0.3 * clump + 0.12 * fibre + 0.1 * (val - 0.5))[..., None]
    col = mix(col, rgb((0.09, 0.075, 0.06), n), wet * 0.7)
    col = mix(col, rgb((0.36, 0.31, 0.25), n), roots * 0.75)
    col = mix(col, rgb((0.3, 0.26, 0.21), n), twigs * 0.8)
    col = mix(col, rgb(TEAL * 0.7, n), spore)
    smooth = np.clip(0.1 + 0.5 * wet + 0.1 * spore, 0.03, 1)
    emission = rgb(TEAL, n) * (spore * 0.85)[..., None]
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=spore, ao=ao, emission=emission)


def ground_alt(n):
    """Moss mat: domed cushions of dark teal-green moss, lighter on their crowns; clusters of glowing spore tips
    on a few crowns only; violet flecks."""
    d1, e, val, _ = E.voronoi(n, 18, 431, jitter=0.95, warp=0.45)
    cushion = np.clip(1 - (d1 / 0.8) ** 2, 0, 1)
    seam = (1 - ss(0.0, 0.12, e)) * 0.5
    fuzz = bn(n, 60, 220, 432)
    tips, tv = E.grains(n, 380, 433, 0.5, radius=0.32)
    lit_cushion = ((val * 13.7) % 1.0) < 0.08
    glow_tip = tips * ss(0.6, 0.85, cushion) * lit_cushion * (tv < 0.5)
    violet = tips * (tv > 0.93) * (1 - lit_cushion)
    h = 0.012 * cushion - 0.004 * seam + 0.0015 * fuzz + 0.006 * bn(n, 1, 4, 434)
    col = mix(rgb((0.1, 0.17, 0.13), n), rgb((0.19, 0.31, 0.24), n), cushion ** 1.5 * 0.7 + 0.3 * ss(-0.4, 0.6, bn(n, 2, 6, 436)))
    col = col * (0.8 + 0.2 * fuzz + 0.35 * (val - 0.5))[..., None]
    col = mix(col, rgb((0.04, 0.06, 0.05), n), seam * 0.7)
    col = mix(col, rgb(TEAL * 0.8, n), glow_tip)
    col = mix(col, rgb(VIOLET * 0.7, n), violet)
    smooth = np.clip(0.08 + 0.1 * glow_tip, 0.03, 1)
    emission = rgb(TEAL, n) * (glow_tip * 0.8)[..., None] + rgb(VIOLET, n) * (violet * 0.6)[..., None]
    ao = E.cavity_ao(h, 4, 0.004)
    return dict(albedo=col, height=h, smooth=smooth, dust=glow_tip, ao=ao, emission=np.clip(emission, 0, 1))


def mud(n):
    """Glossy wet mud: swirled, claw-tracked, with irregular puddles that are flat and mirror-smooth."""
    big = bn(n, 2, 7, 441)
    puddle = ss(0.3, 0.42, -big - 0.15 * bn(n, 6, 16, 445))
    swirl = bn(n, 3, 10, 442)
    tracks = E.scratches(n, 50, 443, length=(0.05, 0.1), width=(0.004, 0.007))
    lumps, lv = E.grains(n, 120, 444, 0.5, radius=0.45)
    h = 0.02 * big * (1 - puddle) - 0.008 * puddle + 0.003 * swirl * (1 - puddle) - 0.004 * tracks         + 0.004 * lumps * (1 - puddle)
    col = rgb((0.25, 0.2, 0.15), n) * (0.85 + 0.2 * swirl)[..., None]
    col = mix(col, rgb((0.16, 0.13, 0.1), n), tracks * 0.8)
    col = mix(col, rgb((0.11, 0.12, 0.1), n), puddle)
    smooth = np.clip(0.45 + 0.2 * swirl + 0.5 * puddle - 0.2 * lumps, 0.05, 0.97)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=puddle, ao=ao)


def cliff(n):
    """A peat and earth bank (V up): earth layers, a curtain of vertical roots, moss on the ledges, teal lichen."""
    u, v = E.coords(n)
    layer = 0.5 + 0.5 * np.sin((v * 5 + 0.3 * bn(n, 1, 4, 451)) * 2 * math.pi)
    _, re, rv, _ = E.voronoi(n, 2, 452, jitter=0.9, warp=0.25, stretch=(7, 1))
    root = (1 - ss(0.0, 0.09, re)) * (rv < 0.7)
    lichen, lcv = E.grains(n, 150, 453, 0.025, radius=0.4)
    h = 0.02 * layer + 0.012 * bn(n, 3, 10, 454) + 0.012 * root + 0.003 * bn(n, 30, 120, 455)
    nrm = E.normal_from_height(h, CONT.tiles["Cliff"])
    up = nrm[..., 1]
    col = rgb((0.24, 0.19, 0.15), n) * (0.8 + 0.3 * layer)[..., None]
    col = mix(col, rgb((0.44, 0.37, 0.3), n), root * 0.85)
    col = mix(col, rgb(MOSS * 1.2, n), 0.7 * ss(0.1, 0.5, up))
    col = mix(col, rgb((0.04, 0.035, 0.03), n), 0.5 * ss(0.1, 0.5, -up))
    col = mix(col, rgb(TEAL * 0.6, n), lichen)
    smooth = np.clip(0.18 + 0.1 * root + 0.2 * ss(0.3, 0.8, -up), 0.03, 1)
    emission = rgb(TEAL, n) * (lichen * 0.7)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=lichen, ao=ao, emission=emission)


SETS = {"Road": road, "Ground": ground, "GroundAlt": ground_alt, "Mud": mud, "Cliff": cliff}


# ------------------------------------------------------------------------------------------------ kit

def kerb_style(seed):
    """Pale silt-stone, the light line along the causeway; moss only creeps up its ground side (+Y)."""
    base = K.rock(SILTSTONE, seed, strata=30, crack=0.45, wet=(-0.02, 0.035), rough=0.75, smooth_top=0.1)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        side = C.smoothstep(0.0, 0.5, n[:, 1]) * (1 - C.smoothstep(0.6, 0.9, n[:, 2]))
        m = side * C.smoothstep(-0.2, 0.3, K.vn(p, 6, seed + 41)) * 0.8
        s.albedo[idx] = C.lerp3(s.albedo[idx], MOSS * 1.2, m)
        s.rough[idx] = np.maximum(s.rough[idx], 0.85 * m)
    return style


def root_style(seed, glow=0.3):
    """Wet dark root: ringed bark, paler on top, teal lichen spots."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        t, radius, dist = C.tube_param(part, p)
        ring = 1 - C.smoothstep(0.0, 0.08, np.abs(((t * C.axis_length(part) * 18) % 1.0) - 0.5) - 0.38)
        col = C.lerp3(BARK * 0.5, BARK, C.smoothstep(-0.2, 0.8, n[:, 2]))
        col = C.lerp3(col, BARK * 0.3, ring * 0.6)
        spot = (1 - C.smoothstep(0.0, 0.25, np.abs(K.vn(p, 22, seed)))) * C.smoothstep(0.1, 0.5, K.vn(p, 4, seed + 1)) * glow
        col = C.lerp3(col, TEAL * 0.6, spot)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(TEAL[None, :] * (spot * 0.9)[:, None], 0, 1)
        s.disp[idx] = -0.002 * ring
        s.rough[idx] = 0.55
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = t * C.axis_length(part)
    return style


def root_over(b, a, bpt, rng, seed, r=0.025, sag=0.05):
    a, bpt = Vector(a), Vector(bpt)
    mid = a.lerp(bpt, 0.5) + Vector((0, 0, sag))
    b.tube("Root", [a, a.lerp(mid, 0.6), mid, mid.lerp(bpt, 0.4), bpt], [r, r * 0.9, r * 0.8, r * 0.65, r * 0.4],
           sides=8, style=root_style(seed))


def kerb_accent(b, rng, kind):
    if kind != "straight":
        return
    for i in range(2):
        x = rng.uniform(-0.35, 0.35)
        root_over(b, (x, 0.3, -0.02), (x + rng.uniform(-0.1, 0.1), -0.13, -0.03), rng, 5210 + i, r=0.022, sag=0.17)


def peat_style(seed):
    return K.rock(PEAT_PAL, seed, strata=50, crack=0.3, moss=(MOSS, 0.65), wet=(-0.03, 0.03), rough=0.8)


def bank_accent(b, rng, kind):
    count = {"straight": 3, "outer": 1, "inner": 1}[kind]
    for i in range(count):
        if kind == "straight":
            x = -0.32 + i * 0.32 + rng.uniform(-0.05, 0.05)
            root_over(b, (x, 0.12, 0.03), (x + rng.uniform(-0.1, 0.1), -0.2, -0.1), rng, 5410 + i, r=0.024, sag=0.05)
        else:
            root_over(b, (0.05, 0.05, 0.03), (-0.16, -0.16, -0.1), rng, 5450 + i, r=0.024, sag=0.05)


def bank_profile(t, s):
    """Mud apron -> a bulging, lumpy earth face -> an undercut -> a turf lip overhanging the face -> the top."""
    x = t * 2.0
    w = K.window(t, 0.12)
    bulge = 0.06 * E.periodic(x, 20.0, 3, 5603) * w
    pts = [(0.34, -0.05), (0.26, 0.0), (0.16, 0.06), (0.1 + bulge, 0.3), (0.15 + bulge, 0.6), (0.06, 0.88),
           (0.0, 0.95), (0.13, 1.02), (0.12, 1.1), (0.0, 1.14), (-0.4, 1.15), (-0.9, 1.16)]
    return K.polyline(pts, s)


def bank_inner_profile(t, s):
    return K.polyline([(0.2, -0.12), (-0.88, 0.9)], s)


def bank_jitter(x, s, d, z):
    lump = 0.05 * E.periodic(x * 2, 40.0, 4, 5601 + int(z * 7)) + 0.035 * E.periodic(x, 20.0, 3, 5602)
    return lump * (0.3 if z > 1.0 else 1.0), 0.0


def earth_style(seed):
    base = K.rock(PEAT_PAL, seed, strata=12, crack=0.2, rough=0.85)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        layer = 0.5 + 0.5 * np.sin(p[:, 2] * 22 + 2 * K.vn(p, 1.5, seed + 2))
        s.albedo[idx] = s.albedo[idx] * (0.85 + 0.35 * layer)[:, None]
        turf = C.smoothstep(0.92, 1.04, p[:, 2]) + C.smoothstep(0.55, 0.85, n[:, 2]) * C.smoothstep(0.5, 0.8, p[:, 2])
        turf = np.clip(turf, 0, 1)
        s.albedo[idx] = C.lerp3(s.albedo[idx], MOSS * (0.85 + 0.4 * K.vn(p, 12, seed + 3))[:, None], turf * 0.9)
        s.disp[idx] += 0.008 * turf * K.vn(p, 20, seed + 4)
        s.fam[idx] = np.array(E.FAM_ROCK) * (1 - turf)[:, None] + np.array(E.FAM_MEMBRANE) * turf[:, None]
    return style


def stump(b, base, seed, height=0.36, radius=0.1):
    """A dead stump with a jagged, splintered top."""
    rng = np.random.default_rng(seed)
    top = base + Vector((0.02, 0.01, height))
    b.tube("Stump", [base - Vector((0, 0, 0.05)), base + Vector((0.01, 0.0, height * 0.5)), top],
           [radius * 1.25, radius, radius * 0.9], sides=10, style=root_style(seed, 0.6), cap=0.1)
    for k in range(4):
        a = k / 4 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        b.shard("Splinter", top - Vector((0, 0, 0.04)) + out * radius * 0.55, out * 0.25 + Vector((0, 0, 1)),
                radius * 0.35, rng.uniform(0.08, 0.18), sides=5, flute=0.2, style=root_style(seed + 1 + k, 0.0))


def earth_bank(kind):
    """Earth bank held by exposed roots that loop out of the turf and drape over the face, a turf lip, moss tufts
    and a dead stump."""
    b = C.PartBuilder()
    rng = np.random.default_rng({"straight": 5610, "outer": 5620, "inner": 5630}[kind])
    path, length = K.cliff_sweep(b, "Bank", kind, bank_profile, bank_inner_profile, earth_style(5611), bank_jitter,
                                 radius=0.6 if kind == "outer" else 0.4)
    count = 8 if kind == "straight" else 5
    for i in range(count):
        t = (i + 0.5) / count + rng.uniform(-0.04, 0.04)
        top = K.along(path, t, 0.05, 1.08)
        loop = K.along(path, t + rng.uniform(-0.03, 0.03), 0.3, rng.uniform(0.75, 0.95))
        mid = K.along(path, t + rng.uniform(-0.04, 0.04), 0.2, rng.uniform(0.35, 0.55))
        hang = rng.uniform() < 0.35
        foot = K.along(path, t + rng.uniform(-0.05, 0.05), 0.32 if not hang else 0.26,
                       -0.03 if not hang else rng.uniform(0.15, 0.3))
        r = rng.uniform(0.028, 0.048)
        b.tube("Root", [top, loop, mid, foot], [r, r * 0.9, r * 0.75, r * 0.4], sides=8, style=root_style(5640 + i))
    for i in range(6 if kind == "straight" else 4):
        t = rng.uniform(0.05, 0.95)
        b.ellipsoid("Tuft", K.along(path, t, 0.1, 1.1), (0.09, 0.07, 0.05), style=earth_style(5680 + i), segments=(14, 8))
    stump(b, K.along(path, 0.35 if kind == "straight" else 0.5, -0.45, 1.12), 5660)
    b.ellipsoid("Mass", K.along(path, 0.5, -1.05, 1.0), (0.8 if kind == "straight" else 0.6, 0.5, 0.35),
                K.rz(math.degrees(K.facing(path, 0.5)) + 90), style=earth_style(5670), segments=(28, 14),
                deform=C.chiselled(14, 5670, 0.12))
    if kind == "outer":
        b.ellipsoid("Fill", Vector((0.9, 0.9, 0.7)), (0.6, 0.6, 0.48), K.rz(45), style=earth_style(5675),
                    segments=(24, 12), deform=C.chiselled(12, 5675, 0.12))
    return b


# ------------------------------------------------------------------------------------------------ decor

def moss_beard(seed):
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        col = C.lerp3(np.array((0.3, 0.36, 0.33)), np.array((0.18, 0.26, 0.22)), t)
        tip = C.smoothstep(0.85, 1.0, t)
        s.albedo[idx] = C.lerp3(col, TEAL * 0.6, tip)
        s.emit[idx] = np.clip(TEAL[None, :] * (tip * 0.6)[:, None], 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.9
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t
    return style


def dead_tree():
    """A bare twisted tree up on arched spider-roots, hooked bare branches, beard moss hanging."""
    b = C.PartBuilder()
    rng = np.random.default_rng(6001)
    base_z = 0.28
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        b.tube("Leg", [Vector((0, 0, base_z + 0.05)), out * 0.16 + Vector((0, 0, base_z + 0.12)), out * 0.32 + Vector((0, 0, 0.18)),
                       out * 0.42 + Vector((0, 0, -0.04))], [0.06, 0.045, 0.035, 0.02], sides=8, style=root_style(6010 + i))
    trunk = [Vector((0, 0, base_z)), Vector((0.04, 0.0, 0.6)), Vector((-0.03, 0.03, 0.9)), Vector((0.02, -0.02, 1.15))]
    b.tube("Trunk", trunk, [0.08, 0.065, 0.05, 0.035], sides=10, style=root_style(6020))
    branches = []
    for i in range(4):
        a = i / 4 * 2 * math.pi + rng.uniform(-0.4, 0.4)
        out = Vector((math.cos(a), math.sin(a), 0))
        z0 = rng.uniform(0.7, 1.05)
        pts = [Vector((0, 0, z0)), out * 0.2 + Vector((0, 0, z0 + 0.12)), out * 0.38 + Vector((0, 0, z0 + 0.1)),
               out * 0.5 + Vector((0, 0, z0 - 0.02))]
        b.tube("Branch", pts, [0.035, 0.025, 0.015, 0.004], sides=8, style=root_style(6030 + i))
        branches.append(pts)
    for i, pts in enumerate(branches):
        for k in range(2):
            top = pts[1].lerp(pts[2], 0.3 + 0.5 * k)
            drop = rng.uniform(0.18, 0.32)
            b.tube("Beard", [top, top + Vector((0.01, 0, -drop * 0.5)), top + Vector((0.0, 0.01, -drop))],
                   [0.016, 0.012, 0.004], sides=5, style=moss_beard(6050 + i * 2 + k))
    return b


def reed_style(seed, glow):
    def style(s, idx, part):
        p = s.p[idx]
        t, radius, dist = C.tube_param(part, p)
        band = 1 - C.smoothstep(0.0, 0.06, np.abs(((t * 6) % 1.0) - 0.5) - 0.42)
        col = C.lerp3(np.array((0.12, 0.16, 0.12)), np.array((0.34, 0.4, 0.3)), C.smoothstep(0.0, 0.7, t))
        col = C.lerp3(col, np.array((0.08, 0.1, 0.08)), band * 0.7)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(np.asarray(glow)[None, :] * (band * C.smoothstep(0.4, 0.9, t) * 0.4)[:, None], 0, 1)
        s.disp[idx] = 0.003 * band
        s.rough[idx] = 0.5
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t * C.axis_length(part)
    return style


def lantern_style(seed, glow):
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        ribs = (np.abs(np.sin(np.arctan2(L[:, 1], L[:, 0]) * 5)) > 0.8)
        col = C.lerp3(np.array((0.1, 0.12, 0.1)), np.asarray(glow) * 0.7, 1 - ribs)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(np.asarray(glow)[None, :] * ((1 - ribs) * 0.85)[:, None], 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.35
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = L[:, 2]
    return style


def reed():
    """A clump of segmented reeds; the tallest carry ribbed lantern seed heads that glow teal and violet."""
    b = C.PartBuilder()
    rng = np.random.default_rng(6101)
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.0, 0.14)
        root = Vector((math.cos(a) * r, math.sin(a) * r, -0.02))
        h = rng.uniform(0.5, 0.95)
        lean = Vector((math.cos(a), math.sin(a), 0)) * rng.uniform(0.04, 0.16)
        top = root + lean + Vector((0, 0, h))
        glow = TEAL if i % 3 else VIOLET
        b.tube("Stalk", [root, root + lean * 0.3 + Vector((0, 0, h * 0.5)), top], [0.016, 0.013, 0.008], sides=6,
               style=reed_style(6110 + i, glow))
        if h > 0.7:
            b.ellipsoid("Head", top + Vector((0, 0, 0.05)), (0.03, 0.03, 0.07), style=lantern_style(6130 + i, glow),
                        segments=(12, 8))
    return b


def gill_style(seed, glow=VIOLET):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        L = C.local_points(part, p)
        r = np.hypot(L[:, 0], L[:, 1])
        under = C.smoothstep(-0.1, -0.6, n[:, 2])
        gills = (np.abs(np.sin(np.arctan2(L[:, 1], L[:, 0]) * 18)) > 0.7) * under
        rings = 0.5 + 0.5 * np.sin(r * 40)
        col = C.lerp3(np.array((0.32, 0.24, 0.2)), np.array((0.5, 0.42, 0.34)), rings * C.smoothstep(0.3, 0.9, n[:, 2]))
        col = C.lerp3(col, np.array((0.62, 0.55, 0.45)), C.smoothstep(0.85, 1.0, r) * 0.6)
        col = C.lerp3(col, glow * 0.6, gills)
        s.albedo[idx] = col * (1 + 0.1 * K.vn(p, 25, seed))[:, None]
        s.emit[idx] = np.clip(np.asarray(glow)[None, :] * (gills * 0.9 + under * 0.1)[:, None], 0, 1)
        s.disp[idx] = -0.002 * gills
        s.rough[idx] = 0.6
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = r * 0.3
    return style


def fungus():
    """Bracket fungi stacked up a broken stump, violet light in their gills."""
    b = C.PartBuilder()
    rng = np.random.default_rng(6201)
    b.tube("Stump", [Vector((0, 0, -0.05)), Vector((0.02, 0.0, 0.3)), Vector((0.0, 0.02, 0.55))], [0.16, 0.13, 0.11],
           sides=12, style=root_style(6210, 0.5), cap=0.15)
    for i in range(4):
        a = rng.uniform(0, 2 * math.pi)
        out = Vector((math.cos(a), math.sin(a), 0))
        z = 0.12 + i * 0.11
        r = rng.uniform(0.13, 0.19)
        c = out * (0.12 + r * 0.45) + Vector((0, 0, z))
        b.ellipsoid("Bracket", c, (r, r * 0.75, 0.035), K.rz(math.degrees(a)), style=gill_style(6220 + i),
                    segments=(28, 10))
    for i in range(3):
        a = i * 2.1
        c = Vector((math.cos(a) * 0.25, math.sin(a) * 0.25, 0.04))
        b.ellipsoid("Cap", c, (0.05, 0.05, 0.03), style=gill_style(6240 + i, TEAL), segments=(16, 8))
        b.tube("Stem", [c - Vector((0, 0, 0.05)), c], [0.012, 0.01], sides=6, style=root_style(6250 + i, 0.0))
    return b


def vent_style(seed):
    base = K.rock(K.pal((0.24, 0.2, 0.16), (0.14, 0.11, 0.09), (0.04, 0.03, 0.02), (0.03, 0.02, 0.02)), seed,
                  strata=40, crack=0.3, rough=0.5, moss=(MOSS, 0.4))

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        pore = 1 - C.smoothstep(0.0, 0.22, np.abs(K.vn(p, 24, seed + 4)))
        pore = pore * C.smoothstep(0.0, 0.4, K.vn(p, 7, seed + 5))
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.02, 0.03, 0.03)), pore * 0.8)
        s.emit[idx] = np.clip(s.emit[idx] + TEAL[None, :] * (pore * 0.25)[:, None], 0, 1)
        s.disp[idx] -= 0.006 * pore
    return style


VENT_TOP = 0.46


def fog_vent():
    """A mud chimney with pores, a crater on top that breathes marsh fog (emitter point in the report)."""
    b = C.PartBuilder()
    b.tube("Chimney", [Vector((0, 0, -0.05)), Vector((0.02, 0.0, 0.2)), Vector((0.0, 0.03, VENT_TOP))], [0.24, 0.16, 0.1],
           sides=14, style=vent_style(6310), cap=0.05)
    b.ellipsoid("Lip", Vector((0.0, 0.03, VENT_TOP - 0.01)), (0.13, 0.13, 0.04), style=vent_style(6311), segments=(24, 10))
    b.ellipsoid("Throat", Vector((0.0, 0.03, VENT_TOP + 0.012)), (0.07, 0.07, 0.02), style=K.glow_seam(TEAL, 0.5,
                                                                                                         np.array((0.02, 0.05, 0.05))),
                segments=(16, 8))
    for i, (x, y, r) in enumerate(((0.3, 0.1, 0.12), (-0.25, 0.2, 0.09), (0.05, -0.3, 0.1))):
        b.ellipsoid("Mound", Vector((x, y, 0.0)), (r, r * 0.9, r * 0.6), style=vent_style(6320 + i), segments=(18, 9))
    return b


def moss_stone():
    b = C.PartBuilder()
    rng = np.random.default_rng(6401)
    b.ellipsoid("Stone", Vector((0, 0, 0.18)), (0.36, 0.3, 0.24), K.rz(20),
                style=K.rock(STONE, 6410, strata=10, crack=0.4, moss=(MOSS, 0.9), wet=(-0.02, 0.05)),
                segments=(32, 16), deform=C.chiselled(12, 6410, 0.15))
    for i in range(2):
        a = 0.8 + i * 2.4
        root_over(b, (math.cos(a) * 0.5, math.sin(a) * 0.45, -0.03), (math.cos(a + 2.4) * 0.45, math.sin(a + 2.4) * 0.4, -0.03),
                  rng, 6420 + i, r=0.03, sag=0.5)
    return b


def twigs():
    b = C.PartBuilder()
    rng = np.random.default_rng(6501)
    for i in range(3):
        a = rng.uniform(0, math.pi)
        d = Vector((math.cos(a), math.sin(a), 0)) * rng.uniform(0.1, 0.16)
        c = Vector((rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), 0.01))
        b.tube("Twig", [c - d, c + Vector((0, 0, 0.01)), c + d], [0.012, 0.01, 0.006], sides=5, style=root_style(6510 + i, 0.0))
    K.chip(b, (0.1, -0.08, 0.0), (0.04, 0.035, 0.025), 6520, K.rock(STONE, 6520, moss=(MOSS, 0.6)), segments=(12, 6))
    return b


KIT = {
    "RoadEdge_Straight": dict(fn=lambda: K.kerb_straight(kerb_style(5110), 5110, kerb_accent), tris=700, voxel=0.007),
    "RoadEdge_OuterCorner": dict(fn=lambda: K.kerb_outer(kerb_style(5210), 5210), tris=500, voxel=0.007),
    "RoadEdge_InnerCorner": dict(fn=lambda: K.kerb_inner(kerb_style(5310), 5310), tris=400, voxel=0.007),
    "Bank_Straight": dict(fn=lambda: K.bank_straight(peat_style(5410), 5410, bank_accent), tris=600, voxel=0.008),
    "Bank_OuterCorner": dict(fn=lambda: K.bank_outer(peat_style(5510), 5510, bank_accent), tris=400, voxel=0.008),
    "Bank_InnerCorner": dict(fn=lambda: K.bank_inner(peat_style(5560), 5560, bank_accent), tris=300, voxel=0.008),
    "Cliff_Straight": dict(fn=lambda: earth_bank("straight"), tris=1500, voxel=0.013),
    "Cliff_OuterCorner": dict(fn=lambda: earth_bank("outer"), tris=1500, voxel=0.013),
    "Cliff_InnerCorner": dict(fn=lambda: earth_bank("inner"), tris=1500, voxel=0.013),
}

FLORA = ("membrane", "chitin")
DECOR = {
    "Flora_DeadTree": dict(fn=dead_tree, tris=1500, voxel=0.007, smoothing=1, families=FLORA, sway=1.2),
    "Flora_Reed": dict(fn=reed, tris=1200, voxel=0.005, smoothing=1, families=FLORA, sway=0.95),
    "Decor_Fungus": dict(fn=fungus, tris=1300, voxel=0.007, smoothing=1, families=FLORA),
    "Decor_FogVent": dict(fn=fog_vent, tris=1000, voxel=0.008,
                          extra={"fog_emitter": {"position_blender": [0.0, 0.03, VENT_TOP + 0.02],
                                                 "position_unity": [0.0, VENT_TOP + 0.02, 0.03],
                                                 "note": "spawn the marsh fog particles here, rising, teal-grey"}}),
    "Decor_MossStone": dict(fn=moss_stone, tris=1000, voxel=0.009),
    "Scatter_Twigs": dict(fn=twigs, tris=200, voxel=0.005, families=FLORA),
}

DIORAMA = dict(
    sets=dict(ground="Ground", alt="GroundAlt", element="Mud", road="Road"),
    alt_areas=[((1.0, 1.0), 2.2), ((8.0, 0.6), 1.8), ((10.6, 5.6), 1.8), ((2.0, 7.2), 2.0)],
    element_areas=[((6.6, 6.0), 1.8), ((0.3, 2.8), 1.5), ((11.3, 0.4), 1.2), ((5.5, 0.4), 1.2)],
    liquid=dict(color=(0.025, 0.06, 0.05), rough=0.04, emit=(0.08, 0.3, 0.25), strength=0.5, z=-0.03, origin="Origin_Bog"),
    world=(0.008, 0.016, 0.016),
    mist=dict(density=0.55, color=(0.42, 0.55, 0.52)),
    lights=dict(key=1.9, rim=1.1, key_color=(0.88, 0.95, 0.92), rim_color=(0.4, 0.9, 0.8),
                points=[((1.3, 1.5), (0.3, 1.0, 0.8), 14), ((7.6, 0.6), (0.7, 0.35, 1.0), 12),
                        ((10, 2.5), (0.5, 0.75, 1.0), 25), ((4.4, 6.6), (0.6, 0.8, 1.0), 18)]),
    decor=[("Flora_DeadTree", (1.0, 1.2), 0, 1.0), ("Flora_DeadTree", (11.3, 0.6), 80, 0.85),
           ("Flora_DeadTree", (2.6, 7.0), 200, 1.0), ("Flora_Reed", (0.3, 2.4), 0, 1.0),
           ("Flora_Reed", (7.6, 6.4), 40, 1.0), ("Flora_Reed", (10.3, 6.3), 0, 0.9), ("Flora_Reed", (8.6, 5.4), 90, 0.8),
           ("Decor_Fungus", (4.2, 0.6), 0, 1.0), ("Decor_Fungus", (11.3, 4.6), 60, 0.8),
           ("Decor_FogVent", (7.6, 0.2), 0, 1.0), ("Decor_FogVent", (0.4, 7.0), 0, 0.8),
           ("Decor_MossStone", (0.2, 0.2), 0, 1.0), ("Decor_MossStone", (8.0, 4.7), 120, 0.6),
           ("@Pioneer1_Wreck", (6.9, 7.0), 200, 0.5)],
    scatter=["Scatter_Twigs"],
    scatter_count=30,
    towers=[("Tower_Acid", "T2", (2, 4)), ("Tower_Gun", "T2", (6, 4)), ("Tower_Laser", "T1", (9, 4)),
            ("Tower_Cannon", "T1", (6, 1)), ("Tower_Flak", "T1", (10, 1))],
    enemies=[("Mireleech", 0.75, None, [(1.3, 5.4, -90), (3.6, 4.0, 180), (6.8, 2.6, -90)]),
             ("Knitback", 0.85, None, [(2.4, 5.8, -95)]),
             ("Surgefin", 0.75, None, [(4.0, 3.2, 175)])],
    close_target=(7.0, 4.2),
)


if __name__ == "__main__":
    K.run(CONT, SETS, KIT, DECOR, DIORAMA, world=(0.02, 0.03, 0.03))
