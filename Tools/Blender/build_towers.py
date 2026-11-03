"""Round 7: the P1 towers (see ArtSource/DESIGN_NOTES.md, round 7). Gun Turret, Cannon, Laser, Flak and Gate Gun,
each with its tiers (T1, T2, T3A, T3B; the Gate Gun T1 to T3), the shared build pad with the rank plate and the
stars, and the wreck with per-tower debris. Hard-surface pieces, atlas and self-bake come from tower_kit.py.

Per tower: `Exports/Tower_<Name>/Tower_<Name>_<Tier>.fbx` (LOD0 and LOD1 skinned to one `Tower_<Name>_Rig`, with
the clips), one 1024 map set shared by all tiers (+ `Low/` at 512), and `Tower_<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_towers.py -- [--only Gun,Laser]
     [--preview <dir>] [--norender] [--nobake]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402

YAW_Z = 0.235
FWD = Vector((0, -1, 0))
UP = Vector((0, 0, 1))
XAX = Vector((1, 0, 0))
ALL = None


def V(x, y, z):
    return Vector((x, y, z))


def kick(f, n=10):
    """Recoil curve over a Fire clip: snaps back on frame 1, returns with an ease by frame n."""
    if f <= 0:
        return 0.0
    if f == 1:
        return 1.0
    return max(0.0, 1.0 - (f - 1) / (n - 1)) ** 2


def ease(t):
    return t * t * (3 - 2 * t)


# -------------------------------------------------------------------------------------------------------------
# Shared pieces: the pad with the rank plate and stars
# -------------------------------------------------------------------------------------------------------------

def burn(look):
    """A burnt-out version of a look: soot-black paint with a hint of the colour, no glow."""
    paint = tuple(np.asarray(look.paint) * 0.4 + 0.025)
    return look.but(paint=paint, emit=None, glow=0.0, fam="metal" if look.fam == "glow" else look.fam,
                    rough=0.75, wear=1.0, lens=0.0)


def face_frame(angle_deg, z, r_bottom=0.326, r_top=0.306, z0=0.064, z1=0.188, out=0.0):
    """Frame on one side face of the hex body: local z = the outward face normal, local y = up the face."""
    a = math.radians(angle_deg)
    c30 = math.cos(math.radians(30))
    ap0, ap1 = r_bottom * c30, r_top * c30
    t = (z - z0) / (z1 - z0)
    ap = ap0 + (ap1 - ap0) * t
    slope = math.atan2(ap0 - ap1, z1 - z0)
    n = V(math.cos(a) * math.cos(slope), math.sin(a) * math.cos(slope), math.sin(slope))
    o = V(math.cos(a) * ap, math.sin(a) * ap, z) + n * out
    return tk.frame(o, z=n, y=UP)


RANK = {"centre": V(0, 0.286, 0.128), "tilt": 44.0, "spacing": 0.09, "star": (0.056, 0.024)}


def rank_plate(k, centre, tilt, spacing, star, bone="Root", burnt=False, width=0.3, height=0.1):
    """The sloped plate facing the camera with three star recesses; stars on bones Star_1..3 (not on a wreck)."""
    L = burn if burnt else (lambda x: x)
    m = tk.frame(centre, rot=(tilt, 0, 0))
    k.block("RankPlate", (width, 0.016, height), m, L(tk.DARK), bone, bevel=0.004)
    normal = (m.to_3x3() @ V(0, 1, 0)).normalized()
    up = (m.to_3x3() @ V(0, 0, 1)).normalized()
    stars = []
    for i in range(3):
        x = (i - 1) * spacing
        c = centre + V(x, 0, 0) + normal * 0.009
        k.cyl("StarRecess", c - normal * 0.004, c + normal * 0.002, star[0] * 0.92, look=L(tk.RUBBER), bone=bone,
              sides=10, bevel=0.0, lod=False)
        if not burnt:
            sc = c + normal * 0.006
            k.plate("Star", tk.star_poly(*star), 0.01, tk.frame(sc, z=normal, y=up), tk.glow("star", 1.3), f"Star_{i + 1}",
                    bevel=0.002)
            stars.append((f"Star_{i + 1}", sc, normal))
    return stars


def pad(k, accent, burnt=False, on=ALL):
    """The build pad every tower stands on: dark foot with hazard chevrons, blue body with vents, white deck with
    a glowing turret race, and the rank plate on the camera side. Returns the star sockets."""
    L = burn if burnt else (lambda x: x)
    I = tk.frame()
    k.lathe("PadFoot", [(0, 0), (0.36, 0), (0.36, 0.045), (0.338, 0.068), (0, 0.068)], I, L(tk.GUNMETAL), "Root",
            on, sides=6, start=0.0, bevel=0.006)
    k.lathe("PadHazard", [(0, 0.02), (0.367, 0.02), (0.367, 0.05), (0, 0.05)], I, L(tk.HAZARD), "Root", on, sides=6,
            start=0.0, bevel=0.002)
    k.lathe("PadBody", [(0, 0.064), (0.326, 0.064), (0.306, 0.188), (0, 0.188)], I, L(tk.BLUE), "Root", on, sides=6,
            start=0.0, bevel=0.006)
    k.lathe("PadDeck", [(0, 0.184), (0.314, 0.184), (0.314, 0.198), (0.292, 0.214), (0, 0.214)], I,
            L(tk.WHITE.but(panel=0.12)), "Root", on, sides=6, start=0.0, bevel=0.005)
    k.cyl("PadCollar", (0, 0, 0.206), (0, 0, YAW_Z), 0.186, 0.176, L(tk.GUNMETAL), "Root", on, sides=20)
    k.torus("PadRace", (0, 0, 0.214), UP, 0.206, 0.008, L(tk.glow(accent, 1.2)), "Root", on, seg=24, sides=4)
    for a in (30, 150, 210, 330):
        f = face_frame(a, 0.128, out=0.004)
        k.block("PadVent", (0.15, 0.056, 0.01), f, L(tk.DARK), "Root", on, bevel=0.003)
        for j in range(3):
            fj = face_frame(a, 0.108 + j * 0.02, out=0.009)
            k.block("PadSlat", (0.13, 0.007, 0.008), fj, L(tk.GUNMETAL), "Root", on, bevel=0.0, lod=False)
    f = face_frame(270, 0.15, out=0.004)
    k.block("PadLight", (0.12, 0.014, 0.008), f, L(tk.glow(accent, 1.4)), "Root", on, bevel=0.002)
    f = face_frame(270, 0.1, out=0.004)
    k.block("PadPlate", (0.17, 0.05, 0.008), f, L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.002)
    return rank_plate(k, RANK["centre"], RANK["tilt"], RANK["spacing"], RANK["star"], burnt=burnt)


def ring_base(k, r, on=ALL, look=None):
    k.lathe("TurretRing", [(0, YAW_Z), (r, YAW_Z), (r, YAW_Z + 0.022), (r - 0.016, YAW_Z + 0.036), (0, YAW_Z + 0.036)],
            tk.frame(), look or tk.GUNMETAL, "Yaw", on, sides=20, bevel=0.004)


# -------------------------------------------------------------------------------------------------------------
# Tower specs
# -------------------------------------------------------------------------------------------------------------

class Spec:
    """One tower: pieces for every variant, the rig per variant and the clips."""
    name = ""
    variants = ["T1", "T2", "T3A", "T3B"]
    accent = "azure"
    recoil = 0.03
    pitch_at = V(0, 0, 0.4)
    smoke_at = V(0, 0.06, 0.45)
    has_pad = True
    preview_pitch = 6.0
    # The head is built at a comfortable working size and grown about the turret ring, so it dominates the pad.
    head_scale = 1.25

    def __init__(self):
        self.stars = []
        self.barrels = {}   # variant -> [(head, direction, muzzle)]
        self.extra_bones = {}

    def kit(self):
        k = tk.Kit("Tower_" + self.name, self.variants)
        if self.has_pad:
            self.stars = pad(k, self.accent)
        self.pieces(k)
        self.grow_head(k, V(0, 0, YAW_Z))
        return k

    def grow_head(self, k, centre):
        f = self.head_scale
        if abs(f - 1.0) < 1e-6:
            return
        from mathutils import Matrix
        m = Matrix.Translation(centre) @ Matrix.Scale(f, 4) @ Matrix.Translation(-centre)

        def sc(x):
            return centre + (Vector(x) - centre) * f
        for p in k.pieces:
            if p.bone != "Root" and not p.bone.startswith("Star_"):
                p.matrix = m @ p.matrix
        self.pitch_at = sc(self.pitch_at)
        self.smoke_at = sc(self.smoke_at)
        self.recoil *= f
        self.barrels = {v: [(sc(h), d, sc(mz)) for h, d, mz in bl] for v, bl in self.barrels.items()}
        for bones in self.extra_bones.values():
            for b in bones:
                b.head = sc(b.head)

    def pieces(self, k):
        raise NotImplementedError

    def bones(self, v):
        b = [tk.Bone("Root", (0, 0, 0), UP)]
        for name, head, normal in self.stars:
            b.append(tk.Bone(name, head, normal, "Root", length=0.04))
        b.append(tk.Bone("Yaw", (0, 0, YAW_Z if self.has_pad else self.yaw_z), UP, "Root"))
        b.append(tk.Bone("Smoke", self.smoke_at, UP, "Yaw", deform=False, length=0.05))
        b.append(tk.Bone("Pitch", self.pitch_at, XAX, "Yaw", length=0.06))
        for i, (head, d, muzzle) in enumerate(self.barrels[v]):
            b.append(tk.Bone(f"Barrel_{i + 1}", head, d, "Pitch", length=0.08))
            b.append(tk.Bone(f"Muzzle_{i + 1}", muzzle, d, f"Barrel_{i + 1}", deform=False, length=0.05))
        b += self.extra_bones.get(v, [])
        return b

    def barrel(self, v, head, d, length):
        self.barrels.setdefault(v, []).append((Vector(head), Vector(d).normalized(), Vector(head) + Vector(d).normalized() * length))

    def clips(self, arm, v):
        acts = [tk.clip(arm, "IdleSweep", 120, lambda t, f: {
            "Yaw": {"rot": (UP, 0.4 * math.sin(2 * math.pi * t))},
            "Pitch": {"rot": (XAX, -0.05 * math.sin(4 * math.pi * t))}}, loop=True)]
        for i, (_, d, _) in enumerate(self.barrels[v]):
            acts.append(tk.clip(arm, f"Fire_{i + 1}", 10, self._fire(i, d)))
        return acts

    def _fire(self, i, d, start=0):
        def keys(t, f):
            k = kick(f - start)
            return {f"Barrel_{i + 1}": {"loc": d * (-self.recoil * k)}, "Pitch": {"rot": (XAX, -math.radians(1.5) * k)}}
        return keys


class Gun(Spec):
    name = "Gun"
    accent = "azure"
    recoil = 0.03
    pitch_at = V(0, -0.06, 0.36)
    smoke_at = V(0, 0.08, 0.44)

    def pieces(self, k):
        ring_base(k, 0.165)
        z = 0.36
        for v, w, d in (("T1", 0.30, 0.32), ("T2", 0.33, 0.34), ("T3A", 0.35, 0.36), ("T3B", 0.33, 0.38)):
            on = [v]
            k.block("Hull", (w, d, 0.13), tk.frame((0, 0.03, 0.335)), tk.BLUE, "Yaw", on, taper=(0.86, 0.82), shift=(0, 0.015), bevel=0.01)
            k.block("TopArmour", (w * 0.84, d * 0.8, 0.026), tk.frame((0, 0.045, 0.41)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (w * 0.88, 0.024, 0.1), tk.frame((0, -0.128, 0.33), rot=(-38, 0, 0)), tk.WHITE.but(panel=0.08), "Yaw", on)
            k.block("Eye", (0.05, 0.008, 0.014), tk.frame((w * 0.24, -0.15, 0.352), rot=(-38, 0, 0)), tk.glow(self.accent, 1.5), "Yaw", on, bevel=0.002)
        # Power cells: left on T1, both sides from T2.
        for v in self.variants:
            sides = (-1,) if v == "T1" else (-1, 1)
            hw = {"T1": 0.15, "T2": 0.165, "T3A": 0.175, "T3B": 0.165}[v]
            for s in sides:
                if v == "T3A" and s == 1:
                    continue  # the drum feed takes the right side
                x = s * (hw + 0.035)
                k.block("Cell", (0.065, 0.17, 0.09), tk.frame((x, 0.05, 0.33)), tk.BLUE.but(panel=0.06), "Yaw", [v])
                k.block("CellCap", (0.068, 0.12, 0.016), tk.frame((x, 0.05, 0.38)), tk.WHITE.but(panel=0), "Yaw", [v])
                k.block("CellLight", (0.01, 0.11, 0.014), tk.frame((x + s * 0.034, 0.05, 0.345)), tk.glow(self.accent, 1.4), "Yaw", [v], bevel=0.002)
        # T2 and up: cheek plates and a sensor block.
        for v in ("T2", "T3A", "T3B"):
            for s in (-1, 1):
                k.block("Cheek", (0.022, 0.2, 0.09), tk.frame((s * 0.17, -0.05, 0.37), rot=(0, s * 18, 0)), tk.WHITE.but(panel=0.07), "Yaw", [v])
            k.block("Sensor", (0.09, 0.07, 0.05), tk.frame((-0.06, 0.13, 0.445)), tk.GUNMETAL, "Yaw", [v])
            k.cyl("SensorLens", (-0.06, 0.094, 0.447), (-0.06, 0.088, 0.447), 0.016, look=tk.glow(self.accent, 1.6), bone="Yaw", on=[v], sides=10, bevel=0.0)
        # T3A: cooling fins on top; T3B: counterweight plates and a scope.
        for i in range(4):
            k.block("Fin", (0.012, 0.13, 0.045), tk.frame((-0.06 + i * 0.04, 0.06, 0.445)), tk.WHITE.but(panel=0), "Yaw", ["T3A"], bevel=0.003)
        for s in (-1, 1):
            k.block("Counterweight", (0.13, 0.05, 0.1), tk.frame((s * 0.08, 0.235, 0.33)), tk.GUNMETAL, "Yaw", ["T3B"])
        k.block("CounterStripe", (0.28, 0.012, 0.025), tk.frame((0, 0.262, 0.355)), tk.HAZARD, "Yaw", ["T3B"], bevel=0.002)
        k.cyl("Scope", (-0.11, 0.1, 0.46), (-0.11, -0.12, 0.46), 0.024, look=tk.GUNMETAL.but(rings=0.05), bone="Yaw", on=["T3B"], sides=12)
        k.block("ScopeMount", (0.03, 0.06, 0.04), tk.frame((-0.11, 0.0, 0.43)), tk.DARK, "Yaw", ["T3B"])
        k.cyl("ScopeLens", (-0.11, -0.12, 0.46), (-0.11, -0.126, 0.46), 0.019, look=tk.glow(self.accent, 1.8, lens=0.019), bone="Yaw", on=["T3B"], sides=12, bevel=0.0)

        # Mantlet on the trunnion.
        for v, half in (("T1", 0.1), ("T2", 0.13), ("T3A", 0.11), ("T3B", 0.1)):
            k.cyl("Mantlet", (-half, -0.06, z), (half, -0.06, z), 0.056, look=tk.GUNMETAL, bone="Pitch", on=[v], sides=16)
            k.block("MantletPlate", (half * 1.6, 0.04, 0.1), tk.frame((0, -0.105, z)), tk.BLUE.but(panel=0), "Pitch", [v])

        def rifle(v, x, length, bone, jacket=True):
            b0 = V(x, -0.12, z)
            k.barrel("Tube", b0, V(x, -0.12 - length, z), 0.021, 0.012, tk.GUNMETAL, bone, [v])
            if jacket:
                jl = min(0.22, length * 0.6)
                k.lathe("Jacket", [(0, 0), (0.038, 0), (0.042, 0.01), (0.042, jl), (0.034, jl + 0.014), (0, jl + 0.014)],
                        tk.frame(b0, z=FWD), tk.STEEL.but(rings=0.024), bone, [v], sides=14)
                for zz in (0.06, 0.16):
                    k.torus("Coil", b0 + FWD * zz, FWD, 0.044, 0.0065, tk.glow(self.accent, 1.4), bone, [v], seg=16, sides=4)
            k.torus("MuzzleRing", b0 + FWD * (length - 0.02), FWD, 0.025, 0.006, tk.YELLOW, bone, [v], seg=14, sides=4)
            self.barrel(v, b0, FWD, length + 0.005)

        rifle("T1", 0.0, 0.35, "Barrel_1")
        rifle("T2", -0.052, 0.36, "Barrel_1")
        rifle("T2", 0.052, 0.36, "Barrel_2")
        # T3A: a four-barrel rotary cluster in a cooling shroud, with a drum feed.
        b0 = V(0, -0.11, z)
        k.cyl("Hub", b0, b0 + FWD * 0.03, 0.045, look=tk.DARK, bone="Barrel_1", on=["T3A"], sides=14)
        for i in range(4):
            a = math.radians(45 + 90 * i)
            off = V(math.cos(a) * 0.03, 0, math.sin(a) * 0.03)
            k.barrel("RotaryTube", b0 + off, b0 + off + FWD * 0.4, 0.014, 0.008, tk.GUNMETAL, "Barrel_1", ["T3A"], sides=10)
        k.lathe("Shroud", [(0, 0), (0.06, 0), (0.068, 0.012), (0.068, 0.19), (0.058, 0.205), (0, 0.205)],
                tk.frame(b0 + FWD * 0.02, z=FWD), tk.STEEL.but(rings=0.03), "Barrel_1", ["T3A"], sides=16)
        k.torus("ShroudGlow", b0 + FWD * 0.2, FWD, 0.066, 0.007, tk.glow(self.accent, 1.5), "Barrel_1", ["T3A"], seg=18, sides=4)
        k.cyl("Clamp", b0 + FWD * 0.33, b0 + FWD * 0.35, 0.05, look=tk.GUNMETAL, bone="Barrel_1", on=["T3A"], sides=14)
        k.torus("MuzzleRing", b0 + FWD * 0.39, FWD, 0.048, 0.006, tk.YELLOW, "Barrel_1", ["T3A"], seg=16, sides=4)
        self.barrel("T3A", b0, FWD, 0.41)
        k.cyl("Drum", V(0.16, 0.0, 0.33), V(0.24, 0.0, 0.33), 0.085, look=tk.GUNMETAL.but(rings=0.03), bone="Pitch", on=["T3A"], sides=18)
        k.torus("DrumBand", V(0.2, 0.0, 0.33), XAX, 0.086, 0.007, tk.YELLOW, "Pitch", ["T3A"], seg=20, sides=4)
        k.block("Feed", (0.08, 0.05, 0.03), tk.frame((0.12, -0.05, 0.35)), tk.DARK, "Pitch", ["T3A"])
        # T3B: one long marksman barrel with capacitor rings and a brake.
        b0 = V(0, -0.11, z)
        k.barrel("LongTube", b0, b0 + FWD * 0.45, 0.025, 0.014, tk.GUNMETAL, "Barrel_1", ["T3B"], sides=14)
        for i, zz in enumerate((0.07, 0.14, 0.21)):
            k.cyl("Capacitor", b0 + FWD * (zz - 0.02), b0 + FWD * (zz + 0.02), 0.046, look=tk.GUNMETAL, bone="Barrel_1", on=["T3B"], sides=14)
            k.torus("CapGlow", b0 + FWD * zz, FWD, 0.047, 0.007, tk.glow(self.accent, 1.5), "Barrel_1", ["T3B"], seg=16, sides=4)
        k.block("Brake", (0.07, 0.07, 0.05), tk.frame(b0 + FWD * 0.45), tk.DARK.but(panel=0.022), "Barrel_1", ["T3B"])
        k.torus("MuzzleRing", b0 + FWD * 0.405, FWD, 0.029, 0.006, tk.YELLOW, "Barrel_1", ["T3B"], seg=14, sides=4)
        self.barrel("T3B", b0, FWD, 0.485)

    def clips(self, arm, v):
        acts = super().clips(arm, v)
        if v == "T3A":
            acts.append(tk.clip(arm, "Spin", 12, lambda t, f: {"Barrel_1": {"rot": (FWD, 2 * math.pi * t)}}, loop=True))
        return acts


class Cannon(Spec):
    name = "Cannon"
    accent = "amber"
    recoil = 0.07
    pitch_at = V(0, -0.12, 0.32)
    smoke_at = V(0, 0.1, 0.4)

    def pieces(self, k):
        ring_base(k, 0.2)
        z = 0.32
        for v in self.variants:
            on = [v]
            wide = 1.06 if v in ("T3A", "T3B") else 1.0
            k.block("Hull", (0.4 * wide, 0.38, 0.12), tk.frame((0, 0.04, 0.305)), tk.BLUE, "Yaw", on, taper=(0.88, 0.78), shift=(0, 0.03), bevel=0.012)
            k.block("TopArmour", (0.33 * wide, 0.27, 0.026), tk.frame((0, 0.07, 0.376)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (0.36 * wide, 0.022, 0.13), tk.frame((0, -0.13, 0.3), rot=(-52, 0, 0)), tk.WHITE.but(panel=0.09), "Yaw", on)
            k.block("Slit", (0.07, 0.008, 0.012), tk.frame((0.1, -0.12, 0.345), rot=(-52, 0, 0)), tk.glow(self.accent, 1.5), "Yaw", on, bevel=0.002)
            k.block("Hatch", (0.1, 0.1, 0.02), tk.frame((-0.07, 0.12, 0.395)), tk.GUNMETAL, "Yaw", on)
            for sx in (-1, 1):
                k.block("SideLight", (0.012, 0.16, 0.014), tk.frame((sx * 0.19 * wide, 0.05, 0.33), rot=(0, sx * 12, 0)),
                        tk.glow(self.accent, 1.3), "Yaw", on, bevel=0.002)
            k.cyl("Periscope", (0.08, 0.12, 0.385), (0.08, 0.12, 0.415), 0.022, look=tk.DARK, bone="Yaw", on=on, sides=10)
        for v in ("T2", "T3A", "T3B"):
            for s in (-1, 1):
                k.block("Skirt", (0.024, 0.36, 0.12), tk.frame((s * 0.225, 0.04, 0.29), rot=(0, s * 20, 0)), tk.WHITE.but(panel=0.09), "Yaw", [v])
                k.block("SkirtStripe", (0.026, 0.3, 0.022), tk.frame((s * 0.245, 0.04, 0.245), rot=(0, s * 20, 0)), tk.HAZARD, "Yaw", [v], bevel=0.002)
            if v != "T3B":
                k.block("Rack", (0.26, 0.07, 0.07), tk.frame((0, 0.235, 0.3)), tk.GUNMETAL, "Yaw", [v])
                for i in range(4):
                    x = -0.09 + i * 0.06
                    k.cyl("Shell", (x, 0.235, 0.33), (x, 0.235, 0.39), 0.018, 0.012, look=tk.YELLOW.but(fam="metal", rough=0.35), bone="Yaw", on=[v], sides=10)
        # T3B: counterweight bustle.
        k.block("Bustle", (0.32, 0.12, 0.11), tk.frame((0, 0.25, 0.32)), tk.GUNMETAL, "Yaw", ["T3B"], taper=(0.9, 0.8))
        k.block("BustleStripe", (0.3, 0.012, 0.03), tk.frame((0, 0.312, 0.31)), tk.HAZARD, "Yaw", ["T3B"], bevel=0.002)

        for v, w in (("T1", 0.17), ("T2", 0.18), ("T3A", 0.28), ("T3B", 0.2)):
            k.block("Mantlet", (w, 0.09, 0.13), tk.frame((0, -0.155, z)), tk.GUNMETAL, "Pitch", [v], bevel=0.012)
            k.cyl("Trunnion", (-w / 2 - 0.02, -0.12, z), (w / 2 + 0.02, -0.12, z), 0.04, look=tk.DARK, bone="Pitch", on=[v], sides=12)

        def gun(v, x, r, length, bone, extractor=False, big=False):
            b0 = V(x, -0.18, z)
            k.barrel("Tube", b0, b0 + FWD * length, r, r * 0.62, tk.GUNMETAL, bone, [v], sides=16)
            if extractor:
                k.lathe("Extractor", [(0, 0), (r * 1.1, 0), (r * 1.4, 0.02), (r * 1.4, 0.06), (r * 1.1, 0.08), (0, 0.08)],
                        tk.frame(b0 + FWD * (length * 0.45), z=FWD), tk.STEEL, bone, [v], sides=16)
            k.torus("MuzzleRing", b0 + FWD * (length - 0.015), FWD, r * 1.06, 0.007, tk.YELLOW, bone, [v], seg=16, sides=4)
            bw = r * 2.4
            k.block("Brake", (bw, 0.07, r * 1.8), tk.frame(b0 + FWD * (length + 0.03)), tk.DARK.but(panel=0.024), bone, [v], bevel=0.006)
            if big:
                k.block("Brake2", (bw * 1.1, 0.05, r * 2.0), tk.frame(b0 + FWD * (length + 0.1)), tk.DARK.but(panel=0.024), bone, [v], bevel=0.006)
                k.cyl("BrakeLink", b0 + FWD * (length + 0.06), b0 + FWD * (length + 0.08), r * 0.9, look=tk.GUNMETAL, bone=bone, on=[v], sides=12)
            self.barrel(v, b0, FWD, length + (0.13 if big else 0.065))

        gun("T1", 0.0, 0.042, 0.26, "Barrel_1")
        gun("T2", 0.0, 0.044, 0.3, "Barrel_1", extractor=True)
        gun("T3A", -0.075, 0.036, 0.3, "Barrel_1", extractor=True)
        gun("T3A", 0.075, 0.036, 0.3, "Barrel_2", extractor=True)
        gun("T3B", 0.0, 0.056, 0.36, "Barrel_1", extractor=True, big=True)
        for s in (-1, 1):
            k.cyl("RecoilCylinder", (s * 0.075, -0.17, z + 0.05), (s * 0.075, -0.34, z + 0.05), 0.016, look=tk.STEEL.but(rings=0.04), bone="Pitch", on=["T3B"], sides=10)


class Laser(Spec):
    name = "Laser"
    accent = "cyan"
    recoil = 0.0
    pitch_at = V(0, 0, 0.43)
    smoke_at = V(0, 0.1, 0.5)

    def pieces(self, k):
        ring_base(k, 0.15)
        z = 0.43
        k.block("YokeBase", (0.34, 0.18, 0.06), tk.frame((0, 0, 0.3)), tk.BLUE.but(panel=0.08), "Yaw", taper=(0.95, 0.9))
        for s in (-1, 1):
            k.block("Arm", (0.05, 0.13, 0.19), tk.frame((s * 0.15, 0, 0.405)), tk.BLUE.but(panel=0.07), "Yaw", taper=(1.0, 0.8))
            k.block("ArmCap", (0.056, 0.11, 0.022), tk.frame((s * 0.15, 0, 0.505)), tk.WHITE.but(panel=0), "Yaw")
            k.cyl("TrunnionStub", (s * 0.122, 0, z), (s * 0.18, 0, z), 0.032, look=tk.GUNMETAL, bone="Pitch", sides=12)
        for v in ("T2", "T3A", "T3B"):
            for s in (-1, 1):
                k.cyl("CapCell", (s * 0.19, 0.03, 0.33), (s * 0.19, 0.03, 0.42), 0.02, look=tk.glow(self.accent, 1.2), bone="Yaw", on=[v], sides=10)
                k.block("CapClamp", (0.05, 0.05, 0.016), tk.frame((s * 0.18, 0.03, 0.38)), tk.GUNMETAL, "Yaw", [v], bevel=0.003)

        rear = V(0, 0.15, z)

        def emitter(v, lens_r, house_r, body_len=0.27):
            on = [v]
            k.cyl("RearCap", rear + V(0, 0.01, 0), rear + FWD * 0.04, 0.058, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=16)
            k.lathe("Body", [(0, 0), (0.07, 0), (0.08, 0.02), (0.08, body_len - 0.03), (0.072, body_len), (0, body_len)],
                    tk.frame(rear + FWD * 0.03, z=FWD), tk.WHITE.but(rings=0.07), "Barrel_1", on, sides=18)
            k.torus("BodyBand", rear + FWD * 0.2, FWD, 0.081, 0.008, tk.BLUE.but(panel=0), "Barrel_1", on, seg=18, sides=4)
            front = rear + FWD * (0.03 + body_len)
            k.lathe("Housing", [(0, 0), (house_r * 0.8, 0), (house_r, 0.02), (house_r, 0.045), (lens_r * 1.05, 0.05), (0, 0.05)],
                    tk.frame(front, z=FWD), tk.DARK, "Barrel_1", on, sides=20)
            k.lathe("Lens", [(0, 0), (lens_r, 0), (lens_r * 0.92, 0.012), (lens_r * 0.6, 0.022), (0, 0.027)],
                    tk.frame(front + FWD * 0.04, z=FWD), tk.glow(self.accent, 1.0, lens=lens_r), "Barrel_1", on, sides=20, bevel=0.0)
            self.barrel(v, rear, FWD, 0.03 + body_len + 0.07)
            return front

        def fins(v, count, size):
            for i in range(count):
                a = math.radians(90 + (i - (count - 1) / 2) * (180 / max(count, 2)) if count <= 2 else 45 + i * 360 / count)
                d = V(math.cos(a), 0, math.sin(a))
                c = rear + FWD * 0.06 + d * (0.08 + size[1] / 2)
                m = tk.frame(c, z=V(0, 1, 0).cross(d).normalized(), y=d)
                k.block("Fin", (size[0], size[1], 0.012), m, tk.WHITE.but(panel=0), "Barrel_1", [v], bevel=0.003)
                k.block("FinEdge", (size[0] * 0.8, 0.008, 0.014), tk.frame(c + d * (size[1] / 2), z=V(0, 1, 0).cross(d).normalized(), y=d),
                        tk.glow(self.accent, 1.1), "Barrel_1", [v], bevel=0.0)

        emitter("T1", 0.07, 0.092)
        fins("T1", 2, (0.12, 0.05))
        f2 = emitter("T2", 0.09, 0.115)
        fins("T2", 4, (0.12, 0.05))
        f3 = emitter("T3A", 0.115, 0.14, body_len=0.29)
        fins("T3A", 4, (0.14, 0.06))
        for i in range(3):
            k.torus("Coil", rear + FWD * (0.09 + i * 0.06), FWD, 0.083, 0.009, tk.glow(self.accent, 1.5), "Barrel_1", ["T3A"], seg=18, sides=4)
        # T3B: long telescopic focus with stacked rings, a small lens and radiator wings.
        on = ["T3B"]
        k.cyl("RearCap", rear + V(0, 0.01, 0), rear + FWD * 0.04, 0.058, look=tk.GUNMETAL, bone="Barrel_1", on=on, sides=16)
        k.lathe("Body", [(0, 0), (0.07, 0), (0.08, 0.02), (0.08, 0.24), (0.072, 0.27), (0, 0.27)],
                tk.frame(rear + FWD * 0.03, z=FWD), tk.WHITE.but(rings=0.07), "Barrel_1", on, sides=18)
        y0 = 0.3
        for r, L in ((0.064, 0.12), (0.054, 0.11), (0.045, 0.1)):
            k.cyl("Telescope", rear + FWD * y0, rear + FWD * (y0 + L), r, look=tk.STEEL.but(rings=0.035), bone="Barrel_1", on=on, sides=16)
            k.torus("FocusRing", rear + FWD * (y0 + 0.01), FWD, r + 0.004, 0.008, tk.glow(self.accent, 1.5), "Barrel_1", on, seg=16, sides=4)
            y0 += L
        k.lathe("Housing", [(0, 0), (0.04, 0), (0.055, 0.015), (0.055, 0.03), (0.046, 0.035), (0, 0.035)],
                tk.frame(rear + FWD * y0, z=FWD), tk.DARK, "Barrel_1", on, sides=16)
        k.lathe("Lens", [(0, 0), (0.044, 0), (0.04, 0.01), (0.025, 0.016), (0, 0.019)],
                tk.frame(rear + FWD * (y0 + 0.03), z=FWD), tk.glow(self.accent, 1.1, lens=0.044), "Barrel_1", on, sides=16, bevel=0.0)
        self.barrel("T3B", rear, FWD, y0 + 0.05)
        for s in (-1, 1):
            poly = [(-0.02, 0.0), (0.16, 0.0), (0.16, 0.12), (0.04, 0.17), (-0.02, 0.17)]
            m = tk.frame(rear + V(s * 0.085, -0.02, -0.005), z=UP, y=V(0, 1, 0))
            pts = [(s * x, y) for x, y in poly] if s > 0 else [(s * x, y) for x, y in reversed(poly)]
            k.plate("Wing", pts, 0.014, m, tk.WHITE.but(panel=0.06), "Barrel_1", on)
            k.block("WingEdge", (0.008, 0.11, 0.016), tk.frame(rear + V(s * 0.245, 0.04, -0.005)), tk.glow(self.accent, 1.2), "Barrel_1", on, bevel=0.0)

        # Focusing prongs (T2, T3A) on their own bones: they close in during ChargeUp.
        self.prongs = {}
        for v, front, house_r, length in (("T2", f2, 0.115, 0.1), ("T3A", f3, 0.14, 0.17)):
            bones = []
            for i in range(3):
                a = math.radians(90 + 120 * i)
                radial = V(math.cos(a), 0, math.sin(a))
                base = front + FWD * 0.03 + radial * (house_r + 0.008)
                tip = base + FWD * length - radial * length * 0.22
                name = f"Prong_{i + 1}"
                k.block("Prong", (0.022, 0.016, (tip - base).length), tk.frame((base + tip) / 2, z=tip - base, y=radial), tk.WHITE.but(panel=0), name, [v], bevel=0.003)
                k.block("ProngTip", (0.024, 0.018, 0.022), tk.frame(tip, z=tip - base, y=radial), tk.glow(self.accent, 1.6), name, [v], bevel=0.002)
                bones.append(tk.Bone(name, base, (tip - base), "Barrel_1", length=0.05))
                self.prongs.setdefault(v, []).append((name, radial))
            self.extra_bones[v] = bones

    def clips(self, arm, v):
        acts = [tk.clip(arm, "IdleSweep", 120, lambda t, f: {
            "Yaw": {"rot": (UP, 0.4 * math.sin(2 * math.pi * t))},
            "Pitch": {"rot": (XAX, -0.05 * math.sin(4 * math.pi * t))}}, loop=True)]
        prongs = self.prongs.get(v, [])

        def close(amount):
            out = {}
            for name, radial in prongs:
                axis = radial.cross(FWD).normalized()
                out[name] = {"rot": (axis, -0.28 * amount)}
            return out

        def charge(t, f):
            e = ease(t)
            spec = close(e)
            spec["Barrel_1"] = {"loc": FWD * (0.018 * e)}
            return spec

        def beam(t, f):
            spec = close(1.0 + 0.06 * math.sin(2 * math.pi * 2 * t))
            spec["Barrel_1"] = {"loc": FWD * (0.018 + 0.002 * math.sin(2 * math.pi * 3 * t))}
            return spec

        acts.append(tk.clip(arm, "ChargeUp", 24, charge))
        acts.append(tk.clip(arm, "Beam", 12, beam, loop=True))
        return acts


FLAK_ELEV = math.radians(40)
FLAK_D = Vector((0, -math.cos(FLAK_ELEV), math.sin(FLAK_ELEV)))
FLAK_UP = Vector((0, math.sin(FLAK_ELEV), math.cos(FLAK_ELEV)))


class Flak(Spec):
    name = "Flak"
    accent = "ice"
    recoil = 0.04
    pitch_at = V(0, 0, 0.4)
    smoke_at = V(0, 0.12, 0.42)
    preview_pitch = 0.0

    def pieces(self, k):
        ring_base(k, 0.18)
        t = V(0, 0, 0.4)
        k.block("Carriage", (0.3, 0.26, 0.08), tk.frame((0, 0.02, 0.29)), tk.BLUE, "Yaw", taper=(0.92, 0.85))
        k.block("CarriageTop", (0.24, 0.12, 0.02), tk.frame((0, 0.1, 0.335)), tk.WHITE.but(panel=0), "Yaw")
        for s in (-1, 1):
            k.block("Cheek", (0.04, 0.17, 0.15), tk.frame((s * 0.135, 0.0, 0.37)), tk.WHITE.but(panel=0.07), "Yaw", taper=(1.0, 0.65))
            k.cyl("TrunnionStub", (s * 0.11, 0, 0.4), (s * 0.16, 0, 0.4), 0.03, look=tk.GUNMETAL, bone="Pitch", sides=12)
        # Ammunition drums on the cheeks: left from T1, right from T2.
        for v in self.variants:
            for s in ((-1,) if v == "T1" else (-1, 1)):
                c = V(s * 0.2, 0.03, 0.36)
                k.cyl("Drum", c - V(0.03, 0, 0), c + V(0.03, 0, 0), 0.07, look=tk.GUNMETAL.but(rings=0.02), bone="Yaw", on=[v], sides=18)
                k.torus("DrumBand", c, XAX, 0.071, 0.007, tk.YELLOW, "Yaw", [v], seg=18, sides=4)
                k.cyl("DrumCap", c + V(s * 0.03, 0, 0), c + V(s * 0.036, 0, 0), 0.045, look=tk.DARK, bone="Yaw", on=[v], sides=12, bevel=0.0)
        # Radar: small dish on T2, big on T3B, a mast on T3A.
        for v, r, h in (("T2", 0.06, 0.12), ("T3A", 0.055, 0.12), ("T3B", 0.11, 0.17)):
            base = V(0.06, 0.13, 0.33)
            k.cyl("Mast", base, base + UP * h, 0.012, look=tk.GUNMETAL, bone="Yaw", on=[v], sides=8)
            dish = base + UP * h
            m = tk.frame(dish, z=V(0, 0.5, 0.86))
            k.lathe("Dish", [(0, 0), (r, 0.02), (r * 1.02, 0.03), (r * 0.96, 0.034), (0, 0.012)], m, tk.WHITE.but(panel=0), "Yaw", [v], sides=16, bevel=0.0)
            k.cyl("Feedhorn", dish, dish + V(0, 0.5, 0.86) * 0.05, 0.007, look=tk.glow(self.accent, 1.5), bone="Yaw", on=[v], sides=6, bevel=0.0)

        cr = tk.frame(t + FLAK_D * 0.03, z=FLAK_D)
        k.block("Cradle", (0.16, 0.09, 0.22), cr, tk.GUNMETAL, "Pitch")

        def shield(v, w, h):
            m = tk.frame(t + FLAK_D * 0.13 + FLAK_UP * 0.01, z=FLAK_D, y=FLAK_UP)
            k.block("Shield", (w, h, 0.022), m, tk.WHITE.but(panel=0.08), "Pitch", [v])
            m2 = tk.frame(t + FLAK_D * 0.142 + FLAK_UP * (0.01 + h / 2 - 0.016), z=FLAK_D, y=FLAK_UP)
            k.block("ShieldStripe", (w * 0.96, 0.024, 0.008), m2, tk.HAZARD, "Pitch", [v], bevel=0.0)

        def tube(v, x, u, length, r, bone, hider=False, brake=False, sides=12):
            b0 = t + V(x, 0, 0) + FLAK_UP * u + FLAK_D * 0.06
            k.barrel("Tube", b0, b0 + FLAK_D * length, r, r * 0.6, tk.GUNMETAL, bone, [v], sides=sides)
            k.lathe("Jacket", [(0, 0), (r * 1.5, 0), (r * 1.5, 0.12), (0, 0.13)], tk.frame(b0 + FLAK_D * 0.05, z=FLAK_D),
                    tk.STEEL.but(rings=0.02), bone, [v], sides=sides)
            if hider:
                k.lathe("Hider", [(0, 0), (r * 1.4, 0), (r * 1.4, 0.05), (r * 0.7, 0.05), (0, 0.04)],
                        tk.frame(b0 + FLAK_D * (length - 0.01), z=FLAK_D), tk.DARK.but(rings=0.012), bone, [v], sides=min(sides, 10))
            if brake:
                k.block("Brake", (r * 2.4, r * 2.0, 0.05), tk.frame(b0 + FLAK_D * (length + 0.015), z=FLAK_D, y=FLAK_UP),
                        tk.DARK.but(panel=0.02), bone, [v], bevel=0.004)
            k.torus("MuzzleRing", b0 + FLAK_D * (length - 0.03), FLAK_D, r * 1.1, 0.005, tk.YELLOW, bone, [v], seg=12, sides=4)
            extra = 0.05 if (hider or brake) else 0.0
            self.barrel(v, b0, FLAK_D, length + extra)

        shield("T1", 0.26, 0.16)
        tube("T1", -0.045, 0.0, 0.38, 0.018, "Barrel_1")
        tube("T1", 0.045, 0.0, 0.38, 0.018, "Barrel_2")
        shield("T2", 0.28, 0.17)
        tube("T2", -0.045, 0.0, 0.44, 0.019, "Barrel_1", hider=True)
        tube("T2", 0.045, 0.0, 0.44, 0.019, "Barrel_2", hider=True)
        shield("T3A", 0.32, 0.2)
        for i, (x, u) in enumerate(((-0.045, 0.03), (0.045, 0.03), (-0.045, -0.025), (0.045, -0.025))):
            tube("T3A", x, u, 0.44, 0.017, f"Barrel_{i + 1}", hider=True, sides=10)
        shield("T3B", 0.3, 0.18)
        tube("T3B", -0.05, 0.0, 0.52, 0.023, "Barrel_1", brake=True)
        tube("T3B", 0.05, 0.0, 0.52, 0.023, "Barrel_2", brake=True)

    def clips(self, arm, v):
        acts = super().clips(arm, v)
        n = len(self.barrels[v])
        step = 16 // n

        def alternate(t, f):
            spec = {}
            for i, (_, d, _) in enumerate(self.barrels[v]):
                k = kick((f - i * step) % 16, 8)
                spec[f"Barrel_{i + 1}"] = {"loc": d * (-self.recoil * k)}
            return spec

        acts.append(tk.clip(arm, "FireAlternate", 16, alternate, loop=True))
        return acts


class GateGun(Spec):
    name = "GateGun"
    variants = ["T1", "T2", "T3"]
    accent = "gate"
    recoil = 0.025
    has_pad = False
    head_scale = 1.0
    yaw_z = 0.06
    pitch_at = V(0, -0.07, 0.14)
    smoke_at = V(0, 0.06, 0.2)

    def kit(self):
        k = tk.Kit("Tower_" + self.name, self.variants)
        k.block("Saddle", (0.34, 0.2, 0.06), tk.frame((0, 0, 0.03)), tk.GUNMETAL, "Root")
        for s in (-1, 1):
            k.block("Jaw", (0.34, 0.03, 0.12), tk.frame((0, s * 0.115, -0.02)), tk.DARK, "Root")
            k.block("JawStripe", (0.3, 0.008, 0.02), tk.frame((0, s * 0.131, -0.066)), tk.HAZARD, "Root", bevel=0.0)
        for x in (-0.13, 0.13):
            k.cyl("Bolt", (x, -0.13, -0.06), (x, 0.13, -0.06), 0.012, look=tk.STEEL, bone="Root", sides=8, lod=False)
        self.stars = rank_plate(k, V(0, 0.142, -0.005), 44.0, 0.07, (0.044, 0.019), width=0.24, height=0.07)
        k.lathe("TurretRing", [(0, 0.06), (0.11, 0.06), (0.11, 0.075), (0.098, 0.085), (0, 0.085)], tk.frame(), tk.GUNMETAL, "Yaw", sides=16)
        self.pieces(k)
        return k

    def pieces(self, k):
        z = 0.14
        for v, w in (("T1", 0.22), ("T2", 0.25), ("T3", 0.26)):
            on = [v]
            k.block("Hull", (w, 0.24, 0.09), tk.frame((0, 0.02, 0.13)), tk.BLUE, "Yaw", on, taper=(0.85, 0.8), shift=(0, 0.01))
            k.block("TopArmour", (w * 0.8, 0.19, 0.02), tk.frame((0, 0.03, 0.184)), tk.WHITE, "Yaw", on, taper=(0.9, 0.85))
            k.block("Glacis", (w * 0.95, 0.02, 0.09), tk.frame((0, -0.1, 0.13), rot=(-30, 0, 0)), tk.WHITE.but(panel=0.07), "Yaw", on)
            k.block("Eye", (0.04, 0.008, 0.012), tk.frame((w * 0.25, -0.118, 0.15), rot=(-30, 0, 0)), tk.glow(self.accent, 1.5), "Yaw", on, bevel=0.002)
            k.cyl("Mantlet", (-w * 0.32, -0.07, z), (w * 0.32, -0.07, z), 0.04, look=tk.GUNMETAL, bone="Pitch", on=on, sides=14)
        k.block("AmmoBox", (0.07, 0.13, 0.07), tk.frame((-0.17, 0.03, 0.12)), tk.GUNMETAL, "Yaw", ["T2", "T3"])
        k.block("AmmoStripe", (0.072, 0.1, 0.014), tk.frame((-0.17, 0.03, 0.15)), tk.HAZARD, "Yaw", ["T2", "T3"], bevel=0.0)
        k.block("Hood", (0.17, 0.14, 0.026), tk.frame((0, -0.13, 0.178)), tk.WHITE.but(panel=0.06), "Pitch", ["T3"])
        k.cyl("SensorMast", (0.08, 0.1, 0.18), (0.08, 0.1, 0.29), 0.008, look=tk.GUNMETAL, bone="Yaw", on=["T3"], sides=8)
        k.lathe("SensorBall", [(0, 0), (0.016, 0.006), (0.02, 0.02), (0.016, 0.034), (0, 0.04)], tk.frame((0.08, 0.1, 0.285)),
                tk.glow(self.accent, 1.6), "Yaw", ["T3"], sides=10, bevel=0.0)

        def gun(v, x, bone):
            b0 = V(x, -0.09, z)
            k.barrel("Tube", b0, b0 + FWD * 0.28, 0.017, 0.01, tk.GUNMETAL, bone, [v], sides=12)
            k.lathe("Jacket", [(0, 0), (0.026, 0), (0.028, 0.008), (0.028, 0.12), (0.022, 0.13), (0, 0.13)],
                    tk.frame(b0 + FWD * 0.01, z=FWD), tk.STEEL.but(rings=0.02), bone, [v], sides=12)
            k.torus("MuzzleRing", b0 + FWD * 0.265, FWD, 0.02, 0.005, tk.YELLOW, bone, [v], seg=12, sides=4)
            self.barrel(v, b0, FWD, 0.285)

        gun("T1", 0.0, "Barrel_1")
        for v in ("T2", "T3"):
            gun(v, -0.042, "Barrel_1")
            gun(v, 0.042, "Barrel_2")


SPECS = {"Gun": Gun, "Cannon": Cannon, "Laser": Laser, "Flak": Flak, "GateGun": GateGun}


# -------------------------------------------------------------------------------------------------------------
# Wreck
# -------------------------------------------------------------------------------------------------------------

WRECK_VARIANTS = ["Pad", "Gun", "Cannon", "Laser", "Flak", "GateGun"]


def wreck_kit():
    k = tk.Kit("Tower_Wreck", WRECK_VARIANTS)
    pad(k, "azure", burnt=True)
    rng = np.random.default_rng(7)
    hull = burn(tk.BLUE)
    # The fallen head: a hull leaning off the collar, a split top plate, a bent pipe.
    k.block("Husk", (0.34, 0.3, 0.13), tk.frame((0.1, -0.06, 0.24), rot=(34, -26, 38)), hull, "Root", taper=(0.86, 0.82), bevel=0.01)
    k.barrel("HuskBarrel", (0.02, -0.18, 0.3), (-0.12, -0.42, 0.06), 0.026, 0.015, burn(tk.GUNMETAL), "Root", sides=12)
    k.block("HuskPlate", (0.24, 0.2, 0.024), tk.frame((-0.12, 0.1, 0.25), rot=(-24, 14, 50)), burn(tk.WHITE), "Root")
    k.block("HuskPlate2", (0.16, 0.14, 0.022), tk.frame((0.16, -0.12, 0.27), rot=(25, -35, -20)), burn(tk.WHITE), "Root")
    k.cyl("Pipe", (-0.05, -0.06, 0.24), (-0.18, -0.2, 0.3), 0.014, look=burn(tk.STEEL), bone="Root", sides=8)
    k.cyl("Pipe", (-0.18, -0.2, 0.3), (-0.3, -0.24, 0.22), 0.014, look=burn(tk.STEEL), bone="Root", sides=8)
    k.cyl("Collar", (0, 0, 0.214), (0, 0, 0.25), 0.15, 0.13, look=burn(tk.GUNMETAL), bone="Root", sides=16)
    # Scrap pile spilling over the edge.
    for i in range(26):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.1, 0.44)
        on_deck = r < 0.29
        size = rng.uniform(0.04, 0.1, 3) * (1, 1, 0.25)
        zc = (0.214 if on_deck else 0.0) + size[2] / 2 + rng.uniform(0, 0.02)
        look = burn([tk.WHITE, tk.BLUE, tk.GUNMETAL, tk.STEEL][i % 4])
        k.block("Scrap", tuple(size), tk.frame((math.cos(a) * r, math.sin(a) * r * 0.9 - 0.04, zc),
                                               rot=tuple(rng.uniform(-35, 35, 2)) + (rng.uniform(0, 180),)), look, "Root", bevel=0.004)
    for i in range(6):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.15, 0.4)
        p0 = V(math.cos(a) * r, math.sin(a) * r, 0.23 if r < 0.29 else 0.012)
        d = V(rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.2, 0.5)).normalized()
        k.cyl("Rod", p0, p0 + d * rng.uniform(0.1, 0.18), rng.uniform(0.007, 0.012), look=burn(tk.STEEL), bone="Root", sides=6, bevel=0.0)
    # A few embers and one dying cyan cell in the rubble.
    for p in ((0.05, -0.08, 0.235), (-0.1, 0.02, 0.23), (0.12, 0.1, 0.24), (-0.02, 0.16, 0.225), (0.2, -0.02, 0.225)):
        k.block("Ember", (0.035, 0.022, 0.014), tk.frame(p, rot=(0, 0, 30)), tk.glow("ember", 1.0), "Root", bevel=0.0)
    k.cyl("DeadCell", (-0.2, -0.05, 0.22), (-0.2, 0.06, 0.24), 0.02, look=tk.glow("cyan", 0.6), bone="Root", sides=8)
    # Per-tower debris, lying on the ground beside the pad.
    at = V(0.36, -0.2, 0.0)
    rot = tk.frame(at, rot=(0, 0, 35))

    def P(x, y, z):
        return rot @ V(x, y, z)

    k.barrel("GunBarrel", P(0, 0.1, 0.03), P(0, -0.12, 0.03), 0.021, 0.012, burn(tk.GUNMETAL), "Root", ["Gun"])
    k.lathe("GunJacket", [(0, 0), (0.042, 0), (0.042, 0.1), (0, 0.11)], tk.frame(P(0, 0.08, 0.03), z=P(0, -1, 0.03) - P(0, 0, 0.03)),
            burn(tk.STEEL.but(rings=0.024)), "Root", ["Gun"], sides=12)
    k.block("CannonBrake", (0.1, 0.07, 0.075), tk.frame(P(0, 0, 0.038), rot=(0, 12, 35)), burn(tk.DARK.but(panel=0.024)), "Root", ["Cannon"])
    k.barrel("CannonStub", P(0, 0.17, 0.045), P(0, 0.035, 0.04), 0.042, 0.026, burn(tk.GUNMETAL), "Root", ["Cannon"], sides=14)
    k.lathe("LaserHousing", [(0, 0), (0.09, 0), (0.1, 0.02), (0.1, 0.045), (0.07, 0.05), (0, 0.05)],
            tk.frame(P(0, 0, 0.06), z=V(0.3, 0.6, 0.75)), burn(tk.DARK), "Root", ["Laser"], sides=18)
    k.lathe("LaserGlass", [(0, 0), (0.07, 0), (0.05, 0.02), (0, 0.025)], tk.frame(P(0, 0, 0.06) + V(0.3, 0.6, 0.75).normalized() * 0.045, z=V(0.3, 0.6, 0.75)),
            tk.GLASS.but(wear=1.0), "Root", ["Laser"], sides=18, bevel=0.0)
    k.cyl("FlakDrum", P(-0.03, 0, 0.07), P(0.03, 0, 0.07), 0.07, look=burn(tk.GUNMETAL.but(rings=0.02)), bone="Root", on=["Flak"], sides=16)
    for x in (-0.03, 0.03):
        k.barrel("FlakStub", P(x, -0.02, 0.02), P(x + 0.04, -0.2, 0.02), 0.018, 0.011, burn(tk.GUNMETAL), "Root", ["Flak"], sides=10)
    k.barrel("GateBarrel", P(0, 0.1, 0.02), P(0, -0.1, 0.02), 0.017, 0.01, burn(tk.GUNMETAL), "Root", ["GateGun"], sides=10)
    return k


# -------------------------------------------------------------------------------------------------------------
# Build, export, preview
# -------------------------------------------------------------------------------------------------------------

def build_variant(spec, kit, v, mat, name=None, lod=True):
    arm = tk.build_armature(name or f"{kit.name}_Rig", spec.bones(v))
    objs = [arm]
    lod0 = tk.assemble(kit.of(v), f"{kit.name}_{v}")
    lod0.data.materials.append(mat)
    tk.skin_to(lod0, arm)
    objs.append(lod0)
    if lod:
        lod1 = tk.assemble(kit.of(v), f"{kit.name}_{v}_LOD1", lod=True)
        lod1.data.materials.append(mat)
        tk.skin_to(lod1, arm)
        objs.append(lod1)
    return objs


def remove(objs):
    for o in objs:
        if o.name in bpy.data.objects:
            bpy.data.objects.remove(o)


def clear_actions():
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)


def stars_for(v):
    return {"T1": 1, "T2": 2}.get(v, 3)


def pose(arm, yaw_deg=0.0, pitch_deg=0.0, stars=3):
    if arm.animation_data:
        arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)
    from mathutils import Quaternion
    if "Yaw" in arm.pose.bones:
        arm.pose.bones["Yaw"].rotation_quaternion = Quaternion(tk.local_axis(arm, "Yaw", UP), math.radians(yaw_deg))
    if "Pitch" in arm.pose.bones:
        arm.pose.bones["Pitch"].rotation_quaternion = Quaternion(tk.local_axis(arm, "Pitch", XAX), -math.radians(pitch_deg))
    for i in range(3):
        name = f"Star_{i + 1}"
        if name in arm.pose.bones:
            arm.pose.bones[name].scale = (1, 1, 1) if i < stars else (0, 0, 0)


def build_tower(spec, out_root, bake=True):
    kit = spec.kit()
    uv = tk.unwrap_kit(kit)
    out_dir = os.path.join(C.EXPORTS, kit.name)
    paths = {s: os.path.join(out_dir, f"{kit.name}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    if bake or not all(os.path.exists(p) for p in paths.values()):
        tex = tk.bake(kit, out_dir)
    else:
        tex = {k.lower(): p for k, p in paths.items()}
    entries, detail = tk.detail_entries(uv["world_per_uv"])
    mat = C.game_material(kit.name + "_Mat", tex, 1.5, 0.45, entries)
    report = {"asset": kit.name, "texture_size": 1024, "low_size": 512, "maps": ["Albedo", "Normal", "Emission", "Mask"],
              "mask": "R AO, G smoothness, B metal detail mask, A ceramic detail mask", "uv": uv, "detail": detail,
              "variants": {}}
    for v in spec.variants:
        objs = build_variant(spec, kit, v, mat)
        arm, lod0, lod1 = objs
        acts = spec.clips(arm, v)
        tk.export_fbx(os.path.join(out_dir, f"{kit.name}_{v}.fbx"), objs, animated=True)
        bones = [b.name for b in arm.data.bones]
        report["variants"][v] = {
            "fbx": f"{kit.name}_{v}.fbx", "tris": C.tri_count(lod0), "tris_lod1": C.tri_count(lod1),
            "bones": bones, "deform_bones": [b.name for b in arm.data.bones if b.use_deform],
            "clips": {a.name: int(a.frame_range[1]) for a in acts},
            "muzzles": {f"Muzzle_{i + 1}": [round(c, 4) for c in m] for i, (_, _, m) in enumerate(spec.barrels[v])},
        }
        C.log(kit.name, v, report["variants"][v]["tris"], report["variants"][v]["tris_lod1"])
        remove(objs)
        clear_actions()
    report["pivot"] = "pad centre on the ground" if spec.has_pad else "top of the gate crossbeam, centre of the saddle"
    report["yaw_height"] = YAW_Z if spec.has_pad else spec.yaw_z
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    return kit, mat, report


def build_wreck(out_root, bake=True):
    kit = wreck_kit()
    uv = tk.unwrap_kit(kit)
    out_dir = os.path.join(C.EXPORTS, kit.name)
    tex = tk.bake(kit, out_dir) if bake else {s.lower(): os.path.join(out_dir, f"{kit.name}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    entries, detail = tk.detail_entries(uv["world_per_uv"])
    mat = C.game_material(kit.name + "_Mat", tex, 1.5, 0.45, entries)
    objs = {}
    for v in WRECK_VARIANTS:
        pieces = [p for p in kit.pieces if p.variants == {v}] if v != "Pad" else [p for p in kit.pieces if len(p.variants) > 1]
        name = "Wreck" if v == "Pad" else f"Debris_{v}"
        o = tk.assemble(pieces, name)
        o.data.materials.append(mat)
        o.vertex_groups.clear()
        objs[name] = o
    tk.export_fbx(os.path.join(out_dir, kit.name + ".fbx"), list(objs.values()), animated=False)
    report = {"asset": kit.name, "objects": {n: C.tri_count(o) for n, o in objs.items()}, "uv": uv, "detail": detail,
              "note": "Static. Wreck = burnt pad + fallen husk + scrap; show it with the tower's Debris_<Tower>."}
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    return kit, mat, objs, report


def build_star():
    k = tk.Kit("Tower_Star", ["S"])
    k.plate("Star", tk.star_poly(*RANK["star"]), 0.01, tk.frame(), tk.glow("star", 1.3), "Root", bevel=0.002)
    obj = tk.assemble(k.pieces, "Tower_Star", with_uv=False)
    obj.vertex_groups.clear()
    tk.export_fbx(os.path.join(C.EXPORTS, "Tower_Star", "Tower_Star.fbx"), [obj], animated=False)
    bpy.data.objects.remove(obj)


# --------------------------------------------------------------------------------------------------- previews

def stage_ground():
    g = C.ground(color=(0.035, 0.04, 0.05))
    road = bpy.data.meshes.new("Road")
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bm.to_mesh(road)
    bm.free()
    r = C.link(bpy.data.objects.new("Road", road))
    r.scale = (40, 1.3, 1)
    r.location = (0, -1.25, 0.002)
    m = C.principled("RoadMat", color=(0.07, 0.075, 0.09), rough=0.9)
    road.materials.append(m)
    return [g, r]


def render_game(objs_center, out, zoom=1.0, width=1280, height=720):
    lights = tk.game_lights()
    cam = tk.game_camera(objs_center, width / tk.PX_PER_UNIT / zoom)
    C.render(cam, out, width, height)
    remove(lights + [cam])


def previews(spec, kit, mat, out):
    tag = spec.name.lower()
    stage = stage_ground()
    rows = []
    spacing = 1.15
    n = len(spec.variants)
    for i, v in enumerate(spec.variants):
        objs = build_variant(spec, kit, v, mat, name=f"{kit.name}_{v}_Rig", lod=False)
        x = -(i - (n - 1) / 2) * spacing
        objs[0].location = (x, 0, 0 if spec.has_pad else 0.6)
        pose(objs[0], 115, spec.preview_pitch, stars_for(v))
        rows.append((v, objs))
    if not spec.has_pad:
        beams = []
        for i in range(n):
            x = -(i - (n - 1) / 2) * spacing
            b = C.link(bpy.data.objects.new("Beam", bpy.data.meshes.new("Beam")))
            import bmesh
            bm = bmesh.new()
            bmesh.ops.create_cube(bm, size=1.0)
            bm.to_mesh(b.data)
            bm.free()
            b.scale = (0.9, 0.22, 0.12)
            b.location = (x, 0, 0.6 - 0.06)
            b.data.materials.append(C.principled("BeamMat", color=(0.25, 0.3, 0.4), rough=0.5))
            p = C.link(b.copy())
            p.scale = (0.12, 0.2, 0.6)
            p.location = (x - 0.5, 0, 0.3)
            p2 = C.link(b.copy())
            p2.scale = (0.12, 0.2, 0.6)
            p2.location = (x + 0.5, 0, 0.3)
            beams += [b, p, p2]
        stage += beams
    centre = V(0, 0, 0.3)
    render_game(centre, os.path.join(out, f"{tag}_tiers_game.png"), zoom=1.0, width=640, height=360)
    render_game(centre, os.path.join(out, f"{tag}_tiers_game_zoom.png"), zoom=2.6, width=1280, height=720)
    # 3/4 close-ups per tier.
    rig = tk.close_lights(0.6)
    shots = []
    for i, (v, objs) in enumerate(rows):
        x = -(i - (n - 1) / 2) * spacing
        cam = tk.closeup_camera(V(x, 0, 0.32 if spec.has_pad else 0.75), 1.5, azimuth=28, elevation=26, lens=50)
        hide = [o for vv, oo in rows if vv != v for o in oo]
        for o in hide:
            o.hide_render = True
        p = os.path.join(out, f"{tag}_{v}_close.png")
        C.render(cam, p, 960, 720)
        for o in hide:
            o.hide_render = False
        shots.append(p)
        remove([cam])
    C.contact_sheet(shots, os.path.join(out, f"{tag}_close_sheet.png"), len(shots), 640)
    # Macro on the first tier.
    v0, objs0 = rows[0]
    x0 = (n - 1) / 2 * spacing
    for o in (o for vv, oo in rows if vv != v0 for o in oo):
        o.hide_render = True
    cam = tk.closeup_camera(V(x0 + 0.02, 0.05, 0.3 if spec.has_pad else 0.72), 0.45, azimuth=35, elevation=30, lens=70)
    C.render(cam, os.path.join(out, f"{tag}_macro.png"), 1280, 720)
    remove([cam])
    # Recoil / charge strip on the last tier, seen from the side.
    v_last, objs_last = rows[-1]
    for vv, oo in rows:
        for o in oo:
            o.hide_render = vv != v_last
    arm = objs_last[0]
    acts = spec.clips(arm, v_last)
    act = next(a for a in acts if a.name in ("Fire_1", "ChargeUp"))
    arm.animation_data.action = act
    xl = -(n - 1) / 2 * spacing
    cam = tk.closeup_camera(V(xl, -0.15, 0.4 if spec.has_pad else 0.8), 1.4, azimuth=90, elevation=10, lens=55)
    frames = (0, 1, 2, 4, 7, 10) if act.name == "Fire_1" else (0, 6, 12, 18, 24)
    strip = []
    for f in frames:
        bpy.context.scene.frame_set(f)
        p = os.path.join(out, f"_{tag}_strip_{f}.png")
        C.render(cam, p, 480, 360, bloom=False)
        strip.append(p)
    C.contact_sheet(strip, os.path.join(out, f"{tag}_{act.name.lower()}_strip.png"), len(strip), 400)
    for p in strip:
        os.remove(p)
    remove([cam] + rig)
    for vv, oo in rows:
        remove(oo)
    clear_actions()
    remove(stage)


def lineup(kits, out, tier_sets):
    """All towers in a row at game zoom, one row per tier set, among a few Skitters on the road."""
    stage = stage_ground()
    placed = []
    import boss_kit
    skitter = None
    try:
        skitter = boss_kit.append_base("Skitter")
    except Exception as e:  # the lineup still renders without enemies
        C.log("no Skitter for the lineup", e)
    for row, tiers in enumerate(tier_sets):
        for i, (name, (spec, kit, mat)) in enumerate(kits.items()):
            v = tiers.get(name, spec.variants[0])
            objs = build_variant(spec, kit, v, mat, name=f"{kit.name}_{v}_L{row}", lod=False)
            x = -(i - (len(kits) - 1) / 2) * 1.2
            objs[0].location = (x, row * 1.3, 0 if spec.has_pad else 0.6)
            pose(objs[0], 100 + 15 * i, spec.preview_pitch, stars_for(v))
            placed += objs
    copies = []
    if skitter:
        arm, mesh, actions = skitter
        walk = actions.get("Walk") or next(iter(actions.values()))
        entries = [(arm, mesh, walk, 3 * j, (x, -1.25 + 0.15 * ((j % 2) * 2 - 1), 0), math.radians(90), 0.45)
                   for j, x in enumerate((-2.6, -1.4, -0.3, 0.9, 2.2))]
        copies = boss_kit.stage(entries)
        bpy.context.scene.frame_set(1000)
    centre = V(0, 0.2, 0.3)
    render_game(centre, os.path.join(out, "towers_lineup_game.png"), zoom=1.0, width=1280, height=720)
    render_game(centre, os.path.join(out, "towers_lineup_zoom.png"), zoom=2.0, width=1280, height=720)
    if copies:
        boss_kit.unstage(copies)
    remove(placed + stage)


def wreck_previews(objs, out):
    stage = stage_ground()
    rig = tk.close_lights(0.6)
    names = [n for n in objs if n != "Wreck"]
    shots = []
    for n in names:
        for k, o in objs.items():
            o.hide_render = not (k == "Wreck" or k == n)
        cam = tk.closeup_camera(V(0.08, -0.05, 0.15), 1.4, azimuth=25, elevation=30, lens=50)
        p = os.path.join(out, f"wreck_{n.lower()}.png")
        C.render(cam, p, 800, 600)
        shots.append(p)
        remove([cam])
    C.contact_sheet(shots, os.path.join(out, "wreck_sheet.png"), 3, 560)
    for k, o in objs.items():
        o.hide_render = not (k == "Wreck" or k == "Debris_Cannon")
    remove(rig)
    render_game(V(0, 0, 0.15), os.path.join(out, "wreck_game_zoom.png"), zoom=3.0, width=960, height=540)
    remove(stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else list(SPECS) + ["Wreck"]
    bake = not args.get("nobake")
    render = not args.get("norender")
    C.preview_world()
    tk.metal_detail_map(force=True)
    build_star()
    kits = {}
    for n in names:
        if n == "Wreck":
            continue
        spec = SPECS[n]()
        kit, mat, report = build_tower(spec, out, bake)
        kits[n] = (spec, kit, mat)
        if render:
            previews(spec, kit, mat, out)
    if "Wreck" in names:
        kit, mat, objs, report = build_wreck(out, bake)
        if render:
            wreck_previews(objs, out)
        remove(list(objs.values()))
    if render and len(kits) > 1:
        lineup(kits, out, [{}, {"Gun": "T3A", "Cannon": "T3B", "Laser": "T3A", "Flak": "T3A", "GateGun": "T3"}])
    C.save_blend("Towers" if only is None else "Towers_" + "_".join(names))


if __name__ == "__main__":
    main()
