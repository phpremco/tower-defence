"""Slagstrider: the fire-element stilt walker that wades through lava and leaves a burning trail (round 15, name
proposed; redesigned from scratch in round 49B, see ArtSource/DESIGN_NOTES.md).

Round 49B (Remco on the round-15 model: "ugly"): a walking crucible. A teardrop of hot rock under a shell of slag
plates with molten seams hangs between four long stilt legs: each thigh is a flat obsidian blade serrated along its
top edge that rises to a glowing knee with a spike, high above the back, and each shin drops to a heat-proof cone of
chilled slag drums. Furnace chimneys with white-hot mouths and a crest of obsidian spines run down the spine; the
molten sac under the belly drips the burning trail (bone `Drip`). A long armoured wedge head on a drum neck, a
glowing slit jaw underneath, two blade mandibles hooked in under the snout, a crown of back spikes, and a row of
small ember eye pits on each flank (the Face part). From above: a glowing teardrop in a wide X of blades.

Clips: Walk (loop, diagonal pairs), Idle (loop), Hit, Death. Also exports Slagstrider_LOD1.

Run: blender --background --python Tools/Blender/build_slagstrider.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "Slagstrider"
K = 1.0
GAME_SCALE = 0.8  # proposal

# Fire element: charcoal slag, orange-yellow molten glow.
PAL = H.Palette(top=(0.17, 0.15, 0.14), side=(0.07, 0.06, 0.055), belly=(0.03, 0.025, 0.022), seam=(0.02, 0.012, 0.008),
                dark=(0.045, 0.035, 0.03), glow=(1.0, 0.5, 0.1), eye=(1.0, 0.85, 0.45), membrane=(0.45, 0.12, 0.03),
                bone=(0.5, 0.44, 0.38))
HOT = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=PAL.dark, glow=(1.0, 0.72, 0.28),
                eye=PAL.eye, membrane=PAL.membrane, bone=PAL.bone)
WHITE_HOT = (1.0, 0.85, 0.5)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

LEG_TABLE = [((0.14, 0.5, 0.14), (0.48, 0.0, 0.44), (0.58, 0.64)),
             ((0.14, 0.5, -0.14), (0.48, 0.0, -0.42), (0.58, 0.64))]
# Round 49B: slag shell plates over the crucible (x, up, fwd), radii, yaw, pitch: a keel row down the spine and two
# shingled flank rows, each lifted at its rear edge so the molten seam shows behind it.
PLATES = [((0.0, 0.6, 0.16), (0.1, 0.1, 0.035), 0, -14), ((0.0, 0.615, 0.0), (0.11, 0.11, 0.035), 0, -10),
          ((0.0, 0.6, -0.16), (0.1, 0.1, 0.035), 0, -6), ((0.11, 0.57, 0.12), (0.075, 0.1, 0.032), 18, -12),
          ((0.125, 0.575, -0.03), (0.08, 0.11, 0.032), 4, -8), ((0.11, 0.56, -0.18), (0.075, 0.09, 0.032), -12, -4),
          ((0.17, 0.48, 0.05), (0.06, 0.13, 0.04), 6, 0), ((0.16, 0.47, -0.14), (0.055, 0.11, 0.04), -10, 0)]
VENTS = [(0.06, 0.1), (-0.06, 0.09), (-0.17, 0.075)]  # (fwd, radius) on the spine between the keel plates
SLIT = G(0, 0.455, 0.53)
# Eyes (the Face part puts the lenses here): a row of three small ember pits per side along the head's flank.
EYES = [((0.062, 0.52, 0.5), 0.016), ((0.07, 0.522, 0.45), 0.013), ((0.072, 0.522, 0.405), 0.011)]


def legs():
    return K3.limbs(LEG_TABLE, pole=(0.1, 1.0, 0.0))


def rot(yaw=0.0, pitch=0.0, roll=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Y"))


def serrate(b, a, c, chain, style, count, length, out_dir):
    """A row of short obsidian teeth along the straight edge a -> c, raked towards c."""
    a, c, out_dir = Vector(a), Vector(c), Vector(out_dir).normalized()
    along = (c - a).normalized()
    for j in range(count):
        p = a.lerp(c, (j + 0.5) / count)
        ln = length * (1.0 - 0.35 * j / max(count - 1, 1))
        b.tube("Serration", [p, p + out_dir * ln * 0.6 + along * ln * 0.2, p + out_dir * ln + along * ln * 0.5],
               [ln * 0.3, ln * 0.15, 0.001], sides=6, chain=chain, style=style)


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    blade = H.blade(HOT, k, glow=0.9, teeth=5)
    obsidian = H.hook(PAL, k, glow=0.9, glow_from=0.6)
    # The crucible: a teardrop of hot rock that sags towards the molten sac under it (the trail starts at Drip).
    b.ellipsoid("Body", G(0, 0.5, 0.0), (0.17, 0.27, 0.13), bone="Body", segments=(40, 20),
                style=S.core(PAL, k, seed=1, cells=50, width=0.026, glow=1.0))
    b.ellipsoid("Belly", G(0, 0.39, -0.03), (0.13, 0.19, 0.1), bone="Body", segments=(32, 16),
                style=T.kiln_crust(PAL, k, cells=12, seed=2, glow=1.0, under=0.5, rifts=0.4))
    b.tube("DripSac", [G(0, 0.34, -0.03), G(0, 0.29, -0.035), G(0, 0.24, -0.03)], [0.05, 0.035, 0.008], sides=12,
           chain=["Drip"], style=T.kiln_crust(PAL, k, cells=6, seed=3, glow=1.0, under=0.9), cap=0.5)
    for i, (p, r, yaw, pitch) in enumerate(PLATES):
        sides = [(None, 1)] if p[0] == 0.0 else SIDES
        for side, sx in sides:
            b.ellipsoid("Plate", K3.gp(p, sx), r, rot(yaw * sx, pitch, 26 * sx * (p[0] != 0) + 40 * sx * (p[1] < 0.5)),
                        bone="Body", segments=(24, 12), deform=C.chiselled(8, 50 + i, depth=0.2, squash=(0.4, 1.0)),
                        style=S.slab(PAL, k, strata=30, veins=0.7, fault=1.0, seed=10 + i, rim=-0.2))
    # Furnace chimneys between the keel plates, raked back, white-hot mouths (heat shimmer and embers in Unity).
    for f, r in VENTS:
        base = G(0, 0.6, f)
        top = base + G(0, r * 0.9, -r * 0.7)
        b.tube("Vent", [base - G(0, 0.03, 0), base.lerp(top, 0.5), top], [r * 0.42, r * 0.36, r * 0.4], sides=10,
               chain=["Body"], style=S.pillar(PAL, k, drums=2, seed=20, glow=0.6), cap=0.0)
        b.ellipsoid("VentFire", top + G(0, 0.003, -0.002), (r * 0.3, r * 0.3, r * 0.1),
                    rot(pitch=-38), bone="Body", style=T.solid_glow(WHITE_HOT, 1.0), segments=(12, 6))
    # A crest of back-swept obsidian spines along both sides of the chimneys.
    for j, f in enumerate((0.12, 0.0, -0.12, -0.22)):
        for side, sx in SIDES:
            base = K3.gp((0.055, 0.62 - 0.01 * j, f), sx)
            ln = 0.1 - 0.015 * j
            b.tube("Spine", [base, base + K3.gp((0.015, ln * 0.6, -ln * 0.4), sx), base + K3.gp((0.03, ln, -ln * 1.0), sx)],
                   [0.016, 0.009, 0.001], sides=6, chain=["Body"], style=obsidian)
    # Neck and head: a long armoured wedge on a drum neck; a slit jaw underneath; two blade mandibles hooked in
    # under the snout; a crown of back spikes. Small ember eye pits in a row on each flank (the Face part).
    b.tube("Neck", [G(0, 0.52, 0.2), G(0, 0.55, 0.27), G(0, 0.53, 0.34)], [0.065, 0.055, 0.05], sides=10,
           chain=["Neck"], style=S.pillar(PAL, k, drums=3, seed=30, glow=0.8))
    b.ellipsoid("Head", G(0, 0.51, 0.44), (0.075, 0.16, 0.065), rot(pitch=12), bone="Head", segments=(32, 16),
                deform=C.chiselled(9, 61, depth=0.12), style=S.slab(PAL, k, strata=40, veins=0.5, fault=0.6, seed=31))
    b.ellipsoid("HeadKeel", G(0, 0.56, 0.42), (0.03, 0.14, 0.04), rot(pitch=8), bone="Head", segments=(20, 10),
                deform=C.chiselled(7, 62, depth=0.12), style=S.slab(PAL, k, strata=40, veins=0.3, fault=0.3, seed=32))
    b.ellipsoid("Slit", SLIT, (0.014, 0.085, 0.035), rot(pitch=12), bone="Head", style=H.inner_glow(HOT, 1.0),
                segments=(12, 10))
    for (x, u, f), r in EYES:
        for side, sx in SIDES:
            b.ellipsoid("EyePit", K3.gp((x, u, f), sx), (r * 1.25, r * 1.15, r * 0.95), bone="Head", segments=(12, 6),
                        style=H.inner_glow(HOT, 0.6))
    for side, sx in SIDES:
        root = K3.gp((0.04, 0.45, 0.5), sx)
        pts = [root, root + K3.gp((0.04, -0.02, 0.06), sx), root + K3.gp((0.03, -0.035, 0.14), sx),
               root + K3.gp((-0.02, -0.035, 0.19), sx)]
        b.tube("Mandible", pts, [0.022, 0.018, 0.01, 0.001], sides=8, chain=["Head"], flatten=0.45, style=blade)
        for j, (x, f) in enumerate(((0.035, 0.38), (0.06, 0.34), (0.07, 0.29))):
            base = K3.gp((x, 0.56, f), sx)
            b.tube("Crown", [base, base + K3.gp((0.025, 0.05, -0.05), sx), base + K3.gp((0.04, 0.08, -0.13), sx)],
                   [0.016, 0.01, 0.001], sides=8, chain=["Head"], style=obsidian)
    # Stilt legs: a flat obsidian blade from the hip up to a glowing knee joint with a spike, then a long shin down
    # to a heat-proof cone of chilled slag drums (they wade lava), serrated along the blade's top edge.
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.ellipsoid("Hip", hip, (0.05,) * 3, bone=lg.names[0], style=S.slab(PAL, k, seed=60 + i), segments=(14, 8))
        mid = hip.lerp(knee, 0.5) + Vector((0, 0, 0.03))
        b.tube("LegUpper", [hip, mid, knee], [0.06, 0.052, 0.032], sides=10, chain=[lg.names[0]], flatten=0.38,
               style=S.pillar(PAL, k, drums=2, seed=62 + i, glow=0.5))
        serrate(b, hip.lerp(knee, 0.25) + Vector((0, 0, 0.035)), hip.lerp(knee, 0.85) + Vector((0, 0, 0.03)),
                [lg.names[0]], obsidian, 4, 0.045, Vector((0, 0, 1)) + (knee - hip).normalized() * 0.3)
        b.ellipsoid("Knee", knee, (0.04,) * 3, bone=lg.names[0], style=S.core(PAL, k, seed=64 + i, cells=12, glow=1.0),
                    segments=(14, 8))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        b.tube("KneeSpike", [knee + Vector((0, 0, 0.02)), knee + K3.gp((0.02, 0.07, -0.03), sx),
                             knee + K3.gp((0.03, 0.13, -0.07), sx)], [0.02, 0.012, 0.001], sides=6,
               chain=[lg.names[0]], style=obsidian)
        b.tube("LegLower", [knee, knee.lerp(foot, 0.6), foot + Vector((0, 0, 0.12))], [0.026, 0.02, 0.026], sides=10,
               chain=[lg.names[1]], style=S.pillar(PAL, k, drums=4, seed=66 + i, glow=0.5))
        b.tube("FootCone", [foot + Vector((0, 0, 0.16)), foot + Vector((0, 0, 0.1)), foot + Vector((0, 0, 0.04)),
                            foot + Vector((0, 0, 0.0))], [0.03, 0.045, 0.026, 0.006], sides=10, chain=[lg.names[1]],
               style=S.pillar(PAL, k, drums=3, seed=68 + i, glow=0.9), cap=0.3)
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.49, -0.2), G(0, 0.51, 0.18), "Root", True),
           ("Neck", G(0, 0.52, 0.2), G(0, 0.53, 0.34), "Body", True),
           ("Head", G(0, 0.53, 0.34), G(0, 0.48, 0.58), "Neck", True),
           ("Drip", G(0, 0.32, -0.02), G(0, 0.22, -0.02), "Body", True)]
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, neck=0.0, head=0.0, head_yaw=0.0):
    return {"Root": {"rot": []},
            "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
            "Neck": {"rot": [(Z, head_yaw * 0.5), (X, neck)]},
            "Head": {"rot": [(Z, head_yaw * 0.5), (X, head)]},
            "Drip": {"rot": []}}


WALK_FRAMES = 36
STRIDE, LIFT, STANCE = 0.24, 0.1, 0.62
PHASES = {("L", 1): 0.0, ("R", 2): 0.0, ("R", 1): 0.5, ("L", 2): 0.5}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.012 * math.cos(2 * w))), roll=math.radians(3) * math.sin(w),
                           yaw=math.radians(3) * math.sin(w), pitch_=math.radians(1.5) * math.sin(2 * w + 0.4),
                           neck=math.radians(3) * math.sin(2 * w + 1.0), head=-math.radians(3) * math.sin(2 * w + 1.0),
                           head_yaw=math.radians(6) * math.sin(w))
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.01 * math.sin(w))), roll=math.radians(1.5) * math.sin(w),
                           pitch_=math.radians(2) * math.sin(w + 0.3), neck=math.radians(5) * math.sin(w),
                           head=math.radians(6) * math.sin(2 * w), head_yaw=math.radians(16) * math.sin(w + 1.0))
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.03 * k, 0.02 * k)), pitch_=math.radians(-7) * k, roll=math.radians(6) * k,
                           neck=math.radians(-12) * k, head=math.radians(-8) * k)
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        fall = K2.ease(0.1, 0.6, t)
        specs = body_specs(offset=Vector((0, 0, 0.03 * jerk - 0.36 * fall)), roll=math.radians(14) * fall,
                           pitch_=math.radians(8) * fall, neck=math.radians(25) * fall, head=math.radians(15) * fall)
        return K3.splay_feet(poser, specs, lg, fall, out=1.15)
    return 42, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
RIGID = {"Plate", "Head", "HeadKeel"}
# LOD1: each leg one bone, Drip dropped into the body: 8 bones (the trail origin is then the body: 0.33 below it).
LOD_DROP = {"Drip": "Body", **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2)}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2)}


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 12, (0.3, 0.9), -0.15), ("Idle", 20, (1.3, 0.3), 0.3),
              ("Walk", 6, (-0.4, -0.5), 0.0), ("Walk", 24, (0.9, -0.7), -0.3)]
    previews = dict(target=(0, 0.0, 0.35), distance=2.8, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Death", "close", range(0, 43, 6))],
                    macros=[("plates", G(0.05, 0.58, 0.0), 0.45, -50, 35), ("head", G(0.0, 0.5, 0.45), 0.4, -30, 12),
                            ("foot", G(0.42, 0.06, 0.42), 0.35, -60, 15)],
                    rig_scale=1.4)
    K3.run_creature(NAME, builder, bones(), CLIPS, tris=3950, families=("rock", "membrane", "chitin"), game_scale=GAME_SCALE,
                    rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS,
                    extra=H.weathering(K, scratches=0.4, dust=0.2, seed=29), specular=0.25, emission_strength=3.0,
                    report={"walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                            "trail_origin": "bone Drip (under the belly, 0.22 above the ground; LOD1: Body)",
                            "footfalls": "Walk frames 0 (L1, R2) and 18 (R1, L2)"},
                    previews=previews)
    # Round 46: the close-up detail (eyes, slit-jaw fangs, foot spurs) as extra parts, exported again with the body.
    import slagstrider_detail
    slagstrider_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
