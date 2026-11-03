"""Round 9: the road gate (Blockade) that the hero defends (see ArtSource/DESIGN_NOTES.md, round 9).

Three levels (G1 plain, G2 buttressed, G3 fortified), white hex armour panels on their own bones (damage hides them,
`Repair` pops them back in), a damage overlay mesh, a `Collapse` clip and a static wreck. Sized from the code: pylon
centres at x = +-1.025 (`TowerView` span 2.05) and the beam's top centre at 1.17 (`GateHeadHeight`), where the round-7
`Tower_GateGun` saddle sits on the `GunMount` socket.

Out: `Exports/Gate/Gate_G1..G3.fbx` (LOD0 + LOD1 + `_Damage` overlay, skinned to `Gate_Rig`, clips), `Gate_Wreck.fbx`,
one 1024 map set (+ Low 512), `Gate_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_gate.py -- [--preview <dir>] [--norender]
     [--nobake]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP, XAX  # noqa: E402

NAME = "Gate"
TIERS = ["G1", "G2", "G3"]
VARIANTS = TIERS + ["Damage", "Wreck"]
PX = 1.025            # pylon centre, half the 2.05 span
BEAM_TOP = 1.17       # GateHeadHeight: the Gate Gun's pivot
BEAM_D = 0.2          # the Gate Gun's saddle depth
COL_W, COL_D, COL_Z0, COL_Z1 = 0.3, 0.4, 0.1, 0.96
TAPER = (0.86, 0.88)
HEX_R = 0.074
HEX_ROWS = (0.47, 0.655, 0.84)
HERO_POST = 1.0       # one cell in front, on the enemy side (+Y here, Unity -Z)
DAMAGED_HEXES = ["Hex_L1", "Hex_L2", "Hex_L4", "Hex_L6", "Hex_L8", "Hex_R1", "Hex_R3", "Hex_R5", "Hex_R6", "Hex_R9"]
OUT = os.path.join(C.EXPORTS, NAME)


def blot(r, seed, stretch=1.0):
    """An irregular, roughly round outline for soot patches."""
    rng = np.random.default_rng(seed)
    pts = []
    for i in range(11):
        a = 2 * math.pi * i / 11
        rr = r * rng.uniform(0.72, 1.0)
        pts.append((math.cos(a) * rr * stretch, math.sin(a) * rr))
    return pts


def face_half(z, half, k):
    """Half width of the tapered column at height z (k = taper factor at the top)."""
    t = (z - COL_Z0) / (COL_Z1 - COL_Z0)
    return half * (1 + (k - 1) * t)


class Gate:
    def __init__(self):
        self.hexes = []       # (bone, centre, normal, parent)
        self.sparks = []      # (bone, head, parent)

    # ---------------------------------------------------------------------------------------------- pieces
    def kit(self):
        k = tk.Kit(NAME, VARIANTS)
        for s in (-1, 1):
            self.pylon(k, s)
        self.beam(k)
        self.tier2(k)
        self.tier3(k)
        self.damage(k)
        self.wreck(k)
        return k

    def pylon(self, k, s):
        bone = "Pylon_L" if s < 0 else "Pylon_R"
        cx = s * PX
        on = TIERS
        k.block("Foot", (0.46, 0.56, 0.1), tk.frame((cx, 0, 0.05)), tk.GUNMETAL, bone, on, taper=(0.92, 0.92), bevel=0.01)
        k.block("FootHazard", (0.47, 0.57, 0.03), tk.frame((cx, 0, 0.055)), tk.HAZARD, bone, on, bevel=0.003)
        for dx in (-0.18, 0.18):
            for dy in (-0.23, 0.23):
                k.cyl("Anchor", (cx + dx, dy, 0.085), (cx + dx, dy, 0.112), 0.022, 0.018, look=tk.STEEL, bone=bone, on=on,
                      sides=8, lod=False)
        h = COL_Z1 - COL_Z0
        k.block("Column", (COL_W, COL_D, h), tk.frame((cx, 0, COL_Z0 + h / 2)), tk.BLUE.but(panel=0.11), bone, on,
                taper=TAPER, bevel=0.01)
        # The field emitter on the road side: a dark rail with a glowing slot, top to bottom.
        xi = cx - s * (face_half(0.53, COL_W / 2, TAPER[0]) + 0.012)
        k.block("EmitterRail", (0.03, 0.13, 0.8), tk.frame((xi, 0, 0.53)), tk.DARK, bone, on, bevel=0.004)
        k.block("EmitterSlot", (0.012, 0.045, 0.74), tk.frame((xi - s * 0.016, 0, 0.53)), bk.glow("gate", 1.2), bone, on,
                bevel=0.0)
        for z in (0.18, 0.88):
            k.block("EmitterCap", (0.05, 0.15, 0.04), tk.frame((xi, 0, z)), tk.YELLOW, bone, on, bevel=0.004)
        # Crown: the white cap the beam is socketed into, a blue band and the beacon.
        k.block("Crown", (0.38, 0.46, 0.24), tk.frame((cx, 0, 1.08)), tk.WHITE.but(panel=0.12), bone, on, bevel=0.012)
        k.block("CrownBand", (0.392, 0.472, 0.04), tk.frame((cx, 0, 0.99)), tk.BLUE.but(panel=0), bone, on, bevel=0.004)
        k.block("CrownLight", (0.012, 0.3, 0.018), tk.frame((cx + s * 0.196, 0, 1.08)), bk.glow("gate", 1.5), bone, on,
                bevel=0.0)
        k.cyl("BeaconBase", (cx, 0, 1.2), (cx, 0, 1.235), 0.07, 0.06, look=tk.GUNMETAL, bone=bone, on=on, sides=12)
        k.lathe("Beacon", [(0, 0), (0.045, 0), (0.042, 0.02), (0.026, 0.04), (0, 0.048)], tk.frame((cx, 0, 1.235)),
                bk.glow("gate", 1.8), bone, on, sides=12, bevel=0.0)
        # Hex armour: three rows on the enemy face (+Y), the base face (-Y) and the outer face.
        n = 0
        for face in ("front", "back", "outer"):
            for z in HEX_ROWS:
                n += 1
                if face == "outer":
                    normal = V(s, 0, 0)
                    c = V(cx + s * face_half(z, COL_W / 2, TAPER[0]), 0, z)
                    yhint = UP
                else:
                    sy = 1 if face == "front" else -1
                    normal = V(0, sy, 0)
                    c = V(cx, sy * face_half(z, COL_D / 2, TAPER[1]), z)
                    yhint = UP
                name = f"Hex_{'L' if s < 0 else 'R'}{n}"
                sock = tk.frame(c + normal * 0.004, z=normal, y=yhint)
                k.plate("HexSocket", bk.hex_poly(HEX_R * 1.12), 0.01, sock, tk.DARK, bone, on, bevel=0.002)
                k.plate("HexConduit", bk.hex_poly(HEX_R * 0.42), 0.006, tk.frame(c + normal * 0.01, z=normal, y=yhint),
                        bk.glow("gate", 1.3), bone, on, bevel=0.0, lod=False)
                pc = c + normal * 0.02
                k.plate("HexPanel", bk.hex_poly(HEX_R), 0.022, tk.frame(pc, z=normal, y=yhint), tk.WHITE.but(panel=0), name, on,
                        bevel=0.005)
                self.hexes.append((name, pc, normal, bone))
        self.sparks.append((f"Smoke_{'L' if s < 0 else 'R'}", V(cx, 0, 1.25), bone))

    def beam(self, k):
        on = TIERS
        z0 = BEAM_TOP - 0.14
        k.block("BeamCore", (2.3, BEAM_D, 0.1), tk.frame((0, 0, z0 + 0.05)), tk.GUNMETAL, "Beam", on, bevel=0.008)
        k.block("BeamTop", (1.68, BEAM_D, 0.04), tk.frame((0, 0, BEAM_TOP - 0.02)), tk.WHITE.but(panel=0.14), "Beam", on,
                bevel=0.006)
        for sy in (-1, 1):
            k.block("BeamSide", (1.62, 0.012, 0.07), tk.frame((0, sy * (BEAM_D / 2 + 0.006), z0 + 0.065)),
                    tk.BLUE.but(panel=0.2), "Beam", on, bevel=0.003)
        for sx in (-1, 1):
            k.block("BeamHazard", (0.11, BEAM_D + 0.016, 0.112), tk.frame((sx * 0.76, 0, z0 + 0.07)), tk.HAZARD, "Beam", on,
                    bevel=0.004)
            # Gussets from the crown's inner face down under the beam.
            for sy in (-0.07, 0.07):
                xc = sx * (PX - 0.135)
                m = tk.frame((xc, sy, z0), rot=(90, 0, 0))
                poly = [(0, 0), (0, -0.2), (-0.28, 0)] if sx > 0 else [(0, 0), (0.28, 0), (0, -0.2)]
                k.plate("Gusset", poly, 0.022, m, tk.DARK, "Beam", on, bevel=0.003)
        k.block("FieldRail", (1.56, 0.09, 0.03), tk.frame((0, 0, z0 - 0.012)), tk.DARK, "Beam", on, bevel=0.004)
        k.block("FieldSlot", (1.46, 0.03, 0.012), tk.frame((0, 0, z0 - 0.03)), bk.glow("gate", 1.7), "Beam", on, bevel=0.0)
        # The saddle pad the Gate Gun clamps on: a darker strip with bolt plates either side.
        k.block("SaddlePad", (0.36, BEAM_D + 0.004, 0.008), tk.frame((0, 0, BEAM_TOP - 0.003)), tk.DARK.but(panel=0), "Beam",
                on, bevel=0.0, lod=False)
        # The threshold sill on the road between the pylons: the field's floor emitter, so the gate reads as a
        # barrier at ground level too.
        k.block("Sill", (2 * PX - 0.28, 0.16, 0.035), tk.frame((0, 0, 0.0175)), tk.GUNMETAL, "Root", on, taper=(1.0, 0.7), bevel=0.006)
        k.block("SillSlot", (2 * PX - 0.36, 0.035, 0.008), tk.frame((0, 0, 0.036)), bk.glow("gate", 1.3), "Root", on, bevel=0.0)
        for sy in (-1, 1):
            k.block("SillHazard", (2 * PX - 0.3, 0.03, 0.02), tk.frame((0, sy * 0.07, 0.012)), tk.HAZARD, "Root", on, bevel=0.002)

    def corner_armour(self, k, size, on):
        """White triangular armour in the corners between the beam and the pylons, front and back, leaving the
        emitter slot between them: the gate's frame reads as one wall from the camera."""
        for s in (-1, 1):
            bone = "Pylon_L" if s < 0 else "Pylon_R"
            xin = s * (PX - face_half(0.9, COL_W / 2, TAPER[0]))
            z0 = BEAM_TOP - 0.14
            for sy in (-1, 1):
                m = tk.frame((xin, sy * 0.092, z0), rot=(90, 0, 0))
                poly = [(0, 0), (-s * size, 0), (0, -size * 0.85)]
                if s < 0:
                    poly = [poly[0], poly[2], poly[1]]
                k.plate("CornerArmour", poly, 0.03, m, tk.WHITE.but(panel=0.1), bone, on, bevel=0.006)
                edge = [(0, -size * 0.85), (-s * size, 0), (-s * (size - 0.03), 0), (0, -size * 0.85 + 0.03)]
                if s < 0:
                    edge = list(reversed(edge))
                k.plate("CornerEdge", edge, 0.034, m, tk.BLUE.but(panel=0), bone, on, bevel=0.002)

    def tier2(self, k):
        on = ["G2", "G3"]
        self.corner_armour(k, 0.36, ["G2"])
        self.corner_armour(k, 0.5, ["G3"])
        for s in (-1, 1):
            bone = "Pylon_L" if s < 0 else "Pylon_R"
            cx = s * PX
            k.block("FootPlate", (0.62, 0.72, 0.05), tk.frame((cx, 0, 0.025)), tk.GUNMETAL, bone, on, bevel=0.008)
            k.block("FootSkirt", (0.632, 0.732, 0.03), tk.frame((cx, 0, 0.03)), tk.HAZARD, bone, on, bevel=0.003)
            # Low sloped armour leaning on the column on all four sides.
            for ax, half in ((V(s, 0, 0), COL_W / 2), (V(0, 1, 0), COL_D / 2), (V(0, -1, 0), COL_D / 2)):
                width = COL_D * 0.9 if ax.x else COL_W * 0.9
                base = V(cx, 0, 0.1) + ax * (half + 0.055)
                m = tk.frame(base + V(0, 0, 0.15), z=(ax * 0.82 + UP * 0.57), y=UP - ax * 0.5)
                k.block("Glacis", (width, 0.34, 0.03), m, tk.WHITE.but(panel=0.09), bone, on, bevel=0.006)
                k.block("GlacisEdge", (width * 0.96, 0.018, 0.034), tk.frame(base + V(0, 0, 0.29) - ax * 0.05,
                                                                            z=(ax * 0.82 + UP * 0.57), y=UP - ax * 0.5),
                        tk.BLUE.but(panel=0), bone, on, bevel=0.002)
        for sy in (-1, 1):
            k.block("BeamSkirt", (1.3, 0.014, 0.075), tk.frame((0, sy * (BEAM_D / 2 + 0.02), BEAM_TOP - 0.17), rot=(sy * 12, 0, 0)),
                    tk.BLUE.but(panel=0.16), "Beam", on, bevel=0.003)

    def tier3(self, k):
        on = ["G3"]
        for s in (-1, 1):
            bone = "Pylon_L" if s < 0 else "Pylon_R"
            cx = s * PX
            # Tank-trap deflectors on both road faces of the foot.
            for sy in (-1, 1):
                for dx in (-0.12, 0.12):
                    c = V(cx + dx, sy * 0.47, 0.11)
                    m = tk.frame(c, rot=(-sy * 22, 0, 0))
                    k.block("Deflector", (0.14, 0.12, 0.22), m, tk.GUNMETAL, bone, on, taper=(0.35, 0.3), bevel=0.006)
                    k.block("DeflectorBand", (0.15, 0.13, 0.03), tk.frame(c + V(0, 0, -0.04), rot=(-sy * 22, 0, 0)), tk.HAZARD,
                            bone, on, taper=(0.9, 0.9), bevel=0.002)
            # Heavier crown: a cap plate and two shield horns raking fore and aft.
            k.block("CrownCap", (0.42, 0.5, 0.05), tk.frame((cx, 0, 1.215)), tk.WHITE.but(panel=0.1), bone, on, bevel=0.008)
            for sy in (-1, 1):
                m = tk.frame((cx + s * 0.08, sy * 0.17, 1.33), rot=(sy * 28, 0, 0))
                k.block("Horn", (0.05, 0.08, 0.24), m, tk.WHITE.but(panel=0), bone, on, taper=(0.6, 0.5), bevel=0.006)
                k.block("HornTip", (0.035, 0.05, 0.05), tk.frame((cx + s * 0.08, sy * 0.225, 1.44), rot=(sy * 28, 0, 0)),
                        bk.glow("gate", 1.8), bone, on, bevel=0.003)
        for sx in (-1, 1):
            k.block("BeamHazard2", (0.08, BEAM_D + 0.016, 0.112), tk.frame((sx * 0.45, 0, BEAM_TOP - 0.07)), tk.HAZARD, "Beam",
                    on, bevel=0.004)

    def damage(self, k):
        """The overlay worn with the damaged state: scorch, a cracked crown plate, torn cables, embers, debris."""
        on = ["Damage"]
        rng = np.random.default_rng(31)
        # Soot on the white tops (the biggest value change at play size), a column face and the beam.
        for cx, seed in ((-PX, 2), (PX, 3)):
            k.plate("Soot", blot(0.2, seed), 0.004, tk.frame((cx + 0.03, -0.04, 1.2 + 0.003)), bk.burn(tk.WHITE), "Pylon_L" if cx < 0 else "Pylon_R",
                    on, bevel=0.0)
        for x, seed in ((-0.3, 4), (0.55, 5)):
            k.plate("Soot", blot(0.13, seed, 1.6), 0.004, tk.frame((x, 0.0, BEAM_TOP + 0.003)), bk.burn(tk.WHITE), "Beam", on, bevel=0.0)
        k.plate("Scorch", bk.rect_poly(0.34, 0.17, 0.05), 0.004, tk.frame((-0.42, 0.01, BEAM_TOP + 0.003)), bk.scorch(tk.WHITE, 0.7),
                "Beam", on, bevel=0.0)
        k.plate("Scorch", bk.rect_poly(0.22, 0.17, 0.05), 0.004, tk.frame((-PX, face_half(0.2, COL_D / 2, TAPER[1]) + 0.004, 0.2), z=(0, 1, 0), y=UP),
                bk.scorch(tk.BLUE, 0.75), "Pylon_L", on, bevel=0.0)
        # A crown plate cracked and hanging off the right pylon's enemy face.
        m = tk.frame((PX + 0.02, 0.245, 1.02), z=(0.0, 0.92, -0.38), y=(0, 0.38, 0.92))
        bk.shards(k, "CrownPlate", bk.rect_poly(0.3, 0.18, 0.02), 0.022, m, tk.WHITE.but(panel=0), "Pylon_R", on, count=3,
                  gap=0.014, tilt=12, seed=4)
        k.block("CrownTear", (0.26, 0.012, 0.012), tk.frame((PX + 0.02, 0.236, 1.1)), bk.glow("ember", 1.4), "Pylon_R", on, bevel=0.0)
        # A buckled side plate on the beam and the hot edge where it tore.
        k.block("BeamBuckle", (0.34, 0.012, 0.07), tk.frame((0.5, 0.13, BEAM_TOP - 0.08), rot=(28, 0, 8)), bk.scorch(tk.BLUE, 0.4),
                "Beam", on, bevel=0.003)
        k.block("BeamTear", (0.3, 0.008, 0.01), tk.frame((0.5, 0.112, BEAM_TOP - 0.05)), bk.glow("ember", 1.3), "Beam", on, bevel=0.0)
        # Torn cables out of two empty hex sockets, and the spark sockets there.
        for hex_name in ("Hex_L2", "Hex_R5"):
            name, c, normal, bone = next(h for h in self.hexes if h[0] == hex_name)
            p0 = c - normal * 0.006
            for j, (dz, out) in enumerate(((-0.02, 0.07), (0.015, 0.05))):
                p1 = p0 + normal * out + V(0, 0, -0.1 - 0.04 * j) + V(rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 0)
                k.cyl("Cable", p0 + V(0, 0, dz), p1, 0.008, look=tk.RUBBER, bone=bone, on=on, sides=6, bevel=0.0)
                k.cyl("CableTip", p1, p1 + (p1 - p0).normalized() * 0.012, 0.009, look=bk.glow("ember", 1.6), bone=bone, on=on,
                      sides=6, bevel=0.0)
            self.sparks.append((f"Spark_{len([s for s in self.sparks if s[0].startswith('Spark')]) + 1}", c + normal * 0.03, bone))
        self.sparks.append(("Spark_3", V(0.5, 0.13, BEAM_TOP - 0.05), "Beam"))
        # Debris at the feet: fallen hex panels and chips.
        for i in range(9):
            s = -1 if i % 2 else 1
            p = V(s * (PX + rng.uniform(-0.25, 0.3)), rng.uniform(-0.5, 0.55), 0.0)
            look = [tk.WHITE, tk.BLUE, tk.GUNMETAL][i % 3]
            if i < 4:
                k.plate("FallenHex", bk.hex_poly(HEX_R), 0.02, tk.frame(p + V(0, 0, 0.012), rot=(rng.uniform(-12, 12), rng.uniform(-12, 12), rng.uniform(0, 60))),
                        bk.scorch(tk.WHITE, 0.3), "Root", on, bevel=0.004)
            else:
                size = rng.uniform(0.035, 0.08, 3) * (1, 1, 0.35)
                k.block("Chip", tuple(size), tk.frame(p + V(0, 0, size[2] / 2), rot=(rng.uniform(-20, 20), rng.uniform(-20, 20), rng.uniform(0, 180))),
                        bk.scorch(look, 0.4), "Root", on, bevel=0.003)
        for p in (V(-PX + 0.2, 0.32, 0.01), V(PX - 0.15, -0.36, 0.01)):
            k.block("Ember", (0.04, 0.025, 0.012), tk.frame(p, rot=(0, 0, 30)), bk.glow("ember", 1.0), "Root", on, bevel=0.0)

    def wreck(self, k):
        """Snapped stumps, pylon tops fallen outward, the beam fallen across the road in two halves, scattered hexes."""
        on = ["Wreck"]
        rng = np.random.default_rng(17)
        B = bk.burn
        for s in (-1, 1):
            cx = s * PX
            k.block("Foot", (0.46, 0.56, 0.1), tk.frame((cx, 0, 0.05)), B(tk.GUNMETAL), "Root", on, taper=(0.92, 0.92), bevel=0.01)
            k.block("FootHazard", (0.47, 0.57, 0.03), tk.frame((cx, 0, 0.055)), B(tk.HAZARD), "Root", on, bevel=0.003)
            k.block("Stump", (COL_W, COL_D, 0.3), tk.frame((cx, 0, 0.25)), B(tk.BLUE), "Root", on, taper=(0.96, 0.97), bevel=0.01)
            bk.shards(k, "StumpTop", bk.rect_poly(COL_W * 0.95, COL_D * 0.95), 0.03, tk.frame((cx, 0, 0.405), rot=(s * 8, -s * 10, 0)),
                      B(tk.BLUE), "Root", on, count=4, gap=0.02, lift=0.03, tilt=22, seed=5 + s)
            k.block("StumpEmber", (0.2, 0.02, 0.014), tk.frame((cx, 0.12, 0.41), rot=(0, 0, 20)), bk.glow("ember", 1.5), "Root", on, bevel=0.0)
            # The fallen top: column section and crown, lying outward.
            top = V(cx + s * 0.62, s * 0.08, 0.15)
            rot = (0, s * 84, s * 12)
            k.block("FallenColumn", (COL_W * 0.9, COL_D * 0.9, 0.5), tk.frame(top, rot=rot), B(tk.BLUE), "Root", on, bevel=0.01)
            crown_at = top + V(s * 0.37, s * 0.05, 0.04)
            k.block("FallenCrown", (0.38, 0.46, 0.24), tk.frame(crown_at, rot=rot), B(tk.WHITE), "Root", on, bevel=0.012)
            for j in range(3):
                p = V(cx + s * rng.uniform(0.25, 0.55), rng.uniform(-0.25, 0.25), 0.012)
                k.plate("Hex", bk.hex_poly(HEX_R), 0.02, tk.frame(p, rot=(rng.uniform(-15, 15), rng.uniform(-15, 15), rng.uniform(0, 60))),
                        B(tk.WHITE), "Root", on, bevel=0.004)
        # The beam in two halves, each with one end on its stump and the broken end on the road.
        for s, end, tip in ((-1, V(-0.8, 0.02, 0.42), V(-0.06, 0.14, 0.07)), (1, V(0.82, -0.03, 0.44), V(0.1, -0.16, 0.06))):
            d = tip - end
            m = tk.frame((end + tip) / 2, z=d, y=UP)
            k.block("BeamHalf", (BEAM_D, 0.1, d.length), m, B(tk.GUNMETAL), "Root", on, bevel=0.008)
            top = tk.frame((end + tip) / 2 + V(0, 0, 0.07), z=d, y=UP)
            k.block("BeamHalfTop", (BEAM_D, 0.04, d.length * 0.85), top, B(tk.WHITE), "Root", on, bevel=0.006)
            k.block("BreakEmber", (0.16, 0.05, 0.03), tk.frame(tip + d.normalized() * 0.01 + V(0, 0, 0.03), z=d, y=UP), bk.glow("ember", 1.4),
                    "Root", on, bevel=0.0)
        k.block("HazardChunk", (0.11, 0.21, 0.11), tk.frame((0.4, 0.35, 0.055), rot=(0, 0, 25)), B(tk.HAZARD), "Root", on, bevel=0.004)
        for i in range(10):
            a = rng.uniform(0, 2 * math.pi)
            p = V(rng.uniform(-1.4, 1.4), rng.uniform(-0.7, 0.7), 0.0)
            size = rng.uniform(0.04, 0.1, 3) * (1, 1, 0.3)
            look = B([tk.WHITE, tk.BLUE, tk.GUNMETAL, tk.STEEL][i % 4])
            k.block("Scrap", tuple(size), tk.frame(p + V(0, 0, size[2] / 2), rot=(rng.uniform(-25, 25), rng.uniform(-25, 25), math.degrees(a))),
                    look, "Root", on, bevel=0.004)
        for i in range(4):
            p0 = V(rng.uniform(-1.2, 1.2), rng.uniform(-0.5, 0.5), 0.01)
            d = V(rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(0.0, 0.4)).normalized()
            k.cyl("Rod", p0, p0 + d * rng.uniform(0.12, 0.22), 0.01, look=B(tk.STEEL), bone="Root", on=on, sides=6, bevel=0.0)
        k.cyl("DyingCell", (-PX, 0.21, 0.2), (-PX, 0.21, 0.32), 0.016, look=bk.glow("gate", 0.6), bone="Root", on=on, sides=8)

    # ---------------------------------------------------------------------------------------------- rig and clips
    def bones(self):
        b = [tk.Bone("Root", (0, 0, 0), UP)]
        for s, name in ((-1, "Pylon_L"), (1, "Pylon_R")):
            b.append(tk.Bone(name, (s * (PX + 0.23), 0, 0), UP, "Root", length=0.3))
        b.append(tk.Bone("Beam", (0, 0, BEAM_TOP - 0.07), XAX, "Root", length=0.3))
        b.append(tk.Bone("GunMount", (0, 0, BEAM_TOP), UP, "Beam", deform=False, length=0.1))
        b.append(tk.Bone("Field", (0, 0, 0.53), UP, "Root", deform=False, length=0.1))
        b.append(tk.Bone("HeroPost", (0, HERO_POST, 0), V(0, 1, 0), "Root", deform=False, length=0.15))
        for name, c, normal, parent in self.hexes:
            b.append(tk.Bone(name, c, normal, parent, length=0.04))
        for name, head, parent in self.sparks:
            b.append(tk.Bone(name, head, UP, parent, deform=False, length=0.05))
        return b

    def clips(self, arm):
        acts = []
        # Repair: the panels the damaged state hides snap back in one after another.
        order = DAMAGED_HEXES

        def repair(t, f):
            out = {}
            for i, name in enumerate(order):
                out[name] = {"scale": bk.pop(t, 0.08 + i * 0.12, 0.2)}
            return out
        acts.append(bk.clip(arm, "Repair", 30, repair))
        # Build: every panel pops in, bottom row first (rebuild after a wreck, cutscene).
        names = [h[0] for h in sorted(self.hexes, key=lambda h: (h[1].z, h[1].x, h[1].y))]

        def build(t, f):
            return {n: {"scale": bk.pop(t, 0.04 + i * (0.74 / len(names)), 0.16)} for i, n in enumerate(names)}
        acts.append(bk.clip(arm, "Build", 45, build))
        # Hit: a short shudder of the beam and pylons.

        def hit(t, f):
            a = math.sin(t * math.pi * 3) * (1 - t) * math.radians(1.6)
            return {"Pylon_L": {"rot": (V(0, 1, 0), a)}, "Pylon_R": {"rot": (V(0, 1, 0), a)},
                    "Beam": {"loc": V(0, 0, -0.01 * math.sin(t * math.pi) * (1 - t))}}
        acts.append(bk.clip(arm, "Hit", 8, hit))
        # Collapse: as GateView does it in code (pylons keel outward 68 degrees, the beam drops and tilts).

        def collapse(t, f):
            u = bk.ease(max(0.0, (t - 0.12) / 0.88))
            fall = 1 - (1 - u) * (1 - u)
            bounce = math.sin((u - 0.8) / 0.2 * math.pi) * 0.06 if u > 0.8 else 0.0
            return {"Pylon_L": {"rot": (V(0, 1, 0), -math.radians(68) * fall), "loc": V(0, 0, -0.12 * fall)},
                    "Pylon_R": {"rot": (V(0, 1, 0), math.radians(64) * fall), "loc": V(0, 0, -0.12 * fall)},
                    "Beam": {"rot": (V(0, 1, 0), math.radians(34) * fall), "loc": V(0, 0, -0.95 * fall + bounce)}}
        acts.append(bk.clip(arm, "Collapse", 36, collapse))
        return acts


def damaged_pose(arm):
    bk.rest_pose(arm)
    for n in DAMAGED_HEXES:
        arm.pose.bones[n].scale = (0, 0, 0)


def build(bake=True):
    gate = Gate()
    kit = gate.kit()
    mat, report = bk.bake_set(kit, OUT, 1024, 512, bake)
    report["variants"] = {}
    for v in TIERS:
        objs = bk.skinned(kit, v, gate.bones(), mat, f"{NAME}_{v}", lod=True, arm_name=f"{NAME}_Rig")
        arm = objs[0]
        dmg = bk.extra_mesh(kit, "Damage", arm, mat, f"{NAME}_{v}_Damage")
        acts = gate.clips(arm)
        bk.export_fbx(os.path.join(OUT, f"{NAME}_{v}.fbx"), objs + [dmg], animated=True)
        report["variants"][v] = {"fbx": f"{NAME}_{v}.fbx", "tris": C.tri_count(objs[1]), "tris_lod1": C.tri_count(objs[2]),
                                 "tris_damage_overlay": C.tri_count(dmg),
                                 "bones": [b.name for b in arm.data.bones],
                                 "deform_bones": [b.name for b in arm.data.bones if b.use_deform],
                                 "clips": {a.name: int(a.frame_range[1]) for a in acts}}
        C.log(NAME, v, report["variants"][v]["tris"], report["variants"][v]["tris_lod1"])
        bk.remove(objs + [dmg])
        bk.clear_actions()
    wreck = tk.assemble(kit.of("Wreck"), f"{NAME}_Wreck")
    wreck.vertex_groups.clear()
    wreck.data.materials.append(mat)
    bk.export_fbx(os.path.join(OUT, f"{NAME}_Wreck.fbx"), [wreck], animated=False)
    report["wreck"] = {"fbx": f"{NAME}_Wreck.fbx", "tris": C.tri_count(wreck)}
    bk.remove([wreck])
    report["pivot"] = "road centre on the ground; local X across the road, -Y (Unity +Z) towards the base"
    report["span"] = {"pylon_centres_x": PX, "beam_top": BEAM_TOP, "beam_depth": BEAM_D}
    report["damaged_state_hidden_hexes"] = DAMAGED_HEXES
    report["sockets"] = {"GunMount": [0, 0, BEAM_TOP], "Field": [0, 0, 0.53], "HeroPost": [0, HERO_POST, 0],
                         "Smoke_L/R": "crown tops", "Spark_1..3": "torn sockets and the beam tear (damaged state)"}
    tk.write_json(os.path.join(OUT, f"{NAME}_report.json"), report)
    return gate, kit, mat, report


# --------------------------------------------------------------------------------------------------- previews

def place_gate(gate, kit, mat, v, at, yaw_deg, state="intact", gun=None, label=""):
    """One gate on the stage. state: intact / damaged / wreck. gun: GateGun tier to mount, or None."""
    objs = []
    if state == "wreck":
        o = tk.assemble(kit.of("Wreck"), f"W{label}")
        o.vertex_groups.clear()
        o.data.materials.append(mat)
        o.location = at
        o.rotation_euler.z = math.radians(yaw_deg)
        return [o]
    objs = bk.skinned(kit, v, gate.bones(), mat, f"{NAME}_{v}{label}", lod=False, arm_name=f"R{label}")
    arm = objs[0]
    if state == "damaged":
        objs.append(bk.extra_mesh(kit, "Damage", arm, mat, f"D{label}"))
        damaged_pose(arm)
    arm.location = at
    arm.rotation_euler.z = math.radians(yaw_deg)
    if gun:
        g = bk.tower("GateGun", gun, (0, 0, BEAM_TOP), 0.0, head_deg=-25)
        g[0].parent = arm
        objs += g
    objs.append(field_proxy(arm, 0.32 if state == "intact" else 0.16))
    return objs


def field_proxy(arm, alpha):
    """A stand-in for GateView's field sheet (the Unity shader), only for the previews."""
    import bmesh
    mesh = bpy.data.meshes.new("FieldProxy")
    bm = bmesh.new()
    w, z0, z1 = PX * 2 - 0.36, 0.08, 0.98
    vs = [bm.verts.new(p) for p in ((-w / 2, 0, z0), (w / 2, 0, z0), (w / 2, 0, z1), (-w / 2, 0, z1))]
    bm.faces.new(vs)
    bm.to_mesh(mesh)
    bm.free()
    o = C.link(bpy.data.objects.new("FieldProxy", mesh))
    mat = bpy.data.materials.get(f"FieldProxy_{alpha}")
    if mat is None:
        mat = C.principled(f"FieldProxy_{alpha}", color=bk.GLOW["gate"], rough=0.3, emission=bk.GLOW["gate"], strength=1.2)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Alpha"].default_value = alpha
        for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(mat, attr, val)
            except (AttributeError, TypeError):
                pass
    mesh.materials.append(mat)
    o.parent = arm
    o.visible_shadow = False
    return o


def previews(gate, kit, mat, out):
    # Tiers on a road that runs across the screen, gates across it, Skitters coming, a Gun Turret beside the road.
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    for i, (v, gun) in enumerate((("G1", None), ("G2", "T1"), ("G3", "T2"))):
        placed += place_gate(gate, kit, mat, v, V(-(i - 1) * 2.8, 0, 0), 90, gun=gun, label=f"t{i}")
    placed += bk.tower("Gun", "T1", (1.4, 1.6, 0), 0)
    placed += bk.tower("Cannon", "T2", (-1.4, -1.6, 0), 0)
    sk = bk.skitters([(-1.2, 0.3, 0), (-1.6, -0.4, 0), (1.5, 0.2, 0), (4.1, -0.3, 0)], yaw=math.radians(90))
    bk.render_game(V(0, 0, 0.4), os.path.join(out, "gate_tiers_game.png"), zoom=1.0, width=1280, height=720)
    bk.render_game(V(0, 0, 0.5), os.path.join(out, "gate_tiers_zoom.png"), zoom=2.3, width=1280, height=720)
    bk.unstage(sk)
    bk.remove(placed)
    # Close-ups per tier, gate face-on to the camera side (road along Y).
    for v, gun in (("G1", None), ("G2", "T1"), ("G3", "T2")):
        objs = place_gate(gate, kit, mat, v, V(0, 0, 0), 0, gun=gun, label=v)
        bk.render_close(V(0, 0, 0.62), os.path.join(out, f"gate_{v}_close.png"), 4.2, azimuth=32, elevation=22, lens=50, light_scale=1.4)
        bk.remove(objs)
    C.contact_sheet([os.path.join(out, f"gate_{v}_close.png") for v in TIERS], os.path.join(out, "gate_close_sheet.png"), 3, 640)
    bk.remove(stage)
    # States row: intact, damaged, wreck (G2 with its gun).
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    for i, st in enumerate(("intact", "damaged", "wreck")):
        placed += place_gate(gate, kit, mat, "G2", V(-(i - 1) * 2.8, 0, 0), 90, state=st, gun="T1" if st != "wreck" else None, label=f"s{i}")
    bk.render_game(V(0, 0, 0.45), os.path.join(out, "gate_states_zoom.png"), zoom=2.3, width=1280, height=720)
    bk.render_game(V(0, 0, 0.45), os.path.join(out, "gate_states_game.png"), zoom=1.0, width=1280, height=720)
    bk.remove(placed)
    for st in ("damaged", "wreck"):
        objs = place_gate(gate, kit, mat, "G2", V(0, 0, 0), 0, state=st, label="c" + st)
        bk.render_close(V(0, 0, 0.5), os.path.join(out, f"gate_{st}_close.png"), 4.0, azimuth=32, elevation=26, lens=50, light_scale=1.4)
        bk.remove(objs)
    # Macro on the hex panels and the emitter.
    objs = place_gate(gate, kit, mat, "G1", V(0, 0, 0), 0, label="m")
    bk.render_close(V(-PX + 0.1, 0.15, 0.6), os.path.join(out, "gate_macro.png"), 1.1, azimuth=40, elevation=12, lens=60,
                    width=1280, height=720, light_scale=0.8)
    # Clip strips: Repair (from damaged) and Collapse.
    arm = objs[0]
    acts = {a.name: a for a in gate.clips(arm)}
    dmg = bk.extra_mesh(kit, "Damage", arm, mat, "Dm")
    rig = tk.close_lights(1.4)
    cam = tk.closeup_camera(V(0, 0, 0.6), 4.0, azimuth=30, elevation=20, lens=50)
    for clip_name, frames in (("Repair", (0, 6, 12, 18, 24, 30)), ("Collapse", (0, 8, 14, 20, 28, 36))):
        arm.animation_data.action = acts[clip_name]
        dmg.hide_render = clip_name != "Repair"
        strip = []
        for f in frames:
            bpy.context.scene.frame_set(f)
            p = os.path.join(out, f"_gate_{clip_name}_{f}.png")
            C.render(cam, p, 480, 360, bloom=False)
            strip.append(p)
        C.contact_sheet(strip, os.path.join(out, f"gate_{clip_name.lower()}_strip.png"), len(strip), 400)
        for p in strip:
            os.remove(p)
    bk.remove([cam] + rig + objs + [dmg])
    bk.clear_actions()
    bk.remove(stage)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    C.preview_world()
    tk.metal_detail_map()
    gate, kit, mat, report = build(bake=not args.get("nobake"))
    if not args.get("norender"):
        previews(gate, kit, mat, out)
    C.save_blend(NAME)


if __name__ == "__main__":
    main()
