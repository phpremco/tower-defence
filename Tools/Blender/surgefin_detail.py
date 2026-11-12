"""Round 46: the Surgefin's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Surgefin.fbx and Surgefin_LOD1.fbx:
- Surgefin_Face (Surgefin_Face_Mat): the four coral eye beads become wet, bulging coral eyes with slit pupils under
  teal lid folds (the round-15 beads' glow goes from the body maps), and a dense row of glassy needle teeth along
  the whole lip, both down from the upper lip and up from the lower, over the glowing mouth seam (an angler's grin).
- Surgefin_Claws (Surgefin_Claws_Mat): hooked keratin points on the three toes of every paddle foot.
No bone, clip or event changes (Swim and Emerge untouched); the actions are exported again as they are.

Run after build_surgefin.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/surgefin_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Surgefin"
PARTS = ("Face", "Claws")
VOXEL = 0.0018

SIDES = (("L", -1), ("R", 1))
EYES = (((0.055, 0.172, 0.4), 0.017), ((0.085, 0.16, 0.35), 0.012))

# Colours (sRGB): wet petrol-teal skin, coral light.
EYE = D.Eye(lens=(0.035, 0.012, 0.008), iris=(1.0, 0.55, 0.3), rim=(0.3, 0.06, 0.04), pupil=(0.005, 0.0, 0.0),
            glow=(1.0, 0.48, 0.26), vein=(0.3, 0.06, 0.05))
LID_DARK, LID_SKIN, LID_WET = (0.015, 0.04, 0.045), (0.06, 0.2, 0.21), (0.1, 0.12, 0.12)
NEEDLE = D.Keratin(root=(0.2, 0.25, 0.26), body=(0.78, 0.84, 0.84), tip=(0.97, 0.99, 1.0), glow=(1.0, 0.45, 0.24),
                   stain=(0.15, 0.2, 0.2))
CLAW = D.Keratin(root=(0.02, 0.05, 0.055), body=(0.08, 0.16, 0.17), tip=(0.95, 0.8, 0.7), glow=(1.0, 0.45, 0.24),
                 stain=(0.03, 0.06, 0.06))

HEAD_FOCUS = G(0, 0.13, 0.44)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=18, rough=0.3)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.27, glow=0.8, seed=s) for s in range(2)]
    head_c = G(0, 0.125, 0.38)
    for k, (p, r) in enumerate(EYES):
        bead = gp(p, 1)
        n = ((bead - head_c).normalized() + G(0.2, 0.3, 0.4)).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(bead + n * 0.003, sx), S.flip(n, sx), r * 1.2, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.2, 0.62, 0.55), lower=(1.1, 0.42, 0.45),
                          segments=(20, 10), lid_segments=(12, 6))
    needle = [D.tooth_style(NEEDLE, glow=0.35, seed=s) for s in range(2)]
    # Needle teeth round the lip, between the round-15 teeth (at x 0.02, 0.05, 0.08): down from the upper lip and up
    # from the lower; the lip curves back towards the corners.
    for j, x in enumerate((0.006, 0.035, 0.065, 0.092)):
        fwd = 0.493 - 0.022 * (x / 0.03) ** 2
        for up, d, ln in ((0.112, G(0, -1.0, 0.25), 0.024), (0.096, G(0, 1.0, 0.25), 0.02)):
            hit = c.hit(G(x, up, fwd + 0.2), Vector((0, 1, 0)))
            if hit is None:
                C.log("needle skipped", x, up)
                continue
            p, n = hit
            for sx in ((1, -1) if x > 0.01 else (1, -1)):
                D.add_tooth(b, S.flip(p, sx) + Vector((0, 0.006, 0)), S.flip(d.normalized(), sx), 0.0042,
                            ln * (1 - 0.25 * j / 3), needle[j % 2], hook=G(0, 0, -1), hook_amount=0.3, bone="Head",
                            lod=False, name="Needle")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.5, glow_from=0.8, seed=s, wet=0.4) for s in range(3)]
    for i in (1, 2):
        foot = c.tail(f"Leg_R{i}_Lower")
        pad = foot + gp((0.015, 0.01, 0.025), 1)
        for j, a in enumerate((-0.6, 0.0, 0.6)):
            d = gp((math.sin(a) + 0.3, 0.0, math.cos(a)), 1).normalized()
            tip = pad + d * 0.06 - Vector((0, 0, 0.005))
            out = (d + Vector((0, 0, -0.2))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(tip - out * 0.01, sx), S.flip(out, sx), 0.0055, 0.024, claw[j],
                           hook=Vector((0, 0, -1)), hook_amount=0.6, bone=f"Leg_{side}{i}_Lower", name="Claw",
                           lod=j == 1)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(gp(p, sx), r * 1.3) for p, r in EYES for sx in (1, -1)]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.015, 0.04, 0.045), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.5), {"Eye": 2.2, "Needle": 1.2}),
             "Claws": (claws, int(lod0 * 0.36), int(lod1 * 0.47), {"Claw": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: four wet coral eyes under lid folds, needle-tooth grin, toe "
                                        "claws; bones, clips and events unchanged",
                       body_maps=f"round-15 bead eyes' glow removed from Surgefin_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    foot = c.tail("Leg_L1_Lower")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L1_Lower", foot, "Leg_L2_Lower", c.tail("Leg_L2_Lower"), 1.2,
                           face_dist=0.75, claw_dist=0.35, foot_dist=0.3)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.06, 0.16, 0.42), "close", dict(distance=0.25, azimuth=-35,
                                                                                    elevation=15))
    out.append(("mouth", "Idle", 15, "Head", G(0, 0.1, 0.48), "close", dict(distance=0.3, azimuth=-8, elevation=4)))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.8)


def run(norender=False):
    """build_surgefin.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
