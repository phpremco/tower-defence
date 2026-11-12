"""Round 45: the PackAlpha's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

- PackAlpha_Face (round 49A, re-fitted to the alien-wolf skull of wolf_kit.py): three slanted wet eyes per side in a
  row under the brow ridge (the Prowler has two: the leader has one pair more), long upper canines and a row of
  teeth under the muzzle, lower fangs on both halves of the split jaw (they open with it on the howl).
- PackAlpha_Claws: a keratin talon on each of the four toes of every paw (dark root to a pale hooked point).
Both have LOD1 versions in PackAlpha_LOD1.fbx (small teeth left out there). No bone, clip or event changes.

Run after build_packalpha.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/packalpha_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_packalpha as A  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "PackAlpha"
PARTS = ("Face", "Claws")
# Budget: body 14018 / LOD1 5997 tris -> at most +2803 / +899.
FACE_TRIS, CLAW_TRIS = 1800, 950
FACE_LOD, CLAW_LOD = 560, 320
VOXEL = 0.0024
P = A.P
SIDES = A.SIDES

EYE = D.Eye(lens=(0.04, 0.01, 0.006), iris=(1.0, 0.48, 0.12), rim=(0.3, 0.05, 0.02), pupil=(0.004, 0.0, 0.0),
            glow=(1.0, 0.4, 0.1), vein=(0.3, 0.05, 0.03))
SOCKET = D.Flesh(dark=(0.03, 0.01, 0.008), flesh=(0.3, 0.06, 0.035))
TOOTH = D.Keratin(root=(0.32, 0.1, 0.05), body=(0.86, 0.76, 0.6), tip=(0.97, 0.94, 0.86), glow=(1.0, 0.52, 0.14),
                  stain=(0.3, 0.05, 0.03))
CLAW = D.Keratin(root=(0.04, 0.015, 0.01), body=(0.2, 0.06, 0.035), tip=(0.95, 0.86, 0.66), glow=(1.0, 0.52, 0.14),
                 stain=(0.08, 0.02, 0.012))

FACE_FOCUS = P(0, 0.74, 0.64)
UP = Vector((0, 0, 1))
FWD = G(0, 0, 1)


def direction(az, el):
    a, e = math.radians(az), math.radians(el)
    return Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.5, pupil_size=0.2, glow=1.0, seed=s) for s in range(3)]
    for side, sx in SIDES:
        # Three slanted eyes per side in a row under the brow ridge, biggest in front.
        for k, (x, up, fwd, r, look) in enumerate(((0.03, 0.768, 0.6, 0.02, (1.0, -0.9, 0.25)),
                                                   (0.04, 0.782, 0.556, 0.016, (1.0, -0.45, 0.4)),
                                                   (0.046, 0.79, 0.515, 0.013, (1.0, -0.1, 0.5)))):
            look = Vector((look[0] * sx, look[1], look[2])).normalized()
            p, n = boss.out(P(x * 0.3 * sx, up, fwd), look)
            n = (n * 0.5 + look * 0.5).normalized()
            D.add_eye(b, p - n * A.R(r) * 0.4, n, A.R(r), eyes[k], sock, squash=(0.8, 1.3, 0.8), socket=0.2,
                      up=(Vector((0, -1, 0.35)) - n * n.y).normalized(), bone="Head", lod=k < 2)
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(3)]
    for side, sx in SIDES:
        # Upper row under the muzzle: a long canine in front, shorter teeth behind it.
        for j, (f, length, rad, lod) in enumerate(((0.715, 0.056, 0.011, True), (0.68, 0.028, 0.007, False),
                                                   (0.645, 0.026, 0.0065, False), (0.61, 0.024, 0.006, False))):
            p, n = boss.out(P(0.0, 0.71, f), Vector((sx * 0.55, 0, -1)))
            d = (Vector((0, 0, -1)) * 0.85 + FWD * 0.15).normalized()
            D.add_tooth(b, p - n * A.R(rad) * 0.6, d, A.R(rad), A.R(length), tooth[j % 3], hook=-FWD, hook_amount=0.25,
                        bone="Head", lod=lod, name="Tooth")
        # Lower fangs on the mandible half.
        crv = D.curve_points(A.jaw_points(sx), 30)
        for j, (t, length, rad, lod) in enumerate(((0.86, 0.042, 0.0095, True), (0.68, 0.024, 0.0062, False),
                                                   (0.52, 0.022, 0.0058, False), (0.36, 0.02, 0.0052, False))):
            c = crv[int(t * (len(crv) - 1))]
            base = c + UP * A.R(0.012) + Vector((-sx * A.R(0.004), 0, 0))
            d = (UP * 0.9 + FWD * 0.1 + Vector((-sx * 0.1, 0, 0))).normalized()
            D.add_tooth(b, base, d, A.R(rad), A.R(length), tooth[j % 3], hook=-FWD, hook_amount=0.25, bone=f"Jaw_{side}",
                        lod=lod, name="Tooth")
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.5, glow_from=0.85, seed=s, wet=0.25) for s in range(3)]
    for (side, kind), lg in A.legs().items():
        _, _, _, toe = lg.rest_points
        bone = lg.names[2]
        base = toe + FWD * A.R(0.02) + UP * A.R(0.014)
        for k, a in enumerate((-0.5, -0.17, 0.17, 0.5)):
            d0 = G(math.sin(a) * 0.6, 0, 1).normalized()
            tip = base + d0 * A.R(0.056) - UP * A.R(0.006)
            d = (d0 + Vector((0, 0, -0.55))).normalized()
            D.add_claw(b, tip, d, A.R(0.0085), A.R(0.042), claw[k % 3], hook=Vector((0, 0, -1)), hook_amount=0.6,
                       bone=bone, lod=k in (1, 2))
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Claw": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 49A: three brow eyes per side, upper canines and teeth, split-jaw "
                                           "fangs, paw talons (round 45 parts re-fitted to the alien-wolf body)")


def qa(boss, tag):
    D.qa_scene(1.6)
    foot = boss.tail("Leg_LF_Foot") + Vector((0, 0, 0.05))
    shots = [
        ("face34", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=1.0, azimuth=-34, elevation=20)),
        ("face_front", "Howl", 22, "Head", FACE_FOCUS, "close", dict(distance=0.95, azimuth=-10, elevation=4)),
        ("foot", "Run", 6, "Leg_LF_Foot", foot, "close", dict(distance=0.55, azimuth=-50, elevation=22)),
        ("game_close", "Run", 6, "Head", FACE_FOCUS, "game", dict(ortho=1.4)),
        ("game_play", "Run", 6, "Root", Vector((0, 0, 0.6)), "game", dict(ortho=4.0)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0))
