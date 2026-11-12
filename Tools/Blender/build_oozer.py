"""Oozer: the slug on a toxic slime foot whose trail hastes other species and burns the hero and gates (round 10,
see ArtSource/DESIGN_NOTES.md).

Round 49A (Remco: "too perfectly round, its mouth may have a kind of slime teeth"): a sagging, lumpy slug body made
of uneven rolls that slump onto a wide slime foot with a ragged outline, an off-centre mantle hump with glowing toxin
glands, a heavy hood that droops over a slack, gaping maw (the slimy teeth and goo strands are the Face part,
oozer_detail.py), a thick lower lip, soft blisters of different sizes down the back; four eye stalks (a long front
pair, a short rear pair), each with a glowing bulb, on their own bones. Not mirrored: the lumps and folds differ left
and right. Dark olive-brown mottled hide so the lime glow carries.

Clips: Crawl (loop: a pedal wave runs from tail to head), Idle (loop), Hit, Death. The stalks retract by bone scale
(Hit, Death). Also exports Oozer_LOD1. Geometry and clips take a `cfg` so the SlimeMonarch can reuse them.

Run: blender --background --python Tools/Blender/build_oozer.py -- [--preview <dir>] [--norender]
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "Oozer"
LOD_NAME = "Oozer_LOD1"
K = 1.0
GAME_TRIS = 3800
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("membrane", "chitin", "crystal")
GAME_SCALE = 0.7

# Species colours from ProjectGenerator.Bestiary: lime body (0.55, 0.8, 0.15), yellow eyes, lime accent and slime.
# The hide is pushed to a dark olive-brown so the lime glands, foot and eyes carry the species colour.
PAL = H.Palette(top=(0.3, 0.3, 0.1), side=(0.13, 0.12, 0.04), belly=(0.05, 0.045, 0.02), seam=(0.03, 0.025, 0.01),
                dark=(0.09, 0.07, 0.03), glow=(0.66, 1.0, 0.16), eye=(1.0, 0.98, 0.5), membrane=(0.4, 0.55, 0.08),
                bone=(0.85, 0.82, 0.6))

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

SPINE = [G(0, 0.12, -0.44), G(0, 0.13, -0.18), G(0, 0.14, 0.08), G(0, 0.14, 0.29), G(0, 0.13, 0.46)]
BODY_BONES = ["Body_1", "Body_2", "Body_3", "Head"]
# Eye stalks: (root, tip) on the right side; 1 = long front pair, 2 = short rear pair.
STALKS = {1: ((0.045, 0.2, 0.37), (0.1, 0.4, 0.46)), 2: ((0.075, 0.21, 0.29), (0.15, 0.31, 0.27))}

CFG = {"glands": 40, "gland_size": 0.2, "stalks": STALKS, "spines": 5}


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def lumpy(seed, amp=0.12, sag=0.35, droop=0.0, folds=0.05, freq=2.6, mirror=False):
    """A deform for PartBuilder.ellipsoid (unit-sphere local: x side, -y forward, z up) that breaks the perfect
    ellipsoid: uneven lumps (sum of tilted sine waves, seeded, so left and right differ; `mirror` makes them match, for the mouth parts the symmetric detail teeth sit
    on), creased folds running
    roughly across the body, a slumping underside that flattens and spreads, and `droop`: the front sags down."""
    rng = random.Random(seed)
    waves = []
    for _ in range(6):
        d = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))).normalized()
        waves.append((d, rng.uniform(0, 2 * math.pi), rng.uniform(0.5, 1.0)))
    total = sum(w for _, _, w in waves)
    tilt = rng.uniform(-0.6, 0.6)
    phase = rng.uniform(0, 2 * math.pi)

    def deform(v):
        q = Vector((abs(v.x), v.y, v.z)) if mirror else v
        bump = sum(w * math.sin(freq * q.dot(d) + ph) for d, ph, w in waves) / total
        fold = -abs(math.sin(7.0 * q.y + tilt * q.x * 3.0 + phase)) ** 3
        r = 1.0 + amp * bump + folds * fold * max(0.0, v.z + 0.3)
        x, y, z = v.x * r, v.y * r, v.z * r
        if z < 0:
            spread = -v.z
            z *= 1 - sag
            x *= 1 + 0.45 * sag * spread
            y *= 1 + 0.15 * sag * spread
        z -= droop * max(0.0, -v.y) ** 2
        return Vector((x, y, z))
    return deform


def ragged(seed, amp=0.1):
    """The slime foot's outline: a wobbling, uneven edge instead of an oval."""
    rng = random.Random(seed)
    terms = [(n, rng.uniform(0, 2 * math.pi), rng.uniform(0.4, 1.0)) for n in (3, 5, 7, 11)]

    def deform(v):
        a = math.atan2(v.y, v.x)
        k = 1.0 + amp * sum(w * math.sin(n * a + ph) for n, ph, w in terms) / len(terms)
        return Vector((v.x * k, v.y * k, v.z))
    return deform


# Body rolls (forward, up, x offset, radii (side, length, height), seed, bones): uneven sizes, a little off the
# midline in turn, so the back reads as slumped folds of flesh, not one smooth pill.
ROLLS = [(-0.33, 0.085, 0.012, (0.125, 0.16, 0.075), 11, ["Body_1"]),
         (-0.13, 0.108, -0.014, (0.165, 0.19, 0.102), 12, ["Body_1", "Body_2"]),
         (0.08, 0.118, 0.01, (0.16, 0.17, 0.108), 13, ["Body_2", "Body_3"]),
         (0.25, 0.12, -0.008, (0.135, 0.12, 0.098), 14, ["Body_3", "Head"])]
# The maw: a drooping hood above, a thick sagging lip below, a dark throat between (Oozer units; oozer_detail places
# the slime teeth and the goo in the gap).
MOUTH = G(0, 0.09, 0.45)
HOOD = (G(0, 0.178, 0.36), (0.132, 0.12, 0.074))
LOWER_LIP = (G(0, 0.045, 0.415), (0.094, 0.058, 0.032))
# The throat: dark olive flesh with a dim lime glow deep inside, so the pale glowing teeth and strands stand out.
THROAT = H.Palette(top=(0.1, 0.1, 0.03), side=(0.05, 0.05, 0.015), belly=(0.03, 0.03, 0.01), seam=(0.02, 0.02, 0.005),
                   dark=(0.03, 0.03, 0.01), glow=(0.11, 0.15, 0.02), eye=(1.0, 0.98, 0.5))
BLISTERS = [(0.17, 0.025, 0.024), (0.05, -0.03, 0.03), (-0.06, 0.035, 0.02), (-0.16, -0.02, 0.028), (-0.27, 0.02, 0.018),
            (0.0, 0.06, 0.016)]


def build_parts(cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    chain = list(BODY_BONES)
    b.ellipsoid("Foot", G(0, 0.035, -0.01), (0.225, 0.48, 0.04), bone="Body_2", deform=ragged(cfg.get("seed", 0) + 3),
                style=K2.slime_foot(PAL, k, waves=10.0, edge_glow=0.75), segments=(56, 20))
    b.parts[-1].chain = chain
    seed0 = cfg.get("seed", 0)
    for f, up, dx, radii, seed, bones in ROLLS:
        b.ellipsoid("Body", G(dx, up, f), radii, bone=bones[0], segments=(40, 20),
                    deform=lumpy(seed + seed0, amp=0.16, sag=0.5, folds=0.12, freq=3.2),
                    style=K2.slug_hide(PAL, k, glands=6, seed=seed, gland_size=0.12, gland_zone=0.5))
        b.parts[-1].chain = bones
    b.ellipsoid("Mantle", G(0.018, 0.185, -0.07), (0.15, 0.28, 0.078),
                Matrix.Rotation(math.radians(3), 3, "X") @ Matrix.Rotation(math.radians(5), 3, "Z"), bone="Body_2",
                segments=(48, 24), deform=lumpy(seed0 + 21, amp=0.16, sag=0.2, folds=0.06),
                style=K2.slug_hide(PAL, k, glands=cfg["glands"], seed=5, gland_size=cfg["gland_size"],
                                   gland_zone=0.05, grooves=7.0))
    b.parts[-1].chain = ["Body_1", "Body_2", "Body_3"]
    # The hood droops over the mouth; the lower lip is a thick, sagging roll; the gullet glows between them.
    b.ellipsoid("Head", HOOD[0], HOOD[1], bone="Head", segments=(36, 18),
                deform=lumpy(seed0 + 31, amp=0.27, sag=0.12, droop=0.5, folds=0.14, freq=3.4, mirror=True),
                style=K2.slug_hide(PAL, k, glands=8, seed=7, gland_size=0.12, gland_zone=0.6))
    b.parts[-1].chain = ["Head", "Body_3"]
    b.ellipsoid("LowerLip", LOWER_LIP[0], LOWER_LIP[1], bone="Head", segments=(32, 16),
                deform=lumpy(seed0 + 41, amp=0.14, sag=0.3, folds=0.04, mirror=True),
                style=K2.slug_hide(PAL, k, glands=3, seed=9, gland_size=0.1, gland_zone=0.7))
    b.parts[-1].chain = ["Head", "Body_3"]
    b.ellipsoid("Gullet", MOUTH + G(0, 0.0, -0.05), (0.075, 0.05, 0.034), bone="Head", style=H.inner_glow(THROAT, 0.7),
                segments=(20, 10))
    # Soft glowing blisters of different sizes down the back (they replace the round-10 spine row).
    for i in range(cfg["spines"]):
        f, dx, r = BLISTERS[i % len(BLISTERS)]
        # `blister_sink` (round 49E, SlimeMonarch only; 0 keeps the Oozer as exported): seats the blisters deeper so
        # none comes loose when the front rears up.
        up = 0.26 - 0.1 * abs(f + 0.05) - cfg.get("blister_sink", 0.0)
        b.ellipsoid("Blister", G(dx, up, f), (r, r * 1.2, r * 0.7), bone="Body_2" if f < -0.05 else "Body_3",
                    segments=(16, 8), deform=lumpy(seed0 + 50 + i, amp=0.2, sag=0.5, folds=0.0),
                    style=K2.bladder(PAL, k, cells=8, seed=60 + i, glow=0.9, plate=0.0))
    for side, sx in SIDES:
        for n, (root, tip) in cfg["stalks"].items():
            r, t = gp(root, sx), gp(tip, sx)
            mid = r.lerp(t, 0.5) + gp((0.015, 0.0, -0.02), sx)
            b.tube("Stalk", [r, mid, t], [0.022, 0.013, 0.011], sides=8, chain=[f"Stalk_{side}{n}"],
                   style=H.tube(PAL, k, PAL.side, PAL.top, rings=(0.3, 0.5, 0.7), ring_dark=0.5))
            b.ellipsoid("EyeBulb", t + (t - mid).normalized() * 0.012, (0.024, 0.024, 0.022), bone=f"Stalk_{side}{n}",
                        style=H.eye(PAL, k, 0.9), segments=(14, 8))
    return b


def bones(cfg=CFG):
    out = [("Root", G(0, 0, 0), G(0, 0, 0.2), None, True)]
    parent = "Root"
    for i, name in enumerate(BODY_BONES):
        out.append((name, SPINE[i], SPINE[i + 1], parent, True))
        parent = name
    for side, sx in SIDES:
        for n, (root, tip) in cfg["stalks"].items():
            out.append((f"Stalk_{side}{n}", gp(root, sx), gp(tip, sx), "Head", True))
    return out


STALK_NAMES = [f"Stalk_{s}{n}" for s, _ in SIDES for n in (1, 2)]


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, wave=(0.0, 0.0), sway=(0.0, 0.0), lift=0.0, head=0.0, stalks=None, retract=1.0):
    """wave: (amplitude, phase) of the vertical pedal wave along the body; sway: (amplitude, phase) sideways;
    lift: head end raised; stalks: {name: (pitch, yaw)}; retract: stalk scale (1 out, small = pulled in)."""
    amp, ph = wave
    samp, sph = sway
    specs = {"Root": {"rot": []}}
    for i, name in enumerate(BODY_BONES):
        a = amp * math.sin(ph - i * 1.3)
        s_ = samp * math.sin(sph - i * 0.9)
        rots = [(Z, s_), (X, -a)]
        if name == "Body_1":
            specs[name] = {"rot": rots, "loc": offset if offset is not None else Vector()}
        else:
            specs[name] = {"rot": rots}
    specs["Body_3"]["rot"].append((X, -lift * 0.5))
    specs["Head"]["rot"].append((X, -lift * 0.5 + head))
    for name in STALK_NAMES:
        p, y = (stalks or {}).get(name, (0.0, 0.0))
        specs[name] = {"rot": [(Z, y), (X, p)], "scale": retract}
    return specs


def stalk_wander(t, amount=1.0, speed=1):
    w = 2 * math.pi * t * speed
    out = {}
    for j, name in enumerate(STALK_NAMES):
        out[name] = (math.radians(10) * amount * math.sin(w + j * 1.7), math.radians(16) * amount * math.sin(w * (1 + j % 2) + j * 2.3))
    return out


CRAWL_FRAMES = 40
CRAWL_DISTANCE = 0.3  # model units per loop for playback speed (a slug glides; the wave is the cue)


def crawl_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return body_specs(offset=Vector((0, 0, 0.004 * math.sin(2 * w))), wave=(math.radians(3.5), 2 * w),
                          sway=(math.radians(4), w), lift=math.radians(3) + math.radians(2) * math.sin(w),
                          head=math.radians(3) * math.sin(2 * w + 1), stalks=stalk_wander(t, 0.8))
    return CRAWL_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return body_specs(wave=(math.radians(1.5), w), sway=(math.radians(3), w), lift=math.radians(5) * (0.5 + 0.5 * math.sin(w)),
                          head=math.radians(4) * math.sin(w + 1), stalks=stalk_wander(t, 1.3, 2))
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t, 0.3, 6.0, 12.0)
        return body_specs(offset=Vector((0, 0.02 * k, -0.01 * k)), wave=(math.radians(6) * k, 1.0), lift=-math.radians(8) * k,
                          head=-math.radians(10) * k, stalks={n: (math.radians(30) * k, 0.0) for n in STALK_NAMES},
                          retract=1.0 - 0.45 * max(k, 0.0))
    return 12, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.7, t)
        writhe = math.exp(-t * 4) * math.sin(t * 30)
        return body_specs(offset=Vector((0, 0, 0.015 * jerk - 0.05 * fall)), wave=(math.radians(10) * writhe, t * 20),
                          sway=(math.radians(14) * fall, 1.2), lift=math.radians(15) * jerk - math.radians(10) * fall,
                          head=-math.radians(15) * fall, stalks={n: (math.radians(70) * fall, math.radians(20) * fall) for n in STALK_NAMES},
                          retract=1.0 - 0.6 * fall)
    return 40, spec, False


CLIPS = {"Crawl": crawl_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
LOD_DROP = {"Stalk_L2": "Head", "Stalk_R2": "Head"}
LOD_AIMS = {}
RIGID = set()


def build_asset(name, builder, tris, voxel=0.0034, k=K):
    # Round 49A: not mirrored (the lumps and folds differ left and right), so the whole body is unwrapped.
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.08,
                              extrusion=0.012, ray=0.04, emission_strength=2.5,
                              extra=H.weathering(k, scratches=0.2, dust=0.25, seed=17), specular=0.45,
                              mirror=False, symmetric=False, k=k, families=FAMILIES, low_size=LOW_TEX)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = build_asset(NAME, builder, GAME_TRIS)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm, STALK_NAMES)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      STALK_NAMES)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "scale_keyed_bones": STALK_NAMES,
                          "crawl_distance_per_loop": CRAWL_DISTANCE, "unity_scale": GAME_SCALE,
                          "pivot": "ground, body centre", "trail_origin": "under Body_1 (rear), on the ground",
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender") or C.cli_args().get("nodetail"):
        if not C.cli_args().get("nodetail"):
            import oozer_detail
            oozer_detail.run(norender=True)
        return
    layout = [("Crawl", 0, (-1.0, 0.6), 0.2), ("Crawl", 12, (0.3, 0.9), -0.15), ("Crawl", 25, (1.3, 0.3), 0.3),
              ("Idle", 30, (-0.4, -0.5), 0.0), ("Crawl", 33, (0.9, -0.7), -0.3)]
    K2.enemy_previews("oozer", arm, asset.low, actions, lod, out, asset, target=(0, 0.0, 0.15), distance=2.2,
                      game_scale=GAME_SCALE, move=("Crawl", 4), layout=layout,
                      strips=[("Crawl", "side", range(0, CRAWL_FRAMES, 5)), ("Death", "close", range(0, 41, 6))],
                      macros=[("glands", G(0.05, 0.28, -0.1), 0.45, -50, 40), ("mouth", G(0.0, 0.12, 0.46), 0.4, -20, 10),
                              ("foot", G(-0.18, 0.04, -0.1), 0.4, -80, 15)],
                      rig_scale=1.1)
    # Round 46: the close-up detail (stalk eyes, lamprey teeth) as extra parts, exported again with the body.
    import oozer_detail
    oozer_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
