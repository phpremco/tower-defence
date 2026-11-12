"""Round 45: The Shattered's face and claws (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Before, the head core had two white dots for eyes and its crystal teeth sat hidden in the jaw. Now:
- `Shattered_Eyes` (`Shattered_Eyes_Mat`): three pairs of compound eyes that burn from within (pink facets, a
  violet core, a small dark false pupil), flush in dark crystal sockets with a chiselled crystal brow over the big
  pair (round 45 polish: no rings; the build's white eye dots are cut from the body); bone Head.
- `Shattered_Teeth` (`Shattered_Teeth_Mat`): jagged Crysium fangs, a row hanging from the head core's rim (Head) and
  a shorter row rising from the cracked jaw (Jaw): violet roots, pink bodies, white tips, lit through.
- `Shattered_Claws` (`Shattered_Claws_Mat`): three hooked talons curling down from the front of each spade fist
  (Arm_*1_Lower) and three splayed round each hind foot (Leg_*1_Lower), dark keratin with pink-hot tips.
No bone, clip or event changes; the actions are exported again as they are (Split/Rejoin scale the limbs and the
claws with them).

Run: blender -b --factory-startup --python Tools/Blender/shattered_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_shattered_mech.py (attach(boss) before boss.export()).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "Shattered"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -1.65, 1.3)
DIST = 3.6

PINK, WHITE_HOT = (1.0, 0.42, 0.78), (1.0, 0.92, 0.98)       # overcharged Crysium (the build's palette)
FACET = (0.8, 0.3, 0.62)
SOCKET_DARK, SOCKET_SKIN, SOCKET_WET = (0.1, 0.03, 0.12), (0.35, 0.12, 0.38), (0.25, 0.04, 0.2)
BROW_CRY = (0.75, 0.35, 0.68)
VIOLET_CORE = (0.78, 0.4, 1.0)
# The build's two white eye dots a side (build_shattered.body_parts): cut out under the new sockets.
OLD_EYES = [((sx * x, -f, u), 0.065) for sx in (1, -1) for x, u, f in ((0.2, 1.42, 1.78), (0.3, 1.32, 1.7))]
CRY_ROOT, CRY_BODY, CRY_TIP = (0.25, 0.04, 0.25), (0.85, 0.42, 0.75), (1.0, 0.95, 1.0)
KER_ROOT, KER_BODY, KER_TIP = (0.05, 0.045, 0.06), (0.16, 0.15, 0.19), (0.92, 0.75, 0.9)

CLIPS = ["Idle", "Walk", "Hit", "PhaseBreak1", "PhaseBreak2", "Death", "Split", "Rejoin", "Roar", "Climb_Up",
         "Dive_Into_Liquid", "Surface_Burst"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def surface(boss, probe, look, push=0.3, r=0.1):
    p, n = boss.ray(probe + look, -look)
    if p is None:
        p, n = boss.nearest(probe)
    return p - n * r * push, n


def eye_parts(boss):
    b = C.PartBuilder()
    big = D.facet_style(FACET, PINK, cells=260, glow_k=1.2, seed=3, inner=VIOLET_CORE)
    small = D.facet_style(FACET, PINK, cells=150, glow_k=1.2, seed=4, inner=VIOLET_CORE)
    # Round 45 polish: the compound eyes burn from within and sit flush in crystal sockets (a dark violet crystal
    # mound lit at the wet rim, a chiselled crystal brow over the big pair) instead of grey bezel rings.
    mound = D.fold_style(SOCKET_SKIN, SOCKET_DARK, wet=SOCKET_WET, facing=0.0, glow=PINK, glow_k=0.6, seed=6,
                         family=(0.0, 0.0, 0.7, 0.0))
    brow = D.fold_style(BROW_CRY, SOCKET_DARK, wet=SOCKET_WET, facing=-1.0, glow=PINK, glow_k=0.45, seed=7, ridges=6,
                        family=(0.0, 0.0, 0.9, 0.0))
    for probe, look, r, st, has_brow in ((G(0.2, 1.42, 1.78), G(0.55, 0.2, 1.0), 0.13, big, True),
                                         (G(0.33, 1.3, 1.66), G(1.0, 0.0, 0.5), 0.085, small, False),
                                         (G(0.12, 1.55, 1.72), G(0.3, 0.8, 0.8), 0.05, small, False)):
        look = look.normalized()
        if has_brow:
            # Straight over the build's old dot (cut from the body, so the surface there is a hole now).
            c, n = probe - look * 0.03, look
        else:
            c, n = surface(boss, probe, look, 0.2, r)
        socket = dict(mound=mound, size=(1.35, 1.4, 0.8), sink=0.35)
        if has_brow:
            socket.update(brow=brow, brow_lift=0.85, brow_tilt=0.7, brow_size=(1.5, 0.6, 1.1),
                          brow_deform=C.chiselled(8, 51, 0.16, squash=(0.8, 1.0)))
        D.eye(b, "Eye", c, n.lerp(look, 0.5), r, "Head", st, squash=(1.0, 0.82, 0.75), socket=socket)
    return b


def teeth_parts(boss):
    b = C.PartBuilder()
    fang = D.keratin_style(CRY_ROOT, CRY_BODY, CRY_TIP, glow=PINK, glow_at="body", glow_k=0.55, seed=11, wet=0.1,
                           translucent=0.1, bands=3, family=(0.0, 0.0, 0.8, 0.0))

    def front(x, z):
        p, n = boss.ray(Vector((x, -4.0, z)), Vector((0, 1, 0)))
        return (p - n * 0.03) if p is not None else None
    # Upper row: jagged crystal fangs hanging from the rim of the head core (Head), over the jaw.
    for x, ln, r in ((0.05, 0.22, 0.036), (0.14, 0.27, 0.042), (0.23, 0.2, 0.034), (0.31, 0.15, 0.028)):
        p = front(x, 1.16)
        if p is not None:
            D.fang(b, "UpperFang", p, G(0.06, -1.0, 0.35).normalized(), r, ln, "Head", fang, hook=G(0, 0, -1),
                   hook_k=0.15, sides=6, flute=0.3, twist=0.6)
    # Lower row: rising from the front lip of the cracked jaw (Jaw).
    for x, ln, r in ((0.09, 0.18, 0.034), (0.2, 0.22, 0.038)):
        p = front(x, 1.1)
        if p is not None:
            D.fang(b, "LowerFang", p, G(-0.05, 1.0, 0.4).normalized(), r, ln, "Jaw", fang, hook=G(0, 0, -1),
                   hook_k=0.15, sides=6, flute=0.3, twist=0.6)
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=PINK, glow_at="tip", glow_k=0.8, seed=21, wet=0.3,
                            bands=8)
    # Spade fists: three hooked talons curling down from the front of each fist (Arm_*1_Lower).
    el, wr = boss.bone("Arm_R1_Lower")
    for k, dx in enumerate((-0.2, 0.0, 0.2)):
        probe = wr + Vector((dx, 0, 0.2))
        p, n = boss.ray(probe + Vector((0, -2, 0)), Vector((0, 1, 0)))
        if p is None:
            continue
        D.claw(b, "FistTalon", p + Vector((0, 0.03, 0)), Vector((dx * 0.5, -1.0, -0.35)).normalized(), 0.075,
               0.4 if k == 1 else 0.34, "Arm_R1_Lower", talon, hook=Vector((0, 0.3, -1)), hook_k=0.7)
    # Hind feet: three talons splayed round each foot (Leg_*1_Lower), tips to the ground.
    kn, ft = boss.bone("Leg_R1_Lower")
    for k, deg in enumerate((-60.0, 0.0, 55.0)):
        phi = math.radians(deg)
        out = Vector((math.sin(phi) * 0.8, -math.cos(phi), 0.0)).normalized()
        base = ft + out * 0.14 + Vector((0, 0, 0.12))
        D.claw(b, "FootTalon", base, (out + Vector((0, 0, 0.1))).normalized(), 0.06, 0.3, "Leg_R1_Lower", talon,
               hook=Vector((0, 0, -1)), hook_k=0.6)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1200, lod_tris=340, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="compound overcharge eyes (three pairs)")),
          ("Teeth", teeth_parts, dict(tris=1100, lod_tris=340, voxel=0.004, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.6, note="crystal fangs on the head rim + jaw")),
          ("Claws", claw_parts, dict(tris=1800, lod_tris=560, voxel=0.006, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="fist talons + hind-foot talons"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def prep(boss):
    D.cut_spheres(boss, OLD_EYES)


def attach(mboss):
    """build_shattered_mech.py hook: bake the parts and hand them to its export."""
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
