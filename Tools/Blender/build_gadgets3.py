"""Round 18: the P3 gadgets and the superweapons (see ArtSource/DESIGN_NOTES.md, round 18).

Gadgets (Docs/STORY.md, continents 2, 4 and 5):
- Decoy Beacon   (C2 L13: lures enemies for a while, draws diggers up): a seismic thumper on a staked tripod with a
                  rotating lure head. Sockets `Pulse` (ground ripple), `Lure`.
- Barricade      (C4 L4: blocks a stretch of road for a while; enemies must break through): five hinged ceramic
                  slabs on a sill across the road, on bones `Slab_1..5`. Sockets `Spark_1..3`, `Smoke`.
- Spike Strip    (C5 L3: a stretch of road with spikes, damage and slow): three hinged plates whose spike rows
                  punch up (bones `Spikes_1..3`). Socket `Hit`.
- Rollback Field (C5 L8: pushes enemies back along the road): a squat emitter with a repulsor dish aimed up the
                  road and a counter-turning chevron ring. Sockets `Push`, `Field`.
Superweapons (C5 L5/L10/L13; small objects, the effect is VFX):
- Payload bomb, Overcharged Crystal (a containment rig round an unstable Crysium spire), Toxin bomb.

Same workshop as rounds 9 and 11: tower_kit pieces, atlas and self-bake, one 512 map set (+ Low 256) per asset;
the Gadget base, build and export from build_gadgets.py (imported, unchanged).
Out: `Exports/Gadget_<Name>/` and `Exports/Super_<Name>/` (FBX with LOD0 + LOD1, clips, sockets; maps; report).

Run: blender --background --factory-startup --python Tools/Blender/build_gadgets3.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Decoy,Barricade,SpikeStrip,Rollback,PayloadBomb,OverchargedCrystal,ToxinBomb]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_gadgets as bg  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import UP, XAX, V  # noqa: E402

YAX = V(0, 1, 0)
FWD = V(0, -1, 0)
PLAIN = tk.WHITE.but(panel=0, bolts=False)
GL = {
    "lure": (1.0, 0.74, 0.2),        # signal yellow: the decoy must shout
    "gate": bk.GLOW["gate"],
    "cyan": bk.GLOW["cyan"],
    "rewind": (0.2, 1.0, 0.8),       # a cool teal, apart from the Chrono's pale blue and the Acid's green
    "amber": bk.GLOW["amber"],
    "crysium": (0.9, 0.58, 1.0),     # overcharged Crysium: hotter than the tamed lilac-white
    "toxin": (0.3, 1.0, 0.38),       # the Acid Sprayer's chemical green
}


def G(name, strength=1.0, lens=0.0):
    return tk.glow(GL[name], strength, lens)


def sphere_prof(r, n=6, zc=0.0):
    return [(0, zc - r)] + [(r * math.sin(math.pi * i / n), zc - r * math.cos(math.pi * i / n)) for i in range(1, n)] + [(0, zc + r)]


def twisted_spire(radii, heights, sides=5, twist=18.0, lean=(0.0, 0.0)):
    """A crystal spire that is not a quartz prism: a pentagonal section that twists `twist` degrees per ring, bulges
    and tapers along `radii`, leans by `lean` (x, y per unit height), and ends in a point."""
    bm = bmesh.new()
    rings = []
    for i, (r, z) in enumerate(zip(radii, heights)):
        a0 = math.radians(twist * i)
        off = (lean[0] * z, lean[1] * z)
        rings.append([bm.verts.new((off[0] + math.cos(a0 + 2 * math.pi * s / sides) * r, off[1] + math.sin(a0 + 2 * math.pi * s / sides) * r, z))
                      for s in range(sides)])
    for a, b in zip(rings, rings[1:]):
        for s in range(sides):
            s2 = (s + 1) % sides
            bm.faces.new((a[s], a[s2], b[s2], b[s]))
    bm.faces.new(list(reversed(rings[0])))
    top = heights[-1] + (heights[-1] - heights[-2]) * 0.9
    tip = bm.verts.new((lean[0] * top, lean[1] * top, top))
    for s in range(sides):
        bm.faces.new((rings[-1][s], rings[-1][(s + 1) % sides], tip))
    return bm


class Gadget3(bg.Gadget):
    prefix = "Gadget_"

    def kit(self):
        k = tk.Kit(self.prefix + self.name, ["Main"])
        self.pieces(k)
        return k


# -------------------------------------------------------------------------------------------------------------
# Decoy Beacon
# -------------------------------------------------------------------------------------------------------------

class Decoy(Gadget3):
    """A seismic thumper staked into the road: a white hammer weight rides two guide rods and pounds an anvil plate
    (it draws diggers up), under a mast with a rotating lure head of three horn emitters."""
    name = "Decoy"
    HAMMER_REST = 0.34

    def pieces(self, k):
        on = ["Main"]
        for i in range(3):
            a = math.pi / 2 + i * 2 * math.pi / 3
            d = V(math.cos(a), math.sin(a), 0)
            p0, p1 = d * 0.08 + V(0, 0, 0.16), d * 0.27 + V(0, 0, 0.03)
            k.block("Leg", (0.04, 0.03, (p1 - p0).length), tk.frame((p0 + p1) / 2, z=p1 - p0, y=UP), tk.GUNMETAL, "Root", on, taper=(0.8, 0.9), bevel=0.004)
            k.block("LegStripe", (0.042, 0.032, 0.03), tk.frame(p0 + (p1 - p0) * 0.55, z=p1 - p0, y=UP), tk.HAZARD, "Root", on, bevel=0.0, lod=False)
            k.cyl("Stake", p1 + V(0, 0, 0.02), p1 - V(0, 0, 0.05), 0.02, 0.004, look=tk.DARK, bone="Root", on=on, sides=6, bevel=0.0)
        # Anvil plate and hub drum.
        k.lathe("Anvil", [(0, 0), (0.15, 0), (0.15, 0.03), (0.12, 0.05), (0, 0.05)], tk.frame(), tk.GUNMETAL, "Root", on, sides=8, start=math.pi / 8)
        k.lathe("AnvilRing", [(0, 0.012), (0.155, 0.012), (0.155, 0.026), (0, 0.026)], tk.frame(), tk.HAZARD, "Root", on, sides=8, start=math.pi / 8,
                bevel=0.002)
        k.torus("PulseRing", V(0, 0, 0.054), UP, 0.1, 0.008, G("lure", 1.5), "Root", on, seg=16, sides=3)
        k.lathe("Housing", [(0, 0.05), (0.06, 0.05), (0.055, 0.14), (0, 0.14)], tk.frame(), tk.BLUE.but(panel=0, bolts=False), "Root", on, sides=10)
        # Guide rods and the crosshead at the top.
        for s in (-1, 1):
            k.cyl("Rod", V(s * 0.095, 0, 0.05), V(s * 0.095, 0, 0.56), 0.012, look=tk.STEEL, bone="Root", on=on, sides=8, bevel=0.0)
        k.block("Crosshead", (0.24, 0.06, 0.04), tk.frame((0, 0, 0.57)), tk.GUNMETAL, "Root", on, bevel=0.006)
        # The hammer weight: white, blue band, hazard face towards the camera, a glow strip that flashes on impact.
        hz = self.HAMMER_REST
        k.block("Hammer", (0.17, 0.12, 0.13), tk.frame((0, 0, hz)), tk.WHITE.but(panel=0.06), "Hammer", on, bevel=0.01)
        k.block("HammerBand", (0.175, 0.125, 0.03), tk.frame((0, 0, hz - 0.035)), tk.BLUE.but(panel=0), "Hammer", on, bevel=0.003)
        k.block("HammerHazard", (0.14, 0.008, 0.04), tk.frame((0, 0.063, hz + 0.03)), tk.HAZARD, "Hammer", on, bevel=0.0)
        k.block("HammerGlow", (0.14, 0.13, 0.012), tk.frame((0, 0, hz - 0.064)), G("lure", 1.4), "Hammer", on, bevel=0.0)
        for s in (-1, 1):
            k.cyl("Sleeve", V(s * 0.095, 0, hz - 0.05), V(s * 0.095, 0, hz + 0.05), 0.02, look=tk.DARK, bone="Hammer", on=on, sides=8, bevel=0.0)
        # Mast and lure head (spins on `Head`).
        k.cyl("Mast", V(0, 0, 0.59), V(0, 0, 0.74), 0.018, 0.014, look=tk.GUNMETAL, bone="Root", on=on, sides=8)
        hc = V(0, 0, 0.8)
        k.lathe("HeadBase", [(0, 0.735), (0.05, 0.735), (0.055, 0.755), (0, 0.76)], tk.frame(), PLAIN, "Head", on, sides=10)
        k.lathe("Core", sphere_prof(0.045, 5), tk.frame(hc), G("lure", 1.3, lens=0.045), "Head", on, sides=10, bevel=0.0)
        for i in range(3):
            a = 2 * math.pi * i / 3
            d = V(math.cos(a), math.sin(a), 0)
            k.lathe("Horn", [(0, 0), (0.018, 0), (0.042, 0.085), (0.037, 0.09), (0, 0.07)], tk.frame(hc + d * 0.03, z=d), tk.WHITE.but(panel=0), "Head",
                    on, sides=8, bevel=0.0)
            k.cyl("HornGlow", hc + d * 0.11, hc + d * 0.114, 0.034, look=G("lure", 1.7), bone="Head", on=on, sides=8, bevel=0.0)
        k.torus("Cage", hc, UP, 0.056, 0.004, tk.GUNMETAL, "Head", on, seg=12, sides=3, lod=False)
        k.cyl("Aerial", hc + UP * 0.035, hc + UP * 0.11, 0.004, look=tk.GUNMETAL, bone="Head", on=on, sides=5, bevel=0.0, lod=False)
        k.cyl("AerialTip", hc + UP * 0.11, hc + UP * 0.118, 0.009, look=G("lure", 2.0), bone="Head", on=on, sides=6, bevel=0.0, lod=False)
        self.extra = [("Hammer", V(0, 0, hz), UP, "Root", True), ("Head", V(0, 0, 0.74), UP, "Root", True),
                      ("Pulse", V(0, 0, 0.01), UP, "Root", False), ("Lure", hc, UP, "Head", False)]

    def thump(self, u):
        """Hammer height offset over one stroke (u 0..1): a slow lift, a hold, a fast drop, a small rebound."""
        if u < 0.5:
            return 0.1 * bk.ease(u / 0.5)
        if u < 0.62:
            return 0.1
        if u < 0.7:
            return 0.1 - 0.235 * ((u - 0.62) / 0.08) ** 2 * (0.1 / 0.1)
        if u < 0.8:
            return -0.135 + 0.02 * math.sin((u - 0.7) / 0.1 * math.pi)
        return -0.135 * (1 - bk.ease((u - 0.8) / 0.2))

    def clips(self, arm):
        def deploy(t, f):
            e = bk.ease(t * 1.3)
            return {"Head": {"loc": V(0, 0, -0.12 * (1 - e)), "scale": max(0.01, bk.pop(t, 0.35, 0.4))},
                    "Hammer": {"loc": V(0, 0, -0.135 * (1 - bk.ease(t * 2)))}}

        def idle(t, f):
            u = (t * 2) % 1.0
            z = self.thump(u)
            hit = 0.68 < u < 0.78
            return {"Hammer": {"loc": V(0, 0, z)}, "Head": {"rot": (UP, 2 * math.pi * t), "scale": 1.12 if hit else 1.0}}

        def pulse(t, f):
            return {"Head": {"rot": (UP, 4 * math.pi * t), "scale": 1.0 + 0.3 * math.sin(math.pi * t)},
                    "Hammer": {"loc": V(0, 0, self.thump(t))}}
        return [bk.clip(arm, "Deploy", 24, deploy), bk.clip(arm, "Idle", 60, idle, loop=True), bk.clip(arm, "Pulse", 20, pulse)]


# -------------------------------------------------------------------------------------------------------------
# Barricade
# -------------------------------------------------------------------------------------------------------------

class Barricade(Gadget3):
    """Five ceramic slabs on hinges across the road (1.9 wide), braced from behind, a lit edge on top. They fold up
    from the sill on Deploy, shudder on Hit and fall out of the line on Break."""
    name = "Barricade"
    N = 5
    W = 1.9
    TILT = 14.0

    def slab_x(self, i):
        return (i - (self.N - 1) / 2) * (self.W / self.N)

    def pieces(self, k):
        on = ["Main"]
        k.block("Sill", (self.W, 0.2, 0.05), tk.frame((0, 0.02, 0.025)), tk.GUNMETAL, "Root", on, bevel=0.008)
        k.block("SillHazard", (self.W - 0.06, 0.008, 0.026), tk.frame((0, -0.081, 0.028)), tk.HAZARD, "Root", on, bevel=0.0)
        k.block("SillLight", (self.W - 0.2, 0.008, 0.01), tk.frame((0, 0.121, 0.03)), G("gate", 1.4), "Root", on, bevel=0.0, lod=False)
        for s in (-1, 1):
            k.block("EndPost", (0.07, 0.22, 0.2), tk.frame((s * (self.W / 2 + 0.02), 0.02, 0.1)), tk.BLUE.but(panel=0, bolts=False), "Root", on, taper=(0.8, 0.8),
                    bevel=0.008)
            k.lathe("EndBeacon", [(0, 0.2), (0.025, 0.2), (0.022, 0.23), (0, 0.235)], tk.frame((s * (self.W / 2 + 0.02), 0.02, 0)), G("gate", 1.8), "Root",
                    on, sides=8, bevel=0.0)
            for y in (-0.08, 0.12):
                k.block("Claw", (0.05, 0.08, 0.03), tk.frame((s * (self.W / 2 - 0.04), y, 0.008), rot=(0, 0, 0)), tk.DARK, "Root", on, taper=(0.5, 0.7),
                        bevel=0.003)
        sw = self.W / self.N - 0.035
        h = 0.48
        tilt = self.TILT
        up = V(0, -math.sin(math.radians(tilt)), math.cos(math.radians(tilt)))     # slab's up axis (leans to the enemy side)
        face = V(0, -math.cos(math.radians(tilt)), -math.sin(math.radians(tilt)))  # slab's front normal (towards the enemies)
        self.extra = []
        for i in range(self.N):
            x = self.slab_x(i)
            name = f"Slab_{i + 1}"
            hinge = V(x, -0.02, 0.05)

            def L(dz, dface, x=x, hinge=hinge):
                return hinge + up * dz + face * dface

            def F(c):
                return tk.frame(c, z=face, y=up)
            k.block("Slab", (sw, h, 0.05), F(L(h / 2, 0)), tk.WHITE.but(panel=0.09), name, on, bevel=0.01)
            k.block("SlabBand", (sw + 0.004, 0.07, 0.054), F(L(0.07, 0)), tk.BLUE.but(panel=0), name, on, bevel=0.004)
            k.block("SlabHazard", (sw * 0.92, 0.07, 0.008), F(L(h - 0.075, 0.027)), tk.HAZARD, name, on, bevel=0.0)
            k.block("SlabHazardBack", (sw * 0.92, 0.05, 0.008), F(L(h - 0.07, -0.027)), tk.HAZARD, name, on, bevel=0.0)
            k.block("SlabTop", (sw * 0.9, 0.016, 0.034), F(L(h + 0.005, 0)), G("gate", 1.9), name, on, bevel=0.0)
            k.block("SlabBoss", (0.06, 0.06, 0.012), F(L(h * 0.55, 0.03)), tk.GUNMETAL, name, on, bevel=0.004, lod=False)
            nrm = up
            # Brace from the slab's back down to the sill (on the slab bone: it folds with the slab).
            b0, b1 = L(h * 0.62, -0.03), V(x, 0.11, 0.05)
            k.block("Brace", (0.03, 0.025, (b1 - b0).length), tk.frame((b0 + b1) / 2, z=b1 - b0, y=YAX), tk.GUNMETAL, name, on, bevel=0.003)
            k.cyl("Hinge", hinge - V(sw * 0.4, 0, 0), hinge + V(sw * 0.4, 0, 0), 0.018, look=tk.DARK, bone="Root", on=on, sides=8, bevel=0.0)
            self.extra.append((name, hinge, nrm, "Root", True))
        self.extra += [("Spark_1", V(self.slab_x(1), -0.06, 0.32), FWD, "Root", False), ("Spark_2", V(self.slab_x(2), -0.06, 0.25), FWD, "Root", False),
                       ("Spark_3", V(self.slab_x(3), -0.06, 0.34), FWD, "Root", False), ("Smoke", V(0, 0.0, 0.3), UP, "Root", False)]

    def clips(self, arm):
        def deploy(t, f):
            out = {}
            for i in range(self.N):
                u = bk.pop(t, 0.08 + i * 0.12, 0.3, 0.12)
                out[f"Slab_{i + 1}"] = {"rot": (XAX, math.radians(-90) * (1 - u))}
            return out

        def hit(t, f):
            kk = math.sin(t * math.pi * 3) * (1 - t)
            return {f"Slab_{i + 1}": {"rot": (XAX, math.radians(4) * kk * (1 if i % 2 else -0.7))} for i in range(self.N)}

        def brk(t, f):
            out = {}
            falls = [(-70, 0.0), (55, 0.15), (-85, 0.05), (75, 0.2), (-60, 0.1)]
            for i, (ang, st) in enumerate(falls):
                u = bk.ease((t - st) / 0.6)
                axis = V(1, 0, 0.25 if i % 2 else -0.3).normalized()
                out[f"Slab_{i + 1}"] = {"rot": (axis, math.radians(ang) * u),
                                        "scale": 1.0 - 0.15 * u}
            return out
        return [bk.clip(arm, "Deploy", 30, deploy), bk.clip(arm, "Hit", 8, hit), bk.clip(arm, "Break", 24, brk)]


# -------------------------------------------------------------------------------------------------------------
# Spike Strip
# -------------------------------------------------------------------------------------------------------------

class SpikeStrip(Gadget3):
    """Three hinged plates across the road (1.8 x 0.42), each with two rows of steel spikes on its own bone; the
    spikes stand half out at rest and punch up on Trigger."""
    name = "SpikeStrip"
    SEG = 3
    W = 1.8
    D = 0.42

    def pieces(self, k):
        on = ["Main"]
        sw = self.W / self.SEG
        self.extra = []
        for j in range(self.SEG):
            x = (j - (self.SEG - 1) / 2) * sw
            k.block("Plate", (sw - 0.03, self.D, 0.04), tk.frame((x, 0, 0.02)), tk.GUNMETAL, "Root", on, taper=(0.98, 0.9), bevel=0.006)
            k.block("Deck", (sw - 0.08, self.D - 0.1, 0.012), tk.frame((x, 0, 0.044)), tk.DARK.but(panel=0.1), "Root", on, bevel=0.003)
            for s in (-1, 1):
                k.block("Edge", (sw - 0.04, 0.03, 0.022), tk.frame((x, s * (self.D / 2 - 0.02), 0.03)), tk.HAZARD, "Root", on, bevel=0.002)
            k.block("Light", (sw - 0.12, 0.01, 0.008), tk.frame((x, -self.D / 2 + 0.04, 0.046)), G("cyan", 1.5), "Root", on, bevel=0.0, lod=False)
            name = f"Spikes_{j + 1}"
            for row, y in enumerate((-0.07, 0.07)):
                for i in range(6):
                    px = x + (i - 2.5) * (sw - 0.1) / 6 + (0.02 if row else -0.02)
                    p0 = V(px, y, 0.03)
                    lean = V(0, -0.012, 0)
                    k.cyl("Spike", p0, p0 + UP * 0.12 + lean, 0.02, 0.0015, look=tk.STEEL.but(paint=(0.62, 0.66, 0.72), rough=0.3), bone=name, on=on,
                          sides=4, bevel=0.0)
                k.block("SpikeBar", (sw - 0.1, 0.03, 0.02), tk.frame((x, y, 0.035)), tk.DARK, name, on, bevel=0.0)
            self.extra.append((name, V(x, 0, 0.03), UP, "Root", True))
            if j < self.SEG - 1:
                hx = x + sw / 2
                k.cyl("Hinge", V(hx, -self.D / 2 + 0.03, 0.025), V(hx, self.D / 2 - 0.03, 0.025), 0.016, look=tk.DARK, bone="Root", on=on, sides=8, bevel=0.0)
        for s in (-1, 1):
            k.block("EndCap", (0.06, self.D + 0.02, 0.06), tk.frame((s * (self.W / 2 + 0.02), 0, 0.03)), tk.YELLOW, "Root", on, taper=(0.6, 0.95), bevel=0.006)
        self.extra.append(("Hit", V(0, 0, 0.06), UP, "Root", False))

    def clips(self, arm):
        rest = -0.035

        def deploy(t, f):
            return {f"Spikes_{j + 1}": {"loc": V(0, 0, -0.12 + (0.12 + rest) * bk.ease((t - j * 0.15) / 0.6))} for j in range(self.SEG)}

        def idle(t, f):
            return {f"Spikes_{j + 1}": {"loc": V(0, 0, rest + 0.003 * math.sin(2 * math.pi * t + j))} for j in range(self.SEG)}

        def trigger(t, f):
            kk = bt_kick(f, 10)
            return {f"Spikes_{j + 1}": {"loc": V(0, 0, rest + 0.045 * kk)} for j in range(self.SEG)}
        return [bk.clip(arm, "Deploy", 24, deploy), bk.clip(arm, "Idle", 60, idle, loop=True), bk.clip(arm, "Trigger", 10, trigger)]


def bt_kick(f, n=10):
    if f <= 0:
        return 0.0
    if f == 1:
        return 1.0
    return max(0.0, 1.0 - (f - 1) / (n - 1)) ** 2


# -------------------------------------------------------------------------------------------------------------
# Rollback Field emitter
# -------------------------------------------------------------------------------------------------------------

class Rollback(Gadget3):
    """A squat hexagonal emitter beside the road with a repulsor dish aimed up the road (local -Y, towards the
    enemies) and a ring of backward-pointing chevrons that turns against the march (the rewind)."""
    name = "Rollback"
    DIR = V(0, -0.94, 0.34).normalized()     # the dish looks up the road and a little up

    def pieces(self, k):
        on = ["Main"]
        I = tk.frame()
        k.lathe("Foot", [(0, 0), (0.24, 0), (0.24, 0.035), (0.225, 0.05), (0, 0.05)], I, tk.GUNMETAL, "Root", on, sides=6, start=0.0)
        k.lathe("FootHazard", [(0, 0.012), (0.245, 0.012), (0.245, 0.032), (0, 0.032)], I, tk.HAZARD, "Root", on, sides=6, start=0.0, bevel=0.002)
        k.lathe("Body", [(0, 0.05), (0.2, 0.05), (0.17, 0.15), (0, 0.15)], I, tk.BLUE.but(panel=0.07), "Root", on, sides=6, start=0.0)
        k.lathe("Deck", [(0, 0.148), (0.17, 0.148), (0.15, 0.165), (0, 0.165)], I, PLAIN, "Root", on, sides=6, start=0.0)
        k.torus("FieldRing", V(0, 0, 0.152), UP, 0.185, 0.008, G("rewind", 1.4), "Root", on, seg=18, sides=3)
        # Two capacitor bottles on the back (camera side).
        for s in (-1, 1):
            b = V(s * 0.08, 0.1, 0.165)
            k.cyl("Bottle", b, b + UP * 0.12, 0.026, look=tk.WHITE.but(panel=0, bolts=False, rings=0.05), bone="Root", on=on, sides=10)
            k.lathe("BottleCap", [(0, 0), (0.026, 0), (0.018, 0.02), (0, 0.024)], tk.frame(b + UP * 0.12), G("rewind", 1.6), "Root", on, sides=10, bevel=0.0)
        # The yoke and the dish.
        piv = V(0, -0.02, 0.33)
        for s in (-1, 1):
            k.block("Arm", (0.04, 0.09, 0.2), tk.frame((s * 0.15, -0.02, 0.25)), tk.BLUE.but(panel=0.05), "Root", on, taper=(1.0, 0.8))
            k.cyl("Pivot", V(s * 0.13, -0.02, 0.33), V(s * 0.175, -0.02, 0.33), 0.026, look=tk.GUNMETAL, bone="Dish", on=on, sides=10)
        d = self.DIR
        k.lathe("DishBack", [(0, -0.05), (0.06, -0.05), (0.13, -0.01), (0.135, 0.01), (0, 0.0)], tk.frame(piv, z=d), tk.GUNMETAL, "Dish", on, sides=16)
        k.lathe("Dish", [(0, -0.01), (0.13, 0.01), (0.135, 0.025), (0.12, 0.028), (0.04, 0.0), (0, -0.002)], tk.frame(piv, z=d), PLAIN, "Dish", on,
                sides=16, bevel=0.0)
        for r in (0.065, 0.1):
            k.torus("DishRing", piv + d * (0.006 + r * 0.12), d, r, 0.006, G("rewind", 1.3), "Dish", on, seg=16, sides=3)
        k.cyl("Emitter", piv, piv + d * 0.07, 0.028, 0.014, look=tk.DARK, bone="Dish", on=on, sides=8, bevel=0.0)
        k.lathe("EmitterTip", sphere_prof(0.016, 3), tk.frame(piv + d * 0.075), G("rewind", 1.8, lens=0.016), "Dish", on, sides=8, bevel=0.0)
        # The chevron ring: eight chevrons round the rim pointing backwards (the rewind sign), on `Ring`.
        u = d.cross(UP).normalized()
        w = d.cross(u).normalized()
        rc = piv + d * 0.03
        for i in range(8):
            a = 2 * math.pi * i / 8
            radial = u * math.cos(a) + w * math.sin(a)
            tangent = d.cross(radial).normalized()
            c = rc + radial * 0.17
            for sgn in (-1, 1):
                arm_dir = (-tangent * 0.7 + radial * sgn * 0.7).normalized()
                k.block("Chevron", (0.014, 0.012, 0.045), tk.frame(c + arm_dir * 0.016, z=arm_dir, y=d), G("rewind", 1.2) if i % 2 == 0 else PLAIN, "Ring",
                        on, bevel=0.0)
        k.torus("RingBand", rc, d, 0.15, 0.007, tk.GUNMETAL, "Ring", on, seg=20, sides=3)
        self.extra = [("Dish", piv, XAX, "Root", True), ("Ring", rc, d, "Dish", True),
                      ("Push", piv + d * 0.09, d, "Dish", False), ("Field", V(0, -0.45, 0.01), UP, "Root", False)]

    def clips(self, arm):
        d = self.DIR

        def deploy(t, f):
            e = bk.ease(t * 1.4)
            return {"Dish": {"rot": (XAX, math.radians(-60) * (1 - e))}, "Ring": {"scale": max(0.01, bk.pop(t, 0.45, 0.4))}}

        def idle(t, f):
            return {"Ring": {"rot": (d, -2 * math.pi * t)}, "Dish": {"rot": (XAX, math.radians(1.5) * math.sin(2 * math.pi * t))}}

        def pulse(t, f):
            kk = bt_kick(f, 12)
            return {"Dish": {"loc": -d * 0.03 * kk}, "Ring": {"rot": (d, -1.2 * t), "scale": 1.0 + 0.25 * kk}}
        return [bk.clip(arm, "Deploy", 30, deploy), bk.clip(arm, "Idle", 90, idle, loop=True), bk.clip(arm, "Pulse", 12, pulse)]


# -------------------------------------------------------------------------------------------------------------
# Superweapons
# -------------------------------------------------------------------------------------------------------------

class PayloadBomb(Gadget3):
    """A fat colony bomb, nose down (pivot on the nose tip, the impact point): white nose with a hazard ring, blue
    body band, a ring of amber arming windows, gunmetal tail cone and four fins in a ring shroud (on `Fins`)."""
    prefix = "Super_"
    name = "PayloadBomb"
    R = 0.16

    def pieces(self, k):
        on = ["Main"]
        R = self.R
        k.lathe("Nose", [(0, 0), (0.05, 0.012), (0.11, 0.06), (0.15, 0.13), (R, 0.2), (0, 0.2)], tk.frame(), PLAIN, "Body", on, sides=16, bevel=0.0)
        k.lathe("NoseCap", [(0, -0.004), (0.045, 0.01), (0.05, 0.018), (0, 0.02)], tk.frame(), tk.DARK, "Body", on, sides=10, bevel=0.0)
        k.lathe("NoseHazard", [(0, 0.15), (R + 0.003, 0.15), (R + 0.003, 0.19), (0, 0.19)], tk.frame(), tk.HAZARD, "Body", on, sides=16, bevel=0.002)
        k.lathe("Body", [(0, 0.2), (R, 0.2), (R, 0.58), (0.13, 0.66), (0, 0.66)], tk.frame(), tk.WHITE.but(panel=0.08, rings=0.0), "Body", on, sides=16)
        k.lathe("Band", [(0, 0.27), (R + 0.004, 0.27), (R + 0.004, 0.36), (0, 0.36)], tk.frame(), tk.BLUE.but(panel=0), "Body", on, sides=16, bevel=0.002)
        for i in range(6):
            a = 2 * math.pi * i / 6
            d = V(math.cos(a), math.sin(a), 0)
            k.block("Window", (0.05, 0.03, 0.01), tk.frame(d * (R + 0.003) + UP * 0.46, z=d, y=UP), G("amber", 1.6), "Body", on, bevel=0.0)
        k.lathe("Tail", [(0, 0.64), (0.13, 0.64), (0.07, 0.86), (0.05, 0.9), (0, 0.9)], tk.frame(), tk.GUNMETAL, "Body", on, sides=12)
        k.cyl("TailLight", V(0, 0, 0.9), V(0, 0, 0.912), 0.03, look=G("amber", 2.0), bone="Body", on=on, sides=8, bevel=0.0)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            d = V(math.cos(a), math.sin(a), 0)
            k.block("Fin", (0.012, 0.15, 0.2), tk.frame(d * 0.13 + UP * 0.8, z=UP, y=d), PLAIN, "Fins", on, taper=(1.0, 0.7), bevel=0.003)
            k.block("FinStripe", (0.014, 0.04, 0.2), tk.frame(d * 0.185 + UP * 0.8, z=UP, y=d), tk.HAZARD, "Fins", on, bevel=0.0, lod=False)
        k.torus("Shroud", UP * 0.87, UP, 0.2, 0.014, tk.GUNMETAL, "Fins", on, seg=20, sides=4)
        self.extra = [("Body", V(0, 0, 0), UP, "Root", True), ("Fins", UP * 0.8, UP, "Body", True),
                      ("Blast", V(0, 0, 0.0), -UP, "Root", False), ("Trail", UP * 0.92, UP, "Body", False)]

    def clips(self, arm):
        def fall(t, f):
            return {"Fins": {"rot": (UP, 2 * math.pi * t)}, "Body": {"rot": (XAX, math.radians(2.5) * math.sin(2 * math.pi * t))}}

        def arm_clip(t, f):
            return {"Fins": {"rot": (UP, 4 * math.pi * t)}, "Body": {"scale": 1.0 + 0.04 * math.sin(math.pi * t)}}
        return [bk.clip(arm, "Fall", 30, fall, loop=True), bk.clip(arm, "Arm", 12, arm_clip)]


class OverchargedCrystal(Gadget3):
    """A containment rig (gunmetal tripod, blue drum, three white clamp arms) holding a twisted, overcharged Crysium
    spire with fused side shards, and three splinters orbiting it. Our cage is cool; the crystal is the weapon."""
    prefix = "Super_"
    name = "OverchargedCrystal"
    CZ = 0.26

    def pieces(self, k):
        on = ["Main"]
        for i in range(3):
            a = math.pi / 2 + i * 2 * math.pi / 3
            d = V(math.cos(a), math.sin(a), 0)
            k.block("Leg", (0.06, 0.05, 0.2), tk.frame(d * 0.2 + V(0, 0, 0.06), z=d * 0.6 + UP, y=UP), tk.GUNMETAL, "Root", on, taper=(0.8, 0.8), bevel=0.006)
            k.block("Foot", (0.09, 0.06, 0.03), tk.frame(d * 0.27 + V(0, 0, 0.015), rot=(0, 0, math.degrees(a) + 90)), tk.HAZARD, "Root", on, bevel=0.004)
        k.lathe("Drum", [(0, 0.08), (0.16, 0.08), (0.17, 0.12), (0.15, 0.2), (0, 0.2)], tk.frame(), tk.BLUE.but(panel=0.07), "Root", on, sides=10)
        k.lathe("DrumDeck", [(0, 0.198), (0.15, 0.198), (0.13, 0.22), (0, 0.22)], tk.frame(), tk.GUNMETAL, "Root", on, sides=10)
        k.torus("WarnRing", V(0, 0, 0.16), UP, 0.168, 0.009, tk.HAZARD, "Root", on, seg=18, sides=4)
        k.torus("Containment", V(0, 0, 0.222), UP, 0.11, 0.009, G("cyan", 1.4), "Root", on, seg=18, sides=3)
        # The crystal: a twisted pentagonal spire with three fused side shards, all on `Crystal`.
        cz = self.CZ
        k._add("Spire", lambda: twisted_spire([0.05, 0.075, 0.08, 0.065, 0.04], [0.0, 0.08, 0.2, 0.32, 0.42], sides=5, twist=22), 0.0,
               tk.frame((0, 0, cz - 0.04)), G("crysium", 1.0, lens=0.06), "Crystal", on)
        for i, (ang, lean, h) in enumerate(((20, 0.45, 0.24), (150, 0.55, 0.19), (265, 0.4, 0.21))):
            a = math.radians(ang)
            lx, ly = math.cos(a) * lean, math.sin(a) * lean
            k._add("SideShard", (lambda lx=lx, ly=ly, h=h: twisted_spire([0.03, 0.04, 0.03, 0.016], [0.0, h * 0.3, h * 0.7, h], sides=4, twist=-30,
                                                                          lean=(lx, ly))), 0.0,
                   tk.frame((math.cos(a) * 0.025, math.sin(a) * 0.025, cz - 0.02)), G("crysium", 0.9, lens=0.03), "Crystal", on)
        # Clamp arms on their own bones: hinge at the drum deck, a white arm, a dark jaw with a cool pad on the crystal.
        self.clamps = []
        for i in range(3):
            a = math.pi / 6 + i * 2 * math.pi / 3
            d = V(math.cos(a), math.sin(a), 0)
            hinge = d * 0.13 + V(0, 0, 0.22)
            elbow = d * 0.17 + V(0, 0, 0.42)
            jaw = d * 0.1 + V(0, 0, 0.5)
            name = f"Clamp_{i + 1}"
            k.cyl("ClampHinge", hinge - d.cross(UP) * 0.03, hinge + d.cross(UP) * 0.03, 0.022, look=tk.DARK, bone="Root", on=on, sides=8, bevel=0.0)
            k.block("ClampArm", (0.04, 0.03, (elbow - hinge).length), tk.frame((hinge + elbow) / 2, z=elbow - hinge, y=d), PLAIN, name, on, bevel=0.005)
            k.block("ClampStripe", (0.042, 0.032, 0.03), tk.frame(hinge + (elbow - hinge) * 0.3, z=elbow - hinge, y=d), tk.HAZARD, name, on, bevel=0.0,
                    lod=False)
            k.block("ClampJaw", (0.05, 0.03, (jaw - elbow).length + 0.02), tk.frame((elbow + jaw) / 2, z=jaw - elbow, y=d), tk.GUNMETAL, name, on, bevel=0.004)
            k.block("JawPad", (0.04, 0.012, 0.03), tk.frame(jaw - d * 0.012, z=-d, y=UP), G("cyan", 1.6), name, on, bevel=0.0)
            self.clamps.append((name, hinge, d))
        # Orbiting splinters (on `Orbit`).
        for i in range(3):
            a = 2 * math.pi * i / 3 + 0.5
            c = V(math.cos(a) * 0.2, math.sin(a) * 0.2, cz + 0.2 + 0.04 * i)
            k._add("Splinter", (lambda: twisted_spire([0.016, 0.02, 0.008], [0.0, 0.025, 0.05], sides=4, twist=40)), 0.0,
                   tk.frame(c, rot=(30 * i - 20, 25, 40 * i)), G("crysium", 1.2), "Orbit", on, lod=i < 2)
        self.extra = [("Crystal", V(0, 0, cz - 0.04), UP, "Root", True), ("Orbit", V(0, 0, cz + 0.2), UP, "Crystal", True)]
        self.extra += [(n, h, UP, "Root", True) for n, h, d in self.clamps]
        self.extra += [("Core", V(0, 0, cz + 0.15), UP, "Crystal", False), ("Blast", V(0, 0, 0.3), UP, "Root", False)]
        self.extra += [(f"Arc_{i + 1}", h + d * 0.04 + V(0, 0, 0.29), -d, n, False) for i, (n, h, d) in enumerate(self.clamps)]

    def clips(self, arm):
        def clamp_rot(name, h, d, amount):
            return {"rot": (d.cross(UP).normalized(), amount)}

        def deploy(t, f):
            e = bk.ease(t * 1.3)
            out = {"Crystal": {"loc": V(0, 0, -0.15 * (1 - e)), "scale": max(0.01, 0.6 + 0.4 * e)}, "Orbit": {"scale": max(0.01, bk.pop(t, 0.55, 0.35))}}
            for n, h, d in self.clamps:
                out[n] = clamp_rot(n, h, d, math.radians(-50) * (1 - bk.ease((t - 0.3) / 0.5)))
            return out

        def idle(t, f):
            j = 0.004 * math.sin(2 * math.pi * 7 * t)
            return {"Crystal": {"loc": V(j, -j * 0.6, 0.006 * math.sin(2 * math.pi * t)), "scale": 1.0 + 0.025 * math.sin(2 * math.pi * 2 * t)},
                    "Orbit": {"rot": (UP, 2 * math.pi * t)}}

        def overload(t, f):
            g = t * t
            j = 0.012 * g * math.sin(2 * math.pi * 11 * t)
            out = {"Crystal": {"loc": V(j, j * 0.7, 0.02 * g), "scale": 1.0 + 0.3 * g}, "Orbit": {"rot": (UP, 6 * math.pi * t), "scale": 1.0 + 0.8 * g}}
            for n, h, d in self.clamps:
                out[n] = clamp_rot(n, h, d, math.radians(-45) * bk.ease((t - 0.55) / 0.35))
            return out
        return [bk.clip(arm, "Deploy", 30, deploy), bk.clip(arm, "Idle", 60, idle, loop=True), bk.clip(arm, "Overload", 36, overload)]


class ToxinBomb(Gadget3):
    """A stubby chemical canister bomb, nose down (pivot on the nose tip): white shell, four sight windows of glowing
    toxin, four vent valves on bones that pop out when it bursts, a squat tail with four fins."""
    prefix = "Super_"
    name = "ToxinBomb"
    R = 0.13

    def pieces(self, k):
        on = ["Main"]
        R = self.R
        k.lathe("Nose", [(0, 0), (0.06, 0.008), (0.11, 0.04), (R, 0.08), (0, 0.08)], tk.frame(), tk.GUNMETAL, "Body", on, sides=14, bevel=0.0)
        k.lathe("Shell", [(0, 0.08), (R, 0.08), (R, 0.5), (0.1, 0.56), (0, 0.56)], tk.frame(), tk.WHITE.but(panel=0.08), "Body", on, sides=14)
        for z in (0.1, 0.44):
            k.lathe("Hazard", [(0, z), (R + 0.004, z), (R + 0.004, z + 0.035), (0, z + 0.035)], tk.frame(), tk.HAZARD, "Body", on, sides=14, bevel=0.002)
        k.torus("ToxinBand", UP * 0.27, UP, R + 0.004, 0.009, G("toxin", 1.3), "Body", on, seg=16, sides=3)
        self.valves = []
        for i in range(4):
            a = i * math.pi / 2
            d = V(math.cos(a), math.sin(a), 0)
            k.block("WindowFrame", (0.06, 0.012, 0.12), tk.frame(d * (R - 0.001) + UP * 0.34, z=d, y=UP), tk.DARK, "Body", on, bevel=0.002)
            k.block("Window", (0.04, 0.014, 0.1), tk.frame(d * (R + 0.001) + UP * 0.34, z=d, y=UP), G("toxin", 1.0), "Body", on, bevel=0.0)
            va = a + math.pi / 4
            dv = V(math.cos(va), math.sin(va), 0)
            base = dv * R + UP * 0.2
            name = f"Valve_{i + 1}"
            k.cyl("Valve", base - dv * 0.01, base + dv * 0.045, 0.022, 0.018, look=tk.GUNMETAL, bone=name, on=on, sides=8)
            k.cyl("ValveCap", base + dv * 0.045, base + dv * 0.055, 0.026, look=tk.YELLOW, bone=name, on=on, sides=8, bevel=0.0)
            self.valves.append((name, base, dv))
        k.lathe("Tail", [(0, 0.55), (0.1, 0.55), (0.06, 0.68), (0, 0.7)], tk.frame(), tk.GUNMETAL, "Body", on, sides=10)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            d = V(math.cos(a), math.sin(a), 0)
            k.block("Fin", (0.012, 0.12, 0.14), tk.frame(d * 0.11 + UP * 0.62, z=UP, y=d), tk.BLUE.but(panel=0), "Body", on, taper=(1.0, 0.6), bevel=0.003)
        k.cyl("TailLight", UP * 0.7, UP * 0.71, 0.02, look=G("toxin", 2.0), bone="Body", on=on, sides=8, bevel=0.0)
        self.extra = [("Body", V(0, 0, 0), UP, "Root", True)]
        self.extra += [(n, b, d, "Body", True) for n, b, d in self.valves]
        self.extra += [(f"Vent_{i + 1}", b + d * 0.06, d, n, False) for i, (n, b, d) in enumerate(self.valves)]
        self.extra += [("Cloud", UP * 0.3, UP, "Root", False), ("Blast", V(0, 0, 0), -UP, "Root", False)]

    def clips(self, arm):
        def fall(t, f):
            return {"Body": {"rot": (UP, 2 * math.pi * t)}}

        def vent(t, f):
            out = {"Body": {"loc": V(0.004 * math.sin(2 * math.pi * 6 * t), 0, 0)}}
            for i, (n, b, d) in enumerate(self.valves):
                u = bk.pop(t, 0.05 + 0.1 * i, 0.25, 0.3)
                out[n] = {"loc": d * 0.03 * u, "rot": (d, 3.0 * u)}
            return out
        return [bk.clip(arm, "Fall", 30, fall, loop=True), bk.clip(arm, "Vent", 16, vent)]


SPECS = {"Decoy": Decoy, "Barricade": Barricade, "SpikeStrip": SpikeStrip, "Rollback": Rollback, "PayloadBomb": PayloadBomb,
         "OverchargedCrystal": OverchargedCrystal, "ToxinBomb": ToxinBomb}
PIVOTS = {"Decoy": "centre on the ground (the anvil's centre)",
          "Barricade": "centre of the sill on the ground; spans 1.9 across local X (the road's width); the slabs lean to -Y (Unity +Z, the enemy side)",
          "SpikeStrip": "centre on the ground; spans 1.8 across X, 0.42 along Y",
          "Rollback": "centre on the ground; the dish looks along -Y (Unity +Z): face it up the road, towards the enemies",
          "PayloadBomb": "nose tip (the impact point); the bomb stands nose down along +Z",
          "OverchargedCrystal": "centre on the ground",
          "ToxinBomb": "nose tip (the impact point); nose down along +Z"}


def build(spec, bake):
    kit, mat = bg.build(spec, bake=bake)
    path = os.path.join(C.EXPORTS, kit.name, kit.name + "_report.json")
    with open(path, encoding="utf-8") as f:
        report = json.load(f)
    report["pivot"] = PIVOTS[spec.name]
    report["sockets"] = {n: [round(c, 3) for c in h] for n, h, d, p, df in spec.extra if not df}
    tk.write_json(path, report)
    return kit, mat


def place(spec, kit, mat, loc, yaw=0.0, label="P"):
    objs = bg.objects(spec, kit, mat, label=label, lod=False)
    objs[0].location = loc
    objs[0].rotation_euler.z = math.radians(yaw)
    return objs


def strip(arm, act, target, out, name, size, azimuth=30, elevation=24, count=5):
    arm.animation_data.action = act
    rig = tk.close_lights(0.6)
    cam = tk.closeup_camera(target, size, azimuth=azimuth, elevation=elevation, lens=50)
    n = int(act.frame_range[1])
    paths = []
    for f in [round(n * i / (count - 1)) for i in range(count)]:
        bpy.context.scene.frame_set(f)
        fp = os.path.join(out, f"_g3_{name}_{f}.png")
        C.render(cam, fp, 400, 300, bloom=False)
        paths.append(fp)
    C.contact_sheet(paths, os.path.join(out, f"g3_{name.lower()}_{act.name.lower()}_strip.png"), len(paths), 360)
    for fp in paths:
        os.remove(fp)
    bk.remove([cam] + rig)
    bk.rest_pose(arm)


# name: (location, yaw, close distance, close target height, strip clip index, strip size)
LAYOUT = {"Decoy": (V(-2.3, 0.35, 0), 0, 1.5, 0.4, 1, 1.6),
          "Barricade": (V(-0.6, 0.0, 0), 90, 2.6, 0.2, 0, 2.6),
          "SpikeStrip": (V(1.0, 0.0, 0), 90, 2.4, 0.05, 2, 2.2),
          "Rollback": (V(2.3, 1.25, 0), -90, 1.4, 0.25, 2, 1.4),
          "PayloadBomb": (V(-1.6, -0.3, 1.1), 0, 2.4, 0.45, 0, 2.2),
          "OverchargedCrystal": (V(3.4, -0.4, 0), 0, 1.5, 0.35, 2, 1.6),
          "ToxinBomb": (V(0.1, 0.4, 1.0), 0, 1.4, 0.35, 1, 1.4)}


def previews(built, out):
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    groups = {}
    for n, (spec, kit, mat) in built.items():
        loc, yaw = LAYOUT[n][0], LAYOUT[n][1]
        groups[n] = place(spec, kit, mat, loc, yaw=yaw)
        placed += groups[n]
    placed += bk.tower("Gun", "T1", (-1.4, 1.6, 0))
    placed += bk.tower("Flak", "T1", (0.6, 1.6, 0))
    sk = bk.skitters([(-3.2, -0.2, 0), (-1.2, 0.5, 0), (0.3, -0.5, 0), (2.8, 0.2, 0)], yaw=math.radians(90))
    bk.render_game(V(0.4, 0.3, 0.3), os.path.join(out, "gadgets3_game.png"), zoom=1.0)
    bk.render_game(V(0.4, 0.3, 0.4), os.path.join(out, "gadgets3_zoom.png"), zoom=2.4)
    bk.unstage(sk)

    def solo(name):
        for n, oo in groups.items():
            for o in oo:
                o.hide_render = n != name
        for o in placed:
            if not any(o in oo for oo in groups.values()):
                o.hide_render = True
    shots = []
    for n, oo in groups.items():
        solo(n)
        spec = built[n][0]
        loc, yaw, size, tz, si, ss = LAYOUT[n]
        arm = oo[0]
        t = arm.location + V(0, 0, tz)
        p = os.path.join(out, f"g3_{n.lower()}_close.png")
        bk.render_close(t, p, size, azimuth=-30 if n != "Rollback" else 200, elevation=28, light_scale=0.55)
        shots.append(p)
        acts = spec.clips(arm)
        strip(arm, acts[min(si, len(acts) - 1)], t, out, n, ss, azimuth=-20, elevation=16)
    C.contact_sheet(shots, os.path.join(out, "gadgets3_close_sheet.png"), 4, 480)
    bk.remove(placed + stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    names = only.split(",") if isinstance(only, str) else list(SPECS)
    C.preview_world()
    tk.metal_detail_map()
    built = {}
    for n in names:
        spec = SPECS[n]()
        kit, mat = build(spec, bake=not args.get("nobake"))
        built[n] = (spec, kit, mat)
    if not args.get("norender"):
        previews(built, out)
    C.save_blend("Gadgets3" if only is None else "Gadgets3_" + "_".join(names))


if __name__ == "__main__":
    main()
