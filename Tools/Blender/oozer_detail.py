"""Round 46: the Oozer's close-up detail (Remco: "do the same for the small enemies").

One extra skinned part (detail_kit / detail_kit_small), exported in Oozer.fbx and Oozer_LOD1.fbx:
- Oozer_Face (Oozer_Face_Mat): a wet, pale-yellow eye with a slit pupil on the front of each of the four stalk bulbs,
  half-closed under a fleshy lid fold (the round-10 bulbs' bead glow goes from the body maps; the bulbs stay as the
  dark wet flesh round the eye), and the lamprey mouth gets an inner ring of hooked keratin teeth raked down the
  gullet and a ring of rasping fangs on the lip, glowing lime at the roots.
The slug has no limbs: there is no Claws part. The eyes ride the stalk bones, so they retract with the stalks
(bone scale in Hit and Death). No bone, clip or event changes; the actions are exported again as they are.
build_slimemonarch.py reuses build_oozer's geometry, not this file: the SlimeMonarch is unaffected.

Run after build_oozer.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/oozer_detail.py -- [--before | --blockout | --qa] [--norender]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
import detail_kit_small as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Oozer"
PARTS = ("Face",)
VOXEL = 0.0018

SIDES = (("L", -1), ("R", 1))
STALKS = {1: ((0.045, 0.2, 0.37), (0.1, 0.4, 0.46)), 2: ((0.075, 0.21, 0.29), (0.15, 0.31, 0.27))}
MOUTH = G(0, 0.09, 0.45)
FWD = G(0, 0, 1)
UP = Vector((0, 0, 1))

# Colours (sRGB): olive hide, pale-yellow eyes, lime toxin.
EYE = D.Eye(lens=(0.03, 0.03, 0.008), iris=(1.0, 0.95, 0.45), rim=(0.25, 0.25, 0.03), pupil=(0.004, 0.004, 0.0),
            glow=(0.85, 1.0, 0.35), vein=(0.3, 0.25, 0.04))
LID_DARK, LID_SKIN, LID_WET = (0.05, 0.045, 0.02), (0.26, 0.26, 0.09), (0.2, 0.25, 0.04)
# Round 49A slime teeth: glassy, pale lime, lit from inside, wet (Remco: "slime teeth").
TOOTH = D.Keratin(root=(0.3, 0.42, 0.08), body=(0.72, 0.9, 0.42), tip=(0.9, 1.0, 0.72), glow=(0.66, 1.0, 0.16),
                  stain=(0.35, 0.5, 0.06))
# The goo strands between the jaws and the drips under the lip.
GOO = H.Palette(top=(0.55, 0.85, 0.18), side=(0.4, 0.66, 0.1), belly=(0.3, 0.45, 0.06), seam=(0.2, 0.3, 0.04),
                dark=(0.2, 0.3, 0.04), glow=(0.66, 1.0, 0.16), eye=(0.9, 1.0, 0.5))

HEAD_FOCUS = G(0, 0.2, 0.42)


def stalk(n, sx):
    """Root, tip and bulb centre of a stalk (build_oozer's geometry)."""
    root, tip = STALKS[n]
    r, t = G(root[0] * sx, root[1], root[2]), G(tip[0] * sx, tip[1], tip[2])
    mid = r.lerp(t, 0.5) + G(0.015 * sx, 0.0, -0.02)
    return r, t, t + (t - mid).normalized() * 0.012


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=11, rough=0.3)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.26, glow=0.8, seed=s) for s in range(2)]
    # The eyeball is a little bigger than the bulb and swallows it; the lids close over its top and bottom.
    for n, r in ((1, 0.028), (2, 0.023)):
        _, _, bulb = stalk(n, 1)
        look = G(0.45, 0.15, 1.0 if n == 1 else 0.4).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(bulb + look * 0.004, sx), S.flip(look, sx), r, eyes[n - 1], lid,
                          up=Vector((0, 0, 1)), bone=f"Stalk_{'R' if sx > 0 else 'L'}{n}", lod=True,
                          squash=(1.0, 0.95, 0.9), brow=(1.12, 0.62, 0.62), lower=(1.06, 0.45, 0.55),
                          brow_tilt=0.55, segments=(22, 11), lid_segments=(12, 6))
    slime_mouth(c, b, 1.0)
    return b


# Right-hand halves (x >= 0) of the rows; each is mirrored to the left (the detail parts are made symmetric).
# Lengths and tilts vary along a row so it looks grown, not stamped.
UPPER = ((0.009, 0.07, 0.0105), (0.031, 0.05, 0.0085), (0.052, 0.062, 0.0095), (0.072, 0.036, 0.0072))
LOWER = ((0.018, 0.042, 0.0085), (0.041, 0.05, 0.009), (0.062, 0.03, 0.007))
# Strands (upper tooth x, lower tooth x, sag) and drips under the lip (x, length).
STRANDS = ((0.009, 0.018, 0.026), (0.052, 0.041, 0.016))
DRIPS = ((0.0, 0.036), (0.034, 0.052))


def slime_mouth(c, b, s, lod_strands=False):
    """Round 49A: the slack maw's slime teeth and goo (`s` the model's size in Oozer units: 1 the Oozer, the
    SlimeMonarch's M). Translucent lime fangs hang from the hood and stand on the lower lip, crooked and of uneven
    length; sticky strands sag between the jaws and drips hang from the lip. All on the Head bone (they stretch with
    the strike)."""
    tooth = [D.tooth_style(TOOTH, glow=0.75, seed=k) for k in range(3)]
    goo = H.tube(GOO, 1.0, GOO.top, GOO.side, tip_glow=0.7, glow_from=-2.0, rough=0.15)
    uppers, lowers = {}, {}
    for sx in (1, -1):
        for i, (x, length, rad) in enumerate(UPPER):
            p, n = c.out(G(x * sx * s, 0.16 * s, 0.37 * s), G(0, -0.75, 1))
            lean = Vector((0.1 * sx * ((i % 2) * 2 - 1), 0, 0))
            d = (Vector((0, 0, -1)) + FWD * (0.12 + 0.08 * (i % 3 - 1)) + lean).normalized()
            D.add_tooth(b, p - n * rad * s * 0.8, d, rad * s, length * s, tooth[i % 3], hook=-FWD, hook_amount=0.3,
                        bone="Head", lod=i % 2 == 0, name="SlimeTooth")
            uppers[x * sx] = p - n * rad * s * 0.5
        for i, (x, length, rad) in enumerate(LOWER):
            p, n = c.out(G(x * sx * s, 0.045 * s, 0.39 * s), G(0, 0.8, 1))
            lean = Vector((-0.1 * sx * ((i % 2) * 2 - 1), 0, 0))
            d = (UP + FWD * (0.15 - 0.08 * (i % 3 - 1)) + lean).normalized()
            D.add_tooth(b, p - n * rad * s * 0.8, d, rad * s, length * s, tooth[(i + 1) % 3], hook=-FWD,
                        hook_amount=0.3, bone="Head", lod=i % 2 == 1, name="SlimeTooth")
            lowers[x * sx] = p - n * rad * s * 0.5
        for xu, xl, sag in STRANDS:
            a = uppers[xu * sx]
            z = lowers[xl * sx]
            mid = a.lerp(z, 0.5) + FWD * 0.004 * s - UP * sag * s * 0.3
            part = b.tube("Goo", [a, a.lerp(mid, 0.6), mid, z.lerp(mid, 0.6), z],
                          [0.008 * s, 0.0045 * s, 0.003 * s, 0.0045 * s, 0.008 * s], sides=7, style=goo)
            D._tag(part, mid, "Head", lod_strands)
        for x, length in DRIPS:
            if sx < 0 and x == 0.0:
                continue
            p, n = c.out(G(x * sx * s, 0.04 * s, 0.39 * s), G(0, -0.2, 1))
            top = p - n * 0.002 * s
            part = b.tube("Drip", [top, top - UP * length * 0.5 * s + FWD * 0.004 * s, top - UP * length * s],
                          [0.007 * s, 0.0035 * s, 0.0065 * s], sides=7, style=goo)
            D._tag(part, top, "Head", False)


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

OLD_BULBS = [(stalk(n, sx)[2], 0.03) for n in (1, 2) for sx in (1, -1)]


def build(c, blockout_only=False):
    face = face_parts(c)
    if blockout_only:
        return {"Face": face}
    dimmed = S.dim_body(c, OLD_BULBS, emission_keep=0.15, darken=(0.08, 0.07, 0.03), darken_amount=0.45)
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.96), int(lod1 * 0.96), {"Eye": 2.2, "SlimeTooth": 1.3, "Goo": 1.1, "Drip": 1.0})}
    return S.build_all(c, specs, VOXEL, "round 49A: wet slit eyes on the stalk bulbs, translucent slime teeth with goo "
                                        "strands and drips in the slack maw (no limbs: no Claws part)",
                       body_maps=f"round-10 stalk bulbs' bead glow dimmed in Oozer_Albedo/Emission ({dimmed} texels "
                                 f"this run)")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Head", MOUTH, "Stalk_L1", stalk(1, -1)[2], 1.2, walk="Crawl",
                           face_dist=0.9, claw_dist=0.35, foot_dist=0.3)
    out[1] = ("face_macro", "Idle", 15, "Stalk_L1", stalk(1, -1)[2], "close", dict(distance=0.22, azimuth=-35,
                                                                                    elevation=12))
    out[4] = ("mouth", "Idle", 15, "Head", MOUTH, "close", dict(distance=0.35, azimuth=-12, elevation=10))
    out[5] = ("stalks", "Idle", 15, "Head", G(0, 0.33, 0.38), "close", dict(distance=0.5, azimuth=-60, elevation=25))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.9)


def run(norender=False):
    """build_oozer.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
