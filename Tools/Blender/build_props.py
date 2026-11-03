"""Crystal Plains level props (round 2 design, see ArtSource/DESIGN_NOTES.md): three Crysium formations, three
rocks with Crysium veins and a burrow (nest) crater. Static meshes with baked AO, albedo, emission, roughness and
normal maps, each exported as its own FBX.

- Crystal_A "Helix": three fluted shards winding around each other, with fragments hovering around the top.
- Crystal_B "Monolith": a double-pointed shard floating above a ring of short shards, a glowing gap between them.
- Crystal_C "Stalk": a dark mineral stalk that forks twice like coral; every branch ends in a shard.

Run: blender --background --python Tools/Blender/build_props.py -- [--preview <dir>] [--compare <dir>] [--norender]
     [--only Rock_A]
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

# Level 1 palette (cut screenshots): blue-grey stone; Crysium veins in icy pink-white.
ROCK_TOP = np.array((0.4, 0.43, 0.52))
ROCK_SIDE = np.array((0.18, 0.19, 0.25))
ROCK_BOTTOM = np.array((0.05, 0.055, 0.075))
VEIN = np.array((1.0, 0.55, 0.88))
STALK = np.array((0.26, 0.17, 0.3))
EMBER = np.array((1.0, 0.42, 0.1))
RIB = (np.array((0.3, 0.08, 0.05)), np.array((0.08, 0.025, 0.02)), np.array((0.85, 0.66, 0.52)))


def rock_style(veins=0.0, seed=0, pools=()):
    """Hewn stone with strata; optional glowing Crysium veins and pools of light on the top surface (where a
    shard hovers above it). pools: list of ((x, y), radius)."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, ROCK_TOP, ROCK_SIDE, ROCK_BOTTOM)
        strata = 0.5 + 0.5 * np.sin(p[:, 2] * 26 + C.value_noise(p, 3, seed) * 2.5)
        col = col * (0.86 + 0.14 * strata)[:, None] * (1 + 0.12 * C.value_noise(p, 9, seed + 1))[:, None]
        pit = C.smoothstep(0.4, 0.75, C.value_noise(p, 45, seed + 6))
        s.disp[idx] = 0.006 * C.value_noise(p, 14, seed + 2) - 0.004 * strata - 0.004 * pit
        s.rough[idx] = 0.78 + 0.1 * C.value_noise(p, 6, seed + 3)
        emit = np.zeros((len(idx), 3))
        if veins:
            line = 1 - C.smoothstep(0.0, 0.06, np.abs(C.value_noise(p, 4.5, seed + 4)))
            line *= C.smoothstep(-0.4, 0.2, C.value_noise(p, 1.3, seed + 5))  # veins come and go
            s.disp[idx] -= 0.012 * line
            col = C.lerp3(col, VEIN * 0.55, line * veins)
            emit += VEIN[None, :] * (line * veins * 0.9)[:, None]
        top = C.smoothstep(0.4, 0.9, n[:, 2])
        for (cx, cy), r in pools:
            d = np.hypot(p[:, 0] - cx, p[:, 1] - cy)
            pool = np.exp(-(d / r) ** 2) * top
            col = C.lerp3(col, Y.GLOW_PINK * 0.5, pool * 0.6)
            emit += Y.GLOW_PINK[None, :] * (pool * 0.85)[:, None]
        s.albedo[idx] = col
        s.emit[idx] = np.clip(emit, 0, 1)
        s.fam[idx] = (0.0, 0.0, 0.0, 1.0)
        s.flow[idx] = p[:, 2] + 0.03 * C.value_noise(p, 2, seed + 9)
    return style


def stalk_style(seed=0):
    """Dark mineral flesh with fine glowing pink veins running up towards the shards."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        t = C.tube_param(part, p)[0] if part.axis is not None else np.full(len(idx), 0.5)
        vein = 1 - C.smoothstep(0.0, 0.07, np.abs(C.value_noise(p, 9, seed + 3)))
        col = STALK * (0.75 + 0.45 * C.smoothstep(-0.5, 0.8, n[:, 2]))[:, None]
        col = col * (1 + 0.15 * C.value_noise(p, 20, seed))[:, None]
        s.albedo[idx] = C.lerp3(col, VEIN * 0.5, vein * 0.6)
        s.emit[idx] = VEIN[None, :] * (vein * (0.25 + 0.6 * t))[:, None]
        s.disp[idx] = -0.006 * vein + 0.003 * C.value_noise(p, 30, seed + 1)
        s.rough[idx] = 0.72 - vein * 0.3
        s.fam[idx] = (0.0, 0.0, 0.0, 1.0)
        s.flow[idx] = t * (C.axis_length(part) if part.axis is not None else 0.2)
    return style


def crack_ground_style(seed=0, pit=0.42, outer=1.35):
    """Ground broken into slabs around a dark pit; glowing seams get hotter towards the opening."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        r = np.linalg.norm(p[:, :2], axis=1)
        rng = np.random.default_rng(seed)
        angles = rng.uniform(0, 2 * math.pi, 40)
        radii = np.sqrt(rng.uniform(0.05, 1.0, 40)) * outer
        seeds = np.c_[np.cos(angles) * radii, np.sin(angles) * radii, np.zeros(40)]
        e = C.voronoi_edge(np.c_[p[:, :2], np.zeros(len(p))], seeds)
        heat = 1 - C.smoothstep(pit, outer * 0.9, r)
        seam = 1 - C.smoothstep(0.0, 0.025 + 0.02 * heat, e)
        in_pit = 1 - C.smoothstep(pit * 0.7, pit, r)
        top = C.smoothstep(0.3, 0.9, n[:, 2])
        s.disp[idx] = (-0.03 * seam - 0.16 * in_pit) * top + 0.004 * C.value_noise(p, 12, seed)
        col = C.chitin_gradient(n, ROCK_TOP * 0.8, ROCK_SIDE, ROCK_BOTTOM) * (1 + 0.1 * C.value_noise(p, 8, seed))[:, None]
        col = C.lerp3(col, ROCK_BOTTOM * 0.5, np.maximum(seam * 0.8, in_pit))
        s.albedo[idx] = col
        bumps = 0.5 + 0.5 * C.value_noise(p, 18, seed + 7)
        s.emit[idx] = EMBER[None, :] * (seam * heat * top * 0.9 + in_pit * (0.35 + 0.65 * bumps ** 2))[:, None]
        s.rough[idx] = 0.85
        s.fam[idx] = np.c_[np.zeros((len(idx), 3)), 1.0 - in_pit]
        s.flow[idx] = p[:, 2]
    return style


def rib_style(seed=0):
    """Chitin ribs of the burrow: dark warm root, pale worn hook point, a hot underside facing the pit."""
    top, dark, bone = RIB

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, top, dark, dark * 0.5)
        col = C.lerp3(col, bone, C.smoothstep(0.6, 1.0, t) * 0.8)
        band = 1 - C.smoothstep(0.0, 0.06, np.abs(((t * 5) % 1.0) - 0.5) - 0.38)
        s.albedo[idx] = C.lerp3(col, dark * 0.4, band * 0.6) * (1 + 0.1 * C.value_noise(s.p[idx], 30, seed))[:, None]
        under = C.smoothstep(0.0, -0.7, n[:, 2]) * (1 - C.smoothstep(0.3, 0.7, t))
        s.emit[idx] = EMBER[None, :] * (under * 0.5)[:, None]
        s.disp[idx] = -0.004 * band
        s.rough[idx] = 0.65
        s.fam[idx] = (1.0, 0.0, 0.0, 0.0)
        s.flow[idx] = t * C.axis_length(part)
    return style


def ellipsoid_rock(b, name, centre, radii, seed, veins=0.0, depth=0.2, count=22, pools=()):
    b.ellipsoid(name, Vector(centre), radii, deform=C.chiselled(count, seed, depth, squash=(0.6, 1.0)),
                style=rock_style(veins, seed, pools), segments=(48, 24))


def bipyramid(t):
    """Profile of a double-pointed floating shard: widest a third of the way up."""
    return (math.sin(math.pi * t ** 0.85)) ** 0.8


def crystal_a():
    """Helix: three fluted shards winding around one another, fragments hovering around the top."""
    b = C.PartBuilder()
    ellipsoid_rock(b, "Base", (0, 0, 0.03), (0.4, 0.36, 0.15), 3, veins=0.6)
    for i, (length, radius, palette) in enumerate(((1.35, 0.1, "pink"), (1.05, 0.09, "ice"), (0.85, 0.085, "pink"))):
        start = i * 2 * math.pi / 3
        b.shard("Strand", Vector((0, 0, 0.02)), Vector((0.05, 0.02, 1)), radius, length, sides=6, twist=4.0,
                flute=0.6, helix=(0.24, 0.42, start), rings=16, style=Y.shard_style(palette, 1.0, seed=i))
    for i, (angle, height, size) in enumerate(((0.4, 1.05, 0.06), (2.6, 0.85, 0.05), (4.4, 1.2, 0.045))):
        centre = Vector((math.cos(angle) * 0.3, math.sin(angle) * 0.3, height))
        b.shard("Fragment", centre - Vector((0, 0, size * 1.6)), Vector((math.cos(angle) * 0.3, math.sin(angle) * 0.3, 1)),
                size, size * 3.2, sides=6, twist=1.2, flute=0.3, profile=bipyramid, style=Y.shard_style("ice", 1.1, seed=5 + i))
    for i, angle in enumerate((1.2, 3.3, 5.3)):
        base = Vector((math.cos(angle) * 0.22, math.sin(angle) * 0.2, 0.02))
        b.shard("Root", base, Vector((math.cos(angle) * 0.9, math.sin(angle) * 0.9, 1)), 0.05, 0.28, sides=8,
                twist=1.5, flute=0.4, style=Y.shard_style("pink", 0.9, seed=9 + i))
    return b


def crystal_b():
    """Monolith: a double-pointed shard floating above a ring of short shards, a pool of light on the rock below."""
    b = C.PartBuilder()
    ellipsoid_rock(b, "Base", (0, 0, 0.02), (0.36, 0.33, 0.13), 5, pools=[((0.0, 0.0), 0.13)])
    b.shard("Monolith", Vector((0, 0, 0.36)), Vector((0.04, -0.03, 1)), 0.17, 1.25, sides=6, twist=1.8, flute=0.5,
            profile=bipyramid, rings=14, style=Y.shard_style("pink", 1.1, nodes=(0.2, 0.42, 0.66), seed=20))
    for i in range(6):
        a = i / 6 * 2 * math.pi + 0.3
        base = Vector((math.cos(a) * 0.2, math.sin(a) * 0.18, 0.05))
        lean = Vector((math.cos(a) * 0.7, math.sin(a) * 0.7, 1))
        b.shard("Ring", base, lean, 0.045, 0.22 + 0.06 * (i % 2), sides=8, twist=1.8, flute=0.4,
                style=Y.shard_style("ice" if i % 2 else "pink", 0.9, seed=21 + i))
    for i, (angle, height) in enumerate(((1.0, 0.95), (3.9, 0.7))):
        centre = Vector((math.cos(angle) * 0.34, math.sin(angle) * 0.34, height))
        b.shard("Fragment", centre, Vector((math.cos(angle) * 0.2, math.sin(angle) * 0.2, 1)), 0.05, 0.18, sides=6,
                twist=1.0, flute=0.3, profile=bipyramid, style=Y.shard_style("ice", 1.1, seed=30 + i))
    return b


def tip_fan(b, rng, end, direction, radius, length, palettes):
    """Three shards bursting from a branch end: one straight on, two splayed."""
    side = direction.cross(Vector((0, 0, 1)))
    side = side.normalized() if side.length > 1e-4 else Vector((1, 0, 0))
    for angle, scale in ((0, 1.0), (38, 0.6), (-34, 0.5)):
        d = Matrix.Rotation(math.radians(angle), 3, side) @ direction
        d = Matrix.Rotation(rng.uniform(-0.6, 0.6), 3, direction) @ d
        palette = palettes[int(rng.integers(len(palettes)))]
        b.shard("Tip", end - direction * radius * 0.5, d, radius * 2.3 * scale ** 0.5, length * 1.3 * scale, sides=6,
                twist=2.0, flute=0.5, style=Y.shard_style(palette, 1.1, seed=int(rng.integers(100))))


def branch(b, rng, start, direction, length, radius, depth, palettes):
    """A knobbly stalk segment that forks into two thinner ones; the last forks burst into shards."""
    direction = direction.normalized()
    bend = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)) * length * 0.18
    end = start + direction * length + bend
    mid = start.lerp(end, 0.5) + bend * 0.5
    b.tube("Stalk", [start, mid, end], [radius, radius * 0.8, radius * 0.75], sides=10, style=stalk_style(depth))
    b.ellipsoid("Knot", end, (radius * 1.05, radius * 1.05, radius * 0.95), style=stalk_style(depth + 10))
    # A shard breaking out of the side of every segment.
    out = direction.cross(Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0.2))).normalized()
    b.shard("Side", mid, (out + direction * 0.8).normalized(), radius * 0.9, length * 0.45, sides=6, twist=1.5,
            flute=0.45, style=Y.shard_style(palettes[int(rng.integers(len(palettes)))], 1.0, seed=int(rng.integers(100))))
    if depth == 0:
        tip_fan(b, rng, end, direction, radius, length * 0.95, palettes)
        return
    for sign in (-1, 1):
        axis = direction.cross(Vector((0, 0, 1)))
        if axis.length < 1e-4:
            axis = Vector((1, 0, 0))
        twist = Matrix.Rotation(rng.uniform(0, 2 * math.pi), 3, direction)
        child = Matrix.Rotation(sign * math.radians(rng.uniform(26, 38)), 3, twist @ axis.normalized()) @ direction
        child = (child + Vector((0, 0, 0.35))).normalized()
        branch(b, rng, end, child, length * 0.72, radius * 0.68, depth - 1, palettes)


def crystal_c():
    """Stalk: a dark mineral stalk that forks twice like coral, every branch ending in a shard."""
    b = C.PartBuilder()
    ellipsoid_rock(b, "Base", (0, 0, 0.0), (0.5, 0.42, 0.11), 7, depth=0.12, veins=0.8)
    rng = np.random.default_rng(4)
    b.ellipsoid("Bulb", Vector((0.02, 0, 0.04)), (0.14, 0.13, 0.07), style=stalk_style(30))
    branch(b, rng, Vector((0.02, 0, 0.06)), Vector((0.15, 0.05, 1)), 0.36, 0.095, 2, ["pink", "ice", "rose"])
    branch(b, rng, Vector((-0.2, 0.14, 0.03)), Vector((-0.6, 0.35, 1)), 0.24, 0.06, 1, ["ice", "pink"])
    return b


def rock_a():
    b = C.PartBuilder()
    ellipsoid_rock(b, "Boulder", (0, 0, 0.3), (0.55, 0.45, 0.42), 11, veins=0.35)
    ellipsoid_rock(b, "Chip", (0.45, 0.2, 0.1), (0.22, 0.2, 0.16), 12)
    return b


def rock_b():
    b = C.PartBuilder()
    ellipsoid_rock(b, "Slab", (0, 0, 0.12), (0.75, 0.5, 0.18), 13, veins=1.0, depth=0.1)
    ellipsoid_rock(b, "Slab2", (0.25, -0.1, 0.32), (0.42, 0.34, 0.15), 14, veins=1.0, depth=0.12)
    ellipsoid_rock(b, "Pebble", (-0.62, 0.35, 0.06), (0.16, 0.14, 0.1), 15)
    # Crysium breaking out of the seam between the slabs.
    for i, (x, y, dx, dy, length) in enumerate(((-0.25, 0.1, -0.5, 0.3, 0.32), (-0.1, 0.25, -0.1, 0.8, 0.24),
                                                 (-0.38, -0.05, -0.8, -0.3, 0.2))):
        b.shard("Crysium", Vector((x, y, 0.2)), Vector((dx, dy, 1)), 0.05, length, sides=8, twist=1.8, flute=0.4,
                style=Y.shard_style("pink", 1.0, seed=40 + i))
    return b


def rock_c():
    b = C.PartBuilder()
    ellipsoid_rock(b, "Spire", (0, 0, 0.62), (0.3, 0.27, 0.68), 16, veins=1.0, depth=0.15, count=14)
    ellipsoid_rock(b, "Foot", (0.18, 0.1, 0.12), (0.36, 0.3, 0.2), 17)
    for i, (z, a, length) in enumerate(((0.85, 0.6, 0.3), (0.55, 2.4, 0.24), (1.05, 4.0, 0.2))):
        d = Vector((math.cos(a), math.sin(a), 0.7))
        b.shard("Crysium", Vector((math.cos(a) * 0.22, math.sin(a) * 0.2, z)), d, 0.045, length, sides=8, twist=1.6,
                flute=0.4, style=Y.shard_style("ice" if i % 2 else "pink", 1.0, seed=50 + i))
    return b


def burrow():
    b = C.PartBuilder()
    # A low crater mound: its rim sinks into the terrain, while the carved pit floor stays above z=0.
    b.ellipsoid("Ground", Vector((0, 0, -0.12)), (1.4, 1.3, 0.32), style=crack_ground_style(41), segments=(64, 32))
    rng = np.random.default_rng(42)
    # Slabs heaved up and outwards around the opening, as if something burst through from below.
    for i in range(8):
        a = i / 8 * 2 * math.pi + rng.uniform(-0.15, 0.15)
        r = 0.6 + rng.uniform(-0.05, 0.08)
        size = (rng.uniform(0.2, 0.28), rng.uniform(0.12, 0.16), 0.07)
        tilt = math.radians(rng.uniform(30, 55))
        rot = Matrix.Rotation(a, 3, "Z") @ Matrix.Rotation(-tilt, 3, "Y") @ Matrix.Rotation(math.pi / 2, 3, "Z")
        centre = Vector((math.cos(a) * r, math.sin(a) * r, 0.2 + rng.uniform(0, 0.05)))
        b.ellipsoid("Slab", centre, (size[1], size[0], size[2]), rot, deform=C.chiselled(10, 50 + i, 0.15),
                    style=rock_style(0.0, 60 + i))
    # Chitin ribs rising from the rim and hooking in over the opening: the nest is alive, not just a hole.
    for i in range(6):
        a = i / 6 * 2 * math.pi + 0.25 + rng.uniform(-0.1, 0.1)
        out = Vector((math.cos(a), math.sin(a), 0))
        root = out * 0.5 + Vector((0, 0, 0.12))
        h = rng.uniform(0.45, 0.6)
        pts = [root, out * 0.56 + Vector((0, 0, h * 0.6)), out * 0.42 + Vector((0, 0, h)), out * 0.2 + Vector((0, 0, h * 0.88))]
        b.tube("Rib", pts, [0.07, 0.055, 0.035, 0.004], sides=10, style=rib_style(i))
    for i, a in enumerate((1.0, 2.9, 4.9)):
        base = Vector((math.cos(a) * 0.95, math.sin(a) * 0.9, 0.08))
        b.shard("Crysium", base, Vector((math.cos(a) * 0.6, math.sin(a) * 0.6, 1)), 0.05, 0.26, sides=8, twist=1.5,
                flute=0.4, style=Y.shard_style("pink", 0.9, seed=60 + i))
    return b


ASSETS = {
    # name: (builder, target tris, texture size, sharp facet angle, voxel, smoothing); crystals use a finer,
    # barely smoothed remesh so the flutes and points stay crisp.
    "Crystal_A": (crystal_a, 1450, 512, 30, 0.007, 1),
    "Crystal_B": (crystal_b, 1350, 512, 30, 0.007, 1),
    "Crystal_C": (crystal_c, 1480, 512, 30, 0.007, 1),
    "Rock_A": (rock_a, 500, 512, None, 0.011, 3),
    "Rock_B": (rock_b, 800, 512, None, 0.011, 3),
    "Rock_C": (rock_c, 700, 512, None, 0.011, 3),
    "Burrow_A": (burrow, 2600, 512, None, 0.011, 3),
}


# Detail families per prop in Mask order (B, A): the main material first.
FAMILIES = {name: ("crystal", "rock") if name.startswith("Crystal") else ("rock", "crystal") for name in ASSETS}
FAMILIES["Burrow_A"] = ("rock", "chitin")


MACRO_AIM = {"Crystal_A": 0.45, "Crystal_B": 0.55, "Crystal_C": 0.6, "Rock_A": 0.6, "Rock_B": 0.5, "Rock_C": 0.6,
             "Burrow_A": 0.4}


def previews(built, out):
    C.preview_world()
    C.ground()
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, asset in built.items():
        for other in built.values():
            other.low.hide_render = other is not asset
        dims = asset.low.dimensions
        size = max(dims.x, dims.y, dims.z)
        cam = C.closeup_camera(Vector((0, 0, dims.z * 0.42)), size * 2.6, azimuth=-35, elevation=24, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"prop_{name.lower()}.png"), 800, 600))
        bpy.data.objects.remove(cam)
        # Macro (round 3): a hand's width from the surface, aimed at the upper part of the prop.
        aim = Vector((0, 0, dims.z * MACRO_AIM.get(name, 0.5)))
        cam = C.macro_camera(aim, size * 0.55, -35, 20)
        C.render(cam, os.path.join(out, f"prop_{name.lower()}_macro.png"), 960, 540)
        bpy.data.objects.remove(cam)
        C.uv_layout(asset.low, os.path.join(out, f"prop_{name.lower()}_uv.png"), 512, background=asset.textures["albedo"])
    # A small field at game scale and camera: the props laid out like a level edge.
    layout = {"Crystal_A": (-2.2, 1.0, 0.3), "Crystal_B": (2.4, 1.4, 1.2), "Crystal_C": (0.4, 2.2, 2.0),
              "Rock_A": (-3.6, -0.6, 0.6), "Rock_B": (3.4, -0.8, 2.5), "Rock_C": (-1.2, 2.6, 0.0), "Burrow_A": (0.6, -0.5, 0.0)}
    for name, (x, y, rz) in layout.items():
        if name in built:
            o = built[name].low
            o.hide_render = False
            o.location = (x, y, 0)
            o.rotation_euler.z = rz
    C.hide_lights(rig, True)
    C.game_lights()
    for zoom, suffix in ((1.0, ""), (2.2, "_zoomed")):
        cam = C.game_camera(Vector((0, 0.6, 0)), 640 / C.GAME_PIXELS_PER_UNIT / zoom)
        paths.append(C.render(cam, os.path.join(out, f"props_game{suffix}.png"), 640, 360))
    C.contact_sheet(paths, os.path.join(out, "sheet_props.png"), 3, 480)
    C.compare_sheet("crystals", ["prop_crystal_a.png", "prop_crystal_b.png", "prop_crystal_c.png",
                                 "props_game_zoomed.png"], out)
    macros = [os.path.join(out, f"prop_{n.lower()}_macro.png") for n in built]
    C.contact_sheet([m for m in macros if os.path.exists(m)], os.path.join(out, "sheet_props_macro.png"), 3, 480)
    C.compare_sheet("rocks", ["prop_rock_a.png", "prop_rock_b.png", "prop_rock_c.png", "prop_burrow_a.png",
                              "props_game.png"], out)


def main():
    C.reset_scene()
    out = C.preview_dir()
    only = C.cli_args().get("only")
    built = {}
    for name, (fn, tris, tex, sharp, voxel, smoothing) in ASSETS.items():
        if only and name != only:
            continue
        asset = C.build_mesh_asset(name, fn(), voxel=voxel, target_tris=tris, tex_size=tex, ao_distance=0.3,
                                   extrusion=0.03, ray=0.1, emission_strength=2.2, smooth_iterations=smoothing,
                                   symmetric=False, low_from_detail=True, sharp_angle=sharp, families=FAMILIES[name])
        C.export_fbx(os.path.join(C.EXPORTS, name, name + ".fbx"), [asset.low], animated=False)
        C.write_report(name, {"asset": name, "game_tris": asset.low_tris, "source_tris": asset.high_tris, "texture_size": tex,
                              "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats,
                              "detail": asset.details})
        built[name] = asset
    C.save_blend("Props_CrystalPlains" if not only else "Props_" + only)
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
