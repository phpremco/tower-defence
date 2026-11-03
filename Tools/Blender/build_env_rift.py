"""Crystal Plains environment kit (round 8): the rift (the Stone element's spawn fissure), scree and small scatter.

- Rift_1x1 / Rift_2x1 / Rift_3x1: a jagged fissure running along X through 1, 2 or 3 cells (the piece covers the
  cells, Y from -0.5 to 0.5). Interlocking saw-tooth lips, heaved and cracked rims, steep strata walls with a
  ledge, a floor 0.8 below the plain and an amber glow rising from it; Crysium shards pushed up along the lips.
  The fissure's course, width and teeth repeat every cell, so the pieces chain end to end without a seam.
- Rift_Cap: the fissure's end: full profile at X = -0.5 (joins a chain piece), closing to nothing by X = +0.3.
- CrystalVein: a ground vein (decal-like strip, 2 x 0.6) of glowing Crysium cracks for the plain.
- Scree_Patch: a fan of loose broken stone round a slab (rift rims, cliff feet).
- Scatter (200 triangles or less, for instancing): Scatter_Pebbles, Scatter_CrystalSplinters,
  Scatter_ShellFragments (broken Burrower chitin).

Run: blender --background --factory-startup --python Tools/Blender/build_env_rift.py -- [--preview <dir>]
     [--norender] [--only Rift_1x1]
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

FLOOR = -0.8
SHELL = (np.array((0.78, 0.36, 0.12)), np.array((0.36, 0.07, 0.06)), np.array((0.92, 0.8, 0.64)))


def tri(x):
    """Triangle wave 0..1 with period 1."""
    f = x % 1.0
    return 1 - abs(2 * f - 1)


def fissure(x, cap):
    """Course of the fissure at x: (centre y, left lip y, right lip y, openness 0..1)."""
    taper = 1.0 - float(C.smoothstep(-0.25, 0.3, np.array(x))) if cap else 1.0
    yc = 0.05 * E.periodic(x, 1.0, 2, 3001)
    hw = (0.2 + 0.035 * E.periodic(x, 1.0, 3, 3002)) * taper
    amp = (0.045 + 0.02 * E.periodic(x, 1.0, 2, 3003)) * taper
    left = yc - hw - amp * tri(x * 6)
    right = yc + hw + amp * tri(x * 6 + 0.5)
    return yc, left, right, taper


def rift_top(length, cap):
    def f(u, v):
        x = -length / 2 + length * u
        y = -0.5 + v
        yc, left, right, taper = fissure(x, cap)
        edge_fade = 1 - float(C.smoothstep(0.36, 0.5, np.array(abs(y))))
        if left < y < right and taper > 0.02:
            t = min(y - left, right - y) / max((right - left) / 2, 1e-4)
            z = (-0.7 * float(C.smoothstep(0.0, 0.32, np.array(t))) - 0.1 * float(C.smoothstep(0.5, 0.75, np.array(t))))
            z = z * taper ** 0.5 + 0.03 * E.periodic(x * 2, 2.0, 3, 3010) * t
        else:
            d = left - y if y <= left else y - right
            d = max(d, 0.0)
            rim = 0.05 * math.exp(-(d / 0.07) ** 2) * (0.4 + 0.6 * taper)
            z = (rim + 0.012 * E.periodic(x, 1.0, 4, 3011 + int(abs(y) * 8))) * edge_fade
        return Vector((x, y, z))
    return f


def rift_bottom(length):
    def f(u, v):
        return Vector((-length / 2 + length * u, -0.5 + v, FLOOR - 0.2))
    return f


def rift_style(seed, period):
    top = E.stone(E.CRUST, seed, crack=0.7, dust=0.45, strata=0.0, period=period)
    wall = E.stone(E.OBSIDIAN, seed + 1, strata=45, period=period, veins=0.7, glow=E.EMBER, crack=0.5,
                   depth_glow=(-0.06, -0.6, E.EMBER, 1.0), rough=0.6)

    def style(s, idx, part):
        top(s, idx, part)
        saved = (s.albedo[idx].copy(), s.emit[idx].copy(), s.rough[idx].copy(), s.disp[idx].copy())
        wall(s, idx, part)
        z = s.p[idx][:, 2]
        w = C.smoothstep(-0.02, -0.12, z)
        for name, old in zip(("albedo", "emit", "rough", "disp"), saved):
            arr = getattr(s, name)
            new = arr[idx]
            arr[idx] = old + (new - old) * (w[:, None] if new.ndim == 2 else w)
        # The heaved rim right at the lip: paler broken crust, cracks glowing faintly where the heat reaches.
        rim = C.smoothstep(0.02, 0.045, z)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.3, 0.29, 0.34)), rim * 0.5)
    return style


def rift(length, cap=False, seed=3100):
    b = C.PartBuilder()
    period = 1.0
    b.slab("Fissure", rift_top(length, cap), rift_bottom(length), int(110 * length), 170, style=rift_style(seed, period))
    rng = np.random.default_rng(seed)
    cells = range(int(length)) if not cap else [0]
    for k in cells:
        x0 = -length / 2 + k
        for i in range(3):
            x = x0 + rng.uniform(0.14, 0.86)
            if cap and x > 0.1:
                x = rng.uniform(-0.4, 0.0)
            yc, left, right, taper = fissure(x, cap)
            side = 1 if rng.uniform() < 0.5 else -1
            lip = right if side > 0 else left
            base = Vector((x, lip + side * 0.025, -0.02))
            direction = Vector((rng.uniform(-0.3, 0.3), -side * rng.uniform(0.35, 0.8), 1.0))
            palette = "ember" if rng.uniform() < 0.35 else ("pink" if rng.uniform() < 0.7 else "ice")
            b.shard("Shard", base, direction, rng.uniform(0.035, 0.05), rng.uniform(0.14, 0.3), sides=6,
                    twist=1.4, flute=0.4, style=Y.shard_style(palette, 1.0, seed=seed + 10 * k + i))
        for i in range(2):
            x = x0 + rng.uniform(0.15, 0.85)
            if cap and x > 0.1:
                continue
            yc, left, right, taper = fissure(x, cap)
            side = 1 if i else -1
            lip = right if side > 0 else left
            c = Vector((x, lip + side * rng.uniform(0.07, 0.14), 0.0))
            b.ellipsoid("Rubble", c, (0.06, 0.045, 0.035), style=E.stone(E.BASALT, seed + 50 + i + k),
                        segments=(16, 8), deform=C.chiselled(9, seed + 50 + i + k, 0.25))
    return b


def crystal_vein():
    """A strip of plain (2 x 0.6) split by a glowing Crysium vein with branches: lies on the ground like a decal."""
    b = C.PartBuilder()
    rng = np.random.default_rng(3401)
    branches = [(rng.uniform(-0.8, 0.8), rng.choice([-1, 1]), rng.uniform(0.12, 0.25)) for _ in range(5)]

    def vein_mask(x, y):
        main = abs(y - 0.08 * E.periodic(x, 2.0, 4, 3402))
        m = max(0.0, 1 - main / 0.03)
        for bx, side, length in branches:
            t = (x - bx) / length
            if 0 <= t <= 1:
                by = 0.08 * E.periodic(bx, 2.0, 4, 3402) + side * t * 0.22
                m = max(m, (1 - abs(y - by) / 0.018) * (1 - t))
        return max(m, 0.0)

    def top(u, v):
        x, y = -1 + 2 * u, -0.3 + 0.6 * v
        fade = (1 - float(C.smoothstep(0.2, 0.3, np.array(abs(y))))) * (1 - float(C.smoothstep(0.85, 1.0, np.array(abs(x)))))
        m = vein_mask(x, y)
        return Vector((x, y, 0.004 + (0.02 * m + 0.006 * E.periodic(x * 3, 6.0, 3, 3403)) * fade))

    def bottom(u, v):
        return Vector((-1 + 2 * u, -0.3 + 0.6 * v, -0.02))

    base = E.stone(E.CRUST, 3410, crack=0.6, dust=0.4, strata=0.0)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        m = np.array([vein_mask(x, y) for x, y in p[:, :2]])
        m = C.smoothstep(0.1, 0.7, m)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.95, 0.75, 0.95)), m)
        s.emit[idx] = np.clip(s.emit[idx] + E.VEIN_GLOW[None, :] * (m * 0.95)[:, None], 0, 1)
        s.rough[idx] = s.rough[idx] - 0.4 * m
        s.fam[idx] = np.c_[np.zeros((len(idx), 2)), m, 1 - m]

    b.slab("Vein", top, bottom, 200, 60, style=style)
    return b


def scree_patch():
    b = C.PartBuilder()
    rng = np.random.default_rng(3501)
    b.ellipsoid("Slab", Vector((0, 0, 0.02)), (0.3, 0.22, 0.07), Matrix.Rotation(0.3, 3, "Z"),
                style=E.stone(E.BASALT, 3510, veins=0.5, dust=0.5), segments=(24, 12), deform=C.chiselled(12, 3510, 0.2))
    for i in range(13):
        a = rng.uniform(-1.2, 1.2) - math.pi / 2
        r = rng.uniform(0.22, 0.62)
        c = Vector((math.cos(a) * r, math.sin(a) * r * 0.8, 0.0))
        size = (rng.uniform(0.04, 0.1), rng.uniform(0.035, 0.08), rng.uniform(0.02, 0.045))
        rot = Matrix.Rotation(rng.uniform(0, 6.3), 3, "Z") @ Matrix.Rotation(rng.uniform(-0.4, 0.4), 3, "X")
        b.ellipsoid("Chip", c, size, rot, style=E.stone(E.BASALT if i % 3 else E.CRUST, 3520 + i, dust=0.4),
                    segments=(14, 7), deform=C.chiselled(8, 3520 + i, 0.3))
    return b


def pebbles():
    b = C.PartBuilder()
    rng = np.random.default_rng(3601)
    for i in range(6):
        r = rng.uniform(0.0, 0.22)
        a = rng.uniform(0, 2 * math.pi)
        size = rng.uniform(0.03, 0.065)
        b.ellipsoid("Pebble", Vector((math.cos(a) * r, math.sin(a) * r, size * 0.3)), (size, size * 0.85, size * 0.6),
                    Matrix.Rotation(rng.uniform(0, 6.3), 3, "Z"), style=E.stone(E.BASALT, 3610 + i, dust=0.3),
                    segments=(12, 6), deform=C.chiselled(7, 3610 + i, 0.3))
    return b


def crystal_splinters():
    b = C.PartBuilder()
    rng = np.random.default_rng(3701)
    b.ellipsoid("Root", Vector((0, 0, -0.01)), (0.06, 0.05, 0.03), style=E.stone(E.BASALT, 3710), segments=(12, 6),
                deform=C.chiselled(7, 3710, 0.3))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.4, 0.4)
        out = Vector((math.cos(a), math.sin(a), 0))
        b.shard("Splinter", out * rng.uniform(0.0, 0.07), out * rng.uniform(0.3, 0.9) + Vector((0, 0, 1)),
                rng.uniform(0.018, 0.03), rng.uniform(0.08, 0.22), sides=5, twist=1.0, flute=0.3,
                style=Y.shard_style("pink" if i % 3 else "ice", 1.0, seed=3720 + i))
    return b


def shell_style(seed):
    top, dark, bone = SHELL

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        L = C.local_points(part, p)
        col = C.chitin_gradient(n, top, dark, dark * 0.5)
        band = 1 - C.smoothstep(0.0, 0.08, np.abs(((L[:, 0] * 3) % 1.0) - 0.5) - 0.36)
        col = C.lerp3(col, dark * 0.5, band * 0.7)
        edge = C.smoothstep(0.7, 0.95, np.maximum(np.abs(L[:, 0]), np.abs(L[:, 1])))
        col = C.lerp3(col, bone, edge * 0.8)
        s.albedo[idx] = col * (1 + 0.1 * C.value_noise(p, 30, seed))[:, None]
        s.emit[idx] = 0.0
        s.disp[idx] = -0.002 * band
        s.rough[idx] = 0.55
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = L[:, 0] * 0.2
    return style


def shell_fragments():
    """Broken Burrower chitin: curved plates with growth bands and pale broken edges, one hooked spine."""
    b = C.PartBuilder()
    rng = np.random.default_rng(3801)
    for i in range(3):
        R = rng.uniform(0.13, 0.18)
        span_a = rng.uniform(0.5, 0.8)
        span_b = rng.uniform(0.4, 0.6)
        yaw = rng.uniform(0, 2 * math.pi)
        tilt = rng.uniform(-0.3, 0.3)
        centre = Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0.0))
        rot = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(tilt, 3, "X")

        def cap(r, R=R, span_a=span_a, span_b=span_b, rot=rot, centre=centre):
            def f(u, v):
                a = (u - 0.5) * span_a
                bb = (v - 0.5) * span_b
                p = Vector((math.sin(a) * r, math.sin(bb) * r, math.cos(a) * math.cos(bb) * r - R + 0.035))
                return centre + rot @ p
            return f

        b.slab("Shell", cap(R), cap(R - 0.014), 10, 8, style=shell_style(3810 + i),
               frame=(Matrix.Translation(centre) @ rot.to_4x4() @ Matrix.Diagonal((R * span_a / 2, R * span_b / 2, R, 1.0))).inverted())
    b.tube("Spine", [Vector((0.12, -0.1, 0.01)), Vector((0.2, -0.08, 0.05)), Vector((0.25, -0.02, 0.04))], [0.02, 0.012, 0.002],
           sides=8, style=shell_style(3820))
    return b


ASSETS = {
    # name: (builder, target tris, voxel, smoothing, sharp angle, families)
    "Rift_1x1": (lambda: rift(1.0, seed=3100), 900, 0.01, 1, None, ("rock", "crystal")),
    "Rift_2x1": (lambda: rift(2.0, seed=3200), 1200, 0.01, 1, None, ("rock", "crystal")),
    "Rift_3x1": (lambda: rift(3.0, seed=3300), 1500, 0.01, 1, None, ("rock", "crystal")),
    "Rift_Cap": (lambda: rift(1.0, cap=True, seed=3150), 800, 0.01, 1, None, ("rock", "crystal")),
    "CrystalVein": (crystal_vein, 300, 0.008, 1, None, ("rock", "crystal")),
    "Scree_Patch": (scree_patch, 600, 0.007, 2, None, ("rock", "crystal")),
    "Scatter_Pebbles": (pebbles, 150, 0.005, 2, None, ("rock", "crystal")),
    "Scatter_CrystalSplinters": (crystal_splinters, 200, 0.004, 1, 30, ("crystal", "rock")),
    "Scatter_ShellFragments": (shell_fragments, 200, 0.004, 1, None, ("chitin", "rock")),
}


def previews(built, out):
    C.preview_world((0.02, 0.02, 0.035))
    C.ground(color=(0.05, 0.05, 0.07))
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, asset in built.items():
        for other in built.values():
            other.low.hide_render = other is not asset
        dims = asset.low.dimensions
        size = max(dims.x, dims.y, 0.5)
        cam = C.closeup_camera(Vector((0, 0, -0.05 if name.startswith("Rift") else dims.z * 0.3)), size * 1.7 + 0.3,
                               azimuth=-25, elevation=48, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 640, 480))
        bpy.data.objects.remove(cam)
    if len(paths) > 1:
        C.contact_sheet(paths, os.path.join(out, "sheet_env_rift.png"), 3, 480)
    C.hide_lights(rig, True)


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    only = C.cli_args().get("only")
    built = {}
    for name, (fn, tris, voxel, smoothing, sharp, families) in ASSETS.items():
        if only and name not in only.split(","):
            continue
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, sharp=sharp, families=families,
                        emission=2.4)
        E.finish(asset)
        built[name] = asset
    C.save_blend("Env_Rift" if not only else "Env_Rift_" + only.replace(",", "_"))
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
