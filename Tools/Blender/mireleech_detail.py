"""Round 46: the Mireleech's close-up detail (Remco: "do the same for the small enemies").

One extra skinned part (detail_kit / detail_kit_small), exported in Mireleech.fbx and Mireleech_LOD1.fbx:
- Mireleech_Face (Mireleech_Face_Mat): a crescent of three small wet eyespots per side on the head ring behind the
  sucker (real leeches have them; yellow like the sensor pits, slit pupils, sunk under fleshy brow folds), a second
  row of hooked keratin teeth on the inner face of each of the three lip petals (they open and clamp with the
  petals in Latch) and a ring of small teeth raked down the glowing gullet.
The leech has no limbs: there is no Claws part. No bone, clip or event changes; the actions are exported again as
they are.

Run after build_mireleech.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/mireleech_detail.py -- [--before | --blockout | --qa] [--norender]
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

NAME = "Mireleech"
PARTS = ("Face",)
VOXEL = 0.0018

MOUTH = G(0, 0.095, 0.47)
FWD = G(0, 0, 1)
PETALS = {"Petal_T": 0.0, "Petal_R": 2 * math.pi / 3, "Petal_L": -2 * math.pi / 3}

# Colours (sRGB): dark plum-brown plates, lime flesh glow, yellow sensor pits.
EYE = D.Eye(lens=(0.03, 0.028, 0.006), iris=(1.0, 0.9, 0.3), rim=(0.28, 0.22, 0.02), pupil=(0.004, 0.003, 0.0),
            glow=(1.0, 0.88, 0.3), vein=(0.3, 0.2, 0.04))
LID_DARK, LID_SKIN, LID_WET = (0.04, 0.035, 0.03), (0.22, 0.2, 0.14), (0.15, 0.2, 0.04)
TOOTH = D.Keratin(root=(0.2, 0.22, 0.05), body=(0.82, 0.8, 0.62), tip=(0.98, 0.96, 0.86), glow=(0.66, 1.0, 0.2),
                  stain=(0.22, 0.3, 0.05))

HEAD_FOCUS = G(0, 0.12, 0.44)


def petal_points(a):
    d = G(math.sin(a), math.cos(a), 0.0)
    return MOUTH + d * 0.03, MOUTH + d * 0.075 + FWD * 0.022, MOUTH + d * 0.105 + FWD * 0.036


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(c):
    b = C.PartBuilder()
    lid = S.skin_style(LID_DARK, LID_SKIN, LID_WET, seed=12, rough=0.35)
    eyes = [D.eye_style(EYE, pupil="slit", iris_size=0.6, pupil_size=0.32, glow=0.6, seed=s) for s in range(3)]
    # A crescent of eyespots on the head ring, the biggest in front; placed on the right and mirrored.
    for k, (x, fwd, r) in enumerate(((0.03, 0.42, 0.017), (0.055, 0.385, 0.014), (0.07, 0.345, 0.012))):
        look = G(0.5 + 0.25 * k, 1.0, 0.45 - 0.15 * k).normalized()
        p, n = c.out(G(x * 0.4, 0.1, fwd), look)
        n = (n * 0.5 + look * 0.5).normalized()
        for sx in (1, -1):
            S.organic_eye(b, S.flip(p - n * r * 0.15, sx), S.flip(n, sx), r, eyes[k], lid, up=Vector((0, 1, 0.3)),
                          bone="Head", lod=k == 0, brow=(1.2, 0.6, 0.55), lower=None, brow_lift=0.5,
                          segments=(18, 9), lid_segments=(12, 6))
    tooth = [D.tooth_style(TOOTH, glow=0.45, seed=s) for s in range(3)]
    # Two more rows of teeth on each petal's inner face, either side of the round-15 row, raked towards the gullet.
    # The petal is flattened across its centre line, so its inner face lies just inside the line.
    # Built for the top and right petals; the left petal's teeth are the right ones mirrored.
    for i, name in enumerate(("Petal_T", "Petal_R")):
        a = PETALS[name]
        root, _, tip = petal_points(a)
        d = G(math.sin(a), math.cos(a), 0.0)
        side = G(math.cos(a), -math.sin(a), 0.0)          # across the petal
        for j, (tt, off, ln) in enumerate(((0.45, 0.019, 0.03), (0.75, 0.015, 0.025))):
            for o in (off, -off):
                base = root.lerp(tip, tt) - d * 0.008 + side * o
                direction = (-d * 0.85 - FWD * 0.35).normalized()
                for sx in ((1,) if name == "Petal_T" else (1, -1)):
                    bone = name if sx > 0 else "Petal_L"
                    D.add_tooth(b, S.flip(base, sx), S.flip(direction, sx), 0.0055, ln, tooth[(i + j) % 3],
                                hook=-FWD, hook_amount=0.45, bone=bone, lod=False, name="Tooth")
    D.add_ring_teeth(b, MOUTH - FWD * 0.008, -FWD, 0.032, 9, 0.026, 0.005, tooth[0], inward=0.6, bone="Head",
                     lod=False, name="RingTooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(c, blockout_only=False):
    face = face_parts(c)
    if blockout_only:
        return {"Face": face}
    lod0, lod1 = S.budget(c)
    specs = {"Face": (face, int(lod0 * 0.96), int(lod1 * 0.96), {"Eye": 2.2, "Tooth": 1.3, "RingTooth": 1.2})}
    return S.build_all(c, specs, VOXEL, "round 46: wet eyespots, petal teeth, gullet tooth ring (no limbs: no Claws "
                                        "part); bones, clips and events unchanged")


def shots(c):
    out = S.standard_shots("Head", HEAD_FOCUS, "Head", MOUTH, "Head", MOUTH, 1.0, walk="Crawl", face_dist=0.7,
                           claw_dist=0.3, foot_dist=0.3)
    out[1] = ("face_macro", "Idle", 15, "Head", G(-0.04, 0.14, 0.4), "close", dict(distance=0.22, azimuth=-40,
                                                                                   elevation=30))
    out[4] = ("mouth", "Idle", 15, "Head", MOUTH, "close", dict(distance=0.3, azimuth=-8, elevation=6))
    out[5] = ("latch", "Latch", 9, "Head", MOUTH, "close", dict(distance=0.45, azimuth=-20, elevation=10))
    return out


CLIP_FOCUS = ("Head", HEAD_FOCUS, 0.8)


def run(norender=False):
    """build_mireleech.py hook (and the default run)."""
    S.run(NAME, PARTS, build, shots, CLIP_FOCUS, norender=norender)


if __name__ == "__main__":
    S.standard_main(NAME, PARTS, build, shots, CLIP_FOCUS)
