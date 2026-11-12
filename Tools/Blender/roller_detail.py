"""Round 46: the Roller's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Roller.fbx and Roller_LOD1.fbx:
- Roller_Face (Roller_Face_Mat): two wet amber eyes per side tucked under the rim of the front scoop (where the
  round-4 sensor pits glow; slit pupils, sand-chitin brow folds), and saw teeth down the inner edge of both hooked
  mandibles (they open with the jaws).
- Roller_Claws (Roller_Claws_Mat): dark keratin digging talons capping the three claws of each front foot, and a
  pair of hooked talons at each rear foot.
No bone, clip or event changes; the actions are exported again as they are. In the ball the eyes and talons are
tucked under the plates like the head and legs.

Run after build_roller.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/roller_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Roller"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): sand plates over a dark body, amber eyes and glow.
EYE = D.Eye(lens=(0.03, 0.014, 0.004), iris=(1.0, 0.78, 0.3), rim=(0.3, 0.12, 0.02), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.62, 0.18), vein=(0.35, 0.1, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.06, 0.035, 0.018), (0.6, 0.45, 0.25), (0.16, 0.07, 0.02)
TOOTH = D.Keratin(root=(0.2, 0.1, 0.03), body=(0.86, 0.78, 0.6), tip=(0.98, 0.95, 0.85), glow=(1.0, 0.6, 0.16),
                  stain=(0.25, 0.12, 0.03))
TALON = D.Keratin(root=(0.05, 0.03, 0.015), body=(0.16, 0.09, 0.04), tip=(0.95, 0.82, 0.55), glow=(1.0, 0.6, 0.16),
                  stain=(0.08, 0.04, 0.02))

HEAD_FOCUS = G(0, 0.13, 0.7)
FRONT_FOOT = G(-0.42, 0.02, 0.36)


def jaw_points(sx):
    return [G(0.05 * sx, 0.1, 0.68), G(0.115 * sx, 0.085, 0.78), G(0.085 * sx, 0.075, 0.88), G(0.018 * sx, 0.08, 0.93)]


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.28, glow=0.6, seed=s) for s in range(2)]
    for k, (x, up, fwd, r) in enumerate(((0.05, 0.16, 0.66, 0.024), (0.07, 0.17, 0.6, 0.017))):
        look = Vector((1.0, -0.6 + 0.3 * k, 0.25)).normalized()
        p, n = c.out(G(x * 0.4, up, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.25, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.35, 0.7, 0.6), lower=(1.15, 0.42, 0.45),
                          segments=(20, 10), lid_segments=(14, 7))
    tooth = D.tooth_style(TOOTH, glow=0.35, seed=4)
    for side, sx in SIDES:
        crv = D.curve_points(jaw_points(sx), 30)
        placed = D.edge_saw(c, b, crv, (0.3, 0.45, 0.6, 0.75), Vector((-sx, 0, 0)), 0.0065, 0.032, tooth,
                            f"Jaw_{side}", rake=0.45, max_off=0.05, lod=False, start=0.05, name="Tooth")
        C.log("jaw teeth", side, placed)
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.6, glow_from=0.8, seed=s, wet=0.3) for s in range(3)]
    # Right side, mirrored (the body is mirrored).
    foot = c.tail("Leg_R1_Tibia")
    for k, a in enumerate((-0.5, 0.0, 0.5)):
        fwd = G(0.35 * a + 0.1, 0, 1).normalized()
        tip = foot + fwd * 0.075 - Vector((0, 0, 0.006))
        d = (fwd + Vector((0, 0, -0.35))).normalized()
        for side, sx in SIDES:
            D.add_claw(b, S.flip(tip, sx), S.flip(d, sx), 0.013, 0.06, talon[k], hook=Vector((0, 0, -1)),
                       hook_amount=0.6, bone=f"Leg_{side}1_Tibia", name="Talon", lod=k == 1)
    knee, foot = c.head("Leg_R2_Tibia"), c.tail("Leg_R2_Tibia")
    down = (foot - knee).normalized()
    for k, spread in enumerate((1.0, -1.0)):
        base = foot - down * 0.012 + Vector((0, 0, 0.004))
        d = (down * 0.3 + G(0.25, 0, spread * 0.8)).normalized()
        for side, sx in SIDES:
            D.add_claw(b, S.flip(base, sx), S.flip(d, sx), 0.009, 0.04, talon[k], hook=Vector((0, 0, -1)),
                       hook_amount=0.6, bone=f"Leg_{side}2_Tibia", name="Talon", lod=k == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.58), int(lod1 * 0.5), {"Eye": 2.2, "Tooth": 1.3}),
             "Claws": (claws, int(lod0 * 0.38), int(lod1 * 0.48), {"Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: wet amber eyes under the scoop, mandible saw teeth, keratin "
                                        "digging talons; bones, clips and events unchanged")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L1_Tibia", FRONT_FOOT, "Leg_L2_Tibia", c.tail("Leg_L2_Tibia"),
                           1.2, face_dist=0.9, claw_dist=0.5, foot_dist=0.45)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.05, 0.16, 0.64), "close", dict(distance=0.3, azimuth=-50,
                                                                                    elevation=12))
    # The rolled-up ball: everything tucked away.
    out.append(("ball", "Roll", 6, "Root", Vector((0, 0, 0.3)), "close", dict(distance=1.6, azimuth=-30,
                                                                              elevation=20)))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_roller.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
