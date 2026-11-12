"""Round 46: the Skyray's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Skyray.fbx and Skyray_LOD1.fbx:
- Skyray_Face (Skyray_Face_Mat): the six cyan eye pits on the ram become wet cyan eyes with slit pupils, sunk under
  chitin brow folds (the round-5 bead eyes' glow goes from the body maps), and a row of hooked teeth down the inner
  edge of both pincers (they close with the pincers).
- Skyray_Claws (Skyray_Claws_Mat): a grappling hook of two keratin talons under each fin tip (the fin's own claw
  is their thumb), and barbs on the three stinger prongs.
No bone, clip or event changes; the actions are exported again as they are.

Run after build_skyray.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/skyray_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Skyray"
PARTS = ("Face", "Claws")
VOXEL = 0.002
GROUND = -0.8

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): deep blue chitin, cyan glow and eyes.
EYE = D.Eye(lens=(0.006, 0.02, 0.035), iris=(0.35, 0.95, 1.0), rim=(0.02, 0.15, 0.3), pupil=(0.0, 0.004, 0.01),
            glow=(0.4, 0.95, 1.0), vein=(0.05, 0.12, 0.3))
LID_DARK, LID_SKIN, LID_WET = (0.03, 0.035, 0.09), (0.22, 0.3, 0.6), (0.05, 0.1, 0.2)
TOOTH = D.Keratin(root=(0.06, 0.08, 0.2), body=(0.6, 0.7, 0.85), tip=(0.92, 0.97, 1.0), glow=(0.4, 0.95, 1.0),
                  stain=(0.05, 0.08, 0.2))
TALON = D.Keratin(root=(0.03, 0.035, 0.09), body=(0.12, 0.15, 0.32), tip=(0.85, 0.95, 1.0), glow=(0.4, 0.95, 1.0),
                  stain=(0.03, 0.04, 0.1))

EYES = ((0.042, 0.37), (0.064, 0.315), (0.086, 0.26))
TAIL_END = G(0, -0.034, -1.3)
HEAD_FOCUS = G(0, 0.02, 0.38)


def pincer_points(sx):
    return [G(0.08 * sx, 0.0, 0.37), G(0.118 * sx, 0.004, 0.47), G(0.098 * sx, 0.008, 0.56), G(0.038 * sx, 0.002, 0.61)]


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=3)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.3, glow=0.65, seed=s) for s in range(3)]
    for i, (x, f) in enumerate(EYES):
        r = 0.02 - 0.0025 * i
        look = Vector((0.45, -0.35, 1.0)).normalized()
        p, n = c.out(G(x * 0.6, 0.0, f), Vector((0.3, 0, 1.0)))
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.15, sx), S.flip(n, sx), r, eyes[i], lid,
                          up=S.flip(Vector((0.4, 1.0, 0.0)), sx), bone="Head", lod=i == 0, squash=(1.0, 0.8, 0.6),
                          brow=(1.35, 0.62, 0.6), lower=(1.2, 0.4, 0.45), segments=(18, 9), lid_segments=(12, 6))
    # Pincer teeth: hooked, raked towards the pincer's point, on the edge that faces the other pincer.
    tooth = D.tooth_style(TOOTH, glow=0.4, seed=5)
    for side, sx in SIDES:
        crv = D.curve_points(pincer_points(sx), 30)
        placed = D.edge_saw(c, b, crv, (0.3, 0.45, 0.6, 0.74), Vector((-sx, 0, 0)), 0.006, 0.03, tooth,
                            f"Pincer_{side}", rake=0.45, max_off=0.05, lod=False, start=0.05, name="Tooth")
        C.log("pincer teeth", side, placed)
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def fin_tip(c, side):
    """The fin tip (end of the leading chain's last bone, rest pose)."""
    return c.tail(f"Fin_{side}_A3")


def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.75, glow_from=0.75, seed=s, wet=0.35) for s in range(2)]
    barb = D.tooth_style(TOOTH, glow=0.6, seed=8)
    for side, sx in SIDES:
        tip = fin_tip(c, side)
        bones = f"Fin_{side}_A3"
        # Two hooked talons under the fin tip, curling down and in: a grapple for the gate walls.
        for k, (fwd, ln) in enumerate(((0.03, 0.075), (-0.03, 0.06))):
            base = tip + G(-0.03 * sx, -0.008, fwd)
            d = (G(0.35 * sx, -1.0, 0.25 if k == 0 else -0.2)).normalized()
            D.add_claw(b, base, d, 0.011, ln, talon[k], hook=G(-0.6 * sx, -0.2, 0.4), hook_amount=0.6, bone=bones,
                       name="Talon", lod=k == 0)
    # Barbs on the stinger prongs: two backward hooks on the outer prongs, one under the centre prong.
    end = TAIL_END
    for sx in (1, -1):
        for t, ln in ((0.45, 0.03), (0.75, 0.025)):
            p = end + G(0.075 * sx, 0.004, -0.11) * t
            D.add_tooth(b, p + G(0.006 * sx, 0, 0), G(1.0 * sx, 0.1, 0.55).normalized(), 0.006, ln, barb,
                        hook=G(0, 0, 1), hook_amount=0.35, bone="Tail_5", lod=False, name="Barb")
    for t, ln in ((0.4, 0.03), (0.7, 0.024)):
        p = end + G(0, 0.0, -0.15) * t
        D.add_tooth(b, p + G(0, -0.006, 0), G(0, -1.0, 0.55).normalized(), 0.006, ln, barb, hook=G(0, 0, 1),
                    hook_amount=0.35, bone="Tail_5", lod=False, name="Barb")
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(G(x * sx, 0.044 - 0.004 * i, f), 0.024) for i, (x, f) in enumerate(EYES) for sx in (1, -1)]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.03, 0.035, 0.09), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.5), {"Eye": 2.0, "Tooth": 1.3}),
             "Claws": (claws, int(lod0 * 0.37), int(lod1 * 0.48), {"Talon": 1.2, "Barb": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: six wet cyan eyes under brow folds, pincer teeth, fin-tip "
                                        "talons, stinger barbs; bones, clips and events unchanged",
                       body_maps=f"round-5 bead eyes' glow removed from Skyray_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Fin_L_A3", fin_tip(c, "L"), "Tail_5", TAIL_END, 1.5, walk="Fly",
                           face_dist=1.0, claw_dist=0.7, foot_dist=0.6, root_focus=Vector((0, 0, 0)))
    out[1] = ("face_macro", "Idle", 15, "Head", HEAD_FOCUS, "close", dict(distance=0.4, azimuth=-30, elevation=40))
    out[4] = ("stinger", "Idle", 15, "Tail_5", TAIL_END, "close", dict(distance=0.6, azimuth=-140, elevation=30))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 1.3)


def run(norender=False):
    """build_skyray.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender, ground_z=GROUND)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS, ground_z=GROUND)
