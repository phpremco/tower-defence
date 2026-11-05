"""Round 29: boss ground decals, boss flipbooks and the "dampened" HUD overlay (numpy, plus one Cycles render for
the icon, run inside Blender like the other 2D sets).

Replaces the stand-ins Unity draws for the continent-boss mechanics (Docs/MODEL_TODO.md "Ronde 4a/4b/4c Unity"):
painted glow pools, the glow blade (`MarkerKind.GlowBlade`, `CrackStyle.Heat/Furrow/Seal`) and the TowerAuraView
haze. Three kinds of texture:

- **Ground decals** (`BossFX_<Name>_Albedo.png` + `BossFX_<Name>_Emission.png`): Albedo is sRGB RGBA with straight
  alpha (coverage) and the light from the top left baked in (like every decal in the game); Emission is linear:
  R = glow mask (0..1, the material colour sets the heat), G = a progress threshold per decal (see the report:
  "lit while G > cool", "shown while G < grow"), B = 0, A = 1. Strips are 1024 x 256: U runs along the path and
  tiles seamlessly (Wrap Repeat U, Clamp V), V runs across with soft edges. The radial frost decal is a 2 x 2 atlas.
- **Flipbooks** (`BossFX_<Name>.png`, 4 x 4 frames of 256, Unity Texture Sheet order: top-left first, row by row):
  linear data like the round-20 VFX set. "CG" packs (R hot core, G glow, B extra, A coverage, additive, tinted) and
  "LIT" packs (R lit shading premultiplied by A, G thickness / heat, B specular, A alpha; the round-20 Acid_Splash
  recipe) for the liquid wakes.
- **UI** (`ArtSource/Exports/UI/UI_Dampened.png` + `@2x`): an overlay for a tower picture on the boss bar, drawn
  with the round-25 icon pipeline (ui_art: bevelled curves, Cycles, dark rim) so it matches UI_Icons.

Outputs ArtSource/Exports/Boss_FX/ (+ Low/ at half size) and Boss_FX_report.json; previews (contact sheets, a
top-down field mock with the strips laid along curved paths, flipbook strips, the icon over tower pictures) go to
--preview (default ArtSource/Previews/Boss_FX).

Run:  blender --background --factory-startup --python Tools/Blender/build_bosses29_fx.py -- [--preview <dir>]
      [--only decals,sheets,icon,previews]      (about 2 minutes)
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
OUT = os.path.join(ROOT, "ArtSource", "Exports", "Boss_FX")
OUT_LOW = os.path.join(OUT, "Low")
UI_OUT = os.path.join(ROOT, "ArtSource", "Exports", "UI")
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = ARGV[ARGV.index("--preview") + 1] if "--preview" in ARGV else os.path.join(ROOT, "ArtSource", "Previews", "Boss_FX")
ONLY = set((ARGV[ARGV.index("--only") + 1] if "--only" in ARGV else "decals,sheets,icon,previews").split(","))

REPORT = {}
DECALS = {}   # name -> (albedo RGBA straight, emission RGBA)
SHEETS = {}   # name -> (atlas RGBA, kind, preview tint)
SW, SH, SA = 1024, 256, 4          # strip size and aspect (U along : V across)
LIGHT = np.array((-0.55, -0.55, 0.63))  # top-left light in image space (x right, y down, z up)
LIGHT = LIGHT / np.linalg.norm(LIGHT)


# ------------------------------------------------------------------------------------------------------- helpers

def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def to_lin(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def ss(a, b, x):
    return V.smoothstep(a, b, x)


def save_tex(name, img, meta, folder=OUT, low=True):
    os.makedirs(os.path.join(folder, "Low"), exist_ok=True)
    size = V.save_png(os.path.join(folder, name + ".png"), img)
    h, w = img.shape[:2]
    lb = V.save_png(os.path.join(folder, "Low", name + ".png"), V.resize(img, h // 2, w // 2)) if low else 0
    entry = {"file": name + ".png", "size": [w, h], "bytes": size, "low_bytes": lb}
    entry.update(meta)
    REPORT[name] = entry
    print(f"[boss_fx] {name} {w}x{h} {size / 1e3:.0f} kB", flush=True)


def save_decal(name, albedo_lin, alpha, glow, thresh, meta):
    """albedo in linear RGB (stored sRGB), straight alpha, glow 0..1, threshold 0..1."""
    alb = np.concatenate([to_srgb(albedo_lin), np.clip(alpha, 0, 1)[..., None]], -1).astype(np.float32)
    em = np.stack([np.clip(glow, 0, 1), np.clip(thresh, 0, 1), np.zeros_like(glow), np.ones_like(glow)], -1)
    base = {"albedo": f"BossFX_{name}_Albedo.png (sRGB on, straight alpha = coverage)",
            "emission": f"BossFX_{name}_Emission.png (sRGB off: R glow mask, G threshold, B 0, A 1)"}
    base.update(meta)
    save_tex(f"BossFX_{name}_Albedo", alb, dict(base, part="albedo"))
    save_tex(f"BossFX_{name}_Emission", em, dict(base, part="emission"))
    DECALS[name] = (alb, em, meta.get("preview_glow", (1.0, 0.4, 0.1)))


def strip_xy():
    x, y = V.grid(SH, SW)
    return x, y, x * SA, y


class Noise:
    """Perlin fbm on strip coordinates (X 0..SA along, periodic; Y 0..1 across)."""

    def __init__(self, seed, X, Y):
        self.p, self.X, self.Y = V.Perlin(seed), X, Y

    def __call__(self, f, z=0.0, octaves=4, gain=0.5):
        f = int(f)
        return self.p.fbm(self.X * f, self.Y * f, z, octaves=octaves, period=(SA * f, 256, 256), gain=gain)


def voronoi_strip(X, Y, nx, ny, seed, lx=SA):
    """Voronoi periodic along X (period lx, nx cells) and across Y (ny cells over 0..1): F1, F2, per-cell randoms
    (3 values) and the F1 cell centre offset (dx, dy) in X/Y units."""
    rng = np.random.default_rng(seed)
    off = rng.uniform(0.08, 0.92, (ny, nx, 2))
    val = rng.uniform(0, 1, (ny, nx, 3))
    sx, sy = nx / lx, float(ny)
    gx, gy = X * sx, Y * sy
    ix, iy = np.floor(gx).astype(int), np.floor(gy).astype(int)
    f1 = np.full(X.shape, 9.0)
    f2 = np.full(X.shape, 9.0)
    v1 = np.zeros(X.shape + (3,))
    dx1 = np.zeros(X.shape)
    dy1 = np.zeros(X.shape)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            wx, wy = cx % nx, cy % ny
            ox, oy = cx + off[wy, wx, 0], cy + off[wy, wx, 1]
            ddx, ddy = (gx - ox) / sx, (gy - oy) / sy
            d = np.hypot(ddx, ddy)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            v1 = np.where(closer[..., None], val[wy, wx], v1)
            dx1 = np.where(closer, ddx, dx1)
            dy1 = np.where(closer, ddy, dy1)
            f1 = np.where(closer, d, f1)
    return f1, f2, v1, dx1, dy1


def shade(h, strength, wrap_x=True):
    """Lambert of a height field (pixels) against the top-left light, 1.0 on flat ground."""
    if wrap_x:
        gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    else:
        gx = np.gradient(h, axis=1)
    gy = np.gradient(h, axis=0)
    nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    lam = (nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]) / ln
    return np.clip(lam / LIGHT[2], 0, 2.5)


def blur_x(img, r):
    return V.blur(img, r, wrap=True) if r > 0 else img


def col(*c):
    return np.array(c, np.float64)


def mixc(a, b, t):
    t = np.asarray(t)[..., None]
    return a * (1 - t) + b * t


# ------------------------------------------------------------------------------------------------ ground decals

def lava_trail():
    """The Cinder Matriarch's trail: a tongue of fresh basalt, ropy in the middle, levees at the edges, crust plates
    split by glowing cracks and a molten channel that opens in places."""
    x, y, X, Y = strip_xy()
    n = Noise(301, X, Y)
    c = 0.5 + 0.05 * n(1, 1.0, 2)
    d = y - c
    half = 0.31 + 0.05 * n(2, 2.0, 3)
    e = np.abs(d) / half
    wx, wy = X + 0.03 * n(6, 4.0, 2), Y + 0.03 * n(6, 5.0, 2)
    f1, f2, cv, _, _ = voronoi_strip(wx, wy, 30, 8, 302)
    gap = f2 - f1
    crack = ss(0.022, 0.004, gap)
    heat = np.exp(-(e / 0.5) ** 2)
    ropes = 0.5 + 0.5 * np.sin(2 * math.pi * (X * 9 + 7 * (d / 0.3) ** 2 * 0.6) + 2.0 * n(4, 8.0, 2))
    ropes_w = ss(0.75, 0.2, e)
    levee = np.exp(-((e - 0.82) / 0.12) ** 2)
    h = (0.55 + 0.45 * cv[..., 0]) * (1 - 0.8 * crack) + 0.35 * ropes * ropes_w * (1 - crack) + 0.9 * levee
    h += 0.15 * n(24, 9.0, 3)
    sh = shade(blur_x(h, 1.0), 9.0)
    rim = ss(0.05, 0.02, gap) * (1 - crack)
    base = mixc(col(0.035, 0.03, 0.032), col(0.07, 0.055, 0.05), cv[..., 1])
    base = mixc(base, col(0.13, 0.05, 0.03), heat * 0.55)            # still-hot crust is reddish
    base = base * (0.75 + 0.5 * rim[..., None])                       # glassy plate rims
    albedo = base * sh[..., None]
    albedo = mixc(albedo, col(0.25, 0.04, 0.01), crack * heat)
    open_ = ss(-0.05, 0.3, n(2, 7.0, 2))
    chan = np.exp(-(d / (0.012 + 0.02 * open_)) ** 2) * open_
    live = ss(0.35, 0.6, cv[..., 2] + 0.35 * heat)
    glow_cr = crack * heat ** 2 * live
    glow = np.maximum(chan, glow_cr * 0.85)
    glow = np.clip(glow + 0.35 * blur_x(glow, 3.0), 0, 1)
    spat = ss(0.55, 0.72, n(12, 3.0, 2)) * ss(1.45, 1.05, e)
    alpha = np.maximum(ss(1.04, 0.9, e), spat * 0.9)
    alpha = np.clip(alpha * ss(0.5, 0.44, np.abs(y - 0.5)), 0, 1)
    thresh = np.clip(np.maximum(heat * (0.6 + 0.3 * cv[..., 2]), chan * 1.2), 0, 1)
    save_decal("LavaTrail", albedo, alpha, glow * alpha, thresh, {
        "kind": "strip decal", "wrap": "Repeat U, Clamp V", "world_size_m": "1.4 wide, 5.6 per tile",
        "use": "Cinder Matriarch lava trail: one strip along her trail (LineRenderer Tile, or per-piece quads with a "
               "random U offset)",
        "threshold": "G = heat: a pixel glows while G > cool, cool 0 -> 1 over the 6 s cooling (edges cool first, the "
                     "molten channel last); emission = R * smoothstep(cool - 0.08, cool, G) * lava colour (1, 0.35, "
                     "0.08) x 3; fade albedo alpha out over the last seconds",
        "replaces": "LavaTrailView painted glow pools (MODEL_TODO ronde 4b 'Lavaspoor')",
        "preview_glow": (1.0, 0.33, 0.07)})


def ground_crack():
    """Gravemaw's crack line: a meandering fissure with a molten core, dark walls, heaved and tilted ground plates
    along both lips with glow seeping into their cracks, and short side cracks."""
    x, y, X, Y = strip_xy()
    n = Noise(311, X, Y)
    c = 0.5 + 0.07 * n(1, 0.0, 2) + 0.02 * n(5, 1.0, 2)
    d = y - c
    pinch = 0.5 + 0.5 * n(2, 2.0, 2)
    w = 0.018 + 0.03 * np.clip(pinch, 0, 1)
    ad = np.abs(d)
    core = np.exp(-(d / (w * 0.55)) ** 2)
    pit = ss(w * 1.15, w * 0.7, ad)
    wall = ss(w * 2.0, w * 1.1, ad) * (1 - pit)
    wx, wy = X + 0.02 * n(8, 3.0, 2), Y + 0.02 * n(8, 4.0, 2)
    f1, f2, cv, cdx, cdy = voronoi_strip(wx, wy, 36, 9, 312)
    gap = f2 - f1
    band = ss(0.34, 0.12, ad)
    seam = ss(0.016, 0.004, gap) * band
    tilt = (cv[..., 0] - 0.5) * 2
    heave = band * (0.6 + 0.4 * cv[..., 1])
    h = heave * (0.6 + 0.5 * (cdy * np.sign(d + 1e-6)) * 6) - seam * 0.6 - pit * 1.5 - wall * 0.6
    h += 0.12 * tilt * band + 0.08 * n(30, 5.0, 3)
    sh = shade(blur_x(h, 0.8), 10.0)
    # Side cracks: thin ridged lines that leave the lips and die out.
    sidecr = (1 - np.abs(n(10, 6.0, 3)) / 0.06).clip(0, 1) ** 3 * ss(0.3, 0.05, ad) * ss(0.25, 0.5, n(3, 7.0, 2) + 0.5)
    base = mixc(col(0.06, 0.05, 0.045), col(0.11, 0.09, 0.075), cv[..., 2]) * (0.8 + 0.3 * band[..., None])
    albedo = base * sh[..., None] * (1 - 0.7 * seam[..., None]) * (1 - 0.6 * sidecr[..., None])
    albedo = mixc(albedo, col(0.015, 0.01, 0.01), np.maximum(wall, pit))
    albedo = mixc(albedo, col(0.4, 0.12, 0.03), core * 0.8)
    glow = np.clip(core + 0.55 * seam * ss(0.2, 0.04, ad) + 0.5 * sidecr * ss(0.15, 0.03, ad), 0, 1)
    glow = np.clip(glow + 0.3 * blur_x(glow, 4.0), 0, 1)
    alpha = np.clip(np.maximum(band * 0.95, np.maximum(wall, pit)), 0, 1) * ss(0.42, 0.34, ad)
    alpha *= ss(0.5, 0.46, np.abs(y - 0.5))
    thresh = np.clip(1 - ad / 0.3, 0, 1)
    save_decal("GroundCrack", albedo, alpha, glow * np.maximum(alpha, core), thresh, {
        "kind": "strip decal", "wrap": "Repeat U, Clamp V", "world_size_m": "2.0 wide, 8 per tile",
        "use": "Gravemaw's crack line over Burrow_Travel (along the crest ridge) and the entrance crack towards the "
               "colony rig",
        "threshold": "G = 1 - distance from the fissure: the crack opens while `open` rises 0 -> 1: show alpha where "
                     "G > 1 - open (the fissure first, the heaved lips last); emission = R * warm colour (1, 0.42, "
                     "0.1) x 3-4",
        "replaces": "MarkerKind.GlowBlade orange seam (MODEL_TODO ronde 4a 'Scheurlijn'); also fits CrackStyle.Heat",
        "preview_glow": (1.0, 0.4, 0.1)})


def furrow():
    """The Sealed One's haul: a dark ploughed trench with drag scratches, heaved lips with clods, dust thrown wide
    and a faint violet seep in the deepest scratches."""
    x, y, X, Y = strip_xy()
    n = Noise(321, X, Y)
    c = 0.5 + 0.03 * n(1, 0.0, 2)
    d = y - c
    ad = np.abs(d)
    w = 0.17 + 0.02 * n(3, 1.0, 2)
    trench = ss(w, w * 0.6, ad)
    lip = np.exp(-((ad - w * 1.35) / (w * 0.4)) ** 2)
    scratch = 0.5 + 0.5 * n(1, 2.0, 1)
    p = V.Perlin(322)
    sc = p.fbm(X * 1, Y * 60, 3.0, octaves=3, period=(SA, 256, 256))
    drag = (1 - np.abs(sc) / 0.08).clip(0, 1) ** 2 * trench
    f1, f2, cv, cdx, cdy = voronoi_strip(X + 0.02 * n(12, 4.0, 2), Y, 70, 18, 323)
    clod = ss(0.045, 0.02, f1) * (cv[..., 0] > 0.45) * ss(0.12, 0.0, np.abs(ad - w * 1.45) - 0.02)
    h = -trench * 1.2 + lip * (0.9 + 0.3 * n(6, 5.0, 2)) + clod * 0.6 - drag * 0.25 + 0.1 * n(28, 6.0, 3)
    sh = shade(blur_x(h, 1.2), 12.0)
    dust = ss(0.48, 0.22, ad) * (0.5 + 0.5 * n(5, 7.0, 3))
    soil = mixc(col(0.09, 0.075, 0.065), col(0.03, 0.025, 0.028), trench)
    soil = mixc(soil, col(0.16, 0.13, 0.11), lip * 0.7)
    albedo = soil * sh[..., None] * (1 - 0.4 * drag[..., None])
    albedo = mixc(albedo, col(0.2, 0.17, 0.15), (dust * (1 - trench) * (1 - lip) * 0.6))
    alpha = np.clip(np.maximum(np.maximum(trench, lip * 1.2), clod) + dust * 0.45, 0, 1)
    alpha *= ss(0.5, 0.45, np.abs(y - 0.5))
    seep = drag * ss(0.6, 1.0, scratch) * trench
    save_decal("Furrow", albedo, alpha, np.clip(seep * 0.7, 0, 1), np.ones_like(x), {
        "kind": "strip decal", "wrap": "Repeat U, Clamp V", "world_size_m": "3.0 wide, 12 per tile",
        "use": "The Sealed One's furrow behind Walk / Climb_* / Rise_OntoBase (it ploughs the ground; add dust puffs)",
        "threshold": "G = 1 (unused); R = a faint violet seep in the deepest drag scratches: emission colour (0.6, "
                     "0.3, 1) x 1.5, fade it with the phase",
        "replaces": "CrackStyle.Furrow dark seam (MODEL_TODO ronde 4c 'Voor van the Sealed One')",
        "preview_glow": (0.6, 0.3, 1.0)})


def rot_trail():
    """Rotbloom's trail: a slick of dark slime with drag streaks, rings of pale mould, pustules and glowing spores."""
    x, y, X, Y = strip_xy()
    n = Noise(331, X, Y)
    c = 0.5 + 0.06 * n(1, 0.0, 2)
    d = y - c
    ad = np.abs(d)
    half = 0.26 + 0.08 * n(3, 1.0, 3)
    e = ad / half
    slime = ss(1.05, 0.7, e)
    blobs = ss(0.5, 0.7, n(8, 2.0, 3)) * ss(1.7, 1.0, e)
    p = V.Perlin(332)
    streak = 0.5 + 0.5 * p.fbm(X * 1, Y * 40, 3.0, octaves=3, period=(SA, 256, 256))
    f1, f2, cv, _, _ = voronoi_strip(X + 0.03 * n(9, 4.0, 2), Y + 0.03 * n(9, 5.0, 2), 30, 8, 333)
    ringr = 0.035 + 0.03 * cv[..., 0]
    mould = np.exp(-((f1 - ringr) / 0.008) ** 2) * (cv[..., 1] > 0.62) * ss(1.5, 0.6, e)
    mould_fill = ss(ringr, ringr * 0.6, f1) * (cv[..., 1] > 0.62) * ss(1.5, 0.6, e) * 0.5
    f1b, _, cvb, _, _ = voronoi_strip(X, Y, 120, 30, 334)
    pust = ss(0.012, 0.004, f1b) * (cvb[..., 0] > 0.72) * np.maximum(slime, blobs)
    spores = ss(0.006, 0.0, f1b) * (cvb[..., 1] > 0.8) * ss(1.6, 0.4, e)
    h = slime * 0.4 + blobs * 0.3 + mould * 0.35 + pust * 0.7 + 0.12 * streak * slime
    sh = shade(blur_x(h, 1.0), 9.0)
    spec = ss(1.15, 1.5, sh) * slime
    slimec = mixc(col(0.015, 0.025, 0.01), col(0.045, 0.07, 0.015), streak)
    albedo = slimec * sh[..., None]
    albedo = mixc(albedo, col(0.42, 0.45, 0.3), np.clip(mould + mould_fill, 0, 1) * 0.85)
    albedo = mixc(albedo, col(0.35, 0.5, 0.08), pust * 0.8)
    albedo = albedo + spec[..., None] * col(0.2, 0.26, 0.12)
    alpha = np.clip(np.maximum(np.maximum(slime * 0.92, blobs * 0.85), np.maximum(mould, mould_fill)) + spores, 0, 1)
    alpha *= ss(0.5, 0.45, np.abs(y - 0.5))
    glow = np.clip(spores + pust * 0.6 + mould * 0.15, 0, 1)
    thresh = np.clip(1 - e * 0.6 + 0.2 * cv[..., 2], 0, 1)
    save_decal("RotTrail", albedo, alpha, glow * alpha, thresh, {
        "kind": "strip decal", "wrap": "Repeat U, Clamp V", "world_size_m": "1.6 wide, 6.4 per tile",
        "use": "Rotbloom's rot trail (per trail piece, like the lava trail)",
        "threshold": "G = life: the decal is visible while G > decay, decay 0 -> 1 over its life (the edges and the "
                     "mould rings go first); emission = R * spore green (0.55, 1.0, 0.2) x 1.5 (pustules and spores)",
        "replaces": "LavaTrailView in rot colours (MODEL_TODO ronde 4c 'Rotbloom-rotspoor')",
        "preview_glow": (0.55, 1.0, 0.2)})


def road_sink():
    """The old road giving way when the Sealed One's new road rises: broken slabs tilting into a dark slump, stepped
    shear cracks along both edges, wet dark centre and a void-violet seep in the deepest cracks."""
    x, y, X, Y = strip_xy()
    n = Noise(341, X, Y)
    v = y - 0.5
    av = np.abs(v)
    sink = ss(0.46, 0.05, av + 0.04 * n(3, 0.0, 2))
    f1, f2, cv, cdx, cdy = voronoi_strip(X + 0.03 * n(10, 1.0, 2), Y + 0.03 * n(10, 2.0, 2), 16, 4, 342)
    gap = f2 - f1
    crack = ss(0.014, 0.004, gap) * ss(0.48, 0.3, av) * ss(0.25, 0.45, cv[..., 1] + 0.3 * sink)
    shear = np.zeros_like(x)
    for s, off in ((-1, 0.36), (1, 0.36)):
        line = 0.5 + s * off + 0.025 * n(4, 3.0 + s, 3)
        broken = ss(-0.1, 0.15, n(3, 8.0 + s, 2))
        shear = np.maximum(shear, ss(0.01, 0.002, np.abs(y - line)) * broken)
    tilt = (cv[..., 0] - 0.5) * 0.8
    h = -sink * 1.2 + tilt * sink * (cdx * 8) + 0.3 * cv[..., 1] * sink - crack * 0.8 - shear * 0.5
    h += 0.08 * n(30, 5.0, 3)
    sh = shade(blur_x(h, 0.8), 12.0)
    wet = sink * ss(0.2, 0.7, 0.5 + 0.5 * n(5, 6.0, 3))
    dark = col(0.02, 0.018, 0.025)
    albedo = mixc(col(0.08, 0.075, 0.075), dark, sink * 0.85) * sh[..., None]
    albedo = albedo * (1 - 0.8 * np.maximum(crack, shear)[..., None])
    albedo = albedo + (ss(1.2, 1.6, sh) * wet)[..., None] * col(0.05, 0.05, 0.07)
    alpha = np.clip(sink * 0.9 + np.maximum(crack, shear) * 0.9, 0, 1) * ss(0.5, 0.47, av)
    seep = crack * sink * ss(0.72, 0.9, cv[..., 2]) * 0.8
    save_decal("RoadSink", albedo, alpha, np.clip(seep, 0, 1), np.clip(sink * (0.8 + 0.2 * cv[..., 2]), 0, 1), {
        "kind": "strip decal", "wrap": "Repeat U, Clamp V", "world_size_m": "road width (about 3.5), 14 per tile",
        "use": "laid over the old road when the Sealed One's new road rises (the old one sinks and goes dark)",
        "threshold": "G = sink order: show where G > 1 - sink, sink 0 -> 1 over ~2 s (the middle first, then out to "
                     "the shear cracks); R = void seep in the deepest cracks, colour (0.55, 0.25, 1) x 1.5",
        "replaces": "nothing yet: the old road did not change (MODEL_TODO ronde 4c 'Nieuwe weg van the Sealed One')",
        "preview_glow": (0.55, 0.25, 1.0)})


def frost_creep():
    """The Frost Warden's frost: four radial rime patches of branching ice dendrites (60 degree side branches like
    real frost ferns) over a speckled rime haze. G = growth order, so the frost visibly creeps out."""
    cell = 512
    albs, ems = [], []
    for k in range(4):
        rng = np.random.default_rng(350 + k)
        px, py = V.grid(cell, cell)
        core = np.zeros(px.shape)
        order = np.full(px.shape, 1.0)
        segs = []

        def grow(p, ang, length, w, s0, depth):
            steps = max(3, int(length / 0.025))
            pts = [np.array(p)]
            a = ang
            for i in range(steps):
                a += rng.normal(0, 0.12)
                pts.append(pts[-1] + np.array((math.cos(a), math.sin(a))) * length / steps)
            for i in range(steps):
                t = i / steps
                segs.append((pts[i], pts[i + 1], w * (1 - 0.7 * t), s0 + length * t))
                if depth > 0 and i > 0 and rng.random() < 0.75:
                    for sgn in (-1, 1):
                        if rng.random() < 0.8:
                            d = a + sgn * math.radians(60 + rng.normal(0, 6))
                            grow(pts[i], d, length * (1 - t) * rng.uniform(0.25, 0.5), w * 0.6, s0 + length * t,
                                 depth - 1)
        arms = rng.integers(5, 8)
        for j in range(arms):
            ang = 2 * math.pi * (j + rng.uniform(-0.25, 0.25)) / arms
            grow((0.5, 0.5), ang, rng.uniform(0.3, 0.44), 0.006, 0.0, 2)
        maxs = max(s[3] for s in segs) + 0.05
        for a, b, w, s0 in segs:
            m = w * 3 + 0.01
            x0, x1 = min(a[0], b[0]) - m, max(a[0], b[0]) + m
            y0, y1 = min(a[1], b[1]) - m, max(a[1], b[1]) + m
            cs = (px[0] >= x0) & (px[0] <= x1)
            rs = (py[:, 0] >= y0) & (py[:, 0] <= y1)
            if not cs.any() or not rs.any():
                continue
            sx, sy = np.ix_(rs, cs)
            dd, t = V.seg_dist(px[sx, sy], py[sx, sy], a[0], a[1], b[0], b[1])
            c = ss(w, w * 0.2, dd)
            seg_len = float(np.hypot(*(b - a)))
            o = (s0 + t * seg_len) / maxs
            sub_core = core[sx, sy]
            sub_order = order[sx, sy]
            better = c > sub_core
            core[sx, sy] = np.maximum(sub_core, c)
            order[sx, sy] = np.where(better & (c > 0.05), np.minimum(sub_order, o), sub_order)
        r = np.hypot(px - 0.5, py - 0.5) * 2
        p = V.Perlin(360 + k)
        reach = ss(0.0, 0.2, V.blur(core, 14.0) * 6.0)
        haze = V.blur(core, 5.0) * 2.0 + reach * ss(0.95, 0.2, r) * 0.7 + ss(0.5, 0.0, r) * 0.5
        haze *= 0.6 + 0.4 * p.fbm(px * 6, py * 6, 1.0, octaves=4)
        speck = ss(0.62, 0.8, p.fbm(px * 60, py * 60, 2.0, octaves=2) * 0.5 + 0.5) * np.clip(haze, 0, 1)
        haze = np.clip(haze, 0, 1)
        # Growth order for the haze: radial, jittered, a little behind the branches.
        horder = np.clip(r * 1.05 + 0.08 * p.fbm(px * 5, py * 5, 3.0, octaves=3), 0, 1)
        order = np.where(core > 0.05, order, horder)
        h = core * 1.0 + haze * 0.25 + speck * 0.2
        sh = shade(V.blur(h, 0.7), 6.0, wrap_x=False)
        albedo = mixc(col(0.35, 0.5, 0.65), col(0.85, 0.93, 1.0), np.clip(core + speck * 0.6, 0, 1)) * sh[..., None]
        alpha = np.clip(np.maximum(core, haze * 0.7) + speck * 0.35, 0, 1) * ss(0.98, 0.8, r)
        glow = np.clip(core * 0.6 + speck * 0.3, 0, 1) * alpha
        albs.append(np.concatenate([albedo, alpha[..., None]], -1))
        ems.append(np.stack([glow, order], -1))
    a_atlas = V.atlas(albs, 2, 2)
    e_atlas = V.atlas(ems, 2, 2)
    save_decal("FrostCreep", a_atlas[..., :3], a_atlas[..., 3], e_atlas[..., 0], e_atlas[..., 1], {
        "kind": "radial decal atlas", "rows": 2, "cols": 2, "cell": [512, 512], "wrap": "Clamp",
        "world_size_m": "about 2.5 across per cell",
        "use": "Frost Warden rime: one cell (random, random rotation) 2.2 m ahead of it and under a frozen gate; "
               "_BaseMap tiling (0.5, 0.5), offset per cell",
        "threshold": "G = growth order (0 at the centre, 1 at the branch tips): show alpha where G < grow, grow 0 -> 1 "
                     "over ~1 s, so the frost creeps out along the dendrites; emission = R * ice (0.5, 0.85, 1) x 1.2",
        "replaces": "the pale glow pool with mist 2.2 m ahead of the Warden (MODEL_TODO ronde 4b 'Rijp/vorst')",
        "preview_glow": (0.5, 0.85, 1.0)})


# ------------------------------------------------------------------------------------------------------ flipbooks

FR, CELL = 16, 256


def cgrid():
    x, y = V.grid(CELL, CELL)
    return x * 2 - 1, y * 2 - 1   # x right, y down, -1..1


def save_sheet(name, frames, meta, preview):
    atlas = V.atlas([f.astype(np.float32) for f in frames], 4, 4)
    m = {"kind": "flipbook", "rows": 4, "cols": 4, "frames": FR, "cell": [CELL, CELL],
         "colour_space": "linear (sRGB off)", "frame_order": "top-left first, row by row (Texture Sheet Animation)",
         "wrap": "Clamp", "preview_tint": list(preview[1]) if preview[0] == "cg" else None}
    m.update(meta)
    save_tex(f"BossFX_{name}", atlas, m)
    SHEETS[name] = (atlas, preview)


def spore_cloud():
    """A cloud of Rotbloom spores dropping onto a target pad (camera-facing, the pad at V 0.2 from the bottom): a
    clump of puffs falls in (0-6), lands and spreads into a low ring of haze (7-11) that lingers and thins (12-15).
    Bright specks are the spores themselves."""
    X, Y = cgrid()
    p = V.Perlin(401)
    rng = np.random.default_rng(402)
    puffs = rng.uniform(-1, 1, (14, 2)) * (0.28, 0.18)
    sizes = rng.uniform(0.12, 0.22, 14)
    specks = rng.uniform(-1, 1, (45, 2))
    ground = 0.6
    frames = []
    for i in range(FR):
        t = i / (FR - 1)
        fall = min(t / 0.42, 1.0)
        cy = -0.5 + (ground - 0.08 + 0.5) * fall ** 1.6
        land = max(0.0, (t - 0.4) / 0.6)
        dens = np.zeros(X.shape)
        for (ox, oy), s in zip(puffs, sizes):
            sp = 1 + 2.2 * land
            px_, py_ = ox * sp, oy * (1 - 0.55 * land) + cy
            r = np.hypot((X - px_) / (s * (1 + 0.8 * land)), (Y - py_) / (s * (1 + 0.2 * land) * (1 - 0.35 * land)))
            dens += np.exp(-r ** 2)
        warp = p.fbm(X * 3, Y * 3, t * 2, octaves=5)
        dens = ss(0.15, 1.4, dens * (0.55 + 0.9 * warp)) * 1.3
        ring = np.exp(-((np.hypot(X / 1.0, (Y - ground) / 0.28) - (0.25 + 0.6 * land)) / (0.1 + 0.12 * land)) ** 2)
        ring *= ss(0.0, 0.15, land) * (0.6 + 0.4 * p.fbm(X * 4, Y * 4, 5 + t, octaves=3))
        dens = np.clip(dens * 0.6 + ring * 0.7, 0, 1.6) * (1 - ss(0.65, 1.0, t) * 0.9)
        core = np.zeros(X.shape)
        for sx, sy in specks:
            spread = 0.35 + 0.8 * land
            qx = sx * spread
            qy = cy + sy * 0.25 * (1 - 0.5 * land) - 0.1 * math.sin(i * 0.7 + sx * 5)
            core += np.exp(-((X - qx) ** 2 + (Y - qy) ** 2) / 0.00035)
        core = np.clip(core * np.clip(dens * 1.6, 0, 1), 0, 1) * (1 - ss(0.75, 1.0, t))
        glow = np.clip(dens / 1.2, 0, 1)
        edge = ss(1.0, 0.85, np.maximum(np.abs(X), np.abs(Y)))
        frames.append(np.stack([core * edge, glow * edge, np.zeros_like(X), np.maximum(core, glow) * edge], -1))
    save_sheet("SporeCloud", frames, {
        "fps": 12, "loop": False, "lifetime": 1.4, "pivot": [0.5, 0.2],
        "channels": "CG: R spore specks, G cloud density, B 0, A coverage",
        "blend": "Additive (or alpha blend on G for a murkier cloud)",
        "tint": "G spore green (0.5, 1.0, 0.25), R pale yellow-green (0.9, 1, 0.6)",
        "use": "Rotbloom's spore pod lands on a target pad: camera-facing quad ~2.5 m on the pad, pivot on the pad",
        "replaces": "the green glow pool + point light + mist of TowerAuraView (MODEL_TODO ronde 4c 'Groene waas')"},
        ("cg", (0.5, 1.0, 0.25)))


def vine_burn():
    """Fire burning up the overgrowth vines (camera-facing, the vine root at the bottom): a flame front climbs a
    thorned vine from the bottom (frames 0-11) leaving glowing embers on a charred line, then dies out."""
    X, Y = cgrid()
    p = V.Perlin(411)
    rng = np.random.default_rng(412)
    vy = np.linspace(0.95, -0.9, 60)
    vx = 0.12 * np.sin(vy * 4.0 + 0.5) + 0.05 * np.sin(vy * 11)
    pts = list(zip((vx + 1) / 2, (vy + 1) / 2))
    thorns = []
    for j in range(9):
        k = rng.integers(4, 56)
        s = rng.choice((-1, 1))
        a = (pts[k][0], pts[k][1])
        b = (a[0] + s * rng.uniform(0.04, 0.08), a[1] - rng.uniform(0.02, 0.06))
        thorns.append((a, b, k / 59))
    px, py = V.grid(CELL, CELL)
    vine, _ = V.polyline_field(px, py, pts, np.full(59, 0.018), np.full(59, 0.03), np.ones(59))
    vt = np.full(px.shape, 2.0)
    for i in range(59):
        d, t = V.seg_dist(px, py, *pts[i], *pts[i + 1])
        vt = np.where(d < 0.03, np.minimum(vt, (i + t) / 59), vt)
    for a, b, tt in thorns:
        c, _ = V.polyline_field(px, py, [a, b], [0.008], [0.012], [1.0])
        vine = np.maximum(vine, c)
        d, _ = V.seg_dist(px, py, *a, *b)
        vt = np.where(d < 0.02, np.minimum(vt, tt), vt)
    frames = []
    for i in range(FR):
        t = i / (FR - 1)
        front = 1.25 * t
        f = front - vt
        burning = ss(-0.02, 0.06, f) * ss(0.4, 0.12, f) * (vt < 1.5)
        ember = ss(0.1, 0.2, f) * (vt < 1.5) * ss(0.62, 0.85, p.fbm(px * 30, py * 30, i * 0.3, octaves=2) + 0.5)
        ember *= (1 - ss(0.15, 0.45, f)) * vine * 0.6
        # Flame tongues rise from the burning part of the vine.
        src = V.blur(burning * vine, 5.0)
        flame = np.zeros(px.shape)
        for k in range(1, 26):
            sh = np.roll(src, -k * 2, axis=0) * (1 - k / 26) ** 1.1
            flame = np.maximum(flame, sh)
        turb = p.fbm(px * 7, py * 5 + i * 0.35, i * 0.15, octaves=4)
        flame = np.clip(flame * 4.0 * (0.5 + 0.9 * turb), 0, 1) * (1 - ss(0.85, 1.0, t))
        core = np.clip(burning * vine * 1.2 + ember * 0.8 + ss(0.6, 0.95, flame) * 0.6, 0, 1)
        char = vine * ss(0.05, 0.2, f)
        edge = ss(1.0, 0.88, np.maximum(np.abs(X), np.abs(Y)))
        frames.append(np.stack([core * edge, flame * edge, char * edge, np.maximum(core, flame) * edge], -1))
    save_sheet("VineBurn", frames, {
        "fps": 16, "loop": False, "lifetime": 1.0, "pivot": [0.5, 0.0],
        "channels": "CG: R flame front + embers (hot core), G flame body, B char line (the burnt vine), A coverage",
        "blend": "Additive R/G; B can alpha-darken the vine if drawn over the mesh",
        "tint": "G fire orange (1, 0.45, 0.1), R yellow-white (1, 0.85, 0.5)",
        "use": "burning the overgrowth off a pad: 3-5 camera-facing quads round the tower (about 0.8 x 1.8 m), "
               "random flip, while RotOvergrowth's Retracted blend shape runs 0 -> 100",
        "replaces": "the emission flash on the shrinking vines (MODEL_TODO ronde 4c 'Overgroeide pad bij branden')"},
        ("cg", (1.0, 0.45, 0.1)))


def shard_flash():
    """The Shattered's split flash: a white-hot core, a six-point crystal star, a faceted hexagon shock ring and
    flying crystal fragments with streaks (frames 0-15). Rejoin plays the same sheet backwards (an implosion: the
    fragments and the ring converge on the core and it flares on the form frame)."""
    X, Y = cgrid()
    r = np.hypot(X, Y) + 1e-9
    th = np.arctan2(Y, X)
    p = V.Perlin(421)
    rng = np.random.default_rng(422)
    frag = [(rng.uniform(0, 2 * math.pi), rng.uniform(0.6, 1.0), rng.uniform(0.02, 0.04)) for _ in range(26)]
    frames = []
    hexd = r * np.cos(np.mod(th, math.pi / 3) - math.pi / 6) / math.cos(math.pi / 6)
    for i in range(FR):
        t = i / (FR - 1)
        peak = math.exp(-((t - 0.12) / 0.1) ** 2)
        fade = 1 - ss(0.1, 1.0, t)
        e = 1 - (1 - t) ** 2.4
        core = np.exp(-(r / (0.06 + 0.12 * peak)) ** 2) * (0.25 + 0.75 * fade) * (0.3 + 0.7 * max(peak, fade ** 2))
        L = 0.3 + 0.65 * min(t / 0.25, 1.0)
        star = np.zeros(X.shape)
        for k, (n_, w_, amp) in enumerate(((6, 0.018, 1.0), (6, 0.012, 0.55))):
            off = 0 if k == 0 else math.pi / 6
            ang = np.mod(th - off + math.pi / n_, 2 * math.pi / n_) - math.pi / n_
            lat = np.abs(ang) * r
            ln = L * (1.0 if k == 0 else 0.6)
            star = np.maximum(star, amp * np.exp(-(lat / (w_ * (1 - r / ln).clip(0.05))) ** 2) * ss(ln, 0, r))
        star *= fade ** 1.5
        R = 0.1 + 0.8 * e
        ring = np.exp(-((hexd - R) / (0.015 + 0.05 * (1 - t))) ** 2) * fade * (0.6 + 0.4 * p.fbm(th * 2, r * 4, t, octaves=2))
        fcore = np.zeros(X.shape)
        fglow = np.zeros(X.shape)
        px, py = (X + 1) / 2, (Y + 1) / 2
        for a, sp, sz in frag:
            dist = 0.05 + sp * 0.85 * e
            cx_, cy_ = 0.5 + 0.5 * dist * math.cos(a), 0.5 + 0.5 * dist * math.sin(a)
            tail = 0.5 * 0.25 * sp * (1 - t) + 0.01
            bx, by = cx_ - tail * math.cos(a), cy_ - tail * math.sin(a)
            c, g = V.polyline_field(px, py, [(bx, by), (cx_, cy_)], [sz * 0.25 * (1 - 0.5 * t)], [sz * 0.5], [1.0])
            fcore = np.maximum(fcore, c)
            fglow = np.maximum(fglow, g * 0.6)
        fr = fade ** 0.7
        coreC = np.clip(core * 1.4 + star * 0.8 + fcore * fr, 0, 1)
        glow = np.clip(core * 0.8 + star + ring * 0.7 + fglow * fr + 0.3 * np.exp(-(r / (0.5 * e + 0.1)) ** 2) * fade, 0, 1)
        extra = np.clip(ring, 0, 1)
        edge = ss(1.0, 0.9, r)
        frames.append(np.stack([coreC * edge, glow * edge, extra * edge, np.maximum(coreC, glow) * edge], -1))
    save_sheet("ShardFlash", frames, {
        "fps": 24, "loop": False, "lifetime": 0.67,
        "channels": "CG: R white-hot core, star spikes, fragment heads; G glow; B hexagon shock ring; A coverage",
        "blend": "Additive", "tint": "G crystal pink-violet (0.95, 0.45, 1.0) as the giant's crystals, B ice (0.6, "
                                     "0.9, 1)",
        "use": "Split at the burst frame (28), camera-facing ~6 m at chest height, plus one per shard spawn point at 1/3 "
               "size; Rejoin: play it backwards (frame 15 -> 0, the fragments and ring converge) ending on the form "
               "frame (30)",
        "replaces": "the generic crystal flash of the Split/Rejoin (MODEL_TODO ronde 28 'The Shattered')"},
        ("cg", (0.95, 0.45, 1.0)))


# Dive wakes ------------------------------------------------------------------------------------------------------

def lit_pack(h, alpha, thick, spec_k=1.0, strength=8.0):
    sh = shade(V.blur(h, 0.8), strength, wrap_x=False)
    lit = np.clip(sh * 0.6, 0, 1)
    spec = np.clip(ss(1.25, 1.8, sh) * spec_k, 0, 1)
    a = np.clip(alpha, 0, 1)
    return np.stack([lit * a, np.clip(thick, 0, 1), spec * a, a], -1)


def wake_frames(kind):
    X, Y = cgrid()
    r = np.hypot(X, Y) + 1e-9
    th = np.arctan2(Y, X)
    p = V.Perlin(430 + len(kind))
    rng = np.random.default_rng(440 + len(kind))
    bubbles = [(rng.uniform(-0.45, 0.45), rng.uniform(-0.35, 0.55), rng.uniform(0, 1), rng.uniform(0.03, 0.08))
               for _ in range(14)]
    # A V-wake trails behind (down the frame): the boss travels to the top of the frame.
    vee = np.abs(X) - (Y + 0.1) * 0.55
    frames = []
    for i in range(FR):
        t = i / FR
        z = math.cos(2 * math.pi * t), math.sin(2 * math.pi * t)
        tn = p.fbm(X * 3 + z[0] * 0.4, Y * 3 + z[1] * 0.4, 0.0, octaves=4)
        fin = ss(1.0, 0.75, r)
        bub = np.zeros(X.shape)
        pop = np.zeros(X.shape)
        for bx, by, ph, bs in bubbles:
            life = (t + ph) % 1.0
            grow_ = ss(0.0, 0.6, life) * bs
            if life < 0.7:
                d = np.hypot(X - bx, Y - by)
                bub = np.maximum(bub, np.sqrt(np.clip(1 - (d / max(grow_, 1e-3)) ** 2, 0, 1)))
            else:
                rr = (life - 0.7) / 0.3
                d = np.hypot(X - bx, Y - by)
                pop = np.maximum(pop, np.exp(-((d - bs * (1 + 2.5 * rr)) / 0.012) ** 2) * (1 - rr))
        ripples = np.zeros(X.shape)
        for k in range(3):
            rr = ((t + k / 3) % 1.0)
            ripples += np.exp(-((r - (0.2 + 0.75 * rr)) / (0.03 + 0.02 * rr)) ** 2) * (1 - rr) ** 1.5
        bulge = np.exp(-(np.hypot(X, (Y + 0.15) / 1.3) / 0.38) ** 2)
        wakev = ss(0.08, 0.0, np.abs(vee)) * ss(-0.2, 0.3, Y) * ss(1.0, 0.4, Y)
        if kind == "Water":
            h = bulge * 0.8 + ripples * 0.25 + wakev * 0.2 + bub * 0.4 + tn * 0.1
            foam = np.clip(ss(0.55, 0.9, bulge + 0.3 * tn) + wakev * (0.5 + 0.5 * tn) + pop + bub * 0.5
                           + ripples * 0.25 * ss(0.0, 0.3, tn + 0.2), 0, 1)
            alpha = np.clip(foam * 0.9 + ripples * 0.3 + bulge * 0.25, 0, 1) * fin
            frames.append(lit_pack(h, alpha, foam * 0.2, spec_k=1.0))
        elif kind == "Lava":
            f1, f2, cv, _, _ = voronoi_strip((X + 1) / 2 + 0.01 * z[0], (Y + 1) / 2 + 0.01 * z[1], 7, 7, 450, lx=1)
            gap = f2 - f1
            crust = ss(0.004, 0.012, gap) * ss(0.12, 0.4, r + 0.1 * tn)
            hot = np.clip(ss(0.4, 0.9, bulge) * 0.9 + (1 - crust) * ss(0.95, 0.3, r) * 0.7 + bub * 0.8 + pop, 0, 1)
            h = crust * (0.5 + 0.3 * cv[..., 0]) + bulge * 0.6 + bub * 0.5 + ripples * 0.15
            alpha = np.clip(np.maximum(hot, crust * 0.9) + ripples * 0.2, 0, 1) * fin
            frames.append(lit_pack(h, alpha, hot, spec_k=0.4))
        elif kind == "Bog":
            scum = ss(0.0, 0.4, tn + 0.15) * ss(0.95, 0.4, r)
            h = bulge * 0.5 + bub * 1.0 + scum * 0.25 + ripples * 0.1
            alpha = np.clip(np.maximum(scum * 0.75, bub) + pop * 0.8 + bulge * 0.35 + ripples * 0.15, 0, 1) * fin
            frames.append(lit_pack(h, alpha, np.clip(bub + pop + bulge * 0.3, 0, 1), spec_k=1.3, strength=12.0))
        elif kind == "Quicksand":
            spiral = 0.5 + 0.5 * np.sin(np.log(r + 0.03) * 9.0 + th * 3 + 2 * math.pi * t * 3)
            grains = ss(0.55, 0.8, p.fbm(np.log(r + 0.03) * 8 + t * 4, th * 5 / math.pi, 3.0, octaves=3, period=(256, 10, 256)) * 0.5 + 0.5)
            funnel = -ss(0.75, 0.0, r) ** 1.5
            h = funnel * 1.5 + spiral * 0.15 * ss(0.85, 0.1, r) + grains * 0.06
            hole = ss(0.12, 0.0, r)
            alpha = np.clip(ss(0.95, 0.55, r) * 0.95 + hole, 0, 1) * fin
            frames.append(lit_pack(h, alpha, np.clip(spiral * ss(0.8, 0.2, r) * 0.5 + grains * 0.3 + hole, 0, 1),
                                   spec_k=0.1, strength=14.0))
        else:  # Ice slush
            ang = 0.15 * math.sin(2 * math.pi * t)
            ca, sa_ = math.cos(ang), math.sin(ang)
            qx, qy = X * ca - Y * sa_, X * sa_ + Y * ca
            f1, f2, cv, _, _ = voronoi_strip((qx + 1) / 2, (qy + 1) / 2, 7, 7, 460, lx=1)
            gap = f2 - f1
            bob = np.sin(2 * math.pi * (t + cv[..., 0])) * 0.15
            plate = ss(0.005, 0.014, gap) * ss(0.15, 0.4, r + 0.1 * tn)
            slush = (1 - plate) * ss(0.95, 0.15, r) * (0.6 + 0.4 * tn)
            radial = (1 - np.abs(np.sin(th * 4 + cv[..., 1])) / 0.05).clip(0, 1) * ss(0.95, 0.3, r) * ss(0.1, 0.3, r) * 0.5
            h = plate * (0.7 + bob + 0.2 * cv[..., 2]) + slush * 0.2 + ripples * 0.1
            alpha = np.clip(plate * 0.95 + slush * 0.85 + bulge * 0.3, 0, 1) * fin
            frames.append(lit_pack(h, alpha, np.clip(slush + radial, 0, 1), spec_k=1.4, strength=6.0))
    return frames


WAKES = {
    "Water": ((0.08, 0.2, 0.3), (0.85, 0.95, 1.0), "foam white-blue; G foam thickness (no emission)"),
    "Lava": ((0.08, 0.02, 0.01), (1.0, 0.45, 0.08), "crust near-black to molten orange; G heat: emission = G * (1, 0.4, 0.08) x 4"),
    "Bog": ((0.04, 0.06, 0.02), (0.35, 0.42, 0.15), "dark olive scum to murky green; G = gas bubbles, a faint marsh-gas glow (0.5, 0.9, 0.3) x 0.5 optional"),
    "Quicksand": ((0.18, 0.12, 0.06), (0.62, 0.48, 0.3), "dark wet sand to dry sand; G = streaming grains (lighter)"),
    "Ice": ((0.25, 0.4, 0.55), (0.9, 0.97, 1.0), "slush blue to white plates; G slush and cracks, a faint ice glow (0.5, 0.85, 1) x 0.6"),
}


def dive_wakes():
    for kind, (dark, bright, tint) in WAKES.items():
        save_sheet(f"DiveWake_{kind}", wake_frames(kind), {
            "fps": 12, "loop": True, "lifetime": 1.33,
            "channels": "LIT (as round-20 Acid_Splash): R lit shading (premultiplied by A), G thickness / heat, "
                        "B specular, A alpha",
            "blend": "Alpha blend premultiplied: rgb = lerp(dark, bright, G) * R + B * 0.6 (+ emission for lava)",
            "tint": tint, "preview_dark": dark, "preview_bright": bright,
            "use": f"flat on the {kind.lower()} surface over a submerged boss (Submerged_Loop / Burrow_Travel in "
                   "quicksand), the top of the frame = the boss's forward; ~1.2 x the boss's width; spawn a "
                   "one-shot ring of it at Dive splash / Surface breach frames",
            "replaces": "the glow pool + generated bubbles (MODEL_TODO ronde 4a 'Wake')"},
            ("lit", dark, bright, kind == "Lava"))


# ------------------------------------------------------------------------------------------------------- the icon

def dampened_icon():
    """A broken, struck-through ring for the boss bar: the tower's picture sits inside it, greyed. Same stroke
    weights, bevel, light and dark rim as UI_Icons (build_ui.py)."""
    import ui_art as A
    SWm = 0.075
    cell, pad = 256, 16
    A.reset()
    A.camera(cell, cell)
    coll = A.layer("dampened")
    c = A.Cell(pad, pad, cell - 2 * pad, coll, colour=(1, 1, 1), bevel=6.0)
    # Three ring pieces with jagged (snapped) ends, each nudged off the circle: the ring is broken, not dashed. The
    # breaks avoid the slash ends so the slash stays one clean stroke.
    rad, hw = 0.41, SWm * 0.5
    breaks = [math.radians(a) for a in (95, 215, 335)]
    jag = math.radians(6)
    for k in range(3):
        a0 = breaks[k] + math.radians(11)
        a1 = breaks[(k + 1) % 3] - math.radians(11) + (2 * math.pi if k == 2 else 0)
        mid = (a0 + a1) / 2
        nudge = (0.012, -0.01, 0.016)[k]
        cx, cy = 0.5 + nudge * math.cos(mid), 0.5 + nudge * math.sin(mid)
        outer = A.circle_pts(cx, cy, rad + hw, 48, a0, a1)
        inner = A.circle_pts(cx, cy, rad - hw, 48, a1, a0, ccw=False)
        end1 = [(cx + rad * math.cos(a1 + jag), cy + rad * math.sin(a1 + jag))]
        end0 = [(cx + rad * math.cos(a0 - jag * 0.4), cy + rad * math.sin(a0 - jag * 0.4))]
        c.poly(outer + end1 + inner + end0)
    # The slash, top-left to bottom-right like every "off" sign.
    c.capsule(0.22, 0.78, 0.78, 0.22, hw)
    raw = A.render(cell, cell, samples=32)
    cov = raw[..., 3]
    rgb = np.clip(raw[..., :3] * 0.88, 0, 1)
    glyph = np.concatenate([rgb * cov[..., None], cov[..., None]], -1)
    rim = A.solid(np.clip(A.blur(A.dilate(cov, 3.0), 2.2) * 0.62, 0, 0.62), (0.0, 0.02, 0.05))
    img2 = A.unpremul(A.over(rim, glyph))
    img1 = A.unpremul(A.resize(A.premul(img2), cell // 2, cell // 2))
    V.save_png(os.path.join(UI_OUT, "UI_Dampened@2x.png"), img2)
    V.save_png(os.path.join(UI_OUT, "UI_Dampened.png"), img1)
    meta = {"file": "UI_Dampened.png", "file_2x": "UI_Dampened@2x.png", "size": [128, 128], "size_2x": [256, 256],
            "folder": "ArtSource/Exports/UI", "colour_space": "sRGB, straight alpha",
            "style": "UI_Icons stroke weights, bevel and soft dark rim (build_ui.py); white, tint with Image.color",
            "tint": "violet (0.72, 0.45, 1.0), the Sealed One's adaptation colour, or white",
            "use": "Image over the dampened tower's picture beside the boss bar (same rect, preserve aspect); the "
                   "picture itself greyed as now; pulse its alpha at the Adapt pulse frame",
            "replaces": "the plain grey-tinted HUD card (MODEL_TODO ronde 4c 'Gedempt-icoon op de bossbalk')"}
    REPORT["UI_Dampened"] = meta
    return img2


# ------------------------------------------------------------------------------------------------------- previews

def ground_tone(h, w, seed=9):
    x, y = V.grid(h, w)
    p = V.Perlin(seed)
    g = 0.5 + 0.5 * p.fbm(x * 10, y * 10 * h / w, 0, octaves=5)
    return np.stack([0.02 + 0.018 * g, 0.02 + 0.017 * g, 0.024 + 0.018 * g], -1)


def sample_bilinear(img, u, v, wrap_u=True):
    h, w = img.shape[:2]
    fx = u * w - 0.5
    fy = np.clip(v * h - 0.5, 0, h - 1)
    x0 = np.floor(fx).astype(int)
    y0 = np.floor(fy).astype(int)
    ax, ay = (fx - x0)[..., None], (fy - y0)[..., None]
    x1 = x0 + 1
    if wrap_u:
        x0, x1 = x0 % w, x1 % w
    else:
        x0, x1 = np.clip(x0, 0, w - 1), np.clip(x1, 0, w - 1)
    y1 = np.clip(y0 + 1, 0, h - 1)
    a = img[y0, x0] * (1 - ax) + img[y0, x1] * ax
    b = img[y1, x0] * (1 - ax) + img[y1, x1] * ax
    return a * (1 - ay) + b * ay


def along_path(H, W, pts, width):
    """Per pixel: (u along in px, signed v across in px, mask) for the nearest segment of a polyline."""
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    best = np.full((H, W), 1e9)
    s_out = np.zeros((H, W))
    d_out = np.zeros((H, W))
    cum = 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        dx, dy = bx - ax, by - ay
        L = math.hypot(dx, dy)
        t = np.clip(((xx - ax) * dx + (yy - ay) * dy) / (L * L), 0, 1)
        qx, qy = xx - ax - t * dx, yy - ay - t * dy
        dist = np.hypot(qx, qy)
        sign = np.sign(dx * qy - dy * qx)
        closer = dist < best
        best = np.where(closer, dist, best)
        s_out = np.where(closer, cum + t * L, s_out)
        d_out = np.where(closer, sign * dist, d_out)
        cum += L
    return s_out, d_out, (best < width * 0.6) & (s_out > 0.5) & (s_out < cum - 0.5)


def lay_strip(canvas, glow_acc, name, pts, width, gain=3.0, cool=0.0, mode="lit"):
    alb, em, tint = DECALS[name]
    H, W = canvas.shape[:2]
    s, d, m = along_path(H, W, pts, width)
    u = s / (width * SA)
    v = 0.5 + d / width
    ok = m & (v > 0) & (v < 1)
    a = sample_bilinear(alb, u, v)
    e = sample_bilinear(em, u, v)
    alpha = np.where(ok, a[..., 3], 0)[..., None]
    canvas[:] = canvas * (1 - alpha) + to_lin(a[..., :3]) * alpha
    lit = e[..., 0] * ss(cool - 0.08, cool, e[..., 1]) if cool > 0 else e[..., 0]
    glow_acc += np.where(ok, lit, 0)[..., None] * np.array(tint) * gain


def lay_cell(canvas, glow_acc, name, cx, cy, size, cell=0, gain=2.0, grow=1.0):
    alb, em, tint = DECALS[name]
    H, W = canvas.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    u = (xx - cx) / size + 0.5
    v = (yy - cy) / size + 0.5
    ok = (u > 0) & (u < 1) & (v > 0) & (v < 1)
    r_, q_ = divmod(cell, 2)
    a = sample_bilinear(alb, np.clip((u + q_) / 2, 0, 0.999), np.clip((v + r_) / 2, 0, 0.999), wrap_u=False)
    e = sample_bilinear(em, np.clip((u + q_) / 2, 0, 0.999), np.clip((v + r_) / 2, 0, 0.999), wrap_u=False)
    show = ss(grow, grow - 0.05, e[..., 1]) if grow < 1 else 1.0
    alpha = np.where(ok, a[..., 3] * show, 0)[..., None]
    canvas[:] = canvas * (1 - alpha) + to_lin(a[..., :3]) * alpha
    glow_acc += np.where(ok, e[..., 0] * show, 0)[..., None] * np.array(tint) * gain


def curve(x0, y0, x1, y1, bend, n=40):
    pts = []
    for i in range(n + 1):
        t = i / n
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t + bend * math.sin(t * math.pi * 1.6)
        pts.append((x, y))
    return pts


def field_mock():
    """A top-down field (ground plane, then squashed by the game camera's 44 degree tilt) with every strip laid
    along a curved path, the old road sinking, and frost cells growing."""
    H0, W = 1000, 1400
    can = ground_tone(H0, W)
    # A road band for the sink decal.
    road = curve(-50, 160, 1450, 120, 40)
    s, d, m = along_path(H0, W, road, 260)
    rd = ss(130, 110, np.abs(d))[..., None]
    can = can * (1 - rd) + rd * np.array((0.11, 0.105, 0.1))
    glow = np.zeros_like(can)
    lay_strip(can, glow, "RoadSink", road, 220, gain=1.5)
    lay_strip(can, glow, "LavaTrail", curve(60, 420, 700, 380, 60), 100, gain=3.5)
    lay_strip(can, glow, "LavaTrail", curve(60, 560, 700, 520, -40), 100, gain=3.5, cool=0.6)
    lay_strip(can, glow, "GroundCrack", curve(740, 330, 1380, 520, 80), 150, gain=4.0)
    lay_strip(can, glow, "Furrow", curve(60, 760, 700, 720, 50), 200, gain=1.5)
    lay_strip(can, glow, "RotTrail", curve(760, 760, 1380, 830, -50), 110, gain=2.0)
    for i, (cx, cy, g) in enumerate(((870, 640, 1.0), (1020, 620, 0.6), (1170, 640, 0.3))):
        lay_cell(can, glow, "FrostCreep", cx, cy, 160, cell=i, gain=1.2, grow=g)
    img = V.tonemap(can + glow)
    img = V.resize(img, int(H0 * math.sin(math.radians(44))), W)
    os.makedirs(PREVIEW, exist_ok=True)
    V.save_png(os.path.join(PREVIEW, "bossfx_field.png"), img)


def decal_sheet():
    tiles = []
    for name, (alb, em, tint) in DECALS.items():
        h, w = alb.shape[:2]
        bg = ground_tone(h, w, 3)
        a = alb[..., 3:4]
        c = bg * (1 - a) + to_lin(alb[..., :3]) * a + em[..., 0:1] * np.array(tint) * 3.0
        img = V.tonemap(c)
        g = np.repeat(em[..., 1:2], 3, -1) * 0.8
        both = np.concatenate([img, np.full((8, w, 3), 0.1), g], 0)
        tiles.append(V.resize(both, int(both.shape[0] * 1024 / w), 1024) if w != 1024 else both)
    out = np.concatenate([np.concatenate([t, np.full((16, 1024, 3), 0.2)], 0) for t in tiles], 0)
    V.save_png(os.path.join(PREVIEW, "bossfx_decals.png"), out)


def sheet_preview():
    rows = []
    for name, (atlas, pv) in SHEETS.items():
        frames = [atlas[r * CELL:(r + 1) * CELL, q * CELL:(q + 1) * CELL] for r in range(4) for q in range(4)]
        tiles = []
        for f in frames[::2]:
            bg = ground_tone(CELL, CELL, 5)
            if pv[0] == "cg":
                tint = np.array(pv[1])
                extra = np.array((0.6, 0.9, 1.0)) if name != 'VineBurn' else np.array((0.0, 0.0, 0.0))
                bgc = bg * (1 - 0.85 * f[..., 2:3]) if name == 'VineBurn' else bg
                em = f[..., 1:2] * tint * 1.6 + f[..., 0:1] * np.array((1.0, 0.95, 0.85)) * 1.6 + f[..., 2:3] * extra
                tiles.append(V.tonemap(bgc + em))
            else:
                dark, bright, lava = np.array(pv[1]), np.array(pv[2]), pv[3]
                a = f[..., 3:4]
                rgb = (dark * (1 - f[..., 1:2]) + bright * f[..., 1:2]) * f[..., 0:1] * 1.6 + f[..., 2:3] * 0.6
                em = f[..., 1:2] * np.array((1.0, 0.4, 0.08)) * 3.0 * a if lava else 0
                tiles.append(V.tonemap(bg * (1 - a) + rgb + em))
        rows.append(np.concatenate(tiles, 1))
    out = np.concatenate([np.concatenate([r, np.full((6, r.shape[1], 3), 0.2)], 0) for r in rows], 0)
    V.save_png(os.path.join(PREVIEW, "bossfx_sheets.png"), out)


def icon_preview(img2):
    """The overlay over three stand-in tower pictures (greyed), at 2x and at 1x, on the HUD navy."""
    icons = os.path.join(UI_OUT, "UI_Icons@2x.png")
    tiles = []
    for idx in (20, 9, 15):
        bg = np.full((256, 256, 3), (0.03, 0.07, 0.12))
        try:
            import bpy
            im = bpy.data.images.load(icons, check_existing=True)
            w, h = im.size
            buf = np.empty(w * h * 4, np.float32)
            im.pixels.foreach_get(buf)
            atl = buf.reshape(h, w, 4)[::-1]
            r, q = divmod(idx, 8)
            ic = atl[r * 256:(r + 1) * 256, q * 256:(q + 1) * 256]
            grey = ic[..., :3].mean(-1, keepdims=True) * 0.55
            bg = bg * (1 - ic[..., 3:4] * 0.8) + grey * ic[..., 3:4] * 0.8
        except Exception:  # noqa: BLE001 - preview only
            pass
        a = img2[..., 3:4]
        tint = np.array((0.72, 0.45, 1.0))
        tiles.append(np.clip(bg * (1 - a) + img2[..., :3] * tint * a, 0, 1))
    row = np.concatenate(tiles, 1)
    small = V.resize(row, 128, 384)
    small = np.concatenate([small, np.zeros((128, row.shape[1] - 384, 3))], 1)
    V.save_png(os.path.join(PREVIEW, "bossfx_icon.png"), np.concatenate([row, small], 0))


def main():
    if "decals" in ONLY or "previews" in ONLY:
        lava_trail()
        ground_crack()
        furrow()
        rot_trail()
        road_sink()
        frost_creep()
    if "sheets" in ONLY:
        spore_cloud()
        vine_burn()
        shard_flash()
        dive_wakes()
    icon = dampened_icon() if "icon" in ONLY else None
    REPORT["_notes"] = {"round": 29, "script": "Tools/Blender/build_bosses29_fx.py",
                        "convention": "PNG row 0 = top (Unity v = 1). Decal albedo sRGB on, everything else sRGB off. "
                                      "Strips: U along the path (Repeat), V across (Clamp). Low/ = half size."}
    os.makedirs(OUT, exist_ok=True)
    rp = os.path.join(OUT, "Boss_FX_report.json")
    old = {}
    if os.path.exists(rp) and ONLY != {"decals", "sheets", "icon", "previews"}:
        with open(rp, encoding="utf-8") as fh:
            old = json.load(fh)
    old.update(REPORT)
    with open(rp, "w", encoding="utf-8") as fh:
        json.dump(old, fh, indent=2)
    if "previews" in ONLY:
        os.makedirs(PREVIEW, exist_ok=True)
        if DECALS:
            decal_sheet()
            field_mock()
        if SHEETS:
            sheet_preview()
        if icon is not None:
            icon_preview(icon)


main()
