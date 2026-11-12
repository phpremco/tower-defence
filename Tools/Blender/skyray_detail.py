"""Round 46: the Skyray's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Skyray.fbx and Skyray_LOD1.fbx:
- Skyray_Face (Skyray_Face_Mat): six wet cyan eyes with slit pupils on top of the head, sunk under chitin brow
  folds, and (round 49A, Remco: "the teeth are not logical") the teeth where a ray's mouth is: a row hanging from
  the upper lip across the front edge of the head and a row standing on the lower lip (Jaw bone), so the mouth
  between the cephalic lobes shows its teeth from the play camera and opens with the dive. Round 49E: the mouth is a
  slit cut into the head (build_skyray.Mouth); the teeth are rooted in its gum line (mouth_teeth).
- Skyray_Claws (Skyray_Claws_Mat): a grappling hook of two keratin talons under each fin tip (the fin's own claw
  is their thumb), and (round 49A) serrations along both edges of the tail's spade.

Run after build_skyray.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/skyray_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Skyray"
PARTS = ("Face", "Claws")
VOXEL = 0.002
GROUND = -0.8

SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): deep blue chitin, cyan glow and eyes.
EYE = D.Eye(lens=(0.006, 0.02, 0.035), iris=(0.35, 0.95, 1.0), rim=(0.02, 0.15, 0.3), pupil=(0.0, 0.004, 0.01),
            glow=(0.4, 0.95, 1.0), vein=(0.05, 0.12, 0.3))
LID_DARK, LID_SKIN, LID_WET = (0.03, 0.035, 0.09), (0.22, 0.3, 0.6), (0.05, 0.1, 0.2)
# Round 49E: ivory teeth with dark gum-stained roots (the round-49A pale blue read as a chrome grille).
TOOTH = D.Keratin(root=(0.2, 0.05, 0.1), body=(0.82, 0.8, 0.74), tip=(0.96, 0.95, 0.9), glow=(0.4, 0.95, 1.0),
                  stain=(0.3, 0.08, 0.12))
TALON = D.Keratin(root=(0.03, 0.035, 0.09), body=(0.12, 0.15, 0.32), tip=(0.85, 0.95, 1.0), glow=(0.4, 0.95, 1.0),
                  stain=(0.03, 0.04, 0.1))

EYES = ((0.042, 0.37), (0.064, 0.315), (0.086, 0.26))
TAIL_END = G(0, -0.036, -1.5)
TAIL_PREV = G(0, -0.024, -1.28)
HEAD_FOCUS = G(0, 0.0, 0.45)
FWD = G(0, 0, 1)
UP = Vector((0, 0, 1))


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
    import build_skyray
    mouth_teeth(c, b, TOOTH, build_skyray.MOUTH, upper=(0.0, 0.03, 0.058), lower=(0.015, 0.045), length=1.15)
    return b


def mouth_teeth(c, b, pal, mouth, upper=(0.0, 0.022, 0.044, 0.064), lower=(0.011, 0.033, 0.054), length=1.0):
    """Round 49E: teeth rooted in the gum line of the slit mouth cut into the head (build_skyray.Mouth; Remco: the
    round-49A mouth "isn't attached"). The upper row hangs from the upper lip's edge (Head bone), the lower row
    stands on the lower lip's edge (Jaw bone), both just behind the lip so their roots sit in the gum band; they
    point a little back into the mouth, the middle teeth longest, and interleave."""
    tooth = D.tooth_style(pal, glow=0.35, seed=5)
    s = mouth.s
    for xs, upper_row in ((upper, True), (lower, False)):
        bone = "Head" if upper_row else "Jaw"
        for i, x in enumerate(xs):
            for sx in ((1, -1) if x else (1,)):
                lip = mouth.palate_up(x) if upper_row else mouth.ceiling(x)
                # The lip edge at this x: where a ray just inside the lip, running forward, leaves the skin.
                inner = lip + (0.003 if upper_row else -0.003)
                p, _ = c.out(G(x * sx * s, inner * s, mouth.start * s), FWD)
                root = Vector((p.x, p.y, lip * s)) - FWD * 0.008 * s + UP * (0.003 if upper_row else -0.003) * s
                d = Vector((0, 0.22, -1 if upper_row else 1)).normalized()
                k = 1.0 - 0.35 * (x / 0.07)
                # Uneven teeth (a grown row, not a comb).
                ln = (0.03 if upper_row else 0.024) * length * (1.0 + 0.16 * math.sin(i * 2.3 + sx * 1.1))
                D.add_tooth(b, root, d, 0.0085 * s * k, ln * s * k, tooth, hook=-FWD, hook_amount=0.25,
                            bone=bone, lod=i < 2, name="Tooth")


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
    spade_barbs(b, barb, TAIL_END, TAIL_PREV, 1.0)
    return b


def spade_barbs(b, barb, end, prev, s):
    """Round 49A: saw teeth along both edges of the tail spade (build_skyray.tail_parts), raked back."""
    d = (end - prev).normalized()
    side = Vector((1, 0, 0))
    for sx in (1, -1):
        for t, w in ((0.1, 0.066), (0.15, 0.052), (0.195, 0.032)):
            p = end + d * t * s + side * sx * (w - 0.004) * s
            D.add_tooth(b, p, (side * sx * 0.8 - d * 0.6).normalized(), 0.006 * s, 0.03 * s, barb, hook=-d,
                        hook_amount=0.35, bone="Tail_5", lod=False, name="Barb")


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.5), {"Eye": 2.0, "Tooth": 1.3}),
             "Claws": (claws, int(lod0 * 0.37), int(lod1 * 0.48), {"Talon": 1.2, "Barb": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 49A: six wet cyan eyes under brow folds, mouth teeth on both lips, "
                                        "fin-tip talons, spade barbs; new Jaw bone (lower lip)",
                       body_maps="round 49A body rebuilt and re-baked (no bead eyes on the body any more)")


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
