"""Round 9: the colony base at the end of the road, and the base turret (see ArtSource/DESIGN_NOTES.md, round 9).

The base is a set of modules (deck, command post, dish, Crysium core, core crystal, landing pad, habitat,
hardpoints), each its own static object with its pivot on its foot, so the cutscene timelapse can raise them one by
one and the upgrade screen can reuse them. A damage overlay and a wreck are separate objects. Sockets are empties.
The base turret is the round-7 Gun Turret head on a square bastion pad (`Tower_BaseTurret`, tiers T1, T2, T3).

Out: `Exports/Base/Base.fbx` (modules + LOD1 + sockets), `Base_Damage.fbx`, `Base_Wreck.fbx`, one 1024 map set
(+ Low 512), `Base_report.json`; `Exports/Tower_BaseTurret/Tower_BaseTurret_T1..T3.fbx` with its own map set.

Run: blender --background --factory-startup --python Tools/Blender/build_base.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Base|Turret]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_towers as bt  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP  # noqa: E402

NAME = "Base"
OUT = os.path.join(C.EXPORTS, NAME)
DECK_Z = 0.23
# Order the timelapse raises them in (FieldBase: command post, landing pad, core).
MODULES = ["Deck", "Command", "Dish", "Pad", "Core", "Crystal", "Habitat", "Hardpoints"]
PIVOTS = {"Deck": V(0, 0, 0), "Command": V(0, 0.38, DECK_Z), "Dish": V(-0.3, 0.62, 2.02), "Pad": V(0.74, -0.5, DECK_Z),
          "Core": V(0.82, 0.42, DECK_Z), "Crystal": V(0.82, 0.42, DECK_Z + 0.36), "Habitat": V(-0.84, -0.3, DECK_Z),
          "Hardpoints": V(0, 0, DECK_Z)}
SOCKETS = {"HeroPost": V(0, -1.0, 0.0), "TurretLink_L": V(-1.36, 0.05, 0.34), "TurretLink_R": V(1.36, 0.05, 0.34),
           "Smoke_Command": V(0.1, 0.45, 1.1), "Smoke_Core": V(0.82, 0.42, 1.5), "Smoke_Deck": V(-0.4, -0.6, 0.3),
           "Spark_1": V(0.0, 0.04, 0.55), "Spark_2": V(0.62, 0.2, 0.4), "Spark_3": V(-0.84, -0.5, 0.5),
           "Spark_4": V(0.4, -0.9, 0.24), "Light_Core": V(0.82, 0.42, 1.0), "Light_Command": V(0, 0.2, 1.2)}


# Small lit details the LOD1 leaves out (the glow stays in the maps of the parts around them).
LOD_DROP = {"Porthole", "BollardLight", "RampLight", "AntennaTip", "Antenna", "LinkGlow", "LinkSocket", "FloodLamp", "MastLight",
            "Cable", "CrateBand", "Vent", "Mullion", "JunctionStripe", "DoorStripe", "FeedArm", "Shard"}


class Base:
    def __init__(self):
        self.module = {}
        self.cur = None

    def tag(self, k, name):
        """Pieces added from here on belong to module `name` (until the next tag)."""
        self.flush(k)
        self.cur = name
        self._start = len(k.pieces)

    def flush(self, k):
        if self.cur is not None:
            for p in k.pieces[self._start:]:
                self.module.setdefault(self.cur, []).append(p)
        self.cur = None

    def kit(self):
        k = tk.Kit(NAME, ["Base", "Damage", "Wreck"])
        self.tag(k, "Deck"); self.deck(k)
        self.tag(k, "Command"); self.command(k)
        self.tag(k, "Dish"); self.dish(k)
        self.tag(k, "Pad"); self.pad(k)
        self.tag(k, "Core"); self.core(k)
        self.tag(k, "Crystal"); self.crystal(k)
        self.tag(k, "Habitat"); self.habitat(k)
        self.tag(k, "Hardpoints"); self.hardpoints(k)
        self.tag(k, "Damage"); self.damage(k)
        self.tag(k, "Wreck"); self.wreck(k)
        self.flush(k)
        # Each module is its own bake variant, so the bake moves them apart: the deck carries no baked shadow of
        # a module that the timelapse has not raised yet (Unity's real-time shadows give the contact shadow).
        k.variants = MODULES + ["Damage", "Wreck"]
        for m, pieces in self.module.items():
            for p in pieces:
                p.variants = {m}
                if p.name in LOD_DROP:
                    p.lod = False
        return k

    # ---------------------------------------------------------------------------------------------- modules
    def deck(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        k.plate("Skirt", bk.octagon(2.8, 2.3, 0.38), 0.1, tk.frame((0, 0, 0.05)), L(tk.GUNMETAL), "Root", on, bevel=0.012)
        k.plate("SkirtHazard", bk.octagon(2.822, 2.322, 0.386), 0.03, tk.frame((0, 0, 0.06)), L(tk.HAZARD), "Root", on, bevel=0.003)
        k.plate("Body", bk.octagon(2.7, 2.2, 0.35), 0.1, tk.frame((0, 0, 0.15)), L(tk.BLUE.but(panel=0.22)), "Root", on, bevel=0.01)
        if look is None:
            k.plate("Deck", bk.octagon(2.62, 2.12, 0.33), 0.03, tk.frame((0, 0, 0.215)), tk.WHITE.but(panel=0.32), "Root", on, bevel=0.006)
            for (x, y, w, d) in ((0, -1.03, 1.5, 0.03), (0, 1.03, 1.5, 0.03), (-1.28, 0, 0.03, 1.2), (1.28, 0, 0.03, 1.2)):
                k.block("EdgeLight", (w, d, 0.012), tk.frame((x, y, DECK_Z + 0.002)), bk.glow("base", 1.5), "Root", on, bevel=0.0)
            for x in (-0.55, 0.55):
                for y in (-0.2, 0.62):
                    k.block("Seam", (0.02, 0.5, 0.006), tk.frame((x, y, DECK_Z + 0.001)), tk.DARK, "Root", on, bevel=0.0, lod=False)
        # Ramp to the road with yellow rails and glowing guide lights.
        k.block("Ramp", (0.74, 0.56, 0.05), tk.frame((0, -1.33, 0.115), rot=(19, 0, 0)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on, bevel=0.006)
        for s in (-1, 1):
            k.block("RampRail", (0.05, 0.58, 0.06), tk.frame((s * 0.39, -1.33, 0.14), rot=(19, 0, 0)), L(tk.HAZARD), "Root", on, bevel=0.004)
            if look is None:
                for j in range(3):
                    y = -1.12 - j * 0.16
                    k.block("RampLight", (0.03, 0.04, 0.012), tk.frame((s * 0.32, y, 0.2 - (j + 0.5) * 0.058), rot=(19, 0, 0)),
                            bk.glow("base", 1.6), "Root", on, bevel=0.0)
        # Corner bollards and vents in the body.
        for x, y in ((-1.18, -0.98), (1.18, -0.98), (-1.18, 0.98), (1.18, 0.98)):
            k.cyl("Bollard", (x, y, DECK_Z - 0.01), (x, y, DECK_Z + 0.2), 0.055, 0.05, look=L(tk.GUNMETAL), bone="Root", on=on, sides=10)
            k.cyl("BollardCap", (x, y, DECK_Z + 0.2), (x, y, DECK_Z + 0.24), 0.058, look=L(tk.YELLOW), bone="Root", on=on, sides=10)
            if look is None:
                k.cyl("BollardLight", (x, y, DECK_Z + 0.24), (x, y, DECK_Z + 0.26), 0.03, look=bk.glow("base", 1.7), bone="Root", on=on,
                      sides=8, bevel=0.0)
        for x in (-0.8, -0.3, 0.3, 0.8):
            k.block("Vent", (0.26, 0.012, 0.05), tk.frame((x, -1.11, 0.15)), L(tk.DARK), "Root", on, bevel=0.002)
        if look is None:
            for i, (x, y, r) in enumerate(((-0.55, -0.78, 12), (-0.72, -0.62, -18), (0.32, -0.86, 5))):
                size = 0.17 if i != 1 else 0.14
                k.block("Crate", (size, size, size), tk.frame((x, y, DECK_Z + size / 2), rot=(0, 0, r)),
                        [tk.BLUE, tk.GUNMETAL, tk.WHITE][i].but(panel=0), "Root", on, bevel=0.006)
                k.block("CrateBand", (size + 0.004, 0.03, size + 0.004), tk.frame((x, y, DECK_Z + size / 2), rot=(0, 0, r)), tk.HAZARD, "Root",
                        on, bevel=0.002)

    def command(self, k):
        on = ["Base"]
        z = DECK_Z
        k.block("Storey1", (1.0, 0.7, 0.44), tk.frame((0, 0.4, z + 0.22)), tk.BLUE.but(panel=0.13), "Root", on, taper=(0.97, 0.95), bevel=0.012)
        k.block("Plinth", (1.06, 0.76, 0.05), tk.frame((0, 0.4, z + 0.025)), tk.GUNMETAL, "Root", on, bevel=0.006)
        # The operations room: a raked glass band across the front with a lit sill.
        k.block("Glass", (0.9, 0.03, 0.2), tk.frame((0, 0.06, z + 0.33), rot=(-26, 0, 0)), tk.GLASS, "Root", on, bevel=0.004)
        k.block("GlassFrame", (0.94, 0.05, 0.03), tk.frame((0, 0.05, z + 0.43)), tk.WHITE.but(panel=0), "Root", on, bevel=0.004)
        k.block("GlassGlow", (0.84, 0.012, 0.022), tk.frame((0, 0.03, z + 0.235)), bk.glow("base", 1.6), "Root", on, bevel=0.0)
        for x in (-0.3, 0.0, 0.3):
            k.block("Mullion", (0.025, 0.035, 0.21), tk.frame((x, 0.045, z + 0.33), rot=(-26, 0, 0)), tk.WHITE.but(panel=0), "Root", on, bevel=0.002)
        k.block("Door", (0.22, 0.02, 0.2), tk.frame((0.38, 0.05, z + 0.1)), tk.DARK, "Root", on, bevel=0.003)
        k.block("DoorLight", (0.24, 0.022, 0.02), tk.frame((0.38, 0.045, z + 0.215)), bk.glow("base", 1.5), "Root", on, bevel=0.0)
        k.block("DoorStripe", (0.26, 0.022, 0.03), tk.frame((0.38, 0.045, z + 0.015)), tk.HAZARD, "Root", on, bevel=0.002)
        k.block("Roof", (1.06, 0.76, 0.05), tk.frame((0, 0.4, z + 0.465)), tk.WHITE.but(panel=0.16), "Root", on, bevel=0.008)
        for x in (-0.35, 0.35):
            k.block("SideFin", (0.03, 0.5, 0.3), tk.frame((x * 1.47, 0.42, z + 0.25)), tk.WHITE.but(panel=0.1), "Root", on, taper=(1, 0.7), bevel=0.004)
        # Upper storey with a window strip, and the antennas.
        k.block("Storey2", (0.56, 0.44, 0.26), tk.frame((0.14, 0.46, z + 0.62)), tk.BLUE.but(panel=0.11), "Root", on, bevel=0.01)
        k.block("Roof2", (0.6, 0.48, 0.04), tk.frame((0.14, 0.46, z + 0.77)), tk.WHITE.but(panel=0.12), "Root", on, bevel=0.006)
        k.block("Window2", (0.44, 0.012, 0.05), tk.frame((0.14, 0.235, z + 0.66)), bk.glow("base", 1.5), "Root", on, bevel=0.0)
        for i, (x, y, h) in enumerate(((0.3, 0.36, 0.28), (0.36, 0.5, 0.2), (0.22, 0.6, 0.34))):
            k.cyl("Antenna", (x, y, z + 0.79), (x, y, z + 0.79 + h), 0.008, 0.005, look=tk.GUNMETAL, bone="Root", on=on, sides=6, bevel=0.0)
            k.cyl("AntennaTip", (x, y, z + 0.79 + h), (x, y, z + 0.8 + h), 0.012, look=bk.glow("base", 1.8), bone="Root", on=on, sides=6, bevel=0.0)
        # The comms mast: four lattice legs from the roof, cross plates and the collar the dish turns on.
        foot, top = V(-0.3, 0.62, z + 0.49), V(-0.3, 0.62, 1.98)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            off = V(math.cos(a), math.sin(a), 0)
            k.cyl("MastLeg", foot + off * 0.13, top + off * 0.04, 0.016, 0.012, look=tk.GUNMETAL, bone="Root", on=on, sides=6, bevel=0.0)
        for j, zz in enumerate((0.95, 1.35, 1.7)):
            w = 0.2 - j * 0.045
            k.block("MastPlate", (w, w, 0.02), tk.frame((foot.x, foot.y, zz)), tk.WHITE.but(panel=0) if j == 1 else tk.GUNMETAL, "Root", on, bevel=0.003)
        k.block("MastStripe", (0.15, 0.15, 0.03), tk.frame((foot.x, foot.y, 1.2)), tk.HAZARD, "Root", on, bevel=0.002)
        k.cyl("Collar", top - V(0, 0, 0.03), top + V(0, 0, 0.04), 0.045, 0.04, look=tk.DARK, bone="Root", on=on, sides=10)
        k.cyl("MastLight", foot + V(0.08, -0.08, 0.6), foot + V(0.08, -0.08, 0.63), 0.02, look=bk.glow("base", 1.8), bone="Root", on=on, sides=8, bevel=0.0)

    def dish(self, k):
        on = ["Base"]
        c = PIVOTS["Dish"]
        d = V(0, -0.55, 0.83).normalized()
        m = tk.frame(c + V(0, 0, 0.06), z=d)
        k.lathe("Dish", [(0, 0), (0.24, 0.07), (0.25, 0.085), (0.232, 0.09), (0, 0.025)], m, tk.WHITE.but(panel=0), "Root", on, sides=18, bevel=0.0)
        k.cyl("Hub", c, c + V(0, 0, 0.08), 0.05, look=tk.GUNMETAL, bone="Root", on=on, sides=10)
        tip = c + V(0, 0, 0.06) + d * 0.2
        k.cyl("FeedArm", c + V(0, 0, 0.08), tip, 0.009, look=tk.DARK, bone="Root", on=on, sides=6, bevel=0.0)
        k.lathe("Feed", [(0, 0), (0.02, 0.0), (0.016, 0.03), (0, 0.035)], tk.frame(tip, z=d), bk.glow("base", 1.9), "Root", on, sides=8, bevel=0.0)

    def pad(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        c = PIVOTS["Pad"]
        k.lathe("PadBase", [(0, 0), (0.44, 0), (0.44, 0.03), (0.42, 0.045), (0, 0.045)], tk.frame(c), L(tk.GUNMETAL), "Root", on, sides=6, start=0.0)
        k.lathe("PadTop", [(0, 0.04), (0.4, 0.04), (0.4, 0.058), (0, 0.058)], tk.frame(c), L(tk.WHITE.but(panel=0)), "Root", on, sides=6, start=0.0)
        if look is None:
            k.torus("PadRing", c + V(0, 0, 0.06), UP, 0.3, 0.012, bk.glow("base", 1.5), "Root", on, seg=24, sides=4)
            tri = [(0, 0.13), (-0.11, -0.07), (0.11, -0.07)]
            k.plate("LandMark", tri, 0.006, tk.frame(c + V(0, 0, 0.061)), tk.YELLOW, "Root", on, bevel=0.0)
            k.plate("LandMarkInner", [(0, 0.06), (-0.05, -0.035), (0.05, -0.035)], 0.007, tk.frame(c + V(0, 0, 0.062)), tk.DARK, "Root", on, bevel=0.0)
        for a in (math.radians(30), math.radians(-30)):
            p = c + V(math.cos(a), math.sin(a), 0) * 0.46
            k.cyl("FloodPost", p, p + V(0, 0, 0.34), 0.016, 0.012, look=L(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
            k.block("FloodHead", (0.08, 0.05, 0.05), tk.frame(p + V(0, 0, 0.36), rot=(0, 0, math.degrees(a) + 90)), L(tk.WHITE.but(panel=0)), "Root", on,
                    bevel=0.004)
            if look is None:
                k.block("FloodLamp", (0.06, 0.012, 0.03), tk.frame(p + V(-math.cos(a) * 0.026, -math.sin(a) * 0.026, 0.36), rot=(0, 0, math.degrees(a) + 90)),
                        bk.glow("base", 2.0), "Root", on, bevel=0.0)

    def core(self, k):
        on = ["Base"]
        c = PIVOTS["Core"]
        k.lathe("CorePlinth", [(0, 0), (0.36, 0), (0.36, 0.08), (0.32, 0.12), (0, 0.12)], tk.frame(c), tk.GUNMETAL, "Root", on, sides=8)
        k.torus("PlinthRing", c + V(0, 0, 0.125), UP, 0.27, 0.014, bk.glow("cyan", 1.5), "Root", on, seg=24, sides=4)
        k.lathe("Socket", [(0, 0.12), (0.16, 0.12), (0.14, 0.2), (0.12, 0.36), (0, 0.36)], tk.frame(c), tk.DARK, "Root", on, sides=8)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            p = c + V(math.cos(a), math.sin(a), 0) * 0.29
            k.block("Pillar", (0.085, 0.085, 1.0), tk.frame(p + V(0, 0, 0.6), rot=(0, 0, math.degrees(a))), tk.BLUE.but(panel=0.12), "Root", on,
                    taper=(0.8, 0.8), bevel=0.008)
            k.block("PillarCap", (0.1, 0.1, 0.07), tk.frame(p + V(0, 0, 1.12), rot=(0, 0, math.degrees(a))), tk.WHITE.but(panel=0), "Root", on, bevel=0.006)
            k.block("PillarFoot", (0.12, 0.12, 0.08), tk.frame(p + V(0, 0, 0.14), rot=(0, 0, math.degrees(a))), tk.WHITE.but(panel=0), "Root", on, bevel=0.006)
        for zz in (0.5, 0.88):
            k.torus("Containment", c + V(0, 0, zz), UP, 0.27, 0.018, bk.glow("cyan", 1.6), "Root", on, seg=24, sides=5)
        k.torus("Crown", c + V(0, 0, 1.16), UP, 0.27, 0.035, tk.WHITE.but(panel=0), "Root", on, seg=24, sides=6)
        k.lathe("CrownCap", [(0, 1.14), (0.12, 1.14), (0.1, 1.22), (0.04, 1.26), (0, 1.26)], tk.frame(c), tk.GUNMETAL, "Root", on, sides=8)
        k.cyl("Emitter", c + V(0, 0, 1.13), c + V(0, 0, 1.1), 0.05, look=bk.glow("crysium", 1.6), bone="Root", on=on, sides=8, bevel=0.0)
        # Cables from the plinth to the command post.
        for dz in (0.05, 0.09):
            k.cyl("Cable", c + V(-0.3, 0.05, dz), V(0.5, 0.42, DECK_Z + dz), 0.02, look=tk.RUBBER, bone="Root", on=on, sides=6, bevel=0.0)

    def crystal(self, k, look=None, on=("Base",)):
        c = PIVOTS["Crystal"]
        L = look or bk.glow((0.72, 0.66, 1.0), 1.0, lens=0.13)
        prof = [(0, 0), (0.1, 0.12), (0.13, 0.22), (0.13, 0.56), (0.09, 0.68), (0, 0.78)]
        k.lathe("Crystal", prof, tk.frame(c, rot=(0, 0, 12)), L, "Root", list(on), sides=6, bevel=0.0)
        k.lathe("Shard", [(0, 0), (0.045, 0.05), (0.05, 0.16), (0, 0.27)], tk.frame(c + V(0.07, 0.02, 0.06), rot=(14, 18, 40)), L, "Root", list(on),
                sides=5, bevel=0.0)

    def habitat(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        c = PIVOTS["Habitat"]

        def module(centre, r, length, paint):
            p0, p1 = centre + V(0, -length / 2, 0), centre + V(0, length / 2, 0)
            k.cyl("Hab", p0, p1, r, look=L(paint.but(rings=0.16, panel=0)), bone="Root", on=on, sides=14)
            for p, d in ((p0, V(0, -1, 0)), (p1, V(0, 1, 0))):
                k.lathe("HabCap", [(0, 0), (r, 0), (r * 0.9, r * 0.28), (r * 0.5, r * 0.45), (0, r * 0.5)], tk.frame(p, z=d), L(tk.WHITE.but(panel=0)),
                        "Root", on, sides=14)
            if look is None:
                for j in range(3):
                    y = -length * 0.3 + j * length * 0.3
                    pp = centre + V(-r * 0.97, y, r * 0.15)
                    k.cyl("Porthole", pp + V(0.012, 0, 0), pp - V(0.012, 0, 0), 0.028, look=bk.glow("base", 1.5), bone="Root", on=on, sides=8, bevel=0.0)
        module(c + V(0, 0, 0.19), 0.17, 0.72, tk.BLUE)
        k.block("Cradle", (0.3, 0.12, 0.08), tk.frame(c + V(0, -0.22, 0.04)), L(tk.GUNMETAL), "Root", on, bevel=0.004)
        k.block("Cradle", (0.3, 0.12, 0.08), tk.frame(c + V(0, 0.22, 0.04)), L(tk.GUNMETAL), "Root", on, bevel=0.004)
        module(c + V(0.04, 0.08, 0.5), 0.13, 0.5, tk.WHITE)
        k.cyl("Link", c + V(0.15, 0.3, 0.2), V(-0.48, 0.42, DECK_Z + 0.2), 0.07, look=L(tk.GUNMETAL.but(rings=0.05)), bone="Root", on=on, sides=10)

    def hardpoints(self, k):
        on = ["Base"]
        for s in (-1, 1):
            c = V(s * 1.17, 0.05, DECK_Z)
            k.block("Junction", (0.2, 0.38, 0.17), tk.frame(c + V(0, 0, 0.085)), tk.GUNMETAL, "Root", on, bevel=0.008)
            k.block("JunctionLid", (0.21, 0.36, 0.03), tk.frame(c + V(0, 0, 0.18)), tk.WHITE.but(panel=0), "Root", on, bevel=0.005)
            k.block("JunctionStripe", (0.012, 0.34, 0.03), tk.frame(c + V(s * 0.103, 0, 0.12)), tk.HAZARD, "Root", on, bevel=0.0)
            for dy in (-0.09, 0.09):
                k.cyl("LinkSocket", c + V(s * 0.1, dy, 0.07), c + V(s * 0.13, dy, 0.07), 0.026, look=tk.DARK, bone="Root", on=on, sides=10, bevel=0.0)
                k.cyl("LinkGlow", c + V(s * 0.128, dy, 0.07), c + V(s * 0.135, dy, 0.07), 0.016, look=bk.glow("base", 1.7), bone="Root", on=on, sides=8,
                      bevel=0.0)

    # ---------------------------------------------------------------------------------------------- states
    def damage(self, k):
        on = ["Damage"]
        rng = np.random.default_rng(3)
        z = DECK_Z
        for (x, y, zz, r, seed) in ((0.0, 0.4, z + 0.493, 0.4, 1), (0.2, 0.48, z + 0.793, 0.22, 2), (0.74, -0.5, z + 0.063, 0.3, 3),
                                    (-0.35, -0.55, z + 0.003, 0.36, 4), (0.45, -0.2, z + 0.003, 0.26, 5), (-0.95, 0.6, z + 0.003, 0.3, 6),
                                    (0.95, 0.85, z + 0.003, 0.22, 7)):
            k.plate("Soot", blot(r, seed), 0.004, tk.frame((x, y, zz)), bk.burn(tk.WHITE), "Root", on, bevel=0.0)
        # Smashed glass band: dark plates with hot edges over two panes.
        for x in (-0.15, 0.3):
            bk.shards(k, "Smash", bk.rect_poly(0.26, 0.18), 0.008, tk.frame((x, 0.04, z + 0.33), rot=(64, 0, 0)), bk.burn(tk.GLASS), "Root", on,
                      count=3, gap=0.01, lift=0.006, tilt=6, seed=int(x * 10) + 9)
            k.block("SmashGlow", (0.22, 0.008, 0.01), tk.frame((x, 0.025, z + 0.25)), bk.glow("ember", 1.3), "Root", on, bevel=0.0)
        # Torn deck plate near the ramp, a bent antenna, a toppled crate, hot edges.
        bk.shards(k, "TornPlate", bk.rect_poly(0.42, 0.3), 0.025, tk.frame((0.35, -0.82, z + 0.03), rot=(10, -8, 15)), bk.scorch(tk.WHITE, 0.5), "Root",
                  on, count=3, gap=0.02, lift=0.04, tilt=24, seed=12)
        k.block("TornGlow", (0.3, 0.02, 0.012), tk.frame((0.35, -0.8, z + 0.008), rot=(0, 0, 15)), bk.glow("ember", 1.4), "Root", on, bevel=0.0)
        k.cyl("BentAntenna", V(0.3, 0.36, z + 0.79), V(0.48, 0.2, z + 0.98), 0.009, look=bk.burn(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        k.block("FallenCrate", (0.17, 0.17, 0.17), tk.frame((-0.95, -0.85, z + 0.06), rot=(38, 12, 30)), bk.scorch(tk.BLUE, 0.6), "Root", on, bevel=0.006)
        for i in range(8):
            p = V(rng.uniform(-1.1, 1.1), rng.uniform(-1.0, -0.2), z)
            size = rng.uniform(0.03, 0.07, 3) * (1, 1, 0.4)
            k.block("Chip", tuple(size), tk.frame(p + V(0, 0, size[2] / 2), rot=(rng.uniform(-20, 20), rng.uniform(-20, 20), rng.uniform(0, 180))),
                    bk.scorch([tk.WHITE, tk.BLUE, tk.GUNMETAL][i % 3], 0.4), "Root", on, bevel=0.003)
        for p in ((0.82 - 0.3, 0.42 + 0.1, z + 0.6), (-0.84 - 0.18, -0.3, z + 0.3)):
            k.block("HotSeam", (0.012, 0.12, 0.08), tk.frame(p), bk.glow("ember", 1.2), "Root", on, bevel=0.0)

    def wreck(self, k):
        on = ["Wreck"]
        B = bk.burn
        z = DECK_Z
        rng = np.random.default_rng(23)
        self.deck(k, look=B, on=on)
        for i, (cx, cy, seed) in enumerate(((-0.6, -0.4, 1), (0.6, -0.4, 2), (-0.6, 0.55, 3), (0.6, 0.55, 4))):
            bk.shards(k, "DeckShard", bk.rect_poly(1.18, 0.9, 0.05), 0.03, tk.frame((cx, cy, 0.215)), B(tk.WHITE), "Root", on, count=4, gap=0.025,
                      lift=0.02, tilt=6, seed=seed)
        # The command post caved in: a shortened lower storey, the roof slab fallen in, the upper storey beside it.
        k.block("Storey1", (1.0, 0.7, 0.26), tk.frame((0, 0.4, z + 0.13)), B(tk.BLUE), "Root", on, taper=(0.97, 0.95), bevel=0.012)
        k.block("Roof", (1.06, 0.76, 0.05), tk.frame((0.05, 0.42, z + 0.26), rot=(14, -9, 4)), B(tk.WHITE), "Root", on, bevel=0.008)
        k.block("Storey2", (0.56, 0.44, 0.26), tk.frame((0.72, 0.85, z + 0.12), rot=(0, 72, 20)), B(tk.BLUE), "Root", on, bevel=0.01)
        k.block("GlassStub", (0.9, 0.03, 0.1), tk.frame((0, 0.06, z + 0.2), rot=(-26, 0, 0)), B(tk.GLASS), "Root", on, bevel=0.004)
        # The mast toppled across the deck, its dish on the ground.
        foot = V(-0.3, 0.62, z + 0.25)
        tip = V(-1.35, -0.55, 0.06)
        for off in (V(0.05, 0.05, 0), V(-0.05, -0.05, 0), V(0.05, -0.05, 0.04)):
            k.cyl("MastLeg", foot + off, tip + off * 0.4, 0.016, 0.012, look=B(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        k.lathe("Dish", [(0, 0), (0.24, 0.07), (0.25, 0.085), (0, 0.025)], tk.frame((-1.55, -0.75, 0.06), rot=(110, 0, 30)), B(tk.WHITE), "Root",
                on, sides=14, bevel=0.0)
        # The core cage broken: two short pillars, two fallen, the crystal a dark cracked stub.
        c = PIVOTS["Core"]
        k.lathe("CorePlinth", [(0, 0), (0.36, 0), (0.36, 0.08), (0.32, 0.12), (0, 0.12)], tk.frame(c), B(tk.GUNMETAL), "Root", on, sides=8)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            p = c + V(math.cos(a), math.sin(a), 0) * 0.29
            if i < 2:
                k.block("PillarStub", (0.085, 0.085, 0.34 + 0.1 * i), tk.frame(p + V(0, 0, 0.17 + 0.05 * i), rot=(0, 0, math.degrees(a))), B(tk.BLUE),
                        "Root", on, bevel=0.008)
            else:
                d = V(math.cos(a), math.sin(a), 0)
                k.block("PillarFallen", (0.085, 0.085, 0.8), tk.frame(p + d * 0.42 + V(0, 0, 0.06), z=d + V(0, 0, 0.12), y=UP), B(tk.BLUE), "Root",
                        on, bevel=0.008)
        dark = tk.GLASS.but(paint=(0.12, 0.09, 0.16), emit=bk.GLOW["crysium"], glow=0.35, fam="glow")
        k.lathe("CrystalStub", [(0, 0), (0.1, 0.1), (0.13, 0.2), (0.11, 0.3), (0, 0.36)], tk.frame(c + V(0, 0, 0.12), rot=(9, 6, 12)), dark, "Root",
                on, sides=6, bevel=0.0)
        k.lathe("CrystalShard", [(0, 0), (0.05, 0.05), (0.05, 0.18), (0, 0.3)], tk.frame(c + V(-0.35, -0.3, 0.03), rot=(80, 0, 60)), dark, "Root",
                on, sides=5, bevel=0.0)
        # The pad split, the habitat cylinder lying cracked.
        self.pad(k, look=B, on=on)
        cH = PIVOTS["Habitat"]
        k.cyl("HabFallen", cH + V(-0.1, -0.36, 0.17), cH + V(0.15, 0.32, 0.12), 0.17, look=B(tk.BLUE.but(rings=0.16)), bone="Root", on=on, sides=14)
        for i in range(12):
            p = V(rng.uniform(-1.6, 1.6), rng.uniform(-1.5, 1.3), 0.0)
            onDeck = abs(p.x) < 1.25 and abs(p.y) < 1.0
            size = rng.uniform(0.05, 0.12, 3) * (1, 1, 0.3)
            k.block("Scrap", tuple(size), tk.frame(p + V(0, 0, (z if onDeck else 0) + size[2] / 2), rot=(rng.uniform(-25, 25), rng.uniform(-25, 25), rng.uniform(0, 180))),
                    B([tk.WHITE, tk.BLUE, tk.GUNMETAL, tk.STEEL][i % 4]), "Root", on, bevel=0.004)
        for p in ((0.05, 0.1, z + 0.29), (0.5, 0.3, z + 0.27), (-0.2, 0.7, z + 0.27), (0.82, 0.3, z + 0.2), (-0.8, -0.2, z + 0.32)):
            k.block("Ember", (0.06, 0.03, 0.014), tk.frame(p, rot=(0, 0, 25)), bk.glow("ember", 1.1), "Root", on, bevel=0.0)


def blot(r, seed):
    rng = np.random.default_rng(seed)
    return [(math.cos(2 * math.pi * i / 11) * r * rng.uniform(0.7, 1.0), math.sin(2 * math.pi * i / 11) * r * rng.uniform(0.7, 1.0)) for i in range(11)]


def sockets(parent=None):
    out = []
    for name, p in SOCKETS.items():
        e = C.link(bpy.data.objects.new(name, None))
        e.empty_display_size = 0.08
        e.location = p
        if parent is not None:
            e.parent = parent
        out.append(e)
    return out


def module_objects(base, mat, lod=False, label=""):
    objs = {}
    for m in MODULES:
        pieces = base.module[m]
        name = f"{NAME}_{m}" + ("_LOD1" if lod else "") + label
        objs[m] = bk.module_object(pieces, name, PIVOTS[m] if m != "Deck" else V(0, 0, 0), mat, lod=lod)
    return objs


def state_object(base, mat, state, label=""):
    o = bk.module_object(base.module[state], f"{NAME}_{state}{label}", V(0, 0, 0), mat)
    return o


def build_base(bake=True):
    base = Base()
    kit = base.kit()
    mat, report = bk.bake_set(kit, OUT, 1024, 512, bake)
    lod0 = module_objects(base, mat)
    lod1 = module_objects(base, mat, lod=True)
    socks = sockets()
    bk.export_fbx(os.path.join(OUT, f"{NAME}.fbx"), list(lod0.values()) + list(lod1.values()) + socks, animated=False)
    dmg = state_object(base, mat, "Damage")
    bk.export_fbx(os.path.join(OUT, f"{NAME}_Damage.fbx"), [dmg], animated=False)
    wreck = state_object(base, mat, "Wreck")
    bk.export_fbx(os.path.join(OUT, f"{NAME}_Wreck.fbx"), [wreck], animated=False)
    report["modules"] = {m: {"object": f"{NAME}_{m}", "pivot": [round(c, 3) for c in (PIVOTS[m] if m != "Deck" else V(0, 0, 0))],
                             "tris": C.tri_count(lod0[m]), "tris_lod1": C.tri_count(lod1[m])} for m in MODULES}
    report["tris_total"] = sum(v["tris"] for v in report["modules"].values())
    report["tris_total_lod1"] = sum(v["tris_lod1"] for v in report["modules"].values())
    report["damage_overlay"] = {"fbx": f"{NAME}_Damage.fbx", "tris": C.tri_count(dmg)}
    report["wreck"] = {"fbx": f"{NAME}_Wreck.fbx", "tris": C.tri_count(wreck)}
    report["sockets"] = {n: [round(c, 3) for c in p] for n, p in SOCKETS.items()}
    report["pivot"] = "base centre on the ground (LevelVisualBuilder.BasePosition); -Y (Unity +Z) faces the road"
    report["timelapse_order"] = MODULES
    report["scale"] = "final size: do not apply LevelVisualBuilder.BaseScale"
    tk.write_json(os.path.join(OUT, f"{NAME}_report.json"), report)
    C.log(NAME, report["tris_total"], report["tris_total_lod1"], report["damage_overlay"], report["wreck"])
    bk.remove(list(lod0.values()) + list(lod1.values()) + socks + [dmg, wreck])
    return base, kit, mat, report


# --------------------------------------------------------------------------------------------------- base turret

class BaseTurret(bt.Gun):
    """The Gun Turret's head on a square bastion pad: part of the base, never a tower the player placed."""
    name = "BaseTurret"
    keep = ["T1", "T2", "T3A"]

    def kit(self):
        k = tk.Kit("Tower_" + self.name, self.variants)
        self.stars = bastion(k, self.accent)
        self.pieces(k)
        keep = set(self.keep)
        k.pieces = [p for p in k.pieces if p.variants & keep]
        for p in k.pieces:
            p.variants &= keep
        k.variants = list(self.keep)
        self.grow_head(k, V(0, 0, bt.YAW_Z))
        self.variants = list(self.keep)
        return k


def bastion(k, accent):
    """Square pad, 0.84 across: gunmetal foot, hazard band, blue walls with vents, a white parapet, the race ring
    and the rank plate with the stars on the camera side."""
    I = tk.frame()
    k.block("BastionFoot", (0.86, 0.86, 0.07), tk.frame((0, 0, 0.035)), tk.GUNMETAL, "Root", taper=(0.96, 0.96), bevel=0.008)
    k.block("BastionHazard", (0.872, 0.872, 0.03), tk.frame((0, 0, 0.045)), tk.HAZARD, "Root", bevel=0.003)
    k.block("BastionBody", (0.8, 0.8, 0.13), tk.frame((0, 0, 0.13)), tk.BLUE.but(panel=0.13), "Root", taper=(0.95, 0.95), bevel=0.01)
    k.block("BastionDeck", (0.78, 0.78, 0.024), tk.frame((0, 0, 0.202)), tk.WHITE.but(panel=0.13), "Root", bevel=0.005)
    for a in (0, 90, 180, 270):
        r = math.radians(a)
        d = V(math.sin(r), -math.cos(r), 0)
        if a != 180:
            k.block("Parapet", (0.62, 0.06, 0.07), tk.frame(d * 0.36 + V(0, 0, 0.245), rot=(0, 0, a)), tk.WHITE.but(panel=0.1), "Root", taper=(1, 0.6),
                    bevel=0.006)
        k.block("WallVent", (0.36, 0.012, 0.05), tk.frame(d * 0.395 + V(0, 0, 0.12), rot=(0, 0, a)), tk.DARK, "Root", bevel=0.002)
    for x in (-1, 1):
        for y in (-1, 1):
            k.block("CornerPost", (0.1, 0.1, 0.3), tk.frame((x * 0.37, y * 0.37, 0.15)), tk.GUNMETAL, "Root", bevel=0.008)
            k.block("CornerCap", (0.11, 0.11, 0.05), tk.frame((x * 0.37, y * 0.37, 0.31)), tk.HAZARD, "Root", bevel=0.004)
    k.block("FrontPlate", (0.56, 0.012, 0.06), tk.frame((0, -0.405, 0.13)), tk.WHITE.but(panel=0), "Root", bevel=0.002)
    k.block("FrontLight", (0.3, 0.014, 0.012), tk.frame((0, -0.41, 0.17)), bk.glow("base", 1.5), "Root", bevel=0.0)
    k.cyl("PadCollar", (0, 0, 0.206), (0, 0, bt.YAW_Z), 0.186, 0.176, look=tk.GUNMETAL, bone="Root", sides=20)
    k.torus("PadRace", (0, 0, 0.214), UP, 0.206, 0.008, bk.glow("base", 1.3), "Root", seg=24, sides=4)
    return bt.rank_plate(k, V(0, 0.402, 0.16), 44.0, 0.09, bt.RANK["star"], width=0.3, height=0.1)


TURRET_TIERS = {"T1": "T1", "T2": "T2", "T3A": "T3"}


def build_turret(bake=True):
    spec = BaseTurret()
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, 1024, 512, bake)
    report["variants"] = {}
    for v, label in TURRET_TIERS.items():
        objs = bt.build_variant(spec, kit, v, mat)
        arm, lod0, lod1 = objs
        acts = spec.clips(arm, v)
        bk.export_fbx(os.path.join(out_dir, f"{kit.name}_{label}.fbx"), objs, animated=True)
        report["variants"][label] = {"fbx": f"{kit.name}_{label}.fbx", "look": f"Gun {v}", "tris": C.tri_count(lod0), "tris_lod1": C.tri_count(lod1),
                                     "bones": [b.name for b in arm.data.bones], "clips": {a.name: int(a.frame_range[1]) for a in acts},
                                     "muzzles": {f"Muzzle_{i + 1}": [round(c, 4) for c in m] for i, (_, _, m) in enumerate(spec.barrels[v])}}
        bk.remove(objs)
        bk.clear_actions()
    report["pivot"] = "pad centre on the ground; rig, clips and aiming as Tower_Gun (round 7)"
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    C.log(kit.name, {k: (v["tris"], v["tris_lod1"]) for k, v in report["variants"].items()})
    return spec, kit, mat, report


# --------------------------------------------------------------------------------------------------- previews

def place_base(base, mat, state="intact", at=V(0, 0, 0), label="", modules=None):
    objs = []
    if state == "wreck":
        o = state_object(base, mat, "Wreck", label)
        o.location = at
        return [o]
    mods = module_objects(base, mat, label=label)
    for m, o in mods.items():
        if modules is not None and m not in modules:
            bk.remove([o])
            continue
        o.location = o.location + at
        objs.append(o)
    if state == "damaged":
        d = state_object(base, mat, "Damage", label)
        d.location = at
        objs.append(d)
    return objs


def previews(base, mat, turret, out):
    tspec, tkit, tmat = turret if turret else (None, None, None)
    stage = bk.road_stage(2.0, 12, axis="y", centre=(0, -7.5))
    placed = place_base(base, mat, label="g")
    if tspec:
        for x, v in ((-1.9, "T2"), (1.9, "T1")):
            objs = bt.build_variant(tspec, tkit, v, tmat, name=f"BT{v}{x}", lod=False)
            objs[0].location = (x, -1.0, 0)
            bt.pose(objs[0], 140 if x < 0 else 220, 6, bt.stars_for(v))
            placed += objs
    placed += bk.tower("Gun", "T2", (1.6, -3.2, 0), 0, head_deg=200)
    placed += bk.tower("Laser", "T1", (-1.6, -4.0, 0), 0, head_deg=160)
    sk = bk.skitters([(0.3, -3.0, 0), (-0.4, -3.8, 0), (0.2, -5.0, 0), (-0.3, -6.2, 0)], yaw=0.0)
    bk.render_game(V(0, -1.6, 0.4), os.path.join(out, "base_game.png"), zoom=1.0)
    bk.render_game(V(0, -0.8, 0.5), os.path.join(out, "base_zoom.png"), zoom=2.2)
    bk.unstage(sk)
    bk.remove(placed)
    # 3/4 close-ups from the front left and the front right.
    objs = place_base(base, mat, label="c")
    for name, az in (("base_close_front", 200), ("base_close_side", 140), ("base_close_back", 25)):
        bk.render_close(V(0, 0, 0.6), os.path.join(out, name + ".png"), 6.2, azimuth=az, elevation=28, lens=50, light_scale=1.8)
    bk.render_close(V(0.2, 0.2, 0.75), os.path.join(out, "base_macro.png"), 1.6, azimuth=195, elevation=14, lens=55, width=1280, height=720,
                    light_scale=1.0)
    bk.remove(objs)
    # States row at play zoom: intact, damaged, wreck.
    placed = []
    for i, st in enumerate(("intact", "damaged", "wreck")):
        placed += place_base(base, mat, state=st, at=V(-(i - 1) * 3.6, 0, 0), label=f"s{i}")
    bk.render_game(V(0, 0, 0.4), os.path.join(out, "base_states_game.png"), zoom=1.3, width=1280, height=720)
    bk.render_game(V(0, 0, 0.4), os.path.join(out, "base_states_zoom.png"), zoom=2.0, width=1600, height=720)
    bk.remove(placed)
    for st in ("damaged", "wreck"):
        objs = place_base(base, mat, state=st, label="d" + st)
        bk.render_close(V(0, 0, 0.5), os.path.join(out, f"base_{st}_close.png"), 6.2, azimuth=200, elevation=28, lens=50, light_scale=1.8)
        bk.remove(objs)
    # Timelapse: the modules appear one after another.
    shots = []
    steps = [["Deck"], ["Deck", "Command"], ["Deck", "Command", "Dish", "Pad"], ["Deck", "Command", "Dish", "Pad", "Core"],
             ["Deck", "Command", "Dish", "Pad", "Core", "Crystal", "Habitat"], MODULES]
    for i, mods in enumerate(steps):
        objs = place_base(base, mat, label=f"tl{i}", modules=mods)
        p = os.path.join(out, f"_base_tl_{i}.png")
        bk.render_close(V(0, 0, 0.6), p, 6.6, azimuth=200, elevation=30, lens=50, width=480, height=360, light_scale=1.8, bloom=False)
        shots.append(p)
        bk.remove(objs)
    C.contact_sheet(shots, os.path.join(out, "base_timelapse_strip.png"), 6, 400)
    for p in shots:
        os.remove(p)
    # Base turret tiers on their own.
    if tspec:
        placed = []
        for i, v in enumerate(TURRET_TIERS):
            objs = bt.build_variant(tspec, tkit, v, tmat, name=f"BTt{v}", lod=False)
            objs[0].location = (-(i - 1) * 1.15, 0, 0)
            bt.pose(objs[0], 115, 6, bt.stars_for(v))
            placed += objs
        placed += bk.tower("Gun", "T1", (2.5, 0, 0), 0)
        bk.render_game(V(0.4, 0, 0.3), os.path.join(out, "baseturret_tiers_zoom.png"), zoom=2.6, width=1280, height=720)
        bk.render_close(V(0, 0, 0.32), os.path.join(out, "baseturret_close.png"), 3.4, azimuth=28, elevation=26, lens=50, light_scale=1.2)
        bk.remove(placed)
    bk.remove(stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    bake = not args.get("nobake")
    C.preview_world()
    tk.metal_detail_map()
    turret = None
    base = mat = None
    if only in (None, "Turret"):
        spec, tkit, tmat, _ = build_turret(bake)
        turret = (spec, tkit, tmat)
    if only in (None, "Base"):
        base, kit, mat, _ = build_base(bake)
    if not args.get("norender") and base is not None:
        previews(base, mat, turret, out)
    C.save_blend(NAME if only is None else NAME + "_" + only)


if __name__ == "__main__":
    main()
