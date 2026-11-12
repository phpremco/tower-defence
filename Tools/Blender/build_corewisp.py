"""CoreWisp: the Wisp boss (air and ground: pulses of damage, splits into wisps as it dies; round 6, see
ArtSource/DESIGN_NOTES.md).

The Wisp's bud grown into a larger, more dangerous energy organism: six short chitin petals around a big heart that
shows at the crown and through a wide glowing slit at the front (the facing cue), two hooked brow horns over that
slit; three rings in their own planes (an inner ring of vertebra beads, a middle band with hooks, an outer crown ring
with four glowing nodes and spikes) and five tendrils that stream backward in flight like a comet's tail and plant
around it as five legs when it lands. The heart and the three rings have bone scale keys: they swell and flash
outward in the Pulse clip.

The palette is one parameter (PALETTES at the top): `--palette cool` (default, the Wisp's colours) or
`--palette warm` (Remco's open question; a warm variant bakes into the preview folder, never into Exports).

The armature origin is on the ground. Clips: Float (loop), Land, Grounded (loop), Hit, Death, Pulse (the power;
the shock leaves at PULSE_FRAME). Also exports CoreWisp_LOD1.

Run: blender --background --python Tools/Blender/build_corewisp.py -- [--preview <dir>] [--palette warm] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

# One switch for the family's open colour question: every colour of the model comes from this table.
PALETTES = {
    # The Wisp's palette: dark umber-violet flesh, ice-blue light, pale violet rings.
    "cool": dict(flesh=dict(top=(0.36, 0.24, 0.3), side=(0.17, 0.1, 0.14), belly=(0.06, 0.04, 0.05), seam=(0.03, 0.02, 0.025),
                            dark=(0.1, 0.06, 0.08), glow=(0.5, 0.74, 1.0), eye=(0.9, 0.97, 1.0), bone=(0.82, 0.76, 0.74)),
                 ring=dict(top=(0.32, 0.3, 0.38), side=(0.14, 0.13, 0.18), belly=(0.05, 0.05, 0.07), seam=(0.02, 0.02, 0.03),
                           dark=(0.08, 0.08, 0.1), glow=(0.62, 0.8, 1.0), eye=(0.9, 0.97, 1.0), bone=(0.82, 0.76, 0.74))),
    # The warm alternative: umber flesh, an amber heart and light, lime-green ring nodes.
    "warm": dict(flesh=dict(top=(0.36, 0.22, 0.14), side=(0.17, 0.09, 0.05), belly=(0.06, 0.035, 0.02), seam=(0.03, 0.018, 0.01),
                            dark=(0.1, 0.055, 0.03), glow=(1.0, 0.62, 0.16), eye=(1.0, 0.92, 0.6), bone=(0.86, 0.76, 0.62)),
                 ring=dict(top=(0.3, 0.27, 0.16), side=(0.13, 0.12, 0.06), belly=(0.05, 0.05, 0.03), seam=(0.02, 0.02, 0.012),
                           dark=(0.08, 0.075, 0.04), glow=(0.6, 1.0, 0.3), eye=(1.0, 0.92, 0.6), bone=(0.86, 0.76, 0.62))),
}
PALETTE_NAME, PALETTE = B.palette_variant(PALETTES, "cool")

NAME = "CoreWisp"
LOD_NAME = "CoreWisp_LOD1"
# Built in Wisp units times M: at Unity scale 1.3 x 0.6 it is about 2.9 times a Wisp (as the factory boss).
M = 1.86
K = 0.8 * M
DETAIL_K = 1.2
GAME_TRIS = 9000
LOD_TRIS = 4000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 1.3 * 0.6
WISP_SCALE = 0.5

PAL = H.Palette(**PALETTE["flesh"])
RING = H.Palette(**PALETTE["ring"])
# The exposed heart is lit flesh: its membrane and the gaps between the cells carry the glow colour, darker.
HEART = H.Palette(**{**PALETTE["flesh"], "membrane": tuple(0.55 * c for c in PALETTE["flesh"]["glow"]),
                     "belly": tuple(0.22 * c for c in PALETTE["flesh"]["glow"])})

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def P(x, up, fwd):
    return G(x * M, up * M, fwd * M)


def R(*r):
    return tuple(v * M for v in r) if len(r) > 1 else r[0] * M


FLOAT_UP, GROUND_UP = 0.45, 0.27
CENTRE = P(0, FLOAT_UP, 0)
BUD_R = 0.165
HEART_R = 0.15
# Six petals, mirror pairs; the wide gap between the front pair is the glowing slit that faces forward.
PETALS = [math.radians(a) for a in (38, 95, 150, 210, 265, 322)]
PETAL_HW = math.radians(25)
PETAL_ELEV = (-1.15, 0.95)
RINGS = {
    "A": dict(radius=0.27, tube=0.019, tilt=20),     # tilted forward
    "B": dict(radius=0.35, tube=0.017, tilt=62),     # nearly upright, facing forward
    "C": dict(radius=0.43, tube=0.02, tilt=-70),     # nearly upright, leaning back: an X with B from the side
}
for _r in RINGS.values():
    _r["normal"] = G(0, math.cos(math.radians(_r["tilt"])), math.sin(math.radians(_r["tilt"])))
# Five tendrils on the rear half: they trail behind like a comet's tail and plant as five legs.
TENDRILS = [math.radians(a) for a in (70, 125, 180, 235, 290)]


def flat_dir(az):
    return G(math.sin(az), 0, math.cos(az))


def bud_point(az, elev, r, centre=None):
    c = centre if centre is not None else CENTRE
    return c + flat_dir(az) * (R(r) * math.cos(elev)) + Z * (R(r) * math.sin(elev))


def petal_taper(elev):
    v = (np.asarray(elev) - PETAL_ELEV[0]) / (PETAL_ELEV[1] - PETAL_ELEV[0])
    return 1 - 0.45 * C.smoothstep(0.65, 1.0, v)


def petal_surface(az0, r):
    def fn(u, v):
        elev = PETAL_ELEV[0] + (PETAL_ELEV[1] - PETAL_ELEV[0]) * v
        hw = PETAL_HW * float(petal_taper(elev))
        return bud_point(az0 + hw * (2 * u - 1), elev, r * (1 + 0.07 * math.sin(math.pi * v)))
    return fn


def tendril_limbs():
    out = []
    for i, az in enumerate(TENDRILS):
        d = flat_dir(az)
        root = bud_point(az, -1.0, BUD_R * 0.85)
        # Rest pose: hanging down and back (the comet tail in flight).
        foot = root + d * R(0.05) + G(0, 0, -1) * R(0.3) - Z * R(0.18)
        names = [f"Tendril_{i + 1}_1", f"Tendril_{i + 1}_2"]
        out.append(C.Limb(names, root, foot, (R(0.21), R(0.21)), d * 1.0 + Z * 0.3))
    return out


def plant_point(i):
    return flat_dir(TENDRILS[i]) * R(0.3)


def ring_frame(normal):
    n = normal.normalized()
    a = (Vector((1, 0, 0)) - n * n.dot(Vector((1, 0, 0)))).normalized()
    return n, a, n.cross(a)


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Heart", CENTRE, R(HEART_R, HEART_R, HEART_R * 1.05), bone="Heart", segments=(40, 20),
                style=H.membrane_sac(HEART, k, plate_line=2.0, cells=64, cell_size=0.13, glow=1.0, seed=19, plate_glow=0.0))
    for i, az in enumerate(PETALS):
        b.slab("Petal", petal_surface(az, BUD_R), petal_surface(az, BUD_R * 0.82), 10, 18, chain=[f"Petal_{i + 1}"],
               style=H.bud_petal(PAL, k, az, PETAL_HW, glow=0.55, taper=petal_taper))
        b.parts[-1].frame = (C.Matrix.Translation(CENTRE) @ C.Matrix.Diagonal((R(BUD_R), R(BUD_R), R(BUD_R), 1.0))).inverted()
        b.parts[-1].radii = (R(BUD_R),) * 3
        tip = bud_point(az, PETAL_ELEV[1] - 0.08, BUD_R * 1.02)
        out = flat_dir(az)
        front = i in (0, 5)
        # The front pair's hooks become brow horns curling forward over the slit.
        if front:
            fwd = G(0, 0, 1)
            pts = [tip, tip + out * R(0.025) + fwd * R(0.06) + Z * R(0.07), tip + fwd * R(0.19) + Z * R(0.06) - out * R(0.015)]
            radii = [R(0.032), R(0.02), R(0.0015)]
        else:
            pts = [tip, tip + out * R(0.03) + Z * R(0.035), tip + out * R(0.075) + Z * R(0.03)]
            radii = [R(0.018), R(0.01), R(0.001)]
        b.tube("PetalHook", pts, radii, sides=8, chain=[f"Petal_{i + 1}"], style=H.hook(PAL, k, glow=0.8, glow_from=0.7))

    ref = Vector((1, 0, 0))
    ra, rb, rc = RINGS["A"], RINGS["B"], RINGS["C"]
    b.ring("RingA", CENTRE, ra["normal"], R(ra["radius"]), R(ra["tube"]), arcs=3, sides=8, chain=["Ring_A"],
           profile=lambda th: 0.75 + 0.45 * abs(math.sin(7 * th)), ref=ref,
           style=H.ring_band(RING, k, CENTRE, ra["normal"], beads=14, joint_glow=1.0, inner_glow=0.55, ref=ref))
    b.ring("RingB", CENTRE, rb["normal"], R(rb["radius"]), R(rb["tube"]), arcs=3, sides=8, chain=["Ring_B"], ref=ref,
           style=H.ring_band(RING, k, CENTRE, rb["normal"], nodes=2, node_glow=1.0, inner_glow=0.7, ref=ref))
    b.ring("RingC", CENTRE, rc["normal"], R(rc["radius"]), R(rc["tube"]), arcs=4, sides=8, chain=["Ring_C"], ref=ref,
           profile=lambda th: 1.0 + 0.7 * max(0.0, math.cos(4 * th)) ** 8,
           style=H.ring_band(RING, k, CENTRE, rc["normal"], nodes=4, node_glow=1.0, inner_glow=0.6, ref=ref, phase=math.pi / 4))
    n, a, bb = ring_frame(rb["normal"])
    for j in range(6):
        th = math.radians(30 + 60 * j)
        radial = a * math.cos(th) + bb * math.sin(th)
        tangent = n.cross(radial)
        base = CENTRE + radial * R(rb["radius"] + 0.005)
        b.tube("Hook", [base, base + radial * R(0.04) + tangent * R(0.01), base + radial * R(0.07) + tangent * R(0.035)],
               [R(0.016), R(0.009), R(0.001)], sides=7, chain=["Ring_B"], style=H.hook(RING, k, glow=0.9, glow_from=0.7))
    n, a, bb = ring_frame(rc["normal"])
    for j in range(8):
        th = math.radians(22.5 + 45 * j)
        radial = a * math.cos(th) + bb * math.sin(th)
        base = CENTRE + radial * R(rc["radius"] + 0.006)
        b.tube("Spike", [base, base + radial * R(0.05) + n * R(0.012), base + radial * R(0.1) + n * R(0.02)],
               [R(0.017), R(0.01), R(0.001)], sides=7, chain=["Ring_C"], style=H.hook(RING, k, glow=1.0, glow_from=0.6))
    for limb in tendril_limbs():
        root, knee, foot = limb.rest_points
        b.tube("Tendril", [root, root.lerp(knee, 0.5), knee], [R(0.032), R(0.026), R(0.021)], sides=8, chain=[limb.names[0]],
               style=H.tube(PAL, k, PAL.top, PAL.side, rings=(0.25, 0.5, 0.75), rough=0.55))
        b.tube("Tendril", [knee, knee.lerp(foot, 0.5), foot], [R(0.021), R(0.016), R(0.008)], sides=8, chain=[limb.names[1]],
               style=H.tube(PAL, k, PAL.side, PAL.dark, rings=(0.3, 0.6), tip_glow=1.0, glow_from=0.55, rough=0.55))
        d = (foot - knee).normalized()
        out = (foot - Vector((CENTRE.x, CENTRE.y, foot.z))).normalized()
        b.tube("Barb", [foot, foot + d * R(0.02) + out * R(0.02), foot + out * R(0.045) - Z * R(0.005)],
               [R(0.011), R(0.006), R(0.001)], sides=6, chain=[limb.names[1]], style=H.hook(PAL, k, glow=1.0, glow_from=0.3))
    return b


def petal_bone(i):
    az = PETALS[i]
    return bud_point(az, -0.8, BUD_R * 0.9), bud_point(az, 0.8, BUD_R * 0.9)


def bones():
    out = [
        ("Root", P(0, 0, 0), P(0, 0, 0.2), None, True),
        ("Core", P(0, FLOAT_UP - 0.1, 0), P(0, FLOAT_UP + 0.1, 0), "Root", True),
        ("Heart", CENTRE, CENTRE + Z * R(0.1), "Core", True),
    ]
    for i in range(len(PETALS)):
        head, tail = petal_bone(i)
        out.append((f"Petal_{i + 1}", head, tail, "Core", True))
    for key, ring in RINGS.items():
        out.append((f"Ring_{key}", CENTRE, CENTRE + ring["normal"] * R(0.15), "Core", True))
    for limb in tendril_limbs():
        out += limb.rest_bones("Core")
    return out


SCALE_BONES = ("Heart", "Ring_A", "Ring_B", "Ring_C")


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def core_specs(lift=0.0, sway=(0.0, 0.0), yaw=0.0, petals=0.0, petal_wave=0.0, w=0.0, heart=1.0, rings=None,
               ring_scale=1.0):
    """lift: bud height change (model units); petals: how far the petals open (rad); heart: its scale;
    rings: {key: (spin, tilt about X, armature-space drop or None)}; ring_scale: all three rings' scale."""
    specs = {
        "Root": {"rot": []},
        "Core": {"rot": [(Z, yaw), (X, sway[0]), (Yv, sway[1])], "loc": Vector((0, 0, lift))},
        "Heart": {"rot": [], "scale": heart},
    }
    for i, az in enumerate(PETALS):
        axis = Z.cross(flat_dir(az)).normalized()
        specs[f"Petal_{i + 1}"] = {"rot": [(axis, petals + petal_wave * math.sin(w + i * 1.7))]}
    for key, ring in RINGS.items():
        spin, tilt, drop = (rings or {}).get(key, (0.0, 0.0, None))
        spec = {"rot": [(X, tilt), (ring["normal"], spin)], "scale": ring_scale}
        if drop is not None:
            spec["loc"] = drop
        specs[f"Ring_{key}"] = spec
    return specs


def place_tendrils(poser, specs, feet):
    pose = poser.solve(specs)
    move = pose["Core"] @ poser.rest["Core"].inverted()
    for limb, foot in zip(tendril_limbs(), feet):
        specs.update(limb.pose(poser, move @ limb.root, foot, move.to_3x3() @ limb.pole))
    return specs


def trailing_feet(poser, specs, w, amount=0.035):
    """The comet tail: tendrils hang back from the moving bud and ripple, each out of step."""
    pose = poser.solve(specs)
    move = pose["Core"] @ poser.rest["Core"].inverted()
    feet = []
    for i, limb in enumerate(tendril_limbs()):
        d = flat_dir(TENDRILS[i])
        feet.append(move @ limb.foot + d * (R(amount) * math.sin(w + i * 1.9)) + Z * (R(0.025) * math.sin(2 * w + i)))
    return feet


def spins(t, turns=(1, -1, 1), tilts=(0.0, 0.0, 0.0), drops=(None, None, None)):
    return {key: (2 * math.pi * turns[i] * t, tilts[i], drops[i]) for i, key in enumerate(RINGS)}


# Grounded, the rings lie down around the bud: tilt each ring's axis up to vertical.
FLAT = {key: -math.radians(ring["tilt"]) for key, ring in RINGS.items()}


def s_(a, b, t):
    return C.smoothstep(a, b, np.array([t]))[0]


FLOAT_FRAMES = 72


def float_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Leaning into its path: the bud tips forward a little, the tail streams behind.
        specs = core_specs(lift=R(0.03) * math.sin(w), sway=(d(6) + d(3) * math.sin(w + 0.4), d(3) * math.cos(w)),
                           petals=d(6) + d(3) * math.sin(2 * w), petal_wave=d(3), w=2 * w, heart=1.0 + 0.04 * math.sin(4 * w),
                           rings=spins(t, (1, -1, 1), (d(6) * math.sin(w), d(8) * math.sin(w + 1.3), d(5) * math.sin(w + 2.4))))
        return place_tendrils(poser, specs, trailing_feet(poser, specs, w))
    return FLOAT_FRAMES, spec, True


LAND_FRAMES = 30


def land_spec(poser):
    d = math.radians

    def spec(t, f):
        down = s_(0.15, 0.75, t)
        squash = math.sin(math.pi * s_(0.6, 0.95, t)) * R(0.025)
        reach = s_(0.0, 0.6, t)
        flat = s_(0.2, 0.9, t)
        specs = core_specs(lift=-R(FLOAT_UP - GROUND_UP) * down - squash, petals=d(6) - d(6) * reach + d(12) * s_(0.75, 1.0, t),
                           heart=1.0 + 0.1 * math.sin(math.pi * s_(0.6, 1.0, t)),
                           rings=spins(s_(0.0, 1.0, t), (1, -1, 1), tuple(FLAT[k] * flat for k in RINGS)))
        hang = trailing_feet(poser, specs, 0.0, 0.0)
        return place_tendrils(poser, specs, [h.lerp(plant_point(i), reach) for i, h in enumerate(hang)])
    return LAND_FRAMES, spec, False


def grounded_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        specs = core_specs(lift=-R(FLOAT_UP - GROUND_UP) + R(0.012) * math.sin(2 * w), sway=(d(3) * math.sin(w), d(2) * math.cos(w)),
                           yaw=d(5) * math.sin(w), petals=d(12) + d(4) * math.sin(2 * w), petal_wave=d(2), w=2 * w,
                           heart=1.0 + 0.05 * math.sin(4 * w),
                           rings=spins(t, (1, -1, 1), tuple(FLAT[k] + d(3) * math.sin(w + i) for i, k in enumerate(RINGS))))
        return place_tendrils(poser, specs, [plant_point(i) for i in range(len(TENDRILS))])
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        specs = core_specs(lift=R(0.03) * k, sway=(-d(12) * k, d(7) * k), petals=d(6) + d(26) * max(k, 0.0),
                           heart=1.0 + 0.08 * max(k, 0.0),
                           rings={"A": (d(25) * k, d(14) * k, None), "B": (-d(30) * k, -d(12) * k, None), "C": (d(20) * k, d(10) * k, None)})
        return place_tendrils(poser, specs, trailing_feet(poser, specs, k * 3, 0.05))
    return 14, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        swell = math.sin(math.pi * s_(0.0, 0.35, t))
        burst = s_(0.25, 0.45, t)
        fall = s_(0.35, 0.9, t)
        specs = core_specs(lift=R(0.04) * swell - R(0.32) * fall, sway=(d(25) * fall, -d(15) * fall),
                           petals=d(6) + d(14) * swell + d(85) * burst, heart=1.0 + 0.3 * swell - 0.45 * burst,
                           rings={"A": (d(160) * s_(0.0, 0.6, t), d(55) * fall, Vector((R(0.02), 0, -R(0.33) * fall))),
                                  "B": (-d(200) * s_(0.0, 0.6, t), -d(40) * fall, Vector((-R(0.03), R(0.02), -R(0.29) * fall))),
                                  "C": (d(120) * s_(0.0, 0.6, t), d(30) * fall, Vector((0, -R(0.03), -R(0.36) * fall)))},
                           ring_scale=1.0 + 0.12 * swell)
        pose = poser.solve(specs)
        move = pose["Core"] @ poser.rest["Core"].inverted()
        feet = [(move @ limb.foot).lerp(plant_point(i) * 1.5, fall) for i, limb in enumerate(tendril_limbs())]
        return place_tendrils(poser, specs, feet)
    return 36, spec, False


PULSE_FRAMES = 40
# The heart is at its largest and the rings fly outward: start the CorePulse ring VFX and the damage here.
PULSE_FRAME = 14


def pulse_spec(poser):
    d = math.radians

    def spec(t, f):
        charge = s_(0.0, 0.3, t) * (1 - s_(0.3, 0.36, t))
        burst = s_(0.3, 0.36, t) * (1 - s_(0.45, 0.95, t))
        spin = s_(0.0, 0.3, t) * 0.25 + s_(0.3, 0.6, t) * 1.5
        shake = math.sin(t * 2 * math.pi * 14) * charge
        # Charge: it closes up and shivers; burst: the petals snap open, the heart blazes, the rings fly outward.
        specs = core_specs(lift=R(0.04) * burst - R(0.02) * charge, sway=(d(2) * shake, d(2) * shake),
                           petals=d(6) - d(8) * charge + d(55) * burst, petal_wave=d(4) * charge, w=t * 60,
                           heart=1.0 - 0.1 * charge + 0.4 * burst,
                           rings=spins(spin, (1, -1, 1)), ring_scale=1.0 - 0.06 * charge + 0.35 * burst)
        pose = poser.solve(specs)
        move = pose["Core"] @ poser.rest["Core"].inverted()
        feet = []
        for i, limb in enumerate(tendril_limbs()):
            # The tendrils whip out sideways with the shock.
            flare = flat_dir(TENDRILS[i]) * R(0.12) * burst + Z * R(0.08) * burst
            feet.append(move @ limb.foot + flare)
        return place_tendrils(poser, specs, feet)
    return PULSE_FRAMES, spec, False


CLIPS = {"Float": float_spec, "Land": land_spec, "Grounded": grounded_spec, "Hit": hit_spec, "Death": death_spec,
         "Pulse": pulse_spec}
# LOD1: petals fold into the core, each tendril one bone aimed from root to tip; heart and rings keep their bones
# (the Pulse needs them). 10 bones.
LOD_DROP = {**{f"Petal_{i}": "Core" for i in range(1, 7)}, **{f"Tendril_{i}_2": f"Tendril_{i}_1" for i in range(1, 6)}}
LOD_AIMS = {f"Tendril_{i}_1": (f"Tendril_{i}_1", f"Tendril_{i}_2") for i in range(1, 6)}
RIGID = {"Petal", "PetalHook", "RingA", "RingB", "RingC", "Hook", "Spike"}
TARGET = (0.0, 0.0, FLOAT_UP * M * 0.85)


def main():
    C.reset_scene()
    out = C.preview_dir()
    variant = PALETTE_NAME != "cool"
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0042, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.1,
                               extrusion=0.018, ray=0.05, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.4, dust=0.2, seed=93), specular=0.35,
                               mirror=True, k=DETAIL_K, families=FAMILIES, low_size=None if variant else LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm, SCALE_BONES)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Float"]
    common = dict(target=TARGET, distance=3.6, game_scale=GAME_SCALE, rig_scale=1.7, idle="Float")
    if variant:
        if not C.cli_args().get("norender"):
            B.variant_previews("corewisp", PALETTE_NAME, arm, asset.low, actions, out, move=("Float", 10), **common)
        return
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS,
                      SCALE_BONES)
    C.write_report(NAME, {"asset": NAME, "base_species": "Wisp", "palette": PALETTE_NAME, "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
                          "low_texture_size": LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "scale_keyed_bones": list(SCALE_BONES),
                          "pulse_frame": PULSE_FRAME, "pivot": "ground", "float_height": round(FLOAT_UP * M, 3),
                          "grounded_height": round(GROUND_UP * M, 3), "unity_scale": GAME_SCALE,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        return
    B.boss_previews(
        "corewisp", arm, asset.low, actions, lod, out, asset,
        base=dict(asset="Wisp", scale=WISP_SCALE, clip="Float", frame=10), move=("Float", 10), power=("Pulse", PULSE_FRAME),
        strips=[("Float", "side", range(0, FLOAT_FRAMES, 9)), ("Pulse", "close", range(0, PULSE_FRAMES, 5)),
                ("Land", "side", range(0, LAND_FRAMES + 1, 4)), ("Death", "close", range(0, 37, 5))],
        macros=[("bud", CENTRE + P(0, 0.04, 0.1), 1.2, -25, 20), ("ring", CENTRE + P(-0.35, 0.1, 0.05), 1.0, -60, 20)],
        minions=[(-1.5, 0.8, 0.2, 0), (-0.9, -0.7, 0.1, 15), (1.3, 0.7, 0.3, 33), (1.6, -0.5, 0.0, 48)],
        extra_shots=[("grounded", "close", "Grounded", 15), ("top_front", "top_front", "Float", 30)], **common)


if __name__ == "__main__":
    main()
    # Round 45: the core eye, slit barbs and tendril grapples (corewisp_detail.py) on the saved .blend, exported
    # again with the body.
    import corewisp_detail as CWD
    CWD.main()
