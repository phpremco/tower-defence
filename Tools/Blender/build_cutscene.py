"""Round 13: the opening cutscene's machines (see ArtSource/DESIGN_NOTES.md, round 13).

- Capsule: the landing pod (heat shield as its own mesh, retro jets, legs, a front ramp). Clips Land, Open.
- Drill: the extraction derrick with a drill string on its own bones. Clips Spin, Sink.
- Mothership: spine, command module, turning habitat ring, cargo, capsule bay, radiators, engines. Clip Spin.
- Beacon: Mercer's survey beacon (tripod, telescoping mast, lamp, ring). Clips Deploy, Idle.

All in the round-7/9 hard-surface kit (tower_kit pieces, one atlas, self-bake, four maps + Low set), one material
each, skinned to a small rig with sockets. Cutscene-only: no LOD1 (see the report).

Out: `Exports/Cutscene_<Name>/Cutscene_<Name>.fbx`, maps, `<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_cutscene.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Capsule,Drill,Mothership,Beacon]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP, FWD, XAX  # noqa: E402

BACK = Vector((0, 1, 0))


def polar(a_deg, r, z):
    a = math.radians(a_deg)
    return V(math.cos(a) * r, math.sin(a) * r, z)


def ease(t):
    return bk.ease(t)


def seg(t, a, b):
    return min(1.0, max(0.0, (t - a) / (b - a)))


class Spec:
    name = ""
    tex = 1024
    low = 512
    variants = ["Main"]
    extras = []          # (variant, object suffix)

    def __init__(self):
        self.sockets = {}
        self.bone_list = [tk.Bone("Root", (0, 0, 0), UP, length=0.2)]

    def kit(self):
        k = tk.Kit(self.name, self.variants)
        self.pieces(k)
        return k

    def bones(self):
        return self.bone_list

    def clips(self, arm):
        return []


# -------------------------------------------------------------------------------------------------------------
# Landing capsule
# -------------------------------------------------------------------------------------------------------------

def hull_r(z, prof):
    """Corner radius of a hex lathe profile [(r, z)] at height z."""
    for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / max(z1 - z0, 1e-9)
    return prof[-1][0]


CAPSULE_HULL = [(0.64, 0.44), (0.6, 0.62), (0.47, 1.26)]
APOTHEM = math.cos(math.radians(30))


class Capsule(Spec):
    name = "Cutscene_Capsule"
    variants = ["Main", "Shield"]
    extras = [("Shield", "_Shield")]
    HINGE_Z, TOP_Z = 0.46, 1.22
    LEG_ANGLES = (90, 210, 330)

    def face_point(self, a_deg, z, out=0.0):
        """A point on the centre line of the hex face facing angle a_deg, at height z, pushed out by `out`."""
        r = hull_r(z, CAPSULE_HULL) * APOTHEM + out
        return polar(a_deg, r, z)

    def face_frame(self, a_deg, z, out=0.0):
        """Frame on a hex face: local z the face normal (outwards), local y up the face."""
        p0, p1 = self.face_point(a_deg, z - 0.05), self.face_point(a_deg, z + 0.05)
        up = (p1 - p0).normalized()
        n = polar(a_deg, 1, 0)
        n = (n - up * n.dot(up)).normalized()
        return tk.frame(self.face_point(a_deg, z, out), z=n, y=up)

    def pieces(self, k):
        on = ["Main"]
        # Heat shield (own mesh: Unity fades its glow after re-entry).
        shield = [(0, 0.12), (0.3, 0.135), (0.52, 0.18), (0.66, 0.26), (0.71, 0.31), (0.69, 0.35), (0, 0.35)]
        k.lathe("Shield", shield, tk.frame(), sk.SHIELD, "Root", ["Shield"], sides=24, bevel=0.0)
        k.torus("ShieldRim", V(0, 0, 0.31), UP, 0.705, 0.018, sk.glow("ember", 1.4), "Root", ["Shield"], seg=32, sides=4)
        for i in range(12):
            a = i * 30 + 15
            k.block("ShieldTile", (0.2, 0.012, 0.06), tk.frame(polar(a, 0.58, 0.215), z=polar(a, 1, 0) + V(0, 0, -1.3), y=polar(a + 90, 1, 0)),
                    sk.SHIELD.but(glow=0.8), "Root", ["Shield"], bevel=0.0)
        # Hull: hex lathes stacked (front face flat towards -Y).
        k.lathe("Waist", [(0, 0.35), (0.67, 0.35), (0.69, 0.39), (0.665, 0.44), (0, 0.44)], tk.frame(), tk.GUNMETAL, "Root", on, sides=6,
                start=0.0, bevel=0.008)
        k.lathe("Band", [(0, 0.44), (0.64, 0.44), (0.6, 0.62), (0, 0.62)], tk.frame(), tk.BLUE.but(panel=0.11), "Root", on, sides=6,
                start=0.0, bevel=0.008)
        k.lathe("Hazard", [(0, 0.605), (0.607, 0.605), (0.598, 0.645), (0, 0.645)], tk.frame(), tk.HAZARD, "Root", on, sides=6, start=0.0,
                bevel=0.0)
        k.lathe("Hull", [(0, 0.62), (0.6, 0.62), (0.47, 1.26), (0, 1.26)], tk.frame(), tk.WHITE.but(panel=0.13), "Root", on, sides=6,
                start=0.0, bevel=0.01)
        k.lathe("Shoulder", [(0, 1.26), (0.48, 1.26), (0.36, 1.42), (0, 1.42)], tk.frame(), tk.GUNMETAL, "Root", on, sides=6, start=0.0,
                bevel=0.008)
        k.lathe("Cap", [(0, 1.42), (0.31, 1.42), (0.25, 1.5), (0, 1.52)], tk.frame(), tk.WHITE.but(panel=0), "Root", on, sides=12, bevel=0.006)
        k.cyl("Chute", (0, 0, 1.5), (0, 0, 1.6), 0.16, 0.15, look=tk.BLUE.but(panel=0), bone="Root", on=on, sides=12)
        k.torus("ChuteBand", V(0, 0, 1.56), UP, 0.158, 0.012, tk.YELLOW, "Root", on, seg=16, sides=4)
        k.cyl("Antenna", (0.16, 0.12, 1.52), (0.18, 0.14, 1.88), 0.012, 0.008, look=tk.STEEL, bone="Root", on=on, sides=6, bevel=0.0)
        lamp = V(0.18, 0.14, 1.9)
        k.lathe("Lamp", [(0, -0.03), (0.03, -0.02), (0.03, 0.02), (0, 0.04)], tk.frame(lamp), sk.glow("base", 2.2), "Root", on, sides=8, bevel=0.0)
        self.sockets["Beacon"] = lamp
        # Doorway under the ramp: a dark recess with a lit frame and the seat (seen once the ramp is down).
        f = self.face_frame(270, 0.84, 0.004)
        k.block("Doorway", (0.4, 0.74, 0.01), f, tk.DARK.but(panel=0), "Root", on, bevel=0.0)
        for x in (-0.19, 0.19):
            k.block("DoorLight", (0.018, 0.7, 0.012), f @ tk.frame((x, 0, 0.004)), sk.glow("cyan", 1.8), "Root", on, bevel=0.0)
        k.block("DoorLight", (0.36, 0.02, 0.012), f @ tk.frame((0, 0.35, 0.004)), sk.glow("cyan", 2.0), "Root", on, bevel=0.0)
        k.block("Seat", (0.26, 0.3, 0.018), f @ tk.frame((0, 0.08, 0.008)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.004)
        k.block("SeatHead", (0.14, 0.1, 0.022), f @ tk.frame((0, 0.27, 0.01)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.004)
        k.block("SeatBelt", (0.03, 0.26, 0.026), f @ tk.frame((-0.06, 0.1, 0.012)), tk.YELLOW, "Root", on, bevel=0.0)
        k.block("SeatBelt", (0.03, 0.26, 0.026), f @ tk.frame((0.06, 0.1, 0.012)), tk.YELLOW, "Root", on, bevel=0.0)
        # Ramp: hinged at its foot, it folds down onto the ground (bone Ramp).
        p0 = self.face_point(270, self.HINGE_Z, 0.03)
        p1 = self.face_point(270, self.TOP_Z, 0.03)
        door = (p1 - p0)
        length = door.length
        dd = door.normalized()
        normal = V(0, -1, 0)
        normal = (normal - dd * normal.dot(dd)).normalized()
        fr = tk.frame(p0 + dd * (length / 2), z=dd, y=normal)
        k.block("Ramp", (0.42, 0.04, length), fr, tk.WHITE.but(panel=0.12), "Ramp", on, bevel=0.008)
        k.block("RampBand", (0.43, 0.044, 0.16), fr @ tk.frame((0, 0, -length / 2 + 0.1)), tk.BLUE.but(panel=0), "Ramp", on, bevel=0.004)
        for x in (-0.2, 0.2):
            k.block("RampEdge", (0.03, 0.05, length - 0.02), fr @ tk.frame((x, 0.002, 0)), tk.HAZARD, "Ramp", on, bevel=0.003)
        k.block("RampPort", (0.16, 0.014, 0.12), fr @ tk.frame((0, 0.022, length * 0.27)), tk.GLASS, "Ramp", on, bevel=0.004)
        k.block("RampPortGlow", (0.12, 0.016, 0.012), fr @ tk.frame((0, 0.022, length * 0.27 - 0.075)), sk.glow("cyan", 1.6), "Ramp", on,
                bevel=0.0)
        k.block("Latch", (0.07, 0.03, 0.04), fr @ tk.frame((0, 0.03, length / 2 - 0.05)), tk.YELLOW, "Ramp", on, bevel=0.004)
        for i in range(6):
            z = -length / 2 + 0.12 + i * (length - 0.2) / 6
            k.block("Tread", (0.34, 0.012, 0.025), fr @ tk.frame((0, -0.024, z)), tk.RUBBER if i % 2 else tk.YELLOW.but(fam="metal"), "Ramp", on,
                    bevel=0.0)
        k.cyl("Hinge", p0 + V(-0.21, 0, 0), p0 + V(0.21, 0, 0), 0.025, look=tk.GUNMETAL, bone="Root", on=on, sides=8)
        self.ramp = (p0, dd, length)
        # Windows and the NTI mark on the side faces.
        for a in (210, 330):
            wf = self.face_frame(a, 1.02, 0.005)
            k.block("Window", (0.18, 0.12, 0.03), wf, tk.GLASS.but(emit=sk.GLOW["cyan"], glow=0.3), "Root", on, bevel=0.01)
            k.block("WindowRim", (0.21, 0.15, 0.02), wf @ tk.frame((0, 0, -0.008)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.004)
            k.block("WindowGlow", (0.14, 0.012, 0.012), wf @ tk.frame((0, -0.085, 0.006)), sk.glow("cyan", 1.5), "Root", on, bevel=0.0)
        sk.emblem(k, self.face_frame(150, 1.0, 0.012), "Root", on, scale=0.33, letters=False, prefix="Mark")
        sk.emblem(k, self.face_frame(30, 1.0, 0.012), "Root", on, scale=0.33, letters=False, prefix="Mark")
        # Retro jets: four pods on the waist with downward nozzles.
        for i, a in enumerate((45, 135, 225, 315)):
            pod = polar(a, 0.66, 0.43)
            k.block("JetPod", (0.13, 0.13, 0.17), tk.frame(pod, z=UP, y=polar(a, 1, 0)), tk.GUNMETAL, "Root", on, bevel=0.012)
            k.block("JetCap", (0.135, 0.135, 0.03), tk.frame(pod + V(0, 0, 0.085), z=UP, y=polar(a, 1, 0)), tk.HAZARD, "Root", on, bevel=0.004)
            d = (polar(a, 0.35, 0) + V(0, 0, -1)).normalized()
            exit_p = pod + V(0, 0, -0.08) + d * 0.12
            k.lathe("Nozzle", [(0.035, 0.0), (0.05, 0.03), (0.07, 0.12), (0.06, 0.125), (0.03, 0.04), (0, 0.04)],
                    tk.frame(pod + V(0, 0, -0.08), z=d), tk.STEEL, "Root", on, sides=10, bevel=0.0)
            k.lathe("NozzleGlow", [(0, 0.04), (0.032, 0.045), (0, 0.05)], tk.frame(pod + V(0, 0, -0.08), z=d), sk.glow("engine", 2.2), "Root", on,
                    sides=8, bevel=0.0)
            self.sockets[f"Jet_{i + 1}"] = (exit_p, d)
        # Landing legs (bone Leg_n at the hip).
        for i, a in enumerate(self.LEG_ANGLES):
            hip = polar(a, 0.57, 0.5)
            knee = polar(a, 0.9, 0.24)
            foot = polar(a, 1.0, 0.04)
            name = f"Leg_{i + 1}"
            k.block("LegMount", (0.12, 0.08, 0.12), tk.frame(hip, z=UP, y=polar(a, 1, 0)), tk.GUNMETAL, name, on, bevel=0.01)
            k.cyl("LegUpper", hip, knee, 0.036, 0.03, look=tk.GUNMETAL, bone=name, on=on, sides=8)
            k.cyl("LegLower", knee + (knee - foot).normalized() * 0.04, foot + V(0, 0, 0.03), 0.024, look=tk.STEEL, bone=name, on=on, sides=8)
            k.torus("LegBand", knee + (foot - knee) * 0.25, foot - knee, 0.03, 0.01, tk.YELLOW, name, on, seg=10, sides=4)
            k.cyl("Brace", polar(a, 0.66, 0.36), knee + (hip - knee) * 0.2, 0.016, look=tk.STEEL, bone=name, on=on, sides=6, bevel=0.0)
            k.lathe("Pad", [(0, 0.0), (0.11, 0.0), (0.12, 0.02), (0.06, 0.045), (0, 0.045)], tk.frame(foot - V(0, 0, 0.04)), tk.DARK, name, on,
                    sides=12, bevel=0.004)
            self.bone_list.append(tk.Bone(name, hip, foot - hip, "Root", length=0.15))
        self.bone_list.append(tk.Bone("Ramp", self.ramp[0], self.ramp[1], "Root", length=0.2))
        self.sockets["Exit"] = (V(0, -1.35, 0.0), FWD)
        self.sockets["ShieldCentre"] = V(0, 0, 0.12)

    def ramp_open_angle(self):
        """Rotation about +X that turns the ramp from its closed direction down onto the ground."""
        p0, dd, length = self.ramp
        drop = math.asin(min(0.99, p0.z / length))
        closed = math.atan2(dd.z, -dd.y)
        return closed + drop

    def clips(self, arm):
        stow = math.radians(122)
        open_a = self.ramp_open_angle()

        def legs(fold):
            spec = {}
            for i, a in enumerate(self.LEG_ANGLES):
                hip = polar(a, 0.57, 0.5)
                foot = polar(a, 1.0, 0.04)
                axis = (foot - hip).normalized().cross(UP).normalized()
                spec[f"Leg_{i + 1}"] = {"rot": (axis, stow * fold)}
            return spec

        def land(t, f):
            u = seg(t, 0.0, 0.75)
            k = 1 - ease(u)
            k -= 0.06 * math.sin(math.pi * seg(t, 0.75, 1.0))   # a small overshoot as the pads take the weight
            return legs(k)

        def open_(t, f):
            u = ease(seg(t, 0.18, 0.86))
            bounce = 0.035 * math.sin(math.pi * seg(t, 0.86, 1.0))
            ang = open_a * u - bounce * open_a
            unlatch = 0.02 * math.sin(math.pi * seg(t, 0.0, 0.18))
            return {"Ramp": {"rot": (XAX, ang + unlatch)}}

        def closed(t, f):
            return {}
        return [bk.clip(arm, "Land", 30, land), bk.clip(arm, "Open", 48, open_)]


# -------------------------------------------------------------------------------------------------------------
# Drill derrick
# -------------------------------------------------------------------------------------------------------------

class Drill(Spec):
    name = "Cutscene_Drill"
    BASE_HALF, TOP_HALF = 0.82, 0.2
    DECK, TOP = 0.34, 4.3
    BLOCK_Z = 3.55
    TRAVEL = 3.0
    STRING_TOP, BIT_TOP, BIT_TIP = 3.3, 1.5, 0.44

    def half(self, z):
        return self.BASE_HALF + (self.TOP_HALF - self.BASE_HALF) * (z - self.DECK) / (self.TOP - self.DECK)

    def pieces(self, k):
        on = ["Main"]
        k.block("Deck", (2.3, 2.3, 0.28), tk.frame((0, 0, 0.14)), tk.GUNMETAL.but(panel=0.3), "Root", on, bevel=0.02)
        for i in range(4):
            a = i * 90
            k.block("DeckEdge", (2.32, 0.1, 0.1), tk.frame(polar(a + 90, 1.11, 0.24), rot=(0, 0, a)), tk.HAZARD, "Root", on, bevel=0.006)
        k.block("DeckPlate", (2.0, 2.0, 0.06), tk.frame((0, 0, 0.31)), tk.WHITE.but(panel=0.25), "Root", on, bevel=0.01)
        k.lathe("Table", [(0, 0.33), (0.38, 0.33), (0.36, 0.42), (0.16, 0.42), (0.16, 0.4), (0, 0.4)], tk.frame(), tk.GUNMETAL, "Root", on, sides=20,
                bevel=0.0)
        k.torus("TableGlow", V(0, 0, 0.42), UP, 0.27, 0.025, sk.glow("cyan", 1.8), "Root", on, seg=24, sides=4)
        for x, y in ((-0.9, -0.9), (0.9, -0.9), (-0.9, 0.9)):
            k.block("Bollard", (0.12, 0.12, 0.2), tk.frame((x, y, 0.44)), tk.HAZARD, "Root", on, bevel=0.01)
        # Motor house with lit vents and an exhaust stack.
        k.block("House", (0.8, 0.66, 0.66), tk.frame((0.6, 0.62, 0.67)), tk.BLUE.but(panel=0.16), "Root", on, bevel=0.02)
        k.block("HouseRoof", (0.86, 0.72, 0.07), tk.frame((0.6, 0.62, 1.03)), tk.WHITE.but(panel=0), "Root", on, bevel=0.012)
        for j in range(3):
            k.block("Vent", (0.55, 0.012, 0.045), tk.frame((0.6, 0.285, 0.55 + j * 0.11)), sk.glow("cyan", 1.6), "Root", on, bevel=0.0)
        k.cyl("Stack", (0.86, 0.82, 1.05), (0.86, 0.82, 1.75), 0.075, 0.065, look=tk.STEEL.but(rings=0.08), bone="Root", on=on, sides=12)
        k.torus("StackCap", V(0.86, 0.82, 1.75), UP, 0.068, 0.018, tk.DARK, "Root", on, seg=12, sides=4)
        self.sockets["Exhaust"] = (V(0.86, 0.82, 1.78), UP)
        k.block("Panel", (0.32, 0.12, 0.5), tk.frame((-0.75, 0.7, 0.62), rot=(0, 0, 0)), tk.GUNMETAL, "Root", on, bevel=0.01)
        k.block("PanelScreen", (0.22, 0.012, 0.14), tk.frame((-0.75, 0.636, 0.74)), sk.glow("cyan", 1.4), "Root", on, bevel=0.0)
        # The derrick: four leaning legs, braces on every face, a work platform and the crown block.
        levels = [self.DECK, 1.3, 2.2, 3.0, 3.7, self.TOP]
        for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            b, t = self.half(self.DECK), self.half(self.TOP)
            k.cyl("Leg", (sx * b, sy * b, self.DECK), (sx * t, sy * t, self.TOP), 0.05, 0.04, look=tk.STEEL, bone="Root", on=on, sides=8, bevel=0.0)
            k.block("Shoe", (0.18, 0.18, 0.1), tk.frame((sx * b, sy * b, self.DECK + 0.05)), tk.DARK, "Root", on, bevel=0.008)
        for face in range(4):
            def corner(z, which):
                h = self.half(z)
                a = -1 if which == 0 else 1
                return [V(a * h, -h, z), V(h, a * h, z), V(-a * h, h, z), V(-h, -a * h, z)][face]
            for i in range(1, len(levels)):
                z0, z1 = levels[i - 1], levels[i]
                k.cyl("Rail", corner(z1, 0), corner(z1, 1), 0.024, look=tk.STEEL, bone="Root", on=on, sides=4, bevel=0.0)
                k.cyl("Brace", corner(z0, 0), corner(z1, 1), 0.018, look=tk.STEEL, bone="Root", on=on, sides=4, bevel=0.0)
                k.cyl("Brace", corner(z0, 1), corner(z1, 0), 0.018, look=tk.STEEL, bone="Root", on=on, sides=4, bevel=0.0)
        hz = 2.95
        h = self.half(hz) + 0.12
        for i in range(4):
            a = i * 90
            k.block("Walk", (2 * h, 0.2, 0.04), tk.frame(polar(a - 90, h - 0.1, hz), rot=(0, 0, a)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.0)
            k.block("WalkRail", (2 * h, 0.025, 0.025), tk.frame(polar(a - 90, h, hz + 0.22), rot=(0, 0, a)), tk.YELLOW, "Root", on, bevel=0.0)
        k.block("CrownBand", (0.64, 0.64, 0.07), tk.frame((0, 0, self.TOP + 0.02)), tk.HAZARD, "Root", on, bevel=0.006)
        k.block("Crown", (0.6, 0.6, 0.22), tk.frame((0, 0, self.TOP + 0.16)), tk.WHITE.but(panel=0.12), "Root", on, bevel=0.015)
        for x in (-0.12, 0.12):
            k.lathe("Sheave", [(0, -0.04), (0.14, -0.04), (0.14, 0.04), (0, 0.04)], tk.frame((x, 0, self.TOP + 0.3), z=XAX), tk.GUNMETAL,
                    "Root", on, sides=16)
        k.cyl("BeaconPost", (0.22, -0.22, self.TOP + 0.27), (0.22, -0.22, self.TOP + 0.45), 0.02, look=tk.STEEL, bone="Root", on=on, sides=6)
        k.lathe("Beacon", [(0, -0.05), (0.06, -0.03), (0.06, 0.04), (0, 0.07)], tk.frame((0.22, -0.22, self.TOP + 0.5)), sk.glow("base", 2.4),
                "Root", on, sides=10, bevel=0.0)
        self.sockets["Beacon"] = V(0.22, -0.22, self.TOP + 0.5)
        # Cables from the crown to the travelling block (bone Cable stretches with the Sink).
        cable_top = self.TOP - 0.02
        block_top = self.BLOCK_Z + 0.18
        for x in (-0.07, 0.07):
            k.cyl("Cable", (x, 0, cable_top), (x, 0, block_top), 0.009, look=tk.DARK, bone="Cable", on=on, sides=4, bevel=0.0)
        # Travelling block, hook and swivel (bone Block).
        k.block("Block", (0.26, 0.18, 0.36), tk.frame((0, 0, self.BLOCK_Z)), tk.HAZARD, "Block", on, bevel=0.02)
        k.block("BlockCheek", (0.28, 0.12, 0.26), tk.frame((0, 0, self.BLOCK_Z)), tk.GUNMETAL, "Block", on, bevel=0.012)
        k.cyl("Swivel", (0, 0, self.BLOCK_Z - 0.18), (0, 0, self.STRING_TOP), 0.07, 0.06, look=tk.GUNMETAL.but(rings=0.04), bone="Block", on=on,
              sides=12)
        k.torus("SwivelGlow", V(0, 0, self.BLOCK_Z - 0.2), UP, 0.07, 0.012, sk.glow("cyan", 1.6), "Block", on, seg=12, sides=4)
        # Drill string (bone String): the square kelly, a lit collar and a fluted bit.
        k.block("Kelly", (0.11, 0.11, self.STRING_TOP - self.BIT_TOP), tk.frame((0, 0, (self.STRING_TOP + self.BIT_TOP) / 2)),
                tk.GUNMETAL.but(rings=0.12), "String", on, bevel=0.01)
        k.lathe("Collar", [(0, self.BIT_TOP - 0.06), (0.12, self.BIT_TOP - 0.06), (0.13, self.BIT_TOP), (0.09, self.BIT_TOP + 0.06),
                           (0, self.BIT_TOP + 0.06)], tk.frame(), tk.STEEL, "String", on, sides=12, bevel=0.0)
        k.torus("CollarGlow", V(0, 0, self.BIT_TOP - 0.02), UP, 0.13, 0.018, sk.glow("cyan", 2.0), "String", on, seg=16, sides=4)
        bit_len = self.BIT_TOP - 0.06 - self.BIT_TIP
        z0 = self.BIT_TIP
        prof = [(0, z0), (0.05, z0 + 0.12), (0.15, z0 + 0.4), (0.22, z0 + bit_len - 0.15), (0.2, z0 + bit_len), (0, z0 + bit_len)]
        k.lathe("Bit", prof, tk.frame(), tk.STEEL.but(rings=0.05), "String", on, sides=12, bevel=0.0)
        for h_ in range(2):
            for j in range(14):
                t = j / 13
                z = z0 + 0.08 + t * (bit_len - 0.15)
                r = 0.05 + (0.22 - 0.05) * min(1.0, (z - z0) / (bit_len - 0.15)) + 0.025
                a = h_ * 180 + t * 540
                p = polar(a, r, z)
                k.block("Flute", (0.05, 0.11, 0.035), tk.frame(p, z=polar(a + 90, 1, 0) + V(0, 0, 0.45), y=polar(a, 1, 0)),
                        tk.YELLOW.but(fam="metal", wear=1.0) if j % 4 == 0 else tk.GUNMETAL, "String", on, bevel=0.0)
        self.bone_list += [tk.Bone("Block", (0, 0, self.BLOCK_Z), UP, "Root", length=0.2),
                           tk.Bone("String", (0, 0, self.BIT_TIP), UP, "Block", length=0.4),
                           tk.Bone("BitTip", (0, 0, self.BIT_TIP), V(0, 0, -1), "String", deform=False, length=0.1),
                           tk.Bone("Cable", (0, 0, cable_top), V(0, 0, -1), "Root", length=0.3)]
        self.cable_len = cable_top - block_top
        self.sockets["Dust"] = (V(0, 0, 0.0), UP)

    def clips(self, arm):
        def spin(t, f):
            return {"String": {"rot": (UP, 2 * math.pi * t)}}

        def sink(t, f):
            drop = self.TRAVEL * ease(seg(t, 0.05, 1.0))
            stretch = (self.cable_len + drop) / self.cable_len
            return {"Block": {"loc": V(0, 0, -drop)}, "String": {"rot": (UP, 2 * math.pi * 10 * t)},
                    "Cable": {"scale": (1.0, stretch, 1.0)}}
        return [bk.clip(arm, "Spin", 30, spin, loop=True), bk.clip(arm, "Sink", 150, sink)]


# -------------------------------------------------------------------------------------------------------------
# Mothership
# -------------------------------------------------------------------------------------------------------------

class Mothership(Spec):
    name = "Cutscene_Mothership"
    RING_Y = -2.0
    RING_R = 2.7

    def pieces(self, k):
        on = ["Main"]
        big_w = tk.WHITE.but(panel=0.32)
        big_b = tk.BLUE.but(panel=0.32)
        # Spine: a square truss round a core pipe.
        y0, y1 = -4.4, 4.9
        k.cyl("Core", (0, y0, 0), (0, y1, 0), 0.17, look=tk.GUNMETAL.but(rings=0.5), bone="Root", on=on, sides=10)
        corners = [(-0.34, -0.34), (0.34, -0.34), (0.34, 0.34), (-0.34, 0.34)]
        for x, z in corners:
            k.cyl("Longeron", (x, y0, z), (x, y1, z), 0.05, look=tk.STEEL, bone="Root", on=on, sides=6, bevel=0.0)
        n = 12
        for i in range(n):
            ya, yb = y0 + (y1 - y0) * i / n, y0 + (y1 - y0) * (i + 1) / n
            for c in range(4):
                (xa, za), (xb, zb) = corners[c], corners[(c + 1) % 4]
                if i % 2:
                    k.cyl("Truss", (xa, ya, za), (xb, yb, zb), 0.022, look=tk.STEEL, bone="Root", on=on, sides=4, bevel=0.0)
                else:
                    k.cyl("Truss", (xb, ya, zb), (xa, yb, za), 0.022, look=tk.STEEL, bone="Root", on=on, sides=4, bevel=0.0)
        # Command module in front (nose towards -Y) with the lit bridge band.
        nose = tk.frame((0, y0 + 0.1, 0), z=FWD, y=UP)
        k.lathe("Command", [(0, 0), (0.78, 0), (0.88, 0.35), (0.86, 1.2), (0.62, 1.85), (0.3, 2.2), (0, 2.28)], nose, big_w, "Root", on, sides=20,
                bevel=0.0)
        k.lathe("CommandBand", [(0, 0.25), (0.85, 0.25), (0.89, 0.32), (0.89, 0.62), (0.86, 0.68), (0, 0.68)], nose, big_b, "Root", on, sides=20,
                bevel=0.0)
        k.lathe("Bridge", [(0, 1.46), (0.79, 1.46), (0.765, 1.56), (0, 1.56)], nose, sk.glow("ora_hot", 0.9), "Root", on, sides=20, bevel=0.0)
        k.lathe("BridgeFrame", [(0, 1.4), (0.815, 1.4), (0.81, 1.44), (0, 1.44)], nose, tk.GUNMETAL, "Root", on, sides=20, bevel=0.0)
        k.lathe("Dish", [(0, 0), (0.08, 0.02), (0.3, 0.12), (0.31, 0.15), (0.28, 0.15), (0.06, 0.06), (0, 0.05)],
                tk.frame((0, y0 - 1.0, -0.95), z=V(0, -0.6, -1)), tk.WHITE.but(panel=0), "Root", on, sides=16, bevel=0.0)
        k.cyl("DishMast", (0, y0 - 0.9, -0.62), (0, y0 - 1.0, -0.95), 0.04, look=tk.STEEL, bone="Root", on=on, sides=6)
        for s in (-1, 1):
            sk.emblem(k, tk.frame((s * 0.875, y0 - 0.85, 0.0), z=XAX * s, y=UP), "Root", on, scale=0.8, letters=True, prefix="Mark")
        # Habitat ring on four spokes round a hub (bone Ring turns about the spine).
        ry, R = self.RING_Y, self.RING_R
        k.lathe("Hub", [(0, -0.4), (0.5, -0.4), (0.62, -0.2), (0.62, 0.2), (0.5, 0.4), (0, 0.4)], tk.frame((0, ry, 0), z=BACK), tk.GUNMETAL,
                "Ring", on, sides=16, bevel=0.0)
        k.torus("HubGlow", V(0, ry, 0), BACK, 0.63, 0.03, sk.glow("base", 1.6), "Ring", on, seg=24, sides=4)
        k.torus("Habitat", V(0, ry, 0), BACK, R, 0.32, big_w, "Ring", on, seg=56, sides=10)
        for s in (-1, 1):
            k.torus("HabitatRim", V(0, ry + s * 0.28, 0), BACK, R, 0.07, big_b, "Ring", on, seg=56, sides=6)
        for i in range(28):
            a = 360 * i / 28
            d = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
            p = V(0, ry, 0) + d * (R + 0.315)
            k.block("RingWindow", (0.1, 0.18, 0.012), tk.frame(p - d * 0.008, z=d, y=BACK), sk.glow("ora_hot", 1.1), "Ring", on, bevel=0.0)
        for i in range(4):
            a = 45 + 90 * i
            d = V(math.cos(math.radians(a)), 0, math.sin(math.radians(a)))
            k.cyl("Spoke", V(0, ry, 0) + d * 0.55, V(0, ry, 0) + d * (R - 0.25), 0.11, 0.09, look=tk.STEEL.but(rings=0.3), bone="Ring", on=on,
                  sides=8)
            k.torus("SpokeBand", V(0, ry, 0) + d * 1.4, d, 0.115, 0.02, tk.YELLOW, "Ring", on, seg=10, sides=4)
        # Cargo pods along the spine (top, bottom, left, right).
        for j, y in enumerate((-0.6, 0.35, 1.3, 2.25)):
            for c, (x, z) in enumerate(((0, 0.66), (0.66, 0), (0, -0.66), (-0.66, 0))):
                look = big_w if (j + c) % 2 == 0 else big_b
                if z < 0 and 0.0 < y < 2.0:
                    continue   # the capsule bay hangs there
                size = (0.52, 0.82, 0.52)
                k.block("Pod", size, tk.frame((x, y, z)), look, "Root", on, bevel=0.03)
                k.block("PodBand", (0.54, 0.08, 0.54), tk.frame((x, y - 0.33, z)), tk.HAZARD, "Root", on, bevel=0.01)
        # Capsule bay underneath: a housing with a lit open mouth and three capsules racked inside.
        k.block("Bay", (1.1, 2.0, 0.42), tk.frame((0, 1.0, -0.72)), tk.GUNMETAL.but(panel=0.3), "Root", on, bevel=0.03)
        k.block("BayMouth", (0.86, 1.7, 0.02), tk.frame((0, 1.0, -0.935)), tk.DARK.but(panel=0), "Root", on, bevel=0.0)
        for x in (-0.44, 0.44):
            k.block("BayLight", (0.03, 1.7, 0.02), tk.frame((x, 1.0, -0.94)), sk.glow("cyan", 1.8), "Root", on, bevel=0.0)
        for i, y in enumerate((0.45, 1.0, 1.55)):
            k.lathe("Racked", [(0, -0.95), (0.2, -0.95), (0.16, -0.8), (0, -0.78)], tk.frame((0, y, 0)), tk.WHITE.but(panel=0), "Root", on,
                    sides=6, start=0.0, bevel=0.0)
        self.sockets["Bay"] = (V(0, 1.0, -0.96), V(0, 0, -1))
        # Radiators: two broad dark fins per side with cyan coolant lines.
        for s in (-1, 1):
            k.cyl("RadBoom", (s * 0.35, 3.6, 0), (s * 0.9, 3.6, 0), 0.06, look=tk.GUNMETAL, bone="Root", on=on, sides=8)
            for j, dz in enumerate((0.0,)):
                c = V(s * 2.0, 3.6, dz)
                k.block("Radiator", (2.2, 1.7, 0.05), tk.frame(c), tk.DARK.but(panel=0.22), "Root", on, bevel=0.01)
                k.block("RadFrame", (2.24, 0.05, 0.07), tk.frame(c + V(0, -0.86, 0)), tk.WHITE.but(panel=0), "Root", on, bevel=0.0)
                k.block("RadFrame", (2.24, 0.05, 0.07), tk.frame(c + V(0, 0.86, 0)), tk.WHITE.but(panel=0), "Root", on, bevel=0.0)
                for q in range(5):
                    k.block("Coolant", (2.0, 0.02, 0.06), tk.frame(c + V(0, -0.6 + q * 0.3, 0)), sk.glow("cyan", 1.2), "Root", on, bevel=0.0)
        # Engine block with three nozzles (sockets Nozzle_n at the exits, +Y).
        eng = tk.frame((0, y1 - 0.2, 0), z=BACK, y=UP)
        k.lathe("Engine", [(0, 0), (0.62, 0), (0.98, 0.35), (1.04, 1.15), (0.92, 1.45), (0, 1.45)], eng, tk.GUNMETAL.but(panel=0.3), "Root", on,
                sides=16, bevel=0.0)
        k.lathe("EngineShroud", [(0, 0.3), (1.0, 0.3), (1.07, 0.42), (1.07, 0.85), (1.02, 0.95), (0, 0.95)], eng, big_w, "Root", on, sides=16,
                bevel=0.0)
        k.torus("EngineHazard", V(0, y1 + 0.85, 0), BACK, 1.07, 0.04, tk.HAZARD, "Root", on, seg=32, sides=4)
        for i, a in enumerate((90, 210, 330)):
            c = V(math.cos(math.radians(a)) * 0.5, y1 + 1.2, math.sin(math.radians(a)) * 0.5)
            nf = tk.frame(c, z=BACK, y=UP)
            k.lathe("Nozzle", [(0, 0), (0.2, 0), (0.24, 0.12), (0.42, 0.85), (0.39, 0.88), (0.2, 0.22), (0, 0.22)], nf, tk.STEEL.but(rings=0.1),
                    "Root", on, sides=16, bevel=0.0)
            k.lathe("NozzleGlow", [(0, 0.21), (0.19, 0.22), (0.3, 0.6), (0, 0.6)], nf, sk.glow("engine", 2.6), "Root", on, sides=12, bevel=0.0)
            self.sockets[f"Nozzle_{i + 1}"] = (c + BACK * 0.9, BACK)
        # Running lights.
        for i in range(7):
            y = y0 + 0.4 + i * 1.35
            for z in (0.4, -0.4):
                k.cyl("NavPip", (0, y, z), (0, y, z + (0.05 if z > 0 else -0.05)), 0.035, look=sk.glow("base", 2.2), bone="Root", on=on, sides=6,
                      bevel=0.0)
        self.bone_list.append(tk.Bone("Ring", (0, ry, 0), BACK, "Root", length=0.6))

    def clips(self, arm):
        def spin(t, f):
            return {"Ring": {"rot": (BACK, 2 * math.pi * t)}}
        return [bk.clip(arm, "Spin", 360, spin, loop=True)]


# -------------------------------------------------------------------------------------------------------------
# Beacon
# -------------------------------------------------------------------------------------------------------------

class Beacon(Spec):
    name = "Cutscene_Beacon"
    tex, low = 512, 256
    LEG_ANGLES = (90, 210, 330)
    HUB_Z = 0.42
    TELESCOPE = 0.38

    def pieces(self, k):
        on = ["Main"]
        hz = self.HUB_Z
        k.block("Hub", (0.2, 0.2, 0.14), tk.frame((0, 0, hz)), tk.BLUE.but(panel=0), "Root", on, bevel=0.015)
        k.block("HubBand", (0.205, 0.205, 0.035), tk.frame((0, 0, hz - 0.04)), tk.HAZARD, "Root", on, bevel=0.004)
        k.block("HubWindow", (0.1, 0.012, 0.04), tk.frame((0, -0.104, hz + 0.02)), sk.glow("base", 1.8), "Root", on, bevel=0.0)
        for i, a in enumerate(self.LEG_ANGLES):
            hip = polar(a, 0.09, hz - 0.03)
            foot = polar(a, 0.44, 0.03)
            name = f"Leg_{i + 1}"
            k.lathe("Knuckle", [(0, -0.025), (0.026, -0.025), (0.026, 0.025), (0, 0.025)], tk.frame(hip, z=polar(a + 90, 1, 0)), tk.GUNMETAL, name,
                    on, sides=8)
            k.cyl("Leg", hip, foot + V(0, 0, 0.02), 0.022, 0.016, look=tk.GUNMETAL, bone=name, on=on, sides=8)
            k.torus("LegBand", hip + (foot - hip) * 0.55, foot - hip, 0.021, 0.006, tk.YELLOW, name, on, seg=8, sides=3)
            k.lathe("Foot", [(0, 0), (0.05, 0), (0.05, 0.015), (0.025, 0.03), (0, 0.03)], tk.frame(foot - V(0, 0, 0.03)), tk.DARK, name, on,
                    sides=10, bevel=0.0)
            self.bone_list.append(tk.Bone(name, hip, foot - hip, "Root", length=0.1))
        top = hz + 1.1
        k.cyl("MastLow", (0, 0, hz + 0.06), (0, 0, hz + 0.6), 0.036, look=tk.STEEL.but(rings=0.1), bone="Mast", on=on, sides=12)
        k.torus("MastCollar", V(0, 0, hz + 0.6), UP, 0.038, 0.01, tk.YELLOW, "Mast", on, seg=12, sides=4)
        k.block("Transmitter", (0.09, 0.07, 0.17), tk.frame((0, 0.03, hz + 0.33)), tk.WHITE.but(panel=0), "Mast", on, bevel=0.01)
        k.block("TransmitterGlow", (0.05, 0.012, 0.012), tk.frame((0, -0.006, hz + 0.37)), sk.glow("base", 1.8), "Mast", on, bevel=0.0)
        k.cyl("Whip", (0.035, 0.05, hz + 0.41), (0.04, 0.06, hz + 0.62), 0.005, look=tk.DARK, bone="Mast", on=on, sides=4, bevel=0.0)
        k.cyl("MastHigh", (0, 0, hz + 0.3), (0, 0, top - 0.04), 0.026, look=tk.WHITE.but(panel=0, rings=0.12), bone="Mast2", on=on, sides=12)
        k.lathe("LampBase", [(0, top - 0.06), (0.05, top - 0.06), (0.06, top - 0.02), (0, top - 0.02)], tk.frame(), tk.GUNMETAL, "Mast2", on,
                sides=12, bevel=0.0)
        k.lathe("Lamp", [(0, top - 0.02), (0.065, top), (0.07, top + 0.08), (0.04, top + 0.13), (0, top + 0.14)], tk.frame(),
                sk.glow("base", 1.7, lens=0.05), "Mast2", on, sides=14, bevel=0.0)
        for i in range(3):
            a = 30 + i * 120
            k.cyl("Cage", polar(a, 0.075, top - 0.02), polar(a, 0.06, top + 0.12), 0.006, look=tk.DARK, bone="Mast2", on=on, sides=4, bevel=0.0)
        k.lathe("LampCap", [(0, top + 0.13), (0.045, top + 0.13), (0.04, top + 0.16), (0, top + 0.165)], tk.frame(), tk.GUNMETAL, "Mast2", on,
                sides=12, bevel=0.0)
        rz = top - 0.14
        k.torus("Ring", V(0, 0, rz), UP, 0.2, 0.016, tk.GUNMETAL, "Ring", on, seg=28, sides=6)
        k.torus("RingGlow", V(0, 0, rz + 0.012), UP, 0.2, 0.008, sk.glow("base", 2.0), "Ring", on, seg=28, sides=4)
        for i in range(3):
            a = i * 120
            k.cyl("RingSpoke", V(0, 0, rz), polar(a, 0.19, rz), 0.006, look=tk.STEEL, bone="Ring", on=on, sides=4, bevel=0.0)
        k.block("RingNub", (0.05, 0.05, 0.05), tk.frame(polar(0, 0.2, rz)), tk.YELLOW, "Ring", on, bevel=0.006)
        self.bone_list += [tk.Bone("Mast", (0, 0, hz + 0.06), UP, "Root", length=0.2),
                           tk.Bone("Mast2", (0, 0, hz + 0.6), UP, "Mast", length=0.2),
                           tk.Bone("Ring", (0, 0, rz), UP, "Mast2", length=0.1),
                           tk.Bone("Lamp", (0, 0, top + 0.06), UP, "Mast2", deform=False, length=0.06)]
        self.sockets["Ground"] = V(0, 0, 0)

    def clips(self, arm):
        def legs(fold):
            spec = {}
            for i, a in enumerate(self.LEG_ANGLES):
                hip = polar(a, 0.09, self.HUB_Z - 0.03)
                foot = polar(a, 0.44, 0.03)
                axis = (foot - hip).normalized().cross(UP).normalized()
                spec[f"Leg_{i + 1}"] = {"rot": (axis, math.radians(150) * fold)}
            return spec

        def deploy(t, f):
            unfold = ease(seg(t, 0.05, 0.45))
            spec = legs(1 - unfold)
            # Folded, the beacon sits on the ground on its hub; the legs push it up as they unfold.
            spec["Root"] = {"loc": V(0, 0, -(self.HUB_Z - 0.1) * (1 - unfold))}
            spec["Mast2"] = {"loc": V(0, 0, -self.TELESCOPE * (1 - ease(seg(t, 0.4, 0.82))))}
            ring = bk.pop(t, 0.78, 0.18, 0.2)
            spec["Ring"] = {"scale": max(0.001, ring), "rot": (UP, 2 * math.pi * 0.25 * seg(t, 0.78, 1.0))}
            spec["Mast"] = {"loc": V(0, 0, -0.04 * (1 - ease(seg(t, 0.3, 0.5))))}
            return spec

        def idle(t, f):
            return {"Ring": {"rot": (UP, 2 * math.pi * t)}}
        return [bk.clip(arm, "Deploy", 54, deploy), bk.clip(arm, "Idle", 60, idle, loop=True)]


SPECS = {"Capsule": Capsule, "Drill": Drill, "Mothership": Mothership, "Beacon": Beacon}


# -------------------------------------------------------------------------------------------------------------
# Build and export
# -------------------------------------------------------------------------------------------------------------

def rig_objects(spec, kit, mat, label=""):
    objs = bk.skinned(kit, "Main", spec.bones(), mat, kit.name + label, lod=False, arm_name=f"{kit.name}_Rig{label}")
    for variant, suffix in spec.extras:
        objs.append(bk.extra_mesh(kit, variant, objs[0], mat, kit.name + suffix + label))
    return objs


def build(spec, bake=True):
    bk.clear_actions()
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, spec.tex, spec.low, bake, samples=16)
    objs = rig_objects(spec, kit, mat)
    arm = objs[0]
    acts = spec.clips(arm)
    bk.rest_pose(arm)
    socks = sk.socket_empties(spec.sockets, parent=arm)
    bk.export_fbx(os.path.join(out_dir, kit.name + ".fbx"), objs + socks, animated=bool(acts))
    report.update({
        "fbx": kit.name + ".fbx", "meshes": {o.name: C.tri_count(o) for o in objs[1:]},
        "tris": sum(C.tri_count(o) for o in objs[1:]), "lod1": "none: cutscene only",
        "bones": [b.name for b in arm.data.bones], "deform_bones": [b.name for b in arm.data.bones if b.use_deform],
        "clips": {a.name: int(a.frame_range[1]) for a in acts},
        "sockets": {n: [round(c, 3) for c in (s[0] if isinstance(s, tuple) else s)] for n, s in spec.sockets.items()},
        "pivot": "centre on the ground (mothership: its centre); faces -Y in Blender (Unity +Z)"})
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    C.log(kit.name, report["tris"], report["meshes"])
    bk.remove(objs + socks)
    for a in acts:
        bpy.data.actions.remove(a)
    return kit, mat, report


# -------------------------------------------------------------------------------------------------------------
# Previews
# -------------------------------------------------------------------------------------------------------------

def stage_ground():
    g = C.ground(color=(0.05, 0.05, 0.08))
    return [g]


def render_capsule(spec, kit, mat, out):
    objs = rig_objects(spec, kit, mat, "P")
    arm = objs[0]
    acts = {a.name: a for a in spec.clips(arm)}
    stage = stage_ground()
    rig = tk.close_lights(1.2)
    bk.rest_pose(arm)
    # Cutscene framing: low, from the front-left, the open ramp towards the camera.
    arm.animation_data.action = acts["Open"]
    bpy.context.scene.frame_set(48)
    cam = C.closeup_camera(V(0, 0, 0.8), 4.4, azimuth=-30, elevation=10, lens=40)
    C.render(cam, os.path.join(out, "capsule_cutscene_open.png"), 1280, 720)
    bk.remove([cam])
    bpy.context.scene.frame_set(0)
    cam = C.closeup_camera(V(0, 0, 0.9), 3.6, azimuth=-32, elevation=18, lens=50)
    C.render(cam, os.path.join(out, "capsule_close.png"), 960, 960)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 0.9), 3.6, azimuth=150, elevation=22, lens=50)
    C.render(cam, os.path.join(out, "capsule_back.png"), 960, 960)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, -0.5, 0.85), 3.4, azimuth=-70, elevation=12, lens=50)
    sk.strip(arm, acts["Open"], cam, (0, 12, 22, 32, 48), os.path.join(out, "capsule_open_strip.png"))
    sk.strip(arm, acts["Land"], cam, (0, 8, 16, 24, 30), os.path.join(out, "capsule_land_strip.png"))
    bk.remove([cam])
    arm.animation_data.action = None
    bk.rest_pose(arm)
    cam = C.macro_camera(V(0, -0.5, 0.9), 1.3, -20, 10, lens=60)
    C.render(cam, os.path.join(out, "capsule_macro.png"), 1280, 720)
    bk.remove([cam] + rig + objs + stage)


def render_drill(spec, kit, mat, out):
    objs = rig_objects(spec, kit, mat, "P")
    arm = objs[0]
    acts = {a.name: a for a in spec.clips(arm)}
    stage = stage_ground()
    rig = tk.close_lights(2.6)
    bk.rest_pose(arm)
    cam = C.closeup_camera(V(0, 0, 2.5), 11.0, azimuth=-35, elevation=8, lens=40)
    C.render(cam, os.path.join(out, "drill_cutscene.png"), 1280, 720)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 1.8), 6.0, azimuth=-30, elevation=28, lens=40)
    C.render(cam, os.path.join(out, "drill_close.png"), 960, 1100)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 2.2), 9.0, azimuth=-20, elevation=6, lens=40)
    sk.strip(arm, acts["Sink"], cam, (0, 40, 80, 120, 150), os.path.join(out, "drill_sink_strip.png"), width=400, height=520)
    bk.remove([cam])
    arm.animation_data.action = None
    bk.rest_pose(arm)
    cam = C.macro_camera(V(0, 0, 1.0), 1.9, -25, 12, lens=60)
    C.render(cam, os.path.join(out, "drill_macro_bit.png"), 1280, 720)
    bk.remove([cam])
    cam = C.macro_camera(V(0.5, 0.4, 0.7), 2.2, -40, 20, lens=60)
    C.render(cam, os.path.join(out, "drill_macro_house.png"), 1280, 720)
    bk.remove([cam] + rig + objs + stage)


def render_mothership(spec, kit, mat, out):
    objs = rig_objects(spec, kit, mat, "P")
    arm = objs[0]
    acts = {a.name: a for a in spec.clips(arm)}
    sk.space_world(1.0)
    pl = sk.planet(V(75, 50, -62), 52)
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy = 4.0
    sun.color = (1.0, 0.96, 0.9)
    so = C.link(bpy.data.objects.new("Sun", sun))
    C.look_at(so, so.location + V(0.55, 0.55, -0.45))
    fill = bpy.data.lights.new("Fill", "SUN")
    fill.energy = 0.5
    fill.color = (0.45, 0.4, 0.9)
    fo = C.link(bpy.data.objects.new("Fill", fill))
    C.look_at(fo, fo.location + V(0.3, -0.2, 0.9))
    bk.rest_pose(arm)
    arm.animation_data.action = acts["Spin"]
    bpy.context.scene.frame_set(20)
    cam = C.closeup_camera(V(0, 0.5, 0), 22.0, azimuth=-58, elevation=14, lens=50)
    cam.data.clip_end = 500
    C.render(cam, os.path.join(out, "mothership_cutscene.png"), 1280, 720)
    C.render(cam, os.path.join(out, "mothership_far.png"), 320, 180)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, -1.0, 0), 13.0, azimuth=-30, elevation=-20, lens=40)
    cam.data.clip_end = 500
    C.render(cam, os.path.join(out, "mothership_close.png"), 1280, 720)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 3.0, 0), 14.0, azimuth=155, elevation=12, lens=40)
    cam.data.clip_end = 500
    C.render(cam, os.path.join(out, "mothership_engines.png"), 1280, 720)
    bk.remove([cam])
    cam = C.macro_camera(V(0.6, -4.8, 0.3), 3.4, -60, 10, lens=50)
    cam.data.clip_end = 500
    C.render(cam, os.path.join(out, "mothership_macro_nose.png"), 1280, 720)
    bk.remove([cam])
    bk.remove([so, fo, pl] + objs)
    C.preview_world()


def render_beacon(spec, kit, mat, out):
    objs = rig_objects(spec, kit, mat, "P")
    arm = objs[0]
    acts = {a.name: a for a in spec.clips(arm)}
    stage = stage_ground()
    rig = tk.close_lights(0.6)
    bk.rest_pose(arm)
    cam = C.closeup_camera(V(0, 0, 0.8), 2.6, azimuth=-30, elevation=14, lens=50)
    C.render(cam, os.path.join(out, "beacon_close.png"), 960, 1100)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 0.75), 3.0, azimuth=-25, elevation=10, lens=50)
    sk.strip(arm, acts["Deploy"], cam, (0, 12, 24, 36, 46, 54), os.path.join(out, "beacon_deploy_strip.png"), width=360, height=480)
    bk.remove([cam])
    arm.animation_data.action = None
    bk.rest_pose(arm)
    cam = C.macro_camera(V(0, 0, 1.45), 0.8, -30, 10, lens=60)
    C.render(cam, os.path.join(out, "beacon_macro_lamp.png"), 1280, 720)
    bk.remove([cam] + rig + objs + stage)


RENDER = {"Capsule": render_capsule, "Drill": render_drill, "Mothership": render_mothership, "Beacon": render_beacon}


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else list(SPECS)
    C.preview_world()
    tk.metal_detail_map()
    for n in names:
        spec = SPECS[n]()
        kit, mat, _ = build(spec, bake=not args.get("nobake"))
        if not args.get("norender"):
            spec2 = SPECS[n]()
            spec2.kit()   # fills bones and sockets again for the preview rig
            RENDER[n](spec2, kit, mat, out)
            bk.clear_actions()
    C.save_blend("Cutscene" if not isinstance(only, str) else "Cutscene_" + "_".join(names))


if __name__ == "__main__":
    main()
