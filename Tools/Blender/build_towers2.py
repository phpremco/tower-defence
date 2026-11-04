"""Round 11: the P2 towers (see ArtSource/DESIGN_NOTES.md, round 11). Tesla Coil, Railgun and Acid Sprayer (T1, T2,
T3A, T3B), the hybrids Artillery (Cannon + Flak), Storm Rifle (Gun + Tesla) and Cryo Beam (Laser + Cryo), the Cryo
Turret, and the out-of-campaign Support and Rocket (T1 to T3).

Same kit and rig as round 7: the pad, the rank plate and the stars come from build_towers.py (imported, unchanged),
pieces, atlas and self-bake from tower_kit.py. Per tower `Exports/Tower_<Name>/Tower_<Name>_<Tier>.fbx` (LOD0 and LOD1
skinned to one `Tower_<Name>_Rig`, with the clips), one 1024 map set shared by all tiers (+ `Low/` 512) and
`Tower_<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_towers2.py -- [--only Tesla,Railgun]
     [--preview <dir>] [--norender] [--nobake] [--lineup]
`--lineup` rebuilds every tower from the maps on disk (no bake, no export) and renders the roster lineup only.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_towers as bt  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from build_towers import FWD, UP, V, XAX, ring_base  # noqa: E402

# Saturated on purpose: pale glows burn out to white under bloom (review round 1), and one hue per tower must read.
GL = {
    "tesla": (0.52, 0.28, 1.0),
    "crysium": (0.7, 0.48, 1.0),
    "rail": (0.5, 0.8, 1.0),
    "acid": (0.3, 1.0, 0.38),
    "amber": tk.GLOW["amber"],
    "storm": (0.4, 0.46, 1.0),
    "frost": (0.4, 0.9, 1.0),
    "ice": (0.5, 1.0, 0.95),
    "signal": (1.0, 0.74, 0.2),
    "rocket": (0.38, 0.7, 1.0),
}

PLAIN = tk.WHITE.but(panel=0, bolts=False)
FROST = tk.WHITE.but(paint=(0.8, 0.87, 0.93), panel=0, bolts=False, rings=0.05)
ELEV40 = math.radians(40)
D40 = Vector((0, -math.cos(ELEV40), math.sin(ELEV40)))
U40 = Vector((0, math.sin(ELEV40), math.cos(ELEV40)))


def G(name, strength=1.0, lens=0.0):
    return tk.glow(GL[name], strength, lens)


def sphere_prof(r, n=6, zc=0.0):
    return [(0, zc - r)] + [(r * math.sin(math.pi * i / n), zc - r * math.cos(math.pi * i / n)) for i in range(1, n)] + [(0, zc + r)]


def square_collar(k, name, centre, axis, r_in, r_out, depth, look, bone, on):
    """A square ring (a lathe with four sides): the accelerator collars round the rails."""
    prof = [(r_in, -depth / 2), (r_out, -depth / 2), (r_out, depth / 2), (r_in, depth / 2), (r_in, -depth / 2)]
    k.lathe(name, prof, tk.frame(centre, z=axis), look, bone, on, sides=4, start=math.pi / 4, bevel=0.003)


def rod(k, name, p0, p1, r, look, bone, on, sides=6, lod=True):
    k.cyl(name, p0, p1, r, look=look, bone=bone, on=on, sides=sides, bevel=0.0, lod=lod)


def ball(k, name, centre, r, look, bone, on, sides=10, n=4, lod=True):
    k.lathe(name, sphere_prof(r, n), tk.frame(centre), look, bone, on, sides=sides, bevel=0.0, lod=lod)


def coil_stack(k, on, z0, z1, n, r0, r1, look, bone, column=(0.042, 0.034), base=None, tube=0.016, seg=14, sides=4, disc=12,
               disc_look=None):
    """Tesla coil: a column with `n` glowing coil rings stacked on white insulator discs (z along world up)."""
    b = V(0, 0, 0) if base is None else Vector(base)
    k.cyl("Column", b + UP * (z0 - 0.05), b + UP * (z1 + 0.035), column[0], column[1], look=tk.GUNMETAL.but(rings=0.03), bone=bone,
          on=on, sides=10)
    for i in range(n):
        f = i / max(n - 1, 1)
        z = z0 + (z1 - z0) * f
        r = r0 + (r1 - r0) * f
        k.torus("Coil", b + UP * z, UP, r, tube, look, bone, on, seg=seg, sides=sides)
        k.lathe("Insulator", [(0, z - tube * 1.5), (r * 0.86, z - tube * 1.5), (r * 0.8, z - tube * 0.6), (0, z - tube * 0.6)],
                tk.frame(b), disc_look or tk.DARK, bone, on, sides=disc, bevel=0.0, lod=False)


def crysium_cell(k, on, c, bone, height=0.105, glow_name="crysium"):
    """A caged Crysium cell: gunmetal base, four posts, a white cap and a glowing hexagonal crystal."""
    c = Vector(c)
    k.block("CellBase", (0.08, 0.08, 0.025), tk.frame(c + V(0, 0, 0.012)), tk.GUNMETAL, bone, on, bevel=0.004)
    k.block("CellCap", (0.08, 0.08, 0.02), tk.frame(c + V(0, 0, height + 0.035)), PLAIN, bone, on, bevel=0.004)
    for dx in (-0.031, 0.031):
        for dy in (-0.031, 0.031):
            rod(k, "CellPost", c + V(dx, dy, 0.022), c + V(dx, dy, height + 0.027), 0.006, tk.GUNMETAL, bone, on, sides=4, lod=False)
    k.lathe("Crystal", [(0, 0), (0.022, 0.018), (0.027, height * 0.5), (0.019, height * 0.85), (0, height)],
            tk.frame(c + V(0, 0, 0.024), rot=(0, 0, 12)), G(glow_name, 1.15), bone, on, sides=6, bevel=0.0)


class Spec2(bt.Spec):
    """A round-11 tower: clips keyed with base_kit.clip (scale channels too), one named glow per tower."""
    accent_name = "azure"
    strip_clip = "Fire_1"
    strip_frames = (0, 1, 2, 4, 7, 10)
    close_z, close_dist, close_az, macro_z = 0.42, 1.6, 28, 0.5

    def __init__(self):
        super().__init__()
        self.accent = GL.get(self.accent_name, tk.GLOW.get(self.accent_name))
        self.sockets = {}

    def glow(self, strength=1.0, lens=0.0):
        return tk.glow(self.accent, strength, lens)

    def sweep(self, t, f):
        return {"Yaw": {"rot": (UP, 0.4 * math.sin(2 * math.pi * t))}, "Pitch": {"rot": (XAX, -0.05 * math.sin(4 * math.pi * t))}}

    def clips(self, arm, v):
        acts = [bk.clip(arm, "IdleSweep", 120, self.sweep, loop=True)]
        for i, (_, d, _) in enumerate(self.barrels[v]):
            acts.append(bk.clip(arm, f"Fire_{i + 1}", 10, self._fire(i, d)))
        return acts + self.more_clips(arm, v)

    def more_clips(self, arm, v):
        return []


# -------------------------------------------------------------------------------------------------------------
# Tesla Coil
# -------------------------------------------------------------------------------------------------------------

class Tesla(Spec2):
    name = "Tesla"
    accent_name = "tesla"
    recoil = 0.0
    pitch_at = V(0, 0, 0.58)
    smoke_at = V(0, 0.08, 0.42)
    close_z, close_dist, macro_z = 0.6, 2.1, 0.8
    TOP = {"T1": 0.6, "T2": 0.64, "T3A": 0.68, "T3B": 0.62}
    COILS = {"T1": 3, "T2": 4, "T3A": 5, "T3B": 4}
    strip_clip = "Fire_1"
    strip_frames = (0, 1, 2, 4, 7, 10)

    def pieces(self, k):
        ring_base(k, 0.165)
        oct_ = math.pi / 8
        k.lathe("Drum", [(0, 0.27), (0.165, 0.27), (0.17, 0.29), (0.155, 0.335), (0, 0.335)], tk.frame(), tk.BLUE.but(panel=0.07), "Yaw",
                sides=8, start=oct_)
        k.lathe("DrumCap", [(0, 0.33), (0.152, 0.33), (0.132, 0.356), (0, 0.356)], tk.frame(), tk.WHITE.but(panel=0.09), "Yaw", sides=8, start=oct_)
        for a in (45, 135, 225, 315):
            d = V(math.cos(math.radians(a)), math.sin(math.radians(a)), 0)
            k.block("DrumSlit", (0.06, 0.012, 0.006), tk.frame(d * 0.157 + V(0, 0, 0.305), z=d, y=UP), self.glow(1.3), "Yaw", bevel=0.0)
        # Two caged Crysium cells feed the column (left and right, so the camera always sees one).
        for s in (-1, 1):
            crysium_cell(k, None, V(s * 0.215, 0.0, 0.27), "Yaw")
            rod(k, "Conduit", V(s * 0.215, 0, 0.41), V(s * 0.19, 0, 0.445), 0.011, tk.RUBBER, "Yaw", None, sides=8, lod=False)
            rod(k, "Conduit", V(s * 0.19, 0, 0.445), V(s * 0.04, 0, 0.47), 0.011, tk.RUBBER, "Yaw", None, sides=8, lod=False)
        caps = {"T2": (0.02, 0.15, 0.195, tk.BLUE.but(panel=0, rings=0.04)), "T3A": (0.022, 0.17, 0.195, tk.BLUE.but(panel=0, rings=0.04)),
                "T3B": (0.034, 0.18, 0.21, tk.WHITE.but(panel=0, bolts=False, rings=0.04))}
        for v, (pr, ph, rad, look) in caps.items():
            for a in (90, 210, 330):
                d = V(math.cos(math.radians(a)), math.sin(math.radians(a)), 0)
                b0 = d * rad + V(0, 0, 0.27)
                k.cyl("Capacitor", b0, b0 + UP * ph, pr, pr * 0.9, look=look, bone="Yaw", on=[v], sides=10)
                k.torus("CapBand", b0 + UP * (ph * 0.3), UP, pr + 0.003, 0.005, tk.YELLOW, "Yaw", [v], seg=8, sides=3, lod=False)
                ball(k, "CapTip", b0 + UP * (ph + 0.008), pr * 0.75, self.glow(1.6), "Yaw", [v], sides=8, n=3, lod=False)
                if v == "T3B":
                    rod(k, "CapConduit", b0 + UP * (ph - 0.01), V(0, 0, 0.48) + d * 0.04, 0.009, tk.RUBBER, "Yaw", [v], sides=6)
        for v in self.variants:
            self.head(k, v)

    def head(self, k, v):
        on = [v]
        top = self.TOP[v]
        coil_stack(k, on, 0.39, top - 0.045, self.COILS[v], 0.118, 0.082, self.glow(1.1), "Yaw")
        arcs = []
        if v == "T3B":
            k.lathe("CrystalCollar", [(0, top - 0.03), (0.075, top - 0.03), (0.08, top - 0.005), (0.06, top + 0.01), (0, top + 0.01)], tk.frame(),
                    tk.GUNMETAL, "Barrel_1", on, sides=12)
            k.lathe("Crystal", [(0, 0), (0.042, 0.025), (0.06, 0.065), (0.053, 0.16), (0.032, 0.215), (0, 0.25)],
                    tk.frame((0, 0, top - 0.01), rot=(0, 0, 15)), G("crysium", 1.05, lens=0.06), "Barrel_1", on, sides=6, bevel=0.0)
            for i in range(3):
                a = 2 * math.pi * i / 3 + math.pi / 2
                d = V(math.cos(a), math.sin(a), 0)
                b0, tip = d * 0.06 + V(0, 0, top), d * 0.072 + V(0, 0, top + 0.13)
                k.block("Claw", (0.026, 0.018, (tip - b0).length), tk.frame((b0 + tip) / 2, z=tip - b0, y=d), PLAIN, "Barrel_1", on, bevel=0.003)
                ball(k, "ClawTip", tip + UP * 0.008, 0.012, self.glow(1.7), "Barrel_1", on, sides=8, n=3)
                arcs.append(tip + UP * 0.02)
            hc = V(0, 0, top + 0.1)
            self.barrel(v, hc, UP, 0.21)
        else:
            r = {"T1": 0.05, "T2": 0.058, "T3A": 0.058}[v]
            k.lathe("HeadCollar", [(0, top - 0.012), (0.052, top - 0.012), (0.058, top + 0.004), (0.036, top + 0.016), (0, top + 0.016)],
                    tk.frame(), PLAIN, "Barrel_1", on, sides=12)
            hc = V(0, 0, top + 0.014 + r)
            k.lathe("Sphere", sphere_prof(r, 6), tk.frame(hc), self.glow(1.25, lens=r), "Barrel_1", on, sides=12, bevel=0.0)
            for ax in (XAX, V(0, 1, 0)):
                k.torus("Cage", hc, ax, r + 0.011, 0.006, tk.GUNMETAL, "Barrel_1", on, seg=14, sides=3, lod=False)
            k.cyl("Rod", hc + UP * (r + 0.008), hc + UP * (r + 0.045), 0.008, 0.002, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=6, bevel=0.0, lod=False)
            self.barrel(v, hc, UP, r + 0.045)
            if v == "T3A":
                hz = top + 0.02
                k.torus("Halo", V(0, 0, hz), UP, 0.17, 0.011, PLAIN, "Barrel_1", on, seg=20, sides=4)
                k.torus("HaloGlow", V(0, 0, hz + 0.011), UP, 0.17, 0.005, self.glow(1.5), "Barrel_1", on, seg=20, sides=3, lod=False)
                for i in range(6):
                    a = 2 * math.pi * i / 6 + math.pi / 6
                    d = V(math.cos(a), math.sin(a), 0)
                    rod(k, "Spoke", d * 0.05 + V(0, 0, top + 0.006), d * 0.165 + V(0, 0, hz), 0.008, tk.GUNMETAL, "Barrel_1", on)
                    tip = d * 0.2 + V(0, 0, hz + 0.075)
                    k.cyl("Prong", d * 0.172 + V(0, 0, hz), tip, 0.009, 0.005, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=6, bevel=0.0)
                    ball(k, "ProngTip", tip, 0.014, self.glow(1.7), "Barrel_1", on, sides=6, n=2, lod=False)
                    arcs.append(tip)
            else:
                n = {"T1": 3, "T2": 4}[v]
                for i in range(n):
                    a = 2 * math.pi * i / n + math.pi / 2
                    d = V(math.cos(a), math.sin(a), 0)
                    p0 = d * 0.05 + V(0, 0, top + 0.004)
                    p1 = p0 + d * 0.075 + UP * 0.055
                    k.cyl("Prong", p0, p1, 0.008, 0.005, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=6, bevel=0.0)
                    ball(k, "ProngTip", p1, 0.013, self.glow(1.7), "Barrel_1", on, sides=6, n=2, lod=False)
                    arcs.append(p1)
        self.extra_bones[v] = [tk.Bone(f"Arc_{i + 1}", p, (p - hc), "Barrel_1", deform=False, length=0.03) for i, p in enumerate(arcs)]

    def sweep(self, t, f):
        return {"Yaw": {"rot": (UP, 2 * math.pi * t)}}

    def clips(self, arm, v):
        def charge(t, f):
            e = bt.ease(t)
            return {"Barrel_1": {"loc": UP * (0.012 * e), "scale": 1.0 + 0.06 * e}}

        def fire(t, f):
            k = bt.kick(f)
            return {"Barrel_1": {"loc": UP * (0.012 * (1 - k) * (1 - t)), "scale": 1.0 + 0.12 * k}, "Yaw": {"rot": (UP, 0.05 * k)}}
        return [bk.clip(arm, "IdleSweep", 240, self.sweep, loop=True), bk.clip(arm, "ChargeUp", 24, charge), bk.clip(arm, "Fire_1", 10, fire)]


# -------------------------------------------------------------------------------------------------------------
# Railgun
# -------------------------------------------------------------------------------------------------------------

class Railgun(Spec2):
    name = "Railgun"
    accent_name = "rail"
    close_z, close_dist, close_az, macro_z = 0.42, 2.0, 140, 0.45
    recoil = 0.06
    pitch_at = V(0, -0.04, 0.385)
    smoke_at = V(0, 0.1, 0.45)
    strip_clip = "Fire_1"

    def pieces(self, k):
        ring_base(k, 0.17)
        z = 0.385
        for v in self.variants:
            on = [v]
            w = 0.32 if v in ("T3A", "T3B") else 0.3
            k.block("Hull", (w, 0.34, 0.11), tk.frame((0, 0.03, 0.32)), tk.BLUE, "Yaw", on, taper=(0.88, 0.84), shift=(0, 0.012), bevel=0.01)
            k.block("TopArmour", (w * 0.82, 0.26, 0.024), tk.frame((0, 0.05, 0.386)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (w * 0.86, 0.022, 0.09), tk.frame((0, -0.125, 0.318), rot=(-40, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            cw = 0.24 if v != "T3B" else 0.2
            k.block("CapBlock", (cw, 0.1, 0.09), tk.frame((0, 0.165, 0.44)), tk.WHITE.but(panel=0.07), "Yaw", on, bevel=0.008)
            for i in range(3):
                k.block("CapSlit", (0.05, 0.006, 0.012), tk.frame(((i - 1) * 0.065, 0.216, 0.445)), self.glow(1.6), "Yaw", on, bevel=0.0)
            k.block("CapFoot", (cw * 0.9, 0.08, 0.03), tk.frame((0, 0.165, 0.39)), tk.GUNMETAL, "Yaw", on)
        for v in ("T2", "T3A", "T3B"):
            for s in (-1, 1):
                c = V(s * 0.195, 0.05, 0.345)
                k.cyl("SideCap", c + V(0, 0.09, 0), c - V(0, 0.09, 0), 0.042, look=tk.GUNMETAL.but(rings=0.03), bone="Yaw", on=[v], sides=14)
                k.torus("SideCapGlow", c, V(0, 1, 0), 0.043, 0.006, self.glow(1.4), "Yaw", [v], seg=14, sides=3)
                k.cyl("SideCapEnd", c + V(0, 0.09, 0), c + V(0, 0.096, 0), 0.03, look=tk.YELLOW, bone="Yaw", on=[v], sides=10, bevel=0.0)
        k.cyl("Drum", V(-0.13, 0.215, 0.46), V(0.13, 0.215, 0.46), 0.068, look=tk.GUNMETAL.but(rings=0.04), bone="Yaw", on=["T3B"], sides=18)
        k.torus("DrumHazard", V(0, 0.215, 0.46), XAX, 0.069, 0.01, tk.HAZARD, "Yaw", ["T3B"], seg=18, sides=4)
        for x in (-0.13, 0.13):
            k.cyl("DrumCap", V(x, 0.215, 0.46), V(x * 1.06, 0.215, 0.46), 0.05, look=tk.DARK, bone="Yaw", on=["T3B"], sides=12, bevel=0.0)

        for v, half in (("T1", 0.085), ("T2", 0.09), ("T3A", 0.14), ("T3B", 0.095)):
            k.block("Breech", (half * 2, 0.16, 0.085), tk.frame((0, -0.04, z)), tk.GUNMETAL, "Pitch", [v], bevel=0.008)
            k.cyl("Trunnion", (-half - 0.025, -0.03, z), (half + 0.025, -0.03, z), 0.03, look=tk.DARK, bone="Pitch", on=[v], sides=12)
            k.block("BreechTop", (half * 1.6, 0.12, 0.012), tk.frame((0, -0.04, z + 0.047)), PLAIN, "Pitch", [v])

        def rails(v, x, length, bone, collars, crown=False, spine=False):
            on = [v]
            b0 = V(x, -0.11, z)
            mid = b0 + FWD * (length / 2)
            for s in (-1, 1):
                k.block("Rail", (0.022, length, 0.05), tk.frame(mid + V(s * 0.026, 0, 0)), tk.STEEL.but(panel=0.0), bone, on, bevel=0.004)
                k.block("RailTip", (0.024, 0.03, 0.054), tk.frame(b0 + FWD * (length - 0.012) + V(s * 0.026, 0, 0)), tk.YELLOW, bone, on, bevel=0.003)
            # The plasma channel rides level with the rail tops, so from above it is a bright line between two dark ones.
            k.block("Channel", (0.016, length * 0.97, 0.034), tk.frame(mid + V(0, 0, 0.01)), self.glow(1.5), bone, on, bevel=0.0)
            k.block("Spine", (0.07, length * 0.9, 0.012), tk.frame(mid - V(0, 0, 0.031)), tk.DARK, bone, on, bevel=0.002)
            for i in range(collars):
                c = b0 + FWD * (0.06 + i * (length - 0.13) / max(collars - 1, 1))
                square_collar(k, "Collar", c, FWD, 0.059, 0.074, 0.026, tk.GUNMETAL, bone, on)
                k.block("CollarStud", (0.016, 0.014, 0.008), tk.frame(c + V(0, 0, 0.054)), self.glow(1.7), bone, on, bevel=0.0)
            if crown:
                c = b0 + FWD * (length - 0.01)
                for a in (45, 135, 225, 315):
                    d = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
                    k.block("Vane", (0.008, 0.09, 0.05), tk.frame(c + d * 0.06 + FWD * 0.02, z=d.cross(FWD).normalized(), y=FWD), PLAIN, bone, on, bevel=0.002)
                    k.block("VaneTip", (0.01, 0.02, 0.052), tk.frame(c + d * 0.06 + FWD * 0.065, z=d.cross(FWD).normalized(), y=FWD), self.glow(1.5), bone, on, bevel=0.0)
            if spine:
                for i in range(6):
                    c = b0 + FWD * (0.14 + i * 0.05) + V(0, 0, 0.06)
                    k.block("Radiator", (0.05, 0.012, 0.03), tk.frame(c), tk.GUNMETAL, bone, on, bevel=0.002)
            self.barrel(v, b0, FWD, length + 0.01)

        rails("T1", 0.0, 0.46, "Barrel_1", 3)
        rails("T2", 0.0, 0.54, "Barrel_1", 4)
        k.block("Brace", (0.05, 0.3, 0.03), tk.frame((0, -0.3, z - 0.05)), tk.GUNMETAL, "Barrel_1", ["T2", "T3B"])
        rails("T3A", -0.068, 0.5, "Barrel_1", 4)
        rails("T3A", 0.068, 0.5, "Barrel_2", 4)
        rails("T3B", 0.0, 0.66, "Barrel_1", 5, crown=True, spine=True)

    def more_clips(self, arm, v):
        def charge(t, f):
            e = bt.ease(t)
            out = {}
            for i, (_, d, _) in enumerate(self.barrels[v]):
                out[f"Barrel_{i + 1}"] = {"loc": d * (-0.02 * e)}
            return out
        return [bk.clip(arm, "ChargeUp", 24, charge)]


# -------------------------------------------------------------------------------------------------------------
# Acid Sprayer
# -------------------------------------------------------------------------------------------------------------

class Acid(Spec2):
    name = "Acid"
    accent_name = "acid"
    recoil = 0.012
    pitch_at = V(0, -0.07, 0.37)
    smoke_at = V(0, 0.06, 0.44)
    strip_clip = "Spray"
    strip_frames = (0, 2, 4, 6, 8, 10)

    def pieces(self, k):
        ring_base(k, 0.165)
        z = 0.37
        for v in self.variants:
            on = [v]
            k.block("Hull", (0.3, 0.3, 0.11), tk.frame((0, 0.02, 0.32)), tk.BLUE, "Yaw", on, taper=(0.88, 0.84), bevel=0.01)
            k.block("TopArmour", (0.25, 0.22, 0.022), tk.frame((0, 0.03, 0.385)), tk.WHITE, "Yaw", on, taper=(0.9, 0.86))
            k.block("Glacis", (0.26, 0.022, 0.09), tk.frame((0, -0.118, 0.318), rot=(-40, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            # Pressure gauge on the top deck, front left.
            k.lathe("Gauge", [(0, 0.394), (0.028, 0.394), (0.028, 0.412), (0, 0.412)], tk.frame((-0.085, -0.05, 0)), tk.GUNMETAL, "Yaw", on, sides=12)
            k.cyl("GaugeFace", (-0.085, -0.05, 0.411), (-0.085, -0.05, 0.415), 0.021, look=self.glow(1.3, lens=0.021), bone="Yaw", on=on, sides=12, bevel=0.0)
        self.slosh = {}

        def tank(v, x, r, h, y=0.11):
            on = [v]
            z0 = 0.33
            k.lathe("Tank", [(0, z0), (r * 0.75, z0), (r, z0 + 0.03), (r, z0 + h - 0.035), (r * 0.72, z0 + h), (0, z0 + h + 0.012)],
                    tk.frame((x, y, 0)), tk.WHITE.but(panel=0, bolts=False, rings=0.045), "Yaw", on, sides=16)
            k.lathe("TankBand", [(0, z0 + h * 0.2), (r + 0.004, z0 + h * 0.2), (r + 0.004, z0 + h * 0.34), (0, z0 + h * 0.34)], tk.frame((x, y, 0)),
                    tk.BLUE.but(panel=0), "Yaw", on, sides=16, bevel=0.002)
            k.cyl("TankCap", (x, y, z0 + h + 0.005), (x, y, z0 + h + 0.03), 0.022, look=tk.YELLOW, bone="Yaw", on=on, sides=10, lod=False)
            # The sight glass on the camera side and the bright meniscus that sloshes.
            gz = z0 + h * 0.6
            n = V(0, 1, 0)
            k.block("GlassFrame", (0.042, h * 0.62, 0.012), tk.frame(V(x, y + r - 0.001, gz), z=n), tk.DARK, "Yaw", on, bevel=0.002)
            k.block("Glass", (0.026, h * 0.54, 0.014), tk.frame(V(x, y + r + 0.001, gz), z=n), self.glow(0.95), "Yaw", on, bevel=0.0)
            # A glowing level band round the tank, so the acid reads from every side, not only through the glass.
            k.torus("AcidBand", V(x, y, gz - h * 0.18), UP, r + 0.003, 0.008, self.glow(1.2), "Yaw", on, seg=14, sides=4)
            level = V(x, y + r + 0.004, gz + h * 0.05)
            k.block("Meniscus", (0.03, 0.012, 0.012), tk.frame(level, z=n), self.glow(2.0), "Slosh", on, bevel=0.0)
            self.slosh.setdefault(v, []).append(level)
            rod(k, "Hose", V(x, y - r * 0.6, z0 + 0.03), V(x * 0.4, -0.04, z0 + 0.06), 0.013, tk.RUBBER, "Yaw", on, sides=8)

        tank("T1", 0.0, 0.1, 0.2)
        for v in ("T2", "T3A"):
            tank(v, -0.08, 0.072, 0.19)
            tank(v, 0.08, 0.072, 0.19)
            k.block("Manifold", (0.2, 0.05, 0.04), tk.frame((0, 0.11, 0.35)), tk.GUNMETAL, "Yaw", [v])
        # T3B: a big spherical pressure tank with a hazard band and two gas bottles.
        on = ["T3B"]
        sc = V(0, 0.1, 0.5)
        k.lathe("Sphere", sphere_prof(0.11, 8), tk.frame(sc), tk.WHITE.but(panel=0, bolts=False), "Yaw", on, sides=18, bevel=0.0)
        k.lathe("SphereBand", [(0, -0.018), (0.113, -0.018), (0.113, 0.018), (0, 0.018)], tk.frame(sc), tk.HAZARD, "Yaw", on, sides=18, bevel=0.002)
        k.torus("AcidBand", sc - UP * 0.045, UP, 0.1015, 0.008, self.glow(1.2), "Yaw", on, seg=18, sides=4)
        k.lathe("SphereSaddle", [(0, 0.385), (0.07, 0.385), (0.06, 0.41), (0, 0.41)], tk.frame((0, 0.1, 0)), tk.GUNMETAL, "Yaw", on, sides=12)
        k.block("GlassFrame", (0.04, 0.09, 0.012), tk.frame(sc + V(0, 0.108, 0.035), z=V(0, 1, 0.3)), tk.DARK, "Yaw", on, bevel=0.002)
        k.block("Glass", (0.026, 0.075, 0.014), tk.frame(sc + V(0, 0.11, 0.035), z=V(0, 1, 0.3)), self.glow(0.95), "Yaw", on, bevel=0.0)
        level = sc + V(0, 0.114, 0.04)
        k.block("Meniscus", (0.03, 0.012, 0.012), tk.frame(level, z=V(0, 1, 0.3)), self.glow(2.0), "Slosh", on, bevel=0.0)
        self.slosh["T3B"] = [level]
        for s in (-1, 1):
            b = V(s * 0.17, 0.09, 0.3)
            k.cyl("Bottle", b, b + UP * 0.15, 0.026, look=tk.BLUE.but(panel=0, rings=0.05), bone="Yaw", on=on, sides=12)
            k.lathe("BottleCap", [(0, 0), (0.026, 0), (0.016, 0.025), (0, 0.03)], tk.frame(b + UP * 0.15), PLAIN, "Yaw", on, sides=12)
        rod(k, "Hose", V(0, 0.02, 0.43), V(0, -0.04, 0.4), 0.014, tk.RUBBER, "Yaw", on, sides=8)

        for v, half in (("T1", 0.07), ("T2", 0.08), ("T3A", 0.11), ("T3B", 0.07)):
            k.cyl("Mantlet", (-half, -0.07, z), (half, -0.07, z), 0.045, look=tk.GUNMETAL, bone="Pitch", on=[v], sides=14)
            k.block("MantletPlate", (half * 1.7, 0.035, 0.08), tk.frame((0, -0.105, z)), tk.BLUE.but(panel=0), "Pitch", [v])

        def nozzle(v, x, length, flare, bone, yaw=0.0, r=0.03, collars=0):
            on = [v]
            d = Vector((math.sin(math.radians(yaw)), -math.cos(math.radians(yaw)), 0))
            b0 = V(x, -0.115, z)
            L = length
            prof = [(0, 0), (r, 0), (r, L * 0.55), (r * 0.82, L * 0.6), (r * 0.82, L * 0.8), (flare, L), (flare * 0.72, L), (flare * 0.55, L - 0.02),
                    (0, L - 0.02)]
            k.lathe("Nozzle", prof, tk.frame(b0, z=d), tk.GUNMETAL.but(rings=0.03), bone, on, sides=12)
            k.torus("NozzleGlow", b0 + d * (L * 0.84), d, r * 0.95, 0.006, self.glow(1.6), bone, on, seg=12, sides=4)
            k.torus("NozzleRing", b0 + d * 0.04, d, r + 0.004, 0.006, tk.YELLOW, bone, on, seg=14, sides=4, lod=False)
            for i in range(collars):
                c = b0 + d * (0.08 + i * 0.07)
                k.cyl("PressureCollar", c - d * 0.012, c + d * 0.012, r + 0.012, look=tk.GUNMETAL, bone=bone, on=on, sides=12)
            self.barrel(v, b0, d, L + 0.005)

        nozzle("T1", 0.0, 0.25, 0.052, "Barrel_1")
        nozzle("T2", 0.0, 0.3, 0.058, "Barrel_1")
        k.block("Guard", (0.13, 0.13, 0.014), tk.frame((0, -0.2, z + 0.055)), tk.WHITE.but(panel=0.06), "Barrel_1", ["T2"])
        k.block("GuardStripe", (0.12, 0.014, 0.016), tk.frame((0, -0.262, z + 0.055)), tk.HAZARD, "Barrel_1", ["T2"], bevel=0.0)
        k.block("FanManifold", (0.19, 0.06, 0.06), tk.frame((0, -0.11, z)), tk.GUNMETAL, "Pitch", ["T3A"])
        for i, (x, yaw) in enumerate(((-0.055, -20.0), (0.0, 0.0), (0.055, 20.0))):
            nozzle("T3A", x, 0.24, 0.046, f"Barrel_{i + 1}", yaw=yaw, r=0.026)
        nozzle("T3B", 0.0, 0.42, 0.04, "Barrel_1", r=0.022, collars=3)
        for v, levels in self.slosh.items():
            c = sum(levels, Vector()) / len(levels)
            self.extra_bones[v] = [tk.Bone("Slosh", c, UP, "Yaw", length=0.03)]

    def more_clips(self, arm, v):
        def spray(t, f):
            out = {"Slosh": {"loc": UP * (0.006 * math.sin(4 * math.pi * t)), "rot": (V(0, 1, 0), 0.12 * math.sin(2 * math.pi * t))},
                   "Pitch": {"rot": (XAX, 0.012 * math.sin(6 * math.pi * t))}}
            for i, (_, d, _) in enumerate(self.barrels[v]):
                out[f"Barrel_{i + 1}"] = {"loc": d * (-0.004 * (1 + math.sin(2 * math.pi * 3 * t + i)))}
            return out

        def slosh(t, f):
            return {"Slosh": {"loc": UP * (0.004 * math.sin(2 * math.pi * t)), "rot": (V(0, 1, 0), 0.07 * math.sin(2 * math.pi * t + 0.6))}}
        return [bk.clip(arm, "Spray", 12, spray, loop=True), bk.clip(arm, "Slosh", 60, slosh, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Hybrids
# -------------------------------------------------------------------------------------------------------------

class Artillery(Spec2):
    """Cannon + Flak: the Cannon's wedge hull, skirts and box brake carrying the Flak's elevated gun, shield and drums."""
    name = "Artillery"
    variants = ["T1", "T2", "T3"]
    accent_name = "amber"
    recoil = 0.07
    pitch_at = V(0, 0.0, 0.42)
    smoke_at = V(0, 0.12, 0.42)
    preview_pitch = 0.0
    head_scale = 1.22

    def pieces(self, k):
        ring_base(k, 0.2)
        t = V(0, 0, 0.42)
        for v in self.variants:
            on = [v]
            wide = 1.06 if v == "T3" else 1.0
            k.block("Hull", (0.4 * wide, 0.38, 0.12), tk.frame((0, 0.04, 0.305)), tk.BLUE, "Yaw", on, taper=(0.88, 0.78), shift=(0, 0.03), bevel=0.012)
            k.block("TopArmour", (0.33 * wide, 0.27, 0.026), tk.frame((0, 0.07, 0.376)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (0.36 * wide, 0.022, 0.13), tk.frame((0, -0.13, 0.3), rot=(-52, 0, 0)), tk.WHITE.but(panel=0.09), "Yaw", on)
            k.block("Slit", (0.07, 0.008, 0.012), tk.frame((0.1, -0.12, 0.345), rot=(-52, 0, 0)), self.glow(1.5), "Yaw", on, bevel=0.002)
            for sx in (-1, 1):
                k.block("SideLight", (0.012, 0.16, 0.014), tk.frame((sx * 0.19 * wide, 0.05, 0.33), rot=(0, sx * 12, 0)), self.glow(1.3), "Yaw", on, bevel=0.002)
                k.block("Cheek", (0.04, 0.17, 0.15), tk.frame((sx * 0.15, 0.01, 0.42)), tk.WHITE.but(panel=0.07), "Yaw", on, taper=(1.0, 0.65))
                k.cyl("TrunnionStub", (sx * 0.125, 0, 0.42), (sx * 0.175, 0, 0.42), 0.032, look=tk.GUNMETAL, bone="Pitch", on=on, sides=12)
            # The Flak's ammunition drums on the cheeks: left on T1, both from T2.
            for s in ((-1,) if v == "T1" else (-1, 1)):
                c = V(s * 0.235, 0.06, 0.4)
                k.cyl("Drum", c - V(0.03, 0, 0), c + V(0.03, 0, 0), 0.066, look=tk.GUNMETAL.but(rings=0.02), bone="Yaw", on=on, sides=16)
                k.torus("DrumBand", c, XAX, 0.067, 0.007, tk.YELLOW, "Yaw", on, seg=14, sides=4)
                k.cyl("DrumCap", c + V(s * 0.03, 0, 0), c + V(s * 0.036, 0, 0), 0.044, look=tk.DARK, bone="Yaw", on=on, sides=12, bevel=0.0)
        for v in ("T2", "T3"):
            for s in (-1, 1):
                k.block("Skirt", (0.024, 0.36, 0.12), tk.frame((s * 0.225, 0.04, 0.29), rot=(0, s * 20, 0)), tk.WHITE.but(panel=0.09), "Yaw", [v])
                k.block("SkirtStripe", (0.026, 0.3, 0.022), tk.frame((s * 0.245, 0.04, 0.245), rot=(0, s * 20, 0)), tk.HAZARD, "Yaw", [v], bevel=0.002)
        # Radar: a mast with a small dish on T2, a big tracking dish on T3; T3 also carries the Cannon's shell rack.
        for v, r, h in (("T2", 0.055, 0.12), ("T3", 0.1, 0.16)):
            base = V(0.1, 0.17, 0.38)
            k.cyl("Mast", base, base + UP * h, 0.012, look=tk.GUNMETAL, bone="Yaw", on=[v], sides=8)
            dish = base + UP * h
            m = tk.frame(dish, z=V(0, 0.5, 0.86))
            k.lathe("Dish", [(0, 0), (r, 0.02), (r * 1.02, 0.03), (r * 0.96, 0.034), (0, 0.012)], m, PLAIN, "Yaw", [v], sides=16, bevel=0.0)
            k.cyl("Feedhorn", dish, dish + V(0, 0.5, 0.86) * 0.05, 0.007, look=self.glow(1.5), bone="Yaw", on=[v], sides=6, bevel=0.0)
        k.block("Rack", (0.2, 0.07, 0.07), tk.frame((-0.08, 0.235, 0.33)), tk.GUNMETAL, "Yaw", ["T3"])
        for i in range(3):
            x = -0.14 + i * 0.06
            k.cyl("Shell", (x, 0.235, 0.36), (x, 0.235, 0.42), 0.018, 0.012, look=tk.YELLOW.but(fam="metal", rough=0.35), bone="Yaw", on=["T3"], sides=10)

        k.block("Cradle", (0.17, 0.1, 0.22), tk.frame(t + D40 * 0.02, z=D40), tk.GUNMETAL, "Pitch")

        def shield(v, w, h):
            m = tk.frame(t + D40 * 0.13 + U40 * 0.015, z=D40, y=U40)
            k.block("Shield", (w, h, 0.024), m, tk.WHITE.but(panel=0.08), "Pitch", [v])
            m2 = tk.frame(t + D40 * 0.143 + U40 * (0.015 + h / 2 - 0.016), z=D40, y=U40)
            k.block("ShieldStripe", (w * 0.96, 0.024, 0.008), m2, tk.HAZARD, "Pitch", [v], bevel=0.0)

        def gun(v, x, r, length, bone, extractor=False):
            b0 = t + V(x, 0, 0) + D40 * 0.05
            k.barrel("Tube", b0, b0 + D40 * length, r, r * 0.62, tk.GUNMETAL, bone, [v], sides=14)
            k.lathe("Jacket", [(0, 0), (r * 1.3, 0), (r * 1.3, 0.1), (0, 0.11)], tk.frame(b0 + D40 * 0.04, z=D40), tk.STEEL.but(rings=0.02), bone, [v],
                    sides=12)
            if extractor:
                k.lathe("Extractor", [(0, 0), (r * 1.1, 0), (r * 1.4, 0.02), (r * 1.4, 0.06), (r * 1.1, 0.08), (0, 0.08)],
                        tk.frame(b0 + D40 * (length * 0.5), z=D40), tk.STEEL, bone, [v], sides=12)
            k.torus("MuzzleRing", b0 + D40 * (length - 0.015), D40, r * 1.06, 0.007, tk.YELLOW, bone, [v], seg=12, sides=4)
            k.block("Brake", (r * 2.4, r * 1.8, 0.07), tk.frame(b0 + D40 * (length + 0.03), z=D40, y=U40), tk.DARK.but(panel=0.024), bone, [v], bevel=0.006)
            self.barrel(v, b0, D40, length + 0.065)

        shield("T1", 0.26, 0.15)
        gun("T1", 0.0, 0.042, 0.33, "Barrel_1")
        shield("T2", 0.28, 0.16)
        gun("T2", 0.0, 0.045, 0.37, "Barrel_1", extractor=True)
        shield("T3", 0.32, 0.18)
        gun("T3", -0.068, 0.038, 0.37, "Barrel_1", extractor=True)
        gun("T3", 0.068, 0.038, 0.37, "Barrel_2", extractor=True)


class StormRifle(Spec2):
    """Gun Turret + Tesla Coil: the Gun's head and barrels with Tesla coils, a small coil spire and a Crysium cell."""
    name = "StormRifle"
    variants = ["T1", "T2", "T3"]
    accent_name = "storm"
    recoil = 0.03
    pitch_at = V(0, -0.06, 0.36)
    smoke_at = V(0, 0.08, 0.44)

    def pieces(self, k):
        ring_base(k, 0.165)
        z = 0.36
        for v, w, d in (("T1", 0.3, 0.32), ("T2", 0.33, 0.34), ("T3", 0.35, 0.36)):
            on = [v]
            k.block("Hull", (w, d, 0.13), tk.frame((0, 0.03, 0.335)), tk.BLUE, "Yaw", on, taper=(0.86, 0.82), shift=(0, 0.015), bevel=0.01)
            k.block("TopArmour", (w * 0.84, d * 0.8, 0.026), tk.frame((0, 0.045, 0.41)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (w * 0.88, 0.024, 0.1), tk.frame((0, -0.128, 0.33), rot=(-38, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            k.block("Eye", (0.05, 0.008, 0.014), tk.frame((w * 0.24, -0.15, 0.352), rot=(-38, 0, 0)), self.glow(1.5), "Yaw", on, bevel=0.002)
            # The Gun's power cell is a caged Crysium cell here (left side).
            crysium_cell(k, on, V(-(w / 2 + 0.045), 0.05, 0.27), "Yaw", height=0.09, glow_name="storm")
            # A small Tesla spire on the back of the hull.
            h = {"T1": 0.15, "T2": 0.19, "T3": 0.24}[v]
            n = {"T1": 2, "T2": 3, "T3": 3}[v]
            sb = V(0.06, 0.12, 0.0)
            coil_stack(k, on, 0.45, 0.42 + h - 0.03, n, 0.052, 0.04, self.glow(1.1), "Yaw", column=(0.02, 0.016), base=sb, tube=0.01, seg=12,
                       sides=3, disc=10)
            top = sb + UP * (0.42 + h + 0.02)
            ball(k, "SpireBall", top, 0.024, self.glow(1.4, lens=0.024), "Yaw", on, sides=10, n=4)
            k.torus("SpireCage", top, XAX, 0.031, 0.004, tk.GUNMETAL, "Yaw", on, seg=8, sides=3, lod=False)
            for i in range(3 if v != "T3" else 4):
                a = 2 * math.pi * i / (3 if v != "T3" else 4) + 0.4
                dd = V(math.cos(a), math.sin(a), 0)
                p0 = top - UP * 0.03 + dd * 0.02
                p1 = p0 + dd * 0.045 + UP * 0.03
                k.cyl("SpireProng", p0, p1, 0.005, 0.004, look=tk.GUNMETAL, bone="Yaw", on=on, sides=5, bevel=0.0, lod=False)
                ball(k, "SpireTip", p1, 0.009, self.glow(1.7), "Yaw", on, sides=6, n=3, lod=False)
        for v in ("T2", "T3"):
            for s in (-1, 1):
                k.block("Cheek", (0.022, 0.2, 0.09), tk.frame((s * 0.17, -0.05, 0.37), rot=(0, s * 18, 0)), tk.WHITE.but(panel=0.07), "Yaw", [v])
        for v, half in (("T1", 0.1), ("T2", 0.13), ("T3", 0.11)):
            k.cyl("Mantlet", (-half, -0.06, z), (half, -0.06, z), 0.056, look=tk.GUNMETAL, bone="Pitch", on=[v], sides=16)
            k.block("MantletPlate", (half * 1.6, 0.04, 0.1), tk.frame((0, -0.105, z)), tk.BLUE.but(panel=0), "Pitch", [v])

        def rifle(v, x, length, bone):
            b0 = V(x, -0.12, z)
            k.barrel("Tube", b0, V(x, -0.12 - length, z), 0.021, 0.012, tk.GUNMETAL, bone, [v])
            jl = min(0.22, length * 0.6)
            k.lathe("Jacket", [(0, 0), (0.036, 0), (0.04, 0.01), (0.04, jl), (0.032, jl + 0.014), (0, jl + 0.014)], tk.frame(b0, z=FWD),
                    tk.STEEL.but(rings=0.024), bone, [v], sides=14)
            for zz in (0.05, 0.11, 0.17):
                k.torus("Coil", b0 + FWD * zz, FWD, 0.046, 0.0085, self.glow(1.2), bone, [v], seg=16, sides=4)
            k.torus("MuzzleRing", b0 + FWD * (length - 0.02), FWD, 0.025, 0.006, tk.YELLOW, bone, [v], seg=14, sides=4)
            self.barrel(v, b0, FWD, length + 0.005)

        rifle("T1", 0.0, 0.35, "Barrel_1")
        rifle("T2", -0.052, 0.36, "Barrel_1")
        rifle("T2", 0.052, 0.36, "Barrel_2")
        b0 = V(0, -0.11, z)
        on = ["T3"]
        k.cyl("Hub", b0, b0 + FWD * 0.03, 0.045, look=tk.DARK, bone="Barrel_1", on=on, sides=14)
        for i in range(4):
            a = math.radians(45 + 90 * i)
            off = V(math.cos(a) * 0.03, 0, math.sin(a) * 0.03)
            k.barrel("RotaryTube", b0 + off, b0 + off + FWD * 0.4, 0.014, 0.008, tk.GUNMETAL, "Barrel_1", on, sides=8)
        k.lathe("Shroud", [(0, 0), (0.06, 0), (0.068, 0.012), (0.068, 0.19), (0.058, 0.205), (0, 0.205)], tk.frame(b0 + FWD * 0.02, z=FWD),
                tk.STEEL.but(rings=0.03), "Barrel_1", on, sides=16)
        for zz in (0.06, 0.12, 0.18):
            k.torus("Coil", b0 + FWD * zz, FWD, 0.072, 0.009, self.glow(1.2), "Barrel_1", on, seg=14, sides=4)
        k.cyl("Clamp", b0 + FWD * 0.33, b0 + FWD * 0.35, 0.05, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=14)
        k.torus("MuzzleRing", b0 + FWD * 0.39, FWD, 0.048, 0.006, tk.YELLOW, "Barrel_1", on, seg=16, sides=4)
        self.barrel("T3", b0, FWD, 0.41)
        k.cyl("Drum", V(0.16, 0.0, 0.33), V(0.24, 0.0, 0.33), 0.08, look=tk.GUNMETAL.but(rings=0.03), bone="Pitch", on=on, sides=16)
        k.torus("DrumBand", V(0.2, 0.0, 0.33), XAX, 0.081, 0.007, tk.YELLOW, "Pitch", on, seg=14, sides=4)
        k.block("Feed", (0.08, 0.05, 0.03), tk.frame((0.12, -0.05, 0.35)), tk.DARK, "Pitch", on)

    def more_clips(self, arm, v):
        if v != "T3":
            return []
        return [bk.clip(arm, "Spin", 12, lambda t, f: {"Barrel_1": {"rot": (FWD, 2 * math.pi * t)}}, loop=True)]


class CryoBeam(Spec2):
    """Laser + Cryo: the Laser's fork yoke and lens emitter, a frosted coolant tank piggyback, vane flaps, canisters."""
    name = "CryoBeam"
    variants = ["T1", "T2", "T3"]
    accent_name = "ice"
    recoil = 0.0
    pitch_at = V(0, 0, 0.43)
    smoke_at = V(0, 0.1, 0.5)
    strip_clip = "ChargeUp"
    strip_frames = (0, 6, 12, 18, 24)

    def pieces(self, k):
        ring_base(k, 0.15)
        z = 0.43
        k.block("YokeBase", (0.34, 0.18, 0.06), tk.frame((0, 0, 0.3)), tk.BLUE.but(panel=0.08), "Yaw", taper=(0.95, 0.9))
        for s in (-1, 1):
            k.block("Arm", (0.05, 0.13, 0.19), tk.frame((s * 0.15, 0, 0.405)), tk.BLUE.but(panel=0.07), "Yaw", taper=(1.0, 0.8))
            k.block("ArmCap", (0.056, 0.11, 0.022), tk.frame((s * 0.15, 0, 0.505)), tk.WHITE.but(panel=0), "Yaw")
            k.cyl("TrunnionStub", (s * 0.122, 0, z), (s * 0.18, 0, z), 0.032, look=tk.GUNMETAL, bone="Pitch", sides=12)
        # The Cryo's coolant canisters on the yoke, one per side, two from T2.
        for v in self.variants:
            ys = (0.035,) if v == "T1" else (0.045, -0.035)
            for s in (-1, 1):
                for y in ys:
                    b = V(s * 0.205, y, 0.28)
                    k.cyl("Canister", b, b + UP * 0.15, 0.024, look=FROST.but(rings=0.04), bone="Yaw", on=[v], sides=10)
                    k.lathe("CanisterCap", [(0, 0), (0.024, 0), (0.02, 0.016), (0, 0.022)], tk.frame(b + UP * 0.15), self.glow(1.6), "Yaw", [v], sides=10,
                            bevel=0.0)
                k.block("CanClamp", (0.06, 0.1 if v != "T1" else 0.05, 0.016), tk.frame((s * 0.19, 0.005 if v != "T1" else 0.035, 0.37)), tk.GUNMETAL,
                        "Yaw", [v], bevel=0.003)

        rear = V(0, 0.15, z)
        self.prongs = {}

        def emitter(v, lens_r, house_r, body_len, tank_r):
            on = [v]
            k.cyl("RearCap", rear + V(0, 0.01, 0), rear + FWD * 0.04, 0.058, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=16)
            k.lathe("Body", [(0, 0), (0.07, 0), (0.08, 0.02), (0.08, body_len - 0.03), (0.072, body_len), (0, body_len)],
                    tk.frame(rear + FWD * 0.03, z=FWD), tk.WHITE.but(rings=0.07), "Barrel_1", on, sides=18)
            front = rear + FWD * (0.03 + body_len)
            k.lathe("Housing", [(0, 0), (house_r * 0.8, 0), (house_r, 0.02), (house_r, 0.045), (lens_r * 1.05, 0.05), (0, 0.05)],
                    tk.frame(front, z=FWD), tk.DARK, "Barrel_1", on, sides=20)
            k.lathe("Lens", [(0, 0), (lens_r, 0), (lens_r * 0.92, 0.012), (lens_r * 0.6, 0.022), (0, 0.027)],
                    tk.frame(front + FWD * 0.04, z=FWD), self.glow(1.0, lens=lens_r), "Barrel_1", on, sides=20, bevel=0.0)
            # The Cryo's vane flaps round the lens housing.
            for a in (45, 135, 225, 315):
                dd = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
                c = front + FWD * 0.03 + dd * (house_r + 0.012)
                m = tk.frame(c, z=dd, y=FWD) @ tk.frame(rot=(-22, 0, 0))
                k.block("Vane", (0.05, 0.075, 0.01), m, PLAIN, "Barrel_1", on, bevel=0.002)
            # The piggyback coolant tank with frost rings.
            tc = rear + FWD * 0.02 + UP * (0.08 + tank_r)
            tl = body_len * 0.75
            k.lathe("Tank", [(0, 0), (tank_r * 0.7, 0), (tank_r, 0.02), (tank_r, tl - 0.02), (tank_r * 0.7, tl), (0, tl)], tk.frame(tc, z=FWD),
                    FROST, "Barrel_1", on, sides=16)
            for i in range(2 if v == "T1" else 3):
                k.torus("FrostRing", tc + FWD * (0.05 + i * (tl - 0.1) / 2), FWD, tank_r + 0.002, 0.006, self.glow(1.4), "Barrel_1", on, seg=16, sides=3)
            for zz in (0.04, tl - 0.04):
                k.block("TankStrap", (0.03, 0.02, 0.07), tk.frame(tc + FWD * zz - UP * (tank_r + 0.02)), tk.GUNMETAL, "Barrel_1", on)
            self.barrel(v, rear, FWD, 0.03 + body_len + 0.07)
            return front, tc, tl

        emitter("T1", 0.07, 0.092, 0.27, 0.045)
        emitter("T2", 0.09, 0.115, 0.27, 0.05)
        f3, tc3, tl3 = emitter("T3", 0.112, 0.138, 0.29, 0.055)
        for i in range(4):
            c = tc3 + FWD * (0.04 + i * 0.045) + UP * 0.055
            k.block("FrostFin", (0.012, 0.03, 0.05), tk.frame(c), PLAIN, "Barrel_1", ["T3"], bevel=0.002)
        bones = []
        for i in range(3):
            a = math.radians(90 + 120 * i)
            radial = V(math.cos(a), 0, math.sin(a))
            base = f3 + FWD * 0.03 + radial * (0.138 + 0.008)
            tip = base + FWD * 0.15 - radial * 0.15 * 0.22
            name = f"Prong_{i + 1}"
            k.block("Prong", (0.022, 0.016, (tip - base).length), tk.frame((base + tip) / 2, z=tip - base, y=radial), PLAIN, name, ["T3"], bevel=0.003)
            k.block("ProngTip", (0.024, 0.018, 0.022), tk.frame(tip, z=tip - base, y=radial), self.glow(1.6), name, ["T3"], bevel=0.002)
            bones.append(tk.Bone(name, base, (tip - base), "Barrel_1", length=0.05))
            self.prongs.setdefault("T3", []).append((name, radial))
        self.extra_bones["T3"] = bones

    def clips(self, arm, v):
        prongs = self.prongs.get(v, [])

        def close(amount):
            return {name: {"rot": (radial.cross(FWD).normalized(), -0.28 * amount)} for name, radial in prongs}

        def charge(t, f):
            e = bt.ease(t)
            spec = close(e)
            spec["Barrel_1"] = {"loc": FWD * (0.018 * e)}
            return spec

        def beam(t, f):
            spec = close(1.0 + 0.06 * math.sin(2 * math.pi * 2 * t))
            spec["Barrel_1"] = {"loc": FWD * (0.018 + 0.002 * math.sin(2 * math.pi * 3 * t))}
            return spec
        return [bk.clip(arm, "IdleSweep", 120, self.sweep, loop=True), bk.clip(arm, "ChargeUp", 24, charge), bk.clip(arm, "Beam", 12, beam, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Cryo Turret, Support, Rocket
# -------------------------------------------------------------------------------------------------------------

class Cryo(Spec2):
    name = "Cryo"
    variants = ["T1", "T2", "T3"]
    accent_name = "frost"
    recoil = 0.01
    pitch_at = V(0, 0.0, 0.405)
    smoke_at = V(0, 0.1, 0.46)
    strip_clip = "Fire_1"

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
                k.cyl("TrunnionStub", (s * 0.105, 0, z), (s * 0.15, 0, z), 0.03, look=tk.GUNMETAL, bone="Pitch", on=on, sides=12)
            spots = {"T1": [(-1, 0.06)], "T2": [(-1, 0.06), (1, 0.06)], "T3": [(-1, 0.09), (-1, -0.01), (1, 0.09), (1, -0.01)]}[v]
            for s, y in spots:
                b = V(s * 0.2, y, 0.28)
                k.cyl("Canister", b, b + UP * 0.16, 0.03, look=FROST.but(rings=0.045), bone="Yaw", on=on, sides=12)
                k.lathe("CanisterCap", [(0, 0), (0.03, 0), (0.024, 0.02), (0, 0.028)], tk.frame(b + UP * 0.16), self.glow(1.6), "Yaw", on, sides=12, bevel=0.0)
                k.torus("CanBand", b + UP * 0.05, UP, 0.031, 0.005, tk.YELLOW, "Yaw", on, seg=12, sides=3)
                rod(k, "CanPipe", b + UP * 0.12 - V(s * 0.02, 0, 0), V(s * 0.12, y, 0.43), 0.008, tk.RUBBER, "Yaw", on)

        def gun(v, length, r):
            on = [v]
            b0 = V(0, 0.12, z)
            k.lathe("Cylinder", [(0, 0), (r * 0.6, 0), (r, 0.03), (r, length), (0, length)], tk.frame(b0, z=FWD), FROST, "Barrel_1", on, sides=18)
            for i in range(3):
                k.torus("FrostRing", b0 + FWD * (0.08 + i * (length - 0.12) / 2), FWD, r + 0.003, 0.008, self.glow(1.45), "Barrel_1", on, seg=18, sides=4)
            front = b0 + FWD * length
            k.lathe("Nozzle", [(0, 0), (r * 0.7, 0), (r * 0.62, 0.03), (r * 0.85, 0.08), (r * 0.7, 0.08), (0, 0.06)], tk.frame(front, z=FWD), tk.DARK,
                    "Barrel_1", on, sides=16)
            k.cyl("NozzleGlow", front + FWD * 0.064, front + FWD * 0.07, r * 0.66, look=self.glow(1.3, lens=r * 0.6), bone="Barrel_1", on=on, sides=16,
                  bevel=0.0)
            for a in (45, 135, 225, 315):
                dd = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
                c = front + FWD * 0.05 + dd * (r * 0.85 + 0.01)
                m = tk.frame(c, z=dd, y=FWD) @ tk.frame(rot=(-25, 0, 0))
                k.block("Vane", (0.055, 0.07, 0.01), m, PLAIN, "Barrel_1", on, bevel=0.002)
            k.block("Saddle", (0.1, 0.12, 0.04), tk.frame(V(0, 0.0, z - r + 0.005)), tk.GUNMETAL, "Barrel_1", on)
            self.barrel(v, b0, FWD, length + 0.09)
            return b0

        gun("T1", 0.32, 0.075)
        gun("T2", 0.36, 0.082)
        b3 = gun("T3", 0.38, 0.088)
        for i in range(5):
            c = b3 + FWD * (0.07 + i * 0.055) + UP * 0.098
            k.block("FrostFin", (0.012, 0.035, 0.05 - abs(i - 2) * 0.008), tk.frame(c), PLAIN, "Barrel_1", ["T3"], bevel=0.002)


class Support(Spec2):
    name = "Support"
    variants = ["T1", "T2", "T3"]
    accent_name = "signal"
    close_z, close_dist, macro_z = 0.55, 2.0, 0.72
    recoil = 0.0
    pitch_at = V(0, 0, 0.6)
    smoke_at = V(0, 0.08, 0.42)
    strip_clip = "Fire_1"

    def pieces(self, k):
        ring_base(k, 0.155)
        oct_ = math.pi / 8
        k.lathe("Drum", [(0, 0.27), (0.16, 0.27), (0.165, 0.29), (0.15, 0.33), (0, 0.33)], tk.frame(), tk.BLUE.but(panel=0.07), "Yaw", sides=8, start=oct_)
        k.lathe("DrumCap", [(0, 0.326), (0.148, 0.326), (0.128, 0.35), (0, 0.35)], tk.frame(), tk.WHITE.but(panel=0.09), "Yaw", sides=8, start=oct_)
        for i in range(4):
            a = math.radians(45 + 90 * i)
            d = V(math.cos(a), math.sin(a), 0)
            b = d * 0.19 + V(0, 0, 0.27)
            k.cyl("Stud", b, b + UP * 0.09, 0.02, 0.016, look=tk.GUNMETAL, bone="Yaw", sides=8)
            ball(k, "StudTip", b + UP * 0.1, 0.016, self.glow(1.7), "Yaw", None, sides=8, n=3)
        for v in self.variants:
            on = [v]
            n = {"T1": 3, "T2": 4, "T3": 5}[v]
            cz = {"T1": 0.6, "T2": 0.62, "T3": 0.64}[v]
            cr = {"T1": 0.052, "T2": 0.058, "T3": 0.066}[v]
            k.cyl("Mast", (0, 0, 0.34), (0, 0, cz - cr - 0.02), 0.038, 0.026, look=tk.GUNMETAL.but(rings=0.04), bone="Yaw", on=on, sides=12)
            k.lathe("MastCup", [(0, cz - cr - 0.035), (0.05, cz - cr - 0.035), (0.06, cz - cr - 0.015), (0, cz - cr - 0.02)], tk.frame(), PLAIN, "Yaw", on,
                    sides=12)
            for i in range(n):
                a = 2 * math.pi * i / n + math.pi / 2
                d = V(math.cos(a), math.sin(a), 0)
                p0, p1, p2 = d * 0.085 + V(0, 0, 0.34), d * 0.17 + V(0, 0, cz - 0.04), d * 0.14 + V(0, 0, cz + 0.11)
                for q0, q1 in ((p0, p1), (p1, p2)):
                    k.block("Fin", (0.04, 0.014, (q1 - q0).length + 0.012), tk.frame((q0 + q1) / 2, z=q1 - q0, y=d), PLAIN, "Yaw", on, taper=(0.85, 1.0),
                            bevel=0.003)
                k.block("FinGlow", (0.016, 0.016, 0.03), tk.frame(p2, z=p2 - p1, y=d), self.glow(1.6), "Yaw", on, bevel=0.0)
            core = V(0, 0, cz)
            k.lathe("Core", sphere_prof(cr, 6), tk.frame(core), self.glow(1.3, lens=cr), "Barrel_1", on, sides=14, bevel=0.0)
            k.torus("CoreRing", core, XAX, cr + 0.012, 0.006, tk.GUNMETAL, "Barrel_1", on, seg=16, sides=3)
            for j in range({"T1": 1, "T2": 2, "T3": 3}[v]):
                hz = cz - 0.06 + j * 0.06
                k.torus("Halo", V(0, 0, hz), UP, 0.215 + j * 0.01, 0.006, self.glow(1.3), "Yaw", on, seg=28, sides=3)
            self.barrel(v, core, UP, cr)

    def clips(self, arm, v):
        def pulse(t, f):
            return {"Barrel_1": {"loc": UP * (0.01 * math.sin(2 * math.pi * t)), "rot": (UP, 2 * math.pi * t)}}

        def fire(t, f):
            return {"Barrel_1": {"scale": 1.0 + 0.18 * bt.kick(f)}}
        return [bk.clip(arm, "IdleSweep", 90, pulse, loop=True), bk.clip(arm, "Fire_1", 10, fire)]


ELEV30 = math.radians(30)
D30 = Vector((0, -math.cos(ELEV30), math.sin(ELEV30)))
U30 = Vector((0, math.sin(ELEV30), math.cos(ELEV30)))


class Rocket(Spec2):
    name = "Rocket"
    variants = ["T1", "T2", "T3"]
    accent_name = "rocket"
    recoil = 0.03
    pitch_at = V(0, 0.0, 0.5)
    smoke_at = V(0, 0.12, 0.42)
    preview_pitch = 0.0

    def pieces(self, k):
        ring_base(k, 0.17)
        t = V(0, 0, 0.5)
        for v in self.variants:
            on = [v]
            k.block("Hull", (0.32, 0.32, 0.1), tk.frame((0, 0.03, 0.315)), tk.BLUE, "Yaw", on, taper=(0.88, 0.84), bevel=0.01)
            k.block("TopArmour", (0.24, 0.2, 0.02), tk.frame((0, 0.08, 0.375)), tk.WHITE, "Yaw", on, taper=(0.9, 0.86))
            for s in (-1, 1):
                k.block("Cheek", (0.035, 0.14, 0.15), tk.frame((s * (0.11 + 0.02 * (v == "T3")), 0.0, 0.45)), tk.WHITE.but(panel=0.07), "Yaw", on,
                        taper=(1.0, 0.7))
        for v in ("T2", "T3"):
            base = V(-0.1, 0.15, 0.38)
            k.cyl("Mast", base, base + UP * 0.1, 0.01, look=tk.GUNMETAL, bone="Yaw", on=[v], sides=8)
            m = tk.frame(base + UP * 0.1, z=V(0, 0.5, 0.86))
            k.lathe("Dish", [(0, 0), (0.05, 0.016), (0.052, 0.024), (0.048, 0.027), (0, 0.01)], m, PLAIN, "Yaw", [v], sides=14, bevel=0.0)
            k.cyl("Feedhorn", base + UP * 0.1, base + UP * 0.1 + V(0, 0.5, 0.86) * 0.04, 0.006, look=self.glow(1.5), bone="Yaw", on=[v], sides=6,
                  bevel=0.0)
        k.block("Cradle", (0.14, 0.12, 0.08), tk.frame(t, z=D30, y=U30), tk.GUNMETAL, "Pitch")
        tubes = {}
        for v, cols, rows in (("T1", 2, 2), ("T2", 3, 2), ("T3", 3, 3)):
            on = [v]
            cell = 0.072
            w, h, L = cols * cell + 0.03, rows * cell + 0.03, 0.3
            c = t + D30 * 0.05 + U30 * 0.01
            m = tk.frame(c, z=D30, y=U30)
            k.block("Pod", (w, h, L), m, tk.BLUE.but(panel=0.07), "Barrel_1", on, bevel=0.008)
            k.block("PodTop", (w * 0.9, 0.012, L * 0.9), tk.frame(c + U30 * (h / 2 + 0.004), z=D30, y=U30), PLAIN, "Barrel_1", on)
            k.block("PodStripe", (w + 0.004, h + 0.004, 0.02), tk.frame(c + D30 * (L / 2 - 0.03), z=D30, y=U30), tk.HAZARD, "Barrel_1", on, bevel=0.0)
            face = c + D30 * (L / 2)
            k.block("Face", (w * 0.94, h * 0.94, 0.012), tk.frame(face, z=D30, y=U30), tk.DARK, "Barrel_1", on, bevel=0.0)
            pts = []
            for r in range(rows):
                for col in range(cols):
                    p = face + XAX * ((col - (cols - 1) / 2) * cell) + U30 * ((r - (rows - 1) / 2) * cell) + D30 * 0.004
                    k.cyl("TubeMouth", p, p + D30 * 0.006, cell * 0.36, look=tk.RUBBER, bone="Barrel_1", on=on, sides=10, bevel=0.0)
                    k.lathe("Nose", [(0, 0), (cell * 0.22, 0), (cell * 0.16, 0.014), (0, 0.022)], tk.frame(p, z=D30), self.glow(1.5), "Barrel_1", on, sides=8,
                            bevel=0.0)
                    pts.append(p + D30 * 0.02)
            tubes[v] = pts
            if v == "T3":
                for s in (-1, 1):
                    for i in range(3):
                        mp = c + XAX * (s * (w / 2 + 0.03)) + U30 * ((i - 1) * 0.06) - D30 * 0.04
                        k.cyl("Standby", mp - D30 * 0.12, mp + D30 * 0.12, 0.022, look=tk.WHITE.but(panel=0, bolts=False), bone="Barrel_1", on=on, sides=10)
                        k.lathe("StandbyNose", [(0, 0), (0.022, 0), (0.012, 0.03), (0, 0.04)], tk.frame(mp + D30 * 0.12, z=D30), self.glow(1.4), "Barrel_1",
                                on, sides=10, bevel=0.0)
                    k.block("RackPlate", (0.012, 0.2, 0.24), tk.frame(c + XAX * (s * (w / 2 + 0.006)), z=D30, y=U30), tk.GUNMETAL, "Barrel_1", on)
            self.barrel(v, c, D30, L / 2 + 0.03)
        for v, pts in tubes.items():
            self.extra_bones[v] = [tk.Bone(f"Tube_{i + 1}", p, D30, "Barrel_1", deform=False, length=0.03) for i, p in enumerate(pts)]


SPECS = {"Tesla": Tesla, "Railgun": Railgun, "Acid": Acid, "Artillery": Artillery, "StormRifle": StormRifle, "CryoBeam": CryoBeam,
         "Cryo": Cryo, "Support": Support, "Rocket": Rocket}


# -------------------------------------------------------------------------------------------------------------
# Build, export, preview
# -------------------------------------------------------------------------------------------------------------

def build_tower(spec, bake=True, export=True):
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, 1024, 512, bake, samples=12)
    report["variants"] = {}
    if not export:
        return kit, mat, report
    for v in spec.variants:
        objs = bt.build_variant(spec, kit, v, mat)
        arm, lod0, lod1 = objs
        acts = spec.clips(arm, v)
        bk.export_fbx(os.path.join(out_dir, f"{kit.name}_{v}.fbx"), objs, animated=True)
        report["variants"][v] = {
            "fbx": f"{kit.name}_{v}.fbx", "tris": C.tri_count(lod0), "tris_lod1": C.tri_count(lod1),
            "bones": [b.name for b in arm.data.bones], "deform_bones": [b.name for b in arm.data.bones if b.use_deform],
            "clips": {a.name: int(a.frame_range[1]) for a in acts},
            "muzzles": {f"Muzzle_{i + 1}": [round(c, 4) for c in m] for i, (_, _, m) in enumerate(spec.barrels[v])},
            "sockets": {b.name: [round(c, 4) for c in b.head] for b in spec.extra_bones.get(v, []) if not b.deform},
        }
        C.log(kit.name, v, report["variants"][v]["tris"], report["variants"][v]["tris_lod1"])
        bk.remove(objs)
        bk.clear_actions()
    report["pivot"] = "pad centre on the ground"
    report["yaw_height"] = bt.YAW_Z
    report["glow"] = list(spec.accent)
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    return kit, mat, report


def hide(objs, flag):
    for o in objs:
        o.hide_render = flag


def previews(spec, kit, mat, out):
    tag = spec.name.lower()
    stage = bt.stage_ground()
    rows = []
    spacing = 1.15
    n = len(spec.variants)
    xs = [-(i - (n - 1) / 2) * spacing for i in range(n)]
    for i, v in enumerate(spec.variants):
        objs = bt.build_variant(spec, kit, v, mat, name=f"{kit.name}_{v}_Rig", lod=False)
        objs[0].location = (xs[i], 0, 0)
        bt.pose(objs[0], 115, spec.preview_pitch, bt.stars_for(v))
        rows.append((v, objs))
    # Scale: round-7 towers at both ends and Skitters walking the road behind.
    cmp = bk.tower("Gun", "T1", (xs[0] + spacing, 0, 0)) + bk.tower("Cannon", "T2", (xs[-1] - spacing, 0, 0))
    sk = bk.skitters([(x, -1.25, 0) for x in (-2.3, -1.0, 0.4, 1.7)], yaw=math.radians(90))
    centre = V(0, 0, 0.3)
    bk.render_game(centre, os.path.join(out, f"{tag}_tiers_game.png"), zoom=1.0, width=640, height=360)
    bk.render_game(centre + V(0, -0.3, 0), os.path.join(out, f"{tag}_tiers_game_zoom.png"), zoom=2.4, width=1280, height=720)
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
    # Macro on the top tier's head.
    vl, ol = rows[-1]
    for vv, oo in rows:
        hide(oo, vv != vl)
    bk.render_close(V(xs[-1], -0.02, spec.macro_z), os.path.join(out, f"{tag}_macro.png"), 0.62, azimuth=35, elevation=30, lens=70, width=1280, height=720,
                    light_scale=0.6)
    # Clip strip on the top tier, from the side.
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


ROUND7 = ["Gun", "Cannon", "Laser", "Flak"]
TOP_TIER = {"Gun": "T3A", "Cannon": "T3B", "Laser": "T3A", "Flak": "T3A"}


def lineup(out):
    """Every tower (round 7 and round 11) in a row at game zoom, T1 in the back row and the top tier in front."""
    stage = bt.stage_ground()
    built = {}
    for n, cls in SPECS.items():
        spec = cls()
        kit, mat, _ = build_tower(spec, bake=False, export=False)
        built[n] = (spec, kit, mat)
    names = ROUND7 + list(SPECS)
    placed = []
    spacing = 1.08
    for row, top in enumerate((False, True)):
        for i, n in enumerate(names):
            x = (i - (len(names) - 1) / 2) * spacing
            y = row * 1.35
            if n in ROUND7:
                tier = TOP_TIER[n] if top else "T1"
                placed += bk.tower(n, tier, (x, y, 0), head_deg=100 + 12 * i)
            else:
                spec, kit, mat = built[n]
                tier = spec.variants[-2 if len(spec.variants) == 4 else -1] if top else "T1"
                objs = bt.build_variant(spec, kit, tier, mat, name=f"{kit.name}_{tier}_L{row}", lod=False)
                objs[0].location = (x, y, 0)
                bt.pose(objs[0], 100 + 12 * i, spec.preview_pitch, bt.stars_for(tier))
                placed += objs
    sk = bk.skitters([(x, -1.25, 0) for x in (-5.5, -3.2, -1.0, 1.4, 3.6, 5.8)], yaw=math.radians(90))
    bk.render_game(V(0, 0.3, 0.3), os.path.join(out, "towers2_lineup_game.png"), zoom=1.0, width=1280, height=720)
    half = (len(names) - 1) / 2 * spacing / 2
    bk.render_game(V(-half - 0.3, 0.4, 0.3), os.path.join(out, "towers2_lineup_zoom_left.png"), zoom=2.0, width=1280, height=720)
    bk.render_game(V(half + 0.3, 0.4, 0.3), os.path.join(out, "towers2_lineup_zoom_right.png"), zoom=2.0, width=1280, height=720)
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
    names = only.split(",") if isinstance(only, str) else list(SPECS)
    bake = not args.get("nobake")
    for n in names:
        spec = SPECS[n]()
        kit, mat, report = build_tower(spec, bake)
        if not args.get("norender"):
            previews(spec, kit, mat, out)
    C.save_blend("Towers2" if only is None else "Towers2_" + "_".join(names))


if __name__ == "__main__":
    main()
