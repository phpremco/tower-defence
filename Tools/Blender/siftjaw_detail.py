"""Round 46: the Siftjaw's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Siftjaw.fbx and Siftjaw_LOD1.fbx:
- Siftjaw_Face (Siftjaw_Face_Mat): the row of five red eye beads becomes five wet red eyes with slit pupils under
  ochre brow folds (the round-15 beads' glow goes from the body maps), a second row of keratin teeth between the
  hooks on the inner edge of both scoop jaws (they open and close with the jaws) and a ring of small teeth raked
  down the glowing gullet.
- Siftjaw_Claws (Siftjaw_Claws_Mat): two long keratin digging claws between the hooks on the front edge of each
  spade, and a pair of hooked talons at the end of the four rear legs.
No bone, clip or event changes (Burrow and Emerge untouched); the actions are exported again as they are.

Run after build_siftjaw.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/siftjaw_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Siftjaw"
PARTS = ("Face", "Claws")
VOXEL = 0.0018

SIDES = (("L", -1), ("R", 1))
SPADE = (0.245, 0.055, 0.42)

# Colours (sRGB): ochre sand plates, red-orange seams and eyes.
EYE = D.Eye(lens=(0.035, 0.006, 0.004), iris=(1.0, 0.32, 0.16), rim=(0.3, 0.03, 0.02), pupil=(0.005, 0.0, 0.0),
            glow=(1.0, 0.33, 0.12), vein=(0.35, 0.05, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.08, 0.05, 0.025), (0.42, 0.28, 0.13), (0.2, 0.06, 0.03)
TOOTH = D.Keratin(root=(0.25, 0.12, 0.05), body=(0.86, 0.76, 0.56), tip=(0.98, 0.94, 0.84), glow=(1.0, 0.33, 0.1),
                  stain=(0.3, 0.1, 0.04))
CLAW = D.Keratin(root=(0.08, 0.05, 0.025), body=(0.25, 0.16, 0.08), tip=(0.95, 0.85, 0.65), glow=(1.0, 0.33, 0.1),
                 stain=(0.12, 0.06, 0.03))

HEAD_FOCUS = G(0, 0.16, 0.38)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def bead(x):
    return G(x, 0.215 - 0.012 * (x > 0.05), 0.33 - 0.01 * (x > 0.05))


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=17)
    eyes = [D.eye_style(EYE, pupil="round", iris_size=0.66, pupil_size=0.3, glow=0.7, seed=s) for s in range(3)]
    head_c = G(0, 0.14, 0.29)
    for k, x in enumerate((0.0, 0.032, 0.062)):
        p = bead(x)
        # Centred on the round-15 bead (dimmed: its dark rim is the socket), small enough that the five stay apart.
        n = ((p - head_c).normalized() + G(0, 0.0, 0.3)).normalized()
        for sx in ((1,) if x == 0 else (1, -1)):
            S.organic_eye(b, S.flip(p + n * 0.004, sx), S.flip(n, sx), (0.0145, 0.013, 0.0125)[k], eyes[k], lid,
                          up=G(0, 0.4, -1.0), bone="Head", lod=k == 0, brow=(1.05, 0.55, 0.5), lower=None,
                          brow_lift=0.55, segments=(18, 9), lid_segments=(12, 6))
    tooth = [D.tooth_style(TOOTH, glow=0.4, seed=s) for s in range(2)]
    for side, sx in SIDES:
        jc = gp((0.075, 0.1, 0.42), sx)
        yaw = Matrix.Rotation(math.radians(16 * sx), 3, "Z")
        for j in range(3):
            # Between the round-15 hooks (they sit at j = 0..3 on -0.045 + 0.022 j, 0.12 - 0.035 j).
            base = jc + yaw @ gp((-0.034 + 0.022 * j, -0.004, 0.103 - 0.035 * j), sx)
            d = gp((-0.8, -0.25, 0.55), sx).normalized()
            D.add_tooth(b, base, d, 0.006, 0.03, tooth[j % 2], hook=gp((-0.3, 0, 1), sx), hook_amount=0.35,
                        bone=f"Jaw_{side}", lod=False, name="Tooth")
    gullet = G(0, 0.1, 0.385)
    D.add_ring_teeth(b, gullet, G(0, 0, -1), 0.03, 8, 0.024, 0.005, tooth[0], inward=0.6, bone="Head", lod=False,
                     name="RingTooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.55, glow_from=0.8, seed=s, wet=0.3) for s in range(2)]
    for side, sx in SIDES:
        sp = gp(SPADE, sx)
        spade_rot = Matrix.Rotation(math.radians(-15 * sx), 3, "Z") @ Matrix.Rotation(math.radians(-35), 3, "X")
        ahead = spade_rot @ Vector((0, -1, 0))
        for j in range(2):
            base = sp + spade_rot @ Vector(((-0.015 + 0.03 * j) * sx, -0.058, 0.0))
            D.add_claw(b, base, (ahead + Vector((0, 0, -0.15))).normalized(), 0.01, 0.06, claw[j],
                       hook=Vector((0, 0, -1)), hook_amount=0.45, bone=f"Arm_{side}_Spade", name="Dig", lod=j == 0)
    for i in (1, 2):
        knee, foot = c.head(f"Leg_R{i}_Lower"), c.tail(f"Leg_R{i}_Lower")
        down = (foot - knee).normalized()
        out = Vector((down.x, down.y, 0)).normalized()
        base = foot - down * 0.015 + Vector((0, 0, 0.003))
        for k, spread in enumerate((1.0, -1.0)):
            d = (out * 0.5 + G(0, 0, spread * 0.85) + Vector((0, 0, -0.1))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(base, sx), S.flip(d, sx), 0.008, 0.036, claw[k], hook=Vector((0, 0, -1)),
                           hook_amount=0.6, bone=f"Leg_{side}{i}_Lower", name="Talon", lod=k == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(S.flip(bead(x), sx), 0.019) for x in (0.0, 0.032, 0.062) for sx in ((1,) if x == 0 else (1, -1))]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.08, 0.05, 0.025), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.58), int(lod1 * 0.5), {"Eye": 2.2, "Tooth": 1.2, "RingTooth": 1.1}),
             "Claws": (claws, int(lod0 * 0.38), int(lod1 * 0.47), {"Dig": 1.2, "Talon": 1.1})}
    return S.build_all(c, specs, VOXEL, "round 46: five wet red eyes, scoop-jaw and gullet teeth, spade digging "
                                        "claws, leg talons; bones, clips and events unchanged",
                       body_maps=f"round-15 bead eyes' glow removed from Siftjaw_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Arm_L_Spade", gp(SPADE, -1), "Leg_L1_Lower", c.tail("Leg_L1_Lower"),
                           1.1, face_dist=0.8, claw_dist=0.4, foot_dist=0.35)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.03, 0.21, 0.33), "close", dict(distance=0.25, azimuth=-30,
                                                                                    elevation=35))
    out.append(("emerge", "Emerge", 20, "Head", HEAD_FOCUS, "close", dict(distance=0.9, azimuth=-30, elevation=15)))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_siftjaw.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
