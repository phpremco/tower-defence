"""Round 13: story props (see ArtSource/DESIGN_NOTES.md, round 13).

- Ora: the base AI as a hologram (faceted core, gyro rings, halo shards, gaze bar, emitter). UV1 = glitch bands.
  Clips Idle, Talk, Glitch.
- NTI_Emblem: the Nova Terra Industries badge (static).
- Pioneer1: the 2047 scout probe, intact (static, log portraits).
- Pioneer1_Wreck: the same probe crashed in a mire mound and overgrown (static decor for Mirewood).
- RadarConsole: the command-post radar desk; the radar screen and the two side screens are own meshes with 0..1 UVs.

Hard-surface kit (tower_kit pieces, one atlas, self-bake, four maps + Low set), one material per asset.

Out: `Exports/<Name>/<Name>.fbx`, maps, `<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_story_props.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Ora,Emblem,Pioneer,Wreck,Console]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP, FWD, XAX  # noqa: E402


def polar(a_deg, r, z):
    a = math.radians(a_deg)
    return V(math.cos(a) * r, math.sin(a) * r, z)


def seg(t, a, b):
    return min(1.0, max(0.0, (t - a) / (b - a)))


class Spec:
    name = ""
    tex, low = 1024, 512
    variants = ["Main"]
    extras = []          # (variant, object suffix)
    static = True

    def __init__(self):
        self.sockets = {}
        self.bone_list = [tk.Bone("Root", (0, 0, 0), UP, length=0.1)]

    def kit(self):
        k = tk.Kit(self.name, self.variants)
        self.pieces(k)
        return k

    def clips(self, arm):
        return []

    def post(self, objs):
        """Extra UV sets on the export objects."""


# -------------------------------------------------------------------------------------------------------------
# ORA
# -------------------------------------------------------------------------------------------------------------

class Ora(Spec):
    name = "Ora"
    static = False
    CORE = V(0, 0, 0.34)
    RINGS = [("Ring_1", 0.19, V(0.0, 0.12, 1.0)), ("Ring_2", 0.235, V(0.87, 0.0, 0.5)), ("Ring_3", 0.28, V(-0.87, 0.0, 0.5))]
    TOP = 0.62

    def pieces(self, k):
        on = ["Main"]
        c = self.CORE
        k.lathe("Emitter", [(0, 0), (0.17, 0), (0.185, 0.018), (0.15, 0.036), (0, 0.036)], tk.frame(), tk.GUNMETAL, "Root", on, sides=24)
        k.torus("EmitterRing", V(0, 0, 0.037), UP, 0.125, 0.008, sk.glow("ora", 2.0), "Root", on, seg=24, sides=4)
        k.lathe("EmitterLens", [(0, 0.036), (0.06, 0.036), (0, 0.05)], tk.frame(), sk.glow("ora_hot", 2.2), "Root", on, sides=12, bevel=0.0)
        for i in range(4):
            a = 45 + 90 * i
            k.cyl("Strand", polar(a, 0.12, 0.04), polar(a, 0.035, c.z - 0.12), 0.004, 0.002, look=sk.glow("ora", 0.9), bone="Float", on=on,
                  sides=4, bevel=0.0)
        sk.add(k, "Core", lambda: sk.bm_ico(0.1, 1), tk.frame(c), sk.glow("ora", 0.6), "Core", on)
        k.lathe("Inner", [(0, -0.058), (0.058, 0), (0, 0.058)], tk.frame(c), sk.glow("ora_hot", 2.4), "Inner", on, sides=4, bevel=0.0, start=0.0)
        k.block("GazeSlot", (0.25, 0.012, 0.034), tk.frame(c + V(0, -0.088, 0)), tk.DARK.but(panel=0), "Gaze", on, bevel=0.0)
        k.block("Gaze", (0.2, 0.03, 0.012), tk.frame(c + V(0, -0.1, 0)), sk.glow("ora_hot", 1.8), "Gaze", on, bevel=0.0)
        for name, r, axis in self.RINGS:
            for j in range(3):
                a0 = math.radians(j * 120 + 8)
                a1 = math.radians(j * 120 + 112)
                sk.add(k, name, (lambda r=r, a0=a0, a1=a1: sk.bm_arc_torus(r, 0.0075, a0, a1, 12, 4)), tk.frame(c, z=axis), sk.glow("ora", 0.75),
                       name, on)
            for j in range(3):
                a = math.radians(j * 120 + 60)
                fr = tk.frame(c, z=axis)
                p = fr @ V(math.cos(a) * r, math.sin(a) * r, 0)
                k.block("RingNode", (0.022, 0.022, 0.022), tk.frame(p, z=(fr.to_3x3() @ V(0, 0, 1))), sk.glow("ora_hot", 1.1), name, on, bevel=0.0)
        for i in range(8):
            a = i * 45
            out = polar(a, 1, 0)
            tilt = (UP * math.cos(math.radians(14)) + out * math.sin(math.radians(14))).normalized()
            m = tk.frame(polar(a, 0.15, 0.5), z=out.cross(tilt).normalized(), y=tilt)
            k.plate("Halo", [(-0.011, 0), (0.011, 0), (0, 0.095 if i % 2 == 0 else 0.06)], 0.006, m, sk.glow("ora", 0.9), "Halo", on, bevel=0.0)
        self.bone_list += [tk.Bone("Float", (0, 0, 0.04), UP, "Root", length=0.1),
                           tk.Bone("Core", c, UP, "Float", length=0.08),
                           tk.Bone("Inner", c, UP, "Float", length=0.05),
                           tk.Bone("Gaze", c + V(0, -0.1, 0), FWD, "Float", length=0.04),
                           tk.Bone("Halo", (0, 0, 0.5), UP, "Float", length=0.06)]
        for name, r, axis in self.RINGS:
            self.bone_list.append(tk.Bone(name, c, axis, "Float", length=0.06))
        self.sockets["Centre"] = c

    def base_pose(self, t, rate=1.0, bob=0.01):
        spec = {"Float": {"loc": V(0, 0, bob * math.sin(2 * math.pi * t))},
                "Core": {"rot": (UP, 2 * math.pi * t * rate)},
                "Inner": {"rot": (UP, -4 * math.pi * t * rate)},
                "Halo": {"rot": (UP, -2 * math.pi * t)}}
        for i, (name, r, axis) in enumerate(self.RINGS):
            spec[name] = {"rot": (axis.normalized(), (1 if i % 2 == 0 else -1) * 2 * math.pi * t * (i + 1) * rate)}
        return spec

    def clips(self, arm):
        def idle(t, f):
            spec = self.base_pose(t)
            spec["Gaze"] = {"scale": (1.0, 1.0, 1.0 + 0.25 * math.sin(4 * math.pi * t) ** 2)}
            return spec

        syll = [0.0, 0.9, 0.3, 1.0, 0.5, 0.0, 0.8, 0.4, 1.0, 0.2, 0.7, 0.0]

        def talk(t, f):
            spec = self.base_pose(t, rate=2.0, bob=0.006)
            x = t * len(syll)
            i = int(x) % len(syll)
            u = x - int(x)
            v = syll[i] + (syll[(i + 1) % len(syll)] - syll[i]) * (u * u * (3 - 2 * u))
            spec["Gaze"] = {"scale": (1.0 + 0.2 * v, 1.0, 1.0 + 2.6 * v)}
            spec["Inner"]["scale"] = 1.0 + 0.18 * v
            return spec

        rng = np.random.default_rng(13)
        jit = rng.uniform(-1, 1, (25, 6))

        def glitch(t, f):
            spec = self.base_pose(t * 0.3)
            j = jit[f % 25]
            on = (f // 3) % 2 == 0
            amp = 1.0 if on else 0.25
            spec["Float"] = {"loc": V(0.03 * j[0] * amp, 0.0, 0.02 * j[1] * amp)}
            spec["Gaze"] = {"scale": (1.0 + 0.6 * j[2] * amp, 1.0, max(0.05, 1.0 + 2.0 * j[3] * amp))}
            spec["Halo"] = {"rot": (UP, 0.4 * j[4] * amp)}
            spec["Core"] = {"rot": (UP, 0.5 * j[5] * amp), "scale": 1.0 + 0.1 * j[0] * amp}
            return spec
        return [bk.clip(arm, "Idle", 120, idle, loop=True), bk.clip(arm, "Talk", 60, talk, loop=True), bk.clip(arm, "Glitch", 24, glitch)]

    def post(self, objs):
        for o in objs:
            if o.type == "MESH":
                sk.uv_bands(o, 0.0, self.TOP)


# -------------------------------------------------------------------------------------------------------------
# NTI emblem
# -------------------------------------------------------------------------------------------------------------

class Emblem(Spec):
    name = "NTI_Emblem"
    tex, low = 512, 256

    def pieces(self, k):
        sk.emblem(k, tk.frame((0, 0, 0), z=FWD, y=UP), "Root", ["Main"], scale=1.0, letters=True)
        self.sockets["Centre"] = V(0, 0, 0)


# -------------------------------------------------------------------------------------------------------------
# Pioneer-1 (intact and wreck share the part builder)
# -------------------------------------------------------------------------------------------------------------

LEG_ANGLES = (90, 210, 330)
WING_TILT = 58.0   # solar wings face forward and up, so they read in a portrait


def rot_about(pivot, axis, deg):
    return Matrix.Translation(Vector(pivot)) @ Matrix.Rotation(math.radians(deg), 4, Vector(axis).normalized()) @ Matrix.Translation(-Vector(pivot))


class Probe:
    """Pioneer-1's parts in probe space; `T[group]` places a group in the world, `wreck` swaps looks and breaks parts."""

    def __init__(self, k, on, T=None, wreck=False):
        self.k, self.on, self.wreck = k, on, wreck
        self.T = T or {}

    def M(self, group, m):
        return self.T.get(group, Matrix.Identity(4)) @ m

    def P(self, group, p):
        return self.T.get(group, Matrix.Identity(4)) @ Vector(p)

    def look(self, look, group=None):
        if not self.wreck:
            return look
        if group in ("wing_R", "dish"):
            return bk.burn(look) if look.fam != "glow" else bk.burn(look)
        if look.fam == "glow":
            return look.but(glow=look.glow * 0.15)
        if look is tk.WHITE or look.paint == tk.WHITE.paint:
            return bk.scorch(look, 0.28).but(wear=1.0)
        return bk.scorch(look, 0.2)

    def cyl(self, g, name, p0, p1, r0, r1=None, look=tk.STEEL, sides=8):
        self.k.cyl(name, self.P(g, p0), self.P(g, p1), r0, r1, look=self.look(look, g), bone="Root", on=self.on, sides=sides, bevel=0.0)

    def lathe(self, g, name, prof, m, look, sides=12, start=None):
        self.k.lathe(name, prof, self.M(g, m), self.look(look, g), "Root", self.on, sides=sides, bevel=0.004, start=start)

    def block(self, g, name, size, m, look, bevel=0.006):
        self.k.block(name, size, self.M(g, m), self.look(look, g), "Root", self.on, bevel=bevel)

    def plate(self, g, name, poly, t, m, look):
        self.k.plate(name, poly, t, self.M(g, m), self.look(look, g), "Root", self.on, bevel=0.003)

    def body(self):
        g = "body"
        self.lathe(g, "Foil", [(0, 0.55), (0.44, 0.55), (0.465, 0.62), (0.465, 0.8), (0, 0.8)], tk.frame(), sk.FOIL, sides=6, start=0.0)
        self.lathe(g, "Band", [(0, 0.8), (0.47, 0.8), (0.47, 0.86), (0, 0.86)], tk.frame(), tk.BLUE.but(panel=0), sides=6, start=0.0)
        self.lathe(g, "Upper", [(0, 0.86), (0.465, 0.86), (0.44, 1.04), (0.3, 1.1), (0, 1.1)], tk.frame(), tk.WHITE.but(panel=0.09), sides=6, start=0.0)
        self.block(g, "Rtg", (0.16, 0.14, 0.12), tk.frame((-0.13, 0.13, 1.15)), tk.GUNMETAL)
        for i in range(4):
            self.block(g, "RtgFin", (0.012, 0.2, 0.1), tk.frame((-0.19 + i * 0.04, 0.13, 1.15)), tk.DARK, bevel=0.0)
        self.lathe(g, "Status", [(0, 1.1), (0.025, 1.1), (0.022, 1.14), (0, 1.15)], tk.frame((0.12, 0.16, 0)), sk.glow("pioneer", 2.4)
                   if not self.wreck else sk.glow("pioneer", 2.4 / 0.15), sides=8)
        # NTI emblem on the front face and the yellow hull mark "P-1" on a dark label.
        r = 0.455 * math.cos(math.radians(30))
        front = self.M(g, tk.frame((0, -r - 0.004, 0.95), z=FWD, y=UP))
        sk.emblem(self.k, front, "Root", self.on, scale=0.27, letters=False, prefix="Mark", star_glow=1.6 if not self.wreck else 0.3,
                  rim=self.look(tk.GUNMETAL), field=self.look(tk.BLUE), mark=self.look(tk.WHITE))
        n = polar(330, 1, 0)
        lf = self.M(g, tk.frame(polar(330, 0.465 * 0.866 + 0.005, 0.69), z=n, y=UP))
        self.k.block("Label", (0.22, 0.1, 0.01), lf, self.look(tk.DARK.but(panel=0)), "Root", self.on, bevel=0.0)
        yel = self.look(tk.YELLOW.but(wear=0.6))
        glyphs = [((-0.075, 0), (0.014, 0.06)), ((-0.058, 0.023), (0.03, 0.013)), ((-0.058, 0.0), (0.03, 0.013)), ((-0.042, 0.012), (0.012, 0.035)),
                  ((-0.005, 0.0), (0.035, 0.012)), ((0.055, 0.0), (0.014, 0.06)), ((0.044, 0.022), (0.02, 0.012))]
        for (x, y), (w, h) in glyphs:
            self.k.block("Glyph", (w, h, 0.012), lf @ tk.frame((x, y, 0.004)), yel, "Root", self.on, bevel=0.0)

    def drill(self):
        g = "drill"
        self.cyl(g, "DrillHouse", (0, 0, 0.56), (0, 0, 0.42), 0.1, 0.09, tk.GUNMETAL, sides=10)
        self.k.torus("DrillGlow", self.P(g, (0, 0, 0.42)), self.T.get(g, Matrix.Identity(4)).to_3x3() @ UP, 0.09, 0.01,
                     self.look(sk.glow("pioneer", 1.6)), "Root", self.on, seg=12, sides=4)
        self.cyl(g, "DrillPipe", (0, 0, 0.42), (0, 0, 0.12), 0.03, 0.03, tk.STEEL.but(rings=0.05), sides=8)
        self.lathe(g, "DrillBit", [(0, 0.0), (0.05, 0.08), (0.055, 0.13), (0, 0.13)], tk.frame(), tk.STEEL, sides=8)

    def legs(self, snapped=()):
        for i, a in enumerate(LEG_ANGLES):
            g = f"leg{i + 1}"
            hip = polar(a, 0.36, 0.6)
            foot = polar(a, 0.88, 0.04)
            knee = hip.lerp(foot, 0.55)
            self.block(g, "LegMount", (0.1, 0.1, 0.1), tk.frame(hip, z=UP, y=polar(a, 1, 0)), tk.GUNMETAL)
            self.cyl(g, "LegUpper", hip, knee, 0.032, 0.03, tk.GUNMETAL)
            lower_g = g + "_low" if i in snapped else g
            self.cyl(lower_g, "LegLower", knee, foot + V(0, 0, 0.03), 0.024, 0.02, tk.STEEL)
            if not self.wreck:   # the wreck saves these triangles for the growth
                self.k.torus("LegFoil", self.P(lower_g, knee.lerp(foot, 0.3)), self.T.get(lower_g, Matrix.Identity(4)).to_3x3() @ (foot - knee), 0.03, 0.012,
                             self.look(sk.FOIL), "Root", self.on, seg=10, sides=4)
            self.cyl(g, "LegBrace", polar(a, 0.44, 0.82), hip.lerp(knee, 0.7), 0.015, 0.015, tk.STEEL, sides=6)
            self.lathe(lower_g, "Pad", [(0, 0), (0.1, 0), (0.11, 0.02), (0.05, 0.04), (0, 0.04)], tk.frame(foot - V(0, 0, 0.03)), tk.DARK, sides=12)

    def wings(self, cracked=None):
        for s, g in ((-1, "wing_L"), (1, "wing_R")):
            self.cyl("body", "WingBoom", (s * 0.42, 0, 0.9), (s * 0.64, 0, 0.9), 0.03, 0.03, tk.GUNMETAL)
            self.cyl(g, "WingHinge", (s * 0.62, -0.06, 0.9), (s * 0.62, 0.06, 0.9), 0.035, 0.035, tk.GUNMETAL, sides=8)
            for i in range(2):
                cx = s * (0.92 + i * 0.52)
                m = tk.frame((cx, 0, 0.9), rot=(WING_TILT, 0, 0))
                self.plate(g, "WingFrame", bk.rect_poly(0.5, 0.56, 0.02), 0.022, m @ tk.frame((0, 0, -0.008)), tk.YELLOW.but(wear=0.7))
                if cracked == (s, i):
                    bk.shards(self.k, "Cells", bk.rect_poly(0.46, 0.52), 0.02, self.M(g, m @ tk.frame((0, 0, 0.004))), self.look(sk.SOLAR, g),
                              "Root", self.on, count=4, gap=0.02, lift=0.015, tilt=12, seed=5, drop=(2,))
                else:
                    self.plate(g, "Cells", bk.rect_poly(0.46, 0.52), 0.02, m @ tk.frame((0, 0, 0.004)), sk.SOLAR)

    def dish(self, broken=False):
        g = "dish"
        self.cyl("body", "DishMast", (0.1, 0.05, 1.08), (0.1, 0.05, 1.36), 0.025, 0.022, tk.STEEL)
        d = V(0.3, -0.55, 1.0).normalized()
        base = V(0.1, 0.05, 1.38)
        prof = [(0, 0), (0.1, 0.012), (0.38, 0.1), (0.4, 0.13), (0.37, 0.13), (0.1, 0.04), (0, 0.03)]
        if broken:
            self.k._add("Dish", lambda: sk.bm_lathe_arc(prof, 16, math.radians(70), math.radians(360)), 0.0, self.M(g, tk.frame(base, z=d)),
                        self.look(tk.WHITE.but(panel=0), g), "Root", self.on)
        else:
            self.lathe(g, "Dish", prof, tk.frame(base, z=d), tk.WHITE.but(panel=0), sides=20)
        fm = tk.frame(base, z=d)
        feed = fm @ V(0, 0, 0.3)
        for i in range(3):
            a = math.radians(90 + 120 * i)
            self.cyl(g, "Strut", fm @ V(math.cos(a) * 0.36, math.sin(a) * 0.36, 0.11), feed, 0.007, 0.006, tk.STEEL, sides=4)
        self.lathe(g, "Feed", [(0, -0.03), (0.03, -0.03), (0.035, 0.02), (0, 0.03)], tk.frame(feed, z=d), sk.glow("pioneer", 1.4), sides=8)

    def camera(self):
        g = "mast"
        self.cyl(g, "CamMast", (-0.26, -0.16, 1.03), (-0.28, -0.18, 1.52), 0.022, 0.018, tk.STEEL)
        self.block(g, "CamHead", (0.15, 0.12, 0.09), tk.frame((-0.28, -0.19, 1.57)), tk.GUNMETAL)
        self.block(g, "CamBand", (0.152, 0.122, 0.02), tk.frame((-0.28, -0.19, 1.6)), tk.YELLOW)
        self.lathe(g, "CamLens", [(0, 0), (0.035, 0), (0.035, 0.03), (0, 0.035)], tk.frame((-0.28, -0.25, 1.57), z=FWD),
                   sk.glow("pioneer", 2.2, lens=0.03), sides=10)

    def build(self, broken=False):
        self.body()
        self.drill()
        self.legs(snapped=(1,) if broken else ())
        self.wings(cracked=(1, 1) if broken else None)
        self.dish(broken)
        self.camera()


class Pioneer(Spec):
    name = "Pioneer1"

    def pieces(self, k):
        Probe(k, ["Main"]).build()
        self.sockets.update({"StatusLight": V(0.12, 0.16, 1.15), "Lens": V(-0.28, -0.27, 1.57), "Centre": V(0, 0, 0.9)})


def lay_flat(a, b, target, yaw):
    """A matrix that lays the segment a..b flat on the ground, its midpoint at `target`, turned by `yaw` degrees."""
    a, b = Vector(a), Vector(b)
    q = (b - a).normalized().rotation_difference(V(1, 0, 0))
    return (Matrix.Translation(target) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ q.to_matrix().to_4x4()
            @ Matrix.Translation(-(a + b) / 2))


def blot(r, seed, n=11):
    rng = np.random.default_rng(seed)
    return [(math.cos(2 * math.pi * i / n) * r * rng.uniform(0.7, 1.0), math.sin(2 * math.pi * i / n) * r * rng.uniform(0.7, 1.0))
            for i in range(n)]


class Wreck(Spec):
    name = "Pioneer1_Wreck"

    def pieces(self, k):
        on = ["Main"]
        tilt_axis = V(1, 0.45, 0)
        body = Matrix.Translation(V(0.05, 0.05, -0.2)) @ rot_about((0, 0, 0.3), tilt_axis, 27)
        T = {"body": body, "drill": body, "leg1": body, "leg3": body @ rot_about(polar(330, 0.36, 0.6), polar(330, 1, 0).cross(UP), -18),
             "leg2": body,
             "leg2_low": lay_flat(polar(210, 0.36, 0.6).lerp(polar(210, 0.88, 0.04), 0.55), polar(210, 0.88, 0.04), V(-1.0, -0.75, 0.1), 115),
             "wing_L": body @ rot_about((-0.62, 0, 0.9), (0, 1, 0), -52),
             "wing_R": Matrix.Translation(V(1.95, 0.7, 0.2)) @ Matrix.Rotation(math.radians(28), 4, "Z") @ Matrix.Rotation(math.radians(7), 4, "Y")
             @ Matrix.Rotation(math.radians(-WING_TILT), 4, "X") @ Matrix.Translation(-V(1.15, 0, 0.9)),
             "dish": Matrix.Translation(V(-1.05, 0.45, -0.78)) @ rot_about((0.1, 0.05, 1.38), (1, 0.2, 0), -78),
             "mast": body @ rot_about((-0.27, -0.17, 1.05), (1, -0.3, 0), 38)}
        probe = Probe(k, on, T, wreck=True)
        probe.build(broken=True)
        # The mire mound and wet puddles.
        sk.add(k, "Mound", lambda: sk.bm_mound(2.4, 0.26, seed=8, rings=5, sectors=18), tk.frame((0.2, 0.1, 0)), sk.MUD, "Root", on)
        for i, (x, y, r) in enumerate(((-1.6, -0.6, 0.45), (1.0, -1.5, 0.35), (1.6, 1.4, 0.4))):
            k.plate("Puddle", blot(r, 20 + i), 0.01, tk.frame((x, y, 0.08 - 0.02 * i)), sk.MIRE, "Root", on, bevel=0.0)
        # Crysium growing out of the hull crack and the mire.
        top = body @ V(0.18, 0.05, 1.06)
        sk.cluster(k, "Crystal", top, body.to_3x3() @ V(0.2, 0.1, 1), 3, 0.6, seed=3)
        for i, (x, y, s, n) in enumerate(((-1.35, 0.95, 0.8, 3), (1.25, -0.95, 0.55, 2), (0.45, 1.45, 0.95, 4), (2.15, 0.85, 0.6, 2),
                                          (-0.6, -1.55, 0.45, 1))):
            sk.cluster(k, "Crystal", V(x, y, 0.12), V(0.15 * (x / 2), 0.1 * (y / 2), 1), n, s, seed=10 + i)
        # Alien flora: plum fronds with glowing tips, tendrils over the legs, glow bulbs.
        rng = np.random.default_rng(4)
        for i in range(4):
            a = rng.uniform(0, 360)
            r = rng.uniform(1.0, 2.0)
            base = polar(a, r, 0.14)
            d = (polar(a, 0.6, 0) + V(0, 0, 1)).normalized()
            tip = sk.glow("lime", 1.0) if i % 2 else sk.glow("pink", 1.0)
            for j in range(3):
                dd = (Matrix.Rotation(math.radians(j * 120 + rng.uniform(-20, 20)), 3, UP) @ d).normalized()
                sk.frond(k, "Frond", base, dd + V(0, 0, 0.4), rng.uniform(0.35, 0.55), 0.09, math.radians(55), sk.PLUM, tip, segs=2,
                         side=UP.cross(dd))
        legp = [body @ v for v in (polar(90, 0.85, 0.06), polar(90, 0.7, 0.25), polar(90, 0.55, 0.42), polar(70, 0.45, 0.6), polar(40, 0.48, 0.82))]
        sk.tendril(k, "Tendril", legp, 0.03, 0.012, sk.PLUM)
        sk.tendril(k, "Tendril", [V(-0.4, 0.9, 0.1), body @ V(-0.3, 0.42, 0.55), body @ V(-0.45, 0.25, 0.85), body @ V(-0.4, 0.0, 1.06),
                                  body @ V(-0.2, -0.1, 1.12)], 0.028, 0.01, sk.PLUM)
        sk.tendril(k, "Tendril", [V(1.2, 0.4, 0.1), V(1.5, 0.6, 0.2), V(1.8, 0.75, 0.16), V(2.1, 0.7, 0.12)], 0.025, 0.01, sk.PLUM)
        for i, (x, y) in enumerate(((-0.9, 1.3), (0.9, 1.0), (-1.7, 0.2), (1.6, -0.5))):
            sk.bulb(k, "Bulb", V(x, y, 0.15), 0.042, sk.glow("lime" if i % 2 else "crysium", 0.9))
        self.sockets.update({"StatusLight": body @ V(0.12, 0.16, 1.15), "Smoke": body @ V(-0.1, 0.1, 1.2), "Log": body @ V(0, -0.5, 0.95),
                             "Centre": V(0, 0, 0.6)})


# -------------------------------------------------------------------------------------------------------------
# Radar console (command post)
# -------------------------------------------------------------------------------------------------------------

class Console(Spec):
    name = "RadarConsole"
    variants = ["Main", "Radar", "Screen_L", "Screen_R"]
    extras = [("Radar", "_Radar"), ("Screen_L", "_Screen_L"), ("Screen_R", "_Screen_R")]
    SLOPE = -20.0

    def pieces(self, k):
        on = ["Main"]
        k.block("Pedestal", (1.3, 0.5, 0.66), tk.frame((0, 0.05, 0.36)), tk.BLUE.but(panel=0.14), "Root", on, bevel=0.02)
        k.block("Kick", (1.24, 0.06, 0.08), tk.frame((0, -0.21, 0.04)), tk.HAZARD, "Root", on, bevel=0.004)
        k.block("Foot", (1.36, 0.58, 0.04), tk.frame((0, 0.05, 0.02)), tk.DARK, "Root", on, bevel=0.006)
        top = tk.frame((0, -0.02, 0.73), rot=(self.SLOPE, 0, 0))
        self.top = top
        k.block("Desk", (1.44, 0.66, 0.06), top, tk.WHITE.but(panel=0.18), "Root", on, bevel=0.012)
        k.block("DeskEdge", (1.46, 0.04, 0.05), top @ tk.frame((0, -0.34, 0)), tk.GUNMETAL, "Root", on, bevel=0.006)
        rc = top @ V(0, 0.04, 0.03)
        n = top.to_3x3() @ UP
        k.lathe("Bezel", [(0.2, 0), (0.27, 0), (0.28, 0.025), (0.235, 0.03), (0.2, 0.012)], tk.frame(rc, z=n), tk.GUNMETAL, "Root", on, sides=32,
                bevel=0.0)
        k.torus("BezelGlow", rc + n * 0.031, n, 0.255, 0.005, sk.glow("cyan", 1.8), "Root", on, seg=32, sides=4)
        k.lathe("Radar", [(0, 0.008), (0.205, 0.008), (0.205, 0.018), (0, 0.018)], tk.frame(rc, z=n), sk.glow((0.12, 0.6, 0.45), 0.7), "Root",
                ["Radar"], sides=40, bevel=0.0)
        self.radar = (rc + n * 0.018, top.to_3x3() @ XAX, top.to_3x3() @ V(0, 1, 0))
        for i in range(12):
            x = -0.62 + (i % 6) * 0.045
            y = -0.2 + (i // 6) * 0.05
            look = sk.glow("cyan", 1.4) if i in (2, 7) else (tk.YELLOW if i == 10 else tk.DARK.but(panel=0))
            k.block("Key", (0.036, 0.04, 0.016), top @ tk.frame((x, y, 0.035)), look, "Root", on, bevel=0.003)
            k.block("Key", (0.036, 0.04, 0.016), top @ tk.frame((-x, y, 0.035)), look if i != 10 else tk.DARK.but(panel=0), "Root", on,
                    bevel=0.003)
        for s in (-1, 1):
            k.block("Slider", (0.025, 0.16, 0.012), top @ tk.frame((s * 0.36, -0.12, 0.034)), tk.GUNMETAL, "Root", on, bevel=0.002)
            k.block("SliderKnob", (0.04, 0.03, 0.03), top @ tk.frame((s * 0.36, -0.15 + 0.06 * (s > 0), 0.045)), tk.YELLOW, "Root", on, bevel=0.004)
            k.cyl("Grip", top @ V(s * 0.66, -0.36, 0.0), top @ V(s * 0.66, -0.36, 0.12), 0.022, look=tk.RUBBER, bone="Root", on=on, sides=8)
        # Riser at the back with two side screens.
        k.block("Riser", (1.4, 0.1, 0.4), tk.frame((0, 0.3, 1.0), rot=(8, 0, 0)), tk.GUNMETAL.but(panel=0.16), "Root", on, bevel=0.015)
        k.block("RiserTop", (1.44, 0.16, 0.05), tk.frame((0, 0.29, 1.21)), tk.WHITE.but(panel=0), "Root", on, bevel=0.01)
        k.block("StatusStrip", (1.0, 0.012, 0.018), tk.frame((0, 0.245, 0.84), rot=(8, 0, 0)), sk.glow("cyan", 1.6), "Root", on, bevel=0.0)
        self.screens = {}
        for s, v in ((-1, "Screen_L"), (1, "Screen_R")):
            sm = tk.frame((s * 0.42, 0.244, 1.02), rot=(82, 0, 0))
            k.block("ScreenFrame", (0.42, 0.28, 0.02), sm @ tk.frame((0, 0, -0.012)), tk.DARK, "Root", on, bevel=0.006)
            k.block(v, (0.37, 0.23, 0.006), sm, sk.glow((0.2, 0.55, 0.8), 0.7), "Root", [v], bevel=0.0)
            self.screens[v] = (sm @ V(0, 0, 0.003), sm.to_3x3() @ XAX, sm.to_3x3() @ V(0, 1, 0))
        k.cyl("Mic", top @ V(0.25, 0.25, 0.03), top @ V(0.22, 0.12, 0.2), 0.008, look=tk.DARK, bone="Root", on=on, sides=6)
        k.lathe("MicTip", [(0, 0), (0.015, 0.0), (0.015, 0.03), (0, 0.035)], tk.frame(top @ V(0.22, 0.12, 0.2), z=V(0, -0.6, -0.4)),
                sk.glow("cyan", 1.6), "Root", on, sides=8, bevel=0.0)
        sk.emblem(k, tk.frame((0, -0.205, 0.42), z=FWD, y=UP), "Root", on, scale=0.4, letters=True, prefix="Mark")
        self.sockets.update({"Radar": self.radar[0], "Operator": V(0, -0.7, 0)})

    def post(self, objs):
        for o in objs:
            base = o.name
            if base.endswith("_Radar"):
                c, u, v = self.radar
                sk.planar_uv(o, c, u, v, 0.205, 0.205)
            for name, (c, u, v) in self.screens.items():
                if base.endswith("_" + name):
                    sk.planar_uv(o, c, u, v, 0.185, 0.115)


SPECS = {"Ora": Ora, "Emblem": Emblem, "Pioneer": Pioneer, "Wreck": Wreck, "Console": Console}


# -------------------------------------------------------------------------------------------------------------
# Build and export
# -------------------------------------------------------------------------------------------------------------

def objects(spec, kit, mat, label=""):
    if spec.static:
        objs = [sk.static_object(kit, "Main", mat, kit.name + label)]
        for variant, suffix in spec.extras:
            objs.append(sk.static_object(kit, variant, mat, kit.name + suffix + label))
        return None, objs
    built = bk.skinned(kit, "Main", spec.bone_list, mat, kit.name + label, lod=False, arm_name=f"{kit.name}_Rig{label}")
    return built[0], built[1:]


def build(spec, bake=True):
    bk.clear_actions()
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, spec.tex, spec.low, bake, samples=16)
    arm, meshes = objects(spec, kit, mat)
    spec.post(meshes)
    acts = spec.clips(arm) if arm else []
    if arm:
        bk.rest_pose(arm)
    socks = sk.socket_empties(spec.sockets, parent=arm)
    allobj = ([arm] if arm else []) + meshes + socks
    bk.export_fbx(os.path.join(out_dir, kit.name + ".fbx"), allobj, animated=bool(acts))
    report.update({"fbx": kit.name + ".fbx", "meshes": {o.name: C.tri_count(o) for o in meshes},
                   "tris": sum(C.tri_count(o) for o in meshes), "lod1": "none (portrait/cutscene or static decor)",
                   "uv_sets": [l.name for l in meshes[0].data.uv_layers],
                   "clips": {a.name: int(a.frame_range[1]) for a in acts},
                   "sockets": {n: [round(c, 3) for c in s] for n, s in ((n, (s[0] if isinstance(s, tuple) else s)) for n, s in spec.sockets.items())},
                   "pivot": "on the ground / badge centre; faces -Y in Blender (Unity +Z)"})
    if arm:
        report["bones"] = [b.name for b in arm.data.bones]
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    C.log(kit.name, report["tris"], report["meshes"])
    bk.remove(allobj)
    bk.clear_actions()
    return kit, mat, report


# -------------------------------------------------------------------------------------------------------------
# Previews
# -------------------------------------------------------------------------------------------------------------

def preview_objects(n, kit, mat):
    spec = SPECS[n]()
    spec.kit()
    arm, meshes = objects(spec, kit, mat, "P")
    spec.post(meshes)
    acts = {a.name: a for a in spec.clips(arm)} if arm else {}
    if arm:
        bk.rest_pose(arm)
    return spec, arm, meshes, acts


def render_ora(kit, mat, out):
    spec, arm, meshes, acts = preview_objects("Ora", kit, mat)
    C.preview_world((0.01, 0.014, 0.022))
    rig = sk.portrait_lights(0.5)
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    # Portrait: the bust framing, centred on the core.
    cam = sk.portrait_camera(V(0, 0, 0.33), distance=1.75)
    C.render(cam, os.path.join(out, "ora_portrait.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 0.32), 1.6, azimuth=-35, elevation=18, lens=50)
    C.render(cam, os.path.join(out, "ora_close.png"), 960, 960)
    sk.strip(arm, acts["Talk"], cam, (0, 6, 12, 18, 24, 30), os.path.join(out, "ora_talk_strip.png"), width=360, height=360)
    sk.strip(arm, acts["Glitch"], cam, (0, 3, 6, 9, 12, 15), os.path.join(out, "ora_glitch_strip.png"), width=360, height=360)
    bk.remove([cam] + rig + meshes + [arm])
    C.preview_world()


def render_emblem(kit, mat, out):
    spec, arm, meshes, acts = preview_objects("Emblem", kit, mat)
    rig = sk.portrait_lights(0.5)
    cam = sk.portrait_camera(V(0, 0, 0), distance=1.45)
    C.render(cam, os.path.join(out, "nti_portrait.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 0), 1.2, azimuth=0, elevation=0, lens=50)
    C.render(cam, os.path.join(out, "nti_front.png"), 960, 960)
    bk.remove([cam])
    cam = C.macro_camera(V(0.05, 0, 0.06), 0.45, -40, 15, lens=60)
    C.render(cam, os.path.join(out, "nti_macro.png"), 1280, 720)
    bk.remove([cam] + rig + meshes)


def render_pioneer(kit, mat, out):
    spec, arm, meshes, acts = preview_objects("Pioneer", kit, mat)
    rig = tk.close_lights(1.4)
    g = C.ground(color=(0.04, 0.04, 0.06))
    cam = C.closeup_camera(V(0, 0, 0.85), 4.8, azimuth=-30, elevation=18, lens=50)
    C.render(cam, os.path.join(out, "pioneer_close.png"), 1280, 960)
    bk.remove([cam, g])
    cam = sk.portrait_camera(V(0, 0, 1.0), distance=5.0, elevation=12)
    C.render(cam, os.path.join(out, "pioneer_portrait.png"), 1024, 1024)
    bk.remove([cam])
    cam = C.macro_camera(V(0, -0.35, 0.9), 1.2, -25, 12, lens=60)
    C.render(cam, os.path.join(out, "pioneer_macro.png"), 1280, 720)
    bk.remove([cam] + rig + meshes)


def render_wreck(kit, mat, out):
    spec, arm, meshes, acts = preview_objects("Wreck", kit, mat)
    g = C.ground(color=(0.035, 0.04, 0.045))
    rig = tk.close_lights(2.0)
    cam = C.closeup_camera(V(0.2, 0, 0.5), 7.5, azimuth=-30, elevation=24, lens=45)
    C.render(cam, os.path.join(out, "wreck_close.png"), 1280, 900)
    bk.remove([cam])
    cam = C.closeup_camera(V(0.2, 0, 0.5), 7.5, azimuth=140, elevation=28, lens=45)
    C.render(cam, os.path.join(out, "wreck_back.png"), 1280, 900)
    bk.remove([cam])
    cam = C.macro_camera(V(0.0, -0.3, 0.7), 1.6, -20, 15, lens=55)
    C.render(cam, os.path.join(out, "wreck_macro.png"), 1280, 720)
    bk.remove([cam] + rig)
    bk.render_game(V(0.2, 0, 0.3), os.path.join(out, "wreck_game.png"), zoom=1.0)
    bk.render_game(V(0.2, 0, 0.3), os.path.join(out, "wreck_game_zoom.png"), zoom=2.5)
    bk.remove(meshes + [g])


def render_console(kit, mat, out):
    spec, arm, meshes, acts = preview_objects("Console", kit, mat)
    g = C.ground(color=(0.04, 0.045, 0.06))
    rig = tk.close_lights(0.9)
    cam = C.closeup_camera(V(0, 0, 0.75), 2.6, azimuth=-30, elevation=24, lens=50)
    C.render(cam, os.path.join(out, "console_close.png"), 1280, 960)
    bk.remove([cam])
    cam = C.macro_camera(V(0, -0.05, 0.78), 1.0, -10, 40, lens=50)
    C.render(cam, os.path.join(out, "console_macro.png"), 1280, 720)
    bk.remove([cam] + rig + meshes + [g])


RENDER = {"Ora": render_ora, "Emblem": render_emblem, "Pioneer": render_pioneer, "Wreck": render_wreck, "Console": render_console}


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else list(SPECS)
    C.preview_world()
    tk.metal_detail_map()
    for n in names:
        kit, mat, _ = build(SPECS[n](), bake=not args.get("nobake"))
        if not args.get("norender"):
            RENDER[n](kit, mat, out)
            bk.clear_actions()
    C.save_blend("StoryProps" if not isinstance(only, str) else "StoryProps_" + "_".join(names))


if __name__ == "__main__":
    main()
