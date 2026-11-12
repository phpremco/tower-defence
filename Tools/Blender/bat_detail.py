"""Round 46: the element Bat's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Bat.fbx and Bat_LOD1.fbx:
- Bat_Face (Bat_Face_Mat): the four pin-head eye beads on each side become two wet, almond eyes with slit pupils
  under brow folds (one over each pair of beads), and needle teeth on the lip of the dropping jaw and under the
  upper lip, round the glowing bolt throat.
- Bat_Claws (Bat_Claws_Mat): hooked keratin points on the two belly talons, the wrist thumbs and the three wing-spine
  hooks of each wing.
One mesh serves every element skin (Ember default, Moss, Dust, Tide, Frost, Mire, Corrupted): the parts are
coloured element-neutral (cream eyes with a warm glow, ivory teeth, dark keratin), since a baked skin has no maps of
its own for the parts and only tints them (BlenderArtEntry.PartTint / PartGlowTint). The body maps are unchanged
(the beads glow on under the new eyes in every skin). No bone, clip or event changes.

Run after build_bat.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/bat_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Bat"
PARTS = ("Face", "Claws")
VOXEL = 0.0014
GROUND = -0.5

SIDES = (("L", -1), ("R", 1))
WRIST = (0.3, 0.09, 0.08)
TIPS = ((0.74, 0.06, 0.03), (0.66, 0.02, -0.17), (0.42, 0.0, -0.3))

# Element-neutral colours (sRGB): every skin's eyes are pale and warm-lit.
EYE = D.Eye(lens=(0.03, 0.025, 0.015), iris=(1.0, 0.92, 0.66), rim=(0.28, 0.18, 0.08), pupil=(0.005, 0.003, 0.0),
            glow=(1.0, 0.82, 0.5), vein=(0.3, 0.12, 0.06))
LID_DARK, LID_SKIN, LID_WET = (0.04, 0.03, 0.03), (0.24, 0.18, 0.15), (0.16, 0.07, 0.05)
TOOTH = D.Keratin(root=(0.25, 0.15, 0.1), body=(0.85, 0.8, 0.7), tip=(0.98, 0.96, 0.9), glow=(1.0, 0.7, 0.4),
                  stain=(0.3, 0.12, 0.06))
TALON = D.Keratin(root=(0.04, 0.03, 0.03), body=(0.14, 0.1, 0.09), tip=(0.92, 0.85, 0.72), glow=(1.0, 0.7, 0.4),
                  stain=(0.06, 0.04, 0.03))

HEAD_FOCUS = G(0, 0.03, 0.2)


def bead(i):
    return G(0.045 + 0.008 * i, 0.045 - 0.004 * i, 0.215 - 0.025 * i)


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=14)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.28, glow=0.75, seed=s) for s in range(2)]
    head_c = G(0, 0.03, 0.16)
    # One almond eye over each pair of beads (stretched along the head), right side, mirrored.
    for k, (i, r) in enumerate(((0, 0.0155), (2, 0.0135))):
        p = bead(i).lerp(bead(i + 1), 0.5)
        n = ((p - head_c).normalized() + G(0.3, 0.2, 0.3)).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p + n * 0.003, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, squash=(1.35, 0.8, 0.7), brow=(1.15, 0.6, 0.55),
                          lower=(1.05, 0.38, 0.45), segments=(20, 10), lid_segments=(12, 6))
    tooth = D.tooth_style(TOOTH, glow=0.4, seed=7)
    for rows, up, bone, d, hook in ((((0.012, 0.022), (0.026, 0.018)), -0.003, "Jaw", G(0, 1.0, 0.2), G(0, 0.3, -0.5)),
                                    (((0.016, 0.024), (0.03, 0.018)), 0.016, "Head", G(0, -1.0, 0.2), G(0, -0.3, -0.5))):
        for x, ln in rows:
            hit = c.hit(G(x, up, 0.6), Vector((0, 1, 0)))
            if hit is None:
                C.log("tooth skipped", bone, x)
                continue
            p, n = hit
            for sx in (1, -1):
                D.add_tooth(b, S.flip(p, sx) + Vector((0, 0.006, 0)), S.flip(d.normalized(), sx), 0.0045, ln, tooth,
                            hook=hook, hook_amount=0.25, bone=bone, lod=False, name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.5, glow_from=0.8, seed=s, wet=0.3) for s in range(2)]
    for side, sx in SIDES:
        # Belly talon tip (build_bat: root + (0, -0.06, 0.04)).
        tip = G(0.04 * sx, -0.115, 0.01)
        D.add_claw(b, tip + G(0, 0.01, -0.008), G(0, -0.5, 1.0).normalized(), 0.008, 0.035, talon[0],
                   hook=G(0, 1.0, 0.2), hook_amount=0.55, bone="Body", name="Talon")
        wrist = G(WRIST[0] * sx, WRIST[1], WRIST[2])
        D.add_claw(b, wrist + G(-0.01 * sx, 0.0, 0.065), G(-0.2 * sx, -0.1, 1.0).normalized(), 0.007, 0.032, talon[1],
                   hook=Vector((0, 0, -1)), hook_amount=0.6, bone=f"Hand_{side}", name="Thumb")
        for i, t in enumerate(TIPS):
            p = G(t[0] * sx, t[1], t[2])
            out = (p - wrist).normalized()
            D.add_claw(b, p + out * 0.02 - Vector((0, 0, 0.012)), (out + Vector((0, 0, -0.6))).normalized(), 0.006,
                       0.03, talon[1], hook=Vector((0, 0, -1)), hook_amount=0.5, bone=f"Finger_{side}{i + 1}",
                       name="Hook", lod=i == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.56), int(lod1 * 0.5), {"Eye": 2.4, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.4), int(lod1 * 0.47), {"Talon": 1.2, "Thumb": 1.2, "Hook": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: element-neutral wet eyes under brow folds, needle teeth, keratin "
                                        "talons and hooks (skins tint them); bones, clips and events unchanged")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Hand_L", G(-0.3, 0.09, 0.12), "Body", G(-0.04, -0.11, 0.01), 0.9,
                           walk="Fly", idle="Glide", face_dist=0.55, claw_dist=0.35, foot_dist=0.3,
                           root_focus=Vector((0, 0, 0)))
    out[1] = ("face_macro", "Glide", 15, "Head", G(-0.055, 0.045, 0.2), "close", dict(distance=0.18, azimuth=-60,
                                                                                      elevation=10))
    out.append(("mouth", "Hit", 4, "Head", G(0, 0.0, 0.24), "close", dict(distance=0.3, azimuth=-12, elevation=-4)))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.6)


def run(norender=False):
    """build_bat.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender, ground_z=GROUND)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS, ground_z=GROUND)
