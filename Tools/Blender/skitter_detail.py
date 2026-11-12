"""Round 46: the Skitter's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Skitter.fbx and Skitter_LOD1.fbx next to the body:
- Skitter_Face (Skitter_Face_Mat): a spider's arc of wet eyes tucked under the shovel crest on each side (where the
  round-2 sensor slits glowed; one big main eye and a smaller one behind it, pale yellow-green irises, slit pupils, sunk
  under a chitin brow fold), and hooked teeth on the inner faces of the four X jaws (they open with the jaws).
- Skitter_Claws (Skitter_Claws_Mat): saw teeth down the inner edge of both fore-blades and a hooked point on each,
  and a two-talon grappling foot at the end of every walking leg.
No bone, clip or event changes; the actions are exported again as they are.

Run after build_skitter.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/skitter_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Skitter"
PARTS = ("Face", "Claws")
VOXEL = 0.0016

SIDES = (("L", -1), ("R", 1))
JAWS = ("UL", "UR", "LL", "LR")

# Colours (sRGB). The species: rust plates on a near-black body, pale-yellow eyes, lime venom.
EYE = D.Eye(lens=(0.012, 0.014, 0.004), iris=(0.92, 1.0, 0.3), rim=(0.2, 0.22, 0.02), pupil=(0.004, 0.003, 0.0),
            glow=(0.85, 1.0, 0.28), vein=(0.25, 0.08, 0.02))
LID_DARK, LID_SKIN, LID_WET = (0.035, 0.018, 0.01), (0.42, 0.16, 0.04), (0.12, 0.05, 0.02)
TOOTH = D.Keratin(root=(0.16, 0.06, 0.02), body=(0.78, 0.68, 0.48), tip=(0.97, 0.94, 0.8), glow=(0.68, 1.0, 0.18),
                  stain=(0.2, 0.12, 0.02))
CLAW = D.Keratin(root=(0.04, 0.02, 0.012), body=(0.2, 0.08, 0.025), tip=(0.95, 0.85, 0.6), glow=(0.68, 1.0, 0.18),
                 stain=(0.1, 0.04, 0.01))

HEAD_FOCUS = G(0, 0.16, 0.3)
BLADE_FOCUS = G(-0.12, 0.2, 0.4)
FOOT_FOCUS = G(-0.31, 0.03, 0.2)


# ------------------------------------------------------------------------------------------------------------
# The body's geometry (build_skitter.py runs on import, so the few numbers are repeated here)
# ------------------------------------------------------------------------------------------------------------

def jaw_points(name):
    sx = -1 if name[1] == "L" else 1
    if name[0] == "U":
        return [G(0.03 * sx, 0.14, 0.3), G(0.075 * sx, 0.165, 0.37), G(0.062 * sx, 0.155, 0.44),
                G(0.018 * sx, 0.135, 0.478)]
    return [G(0.03 * sx, 0.1, 0.3), G(0.072 * sx, 0.075, 0.365), G(0.056 * sx, 0.08, 0.432),
            G(0.015 * sx, 0.104, 0.468)]


def blade_points(sx):
    elbow = G(0.155 * sx, 0.31, 0.17)
    return [elbow, G(0.15 * sx, 0.25, 0.33), G(0.105 * sx, 0.155, 0.5), G(0.068 * sx, 0.105, 0.6)]


MOUTH_BACK, MOUTH_FRONT = G(0, 0.12, 0.3), G(0, 0.12, 0.48)


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=4)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.62, pupil_size=0.3, glow=0.38, seed=s) for s in range(2)]
    # (game x, up, forward of a point inside the head, radius): the main eye low and forward under the crest rim,
    # two smaller ones behind and above it, like a jumping spider's side eyes.
    for k, (x, up, fwd, r) in enumerate(((0.038, 0.168, 0.29, 0.022), (0.052, 0.176, 0.235, 0.015))):
        look = (Vector((1.0, -0.55 + 0.25 * k, 0.15))).normalized()
        p, n = c.out(G(x * 0.4, up, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        # Placed on the right and mirrored, so both sides match exactly (the bake mirrors the UVs across x = 0).
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.25, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 0, 1)),
                          bone="Head", brow=(1.35, 0.7, 0.6), lower=(1.15, 0.42, 0.45), lod=k == 0,
                          segments=(20, 10), lid_segments=(14, 7))
    # Jaw teeth: hooked fangs on the inner face of every X jaw, raked back down the throat.
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(2)]
    for name in JAWS:
        crv = D.curve_points(jaw_points(name), 30)
        D.inner_teeth(c, b, crv, MOUTH_BACK, MOUTH_FRONT, ((0.35, 0.0055, 0.026, 0.0), (0.55, 0.005, 0.024, 0.0),
                                                           (0.75, 0.004, 0.02, 0.0)),
                      tooth[0 if name[0] == "U" else 1], f"Jaw_{name}", max_off=0.05, rake=0.3, hook_amount=0.45,
                      name="Tooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.7, glow_from=0.8, seed=s, wet=0.3) for s in range(2)]
    saw = D.tooth_style(TOOTH, glow=0.3, seed=9)
    for side, sx in SIDES:
        bone = f"Blade_{side}_2"
        crv = D.curve_points(blade_points(sx), 40)
        # Saw teeth on the blade's inner (cutting) edge, raked towards the point.
        placed = D.edge_saw(c, b, crv, (0.45, 0.57, 0.69, 0.81), Vector((-sx, 0, -0.6)), 0.0075, 0.034, saw, bone,
                            rake=0.5, max_off=0.06, lod=False, start=0.06)
        C.log("blade saw teeth", side, placed)
        end = crv[-3]
        tangent = (crv[-1] - crv[-5]).normalized()
        D.add_claw(b, end - tangent * 0.01, tangent, 0.007, 0.05, claw[0], hook=Vector((-sx * 0.4, 0, -1)),
                   hook_amount=0.6, bone=bone, name="BladeTip")
        for i in (1, 2):
            bone = f"Leg_{side}{i}_Tibia"
            knee, foot = c.head(bone), c.tail(bone)
            down = (foot - knee).normalized()
            fwd = Vector((0, -1, 0))
            base = foot - down * 0.03 + Vector((0, 0, 0.006))
            # Two talons splayed fore and aft, curling down so the points rest on the ground.
            for k, spread in enumerate((1.0, -1.0)):
                d = (down * 0.35 + fwd * spread * 0.8 + Vector((sx * 0.25, 0, 0.05))).normalized()
                D.add_claw(b, base, d, 0.006, 0.04, claw[k], hook=Vector((0, 0, -1)), hook_amount=0.65, bone=bone,
                           name="Talon", lod=k == 0)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.6), int(lod1 * 0.5), {"Eye": 2.2, "Tooth": 1.4}),
             "Claws": (claws, int(lod0 * 0.4), int(lod1 * 0.5), {"Saw": 1.3, "BladeTip": 1.3, "Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: wet eyes under the crest, jaw teeth, blade saws and tips, "
                                        "talon feet; bones, clips and events unchanged")


def shots(c):
    return S.standard_shots("Head", HEAD_FOCUS, "Blade_L_2", BLADE_FOCUS, "Leg_L1_Tibia", FOOT_FOCUS, 1.0,
                            face_dist=0.8, claw_dist=0.9, foot_dist=0.55)


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.8)


def run(norender=False):
    """build_skitter.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
