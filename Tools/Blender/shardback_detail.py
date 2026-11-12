"""Round 46: the Shardback's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Shardback.fbx and Shardback_LOD1.fbx:
- Shardback_Face (Shardback_Face_Mat): the four amber eye beads become wet amber eyes with slit pupils, sunk under
  slate-violet lid folds below the brow plate (the round-15 beads' glow goes from the body maps), and frost-pale saw
  teeth down the inner edge of both side-closing mandibles (they close with the jaws).
- Shardback_Claws (Shardback_Claws_Mat): a pair of hooked keratin talons at the end of all six legs (dark slate
  roots, icy pale tips with a faint amber glow).
No bone, clip or event changes (the shard bones and the Shatter clip are untouched); the actions are exported again
as they are.

Run after build_shardback.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/shardback_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Shardback"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))
EYES = (((0.08, 0.232, 0.447), 0.017), ((0.125, 0.222, 0.42), 0.013))

# Colours (sRGB): slate-violet chitin, amber eyes and sockets, frost-white ice.
EYE = D.Eye(lens=(0.03, 0.014, 0.006), iris=(1.0, 0.72, 0.3), rim=(0.3, 0.12, 0.03), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.6, 0.2), vein=(0.3, 0.1, 0.05))
LID_DARK, LID_SKIN, LID_WET = (0.03, 0.03, 0.05), (0.2, 0.2, 0.33), (0.12, 0.06, 0.05)
TOOTH = D.Keratin(root=(0.12, 0.12, 0.2), body=(0.72, 0.8, 0.92), tip=(0.95, 0.98, 1.0), glow=(1.0, 0.55, 0.16),
                  stain=(0.15, 0.12, 0.2))
TALON = D.Keratin(root=(0.03, 0.03, 0.05), body=(0.12, 0.12, 0.2), tip=(0.85, 0.92, 1.0), glow=(1.0, 0.55, 0.16),
                  stain=(0.05, 0.05, 0.08))

HEAD_FOCUS = G(0, 0.21, 0.45)


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def mandible(sx):
    return [gp((0.095, 0.16, 0.42), sx), gp((0.09, 0.15, 0.5), sx), gp((0.03, 0.14, 0.56), sx)]


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=13)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.28, glow=0.8, seed=s) for s in range(2)]
    head_c = G(0, 0.2, 0.36)
    for k, (p, r) in enumerate(EYES):
        bead = gp(p, 1)
        n = ((bead - head_c).normalized() + G(0, 0, 0.6)).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(bead + n * 0.004, sx), S.flip(n, sx), r * 1.12, eyes[k], lid,
                          up=Vector((0, 0, 1)), bone="Head", lod=k == 0, brow=(1.25, 0.6, 0.55),
                          lower=(1.1, 0.4, 0.45), segments=(20, 10), lid_segments=(12, 6))
    tooth = D.tooth_style(TOOTH, glow=0.35, seed=6)
    for side, sx in SIDES:
        crv = D.curve_points(mandible(sx), 30)
        placed = D.edge_saw(c, b, crv, (0.3, 0.48, 0.66, 0.82), Vector((-sx, 0, 0)), 0.007, 0.032, tooth,
                            f"Jaw_{side}", rake=0.45, max_off=0.05, lod=False, start=0.04, name="Tooth")
        C.log("mandible teeth", side, placed)
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.45, glow_from=0.85, seed=s, wet=0.35) for s in range(2)]
    for i in (1, 2, 3):
        knee, foot = c.head(f"Leg_R{i}_Lower"), c.tail(f"Leg_R{i}_Lower")
        down = (foot - knee).normalized()
        out = Vector((down.x, down.y, 0)).normalized()
        base = foot - down * 0.02 + Vector((0, 0, 0.004))
        for k, spread in enumerate((1.0, -1.0)):
            d = (out * 0.6 + G(0, 0, spread * 0.8) + Vector((0, 0, -0.1))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(base, sx), S.flip(d, sx), 0.01, 0.042, talon[k], hook=Vector((0, 0, -1)),
                           hook_amount=0.6, bone=f"Leg_{side}{i}_Lower", name="Talon", lod=k == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_EYES = [(gp(p, sx), r * 1.3) for p, r in EYES for sx in (1, -1)]


def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.03, 0.03, 0.05), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.57), int(lod1 * 0.5), {"Eye": 2.2, "Tooth": 1.3}),
             "Claws": (claws, int(lod0 * 0.39), int(lod1 * 0.47), {"Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: four wet amber eyes under lid folds, mandible saw teeth, leg "
                                        "talons; bones, clips and events unchanged",
                       body_maps=f"round-15 bead eyes' glow removed from Shardback_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    foot = c.tail("Leg_L1_Lower")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L1_Lower", foot, "Leg_L2_Lower", c.tail("Leg_L2_Lower"), 1.2,
                           face_dist=0.9, claw_dist=0.4, foot_dist=0.35)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.1, 0.23, 0.44), "close", dict(distance=0.25, azimuth=-40,
                                                                                   elevation=10))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_shardback.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
