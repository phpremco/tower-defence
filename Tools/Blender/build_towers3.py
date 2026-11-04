"""Round 18: the five continent-3 combo hybrids from the script (Docs/STORY.md, continent 3; see
ArtSource/DESIGN_NOTES.md, round 18). Each hybrid merges two parent towers and must show both:

- Shatter         = Cryo Turret + Railgun     (rails on a frosted coolant cylinder, Cryo canisters and vane crown)
- ArcLaser        = Laser + Tesla Coil        (fork yoke and lens, Tesla coils round the emitter, arc prongs)
- CorrosiveBlast  = Cannon + Acid Sprayer     (wedge hull and box brake, the acid tank and sight glass on the back)
- StormFlak       = Flak + Tesla Coil         (elevated twin guns and shield, coil jackets, a coil spire on the back)
- ThermalShock    = Cryo Turret + Lava Ball   (frosted cylinder gun, the plated thermite ball as a heat core)

Same kit and rig as rounds 7 and 11 (the pad, the rank plate and the stars from build_towers.py; pieces, atlas and
self-bake from tower_kit.py; the round-11 helpers from build_towers2.py, all imported unchanged). Hybrids have three
levels (T1..T3, no A/B). Per tower `Exports/Tower_<Name>/Tower_<Name>_T1..T3.fbx` (LOD0 + LOD1 skinned to one rig,
with the clips), one 1024 map set (+ `Low/` 512) and `Tower_<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_towers3.py -- [--only Shatter,ArcLaser]
     [--preview <dir>] [--norender] [--nobake]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_towers as bt  # noqa: E402
import build_towers2 as t2  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from build_towers import FWD, UP, V, XAX, ring_base  # noqa: E402
from build_towers2 import ball, coil_stack, crysium_cell, rod, sphere_prof, square_collar  # noqa: E402

# Glows: saturated (round-11 lesson: pale glows burn out under bloom). Each hybrid keeps its parents' hues.
GL3 = {
    "shatter": (0.45, 0.8, 1.0),     # the Railgun's plasma channel, a touch icier
    "frost": t2.GL["frost"],         # the Cryo's frost rings and caps
    "volt": (0.4, 0.3, 1.0),         # Tesla coils, bluer than the Tesla (its violet sat close to the aliens' pink)
    "lens": tk.GLOW["cyan"],         # the Laser's lens
    "acid": t2.GL["acid"],
    "amber": tk.GLOW["amber"],
    "lava": (1.0, 0.34, 0.05),       # the Lava Ball's molten core (the weapon itself; the shell stays ours)
    "crysium": t2.GL["crysium"],
}
PLAIN = t2.PLAIN
FROST = t2.FROST
D40, U40 = t2.D40, t2.U40
YAX = V(0, 1, 0)


def G3(name, strength=1.0, lens=0.0):
    return tk.glow(GL3[name], strength, lens)


class Spec3(t2.Spec2):
    variants = ["T1", "T2", "T3"]

    def __init__(self):
        super().__init__()
        self.accent = GL3[self.accent_name]


def thermite_patch(corners, r_out, r_in, n=3):
    """A curved Lava Ball shell plate over a spherical triangle (corners: unit vectors), as build_gadgets2 does."""
    bm = bmesh.new()
    A, B, Cc = (Vector(c) for c in corners)

    def P(i, j, r):
        a, b, c = (n - i - j) / n, i / n, j / n
        return (A * a + B * b + Cc * c).normalized() * r
    outer, inner = {}, {}
    for i in range(n + 1):
        for j in range(n + 1 - i):
            outer[i, j] = bm.verts.new(P(i, j, r_out))
            inner[i, j] = bm.verts.new(P(i, j, r_in))
    tris = []
    for i in range(n):
        for j in range(n - i):
            tris.append(((i, j), (i + 1, j), (i, j + 1)))
            if i + j < n - 1:
                tris.append(((i + 1, j), (i + 1, j + 1), (i, j + 1)))
    for t in tris:
        bm.faces.new([outer[x] for x in t])
        bm.faces.new([inner[x] for x in reversed(t)])
    loop = [(i, 0) for i in range(n)] + [(n - j, j) for j in range(n)] + [(0, n - j) for j in range(n)]
    for a, b in zip(loop, loop[1:] + loop[:1]):
        bm.faces.new((outer[b], outer[a], inner[a], inner[b]))
    return bm


def thermite_ball(k, c, R, bone, on, bands=True):
    """The Lava Ball in miniature: a molten core under eight white/blue plates with gunmetal bands and hazard hubs."""
    k.lathe("ThermCore", sphere_prof(R * 0.86, 6), tk.frame(c), G3("lava", 1.1, lens=R * 0.86), bone, on, sides=12, bevel=0.0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                corners = [V(sx, 0, 0), V(0, sy, 0), V(0, 0, sz)]
                mid = V(sx, sy, sz).normalized()
                inset = [(cc * 0.78 + mid * 0.22).normalized() for cc in corners]
                look = PLAIN if sz > 0 else tk.BLUE.but(panel=0, bolts=False)
                k._add("ThermPlate", (lambda ins=inset: thermite_patch(ins, R, R - 0.012 - R * 0.06)), 0.0, tk.frame(c), look, bone, on)
    if bands:
        for ax in (XAX, YAX, UP):
            k.torus("ThermBand", c, ax, R - 0.002, R * 0.07, tk.GUNMETAL, bone, on, seg=16, sides=3, lod=False)
    for d in (XAX, -XAX, YAX, -YAX, UP):
        p = c + d * (R + 0.001)
        k.cyl("ThermHub", p - d * 0.01, p + d * 0.008, R * 0.17, R * 0.15, look=tk.HAZARD, bone=bone, on=on, sides=6, bevel=0.0, lod=False)


def arc_prong(k, base, tip, bone, on, look=None, tip_r=0.011):
    """A short gunmetal arc pin with a glowing ball at its tip (the Tesla's prongs, in small)."""
    k.cyl("ArcPin", base, tip, 0.006, 0.0045, look=look or tk.GUNMETAL, bone=bone, on=on, sides=5, bevel=0.0, lod=False)
    ball(k, "ArcTip", tip, tip_r, G3("volt", 1.7), bone, on, sides=6, n=3, lod=False)


def horizontal_coils(k, on, start, axis, n, step, r, look, bone, disc_r=None, tube=0.011):
    """Tesla coil rings stacked along a horizontal axis with dark insulator discs between (the Tesla laid down)."""
    for i in range(n):
        c = start + axis * (i * step)
        k.torus("Coil", c, axis, r, tube, look, bone, on, seg=16, sides=4)
        if disc_r:
            d = c + axis * (step / 2)
            if i < n - 1:
                k.cyl("Insulator", d - axis * 0.005, d + axis * 0.005, disc_r, look=tk.DARK, bone=bone, on=on, sides=14, bevel=0.0, lod=False)


# -------------------------------------------------------------------------------------------------------------
# Shatter = Cryo + Railgun
# -------------------------------------------------------------------------------------------------------------

class Shatter(Spec3):
    """The Railgun's rails and plasma channel on the Cryo's frosted coolant cylinder, Cryo canisters and vane crown."""
    name = "Shatter"
    accent_name = "shatter"
    recoil = 0.06
    pitch_at = V(0, -0.04, 0.4)
    smoke_at = V(0, 0.1, 0.45)
    close_z, close_dist, close_az, macro_z = 0.4, 3.1, 150, 0.45
    strip_clip = "Fire_1"
    LEN = {"T1": 0.46, "T2": 0.56, "T3": 0.66}
    COLLARS = {"T1": 3, "T2": 4, "T3": 5}
    CANS = {"T1": [(-1, 0.06)], "T2": [(-1, 0.07), (1, 0.07), (-1, -0.03)], "T3": [(-1, 0.09), (-1, -0.01), (1, 0.09)]}

    def pieces(self, k):
        ring_base(k, 0.17)
        z = 0.4
        for v in self.variants:
            on = [v]
            w = 0.32 if v == "T3" else 0.3
            # The Railgun's hull, glacis and capacitor block on the back.
            k.block("Hull", (w, 0.34, 0.11), tk.frame((0, 0.03, 0.32)), tk.BLUE, "Yaw", on, taper=(0.88, 0.84), shift=(0, 0.012), bevel=0.01)
            k.block("TopArmour", (w * 0.82, 0.26, 0.024), tk.frame((0, 0.05, 0.386)), FROST.but(panel=0.08, bolts=True), "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (w * 0.86, 0.022, 0.09), tk.frame((0, -0.125, 0.318), rot=(-40, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            cw = 0.22 if v == "T1" else 0.24
            k.block("CapBlock", (cw, 0.1, 0.09), tk.frame((0, 0.165, 0.44)), tk.WHITE.but(panel=0.07), "Yaw", on, bevel=0.008)
            for i in range(3):
                k.block("CapSlit", (0.05, 0.006, 0.012), tk.frame(((i - 1) * 0.065, 0.216, 0.445)), self.glow(1.6), "Yaw", on, bevel=0.0)
            k.block("CapFoot", (cw * 0.9, 0.08, 0.03), tk.frame((0, 0.165, 0.39)), tk.GUNMETAL, "Yaw", on)
            # The Cryo's coolant canisters on the hull sides (1, 3, 4), piped into the coolant cylinder.
            for s, y in self.CANS[v]:
                b = V(s * 0.2, y, 0.28)
                k.cyl("Canister", b, b + UP * 0.16, 0.03, look=FROST.but(rings=0.045), bone="Yaw", on=on, sides=12)
                k.lathe("CanisterCap", [(0, 0), (0.03, 0), (0.024, 0.02), (0, 0.028)], tk.frame(b + UP * 0.16), G3("frost", 1.6), "Yaw", on, sides=12,
                        bevel=0.0)
                rod(k, "CanPipe", b + UP * 0.12 - V(s * 0.02, 0, 0), V(s * 0.1, y * 0.5, 0.4), 0.008, tk.RUBBER, "Yaw", on, lod=False)
        # T3: the Railgun's heavy drum with the hazard band at the back.
        k.cyl("Drum", V(-0.13, 0.225, 0.47), V(0.13, 0.225, 0.47), 0.066, look=tk.GUNMETAL.but(rings=0.04), bone="Yaw", on=["T3"], sides=16)
        k.torus("DrumHazard", V(0, 0.225, 0.47), XAX, 0.067, 0.01, tk.HAZARD, "Yaw", ["T3"], seg=16, sides=4)

        for v, half in (("T1", 0.085), ("T2", 0.09), ("T3", 0.1)):
            k.block("Breech", (half * 2, 0.16, 0.085), tk.frame((0, -0.04, z)), tk.GUNMETAL, "Pitch", [v], bevel=0.008)
            k.cyl("Trunnion", (-half - 0.025, -0.03, z), (half + 0.025, -0.03, z), 0.03, look=tk.DARK, bone="Pitch", on=[v], sides=12)
            k.block("BreechTop", (half * 1.6, 0.12, 0.012), tk.frame((0, -0.04, z + 0.047)), PLAIN, "Pitch", [v])

        for v in self.variants:
            self.gun(k, v, z)

    def gun(self, k, v, z):
        on = [v]
        L = self.LEN[v]
        b0 = V(0, -0.11, z)
        mid = b0 + FWD * (L / 2)
        # The Cryo's frosted coolant cylinder slung under the rails for the rear 55%, with glowing frost rings.
        cl, cr = L * 0.66, 0.072 + 0.006 * (v == "T3")
        cz = V(0, 0, -0.05)
        k.lathe("Coolant", [(0, 0), (cr * 0.6, 0), (cr, 0.025), (cr, cl - 0.02), (cr * 0.7, cl), (0, cl)], tk.frame(b0 + cz, z=FWD), FROST, "Barrel_1",
                on, sides=14)
        for i in range(2 if v == "T1" else 3):
            k.torus("FrostRing", b0 + cz + FWD * (0.05 + i * (cl - 0.09) / max(1, (1 if v == "T1" else 2))), FWD, cr + 0.003, 0.0075, G3("frost", 1.6),
                    "Barrel_1", on, seg=16, sides=4)
        # The Railgun's rails with the plasma channel level with their tops.
        for s in (-1, 1):
            k.block("Rail", (0.022, L, 0.05), tk.frame(mid + V(s * 0.026, 0, 0.004)), tk.STEEL.but(panel=0.0), "Barrel_1", on, bevel=0.004)
            k.block("RailTip", (0.024, 0.03, 0.054), tk.frame(b0 + FWD * (L - 0.012) + V(s * 0.026, 0, 0.004)), tk.YELLOW, "Barrel_1", on, bevel=0.003)
        k.block("Channel", (0.016, L * 0.97, 0.034), tk.frame(mid + V(0, 0, 0.014)), self.glow(1.5), "Barrel_1", on, bevel=0.0)
        n = self.COLLARS[v]
        for i in range(n):
            c = b0 + FWD * (0.06 + i * (L - 0.13) / max(n - 1, 1)) + V(0, 0, 0.004)
            square_collar(k, "Collar", c, FWD, 0.059, 0.074, 0.026, tk.GUNMETAL, "Barrel_1", on)
            k.block("CollarStud", (0.016, 0.014, 0.008), tk.frame(c + V(0, 0, 0.054)), self.glow(1.7), "Barrel_1", on, bevel=0.0, lod=False)
        # The Cryo's vane flaps as the muzzle crown (four on T1/T2, larger on T3) instead of the Railgun's crown.
        tip = b0 + FWD * (L - 0.005) + V(0, 0, 0.004)
        vl = 0.075 if v != "T3" else 0.1
        for a in (45, 135, 225, 315):
            dd = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
            c = tip + dd * 0.07
            m = tk.frame(c, z=dd, y=FWD) @ tk.frame(rot=(-25, 0, 0))
            k.block("Vane", (0.05, vl, 0.01), m, PLAIN, "Barrel_1", on, bevel=0.002)
            k.block("VaneTip", (0.052, 0.014, 0.012), tk.frame(c + FWD * (vl * 0.45) + dd * (vl * 0.2), z=dd, y=FWD) @ tk.frame(rot=(-25, 0, 0)),
                    G3("frost", 1.5), "Barrel_1", on, bevel=0.0, lod=False)
        if v in ("T2", "T3"):
            k.block("Brace", (0.05, L * 0.45, 0.03), tk.frame(b0 + FWD * (L * 0.7) - V(0, 0, 0.04)), tk.GUNMETAL, "Barrel_1", on)
        if v == "T3":
            # Frost fins along the top of the coolant cylinder, between the rails' feet.
            for i in range(5):
                c = b0 + FWD * (0.05 + i * 0.05) + V(0, 0, 0.06)
                k.block("FrostFin", (0.012, 0.03, 0.05 - abs(i - 2) * 0.008), tk.frame(c), PLAIN, "Barrel_1", on, bevel=0.002)
        self.barrel(v, b0, FWD, L + 0.01)

    def more_clips(self, arm, v):
        def charge(t, f):
            e = bt.ease(t)
            return {"Barrel_1": {"loc": FWD * (-0.02 * e)}}
        return [bk.clip(arm, "ChargeUp", 24, charge)]


# -------------------------------------------------------------------------------------------------------------
# Arc Laser = Laser + Tesla Coil
# -------------------------------------------------------------------------------------------------------------

class ArcLaser(Spec3):
    """The Laser's fork yoke and white emitter with its lens, Tesla coils stacked round the emitter, caged Crysium
    cells on the yoke, and a ring of arc prongs round the lens where the beam jumps (sockets Arc_n)."""
    name = "ArcLaser"
    accent_name = "volt"
    recoil = 0.0
    pitch_at = V(0, 0, 0.43)
    smoke_at = V(0, 0.1, 0.5)
    strip_clip = "ChargeUp"
    strip_frames = (0, 6, 12, 18, 24)
    COILS = {"T1": 2, "T2": 3, "T3": 4}
    PRONGS = {"T1": 3, "T2": 4, "T3": 6}

    def pieces(self, k):
        ring_base(k, 0.15)
        z = 0.43
        k.block("YokeBase", (0.34, 0.18, 0.06), tk.frame((0, 0, 0.3)), tk.BLUE.but(panel=0.08), "Yaw", taper=(0.95, 0.9))
        for s in (-1, 1):
            k.block("Arm", (0.05, 0.13, 0.19), tk.frame((s * 0.15, 0, 0.405)), tk.BLUE.but(panel=0.07), "Yaw", taper=(1.0, 0.8))
            k.block("ArmCap", (0.056, 0.11, 0.022), tk.frame((s * 0.15, 0, 0.505)), tk.WHITE.but(panel=0), "Yaw")
            k.cyl("TrunnionStub", (s * 0.122, 0, z), (s * 0.18, 0, z), 0.032, look=tk.GUNMETAL, bone="Pitch", sides=12)
        # The Tesla's caged Crysium cells on the yoke: one on T1 (left), both from T2.
        for v in self.variants:
            for s in ((-1,) if v == "T1" else (-1, 1)):
                crysium_cell(k, [v], V(s * 0.215, 0.02, 0.27), "Yaw", height=0.1, glow_name="crysium")
                rod(k, "Conduit", V(s * 0.215, 0.02, 0.4), V(s * 0.175, 0.0, 0.48), 0.01, tk.RUBBER, "Yaw", [v], sides=8, lod=False)
        rear = V(0, 0.15, z)
        self.prongs = {}
        for v, lens_r, house_r, body_len in (("T1", 0.072, 0.094, 0.27), ("T2", 0.09, 0.115, 0.28), ("T3", 0.11, 0.138, 0.3)):
            self.emitter(k, v, rear, lens_r, house_r, body_len)

    def emitter(self, k, v, rear, lens_r, house_r, body_len):
        on = [v]
        k.cyl("RearCap", rear + V(0, 0.01, 0), rear + FWD * 0.04, 0.058, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=16)
        k.lathe("Body", [(0, 0), (0.068, 0), (0.076, 0.02), (0.076, body_len - 0.03), (0.07, body_len), (0, body_len)],
                tk.frame(rear + FWD * 0.03, z=FWD), tk.WHITE.but(rings=0.0, panel=0.07), "Barrel_1", on, sides=18)
        # The Tesla's coils laid round the emitter body, on dark insulator discs.
        n = self.COILS[v]
        step = (body_len - 0.1) / max(n - 1, 1)
        horizontal_coils(k, on, rear + FWD * 0.09, FWD, n, step, 0.09, G3("volt", 1.0), "Barrel_1", disc_r=0.088, tube=0.012)
        front = rear + FWD * (0.03 + body_len)
        k.lathe("Housing", [(0, 0), (house_r * 0.8, 0), (house_r, 0.02), (house_r, 0.045), (lens_r * 1.05, 0.05), (0, 0.05)],
                tk.frame(front, z=FWD), tk.DARK, "Barrel_1", on, sides=20)
        k.lathe("Lens", [(0, 0), (lens_r, 0), (lens_r * 0.92, 0.012), (lens_r * 0.6, 0.022), (0, 0.027)],
                tk.frame(front + FWD * 0.04, z=FWD), G3("lens", 1.0, lens=lens_r), "Barrel_1", on, sides=20, bevel=0.0)
        # Arc prongs round the lens on their own bones (they close in during ChargeUp, as the Laser's do); each tip
        # is an Arc_n socket where the jumping beam's VFX leaves the tower.
        count = self.PRONGS[v]
        length = {"T1": 0.08, "T2": 0.1, "T3": 0.12}[v]
        spread = 1.0 if v != "T3" else 1.55   # T3: a wide halo, like the Tesla T3A crown
        bones, socks = [], []
        if v == "T3":
            k.torus("Halo", front + FWD * 0.035, FWD, house_r * spread + 0.012, 0.007, G3("volt", 1.3), "Barrel_1", on, seg=28, sides=3)
        for i in range(count):
            a = math.radians(90 + 360 * i / count)
            radial = V(math.cos(a), 0, math.sin(a))
            base = front + FWD * 0.03 + radial * (house_r * spread + 0.006)
            tip = base + FWD * length + radial * (0.02 if v == "T3" else -length * 0.18)
            name = f"Prong_{i + 1}"
            k.block("Prong", (0.02, 0.014, (tip - base).length), tk.frame((base + tip) / 2, z=tip - base, y=radial), PLAIN, name, on, bevel=0.003)
            ball(k, "ProngTip", tip, 0.014, G3("volt", 1.7), name, on, sides=8, n=3)
            bones.append(tk.Bone(name, base, tip - base, "Barrel_1", length=0.04))
            socks.append(tk.Bone(f"Arc_{i + 1}", tip + (tip - base).normalized() * 0.012, tip - base, name, deform=False, length=0.03))
            self.prongs.setdefault(v, []).append((name, radial))
        self.extra_bones[v] = bones + socks
        if v == "T3":
            # A small Tesla discharge ball in a cage on the emitter's back, the Tesla's crown in small.
            top = rear + FWD * 0.06 + UP * 0.14
            k.cyl("SpireColumn", rear + FWD * 0.06 + UP * 0.07, top - UP * 0.03, 0.016, 0.012, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=8)
            ball(k, "SpireBall", top, 0.03, G3("volt", 1.4, lens=0.03), "Barrel_1", on, sides=10, n=4)
            k.torus("SpireCage", top, XAX, 0.038, 0.004, tk.GUNMETAL, "Barrel_1", on, seg=10, sides=3, lod=False)
            k.torus("SpireCage", top, YAX, 0.038, 0.004, tk.GUNMETAL, "Barrel_1", on, seg=10, sides=3, lod=False)
        self.barrel(v, rear, FWD, 0.03 + body_len + 0.07)

    def clips(self, arm, v):
        prongs = self.prongs.get(v, [])

        def close(amount):
            return {name: {"rot": (radial.cross(FWD).normalized(), -0.25 * amount)} for name, radial in prongs}

        def charge(t, f):
            e = bt.ease(t)
            spec = close(e)
            spec["Barrel_1"] = {"loc": FWD * (0.018 * e)}
            return spec

        def beam(t, f):
            # The prongs chatter (each its own phase) while the beam jumps.
            spec = {name: {"rot": (radial.cross(FWD).normalized(), -0.25 * (1.0 + 0.12 * math.sin(2 * math.pi * 3 * t + i)))}
                    for i, (name, radial) in enumerate(prongs)}
            spec["Barrel_1"] = {"loc": FWD * (0.018 + 0.002 * math.sin(2 * math.pi * 3 * t))}
            return spec
        return [bk.clip(arm, "IdleSweep", 120, self.sweep, loop=True), bk.clip(arm, "ChargeUp", 24, charge), bk.clip(arm, "Beam", 12, beam, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Corrosive Blast = Cannon + Acid Sprayer
# -------------------------------------------------------------------------------------------------------------

class CorrosiveBlast(Spec3):
    """The Cannon's wedge hull, skirts, thick barrel and box brake fed by the Acid Sprayer's tank on the back
    (sight glass and glowing level band towards the camera, the sloshing meniscus on `Slosh`)."""
    name = "CorrosiveBlast"
    accent_name = "acid"
    recoil = 0.07
    pitch_at = V(0, -0.12, 0.32)
    smoke_at = V(0, 0.1, 0.42)
    strip_clip = "Fire_1"

    def pieces(self, k):
        ring_base(k, 0.2)
        z = 0.32
        for v in self.variants:
            on = [v]
            wide = 1.06 if v == "T3" else 1.0
            k.block("Hull", (0.4 * wide, 0.38, 0.12), tk.frame((0, 0.04, 0.305)), tk.BLUE, "Yaw", on, taper=(0.88, 0.78), shift=(0, 0.03), bevel=0.012)
            k.block("TopArmour", (0.33 * wide, 0.27, 0.026), tk.frame((0, 0.07, 0.376)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (0.36 * wide, 0.022, 0.13), tk.frame((0, -0.13, 0.3), rot=(-52, 0, 0)), tk.WHITE.but(panel=0.09), "Yaw", on)
            k.block("Slit", (0.07, 0.008, 0.012), tk.frame((0.1, -0.12, 0.345), rot=(-52, 0, 0)), self.glow(1.5), "Yaw", on, bevel=0.002)
            for sx in (-1, 1):
                k.block("SideLight", (0.012, 0.16, 0.014), tk.frame((sx * 0.19 * wide, 0.05, 0.33), rot=(0, sx * 12, 0)), self.glow(1.3), "Yaw", on,
                        bevel=0.002)
            # The Acid Sprayer's gauge on the deck.
            k.lathe("Gauge", [(0, 0.39), (0.026, 0.39), (0.026, 0.406), (0, 0.406)], tk.frame((0.1, -0.02, 0)), tk.GUNMETAL, "Yaw", on, sides=12)
            k.cyl("GaugeFace", (0.1, -0.02, 0.405), (0.1, -0.02, 0.409), 0.02, look=self.glow(1.3, lens=0.02), bone="Yaw", on=on, sides=12, bevel=0.0)
        for v in ("T2", "T3"):
            for s in (-1, 1):
                k.block("Skirt", (0.024, 0.36, 0.12), tk.frame((s * 0.225, 0.04, 0.29), rot=(0, s * 20, 0)), tk.WHITE.but(panel=0.09), "Yaw", [v])
                k.block("SkirtStripe", (0.026, 0.3, 0.022), tk.frame((s * 0.245, 0.04, 0.245), rot=(0, s * 20, 0)), tk.HAZARD, "Yaw", [v], bevel=0.002)
        self.slosh = {}
        self.tank(k, "T1", 0.0, 0.088, 0.18)
        self.tank(k, "T2", -0.085, 0.07, 0.18)
        self.tank(k, "T2", 0.085, 0.07, 0.18)
        k.block("Manifold", (0.22, 0.05, 0.04), tk.frame((0, 0.12, 0.4)), tk.GUNMETAL, "Yaw", ["T2"])
        self.sphere(k, "T3")
        # The Cannon's shell rack holds acid shells (glowing caps) on T3.
        k.block("Rack", (0.08, 0.2, 0.07), tk.frame((-0.17, 0.12, 0.38)), tk.GUNMETAL, "Yaw", ["T3"])
        for i in range(2):
            y = 0.08 + i * 0.07
            k.cyl("Shell", (-0.17, y, 0.41), (-0.17, y, 0.46), 0.018, 0.013, look=tk.YELLOW.but(fam="metal", rough=0.35), bone="Yaw", on=["T3"], sides=10)
            k.cyl("ShellTip", (-0.17, y, 0.46), (-0.17, y, 0.475), 0.013, 0.004, look=self.glow(1.6), bone="Yaw", on=["T3"], sides=8, bevel=0.0, lod=False)

        for v, w in (("T1", 0.17), ("T2", 0.18), ("T3", 0.28)):
            k.block("Mantlet", (w, 0.09, 0.13), tk.frame((0, -0.155, z)), tk.GUNMETAL, "Pitch", [v], bevel=0.012)
            k.cyl("Trunnion", (-w / 2 - 0.02, -0.12, z), (w / 2 + 0.02, -0.12, z), 0.04, look=tk.DARK, bone="Pitch", on=[v], sides=12)
            # The acid feed: a hose from the tank over the hull into the mantlet's top.
            rod(k, "FeedHose", V(0, 0.06, 0.4), V(0, -0.14, z + 0.07), 0.014, tk.RUBBER, "Pitch", [v], sides=8)
        self.gun(k, "T1", 0.0, 0.042, 0.26, "Barrel_1")
        self.gun(k, "T2", 0.0, 0.045, 0.31, "Barrel_1", extractor=True)
        self.gun(k, "T3", -0.075, 0.038, 0.31, "Barrel_1", extractor=True)
        self.gun(k, "T3", 0.075, 0.038, 0.31, "Barrel_2", extractor=True)
        for v, levels in self.slosh.items():
            c = sum(levels, Vector()) / len(levels)
            self.extra_bones[v] = [tk.Bone("Slosh", c, UP, "Yaw", length=0.03)]

    def tank(self, k, v, x, r, h, y=0.12):
        on = [v]
        z0 = 0.39
        k.lathe("Tank", [(0, z0), (r * 0.75, z0), (r, z0 + 0.03), (r, z0 + h - 0.035), (r * 0.72, z0 + h), (0, z0 + h + 0.012)],
                tk.frame((x, y, 0)), tk.WHITE.but(panel=0, bolts=False, rings=0.045), "Yaw", on, sides=16)
        k.lathe("TankBand", [(0, z0 + h * 0.2), (r + 0.004, z0 + h * 0.2), (r + 0.004, z0 + h * 0.34), (0, z0 + h * 0.34)], tk.frame((x, y, 0)),
                tk.BLUE.but(panel=0), "Yaw", on, sides=16, bevel=0.002)
        k.cyl("TankCap", (x, y, z0 + h + 0.005), (x, y, z0 + h + 0.03), 0.02, look=tk.YELLOW, bone="Yaw", on=on, sides=10, lod=False)
        gz = z0 + h * 0.6
        n = V(0, 1, 0)
        k.block("GlassFrame", (0.04, h * 0.62, 0.012), tk.frame(V(x, y + r - 0.001, gz), z=n), tk.DARK, "Yaw", on, bevel=0.002)
        k.block("Glass", (0.026, h * 0.54, 0.014), tk.frame(V(x, y + r + 0.001, gz), z=n), self.glow(0.95), "Yaw", on, bevel=0.0)
        k.torus("AcidBand", V(x, y, gz - h * 0.18), UP, r + 0.003, 0.008, self.glow(1.2), "Yaw", on, seg=14, sides=4)
        level = V(x, y + r + 0.004, gz + h * 0.05)
        k.block("Meniscus", (0.03, 0.012, 0.012), tk.frame(level, z=n), self.glow(2.0), "Slosh", on, bevel=0.0)
        self.slosh.setdefault(v, []).append(level)

    def sphere(self, k, v):
        """The Acid Sprayer's T3B spherical pressure tank with its hazard band and two gas bottles."""
        on = [v]
        sc = V(0.03, 0.12, 0.51)
        k.lathe("Sphere", sphere_prof(0.115, 8), tk.frame(sc), tk.WHITE.but(panel=0, bolts=False), "Yaw", on, sides=18, bevel=0.0)
        k.lathe("SphereBand", [(0, -0.018), (0.118, -0.018), (0.118, 0.018), (0, 0.018)], tk.frame(sc), tk.HAZARD, "Yaw", on, sides=18, bevel=0.002)
        k.torus("AcidBand", sc - UP * 0.048, UP, 0.1055, 0.008, self.glow(1.2), "Yaw", on, seg=18, sides=4)
        k.lathe("SphereSaddle", [(0, 0.385), (0.075, 0.385), (0.065, 0.41), (0, 0.41)], tk.frame((sc.x, sc.y, 0)), tk.GUNMETAL, "Yaw", on, sides=12)
        k.block("GlassFrame", (0.04, 0.09, 0.012), tk.frame(sc + V(0, 0.112, 0.035), z=V(0, 1, 0.3)), tk.DARK, "Yaw", on, bevel=0.002)
        k.block("Glass", (0.026, 0.075, 0.014), tk.frame(sc + V(0, 0.114, 0.035), z=V(0, 1, 0.3)), self.glow(0.95), "Yaw", on, bevel=0.0)
        level = sc + V(0, 0.118, 0.04)
        k.block("Meniscus", (0.03, 0.012, 0.012), tk.frame(level, z=V(0, 1, 0.3)), self.glow(2.0), "Slosh", on, bevel=0.0)
        self.slosh[v] = [level]
        b = V(0.2, 0.08, 0.38)
        k.cyl("Bottle", b, b + UP * 0.13, 0.024, look=tk.BLUE.but(panel=0, rings=0.05), bone="Yaw", on=on, sides=12)
        k.lathe("BottleCap", [(0, 0), (0.024, 0), (0.015, 0.022), (0, 0.027)], tk.frame(b + UP * 0.13), PLAIN, "Yaw", on, sides=12)

    def gun(self, k, v, x, r, length, bone, extractor=False):
        z = 0.32
        b0 = V(x, -0.18, z)
        k.barrel("Tube", b0, b0 + FWD * length, r, r * 0.62, tk.GUNMETAL, bone, [v], sides=14)
        # Acid bands round the tube: the shells come out coated.
        for f in (0.22, 0.62) if v == "T2" else (0.25,):
            k.torus("AcidRing", b0 + FWD * (length * f), FWD, r * 1.06, 0.008, self.glow(1.4), bone, [v], seg=12, sides=3)
        if extractor:
            k.lathe("Extractor", [(0, 0), (r * 1.1, 0), (r * 1.4, 0.02), (r * 1.4, 0.06), (r * 1.1, 0.08), (0, 0.08)],
                    tk.frame(b0 + FWD * (length * 0.5), z=FWD), tk.WHITE.but(panel=0, bolts=False), bone, [v], sides=12)
        k.torus("MuzzleRing", b0 + FWD * (length - 0.015), FWD, r * 1.06, 0.007, tk.YELLOW, bone, [v], seg=12, sides=3)
        bw = r * 2.4
        k.block("Brake", (bw, 0.07, r * 1.8), tk.frame(b0 + FWD * (length + 0.03)), tk.DARK.but(panel=0.024), bone, [v], bevel=0.006)
        for s in (-1, 1):
            k.block("BrakePort", (0.008, 0.04, r * 1.2), tk.frame(b0 + FWD * (length + 0.03) + V(s * bw / 2, 0, 0)), self.glow(1.5), bone, [v], bevel=0.0)
        self.barrel(v, b0, FWD, length + 0.065)

    def more_clips(self, arm, v):
        def slosh(t, f):
            return {"Slosh": {"loc": UP * (0.004 * math.sin(2 * math.pi * t)), "rot": (YAX, 0.07 * math.sin(2 * math.pi * t + 0.6))}}
        return [bk.clip(arm, "Slosh", 60, slosh, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Storm Flak = Flak + Tesla Coil
# -------------------------------------------------------------------------------------------------------------

class StormFlak(Spec3):
    """The Flak's elevated guns (40 degrees), shield and drums, with Tesla coils round the jackets and a coil spire on
    the back whose prong tips are the chain-lightning sockets (Arc_n)."""
    name = "StormFlak"
    accent_name = "volt"
    recoil = 0.04
    pitch_at = V(0, 0, 0.4)
    smoke_at = V(0, 0.12, 0.42)
    preview_pitch = 0.0
    SPIRE = {"T1": (0.17, 2, 3), "T2": (0.22, 3, 4), "T3": (0.29, 4, 6)}

    def pieces(self, k):
        ring_base(k, 0.18)
        t = V(0, 0, 0.4)
        k.block("Carriage", (0.3, 0.26, 0.08), tk.frame((0, 0.02, 0.29)), tk.BLUE, "Yaw", taper=(0.92, 0.85))
        k.block("CarriageTop", (0.24, 0.12, 0.02), tk.frame((0, 0.1, 0.335)), tk.WHITE.but(panel=0), "Yaw")
        for s in (-1, 1):
            k.block("Cheek", (0.04, 0.17, 0.15), tk.frame((s * 0.135, 0.0, 0.37)), tk.WHITE.but(panel=0.07), "Yaw", taper=(1.0, 0.65))
            k.cyl("TrunnionStub", (s * 0.11, 0, 0.4), (s * 0.16, 0, 0.4), 0.03, look=tk.GUNMETAL, bone="Pitch", sides=10)
        self.arcs = {}
        for v in self.variants:
            on = [v]
            # The Flak's drum on the left, the Tesla's caged Crysium cell on the right (from T2).
            c = V(-0.2, 0.03, 0.36)
            k.cyl("Drum", c - V(0.03, 0, 0), c + V(0.03, 0, 0), 0.07, look=tk.GUNMETAL.but(rings=0.02), bone="Yaw", on=on, sides=14)
            k.torus("DrumBand", c, XAX, 0.071, 0.007, tk.YELLOW, "Yaw", on, seg=14, sides=3)
            k.cyl("DrumCap", c - V(0.03, 0, 0), c - V(0.036, 0, 0), 0.045, look=tk.DARK, bone="Yaw", on=on, sides=12, bevel=0.0)
            if v != "T1":
                crysium_cell(k, on, V(0.2, 0.02, 0.27), "Yaw", height=0.11)
            # The coil spire on the back: column, coils, discharge ball, prongs.
            h, n, pr = self.SPIRE[v]
            sb = V(0, 0.15, 0.0)
            z0 = 0.35
            coil_stack(k, on, z0 + 0.03, z0 + h - 0.04, n, 0.06, 0.044, self.glow(1.0), "Yaw", column=(0.022, 0.017), base=sb, tube=0.011, seg=12,
                       sides=3, disc=10)
            top = sb + UP * (z0 + h + 0.015)
            ball(k, "SpireBall", top, 0.03, self.glow(1.35, lens=0.03), "Yaw", on, sides=10, n=4)
            k.torus("SpireCage", top, XAX, 0.038, 0.004, tk.GUNMETAL, "Yaw", on, seg=10, sides=3, lod=False)
            k.torus("SpireCage", top, YAX, 0.038, 0.004, tk.GUNMETAL, "Yaw", on, seg=10, sides=3, lod=False)
            arcs = []
            reach = 0.05 if v != "T3" else 0.085
            for i in range(pr):
                a = 2 * math.pi * i / pr + 0.3
                dd = V(math.cos(a), math.sin(a), 0)
                p0 = top - UP * 0.03 + dd * 0.024
                p1 = p0 + dd * reach + UP * 0.03
                arc_prong(k, p0, p1, "Yaw", on)
                arcs.append(p1 + dd * 0.012)
            if v == "T3":
                k.torus("Halo", top - UP * 0.005, UP, reach + 0.03, 0.006, self.glow(1.3), "Yaw", on, seg=24, sides=3)
            self.arcs[v] = arcs
        # T2+: a small radar dish (the Flak's).
        for v, r, h in (("T2", 0.055, 0.1), ("T3", 0.07, 0.12)):
            base = V(-0.12, 0.13, 0.33)
            k.cyl("Mast", base, base + UP * h, 0.011, look=tk.GUNMETAL, bone="Yaw", on=[v], sides=8)
            dish = base + UP * h
            m = tk.frame(dish, z=V(0, 0.5, 0.86))
            k.lathe("Dish", [(0, 0), (r, 0.02), (r * 1.02, 0.03), (r * 0.96, 0.034), (0, 0.012)], m, PLAIN, "Yaw", [v], sides=12, bevel=0.0)
            k.cyl("Feedhorn", dish, dish + V(0, 0.5, 0.86) * 0.045, 0.007, look=self.glow(1.5), bone="Yaw", on=[v], sides=6, bevel=0.0)

        k.block("Cradle", (0.16, 0.09, 0.22), tk.frame(t + D40 * 0.03, z=D40), tk.GUNMETAL, "Pitch")
        for v, w, h in (("T1", 0.26, 0.16), ("T2", 0.28, 0.17), ("T3", 0.32, 0.2)):
            m = tk.frame(t + D40 * 0.13 + U40 * 0.01, z=D40, y=U40)
            k.block("Shield", (w, h, 0.022), m, tk.WHITE.but(panel=0.08), "Pitch", [v])
            m2 = tk.frame(t + D40 * 0.142 + U40 * (0.01 + h / 2 - 0.016), z=D40, y=U40)
            k.block("ShieldStripe", (w * 0.96, 0.024, 0.008), m2, tk.HAZARD, "Pitch", [v], bevel=0.0)
            k.block("ShieldGlow", (w * 0.5, 0.012, 0.008), tk.frame(t + D40 * 0.142 - U40 * (h / 2 - 0.03), z=D40, y=U40), self.glow(1.4), "Pitch", [v],
                    bevel=0.0, lod=False)

        def tube(v, x, u, length, r, bone, sides=12, coils=2, brake=False):
            b0 = t + V(x, 0, 0) + U40 * u + D40 * 0.06
            k.barrel("Tube", b0, b0 + D40 * length, r, r * 0.6, tk.GUNMETAL, bone, [v], sides=sides)
            k.lathe("Jacket", [(0, 0), (r * 1.5, 0), (r * 1.5, 0.15), (0, 0.16)], tk.frame(b0 + D40 * 0.05, z=D40), tk.STEEL.but(rings=0.02), bone, [v],
                    sides=sides)
            # Tesla coils on the jacket (dropped on LOD1: the spire carries the read from far).
            for i in range(coils):
                k.torus("Coil", b0 + D40 * (0.08 + i * 0.045), D40, r * 1.5 + 0.009, 0.0075, self.glow(1.05), bone, [v], seg=10, sides=3, lod=False)
            if brake:
                k.block("Brake", (r * 2.4, r * 2.0, 0.05), tk.frame(b0 + D40 * (length + 0.015), z=D40, y=U40), tk.DARK.but(panel=0.02), bone, [v],
                        bevel=0.004)
            # A forked arc muzzle (two short horns) instead of the flash hider: the shells leave charged.
            for s in (-1, 1):
                p0 = b0 + D40 * (length - 0.02) + U40 * (s * r * 1.1)
                k.cyl("Horn", p0, p0 + D40 * 0.045 + U40 * (s * 0.012), 0.005, 0.004, look=tk.GUNMETAL, bone=bone, on=[v], sides=5, bevel=0.0,
                      lod=False)
            k.torus("MuzzleRing", b0 + D40 * (length - 0.03), D40, r * 1.1, 0.005, tk.YELLOW, bone, [v], seg=10, sides=3)
            self.barrel(v, b0, D40, length + 0.03)

        tube("T1", -0.045, 0.0, 0.38, 0.018, "Barrel_1")
        tube("T1", 0.045, 0.0, 0.38, 0.018, "Barrel_2")
        tube("T2", -0.045, 0.0, 0.45, 0.019, "Barrel_1", coils=3)
        tube("T2", 0.045, 0.0, 0.45, 0.019, "Barrel_2", coils=3)
        # T3: the Flak T3B's long heavy pair with brakes, three coils each (lighter than a quad; the spire grows).
        tube("T3", -0.05, 0.0, 0.53, 0.023, "Barrel_1", coils=2, brake=True)
        tube("T3", 0.05, 0.0, 0.53, 0.023, "Barrel_2", coils=2, brake=True)
        for v, pts in self.arcs.items():
            self.extra_bones[v] = [tk.Bone(f"Arc_{i + 1}", p, UP, "Yaw", deform=False, length=0.03) for i, p in enumerate(pts)]

    def more_clips(self, arm, v):
        n = len(self.barrels[v])
        step = 16 // n

        def alternate(t, f):
            spec = {}
            for i, (_, d, _) in enumerate(self.barrels[v]):
                kk = bt.kick((f - i * step) % 16, 8)
                spec[f"Barrel_{i + 1}"] = {"loc": d * (-self.recoil * kk)}
            return spec
        return [bk.clip(arm, "FireAlternate", 16, alternate, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Thermal Shock = Cryo Turret + Lava Ball
# -------------------------------------------------------------------------------------------------------------

class ThermalShock(Spec3):
    """The Cryo's frosted cylinder gun with vane nozzle and canisters, and the Lava Ball's plated thermite ball in a
    cradle on the back as the heat core: cold out of the front, heat from the back, the shock where they meet."""
    name = "ThermalShock"
    accent_name = "frost"
    recoil = 0.01
    pitch_at = V(0, 0.0, 0.405)
    smoke_at = V(0, 0.1, 0.46)
    strip_clip = "Fire_1"
    BALL = {"T1": 0.075, "T2": 0.088, "T3": 0.1}

    def pieces(self, k):
        ring_base(k, 0.17)
        z = 0.405
        for v in self.variants:
            on = [v]
            k.block("Hull", (0.32, 0.32, 0.11), tk.frame((0, 0.03, 0.32)), tk.BLUE, "Yaw", on, taper=(0.88, 0.84), bevel=0.01)
            k.block("TopArmour", (0.26, 0.24, 0.022), tk.frame((0, 0.04, 0.385)), tk.WHITE, "Yaw", on, taper=(0.9, 0.86))
            k.block("Glacis", (0.28, 0.022, 0.09), tk.frame((0, -0.125, 0.318), rot=(-40, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            for s in (-1, 1):
                k.block("Cheek", (0.035, 0.15, 0.13), tk.frame((s * 0.13, 0.0, 0.4)), tk.WHITE.but(panel=0.07), "Yaw", on, taper=(1.0, 0.7))
                k.cyl("TrunnionStub", (s * 0.105, 0, z), (s * 0.15, 0, z), 0.03, look=tk.GUNMETAL, bone="Pitch", on=on, sides=10)
            # Cryo canisters (left), heat sinks (right): cold and hot halves on the hull.
            spots = {"T1": [(-1, 0.04)], "T2": [(-1, 0.07), (-1, -0.03)], "T3": [(-1, 0.08), (-1, -0.03)]}[v]
            for s, y in spots:
                b = V(s * 0.2, y, 0.28)
                k.cyl("Canister", b, b + UP * 0.16, 0.03, look=FROST.but(rings=0.045), bone="Yaw", on=on, sides=12)
                k.lathe("CanisterCap", [(0, 0), (0.03, 0), (0.024, 0.02), (0, 0.028)], tk.frame(b + UP * 0.16), self.glow(1.6), "Yaw", on, sides=12,
                        bevel=0.0)
            fins = {"T1": 3, "T2": 4, "T3": 5}[v]
            for i in range(fins):
                y = -0.08 + i * 0.045
                k.block("HeatFin", (0.05, 0.012, 0.09), tk.frame((0.19, y, 0.32)), tk.GUNMETAL, "Yaw", on, bevel=0.0)
            k.block("HeatGlow", (0.054, 0.045 * (fins - 1) + 0.014, 0.012), tk.frame((0.19, -0.08 + 0.0225 * (fins - 1), 0.36)), G3("lava", 1.3), "Yaw",
                    on, bevel=0.0)
            # The thermite ball in a cradle on the back (towards the camera), with a hot conduit into the breech.
            R = self.BALL[v]
            bc = V(0, 0.19, 0.4 + R)
            k.lathe("BallCradle", [(0, 0.39), (R * 0.95, 0.39), (R * 1.05, 0.41), (R * 0.85, 0.4 + R * 0.55), (R * 0.75, 0.4 + R * 0.55), (0, 0.41)],
                    tk.frame((0, 0.19, 0)), tk.GUNMETAL, "Yaw", on, sides=14)
            for a in (0, 120, 240):
                d = V(math.cos(math.radians(a + 90)), math.sin(math.radians(a + 90)), 0)
                p0 = V(0, 0.19, 0.4) + d * (R * 0.9)
                k.block("BallClaw", (0.02, 0.016, R * 0.9), tk.frame(p0 + UP * (R * 0.4) + d * (R * 0.12), z=UP + d * 0.4, y=d), tk.DARK, "Yaw", on,
                        bevel=0.002)
            thermite_ball(k, bc, R, "Yaw", on, bands=v != "T1")
            rod(k, "HeatConduit", bc + V(0, -R * 0.8, -R * 0.3), V(0, 0.07, z + 0.04), 0.014, tk.RUBBER, "Yaw", on, sides=8)

        def gun(v, x, length, r, bone, hot=False):
            on = [v]
            b0 = V(x, 0.1, z)
            look = FROST if not hot else tk.GUNMETAL.but(rings=0.035)
            sides = 14 if not hot else 10
            k.lathe("Cylinder", [(0, 0), (r * 0.6, 0), (r, 0.03), (r, length), (0, length)], tk.frame(b0, z=FWD), look, bone, on, sides=sides)
            rings = 3
            for i in range(rings):
                g = G3("frost", 1.45) if not hot else G3("lava", 1.35)
                k.torus("Ring", b0 + FWD * (0.08 + i * (length - 0.12) / (rings - 1)), FWD, r + 0.003, 0.0075, g, bone, on, seg=sides, sides=3)
            front = b0 + FWD * length
            k.lathe("Nozzle", [(0, 0), (r * 0.7, 0), (r * 0.62, 0.03), (r * 0.85, 0.08), (r * 0.7, 0.08), (0, 0.06)], tk.frame(front, z=FWD), tk.DARK,
                    bone, on, sides=sides)
            k.cyl("NozzleGlow", front + FWD * 0.064, front + FWD * 0.07, r * 0.66, look=(self.glow(1.3, lens=r * 0.6) if not hot else G3("lava", 1.3, lens=r * 0.6)),
                  bone=bone, on=on, sides=sides, bevel=0.0)
            if not hot:
                for a in (45, 135, 225, 315):
                    dd = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
                    c = front + FWD * 0.05 + dd * (r * 0.85 + 0.01)
                    m = tk.frame(c, z=dd, y=FWD) @ tk.frame(rot=(-25, 0, 0))
                    k.block("Vane", (0.05, 0.065, 0.01), m, PLAIN, bone, on, bevel=0.002)
            if not hot:
                k.block("Saddle", (0.09, 0.12, 0.04), tk.frame(V(x, 0.0, z - r + 0.005)), tk.GUNMETAL, bone, on)
            self.barrel(v, b0, FWD, length + 0.09)
            return b0

        gun("T1", 0.0, 0.33, 0.07, "Barrel_1")
        b2 = gun("T2", 0.0, 0.37, 0.078, "Barrel_1")
        # T2: the shock collar where the heat meets the cold: a ring of hot slits behind the nozzle.
        for i in range(8):
            a = 2 * math.pi * i / 8
            dd = V(math.cos(a), 0, math.sin(a))
            k.block("ShockSlit", (0.012, 0.03, 0.006), tk.frame(b2 + FWD * 0.3 + dd * 0.083, z=dd, y=FWD), G3("lava", 1.5), "Barrel_1", ["T2"], bevel=0.0,
                    lod=False)
        k.torus("ShockCollar", b2 + FWD * 0.3, FWD, 0.083, 0.012, tk.GUNMETAL, "Barrel_1", ["T2"], seg=16, sides=4)
        # T3: twin barrels, the frost cylinder and a thermite barrel side by side.
        gun("T3", -0.06, 0.38, 0.07, "Barrel_1")
        gun("T3", 0.075, 0.34, 0.05, "Barrel_2", hot=True)


SPECS3 = {"Shatter": Shatter, "ArcLaser": ArcLaser, "CorrosiveBlast": CorrosiveBlast, "StormFlak": StormFlak, "ThermalShock": ThermalShock}
# Parents (for the comparison renders): round-7 names go through base_kit.tower, round-11 ones are rebuilt from maps.
PARENTS = {"Shatter": ("Cryo", "Railgun"), "ArcLaser": ("Laser", "Tesla"), "CorrosiveBlast": ("Cannon", "Acid"), "StormFlak": ("Flak", "Tesla"),
           "ThermalShock": ("Cryo", "LavaBall")}
PARENT_TIER = {"Cryo": "T2", "Railgun": "T2", "Laser": "T2", "Tesla": "T2", "Cannon": "T2", "Acid": "T2", "Flak": "T2"}


# -------------------------------------------------------------------------------------------------------------
# Previews
# -------------------------------------------------------------------------------------------------------------

_PARENT_CACHE = {}


def parent_objs(name, loc, head_deg=115):
    """A parent tower (or the Lava Ball) at `loc`, LOD0 from its maps on disk."""
    if name in bt.SPECS:
        return bk.tower(name, PARENT_TIER[name], loc, head_deg=head_deg)
    if name == "LavaBall":
        import build_gadgets as bg
        import build_gadgets2 as g2
        if name not in _PARENT_CACHE:
            spec = g2.LavaBall()
            kit = spec.kit()
            mat, _ = bk.bake_set(kit, os.path.join(C.EXPORTS, kit.name), 512, 256, bake=False)
            _PARENT_CACHE[name] = (spec, kit, mat)
        spec, kit, mat = _PARENT_CACHE[name]
        objs = bg.objects(spec, kit, mat, label=f"Cmp{len(bpy.data.objects)}", lod=False)
        objs[0].location = loc
        return objs
    if name not in _PARENT_CACHE:
        spec = t2.SPECS[name]()
        kit, mat, _ = t2.build_tower(spec, bake=False, export=False)
        _PARENT_CACHE[name] = (spec, kit, mat)
    spec, kit, mat = _PARENT_CACHE[name]
    tier = PARENT_TIER[name]
    objs = bt.build_variant(spec, kit, tier, mat, name=f"{kit.name}_{tier}_Cmp{len(bpy.data.objects)}", lod=False)
    objs[0].location = loc
    bt.pose(objs[0], head_deg, spec.preview_pitch, bt.stars_for(tier))
    return objs


def hide(objs, flag):
    for o in objs:
        o.hide_render = flag


def previews(spec, kit, mat, out):
    tag = spec.name.lower()
    stage = bt.stage_ground()
    rows = []
    spacing = 1.1
    xs = [(i - 1) * spacing for i in range(3)]
    for i, v in enumerate(spec.variants):
        objs = bt.build_variant(spec, kit, v, mat, name=f"{kit.name}_{v}_Rig", lod=False)
        objs[0].location = (xs[i], 0, 0)
        bt.pose(objs[0], 115, spec.preview_pitch, bt.stars_for(v))
        rows.append((v, objs))
    pa, pb = PARENTS[spec.name]
    cmp = parent_objs(pa, (xs[0] - 1.25, 0, 0)) + parent_objs(pb, (xs[-1] + 1.25, 0, 0))
    sk = bk.skitters([(x, -1.25, 0) for x in (-2.6, -1.1, 0.4, 1.9)], yaw=math.radians(90))
    centre = V(0, 0, 0.3)
    bk.render_game(centre, os.path.join(out, f"{tag}_parents_game.png"), zoom=1.0, width=640, height=360)
    bk.render_game(centre + V(0, -0.3, 0), os.path.join(out, f"{tag}_parents_zoom.png"), zoom=2.2, width=1280, height=720)
    bk.unstage(sk)
    hide(cmp, True)
    shots = []
    for i, (v, objs) in enumerate(rows):
        for vv, oo in rows:
            hide(oo, vv != v)
        p = os.path.join(out, f"{tag}_{v}_close.png")
        bk.render_close(V(xs[i], 0, spec.close_z), p, spec.close_dist, azimuth=spec.close_az, elevation=26, lens=50, light_scale=0.6)
        shots.append(p)
    C.contact_sheet(shots, os.path.join(out, f"{tag}_close_sheet.png"), len(shots), 640)
    vl, ol = rows[-1]
    for vv, oo in rows:
        hide(oo, vv != vl)
    bk.render_close(V(xs[-1], -0.02, spec.macro_z), os.path.join(out, f"{tag}_macro.png"), 0.62, azimuth=35, elevation=30, lens=70, width=1280,
                    height=720, light_scale=0.6)
    arm = ol[0]
    acts = spec.clips(arm, vl)
    act = next((a for a in acts if a.name == spec.strip_clip), acts[-1])
    arm.animation_data.action = act
    rig = tk.close_lights(0.6)
    cam = tk.closeup_camera(V(xs[-1], -0.1, 0.45), 1.5, azimuth=90, elevation=12, lens=55)
    strip = []
    last = int(act.frame_range[1])
    for f in spec.strip_frames:
        bpy.context.scene.frame_set(min(f, last))
        p = os.path.join(out, f"_{tag}_strip_{f}.png")
        C.render(cam, p, 480, 360, bloom=False)
        strip.append(p)
    C.contact_sheet(strip, os.path.join(out, f"{tag}_{act.name.lower()}_strip.png"), len(strip), 400)
    for p in strip:
        os.remove(p)
    bk.remove([cam] + rig)
    for vv, oo in rows:
        bk.remove(oo)
    bk.remove(cmp + stage)
    bk.clear_actions()


def lineup(out):
    """All five hybrids, T1 at the back and T3 in front, with Skitters on the road, at game zoom."""
    stage = bt.stage_ground()
    placed = []
    names = list(SPECS3)
    spacing = 1.1
    for n_i, n in enumerate(names):
        spec = SPECS3[n]()
        kit, mat, _ = t2.build_tower(spec, bake=False, export=False)
        for row, tier in enumerate(("T1", "T3")):
            x = (n_i - (len(names) - 1) / 2) * spacing
            objs = bt.build_variant(spec, kit, tier, mat, name=f"{kit.name}_{tier}_L{row}", lod=False)
            objs[0].location = (x, row * 1.35, 0)
            bt.pose(objs[0], 100 + 14 * n_i, spec.preview_pitch, bt.stars_for(tier))
            placed += objs
    placed += bk.tower("Gun", "T1", (-3.6, 0.0, 0))
    placed += bk.tower("Cannon", "T3A", (3.6, 1.35, 0))
    sk = bk.skitters([(x, -1.25, 0) for x in (-3.4, -1.6, 0.2, 2.0, 3.8)], yaw=math.radians(90))
    bk.render_game(V(0, 0.4, 0.3), os.path.join(out, "towers3_lineup_game.png"), zoom=1.0, width=1280, height=720)
    bk.render_game(V(0, 0.5, 0.3), os.path.join(out, "towers3_lineup_zoom.png"), zoom=1.9, width=1280, height=720)
    bk.unstage(sk)
    bk.remove(placed + stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    C.preview_world()
    tk.metal_detail_map()
    if args.get("lineup"):
        lineup(out)
        return
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else list(SPECS3)
    bake = not args.get("nobake")
    for n in names:
        spec = SPECS3[n]()
        kit, mat, report = t2.build_tower(spec, bake)
        report["hybrid_of"] = list(PARENTS[spec.name])
        report["levels"] = "T1..T3, no A/B"
        report["glows"] = {k: list(GL3[k]) for k in GL3 if k in ("shatter", "frost", "volt", "lens", "acid", "lava", "crysium")}
        tk.write_json(os.path.join(C.EXPORTS, kit.name, kit.name + "_report.json"), report)
        if not args.get("norender"):
            previews(spec, kit, mat, out)
    C.save_blend("Towers3" if only is None else "Towers3_" + "_".join(names))


if __name__ == "__main__":
    main()
