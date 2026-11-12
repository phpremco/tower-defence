"""Round 45: the PackAlpha's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

- PackAlpha_Face: a hunter's eyes: three slanted wet eyes per side along the brow (orange-red irises, vertical slit
  pupils, the front pair biggest), saw teeth down the inner edges of both muzzle shears and an arc of hooked teeth
  over the glowing throat.
- PackAlpha_Claws: a keratin talon on every toe (three per foot, dark root to a pale hooked point).
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

HEAD_C = P(0, 0.42, 0.58)
THROAT = P(0, 0.37, 0.69)
FACE_FOCUS = P(0, 0.4, 0.7)


def direction(az, el):
    a, e = math.radians(az), math.radians(el)
    return Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.5, pupil_size=0.2, glow=1.0, seed=s) for s in range(3)]
    for side, sx in SIDES:
        # Three slanted eyes per side along the brow, from the snout back to the horns, biggest in front.
        for k, (az, el, r) in enumerate(((38, 14, 0.032), (56, 16, 0.026), (72, 14, 0.021))):
            p, n = boss.out(HEAD_C + Vector((0, -0.04 + 0.03 * k, 0)), direction(az * sx, el))
            D.add_eye(b, p - n * r * 0.4, n, r, eyes[k], sock, squash=(0.8, 1.35, 0.8), socket=0.2,
                      up=(Vector((0, -1, 0.35)) - n * n.y).normalized(), bone="Head")
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(3)]
    for side, sx in SIDES:
        crv = D.curve_points(A.jaw_points(sx), 50)
        D.edge_saw(boss, b, crv, (0.2, 0.3, 0.4, 0.5, 0.6, 0.7), Vector((-sx, 0, 0)), 0.013, 0.05, tooth[0], f"Jaw_{side}",
                   rake=0.3, max_off=0.12, start=0.1)
    # An arc of hooked teeth over and beside the glowing throat between the shears (none below it, where they
    # would cross the glow): the maw seen when it howls.
    axis = Vector((0, 1, 0.05)).normalized()
    D.add_ring_teeth(b, THROAT + Vector((0, -0.07, 0)), axis, 0.078, 12, 0.06, 0.012, tooth[1], inward=0.8,
                     bone="Head", lod=False, name="RingTooth", skip=lambda phi: math.cos(phi) < -0.2)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.5, glow_from=0.85, seed=s, wet=0.25) for s in range(3)]
    for (side, kind), lg in A.legs().items():
        _, _, _, toe = lg.rest_points
        bone = lg.names[2]
        for k, a in enumerate((-0.4, 0.0, 0.4)):
            fwd = G(math.sin(a) * 0.7, 0, 1).normalized()
            tip = toe + fwd * A.R(0.07) - Vector((0, 0, A.R(0.008)))
            d = (fwd + Vector((0, 0, -0.35))).normalized()
            D.add_claw(b, tip, d, 0.016, 0.07, claw[k], hook=Vector((0, 0, -1)), hook_amount=0.55, bone=bone,
                       lod=k != 1 or kind == "F")
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.4, "RingTooth": 1.3}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Claw": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 45: brow eyes, shear saw teeth, throat tooth ring, toe talons; bones, "
                                           "clips and events unchanged")


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
