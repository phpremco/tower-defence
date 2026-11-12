"""Round 46: the Knitback's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Knitback.fbx and Knitback_LOD1.fbx:
- Knitback_Face (Knitback_Face_Mat): the amber eye slits in the bark mask become wet amber almond eyes with slit
  pupils peering out under bark lid folds (the round-15 slits' glow goes from the body maps), and a row of hooked
  thorn teeth up from the lower lip and down from under the mask round the dim mouth.
- Knitback_Claws (Knitback_Claws_Mat): a hooked thorn point at the end of every root claw (three per foot), dark
  heartwood roots, pale tips with a faint amber glow.
No bone, clip or event changes (the pod bones and the Regrow clip are untouched); the actions are exported again
as they are.

Run after build_knitback.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/knitback_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Knitback"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))
EYES = (((0.045, 0.305, 0.485), 0.02), ((0.088, 0.29, 0.468), 0.016))

# Colours (sRGB): bark and moss, amber eyes and pods.
EYE = D.Eye(lens=(0.03, 0.016, 0.004), iris=(1.0, 0.7, 0.22), rim=(0.3, 0.12, 0.02), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.65, 0.2), vein=(0.3, 0.12, 0.03))
LID_DARK, LID_SKIN, LID_WET = (0.05, 0.04, 0.025), (0.26, 0.2, 0.12), (0.12, 0.08, 0.03)
THORN = D.Keratin(root=(0.08, 0.06, 0.03), body=(0.45, 0.38, 0.25), tip=(0.9, 0.85, 0.68), glow=(0.95, 0.82, 0.22),
                  stain=(0.15, 0.18, 0.05))

HEAD_FOCUS = G(0, 0.27, 0.46)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=15, rough=0.55)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.27, glow=0.85, seed=s) for s in range(2)]
    for k, (p, r) in enumerate(EYES):
        slit = gp(p, 1)
        look = (G(0.35 + 0.25 * k, 0.1, 1.0)).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(slit + look * 0.004, sx), S.flip(look, sx), r, eyes[k], lid,
                          up=Vector((0, 0, 1)), bone="Head", lod=k == 0, squash=(1.45, 0.75, 0.7),
                          brow=(1.15, 0.62, 0.6), lower=(1.05, 0.4, 0.45), segments=(20, 10), lid_segments=(12, 6))
    thorn = [D.tooth_style(THORN, glow=0.3, seed=s) for s in range(2)]
    for rows, up, d, hook in ((((0.015, 0.026), (0.035, 0.022)), 0.196, G(0, 1.0, 0.25), G(0, 0.3, -0.5)),
                              (((0.022, 0.03), (0.042, 0.024)), 0.228, G(0, -1.0, 0.25), G(0, -0.3, -0.5))):
        for k, (x, ln) in enumerate(rows):
            hit = c.hit(G(x, up, 0.9), Vector((0, 1, 0)))
            if hit is None:
                C.log("thorn tooth skipped", x, up)
                continue
            p, n = hit
            for sx in (1, -1):
                D.add_tooth(b, S.flip(p, sx) + Vector((0, 0.008, 0)), S.flip(d.normalized(), sx), 0.006, ln, thorn[k],
                            hook=hook, hook_amount=0.25, bone="Head", lod=False, name="Thorn")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    point = [D.keratin_style(THORN, glow=0.45, glow_from=0.8, seed=s, wet=0.2) for s in range(3)]
    for i in (1, 2):
        foot = c.tail(f"Leg_R{i}_Lower")
        base = foot + Vector((0, 0, 0.02))
        for j, a in enumerate((-40, 0, 40)):
            d = Matrix.Rotation(math.radians(a + (25 if i == 1 else -10)), 3, "Z") @ gp((0.25, 0.0, 1.0), 1).normalized()
            tip = base + d * 0.1 + Vector((0, 0, -0.02))
            out = (d + Vector((0, 0, -0.2))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(tip - out * 0.015, sx), S.flip(out, sx), 0.01, 0.045, point[j],
                           hook=Vector((0, 0, -1)), hook_amount=0.6, bone=f"Leg_{side}{i}_Lower", name="Thorn",
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
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.05, 0.04, 0.025), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.58), int(lod1 * 0.5), {"Eye": 2.2, "Thorn": 1.2}),
             "Claws": (claws, int(lod0 * 0.38), int(lod1 * 0.47), {"Thorn": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: wet amber eyes under bark lids, thorn teeth, root-claw thorns; "
                                        "bones, clips and events unchanged",
                       body_maps=f"round-15 eye slits' glow removed from Knitback_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    foot = c.tail("Leg_L1_Lower")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L1_Lower", foot, "Leg_L2_Lower", c.tail("Leg_L2_Lower"), 1.3,
                           face_dist=0.9, claw_dist=0.45, foot_dist=0.4)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.06, 0.3, 0.48), "close", dict(distance=0.25, azimuth=-30,
                                                                                   elevation=8))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_knitback.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
