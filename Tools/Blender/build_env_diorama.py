"""Crystal Plains environment kit (round 8): a level diorama assembled from the exported kit, to judge the whole
environment at once. Nothing here is exported; it only renders.

A 12 x 8 cell piece of a level: a two-cell road with two bends, a plaza with road-to-plaza joins, a gate on the
exit, build pads (one on a raised terrace), a rift, a small pool with banks, a nest with eggs, flora patches, rock
decor and crystals, scatter, a cliff border at the back and a chasm at the front, far silhouettes in the mist, and
a few towers and enemies from ArtSource. The ground is one plane wearing a terrain shader that blends the tiling
sets by a splat map (the way TerrainSurfaceBuilder could do it), and the kerbs are placed by the neighbourhood rules
in ArtSource/README.md (round 8), so the render also tests those rules.

Run: blender --background --factory-startup --python Tools/Blender/build_env_diorama.py -- --preview <dir>
     [--samples 64] [--nomist]
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

W, H = 12, 8
CELL = 1.0
PLATEAU = 0.55
EXP = C.EXPORTS

# --- layout (cells, x to the right, y away from the camera) ----------------------------------------------------
ROAD = {(x, y) for x in range(0, 5) for y in (5, 6)} | {(x, y) for x in (3, 4) for y in (2, 3, 4)} | {(5, 2), (5, 3)} \
    | {(x, y) for x in range(9, 12) for y in (2, 3)}
PLAZA = {(x, y) for x in (6, 7, 8) for y in (2, 3, 4)}
PADS = [(0, 4), (1, 4), (2, 4), (2, 3), (2, 2), (5, 4), (5, 5), (7, 5), (9, 4), (10, 4), (9, 1), (10, 1), (6, 7)]
RAISED = {(5, 6), (6, 6), (5, 7), (6, 7)}
RIFT = [("Rift_Cap", (5, 1), 180), ("Rift_2x1", (6.5, 1), 0), ("Rift_Cap", (8, 1), 0)]
POOL = {(9, 6), (10, 6)}
GATE_CELL = (10, 2.5)
TOWERS = [("Tower_Gun", "T2", (2, 4)), ("Tower_Cannon", "T1", (5, 4)), ("Tower_Laser", "T1", (9, 4)),
          ("Tower_Gun", "T1", (6, 7)), ("Tower_Cannon", "T2", (10, 1))]
FLORA_AREAS = [((0.5, 1.0), 1.3), ((11.0, 5.0), 1.2), ((3.0, 0.3), 1.0), ((8.2, 6.5), 0.9)]


def world(cell):
    return Vector(((cell[0] - (W - 1) / 2) * CELL, (cell[1] - (H - 1) / 2) * CELL, 0.0))


def in_road(c):
    return c in ROAD or c in PLAZA


# --- loading kit pieces --------------------------------------------------------------------------------------

_SOURCES = {}


def source(name, folder=None):
    """Imports an exported asset once (hidden) and returns its mesh object; placements link its data."""
    if name in _SOURCES:
        return _SOURCES[name]
    folder = folder or os.path.join(E.ENV, name)
    if not os.path.exists(os.path.join(folder, name + ".fbx")):
        C.log("missing asset", name)
        _SOURCES[name] = None
        return None
    objs = E.textured_import(folder, name, emission_strength=2.4)
    mesh = [o for o in objs if o.type == "MESH"][0]
    for o in objs:
        o.hide_render = True
        o.hide_viewport = True
    _SOURCES[name] = mesh
    return mesh


def place(name, location, rot_deg=0.0, scale=1.0, mirror=False, folder=None, z=0.0):
    src = source(name, folder)
    if src is None:
        return None
    obj = C.link(bpy.data.objects.new(name + "_inst", src.data))
    obj.matrix_world = src.matrix_world.copy()
    obj.location = Vector((location[0], location[1], z))
    euler = src.rotation_euler.copy()
    euler.z += math.radians(rot_deg)
    obj.rotation_euler = euler
    sx = -scale if mirror else scale
    base = src.scale
    obj.scale = (base.x * sx, base.y * scale, base.z * scale)
    obj.hide_render = False
    return obj


def angle_for(direction):
    """Rotation (degrees) that turns the piece's +Y towards `direction` (a grid step)."""
    return math.degrees(math.atan2(-direction[0], direction[1]))


# --- kerbs by the neighbourhood rules ----------------------------------------------------------------------------

SIDES = [(1, 0), (0, 1), (-1, 0), (0, -1)]


def inside(c):
    return 0 <= c[0] < W and 0 <= c[1] < H


def place_kerbs():
    gate = (int(GATE_CELL[0]), 2), (int(GATE_CELL[0]), 3)
    for c in sorted(ROAD | PLAZA):
        for d in SIDES:
            n = (c[0] + d[0], c[1] + d[1])
            if in_road(n) or not inside(n) or c in gate:
                continue
            mid = world(c) + Vector((d[0] * 0.5, d[1] * 0.5, 0))
            rot = angle_for(d)
            if c in PLAZA:
                place("PlazaEdge_Straight", mid, rot)
                continue
            # Road edge whose line continues into a plaza edge: the join piece, +X towards the plaza.
            joined = False
            for t in ((-d[1], d[0]), (d[1], -d[0])):
                along = (c[0] + t[0], c[1] + t[1])
                beyond = (along[0] + d[0], along[1] + d[1])
                if along in PLAZA and not in_road(beyond):
                    plus_x = (math.cos(math.radians(rot)), math.sin(math.radians(rot)))
                    mirror = (plus_x[0] * t[0] + plus_x[1] * t[1]) < 0
                    place("RoadEdge_PlazaJoin", mid, rot, mirror=mirror)
                    joined = True
            if not joined:
                place("RoadEdge_Straight", mid, rot)
    for gx in range(1, W):
        for gy in range(1, H):
            quads = {(sx, sy): (gx + (sx - 1) // 2, gy + (sy - 1) // 2) for sx in (-1, 1) for sy in (-1, 1)}
            road = [q for q, cell in quads.items() if in_road(cell)]
            if len(road) not in (1, 3):
                continue
            point = world((gx - 0.5, gy - 0.5))
            plaza = any(cell in PLAZA for cell in quads.values())
            if len(road) == 1:
                target = road[0]
                name = "PlazaEdge_Corner" if plaza else "RoadEdge_OuterCorner"
            else:
                g = [q for q in quads if q not in road][0]
                target = (-g[0], -g[1])
                name = "PlazaEdge_Corner" if plaza else "RoadEdge_InnerCorner"
            # The piece's road quadrant (-1, -1) turned onto the target quadrant.
            rot = math.degrees(math.atan2(target[1], target[0]) - math.atan2(-1, -1))
            place(name, point, rot)


# --- terrain: one plane, tiling sets blended by a splat map --------------------------------------------------

X0, X1, Y0, Y1 = -9.0, 9.0, -4.0, 11.0
SPLAT_PX = 32


def splat_image(path):
    w = int((X1 - X0) * SPLAT_PX)
    h = int((Y1 - Y0) * SPLAT_PX)
    xs = X0 + (np.arange(w) + 0.5) / SPLAT_PX
    ys = Y0 + (np.arange(h) + 0.5) / SPLAT_PX
    X, Y = np.meshgrid(xs, ys)
    cx = np.floor(X / CELL + (W - 1) / 2 + 0.5).astype(int)
    cy = np.floor(Y / CELL + (H - 1) / 2 + 0.5).astype(int)
    road = np.zeros((h, w))
    plaza = np.zeros((h, w))
    for (x, y) in ROAD:
        road[(cx == x) & (cy == y)] = 1
    for (x, y) in PLAZA:
        plaza[(cx == x) & (cy == y)] = 1
    # Roads leave the field on its open sides.
    for (x, y) in ROAD:
        if x == 0:
            road[(cx < 0) & (cy == y)] = 1
        if x == W - 1:
            road[(cx > W - 1) & (cy == y)] = 1
    flora = np.zeros((h, w))
    for (fx, fy), r in FLORA_AREAS:
        c = world((fx, fy))
        d = np.hypot(X - c.x, Y - c.y)
        wob = 0.25 * np.sin(X * 3.1 + Y * 1.7) + 0.2 * np.sin(X * 1.3 - Y * 2.9)
        flora = np.maximum(flora, 1 - C.smoothstep(r * 0.6, r, d + wob))
    flora *= (1 - road) * (1 - plaza)
    img = np.dstack([road, plaza, flora, np.ones_like(road)]).astype(np.float32)
    C.array_to_image(img, path, colorspace="Non-Color")
    return path


def _tex(nt, path, vec, non_color=False):
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = bpy.data.images.load(path, check_existing=True)
    if non_color:
        t.image.colorspace_settings.name = "Non-Color"
        t.image.alpha_mode = "CHANNEL_PACKED"
    nt.links.new(vec, t.inputs["Vector"])
    return t


def _math(nt, op, a, b=None, clamp=False):
    m = nt.nodes.new("ShaderNodeMath")
    m.operation = op
    m.use_clamp = clamp
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            m.inputs[i].default_value = v
        else:
            nt.links.new(v, m.inputs[i])
    return m.outputs[0]


def _mix(nt, a, b, f, kind="RGBA"):
    m = nt.nodes.new("ShaderNodeMix")
    m.data_type = kind
    m.clamp_factor = True
    nt.links.new(f, m.inputs["Factor"])
    if kind == "RGBA":
        ia, ib, out = 6, 7, 2
    else:
        ia, ib, out = 2, 3, 0
    for idx, v in ((ia, a), (ib, b)):
        if isinstance(v, (int, float)):
            m.inputs[idx].default_value = v
        elif isinstance(v, tuple):
            m.inputs[idx].default_value = v
        else:
            nt.links.new(v, m.inputs[idx])
    return m.outputs[out]


def terrain_layer(nt, name, obj_vec):
    """Sockets of one tiling set sampled in world space (UV = world / tile size)."""
    paths = E.set_paths(name)
    mp = nt.nodes.new("ShaderNodeMapping")
    k = 1.0 / E.TILE_WORLD[name]
    mp.inputs["Scale"].default_value = (k, k, k)
    nt.links.new(obj_vec, mp.inputs["Vector"])
    vec = mp.outputs["Vector"]
    alb = _tex(nt, paths["albedo"], vec)
    mask = _tex(nt, paths["mask"], vec, True)
    nrm = _tex(nt, paths["normal"], vec, True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], sep.inputs[0])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(alb.outputs["Color"], mul.inputs[6])
    nt.links.new(sep.outputs[0], mul.inputs[7])
    out = {"col": mul.outputs[2], "smooth": sep.outputs[1], "height": sep.outputs[2], "nrm": nrm.outputs["Color"]}
    out["emit"] = _tex(nt, paths["emission"], vec).outputs["Color"] if "emission" in paths else None
    return out


def height_weight(nt, splat, layer_h, base_h, sharp=5.0):
    """Height blend: the layer wins where the splat says so, earlier where its height stands above the base
    (grit fills the joints of the road before it covers the slabs)."""
    diff = _math(nt, "SUBTRACT", layer_h, base_h)
    v = _math(nt, "ADD", splat, _math(nt, "MULTIPLY", diff, 0.35))
    v = _math(nt, "SUBTRACT", v, 0.5)
    v = _math(nt, "MULTIPLY", v, sharp)
    return _math(nt, "ADD", v, 0.5, clamp=True)


def terrain_material(splat_path):
    mat = C.new_material("Terrain")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    obj = geo.outputs["Position"]  # world space: the splat and the tiling line up with the cells
    smp = nt.nodes.new("ShaderNodeMapping")
    smp.inputs["Location"].default_value = (-X0 / (X1 - X0), -Y0 / (Y1 - Y0), 0)
    smp.inputs["Scale"].default_value = (1 / (X1 - X0), 1 / (Y1 - Y0), 1)
    nt.links.new(obj, smp.inputs["Vector"])
    splat = _tex(nt, splat_path, smp.outputs["Vector"], True)
    sp = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(splat.outputs["Color"], sp.inputs[0])
    g = terrain_layer(nt, "Ground", obj)
    f = terrain_layer(nt, "GroundFlora", obj)
    r = terrain_layer(nt, "Road", obj)
    p = terrain_layer(nt, "Plaza", obj)
    acc = dict(g)
    for layer, ch, sharp in ((f, 2, 4.0), (r, 0, 7.0), (p, 1, 7.0)):
        w = height_weight(nt, sp.outputs[ch], layer["height"], acc["height"], sharp)
        acc["col"] = _mix(nt, acc["col"], layer["col"], w)
        acc["nrm"] = _mix(nt, acc["nrm"], layer["nrm"], w)
        acc["smooth"] = _mix(nt, acc["smooth"], layer["smooth"], w, "FLOAT")
        acc["height"] = _mix(nt, acc["height"], layer["height"], w, "FLOAT")
        le = layer["emit"] if layer["emit"] is not None else (0.0, 0.0, 0.0, 1.0)
        acc["emit"] = _mix(nt, acc["emit"], le, w)
    nt.links.new(acc["col"], bsdf.inputs["Base Color"])
    nt.links.new(_math(nt, "SUBTRACT", 1.0, acc["smooth"]), bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(acc["nrm"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    nt.links.new(acc["emit"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.6
    return mat


RIFT_CELLS = {(5, 1), (6, 1), (7, 1), (8, 1)}


def cut_cells(obj, cells):
    """Deletes the terrain faces over cells that bring their own ground (the rift pieces carve their fissure; in
    the game TerrainField sinks rift cells the same way)."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    kill = []
    for f in bm.faces:
        c = obj.matrix_world @ f.calc_center_median()
        cell = (int(math.floor(c.x / CELL + (W - 1) / 2 + 0.5)), int(math.floor(c.y / CELL + (H - 1) / 2 + 0.5)))
        if cell in cells:
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(obj.data)
    bm.free()


def grid_terrain(name, mat, skip):
    """The ground as one quad per unit square (cells line up with the field), leaving out `skip` cells."""
    import bmesh
    bm = bmesh.new()
    verts = {}

    def v(x, y):
        if (x, y) not in verts:
            verts[(x, y)] = bm.verts.new((x, y, 0.0))
        return verts[(x, y)]

    for ix in range(int(X0), int(X1)):
        for iy in range(int(Y0), int(Y1)):
            cell = (int(math.floor(ix + 0.5 + (W - 1) / 2 + 0.5)), int(math.floor(iy + 0.5 + (H - 1) / 2 + 0.5)))
            if cell in skip:
                continue
            bm.faces.new((v(ix, iy), v(ix + 1, iy), v(ix + 1, iy + 1), v(ix, iy + 1)))
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(mat)
    return C.link(bpy.data.objects.new(name, mesh))


def build_terrain(out):
    splat = splat_image(os.path.join(out, "diorama_splat.png"))
    mat = terrain_material(splat)
    grid_terrain("Terrain", mat, RIFT_CELLS | RAISED)
    # Raised ground: a block of plateau surface wearing the same terrain material (the faces come from the kit).
    xs = [world(c).x for c in RAISED]
    ys = [world(c).y for c in RAISED]
    E.plane("Plateau", max(xs) - min(xs) + 1.0, max(ys) - min(ys) + 1.0, mat,
            location=((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, PLATEAU))
    # The void below the chasm: a faint violet haze far down.
    haze = E.emissive("VoidHaze", (0.35, 0.22, 0.8), 0.6)
    E.plane("Void", 30, 8, haze, location=(0, -8.5, -3.2))
    deep = E.emissive("VoidDeep", (0.01, 0.008, 0.02), 1.0)
    E.plane("VoidFloor", 40, 12, deep, location=(0, -9, -3.25))


# --- dressing ----------------------------------------------------------------------------------------------------

def dress(rng):
    place_kerbs()
    for c in PADS:
        z = PLATEAU if c in RAISED else 0.0
        place("BuildPadTile", world(c), 0, z=z)
    gx = world(GATE_CELL)
    place("GatePlinth", gx, 90)
    for name, cell, rot in RIFT:
        place(name, world(cell), rot)
    # Map border: cliffs at the back, a chasm at the front.
    for x in range(0, W, 2):
        place("Cliff_Straight", world((x + 0.5, H - 0.5)) + Vector((0, 0.05, 0)), 0)
        place("Chasm_Edge", world((x + 0.5, -0.5)), 180)
    place("Cliff_OuterCorner", world((-0.5, H - 0.5)) + Vector((-0.6, 0.0, 0)), 0)
    # Raised terrace (cells x 5..6, y 6..7): steps on its open sides, a slope down to the east, a ledge.
    for c, d in (((5, 6), (0, -1)), ((6, 6), (0, -1)), ((5, 6), (-1, 0)), ((5, 7), (-1, 0)), ((6, 7), (1, 0))):
        place("TerraceStep", world(c) + Vector((d[0] * 0.5, d[1] * 0.5, 0)), angle_for((-d[0], -d[1])))
    place("TerraceStep_Outer", world((4.5, 5.5)) + Vector((0, 0, 0)), 0)
    place("Slope", world((7, 6)), angle_for((-1, 0)))
    place("Ledge", world((5.5, 5.5)) + Vector((0.5, 0.0, 0)), angle_for((0, 1)))
    # Pool with banks.
    pool_mat = C.principled("Pool", (0.02, 0.06, 0.1), 0.05, emission=(0.1, 0.45, 0.7), strength=0.6)
    xs = [world(c).x for c in POOL]
    E.plane("PoolSurface", max(xs) - min(xs) + 0.9, 0.9, pool_mat, location=((max(xs) + min(xs)) / 2, world((9, 6)).y, 0.006))
    for c in POOL:
        for d in SIDES:
            n = (c[0] + d[0], c[1] + d[1])
            if n in POOL:
                continue
            place("Bank_Straight", world(c) + Vector((d[0] * 0.5, d[1] * 0.5, 0)), angle_for(d))
    for (gx_, gy_), q in (((9, 6), (1, 1)), ((11, 7), (-1, -1)), ((9, 7), (1, -1)), ((11, 6), (-1, 1))):
        point = world((gx_ - 0.5, gy_ - 0.5))
        rot = math.degrees(math.atan2(q[1], q[0]) - math.atan2(-1, -1))
        place("Bank_OuterCorner", point, rot)
    # Nest with eggs (warm), shells round it.
    nest = world((11, 7))
    place("Burrow_A", nest + Vector((0.1, 0.2, 0)), 30, scale=0.8, folder=os.path.join(EXP, "Burrow_A"))
    place("Egg_Cluster", nest + Vector((-0.9, -0.2, 0)), 20)
    place("Egg_Cluster", nest + Vector((-0.6, -0.8, 0)), 140, scale=0.7)
    for i in range(4):
        place("Scatter_ShellFragments", nest + Vector((rng.uniform(-1.6, 0.4), rng.uniform(-1.4, -0.4), 0)), rng.uniform(0, 360))
    # Rock decor, crystals and flora on blocked cells and round the field.
    props = [("Decor_Boulder", (0, 0.2), 0, 1.0), ("Crystal_A", (1.2, 1.4), 30, 1.0), ("Crystal_B", (4.0, 0.8), 0, 0.9),
             ("Decor_Column", (11.2, 0.6), 0, 1.0), ("Decor_Spike", (7.2, 0.1), 0, 0.9), ("Rock_B", (5.3, 0.2), 20, 0.8),
             ("Crystal_C", (8.2, 7.4), 10, 0.9), ("Decor_Spire", (3.4, 7.6), 0, 0.9), ("Rock_A", (1.6, 2.7), 40, 0.7),
             ("Rock_C", (11.4, 4.6), 0, 0.8), ("Decor_Boulder", (7.6, 6.6), 120, 0.8), ("Crystal_A", (2.4, 7.3), 200, 0.8),
             ("Decor_Spike", (0.3, 7.2), 30, 0.8), ("Crystal_B", (11.3, 1.6), 0, 0.8)]
    for name, cell, rot, scale in props:
        folder = os.path.join(EXP, name) if name.startswith(("Crystal_", "Rock_")) else None
        place(name, world(cell), rot, scale=scale, folder=folder)
    flora = [("Flora_GlowTree", (0.4, 1.1), 0), ("Flora_Frond", (1.1, 0.6), 40), ("Flora_Mushroom", (0.0, 1.6), 0),
             ("Flora_Tendril", (11.2, 5.4), 0), ("Flora_Frond", (10.8, 4.9), 100), ("Flora_GlowTree", (11.6, 5.6), 70),
             ("Flora_Mushroom", (3.0, 0.4), 20), ("Flora_Tendril", (2.6, 0.1), 0), ("Flora_Frond", (8.4, 6.3), 0)]
    for name, cell, rot in flora:
        place(name, world(cell), rot)
    place("Scree_Patch", world((4.6, 1.0)), 200)
    place("Scree_Patch", world((9.6, 7.3)), 0, scale=0.8)
    place("CrystalVein", world((1.0, 3.0)), 70)
    for i in range(26):
        x, y = rng.uniform(-0.4, W - 0.6), rng.uniform(-0.3, H - 0.6)
        c = (int(round(x)), int(round(y)))
        if in_road(c) or c in PADS or c in RAISED or c in POOL:
            continue
        kind = ["Scatter_Pebbles", "Scatter_Pebbles", "Scatter_CrystalSplinters"][i % 3]
        place(kind, world((x, y)), rng.uniform(0, 360))
    # Far silhouettes in the mist.
    place("Silhouette_Ridge", (0.0, 13.5), 0)
    place("Silhouette_CrystalTower", (-6.5, 9.0), 0, scale=0.9)
    place("Silhouette_Mesa", (7.5, 10.5), 30, scale=0.9)
    place("Silhouette_CrystalTower", (4.0, 16.0), 50, scale=1.2)


def holder(objs, location, rot_deg, scale):
    """Imported rigs key their own object transform, so they are placed through a parent empty."""
    empty = C.link(bpy.data.objects.new("Holder", None))
    empty.location = location
    empty.rotation_euler.z = math.radians(rot_deg)
    empty.scale = (scale, scale, scale)
    for o in objs:
        if o.parent is None:
            o.parent = empty
    return empty


def actors():
    """Towers on some pads and Burrowers on the road (from ArtSource, if present)."""
    for tower, tier, cell in TOWERS:
        name = f"{tower}_{tier}"
        folder = os.path.join(EXP, tower)
        fbx = os.path.join(folder, name + ".fbx")
        if not os.path.exists(fbx):
            continue
        objs = E.import_fbx(fbx)
        tex = {k.lower(): os.path.join(folder, f"{tower}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        mat = C.game_material(name + "_Mat", tex, 2.2, 0.4, ())
        z = PLATEAU if cell in RAISED else 0.0
        for o in objs:
            if o.type == "MESH":
                o.data.materials.clear()
                o.data.materials.append(mat)
        holder(objs, world(cell) + Vector((0, 0, z + 0.045)), 0.0, 1.0)
    enemies = [("Skitter", 0.45, [(1.0, 5.3, -90), (1.6, 5.8, -95), (2.3, 5.4, -80), (3.4, 4.2, 180), (4.0, 3.6, 175)]),
               ("Cragback", 0.85, [(5.4, 2.7, -90)])]
    for name, scale, spots in enemies:
        fbx = os.path.join(EXP, name, name + ".fbx")
        if not os.path.exists(fbx):
            continue
        tex = {k.lower(): os.path.join(EXP, name, f"{name}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        mat = C.game_material(name + "_Mat", tex, 2.2, 0.4, ())
        for x, y, rot in spots:
            objs = E.import_fbx(fbx)
            for o in objs:
                if o.type == "MESH":
                    o.data.materials.clear()
                    o.data.materials.append(mat)
            holder(objs, world((x, y)), rot, scale)


def ground_mist(density):
    """A low layer of violet mist hugging the ground and filling the chasm (like GroundMistBuilder's sheets)."""
    import bmesh
    mesh = bpy.data.meshes.new("Mist")
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bm.to_mesh(mesh)
    bm.free()
    obj = C.link(bpy.data.objects.new("Mist", mesh))
    obj.location = ((X0 + X1) / 2, (Y0 + Y1) / 2 - 3.0, -1.6)
    obj.scale = (X1 - X0 + 6, Y1 - Y0 + 10, 4.2)
    mat = C.new_material("MistVolume")
    nt = mat.node_tree
    nt.nodes.remove(nt.nodes.get("Principled BSDF"))
    vol = nt.nodes.new("ShaderNodeVolumePrincipled")
    vol.inputs["Density"].default_value = density
    vol.inputs["Color"].default_value = (0.34, 0.3, 0.6, 1)
    vol.inputs["Anisotropy"].default_value = 0.2
    nt.links.new(vol.outputs[0], nt.nodes.get("Material Output").inputs["Volume"])
    mesh.materials.append(mat)
    return obj


def lights():
    E.level_lights(key=2.0, rim=1.2)
    for cell, col, energy in (((6.5, 1), (1.0, 0.55, 0.22), 60), ((11, 7), (1.0, 0.45, 0.25), 40),
                              ((1.2, 1.4), (1.0, 0.5, 0.85), 18), ((4.0, 0.8), (1.0, 0.5, 0.85), 14),
                              (GATE_CELL, (0.5, 0.75, 1.0), 25), ((0.4, 1.1), (0.7, 1.0, 0.4), 12),
                              ((11.2, 5.4), (1.0, 0.45, 0.8), 12), ((9.5, 6), (0.3, 0.7, 1.0), 10)):
        E.point_light("Glow", world(cell) + Vector((0, 0, 0.7)), col, energy, 0.3)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    samples = int(args.get("samples", 64))
    rng = np.random.default_rng(9)
    E.level_world((0.01, 0.01, 0.025), 1.0)
    build_terrain(out)
    if not args.get("nomist"):
        ground_mist(float(args.get("mist", 0.45)))
    dress(rng)
    actors()
    lights()
    cam = C.game_camera(Vector((0, 0.5, 0)), 14.5, yaw=16)
    C.render(cam, os.path.join(out, "env_diorama_game.png"), 1600, 900, samples=samples)
    cam2 = C.closeup_camera(world((6.3, 2.2)) + Vector((0, 0, 0.3)), 7.5, azimuth=-38, elevation=26, lens=30)
    C.render(cam2, os.path.join(out, "env_diorama_close.png"), 1600, 900, samples=samples)
    cam3 = C.game_camera(world((6.0, 2.5)), 6.0, yaw=16)
    C.render(cam3, os.path.join(out, "env_diorama_zoomed.png"), 1600, 900, samples=samples)
    C.save_blend("Env_Diorama")


main()
