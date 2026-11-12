"""Round 45: the SkySovereign's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

The bat boss (in Unity on the Storm Matriarch's data, entry "BatBoss").
- SkySovereign_Face: wet amber eyes with slit pupils on the four round-10 eye beads per side, two long hooked upper
  fangs with magenta-lit points and a row of smaller teeth under the snout, and a row of teeth along the dropping
  jaw (on the Jaw bone, so they open with it).
- SkySovereign_Claws: a big hooked thumb claw on each wrist and a hooked keratin claw on the tip of every wing finger
  (they break the wing outline from above).
Both have LOD1 versions in SkySovereign_LOD1.fbx (small teeth left out there). No bone, clip or event changes.

Run after build_skysovereign.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/skysovereign_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_bat as BT  # noqa: E402
import build_skysovereign as SS  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "SkySovereign"
PARTS = ("Face", "Claws")
# Budget: body 12607 / LOD1 6000 tris -> at most +2521 / +900.
FACE_TRIS, CLAW_TRIS = 1500, 950
FACE_LOD, CLAW_LOD = 520, 360
VOXEL = 0.0018
M = SS.M
SIDES = SS.SIDES

EYE = D.Eye(lens=(0.04, 0.012, 0.02), iris=(1.0, 0.74, 0.36), rim=(0.3, 0.08, 0.06), pupil=(0.004, 0.0, 0.002),
            glow=(1.0, 0.6, 0.35), vein=(0.3, 0.05, 0.1))
SOCKET = D.Flesh(dark=(0.03, 0.008, 0.016), flesh=(0.3, 0.07, 0.14))
TOOTH = D.Keratin(root=(0.55, 0.3, 0.32), body=(0.9, 0.82, 0.74), tip=(0.98, 0.94, 0.88), glow=(1.0, 0.32, 0.62),
                  stain=(0.4, 0.08, 0.18))
CLAW = D.Keratin(root=(0.04, 0.012, 0.02), body=(0.2, 0.05, 0.09), tip=(0.95, 0.85, 0.72), glow=(1.0, 0.32, 0.62),
                 stain=(0.08, 0.02, 0.04))

# Round 49C: the head sits on a neck and is bigger (BT.hp with the boss's head scale SS.HS).
HS = SS.HS
FACE_FOCUS = BT.hp(G(0, 0.02, 0.2), HS) * M


def bead(i, sx):
    return BT.eye_bead(i, sx, HS) * M


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=9)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.5, pupil_size=0.22, glow=1.0, seed=s) for s in range(4)]
    head_c = BT.hp(G(0, 0.03, 0.15), HS) * M
    for side, sx in SIDES:
        for i, r in enumerate((0.018 * HS, 0.016 * HS, 0.0145 * HS, 0.013 * HS)):
            p = bead(i, sx)
            n = (p - head_c).normalized()
            D.add_eye(b, p + n * 0.002, n, r, eyes[i], sock, squash=(1.0, 1.0, 0.85), socket=0.2,
                      up=Vector((0, 0, 1)) - n * n.z, bone="Head")
    tooth = [D.tooth_style(TOOTH, glow=0.5, seed=s) for s in range(3)]
    fang = D.keratin_style(D.Keratin(root=TOOTH.root, body=TOOTH.body, tip=(1.0, 0.7, 0.85), glow=TOOTH.glow),
                           glow=1.0, glow_from=0.75, seed=4, wet=0.5)
    # Upper teeth hanging under the snout: two long hooked fangs and smaller teeth beside and between them.
    for x, f, length, rad, style, name in ((0.022, 0.235, 0.075, 0.011, fang, "Fang"),
                                            (0.042, 0.215, 0.04, 0.008, tooth[0], "Tooth"),
                                            (0.008, 0.245, 0.032, 0.007, tooth[1], "Tooth")):
        for sx in (-1, 1):
            p, n = boss.nearest(BT.hp(G(x * sx, 0.005, f), HS) * M)
            d = Vector((0, -0.18, -1)).normalized()
            if name == "Fang":
                D.add_claw(b, p + Vector((0, 0, 0.004)), d, rad * HS, length * HS, style, hook=Vector((0, 1, 0)),
                           hook_amount=0.4, bone="Head", name=name)
            else:
                D.add_tooth(b, p + Vector((0, 0, 0.003)), d, rad * HS, length * HS, style, hook=Vector((0, 1, 0)),
                            bone="Head", lod=False, name=name)
    # Lower teeth along the jaw, pointing up.
    for x, f, length in ((0.03, 0.22, 0.03), (0.018, 0.235, 0.034), (0.038, 0.205, 0.026)):
        for sx in (-1, 1):
            p, n = boss.nearest(BT.hp(G(x * sx, 0.0, f), HS) * M + Vector((0, 0, -0.02)))
            D.add_tooth(b, p - n * 0.003, Vector((0, -0.1, 1)).normalized(), 0.007 * HS, length * HS, tooth[2],
                        hook=Vector((0, 1, 0)), bone="Jaw", lod=False, name="JawTooth")
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.8, glow_from=0.8, seed=s, wet=0.3) for s in range(3)]
    for side, sx in SIDES:
        wrist = boss.head(f"Hand_{side}")
        thumb = wrist + G(-0.01 * sx, 0.0, 0.07) * M
        D.add_claw(b, thumb - Vector((0, -0.02, 0)), Vector((-0.2 * sx, -1.0, -0.3)).normalized(), 0.018, 0.1, claw[0],
                   hook=Vector((0, 0.3, -1)), hook_amount=0.6, bone=f"Hand_{side}", name="Thumb")
        for i in (1, 2, 3):
            bone = f"Finger_{side}{i}"
            a, t = boss.head(bone), boss.tail(bone)
            d = (t - a).normalized()
            D.add_claw(b, t - d * 0.03, (d + Vector((0, 0.25, -0.15))).normalized(), 0.013, 0.08, claw[i - 1],
                       hook=Vector((0, 0.6, -1)), hook_amount=0.55, bone=bone, name="WingClaw")
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.5, "JawTooth": 1.5, "Fang": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Thumb": 1.3, "WingClaw": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 45: wet eyes on the bead rows, fangs and jaw teeth, thumb and wing "
                                           "claws; bones, clips and events unchanged")


def qa(boss, tag):
    D.qa_scene(1.4, ground_z=boss.head("Root").z)
    shots = [
        ("face34", "Glide", 10, "Head", FACE_FOCUS, "close", dict(distance=0.6, azimuth=-34, elevation=14)),
        ("face_open", "Laser", 30, "Head", FACE_FOCUS, "close", dict(distance=0.6, azimuth=-12, elevation=-6)),
        ("claws34", "Glide", 10, "Hand_L", boss.head("Hand_L"), "close", dict(distance=0.7, azimuth=-40, elevation=30)),
        ("game_close", "Glide", 10, "Head", FACE_FOCUS, "game", dict(ortho=1.0)),
        ("game_play", "Fly", 4, "Body", Vector((0, 0, 0)), "game", dict(ortho=3.6)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 0.65), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Head", FACE_FOCUS, 0.65))
