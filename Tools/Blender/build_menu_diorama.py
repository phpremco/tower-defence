"""Round 26: the main-menu diorama as designed art (replaces the code-built island of UI/MenuTerrain.cs and the
procedural props of UI/MenuDiorama.cs).

A floating chunk of S-69-CRYS ground: a plateau with the Burrower hive mound and its hooded burrow mouth, five pools
(lava, pond, quicksand pit, frozen lake, bog) in the same places and sizes as MenuDiorama, a small NTI outpost on a
rise at the back, and under it all a hanging rock mass with strata ledges, Crysium veins and three dripping spires.

Exports (ArtSource/Exports/):
  Menu_Island/      Menu_Island.fbx (two materials: Top, Cliff), Top maps 2048 (planar UV, same convention as
                    MenuTerrain.Uv but extent 13), Cliff maps 4096 x 512 (u = angle round the island, v = depth),
                    Menu_Island_Height.png (16-bit ground height of the game mesh, for MenuTerrain.OnGround),
                    Menu_Island_layout.json (every placed asset in Unity world space, pool outlines, lights, camera),
                    Menu_Island_report.json, Low/ (half-size maps)
  Menu_NestMouth/   the hooded burrow mouth over the porch (replaces MenuDiorama.BuildMouth's quad and torus)

The island is built in Unity world coordinates (pivot at the world origin, as MenuDiorama builds it), so Unity
places Menu_Island at (0, 0, 0) and every layout entry at its own position.

Run: blender --background --factory-startup --python Tools/Blender/build_menu_diorama.py -- [--preview <dir>]
     [--norender] [--skipmouth] [--quick]
"""

import json
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import common as C  # noqa: E402
import env_kit as E  # noqa: E402
import stage_kit as S  # noqa: E402
from stage_kit import ss, fbm2, ridged2, fbm3, mix  # noqa: E402

F32 = np.float32
ARGS = C.cli_args()
QUICK = bool(ARGS.get("quick"))
OUT = os.path.join(C.EXPORTS, "Menu_Island")
OUT_MOUTH = os.path.join(C.EXPORTS, "Menu_NestMouth")
PREVIEW = ARGS.get("preview") or os.path.join(C.REPO, "ArtSource", "Previews", "Menu")
os.makedirs(PREVIEW, exist_ok=True)
T0 = time.time()


def log(*a):
    C.log(f"[menu {time.time() - T0:6.1f}s]", *a)


# ---------------------------------------------------------------------------------------------------------------
# The stage, in Unity world units (x right, z away from the camera), copied from MenuDiorama.cs
# ---------------------------------------------------------------------------------------------------------------

IC = np.array((3.6, 2.2), F32)
R0 = 11.5
NEST = np.array((4.8, 5.2), F32)
NEST_R = 4.2
NEST_H = 2.6
MOUTH = np.array((4.8, 2.4), F32)       # NestCentre + back * (NestRadius - MouthInset)
PORCH = 0.3
POOLS = [
    dict(kind="Lava", c=(8.6, -0.9), r=2.1, theme="LavaTheme", bank=260.0, surface=-0.22),
    dict(kind="Water", c=(1.4, -2.2), r=2.0, theme="ForestTheme", bank=20.0, surface=-0.2),
    dict(kind="Quicksand", c=(-1.8, 6.0), r=1.7, theme="RuinsTheme", bank=300.0, surface=-0.12),
    dict(kind="Ice", c=(-3.4, 0.6), r=1.7, theme="IceTheme", bank=200.0, surface=-0.06),
    dict(kind="Bog", c=(10.2, 5.8), r=1.8, theme="SwampTheme", bank=120.0, surface=-0.14),
]
OUTPOST = np.array((0.5, 9.4), F32)
OUTPOST_H = 0.42
WALKER_LAP = 3.9
BANK_MARGIN = 0.9
BANK_ARC = 150.0
POOL_DEPTH = 0.8

CAMERA_FOCUS = (3.1, 0.6, 1.8)
CAMERA_DISTANCE = 15.5
CAMERA_PITCH = 29.0
CAMERA_YAW = 6.0
CAMERA_FOV = 36.0
SUN_EULER = (46.0, -38.0)
SUN_COLOR = (0.72, 0.68, 0.9)
FOG = (0.05, 0.025, 0.055)

EXTENT = 13.0          # Top UV: u = (x - 3.6 + 13) / 26, v = (z - 2.2 + 13) / 26
CLIFF_DEPTH = 11.0     # Cliff UV: v = 1 - (rim height - y) / 11
TOP_RES = 1024 if QUICK else 2048
CLIFF_RES = (256, 2048) if QUICK else (512, 4096)
N_ANGLE = 360 if QUICK else 720
N_TOP = 120 if QUICK else 240
N_UNDER = 70 if QUICK else 140
TARGET_TRIS = 13800


# ---------------------------------------------------------------------------------------------------------------
# Ground shape
# ---------------------------------------------------------------------------------------------------------------

def rim_radius(a):
    """Island edge radius by angle (radians, counter-clockwise from +x in the xz plane, like MenuDiorama.Lap)."""
    a = np.asarray(a, F32)
    cx, cz = np.cos(a), np.sin(a)
    n1 = fbm2(cx * 1.3, cz * 1.3, 1.0, 3, 41)
    n2 = fbm2(cx * 4.0, cz * 4.0, 1.0, 2, 42)
    bites = ss(0.35, 0.75, fbm2(cx * 2.4, cz * 2.4, 1.0, 1, 43))
    return np.maximum(11.05, R0 + 0.3 + 0.65 * n1 + 0.22 * n2 - 0.55 * bites).astype(F32)


def pool_distance(x, z, pool):
    cx, cz = pool["c"]
    d = np.hypot(x - cx, z - cz)
    wob = fbm2(x * 0.7 + cx * 3.0, z * 0.7 + cz * 3.0, 1.0, 2, 9)
    return d + wob * pool["r"] * 0.42


def basin(x, z, pool):
    return 1 - ss(pool["r"] * 0.5, pool["r"], pool_distance(x, z, pool))


def mound(x, z):
    dn = np.hypot(x - NEST[0], z - NEST[1])
    rho = np.clip(dn / NEST_R, 0, 1)
    # A dome, not a cone: full and round over the top, steepening into the ground at its foot.
    m = np.power(1 - rho * rho, 1.25)
    lump = 1 + 0.16 * fbm2(x, z, 0.9, 2, 5)
    ang = np.arctan2(z - NEST[1], x - NEST[0])
    rib = 0.5 + 0.5 * np.sin(ang * 11 + 1.8 * fbm2(x, z, 0.45, 2, 6))
    ribs = 0.11 * (4 * m * (1 - m)) * rib ** 3
    return m * NEST_H * lump + ribs, m


def porch_weight(x, z):
    disc = 1 - ss(1.45, 2.45, np.hypot(x - MOUTH[0], z - MOUTH[1]))
    # The notch runs into the mound under the hood, so the crawlers walk into the dark at porch level.
    # It reaches under the whole hood (Menu_NestMouth spans z 3.55..5.4), so the throat is not walled off.
    zq = np.clip((z - 1.2) / (5.3 - 1.2), 0, 1)
    half = 1.95 + (1.05 - 1.95) * zq
    notch = (1 - ss(half - 0.2, half + 0.3, np.abs(x - MOUTH[0]))) * (1 - ss(5.25, 5.6, z)) * ss(0.4, 1.1, z)
    return np.maximum(disc, notch)


def height(x, z):
    """Ground height of the top surface (Unity y)."""
    x = np.asarray(x, F32)
    z = np.asarray(z, F32)
    h = 0.07 * fbm2(x, z, 0.45, 3, 1) + 0.09 * fbm2(x, z, 0.13, 2, 2)
    d = np.hypot(x - IC[0], z - IC[1])
    R = rim_radius(np.arctan2(z - IC[1], x - IC[0]))
    lip = ss(R - 1.25, R + 0.02, d)
    h = h - 0.62 * lip ** 1.7
    for pool in POOLS:
        pd = pool_distance(x, z, pool)
        r = pool["r"]
        h = h - (1 - ss(r * 0.5, r, pd)) * POOL_DEPTH
        h = h + 0.05 * ss(r * 0.95, r * 1.15, pd) * (1 - ss(r * 1.15, r * 1.75, pd))
    rise = 1 - ss(1.35, 2.7, np.hypot(x - OUTPOST[0], z - OUTPOST[1]))
    h = h + (OUTPOST_H + 0.015 * fbm2(x, z, 2.0, 1, 7) - h) * rise
    mh, _ = mound(x, z)
    h = h + mh
    w = porch_weight(x, z)
    h = h + (PORCH + 0.012 * fbm2(x, z, 2.5, 1, 8) - h) * w
    return h.astype(F32)


def rim_height(a):
    R = rim_radius(a)
    return height(IC[0] + np.cos(a) * R, IC[1] + np.sin(a) * R)


SPIRES = [((0.8, 3.0), 3.2, 1.6), ((7.2, 1.0), 2.4, 1.3), ((4.6, 6.8), 2.0, 1.4)]


def under_depth(x, z, rho):
    """How far the underside hangs below the rim line at this point (rho = radius / rim radius)."""
    body = 6.4 * np.power(np.clip(1 - rho * rho, 0, 1), 0.55)
    lobes = 0.78 + 0.32 * fbm2(x, z, 0.16, 2, 21) + 0.12 * fbm2(x, z, 0.5, 2, 22)
    depth = body * lobes
    for (sx, sz), extra, width in SPIRES:
        depth = depth + extra * np.exp(-((x - sx) ** 2 + (z - sz) ** 2) / (2 * width * width)) * ss(0.1, 0.6, 1 - rho)
    return depth


# ---------------------------------------------------------------------------------------------------------------
# Meshes
# ---------------------------------------------------------------------------------------------------------------

def make_mesh(name, verts, faces, mats, params):
    mesh = bpy.data.meshes.new(name)
    mesh.vertices.add(len(verts))
    mesh.vertices.foreach_set("co", verts.astype(F32).ravel())
    sizes = np.array([len(f) for f in faces], np.int32)
    loops = np.concatenate(faces).astype(np.int32)
    mesh.loops.add(len(loops))
    mesh.loops.foreach_set("vertex_index", loops)
    mesh.polygons.add(len(faces))
    starts = np.concatenate([[0], np.cumsum(sizes)[:-1]]).astype(np.int32)
    mesh.polygons.foreach_set("loop_start", starts)
    mesh.polygons.foreach_set("material_index", np.asarray(mats, np.int32))
    uv = mesh.uv_layers.new(name="Param")
    uv.data.foreach_set("uv", params[loops].astype(F32).ravel())
    mesh.update(calc_edges=True)
    mesh.validate()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def build_high():
    """The dense source surface: polar top rings + the hanging underside, one closed shell (Blender coordinates)."""
    A = np.linspace(-math.pi, math.pi, N_ANGLE, endpoint=False).astype(F32)
    R = rim_radius(A)
    y_rim = rim_height(A)
    mean_rim = float(y_rim.mean())
    rho_top = (np.arange(1, N_TOP + 1) / N_TOP).astype(F32)
    rr = rho_top[:, None] * R[None, :]
    tx = IC[0] + np.cos(A)[None, :] * rr
    tz = IC[1] + np.sin(A)[None, :] * rr
    ty = height(tx, tz)
    # The outermost top ring is the rim: pin it to rim_height so the underside meets it exactly.
    ty[-1] = y_rim
    j = np.arange(1, N_UNDER, dtype=F32)
    rho_u = np.cos(j / N_UNDER * math.pi / 2).astype(F32)
    ur = rho_u[:, None] * R[None, :]
    ux = IC[0] + np.cos(A)[None, :] * ur
    uz = IC[1] + np.sin(A)[None, :] * ur
    base = y_rim[None, :] + (mean_rim - y_rim[None, :]) * (1 - rho_u[:, None])
    depth = under_depth(ux, uz, rho_u[:, None])
    uy = base - depth
    # Strata: soft terraces in height, sharper near the rim where the wall is steep.
    terr = 0.85
    q = uy / terr
    uy = uy + (np.round(q) - q) * terr * 0.35 * ss(0.0, 0.5, depth)
    # Rock relief: pushed out sideways where the wall is steep, so ledges and buttresses overhang a little.
    n1 = fbm3(ux * 0.35, uy * 0.35, uz * 0.35, 1.0, 4, 31)
    n2 = ridged2(np.arctan2(uz - IC[1], ux - IC[0]) * 6.0, uy * 0.8, 1.0, 3, 32)
    steep = ss(0.55, 0.97, rho_u)[:, None]
    push = (0.55 * n1 + 0.22 * n2) * ss(0.0, 0.4, depth) * (0.4 + 0.6 * steep)
    ux = ux + np.cos(A)[None, :] * push
    uz = uz + np.sin(A)[None, :] * push
    uy = uy + 0.25 * fbm3(ux * 0.6, uy * 0.6, uz * 0.6, 1.0, 3, 33) * (1 - steep)
    pole = np.array([[IC[0] + 0.4, mean_rim - float(under_depth(np.array(IC[0] + 0.4), np.array(IC[1]), np.array(0.0))) - 0.5, IC[1]]], F32)

    centre = np.array([[IC[0], float(height(np.array(IC[0]), np.array(IC[1]))), IC[1]]], F32)
    top = np.stack([tx, ty, tz], -1).reshape(-1, 3)
    und = np.stack([ux, uy, uz], -1).reshape(-1, 3)
    U = np.concatenate([centre, top, und, pole])
    verts = np.stack([-U[:, 0], -U[:, 2], U[:, 1]], -1)

    N = N_ANGLE

    def ring(k):
        """Vertex indices of ring k: 0..N_TOP-1 top rings, N_TOP.. underside rings."""
        return 1 + k * N + np.arange(N)
    faces, mats = [], []
    r0 = ring(0)
    for i in range(N):
        faces.append(np.array((0, r0[i], r0[(i + 1) % N])))
        mats.append(0)
    rings = N_TOP + N_UNDER - 1
    for k in range(rings - 1):
        a, b = ring(k), ring(k + 1)
        nxt = np.roll(np.arange(N), -1)
        quads = np.stack([a, a[nxt], b[nxt], b], -1)
        mat = 0 if k < N_TOP - 1 else 1
        for qd in quads:
            faces.append(qd)
            mats.append(mat)
    last = ring(rings - 1)
    pi = len(U) - 1
    for i in range(N):
        faces.append(np.array((last[i], last[(i + 1) % N], pi)))
        mats.append(1)
    # The shell is closed, so make_mesh points every face outwards.
    # Param UV: u < 1 on the top, u >= 1 on the underside (classification after decimation).
    params = np.zeros((len(U), 2), F32)
    params[1:1 + N_TOP * N, 0] = np.repeat(rho_top, N) * 0.98
    params[1 + N_TOP * N:, 0] = 1.5
    params[-1, 0] = 1.5
    mesh = make_mesh("Menu_Island_High", verts, faces, mats, params)
    obj = C.link(bpy.data.objects.new("Menu_Island_High", mesh))
    for p in mesh.polygons:
        p.use_smooth = True
    log("high mesh", len(mesh.vertices), "verts", len(mesh.polygons), "faces")
    return obj, (A, R, y_rim)


def build_low(high):
    low = C.link(bpy.data.objects.new("Menu_Island", high.data.copy()))
    low.data.name = "Menu_Island"
    vg = low.vertex_groups.new(name="Weight")
    params = np.zeros(len(low.data.loops) * 2, F32)
    low.data.uv_layers["Param"].data.foreach_get("uv", params)
    loops_v = np.zeros(len(low.data.loops), np.int32)
    low.data.loops.foreach_get("vertex_index", loops_v)
    top = np.zeros(len(low.data.vertices), bool)
    top[loops_v[params.reshape(-1, 2)[:, 0] < 1.0]] = True
    # Decimate weights scale how freely a vertex may collapse: the walkable top keeps about three quarters of the
    # budget (creatures stand on it, it fills the screen), the underside takes the cuts.
    vg.add(np.where(top)[0].tolist(), 0.25, "REPLACE")
    vg.add(np.where(~top)[0].tolist(), 1.0, "REPLACE")
    total = sum(len(p.vertices) - 2 for p in low.data.polygons)
    mod = low.modifiers.new("Decimate", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = TARGET_TRIS / total
    mod.use_collapse_triangulate = True
    mod.vertex_group = "Weight"
    mod.vertex_group_factor = 1.0
    C.activate(low)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    low.vertex_groups.clear()
    low.data.calc_loop_triangles()
    log("low tris", len(low.data.loop_triangles))
    return low


def finish_low(low, rim):
    """Materials by the Param UV, then the final UVs: planar on the top, cylindrical on the underside."""
    mesh = low.data
    nl = len(mesh.loops)
    params = np.zeros(nl * 2, F32)
    mesh.uv_layers["Param"].data.foreach_get("uv", params)
    params = params.reshape(-1, 2)
    lv = np.zeros(nl, np.int32)
    mesh.loops.foreach_get("vertex_index", lv)
    co = np.zeros(len(mesh.vertices) * 3, F32)
    mesh.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    ux, uy, uz = -co[:, 0], co[:, 2], -co[:, 1]
    ls = np.zeros(len(mesh.polygons), np.int32)
    lt = np.zeros(len(mesh.polygons), np.int32)
    mesh.polygons.foreach_get("loop_start", ls)
    mesh.polygons.foreach_get("loop_total", lt)
    face_of_loop = np.repeat(np.arange(len(ls)), lt)
    face_u = np.bincount(face_of_loop, params[:, 0], len(ls)) / lt
    mat = (face_u >= 1.0).astype(np.int32)
    mesh.polygons.foreach_set("material_index", mat)
    # Planar top UV.
    lx, ly, lz = ux[lv], uy[lv], uz[lv]
    top_uv = np.stack([(lx - IC[0] + EXTENT) / (2 * EXTENT), (lz - IC[1] + EXTENT) / (2 * EXTENT)], -1)
    # Cylindrical underside UV, angle unwrapped around each face's centre.
    ang = np.arctan2(lz - IC[1], lx - IC[0])
    fx = np.bincount(face_of_loop, lx, len(ls)) / lt
    fz = np.bincount(face_of_loop, lz, len(ls)) / lt
    fa = np.arctan2(fz - IC[1], fx - IC[0])[face_of_loop]
    da = (ang - fa + math.pi) % (2 * math.pi) - math.pi
    a_un = fa + da
    A, R, y_rim = rim
    yr = np.interp((a_un + math.pi) % (2 * math.pi) - math.pi, A, y_rim, period=2 * math.pi)
    cliff_uv = np.stack([(a_un + math.pi) / (2 * math.pi), 1 - (yr - ly) / CLIFF_DEPTH], -1)
    is_cliff = mat[face_of_loop] == 1
    uvs = np.where(is_cliff[:, None], cliff_uv, top_uv).astype(F32)
    uv = mesh.uv_layers.new(name="UVMap")
    uv.data.foreach_set("uv", uvs.ravel())
    mesh.uv_layers.remove(mesh.uv_layers["Param"])
    mesh.uv_layers.active = mesh.uv_layers["UVMap"]
    for p in mesh.polygons:
        p.use_smooth = True
    tris = np.zeros(2, np.int64)
    mesh.calc_loop_triangles()
    for t in mesh.loop_triangles:
        tris[mat[t.polygon_index]] += 1
    log("low tris top", tris[0], "cliff", tris[1])
    return {"top": int(tris[0]), "cliff": int(tris[1])}


# ---------------------------------------------------------------------------------------------------------------
# Layout: every asset on the island, in Unity world space
# ---------------------------------------------------------------------------------------------------------------

def R_(folder, name, prefix=None):
    return (folder, name, prefix or name)


A_ = dict(
    Frond=R_("Env/Flora_Frond", "Flora_Frond"), GlowTree=R_("Env/Flora_GlowTree", "Flora_GlowTree"),
    Mushroom=R_("Env/Flora_Mushroom", "Flora_Mushroom"), Tendril=R_("Env/Flora_Tendril", "Flora_Tendril"),
    Egg=R_("Env/Egg_Cluster", "Egg_Cluster"), Boulder=R_("Env/Decor_Boulder", "Decor_Boulder"),
    Column=R_("Env/Decor_Column", "Decor_Column"), Spike=R_("Env/Decor_Spike", "Decor_Spike"),
    Spire=R_("Env/Decor_Spire", "Decor_Spire"), Pebbles=R_("Env/Scatter_Pebbles", "Scatter_Pebbles"),
    Splinters=R_("Env/Scatter_CrystalSplinters", "Scatter_CrystalSplinters"),
    Shells=R_("Env/Scatter_ShellFragments", "Scatter_ShellFragments"), Vein=R_("Env/CrystalVein", "CrystalVein"),
    CrystalA=R_("Crystal_A", "Crystal_A"), CrystalB=R_("Crystal_B", "Crystal_B"), CrystalC=R_("Crystal_C", "Crystal_C"),
    RockA=R_("Rock_A", "Rock_A"), RockB=R_("Rock_B", "Rock_B"), RockC=R_("Rock_C", "Rock_C"),
    LavaRock=R_("Env/AshenReach/Ash_Decor_LavaRock", "Ash_Decor_LavaRock"),
    EmberBloom=R_("Env/AshenReach/Ash_Flora_EmberBloom", "Ash_Flora_EmberBloom"),
    Fulgurite=R_("Env/AshenReach/Ash_Decor_Fulgurite", "Ash_Decor_Fulgurite"),
    IceShard=R_("Env/NorthernCrown/North_Decor_IceShard", "North_Decor_IceShard"),
    FrozenCrystal=R_("Env/NorthernCrown/North_Decor_FrozenCrystal", "North_Decor_FrozenCrystal"),
    FrostFern=R_("Env/NorthernCrown/North_Flora_FrostFern", "North_Flora_FrostFern"),
    Reed=R_("Env/Mirewood/Mire_Flora_Reed", "Mire_Flora_Reed"), DeadTree=R_("Env/Mirewood/Mire_Flora_DeadTree", "Mire_Flora_DeadTree"),
    Fungus=R_("Env/Mirewood/Mire_Decor_Fungus", "Mire_Decor_Fungus"),
    Pillar=R_("Env/SouthernCrown/South_Decor_ShatteredPillar", "South_Decor_ShatteredPillar"),
    Pad=R_("Env/BuildPadTile", "BuildPadTile"), Gun=R_("Tower_Gun", "Tower_Gun_T1", "Tower_Gun"),
    Laser=R_("Tower_Laser", "Tower_Laser_T1", "Tower_Laser"), Beacon=R_("Cutscene_Beacon", "Cutscene_Beacon"),
    Mouth=R_("Menu_NestMouth", "Menu_NestMouth"),
)
# Footprint radius per unit scale (from the exports' size_m), for spacing and path clearance.
FOOT = dict(Frond=0.45, GlowTree=0.55, Mushroom=0.35, Tendril=0.22, Egg=0.4, Boulder=0.45, Column=0.35, Spike=0.3,
            Spire=0.6, Pebbles=0.25, Splinters=0.2, Shells=0.25, Vein=0.4, CrystalA=0.4, CrystalB=0.35, CrystalC=0.3,
            RockA=0.3, RockB=0.3, RockC=0.3, LavaRock=0.4, EmberBloom=0.28, Fulgurite=0.3, IceShard=0.3,
            FrozenCrystal=0.4, FrostFern=0.3, Reed=0.25, DeadTree=0.48, Fungus=0.35, Pillar=0.6, Pad=0.46, Gun=0.4,
            Laser=0.4, Beacon=0.45, Mouth=1.3)
GLOWS = {"GlowTree", "Mushroom", "CrystalA", "CrystalB", "CrystalC", "Egg", "FrozenCrystal", "EmberBloom", "Vein",
         "Fungus", "Beacon"}
SWAYS = {"Frond", "GlowTree", "Mushroom", "Tendril", "Reed", "FrostFern", "EmberBloom"}


def routes():
    """The creature paths of MenuDiorama.BuildCreatures as xz polylines (props keep clear of them)."""
    paths = []
    t = np.radians(np.linspace(-90, 270, 60))
    paths.append(np.stack([NEST[0] + np.cos(t) * WALKER_LAP, NEST[1] + np.sin(t) * WALKER_LAP], -1))
    paths.append(np.array([[MOUTH[0], MOUTH[1] + 1.26], [MOUTH[0], MOUTH[1] - 0.7], [NEST[0], NEST[1] - WALKER_LAP]]))
    for p in POOLS:
        a = np.radians(np.linspace(p["bank"] - BANK_ARC / 2, p["bank"] + BANK_ARC / 2, 24))
        r = p["r"] + BANK_MARGIN
        arc = np.stack([p["c"][0] + np.cos(a) * r, p["c"][1] + np.sin(a) * r], -1)
        paths.append(np.vstack([[p["c"]], arc, [p["c"]]]))
    return paths


def seg_distance(pt, poly):
    a, b = poly[:-1], poly[1:]
    ab = b - a
    t = np.clip(np.sum((pt - a) * ab, 1) / np.maximum(np.sum(ab * ab, 1), 1e-9), 0, 1)
    return float(np.min(np.hypot(*(a + ab * t[:, None] - pt).T)))


class Layout:
    def __init__(self, seed=26):
        self.rng = np.random.default_rng(seed)
        self.items = []
        self.paths = routes()

    def free(self, x, z, radius, allow_shore=False, route_clear=0.45, island_margin=1.2):
        pt = np.array((x, z), F32)
        d = math.hypot(x - IC[0], z - IC[1])
        if d > float(rim_radius(np.array(math.atan2(z - IC[1], x - IC[0])))) - island_margin - radius:
            return False
        for p in POOLS:
            pd = float(pool_distance(np.array(x, F32), np.array(z, F32), p))
            limit = p["r"] * (0.92 if allow_shore else 1.08) + radius * (0.3 if allow_shore else 1.0)
            if pd < limit:
                return False
        if float(porch_weight(np.array(x, F32), np.array(z, F32))) > 0.05:
            return False
        for poly in self.paths:
            if seg_distance(pt, poly) < route_clear + radius:
                return False
        for it in self.items:
            if math.hypot(it["x"] - x, it["z"] - z) < (it["foot"] + radius) * 0.85:
                return False
        return True

    def add(self, kind, x, z, scale, group, yaw=None, sink=0.04, lift=0.0, tilt=(0.0, 0.0), note=None, force=False,
            flip=False, y=None):
        foot = FOOT[kind] * scale
        if not force and not self.free(x, z, foot):
            return False
        yy = float(height(np.array(x, F32), np.array(z, F32))) - sink + lift if y is None else y
        yaw = float(self.rng.uniform(0, 360)) if yaw is None else yaw
        it = dict(kind=kind, x=float(x), y=yy, z=float(z), yaw=round(yaw, 2), scale=round(float(scale), 3),
                  group=group, foot=foot, tilt=[round(t, 2) for t in tilt], flip=flip)
        if note:
            it["note"] = note
        self.items.append(it)
        return True

    def ring(self, kind, centre, rmin, rmax, count, smin, smax, a0, a1, group, shore=False, tries=60, **kw):
        placed = 0
        for i in range(count):
            for _ in range(tries):
                a = math.radians(a0 + (a1 - a0) * (i + 0.5) / count + self.rng.uniform(-14, 14))
                r = self.rng.uniform(rmin, rmax)
                x, z = centre[0] + math.cos(a) * r, centre[1] + math.sin(a) * r
                s = self.rng.uniform(smin, smax)
                foot = FOOT[kind] * s
                if self.free(x, z, foot, allow_shore=shore):
                    self.add(kind, x, z, s, group, force=True, **kw)
                    placed += 1
                    break
        return placed


def build_layout():
    L = Layout()
    pools = {p["kind"]: p for p in POOLS}
    # The hive: the hooded mouth over the notch, a crown spire, tendrils and spikes on the mound, eggs round its foot.
    L.add("Mouth", MOUTH[0], 3.55, 1.0, "nest", yaw=180.0, sink=0.0, y=PORCH, force=True,
          note="opening faces -z; the crawlers' 'inside' point (z 3.66) lies under the hood")
    L.add("Spire", NEST[0] + 0.25, NEST[1] + 0.55, 0.62, "nest", yaw=20.0, sink=0.12, force=True)
    for kind, ang, rad, s in (("Spike", 205, 2.15, 0.85), ("Spike", 335, 2.2, 0.8), ("Spike", 140, 1.2, 1.0),
                              ("Spike", 60, 1.5, 0.9), ("Spike", 25, 2.6, 0.75)):
        a = math.radians(ang)
        L.add(kind, NEST[0] + math.cos(a) * rad, NEST[1] + math.sin(a) * rad, s, "nest", sink=0.08, force=True)
    L.ring("Tendril", NEST, NEST_R * 0.42, NEST_R * 0.82, 7, 0.9, 1.35, -10, 330, "nest", sink=0.05)
    L.ring("Egg", NEST, NEST_R + 0.35, NEST_R + 1.35, 6, 0.8, 1.2, -40, 250, "nest")
    L.ring("Shells", NEST, NEST_R + 0.4, NEST_R + 1.8, 4, 0.9, 1.2, 180, 360, "nest")
    # Pond (forest corner, teal): fronds, glow trees and mushrooms round the bank, reeds in the shallows.
    p = pools["Water"]["c"]
    L.ring("GlowTree", p, 3.1, 4.3, 2, 0.95, 1.15, 160, 300, "pond")
    L.ring("Frond", p, 2.7, 4.2, 5, 0.85, 1.2, 130, 330, "pond")
    L.ring("Mushroom", p, 2.6, 3.9, 4, 0.65, 1.0, 110, 340, "pond")
    L.ring("Reed", p, 1.85, 2.35, 5, 0.8, 1.15, 120, 330, "pond", shore=True, sink=0.1)
    # Lava: lava rock, spikes, a column, ember blooms on the warm crust.
    p = pools["Lava"]["c"]
    L.ring("LavaRock", p, 2.7, 3.8, 3, 0.8, 1.2, 20, 170, "lava")
    L.ring("Spike", p, 2.8, 3.9, 3, 0.7, 1.1, 30, 190, "lava", sink=0.06)
    L.ring("Boulder", p, 3.3, 4.4, 2, 0.8, 1.1, 40, 160, "lava")
    L.ring("Column", p, 3.5, 4.2, 1, 0.9, 1.0, 100, 140, "lava")
    L.ring("Fulgurite", p, 2.6, 3.6, 2, 0.8, 1.0, 340, 420, "lava")
    L.ring("EmberBloom", p, 2.2, 2.7, 4, 0.8, 1.1, 10, 200, "lava", shore=True)
    # Quicksand pit (old ruins): columns, a shattered pillar, Crysium clusters.
    p = pools["Quicksand"]["c"]
    L.ring("Column", p, 2.4, 3.2, 2, 0.85, 1.1, 190, 260, "sand")
    L.ring("Pillar", p, 2.6, 3.4, 1, 0.9, 1.0, 120, 170, "sand")
    L.add("CrystalA", p[0] + 1.6, p[1] - 2.0, 1.35, "sand", note="large Crysium cluster, the sand corner's light")
    L.ring("CrystalB", p, 2.2, 3.0, 2, 0.65, 0.9, 20, 160, "sand")
    L.ring("RockB", p, 2.4, 3.2, 2, 0.7, 0.95, 160, 250, "sand")
    # Frozen lake: ice shards, a frozen crystal, frost ferns.
    p = pools["Ice"]["c"]
    L.ring("IceShard", p, 2.2, 3.2, 3, 0.8, 1.15, 20, 160, "ice")
    L.ring("FrozenCrystal", p, 2.5, 3.2, 1, 0.9, 1.0, 60, 120, "ice")
    L.ring("FrostFern", p, 2.1, 3.0, 3, 0.8, 1.1, 250, 380, "ice")
    # Bog: a dead tree, reeds, fungus.
    p = pools["Bog"]["c"]
    L.ring("DeadTree", p, 2.6, 3.4, 1, 0.95, 1.1, 200, 250, "bog")
    L.ring("Reed", p, 1.75, 2.3, 5, 0.8, 1.2, 200, 420, "bog", shore=True, sink=0.1)
    L.ring("Fungus", p, 2.3, 3.2, 2, 0.75, 1.0, 180, 300, "bog")
    # The outpost on its rise: two pads with a gun and a laser, the survey beacon, a lit crate of crystal.
    o = OUTPOST
    L.add("Pad", o[0] - 0.55, o[1] - 0.15, 1.0, "outpost", yaw=8.0, sink=0.0, force=True)
    L.add("Pad", o[0] + 0.55, o[1] + 0.2, 1.0, "outpost", yaw=8.0, sink=0.0, force=True)
    L.add("Gun", o[0] - 0.55, o[1] - 0.15, 1.0, "outpost", yaw=172.0, sink=-0.12, force=True,
          note="stands on the pad (pad top 0.12)")
    L.add("Laser", o[0] + 0.55, o[1] + 0.2, 1.0, "outpost", yaw=200.0, sink=-0.12, force=True)
    L.add("Beacon", o[0] + 0.15, o[1] + 1.15, 0.8, "outpost", yaw=190.0, sink=0.0, force=True)
    L.add("CrystalC", o[0] - 1.3, o[1] + 0.9, 0.7, "outpost", force=True)
    # Open ground: rocks, boulders, pebbles, splinters and veins of Crysium, a few lone crystals with light.
    L.ring("RockA", IC, 5.5, 9.3, 5, 0.55, 1.1, 0, 360, "scatter")
    L.ring("RockC", IC, 5.5, 9.3, 4, 0.55, 1.0, 0, 360, "scatter")
    L.ring("Boulder", IC, 6.5, 9.0, 3, 0.7, 1.0, 0, 360, "scatter")
    L.ring("Pebbles", IC, 2.0, 9.5, 8, 0.9, 1.3, 0, 360, "scatter", sink=0.0)
    L.ring("Splinters", IC, 2.0, 9.5, 6, 0.9, 1.2, 0, 360, "scatter", sink=0.0)
    L.ring("Vein", IC, 3.0, 8.5, 4, 0.8, 1.1, 0, 360, "scatter", sink=0.01)
    L.ring("CrystalB", IC, 5.0, 8.6, 2, 0.75, 1.0, 330, 400, "scatter")
    L.ring("CrystalC", IC, 5.0, 8.6, 2, 0.6, 0.9, 120, 200, "scatter")
    # Under the island: Crysium hanging from the spires (only seen when the camera drifts or from other angles).
    for (sx, sz), extra, width in SPIRES:
        rho = math.hypot(sx - IC[0], sz - IC[1]) / R0
        y = float(rim_height(np.array([math.atan2(sz - IC[1], sx - IC[0])], F32))[0])
        mean = y
        dep = float(under_depth(np.array(sx, F32), np.array(sz, F32), np.array(rho, F32)))
        L.add("CrystalA", sx, sz, 1.1, "underside", y=mean - dep * 0.93, flip=True, force=True,
              note="hangs point down from the spire tip")
    return L


# ---------------------------------------------------------------------------------------------------------------
# The burrow mouth (shared pipeline: parts -> remesh -> bake)
# ---------------------------------------------------------------------------------------------------------------

WINE = dict(top=np.array((0.34, 0.1, 0.14)), side=np.array((0.17, 0.045, 0.08)), bottom=np.array((0.04, 0.01, 0.025)))
EMBER = np.array((1.0, 0.36, 0.14))
PINK = np.array((1.0, 0.42, 0.78))


def chitin_style(seed, glow=0.0, depth_glow=0.0):
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, WINE["top"], WINE["side"], WINE["bottom"])
        plates = C.value_noise(p, 9, seed)
        groove = 1 - C.smoothstep(0.0, 0.08, np.abs(C.value_noise(p, 5.5, seed + 1)))
        col = col * (0.85 + 0.25 * plates)[:, None]
        col = C.lerp3(col, np.array((0.03, 0.005, 0.015)), groove * 0.7)
        # Pale worn ridges where the crawlers rub past.
        ridge = C.smoothstep(0.35, 0.8, C.value_noise(p, 3.0, seed + 2)) * C.smoothstep(0.2, 0.8, n[:, 2])
        col = C.lerp3(col, np.array((0.55, 0.32, 0.3)), ridge * 0.35)
        emit = PINK[None, :] * (groove * glow * C.smoothstep(-0.2, 0.5, C.value_noise(p, 2.0, seed + 3)))[:, None]
        if depth_glow:
            # Deeper in the burrow (+y, local) the dark warms to ember.
            d = C.smoothstep(0.4, 1.9, p[:, 1]) * C.smoothstep(0.9, 0.1, np.abs(p[:, 0]))
            col = C.lerp3(col, np.array((0.06, 0.012, 0.01)), np.clip(d * 1.2, 0, 1))
            emit = emit + EMBER[None, :] * (d * depth_glow)[:, None]
        s.albedo[idx] = col
        s.emit[idx] = np.clip(emit, 0, 1)
        s.disp[idx] = 0.008 * plates - 0.012 * groove
        s.rough[idx] = np.clip(0.38 + 0.25 * groove - 0.15 * ridge, 0.15, 1)
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = p[:, 2]
    return style


def throat_style(seed):
    def style(s, idx, part):
        p = s.p[idx]
        pulse = C.smoothstep(-0.2, 0.6, C.value_noise(p, 4, seed))
        s.albedo[idx] = np.array((0.035, 0.006, 0.008)) * (1 + 0.5 * pulse)[:, None]
        s.emit[idx] = np.clip(EMBER[None, :] * (0.25 + 0.5 * pulse)[:, None], 0, 1)
        s.disp[idx] = 0.01 * pulse
        s.rough[idx] = 0.22
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = p[:, 2]
    return style


def tooth_style(seed):
    def style(s, idx, part):
        z, radius, dist = C.tube_param(part, s.p[idx])
        col = C.lerp3(np.array((0.62, 0.48, 0.4)), np.array((0.08, 0.03, 0.03)), C.smoothstep(0.3, 1.0, z))
        s.albedo[idx] = col * (0.9 + 0.2 * C.value_noise(s.p[idx], 20, seed))[:, None]
        s.emit[idx] = np.clip(EMBER[None, :] * (0.25 * C.smoothstep(0.6, 1.0, z))[:, None], 0, 1)
        s.disp[idx] = 0.0
        s.rough[idx] = 0.32
        s.fam[idx] = E.FAM_CHITIN
        s.flow[idx] = z
    return style


def mouth_builder():
    """Hooded burrow over the porch notch. Local frame: x across, -y out of the burrow (front), z up, pivot on the
    porch floor at the centre of the opening."""
    b = C.PartBuilder()
    rng = np.random.default_rng(2601)
    W0, H0 = 1.2, 1.55

    def shell(depth0, depth1, scale):
        def outer(u, v):
            a = math.pi * (0.03 + 0.94 * u)
            y = depth0 + (depth1 - depth0) * v
            k = 1 - 0.2 * v
            return (math.cos(a) * (W0 + 0.22) * k * scale, y, math.sin(a) * (H0 + 0.22) * k * scale - 0.12)

        def inner(u, v):
            a = math.pi * (0.03 + 0.94 * u)
            y = depth0 + (depth1 - depth0) * v
            k = 1 - 0.2 * v
            return (math.cos(a) * W0 * k * scale, y, math.sin(a) * H0 * k * scale - 0.12)
        return outer, inner
    o, i = shell(-0.05, 1.85, 1.0)
    b.slab("Hood", o, i, 28, 10, style=chitin_style(2610, glow=0.5, depth_glow=0.85))
    # Ribs: arches over the opening, each further in a little smaller, the front one a heavy lip.
    for k in range(5):
        y = -0.12 + 0.42 * k
        sc = 1 - 0.08 * k
        rad = (0.17 if k == 0 else 0.11) * (1 + 0.15 * rng.uniform(-1, 1))
        pts = []
        for t in np.linspace(0.02, 0.98, 9):
            a = math.pi * t
            wob = 0.04 * math.sin(t * 9 + k)
            pts.append((math.cos(a) * (W0 + 0.06) * sc, y + wob, math.sin(a) * (H0 + 0.06) * sc - 0.14))
        radii = [rad * (1.3 if j in (0, 8) else 1.0) * (1 + 0.25 * math.sin(j * 1.7 + k)) for j in range(9)]
        b.tube("Rib", pts, radii, sides=10, samples=4, style=chitin_style(2620 + k, glow=0.9))
    # Buttress feet either side of the opening, sunk into the porch.
    for side in (-1, 1):
        b.ellipsoid("Foot", Vector((side * 1.38, 0.15, 0.05)), (0.42, 0.62, 0.42), style=chitin_style(2630 + side),
                    deform=C.chiselled(9, 2631 + side, 0.14), segments=(20, 12))
        b.ellipsoid("Shoulder", Vector((side * 1.05, 0.55, 1.15)), (0.36, 0.55, 0.32), style=chitin_style(2640 + side, glow=0.4),
                    deform=C.chiselled(7, 2641 + side, 0.12), segments=(16, 10))
    # The throat: a wet dark membrane closing the burrow, glowing ember from deeper inside.
    b.ellipsoid("Throat", Vector((0.0, 1.55, 0.5)), (0.95, 0.22, 0.85), style=throat_style(2650), segments=(24, 14))
    b.ellipsoid("BackCap", Vector((0.0, 1.9, 0.45)), (1.3, 0.38, 1.3), style=chitin_style(2655, glow=0.3),
                deform=C.chiselled(8, 2656, 0.1), segments=(22, 14))
    # Teeth: hooked chitin hanging from the lip, pointing down and in.
    for j in range(9):
        t = 0.12 + 0.76 * j / 8
        a = math.pi * t
        base = Vector((math.cos(a) * (W0 - 0.02), -0.1, math.sin(a) * (H0 - 0.02) - 0.14))
        inward = Vector((-math.cos(a), 0.25, -math.sin(a))).normalized()
        down = (inward + Vector((0, 0.15, -0.6))).normalized()
        length = 0.22 + 0.16 * (1 - abs(t - 0.5) * 2) + rng.uniform(-0.04, 0.04)
        b.shard("Tooth", base, down, 0.055, length, sides=6, twist=0.4, flute=0.15, bend=Vector((0, 0.06, -0.04)),
                style=tooth_style(2660 + j))
    # Spines along the hood ridge, raking back.
    for j in range(5):
        t = 0.3 + 0.4 * j / 4
        a = math.pi * t
        base = Vector((math.cos(a) * (W0 + 0.2), 0.35 + 0.3 * j, math.sin(a) * (H0 + 0.2) - 0.14))
        out = Vector((math.cos(a) * 0.6, 0.8, math.sin(a))).normalized()
        b.shard("Spine", base, out, 0.07, 0.45 + rng.uniform(0, 0.25), sides=6, twist=0.8, flute=0.2,
                bend=Vector((0, 0.15, -0.05)), style=tooth_style(2680 + j))
    return b


def build_mouth():
    C.reset_scene()
    asset = C.build_mesh_asset("Menu_NestMouth", mouth_builder(), voxel=0.022, target_tris=2400, tex_size=1024,
                               ao_distance=0.5, extrusion=0.04, ray=0.12, emission_strength=2.4, smooth_iterations=2,
                               symmetric=False, low_from_detail=True, families=("chitin", "membrane", "crystal"),
                               low_size=512)
    C.export_fbx(os.path.join(OUT_MOUTH, "Menu_NestMouth.fbx"), [asset.low], animated=False)
    dims = asset.low.dimensions
    C.write_report("Menu_NestMouth", {
        "asset": "Menu_NestMouth", "round": 26, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
        "texture_size": 1024, "low_texture_size": 512, "maps": ["Albedo", "Normal", "Emission", "Mask"],
        "uv": asset.uv_stats, "detail": asset.details,
        "size_m": [round(dims.x, 3), round(dims.y, 3), round(dims.z, 3)],
        "pivot": "porch floor at the centre of the opening; the opening faces local +Z in Unity (place with yaw 180 "
                 "so it faces the menu camera, see Menu_Island_layout.json)",
        "replaces": "UI/MenuDiorama.cs:220-228 (MouthHole quad + MouthRim torus); MouthLight stays",
    })
    C.save_blend("Menu_NestMouth")
    if not ARGS.get("norender"):
        C.preview_world((0.03, 0.02, 0.035))
        C.ground(color=(0.06, 0.05, 0.07))
        rig = C.light_rig(scale=2.0)
        for az, tag in ((22, "front"), (200, "back")):
            cam = C.closeup_camera(Vector((0, 0.4, 0.8)), 6.2, azimuth=az, elevation=18, lens=50)
            C.render(cam, os.path.join(PREVIEW, f"nestmouth_{tag}.png"), 900, 640)
            bpy.data.objects.remove(cam)
        C.hide_lights(rig, True)
    return asset


# ---------------------------------------------------------------------------------------------------------------
# Baking (Cycles): tangent normals high -> low, ambient occlusion of the low mesh with every prop in place
# ---------------------------------------------------------------------------------------------------------------

def bake_targets(low, sizes):
    images = {}
    low.data.materials.clear()
    for key, (h, w) in sizes.items():
        mat = bpy.data.materials.new("Menu_Island_" + key)
        mat.use_nodes = True
        img = bpy.data.images.new(f"bake_{key}", w, h, alpha=False, float_buffer=True)
        img.colorspace_settings.name = "Non-Color"
        node = mat.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = img
        mat.node_tree.nodes.active = node
        low.data.materials.append(mat)
        images[key] = img
    return images


def image_array(img):
    w, h = img.size
    a = np.empty(w * h * 4, F32)
    img.pixels.foreach_get(a)
    return a.reshape(h, w, 4)


def bake(low, high, props_visible, kind, samples, sizes, extrusion=0.1, ray=0.6, distance=1.4):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = False
    images = bake_targets(low, sizes)
    world = scene.world or bpy.data.worlds.new("BakeWorld")
    scene.world = world
    world.light_settings.distance = distance
    for o in bpy.context.scene.objects:
        o.select_set(False)
    bake = scene.render.bake
    bake.margin = 8
    if kind == "NORMAL":
        high.hide_render = False
        high.select_set(True)
        low.select_set(True)
        bpy.context.view_layer.objects.active = low
        bake.use_selected_to_active = True
        bake.cage_extrusion = extrusion
        bake.max_ray_distance = ray
        bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", use_selected_to_active=True,
                            cage_extrusion=extrusion, max_ray_distance=ray)
    else:
        high.hide_render = True
        low.select_set(True)
        bpy.context.view_layer.objects.active = low
        bake.use_selected_to_active = False
        bpy.ops.object.bake(type="AO", use_selected_to_active=False)
    out = {k: image_array(img)[..., :3].copy() for k, img in images.items()}
    for img in images.values():
        bpy.data.images.remove(img)
    log("baked", kind)
    return out


# ---------------------------------------------------------------------------------------------------------------
# Painting the top (planar, 2048) and the cliff (cylindrical, 4096 x 512)
# ---------------------------------------------------------------------------------------------------------------

TILE = os.path.join(C.EXPORTS, "Env", "Tiling")


def tiling(name, map_, srgb):
    return S.load_png(os.path.join(TILE, name, f"{name}_{map_}.png"), linear_from_srgb=srgb)


def theme_weights(x, z):
    w = {}
    for p in POOLS:
        d = np.hypot(x - p["c"][0], z - p["c"][1])
        w[p["kind"]] = (1 - ss(p["r"] * 0.9, p["r"] + 3.0 + 0.8 * fbm2(x, z, 0.4, 2, 50 + len(w)), d)).astype(F32)
    return w


def paint_top(items, ao_baked, n_baked):
    n = TOP_RES
    t = ((np.arange(n) + 0.5) / n).astype(F32)
    X = IC[0] - EXTENT + t[None, :] * 2 * EXTENT
    Z = IC[1] - EXTENT + t[:, None] * 2 * EXTENT
    X = np.broadcast_to(X, (n, n)).copy()
    Z = np.broadcast_to(Z, (n, n)).copy()
    log("paint top: height")
    H = height(X, Z)
    px = 2 * EXTENT / n
    gx = (np.roll(H, -1, 1) - np.roll(H, 1, 1)) / (2 * px)
    gz = (np.roll(H, -1, 0) - np.roll(H, 1, 0)) / (2 * px)
    slope = np.hypot(gx, gz)
    cav = H - S.blur(H, 6)
    d_ic = np.hypot(X - IC[0], Z - IC[1])
    R = rim_radius(np.arctan2(Z - IC[1], X - IC[0]))
    edge = ss(R - 1.5, R, d_ic)

    log("paint top: base")
    g_alb = S.sample_wrap(tiling("Ground", "Albedo", True), X / 3.0, Z / 3.0)[..., :3]
    g_msk = S.sample_wrap(tiling("Ground", "Mask", False), X / 3.0, Z / 3.0)
    g_em = S.sample_wrap(tiling("Ground", "Emission", True), X / 3.0, Z / 3.0)[..., :3]
    f_alb = S.sample_wrap(tiling("GroundFlora", "Albedo", True), X / 2.0, Z / 2.0)[..., :3]
    f_em = S.sample_wrap(tiling("GroundFlora", "Emission", True), X / 2.0, Z / 2.0)[..., :3]
    c_alb = S.sample_wrap(tiling("Cliff", "Albedo", True), X / 3.0, Z / 3.0)[..., :3]
    big = fbm2(X, Z, 0.18, 3, 60)
    mid = fbm2(X, Z, 0.9, 3, 61)
    alb = g_alb * (0.9 + 0.22 * big[..., None]) * (0.95 + 0.1 * mid[..., None])
    emit = g_em * 0.6
    smooth = 0.14 + 0.1 * g_msk[..., 1]
    dust = g_msk[..., 3] * 0.6
    alb = mix(alb, c_alb * 0.9, edge * 0.8)
    # Slopes show the layered rock under the plates.
    alb = mix(alb, c_alb * 0.8, ss(0.5, 1.2, slope) * 0.6)

    w = theme_weights(X, Z)
    log("paint top: corners")
    # Lava: basalt crust, ash, glowing cracks that run up from the shore and fade.
    lw = w["Lava"]
    crack = ridged2(X, Z, 1.1, 3, 70)
    lava_col = np.array((0.035, 0.022, 0.024), F32) * (0.8 + 0.4 * mid[..., None])
    alb = mix(alb, lava_col, lw * 0.85)
    cr = ss(0.86, 0.98, crack) * lw ** 2.0
    emit = emit + np.array((1.0, 0.32, 0.06), F32) * (cr * 1.2)[..., None]
    alb = mix(alb, np.array((0.25, 0.06, 0.01), F32), cr * 0.6)
    ash = ss(0.1, 0.6, fbm2(X, Z, 2.2, 2, 71)) * lw * (1 - cr)
    alb = mix(alb, np.array((0.08, 0.075, 0.08), F32), ash * 0.5)
    # Pond (forest): wet teal moss creeping out from the bank, glowing spore dots.
    ww = w["Water"]
    moss = ss(-0.15, 0.45, fbm2(X, Z, 1.3, 3, 72) + ww * 0.8 - 0.4) * ww
    alb = mix(alb, np.array((0.012, 0.05, 0.05), F32) * (0.7 + 0.6 * mid[..., None]), moss * 0.9)
    f1, _, cv = S.cellular(X, Z, 0.09, 73)
    dots = (1 - ss(0.1, 0.25, f1)) * ss(0.94, 0.98, cv) * moss * ss(0.2, 0.6, fbm2(X, Z, 0.7, 2, 79) + 0.3)
    emit = emit + np.array((0.35, 1.0, 0.85), F32) * (dots * 0.6)[..., None]
    smooth = smooth + 0.25 * moss
    # Quicksand (ruins): pale sand drifts and a scatter of grit.
    sw = w["Quicksand"]
    sand = ss(-0.3, 0.4, fbm2(X, Z, 0.8, 3, 74) + sw - 0.5) * sw
    ripple = 0.5 + 0.5 * np.sin((X * 0.7 + Z) * 9 + 3 * fbm2(X, Z, 0.6, 2, 75))
    alb = mix(alb, np.array((0.2, 0.18, 0.22), F32) * (0.85 + 0.25 * ripple[..., None]), sand * 0.85)
    # Ice: rime and frost feathers, glossy.
    iw = w["Ice"]
    rime = ss(-0.1, 0.5, fbm2(X, Z, 1.6, 3, 76) + iw * 0.8 - 0.55) * iw
    alb = mix(alb, np.array((0.2, 0.25, 0.34), F32) * (0.75 + 0.3 * mid[..., None]), rime * 0.75)
    smooth = smooth + 0.3 * rime
    # Bog: black-green mud, glossy, a faint green glow in puddles.
    bw = w["Bog"]
    mud = ss(-0.2, 0.5, fbm2(X, Z, 1.0, 3, 77) + bw - 0.4) * bw
    alb = mix(alb, np.array((0.018, 0.024, 0.01), F32) * (0.7 + 0.6 * mid[..., None]), mud * 0.9)
    puddle = ss(0.45, 0.6, fbm2(X, Z, 2.4, 2, 78)) * mud
    smooth = smooth + 0.35 * mud + 0.3 * puddle
    emit = emit + np.array((0.25, 0.6, 0.1), F32) * (puddle * 0.08)[..., None]

    log("paint top: banks")
    for p in POOLS:
        pd = pool_distance(X, Z, p)
        r = p["r"]
        under = 1 - ss(r * 0.7, r * 1.0, pd)
        wet = ss(r * 0.55, r * 0.95, pd) * (1 - ss(r * 0.95, r * 1.25, pd))
        if p["kind"] == "Lava":
            crust = wet
            alb = mix(alb, np.array((0.02, 0.012, 0.012), F32), crust * 0.9)
            glow = (1 - ss(r * 0.94, r * 1.03, pd)) * ss(r * 0.8, r * 0.95, pd)
            glow = glow * ss(0.5, 0.9, crack)
            emit = emit + np.array((1.0, 0.38, 0.08), F32) * (glow * 0.7)[..., None]
            alb = mix(alb, np.array((0.25, 0.06, 0.015), F32), glow * 0.5)
        elif p["kind"] == "Ice":
            alb = mix(alb, np.array((0.5, 0.6, 0.72), F32), wet * 0.7)
            smooth = smooth + 0.3 * wet
        else:
            alb = alb * (1 - 0.55 * wet)[..., None]
            smooth = smooth + 0.4 * wet
        alb = alb * (1 - 0.5 * under)[..., None]
        smooth = smooth + 0.2 * under

    log("paint top: hive")
    mh, m = mound(X, Z)
    dn = np.hypot(X - NEST[0], Z - NEST[1])
    creep = ss(-0.25, 0.35, fbm2(X, Z, 0.9, 3, 80) + (1 - ss(NEST_R * 0.6, NEST_R * 1.7, dn)) - 0.55)
    alb = mix(alb, f_alb * 0.75, creep * 0.85)
    emit = emit + f_em * (creep * 0.25)[..., None]
    # The mound itself: wine-dark resin in a honeycomb of cells, ribs catching a lighter sheen, pink veins.
    f1, f2, cv = S.cellular(X, Z, 0.34, 81)
    cells = ss(0.02, 0.12, f2)
    resin = np.array((0.07, 0.015, 0.025), F32) * (0.7 + 0.6 * cv[..., None])
    on_mound = ss(0.05, 0.3, m)
    alb = mix(alb, resin * (0.5 + 0.5 * cells[..., None]), on_mound)
    ang = np.arctan2(Z - NEST[1], X - NEST[0])
    rib = 0.5 + 0.5 * np.sin(ang * 11 + 1.8 * fbm2(X, Z, 0.45, 2, 6))
    alb = mix(alb, np.array((0.2, 0.07, 0.08), F32), (rib ** 6 * on_mound * 4 * m * (1 - m)) * 0.6)
    vein = ss(0.86, 0.97, ridged2(X, Z, 0.75, 3, 82)) * ss(0.02, 0.4, m + creep * 0.3)
    emit = emit + np.array((1.0, 0.35, 0.72), F32) * (vein * 1.1)[..., None]
    emit = emit + np.array((1.0, 0.25, 0.4), F32) * ((1 - cells) * on_mound * 0.08)[..., None]
    smooth = smooth + 0.3 * on_mound
    # Trails: the walkers' lap round the mound and the porch are trodden smooth and darker.
    lap = np.exp(-((dn - WALKER_LAP) / 0.32) ** 2) * (0.6 + 0.4 * ss(-0.3, 0.3, fbm2(X, Z, 1.5, 2, 83)))
    porch = porch_weight(X, Z)
    trod = np.clip(lap * 0.7 + porch * 0.8, 0, 1)
    alb = alb * (1 - 0.3 * trod)[..., None]
    scr = ss(0.9, 0.98, ridged2(X * 3, Z * 0.6, 2.0, 2, 84)) * porch
    alb = mix(alb, np.array((0.16, 0.1, 0.12), F32), scr * 0.5)
    smooth = smooth + 0.1 * trod

    log("paint top: outpost and props")
    do = np.hypot(X - OUTPOST[0], Z - OUTPOST[1])
    cleared = 1 - ss(1.2, 2.2, do)
    alb = mix(alb, np.array((0.07, 0.07, 0.085), F32) * (0.85 + 0.3 * mid[..., None]), cleared * 0.7)
    tracks = (1 - ss(0.0, 0.05, np.abs(np.sin((X - OUTPOST[0]) * 1.3 + (Z - OUTPOST[1]) * 0.4) * 0.6))) * cleared
    alb = alb * (1 - 0.25 * tracks)[..., None]
    # Crysium dust round every crystal, glints that catch the bloom.
    crys = np.zeros_like(X)
    for it in items:
        if it["kind"].startswith("Crystal") and it["group"] != "underside":
            dd = np.hypot(X - it["x"], Z - it["z"])
            crys = np.maximum(crys, 1 - ss(0.3, 1.6 * it["scale"] + 0.3, dd))
    _, _, gv = S.cellular(X, Z, 0.05, 85)
    crys = crys * crys
    glint = ss(0.975, 0.99, gv) * crys
    alb = mix(alb, np.array((0.3, 0.1, 0.26), F32), crys * 0.12 + glint * 0.4)
    emit = emit + np.array((1.0, 0.45, 0.85), F32) * (glint * 0.9 + crys * 0.02)[..., None]
    dust = np.clip(dust + crys * 0.8, 0, 1)

    # Occlusion: baked (props included) times the small-scale cavity of the height field.
    ao = np.clip(ao_baked[..., 0], 0, 1) ** 0.85 if ao_baked is not None else np.ones_like(X)
    ao = ao * np.clip(1 + cav * 1.8, 0.55, 1.0)
    ao = ao * (g_msk[..., 0] * 0.4 + 0.6)

    nrm_detail = S.decode_normal(S.sample_wrap(tiling("Ground", "Normal", False), X / 3.0, Z / 3.0))
    base_n = S.decode_normal(n_baked) if n_baked is not None else np.dstack([np.zeros_like(X), np.zeros_like(X), np.ones_like(X)])
    nrm = S.rnm(base_n, nrm_detail, 0.8 * (1 - 0.5 * on_mound)[..., None])
    # The mound's honeycomb as relief.
    cell_h = cells * 0.012 * on_mound
    ch_x = (np.roll(cell_h, -1, 1) - np.roll(cell_h, 1, 1)) / (2 * px)
    ch_z = (np.roll(cell_h, -1, 0) - np.roll(cell_h, 1, 0)) / (2 * px)
    cn = np.dstack([-ch_x, -ch_z, np.ones_like(ch_x)])
    cn /= np.linalg.norm(cn, axis=-1, keepdims=True)
    nrm = S.rnm(nrm, cn, 1.0)

    hn = (H - H.min()) / max(1e-6, H.max() - H.min())
    maps = dict(
        Albedo=S.linear_to_srgb(np.clip(alb, 0, 1)),
        Emission=S.linear_to_srgb(np.clip(emit, 0, 1)),
        Mask=np.dstack([np.clip(ao, 0, 1), np.clip(smooth, 0, 0.9), hn, np.clip(dust, 0, 1)]),
        Normal=S.encode_normal(nrm),
    )
    return maps


def paint_cliff(rim, ao_baked, n_baked):
    hgt, wid = CLIFF_RES
    A, R, y_rim = rim
    u = ((np.arange(wid) + 0.5) / wid).astype(F32)
    v = ((np.arange(hgt) + 0.5) / hgt).astype(F32)
    ang = u * 2 * math.pi - math.pi
    depth = (1 - v) * CLIFF_DEPTH
    a2 = np.broadcast_to(ang[None, :], (hgt, wid))
    d2 = np.broadcast_to(depth[:, None], (hgt, wid))
    Rr = np.interp(ang, A, R, period=2 * math.pi)[None, :]
    arc = a2 * float(R.mean())
    yr = np.interp(ang, A, y_rim, period=2 * math.pi)[None, :]
    y = yr - d2
    log("paint cliff")
    c_alb = S.sample_wrap(tiling("Cliff", "Albedo", True), arc / 3.0, -y / 3.0)[..., :3]
    c_msk = S.sample_wrap(tiling("Cliff", "Mask", False), arc / 3.0, -y / 3.0)
    c_em = S.sample_wrap(tiling("Cliff", "Emission", True), arc / 3.0, -y / 3.0)[..., :3]
    px_ = np.cos(a2) * Rr
    pz_ = np.sin(a2) * Rr
    big = fbm3(px_ * 0.2, y * 0.2, pz_ * 0.2, 1.0, 3, 90)
    strata = 0.5 + 0.5 * np.sin(y * 7.4 + 2.0 * fbm3(px_ * 0.3, y * 0.3, pz_ * 0.3, 1.0, 2, 91))
    band = ss(0.35, 0.65, strata)
    alb = c_alb * (0.75 + 0.35 * band[..., None]) * (0.85 + 0.25 * big[..., None])
    # Deeper rock is colder and more violet; near the rim it keeps the plain's grey-lilac.
    deep = ss(1.5, 8.0, d2)
    alb = mix(alb, alb * np.array((0.65, 0.5, 1.0), F32), deep * 0.6)
    # Crysium veins running down the faces, brighter in the depths, and nodes where they cross the strata.
    vein = ss(0.88, 0.975, ridged2(arc * 0.55, y * 0.18, 1.0, 3, 92))
    vein = vein * ss(0.3, 1.5, d2) * (0.6 + 0.4 * ss(-0.2, 0.4, fbm3(px_ * 0.4, y * 0.4, pz_ * 0.4, 1.0, 2, 93)))
    node = vein * ss(0.7, 0.95, strata)
    alb = mix(alb, np.array((0.5, 0.12, 0.42), F32), vein * 0.6)
    emit = c_em * 0.5 + np.array((1.0, 0.42, 0.85), F32) * (vein * 0.9 + node * 0.8)[..., None]
    # Seepage: dark wet streaks under the ledges.
    streak = ss(0.55, 0.85, fbm2(arc * 1.6, y * 0.15, 1.0, 3, 94)) * (1 - band)
    alb = alb * (1 - 0.35 * streak)[..., None]
    smooth = 0.12 + 0.3 * streak + 0.2 * vein
    ao = np.clip(ao_baked[..., 0], 0, 1) ** 0.9 if ao_baked is not None else np.ones_like(arc)
    ao = ao * (0.75 + 0.25 * band) * (c_msk[..., 0] * 0.4 + 0.6)
    nrm_detail = S.decode_normal(S.sample_wrap(tiling("Cliff", "Normal", False), arc / 3.0, -y / 3.0))
    base_n = S.decode_normal(n_baked) if n_baked is not None else np.dstack([np.zeros_like(arc), np.zeros_like(arc), np.ones_like(arc)])
    nrm = S.rnm(base_n, nrm_detail, 0.9)
    return dict(
        Albedo=S.linear_to_srgb(np.clip(alb, 0, 1)),
        Emission=S.linear_to_srgb(np.clip(emit, 0, 1)),
        Mask=np.dstack([np.clip(ao, 0, 1), np.clip(smooth, 0, 0.9), band, vein]),
        Normal=S.encode_normal(nrm),
    )


def write_maps(prefix, maps):
    paths = {}
    for k, img in maps.items():
        path = os.path.join(OUT, f"Menu_Island_{prefix}_{k}.png")
        S.write_png(path, img)
        S.write_png(os.path.join(OUT, "Low", f"Menu_Island_{prefix}_{k}.png"),
                    S.half(img) if k != "Normal" else S.encode_normal(S.decode_normal(S.half(img))))
        paths[k.lower()] = path
    return paths


# ---------------------------------------------------------------------------------------------------------------
# Height map of the game mesh, pool outlines, layout json
# ---------------------------------------------------------------------------------------------------------------

def height_map(low, res=512):
    bm = bmesh.new()
    bm.from_mesh(low.data)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    tree = BVHTree.FromBMesh(bm)
    t = (np.arange(res) + 0.5) / res
    out = np.full((res, res), np.nan, F32)
    for iz in range(res):
        z = IC[1] - EXTENT + t[iz] * 2 * EXTENT
        for ix in range(res):
            x = IC[0] - EXTENT + t[ix] * 2 * EXTENT
            hit = tree.ray_cast(Vector((-x, -z, 20.0)), Vector((0, 0, -1)), 40.0)
            if hit[0] is not None:
                out[iz, ix] = hit[0].z
    bm.free()
    inside = ~np.isnan(out)
    lo, hi = float(np.nanmin(out)), float(np.nanmax(out))
    img = np.where(inside, (out - lo) / (hi - lo), 0.0)
    path = os.path.join(OUT, "Menu_Island_Height.png")
    S.write_png(path, img.astype(F32), bits=16)
    log("height map", lo, hi)
    return {"file": "Menu_Island_Height.png", "size": res, "bits": 16, "min_y": round(lo, 4), "max_y": round(hi, 4),
            "decode": "y = min_y + texel * (max_y - min_y); texel 0 where no ground (outside the island)",
            "uv": "u = (x - 3.6 + 13) / 26, v = (z - 2.2 + 13) / 26 (v = 0 is the bottom row of the image)",
            "source": "ray cast onto the exported game mesh, so feet match what is drawn"}


def pool_outlines():
    out = []
    for p in POOLS:
        pts = []
        for a in np.linspace(0, 2 * math.pi, 72, endpoint=False):
            lo, hi = 0.2 * p["r"], 2.2 * p["r"]
            for _ in range(30):
                mid = (lo + hi) / 2
                x = np.array(p["c"][0] + math.cos(a) * mid, F32)
                z = np.array(p["c"][1] + math.sin(a) * mid, F32)
                if float(pool_distance(x, z, p)) < p["r"]:
                    lo = mid
                else:
                    hi = mid
            pts.append([round(p["c"][0] + math.cos(a) * lo, 3), round(p["c"][1] + math.sin(a) * lo, 3)])
        out.append(dict(kind=p["kind"], theme=p["theme"], centre=list(p["c"]), radius=p["r"], surface_y=p["surface"],
                        bank_degrees=p["bank"], outline_xz=pts))
    return out


def lights_json(items):
    lights = [dict(name="MouthLight", position=[float(MOUTH[0]), 0.95, 3.0], colour="NestTheme.AccentColor",
                   intensity=2.6, range=6.0, note="inside the hood, so the burrow glows; keep the flicker")]
    pond = POOLS[1]["c"]
    lights.append(dict(name="PondLight", position=[pond[0] - 1.9, 0.9, pond[1] + 1.4], colour="ForestTheme.AccentColor",
                       intensity=1.3, range=4.5))
    for it in items:
        if it["kind"] == "CrystalA" and it["group"] == "sand":
            lights.append(dict(name="CrystalLight", position=[it["x"], it["y"] + 0.9, it["z"]],
                               colour="RuinsTheme.AccentColor", intensity=1.1, range=4.0))
    lights.append(dict(name="LavaGlow", position=[POOLS[0]["c"][0], 0.5, POOLS[0]["c"][1]], colour=[1.0, 0.42, 0.12],
                       intensity=1.6, range=5.0, note="new: the lava pool lights its rocks"))
    lights.append(dict(name="OutpostLamp", position=[float(OUTPOST[0]) + 0.15, 1.4, float(OUTPOST[1]) + 1.15],
                       colour=[0.62, 0.86, 1.0], intensity=0.9, range=3.5, note="new: colony light on the beacon"))
    return lights


def write_layout(layout, hmap, tris, top_paths, cliff_paths):
    items = []
    for it in layout.items:
        f, fbx, _ = A_[it["kind"]]
        e = dict(asset=fbx, folder=f"ArtSource/Exports/{f}", group=it["group"],
                 position=[round(it["x"], 3), round(it["y"], 3), round(it["z"], 3)], yaw=it["yaw"], scale=it["scale"])
        if it["flip"]:
            e["flip_x_180"] = True
        if it.get("note"):
            e["note"] = it["note"]
        e["sway"] = it["kind"] in SWAYS
        e["glow_pool"] = it["kind"] in GLOWS and it["group"] != "underside"
        items.append(e)
    data = {
        "round": 26, "generator": "Tools/Blender/build_menu_diorama.py",
        "frame": "Unity world space (x right, y up, z away from the menu camera); yaw = degrees about +Y; "
                 "the island mesh is placed at the origin",
        "island": {"asset": "Menu_Island", "position": [0, 0, 0], "materials": ["Menu_Island_Top", "Menu_Island_Cliff"],
                   "top_uv": "u = (x - 3.6 + 13) / 26, v = (z - 2.2 + 13) / 26", "cliff_uv":
                   "u = (angle + pi) / 2pi round (3.6, 2.2), v = 1 - depth below the rim / 11"},
        "camera": {"focus": list(CAMERA_FOCUS), "distance": CAMERA_DISTANCE, "pitch": CAMERA_PITCH,
                   "yaw": CAMERA_YAW, "fov": CAMERA_FOV, "note": "unchanged from MenuDiorama.cs"},
        "height_map": hmap,
        "pools": pool_outlines(),
        "creature_routes": "MenuDiorama.BuildCreatures unchanged; props keep >= 0.45 m (plus their footprint) "
                           "off the walker lap (r 3.9), the mouth-porch line and every pool's bank arc",
        "lights": lights_json(layout.items),
        "items": items,
        "groups": {"outpost": "new in round 26 (towers on the rise at the back); optional",
                   "underside": "hang under the island, only seen when the camera drifts"},
    }
    with open(os.path.join(OUT, "Menu_Island_layout.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)
    report = {"asset": "Menu_Island", "round": 26, "game_tris": tris["top"] + tris["cliff"], "tris": tris,
              "maps": {"Top": [f"Menu_Island_Top_{k}.png" for k in ("Albedo", "Normal", "Emission", "Mask")],
                       "Cliff": [f"Menu_Island_Cliff_{k}.png" for k in ("Albedo", "Normal", "Emission", "Mask")]},
              "sizes": {"Top": TOP_RES, "Cliff": [CLIFF_RES[1], CLIFF_RES[0]], "Low": "half"},
              "mask": "Top: R AO (props baked in), G smoothness, B height 0..1, A Crysium dust; "
                      "Cliff: R AO, G smoothness, B strata band, A vein",
              "placed_assets": len(items), "replaces": ["UI/MenuTerrain.cs:196-236 (mesh, painted albedo/emission)",
                                                        "UI/MenuDiorama.cs:220-228 (mouth)",
                                                        "UI/MenuDiorama.cs:271-319 (PropFactory rings)"]}
    with open(os.path.join(OUT, "Menu_Island_report.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=1)
    log("layout", len(items), "items")


# ---------------------------------------------------------------------------------------------------------------
# Preview scene
# ---------------------------------------------------------------------------------------------------------------

LIQ = os.path.join(C.EXPORTS, "Liquids")


def liquid_material(kind):
    if kind == "Water":
        return S.image_material("Liq_Water", normal=os.path.join(LIQ, "Water", "Water_Normal_Swell.png"),
                                color=(0.004, 0.02, 0.05), rough=0.06, emission=None, specular=0.6)
    if kind == "Lava":
        mat = S.image_material("Liq_Lava", albedo=os.path.join(LIQ, "Lava", "Lava_CrustA_Albedo.png"),
                               normal=os.path.join(LIQ, "Lava", "Lava_CrustA_Normal.png"), rough=0.6)
        # Heat is a mask: glow orange through it (the game's lava shader ramps it the same way).
        nt = mat.node_tree
        heat = nt.nodes.new("ShaderNodeTexImage")
        heat.image = bpy.data.images.load(os.path.join(LIQ, "Lava", "Lava_CrustA_Heat.png"), check_existing=True)
        heat.image.colorspace_settings.name = "Non-Color"
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(heat.outputs["Color"], sep.inputs[0])
        bsdf = nt.nodes.get("Principled BSDF")
        bsdf.inputs["Emission Color"].default_value = (1.0, 0.3, 0.05, 1)
        mul = nt.nodes.new("ShaderNodeMath")
        mul.operation = "MULTIPLY"
        mul.inputs[1].default_value = 7.0
        nt.links.new(sep.outputs[0], mul.inputs[0])
        nt.links.new(mul.outputs[0], bsdf.inputs["Emission Strength"])
        return mat
    if kind == "Quicksand":
        return S.image_material("Liq_Sand", albedo=os.path.join(LIQ, "Quicksand", "Quicksand_Albedo.png"),
                                normal=os.path.join(LIQ, "Quicksand", "Quicksand_Normal.png"), rough=0.8)
    if kind == "Ice":
        return S.image_material("Liq_Ice", albedo=os.path.join(LIQ, "Ice", "Ice_Albedo.png"),
                                normal=os.path.join(LIQ, "Ice", "Ice_Normal.png"), rough=0.12, specular=0.6)
    return S.image_material("Liq_Bog", normal=os.path.join(LIQ, "Bog", "Bog_Normal.png"), color=(0.02, 0.03, 0.012),
                            rough=0.2)


def liquids():
    objs = []
    for p in pool_outlines():
        pts = [S.u2b(x, p["surface_y"], z) for x, z in p["outline_xz"]]
        mesh = bpy.data.meshes.new("Liquid_" + p["kind"])
        c = S.u2b(p["centre"][0], p["surface_y"], p["centre"][1])
        verts = [c] + pts
        n = len(pts)
        faces = [(0, 1 + (i + 1) % n, 1 + i) for i in range(n)]
        mesh.from_pydata([tuple(v) for v in verts], [], faces)
        uv = mesh.uv_layers.new()
        for poly in mesh.polygons:
            for li in poly.loop_indices:
                v = mesh.vertices[mesh.loops[li].vertex_index].co
                uv.data[li].uv = (v.x / 3.0, v.y / 3.0)
        mesh.materials.append(liquid_material(p["kind"]))
        objs.append(C.link(bpy.data.objects.new("Liquid_" + p["kind"], mesh)))
    return objs


def island_material(name, paths, emission=2.0):
    return C.game_material(name, paths, emission, 0.4, ())


def preview(low, lib, top_paths, cliff_paths, layout):
    low.data.materials.clear()
    low.data.materials.append(island_material("Menu_Island_Top", top_paths, 2.2))
    low.data.materials.append(island_material("Menu_Island_Cliff", cliff_paths, 2.0))
    liquids()
    C.preview_world(FOG, 1.0)
    S.emission_plane("Stars", (IC[0], -9.0, IC[1]), 90.0, os.path.join(C.EXPORTS, "UI", "UI_Starfield.png"), 2.0)
    S.emission_plane("StarsFar", (IC[0], -30.0, IC[1] + 20), 260.0, os.path.join(C.EXPORTS, "UI", "UI_Starfield.png"), 1.2)
    sun_dir = S.unity_direction(*SUN_EULER)
    S.sun("Sun", sun_dir, SUN_COLOR, 2.6)
    # The flat ambient of the menu (fog colour pulled towards the sun colour) as a weak fill from the camera side.
    fill = S.sun("Fill", (0.2, -0.5, -0.8), (0.45, 0.42, 0.6), 0.35, shadow=False)
    pal = {"NestTheme.AccentColor": (1.0, 0.42, 0.55), "ForestTheme.AccentColor": (0.4, 0.95, 0.85),
           "RuinsTheme.AccentColor": (0.8, 0.6, 1.0)}
    for lj in lights_json(layout.items):
        col = pal.get(lj["colour"], lj["colour"]) if isinstance(lj["colour"], str) else lj["colour"]
        S.point(lj["name"], lj["position"], col, lj["intensity"] * 25.0, radius=0.2)
    # Stand-ins for the creatures, for scale only (MenuDiorama spawns the real ones).
    sk = ("Skitter", "Skitter", "Skitter")
    for i, a in enumerate((200.0, 250.0, 320.0)):
        x = NEST[0] + math.cos(math.radians(a)) * WALKER_LAP
        z = NEST[1] + math.sin(math.radians(a)) * WALKER_LAP
        lib.place(sk, (x, float(height(np.array(x, F32), np.array(z, F32))), z), yaw=a + 90 + 180, scale=1.0)
    return fill


def render_views(low, tag=""):
    w, h = (960, 540) if QUICK else (1600, 900)
    samples = 32 if QUICK else 64
    paths = []
    pos = S.unity_orbit_position(CAMERA_FOCUS, CAMERA_DISTANCE, CAMERA_PITCH, CAMERA_YAW)
    cam = S.unity_camera("MenuCam", pos, CAMERA_PITCH, CAMERA_YAW, CAMERA_FOV)
    paths.append(S.render(cam, os.path.join(PREVIEW, f"menu_diorama{tag}.png"), w, h, samples))
    # The drift's far ends (MenuCameraDrift: yaw +-4.5, pitch +-1.4).
    pos2 = S.unity_orbit_position(CAMERA_FOCUS, CAMERA_DISTANCE, CAMERA_PITCH - 1.4, CAMERA_YAW + 4.5)
    cam2 = S.unity_camera("MenuCamDrift", pos2, CAMERA_PITCH - 1.4, CAMERA_YAW + 4.5, CAMERA_FOV)
    paths.append(S.render(cam2, os.path.join(PREVIEW, f"menu_diorama_drift{tag}.png"), w // 2, h // 2, samples))
    # The floating chunk from the side and below (other uses, and to judge the underside).
    side = S.unity_camera("SideCam", (IC[0] - 26, -2.5, IC[1] - 20), 2.0, 52.0, 34.0, clip_end=200)
    paths.append(S.render(side, os.path.join(PREVIEW, f"menu_island_side{tag}.png"), w // 2, h // 2, samples))
    over = S.unity_camera("OverCam", (IC[0], 30.0, IC[1] - 14.0), 64.0, 0.0, 50.0, clip_end=200)
    paths.append(S.render(over, os.path.join(PREVIEW, f"menu_island_over{tag}.png"), w // 2, h // 2, samples))
    close = S.unity_camera("MouthCam", (MOUTH[0] - 1.2, 2.2, MOUTH[1] - 4.6), 18.0, 14.0, 40.0)
    paths.append(S.render(close, os.path.join(PREVIEW, f"menu_nest_close{tag}.png"), w // 2, h // 2, samples))
    return paths


# ---------------------------------------------------------------------------------------------------------------

def main():
    if not ARGS.get("skipmouth"):
        build_mouth()
    C.reset_scene()
    high, rim = build_high()
    low = build_low(high)
    tris = finish_low(low, rim)
    layout = build_layout()
    log("layout items", len(layout.items))
    lib = S.Library()
    for it in layout.items:
        lib.place(A_[it["kind"]], (it["x"], it["y"], it["z"]), yaw=it["yaw"], scale=it["scale"], flip=it["flip"])
    sizes = {"Top": (TOP_RES, TOP_RES), "Cliff": CLIFF_RES}
    nb = bake(low, high, True, "NORMAL", 1, sizes, extrusion=0.12, ray=0.6)
    ab = bake(low, high, True, "AO", 12 if QUICK else 24, sizes, distance=1.2)
    top = paint_top(layout.items, ab["Top"], nb["Top"])
    top_paths = write_maps("Top", top)
    cliff = paint_cliff(rim, ab["Cliff"], nb["Cliff"])
    cliff_paths = write_maps("Cliff", cliff)
    low.data.materials.clear()
    for key in ("Top", "Cliff"):
        low.data.materials.append(bpy.data.materials.new("Menu_Island_" + key))
    C.export_fbx(os.path.join(OUT, "Menu_Island.fbx"), [low], animated=False)
    hmap = height_map(low, 256 if QUICK else 512)
    write_layout(layout, hmap, tris, top_paths, cliff_paths)
    high.hide_render = True
    high.hide_viewport = True
    preview(low, lib, top_paths, cliff_paths, layout)
    C.save_blend("Menu_Diorama")
    if not ARGS.get("norender"):
        render_views(low)
    log("done")


main()
