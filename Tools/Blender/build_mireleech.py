"""Mireleech: the marsh leech that leaves a mud slick hurrying everything behind it (round 15, see
ArtSource/DESIGN_NOTES.md).

A long, low body of seven segments: each carries a hard dark dorsal ring plate with two backward hooks and a pair
of yellow sensor pits, and the swollen flesh between the plates glows lime. The front is a sucker of three hinged
lip petals (own bones Petal_T/L/R) lined with hooked teeth round a glowing gullet; a flat ringed rear sucker anchors
the tail end. No eyes.

Clips: Crawl (loop: a hump runs from the tail to the head), Idle (loop), Hit, Death, Latch (rears up, petals open,
lunges and clamps on at LATCH_BITE_FRAME, then sucks in pulses). Also exports Mireleech_LOD1.

Run: blender --background --python Tools/Blender/build_mireleech.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
from common import G  # noqa: E402

NAME = "Mireleech"
K = 1.0
GAME_SCALE = 0.7

# Data colours: mud-brown body (0.32, 0.3, 0.2), yellow eyes, lime accent. Plates dark mud-brown, flesh olive with
# the lime glow between the plates, yellow sensor pits.
PAL = H.Palette(top=(0.2, 0.15, 0.085), side=(0.09, 0.066, 0.038), belly=(0.05, 0.04, 0.025), seam=(0.025, 0.018, 0.01),
                dark=(0.07, 0.05, 0.03), glow=(0.62, 1.0, 0.18), eye=(1.0, 0.9, 0.35), membrane=(0.13, 0.14, 0.035),
                bone=(0.72, 0.62, 0.44))

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

# Spine nodes from the tail to the mouth (forward, height), and the body radius at each segment.
NODES = [(-0.52, 0.075), (-0.38, 0.085), (-0.23, 0.095), (-0.08, 0.1), (0.07, 0.1), (0.21, 0.098), (0.34, 0.095), (0.46, 0.09)]
RADII = [0.068, 0.084, 0.098, 0.106, 0.106, 0.098, 0.088]
CHAIN = ["Seg_1", "Seg_2", "Seg_3", "Seg_4", "Seg_5", "Seg_6", "Head"]
MOUTH = G(0, 0.095, 0.47)
# Petals: the angle of each petal's centre line round the mouth (0 = up), right side and midline only.
PETALS = {"Petal_T": 0.0, "Petal_R": 2 * math.pi / 3, "Petal_L": -2 * math.pi / 3}


def node(i):
    f, h = NODES[i]
    return G(0, h, f)


def petal_dir(a):
    """Unit vector in the mouth plane (x, up) at angle a from up, clockwise seen from the front (right = +x)."""
    return G(math.sin(a), math.cos(a), 0.0)


def petal_points(a, open_=0.0):
    d = petal_dir(a)
    fwd = G(0, 0, 1)
    root = MOUTH + d * 0.03
    mid = MOUTH + d * 0.075 + fwd * 0.022
    tip = MOUTH + d * 0.105 + fwd * 0.036
    return root, mid, tip


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    for i, name in enumerate(CHAIN):
        a, c = node(i), node(i + 1)
        mid = a.lerp(c, 0.5)
        r = RADII[i]
        seg = (c - a).length
        chain = [name] + ([CHAIN[i + 1]] if i + 1 < len(CHAIN) else [])
        b.ellipsoid("Flesh", mid, (r * 1.02, seg * 0.9, r * 0.66), bone=name, segments=(32, 16),
                    style=H.glow_flesh(PAL, k, glow=0.22, flank=(0.6, 0.98), folds=7.0))
        b.parts[-1].chain = chain
        b.ellipsoid("Plate", mid + G(0, r * 0.32, -0.01), (r * 1.08, seg * 0.46, r * 0.55), bone=name, segments=(32, 14),
                    style=K2.armour_plate(PAL, k, rim_glow=0.6, chevron=0.7 if i in (2, 4) else 0.0, lip=0.45, cells=7,
                                          seed=10 + i))
        for side, sx in SIDES:
            base = mid + G(r * 0.62 * sx, r * 0.7, -seg * 0.12)
            b.tube("PlateHook", [base, base + G(0.025 * sx, 0.012, -0.03), base + G(0.03 * sx, 0.0, -0.065)],
                   [0.012, 0.007, 0.001], sides=8, chain=[name], style=H.hook(PAL, k, glow=0.6, glow_from=0.7))
            b.ellipsoid("Pit", mid + G(r * 0.98 * sx, r * 0.15, 0.0), (0.012, 0.012, 0.012), bone=name,
                        style=H.eye(PAL, k, 0.85), segments=(10, 6))
    # The rear sucker: a flat ringed disc under the tail end.
    b.ellipsoid("Sucker", node(0) + G(0, -0.045, 0.04), (0.075, 0.07, 0.022), bone="Seg_1",
                style=H.band_plates(PAL, k, 4, height=0.002, glow=0.0, axis=0), segments=(28, 10))
    # The front: a collar round the three-petal sucker, a glowing gullet, teeth on the petals.
    b.ring("Lip", MOUTH + G(0, 0, -0.005), G(0, 0, 1), 0.052, 0.016, arcs=3, sides=8, chain=["Head"],
           style=H.band_plates(PAL, k, 1, height=0.0), ref=Vector((1, 0, 0)))
    b.ellipsoid("Gullet", MOUTH + G(0, 0, -0.015), (0.04, 0.02, 0.04), bone="Head", style=H.inner_glow(PAL, 1.0), segments=(16, 8))
    for name, a in PETALS.items():
        root, mid, tip = petal_points(a)
        b.tube("Petal", [root, mid, tip], [0.05, 0.046, 0.012], sides=10, chain=[name],
               style=K2.armour_plate(PAL, k, rim_glow=1.0, chevron=0.0, lip=0.9, cells=0, seed=40), flatten=0.32)
        d = petal_dir(a)
        for j in range(3):
            tt = 0.25 + 0.3 * j
            c = root.lerp(tip, tt) - d * 0.006 + G(0, 0, 0.004)
            t_tip = c - d * 0.03 + G(0, 0, -0.012)
            b.tube("Tooth", [c, c.lerp(t_tip, 0.5) + G(0, 0, 0.006), t_tip], [0.008, 0.005, 0.0008], sides=6, chain=[name],
                   style=H.hook(PAL, k, glow=0.5))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.15), None, True)]
    parent = "Root"
    for i, name in enumerate(CHAIN):
        out.append((name, node(i), node(i + 1), parent, True))
        parent = name
    for name, a in PETALS.items():
        root, mid, tip = petal_points(a)
        out.append((name, root, tip, "Head", True))
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def hump(x, height, centre, width):
    """Height and slope of a travelling bump along the body (forward coordinate x)."""
    h = height * math.exp(-((x - centre) / width) ** 2)
    return h, -2 * (x - centre) / width ** 2 * h


def body_specs(offset=None, wave=(0.0, 0.0), sway=(0.0, 0.0), lift=0.0, head=0.0, petals=0.0, curl=0.0, throb=(0.0, 0.0),
               bump=None):
    """wave: (amplitude, phase) of the vertical hump along the chain (moves to the head as the phase grows);
    sway: sideways wave; lift: front raised; petals: how far the petals open (rad, < 0 clamps); curl: sideways curl
    (death); throb: (amplitude, phase) of a short pumping pitch along the body (sucking)."""
    amp, ph = wave
    samp, sph = sway
    tamp, tph = throb
    specs = {"Root": {"rot": []}}
    n = len(CHAIN)
    # bump: (height, centre, width) of a hump whose slope sets each segment's pitch: the ends stay on the ground.
    prev = 0.0
    hump_rel = [0.0] * n
    if bump is not None:
        for i in range(n):
            x = 0.5 * (NODES[i][0] + NODES[i + 1][0])
            ang = math.atan(hump(x, *bump)[1])
            hump_rel[i] = ang - prev
            prev = ang
        offset = (offset if offset is not None else Vector()) + Vector((0, 0, hump(NODES[0][0], *bump)[0]))
    for i, name in enumerate(CHAIN):
        a = amp * math.sin(ph - i * 1.05) + tamp * math.sin(tph - i * 0.7) + hump_rel[i]
        s_ = samp * math.sin(sph - i * 0.9) + curl * (0.4 + 0.6 * i / (n - 1))
        rots = [(Z, s_), (X, -a)]
        if name == "Seg_1":
            specs[name] = {"rot": rots, "loc": offset if offset is not None else Vector()}
        else:
            specs[name] = {"rot": rots}
    specs["Seg_5"]["rot"].append((X, -lift * 0.3))
    specs["Seg_6"]["rot"].append((X, -lift * 0.4))
    specs["Head"]["rot"].append((X, -lift * 0.3 + head))
    for pname, ang in PETALS.items():
        d = petal_dir(ang)
        hinge = d.cross(G(0, 0, 1)).normalized()
        specs[pname] = {"rot": [(hinge, -petals)]}
    return specs


CRAWL_FRAMES = 30
CRAWL_DISTANCE = 0.32  # model units per loop (the leech is quick; the hump is the cue)


def crawl_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return body_specs(bump=(0.07, -0.85 + 1.7 * t, 0.12),
                          sway=(math.radians(5), w), lift=math.radians(3) + math.radians(2) * math.sin(w + 1.0),
                          head=math.radians(5) * math.sin(w + 2.0), petals=math.radians(8) * (0.5 + 0.5 * math.sin(2 * w)))
    return CRAWL_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        probe = max(0.0, math.sin(w + 0.5)) ** 3
        return body_specs(wave=(math.radians(2), w), sway=(math.radians(4), w), lift=math.radians(6) + math.radians(14) * probe,
                          head=math.radians(8) * math.sin(2 * w), petals=math.radians(6) + math.radians(18) * probe,
                          throb=(math.radians(1.5), 3 * w))
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = K2.recoil(t, 0.3, 6.0, 12.0)
        return body_specs(offset=Vector((0, 0.02 * k, -0.008 * k)), wave=(math.radians(7) * k, 1.0), lift=-math.radians(10) * k,
                          head=-math.radians(12) * k, petals=-math.radians(8) * k, sway=(math.radians(6) * k, 0.5))
    return 12, spec, False


def death_spec(poser):
    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.65, t)
        writhe = math.exp(-t * 4) * math.sin(t * 32)
        return body_specs(offset=Vector((0, 0, 0.015 * jerk - 0.04 * fall)), wave=(math.radians(12) * writhe, t * 22),
                          lift=math.radians(25) * jerk - math.radians(6) * fall, head=-math.radians(12) * fall,
                          petals=math.radians(35) * fall, curl=math.radians(16) * fall)
    return 40, spec, False


LATCH_FRAMES = 36
LATCH_BITE_FRAME = 13


def latch_spec(poser):
    bite = LATCH_BITE_FRAME / LATCH_FRAMES

    def spec(t, f):
        rear = K2.pulse(t, 0.0, 0.25, bite)
        lunge = K2.ease(0.25, bite, t) * (1 - K2.ease(0.8, 1.0, t))
        hold = K2.ease(bite, bite + 0.05, t) * (1 - K2.ease(0.85, 1.0, t))
        suck = hold * max(0.0, math.sin((t - bite) * 2 * math.pi * 3.0))
        fwd = Vector((0, -0.11 * lunge, 0))
        return body_specs(offset=fwd, lift=math.radians(38) * rear - math.radians(4) * hold,
                          head=-math.radians(10) * rear + math.radians(8) * hold,
                          petals=math.radians(55) * rear - math.radians(6) * hold + math.radians(10) * suck,
                          throb=(math.radians(4) * hold, (t - bite) * 2 * math.pi * 3.0),
                          wave=(math.radians(5) * rear, 1.5))
    return LATCH_FRAMES, spec, False


CLIPS = {"Crawl": crawl_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Latch": latch_spec}
RIGID = {"Plate", "Sucker", "Lip"}
# LOD1: 11 bones already; nothing to fold.
LOD_DROP = {}
LOD_AIMS = {}


def main():
    builder = build_parts()
    layout = [("Crawl", 0, (-1.0, 0.6), 0.2), ("Crawl", 8, (0.3, 0.9), -0.15), ("Latch", LATCH_BITE_FRAME, (1.3, 0.3), 0.3),
              ("Idle", 30, (-0.4, -0.5), 0.0), ("Crawl", 20, (0.9, -0.7), -0.3)]
    previews = dict(target=(0, 0.0, 0.12), distance=2.0, move=("Crawl", 4), layout=layout,
                    strips=[("Crawl", "side", range(0, CRAWL_FRAMES, 4)), ("Latch", "side", range(0, LATCH_FRAMES, 4)),
                            ("Death", "close", range(0, 41, 6))],
                    macros=[("plates", G(0.05, 0.17, -0.05), 0.42, -50, 40), ("mouth", G(0.0, 0.1, 0.5), 0.35, -25, 10),
                            ("pits", G(0.1, 0.1, -0.2), 0.35, -85, 10)],
                    rig_scale=1.1, power=("Latch", 8))
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3900, families=("membrane", "chitin", "crystal"),
                    game_scale=GAME_SCALE, rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
                    extra=H.weathering(K, scratches=0.3, dust=0.45, seed=19), specular=0.45,
                    report={"crawl_distance_per_loop": CRAWL_DISTANCE, "latch_bite_frame": LATCH_BITE_FRAME,
                            "pivot": "ground, body centre", "trail_origin": "under Seg_1 (the rear sucker), on the ground"},
                    previews=previews)
    # Round 46: the close-up detail (eyespots, petal teeth, gullet ring) as extra parts, exported again with the body.
    import mireleech_detail
    mireleech_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
