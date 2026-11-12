"""Round 46: the Stinger's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Stinger.fbx and Stinger_LOD1.fbx:
- Stinger_Face (Stinger_Face_Mat): the six magenta eye beads on the mound become wet magenta eyes with slit pupils
  under plum brow folds (the round-10 beads' glow goes from the body maps), and a pair of hooked chelicera fangs
  with smaller teeth beside them over the acid mouth.
- Stinger_Claws (Stinger_Claws_Mat): saw teeth down the cutting edges of both trap-claw fingers (the hook finger's
  teeth close with it) and a hooked keratin point on every finger.
No bone, clip or event changes; the actions are exported again as they are. build_acidempress.py reuses
build_stinger's geometry, not this file: the AcidEmpress is unaffected.

Run after build_stinger.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/stinger_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Stinger"
PARTS = ("Face", "Claws")
VOXEL = 0.0018

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): dark plum armour, magenta eyes and rims, acid-green venom.
EYE = D.Eye(lens=(0.03, 0.004, 0.025), iris=(1.0, 0.3, 0.85), rim=(0.25, 0.02, 0.2), pupil=(0.004, 0.0, 0.004),
            glow=(1.0, 0.35, 0.85), vein=(0.3, 0.03, 0.2))
LID_DARK, LID_SKIN, LID_WET = (0.05, 0.02, 0.05), (0.3, 0.12, 0.3), (0.15, 0.03, 0.12)
FANG = D.Keratin(root=(0.18, 0.05, 0.16), body=(0.7, 0.62, 0.66), tip=(0.95, 1.0, 0.75), glow=(0.7, 1.0, 0.25),
                 stain=(0.25, 0.3, 0.05))
CLAW = D.Keratin(root=(0.05, 0.02, 0.05), body=(0.18, 0.07, 0.17), tip=(1.0, 0.75, 0.95), glow=(1.0, 0.35, 0.85),
                 stain=(0.08, 0.02, 0.07))

HEAD_FOCUS = G(0, 0.24, 0.3)
MOUND = G(0, 0.275, 0.27)


def claw_points(sx):
    """build_stinger.claw_points with claw size 1: hand centre, fixed finger, moving finger."""
    def P(x, up, f):
        return G(x * sx, up, f)
    hand = P(0.215, 0.2, 0.53)
    fixed = [P(0.245, 0.2, 0.57), P(0.235, 0.2, 0.67), P(0.19, 0.2, 0.75), P(0.15, 0.2, 0.775)]
    moving = [P(0.18, 0.2, 0.575), P(0.14, 0.2, 0.64), P(0.135, 0.2, 0.7), P(0.155, 0.2, 0.735)]
    return hand, fixed, moving


def bead(i):
    a = math.radians(-70 + 140 * i / 5)
    return G(0.062 * math.sin(a), 0.292 + 0.006 * math.cos(a * 2), 0.27 + 0.048 * math.cos(a))


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=9)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.62, pupil_size=0.26, glow=1.0, seed=s) for s in range(3)]
    for k, i in enumerate((3, 4, 5)):                      # right-hand beads, front to side; mirrored
        p = bead(i)
        n = ((p - MOUND).normalized() + Vector((0, 0, 0.5))).normalized()
        # The beads touch: the eyes are smaller than them (the middle pair smallest) so the brows stay apart.
        r = (0.0155, 0.012, 0.0135)[k]
        push = (0.007, 0.011, 0.008)[k]
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p + n * push, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0.3, 1)),
                          bone="Body", lod=k == 0, brow=(1.05, 0.55, 0.5), lower=None, brow_lift=0.55,
                          segments=(20, 10), lid_segments=(12, 6))
    fang = [D.tooth_style(FANG, glow=0.45, seed=s) for s in range(2)]
    for sx in (1, -1):
        # Chelicera fangs: hooked down and back from over the mouth; smaller teeth outside them.
        D.add_tooth(b, G(0.022 * sx, 0.172, 0.35), G(0.15 * sx, -1.0, 0.35).normalized(), 0.009, 0.06, fang[0],
                    hook=G(-0.3 * sx, 0, -1), hook_amount=0.4, bone="Body", name="Fang")
        D.add_tooth(b, G(0.042 * sx, 0.165, 0.335), G(0.3 * sx, -1.0, 0.25).normalized(), 0.006, 0.032, fang[1],
                    hook=G(-0.3 * sx, 0, -1), hook_amount=0.3, bone="Body", lod=False, name="Tooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    point = [D.keratin_style(CLAW, glow=0.7, glow_from=0.75, seed=s, wet=0.35) for s in range(2)]
    saw = D.tooth_style(FANG, glow=0.35, seed=12)
    for side, sx in SIDES:
        hand, fixed, moving = claw_points(sx)
        for k, (pts, bone, inward) in enumerate(((fixed, f"Claw_{side}", Vector((-sx, 0, 0))),
                                                  (moving, f"Finger_{side}", Vector((sx, 0, 0))))):
            crv = D.curve_points(pts, 30)
            placed = D.edge_saw(c, b, crv, (0.3, 0.5, 0.7), inward, 0.006, 0.026, saw, bone, rake=0.45,
                                max_off=0.05, lod=False, start=0.04)
            C.log("claw saw", side, bone, placed)
            end = crv[-3]
            tangent = (crv[-1] - crv[-5]).normalized()
            D.add_claw(b, end - tangent * 0.008, tangent, 0.009, 0.045, point[k], hook=inward + Vector((0, 0, -0.3)),
                       hook_amount=0.55, bone=bone, name="Point")
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(bead(i), 0.022) for i in range(6)]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.05, 0.02, 0.05), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.56), int(lod1 * 0.5), {"Eye": 2.0, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.4), int(lod1 * 0.47), {"Point": 1.2, "Saw": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: six wet magenta eyes under brow folds, chelicera fangs, "
                                        "trap-claw saws and points; bones, clips and events unchanged",
                       body_maps=f"round-10 bead eyes' glow removed from Stinger_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    hand, fixed, _ = claw_points(-1)
    out = S.standard_shots("Body", HEAD_FOCUS, "Claw_L", fixed[1], "Claw_L", fixed[2], 1.2, face_dist=0.9,
                           claw_dist=0.45, foot_dist=0.35)
    out[1] = ("face_macro", "Idle", 15, "Body", G(-0.03, 0.29, 0.31), "close", dict(distance=0.28, azimuth=-30,
                                                                                    elevation=30))
    out[5] = ("mouth", "Idle", 15, "Body", G(0, 0.16, 0.34), "close", dict(distance=0.3, azimuth=-10, elevation=-2))
    return out


CLIP_FOCUS = ("Body", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_stinger.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
