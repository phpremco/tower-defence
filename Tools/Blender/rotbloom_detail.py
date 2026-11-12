"""Round 45: Rotbloom's face and claws (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Before, the bark skull had four small amber dots for eyes and a lime rim for a mouth. Now:
- `Rotbloom_Eyes` (`Rotbloom_Eyes_Mat`): two big wet amber eyes a side over the old dots (ringed irises, round
  pupils) sunk in bark sockets under the skull's ridges (round 45 polish; the old dots are cut from the
  body), and a spider cluster of three small slit-pupil eyes higher on the skull; bone Head.
- `Rotbloom_Teeth` (`Rotbloom_Teeth_Mat`): thorn teeth: a row hanging from the skull rim over the lime maw (Head) and
  a row rising from the front of the shovel jaw (Jaw); dark wood roots with a lime glow, pale wet bone tips.
- `Rotbloom_Claws` (`Rotbloom_Claws_Mat`): three hooked root talons round each of the six feet (Leg_*_Lower) and a
  thorn spur on every knee (Leg_*_Upper).
No bone, clip or event changes; the actions are exported again as they are.

Run: blender -b --factory-startup --python Tools/Blender/rotbloom_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_rotbloom_mech.py (attach(boss) before boss.export()).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "Rotbloom"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -1.85, 0.7)
DIST = 3.4

IRIS, EYE_GLOW = (1.0, 0.66, 0.16), (1.0, 0.9, 0.4)         # amber eyes (the build's eye colour)
SCLERA = (0.04, 0.035, 0.015)
LID_DARK, LID_SKIN, LID_WET = (0.08, 0.06, 0.04), (0.3, 0.24, 0.15), (0.1, 0.09, 0.03)   # bark, wet rot rim
# The build's four amber dots (build_rotbloom.body_parts): cut out under the new eyes.
OLD_EYES = [((sx * x, -f, u), r * 1.3) for sx in (1, -1) for x, u, f, r in ((0.3, 0.95, 1.92, 0.06),
                                                                           (0.42, 0.85, 1.86, 0.048))]
THORN_ROOT, THORN_BODY, THORN_TIP = (0.07, 0.05, 0.03), (0.3, 0.24, 0.15), (0.9, 0.86, 0.66)
LIME = (0.72, 1.0, 0.18)
ROT_STAIN = (0.25, 0.32, 0.06)

CLIPS = ["Idle", "Crawl", "Hit", "Death", "Regrow", "Bloom", "Climb_Up", "Dive_Into_Liquid", "Gate_Mount",
         "Submerged_Loop"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def eye_parts(boss):
    b = C.PartBuilder()
    eye = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="round", glow_k=1.0, seed=3, rings=0.6)
    small = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="slit", glow_k=0.9, seed=4)
    # Round 45 polish: organic bark sockets (a mound the lens breaks through, wet at the rim) instead of ring lids;
    # the skull's own bark ridges are the brow.
    mound = D.fold_style(LID_SKIN, LID_DARK, wet=LID_WET, facing=0.0, seed=5, ridges=20)
    # Over the build's amber dots (same centres, bigger), then a spider cluster of three small ones above them.
    for x, u, f, r, st in ((0.3, 0.95, 1.92, 0.1, eye), (0.43, 0.84, 1.85, 0.08, eye)):
        look = G(0.45 * (x / 0.3), 0.15, 1.0).normalized()
        socket = dict(mound=mound, size=(1.4, 1.5, 0.9), sink=0.3)
        D.eye(b, "Eye", G(x, u, f) - look * 0.015, look, r, "Head", st, squash=(1.0, 0.82, 0.8), socket=socket)
    for x, u, f, r in ((0.17, 1.12, 1.8, 0.05), (0.3, 1.13, 1.72, 0.045), (0.08, 1.16, 1.7, 0.04)):
        probe = G(x, u, f)
        look = G(0.3 + x, 0.7, 0.7).normalized()
        p, n = boss.ray(probe + look, -look)
        if p is None:
            p, n = boss.nearest(probe)
        D.eye(b, "EyeSmall", p - n * r * 0.3, n.lerp(look, 0.4), r, "Head", small, squash=(1.0, 0.85, 0.8),
              socket=dict(mound=mound, size=(1.4, 1.5, 0.85), sink=0.35))
    return b


def teeth_parts(boss):
    b = C.PartBuilder()
    thorn = D.keratin_style(THORN_ROOT, THORN_BODY, THORN_TIP, glow=LIME, glow_at="root", glow_k=0.45, seed=11,
                            stain=ROT_STAIN, wet=0.18, bands=6)

    def front(x, z):
        p, n = boss.ray(Vector((x, -4.0, z)), Vector((0, 1, 0)))
        return (p - n * 0.03, n) if p is not None else (None, None)

    def top(x, f):
        p, n = boss.ray(Vector((x, -f, 3.0)), Vector((0, 0, -1)))
        return p - n * 0.025 if p is not None else None
    # Upper row: thorn teeth hanging from the skull's rim over the lime maw (Head).
    for x, ln, r in ((0.05, 0.2, 0.034), (0.15, 0.24, 0.038), (0.25, 0.19, 0.032), (0.34, 0.15, 0.028)):
        p, n = front(x, 0.76)
        if p is not None:
            D.fang(b, "UpperThorn", p, G(0.05, -1.0, 0.3).normalized(), r, ln, "Head", thorn, hook=G(0, 0, -1),
                   hook_k=0.3)
    # Lower row: rising from the front of the shovel jaw (Jaw), hooked back into the mouth.
    for x, ln, r in ((0.1, 0.2, 0.036), (0.22, 0.23, 0.038), (0.34, 0.18, 0.032), (0.45, 0.13, 0.028)):
        p = top(x, 2.28 - 0.22 * (x / 0.45) ** 2)
        if p is not None:
            D.fang(b, "JawThorn", p, G(-0.04, 1.0, -0.25).normalized(), r, ln, "Jaw", thorn, hook=G(0, 0, -1),
                   hook_k=0.35)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(THORN_ROOT, THORN_BODY, THORN_TIP, glow=LIME, glow_at="tip", glow_k=0.35, seed=21,
                            stain=ROT_STAIN, wet=0.35, bands=9)
    for leg in ("Leg_R1_Lower", "Leg_R2_Lower", "Leg_R3_Lower"):
        knee, foot = boss.bone(leg)
        fwd = -foot.y
        for k, deg in enumerate((-45.0, 10.0, 60.0)):
            phi = math.radians(deg) * (1 if fwd >= -0.2 else -1)
            out = Vector((math.cos(phi), -math.sin(phi), 0.0))
            base = foot + out * 0.07 + Vector((0, 0, 0.1))
            D.claw(b, "RootTalon", base, (out + Vector((0, 0, 0.25))).normalized(), 0.05, 0.27 + 0.05 * (k == 1), leg,
                   talon, hook=Vector((0, 0, -1)), hook_k=0.65)
        # A thorn spur on the knee, pointing up and out (reads from the game camera).
        D.fang(b, "KneeSpur", knee + Vector((0.03, 0, 0.08)), Vector((0.5, 0.15 * (1 if fwd > 0 else -1), 1.0)),
               0.04, 0.22, leg.replace("Lower", "Upper"), talon, hook=Vector((1, 0, 0)), hook_k=0.3)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1200, lod_tris=340, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="amber ringed eyes over the old dots + a spider cluster of three")),
          ("Teeth", teeth_parts, dict(tris=1100, lod_tris=340, voxel=0.004, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.6, note="thorn teeth over the lime maw + on the shovel jaw")),
          ("Claws", claw_parts, dict(tris=1800, lod_tris=560, voxel=0.006, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="root talons + knee spurs"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def prep(boss):
    D.cut_spheres(boss, OLD_EYES)


def attach(mboss):
    """build_rotbloom_mech.py hook: bake the parts and hand them to its export."""
    return D.attach(mboss, PARTS, MAKERS, prep)


def main():
    D.use_gpu()
    args = C.cli_args()
    boss = D.Boss(NAME, PARTS)
    if args.get("before"):
        D.qa(boss, "before", FACE_BONE, FACE, DIST)
        return
    if args.get("blockout"):
        prep(boss)
        D.blockout(boss, eye_parts(boss), "Eyes", (0.9, 0.9, 0.9), (1, 1, 1))
        D.blockout(boss, teeth_parts(boss), "Teeth", (0.2, 0.9, 0.3), (0.2, 1, 0.3))
        D.blockout(boss, claw_parts(boss), "Claws", (1.0, 0.4, 0.2), (1, 0.4, 0.2))
        D.qa(boss, "blockout", FACE_BONE, FACE, DIST)
        return
    if args.get("qa"):
        after_qa(boss)
        return
    D.run(boss, MAKERS, args, after_qa, prep)


if __name__ == "__main__":
    main()
