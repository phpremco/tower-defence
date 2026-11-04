"""Round 26: the exhausted Earth of 2053 as an equirectangular texture set (procedural numpy, no imagery).

Replaces the runtime painting in Assets/_Project/Scripts/UI/EarthPainter.cs (Paint(), line 54: Maps.Albedo /
Normal / Gloss / Emission and the Smog deck used by UI/Cutscene/EarthSet.cs:37). Same palette and story as that
painter: grey-brown continents, dead forest and dust belts, murky seas with silt plumes and algae, terraced
open-pit mining scars, shrunken dirty ice, dim amber cities of which most have gone dark, a brown smog deck.

The continents are hand-placed lobes in roughly the real arrangement (so the planet reads as Earth at a glance),
domain-warped and broken up with fractal coast noise; nothing is traced from maps or satellite imagery.

Conventions are the Planet_S69 ones (build_planet_s69.py): u = (longitude + 180) / 360 with east to the right,
v = (latitude + 90) / 180, PNG top row = north pole; identical to Game.Levels.PlanetCoordinates.ToUv.

Outputs ArtSource/Exports/Planet_Earth/ (4096 x 2048) and Low/ (2048 x 1024):
  Planet_Earth_Albedo.png    sRGB colour, unlit, no smog
  Planet_Earth_Height.png    16-bit grey, 0.5 = sea level, 1 = highest peak, 0 = deepest trench
  Planet_Earth_Normal.png    tangent space from the land relief only (sea flat): R = east, G = north (OpenGL)
  Planet_Earth_Mask.png      linear: R land, G wetness / specular (sea 1), B city glow strength, A ice
  Planet_Earth_Emission.png  sRGB: amber city lights (most dead), white-hot mine complexes, a few gas flares
  Planet_Earth_Clouds.png    RGBA smog deck: RGB smog colour (brown / dust / soot), A cover; wraps in longitude
  Planet_Earth_report.json

Run:  blender --background --factory-startup --python Tools/Blender/build_planet_earth.py -- [--res 4096]
        [--out <dir>] [--renders <dir>] [--norender]
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


RES = int(_arg("--res", 4096))
OUT = _arg("--out", os.path.join(ROOT, "ArtSource", "Exports", "Planet_Earth"))
RENDERS = _arg("--renders", None)
NORENDER = bool(_arg("--norender", False))


def log(msg):
    K.log("earth", msg)


# ----------------------------------------------------------------------------------------------------------------
# Layout: continent lobes (lon, lat, radius in degrees), carved seas and mountain ranges
# ----------------------------------------------------------------------------------------------------------------

LOBES = [
    # North America, Greenland
    (-100, 48, 15), (-113, 58, 12), (-90, 39, 10), (-80, 47, 8), (-100, 31, 8), (-110, 38, 9), (-121, 45, 7),
    (-76, 57, 7), (-63, 53, 6), (-152, 64, 7.5), (-139, 62, 7), (-97, 70, 7), (-80, 71, 5.5), (-110, 70, 5),
    (-81, 29, 2.6), (-103, 23, 6), (-96, 18, 4), (-89, 15, 3), (-84, 11, 2.4), (-79.5, 8.5, 1.8), (-77, 35, 4),
    (-68, 46, 4), (-123, 52, 4), (-130, 56, 3.5), (-42, 73, 9), (-41, 64, 4.5), (-30, 79, 5),
    # South America
    (-60, -8, 13), (-70, 1, 7), (-47, -7, 8), (-55, -21, 10), (-64, -31, 8), (-68, -41, 5), (-71, -49, 3.4),
    (-73, 6, 5), (-77, -6, 4.5), (-41, -12, 4.5), (-50, -27, 4.5), (-64, 9, 3.5),
    # Europe
    (10, 50, 7), (25, 52, 10), (42, 56, 10), (2, 47, 5), (-4, 40.5, 5), (13, 43, 2.4), (16, 40.5, 1.6),
    (22, 41, 3.2), (15, 62, 5), (25, 65, 5.5), (13, 67, 3.5), (-2, 53, 2.4), (-8, 53.2, 1.4), (-19, 65, 2.2),
    (30, 62, 6), (9, 56, 2),
    # Africa
    (15, 11, 13), (0, 13, 9), (30, 13, 9), (-9, 10, 6), (24, -5, 12), (28, -19, 10), (24, -28, 6), (39, 5, 6),
    (45, 9, 3.8), (49, 9, 2.4), (35, 25, 7), (15, 26, 10), (0, 25, 10), (-10, 21, 6), (8, 32, 4), (47, -19, 3.4),
    (12, -3, 6), (-14, 13, 3),
    # Middle East, Asia
    (45, 23, 8), (53, 21, 5), (33, 39, 5), (55, 32, 7), (63, 33, 6), (60, 52, 14), (80, 55, 15), (100, 58, 15),
    (120, 59, 13), (140, 63, 10), (160, 65, 8), (172, 66, 5), (75, 43, 10), (95, 41, 12), (110, 35, 11),
    (118, 29, 8), (105, 25, 6), (90, 70, 8), (108, 72, 6), (130, 68, 7), (78, 21, 8), (77, 13, 5), (80.7, 7.8, 1.5),
    (100, 16, 5), (106, 13, 4), (101.5, 5, 2.4), (127, 37, 2.4), (138, 36, 2.8), (141, 41, 2), (143, 44, 1.8),
    (159, 56, 3), (135, 48, 6), (122, 42, 5), (68, 26, 5), (88, 24, 5),
    # Maritime south-east Asia, Australia, New Zealand
    (102, -1, 3), (114, 0.5, 3.6), (120.5, -2, 2), (137, -5, 4.2), (145, -6.5, 2.6), (110, -7.3, 1.8),
    (122, 12, 2.4), (124, 8, 1.8), (121, 16, 1.5), (134, -25, 12), (122, -24, 7), (145, -22, 6), (146, -36, 3.5),
    (146.8, -42, 1.4), (132, -14, 4), (142, -13, 2.6), (175, -39, 1.9), (171, -44, 2), (117, -32, 3.5),
    # Antarctica (one cap with a ragged rim and the peninsula)
    (0, -90, 21), (60, -76, 8), (130, -74, 9), (-120, -76, 8), (-65, -71, 4), (-60, -66, 2.2), (20, -72, 6),
    (90, -70, 6), (160, -76, 6),
]

SEAS = [  # carved: (lon, lat, radius)
    (15, 37.5, 4.5), (25, 34.8, 3.5), (5, 39.5, 3.6), (32, 33.5, 2.5), (19, 35.5, 3), (34.5, 43.3, 2.6),
    (50.5, 42, 2.3), (51, 38.5, 1.6), (-86, 59, 6), (-90, 25, 5.2), (-84, 23, 2.5), (37, 23, 1.2), (39.5, 18, 1.2),
    (41.5, 14.5, 1.0), (51, 27, 1.5), (19.5, 58, 2.2), (21, 62, 1.6), (88, 15, 5), (139, -15, 2.4), (-70, 18, 2.5),
    (-66, 15, 2.5), (-62, 46, 1.6), (4, 55, 2), (128, 35, 0.9), (135, 40, 2.6), (121, 37, 1.6), (110, 15, 4),
    (60, 72, 2.8), (130, 53, 0.8), (-112, 27, 1.0), (60, 45, 1.1), (-170, 62, 2.5), (-110, 66, 1.5),
]

RANGES = [  # (polyline, width deg, amplitude)
    ([(-152, 62), (-135, 58), (-125, 51), (-115, 45), (-108, 38), (-106, 31), (-101, 21), (-96, 17)], 3.0, 0.75),
    ([(-77, 9), (-78, -3), (-75, -12), (-69, -19), (-69, -30), (-71, -40), (-73, -50)], 2.0, 1.0),
    ([(70, 36), (78, 33), (86, 29), (92, 28), (98, 28)], 2.2, 1.0),
    ([(78, 36), (90, 34), (100, 33)], 5.5, 0.55),
    ([(5, 45), (10, 46.5), (16, 47.2)], 1.2, 0.45),
    ([(60, 50), (59, 58), (64, 67)], 1.6, 0.25),
    ([(-86, 34), (-79, 38.5), (-71, 45)], 1.6, 0.25),
    ([(-8, 31), (2, 34.5), (9, 36)], 1.6, 0.35),
    ([(38, 12), (37, 3), (35, -4), (32, -11)], 2.4, 0.5),
    ([(45, 36), (52, 30), (57, 27)], 1.8, 0.4),
    ([(145, -15), (150, -27), (147, -37)], 1.6, 0.25),
    ([(7, 60), (14, 65), (20, 69)], 1.5, 0.3),
    ([(40, 43.5), (48, 41.5)], 1.2, 0.45),
    ([(70, 40), (80, 42), (90, 43), (100, 46)], 2.2, 0.4),
    ([(110, 52), (125, 60), (140, 63)], 2.6, 0.3),
    ([(18, -30), (28, -26), (30, -22)], 1.8, 0.2),
    ([(-48, 72), (-38, 70), (-36, 64)], 3.0, 0.35),
]

# Dried lake and salt basins (lon, lat, radius): the Earth has lost its inland waters.
SALT = [(60, 45, 2.6), (14, 13, 3.2), (137, -28.5, 2.2), (-113, 41, 1.6), (-67, -20, 1.5), (54, 33, 2.4),
        (8, 33, 1.4), (33, 46, 1.0), (90, 40, 2.5), (110, 42, 2.0), (22, -20, 2.0)]

# Narrow seas as polylines (line, half-width deg): the Mediterranean, the Red Sea and the like stay continuous.
SEA_LINES = [
    ([(-5.5, 36), (2, 37.8), (9, 38.5), (15, 37), (20, 34.8), (27, 34.6), (34, 33.8)], 2.6),
    ([(13, 44), (17, 42), (19, 40)], 1.2),
    ([(23, 39), (25.5, 37.5)], 1.6),
    ([(33.5, 28), (37, 23), (40, 17.5), (43, 13)], 1.1),
    ([(48.5, 29.8), (52, 27), (56, 26)], 1.3),
    ([(11, 55.2), (17, 56), (20, 58.5), (21, 61.5), (23, 64.5)], 1.5),
    ([(-80, 20), (-72, 17), (-63, 15)], 3.0),
    ([(-111, 30), (-108, 25)], 1.0),
]
SCARS = 52

DEEP_SEA = col(0.06, 0.085, 0.10)
MURKY_SEA = col(0.14, 0.16, 0.145)
SILT = col(0.21, 0.18, 0.13)
ALGAE = col(0.12, 0.165, 0.11)
DRY_LAND = col(0.34, 0.29, 0.23)
GREY_LAND = col(0.32, 0.31, 0.29)
DEAD_FOREST = col(0.19, 0.17, 0.13)
REMNANT = col(0.20, 0.22, 0.14)
DUST = col(0.47, 0.40, 0.30)
SALT_FLAT = col(0.56, 0.53, 0.47)
SPRAWL = col(0.30, 0.295, 0.29)
SCAR_RIM = col(0.52, 0.46, 0.37)
SCAR_FLOOR = col(0.16, 0.13, 0.11)
RUST_POND = col(0.42, 0.22, 0.10)
TOXIC_POND = col(0.24, 0.30, 0.18)
DIRTY_ICE = col(0.55, 0.55, 0.53)
SOOT = col(0.30, 0.29, 0.27)
ROCK = col(0.22, 0.21, 0.20)
CITY = col(1.0, 0.62, 0.25)
MINE_LIGHT = col(1.0, 0.86, 0.62)
FLARE = col(1.0, 0.36, 0.10)
SMOG = col(0.62, 0.50, 0.36)
SMOG_DUST = col(0.68, 0.58, 0.40)
SMOG_SOOT = col(0.36, 0.31, 0.26)


def lobe_field(P, lobes, k=0.18):
    """Smooth union of cones: 1 at a lobe's middle, 0 on its rim, negative outside."""
    acc = np.full(len(P[0]), -1.0, F32)
    for lon, lat, r in lobes:
        f = 1.0 - K.angle_to(P, lon, lat) / r
        # Polynomial smooth max keeps the joins between lobes round.
        h = np.clip(0.5 + 0.5 * (f - acc) / k, 0, 1)
        acc = mix(acc, f, h) + k * h * (1 - h)
    return acc


def sea_field(P, seas):
    """0 outside every carved sea, up to 1 in its middle."""
    acc = np.zeros(len(P[0]), F32)
    for lon, lat, r in seas:
        acc = np.maximum(acc, 1.0 - K.angle_to(P, lon, lat) / r)
    return acc


def sea_line_field(P, lines):
    acc = np.zeros(len(P[0]), F32)
    for poly, width in lines:
        acc = np.maximum(acc, 1.0 - K.poly_distance(P, [poly], 0.3) / width)
    return acc


def rng_points_on(mask_fn, count, seed, lat_range):
    rng = np.random.default_rng(seed)
    out = []
    tries = 0
    while len(out) < count and tries < count * 400:
        tries += 1
        lon = rng.uniform(-180, 180)
        lat = math.degrees(math.asin(rng.uniform(math.sin(math.radians(lat_range[0])), math.sin(math.radians(lat_range[1])))))
        if mask_fn(lon, lat):
            out.append((lon, lat))
    return out


def paint(W, H):
    G = K.Grid(W, H)
    P = G.p()
    n = W * H
    lat = G.latf
    alat = np.abs(lat)

    log("continents")
    Pw = K.warp(P, 0.055, 1.8, 4, 11)
    Pw = K.warp(Pw, 0.018, 7.0, 3, 21)
    Pw = K.warp(Pw, 0.006, 22.0, 3, 23)
    field = lobe_field(Pw, LOBES)
    carve = np.maximum(sea_field(Pw, SEAS), sea_line_field(Pw, SEA_LINES))
    field = np.minimum(field, 0.25 - carve * 1.6) if len(SEAS) else field
    coast_n = K.fbm(Pw, 9.0, 7, 31, gain=0.6) * 0.20 + K.fbm(Pw, 45.0, 4, 33, gain=0.6) * 0.07
    land_f = field + coast_n
    land = smooth(-0.012, 0.012, land_f)

    log("relief")
    rough = K.ridged(Pw, 6.0, 6, 41)
    hills = K.fbm(Pw, 8.0, 4, 43) * 0.5 + 0.5
    mount = np.zeros(n, F32)
    for poly, width, amp in RANGES:
        d = K.poly_distance(P, [poly])
        mount = np.maximum(mount, amp * np.exp(-(d / width) ** 2))
    interior = smooth(0.0, 0.6, land_f)
    # Relief rises from zero at the shore, so the coast is no cliff in the normal map.
    shore = smooth(0.0, 0.35, land_f)
    elev_land = (0.03 + 0.08 * interior + 0.08 * hills * interior + mount * (0.35 + 0.65 * rough) * 0.75) * shore

    # Mining scars: terraced open pits eaten into the land, more and wider than the old painter's.
    log("scars")
    lowres = K.Grid(720, 360)
    lowP = lowres.p()
    lw = K.warp(lowP, 0.055, 1.8, 4, 11)
    lw = K.warp(lw, 0.018, 7.0, 3, 21)
    lf = np.minimum(lobe_field(lw, LOBES), 0.25 - np.maximum(sea_field(lw, SEAS), sea_line_field(lw, SEA_LINES)) * 1.6).reshape(360, 720)

    def on_land(lon, lat_, margin=0.15):
        x = int((lon + 180) / 360 * 720) % 720
        y = min(359, max(0, int((lat_ + 90) / 180 * 360)))
        return lf[y, x] > margin

    scars = rng_points_on(lambda lo, la: on_land(lo, la), SCARS, 501, (-48, 66))
    rng = np.random.default_rng(503)
    pit = np.zeros(n, F32)
    terr = np.zeros(n, F32)
    rings = np.zeros(n, F32)
    ponds = np.zeros(n, F32)
    pond_kind = np.zeros(n, F32)
    scar_wobble = K.fbm(P, 18.0, 4, 505)
    # Terrace rims only show in places: benches are cut where the ore was, not as perfect rings.
    bench_breaks = smooth(-0.1, 0.3, K.fbm(P, 60.0, 3, 507))
    scar_info = []
    for i, (lon, lat0) in enumerate(scars):
        rad = rng.uniform(0.45, 1.5) * (1.8 if rng.random() < 0.12 else 1.0)
        stretch = rng.uniform(1.3, 2.6)
        ang = rng.uniform(0, math.pi)
        c, e, nn = K.frame(lon, lat0)
        dirx = e * math.cos(ang) + nn * math.sin(ang)
        diry = -e * math.sin(ang) + nn * math.cos(ang)
        dot = P[0] * c[0] + P[1] * c[1] + P[2] * c[2]
        near = dot > math.cos(math.radians(rad * stretch * 1.6))
        if not near.any():
            continue
        idx = np.nonzero(near)[0]
        px_, py_, pz_ = P[0][idx], P[1][idx], P[2][idx]
        ax = np.degrees(px_ * dirx[0] + py_ * dirx[1] + pz_ * dirx[2]) / stretch
        ay = np.degrees(px_ * diry[0] + py_ * diry[1] + pz_ * diry[2])
        d = np.sqrt(ax * ax + ay * ay) * (1 + 0.45 * scar_wobble[idx])
        share = np.clip(1.0 - d / rad, 0, 1)
        steps = rng.integers(4, 8)
        stair = np.floor(share * steps) / steps
        pit[idx] = np.maximum(pit[idx], share)
        terr[idx] = np.maximum(terr[idx], stair)
        rings[idx] = np.maximum(rings[idx], (1 - smooth(0.0, 0.18, np.abs((share * steps) % 1.0 - 0.0))) * (share > 0.02) * (1 - share * 0.6))
        # Tailings pond beside most pits: rust or toxic green.
        if rng.random() < 0.75:
            off = rng.uniform(1.1, 1.5) * rad
            pa = rng.uniform(0, 2 * math.pi)
            pc = c + (e * math.cos(pa) + nn * math.sin(pa)) * math.radians(off)
            pc /= np.linalg.norm(pc)
            pr = rad * rng.uniform(0.25, 0.45)
            dp = np.degrees(np.arccos(np.clip(px_ * pc[0] + py_ * pc[1] + pz_ * pc[2], -1, 1))) * (1 + 0.3 * scar_wobble[idx])
            pw = smooth(pr, pr * 0.7, dp)
            ponds[idx] = np.maximum(ponds[idx], pw)
            pond_kind[idx] = np.where(pw > 0.01, float(rng.random() < 0.55), pond_kind[idx])
        scar_info.append({"longitude": round(lon, 2), "latitude": round(lat0, 2), "radius_degrees": round(rad, 2),
                          "stretch": round(stretch, 2)})
    pit *= land
    terr *= land
    rings *= land * bench_breaks
    ponds *= land * (1 - smooth(0.0, 0.05, pit))

    log("climate")
    land_blur = K.blur_sphere(land.reshape(H, W), 4.0, G.lat1).ravel()
    coastal = land * smooth(0.98, 0.55, land_blur)
    inland = land * smooth(0.6, 0.97, land_blur)
    moist_n = K.fbm(Pw, 3.0, 5, 61)
    humid = (np.exp(-(lat / 11.0) ** 2) * 0.9 + np.exp(-((alat - 55) / 9.0) ** 2) * 0.8
             + 0.35 * coastal + 0.5 * moist_n)
    dry = (np.exp(-((alat - 24) / 9.0) ** 2) * 0.9 + 0.55 * inland * smooth(15, 35, alat) * smooth(55, 40, alat)
           - 0.4 * moist_n - 0.4 * humid * 0.5)
    salt = np.zeros(n, F32)
    for lon, lat0, r in SALT:
        d = K.angle_to(Pw, lon, lat0)
        salt = np.maximum(salt, smooth(r, r * 0.5, d * (1 + 0.25 * coast_n * 4)))
    salt *= land

    log("albedo")
    grain = K.fbm(Pw, 60.0, 3, 71) * 0.5 + 0.5
    pat = K.fbm(Pw, 7.0, 5, 73) * 0.5 + 0.5
    ground = mix(DRY_LAND[None], GREY_LAND[None], smooth(0.35, 0.65, pat)[:, None])
    forest = smooth(0.55, 0.85, humid) * (1 - smooth(0.3, 0.6, dry))
    ground = mix(ground, DEAD_FOREST[None], (forest * 0.9)[:, None])
    remnant = forest * smooth(0.62, 0.78, K.fbm(Pw, 18.0, 4, 75) * 0.5 + 0.5) * 0.7
    ground = mix(ground, REMNANT[None], remnant[:, None])
    ground = mix(ground, DUST[None], np.clip(smooth(0.35, 0.85, dry), 0, 1)[:, None] * 0.85)
    dune = K.ridged(Pw, 55.0, 2, 77, stretch=(1, 1, 0.4))
    ground = ground * (1 + 0.08 * (dune - 0.5) * smooth(0.4, 0.8, dry))[:, None]
    ground = mix(ground, SALT_FLAT[None], (salt * 0.75)[:, None])
    ground = mix(ground, ROCK[None], (smooth(0.25, 0.7, mount) * 0.6)[:, None])
    shade_rel = 0.82 + 0.36 * grain * (0.6 + 0.4 * rough)
    ground = ground * shade_rel[:, None]
    # Dry river beds: thin dark lines across the plains.
    rivers = smooth(0.92, 0.985, K.ridged(Pw, 11.0, 3, 79)) * land * (1 - smooth(0.2, 0.5, mount)) * (1 - salt)
    ground = mix(ground, ground * 0.62, (rivers * 0.7)[:, None])

    # Cities: sprawl grey patches where people lived (coasts, rivers, temperate), most of the light gone.
    habit = land * smooth(68, 55, alat) * (0.35 + coastal) * (1 - 0.7 * smooth(0.5, 0.85, dry)) * (1 - salt)
    cl = K.fbm(Pw, 5.0, 5, 81) * 0.5 + 0.5
    cluster = smooth(0.52, 0.68, cl) * habit
    speck = K.fbm(Pw, 220.0, 2, 83) * 0.5 + 0.5
    speck2 = K.fbm(Pw, 520.0, 1, 85) * 0.5 + 0.5
    sprawl = smooth(0.25, 0.7, cluster)
    ground = mix(ground, SPRAWL[None], (sprawl * 0.55 * (0.6 + 0.4 * speck))[:, None])

    # Mines.
    scar_col = mix(SCAR_RIM[None], SCAR_FLOOR[None], smooth(0.25, 0.95, terr)[:, None])
    ground = mix(ground, scar_col, (smooth(0.0, 0.12, pit) * 0.85)[:, None])
    ground = mix(ground, SCAR_RIM[None], (rings * 0.3)[:, None])
    pond_col = mix(TOXIC_POND[None], RUST_POND[None], pond_kind[:, None])
    ground = mix(ground, pond_col, ponds[:, None])

    # Seas: deep, shelf, silt plumes at river mouths and swirls of algae along the currents.
    log("seas")
    depth = smooth(0.0, 0.55, -land_f)
    shelf = 1 - smooth(0.0, 0.12, -land_f)
    sea = mix(DEEP_SEA[None], MURKY_SEA[None], shelf[:, None])
    Pc = K.warp(P, 0.06, 4.0, 4, 91)
    swirl = K.fbm(Pc, 9.0, 5, 93) * 0.5 + 0.5
    plume = shelf * smooth(0.5, 0.75, swirl) * smooth(0.45, 0.6, K.fbm(Pw, 4.0, 3, 95) * 0.5 + 0.5)
    sea = mix(sea, SILT[None], (plume * 0.7)[:, None])
    bloom = smooth(0.62, 0.8, K.fbm(Pc, 16.0, 4, 97) * 0.5 + 0.5) * smooth(0.2, 0.0, depth) * smooth(60, 30, alat)
    sea = mix(sea, ALGAE[None], (bloom * 0.6)[:, None])
    sea = sea * (0.92 + 0.12 * swirl)[:, None]
    albedo = mix(sea, ground, land[:, None])

    # Ice: shrunken, grey, streaked with soot; bare rock breaks through on Antarctica and Greenland.
    log("ice")
    ice_n = K.fbm(Pw, 8.0, 5, 101)
    south_ice = land * smooth(69, 75, -lat + ice_n * 7) * (1 - smooth(0.62, 0.74, K.fbm(Pw, 12.0, 4, 103) * 0.5 + 0.5))
    green = smooth(8.5, 4.0, K.angle_to(Pw, -40, 74) * (1 + 0.25 * ice_n))
    north_land_ice = land * np.maximum(green, smooth(80, 84, lat + ice_n * 5)) * \
        (1 - smooth(0.6, 0.7, K.fbm(Pw, 12.0, 4, 105) * 0.5 + 0.5))
    sea_ice = (1 - land) * smooth(84, 88.5, lat + ice_n * 5) * smooth(0.3, 0.6, K.fbm(Pw, 20.0, 5, 107) * 0.5 + 0.5)
    ice = np.clip(np.maximum(np.maximum(south_ice, north_land_ice), sea_ice), 0, 1)
    soot = smooth(0.55, 0.8, K.ridged(Pw, 14.0, 3, 109, stretch=(1, 1, 0.5)))
    ice_col = mix(DIRTY_ICE[None], SOOT[None], (soot * 0.45)[:, None]) * (0.9 + 0.15 * grain)[:, None]
    albedo = mix(albedo, ice_col, ice[:, None])

    # Elevation.
    elev = np.where(land > 0.5, 0.5 + 0.5 * np.clip(elev_land * (1 - 0.4 * ice), 0, 1),
                    0.5 - 0.48 * depth * (0.8 + 0.2 * (K.fbm(P, 6.0, 4, 111) * 0.5 + 0.5)))
    elev = mix(0.5 - 0.02 * depth, elev, land)
    # Pits step down in terraces; the tailings ponds sit a little low.
    elev = elev - terr * 0.03 * land - ponds * 0.004

    # Lights.
    log("lights")
    alive = smooth(0.42, 0.56, K.fbm(Pw, 11.0, 4, 121) * 0.5 + 0.5)
    city_core = smooth(0.2, 0.6, cluster) * smooth(0.5, 0.72, speck) * (0.4 + 0.6 * smooth(0.45, 0.8, speck2))
    dead_glim = smooth(0.1, 0.5, cluster) * smooth(0.8, 0.88, speck2) * 0.22
    lit = np.clip(city_core * alive * 0.95 + dead_glim, 0, 1) * (1 - ice) * (1 - smooth(0.0, 0.1, pit))
    emission = CITY[None] * lit[:, None]
    # Mines still run: a few hard white work lights on the pit floors and benches, not a ring.
    mine_lights = smooth(0.25, 0.6, pit) * smooth(0.86, 0.93, speck2) * land * 0.9
    emission = emission + MINE_LIGHT[None] * mine_lights[:, None]
    flares = smooth(0.72, 0.9, dry) * land * smooth(0.965, 0.985, speck2) * smooth(0.55, 0.7, K.fbm(Pw, 6.0, 3, 123) * 0.5 + 0.5)
    emission = emission + FLARE[None] * flares[:, None] * 0.9
    emission = np.clip(emission, 0, 1)
    glow = np.clip(emission.max(axis=1), 0, 1)

    # Mask.
    wet = np.where(land > 0.5, 0.12 + 0.25 * salt + 0.5 * ponds, 1.0 - 0.15 * plume - 0.2 * bloom)
    wet = mix(wet, 0.7, ice)
    mask = np.stack([land, np.clip(wet, 0, 1), glow, ice], axis=-1)

    # Smog deck.
    log("smog")
    x, y, z = [a.copy() for a in P]
    storms = [(-60, 38, 9), (15, 50, 10), (125, 34, 9), (-150, 48, 10), (165, -42, 10), (-25, -48, 11),
              (75, -40, 9), (-105, -35, 8), (95, 18, 6), (-45, 18, 5), (45, 62, 8)]
    for lon, lat0, rad in storms:
        c = K.vec(lon, lat0)
        d = np.degrees(np.arccos(np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1)))
        ang = (2.6 if lat0 > 0 else -2.6) * np.exp(-(d / rad) ** 2)
        ca, sa = np.cos(ang), np.sin(ang)
        dot = x * c[0] + y * c[1] + z * c[2]
        cx, cy, cz = c[1] * z - c[2] * y, c[2] * x - c[0] * z, c[0] * y - c[1] * x
        x, y, z = (x * ca + cx * sa + c[0] * dot * (1 - ca), y * ca + cy * sa + c[1] * dot * (1 - ca),
                   z * ca + cz * sa + c[2] * dot * (1 - ca))
    Pq = (x.astype(F32), y.astype(F32), z.astype(F32))
    Pq = K.warp(Pq, 0.16, 1.6, 4, 131)
    Pq = K.warp(Pq, 0.05, 6.0, 3, 133)
    iso = smooth(58.0, 78.0, alat)
    sheets = mix(K.fbm(Pq, 2.0, 7, 141, gain=0.55, stretch=(1, 1, 2.4)), K.fbm(Pq, 2.0, 7, 143, gain=0.55), iso)
    wisps = mix(K.ridged(Pq, 6.5, 4, 145, stretch=(1, 1, 3.0)), K.ridged(Pq, 6.5, 4, 147), iso)
    industry = K.blur_sphere((cluster + pit * 2).reshape(H, W), 6.0, G.lat1).ravel()
    dust_src = K.blur_sphere((smooth(0.4, 0.8, dry) * land).reshape(H, W), 7.0, G.lat1).ravel()
    belts = (0.18 * np.exp(-((lat - 38) / 13.0) ** 2) + 0.10 * np.exp(-((lat + 3) / 9.0) ** 2)
             + 0.08 * np.exp(-((lat + 35) / 12.0) ** 2) - 0.12 * smooth(62, 80, alat))
    dens = sheets + belts + 0.55 * industry + 0.35 * dust_src - 0.02
    cover = smooth(0.18, 0.7, dens)
    cover = np.maximum(cover, smooth(0.8, 0.95, wisps) * smooth(0.0, 0.25, dens) * 0.55)
    # A thin brown veil everywhere outside the clear polar air: the sky of 2053 is never quite clean.
    veil = 0.16 * smooth(-0.35, 0.35, sheets) * (1 - smooth(60, 80, alat))
    cover = np.clip(cover * 0.7 + veil * (1 - cover), 0, 0.75)
    tone = smooth(0.0, 0.4, dust_src)[:, None]
    smog_rgb = mix(SMOG[None], SMOG_DUST[None], tone)
    smog_rgb = mix(smog_rgb, SMOG_SOOT[None], smooth(0.1, 0.5, industry)[:, None] * 0.7)
    smog_rgb = smog_rgb * (0.9 + 0.15 * (K.fbm(Pq, 12.0, 3, 149) * 0.5 + 0.5))[:, None]
    clouds = np.concatenate([np.clip(smog_rgb, 0, 1), cover[:, None]], axis=1)

    def img(a):
        return a.reshape(H, W, -1) if a.ndim == 2 else a.reshape(H, W)

    stats = {
        "land_percent": round(float((land * np.cos(np.radians(lat))).sum() / np.cos(np.radians(lat)).sum() * 100), 2),
        "ice_percent": round(float((ice * np.cos(np.radians(lat))).sum() / np.cos(np.radians(lat)).sum() * 100), 2),
        "smog_mean_cover": round(float(cover.mean()), 3),
        "lit_land_share": round(float((lit > 0.1).sum() / max(1, (land > 0.5).sum())), 4),
        "scars": scar_info,
    }
    return {"albedo": img(albedo), "height": img(elev), "mask": img(mask), "emission": img(emission),
            "clouds": img(clouds), "land": img(land)}, stats


# ----------------------------------------------------------------------------------------------------------------
# Preview renderer (orthographic, Unity's convention: east to the right, north up)
# ----------------------------------------------------------------------------------------------------------------

def render(maps, normal, lon0, lat0, size=1100, light=(-0.35, 0.45, 0.82), night=False, smog_k=1.0, zoom=1.0):
    alb = K.srgb_to_lin(maps["albedo"])
    span = 1.12 / zoom
    s = (np.arange(size) + 0.5) / size * 2 - 1
    sx, sy = np.meshgrid(s * span, -s * span)
    r2 = sx * sx + sy * sy
    hit = r2 < 1
    sz = np.sqrt(np.clip(1 - r2, 0, 1))
    c, e, n = K.frame(lon0, lat0)
    d = c[None, None] * sz[..., None] + e[None, None] * sx[..., None] + n[None, None] * sy[..., None]
    lat = np.degrees(np.arcsin(np.clip(d[..., 2], -1, 1)))
    lon = np.degrees(np.arctan2(d[..., 1], d[..., 0]))
    u, v = (lon + 180) / 360, (lat + 90) / 180
    A = K.sample(alb, u, v)
    Nt = K.sample(normal, u, v)
    M = K.sample(maps["mask"], u, v)
    Em = K.sample(K.srgb_to_lin(maps["emission"]), u, v)
    Cl = K.sample(maps["clouds"], u, v)
    lo, la = np.radians(lon), np.radians(lat)
    E = np.stack([-np.sin(lo), np.cos(lo), np.zeros_like(lo)], -1)
    Nn = np.stack([-np.sin(la) * np.cos(lo), -np.sin(la) * np.sin(lo), np.cos(la)], -1)
    wn = E * Nt[..., :1] + Nn * Nt[..., 1:2] + d * Nt[..., 2:3]
    wn /= np.linalg.norm(wn, axis=-1, keepdims=True) + 1e-6
    basis = np.stack([e, n, c])
    vn = wn @ basis.T
    gn = np.stack([sx, sy, sz], -1)
    L = np.array(light, F32)
    L /= np.linalg.norm(L)
    ndl_g = (gn * L).sum(-1)
    day = smooth(-0.1, 0.22, ndl_g)
    ndl = np.clip((vn * L).sum(-1), 0, 1)
    sun = np.array([1.0, 0.86, 0.68], F32)  # the light reaches the ground through smog
    Hh = L + np.array([0, 0, 1], F32)
    Hh /= np.linalg.norm(Hh)
    spec = np.power(np.clip((gn * Hh).sum(-1), 0, 1), 400) * M[..., 1] * (1 - M[..., 0]) * 0.12 * day
    colr = A * (ndl * day)[..., None] * sun * 1.35 + A * 0.012 + spec[..., None] * np.array([1.0, 0.8, 0.55])
    cov = Cl[..., 3] * smog_k
    shadow = K.sample(maps["clouds"][..., 3], u - L[0] * 0.003, v - L[1] * 0.003) * smog_k
    colr *= (1 - 0.35 * shadow[..., None] * day[..., None])
    smog_lit = K.srgb_to_lin(Cl[..., :3]) * (np.clip(ndl_g * 0.85 + 0.15, 0, 1) * day)[..., None] * sun * 1.2
    colr = mix(colr, smog_lit, cov[..., None])
    glow_k = (mix(0.2, 2.2, 1 - day) if night else np.full(day.shape, 0.15, F32))
    colr += Em * (glow_k * (1 - 0.65 * cov))[..., None] * 1.6
    fres = np.power(1 - sz, 3.5)
    haze = np.array([0.55, 0.36, 0.18], F32)  # a tan-orange limb instead of a blue one
    atmo = haze * (fres * (0.02 + 0.98 * smooth(-0.2, 0.45, ndl_g)))[..., None] * 0.75
    twil = np.array([0.9, 0.32, 0.12]) * (np.exp(-((ndl_g - 0.02) / 0.06) ** 2) * np.power(1 - sz, 4.0) * 0.12)[..., None]
    colr = colr + atmo + twil
    rr = np.sqrt(r2)
    halo = np.exp(-np.clip(rr - 1, 0, None) / 0.03) * (rr >= 1)
    hdl = smooth(-0.4, 0.5, (np.stack([sx, sy, np.zeros_like(sx)], -1) / np.maximum(rr, 1e-6)[..., None] * L).sum(-1))
    bg = np.array([0.004, 0.004, 0.008]) + haze * (halo * (0.15 + 0.85 * hdl))[..., None] * 0.5
    rng = np.random.default_rng(3)
    stars = (rng.random((size, size)) > 0.9985) * rng.random((size, size)) * 0.5
    bg = bg + stars[..., None]
    out = np.where(hit[..., None], colr, bg)
    out = out / (1 + out * 0.35) * 1.2
    return K.lin_to_srgb(out)[::-1]


def main():
    W, H = RES, RES // 2
    maps, stats = paint(W, H)
    log("normal")
    relief = np.where(maps["land"] > 0.5, maps["height"], 0.5)
    normal = K.normal_map((relief - 0.5) * 2.0, 0.18)
    low = os.path.join(OUT, "Low")
    os.makedirs(low, exist_ok=True)

    def save(name, a, bits=8):
        K.write_png(os.path.join(OUT, f"Planet_Earth_{name}.png"), a, bits)
        K.write_png(os.path.join(low, f"Planet_Earth_{name}.png"), K.box_down(a, 2), bits)

    log("write")
    save("Albedo", maps["albedo"])
    save("Height", maps["height"], 16)
    lown = K.box_down(normal, 2)
    lown /= np.linalg.norm(lown, axis=-1, keepdims=True)
    K.write_png(os.path.join(OUT, "Planet_Earth_Normal.png"), normal * 0.5 + 0.5)
    K.write_png(os.path.join(low, "Planet_Earth_Normal.png"), lown * 0.5 + 0.5)
    save("Mask", maps["mask"])
    save("Emission", maps["emission"])
    save("Clouds", maps["clouds"])

    report = {
        "name": "Planet_Earth",
        "round": 26,
        "size": [W, H],
        "low_size": [W // 2, H // 2],
        "uv_convention": "Equirectangular, identical to Planet_S69 and Game.Levels.PlanetCoordinates.ToUv: u = (longitude + 180) / 360 with east to the right, v = (latitude + 90) / 180, PNG top row = north pole.",
        "files": {
            "Albedo": "sRGB colour, unlit, no smog. Import sRGB, wrap U Repeat / V Clamp, aniso 8.",
            "Height": "16-bit grey, linear. 0.5 = sea level, 0.5..1 land, 0..0.5 sea floor. Pits are stepped (terraces).",
            "Normal": "Tangent space from the land relief only (sea flat). R = +east, G = +north (OpenGL / Unity, same as GlobePainter.Normals). Import as Normal map.",
            "Mask": "Linear. R land 0/1 (antialiased), G wetness / specular (open sea 1, silted sea ~0.85, ice 0.7, tailings ponds ~0.6, salt ~0.37, land 0.12), B glow strength (max of Emission), A ice cover.",
            "Emission": "sRGB glow: dim amber cities (about two thirds already dark), white-hot mine complexes on the pit rims, a few orange gas flares. Multiply by an HDR intensity; strongest on the night side.",
            "Clouds": "RGBA smog deck. RGB = smog colour (brown, yellow dust over deserts, dark soot over the industrial belts), A = cover (max 0.82, mean ~0.3). Wraps in longitude. Unlike Planet_S69_Clouds the colour is NOT white: use RGB as the deck tint, A as coverage.",
        },
        "replaces": {
            "code": "Assets/_Project/Scripts/UI/EarthPainter.cs:54 Paint(width) (whole painter) used by UI/Cutscene/EarthSet.cs:37",
            "Maps.Albedo": "Planet_Earth_Albedo",
            "Maps.Normal": "Planet_Earth_Normal",
            "Maps.Gloss": "EarthPainter stores smoothness in Gloss.a (sea 0.25, land 0.08, ice 0.3). Build it from Mask.G: smoothness = 0.05 + 0.22 * Mask.G (sea 0.27, land 0.08, ice 0.20); a one-off editor step can write an RGBA gloss texture with A = that, or the shader reads Mask.g directly with that remap.",
            "Maps.Emission": "Planet_Earth_Emission",
            "Smog": "Planet_Earth_Clouds (A = density, already scaled like EarthPainter's density * SmogCover; keep SmogAlpha in EarthSet)",
            "CutsceneCast.EarthSurface": "can point at Planet_Earth_Albedo (it already overrides Maps.Albedo)",
        },
        "stats": stats,
    }
    with open(os.path.join(OUT, "Planet_Earth_report.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(report, f, indent=1)

    if NORENDER or not RENDERS:
        log("done")
        return
    os.makedirs(RENDERS, exist_ok=True)
    log("renders")
    small = {k: K.box_down(v, 2) if k != "land" else v for k, v in maps.items()}
    small["height"] = None
    nrm = lown
    shots = [("earth_day_atlantic", -25, 22, (-0.35, 0.45, 0.82), False),
             ("earth_day_asia", 95, 28, (-0.35, 0.45, 0.82), False),
             ("earth_terminator_europe", 20, 38, (-0.85, 0.25, 0.3), True),
             ("earth_night_americas", -85, 25, (0.95, 0.1, -0.45), True),
             ("earth_day_pacific", -160, 0, (-0.35, 0.45, 0.82), False)]
    for name, lon, lat, light, night in shots:
        K.write_png(os.path.join(RENDERS, name + ".png"), render(small, nrm, lon, lat, 1000, light, night))
    K.write_png(os.path.join(RENDERS, "earth_day_nosmog.png"), render(small, nrm, 20, 25, 1000, (-0.35, 0.45, 0.82), False, smog_k=0.0))
    K.write_png(os.path.join(RENDERS, "earth_closeup_mines.png"), render(small, nrm, 30, 45, 1000, (-0.5, 0.5, 0.7), False, smog_k=0.6, zoom=3.0))
    # Flat maps, 2048 x 1024, rows bottom-up for write_png.
    flat = K.box_down(maps["albedo"], 2)
    K.write_png(os.path.join(RENDERS, "earth_flat_albedo.png"), flat)
    em = K.box_down(maps["emission"], 2)
    K.write_png(os.path.join(RENDERS, "earth_flat_emission_x3.png"), np.clip(em * 3, 0, 1))
    cl = K.box_down(maps["clouds"], 2)
    comp = mix(flat, cl[..., :3], cl[..., 3:4])
    K.write_png(os.path.join(RENDERS, "earth_flat_with_smog.png"), comp)
    log("done")


if __name__ == "__main__":
    main()
