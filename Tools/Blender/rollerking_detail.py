"""Round 45: the RollerKing's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

The Roller is eyeless (glowing sensor pits); in its king the pits have grown into eyes: three wet amber eyes per side
in a row along the brow under the scoop, set deep in dark lids (slit pupils).
- RollerKing_Face: those eyes, saw teeth down the inner edges of both hooked mandibles and an arc of hooked teeth over
  the glowing throat.
- RollerKing_Claws: three big keratin digging claws on each front foot (over the Roller's small ones, glowing amber
  at the points) and two hooked claws round each rear spike.
Both have LOD1 versions in RollerKing_LOD1.fbx (small teeth left out there). No bone, clip or event changes.

Run after build_rollerking.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/rollerking_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_rollerking as RK  # noqa: E402
import build_roller as R  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "RollerKing"
PARTS = ("Face", "Claws")
# Budget: body 13835 / LOD1 5997 tris -> at most +2767 / +899.
FACE_TRIS, CLAW_TRIS = 1700, 1000
FACE_LOD, CLAW_LOD = 560, 320
VOXEL = 0.0024
M = RK.M
SIDES = R.SIDES

EYE = D.Eye(lens=(0.04, 0.025, 0.008), iris=(1.0, 0.72, 0.25), rim=(0.3, 0.12, 0.03), pupil=(0.004, 0.002, 0.0),
            glow=(1.0, 0.6, 0.16), vein=(0.28, 0.1, 0.03))
SOCKET = D.Flesh(dark=(0.03, 0.018, 0.008), flesh=(0.3, 0.17, 0.07))
TOOTH = D.Keratin(root=(0.7, 0.55, 0.38), body=(0.9, 0.82, 0.64), tip=(0.98, 0.95, 0.86), glow=(1.0, 0.55, 0.12),
                  stain=(0.6, 0.4, 0.22))
CLAW = D.Keratin(root=(0.05, 0.03, 0.015), body=(0.24, 0.13, 0.05), tip=(0.97, 0.88, 0.62), glow=(1.0, 0.55, 0.12),
                 stain=(0.1, 0.05, 0.02))

HEAD_C = G(0, 0.14, 0.6) * M
THROAT = G(0, 0.11, 0.73) * M
FACE_FOCUS = G(0, 0.12, 0.72) * M


def direction(az, el):
    a, e = math.radians(az), math.radians(el)
    return Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=7)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.5, pupil_size=0.2, glow=1.0, seed=s) for s in range(3)]
    for side, sx in SIDES:
        # The sensor pits grown into eyes: three per side in a row along the brow under the scoop, set deep.
        for k, (az, el, r) in enumerate(((20, 16, 0.034), (34, 13, 0.029), (48, 10, 0.024))):
            p, n = boss.out(HEAD_C, direction(az * sx, el))
            D.add_eye(b, p - n * r * 0.55, n, r, eyes[k], sock, squash=(1.1, 0.9, 0.85), socket=0.24,
                      up=Vector((0, 0, 1)) - n * n.z, bone="Head")
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(3)]
    for side, sx in SIDES:
        crv = D.curve_points([p * M for p in R.jaw_points(sx)], 50)
        D.edge_saw(boss, b, crv, (0.22, 0.34, 0.46, 0.58, 0.7), Vector((-sx, 0, 0)), 0.014, 0.055, tooth[0],
                   f"Jaw_{side}", rake=0.3, max_off=0.12, start=0.1)
    D.add_ring_teeth(b, THROAT + Vector((0, -0.05, 0.0)), Vector((0, 1, 0.05)).normalized(), 0.08, 12, 0.055, 0.012,
                     tooth[1], inward=0.6, bone="Head", lod=False, name="RingTooth",
                     skip=lambda phi: math.cos(phi) < -0.2)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.7, glow_from=0.82, seed=s, wet=0.25) for s in range(3)]
    for side, sx in SIDES:
        bone = f"Leg_{side}1_Tibia"
        foot = boss.tail(bone)
        # Digging claws: broad, long and hooked down, fanned forward over the Roller's own small ones.
        for k, a in enumerate((-0.5, 0.0, 0.5)):
            fwd = G(0.6 * a * sx + 0.1 * sx, 0, 1).normalized()
            # Apart at the root, so the remesh keeps three claws and does not fuse them into a hoof.
            base = foot + Vector((0.09 * a * sx, 0, 0.035))
            d = (fwd + Vector((0, 0, -0.15))).normalized()
            D.add_claw(b, base, d, 0.03, 0.24, claw[k], hook=Vector((0, 0, -1)), hook_amount=0.45, bone=bone)
        bone = f"Leg_{side}2_Tibia"
        knee, foot = boss.head(bone), boss.tail(bone)
        down = (foot - knee).normalized()
        for k, f in enumerate((-1.0, 1.0)):
            d = (down * 0.3 + Vector((0, f * 0.8, 0)) + Vector((sx * 0.4, 0, 0.1))).normalized()
            D.add_claw(b, foot - down * 0.03, d, 0.02, 0.1, claw[k + 1], hook=Vector((0, 0, -1)), hook_amount=0.6,
                       bone=bone, lod=k == 0)
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.4, "RingTooth": 1.3}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Claw": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 45: pit eyes, mandible saw teeth, throat tooth arc, "
                                           "digging claws; bones, clips and events unchanged")


def qa(boss, tag):
    D.qa_scene(1.8)
    foot = boss.tail("Leg_L1_Tibia") + Vector((0, -0.05, 0.05))
    shots = [
        ("face34", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=1.0, azimuth=-34, elevation=14)),
        ("face_front", "Walk", 6, "Head", FACE_FOCUS, "close", dict(distance=0.95, azimuth=-8, elevation=6)),
        ("foot", "Idle", 10, "Leg_L1_Tibia", foot, "close", dict(distance=0.8, azimuth=-100, elevation=20)),
        ("rolled", "Roll", 6, "Root", Vector((0, 0, 0.6)), "close", dict(distance=2.6, azimuth=-40, elevation=18)),
        ("game_close", "Walk", 6, "Head", FACE_FOCUS, "game", dict(ortho=1.6)),
        ("game_play", "Walk", 6, "Root", Vector((0, 0, 0.5)), "game", dict(ortho=4.4)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.0))
