"""Round 29: boss set pieces Unity asked for in MODEL_TODO "Ronde 4a/4b/4c Unity" (Docs/BOSSES.md).

- `RuinGate` (Exports/RuinGate/): the sealed ruin gate The Shattered breaks out of (BOSSES.md "Entrance: a sealed gate
  in the decor opens for the first and only time"). A pointed arch of pale granite voussoirs with charged glyph
  grooves, two leaning buttresses with overcharge crystals, a crown of three crystal horns, and an iris of five
  black-glass petals with spiral seams locked by a crystal seal knot. Clips Closed_Idle, Open, Opened_Idle.
- `SealCrystal` (Exports/SealCrystal/): three sealed crystals for The Core's decor round the rift (A spire in a
  collar, B cluster in a broken socket, C held in a stone cradle). Their emission map is greyscale: Unity sets the
  immune type's colour through `_EmissionColor` (BOSSES.md "The sealed crystals turn the colour of the immune type").
- `Gate_IceBlock` (Exports/Gate_IceBlock/): the Frost Warden's frozen gate (Freeze_Gate), a chunk of grown ice that
  fits over every gate tier (G1-G3) with frost spikes; albedo alpha carries the opacity (clear ice, white cracks).
- `Cutscene_Drill_Wreck` (Exports/Cutscene_Drill_Wreck/): the colony drill rig after Gravemaw surfaced under it.
  Since round 31 built by build_drill_collapse.py (rigged, bind pose = the wreck, clip Collapse).
- Drained gate emission (Exports/Gate/, Exports/Tower_GateGun/): `*_Drained_Emission.png` (+ Low): the gate and
  gate gun glow dying to a faint violet residue (the Sealed One's GateDrainer).

Run: blender --background --factory-startup --python Tools/Blender/build_bosses29_props.py --
     [--preview <dir>] [--norender] [--only RuinGate,SealCrystal,IceBlock,DrillWreck,Drained] [--old-gate]
(Round 53: RuinGate comes from build_ruingate53.py; --old-gate builds the round-29 gate below.)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402
import ice_kit as IK  # noqa: E402
from boss_mech_kit import ease  # noqa: E402
from env_kit import ss  # noqa: E402

R = math.radians
ROUND = 29
GATE, SEAL, ICE, WRECK = "RuinGate", "SealCrystal", "Gate_IceBlock", "Cutscene_Drill_Wreck"

# Southern Crown / Shattered palette (round 17 build_env_southerncrown, round 28 build_shattered2).
CHARGE = np.array((1.0, 0.3, 0.8))
GRANITE = K.pal((0.5, 0.52, 0.56), (0.3, 0.32, 0.36), (0.07, 0.08, 0.1), (0.06, 0.06, 0.08))
BLACKGLASS = K.pal((0.13, 0.12, 0.17), (0.07, 0.065, 0.1), (0.02, 0.02, 0.035), (0.02, 0.02, 0.03))
Y.PALETTES.setdefault("overcharge_w", ((np.array((0.3, 0.04, 0.3)), np.array((1.0, 0.5, 0.86)), np.array((1.0, 0.97, 1.0))),
                                       (np.array((1.0, 0.3, 0.8)), np.array((1.0, 0.92, 0.98)))))
# Sealed crystals: neutral albedo and a white glow, so the emission map is a mask Unity tints per immune type.
Y.PALETTES.setdefault("seal_white", ((np.array((0.13, 0.12, 0.17)), np.array((0.58, 0.56, 0.64)), np.array((0.9, 0.9, 0.94))),
                                     (np.array((0.85, 0.85, 0.85)), np.array((1.0, 1.0, 1.0)))))
SEAL_STONE = K.pal((0.34, 0.31, 0.42), (0.17, 0.15, 0.23), (0.04, 0.03, 0.06), (0.03, 0.02, 0.05))
PACK = K.pal((0.78, 0.9, 1.0), (0.52, 0.74, 0.95), (0.26, 0.42, 0.62), (0.3, 0.45, 0.65))
COLD = np.array((0.5, 0.8, 1.0))
VIOLET = np.array((0.62, 0.22, 1.0))


# =============================================================================================================
# RuinGate
# =============================================================================================================

HALF_W, SPRING, CAP = 2.0, 2.0, 2.3       # opening: jambs +-2.0 up to 2.0, a semi-elliptic head 2.3 high (apex 4.3)
RING_W = 0.95                              # voussoir width
OZ = 1.9                                   # centre of the iris
DEPTH = 0.55                               # half depth of the ring stones
PETALS = 5
TWIST = 0.55                               # spiral of the petal seams (rad from centre to rim)
PETAL_R0, PETAL_R1 = 0.17, 0.982           # petal radial span (fraction of the opening radius)
FLOOR_CLAMP = 0.14
SEAL_C = Vector((0, -0.32, OZ))
OPEN_FRAMES, SEAL_CRACK, FIRST_LIGHT, SWING_START, FULLY_OPEN = 160, 30, 38, 52, 130
IDLE_FRAMES = 72
OPEN_ANGLE, FLOOR_ANGLE = R(98), R(88)


def inside(x, z, grow=0.0):
    a, c = HALF_W + grow, CAP + grow
    if z < -0.1 - grow:
        return False
    if z <= SPRING:
        return abs(x) <= a
    return (x / a) ** 2 + ((z - SPRING) / c) ** 2 <= 1.0


def ray_radius(phi, grow=0.0, floor=True):
    """Distance from the iris centre along phi to the opening outline (inflated by `grow`); the floor (z = 0)
    closes the opening when `floor`."""
    d = Vector((math.cos(phi), 0, math.sin(phi)))
    lo, hi = 0.0, 8.0
    for _ in range(40):
        m = (lo + hi) / 2
        x, z = d.x * m, OZ + d.z * m
        ok = inside(x, z, grow) and (z >= 0.0 or not floor)
        lo, hi = (m, hi) if ok else (lo, m)
    return lo


_TABLE = {}


def outline_table(phi):
    """ray_radius(phi, floor=False) for numpy arrays, from a cached table (styles run per vertex)."""
    if "t" not in _TABLE:
        grid = np.linspace(-math.pi, math.pi, 1441)
        _TABLE["g"] = grid
        _TABLE["t"] = np.array([ray_radius(float(f), floor=False) for f in grid])
    return np.interp(phi, _TABLE["g"], _TABLE["t"])


def at_iris(phi, r, y, grow=0.0):
    rr = ray_radius(phi, grow) * r
    return Vector((math.cos(phi) * rr, y, OZ + math.sin(phi) * rr))


def glyph_stone(seed):
    """Pale granite with charged cracks, plus carved glyph grooves on the front faces that burn magenta: two bands
    following the arch, broken into glyph dashes, and short radial ticks between them."""
    base = K.rock(GRANITE, seed, strata=6, crack=0.55, crack_glow=0.45, crack_color=CHARGE, frost=0.25, rough=0.65,
                  smooth_top=0.1)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        x, z = p[:, 0], p[:, 2] - OZ
        phi = np.arctan2(z, x)
        rr = outline_table(phi)
        d = np.hypot(x, z) - rr                                       # metres outside the opening outline
        front = ss(0.35, 0.75, -n[:, 1])
        band = np.zeros(len(idx))
        for c, w in ((0.22, 0.035), (0.68, 0.03)):
            band = np.maximum(band, 1 - ss(0.0, w, np.abs(d - c)))
        dash = ss(-0.2, 0.2, np.sin(phi * 38 + 1.7 * np.sin(phi * 7 + seed)))
        tick = (1 - ss(0.0, 0.035, np.abs(((phi * 19) % 1.0) - 0.5) * 0.4)) * ss(0.3, 0.35, d) * (1 - ss(0.55, 0.6, d))
        # Eroded: whole runs of glyphs are worn away, the rest burn unevenly (old, not a neon sign).
        worn = ss(-0.15, 0.25, C.value_noise(p, 0.9, seed + 3)) * (0.55 + 0.45 * ss(-0.5, 0.5, C.value_noise(p, 4.0, seed + 4)))
        carved = np.clip(band * dash + tick, 0, 1) * front
        glyph = carved * worn
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.05, 0.04, 0.06)), carved * 0.6)
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.3, glyph * 0.5)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (glyph * 0.62)[:, None], 0, 1)
        s.disp[idx] -= 0.014 * carved
        # The jamb faces (towards the opening) carry two thin charged seams: the seal's light leaking round the iris.
        jamb = ss(0.5, 0.85, -(n[:, 0] * x + n[:, 2] * z) / np.maximum(np.hypot(x, z), 1e-3)) * (1 - ss(0.0, 0.08, d))
        line = np.maximum(1 - ss(0.0, 0.025, np.abs(p[:, 1] + 0.36)), 1 - ss(0.0, 0.02, np.abs(p[:, 1] - 0.3)))
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (0.6 * jamb * line)[:, None], 0, 1)
        s.disp[idx] -= 0.01 * jamb * line
    return style


def petal_style(seed):
    """Black volcanic glass with the seal sigil carved across all five petals (concentric rings and radial spokes
    that only line up while the iris is shut), the petal rims glowing."""
    base = K.rock(BLACKGLASS, seed, strata=4, crack=0.4, crack_glow=0.25, crack_color=CHARGE, rough=0.35)

    def style(s, idx, part):
        base(s, idx, part)
        p, n = s.p[idx], s.n[idx]
        x, z = p[:, 0], p[:, 2] - OZ
        r = np.hypot(x, z)
        phi = np.arctan2(z, x)
        front = ss(0.3, 0.7, -n[:, 1])
        ring = np.zeros(len(idx))
        for c in (0.55, 1.05, 1.55):
            ring = np.maximum(ring, 1 - ss(0.0, 0.03, np.abs(r - c)))
        spoke = (1 - ss(0.0, 0.05, np.abs(((phi / (2 * math.pi) * 15) % 1.0) - 0.5))) * ss(0.6, 0.7, r) * (1 - ss(1.0, 1.05, r))
        sig = np.clip(ring + spoke * 0.8, 0, 1) * front
        rim = (1 - ss(0.0, 0.04, np.abs(n[:, 1]))) * (1 - ss(0.0, 0.03, np.abs(p[:, 1] + 0.02)))  # a seam mid-edge
        s.albedo[idx] = C.lerp3(s.albedo[idx], CHARGE * 0.3, sig * 0.6)
        s.emit[idx] = np.clip(s.emit[idx] + CHARGE[None, :] * (0.72 * sig + 0.5 * rim)[:, None], 0, 1)
        s.disp[idx] -= 0.01 * sig
        s.rough[idx] = np.clip(s.rough[idx] * (1 - 0.4 * front), 0.15, 1)
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def petal_bounds(i):
    """Seam angles of petal i at the centre (r = 0): the seams spiral by TWIST towards the rim."""
    mid = -math.pi / 2 + 2 * math.pi * i / PETALS - TWIST
    return mid - math.pi / PETALS, mid + math.pi / PETALS


def petal_hinge(i):
    """Hinge of petal i: the middle of its rim, on its back face; axis along the outline there."""
    a0, a1 = petal_bounds(i)
    phi = (a0 + a1) / 2 + TWIST * PETAL_R1
    p = at_iris(phi, 1.0, 0.16)
    if p.z < FLOOR_CLAMP + 0.02:
        p.z = FLOOR_CLAMP
    q0, q1 = at_iris(phi - 0.03, 1.0, 0.16), at_iris(phi + 0.03, 1.0, 0.16)
    t = (q1 - q0)
    t.y = 0
    t.normalize()
    inward = (Vector((0, 0.16, OZ)) - p)
    inward.y = 0
    inward.normalize()
    sign = 1.0 if t.cross(inward).y > 0 else -1.0
    floor = p.z <= FLOOR_CLAMP + 0.01
    return p, t, inward, sign, floor


def petal_surface(i, front):
    a0, a1 = petal_bounds(i)

    def fn(u, v):
        r = PETAL_R0 + (PETAL_R1 - PETAL_R0) * v
        rr = max(ray_radius(0.5 * (a0 + a1) + TWIST * r), 0.5) * r
        gap = 0.028 / max(rr, 0.2)                                     # constant 5.6 cm seam between petals
        phi = a0 + gap + (a1 - a0 - 2 * gap) * u + TWIST * r
        dome = 1 - r * r
        y = -0.11 - 0.24 * dome if front else 0.11 - 0.08 * dome
        p = at_iris(phi, r, y)
        p.z = max(p.z, FLOOR_CLAMP)
        return p
    return fn


def gate_parts():
    b = C.PartBuilder()
    rng = np.random.default_rng(2901)
    # Voussoirs: eleven stones round the arch, the keystone deeper and taller; ragged outer edges (old, broken).
    lo, hi = -math.atan2(OZ, HALF_W), math.pi + math.atan2(OZ, HALF_W)
    n = 11
    for j in range(n):
        g = 0.035 / (HALF_W + RING_W / 2)
        f0 = lo + (hi - lo) * j / n + g
        f1 = lo + (hi - lo) * (j + 1) / n - g
        key = j == n // 2
        depth = DEPTH + (0.16 if key else rng.uniform(-0.05, 0.05))
        grow = RING_W + (0.35 if key else rng.uniform(-0.12, 0.08))
        chip = rng.uniform(0.0, 0.25) if not key else 0.0

        def surf(front, f0=f0, f1=f1, depth=depth, grow=grow, chip=chip, key=key):
            def fn(u, v):
                phi = f0 + (f1 - f0) * u
                ri = ray_radius(phi, floor=False)
                ro = ray_radius(phi, grow=grow, floor=False) - chip * math.sin(math.pi * u) * (v ** 3)
                rr = ri + (ro - ri) * v
                y = (-depth - (0.12 if key else 0.0) * v) if front else depth
                p = Vector((math.cos(phi) * rr, y, OZ + math.sin(phi) * rr))
                p.z = max(p.z, -0.12)
                return p
            return fn
        b.slab("Voussoir", surf(True), surf(False), 8, 4, bone="Root", style=glyph_stone(2910 + j))
    # Buttresses leaning on the jambs, crystals bursting from their cracks.
    for sx in (-1, 1):
        rot = K.rz(8 * sx) @ Matrix.Rotation(R(-7 * sx), 3, "Y")
        b.ellipsoid("Buttress", Vector((sx * (HALF_W + RING_W + 0.45), 0.15, 1.05)), (0.62, 0.78, 1.25), rot, bone="Root",
                    style=K.rock(GRANITE, 2930 + sx, strata=5, crack=0.7, crack_glow=0.6, crack_color=CHARGE, frost=0.35,
                                 rough=0.7), segments=(28, 16), deform=C.chiselled(14, 2930 + sx, 0.3))
        b.ellipsoid("Footing", Vector((sx * (HALF_W + RING_W + 0.95), -0.35, 0.18)), (0.5, 0.42, 0.3), K.rz(25 * sx),
                    bone="Root", style=K.rock(GRANITE, 2935 + sx, strata=5, crack=0.5, frost=0.3, rough=0.75),
                    segments=(18, 10), deform=C.chiselled(10, 2935 + sx, 0.3))
        for k_, (dz, ln, tilt) in enumerate(((1.75, 1.4, 30), (0.9, 0.95, 55), (2.25, 0.8, 15))):
            base = Vector((sx * (HALF_W + RING_W + 0.55), -0.35 + 0.2 * k_, dz))
            d = Vector((sx * math.sin(R(tilt)), -0.35, math.cos(R(tilt))))
            b.shard("Crystal", base, d, 0.15 - 0.03 * k_, ln, sides=6, twist=0.9, flute=0.35, bone="Root",
                    style=Y.shard_style("overcharge_w", 1.1, seed=2940 + 3 * k_ + (sx > 0)))
    # Crown of three crystal horns on the keystone.
    top = OZ + ray_radius(math.pi / 2, grow=RING_W + 0.35, floor=False)
    for k_, (x, ln, lean) in enumerate(((0.0, 2.0, 0.0), (-0.48, 1.35, -0.5), (0.48, 1.35, 0.5))):
        b.shard("Horn", Vector((x, -0.15, top - 0.15)), Vector((lean, -0.2, 1.0)), 0.19 if k_ == 0 else 0.14, ln, sides=6,
                twist=1.1, flute=0.4, bone="Root", style=Y.shard_style("overcharge_w", 1.15, seed=2950 + k_))
    # Threshold slab under the iris.
    b.ellipsoid("Threshold", Vector((0, -0.25, -0.02)), (HALF_W * 1.02, 0.95, 0.12), bone="Root",
                style=K.rock(GRANITE, 2960, strata=4, crack=0.6, crack_glow=0.3, crack_color=CHARGE, rough=0.6, smooth_top=0.3),
                segments=(28, 10), deform=E.boxy(2960, 0.16, 6, 0.12))
    # Iris petals and the seal knot.
    for i in range(PETALS):
        b.slab("Petal", petal_surface(i, True), petal_surface(i, False), 10, 10, bone=f"Petal_{i + 1}",
               style=petal_style(2970 + i))
    b.ellipsoid("SealCore", SEAL_C, (0.15, 0.3, 0.15), bone="Seal", style=K.glow_seam((1.0, 0.75, 0.95), 1.0, CHARGE * 0.7),
                segments=(20, 12), deform=C.chiselled(10, 2980, 0.25))
    for k_ in range(6):
        a = 2 * math.pi * k_ / 6 + 0.3
        d = Vector((math.cos(a) * 0.55, -1.0, math.sin(a) * 0.55))
        b.shard("SealSpike", SEAL_C + Vector((0, -0.18, 0)), d, 0.05, 0.34 if k_ % 2 else 0.26, sides=5, twist=0.8,
                flute=0.3, bone="Seal", style=Y.shard_style("overcharge_w", 1.5, seed=2981 + k_))
    for k_ in range(4):
        b.shard("SealShard", seal_shard_base(k_), seal_shard_dir(k_), 0.045, 0.22, sides=5, twist=0.6, flute=0.3,
                bone=f"Seal_{k_ + 1}", style=Y.shard_style("overcharge_w", 1.3, seed=2990 + k_))
    return b


def seal_shard_out(k_):
    a = 2 * math.pi * k_ / 4 + math.pi / 4
    return Vector((math.cos(a), 0, math.sin(a)))


def seal_shard_base(k_):
    return SEAL_C + seal_shard_out(k_) * 0.27 + Vector((0, -0.12, 0))


def seal_shard_dir(k_):
    return seal_shard_out(k_) + Vector((0, -0.6, 0))


def gate_bones():
    out = [("Root", Vector((0, 0, 0)), Vector((0, 0, 0.4)), None, True),
           ("Seal", SEAL_C, SEAL_C + Vector((0, -0.35, 0)), "Root", True)]
    for k_ in range(4):
        h = seal_shard_base(k_)
        out.append((f"Seal_{k_ + 1}", h, h + seal_shard_dir(k_).normalized() * 0.2, "Root", True))
    for i in range(PETALS):
        h, t, inward, sign, floor = petal_hinge(i)
        out.append((f"Petal_{i + 1}", h, h + inward * 0.4, "Root", True))
    return out


GATE_SCALE = ["Seal"] + [f"Seal_{k_ + 1}" for k_ in range(4)]


def gate_specs(swing=None, jitter=0.0, seal=1.0, shards=0.0, shard_scale=1.0, f=0.0):
    """`swing[i]` 0..1 (1 = fully open), `jitter` rumble amplitude in radians, `seal` the knot's scale, `shards` how
    far the four seal shards have flown (0..1)."""
    sp = {"Root": {"rot": []}, "Seal": {"rot": [], "scale": max(seal, 0.001)}}
    for k_ in range(4):
        out = seal_shard_out(k_) * 1.3 + Vector((0, -1.1, 0.4 * shards))
        sp[f"Seal_{k_ + 1}"] = {"rot": [(Vector((0, 1, 0)), 3.0 * shards * (1 if k_ % 2 else -1))],
                                "loc": out * shards, "scale": max(shard_scale, 0.001)}
    for i in range(PETALS):
        h, t, inward, sign, floor = petal_hinge(i)
        k_ = (swing or [0.0] * PETALS)[i]
        ang = (FLOOR_ANGLE if floor else OPEN_ANGLE) * k_ + jitter * math.sin(f * 1.9 + i * 1.3)
        sp[f"Petal_{i + 1}"] = {"rot": [(t, sign * ang)]}
    return sp


def overshoot(t):
    """0..1 with a small bounce past 1 (heavy stone stopping against its seat)."""
    t = min(max(t, 0.0), 1.0)
    return 1 - (1 - t) ** 3 + 0.06 * math.sin(math.pi * t) * t


SWING_ORDER = [0, 2, 4, 1, 3]               # bottom first, then alternating round the arch


def gate_closed(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return gate_specs(seal=1.0 + 0.04 * math.sin(w), shards=0.01 * (0.5 + 0.5 * math.sin(w + 1)))
    return IDLE_FRAMES, spec, True


def gate_open(poser):
    """Rumble (0-30, the knot swells), the seal cracks at SEAL_CRACK (shards fly, the knot collapses), the petals
    unlock at FIRST_LIGHT (seams open: the light shows), then swing back one after another from SWING_START, the
    last seats at FULLY_OPEN."""
    def spec(t, f):
        rumble = ease(0, SEAL_CRACK, f) * (1 - ease(SEAL_CRACK + 4, FIRST_LIGHT + 8, f))
        seal = (1.0 + 0.22 * ease(4, SEAL_CRACK, f)) * (1 - ease(SEAL_CRACK, SEAL_CRACK + 8, f))
        shards = ease(SEAL_CRACK, SEAL_CRACK + 16, f)
        unlock = ease(FIRST_LIGHT, FIRST_LIGHT + 8, f) * 0.045
        swing = []
        for i in range(PETALS):
            start = SWING_START + 9 * SWING_ORDER.index(i)
            dur = FULLY_OPEN - SWING_START - 9 * (PETALS - 1)
            swing.append(max(unlock, overshoot((f - start) / dur)))
        return gate_specs(swing=swing, jitter=R(0.7) * rumble, seal=seal, shards=shards,
                          shard_scale=1 - ease(SEAL_CRACK + 10, SEAL_CRACK + 18, f), f=f)
    return OPEN_FRAMES, spec, False


def gate_opened(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return gate_specs(swing=[1.0 + 0.006 * math.sin(w + i) for i in range(PETALS)], seal=0.0, shards=1.0,
                          shard_scale=0.0)
    return IDLE_FRAMES, spec, True


def build_gate():
    rigid = {"Voussoir", "Buttress", "Footing", "Crystal", "Horn", "Threshold", "Petal", "SealCore", "SealSpike", "SealShard"}
    clips = {"Closed_Idle": gate_closed, "Open": gate_open, "Opened_Idle": gate_opened}
    report = {
        "pivot": "centre of the threshold on the ground; the gate faces -Y in Blender (Unity +Z after the FBX axis "
                 "conversion): the giant walks out towards -Y",
        "opening_m": {"width": HALF_W * 2, "height": SPRING + CAP, "note": "The Shattered (3.4 x 5.3 x 3.6) fits"},
        "unity_scale": 1.0,
        "clips_info": {
            "Closed_Idle": {"frames": IDLE_FRAMES, "loop": True, "note": "dormant: the seal knot breathes"},
            "Open": {"frames": OPEN_FRAMES, "loop": False, "seal_crack_frame": SEAL_CRACK, "first_light_frame": FIRST_LIGHT,
                     "swing_start_frame": SWING_START, "fully_open_frame": FULLY_OPEN,
                     "note": "the first and only opening; rumble 0-30 (camera shake, dust off the voussoirs), crack: "
                             "the knot collapses and four seal shards fly (flash + shard burst at Seal), first light: "
                             "the seams open (raise the emission and a point light inside), the petals swing back "
                             "bottom first; spawn the giant in the flash at first_light or when fully open"},
            "Opened_Idle": {"frames": IDLE_FRAMES, "loop": True, "note": "stays open for the rest of the level"}},
        "scale_keyed_bones": GATE_SCALE,
        "emission": "magenta glyph grooves, jamb seams, petal rims and sigil, crystals; suggested _EmissionColor "
                    "strength 0.6 while closed (dormant), ramp to 2.5 from first_light to fully_open, then 1.6",
    }
    asset, arm, actions, poser = M.small_rig(GATE, gate_parts(), gate_bones(), clips, 6400, families=("rock", "crystal"),
                                             rigid=rigid, blend=0, scale_bones=GATE_SCALE, voxel=0.013, ao=0.4,
                                             extrusion=0.04, ray=0.12, emission=2.6, specular=0.3, k=3.0, mirror=False,
                                             tex=1024, low=512, report=report, symmetric=False)
    return asset, arm, actions


# =============================================================================================================
# SealCrystal
# =============================================================================================================

SEAL_X = {"A": -2.5, "B": 0.0, "C": 2.5}


def seal_stone(seed, inlay=0.0):
    base = K.rock(SEAL_STONE, seed, strata=12, crack=0.45, rough=0.55)

    def style(s, idx, part):
        base(s, idx, part)
        s.emit[idx] = s.emit[idx] * 0.0
    return style


def crystal(seed, strength=1.0):
    """A darker glow mask than the decor crystals: bright flutes, node rings and tip, a dim body, so the facets and
    fracture lines still show once Unity tints and blooms it. Round 30: on flat-faced crystals the face centres catch
    the flute glow and the edges stay dark, so every facet reads as its own plane."""
    return Y.shard_style("seal_white", 0.62 * strength, seed=seed, flute_glow=0.9)


def facet_glow(style, sides, phase, gain=0.35):
    """Round 30: lights the middle of each flat face a step more than its edges (the inner light shows through
    the faces, the edges stay dark lines), so a flat-faced crystal never reads as a plain lamp."""
    cell = 2 * math.pi / sides

    def wrapped(s, idx, part):
        style(s, idx, part)
        L = C.local_points(part, s.p[idx])
        z = np.clip(L[:, 2], 0, 1)
        ang = (np.arctan2(L[:, 1], L[:, 0]) - phase) % cell
        centre = 1 - np.abs(ang - cell / 2) / (cell / 2)
        face = ss(0.35, 0.95, centre) * ss(0.05, 0.3, z)
        edge = 1 - ss(0.04, 0.16, centre)
        s.emit[idx] = np.clip(s.emit[idx] * (1 - 0.6 * edge)[:, None] + (gain * face)[:, None], 0, 1)
        s.albedo[idx] = s.albedo[idx] * (1 - 0.35 * edge)[:, None]
    return wrapped


def faceted(b, name, base, direction, radius, length, seed, tip=0.32, phase=0.0, strength=1.0, sides=6, bottom=0.0,
            nodes=(0.32, 0.58, 0.8)):
    """Round 30 (SC1): a flat-faced crystal: straight prism faces (no twist, no flutes) up to 1 - `tip`, then a
    pyramid point; `bottom` > 0 also points the root (a doubly terminated crystal). The irregular `phase` keeps
    neighbours from lining up."""
    def profile(t):
        r = 1.0 if t < 1 - tip else max((1 - t) / tip, 0.0)
        if bottom and t < bottom:
            r *= 0.35 + 0.65 * t / bottom
        return r
    style = Y.shard_style("seal_white", 0.62 * strength, nodes=nodes, seed=seed, flute_glow=0.9)
    return b.shard(name, base, direction, radius, length, sides=sides, twist=0.0, flute=0.0, profile=profile, rings=14,
                   phase=phase, bone="Root", style=facet_glow(style, sides, phase))


def side_crystals(b, o, centre_z, count, radius, length, seed, spread=0.32, lean=0.9, strength=0.85):
    """Small crystals sprouting round a base or up a spire at odd angles (they make the glow break into facets)."""
    rng = np.random.default_rng(seed)
    for k_ in range(count):
        a = 2 * math.pi * k_ / count + rng.uniform(-0.4, 0.4)
        out = Vector((math.cos(a), math.sin(a), 0))
        z = centre_z + rng.uniform(-0.04, 0.08)
        d = out * lean + Vector((0, 0, 1.0)) + Vector(rng.uniform(-0.2, 0.2, 3))
        faceted(b, "SideCrystal", o + out * spread + Vector((0, 0, z)), d, radius * rng.uniform(0.75, 1.2),
                length * rng.uniform(0.6, 1.4), seed + k_, tip=0.4, phase=rng.uniform(0, 1), strength=strength, nodes=())


def seal_parts():
    b = C.PartBuilder()
    rng = np.random.default_rng(2920)
    # A: a tall faceted spire rising out of a carved collar, a branch crystal grown out of its flank, small side
    # crystals round its foot and two splinters.
    o = Vector((SEAL_X["A"], 0, 0))
    faceted(b, "Spire", o + Vector((0, 0, -0.05)), Vector((0.06, 0.03, 1)), 0.2, 1.65, 2921, tip=0.3, phase=0.2)
    faceted(b, "Branch", o + Vector((0.08, 0.02, 0.55)), Vector((0.75, 0.25, 1.0)), 0.085, 0.62, 2924, tip=0.4, phase=0.6)
    faceted(b, "Branch", o + Vector((-0.06, -0.05, 0.85)), Vector((-0.55, -0.6, 1.0)), 0.065, 0.45, 2925, tip=0.45,
            phase=0.1)
    for k_ in range(6):
        a = 2 * math.pi * k_ / 6
        b.ellipsoid("Collar", o + Vector((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.12)), (0.17, 0.12, 0.17), K.rz(math.degrees(a)),
                    bone="Root", style=seal_stone(2922 + k_), segments=(14, 8), deform=C.chiselled(8, 2922 + k_, 0.3))
    side_crystals(b, o, 0.1, 4, 0.06, 0.32, 2934, spread=0.36)
    for k_, a in enumerate((0.8, 3.6)):
        d = Vector((math.cos(a), math.sin(a), 1.6))
        faceted(b, "Splinter", o + Vector((math.cos(a) * 0.42, math.sin(a) * 0.42, 0.0)), d, 0.07, 0.45 - 0.1 * k_,
                2930 + k_, tip=0.4, phase=0.3 * k_, strength=0.9)
    # B: three faceted crystals leaning out of a broken socket, small side crystals round them.
    o = Vector((SEAL_X["B"], 0, 0))
    b.ellipsoid("Socket", o + Vector((0, 0, 0.08)), (0.5, 0.44, 0.2), bone="Root", style=seal_stone(2940),
                segments=(24, 10), deform=C.chiselled(12, 2940, 0.3))
    for k_, (dx, dy, ln, lean, r) in enumerate(((0.0, 0.05, 1.15, (0.1, 0.05), 0.16), (0.22, -0.12, 0.8, (0.6, -0.3), 0.12),
                                                (-0.2, -0.08, 0.6, (-0.55, -0.35), 0.11))):
        faceted(b, "Cluster", o + Vector((dx, dy, 0.1)), Vector((lean[0], lean[1], 1)), r, ln, 2941 + k_, tip=0.32,
                phase=0.37 * k_)
    side_crystals(b, o + Vector((0.02, 0.1, 0)), 0.2, 5, 0.055, 0.3, 2944, spread=0.24, lean=1.2)
    # C: a crystal held up in a cradle of three curved stone claws.
    o = Vector((SEAL_X["C"], 0, 0))
    b.ellipsoid("CradleBase", o + Vector((0, 0, 0.06)), (0.42, 0.42, 0.14), bone="Root", style=seal_stone(2950),
                segments=(24, 10), deform=C.chiselled(10, 2950, 0.25))
    for k_ in range(3):
        a = 2 * math.pi * k_ / 3 + 0.4
        out = Vector((math.cos(a), math.sin(a), 0))
        pts = [o + out * 0.3 + Vector((0, 0, 0.1)), o + out * 0.42 + Vector((0, 0, 0.55)), o + out * 0.3 + Vector((0, 0, 0.95)),
               o + out * 0.16 + Vector((0, 0, 1.12))]
        b.tube("Claw", pts, [0.09, 0.075, 0.055, 0.025], sides=8, chain=["Root"], style=seal_stone(2951 + k_))
    faceted(b, "Held", o + Vector((0, 0, 0.3)), Vector((0, 0, 1)), 0.17, 0.97, 2955, tip=0.28, bottom=0.25, phase=0.4,
            strength=1.1)
    for k_ in range(5):
        a = rng.uniform(0, 2 * math.pi)
        K.chip(b, (o.x + math.cos(a) * 0.55, math.sin(a) * 0.5, 0.0), (0.07, 0.06, 0.05), 2960 + k_, seal_stone(2960 + k_))
    return b


def build_seal():
    asset = C.build_mesh_asset(SEAL, seal_parts(), voxel=0.005, target_tris=5200, tex_size=512, ao_distance=0.12,
                               extrusion=0.02, ray=0.05, emission_strength=2.6, smooth_iterations=2, symmetric=False,
                               sharp_angle=35, specular=0.3, mirror=False, k=1.0, families=("crystal", "rock"),
                               low_size=256)
    objs = split_by_x(asset.low, SEAL_X, SEAL)
    bpy.data.objects.remove(asset.low)
    M.export_static(os.path.join(C.EXPORTS, SEAL, SEAL + ".fbx"), objs)
    C.write_report(SEAL, {
        "asset": SEAL, "round": ROUND, "objects": {o.name: C.tri_count(o) for o in objs},
        "size_m": {o.name: [round(d, 3) for d in o.dimensions] for o in objs},
        "texture_size": 512, "low_texture_size": 256, "material": f"{SEAL}_Mat (shared by the three)",
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "detail": asset.details,
        "pivot": "each crystal's centre on the ground", "unity_scale": 1.0,
        "round30": "SC1: flat-faced crystals (no flutes, no twist, hard edges) with the glow brightest in the middle "
                   "of each face and dark edge lines; A gets two branch crystals and side crystals, B side crystals",
        "emission": "greyscale mask (white glow, stone black): set _EmissionColor = the immune tower's colour x 2.4; "
                    "dormant/neutral: lilac (0.62, 0.45, 1.0) x 1.6; Release: white x 3",
        "use": "BOSSES.md 'the sealed crystals turn the colour of the immune type': 3-5 round the rift (mix A/B/C, "
               "any yaw, scale 0.8-1.2)"})
    C.save_blend(SEAL)
    return objs


def split_by_x(obj, centres, prefix, half=1.2):
    out = []
    for key, cx in centres.items():
        me = obj.data.copy()
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(me)
        kill = [f for f in bm.faces if abs(f.calc_center_median().x - cx) > half]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        for v in bm.verts:
            v.co.x -= cx
        bm.to_mesh(me)
        bm.free()
        name = f"{prefix}_{key}"
        me.name = name
        out.append(C.link(bpy.data.objects.new(name, me)))
    return out


# =============================================================================================================
# Gate_IceBlock
# =============================================================================================================

ICE_W, ICE_D, ICE_H = 1.6, 0.74, 1.9        # half width, half depth, height (over Gate_G3: 2.68 x 1.06 x 1.49)


def ice_style(seed, glow=0.35, facets=None, frost=1.0):
    """Round 30 (IB1): clear blue ice in the middle of the big faces (the albedo alpha turns that into ~0.25
    opacity), white where it is thick or broken: fracture planes inside, frost on the facet ridges (`facets`, an
    ice_kit.Facets), rime patches on the up faces. The whiter the texel, the more opaque (alpha_from_cracks)."""
    clear = np.array((0.4, 0.64, 0.84))
    white = np.array((0.93, 0.97, 1.0))

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        if facets is not None:
            ridge, inner = facets.ridge(C.local_points(part, p))
        else:
            ridge, inner = np.zeros(len(idx)), np.ones(len(idx))
        q = p * np.array((1.0, 1.0, 0.8))
        frac = (1 - ss(0.0, 0.045, np.abs(C.value_noise(q, 2.2, seed + 5)))) * ss(-0.3, 0.2, C.value_noise(q, 0.9, seed + 2))
        frac = np.maximum(frac, (1 - ss(0.0, 0.02, np.abs(C.value_noise(q, 5.0, seed + 6)))) * 0.35)
        up = ss(0.45, 0.9, n[:, 2]) * ss(-0.3, 0.3, C.value_noise(p, 3.0, seed + 7))
        edge = ridge * (0.6 + 0.4 * ss(-0.3, 0.3, C.value_noise(p, 6.0, seed + 9)))
        col = clear * (0.9 + 0.12 * C.value_noise(p, 1.5, seed + 1))[:, None]
        col = C.lerp3(col, clear * 0.75, inner * 0.3)
        amount = np.clip(np.maximum.reduce([frac * 0.9, edge * frost, up * 0.85 * frost]), 0, 1)
        col = C.lerp3(col, white, amount)
        s.albedo[idx] = col
        line = (1 - ss(0.0, 0.03, np.abs(C.value_noise(q, 3.5, seed + 11)))) * ss(0.1, 0.4, C.value_noise(q, 1.2, seed + 12))
        s.emit[idx] = np.clip(COLD[None, :] * (0.2 * frac + glow * line)[:, None], 0, 1)
        s.rough[idx] = np.clip(0.06 + 0.55 * amount, 0.05, 1)
        s.disp[idx] = -0.004 * frac + 0.003 * edge
        s.fam[idx] = E.FAM_CRYSTAL
    return style


def ice_chunk(b, centre, radii, rot, seed, count=14, depth=0.28, glow=0.35):
    f = IK.Facets(count, seed, depth)
    b.ellipsoid("Ice", Vector(centre), radii, rot, bone="Root", style=ice_style(seed, glow, facets=f), segments=(26, 14),
                deform=f.deform)


def ice_parts():
    b = C.PartBuilder()
    rng = np.random.default_rng(2925)
    # The mass: big hewn chunks grown together round the gate, taller at the pylons.
    chunks = [((-1.0, 0.0, 0.85), (0.72, 0.72, 1.0)), ((1.0, 0.0, 0.85), (0.72, 0.72, 1.0)),
              ((0.0, 0.0, 0.95), (1.05, 0.7, 0.92)), ((-0.35, -0.2, 0.45), (0.9, 0.6, 0.5)),
              ((0.45, 0.2, 0.5), (0.9, 0.6, 0.55)), ((0.0, 0.05, 1.55), (0.95, 0.55, 0.45))]
    for k_, (c, r) in enumerate(chunks):
        ice_chunk(b, c, r, K.rz(rng.uniform(-12, 12)), 2926 + k_)
    # Frost spikes grown off the top and the corners (the Warden's breath froze outwards).
    for k_ in range(9):
        x = rng.uniform(-1.4, 1.4)
        y = rng.uniform(-0.45, 0.45)
        z = 1.55 + 0.25 * math.cos(x * 1.2)
        d = Vector((x * 0.35 + rng.uniform(-0.3, 0.3), y * 0.8, 1.0))
        b.prism("Spike", Vector((x, y, z - 0.08)), d, rng.uniform(0.07, 0.13), rng.uniform(0.2, 0.5), rng.uniform(0.12, 0.25),
                sides=5, twist=rng.uniform(0, 1), bone="Root", style=ice_style(2940 + k_, 0.5), taper=0.8)
    for k_, sx in enumerate((-1, 1, -1, 1)):
        y = -0.55 if k_ < 2 else 0.55
        b.prism("Spike", Vector((sx * 1.4, y, 0.25)), Vector((sx * 1.0, y, 0.6)), 0.09, 0.3, 0.2, sides=5, twist=0.4 * k_,
                bone="Root", style=ice_style(2950 + k_, 0.5), taper=0.8)
    # Round 30 (IB1): icicles hanging off the top rim of both long faces, so the block reads as frozen ice (and its
    # outline breaks up) from the game camera.
    for side in (-1, 1):
        # An overhanging frost lip along the rim, which the icicles hang from.
        ice_chunk(b, (0, side * 0.64, 1.5), (1.3, 0.2, 0.13), K.rz(3 * side), 2965 + side, count=12, depth=0.3)
        for k_, x in enumerate(np.linspace(-1.2, 1.2, 7)):
            x += rng.uniform(-0.1, 0.1)
            top = 1.5 - 0.05 * abs(x)
            base = Vector((x, side * (0.76 - 0.1 * (x / 1.3) ** 2), top))
            ln = rng.uniform(0.3, 0.62)
            b.shard("Icicle", base, Vector((rng.uniform(-0.08, 0.08), side * 0.12, -1.0)), rng.uniform(0.06, 0.095), ln,
                    sides=6, twist=rng.uniform(0, 1), flute=0.25, bone="Root", style=ice_style(2970 + k_, 0.4, frost=0.7))
    # A frost apron on the road.
    b.ellipsoid("Apron", Vector((0, 0, 0.0)), (1.85, 1.05, 0.08), bone="Root", style=ice_style(2960, 0.2), segments=(30, 8),
                deform=E.boxy(2960, 0.2, 8, 0.2))
    return b


def build_ice():
    asset = M.static_asset(ICE, ice_parts(), 2900, families=("crystal", "rock"), voxel=0.012, ao=0.2, extrusion=0.03,
                           ray=0.08, emission=2.0, k=2.0, mirror=False)
    low = asset.low
    low.data.set_sharp_from_angle(angle=R(38))
    alpha_from_cracks(os.path.join(C.EXPORTS, ICE))
    M.export_static(os.path.join(C.EXPORTS, ICE, ICE + ".fbx"), [low])
    C.write_report(ICE, {
        "asset": ICE, "round": 30, "game_tris": C.tri_count(low), "source_tris": asset.high_tris,
        "size_m": [round(d, 3) for d in low.dimensions], "texture_size": 512, "low_texture_size": 256,
        "material": f"{ICE}_Mat", "maps": ["Albedo (RGBA: alpha = opacity)", "Normal", "Emission", "Mask"],
        "detail": asset.details, "unity_scale": 1.0,
        "pivot": "road centre on the ground, as the Gate prefab: put it at the gate's own position and rotation",
        "fits": "Gate_G1 (2.52 x 0.57 x 1.28) to Gate_G3 (2.68 x 1.06 x 1.49) with 0.1-0.25 m of ice round them",
        "material_note": "transparent (alpha blend, depth write on, rendered after the gate): Albedo alpha is 0.25 in "
                         "the clear middle of the big faces up to 0.9 in the white fractures, facet ridges, frost and "
                         "icicles; smoothness from the Mask G; emission a faint cold glow in the cracks (x 1.2). The "
                         "Liquid_Ice shader's fresnel/refraction may be layered on top",
        "round30": "IB1: thickness variation in the alpha (clear faces 0.25, breaks 0.9) and 14 icicles hanging off "
                   "the top rim of both long faces",
        "use": "Frost Warden Freeze_Gate: show for 8 s over the gate (scale 0.9 -> 1 in 0.3 s at the cast frame); on "
               "Shatter_Through hide it with the ice shard burst (VFX Debris_Shards tinted ice)"})
    C.save_blend(ICE)
    return low


def alpha_from_cracks(folder):
    """Albedo alpha from the baked colour: clear blue ice 0.25, the whiter (fractures, frost, ridges, icicles) the
    more opaque, up to 0.9."""
    for sub in ("", "Low"):
        path = os.path.join(folder, sub, f"{ICE}_Albedo.png")
        if not os.path.exists(path):
            continue
        img = C.image_to_array(path)
        rgb = img[..., :3]
        lum = rgb.mean(axis=2)
        sat = rgb.max(axis=2) - rgb.min(axis=2)
        white = np.clip((lum - 0.5) / 0.38, 0, 1) * np.clip(1 - sat * 2.2, 0, 1)
        img[..., 3] = np.clip(0.25 + 0.65 * white ** 0.8, 0, 1)
        C.array_to_image(img, path)


# =============================================================================================================
# Cutscene_Drill_Wreck
# =============================================================================================================

# Round 31: the wreck is now the bind pose of a rigged, fractured rig with a Collapse clip that starts from the
# standing derrick (build_drill_collapse.py). This entry point stays so `--only DrillWreck` keeps working.

def build_wreck(bake=True):
    import build_drill_collapse as DC
    return DC.build_wreck(bake=bake)


# =============================================================================================================
# Drained gate emission
# =============================================================================================================

def drained_maps():
    """The gate and gate-gun glow almost out: each lit texel dims to 14 % of its colour plus a faint violet residue
    of its brightness (the Sealed One drank the power). Same atlas, so it swaps over the shipped Emission map."""
    out = []
    for asset in ("Gate", "Tower_GateGun"):
        folder = os.path.join(C.EXPORTS, asset)
        for sub in ("", "Low"):
            src = os.path.join(folder, sub, f"{asset}_Emission.png")
            if not os.path.exists(src):
                continue
            img = C.image_to_array(src)
            rgb = img[..., :3]
            lum = rgb.max(axis=2)
            h, w = lum.shape
            yy, xx = np.mgrid[0:h, 0:w]
            # Uneven dying: some islands keep a little more light (noise in UV space).
            flicker = 0.75 + 0.25 * np.sin(xx * 0.043 + np.sin(yy * 0.031) * 3.0) * np.sin(yy * 0.057 + 1.3)
            res = rgb * 0.14 * flicker[..., None] + VIOLET[None, None, :] * (lum * 0.2 * flicker)[..., None]
            arr = img.copy()
            arr[..., :3] = np.clip(res, 0, 1)
            path = os.path.join(folder, sub, f"{asset}_Drained_Emission.png")
            C.array_to_image(arr, path)
            out.append(os.path.relpath(path, C.EXPORTS).replace("\\", "/"))
    C.log("drained", out)
    return out


# =============================================================================================================
# Previews
# =============================================================================================================

def game_view(path, target, width=900, height=560, zoom=1.0):
    cam = C.game_camera(Vector(target), width / C.GAME_PIXELS_PER_UNIT / zoom)
    C.render(cam, path, width, height, samples=24)
    bpy.data.objects.remove(cam)
    return path


def preview_gate(arm, actions, out):
    C.preview_world()
    C.light_rig(scale=3.0, rim_color=(0.75, 0.6, 1.0))
    C.ground(color=(0.05, 0.05, 0.07))
    acts = actions
    cam = C.closeup_camera(Vector((0, 0, 2.3)), 15, azimuth=-28, elevation=16, lens=50)
    M.strip(GATE, arm, acts["Open"], cam, out, "Open", [0, 24, SEAL_CRACK + 4, FIRST_LIGHT + 6, 72, 92, 112, FULLY_OPEN])
    bpy.data.objects.remove(cam)
    cam = C.closeup_camera(Vector((0, 0.5, 2.3)), 15, azimuth=150, elevation=20, lens=50)
    M.strip(GATE, arm, acts["Open"], cam, out, "Open_Back", [0, 60, 100, FULLY_OPEN])
    bpy.data.objects.remove(cam)
    M.turntable(GATE, arm, acts["Closed_Idle"], 0, (0, 0, 2.3), 15, out)
    paths = []
    for i, (act, f) in enumerate(((acts["Closed_Idle"], 0), (acts["Open"], FIRST_LIGHT + 6), (acts["Opened_Idle"], 0))):
        C.set_action(arm, act, f)
        paths.append(game_view(os.path.join(out, f"_gate_game_{i}.png"), (0, 0, 1.6)))
    C.contact_sheet(paths, os.path.join(out, f"{GATE}_game.png"), 3, 600)
    for p in paths:
        os.remove(p)


def preview_static(objs, out, label, target, distance, game_zoom=1.0, extra=()):
    C.preview_world()
    C.light_rig(scale=max(distance / 5, 0.8))
    C.ground(color=(0.05, 0.05, 0.07))
    paths = []
    for az in (-35, 60, 160):
        cam = C.closeup_camera(Vector(target), distance, azimuth=az, elevation=22, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"_{label}_{az}.png"), 640, 420, samples=24))
        bpy.data.objects.remove(cam)
    paths.append(game_view(os.path.join(out, f"_{label}_game.png"), target, 640, 420, zoom=game_zoom))
    C.contact_sheet(paths, os.path.join(out, f"{label}.png"), 2, 640)
    for p in paths:
        os.remove(p)


def preview_alpha(obj, image_key="Albedo"):
    """Preview only: the Albedo alpha as opacity (as the Unity material will use it)."""
    mat = obj.data.materials[0]
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    for n in nt.nodes:
        if n.type == "TEX_IMAGE" and n.image and image_key in n.image.name:
            n.image.reload()
            nt.links.new(n.outputs["Alpha"], bsdf.inputs["Alpha"])
    for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
        try:
            setattr(mat, attr, val)
        except (AttributeError, TypeError):
            pass


def preview_tints(objs, out, label, target, tints):
    """Preview only: the greyscale emission tinted like Unity's _EmissionColor, one game-camera view per tint."""
    mat = objs[0].data.materials[0]
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    src = bsdf.inputs["Emission Color"].links[0].from_socket
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    nt.links.new(src, mix.inputs[6])
    nt.links.new(mix.outputs[2], bsdf.inputs["Emission Color"])
    paths = []
    for i, t in enumerate(tints):
        mix.inputs[7].default_value = (*t, 1.0)
        paths.append(game_view(os.path.join(out, f"_{label}_tint{i}.png"), target, 520, 340, zoom=2.4))
    C.contact_sheet(paths, os.path.join(out, f"{label}_tints.png"), len(tints), 520)
    for p in paths:
        os.remove(p)
    nt.links.new(src, bsdf.inputs["Emission Color"])
    nt.nodes.remove(mix)


def main():
    args = C.cli_args()
    out = os.path.join(C.preview_dir())
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else ["RuinGate", "SealCrystal", "IceBlock", "DrillWreck", "Drained"]
    render = not args.get("norender")
    if "RuinGate" in names:
        # Round 53: the gate is built by build_ruingate53.py (one band of stone on a plinth); this round-29 build is
        # kept only as the base it was rebuilt from (--old-gate).
        if args.get("old-gate"):
            asset, arm, actions = build_gate()
        else:
            import build_ruingate53 as RG
            asset, arm, actions = RG.build_gate()
        if render:
            preview_gate(arm, actions, out)
    if "SealCrystal" in names:
        C.reset_scene()
        objs = build_seal()
        if render:
            for i, o in enumerate(objs):
                o.location.x = (i - 1) * 1.6
            preview_static(objs, out, SEAL, (0, 0, 0.6), 5.5, game_zoom=2.0)
            preview_tints(objs, out, SEAL, (0, 0, 0.5), [(1.0, 0.55, 0.15), (0.3, 0.75, 1.0), (0.62, 0.45, 1.0)])
    if "IceBlock" in names:
        C.reset_scene()
        low = build_ice()
        if render:
            gate = None
            path = os.path.join(C.EXPORTS, "Gate", "Gate_G2.fbx")
            if os.path.exists(path):
                bpy.ops.import_scene.fbx(filepath=path)
                gate = [o for o in bpy.context.selected_objects]
                for o in gate:
                    if o.type == "MESH" and not o.name.endswith("G2"):
                        o.hide_render = True
            preview_alpha(low)
            preview_static([low], out, ICE, (0, 0, 0.8), 6.0, game_zoom=2.0)
    if "DrillWreck" in names:
        C.reset_scene()
        C.preview_world()
        spec, objs, acts = build_wreck(bake=not args.get("nobake"))
        if render:
            import build_drill_collapse as DC
            DC.previews(spec, objs, acts, out)
    if "Drained" in names:
        drained_maps()


if __name__ == "__main__":
    main()
