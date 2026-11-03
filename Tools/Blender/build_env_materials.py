"""Crystal Plains environment kit (round 8): the six seamless tiling material sets of the level ground.

- Road: the fused slab. Large pale lilac-grey polygonal plates (cooled-and-cracked glassy rock), worn smooth, pale
  claw scuffs, dark grit in the joints with a little Crysium dust. The light band of the level.
- Plaza: the same stone cut in large square slabs (running bond) with an engraved ring sigil full of glowing dust.
- Ground: dark blue-violet basalt crust in a fine curled crackle, gravel and glinting Crysium grit in the cracks.
- GroundFlora: the ground under a mat of plum lichen polyps (tiny cups) with pink and lime glowing pinpoints.
- Cliff: wall rock (V runs up): strata ledges, narrow columnar joints, mineral streaks and thin Crysium veins.
- RiftRock: near-black glassy basalt split by a network of amber fissures (the rift walls and floor).

Written with numpy, periodic on the tile by construction (wrapped noise, wrapped voronoi, wrapped differences).
Output: ArtSource/Exports/Env/Tiling/<Set>/ (see env_kit.py for the channel layout) and a 3 x 3 tiling test render
per set.

Run: blender --background --factory-startup --python Tools/Blender/build_env_materials.py -- [--preview <dir>]
     [--norender] [--only Road] [--size 1024]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import env_kit as E  # noqa: E402
from env_kit import bn, ss, mix, rgb  # noqa: E402

PINK = np.array((0.95, 0.58, 0.86))
ICE = np.array((0.78, 0.88, 1.0))
GLOW_PINK = np.array((1.0, 0.45, 0.82))
GLOW_ICE = np.array((0.7, 0.88, 1.0))
LIME = np.array((0.72, 1.0, 0.3))
AMBER = np.array((1.0, 0.6, 0.28))


def road(n):
    """The fused slab: big polygonal plates, smooth and pale, dark joints with grit, pale claw scuffs."""
    d1, edge, val, _ = E.voronoi(n, 4, 11, jitter=0.85, warp=0.12)
    joint = 1 - ss(0.0, 0.03, edge)
    bevel = ss(0.015, 0.1, edge)
    _, e2, _, _ = E.voronoi(n, 10, 12, warp=0.25)
    crack = (1 - ss(0.0, 0.022, e2)) * (bn(n, 1, 4, 13) > 0.2) * bevel
    scuff = E.scratches(n, 90, 17)
    pits = E.D.pits(n, 70, 16, 0.25, radius=0.3)
    g, gv = E.grains(n, 300, 21, 0.75, radius=0.38)
    grit = g * (1 - ss(0.02, 0.07, edge))
    h = (0.010 * bevel + 0.003 * (val - 0.5) - 0.012 * joint - 0.003 * crack + 0.0012 * bn(n, 40, 160, 14)
         + 0.002 * bn(n, 6, 20, 15) - 0.0015 * pits - 0.0005 * scuff + 0.004 * grit)

    tone = 0.9 + 0.2 * (val - 0.5) + 0.06 * bn(n, 3, 10, 18)
    col = rgb((0.57, 0.57, 0.65), n) * tone[..., None]
    col = mix(col, rgb((0.56, 0.52, 0.55), n), 0.3 * ss(0.2, 0.8, bn(n, 2, 6, 19)))
    polish = ss(0.1, 0.7, bn(n, 2, 5, 20)) * bevel
    col = col * (1 + 0.08 * polish)[..., None]
    grime = 1 - ss(0.03, 0.16, edge)
    col = mix(col, rgb((0.22, 0.21, 0.26), n), 0.5 * grime)
    col = mix(col, rgb((0.07, 0.065, 0.09), n), joint)
    col = mix(col, rgb((0.17, 0.16, 0.21), n), 0.7 * crack)
    col = mix(col, rgb((0.3, 0.3, 0.35), n), 0.6 * pits)
    dust = grit * (gv < 0.22)
    col = mix(col, rgb((0.32, 0.31, 0.37), n), grit * (gv >= 0.22))
    col = mix(col, rgb(PINK, n), dust)
    col = mix(col, rgb((0.76, 0.75, 0.82), n), 0.6 * scuff)
    smooth = np.clip(0.4 + 0.16 * polish - 0.32 * joint - 0.18 * grime - 0.15 * crack + 0.1 * scuff, 0.05, 1)
    ao = E.cavity_ao(h, 5, 0.006)
    return dict(albedo=col, height=h, smooth=smooth, dust=dust, ao=ao)


def plaza(n):
    """Large square slabs in a running bond with an engraved ring sigil filled with glowing Crysium dust."""
    u, v = E.coords(n)
    cols = rows = 4
    x, y = u * cols, v * rows
    row = np.floor(y)
    x2 = x + 0.5 * (row % 2)
    fx, fy = x2 % 1.0, y % 1.0
    slab_id = (np.floor(x2) % cols) + row * cols
    rng = np.random.default_rng(71)
    vals = rng.uniform(0, 1, cols * rows)
    val = vals[slab_id.astype(int)]
    wob = 0.012 * bn(n, 4, 12, 72)
    e = np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy)) + wob
    joint = 1 - ss(0.0, 0.02, e)
    bevel = ss(0.01, 0.06, e)
    # The sigil: two rings and eight spokes, centred on the tile, in metres from the centre.
    tile = E.TILE_WORLD["Plaza"]
    px, py = (u - 0.5) * tile, (v - 0.5) * tile
    r = np.hypot(px, py)
    a = np.arctan2(py, px)
    w = 0.022
    rings = np.maximum.reduce([1 - ss(0.0, w, np.abs(r - rr)) for rr in (0.38, 0.62, 1.05)])
    spoke_d = np.abs(np.sin((a + math.pi / 8) * 4)) * r
    spokes = (1 - ss(0.0, w, spoke_d)) * ((r > 0.62) & (r < 1.05))
    dots = np.maximum.reduce([1 - ss(0.0, 0.05, np.hypot(px - 0.84 * math.cos(k * math.pi / 4),
                                                          py - 0.84 * math.sin(k * math.pi / 4))) for k in range(8)])
    engr = np.clip(np.maximum.reduce([rings, spokes, dots]), 0, 1)
    worn = ss(-0.6, 0.4, bn(n, 2, 6, 73))  # the inlay is broken off in places
    engr = engr * (0.35 + 0.65 * worn)
    scuff = E.scratches(n, 60, 74)
    pits = E.D.pits(n, 60, 75, 0.25, radius=0.3)
    h = (0.008 * bevel + 0.003 * (val - 0.5) - 0.01 * joint - 0.005 * engr + 0.0012 * bn(n, 40, 160, 76)
         + 0.0015 * bn(n, 6, 20, 77) - 0.0012 * pits - 0.0004 * scuff)
    tone = 0.9 + 0.2 * (val - 0.5) + 0.05 * bn(n, 3, 10, 78)
    col = rgb((0.54, 0.54, 0.63), n) * tone[..., None]
    grime = 1 - ss(0.02, 0.12, e)
    col = mix(col, rgb((0.24, 0.23, 0.29), n), 0.45 * grime)
    col = mix(col, rgb((0.08, 0.075, 0.1), n), joint)
    col = mix(col, rgb((0.32, 0.31, 0.37), n), 0.6 * pits)
    col = mix(col, rgb((0.86, 0.6, 0.86), n), 0.85 * engr)
    col = mix(col, rgb((0.78, 0.77, 0.84), n), 0.5 * scuff)
    smooth = np.clip(0.42 - 0.3 * joint - 0.15 * grime + 0.1 * scuff + 0.1 * engr, 0.05, 1)
    emission = rgb(GLOW_PINK, n) * (engr * 0.75)[..., None]
    ao = E.cavity_ao(h, 5, 0.005)
    return dict(albedo=col, height=h, smooth=smooth, dust=engr, ao=ao, emission=emission)


def ground(n):
    """Dark basalt crust: curled plates in a bold crackle, pebbles, Crysium grit glinting in the cracks, pale dust
    in the hollows. Contrast sits at the scale of the plates (a few per cell) so it reads at game zoom."""
    d1, e, val, _ = E.voronoi(n, 7, 31, jitter=0.9, warp=0.3)
    crack = 1 - ss(0.0, 0.045, e)
    curl = 1 - ss(0.05, 0.25, e)
    _, e_fine, _, _ = E.voronoi(n, 19, 39, warp=0.3)
    fine_crack = (1 - ss(0.0, 0.03, e_fine)) * (1 - crack) * (bn(n, 1, 4, 40) > -0.2)
    large = bn(n, 1, 4, 32)
    gravel_zone = ss(-0.1, 0.5, bn(n, 2, 6, 36))
    peb, pv = E.grains(n, 46, 30, 0.5, radius=0.42)
    peb = peb * gravel_zone
    g, gv = E.grains(n, 150, 35, 0.6, radius=0.38)
    g = g * np.clip(gravel_zone + crack, 0, 1)
    fine_g, fgv = E.grains(n, 320, 37, 0.35, radius=0.4)
    fine_g = fine_g * np.clip(crack * 1.5 + 0.2, 0, 1)
    pits = E.D.pits(n, 50, 34, 0.4)
    h = (0.03 * large + 0.007 * (1 - crack) + 0.005 * curl * (1 - crack) - 0.014 * crack - 0.004 * fine_crack
         + 0.0015 * bn(n, 40, 200, 33) - 0.002 * pits + 0.012 * peb + 0.005 * g + 0.002 * fine_g)
    tone = 0.85 + 0.4 * (val - 0.5) + 0.1 * bn(n, 3, 10, 38)
    col = rgb((0.165, 0.165, 0.228), n) * tone[..., None]
    hollow = ss(0.0, -0.6, large)
    col = mix(col, rgb((0.27, 0.26, 0.32), n), 0.55 * hollow)
    col = col * (1 + 0.18 * curl * (1 - crack))[..., None]
    col = mix(col, rgb((0.04, 0.04, 0.06), n), crack * 0.95)
    col = mix(col, rgb((0.08, 0.08, 0.11), n), fine_crack * 0.6)
    col = mix(col, rgb((0.25, 0.25, 0.3), n) * (0.75 + 0.5 * ((pv * 17) % 1))[..., None], np.clip(peb * 1.6, 0, 1))
    stone = g * (gv >= 0.12)
    col = mix(col, rgb((0.27, 0.27, 0.32), n) * (0.8 + 0.4 * ((gv * 17) % 1))[..., None], stone)
    crys = np.maximum(g * (gv < 0.12), fine_g * (fgv < 0.1))
    tint = mix(rgb(PINK, n), rgb(ICE, n), ((gv * 31) % 1.0) > 0.5)
    col = mix(col, tint, crys)
    smooth = np.clip(0.12 + 0.05 * curl - 0.06 * crack + 0.5 * crys + 0.08 * peb, 0.03, 1)
    glint = crys * (((gv * 53) % 1.0) < 0.5)
    emission = mix(rgb(GLOW_PINK, n), rgb(GLOW_ICE, n), ((gv * 31) % 1.0) > 0.5) * (glint * 0.7)[..., None]
    ao = E.cavity_ao(h, 6, 0.008)
    return dict(albedo=col, height=h, smooth=smooth, dust=crys, ao=ao, emission=emission)


def ground_flora(n):
    """Plum lichen polyps (tiny cups) in patches over dark soil; pink and lime pinpoints glow in the cups."""
    # Mostly covered: the patch shapes come from the terrain's splat mask in the game, so the tile itself only has
    # small gaps of soil (big blotches would repeat visibly).
    cover = ss(-0.75, -0.15, bn(n, 4, 10, 41))
    d1, e, val, _ = E.voronoi(n, 28, 42, jitter=0.8)
    r = 0.26 + 0.12 * ((val * 7.3) % 1.0)
    rim = np.exp(-((d1 - r) / 0.07) ** 2)
    inside = 1 - ss(r * 0.6, r, d1)
    present = (val < 0.25 + 0.75 * cover) & (cover > 0.05)
    polyp = present * cover
    _, e2, v2, _ = E.voronoi(n, 13, 43, warp=0.3)
    pebble = (1 - ss(0.0, 0.05, e2))
    h = (0.01 * bn(n, 1, 4, 44) + 0.006 * polyp * rim - 0.002 * polyp * inside + 0.003 * cover
         + 0.0012 * bn(n, 50, 200, 45) - 0.003 * pebble * (1 - cover))
    soil = rgb((0.13, 0.11, 0.16), n) * (0.85 + 0.3 * v2)[..., None]
    soil = mix(soil, rgb((0.05, 0.045, 0.06), n), pebble * 0.7)
    flesh = mix(rgb((0.2, 0.08, 0.22), n), rgb((0.5, 0.2, 0.48), n), rim)
    flesh = mix(flesh, rgb((0.1, 0.03, 0.12), n), inside * 0.8)
    col = mix(soil, flesh, polyp)
    glow_dot = (1 - ss(0.0, 0.11, d1)) * polyp * ((val * 11.0) % 1.0 < 0.45)
    lime = ((val * 29.0) % 1.0) < 0.35
    col = mix(col, mix(rgb(GLOW_PINK, n), rgb(LIME, n), lime), glow_dot * 0.8)
    smooth = np.clip(0.12 + 0.3 * polyp * rim + 0.2 * glow_dot, 0.03, 1)
    emission = mix(rgb(GLOW_PINK, n), rgb(LIME, n), lime) * (glow_dot * 0.85)[..., None]
    ao = E.cavity_ao(h, 4, 0.004)
    return dict(albedo=col, height=h, smooth=smooth, dust=glow_dot, ao=ao, emission=emission)


def cliff(n):
    """Wall rock with V running up: strata ledges, narrow columnar joints, streaks and thin Crysium veins."""
    u, v = E.coords(n)
    warp = 0.25 * bn(n, 1, 4, 50)
    layer_coord = v * 6 + warp
    frac = layer_coord % 1.0
    layer_id = np.floor(layer_coord)
    layer_val = (np.sin(layer_id * 12.9898) * 43758.5453) % 1.0
    ledge = 0.03 * (1 - frac) ** 1.5 + 0.012 * layer_val
    _, joint_e, col_val, _ = E.voronoi(n, 2, 52, jitter=0.6, warp=0.15, stretch=(4, 1))
    joint = 1 - ss(0.0, 0.035, joint_e)
    _, _, streak_val, _ = E.voronoi(n, 1, 53, jitter=0.9, stretch=(28, 2))
    vein = (1 - ss(0.0, 0.012, np.abs(bn(n, 2, 6, 54)))) * ss(0.35, 0.55, bn(n, 1, 3, 55))
    h = ledge + 0.02 * bn(n, 3, 10, 56) + 0.004 * bn(n, 30, 120, 57) - 0.03 * joint - 0.004 * vein
    nrm = E.normal_from_height(h, E.TILE_WORLD["Cliff"])
    up = nrm[..., 1]
    col = rgb((0.28, 0.28, 0.36), n) * (0.75 + 0.3 * layer_val + 0.12 * (col_val - 0.5))[..., None]
    col = col * (0.88 + 0.2 * streak_val)[..., None]
    col = mix(col, rgb((0.46, 0.46, 0.56), n), 0.6 * ss(0.15, 0.6, up))
    col = mix(col, rgb((0.1, 0.1, 0.14), n), 0.5 * ss(0.1, 0.5, -up))
    col = mix(col, rgb((0.05, 0.05, 0.07), n), joint * 0.9)
    col = mix(col, rgb(PINK * 0.8, n), vein)
    smooth = np.clip(0.14 + 0.1 * layer_val - 0.08 * joint + 0.35 * vein, 0.03, 1)
    emission = rgb(GLOW_PINK, n) * (vein * 0.6)[..., None]
    ao = E.cavity_ao(h, 8, 0.02)
    return dict(albedo=col, height=h, smooth=smooth, dust=vein, ao=ao, emission=emission)


def rift_rock(n):
    """Near-black glassy basalt split by amber fissures that run hotter in places."""
    d1, e, val, _ = E.voronoi(n, 6, 61, warp=0.3)
    fiss = 1 - ss(0.0, 0.03, e)
    _, e2, _, _ = E.voronoi(n, 16, 62, warp=0.3)
    fine = (1 - ss(0.0, 0.02, e2)) * (bn(n, 1, 4, 67) > 0.0)
    heat = ss(-0.3, 0.6, bn(n, 1, 3, 63))
    sd1, _, _, _ = E.voronoi(n, 24, 66)
    h = (0.01 * ss(0.02, 0.2, e) - 0.014 * fiss - 0.004 * fine + 0.006 * bn(n, 4, 12, 64)
         + 0.0015 * bn(n, 40, 160, 65) + 0.003 * sd1 ** 2)
    glass = ss(0.2, 0.6, bn(n, 3, 9, 68))
    col = rgb((0.11, 0.095, 0.115), n) * (0.85 + 0.3 * (val - 0.5))[..., None]
    scorch = (1 - ss(0.02, 0.12, e)) * heat
    col = mix(col, rgb((0.32, 0.13, 0.06), n), 0.6 * scorch)
    col = mix(col, rgb(AMBER * 0.9, n), fiss * (0.4 + 0.6 * heat))
    col = mix(col, rgb(AMBER * 0.7, n), fine * heat * 0.6)
    smooth = np.clip(0.25 + 0.45 * glass * (1 - fiss) - 0.1 * scorch, 0.05, 1)
    emission = rgb(AMBER, n) * np.clip(fiss * (0.35 + 0.65 * heat) + 0.45 * fine * heat, 0, 1)[..., None]
    ao = E.cavity_ao(h, 5, 0.008)
    return dict(albedo=col, height=h, smooth=smooth, dust=fiss, ao=ao, emission=emission)


SETS = {"Road": road, "Plaza": plaza, "Ground": ground, "GroundFlora": ground_flora, "Cliff": cliff,
        "RiftRock": rift_rock}


def tiling_render(name, out):
    """A 3 x 3 tiling test: the tile repeated on a plane (seams and repetition show here), lit like a level."""
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    E.level_world((0.03, 0.03, 0.05), 1.0)
    tile = E.TILE_WORLD[name]
    mat = E.tiling_material(name + "_Tiling", name, tile_scale=3.0, emission_strength=2.0)
    size = tile * 3
    E.plane("Test", size, size, mat, uv_scale=1.0)
    E.level_lights(key=3.0, rim=0.8)
    cam = C.game_camera(Vector((0, 0, 0)), size * 1.02, pitch=70, yaw=0)
    path = os.path.join(out, f"env_tiling_{name.lower()}.png")
    C.render(cam, path, 900, 900)
    return path


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    n = int(args.get("size", 1024))
    only = args.get("only")
    renders = []
    for name, fn in SETS.items():
        if only and name != only:
            continue
        maps = fn(n)
        E.write_set(name, maps["albedo"], maps["height"], maps["smooth"], maps["dust"], maps["ao"],
                    maps.get("emission"))
        if not args.get("norender"):
            renders.append(tiling_render(name, out))
    if renders and not only:
        C.contact_sheet(renders, os.path.join(out, "sheet_env_tiling.png"), 3, 600)


main()
