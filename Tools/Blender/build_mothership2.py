"""Round 19: the expedition mothership rebuilt as a hero hard-surface asset (see ArtSource/DESIGN_NOTES.md, round 19).

Replaces round 13's `Mothership` in build_cutscene.py (same file names, bones, clip and sockets, so the Unity import keeps
working). Built on the round-7 hard-surface kit (tower_kit pieces and the atlas unwrap), with its own pieces and bake:

- **Lofted hull pieces** (`loft`, `revolve`): an angular, tapered command hull with a raked bridge, octagonal keel,
  a box-section habitat ring, engine block and armour plates, instead of round lathes on a stick.
- **Weighted normals**: big faces stay flat and the single-segment chamfers carry the highlight (applied to the bake
  target and the exported meshes alike, so the tangent-space normals match).
- **Ship-scale self-bake** (`bake_material`): panel seams sized for a 15-unit ship at 2048, staggered plating with a
  tint and smoothness per panel, light-grey accent panels, hatches and vents in the normal map, lit window rows
  (habitat ring, command decks), heat tint on the engine bells, AO grime, and **decals** (ship name, hull code,
  NTI emblem, engine number) rendered from text into images and projected in the bake.
- 2048 maps (+ 1024 `Low/`), LOD1 as `Cutscene_Mothership_LOD1.fbx` (no chamfers, no greebles).

Out: `ArtSource/Exports/Cutscene_Mothership/` (FBX, LOD1 FBX, maps, report), `ArtSource/Blend/Cutscene_Mothership.blend`.

Run: blender --background --factory-startup --python Tools/Blender/build_mothership2.py -- [--preview <dir>]
     [--norender] [--nobake] [--fast (1024 maps, 6 samples: for look-dev only)] [--old <folder with the round-13 export>]
"""

import math
import os
import sys
import tempfile

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import surface  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP, XAX  # noqa: E402

NAME = "Cutscene_Mothership"
TEX = 2048
LOW = 1024
BACK = Vector((0, 1, 0))
FRONT = Vector((0, -1, 0))

# -------------------------------------------------------------------------------------------------------------
# Looks (the round-7 palette with ship-scale panels; extra fields are read by this file's bake only)
# -------------------------------------------------------------------------------------------------------------
# Extra look fields: cyl (radius for angular panel mapping round local Z, 0 = box mapping), plating (panel tint
# variation), accent (share of light-grey accent panels), hatch (share of hatch panels), vent (share of vent panels),
# win (period, width, height), rows (v1, v2, u0, u1), heat (heat tint along local z per unit), decal (decals apply).

HULL = tk.WHITE.but(paint=(0.79, 0.805, 0.82), panel=0.36, bolts=False, plating=1.0, accent=0.15, hatch=0.16, vent=0.06,
                    decal=1.0, grad=0.45, rough=0.4, wear=0.5)
HULL_B = tk.BLUE.but(panel=0.3, bolts=False, plating=0.8, accent=0.15, hatch=0.1, grad=0.45, wear=0.5)
PLATE = HULL.but(panel=0.28, accent=0.25, hatch=0.22)
GREY = tk.WHITE.but(paint=(0.6, 0.62, 0.65), panel=0.28, bolts=False, plating=0.7, accent=0.1, hatch=0.2, grad=0.45, wear=0.5)
GUN = tk.GUNMETAL.but(panel=0.3, plating=0.6, hatch=0.25, vent=0.15, grad=0.4)
GUN_PLAIN = tk.GUNMETAL.but(grad=0.4)
STEEL = tk.STEEL.but(grad=0.4)
DARK = tk.DARK.but(grad=0.35)
HAZ = tk.HAZARD.but(grad=0.4, wear=0.6)
YEL = tk.YELLOW.but(panel=0.22, bolts=False, plating=0.6, grad=0.45, rings=0.11)
GLASS = tk.GLASS.but(grad=0.3)
RADIATOR = tk.DARK.but(paint=(0.1, 0.11, 0.13), panel=0.16, plating=0.4, grad=0.3, rough=0.35)
WIN_LIGHT = (0.8, 0.92, 1.0)
GLOW_WHITE = sk.glow("ora_hot", 1.1)
GLOW_CYAN = sk.glow("cyan", 1.2)
GLOW_ENGINE = sk.glow((0.45, 0.72, 1.0), 1.0)
GLOW_BEACON = sk.glow("star", 1.8)


def cont(look, ribs=0.11):
    """A cargo container's skin: ribbed (ring grooves along its length), no seams."""
    return look.but(panel=0.0, rings=ribs, plating=0.0, accent=0.0, hatch=0.0, decal=0.0)


# -------------------------------------------------------------------------------------------------------------
# Recipes
# -------------------------------------------------------------------------------------------------------------

def section(w, h, zc=0.0, top=0.25, bottom=0.35):
    """A chamfered rectangle in the XZ plane (8 points, counter-clockwise seen from -Y): flat deck, deeper keel chamfer."""
    x, z = w / 2, h / 2
    ct, cb = min(w, h) * top, min(w, h) * bottom
    return [(-x + cb, zc - z), (x - cb, zc - z), (x, zc - z + cb), (x, zc + z - ct), (x - ct, zc + z), (-x + ct, zc + z),
            (-x, zc + z - ct), (-x, zc - z + cb)]


def loft(sections):
    """Sections [(y, [(x, z)...])] of equal point count, lofted along +Y with flat caps."""
    def recipe():
        bm = bmesh.new()
        rings = [[bm.verts.new((x, y, z)) for x, z in poly] for y, poly in sections]
        n = len(rings[0])
        for a, b in zip(rings, rings[1:]):
            for i in range(n):
                i2 = (i + 1) % n
                bm.faces.new((a[i], a[i2], b[i2], b[i]))
        bm.faces.new(rings[0])
        bm.faces.new(list(reversed(rings[-1])))
        return bm
    return recipe


def revolve(profile, sides, a0=0.0, a1=None):
    """A closed (r, z) polygon revolved round local Z: a box-section ring. a0..a1 (radians) makes a capped sector."""
    full = a1 is None

    def recipe():
        bm = bmesh.new()
        span = 2 * math.pi if full else a1 - a0
        count = sides if full else sides + 1
        rings = []
        for s in range(count):
            a = a0 + span * s / sides
            c, si = math.cos(a), math.sin(a)
            rings.append([bm.verts.new((c * r, si * r, z)) for r, z in profile])
        n = len(profile)
        pairs = zip(rings, rings[1:] + ([rings[0]] if full else []))
        for a, b in pairs:
            for i in range(n):
                i2 = (i + 1) % n
                bm.faces.new((a[i], a[i2], b[i2], b[i]))
        if not full:
            bm.faces.new(rings[0])
            bm.faces.new(list(reversed(rings[-1])))
        return bm
    return recipe


def chamfer_rect(w, h, c):
    return bk.rect_poly(w, h, c)


def add(k, name, recipe, m, look, bone="Root", bevel=0.0, lod=True):
    return k._add(name, recipe, bevel, m, look, bone, ["Main"], lod)


# -------------------------------------------------------------------------------------------------------------
# The ship
# -------------------------------------------------------------------------------------------------------------

class Mothership:
    """Blender space: nose towards -Y (Unity +Z), up +Z. About 15 long (-7.35 .. 7.6)."""
    name = NAME
    RING_Y = -2.1
    RING_R = 3.15
    HULL = [  # y, width, height, deck centre z
        (-7.35, 0.34, 0.2, -0.02), (-7.05, 0.9, 0.46, 0.0), (-6.35, 1.5, 0.86, 0.05), (-5.35, 1.92, 1.16, 0.08),
        (-4.45, 2.04, 1.28, 0.08), (-4.0, 1.92, 1.2, 0.08)]
    KEEL = 0.86          # spine width and height
    BAY_Y = (-0.86, 1.94)

    def __init__(self):
        self.sockets = {}
        self.lights = []
        self.k = tk.Kit(self.name, ["Main"])
        self.bone_list = [tk.Bone("Root", (0, 0, 0), UP, length=0.2),
                          tk.Bone("Ring", (0, self.RING_Y, 0), BACK, "Root", length=0.6)]
        self.decals = []

    # Helpers ------------------------------------------------------------------------------------------------
    def hull_at(self, y):
        """(half width, half height, deck centre z) of the command hull at y."""
        hs = self.HULL
        for (y0, w0, h0, z0), (y1, w1, h1, z1) in zip(hs, hs[1:]):
            if y0 <= y <= y1:
                t = (y - y0) / (y1 - y0)
                return (w0 + (w1 - w0) * t) / 2, (h0 + (h1 - h0) * t) / 2, z0 + (z1 - z0) * t
        y0, w0, h0, z0 = hs[-1]
        return w0 / 2, h0 / 2, z0

    def top(self, y):
        _, hh, zc = self.hull_at(y)
        return zc + hh

    def light(self, pos, look=GLOW_WHITE, r=0.035, bone="Root"):
        k = self.k
        add(k, "NavLight", lambda: tk._bm_lathe([(0, -r), (r, -r * 0.4), (r, r * 0.4), (0, r)], 8), tk.frame(pos), look, bone,
            lod=False)
        if bone == "Root":
            self.lights.append(Vector(pos))

    def pieces(self):
        self.command()
        self.keel()
        self.ring()
        self.cargo()
        self.bay()
        self.tanks()
        self.radiators()
        self.engine()
        for i, p in enumerate(self.lights):
            self.sockets[f"Light_{i + 1}"] = p
        return self.k

    # Command section ------------------------------------------------------------------------------------------
    def command(self):
        k = self.k
        secs = [(y, section(w, h, z, top=0.22, bottom=0.42)) for y, w, h, z in self.HULL]
        hull = HULL.but(win=(0.19, 0.09, 0.05), rows=(0.3, 0.3, -6.05, -4.2), belly=-0.06)
        add(k, "Command", loft(secs), tk.frame(), hull, bevel=0.03)
        # Neck: a short step-down into the keel with a gunmetal collar.
        add(k, "Neck", loft([(-4.02, section(1.6, 1.04, 0.06)), (-3.6, section(1.12, 1.0, 0.03))]), tk.frame(), GUN, bevel=0.025)
        # Raked bridge on the deck: a low wedge with the lit window band on its sloping front.
        deck = self.top(-5.0)
        bsec = []
        for y, w, h in ((-5.9, 0.86, 0.14), (-5.45, 1.08, 0.56), (-4.6, 1.16, 0.6), (-4.3, 0.96, 0.44)):
            bottom = self.top(y) - 0.05
            bsec.append((y, section(w, h, bottom + h / 2, 0.22, 0.1)))
        row = self.top(-4.95) - 0.05 + 0.33
        add(k, "Bridge", loft(bsec), tk.frame(), PLATE.but(decal=0.0, win=(0.12, 0.075, 0.06), rows=(row, row, -5.38, -4.5)), bevel=0.02)
        a, b = V(0, -5.9, self.top(-5.9) - 0.05 + 0.14), V(0, -5.45, self.top(-5.45) - 0.05 + 0.56)
        along = (b - a).normalized()
        n = along.cross(XAX).normalized()
        if n.y > 0:
            n = -n
        mid = a + (b - a) * 0.55 + n * 0.012
        f = tk.frame(mid, z=n, y=along)
        add(k, "BridgeGlass", lambda: tk._bm_box(0.92, (b - a).length * 0.46, 0.02), f, GLASS)
        add(k, "BridgeWindow", lambda: tk._bm_box(0.86, 0.025, 0.024), f @ tk.frame((0, -(b - a).length * 0.17, 0)), sk.glow("ora", 1.0))
        for x in (-0.3, -0.1, 0.1, 0.3):
            add(k, "Mullion", lambda: tk._bm_box(0.025, (b - a).length * 0.44, 0.03), f @ tk.frame((x, 0, 0.002)), GUN_PLAIN,
                lod=False)
        # Armour plates on the flanks: layered, angled with the hull taper.
        for s in (-1, 1):
            for (y0, y1, z0, z1, look) in ((-6.5, -5.4, 0.0, 0.22, PLATE), (-5.5, -4.15, -0.01, 0.23, PLATE),
                                           (-4.35, -4.05, 0.0, 0.3, HULL_B)):
                self.flank_plate(s, y0, y1, z0, z1, look)
        # Under the nose: a sensor chin with a glass eye, and two forward sensor booms.
        add(k, "Chin", loft([(-6.7, section(0.5, 0.16, -0.36, 0.2, 0.4)), (-6.2, section(0.72, 0.26, -0.44, 0.2, 0.4)),
                             (-5.5, section(0.7, 0.24, -0.46, 0.2, 0.4))]), tk.frame(), GUN, bevel=0.015)
        add(k, "SensorEye", lambda: tk._bm_lathe([(0, 0), (0.1, 0), (0.09, 0.05), (0, 0.07)], 12), tk.frame((0, -6.5, -0.47), z=V(0, -0.5, -1)),
            sk.glow("cyan", 1.3, lens=0.1))
        for s in (-1, 1):
            k.cyl("SensorBoom", (s * 0.32, -6.9, 0.12), (s * 0.3, -7.85, 0.1), 0.028, 0.016, look=STEEL, bone="Root", on=["Main"], sides=6,
                  bevel=0.0)
            self.light(V(s * 0.3, -7.87, 0.1), sk.glow("ora_hot", 2.0), r=0.03)
        # Dorsal comm mast with a large dish behind the bridge and an antenna farm.
        self.dish(V(0.35, -4.12, self.top(-4.12) - 0.02), V(0.55, 0.0, 1.0), 0.6, mast=0.5)
        for i, (x, y, hgt) in enumerate(((-0.35, -4.5, 0.7), (-0.25, -4.75, 0.5), (0.05, -4.45, 0.9))):
            k.cyl("Antenna", (x, y, deck + 0.4), (x, y, deck + 0.4 + hgt), 0.016, 0.008, look=STEEL, bone="Root", on=["Main"], sides=5,
                  bevel=0.0, lod=False)
        self.light(V(0.05, -4.45, deck + 1.32), GLOW_BEACON, r=0.03)
        # Nose cap and nav lights.
        self.light(V(0, -7.37, -0.02), GLOW_WHITE, r=0.04)
        # Decals: ship name and hull code behind the emblem on both flanks, emblem forward.
        for s in (-1, 1):
            hw = self.hull_at(-5.2)[0]
            u = BACK * s
            self.decals.append(("name", V(s * hw, -4.95, 0.06), u, UP, V(s, 0, 0), 1.25, 0.31))
            hw2 = self.hull_at(-6.15)[0]
            self.decals.append(("emblem", V(s * hw2, -6.12, 0.08), u, UP, V(s, 0, 0), 0.42, 0.42))

    def flank_plate(self, s, y0, y1, z0, z1, look):
        a, b = self.hull_at(y0)[0], self.hull_at(y1)[0]
        p0, p1 = V(s * a, y0, 0), V(s * b, y1, 0)
        along = (p1 - p0).normalized()
        n = UP.cross(along).normalized() * (1 if s < 0 else -1)
        if n.x * s < 0:
            n = -n
        c = (p0 + p1) / 2 + n * 0.03 + V(0, 0, (z0 + z1) / 2)
        f = tk.frame(c, z=n, y=UP)
        L, H = (p1 - p0).length, z1 - z0
        poly = [(-L / 2, -H / 2), (L / 2 - 0.08, -H / 2), (L / 2, -H / 2 + 0.08), (L / 2, H / 2), (-L / 2 + 0.12, H / 2), (-L / 2, H / 2 - 0.12)]
        if s > 0:
            poly = [(-x, y) for x, y in reversed(poly)]
        self.k.plate("FlankPlate", poly, 0.05, f, look, "Root", ["Main"], bevel=0.012)

    def dish(self, base, normal, r, mast=0.4, bone="Root"):
        k = self.k
        n = Vector(normal).normalized()
        top = base + UP * mast
        k.cyl("DishMast", base, top, 0.06, 0.045, look=GUN_PLAIN, bone=bone, on=["Main"], sides=8)
        add(k, "DishYoke", lambda: tk._bm_box(0.16, 0.16, 0.12), tk.frame(top, z=n), GUN_PLAIN, bone, bevel=0.01)
        centre = top + n * 0.1
        prof = [(0, 0.0), (0.1, 0.01), (r * 0.6, r * 0.12), (r, r * 0.3), (r * 1.02, r * 0.34), (r * 0.97, r * 0.35), (r * 0.58, r * 0.17),
                (0.08, 0.06), (0, 0.06)]
        add(k, "Dish", lambda: tk._bm_lathe(prof, 24), tk.frame(centre, z=n), HULL.but(panel=0.0, decal=0.0, plating=0.0), bone)
        k.cyl("DishFeed", centre + n * 0.06, centre + n * r * 0.75, 0.02, 0.012, look=STEEL, bone=bone, on=["Main"], sides=6, bevel=0.0)
        add(k, "DishHorn", lambda: tk._bm_lathe([(0, 0), (0.05, 0), (0.035, 0.07), (0, 0.07)], 8), tk.frame(centre + n * r * 0.75, z=n),
            GUN_PLAIN, bone)

    # Keel (spine) ---------------------------------------------------------------------------------------------
    def keel(self):
        k = self.k
        w = self.KEEL
        # In segments between the collars: short UV islands pack far better than one 8.7-long strip.
        cuts = (-3.7, -3.3, -2.1, -0.85, 0.85, 2.55, 3.5, 4.45, 5.0)
        for y0, y1 in zip(cuts, cuts[1:]):
            add(k, "Keel", loft([(y0, section(w, w, 0, 0.3, 0.3)), (y1, section(w, w, 0, 0.3, 0.3))]), tk.frame(), GUN, bevel=0.025)
        # Structural collars between modules.
        for y in (-3.3, -0.85, 2.55, 4.45):
            add(k, "Collar", loft([(y - 0.11, section(w + 0.22, w + 0.22, 0, 0.3, 0.3)), (y + 0.11, section(w + 0.22, w + 0.22, 0, 0.3, 0.3))]),
                tk.frame(), GREY.but(decal=0.0), bevel=0.02)
            for s in (-1, 1):
                add(k, "CollarHaz", lambda: tk._bm_box(0.025, 0.16, 0.5), tk.frame((s * (w / 2 + 0.115), y, 0)), HAZ, lod=False)
        # Conduits along the flanks of the keel (visible on the neck and between the tanks).
        for s in (-1, 1):
            for z in (0.2, -0.2):
                for y0, y1 in ((-3.5, -0.85), (-0.85, 2.55), (2.55, 4.4)):
                    k.cyl("Conduit", (s * (w / 2 + 0.04), y0, z), (s * (w / 2 + 0.04), y1, z), 0.04, look=STEEL.but(rings=0.5), bone="Root",
                          on=["Main"], sides=8, bevel=0.0, lod=y0 > 0)
        # Neck greebles: service boxes and a truss bridge on the top between the command and the ring bearing.
        rng = np.random.default_rng(19)
        for i in range(6):
            y = -3.55 + i * 0.13 + rng.uniform(-0.02, 0.02)
            sx, sy, sz = rng.uniform(0.18, 0.34), rng.uniform(0.08, 0.12), rng.uniform(0.06, 0.12)
            add(k, "Greeble", lambda sx=sx, sy=sy, sz=sz: tk._bm_box(sx, sy, sz), tk.frame((rng.uniform(-0.2, 0.2), y, w / 2 + sz / 2)), GUN_PLAIN,
                bevel=0.008, lod=False)

    # Habitat ring ---------------------------------------------------------------------------------------------
    def ring(self):
        k = self.k
        ry, R = self.RING_Y, self.RING_R
        rf = tk.frame((0, ry, 0), z=BACK)
        # Bearing collars (static, on the keel) either side of the turning hub.
        for dy in (-0.62, 0.62):
            add(k, "Bearing", lambda: tk._bm_lathe([(0, -0.12), (0.7, -0.12), (0.78, -0.06), (0.78, 0.06), (0.7, 0.12), (0, 0.12)], 20),
                tk.frame((0, ry + dy, 0), z=BACK), GREY.but(cyl=0.78, decal=0.0), bevel=0.0)
            add(k, "BearingHaz", lambda: tk._bm_lathe([(0, -0.02), (0.79, -0.02), (0.79, 0.02), (0, 0.02)], 20),
                tk.frame((0, ry + dy * 0.82, 0), z=BACK), HAZ, lod=False)
        # Hub (turns with the ring).
        hub = [(0, -0.5), (0.62, -0.5), (0.72, -0.4), (0.72, 0.4), (0.62, 0.5), (0, 0.5)]
        add(k, "Hub", lambda: tk._bm_lathe(hub, 20), rf, GUN.but(cyl=0.72), "Ring")
        add(k, "HubBand", lambda: tk._bm_lathe([(0, -0.1), (0.735, -0.1), (0.735, 0.1), (0, 0.1)], 20), rf, HULL_B.but(cyl=0.735), "Ring",
            lod=False)
        add(k, "HubGlow", lambda: tk._bm_torus(0.74, 0.018, 24, 4), rf @ tk.frame((0, 0, 0.3)), sk.glow("base", 1.4), "Ring", lod=False)
        # The habitat: a box-section ring with lit window rows on the outer wall.
        hw, hh, c = 0.38, 0.27, 0.07
        prof = [(R - hh + c, -hw), (R + hh - c, -hw), (R + hh, -hw + c), (R + hh, hw - c), (R + hh - c, hw), (R - hh + c, hw), (R - hh, hw - c),
                (R - hh, -hw + c)]
        hab = HULL.but(cyl=R + hh, panel=0.42, accent=0.08, win=(0.16, 0.07, 0.05), rows=(0.13, -0.13, -99.0, 99.0), decal=0.0, hatch=0.12)
        # Eight sectors between the bulkheads (each its own UV island instead of two 6.8-wide annulus discs).
        for i in range(8):
            a0 = 2 * math.pi * i / 8
            add(k, "Habitat", revolve(prof, 12, a0, a0 + 2 * math.pi / 8), rf, hab, "Ring")
        liner = [(R - hh - 0.025, -hw + 0.05), (R - hh + 0.01, -hw + 0.05), (R - hh + 0.01, hw - 0.05), (R - hh - 0.025, hw - 0.05)]
        for i in range(8):
            a0 = 2 * math.pi * i / 8
            add(k, "Liner", revolve(liner, 12, a0, a0 + 2 * math.pi / 8), rf, GUN.but(cyl=R - hh, panel=0.3), "Ring", lod=False)
        # Pressure bulkheads (eight raised bands) and a colony-blue rail along each rim.
        prof2 = [(r + (0.035 if r > R else -0.035), z * 1.08) for r, z in prof]
        for i in range(8):
            a = 2 * math.pi * i / 8
            add(k, "Bulkhead", revolve(prof2, 3, a - 0.035, a + 0.035), rf, GREY.but(cyl=R + hh + 0.035, decal=0.0), "Ring")
        for z in (-hw - 0.012, hw + 0.012):
            for i in range(4):
                a0 = 2 * math.pi * i / 4
                add(k, "RingRail", revolve([(R + hh - 0.02, -0.02), (R + hh + 0.01, -0.02), (R + hh + 0.01, 0.02), (R + hh - 0.02, 0.02)], 24,
                                           a0, a0 + math.pi / 2), rf @ tk.frame((0, 0, z * 0.98)),
                    HULL_B.but(cyl=R + hh, panel=0.0, hatch=0.0, accent=0.0), "Ring", lod=False)
        # Ring lights on the bulkheads (glow pips that turn with the ring).
        for i in range(8):
            a = 2 * math.pi * i / 8
            p = V(math.cos(a) * (R + hh - 0.07), math.sin(a) * (R + hh - 0.07), hw + 0.015)
            add(k, "RingLight", lambda: tk._bm_box(0.05, 0.05, 0.03), rf @ tk.frame(p), sk.glow("cyan", 1.8), "Ring", lod=False)
        # Four spokes (diagonal, so the keel stays clear in side view): a box beam, a lift tube and a junction pod.
        for i in range(4):
            a = math.radians(45 + 90 * i)
            d = V(math.cos(a), math.sin(a), 0)
            side = V(-math.sin(a), math.cos(a), 0)
            r0, r1 = 0.68, R - hh + 0.02
            mid = d * ((r0 + r1) / 2)
            f = rf @ tk.frame(mid, z=d, y=V(0, 0, 1))
            add(k, "Spoke", lambda: tk._bm_box(0.28, 0.2, r1 - r0, taper=(0.75, 0.8)), f, GREY.but(panel=0.26, decal=0.0), "Ring", bevel=0.02)
            p0, p1 = d * r0 + side * 0.18, d * r1 + side * 0.15
            k.cyl("LiftTube", rf @ p0, rf @ p1, 0.065, look=STEEL.but(rings=0.18), bone="Ring", on=["Main"], sides=8, bevel=0.0)
            pod = rf @ tk.frame(d * (r0 + (r1 - r0) * 0.55), z=d, y=V(0, 0, 1))
            add(k, "SpokePod", lambda: tk._bm_box(0.38, 0.3, 0.32), pod, HULL.but(decal=0.0, panel=0.2), "Ring", bevel=0.025, lod=False)
            add(k, "SpokeHaz", lambda: tk._bm_box(0.4, 0.32, 0.04), pod @ tk.frame((0, 0, -0.18)), HAZ, "Ring", lod=False)

    # Cargo racks ------------------------------------------------------------------------------------------------
    def cargo(self):
        k = self.k
        w = self.KEEL
        rows = (-0.4, 0.55, 1.5)
        colours = [HULL, HULL, GREY, HULL_B, HULL, YEL, HULL, GREY, HULL_B, HULL, HULL, HULL_B]
        n = 0
        for j, y in enumerate(rows):
            for face in ("top", "left", "right", "bottom"):
                for slot in (-1, 1):
                    if face == "bottom" and self.BAY_Y[0] - 0.5 < y < self.BAY_Y[1] + 0.5:
                        continue
                    if face == "top":
                        c = V(slot * 0.3, y, w / 2 + 0.27)
                        size = (0.56, 0.86, 0.52)
                    elif face == "bottom":
                        c = V(slot * 0.3, y, -w / 2 - 0.27)
                        size = (0.56, 0.86, 0.52)
                    else:
                        s = -1 if face == "left" else 1
                        c = V(s * (w / 2 + 0.27), y, slot * 0.25)
                        size = (0.52, 0.86, 0.48)
                    look = cont(colours[(n * 7 + j * 3) % len(colours)])
                    n += 1
                    add(k, "Container", lambda size=size: tk._bm_box(size[0], size[2], size[1]), tk.frame(c, z=BACK, y=UP), look, bevel=0.03)
        # Rack frames: square rings round each row's ends and long rails on the outer corners.
        R = w / 2 + 0.56
        for y in (-0.92, 0.07, 1.03, 1.98):
            for sx, sz, x, z in ((2 * R, 0.07, 0, R), (0.07, 2 * R, R, 0), (0.07, 2 * R, -R, 0)):
                add(k, "RackFrame", lambda sx=sx, sz=sz: tk._bm_box(sx * 0.7 if sx < 0.1 else sx, 0.05, sz * 0.7 if sz < 0.1 else sz), tk.frame((x, y, z)),
                    GREY.but(panel=0.0, decal=0.0), bevel=0.008, lod=sz > 0.1)
        for x, z in ((R, R), (-R, R), (R, -R), (-R, -R)):
            add(k, "RackRail", lambda: tk._bm_box(0.09, 2.98, 0.09), tk.frame((x, 0.53, z)), GREY.but(decal=0.0), bevel=0.015)

    # Capsule bay ---------------------------------------------------------------------------------------------------
    def bay(self):
        k = self.k
        y0, y1 = self.BAY_Y
        yc, L = (y0 + y1) / 2, y1 - y0
        top, depth, W = -self.KEEL / 2, 0.62, 1.5
        zc = top - depth / 2
        # Walls of the hangar (open floor), dark lit interior, and the racked capsules.
        for s in (-1, 1):
            add(k, "BayWall", lambda: tk._bm_box(0.1, L, depth), tk.frame((s * (W / 2 - 0.05), yc, zc)), GUN, bevel=0.02)
        for y in (y0 + 0.05, y1 - 0.05):
            add(k, "BayEnd", lambda: tk._bm_box(W, 0.1, depth), tk.frame((0, y, zc)), GUN, bevel=0.02)
        add(k, "BayCeiling", lambda: tk._bm_box(W - 0.2, L - 0.2, 0.04), tk.frame((0, yc, top - 0.02)), GUN.but(paint=(0.28, 0.3, 0.33)))
        for s in (-1, 1):
            add(k, "BayStrip", lambda: tk._bm_box(0.012, L - 0.3, 0.05), tk.frame((s * (W / 2 - 0.105), yc, zc + 0.12)), GLOW_CYAN, lod=False)
        for x in (-0.42, 0.42):
            add(k, "BayCeilLight", lambda: tk._bm_box(0.06, L - 0.4, 0.012), tk.frame((x, yc, top - 0.045)), sk.glow("ora_hot", 1.6), lod=False)
        cap = [(0, 0.0), (0.2, 0.0), (0.21, 0.05), (0.16, 0.34), (0.1, 0.4), (0, 0.42)]
        for i in range(3):
            y = y0 + L * (i + 1) / 4
            add(k, "RackClamp", lambda: tk._bm_box(0.3, 0.12, 0.12), tk.frame((0, y, top - 0.08)), GUN_PLAIN, bevel=0.01, lod=False)
            add(k, "Racked", lambda: tk._bm_lathe(cap, 6, 0.0), tk.frame((0, y, top - 0.56)), HULL.but(panel=0.0, decal=0.0, plating=0.0),
                lod=i == 1)
            add(k, "RackedShield", lambda: tk._bm_lathe([(0, 0), (0.205, 0), (0.205, 0.04), (0, 0.04)], 12), tk.frame((0, y, top - 0.6)),
                sk.SHIELD.but(glow=0.0, emit=None), lod=False)
        # Clamshell doors hinged on the lower wall edges, swung open outwards.
        for s in (-1, 1):
            hinge = V(s * (W / 2), yc, top - depth)
            a = math.radians(62)
            d = V(s * math.cos(a), 0, -math.sin(a))
            c = hinge + d * (W / 4)
            f = tk.frame(c, z=V(s * math.sin(a), 0, math.cos(a)), y=BACK)
            add(k, "BayDoor", lambda: tk._bm_box(W / 2, L - 0.04, 0.05), f, HULL.but(panel=0.24, decal=0.0), bevel=0.012)
            add(k, "DoorInner", lambda: tk._bm_box(W / 2 - 0.08, L - 0.14, 0.012), f @ tk.frame((0, 0, -0.03)), GUN_PLAIN.but(panel=0.18), lod=False)
            add(k, "DoorHaz", lambda: tk._bm_box(0.06, L - 0.02, 0.054), f @ tk.frame((s * (W / 4 - 0.03), 0, 0)), HAZ, lod=False)
            add(k, "Hinge", lambda: tk._bm_lathe([(0, 0), (0.05, 0), (0.05, L - 0.1), (0, L - 0.1)], 8), tk.frame(hinge - V(0, L / 2 - 0.05, 0), z=BACK),
                STEEL, lod=False)
        for s in (-1, 1):
            self.light(V(s * (W / 2 + 0.02), y0 + 0.08, top - depth + 0.05), sk.glow("star", 1.6), r=0.03)
        self.sockets["Bay"] = (V(0, yc, top - depth - 0.12), V(0, 0, -1))

    # Tanks -----------------------------------------------------------------------------------------------------------
    def tanks(self):
        k = self.k
        prof = [(0, 0), (0.14, 0.01), (0.3, 0.08), (0.37, 0.2), (0.38, 0.4), (0.38, 1.0), (0.37, 1.2), (0.3, 1.32), (0.14, 1.39), (0, 1.4)]
        for i in range(4):
            a = math.radians(45 + 90 * i)
            c = V(math.cos(a) * 0.74, 2.85, math.sin(a) * 0.74)
            look = HULL.but(cyl=0.38, panel=0.24, decal=0.0, hatch=0.0) if i % 2 == 0 else GREY.but(cyl=0.38, hatch=0.0)
            add(k, "Tank", lambda: tk._bm_lathe(prof, 20), tk.frame(c, z=BACK), look)
            for dz in (0.35, 1.05):
                add(k, "TankBand", lambda: tk._bm_lathe([(0, -0.04), (0.392, -0.04), (0.392, 0.04), (0, 0.04)], 20), tk.frame(c + BACK * dz, z=BACK),
                    HULL_B.but(cyl=0.392, panel=0.0, hatch=0.0, accent=0.0) if i % 2 == 0 else HAZ, lod=False)
            k.cyl("TankStrut", c + BACK * 0.7 - V(math.cos(a), 0, math.sin(a)) * 0.3, V(math.cos(a) * 0.43, 3.55, math.sin(a) * 0.43), 0.035,
                  look=STEEL, bone="Root", on=["Main"], sides=6, bevel=0.0, lod=False)

    # Radiators -------------------------------------------------------------------------------------------------------
    def radiators(self):
        k = self.k
        y = 4.1
        for s in (-1, 1):
            # Boom: a box truss beam out of the keel.
            add(k, "RadBoom", lambda: tk._bm_box(1.0, 0.16, 0.16), tk.frame((s * 0.92, y, 0)), GUN, bevel=0.015)
            add(k, "RadJoint", lambda: tk._bm_lathe([(0, -0.14), (0.16, -0.14), (0.16, 0.14), (0, 0.14)], 12), tk.frame((s * 1.42, y, 0), z=XAX),
                GREY, bevel=0.0)
            tilt = math.radians(12) * s
            for j in range(3):
                x0 = 1.55 + j * 0.92
                cx = s * (x0 + 0.44)
                f = tk.frame((cx, y, (x0 + 0.44 - 1.5) * math.sin(tilt) * s), rot=(0, -math.degrees(tilt), 0))
                add(k, "Radiator", lambda: tk._bm_box(0.86, 1.7, 0.035), f, RADIATOR, bevel=0.006)
                for yy in (-0.86, 0.86):
                    add(k, "RadFrame", lambda: tk._bm_box(0.9, 0.05, 0.06), f @ tk.frame((0, yy, 0)), HULL.but(panel=0.0, decal=0.0), bevel=0.008)
                add(k, "RadSpar", lambda: tk._bm_box(0.05, 1.76, 0.06), f @ tk.frame((s * 0.44, 0, 0)), GUN_PLAIN, bevel=0.0)
                for q in range(5):
                    for zz in (0.02, -0.02):
                        add(k, "Coolant", lambda: tk._bm_box(0.8, 0.014, 0.006), f @ tk.frame((0, -0.6 + q * 0.3, zz)), GLOW_CYAN, lod=False)
            tip = V(s * (1.55 + 3 * 0.92 + 0.02), y + 0.88, (3 * 0.92 + 0.05) * math.sin(abs(tilt)))
            self.light(tip, sk.glow("cyan", 2.0), r=0.035)

    # Engine ---------------------------------------------------------------------------------------------------------------
    def engine(self):
        k = self.k
        # Shadow shield: a broad disc between the tanks and the reactor.
        add(k, "Shield", lambda: tk._bm_lathe([(0, -0.1), (1.05, -0.1), (1.18, -0.03), (1.18, 0.03), (1.05, 0.1), (0, 0.1)], 32),
            tk.frame((0, 4.62, 0), z=BACK), GREY.but(cyl=1.18, decal=0.0), bevel=0.0)
        add(k, "ShieldHaz", lambda: tk._bm_lathe([(0, -0.035), (1.19, -0.035), (1.19, 0.035), (0, 0.035)], 32), tk.frame((0, 4.62, 0), z=BACK),
            HAZ, lod=False)
        # Engine block: an octagonal thrust frame that widens to the back.
        secs = [(4.72, section(1.5, 1.5, 0, 0.3, 0.3)), (5.1, section(2.0, 1.9, 0, 0.3, 0.3)), (6.25, section(2.36, 2.2, 0, 0.3, 0.3)),
                (6.62, section(2.2, 2.06, 0, 0.3, 0.3))]
        add(k, "EngineBlock", loft(secs), tk.frame(), HULL.but(panel=0.32, hatch=0.22, vent=0.12, accent=0.2, belly=-0.42), bevel=0.03)
        add(k, "EngineBand", loft([(5.28, section(2.12, 2.0, 0, 0.3, 0.3)), (5.52, section(2.18, 2.06, 0, 0.3, 0.3))]), tk.frame(), HULL_B.but(decal=0.0),
            bevel=0.012)
        add(k, "EngineHaz", loft([(6.3, section(2.38, 2.22, 0, 0.3, 0.3)), (6.4, section(2.38, 2.22, 0, 0.3, 0.3))]), tk.frame(), HAZ, lod=False)
        add(k, "ThrustPlate", lambda: tk._bm_lathe([(0, 0), (1.0, 0), (1.0, 0.08), (0, 0.08)], 8, math.pi / 8), tk.frame((0, 6.6, 0), z=BACK),
            GUN, bevel=0.015)
        # Side thrust pods (secondary manoeuvre engines) and vent stacks on the block.
        for s in (-1, 1):
            p = V(s * 1.08, 5.05, 0)
            add(k, "RcsPod", lambda: tk._bm_box(0.3, 0.34, 0.5), tk.frame(p), GUN, bevel=0.015)
            p = p + V(0, 0.2, 0)
            for dz in (-0.16, 0.16):
                add(k, "RcsBell", lambda: tk._bm_lathe([(0.04, 0), (0.07, 0), (0.1, 0.14), (0.085, 0.15), (0.05, 0.03), (0.0, 0.03)], 10),
                    tk.frame(p + V(s * 0.21, -0.2, dz), z=V(s, 0, 0)), STEEL, lod=False)
        rng = np.random.default_rng(23)
        for i in range(7):
            y = 5.95 + i * 0.045
            sx, sz = rng.uniform(0.16, 0.36), rng.uniform(0.04, 0.1)
            add(k, "EngGreeble", lambda sx=sx, sz=sz: tk._bm_box(sx, 0.04, sz), tk.frame((rng.uniform(-0.25, 0.25), y, 1.06 + sz / 2)), GUN_PLAIN,
                bevel=0.008, lod=False)
        self.light(V(0, 6.35, 1.13), GLOW_WHITE, r=0.04)
        self.light(V(0, 6.35, -1.13), GLOW_WHITE, r=0.04)
        # Three main bells on gimbal collars, glowing throats.
        bell = [(0.2, 0.0), (0.24, 0.06), (0.26, 0.16), (0.33, 0.4), (0.44, 0.82), (0.48, 1.0), (0.455, 1.01), (0.41, 0.82), (0.3, 0.4),
                (0.22, 0.16), (0.18, 0.08), (0.0, 0.08)]
        for i, a in enumerate((90, 210, 330)):
            c = V(math.cos(math.radians(a)) * 0.6, 6.68, math.sin(math.radians(a)) * 0.6)
            nf = tk.frame(c, z=BACK)
            add(k, "Gimbal", lambda: tk._bm_lathe([(0, -0.08), (0.3, -0.08), (0.32, 0.0), (0.3, 0.1), (0, 0.1)], 16), nf, GUN_PLAIN.but(rings=0.05))
            add(k, "Nozzle", lambda: tk._bm_lathe(bell, 24), nf @ tk.frame((0, 0, 0.08)), STEEL.but(rings=0.1, heat=1.0, cyl=0.48, rough=0.32))
            add(k, "NozzleGlow", lambda: tk._bm_lathe([(0, 0.1), (0.2, 0.12), (0.34, 0.52), (0, 0.52)], 16), nf @ tk.frame((0, 0, 0.08)), GLOW_ENGINE)
            for q in (0, 1):
                ang = math.radians(a + 90 + q * 180)
                o = V(math.cos(ang) * 0.24, 0, math.sin(ang) * 0.24)
                k.cyl("Actuator", c + o - BACK * 0.02, c + o * 1.35 + BACK * 0.38, 0.025, look=STEEL, bone="Root", on=["Main"], sides=6,
                      bevel=0.0, lod=False)
            self.sockets[f"Nozzle_{i + 1}"] = (c + BACK * 1.1, BACK)
        for s in (-1, 1):
            self.decals.append(("number", V(s * 1.12, 5.95, 0.1), BACK * s, UP, V(s, 0, 0), 0.62, 0.42))
        self.decals.append(("emblem", V(0, 5.55, 1.02), V(1, 0, 0), FRONT, UP, 0.56, 0.56))


# -------------------------------------------------------------------------------------------------------------
# Decals: text and the NTI emblem rendered into images, projected in the bake
# -------------------------------------------------------------------------------------------------------------

INK = (0.07, 0.08, 0.1)
NAVY = (0.12, 0.2, 0.38)


def _emit_mat(name, colour):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*tk.srgb(colour), 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    return mat


def _text(body, size, pos, colour, z=0.0):
    cu = bpy.data.curves.new("DecalText", "FONT")
    cu.body = body
    cu.size = size
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = 1.1
    o = C.link(bpy.data.objects.new("DecalText", cu))
    o.location = (pos[0], pos[1], z)
    cu.materials.append(_emit_mat("DecalInk", colour))
    return o


def _poly(points, pos, colour, z=0.0, name="DecalPoly"):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x + pos[0], y + pos[1], z) for x, y in points], [], [list(range(len(points)))])
    o = C.link(bpy.data.objects.new(name, me))
    me.materials.append(_emit_mat(name + "Mat", colour))
    return o


def _decal_objects(kind):
    """Objects for a decal in a 2 x (2 * aspect) ortho frame centred on the origin; returns (objects, aspect h/w)."""
    if kind == "name":
        objs = [_text("NEW DAWN", 0.27, (0, 0.085), INK), _text("NTI  ND-01    COLONY TRANSPORT", 0.085, (0, -0.165), INK),
                _poly(bk.rect_poly(1.86, 0.022), (0, -0.085), NAVY, name="Rule")]
        return objs, 0.25
    if kind == "number":
        num = _text("01", 1.0, (0.22, 0.0), NAVY)
        num.data.space_character = 0.85
        objs = [num, _poly(bk.rect_poly(0.07, 1.0), (-0.78, 0), INK, name="Bar"), _poly(bk.rect_poly(0.03, 1.0), (-0.64, 0), INK, name="Bar2")]
        return objs, 0.68
    # The NTI emblem (round 13 design) as flat colour.
    hexo = sk.chamfered_hex(0.98, 0.18)
    hexi = sk.chamfered_hex(0.84, 0.18)
    objs = [_poly(hexo, (0, 0), (0.2, 0.215, 0.24), name="Rim"), _poly(hexi, (0, 0), (0.15, 0.28, 0.52), 0.01, "Field"),
            _poly(sk.arc_band(0.82, 0.95, math.radians(52), math.radians(128), 24), (0, -1.08), (0.92, 0.94, 0.96), 0.02, "Arc"),
            _poly(sk.four_star(0.37, 0.1, 0.56), (0, 0.24), (1.0, 0.9, 0.62), 0.03, "Star"),
            _poly(bk.rect_poly(0.82, 0.24), (0, -0.5), (0.05, 0.055, 0.065), 0.02, "Bar"),
            _text("NTI", 0.2, (0, -0.5), (0.92, 0.94, 0.96), 0.03)]
    return objs, 1.0


def render_decals(folder):
    """Renders each decal kind to <folder>/decal_<kind>.png (RGBA, sRGB)."""
    scene = bpy.context.scene
    hidden = [(o, o.hide_render) for o in scene.objects]
    for o, _ in hidden:
        o.hide_render = True
    paths = {}
    old_world = scene.world
    for kind in ("name", "number", "emblem"):
        objs, aspect = _decal_objects(kind)
        cam_data = bpy.data.cameras.new("DecalCam")
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = 2.0
        cam = C.link(bpy.data.objects.new("DecalCam", cam_data))
        cam.location = (0, 0, 5)
        w = 1024
        h = int(w * aspect)
        C.setup_eevee(w, h, 16)
        scene.render.film_transparent = True
        scene.render.image_settings.color_mode = "RGBA"
        scene.camera = cam
        path = os.path.join(folder, f"decal_{kind}.png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        scene.render.film_transparent = False
        paths[kind] = path
        bk.remove(objs + [cam])
    for o, h in hidden:
        o.hide_render = h
    scene.world = old_world
    return paths


# -------------------------------------------------------------------------------------------------------------
# Mesh assembly (weighted normals) and attributes
# -------------------------------------------------------------------------------------------------------------

SMOOTH_DEGREES = 50.0


def finish_normals(obj):
    """Sharp edges above 50 degrees (box edges stay soft across their chamfer), then weighted normals: the big faces
    stay flat and the chamfers carry the highlight."""
    mesh = obj.data
    mesh.set_sharp_from_angle(angle=math.radians(SMOOTH_DEGREES))
    mod = obj.modifiers.new("WN", "WEIGHTED_NORMAL")
    mod.mode = "FACE_AREA"
    mod.weight = 50
    mod.keep_sharp = True
    mod.thresh = 0.01
    C.activate(obj)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def make_mesh(pieces, name, lod=False, attributes=False, with_uv=True):
    obj = tk.assemble(pieces, name, lod=lod, with_uv=with_uv)
    finish_normals(obj)
    if attributes:
        write_attributes(obj.data, pieces)
    return obj


def _f(look, key, default=0.0):
    return getattr(look, key, default)


def write_attributes(mesh, pieces):
    ranges = []
    b = 0
    for p in pieces:
        ranges.append((p, b, len(p.local), p.local))
        b += len(p.local)
    tk.write_attributes(mesh, ranges)
    n = len(mesh.vertices)
    att = {k: np.zeros((n, 4), dtype=np.float32) for k in ("Ms1", "Ms2", "Ms3", "Ms4")}
    rng = np.random.default_rng(7)
    for p, b, cnt, _ in ranges:
        s = slice(b, b + cnt)
        L = p.look
        win = _f(L, "win", (0.0, 0.0, 0.0))
        rows = _f(L, "rows", (0.0, 0.0, 0.0, 0.0))
        att["Ms1"][s] = (_f(L, "cyl"), _f(L, "plating"), _f(L, "accent"), _f(L, "hatch"))
        att["Ms2"][s] = (win[0], win[1], win[2], rng.uniform(0, 100))
        att["Ms3"][s] = rows
        belly = _f(L, "belly", None)
        att["Ms4"][s] = (_f(L, "heat"), _f(L, "vent"), _f(L, "decal"), 0.0 if belly is None else belly + 10.0)
    for k, arr in att.items():
        a = mesh.attributes.new(k, "FLOAT_COLOR", "POINT")
        a.data.foreach_set("color", arr.ravel())


# -------------------------------------------------------------------------------------------------------------
# The ship-scale bake shader
# -------------------------------------------------------------------------------------------------------------

GROOVE = 0.016          # seam half-width in world units (about one texel at 2048)
ASPECT = 1.6            # panels are longer than they are tall
STRIPE = 0.2            # hazard stripe period
BEVEL_R = 0.02
AO_DIST = 0.7
ACCENT = (0.64, 0.66, 0.7)
BELLY = (0.36, 0.38, 0.42)
_CH = {}


def _comb(g, x, y, z):
    n = g.node("ShaderNodeCombineXYZ")
    g.set(n.inputs[0], x)
    g.set(n.inputs[1], y)
    g.set(n.inputs[2], z)
    return n.outputs[0]


def _rand(g, a, b, seed):
    n = g.node("ShaderNodeTexWhiteNoise", noise_dimensions="4D")
    g.set(n.inputs["Vector"], _comb(g, a, b, 0.0))
    g.set(n.inputs["W"], seed)
    r, _, _ = g.rgb(n.outputs["Color"])
    return n.outputs["Value"], r


def _line(g, d, w):
    """1 on a line at distance d = 0, falling to 0 at w."""
    return g.map(d, 0.0, w, 1.0, 0.0)


def _plane(g, u, v, s, seed, hatch, vent):
    """Plating features on one projection plane: seams, panel randoms, split seams, hatch outlines, vent slats, stripes."""
    sy = s
    sx = g.mul(s, ASPECT)
    row = g.math("FLOOR", g.math("DIVIDE", v, sy))
    stagger = g.mul(g.math("FLOORED_MODULO", row, 2.0), 0.5)
    uu = g.add(g.math("DIVIDE", u, sx), stagger)
    col = g.math("FLOOR", uu)
    fu = g.sub(uu, col)
    fv = g.sub(g.math("DIVIDE", v, sy), row)
    du = g.mul(g.sub(0.5, g.math("ABSOLUTE", g.sub(fu, 0.5))), sx)
    dv = g.mul(g.sub(0.5, g.math("ABSOLUTE", g.sub(fv, 0.5))), sy)
    groove = g.math("MAXIMUM", _line(g, du, GROOVE), _line(g, dv, GROOVE))
    r1, r2 = _rand(g, col, row, seed)
    # Some panels are split once more across their length.
    split = g.mul(_line(g, g.mul(g.math("ABSOLUTE", g.sub(fu, 0.5)), sx), GROOVE), g.math("GREATER_THAN", r2, 0.62))
    groove = g.math("MAXIMUM", groove, split)
    # Hatches: an inset rectangle outline; vents: slats inside the same rectangle.
    ex = g.sub(g.mul(g.math("ABSOLUTE", g.sub(fu, 0.5)), sx), g.mul(sx, 0.3))
    ey = g.sub(g.mul(g.math("ABSOLUTE", g.sub(fv, 0.5)), sy), g.mul(sy, 0.27))
    d = g.math("MAXIMUM", ex, ey)
    is_hatch = g.math("GREATER_THAN", r1, g.sub(1.0, hatch))
    is_vent = g.math("LESS_THAN", r1, vent)
    hg = g.mul(_line(g, g.math("ABSOLUTE", d), GROOVE * 0.8), g.math("MAXIMUM", is_hatch, is_vent))
    inside = g.math("LESS_THAN", d, 0.0)
    hatch_in = g.mul(inside, is_hatch)
    slat = g.map(g.math("ABSOLUTE", g.sub(g.math("FRACT", g.math("DIVIDE", v, 0.045)), 0.5)), 0.18, 0.32)
    vent_in = g.mul(g.mul(inside, is_vent), g.math("MAXIMUM", g.math("LESS_THAN", r2, 0.9), 0.0))
    st = g.math("FRACT", g.math("DIVIDE", g.add(u, v), STRIPE))
    stripe = g.map(g.math("ABSOLUTE", g.sub(st, 0.5)), 0.23, 0.27, 1.0, 0.0)
    return {"groove": groove, "hg": hg, "hatch_in": hatch_in, "vent_in": vent_in, "slat": slat, "r1": r1, "r2": r2,
            "stripe": stripe}


def _windows(g, u, v, period, ww, wh, rows, seed):
    v1, v2, u0, u1 = rows
    on = g.math("GREATER_THAN", period, 0.001)
    p = g.math("MAXIMUM", period, 0.001)
    q = g.math("DIVIDE", u, p)
    wid = g.math("FLOOR", q)
    wu = g.mul(g.math("ABSOLUTE", g.sub(g.sub(q, wid), 0.5)), p)
    in_u = g.map(wu, g.sub(g.mul(ww, 0.5), 0.004), g.mul(ww, 0.5), 1.0, 0.0)
    rv = None
    for vc in (v1, v2):
        dv = g.math("ABSOLUTE", g.sub(v, vc))
        x = g.map(dv, g.sub(g.mul(wh, 0.5), 0.004), g.mul(wh, 0.5), 1.0, 0.0)
        rv = x if rv is None else g.math("MAXIMUM", rv, x)
    rnd, _ = _rand(g, wid, g.add(g.math("FLOOR", g.mul(v, 4.0)), 31.0), seed)
    lit = g.math("GREATER_THAN", rnd, 0.22)
    rng_ok = g.mul(g.math("GREATER_THAN", u, u0), g.math("LESS_THAN", u, u1))
    shape = g.mul(g.mul(in_u, rv), g.mul(on, rng_ok))
    return shape, g.mul(shape, g.add(0.35, g.mul(lit, g.add(0.45, g.mul(rnd, 0.4)))))


def bake_material(name, decals, decal_images):
    mat = C.new_material(name)
    nt = mat.node_tree
    nt.nodes.clear()
    g = surface.Graph(nt)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emission = nt.nodes.new("ShaderNodeEmission")

    loc = g.attr("Loc").outputs["Vector"]
    axes = [g.attr(n).outputs["Vector"] for n in ("AxX", "AxY", "AxZ")]
    paint = g.attr("Paint").outputs["Color"]
    emit = g.attr("Emit").outputs["Color"]
    matw = g.attr("MatW")
    cer, met, rub = g.rgb(matw.outputs["Color"])
    gla = matw.outputs["Alpha"]
    pan = g.attr("Pan")
    spacing, _, stripes_on = g.rgb(pan.outputs["Color"])
    wear_amt = pan.outputs["Alpha"]
    look = g.attr("Look2")
    rough0, grad, lens = g.rgb(look.outputs["Color"])
    rings = look.outputs["Alpha"]
    ms1 = g.attr("Ms1")
    cyl, plating, accent = g.rgb(ms1.outputs["Color"])
    hatch = ms1.outputs["Alpha"]
    ms2 = g.attr("Ms2")
    wper, ww, wh = g.rgb(ms2.outputs["Color"])
    seed = ms2.outputs["Alpha"]
    ms3 = g.attr("Ms3")
    r_v1, r_v2, r_u0 = g.rgb(ms3.outputs["Color"])
    r_u1 = ms3.outputs["Alpha"]
    ms4 = g.attr("Ms4")
    heat, vent, decal_ok = g.rgb(ms4.outputs["Color"])
    belly = ms4.outputs["Alpha"]

    geo = g.node("ShaderNodeNewGeometry")
    N = geo.outputs["True Normal"]
    P = geo.outputs["Position"]
    _, _, nz = tk._xyz(g, N)
    lx, ly, lz = tk._xyz(g, loc)

    # Projection weights on the piece's own axes.
    w = [g.math("POWER", g.math("ABSOLUTE", tk._dot(g, N, a)), 8.0) for a in axes]
    wsum = g.add(w[0], w[1], w[2], 1e-6)
    w = [g.math("DIVIDE", x, wsum) for x in w]
    # Angular mapping for revolved pieces: u = angle x radius (cell count rounded so the seam closes).
    cyl_on = g.math("GREATER_THAN", cyl, 0.001)
    theta = g.math("ARCTAN2", ly, lx)
    rr = g.math("SQRT", g.add(g.mul(lx, lx), g.mul(ly, ly)))
    circ = g.mul(g.math("MAXIMUM", cyl, 0.001), 2 * math.pi)
    sp = g.math("MAXIMUM", spacing, 0.001)
    cells = g.math("MAXIMUM", g.math("ROUND", g.math("DIVIDE", circ, g.mul(sp, ASPECT))), 1.0)
    ucyl = g.mul(g.math("DIVIDE", theta, 2 * math.pi), g.mul(cells, g.mul(sp, ASPECT)))

    def pick(a, b):
        return g.add(g.mul(a, g.sub(1.0, cyl_on)), g.mul(b, cyl_on))
    planes = [(pick(ly, ucyl), lz), (pick(lx, ucyl), lz), (pick(lx, ucyl), pick(ly, rr))]
    enable = g.math("GREATER_THAN", spacing, 0.0005)
    acc = {}
    for i, (wi, (u, v)) in enumerate(zip(w, planes)):
        f = _plane(g, u, v, sp, g.add(seed, i * 13.0), hatch, vent)
        for key, val in f.items():
            term = g.mul(val, wi)
            acc[key] = term if key not in acc else g.add(acc[key], term)
    groove = g.mul(g.math("MAXIMUM", acc["groove"], acc["hg"]), enable)
    hatch_in = g.mul(acc["hatch_in"], enable)
    vent_in = g.mul(acc["vent_in"], enable)
    slat = acc["slat"]
    r1, r2 = acc["r1"], acc["r2"]
    stripe = acc["stripe"]
    # Ring grooves (containers' ribs, bells, lift tubes).
    ring_on = g.math("GREATER_THAN", rings, 0.0005)
    rd = tk._cell(g, lz, g.math("MAXIMUM", rings, 0.001))
    ring = g.mul(_line(g, rd, GROOVE * 1.1), ring_on)
    groove = g.math("MAXIMUM", groove, ring)

    # Windows on the side planes (box: local X faces along local Y; revolved: the outer wall round the axis).
    wper_c = g.add(g.mul(wper, g.sub(1.0, cyl_on)),
                   g.mul(g.math("DIVIDE", circ, g.math("MAXIMUM", g.math("ROUND", g.math("DIVIDE", circ, g.math("MAXIMUM", wper, 0.001))), 1.0)), cyl_on))
    wu = pick(ly, g.mul(g.math("DIVIDE", theta, 2 * math.pi), circ))
    wweight = g.add(w[0], g.mul(w[1], cyl_on))
    wweight = g.map(wweight, 0.85, 0.95)
    outer = g.add(g.sub(1.0, cyl_on), g.mul(cyl_on, g.math("GREATER_THAN", rr, g.sub(cyl, 0.06))))
    wshape, wlit = _windows(g, wu, lz, g.mul(wper_c, g.math("GREATER_THAN", wper, 0.001)), ww, wh, (r_v1, r_v2, r_u0, r_u1), seed)
    wshape = g.mul(g.mul(wshape, wweight), outer)
    wlit = g.mul(g.mul(wlit, wweight), outer)

    # Edges.
    bev = g.node("ShaderNodeBevel")
    bev.inputs["Radius"].default_value = BEVEL_R
    bev.samples = 8
    nb = bev.outputs["Normal"]
    edge = g.map(g.sub(1.0, tk._dot(g, nb, N)), 0.004, 0.08)
    chip_n = g.noise(P, 9.0, detail=3.0, rough=0.6, w=3.0)
    chip = g.mul(g.map(g.add(chip_n, g.mul(edge, 0.5)), 0.8, 0.86), g.math("MINIMUM", g.mul(edge, 4.0), 1.0))
    chip = g.mul(chip, g.mul(wear_amt, 0.6))
    scr = g.mul(surface.scratches(g, P, 22.0, 11.0), g.mul(wear_amt, 0.6))
    paint_w = g.add(cer, rub)

    # Value: soft top-to-underside ramp, per-panel tint, light-grey accent panels, large-scale mottling.
    upper = g.map(nz, -0.1, 0.8)
    lower = g.map(nz, -0.9, -0.1)
    val = g.add(tk.UNDER, g.mul(g.sub(tk.SIDE, tk.UNDER), lower))
    val = g.add(val, g.mul(g.sub(tk.TOP, val), upper))
    val = g.add(1.0, g.mul(g.sub(val, 1.0), grad))
    tint = g.add(1.0, g.mul(g.mul(g.sub(r1, 0.5), 0.13), g.mul(plating, enable)))
    mott = g.add(0.98, g.mul(g.noise(P, 0.9, detail=2.0, w=5.0), 0.04))
    val = g.mul(g.mul(val, tint), mott)
    base = g.vscale(paint, val)
    is_acc = g.mul(g.math("LESS_THAN", r2, accent), enable)
    acc_col = g.vscale(tuple(tk.srgb(ACCENT)), val)
    base = tk._mix_rgb(g, g.mul(is_acc, g.math("GREATER_THAN", cer, 0.5)), base, acc_col)
    base = tk._mix_rgb(g, g.mul(is_acc, g.sub(1.0, g.math("GREATER_THAN", cer, 0.5))), base, g.vscale(base, 0.8))
    # Livery: a grey belly under a colony-blue pinstripe (two-tone hull, so the white masses get a value structure).
    belly_on = g.math("GREATER_THAN", belly, 0.5)
    bz = g.sub(belly, 10.0)
    below = g.mul(g.map(lz, g.add(bz, 0.004), g.sub(bz, 0.004)), belly_on)
    base = tk._mix_rgb(g, below, base, g.vscale(tuple(tk.srgb(BELLY)), val))
    pin = g.mul(g.map(g.math("ABSOLUTE", g.sub(lz, g.add(bz, 0.05))), 0.022, 0.016), belly_on)
    base = tk._mix_rgb(g, pin, base, g.vscale(tuple(tk.srgb(tk.PAL["blue"])), val))
    base = g.vscale(base, g.sub(1.0, g.mul(hatch_in, 0.07)))
    base = g.vscale(base, g.sub(1.0, g.mul(vent_in, g.add(0.35, g.mul(slat, 0.4)))))
    # Heat tint on the bells: straw, then blue-violet towards the exit.
    hz = g.math("MINIMUM", g.mul(lz, heat), 1.0)
    heat_on = g.math("GREATER_THAN", heat, 0.001)
    straw = tk._mix_rgb(g, g.map(hz, 0.1, 0.45), base, g.vscale(tuple(tk.srgb((0.5, 0.42, 0.28))), val))
    hot = tk._mix_rgb(g, g.map(hz, 0.4, 0.95), straw, g.vscale(tuple(tk.srgb((0.2, 0.2, 0.32))), val))
    base = tk._mix_rgb(g, heat_on, base, hot)
    # Hazard stripes, glass, windows.
    haz = tk._mix_rgb(g, stripe, (*tk.HAZ_DARK, 1.0), paint)
    base = tk._mix_rgb(g, stripes_on, base, g.vscale(haz, val))
    base = tk._mix_rgb(g, gla, base, g.vscale(tuple(tk.srgb(tk.PAL["glass"])), g.add(0.6, g.mul(upper, 0.8))))
    base = tk._mix_rgb(g, wshape, base, (0.02, 0.025, 0.035, 1.0))
    # Decals (painted: no bump).
    dec_a = None
    for kind, origin, au, av, an, w_, h_ in decals:
        img = decal_images[kind]
        d = g.vec("SUBTRACT", P, tuple(origin)).outputs[0]
        pu = g.add(g.math("DIVIDE", tk._dot(g, d, tuple(au)), w_), 0.5)
        pv = g.add(g.math("DIVIDE", tk._dot(g, d, tuple(av)), h_), 0.5)
        pn = tk._dot(g, d, tuple(an))
        inside = g.mul(g.mul(g.math("GREATER_THAN", pu, 0.0), g.math("LESS_THAN", pu, 1.0)),
                       g.mul(g.math("GREATER_THAN", pv, 0.0), g.math("LESS_THAN", pv, 1.0)))
        facing = g.math("GREATER_THAN", tk._dot(g, N, tuple(an)), 0.55)
        near = g.math("LESS_THAN", g.math("ABSOLUTE", pn), 0.16)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        tex.extension = "CLIP"
        tex.interpolation = "Cubic"
        g.set(tex.inputs["Vector"], _comb(g, pu, pv, 0.0))
        a = g.mul(g.mul(tex.outputs["Alpha"], g.mul(inside, g.mul(facing, near))), decal_ok)
        base = tk._mix_rgb(g, g.mul(a, 0.92), base, g.vscale(tex.outputs["Color"], g.add(0.7, g.mul(val, 0.3))))
        dec_a = a if dec_a is None else g.math("MAXIMUM", dec_a, a)
    # Seams, chips and scratches.
    base = g.vscale(base, g.sub(1.0, g.mul(groove, 0.5)))
    base = tk._mix_rgb(g, g.mul(chip, paint_w), base, (*tk.SUBSTRATE, 1.0))
    base = tk._mix_rgb(g, g.mul(g.math("MAXIMUM", chip, g.mul(edge, 0.25)), met), base, (*tk.POLISHED, 1.0))
    base = tk._mix_rgb(g, g.mul(scr, g.mul(met, 0.5)), base, (*tk.POLISHED, 1.0))
    base = g.vscale(base, g.add(1.0, g.mul(g.mul(edge, paint_w), 0.1)))
    # Grime from occlusion.
    ao_node = g.node("ShaderNodeAmbientOcclusion")
    ao_node.inputs["Distance"].default_value = AO_DIST
    ao_node.samples = 16
    ao = ao_node.outputs["AO"]
    grime = g.sub(1.0, ao)
    base = g.vscale(base, g.sub(1.0, g.mul(grime, 0.5)))

    # Emission: glow pieces (lenses hottest in their centre) plus lit windows.
    lens_on = g.math("GREATER_THAN", lens, 0.0005)
    rad = g.math("DIVIDE", g.math("SQRT", g.add(g.mul(lx, lx), g.mul(ly, ly))), g.math("MAXIMUM", lens, 0.001))
    lf = g.map(rad, 0.0, 1.0, 1.35, 0.45)
    lf = g.add(g.mul(lf, lens_on), g.sub(1.0, lens_on))
    final_emit = g.vec("ADD", g.vscale(emit, lf), g.vscale(tuple(tk.srgb(WIN_LIGHT)), g.mul(wlit, 1.3))).outputs[0]

    # Roughness: per-panel variation (painted panels vs bare metal), glossier glass and decals.
    rough = g.add(rough0, g.mul(groove, 0.12), g.mul(grime, 0.08), g.mul(g.mul(g.sub(r2, 0.5), 0.14), plating),
                  g.mul(is_acc, 0.06), g.mul(vent_in, 0.15))
    rough = g.sub(rough, g.mul(g.add(chip, g.mul(scr, 0.6)), g.mul(met, 0.22)))
    rough = g.sub(rough, g.mul(wshape, 0.3))
    if dec_a is not None:
        rough = g.sub(rough, g.mul(dec_a, 0.08))
    rough = g.math("MINIMUM", g.math("MAXIMUM", rough, 0.05), 0.95)

    fam = g.node("ShaderNodeCombineColor")
    g.set(fam.inputs[0], met)
    g.set(fam.inputs[1], cer)
    g.set(fam.inputs[2], 0.0)

    dent = g.sub(g.noise(P, 6.0, detail=1.0, w=41.0), 0.5)
    height = g.add(g.mul(groove, -0.0035), g.mul(hatch_in, -0.0012), g.mul(g.mul(vent_in, slat), -0.0025),
                   g.mul(scr, -0.0002), g.mul(g.mul(chip, paint_w), -0.0006), g.mul(g.mul(dent, met), 0.0004),
                   g.mul(wshape, -0.002), g.mul(g.mul(g.sub(r1, 0.5), plating), 0.0012))
    bump = g.node("ShaderNodeBump")
    g.set(bump.inputs["Height"], height)
    bump.inputs["Distance"].default_value = 1.0
    bump.inputs["Strength"].default_value = 1.0
    g.set(bump.inputs["Normal"], nb)
    diffuse = nt.nodes.new("ShaderNodeBsdfDiffuse")
    nt.links.new(bump.outputs["Normal"], diffuse.inputs["Normal"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])
    ao_col = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g.set(ao_col.inputs[i], ao)
    rough_col = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g.set(rough_col.inputs[i], rough)
    _CH[mat.name] = {"albedo": base, "emission": final_emit, "ao": ao_col.outputs[0], "rough": rough_col.outputs[0],
                     "fam": fam.outputs[0], "normal": diffuse.outputs[0], "_emission": emission, "_out": out}
    return mat


def _channel(mat, channel):
    ch = _CH[mat.name]
    nt = mat.node_tree
    out, emission = ch["_out"], ch["_emission"]
    for link in list(out.inputs["Surface"].links) + list(emission.inputs["Color"].links):
        nt.links.remove(link)
    if channel == "normal":
        nt.links.new(ch["normal"], out.inputs["Surface"])
        return
    nt.links.new(ch[channel], emission.inputs["Color"])
    nt.links.new(emission.outputs[0], out.inputs["Surface"])


# Texel weight per piece name: hidden or minor parts get fewer texels, so the hull, ring and engine get more.
UV_WEIGHT = {"Keel": 0.55, "Container": 0.75, "BayCeiling": 0.4, "BayWall": 0.7, "BayEnd": 0.5, "Racked": 0.5, "RackedShield": 0.4,
             "RackClamp": 0.4, "Conduit": 0.5, "TankStrut": 0.4, "Greeble": 0.6, "EngGreeble": 0.6, "Gimbal": 0.6, "ThrustPlate": 0.6,
             "DoorInner": 0.5, "Hinge": 0.4, "Actuator": 0.4, "Coolant": 0.3, "Mullion": 0.4, "NavLight": 0.3, "RingLight": 0.3,
             "HubGlow": 0.3, "Antenna": 0.4, "Hub": 0.7, "Bearing": 0.7, "Liner": 0.6, "RackFrame": 0.6, "RcsBell": 0.5, "CollarHaz": 0.5,
             "BayStrip": 0.3, "BayCeilLight": 0.3, "NozzleGlow": 0.5, "Radiator": 0.8, "DishFeed": 0.4, "DishHorn": 0.4, "DishYoke": 0.5}


def unwrap(kit, margin=0.0016):
    """tower_kit.unwrap_kit with a tighter, concave-aware pack (more texels on a 2048 hero map)."""
    obj = tk.assemble(kit.pieces, kit.name + "_Atlas", with_uv=True)
    C.activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=margin, area_weight=0.0, correct_aspect=True,
                             scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.average_islands_scale()
    bpy.ops.object.mode_set(mode="OBJECT")
    uv = np.empty(len(obj.data.loops) * 2, dtype=np.float32)
    obj.data.uv_layers.active.data.foreach_get("uv", uv)
    uv = uv.reshape(-1, 2)
    k = 0
    for p in kit.pieces:
        n = p.loops()
        wgt = UV_WEIGHT.get(p.name, 1.0)
        if wgt != 1.0:
            c = uv[k:k + n].mean(axis=0)
            uv[k:k + n] = c + (uv[k:k + n] - c) * wgt
        k += n
    obj.data.uv_layers.active.data.foreach_set("uv", uv.ravel())
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, margin=margin, shape_method="CONCAVE")
    bpy.ops.object.mode_set(mode="OBJECT")
    uv = np.empty(len(obj.data.loops) * 2, dtype=np.float32)
    obj.data.uv_layers.active.data.foreach_get("uv", uv)
    uv = uv.reshape(-1, 2)
    k = 0
    for p in kit.pieces:
        n = p.loops()
        p.uv = [tuple(x) for x in uv[k:k + n]]
        k += n
    stats = tk.uv_stats(obj)
    bpy.data.objects.remove(obj)
    return stats


def bake(kit, decals, out_dir, size=TEX, low=LOW, samples=16):
    os.makedirs(out_dir, exist_ok=True)
    tmp = tempfile.mkdtemp(prefix="ms2_decals_")
    images = {k: bpy.data.images.load(p) for k, p in render_decals(tmp).items()}
    for img in images.values():
        img.colorspace_settings.name = "sRGB"
    target = make_mesh(kit.pieces, kit.name + "_Bake", attributes=True)
    mat = bake_material(kit.name + "_BakeMat", decals, images)
    target.data.materials.append(mat)
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    mat.node_tree.nodes.active = node
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.use_denoising = False
    scene.world = scene.world or bpy.data.worlds.new("BakeWorld")
    b = scene.render.bake
    b.use_selected_to_active = False
    b.margin = 12
    b.use_clear = True
    b.target = "IMAGE_TEXTURES"
    hidden = [(o, o.hide_render) for o in scene.objects if o is not target]
    for o, _ in hidden:
        o.hide_render = True
    C.activate(target)

    def one(kind, channel, space, spp):
        scene.cycles.samples = spp
        img = bpy.data.images.new(f"{kit.name}_{channel}", size, size, alpha=False, float_buffer=True)
        img.colorspace_settings.name = space
        node.image = img
        _channel(mat, channel)
        if kind == "NORMAL":
            bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")
        else:
            bpy.ops.object.bake(type="EMIT")
        arr = np.array(img.pixels[:], dtype=np.float32).reshape(size, size, 4)
        bpy.data.images.remove(img)
        C.log("baked", kit.name, channel)
        return arr

    maps = {"albedo": one("EMIT", "albedo", "Linear Rec.709", samples),
            "emission": one("EMIT", "emission", "Linear Rec.709", 4),
            "normal": one("NORMAL", "normal", "Non-Color", samples)}
    ao = one("EMIT", "ao", "Non-Color", samples)
    rough = one("EMIT", "rough", "Non-Color", samples)
    fam = one("EMIT", "fam", "Non-Color", 1)
    maps["mask"] = np.clip(np.dstack([ao[..., 0], 1.0 - rough[..., 0], fam[..., 0], fam[..., 1]]), 0, 1)
    for o, h in hidden:
        o.hide_render = h
    bpy.data.objects.remove(target)
    for img in images.values():
        bpy.data.images.remove(img)
    suffix = {"albedo": "Albedo", "emission": "Emission", "normal": "Normal", "mask": "Mask"}
    spaces = {"albedo": "sRGB", "emission": "sRGB", "normal": "Non-Color", "mask": "Non-Color"}
    paths = {}
    for key, arr in maps.items():
        paths[key] = tk._save(arr, os.path.join(out_dir, f"{kit.name}_{suffix[key]}.png"), spaces[key])
        tk._save(C.downsample(arr, size // low, normal=key == "normal"), os.path.join(out_dir, "Low", f"{kit.name}_{suffix[key]}.png"),
                 spaces[key])
    return paths


# -------------------------------------------------------------------------------------------------------------
# Build and export
# -------------------------------------------------------------------------------------------------------------

def rigged(ship, kit, mat, label="", lod=False):
    arm = tk.build_armature(f"{kit.name}_Rig{label}", ship.bone_list)
    mesh = make_mesh(kit.pieces, kit.name + ("_LOD1" if lod else "") + label, lod=lod)
    mesh.data.materials.append(mat)
    tk.skin_to(mesh, arm)
    return arm, mesh


def spin(arm):
    def keys(t, f):
        return {"Ring": {"rot": (BACK, 2 * math.pi * t)}}
    return bk.clip(arm, "Spin", 360, keys, loop=True)


def build(bake_maps=True, fast=False):
    bk.clear_actions()
    ship = Mothership()
    kit = ship.pieces()
    out_dir = os.path.join(C.EXPORTS, NAME)
    uv = unwrap(kit)
    paths = {s.lower(): os.path.join(out_dir, f"{NAME}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    if bake_maps or not all(os.path.exists(p) for p in paths.values()):
        paths = bake(kit, ship.decals, out_dir, *((1024, 512, 6) if fast else (TEX, LOW, 16)))
    entries, detail = tk.detail_entries(uv["world_per_uv"])
    mat = C.game_material(NAME + "_Mat", paths, 1.5, 0.45, entries)
    report = {"asset": NAME, "round": 19, "texture_size": TEX, "low_size": LOW, "maps": ["Albedo", "Normal", "Emission", "Mask"],
              "mask": "R AO, G smoothness, B metal detail mask, A ceramic detail mask", "uv": uv, "detail": detail}
    # LOD0
    arm, mesh = rigged(ship, kit, mat)
    act = spin(arm)
    bk.rest_pose(arm)
    socks = sk.socket_empties(ship.sockets, parent=arm)
    bk.export_fbx(os.path.join(out_dir, NAME + ".fbx"), [arm, mesh] + socks, animated=True)
    tris0 = C.tri_count(mesh)
    bk.remove([arm, mesh] + socks)
    bpy.data.actions.remove(act)
    # LOD1: the same rig and clip, no chamfers, no greebles (shares the maps).
    arm, lod = rigged(ship, kit, mat, lod=True)
    act = spin(arm)
    bk.rest_pose(arm)
    bk.export_fbx(os.path.join(out_dir, NAME + "_LOD1.fbx"), [arm, lod], animated=True)
    tris1 = C.tri_count(lod)
    bk.remove([arm, lod])
    bpy.data.actions.remove(act)
    report.update({
        "fbx": NAME + ".fbx", "lod1_fbx": NAME + "_LOD1.fbx", "meshes": {NAME: tris0, NAME + "_LOD1": tris1}, "tris": tris0,
        "lod1": tris1, "lod1_ratio": round(tris1 / tris0, 3), "bones": ["Root", "Ring"], "deform_bones": ["Root", "Ring"],
        "clips": {"Spin": 360},
        "sockets": {n: [round(c, 3) for c in (s[0] if isinstance(s, tuple) else s)] for n, s in ship.sockets.items()},
        "pivot": "ship centre (between the cargo racks); nose towards -Y in Blender (Unity +Z)",
        "length": 14.95})
    tk.write_json(os.path.join(out_dir, NAME + "_report.json"), report)
    C.log(NAME, tris0, "LOD1", tris1)
    return ship, kit, mat, report


# -------------------------------------------------------------------------------------------------------------
# Previews: space lighting, cutscene-like angles, old vs new
# -------------------------------------------------------------------------------------------------------------

def lit_planet(centre, radius, sun_dir):
    """S-69-CRYS backdrop shaded in the material (emission only), so the ship's shadow never lands on it."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=96, ring_count=48, radius=radius, location=centre)
    obj = bpy.context.active_object
    bpy.ops.object.shade_smooth()
    mat = C.new_material("PlanetLit")
    nt = mat.node_tree
    nt.nodes.clear()
    g = surface.Graph(nt)
    geo = g.node("ShaderNodeNewGeometry")
    lam = g.math("MAXIMUM", tk._dot(g, geo.outputs["Normal"], tuple(-Vector(sun_dir).normalized())), 0.0)
    ramp = g.node("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.05, 0.05, 0.16, 1)
    ramp.color_ramp.elements[1].color = (0.24, 0.14, 0.36, 1)
    g.set(ramp.inputs["Fac"], g.noise(geo.outputs["Position"], 0.05, detail=8.0))
    col = g.vscale(ramp.outputs["Color"], g.add(0.05, g.mul(lam, 1.6)))
    lw = g.node("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.25
    rim = g.math("POWER", lw.outputs["Facing"], 3.0)
    col = g.vec("ADD", col, g.vscale((0.35, 0.55, 1.0), g.mul(rim, g.add(0.15, lam)))).outputs[0]
    em = nt.nodes.new("ShaderNodeEmission")
    g.set(em.inputs["Color"], col)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    obj.data.materials.append(mat)
    obj.visible_shadow = False
    return obj


def space_stage():
    sk.space_world(1.0)
    objs = [lit_planet(V(70, 40, -70), 60, V(0.6, 0.45, -0.5))]
    for name, energy, colour, d, shadow in (("Key", 5.0, (1.0, 0.96, 0.9), V(0.6, 0.45, -0.5), True),
                                             ("Rim", 2.6, (0.5, 0.7, 1.0), V(-0.5, -0.6, -0.35), False),
                                             ("Bounce", 0.35, (0.45, 0.4, 0.9), V(-0.3, -0.2, 0.9), False)):
        L = bpy.data.lights.new(name, "SUN")
        L.energy = energy
        L.color = colour
        L.angle = math.radians(0.6)
        L.use_shadow = shadow
        o = C.link(bpy.data.objects.new(name, L))
        C.look_at(o, o.location + d)
        objs.append(o)
    return objs


VIEWS = [  # name, target, distance, azimuth, elevation, lens, size
    ("hero", V(0, 0, 0), 21.0, -55, 14, 50, (1600, 900)),
    ("side", V(0, 0, 0), 24.0, -90, 4, 50, (1600, 900)),
    ("under", V(0, 0.5, -0.5), 16.0, -35, -24, 40, (1600, 900)),
    ("bay", V(0, 0.55, -1.0), 7.0, -60, -38, 50, (1600, 900)),
    ("rear", V(0, 4.0, 0), 13.0, 148, 16, 40, (1600, 900)),
    ("nose", V(0, -5.6, 0.2), 6.2, -38, 16, 50, (1600, 900)),
    ("ring", V(0.5, -2.1, 1.2), 7.5, -60, 30, 50, (1600, 900)),
    ("top", V(0, 0, 0), 22.0, -20, 62, 50, (1600, 900)),
    ("far", V(0, 0, 0), 26.0, -55, 14, 50, (320, 180)),
]


def render_views(objs_arm, out, prefix, views=None):
    arm = objs_arm
    if arm is not None and arm.animation_data and arm.animation_data.action:
        bpy.context.scene.frame_set(20)
    paths = {}
    for name, target, dist, az, el, lens, (w, h) in VIEWS:
        if views and name not in views:
            continue
        cam = C.closeup_camera(target, dist, azimuth=az, elevation=el, lens=lens)
        cam.data.clip_end = 600
        p = os.path.join(out, f"{prefix}_{name}.png")
        C.render(cam, p, w, h, samples=64)
        paths[name] = p
        bk.remove([cam])
    return paths


def import_old(folder):
    """The round-13 export, re-imported with its own maps (for the side-by-side)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(folder, NAME + ".fbx"))
    objs = [o for o in bpy.data.objects if o not in before]
    tex = {s.lower(): os.path.join(folder, f"{NAME}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    mat = C.game_material("OldMat", tex, 1.5, 0.45, [])
    for o in objs:
        if o.type == "MESH":
            o.data.materials.clear()
            o.data.materials.append(mat)
    return objs


def previews(ship, kit, mat, out, old=None):
    stage = space_stage()
    arm, mesh = rigged(ship, kit, mat, "P")
    act = spin(arm)
    arm.animation_data.action = act
    new = render_views(arm, out, "ms2")
    # LOD1 at the far view and the hero view.
    mesh.hide_render = True
    arm2, lod = rigged(ship, kit, mat, "PL", lod=True)
    render_views(None, out, "ms2_lod1", views=("hero", "far"))
    bk.remove([arm2, lod])
    mesh.hide_render = False
    if old and os.path.exists(os.path.join(old, NAME + ".fbx")):
        arm.hide_render = mesh.hide_render = True
        olds = import_old(old)
        prev = render_views(None, out, "old", views=("hero", "side", "under", "rear"))
        for name in ("hero", "side", "under", "rear"):
            C.contact_sheet([prev[name], new[name]], os.path.join(out, f"compare_{name}.png"), 2, 960)
        bk.remove(olds)
        arm.hide_render = mesh.hide_render = False
    bk.remove(stage + [arm, mesh])
    C.preview_world()


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    C.preview_world()
    tk.metal_detail_map()
    ship, kit, mat, report = build(bake_maps=not args.get("nobake"), fast=bool(args.get("fast")))
    if not args.get("norender"):
        previews(ship, kit, mat, out, args.get("old"))
    C.save_blend(NAME)


if __name__ == "__main__":
    main()
