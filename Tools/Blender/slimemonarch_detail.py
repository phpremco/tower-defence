"""Round 45: the SlimeMonarch's close-up detail (eyes and teeth), detail_kit / gravemaw_maw technique.

A slug has no claws, so the monarch gets one part, its face:
- SlimeMonarch_Face: a wet eye on the tip of each of the six stalks over the old glowing bulbs (yellow irises with
  horizontal bar pupils, lime glow, glossy corneas, dark lids; they follow the stalks' scale when they retract) and
  a second ring of hooked teeth inside the lamprey lip, raking down the glowing gullet. The old
  bulbs' glow is lowered in the body maps.
LOD1 version in SlimeMonarch_LOD1.fbx (the inner tooth ring left out there). No bone, clip or event changes.

Run after build_slimemonarch.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/slimemonarch_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_oozer as OZ  # noqa: E402
import build_slimemonarch as SM  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "SlimeMonarch"
PARTS = ("Face",)
# Budget: body 13000 / LOD1 5999 tris -> at most +2600 / +899.
FACE_TRIS, FACE_LOD = 2500, 850
VOXEL = 0.0024
M = SM.M
SIDES = OZ.SIDES

EYE = D.Eye(lens=(0.035, 0.035, 0.008), iris=(1.0, 0.95, 0.4), rim=(0.2, 0.25, 0.03), pupil=(0.004, 0.006, 0.0),
            glow=(0.7, 1.0, 0.2), vein=(0.2, 0.26, 0.04))
SOCKET = D.Flesh(dark=(0.03, 0.03, 0.01), flesh=(0.22, 0.22, 0.06))
TOOTH = D.Keratin(root=(0.55, 0.6, 0.35), body=(0.9, 0.9, 0.72), tip=(0.98, 0.98, 0.9), glow=(0.66, 1.0, 0.16),
                  stain=(0.4, 0.5, 0.15))

MOUTH = G(0, 0.12, 0.465) * M
MOUTH_AXIS = G(0, 0.15, 1.0).normalized()
FACE_FOCUS = G(0, 0.2, 0.42) * M


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=8)
    eyes = [D.eye_style(EYE, pupil="bar", iris_size=0.5, pupil_size=0.24, glow=0.9, seed=s) for s in range(3)]
    for side, sx in SIDES:
        for n, (root, tip) in SM.STALKS.items():
            bone = f"Stalk_{side}{n}"
            r_, t_ = boss.head(bone), boss.tail(bone)
            d = (t_ - r_).normalized()
            # The round-10 bulb sits a little past the stalk tip: the eye covers it, looking out along the stalk.
            r = 0.058 if n == 1 else 0.05
            # The eye looks forward and out from the stalk tip, so the play camera and a front view both meet it.
            look = (d * 0.55 + Vector((0.3 * sx, -0.75, 0.0))).normalized()
            D.add_eye(b, t_ + d * 0.02, look, r, eyes[n - 1], sock, squash=(1.0, 1.0, 0.9), socket=0.22,
                      up=Vector((0, 0, 1)) - look * look.z, bone=bone, socket_shift=-0.5,
                      socket_size=0.86)
    # The maw: a second ring of hooked teeth inside the lamprey lip, between the old teeth, raking down the glowing
    # gullet (deeper rings would sit inside the gullet's glow and never show).
    tooth = [D.tooth_style(TOOTH, glow=0.4, seed=s) for s in range(3)]
    D.add_ring_teeth(b, MOUTH + MOUTH_AXIS * 0.01, -MOUTH_AXIS, 0.068, 12, 0.055, 0.012, tooth[0], inward=0.85,
                     offset=0.2618, bone="Head", lod=False, name="RingTooth")
    return b


def build(boss, blockout_only=False):
    face = face_parts(boss)
    if blockout_only:
        return {"Face": face}
    # The round-10 glowing bulbs under the new stalk eyes: their glow goes, so the eyes carry it.
    bulbs = []
    for side, sx in SIDES:
        for n in SM.STALKS:
            bone = f"Stalk_{side}{n}"
            d = (boss.tail(bone) - boss.head(bone)).normalized()
            bulbs.append((boss.tail(bone) + d * 0.024, 0.06))
    dimmed = D.dim_body(boss, bulbs, emission_keep=0.12)
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.6, "RingTooth": 1.4})}
    return D.build_all(boss, specs, VOXEL, "round 45: stalk eyes and a toothed maw (no claws on a slug); bones, clips "
                                           "and events unchanged",
                       dimmed=f"stalk bulbs' glow lowered in SlimeMonarch_Emission ({dimmed} texels this run)")


def qa(boss, tag):
    D.qa_scene(1.6)
    shots = [
        ("face34", "Idle", 20, "Head", FACE_FOCUS, "close", dict(distance=1.1, azimuth=-34, elevation=20)),
        ("mouth", "Flood", 18, "Head", MOUTH, "close", dict(distance=0.6, azimuth=-12, elevation=0)),
        ("eyes", "Idle", 30, "Stalk_L1", boss.tail("Stalk_L1"), "close", dict(distance=0.45, azimuth=-30, elevation=15)),
        ("game_close", "Crawl", 10, "Head", FACE_FOCUS, "game", dict(ortho=1.4)),
        ("game_play", "Crawl", 10, "Root", Vector((0, 0, 0.3)), "game", dict(ortho=3.4)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.1), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 1.1))
