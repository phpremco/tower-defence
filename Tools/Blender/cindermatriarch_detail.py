"""Round 45: Cinder Matriarch's face and claws (Remco: "restyle the detail features on all the bosses - think claws,
teeth, eyes etc."). Technique: gravemaw_maw.py; helpers: detail_kit_b.py.

Before, her face read as a slag wedge with four mandibles and pin-head eyes (0.045 m). Now:
- `CinderMatriarch_Eyes` (`CinderMatriarch_Eyes_Mat`): a cluster of three wet ember eyes per side in dark lid rings
  (one big main eye under the brow, two smaller behind it), gold-orange irises with a hot inner ring and a black
  slit pupil, glossy cornea; on bone Head.
- `CinderMatriarch_Teeth` (`CinderMatriarch_Teeth_Mat`): an ember-lit lamprey ring of hooked obsidian fangs round the
  glowing mouth (Head) and a serrated row of fangs on the inner edge of each of the four mandibles (Mand_*), dark
  glassy roots, hot cracked tips.
- `CinderMatriarch_Claws` (`CinderMatriarch_Claws_Mat`): three hooked obsidian talons splayed round each stilt foot
  (Leg_*_Lower), tips at ground level, ember glow in the cracks and tips; a hooked killing claw and two barbs on each
  scythe blade (Scythe_*_Blade).
No bone, clip or event changes; the actions are exported again as they are.

Round 49B (Remco: "the head may be scarier"): the skull was rebuilt (build_cindermatriarch.py: V brow ridges over
deep glowing sockets, hood, underjaw, cheek spikes, longer hooked mandibles). The eyes are probed into the sockets
(BC.SOCKETS), the mandible serrations follow the new curve (BC.mandible_points), and two sabre fangs hang from the
upper lip either side of the lamprey ring.

Run: blender -b --factory-startup --python Tools/Blender/cindermatriarch_detail.py -- [--before | --blockout | --qa]
     [--norender]
Hooked from build_cindermatriarch_mech.py (attach(boss) before boss.export()).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import build_cindermatriarch as BC  # noqa: E402
import common as C  # noqa: E402
import detail_kit_b as D  # noqa: E402

NAME = "CinderMatriarch"
PARTS = ("Eyes", "Teeth", "Claws")
FACE_BONE, FACE = "Head", (0.0, -1.9, 1.42)
DIST = 3.8

# Palette (sRGB, like the styles in the builds): ember eyes, obsidian teeth and talons with hot tips.
IRIS, EYE_GLOW = (1.0, 0.55, 0.08), (1.0, 0.82, 0.4)
SCLERA = (0.06, 0.02, 0.01)
LID_DARK, LID_RIM = (0.06, 0.045, 0.04), (0.3, 0.2, 0.14)
OBS_ROOT, OBS_BODY, OBS_TIP = (0.05, 0.03, 0.025), (0.12, 0.09, 0.08), (0.95, 0.62, 0.3)
EMBER = (1.0, 0.5, 0.12)

CLIPS = ["Idle", "Walk", "Hunt_Stride", "Smash", "Hit", "Death", "Roar", "Spawn", "Lay_Eggs", "Climb_Up",
         "Gate_Mount", "Surface_Burst"]


def eye_parts(boss):
    b = C.PartBuilder()
    eye = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="slit", glow_k=1.0, seed=3)
    small = D.eye_style(IRIS, EYE_GLOW, SCLERA, pupil="round", glow_k=0.9, seed=4)
    mound = D.fold_style(LID_RIM, LID_DARK, wet=(0.35, 0.1, 0.03), facing=-1.0, glow=EMBER, glow_k=0.4, seed=5)
    brow = D.fold_style(LID_RIM, LID_DARK, wet=(0.2, 0.06, 0.02), facing=-1.0, seed=6)
    # (socket centre, look direction, radius, style): each lens sits in its glowing socket under the brow ridges.
    for ((x, u, f), r_socket), look, r, st in zip(BC.SOCKETS, ((0.75, -0.75, 0.15), (0.85, -0.35, 0.4), (0.95, -0.3, -0.1)),
                                                 (0.1, 0.075, 0.065), (eye, small, small)):
        probe = Vector((x, -f, u))
        hit = boss.ray(probe + Vector(look) * 1.0, -Vector(look))
        p, n = hit if hit[0] is not None else boss.nearest(probe)
        # Round 49B: the round-45 organic socket (a slag mound the lens breaks through as a narrow almond) and a
        # heavy brow fold tilted down over it, instead of the lid ring that read as goggles.
        centre = p - n * r * 0.4
        D.eye(b, "Eye", centre, n.lerp(Vector(look).normalized(), 0.5), r, "Head", st, squash=(1.15, 0.75, 0.7),
              socket=dict(mound=mound, brow=brow, size=(1.4, 1.5, 0.85), sink=0.35,
                                                      brow_tilt=0.75, brow_size=(1.6, 0.8, 1.1)))
    return b


def curve_at(pts, t):
    """Point and tangent at t (0..1) on the Catmull-Rom curve the build put the mandible on."""
    c = C.catmull_rom([Vector(p) for p in pts], 8)
    i = min(int(t * (len(c) - 1)), len(c) - 2)
    return c[i], (c[i + 1] - c[i]).normalized()


def mandible_points(ang):
    """Round 49B: the build's own (heavier, hooked) mandible curve."""
    return BC.mandible_points(ang)


def teeth_parts(boss):
    b = C.PartBuilder()
    a, up, right = BC.mouth_frame()
    tooth = D.keratin_style(OBS_ROOT, OBS_BODY, OBS_TIP, glow=EMBER, glow_at="tip", glow_k=1.0, seed=11, wet=0.22)
    root_lit = D.keratin_style(OBS_ROOT, OBS_BODY, OBS_TIP, glow=EMBER, glow_at="root", glow_k=0.9, seed=12, wet=0.2)
    # Serrated inner edge of each mandible (right-hand ones; mirrored): hooked back towards the mouth.
    for name, ang in BC.MANDS:
        if not name.endswith("R"):
            continue
        pts, d = mandible_points(ang)
        for j, t in enumerate((0.2, 0.36, 0.52, 0.68)):
            p, tan = curve_at(pts, t)
            rad = 0.085 - 0.06 * t
            inward = (-d - tan * tan.dot(-d)).normalized()
            base = p + inward * rad * 0.45
            ln = 0.12 - 0.05 * t
            D.fang(b, "MandTooth", base, (inward * 0.85 - tan * 0.35).normalized(), 0.036 - 0.012 * t, ln * 1.25, name,
                   tooth, hook=-tan, hook_k=0.35)
    # Round 49B: two sabre fangs hanging from the upper lip, outside the ring, hooked back.
    base = BC.MOUTH - a * 0.06 + up * 0.2 + right * 0.16
    if base.x < 0:
        base = base - right * 0.32
    D.fang(b, "Sabre", base, (-up * 0.85 + a * 0.35).normalized(), 0.05, 0.42, "Head", tooth, hook=-a, hook_k=0.3)
    # Lamprey ring round the mouth: fangs pointing in and down the throat, ember light at the roots.
    centre = BC.MOUTH - a * 0.03
    for deg in (0.0, 20.0, 72.0, 92.0, 112.0, 162.0, 180.0):
        phi = math.radians(deg)
        radial = (up * math.cos(phi) + right * math.sin(phi)).normalized()
        base = centre + radial * 0.125
        direction = (-radial * 0.75 - a * 0.65).normalized()
        D.fang(b, "RingTooth", base, direction, 0.024, 0.1, "Head", root_lit, hook=-a, hook_k=0.3,
               both=deg not in (0.0, 180.0))
    return b


def claw_parts(boss):
    b = C.PartBuilder()
    talon = D.keratin_style(OBS_ROOT, OBS_BODY, OBS_TIP, glow=EMBER, glow_at="tip", glow_k=0.95, seed=21, wet=0.3,
                            bands=9)
    for leg in ("Leg_R1_Lower", "Leg_R2_Lower"):
        head, foot = boss.bone(leg)
        front = 1.0 if foot.y < 0 else -1.0
        for k, deg in enumerate((-55.0, 15.0, 80.0)):
            # Splay round the foot cone: deg 0 is straight out (+x), negative towards the front of the body.
            phi = math.radians(deg) * front
            out = Vector((math.cos(phi), math.sin(phi), 0.0))
            base = foot + out * 0.13 + Vector((0, 0, 0.2))
            D.claw(b, "Talon", base, (out * 0.85 + Vector((0, 0, 0.15))).normalized(), 0.065 - 0.008 * (k == 1),
                   0.36 if k != 1 else 0.42, leg, talon, hook=Vector((0, 0, -1)), hook_k=0.6)
    for blade in ("Scythe_R_Blade",):
        el, tip = boss.bone(blade)
        d = (tip - el).normalized()
        down = Vector((0, 0, -1))
        D.claw(b, "ScytheHook", tip - d * 0.06, (d * 0.7 + Vector((0, -0.3, 0))).normalized(), 0.05, 0.32, blade,
               talon, hook=(down + Vector((0, -0.4, 0))).normalized(), hook_k=0.75)
        inner = Vector((-1, 0, 0))
        for t in (0.45, 0.68):
            p = el.lerp(tip, t)
            D.fang(b, "ScytheBarb", p + inner * 0.05, (inner * 0.7 - d * 0.55).normalized(), 0.035, 0.16, blade,
                   talon, hook=-d, hook_k=0.35)
    return b


MAKERS = [("Eyes", eye_parts, dict(tris=1300, lod_tris=360, voxel=0.0035, families=("chitin", "membrane"),
                                  emission_strength=2.5, specular=0.6, note="three wet ember eyes per side")),
          ("Teeth", teeth_parts, dict(tris=1500, lod_tris=460, voxel=0.003, families=("chitin", "crystal"),
                                      emission_strength=2.5, specular=0.55, note="mandible serrations + mouth ring")),
          ("Claws", claw_parts, dict(tris=1600, lod_tris=500, voxel=0.0045, families=("chitin", "crystal"),
                                     emission_strength=2.5, specular=0.5, note="foot talons + scythe hooks"))]


def after_qa(boss):
    D.qa(boss, "after", FACE_BONE, FACE, DIST)
    D.clip_check(boss, FACE_BONE, FACE, DIST * 1.5, CLIPS)
    D.qa_lod(boss, FACE_BONE, FACE, DIST)


def attach(mboss):
    """build_cindermatriarch_mech.py hook: bake the parts and hand them to its export."""
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
        D.blockout(boss, claw_parts(boss), "Claws", (0.2, 0.4, 1.0), (0.2, 0.4, 1))
        D.qa(boss, "blockout", FACE_BONE, FACE, DIST)
        return
    if args.get("qa"):
        after_qa(boss)
        return
    D.run(boss, MAKERS, args, after_qa)


if __name__ == "__main__":
    main()
