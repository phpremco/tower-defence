"""Round 27: upgrade-screen display stage (see ArtSource/DESIGN_NOTES.md and README.md, round 27).

Replaces the code-made stage of `ModelPreview.BuildStage` (`Assets/_Project/Scripts/UI/ModelPreview.cs:207-250`):
the floor disc (PropMeshBuilder tube), the glow torus and the gradient quad behind the model.

Assets (all in `Exports/UI_Display/`, Low/ copies at half size):
  UI_DisplayPedestal.fbx   `UI_DisplayPedestal` (static base) + `UI_DisplayPedestal_Ring` (top ring, turns about its
                           own up axis). Pivot of both: the centre of the top face (the model stands at 0). Top radius
                           1.0, foot radius 1.08, 0.2 high. Hard-surface tower_kit pieces, one atlas, self-baked:
                           UI_DisplayPedestal_Albedo/_Normal/_Emission/_Mask.png (1024, Low 512). The emission is
                           white so `_EmissionColor` tints the rim channel and lamps.
  UI_DisplayRing.fbx/.png  flat quad 2.6 x 2.6 (radius 1.3) + greyscale additive halo texture (1024, Low 512).
  UI_DisplayBackdrop.png   2048 x 1024 (Low 1024) research-bay wall, rendered from a 3D scene in this script.

Run: blender --background --factory-startup --python Tools/Blender/build_ui_display.py -- [--preview <dir>]
     [--only pedestal,ring,backdrop,preview] [--nobake] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import marker_kit as M  # noqa: E402
import stage_kit as sk  # noqa: E402
import story_kit as stk  # noqa: E402
import tower_kit as tk  # noqa: E402
from marker_kit import centred, gauss, line, sstep  # noqa: E402

ASSET = "UI_Display"
OUT = os.path.join(C.EXPORTS, ASSET)
TAU = 2 * math.pi
V = bk.V

# Pedestal dimensions (top radius 1, as the old floor disc of radius 1 that ModelPreview scales).
TOP = 0.2
FOOT_R = 1.08
RING_QUAD = 2.6          # halo quad side: texture r 1.0 = 1.3 units
RING_MAIN = 1.1          # main halo line just outside the foot
SIDES = 40

# The ModelPreview rig (ModelPreview.cs constants), for the staged preview.
LOOK_DOWN = 16.0
FOV = 30.0
FRAME_FILL = 0.8
FLOOR_SHARE = 1.25
BACKDROP_DISTANCE = 30.0
BACKDROP_SIZE = 40.0
COLONY_RIM = (0.55, 0.8, 1.0)

REPORT = {"round": 27, "asset": ASSET, "files": {}}

WHITE_RIM = (1.0, 1.0, 1.0)


def rim_glow(strength):
    return tk.glow(WHITE_RIM, strength)


# ---------------------------------------------------------------------------------------------------------------
# 1. Pedestal
# ---------------------------------------------------------------------------------------------------------------

def pedestal_kit():
    k = tk.Kit("UI_DisplayPedestal", ["Main"])
    on = ["Main"]
    foot = tk.GUNMETAL.but(wear=0.5)
    # Foot: a chamfered plinth (open underneath; nobody sees the bottom).
    k.lathe("Foot", [(0.98, 0.0), (1.06, 0.0), (FOOT_R, 0.018), (1.065, 0.045), (0.98, 0.045)], tk.frame(), foot,
            "Root", on, sides=SIDES, bevel=0.0)
    # Core drum, seen only through the seams between the cladding panels.
    k.lathe("Drum", [(1.0, 0.045), (1.0, 0.15)], tk.frame(), tk.DARK, "Root", on,
            sides=SIDES, bevel=0.0)
    # Cladding: eight white ceramic panels with real gaps (the panel seams), chamfered top and bottom.
    prof = [(0.99, 0.05), (1.035, 0.05), (1.056, 0.07), (1.056, 0.118), (1.035, 0.14), (0.99, 0.14)]
    gap = math.radians(1.4)
    n = 8
    for i in range(n):
        a0 = TAU * i / n + gap / 2 + math.radians(22.5)
        a1 = TAU * (i + 1) / n - gap / 2 + math.radians(22.5)
        stk.add(k, "Panel", (lambda a0=a0, a1=a1: stk.bm_lathe_arc(prof, 5, a0, a1)), tk.frame(),
                tk.WHITE.but(panel=0.0, bolts=False, wear=0.7), "Root", on, bevel=0.003)
    # Indicator lamps in four of the seams and two hazard tabs on opposite panels (camera side and back).
    for i in range(4):
        a = math.radians(22.5 + 90 * i)
        p = V(math.cos(a) * 1.012, math.sin(a) * 1.012, 0.095)
        k.block("Lamp", (0.014, 0.012, 0.05), tk.frame(p, z=V(math.cos(a), math.sin(a), 0), y=bk.UP), rim_glow(1.6),
                "Root", on, bevel=0.0)
    for a_deg in (90.0, 270.0):
        a = math.radians(a_deg)
        p = V(math.cos(a) * 1.058, math.sin(a) * 1.058, 0.094)
        k.block("Hazard", (0.2, 0.024, 0.004), tk.frame(p, z=V(math.cos(a), math.sin(a), 0), y=bk.UP),
                tk.HAZARD.but(wear=0.6), "Root", on, bevel=0.0)
    # Blue lip with the recessed rim channel inside it (the emissive ring the shader tints).
    k.lathe("Lip", [(1.04, 0.14), (1.064, 0.14), (1.064, 0.17), (1.04, 0.17)], tk.frame(), tk.BLUE.but(panel=0.0,
            bolts=False), "Root", on, sides=SIDES, bevel=0.0)
    k.lathe("RimChannel", [(0.99, 0.152), (1.04, 0.152), (1.04, 0.145)], tk.frame(), rim_glow(1.1),
            "Root", on, sides=SIDES, bevel=0.0)
    # Deck: the plate the model stands on, flush with the top ring, with a thin inner light ring.
    k.lathe("Deck", [(0.0, TOP), (0.735, TOP), (0.745, 0.19), (0.745, 0.155)], tk.frame(),
            tk.GUNMETAL.but(panel=0.0, wear=0.12, grad=0.4, rough=0.5), "Root", on, sides=SIDES, bevel=0.0)
    k.lathe("DeckLight", [(0.6, TOP), (0.6, TOP + 0.002), (0.615, TOP + 0.002), (0.615, TOP)], tk.frame(),
            rim_glow(0.9), "Root", on, sides=SIDES, bevel=0.0)
    # Deck seams: four radial dark strips from the light ring to the ring edge (the deck is otherwise plain).
    for i in range(4):
        a = math.radians(45 + 90 * i)
        mid = (0.62 + 0.735) / 2
        k.block("DeckSeam", (0.115, 0.006, 0.002), tk.frame(V(math.cos(a) * mid, math.sin(a) * mid, TOP + 0.0005),
                rot=(0, 0, math.degrees(a))), tk.DARK.but(panel=0.0), "Root", on, bevel=0.0)
    # Top ring (bone "Ring": its own object, turns slowly): steel with yellow index ticks and three lamps.
    k.lathe("TopRing", [(0.745, 0.152), (0.985, 0.152), (0.985, 0.186), (0.97, TOP), (0.76, TOP), (0.745, 0.19)],
            tk.frame(), tk.STEEL.but(wear=0.6, rings=0.0), "Ring", on, sides=SIDES, bevel=0.0)
    for i in range(12):
        a = TAU * i / 12
        r = 0.905
        major = i % 3 == 0
        k.block("Tick", (0.06 if major else 0.035, 0.012, 0.003),
                tk.frame(V(math.cos(a) * r, math.sin(a) * r, TOP + 0.0012), rot=(0, 0, math.degrees(a))),
                tk.YELLOW.but(wear=0.5), "Ring", on, bevel=0.0)
    for i in range(3):
        a = TAU * i / 3 + TAU / 24
        r = 0.81
        k.block("RingLamp", (0.03, 0.022, 0.003),
                tk.frame(V(math.cos(a) * r, math.sin(a) * r, TOP + 0.0012), rot=(0, 0, math.degrees(a))),
                rim_glow(1.4), "Ring", on, bevel=0.0)
    return k


def tint_emission(mat, colour, strength=None):
    """Preview only: multiply the (white) emission map by a tint, as `_EmissionColor` does in Unity."""
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    link = bsdf.inputs["Emission Color"].links[0]
    src = link.from_socket
    nt.links.remove(link)
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    nt.links.new(src, mul.inputs[6])
    mul.inputs[7].default_value = (*colour, 1.0)
    nt.links.new(mul.outputs[2], bsdf.inputs["Emission Color"])
    if strength is not None:
        bsdf.inputs["Emission Strength"].default_value = strength


def pedestal_objects(kit, mat, label=""):
    base = bk.module_object([p for p in kit.pieces if p.bone == "Root"], "UI_DisplayPedestal" + label, V(0, 0, TOP), mat)
    ring = bk.module_object([p for p in kit.pieces if p.bone == "Ring"], "UI_DisplayPedestal_Ring" + label,
                            V(0, 0, TOP), mat)
    # Both objects at the origin with the top face at 0: the model's feet go on the pivot.
    for o in (base, ring):
        o.location = (0, 0, 0)
        triangulate(o)
    return base, ring


def triangulate(obj):
    """The deck cap is an n-gon; Unity triangulates anyway, but the FBX exporter needs tris or quads for tangents."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    ngons = [f for f in bm.faces if len(f.verts) > 4]
    if ngons:
        bmesh.ops.triangulate(bm, faces=ngons, quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.to_mesh(obj.data)
    bm.free()


def build_pedestal(bake=True):
    bk.clear_actions()
    kit = pedestal_kit()
    mat, rep = bk.bake_set(kit, OUT, 1024, 512, bake, samples=16)
    base, ring = pedestal_objects(kit, mat)
    bk.export_fbx(os.path.join(OUT, "UI_DisplayPedestal.fbx"), [base, ring], animated=False)
    rep.update({"fbx": "UI_DisplayPedestal.fbx",
                "meshes": {o.name: C.tri_count(o) for o in (base, ring)},
                "tris": C.tri_count(base) + C.tri_count(ring),
                "dims": {o.name: [round(d, 3) for d in o.dimensions] for o in (base, ring)},
                "pivot": "centre of the top face (model feet); both objects share it; Ring turns about Unity +Y",
                "top_radius": 1.0, "foot_radius": FOOT_R, "height": TOP,
                "emission": "white (rim channel, deck light ring, seam lamps, ring lamps): tint with _EmissionColor"})
    REPORT["files"]["UI_DisplayPedestal"] = rep
    C.log("pedestal tris", rep["tris"], rep["meshes"])
    bk.remove([base, ring])
    return kit, mat


# ---------------------------------------------------------------------------------------------------------------
# 2. Halo ring
# ---------------------------------------------------------------------------------------------------------------

def ring_texture(size=1024):
    x, y, r = centred(size, size)
    a = np.arctan2(y, x)
    half = RING_QUAD / 2
    t_main = RING_MAIN / half
    t_arc = 1.2 / half
    px = 2.0 / size
    main = line(r - t_main, px * 1.6, px * 2.0)
    glow = gauss(r - t_main, 0.045) * 0.32 + gauss(r - t_main, 0.012) * 0.45
    # Spill under the pedestal edge (mostly hidden by the foot; reads as light leaking out at the rim).
    spill = sstep(t_main - 0.12, t_main, r) * (r < t_main) * 0.14
    # Outer segmented arc: six arcs with gaps, thinner and dimmer.
    seg = (a / TAU * 6 + 0.04) % 1.0
    arcs_on = sstep(0.0, 0.012, seg) * (1 - sstep(0.9, 0.912, seg))
    arcs = line(r - t_arc, px * 1.1, px * 1.6) * arcs_on * 0.6 + gauss(r - t_arc, 0.01) * arcs_on * 0.12
    # Ticks between the main line and the arcs: every 5 degrees, longer and brighter every 30.
    step = TAU / 72
    d = (((a + step / 2) % step) - step / 2) * r
    tick = line(d, px * 0.9, px * 1.2)
    minor = tick * sstep(t_main + 0.022, t_main + 0.028, r) * (1 - sstep(t_main + 0.042, t_main + 0.048, r))
    major_on = (np.abs(((a + TAU / 24) % (TAU / 12)) - TAU / 24) * r < px * 3).astype(np.float32)
    major = tick * major_on * sstep(t_main + 0.016, t_main + 0.022, r) * (1 - sstep(t_arc - 0.012, t_arc - 0.006, r))
    ticks = minor * 0.35 + major * 0.7
    grey = np.clip(main + glow + spill + arcs + ticks, 0, 1) * (1 - sstep(0.94, 1.0, r))
    return np.dstack([grey, grey, grey, grey]).astype(np.float32)


def build_ring():
    arr = ring_texture()
    path = M.save_tex(ASSET, "UI_DisplayRing", arr)
    quad = M.quad("UI_DisplayRing", RING_QUAD, RING_QUAD, "XY")
    bk.export_fbx(os.path.join(OUT, "UI_DisplayRing.fbx"), [quad], animated=False)
    REPORT["files"]["UI_DisplayRing"] = {
        "fbx": "UI_DisplayRing.fbx", "texture": "UI_DisplayRing.png", "size": [1024, 1024], "low": 512,
        "tris": C.tri_count(quad), "quad": RING_QUAD, "pivot": "centre, flat (normal Unity +Y)",
        "texture_radius_1": RING_QUAD / 2, "main_line_radius": RING_MAIN, "arc_radius": 1.2,
        "colour_space": "linear (sRGB off)", "blend": "additive, greyscale (RGB = A): tint in the material"}
    bk.remove([quad])
    return path, arr


# ---------------------------------------------------------------------------------------------------------------
# 3. Backdrop: a dim research-bay wall, rendered from a small 3D set
# ---------------------------------------------------------------------------------------------------------------

_MATS = {}


def mat(name, colour, rough=0.6, metal=0.0, emit=None, strength=0.0):
    if name not in _MATS:
        _MATS[name] = C.principled("BD_" + name, color=colour, rough=rough, metal=metal, emission=emit,
                                   strength=strength)
    return _MATS[name]


def box(name, lo, hi, material, bevel=0.0, objs=None):
    lo, hi = Vector(lo), Vector(hi)
    size = hi - lo
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    bmesh.ops.translate(bm, vec=(lo + hi) / 2, verts=bm.verts)
    bm.to_mesh(me)
    bm.free()
    o = C.link(bpy.data.objects.new(name, me))
    me.materials.append(material)
    if bevel > 0:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    if objs is not None:
        objs.append(o)
    return o


def area(name, loc, target, size, power, colour, objs, size_y=None):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = power
    data.color = colour
    if size_y is None:
        data.shape = "SQUARE"
        data.size = size
    else:
        data.shape = "RECTANGLE"
        data.size = size
        data.size_y = size_y
    o = C.link(bpy.data.objects.new(name, data))
    o.location = loc
    C.look_at(o, Vector(target))
    objs.append(o)
    return o


def spot(name, loc, target, power, colour, angle, objs, blend=0.6, radius=0.1):
    data = bpy.data.lights.new(name, "SPOT")
    data.energy = power
    data.color = colour
    data.spot_size = math.radians(angle)
    data.spot_blend = blend
    data.shadow_soft_size = radius
    o = C.link(bpy.data.objects.new(name, data))
    o.location = loc
    C.look_at(o, Vector(target))
    objs.append(o)
    return o


# The bay opening (Blender: wall in the XZ plane at y = 0, the camera on +Y).
BAY_X, BAY_Z, BAY_DEPTH = 3.0, 3.6, 1.6


def backdrop_set():
    objs = []
    wall = mat("Wall", (0.11, 0.13, 0.17), rough=0.55, metal=0.0)
    wall2 = mat("Wall2", (0.085, 0.1, 0.13), rough=0.6, metal=0.0)
    seam = mat("Seam", (0.006, 0.007, 0.01), rough=0.9)
    pillar = mat("Pillar", (0.15, 0.17, 0.2), rough=0.45, metal=0.1)
    back = mat("Back", (0.07, 0.085, 0.115), rough=0.55, metal=0.0)
    rib = mat("Rib", (0.085, 0.095, 0.115), rough=0.45, metal=0.1)
    floor = mat("Floor", (0.08, 0.09, 0.105), rough=0.42, metal=0.2)
    strip = mat("Strip", (0.0, 0.0, 0.0), emit=(0.45, 0.7, 1.0), strength=1.2)
    bar = mat("Bar", (0.0, 0.0, 0.0), emit=(0.7, 0.85, 1.0), strength=1.6)
    amber = mat("Amber", (0.0, 0.0, 0.0), emit=(1.0, 0.72, 0.28), strength=4.0)
    yellow = mat("Yellow", (0.55, 0.42, 0.08), rough=0.5)
    floorline = mat("FloorLine", (0.0, 0.0, 0.0), emit=(0.4, 0.65, 1.0), strength=0.7)

    # Outer wall: a seam backing plus inset panels (1.6 x 1.0) with alternating rows standing out a little.
    # Seam backing around (not across) the bay opening.
    box("WallBack", (-14, -0.14, 0), (-BAY_X, -0.1, 8), seam, objs=objs)
    box("WallBack", (BAY_X, -0.14, 0), (14, -0.1, 8), seam, objs=objs)
    box("WallBack", (-BAY_X, -0.14, BAY_Z), (BAY_X, -0.1, 8), seam, objs=objs)
    pw, ph, g = 1.6, 1.0, 0.035
    for row in range(8):
        for col in range(-9, 9):
            x0 = col * pw + (pw / 2 if row % 2 else 0)
            x1 = x0 + pw
            z0, z1 = row * ph, (row + 1) * ph
            if x1 > -BAY_X - 0.7 and x0 < BAY_X + 0.7 and z0 < BAY_Z + 0.8:
                continue
            depth = 0.0 if (row + col) % 3 else 0.025
            box("WallPanel", (x0 + g, -0.1, z0 + g), (x1 - g, depth, z1 - g), wall if row % 2 else wall2,
                bevel=0.012, objs=objs)
    # Pillars and beam framing the bay, with blue light strips on the inner faces.
    for s in (-1, 1):
        x_in, x_out = s * BAY_X, s * (BAY_X + 0.7)
        box("Pillar", (min(x_in, x_out), -BAY_DEPTH, 0), (max(x_in, x_out), 0.18, BAY_Z + 0.8), pillar, bevel=0.03,
            objs=objs)
        xs = x_in - s * 0.012
        box("PillarStrip", (min(xs, xs - s * 0.03), -1.1, 0.35), (max(xs, xs - s * 0.03), -0.95, BAY_Z - 0.3), strip,
            objs=objs)
        # Small amber status lamps and a hazard band on each pillar front.
        xc = s * (BAY_X + 0.35)
        box("StatusLamp", (xc - 0.05, 0.18, 1.55), (xc + 0.05, 0.2, 1.62), amber, objs=objs)
        box("StatusLamp", (xc - 0.05, 0.18, 1.7), (xc + 0.05, 0.2, 1.77), amber, objs=objs)
        for k in range(5):
            z0 = 0.06 + k * 0.08
            box("Hazard", (xc - 0.3, 0.18, z0), (xc + 0.3, 0.195, z0 + 0.04), yellow, objs=objs)
    box("Beam", (-BAY_X - 0.7, -BAY_DEPTH, BAY_Z), (BAY_X + 0.7, 0.18, BAY_Z + 0.8), pillar, bevel=0.03, objs=objs)
    # Chamfer blocks in the upper bay corners.
    for s in (-1, 1):
        o = box("Chamfer", (-0.35, -BAY_DEPTH, -0.35), (0.35, 0.1, 0.35), pillar, bevel=0.02, objs=objs)
        o.rotation_euler = (0, math.radians(45), 0)
        o.location = (s * BAY_X, 0, BAY_Z)
    # Light bar under the beam.
    box("LightBar", (-BAY_X + 0.4, -1.0, BAY_Z - 0.05), (BAY_X - 0.4, -0.75, BAY_Z), bar, objs=objs)
    # Bay back wall: panels with vertical ribs and a recessed horizontal service slot.
    box("BayBack", (-BAY_X, -BAY_DEPTH - 0.1, 0), (BAY_X, -BAY_DEPTH, BAY_Z), back, objs=objs)
    for i in range(11):
        x = -BAY_X + 0.3 + i * (2 * BAY_X - 0.6) / 10
        box("Rib", (x - 0.06, -BAY_DEPTH, 0), (x + 0.06, -BAY_DEPTH + 0.14, BAY_Z), rib, bevel=0.015, objs=objs)
    box("Slot", (-BAY_X, -BAY_DEPTH + 0.0, 1.45), (BAY_X, -BAY_DEPTH + 0.16, 1.55), seam, objs=objs)
    box("SlotLight", (-BAY_X + 0.2, -BAY_DEPTH + 0.15, 1.48), (BAY_X - 0.2, -BAY_DEPTH + 0.17, 1.52), floorline,
        objs=objs)
    # Floor: tiles with dark joints, and a dim light line along the bay threshold.
    box("FloorBase", (-16, -BAY_DEPTH, -0.1), (16, 30, -0.02), seam, objs=objs)
    t = 1.2
    for ix in range(-12, 12):
        for iy in range(-2, 22):
            x0, y0 = ix * t, iy * t * 0.999 - 1.2
            if y0 + t < -BAY_DEPTH or (y0 < 0.18 and abs(x0 + t / 2) > BAY_X + 0.1):
                continue
            box("Tile", (x0 + 0.02, max(y0, -BAY_DEPTH) + 0.02, -0.03), (x0 + t - 0.02, y0 + t - 0.02, 0.0), floor,
                bevel=0.006, objs=objs)
    box("Threshold", (-BAY_X, 0.24, 0.0), (BAY_X, 0.3, 0.008), floorline, objs=objs)

    # Lights: the bay light pours down the back wall; wall washers make pools on the outer panels; a weak fill.
    area("BayLight", (0, -0.9, BAY_Z - 0.1), (0, -1.1, 0), 5.2, 150.0, (0.55, 0.72, 1.0), objs, size_y=0.4)
    area("BayBounce", (0, 0.6, 0.15), (0, -1.6, 1.4), 4.0, 30.0, (0.4, 0.6, 1.0), objs, size_y=0.3)
    for s in (-1, 1):
        for x in (6.2, 10.4):
            spot("Washer", (s * x, 0.9, 7.6), (s * x, -0.1, 2.4), 900.0, (0.5, 0.68, 1.0), 70, objs, blend=0.9)
    area("Fill", (0, 16, 5.5), (0, 0, 1.5), 12.0, 450.0, (0.55, 0.65, 0.9), objs)
    return objs


def backdrop_camera():
    data = bpy.data.cameras.new("BackdropCam")
    data.lens = 50
    data.sensor_fit = "HORIZONTAL"
    data.sensor_width = 36
    data.clip_end = 200
    cam = C.link(bpy.data.objects.new("BackdropCam", data))
    cam.location = (0, 18.0, 1.2)
    C.look_at(cam, Vector((0, 0, 1.2)))
    return cam


def blur2(arr, radius):
    out = arr.copy()
    for c in range(arr.shape[2]):
        out[..., c] = C._blur(arr[..., c], radius)
    return out


def build_backdrop(scratch):
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    C.preview_world((0.004, 0.006, 0.01))
    objs = backdrop_set()
    cam = backdrop_camera()
    scene = bpy.context.scene
    try:
        scene.eevee.use_raytracing = True
    except AttributeError:
        pass
    raw = os.path.join(scratch, "_backdrop_raw.png")
    C.render(cam, raw, 2048, 1024, bloom=False, samples=96)
    arr = C.image_to_array(raw)
    rgb = arr[..., :3]
    # Calm it down: a soft focus (models read against a blurred background), bloom on the lights, a vignette
    # towards the edges and the bottom, a slight overall dimming and a dither against banding.
    soft = blur2(rgb, 3.5)
    rgb = rgb * 0.35 + soft * 0.65
    lum = rgb.max(axis=2)
    bright = rgb * sstep(0.35, 0.9, lum)[..., None]
    rgb = rgb + blur2(bright, 18) * 0.45 + blur2(bright, 60) * 0.35
    h, w = lum.shape
    u, v = M.grid(w, h)
    vig = 1 - 0.55 * sstep(0.18, 0.62, np.hypot((u - 0.5) * 1.15, (v - 0.55) * 1.6))
    vig *= 0.72 + 0.28 * sstep(0.0, 0.3, v)
    rgb = rgb * vig[..., None] * 0.92
    rng = np.random.default_rng(27)
    rgb = rgb + (rng.random(rgb.shape).astype(np.float32) - 0.5) / 255.0
    out = np.dstack([np.clip(rgb, 0, 1), np.ones((h, w), np.float32)]).astype(np.float32)
    path = os.path.join(OUT, "UI_DisplayBackdrop.png")
    C.array_to_image(out, path, "sRGB")
    os.makedirs(os.path.join(OUT, "Low"), exist_ok=True)
    C.array_to_image(C.downsample(out, 2), os.path.join(OUT, "Low", "UI_DisplayBackdrop.png"), "sRGB")
    stats = {"mean": round(float(rgb.mean()), 4), "max_lum_p99": round(float(np.percentile(rgb.max(axis=2), 99)), 4)}
    REPORT["files"]["UI_DisplayBackdrop"] = {
        "texture": "UI_DisplayBackdrop.png", "size": [2048, 1024], "low": [1024, 512], "colour_space": "sRGB",
        "opaque": True, "stats_display_values": stats,
        "quad": "scale (BackdropSize, BackdropSize / 2) = 40 x 20; a square preview at distance ~32 sees the centre "
                "~43 % of the width and ~86 % of the height",
        "horizon_v": 0.32}
    C.log("backdrop", path, stats)
    bk.remove(objs + [cam])
    return path


# ---------------------------------------------------------------------------------------------------------------
# Staged preview: the ModelPreview rig with a tower on the pedestal in front of the backdrop
# ---------------------------------------------------------------------------------------------------------------

def bounds(objs):
    bpy.context.view_layer.update()
    pts = []
    for o in objs:
        if o.type != "MESH":
            continue
        dg = bpy.context.evaluated_depsgraph_get()
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        mw = o.matrix_world
        pts.extend(mw @ v.co for v in me.vertices)
        ev.to_mesh_clear()
    arr = np.array([tuple(p) for p in pts])
    return Vector(arr.min(axis=0)), Vector(arr.max(axis=0))


def backdrop_plane(path, fwd_u):
    """The backdrop quad as ModelPreview places it: on the camera pivot, BackdropDistance ahead, facing the
    camera, 40 x 20 (the texture's 2:1)."""
    me = bpy.data.meshes.new("Backdrop")
    hw, hh = BACKDROP_SIZE / 2, BACKDROP_SIZE / 4
    me.from_pydata([(-hw, -hh, 0), (hw, -hh, 0), (hw, hh, 0), (-hw, hh, 0)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for i, c in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))):
        uv.data[i].uv = c
    o = C.link(bpy.data.objects.new("Backdrop", me))
    fwd = sk.u2b(*fwd_u).normalized()
    o.location = fwd * BACKDROP_DISTANCE
    # Local +Z towards the camera (-fwd), local +Y up the picture.
    up = Vector((0, 0, 1))
    zz = -fwd
    xx = up.cross(zz).normalized()
    yy = zz.cross(xx)
    o.matrix_world = Matrix.Translation(o.location) @ Matrix((xx, yy, zz)).transposed().to_4x4()
    m = bpy.data.materials.new("BackdropMat")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(path, check_existing=True)
    img.extension = "EXTEND"
    nt.links.new(img.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    me.materials.append(m)
    o.visible_shadow = False
    return o


def preview_lights():
    """ModelPreview's three directional lights (Unity Euler -> travel direction)."""
    objs = []
    for name, (pitch, yaw), colour, energy in (("Key", (40, -35), (1.0, 0.96, 0.9), 3.2),
                                                ("Fill", (20, 60), (0.55, 0.7, 1.0), 1.0),
                                                ("Rim", (-20, 160), (0.4, 0.8, 1.0), 2.2)):
        objs.append(sk.sun("P_" + name, sk.unity_direction(pitch, yaw), colour, energy, angle=4.0,
                           shadow=name == "Key"))
    return objs


def stage_preview(kit, mat, ring_arr, backdrop_path, out, tower=("Gun", "T3A"), yaw=-35.0, tag="gun"):
    scene_objs = []
    model = bk.tower(tower[0], tower[1], (0, 0, 0), yaw_deg=yaw, head_deg=yaw + 25.0)
    lo, hi = bounds(model)
    centre = (lo + hi) / 2
    # Turntable: centre the model's bounds on the rig origin (Blender and Unity both).
    model[0].location -= centre
    lo, hi = lo - centre, hi - centre
    ext = (hi - lo) / 2
    wide, tall = max(ext.x, ext.y), max(ext.z, 0.1)
    radius_b = max(ext.length, 0.2)
    half_tan = math.tan(math.radians(FOV / 2))
    # With a pedestal the framing must include it: frame the model plus the pedestal height below it.
    floor_r = max(wide, 0.25) * FLOOR_SHARE
    ped_h = TOP * floor_r
    tall_f = tall + ped_h * 0.5
    distance = (max(wide, tall_f) / half_tan + wide) / FRAME_FILL
    shift = ped_h * 0.5
    fwd_u = sk.unity_direction(LOOK_DOWN, 0)
    cam_pos_u = tuple(-np.asarray(fwd_u) * distance + np.array((0, -shift, 0)))
    cam = sk.unity_camera("PreviewCam", cam_pos_u, LOOK_DOWN, 0, FOV, clip_end=distance + 40, clip_start=0.05)
    scene_objs.append(cam)
    base, ring = pedestal_objects(kit, mat, label="_P")
    for o in (base, ring):
        o.scale = (floor_r, floor_r, floor_r)
        o.location = (0, 0, lo.z)
    ring.rotation_euler.z = math.radians(15)
    scene_objs += [base, ring]
    halo_img = M.preview_image("HaloPrev", M.tinted(ring_arr, [(0, COLONY_RIM, 1.0)]))
    halo = M.quad("HaloQuad", RING_QUAD, RING_QUAD, "XY")
    M.assign(halo, M.fx_material("HaloMat", halo_img, "add", 1.1))
    halo.scale = (floor_r, floor_r, 1)
    halo.location = (0, 0, lo.z - TOP * floor_r + 0.003)
    scene_objs.append(halo)
    bd = backdrop_plane(backdrop_path, fwd_u)
    bd.location += sk.u2b(0, -shift, 0)
    scene_objs.append(bd)
    lights = preview_lights()
    scene_objs += lights
    C.preview_world((0.02, 0.03, 0.05), 0.6)
    p = C.render(cam, os.path.join(out, f"ui_display_{tag}.png"), 1024, 1024, bloom=True, samples=64)
    bk.remove(scene_objs + model)
    return p


def pedestal_close(kit, mat, ring_arr, out):
    base, ring = pedestal_objects(kit, mat, label="_C")
    halo_img = M.preview_image("HaloPrevC", M.tinted(ring_arr, [(0, COLONY_RIM, 1.0)]))
    halo = M.quad("HaloQuadC", RING_QUAD, RING_QUAD, "XY")
    M.assign(halo, M.fx_material("HaloMatC", halo_img, "add", 1.1))
    halo.location = (0, 0, -TOP + 0.003)
    lights = preview_lights()
    C.preview_world((0.02, 0.03, 0.05), 0.6)
    paths = []
    for name, az, el, dist in (("pedestal_close", -30, 24, 4.2), ("pedestal_top", -10, 62, 3.6)):
        cam = tk.closeup_camera(V(0, 0, -0.1), dist, azimuth=az, elevation=el, lens=50)
        paths.append(C.render(cam, os.path.join(out, name + ".png"), 1280, 800, bloom=True, samples=48))
        bk.remove([cam])
    bk.remove([base, ring, halo] + lights)
    return paths


def ring_card(arr, out):
    p = os.path.join(out, "ring_card.png")
    M.texture_card(arr, [(0, COLONY_RIM, 1.0)], p, cell=512, mode="add")
    return p


# ---------------------------------------------------------------------------------------------------------------

def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    parts = only.split(",") if isinstance(only, str) else ["pedestal", "ring", "backdrop", "preview"]
    tk.metal_detail_map()
    os.makedirs(OUT, exist_ok=True)
    kit = mat = None
    if "pedestal" in parts or "preview" in parts:
        kit, mat = build_pedestal(bake=not args.get("nobake") and "pedestal" in parts)
        tint_emission(mat, COLONY_RIM, 1.3)
    ring_arr = build_ring()[1] if "ring" in parts or "preview" in parts else None
    backdrop = os.path.join(OUT, "UI_DisplayBackdrop.png")
    if "backdrop" in parts or not os.path.exists(backdrop):
        backdrop = build_backdrop(out)
    if "pedestal" in parts or "ring" in parts or "backdrop" in parts:
        tk.write_json(os.path.join(OUT, "UI_Display_report.json"), REPORT)
    if args.get("norender"):
        return
    if "preview" in parts:
        ring_card(ring_arr, out)
        pedestal_close(kit, mat, ring_arr, out)
        stage_preview(kit, mat, ring_arr, backdrop, out, ("Gun", "T3A"), -35.0, "gun")
        stage_preview(kit, mat, ring_arr, backdrop, out, ("Cannon", "T2"), 30.0, "cannon")
    C.save_blend("UI_Display")


if __name__ == "__main__":
    main()
