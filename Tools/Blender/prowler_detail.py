"""Round 46: the Prowler's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Prowler.fbx and Prowler_LOD1.fbx:
- Prowler_Face (Prowler_Face_Mat): two wet red-orange eyes per side at the front of the red sensor slits, sunk
  under brow folds below the horns (slit pupils, the pack's orange glow), and a row of hooked fangs down the inner
  edge of both shear jaws (they open with the jaws).
- Prowler_Claws (Prowler_Claws_Mat): dark keratin talons capping the two hooked toes of every foot (pale, glowing
  tips), so the raised feet end in real hunting claws.
No bone, clip or event changes; the actions are exported again as they are.

Run after build_prowler.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/prowler_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Prowler"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): rust-red plates, red sensor eyes, orange pack glow.
EYE = D.Eye(lens=(0.03, 0.006, 0.004), iris=(1.0, 0.4, 0.16), rim=(0.3, 0.04, 0.02), pupil=(0.006, 0.0, 0.0),
            glow=(1.0, 0.45, 0.16), vein=(0.35, 0.04, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.06, 0.02, 0.012), (0.5, 0.16, 0.07), (0.18, 0.04, 0.02)
FANG = D.Keratin(root=(0.25, 0.06, 0.03), body=(0.86, 0.76, 0.6), tip=(0.98, 0.94, 0.86), glow=(1.0, 0.55, 0.17),
                 stain=(0.3, 0.06, 0.03))
TALON = D.Keratin(root=(0.05, 0.02, 0.012), body=(0.16, 0.06, 0.035), tip=(0.95, 0.8, 0.6), glow=(1.0, 0.55, 0.17),
                  stain=(0.08, 0.02, 0.01))

HEAD_FOCUS = G(0, 0.42, 0.62)


def jaw_points(sx):
    return [G(0.04 * sx, 0.39, 0.53), G(0.078 * sx, 0.375, 0.65), G(0.055 * sx, 0.365, 0.76), G(0.012 * sx, 0.37, 0.83)]


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=7)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.26, glow=0.65, seed=s) for s in range(2)]
    for k, (x, up, fwd, r) in enumerate(((0.04, 0.45, 0.62, 0.021), (0.05, 0.458, 0.555, 0.016))):
        look = Vector((1.0, -0.7 + 0.3 * k, 0.3)).normalized()
        p, n = c.out(G(x * 0.4, up, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.25, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.4, 0.72, 0.6), lower=(1.15, 0.42, 0.45), brow_tilt=0.6,
                          segments=(20, 10), lid_segments=(14, 7))
    fang = D.tooth_style(FANG, glow=0.35, seed=4)
    for side, sx in SIDES:
        crv = D.curve_points(jaw_points(sx), 30)
        placed = D.edge_saw(c, b, crv, (0.28, 0.42, 0.56, 0.7, 0.82), Vector((-sx, 0, 0)), 0.0085, 0.046, fang,
                            f"Jaw_{side}", rake=0.4, max_off=0.05, lod=False, start=0.05, name="Fang")
        C.log("jaw fangs", side, placed)
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.6, glow_from=0.8, seed=s, wet=0.3) for s in range(2)]
    # Right side, mirrored (the body is mirrored): at the end of each toe hook of build_prowler.
    for kind in ("F", "H"):
        toe = c.tail(f"Leg_R{kind}_Foot")
        for k, a in enumerate((-0.35, 0.35)):
            fwd = G(math.sin(a) * 0.7, 0, 1).normalized()
            tip = toe + fwd * 0.048 - Vector((0, 0, 0.0))
            d = (fwd + Vector((0, 0, -0.45))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(tip, sx), S.flip(d, sx), 0.012, 0.058, talon[k], hook=Vector((0, 0, -1)),
                           hook_amount=0.6, bone=f"Leg_{side}{kind}_Foot", name="Talon", lod=k == 1)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.52), {"Eye": 2.2, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.36), int(lod1 * 0.45), {"Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: wet red eyes under brow folds, shear-jaw fangs, toe talons; "
                                        "bones, clips and events unchanged")


def shots(c):
    foot = c.tail("Leg_LF_Foot")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_LF_Foot", foot, "Leg_LH_Foot", c.tail("Leg_LH_Foot"), 1.6,
                           walk="Run", face_dist=0.9, claw_dist=0.45, foot_dist=0.45)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.05, 0.45, 0.6), "close", dict(distance=0.3, azimuth=-55,
                                                                                   elevation=12))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_prowler.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
