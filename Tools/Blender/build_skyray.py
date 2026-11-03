"""Skyray: the flying ray that lashes gates with its tail (round 5, see ArtSource/DESIGN_NOTES.md).

A flat body between two broad fins. The head is a pointed chitin ram with two hooked pincers curling in ahead of it
and six cyan eye pits on top; the fins are membranes over a fan of raised rays with a glowing leading edge, pale
chevrons across the dark back, a serrated trailing edge and a hooked claw at each tip; a ridge of backward hooks and
two pairs of glowing jet vents run down the back; the pale belly carries chevrons of glowing gill slits; the long
segmented tail is barbed and ends in a glowing three-pronged stinger.

The fin wave is in the bones: each fin has a leading and a trailing chain of three bones along the span, keyed
as a wave that travels from root to tip and from the leading to the trailing edge (Unity's shader wave can be
switched off or kept very light).

Clips: Fly (loop), Idle (hover loop), Hit, Death (spiral fall), Dive (tail strike). Also exports Skyray_LOD1.

Run: blender --background --python Tools/Blender/build_skyray.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "Skyray"
LOD_NAME = "Skyray_LOD1"
K = 1.3
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("membrane", "chitin", "crystal")
GAME_SCALE = 0.7
# Flyers cruise this high over the road in the game (EnemySpawner.AirHeight).
AIR_HEIGHT = 1.6

# Species colours from ProjectGenerator.Bestiary: blue body (0.25, 0.35, 0.65), cyan eyes and accent. A darker
# indigo back with pale chevrons so the fins read from above, a violet flush at the fin roots, a pale belly.
PAL = H.Palette(top=(0.27, 0.38, 0.72), side=(0.1, 0.13, 0.32), belly=(0.05, 0.06, 0.13), seam=(0.025, 0.03, 0.07),
                dark=(0.06, 0.07, 0.19), glow=(0.4, 0.95, 1.0), eye=(0.3, 0.9, 1.0), membrane=(0.2, 0.12, 0.32),
                bone=(0.66, 0.8, 0.96))
UNDER = (0.5, 0.56, 0.68)
FLUSH = (0.3, 0.14, 0.4)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

# ---- Fin outline (right side, game units): u 0..1 root to tip, v 0..1 leading to trailing edge -----------------
FIN_X0, FIN_SPAN = 0.12, 0.86
CHAINS = {"A": 0.22, "B": 0.74}       # chord position of the leading and the trailing bone chain
JOINTS = (0.02, 0.34, 0.67, 1.0)       # span positions of the chain joints


def lead(u):
    # A crescent: the leading edge bulges forward mid-span and sweeps back hard into a pointed tip.
    return 0.24 - 0.52 * np.power(u, 1.6) + 0.07 * np.sin(math.pi * u)


def trail(u):
    chord = 0.56 * (1 - np.power(u, 2.2)) + 0.03
    # Serrated trailing edge: three hooked points that rake backwards.
    saw = np.power((u * 3.2 + 0.15) % 1.0, 3)
    hooks = 0.075 * saw * C.smoothstep(0.18, 0.35, u) * (1 - 0.4 * u)
    return lead(u) - chord - hooks


def fin_up(u, v):
    # Gull shape: a little dihedral at the root, drooping towards the tip, and a cambered chord.
    return 0.012 + 0.05 * u - 0.12 * u * u + 0.016 * np.sin(math.pi * v) * (1 - 0.5 * u)


def fin_half_thickness(u, v):
    body = (0.03 * np.power(1 - u, 1.4) + 0.003) * np.power(np.clip(np.sin(math.pi * np.clip(v, 0, 1)), 0, 1), 0.5)
    rod = 0.008 * (1 - u * 0.6) * (1 - C.smoothstep(0.0, 0.14, v))
    return np.maximum(body + rod, 0.0065)


def fin_point(sx, u, v, side=0.0):
    f = lead(u) - v * (lead(u) - trail(u))
    up = fin_up(u, v) + side * fin_half_thickness(u, v)
    return G(sx * (FIN_X0 + FIN_SPAN * u), float(up), float(f))


def fin_uv(points):
    """(u, v) on the fin of the nearest side for Blender-space points (for the fin style)."""
    u = np.clip((np.abs(points[:, 0]) - FIN_X0) / FIN_SPAN, 0, 1)
    fwd = -points[:, 1]
    lo, hi = lead(u), trail(u)
    v = np.clip((lo - fwd) / np.maximum(lo - hi, 1e-4), 0, 1)
    return u, v


def fin_chain(side, sx, chain):
    names = [f"Fin_{side}_{chain}{i + 1}" for i in range(3)]
    pts = [fin_point(sx, j, CHAINS[chain]) for j in JOINTS]
    return names, pts


TAIL = [G(0, 0.0, -0.3), G(0, 0.005, -0.5), G(0, 0.0, -0.71), G(0, -0.012, -0.92), G(0, -0.024, -1.12),
        G(0, -0.034, -1.3)]
TAIL_BONES = [f"Tail_{i + 1}" for i in range(5)]


def pincer_points(sx):
    return [G(0.08 * sx, 0.0, 0.37), G(0.118 * sx, 0.004, 0.47), G(0.098 * sx, 0.008, 0.56), G(0.038 * sx, 0.002, 0.61)]


def body_top(fwd):
    """Height of the top of the body ellipsoid at `fwd`."""
    q = max(0.0, 1 - ((fwd + 0.02) / 0.36) ** 2)
    return 0.075 * q ** 0.5


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Body", G(0, 0.0, -0.02), (0.2, 0.36, 0.075), bone="Body", segments=(48, 24),
                style=H.band_plates(PAL, k, 5, height=0.004, glow=0.35, shingle=True))
    b.ellipsoid("Belly", G(0, -0.022, -0.03), (0.185, 0.33, 0.055), bone="Body", segments=(40, 20),
                style=H.gill_belly(PAL, k, UNDER, slits=4, start=-0.32, spacing=0.13, width=0.03, side=(0.2, 0.8)))

    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.6 * front), v.y * (1 + 0.15 * front), v.z * (1 - 0.45 * front)))
    b.ellipsoid("Head", G(0, 0.012, 0.33), (0.125, 0.21, 0.048), bone="Head", segments=(40, 20), deform=head_deform,
                style=H.crest(PAL, k, ribs=7, height=0.004))
    for side, sx in SIDES:
        for i, (x, f) in enumerate(((0.042, 0.37), (0.064, 0.315), (0.086, 0.26))):
            b.ellipsoid("Eye", G(x * sx, 0.044 - 0.004 * i, f), (0.017, 0.017, 0.012), bone="Head",
                        style=H.eye(PAL, k, 1.0), segments=(12, 6))
        b.tube("Pincer", pincer_points(sx), [0.03, 0.026, 0.016, 0.002], sides=10, chain=[f"Pincer_{side}"],
               style=H.blade(PAL, k, glow=0.9, teeth=3), flatten=0.45)
        # Glowing jet vents between the dorsal hooks.
        for x, f in ((0.07, 0.08), (0.08, -0.08)):
            b.ellipsoid("Vent", G(x * sx, body_top(f) - 0.006, f), (0.012, 0.04, 0.01), bone="Body",
                        style=H.eye(PAL, k, 0.45), segments=(14, 7))
        # The fin: a membrane over raised rays, one slab per side, carried by the leading and trailing chains.
        names = fin_chain(side, sx, "A")[0] + fin_chain(side, sx, "B")[0]
        # Upper and lower half as two parts: the rim becomes a UV seam, so each half unrolls flat.
        fin_style = H.fin_sheet(PAL, k, fin_uv, UNDER, rays=6, chevrons=2, chevron_width=0.2, flush=FLUSH,
                                ray_height=0.006, dashed=True)
        for name, upper, lower in (("Fin", 1.0, 0.0), ("FinUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: fin_point(sx, u, v, h),
                   lambda u, v, sx=sx, h=lower: fin_point(sx, u, v, h), 40, 16, chain=names, style=fin_style)
        # A muscle at the fin root blends the fin into the body and gives the flat ray some bulk.
        b.ellipsoid("Shoulder", G(0.16 * sx, 0.004, 0.0), (0.1, 0.24, 0.042), bone=f"Fin_{side}_A1", segments=(32, 16),
                    style=H.band_plates(PAL, k, 3, height=0.003, glow=0.25))
        b.parts[-1].chain = [f"Fin_{side}_A1", f"Fin_{side}_B1"]
        tip = fin_point(sx, 1.0, 0.5)
        b.tube("Claw", [tip, tip + G(0.045 * sx, -0.03, -0.045), tip + G(0.05 * sx, -0.085, -0.02)],
               [0.017, 0.011, 0.001], sides=8, chain=[f"Fin_{side}_A3", f"Fin_{side}_B3"], style=H.hook(PAL, k, glow=0.6))
    for f, length in ((0.17, 0.05), (0.03, 0.07), (-0.12, 0.09), (-0.26, 0.1)):
        base = G(0, body_top(f) - 0.012, f)
        b.tube("Hook", [base, base + G(0, length * 0.7, -length * 0.2), base + G(0, length * 0.9, -length * 0.85)],
               [0.018 + length * 0.12, 0.01, 0.001], sides=10, chain=["Body"], style=H.hook(PAL, k, glow=0.8, glow_from=0.78))

    b.tube("Tail", TAIL, [0.05, 0.032, 0.024, 0.018, 0.013, 0.01], sides=12, chain=TAIL_BONES,
           style=H.tube(PAL, k, PAL.top * 0.85, PAL.dark, rings=(0.12, 0.25, 0.38, 0.51, 0.64, 0.77, 0.9), rough=0.6))
    for i in range(1, 5):
        scale = 1 - 0.15 * (i - 1)
        for sx in (-1, 1):
            base = TAIL[i] + G(0.012 * sx, 0.008, 0)
            b.tube("Barb", [base, base + G(0.03 * sx, 0.02, -0.02) * scale, base + G(0.052 * sx, 0.03, -0.06) * scale],
                   [0.012 * scale, 0.007 * scale, 0.001], sides=8, chain=[TAIL_BONES[i]], style=H.hook(PAL, k, glow=0.5))
    end = TAIL[-1]
    b.tube("Prong", [end, end + G(0, 0.004, -0.07), end + G(0, 0.0, -0.15)], [0.022, 0.015, 0.001], sides=10,
           chain=["Tail_5"], style=H.blade(PAL, k, glow=1.0), flatten=0.4)
    for sx in (-1, 1):
        b.tube("Prong", [end, end + G(0.042 * sx, 0.0, -0.04), end + G(0.075 * sx, 0.004, -0.11)], [0.018, 0.012, 0.001],
               sides=10, chain=["Tail_5"], style=H.blade(PAL, k, glow=1.0), flatten=0.4)
    return b


def bones():
    out = [
        # Root sits below the body so its weights never pull on the belly; the armature origin is the body centre.
        ("Root", G(0, -0.5, 0), G(0, -0.5, 0.2), None, True),
        ("Body", G(0, 0, -0.28), G(0, 0, 0.16), "Root", True),
        ("Head", G(0, 0, 0.16), G(0, 0.01, 0.5), "Body", True),
    ]
    for side, sx in SIDES:
        pts = pincer_points(sx)
        out.append((f"Pincer_{side}", pts[0], pts[-1], "Head", True))
        for chain in CHAINS:
            names, pts = fin_chain(side, sx, chain)
            prev = "Body"
            for i, n in enumerate(names):
                out.append((n, pts[i], pts[i + 1], prev, True))
                prev = n
    prev = "Body"
    for i, n in enumerate(TAIL_BONES):
        out.append((n, TAIL[i], TAIL[i + 1], prev, True))
        prev = n
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs_for(loc=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, pincer=0.0, fins=None, sweep=0.0, tail_pitch=None,
              tail_yaw=None):
    """fins: {(side, chain): [three angles]} in radians, up positive (each relative to its parent bone);
    sweep: fins swept back (radians); tail_pitch/tail_yaw: five angles each, up / to the right positive."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": loc if loc is not None else Vector()},
        "Head": {"rot": [(X, head)]},
    }
    for side, sx in SIDES:
        specs[f"Pincer_{side}"] = {"rot": [(Z, sx * pincer)]}
        for chain in CHAINS:
            angles = (fins or {}).get((side, chain), (0.0, 0.0, 0.0))
            names = fin_chain(side, sx, chain)[0]
            for i, n in enumerate(names):
                rot = [(Vector((0, -sx, 0)), angles[i])]
                if i == 0:
                    rot = [(Vector((0, 0, sx)), sweep)] + rot
                specs[n] = {"rot": rot}
    tp = tail_pitch or [0.0] * 5
    ty = tail_yaw or [0.0] * 5
    for i, n in enumerate(TAIL_BONES):
        specs[n] = {"rot": [(Z, -ty[i]), (X, tp[i])]}
    return specs


def wave_fins(w, amp_a, amp_b, span_lag=0.55, chord_lag=0.9, offset=(0.0, 0.0, 0.0), harmonic=1):
    """The travelling wave: root to tip (`span_lag` per segment) and leading to trailing edge (`chord_lag`)."""
    out = {}
    for side, _ in SIDES:
        out[(side, "A")] = [offset[i] + amp_a[i] * math.sin(harmonic * w - i * span_lag) for i in range(3)]
        out[(side, "B")] = [offset[i] + amp_b[i] * math.sin(harmonic * w - i * span_lag - chord_lag) for i in range(3)]
    return out


FLY_FRAMES = 42


def fly_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        fins = wave_fins(w, [d(15), d(12), d(11)], [d(17), d(14), d(13)])
        return specs_for(loc=Vector((0, 0, -0.022 * math.sin(w))), pitch=d(2.5) * math.cos(w), head=-d(2) * math.sin(w),
                         pincer=d(4) * math.sin(2 * w), fins=fins,
                         tail_pitch=[d(5) * math.sin(w - 1.3 - i * 0.75) for i in range(5)],
                         tail_yaw=[d(3) * math.sin(w - i * 0.9) for i in range(5)])
    return FLY_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        fins = wave_fins(w, [d(3), d(4), d(6)], [d(4), d(6), d(8)], span_lag=0.8, harmonic=2)
        return specs_for(loc=Vector((0, 0, 0.016 * math.sin(w))), roll=d(3) * math.sin(w + 0.5), yaw=d(3) * math.sin(w),
                         head=d(2) * math.sin(w + 1), pincer=d(5) + d(8) * max(0.0, math.sin(3 * w)) ** 4, fins=fins,
                         tail_pitch=[d(3) * math.sin(2 * w - i * 0.8) for i in range(5)],
                         tail_yaw=[d(7) * math.sin(w - i * 0.6) for i in range(5)])
    return 60, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        fins = {(s, "A"): [d(22) * k, d(10) * k, d(6) * k] for s, _ in SIDES}
        fins.update({(s, "B"): [d(28) * k, d(14) * k, d(10) * k] for s, _ in SIDES})
        return specs_for(loc=Vector((0, 0.02 * k, 0.03 * k)), pitch=-d(12) * k, roll=d(14) * k, head=-d(6) * k,
                         pincer=d(25) * max(k, 0.0), fins=fins, tail_pitch=[d(9) * k * (1 - 0.1 * i) for i in range(5)],
                         tail_yaw=[-d(8) * k for _ in range(5)])
    return 12, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        s = C.smoothstep
        spin = s(0.04, 1.0, np.array([t]))[0]
        crumple = s(0.0, 0.45, np.array([t]))[0]
        flail = math.exp(-t * 5) * math.sin(t * 38)
        fins = {(sd, "A"): [d(38) * crumple + d(18) * flail, d(26) * crumple, d(24) * crumple] for sd, _ in SIDES}
        fins.update({(sd, "B"): [d(48) * crumple + d(22) * flail, d(32) * crumple, d(30) * crumple] for sd, _ in SIDES})
        # Spirals down: rolls a turn and a quarter (ends belly up), nose dipping; Unity moves it to the ground.
        return specs_for(roll=d(450) * spin, pitch=d(38) * crumple, head=d(14) * crumple,
                         pincer=d(30) * math.sin(math.pi * min(1.0, t * 2.5)), fins=fins, sweep=d(18) * crumple,
                         tail_pitch=[-d(16) * crumple + d(10) * flail for _ in range(5)],
                         tail_yaw=[d(10) * crumple * math.sin(i * 1.3) for i in range(5)])
    return 40, spec, False


DIVE_FRAMES = 24


def dive_spec(poser):
    d = math.radians

    def spec(t, f):
        s = lambda a, b: C.smoothstep(a, b, np.array([t]))[0]  # noqa: E731
        dive = s(0.0, 0.3) * (1 - s(0.6, 1.0))
        cock = s(0.0, 0.3) * (1 - s(0.32, 0.45))
        strike = s(0.32, 0.5) * (1 - s(0.62, 0.95))
        fins = {(sd, "A"): [-d(14) * dive, -d(6) * dive, d(10) * dive] for sd, _ in SIDES}
        fins.update({(sd, "B"): [-d(10) * dive, -d(4) * dive, d(14) * dive] for sd, _ in SIDES})
        return specs_for(loc=Vector((0, -0.12 * dive, -0.1 * dive)), pitch=d(30) * dive - d(12) * strike,
                         head=-d(8) * dive, pincer=d(32) * strike, fins=fins, sweep=d(26) * dive,
                         tail_pitch=[d(13) * cock - d(15) * strike for _ in range(5)],
                         tail_yaw=[d(4) * strike * (i - 2) for i in range(5)])
    return DIVE_FRAMES, spec, False


CLIPS = {"Fly": fly_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Dive": dive_spec}
# LOD1: the trailing chains fold into the leading ones (the fins still wave root to tip, without the chord lag),
# pincers into the head, the tail into three bones. 12 bones.
LOD_DROP = {"Pincer_L": "Head", "Pincer_R": "Head", "Tail_2": "Tail_1", "Tail_4": "Tail_3",
            **{f"Fin_{s}_B{i}": f"Fin_{s}_A{i}" for s, _ in SIDES for i in (1, 2, 3)}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_2"), "Tail_3": ("Tail_3", "Tail_4")}


def old_proxy():
    """EnemyModelFactory.Skyray in primitives (factory units x 1.4, like the new model), flat colours."""
    f = 1.4

    def P(x, up, fwd):
        return G(x * f, up * f, fwd * f)
    body, dark, glow, eyes = (0.25, 0.35, 0.65), (0.1, 0.12, 0.25), (0.4, 0.95, 1.0), (0.5, 1.0, 1.0)
    groups = {c: C.PartBuilder() for c in (body, dark, glow, eyes)}
    groups[body].ellipsoid("Body", P(0, 0, 0), (0.14 * f, 0.28 * f, 0.06 * f))
    groups[dark].ellipsoid("Saddle", P(0, 0.03, 0.05), (0.1 * f, 0.18 * f, 0.04 * f))
    for sx in (-1, 1):
        groups[eyes].ellipsoid("Eye", P(0.05 * sx, 0.05, 0.16), (0.034 * f, 0.03 * f, 0.028 * f))
        groups[body].ellipsoid("Fin", P(0.4 * sx, 0, -0.04), (0.3 * f, 0.2 * f, 0.014 * f))
        groups[glow].tube("Edge", [P(0.12 * sx, 0.006, 0.2), P(0.45 * sx, 0.006, 0.12), P(0.7 * sx, 0.004, -0.1)],
                          [0.012 * f, 0.009 * f, 0.003 * f])
        groups[dark].tube("Horn", [P(0.07 * sx, 0.01, 0.22), P(0.09 * sx, 0.03, 0.32), P(0.07 * sx, 0.06, 0.38)],
                          [0.022 * f, 0.016 * f, 0.002])
    groups[glow].tube("Ridge", [P(0, 0.06, 0.2), P(0, 0.07, -0.2)], [0.012 * f, 0.008 * f])
    groups[body].tube("Tail", [P(0, 0, -0.26), P(0, 0, -0.6), P(0, 0, -0.96)], [0.03 * f, 0.02 * f, 0.004])
    return C.flat_proxy(groups, "OldSkyray")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.6)
    C.ground()
    lift = 0.8
    arm.location.z = lift
    target = Vector((0, -0.25, lift))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    lod_arm.location.z = lift
    paths = []
    close = C.closeup_camera(target, 3.6, azimuth=-38, elevation=30, lens=55)
    C.set_action(arm, actions["Idle"], 15)
    paths.append(C.render(close, os.path.join(out, "skyray_close.png"), 1280, 720))
    top = C.closeup_camera(target, 3.8, azimuth=-12, elevation=62, lens=55)
    C.set_action(arm, actions["Fly"], 8)
    paths.append(C.render(top, os.path.join(out, "skyray_top.png"), 1280, 720))
    front = C.closeup_camera(target + Vector((0, 0.2, 0)), 3.4, azimuth=-4, elevation=6, lens=55)
    C.set_action(arm, actions["Fly"], 0)
    paths.append(C.render(front, os.path.join(out, "skyray_front.png"), 1280, 720))
    arm.location.z = 2.2
    under = C.closeup_camera(target + Vector((0, 0, 1.4)), 3.4, azimuth=-30, elevation=-18, lens=55)
    C.set_action(arm, actions["Fly"], 30)
    paths.append(C.render(under, os.path.join(out, "skyray_under.png"), 1280, 720))
    arm.location.z = lift
    side = C.closeup_camera(target, 3.8, azimuth=-90, elevation=10, lens=55)
    C.set_action(arm, actions["Dive"], 10)
    paths.append(C.render(side, os.path.join(out, "skyray_dive.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 22)
    paths.append(C.render(close, os.path.join(out, "skyray_death.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Fly"], front, out, "skyray_fly", range(0, FLY_FRAMES, 6)[:8]),
              C.strip_sheet(arm, actions["Fly"], close, out, "skyray_fly34", range(0, FLY_FRAMES, 6)[:8]),
              C.strip_sheet(arm, actions["Dive"], side, out, "skyray_divestrip", range(0, DIVE_FRAMES, 3)[:8])]

    C.set_action(lod_arm, lod_actions["Fly"], 8)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "skyray_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 15)
    macros = [("skyray_macro_fin.png", G(-0.55, 0.0, -0.05) + Vector((0, 0, lift)), 0.6, -40, 40),
              ("skyray_macro_head.png", G(0.0, 0.03, 0.42) + Vector((0, 0, lift)), 0.55, -30, 28),
              ("skyray_macro_tail.png", None, 0.7, -60, 30)]
    for file_name, aim, dist, az, el in macros:
        if aim is None:
            aim = arm.matrix_world @ arm.pose.bones["Tail_5"].tail
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    fly = actions["Fly"]
    h = AIR_HEIGHT
    layout = [(fly, 0, (-1.6, 0.9, h), 0.25), (fly, 9, (0.2, 1.4, h + 0.15), -0.1), (fly, 20, (1.7, 0.6, h - 0.1), 0.3),
              (fly, 31, (-0.7, -0.5, h + 0.1), -0.2), (actions["Dive"], 12, (1.0, -0.8, h - 0.2), 0.05)]
    cam_target = (0, 0.3, 0.9)
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "skyray_game.png"), GAME_SCALE, layout, close_lights=rig,
                                   target=cam_target))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "skyray_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig, target=cam_target))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    for o in old:
        o.location.z = lift
    olds = [C.render(close, os.path.join(out, "skyray_old_close.png"), 1280, 720)]
    for o in old:
        o.location.z = 0
    olds.append(C.game_view_static(old, os.path.join(out, "skyray_old_game_zoomed.png"), GAME_SCALE, zoom=2.5,
                                   close_lights=rig, target=(0, 0.3, 0.9), lift=h))
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Fly"], 8)
    single = C.game_view(arm, mesh, os.path.join(out, "skyray_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig, target=(0, 0.3, 0.9), lift=h)
    arm.location.z = lift
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare5_skyray.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_skyray.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "skyray_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0036, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.1,
                               extrusion=0.02, ray=0.05, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.6, dust=0.3, markings=PAL.dark, marking_amount=0.7,
                                                  seed=71),
                               specular=0.3, mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Fly"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "fly_beat_seconds": round(FLY_FRAMES / 30, 2), "pivot": "body centre (flight height)",
                          "dive_strike_frame": 10,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)


if __name__ == "__main__":
    main()
