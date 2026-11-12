"""Round 46: the Wisp's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Wisp.fbx and Wisp_LOD1.fbx:
- Wisp_Face (Wisp_Face_Mat): the heart looks out: a wet, ice-white eye with a slit pupil bulges into the front seam
  and into each side seam of the closed bud, the petal edges closing over its rim like lids; and hooked teeth along
  the side edges of every petal, raking into the seams, so the bud reads as a closed fly-trap maw (and bares them
  when the petals burst open in Death).
- Wisp_Claws (Wisp_Claws_Mat): a hooked keratin talon under each of the three tendril tips (they plant as a tripod
  in Land/Grounded).
No bone, clip or event changes; the actions are exported again as they are.

Run after build_wisp.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/wisp_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Wisp"
PARTS = ("Face", "Claws")
VOXEL = 0.0018

FLOAT_UP = 0.42
CENTRE = G(0, FLOAT_UP, 0)
BUD_R = 0.155
PETAL_HW = math.radians(40)
Z = Vector((0, 0, 1))

# Colours (sRGB): umber-violet flesh and petals, ice-blue light, white eyes.
EYE = D.Eye(lens=(0.006, 0.008, 0.02), iris=(0.82, 0.92, 1.0), rim=(0.15, 0.2, 0.35), pupil=(0.004, 0.0, 0.008),
            glow=(0.6, 0.8, 1.0), vein=(0.3, 0.12, 0.25))
TOOTH = D.Keratin(root=(0.2, 0.1, 0.16), body=(0.75, 0.7, 0.72), tip=(0.95, 0.97, 1.0), glow=(0.5, 0.74, 1.0),
                  stain=(0.2, 0.08, 0.15))
TALON = D.Keratin(root=(0.06, 0.035, 0.05), body=(0.2, 0.12, 0.17), tip=(0.85, 0.92, 1.0), glow=(0.5, 0.74, 1.0),
                  stain=(0.08, 0.04, 0.06))

HEAD_FOCUS = CENTRE + G(0, 0, 0.05)


def flat_dir(az):
    return G(math.sin(az), 0, math.cos(az))


def bud_point(az, elev, r):
    return CENTRE + flat_dir(az) * (r * math.cos(elev)) + Z * (r * math.sin(elev))


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.58, pupil_size=0.3, glow=0.55, seed=s) for s in range(2)]
    # Eyes in the seams between the petals (seams at 0, 90, 180, 270 degrees): front and both sides. The petal
    # edges overlap the lens rim, so the bud itself is the lid.
    for k, (az, r) in enumerate(((0.0, 0.034), (math.pi / 2, 0.027))):
        n = (flat_dir(az) + Z * 0.15).normalized()
        p = bud_point(az, 0.15, BUD_R * 0.86)
        for sx in ((1,) if az == 0.0 else (1, -1)):
            D.add_eye(b, S.flip(p, sx), S.flip(n, sx), r, eyes[k], None, squash=(0.75, 1.0, 0.8), socket=0,
                      up=Z, bone="Core", lod=True, segments=(20, 12))
    # Fly-trap teeth on both side edges of every petal, raking into the seams, above and below the eyes
    # (right-hand petals; mirrored).
    tooth = [D.tooth_style(TOOTH, glow=0.5, seed=s) for s in range(2)]
    for i, az0 in enumerate((math.radians(45), math.radians(135))):
        for edge in (-1, 1):
            for j, (elev, ln) in enumerate(((-0.45, 0.042), (0.62, 0.04))):
                hw = PETAL_HW * (1 - 0.4 * max(0.0, (elev + 1.15) / 2.4 - 0.7) / 0.3) * 0.96
                base = bud_point(az0 + edge * hw, elev, BUD_R * 1.0)
                seam = flat_dir(az0 + edge * (hw + 0.4)) - flat_dir(az0 + edge * hw)
                d = (seam.normalized() * 0.65 + (base - CENTRE).normalized() * 0.55).normalized()
                for sx in (1, -1):
                    bone = f"Petal_{i + 1}" if sx > 0 else f"Petal_{4 - i}"
                    D.add_tooth(b, S.flip(base, sx), S.flip(d, sx), 0.0075, ln, tooth[j % 2],
                                hook=S.flip((CENTRE - base).normalized(), sx), hook_amount=0.3, bone=bone,
                                lod=False, name="Tooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = D.keratin_style(TALON, glow=0.7, glow_from=0.75, seed=3, wet=0.35)
    for i, sxs in ((1, (1,)), (2, (1, -1))):
        foot = c.tail(f"Tendril_{i}_2")
        knee = c.head(f"Tendril_{i}_2")
        down = (foot - knee).normalized()
        out = Vector((down.x, down.y, 0)).normalized()
        d = (down * 0.5 + out * 0.5).normalized()
        for sx in sxs:
            bone = f"Tendril_{i}_2" if sx > 0 else "Tendril_3_2"
            D.add_claw(b, S.flip(foot - down * 0.03, sx), S.flip(d, sx), 0.011, 0.055, talon,
                       hook=S.flip(-out + Vector((0, 0, -0.5)), sx), hook_amount=0.55, bone=bone, name="Talon")
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.7), int(lod1 * 0.5), {"Eye": 2.6, "Tooth": 1.1}),
             "Claws": (claws, int(lod0 * 0.26), int(lod1 * 0.3), {"Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: eyes in the bud seams, fly-trap petal teeth, tendril talons; "
                                        "bones, clips and events unchanged")


def shots(c):
    foot = c.tail("Tendril_1_2")
    out = S.standard_shots("Core", HEAD_FOCUS, "Tendril_1_2", foot, "Tendril_1_2", foot, 1.0, walk="Float",
                           idle="Float", face_dist=1.3, claw_dist=0.35, foot_dist=0.35,
                           root_focus=Vector((0, 0, 0.3)))
    out[1] = ("face_macro", "Float", 15, "Core", bud_point(0.0, 0.15, BUD_R), "close", dict(distance=0.3,
                                                                                            azimuth=-25,
                                                                                            elevation=10))
    out[5] = ("death", "Death", 12, "Core", HEAD_FOCUS, "close", dict(distance=0.9, azimuth=-30, elevation=25))
    out.append(("grounded", "Grounded", 20, "Root", Vector((0, 0, 0.15)), "close", dict(distance=1.0, azimuth=-35,
                                                                                         elevation=15)))
    return out


CLIP_FOCUS = ("Core", HEAD_FOCUS, 1.0)


def run(norender=False):
    """build_wisp.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
