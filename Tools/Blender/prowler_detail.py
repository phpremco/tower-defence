"""Round 46: the Prowler's close-up detail (Remco: "do the same for the small enemies"), re-fitted in round 49A to the
alien-wolf body (wolf_kit.py) and in round 49E to its rebuilt skull (eyes under the heavy brow, the upper teeth in the
gum line under the snarling lip: wolf_kit.Wolf.eye_points / upper_teeth / lower_teeth).

Two extra skinned parts (detail_kit / detail_kit_small), exported in Prowler.fbx and Prowler_LOD1.fbx:
- Prowler_Face (Prowler_Face_Mat): two wet red-orange eyes per side in a row under the brow ridge (slit pupils, the
  pack's orange glow), upper fangs under the muzzle (a canine in front) and lower fangs on both mandible halves
  (they open with the split jaw).
- Prowler_Claws (Prowler_Claws_Mat): dark keratin talons on the four toes of every paw (pale, glowing tips).

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
import wolf_kit as W  # noqa: E402
from common import G  # noqa: E402

NAME = "Prowler"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): red sensor eyes, orange pack glow; stained ivory fangs, dark talons with pale points.
EYE = D.Eye(lens=(0.03, 0.006, 0.004), iris=(1.0, 0.4, 0.16), rim=(0.3, 0.04, 0.02), pupil=(0.006, 0.0, 0.0),
            glow=(1.0, 0.45, 0.16), vein=(0.35, 0.04, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.06, 0.02, 0.012), (0.5, 0.16, 0.07), (0.18, 0.04, 0.02)
FANG = D.Keratin(root=(0.3, 0.1, 0.07), body=(0.8, 0.72, 0.58), tip=(0.95, 0.92, 0.84), glow=(1.0, 0.55, 0.17),
                 stain=(0.35, 0.18, 0.1))
TALON = D.Keratin(root=(0.05, 0.02, 0.012), body=(0.16, 0.06, 0.035), tip=(0.95, 0.8, 0.6), glow=(1.0, 0.55, 0.17),
                  stain=(0.08, 0.02, 0.01))

HEAD_FOCUS = G(0, 0.58, 0.62)
UP = Vector((0, 0, 1))
FWD = G(0, 0, 1)
WOLF = W.Wolf(1.0)


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    """Round 49E (the rebuilt wolf skull): two wet eyes per side under the heavy brow, the upper teeth rooted in the
    gum line under the snarling lip (a long canine in front, carnassials behind, small incisors), the lower fangs
    on both mandible halves (they open with the split jaw)."""
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=7)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.26, glow=0.65, seed=s) for s in range(2)]
    for k, ((centre, r), look) in enumerate(zip(WOLF.eye_points(), ((1.0, -0.55, 0.12), (1.0, -0.25, 0.2)))):
        look = Vector(look).normalized()
        p, n = c.out(Vector((centre.x * 0.3, centre.y, centre.z)), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.3, sx), S.flip(n, sx), r, eyes[k], lid, up=UP,
                          bone="Head", lod=k == 0, brow=(1.45, 0.75, 0.62), lower=(1.15, 0.42, 0.45), brow_tilt=0.7,
                          segments=(20, 10), lid_segments=(14, 7))
    fang = D.tooth_style(FANG, glow=0.3, seed=4)
    for side, sx in SIDES:
        for base, d, length, rad, lod in WOLF.upper_teeth(sx):
            D.add_tooth(b, base, d, rad, length, fang, hook=-FWD, hook_amount=0.22, bone="Head", lod=lod, name="Fang")
        for base, d, length, rad, lod in WOLF.lower_teeth(sx):
            D.add_tooth(b, base, d, rad, length, fang, hook=-FWD, hook_amount=0.22, bone=f"Jaw_{side}", lod=lod,
                        name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    """A dark talon at the end of each of the four toes of every paw (wolf_kit's toe layout)."""
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.6, glow_from=0.8, seed=s, wet=0.3) for s in range(2)]
    for kind in ("F", "H"):
        toe = c.tail(f"Leg_R{kind}_Foot")
        base = toe + FWD * 0.02 + UP * 0.014
        for k, ang in enumerate((-0.5, -0.17, 0.17, 0.5)):
            d = G(math.sin(ang) * 0.6, 0, 1).normalized()
            tip = base + d * 0.056 - UP * 0.006
            dd = (d + Vector((0, 0, -0.55))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(tip, sx), S.flip(dd, sx), 0.0085, 0.04, talon[k % 2], hook=Vector((0, 0, -1)),
                           hook_amount=0.6, bone=f"Leg_{side}{kind}_Foot", name="Talon", lod=k in (1, 2))
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
    return S.build_all(c, specs, VOXEL, "round 49A: four wet red eyes under the brow ridge, upper and split-jaw fangs, paw talons "
                                        "(round 46 parts re-fitted to the alien-wolf body)")


def shots(c):
    foot = c.tail("Leg_LF_Foot")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_LF_Foot", foot, "Leg_LH_Foot", c.tail("Leg_LH_Foot"), 1.6,
                           walk="Run", face_dist=0.9, claw_dist=0.45, foot_dist=0.45)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.05, 0.6, 0.62), "close", dict(distance=0.3, azimuth=-55,
                                                                                   elevation=12))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_prowler.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
