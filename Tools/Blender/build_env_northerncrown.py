"""Northern Crown environment set (round 17, continent 3: ice to the north pole, stone and fire). See
ArtSource/DESIGN_NOTES.md round 17.

Tiling sets (Exports/Env/NorthernCrown/Tiling/North_<Set>/): Road (the colony's heated road: pale grey-tan laid
slabs, frost in the joints, melt-water stains), Ground (blue-black slate with frost in the cracks), GroundAlt
(frozen gravel in permafrost polygons), Snow (wind-packed crust with sastrugi and blue ice windows), Cliff
(hexagonal basalt columns, V up, amber heat in the deepest joints).
Kit: RoadEdge_* (dark granite with a snow cap: a white line along the road), Bank_* (ice lake: a snow-capped ice
shelf), Cliff_* (hexagonal basalt columns with snow caps and icicles).
Decor: IceShard, Snowdrift, FrozenCrystal, RimeBoulder, Flora_FrostFern, Scatter_IceChips.

Run: blender --background --factory-startup --python Tools/Blender/build_env_northerncrown.py -- --preview <dir>
     [--part materials|kit|decor|diorama|all] [--only Road,Cliff_Straight] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
from env_kit import bn, ss, mix, rgb  # noqa: E402

CONT = K.Continent("NorthernCrown", "North", {"Road": 2.0, "Ground": 3.0, "GroundAlt": 2.0, "Snow": 3.0, "Cliff": 3.0})

AMBER = np.array((1.0, 0.6, 0.25))
SNOW = np.array((0.8, 0.85, 0.92))
ICE = np.array((0.55, 0.7, 0.84))
FROST = np.array((0.78, 0.86, 0.95))

GRANITE = K.pal((0.34, 0.36, 0.42), (0.18, 0.2, 0.25), (0.04, 0.05, 0.07), (0.04, 0.04, 0.06))
SLATE = K.pal((0.22, 0.24, 0.3), (0.12, 0.13, 0.17), (0.03, 0.03, 0.05), (0.02, 0.02, 0.03))
ICE_PAL = K.pal((0.72, 0.82, 0.9), (0.42, 0.56, 0.7), (0.12, 0.18, 0.26), (0.2, 0.3, 0.42))


# ------------------------------------------------------------------------------------------------ tiling sets

def road(n):
    """Laid slabs in a running bond (2:1), chamfered, frost in the joints, darker melt-water round them."""
    u, v = E.coords(n)
    cols, rows = 2, 4
    y = v * rows
    row = np.floor(y)
    x = u * cols + 0.5 * (row % 2) + 0.03 * bn(n, 2, 6, 311)
    fx, fy = x % 1.0, y % 1.0
    sid = (np.floor(x) % cols) + row * cols
    vals = np.random.default_rng(312).uniform(0, 1, cols * rows)[sid.astype(int)]
    e = np.minimum(np.minimum(fx, 1 - fx) * 2.0, np.minimum(fy, 1 - fy)) + 0.01 * bn(n, 4, 12, 313)
    joint = 1 - ss(0.0, 0.022, e)
    bevel = ss(0.01, 0.07, e)
    _, e2, _, _ = E.voronoi(n, 9, 314, warp=0.25)
    crack = (1 - ss(0.0, 0.018, e2)) * (bn(n, 1, 4, 315) > 0.3) * bevel
    scuff = E.scratches(n, 70, 316)
    pits = E.D.pits(n, 60, 317, 0.25, radius=0.3)
    frost_g, fv = E.grains(n, 360, 318, 0.8, radius=0.36)
    frost = frost_g * (1 - ss(0.01, 0.05, e))
    melt = (1 - ss(0.02, 0.12, e)) * ss(-0.2, 0.4, bn(n, 2, 6, 319))
    h = (0.009 * bevel + 0.003 * (vals - 0.5) - 0.012 * joint - 0.003 * crack + 0.0012 * bn(n, 40, 160, 320)
         + 0.0015 * bn(n, 6, 20, 321) - 0.0015 * pits + 0.003 * frost)
    tone = 0.9 + 0.18 * (vals - 0.5) + 0.06 * bn(n, 3, 10, 322)
    col = rgb((0.58, 0.56, 0.52), n) * tone[..., None]
    col = mix(col, rgb((0.3, 0.3, 0.32), n), 0.45 * melt)
    col = mix(col, rgb((0.07, 0.075, 0.09), n), joint)
    col = mix(col, rgb((0.2, 0.2, 0.23), n), 0.7 * crack)
    col = mix(col, rgb((0.35, 0.34, 0.33), n), 0.5 * pits)
    col = mix(col, rgb(FROST, n), frost)
    col = mix(col, rgb((0.74, 0.72, 0.7), n), 0.5 * scuff)
    smooth = np.clip(0.34 + 0.35 * melt - 0.3 * joint - 0.12 * crack + 0.1 * scuff + 0.2 * frost, 0.05, 1)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=frost, ao=ao)


def ground(n):
    """Blue-black slate: split plates stepping in layers, frost crystals filling the cracks."""
    d1, e, val, _ = E.voronoi(n, 6, 331, jitter=0.9, warp=0.25, stretch=(1, 2))
    crack = 1 - ss(0.0, 0.04, e)
    step = (val * 4).astype(int) / 4.0
    _, e_fine, _, _ = E.voronoi(n, 17, 332, warp=0.3)
    fine = (1 - ss(0.0, 0.025, e_fine)) * (1 - crack) * (bn(n, 1, 4, 333) > 0.0)
    layers = 0.5 + 0.5 * np.sin((E.coords(n)[1] * 30 + 2.0 * bn(n, 1, 4, 334)) * 2 * math.pi)
    frost, fv = E.grains(n, 300, 335, 0.8, radius=0.42)
    frost = frost * np.clip(crack * 2.0 + fine * 1.5 + 0.15, 0, 1)
    heat = ss(0.35, 0.7, bn(n, 2, 5, 336)) * crack
    h = 0.02 * bn(n, 1, 4, 337) + 0.008 * step - 0.015 * crack - 0.004 * fine + 0.002 * layers + 0.003 * frost \
        + 0.0012 * bn(n, 40, 200, 338)
    tone = 0.85 + 0.35 * (val - 0.5) + 0.08 * bn(n, 3, 10, 339)
    col = rgb((0.15, 0.17, 0.22), n) * tone[..., None] * (0.94 + 0.08 * layers)[..., None]
    col = mix(col, rgb((0.03, 0.035, 0.05), n), crack * 0.9)
    col = mix(col, rgb((0.07, 0.08, 0.1), n), fine * 0.6)
    col = mix(col, rgb(FROST * 0.9, n), frost)
    col = mix(col, rgb(AMBER * 0.5, n), heat * (1 - frost) * 0.6)
    smooth = np.clip(0.2 + 0.1 * layers - 0.08 * crack + 0.35 * frost, 0.03, 1)
    emission = rgb(AMBER, n) * (heat * (1 - frost) * 0.5)[..., None]
    ao = E.cavity_ao(h, 6, 0.008)
    return dict(albedo=col, height=h, smooth=smooth, dust=frost, ao=ao, emission=emission)


def ground_alt(n):
    """Frozen gravel in permafrost polygons: raised gravel rims, frost in the low centres."""
    d1, e, val, _ = E.voronoi(n, 4, 341, jitter=0.7, warp=0.15)
    rim = 1 - ss(0.0, 0.1, e)
    centre = ss(0.1, 0.3, e)
    g, gv = E.grains(n, 220, 342, 0.85, radius=0.42)
    g2, gv2 = E.grains(n, 90, 343, 0.6, radius=0.45)
    frost = ss(0.2, 0.8, bn(n, 4, 14, 344)) * centre
    h = 0.012 * rim + 0.005 * g + 0.008 * g2 * rim + 0.0015 * bn(n, 40, 200, 345)
    col = rgb((0.2, 0.21, 0.24), n) * (0.85 + 0.3 * ((gv * 17) % 1))[..., None]
    col = mix(col, rgb((0.28, 0.27, 0.27), n) * (0.8 + 0.4 * ((gv2 * 13) % 1))[..., None], g2 * rim)
    col = mix(col, rgb((0.06, 0.06, 0.08), n), (1 - g) * 0.5)
    col = mix(col, rgb(FROST * 0.85, n), frost * 0.75)
    smooth = np.clip(0.12 + 0.3 * frost, 0.03, 1)
    ao = E.cavity_ao(h, 4, 0.005)
    return dict(albedo=col, height=h, smooth=smooth, dust=frost, ao=ao)


def snow(n):
    """Wind-packed snow: long sharp sastrugi crests along the wind, blue shadow in the scoops behind them, small
    windows of blue ice where the wind scoured it bare."""
    u, v = E.coords(n)
    phase = v * 9 + 1.2 * bn(n, 1, 4, 351) + 0.35 * bn(n, 3, 9, 352)
    saw = phase % 1.0
    crest = np.where(saw < 0.8, saw / 0.8, (1 - saw) / 0.2)  # long windward slope, short steep lee
    broken = ss(-0.3, 0.3, bn(n, 2, 6, 353))
    ridge = crest * broken
    scoop = (1 - ss(0.0, 0.2, crest)) * broken
    window = ss(0.5, 0.62, bn(n, 4, 10, 354)) * (1 - broken * 0.7)
    sparkle, sv = E.grains(n, 500, 355, 0.25, radius=0.3)
    h = 0.02 * bn(n, 1, 3, 356) + 0.012 * ridge * (1 - window) - 0.004 * window + 0.001 * bn(n, 60, 220, 357)
    # Value capped near 0.4 (the road is about 0.48): snow must never out-shine the road.
    col = rgb((0.44, 0.48, 0.57), n) * (0.86 + 0.16 * ridge)[..., None]
    col = mix(col, rgb((0.27, 0.32, 0.43), n), 0.55 * scoop)
    col = mix(col, rgb((0.14, 0.24, 0.36), n), window * 0.85)
    col = np.minimum(col, 0.56)
    smooth = np.clip(0.2 + 0.6 * window + 0.2 * sparkle, 0.03, 1)
    ao = E.cavity_ao(h, 6, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=sparkle * (sv < 0.5), ao=ao)


def cliff(n):
    """Hexagonal basalt columns (V up): narrow vertical prisms with cross joints, frost on the ledges, amber in a
    few deep joints."""
    d1, e, val, _ = E.voronoi(n, 3, 361, jitter=0.35, stretch=(3, 1))
    u, v = E.coords(n)
    joint = 1 - ss(0.0, 0.03, e)
    cross_c = (v * 5 + val * 3.7) % 1.0
    cross = 1 - ss(0.0, 0.03, np.minimum(cross_c, 1 - cross_c))
    face = 0.5 + 0.5 * np.cos(np.clip(d1 * 4.5, 0, math.pi))
    heat = ss(0.55, 0.8, bn(n, 3, 8, 362))
    h = 0.04 * face - 0.035 * joint - 0.012 * cross + 0.003 * bn(n, 30, 120, 363)
    nrm = E.normal_from_height(h, CONT.tiles["Cliff"])
    up = nrm[..., 1]
    col = rgb((0.2, 0.22, 0.27), n) * (0.8 + 0.35 * val + 0.08 * bn(n, 3, 10, 364))[..., None]
    col = mix(col, rgb(FROST, n), 0.7 * ss(0.15, 0.55, up))
    col = mix(col, rgb((0.03, 0.03, 0.05), n), joint * 0.9)
    col = mix(col, rgb((0.08, 0.09, 0.11), n), cross * 0.7)
    ember = joint * heat
    col = mix(col, rgb(AMBER * 0.6, n), ember * 0.6)
    smooth = np.clip(0.25 + 0.1 * val - 0.08 * joint + 0.3 * ss(0.15, 0.55, up), 0.03, 1)
    emission = rgb(AMBER, n) * (ember * 0.7)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=ember, ao=ao, emission=emission)


SETS = {"Road": road, "Ground": ground, "GroundAlt": ground_alt, "Snow": snow, "Cliff": cliff}


# ------------------------------------------------------------------------------------------------ kit

def kerb_style(seed):
    return K.rock(GRANITE, seed, strata=30, crack=0.5, cap=(SNOW, 1.0, 0.55), wet=(-0.02, 0.03), frost=0.25,
                  rough=0.7)


def ice_style(seed, cap=0.9):
    """Lake ice: pale blue, dark blue fracture planes, frosted edges, a snow cap on the tops."""
    base = K.rock(ICE_PAL, seed, strata=0.0, crack=0.9, rough=0.25, cap=(SNOW, cap, 0.62))

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        deep = C.smoothstep(0.03, -0.12, p[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.12, 0.22, 0.34)), deep * 0.7)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def bank_accent(b, rng, kind):
    """Ice blocks pushed up at the lip."""
    count = {"straight": 3, "outer": 1, "inner": 1}[kind]
    for i in range(count):
        if kind == "straight":
            c = (-0.3 + i * 0.3 + rng.uniform(-0.05, 0.05), rng.uniform(-0.08, -0.02), 0.03)
        else:
            c = (-0.05, -0.05, 0.03)
        b.ellipsoid("Block", Vector(c), (rng.uniform(0.07, 0.1), rng.uniform(0.05, 0.07), rng.uniform(0.05, 0.07)),
                    K.tilt(rng, 30, 18), style=ice_style(2450 + i), segments=(20, 10),
                    deform=E.boxy(2450 + i, 0.22, 5, 0.15))


def column_style(seed, snow=1.0):
    return K.rock(SLATE, seed, strata=0.0, crack=0.35, cap=(SNOW, snow, 0.7), frost=0.35, crack_glow=0.22,
                  crack_color=AMBER, rough=0.7)


def icicle_style(seed):
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        col = C.lerp3(np.array((0.75, 0.84, 0.94)), np.array((0.5, 0.66, 0.84)), t)
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = 0.0
        s.rough[idx] = 0.15
        s.fam[idx] = E.FAM_CRYSTAL
        s.flow[idx] = t
    return style


def columns_piece(kind):
    """Hexagonal basalt columns along the edge, tallest at the back, snow caps, icicles off the front tops."""
    b = C.PartBuilder()
    rng = np.random.default_rng({"straight": 3610, "outer": 3620, "inner": 3630}[kind])
    if kind == "straight":
        path = K.straight_path(1.0)
    elif kind == "outer":
        path = K.cliff_outer_path(0.6)
    else:
        path = K.cliff_inner_path(0.4)
    length = K.path_length(path)
    rows = ((0.0, 0.15, (0.75, 1.05)), (-0.29, 0.17, (1.05, 1.4)), (-0.6, 0.19, (1.3, 1.75)),
            (-0.94, 0.21, (1.5, 1.95)))
    for r, (depth, radius, (h0, h1)) in enumerate(rows):
        count = max(2, int(round(length / (radius * 1.85))))
        for i in range(count):
            t = (i + 0.5 + rng.uniform(-0.12, 0.12) + 0.5 * (r % 2)) / (count + 0.5 * (r % 2))
            if t <= 0.02 or t >= 0.98:
                continue
            c = K.along(path, t, depth + rng.uniform(-0.03, 0.03), -0.1)
            h = rng.uniform(h0, h1)
            b.prism("Column", c, Vector((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 1)), radius * rng.uniform(0.9, 1.05),
                    h, 0.025, sides=6, twist=rng.uniform(0, 1), taper=0.97,
                    style=column_style(3640 + r * 20 + i, 1.0 if r == 0 or rng.uniform() < 0.35 else 0.0))
            if r == 0 and rng.uniform() < 0.6:
                tip = K.along(path, t, depth + radius * 0.7, h - 0.12)
                for k in range(2):
                    off = Vector((rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), 0))
                    b.tube("Icicle", [tip + off, tip + off + Vector((0, 0, -rng.uniform(0.1, 0.22)))], [0.018, 0.002],
                           sides=6, style=icicle_style(3700 + i * 2 + k))
    # Snowdrift banked against the foot, rubble.
    for i, t in enumerate(np.linspace(0.12, 0.88, 3 if kind == "straight" else 2)):
        c = K.along(path, t, 0.12, -0.05)
        b.ellipsoid("Drift", c, (0.32, 0.18, 0.12), K.rz(math.degrees(K.facing(path, t)) + 90),
                    style=K.rock(K.pal(SNOW, SNOW * 0.8, SNOW * 0.5, SNOW * 0.6), 3750 + i, strata=0.0, crack=0.0,
                                 rough=0.85), segments=(24, 12))
    for i in range(3 if kind == "straight" else 1):
        c = K.along(path, rng.uniform(0.1, 0.9), rng.uniform(0.25, 0.4), 0.0)
        K.chip(b, c, (0.07, 0.06, 0.05), 3760 + i, column_style(3760 + i))
    # A low base behind the columns so no gap shows between them from the camera.
    for i, t in enumerate(np.linspace(0.2, 0.8, 2)):
        b.ellipsoid("Base", K.along(path, t, -0.55, 0.4), (0.75, 0.5, 0.5), K.rz(math.degrees(K.facing(path, t)) + 90),
                    style=K.rock(SLATE, 3780 + i, strata=0.0, crack=0.3, rough=0.8), segments=(24, 12),
                    deform=C.chiselled(12, 3780 + i, 0.12))
    if kind == "outer":
        for i, (x, y) in enumerate(((0.9, 0.9), (0.55, 1.1), (1.1, 0.55))):
            b.prism("Column", Vector((x, y, -0.1)), Vector((0, 0, 1)), 0.17, rng.uniform(1.5, 1.9), 0.025, sides=6,
                    twist=rng.uniform(0, 1), style=column_style(3790 + i), taper=0.97)
    return b


# ------------------------------------------------------------------------------------------------ decor

def ice_shard():
    """Spiralling, leaning ice prisms from a frozen root; the planet's heat glows amber in their hearts."""
    b = C.PartBuilder()
    rng = np.random.default_rng(4001)
    b.ellipsoid("Root", Vector((0, 0, 0.0)), (0.3, 0.26, 0.08), style=ice_style(4010), segments=(24, 12),
                deform=C.chiselled(12, 4010, 0.2))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        length = rng.uniform(0.45, 0.8) * (1.45 if i == 0 else 1.0)
        d = (out * (0.15 if i else 0.05) + Vector((0, 0, 1))).normalized()
        b.shard("Prism", out * (0.04 if i == 0 else 0.13), d, rng.uniform(0.07, 0.1) * (1.3 if i == 0 else 1.0), length,
                sides=6, twist=rng.uniform(0.8, 1.8), flute=0.22, rings=10, style=(Y.shard_style("frost", 0.75, seed=4020 + i) if i == 0 else
                       Y.shard_style("ice", 0.35, nodes=(0.4,), flute_glow=0.3, seed=4020 + i)),
                bend=out * 0.08, helix=(0.02, 0.5, a) if i == 0 else None)
    return b


def snow_style(seed):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        ridge = np.abs(np.sin(p[:, 0] * 22 + 2.0 * K.vn(p, 2, seed)))
        shade = C.smoothstep(0.2, 0.7, n[:, 1])
        col = SNOW * (0.8 + 0.18 * ridge)[:, None]
        col = C.lerp3(col, np.array((0.42, 0.5, 0.64)), shade * 0.6)
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = 0.004 * ridge
        s.rough[idx] = 0.75
        s.fam[idx] = (0.0, 0.0, 0.0, 0.0)  # snow: no rock or crystal micro-detail
        s.flow[idx] = p[:, 0]
    return style


def snowdrift():
    """A wind-carved drift: sastrugi ridges on the long windward back, a crust lip curling over the lee."""
    b = C.PartBuilder()
    L, Wd = 1.4, 0.8

    def top(u, v):
        x, y = -L / 2 + L * u, -Wd / 2 + Wd * v
        prof = (1 - min(1.0, abs(x) / (L / 2)) ** 2.2)
        crest = 0.12 + 0.1 * math.sin(x * 3)
        hgt = 0.46 * prof * (math.exp(-((y - crest) / 0.3) ** 2) if y < crest else math.exp(-((y - crest) / 0.07) ** 2))
        return Vector((x, y, hgt + 0.01))

    b.slab("Drift", top, lambda u, v: Vector((-L / 2 + L * u, -Wd / 2 + Wd * v, -0.04)), 60, 40, style=snow_style(4110))
    b.ellipsoid("Lip", Vector((0.0, 0.17, 0.4)), (0.4, 0.06, 0.035), K.rz(-8), style=snow_style(4111), segments=(24, 10))
    for i in range(2):
        K.chip(b, (0.45 - i * 0.9, -0.25 + i * 0.4, 0.0), (0.08, 0.06, 0.06), 4120 + i, column_style(4120 + i))
    return b


def frozen_crystal():
    """Pink Crysium bursting out of a cracked slab of lake ice: the slab is low and broken, the crystal pushes up
    through it and glows faintly pink inside the ice around it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(4201)
    base = ice_style(4210, cap=0.5)

    def slab_style(s, idx, part):
        base(s, idx, part)
        r = np.hypot(s.p[idx][:, 0], s.p[idx][:, 1])
        core = 1 - C.smoothstep(0.05, 0.3, r)
        pink = np.array((1.0, 0.45, 0.82))
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.62, 0.42, 0.7)), core * 0.7)
        s.emit[idx] = np.clip(s.emit[idx] + pink[None, :] * (core * 0.5)[:, None], 0, 1)

    for i, (x, y, rot, tip) in enumerate(((-0.2, 0.05, 10, 14), (0.18, 0.12, -25, -12), (0.02, -0.2, 70, 10))):
        b.ellipsoid("Slab", Vector((x, y, 0.07)), (0.22, 0.17, 0.08),
                    K.rz(rot) @ Matrix.Rotation(math.radians(tip), 3, "X"), style=slab_style, segments=(28, 12),
                    deform=C.chiselled(12, 4220 + i, 0.15, squash=(0.6, 1.0)))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        length = rng.uniform(0.38, 0.6) * (1.4 if i == 0 else 1.0)
        b.shard("Crysium", out * (0.02 if i == 0 else 0.07) + Vector((0, 0, 0.02)), out * (0.15 if i == 0 else 0.6) + Vector((0, 0, 1)),
                0.06 if i == 0 else 0.045, length, sides=8, twist=1.4, flute=0.4,
                style=Y.shard_style("pink" if i % 2 == 0 else "rose", 0.9, seed=4230 + i))
    for i in range(3):
        a = rng.uniform(0, 2 * math.pi)
        K.chip(b, (math.cos(a) * 0.42, math.sin(a) * 0.38, 0.0), (0.07, 0.06, 0.04), 4240 + i, ice_style(4240 + i))
    return b


def feather_style(seed):
    def style(s, idx, part):
        s.albedo[idx] = FROST * (0.9 + 0.1 * K.vn(s.p[idx], 30, seed))[:, None]
        s.emit[idx] = 0.0
        s.disp[idx] = 0.0
        s.rough[idx] = 0.5
        s.fam[idx] = E.FAM_CRYSTAL
        s.flow[idx] = s.p[idx][:, 2]
    return style


def rime_boulder():
    """Dark stone furred with frost feathers on its windward side, icicles from its overhang."""
    b = C.PartBuilder()
    rng = np.random.default_rng(4301)
    b.ellipsoid("Stone", Vector((0, 0, 0.22)), (0.38, 0.3, 0.28), K.rz(15), style=column_style(4310), segments=(36, 18),
                deform=C.chiselled(14, 4310, 0.18, squash=(0.7, 1.0)))
    b.ellipsoid("Overhang", Vector((0.12, -0.12, 0.42)), (0.26, 0.22, 0.1), K.rz(-20), style=column_style(4311),
                segments=(28, 12), deform=C.chiselled(12, 4311, 0.2))
    for i in range(9):
        a = rng.uniform(math.pi * 0.3, math.pi * 0.95)
        z = rng.uniform(0.15, 0.45)
        base = Vector((math.cos(a) * 0.33, math.sin(a) * 0.27, z))
        out = Vector((math.cos(a), math.sin(a), 0.2))
        b.shard("Feather", base, out, 0.025, rng.uniform(0.08, 0.14), sides=4, flute=0.5, style=feather_style(4320 + i))
    for i in range(5):
        x = -0.05 + i * 0.07
        tip = Vector((x + 0.06, -0.28 + 0.02 * (i % 2), 0.36))
        b.tube("Icicle", [tip, tip + Vector((0, 0, -rng.uniform(0.1, 0.2)))], [0.02, 0.002], sides=6,
               style=icicle_style(4340 + i))
    return b


def fern_style(seed):
    def style(s, idx, part):
        p = s.p[idx]
        t, radius, dist = C.tube_param(part, p)
        col = C.lerp3(np.array((0.18, 0.22, 0.3)), FROST * 0.95, C.smoothstep(0.1, 0.7, t))
        tip = C.smoothstep(0.8, 0.97, t)
        col = C.lerp3(col, AMBER * 0.7, tip)
        s.albedo[idx] = col * (1 + 0.1 * K.vn(p, 30, seed))[:, None]
        s.emit[idx] = np.clip(AMBER[None, :] * (tip * 0.9)[:, None], 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.4
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t * C.axis_length(part)
    return style


def frost_fern():
    """Stiff frozen fronds with comb spines; their tips glow amber."""
    b = C.PartBuilder()
    rng = np.random.default_rng(4401)
    b.ellipsoid("Crown", Vector((0, 0, 0.03)), (0.1, 0.1, 0.06), style=column_style(4410), segments=(16, 8))
    for i in range(6):
        a = i / 6 * 2 * math.pi + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.28, 0.4)
        h = rng.uniform(0.45, 0.62)
        pts = [out * 0.03 + Vector((0, 0, 0.05)), out * reach * 0.3 + Vector((0, 0, h * 0.55)), out * reach * 0.75 + Vector((0, 0, h * 0.95)),
               out * reach + Vector((0, 0, h))]
        b.tube("Frond", pts, [0.025, 0.02, 0.014, 0.004], sides=8, flatten=0.5, style=fern_style(4420 + i))
        side = out.cross(Vector((0, 0, 1)))
        for k in range(3):
            q = pts[1].lerp(pts[2], 0.2 + 0.3 * k)
            for sgn in (-1, 1):
                b.tube("Spine", [q, q + side * sgn * 0.06 + out * 0.03 + Vector((0, 0, 0.03))], [0.008, 0.002], sides=5,
                       style=fern_style(4440 + i * 6 + k))
    return b


def ice_chips():
    b = C.PartBuilder()
    rng = np.random.default_rng(4501)
    for i in range(5):
        c = (rng.uniform(-0.13, 0.13), rng.uniform(-0.13, 0.13), 0.0)
        sz = rng.uniform(0.03, 0.06)
        K.chip(b, c, (sz, sz * 0.8, sz * 0.75), 4510 + i, ice_style(4510 + i, cap=0.0), depth=0.3, segments=(12, 6))
    return b


KIT = {
    "RoadEdge_Straight": dict(fn=lambda: K.kerb_straight(kerb_style(3110), 3110), tris=700, voxel=0.007),
    "RoadEdge_OuterCorner": dict(fn=lambda: K.kerb_outer(kerb_style(3210), 3210), tris=500, voxel=0.007),
    "RoadEdge_InnerCorner": dict(fn=lambda: K.kerb_inner(kerb_style(3310), 3310), tris=400, voxel=0.007),
    "Bank_Straight": dict(fn=lambda: K.bank_straight(ice_style(3410), 3410, bank_accent), tris=600, voxel=0.008,
                          families=("crystal", "rock")),
    "Bank_OuterCorner": dict(fn=lambda: K.bank_outer(ice_style(3510), 3510, bank_accent), tris=400, voxel=0.008,
                             families=("crystal", "rock")),
    "Bank_InnerCorner": dict(fn=lambda: K.bank_inner(ice_style(3560), 3560), tris=300, voxel=0.008,
                             families=("crystal", "rock")),
    "Cliff_Straight": dict(fn=lambda: columns_piece("straight"), tris=1500, voxel=0.013, smoothing=1),
    "Cliff_OuterCorner": dict(fn=lambda: columns_piece("outer"), tris=1500, voxel=0.013, smoothing=1),
    "Cliff_InnerCorner": dict(fn=lambda: columns_piece("inner"), tris=1500, voxel=0.013, smoothing=1),
}

DECOR = {
    "Decor_IceShard": dict(fn=ice_shard, tris=1200, voxel=0.008, smoothing=1, sharp=40, families=("crystal", "rock")),
    "Decor_Snowdrift": dict(fn=snowdrift, tris=1000, voxel=0.01),
    "Decor_FrozenCrystal": dict(fn=frozen_crystal, tris=1300, voxel=0.008, smoothing=1, families=("crystal", "rock")),
    "Decor_RimeBoulder": dict(fn=rime_boulder, tris=1300, voxel=0.008),
    "Flora_FrostFern": dict(fn=frost_fern, tris=1300, voxel=0.005, smoothing=1, families=("membrane", "crystal"), sway=0.62),
    "Scatter_IceChips": dict(fn=ice_chips, tris=200, voxel=0.006, families=("crystal", "rock")),
}

DIORAMA = dict(
    sets=dict(ground="Ground", alt="GroundAlt", element="Snow", road="Road"),
    alt_areas=[((1.0, 1.0), 2.0), ((8.0, 0.6), 1.6), ((10.5, 5.5), 1.5)],
    element_areas=[((2.5, 7.3), 2.4), ((6.0, 6.6), 2.0), ((0.3, 2.6), 1.5), ((11.3, 0.4), 1.4)],
    liquid=dict(color=(0.5, 0.66, 0.8), rough=0.08, emit=(0.15, 0.3, 0.45), strength=0.4, z=-0.02, origin="Origin_Ice"),
    world=(0.012, 0.016, 0.026),
    mist=dict(density=0.3, color=(0.5, 0.58, 0.72)),
    lights=dict(key=2.3, rim=1.0, key_color=(0.88, 0.92, 1.0), rim_color=(0.5, 0.65, 1.0),
                points=[((1.3, 1.5), (1.0, 0.6, 0.25), 16), ((7.4, 0.4), (1.0, 0.6, 0.25), 14),
                        ((10, 2.5), (0.5, 0.75, 1.0), 25), ((4.0, 7.0), (1.0, 0.55, 0.25), 20)]),
    decor=[("Decor_IceShard", (1.2, 1.3), 0, 1.0), ("Decor_IceShard", (7.5, 0.3), 70, 0.8),
           ("Decor_IceShard", (11.2, 4.6), 0, 0.9), ("Decor_Snowdrift", (4.7, 6.8), 0, 0.9),
           ("Decor_Snowdrift", (0.4, 2.6), 90, 0.8), ("Decor_FrozenCrystal", (4.1, 0.6), 30, 1.0),
           ("Decor_FrozenCrystal", (3.0, 7.1), 0, 0.8), ("Decor_RimeBoulder", (0.2, 0.2), 0, 1.0),
           ("Decor_RimeBoulder", (11.3, 0.5), 140, 0.8), ("Decor_RimeBoulder", (8.0, 4.7), 40, 0.6),
           ("Flora_FrostFern", (0.6, 1.4), 0, 1.0), ("Flora_FrostFern", (8.3, 0.9), 50, 0.9),
           ("Flora_FrostFern", (10.6, 5.6), 0, 1.0), ("Flora_FrostFern", (4.6, 1.6), 0, 0.8)],
    scatter=["Scatter_IceChips"],
    scatter_count=30,
    towers=[("Tower_Cryo", "T2", (2, 4)), ("Tower_Railgun", "T1", (6, 4)), ("Tower_Gun", "T2", (9, 4)),
            ("Tower_Tesla", "T1", (6, 1)), ("Tower_Cannon", "T1", (10, 1))],
    enemies=[("Shardback", 0.85, None, [(1.3, 5.4, -90), (3.6, 4.0, 180), (6.8, 2.6, -90)]),
             ("Skitter", 0.45, None, [(0.6, 5.7, -90), (2.4, 5.8, -95), (4.0, 3.2, 175)])],
    close_target=(7.0, 4.2),
)


if __name__ == "__main__":
    K.run(CONT, SETS, KIT, DECOR, DIORAMA, world=(0.025, 0.03, 0.045))
