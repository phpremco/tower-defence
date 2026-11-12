"""Round 45: the StormMatriarch's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

The ray boss (in Unity on the Sky Sovereign's data, entry "RayBoss"); cool palette (blue body, cyan glow).
- StormMatriarch_Face: wet cyan eyes with cuttlefish bar pupils on top of the head and (round 49A, Remco: "the teeth
  are not logical") the teeth where a ray's mouth is: rows on the upper and the lower lip across the blunt front edge
  (skyray_detail.mouth_teeth at the boss's size); the cephalic lobes (Pincer bones) carry none.
- StormMatriarch_Claws: hooked talons under the fin tips and (round 49A) saw barbs along the tail spade's edges.
Both have LOD1 versions in StormMatriarch_LOD1.fbx (small teeth left out there). Round 49A adds the Jaw bone.

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
import skyray_detail as SD  # noqa: E402

NAME = "StormMatriarch"
PARTS = ("Face", "Claws")
# Budget: body 13446 / LOD1 6000 tris -> at most +2689 / +900.
FACE_TRIS, CLAW_TRIS = 2150, 450
FACE_LOD, CLAW_LOD = 560, 320
VOXEL = 0.0028
P = SM.P
SIDES = SM.SIDES

EYE = D.Eye(lens=(0.01, 0.02, 0.045), iris=(0.45, 0.95, 1.0), rim=(0.04, 0.12, 0.3), pupil=(0.0, 0.002, 0.008),
            glow=(0.4, 0.95, 1.0), vein=(0.12, 0.08, 0.3))
SOCKET = D.Flesh(dark=(0.012, 0.016, 0.04), flesh=(0.1, 0.13, 0.32))
# Round 49E: ivory teeth with dark gum-stained roots, as the Skyray's.
TOOTH = D.Keratin(root=(0.2, 0.05, 0.1), body=(0.82, 0.8, 0.74), tip=(0.96, 0.95, 0.9), glow=(0.4, 0.95, 1.0),
                  stain=(0.3, 0.08, 0.12))
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
    SD.mouth_teeth(boss, b, TOOTH, SM.MOUTH, upper=(0.0, 0.02, 0.04, 0.06, 0.078), lower=(0.01, 0.03, 0.05, 0.068),
                   length=1.3)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.9, glow_from=0.78, seed=s, wet=0.3) for s in range(3)]
    for side, sx in SIDES:
        tip = boss.tail(f"Fin_{side}_A3")
        for k, (fwd, ln) in enumerate(((0.03, 0.075), (-0.03, 0.06))):
            # Round 56: rooted under the fin skin and skinned with its blended weights (skyray_detail.talon_root).
            base, anchor = SD.talon_root(boss, tip + P(-0.03 * sx, -0.008, fwd), 0.004 * SM.M)
            d = (P(0.35 * sx, -1.0, 0.25 if k == 0 else -0.2)).normalized()
            D.add_claw(b, base, d, 0.011 * SM.M, ln * SM.M, claw[k], hook=P(-0.6 * sx, -0.2, 0.4), hook_amount=0.6,
                       anchor=anchor, name="Talon", lod=k == 0)
    barb = D.tooth_style(TOOTH, glow=0.6, seed=8)
    SD.spade_barbs(b, barb, SM.TAIL[-1], SM.TAIL[-2], SM.M, boss)
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Tooth": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Talon": 1.2, "Barb": 1.2})}
    return D.build_all(boss, specs, VOXEL, "round 49A: wet eyes on the head, mouth teeth on both lips (Jaw bone new), "
                                           "fin-tip talons, spade barbs")


def qa(boss, tag):
    D.qa_scene(2.0, ground_z=boss.head("Root").z)
    tail = SM.TAIL[-1] + P(0, 0.0, -0.1)
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
