"""Round 45: the StormMatriarch's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

The ray boss (in Unity on the Sky Sovereign's data, entry "RayBoss"); cool palette (blue body, cyan glow).
- StormMatriarch_Face: wet cyan eyes with cuttlefish bar pupils on the six round-6 eye pits, saw teeth down the
  facing edges of both barbed pincers, and a row of hooked teeth along the mouth slit under the prow.
- StormMatriarch_Claws: hooked keratin points (cyan-lit) on both pincers and on the three tines of the tail trident.
Both have LOD1 versions in StormMatriarch_LOD1.fbx (small teeth left out there). No bone, clip or event changes.

Run after build_stormmatriarch.py (which calls run() at the end of a rebuild; the warm palette variant never
exports, so it never runs this):
  blender -b --factory-startup --python Tools/Blender/stormmatriarch_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_stormmatriarch as SM  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402

NAME = "StormMatriarch"
PARTS = ("Face", "Claws")
# Budget: body 13446 / LOD1 6000 tris -> at most +2689 / +900.
FACE_TRIS, CLAW_TRIS = 1750, 850
FACE_LOD, CLAW_LOD = 560, 320
VOXEL = 0.0028
P = SM.P
SIDES = SM.SIDES

EYE = D.Eye(lens=(0.01, 0.02, 0.045), iris=(0.45, 0.95, 1.0), rim=(0.04, 0.12, 0.3), pupil=(0.0, 0.002, 0.008),
            glow=(0.4, 0.95, 1.0), vein=(0.12, 0.08, 0.3))
SOCKET = D.Flesh(dark=(0.012, 0.016, 0.04), flesh=(0.1, 0.13, 0.32))
TOOTH = D.Keratin(root=(0.4, 0.45, 0.6), body=(0.8, 0.86, 0.95), tip=(0.95, 0.98, 1.0), glow=(0.4, 0.95, 1.0),
                  stain=(0.2, 0.15, 0.35))
CLAW = D.Keratin(root=(0.02, 0.025, 0.06), body=(0.1, 0.13, 0.3), tip=(0.85, 0.95, 1.0), glow=(0.4, 0.95, 1.0),
                 stain=(0.04, 0.04, 0.1))

FACE_FOCUS = P(0, 0.02, 0.42)
EYES = ((0.044, 0.39), (0.067, 0.335), (0.09, 0.28))


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=10)
    eyes = [D.eye_style(EYE, pupil="bar", iris_size=0.5, pupil_size=0.24, glow=1.0, seed=s) for s in range(3)]
    head_c = P(0, -0.01, 0.33)
    for side, sx in SIDES:
        for i, (x, f) in enumerate(EYES):
            p = P(x * sx, 0.047 - 0.004 * i, f)
            n = (p - head_c).normalized()
            n = (n + Vector((0, 0, 1.2))).normalized()
            # The round-6 eye pit is a bead of radius 0.033 round p: the eye stands out of it and covers it.
            D.add_eye(b, p + n * 0.012, n, (0.037, 0.032, 0.028)[i], eyes[i], sock, squash=(0.92, 1.2, 0.8),
                      socket=0.2, up=Vector((0, -1, 0)), bone="Head")
    tooth = [D.tooth_style(TOOTH, glow=0.45, seed=s) for s in range(3)]
    for side, sx in SIDES:
        crv = D.curve_points(SM.pincer_points(sx), 50)
        D.edge_saw(boss, b, crv, (0.3, 0.42, 0.54, 0.66, 0.78), Vector((-sx, 0, 0)), 0.014, 0.05, tooth[0],
                   f"Pincer_{side}", rake=0.3, max_off=0.12, start=0.1)
    # The mouth slit under the prow: a row of hooked teeth hanging from its front lip.
    for k in range(4):
        for sx in (-1, 1):
            x = 0.012 + 0.022 * k
            p, n = boss.nearest(P(x * sx, -0.03, 0.47 - 0.02 * k * k * 0.3))
            D.add_tooth(b, p - n * 0.004, Vector((0, 0.2, -1)).normalized(), 0.011, 0.045 - 0.005 * k, tooth[k % 3],
                        hook=Vector((0, 1, 0)), bone="Head", lod=False, name="MouthTooth")
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.9, glow_from=0.78, seed=s, wet=0.3) for s in range(3)]
    for side, sx in SIDES:
        crv = D.curve_points(SM.pincer_points(sx), 50)
        end, tangent = crv[-6], (crv[-1] - crv[-7]).normalized()
        D.add_claw(b, end, tangent, 0.024, 0.11, claw[0], hook=Vector((-sx, 0, -0.2)), hook_amount=0.5,
                   bone=f"Pincer_{side}", name="PincerTip")
    end = SM.TAIL[-1]
    for k, (a, c) in enumerate(((end, end + P(0, 0.0, -0.18)), (end, end + P(0.09, 0.004, -0.13)),
                                (end, end + P(-0.09, 0.004, -0.13)))):
        d = (c - a).normalized()
        D.add_claw(b, c - d * 0.05, d, 0.02, 0.12, claw[k], hook=Vector((0, 0, -1)), hook_amount=0.45, bone="Tail_5",
                   name="Tine")
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.4, "MouthTooth": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"PincerTip": 1.2, "Tine": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 45: wet eyes on the eye pits, pincer saw teeth, mouth teeth, "
                                           "pincer and trident points; bones, clips and events unchanged")


def qa(boss, tag):
    D.qa_scene(2.0, ground_z=boss.head("Root").z)
    tail = SM.TAIL[-1] + P(0, 0.0, -0.08)
    shots = [
        ("face34", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=1.3, azimuth=-34, elevation=26)),
        ("face_front", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=1.2, azimuth=-6, elevation=4)),
        ("tail", "Idle", 20, "Tail_5", tail, "close", dict(distance=0.9, azimuth=-150, elevation=30)),
        ("game_close", "Fly", 8, "Head", FACE_FOCUS, "game", dict(ortho=2.0)),
        ("game_play", "Fly", 8, "Body", Vector((0, 0.2, 0)), "game", dict(ortho=6.0)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.3), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.3))
