"""Round 46: the Stinger's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Stinger.fbx and Stinger_LOD1.fbx:
- Stinger_Face (Stinger_Face_Mat): wet magenta eyes with slit pupils under plum brow folds over the body's eye beads
  (their glow goes from the body maps), and a hooked pale fang with a small tooth on each chelicera. Round 49C: they
  sit on the new head in front of the carapace (two big median eyes, three small ones at each front corner).
- Stinger_Claws (Stinger_Claws_Mat): saw teeth down the cutting edges of both trap-claw fingers (the hook finger's
  teeth close with it) and a hooked keratin point on every finger.
No bone, clip or event changes; the actions are exported again as they are. build_acidempress.py reuses
build_stinger's geometry, not this file: the AcidEmpress is unaffected.

Run after build_stinger.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/stinger_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_stinger as BS  # noqa: E402
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

# Round 49C: the eyes and fangs sit on the new head in front of the carapace (build_stinger.head_parts).
HEAD_FOCUS = BS.hs(G(0, 0.235, 0.39))
MOUND = BS.hs(G(0, 0.255, 0.385))


def claw_points(sx):
    """build_stinger.claw_points with claw size 1: hand centre, fixed finger, moving finger."""
    def P(x, up, f):
        return G(x * sx, up, f)
    hand = P(0.215, 0.2, 0.53)
    fixed = [P(0.245, 0.2, 0.57), P(0.235, 0.2, 0.67), P(0.19, 0.2, 0.75), P(0.15, 0.2, 0.775)]
    moving = [P(0.18, 0.2, 0.575), P(0.14, 0.2, 0.64), P(0.135, 0.2, 0.7), P(0.155, 0.2, 0.735)]
    return hand, fixed, moving


def eye_beads():
    """The body's eye beads on the head (right side first, then mirrored): (centre, radius)."""
    out = []
    for sx in (1, -1):
        for _, c, r in BS.head_eyes():
            out.append((Vector((c.x * sx, c.y, c.z)), r))
    return out


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=9)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.62, pupil_size=0.26, glow=1.0, seed=s) for s in range(4)]
    # Two big median eyes on the brow mound and three small ones at each front corner of the head (round 49C).
    for k, (name, p, r_bead) in enumerate(BS.head_eyes()):
        n = ((p - BS.head_centre()).normalized() + Vector((0, -0.2, 0.25))).normalized()
        r = r_bead * (0.95 if name == "Main" else 0.9)
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0.3, 1)),
                          bone="Body", lod=k < 2, brow=(1.05, 0.55, 0.5), lower=None, brow_lift=0.55,
                          segments=(20, 10), lid_segments=(12, 6))
    fang = [D.tooth_style(FANG, glow=0.55, seed=s) for s in range(2)]
    for sx in (1, -1):
        # A pale fang at the tip of each chelicera, hooked in and down; smaller teeth on the inner edge behind it.
        pts = BS.chelicera_points(1)
        tip, before = pts[2], pts[1]
        D.add_tooth(b, S.flip(tip, sx), S.flip(G(-0.35, -1.0, 0.45).normalized(), sx), 0.011, 0.06, fang[0],
                    hook=S.flip(G(-1.0, -0.2, -0.3), sx), hook_amount=0.4, bone="Body", name="Fang")
        D.add_tooth(b, S.flip(before.lerp(tip, 0.4) + G(-0.012, -0.004, 0), sx), S.flip(G(-1.0, -0.5, 0.1).normalized(), sx),
                    0.006, 0.026, fang[1], hook=S.flip(G(0, -1, 0), sx), hook_amount=0.3, bone="Body", lod=False,
                    name="Tooth")
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

OLD_EYES = [(c, r * 1.3) for c, r in eye_beads()]


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
    out[1] = ("face_macro", "Idle", 15, "Body", BS.hs(G(-0.03, 0.27, 0.41)), "close", dict(distance=0.28, azimuth=-30,
                                                                                    elevation=30))
    out[5] = ("mouth", "Idle", 15, "Body", BS.MOUTH_C, "close", dict(distance=0.3, azimuth=-10, elevation=-2))
    return out


CLIP_FOCUS = ("Body", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_stinger.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
