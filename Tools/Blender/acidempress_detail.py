"""Round 45: the AcidEmpress's close-up detail (eyes, teeth, claws), detail_kit / gravemaw_maw technique.

- AcidEmpress_Face: the eye mound gets a jumping-spider stare: two big principal eyes in front (magenta irises,
  round black pupils, glossy corneas) and eight wet eyes on the round-10 bead ring; a pair of hooked chelicera fangs
  with acid-lit tips over the mouth and rows of small teeth on both lips of the glowing mouth.
- AcidEmpress_Claws: crab teeth on the facing edges of both trap-claw fingers, keratin points on all four fingers,
  and two small hooked claws on each of the six feet.
Both have LOD1 versions in AcidEmpress_LOD1.fbx (small teeth left out there). No bone, clip or event changes.

Run after build_acidempress.py (which calls run() at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/acidempress_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_acidempress as A  # noqa: E402
import build_stinger as S  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "AcidEmpress"
PARTS = ("Face", "Claws")
# Budget: body 14217 / LOD1 5998 tris -> at most +2843 / +899.
FACE_TRIS, CLAW_TRIS = 1650, 1150
FACE_LOD, CLAW_LOD = 520, 360
VOXEL = 0.0026
M = A.M
SIDES = S.SIDES

EYE = D.Eye(lens=(0.03, 0.006, 0.03), iris=(1.0, 0.4, 0.86), rim=(0.2, 0.02, 0.16), pupil=(0.004, 0.0, 0.006),
            glow=(1.0, 0.36, 0.84), vein=(0.18, 0.03, 0.14))
SOCKET = D.Flesh(dark=(0.025, 0.008, 0.028), flesh=(0.26, 0.06, 0.24))
TOOTH = D.Keratin(root=(0.5, 0.3, 0.42), body=(0.86, 0.76, 0.82), tip=(0.97, 0.95, 0.9), glow=(0.78, 1.0, 0.2),
                  stain=(0.16, 0.03, 0.12))
CLAW = D.Keratin(root=(0.03, 0.01, 0.035), body=(0.18, 0.05, 0.17), tip=(0.95, 0.9, 0.86), glow=(1.0, 0.28, 0.82),
                 stain=(0.06, 0.015, 0.06))
FANG = D.Keratin(root=(0.04, 0.012, 0.04), body=(0.2, 0.06, 0.18), tip=(0.85, 1.0, 0.4), glow=(0.78, 1.0, 0.2),
                 stain=(0.06, 0.015, 0.06))

MOUND = G(0, 0.275, 0.27) * M
MOUTH = G(0, 0.16, 0.32) * M
FACE_FOCUS = G(0, 0.22, 0.3) * M
CLAW_FOCUS = G(-0.26, 0.2, 0.79) * M
FOOT_FOCUS = None   # set from the bones


def bead(i):
    a = math.radians(-70 + 140 * i / (A.CFG["eyes"] - 1))
    return G(0.062 * math.sin(a), 0.292 + 0.006 * math.cos(a * 2), 0.27 + 0.048 * math.cos(a)) * M


def face_parts(boss):
    b = C.PartBuilder()
    sock = D.socket_style(SOCKET, seed=5)
    small = [D.eye_style(EYE, pupil="slit", iris_size=0.46, pupil_size=0.22, glow=0.75, seed=s) for s in range(4)]
    big = D.eye_style(EYE, pupil="cross", iris_size=0.5, pupil_size=0.22, glow=0.6, seed=9)
    mound_c = MOUND - Vector((0, 0, 0.03))
    # A jumping spider's stare along the round-10 bead arc (whose glow goes from the body maps): two big principal
    # eyes in front, two pairs of smaller ones round the sides and a pair on top behind them.
    arc = ((18, 0.05, big, 0.0), (48, 0.036, small[0], 0.0), (74, 0.03, small[1], 0.0))
    for deg, r, style, _ in arc:
        for sx in (-1, 1):
            a = math.radians(deg) * sx
            p = G(0.062 * math.sin(a), 0.292 + 0.006 * math.cos(a * 2), 0.27 + 0.048 * math.cos(a)) * M
            n = (p - mound_c).normalized()
            n = (n + Vector((0, -0.3 if r > 0.05 else 0.0, 0))).normalized()
            D.add_eye(b, p - n * r * 0.15, n, r, style, sock, squash=(1.0, 1.0, 0.8), socket=0.16,
                      up=Vector((0, 0, 1)) - n * n.z, bone="Body")
    for sx in (-1, 1):
        p, n = boss.out(mound_c, Vector((0.35 * sx, 0.1, 1.0)))
        D.add_eye(b, p - n * 0.012, n, 0.028, small[2], sock, squash=(1.0, 1.0, 0.8), socket=0.16,
                  up=Vector((0, -1, 0)), bone="Body")
    # Chelicerae: the round-10 palps end in hooked fangs, acid-lit at the tips, curling in under the mouth.
    fang = D.keratin_style(FANG, glow=1.0, glow_from=0.65, seed=3, wet=0.5)
    for sx in (-1, 1):
        tip = G(0.035 * sx, 0.125, 0.395) * M
        d = Vector((-0.35 * sx, -0.35, -1.0)).normalized()
        D.add_claw(b, tip + Vector((0, 0.01, 0.02)), d, 0.026, 0.13, fang, hook=Vector((-0.3 * sx, 1.0, 0.1)),
                   hook_amount=0.55, bone="Body", name="Fang")
    # Lips: small teeth on the upper and lower rim of the glowing mouth, pointing in and back.
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(3)]
    rx, rz = 0.075, 0.05
    for k in (-1.5, -0.5, 0.5, 1.5):
        x = rx * k / 2.2
        for up in (1, -1):
            z = up * rz * math.sqrt(max(0.0, 1 - (x / rx) ** 2))
            base = MOUTH + Vector((x, -0.05, z))
            # Hanging from the lips like fangs (seen in profile from the front), only a little raked back.
            d = (Vector((-0.4 * x, 0, -z)).normalized() * 0.92 + Vector((0, 0.2, 0))).normalized()
            D.add_tooth(b, base, d, 0.012, 0.07 * (1.1 - 0.12 * abs(k)), tooth[int(k + 2) % 3],
                        hook=Vector((0, 1, 0)), hook_amount=0.3, bone="Body", lod=False, name="LipTooth")
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.8, glow_from=0.8, seed=s, wet=0.3) for s in range(3)]
    saw = D.tooth_style(TOOTH, glow=0.25, seed=21)
    for side, sx in SIDES:
        _, _, _, _, fixed, moving = S.claw_points(sx, A.CFG)
        fixed = [p * M for p in fixed]
        moving = [p * M for p in moving]
        cf, cm = D.curve_points(fixed, 50), D.curve_points(moving, 50)
        # Crab teeth on the facing edges: the fixed finger's inner edge faces the midline, the hook finger's outward.
        D.edge_saw(boss, b, cf, (0.18, 0.3, 0.42, 0.54, 0.66), Vector((-sx, 0, 0)), 0.014, 0.05, saw, f"Claw_{side}",
                   rake=0.3, max_off=0.12, start=0.08)
        D.edge_saw(boss, b, cm, (0.2, 0.35, 0.5, 0.65), Vector((sx, 0, 0)), 0.013, 0.045, saw, f"Finger_{side}",
                   rake=0.3, max_off=0.12, start=0.06)
        for crv, bone in ((cf, f"Claw_{side}"), (cm, f"Finger_{side}")):
            end, tangent = crv[-5], (crv[-1] - crv[-6]).normalized()
            D.add_claw(b, end - tangent * 0.02, tangent, 0.024, 0.1, claw[0], hook=Vector((-sx, 0, -0.4)) if bone[0] == "C"
                       else Vector((sx, 0, -0.4)), hook_amount=0.5, bone=bone, name="FingerTip")
    for side, sx in SIDES:
        for i in (1, 2, 3):
            bone = f"Leg_{side}{i}_Lower"
            knee, foot = boss.head(bone), boss.tail(bone)
            down = (foot - knee).normalized()
            base = foot - down * 0.07
            for k, f in enumerate((-1.0, 1.0)):
                d = (down * 0.3 + Vector((0, f * 0.8, 0)) + Vector((sx * 0.5, 0, 0.12))).normalized()
                D.add_claw(b, base, d, 0.014, 0.075, claw[k + 1], hook=Vector((0, 0, -1)), hook_amount=0.6, bone=bone,
                           lod=k == 0)
    return b


def build(boss, blockout_only=False):
    face, claws = face_parts(boss), claw_parts(boss)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    # The round-10 eye beads fused into one glowing bar: its glow goes, the new eyes sit on the dark ridge.
    dimmed = D.dim_body(boss, [(bead(i), 0.05) for i in range(A.CFG["eyes"])], emission_keep=0.05,
                        darken=(0.07, 0.02, 0.07), darken_amount=0.55)
    specs = {"Face": (face, FACE_TRIS, FACE_LOD, {"Eye": 1.8, "Fang": 1.3, "LipTooth": 1.4}),
             "Claws": (claws, CLAW_TRIS, CLAW_LOD, {"Saw": 1.4, "FingerTip": 1.3})}
    return D.build_all(boss, specs, VOXEL, "round 45: principal eyes and eye ring, chelicera fangs, lip teeth, crab "
                                           "teeth and finger points, foot claws; bones, clips and events unchanged",
                       dimmed=f"eye-bead bar's glow removed from AcidEmpress_Albedo/Emission ({dimmed} texels this run)")


def qa(boss, tag):
    D.qa_scene(1.8)
    foot = boss.tail("Leg_L1_Lower") + Vector((0, 0, 0.06))
    shots = [
        ("face34", "Idle", 20, "Body", FACE_FOCUS, "close", dict(distance=1.1, azimuth=-30, elevation=20)),
        ("face_front", "Strike", 8, "Body", FACE_FOCUS, "close", dict(distance=1.0, azimuth=-6, elevation=10)),
        ("mouth", "Idle", 20, "Body", MOUTH, "close", dict(distance=0.55, azimuth=-25, elevation=12)),
        ("claws34", "Idle", 20, "Claw_L", CLAW_FOCUS, "close", dict(distance=0.9, azimuth=-20, elevation=45)),
        ("foot", "Walk", 10, "Leg_L1_Lower", foot, "close", dict(distance=0.6, azimuth=-55, elevation=20)),
        ("game_close", "Walk", 10, "Body", FACE_FOCUS, "game", dict(ortho=1.6)),
        ("game_play", "Walk", 10, "Root", Vector((0, 0, 0.4)), "game", dict(ortho=4.6)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    D.run(NAME, PARTS, build, qa, ("Body", FACE_FOCUS, 1.1), norender)


if __name__ == "__main__":
    D.standard_main(NAME, PARTS, build, qa, ("Body", FACE_FOCUS, 1.1))
