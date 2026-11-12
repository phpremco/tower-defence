"""Round 45: The Sealed One's face and claws (Remco: "restyle the detail features on all the bosses - think
claws, teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Round 49C (Remco: "I can't make out where the head is"): the head is a long skull now (build_sealedone.py) and gets
a face that reads from the play camera:
- `SealedOne_Eyes` (`SealedOne_Eyes_Mat`): two pairs of violet eyes in deep dark sockets under heavy bone brows on
  the face plates (bones Plate_R / Plate_L, so they part with the plates), and the tall seam eye (horizontal slit
  pupil) in the light column between the plates as a third eye on the brow (bone Head).
- `SealedOne_Teeth` (`SealedOne_Teeth_Mat`): round 49F: two interlocking rows of ivory fangs rooted in the gums of
  the maw (upper row on Head, lower row on the new Jaw bone); violet light at the roots.
- `SealedOne_Claws` (`SealedOne_Claws_Mat`): hooked talons over the three fingertips of each planted hand
  (LArm_*_Hand) and a hooked spur on each lower elbow (LArm_*_Upper), violet-hot tips.
No bone, clip or event changes; the actions are exported again as they are.

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

NAME = "SealedOne"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -0.95, 3.95)
DIST = 5.6

VIOLET, WHITE_HOT = (0.62, 0.26, 1.0), (0.95, 0.85, 1.0)      # the being's light (the build's VIOLET)
IRIS = (0.75, 0.4, 1.0)
SCLERA = (0.04, 0.02, 0.06)
LID_DARK, LID_RIM = (0.2, 0.17, 0.2), (0.86, 0.81, 0.72)        # bone-stone
SOCKET_DARK = (0.05, 0.03, 0.07)                                  # round 49C: the sockets read dark from above
BONE_ROOT, BONE_BODY, BONE_TIP = (0.16, 0.1, 0.2), (0.82, 0.77, 0.68), (0.97, 0.95, 0.9)
KER_ROOT, KER_BODY, KER_TIP = (0.06, 0.05, 0.08), (0.2, 0.18, 0.22), (0.85, 0.7, 1.0)

CLIPS = ["Idle", "Rise", "Summon", "Hit", "Death", "Adapt", "Roar", "Climb_Up", "Dive_Into_Liquid", "OnBase_Loop"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def eye_parts(boss):
    import build_sealedone as BSO
    b = C.PartBuilder()
    big = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="slit", glow_k=1.0, seed=3, rings=0.5)
    plate = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="slit", glow_k=1.4, seed=5, rings=0.3)
    small = D.eye_style(IRIS, WHITE_HOT, SCLERA, pupil="round", glow_k=1.3, seed=4)
    # Two pairs of eyes on the face plates, sunk in dark sockets under heavy bone brows (organic sockets, as the
    # round-45 Warden): the big pair on the front slope, a small pair behind and above them. The plates' gap and
    # thickness (0.04 + 0.09) lift them onto the plate surface.
    # Dark sunken sockets against the ivory plates, heavy bone brows over the big pair (a V over the face from the
    # play camera); the eyes sit on the plates' outer surface (gap 0.04 + thickness 0.09 + dome).
    mound = D.fold_style(SOCKET_DARK, LID_DARK, wet=(0.12, 0.05, 0.16), facing=0.0, seed=6)
    brow = D.fold_style(LID_RIM, LID_DARK, wet=(0.12, 0.05, 0.16), facing=-1.0, seed=8, ridges=10)
    socket = dict(mound=mound, brow=brow, size=(1.4, 1.9, 1.0), sink=0.3, brow_lift=0.9, brow_tilt=0.5,
                  brow_size=(2.0, 0.9, 1.5), brow_deform=C.chiselled(9, 41, 0.12, squash=(0.8, 1.0)))
    for unit, r, style, sock in (((0.38, -0.55, 0.76), 0.21, plate, socket),
                                 ((0.6, -0.1, 0.82), 0.12, small, dict(mound=mound, size=(1.35, 1.8, 0.95), sink=0.25))):
        p, n = BSO.head_surface(unit)
        look = (n + Vector((0, -0.7, -0.05))).normalized()
        D.eye(b, "PlateEye", p + n * 0.16, look, r, "Plate_R", style, squash=(1.0, 0.62, 0.7), socket=sock)
    # The seam eye: a tall eye with a horizontal slit in the light column on the brow (turned 90 degrees: local x
    # is world up, so squash[0] is its height).
    p, n = BSO.head_surface((0.0, -0.3, 0.95))
    look = (n + Vector((0, -0.6, 0))).normalized()
    D.eye(b, "Eye", p - n * 0.03, look, 0.24, "Head", big, squash=(1.0, 0.45, 0.5), both=False, up=Vector((1, 0, 0)))
    return b


def teeth_parts(boss):
    """Round 49F (Remco: "I don't understand the mouth"): two rows of ivory fangs rooted in the gums of the new maw
    (build_sealedone.gum_path), the upper row on Head pointing down, the lower row on Jaw pointing up. The rows
    interlock (a lower fang stands between two upper ones), and every fang stops TOOTH_CLEARANCE short of the other
    gum, so with the jaw at rest (it never closes further) no tooth cuts a lip or a gum. Longest at the front, two
    heavier fangs a little off the middle, short ones in the corners."""
    import build_sealedone as BSO
    b = C.PartBuilder()
    # Ivory first: only a faint violet at the roots (bright roots read as glowing blobs along the gums).
    fang = D.keratin_style(BONE_ROOT, BONE_BODY, BONE_TIP, glow=VIOLET, glow_at="root", glow_k=0.3, seed=11,
                           wet=0.22, bands=5, family=(0.5, 0.0, 0.0, 0.0))
    for upper, count, bone in ((True, 9, "Head"), (False, 8, "Jaw")):
        curve = C.catmull_rom(BSO.gum_path(upper), 12)
        length = [0.0]
        for a, c in zip(curve, curve[1:]):
            length.append(length[-1] + (c - a).length)
        for i in range(count):
            # Upper fangs at the middles of `count` equal spans, lower ones on the span borders: they interlock.
            s_ = (i + 0.5) / count if upper else (i + 1.0) / (count + 1)
            p, tangent = _along(curve, length, s_)
            fwd = -p.y
            gap = BSO.mouth_gap(fwd)
            out = Vector((tangent.y, -tangent.x, 0.0)).normalized()  # away from the mouth's middle
            if out.dot(Vector((p.x, p.y + 0.6, 0.0))) < 0:
                out = -out
            down = Vector((0, 0, -1 if upper else 1))
            canine = abs(abs(s_ - 0.5) - 0.17) < 0.05
            d = (down + out * 0.12).normalized()
            root = p + down * (BSO.GUM_R * 0.5)
            reach = (gap - BSO.GUM_R * 1.5 - TOOTH_CLEARANCE) / d.dot(down)
            ln = reach * (1.0 if canine else 0.82 + 0.1 * math.cos(9.0 * s_))
            r = (0.03 + 0.03 * reach / 0.22) * (1.25 if canine else 1.0)
            D.fang(b, "Fang", root, d, r, ln, bone, fang, hook=Vector((0, 1, 0)), hook_k=0.12, both=False)
    return b


TOOTH_CLEARANCE = 0.035


def _along(curve, length, s_):
    """The point and unit tangent at arc fraction s_ of a polyline."""
    target = s_ * length[-1]
    for i in range(len(curve) - 1):
        if length[i + 1] >= target:
            u = (target - length[i]) / max(length[i + 1] - length[i], 1e-9)
            return curve[i].lerp(curve[i + 1], u), (curve[i + 1] - curve[i]).normalized()
    return curve[-1], (curve[-1] - curve[-2]).normalized()


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


MAKERS = [("Eyes", eye_parts, dict(tris=1700, lod_tris=460, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6,
                                  note="round 49C: two pairs of socketed plate eyes under brows + the seam eye")),
          ("Teeth", teeth_parts, dict(tris=1500, lod_tris=420, voxel=0.004, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.6,
                                      note="round 49F: two interlocking fang rows rooted in the maw's gums")),
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
