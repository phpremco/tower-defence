"""BroodMother: the Skitter boss, boss of level 1 (round 6, see ArtSource/DESIGN_NOTES.md).

The Skitter swollen into a breeding queen: four thick armoured walking legs with knee spurs, two raised fore-limbs
ending in long barbed sickles, the X of four hooked jaws under a broad crowned hood (five twisted Crysium shards and a
pale forward chevron), rows of hot sensor slits. Behind the legs a huge three-lobed brood sac is dragged low: amber
eggs glowing through the membrane, shingled plates with hooks on top, two lateral egg sacs on their own bones and a
ringed birth vent at the rear where the broodlings come out.

Clips: Walk (loop), Idle (loop), Hit, Death, Spawn (the power: sacs squeeze, the vent lowers and opens; broodlings at
SPAWN_RELEASE_FRAME). The sacs and the vent scale (bone scale keys). Also exports BroodMother_LOD1.

Run: blender --background --python Tools/Blender/build_broodmother.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
from common import G  # noqa: E402

NAME = "BroodMother"
LOD_NAME = "BroodMother_LOD1"
# Built in Skitter units times M: at Unity scale 1.3 x 0.6 she stands about 3.2 times a Skitter (as the factory boss).
M = 1.75
K = M
DETAIL_K = 1.5
GAME_TRIS = 14500
LOD_TRIS = 6000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 1.3 * 0.6
SKITTER_SCALE = 0.45

# The Skitter's colours (rust plates, near-black body, lime venom), older and darker; amber-yellow eggs for the
# brood (the Brood ability's colour) so the sac reads as "full of young", not as venom.
PAL = H.Palette(top=(0.72, 0.27, 0.06), side=(0.26, 0.075, 0.025), belly=(0.045, 0.025, 0.018), seam=(0.03, 0.015, 0.01),
                dark=(0.065, 0.03, 0.02), glow=(0.68, 1.0, 0.18), eye=(1.0, 0.95, 0.45), membrane=(0.36, 0.3, 0.05),
                bone=(0.95, 0.82, 0.58))
SAC = H.Palette(top=(0.6, 0.22, 0.06), side=(0.24, 0.07, 0.025), belly=(0.05, 0.025, 0.015), seam=(0.03, 0.015, 0.01),
                dark=(0.07, 0.03, 0.02), glow=(1.0, 0.72, 0.16), eye=PAL.eye, membrane=(0.42, 0.24, 0.04),
                bone=PAL.bone)
LEGP = H.Palette(top=(0.55, 0.2, 0.05), side=(0.22, 0.065, 0.022), belly=(0.04, 0.02, 0.015), seam=(0.03, 0.014, 0.01),
                 dark=(0.06, 0.028, 0.018), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
JAWS = ("UL", "UR", "LL", "LR")
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def P(x, up, fwd):
    """A point in Skitter units, scaled to the boss."""
    return G(x * M, up * M, fwd * M)


def R(*r):
    return tuple(v * M for v in r) if len(r) > 1 else r[0] * M


LEG_SPECS = {
    1: ((0.085, 0.21, 0.09), (0.43, 0.0, 0.3)),
    2: ((0.09, 0.21, -0.06), (0.46, 0.0, -0.24)),
}
COXA, FEMUR, TIBIA = 0.075, 0.27, 0.34
# A low knee: thick columns spread wide, not a spider's stilts.
KNEE_LIFT = 0.72
COXA_LIFT = math.radians(35)
# A heavy four-legged walk: one leg lifts at a time.
STEP_PHASE = {("L", 1): 0.0, ("R", 2): 0.25, ("R", 1): 0.5, ("L", 2): 0.75}
SAC_CENTRE, SAC_RADII = (0.0, 0.27, -0.45), (0.25, 0.37, 0.18)
LOBE_CENTRE = (0.0, 0.2, -0.82)
VENT = (0.0, 0.17, -0.94)
SIDE_SAC = (0.235, 0.2, -0.37)


def legs():
    out = {}
    for side, sx in SIDES:
        for i, (root, foot) in LEG_SPECS.items():
            names = [f"Leg_{side}{i}_{s}" for s in ("Coxa", "Femur", "Tibia")]
            out[(side, i)] = C.Leg(names, P(root[0] * sx, root[1], root[2]), P(foot[0] * sx, foot[1], foot[2]),
                                   R(COXA), R(FEMUR), R(TIBIA), COXA_LIFT, knee_lift=KNEE_LIFT)
    return out


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def jaw_points(name):
    sx = -1 if name[1] == "L" else 1
    if name[0] == "U":
        return [P(0.04 * sx, 0.165, 0.37), P(0.1 * sx, 0.195, 0.46), P(0.085 * sx, 0.185, 0.55), P(0.022 * sx, 0.16, 0.6)]
    return [P(0.04 * sx, 0.11, 0.37), P(0.095 * sx, 0.08, 0.45), P(0.076 * sx, 0.085, 0.53), P(0.02 * sx, 0.115, 0.585)]


def blade_points(sx):
    shoulder = P(0.09 * sx, 0.25, 0.15)
    elbow = P(0.215 * sx, 0.42, 0.22)
    return shoulder, elbow, [elbow, P(0.215 * sx, 0.35, 0.44), P(0.155 * sx, 0.215, 0.66), P(0.085 * sx, 0.135, 0.8)]


def sac_top(x, fwd):
    cx, cy, cz = SAC_CENTRE
    rx, ry, rz = SAC_RADII
    q = 1 - ((fwd - cz) / ry) ** 2 - (x / rx) ** 2
    return cy + rz * math.sqrt(max(0.0, q))


# --------------------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------------------

def crest_deform(v):
    """A broad arrowhead hood: pointed in front, its side rims cut into backward teeth."""
    front = max(0.0, -v.y)
    x = v.x * (1 - 0.55 * front ** 1.5)
    saw = ((v.y + 1) * 3.5) % 1.0
    x *= 1 + 0.32 * saw * (1 - abs(v.z)) * (1 - 0.5 * front)
    return Vector((x, v.y * (1 + 0.22 * front), v.z))


def head_deform(v):
    front = max(0.0, -v.y)
    return Vector((v.x * (1 - 0.35 * front), v.y, v.z * (1 - 0.3 * front)))


def sac_deform(v):
    # Three egg lobes along the sac and knobbly clutches on the flanks: not a smooth spider abdomen.
    rear = max(0.0, v.y)
    lobes = 1 + 0.07 * math.cos(3 * math.pi * (v.y + 1) * 0.5 * 2) * (1 - v.y * v.y)
    knobs = 1 + 0.045 * math.sin(6 * math.atan2(v.z, v.x)) * math.sin(5 * v.y + 0.4) * (1 - abs(v.y))
    return Vector((v.x * (1 - 0.3 * rear) * lobes * knobs, v.y, (v.z * (1 - 0.2 * rear) - 0.06 * rear * rear) * lobes * knobs))


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Thorax", P(0, 0.235, 0.02), R(0.145, 0.2, 0.11), pitch(8), bone="Thorax",
                style=H.band_plates(PAL, k, 3, height=0.004, glow=0.25), segments=(48, 24))
    b.ellipsoid("Head", P(0, 0.17, 0.3), R(0.105, 0.135, 0.07), pitch(16), bone="Head",
                style=H.wedge_head(PAL, k, slits=4, slit_width=0.055), segments=(48, 24), deform=head_deform)
    b.ellipsoid("Crest", P(0, 0.265, 0.21), R(0.15, 0.2, 0.032), pitch(18), bone="Head", style=H.crest(PAL, k, ribs=9),
                segments=(56, 24), deform=crest_deform)
    # A pale keel along the crest's ridge, glowing towards its point: the forward chevron seen from above.
    b.tube("Keel", [P(0, 0.3, 0.06), P(0, 0.292, 0.22), P(0, 0.255, 0.38)], [R(0.022), R(0.018), R(0.004)], sides=10,
           chain=["Head"], style=H.blade(PAL, k, glow=1.0), flatten=0.5)
    for name in JAWS:
        b.tube("Jaw", jaw_points(name), [R(0.026), R(0.024), R(0.016), R(0.003)], sides=12, chain=[f"Jaw_{name}"],
               style=H.tube(PAL, k, PAL.dark, PAL.bone * 0.8, tip_glow=1.0, glow_from=0.7), flatten=0.6)
    b.ellipsoid("Throat", P(0, 0.135, 0.4), R(0.032, 0.03, 0.03), bone="Head", style=H.inner_glow(PAL, 1.0))

    # Crown: five twisted Crysium shards from the back edge of the hood, pointing up and back.
    for i in range(5):
        a = (i - 2) / 2.0
        base = P(0.075 * a, 0.3 - 0.01 * abs(a), 0.11 - 0.025 * abs(a))
        b.shard("Crown", base, G(0.55 * a, 1.0, -0.6), R(0.026 - 0.004 * abs(a)), R(0.24 - 0.07 * abs(a)), sides=8,
                twist=2.4 * (1 if a >= 0 else -1), flute=0.45, bone="Head", style=Y.shard_style("pink", 1.1, seed=40 + i))

    for side, sx in SIDES:
        shoulder, elbow, blade = blade_points(sx)
        b.tube("BladeArm", [shoulder, shoulder.lerp(elbow, 0.5) + P(0, 0.02, -0.015), elbow], [R(0.034), R(0.03), R(0.025)],
               sides=12, chain=[f"Blade_{side}_1"], style=H.leg(LEGP, k, "Femur", spines=3))
        b.tube("Blade", blade, [R(0.025), R(0.05), R(0.04), R(0.002)], sides=14, chain=[f"Blade_{side}_2"],
               style=H.blade(PAL, k, glow=1.0, teeth=7), flatten=0.3)
        curve = C.catmull_rom([Vector(p) for p in blade], 6)
        for t in (0.3, 0.45, 0.6, 0.75):
            root = curve[int(t * (len(curve) - 1))]
            b.tube("Barb", [root, root + P(-0.034 * sx, 0.005, -0.03)], [R(0.01), R(0.0012)], sides=8,
                   chain=[f"Blade_{side}_2"], style=H.hook(PAL, k, glow=0.6))
        # Crysium grown into the thorax, as on every Skitter, but larger.
        b.shard("Crysium", P(0.07 * sx, 0.3, 0.03), G(0.6 * sx, 1.0, -0.4), R(0.024), R(0.15), sides=8, twist=2.0,
                flute=0.4, bone="Thorax", style=Y.shard_style("pink", 1.0, seed=3))

    # The brood sac: a translucent membrane over amber eggs, shingled plates with glowing gaps on top.
    b.ellipsoid("Abdomen", P(*SAC_CENTRE), R(*SAC_RADII), pitch(5), bone="Abdomen_1",
                style=H.membrane_sac(SAC, k, bands=5, plate_line=0.55, cells=80, cell_size=0.13, glow=1.0, plate_glow=0.6, seed=37),
                segments=(72, 36), deform=sac_deform).chain = ["Abdomen_1", "Abdomen_2"]
    b.ellipsoid("Lobe", P(*LOBE_CENTRE), R(0.135, 0.16, 0.11), pitch(-8), bone="Abdomen_3",
                style=H.membrane_sac(SAC, k, bands=2, plate_line=0.3, cells=26, cell_size=0.14, glow=1.0, plate_glow=0.3, seed=38),
                segments=(40, 20)).chain = ["Abdomen_3", "Abdomen_2"]
    for side, sx in SIDES:
        b.ellipsoid("SideSac", P(SIDE_SAC[0] * sx, SIDE_SAC[1], SIDE_SAC[2]), R(0.12, 0.17, 0.11),
                    Matrix.Rotation(math.radians(-12 * sx), 3, "Z") @ pitch(6), bone=f"Sac_{side}",
                    style=H.membrane_sac(SAC, k, bands=3, plate_line=0.55, cells=30, cell_size=0.15, glow=1.0, plate_glow=0.2,
                                         seed=39), segments=(40, 20))
    # The birth vent: a ring of hooked lips around a glowing opening, on its own bone (it opens by scale).
    vent = P(*VENT)
    b.ellipsoid("VentGlow", vent + P(0, 0, 0.012), R(0.045, 0.03, 0.04), bone="Abdomen_3", style=H.inner_glow(SAC, 1.0))
    b.ring("VentRing", vent, G(0, 0.3, -1.0), R(0.055), R(0.018), arcs=3, sides=8, chain=["Vent"], ref=Vector((1, 0, 0)),
           style=H.ring_band(SAC, k, vent, G(0, 0.3, -1.0), beads=8, joint_glow=0.9, inner_glow=0.8))
    n = G(0, 0.3, -1.0).normalized()
    a_ = (Vector((1, 0, 0)) - n * n.dot(Vector((1, 0, 0)))).normalized()
    b_ = n.cross(a_)
    for j in range(6):
        th = math.radians(30 + 60 * j)
        radial = a_ * math.cos(th) + b_ * math.sin(th)
        base = vent + radial * R(0.06)
        b.tube("VentHook", [base, base + radial * R(0.03) + n * R(0.02), base + radial * R(0.035) + n * R(0.055)],
               [R(0.013), R(0.008), R(0.001)], sides=8, chain=["Vent"], style=H.hook(SAC, k, glow=0.8, glow_from=0.7))

    # Hooks along the sac's spine, largest over its middle, and pairs on its flanks: a serrated outline from above.
    for fwd, size, bone in ((-0.14, 0.8, "Abdomen_1"), (-0.28, 1.0, "Abdomen_1"), (-0.42, 1.15, "Abdomen_1"),
                            (-0.56, 1.1, "Abdomen_2"), (-0.7, 0.9, "Abdomen_2")):
        base = P(0, sac_top(0, fwd) - 0.012, fwd)
        b.tube("Spine", [base, base + P(0, 0.06 * size, -0.03 * size), base + P(0, 0.075 * size, -0.085 * size)],
               [R(0.022 * size), R(0.012 * size), R(0.0015)], sides=10, chain=[bone], style=H.hook(PAL, k, glow=1.0, glow_from=0.72))
    for side, sx in SIDES:
        for fwd, bone in ((-0.2, "Abdomen_1"), (-0.55, "Abdomen_2")):
            x = 0.17
            base = P(x * sx, sac_top(x, fwd) - 0.02, fwd)
            b.tube("FlankHook", [base, base + P(0.07 * sx, 0.04, -0.02), base + P(0.1 * sx, 0.03, -0.08)],
                   [R(0.017), R(0.009), R(0.0012)], sides=8, chain=[bone], style=H.hook(PAL, k, glow=0.5))

    for (side, i), lg in legs().items():
        root, c, kn, f, yaw = lg.joints(lg.root, lg.foot)
        tip = f + (f - kn).normalized() * R(0.015)
        b.tube("Coxa", [root, c], [R(0.058), R(0.052)], sides=12, chain=[lg.names[0]], style=H.leg(LEGP, k, "Coxa"))
        b.tube("Femur", [c, c.lerp(kn, 0.5) + Vector((0, 0, R(0.015))), kn], [R(0.05), R(0.056), R(0.042)], sides=12,
               chain=[lg.names[1]], style=H.leg(LEGP, k, "Femur", spines=4))
        kink = kn.lerp(f, 0.42) + yaw * R(0.035) + Vector((0, 0, R(0.012)))
        b.tube("Tibia", [kn, kink, tip], [R(0.04), R(0.03), R(0.003)], sides=12, chain=[lg.names[2]],
               style=H.leg(LEGP, k, "Tibia", spines=2))
        b.tube("Spur", [kn, kn + Vector((0, 0, R(0.075))) - yaw * R(0.018)], [R(0.016), R(0.0012)], sides=8,
               chain=[lg.names[1]], style=H.hook(PAL, k, glow=0.5))
        # A Crysium spur at each knee repeats the crown down the legs.
        b.shard("KneeCrysium", kn - (kn - c).normalized() * R(0.01), Vector((0, 0, 1)) + (kn - c).normalized() * 0.5,
                R(0.016), R(0.09), sides=8, twist=2.0, flute=0.4, bone=lg.names[1], style=Y.shard_style("pink", 0.9, seed=50 + i))
    return b


def bones():
    out = [
        ("Root", P(0, 0, 0), P(0, 0, 0.12), None, True),
        ("Thorax", P(0, 0.23, -0.1), P(0, 0.24, 0.16), "Root", True),
        ("Head", P(0, 0.23, 0.16), P(0, 0.15, 0.42), "Thorax", True),
        ("Abdomen_1", P(0, 0.25, -0.1), P(0, 0.28, -0.45), "Thorax", True),
        ("Abdomen_2", P(0, 0.28, -0.45), P(0, 0.22, -0.75), "Abdomen_1", True),
        ("Abdomen_3", P(0, 0.22, -0.75), P(0, 0.18, -0.92), "Abdomen_2", True),
        ("Vent", P(*VENT), P(*VENT) + G(0, 0.3, -1.0).normalized() * R(0.08), "Abdomen_3", True),
    ]
    for side, sx in SIDES:
        c = P(SIDE_SAC[0] * sx, SIDE_SAC[1], SIDE_SAC[2])
        out.append((f"Sac_{side}", c, c + P(0.1 * sx, 0, 0), "Abdomen_1", True))
    for name in JAWS:
        pts = jaw_points(name)
        out.append((f"Jaw_{name}", pts[0], pts[-1], "Head", True))
    for side, sx in SIDES:
        shoulder, elbow, blade = blade_points(sx)
        out.append((f"Blade_{side}_1", shoulder, elbow, "Thorax", True))
        out.append((f"Blade_{side}_2", elbow, blade[-1], f"Blade_{side}_1", True))
    for lg in legs().values():
        out += lg.rest_bones("Thorax")
    return out


SCALE_BONES = ("Sac_L", "Sac_R", "Vent")


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, roll=0.0, sway=0.0, thorax_pitch=0.0, head_pitch=0.0, head_yaw=0.0, jaw=0.0, blade=0.0,
               spread=0.0, reach=0.0, belly=0.0, tail=0.0, sacs=1.0, vent=1.0):
    """jaw: how far the four jaws open (rad); blade lifts the fore-limbs, reach swings the sickles forward and down,
    spread swings both outward; belly pitches the sac (+ down), tail the rear lobe; sacs/vent: their scale."""
    specs = {
        "Root": {"rot": []},
        "Thorax": {"rot": [(Yv, roll), (X, thorax_pitch)], "loc": offset if offset is not None else Vector()},
        "Head": {"rot": [(X, head_pitch), (Z, head_yaw)]},
        "Abdomen_1": {"rot": [(Z, sway), (X, belly)]},
        "Abdomen_2": {"rot": [(Z, sway * 0.9), (X, belly * 0.7)]},
        "Abdomen_3": {"rot": [(Z, sway * 0.6), (X, tail)]},
        "Vent": {"rot": [], "scale": vent},
        "Sac_L": {"rot": [], "scale": sacs},
        "Sac_R": {"rot": [], "scale": sacs},
    }
    for name in JAWS:
        sx = -1 if name[1] == "L" else 1
        up = -1 if name[0] == "U" else 1
        specs[f"Jaw_{name}"] = {"rot": [(X, up * jaw * 0.8), (Z, sx * jaw * 0.6)]}
    for side, sx in SIDES:
        specs[f"Blade_{side}_1"] = {"rot": [(X, -blade), (Z, sx * spread)]}
        specs[f"Blade_{side}_2"] = {"rot": [(X, reach)]}
    return specs


def place_legs(poser, specs, feet):
    pose = poser.solve(specs)
    thorax = pose["Thorax"] @ poser.rest["Thorax"].inverted()
    for key, lg in legs().items():
        specs.update(lg.pose(poser, thorax @ lg.root, feet[key]))
    return specs


def s_(a, b, t):
    return C.smoothstep(a, b, np.array([t]))[0]


WALK_FRAMES = 48
STRIDE, LIFT, SWING = 0.22 * M, 0.085 * M, 0.25


def walk_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Each footfall drops the body a little; the sac swings behind it, half a beat late.
        specs = body_specs(offset=Vector((0, 0, -R(0.006) + R(0.012) * math.cos(4 * w))), roll=d(2.5) * math.sin(w),
                           sway=d(5) * math.sin(w - 0.9), thorax_pitch=d(1.5) * math.sin(4 * w),
                           head_pitch=d(2) * math.sin(4 * w + 1), head_yaw=d(4) * math.sin(w),
                           jaw=d(6) * (0.5 + 0.5 * math.sin(2 * w)), blade=d(4) * math.sin(2 * w),
                           spread=d(2) * math.sin(w + 1), belly=d(2.5) * math.sin(4 * w - 1.2), tail=d(4) * math.sin(2 * w - 1.6),
                           sacs=1.0 + 0.025 * math.sin(4 * w - 1.4))
        feet = {}
        for key, lg in legs().items():
            fwd, up = C.step_curve((t + STEP_PHASE[key]) % 1.0, 1 - SWING, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w)) ** 3
        breathe = math.sin(w)
        specs = body_specs(offset=Vector((0, 0, R(0.004) * breathe)), roll=d(1) * math.sin(w), sway=d(3) * math.sin(w + 1),
                           head_yaw=d(10) * math.sin(w) * math.sin(0.5 * w + 0.3), head_pitch=d(3) * math.sin(2 * w + 0.5),
                           jaw=d(24) * snap, blade=d(8) * math.sin(w + 0.4) + d(5) * snap, spread=d(4) * math.sin(2 * w),
                           reach=-d(6) * snap, belly=d(2) * breathe, tail=d(4) * math.sin(w + 0.8),
                           sacs=1.0 + 0.05 * breathe, vent=1.0 + 0.08 * max(0.0, math.sin(w + 2.0)))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 80, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        specs = body_specs(offset=Vector((0, R(0.025) * k, R(0.018) * k)), thorax_pitch=-d(8) * k, head_pitch=-d(12) * k,
                           belly=d(6) * k, tail=d(8) * k, jaw=d(30) * max(k, 0), blade=d(18) * k, spread=d(10) * k,
                           reach=-d(12) * k, sacs=1.0 - 0.06 * k)
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


DEATH_FRAMES = 48


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = s_(0.12, 0.6, t)
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 55) * (1 if t > 0.6 else 0)
        # Too heavy to flip: she rolls onto her right side and the sac slumps flat.
        specs = body_specs(offset=Vector((0, 0, R(0.03) * jerk)), head_pitch=d(20) * fall, jaw=d(38) * fall + d(6) * twitch,
                           blade=-d(30) * fall, reach=d(45) * fall + d(6) * twitch, spread=d(15) * fall,
                           belly=d(10) * fall, tail=d(18) * fall, sacs=1.0 - 0.18 * fall, vent=1.0 + 0.3 * fall)
        pivot = P(0.3, 0, 0)
        angle = d(70) * fall
        rot = Matrix.Rotation(angle, 3, "Y")
        specs["Root"] = {"rot": [(Yv, angle)], "loc": pivot - rot @ pivot + Vector((0, 0, -R(0.06) * fall))}
        curl = s_(0.1, 0.7, t)
        for (side, i), lg in legs().items():
            sx = -1 if side == "L" else 1
            _, _, _, _, yaw_dir = lg.joints(lg.root, lg.foot)
            hinge = Z.cross(yaw_dir).normalized()
            kick = twitch * (0.25 if (side, i) in (("L", 1), ("R", 2)) else -0.25)
            specs[lg.names[0]] = {"rot": [(Yv, -sx * d(15) * curl)]}
            specs[lg.names[1]] = {"rot": [(hinge, d(50) * curl + kick)]}
            specs[lg.names[2]] = {"rot": [(hinge, d(70) * curl - kick)]}
        return specs
    return DEATH_FRAMES, spec, False


SPAWN_FRAMES = 60
# The vent is open widest here: spawn the broodlings at the vent bone (Unity: BroodMother_Rig/.../Vent).
SPAWN_RELEASE_FRAME = 40


def spawn_spec(poser):
    d = math.radians

    def spec(t, f):
        brace = s_(0.0, 0.18, t) * (1 - s_(0.85, 1.0, t))
        # Two squeezes of the side sacs, then the vent opens and the young come out.
        squeeze = 0.22 * math.sin(math.pi * s_(0.15, 0.4, t)) + 0.26 * math.sin(math.pi * s_(0.4, 0.68, t))
        lower = s_(0.2, 0.55, t) * (1 - s_(0.78, 0.98, t))
        opening = math.sin(math.pi * s_(0.5, 0.88, t))
        pulse = math.sin(t * 2 * math.pi * 6) * lower
        specs = body_specs(offset=Vector((0, 0, -R(0.035) * brace)), thorax_pitch=-d(5) * brace,
                           head_pitch=-d(10) * brace + d(2) * pulse, jaw=d(30) * brace + d(8) * opening,
                           blade=d(32) * brace, spread=d(18) * brace, reach=-d(20) * brace,
                           belly=d(9) * lower + d(1.5) * pulse, tail=d(34) * lower,
                           sacs=1.0 + 0.06 * brace - squeeze, vent=1.0 + 0.75 * opening)
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return SPAWN_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Spawn": spawn_spec}
# LOD1: jaws into the head, the rear lobe and the vent into the sac, each sickle one bone. 21 bones.
LOD_DROP = {**{f"Jaw_{n}": "Head" for n in JAWS}, "Abdomen_3": "Abdomen_2", "Vent": "Abdomen_2",
            "Blade_L_2": "Blade_L_1", "Blade_R_2": "Blade_R_1"}
LOD_AIMS = {"Blade_L_1": ("Blade_L_1", "Blade_L_2"), "Blade_R_1": ("Blade_R_1", "Blade_R_2")}
# Preview aim (Blender space): the middle of the body between the blades and the vent.
TARGET = (0.0, 0.12, 0.38)


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0052, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.14,
                               extrusion=0.02, ray=0.06, emission_strength=2.5,
                               extra=H.weathering(K, markings=PAL.dark, marking_amount=0.65, seed=61), specular=0.3,
                               mirror=True, k=DETAIL_K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid={"VentRing", "VentHook"}, blend=1)
    poser = B.ScalePoser(arm, SCALE_BONES)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      SCALE_BONES)
    C.write_report(NAME, {"asset": NAME, "base_species": "Skitter", "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
                          "low_texture_size": LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "scale_keyed_bones": list(SCALE_BONES),
                          "spawn_release_frame": SPAWN_RELEASE_FRAME, "spawn_point_bone": "Vent",
                          "walk_distance_per_loop": round(STRIDE / (1 - SWING), 3),
                          "unity_scale": GAME_SCALE,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    walk = (WALK_FRAMES * 4) // 48
    B.boss_previews(
        "broodmother", arm, asset.low, actions, lod, out, asset, target=TARGET, distance=5.5,
        game_scale=GAME_SCALE, base=dict(asset="Skitter", scale=SKITTER_SCALE, clip="Walk", frame=6),
        move=("Walk", walk), power=("Spawn", SPAWN_RELEASE_FRAME),
        strips=[("Walk", "side", range(0, WALK_FRAMES, 6)), ("Spawn", "side", range(0, SPAWN_FRAMES, 8)),
                ("Death", "close", range(0, DEATH_FRAMES + 1, 7))],
        macros=[("sac", P(-0.18, 0.32, -0.42), 1.7, -70, 35), ("head", P(0.05, 0.25, 0.32), 1.5, -30, 22),
                ("vent", P(0, 0.2, -0.95), 1.3, -150, 20)],
        rig_scale=2.2, minions=[(-1.4, 0.9, 0.2, 3), (-0.9, -0.6, -0.1, 10), (1.3, 0.7, 0.3, 17), (1.6, -0.4, 0.0, 21),
                                (-1.8, 0.0, 0.1, 14), (0.4, 1.3, -0.2, 7)])


if __name__ == "__main__":
    main()
