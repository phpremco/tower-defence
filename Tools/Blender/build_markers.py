"""Round 25: gameplay markers, decals, health bars and particle sprites (replaces code-made primitives and
generated textures; see ArtSource/README.md, round 25, for the Unity notes per asset).

Assets (each in Exports/<Asset>/ with a half-size Low/ copy of every texture):
  Decal_GlowPool         Decal_GlowPool.png (radial pool), Glow_Blade.png (sword blade glow), Field_Energy.png
                         (gate energy field), Decal_Quad.fbx (Quad_Flat, Quad_Upright: shared unit quads)
  Decal_BlobShadow       Decal_BlobShadow.png (flyer ground shadow)
  Marker_PlacementCursor Marker_PlacementCursor.png + Marker_FreeCell.png, Marker_PlacementCursor.fbx
  Marker_RangeRing       Marker_RangeRing.png (band) + Marker_RangeFill.png, Marker_RangeRing.fbx
  Marker_HeroSpot        Marker_HeroSpot.png, Marker_HeroSpot.fbx
  Marker_HeroStand       Marker_HeroRing.png, Marker_Beacon.png, Marker_Emblem.png, Marker_HeroStand.fbx
  Marker_SlowField       Marker_SlowField.png, Marker_SlowField.fbx
  Marker_RoadCue         Cue_Trail.png, Cue_Dash.png, Cue_Pulse.png, Marker_RoadCue.fbx
  UI_HealthBar           UI_HealthBar.png (atlas), UI_HealthBar.fbx
  Particles_Atlas        Particles_Atlas.png (4 x 2 sprites)

Run: blender --background --factory-startup --python Tools/Blender/build_markers.py -- [--preview <dir>]
     [--norender] [--noscene]
The game-camera scene imports SpawnGate_Ground/Air (build_spawngate.py) and the projectiles (build_projectiles.py)
when their exports exist, so run those two first for the full scene.
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import marker_kit as M  # noqa: E402
from marker_kit import centred, gauss, grid, line, pack, sstep  # noqa: E402

TAU = 2 * math.pi
# Preview tints (the game's own colours: PlacementController, HeroView trim, theme spawn colour).
VALID = (0.3, 1.0, 0.8)
INVALID = (1.0, 0.3, 0.25)
HERO = (1.0, 0.8, 0.35)
COOL = (0.35, 0.75, 1.0)
WARM = (1.0, 0.45, 0.18)
SLOW = (0.45, 0.6, 1.0)

TEXTURES = {}     # name -> (asset, array, preview recipe, mode, notes)
REPORT = {}


def tex(asset, name, arr, recipe, mode="add", notes=""):
    path = M.save_tex(asset, name, arr)
    TEXTURES[name] = (asset, arr, recipe, mode, notes)
    REPORT.setdefault(asset, {"textures": {}, "meshes": {}})["textures"][name] = {
        "file": os.path.basename(path), "size": [arr.shape[1], arr.shape[0]], "colour_space": "linear (sRGB off)",
        "blend": mode, "channels": notes}
    return path


def mesh_report(asset, obj, **info):
    REPORT.setdefault(asset, {"textures": {}, "meshes": {}})["meshes"][obj.name] = dict(
        {"tris": M.tris(obj), "dims": [round(d, 3) for d in obj.dimensions], "uv_sets": len(obj.data.uv_layers)}, **info)


# ---------------------------------------------------------------------------------------------------------------
# Decals: glow pool, blade, energy field, blob shadow
# ---------------------------------------------------------------------------------------------------------------

def glow_pool():
    x, y, r = centred(128, 128)
    pool = np.clip(1 - r, 0, 1) ** 2.2
    soft = gauss(r, 0.5) * (1 - sstep(0.85, 1.0, r))
    ring = line(r - 0.74, 0.02, 0.07) * (1 - sstep(0.9, 1.0, r))
    tex("Decal_GlowPool", "Decal_GlowPool", pack(pool, soft, ring, pool), [(0, COOL, 1.0), (1, COOL, 0.25)],
        notes="R pool (1-r)^2.2 as GlowDecal.Radial, G wider soft falloff, B thin ring at r 0.74 (pulse), A = R")

    u, v = grid(128, 128)
    ends = np.clip(v / 0.08, 0, 1) * np.clip((1 - v) / 0.2, 0, 1)
    blade = (1 - u) ** 1.6 * ends
    core = gauss(u, 0.14) * ends
    streak = M.fbm(u, v, 2, 3, seed=5, cells_v=6)
    streak = sstep(0.45, 0.8, streak) * (1 - u) * ends
    tex("Decal_GlowPool", "Glow_Blade", pack(blade, core, streak, blade), [(0, (0.5, 0.85, 1.0), 1.0), (1, (1, 1, 1), 0.6)],
        notes="U core (0) to rim (1), V base (0) to tip (1) as GlowDecal.Blade; R glow, G hot core line, B streaks (scroll V)")

    u, v = grid(128, 128)
    across = u * 2 - 1
    edge = 1 - np.abs(across) ** 4
    scan = 0.35 + 0.65 * (0.5 + 0.5 * np.sin(v * TAU * 6)) ** 3
    # Hex lattice (4 cells across, tiling in V): distance to the nearest cell centre edge.
    hx, hy = u * 4, v * 4 * 1.1547
    rows = np.floor(hy)
    hx2 = hx + 0.5 * (rows % 2)
    cx, cy = hx2 - np.floor(hx2) - 0.5, hy - rows - 0.5
    hexd = np.maximum(np.abs(cx) * 1.0, np.abs(cx) * 0.5 + np.abs(cy) * 0.866)
    hexes = line(hexd - 0.46, 0.012, 0.03)
    shimmer = M.fbm(u, v, 3, 3, seed=9)
    tex("Decal_GlowPool", "Field_Energy", pack(scan * edge, hexes * edge, shimmer * edge, edge),
        [(0, (0.55, 0.75, 1.0), 0.6), (1, (0.7, 0.9, 1.0), 0.5), (2, (0.55, 0.75, 1.0), 0.2)],
        notes="tiles in V (scroll it); U across the sheet. R scanlines (6 per tile, as GlowDecal.Scanlines), G hex "
              "lattice, B shimmer noise; every channel already faded at the U edges; A edge fade")

    x, y, r = centred(128, 128)
    density = (1 - sstep(0.3, 1.0, r)) ** 1.4
    core = gauss(r, 0.35)
    arr = np.dstack([core, np.zeros_like(r), np.zeros_like(r), np.clip(density * 0.9 + core * 0.1, 0, 1)]).astype(np.float32)
    tex("Decal_BlobShadow", "Decal_BlobShadow", arr, [("alpha", 3, 0.75), (0, (0, 0, 0), 0.0)], mode="alpha",
        notes="alpha blended, black: A density (soft edge), R tighter core (shrink the shadow as the flyer climbs: "
              "alpha = lerp(A, R, height01))")


def decal_quads():
    flat = M.quad("Quad_Flat", 1, 1, "XY")
    up = M.quad("Quad_Upright", 1, 1, "XZ")
    M.export(os.path.join(C.EXPORTS, "Decal_GlowPool", "Decal_Quad.fbx"), [flat, up])
    mesh_report("Decal_GlowPool", flat, pivot="centre", note="flat on the ground (Unity XZ, normal +Y); scale = diameter")
    mesh_report("Decal_GlowPool", up, pivot="centre", note="upright, faces Unity -Z (camera side); for the gate field sheet")
    return [flat, up]


# ---------------------------------------------------------------------------------------------------------------
# Placement cursor, free cell, range ring, hero spot and stand, slow field
# ---------------------------------------------------------------------------------------------------------------

CURSOR_SPAN = 1.5   # the cursor quad covers 1.5 cells: the cell plus a soft halo


def cursor():
    x, y, r = centred(256, 256)
    half = 0.96 / CURSOR_SPAN
    sd = M.rounded_rect(x, y, half, half, 0.1)
    corner = np.minimum(np.abs(x), np.abs(y)) / half
    edge = line(sd + 0.012, 0.008, 0.014) * (0.28 + 0.72 * sstep(0.45, 0.9, corner))
    inside = 1 - sstep(-0.01, 0.005, sd)
    fill = inside * (0.18 + 0.82 * sstep(-0.3, 0.0, sd)) * 0.55
    halo = np.exp(-np.maximum(sd, 0) / 0.07) * (1 - inside) * (1 - sstep(0.85, 1.0, np.maximum(np.abs(x), np.abs(y))))
    tex("Marker_PlacementCursor", "Marker_PlacementCursor", pack(edge, fill, halo * 0.6),
        [(0, VALID, 1.3), (1, VALID, 0.35), (2, VALID, 0.4)],
        notes="quad = 1.5 cells (cell edge at 0.96 cell, as MarkerCellFraction). R edge line, brightest in the "
              "corners; G inner fill (stronger near the edge); B soft halo outside the cell; A max")
    # Free cell near the cursor (2026-11-04 wish): a faint rounded tile, no hard frame.
    x, y, r = centred(128, 128)
    sd = M.rounded_rect(x, y, 0.8, 0.8, 0.55)
    spot = (1 - sstep(-0.45, 0.1, sd)) * 0.4
    rim = line(sd + 0.12, 0.03, 0.12) * 0.35
    tex("Marker_PlacementCursor", "Marker_FreeCell", pack(spot, rim, np.zeros_like(r)),
        [(0, VALID, 0.25), (1, VALID, 0.25)],
        notes="one cell. R soft rounded glow (no hard frame), G a very faint inner rim; fade per cell by distance to "
              "the cursor (only while placing)")
    q = M.quad("Marker_PlacementCursor", CURSOR_SPAN, CURSOR_SPAN, "XY")
    f = M.quad("Marker_FreeCell", 1.0, 1.0, "XY")
    M.export(os.path.join(C.EXPORTS, "Marker_PlacementCursor", "Marker_PlacementCursor.fbx"), [q, f])
    mesh_report("Marker_PlacementCursor", q, pivot="cell centre", note="1.5 x 1.5 at cell size 1; scale by CellSize")
    mesh_report("Marker_PlacementCursor", f, pivot="cell centre", note="1 x 1 at cell size 1")
    return [q, f]


RING_INNER = 0.93


def range_ring():
    u, v = grid(256, 64)
    rim_v = 0.8
    rim = line(v - rim_v, 0.035, 0.05)
    # Starts at 0.22 on the inner edge, the value Marker_RangeFill ends with, so fill and band meet without a seam.
    inner = (0.22 + 0.3 * sstep(0.0, rim_v, v) ** 2) * (1 - sstep(rim_v - 0.02, rim_v + 0.02, v))
    outer = np.exp(-np.maximum(v - rim_v, 0) / 0.06) * (v > rim_v)
    glow = np.maximum(inner, outer * 0.7)
    dash = sstep(0.0, 0.08, u) * (1 - sstep(0.42, 0.5, u)) * gauss(v - rim_v, 0.16)
    tex("Marker_RangeRing", "Marker_RangeRing", pack(rim, glow, dash),
        [(0, VALID, 1.2), (1, VALID, 0.45), (2, VALID, 0.3)],
        notes="band: U around (24 tiles per turn on the mesh), V inner edge (0) to outer edge (1). R crisp rim at "
              "V 0.8, G inner glow (0.22 at V 0, continuing Marker_RangeFill) and outer falloff, B one dash per tile (scroll U slowly for a turning dash)")
    x, y, r = centred(128, 128)
    # Texture r 1.0 = the disc rim (RING_INNER on the mesh): a narrow glow that meets the band, clear inside.
    fill = sstep(0.8, 0.99, r) ** 2 * (1 - sstep(0.985, 1.0, r))
    flat = (1 - sstep(0.97, 1.0, r)) * 0.01
    tex("Marker_RangeRing", "Marker_RangeFill", pack(fill * 0.22, flat),
        [(0, VALID, 0.35), (1, VALID, 0.35)],
        notes="disc under the band: R soft glow in the outer quarter, rising to 0.22 at the disc rim = the band's inner edge (use "
              "the same gain as the band's G so they meet without a seam), G a barely visible flat fill")
    band = M.annulus("RangeRing_Band", RING_INNER, 1.0, 96, u_tiles=24)
    fill_disc = M.disc("RangeRing_Fill", RING_INNER, 48, z=-0.002)
    M.export(os.path.join(C.EXPORTS, "Marker_RangeRing", "Marker_RangeRing.fbx"), [band, fill_disc])
    mesh_report("Marker_RangeRing", band, pivot="ring centre", radius="0.93..1.0 (unit range)",
                note="scale XZ by the range; UV2.y 0 inner / 1 outer for a constant-width shader")
    mesh_report("Marker_RangeRing", fill_disc, pivot="ring centre", radius=RING_INNER)
    return [band, fill_disc]


def hero_spot():
    u, v = grid(256, 128)
    x, y = u * 2 - 1, v * 2 - 1
    ends = 1 - sstep(0.82, 1.0, np.abs(x))
    rails = (line(np.abs(y) - 0.72, 0.018, 0.05)) * ends * 0.8
    fill = (0.15 + 0.85 * sstep(0.2, 0.72, np.abs(y))) * (np.abs(y) < 0.74) * ends * 0.45
    # A small open diamond ring in the middle: the hero's crest, not a direction arrow.
    dia = np.abs(x) * 2.0 + np.abs(y)
    crest = line(dia - 0.42, 0.025, 0.05) + gauss(dia, 0.1) * 0.6
    tex("Marker_HeroSpot", "Marker_HeroSpot", pack(rails, fill, crest),
        [(0, HERO, 1.1), (1, HERO, 0.3), (2, HERO, 0.8)],
        notes="U across the road (spot width), V along the road. R two rails, G soft fill towards the rails, "
              "B centre crest (small open diamond); pulse B slowly")
    q = M.quad("Marker_HeroSpot", 1.0, 1.0, "XY")
    M.export(os.path.join(C.EXPORTS, "Marker_HeroSpot", "Marker_HeroSpot.fbx"), [q])
    mesh_report("Marker_HeroSpot", q, pivot="centre", note="unit quad: scale X = cells*(width-SpotMargin), "
                "Z = cell*(1-SpotMargin); local X across the road (code: LookRotation(along))")
    return [q]


def hero_stand():
    u, v = grid(256, 32)
    gap = 0.07
    seg = sstep(gap, gap + 0.04, u) * (1 - sstep(1 - 0.04, 1.0, u))
    ring = line(v - 0.5, 0.18, 0.12) * seg
    glow = gauss(v - 0.5, 0.42) * (0.4 + 0.6 * seg)
    ticks = gauss(u - (gap + 0.06), 0.012) * gauss(v - 0.5, 0.3) + gauss(u - 0.97, 0.012) * gauss(v - 0.5, 0.3)
    tex("Marker_HeroStand", "Marker_HeroRing", pack(ring, glow * 0.6, ticks),
        [(0, HERO, 1.3), (1, HERO, 0.4), (2, (1, 1, 1), 0.6)],
        notes="band: U around (3 tiles = three arcs with gaps), V across. R ring line, G glow, B bright arc-end ticks")
    u, v = grid(64, 256)
    x = u * 2 - 1
    fade = sstep(0.0, 0.15, v) * (1 - sstep(0.55, 1.0, v))
    core = gauss(x, 0.12) * fade
    halo = gauss(x, 0.45) * fade
    bands = ((0.5 + 0.5 * np.sin(v * TAU * 5)) ** 6) * gauss(x, 0.5)
    tex("Marker_HeroStand", "Marker_Beacon", pack(core, halo * 0.6, bands, np.maximum(core, halo)),
        [(0, HERO, 1.2), (1, HERO, 0.45), (2, HERO, 0.25)],
        notes="crossed quads: U across, V bottom (0) to top (1). R core column, G halo, B rising bands (tiles in V; "
              "sample B a second time with V scrolled)")
    x, y, r = centred(64, 64)
    tex("Marker_HeroStand", "Marker_Emblem", pack(1 - r * 0.6, line(r - 0.8, 0.08, 0.1), np.zeros_like(r), np.ones_like(r)),
        [(0, HERO, 1.0), (1, (1, 1, 1), 0.5)],
        notes="emblem mesh (planar UVs from the front): R centre-bright facet ramp, G rim; opaque emissive")
    ring = M.annulus("HeroStand_Ring", 0.47, 0.53, 48, u_tiles=3)
    # Beacon: three crossed upright quads, bottom at the origin (place at BeaconBottom), 1.3 high, 0.22 wide.
    beacon_parts = []
    for i in range(3):
        a = math.pi * i / 3
        side = Vector((math.cos(a), math.sin(a), 0)) * 0.11
        verts = [-side, side, side + Vector((0, 0, 1.3)), -side + Vector((0, 0, 1.3))]
        beacon_parts.append(M.mesh_object(f"_b{i}", verts, [(0, 1, 2, 3)], [[(0, 0), (1, 0), (1, 1), (0, 1)]]))
    beacon = M.join(beacon_parts, "HeroStand_Beacon")
    emblem = emblem_mesh()
    M.export(os.path.join(C.EXPORTS, "Marker_HeroStand", "Marker_HeroStand.fbx"), [ring, beacon, emblem])
    mesh_report("Marker_HeroStand", ring, pivot="hero feet", radius="0.47..0.53 (StandRingRadius 0.5)")
    mesh_report("Marker_HeroStand", beacon, pivot="bottom centre", note="place at BeaconBottom 1.7; 1.3 high = BeaconTop - BeaconBottom")
    mesh_report("Marker_HeroStand", emblem, pivot="centre", note="replaces the 45/45 rotated emblem cube; spin about Y in code")
    return [ring, beacon, emblem]


def emblem_mesh():
    """The hero's crest: a tall faceted diamond (8 facets) with a slim halo ring around its waist."""
    w, h, d = 0.1, 0.16, 0.045
    verts = [(0, 0, h), (0, 0, -h * 0.75), (w, 0, 0.02), (0, -d, 0.02), (-w, 0, 0.02), (0, d, 0.02)]
    faces = [(0, 2, 3), (0, 3, 4), (0, 4, 5), (0, 5, 2), (1, 3, 2), (1, 4, 3), (1, 5, 4), (1, 2, 5)]

    def uv(i):
        p = verts[i]
        return (0.5 + p[0] / (2 * w) * 0.9, 0.5 + p[2] / (2 * h) * 0.9)
    gem = M.mesh_object("_gem", verts, faces, [[uv(i) for i in f] for f in faces])
    rv, rf, ru = [], [], []
    seg, sides, R, t = 16, 4, 0.14, 0.008
    for i in range(seg):
        a = TAU * i / seg
        for j in range(sides):
            b = TAU * j / sides
            rr = R + t * math.cos(b)
            rv.append((math.cos(a) * rr, math.sin(a) * rr * 0.55, t * math.sin(b) + 0.02))
    for i in range(seg):
        for j in range(sides):
            a0 = i * sides + j
            a1 = i * sides + (j + 1) % sides
            b0 = ((i + 1) % seg) * sides + j
            b1 = ((i + 1) % seg) * sides + (j + 1) % sides
            rf.append((a0, b0, b1, a1))
            ru.append([(0.5, 0.06)] * 4)
    halo = M.mesh_object("_halo", rv, rf, ru)
    obj = M.join([gem, halo], "HeroStand_Emblem")
    return obj


def slow_field():
    x, y, r = centred(256, 256)
    rim = line(r - 0.955, 0.012, 0.025)
    ang = np.arctan2(y, x)
    wob = 0.025 * np.sin(ang * 5 + 1.0) + 0.018 * np.sin(ang * 9 + 2.0)
    ripples = sum(line(r + wob * k - k, 0.006, 0.04) * w for k, w in ((0.42, 0.25), (0.7, 0.4)))
    fill = (0.12 + 0.88 * r ** 3) * (1 - sstep(0.95, 0.975, r)) * 0.5
    tex("Marker_SlowField", "Marker_SlowField", pack(rim, ripples, fill),
        [(0, SLOW, 1.2), (1, SLOW, 0.5), (2, SLOW, 0.4)],
        notes="disc, planar UVs. R rim at r 0.955, G two faint inner ripples (drive them outwards with "
              "uv' = 0.5 + (uv-0.5) * frac(-time*0.3) on a second sample; they wobble, so no target look), B soft "
              "fill that rises to the rim")
    d = M.disc("Marker_SlowField", 1.0, 40)
    M.export(os.path.join(C.EXPORTS, "Marker_SlowField", "Marker_SlowField.fbx"), [d])
    mesh_report("Marker_SlowField", d, pivot="centre", radius=1.0, note="scale XZ by definition.Radius; place 0.05 up")
    return [d]


# ---------------------------------------------------------------------------------------------------------------
# Road and flight cues: flowing light, never chevrons
# ---------------------------------------------------------------------------------------------------------------

def _wrap(d):
    return (d + 0.5) % 1.0 - 0.5


def road_cues():
    u, v = grid(256, 64)
    s_ = _wrap(u - 0.75)
    comet = np.where(s_ > 0, gauss(s_, 0.05), np.exp(s_ / 0.32)) * (s_ > -0.5)
    across = v - 0.5
    core = comet * gauss(across, 0.07 + 0.05 * np.clip(-s_, 0, 1))
    guide = gauss(across, 0.05) * 0.12
    glow = comet * gauss(across, 0.3) * 0.4 + guide
    rng = np.random.default_rng(7)
    motes = np.zeros_like(u)
    for _ in range(14):
        cu, cv = rng.uniform(0, 1), rng.uniform(0.25, 0.75)
        dd = np.hypot(_wrap(u - cu) * 4, v - cv)
        motes += gauss(dd, 0.03) * rng.uniform(0.4, 1.0)
    motes *= np.clip(comet * 1.5, 0, 1)
    tex("Marker_RoadCue", "Cue_Trail", pack(core, glow, motes),
        [(0, WARM, 1.1), (1, WARM, 0.5), (2, (1, 0.8, 0.5), 0.5)],
        notes="road light trail: U along the travel direction (tiles; the head points to +U), V across. R one long "
              "soft light streak per tile, G its glow plus a faint continuous guide line, B sparkle motes in the tail. "
              "Scroll: uv.x -= time * speed so the pulses run towards the base")
    u, v = grid(256, 32)
    beads = np.zeros_like(u)
    for head in (0.125, 0.375, 0.625, 0.875):
        s = _wrap(u - head)
        beads += np.where(s > 0, gauss(s, 0.012), np.exp(s / 0.05)) * (np.abs(s) < 0.2)
    across = v - 0.5
    tex("Marker_RoadCue", "Cue_Dash", pack(beads * gauss(across, 0.16), beads * gauss(across, 0.38) * 0.5 + gauss(across, 0.05) * 0.15),
        [(0, (0.55, 0.95, 0.95), 1.0), (1, (0.55, 0.95, 0.95), 0.6)],
        notes="flight route: four light beads with short tails per tile (heads to +U), faint guide line in G; "
              "scroll like Cue_Trail. Replaces the dashes and the arrowheads")
    x, y, r = centred(128, 128)
    ring = gauss(r - 0.78, 0.07) * (1 - sstep(0.92, 1.0, r))
    dot = gauss(r, 0.12)
    tex("Marker_RoadCue", "Cue_Pulse", pack(ring, dot, np.zeros_like(r)),
        [(0, WARM, 1.0), (1, WARM, 0.8)],
        notes="pulse ring: R soft ring at r 0.78, G centre dot. Scale the quad 0.4 -> 3 and fade alpha for an "
              "expanding pulse (spawn mouths, recon rings, route start)")
    strip = M.quad("RoadCue_Strip", 0.42, 1.0, "XY", segments=1)
    # One pulse every three cells: the strip covers a third of a texture tile.
    # The quad's U runs along Blender X; turn it so U runs along Blender -Y (Unity +Z, the travel direction).
    rotate_uv_forward(strip, 1.0 / 3.0)
    fstrip = M.quad("FlightCue_Strip", 0.16, 1.0, "XY")
    rotate_uv_forward(fstrip)
    M.export(os.path.join(C.EXPORTS, "Marker_RoadCue", "Marker_RoadCue.fbx"), [strip, fstrip])
    mesh_report("Marker_RoadCue", strip, pivot="centre", note="1 unit long along Unity +Z (travel), 0.42 wide; U along. "
                "One per road cell (Road.CentreOf, LookRotation(forward)), or tile Cue_Trail on a LineRenderer ribbon")
    mesh_report("Marker_RoadCue", fstrip, pivot="centre", note="1 unit long along Unity +Z, 0.16 wide; scale Z by the leg length")
    return [strip, fstrip]


def rotate_uv_forward(obj, u_span=1.0):
    """Lays a quad's long side along Blender Y and maps U along -Y (Unity +Z) with V across X."""
    mesh = obj.data
    xs = [v.co.x for v in mesh.vertices]
    ys = [v.co.y for v in mesh.vertices]
    w = (max(xs) - min(xs)) or 1.0
    length = (max(ys) - min(ys)) or 1.0
    uv = mesh.uv_layers["UVMap"].data
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            uv[li].uv = ((0.5 - co.y / length) * u_span, 0.5 + co.x / w)
    mesh.update()


# ---------------------------------------------------------------------------------------------------------------
# Health and armour bar
# ---------------------------------------------------------------------------------------------------------------

BAR_W, BAR_H = 0.85, 0.085          # EnemyView BarWidth / BarHeight (2026-11-01 night)
ARMOUR_H = BAR_H * 0.7
ARMOUR_Y = BAR_H * 1.6
PLATE_W, PLATE_H = BAR_W * 1.1, BAR_H * 3.2
PLATE_Y = BAR_H * 0.8
# Atlas rows (V, bottom-up): armour 0..0.25, fill 0.25..0.5, plate 0.5..1.
ROW_ARMOUR, ROW_FILL, ROW_PLATE = (0.0, 0.25), (0.25, 0.5), (0.5, 1.0)


def health_bar():
    W, H = 256, 64
    atlas = np.zeros((H, W, 4), dtype=np.float32)
    # Plate (256 x 32 px): a rounded dark plate with a thin light rim along the top edge.
    py, px = np.mgrid[0:32, 0:W] + 0.5
    sd = M.rounded_rect(px - W / 2, py - 16, W / 2 - 2, 14, 7)
    cover = 1 - sstep(-0.75, 0.75, sd)
    rim = line(sd + 1.6, 0.6, 0.9) * (0.35 + 0.65 * sstep(12, 30, py))
    shade = 0.55 + 0.45 * (py / 32)
    atlas[32:64] = np.dstack([rim, shade * cover, np.zeros_like(rim), cover])
    # Fill (256 x 16 px): a pill with a sheen near the top and faint ticks every 10 %.
    fy, fx = np.mgrid[0:16, 0:W] + 0.5
    sd = M.rounded_rect(fx - W / 2, fy - 8, W / 2 - 1.5, 6.5, 6.5)
    cover = 1 - sstep(-0.75, 0.75, sd)
    sheen = gauss(fy - 11.0, 1.6) * cover
    body = (0.72 + 0.28 * (fy / 16)) * cover
    ticks = np.zeros_like(fx)
    for k in range(1, 10):
        ticks = np.maximum(ticks, line(fx - W * k / 10, 0.4, 0.8))
    atlas[16:32] = np.dstack([sheen, body, ticks * cover, cover])
    # Armour (256 x 16 px): eight segments, thinner, with gaps.
    ay, ax = np.mgrid[0:16, 0:W] + 0.5
    cover = np.zeros_like(ax)
    for k in range(8):
        c = (k + 0.5) * W / 8
        sd = M.rounded_rect(ax - c, ay - 8, W / 16 - 2.5, 5.0, 3.0)
        cover = np.maximum(cover, 1 - sstep(-0.75, 0.75, sd))
    sheen = gauss(ay - 10.5, 1.3) * cover
    body = (0.75 + 0.25 * (ay / 16)) * cover
    atlas[0:16] = np.dstack([sheen, body, np.zeros_like(ax), cover])
    tex("UI_HealthBar", "UI_HealthBar", atlas,
        [(1, (0.4, 1.0, 0.4), 1.0), (0, (1, 1, 1), 0.6), ("alpha", 3)], mode="alpha",
        notes="atlas rows (V bottom-up): armour 0-0.25, fill 0.25-0.5, plate 0.5-1. Plate: R top rim, G shade, A "
              "cover (draw at PlateColor, alpha 0.5*A). Fill: R sheen, G body, B 10 % ticks, A cover. Armour: eight "
              "segments, R sheen, G body, A cover. Colour = tint * G + R * 0.35 (B darkens by 0.25)")

    def uv_rect(row):
        return (0.0, row[0], 1.0, row[1])
    plate = M.quad("HealthBar_Plate", PLATE_W, PLATE_H, "XZ", uv_rect=uv_rect(ROW_PLATE), uv2_rect=(0, 0, 1, 1),
                   offset=(0, 0.004, PLATE_Y))
    fill = M.quad("HealthBar_Fill", BAR_W, BAR_H, "XZ", uv_rect=uv_rect(ROW_FILL), uv2_rect=(0, 0, 1, 1))
    armour = M.quad("HealthBar_Armour", BAR_W, ARMOUR_H, "XZ", uv_rect=uv_rect(ROW_ARMOUR), uv2_rect=(0, 0, 1, 1),
                    offset=(0, 0, ARMOUR_Y))
    M.export(os.path.join(C.EXPORTS, "UI_HealthBar", "UI_HealthBar.fbx"), [plate, fill, armour])
    for o, n in ((plate, "plate behind the lines (0.004 further from the camera)"), (fill, "health line; pivot = bar anchor"),
                 (armour, "armour line above the health line")):
        mesh_report("UI_HealthBar", o, pivot="health line centre (EnemyView _barY)", note=n + "; UV2.x 0..1 along the "
                    "bar: the shader clips UV2.x > _Fill (cut from the right) so the mesh is never scaled")
    return [plate, fill, armour]


# ---------------------------------------------------------------------------------------------------------------
# Particle atlas
# ---------------------------------------------------------------------------------------------------------------

PARTICLES = ["Spore", "Mote", "Ember", "Drip", "IchorSplat", "GooDecal", "FrostMote", "ChronoMote"]


def particle_cell(name, seed):
    n = 128
    x, y, r = centred(n, n)
    a = np.arctan2(y, x)
    u, v = grid(n, n)
    noise = M.fbm(u, v, 4, 4, seed=seed)
    if name == "Spore":
        body = 1 - sstep(0.42, 0.56, r + 0.05 * (noise - 0.5))
        core = gauss(r, 0.22)
        cilia = (np.abs(np.sin(a * 7 + noise * 2)) ** 18) * sstep(0.48, 0.55, r) * (1 - sstep(0.62, 0.85, r))
        inner = body * (0.5 + 0.5 * sstep(0.2, 0.5, r))
        return core, inner, cilia, np.clip(np.maximum(body, cilia * 0.8), 0, 1)
    if name == "Mote":
        core = gauss(r, 0.14)
        halo = gauss(r, 0.45) * (1 - sstep(0.85, 1.0, r))
        return core, halo, np.zeros_like(r), np.maximum(core, halo)
    if name == "Ember":
        xx, yy = x * 1.4, y * 0.8 + 0.1 * np.sin(x * 4)
        rr = np.hypot(xx, yy) + 0.25 * (noise - 0.5)
        body = 1 - sstep(0.42, 0.5, rr)
        hot = gauss(rr, 0.2)
        cool = body * sstep(0.45, 0.7, noise)
        return hot, body, cool, body
    if name == "Drip":
        yy = y + 0.15
        width = 0.36 * np.sqrt(np.clip((0.75 - yy) / 1.2, 0, 1)) * np.clip((yy + 0.65) / 0.4, 0, 1) ** 0.5
        width = np.where(yy < -0.25, np.sqrt(np.clip(0.4 ** 2 - (yy + 0.25) ** 2, 0, None)) * 0.9, width)
        body = 1 - sstep(-0.02, 0.02, np.abs(x) - width)
        hi = gauss(np.hypot(x + 0.12, yy + 0.3), 0.09)
        rim = body * sstep(0.5, 0.95, np.abs(x) / np.maximum(width, 1e-3))
        return hi * body, body, rim, body
    if name == "IchorSplat":
        rr = r + 0.35 * (M.fbm(u, v, 3, 3, seed=seed + 1) - 0.5) - 0.12 * np.abs(np.sin(a * 5))
        body = 1 - sstep(0.4, 0.46, rr)
        rng = np.random.default_rng(seed)
        for _ in range(9):
            ang, dist, rad = rng.uniform(0, TAU), rng.uniform(0.55, 0.85), rng.uniform(0.03, 0.07)
            body = np.maximum(body, 1 - sstep(rad * 0.8, rad, np.hypot(x - math.cos(ang) * dist, y - math.sin(ang) * dist)))
        thick = gauss(rr, 0.3) * body
        hi = gauss(np.hypot(x + 0.12, y - 0.12), 0.1) * body
        return hi, thick, body * sstep(0.5, 0.8, noise), body
    if name == "GooDecal":
        rr = r + 0.3 * (M.fbm(u, v, 2, 3, seed=seed + 2) - 0.5)
        body = 1 - sstep(0.62, 0.7, rr)
        rng = np.random.default_rng(seed + 3)
        bubbles, hi = np.zeros_like(r), np.zeros_like(r)
        for _ in range(7):
            ang, dist, rad = rng.uniform(0, TAU), rng.uniform(0.05, 0.45), rng.uniform(0.05, 0.11)
            dd = np.hypot(x - math.cos(ang) * dist, y - math.sin(ang) * dist)
            bubbles = np.maximum(bubbles, line(dd - rad, 0.012, 0.02))
            hi = np.maximum(hi, gauss(np.hypot(x - math.cos(ang) * dist + rad * 0.4, y - math.sin(ang) * dist - rad * 0.4), rad * 0.25))
        thick = (0.4 + 0.6 * (1 - sstep(0.0, 0.7, rr))) * body
        return hi * body, thick, bubbles * body, body
    if name == "FrostMote":
        arms = np.zeros_like(r)
        branch = np.zeros_like(r)
        for k in range(6):
            ang = k * TAU / 6
            d = np.array([math.cos(ang), math.sin(ang)])
            along = x * d[0] + y * d[1]
            across = -x * d[1] + y * d[0]
            ok = (along > 0) & (along < 0.82)
            arms = np.maximum(arms, line(across, 0.018 * (1 - along), 0.025) * ok)
            for t in (0.35, 0.58):
                for sgn in (-1, 1):
                    bd = np.array([math.cos(ang + sgn * 0.9), math.sin(ang + sgn * 0.9)])
                    bx, by = x - d[0] * t, y - d[1] * t
                    ba = bx * bd[0] + by * bd[1]
                    bc = -bx * bd[1] + by * bd[0]
                    branch = np.maximum(branch, line(bc, 0.012, 0.02) * (ba > 0) * (ba < 0.22 * (1.1 - t)))
        core = gauss(r, 0.1)
        glow = gauss(r, 0.5) * 0.5
        return np.maximum(core, arms), glow, branch, np.clip(np.maximum.reduce([arms, branch, glow, core]), 0, 1)
    if name == "ChronoMote":
        ring = line(r - 0.5, 0.025, 0.03)
        ticks = np.zeros_like(r)
        for k in range(3):
            ang = k * TAU / 3 + 0.4
            dd = np.hypot(x - math.cos(ang) * 0.5, y - math.sin(ang) * 0.5)
            ticks = np.maximum(ticks, gauss(dd, 0.06))
        hand = line(x * math.sin(1.1) - y * math.cos(1.1), 0.02, 0.02) * (r < 0.38) * ((x * math.cos(1.1) + y * math.sin(1.1)) > -0.05)
        glow = gauss(r - 0.5, 0.2) * 0.6 + gauss(r, 0.2) * 0.3
        return np.maximum(ring, hand), glow, ticks, np.clip(np.maximum.reduce([ring, glow, ticks, hand]), 0, 1)
    raise KeyError(name)


def particles():
    cell = 128
    atlas = np.zeros((cell * 2, cell * 4, 4), dtype=np.float32)
    for i, name in enumerate(PARTICLES):
        row, col = divmod(i, 4)
        r, g, b, a = particle_cell(name, 11 + i * 7)
        y0 = (1 - row) * cell   # row 0 is the top row of the image (Unity sheet order)
        atlas[y0:y0 + cell, col * cell:(col + 1) * cell] = pack(r, g, b, a)
    tex("Particles_Atlas", "Particles_Atlas", atlas,
        [(1, WARM, 0.8), (0, (1, 1, 1), 0.9), (2, WARM, 0.4)],
        notes="4 x 2 sprites, 128 px, Texture Sheet Animation order (top-left first): " + ", ".join(PARTICLES) +
              ". R lit core / highlight, G body or glow, B inner detail (cilia, cooling edge, rim, bubbles, branches, "
              "ticks), A coverage. Tint per system; additive for Mote/Ember/Frost/Chrono, alpha for Drip/Ichor/Goo, "
              "either for Spore")
    return []


# ---------------------------------------------------------------------------------------------------------------
# Review: contact sheet of every texture as the shader shows it
# ---------------------------------------------------------------------------------------------------------------

def contact(out):
    paths = []
    for name, (asset, arr, recipe, mode, _) in TEXTURES.items():
        p = os.path.join(out, f"_card_{name}.png")
        M.texture_card(arr, recipe, p, cell=256, mode=mode)
        paths.append(p)
    sheet = os.path.join(out, "markers_textures_sheet.png")
    C.contact_sheet(paths, sheet, 6, 256)
    for p in paths:
        os.remove(p)
    return sheet


def write_reports():
    for asset, data in REPORT.items():
        data["round"] = 25
        path = os.path.join(C.EXPORTS, asset, asset + "_report.json")
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
        C.log("report", asset)


def build_all():
    glow_pool()
    meshes = {"quads": decal_quads(), "cursor": cursor(), "range": range_ring(), "spot": hero_spot(),
              "stand": hero_stand(), "slow": slow_field(), "cues": road_cues(), "bar": health_bar()}
    particles()
    write_reports()
    return meshes


def main():
    C.reset_scene()
    args = C.cli_args()
    out = os.path.join(C.preview_dir())
    meshes = build_all()
    C.save_blend("Markers")
    if args.get("norender"):
        return
    C.log("sheet", contact(out))
    if not args.get("noscene"):
        import marker_scene
        marker_scene.render_scene(meshes, TEXTURES, out)


if __name__ == "__main__":
    main()
