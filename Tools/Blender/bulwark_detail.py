"""Round 46: the Bulwark's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Bulwark.fbx and Bulwark_LOD1.fbx:
- Bulwark_Face (Bulwark_Face_Mat): two wet amber eyes per side at the front of the amber eye slits, deep under the
  bony visor (its rim is the brow; olive lid folds below), and a row of hooked teeth up from the lower jaw's lip and
  down from the upper lip, so the dropped jaw shows a mouthful of fangs round the glowing throat.
- Bulwark_Claws (Bulwark_Claws_Mat): bone-coloured keratin talons capping the three hooked toes of each foot
  (dark roots, pale points with a faint lime glow).
No bone, clip or event changes; the actions are exported again as they are. build_crystalwarlord.py reuses
build_bulwark's geometry, not this file: the CrystalWarlord is unaffected.

Run after build_bulwark.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/bulwark_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Bulwark"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): olive scales, bone plates, amber eyes, lime goo.
EYE = D.Eye(lens=(0.03, 0.016, 0.004), iris=(1.0, 0.72, 0.22), rim=(0.3, 0.12, 0.02), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.65, 0.2), vein=(0.3, 0.12, 0.03))
LID_DARK, LID_SKIN, LID_WET = (0.035, 0.045, 0.02), (0.25, 0.33, 0.13), (0.12, 0.1, 0.03)
FANG = D.Keratin(root=(0.2, 0.22, 0.08), body=(0.82, 0.78, 0.62), tip=(0.97, 0.95, 0.85), glow=(0.62, 1.0, 0.22),
                 stain=(0.25, 0.3, 0.06))
TALON = D.Keratin(root=(0.05, 0.06, 0.03), body=(0.3, 0.3, 0.2), tip=(0.95, 0.92, 0.78), glow=(0.62, 1.0, 0.22),
                  stain=(0.1, 0.12, 0.04))

HEAD_FOCUS = G(0, 0.88, 0.38)


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=10)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.27, glow=0.75, seed=s) for s in range(2)]
    for k, (x, up, fwd, r) in enumerate(((0.05, 0.905, 0.39, 0.019), (0.064, 0.905, 0.33, 0.015))):
        look = Vector((1.0, -0.75 + 0.3 * k, 0.05)).normalized()
        p, n = c.out(G(x * 0.4, up, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            # The visor's rim is the brow: only a lower lid fold, plus a small upper fold tucked under the visor.
            S.organic_eye(b, S.flip(p - n * r * 0.2, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.2, 0.5, 0.5), lower=(1.15, 0.42, 0.45), brow_lift=0.45,
                          segments=(20, 10), lid_segments=(14, 7))
    fang = [D.tooth_style(FANG, glow=0.35, seed=s) for s in range(2)]
    # Lower teeth on the jaw's lip (bone Jaw) pointing up, upper teeth under the head pointing down; mirrored.
    for rows, up, bone, d, hook in ((((0.02, 0.03), (0.04, 0.026)), 0.852, "Jaw", G(0, 1.0, 0.2), G(0, 0.3, -0.5)),
                                    (((0.025, 0.032), (0.045, 0.026)), 0.868, "Head", G(0, -1.0, 0.2), G(0, -0.3, -0.5))):
        for k, (x, ln) in enumerate(rows):
            hit = c.hit(G(x, up, 0.9), Vector((0, 1, 0)))
            if hit is None:
                C.log("tooth skipped", bone, x)
                continue
            p, n = hit
            for sx in (1, -1):
                D.add_tooth(b, S.flip(p, sx) + Vector((0, 0.01, 0)), S.flip(d.normalized(), sx), 0.0065, ln, fang[k],
                            hook=hook, hook_amount=0.25, bone=bone, lod=False, name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.45, glow_from=0.82, seed=s, wet=0.3) for s in range(3)]
    toe = c.tail("Leg_R_Foot")
    for k, a in enumerate((-0.45, 0.0, 0.45)):
        fwd = G(math.sin(a) * 0.8, 0, 1).normalized()
        tip = toe + fwd * 0.07 - Vector((0, 0, 0.004))
        d = (fwd + Vector((0, 0, -0.45))).normalized()
        for side, sx in SIDES:
            D.add_claw(b, S.flip(tip, sx), S.flip(d, sx), 0.017, 0.072, talon[k], hook=Vector((0, 0, -1)),
                       hook_amount=0.6, bone=f"Leg_{side}_Foot", name="Talon", lod=k == 1)
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
    return S.build_all(c, specs, VOXEL, "round 46: wet amber eyes under the visor, jaw fangs, toe talons; bones, "
                                        "clips and events unchanged")


def shots(c):
    foot = c.tail("Leg_L_Foot")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L_Foot", foot, "Leg_L_Foot", foot, 1.4, face_dist=0.8,
                           claw_dist=0.45, foot_dist=0.35, root_focus=Vector((0, 0, 0.5)))
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.05, 0.9, 0.38), "close", dict(distance=0.28, azimuth=-45,
                                                                                   elevation=4))
    out[5] = ("mouth", "Hit", 4, "Head", G(0, 0.85, 0.4), "close", dict(distance=0.4, azimuth=-15, elevation=0))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_bulwark.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
