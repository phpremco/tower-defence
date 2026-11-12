"""Cragback: the Stone-element crawler, heavy, slow and very tough (round 4; redesigned in round 49B, see
ArtSource/DESIGN_NOTES.md).

Round 49B (Remco: "the spikes are not logical and neither is the single eye; maybe lightly smoking, the lava
glowing"): a walking lava field. The shell is a dome of cooled crust floes (on the plate bones Plate_L/Plate_R)
with the molten core glowing in every gap; the spikes are columns of basalt that grew where the lava cooled along
the open fissure down its spine (bone Plate_C); three squat vent cones on the rear smoke and glow (sockets
Smoke_1..3 at the crater mouths); a broad stone skull with a brow ram and two stub basalt horns, two pairs of eyes in
glowing sockets (the Face part), a dropped jaw over a molten throat; lava drips under the belly; six columnar legs.

Clips: Walk (heavy tripod with a body dip on every footfall), Idle, Hit, Death (Strike comes from
build_attacks36.py). The veins fade on death in Unity by animating the emission (see ArtSource/README.md). Also
exports Cragback_LOD1.

Run: blender --background --python Tools/Blender/build_cragback.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import stone as S  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = "Cragback"
LOD_NAME = "Cragback_LOD1"
K = 1.2
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("rock", "membrane", "chitin")
GAME_SCALE = 0.85

# Species colours from ProjectGenerator.Bestiary: grey-brown stone (0.4, 0.36, 0.32), amber eyes, orange accent and
# veins. Lighter on top and near-black in the faults so the floes read as separate slabs at game size; round 49B
# makes the stone a cooler basalt grey so the lava orange carries the colour.
PAL = H.Palette(top=(0.46, 0.42, 0.38), side=(0.2, 0.18, 0.16), belly=(0.06, 0.05, 0.045), seam=(0.04, 0.025, 0.02),
                dark=(0.1, 0.085, 0.075), glow=(1.0, 0.42, 0.08), eye=(1.0, 0.75, 0.3), bone=(0.66, 0.62, 0.58))
HOT = H.Palette(top=PAL.top, side=PAL.side, belly=PAL.belly, seam=PAL.seam, dark=PAL.dark, glow=(1.0, 0.55, 0.14),
                eye=PAL.eye, bone=PAL.bone)
WHITE_HOT = (1.0, 0.8, 0.45)
LEG = H.Palette(top=(0.44, 0.4, 0.36), side=(0.22, 0.2, 0.18), belly=(0.06, 0.05, 0.045), seam=(0.04, 0.03, 0.025),
                dark=(0.1, 0.085, 0.075), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = (("L", -1), ("R", 1))
PAIRS = {1: 0.22, 2: -0.03, 3: -0.27}          # leg pair: forward position of the hip
FEMUR, TIBIA = 0.2, 0.24
TRIPOD_A = {("L", 1), ("R", 2), ("L", 3)}

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def legs():
    out = {}
    for side, sx in SIDES:
        for i, z in PAIRS.items():
            names = [f"Leg_{side}{i}_Upper", f"Leg_{side}{i}_Lower"]
            out[(side, i)] = C.Limb(names, G(0.24 * sx, 0.25, z), G(0.47 * sx, 0.0, z * 1.15 + 0.02), (FEMUR, TIBIA),
                                    G(sx * 1.0, 0.9, 0.0))
    return out


def rot(pitch=0.0, roll=0.0, yaw=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(roll), 3, "Y")
            @ Matrix.Rotation(math.radians(pitch), 3, "X"))


def slab(b, name, centre, radii, r, bone, seed, count=20, depth=0.18, rim=-0.35, veins=0.6, fault=1.0, pal=PAL):
    b.ellipsoid(name, centre, radii, r, bone=bone, segments=(40, 20),
                deform=C.chiselled(count, seed, depth, squash=(0.55, 1.0)),
                style=S.slab(pal, K, seed=seed, rim=rim, veins=veins, fault=fault))


# Round 49B: the carapace is a dome of cooled lava crust. A floe sits at (phi, psi) on the dome ellipsoid: phi the
# angle from the midline down the flank, psi the angle forward (+) or back (-) along the body.
DOME_C = G(0, 0.33, -0.05)
DOME_R = (0.31, 0.47, 0.25)
# Floes (right side; mirrored): phi, psi, (half size across, half size along), seed. Two rows over the top and a
# skirt row low on the flank; they leave the midline open (the ridge fissure) and narrow gaps between each other,
# where the molten core shows.
FLOES = [(26, 43, (0.11, 0.14), 1), (26, 14, (0.115, 0.15), 2), (26, -15, (0.115, 0.15), 3), (25, -44, (0.105, 0.135), 4),
         (58, 33, (0.105, 0.14), 5), (58, 3, (0.11, 0.15), 6), (58, -27, (0.105, 0.14), 7),
         (86, 20, (0.09, 0.16), 8), (86, -14, (0.09, 0.16), 9), (80, -46, (0.085, 0.13), 10)]
# Basalt columns along the ridge fissure (x, psi, length, radius, lean back and tilt sideways in degrees): columnar
# jointing where the lava cooled along the crack; uneven heights, tallest over the shoulders, flat broken tops.
COLUMNS = [(0.0, 36, 0.13, 0.04, 14, 8), (0.04, 27, 0.21, 0.045, 20, -6), (-0.045, 25, 0.17, 0.042, 22, 10),
           (0.0, 15, 0.26, 0.05, 26, 4), (0.05, 5, 0.2, 0.046, 30, -12), (-0.05, 3, 0.23, 0.046, 30, 9),
           (0.01, -9, 0.17, 0.044, 36, -5), (0.045, -19, 0.11, 0.038, 42, 14), (-0.04, -21, 0.14, 0.04, 40, -9)]
# Vent cones (the volcano on its back; smoke and embers rise from the craters: bones Smoke_1..3).
VENT_CONES = [("Smoke_1", 0.0, -38, "Plate_C", 0.075, 0.075), ("Smoke_2", 44.0, -26, "Plate_R", 0.06, 0.06),
              ("Smoke_3", -44.0, -26, "Plate_L", 0.06, 0.06)]
SMOKE_BONES = [v[0] for v in VENT_CONES]
# Eyes (round 49B; the Face part puts the lenses here): a big front pair low under the brow, a small pair behind
# and above it, both in glowing sockets.
EYES = [((0.085, 0.335, 0.555), 0.033), ((0.125, 0.37, 0.505), 0.022)]


def dome(phi, psi, sx=1):
    """Point and outward normal on the carapace dome."""
    f, s = math.radians(phi), math.radians(psi)
    d = Vector((sx * math.sin(f) * math.cos(s), -math.sin(s), math.cos(f) * math.cos(s)))
    p = DOME_C + Vector((d.x * DOME_R[0], d.y * DOME_R[1], d.z * DOME_R[2]))
    n = Vector((d.x / DOME_R[0], d.y / DOME_R[1], d.z / DOME_R[2])).normalized()
    return p, n


def vent_top(phi, psi, height):
    sx = 1 if phi >= 0 else -1
    p, n = dome(abs(phi), psi, sx)
    n = (n + Vector((0, 0.3, 1.2))).normalized()
    return p, n, p + n * height


def surface_frame(n, ahead=Vector((0, -1, 0))):
    """Rotation with local +Z along `n` and local -Y towards `ahead` (projected): a plate lying on the surface."""
    z = n.normalized()
    y = -(ahead - z * z.dot(ahead)).normalized()
    x = y.cross(z).normalized()
    return Matrix((x, y, z)).transposed()


def column_profile(t):
    """A basalt column: straight hexagonal sides and a flat, slightly bevelled top (a broken column, not a needle)."""
    return 1.0 if t < 0.9 else max(0.55, 1.0 - (t - 0.9) / 0.1 * 0.45)


def basalt_column(pal, k, seed=0):
    """Columnar basalt grown from the molten fissure: glowing root, near-black body with cross joints, a cooled pale
    grey top; a thin hot line still runs in the joints near the base."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, pal.top * 0.9, pal.side, pal.belly)
        col = C.lerp3(col, pal.bone * 0.85, C.smoothstep(0.7, 1.0, t) * 0.6)
        joint = (t * 4 + 0.2 * C.value_noise(p, 10 / k, seed)) % 1.0
        seam = 1 - C.smoothstep(0.0, 0.07, np.minimum(joint, 1 - joint))
        root = 1 - C.smoothstep(0.0, 0.22, t)
        col = col * (1 + 0.14 * C.value_noise(p, 18 / k, seed + 1))[:, None]
        col = C.lerp3(col, pal.seam, seam * 0.8)
        col = C.lerp3(col, pal.glow * 0.6, root)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip(root + seam * 0.5 * (1 - C.smoothstep(0.2, 0.6, t)), 0, 1)[:, None]
        s.disp[idx] = -0.004 * k * seam + 0.002 * k * C.value_noise(p, 30 / k, seed + 2)
        s.rough[idx] = 0.84 - 0.2 * root
        H.family(s, idx, rock=1.0 - root, flow=t * C.axis_length(part))
    return style


def drip(pal, k):
    """A lava drip hanging under the belly: hot at the tip where it is still liquid, a dark cooled skin at the root."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        hot = C.smoothstep(0.3, 0.9, t)
        s.albedo[idx] = C.lerp3(np.tile(pal.dark, (len(idx), 1)), np.tile(pal.glow * 0.7, (len(idx), 1)), hot)
        s.emit[idx] = pal.glow[None, :] * (0.3 + 0.7 * hot)[:, None]
        s.rough[idx] = 0.5
        s.disp[idx] = 0.0
        H.family(s, idx, rock=1.0 - hot, flow=t * C.axis_length(part))
    return style


def build_parts():
    b = C.PartBuilder()
    k = K
    # The molten core under the crust (seen in every fault), and a molten underside that drips.
    b.ellipsoid("Core", G(0, 0.32, -0.05), (0.3, 0.46, 0.235), bone="Body", segments=(40, 20),
                style=S.core(PAL, k, seed=3, cells=56, width=0.022, glow=1.0))
    b.ellipsoid("Belly", G(0, 0.17, -0.03), (0.23, 0.36, 0.08), bone="Body", segments=(36, 16),
                style=K3.molten_sac(HOT, k, cells=22, seed=4, glow=0.9))
    # Crust floes on the dome, each tilted a little (broken, lifted edges), gaps of molten core between them.
    for phi, psi, (w, l), seed in FLOES:
        for side, sx in SIDES:
            p, n = dome(phi, psi, sx)
            tilt = rot(pitch=6 * math.sin(seed * 1.7), roll=-8 * sx * math.cos(seed * 2.3))
            thick = 0.036 if phi < 70 else 0.045
            b.ellipsoid("Floe", p + n * 0.006, (w, l, thick), surface_frame(n) @ tilt, bone=f"Plate_{side}",
                        segments=(32, 16), deform=C.chiselled(11, 200 + seed, 0.16, squash=(0.3, 1.0)),
                        style=S.slab(PAL, k, strata=34, veins=0.45, fault=1.0, seed=200 + seed, rim=-0.1))
    # The rump: a heavy cooled floe over the tail end; the shoulder floe that carries the head.
    slab(b, "Rump", G(0, 0.33, -0.44), (0.2, 0.12, 0.1), rot(pitch=50), "Body", 14, count=14, depth=0.22, rim=-0.1,
         veins=0.3)
    slab(b, "Shoulder", G(0, 0.45, 0.27), (0.21, 0.13, 0.08), rot(pitch=24), "Front", 13, count=14, depth=0.2,
         rim=-0.2, veins=0.5)
    # The ridge: basalt columns standing in the open fissure down the midline, a hot seam at their roots.
    b.ellipsoid("Fissure", G(0, 0.535, -0.06), (0.045, 0.36, 0.045), bone="Plate_C", segments=(24, 12),
                style=H.inner_glow(HOT, 0.95))
    for i, (x, psi, length, radius, lean, tilt) in enumerate(COLUMNS):
        p, n = dome(0, psi)
        base = p + Vector((x, 0, -0.03))
        up = Vector((math.sin(math.radians(tilt)) + x * 1.5, math.sin(math.radians(lean)),
                     math.cos(math.radians(lean)))).normalized()
        b.shard("Column", base, up, radius, length + 0.03, sides=6, profile=column_profile, rings=10,
                phase=0.3 * i, bone="Plate_C", chain=["Plate_C"], style=basalt_column(PAL, k, seed=300 + i))
    # Vent cones: squat volcanic cones with a ragged crater rim and a lava pool in each mouth.
    for name, phi, psi, parent, radius, height in VENT_CONES:
        p, n, top = vent_top(phi, psi, height)
        b.tube("Cone", [p - n * 0.04, p + n * height * 0.4, top], [radius * 1.9, radius * 1.15, radius * 0.8],
               sides=12, chain=[parent], style=S.pillar(PAL, k, drums=2, seed=40, glow=0.8), cap=0.0)
        b.ring("CraterRim", top, n, radius * 0.66, radius * 0.22, arcs=3, sides=8, chain=[parent],
               style=S.spike(PAL, k, glow=0.6, seed=41), profile=lambda th: 0.85 + 0.3 * math.sin(th * 5 + 1))
        b.ellipsoid("CraterLava", top + n * 0.004, (radius * 0.6, radius * 0.6, radius * 0.12), surface_frame(n),
                    bone=parent, segments=(16, 8), style=T.solid_glow(WHITE_HOT, 1.0))
    # Head: a broad stone skull pushed out in front of the shell, a heavy brow ram with two stub horns of basalt,
    # deep glowing sockets (the eyes are the Face part), a dropped jaw with a molten throat behind it.
    slab(b, "Skull", G(0, 0.3, 0.43), (0.16, 0.15, 0.12), rot(pitch=-8), "Front", 16, count=16, depth=0.18, rim=-0.5,
         veins=0.2, fault=0.4)
    # The brow ram: two heavy slabs meeting low over the snout in a V, so the face scowls instead of smiling.
    for side, sx in SIDES:
        slab(b, "Brow", G(0.08 * sx, 0.385, 0.48), (0.12, 0.13, 0.055), rot(pitch=-16, roll=-16 * sx, yaw=10 * sx),
             "Front", 15, count=14, depth=0.22, rim=-0.4, veins=0.3, fault=0.6)
    for side, sx in SIDES:
        slab(b, "Cheek", G(0.14 * sx, 0.3, 0.46), (0.06, 0.09, 0.07), rot(roll=30 * sx, yaw=-20 * sx), "Front", 17,
             count=10, depth=0.25, rim=-0.4, veins=0.0, fault=0.5)
        b.shard("Horn", G(0.1 * sx, 0.43, 0.49), G(0.45 * sx, 0.7, 0.8), 0.034, 0.17, sides=6, profile=column_profile,
                rings=8, bone="Front", chain=["Front"], style=basalt_column(PAL, k, seed=320))
        for (x, u, f), r in EYES:
            # A dark, deep socket with only a dull ember rim of light behind the lens.
            b.ellipsoid("Socket", G(x * sx, u, f - 0.016), (r * 0.95, r * 0.8, r * 0.7), bone="Front", segments=(16, 8),
                        style=H.inner_glow(HOT, 0.18))
    b.ellipsoid("Throat", G(0, 0.235, 0.45), (0.06, 0.05, 0.022), bone="Front", segments=(20, 10),
                style=H.inner_glow(HOT, 0.6))
    slab(b, "Jaw", G(0, 0.18, 0.45), (0.15, 0.13, 0.06), rot(pitch=8), "Jaw", 18, count=14, depth=0.2, rim=0.3,
         veins=0.0, fault=0.0)
    # Lava drips under the belly, still glowing at the tips.
    for x, f, length in ((0.12, 0.12, 0.06), (-0.12, 0.12, 0.06), (0.08, -0.15, 0.07), (-0.08, -0.15, 0.07),
                         (0.0, -0.3, 0.06)):
        root = G(x, 0.13, f)
        b.tube("Drip", [root, root + G(0, -length * 0.6, 0.0), root + G(0, -length, -0.006)], [0.024, 0.014, 0.008],
               sides=8, chain=["Body"], style=drip(HOT, k))

    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("Upper", [hip, hip.lerp(knee, 0.5), knee], [0.085, 0.078, 0.07], sides=6, chain=[lg.names[0]],
               style=S.pillar(LEG, k, drums=2, seed=30 + i, glow=0.8), cap=0.5)
        b.tube("Lower", [knee, knee.lerp(foot, 0.5), foot + Vector((0, 0, 0.02))], [0.07, 0.068, 0.09], sides=6,
               chain=[lg.names[1]], style=S.pillar(LEG, k, drums=3, seed=33 + i, glow=0.8), cap=0.3)
        slab(b, "Knee", knee + Vector((0, 0, 0.02)), (0.085, 0.08, 0.07), rot(roll=20 * sx), lg.names[0], 60 + i,
             count=12, depth=0.25, rim=-0.4, veins=0.0, fault=0.8, pal=LEG)
        for a in (-0.65, 0.0, 0.65):
            out = G(sx * math.sin(a + 0.3 * sx), 0, math.cos(a + 0.3 * sx)).normalized()
            toe = foot + Vector((0, 0, 0.018))
            b.tube("Toe", [toe, toe + out * 0.06 + Vector((0, 0, 0.004)), toe + out * 0.1 - Vector((0, 0, 0.012))],
                   [0.034, 0.024, 0.006], sides=6, chain=[lg.names[1]], style=S.spike(LEG, k, seed=50 + i))
    return b


def bones():
    out = [
        ("Root", G(0, 0, 0), G(0, 0, 0.2), None, True),
        ("Body", G(0, 0.32, -0.08), G(0, 0.34, 0.2), "Root", True),
        ("Front", G(0, 0.4, 0.12), G(0, 0.38, 0.46), "Body", True),
        ("Jaw", G(0, 0.21, 0.22), G(0, 0.15, 0.47), "Front", True),
        ("Plate_C", G(0, 0.5, 0.06), G(0, 0.54, -0.36), "Body", True),
    ]
    for side, sx in SIDES:
        out.append((f"Plate_{side}", G(0.17 * sx, 0.42, 0.28), G(0.2 * sx, 0.44, -0.32), "Body", True))
    # Round 49B: smoke sockets in the crater mouths (deform bones without weights, so the FBX keeps them; Unity parents
    # the smoke and ember emitters here; each bone points straight up out of its crater).
    for name, phi, psi, parent, radius, height in VENT_CONES:
        top = vent_top(phi, psi, height)[2]
        out.append((name, top, top + Vector((0, 0, 0.1)), parent, True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch=0.0, roll=0.0, yaw=0.0, front=0.0, front_yaw=0.0, jaw=0.0, plates=(0.0, 0.0, 0.0)):
    """plates: (left roll, right roll, crest pitch) in radians: the slabs grinding against each other."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": offset if offset is not None else Vector()},
        "Front": {"rot": [(X, front), (Z, front_yaw)]},
        "Jaw": {"rot": [(X, -jaw)]},
        "Plate_L": {"rot": [(Yv, -plates[0])]},
        "Plate_R": {"rot": [(Yv, plates[1])]},
        "Plate_C": {"rot": [(X, plates[2])]},
    }
    return specs


def place_legs(poser, specs, feet):
    pose = poser.solve(specs)
    body = pose["Body"] @ poser.rest["Body"].inverted()
    for key, lg in legs().items():
        specs.update(lg.pose(poser, body @ lg.root, feet[key]))
    return specs


STRIDE, LIFT, STANCE = 0.32, 0.08, 0.65
WALK_FRAMES = 32


def impact(t, lands, rise=0.05, fall=9.0):
    """0..1 jolt after each footfall time in `lands`: a quick drop, then a slow recovery."""
    out = 0.0
    for land in lands:
        s = (t - land) % 1.0
        out = max(out, s / rise if s < rise else math.exp(-(s - rise) * fall))
    return out


def walk_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        hit = impact(t, (0.0, 0.5))
        side = 1 if (t % 1.0) < 0.5 else -1  # which tripod carries the weight
        specs = body_specs(offset=Vector((0, 0, -0.035 * hit + 0.01)), pitch=math.radians(2.5) * hit,
                           roll=math.radians(2.5) * math.sin(w), yaw=math.radians(2) * math.sin(w),
                           front=math.radians(5) * hit - math.radians(2), front_yaw=math.radians(3) * math.sin(w + 0.5),
                           jaw=math.radians(6) * hit,
                           plates=(math.radians(4) * hit * (side > 0) + math.radians(1.5) * math.sin(w),
                                   math.radians(4) * hit * (side < 0) - math.radians(1.5) * math.sin(w),
                                   math.radians(3) * hit))
        feet = {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key in TRIPOD_A else 0.5)) % 1.0
            # Phase 0 = the foot lands at the front: shift so the swing ends exactly on the footfall.
            fwd, up = C.step_curve(phase, STANCE, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        return place_legs(poser, specs, feet)
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        breathe = 0.5 + 0.5 * math.sin(w)
        grind = max(0.0, math.sin(2 * w + 0.7)) ** 3
        specs = body_specs(offset=Vector((0, 0, 0.008 * breathe)), roll=math.radians(1.2) * math.sin(w),
                           front=math.radians(3) * math.sin(w + 1.0), front_yaw=math.radians(10) * math.sin(w),
                           jaw=math.radians(4) + math.radians(10) * grind,
                           plates=(math.radians(2.5) * breathe, math.radians(2.5) * breathe, math.radians(2) * breathe))
        return place_legs(poser, specs, {k: lg.foot for k, lg in legs().items()})
    return 72, spec, True


def hit_spec(poser):
    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch=-math.radians(5) * k, front=-math.radians(10) * k,
                           jaw=math.radians(18) * max(k, 0.0), plates=(math.radians(6) * k, math.radians(6) * k,
                                                                         math.radians(5) * k))
        return place_legs(poser, specs, {key: lg.foot for key, lg in legs().items()})
    return 14, spec, False


def death_spec(poser):
    def spec(t, f):
        buckle = C.smoothstep(0.05, 0.55, np.array([t]))[0]
        slump = C.smoothstep(0.35, 0.85, np.array([t]))[0]
        settle = math.sin(math.pi * C.smoothstep(0.5, 0.7, np.array([t]))[0]) * 0.012
        specs = body_specs(offset=Vector((0, 0, -0.17 * buckle + settle)), roll=math.radians(6) * slump,
                           pitch=math.radians(6) * buckle, front=math.radians(18) * slump, jaw=math.radians(22) * slump,
                           plates=(math.radians(14) * slump, math.radians(10) * slump, -math.radians(8) * slump))
        feet = {}
        for key, lg in legs().items():
            sx = -1 if key[0] == "L" else 1
            # The legs splay out sideways as the weight comes down.
            feet[key] = lg.foot + Vector((sx * 0.12, 0, 0)) * buckle
        return place_legs(poser, specs, feet)
    return 40, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec}
# LOD1: plates fold into the body, each leg is one bone from hip to foot. 10 bones.
LOD_DROP = {"Plate_L": "Body", "Plate_R": "Body", "Plate_C": "Body",
            **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in PAIRS}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in PAIRS}
# Footfalls (frame of the Walk clip) for dust puffs and a camera-free "thud" sound in Unity.
FOOTFALL_FRAMES = (0, WALK_FRAMES // 2)


def old_proxy():
    """EnemyModelFactory.Cragback in primitives, flat colours (for the compare sheet)."""
    body, dark, accent, eyes = (0.4, 0.36, 0.32), (0.16, 0.14, 0.13), (0.85, 0.42, 0.14), (1.0, 0.75, 0.3)
    groups = {c: C.PartBuilder() for c in (body, dark, accent, eyes)}
    groups[dark].ellipsoid("Base", G(0, 0.14, 0), (0.3, 0.36, 0.08))
    groups[body].ellipsoid("Dome", G(0, 0.3, -0.04), (0.34, 0.4, 0.25))
    for p in ((0, 0.53, -0.06), (0.2, 0.46, 0.1), (-0.21, 0.45, 0.06), (0.23, 0.43, -0.22), (-0.2, 0.44, -0.24),
              (0.02, 0.48, 0.22), (-0.03, 0.46, -0.33)):
        groups[dark].ellipsoid("Plate", G(*p), (0.13, 0.12, 0.05))
    groups[dark].ellipsoid("Brow", G(0, 0.31, 0.36), (0.23, 0.11, 0.06))
    groups[body].ellipsoid("Head", G(0, 0.2, 0.4), (0.14, 0.1, 0.09))
    for i in range(3):
        groups[eyes].ellipsoid("Eye", G(0, 0.25 - i * 0.045, 0.49), (0.02,) * 3)
    for tip, base in ((G(0.1, 0.78, -0.2), G(0.06, 0.55, -0.12)), (G(-0.15, 0.7, 0.02), G(-0.08, 0.52, -0.02))):
        groups[accent].tube("Spire", [base, tip], [0.04, 0.003])
    for sx in (-1, 1):
        for z in (0.22, 0.0, -0.22):
            groups[dark].tube("Leg", [G(0.22 * sx, 0.16, z), G(0.34 * sx, 0.1, z), G(0.33 * sx, 0.0, z + 0.03)], [0.05, 0.04, 0.02])
    return C.flat_proxy(groups, "OldCragback")


def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    target = Vector((0, -0.05, 0.3))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 2.4, azimuth=-38, elevation=26, lens=55)
    C.set_action(arm, actions["Idle"], 20)
    paths.append(C.render(close, os.path.join(out, "cragback_close.png"), 1280, 720))
    side = C.closeup_camera(target, 2.5, azimuth=-90, elevation=8, lens=55)
    C.set_action(arm, actions["Walk"], 0)
    paths.append(C.render(side, os.path.join(out, "cragback_side.png"), 1280, 720))
    front = C.closeup_camera(target, 1.9, azimuth=-14, elevation=12, lens=55)
    C.set_action(arm, actions["Hit"], 4)
    paths.append(C.render(front, os.path.join(out, "cragback_front.png"), 1280, 720))
    top = C.closeup_camera(target, 2.5, azimuth=-160, elevation=58, lens=55)
    C.set_action(arm, actions["Walk"], 8)
    paths.append(C.render(top, os.path.join(out, "cragback_top.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 40)
    paths.append(C.render(close, os.path.join(out, "cragback_dead.png"), 1280, 720))
    strips = [C.strip_sheet(arm, actions["Walk"], side, out, "cragback_walk", range(0, WALK_FRAMES, 4))]

    C.set_action(lod_arm, lod_actions["Walk"], 8)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(close, os.path.join(out, "cragback_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 20)
    macros = [("cragback_macro_back.png", G(0.0, 0.62, -0.12), 0.6, -50, 40),
              ("cragback_macro_maw.png", G(0.0, 0.3, 0.42), 0.5, -25, 8),
              ("cragback_macro_leg.png", G(-0.42, 0.12, 0.25), 0.45, -70, 12)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))

    walk = actions["Walk"]
    layout = [(walk, 0, (-1.2, 0.5, 0), 0.2), (walk, 10, (0.2, 0.7, 0), -0.1), (walk, 20, (1.3, 0.3, 0), 0.3),
              (walk, 5, (-0.5, -0.6, 0), -0.3), (walk, 27, (0.8, -0.6, 0), 0.0)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "cragback_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "cragback_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "cragback_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "cragback_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Idle"], 20)
    single = C.game_view(arm, mesh, os.path.join(out, "cragback_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5,
                         close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], single], os.path.join(out, "compare4_cragback.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_cragback.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "cragback_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.004, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.1,
                               extrusion=0.016, ray=0.05, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.3, dust=0.7, seed=61), specular=0.25,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "walk_distance_per_loop": round(STRIDE / STANCE, 3), "footfall_frames": list(FOOTFALL_FRAMES),
                          "death_emission_fade": {"from_frame": 8, "to_frame": 40, "curve": "ease-out to 0"},
                          "r49_smoke_sockets": {"bones": SMOKE_BONES, "parents": {v[0]: v[3] for v in VENT_CONES},
                                                "note": "crater mouths of the vent cones; bone +Y points up out of the "
                                                        "crater; no weights; kept in LOD1 (parented to Body there)"},
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)
    # Round 46: the close-up detail (eyes under stone lids, fangs, talons) as extra parts, exported again with the body.
    import cragback_detail
    cragback_detail.run(norender=bool(C.cli_args().get("norender")))


# Slabs and the jaw are stone: they move as one piece and meet their neighbours in a narrow soft crease.
RIGID = {"Floe", "Shoulder", "Rump", "Skull", "Brow", "Cheek", "Horn", "Jaw", "Column", "Cone", "CraterRim",
         "CraterLava", "Drip", "Knee"}
QUICK_TARGET = (0, 0, 0.3)

if __name__ == "__main__":
    main()
