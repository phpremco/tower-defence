"""The Sealed One's detail parts: eyes, teeth and claws (round 45 technique: gravemaw_maw.py; helpers:
detail_kit_b.py). Each is its own skinned mesh with its own material and maps (Unity ExtraParts Eyes, Teeth, Claws).

Round 52 (concept B, "The opening seal"; the head is build_sealedone.py's hood with the three-petal maw):
- `SealedOne_Eyes` (`SealedOne_Eyes_Mat`): three pairs of small violet eyes in dark sockets on the hood's brow, in an
  arc over the gold glyph band that rings the maw (they say "this is the front of the head"; the maw is the face).
- `SealedOne_Teeth` (`SealedOne_Teeth_Mat`): matte ivory teeth with dark roots (mouth49g.ivory_style), rooted in the
  wet gums: a row along both edges of every petal's inner face (on the petal's bone: Plate_R, Plate_L, Jaw) pointing
  in towards the maw's axis and hooked back to the throat, and two inner rings round the funnel (Head) pointing in.
  At rest the petals stand REST_OPEN apart and no clip closes them further, so the tips (TOOTH_CLEARANCE) never reach
  another petal's tooth, another petal or the far wall.
- `SealedOne_Claws` (`SealedOne_Claws_Mat`): hooked talons over the three fingertips of each planted hand
  (LArm_*_Hand) and a hooked spur on each lower elbow (LArm_*_Upper), violet-hot tips.

Run: blender -b --factory-startup --python Tools/Blender/sealedone_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_sealedone_mech.py (attach(boss) before boss.export()).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402
import mouth49g as MO  # noqa: E402

NAME = "SealedOne"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -0.95, 3.95)
DIST = 5.6

VIOLET, WHITE_HOT = (0.62, 0.26, 1.0), (0.95, 0.85, 1.0)      # the being's light (the build's VIOLET)
IRIS = (0.75, 0.4, 1.0)
SCLERA = (0.04, 0.02, 0.06)
SOCKET_DARK, LID_DARK, LID_RIM = (0.04, 0.025, 0.05), (0.12, 0.1, 0.13), (0.3, 0.27, 0.3)
KER_ROOT, KER_BODY, KER_TIP = (0.06, 0.05, 0.08), (0.2, 0.18, 0.22), (0.85, 0.7, 1.0)

CLIPS = ["Idle", "Rise", "Summon", "Hit", "Death", "Adapt", "Roar", "Climb_Up", "Dive_Into_Liquid", "OnBase_Loop"]

# Petal teeth: (s along the petal, length share of the petal's inner radius there, radius); one row per petal edge,
# the two edges of a petal offset by half a step so the rows of neighbouring petals interlock across each seam.
PETAL_TEETH = [(0.12, 0.42, 0.05), (0.25, 0.44, 0.052), (0.38, 0.42, 0.048), (0.51, 0.4, 0.042), (0.63, 0.38, 0.036)]
INNER_TEETH = [(9, 0.17, 0.04, 0.0), (8, 0.13, 0.034, 22.5)]     # per throat gum ring: count, length, radius, phase
TOOTH_CLEARANCE = 0.06


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def eye_parts(boss):
    import build_sealedone as BS
    b = C.PartBuilder()
    big = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="slit", glow_k=1.5, seed=3, rings=0.4)
    small = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="round", glow_k=1.4, seed=4)
    mound = D.fold_style(SOCKET_DARK, LID_DARK, wet=(0.12, 0.05, 0.16), facing=0.0, seed=6)
    brow = D.fold_style(LID_RIM, LID_DARK, wet=(0.12, 0.05, 0.16), facing=-1.0, seed=8, ridges=10)
    z = -0.4
    for deg, r, style, with_brow in ((22, 0.085, big, True), (44, 0.07, small, True), (66, 0.055, small, False)):
        phi = math.radians(deg)
        p = BS.maw_point(BS.hood_ro(z, phi), phi, z)
        out = (BS.radial(phi) * 0.55 + BS.D * 0.8).normalized()
        look = (BS.D + BS.radial(phi) * 0.35 + BS.E_UP * 0.1).normalized()
        sock = dict(mound=mound, size=(1.45, 1.9, 1.0), sink=0.35)
        if with_brow:
            sock.update(brow=brow, brow_lift=0.95, brow_tilt=0.5, brow_size=(1.9, 0.8, 1.3),
                        brow_deform=C.chiselled(9, 41 + deg, 0.12, squash=(0.8, 1.0)))
        D.eye(b, "BrowEye", p + out * (r * 0.25), look, r, "Head", style, squash=(1.0, 0.6, 0.65), socket=sock)
    return b


def teeth_parts(boss):
    """Petal rows (on Plate_R / Plate_L / Jaw) and the two throat rings (Head); see the module notes."""
    import build_sealedone as BS
    b = C.PartBuilder()
    fang = MO.ivory_style(seed=11, stain=VIOLET)
    for bone, phi in BS.PETALS:
        if bone == "Plate_L":
            continue  # built as the mirror of Plate_R (D.fang both=True)
        # The lower petal's -1 edge is its right-hand (+x) one; its left edge is the mirror.
        for edge in ((-1, 1) if bone == "Plate_R" else (-1,)):
            w = edge * (1.0 - BS.PETAL_GUM_INSET)
            for s, share, rad in PETAL_TEETH:
                s_ = s + (0.065 if edge > 0 else 0.0)
                root = BS.petal_inner_point(phi, s_, w)
                inward, along = BS.petal_dirs(phi, s_, w)
                # In towards the axis, hooked back to the throat, a little towards the petal's middle.
                across = BS.petal_inner_point(phi, s_, 0.0) - root
                across = across.normalized() if across.length > 1e-6 else Vector()
                d = (inward - along * 0.3 + across * 0.15).normalized()
                ln = max(0.07, share * BS.petal_r(s_) - TOOTH_CLEARANCE * 0.5)
                D.fang(b, "PetalTooth", root - inward * BS.GUM_R * 0.4, d, rad, ln, bone, fang, hook=-along,
                       hook_k=0.18, sides=8, flute=0.06)
    for j, ((z, r), (count, ln, rad, phase)) in enumerate(zip(BS.INNER_GUMS, INNER_TEETH)):
        for i in range(count):
            phi = math.radians(phase + 360.0 * i / count)
            if phi > math.pi + 1e-6:
                continue  # the right half and the midline; the left half is the mirror (D.fang both=True)
            root = BS.maw_point(r - BS.GUM_R * 0.3, phi, z)
            d = (-BS.radial(phi) + BS.D * 0.35).normalized()
            mid = abs(math.sin(phi)) < 1e-4
            D.fang(b, "ThroatTooth", root, d, rad, ln, "Head", fang, hook=BS.D, hook_k=0.15, both=not mid, sides=8,
                   flute=0.06)
    return b


def claw_parts(boss):
    import build_sealedone as BS
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=VIOLET, glow_at="tip", glow_k=0.9, seed=21, wet=0.3,
                            bands=9)
    hand = BS.hand_centre("R")
    for d in BS.finger_dirs(1):
        r0 = hand + d * 0.34
        tip = r0 + d * 1.2 - G(0, 0.16, 0)
        base = r0 + d * 0.92 - G(0, 0.04, 0)
        D.claw(b, "FingerTalon", base, (tip - base).normalized(), 0.085, 0.58, "LArm_R_Hand", talon,
               hook=Vector((0, 0, -1)), hook_k=0.55)
    lg = BS.lower_arms()["R"]
    sh, el, wr = lg.rest_points
    D.claw(b, "ElbowSpur", el + G(0.22, 0.28, -0.12), G(0.5, 0.7, -0.6).normalized(), 0.1, 0.55, "LArm_R_Upper",
           talon, hook=G(0, 0, 1), hook_k=0.4)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=900, lod_tris=300, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6,
                                  note="round 52: three pairs of socketed brow eyes over the maw")),
          ("Teeth", teeth_parts, dict(tris=2500, lod_tris=700, voxel=0.004, families=("chitin", "crystal"),
                                      emission_strength=2.0, specular=0.4,
                                      note="round 52: petal-edge rows (Plate_R/L, Jaw) and two throat rings (Head)")),
          ("Claws", claw_parts, dict(tris=1600, lod_tris=500, voxel=0.006, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="fingertip talons + elbow spurs"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def attach(mboss):
    """build_sealedone_mech.py hook: bake the parts and hand them to its export. Round 52: islands under 4 vertices
    (single triangles the decimation leaves off a tooth tip; the strict check found one 18 cm off the jaw) are
    deleted from every part and its LOD1 before the export."""
    import build_bulwark as BW
    boss = D.attach(mboss, PARTS, MAKERS)
    for o in boss.new + boss.new_lod:
        BW.drop_specks(o, min_verts=4)
    return boss


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
