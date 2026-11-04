"""Round 26: equirectangular reflection / ambient skies per theme (procedural numpy, no imagery).

Replaces the painted 32 px cubemap in Assets/_Project/Scripts/Levels/SkyProbe.cs:40 (Build(sky, horizon, ground)),
which only knows a sky colour, a horizon band and a dark ground. These skies keep that structure (dark zenith,
a brighter band in the light colour at the horizon, a dark ground hemisphere) and add what shiny metal, water,
ice and crystal can catch: faint nebula in the continent's palette, aurora curtains over the ice continents, a
low ember / seal / lava glow from below where the land itself lights the haze, and sparse stars.

Eight skies: the six continents and the two cutscene themes (Theme_Earth, Theme_Underground). The Core and the
Underground are caverns: a rough ceiling with crystal glints instead of a sky, lit from below.

Outputs ArtSource/Exports/Skies/ (2048 x 1024) and Low/ (512 x 256):
  Sky_<Theme>.png       sRGB, equirectangular (u = (azimuth + 180) / 360, top row = straight up), no alpha
  Skies_report.json     per sky: exposure to restore the HDR range, trilight ambient (sky / equator / ground) and
                        the brightest direction

Run:  blender --background --factory-startup --python Tools/Blender/build_skies.py -- [--out <dir>]
        [--renders <dir>] [--only CrystalPlains,Earth]
"""

import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import planet_kit as K  # noqa: E402
from planet_kit import F32, col, mix, smooth  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def _arg(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith("--") else True
    return default


OUT = _arg("--out", os.path.join(ROOT, "ArtSource", "Exports", "Skies"))
RENDERS = _arg("--renders", None)
ONLY = set(_arg("--only", "").split(",")) - {""}
W, H = 2048, 1024
# Values are designed in linear light where 1.0 = the brightest thing a sky shows (a horizon glow, an aurora
# core); the PNG stores them through a soft shoulder, the report's exposure restores the range in Unity.
SHOULDER = 0.6


def log(msg):
    K.log("skies", msg)


# Colours below are linear. `tint` keys come from the theme assets (sRGB) where one exists.
def lin(*rgb):
    return K.srgb_to_lin(col(*rgb))


SKIES = {
    "CrystalPlains": dict(
        theme="Theme_C1_01..06 (sky 0.32/0.32/0.46, light 0.94/0.94/1 x 1.25, fog 0.04/0.04/0.09)",
        zenith=lin(0.035, 0.035, 0.075), sky=lin(0.11, 0.105, 0.19), horizon=lin(0.42, 0.40, 0.62),
        horizon_width=9.0, ground=lin(0.05, 0.05, 0.085), ground_glow=lin(0.35, 0.22, 0.42), ground_glow_width=7.0,
        nebula=[(lin(0.55, 0.34, 0.75), 0.55), (lin(0.30, 0.36, 0.80), 0.45)], nebula_strength=0.30,
        stars=0.8, sun=(40.0, 34.0, lin(0.80, 0.80, 0.95), 9.0, 0.45), exposure=1.3),
    "AshenReach": dict(
        theme="no VisualTheme yet: round 17 Ashen Reach palette (smoke-brown night, ember cracks)",
        zenith=lin(0.035, 0.024, 0.02), sky=lin(0.12, 0.075, 0.05), horizon=lin(0.55, 0.30, 0.15),
        horizon_width=7.0, ground=lin(0.06, 0.035, 0.025), ground_glow=lin(0.85, 0.36, 0.10), ground_glow_width=6.0,
        nebula=[(lin(0.50, 0.26, 0.14), 0.6), (lin(0.36, 0.22, 0.20), 0.4)], nebula_strength=0.26, smoke=0.55,
        stars=0.35, sun=(30.0, 22.0, lin(1.0, 0.62, 0.32), 12.0, 0.4), exposure=1.4),
    "NorthernCrown": dict(
        theme="no VisualTheme yet: round 17 Northern Crown palette (blue-black ice, amber heat under it)",
        zenith=lin(0.02, 0.03, 0.06), sky=lin(0.07, 0.10, 0.17), horizon=lin(0.36, 0.44, 0.58),
        horizon_width=8.0, ground=lin(0.05, 0.065, 0.09), ground_glow=lin(0.65, 0.42, 0.16), ground_glow_width=4.0,
        nebula=[(lin(0.25, 0.38, 0.70), 0.7), (lin(0.40, 0.50, 0.75), 0.3)], nebula_strength=0.18,
        aurora=(lin(0.25, 0.95, 0.70), lin(0.35, 0.45, 0.95), 0.5, 22.0, 62.0), stars=1.0,
        sun=(-60.0, 18.0, lin(0.75, 0.85, 1.0), 10.0, 0.35), exposure=1.3),
    "Mirewood": dict(
        theme="no VisualTheme yet: round 17 Mirewood palette (teal bioluminescence, violet fungus, fog)",
        zenith=lin(0.03, 0.045, 0.05), sky=lin(0.07, 0.11, 0.115), horizon=lin(0.26, 0.42, 0.42),
        horizon_width=16.0, ground=lin(0.035, 0.05, 0.045), ground_glow=lin(0.20, 0.55, 0.50), ground_glow_width=10.0,
        nebula=[(lin(0.45, 0.30, 0.65), 0.55), (lin(0.20, 0.55, 0.52), 0.45)], nebula_strength=0.22, mist=0.55,
        stars=0.25, sun=(100.0, 26.0, lin(0.70, 0.90, 0.85), 16.0, 0.3), exposure=1.3),
    "SouthernCrown": dict(
        theme="no VisualTheme yet: round 17 Southern Crown palette (pale shattered ice, magenta-white veins)",
        zenith=lin(0.03, 0.03, 0.05), sky=lin(0.10, 0.10, 0.15), horizon=lin(0.48, 0.46, 0.56),
        horizon_width=8.0, ground=lin(0.06, 0.055, 0.075), ground_glow=lin(0.85, 0.30, 0.75), ground_glow_width=5.0,
        nebula=[(lin(0.70, 0.30, 0.65), 0.5), (lin(0.40, 0.45, 0.75), 0.5)], nebula_strength=0.24,
        aurora=(lin(0.95, 0.35, 0.85), lin(0.50, 0.38, 0.90), 0.42, -10.0, 58.0), veins=0.8, stars=0.7,
        sun=(150.0, 30.0, lin(0.95, 0.85, 1.0), 9.0, 0.4), exposure=1.35),
    "TheCore": dict(
        theme="no VisualTheme yet: round 17 The Core palette (indigo crystal glass, gold-white seal light, violet corruption)",
        cavern=True, zenith=lin(0.025, 0.02, 0.05), sky=lin(0.06, 0.05, 0.11), horizon=lin(0.38, 0.30, 0.40),
        horizon_width=10.0, ground=lin(0.12, 0.09, 0.08), ground_glow=lin(0.85, 0.66, 0.34), ground_glow_width=14.0,
        nebula=[(lin(0.55, 0.30, 0.85), 1.0)], nebula_strength=0.16, glints=(lin(1.0, 0.82, 0.45), lin(0.70, 0.45, 1.0)),
        stars=0.0, sun=None, exposure=1.5),
    "Earth": dict(
        theme="Theme_Earth (sky 0.36/0.30/0.24, light 1/0.9/0.76 x 2.5, fog 0.04/0.03/0.02): space around the dying Earth",
        zenith=lin(0.006, 0.006, 0.01), sky=lin(0.012, 0.011, 0.014), horizon=lin(0.16, 0.11, 0.07),
        horizon_width=4.0, ground=lin(0.06, 0.045, 0.03), ground_glow=lin(0.30, 0.20, 0.12), ground_glow_width=20.0,
        nebula=[(lin(0.40, 0.30, 0.25), 1.0)], nebula_strength=0.08, stars=1.4,
        sun=(-35.0, 12.0, lin(1.0, 0.86, 0.66), 5.0, 1.0), earthshine=True, exposure=2.0),
    "Underground": dict(
        theme="Theme_Underground (sky 0.04/0.042/0.055, accent 0.35/0.85/1, rim 1/0.55/0.3): the cave under the field",
        cavern=True, zenith=lin(0.012, 0.012, 0.016), sky=lin(0.025, 0.026, 0.034), horizon=lin(0.10, 0.10, 0.12),
        horizon_width=12.0, ground=lin(0.03, 0.03, 0.035), ground_glow=lin(0.35, 0.75, 0.95), ground_glow_width=10.0,
        nebula=[(lin(0.35, 0.80, 1.0), 0.7), (lin(1.0, 0.55, 0.30), 0.3)], nebula_strength=0.10,
        glints=(lin(0.40, 0.88, 1.0), lin(1.0, 0.60, 0.32)), stars=0.0, sun=None, exposure=1.6),
}


def directions():
    G = K.Grid(W, H)
    return G, G.p(), G.latf, G.lonf


def nebula_field(P, seed, stretch=(1, 1, 1)):
    Pq = K.warp(P, 0.35, 1.2, 4, seed)
    Pq = K.warp(Pq, 0.08, 5.0, 3, seed + 10)
    body = smooth(-0.1, 0.7, K.fbm(Pq, 1.6, 7, seed + 20, gain=0.55, stretch=stretch))
    fil = K.ridged(Pq, 3.5, 5, seed + 30, stretch=stretch)
    dust = smooth(0.1, 0.6, K.fbm(Pq, 4.0, 5, seed + 40))
    return body, fil, dust


def build_sky(name, p, G, P, lat, lon):
    log(name)
    seed = 1000 + 97 * list(SKIES).index(name)
    up = np.sin(np.radians(lat))
    alat = np.abs(lat)
    sky_t = smooth(0.0, 1.0, np.clip(up, 0, 1) ** 0.55)
    base = mix(p["sky"][None], p["zenith"][None], sky_t[:, None])
    hz = np.exp(-(lat / p["horizon_width"]) ** 2)
    hz_var = 0.75 + 0.25 * (K.fbm(P, 2.0, 4, seed + 1) * 0.5 + 0.5)
    rgb = base + p["horizon"][None] * (hz * hz_var)[:, None] * 0.7
    below = lat < 0
    gnd_t = smooth(0.0, 1.0, np.clip(-up, 0, 1) ** 0.5)
    ground = mix(p["horizon"][None] * 0.35, p["ground"][None], gnd_t[:, None])
    gglow = np.exp(-(np.minimum(lat, 0) / p["ground_glow_width"]) ** 2) * (lat < 2)
    gvar = smooth(-0.2, 0.6, K.fbm(P, 3.0, 4, seed + 2))
    ground = ground + p["ground_glow"][None] * (gglow * (0.4 + 0.6 * gvar))[:, None] * 0.55
    # A low ground texture (the land at the horizon, broken by its own glow) so the lower half is not a flat fill.
    gtex = 0.8 + 0.4 * (K.fbm(P, 9.0, 5, seed + 3) * 0.5 + 0.5)
    ground = ground * gtex[:, None]
    rgb = np.where(below[:, None], mix(rgb, ground, smooth(0.0, -4.0, lat)[:, None]), rgb)

    body, fil, dust = nebula_field(P, seed + 100)
    neb = np.zeros_like(rgb)
    for i, (c, w) in enumerate(p["nebula"]):
        sel = smooth(0.35, 0.65, K.fbm(P, 1.1, 3, seed + 200 + i) * 0.5 + 0.5) if len(p["nebula"]) > 1 else 1.0
        if i == 0 and len(p["nebula"]) > 1:
            sel = 1 - smooth(0.35, 0.65, K.fbm(P, 1.1, 3, seed + 201) * 0.5 + 0.5)
        neb += c[None] * (w * (body * (0.35 + 0.65 * fil ** 2.0)) * sel)[:, None]
    neb *= (1 - 0.7 * dust)[:, None]
    above = smooth(-2.0, 12.0, lat)
    rgb = rgb + neb * (p["nebula_strength"] * above)[:, None]

    if p.get("smoke"):
        smoke = smooth(0.0, 0.7, K.fbm(K.warp(P, 0.2, 2.0, 3, seed + 300), 3.0, 6, seed + 301, stretch=(1, 1, 2.5)) * 0.5 + 0.5)
        band = np.exp(-((lat - 10) / 22.0) ** 2)
        rgb = mix(rgb, rgb * 0.45 + p["horizon"][None] * 0.12, (smoke * band * p["smoke"])[:, None])

    if p.get("aurora"):
        a1, a2, strength, lat0, lat1 = p["aurora"]
        # Curtains: a wavy arc across the sky whose rays hang down from it, brightest at their lower edge.
        arc_lat = 0.5 * (lat0 + lat1) + 0.5 * (lat1 - lat0) * 0.45 * np.sin(np.radians(lon) * 1.0 + 0.6) \
            + 6.0 * K.fbm(P, 1.5, 3, seed + 400)
        rel = (lat - arc_lat)
        rays = K.fbm(K.warp(P, 0.05, 3.0, 2, seed + 410), 3.0, 4, seed + 411, stretch=(5.0, 5.0, 0.12)) * 0.5 + 0.5
        rays = 0.35 + 0.65 * smooth(0.3, 0.8, rays)
        curtain = smooth(-3.0, 1.5, rel) * np.exp(-np.clip(rel, 0, None) / 20.0)
        fold = 0.55 + 0.45 * np.sin(np.radians(lon) * 7.0 + 3.0 * K.fbm(P, 2.0, 3, seed + 412))
        aur = curtain * rays * fold * strength
        colour = mix(a1[None], a2[None], smooth(0.0, 22.0, rel)[:, None])
        rgb = rgb + colour * (aur * above)[:, None] * 0.6

    if p.get("veins"):
        v = smooth(0.95, 0.99, K.ridged(K.warp(P, 0.1, 2.0, 3, seed + 500), 2.5, 4, seed + 501))
        v *= smooth(-0.2, 0.4, K.fbm(P, 1.5, 3, seed + 502)) * smooth(5, 30, lat)
        rgb = rgb + p["ground_glow"][None] * (v * p["veins"] * 0.4)[:, None]

    if p.get("mist"):
        m = p["mist"] * np.exp(-(lat / 30.0) ** 2) * (0.7 + 0.3 * (K.fbm(P, 2.5, 4, seed + 600) * 0.5 + 0.5))
        rgb = mix(rgb, p["horizon"][None] * 0.6, (m * 0.6)[:, None])

    if p.get("cavern"):
        # A rocky dome: dark ribbed rock, hanging crystal clusters catching the light from below, the seal's glow
        # pooling on the lower walls. Darker than any open sky so metal reads as being underground.
        rock = K.ridged(K.warp(P, 0.15, 2.5, 3, seed + 700), 3.0, 4, seed + 701)
        rock_c = mix(p["zenith"][None] * 0.7, p["sky"][None] * 1.2, (rock ** 2.0)[:, None])
        wall_lit = np.exp(-(np.clip(lat, 0, None) / 25.0) ** 2)
        rock_c = rock_c + p["ground_glow"][None] * (wall_lit * (0.3 + 0.7 * rock) * 0.06)[:, None]
        dome = smooth(-3.0, 6.0, lat)
        rgb = mix(rgb, rock_c + neb * p["nebula_strength"], (dome * 0.85)[:, None])
        gl1, gl2 = p["glints"]
        cells = K.fbm(P, 90.0, 1, seed + 710) * 0.5 + 0.5
        cluster = smooth(0.55, 0.75, K.fbm(P, 6.0, 3, seed + 711) * 0.5 + 0.5)
        g = smooth(0.80, 0.9, cells) * cluster * dome
        pick = smooth(0.4, 0.6, K.fbm(P, 3.0, 2, seed + 712) * 0.5 + 0.5)
        rgb = rgb + mix(gl1[None], gl2[None], pick[:, None]) * (g * 0.9)[:, None]

    if p.get("stars"):
        rng = np.random.default_rng(seed + 800)
        n = int(2600 * p["stars"])
        z = rng.uniform(-0.15, 1, n)
        a = rng.uniform(-math.pi, math.pi, n)
        slat = np.degrees(np.arcsin(z))
        x = ((np.degrees(a) + 180) / 360 * W).astype(int) % W
        y = np.clip(((slat + 90) / 180 * H).astype(int), 0, H - 1)
        mag = (rng.random(n) ** 6) * 0.55 * smooth(-2, 15, slat)
        img = np.zeros((H, W), F32)
        np.add.at(img, (y, x), mag)
        img = img + K.blur_sphere(img, 0.08, G.lat1, 1) * 0.6
        tint = mix(col(1.0, 0.85, 0.7)[None], col(0.75, 0.85, 1.0)[None], rng.random(1)[:, None])
        rgb = rgb + img.ravel()[:, None] * tint * (1 - 0.6 * dust)[:, None]

    if p.get("sun"):
        slon, slat0, scol, size, strength = p["sun"]
        d = K.angle_to(P, slon, slat0)
        glow = np.exp(-(d / size) ** 2) * 0.7 + np.exp(-(d / (size * 3.5)) ** 2) * 0.3
        rgb = rgb + scol[None] * (glow * strength)[:, None] * (1 - 0.6 * below)[:, None]
        if p.get("earthshine"):
            core = smooth(1.6, 0.6, d)
            rgb = rgb + scol[None] * (core * 1.5)[:, None]

    if p.get("earthshine"):
        # Low under the camera the brown Earth fills the lower sky: smog-brown, lit on the sun side.
        es = smooth(-2.0, -30.0, lat) * (0.35 + 0.65 * (0.5 + 0.5 * np.cos(np.radians(lon + 35.0))))
        swirl = 0.75 + 0.5 * (K.fbm(P, 4.0, 5, seed + 900) * 0.5 + 0.5)
        rgb = mix(rgb, lin(0.36, 0.27, 0.19)[None] * swirl[:, None] * 0.6, (es * 0.85)[:, None])

    rgb = np.clip(rgb, 0, None).reshape(H, W, 3)
    return rgb


def encode(rgb):
    """Linear HDR -> sRGB PNG with a soft shoulder above SHOULDER (so glows keep their shape instead of clipping)."""
    over = np.clip(rgb - SHOULDER, 0, None)
    shaped = np.where(rgb > SHOULDER, SHOULDER + (1 - SHOULDER) * (1 - np.exp(-over / (1 - SHOULDER))), rgb)
    return K.lin_to_srgb(np.clip(shaped, 0, 1))


def ambient(rgb, lat1):
    w = np.cos(np.radians(lat1))[:, None]

    def band(lo, hi):
        m = ((lat1 >= lo) & (lat1 < hi))[:, None] * w
        return (rgb * m[..., None]).sum(axis=(0, 1)) / (m.sum() * rgb.shape[1])
    return {k: [round(float(v), 4) for v in K.lin_to_srgb(band(*r))] for k, r in
            {"sky": (30, 90), "equator": (-15, 30), "ground": (-90, -15)}.items()}


# ---------------------------------------------------------------------------------------------------------------
# Previews: an equirect contact sheet and, per sky, a mirror sphere and a matte sphere in front of the horizon
# ---------------------------------------------------------------------------------------------------------------

def view_dirs(size_x, size_y, fov=70.0, pitch=8.0):
    """Perspective camera looking at azimuth 0, slightly up. Returns world dirs (H, W, 3) in the Grid frame."""
    f = 1.0 / math.tan(math.radians(fov) / 2)
    xs = (np.arange(size_x) + 0.5) / size_x * 2 - 1
    ys = ((np.arange(size_y) + 0.5) / size_y * 2 - 1) * size_y / size_x
    X, Y = np.meshgrid(xs, -ys)
    d = np.stack([np.full_like(X, f), X, Y], -1)
    cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))
    d = np.stack([d[..., 0] * cp - d[..., 2] * sp, d[..., 1], d[..., 0] * sp + d[..., 2] * cp], -1)
    return d / np.linalg.norm(d, axis=-1, keepdims=True)


def sample_dir(img, d):
    lat = np.degrees(np.arcsin(np.clip(d[..., 2], -1, 1)))
    lon = np.degrees(np.arctan2(d[..., 1], d[..., 0]))
    return K.sample(img, (lon + 180) / 360, (lat + 90) / 180)


def preview(rgb, exposure, path):
    small = K.box_down(rgb, 4)
    lat1 = (np.arange(small.shape[0]) + 0.5) / small.shape[0] * 180 - 90
    irr = np.stack([K.blur_sphere(small[..., c], 45.0, lat1, 3) for c in range(3)], -1)
    sx, sy = 900, 420
    d = view_dirs(sx, sy)
    bg = sample_dir(rgb, d)
    out = bg.copy()
    for cx, mirror in ((0.32, True), (0.68, False)):
        r = 0.32 * sy
        yy, xx = np.mgrid[:sy, :sx]
        u = (xx - cx * sx) / r
        v = -(yy - 0.56 * sy) / r
        inside = u * u + v * v < 1
        nz = np.sqrt(np.clip(1 - u * u - v * v, 0, 1))
        # Sphere normal in world: viewer looks along +x, screen right = +y, up = +z.
        n = np.stack([-nz, u, v], -1)
        if mirror:
            view = np.array([1.0, 0, 0])
            refl = view - 2 * (n @ view)[..., None] * n
            c = sample_dir(rgb, refl) * np.array([0.75, 0.78, 0.82]) + 0.02
        else:
            c = sample_dir(irr, n) * np.array([0.6, 0.6, 0.62]) * 1.6
        out = np.where(inside[..., None], c, out)
    img = encode(out * exposure)
    K.write_png(path, img[::-1])
    return img


def main():
    G, P, lat, lon = directions()
    os.makedirs(os.path.join(OUT, "Low"), exist_ok=True)
    report = {"round": 26, "size": [W, H], "low_size": [W // 4, H // 4], "skies": {},
              "encoding": "sRGB PNG, no alpha. Designed in linear light (1.0 = the brightest glow), stored through a soft "
                          "shoulder above 0.6 so glows keep their falloff. Multiply by `exposure` (reflection intensity "
                          "or the cubemap's HDR tint) to restore the intended range.",
              "uv_convention": "Equirectangular: u = (azimuth + 180) / 360, v = 0.5 at the horizon, top row = straight up. "
                               "Import as Texture Shape Cube, Mapping Latitude-Longitude Layout (Cylindrical).",
              "replaces": "Assets/_Project/Scripts/Levels/SkyProbe.cs:40 Build(sky, horizon, ground) and its Apply(theme)"}
    sheet = []
    for name, p in SKIES.items():
        if ONLY and name not in ONLY:
            continue
        rgb = build_sky(name, p, G, P, lat, lon)
        png = encode(rgb)
        K.write_png(os.path.join(OUT, f"Sky_{name}.png"), png)
        K.write_png(os.path.join(OUT, "Low", f"Sky_{name}.png"), encode(K.box_down(rgb, 4)))
        flat = rgb.reshape(-1, 3)
        peak = np.unravel_index(np.argmax(rgb.sum(-1)), rgb.shape[:2])
        report["skies"][name] = {
            "file": f"Sky_{name}.png", "theme": p["theme"], "exposure": p["exposure"],
            "ambient_trilight_srgb": ambient(rgb * p["exposure"], G.lat1),
            "mean_linear": [round(float(v), 4) for v in flat.mean(0)],
            "brightest_direction": {"azimuth_degrees": round(float(G.lon1[peak[1]]), 1),
                                    "elevation_degrees": round(float(G.lat1[peak[0]]), 1)},
            "cavern": bool(p.get("cavern", False)),
        }
        if RENDERS:
            os.makedirs(RENDERS, exist_ok=True)
            preview(rgb, p["exposure"], os.path.join(RENDERS, f"sky_{name}_spheres.png"))
            sheet.append(K.box_down(png, 4)[::-1])
    with open(os.path.join(OUT, "Skies_report.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(report, f, indent=1)
    if RENDERS and sheet:
        K.write_png(os.path.join(RENDERS, "skies_contact.png"), K.contact(sheet, 2)[::-1])
    log("done")


if __name__ == "__main__":
    main()
