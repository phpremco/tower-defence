"""SlimeMonarch: the Oozer boss: floods of slime and more oozers (round 10, see ArtSource/DESIGN_NOTES.md).

The Oozer's body plan (build_oozer.py) as a monarch: a crown of six eye stalks (three pairs, the front ones long),
a broader mantle crowded with toxin glands, a crest of glowing spines, a wide slime foot and two brood sacs on the
back (on their own bones, bone scale) where young oozers come out. Built in Oozer units, then scaled as a whole
(creature_kit2.scale_rig).

Round 49E (Remco: "those 2 watermelon-like things are not logical; maybe this should become a kind of transparent vat
with acid-like slime that moves when it moves"): the two brood sacs are gone. On its back the monarch carries one
acid vat: a bag of thin, wet, veined membrane grown out of the mantle (lit from inside where the acid stands, darker
above it, as if you look through it), open at the top under a ragged fleshy lip with cartilage hoops arching over it,
and filled to below the rim with glowing acid slime. The slime's surface is its own bone, `Slosh` (child of Body_2,
pivot at the slime level): every clip keys it from a damped-spring slosh driven by how the vat itself moves (it lags,
tilts and rings out; it bulges by bone scale), so the acid visibly sloshes when the monarch crawls, rears or is hit.
Two short spouts on the vat's flanks sit exactly on the old `Sac_L`/`Sac_R` sockets (same bone names and positions,
the young oozers still come out there); their scale keys now make the spouts swell and spit on Spawn.

Clips: Crawl, Idle, Hit, Death (from the Oozer, with the sacs), Flood (the power: it rears up and slams the front
down; slime bursts out at FLOOD_SLAM_FRAME) and Spawn (the sacs squeeze; young oozers leave at SPAWN_RELEASE_FRAME
from the Sac bones). Also exports SlimeMonarch_LOD1.

Run: blender --background --python Tools/Blender/build_slimemonarch.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import build_oozer as OZ  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "SlimeMonarch"
LOD_NAME = "SlimeMonarch_LOD1"
GAME_TRIS = 13000
LOD_TRIS = 6000
M = 2.0
GAME_SCALE = 1.5 * 0.6
OOZER_SCALE = 0.7

X, Yv, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
SIDES = OZ.SIDES

STALKS = {1: ((0.04, 0.21, 0.37), (0.09, 0.44, 0.47)), 2: ((0.07, 0.22, 0.31), (0.16, 0.4, 0.33)),
          3: ((0.085, 0.22, 0.25), (0.2, 0.33, 0.2))}
CFG = dict(OZ.CFG)
CFG.update({"glands": 56, "gland_size": 0.17, "stalks": STALKS, "spines": 6, "blister_sink": 0.014})
# The base module's stalk list drives its clip specs: point it at the monarch's six stalks.
OZ.STALK_NAMES = [f"Stalk_{s}{n}" for s, _ in SIDES for n in STALKS]
SAC = (0.1, 0.27, -0.2)
SCALE_BONES = OZ.STALK_NAMES + ["Sac_L", "Sac_R", "Slosh"]

# Round 49E: the acid vat (Oozer units): a vertical bag on the back between the mantle and the rear roll.
VAT_C = (0.0, -0.19)             # (x, forward) of its axis
VAT_BASE, VAT_RIM = 0.2, 0.38    # up: the base sits inside the mantle, the rim stands above the back
VAT_R = (0.108, 0.13)            # widest half-width (side, length)
VAT_WALL = 0.011
SLIME_LEVEL = 0.332              # up: the acid surface (the Slosh bone's pivot)
SPOUT = (0.1, 0.27, -0.12)       # the Sac bones' heads (the spawn sockets), unchanged
ACID = H.Palette(top=(0.62, 0.86, 0.16), side=(0.4, 0.62, 0.08), belly=(0.2, 0.32, 0.03), seam=(0.06, 0.09, 0.01),
                 dark=(0.1, 0.16, 0.02), glow=(0.74, 1.0, 0.2), eye=(1.0, 1.0, 0.6), membrane=(0.3, 0.42, 0.07),
                 bone=(0.86, 0.9, 0.6))


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def vat_profile(v):
    """Half-width scale of the vat bag at height fraction v (0 base .. 1 rim): a pot belly that narrows to the lip."""
    return 0.78 + 0.3 * math.sin(math.pi * min(1.0, v * 1.15 + 0.05)) - 0.12 * v * v


def rim_height(a):
    """The ragged rim: its height wobbles round the lip (a grown bag, not a cut pot)."""
    return VAT_RIM + 0.012 * math.sin(3 * a + 0.7) + 0.007 * math.sin(7 * a + 2.1) - 0.01 * max(0.0, math.cos(a))


def vat_point(u, v, inset=0.0):
    a = 2 * math.pi * u
    top = rim_height(a)
    up = VAT_BASE + (top - VAT_BASE) * v
    sc = vat_profile(v)
    rx, rf = VAT_R[0] * sc - inset, VAT_R[1] * sc - inset
    # A slow wobble in the wall so the bag sags unevenly (membrane, not glass).
    wob = 1.0 + 0.04 * math.sin(2 * a + 5 * v + 0.4) + 0.025 * math.sin(5 * a - 3 * v)
    return G(VAT_C[0] + rx * wob * math.cos(a), up, VAT_C[1] + rf * wob * math.sin(a))


def vat_membrane(pal, acid, k, level, seed=3):
    """Thin, wet, translucent membrane lit from inside: below the acid level the glowing slime shows through (bright
    lime with darker suspended clots), above it the empty bag is a dark olive skin; a net of dark veins over all, a
    wet sheen, and a darker band where the acid meets the wall."""
    def style(s, idx, part):
        p = s.p[idx]
        up = p[:, 2]
        below = 1 - C.smoothstep(level - 0.025 * k, level + 0.01 * k, up)
        meniscus = 1 - C.smoothstep(0.0, 0.012 * k, np.abs(up - level))
        cells = C.fibonacci_sphere(70, seed=seed)
        u = p - np.array([VAT_C[0] * k, -VAT_C[1] * k, (VAT_BASE + VAT_RIM) * 0.5 * k])
        u = u / np.maximum(np.linalg.norm(u, axis=1, keepdims=True), 1e-6)
        vein = 1 - C.smoothstep(0.0, 0.03, C.voronoi_edge(u, cells))
        fine = 1 - C.smoothstep(0.0, 0.08, np.abs(C.value_noise(p, 60 / k, seed + 1)))
        clot = C.smoothstep(0.15, 0.6, C.value_noise(p, 14 / k, seed + 2))
        through = C.lerp3(acid.top, acid.belly, clot * 0.6)
        skin = C.lerp3(pal.membrane * 0.55, pal.side, 0.4) * (0.85 + 0.3 * C.value_noise(p, 25 / k, seed + 3))[:, None]
        col = C.lerp3(skin, through, below * 0.85)
        col = C.lerp3(col, pal.seam, np.maximum(vein * 0.85, fine * 0.35))
        col = C.lerp3(col, acid.dark, meniscus * 0.6)
        s.albedo[idx] = col
        # Thicker, darker flesh low down where the bag grows out of the mantle; the light comes through higher up.
        root = 1 - C.smoothstep((VAT_BASE + 0.03) * k, (VAT_BASE + 0.08) * k, up)
        col = C.lerp3(col, pal.side, root * 0.8)
        s.albedo[idx] = col
        lit = below * (0.45 + 0.4 * (1 - clot)) * (1 - vein * 0.9) * (1 - fine * 0.4) * (1 - root * 0.85)
        s.emit[idx] = acid.glow[None, :] * np.clip(lit * 0.85 + meniscus * 0.2, 0, 1)[:, None]
        s.disp[idx] = (0.0016 * vein - 0.0008 * fine) * k
        s.rough[idx] = 0.16 + 0.22 * vein
        H.family(s, idx, membrane=1.0, flow=up)
    return style


def acid_surface(acid, k, seed=5):
    """The open acid: a bright, glossy, bubbling slime surface with darker scum and foam rings."""
    def style(s, idx, part):
        p = s.p[idx]
        L = C.local_points(part, p)
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        top = C.smoothstep(0.1, 0.6, u[:, 2])
        seeds = C.fibonacci_sphere(40, seed=seed)
        bub = C.voronoi_edge(u, seeds)
        ring = 1 - C.smoothstep(0.0, 0.025, np.abs(bub - 0.06))
        scum = C.smoothstep(0.2, 0.7, C.value_noise(p, 20 / k, seed + 1))
        col = C.lerp3(acid.top, acid.side, scum * 0.55)
        col = C.lerp3(col, acid.bone, ring * 0.5 * top)
        s.albedo[idx] = col
        s.emit[idx] = acid.glow[None, :] * np.clip(0.55 + 0.45 * top * (1 - scum * 0.6) + 0.3 * ring * top, 0, 1)[:, None]
        s.disp[idx] = (0.003 * ring - 0.0015 * scum) * k * top
        s.rough[idx] = 0.12 + 0.12 * scum
        H.family(s, idx, membrane=1.0, flow=u[:, 1] * part.radii[1])
    return style


def build_vat(b, k):
    """The acid vat: membrane bag, fleshy lip, cartilage hoops, the acid (Slosh), the two spouts (Sac_L/R)."""
    pal = OZ.PAL
    wall = vat_membrane(pal, ACID, k, SLIME_LEVEL * k)
    b.slab("Vat", lambda u, v: vat_point(u, v), lambda u, v: vat_point(u, v, VAT_WALL), 48, 10, bone="Body_2",
           chain=["Body_2", "Body_1"], style=wall)
    # A thick, lumpy lip of slug flesh round the opening (three overlapping arcs; the remesh fuses them).
    lip = [vat_point(i / 48, 1.0, VAT_WALL * 0.5) for i in range(49)]
    for j in range(3):
        seg = lip[j * 16:(j + 1) * 16 + 2]
        b.tube("VatLip", seg, [0.016 + 0.006 * math.sin(i * 1.7 + j) for i in range(len(seg))], sides=8,
               chain=["Body_2", "Body_1"], style=K2.slug_hide(pal, k, glands=4, seed=70 + j, gland_size=0.1, gland_zone=0.7))
    # Cartilage hoops arching over the opening (they keep the bag's shape; the acid shows between them).
    for u0, u1, h in ((0.06, 0.47, 0.05), (0.2, 0.78, 0.045), (0.6, 0.93, 0.04)):
        a0, a1 = vat_point(u0, 0.97), vat_point(u1, 0.97)
        mid = a0.lerp(a1, 0.5) + Vector((0, 0, h))
        b.tube("VatHoop", [a0, a0.lerp(mid, 0.55) + Vector((0, 0, h * 0.35)), mid,
                           a1.lerp(mid, 0.55) + Vector((0, 0, h * 0.35)), a1],
               [0.011, 0.009, 0.008, 0.009, 0.011], sides=8, chain=["Body_2", "Body_1"],
               style=H.tube(pal, k, pal.side * 1.3, pal.top * 0.8, rings=(0.25, 0.5, 0.75), ring_dark=0.5, rough=0.5))
    # A fleshy collar where the bag grows out of the mantle (it is part of the slug, not a jar set on its back).
    base = [vat_point(i / 48, 0.2, -0.006) for i in range(49)]
    for j in range(3):
        seg = base[j * 16:(j + 1) * 16 + 2]
        b.tube("VatCollar", seg, [0.02 + 0.007 * math.sin(i * 1.3 + j * 2) for i in range(len(seg))], sides=8,
               chain=["Body_2", "Body_1"], style=K2.slug_hide(pal, k, glands=5, seed=74 + j, gland_size=0.12, gland_zone=0.5))
    # Raised veins running down the bag from the lip into the mantle.
    for j in range(7):
        u = (j + 0.3) / 7
        pts = [vat_point(u, v, -0.004) for v in (0.92, 0.65, 0.35, 0.1)]
        b.tube("VatVein", pts, [0.006, 0.007, 0.006, 0.008], sides=6, chain=["Body_2", "Body_1"],
               style=H.tube(pal, k, pal.seam * 2.0, pal.side, rough=0.4))
    # The acid itself: a slumped glowing dome filling the bag to SLIME_LEVEL (its surface sloshes on bone Slosh).
    b.ellipsoid("Acid", G(VAT_C[0], SLIME_LEVEL - 0.075, VAT_C[1]), (VAT_R[0] * 0.98, VAT_R[1] * 0.98, 0.1),
                bone="Slosh", segments=(40, 20), deform=OZ.lumpy(88, amp=0.05, sag=0.0, folds=0.0, freq=4.0),
                style=acid_surface(ACID, k))
    b.parts[-1].chain = ["Slosh"]
    # Two spouts on the flanks, on the spawn sockets: short fleshy nozzles with acid glowing in their mouths.
    for side, sx in SIDES:
        root = gp(SPOUT, sx)
        out = gp((1.0, 0.45, 0.15), sx).normalized()
        b.tube("Spout", [root - out * 0.035, root, root + out * 0.03, root + out * 0.05 + Vector((0, 0, 0.004))],
               [0.03, 0.03, 0.027, 0.033], sides=12, chain=[f"Sac_{side}"],
               style=K2.slug_hide(pal, k, glands=3, seed=80 + (sx > 0), gland_size=0.12, gland_zone=0.6))
        b.ellipsoid("SpoutAcid", root + out * 0.052, (0.022, 0.022, 0.022), bone=f"Sac_{side}", segments=(16, 8),
                    style=H.inner_glow(ACID, 1.0))


def build_parts():
    b = OZ.build_parts(CFG)
    k = OZ.K
    build_vat(b, k)
    # A crest of glowing spines round the back of the head: the crown of the slugs.
    for j in range(5):
        a = math.radians(-60 + 30 * j)
        base = G(0.08 * math.sin(a), 0.22, 0.3 - 0.05 * math.cos(a))
        out = G(math.sin(a) * 0.5, 1.0, -0.6).normalized()
        b.tube("CrownSpine", [base, base + out * 0.05, base + out * 0.09 + G(0, 0, -0.03)], [0.018, 0.01, 0.0015], sides=8,
               chain=["Head"], style=H.hook(OZ.PAL, k, glow=1.0, glow_from=0.5))
    return b


def bones():
    out = OZ.bones(CFG)
    for side, sx in SIDES:
        c = gp(SAC, sx)
        out.append((f"Sac_{side}", c - G(0, 0.0, -0.08), c + G(0, 0.0, -0.1), "Body_2", True))
    # Round 49E: the acid's surface, pivoting at the slime level in the middle of the vat.
    out.append(("Slosh", G(VAT_C[0], SLIME_LEVEL, VAT_C[1]), G(VAT_C[0], SLIME_LEVEL + 0.1, VAT_C[1]), "Body_2", True))
    return out


# Round 49E: the slosh. The acid's surface wants to lie level under (gravity - the vat's acceleration) and gets there
# like a damped spring, so it lags the vat's own tilt, overshoots and rings out. Units: Oozer units and seconds (one
# Oozer unit is about 1.8 m on the monarch in game, so g = 9.81 / 1.8).
SLOSH_G = 9.81 / 1.8
# The natural frequency sits well below the crawl's 1.5 Hz body wave (at 1.5 Hz the acid resonated and pinned at the
# limit); damping lets a hit or the flood slam ring out over about two swings.
SLOSH_HZ, SLOSH_DAMP = 0.9, 0.3
SLOSH_GAIN = 1.0
SLOSH_MAX = math.radians(22)
FPS, SUB = 30, 4


def _vat_state(poser, spec, t, frames):
    # The frame argument is t in frames (fractional between keys): build_slug_strike's clips run on frames.
    pose = poser.solve(spec(t, t * frames))
    m = pose["Body_2"] @ poser.rest["Body_2"].inverted()
    up = m.to_3x3() @ Z
    centre = m @ G(VAT_C[0], SLIME_LEVEL, VAT_C[1])
    return math.atan2(-up.y, up.z), math.atan2(up.x, up.z), centre


def slosh_track(poser, frames, spec, loop):
    """Per frame: (pitch, roll) of the acid surface relative to the vat (rad) and a bulge factor."""
    n = frames * SUB
    dt = 1.0 / (FPS * SUB)
    cycles = 3 if loop else 1
    samples = []
    for i in range(n * cycles + 1):
        t = (i % n) / n if loop else min(1.0, i / n)
        samples.append(_vat_state(poser, spec, t, frames))
    pos = [c for _, _, c in samples]
    w0 = 2 * math.pi * SLOSH_HZ
    th = [samples[0][0], samples[0][1]]
    vel = [0.0, 0.0]
    out = []
    for i in range(len(samples)):
        prev, nxt = pos[max(0, i - 1)], pos[min(len(pos) - 1, i + 1)]
        acc = (nxt - 2 * pos[i] + prev) / (dt * dt) if 0 < i < len(pos) - 1 else Vector()
        eq = (math.atan2(-acc.y, SLOSH_G + acc.z), math.atan2(acc.x, SLOSH_G + acc.z))
        for a in range(2):
            force = -w0 * w0 * (th[a] - eq[a]) - 2 * SLOSH_DAMP * w0 * vel[a]
            vel[a] += force * dt
            th[a] += vel[a] * dt
        if i % SUB == 0:
            rel = [max(-SLOSH_MAX, min(SLOSH_MAX, SLOSH_GAIN * (th[a] - samples[i][a]))) for a in range(2)]
            speed = math.hypot(*vel)
            out.append((rel[0], rel[1], 1.0 + min(0.08, 0.02 * speed)))
    if loop:
        out = out[-(frames + 1):]
        # Close the loop exactly: spread the small seam error over the cycle.
        err = [out[-1][a] - out[0][a] for a in range(3)]
        out = [tuple(o[a] - err[a] * i / frames for a in range(3)) for i, o in enumerate(out)]
    return out


def with_sacs(fn, sac=lambda t: 1.0):
    """The Oozer clip plus the spouts' scale (Sac_L/R) and, round 49E, the acid's slosh (Slosh)."""
    def make(poser):
        frames, spec, loop = fn(poser)
        track = slosh_track(poser, frames, spec, loop) if "Slosh" in poser.rest else None

        def full(t, f):
            specs = spec(t, f)
            for side, _ in SIDES:
                specs[f"Sac_{side}"] = {"rot": [], "scale": sac(t)}
            if track is not None:
                i = min(len(track) - 1, max(0, int(round(t * frames))))
                pitch, roll, bulge = track[i]
                specs["Slosh"] = {"rot": [(X, pitch), (Yv, roll)], "scale": bulge}
            return specs
        return frames, full, loop
    return make


FLOOD_FRAMES = 44
FLOOD_SLAM_FRAME = 24
SPAWN_FRAMES = 48
SPAWN_RELEASE_FRAME = 30


def flood_spec(poser):
    slam = FLOOD_SLAM_FRAME / FLOOD_FRAMES

    def spec(t, f):
        rise = K2.ease(0.0, slam - 0.12, t) * (1 - K2.ease(slam - 0.1, slam, t))
        impact = K2.pulse(t, slam - 0.04, slam + 0.02, 0.9)
        ripple = math.exp(-max(0.0, t - slam) * 6) * math.sin((t - slam) * 50) * (t > slam)
        return OZ.body_specs(offset=Vector((0, 0, -0.02 * impact)), wave=(math.radians(8) * ripple, t * 30),
                             lift=math.radians(55) * rise - math.radians(10) * impact, head=-math.radians(15) * rise + math.radians(10) * impact,
                             stalks={n: (-math.radians(25) * rise + math.radians(30) * impact, 0.0) for n in OZ.STALK_NAMES},
                             retract=1.0 - 0.3 * impact)
    return FLOOD_FRAMES, spec, False


def spawn_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        brace = K2.pulse(t, 0.05, 0.3, 0.95)
        return OZ.body_specs(wave=(math.radians(3) * brace, 3 * w), sway=(math.radians(4) * brace, 2 * w),
                             lift=-math.radians(4) * brace, stalks=OZ.stalk_wander(t, 1.5, 3), retract=1.0)
    return SPAWN_FRAMES, spec, False


def spawn_sac(t):
    rel = SPAWN_RELEASE_FRAME / SPAWN_FRAMES
    swell = 0.35 * K2.pulse(t, 0.05, rel - 0.06, rel) + 0.12 * math.sin(t * 40) * K2.pulse(t, 0.2, 0.4, rel)
    squeeze = -0.3 * K2.pulse(t, rel - 0.03, rel + 0.05, 0.95)
    return 1.0 + swell + squeeze


CLIPS = {
    "Crawl": with_sacs(OZ.crawl_spec, lambda t: 1.0 + 0.05 * math.sin(4 * math.pi * t)),
    "Idle": with_sacs(OZ.idle_spec, lambda t: 1.0 + 0.07 * math.sin(2 * math.pi * t)),
    "Hit": with_sacs(OZ.hit_spec, lambda t: 1.0 - 0.12 * max(0.0, K2.recoil(t))),
    "Death": with_sacs(OZ.death_spec, lambda t: 1.0 - 0.45 * K2.ease(0.1, 0.8, t)),
    "Flood": with_sacs(flood_spec, lambda t: 1.0 + 0.2 * K2.pulse(t, 0.1, 0.45, 0.6)),
    "Spawn": with_sacs(spawn_spec, spawn_sac),
}
LOD_DROP = {f"Stalk_{s}3": "Head" for s, _ in SIDES}
LOD_AIMS = {}
RIGID = OZ.RIGID | {"CrownSpine", "SpoutAcid"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = OZ.build_asset(NAME, builder, GAME_TRIS, voxel=0.0024)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm, SCALE_BONES)
    actions = C.key_clips(poser, CLIPS)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      SCALE_BONES)
    frames = {n: CLIPS[n](poser)[0] for n in CLIPS}
    K2.scale_rig(arm, [asset.low], actions.values(), M)
    K2.scale_rig(lod[0], [lod[1]], lod[2].values(), M)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), arm, asset.low, actions)
    K2.export_rig(os.path.join(C.EXPORTS, NAME, LOD_NAME + ".fbx"), lod[0], lod[1], lod[2])
    C.write_report(NAME, {"asset": NAME, "base_species": "Oozer", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": OZ.TEX,
                          "low_texture_size": OZ.LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details, "actions": frames,
                          "scale_keyed_bones": SCALE_BONES, "flood_slam_frame": FLOOD_SLAM_FRAME,
                          "spawn_release_frame": SPAWN_RELEASE_FRAME, "spawn_point_bones": ["Sac_L", "Sac_R"],
                          "crawl_distance_per_loop": round(OZ.CRAWL_DISTANCE * M, 3), "unity_scale": GAME_SCALE,
                          "model_scale_from_oozer_units": M,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("nodetail"):
        return
    if C.cli_args().get("norender"):
        detail(norender=True)
        return
    B.boss_previews(
        "slimemonarch", arm, asset.low, actions, lod, out, asset, target=(0, 0, 0.15 * M), distance=2.3 * M,
        game_scale=GAME_SCALE, base=dict(asset="Oozer", scale=OOZER_SCALE, clip="Crawl", frame=6),
        move=("Crawl", 4), power=("Flood", 16),
        strips=[("Crawl", "side", range(0, OZ.CRAWL_FRAMES, 5)), ("Flood", "side", range(0, FLOOD_FRAMES, 5)),
                ("Spawn", "close", range(0, SPAWN_FRAMES, 6)), ("Death", "close", range(0, 41, 6))],
        macros=[("glands", G(0.06, 0.27, -0.1) * M, 0.45 * M, -50, 40), ("crown", G(0.0, 0.3, 0.33) * M, 0.45 * M, -30, 20),
                ("sac", gp(SAC, 1) * M, 0.4 * M, -70, 30)],
        rig_scale=2.0, minions=[(-1.5, 0.9, 0.2, 3), (-1.0, -0.7, -0.1, 10), (1.4, 0.8, 0.3, 17), (1.6, -0.5, 0.0, 21)],
        extra_shots=[("spawn", "close", "Spawn", SPAWN_RELEASE_FRAME - 4)])
    detail()


def detail(norender=False):
    """Round 45: the close-up detail (eyes, teeth, claws) as extra parts, exported again with the body."""
    import slimemonarch_detail
    slimemonarch_detail.run(norender)


if __name__ == "__main__":
    main()
