"""Round 45: The Crystal Warlord's face and claws (Remco: "restyle the detail features on all the bosses - think
claws, teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Before, the eyes were amber slits painted under the visor. Now:
- `CrystalWarlord_Eyes` (`CrystalWarlord_Eyes_Mat`): three wet amber slit-pupil eyes a side under the visor, in a
  row, sunk in mounds of green hide with wet dark rims (round 45 polish: no rings); bone Head.
- `CrystalWarlord_Teeth` (`CrystalWarlord_Teeth_Mat`): (round 49G) two interlocking rows of matte ivory teeth rooted
  in the gums of the shared Bulwark mouth (mouth49g.py), upper on Head, lower on Jaw.
- `CrystalWarlord_Claws` (`CrystalWarlord_Claws_Mat`): three hooked toe talons and a heel spur per foot
  (Leg_*_Foot), pink-hot tips.
No bone, clip or event changes; the actions are exported again as they are. The body's Corrupted skin
(Skins/Corrupted) has no counterpart for the new parts: they keep their own maps.

Run: blender -b --factory-startup --python Tools/Blender/crystalwarlord_detail.py -- [--before | --blockout | --qa]
     [--norender]
Standalone: build_crystalwarlord.py runs main() of this script after its own main().
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "CrystalWarlord"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -0.8, 1.82)
DIST = 2.4

IRIS, EYE_GLOW = (1.0, 0.62, 0.15), (1.0, 0.86, 0.45)        # the species' amber eyes
SCLERA = (0.03, 0.04, 0.02)
LID_DARK, LID_SKIN, LID_WET = (0.06, 0.085, 0.035), (0.2, 0.28, 0.11), (0.05, 0.05, 0.02)
PINK, PINK_HOT = (1.0, 0.45, 0.8), (1.0, 0.92, 0.98)
KER_ROOT, KER_BODY, KER_TIP = (0.04, 0.05, 0.03), (0.16, 0.18, 0.12), (1.0, 0.7, 0.9)

CLIPS = ["Idle", "Walk", "Sweep", "Block", "Hit", "Death"]


def eye_parts(boss):
    b = C.PartBuilder()
    eye = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="slit", glow_k=1.0, seed=3)
    # Round 45 polish: plated sockets under the visor (no ring lids): the lens breaks through a mound of green
    # hide, wet and dark at the rim.
    mound = D.fold_style(LID_SKIN, LID_DARK, wet=LID_WET, facing=0.0, seed=5)
    # Three eyes a side in a row under the visor (where the build painted its amber slits), biggest at the front.
    for y, z, r in ((-0.78, 1.87, 0.036), (-0.7, 1.885, 0.031), (-0.62, 1.9, 0.027)):
        probe = Vector((0.14, y, z))
        look = Vector((1.0, -0.75, 0.05)).normalized()
        p, n = boss.ray(probe + look, -look)
        if p is None:
            p, n = boss.nearest(probe)
        D.eye(b, "Eye", p - n * r * 0.3, n.lerp(look, 0.5), r, "Head", eye, squash=(1.0, 0.7, 0.75),
              socket=dict(mound=mound, size=(1.4, 1.6, 0.85), sink=0.35))
    return b


def teeth_parts(boss):
    """Round 49G (Remco on the Bulwark, whose head and mouth this boss shares: "those teeth are strange with the new
    mouth"): the 49F Crysium fangs stood on the front of the snout. Now two interlocking rows of matte ivory teeth
    rooted in the gums of build_bulwark's mouth (mouth49g.add_teeth), at boss scale (build_crystalwarlord.M): upper on
    Head, lower on Jaw, each stopping short of the other gum with the jaw at rest. Old ivory with a faint pink-violet
    stain at the roots (the crystal in its blood), never glassy."""
    import build_bulwark as BW
    import build_crystalwarlord as CW
    import mouth49g as MO
    b = C.PartBuilder()
    # Round 56: paler ivory with a darker root (mouth49g.IVORY_BRIGHT) against the darkened gums.
    style = MO.ivory_style(11, stain=(0.3, 0.1, 0.2), **MO.IVORY_BRIGHT)
    placed = MO.add_teeth(b, BW.MOUTH, CW.M, style, lambda part, bone, p: None, name="Fang")
    C.log("crystalwarlord teeth", placed)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=PINK, glow_at="tip", glow_k=0.9, seed=21, wet=0.3,
                            bands=8)
    ankle, toe = boss.bone("Leg_R_Foot")
    for k, deg in enumerate((-35.0, 0.0, 35.0)):
        phi = math.radians(deg)
        out = Vector((math.sin(phi), -math.cos(phi), 0.0))
        base = toe + out * 0.06 + Vector((0, 0, 0.07))
        D.claw(b, "ToeTalon", base, (out + Vector((0, 0, 0.15))).normalized(), 0.032, 0.2 + 0.03 * (k == 1),
               "Leg_R_Foot", talon, hook=Vector((0, 0, -1)), hook_k=0.6)
    # A hooked dew-claw spur on each heel, pointing back.
    D.claw(b, "HeelSpur", ankle + Vector((0.0, 0.06, 0.02)), Vector((0.15, 1.0, 0.2)).normalized(), 0.03, 0.16,
           "Leg_R_Foot", talon, hook=Vector((0, 0, -1)), hook_k=0.5)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=800, lod_tris=250, voxel=0.002, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="three amber slit eyes a side under the visor")),
          ("Teeth", teeth_parts, dict(tris=700, lod_tris=250, voxel=0.002, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.4, note="round 49G: ivory teeth rooted in the gums")),
          ("Claws", claw_parts, dict(tris=900, lod_tris=340, voxel=0.0025, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="toe talons + heel spurs"))]


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
