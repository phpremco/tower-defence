"""Round 26: the Underground cutscene as designed art (replaces the PropFactory cave of UI/Cutscene/UndergroundSet.cs
and the code-made eye quads of UI/Cutscene/EyeField.cs).

Story (Docs/STORY.md, intro shot 6): the camera dives into the ground; in the dark the crystals tremble with the drill;
something opens its eyes, then another pair, then hundreds. The eyes are the Burrowers waking in the walls.

Cave kit, in The Core's language (round 17: indigo-black crystal glass, gold veins) with the Underground theme's cold
cyan Crysium light (Exports/Underground/<Piece>/, Albedo/Normal/Emission/Mask like every env piece):
  Cave_Wall_A, Cave_Wall_B (with a deep crevice), Cave_Ceiling (hangs from its pivot), Cave_Stalactites,
  Cave_Floor_A, Cave_Floor_B (a glowing fissure), Cave_Boulder, Cave_CrystalCluster (the trembling light sources),
  Cave_Vein (a flat glowing vein to lay over rock)
Burrower eyes (Exports/Underground/Eye_Burrower_<A|B|C>/): one eye per mesh (a pair is two instances, the second
mirrored), lids, eyeball with painted iris, a wet cornea (second material), under 600 triangles. Shape keys Blink,
LookLeft, LookRight, LookUp, LookDown, and a vertex-colour rig (R upper lid, G lower lid, B eyeball) so an instanced
shader can blink and look without skinning.

Run: blender --background --factory-startup --python Tools/Blender/build_underground.py -- [--preview <dir>]
     [--part kit|eyes|scene|all] [--only Cave_Wall_A,...] [--norender]
"""

import json
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
import stage_kit as S  # noqa: E402

F32 = np.float32
ARGS = C.cli_args()
PART = ARGS.get("part", "all")
ONLY = ARGS.get("only")
BASE_EXPORTS = C.EXPORTS
OUT = os.path.join(BASE_EXPORTS, "Underground")
PREVIEW = ARGS.get("preview") or os.path.join(C.REPO, "ArtSource", "Previews", "Underground")
os.makedirs(PREVIEW, exist_ok=True)
T0 = time.time()


def log(*a):
    C.log(f"[cave {time.time() - T0:6.1f}s]", *a)


# ---------------------------------------------------------------------------------------------------------------
# Palette: The Core's crystal glass and gold, lit by the Underground theme's cyan Crysium
# ---------------------------------------------------------------------------------------------------------------

GLASS = K.pal((0.22, 0.2, 0.3), (0.1, 0.09, 0.15), (0.025, 0.02, 0.04), (0.015, 0.012, 0.03))
GLASS_DEEP = K.pal((0.16, 0.15, 0.22), (0.07, 0.065, 0.11), (0.02, 0.015, 0.03), (0.01, 0.01, 0.02))
GOLD = np.array((1.0, 0.74, 0.32))
CYAN = np.array((0.35, 0.85, 1.0))
EMBER = np.array((1.0, 0.45, 0.18))


def cave_rock(seed, veins=0.45, crack=0.5, pal=GLASS, cyan=0.0):
    # Gold veins sparse (a hint of The Core, not a scribble over every face); cyan light only in some cracks.
    return K.rock(pal, seed, veins=veins * 0.3, glow=GOLD * 0.8, strata=9.0, crack=crack, rough=0.55,
                  crack_glow=cyan * 0.7, crack_color=CYAN, dust=0.15)


def ice_shard(seed, strength=1.0):
    return Y.shard_style("ice", strength, seed=seed)


def pink_shard(seed, strength=0.8):
    return Y.shard_style("pink", strength, seed=seed)


# ---------------------------------------------------------------------------------------------------------------
# Kit builders (Blender local: x along the piece, -y the side that faces the cavern, z up)
# ---------------------------------------------------------------------------------------------------------------

def blob(b, centre, size, seed, style, depth=0.22, segments=(22, 14)):
    b.ellipsoid("Rock", Vector(centre), size, style=style, segments=segments, deform=C.chiselled(11, seed, depth))


def wall(seed, crevice=False):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    st = cave_rock(seed, veins=0.5, cyan=0.25)
    # A backing mass so the face is continuous, then boulders stacked up the face, the top ones leaning out.
    b.ellipsoid("Back", Vector((0, 0.9, 2.1)), (2.25, 0.8, 2.4), style=st, segments=(28, 18), deform=C.chiselled(9, seed, 0.12))
    rows = [(0.45, 1.15), (1.5, 1.0), (2.55, 0.95), (3.6, 0.85)]
    for k, (z, s) in enumerate(rows):
        n = 3 if k < 3 else 2
        for i in range(n):
            x = -1.6 + 3.2 * (i + 0.5) / n + rng.uniform(-0.25, 0.25)
            if crevice and abs(x) < 0.55:
                x += 0.75 * (1 if x >= 0 else -1)
            lean = -0.12 * k
            blob(b, (x, lean + rng.uniform(-0.1, 0.1), z + rng.uniform(-0.15, 0.15)),
                 (s * rng.uniform(0.75, 1.0), s * 0.62, s * rng.uniform(0.6, 0.8)), seed + 10 + k * 5 + i,
                 cave_rock(seed + 10 + k * 5 + i, veins=0.4, cyan=0.2 if k % 2 else 0.0))
    if crevice:
        # The crevice: two buttresses either side of a deep dark slot; veins of cyan light inside it.
        for side in (-1, 1):
            b.ellipsoid("Buttress", Vector((side * 0.7, -0.05, 2.0)), (0.5, 0.55, 2.1), style=cave_rock(seed + 40 + side, veins=0.6),
                        segments=(18, 18), deform=C.chiselled(9, seed + 41 + side, 0.16))
        b.ellipsoid("Slot", Vector((0, 0.6, 2.0)), (0.32, 0.25, 1.9), style=cave_rock(seed + 45, veins=0.2, pal=GLASS_DEEP, cyan=0.9),
                    segments=(14, 16))
    # Crysium breaking out of the seams: a cyan cluster low and one pink shard high.
    for i in range(3):
        a = rng.uniform(-0.6, 0.6)
        base = Vector((rng.uniform(-1.4, 1.4), -0.35, rng.uniform(0.3, 1.2)))
        b.shard("Shard", base, Vector((math.sin(a) * 0.5, -1.0, 0.6)), rng.uniform(0.05, 0.09), rng.uniform(0.3, 0.55),
                sides=6, twist=rng.uniform(-1, 1), flute=0.3, style=ice_shard(seed + 60 + i))
    b.shard("Shard", Vector((rng.uniform(-1.2, 1.2), -0.6, 3.2)), Vector((0.2, -1.0, -0.4)), 0.07, 0.45, sides=6,
            twist=0.8, flute=0.3, style=pink_shard(seed + 70))
    return b


def ceiling(seed):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    st = cave_rock(seed, veins=0.35, pal=GLASS_DEEP)
    b.ellipsoid("Slab", Vector((0, 0, -0.35)), (2.2, 2.2, 0.55), style=st, segments=(28, 14), deform=C.chiselled(10, seed, 0.12))
    for i in range(5):
        a = i / 5 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        r = rng.uniform(0.6, 1.6)
        blob(b, (math.cos(a) * r, math.sin(a) * r, -0.75), (rng.uniform(0.5, 0.8), rng.uniform(0.5, 0.8), 0.4), seed + 5 + i,
             cave_rock(seed + 5 + i, veins=0.3, pal=GLASS_DEEP))
    # Rock drips and crystal points hang from it.
    for i in range(6):
        x, y = rng.uniform(-1.6, 1.6), rng.uniform(-1.6, 1.6)
        L = rng.uniform(0.6, 1.3)
        pts = [(x, y, -0.8), (x + rng.uniform(-0.05, 0.05), y, -0.8 - L * 0.5), (x, y, -0.8 - L)]
        b.tube("Drip", pts, [0.22, 0.12, 0.02], sides=10, samples=4, style=cave_rock(seed + 20 + i, veins=0.6))
    for i in range(5):
        x, y = rng.uniform(-1.5, 1.5), rng.uniform(-1.5, 1.5)
        b.shard("Point", Vector((x, y, -0.7)), Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), -1)),
                rng.uniform(0.07, 0.12), rng.uniform(0.6, 1.1), sides=6, twist=rng.uniform(-1, 1), flute=0.35,
                style=(ice_shard if i % 3 else pink_shard)(seed + 40 + i))
    return b


def stalactites(seed):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    blob(b, (0, 0, -0.15), (0.45, 0.4, 0.3), seed, cave_rock(seed, veins=0.5))
    for i in range(6):
        a = i / 6 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        r = 0.0 if i == 0 else rng.uniform(0.12, 0.3)
        b.shard("Hang", Vector((math.cos(a) * r, math.sin(a) * r, -0.25)),
                Vector((math.cos(a) * 0.25 * (r > 0), math.sin(a) * 0.25 * (r > 0), -1)),
                rng.uniform(0.06, 0.1) * (1.4 if i == 0 else 1), rng.uniform(0.6, 1.0) * (1.5 if i == 0 else 1),
                sides=6, twist=rng.uniform(-1.2, 1.2), flute=0.35, style=ice_shard(seed + i + 1, 1.1))
    return b


def floor(seed, fissure=False):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    b.ellipsoid("Bed", Vector((0, 0, -0.2)), (2.2, 2.2, 0.3), style=cave_rock(seed, veins=0.25), segments=(28, 12),
                deform=C.chiselled(10, seed, 0.08))
    for i in range(7):
        x, y = rng.uniform(-1.6, 1.6), rng.uniform(-1.6, 1.6)
        if fissure and abs(y - 0.3 * x) < 0.45:
            continue
        s = rng.uniform(0.25, 0.6)
        blob(b, (x, y, 0.0), (s, s * rng.uniform(0.7, 1.0), s * 0.55), seed + 3 + i, cave_rock(seed + 3 + i, veins=0.3))
    for i in range(10):
        x, y = rng.uniform(-1.8, 1.8), rng.uniform(-1.8, 1.8)
        s = rng.uniform(0.07, 0.16)
        b.ellipsoid("Grit", Vector((x, y, 0.02)), (s, s, s * 0.6), style=cave_rock(seed + 30 + i, veins=0.0),
                    segments=(10, 6), deform=C.chiselled(6, seed + 30 + i, 0.2))
    if fissure:
        def lit(s_, idx, part):
            p = s_.p[idx]
            g = C.smoothstep(-0.3, 0.6, C.value_noise(p, 5, seed + 50))
            s_.albedo[idx] = CYAN * 0.25 * (0.6 + 0.4 * g)[:, None]
            s_.emit[idx] = np.clip(CYAN[None, :] * (0.6 + 0.5 * g)[:, None], 0, 1)
            s_.disp[idx] = 0.0
            s_.rough[idx] = 0.3
            s_.fam[idx] = E.FAM_CRYSTAL
            s_.flow[idx] = p[:, 0]
        pts = [(-2.0, -0.6 - 0.1, -0.05), (-1.0, -0.3, -0.04), (0.0, 0.05, -0.05), (1.0, 0.25, -0.04), (2.0, 0.6, -0.05)]
        b.tube("Fissure", pts, [0.05, 0.12, 0.16, 0.1, 0.04], sides=8, samples=5, style=lit, flatten=0.35)
        for i in range(4):
            t = rng.uniform(-1.6, 1.6)
            b.shard("Grow", Vector((t, 0.3 * t, 0.0)), Vector((rng.uniform(-0.4, 0.4), rng.uniform(-0.4, 0.4), 1)),
                    0.05, rng.uniform(0.2, 0.4), sides=6, twist=1.0, flute=0.3, style=ice_shard(seed + 60 + i))
    return b


def boulder(seed):
    b = C.PartBuilder()
    blob(b, (0, 0, 0.55), (0.95, 0.8, 0.7), seed, cave_rock(seed, veins=0.55, cyan=0.25), depth=0.25)
    blob(b, (0.55, 0.2, 0.25), (0.45, 0.42, 0.35), seed + 1, cave_rock(seed + 1, veins=0.4))
    return b


def crystal_cluster(seed):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    blob(b, (0, 0, 0.0), (0.45, 0.4, 0.22), seed, cave_rock(seed, veins=0.6, cyan=0.5))
    for i in range(8):
        a = i / 8 * 2 * math.pi + rng.uniform(-0.3, 0.3)
        r = 0.0 if i == 0 else rng.uniform(0.12, 0.32)
        out = Vector((math.cos(a), math.sin(a), 0))
        b.shard("Crystal", out * r + Vector((0, 0, 0.05)), out * (0.0 if i == 0 else rng.uniform(0.35, 0.7)) + Vector((0, 0, 1)),
                rng.uniform(0.07, 0.11) * (1.6 if i == 0 else 1), rng.uniform(0.4, 0.7) * (1.8 if i == 0 else 1),
                sides=6, twist=rng.uniform(-1.5, 1.5), flute=0.35,
                style=(pink_shard if i in (2, 5) else ice_shard)(seed + 10 + i, 1.2))
    return b


def vein(seed):
    b = C.PartBuilder()

    def lit(s_, idx, part):
        p = s_.p[idx]
        g = C.smoothstep(-0.3, 0.5, C.value_noise(p, 8, seed))
        s_.albedo[idx] = C.lerp3(np.array((0.05, 0.04, 0.08)), CYAN * 0.4, g)
        s_.emit[idx] = np.clip(C.lerp3(GOLD * 0.3, CYAN, g) * (0.5 + 0.5 * g)[:, None], 0, 1)
        s_.disp[idx] = 0.0
        s_.rough[idx] = 0.35
        s_.fam[idx] = E.FAM_CRYSTAL
        s_.flow[idx] = p[:, 0]
    pts = [(-1.0, 0.0, 0.0), (-0.5, 0.08, 0.0), (0.0, -0.05, 0.0), (0.5, 0.06, 0.0), (1.0, 0.0, 0.0)]
    b.tube("Vein", pts, [0.03, 0.08, 0.1, 0.07, 0.03], sides=8, samples=5, style=lit, flatten=0.3)
    b.tube("Branch", [(0.0, -0.05, 0.0), (0.3, -0.25, 0.0), (0.55, -0.32, 0.0)], [0.06, 0.04, 0.015], sides=8,
           samples=4, style=lit, flatten=0.3)
    return b


KIT = {
    "Cave_Wall_A": dict(fn=lambda: wall(26100), tris=1900, voxel=0.035, tex=1024),
    "Cave_Wall_B": dict(fn=lambda: wall(26200, crevice=True), tris=1950, voxel=0.035, tex=1024),
    "Cave_Ceiling": dict(fn=lambda: ceiling(26300), tris=1950, voxel=0.03, tex=1024),
    "Cave_Stalactites": dict(fn=lambda: stalactites(26400), tris=1000, voxel=0.012, tex=512),
    "Cave_Floor_A": dict(fn=lambda: floor(26500), tris=1500, voxel=0.03, tex=1024),
    "Cave_Floor_B": dict(fn=lambda: floor(26600, fissure=True), tris=1700, voxel=0.025, tex=1024),
    "Cave_Boulder": dict(fn=lambda: boulder(26700), tris=900, voxel=0.025, tex=512),
    "Cave_CrystalCluster": dict(fn=lambda: crystal_cluster(26800), tris=1500, voxel=0.01, tex=512),
    "Cave_Vein": dict(fn=lambda: vein(26900), tris=300, voxel=0.008, tex=512),
}
PIVOTS = {"Cave_Ceiling": "top of the slab (hangs down from it)", "Cave_Stalactites": "top (hangs down)"}
REPLACES = {
    "Cave_Wall_A": "UndergroundSet.BuildCave boulders (PropKind.Boulder x46, UndergroundSet.cs:125-132)",
    "Cave_Wall_B": "same; the crevice is where eye pairs sit",
    "Cave_Ceiling": "UndergroundSet.BuildCave spikes (PropKind.Spike x26 flipped, UndergroundSet.cs:140-145)",
    "Cave_Stalactites": "same (hanging crystal points)",
    "Cave_Floor_A": "UndergroundSet.BuildCave floor rocks (PropKind.Rock x18, UndergroundSet.cs:134-138)",
    "Cave_Floor_B": "same, with a glowing fissure",
    "Cave_Boulder": "loose boulders (PropKind.Boulder)",
    "Cave_CrystalCluster": "UndergroundSet.BuildCrystals (PropKind.Crystal x5, UndergroundSet.cs:149-175); keeps the lights",
    "Cave_Vein": "new: glowing veins laid over walls and floor",
}


def build_kit():
    built = {}
    C.EXPORTS = OUT
    try:
        for name, spec in KIT.items():
            if ONLY and name not in ONLY.split(","):
                continue
            C.reset_scene()
            asset = E.build(name, spec["fn"](), spec["tris"], voxel=spec["voxel"], smoothing=2, families=("rock", "crystal"),
                            emission=2.4, tex=spec["tex"], ao=0.4, extrusion=0.04, ray=0.12)
            K.strip_colour_layers(asset.low)
            C.export_fbx(os.path.join(OUT, name, name + ".fbx"), [asset.low], animated=False)
            dims = asset.low.dimensions
            C.write_report(name, {"asset": name, "round": 26, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                                  "texture_size": spec["tex"], "maps": ["Albedo", "Normal", "Emission", "Mask"],
                                  "mask": "R AO, G smoothness, B rock detail, A crystal detail", "uv": asset.uv_stats,
                                  "detail": asset.details, "size_m": [round(d, 3) for d in dims],
                                  "pivot": PIVOTS.get(name, "on the ground (z = 0), cavern side towards local +Z in Unity"),
                                  "replaces": REPLACES[name]})
            write_low(name, spec["tex"])
            C.save_blend("Underground_" + name)
            if not ARGS.get("norender"):
                kit_preview(asset, name)
            built[name] = asset.low_tris
    finally:
        C.EXPORTS = BASE_EXPORTS
    return built


def write_low(name, tex):
    d = os.path.join(OUT, name)
    for k in ("Albedo", "Normal", "Emission", "Mask"):
        path = os.path.join(d, f"{name}_{k}.png")
        if os.path.exists(path):
            img = S.load_png(path)
            ch = 4 if k == "Mask" else 3
            half = S.half(img[..., :ch])
            if k == "Normal":
                half = S.encode_normal(S.decode_normal(half))
            S.write_png(os.path.join(d, "Low", f"{name}_{k}.png"), half)


def kit_preview(asset, name):
    C.preview_world((0.012, 0.01, 0.018))
    g = C.ground(color=(0.03, 0.028, 0.04))
    rig = C.light_rig(scale=2.0, key_color=(0.6, 0.62, 0.8), rim_color=(0.35, 0.85, 1.0))
    dims = asset.low.dimensions
    bb = asset.low.bound_box
    centre = Vector((sum(c[0] for c in bb) / 8, sum(c[1] for c in bb) / 8, sum(c[2] for c in bb) / 8))
    if name in PIVOTS:
        g.location.z = -10
    size = max(dims.x, dims.y, dims.z * 1.2)
    cam = C.closeup_camera(centre, size * 1.8 + 0.3, azimuth=-150 if name.startswith("Cave_Wall") else -30,
                           elevation=-12 if name in PIVOTS else 24, lens=50)
    S.render(cam, os.path.join(PREVIEW, f"kit_{name}.png"), 640, 480, 48)


# ---------------------------------------------------------------------------------------------------------------
# Burrower eyes
# ---------------------------------------------------------------------------------------------------------------

EYES = {
    "A": dict(name="Eye_Burrower_A", iris=(1.0, 0.62, 0.15), iris_dark=(0.45, 0.16, 0.02), pupil="slit", slant=14.0,
              open_up=0.5, open_down=0.36, width=1.0, lid=(0.16, 0.05, 0.05), brow=0, note="amber, vertical slit pupil"),
    "B": dict(name="Eye_Burrower_B", iris=(1.0, 0.28, 0.08), iris_dark=(0.4, 0.05, 0.02), pupil="bar", slant=20.0,
              open_up=0.42, open_down=0.3, width=1.08, lid=(0.12, 0.04, 0.04), brow=2, note="ember, horizontal bar pupil, brow spines"),
    "C": dict(name="Eye_Burrower_C", iris=(0.9, 0.85, 0.2), iris_dark=(0.35, 0.3, 0.02), pupil="triple", slant=6.0,
              open_up=0.56, open_down=0.44, width=0.92, lid=(0.1, 0.06, 0.04), brow=0, note="sulphur, three pupils"),
}
BALL_R = 0.4
LID_R_UP = 0.445
LID_R_DOWN = 0.438
CORNEA_R = 0.405
BLINK_EXTRA = 0.06
LOOK_DEG = 22.0
TEX = 512
# Preview stand-in for EyeField.Emission (5 in the game, where the HDR bloom is softer than this preview's).
EYE_PREVIEW_EMISSION = 1.6


def sph(x_frac, alpha, radius, width):
    """Point on a sphere of `radius`: x = x_frac * width / 2 across, alpha = angle round the X axis from the
    front (-Y) towards +Z (up)."""
    x = x_frac * width * 0.5
    x = max(-radius * 0.97, min(radius * 0.97, x))
    rr = math.sqrt(max(radius * radius - x * x, 1e-6))
    return Vector((x, -math.cos(alpha) * rr, math.sin(alpha) * rr))


def almond(x_frac, top):
    """Half-height (as an angle) of the opening at this position across: an almond, fuller towards the inside."""
    t = 1 - abs(x_frac) ** 2.0
    skew = 1 + 0.12 * x_frac
    return top * max(t, 0) ** 0.75 * skew


class MeshData:
    def __init__(self):
        self.verts = []
        self.faces = []
        self.uvs = []          # per face: list of uv tuples
        self.mats = []
        self.rig = []          # per vertex (R upper lid, G lower lid, B eyeball)

    def v(self, co, rig=(0, 0, 0)):
        self.verts.append(Vector(co))
        self.rig.append(rig)
        return len(self.verts) - 1

    def f(self, idx, uv, mat=0):
        self.faces.append(tuple(idx))
        self.uvs.append(uv)
        self.mats.append(mat)


def grid(md, rows, cols, point, uvf, rig, mat=0, wrap=False, flip=False):
    """rows x cols grid of points point(i, j) with uv uvf(i, j) (atlas coordinates)."""
    idx = [[md.v(point(i, j), rig) for j in range(cols)] for i in range(rows)]
    jmax = cols if wrap else cols - 1
    for i in range(rows - 1):
        for j in range(jmax):
            j2 = (j + 1) % cols
            q = [idx[i][j], idx[i][j2], idx[i + 1][j2], idx[i + 1][j]]
            uvq = [uvf(i, j), uvf(i, j + 1), uvf(i + 1, j + 1), uvf(i + 1, j)]
            if flip:
                q.reverse()
                uvq.reverse()
            md.f(q, uvq, mat)
    return idx


def eye_mesh(spec):
    md = MeshData()
    W = spec["width"]
    up, down = spec["open_up"], spec["open_down"]
    # Eyeball: the front of a sphere (looking down -Y), rings from the front pole back to 100 degrees.
    seg, rings = 14, 6
    pole = md.v((0, -BALL_R, 0), (0, 0, 1))
    ring_idx = []
    for r in range(1, rings + 1):
        th = math.radians(100) * r / rings
        ring = []
        for s in range(seg):
            a = 2 * math.pi * s / seg
            ring.append(md.v((math.sin(th) * math.cos(a) * BALL_R, -math.cos(th) * BALL_R, math.sin(th) * math.sin(a) * BALL_R), (0, 0, 1)))
        ring_idx.append(ring)

    def ball_uv(i):
        co = md.verts[i]
        return (0.25 + co.x / BALL_R * 0.24, 0.75 + co.z / BALL_R * 0.24)
    for s in range(seg):
        s2 = (s + 1) % seg
        md.f([pole, ring_idx[0][s2], ring_idx[0][s]], [ball_uv(pole), ball_uv(ring_idx[0][s2]), ball_uv(ring_idx[0][s])])
    for r in range(rings - 1):
        for s in range(seg):
            s2 = (s + 1) % seg
            q = [ring_idx[r][s], ring_idx[r][s2], ring_idx[r + 1][s2], ring_idx[r + 1][s]]
            md.f(q, [ball_uv(i) for i in q])
    # Cornea: a clear bulge over the iris (material 1).
    cseg, crings = 14, 3
    cpole = md.v((0, -CORNEA_R * 1.04, 0), (0, 0, 1))
    crs = []
    for r in range(1, crings + 1):
        th = math.radians(38) * r / crings
        ring = []
        for s in range(cseg):
            a = 2 * math.pi * s / cseg
            bul = 1.04 - 0.04 * (r / crings) ** 1.5
            ring.append(md.v((math.sin(th) * math.cos(a) * CORNEA_R, -math.cos(th) * CORNEA_R * bul,
                              math.sin(th) * math.sin(a) * CORNEA_R), (0, 0, 1)))
        crs.append(ring)

    def c_uv(i):
        co = md.verts[i]
        return (0.75 + co.x / BALL_R * 0.24, 0.75 + co.z / BALL_R * 0.24)
    for s in range(cseg):
        s2 = (s + 1) % cseg
        md.f([cpole, crs[0][s2], crs[0][s]], [c_uv(cpole), c_uv(crs[0][s2]), c_uv(crs[0][s])], 1)
    for r in range(crings - 1):
        for s in range(cseg):
            s2 = (s + 1) % cseg
            q = [crs[r][s], crs[r][s2], crs[r + 1][s2], crs[r + 1][s]]
            md.f(q, [c_uv(i) for i in q], 1)

    # Lids: shells round the X axis. Upper lid from the opening edge up and back; lower lid down and back.
    ncol, nrow = 11, 4
    xs = [math.cos(math.pi * (1 - j / (ncol - 1))) for j in range(ncol)]       # -1..1, denser at the corners

    def lid(upper):
        R = LID_R_UP if upper else LID_R_DOWN
        top = up if upper else down
        sign = 1 if upper else -1
        rig = (1, 0, 0) if upper else (0, 1, 0)
        v0 = 0.25 if upper else 0.375

        def point(i, j):
            xf = xs[j]
            edge = almond(xf, top)
            if i == 0:
                # The lid margin: tucked in towards the ball, so the edge reads thick.
                return sph(xf, sign * edge, R - 0.011, W)
            back = math.radians(115)
            a = edge + (back - edge) * ((i - 1) / (nrow - 2)) ** 1.2
            return sph(xf, sign * a, R + 0.012 * math.sin(math.pi * (i - 1) / (nrow - 2)), W)

        def uvf(i, j):
            return ((j / (ncol - 1)) * 0.98 + 0.01, v0 + 0.01 + (i / (nrow - 1)) * 0.11)
        return grid(md, nrow, ncol, point, uvf, rig, flip=not upper)
    up_idx = lid(True)
    down_idx = lid(False)

    # Socket: a knotted chitin rim round the opening, set back, its inner ring under the lid roots.
    sseg = 20
    # Inner ring hugs the lids where they pass y = -0.16; the middle ring swells forward into a fleshy rim.
    rings_s = [(0.42, 0.38, -0.12, 0.0), (0.58, 0.5, -0.17, 0.0), (0.72, 0.6, 0.06, 0.0)]

    def spoint(i, j):
        a = 2 * math.pi * j / sseg
        ax, az, y, lift = rings_s[i]
        bump = 1 + 0.06 * math.sin(a * 5 + i)
        return Vector((math.cos(a) * ax * W * bump, y - lift, math.sin(a) * az * bump))

    def suv(i, j):
        return (j / sseg, 0.01 + i / (len(rings_s) - 1) * 0.22)
    sock = [[md.v(spoint(i, j)) for j in range(sseg + 1)] for i in range(len(rings_s))]
    for i in range(len(rings_s) - 1):
        for j in range(sseg):
            q = [sock[i][j], sock[i][j + 1], sock[i + 1][j + 1], sock[i + 1][j]]
            md.f(q[::-1], [suv(i, j), suv(i, j + 1), suv(i + 1, j + 1), suv(i + 1, j)][::-1])
    # The seam of the socket ring: merge the duplicate column at j = sseg onto j = 0 later (kept for the UV wrap).
    # Brow spines (variant B): two short hooked cones on the upper rim.
    for k in range(spec["brow"]):
        sx = (-0.25 if k == 0 else 0.25) * W
        base = [md.v((sx + dx, -0.12, 0.5 + dz)) for dx, dz in ((-0.06, 0), (0.06, 0), (0.0, 0.05))]
        tip = md.v((sx * 1.6, 0.05, 0.85))
        uvb = [(0.9, 0.05), (0.95, 0.05), (0.92, 0.1)]
        for a_, b_ in ((0, 1), (1, 2), (2, 0)):
            md.f([base[a_], base[b_], tip], [uvb[a_], uvb[b_], (0.98, 0.2)])
    # Slant the whole eye (inner corner lower, the way a predator's eyes sit).
    rot = Matrix.Rotation(math.radians(-spec["slant"]), 3, "Y")
    md.verts = [rot @ v for v in md.verts]
    return md, up_idx, down_idx


def to_object(md, name, spec):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in md.verts], [], md.faces)
    uv = mesh.uv_layers.new(name="UVMap")
    li = 0
    for p, uvs in zip(mesh.polygons, md.uvs):
        for k, l in enumerate(p.loop_indices):
            uv.data[l].uv = uvs[k]
        p.material_index = md.mats[p.index]
        p.use_smooth = True
    # Orientation: ball, cornea and lids face away from the eye centre, the socket faces the viewer (-Y).
    for p in mesh.polygons:
        c = p.center
        r, g, bl = md.rig[mesh.polygons[p.index].vertices[0]]
        want = c.normalized() if (r or g or bl) else Vector((0, -1, 0))
        if p.normal.dot(want) < 0:
            p.flip()
    rig = mesh.color_attributes.new("EyeRig", "BYTE_COLOR", "POINT")
    rig.data.foreach_set("color_srgb", np.array([(r, g, b, 1.0) for r, g, b in md.rig], F32).ravel())
    obj = C.link(bpy.data.objects.new(name, mesh))
    return obj


def add_shape_keys(obj, spec):
    md_rig = np.zeros((len(obj.data.vertices), 4), F32)
    obj.data.color_attributes["EyeRig"].data.foreach_get("color_srgb", md_rig.ravel())
    co = np.zeros(len(obj.data.vertices) * 3, F32)
    obj.data.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    slant = Matrix.Rotation(math.radians(-spec["slant"]), 3, "Y")
    axis = slant @ Vector((1, 0, 0))
    obj.shape_key_add(name="Basis")

    def key(name, rot_up, rot_down, look=None):
        kb = obj.shape_key_add(name=name, from_mix=False)
        out = co.copy()
        if rot_up:
            M = np.array(Matrix.Rotation(rot_up, 3, axis))
            m = md_rig[:, 0] > 0.5
            out[m] = co[m] @ M.T
        if rot_down:
            M = np.array(Matrix.Rotation(rot_down, 3, axis))
            m = md_rig[:, 1] > 0.5
            out[m] = co[m] @ M.T
        if look is not None:
            M = np.array(look)
            m = md_rig[:, 2] > 0.5
            out[m] = co[m] @ M.T
        kb.data.foreach_set("co", out.ravel())
        # Exported as the FBX DeformPercent = Unity's default blend-shape weight: every eye rests open.
        kb.value = 0.0
    # Closing: the upper lid turns down past the middle, the lower lid up to meet it.
    # (A positive turn about +X moves the front of the eye down.)
    key("Blink", spec["open_up"] + BLINK_EXTRA, -(spec["open_down"] + BLINK_EXTRA * 0.5))
    up_axis = slant @ Vector((0, 0, 1))
    key("LookLeft", 0, 0, Matrix.Rotation(math.radians(LOOK_DEG), 3, up_axis))
    key("LookRight", 0, 0, Matrix.Rotation(math.radians(-LOOK_DEG), 3, up_axis))
    key("LookUp", 0, 0, Matrix.Rotation(math.radians(-LOOK_DEG * 0.7), 3, axis))
    key("LookDown", 0, 0, Matrix.Rotation(math.radians(LOOK_DEG * 0.7), 3, axis))
    return {"Blink": {"upper_lid_deg": round(math.degrees(spec["open_up"] + BLINK_EXTRA), 1),
                      "lower_lid_deg": round(math.degrees(spec["open_down"] + BLINK_EXTRA * 0.5), 1)},
            "Look": {"deg": LOOK_DEG, "up_down_deg": round(LOOK_DEG * 0.7, 1)},
            "axis_unity": [round(-axis.x, 4), round(axis.z, 4), round(-axis.y, 4)],
            "unity_rule": "turn the EyeRig.R vertices about axis_unity through the pivot so the upper lid's front "
                          "edge moves down by upper_lid_deg x blink, the EyeRig.G vertices so the lower lid's edge "
                          "moves up by lower_lid_deg x blink; EyeRig.B vertices turn by Look.deg for a look"}


def paint_eye(spec, out_dir, name):
    n = TEX
    t = (np.arange(n) + 0.5) / n
    U, V = np.meshgrid(t, t)
    alb = np.zeros((n, n, 3), F32)
    emit = np.zeros((n, n, 3), F32)
    smooth = np.full((n, n), 0.4, F32)
    ao = np.ones((n, n), F32)
    hgt = np.zeros((n, n), F32)
    iris = np.array(spec["iris"], F32)
    dark = np.array(spec["iris_dark"], F32)
    # Eyeball (u < 0.5, v > 0.5): front projection, x/z in units of the ball radius.
    bx = (U - 0.25) / 0.24
    bz = (V - 0.75) / 0.24
    rr = np.hypot(bx, bz)
    ball = (U < 0.5) & (V > 0.5)
    ang = np.arctan2(bz, bx)
    fib = S.fbm2(ang * 3.0, rr * 6.0, 1.0, 3, 5) * 0.5 + 0.5
    rings = 0.5 + 0.5 * np.sin(rr * 40 + 2 * S.fbm2(bx * 3, bz * 3, 1.0, 2, 6))
    iris_r = 0.62
    in_iris = 1 - S.ss(iris_r - 0.03, iris_r + 0.02, rr)
    limbal = S.ss(iris_r - 0.14, iris_r - 0.01, rr) * in_iris
    col_iris = S.mix(dark, iris * 0.8, np.clip(fib * 0.7 + 0.25 * rings - 0.2 + (1 - rr / iris_r) * 0.3, 0, 1))
    col_iris = col_iris * (1 - 0.6 * limbal)[..., None]
    if spec["pupil"] == "slit":
        pup = 1 - S.ss(0.0, 0.035, np.abs(bx) - 0.06 * np.sqrt(np.clip(1 - (bz / 0.55) ** 2, 0, 1)))
        pup *= 1 - S.ss(0.5, 0.56, np.abs(bz))
    elif spec["pupil"] == "bar":
        pup = 1 - S.ss(0.0, 0.035, np.abs(bz) - 0.09 * np.sqrt(np.clip(1 - (bx / 0.5) ** 2, 0, 1)))
        pup *= 1 - S.ss(0.45, 0.52, np.abs(bx))
    else:
        pup = np.zeros_like(bx)
        for k in range(3):
            a = math.radians(90 + 120 * k)
            px, pz = 0.22 * math.cos(a), 0.22 * math.sin(a)
            pup = np.maximum(pup, 1 - S.ss(0.1, 0.13, np.hypot(bx - px, bz - pz)))
    sclera = np.array((0.05, 0.025, 0.012), F32) * (0.7 + 0.6 * S.fbm2(bx * 2, bz * 2, 1.0, 2, 7)[..., None] * 0.5 + 0.3)
    vein = S.ss(0.9, 0.97, S.ridged2(bx * 1.5, bz * 1.5, 1.0, 3, 8)) * (1 - in_iris)
    sclera = S.mix(sclera, np.array((0.3, 0.04, 0.02), F32), vein * 0.6)
    eball = S.mix(sclera, col_iris, in_iris)
    eball = S.mix(eball, np.zeros(3, F32), pup)
    # Hot gold round the pupil, deep orange towards the rim, fibres darker: saturated, so bloom keeps the colour.
    heat = 1 - S.ss(0.0, iris_r, rr)
    glow_col = S.mix(dark * 1.6, iris, np.clip(heat * 0.9 + 0.2 * fib, 0, 1))
    e_em = glow_col * (in_iris * (0.35 + 0.45 * fib + 0.35 * heat) * (1 - pup) * (1 - 0.6 * limbal))[..., None]
    e_em = e_em + np.array((1.0, 0.3, 0.08), F32) * (vein * 0.06 + (1 - in_iris) * 0.008)[..., None]
    alb = np.where(ball[..., None], eball, alb)
    emit = np.where(ball[..., None], e_em, emit)
    smooth = np.where(ball, 0.85, smooth)
    hgt = np.where(ball, in_iris * 0.4 * rings * 0.3 - pup * 0.3, hgt)
    # Cornea region (u > 0.5, v > 0.5): almost clear; a faint warm film.
    corn = (U >= 0.5) & (V > 0.5)
    alb = np.where(corn[..., None], np.array((0.9, 0.85, 0.75), F32), alb)
    smooth = np.where(corn, 0.97, smooth)
    # Lids (v 0.25..0.5): chitin scales, a pale wet margin that glows faintly warm.
    lid_c = np.array(spec["lid"], F32)
    lids = (V > 0.25) & (V <= 0.5)
    lv = ((V - 0.25) % 0.125) / 0.125
    f1, f2, cv = S.cellular(U * 6, lv * 3, 0.5, 9)
    scale = S.ss(0.0, 0.18, f2)
    lid_col = lid_c * 0.6 * (0.7 + 0.6 * cv)[..., None] * (0.55 + 0.45 * scale)[..., None]
    margin = 1 - S.ss(0.03, 0.2, lv)
    lid_col = S.mix(lid_col, np.array((0.2, 0.06, 0.05), F32), margin * 0.7)
    alb = np.where(lids[..., None], lid_col, alb)
    emit = np.where(lids[..., None], np.array((1.0, 0.35, 0.12), F32) * (margin * 0.12)[..., None], emit)
    smooth = np.where(lids, 0.45 + 0.35 * margin, smooth)
    ao = np.where(lids, 0.75 + 0.25 * S.ss(0.0, 0.3, lv), ao)
    hgt = np.where(lids, scale * 0.5, hgt)
    # Socket (v < 0.25): knotted chitin, darker in the folds.
    sock = V <= 0.25
    k1, k2, kv = S.cellular(U * 20, V * 8, 1.0, 10)
    knots = S.ss(0.0, 0.3, k2)
    s_col = lid_c * 1.2 * (0.6 + 0.5 * kv)[..., None] * (0.5 + 0.5 * knots)[..., None]
    s_col = S.mix(s_col, np.array((0.5, 0.2, 0.14), F32), S.ss(0.85, 1.0, knots * kv) * 0.6)
    alb = np.where(sock[..., None], s_col, alb)
    smooth = np.where(sock, 0.35, smooth)
    ao = np.where(sock, 0.6 + 0.4 * S.ss(0.02, 0.18, V), ao)
    hgt = np.where(sock, knots * 0.6, hgt)
    gx = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * n / 2 * 0.004
    gy = (np.roll(hgt, -1, 0) - np.roll(hgt, 1, 0)) * n / 2 * 0.004
    nrm = np.dstack([-gx, -gy, np.ones_like(gx)])
    nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
    maps = dict(Albedo=S.linear_to_srgb(np.clip(alb, 0, 1)), Emission=S.linear_to_srgb(np.clip(emit, 0, 1)),
                Mask=np.dstack([ao, smooth, np.zeros_like(ao), np.zeros_like(ao)]), Normal=S.encode_normal(nrm))
    paths = {}
    for k, img in maps.items():
        p = os.path.join(out_dir, f"{name}_{k}.png")
        S.write_png(p, img)
        S.write_png(os.path.join(out_dir, "Low", f"{name}_{k}.png"), S.half(img))
        paths[k.lower()] = p
    return paths


def eye_materials(obj, paths, name):
    keep = np.zeros(len(obj.data.polygons), np.int32)
    obj.data.polygons.foreach_get("material_index", keep)
    obj.data.materials.clear()
    obj.data.materials.append(C.game_material(name + "_Eye", paths, EYE_PREVIEW_EMISSION, 0.5, ()))
    corn = bpy.data.materials.new(name + "_Cornea")
    corn.use_nodes = True
    bsdf = corn.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.9, 0.85, 0.75, 1)
    bsdf.inputs["Roughness"].default_value = 0.03
    bsdf.inputs["Alpha"].default_value = 0.12
    bsdf.inputs["Coat Weight"].default_value = 1.0
    try:
        corn.surface_render_method = "BLENDED"
    except AttributeError:
        pass
    obj.data.materials.append(corn)
    obj.data.polygons.foreach_set("material_index", keep)


def build_eyes():
    reports = {}
    for key, spec in EYES.items():
        name = spec["name"]
        if ONLY and name not in ONLY.split(","):
            continue
        C.reset_scene()
        md, _, _ = eye_mesh(spec)
        obj = to_object(md, name, spec)
        # Weld the socket's UV seam column without touching UVs (FBX keeps split UVs per loop).
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.normal_update()
        bm.to_mesh(obj.data)
        bm.free()
        keys = add_shape_keys(obj, spec)
        out_dir = os.path.join(OUT, name)
        paths = paint_eye(spec, out_dir, name)
        eye_materials(obj, paths, name)
        tris = C.tri_count(obj)
        C.export_fbx(os.path.join(out_dir, name + ".fbx"), [obj], animated=False)
        dims = obj.dimensions
        rep = {"asset": name, "round": 26, "game_tris": tris, "texture_size": TEX, "low_texture_size": TEX // 2,
               "maps": ["Albedo", "Normal", "Emission", "Mask"], "materials": [name + "_Eye", name + "_Cornea"],
               "size_m": [round(d, 3) for d in dims], "variant": spec["note"],
               "unit": "1 m wide slit: scale = 2 * EyeField.EyeWidth * size (0.15 per unit size)",
               "pivot": "eyeball centre; the eye looks along local +Z in Unity",
               "shape_keys": ["Blink", "LookLeft", "LookRight", "LookUp", "LookDown"], "rig": keys,
               "vertex_colours": "EyeRig: R upper lid, G lower lid, B eyeball + cornea (rotate about rig.axis_unity "
                                 "through the pivot: lids by Blink deg x weight, eyeball for looks)",
               "emission": "iris only, meant for EyeField.Emission 5 (HDR) with bloom",
               "replaces": "UI/Cutscene/EyeField.cs:74,122 (diamond slit quads)"}
        with open(os.path.join(out_dir, name + "_report.json"), "w", encoding="utf-8") as f:
            json.dump(rep, f, indent=1)
        C.save_blend("Underground_" + name)
        reports[name] = tris
        log(name, "tris", tris)
    return reports


# ---------------------------------------------------------------------------------------------------------------
# The cave scene (preview of UndergroundSet's layout built from the kit)
# ---------------------------------------------------------------------------------------------------------------

LENGTH = 34.0
WALL_R = 5.2
FLOOR_Y = -1.6
CLUSTERS = 5


def kit_ref(name):
    return (os.path.join("Underground", name), name, name)


def eye_sets(lib, pairs, rng, open_amount=1.0, look=(0.0, 0.6, 0.0)):
    centres = [(1.6, 0.4, 9.0), (-2.2, 1.3, 12.0)]
    sizes = [1.6, 1.28]
    for i in range(2, pairs):
        a = (rng.uniform(-0.2, 1.2)) * math.pi
        r = WALL_R - 0.55 + rng.uniform(0, 0.35)
        centres.append((math.cos(a) * r, max(FLOOR_Y + 0.3, math.sin(a) * r * 0.75), 4 + rng.uniform(0, LENGTH - 4)))
        sizes.append(0.8 + rng.uniform(0, 0.9))
    keys = list(EYES.values())
    for spec in keys:
        src = lib.source(kit_ref(spec["name"]))
        d = os.path.join(OUT, spec["name"])
        tex = {k.lower(): os.path.join(d, f"{spec['name']}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        if src is not None:
            eye_materials(src, tex, spec["name"])
    for i, (c, s) in enumerate(zip(centres, sizes)):
        spec = keys[i % 3 if i > 1 else 0]
        ref = kit_ref(spec["name"])
        lookv = Vector(look) - Vector(c)
        yaw = math.degrees(math.atan2(lookv.x, lookv.z))
        pitch = -math.degrees(math.atan2(lookv.y, math.hypot(lookv.x, lookv.z)))
        right = Vector((math.cos(math.radians(yaw)), 0, -math.sin(math.radians(yaw))))
        scale = 0.15 * s
        gap = 0.055 * s
        for side in (-1, 1):
            pos = Vector(c) + right * (gap * side)
            o = lib.place(ref, tuple(pos), yaw=yaw, scale=scale)
            if o is None:
                continue
            o.rotation_euler = (math.radians(-pitch), 0, -math.radians(yaw))
            if side > 0:
                o.scale.x = -scale
            if o.data.shape_keys is None and lib.sources[ref].data.shape_keys is None:
                pass


def build_scene(eyes_open=True):
    C.reset_scene()
    lib = S.Library(emission_strength=2.4)
    rng = np.random.default_rng(2626)
    # Walls along both sides, facing the corridor axis; ceiling slabs and stalactites above; floor pieces below.
    z = -2.0
    k = 0
    while z < LENGTH + 2:
        for side in (-1, 1):
            name = "Cave_Wall_B" if (k + (side > 0)) % 3 == 0 else "Cave_Wall_A"
            lib.place(kit_ref(name), (side * (WALL_R + 0.9) + rng.uniform(-0.2, 0.2), FLOOR_Y - 0.2, z + rng.uniform(-0.3, 0.3)),
                      yaw=-90 * side + rng.uniform(-10, 10), scale=rng.uniform(1.05, 1.25))
            lib.place(kit_ref("Cave_Boulder"), (side * (WALL_R - 0.6), FLOOR_Y - 0.1, z + 1.8 + rng.uniform(-0.4, 0.4)),
                      yaw=rng.uniform(0, 360), scale=rng.uniform(0.8, 1.3))
            lib.place(kit_ref("Cave_Wall_A"), (side * 3.0, 4.6, z + 1.0 + rng.uniform(-0.5, 0.5)),
                      yaw=-90 * side, scale=1.1, tilt=(0.0, 60.0 * side))
        lib.place(kit_ref("Cave_Ceiling"), (rng.uniform(-0.8, 0.8), 5.6, z + 1.8), yaw=rng.uniform(0, 360), scale=1.3)
        lib.place(kit_ref("Cave_Floor_B" if k % 3 == 1 else "Cave_Floor_A"), (rng.uniform(-1.2, 1.2), FLOOR_Y, z + 1.5),
                  yaw=rng.uniform(0, 360), scale=1.15)
        if k % 2 == 0:
            lib.place(kit_ref("Cave_Stalactites"), (rng.uniform(-2.2, 2.2), 4.2 + rng.uniform(-0.3, 0.2), z + rng.uniform(0, 3)),
                      yaw=rng.uniform(0, 360), scale=rng.uniform(0.9, 1.4))
        if k % 2 == 1:
            lib.place(kit_ref("Cave_Vein"), (rng.uniform(-2.5, 2.5), FLOOR_Y + 0.05, z + rng.uniform(0, 3)),
                      yaw=rng.uniform(0, 360), scale=1.2)
        z += 3.6
        k += 1
    for i in range(CLUSTERS):
        zc = 7 + i * (LENGTH - 9) / CLUSTERS + rng.uniform(0, 2)
        side = -1 if i % 2 == 0 else 1
        pos = (side * (3.6 + rng.uniform(0, 0.8)), FLOOR_Y + 0.2, zc)
        lib.place(kit_ref("Cave_CrystalCluster"), pos, yaw=rng.uniform(0, 360), scale=1.2 + rng.uniform(0, 0.8))
        S.point(f"Crystal{i}", (pos[0], pos[1] + 1.2, pos[2]), (0.35, 0.85, 1.0), 2.2 * 500, radius=0.3)
    if eyes_open:
        eye_sets(lib, 120, np.random.default_rng(81))
    world = C.preview_world((0.004, 0.004, 0.006))
    vol = world.node_tree.nodes.new("ShaderNodeVolumePrincipled")
    vol.inputs["Density"].default_value = 0.02
    vol.inputs["Color"].default_value = (0.4, 0.4, 0.45, 1)
    world.node_tree.links.new(vol.outputs[0], world.node_tree.nodes["World Output"].inputs["Volume"])
    S.sun("CaveFill", (0.2, -0.8, 0.5), (0.3, 0.27, 0.38), 3.0, shadow=False)
    S.point("Ember", (0.0, -0.8, 30.0), (1.0, 0.55, 0.3), 120, radius=1.0)
    return lib


def render_scene():
    scene = bpy.context.scene
    try:
        scene.eevee.volumetric_end = 40
        scene.eevee.volumetric_tile_size = "4"
    except AttributeError:
        pass
    paths = []
    cam = S.unity_camera("CaveCam", (0.0, 0.6, -1.0), 4.0, 0.0, 52.0, clip_end=80)
    paths.append(S.render(cam, os.path.join(PREVIEW, "cave_corridor.png"), 1280, 720, 64, threshold=0.6))
    cam2 = S.unity_camera("EyeCam", (0.3, 0.55, 6.6), 0.0, 28.0, 30.0, clip_end=80)
    paths.append(S.render(cam2, os.path.join(PREVIEW, "cave_first_eyes.png"), 1280, 720, 64, threshold=0.6))
    cam3 = S.unity_camera("WaveCam", (-1.5, 0.2, 3.0), -4.0, 12.0, 60.0, clip_end=80)
    paths.append(S.render(cam3, os.path.join(PREVIEW, "cave_eye_field.png"), 1280, 720, 64, threshold=0.6))
    return paths


def eye_sheet():
    """Close-ups of the three eyes, open / half / closed / looking, on dark chitin."""
    paths = []
    for spec in EYES.values():
        C.reset_scene()
        lib = S.Library(emission_strength=EYE_PREVIEW_EMISSION)
        ref = kit_ref(spec["name"])
        src = lib.source(ref)
        if src is None:
            continue
        d = os.path.join(OUT, spec["name"])
        tex = {k.lower(): os.path.join(d, f"{spec['name']}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        eye_materials(src, tex, spec["name"])
        # The FBX import keeps the shape keys on the source mesh; each pose is its own copy.
        poses = [("open", {}), ("half", {"Blink": 0.5}), ("closed", {"Blink": 1.0}), ("left", {"LookLeft": 1.0}),
                 ("up", {"LookUp": 1.0})]
        for i, (tag, vals) in enumerate(poses):
            o = bpy.data.objects.new(f"{tag}", src.data.copy())
            lib.collection.objects.link(o)
            o.location = Vector(((i - 2) * 1.35, 0, 0))
            if o.data.shape_keys:
                for kb in o.data.shape_keys.key_blocks[1:]:
                    kb.value = vals.get(kb.name, 0.0)
        C.preview_world((0.01, 0.008, 0.01))
        bg = C.ground(color=(0.04, 0.015, 0.02))
        bg.rotation_euler = (math.radians(90), 0, 0)
        bg.location = (0, 0.6, 0)
        S.point("Key", (-1.0, 1.8, -3.0), (1.0, 0.9, 0.85), 150)
        S.point("Key2", (2.5, 1.2, -3.0), (0.8, 0.85, 1.0), 90)
        S.point("Rim", (2.0, 1.0, 1.5), (0.4, 0.8, 1.0), 40)
        cam = C.closeup_camera(Vector((0, 0, 0)), 9.0, azimuth=-180 + 0.0, elevation=4, lens=50)
        cam.location = Vector((0, -9.0, 0.3))
        C.look_at(cam, Vector((0, 0, 0)))
        paths.append(S.render(cam, os.path.join(PREVIEW, f"eye_{spec['name']}.png"), 1400, 340, 48, threshold=0.75))
    if paths:
        C.contact_sheet(paths, os.path.join(PREVIEW, "eye_sheet.png"), 1, 1400)
    return paths


def main():
    results = {}
    if PART in ("kit", "all"):
        results["kit"] = build_kit()
    if PART in ("eyes", "all"):
        results["eyes"] = build_eyes()
    if not ARGS.get("norender") and PART in ("eyes", "all", "scene"):
        eye_sheet()
    if PART in ("scene", "all"):
        build_scene()
        C.save_blend("Underground_Scene")
        if not ARGS.get("norender"):
            render_scene()
    log("done", results)


main()
