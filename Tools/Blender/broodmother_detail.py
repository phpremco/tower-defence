"""Round 45: the BroodMother's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

- BroodMother_Face: under the hood rim a cluster of three wet eyes per side (lime-yellow irises with four-lobed
  pupils, dark lids), serrated teeth on the inner faces of the four hooked jaws, a lamprey ring of small teeth round
  the glowing throat, and a pair of segmented mouth palps.
- BroodMother_Claws: two hooked keratin talons on every foot, saw teeth down the inner edge of both sickles and a
  glowing hooked point on each.
Both have LOD1 versions in BroodMother_LOD1.fbx (teeth and saws left out there). No bone, clip or event changes.

Run after build_broodmother.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/broodmother_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "BroodMother"
PARTS = ("Face", "Claws")
# Budget: body 14506 / LOD1 6000 tris -> at most +2900 / +900 for both parts.
FACE_TRIS, CLAW_TRIS = 1850, 1000
FACE_LOD, CLAW_LOD = 560, 320
VOXEL = 0.0024
M = 1.75
SIDES = (("L", -1), ("R", 1))
JAWS = ("UL", "UR", "LL", "LR")

EYE = D.Eye(lens=(0.02, 0.014, 0.005), iris=(0.86, 1.0, 0.32), rim=(0.12, 0.1, 0.02), pupil=(0.004, 0.004, 0.0),
            glow=(0.72, 1.0, 0.2), vein=(0.2, 0.08, 0.02))
SOCKET = D.Flesh(dark=(0.03, 0.015, 0.01), flesh=(0.32, 0.12, 0.035))
TOOTH = D.Keratin(root=(0.25, 0.1, 0.03), body=(0.82, 0.68, 0.45), tip=(0.96, 0.92, 0.8), glow=(0.68, 1.0, 0.18),
                  stain=(0.2, 0.12, 0.02))
CLAW = D.Keratin(root=(0.04, 0.02, 0.012), body=(0.26, 0.1, 0.035), tip=(0.95, 0.85, 0.6), glow=(0.68, 1.0, 0.18),
                 stain=(0.08, 0.04, 0.01))
GUM = D.Flesh(dark=(0.025, 0.01, 0.006), flesh=(0.2, 0.05, 0.03), glow=(0.68, 1.0, 0.18))

HEAD_C = Vector((0.0, -0.53, 0.3))
FACE_FOCUS = Vector((0.0, -0.78, 0.3))
FOOT_FOCUS = Vector((-0.76, -0.53, 0.06))
BLADE_FOCUS = Vector((-0.3, -1.0, 0.45))


def P(x, up, fwd):
    return G(x * M, up * M, fwd * M)


def jaw_points(name):
    sx = -1 if name[1] == "L" else 1
    if name[0] == "U":
        return [P(0.04 * sx, 0.165, 0.37), P(0.1 * sx, 0.195, 0.46), P(0.085 * sx, 0.185, 0.55), P(0.022 * sx, 0.16, 0.6)]
    return [P(0.04 * sx, 0.11, 0.37), P(0.095 * sx, 0.08, 0.45), P(0.076 * sx, 0.085, 0.53), P(0.02 * sx, 0.115, 0.585)]


def blade_points(sx):
    elbow = P(0.215 * sx, 0.42, 0.22)
    return [elbow, P(0.215 * sx, 0.35, 0.44), P(0.155 * sx, 0.215, 0.66), P(0.085 * sx, 0.135, 0.8)]


THROAT = P(0, 0.135, 0.4)
MOUTH_TIP = P(0, 0.14, 0.6)


def direction(az, el):
    """Outward from the head centre: azimuth from the front (degrees, + to the right), elevation above level."""
    a, e = math.radians(az), math.radians(el)
    return Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=2)
    eyes = [D.eye_style(EYE, pupil="bar", iris_size=0.46, pupil_size=0.26, glow=0.8, seed=s) for s in range(3)]
    for side, sx in SIDES:
        # A cluster of three on the front of the hood either side of the keel (what the play camera sees) ...
        for k, (x, y, r) in enumerate(((0.075, -0.6, 0.036), (0.135, -0.56, 0.027), (0.105, -0.5, 0.022))):
            p, n = boss.onto(Vector((x * sx, y, 1.5)), Vector((0, 0, -1)))
            n = (n + Vector((0, -0.35, 0))).normalized()
            D.add_eye(b, p - n * r * 0.5, n, r, eyes[k], sock, squash=(1.0, 1.0, 0.8), socket=0.2,
                      up=Vector((0, -1, 0)) - n * n.y * -1, bone="Head")
        # ... and one big wet eye each side of the face under the hood rim, over the mouth (the front view).
        p, n = boss.out(HEAD_C, direction(26 * sx, 12))
        D.add_eye(b, p - n * 0.02, n, 0.042, eyes[0], sock, squash=(1.0, 1.1, 0.85), socket=0.2,
                  up=Vector((0, 0, 1)) - n * n.z, bone="Head")
    tooth = [D.tooth_style(TOOTH, glow=0.4, seed=s) for s in range(3)]
    for name in JAWS:
        sx = -1 if name[1] == "L" else 1
        crv = D.curve_points(jaw_points(name))
        rows = [(0.25, 0.012, 0.05, 0.0), (0.4, 0.013, 0.055, 0.0), (0.55, 0.011, 0.045, 0.0), (0.68, 0.009, 0.035, 0.0)]
        D.inner_teeth(boss, b, crv, THROAT, MOUTH_TIP, rows, tooth[len(name) % 3], f"Jaw_{name}", max_off=0.1,
                      rake=0.3)
    axis = (MOUTH_TIP - THROAT).normalized()
    gum_c = THROAT + axis * 0.05
    D._tag(b.ring("Gum", gum_c, axis, 0.06, 0.009, arcs=3, sides=8,
                  style=D.flesh_style(GUM, glow=0.5, glow_axis=(gum_c, -axis, 0.04), seed=4), ref=Vector((1, 0, 0)),
                  profile=lambda th: 1.0 + 0.3 * math.cos(6 * th)), gum_c, bone="Head")
    D.add_ring_teeth(b, gum_c + axis * 0.006, -axis, 0.058, 12, 0.06, 0.012, tooth[1], inward=0.6, bone="Head",
                     lod=False, name="RingTooth")
    # Mouth palps: two segmented feelers under the lower jaws, keratin-tipped.
    palp = D.keratin_style(CLAW, glow=0.4, glow_from=0.85, seed=9)
    for side, sx in SIDES:
        root = P(0.05 * sx, 0.07, 0.36)
        pts = [root, root + Vector((0.03 * sx, -0.07, -0.04)), root + Vector((0.045 * sx, -0.15, -0.1)),
               root + Vector((0.035 * sx, -0.21, -0.15))]
        D.add_palp(b, pts, [0.016, 0.012, 0.009, 0.002], palp, bone="Head", lod=False)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.6, glow_from=0.85, seed=s, wet=0.3) for s in range(3)]
    saw = D.tooth_style(TOOTH, glow=0.3, seed=12)
    for side, sx in SIDES:
        for i in (1, 2):
            bone = f"Leg_{side}{i}_Tibia"
            knee, foot = boss.head(bone), boss.tail(bone)
            down = (foot - knee).normalized()
            base = foot - down * 0.1
            out = Vector((sx, 0, 0))
            for k, f in enumerate((-1.0, 1.0)):
                d = (down * 0.35 + Vector((0, f, 0)) * 0.7 + out * 0.45 + Vector((0, 0, 0.15))).normalized()
                D.add_claw(b, base, d, 0.017, 0.1 - 0.015 * k, claw[k], hook=Vector((0, 0, -1)), hook_amount=0.6,
                           bone=bone)
        crv = D.curve_points(blade_points(sx), 60)
        bone = f"Blade_{side}_2"
        D.edge_saw(boss, b, crv, (0.24, 0.3, 0.36, 0.42, 0.48, 0.54, 0.6), Vector((-sx, 0, 0)), 0.014, 0.06, saw, bone,
                   max_off=0.15)
        end = crv[-4]
        tangent = (crv[-1] - crv[-6]).normalized()
        D.add_claw(b, end - tangent * 0.03, tangent, 0.022, 0.1, claw[0], hook=Vector((-sx, 0.3, -0.6)),
                   hook_amount=0.5, bone=bone, name="BladeTip")
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.5, "RingTooth": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Saw": 1.3, "BladeTip": 1.3})}
    return D.build_all(boss, specs, VOXEL, "round 45: eye clusters, jaw teeth, throat ring, palps, talon feet, "
                                           "sickle saw teeth and tips; bones, clips and events unchanged")


def qa(boss, tag):
    D.qa_scene(1.6)
    shots = [
        ("face34", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=0.9, azimuth=-24, elevation=32)),
        ("face_front", "Spawn", 30, "Head", FACE_FOCUS, "close", dict(distance=0.85, azimuth=-8, elevation=6)),
        ("claws34", "Idle", 20, "Blade_L_2", BLADE_FOCUS, "close", dict(distance=1.1, azimuth=-55, elevation=18)),
        ("foot", "Walk", 10, "Leg_L1_Tibia", FOOT_FOCUS, "close", dict(distance=0.6, azimuth=-55, elevation=20)),
        ("game_close", "Walk", 10, "Head", FACE_FOCUS, "game", dict(ortho=1.3)),
        ("game_play", "Walk", 10, "Root", Vector((0, 0, 0.4)), "game", dict(ortho=3.6)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0))
