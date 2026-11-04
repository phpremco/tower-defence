"""Round 26: cutscene glow, flare, trail and decal sprites (numpy, authored in Blender's Python like the VFX set).

Replaces the textures the cutscene code paints at runtime: SpaceKit.SoftDot / SoftBand (every SpaceKit.Glow and
SpaceKit.Line: sun core, halo and streak, glints, engine glows, running lights, capsule heads, scan lines, meridians,
capsule trails), the stretched GlowDecal.Radial quads used as ground cracks (FieldDrill, FieldSwarm), the scream ring
(FieldSwarm) and the GlowDecal.Scanlines gate sheets (FieldBase). Everything is white / greyscale so the code keeps
tinting it with its HDR colours; RGB = A (premultiplied-style additive, like the old textures).

Outputs ArtSource/Exports/Cutscene_FX/ (+ Low/ at half size) and Cutscene_FX_report.json.

Run:  blender --background --factory-startup --python Tools/Blender/build_cutscene_fx.py -- [--preview <dir>]
      (pure numpy; about a minute)
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np  # noqa: E402

import vfx_kit as V  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "ArtSource", "Exports", "Cutscene_FX")
OUT_LOW = os.path.join(OUT, "Low")
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = ARGV[ARGV.index("--preview") + 1] if "--preview" in ARGV else os.path.join(ROOT, "ArtSource", "Previews", "Cutscene_FX")

REPORT = {}
IMAGES = {}


def save(name, img, meta):
    """Greyscale value -> RGBA with RGB = A (additive, tinted by the material colour); full + Low copy."""
    v = np.clip(img, 0, 1).astype(np.float32)
    rgba = np.stack([v, v, v, v], axis=-1)
    os.makedirs(OUT_LOW, exist_ok=True)
    size = V.save_png(os.path.join(OUT, name + ".png"), rgba)
    h, w = v.shape
    V.save_png(os.path.join(OUT_LOW, name + ".png"), V.resize(rgba, h // 2, w // 2))
    entry = {"file": name + ".png", "size": [w, h], "bytes": size, "colour_space": "linear (sRGB off)",
             "blend": "additive (One One), RGB = A"}
    entry.update(meta)
    REPORT[name] = entry
    IMAGES[name] = v
    print(f"[fx] {name} {w}x{h} {size / 1e3:.0f} kB", flush=True)


def centred(n, m=None):
    """Coordinates -1..1 (x right, y up) for an n x m image (rows top to bottom)."""
    m = m or n
    x, y = V.grid(n, m)
    return x * 2 - 1, 1 - y * 2


def edge_fade(r, start=0.82):
    """Forces every radial sprite to exactly zero at its quad edge (no visible square)."""
    return V.smoothstep(1.0, start, r)


# ------------------------------------------------------------------------------------------------ glows and suns

def glow_soft():
    x, y = centred(256)
    r = np.hypot(x, y)
    core = np.exp(-(r / 0.07) ** 2)
    body = np.exp(-(r / 0.26) ** 2) * 0.55
    tail = 0.22 / (1 + (r / 0.18) ** 2) - 0.22 / (1 + (1 / 0.18) ** 2)
    v = (core * 0.55 + body + tail * 1.4) * edge_fade(r, 0.6)
    save("FX_GlowSoft", v / v.max(), {
        "use": "every round glow quad: SpaceKit.Glow (sun parts, glints, engine glows, running lights, capsule heads)",
        "replaces": "SpaceKit.SoftDot (UI/Cutscene/SpaceKit.cs:94-103, 115-132)",
        "wrap": "Clamp"})


def beam_soft():
    h, w = 64, 256
    x, y = V.grid(h, w)
    across = np.abs(y * 2 - 1)
    p = V.Perlin(11)
    fil = 0.5 + 0.5 * p.fbm(x * 4, y * 3, 0.0, octaves=3, period=(4, 256, 256))
    core = np.exp(-(across / 0.09) ** 2)
    body = np.exp(-(across / 0.32) ** 2)
    v = core * 0.75 + body * (0.35 + 0.25 * fil)
    v *= V.smoothstep(1.0, 0.85, across)
    save("FX_BeamSoft", v / v.max(), {
        "use": "LineRenderer texture (Texture Mode Tile or Stretch): U along the line (tiles seamlessly), V across",
        "replaces": "SpaceKit.SoftBand (UI/Cutscene/SpaceKit.cs:105-113) via SpaceKit.Line: SurveySet scan/echo and "
                    "meridians (SurveySet.cs:59-68)",
        "wrap": "Repeat U, Clamp V"})


def sun_core():
    x, y = centred(512)
    r = np.hypot(x, y)
    disc_r = 0.34
    mu = np.sqrt(np.clip(1 - (r / disc_r) ** 2, 0, 1))
    limb = 0.62 + 0.38 * mu  # limb darkening
    p = V.Perlin(3)
    gran = 1 + 0.06 * p.fbm(x * 14, y * 14, 0, octaves=3)
    disc = V.smoothstep(disc_r + 0.012, disc_r - 0.01, r) * limb * gran
    corona = np.exp(-((r - disc_r).clip(0) / 0.09) ** 1.3) * 0.55 * (r > disc_r * 0.9)
    ang = np.arctan2(y, x)
    streamers = 0.75 + 0.25 * (0.5 + 0.5 * p.fbm(np.cos(ang) * 3 + 10, np.sin(ang) * 3, r * 2, octaves=3))
    v = np.maximum(disc, corona * streamers) * edge_fade(r, 0.75)
    save("FX_SunCore", v / v.max(), {
        "use": "the sun disc quad (scale = SpaceStyle.SunSize); disc fills 34 % of the quad radius, corona to 75 %",
        "replaces": "SoftDot on SunCore (SpaceBackdrop.cs:35)", "wrap": "Clamp"})


def sun_halo():
    x, y = centred(512)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    p = V.Perlin(5)
    wide = np.exp(-(r / 0.42) ** 1.6)
    near = np.exp(-(r / 0.12) ** 2) * 0.8
    shoulder = np.exp(-((r - 0.2) / 0.05) ** 2) * 0.12
    # Fine irregular rays: angular noise sampled on a circle so it wraps, sharpened, fading outward.
    rays = 0.5 + 0.5 * p.fbm(np.cos(ang) * 5, np.sin(ang) * 5, 0.0, octaves=4)
    rays = V.smoothstep(0.5, 0.9, rays) ** 1.5 * np.exp(-(r / 0.55) ** 1.3) * V.smoothstep(0.06, 0.22, r)
    ring = np.exp(-((r - 0.62) / 0.05) ** 2) * 0.018  # barely-there lens ring
    v = (wide * 0.55 + near + shoulder + rays * 0.65 + ring) * edge_fade(r, 0.7)
    save("FX_SunHalo", v / v.max(), {
        "use": "the halo quad around the sun (scale = SpaceStyle.HaloSize); faint fine rays and a limb shoulder",
        "replaces": "SoftDot on SunHalo (SpaceBackdrop.cs:34)", "wrap": "Clamp"})


def sun_streak():
    h, w = 128, 512
    x, y = V.grid(h, w)
    u, v_ = x * 2 - 1, y * 2 - 1
    au = np.abs(u)
    along = (1 - au) ** 2.2
    line = np.exp(-(v_ / 0.05) ** 2) * along
    soft = np.exp(-(v_ / 0.22) ** 2) * (1 - au) ** 3.5 * 0.45
    ghost = np.exp(-((np.abs(v_) - 0.18) / 0.03) ** 2) * (1 - au) ** 4 * 0.12
    centre = np.exp(-(np.hypot(u * 4, v_) / 0.25) ** 2) * 0.5
    val = (line + soft + ghost + centre) * V.smoothstep(1.0, 0.9, np.abs(v_))
    save("FX_SunStreak", val / val.max(), {
        "use": "anamorphic flare across the sun (quad scaled StreakLength x StreakLength * StreakThickness)",
        "replaces": "SoftDot on SunStreak (SpaceBackdrop.cs:36)", "wrap": "Clamp"})


def glint():
    x, y = centred(256)
    r = np.hypot(x, y)
    core = np.exp(-(r / 0.06) ** 2)
    halo = np.exp(-(r / 0.22) ** 2) * 0.3

    def spike(ax, ay, width, length):
        return np.exp(-(np.abs(ay) / (width * (1 + 2 * np.abs(ax)))) ** 2) * np.exp(-(np.abs(ax) / length) ** 1.3)

    main = spike(x, y, 0.012, 0.42) + spike(y, x, 0.012, 0.42)
    d1, d2 = (x + y) / math.sqrt(2), (x - y) / math.sqrt(2)
    diag = (spike(d1, d2, 0.01, 0.16) + spike(d2, d1, 0.01, 0.16)) * 0.35
    v = (core + halo + main * 0.9 + diag) * edge_fade(r, 0.85)
    save("FX_Glint", v / v.max(), {
        "use": "crystal glints on the survey planet; four long spikes, four short diagonals",
        "replaces": "SoftDot on Glint quads (SurveySet.cs:70-77)", "wrap": "Clamp"})


def engine_glow():
    x, y = centred(256)
    r = np.hypot(x, y)
    core = np.exp(-(r / 0.1) ** 2)
    ring = np.exp(-((r - 0.2) / 0.035) ** 2) * 0.55  # the nozzle rim lit from inside
    shock = np.exp(-((r - 0.34) / 0.05) ** 2) * 0.05
    halo = np.exp(-(r / 0.45) ** 1.5) * 0.35
    v = (core + ring + shock + halo) * edge_fade(r, 0.7)
    save("FX_EngineGlow", v / v.max(), {
        "use": "engine exhaust glow on the mothership nozzles (camera-facing): hot core, lit nozzle ring, shock rings",
        "replaces": "SoftDot on Exhaust quads (OrbitSet.cs:189)", "wrap": "Clamp"})


def exhaust_plume():
    h, w = 512, 128
    x, y = V.grid(h, w)
    across = (x * 2 - 1)
    t = y  # 0 at the nozzle (top row = v 1 in Unity: nozzle at the top)
    p = V.Perlin(21)
    width = 0.22 + 0.55 * t ** 0.7
    a = np.abs(across) / width
    turb = 0.6 + 0.4 * (0.5 + 0.5 * p.fbm(across * 3, t * 9, 0, octaves=4))
    diamonds = 1 + 0.35 * np.exp(-(a / 0.4) ** 2) * (0.5 + 0.5 * np.cos(t * 2 * math.pi * 5)) * np.exp(-t * 3)
    v = np.exp(-a ** 2 * 2.5) * (1 - t) ** 1.4 * turb * diamonds
    v += np.exp(-(np.hypot(across / 0.25, t / 0.06)) ** 2) * 0.6
    v *= V.smoothstep(1.0, 0.85, np.abs(across))
    save("FX_ExhaustPlume", v / v.max(), {
        "use": "optional stretched quad / particle behind a nozzle (nozzle at the top edge, V down the plume); for a "
               "plume aligned with the ship's back axis",
        "replaces": "new (OrbitSet.cs:189 exhaust, if a directional plume is added)", "wrap": "Clamp"})


def running_light():
    x, y = centred(128)
    r = np.hypot(x, y)
    core = np.exp(-(r / 0.12) ** 2)
    halo = np.exp(-(r / 0.35) ** 2) * 0.35
    cross = (np.exp(-(np.abs(y) / 0.02) ** 2) * np.exp(-np.abs(x) / 0.25)
             + np.exp(-(np.abs(x) / 0.02) ** 2) * np.exp(-np.abs(y) / 0.25)) * 0.45
    v = (core + halo + cross) * edge_fade(r, 0.8)
    save("FX_RunningLight", v / v.max(), {
        "use": "blinking navigation lights on the mothership: tight dot with a tiny cross",
        "replaces": "SoftDot on RunningLight quads (OrbitSet.cs:229)", "wrap": "Clamp"})


def plasma_trail():
    h, w = 128, 512
    x, y = V.grid(h, w)
    u = x  # 0 = head (LineRenderer position 0, the capsule), 1 = tail
    v = y * 2 - 1
    p = V.Perlin(31)
    width = 0.18 + 0.75 * u ** 0.6
    a = np.abs(v) / width
    body = np.exp(-a ** 2 * 3) * (1 - u) ** 1.1
    core = np.exp(-(np.abs(v) / (0.05 + 0.12 * u)) ** 2) * (1 - u) ** 2.4
    # Filaments: thin bright strands that braid and break up towards the tail.
    fil = 0.0
    for k, seed in enumerate((0.0, 3.1, 7.7)):
        off = 0.35 * width * p.fbm(u * 6 + seed, seed, 0, octaves=3)
        strand = np.exp(-((v - off) / (0.025 + 0.04 * u)) ** 2)
        breakup = V.smoothstep(-0.15, 0.25, p.fbm(u * 14 + seed * 2, seed + 5, 0, octaves=2) + 0.35 - u * 0.6)
        fil = fil + strand * breakup * (1 - u) ** 0.8 * (0.6 - 0.12 * k)
    flicker = 0.7 + 0.3 * (0.5 + 0.5 * p.fbm(u * 24, v * 2, 3.3, octaves=2))
    head = np.exp(-(np.hypot(u / 0.05, v / 0.25)) ** 2)
    val = (body * flicker * 0.6 + core + fil * 0.7 + head * 0.8)
    val *= V.smoothstep(1.0, 0.8, np.abs(v)) * V.smoothstep(1.0, 0.9, u)
    save("FX_PlasmaTrail", val / val.max(), {
        "use": "re-entry plasma trail, LineRenderer Texture Mode Stretch: U = 0 at the capsule (head), 1 at the tail; "
               "keep the gradient/width curves",
        "replaces": "default particle texture on the FieldSet capsule streak (FieldSet.cs:412-424) and SoftBand on "
                    "the CapsuleDrop trails (CapsuleDrop.cs:179-182)", "wrap": "Clamp"})


# ------------------------------------------------------------------------------------------------ cracks and rings

def jagged(rng, a, b, depth, rough):
    """Midpoint-displaced polyline from a to b: an organic jagged crack line."""
    pts = [np.array(a, float), np.array(b, float)]
    for d in range(depth):
        out = [pts[0]]
        for i in range(len(pts) - 1):
            p0, p1 = pts[i], pts[i + 1]
            seg = p1 - p0
            n = np.array((-seg[1], seg[0]))
            m = (p0 + p1) / 2 + n * rng.normal(0, rough * 0.5 ** (d * 0.7))
            out += [m, p1]
        pts = out
    return pts


def crack_layer(px, py, branches, rng, base_w, glow_r):
    """branches: list of (points, w0, w1) -> core / glow max fields (pixel units 0..1)."""
    core = np.zeros(px.shape)
    glow = np.zeros(px.shape)
    for pts, w0, w1 in branches:
        n = len(pts) - 1
        t = np.linspace(0, 1, n)
        swell = 0.55 + 0.45 * np.abs(np.sin(t * math.pi * rng.uniform(2, 4) + rng.uniform(0, 6)))
        ws = base_w * (w0 + (w1 - w0) * t) * swell
        gs = glow_r * (w0 + (w1 - w0) * t) + 0.004
        c, g = V.polyline_field(px, py, pts, ws, gs, np.ones(n))
        core = np.maximum(core, c)
        glow = np.maximum(glow, g)
    return core, glow


def branch_from(rng, pts, count, length, spread, w):
    """Side branches leaving a crack at random points, angled off its direction."""
    out = []
    for _ in range(count):
        i = rng.integers(1, len(pts) - 2)
        p0 = pts[i]
        d = pts[i + 1] - pts[i - 1]
        d = d / (np.linalg.norm(d) + 1e-9)
        ang = math.atan2(d[1], d[0]) + rng.choice((-1, 1)) * rng.uniform(0.35, spread)
        L = length * rng.uniform(0.5, 1.0)
        p1 = p0 + np.array((math.cos(ang), math.sin(ang))) * L
        out.append((jagged(rng, p0, p1, 4, 0.18), w, 0.05))
    return out


def drill_cracks():
    """2 x 2 sheet of radial drill-hole cracks: a scorched glowing hole, jagged spokes that fork, hot cores."""
    cell = 512
    tiles = []
    for k in range(4):
        rng = np.random.default_rng(100 + k)
        px, py = V.grid(cell, cell)
        branches = []
        spokes = 5 + k
        for s in range(spokes):
            ang = 2 * math.pi * (s + rng.uniform(-0.3, 0.3)) / spokes
            L = rng.uniform(0.3, 0.46)
            a = (0.5 + math.cos(ang) * 0.05, 0.5 + math.sin(ang) * 0.05)
            b = (0.5 + math.cos(ang) * L, 0.5 + math.sin(ang) * L)
            main = jagged(rng, a, b, 5, 0.22)
            branches.append((main, 1.7, 0.12))
            branches += branch_from(rng, main, rng.integers(0, 3), L * 0.4, 0.8, 0.45)
        core, glow = crack_layer(px, py, branches, rng, 0.011, 0.028)
        r = np.hypot(px - 0.5, py - 0.5)
        p = V.Perlin(200 + k)
        hole = V.smoothstep(0.09, 0.03, r + 0.02 * p.fbm(px * 8, py * 8, 0, octaves=3))
        heat = np.exp(-(r / 0.16) ** 2)
        crust = 0.75 + 0.25 * (0.5 + 0.5 * p.fbm(px * 20, py * 20, 1, octaves=3))
        v = (core * (0.6 + 0.4 * heat) + glow * 0.55 * crust + hole * 0.9 + heat * 0.2) * crust
        v *= V.smoothstep(0.5, 0.42, r)
        tiles.append(np.clip(v / max(v.max(), 1e-6), 0, 1))
    sheet = V.atlas([t[..., None] for t in tiles], 2, 2)
    save("FX_DrillCracks", sheet, {
        "use": "ground decal under the drill, 2 x 2 sheet (512 px cells, top-left first): radial glowing cracks "
               "from the bore hole. One quad of radius ~2.6 m with _BaseMap tiling (0.5, 0.5) and offset per cell; "
               "grow its scale with `open`",
        "cells": {"rows": 2, "cols": 2, "order": "top-left first, row by row"},
        "replaces": "the 6 stretched GlowDecal.Radial crack quads in FieldDrill (FieldDrill.cs:75-84, posed at "
                    "FieldDrill.cs:127-134)", "wrap": "Clamp"})


def crack_strip(seed, fissure):
    """One vertical strip (V along the crack, centred): drill style tapers to both ends, fissure style opens a
    molten lens in the middle with crusted edges."""
    h, w = 1024, 256
    rng = np.random.default_rng(seed)
    px, py = V.grid(h, w)
    # Work in a square metric: x scaled so a unit is the strip's height.
    sx = px * (w / h)
    cx = 0.5 * (w / h)
    main = jagged(rng, (cx + rng.uniform(-0.02, 0.02), 0.03), (cx + rng.uniform(-0.02, 0.02), 0.97), 6, 0.06)
    main = [np.array((min(max(p[0], 0.03), w / h - 0.03), p[1])) for p in main]
    t = np.array([p[1] for p in main])
    taper = np.sin(np.clip(t, 0, 1) * math.pi) ** 0.8
    n = len(main) - 1
    tm = (taper[:-1] + taper[1:]) / 2
    widths = (0.008 if not fissure else 0.02) * (0.3 + 0.7 * tm) * (0.8 + 0.4 * rng.random(n))
    glows = 0.012 + 0.03 * tm
    core, glow = V.polyline_field(sx, py, main, widths, glows, np.ones(n))
    side = []
    for _ in range(rng.integers(3, 6) if fissure else rng.integers(6, 10)):
        i = rng.integers(2, len(main) - 3)
        p0 = main[i]
        sgn = rng.choice((-1, 1))
        L = rng.uniform(0.04, 0.09)
        p1 = p0 + np.array((sgn * L * 0.9, rng.uniform(-0.4, 0.4) * L))
        p1[0] = min(max(p1[0], 0.02), w / h - 0.02)
        side.append(jagged(rng, p0, p1, 3, 0.25))
    for pts in side:
        m = len(pts) - 1
        c, g = V.polyline_field(sx, py, pts, np.linspace(0.006, 0.0015, m), np.full(m, 0.01), np.ones(m) * 0.7)
        core = np.maximum(core, c)
        glow = np.maximum(glow, g)
    p = V.Perlin(seed)
    crust = 0.7 + 0.3 * (0.5 + 0.5 * p.fbm(px * 6, py * 24, 0, octaves=3))
    mid = np.sin(np.clip(py, 0, 1) * math.pi) ** 2
    v = core * (0.6 + 0.4 * mid) + glow * 0.5 * crust
    if fissure:
        # The opening: a narrow lens of molten light around the crack's own path, brightest mid-length.
        path_x = np.interp(py[:, 0], t, [q[0] for q in main])[:, None]
        gaps = V.smoothstep(-0.25, 0.1, p.fbm(py * 9, 3.7, 0, octaves=2))
        d = np.abs(sx - path_x)
        lens = np.exp(-(d / (0.003 + 0.011 * mid)) ** 2) * mid * gaps
        halo = np.exp(-(d / (0.01 + 0.03 * mid)) ** 2) * mid * (0.5 + 0.5 * gaps) * 0.35
        v = np.maximum(v, lens * crust + halo)
    v *= V.smoothstep(0.5, 0.46, np.abs(px - 0.5)) * V.smoothstep(0.5, 0.47, np.abs(py - 0.5))
    return np.clip(v / max(v.max(), 1e-6), 0, 1)


def crack_strips():
    for name, seeds, fissure, use, site in (
            ("FX_CrackStrip_Drill", (41, 43), False,
             "one straight crack per quad, centred, tapering to both ends (scale x = width, y = length)",
             "GlowDecal.Radial on the FieldDrill crack quads (FieldDrill.cs:76-84) if the 6-quad rig is kept"),
            ("FX_CrackStrip_Swarm", (51, 53), True,
             "a ground fissure that opens: molten lens mid-length, crust edges (scale x = width, y = length)",
             "GlowDecal.Radial on the FieldSwarm crack decals (FieldSwarm.cs:69, BuildCracks 164-176, posed 90-98)")):
        for j, s in enumerate(seeds):
            save(f"{name}_{'AB'[j]}", crack_strip(s, fissure), {"use": use, "replaces": site, "wrap": "Clamp"})


def scream_ring():
    x, y = centred(512)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    p = V.Perlin(61)
    wob = 0.012 * p.fbm(np.cos(ang) * 6, np.sin(ang) * 6, 0, octaves=3)
    rr = r + wob
    front = 0.9
    lead = np.exp(-((rr - front) / 0.018) ** 2)
    trail = np.exp(-((front - rr).clip(0) / 0.09) ** 1.3) * (rr < front) * 0.4
    ripples = (0.5 + 0.5 * np.cos((front - rr) * 2 * math.pi * 14)) ** 4 * np.exp(-((front - rr).clip(0) / 0.16)) * (rr < front) * 0.18
    outer = np.exp(-((rr - front).clip(0) / 0.025)) * (rr >= front) * 0.3
    broken = 0.65 + 0.35 * (0.5 + 0.5 * p.fbm(np.cos(ang) * 14, np.sin(ang) * 14, 2, octaves=3))
    v = (lead + trail + ripples + outer) * broken * edge_fade(r, 0.97)
    save("FX_ScreamRing", v / v.max(), {
        "use": "expanding shock ring of the scream (ground decal, scale = radius * 2): sharp leading edge at 90 % of "
               "the quad radius, ripples and a fading wake inside",
        "replaces": "GlowDecal.Radial on ScreamRing (FieldSwarm.cs:70, Decal at 282-293, posed at 112-114)",
        "wrap": "Clamp"})


def gate_field():
    n = 256
    x, y = V.grid(n, n)
    # Scanlines: 6 per tile like the old texture, sharpened, plus one fine line between each.
    lines = 0.06 + 0.6 * (0.5 + 0.5 * np.sin(y * 2 * math.pi * 6)) ** 8 + 0.12 * (0.5 + 0.5 * np.sin(y * 2 * math.pi * 24)) ** 6
    # Hex lattice (pointy-top, cell radius r): two offset rectangular lattices, nearest centre of each; the
    # boundary between them is the hex edge. 4 hex rows per tile, so it repeats along V.
    s = 1.0 / 4.0
    r = s / 3.0
    hx = math.sqrt(3) * r
    ax, ay = (x / hx) % 1.0 - 0.5, (y / s) % 1.0 - 0.5
    bx, by = (x / hx + 0.5) % 1.0 - 0.5, (y / s + 0.5) % 1.0 - 0.5
    da = np.hypot(ax * hx, ay * s)
    db = np.hypot(bx * hx, by * s)
    qx = np.where(da < db, ax * hx, bx * hx)
    qy = np.where(da < db, ay * s, by * s)
    metric = np.maximum(np.abs(qx), np.abs(qx) * 0.5 + np.abs(qy) * 0.866)
    edge = (hx / 2 - metric) / r
    hexl = V.smoothstep(0.07, 0.0, edge) * 0.32
    p = V.Perlin(71)
    shimmer = 0.5 + 0.5 * p.fbm(x * 12, y * 1.0, 0, octaves=3, period=(12, 1, 256))
    shimmer = shimmer ** 2
    across = np.abs(x * 2 - 1)
    edgefade = 1 - across ** 4
    rim = np.exp(-((1 - across) / 0.03) ** 2) * 0.6
    v = (lines * (0.4 + 0.6 * shimmer) + hexl * (0.5 + 0.5 * shimmer)) * edgefade + rim
    save("FX_GateField", v / v.max(), {
        "use": "gate energy sheets: U across the field (built-in edge fade and bright rims), V tiles seamlessly "
               "(scroll it like the old scanlines); 6 scanlines, 4 hex rows and vertical shimmer per tile",
        "replaces": "GlowDecal.Scanlines via GlowDecal.ScanlineMaterial on the FieldBase gate sheets "
                    "(FieldBase.cs:389-392, Sheet 395-406, posed via _gateFields; also FieldBase.cs:483)",
        "wrap": "Clamp U, Repeat V"})


# ------------------------------------------------------------------------------------------------ previews

TINTS = {"FX_GlowSoft": (1.0, 0.8, 0.5), "FX_BeamSoft": (0.3, 0.85, 1.0), "FX_SunCore": (1.0, 0.92, 0.75),
         "FX_SunHalo": (1.0, 0.75, 0.45), "FX_SunStreak": (0.8, 0.85, 1.0), "FX_Glint": (0.7, 0.95, 1.0),
         "FX_EngineGlow": (0.45, 0.75, 1.0), "FX_ExhaustPlume": (0.45, 0.75, 1.0), "FX_RunningLight": (1.0, 0.3, 0.25),
         "FX_PlasmaTrail": (1.0, 0.55, 0.2), "FX_DrillCracks": (1.0, 0.5, 0.85), "FX_ScreamRing": (1.0, 0.4, 0.15),
         "FX_GateField": (0.4, 0.8, 1.0)}


def preview_tile(name, img, size=256):
    tint = next((v for k, v in TINTS.items() if name.startswith(k)), (1.0, 0.45, 0.15))
    h, w = img.shape
    s = size / max(h, w)
    th, tw = max(8, int(h * s)), max(8, int(w * s))
    small = V.resize(img, th, tw)
    canvas = np.zeros((size, size))
    oy, ox = (size - th) // 2, (size - tw) // 2
    canvas[oy:oy + th, ox:ox + tw] = small
    bg = np.full((size, size, 3), (0.02, 0.022, 0.03))
    em = canvas[..., None] * np.array(tint) * 2.2 + (canvas ** 3)[..., None] * 1.5
    return V.tonemap(bg + em)


def contact_sheet():
    names = list(IMAGES)
    tiles = [preview_tile(n, IMAGES[n]) for n in names]
    sheet = V.sheet(tiles, 6, pad=6, bgval=0.08)
    os.makedirs(PREVIEW, exist_ok=True)
    V.save_png(os.path.join(PREVIEW, "fx_contact_sheet.png"), sheet)
    with open(os.path.join(PREVIEW, "fx_contact_sheet.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(f"{i}: {n}" for i, n in enumerate(names)))


def paste_add(canvas, img, cx, cy, scale_px, tint, gain=1.0, aspect=None, rot=0.0):
    """Additively draws a sprite centred at (cx, cy) px, `scale_px` wide (height keeps aspect unless given)."""
    h, w = img.shape
    W = int(scale_px)
    H = int(scale_px * (aspect if aspect is not None else h / w))
    if W < 2 or H < 2:
        return
    # Sample with rotation by inverse mapping.
    Hc, Wc = canvas.shape[:2]
    R = int(math.hypot(W, H) / 2) + 2
    x0, x1 = max(0, int(cx - R)), min(Wc, int(cx + R))
    y0, y1 = max(0, int(cy - R)), min(Hc, int(cy + R))
    if x0 >= x1 or y0 >= y1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1].astype(float)
    dx, dy = xx - cx, yy - cy
    c, s = math.cos(-rot), math.sin(-rot)
    lx, ly = dx * c - dy * s, dx * s + dy * c
    u = lx / W + 0.5
    v = ly / H + 0.5
    ok = (u >= 0) & (u < 1) & (v >= 0) & (v < 1)
    iu = np.clip((u * w).astype(int), 0, w - 1)
    iv = np.clip((v * h).astype(int), 0, h - 1)
    val = np.where(ok, img[iv, iu], 0.0)
    canvas[y0:y1, x0:x1] += val[..., None] * np.array(tint) * gain


def mock():
    H, W = 720, 1280
    p = V.Perlin(9)
    x, y = V.grid(H, W)
    ground = 0.5 + 0.5 * p.fbm(x * 12, y * 7, 0, octaves=5)
    bg = np.stack([0.03 + 0.03 * ground, 0.028 + 0.026 * ground, 0.04 + 0.03 * ground], -1)
    # Space band at the top, ground below a soft horizon.
    sky = V.smoothstep(0.42, 0.3, y)[..., None]
    bg = bg * (1 - sky) + sky * np.array((0.006, 0.008, 0.02))
    can = bg.copy()
    # Sun with halo and streak, top right.
    paste_add(can, IMAGES["FX_SunHalo"], 1050, 110, 520, (1.0, 0.72, 0.42), 1.1)
    paste_add(can, IMAGES["FX_SunCore"], 1050, 110, 150, (1.0, 0.92, 0.75), 3.0)
    paste_add(can, IMAGES["FX_SunStreak"], 1050, 110, 900, (0.75, 0.82, 1.0), 0.9)
    for gx, gy in ((300, 60), (520, 140), (760, 40)):
        paste_add(can, IMAGES["FX_Glint"], gx, gy, 60, (0.7, 0.95, 1.0), 1.5)
    paste_add(can, IMAGES["FX_RunningLight"], 180, 180, 40, (1.0, 0.3, 0.25), 2.0)
    paste_add(can, IMAGES["FX_EngineGlow"], 240, 200, 70, (0.45, 0.75, 1.0), 2.0)
    # Capsule trail falling from upper left towards the ground.
    ang = math.radians(32)
    hx, hy = 640, 330
    L = 600
    paste_add(can, IMAGES["FX_PlasmaTrail"], hx - math.cos(ang) * L / 2, hy - math.sin(ang) * L / 2, L,
              (1.0, 0.5, 0.18), 2.2, aspect=0.14, rot=ang + math.pi)
    paste_add(can, IMAGES["FX_GlowSoft"], hx, hy, 90, (1.0, 0.85, 0.6), 2.5)
    # Ground decals foreshortened (flattened vertically): drill cracks, swarm fissures, scream ring.
    cell = IMAGES["FX_DrillCracks"][:512, :512]
    paste_add(can, cell, 360, 560, 360, (1.0, 0.45, 0.85), 2.0, aspect=0.45)
    for i, (fx_, fy, rot) in enumerate(((820, 520, 1.3), (930, 600, 1.9), (1080, 540, 1.5))):
        strip = IMAGES["FX_CrackStrip_Swarm_" + "ABA"[i]]
        paste_add(can, strip, fx_, fy, 70, (1.0, 0.32, 0.12), 2.2, aspect=4.0 * 0.45, rot=rot)
    paste_add(can, IMAGES["FX_ScreamRing"], 980, 600, 380, (1.0, 0.4, 0.15), 1.2, aspect=0.45)
    # Gate field sheet standing on the ground.
    gf = np.tile(IMAGES["FX_GateField"], (2, 1))
    paste_add(can, gf, 620, 600, 160, (0.4, 0.8, 1.0), 0.8, aspect=1.1)
    os.makedirs(PREVIEW, exist_ok=True)
    V.save_png(os.path.join(PREVIEW, "fx_mock.png"), V.tonemap(can))


def main():
    glow_soft()
    beam_soft()
    sun_core()
    sun_halo()
    sun_streak()
    glint()
    engine_glow()
    exhaust_plume()
    running_light()
    plasma_trail()
    drill_cracks()
    crack_strips()
    scream_ring()
    gate_field()
    REPORT["_notes"] = {"round": 26, "convention": "PNG row 0 = top (Unity v = 1). Greyscale RGBA with RGB = A; tint "
                        "with the existing HDR colours. Low/ = half size."}
    with open(os.path.join(OUT, "Cutscene_FX_report.json"), "w", encoding="utf-8") as f:
        json.dump(REPORT, f, indent=2)
    contact_sheet()
    mock()


main()
