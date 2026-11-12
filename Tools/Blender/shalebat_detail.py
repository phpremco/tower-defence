"""Round 46: the Shalebat's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Shalebat.fbx and Shalebat_LOD1.fbx:
- Shalebat_Face (Shalebat_Face_Mat): the three amber bead eyes on each side of the wedge become wet amber eyes
  with slit pupils, sunk under shale lids below the brow slab (the round-5 beads' glow goes from the body maps), and
  stone fangs up from the lip of the dropping lower jaw and down from the upper lip, hot at the roots.
- Shalebat_Claws (Shalebat_Claws_Mat): keratin hooks on the belly talons and a hooked claw at the tip of every
  shale wing blade (dark roots, ember tips).
No bone, clip or event changes; the actions are exported again as they are.

Run after build_shalebat.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/shalebat_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Shalebat"
PARTS = ("Face", "Claws")
VOXEL = 0.0016
GROUND = -0.5

SIDES = (("L", -1), ("R", 1))
EYES = ((0.068, 0.07, 0.24), (0.074, 0.055, 0.205), (0.072, 0.075, 0.175))
TIPS = ((0.8, 0.07, 0.0), (0.74, 0.03, -0.2), (0.5, 0.0, -0.33))

# Colours (sRGB): slate stone, amber eyes, ember glow.
EYE = D.Eye(lens=(0.03, 0.014, 0.004), iris=(1.0, 0.66, 0.22), rim=(0.3, 0.1, 0.02), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.58, 0.18), vein=(0.35, 0.1, 0.03))
LID_DARK, LID_SKIN, LID_WET = (0.07, 0.07, 0.08), (0.36, 0.37, 0.41), (0.14, 0.07, 0.04)
FANG = D.Keratin(root=(0.12, 0.06, 0.04), body=(0.55, 0.54, 0.56), tip=(0.88, 0.86, 0.82), glow=(1.0, 0.55, 0.18),
                 stain=(0.25, 0.1, 0.03))
TALON = D.Keratin(root=(0.05, 0.05, 0.06), body=(0.14, 0.13, 0.15), tip=(0.95, 0.65, 0.3), glow=(1.0, 0.55, 0.18),
                  stain=(0.06, 0.05, 0.05))

HEAD_FOCUS = G(0, 0.04, 0.24)


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=8, rough=0.6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.28, glow=0.7, seed=s) for s in range(3)]
    for k, ((x, up, f), r) in enumerate(zip(EYES, (0.016, 0.0145, 0.013))):
        # Centred on the round-5 bead (body geometry), a little out of it, so the wet eye covers it.
        look = Vector((1.0, -0.5 + 0.2 * k, 0.1)).normalized()
        bead = G(x, up, f)
        n = look
        for sx in (1, -1):
            # Brow fold only: the three eyes sit close, and lower lids would fuse them into one lump.
            S.organic_eye(b, S.flip(bead + n * 0.005, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.15, 0.55, 0.5), lower=None, brow_lift=0.55,
                          segments=(20, 10), lid_segments=(12, 6))
    fang = [D.tooth_style(FANG, glow=0.5, seed=s) for s in range(2)]
    # Lower fangs on the jaw's lip (bone Jaw), upper fangs under the head slab (bone Head); mirrored pairs.
    for rows, up, bone, d, hook in (((0.022, 0.03), (0.045, 0.024)), 0.004, "Jaw", G(0, 1.0, 0.25), G(0, 0.4, -0.6)), \
                                   (((0.03, 0.028), (0.052, 0.022)), 0.03, "Head", G(0, -1.0, 0.25), G(0, -0.4, -0.6)):
        for k, (x, ln) in enumerate(rows):
            hit = c.hit(G(x, up, 0.6), Vector((0, 1, 0)))
            if hit is None:
                C.log("fang skipped", bone, x)
                continue
            p, n = hit
            for sx in (1, -1):
                D.add_tooth(b, S.flip(p, sx) + Vector((0, 0.008, 0)), S.flip(d.normalized(), sx), 0.0065, ln,
                            fang[k], hook=hook, hook_amount=0.25, bone=bone, lod=False, name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.7, glow_from=0.75, seed=s, wet=0.3) for s in range(2)]
    tip = G(0.055, -0.135, 0.03)
    for side, sx in SIDES:
        # Belly talons: a hooked keratin point on the stone talon, curling forward.
        D.add_claw(b, S.flip(tip, sx) + G(0, 0.012, -0.01), G(0, -0.5, 1.0).normalized(), 0.01, 0.045, talon[0],
                   hook=G(0, 1.0, 0.2), hook_amount=0.55, bone="Body", name="Talon")
        # A hook at every blade tip, curling down and back.
        for i, t in enumerate(TIPS):
            p = G(t[0] * sx, t[1], t[2])
            out = (p - G(0.4 * sx, 0.1, 0.09)).normalized()
            D.add_claw(b, p - out * 0.012, out, 0.007, 0.036, talon[1], hook=Vector((0, 0, -1)), hook_amount=0.6,
                       bone=f"Finger_{side}{i + 1}", name="Hook", lod=i == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(G(x * sx, up, f), 0.02) for (x, up, f) in EYES for sx in (1, -1)]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.07, 0.07, 0.08), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.5), {"Eye": 2.2, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.36), int(lod1 * 0.47), {"Talon": 1.2, "Hook": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: six wet amber eyes under shale lids, stone fangs, belly and "
                                        "blade-tip hooks; bones, clips and events unchanged",
                       body_maps=f"round-5 bead eyes' glow removed from Shalebat_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Finger_L1", G(-0.8, 0.07, 0.0), "Body", G(-0.055, -0.13, 0.03), 1.0,
                           walk="Fly", idle="Glide", face_dist=0.65, claw_dist=0.45, foot_dist=0.4,
                           root_focus=Vector((0, 0, 0)))
    out[1] = ("face_macro", "Glide", 15, "Head", G(-0.07, 0.06, 0.21), "close", dict(distance=0.25, azimuth=-60,
                                                                                     elevation=8))
    out.append(("mouth", "Hit", 4, "Head", G(0, 0.0, 0.27), "close", dict(distance=0.4, azimuth=-12, elevation=-4)))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.8)


def run(norender=False):
    """build_shalebat.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender, ground_z=GROUND)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS, ground_z=GROUND)
