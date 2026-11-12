"""Round 45: The Core Wisp's face and claws (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

An energy being, so no mouth and no teeth:
- `CoreWisp_Eyes` (`CoreWisp_Eyes_Mat`): one great core eye filling the glowing front slit (ice-white iris with
  concentric light rings, round pupil), on the Heart bone so it swells and flashes with the heart in Pulse, and two
  small sentinel eyes high in the slit under the brow horns.
- `CoreWisp_Barbs` (`CoreWisp_Barbs_Mat`): short hooked barbs down both edges of the slit (Petal_1 / Petal_6), so
  the eye sits in a dangerous socket and the barbs open with the petals.
- `CoreWisp_Claws` (`CoreWisp_Claws_Mat`): a three-hooked grapple at the tip of each of the five tendrils
  (Tendril_*_2), ice-lit tips; they plant with the tendrils in Land/Grounded.
No bone, clip or event changes; the actions are exported again as they are. The Corrupted skin (Skins/Corrupted)
has no counterpart for the new parts.

Run: blender -b --factory-startup --python Tools/Blender/corewisp_detail.py -- [--before | --blockout | --qa]
     [--norender]
Standalone: build_corewisp.py runs main() of this script after its own main().
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "CoreWisp"
PARTS = ("Eyes", "Barbs", "Claws")
FACE_BONE, FACE = "Core", (0.0, -0.3, 0.84)
DIST = 2.4

ICE, WHITE_HOT = (0.5, 0.74, 1.0), (0.92, 0.97, 1.0)          # the Wisp's light (palette "cool")
IRIS = (0.55, 0.8, 1.0)
SCLERA = (0.05, 0.03, 0.05)
LID_DARK, LID_RIM = (0.1, 0.06, 0.08), (0.36, 0.24, 0.3)
KER_ROOT, KER_BODY, KER_TIP = (0.06, 0.035, 0.05), (0.22, 0.14, 0.18), (0.85, 0.94, 1.0)

CLIPS = ["Float", "Land", "Grounded", "Pulse", "Hit", "Death"]
CENTRE = Vector((0.0, 0.0, 0.837))
FRONT = Vector((0.0, -1.0, 0.0))


def eye_parts(boss):
    b = C.PartBuilder()
    core = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="round", glow_k=1.0, seed=3, rings=0.8)
    small = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="slit", glow_k=1.0, seed=4)
    # An energy being: no mouth, but one great core eye that looks out of the glowing slit at the front (on the
    # Heart bone, so it swells and flashes with the heart in Pulse), tall to fill the slit (local x is up).
    p, n = boss.ray(CENTRE + FRONT * 1.0, -FRONT)
    c = (p if p is not None else CENTRE + FRONT * 0.3) - FRONT * 0.06
    D.eye(b, "CoreEye", c, (FRONT + Vector((0, 0, 0.12))).normalized(), 0.13, "Heart", core,
          squash=(1.35, 0.72, 0.62), both=False, up=Vector((1, 0, 0)))
    # Two small sentinel eyes high in the slit, under the brow horns.
    for z, r in ((1.06, 0.04),):
        probe = Vector((0.07, -0.25, z))
        look = Vector((0.35, -1.0, 0.45)).normalized()
        q, m = boss.ray(probe + look, -look)
        if q is None:
            q, m = boss.nearest(probe)
        D.eye(b, "SmallEye", q - m * r * 0.3, m.lerp(look, 0.5), r, "Heart", small, squash=(1.0, 0.9, 0.75))
    return b


def teeth_parts(boss):
    """No teeth (an energy being has no mouth): the slit's rim gets a ring of short hooked barbs instead, so the eye
    sits in a dangerous socket: four a side down the edges of the front petals (Petal_1 / Petal_6)."""
    b = C.PartBuilder()
    barb = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=ICE, glow_at="tip", glow_k=0.9, seed=11, wet=0.25,
                           bands=5)
    for z in (0.66, 0.76, 0.88, 0.98):
        probe = Vector((0.085, -0.4, z))
        q, m = boss.ray(probe, Vector((0, 1, 0)))
        if q is None:
            continue
        base, direction = q - m * 0.012, Vector((-0.8, -0.6, 0.0)).normalized()
        # Petal_1 is the right front petal, Petal_6 its mirror (not an _L/_R pair, so placed by hand).
        for sx, bone in ((1, "Petal_1"), (-1, "Petal_6")):
            D.fang(b, "SlitBarb", D.flip(base, sx), D.flip(direction, sx), 0.022, 0.085, bone, barb,
                   hook=Vector((0, 1, 0)), hook_k=0.35, both=False)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=ICE, glow_at="tip", glow_k=1.0, seed=21, wet=0.3,
                            bands=8)
    # Each tendril ends in a three-hooked grapple (it plants these as feet when it lands). Built per tendril, not
    # mirrored by name (Tendril_1..5 are not _L/_R pairs); the rig is mirror-symmetric, so the claws are too.
    for i in range(1, 6):
        knee, foot = boss.bone(f"Tendril_{i}_2")
        d = (foot - knee).normalized()
        out = Vector((foot.x, foot.y, 0.0))
        out = out.normalized() if out.length > 1e-4 else Vector((0, 1, 0))
        side = d.cross(out).normalized()
        for k, (sd, ln) in enumerate(((0.0, 0.17), (0.7, 0.12), (-0.7, 0.12))):
            direction = (d + side * sd + out * 0.25).normalized()
            D.claw(b, "Grapple", foot - d * 0.03, direction, 0.024 if k == 0 else 0.019, ln, f"Tendril_{i}_2", talon,
                   hook=(-out + Vector((0, 0, -0.5))).normalized(), hook_k=0.6, both=False)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=700, lod_tris=220, voxel=0.0025, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="one great core eye in the slit + two sentinel eyes")),
          ("Barbs", teeth_parts, dict(tris=300, lod_tris=100, voxel=0.002, families=("chitin", "membrane"),
                                      emission_strength=2.5, specular=0.6, note="hooked barbs round the slit (no mouth)")),
          ("Claws", claw_parts, dict(tris=700, lod_tris=240, voxel=0.0025, families=("chitin", "membrane"),
                                     emission_strength=2.5, specular=0.5, note="three-hooked grapples on the five tendril tips"))]


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
        D.blockout(boss, teeth_parts(boss), "Barbs", (0.2, 0.9, 0.3), (0.2, 1, 0.3))
        D.blockout(boss, claw_parts(boss), "Claws", (1.0, 0.4, 0.2), (1, 0.4, 0.2))
        D.qa(boss, "blockout", FACE_BONE, FACE, DIST)
        return
    if args.get("qa"):
        after_qa(boss)
        return
    D.run(boss, MAKERS, args, after_qa)


if __name__ == "__main__":
    main()
