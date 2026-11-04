"""Ashen Reach environment set (round 17, continent 2: fire and sand). See ArtSource/DESIGN_NOTES.md round 17.

Tiling sets (Exports/Env/AshenReach/Tiling/Ash_<Set>/): Road (pale bone-ash sandstone slabs), Ground (charcoal
scoria crust with ember cracks), GroundAlt (fine grey ash, charcoal flakes), Sand (rust dune sand with ripples and
black glass grit), Cliff (stepped mesa strata, V up).
Kit (same rules as round 8): RoadEdge_Straight/_OuterCorner/_InnerCorner, Bank_Straight/_OuterCorner/_InnerCorner
(lava: rope crust, glowing waterline), Cliff_Straight/_OuterCorner/_InnerCorner (stepped mesa strata).
Decor: LavaRock, AshDune, Flora_CharredTree, Flora_EmberBloom, Fulgurite, Scatter_Cinders.

Run: blender --background --factory-startup --python Tools/Blender/build_env_ashenreach.py -- --preview <dir>
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

CONT = K.Continent("AshenReach", "Ash", {"Road": 2.0, "Ground": 3.0, "GroundAlt": 2.0, "Sand": 3.0, "Cliff": 3.0})

EMBER = np.array((1.0, 0.45, 0.12))
EMBER_HOT = np.array((1.0, 0.75, 0.35))
ASH = np.array((0.36, 0.34, 0.33))
SAND = np.array((0.5, 0.33, 0.2))
BONE = np.array((0.74, 0.68, 0.58))

# Stone palettes (light top, mid side, dark underside, seam).
SANDSTONE = K.pal((0.62, 0.57, 0.5), (0.4, 0.34, 0.29), (0.1, 0.08, 0.07), (0.1, 0.07, 0.06))
SCORIA = K.pal((0.24, 0.2, 0.19), (0.13, 0.1, 0.1), (0.04, 0.03, 0.03), (0.03, 0.02, 0.02))
CRUST = K.pal((0.11, 0.1, 0.1), (0.06, 0.05, 0.05), (0.02, 0.02, 0.02), (0.02, 0.01, 0.01))
MESA = K.pal((0.5, 0.36, 0.25), (0.33, 0.21, 0.14), (0.08, 0.05, 0.04), (0.06, 0.03, 0.02))


# ------------------------------------------------------------------------------------------------ tiling sets

def road(n):
    """Pale sintered sandstone slabs: large calm plates, dark joints with rust sand, pale scuffs, a few soot ghosts."""
    d1, edge, val, _ = E.voronoi(n, 4, 211, jitter=0.85, warp=0.12)
    joint = 1 - ss(0.0, 0.03, edge)
    bevel = ss(0.015, 0.1, edge)
    _, e2, _, _ = E.voronoi(n, 11, 212, warp=0.25)
    crack = (1 - ss(0.0, 0.02, e2)) * (bn(n, 1, 4, 213) > 0.25) * bevel
    scuff = E.scratches(n, 80, 217)
    pits = E.D.pits(n, 90, 216, 0.3, radius=0.3)
    g, gv = E.grains(n, 300, 221, 0.75, radius=0.38)
    grit = g * (1 - ss(0.02, 0.07, edge))
    layers = 0.5 + 0.5 * np.sin((E.coords(n)[1] * 9 + 0.6 * bn(n, 1, 4, 218)) * 2 * math.pi)
    h = (0.010 * bevel + 0.003 * (val - 0.5) - 0.012 * joint - 0.003 * crack + 0.0012 * bn(n, 40, 160, 214)
         + 0.002 * bn(n, 6, 20, 215) - 0.002 * pits - 0.0005 * scuff + 0.004 * grit + 0.0008 * layers)
    tone = 0.9 + 0.18 * (val - 0.5) + 0.06 * bn(n, 3, 10, 219)
    col = rgb((0.6, 0.55, 0.48), n) * tone[..., None]
    col = col * (0.95 + 0.07 * layers)[..., None]
    soot = ss(0.35, 0.8, bn(n, 1, 4, 220)) * bevel
    col = mix(col, rgb((0.36, 0.32, 0.29), n), 0.35 * soot)
    grime = 1 - ss(0.03, 0.16, edge)
    col = mix(col, rgb((0.25, 0.19, 0.15), n), 0.5 * grime)
    col = mix(col, rgb((0.08, 0.06, 0.05), n), joint)
    col = mix(col, rgb((0.2, 0.16, 0.13), n), 0.7 * crack)
    col = mix(col, rgb((0.33, 0.29, 0.25), n), 0.6 * pits)
    col = mix(col, rgb(SAND * 0.9, n), grit)
    col = mix(col, rgb((0.78, 0.74, 0.68), n), 0.55 * scuff)
    smooth = np.clip(0.36 - 0.3 * joint - 0.15 * grime - 0.12 * crack + 0.1 * scuff - 0.08 * pits, 0.05, 1)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=grit, ao=ao)


def ground(n):
    """Charcoal scoria crust: glassy cinder plates in a bold crackle, ember light deep in some cracks, porous pits,
    obsidian chips that catch the light."""
    d1, e, val, _ = E.voronoi(n, 7, 231, jitter=0.9, warp=0.3)
    crack = 1 - ss(0.0, 0.045, e)
    curl = 1 - ss(0.05, 0.25, e)
    _, e_fine, _, _ = E.voronoi(n, 19, 239, warp=0.3)
    fine_crack = (1 - ss(0.0, 0.028, e_fine)) * (1 - crack) * (bn(n, 1, 4, 240) > -0.1)
    large = bn(n, 1, 4, 232)
    heat = ss(0.1, 0.55, bn(n, 3, 9, 241))
    pits = E.D.pits(n, 180, 234, 0.55, radius=0.35)
    peb, pv = E.grains(n, 46, 230, 0.45, radius=0.42)
    g, gv = E.grains(n, 160, 235, 0.55, radius=0.38)
    h = (0.03 * large + 0.008 * (1 - crack) + 0.005 * curl * (1 - crack) - 0.016 * crack - 0.004 * fine_crack
         + 0.0015 * bn(n, 40, 200, 233) - 0.003 * pits + 0.012 * peb + 0.005 * g)
    tone = 0.85 + 0.35 * (val - 0.5) + 0.1 * bn(n, 3, 10, 238)
    col = rgb((0.2, 0.16, 0.15), n) * tone[..., None]
    col = mix(col, rgb((0.26, 0.22, 0.2), n), 0.4 * ss(0.0, -0.6, large))
    col = col * (1 + 0.15 * curl * (1 - crack))[..., None]
    col = mix(col, rgb((0.03, 0.02, 0.02), n), crack * 0.95)
    col = mix(col, rgb((0.07, 0.05, 0.05), n), fine_crack * 0.6)
    col = mix(col, rgb((0.1, 0.07, 0.06), n), pits * 0.7)
    col = mix(col, rgb((0.3, 0.22, 0.18), n) * (0.75 + 0.5 * ((pv * 17) % 1))[..., None], np.clip(peb * 1.6, 0, 1))
    glass = g * (gv < 0.25)
    col = mix(col, rgb((0.05, 0.04, 0.05), n), glass)
    col = mix(col, rgb((0.27, 0.22, 0.2), n), g * (gv >= 0.25))
    ember = crack * heat
    col = mix(col, rgb(EMBER * 0.55, n), ember * 0.8)
    smooth = np.clip(0.14 + 0.05 * curl - 0.06 * crack + 0.6 * glass - 0.06 * pits, 0.03, 1)
    emission = rgb(EMBER, n) * np.clip(ember * 0.6 + fine_crack * heat * 0.2, 0, 1)[..., None]
    ao = E.cavity_ao(h, 6, 0.008)
    return dict(albedo=col, height=h, smooth=smooth, dust=glass, ao=ao, emission=emission)


def ground_alt(n):
    """Fine grey ash in soft wind streaks, charcoal flakes, half-buried cinders, rare ember pinpoints."""
    u, v = E.coords(n)
    streak = bn(n, 2, 8, 251)
    drift = 0.5 + 0.5 * np.sin((u * 3 + v * 7 + 0.5 * bn(n, 1, 3, 252)) * 2 * math.pi)
    _, fe, fv, _ = E.voronoi(n, 26, 253, jitter=0.9, stretch=(1, 3))
    flake = (1 - ss(0.0, 0.06, fe)) * (fv < 0.15)
    cin, cv = E.grains(n, 40, 254, 0.35, radius=0.45)
    fine, fgv = E.grains(n, 260, 255, 0.4, radius=0.35)
    pin = fine * (fgv < 0.03)
    h = 0.012 * streak + 0.003 * drift + 0.008 * cin + 0.0015 * bn(n, 40, 200, 256) + 0.001 * fine
    col = rgb((0.27, 0.26, 0.25), n) * (0.9 + 0.12 * streak + 0.06 * drift)[..., None]
    col = mix(col, rgb((0.32, 0.31, 0.3), n), 0.4 * ss(0.3, 0.8, streak))
    col = mix(col, rgb((0.06, 0.05, 0.05), n), flake * 0.9)
    col = mix(col, rgb((0.12, 0.08, 0.07), n), cin)
    col = mix(col, rgb(EMBER * 0.7, n), pin)
    smooth = np.clip(0.06 + 0.04 * flake + 0.05 * cin, 0.02, 1)
    emission = rgb(EMBER, n) * (pin * 0.9 + cin * (cv < 0.08) * 0.35)[..., None]
    ao = E.cavity_ao(h, 4, 0.004)
    return dict(albedo=col, height=h, smooth=smooth, dust=flake, ao=ao, emission=emission)


def sand(n):
    """Rust dune sand: wind ripples bending round, coarser grains in the troughs, black glass grit that glints."""
    u, v = E.coords(n)
    warp = 0.06 * bn(n, 1, 3, 261)
    phase = (v * 14 + u * 2 + warp * 14 + 0.25 * bn(n, 2, 5, 262)) % 1.0
    ripple = np.where(phase < 0.7, phase / 0.7, (1 - phase) / 0.3)  # gentle windward, steep lee
    trough = 1 - ss(0.0, 0.25, ripple)
    dune = bn(n, 1, 3, 263)
    g, gv = E.grains(n, 340, 264, 0.55, radius=0.4)
    glass = g * (gv < 0.12)
    h = 0.03 * dune + 0.006 * ripple + 0.0012 * bn(n, 60, 220, 265) + 0.0015 * g * trough
    col = rgb((0.45, 0.29, 0.18), n) * (0.88 + 0.1 * ripple + 0.08 * dune)[..., None]
    col = mix(col, rgb((0.55, 0.38, 0.24), n), 0.35 * ss(0.6, 1.0, ripple))
    col = mix(col, rgb((0.3, 0.18, 0.11), n), 0.5 * trough)
    col = mix(col, rgb((0.04, 0.03, 0.04), n), glass * 0.9)
    smooth = np.clip(0.08 + 0.55 * glass, 0.02, 1)
    emission = rgb(EMBER_HOT, n) * (glass * (((gv * 53) % 1.0) < 0.25) * 0.35)[..., None]
    ao = E.cavity_ao(h, 4, 0.004)
    return dict(albedo=col, height=h, smooth=smooth, dust=glass, ao=ao, emission=emission)


def cliff(n):
    """Mesa strata, V up: ochre, rust and charcoal bands with ledges, vertical joints, ember in a few deep joints."""
    u, v = E.coords(n)
    warp = 0.18 * bn(n, 1, 4, 271)
    layer_coord = v * 7 + warp
    frac = layer_coord % 1.0
    layer_id = np.floor(layer_coord)
    layer_val = (np.sin(layer_id * 12.9898) * 43758.5453) % 1.0
    ledge = 0.035 * (1 - frac) ** 2 + 0.012 * layer_val
    _, joint_e, col_val, _ = E.voronoi(n, 2, 272, jitter=0.6, warp=0.12, stretch=(3, 1))
    joint = 1 - ss(0.0, 0.03, joint_e)
    heat = ss(0.3, 0.7, bn(n, 1, 3, 273))
    h = ledge + 0.015 * bn(n, 3, 10, 274) + 0.004 * bn(n, 30, 120, 275) - 0.03 * joint
    nrm = E.normal_from_height(h, CONT.tiles["Cliff"])
    up = nrm[..., 1]
    band = np.stack([layer_val] * 3, axis=-1)
    ochre = rgb((0.5, 0.34, 0.2), n)
    rust = rgb((0.4, 0.2, 0.12), n)
    char = rgb((0.24, 0.16, 0.13), n)
    col = mix(mix(ochre, rust, band[..., 0] > 0.45), char, band[..., 0] > 0.86)
    col = col * (0.8 + 0.25 * col_val + 0.1 * bn(n, 3, 10, 276))[..., None]
    col = mix(col, rgb((0.45, 0.42, 0.4), n), 0.55 * ss(0.15, 0.6, up))  # ash on the ledges
    col = mix(col, rgb((0.06, 0.04, 0.03), n), 0.5 * ss(0.1, 0.5, -up))
    col = mix(col, rgb((0.04, 0.03, 0.02), n), joint * 0.9)
    ember = joint * heat
    col = mix(col, rgb(EMBER * 0.6, n), ember * 0.6)
    smooth = np.clip(0.1 + 0.06 * layer_val - 0.05 * joint, 0.03, 1)
    emission = rgb(EMBER, n) * (ember * 0.75)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=ember, ao=ao, emission=emission)


SETS = {"Road": road, "Ground": ground, "GroundAlt": ground_alt, "Sand": sand, "Cliff": cliff}


# ------------------------------------------------------------------------------------------------ kit styles

def kerb_style(seed, road_dir=(0.0, -1.0)):
    base = K.rock(SANDSTONE, seed, strata=40, crack=0.45, dust=0.3, smooth_top=0.15, rough=0.75)
    rd = np.array(road_dir) / np.linalg.norm(road_dir)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        facing = C.smoothstep(0.1, 0.6, n[:, 0] * rd[0] + n[:, 1] * rd[1])
        low = 1 - C.smoothstep(0.0, 0.05, p[:, 2])
        f = facing * low * (0.6 + 0.4 * C.smoothstep(-0.5, 0.5, K.vn(p, 5, seed + 6)))
        s.albedo[idx] = C.lerp3(s.albedo[idx], EMBER * 0.5, f)
        s.emit[idx] = np.clip(s.emit[idx] + EMBER[None, :] * (f * 0.6)[:, None], 0, 1)
        # Soot licks up the ground side, rust sand banked at the foot.
        sand_foot = (1 - C.smoothstep(0.0, 0.07, p[:, 2])) * (1 - facing)
        s.albedo[idx] = C.lerp3(s.albedo[idx], SAND * 0.8, sand_foot * 0.7)
    return style


def kerb_accent(b, rng, kind):
    if kind == "straight":
        for i in range(3):
            K.chip(b, (rng.uniform(-0.45, 0.45), rng.uniform(0.15, 0.22), 0.0), (0.035, 0.03, 0.025), 2140 + i,
                   K.rock(SCORIA, 2140 + i, crack_glow=0.4, crack_color=EMBER))


def lava_bank_style(seed):
    """Rope-lava crust: near-black glassy rolls; the waterline glows where the crust meets the lava."""
    base = K.rock(CRUST, seed, strata=55, crack=0.5, rough=0.35, crack_glow=0.5, crack_color=EMBER)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        rope = 0.5 + 0.5 * np.sin(p[:, 0] * 60 + p[:, 1] * 60 + 3 * K.vn(p, 4, seed))
        s.albedo[idx] = s.albedo[idx] * (0.8 + 0.35 * rope)[:, None]
        s.disp[idx] += 0.004 * rope
        hot = 1 - C.smoothstep(-0.02, 0.025, p[:, 2])
        s.albedo[idx] = C.lerp3(s.albedo[idx], EMBER * 0.6, hot)
        s.emit[idx] = np.clip(s.emit[idx] + EMBER_HOT[None, :] * (hot * 0.95)[:, None], 0, 1)
        s.rough[idx] = s.rough[idx] - 0.15 * rope
    return style


def bank_accent(b, rng, kind):
    """Pahoehoe rolls lapping over the lip into the lava."""
    if kind != "straight":
        return
    for i in range(3):
        x = -0.32 + i * 0.32 + rng.uniform(-0.05, 0.05)
        b.tube("Roll", [Vector((x - 0.08, 0.02, 0.04)), Vector((x, -0.05, 0.045)), Vector((x + 0.06, -0.11, 0.0)),
                        Vector((x + 0.07, -0.13, -0.06))], [0.035, 0.04, 0.03, 0.02], sides=10,
               style=lava_bank_style(2400 + i))


def mesa_style(seed):
    """Banded strata by height (ochre, rust, charcoal), ash on the ledges, ember in the deep joints."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        q = p
        layer = np.floor(p[:, 2] * 5.5 + 0.35 * K.vn(q, 1.2, seed))
        lv = (np.sin(layer * 12.9898 + seed) * 43758.5453) % 1.0
        col = np.where((lv > 0.8)[:, None], np.array((0.17, 0.13, 0.12)),
                       np.where((lv > 0.45)[:, None], np.array((0.42, 0.22, 0.13)), np.array((0.52, 0.36, 0.22))))
        col = col * C.chitin_gradient(n, np.ones(3) * 1.15, np.ones(3) * 0.85, np.ones(3) * 0.3)
        col = col * (0.88 + 0.16 * K.vn(q, 6, seed + 1))[:, None]
        top = C.smoothstep(0.55, 0.85, n[:, 2])
        col = C.lerp3(col, np.array((0.44, 0.42, 0.4)) * (0.9 + 0.1 * K.vn(q, 12, seed + 2))[:, None], top * 0.75)
        joint = (1 - C.smoothstep(0.0, 0.035, np.abs(K.vn(q * np.array((1, 1, 0.25)), 4, seed + 3)))) * (1 - top)
        heat = C.smoothstep(0.0, 0.5, K.vn(q, 1.0, seed + 4))
        col = C.lerp3(col, np.array((0.05, 0.03, 0.02)), joint * 0.85)
        ember = joint * heat
        col = C.lerp3(col, EMBER * 0.6, ember)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(EMBER[None, :] * (ember * 0.9)[:, None], 0, 1)
        s.disp[idx] = -0.006 * joint + 0.004 * K.vn(q, 14, seed + 5)
        s.rough[idx] = 0.85 - 0.1 * top
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = p[:, 2]
    return style


MESA_H = 1.45
STEPS = ((0.0, 0.42), (0.5, 0.9), (0.98, 1.45))  # z ranges of the three risers


def mesa_profile(t, s):
    """Foot apron (ash fan) -> three risers with ledges -> top back into the wall. d > 0 towards the field. Ledge
    depth and riser heights wander along the wall (they merge and split), so it never reads as regular steps; at
    both ends they return to the base profile so pieces chain."""
    x = t * 2.0
    w = K.window(t, 0.15)
    a = 1.0 + 0.9 * E.periodic(x, 20.0, 3, 2603) * w        # ledge depth factor, 0.1..1.9
    b = 0.12 * E.periodic(x, 20.0, 2, 2604) * w             # riser height shift
    c = 0.1 * E.periodic(x * 1.7, 34.0, 3, 2605) * w
    l1, l2 = 0.08 * max(a, 0.1), 0.1 * max(2.0 - a, 0.1)
    z1, z2 = 0.42 + b, 0.92 + c
    pts = [(0.32, -0.05), (0.26, 0.0), (0.12, 0.05), (0.05, 0.12), (0.04, z1), (0.04 - l1 * 0.6, z1 + 0.03),
           (0.04 - l1, z1 + 0.08), (0.02 - l1 - 0.02, z2), (0.02 - l1 - l2 * 0.6, z2 + 0.03), (-l1 - l2, z2 + 0.06),
           (-l1 - l2 - 0.02, MESA_H - 0.04), (-l1 - l2 - 0.06, MESA_H), (-0.9, MESA_H + 0.02)]
    return K.polyline(pts, s)


def mesa_inner(t, s):
    return K.polyline([(0.2, -0.12), (-0.88, MESA_H - 0.25)], s)


def mesa_jitter(x, s, d, z):
    """Riser roughness and vertical clefts cut into the face every so often."""
    riser = 0.025 * E.periodic(x * 3, 60.0, 4, 2601 + int(z * 6))
    cleft = max(0.0, math.sin(x * 2.4 + 0.7)) ** 10 * 0.09 * (z > 0.05)
    return riser + 0.02 * E.periodic(x, 20.0, 3, 2602) - cleft, 0.0


def cliff_piece(kind):
    b = C.PartBuilder()
    rng = np.random.default_rng({"straight": 2610, "outer": 2620, "inner": 2630}[kind])
    path, length = K.cliff_sweep(b, "Mesa", kind, mesa_profile, mesa_inner, mesa_style(2611), mesa_jitter,
                                 radius=0.6 if kind == "outer" else 0.4)
    # Mass behind: a higher butte stepping back.
    for i, t in enumerate(np.linspace(0.15, 0.85, 3 if kind == "straight" else 2)):
        c = K.along(path, t, -0.95, MESA_H + 0.05)
        b.ellipsoid("Butte", c, (0.55, 0.45, 0.35), K.rz(math.degrees(K.facing(path, t))), style=mesa_style(2640 + i),
                    segments=(28, 14), deform=E.boxy(2640 + i, 0.25, 6, 0.15))
    if kind == "outer":
        # The wall quadrant behind the promontory: fill it so the corner never reads as a free-standing pillar.
        b.ellipsoid("Butte", Vector((0.95, 0.95, MESA_H - 0.1)), (0.75, 0.75, 0.3), K.rz(45), style=mesa_style(2645),
                    segments=(28, 14), deform=E.boxy(2645, 0.25, 6, 0.15))
        b.ellipsoid("Fill", Vector((0.9, 0.9, 0.6)), (0.65, 0.65, 0.75), K.rz(45), style=mesa_style(2646),
                    segments=(24, 12), deform=E.boxy(2646, 0.3, 4, 0.1))
    # Fallen blocks at the foot.
    for i in range(4 if kind == "straight" else 2):
        t = rng.uniform(0.1, 0.9)
        c = K.along(path, t, rng.uniform(0.3, 0.5), 0.0)
        K.chip(b, c, (rng.uniform(0.07, 0.12), rng.uniform(0.06, 0.1), rng.uniform(0.05, 0.08)), 2650 + i,
               mesa_style(2650 + i))
    return b


# ------------------------------------------------------------------------------------------------ decor

def lava_rock():
    """A rope-crust boulder split in two: matte cinder crust with glowing cracks, a hot seam between the halves."""
    b = C.PartBuilder()
    crust = lambda seed: K.rock(K.pal((0.2, 0.17, 0.16), (0.1, 0.08, 0.08), (0.03, 0.02, 0.02), (0.03, 0.02, 0.02)),
                                seed, strata=30, crack=0.6, rough=0.85, crack_glow=1.0, crack_color=EMBER)
    for i, (x, rot) in enumerate(((-0.17, -10), (0.18, 14))):
        b.ellipsoid("Half", Vector((x, 0.0, 0.2)), (0.22, 0.3, 0.26), K.rz(rot) @ Matrix.Rotation(math.radians(10 * (1 - 2 * i)), 3, "Y"),
                    style=crust(3010 + i), segments=(36, 18), deform=C.chiselled(14, 3010 + i, 0.2))
    b.ellipsoid("Core", Vector((0.0, 0.0, 0.17)), (0.09, 0.26, 0.2), style=K.glow_seam(EMBER_HOT, 1.0, EMBER * 0.7),
                segments=(16, 10), deform=C.chiselled(10, 3015, 0.2))
    b.ellipsoid("Foot", Vector((0.27, 0.22, 0.0)), (0.12, 0.1, 0.07), style=crust(3020), segments=(16, 8),
                deform=C.chiselled(9, 3020, 0.25))
    return b


def bone_style(seed):
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        ring = 1 - C.smoothstep(0.0, 0.08, np.abs(((t * 9) % 1.0) - 0.5) - 0.38)
        col = C.lerp3(BONE * 0.55, BONE, C.smoothstep(-0.2, 0.8, s.n[idx][:, 2]))
        col = C.lerp3(col, np.array((0.3, 0.24, 0.2)), ring * 0.6)
        s.albedo[idx] = col * (1 + 0.12 * K.vn(s.p[idx], 30, seed))[:, None]
        s.emit[idx] = 0.0
        s.disp[idx] = -0.003 * ring
        s.rough[idx] = 0.7
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = t * C.axis_length(part)
    return style


def dune_style(seed):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        ripple = 0.5 + 0.5 * np.sin(p[:, 1] * 38 + 0.6 * np.sin(p[:, 0] * 5) + 2 * K.vn(p, 2, seed))
        lee = C.smoothstep(0.1, 0.6, n[:, 1])
        col = C.lerp3(SAND, ASH * 0.9, C.smoothstep(-0.3, 0.4, K.vn(p, 1.5, seed + 1)))
        col = col * (0.85 + 0.2 * ripple)[:, None] * (1 - 0.25 * lee)[:, None]
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.disp[idx] = 0.004 * ripple * (1 - lee)
        s.rough[idx] = 0.95
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = p[:, 1]
    return style


def ash_dune():
    """A crescent drift (gentle windward, steep lee) with the half-buried rib arch of something ancient."""
    b = C.PartBuilder()
    L, Wd = 1.3, 0.9

    def top(u, v):
        x, y = -L / 2 + L * u, -Wd / 2 + Wd * v
        crest = 0.12 * math.sin(x * 2.2)
        yy = y - crest
        prof = (1 - min(1.0, abs(x) / (L / 2)) ** 2) * 0.34
        h = prof * (math.exp(-((yy + 0.05) / 0.32) ** 2) if yy < 0.05 else math.exp(-((yy - 0.05) / 0.12) ** 2 * 1.0))
        return Vector((x, y, h + 0.01))

    b.slab("Dune", top, lambda u, v: Vector((-L / 2 + L * u, -Wd / 2 + Wd * v, -0.04)), 60, 40, style=dune_style(3110))
    for i, x in enumerate((-0.36, -0.15, 0.06, 0.27)):
        r = 0.36 - 0.05 * abs(i - 1.5)
        top = 0.62 - 0.08 * abs(i - 1.5)
        pts = [Vector((x + 0.02 * i, -r * 1.1, 0.0)), Vector((x, -r * 0.75, top * 0.75)), Vector((x - 0.01, -0.05, top)),
               Vector((x, r * 0.45, top * 0.85)), Vector((x + 0.02, r * 0.8, top * 0.45)), Vector((x + 0.03, r * 0.85, top * 0.25))]
        b.tube("Rib", pts, [0.04, 0.035, 0.03, 0.026, 0.02, 0.006], sides=8, style=bone_style(3120 + i))
    b.tube("Spine", [Vector((-0.5, -0.04, 0.5)), Vector((0.0, -0.05, 0.64)), Vector((0.45, -0.03, 0.52))], [0.045, 0.05, 0.035],
           sides=10, style=bone_style(3130))
    for i, x in enumerate((-0.25, 0.0, 0.22)):
        b.tube("Barb", [Vector((x, -0.05, 0.6)), Vector((x + 0.02, -0.08, 0.72)), Vector((x + 0.05, -0.04, 0.78))],
               [0.025, 0.015, 0.003], sides=6, style=bone_style(3135 + i))
    return b


def bark_style(seed, ember=0.8):
    """Charred bark: black, cracked into scales that glow from inside, lighter ash on top."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        if part.axis is not None:
            t, radius, dist = C.tube_param(part, p)
        else:
            t = np.zeros(len(idx))
        q = p * np.array((1.0, 1.0, 0.35))
        scale = (1 - C.smoothstep(0.0, 0.045, np.abs(K.vn(q, 9, seed)))) * C.smoothstep(-0.4, 0.3, K.vn(p, 2.5, seed + 1))
        col = np.array((0.07, 0.06, 0.06)) * (0.8 + 0.4 * C.smoothstep(-0.5, 0.5, K.vn(p, 14, seed + 2)))[:, None]
        col = C.lerp3(col, ASH * 0.8, C.smoothstep(0.55, 0.95, n[:, 2]) * 0.6)
        heat = (1 - C.smoothstep(0.2, 0.9, t)) * ember
        col = C.lerp3(col, EMBER * 0.5, scale * heat)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(EMBER[None, :] * (scale * heat)[:, None], 0, 1)
        s.disp[idx] = -0.004 * scale
        s.rough[idx] = 0.85
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = t * (C.axis_length(part) if part.axis is not None else 1.0)
    return style


def charred_tree():
    """A spiral double trunk splitting into hooked, clawing branches; the bark still glows inside."""
    b = C.PartBuilder()
    rng = np.random.default_rng(3201)
    height = 1.15
    for k in range(2):
        pts = []
        for j in range(7):
            t = j / 6
            a = k * math.pi + t * 3.0
            r = 0.05 + 0.05 * (1 - t) ** 2
            pts.append(Vector((math.cos(a) * r, math.sin(a) * r, t * height)))
        b.tube("Trunk", pts, [0.085, 0.065, 0.055, 0.05, 0.045, 0.04, 0.035], sides=10, style=bark_style(3210 + k))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        side = out.cross(Vector((0, 0, 1)))
        z0 = height * rng.uniform(0.6, 0.95)
        reach = rng.uniform(0.35, 0.55)
        pts = [Vector((0, 0, z0)), out * reach * 0.4 + Vector((0, 0, z0 + 0.18)), out * reach * 0.8 + side * 0.06 + Vector((0, 0, z0 + 0.22)),
               out * reach + side * 0.12 + Vector((0, 0, z0 + 0.1)), out * reach * 0.92 + side * 0.16 + Vector((0, 0, z0 - 0.02))]
        b.tube("Branch", pts, [0.035, 0.028, 0.02, 0.012, 0.003], sides=8, style=bark_style(3220 + i, 0.4))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        out = Vector((math.cos(a), math.sin(a), 0))
        b.tube("Root", [Vector((0, 0, 0.18)), out * 0.16 + Vector((0, 0, 0.06)), out * 0.32 + Vector((0, 0, -0.03))],
               [0.06, 0.04, 0.012], sides=8, style=bark_style(3230 + i))
    return b


def leaf_style(seed):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        t, radius, dist = C.tube_param(part, p)
        rho = dist / np.maximum(radius, 1e-4)
        col = C.lerp3(np.array((0.12, 0.1, 0.1)), ASH * 1.1, C.smoothstep(0.1, 0.6, t))
        rim = C.smoothstep(0.75, 1.0, t) + C.smoothstep(0.7, 1.0, rho) * 0.5
        col = C.lerp3(col, np.array((0.55, 0.22, 0.1)), np.clip(rim, 0, 1) * 0.7)
        inner = C.smoothstep(0.2, -0.6, n[:, 2]) * 0.0 + C.smoothstep(0.3, 0.9, n[:, 2]) * (1 - C.smoothstep(0.3, 0.7, t))
        glow = inner * 0.6
        col = C.lerp3(col, EMBER * 0.6, glow)
        s.albedo[idx] = col * (1 + 0.12 * K.vn(p, 30, seed))[:, None]
        s.emit[idx] = np.clip(EMBER[None, :] * glow[:, None], 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.6
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t * C.axis_length(part)
    return style


def ember_bloom():
    """A rosette of ash-grey fleshy spoons cupping a glowing coal, three hooked stamens."""
    b = C.PartBuilder()
    rng = np.random.default_rng(3301)
    for i in range(8):
        a = i / 8 * 2 * math.pi + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        reach = rng.uniform(0.2, 0.28) * (1.15 if i % 2 else 1.0)
        pts = [out * 0.04 + Vector((0, 0, 0.03)), out * reach * 0.6 + Vector((0, 0, 0.08)), out * reach + Vector((0, 0, 0.2)),
               out * reach * 0.85 + Vector((0, 0, 0.3))]
        b.tube("Spoon", pts, [0.03, 0.07, 0.065, 0.01], sides=10, flatten=0.35, style=leaf_style(3310 + i))
    b.ellipsoid("Coal", Vector((0, 0, 0.1)), (0.07, 0.07, 0.06), style=K.glow_seam(EMBER_HOT, 1.0, EMBER * 0.7),
                segments=(16, 8), deform=C.chiselled(10, 3320, 0.25))
    for i in range(3):
        a = i / 3 * 2 * math.pi + 0.5
        out = Vector((math.cos(a), math.sin(a), 0))
        b.tube("Stamen", [Vector((0, 0, 0.12)), out * 0.04 + Vector((0, 0, 0.3)), out * 0.08 + Vector((0, 0, 0.36))],
               [0.012, 0.009, 0.003], sides=6, style=bark_style(3330 + i, 1.0))
    return b


def glass_style(seed):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        t, radius, dist = C.tube_param(part, p)
        col = np.array((0.06, 0.05, 0.06)) * (0.8 + 0.5 * C.smoothstep(0.3, 1.0, n[:, 2]))[:, None]
        crust = 1 - C.smoothstep(0.0, 0.35, t)
        col = C.lerp3(col, SAND * 0.75, crust * C.smoothstep(-0.3, 0.3, K.vn(p, 8, seed)))
        mouth = C.smoothstep(0.84, 0.94, t) * (1 - C.smoothstep(0.965, 0.99, t))
        hollow = C.smoothstep(0.975, 1.0, t)
        col = C.lerp3(col, EMBER * 0.55, mouth)
        col = C.lerp3(col, np.array((0.02, 0.01, 0.01)), hollow)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(EMBER[None, :] * (mouth * 0.7)[:, None], 0, 1)
        s.disp[idx] = 0.003 * K.vn(p, 25, seed + 1) * crust
        s.rough[idx] = 0.15 + 0.6 * crust
        s.fam[idx] = E.FAM_CRYSTAL
        s.flow[idx] = t * C.axis_length(part)
    return style


def fulgurite():
    """Branching hollow glass tubes fused from sand; their mouths glow."""
    b = C.PartBuilder()
    rng = np.random.default_rng(3401)
    b.ellipsoid("Mound", Vector((0, 0, 0.0)), (0.28, 0.24, 0.07), style=dune_style(3410), segments=(20, 10))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.4, 0.4)
        out = Vector((math.cos(a), math.sin(a), 0))
        h = rng.uniform(0.35, 0.75) * (1.3 if i == 0 else 1.0)
        lean = out * rng.uniform(0.05, 0.18)
        base = out * rng.uniform(0.0, 0.12)
        pts = [base, base + lean * 0.4 + Vector((0, 0, h * 0.4)), base + lean * 0.8 + Vector((0.02, -0.02, h * 0.75)),
               base + lean + Vector((0, 0, h))]
        r0 = rng.uniform(0.04, 0.065)
        knobs = [r0 * f for f in (1.25, 0.8, 1.15, 0.7, 1.05, 0.85, 0.9)]
        wiggle = [p + Vector((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 0)) for p in pts]
        b.tube("Tube", wiggle, knobs, sides=7, style=glass_style(3420 + i), cap=0.05)
        if i < 3:
            mid = pts[1].lerp(pts[2], 0.5)
            side = out.cross(Vector((0, 0, 1))) * (1 if i % 2 else -1)
            b.tube("Branch", [mid, mid + side * 0.09 + Vector((0, 0, 0.08)), mid + side * 0.13 + Vector((0, 0, 0.18))],
                   [r0 * 0.6, r0 * 0.5, r0 * 0.45], sides=8, style=glass_style(3430 + i), cap=0.15)
    return b


def cinders():
    b = C.PartBuilder()
    rng = np.random.default_rng(3501)
    for i in range(5):
        c = (rng.uniform(-0.13, 0.13), rng.uniform(-0.13, 0.13), 0.0)
        s = rng.uniform(0.03, 0.06)
        K.chip(b, c, (s, s * 0.85, s * 0.7), 3510 + i, K.rock(SCORIA, 3510 + i, crack_glow=0.9, crack_color=EMBER),
               depth=0.3, segments=(12, 6))
    return b


KIT = {
    "RoadEdge_Straight": dict(fn=lambda: K.kerb_straight(kerb_style(2110), 2110, kerb_accent), tris=700, voxel=0.007),
    "RoadEdge_OuterCorner": dict(fn=lambda: K.kerb_outer(kerb_style(2210, (-0.7, -0.7)), 2210), tris=500, voxel=0.007),
    "RoadEdge_InnerCorner": dict(fn=lambda: K.kerb_inner(kerb_style(2310, (-0.7, -0.7)), 2310), tris=400, voxel=0.007),
    "Bank_Straight": dict(fn=lambda: K.bank_straight(lava_bank_style(2410), 2410, bank_accent), tris=600, voxel=0.008),
    "Bank_OuterCorner": dict(fn=lambda: K.bank_outer(lava_bank_style(2510), 2510), tris=400, voxel=0.008),
    "Bank_InnerCorner": dict(fn=lambda: K.bank_inner(lava_bank_style(2560), 2560), tris=300, voxel=0.008),
    "Cliff_Straight": dict(fn=lambda: cliff_piece("straight"), tris=1500, voxel=0.014),
    "Cliff_OuterCorner": dict(fn=lambda: cliff_piece("outer"), tris=1500, voxel=0.014),
    "Cliff_InnerCorner": dict(fn=lambda: cliff_piece("inner"), tris=1500, voxel=0.014),
}

FLORA = ("membrane", "chitin")
DECOR = {
    "Decor_LavaRock": dict(fn=lava_rock, tris=1200, voxel=0.01),
    "Decor_AshDune": dict(fn=ash_dune, tris=1200, voxel=0.01, families=("rock", "chitin")),
    "Flora_CharredTree": dict(fn=charred_tree, tris=1500, voxel=0.008, smoothing=1, families=FLORA, sway=1.4),
    "Flora_EmberBloom": dict(fn=ember_bloom, tris=1300, voxel=0.006, smoothing=1, families=FLORA, sway=0.36),
    "Decor_Fulgurite": dict(fn=fulgurite, tris=1200, voxel=0.007, smoothing=1, families=("crystal", "rock")),
    "Scatter_Cinders": dict(fn=cinders, tris=200, voxel=0.006),
}

DIORAMA = dict(
    sets=dict(ground="Ground", alt="GroundAlt", element="Sand", road="Road"),
    alt_areas=[((1.0, 1.0), 2.0), ((8.0, 0.6), 1.6), ((2.5, 7.4), 1.8)],
    element_areas=[((10.5, 5.5), 2.2), ((5.6, 6.3), 1.8), ((0.3, 2.8), 1.4)],
    liquid=dict(color=(0.3, 0.06, 0.02), rough=0.6, emit=(1.0, 0.28, 0.05), strength=0.55, z=-0.03, origin="Origin_Lava"),
    world=(0.016, 0.01, 0.01),
    mist=dict(density=0.32, color=(0.5, 0.32, 0.24)),
    lights=dict(key=2.1, rim=1.0, key_color=(1.0, 0.92, 0.84), rim_color=(1.0, 0.55, 0.35),
                points=[((8.5, 6), (1.0, 0.4, 0.1), 18), ((1.3, 1.5), (1.0, 0.5, 0.2), 14),
                        ((10, 2.5), (0.5, 0.75, 1.0), 25), ((6.5, 5.8), (1.0, 0.5, 0.2), 12)]),
    decor=[("Decor_LavaRock", (0.2, 0.2), 0, 1.0), ("Decor_LavaRock", (7.6, 0.3), 110, 0.8),
           ("Decor_AshDune", (10.6, 5.6), 10, 1.0), ("Decor_AshDune", (5.5, 6.6), 160, 0.8),
           ("Flora_CharredTree", (1.2, 1.3), 0, 1.0), ("Flora_CharredTree", (11.2, 0.6), 80, 0.8),
           ("Flora_CharredTree", (3.2, 7.0), 200, 0.9), ("Flora_EmberBloom", (0.5, 1.6), 0, 1.0),
           ("Flora_EmberBloom", (8.2, 0.9), 40, 0.9), ("Flora_EmberBloom", (7.4, 6.6), 0, 1.0),
           ("Decor_Fulgurite", (4.1, 0.6), 0, 1.0), ("Decor_Fulgurite", (11.3, 4.4), 60, 0.8),
           ("Decor_Fulgurite", (0.4, 7.0), 0, 0.9), ("@Crystal_B", (8.0, 4.6), 0, 0.8),
           ("Flora_EmberBloom", (4.4, 1.7), 0, 0.8), ("Decor_LavaRock", (1.6, 6.9), 30, 0.7)],
    scatter=["Scatter_Cinders"],
    scatter_count=34,
    towers=[("Tower_Gun", "T2", (2, 4)), ("Tower_Cannon", "T1", (6, 4)), ("Tower_Cryo", "T1", (9, 4)),
            ("Tower_Laser", "T1", (6, 1)), ("Tower_Cannon", "T2", (10, 1))],
    enemies=[("Slagstrider", 0.8, None, [(1.3, 5.4, -90), (3.6, 4.0, 180)]),
             ("Siftjaw", 0.75, None, [(2.4, 5.8, -95), (6.4, 2.6, -90)]),
             ("Skitter", 0.45, None, [(0.6, 5.7, -90), (4.0, 3.2, 175), (7.4, 2.3, -88)])],
    close_target=(7.0, 4.2),
)


if __name__ == "__main__":
    K.run(CONT, SETS, KIT, DECOR, DIORAMA, world=(0.035, 0.028, 0.025))
