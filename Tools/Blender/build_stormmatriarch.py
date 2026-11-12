"""StormMatriarch: the Skyray boss (air) that releases small flyers (round 6, see ArtSource/DESIGN_NOTES.md).

The Skyray as a brood mother of the air: broad sickle fins with a glowing leading edge studded with capacitor beads,
a smaller aft fin pair behind them, a crowned ram prow (three backward horns, big barbed pincers, six eye pits) and
two long charge horns sweeping back from it with glowing node rings. On the back, two armoured hatch plates on
their own bones cover a glowing brood chamber: in the Release clip they open like beetle elytra and snap shut, and
the small flyers come out of the back. A long barbed tail ends in a trident with charge nodes.

The palette is one parameter (PALETTES at the top): `--palette cool` (default, the Skyray's colours) or
`--palette warm` (Remco's open question; a warm variant bakes into the preview folder, never into Exports).

Clips: Fly (loop), Idle (loop), Hit, Death, Release (the power; flyers at RELEASE_FRAME). Also exports
StormMatriarch_LOD1.

Run: blender --background --python Tools/Blender/build_stormmatriarch.py -- [--preview <dir>] [--palette warm] [--norender]
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
    # The Skyray's palette (blue body, cyan glow, violet flush): the family stays cool.
    "cool": dict(top=(0.24, 0.34, 0.68), side=(0.09, 0.11, 0.3), belly=(0.045, 0.05, 0.12), seam=(0.022, 0.026, 0.065),
                 dark=(0.05, 0.06, 0.17), glow=(0.4, 0.95, 1.0), eye=(0.35, 0.95, 1.0), membrane=(0.2, 0.12, 0.32),
                 bone=(0.66, 0.8, 0.96), under=(0.5, 0.56, 0.68), flush=(0.32, 0.14, 0.42), brood=(0.55, 0.95, 1.0)),
    # The warm alternative: plum body, magenta flush, orange leading edge, amber charge.
    "warm": dict(top=(0.42, 0.12, 0.2), side=(0.16, 0.04, 0.08), belly=(0.06, 0.02, 0.035), seam=(0.03, 0.01, 0.018),
                 dark=(0.1, 0.03, 0.06), glow=(1.0, 0.48, 0.2), eye=(1.0, 0.75, 0.25), membrane=(0.35, 0.06, 0.2),
                 bone=(0.95, 0.8, 0.66), under=(0.66, 0.46, 0.42), flush=(0.55, 0.06, 0.34), brood=(1.0, 0.7, 0.25)),
}
PALETTE_NAME, PALETTE = B.palette_variant(PALETTES, "cool")

NAME = "StormMatriarch"
LOD_NAME = "StormMatriarch_LOD1"
# Built in Skyray units times M: at Unity scale 1.2 x 0.6 she is about 1.9 times a Skyray (as the factory boss).
M = 1.85
K = 1.3 * M
DETAIL_K = 1.9
GAME_TRIS = 14500
LOD_TRIS = 6000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("membrane", "chitin", "crystal")
GAME_SCALE = 1.2 * 0.6
SKYRAY_SCALE = 0.7
AIR_HEIGHT = 1.6

PAL = H.Palette(**{k: PALETTE[k] for k in ("top", "side", "belly", "seam", "dark", "glow", "eye", "membrane", "bone")})
BROOD = H.Palette(top=PALETTE["side"], side=PALETTE["seam"], belly=PALETTE["belly"], seam=PALETTE["seam"],
                  dark=PALETTE["dark"], glow=PALETTE["brood"], eye=PALETTE["eye"], membrane=PALETTE["membrane"],
                  bone=PALETTE["bone"])
UNDER = PALETTE["under"]
FLUSH = PALETTE["flush"]

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def P(x, up, fwd):
    """A point in Skyray units, scaled to the boss."""
    return G(x * M, up * M, fwd * M)


def R(*r):
    return tuple(v * M for v in r) if len(r) > 1 else r[0] * M


# ---- Main fins (Skyray units, right side): u 0..1 root to tip, v 0..1 leading to trailing edge -----------------
FIN_X0, FIN_SPAN = 0.14, 0.92
CHAINS = {"A": 0.22, "B": 0.74}
JOINTS = (0.02, 0.34, 0.67, 1.0)


def lead(u):
    return 0.26 - 0.56 * np.power(u, 1.6) + 0.08 * np.sin(math.pi * u)


def trail(u):
    chord = 0.6 * (1 - np.power(u, 2.2)) + 0.03
    saw = np.power((u * 4.2 + 0.15) % 1.0, 3)
    hooks = 0.08 * saw * C.smoothstep(0.15, 0.3, u) * (1 - 0.4 * u)
    return lead(u) - chord - hooks


def fin_up(u, v):
    return 0.014 + 0.05 * u - 0.12 * u * u + 0.018 * np.sin(math.pi * v) * (1 - 0.5 * u)


def fin_half_thickness(u, v):
    body = (0.032 * np.power(1 - u, 1.4) + 0.0035) * np.power(np.clip(np.sin(math.pi * np.clip(v, 0, 1)), 0, 1), 0.5)
    rod = 0.009 * (1 - u * 0.6) * (1 - C.smoothstep(0.0, 0.14, v))
    return np.maximum(body + rod, 0.007)


def fin_point(sx, u, v, side=0.0):
    f = lead(u) - v * (lead(u) - trail(u))
    up = fin_up(u, v) + side * fin_half_thickness(u, v)
    return P(sx * (FIN_X0 + FIN_SPAN * u), float(up), float(f))


def fin_uv(points):
    u = np.clip((np.abs(points[:, 0]) / M - FIN_X0) / FIN_SPAN, 0, 1)
    fwd = -points[:, 1] / M
    lo, hi = lead(u), trail(u)
    v = np.clip((lo - fwd) / np.maximum(lo - hi, 1e-4), 0, 1)
    return u, v


def fin_chain(side, sx, chain):
    names = [f"Fin_{side}_{chain}{i + 1}" for i in range(3)]
    pts = [fin_point(sx, j, CHAINS[chain]) for j in JOINTS]
    return names, pts


# ---- Aft fins: a smaller sickle pair behind the main fins ----------------------------------------------------
AFT_X0, AFT_SPAN, AFT_FWD = 0.1, 0.36, -0.3


def aft_lead(u):
    return AFT_FWD + 0.02 - 0.2 * np.power(u, 1.5)


def aft_trail(u):
    return aft_lead(u) - (0.17 * (1 - np.power(u, 1.8)) + 0.02)


def aft_point(sx, u, v, side=0.0):
    f = aft_lead(u) - v * (aft_lead(u) - aft_trail(u))
    th = (0.018 * (1 - u) + 0.004) * math.sqrt(max(0.0, math.sin(math.pi * min(max(v, 0.0), 1.0))))
    up = -0.005 + 0.02 * u + side * max(th, 0.0055)
    return P(sx * (AFT_X0 + AFT_SPAN * u), up, float(f))


def aft_uv(points):
    u = np.clip((np.abs(points[:, 0]) / M - AFT_X0) / AFT_SPAN, 0, 1)
    fwd = -points[:, 1] / M
    lo, hi = aft_lead(u), aft_trail(u)
    v = np.clip((lo - fwd) / np.maximum(lo - hi, 1e-4), 0, 1)
    return u, v


TAIL = [P(0, 0.0, -0.34), P(0, 0.006, -0.6), P(0, 0.0, -0.86), P(0, -0.014, -1.12), P(0, -0.028, -1.36),
        P(0, -0.04, -1.56)]
TAIL_BONES = [f"Tail_{i + 1}" for i in range(5)]


def pincer_points(sx):
    return [P(0.085 * sx, 0.0, 0.38), P(0.13 * sx, 0.005, 0.49), P(0.108 * sx, 0.01, 0.6), P(0.04 * sx, 0.002, 0.66)]


def horn_points(sx):
    """A charge horn: from the brow back over the body, rising and sweeping out."""
    return [P(0.055 * sx, 0.05, 0.36), P(0.09 * sx, 0.1, 0.22), P(0.13 * sx, 0.14, 0.02), P(0.16 * sx, 0.15, -0.16)]


def body_top(fwd):
    q = max(0.0, 1 - ((fwd + 0.02) / 0.38) ** 2)
    return 0.085 * q ** 0.5


# Hatch plates (one per side): x from the midline gap to the outer hinge, fwd range.
HATCH_X = (0.012, 0.15)
HATCH_FWD = (0.15, -0.22)


def hatch_surface(sx, lift):
    def fn(u, v):
        x = HATCH_X[0] + (HATCH_X[1] - HATCH_X[0]) * u
        f = HATCH_FWD[0] + (HATCH_FWD[1] - HATCH_FWD[0]) * v
        taper = math.sin(math.pi * v) ** 0.35
        # The plate follows the dome of the back, a gap above it.
        up = body_top(f) * (1 - 0.6 * (x / 0.2) ** 2) + 0.012 + lift * taper * (1 - 0.5 * u)
        return P(sx * x * (0.65 + 0.35 * taper), up, f)
    return fn


def build_parts():
    b = C.PartBuilder()
    k = K
    b.ellipsoid("Body", P(0, 0.0, -0.02), R(0.21, 0.38, 0.082), bone="Body", segments=(56, 28),
                style=H.band_plates(PAL, k, 5, height=0.004, glow=0.35, shingle=True))
    b.ellipsoid("Belly", P(0, -0.024, -0.03), R(0.195, 0.35, 0.058), bone="Body", segments=(48, 24),
                style=H.gill_belly(PAL, k, UNDER, slits=5, start=-0.36, spacing=0.12, width=0.03, side=(0.18, 0.8)))
    # The brood chamber: a glowing egg membrane in the back, under the hatches.
    b.ellipsoid("Brood", P(0, 0.03, -0.035), R(0.14, 0.2, 0.065), bone="Body", segments=(40, 20),
                style=H.membrane_sac(BROOD, k, bands=2, plate_line=1.5, cells=34, cell_size=0.16, glow=1.0, seed=73, plate_glow=0.0))

    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.6 * front), v.y * (1 + 0.15 * front), v.z * (1 - 0.45 * front)))
    b.ellipsoid("Head", P(0, 0.014, 0.35), R(0.13, 0.22, 0.052), bone="Head", segments=(48, 24), deform=head_deform,
                style=H.crest(PAL, k, ribs=9, height=0.004))
    for side, sx in SIDES:
        for i, (x, f) in enumerate(((0.044, 0.39), (0.067, 0.335), (0.09, 0.28))):
            b.ellipsoid("Eye", P(x * sx, 0.047 - 0.004 * i, f), R(0.018, 0.018, 0.013), bone="Head",
                        style=H.eye(PAL, k, 1.0), segments=(12, 6))
        pp = pincer_points(sx)
        b.tube("Pincer", pp, [R(0.036), R(0.032), R(0.02), R(0.002)], sides=10, chain=[f"Pincer_{side}"],
               style=H.blade(PAL, k, glow=1.0, teeth=4), flatten=0.45)
        barb = pp[1] + P(0.012 * sx, 0.004, 0.0)
        b.tube("PincerBarb", [barb, barb + P(0.03 * sx, 0.004, -0.02), barb + P(0.05 * sx, 0.0, -0.055)],
               [R(0.012), R(0.007), R(0.001)], sides=8, chain=[f"Pincer_{side}"], style=H.hook(PAL, k, glow=0.6))
        # Charge horns: chitin rods with glowing node rings, the storm organ.
        hp = horn_points(sx)
        b.tube("Horn", hp, [R(0.022), R(0.02), R(0.015), R(0.002)], sides=10, chain=["Head"],
               style=H.tube(PAL, k, PAL.side, PAL.dark, rings=(0.3, 0.55, 0.78), tip_glow=1.0, glow_from=0.86, rough=0.5))
        curve = C.catmull_rom([Vector(p) for p in hp], 6)
        for t in (0.3, 0.55, 0.78):
            c = curve[int(t * (len(curve) - 1))]
            b.ellipsoid("HornNode", c, (R(0.026 - 0.008 * t),) * 3, bone="Head", style=H.eye(PAL, k, 1.0), segments=(14, 7))
        for x, f in ((0.08, 0.1), (0.09, -0.12)):
            b.ellipsoid("Vent", P(x * sx, body_top(f) - 0.006, f), R(0.013, 0.042, 0.011), bone="Body",
                        style=H.eye(PAL, k, 0.5), segments=(14, 7))
        names = fin_chain(side, sx, "A")[0] + fin_chain(side, sx, "B")[0]
        fin_style = H.fin_sheet(PAL, k, fin_uv, UNDER, rays=7, chevrons=3, chevron_width=0.2, flush=FLUSH,
                                ray_height=0.006, dashed=True, lead_width=0.08)
        for name, upper, lower in (("Fin", 1.0, 0.0), ("FinUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: fin_point(sx, u, v, h),
                   lambda u, v, sx=sx, h=lower: fin_point(sx, u, v, h), 48, 18, chain=names, style=fin_style)
        b.ellipsoid("Shoulder", P(0.17 * sx, 0.004, 0.0), R(0.11, 0.25, 0.046), bone=f"Fin_{side}_A1", segments=(36, 18),
                    style=H.band_plates(PAL, k, 3, height=0.003, glow=0.25))
        b.parts[-1].chain = [f"Fin_{side}_A1", f"Fin_{side}_B1"]
        # Capacitor beads along the leading edge.
        for j, u in enumerate((0.16, 0.32, 0.48, 0.64, 0.8)):
            chain = names[0] if u < JOINTS[1] else names[1] if u < JOINTS[2] else names[2]
            c = fin_point(sx, u, 0.045, 0.6)
            b.ellipsoid("Bead", c, (R(0.022 * (1 - 0.4 * u)),) * 3, bone=chain, style=H.eye(PAL, k, 1.0), segments=(14, 7))
        tip = fin_point(sx, 1.0, 0.5)
        b.tube("Claw", [tip, tip + P(0.05 * sx, -0.03, -0.05), tip + P(0.055 * sx, -0.095, -0.02)],
               [R(0.019), R(0.012), R(0.001)], sides=8, chain=[f"Fin_{side}_A3", f"Fin_{side}_B3"], style=H.hook(PAL, k, glow=0.7))
        aft_style = H.fin_sheet(PAL, k, aft_uv, UNDER, rays=4, chevrons=1, chevron_width=0.25, flush=FLUSH,
                                ray_height=0.005, lead_width=0.12)
        for name, upper, lower in (("Aft", 1.0, 0.0), ("AftUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: aft_point(sx, u, v, h),
                   lambda u, v, sx=sx, h=lower: aft_point(sx, u, v, h), 20, 10, chain=[f"Aft_{side}"], style=aft_style)
        # The hatch over the brood chamber: an armoured plate floating just above the back (its own shell).
        b.slab("Hatch", hatch_surface(sx, 0.02), hatch_surface(sx, 0.0), 12, 16, chain=[f"Hatch_{side}"],
               style=H.band_plates(PAL, k, 3, height=0.005, glow=0.5, axis=1, shingle=True))
        b.parts[-1].frame = (C.Matrix.Translation(P(0.08 * sx, 0.07, -0.035)) @
                             C.Matrix.Diagonal((R(0.08), R(0.19), R(0.03), 1.0))).inverted()
        b.parts[-1].radii = (R(0.08), R(0.19), R(0.03))
    # The crown: three backward horns on the prow.
    for x, f, length in ((0.0, 0.3, 0.1), (-0.05, 0.25, 0.075), (0.05, 0.25, 0.075)):
        base = P(x, 0.04, f)
        b.tube("Crown", [base, base + P(x * 0.4, length * 0.8, -length * 0.3), base + P(x * 0.8, length * 0.95, -length * 0.95)],
               [R(0.02 + length * 0.1), R(0.011), R(0.001)], sides=10, chain=["Head"], style=H.hook(PAL, k, glow=0.9, glow_from=0.72))
    for f, length in ((-0.26, 0.11), (-0.33, 0.12)):
        base = P(0, body_top(f) - 0.012, f)
        b.tube("Hook", [base, base + P(0, length * 0.7, -length * 0.2), base + P(0, length * 0.9, -length * 0.85)],
               [R(0.02 + length * 0.12), R(0.011), R(0.001)], sides=10, chain=["Body"], style=H.hook(PAL, k, glow=0.8, glow_from=0.78))

    b.tube("Tail", TAIL, [R(0.055), R(0.036), R(0.027), R(0.02), R(0.015), R(0.011)], sides=12, chain=TAIL_BONES,
           style=H.tube(PAL, k, PAL.top * 0.85, PAL.dark, rings=(0.12, 0.25, 0.38, 0.51, 0.64, 0.77, 0.9), rough=0.6))
    for i in range(1, 5):
        scale = 1 - 0.14 * (i - 1)
        for sx in (-1, 1):
            base = TAIL[i] + P(0.013 * sx, 0.009, 0)
            b.tube("Barb", [base, base + P(0.034 * sx, 0.022, -0.02) * scale, base + P(0.058 * sx, 0.034, -0.065) * scale],
                   [R(0.013 * scale), R(0.008 * scale), R(0.001)], sides=8, chain=[TAIL_BONES[i]], style=H.hook(PAL, k, glow=0.6))
    end = TAIL[-1]
    b.tube("Prong", [end, end + P(0, 0.004, -0.08), end + P(0, 0.0, -0.18)], [R(0.025), R(0.017), R(0.001)], sides=10,
           chain=["Tail_5"], style=H.blade(PAL, k, glow=1.0), flatten=0.4)
    for sx in (-1, 1):
        b.tube("Prong", [end, end + P(0.05 * sx, 0.0, -0.045), end + P(0.09 * sx, 0.004, -0.13)], [R(0.021), R(0.014), R(0.001)],
               sides=10, chain=["Tail_5"], style=H.blade(PAL, k, glow=1.0), flatten=0.4)
        b.ellipsoid("TailNode", end + P(0.03 * sx, 0.004, -0.02), (R(0.02),) * 3, bone="Tail_5", style=H.eye(PAL, k, 1.0),
                    segments=(14, 7))
    return b


def bones():
    out = [
        ("Root", P(0, -0.5, 0), P(0, -0.5, 0.2), None, True),
        ("Body", P(0, 0, -0.3), P(0, 0, 0.17), "Root", True),
        ("Head", P(0, 0, 0.17), P(0, 0.01, 0.54), "Body", True),
    ]
    for side, sx in SIDES:
        pts = pincer_points(sx)
        out.append((f"Pincer_{side}", pts[0], pts[-1], "Head", True))
        for chain in CHAINS:
            names, pts = fin_chain(side, sx, chain)
            prev = "Body"
            for i, n in enumerate(names):
                out.append((n, pts[i], pts[i + 1], prev, True))
                prev = n
        out.append((f"Aft_{side}", aft_point(sx, 0.0, 0.4), aft_point(sx, 1.0, 0.4), "Body", True))
        # The hatch hinges along its outer edge, front to back.
        out.append((f"Hatch_{side}", P(HATCH_X[1] * 0.9 * sx, body_top(0.1) * 0.6 + 0.01, HATCH_FWD[0] - 0.02),
                    P(HATCH_X[1] * 0.9 * sx, body_top(-0.15) * 0.6 + 0.01, HATCH_FWD[1] + 0.02), "Body", True))
    prev = "Body"
    for i, n in enumerate(TAIL_BONES):
        out.append((n, TAIL[i], TAIL[i + 1], prev, True))
        prev = n
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs_for(loc=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, pincer=0.0, fins=None, sweep=0.0, tail_pitch=None,
              tail_yaw=None, aft=0.0, hatch=0.0):
    """As the Skyray's, plus aft: the aft fins' beat (rad, up positive) and hatch: how far the hatches open (rad)."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": loc if loc is not None else Vector()},
        "Head": {"rot": [(X, head)]},
    }
    for side, sx in SIDES:
        specs[f"Pincer_{side}"] = {"rot": [(Z, sx * pincer)]}
        for chain in CHAINS:
            angles = (fins or {}).get((side, chain), (0.0, 0.0, 0.0))
            names = fin_chain(side, sx, chain)[0]
            for i, n in enumerate(names):
                rot = [(Vector((0, -sx, 0)), angles[i])]
                if i == 0:
                    rot = [(Vector((0, 0, sx)), sweep)] + rot
                specs[n] = {"rot": rot}
        specs[f"Aft_{side}"] = {"rot": [(Vector((0, -sx, 0)), aft)]}
        # Opening lifts the inner (midline) edge: a turn about the hinge running front to back.
        specs[f"Hatch_{side}"] = {"rot": [(Vector((0, sx, 0)), hatch)]}
    tp = tail_pitch or [0.0] * 5
    ty = tail_yaw or [0.0] * 5
    for i, n in enumerate(TAIL_BONES):
        specs[n] = {"rot": [(Z, -ty[i]), (X, tp[i])]}
    return specs


def wave_fins(w, amp_a, amp_b, span_lag=0.55, chord_lag=0.9, offset=(0.0, 0.0, 0.0), harmonic=1):
    out = {}
    for side, _ in SIDES:
        out[(side, "A")] = [offset[i] + amp_a[i] * math.sin(harmonic * w - i * span_lag) for i in range(3)]
        out[(side, "B")] = [offset[i] + amp_b[i] * math.sin(harmonic * w - i * span_lag - chord_lag) for i in range(3)]
    return out


def s_(a, b, t):
    return C.smoothstep(a, b, np.array([t]))[0]


FLY_FRAMES = 56


def fly_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        # Slower and heavier than the Skyray (1.9 s per beat), the aft fins a quarter beat behind.
        fins = wave_fins(w, [d(13), d(11), d(10)], [d(15), d(13), d(12)])
        return specs_for(loc=Vector((0, 0, -R(0.02) * math.sin(w))), pitch=d(2) * math.cos(w), head=-d(2) * math.sin(w),
                         pincer=d(4) * math.sin(2 * w), fins=fins, aft=d(14) * math.sin(w - 1.6),
                         hatch=d(2) * max(0.0, math.sin(w)),
                         tail_pitch=[d(4.5) * math.sin(w - 1.3 - i * 0.75) for i in range(5)],
                         tail_yaw=[d(3) * math.sin(w - i * 0.9) for i in range(5)])
    return FLY_FRAMES, spec, True


def idle_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        fins = wave_fins(w, [d(3), d(4), d(6)], [d(4), d(6), d(8)], span_lag=0.8, harmonic=2)
        return specs_for(loc=Vector((0, 0, R(0.016) * math.sin(w))), roll=d(3) * math.sin(w + 0.5), yaw=d(3) * math.sin(w),
                         head=d(2) * math.sin(w + 1), pincer=d(5) + d(8) * max(0.0, math.sin(3 * w)) ** 4, fins=fins,
                         aft=d(6) * math.sin(2 * w - 1.2), hatch=d(4) * max(0.0, math.sin(w + 0.5)) ** 2,
                         tail_pitch=[d(3) * math.sin(2 * w - i * 0.8) for i in range(5)],
                         tail_yaw=[d(7) * math.sin(w - i * 0.6) for i in range(5)])
    return 72, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.4
        fins = {(s, "A"): [d(18) * k, d(9) * k, d(5) * k] for s, _ in SIDES}
        fins.update({(s, "B"): [d(24) * k, d(12) * k, d(9) * k] for s, _ in SIDES})
        return specs_for(loc=Vector((0, R(0.02) * k, R(0.03) * k)), pitch=-d(10) * k, roll=d(10) * k, head=-d(6) * k,
                         pincer=d(25) * max(k, 0.0), fins=fins, aft=d(15) * k, hatch=d(8) * max(k, 0.0),
                         tail_pitch=[d(8) * k * (1 - 0.1 * i) for i in range(5)], tail_yaw=[-d(7) * k for _ in range(5)])
    return 14, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        spin = s_(0.04, 1.0, t)
        crumple = s_(0.0, 0.45, t)
        flail = math.exp(-t * 5) * math.sin(t * 34)
        fins = {(sd, "A"): [d(34) * crumple + d(16) * flail, d(24) * crumple, d(22) * crumple] for sd, _ in SIDES}
        fins.update({(sd, "B"): [d(44) * crumple + d(20) * flail, d(30) * crumple, d(28) * crumple] for sd, _ in SIDES})
        return specs_for(roll=d(400) * spin, pitch=d(34) * crumple, head=d(14) * crumple,
                         pincer=d(30) * math.sin(math.pi * min(1.0, t * 2.5)), fins=fins, sweep=d(18) * crumple,
                         aft=d(30) * crumple, hatch=d(40) * s_(0.1, 0.4, t),
                         tail_pitch=[-d(14) * crumple + d(10) * flail for _ in range(5)],
                         tail_yaw=[d(10) * crumple * math.sin(i * 1.3) for i in range(5)])
    return 48, spec, False


RELEASE_FRAMES = 54
# The hatches are open widest and the body heaves: spawn the small flyers over the back (Body bone, about 0.25
# above it) here.
RELEASE_FRAME = 30


def release_spec(poser):
    d = math.radians

    def spec(t, f):
        rear = s_(0.0, 0.3, t) * (1 - s_(0.75, 1.0, t))
        opening = s_(0.18, 0.45, t) * (1 - s_(0.66, 0.82, t))
        heave = math.sin(math.pi * s_(0.42, 0.68, t))
        snap = math.exp(-max(0.0, t - 0.8) * 30) * math.sin(max(0.0, t - 0.8) * 60) * (1 if t > 0.8 else 0)
        # She rears up, holds the fins high, opens the hatches and heaves the brood out; the hatches snap shut.
        fins = {(sd, "A"): [d(12) * rear - d(8) * heave, d(8) * rear, d(10) * rear] for sd, _ in SIDES}
        fins.update({(sd, "B"): [d(16) * rear - d(10) * heave, d(10) * rear, d(12) * rear] for sd, _ in SIDES})
        return specs_for(loc=Vector((0, 0, R(0.06) * rear - R(0.05) * heave)), pitch=-d(16) * rear, head=d(8) * rear,
                         pincer=d(26) * rear, fins=fins, aft=-d(16) * rear + d(10) * heave,
                         hatch=d(80) * opening + d(6) * snap,
                         tail_pitch=[-d(9) * rear + d(6) * heave for _ in range(5)],
                         tail_yaw=[d(5) * heave * math.sin(t * 20 - i) for i in range(5)])
    return RELEASE_FRAMES, spec, False


CLIPS = {"Fly": fly_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Release": release_spec}
# LOD1: trailing fin chains fold into the leading ones, pincers into the head, the tail into three bones. 16 bones.
LOD_DROP = {"Pincer_L": "Head", "Pincer_R": "Head", "Tail_2": "Tail_1", "Tail_4": "Tail_3",
            **{f"Fin_{s}_B{i}": f"Fin_{s}_A{i}" for s, _ in SIDES for i in (1, 2, 3)}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_2"), "Tail_3": ("Tail_3", "Tail_4")}
TARGET = (0.0, 0.35, 0.0)
CLOSE_LIFT = 1.5
RIGID = {"Hatch"}


def main():
    C.reset_scene()
    out = C.preview_dir()
    variant = PALETTE_NAME != "cool"
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0058, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.18,
                               extrusion=0.03, ray=0.08, emission_strength=2.5,
                               extra=H.weathering(K, scratches=0.6, dust=0.3, markings=PAL.dark, marking_amount=0.7, seed=77),
                               specular=0.3, mirror=True, k=DETAIL_K, families=FAMILIES, low_size=None if variant else LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=1)
    poser = B.ScalePoser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Fly"]
    common = dict(target=TARGET, distance=6.4, game_scale=GAME_SCALE, lift=CLOSE_LIFT, game_lift=AIR_HEIGHT,
                  game_target=(0, 0.3, 0.9), rig_scale=2.6)
    if variant:
        if not C.cli_args().get("norender"):
            B.variant_previews("stormmatriarch", PALETTE_NAME, arm, asset.low, actions, out, move=("Fly", 8), **common)
        return
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = B.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    C.write_report(NAME, {"asset": NAME, "base_species": "Skyray", "palette": PALETTE_NAME, "game_tris": asset.low_tris,
                          "source_tris": asset.high_tris, "bones": len(arm.data.bones), "texture_size": TEX,
                          "low_texture_size": LOW_TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                          "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "release_frame": RELEASE_FRAME,
                          "release_point": "Body bone, 0.25 above the back", "fly_beat_seconds": round(FLY_FRAMES / 30, 2),
                          "pivot": "body centre (flight height)", "unity_scale": GAME_SCALE,
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if C.cli_args().get("norender"):
        detail(norender=True)
        return
    B.boss_previews(
        "stormmatriarch", arm, asset.low, actions, lod, out, asset, idle="Idle",
        base=dict(asset="Skyray", scale=SKYRAY_SCALE, clip="Fly", frame=8), move=("Fly", 8), power=("Release", RELEASE_FRAME),
        strips=[("Fly", "front", range(0, FLY_FRAMES, 7)), ("Release", "side", range(0, RELEASE_FRAMES, 7)),
                ("Death", "close", range(0, 49, 7))],
        macros=[("fin", P(-0.6, 0.0, -0.05), 2.2, -40, 40), ("head", P(0.0, 0.05, 0.42), 1.9, -30, 28),
                ("hatch", P(0.06, 0.1, -0.03), 1.9, -150, 45)],
        minions=[(-2.0, 1.0, 0.25, 0), (2.0, 0.7, -0.2, 20), (-1.4, -0.8, 0.1, 31)],
        compare_span=2.6, compare_zoom=1.7, compare_distance=2.0, extra_shots=[("release_top", "top_front", "Release", RELEASE_FRAME), ("under", "front", "Fly", 30)], **common)
    detail()


def detail(norender=False):
    """Round 45: the close-up detail (eyes, teeth, claws) as extra parts, exported again with the body."""
    import stormmatriarch_detail
    stormmatriarch_detail.run(norender)


if __name__ == "__main__":
    main()
