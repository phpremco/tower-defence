"""Round 45: Gravemaw's eyes and talons (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). The round-38 maw (gravemaw_maw.py) is the reference and stays untouched: no teeth here.
Helpers: detail_kit_b.py.

- `Gravemaw_Eyes` (`Gravemaw_Eyes_Mat`): the sensor crown on the head collar, three eyes a side over the build's
  yellow dots (same centres, a size up so the old dots vanish inside) and a fourth pair low on the collar flanks: wet amber eyes with a slit pupil and a hot
  pink ring, in dark lid rings; bone Head.
- `Gravemaw_Claws` (`Gravemaw_Claws_Mat`): every short leg gets two hooked talons at the foot (front and back) and a
  hooked spur on the knee pointing up and out (Leg_*_Lower / Leg_*_Upper); each digging arm gets a big hooked elbow
  spur (Arm_*_Upper) and a dew claw on the back of the spade (Arm_*_Spade). Dark keratin, pale bone towards the tip,
  pink light in the tip.
No bone, clip or event changes; the actions (and the maw) are exported again as they are.

Run: blender -b --factory-startup --python Tools/Blender/gravemaw_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_gravemaw_mech.py (attach(boss) after the maw, before boss.export()).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "Gravemaw"
PARTS = ("Eyes", "Claws")
FACE_BONE, FACE = "Head", (0.0, -2.0, 0.65)
DIST = 3.6

IRIS, EYE_GLOW = (1.0, 0.7, 0.28), (1.0, 0.42, 0.66)       # the build's amber eye, pink light
SCLERA = (0.05, 0.02, 0.03)
LID_DARK, LID_RIM = (0.1, 0.055, 0.07), (0.46, 0.29, 0.31)
KER_ROOT, KER_BODY, KER_TIP = (0.05, 0.03, 0.035), (0.2, 0.14, 0.15), (0.92, 0.86, 0.8)
PINK = (1.0, 0.36, 0.6)

CLIPS = ["Idle", "Crawl", "Roar", "Hit", "Death", "Burrow_Down", "Surface_Throw", "Climb_Up", "Gate_Mount",
         "Dive_Into_Liquid", "Emerge", "Surface_Burst"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def eye_parts(boss):
    b = C.PartBuilder()
    eye = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="slit", glow_k=1.0, seed=3)
    lid = D.lid_style(LID_DARK, LID_RIM, seed=5)
    # The build's collar eyes (build_gravemaw.body_parts): same formula, same centres; a fourth, lower pair on the
    # flanks of the collar so the face also reads from the front three-quarter (the top petal hides the crown).
    collar_c, collar_r = G(0, 0.52, 1.82), (0.6, 0.42, 0.44)
    for i, (dx, du, df) in enumerate(((0.22, 0.8, 0.56), (0.45, 0.66, 0.6), (0.66, 0.42, 0.62),
                                    (0.8, 0.12, 0.5))):
        d = G(dx, du, df)
        t = 1.0 / math.sqrt((d.x / collar_r[0]) ** 2 + (d.y / collar_r[1]) ** 2 + (d.z / collar_r[2]) ** 2)
        c = collar_c + d * t * 0.98
        D.eye(b, "Eye", c, d.normalized(), 0.092 - i * 0.01, "Head", eye, squash=(1.0, 0.92, 0.72),
              lid=(1.03, 0.22, 0.3), lid_style_fn=lid)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=PINK, glow_at="tip", glow_k=0.85, seed=21, wet=0.3,
                            bands=9)
    for i in (1, 2, 3):
        knee, foot = boss.bone(f"Leg_R{i}_Lower")
        for fy in (-1.0, 1.0):
            out = Vector((0.55, fy, 0.0)).normalized()
            D.claw(b, "FootTalon", foot + Vector((0, 0, 0.09)) + out * 0.03, (out + Vector((0, 0, 0.1))).normalized(),
                   0.045, 0.22 if fy < 0 else 0.17, f"Leg_R{i}_Lower", talon, hook=Vector((0, 0, -1)), hook_k=0.6)
        D.claw(b, "KneeSpur", knee + Vector((0.02, 0, 0.09)), Vector((0.55, 0.25, 1.0)).normalized(), 0.05, 0.26,
               f"Leg_R{i}_Upper", talon, hook=Vector((1, 0.3, 0)), hook_k=0.35)
    sh, el = boss.bone("Arm_R_Upper")
    D.claw(b, "ElbowSpur", el + Vector((0.04, 0.03, 0.1)), Vector((0.6, 0.5, 0.9)).normalized(), 0.065, 0.36,
           "Arm_R_Upper", talon, hook=Vector((0.3, 1.0, 0)), hook_k=0.4)
    wr, sp = boss.bone("Arm_R_Spade")
    D.claw(b, "DewClaw", wr + Vector((0.06, 0.05, 0.07)), Vector((0.5, 0.6, 0.6)).normalized(), 0.04, 0.2,
           "Arm_R_Spade", talon, hook=Vector((0, 0, -1)), hook_k=0.5)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1000, lod_tris=300, voxel=0.004, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="sensor crown: three amber eyes a side")),
          ("Claws", claw_parts, dict(tris=1700, lod_tris=520, voxel=0.004, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5,
                                     note="foot talons, knee and elbow spurs, dew claws"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def attach(mboss):
    """build_gravemaw_mech.py hook: bake the parts and hand them to its export."""
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
        D.blockout(boss, claw_parts(boss), "Claws", (0.2, 0.4, 1.0), (0.2, 0.4, 1))
        D.qa(boss, "blockout", FACE_BONE, FACE, DIST)
        return
    if args.get("qa"):
        after_qa(boss)
        return
    D.run(boss, MAKERS, args, after_qa)


if __name__ == "__main__":
    main()
