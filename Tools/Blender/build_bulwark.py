"""Bulwark: the upright shield bearer with a crystal sword, armoured, from level 11 to 12 (round 10, see
ArtSource/DESIGN_NOTES.md).

A heavy saurian biped leaning into its walk on thick digitigrade legs with hooked toes; a natural shield (a curved
bone plate with growth ridges, a pale rim and a glowing boss) grown from the left forearm; a Crysium blade grown out
of the right forearm, held low; shoulder plates, a chest plate and a ridge of back spines over dark olive scales with
lime goo in the deep seams; a long low head under a bony visor with amber eye slits, tusked mandibles and a crest of
backward horns; a heavy tail with keel plates.

Clips: Walk (loop), Idle (loop), Hit, Death, Block (shield up by BLOCK_UP_FRAME, then held). Also exports
Bulwark_LOD1. Geometry and clips take a `cfg` so the CrystalWarlord (build_crystalwarlord.py) can reuse them.

Run: blender --background --python Tools/Blender/build_bulwark.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import crysium as Y  # noqa: E402
import crystal_growth as G49  # noqa: E402
import follow_through as FT  # noqa: E402
import mouth49g as MO  # noqa: E402
import uvlayout  # noqa: E402
from common import G  # noqa: E402

NAME = "Bulwark"
LOD_NAME = "Bulwark_LOD1"
K = 1.0
GAME_TRIS = 3950
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "crystal", "membrane")
GAME_SCALE = 0.9

# Species colours from ProjectGenerator.Bestiary: green body (0.3, 0.6, 0.35), amber eyes, goo-green blood; the data
# accent is cyan (our side's colour), so the sword is the planet's own pink Crysium instead.
PAL = H.Palette(top=(0.3, 0.42, 0.16), side=(0.1, 0.16, 0.06), belly=(0.035, 0.045, 0.025), seam=(0.02, 0.03, 0.012),
                dark=(0.06, 0.085, 0.035), glow=(0.62, 1.0, 0.22), eye=(1.0, 0.72, 0.22), membrane=(0.25, 0.4, 0.08),
                bone=(0.86, 0.82, 0.66))
PLATE = H.Palette(top=(0.4, 0.42, 0.26), side=(0.15, 0.17, 0.08), belly=(0.05, 0.06, 0.03), seam=(0.025, 0.03, 0.015),
                  dark=(0.08, 0.1, 0.05), glow=PAL.glow, eye=PAL.eye, bone=(0.9, 0.86, 0.7))
EYEP = H.Palette(top=PLATE.top, side=PLATE.side, belly=PLATE.belly, seam=PLATE.seam, dark=PLATE.dark,
                 glow=(1.0, 0.65, 0.2), eye=(1.0, 0.72, 0.22), bone=PLATE.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

CFG = {
    "sword_len": 0.6,
    "sword_width": 0.05,
    "shield": (0.035, 0.2, 0.26),
    "horns": 3,
}

SHOULDER = (0.2, 0.79, 0.04)
ELBOW = (0.3, 0.6, 0.0)
WRIST_R = (0.31, 0.48, 0.17)
WRIST_L = (0.24, 0.52, 0.18)
HIP = (0.11, 0.46, -0.03)
FOOT = (0.16, 0.0, 0.04)
TAIL = [G(0, 0.47, -0.14), G(0, 0.4, -0.38), G(0, 0.31, -0.62), G(0, 0.23, -0.86)]


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def legs():
    out = {}
    for side, sx in SIDES:
        names = [f"Leg_{side}_{s}" for s in ("Thigh", "Shin", "Foot")]
        out[side] = C.Limb(names, gp(HIP, sx), gp(FOOT, sx), (0.23, 0.23, 0.11), G(0.1 * sx, 0.1, 1.0),
                           foot_dir=G(0, -0.9, 0.42))
    return out


def rot(axis, deg):
    return Matrix.Rotation(math.radians(deg), 3, axis)


def sword_points(cfg=CFG):
    w = gp(WRIST_R, 1)
    d = G(0.02, -0.55, 0.84).normalized()
    L = cfg["sword_len"]
    return [w, w + d * L * 0.35 + G(0.01, 0, 0), w + d * L * 0.75, w + d * L]


# --------------------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------------------

def shield_deform(seed=12):
    """Round 49D (Remco: "shield too perfect"): a grown plate, not a disc. The outline is chipped (an uneven saw and
    three bitten-out notches), lopsided and drawn out into a jagged point at the bottom; the face carries warped growth
    ridges of uneven spacing and two scar gouges. Local x is the face normal, y/z the plate (z up)."""
    saw = G49.serrated_outline(11, seed, depth=0.16, jitter=0.8)
    rng = np.random.default_rng(seed)
    notches = rng.uniform(-math.pi, math.pi, 3)
    scars = [(rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(0, math.pi)) for _ in range(2)]

    def deform(v):
        a = math.atan2(v.z, v.y)
        f = saw(a) * (1.0 + 0.1 * math.sin(a + 0.7) + 0.07 * math.sin(2 * a + 2.1))
        f += 0.42 * max(0.0, -math.sin(a) - 0.8) / 0.2 * max(0.0, 1.0 - abs(math.cos(a)) * 3.0)
        for n_ in notches:
            da = math.atan2(math.sin(a - n_), math.cos(a - n_))
            f *= 1.0 - 0.18 * math.exp(-(da / 0.11) ** 2)
        y, z = v.y * f, v.z * f
        r = math.hypot(v.y, v.z)
        ridge = math.sin(13.0 * r + 2.2 * math.sin(3 * a + 0.4) + 1.3 * math.sin(5 * a + 1.9))
        thick = 1.0 + 0.14 * ridge * min(1.0, r * 1.6)
        for sy, sz, ang in scars:
            d = abs((v.y - sy) * math.sin(ang) - (v.z - sz) * math.cos(ang))
            thick *= 1.0 - 0.35 * math.exp(-(d / 0.05) ** 2) * (abs(v.x) > 0.2)
        return Vector((v.x * thick, y, z))
    return deform


def chitin_shield(pal, k, seed=0):
    """The round-49D shield skin: weathered bone with warped, broken growth lines (never even rings), dark scar
    gouges, faint lime light in a few deep cracks, a pale chipped rim and a dull off-centre knot instead of a target."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        n = s.n[idx]
        p = s.p[idx]
        warp = 0.35 * C.value_noise(p, 6 / k, seed) + 0.12 * C.value_noise(p, 17 / k, seed + 1)
        r = np.hypot(u[:, 1] + 0.2, u[:, 2] - 0.15) + warp
        frac = (r * 5.5) % 1.0
        broken = C.smoothstep(-0.2, 0.35, C.value_noise(p, 8 / k, seed + 3))
        groove = (1 - C.smoothstep(0.0, 0.1, np.minimum(frac, 1 - frac))) * broken
        face = C.smoothstep(0.1, 0.5, np.abs(u[:, 0]))
        crack = 1 - C.smoothstep(0.0, 0.035, np.abs(C.value_noise(p, 11 / k, seed + 5)))
        crack *= C.smoothstep(0.25, 0.6, C.value_noise(p, 3 / k, seed + 6)) * face
        knot = 1 - C.smoothstep(0.04, 0.14, np.hypot(u[:, 1] + 0.25, u[:, 2] - 0.3) + 0.05 * warp)
        rim = C.smoothstep(0.8, 0.97, np.hypot(u[:, 1], u[:, 2]))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * H.mottle(s, idx, 0.22, 26 / k, seed + 1)
        col = C.lerp3(col, pal.bone * 0.6, (0.06 + 0.12 * broken) * face)
        col = C.lerp3(col, pal.seam, np.maximum(groove * 0.5, crack * 0.8) * face)
        col = C.lerp3(col, pal.bone * 0.9, rim * 0.7)
        col = C.lerp3(col, pal.dark, knot * 0.6 * face)
        hot = np.clip(crack * 0.75 + knot * 0.25 * face, 0, 1)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = (0.004 * k * broken * (1 - groove) - 0.004 * k * groove - 0.003 * k * crack) * face
        s.rough[idx] = 0.72 - 0.12 * rim
        H.family(s, idx, chitin=1.0 - hot * 0.6, flow=r * part.radii[1])
    return style


def head_deform():
    """Round 49D (Remco: "head too perfectly round"): a hewn, narrowing wedge, lopsided by a hair."""
    facets = C.chiselled(18, 61, depth=0.1)

    def deform(v):
        u = facets(v)
        front = max(0.0, -u.y)
        return Vector((u.x * (1 - 0.38 * front) * (1.04 if u.x > 0 else 1.0), u.y, u.z * (1 - 0.3 * front)))
    return deform


def visor_deform():
    """The bony visor: a jagged, back-swept brow shelf, not a cap: saw-toothed side and rear edge, facets."""
    facets = C.chiselled(16, 67, depth=0.2)
    saw = G49.serrated_outline(9, 71, depth=0.36, jitter=0.7)

    def deform(v):
        u = facets(v)
        a = math.atan2(u.y, u.x)
        rear = C.smoothstep(-0.4, 0.6, u.y)
        rim = max(0.0, 1.0 - abs(u.z) * 1.4)
        k = 1.0 + (saw(a) - 1.0) * rim * (0.3 + 0.7 * rear)
        return Vector((u.x * k, u.y * k, u.z))
    return deform


def serrated_sword(b, cfg, fore, k):
    """Round 49D (Remco: "the sword may have serrations"): a Crysium blade of uneven width with a slight hook at the
    point, a row of raked crystal teeth along its cutting edge (bigger towards the middle, uneven) and a few small
    barbs along its back."""
    pts = sword_points(cfg)
    w = cfg["sword_width"]
    path = [pts[0], pts[1], pts[1].lerp(pts[2], 0.55), pts[2], pts[2].lerp(pts[3], 0.6) + G(0.0, 0.0, 0.0), pts[3]]
    tip_dir = (pts[3] - pts[2]).normalized()
    path[-1] = path[-1] + G(0.0, 0.02, 0.0) * (cfg["sword_len"] / 0.6) - tip_dir * 0.01
    radii = [w * 0.85, w * 1.0, w * 1.12, w * 0.9, w * 0.75, 0.002]
    b.tube("Sword", path, radii, sides=10, chain=[fore], style=K2.crysium_blade(k), flatten=0.22)
    curve = C.catmull_rom([Vector(p) for p in path], 6)
    frames = C.parallel_frames(curve)
    rad = np.interp(np.linspace(0, 1, len(curve)), np.linspace(0, 1, len(radii)), radii)
    rng = np.random.default_rng(83)
    teeth = 7 if cfg["sword_len"] < 0.8 else 9
    for i in range(teeth):
        t = 0.2 + 0.68 * (i + rng.uniform(-0.25, 0.25)) / (teeth - 1)
        j = int(t * (len(curve) - 1))
        tan, n, _ = frames[j]
        size = (0.55 + 0.6 * math.sin(math.pi * t)) * rng.uniform(0.75, 1.2)
        base = curve[j] + n * rad[j] * 0.7
        d = (n * 0.85 + tan * rng.uniform(0.45, 0.8)).normalized()
        G49.fractured_shard(b, "SwordTooth", base, d, w * 0.42 * size, w * 1.5 * size, 400 + i, flat=0.35,
                            tip="point", steps=0, chips=0, root_flare=1.3, rings=8, chain=[fore],
                            style=K2.crysium_blade(k))
    for i, t in enumerate((0.35, 0.55, 0.72)):
        j = int(t * (len(curve) - 1))
        tan, n, _ = frames[j]
        base = curve[j] - n * rad[j] * 0.7
        d = (-n * 0.8 - tan * 0.6).normalized()
        G49.fractured_shard(b, "SwordTooth", base, d, w * 0.26, w * 0.85, 420 + i, flat=0.35, tip="point",
                            steps=0, chips=0, root_flare=1.3, rings=6, chain=[fore], style=K2.crysium_blade(k))


# Round 49G (Remco on 49F: "those teeth are strange with the new mouth"): a real mouth (mouth49g.py). The jaw hangs
# JAW_DROP lower than in 49F, so a slot opens under the head; gums run along both rims, the teeth (bulwark_detail)
# are rooted in them, a dark palate, tongue and the glowing throat fill the slot, and the tusks are lower canines
# grown out of a bony boss on each front corner of the jaw (matte ivory, dark roots).
JAW_DROP = 0.05


def head_shape(b, style=None):
    return b.ellipsoid("Head", G(0, 0.89, 0.32), (0.08, 0.15, 0.065), rot("X", 6), bone="Head", segments=(48, 24),
                       deform=head_deform(), style=style)


def jaw_shape(b, style=None):
    return b.ellipsoid("Jaw", G(0, 0.835 - JAW_DROP, 0.33), (0.056, 0.12, 0.028), rot("X", -6), bone="Jaw",
                       segments=(32, 14), deform=C.chiselled(12, 79, depth=0.12), style=style)


MOUTH = MO.Mouth(u=[(-0.045, 0.32), (-0.04, 0.36), (-0.03, 0.39), (-0.017, 0.415), (0.0, 0.432), (0.017, 0.415),
                    (0.03, 0.39), (0.04, 0.36), (0.045, 0.32)],
                 upper_fn=head_shape, lower_fn=jaw_shape, gum_r=0.006, gum_sink=0.003, ray_from=(0.6, 1.2),
                 lower_inset=0.9, clearance=0.003, upper_teeth=6, lower_teeth=5, tooth_r=0.0075, canine=1.3)


def gum_style(seed):
    return MO.gum_style(seed, flesh=(0.26, 0.08, 0.05), deep=(0.06, 0.02, 0.012))


def mouth_parts(b, k):
    """The gums, palate, tongue, throat and tusks (round 49G); the teeth are in bulwark_detail.py."""
    up, lo = MO.gum_path(MOUTH, True), MO.gum_path(MOUTH, False)
    radii = [MOUTH.gum_r * 0.8] + [MOUTH.gum_r] * (len(up) - 2) + [MOUTH.gum_r * 0.8]
    b.tube("UpperGum", up, radii, sides=10, chain=["Head"], style=gum_style(1))
    b.tube("LowerGum", lo, radii, sides=10, chain=["Jaw"], style=gum_style(4))
    mid_up = sum((v.z for v in up), 0.0) / len(up)
    mid_lo = sum((v.z for v in lo), 0.0) / len(lo)
    flesh = gum_style(7)
    b.ellipsoid("Palate", G(0, mid_up + 0.001, 0.37), (0.028, 0.055, 0.005), bone="Head", segments=(20, 10),
                style=flesh)
    b.ellipsoid("Tongue", G(0, mid_lo - 0.001, 0.36), (0.024, 0.05, 0.006), rot("X", -4), bone="Jaw",
                segments=(20, 10), style=flesh)
    b.ellipsoid("Throat", G(0, (mid_up + mid_lo) * 0.5, 0.3), (0.026, 0.03, 0.008), bone="Head",
                style=H.inner_glow(PAL, 0.9), segments=(16, 8))
    tusk = MO.ivory_style(31)
    for side, sx in SIDES:
        boss = G(0.04 * sx, 0.805, 0.405)
        b.ellipsoid("TuskBoss", boss, (0.014, 0.017, 0.012), bone="Jaw", segments=(16, 8),
                    deform=C.chiselled(8, 83 + (sx > 0), depth=0.12), style=H.band_plates(PAL, k, 2, height=0.001))
        size = 1.1 if sx < 0 else 0.9
        root = G(0.03 * sx, 0.795, 0.4)
        b.tube("Tusk", [root, root + G(0.026 * sx, 0.02, 0.01), root + G(0.044 * sx, 0.042 * size, 0.02),
                        root + G(0.05 * sx, 0.06 * size, 0.012)], [0.0105, 0.0085, 0.005, 0.0012], sides=10,
               chain=["Jaw"], style=tusk)


def rooted(path, radii, centre, depth):
    """Round 49F (floating-part check): a spike or hook grows out of the body. The path gets a root point `depth`
    inside towards `centre`, at the root's radius, so the spike fuses into the plate or body it stands on instead of
    hovering a hair above it (Remco reads a hairline gap as a loose part)."""
    root = Vector(path[0])
    inner = root + (Vector(centre) - root).normalized() * depth
    return [inner] + list(path), [radii[0]] + list(radii)


def build_parts(cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    rng = np.random.default_rng(491)
    hide = K2.scale_hide(PAL, k, cells=70, seed=3, joint_glow=0.55)
    b.ellipsoid("Pelvis", G(0, 0.47, -0.04), (0.15, 0.13, 0.11), bone="Pelvis", style=hide, segments=(32, 16))
    b.ellipsoid("Torso", G(0, 0.64, 0.04), (0.17, 0.15, 0.2), rot("X", -18), bone="Spine", style=hide, segments=(36, 18))
    b.parts[-1].chain = ["Spine", "Pelvis"]
    b.ellipsoid("ChestPlate", G(0, 0.66, 0.17), (0.13, 0.15, 0.035), rot("X", 70), bone="Spine",
                style=K2.armour_plate(PLATE, k, rim_glow=0.7, chevron=0.6, chevron_angle=1.1, cells=10, seed=5),
                segments=(48, 20), deform=C.chiselled(14, 73, depth=0.08))
    b.ellipsoid("Belly", G(0, 0.52, 0.07), (0.12, 0.08, 0.08), bone="Pelvis", segments=(24, 12),
                style=H.glow_flesh(PAL, k, glow=0.45, flank=(0.0, 0.5), folds=10.0))
    for i, (f, ln) in enumerate(((0.0, 1.0), (-0.1, 1.35), (-0.2, 0.8))):
        base = G(0.008 * (i - 1), 0.8 - 0.06 * i, f)
        path, radii = rooted([base, base + G(0, 0.07 * ln, -0.03), base + G(0.01 * i, 0.09 * ln, -0.1 * ln)],
                             [0.022, 0.012, 0.0015], G(0, 0.64, 0.04), 0.1)
        b.tube("BackSpine", path, radii, sides=8, chain=["Spine"], style=H.hook(PLATE, k, glow=0.6, glow_from=0.7))
    b.tube("Neck", [G(0, 0.76, 0.1), G(0, 0.83, 0.18), G(0, 0.87, 0.24)], [0.08, 0.07, 0.06], sides=12, chain=["Neck"],
           style=H.tube(PAL, k, PAL.top * 0.9, PAL.side, rings=(0.3, 0.6)))

    head_shape(b, H.wedge_head(EYEP, k, slits=3, slit_width=0.05))
    b.ellipsoid("Visor", G(0, 0.935, 0.29), (0.088, 0.14, 0.021), rot("X", 4), bone="Head", segments=(64, 24),
                deform=visor_deform(),
                style=K2.armour_plate(PLATE, k, rim_glow=0.5, chevron=0.9, chevron_angle=1.3, lip=0.8, seed=7))
    # Heavy brow ridges meeting low over the snout in a V: the face reads angular and glaring, not round.
    for sx in (-1, 1):
        b.tube("BrowRidge", [G(0.085 * sx, 0.94, 0.25), G(0.06 * sx, 0.935, 0.34), G(0.012 * sx, 0.915, 0.42)],
               [0.026, 0.022, 0.009], sides=10, chain=["Head"], style=H.hook(PLATE, k), flatten=0.6)
    # A keel along the top of the visor, broken into two uneven blades.
    for a_, b_, h_ in ((0.36, 0.27, 0.028), (0.25, 0.17, 0.02)):
        b.tube("VisorKeel", [G(0.0, 0.955, a_), G(0.004, 0.955 + h_, (a_ + b_) / 2 - 0.01), G(0.0, 0.952, b_)],
               [0.008, 0.007, 0.004], sides=8, chain=["Head"], style=H.hook(PLATE, k), flatten=0.35)
    jaw_shape(b, H.band_plates(PAL, k, 3, height=0.002))
    path, radii = rooted([G(0, 0.8 - JAW_DROP, 0.41), G(0, 0.78 - JAW_DROP, 0.44), G(0, 0.755 - JAW_DROP, 0.43)],
                         [0.014, 0.009, 0.0012], G(0, 0.835 - JAW_DROP, 0.33), 0.02)
    b.tube("ChinHook", path, radii, sides=8, chain=["Jaw"], style=H.hook(PLATE, k, glow=0.3))
    mouth_parts(b, k)
    for i in range(cfg["horns"]):
        # A crest of uneven back-swept horns, the middle one split.
        base = G(0.006 * (1 - i), 0.965 - 0.005 * i, 0.32 - 0.075 * i)
        size = (1.0, 1.25, 0.8)[i] if i < 3 else 0.7
        path, radii = rooted([base, base + G(0.01 * i, 0.06 * size, -0.04), base + G(0.02 * i, 0.08 * size, -0.13 * size)],
                             [0.022 * size, 0.013 * size, 0.0015], G(0, 0.89, 0.32), 0.04)
        b.tube("Horn", path, radii, sides=8, chain=["Head"], style=H.hook(PLATE, k))
    for side, sx in SIDES:
        for j, (dz, ln) in enumerate(((0.0, 1.0 if sx < 0 else 0.75), (-0.06, 0.6 if sx < 0 else 0.85))):
            root = G(0.075 * sx, 0.93 - 0.02 * j, 0.25 + dz)
            path, radii = rooted([root, root + G(0.04 * sx, 0.01, -0.05 * ln), root + G(0.06 * sx, 0.0, -0.11 * ln)],
                                 [0.016, 0.01, 0.0012], G(0, 0.89, 0.32), 0.05)
            b.tube("Cheek", path, radii, sides=8, chain=["Head"], style=H.hook(PLATE, k))

        # Arms; the shoulder plate rides the upper arm.
        sh, el = gp(SHOULDER, sx), gp(ELBOW, sx)
        wr = gp(WRIST_R if sx > 0 else WRIST_L, sx)
        up, fore = f"UpperArm_{side}", f"ForeArm_{side}"
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5) + gp((0.02, 0, 0), sx), el], [0.07, 0.06, 0.05], sides=12, chain=[up],
               style=hide)
        # Round 49F: a shoulder ball bridging the chest and the upper arm, so the arm is one piece with the body (the
        # Crystal Warlord's finer remesh left a hairline gap there, and its arms drifted off the chest in the Sweep).
        b.ellipsoid("ShoulderBall", sh.lerp(G(0, 0.75, 0.04), 0.3), (0.085, 0.085, 0.08), bone=up, style=hide,
                    segments=(20, 10))
        b.parts[-1].chain = [up, "Spine"]
        b.ellipsoid("Elbow", el, (0.05,) * 3, bone=up, style=hide, segments=(16, 8))
        b.parts[-1].chain = [up, fore]
        b.tube("ForeArm", [el, el.lerp(wr, 0.5), wr], [0.05, 0.05, 0.045], sides=12, chain=[fore], style=hide)
        b.ellipsoid("ShoulderPlate", sh + gp((0.03, 0.03, 0.0), sx), (0.085, 0.11, 0.03), rot("Y", 48 * sx), bone=up,
                    style=K2.armour_plate(PLATE, k, rim_glow=0.8, chevron=0.0, lip=0.7, cells=8, seed=9 + (sx > 0)),
                    segments=(48, 18), deform=visor_deform())
        for j in range(2 + (sx < 0)):
            base = sh + gp((0.07 + 0.03 * j, 0.06, 0.03 - 0.05 * j), sx)
            ln = rng.uniform(0.8, 1.3)
            path, radii = rooted([base, base + gp((0.03, 0.05 * ln, -0.02), sx), base + gp((0.05, 0.06 * ln, -0.07 * ln), sx)],
                                 [0.016, 0.009, 0.001], sh + gp((0.03, 0.03, 0.0), sx), 0.075)
            b.tube("ShoulderSpike", path, radii, sides=8, chain=[up], style=H.hook(PLATE, k, glow=0.5, glow_from=0.75))
        if sx < 0:
            # The natural shield on the left forearm: a grown, chipped, scarred bone plate.
            centre = el.lerp(wr, 0.55) + G(-0.075, 0.02, 0.0)
            sh_rot = rot("Z", 22) @ rot("X", -8)
            b.ellipsoid("Shield", centre, cfg["shield"], sh_rot, bone=fore, segments=(112, 56), deform=shield_deform(12),
                        style=chitin_shield(PLATE, k, seed=12))
            # Hooks grown from the rim at uneven places and lengths; the longest from the bottom point.
            sy, sz = cfg["shield"][1], cfg["shield"][2]
            for j, (a, ln) in enumerate(((-1.62, 1.6), (-2.5, 0.8), (-0.6, 1.0), (0.5, 0.7), (1.35, 1.15), (2.3, 0.85),
                                         (2.85, 0.6))):
                local = Vector((0.0, math.cos(a) * sy * 0.97, math.sin(a) * sz * 0.97))
                base = centre + sh_rot @ local
                out_dir = (sh_rot @ Vector((0.0, math.cos(a), math.sin(a)))).normalized()
                s_ = cfg["shield"][2] / 0.26 * ln
                path, radii = rooted([base, base + out_dir * 0.03 * s_ + G(-0.01, 0, 0),
                                      base + out_dir * 0.055 * s_ + G(-0.03, -0.01, 0)],
                                     [0.014, 0.008, 0.001], centre, 0.035 * s_)
                b.tube("ShieldHook", path, radii, sides=8, chain=[fore], style=H.hook(PLATE, k, glow=0.4))
            b.ellipsoid("Fist", wr, (0.045, 0.05, 0.04), bone=fore, style=hide, segments=(16, 8))
        else:
            serrated_sword(b, cfg, fore, k)
            for j, (a, d) in enumerate(((0.3, (0.6, 1.0, -0.3)), (0.5, (1.0, 0.6, 0.2)), (0.7, (0.4, 1.0, 0.5)))):
                base = el.lerp(wr, a) + gp((0.03, 0.02, 0.0), sx)
                G49.fractured_shard(b, "Bracer", base, gp(d, sx), 0.017, 0.06 + 0.02 * ((j * 2) % 3), 30 + j,
                                    bone=fore, chain=[fore], style=G49.uneven_glow(Y.shard_style("pink", 0.9, seed=30 + j), 30 + j))
            b.ellipsoid("Fist", wr, (0.05, 0.055, 0.045), bone=fore, style=hide, segments=(16, 8))

    for i in range(3):
        a, c = TAIL[i], TAIL[i + 1]
        b.tube("Tail", [a, a.lerp(c, 0.5), c], [0.1 - 0.025 * i, 0.085 - 0.025 * i, 0.07 - 0.025 * i], sides=12,
               chain=[f"Tail_{i + 1}"], style=hide)
        top = a.lerp(c, 0.5) + G(0, 0.08 - 0.022 * i, 0)
        path, radii = rooted([top - G(0, 0.03, -0.03), top + G(0, 0.03, -0.05), top + G(0, 0.03, -0.12)],
                             [0.018, 0.01, 0.0015], a.lerp(c, 0.5), 0.03)
        b.tube("TailKeel", path, radii, sides=8, chain=[f"Tail_{i + 1}"], style=H.hook(PLATE, k, glow=0.5))

    for side, lg in legs().items():
        hip, knee, ankle, toe = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("Thigh", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.01)), knee], [0.085, 0.075, 0.05], sides=12,
               chain=[lg.names[0]], style=hide)
        b.ellipsoid("Knee", knee, (0.05,) * 3, bone=lg.names[0], style=hide, segments=(16, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("Shin", [knee, knee.lerp(ankle, 0.5), ankle], [0.05, 0.042, 0.035], sides=10, chain=[lg.names[1]], style=hide)
        b.ellipsoid("Ankle", ankle, (0.036,) * 3, bone=lg.names[1], style=hide, segments=(14, 7))
        b.parts[-1].chain = [lg.names[1], lg.names[2]]
        b.tube("Foot", [ankle, ankle.lerp(toe, 0.5), toe], [0.035, 0.032, 0.03], sides=10, chain=[lg.names[2]], style=hide)
        for a in (-0.45, 0.0, 0.45):
            fwd = G(math.sin(a) * 0.8, 0, 1).normalized()
            b.tube("Toe", [toe, toe + fwd * 0.05 + Vector((0, 0, 0.008)), toe + fwd * 0.085 - Vector((0, 0, 0.015))],
                   [0.02, 0.013, 0.0015], sides=8, chain=[lg.names[2]], style=H.hook(PLATE, k))
        b.tube("Spur", [ankle, ankle + G(0, 0.0, -0.05), ankle + G(0, 0.02, -0.09)], [0.016, 0.009, 0.001], sides=8,
               chain=[lg.names[2]], style=H.hook(PLATE, k))
    return b


def bones(cfg=CFG):
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Pelvis", G(0, 0.42, -0.06), G(0, 0.55, -0.02), "Root", True),
        ("Spine", G(0, 0.55, -0.02), G(0, 0.78, 0.1), "Pelvis", True),
        ("Neck", G(0, 0.78, 0.1), G(0, 0.88, 0.24), "Spine", True),
        ("Head", G(0, 0.88, 0.24), G(0, 0.88, 0.44), "Neck", True),
        ("Jaw", G(0, 0.85, 0.25), G(0, 0.82, 0.44), "Head", True),
        ("Tail_1", TAIL[0], TAIL[1], "Pelvis", True),
        ("Tail_2", TAIL[1], TAIL[2], "Tail_1", True),
        ("Tail_3", TAIL[2], TAIL[3], "Tail_2", True),
    ]
    for side, sx in SIDES:
        sh, el = gp(SHOULDER, sx), gp(ELBOW, sx)
        wr = gp(WRIST_R if sx > 0 else WRIST_L, sx)
        out.append((f"UpperArm_{side}", sh, el, "Spine", True))
        out.append((f"ForeArm_{side}", el, wr, f"UpperArm_{side}", True))
    for lg in legs().values():
        out += lg.rest_bones("Pelvis")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, lean=0.0, roll=0.0, yaw=0.0, twist=0.0, neck=0.0, head=0.0, head_yaw=0.0, jaw=0.0,
               tail=(0.0, 0.0, 0.0), sword=(0.0, 0.0, 0.0), shield=(0.0, 0.0, 0.0), head_roll=0.0):
    """lean: spine pitch forward (+); twist: chest yaw against the hips; tail: (sway, phase, lift);
    sword / shield: (upper arm forward swing, upper arm out, forearm bend) in radians, + forward/up;
    head: + nods down; head_yaw: + looks to its right (the sword side); head_roll: + tilts the crown to its right."""
    sway, phase, lift = tail
    specs = {
        "Root": {"rot": []},
        "Pelvis": {"rot": [(Z, yaw), (Yv, roll)], "loc": offset if offset is not None else Vector()},
        "Spine": {"rot": [(Z, twist), (X, lean)]},
        "Neck": {"rot": [(X, neck), (Z, head_yaw * 0.4)]},
        "Head": {"rot": [(X, head), (Z, head_yaw * 0.6), (Yv, head_roll)]},
        "Jaw": {"rot": [(X, -jaw)]},
    }
    for i in range(3):
        specs[f"Tail_{i + 1}"] = {"rot": [(Z, sway * math.sin(phase - i * 0.9) * (0.6 + 0.3 * i)), (X, lift * (1 - 0.3 * i))]}
    for side, sx, (fwd, out, bend) in (("R", 1, sword), ("L", -1, shield)):
        specs[f"UpperArm_{side}"] = {"rot": [(Yv, sx * out), (X, -fwd)]}
        specs[f"ForeArm_{side}"] = {"rot": [(X, -bend)]}
    return specs


def place_legs(poser, specs, feet, foot_dirs=None):
    pose = poser.solve(specs)
    move = pose["Pelvis"] @ poser.rest["Pelvis"].inverted()
    for key, lg in legs().items():
        fd = foot_dirs[key] if foot_dirs else lg.foot_dir
        specs.update(lg.pose(poser, move @ lg.root, feet[key], move.to_3x3() @ lg.pole, fd))
    return specs


CROUCH = -0.03
WALK_FRAMES = 32
STRIDE, LIFT, STANCE = 0.3, 0.07, 0.6

# Round 49D (Remco: "the animation smoother"): every curve below is smooth in position and velocity. The old walk had
# kinks where the foot lifted and touched down (constant slide back, then a swing starting at rest, and a toe fold that
# started at full speed), a hip bob on |cos| (a corner twice a step); the hit jumped from its shove into its ring-out;
# the death snapped the feet from planted to buckled on the first frame of the fall.


def smooth_step_curve(phase, stance, stride, lift):
    """Foot offset (forward, up) over one gait cycle, like common.step_curve, but the swing leaves and lands at the
    stance's ground speed (a cubic Hermite with matching end tangents) and lifts on sin^2, so neither the foot nor its
    speed jumps at lift-off or touchdown. phase 0 = touchdown at the front."""
    if phase < stance:
        s = phase / stance
        return stride * (0.5 - s), 0.0
    s = (phase - stance) / (1 - stance)
    m = -stride * (1 - stance) / stance
    h00, h10, h01, h11 = 2 * s ** 3 - 3 * s ** 2 + 1, s ** 3 - 2 * s ** 2 + s, -2 * s ** 3 + 3 * s ** 2, s ** 3 - s ** 2
    fwd = h00 * (-0.5 * stride) + h10 * m + h01 * (0.5 * stride) + h11 * m
    # The highest point comes a little early (the toe clears, then the foot reaches out and sets down).
    up = lift * math.sin(math.pi * s ** 0.85) ** 2
    return fwd, up


def soft_recoil(t, attack=0.18, decay=4.5, freq=7.5):
    """A hit: eased into the shove (no first-frame jump) and rung out as a damped spring that starts with zero
    velocity, so the shove and the ring-out meet without a corner (K2.recoil jumps 0 -> 0.4 there)."""
    if t < attack:
        return math.sin(0.5 * math.pi * t / attack) ** 2
    g = t - attack
    return math.exp(-decay * g) * (math.cos(freq * g) + decay / freq * math.sin(freq * g))


def walk_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Weight over the stance foot: the hips slide to the planted side (left mid-stance at t 0.3, right at 0.8),
        # roll down over the swing side, and the chest counter-twists; head, sword and tail follow a beat later.
        shift = math.cos(w - 2 * math.pi * 0.3)
        bob = 0.5 + 0.5 * math.cos(2 * w)
        specs = body_specs(offset=Vector((-0.022 * shift, 0.006 * math.sin(2 * w - 0.5), CROUCH - 0.016 * bob)),
                           lean=d(8) + d(1.5) * math.sin(2 * w + 0.6), roll=-d(3.5) * shift, yaw=d(5) * math.sin(w),
                           twist=-d(7) * math.sin(w - 0.25), neck=-d(4) + d(3) * math.sin(2 * w - 0.4),
                           head=d(5) * math.sin(2 * w - 1.0), head_yaw=-d(7) * math.sin(w - 0.6),
                           head_roll=d(4) * math.sin(w - 0.9),
                           jaw=d(3) * (0.5 + 0.5 * math.sin(2 * w - 1.6)),
                           tail=(d(9), w - 0.9, -d(4) + d(4) * math.sin(2 * w - 1.2)),
                           sword=(d(9) * math.sin(w + math.pi - 0.7), d(6) + d(2) * math.sin(2 * w - 1.0),
                                  d(10) + d(4) * math.sin(w + math.pi - 1.2)),
                           shield=(d(10) + d(3) * math.sin(w - 0.7), d(4), d(15) + d(3) * math.sin(w - 1.1)))
        feet, dirs = {}, {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key == "L" else 0.5)) % 1.0
            fwd, up = smooth_step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
            # The toe folds back through the swing and opens again before touchdown, eased at both ends.
            fold = math.sin(math.pi * (phase - STANCE) / (1 - STANCE)) ** 2 if phase >= STANCE else 0.0
            dirs[key] = Matrix.Rotation(d(-32) * fold, 3, "X") @ lg.foot_dir
        return place_legs(poser, specs, feet, dirs)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0.006 * math.sin(w + 0.4), 0, CROUCH + 0.006 * math.sin(2 * w))),
                           lean=d(6) + d(1.5) * math.sin(2 * w), roll=d(1.5) * math.sin(w),
                           neck=-d(3) + d(3) * math.sin(2 * w + 0.4),
                           head=d(4) * math.sin(2 * w + 1) + d(3) * math.sin(3 * w + 0.3),
                           head_yaw=d(18) * math.sin(w) + d(6) * math.sin(3 * w + 0.5),
                           head_roll=d(6) * math.sin(w - 0.5),
                           jaw=d(6) * max(0.0, math.sin(3 * w)) ** 4,
                           tail=(d(12), w, -d(4) + d(3) * math.sin(2 * w + 0.7)), sword=(d(5) + d(4) * math.sin(w - 0.5), d(6), d(10) + d(2) * math.sin(w - 0.9)),
                           shield=(d(10), d(4), d(15) + d(2) * math.sin(w + 1)))
        return place_legs(poser, specs, {k_: lg.foot for k_, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = soft_recoil(t)
        lag = soft_recoil(max(0.0, t - 0.06))  # the head, tail and arms answer a frame later
        specs = body_specs(offset=Vector((0, 0.03 * k, CROUCH - 0.01 * k)), lean=d(6) - d(14) * k, roll=d(5) * k,
                           twist=d(8) * k, neck=-d(3) - d(14) * lag, head=-d(14) * lag, head_yaw=-d(12) * lag,
                           head_roll=-d(8) * lag, jaw=d(25) * max(lag, 0.0),
                           tail=(d(32) * lag, 1.0, d(22) * lag), sword=(d(5) - d(10) * lag, d(6) + d(10) * lag, d(10)),
                           shield=(d(10) + d(10) * k, d(4), d(15)))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


BLOCK_FRAMES = 24
BLOCK_UP_FRAME = 7


def block_spec(poser):
    d = math.radians
    up_t = BLOCK_UP_FRAME / BLOCK_FRAMES

    def spec(t, f):
        up = K2.ease(0.0, up_t, t)
        # The shield arrives with a little follow-through (it rides past and settles), the body braces after it.
        settle = math.exp(-max(0.0, t - up_t) * 9.0) * math.sin(max(0.0, t - up_t) * 14.0) * (t > up_t)
        brace = K2.pulse(t, up_t * 0.7, up_t + 0.08, 0.5) * 0.6
        lead = K2.ease(0.0, up_t * 0.75, t)
        specs = body_specs(offset=Vector((0, 0.02 * up, CROUCH - 0.05 * up - 0.012 * brace)), lean=d(6) + d(6) * up,
                           twist=d(16) * up + d(2) * settle, neck=-d(3) + d(4) * lead, head=d(9) * lead - d(2) * settle,
                           head_yaw=-d(16) * lead, head_roll=-d(5) * lead, jaw=d(14) * up,
                           tail=(d(5) + d(14) * up, 1.0, d(8) * up), sword=(d(5) - d(12) * up, d(6) + d(12) * up, d(10) - d(12) * up),
                           shield=(d(10) + d(58) * up + d(5) * settle, d(4) - d(14) * up, d(15) + d(45) * up + d(4) * settle))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return BLOCK_FRAMES, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        buckle = K2.ease(0.05, 0.4, t)
        fall = K2.ease(0.3, 0.8, t)
        twitch = math.exp(-max(0.0, t - 0.8) * 8) * math.sin(max(0.0, t - 0.8) * 40) * (1 if t > 0.8 else 0)
        specs = body_specs(offset=Vector((0, 0, CROUCH - 0.12 * buckle)), lean=d(10) + d(25) * buckle,
                           neck=-d(8) * buckle * (1 - fall) + d(20) * fall, head=-d(10) * buckle * (1 - fall) + d(15) * fall,
                           head_yaw=d(14) * fall, head_roll=d(10) * fall, jaw=d(30) * fall + d(6) * twitch,
                           tail=(d(16) * fall, 0.4, -d(8) * fall),
                           sword=(d(25) * fall, d(30) * fall, -d(10) * fall), shield=(d(10) - d(20) * fall, d(35) * fall, d(5)))
        # It topples onto its right side (the sword side), pivoting on the right foot.
        pivot = Vector((0.2, 0, 0))
        angle = d(80) * fall
        r = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - r @ pivot + Vector((0, 0, -0.06 * fall))}
        pose = poser.solve(specs)
        move = pose["Pelvis"] @ poser.rest["Pelvis"].inverted()
        for key, lg in legs().items():
            hip = move @ lg.root
            rest_vec = lg.foot - lg.root
            bent = Vector((rest_vec.x, rest_vec.y - 0.12, rest_vec.z * 0.7))
            carried = hip + move.to_3x3() @ rest_vec.lerp(bent, max(buckle, fall))
            # Planted until the fall starts, then carried along with the body: blended, never switched.
            foot = lg.foot.lerp(carried, K2.ease(0.3, 0.55, t))
            specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole, move.to_3x3() @ lg.foot_dir))
        return specs
    return 44, spec, False


# Round 49F (Remco: "the tail, neck and head are very static"): the neck and head and the tail follow the body through
# damped springs (follow_through.py): they lag a turn or a lurch, overshoot and settle, and the lag grows down the
# tail. The tail is softer than the neck; the gain exaggerates the lag a little, the cap stops a fall from flinging it.
TAIL_CHAIN = FT.Chain(["Tail_1", "Tail_2", "Tail_3"], omega=(0.36, 0.3, 0.25), zeta=(0.5, 0.45, 0.4),
                      gain=(1.1, 1.2, 1.3), cap=(16, 20, 24))
NECK_CHAIN = FT.Chain(["Neck", "Head"], omega=(0.6, 0.5), zeta=(0.5, 0.45), gain=(1.0, 1.2), cap=(12, 16))
CHAINS = [TAIL_CHAIN, NECK_CHAIN]


def follow(fn):
    return FT.wrap_clip(fn, CHAINS)


CLIPS = {"Walk": follow(walk_spec), "Idle": follow(idle_spec), "Hit": follow(hit_spec), "Death": follow(death_spec),
         "Block": follow(block_spec)}
# LOD1: jaw into the head, one tail bone. 16 bones.
LOD_DROP = {"Jaw": "Head", "Tail_2": "Tail_1", "Tail_3": "Tail_1"}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_3")}
RIGID = {"ChestPlate", "Visor", "ShoulderPlate", "Shield", "ShieldHook", "Sword", "SwordTooth", "Bracer", "Horn", "Jaw",
         "Head", "BrowRidge", "VisorKeel", "ChinHook", "UpperGum", "LowerGum", "Palate", "Tongue", "Throat", "TuskBoss",
         "Tusk"}


def drop_specks(obj, min_verts=8):
    """Round 49F: deletes islands of fewer than `min_verts` vertices from a game mesh (single triangles the remesh
    leaves where a needle tip is thinner than a voxel: they hover as specks off the spikes). Their UV space stays
    unused; nothing else moves. Returns how many islands went."""
    import bmesh
    import check_floating_parts as F
    small = [isl for isl in F.islands(obj.data) if len(isl) < min_verts]
    if not small:
        return 0
    doomed = set(int(i) for isl in small for i in isl)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.verts[i] for i in sorted(doomed)], context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    C.log(obj.name, "dropped specks", len(small))
    return len(small)


def build_asset(name, builder, tris, voxel=0.0042, k=K):
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.1,
                              extrusion=0.014, ray=0.045, emission_strength=2.5,
                              extra=H.weathering(k, markings=PAL.dark, marking_amount=0.5, seed=33), specular=0.3,
                              mirror=False, symmetric=False, k=k, families=FAMILIES, low_size=LOW_TEX)


def asymmetric_decimate():
    """The Bulwark is not mirror-symmetric (shield left, sword right): common.build_lod always asks for a symmetric
    decimation, so this run swaps in a plain one (only inside this Blender process)."""
    plain = C.decimate

    def decimate(mesh, target_tris, name, symmetric=True):
        return plain(mesh, target_tris, name, symmetric=False)
    C.decimate = decimate


def main():
    asymmetric_decimate()
    uvlayout.SPARSE_FIX = True  # round 49D: serrations and hooks cut into many small islands
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = build_asset(NAME, builder, GAME_TRIS)
    specks = drop_specks(asset.low)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "block_up_frame": BLOCK_UP_FRAME,
                          "walk_distance_per_loop": round(STRIDE / STANCE, 3), "unity_scale": GAME_SCALE,
                          "pivot": "ground, between the feet", "round49f_specks_dropped": specks,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 10, (0.3, 0.9), -0.15), ("Block", 20, (1.3, 0.3), 0.3),
              ("Walk", 6, (-0.4, -0.5), 0.0), ("Walk", 22, (0.9, -0.7), -0.3)]
    K2.enemy_previews("bulwark", arm, asset.low, actions, lod, out, asset, target=(0, 0.0, 0.55), distance=3.2,
                      game_scale=GAME_SCALE, move=("Walk", 4), layout=layout,
                      strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Block", "front", range(0, BLOCK_FRAMES, 3)),
                              ("Death", "close", range(0, 45, 6))],
                      macros=[("shield", G(-0.38, 0.58, 0.12), 0.5, -80, 10), ("head", G(0.0, 0.92, 0.32), 0.45, -35, 15),
                              ("sword", G(0.32, 0.3, 0.45), 0.5, -10, 20)],
                      rig_scale=1.4, power=("Block", 20))
    # Round 46: the close-up detail (eyes, fangs, talons) as extra parts, exported again with the body.
    import bulwark_detail
    bulwark_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
