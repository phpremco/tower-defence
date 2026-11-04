"""Shardback: the ice beetle whose crust of ice prisms is its armour (round 15, see ArtSource/DESIGN_NOTES.md).

A low, wide slate-violet carapace on six stocky legs; six clusters of twisted ice prisms grow from glowing amber
sockets on the back (two on the midline, two pairs on the flanks). Each cluster is its own mesh island on its own
bone (Shard_1..6, the shedding order), rigid, so Unity hides a chipped cluster by scaling its bone to 0 and the
amber socket shows. A plough head: a wide brow plate with a frosted horn, two side-closing frost mandibles round an
amber throat (the frost spit), four amber eyes. Fixed frost crystals along the flank edges.

Clips: Walk (loop), Idle (loop), Hit, Death, Shatter (the six clusters burst off in order). Only Shatter keys the
shard bones' scale. Also exports Shardback_LOD1 (shard bones kept, rigid).

Run: blender --background --python Tools/Blender/build_shardback.py -- [--preview <dir>] [--norender]
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
from common import G  # noqa: E402

NAME = "Shardback"
K = 1.0
GAME_SCALE = 0.8  # EnemyDefinition scale

# Data colours (ProjectGenerator.Bestiary): pale blue body, ice-white eyes and accent. The element lives in the ice;
# the body is dark slate-violet so the ice reads against it, and the glow is warm amber (alien rule).
PAL = H.Palette(top=(0.24, 0.24, 0.38), side=(0.085, 0.085, 0.15), belly=(0.03, 0.03, 0.05), seam=(0.02, 0.02, 0.035),
                dark=(0.05, 0.05, 0.09), glow=(1.0, 0.55, 0.16), eye=(1.0, 0.72, 0.3), membrane=(0.38, 0.14, 0.05),
                bone=(0.78, 0.86, 0.98))
LEGP = H.Palette(top=(0.17, 0.17, 0.28), side=(0.07, 0.07, 0.12), belly=(0.03, 0.03, 0.05), seam=(0.02, 0.02, 0.035),
                 dark=(0.04, 0.04, 0.07), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

# The back: the elytra ellipsoid (centre in game coords, radii x / length / height).
ELY_C = (0.0, 0.29, -0.07)
ELY_R = (0.27, 0.37, 0.12)

# Clusters in shedding order: (bone, socket centre (x, fwd) right side or midline, mirrored side, prisms).
# A prism: (dx, dfwd) from the socket, lean (x, up, fwd), radius, length, twist (radians).
CLUSTERS = [
    ("Shard_1", (0.0, -0.27), None, [((0.035, 0.0), (0.22, 1.0, -0.5), 0.034, 0.23, 0.6),
                                     ((-0.035, 0.0), (-0.22, 1.0, -0.5), 0.034, 0.23, -0.6)]),
    ("Shard_2", (0.165, -0.16), "L", [((0.0, 0.0), (0.55, 1.0, -0.45), 0.036, 0.25, 0.7),
                                      ((0.04, 0.03), (1.0, 0.6, -0.05), 0.021, 0.12, -0.5),
                                      ((-0.02, -0.04), (0.25, 1.0, -0.75), 0.019, 0.11, 0.4)]),
    ("Shard_3", (0.165, -0.16), "R", None),
    ("Shard_4", (0.0, 0.0), None, [((0.042, 0.0), (0.16, 1.0, -0.45), 0.046, 0.36, 0.5),
                                   ((-0.042, 0.0), (-0.16, 1.0, -0.45), 0.046, 0.36, -0.5),
                                   ((0.07, -0.06), (0.55, 1.0, -0.35), 0.024, 0.15, -0.4),
                                   ((-0.07, -0.06), (-0.55, 1.0, -0.35), 0.024, 0.15, 0.4)]),
    ("Shard_5", (0.185, 0.1), "L", [((0.0, 0.0), (0.5, 1.0, -0.3), 0.038, 0.27, 0.6),
                                    ((0.045, 0.02), (1.0, 0.55, 0.15), 0.022, 0.13, -0.6)]),
    ("Shard_6", (0.185, 0.1), "R", None),
]
SHARD_NAMES = [c[0] for c in CLUSTERS]


def surface(x, f):
    """Height and normal of the elytra at (x, forward)."""
    cx, cu, cf = ELY_C
    rx, ry, rz = ELY_R
    q = 1 - (x / rx) ** 2 - ((f - cf) / ry) ** 2
    h = cu + rz * math.sqrt(max(q, 0.0))
    n = G(x / rx ** 2, (h - cu) / rz ** 2, (f - cf) / ry ** 2).normalized()
    return h, n


def clusters():
    """Expanded clusters: [(bone, sx, socket (game x, fwd), prisms)] with mirrored flank pairs."""
    out = []
    for name, socket, side, prisms in CLUSTERS:
        if prisms is None:
            prisms = [c for c in CLUSTERS if c[3] is not None and c[1] == socket][0][3]
        sx = -1 if side == "L" else 1
        out.append((name, sx, (socket[0] * sx, socket[1]), prisms))
    return out


def hex_profile(t):
    """A blunt hexagonal column that ends in a short point (not a needle)."""
    if t < 0.72:
        return 1.0 - 0.12 * t
    return max(0.0, (1.0 - t) / 0.28) ** 0.9 * 0.91


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


LEG_TABLE = [((0.16, 0.19, 0.2), (0.4, 0.0, 0.32), (0.2, 0.22)),
             ((0.185, 0.19, 0.03), (0.44, 0.0, 0.04), (0.21, 0.23)),
             ((0.17, 0.19, -0.15), (0.41, 0.0, -0.27), (0.2, 0.22))]


def legs():
    return K3.limbs(LEG_TABLE)


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    b.ellipsoid("Under", G(0, 0.2, -0.03), (0.22, 0.34, 0.1), bone="Body",
                style=H.glow_flesh(PAL, k, glow=0.55, flank=(0.6, 0.95), folds=10.0), segments=(40, 20))
    b.ellipsoid("Elytra", G(*ELY_C), ELY_R, bone="Body", segments=(48, 24),
                style=K2.armour_plate(PAL, k, rim_glow=0.7, chevron=0.0, lip=0.5, cells=18, seed=3))
    b.ellipsoid("Pronotum", G(0, 0.3, 0.22), (0.23, 0.13, 0.085), pitch(-8), bone="Body", segments=(40, 16),
                style=K2.armour_plate(PAL, k, rim_glow=0.8, chevron=0.9, chevron_angle=1.1, lip=0.8, cells=10, seed=5))
    # The plough head: brow plate, horn, mandibles round an amber throat, four eyes.
    b.ellipsoid("Head", G(0, 0.2, 0.36), (0.14, 0.1, 0.08), bone="Head", style=H.band_plates(PAL, k, 2, height=0.002),
                segments=(32, 16))
    b.ellipsoid("Brow", G(0, 0.265, 0.39), (0.2, 0.09, 0.04), pitch(-14), bone="Head", segments=(36, 14),
                style=K2.armour_plate(PAL, k, rim_glow=0.4, chevron=0.0, lip=1.0, cells=8, seed=7))
    b.tube("Horn", [G(0, 0.29, 0.41), G(0, 0.35, 0.47), G(0, 0.41, 0.47)], [0.034, 0.022, 0.003], sides=10,
           chain=["Head"], style=K3.ice_prism(k, warm_glow=0.6, seed=9))
    b.ellipsoid("Throat", G(0, 0.165, 0.45), (0.045, 0.025, 0.03), bone="Head", style=H.inner_glow(PAL, 1.0), segments=(16, 8))
    for side, sx in SIDES:
        b.tube("Mandible", [K3.gp((0.095, 0.16, 0.42), sx), K3.gp((0.09, 0.15, 0.5), sx), K3.gp((0.03, 0.14, 0.56), sx)],
               [0.03, 0.022, 0.004], sides=10, chain=[f"Jaw_{side}"], style=H.blade(PAL, k, glow=0.9, teeth=3), flatten=0.55)
        for p, r in (((0.08, 0.232, 0.447), 0.017), ((0.125, 0.222, 0.42), 0.013)):
            b.ellipsoid("Eye", K3.gp(p, sx), (r, r, r * 0.9), bone="Head", style=H.eye(PAL, k, 0.95), segments=(12, 6))
        # Fixed frost crust along the flank edge: the outline stays spiky when the clusters are gone.
        for i in range(7):
            f = 0.17 - i * 0.075
            h, n = surface(0.24, f)
            base = K3.gp((0.235, h - 0.035, f), sx)
            d = K3.gp((1.0, 0.45 + 0.2 * (i % 2), -0.35), sx).normalized()
            b.shard("Frost", base, d, 0.017 + 0.004 * (i % 3), 0.07 + 0.02 * ((i + 1) % 3), sides=6, flute=0.2,
                    twist=0.5 * sx, profile=hex_profile, bone="Body", chain=["Body"],
                    style=K3.ice_prism(k, warm_glow=0.45, seed=20 + i))
    # Sockets and ice clusters (own islands: the prisms float a hair above the socket so the remesh keeps them apart).
    for name, sx, (x, f), prisms in clusters():
        h, n = surface(x, f)
        centre = G(x, h, f)
        foot = max(math.hypot(dx, df) + r * 1.15 for (dx, df), _, r, _, _ in prisms)
        b.ellipsoid("Socket", centre - n * 0.008, (foot * 0.85, foot * 0.85, 0.022), n.to_track_quat("Z", "Y").to_matrix(),
                    bone="Body", style=K3.socket_floor(PAL, k, glow=0.55, seed=31), segments=(24, 10))
        # A low lip of chitin round the socket, clear of the prism footprint (a gap of a few voxels keeps the
        # cluster its own island).
        b.ring("Collar", centre - n * 0.002, n, foot + 0.022, 0.009, arcs=3, sides=6, chain=["Body"],
               style=K2.armour_plate(PAL, k, rim_glow=0.0, chevron=0.0, lip=0.6, seed=33), ref=Vector((1, 0, 0)))
        for (dx, df), lean, r, length, twist in prisms:
            px, pf = x + dx * sx, f + df
            # Measured from the socket's own (tilted) plane, so no prism base dips into the socket disc.
            flat = G(px, h, pf)
            base = flat - n * (flat - centre).dot(n) + n * 0.04
            d = K3.gp(lean, sx).normalized()
            b.shard("Prism", base, d, r, length, sides=6, flute=0.12, twist=twist * sx, profile=hex_profile, rings=12,
                    bone=name, chain=[name], style=K3.ice_prism(k, seed=40 + int(1000 * abs(px + pf))))
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.014)), knee], [0.055, 0.05, 0.036], sides=10,
               chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=3), flatten=0.75)
        b.ellipsoid("Knee", knee, (0.04,) * 3, bone=lg.names[0], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(14, 7))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        tip = foot + (foot - knee).normalized() * 0.012
        b.tube("LegLower", [knee, knee.lerp(foot, 0.45) + K3.gp((0.014, 0.012, 0), sx), tip], [0.036, 0.026, 0.004],
               sides=10, chain=[lg.names[1]], style=H.leg(LEGP, k, "Tibia"))
        b.shard("Spur", knee + Vector((0, 0, 0.01)), K3.gp((0.35, 1.0, -0.2), sx), 0.013, 0.06, sides=6, flute=0.2,
                profile=hex_profile, bone=lg.names[0], chain=[lg.names[0]], style=K3.ice_prism(k, warm_glow=0.4, seed=60 + i))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.26, -0.12), G(0, 0.28, 0.24), "Root", True),
           ("Head", G(0, 0.22, 0.3), G(0, 0.22, 0.48), "Body", True)]
    for side, sx in SIDES:
        out.append((f"Jaw_{side}", K3.gp((0.095, 0.16, 0.42), sx), K3.gp((0.03, 0.14, 0.56), sx), "Head", True))
    for name, sx, (x, f), prisms in clusters():
        h, n = surface(x, f)
        out.append((name, G(x, h, f), G(x, h, f) + n * 0.15, "Body", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, head=0.0, head_yaw=0.0, jaws=0.0):
    specs = {"Root": {"rot": []},
             "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
             "Head": {"rot": [(Z, head_yaw), (X, head)]}}
    for side, sx in SIDES:
        specs[f"Jaw_{side}"] = {"rot": [(Z, -sx * jaws)]}
    return specs


WALK_FRAMES = 32
STRIDE, LIFT, STANCE = 0.12, 0.045, 0.6
TRIPOD_A = {("L", 1), ("R", 2), ("L", 3)}
PHASES = {key: (0.0 if key in TRIPOD_A else 0.5) for key in [(s, i) for s, _ in SIDES for i in (1, 2, 3)]}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.006 * math.cos(2 * w))), roll=math.radians(2.5) * math.sin(w),
                           yaw=math.radians(2) * math.sin(w), pitch_=math.radians(1) * math.sin(2 * w),
                           head=math.radians(2) * math.sin(2 * w + 0.6), head_yaw=math.radians(4) * math.sin(w + 0.4),
                           jaws=math.radians(6) * (0.5 + 0.5 * math.sin(2 * w)))
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, 0.004 * math.sin(w))), roll=math.radians(1.2) * math.sin(w),
                           pitch_=math.radians(1.5) * math.sin(w + 0.5), head=math.radians(4) * math.sin(w) - math.radians(5) * snap,
                           head_yaw=math.radians(9) * math.sin(w + 1.0), jaws=math.radians(4) + math.radians(22) * snap)
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch_=math.radians(-6) * k, roll=math.radians(4) * k,
                           head=math.radians(-10) * k, jaws=math.radians(25) * max(k, 0.0))
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = K2.ease(0.08, 0.5, t)
        twitch = math.exp(-max(0.0, t - 0.55) * 6) * math.sin(t * 55) * (1 if t > 0.55 else 0)
        specs = body_specs(offset=Vector((0, 0, 0.02 * jerk - 0.12 * fall)), roll=math.radians(9) * fall,
                           pitch_=math.radians(5) * fall, head=math.radians(14) * fall + math.radians(4) * twitch,
                           jaws=math.radians(30) * fall)
        return K3.splay_feet(poser, specs, lg, fall, twitch, kick_keys=TRIPOD_A)
    return 40, spec, False


SHATTER_FRAMES = 40
SHATTER_START = 3       # Shard_1 bursts here
SHATTER_STEP = 5        # frames between clusters
SHATTER_FLIGHT = 10     # frames from burst to gone


def shatter_frames():
    return {n: SHATTER_START + i * SHATTER_STEP for i, n in enumerate(SHARD_NAMES)}


def shatter_spec(poser):
    lg = legs()
    starts = shatter_frames()
    info = {}
    for name, sx, (x, f), prisms in clusters():
        h, n = surface(x, f)
        out = Vector((n.x * 1.6 + 0.3 * sx * (x != 0), n.y + 0.4, 0.0))
        info[name] = (out, sx if x != 0 else (1 if name == "Shard_1" else -1))

    def spec(t, f):
        frame = t * SHATTER_FRAMES
        shake = sum(math.exp(-max(0.0, frame - s) * 0.5) * (frame >= s) for s in starts.values())
        jolt = math.sin(frame * 2.4) * min(1.0, shake)
        specs = body_specs(offset=Vector((0, 0, -0.012 * min(1.0, shake))), roll=math.radians(3) * jolt,
                           pitch_=math.radians(-3) * min(1.0, shake), head=math.radians(-8) * min(1.0, shake),
                           jaws=math.radians(20) * min(1.0, shake))
        for name, start in starts.items():
            out, spin_sign = info[name]
            tau = max(0.0, frame - start) / 30.0
            if frame < start:
                specs[name] = {"rot": [], "scale": 1.0}
                continue
            loc = out * (0.9 * tau) + Vector((0, 0, 1.4 * tau - 4.5 * tau * tau))
            gone = K2.ease(start + 2, start + SHATTER_FLIGHT, frame)
            specs[name] = {"rot": [(Yv, spin_sign * 9.0 * tau), (X, -5.0 * tau)], "loc": loc,
                           "scale": max(0.001, 1.0 - gone)}
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return SHATTER_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Shatter": shatter_spec}
CLIP_SCALES = None  # every clip keys the shard scale (1 outside Shatter)
RIGID = {"Elytra", "Pronotum", "Brow", "Prism", "Collar", "Socket"}
# LOD1: jaws fold into the head, each leg is one bone; the shard bones stay. 15 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2, 3)}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2, 3)}


def chipped_render(arm, actions, out, hidden, path, target=(0, 0, 0.3), distance=2.2):
    """A Shardback with the first clusters shed (bone scale 0, as Unity does it): the amber sockets show."""
    muted = [fc for fc in K2.action_fcurves(actions["Idle"])
             if fc.data_path.endswith(".scale") and any(f'"{n}"' in fc.data_path for n in hidden)]
    for fc in muted:
        fc.mute = True
    C.set_action(arm, actions["Idle"], 15)
    for name in SHARD_NAMES:
        arm.pose.bones[name].scale = (0.001,) * 3 if name in hidden else (1.0, 1.0, 1.0)
    bpy_scene_update()
    cam = C.closeup_camera(Vector(target), distance, azimuth=-38, elevation=34, lens=55)
    C.render(cam, os.path.join(out, path), 1280, 720)
    for name in SHARD_NAMES:
        arm.pose.bones[name].scale = (1.0, 1.0, 1.0)
    for fc in muted:
        fc.mute = False


def bpy_scene_update():
    import bpy
    bpy.context.view_layer.update()


def main():
    builder = build_parts()
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 10, (0.2, 0.9), -0.15), ("Shatter", 12, (1.2, 0.4), 0.3),
              ("Walk", 5, (-0.5, -0.4), 0.0), ("Walk", 20, (0.8, -0.6), -0.3), ("Idle", 20, (-1.6, -0.2), 0.4)]
    previews = dict(target=(0, 0.0, 0.3), distance=2.4, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Shatter", "close", range(0, SHATTER_FRAMES, 4)),
                            ("Death", "close", range(0, 41, 6))],
                    macros=[("ice", G(0.06, 0.55, 0.0), 0.5, -50, 30), ("head", G(0.0, 0.25, 0.45), 0.45, -25, 15),
                            ("socket", G(0.17, 0.42, -0.16), 0.4, -60, 50)],
                    rig_scale=1.2, power=("Shatter", 14))
    asset, arm, actions, lod = K3.run_creature(
        NAME, builder, bones(), CLIPS, tris=3950, families=("chitin", "crystal", "membrane"), game_scale=GAME_SCALE,
        rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS, scale_bones=SHARD_NAMES, clip_scales=CLIP_SCALES,
        extra=H.weathering(K, markings=PAL.dark, marking_amount=0.4, dust=0.3, seed=71), specular=0.35,
        report={"shard_bones_in_shed_order": SHARD_NAMES, "shatter_burst_frames": shatter_frames(),
                "walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                "spit_origin": "Throat, about (0, 0.17, 0.46) model units, from the Head bone"},
        previews=None if C.cli_args().get("norender") else previews)
    if not C.cli_args().get("norender"):
        out = C.preview_dir()
        chipped_render(arm, actions, out, set(SHARD_NAMES[:3]), "shardback_chipped.png")
        chipped_render(arm, actions, out, set(SHARD_NAMES), "shardback_bare.png")


if __name__ == "__main__":
    main()
