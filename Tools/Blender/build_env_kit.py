"""Crystal Plains environment kit (round 8): the modular pieces the level code places along the layout.

One grid cell is 1 unit. Every piece has its pivot on the ground plane. A piece that borders something has that
thing on its local -Y side in Blender (Unity +Z after the FBX axis conversion), so Unity orients it with
Quaternion.LookRotation(direction towards the road / liquid / field / low side). Edge pieces are centred on the
middle of a cell side; corner pieces on the corner point, with the bordered thing in the (-X, -Y) quadrant.

- Road edge: RoadEdge_Straight, _OuterCorner (road on 1 of the 4 cells round the point), _InnerCorner (road on 3),
  _EndCap, _PlazaJoin (road kerb on -X, plaza kerb on +X); PlazaEdge_Straight, PlazaEdge_Corner.
- BuildPadTile (a whole cell), GatePlinth (road along Y, pylons at X = +-1 for a two-cell road).
- Map border: Cliff_Straight (2 cells), Cliff_InnerCorner (field on one quadrant), Cliff_OuterCorner (field on
  three), Chasm_Edge (2 cells; field on -Y, void on +Y).
- Banks: Bank_Straight, Bank_OuterCorner, Bank_InnerCorner (liquid on -Y / in the (-X, -Y) quadrant).
- Height: Slope (one cell, low at -Y, 0.55 high at +Y), TerraceStep (face along X, plateau on +Y),
  TerraceStep_Outer (plateau quadrant +X +Y), Ledge (lip trim along a terrace top edge).

Run: blender --background --factory-startup --python Tools/Blender/build_env_kit.py -- [--preview <dir>]
     [--norender] [--only RoadEdge_Straight]
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

PLATEAU = 0.55  # TerrainMap.PlateauHeight
GUNMETAL = (0.16, 0.17, 0.2)
CERAMIC = (0.8, 0.82, 0.85)
COLONY_BLUE = (0.16, 0.3, 0.56)


def rot_z(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


def tilt(rng, yaw=6, roll=5):
    return (rot_z(rng.uniform(-yaw, yaw)) @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "X")
            @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "Y"))


def block(b, name, centre, radii, seed, style, rot=None, power=0.3, chips=8, chip_depth=0.2):
    b.ellipsoid(name, Vector(centre), radii, rot, style=style, segments=(40, 20),
                deform=E.boxy(seed, power, chips, chip_depth))


def chip(b, centre, size, seed, style, depth=0.25):
    b.ellipsoid("Chip", Vector(centre), size, style=style, segments=(16, 8), deform=C.chiselled(9, seed, depth))


def splinter(b, base, direction, length, seed, palette="pink", radius=0.022):
    b.shard("Splinter", Vector(base), Vector(direction), radius, length, sides=6, twist=1.2, flute=0.35,
            style=Y.shard_style(palette, 1.0, seed=seed))


def kerb_style(seed):
    return E.stone(E.KERB, seed, foot=(0.022, -1.0), dust=0.35, crack=0.5, smooth_top=0.15)


def apron_style(seed):
    # The broken lip of the fused road slab: road coloured, smoother.
    return E.stone(E.SLAB, seed, crack=0.4, strata=0.0, smooth_top=0.3, rough=0.6)


def kerb_run(b, rng, x0, x1, seed, heights=(0.12, 0.17), y=0.03, depth=0.1):
    """Chiselled kerb blocks filling x0..x1 with small gaps, mostly on the ground side (+Y)."""
    span = x1 - x0
    count = max(1, int(round(span / 0.25)))
    w = rng.uniform(0.75, 1.25, count)
    w = w / w.sum() * span
    x = x0
    for i in range(count):
        gap = 0.012
        cx = x + w[i] / 2
        top = rng.uniform(*heights)
        rz = 0.08 + top / 2
        block(b, "Kerb", (cx, y + rng.uniform(-0.01, 0.015), top - rz), (w[i] / 2 - gap, depth, rz), seed + i,
              kerb_style(seed + i), rot=tilt(rng), power=0.28)
        x += w[i]


def road_edge_straight():
    b = C.PartBuilder()
    rng = np.random.default_rng(101)
    kerb_run(b, rng, -0.5, 0.5, 110)
    for i, (x0, x1) in enumerate(((-0.5, 0.02), (0.0, 0.5))):
        block(b, "Apron", ((x0 + x1) / 2, -0.1, -0.03), ((x1 - x0) / 2, 0.07, 0.05), 120 + i, apron_style(120 + i),
              rot=tilt(rng, 3, 2), power=0.35, chips=5)
    for i in range(2):
        x = rng.uniform(-0.35, 0.35)
        splinter(b, (x, -0.07, 0.0), (rng.uniform(-0.3, 0.3), -0.5, 1), rng.uniform(0.07, 0.11), 130 + i)
    for i in range(3):
        chip(b, (rng.uniform(-0.45, 0.45), rng.uniform(0.16, 0.24), 0.0), (0.035, 0.03, 0.025), 140 + i, E.stone(E.CRUST, 140 + i))
    return b


def road_edge_outer_corner():
    """Outside of a bend: the road is the (-X, -Y) cell; a big corner stone fills the gap where the two kerbs meet."""
    b = C.PartBuilder()
    rng = np.random.default_rng(201)
    block(b, "Corner", (0.06, 0.06, 0.0), (0.15, 0.15, 0.17), 210, kerb_style(210), rot=rot_z(45 + rng.uniform(-8, 8)),
          power=0.4)
    block(b, "Lean", (0.2, 0.17, -0.02), (0.08, 0.07, 0.1), 211, kerb_style(211), rot=tilt(rng, 30, 15))
    splinter(b, (-0.07, -0.07, 0.0), (-0.6, -0.6, 1), 0.12, 212)
    chip(b, (0.27, 0.05, 0.0), (0.04, 0.035, 0.03), 213, E.stone(E.CRUST, 213))
    return b


def road_edge_inner_corner():
    """Inside of a bend: the road is on three cells, the ground cell (+X, +Y) pokes in; a rounded stone caps it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(301)
    block(b, "Corner", (0.05, 0.05, -0.03), (0.12, 0.12, 0.17), 310, kerb_style(310), rot=rot_z(45), power=0.45)
    block(b, "Apron", (-0.06, -0.06, -0.03), (0.1, 0.1, 0.05), 311, apron_style(311), rot=rot_z(45), power=0.5)
    splinter(b, (-0.04, -0.1, 0.0), (-0.2, -0.6, 1), 0.09, 312, "ice")
    return b


def road_edge_end_cap():
    """The kerb stops: two blocks stepping down into the ground towards +X and a trail of rubble."""
    b = C.PartBuilder()
    rng = np.random.default_rng(401)
    block(b, "Kerb", (-0.3, 0.03, 0.15 - 0.165), (0.17, 0.1, 0.165), 410, kerb_style(410), rot=tilt(rng), power=0.28)
    block(b, "Kerb", (0.0, 0.04, 0.09 - 0.14), (0.12, 0.09, 0.14), 411, kerb_style(411), rot=tilt(rng, 10, 12), power=0.3)
    block(b, "Apron", (-0.2, -0.1, -0.03), (0.3, 0.07, 0.05), 412, apron_style(412), power=0.35, chips=5)
    for i, x in enumerate((0.17, 0.26, 0.34)):
        chip(b, (x, rng.uniform(0.0, 0.08), 0.0), (0.05 - i * 0.01, 0.04, 0.03), 413 + i, E.stone(E.KERB, 413 + i))
    return b


def plaza_kerb_style(seed):
    """Plaza kerb: the same stone but cut square, lighter, with an engraved channel of glowing dust along its top."""
    base = E.stone(E.SLAB, seed, foot=(0.05, -1.0), crack=0.3, dust=0.2, smooth_top=0.2)

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        n = s.n[idx]
        top = C.smoothstep(0.6, 0.9, n[:, 2])
        L = C.local_points(part, p)
        groove = (1 - C.smoothstep(0.0, 0.1, np.abs(L[:, 1]))) * top * (np.abs(L[:, 0]) < 0.85)
        s.albedo[idx] = C.lerp3(s.albedo[idx], E.VEIN * 0.7, groove)
        s.emit[idx] = np.clip(s.emit[idx] + E.VEIN_GLOW[None, :] * (groove * 0.8)[:, None], 0, 1)
        s.disp[idx] -= 0.006 * groove
    return style


def road_edge_plaza_join():
    b = C.PartBuilder()
    rng = np.random.default_rng(501)
    kerb_run(b, rng, -0.5, -0.02, 510)
    block(b, "PlazaKerb", (0.25, 0.04, 0.16 - 0.17), (0.235, 0.11, 0.17), 520, plaza_kerb_style(520), power=0.18, chips=4,
          chip_depth=0.08)
    block(b, "Apron", (-0.25, -0.1, -0.03), (0.25, 0.07, 0.05), 521, apron_style(521), power=0.35, chips=5)
    splinter(b, (-0.2, -0.07, 0.0), (0.2, -0.5, 1), 0.09, 522)
    return b


def plaza_edge_straight():
    b = C.PartBuilder()
    block(b, "PlazaKerb", (0.0, 0.04, 0.16 - 0.17), (0.48, 0.11, 0.17), 610, plaza_kerb_style(610), power=0.18, chips=6,
          chip_depth=0.08)
    return b


def plaza_edge_corner():
    """A square corner post where two plaza kerbs meet, a Crysium node set into its top."""
    b = C.PartBuilder()
    block(b, "Post", (0.04, 0.04, 0.28 - 0.3), (0.16, 0.16, 0.3), 710, plaza_kerb_style(710), power=0.18, chips=6,
          chip_depth=0.08)
    b.shard("Node", Vector((0.04, 0.04, 0.22)), Vector((0, 0, 1)), 0.07, 0.16, sides=6, twist=0.8, flute=0.3,
            style=Y.shard_style("pink", 1.1, seed=711))
    return b


def build_pad_tile():
    """Colony survey plate on a buildable cell: dark gunmetal, a cool light rim and four yellow corner ticks.
    The tower's own pad (0.72 across) stands in the middle; the rim stays visible around it."""
    b = C.PartBuilder()

    def rim(p, n):
        top = C.smoothstep(0.7, 0.95, n[:, 2])
        m = np.maximum(np.abs(p[:, 0]), np.abs(p[:, 1]))
        line = 1 - C.smoothstep(0.0, 0.012, np.abs(m - 0.405))
        return line * top

    def plate_style(s, idx, part):
        E.colony(GUNMETAL, 801, rough=0.55, glow_fn=rim, dirt=0.0)(s, idx, part)
        p = s.p[idx]
        n = s.n[idx]
        top = C.smoothstep(0.7, 0.95, n[:, 2])
        ax, ay = np.abs(p[:, 0]), np.abs(p[:, 1])
        m = np.maximum(ax, ay)
        # Yellow corner ticks (L shapes) just outside the light rim.
        tick = ((m > 0.425) & (m < 0.448) & (np.minimum(ax, ay) > 0.3)) * top
        s.albedo[idx] = C.lerp3(s.albedo[idx], E.HAZARD, tick)
        s.emit[idx] = np.clip(s.emit[idx] + E.HAZARD[None, :] * (tick * 0.35)[:, None], 0, 1)
        # A recessed inner panel with seams and four bolts; dust blown onto the outer band.
        panel = 1 - C.smoothstep(0.0, 0.008, np.abs(m - 0.33))
        bolt = np.zeros(len(idx))
        for sx in (-1, 1):
            for sy in (-1, 1):
                bolt = np.maximum(bolt, 1 - C.smoothstep(0.012, 0.02, np.hypot(p[:, 0] - sx * 0.37, p[:, 1] - sy * 0.37)))
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.06, 0.06, 0.08)), panel * top)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.42, 0.44, 0.48)), bolt * top)
        dust = C.smoothstep(0.36, 0.46, m) * C.smoothstep(-0.2, 0.5, C.value_noise(p, 9, 802)) * top * (1 - tick)
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.27, 0.26, 0.32)), dust * 0.6)
        s.disp[idx] = -0.004 * panel * top + 0.003 * bolt * top
        s.rough[idx] = s.rough[idx] + 0.25 * dust
        s.fam[idx] = E.FAM_METAL

    block(b, "Plate", (0.0, 0.0, 0.045 - 0.06), (0.46, 0.46, 0.06), 800, plate_style, power=0.12, chips=0)
    return b


def gate_plinth():
    """The footings a road gate stands on: two ceramic anchor blocks (the pylons stand at X = +-1) with bolt rings
    and claw anchors driven into the rock, a threshold sill across the road with hazard stripes and a lit groove
    for the field's floor, and rock heaved up round the footings."""
    b = C.PartBuilder()
    rng = np.random.default_rng(901)

    def strip(p, n):
        # Colony light strip on the road-facing side of each footing.
        side = C.smoothstep(0.5, 0.9, -np.sign(p[:, 0]) * n[:, 0])
        return side * (1 - C.smoothstep(0.0, 0.012, np.abs(p[:, 2] - 0.06)))

    for sx in (-1, 1):
        cx = sx * 1.0
        block(b, "Footing", (cx, 0.0, 0.13 - 0.16), (0.3, 0.36, 0.16), 910 + sx,
              E.colony(COLONY_BLUE, 911 + sx, rough=0.45, fam=E.FAM_CERAMIC, top=CERAMIC, glow_fn=strip),
              power=0.15, chips=3, chip_depth=0.06)
        for i in range(6):
            a = i / 6 * 2 * math.pi + 0.3
            p = Vector((cx + math.cos(a) * 0.22, math.sin(a) * 0.27, 0.13))
            b.tube("Bolt", [p - Vector((0, 0, 0.02)), p + Vector((0, 0, 0.025))], [0.028, 0.026], sides=10,
                   style=E.colony(GUNMETAL, 920 + i, rough=0.35))
        for i, a in enumerate((sx * 0.0, sx * 0.0 + 0.9 * sx, -0.9 * sx)):
            out = Vector((sx * math.cos(a), math.sin(a), 0))
            root = Vector((cx, 0, 0.04)) + out * 0.28
            b.tube("Claw", [root, root + out * 0.17 + Vector((0, 0, 0.03)), root + out * 0.3 + Vector((0, 0, -0.08))],
                   [0.04, 0.032, 0.012], sides=10, style=E.colony(GUNMETAL, 930 + i, rough=0.4,
                                                                stripes=(0.06, E.HAZARD)))
        for i in range(3):
            a = rng.uniform(-1.2, 1.2) + (0 if sx > 0 else math.pi)
            c = Vector((cx, 0, 0)) + Vector((math.cos(a), math.sin(a), 0)) * rng.uniform(0.42, 0.5)
            b.ellipsoid("Heave", c + Vector((0, 0, -0.02)), (0.12, 0.09, 0.07), tilt(rng, 40, 25),
                        style=E.stone(E.BASALT, 940 + i, veins=0.6), segments=(20, 10),
                        deform=C.chiselled(10, 940 + i + sx, 0.25))

    def sill_glow(p, n):
        top = C.smoothstep(0.7, 0.95, n[:, 2])
        return top * (1 - C.smoothstep(0.0, 0.03, np.abs(p[:, 1])))

    def sill_style(s, idx, part):
        E.colony(GUNMETAL, 950, rough=0.5, glow_fn=sill_glow, dirt=0.0)(s, idx, part)
        p = s.p[idx]
        n = s.n[idx]
        top = C.smoothstep(0.7, 0.95, n[:, 2])
        edge = C.smoothstep(0.12, 0.15, np.abs(p[:, 1]))
        stripe = C.smoothstep(0.45, 0.55, ((p[:, 0] + np.abs(p[:, 1])) / 0.12) % 1.0) * edge * top
        s.albedo[idx] = C.lerp3(s.albedo[idx], E.HAZARD, stripe)
        s.fam[idx] = E.FAM_METAL

    block(b, "Sill", (0.0, 0.0, 0.025 - 0.05), (0.75, 0.22, 0.05), 951, sill_style, power=0.12, chips=0)
    return b


def cliff_plate(b, rng, centre, facing, size, seed, lean=28, veins=0.7):
    """One leaning plate of a cliff wall: a flattened chiselled slab, its face towards `facing` (an angle in the
    XY plane), tipped back by `lean` degrees."""
    rot = rot_z(math.degrees(facing) + 90) @ Matrix.Rotation(math.radians(lean + rng.uniform(-6, 6)), 3, "X") \
        @ rot_z(rng.uniform(-6, 6))
    b.ellipsoid("Plate", Vector(centre), size, rot, style=E.stone(E.BASALT, seed, veins=veins, strata=14, dust=0.5),
                segments=(36, 18), deform=C.chiselled(16, seed, 0.16, squash=(0.8, 1.0)))


def cliff_straight():
    """Two cells of map-border wall: leaning basalt plates facing the field (-Y), a mass behind them, rubble at
    the foot and a Crysium cluster in a seam."""
    b = C.PartBuilder()
    rng = np.random.default_rng(1001)
    xs = np.linspace(-0.85, 0.85, 5) + rng.uniform(-0.06, 0.06, 5)
    for i, x in enumerate(xs):
        h = rng.uniform(0.75, 1.0)
        cliff_plate(b, rng, (x, 0.28 + rng.uniform(-0.04, 0.06), h), -math.pi / 2,
                    (rng.uniform(0.28, 0.36), 0.13, h + 0.1), 1010 + i)
    b.ellipsoid("Mass", Vector((0, 0.95, 0.85)), (1.15, 0.6, 0.95), style=E.stone(E.BASALT, 1020, veins=0.4, dust=0.6),
                segments=(40, 20), deform=C.chiselled(22, 1020, 0.14))
    for i in range(5):
        chip(b, (rng.uniform(-0.9, 0.9), rng.uniform(-0.12, 0.02), 0.0), (rng.uniform(0.06, 0.11), 0.06, 0.05), 1030 + i,
             E.stone(E.BASALT, 1030 + i))
    for i in range(3):
        splinter(b, (0.35 + i * 0.07, 0.15, 0.55 + i * 0.12), (0.2 * (i - 1), -0.8, 0.6), 0.22 - i * 0.04, 1040 + i,
                 "pink" if i != 1 else "ice", radius=0.045)
    return b


def cliff_inner_corner():
    """The field's own corner (the field is the (-X, -Y) quadrant): plates face the corner from both sides."""
    b = C.PartBuilder()
    rng = np.random.default_rng(1101)
    spots = [((0.75, 0.28), -math.pi / 2), ((0.35, 0.3), -math.pi * 0.62), ((0.28, 0.32), -math.pi * 0.75),
             ((0.28, 0.72), math.pi), ((0.3, 1.05), math.pi)]
    for i, ((x, y), face) in enumerate(spots):
        h = rng.uniform(0.75, 1.0)
        cliff_plate(b, rng, (x, y, h), face, (0.3, 0.13, h + 0.1), 1110 + i)
    b.ellipsoid("Mass", Vector((0.95, 0.95, 0.85)), (0.75, 0.75, 0.95), style=E.stone(E.BASALT, 1120, veins=0.4, dust=0.6),
                segments=(40, 20), deform=C.chiselled(22, 1120, 0.14))
    for i in range(3):
        chip(b, (0.1 + rng.uniform(0, 0.5), 0.05 + rng.uniform(0, 0.3), 0.0), (0.08, 0.06, 0.05), 1130 + i,
             E.stone(E.BASALT, 1130 + i))
    return b


def cliff_outer_corner():
    """A promontory of the border wall into the field (the field is on three quadrants round the point)."""
    b = C.PartBuilder()
    rng = np.random.default_rng(1201)
    for i, a in enumerate(np.linspace(-math.pi * 0.95, -math.pi * 0.05, 5) + math.pi / 2 * 0 - 0.0):
        face = a - math.pi / 2 + 0.0
        r = 0.32
        c = (0.3 + math.cos(a - math.pi / 2) * r, 0.3 + math.sin(a - math.pi / 2) * r)
        h = rng.uniform(0.85, 1.05)
        cliff_plate(b, rng, (c[0], c[1], h), a - math.pi / 2, (0.26, 0.13, h + 0.1), 1210 + i)
    b.ellipsoid("Mass", Vector((0.55, 0.55, 0.95)), (0.55, 0.55, 1.05), style=E.stone(E.BASALT, 1220, veins=0.5, dust=0.6),
                segments=(40, 20), deform=C.chiselled(20, 1220, 0.14))
    splinter(b, (0.1, 0.1, 1.2), (-0.5, -0.5, 1), 0.3, 1230, radius=0.06)
    splinter(b, (0.2, 0.05, 1.1), (-0.2, -0.8, 0.8), 0.2, 1231, "ice", radius=0.045)
    return b


def chasm_edge():
    """Two cells where the field breaks off into a void (+Y): a jagged lip, a strata face with an overhang that
    drops 2.4 units, faint violet light from the depth and Crysium veins down the face."""
    b = C.PartBuilder()
    L = 2.0
    top_share = 0.32

    def lip(t):
        x = -1 + 2 * t
        saw = abs(((x * 3.5) % 1.0) - 0.5) * 2  # 7 teeth over 2 cells: periodic
        return 0.05 * saw + 0.05 * E.periodic(x, L, 3, 1301)

    def out(t, s):
        if s < top_share:
            return -0.5 + (0.5 + lip(t)) * s / top_share, 0.0
        u = (s - top_share) / (1 - top_share)
        step = 0.06 * math.floor(u * 6) / 6
        return lip(t) - 0.25 * u ** 0.7 + step + 0.04 * math.sin(u * 20 + t * 2 * math.pi * 2), -2.4 * u

    def inner(t, s):
        if s < top_share:
            return -0.5 + 0.2 * s / top_share, -0.3
        u = (s - top_share) / (1 - top_share)
        return -0.55 - 0.1 * u, -0.3 - 2.15 * u

    def jitter(t, s, d, z):
        x = -1 + 2 * t
        if s < top_share:
            return 0.0, 0.015 * E.periodic(x * 3, L * 3, 3, 1302 + int(s * 10)) * (s / top_share)
        return 0.04 * E.periodic(x, L, 4, 1303 + int(z * 4)), 0.0

    E.sweep(b, "Chasm", E.line_path(-1, 1, 0.0, (0.0, 1.0)), out, inner, 140, 90,
            E.stone(E.BASALT, 1310, veins=0.25, strata=10, period=L, depth_glow=(-0.4, -2.0, E.VOID, 0.45), dust=0.3),
            jitter)
    rng = np.random.default_rng(1320)
    for i in range(4):
        chip(b, (rng.uniform(-0.85, 0.85), rng.uniform(-0.25, -0.05), 0.0), (0.07, 0.05, 0.04), 1321 + i,
             E.stone(E.BASALT, 1321 + i))
    return b


def bank_style(seed):
    # Wet stone at the waterline: darker and glossier low down, a crust line where the liquid rises and falls.
    base = E.stone(E.BASALT, seed, crack=0.4, dust=0.3)

    def style(s, idx, part):
        base(s, idx, part)
        z = s.p[idx][:, 2]
        wet = 1 - C.smoothstep(-0.02, 0.05, z)
        line = 1 - C.smoothstep(0.0, 0.012, np.abs(z - 0.045))
        s.albedo[idx] = s.albedo[idx] * (1 - 0.45 * wet)[:, None]
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.55, 0.52, 0.6)), line * 0.6)
        s.rough[idx] = s.rough[idx] - 0.45 * wet
    return style


def bank_run(b, rng, x0, x1, seed):
    span = x1 - x0
    count = max(1, int(round(span / 0.28)))
    for i in range(count):
        cx = x0 + (i + 0.5) * span / count + rng.uniform(-0.03, 0.03)
        b.ellipsoid("Stone", Vector((cx, -0.04 + rng.uniform(-0.03, 0.03), -0.03)),
                    (span / count * 0.62, rng.uniform(0.1, 0.14), rng.uniform(0.07, 0.1)), tilt(rng, 25, 8),
                    style=bank_style(seed + i), segments=(24, 12), deform=C.chiselled(12, seed + i, 0.2))


def bank_straight():
    """A shore lip on a cell side, the liquid on -Y: rounded wet stones and a crust shelf stepping into it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(1401)
    bank_run(b, rng, -0.5, 0.5, 1410)

    def out(t, s):
        return -0.12 + 0.3 * s, -0.16 + 0.17 * C.smoothstep(0.0, 1.0, np.array(s)).item()

    def inner(t, s):
        return -0.12 + 0.3 * s, -0.26

    E.sweep(b, "Shelf", E.line_path(-0.5, 0.5, 0.0, (0.0, 1.0)), out, inner, 40, 12, bank_style(1420),
            lambda t, s, d, z: (0.03 * E.periodic(-0.5 + t, 1.0, 3, 1421), 0.0))
    return b


def bank_outer_corner():
    """Shore round the liquid's convex corner (the liquid is the (-X, -Y) cell)."""
    b = C.PartBuilder()
    rng = np.random.default_rng(1501)
    for i, a in enumerate((-0.15, 0.4, 0.95, 1.5)):
        aa = -math.pi / 2 + a + math.pi / 2
        c = Vector((0.05 + math.cos(aa) * 0.04, 0.05 + math.sin(aa) * 0.04, -0.03))
        b.ellipsoid("Stone", c + Vector((math.cos(aa), math.sin(aa), 0)) * 0.03, (0.13, 0.1, 0.09), rot_z(math.degrees(aa)),
                    style=bank_style(1510 + i), segments=(24, 12), deform=C.chiselled(12, 1510 + i, 0.2))
    return b


def bank_inner_corner():
    """Shore round a dry corner poking into the liquid (the liquid is on three cells round the point)."""
    b = C.PartBuilder()
    b.ellipsoid("Stone", Vector((0.05, 0.05, -0.03)), (0.16, 0.16, 0.1), rot_z(45), style=bank_style(1610),
                segments=(24, 12), deform=C.chiselled(12, 1610, 0.2))
    b.ellipsoid("Stone", Vector((-0.06, 0.12, -0.05)), (0.1, 0.08, 0.07), rot_z(10), style=bank_style(1611),
                segments=(20, 10), deform=C.chiselled(10, 1611, 0.2))
    return b


def slope():
    """A ramp across one cell from the plain (-Y) up to raised ground (+Y), crusted, with rubble on it."""
    b = C.PartBuilder()

    def top(u, v):
        x, y = -0.5 + u, -0.5 + v
        h = PLATEAU * float(C.smoothstep(-0.45, 0.45, np.array(y)))
        bump = 0.02 * E.periodic(x, 1.0, 3, 1701 + int(v * 6)) * math.sin(math.pi * v)
        return Vector((x, y, h + bump))

    def bottom(u, v):
        return Vector((-0.5 + u, -0.5 + v, -0.12))

    b.slab("Ramp", top, bottom, 60, 60, style=E.stone(E.CRUST, 1710, crack=0.5, strata=30, period=1.0, dust=0.5))
    rng = np.random.default_rng(1720)
    for i in range(4):
        y = rng.uniform(-0.3, 0.3)
        chip(b, (rng.uniform(-0.38, 0.38), y, PLATEAU * float(C.smoothstep(-0.45, 0.45, np.array(y)))), (0.05, 0.04, 0.035),
             1721 + i, E.stone(E.BASALT, 1721 + i))
    return b


def terrace_profiles(face_jitter_seed, period):
    def out(t, s):
        # s 0..0.2 foot apron on the low side, 0.2..0.7 the face, 0.7..1 the top back into the plateau.
        if s < 0.2:
            return 0.16 - 0.1 * s / 0.2, 0.0
        if s < 0.7:
            u = (s - 0.2) / 0.5
            step = 0.04 * (math.floor(u * 3) / 3)
            lip = 0.05 * C.smoothstep(0.8, 1.0, np.array(u)).item()
            return 0.06 - 0.08 * u - step + lip, PLATEAU * u + 0.02
        u = (s - 0.7) / 0.3
        return 0.03 - 0.53 * u, PLATEAU + 0.02 - 0.02 * u

    def inner(t, s):
        if s < 0.2:
            return 0.16 - 0.1 * s / 0.2, -0.14
        if s < 0.7:
            u = (s - 0.2) / 0.5
            return -0.18, -0.14 + (PLATEAU - 0.1) * u
        u = (s - 0.7) / 0.3
        return -0.18 - 0.32 * u, PLATEAU - 0.24

    def jitter(t, s, d, z):
        if 0.2 <= s < 0.7:
            return 0.025 * E.periodic(t * period, period, 4, face_jitter_seed + int(z * 8)), 0.0
        return 0.0, 0.0
    return out, inner, jitter


def terrace_step():
    """The face of raised ground along one cell side: strata steps, a lighter overhanging lip, plateau on +Y."""
    b = C.PartBuilder()
    out, inner, jitter = terrace_profiles(1801, 1.0)
    E.sweep(b, "Step", E.line_path(-0.5, 0.5, 0.0, (0.0, -1.0)), out, inner, 60, 70,
            E.stone(E.BASALT, 1810, veins=0.35, strata=26, period=1.0, dust=0.6), jitter)
    return b


def terrace_step_outer():
    """The convex corner of raised ground (the plateau is the (+X, +Y) quadrant), rounded with radius 0.3."""
    b = C.PartBuilder()
    out, inner, jitter = terrace_profiles(1901, 1.0)
    E.sweep(b, "Step", E.corner_path(0.3, 0.5), out, inner, 70, 70,
            E.stone(E.BASALT, 1910, veins=0.35, strata=26, dust=0.6), jitter)
    return b


def ledge():
    """A lip trim along a terrace top edge: a plate sticking out over the face, Crysium splinters under it."""
    b = C.PartBuilder()
    rng = np.random.default_rng(2001)
    for i, x in enumerate((-0.27, 0.0, 0.26)):
        b.ellipsoid("Lip", Vector((x, 0.0, PLATEAU + 0.01)), (0.17, 0.14, 0.04), tilt(rng, 8, 5),
                    style=E.stone(E.BASALT, 2010 + i, veins=0.4, dust=0.6), segments=(24, 12),
                    deform=C.chiselled(12, 2010 + i, 0.18))
    for i, x in enumerate((-0.2, 0.15)):
        splinter(b, (x, -0.05, PLATEAU - 0.02), (0.1, -0.4, -1), 0.12, 2020 + i, "ice" if i else "pink")
    return b


ASSETS = {
    # name: (builder, target tris, voxel, smoothing, families, family_set)
    "RoadEdge_Straight": (road_edge_straight, 700, 0.007, 2, None, None),
    "RoadEdge_OuterCorner": (road_edge_outer_corner, 500, 0.007, 2, None, None),
    "RoadEdge_InnerCorner": (road_edge_inner_corner, 400, 0.007, 2, None, None),
    "RoadEdge_EndCap": (road_edge_end_cap, 500, 0.007, 2, None, None),
    "RoadEdge_PlazaJoin": (road_edge_plaza_join, 600, 0.007, 2, None, None),
    "PlazaEdge_Straight": (plaza_edge_straight, 300, 0.007, 2, None, None),
    "PlazaEdge_Corner": (plaza_edge_corner, 400, 0.007, 2, None, None),
    "BuildPadTile": (build_pad_tile, 200, 0.006, 1, ("ceramic", "weave", "metal"), E.SUIT_SET),
    "GatePlinth": (gate_plinth, 1500, 0.008, 2, ("ceramic", "weave", "metal"), E.SUIT_SET),
    "Cliff_Straight": (cliff_straight, 1500, 0.014, 2, None, None),
    "Cliff_InnerCorner": (cliff_inner_corner, 1500, 0.014, 2, None, None),
    "Cliff_OuterCorner": (cliff_outer_corner, 1500, 0.014, 2, None, None),
    "Chasm_Edge": (chasm_edge, 1500, 0.014, 2, None, None),
    "Bank_Straight": (bank_straight, 500, 0.008, 2, None, None),
    "Bank_OuterCorner": (bank_outer_corner, 400, 0.008, 2, None, None),
    "Bank_InnerCorner": (bank_inner_corner, 300, 0.008, 2, None, None),
    "Slope": (slope, 600, 0.012, 2, None, None),
    "TerraceStep": (terrace_step, 700, 0.01, 2, None, None),
    "TerraceStep_Outer": (terrace_step_outer, 800, 0.01, 2, None, None),
    "Ledge": (ledge, 500, 0.007, 2, None, None),
}

# Where each piece sits in the kit overview render (x, y, rotation in degrees).
OVERVIEW = {
    "RoadEdge_Straight": (-6, 2), "RoadEdge_OuterCorner": (-4.5, 2), "RoadEdge_InnerCorner": (-3, 2),
    "RoadEdge_EndCap": (-1.5, 2), "RoadEdge_PlazaJoin": (0, 2), "PlazaEdge_Straight": (1.5, 2),
    "PlazaEdge_Corner": (3, 2), "BuildPadTile": (4.5, 2), "GatePlinth": (-4.5, 0), "Bank_Straight": (-1.5, 0),
    "Bank_OuterCorner": (0, 0), "Bank_InnerCorner": (1.5, 0), "Slope": (3, 0), "TerraceStep": (4.5, 0),
    "TerraceStep_Outer": (6, 0), "Ledge": (6, 2), "Cliff_Straight": (-5, -2.8), "Cliff_InnerCorner": (-2, -2.8),
    "Cliff_OuterCorner": (1, -2.8), "Chasm_Edge": (4, -2.8),
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
        size = max(dims.x, dims.y, dims.z * 1.6)
        centre = Vector((sum(c[0] for c in asset.low.bound_box) / 8, sum(c[1] for c in asset.low.bound_box) / 8,
                         dims.z * 0.3))
        cam = C.closeup_camera(centre, size * 2.0 + 0.4, azimuth=-30, elevation=38, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 640, 480))
        bpy.data.objects.remove(cam)
    if len(paths) > 1:
        C.contact_sheet(paths, os.path.join(out, "sheet_env_kit.png"), 4, 400)
    C.hide_lights(rig, True)
    E.level_lights()
    for name, asset in built.items():
        asset.low.hide_render = False
        x, y = OVERVIEW.get(name, (0, 0))
        asset.low.location = (x, y, 0)
    cam = C.game_camera(Vector((0, 0, 0)), 15.0, yaw=16)
    C.render(cam, os.path.join(out, "env_kit_overview.png"), 1280, 720)


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    only = C.cli_args().get("only")
    built = {}
    for name, (fn, tris, voxel, smoothing, families, family_set) in ASSETS.items():
        if only and name not in only.split(","):
            continue
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, families=families or ("rock", "crystal"),
                        family_set=family_set, emission=2.2)
        E.finish(asset)
        built[name] = asset
    C.save_blend("Env_Kit" if not only else "Env_Kit_" + only.replace(",", "_"))
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
