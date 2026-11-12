"""Round 46: the Slagstrider's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Slagstrider.fbx and Slagstrider_LOD1.fbx:
- Slagstrider_Face (Slagstrider_Face_Mat): two small, deep-set ember eyes per side on the wedge head (molten irises
  with a black slit, sunk under slag brow folds; the round-15 design had no visible eyes, so these stay small and
  low under the crown), and a row of obsidian fangs hanging from both lips of the glowing slit jaw under the head, raking across it.
- Slagstrider_Claws (Slagstrider_Claws_Mat): three hooked obsidian spurs splayed round the base of every slag foot
  cone (dark glassy roots, ember-hot tips).
No bone, clip or event changes; the actions are exported again as they are.

Round 49B: the body was redesigned (build_slagstrider.py); three eyes per side sit in the new head's ember pits, the
slit fangs follow its slit, the spurs sit on the new pointed foot cones.

Run after build_slagstrider.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/slagstrider_detail.py -- [--before | --blockout | --qa]
  [--norender]
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

NAME = "Slagstrider"
PARTS = ("Face", "Claws")
VOXEL = 0.002

SIDES = (("L", -1), ("R", 1))
SLIT = G(0, 0.455, 0.53)  # round 49B: build_slagstrider.SLIT
PITCH = Matrix.Rotation(math.radians(12), 3, "X")

# Colours (sRGB): charcoal slag, molten orange glow.
EYE = D.Eye(lens=(0.04, 0.012, 0.004), iris=(1.0, 0.62, 0.18), rim=(0.35, 0.08, 0.02), pupil=(0.005, 0.0, 0.0),
            glow=(1.0, 0.55, 0.14), vein=(0.4, 0.08, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.03, 0.025, 0.022), (0.16, 0.14, 0.13), (0.3, 0.08, 0.02)
OBSIDIAN = D.Keratin(root=(0.04, 0.03, 0.03), body=(0.1, 0.085, 0.08), tip=(1.0, 0.65, 0.3), glow=(1.0, 0.5, 0.1),
                     stain=(0.3, 0.08, 0.02))

HEAD_FOCUS = G(0, 0.5, 0.48)
# Round 49B: the redesigned head's eye pits (build_slagstrider.EYES): three small ember eyes per side in a row.
EYES = [((0.062, 0.52, 0.5), 0.016), ((0.07, 0.522, 0.45), 0.013), ((0.072, 0.522, 0.405), 0.011)]


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=16, rough=0.6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.26, glow=0.95, seed=s) for s in range(3)]
    for k, ((x, up, fwd), r) in enumerate(EYES):
        look = G(1.0, 0.25, 0.7 - 0.25 * k).normalized()
        # From inside the head out through its pit: the lens sits on the skin, not buried in the wedge.
        p, n = c.out(G(x * 0.3, up, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.3, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", lod=k == 0, brow=(1.3, 0.7, 0.6), lower=(1.1, 0.42, 0.45), brow_tilt=0.6,
                          segments=(18, 9), lid_segments=(12, 6))
    fang = [D.tooth_style(OBSIDIAN, glow=0.6, seed=s) for s in range(2)]
    # Along both lips of the slit jaw under the head, from its back to its front: fangs hanging down either side of
    # the glowing slit and raking across it (right lip, mirrored).
    for j, t in enumerate((-0.6, -0.25, 0.1, 0.45, 0.8)):
        along = SLIT + PITCH @ Vector((0.016, -0.07 * t, 0))
        hit = c.hit(along - Vector((0, 0, 0.15)), Vector((0, 0, 1)))
        if hit is None or (hit[0] - along).length > 0.06:
            C.log("slit fang skipped", t)
            continue
        p, n = hit
        d = (PITCH @ Vector((0, -0.2, -1.0)) + Vector((-0.35, 0, 0))).normalized()
        for sx in (1, -1):
            D.add_tooth(b, S.flip(p + Vector((0, 0, 0.004)), sx), S.flip(d, sx), 0.007, 0.042 - 0.008 * abs(t),
                        fang[j % 2], hook=S.flip(Vector((-1, 0, 0)), sx), hook_amount=0.25, bone="Head", lod=False,
                        name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    spur = [D.keratin_style(OBSIDIAN, glow=0.85, glow_from=0.7, seed=s, wet=0.4) for s in range(3)]
    for i in (1, 2):
        foot = c.tail(f"Leg_R{i}_Lower")
        for k, deg in enumerate((-50.0, 10.0, 70.0)):
            phi = math.radians(deg) * (1 if i == 1 else -1)
            out = Vector((math.cos(phi), -math.sin(phi), 0.0))
            base = foot + out * 0.02 + Vector((0, 0, 0.035))
            d = (out + Vector((0, 0, -0.25))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(base, sx), S.flip(d, sx), 0.011, 0.05, spur[k], hook=Vector((0, 0, -1)),
                           hook_amount=0.6, bone=f"Leg_{side}{i}_Lower", name="Spur", lod=k == 1)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.56), int(lod1 * 0.5), {"Eye": 2.2, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.4), int(lod1 * 0.47), {"Spur": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 49B (redesigned body): three ember eyes per side in their pits, "
                                        "obsidian slit-jaw fangs, foot spurs; bones, clips and events unchanged")


def shots(c):
    foot = c.tail("Leg_L1_Lower")
    out = S.standard_shots("Head", HEAD_FOCUS, "Leg_L1_Lower", foot, "Leg_L2_Lower", c.tail("Leg_L2_Lower"), 1.6,
                           face_dist=0.8, claw_dist=0.45, foot_dist=0.4, root_focus=Vector((0, 0, 0.4)))
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.04, 0.5, 0.47), "close", dict(distance=0.25, azimuth=-40,
                                                                                   elevation=8))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_slagstrider.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
