"""Round 46: the Cragback's close-up detail (Remco: "do the same for the small enemies").

Two extra skinned parts (detail_kit / detail_kit_small), exported in Cragback.fbx and Cragback_LOD1.fbx:
- Cragback_Face (Cragback_Face_Mat): the three amber eyes in the brow's slit become wet, glowing eyes sunk under
  stone lids (the round-4 bead eyes' glow goes from the body maps), and a ragged row of basalt fangs on the lip of
  the lower jaw (they drop with the jaw) and under the brow, glowing hot at the roots like the faults.
- Cragback_Claws (Cragback_Claws_Mat): obsidian talons on the splayed stone toes (all three toes of the front feet,
  the forward toe of the others), dark glassy roots, ember-hot tips.
No bone, clip or event changes; the actions are exported again as they are.

Run after build_cragback.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/cragback_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Cragback"
PARTS = ("Face", "Claws")
VOXEL = 0.0024

SIDES = (("L", -1), ("R", 1))
PAIRS = {1: 0.22, 2: -0.03, 3: -0.27}

# Colours (sRGB): grey-brown stone, amber eyes, orange fault glow.
EYE = D.Eye(lens=(0.03, 0.012, 0.004), iris=(1.0, 0.72, 0.22), rim=(0.3, 0.1, 0.02), pupil=(0.006, 0.002, 0.0),
            glow=(1.0, 0.6, 0.2), vein=(0.4, 0.12, 0.03))
LID_DARK, LID_SKIN, LID_WET = (0.06, 0.05, 0.045), (0.36, 0.31, 0.26), (0.16, 0.06, 0.02)
FANG = D.Keratin(root=(0.1, 0.05, 0.03), body=(0.32, 0.29, 0.26), tip=(0.78, 0.72, 0.62), glow=(1.0, 0.45, 0.12),
                 stain=(0.25, 0.08, 0.02))
TALON = D.Keratin(root=(0.04, 0.035, 0.035), body=(0.1, 0.085, 0.08), tip=(0.95, 0.55, 0.2), glow=(1.0, 0.45, 0.12),
                  stain=(0.05, 0.03, 0.02))

HEAD_FOCUS = G(0, 0.38, 0.4)
FOOT_FOCUS = G(-0.47, 0.03, 0.27)
OLD_EYES = [(G(0, 0.55 - i * 0.008, 0.38 - i * 0.055), 0.03) for i in range(3)]


def foot(side, i):
    sx = -1 if side == "L" else 1
    z = PAIRS[i]
    return G(0.47 * sx, 0.0, z * 1.15 + 0.02), sx


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=2, rough=0.6)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.26, glow=0.95, seed=s) for s in range(3)]
    # The three eyes in a row along the top of the brow, looking up and forward, the biggest in front; their lids
    # (stone folds) close from behind and in front so they sit deep in the slab.
    for i, ((p0, _), r) in enumerate(zip(OLD_EYES, (0.036, 0.03, 0.025))):
        p, n = c.out(p0 - Vector((0, 0, 0.08)), Vector((0, -0.45, 1.0)))
        n = (n * 0.6 + Vector((0, -0.45, 1.0)).normalized() * 0.4).normalized()
        S.organic_eye(b, p - n * r * 0.2, n, r, eyes[i], lid, up=Vector((0, 1, 0.2)), bone="Front", lod=i == 0,
                      squash=(0.85, 1.0, 0.65), brow=(1.4, 0.6, 0.65), lower=(1.3, 0.45, 0.5), brow_tilt=0.35,
                      segments=(20, 10), lid_segments=(14, 7))
    # Fangs: a ragged row up from the lower jaw's lip (bone Jaw) and a row down from under the brow (bone Front),
    # mirrored pairs plus a centre fang below; hot at the roots like the faults.
    fang = [D.tooth_style(FANG, glow=0.55, seed=s) for s in range(3)]
    for k, (x, ln) in enumerate(((0.03, 0.06), (0.068, 0.078), (0.1, 0.05))):
        p, n = c.hit(G(x, 0.225, 0.8), Vector((0, 1, 0))) or (None, None)
        if p is None:
            C.log("lower fang skipped", x)
            continue
        for sx in (1, -1):
            q = S.flip(p, sx) + Vector((0, 0.012, 0.004))
            d = (Vector((0, 0, 1)) + Vector((0, -0.35, 0)) + Vector((sx * 0.08, 0, 0))).normalized()
            D.add_tooth(b, q, d, 0.014 + 0.004 * (k == 1), ln, fang[k], hook=Vector((0, 0.6, 0.4)), hook_amount=0.25, bone="Jaw",
                        lod=False, name="Fang")
    for k, (x, ln) in enumerate(((0.0, 0.05), (0.06, 0.045), (0.11, 0.038))):
        p, n = c.hit(G(x, 0.285, 0.8), Vector((0, 1, 0))) or (None, None)
        if p is None:
            C.log("upper fang skipped", x)
            continue
        for sx in ((1,) if x == 0 else (1, -1)):
            q = S.flip(p, sx) + Vector((0, 0.015, -0.004))
            d = (Vector((0, 0, -1)) + Vector((0, -0.3, 0))).normalized()
            D.add_tooth(b, q, d, 0.01, ln, fang[k], hook=Vector((0, 0.6, -0.4)), hook_amount=0.25, bone="Front",
                        lod=False, name="Fang")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(c):
    b = C.PartBuilder()
    talon = [D.keratin_style(TALON, glow=0.8, glow_from=0.7, seed=s, wet=0.4) for s in range(3)]
    # The toes of build_cragback (right side; the body is mirrored, so the left talons are the right ones mirrored):
    # splayed at a - 0.65, a, a + 0.65 round the forward direction.
    for i in PAIRS:
        ft, _ = foot("R", i)
        toe = ft + Vector((0, 0, 0.018))
        for k, a in enumerate((-0.65, 0.0, 0.65)):
            if i != 1 and k != 1:
                continue
            out = G(math.sin(a + 0.3), 0, math.cos(a + 0.3)).normalized()
            tip = toe + out * 0.09 - Vector((0, 0, 0.008))
            d = (out + Vector((0, 0, -0.15))).normalized()
            for side, sx in SIDES:
                D.add_claw(b, S.flip(tip - out * 0.012, sx), S.flip(d, sx), 0.016, 0.06 if k == 1 else 0.05,
                           talon[k], hook=Vector((0, 0, -1)), hook_amount=0.55, bone=f"Leg_{side}{i}_Lower",
                           name="Talon", lod=k == 1)
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face, claws = face_parts(c), claw_parts(c)
    if blockout_only:
        return {"Face": face, "Claws": claws}
    dimmed = S.dim_body(c, OLD_EYES, emission_keep=0.05, darken=(0.1, 0.08, 0.07), darken_amount=0.5)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.57), int(lod1 * 0.5), {"Eye": 2.0, "Fang": 1.3}),
             "Claws": (claws, int(lod0 * 0.38), int(lod1 * 0.5), {"Talon": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: three wet amber eyes under stone lids, basalt fangs, obsidian "
                                        "talons; bones, clips and events unchanged",
                       body_maps=f"round-4 bead eyes' glow removed from Cragback_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


EYE_FOCUS = G(0, 0.53, 0.33)


def shots(c):
    out = S.standard_shots("Front", HEAD_FOCUS, "Leg_L1_Lower", FOOT_FOCUS, "Leg_L1_Lower", FOOT_FOCUS, 1.2,
                           face_dist=1.5, claw_dist=0.7, foot_dist=0.55)
    # The eyes sit on top of the brow (seen from the game camera): the macro looks down at them.
    out[1] = ("face_macro", "Idle", 15, "Front", EYE_FOCUS, "close", dict(distance=0.45, azimuth=-25, elevation=48))
    return out


CLIP_FOCUS = ("Front", HEAD_FOCUS, 1.3)


def run(norender=False):
    """build_cragback.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
