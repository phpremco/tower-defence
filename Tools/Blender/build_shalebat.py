"""Shalebat: the Stone-element bat that spits bolts at towers (round 5, see ArtSource/DESIGN_NOTES.md).

A small geode body (a split stone shell with ember Crysium growing out of the crack) under wide wings whose
"fingers" are three long flat shale blades fanning from a boulder wrist, with a thin dark stone skin between them,
crazed with glowing amber veins and scalloped between the blade tips. No ears and no face: a blunt stacked-slab
wedge with a spike for a nose, three amber eyes on each side, two stone horns swept back and a lower jaw that drops
to show the glowing throat. Ember Crysium thumb claws at the wrists, hooked talons under the belly, a short spiked
tail with a slate blade.

Clips: Fly (flap loop), Glide (loop), Hit, Death. Also exports Shalebat_LOD1.

Run: blender --background --python Tools/Blender/build_shalebat.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import stone as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Shalebat"
LOD_NAME = "Shalebat_LOD1"
K = 1.0
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("rock", "membrane", "crystal")
GAME_SCALE = 0.65
AIR_HEIGHT = 1.6

# Species colours from ProjectGenerator.Bestiary (CreateBat, Stone): slate (0.38, 0.4, 0.46), amber eyes
# (1, 0.8, 0.4), orange accent (0.95, 0.6, 0.25). Lighter slate on top so the wings read from above.
PAL = H.Palette(top=(0.5, 0.51, 0.56), side=(0.24, 0.25, 0.29), belly=(0.07, 0.07, 0.085), seam=(0.04, 0.035, 0.035),
                dark=(0.12, 0.12, 0.14), glow=(1.0, 0.55, 0.18), eye=(1.0, 0.62, 0.2), bone=(0.78, 0.76, 0.72))
BLADE = H.Palette(top=(0.58, 0.58, 0.62), side=(0.3, 0.3, 0.34), belly=(0.08, 0.08, 0.09), seam=(0.04, 0.035, 0.035),
                  dark=(0.14, 0.14, 0.16), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

# Wing (right side, game units): shoulder, wrist, blade tips, and where the skin meets the flank.
SHOULDER = (0.09, 0.05, 0.03)
WRIST = (0.4, 0.1, 0.09)
TIPS = ((0.8, 0.07, 0.0), (0.74, 0.03, -0.2), (0.5, 0.0, -0.33))
FLANK = (0.08, 0.0, -0.15)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def wing_edge(sx, v):
    """The outer edge of the skin, v 0..1: blade tip 1 -> tip 2 -> tip 3 -> flank, scalloped between the tips."""
    pts = [gp(t, sx) for t in TIPS] + [gp(FLANK, sx)]
    seg = min(int(v * 3), 2)
    f = v * 3 - seg
    a, b = pts[seg], pts[seg + 1]
    p = a.lerp(b, f)
    hub = gp(WRIST, sx).lerp(gp(SHOULDER, sx), 0.5)
    dip = 0.2 if seg < 2 else 0.14
    return p.lerp(hub, dip * math.sin(math.pi * f))


def wing_point(sx, u, v, side=0.0):
    """Skin point: u 0 on the arm (wrist at v 0 .. shoulder at v 1) to 1 on the outer edge."""
    inner = gp(WRIST, sx).lerp(gp(SHOULDER, sx), v)
    p = inner.lerp(wing_edge(sx, v), u)
    # The skin billows a little between the blades and is thicker near the arm.
    p = p + Vector((0, 0, 0.018 * math.sin(math.pi * u) * math.sin(math.pi * v)))
    half = 0.0055 + 0.006 * (1 - u)
    return p + Vector((0, 0, side * half))


def head_rot(pitch):
    return Matrix.Rotation(math.radians(pitch), 3, "X")


def slab(b, name, centre, radii, r, bone, seed, count=16, depth=0.2, rim=-0.4, veins=0.5, fault=0.8, pal=PAL):
    b.ellipsoid(name, centre, radii, r, bone=bone, segments=(32, 16),
                deform=C.chiselled(count, seed, depth, squash=(0.6, 1.0)),
                style=S.slab(pal, K, seed=seed, rim=rim, veins=veins, fault=fault, strata=40.0))


TAIL = [G(0, 0.0, -0.17), G(0, -0.01, -0.3), G(0, -0.025, -0.43)]


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Core", G(0, 0.02, 0.0), (0.12, 0.19, 0.11), bone="Body", style=S.core(PAL, k, seed=4, cells=30),
                segments=(32, 16))
    # The geode shell: two plates either side of a glowing crack down the back, a belly plate under it.
    for sx in (-1, 1):
        slab(b, "Shell", G(0.065 * sx, 0.075, -0.01), (0.085, 0.19, 0.075), Matrix.Rotation(math.radians(28 * sx), 3, "Y"),
             "Body", 21 + (sx > 0))
    slab(b, "Belly", G(0, -0.045, 0.0), (0.1, 0.16, 0.06), None, "Body", 25, rim=0.3, veins=0.2, fault=0.3)
    for i, (base, direction, length) in enumerate(((G(0, 0.1, 0.03), G(0, 1.0, -0.35), 0.15),
                                                   (G(0.03, 0.09, -0.06), G(0.45, 1.0, -0.6), 0.11),
                                                   (G(-0.03, 0.09, -0.06), G(-0.45, 1.0, -0.6), 0.11))):
        b.shard("Geode", base, direction, 0.026 if i == 0 else 0.02, length, sides=8, twist=2.0 * (1 if i % 2 else -1),
                flute=0.4, bone="Body", style=Y.shard_style("ember", 0.85, seed=50 + i))

    slab(b, "Head", G(0, 0.04, 0.19), (0.085, 0.11, 0.07), head_rot(-12), "Head", 31, count=14, rim=-0.6, veins=0.3)
    b.tube("Nose", [G(0, 0.04, 0.27), G(0, 0.03, 0.33), G(0, 0.015, 0.39)], [0.035, 0.02, 0.002], sides=8,
           chain=["Head"], style=S.spike(PAL, k, seed=3))
    slab(b, "Jaw", G(0, -0.015, 0.21), (0.062, 0.09, 0.03), head_rot(8), "Jaw", 33, count=12, rim=0.3, veins=0.0, fault=0.0)
    b.ellipsoid("Throat", G(0, 0.006, 0.23), (0.032, 0.045, 0.009), bone="Head", style=H.inner_glow(PAL, 0.8))
    # A heavy brow slab over the eyes: the face reads as a scowl, not a pebble.
    slab(b, "Brow", G(0, 0.095, 0.21), (0.09, 0.07, 0.03), head_rot(-25), "Head", 35, count=10, rim=-0.5, veins=0.0,
         fault=0.3)
    for side, sx in SIDES:
        for i, (x, up, f) in enumerate(((0.068, 0.07, 0.24), (0.074, 0.055, 0.205), (0.072, 0.075, 0.175))):
            b.ellipsoid("Eye", G(x * sx, up, f), (0.013, 0.013, 0.011), bone="Head", style=H.eye(PAL, k, 0.7),
                        segments=(12, 6))
        root = G(0.045 * sx, 0.09, 0.16)
        b.tube("Horn", [root, root + G(0.035 * sx, 0.07, -0.07), root + G(0.075 * sx, 0.1, -0.2)], [0.028, 0.016, 0.002],
               sides=8, chain=["Head"], style=S.spike(PAL, k, seed=6))
        root = G(0.05 * sx, -0.07, -0.02)
        b.tube("Talon", [root, root + G(0.005 * sx, -0.05, 0.01), root + G(0.0, -0.065, 0.05)], [0.02, 0.013, 0.002],
               sides=8, chain=["Body"], style=S.spike(PAL, k, seed=8))

        # The wing: an arm of basalt drums, a boulder wrist, three shale blades, the stone skin between them.
        arm, hand = f"Arm_{side}", f"Hand_{side}"
        shoulder, wrist = gp(SHOULDER, sx), gp(WRIST, sx)
        b.tube("Arm", [shoulder, shoulder.lerp(wrist, 0.5) + Vector((0, 0, 0.012)), wrist], [0.045, 0.038, 0.032],
               sides=8, chain=[arm], style=S.pillar(BLADE, k, drums=2, seed=40, glow=0.5), cap=0.5)
        slab(b, "Wrist", wrist, (0.042, 0.042, 0.036), None, arm, 41 + (sx > 0), count=12, rim=-0.7, veins=0.0,
             fault=0.4, pal=BLADE)
        b.parts[-1].chain = [arm, hand]
        for i, tip in enumerate(TIPS):
            t = gp(tip, sx)
            mid = wrist.lerp(t, 0.5) + Vector((0, 0, 0.012))
            b.tube("Blade", [wrist, mid, t], [0.03, 0.024, 0.003], sides=8, chain=[f"Finger_{side}{i + 1}"],
                   style=S.pillar(BLADE, k, drums=3, seed=44 + i, glow=0.7), flatten=0.35, cap=0.4)
        b.shard("Claw", wrist + G(0.0, 0.02, 0.02), G(0.15 * sx, 0.55, 1.0), 0.02, 0.1, sides=8, twist=1.5, flute=0.35,
                bone=hand, style=Y.shard_style("ember", 0.9, seed=60 + (sx > 0)),
                bend=G(0, -0.03, 0.0))
        fingers = [f"Finger_{side}{i + 1}" for i in range(3)]
        skin = S.skin(PAL, k, seed=70 + (sx > 0), cells=22, width=0.035, glow=0.75)
        for name, upper, lower in (("Skin", 1.0, 0.0), ("SkinUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: wing_point(sx, u, v, h),
                   lambda u, v, sx=sx, h=lower: wing_point(sx, u, v, h), 24, 30, chain=[arm, hand] + fingers, style=skin)

    b.tube("Tail", TAIL, [0.04, 0.026, 0.016], sides=8, chain=["Tail_1", "Tail_2"],
           style=S.pillar(PAL, k, drums=3, seed=12, glow=0.6))
    end = TAIL[-1]
    b.tube("TailBlade", [end, end + G(0, 0.0, -0.05), end + G(0, -0.005, -0.11)], [0.032, 0.022, 0.002], sides=8,
           chain=["Tail_2"], style=S.spike(PAL, k, glow=0.6, seed=13), flatten=0.3)
    return b


def bones():
    out = [
        ("Root", G(0, -0.5, 0), G(0, -0.5, 0.2), None, True),
        ("Body", G(0, 0.0, -0.15), G(0, 0.02, 0.12), "Root", True),
        ("Head", G(0, 0.03, 0.12), G(0, 0.03, 0.33), "Body", True),
        ("Jaw", G(0, 0.0, 0.14), G(0, -0.02, 0.29), "Head", True),
        ("Tail_1", TAIL[0], TAIL[1], "Body", True),
        ("Tail_2", TAIL[1], TAIL[2], "Tail_1", True),
    ]
    for side, sx in SIDES:
        shoulder, wrist = gp(SHOULDER, sx), gp(WRIST, sx)
        out.append((f"Arm_{side}", shoulder, wrist, "Body", True))
        out.append((f"Hand_{side}", wrist, wrist + G(0.05 * sx, 0.0, -0.03), f"Arm_{side}", True))
        for i, tip in enumerate(TIPS):
            out.append((f"Finger_{side}{i + 1}", wrist, gp(tip, sx), f"Hand_{side}", True))
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs_for(loc=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, jaw=0.0, arm=0.0, sweep=0.0, hand=0.0, fold=0.0,
              finger_lift=0.0, tail=(0.0, 0.0)):
    """arm: wing up (radians), sweep: arm swept back, hand: wrist bend up, fold: blades folded back towards the
    body (the first blade least), finger_lift: blades bent up; tail: (pitch, yaw)."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": loc if loc is not None else Vector()},
        "Head": {"rot": [(X, head)]},
        "Jaw": {"rot": [(X, jaw)]},
        "Tail_1": {"rot": [(Z, -tail[1]), (X, tail[0])]},
        "Tail_2": {"rot": [(Z, -tail[1] * 1.3), (X, tail[0] * 1.2)]},
    }
    for side, sx in SIDES:
        flap = Vector((0, -sx, 0))
        back = Vector((0, 0, sx))
        specs[f"Arm_{side}"] = {"rot": [(back, sweep), (flap, arm)]}
        specs[f"Hand_{side}"] = {"rot": [(flap, hand)]}
        for i in range(3):
            specs[f"Finger_{side}{i + 1}"] = {"rot": [(back, fold * (0.5 + 0.35 * i)), (flap, finger_lift * (1 - 0.2 * i))]}
    return specs


FLY_FRAMES = 16


def fly_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Up is sin(w) > 0; on the upstroke (cos > 0) the blades fold back and the wrist trails.
        up = math.cos(w)
        return specs_for(loc=Vector((0, 0, -0.025 * math.cos(w))), pitch=d(3) * math.sin(w), head=-d(4) * math.sin(w),
                         jaw=d(3), arm=d(8) + d(40) * math.sin(w), sweep=d(10) * up, hand=d(22) * math.sin(w - 0.9),
                         fold=d(28) * max(0.0, up), finger_lift=d(10) * math.sin(w - 1.5),
                         tail=(d(8) * math.sin(w - 1.2), d(3) * math.sin(w)))
    return FLY_FRAMES, spec, True


def glide_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        return specs_for(loc=Vector((0, 0, 0.012 * math.sin(w))), roll=d(5) * math.sin(w), yaw=d(3) * math.sin(w + 1),
                         pitch=d(2) * math.sin(2 * w), head=d(3) * math.sin(w + 0.5), jaw=d(3),
                         arm=d(10) + d(3) * math.sin(2 * w), hand=-d(4) + d(2) * math.sin(2 * w + 1), fold=-d(4),
                         finger_lift=d(2) * math.sin(2 * w + 2), tail=(d(4) * math.sin(w), d(10) * math.sin(w + 0.6)))
    return 48, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        return specs_for(loc=Vector((0, 0.02 * k, 0.03 * k)), pitch=-d(14) * k, roll=d(10) * k, head=-d(12) * k,
                         jaw=d(3) + d(25) * max(k, 0.0), arm=d(8) + d(35) * k, sweep=-d(8) * k, hand=d(15) * k,
                         fold=d(15) * k, tail=(d(15) * k, -d(10) * k))
    return 12, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        s = C.smoothstep
        spin = s(0.05, 1.0, np.array([t]))[0]
        fold = s(0.0, 0.5, np.array([t]))[0]
        flail = math.exp(-t * 5) * math.sin(t * 40)
        # The wings fold up like a closing book and it tumbles out of the air; Unity moves it to the ground.
        return specs_for(roll=d(300) * spin, pitch=d(45) * fold, head=d(20) * fold, jaw=d(30) * fold,
                         arm=d(55) * fold + d(25) * flail, sweep=d(40) * fold, hand=d(30) * fold, fold=d(55) * fold,
                         finger_lift=d(15) * fold, tail=(-d(25) * fold, d(15) * fold))
    return 36, spec, False


CLIPS = {"Fly": fly_spec, "Glide": glide_spec, "Hit": hit_spec, "Death": death_spec}
# LOD1: jaw into the head, one tail bone, the second and third blade fold into the hand. 10 bones.
LOD_DROP = {"Jaw": "Head", "Tail_2": "Tail_1",
            **{f"Finger_{s}{i}": f"Hand_{s}" for s, _ in SIDES for i in (2, 3)}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_2")}


def old_proxy():
    """EnemyModelFactory.Bat in primitives (factory units x 1.4), flat colours: furred body, ears, membrane wings."""
    f = 1.4

    def P(x, up, fwd):
        return G(x * f, up * f, fwd * f)
    body, dark, accent, eyes = (0.38, 0.4, 0.46), (0.14, 0.14, 0.16), (0.95, 0.6, 0.25), (1.0, 0.8, 0.4)
    groups = {c: C.PartBuilder() for c in (body, dark, accent, eyes)}
    groups[body].ellipsoid("Body", P(0, 0, 0), (0.1 * f, 0.15 * f, 0.1 * f))
    groups[body].ellipsoid("Head", P(0, 0.03, 0.16), (0.075 * f, 0.08 * f, 0.07 * f))
    for sx in (-1, 1):
        groups[eyes].ellipsoid("Eye", P(0.045 * sx, 0.06, 0.21), (0.022 * f,) * 3)
        groups[dark].tube("Ear", [P(0.05 * sx, 0.09, 0.16), P(0.09 * sx, 0.22, 0.13)], [0.025 * f, 0.003])
        groups[dark].ellipsoid("Wing", P(0.34 * sx, 0.04, 0.0), (0.28 * f, 0.13 * f, 0.008 * f))
        groups[body].tube("Arm", [P(0.06 * sx, 0.04, 0.03), P(0.26 * sx, 0.05, 0.05), P(0.56 * sx, 0.05, 0.0)],
                          [0.016 * f, 0.012 * f, 0.004])
        for i in range(3):
            groups[accent].tube("Finger", [P(0.26 * sx, 0.05, 0.05), P((0.36 + i * 0.07) * sx, 0.05, -0.1 * (i + 1))],
                                [0.009 * f, 0.003])
    groups[accent].ellipsoid("Crack", P(0, 0.06, 0.0), (0.05 * f, 0.1 * f, 0.03 * f))
    return C.flat_proxy(groups, "OldBat")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.3)
    C.ground()
    lift = 0.7
    arm.location.z = lift
    target = Vector((0, -0.05, lift))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    lod_arm.location.z = lift
    paths = []
    close = C.closeup_camera(target, 2.8, azimuth=-38, elevation=30, lens=55)
    C.set_action(arm, actions["Glide"], 10)
    paths.append(C.render(close, os.path.join(out, "shalebat_close.png"), 1280, 720))
    top = C.closeup_camera(target, 3.0, azimuth=-12, elevation=62, lens=55)
    C.set_action(arm, actions["Fly"], 4)
    paths.append(C.render(top, os.path.join(out, "shalebat_top.png"), 1280, 720))
    front = C.closeup_camera(target, 2.6, azimuth=-4, elevation=6, lens=55)
    C.set_action(arm, actions["Fly"], 0)
    paths.append(C.render(front, os.path.join(out, "shalebat_front.png"), 1280, 720))
    face = C.closeup_camera(target + G(0, 0.03, 0.22), 0.9, azimuth=-30, elevation=12, lens=55)
    C.set_action(arm, actions["Hit"], 4)
    paths.append(C.render(face, os.path.join(out, "shalebat_face.png"), 1280, 720))
    arm.location.z = 2.0
    under = C.closeup_camera(target + Vector((0, 0, 1.3)), 2.6, azimuth=-30, elevation=-18, lens=55)
    C.set_action(arm, actions["Glide"], 20)
    paths.append(C.render(under, os.path.join(out, "shalebat_under.png"), 1280, 720))
    arm.location.z = lift
    C.set_action(arm, actions["Death"], 18)
    paths.append(C.render(close, os.path.join(out, "shalebat_death.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Fly"], front, out, "shalebat_fly", range(0, FLY_FRAMES, 2)),
              C.strip_sheet(arm, actions["Fly"], close, out, "shalebat_fly34", range(0, FLY_FRAMES, 2))]

    C.set_action(lod_arm, lod_actions["Fly"], 4)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "shalebat_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Glide"], 10)
    macros = [("shalebat_macro_wing.png", G(-0.5, 0.06, -0.08) + Vector((0, 0, lift)), 0.5, -40, 45),
              ("shalebat_macro_geode.png", G(0.0, 0.12, -0.02) + Vector((0, 0, lift)), 0.45, -40, 35),
              ("shalebat_macro_wrist.png", G(0.4, 0.1, 0.1) + Vector((0, 0, lift)), 0.4, 30, 25)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    fly, glide = actions["Fly"], actions["Glide"]
    h = AIR_HEIGHT
    layout = [(fly, 0, (-1.4, 0.9, h), 0.25), (fly, 5, (0.0, 1.3, h + 0.15), -0.1), (glide, 20, (1.4, 0.6, h - 0.1), 0.3),
              (fly, 11, (-0.6, -0.3, h + 0.1), -0.2), (fly, 8, (0.8, -0.7, h - 0.2), 0.05), (glide, 3, (-1.7, -0.6, h), 0.4)]
    cam_target = (0, 0.3, 0.9)
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "shalebat_game.png"), GAME_SCALE, layout, close_lights=rig,
                                   target=cam_target))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "shalebat_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig, target=cam_target))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    for o in old:
        o.location.z = lift
    olds = [C.render(close, os.path.join(out, "shalebat_old_close.png"), 1280, 720)]
    for o in old:
        o.location.z = 0
    olds.append(C.game_view_static(old, os.path.join(out, "shalebat_old_game_zoomed.png"), GAME_SCALE, zoom=2.5,
                                   close_lights=rig, target=cam_target, lift=h))
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Glide"], 10)
    single = C.game_view(arm, mesh, os.path.join(out, "shalebat_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig, target=cam_target, lift=h)
    arm.location.z = lift
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare5_shalebat.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_shalebat.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "shalebat_uv.png"), background=asset.textures["albedo"])


# Stone parts move as one piece and meet their neighbours in a narrow soft crease.
RIGID = {"Shell", "Belly", "Head", "Brow", "Jaw", "Geode", "Nose", "Horn", "Talon", "Claw", "Wrist"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.003, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.08,
                               extrusion=0.016, ray=0.04, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.3, dust=0.5, seed=81), specular=0.25,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Fly"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "flap_seconds": round(FLY_FRAMES / 30, 2), "pivot": "body centre (flight height)",
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)


if __name__ == "__main__":
    main()
