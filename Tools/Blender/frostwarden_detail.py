"""Round 45: Frost Warden's face and claws (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Before, the face was three amber marbles on a slate wedge and a cyan slit. Now:
- `FrostWarden_Eyes` (`FrostWarden_Eyes_Mat`): the triangle of three eyes becomes three narrow wet amber almond
  eyes (over the old balls) with a four-armed cross pupil, sunk under slate lid folds and rimed ice brow plates
  (round 45 polish: no rings), plus a pair of small side eyes further back; bone Head.
- `FrostWarden_Teeth` (`FrostWarden_Teeth_Mat`): ice fangs along the maw: a row hanging from the upper lip (Head) and
  a row rising from the jaw (Jaw), dark slate roots, a smooth ramp from dark gum to clear pale-cyan ice to a bright white tip.
  Round 49C: about twice the size and paler (two sabre fangs past the chin, tusks rising outside the upper lip),
  and every clip holds the jaw 12 degrees further open (`open_jaw_at_rest`, flagged per action).
- `FrostWarden_Claws` (`FrostWarden_Claws_Mat`): three hooked black talons with frosted tips curling out of the front
  of each fist maul (Arm_*_Fist) and three over the hind toes (Leg_*_Foot), tips to the ground.
No bone, clip or event changes; the actions are exported again as they are.

Run: blender -b --factory-startup --python Tools/Blender/frostwarden_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_frostwarden_mech.py (attach(boss) before boss.export()).
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "FrostWarden"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -2.4, 2.5)
DIST = 4.6

IRIS, EYE_GLOW = (1.0, 0.55, 0.12), (1.0, 0.85, 0.45)       # amber: the enemy colour code in a blue boss
SCLERA = (0.05, 0.03, 0.02)
LID_DARK, LID_SKIN, LID_WET = (0.2, 0.22, 0.27), (0.5, 0.54, 0.6), (0.05, 0.035, 0.04)   # the slate hide
SINK = 0.05
# The build's three amber balls (build_frostwarden.body_parts): cut out of the body under the sunk eyes.
OLD_EYES = [((0.0, -2.42, 2.9), 0.105), ((0.3, -2.44, 2.76), 0.09), ((-0.3, -2.44, 2.76), 0.09)]
BROW_ICE = (0.55, 0.72, 0.84)
# Round 49C: paler teeth (bone-ivory with a cold tint, was cyan ice 0.5, 0.74, 0.9), so they stand out on the slate.
ICE_ROOT, ICE_BODY, ICE_TIP = (0.16, 0.07, 0.09), (0.86, 0.9, 0.94), (0.97, 0.99, 1.0)   # dark gum, ivory, tip
ICE_GLOW = (0.42, 0.82, 1.0)
KER_ROOT, KER_BODY, KER_TIP = (0.04, 0.045, 0.06), (0.12, 0.13, 0.16), (0.8, 0.9, 0.98)

CLIPS = ["Idle", "Walk", "WalkEnraged", "FreezePulse", "Enrage", "Hit", "Death", "Armour_Break", "Freeze_Gate",
         "Climb_Up", "Dive_Into_Liquid", "Gate_Mount"]


def G(x, up, fwd):
    return Vector((x, -fwd, up))


def eye_parts(boss):
    b = C.PartBuilder()
    eye = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="cross", glow_k=1.1, seed=3)
    small = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="slit", glow_k=1.0, seed=4)
    # Round 45 polish: narrow almond eyes sunk under icy brow plates (no rings): slate lid folds, wet dark rims,
    # a heavy rimed brow over each.
    mound = D.fold_style(LID_SKIN, LID_DARK, wet=LID_WET, facing=0.0, seed=6)
    brow = D.fold_style(BROW_ICE, LID_DARK, wet=LID_WET, facing=-1.0, glow=ICE_GLOW, glow_k=0.25, seed=8, ridges=8,
                        family=(0.0, 0.0, 0.6, 0.0))
    socket = dict(mound=mound, brow=brow, size=(1.35, 2.0, 1.0), sink=0.3, brow_lift=0.75, brow_tilt=0.75,
                  brow_size=(1.55, 0.7, 1.15), brow_deform=C.chiselled(9, 41, 0.14, squash=(0.8, 1.0)))
    look = G(0, 0.42, 1.0).normalized()
    # Over the build's three amber balls (same centres; the folds hide what the narrower lens does not cover).
    for x, u, f, r in ((0.0, 2.9, 2.42, 0.15), (0.3, 2.76, 2.44, 0.13)):
        D.eye(b, "Eye", G(x, u, f) - look * SINK, (look + Vector((0.35 * (x > 0), 0, 0))).normalized(), r, "Head",
              eye, squash=(1.0, 0.56, 0.7), socket=socket)
    probe = G(0.62, 2.68, 2.05)
    side = G(0.8, 0.25, 0.55).normalized()
    p, n = boss.ray(probe + side, -side)
    if p is None:
        p, n = boss.nearest(probe)
    D.eye(b, "EyeSide", p - n * 0.03, n.lerp(side, 0.5), 0.08, "Head", small, squash=(1.0, 0.55, 0.7),
          socket=dict(mound=mound, size=(1.3, 1.9, 0.95), sink=0.25))
    return b


def teeth_parts(boss):
    b = C.PartBuilder()
    # Round 49C (Remco: "I don't see his teeth"): pale bone-ivory teeth with clear ice tips, lit from the root up
    # (the maw's ice light), about twice the round-45 size, so they read against the slate head from the play camera.
    fang = D.keratin_style(ICE_ROOT, ICE_BODY, ICE_TIP, glow=ICE_GLOW, glow_at="rise", glow_k=0.45, seed=11, wet=0.2,
                           translucent=0.1, bands=4, family=(0.0, 0.0, 0.7, 0.0))
    tusk = D.keratin_style(ICE_ROOT, ICE_BODY, ICE_TIP, glow=ICE_GLOW, glow_at="rise", glow_k=0.4, seed=13, wet=0.22,
                           translucent=0.1, bands=6, family=(0.0, 0.0, 0.7, 0.0))
    # Maw slit: centre G(0, 2.34, 2.58), half width 0.28. The roots are found on the lips by probing from the front.
    def lip(x, z):
        p, n = boss.ray(Vector((x, -4.0, z)), Vector((0, 1, 0)))
        return p - n * 0.03 if p is not None else Vector((x, -2.6, z))
    # Two sabre fangs from the upper lip hang past the chin (seen even with the jaw shut), a smaller fang between and
    # beside them; they hook back a little towards the throat.
    for x, ln, r in ((0.2, 0.72, 0.092), (0.07, 0.36, 0.056), (0.33, 0.34, 0.056)):
        D.fang(b, "UpperFang", lip(x, 2.43), G(0.03, -1.0, 0.22).normalized(), r, ln, "Head", fang,
               hook=G(0, 0, -1), hook_k=0.22)
    # Lower teeth stand on the jaw lip; the outer pair are tusks that rise outside the upper lip beside the snout,
    # tips curling out and up, so the teeth also read from above.
    for x, ln, r in ((0.11, 0.3, 0.056), (0.21, 0.32, 0.058)):
        D.fang(b, "LowerFang", lip(x, 2.25), G(-0.03, 1.0, 0.3).normalized(), r, ln, "Jaw", fang,
               hook=G(0, 0, -1), hook_k=0.2)
    D.fang(b, "Tusk", lip(0.34, 2.24) + G(0.02, 0.0, -0.04), G(0.18, 1.0, 0.3).normalized(), 0.09, 0.62, "Jaw", tusk,
           hook=G(0.35, 0.2, -1.0), hook_k=0.35)
    return b


# Round 49C: the maw hangs a little open at rest (Remco: "I don't see his teeth"), so the teeth and the lit throat
# show between the lips in every clip. A constant turn of the Jaw bone about its own X axis on top of each clip's
# keys (the clips' own jaw motion stays as it was); each action is flagged so a rerun never adds it twice.
JAW_REST_OPEN = 12.0
JAW_FLAG = "r49c_jaw_rest_open_deg"


def open_jaw_at_rest(boss):
    import math
    from mathutils import Quaternion
    import creature_kit2 as K2
    path = 'pose.bones["Jaw"].rotation_quaternion'
    done = []
    for act in bpy.data.actions:
        # The flag holds the turn already applied, so a rerun (or a changed JAW_REST_OPEN) adds only the difference.
        delta = JAW_REST_OPEN - float(act.get(JAW_FLAG, 0.0))
        if abs(delta) < 1e-6:
            continue
        for bag in K2._channelbags(act):
            curves = [bag.fcurves.find(path, index=i) for i in range(4)]
            if any(c is None for c in curves):
                continue
            off = Quaternion((1, 0, 0), math.radians(delta * JAW_SIGN))
            frames = sorted({k.co[0] for c in curves for k in c.keyframe_points})
            values = [Quaternion([c.evaluate(f) for c in curves]) @ off for f in frames]
            for c, i in zip(curves, range(4)):
                # Keys stay where they are (same frames, same interpolation); only the values and handles move.
                by_frame = {k.co[0]: k for k in c.keyframe_points}
                for f, q in zip(frames, values):
                    k = by_frame.get(f)
                    if k is None:
                        k = c.keyframe_points.insert(f, q[i])
                        continue
                    dv = q[i] - k.co[1]
                    k.co[1] += dv
                    k.handle_left[1] += dv
                    k.handle_right[1] += dv
                c.update()
            act[JAW_FLAG] = JAW_REST_OPEN
            done.append(act.name)
    boss.facts["jaw_rest_open"] = {"degrees": JAW_REST_OPEN, "actions": done}
    C.log(boss.name, "jaw open at rest", JAW_REST_OPEN, len(done), "actions")


# +1 if a positive turn about the Jaw bone's local X drops the jaw (checked in prep from the rest bone).
JAW_SIGN = 1.0


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(KER_ROOT, KER_BODY, KER_TIP, glow=ICE_GLOW, glow_at="tip", glow_k=0.55, seed=21, wet=0.3,
                            bands=8)
    # Fists: probe the front of the maul (the knuckle contact is the bone tail, the fist faces forward).
    wrist, knuck = boss.bone("Arm_R_Fist")
    for k, dx in enumerate((-0.2, 0.0, 0.2)):
        probe = knuck + Vector((dx, 0, 0.32 - 0.06 * abs(dx) / 0.2))
        p, n = boss.ray(probe + Vector((0, -2, 0)), Vector((0, 1, 0)))
        if p is None:
            continue
        D.claw(b, "FistTalon", p + Vector((0, 0.04, 0)), Vector((dx * 0.6, -1.0, -0.25)).normalized(),
               0.085, 0.46 if k == 1 else 0.4, "Arm_R_Fist", talon, hook=Vector((0, 0.25, -1)), hook_k=0.7)
    # Hind feet: three over the toes.
    ankle, toe = boss.bone("Leg_R_Foot")
    for k, dx in enumerate((-0.17, 0.0, 0.17)):
        base = toe + Vector((dx, -0.02, 0.13))
        D.claw(b, "ToeTalon", base, Vector((dx * 0.8, -1.0, -0.12)).normalized(), 0.075, 0.36, "Leg_R_Foot", talon,
               hook=Vector((0, 0, -1)), hook_k=0.55)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1200, lod_tris=340, voxel=0.0045, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="three amber cross-pupil eyes + side pair")),
          ("Teeth", teeth_parts, dict(tris=1700, lod_tris=460, voxel=0.004, families=("crystal", "chitin"),
                                      emission_strength=2.5, specular=0.6,
                                      note="round 49C: big pale sabre fangs, lower teeth and tusks; jaw 12 deg open at rest")),
          ("Claws", claw_parts, dict(tris=1800, lod_tris=560, voxel=0.006, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="fist talons + toe talons"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def jaw_sign(boss):
    """+1 if a positive turn about the Jaw bone's local X axis moves its tip down (opens the maw)."""
    from mathutils import Matrix
    import math
    bone = boss.arm.data.bones["Jaw"]
    m = bone.matrix_local
    tail = m.inverted() @ bone.tail_local
    turned = m @ (Matrix.Rotation(math.radians(10), 4, "X") @ tail)
    return 1.0 if turned.z < bone.tail_local.z else -1.0


def prep(boss):
    global JAW_SIGN
    D.cut_spheres(boss, OLD_EYES)
    JAW_SIGN = jaw_sign(boss)
    open_jaw_at_rest(boss)


def attach(mboss):
    """build_frostwarden_mech.py hook: bake the parts and hand them to its export."""
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
