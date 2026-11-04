"""Round 25: projectile meshes, beams, the air-net strand, the recon scout and the debris chunk set.

Assets (Exports/<Asset>/, textures with a half-size Low/ copy):
  Proj_Bolt        Proj_Bolt.fbx (Proj_Bolt rounded bar, Proj_Bolt_Glow crossed shell), Proj_Glow.png
  Proj_RailSlug    Proj_RailSlug.fbx (faceted slug + glow shell); uses Proj_Bolt/Proj_Glow.png
  Proj_Missile     Proj_Missile.fbx (+ Socket_Exhaust, Socket_Nose), baked 256 maps (Low 128)
  Proj_EnemyBolt   Proj_EnemyBolt.fbx (alien ichor glob with barbs + glow shell), Proj_EnemyBolt.png
  Proj_PulseRing   Proj_PulseRing.fbx (boss pulse ring, unit radius), Proj_PulseRing.png
  Beam_Tool        Beam_Tool.fbx (open cone, unit length), Beam_Tool.png
  Net_Strand       Net_Strand.fbx (crossed strip, unit length along X), Net_Strand.png
  Recon_Scout      Recon_Scout.fbx (small probe), baked 256 maps, Trail_Scout.png
  Debris_Chunks    Debris_Chunks.fbx (10 chunks in one 512 atlas, Low 256)
The glow shells and every additive mesh reuse ArtSource/Exports/VFX/Bolt_Core.png where it fits (see README).

Run: blender --background --factory-startup --python Tools/Blender/build_projectiles.py -- [--preview <dir>]
     [--norender] [--nobake]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import marker_kit as M  # noqa: E402
import tower_kit as tk  # noqa: E402
from marker_kit import centred, gauss, grid, line, pack, sstep  # noqa: E402

TAU = 2 * math.pi
FWD = Vector((0, -1, 0))   # Blender -Y = Unity +Z, the flight direction
REPORT = {}
TEXTURES = {}
WARM = (1.0, 0.5, 0.15)
ICHOR = (0.6, 1.0, 0.2)
COOL = (0.4, 0.8, 1.0)


def tex(asset, name, arr, recipe, notes, mode="add"):
    M.save_tex(asset, name, arr)
    TEXTURES[name] = (arr, recipe, mode)
    REPORT.setdefault(asset, {"textures": {}, "meshes": {}})["textures"][name] = {
        "file": name + ".png", "size": [arr.shape[1], arr.shape[0]], "colour_space": "linear (sRGB off)",
        "blend": mode, "channels": notes}


def mesh_report(asset, obj, **info):
    REPORT.setdefault(asset, {"textures": {}, "meshes": {}})["meshes"][obj.name] = dict(
        {"tris": M.tris(obj), "dims": [round(d, 3) for d in obj.dimensions], "uv_sets": len(obj.data.uv_layers)}, **info)


def socket(name, location):
    e = C.link(bpy.data.objects.new(name, None))
    e.empty_display_size = 0.03
    e.location = location
    return e


# ---------------------------------------------------------------------------------------------------------------
# Emissive projectiles
# ---------------------------------------------------------------------------------------------------------------

def proj_glow_texture():
    u, v = grid(64, 128)
    core = sstep(0.25, 0.92, v)
    body = (0.35 + 0.65 * v) * sstep(0.0, 0.3, v)
    rings = sum(line(v - c, 0.02, 0.025) for c in (0.36, 0.5, 0.64))
    tex("Proj_Bolt", "Proj_Glow", pack(core, body, rings, np.ones_like(u)),
        [(0, (1, 1, 1), 1.0), (1, COOL, 0.9), (2, COOL, 0.6)],
        notes="U around, V tail (0) to nose (1). R white-hot core towards the nose, G body glow fading to the tail, "
              "B three rail rings (slug only). Colour = white * R + tint * G (+ tint * B on the slug); HDR x3-4 for bloom")


def proj_bolt():
    prof = [(0.0, -0.15)] + [(0.048 * math.sin(a), -0.15 + 0.048 * (1 - math.cos(a))) for a in (0.6, 1.1, TAU / 4)]
    prof += [(0.048, 0.1)] + [(0.048 * math.cos(a), 0.1 + 0.048 * math.sin(a)) for a in (0.5, 1.0, 1.35)] + [(0.0, 0.15)]
    body = M.lathe("Proj_Bolt", prof, 8, axis="Y")
    glow = M.crossed("Proj_Bolt_Glow", 0.62, 0.2, axis="Y", planes=2, pivot=0.62)
    M.export(os.path.join(C.EXPORTS, "Proj_Bolt", "Proj_Bolt.fbx"), [body, glow])
    mesh_report("Proj_Bolt", body, pivot="centre", note="rounded bar 0.3 long, 0.096 thick, nose to Unity +Z; Proj_Glow.png")
    mesh_report("Proj_Bolt", glow, pivot="0.62 along (head over the nose)",
                note="two crossed quads, U along (head at +U) for VFX/Bolt_Core.png, additive; replaces the sphere core")
    return [body, glow]


def proj_slug():
    r = 0.034
    prof = [(0.0, -0.2), (0.016, -0.2), (r, -0.14), (r, 0.06), (r * 0.8, 0.12), (0.0, 0.21)]
    body = M.lathe("Proj_RailSlug", prof, 6, axis="Y")
    body.data.shade_flat()
    glow = M.crossed("Proj_RailSlug_Glow", 0.9, 0.16, axis="Y", planes=2, pivot=0.68)
    M.export(os.path.join(C.EXPORTS, "Proj_RailSlug", "Proj_RailSlug.fbx"), [body, glow])
    mesh_report("Proj_RailSlug", body, pivot="centre", note="hex dart 0.41 long, faceted; ../Proj_Bolt/Proj_Glow.png with B rings")
    mesh_report("Proj_RailSlug", glow, pivot="0.68 along", note="Bolt_Core.png shell, longer (rail streak)")
    return [body, glow]


def enemy_bolt_texture():
    u, v = grid(128, 128)
    n1 = M.fbm(u, v, 4, 4, seed=31)
    n2 = M.fbm(u, v, 6, 3, seed=37)
    veins = line(n1 - 0.5, 0.02, 0.05)
    body = 0.45 + 0.55 * n2
    spots = sstep(0.62, 0.75, M.fbm(u, v, 8, 2, seed=41))
    tex("Proj_EnemyBolt", "Proj_EnemyBolt", pack(veins, body, spots, np.ones_like(u)),
        [(1, ICHOR, 0.8), (0, (1, 0.95, 0.6), 1.0), (2, (0.1, 0.05, 0.0), 0.0)],
        notes="spherical UVs. R hot veins, G body glow, B dark membrane spots (subtract: col *= 1 - 0.6 B). "
              "Additive or opaque emissive; tint = the creature's bolt colour")


def enemy_bolt():
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.07)
    rng = np.random.default_rng(5)
    for vtx in bm.verts:
        p = vtx.co.copy()
        d = p.normalized()
        bump = 1 + 0.18 * math.sin(d.x * 7 + 1) * math.sin(d.z * 6) + 0.1 * rng.uniform(-1, 1)
        # Drawn out behind (Blender +Y = backwards): a glob with a tapering wake.
        back = max(0.0, d.y)
        vtx.co = Vector((p.x * bump * (1 - 0.35 * back), p.y * (1 + 1.1 * back) * bump, p.z * bump * (1 - 0.35 * back)))
    # Three barbs swept backwards: the bolt is a spat stinger, not a ball of light.
    for k in range(3):
        a = TAU * k / 3 + 0.4
        root = Vector((math.cos(a) * 0.045, -0.02, math.sin(a) * 0.045))
        tip = Vector((math.cos(a) * 0.09, 0.1, math.sin(a) * 0.09))
        ring = []
        for s in range(3):
            b = TAU * s / 3
            side = Vector((math.cos(a + math.pi / 2), 0, math.sin(a + math.pi / 2)))
            off = side * math.cos(b) * 0.018 + Vector((0, 0, 1)).cross(side).normalized() * math.sin(b) * 0.018
            ring.append(bm.verts.new(root + off))
        t = bm.verts.new(tip)
        for s in range(3):
            bm.faces.new((ring[s], ring[(s + 1) % 3], t))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new("Proj_EnemyBolt")
    bm.to_mesh(mesh)
    bm.free()
    obj = C.link(bpy.data.objects.new("Proj_EnemyBolt", mesh))
    layer = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        us = []
        for li in poly.loop_indices:
            d = mesh.vertices[mesh.loops[li].vertex_index].co.normalized()
            us.append([0.5 + math.atan2(d.x, d.z) / TAU, 0.5 + math.asin(max(-1, min(1, d.y))) / math.pi])
        # Keep a face from wrapping across the seam.
        if max(x[0] for x in us) - min(x[0] for x in us) > 0.5:
            for x in us:
                if x[0] < 0.5:
                    x[0] += 1
        for li, x in zip(poly.loop_indices, us):
            layer.data[li].uv = x
    mesh.shade_smooth()
    glow = M.crossed("Proj_EnemyBolt_Glow", 0.5, 0.26, axis="Y", planes=2, pivot=0.62)
    M.export(os.path.join(C.EXPORTS, "Proj_EnemyBolt", "Proj_EnemyBolt.fbx"), [obj, glow])
    mesh_report("Proj_EnemyBolt", obj, pivot="glob centre", note="0.14 glob, wake and three barbs trail to Unity -Z; "
                "replaces the core + halo spheres (CoreSize 0.12, HaloSize 0.26)")
    mesh_report("Proj_EnemyBolt", glow, pivot="0.62 along", note="Bolt_Core.png tinted warm, additive (the halo)")
    return [obj, glow]


def pulse_ring_texture():
    u, v = grid(256, 64)
    crest = line(v - 0.55, 0.03, 0.05)
    noise = M.fbm(u, v, 8, 3, seed=51, cells_v=2)
    body = (0.35 + 0.65 * noise) * sstep(0.05, 0.4, v) * (1 - sstep(0.75, 0.98, v))
    # Serrated barbs on the leading (outer) edge: three per tile.
    tooth = 1 - np.abs(((u * 3) % 1.0) * 2 - 1)
    barbs = (v > 0.55) * (v < 0.6 + 0.32 * tooth ** 1.5) * sstep(0.55, 0.62, v)
    tex("Proj_PulseRing", "Proj_PulseRing", pack(crest, body, barbs),
        [(0, (1, 0.85, 0.5), 1.0), (1, WARM, 0.7), (2, WARM, 0.9)],
        notes="U around (8 tiles), V inner (0) to outer (1). R crest line, G organic body glow, B serrated barbs on "
              "the outer edge. Additive or alpha (alpha = max); colour = tint; fade with the ring's age")


def pulse_ring():
    prof = [(0.9, 0.0), (0.96, 0.025), (1.0, 0.04), (1.03, 0.03), (1.07, 0.0)]
    obj = M.lathe("Proj_PulseRing", prof, 48, u_tiles=8)
    M.export(os.path.join(C.EXPORTS, "Proj_PulseRing", "Proj_PulseRing.fbx"), [obj])
    mesh_report("Proj_PulseRing", obj, pivot="centre on the ground", radius="0.9..1.07, crest at 1.0",
                note="replaces BossAbilityView.BuildRing (torus r 1, tube 0.06); scale XZ by the pulse radius, keep Y")
    return [obj]


def beam_tool():
    u, v = grid(64, 128)
    across = np.abs(np.sin(u * math.pi))       # brighter where the cone faces the camera (U around)
    fade = sstep(0.0, 0.08, v) * (1 - sstep(0.7, 1.0, v))
    core = fade * (0.5 + 0.5 * across) * (1 - 0.5 * v)
    scan = ((0.5 + 0.5 * np.sin(v * TAU * 6)) ** 8) * fade
    sparkle = sstep(0.7, 0.85, M.fbm(u, v, 8, 2, seed=61, cells_v=16)) * fade
    tex("Beam_Tool", "Beam_Tool", pack(core, scan, sparkle),
        [(0, (0.5, 0.9, 1.0), 0.8), (1, (0.8, 1.0, 1.0), 0.7), (2, (1, 1, 1), 0.5)],
        notes="U around the cone, V drone (0) to target (1). R soft cone light, G scan rings (6 per beam: scroll V "
              "towards the target), B sparkle. Additive, tint = DroneDefinition.Color")
    prof = [(0.02, 0.0), (0.35, 0.5), (0.7, 1.0)]
    obj = M.lathe("Beam_ToolCone", prof, 12, axis="Y")
    M.export(os.path.join(C.EXPORTS, "Beam_Tool", "Beam_Tool.fbx"), [obj])
    mesh_report("Beam_Tool", obj, pivot="cone tip (at the drone)",
                note="open cone 1 long along Unity +Z, mouth radius 0.7: LookRotation(target - drone), scale Z by the "
                     "distance and XY by the spot size (replaces the stretched beam cube)")
    return [obj]


def net_strand():
    u, v = grid(128, 32)
    y = v * 2 - 1
    s1 = line(y - 0.45 * np.sin(u * TAU * 2), 0.07, 0.1)
    s2 = line(y + 0.45 * np.sin(u * TAU * 2), 0.07, 0.1)
    core = np.maximum(s1, s2)
    glow = gauss(y, 0.6) * 0.5
    beads = sum(gauss(np.hypot((u - c) * 4, y), 0.12) for c in (0.0, 0.25, 0.5, 0.75, 1.0))
    tex("Net_Strand", "Net_Strand", pack(core, glow, beads),
        [(0, (0.7, 0.95, 1.0), 1.0), (1, (0.4, 0.8, 1.0), 0.5), (2, (1, 1, 1), 0.8)],
        notes="tiles in U (one tile per 0.25 m), V across. R two braided filaments, G soft glow, B beads at the "
              "crossings. Additive, tint = the net colour")
    obj = M.crossed("Net_Strand", 1.0, 0.045, axis="X", planes=2, pivot=0.5, uv_rect=(0, 0, 4, 1))
    M.export(os.path.join(C.EXPORTS, "Net_Strand", "Net_Strand.fbx"), [obj])
    mesh_report("Net_Strand", obj, pivot="centre", note="1 long along Unity X (FromToRotation(Vector3.right, b - a)), scale X "
                "by the strand length; U tiles 4 per metre (set the material tiling to length*4 or use world UVs)")
    return [obj]


def scout_trail():
    u, v = grid(256, 64)
    y = v - 0.5
    core = gauss(y, 0.06) * np.exp(-u * 2.2)
    glow = gauss(y, 0.28) * np.exp(-u * 1.2)
    rng = np.random.default_rng(71)
    sparks = np.zeros_like(u)
    for _ in range(26):
        cu, cv = rng.uniform(0.05, 0.9), rng.uniform(0.3, 0.7)
        sparks += gauss(np.hypot((u - cu) * 4, v - cv), 0.025) * (1 - cu)
    tex("Recon_Scout", "Trail_Scout", pack(core, glow, sparks),
        [(0, (1, 1, 1), 1.0), (1, (1, 0.55, 0.3), 0.8), (2, (1, 0.8, 0.6), 0.6)],
        notes="TrailRenderer, textureMode Stretch: U head (0) to tail (1), V across. R core, G glow, B sparks. "
              "Additive; tint from ReconView (ground warm, air cyan)")


# ---------------------------------------------------------------------------------------------------------------
# Lit, baked: missile, scout, debris
# ---------------------------------------------------------------------------------------------------------------

def missile_kit():
    k = tk.Kit("Proj_Missile", ["Main"])
    on = ["Main"]
    m = tk.frame((0, 0.16, 0), z=FWD, y=(0, 0, 1))
    k.lathe("Nozzle", [(0.0, 0.0), (0.026, 0.0), (0.03, 0.03), (0.0, 0.03)], m, tk.DARK, "Root", on, sides=10, bevel=0.0)
    k.lathe("Body", [(0.0, 0.03), (0.036, 0.03), (0.041, 0.05), (0.041, 0.22), (0.0, 0.22)], m,
            tk.WHITE.but(panel=0.0, bolts=False), "Root", on, sides=10, bevel=0.002)
    k.lathe("Band", [(0.0, 0.1), (0.0425, 0.1), (0.0425, 0.135), (0.0, 0.135)], m, tk.BLUE.but(panel=0.0, bolts=False),
            "Root", on, sides=10, bevel=0.0)
    k.lathe("Hazard", [(0.0, 0.2), (0.0425, 0.2), (0.0425, 0.222), (0.0, 0.222)], m, tk.HAZARD, "Root", on, sides=10,
            bevel=0.0)
    k.lathe("Nose", [(0.0, 0.222), (0.041, 0.222), (0.033, 0.27), (0.017, 0.305), (0.0, 0.305)], m, tk.GUNMETAL,
            "Root", on, sides=10, bevel=0.0)
    k.lathe("Seeker", [(0.0, 0.3), (0.018, 0.3), (0.01, 0.325), (0.0, 0.333)], m, bk.glow("azure", 1.8, lens=0.5),
            "Root", on, sides=8, bevel=0.0)
    for i in range(4):
        a = TAU * i / 4 + math.pi / 4
        radial = Vector((math.cos(a), 0, math.sin(a)))
        centre = Vector((0, 0.16 - 0.075, 0)) + radial * 0.058
        k.block("Fin", (0.006, 0.042, 0.075), tk.frame(centre, z=FWD, y=radial), tk.DARK, "Root", on,
                taper=(1.0, 0.35), shift=(0.0, -0.012), bevel=0.0)
    return k


def scout_kit():
    k = tk.Kit("Recon_Scout", ["Main"])
    on = ["Main"]
    m = tk.frame((0, 0.07, 0), z=FWD, y=(0, 0, 1))
    k.lathe("Hull", [(0.0, 0.0), (0.05, 0.005), (0.085, 0.04), (0.09, 0.09), (0.07, 0.13), (0.0, 0.13)], m,
            tk.WHITE.but(panel=0.0, bolts=False), "Root", on, sides=12, bevel=0.0)
    k.lathe("Lens", [(0.0, 0.125), (0.055, 0.125), (0.04, 0.15), (0.0, 0.158)], m, bk.glow("ice", 2.4, lens=0.8),
            "Root", on, sides=12, bevel=0.0)
    k.torus("Cowl", Vector((0, 0.07 - 0.09, 0)), FWD, 0.098, 0.012, tk.BLUE.but(panel=0.0, bolts=False), "Root", on,
            seg=16, sides=4)
    for i in range(3):
        a = TAU * i / 3 + math.pi / 2
        radial = Vector((math.cos(a), 0, math.sin(a)))
        centre = Vector((0, 0.07 - 0.03, 0)) + radial * 0.1
        k.block("Vane", (0.008, 0.06, 0.07), tk.frame(centre, z=FWD, y=radial), tk.GUNMETAL, "Root", on,
                taper=(1.0, 0.5), bevel=0.0)
    k.cyl("Emitter", (0, 0.07, 0), (0, 0.1, 0), 0.03, 0.022, bk.glow("amber", 1.6), "Root", on, sides=8, bevel=0.0)
    return k


DEBRIS = ["Debris_PlateWhite", "Debris_PlateBlue", "Debris_HexPanel", "Debris_HexHalf", "Debris_Strut",
          "Debris_Pipe", "Debris_Bracket", "Debris_Hazard", "Debris_RockA", "Debris_RockB"]
ROCK = tk.Look((0.27, 0.25, 0.23), "rubber", rough=0.9, wear=0.35, grad=0.9)


def rock_recipe(seed, size):
    def recipe():
        rng = np.random.default_rng(seed)
        bm = bmesh.new()
        for _ in range(16):
            d = rng.normal(size=3)
            d /= np.linalg.norm(d)
            r = rng.uniform(0.75, 1.0)
            bm.verts.new((d[0] * size[0] * r, d[1] * size[1] * r, d[2] * size[2] * r))
        res = bmesh.ops.convex_hull(bm, input=bm.verts[:])
        drop = {g for g in res["geom_interior"] + res["geom_unused"] if isinstance(g, bmesh.types.BMVert)}
        if drop:
            bmesh.ops.delete(bm, geom=list(drop), context="VERTS")
        return bm
    return recipe


def debris_kit():
    k = tk.Kit("Debris_Chunks", DEBRIS)
    I = tk.frame()
    white = tk.WHITE.but(bolts=False)
    # Bent plating: two plates meeting at a crease.
    k.plate("A", bk.rect_poly(0.14, 0.12, 0.02), 0.018, tk.frame((0, -0.06, 0)), white, "Root", ["Debris_PlateWhite"], bevel=0.003)
    k.plate("B", [(-0.07, -0.06), (0.07, -0.06), (0.05, 0.04), (-0.06, 0.05)], 0.018,
            tk.frame((0, 0.035, 0.03), rot=(-28, 0, 0)), white, "Root", ["Debris_PlateWhite"], bevel=0.003)
    k.plate("P", [(-0.09, -0.05), (0.08, -0.07), (0.1, 0.03), (0.02, 0.07), (-0.08, 0.04)], 0.02, I,
            tk.BLUE, "Root", ["Debris_PlateBlue"], bevel=0.003)
    k.block("Rib", (0.16, 0.014, 0.012), tk.frame((0, -0.01, 0.014)), tk.GUNMETAL, "Root", ["Debris_PlateBlue"], bevel=0.0)
    k.plate("Hex", bk.hex_poly(0.085), 0.022, I, tk.GUNMETAL, "Root", ["Debris_HexPanel"], bevel=0.003)
    k.plate("HexGlow", bk.hex_poly(0.055), 0.026, I, bk.glow("azure", 1.4), "Root", ["Debris_HexPanel"], bevel=0.0)
    half = bk._clip_poly(bk.hex_poly(0.09), (0.3, 1.0), 0.01)
    k.plate("HexHalf", half, 0.022, I, bk.burn(tk.GUNMETAL), "Root", ["Debris_HexHalf"], bevel=0.003)
    k.block("Ember", (0.12, 0.01, 0.026), tk.frame((0.0, 0.012, 0.0), rot=(0, 0, -16.7)), bk.glow("ember", 1.6), "Root",
            ["Debris_HexHalf"], bevel=0.0)
    for name, size, pos in (("Web", (0.012, 0.2, 0.06), (0, 0, 0)), ("FlangeA", (0.05, 0.2, 0.01), (0, 0, 0.03)),
                            ("FlangeB", (0.05, 0.2, 0.01), (0, 0, -0.03))):
        k.block(name, size, tk.frame(pos), tk.GUNMETAL, "Root", ["Debris_Strut"], bevel=0.002)
    k.barrel("Pipe", (0, 0.09, 0), (0, -0.09, 0), 0.03, 0.02, tk.STEEL, "Root", ["Debris_Pipe"], sides=8, depth=0.05)
    k.torus("PipeRing", Vector((0, 0.04, 0)), (0, 1, 0), 0.031, 0.006, tk.DARK, "Root", ["Debris_Pipe"], seg=8, sides=3)
    k.block("Arm1", (0.12, 0.04, 0.014), tk.frame((0, 0, 0)), tk.DARK, "Root", ["Debris_Bracket"], bevel=0.002)
    k.block("Arm2", (0.014, 0.04, 0.09), tk.frame((0.053, 0, 0.038)), tk.DARK, "Root", ["Debris_Bracket"], bevel=0.002)
    k.cyl("Bolt", (-0.03, 0, 0.007), (-0.03, 0, 0.016), 0.008, look=tk.STEEL, bone="Root", on=["Debris_Bracket"], sides=6, bevel=0.0)
    k.block("Haz", (0.12, 0.08, 0.05), I, tk.HAZARD, "Root", ["Debris_Hazard"], taper=(0.85, 0.8), bevel=0.004)
    k._add("RockA", rock_recipe(3, (0.08, 0.06, 0.05)), 0.0, I, ROCK, "Root", ["Debris_RockA"])
    k._add("RockB", rock_recipe(9, (0.06, 0.09, 0.04)), 0.0, I, ROCK.but(paint=(0.2, 0.19, 0.18)), "Root", ["Debris_RockB"])
    return k


def baked(kit, size, low, bake):
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, size, low, bake, samples=16)
    return mat, report


def build_missile(bake):
    kit = missile_kit()
    mat, rep = baked(kit, 256, 128, bake)
    obj = bk.module_object(kit.of("Main"), "Proj_Missile", Vector((0, 0, 0)), mat)
    socks = [socket("Socket_Exhaust", (0, 0.16, 0)), socket("Socket_Nose", (0, -0.173, 0))]
    M.export(os.path.join(C.EXPORTS, "Proj_Missile", "Proj_Missile.fbx"), [obj] + socks)
    REPORT.setdefault("Proj_Missile", {"textures": {}, "meshes": {}})["bake"] = rep
    mesh_report("Proj_Missile", obj, pivot="centre of mass, nose to Unity +Z", sockets={"Socket_Exhaust": "tail (flame, smoke "
                "trail, Rocket_Exhaust.png)", "Socket_Nose": "seeker tip"}, note="replaces BuildMissile (capsule, sphere, 3 cubes)")
    return [obj] + socks, mat


def build_scout(bake):
    kit = scout_kit()
    mat, rep = baked(kit, 256, 128, bake)
    obj = bk.module_object(kit.of("Main"), "Recon_Scout", Vector((0, 0, 0)), mat)
    socks = [socket("Socket_Trail", (0, 0.17, 0)), socket("Socket_Light", (0, -0.09, 0))]
    M.export(os.path.join(C.EXPORTS, "Recon_Scout", "Recon_Scout.fbx"), [obj] + socks)
    REPORT.setdefault("Recon_Scout", {"textures": {}, "meshes": {}})["bake"] = rep
    mesh_report("Recon_Scout", obj, pivot="centre, lens to Unity +Z (the route direction)",
                sockets={"Socket_Trail": "trail origin (behind)", "Socket_Light": "point light / lens"},
                note="replaces the 0.9 glowing sphere; keep a small point light at Socket_Light; ~0.3 across")
    return [obj] + socks, mat


def build_debris(bake):
    kit = debris_kit()
    mat, rep = baked(kit, 512, 256, bake)
    objs = [bk.module_object(kit.of(v), v, Vector((0, 0, 0)), mat) for v in DEBRIS]
    M.export(os.path.join(C.EXPORTS, "Debris_Chunks", "Debris_Chunks.fbx"), objs)
    REPORT.setdefault("Debris_Chunks", {"textures": {}, "meshes": {}})["bake"] = rep
    for o in objs:
        hot = o.name in ("Debris_HexPanel", "Debris_HexHalf")
        mesh_report("Debris_Chunks", o, pivot="centre", hot=hot,
                    note="rock bit for ground hits" if "Rock" in o.name else "colony plating")
    return objs, mat


def write_reports():
    for asset, data in REPORT.items():
        data["round"] = 25
        with open(os.path.join(C.EXPORTS, asset, asset + "_report.json"), "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

def vfx(name):
    return os.path.join(C.EXPORTS, "VFX", name + ".png")


def additive_from(path_or_arr, recipe, name):
    arr = C.image_to_array(path_or_arr) if isinstance(path_or_arr, str) else path_or_arr
    return M.fx_material(name, M.preview_image(name + "_img", M.tinted(arr, recipe)), "add", 1.0)


def preview_materials():
    """Preview look per emissive mesh (what the Unity shaders should do)."""
    mats = {}
    glow = TEXTURES["Proj_Glow"][0]
    mats["bolt"] = additive_from(glow, [(0, (1, 1, 1), 3.0), (1, COOL, 2.5)], "PrevBolt")
    mats["slug"] = additive_from(glow, [(0, (1, 1, 1), 3.0), (1, (0.6, 0.85, 1.0), 2.0), (2, COOL, 3.0)], "PrevSlug")
    core = vfx("Bolt_Core")
    if os.path.exists(core):
        mats["shell_cool"] = additive_from(core, [(1, COOL, 1.4), (0, (1, 1, 1), 0.6)], "PrevShellCool")
        mats["shell_warm"] = additive_from(core, [(1, WARM, 1.4), (0, (1, 0.9, 0.6), 0.5)], "PrevShellWarm")
    eb = TEXTURES["Proj_EnemyBolt"][0]
    mats["enemy"] = additive_from(eb, [(1, (0.9, 0.4, 0.08), 0.9), (0, (1.0, 0.85, 0.3), 1.6)], "PrevEnemy")
    mats["pulse"] = additive_from(TEXTURES["Proj_PulseRing"][0], [(0, (1, 0.8, 0.5), 2.0), (1, WARM, 1.2), (2, WARM, 1.6)], "PrevPulse")
    mats["beam"] = additive_from(TEXTURES["Beam_Tool"][0], [(0, (0.5, 0.9, 1.0), 0.45), (1, (0.8, 1, 1), 0.6), (2, (1, 1, 1), 0.5)], "PrevBeam")
    mats["strand"] = additive_from(TEXTURES["Net_Strand"][0], [(0, (0.7, 0.95, 1.0), 2.0), (1, (0.4, 0.8, 1.0), 0.7), (2, (1, 1, 1), 1.5)], "PrevStrand")
    return mats


def previews(built, out):
    mats = preview_materials()
    C.preview_world()
    for group in built.values():
        for o in group:
            o.hide_render = True
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []

    def put(obj, loc, mat=None, rot=None, scale=None):
        o = obj.copy()
        if obj.data is not None:
            o.data = obj.data.copy()
        C.link(o)
        o.hide_render = False
        o.location = loc
        if rot:
            o.rotation_euler = rot
        if scale:
            o.scale = scale
        if mat is not None:
            M.assign(o, mat)
        placed.append(o)
        return o

    b = built
    put(b["bolt"][0], (-2.4, 0, 0.5), mats["bolt"])
    if "shell_cool" in mats:
        put(b["bolt"][1], (-2.4, 0, 0.5), mats["shell_cool"])
    put(b["slug"][0], (-1.6, 0, 0.5), mats["slug"])
    if "shell_cool" in mats:
        put(b["slug"][1], (-1.6, 0, 0.5), mats["shell_cool"])
    put(b["missile"][0], (-0.7, 0, 0.5))
    put(b["enemy"][0], (0.2, 0, 0.5), mats["enemy"])
    if "shell_warm" in mats:
        put(b["enemy"][1], (0.2, 0, 0.5), mats["shell_warm"])
    put(b["scout"][0], (1.1, 0, 0.5))
    put(b["pulse"][0], (2.8, 0.2, 0.01), mats["pulse"], scale=(0.8, 0.8, 1))
    put(b["beam"][0], (1.9, 0.6, 1.0), mats["beam"], rot=(math.radians(-70), 0, 0), scale=(0.25, 1.0, 0.25))
    for i in range(3):
        put(b["strand"][0], (-0.3 + i * 0.0, 1.2, 0.25 + i * 0.12), mats["strand"], scale=(1.6, 1, 1))
    for i, o in enumerate(b["debris"]):
        put(o, (-1.35 + i * 0.3, -1.1, 0.06), rot=(0.3 * i, 0.5, 0.7 * i), scale=(1.4, 1.4, 1.4))
    bk.render_game(Vector((0, 0, 0.3)), os.path.join(out, "projectiles_game.png"), zoom=1.6)
    bk.render_game(Vector((-0.6, 0, 0.4)), os.path.join(out, "projectiles_zoom.png"), zoom=5.0)
    bk.render_close(Vector((-0.1, -1.1, 0.06)), os.path.join(out, "debris_close.png"), 3.6, azimuth=10, elevation=40,
                    lens=45, light_scale=0.6)
    bk.render_close(Vector((-0.6, 0, 0.5)), os.path.join(out, "projectiles_close.png"), 3.0, azimuth=15, elevation=35,
                    lens=55, light_scale=0.6)
    bk.remove(placed + stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    bake = not args.get("nobake")
    tk.metal_detail_map()
    proj_glow_texture()
    enemy_bolt_texture()
    pulse_ring_texture()
    scout_trail()
    built = {"bolt": proj_bolt(), "slug": proj_slug(), "enemy": enemy_bolt(), "pulse": pulse_ring(),
             "beam": beam_tool(), "strand": net_strand()}
    built["missile"], _ = build_missile(bake)
    built["scout"], _ = build_scout(bake)
    built["debris"], _ = build_debris(bake)
    write_reports()
    C.save_blend("Projectiles")
    if not args.get("norender"):
        previews(built, out)


if __name__ == "__main__":
    main()
