"""Round 45: The Sealed One's face and claws (Remco: "restyle the detail features on all the bosses - think
claws, teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

The head is eyeless by design (two face plates over a column of violet light), so the face is put where it keeps
that identity:
- `SealedOne_Eyes` (`SealedOne_Eyes_Mat`): one tall eye (horizontal slit pupil, violet-white iris) set deep in the
  light seam between the face plates, and a small round one above it; bone Head (fully seen when the plates part).
- `SealedOne_Teeth` (`SealedOne_Teeth_Mat`): ivory fangs on the inner edge of each face plate (Plate_R / Plate_L),
  interlocking across the lower seam: a vertical maw that opens with the plates; violet light at the roots.
- `SealedOne_Claws` (`SealedOne_Claws_Mat`): hooked talons over the three fingertips of each planted hand
  (LArm_*_Hand) and a hooked spur on each lower elbow (LArm_*_Upper), violet-hot tips.
No bone, clip or event changes; the actions are exported again as they are.

Run: blender -b --factory-startup --python Tools/Blender/sealedone_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_sealedone_mech.py (attach(boss) before boss.export()).
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "SealedOne"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -0.8, 4.1)
DIST = 5.6

VIOLET, WHITE_HOT = (0.62, 0.26, 1.0), (0.95, 0.85, 1.0)      # the being's light (the build's VIOLET)
IRIS = (0.75, 0.4, 1.0)
SCLERA = (0.04, 0.02, 0.06)
LID_DARK, LID_RIM = (0.2, 0.17, 0.2), (0.86, 0.81, 0.72)        # bone-stone
BONE_ROOT, BONE_BODY, BONE_TIP = (0.16, 0.1, 0.2), (0.82, 0.77, 0.68), (0.97, 0.95, 0.9)
KER_ROOT, KER_BODY, KER_TIP = (0.06, 0.05, 0.08), (0.2, 0.18, 0.22), (0.85, 0.7, 1.0)

CLIPS = ["Idle", "Rise", "Summon", "Hit", "Death", "Adapt", "Roar", "Climb_Up", "Dive_Into_Liquid", "OnBase_Loop"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def front(boss, x, z):
    p, n = boss.ray(Vector((x, -6.0, z)), Vector((0, 1, 0)))
    return p, n


def eye_parts(boss):
    b = C.PartBuilder()
    big = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="slit", glow_k=1.0, seed=3, rings=0.5)
    small = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="round", glow_k=1.0, seed=4)
    # The head stays "eyeless" from the side: what watches sits in the column of light between the face plates,
    # a tall vertical eye with a horizontal slit (turned 90 degrees: x is up), and two small ones under it.
    # Local x of these eyes is world up (up=X below), so squash[0] is the height.
    for z, r, st, sq in ((4.42, 0.34, big, (1.0, 0.42, 0.5)), (4.9, 0.09, small, (1.0, 0.8, 0.6))):
        p, n = front(boss, 0.0, z)
        if p is None:
            continue
        look = Vector((0, -1, 0.15)).normalized()
        D.eye(b, "Eye", p - n * r * sq[2] * 0.45, look, r, "Head", st, squash=sq, both=False, up=Vector((1, 0, 0)))
    return b


def teeth_parts(boss):
    b = C.PartBuilder()
    fang = D.keratin_style(BONE_ROOT, BONE_BODY, BONE_TIP, glow=VIOLET, glow_at="root", glow_k=0.85, seed=11,
                           wet=0.22, bands=5, family=(0.5, 0.0, 0.0, 0.0))
    # A maw down the lower seam: ivory fangs on the inner edge of each face plate (Plate_R / Plate_L), interlocking
    # across the violet column; when the plates part (Summon, Rise) the fangs part with them like jaws.
    # Round 45 polish: half as thick again, so the seam fangs read at game zoom.
    for z, ln, r in ((4.02, 0.26, 0.063), (3.86, 0.34, 0.075), (3.7, 0.36, 0.078), (3.55, 0.3, 0.069),
                     (3.42, 0.22, 0.06)):
        p, n = front(boss, 0.11, z)
        if p is None:
            continue
        base = p - n * 0.03
        D.fang(b, "SeamFang", base, Vector((-1.0, -0.6, -0.3)).normalized(), r, ln, "Plate_R", fang,
               hook=Vector((0, 0, -1)), hook_k=0.3)
    return b


def claw_parts(boss):
    import build_sealedone as BSO
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=VIOLET, glow_at="tip", glow_k=0.9, seed=21, wet=0.3,
                            bands=9)
    lg = BSO.lower_arms()["R"]
    sh, el, wr = lg.rest_points
    hand = wr + G(0, -0.1, 0.15)
    for j, spread in enumerate((-0.45, 0.0, 0.45)):
        d = G(spread * 0.8 + 0.15, 0, 1.0).normalized()
        r0 = hand + d * 0.3
        tip = r0 + d * 1.1 - G(0, 0.15, 0)
        base = r0 + d * 0.86 - G(0, 0.04, 0)
        D.claw(b, "FingerTalon", base, (tip - base).normalized(), 0.075, 0.5, "LArm_R_Hand", talon,
               hook=Vector((0, 0, -1)), hook_k=0.55)
    # A hooked spur on each lower elbow, pointing back and up (reads from above).
    D.claw(b, "ElbowSpur", el + G(0.2, 0.25, -0.1), G(0.5, 0.7, -0.6).normalized(), 0.09, 0.5, "LArm_R_Upper",
           talon, hook=G(0, 0, 1), hook_k=0.4)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1200, lod_tris=340, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="a tall eye in the light seam + a small one above")),
          ("Teeth", teeth_parts, dict(tris=1100, lod_tris=340, voxel=0.004, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.6, note="ivory seam fangs on the face plates")),
          ("Claws", claw_parts, dict(tris=1800, lod_tris=560, voxel=0.006, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="fingertip talons + elbow spurs"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def attach(mboss):
    """build_sealedone_mech.py hook: bake the parts and hand them to its export."""
    return D.attach(mboss, PARTS, MAKERS)


def main():
    D.use_gpu()
    args = C.cli_args()
    boss = D.Boss(NAME, PARTS)
    if args.get("before"):
        D.qa(boss, "before", FACE_BONE, FACE, DIST)
        return
    if args.get("blockout"):
        D.blockout(boss, eye_parts(boss), "Eyes", (0.9, 0.9, 0.9), (1, 1, 1))
        D.blockout(boss, teeth_parts(boss), "Teeth", (0.2, 0.9, 0.3), (0.2, 1, 0.3))
        D.blockout(boss, claw_parts(boss), "Claws", (1.0, 0.4, 0.2), (1, 0.4, 0.2))
        D.qa(boss, "blockout", FACE_BONE, FACE, DIST)
        return
    if args.get("qa"):
        after_qa(boss)
        return
    D.run(boss, MAKERS, args, after_qa)


if __name__ == "__main__":
    main()
