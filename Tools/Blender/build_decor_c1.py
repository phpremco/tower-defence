"""Round 26: Crystal Plains versions of decor kinds the round-8 set lacked (PropScatterBuilder fallbacks).

The round-17 IceShard (Northern Crown), DeadTree and Reed (Mirewood) read fine on Crystal Plains ground and are mapped
as they are (see ArtSource/README.md, round 26). The Northern Crown Snowdrift does not: a bright white sheet with a
straight cut edge that reads as a tarp on the dark violet plains. This builds the Crystal Plains drift instead:

- Decor_Snowdrift: a low, wind-carved drift of pale frost and Crysium dust with an organic outline that thins into the
  ground, sastrugi ridges on its back, a broken crust lip on the lee, a few frost needles and two small pink shards
  showing through, faint glow in the crust cracks.

Exports ArtSource/Exports/Env/Decor_Snowdrift/ (same four maps and report as the round-8 decor).

Run: blender --background --factory-startup --python Tools/Blender/build_decor_c1.py -- [--preview <dir>] [--norender]
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

FROST_TOP = np.array((0.62, 0.6, 0.72))
FROST_SIDE = np.array((0.38, 0.36, 0.48))
FROST_FOOT = np.array((0.16, 0.14, 0.22))
CRUST_SEAM = np.array((0.2, 0.12, 0.24))


def rz(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


def frost_style(seed):
    """Pale frost over dust: lighter on top, violet in the shade, sastrugi streaks along the wind (x), fine glitter,
    pink glow only in the crust cracks."""

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, FROST_TOP, FROST_SIDE, FROST_FOOT)
        streak = 0.5 + 0.5 * np.sin(p[:, 1] * 38 + E.vn(p * np.array((0.3, 1.0, 1.0)), 4, seed) * 3)
        col = col * (0.86 + 0.14 * streak)[:, None]
        foot = 1 - C.smoothstep(0.0, 0.07, p[:, 2])
        col = C.lerp3(col, FROST_FOOT * 1.3, foot * 0.7)
        cr = (1 - C.smoothstep(0.0, 0.03, np.abs(E.vn(p, 7, seed + 2)))) * C.smoothstep(-0.1, 0.4, E.vn(p, 2, seed + 3))
        cr *= C.smoothstep(0.03, 0.1, p[:, 2])
        col = C.lerp3(col, CRUST_SEAM, cr * 0.6)
        glitter = C.smoothstep(0.9, 0.98, E.vn(p, 60, seed + 4)) * C.smoothstep(0.4, 0.8, n[:, 2])
        col = C.lerp3(col, np.array((0.85, 0.88, 1.0)), glitter * 0.35)
        s.albedo[idx] = col
        s.emit[idx] = np.clip(E.VEIN_GLOW[None, :] * (cr * 0.28)[:, None] + np.array((0.6, 0.75, 1.0))[None, :] * (glitter * 0.15)[:, None], 0, 1)
        s.disp[idx] = 0.004 * E.vn(p, 16, seed + 5) + 0.006 * streak - 0.006 * cr
        s.rough[idx] = np.clip(0.7 - 0.25 * glitter + 0.1 * E.vn(p, 5, seed + 6), 0.3, 1)
        s.fam[idx] = E.FAM_ROCK
        s.flow[idx] = p[:, 0]
    return style


def snowdrift():
    b = C.PartBuilder()
    rng = np.random.default_rng(4601)
    harm = [(k, rng.uniform(0.05, 0.13) / k ** 0.6, rng.uniform(0, 2 * math.pi)) for k in range(2, 6)]
    L, Wd, Hm = 0.72, 0.42, 0.3

    def outline(a):
        r = 1.0 + sum(amp * math.cos(k * a + ph) for k, amp, ph in harm)
        return math.cos(a) * L * r, math.sin(a) * Wd * r

    crest_y = 0.12 * Wd

    def height(x, y, v=0.0):
        # Long gentle windward back (-y), a sharp crest, a steep short lee (+y): a wind-carved drift, not a dome.
        along = min(1.0, (x / (L * 1.05)) ** 2)
        if y <= crest_y:
            side = max(0.0, 1 - (crest_y - y) / (Wd * 1.25))
            prof = side ** 1.6
        else:
            prof = math.exp(-((y - crest_y) / (Wd * 0.22)) ** 2)
        wave = 1 + 0.08 * math.sin(x * 11 + y * 4)
        return Hm * (1 - along) ** 1.6 * prof * wave * (1 - v ** 2.2)

    def top(u, v):
        ex, ey = outline(u * 2 * math.pi)
        x, y = ex * v, ey * v
        return Vector((x, y, height(x, y, v) + 0.006))

    def bottom(u, v):
        a = u * 2 * math.pi
        ex, ey = outline(a)
        return Vector((ex * v, ey * v, -0.04))

    b.slab("Drift", top, bottom, 72, 24, style=frost_style(4610))
    # Broken crust lip curling over the lee side, in pieces.
    for i, (x, w) in enumerate(((-0.22, 0.2), (0.05, 0.17), (0.28, 0.13))):
        b.ellipsoid("Lip", Vector((x, crest_y + 0.02, height(x, crest_y) - 0.03)), (w, 0.055, 0.03),
                    rz(rng.uniform(-12, 12)) @ Matrix.Rotation(math.radians(-18), 3, "X"), style=frost_style(4620 + i),
                    segments=(20, 8), deform=C.chiselled(8, 4620 + i, 0.25, squash=(0.6, 1.0)))
    # Crust plates breaking off at the thin windward edge.
    for i in range(4):
        a = math.radians(rng.uniform(200, 340))
        ex, ey = outline(a)
        c = Vector((ex * 0.97, ey * 0.97, -0.004))
        b.ellipsoid("Plate", c, (rng.uniform(0.05, 0.08), rng.uniform(0.035, 0.055), 0.012), rz(math.degrees(a)),
                    style=frost_style(4630 + i), segments=(14, 6), deform=C.chiselled(6, 4630 + i, 0.3))
    # Frost needles and two small Crysium shards showing through the drift.
    for i in range(5):
        x = rng.uniform(-0.45, 0.45)
        y = rng.uniform(-0.15, 0.2)
        d = Vector((rng.uniform(-0.4, 0.4), rng.uniform(-0.6, 0.1), 1))
        b.shard("Needle", Vector((x, y, height(x, y) * 0.8)), d, rng.uniform(0.018, 0.03), rng.uniform(0.14, 0.24), sides=6,
                twist=1.2, flute=0.2, style=Y.shard_style("ice", 0.4, seed=4640 + i))
    for i, (x, y, dx, length) in enumerate(((0.38, 0.12, 0.5, 0.3), (-0.5, -0.02, -0.4, 0.22))):
        b.shard("Crysium", Vector((x, y, height(x, y) * 0.6)), Vector((dx, 0.15, 1)), 0.045, length, sides=8, twist=1.6, flute=0.4,
                style=Y.shard_style("pink", 0.9, seed=4650 + i))
    return b


ASSETS = {"Decor_Snowdrift": (snowdrift, 1000, 0.008, 2, None, ("rock", "crystal"), 1.0)}


def previews(built, out):
    C.preview_world((0.02, 0.02, 0.035))
    C.ground(color=(0.05, 0.05, 0.07))
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, asset in built.items():
        dims = asset.low.dimensions
        size = max(dims.x, dims.y, dims.z)
        cam = C.closeup_camera(Vector((0, 0, dims.z * 0.42)), size * 2.2, azimuth=-30, elevation=22, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 640, 480))
        bpy.data.objects.remove(cam)
    C.hide_lights(rig, True)


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    built = {}
    for name, (fn, tris, voxel, smoothing, sharp, families, k) in ASSETS.items():
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, sharp=sharp, families=families,
                        emission=2.2, k=k)
        E.finish(asset, {"round": 26, "prop_kind": "Snowdrift", "replaces": "North_Decor_Snowdrift as the Crystal "
                         "Plains fallback (PropScatterBuilder.cs:283-323)"})
        built[name] = asset
    C.save_blend("Env_Decor_C1")
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
