"""Round 37: the expanded colony base (radar, landing zone, drone bays, storage). Replaces round 9's base export.

Humanity's forward base on S-69, about 1.65x the round-9 footprint, laid out round a compact armoured command post at
the front (the face the enemies attack, -Y = Unity +Z), with the support yards behind it:

    front (road)   ramp, command post on its prow, a turret hardpoint at each front corner
    middle         drone bays (2x2 hangar wells, left)        storage: tanks, pipes, Crysium silo (right)
    back (camera)  radar array (left)   habitat + greenhouse   containers   landing zone + shuttle (right)

Every module is its own static object with its pivot on its foot (same convention as round 9, so the cutscene
timelapse and the upgrade screen keep working). The parts Unity animates are separate objects whose FBX node rotation
is the identity (see `upright`): the radar head `Base_Dish` (turns about the vertical mast axis only), the blinking
`Base_Beacon`, the pulsing/turning `Base_Crystal`, the parked `Base_Shuttle` and the four hinged `Base_BayDoor_N`.
The damage overlay and the wreck are separate FBX files, as before. Socket names of round 9 are kept; drone docks,
the landing spot and the beacon get new `Socket_*` empties.

The base turret (`Tower_BaseTurret`) is still built by `build_base.py`; this script only replaces `Exports/Base`.

Out: `Exports/Base/Base.fbx` (modules + LOD1 + sockets), `Base_Damage.fbx`, `Base_Wreck.fbx`, one 1024 map set
(+ Low 512), `Base_report.json`; previews in `ArtSource/Previews/Base37/`; `ArtSource/Blend/Base2.blend`.

Run: blender --background --factory-startup --python Tools/Blender/build_base2.py -- [--preview <dir>] [--norender]
     [--nobake] [--oldbase <folder with the round-9 Base.fbx + maps, for the old/new comparison>]
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
import tower_kit as tk  # noqa: E402
from base_kit import V, UP  # noqa: E402

NAME = "Base"
OUT = os.path.join(C.EXPORTS, NAME)
PREVIEW = os.path.join(C.REPO, "ArtSource", "Previews", "Base37")
DECK_Z = 0.23
TEX, TEX_LOW = 2048, 1024
# Island margin in UV units (2.5 px at 2048): with ~1000 small pieces the margins, not the islands, fill a coarser
# atlas (0.0025 gave 18 % coverage).
UV_MARGIN = 0.00125
# Bake spacing between modules: wider than the largest module (the 5.1 m deck) so no module shades another.
BAKE_SPACING = 8.0

# Layout (Blender metres; the pivot of the whole base is the origin, on the ground under the command post's back).
REAR = ((0.0, 0.83), (5.1, 3.06), 0.62)     # centre, size, chamfer: y -0.70 .. 2.36
PROW = ((0.0, -1.03), (2.0, 0.66), 0.24)    # y -1.36 .. -0.70, chamfered on the road side only
CMD = V(0.0, -0.5, DECK_Z)
BAYS = V(-1.66, 0.66, DECK_Z)
WELL = 0.6
WALL = 0.08
WALL_H = 0.3
RADAR = V(-1.78, 1.86, DECK_Z)
MAST_TOP = 1.86                              # above the deck
HEAD = RADAR + V(0, 0, MAST_TOP)
BEACON_UP = 0.74                             # beacon foot above the radar head pivot
PAD = V(1.58, 1.56, DECK_Z)
PAD_R = 0.8
PAD_TOP = 0.075
CORE = V(1.22, 0.34, DECK_Z)
STORE = V(2.18, 0.23, DECK_Z)
HAB = V(-0.47, 1.62, DECK_Z)
HARD_X, HARD_Y = 1.78, -0.36
SHUTTLE_YAW = 32.0
DOOR_OPEN = 100.0                            # degrees the bay lids stand open (rest pose of the export)


def well_centre(i):
    """Bay well i (0..3): 0 front-left, 1 front-right, 2 back-left, 3 back-right (Blender X/Y, -Y = road)."""
    col, row = i % 2, i // 2
    off = (WELL + WALL) / 2
    return BAYS + V((-1 if col == 0 else 1) * off, (-1 if row == 0 else 1) * off, 0)


def door_hinge(i):
    """Hinge line of lid i: on the outer wall of its column, at the wall top; `side` -1 for the left column."""
    side = -1 if i % 2 == 0 else 1
    c = well_centre(i)
    return c + V(side * (WELL / 2 + WALL / 2), 0, WALL_H + 0.0175), side


MODULES = ["Deck", "Command", "Hardpoints", "Habitat", "Radar", "Dish", "Beacon", "Bays", "BayDoor_1", "BayDoor_2",
           "BayDoor_3", "BayDoor_4", "Storage", "Core", "Crystal", "Pad", "PadGear", "Shuttle"]
PIVOTS = {"Deck": V(0, 0, 0), "Command": CMD, "Hardpoints": V(0, HARD_Y, DECK_Z), "Habitat": HAB, "Radar": RADAR,
          "Dish": HEAD, "Beacon": HEAD + V(0, 0, BEACON_UP), "Bays": BAYS, "Storage": STORE, "Core": CORE,
          "Crystal": CORE + V(0, 0, 0.36), "Pad": PAD, "PadGear": PAD, "Shuttle": PAD + V(0, 0, PAD_TOP)}
for _i in range(4):
    PIVOTS[f"BayDoor_{_i + 1}"] = door_hinge(_i)[0]
# Separate parts Unity animates: their FBX node carries no rotation, so local +Y is world up (see `upright`).
ANIMATED = {"Dish", "Beacon", "Crystal", "Shuttle", "BayDoor_1", "BayDoor_2", "BayDoor_3", "BayDoor_4"}

# Round-9 sockets (names kept; root empties as before) at their new places.
SOCKETS = {"HeroPost": V(0, -1.95, 0.0), "TurretLink_L": V(-HARD_X - 0.3, HARD_Y, 0.34),
           "TurretLink_R": V(HARD_X + 0.3, HARD_Y, 0.34),
           "Smoke_Command": V(0.1, -0.55, 1.45), "Smoke_Core": V(CORE.x, CORE.y, 1.55), "Smoke_Deck": V(-0.6, -0.95, 0.3),
           "Smoke_Bays": V(BAYS.x, BAYS.y, 0.6), "Smoke_Pad": V(PAD.x - 0.3, PAD.y - 0.2, 0.5),
           "Spark_1": V(0.0, -1.2, 0.55), "Spark_2": V(1.9, 0.05, 0.5), "Spark_3": V(BAYS.x, BAYS.y - 0.4, 0.55),
           "Spark_4": V(0.45, -1.3, 0.26), "Spark_5": V(-1.6, 1.6, 0.9), "Spark_6": V(1.2, 1.4, 0.35),
           "Light_Core": V(CORE.x, CORE.y, 1.0), "Light_Command": V(0, -0.45, 1.35)}
# New sockets (upright, Unity +Z = the road side): drone docks on the well floors, the shuttle spot, the beacon.
NEW_SOCKETS = {f"Socket_DroneBay_{i + 1}": well_centre(i) + V(0, 0, 0.08) for i in range(4)}
NEW_SOCKETS.update({"Socket_Landing": PAD + V(0, 0, PAD_TOP), "Socket_Beacon": PIVOTS["Beacon"] + V(0, 0, 0.05)})

# Small lit details the LOD1 leaves out (the glow stays in the maps of the parts around them).
LOD_DROP = {"Porthole", "BollardLight", "RampLight", "AntennaTip", "Antenna", "LinkGlow", "LinkSocket", "FloodLamp",
            "Cable", "CrateBand", "Vent", "Mullion", "JunctionStripe", "DoorStripe", "Shard", "Brace", "GuyWire",
            "ArrayTile", "PadLight", "PadDash", "WellLight", "Rung", "Rib", "Valve", "Rail", "Window", "Stud",
            "ClampPad", "FenceLight", "Bolt", "Hose"}


def upright(obj):
    """Blender's FBX export (axis_up Y, forward -Z, no baked space transform) gives every root node an X rotation
    of -90 degrees, so in Unity a module's local +Y is horizontal and a spin about local Y tumbles it. Turning the
    node by +90 about X and the mesh by -90 about X keeps the shape in place and leaves the node rotation at the
    identity in the FBX: Unity's local axes are the world axes (local +Y up)."""
    r = Matrix.Rotation(math.radians(90), 4, "X")
    if obj.type == "MESH":
        obj.data.transform(r.inverted())
    obj.rotation_euler = (math.radians(90), 0, 0)


def prune_hidden(piece, below=DECK_Z + 0.09):
    """Drops the faces that look straight down and sit on the ground or the deck (slab bottoms, the undersides of
    everything standing on the deck). The camera never sees them, and on a 5 m deck they would take a large share
    of the atlas. Wraps the recipe, so the LOD1 rebuild drops the same faces."""
    import bmesh
    recipe, m = piece.recipe, piece.matrix

    def pruned():
        bm = recipe()
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bm.normal_update()
        rot = m.to_3x3()
        dead = []
        for f in bm.faces:
            n = rot @ f.normal
            if n.length > 1e-9 and n.normalized().z < -0.7 and max((m @ v.co).z for v in f.verts) <= below:
                dead.append(f)
        if dead and len(dead) < len(bm.faces):
            bmesh.ops.delete(bm, geom=dead, context="FACES")
        return bm
    piece.recipe = pruned
    piece.local, piece.faces = tk._finish(pruned(), piece.bevel)


def blot(r, seed, n=11):
    rng = np.random.default_rng(seed)
    return [(math.cos(2 * math.pi * i / n) * r * rng.uniform(0.7, 1.0), math.sin(2 * math.pi * i / n) * r * rng.uniform(0.7, 1.0))
            for i in range(n)]


def prow_poly(w, d, c):
    x, y = w / 2, d / 2
    return [(-x + c, -y), (x - c, -y), (x, -y + c), (x, y), (-x, y), (-x, -y + c)]


def poly_at(poly, centre):
    return [(p[0] + centre[0], p[1] + centre[1]) for p in poly]


def edges(poly):
    """(a, b, outward normal) per edge of a counter-clockwise polygon."""
    out = []
    for i in range(len(poly)):
        a, b = Vector((*poly[i], 0)), Vector((*poly[(i + 1) % len(poly)], 0))
        d = b - a
        out.append((a, b, Vector((d.y, -d.x, 0)).normalized()))
    return out


class Base2:
    def __init__(self):
        self.module = {}
        self.cur = None
        self._start = 0

    def tag(self, k, name):
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
        for m in MODULES:
            self.tag(k, m)
            if m.startswith("BayDoor_"):
                self.bay_door(k, int(m[-1]) - 1)
            else:
                getattr(self, m.lower())(k)
        self.tag(k, "Damage"); self.damage(k)
        self.tag(k, "Wreck"); self.wreck(k)
        self.flush(k)
        k.variants = MODULES + ["Damage", "Wreck"]
        for m, pieces in self.module.items():
            for p in pieces:
                p.variants = {m}
                if p.name in LOD_DROP:
                    p.lod = False
                if m not in ("Shuttle",):
                    prune_hidden(p)
        return k

    # ------------------------------------------------------------------------------------------------ deck
    def deck(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        rear_c, rear_s, rear_ch = REAR
        prow_c, prow_s, prow_ch = PROW
        rear_at = lambda d: bk.octagon(rear_s[0] + 2 * d, rear_s[1] + 2 * d, rear_ch + d * 0.414)
        prow_at = lambda d: prow_poly(prow_s[0] + 2 * d, prow_s[1] + 2 * d, prow_ch + d * 0.414)
        rear, prow = rear_at(0.0), prow_at(0.0)
        for grow, c, tag in ((rear_at, rear_c, "Rear"), (prow_at, prow_c, "Prow")):
            poly = grow(0.0)
            k.plate("Skirt" + tag, grow(0.04), 0.1, tk.frame((c[0], c[1], 0.05)), L(tk.GUNMETAL), "Root", on, bevel=0.012)
            k.plate("SkirtHazard" + tag, grow(0.052), 0.03, tk.frame((c[0], c[1], 0.062)), L(tk.HAZARD), "Root", on, bevel=0.003)
            k.plate("Body" + tag, poly, 0.1, tk.frame((c[0], c[1], 0.15)), L(tk.BLUE.but(panel=0.24)), "Root", on, bevel=0.01)
            if intact:
                top = tk.WHITE.but(panel=0.4)
                k.plate("DeckTop" + tag, grow(-0.03), 0.03, tk.frame((c[0], c[1], DECK_Z - 0.015)), top, "Root", on, bevel=0.006)
        # The seam where the prow meets the rear yard, covered by a yellow threshold strip.
        k.block("Threshold", (2.0, 0.09, 0.012), tk.frame((0, -0.7, DECK_Z + 0.004)), L(tk.HAZARD), "Root", on, bevel=0.002)
        # Foundation feet round the rim: darker blocks that stand a little proud of the skirt (a dark underside).
        for poly, c in ((rear, rear_c), (prow, prow_c)):
            for a, b, n in edges(poly_at(poly, c)):
                if n.y > 0.5 and abs(a.y + 0.7) < 0.02:
                    continue
                length = (b - a).length
                count = max(1, int(length / 0.85))
                for j in range(count):
                    p = a + (b - a) * ((j + 0.5) / count) + n * 0.05
                    yaw = math.degrees(math.atan2(n.y, n.x)) + 90
                    k.block("Foot", (0.26, 0.14, 0.2), tk.frame((p.x, p.y, 0.1), rot=(0, 0, yaw)), L(tk.DARK.but(panel=0)), "Root", on,
                            taper=(0.85, 0.7), bevel=0.01)
                    if intact and j % 2 == 0:
                        q = p + n * 0.072
                        k.block("FenceLight", (0.12, 0.012, 0.018), tk.frame((q.x, q.y, 0.16), rot=(0, 0, yaw)),
                                bk.glow("base", 1.5), "Root", on, bevel=0.0)
        # Low barrier walls along the back and the flanks (the road side stays open), with gaps for the lanes.
        for a, b, n in edges(poly_at(rear, rear_c)):
            if n.y < -0.3:
                continue
            length = (b - a).length
            segs = max(1, int(length / 0.7))
            for j in range(segs):
                if (j + segs) % 3 == 2:
                    continue
                t0, t1 = (j + 0.08) / segs, (j + 0.92) / segs
                p = a + (b - a) * ((t0 + t1) / 2) - n * 0.07
                yaw = math.degrees(math.atan2(n.y, n.x)) + 90
                w = length * (t1 - t0)
                k.block("Barrier", (w, 0.09, 0.12), tk.frame((p.x, p.y, DECK_Z + 0.06), rot=(0, 0, yaw)), L(tk.GUNMETAL.but(panel=0.2)),
                        "Root", on, taper=(1, 0.6), bevel=0.008)
                k.block("BarrierCap", (w * 0.96, 0.05, 0.02), tk.frame((p.x, p.y, DECK_Z + 0.125), rot=(0, 0, yaw)),
                        L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.004)
        # The ramp down to the road, with yellow rails and guide lights.
        k.block("Ramp", (0.86, 0.5, 0.05), tk.frame((0, -1.56, 0.115), rot=(22, 0, 0)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on, bevel=0.006)
        for s in (-1, 1):
            k.block("RampRail", (0.05, 0.52, 0.06), tk.frame((s * 0.45, -1.56, 0.14), rot=(22, 0, 0)), L(tk.HAZARD), "Root", on, bevel=0.004)
            if intact:
                for j in range(3):
                    k.block("RampLight", (0.03, 0.04, 0.012), tk.frame((s * 0.37, -1.38 - j * 0.14, 0.2 - (j + 0.5) * 0.055), rot=(22, 0, 0)),
                            bk.glow("base", 1.6), "Root", on, bevel=0.0)
        # Corner bollards on the prow.
        for x, y in ((-0.88, -1.24), (0.88, -1.24)):
            k.cyl("Bollard", (x, y, DECK_Z - 0.01), (x, y, DECK_Z + 0.2), 0.05, 0.045, look=L(tk.GUNMETAL), bone="Root", on=on, sides=10)
            k.cyl("BollardCap", (x, y, DECK_Z + 0.2), (x, y, DECK_Z + 0.24), 0.053, look=L(tk.YELLOW), bone="Root", on=on, sides=10)
            if intact:
                k.cyl("BollardLight", (x, y, DECK_Z + 0.24), (x, y, DECK_Z + 0.26), 0.028, look=bk.glow("base", 1.7), bone="Root", on=on,
                      sides=8, bevel=0.0)
        if not intact:
            return
        # Painted walkways (yellow edge lines) joining the command post's back door to the yards.
        for (x0, y0, x1, y1) in ((-0.62, 0.42, 0.95, 0.42), (-0.62, 0.72, 0.3, 0.72), (-0.15, 0.42, -0.15, 1.1), (0.15, 0.42, 0.15, 1.1)):
            p0, p1 = V(x0, y0, 0), V(x1, y1, 0)
            mid, d = (p0 + p1) / 2, p1 - p0
            k.block("WalkLine", (d.length, 0.025, 0.006), tk.frame((mid.x, mid.y, DECK_Z + 0.002), rot=(0, 0, math.degrees(math.atan2(d.y, d.x)))),
                    tk.YELLOW, "Root", on, bevel=0.0)
        k.plate("Apron", bk.octagon(1.1, 0.26, 0.06), 0.008, tk.frame((0, 0.57, DECK_Z + 0.002)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.0)
        # Deck lights in the rear yard, between the modules.
        for (x, y) in ((-0.95, 0.57), (0.95, 0.57), (-1.0, 2.15), (0.95, 2.18), (2.42, 1.05), (-2.4, 1.6)):
            k.cyl("Stud", (x, y, DECK_Z), (x, y, DECK_Z + 0.02), 0.03, look=bk.glow("base", 1.6), bone="Root", on=on, sides=8, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ command post
    def command(self, k):
        on = ["Base"]
        z = DECK_Z
        c = CMD
        k.block("Plinth", (1.52, 1.5, 0.06), tk.frame((c.x, c.y, z + 0.03)), tk.GUNMETAL, "Root", on, bevel=0.006)
        k.block("Storey1", (1.42, 1.38, 0.5), tk.frame((c.x, c.y, z + 0.31)), tk.BLUE.but(panel=0.13), "Root", on, taper=(0.96, 0.95), bevel=0.012)
        front = c.y - 0.69
        # The armoured front: a raked glacis with hex plates and a heavy blast door, the face the swarm batters.
        k.block("Glacis", (1.48, 0.12, 0.34), tk.frame((0, front - 0.03, z + 0.2), rot=(-18, 0, 0)), tk.GUNMETAL.but(panel=0.12), "Root", on,
                bevel=0.01)
        gm = tk.frame((0, front - 0.1, z + 0.2), rot=(72, 0, 0))
        for i, x in enumerate((-0.56, -0.34, 0.34, 0.56)):
            k.plate("HexArmour", bk.hex_poly(0.1), 0.025, gm @ tk.frame((x, 0.02 * (i % 2), 0)), [tk.WHITE, tk.BLUE][i % 2].but(panel=0), "Root", on,
                    bevel=0.004)
        k.block("BlastDoor", (0.4, 0.05, 0.3), tk.frame((0, front - 0.1, z + 0.19)), tk.DARK.but(panel=0.08), "Root", on, bevel=0.006)
        k.block("DoorFrame", (0.48, 0.06, 0.04), tk.frame((0, front - 0.1, z + 0.36)), tk.HAZARD, "Root", on, bevel=0.004)
        k.block("DoorLight", (0.36, 0.012, 0.02), tk.frame((0, front - 0.128, z + 0.335)), bk.glow("base", 1.7), "Root", on, bevel=0.0)
        # The operations room: a raked glass band across the front with a lit sill.
        k.block("Glass", (1.18, 0.03, 0.13), tk.frame((0, front + 0.01, z + 0.47), rot=(-28, 0, 0)), tk.GLASS, "Root", on, bevel=0.004)
        k.block("GlassGlow", (1.12, 0.012, 0.02), tk.frame((0, front - 0.012, z + 0.405)), bk.glow("base", 1.6), "Root", on, bevel=0.0)
        for x in (-0.4, 0.0, 0.4):
            k.block("Mullion", (0.025, 0.035, 0.14), tk.frame((x, front, z + 0.47), rot=(-28, 0, 0)), tk.WHITE.but(panel=0), "Root", on, bevel=0.002)
        k.block("Roof", (1.48, 1.44, 0.05), tk.frame((c.x, c.y, z + 0.585)), tk.WHITE.but(panel=0.16), "Root", on, bevel=0.008)
        k.block("RoofLip", (1.5, 0.06, 0.04), tk.frame((0, front + 0.0, z + 0.585)), tk.HAZARD, "Root", on, bevel=0.004)
        for s in (-1, 1):
            k.block("SideFin", (0.04, 1.0, 0.42), tk.frame((s * 0.735, c.y + 0.02, z + 0.3)), tk.WHITE.but(panel=0.1), "Root", on, taper=(1, 0.7),
                    bevel=0.004)
            k.block("SideVent", (0.012, 0.5, 0.08), tk.frame((s * 0.712, c.y + 0.25, z + 0.18)), tk.DARK, "Root", on, bevel=0.002)
        # The back (camera side): the yard door with its light, lit windows, two conduits up to the roof.
        back = c.y + 0.69
        k.block("BackDoor", (0.3, 0.02, 0.26), tk.frame((-0.15, back + 0.005, z + 0.13)), tk.DARK, "Root", on, bevel=0.003)
        k.block("DoorStripe", (0.34, 0.022, 0.03), tk.frame((-0.15, back + 0.008, z + 0.015)), tk.HAZARD, "Root", on, bevel=0.002)
        k.block("BackDoorLight", (0.32, 0.022, 0.02), tk.frame((-0.15, back + 0.008, z + 0.275)), bk.glow("base", 1.5), "Root", on, bevel=0.0)
        for x in (0.25, 0.5):
            k.block("Window", (0.18, 0.012, 0.07), tk.frame((x, back + 0.004, z + 0.36)), bk.glow("base", 1.4), "Root", on, bevel=0.0)
        for x in (-0.55, -0.47):
            k.cyl("Conduit", (x, back + 0.04, z + 0.02), (x, back + 0.04, z + 0.58), 0.022, look=tk.GUNMETAL.but(rings=0.1), bone="Root", on=on, sides=8)
        # Upper storey set back, a wrap-round window band, then the glazed ops cupola.
        s2 = V(0.12, c.y - 0.1, z + 0.61)
        k.block("Storey2", (0.92, 0.86, 0.34), tk.frame((s2.x, s2.y, s2.z + 0.17)), tk.BLUE.but(panel=0.11), "Root", on, bevel=0.01)
        for (dx, dy, w, rot) in ((0, -0.432, 0.74, 0), (0, 0.432, 0.74, 0), (0.462, 0, 0.66, 90), (-0.462, 0, 0.66, 90)):
            k.block("Window2", (w, 0.012, 0.055), tk.frame((s2.x + dx, s2.y + dy, s2.z + 0.22), rot=(0, 0, rot)), bk.glow("base", 1.5), "Root", on,
                    bevel=0.0)
        k.block("Roof2", (0.98, 0.92, 0.04), tk.frame((s2.x, s2.y, s2.z + 0.36)), tk.WHITE.but(panel=0.12), "Root", on, bevel=0.006)
        cz = s2.z + 0.38
        k.lathe("CupolaBase", [(0, 0), (0.3, 0), (0.3, 0.04), (0, 0.04)], tk.frame((s2.x, s2.y, cz)), tk.GUNMETAL, "Root", on, sides=8)
        k.lathe("CupolaGlass", [(0.27, 0.04), (0.29, 0.17), (0.0, 0.17)], tk.frame((s2.x, s2.y, cz)), tk.GLASS, "Root", on, sides=8, bevel=0.0)
        k.torus("CupolaGlow", (s2.x, s2.y, cz + 0.05), UP, 0.275, 0.01, bk.glow("base", 1.6), "Root", on, seg=16, sides=4)
        k.lathe("CupolaCap", [(0, 0.17), (0.33, 0.17), (0.3, 0.22), (0.12, 0.26), (0, 0.26)], tk.frame((s2.x, s2.y, cz)), tk.WHITE.but(panel=0),
                "Root", on, sides=8)
        top = cz + 0.26
        for i, (x, y, h) in enumerate(((0.06, -0.06, 0.34), (0.2, 0.05, 0.22), (-0.06, 0.08, 0.26))):
            p = V(s2.x + x, s2.y + y, top)
            k.cyl("Antenna", p, p + V(0, 0, h), 0.008, 0.005, look=tk.GUNMETAL, bone="Root", on=on, sides=6, bevel=0.0)
            k.cyl("AntennaTip", p + V(0, 0, h), p + V(0, 0, h + 0.012), 0.013, look=bk.glow("base", 1.8), bone="Root", on=on, sides=6, bevel=0.0)
        # Roof plant on the lower roof behind the upper storey: two air handlers, a fixed uplink dish, a hatch.
        rz = z + 0.61
        for x in (-0.42, 0.42):
            k.block("AirUnit", (0.3, 0.2, 0.12), tk.frame((x, c.y + 0.53, rz + 0.06)), tk.GUNMETAL.but(panel=0), "Root", on, bevel=0.006)
            k.lathe("Fan", [(0, 0), (0.07, 0), (0.07, 0.008), (0, 0.008)], tk.frame((x - 0.06, c.y + 0.53, rz + 0.121)), tk.DARK, "Root", on, sides=10,
                    bevel=0.0)
            k.lathe("Fan", [(0, 0), (0.07, 0), (0.07, 0.008), (0, 0.008)], tk.frame((x + 0.07, c.y + 0.53, rz + 0.121)), tk.DARK, "Root", on, sides=10,
                    bevel=0.0)
        up = V(-0.52, c.y - 0.3, rz)
        k.cyl("UplinkPost", up, up + V(0, 0, 0.12), 0.025, look=tk.GUNMETAL, bone="Root", on=on, sides=8)
        k.lathe("UplinkDish", [(0, 0), (0.13, 0.04), (0.14, 0.05), (0.125, 0.055), (0, 0.015)], tk.frame(up + V(0, 0, 0.12), z=V(0.2, 0.5, 1)),
                tk.WHITE.but(panel=0), "Root", on, sides=14, bevel=0.0)
        k.block("Hatch", (0.18, 0.18, 0.02), tk.frame((0.0, c.y + 0.53, rz + 0.01)), tk.HAZARD, "Root", on, bevel=0.003)

    # ------------------------------------------------------------------------------------------------ hardpoints
    def hardpoints(self, k):
        on = ["Base"]
        for s in (-1, 1):
            c = V(s * HARD_X, HARD_Y, DECK_Z)
            k.block("HardPlinth", (0.56, 0.56, 0.08), tk.frame(c + V(0, 0, 0.04)), tk.GUNMETAL, "Root", on, taper=(0.94, 0.94), bevel=0.008)
            k.block("Junction", (0.24, 0.42, 0.17), tk.frame(c + V(s * 0.12, 0, 0.165)), tk.BLUE.but(panel=0.1), "Root", on, bevel=0.008)
            k.block("JunctionLid", (0.25, 0.4, 0.03), tk.frame(c + V(s * 0.12, 0, 0.26)), tk.WHITE.but(panel=0), "Root", on, bevel=0.005)
            k.block("JunctionStripe", (0.012, 0.38, 0.03), tk.frame(c + V(s * 0.245, 0, 0.2)), tk.HAZARD, "Root", on, bevel=0.0)
            for dy in (-0.1, 0.1):
                p = c + V(s * 0.24, dy, 0.15)
                k.cyl("LinkSocket", p, p + V(s * 0.04, 0, 0), 0.028, look=tk.DARK, bone="Root", on=on, sides=10, bevel=0.0)
                k.cyl("LinkGlow", p + V(s * 0.038, 0, 0), p + V(s * 0.046, 0, 0), 0.017, look=bk.glow("base", 1.7), bone="Root", on=on, sides=8,
                      bevel=0.0)
            # A blast wall on the road side of the hardpoint, the ammunition lockers behind it.
            k.block("BlastWall", (0.56, 0.1, 0.22), tk.frame(c + V(0, -0.31, 0.11)), tk.GUNMETAL.but(panel=0.14), "Root", on, taper=(1, 0.55),
                    bevel=0.01)
            k.block("BlastWallCap", (0.5, 0.05, 0.02), tk.frame(c + V(0, -0.305, 0.225)), tk.HAZARD, "Root", on, bevel=0.003)
            for j in range(2):
                k.block("Locker", (0.14, 0.16, 0.14), tk.frame(c + V(-s * 0.13, 0.12 + j * 0.0, 0.07 + j * 0.14)), [tk.BLUE, tk.WHITE][j].but(panel=0),
                        "Root", on, bevel=0.006)
            k.cyl("FloodPost", c + V(-s * 0.22, -0.2, 0.08), c + V(-s * 0.22, -0.2, 0.5), 0.016, 0.012, look=tk.GUNMETAL, bone="Root", on=on, sides=6,
                  bevel=0.0)
            k.block("FloodHead", (0.08, 0.05, 0.05), tk.frame(c + V(-s * 0.22, -0.2, 0.52)), tk.WHITE.but(panel=0), "Root", on, bevel=0.004)
            k.block("FloodLamp", (0.06, 0.012, 0.03), tk.frame(c + V(-s * 0.22, -0.226, 0.52)), bk.glow("base", 2.0), "Root", on, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ habitat
    def habitat(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        c = HAB
        intact = look is None

        def tube(centre, r, length, paint):
            p0, p1 = centre + V(-length / 2, 0, 0), centre + V(length / 2, 0, 0)
            k.cyl("Hab", p0, p1, r, look=L(paint.but(rings=0.16, panel=0)), bone="Root", on=on, sides=14)
            for p, d in ((p0, V(-1, 0, 0)), (p1, V(1, 0, 0))):
                k.lathe("HabCap", [(0, 0), (r, 0), (r * 0.9, r * 0.28), (r * 0.5, r * 0.45), (0, r * 0.5)], tk.frame(p, z=d), L(tk.WHITE.but(panel=0)),
                        "Root", on, sides=14)
            for x in (-length * 0.32, length * 0.32):
                k.block("Cradle", (0.12, r * 1.9, 0.1), tk.frame(centre + V(x, 0, -r + 0.04)), L(tk.GUNMETAL), "Root", on, bevel=0.004)
            if intact:
                for j in range(4):
                    x = -length * 0.36 + j * length * 0.24
                    pp = centre + V(x, r * 0.92, r * 0.3)
                    k.cyl("Porthole", pp - V(0, 0.012, 0), pp + V(0, 0.014, 0), 0.026, look=bk.glow("base", 1.5), bone="Root", on=on, sides=8, bevel=0.0)
        r = 0.17
        tube(c + V(0, -0.27, r + 0.04), r, 0.95, tk.WHITE)
        tube(c + V(0, 0.27, r + 0.04), r, 0.95, tk.BLUE)
        # The node between the tubes, capped by the greenhouse dome (grow lights under glass).
        k.cyl("Node", c, c + V(0, 0, 0.46), 0.2, look=L(tk.GUNMETAL.but(rings=0.08)), bone="Root", on=on, sides=16)
        k.torus("NodeRing", c + V(0, 0, 0.46), UP, 0.2, 0.02, L(tk.WHITE.but(panel=0)), "Root", on, seg=16, sides=5)
        if intact:
            k.lathe("Greenhouse", [(0.19, 0), (0.18, 0.08), (0.13, 0.16), (0.06, 0.2), (0, 0.21)], tk.frame(c + V(0, 0, 0.46)),
                    tk.GLASS.but(paint=(0.12, 0.2, 0.18), emit=(0.45, 0.95, 0.8), glow=0.5, fam="glow", lens=0.08), "Root", on,
                    sides=16, bevel=0.0)
            k.lathe("GrowLight", [(0, 0), (0.15, 0), (0.15, 0.03), (0, 0.03)], tk.frame(c + V(0, 0, 0.465)), bk.glow((0.55, 1.0, 0.85), 1.2), "Root", on,
                    sides=12, bevel=0.0)
            for a in range(4):
                ang = math.pi / 4 + a * math.pi / 2
                k.cyl("Rib", c + V(math.cos(ang) * 0.19, math.sin(ang) * 0.19, 0.46), c + V(0, 0, 0.67), 0.008, look=tk.WHITE, bone="Root", on=on,
                      sides=5, bevel=0.0)
        # A solar rack on the front tube and a walkway to the command post's back door.
        k.block("SolarRack", (0.7, 0.24, 0.02), tk.frame(c + V(0, -0.27, 0.43), rot=(-18, 0, 0)), L(tk.DARK.but(panel=0.06, fam="glass", rough=0.2)),
                "Root", on, bevel=0.003)
        k.block("SolarFrame", (0.74, 0.03, 0.03), tk.frame(c + V(0, -0.38, 0.4)), L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.003)
        k.block("Walkway", (0.24, 0.5, 0.025), tk.frame(c + V(0.0, -0.66, 0.0125)), L(tk.GUNMETAL.but(panel=0.08)), "Root", on, bevel=0.003)

    # ------------------------------------------------------------------------------------------------ radar
    def radar(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        c = RADAR
        intact = look is None
        k.lathe("RadarFoot", [(0, 0), (0.42, 0), (0.42, 0.06), (0.39, 0.08), (0, 0.08)], tk.frame(c), L(tk.GUNMETAL), "Root", on, sides=8)
        k.lathe("RadarHut", [(0, 0.08), (0.35, 0.08), (0.34, 0.34), (0, 0.34)], tk.frame(c), L(tk.BLUE.but(panel=0.12)), "Root", on, sides=8)
        k.lathe("RadarHutRoof", [(0, 0.34), (0.38, 0.34), (0.37, 0.38), (0.3, 0.4), (0, 0.4)], tk.frame(c), L(tk.WHITE.but(panel=0)), "Root", on,
                sides=8)
        k.torus("HutBand", c + V(0, 0, 0.11), UP, 0.352, 0.012, L(tk.HAZARD), "Root", on, seg=8, sides=4)
        if intact:
            k.block("HutDoor", (0.16, 0.02, 0.2), tk.frame(c + V(0.1, 0.33, 0.19), rot=(0, 0, -22.5)), tk.DARK, "Root", on, bevel=0.003)
            k.block("HutDoorLight", (0.17, 0.022, 0.018), tk.frame(c + V(0.1, 0.335, 0.3), rot=(0, 0, -22.5)), bk.glow("base", 1.5), "Root", on,
                    bevel=0.0)
            for a in (200, 290):
                r = math.radians(a)
                k.block("Vent", (0.14, 0.012, 0.05), tk.frame(c + V(math.cos(r) * 0.345, math.sin(r) * 0.345, 0.25), rot=(0, 0, a + 90)), tk.DARK,
                        "Root", on, bevel=0.002)
        # The lattice mast: four legs tapering to the bearing, ring frames and X-braces on every face.
        base_z, top_z = 0.4, MAST_TOP - 0.1
        levels = [base_z, 0.85, 1.25, top_z]

        def leg_at(i, zz):
            t = (zz - base_z) / (top_z - base_z)
            r = 0.27 * (1 - t) + 0.075 * t
            a = math.pi / 4 + i * math.pi / 2
            return c + V(math.cos(a) * r, math.sin(a) * r, zz)
        for i in range(4):
            k.cyl("MastLeg", leg_at(i, base_z), leg_at(i, top_z), 0.018, 0.013, look=L(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        for li, zz in enumerate(levels[1:]):
            for i in range(4):
                a, b = leg_at(i, zz), leg_at((i + 1) % 4, zz)
                k.cyl("MastRing", a, b, 0.009, look=L(tk.WHITE if li == 1 else tk.GUNMETAL), bone="Root", on=on, sides=5, bevel=0.0)
        for li in range(len(levels) - 1):
            z0, z1 = levels[li], levels[li + 1]
            for i in range(4):
                a0, b0, a1, b1 = leg_at(i, z0), leg_at((i + 1) % 4, z0), leg_at(i, z1), leg_at((i + 1) % 4, z1)
                k.cyl("Brace", a0, b1, 0.006, look=L(tk.GUNMETAL), bone="Root", on=on, sides=4, bevel=0.0)
                k.cyl("Brace", b0, a1, 0.006, look=L(tk.GUNMETAL), bone="Root", on=on, sides=4, bevel=0.0)
        # The service platform half way up, with a rail, and the hazard band on the mast.
        pz = 1.25
        k.lathe("Platform", [(0.1, 0), (0.3, 0), (0.3, 0.025), (0.1, 0.025)], tk.frame(c + V(0, 0, pz)), L(tk.GUNMETAL.but(panel=0)), "Root", on,
                sides=8, bevel=0.0)
        k.torus("Rail", c + V(0, 0, pz + 0.1), UP, 0.29, 0.007, L(tk.YELLOW), "Root", on, seg=8, sides=4)
        for a in range(8):
            ang = math.pi / 8 + a * math.pi / 4
            p = c + V(math.cos(ang) * 0.29, math.sin(ang) * 0.29, pz)
            k.cyl("Rail", p, p + V(0, 0, 0.1), 0.006, look=L(tk.YELLOW), bone="Root", on=on, sides=4, bevel=0.0)
        k.block("MastStripe", (0.2, 0.2, 0.03), tk.frame(c + V(0, 0, 0.85)), L(tk.HAZARD), "Root", on, bevel=0.002)
        # The bearing housing the head turns on: its top face is the head's pivot (the mast axis).
        k.cyl("Bearing", c + V(0, 0, top_z - 0.02), c + V(0, 0, MAST_TOP - 0.012), 0.11, 0.1, look=L(tk.WHITE.but(panel=0)), bone="Root", on=on,
              sides=14)
        k.cyl("Collar", c + V(0, 0, MAST_TOP - 0.012), c + V(0, 0, MAST_TOP), 0.085, look=L(tk.DARK), bone="Root", on=on, sides=14, bevel=0.0)
        if intact:
            for a in (30, 150, 270):
                r = math.radians(a)
                k.cyl("GuyWire", c + V(math.cos(r) * 0.16, math.sin(r) * 0.16, 1.2), c + V(math.cos(r) * 0.62, math.sin(r) * 0.62, 0.0), 0.004,
                      look=tk.RUBBER, bone="Root", on=on, sides=4, bevel=0.0)
                k.cyl("GuyAnchor", c + V(math.cos(r) * 0.62, math.sin(r) * 0.62, 0.0), c + V(math.cos(r) * 0.62, math.sin(r) * 0.62, 0.04), 0.025,
                      look=tk.YELLOW, bone="Root", on=on, sides=6)
            # The cable run to the drone bays' control booth.
            k.cyl("Cable", c + V(0.3, -0.3, 0.03), V(-0.9, 0.92, DECK_Z + 0.03), 0.02, look=tk.RUBBER, bone="Root", on=on, sides=6, bevel=0.0)

    def dish(self, k, look=None, on=("Base",)):
        """The phased-array head, built round its pivot on the mast axis: a turntable, a raked array panel in front
        of the axis with lit tiles, the electronics boxes behind it, a small tracking dish and the beacon stalk."""
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        P = HEAD
        k.lathe("Turntable", [(0, 0), (0.17, 0), (0.17, 0.035), (0.14, 0.06), (0, 0.06)], tk.frame(P), L(tk.GUNMETAL), "Root", on, sides=16)
        k.torus("TurntableRing", P + V(0, 0, 0.03), UP, 0.172, 0.01, L(tk.HAZARD) if not intact else bk.glow("base", 1.4), "Root", on, seg=16, sides=4)
        # Yoke arms from the turntable to the panel's sides.
        tilt = -16
        pc = P + V(0, -0.13, 0.4)
        panel = tk.frame(pc, rot=(tilt, 0, 0))
        for s in (-1, 1):
            k.block("Yoke", (0.05, 0.2, 0.36), tk.frame(P + V(s * 0.47, -0.08, 0.22), rot=(tilt, 0, 0)), L(tk.WHITE.but(panel=0)), "Root", on,
                    taper=(1, 0.7), bevel=0.006)
        k.block("YokeBeam", (1.0, 0.1, 0.06), tk.frame(P + V(0, -0.05, 0.09)), L(tk.GUNMETAL.but(panel=0)), "Root", on, bevel=0.006)
        k.block("ArrayBack", (0.94, 0.07, 0.54), panel, L(tk.BLUE.but(panel=0.12)), "Root", on, bevel=0.01)
        k.block("ArrayFrame", (0.98, 0.05, 0.58), panel @ tk.frame((0, -0.03, 0)), L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.008)
        k.block("ArrayFace", (0.86, 0.02, 0.46), panel @ tk.frame((0, -0.06, 0)), L(tk.GLASS.but(paint=(0.04, 0.07, 0.11))), "Root", on, bevel=0.002)
        # Back to back: the array faces both ways (the rear face a little dimmer), so the lit tiles always show.
        k.block("ArrayFace", (0.86, 0.02, 0.46), panel @ tk.frame((0, 0.06, 0)), L(tk.GLASS.but(paint=(0.04, 0.07, 0.11))), "Root", on, bevel=0.002)
        if intact:
            for face, strength in ((-1, 1.0), (1, 0.75)):
                for i in range(7):
                    for j in range(3):
                        x, zz = (i - 3) * 0.118, (j - 1) * 0.142
                        k.block("ArrayTile", (0.1, 0.008, 0.12), panel @ tk.frame((x, face * 0.072, zz)),
                                bk.glow("azure", strength * (0.9 + 0.25 * ((i + j) % 2))), "Root", on, bevel=0.0)
                k.block("ArrayTopLight", (0.9, 0.01, 0.014), panel @ tk.frame((0, face * 0.06, 0.28)), bk.glow("yellow", 1.4), "Root", on, bevel=0.0)
        for s in (-1, 1):
            k.block("Electronics", (0.2, 0.14, 0.12), tk.frame(P + V(s * 0.25, 0.06, 0.12)), L(tk.GUNMETAL.but(panel=0.08)), "Root", on, bevel=0.008)
            k.block("ElectronicsLid", (0.21, 0.15, 0.02), tk.frame(P + V(s * 0.25, 0.06, 0.185)), L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.004)
            if intact:
                k.block("Vent", (0.012, 0.1, 0.06), tk.frame(P + V(s * 0.352, 0.06, 0.12)), tk.DARK, "Root", on, bevel=0.002)
        # The tracking dish on an arm at the back.
        arm0, arm1 = P + V(0.3, 0.1, 0.19), P + V(0.36, 0.27, 0.32)
        k.cyl("DishArm", arm0, arm1, 0.014, look=L(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        k.lathe("TrackDish", [(0, 0), (0.12, 0.035), (0.13, 0.045), (0.115, 0.05), (0, 0.014)], tk.frame(arm1, z=V(0.1, 0.6, 0.8)),
                L(tk.WHITE.but(panel=0)), "Root", on, sides=14, bevel=0.0)
        if intact:
            k.lathe("TrackFeed", [(0, 0), (0.016, 0), (0.012, 0.025), (0, 0.03)], tk.frame(arm1 + V(0.1, 0.6, 0.8).normalized() * 0.08,
                    z=V(0.1, 0.6, 0.8)), bk.glow("base", 1.8), "Root", on, sides=8, bevel=0.0)
            # The beacon stalk on the turn axis (round, so its turning is invisible).
            k.cyl("BeaconStalk", P + V(0, 0, 0.06), P + V(0, 0, BEACON_UP), 0.022, 0.016, look=tk.GUNMETAL, bone="Root", on=on, sides=8, bevel=0.0)

    def beacon(self, k):
        on = ["Base"]
        b = PIVOTS["Beacon"]
        k.cyl("BeaconCollar", b, b + V(0, 0, 0.025), 0.034, look=tk.GUNMETAL, bone="Root", on=on, sides=10)
        k.lathe("BeaconLens", [(0, 0), (0.035, 0), (0.042, 0.035), (0.034, 0.07), (0, 0.08)], tk.frame(b + V(0, 0, 0.022)), bk.glow("amber", 2.0, lens=0.04),
                "Root", on, sides=10, bevel=0.0)
        for a in range(3):
            r = math.radians(a * 120 + 30)
            p = b + V(math.cos(r) * 0.045, math.sin(r) * 0.045, 0.02)
            k.cyl("BeaconCage", p, p + V(0, 0, 0.075), 0.005, look=tk.GUNMETAL, bone="Root", on=on, sides=4, bevel=0.0)
        k.cyl("BeaconCap", b + V(0, 0, 0.095), b + V(0, 0, 0.105), 0.05, look=tk.GUNMETAL, bone="Root", on=on, sides=10, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ drone bays
    def bays(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        c = BAYS
        span = 2 * WELL + 3 * WALL
        k.block("BayBase", (span + 0.06, span + 0.06, 0.07), tk.frame(c + V(0, 0, 0.035)), L(tk.GUNMETAL), "Root", on, bevel=0.008)
        h = WALL_H
        for s in (-1, 1):
            # Outer walls, panelled blue with white caps; the inner cross walls carry hazard caps.
            k.block("BayWall", (span, WALL, h), tk.frame(c + V(0, s * (span - WALL) / 2, h / 2)), L(tk.BLUE.but(panel=0.1)), "Root", on, bevel=0.008)
            k.block("BayWall", (WALL, span - 2 * WALL, h), tk.frame(c + V(s * (span - WALL) / 2, 0, h / 2)), L(tk.BLUE.but(panel=0.1)), "Root", on,
                    bevel=0.008)
            k.block("BayCap", (span + 0.02, WALL + 0.02, 0.025), tk.frame(c + V(0, s * (span - WALL) / 2, h + 0.0125)), L(tk.WHITE.but(panel=0)),
                    "Root", on, bevel=0.004)
            k.block("BayCap", (WALL + 0.02, span - 2 * WALL - 0.02, 0.025), tk.frame(c + V(s * (span - WALL) / 2, 0, h + 0.0125)),
                    L(tk.WHITE.but(panel=0)), "Root", on, bevel=0.004)
        k.block("CrossWall", (span - 2 * WALL, WALL, h - 0.04), tk.frame(c + V(0, 0, (h - 0.04) / 2)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on,
                bevel=0.006)
        k.block("CrossWall", (WALL, span - 2 * WALL, h - 0.04), tk.frame(c + V(0, 0, (h - 0.04) / 2)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on,
                bevel=0.006)
        k.block("CrossCap", (span - 2 * WALL, WALL + 0.01, 0.02), tk.frame(c + V(0, 0, h - 0.03)), L(tk.HAZARD), "Root", on, bevel=0.003)
        k.block("CrossCap", (WALL + 0.01, span - 2 * WALL, 0.02), tk.frame(c + V(0, 0, h - 0.03)), L(tk.HAZARD), "Root", on, bevel=0.003)
        for i in range(4):
            w = well_centre(i)
            k.block("WellFloor", (WELL, WELL, 0.012), tk.frame(w + V(0, 0, 0.066)), L(tk.DARK.but(panel=0.15)), "Root", on, bevel=0.0)
            k.lathe("ChargePad", [(0, 0), (0.14, 0), (0.13, 0.014), (0, 0.014)], tk.frame(w + V(0, 0, 0.072)), L(tk.STEEL), "Root", on, sides=12,
                    bevel=0.0)
            if intact:
                k.torus("DockRing", w + V(0, 0, 0.078), UP, 0.22, 0.009, bk.glow("cyan", 1.5), "Root", on, seg=20, sides=4)
            for a in range(4):
                ang = math.pi / 4 + a * math.pi / 2
                p = w + V(math.cos(ang) * 0.24, math.sin(ang) * 0.24, 0.1)
                k.block("Clamp", (0.06, 0.03, 0.05), tk.frame(p, rot=(0, 0, math.degrees(ang) + 90)), L(tk.GUNMETAL), "Root", on, bevel=0.004)
                if intact:
                    k.block("ClampPad", (0.04, 0.012, 0.02), tk.frame(p + V(0, 0, 0.03), rot=(0, 0, math.degrees(ang) + 90)), tk.YELLOW, "Root", on,
                            bevel=0.0)
            # Hinge blocks on the outer wall for the lid, and a status light on the inner corner.
            hinge, side = door_hinge(i)
            for dy in (-0.2, 0.2):
                k.cyl("Hinge", hinge + V(0, dy - 0.05, -0.005), hinge + V(0, dy + 0.05, -0.005), 0.02, look=L(tk.GUNMETAL), bone="Root", on=on, sides=8)
            if intact:
                corner = V(c.x + (w.x - c.x) * 0.12, c.y + (w.y - c.y) * 0.12, c.z + h - 0.01)
                k.cyl("WellLight", corner, corner + V(0, 0, 0.012), 0.018, look=bk.glow("cyan", 1.8), bone="Root", on=on, sides=8, bevel=0.0)
        # The control booth on the yard side, its window facing the camera.
        b = V(-0.84, 0.92, DECK_Z)
        k.block("Booth", (0.3, 0.46, 0.36), tk.frame(b + V(0, 0, 0.18)), L(tk.WHITE.but(panel=0.12)), "Root", on, taper=(0.95, 0.95), bevel=0.008)
        k.block("BoothRoof", (0.34, 0.5, 0.03), tk.frame(b + V(0, 0, 0.375)), L(tk.GUNMETAL), "Root", on, bevel=0.005)
        if intact:
            k.block("BoothWindow", (0.2, 0.012, 0.1), tk.frame(b + V(0, 0.232, 0.24)), bk.glow("base", 1.5), "Root", on, bevel=0.0)
            k.block("BoothWindow", (0.012, 0.3, 0.1), tk.frame(b + V(0.152, 0, 0.24)), bk.glow("base", 1.3), "Root", on, bevel=0.0)
            k.cyl("Antenna", b + V(0.08, 0.15, 0.39), b + V(0.08, 0.15, 0.62), 0.007, 0.005, look=tk.GUNMETAL, bone="Root", on=on, sides=6, bevel=0.0)
            k.cyl("AntennaTip", b + V(0.08, 0.15, 0.62), b + V(0.08, 0.15, 0.632), 0.012, look=bk.glow("base", 1.8), bone="Root", on=on, sides=6,
                  bevel=0.0)

    def bay_door(self, k, i, look=None, on=("Base",)):
        """One hinged lid, built open (DOOR_OPEN degrees up from shut, standing on its outer wall)."""
        L = look or (lambda x: x)
        on = list(on)
        hinge, side = door_hinge(i)
        # Shut, the lid lies inward (-side) from the hinge; open it turns about the hinge line (Blender Y).
        m = Matrix.Translation(hinge) @ Matrix.Rotation(math.radians(DOOR_OPEN) * side, 4, "Y")
        inward = -side
        w = WELL + WALL * 0.9
        k.block("Lid", (w, WELL + 0.04, 0.035), m @ tk.frame((inward * w / 2, 0, 0)), L(tk.WHITE.but(panel=0.14)), "Root", on, bevel=0.006)
        k.block("LidBand", (w * 0.9, 0.1, 0.006), m @ tk.frame((inward * w / 2, 0, 0.019)), L(tk.BLUE.but(panel=0)), "Root", on, bevel=0.0)
        for s in (-1, 1):
            k.block("LidEdge", (w, 0.035, 0.04), m @ tk.frame((inward * w / 2, s * (WELL / 2 + 0.005), 0.0)), L(tk.HAZARD), "Root", on, bevel=0.003)
        k.block("LidRib", (0.04, WELL * 0.8, 0.03), m @ tk.frame((inward * w * 0.75, 0, -0.03)), L(tk.GUNMETAL), "Root", on, bevel=0.003)
        k.block("LidKnuckle", (0.05, 0.5, 0.04), m @ tk.frame((inward * 0.02, 0, 0)), L(tk.GUNMETAL), "Root", on, bevel=0.004)

    # ------------------------------------------------------------------------------------------------ storage
    def storage(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        for j, y in enumerate((-0.04, 0.5)):
            c = V(STORE.x, y, DECK_Z)
            self.tank(k, c, 0.21, 0.6, L, on, intact, seed=j)
        # The pipe manifold from the tanks to the Crysium silo, with flanges and a pump.
        pump = V(1.8, 0.23, DECK_Z)
        k.block("Pump", (0.2, 0.22, 0.2), tk.frame(pump + V(0, 0, 0.1)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on, bevel=0.008)
        k.block("PumpLid", (0.21, 0.23, 0.02), tk.frame(pump + V(0, 0, 0.205)), L(tk.YELLOW), "Root", on, bevel=0.004)
        for y in (-0.04, 0.5):
            for zz, r in ((0.13, 0.03), (0.07, 0.022)):
                k.cyl("Pipe", V(STORE.x - 0.18, y, DECK_Z + zz), pump + V(0.08, (y - pump.y) * 0.3, zz), r, look=L(tk.STEEL.but(rings=0.12)), bone="Root",
                      on=on, sides=8)
        for zz in (0.08, 0.15):
            k.cyl("Pipe", pump + V(-0.1, 0, zz), CORE + V(0.34, 0, zz), 0.028, look=L(tk.STEEL.but(rings=0.12)), bone="Root", on=on, sides=8)
            p = (pump + V(-0.1, 0, zz) + CORE + V(0.34, 0, zz)) / 2
            k.cyl("Flange", p - V(0.012, 0, 0), p + V(0.012, 0, 0), 0.04, look=L(tk.GUNMETAL), bone="Root", on=on, sides=8, bevel=0.0)
        if intact:
            k.torus("Valve", pump + V(0, -0.13, 0.12), V(0, 1, 0), 0.045, 0.008, tk.YELLOW, "Root", on, seg=12, sides=4)
        # Containers stacked between the habitat and the landing zone, with a crate pile.
        for (x, y, zz, rot, paint) in ((0.5, 1.02, 0.0, 0, tk.BLUE), (0.5, 1.56, 0.0, 0, tk.WHITE), (0.5, 1.28, 0.25, 4, tk.GUNMETAL),
                                       (0.5, 2.06, 0.0, -6, tk.BLUE)):
            self.container(k, V(x, y, DECK_Z + zz), rot, L(paint.but(panel=0)), L, on, intact)
        for i, (x, y, r, s) in enumerate(((0.9, 1.02, 12, 0.15), (0.84, 0.86, -20, 0.12), (0.2, 1.08, 8, 0.13), (2.4, 2.05, 30, 0.12))):
            k.block("Crate", (s, s, s), tk.frame((x, y, DECK_Z + s / 2), rot=(0, 0, r)), L([tk.BLUE, tk.GUNMETAL, tk.WHITE][i % 3].but(panel=0)), "Root",
                    on, bevel=0.006)
            if intact:
                k.block("CrateBand", (s + 0.004, 0.026, s + 0.004), tk.frame((x, y, DECK_Z + s / 2), rot=(0, 0, r)), tk.HAZARD, "Root", on, bevel=0.002)

    def tank(self, k, c, r, h, L, on, intact, seed=0):
        k.lathe("TankFoot", [(0, 0), (r + 0.04, 0), (r + 0.04, 0.05), (0, 0.05)], tk.frame(c), L(tk.GUNMETAL), "Root", on, sides=16)
        k.lathe("Tank", [(0, 0.05), (r, 0.05), (r, h), (0, h)], tk.frame(c), L(tk.BLUE.but(rings=0.18, panel=0)), "Root", on, sides=16)
        k.lathe("TankDome", [(0, h), (r, h), (r * 0.93, h + r * 0.3), (r * 0.6, h + r * 0.5), (0, h + r * 0.56)], tk.frame(c),
                L(tk.WHITE.but(panel=0)), "Root", on, sides=16)
        k.torus("TankBand", c + V(0, 0, h * 0.55), UP, r + 0.004, 0.018, L(tk.HAZARD), "Root", on, seg=16, sides=4)
        k.cyl("TankVent", c + V(0, 0, h + r * 0.5), c + V(0, 0, h + r * 0.5 + 0.06), 0.03, look=L(tk.GUNMETAL), bone="Root", on=on, sides=8)
        if intact:
            # Ladder on the camera side and a level gauge glowing on the tank wall.
            for s in (-1, 1):
                k.cyl("LadderRail", c + V(s * 0.05, r + 0.03, 0.05), c + V(s * 0.05, r + 0.03, h + 0.05), 0.007, look=tk.YELLOW, bone="Root", on=on,
                      sides=4, bevel=0.0)
            for j in range(5):
                zz = 0.12 + j * (h - 0.1) / 5
                k.cyl("Rung", c + V(-0.05, r + 0.03, zz), c + V(0.05, r + 0.03, zz), 0.005, look=tk.YELLOW, bone="Root", on=on, sides=4, bevel=0.0)
            k.block("Gauge", (0.03, 0.012, h * 0.5), tk.frame(c + V(-r * 0.7, r * 0.72, h * 0.5), rot=(0, 0, -45)), bk.glow("cyan", 1.4), "Root", on,
                    bevel=0.0)

    def container(self, k, c, rot, paint, L, on, intact):
        m = tk.frame(c + V(0, 0, 0.12), rot=(0, 0, rot))
        k.block("Container", (0.25, 0.5, 0.24), m, paint, "Root", on, bevel=0.008)
        for s in (-1, 1):
            k.block("ContainerDoor", (0.24, 0.012, 0.22), m @ tk.frame((0, s * 0.252, 0)), L(tk.GUNMETAL.but(panel=0)), "Root", on, bevel=0.002)
            k.block("ContainerStripe", (0.252, 0.03, 0.03), m @ tk.frame((0, s * 0.22, 0.106)), L(tk.HAZARD), "Root", on, bevel=0.002)
        if intact:
            for j in range(4):
                k.block("Rib", (0.254, 0.014, 0.2), m @ tk.frame((0, -0.15 + j * 0.1, 0)), tk.GUNMETAL, "Root", on, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ Crysium silo
    def core(self, k):
        on = ["Base"]
        c = CORE
        k.lathe("CorePlinth", [(0, 0), (0.38, 0), (0.38, 0.08), (0.34, 0.12), (0, 0.12)], tk.frame(c), tk.GUNMETAL, "Root", on, sides=12)
        k.torus("PlinthRing", c + V(0, 0, 0.125), UP, 0.29, 0.014, bk.glow("cyan", 1.5), "Root", on, seg=24, sides=4)
        k.lathe("Socket", [(0, 0.12), (0.17, 0.12), (0.15, 0.2), (0.13, 0.36), (0, 0.36)], tk.frame(c), tk.DARK, "Root", on, sides=12)
        for i in range(6):
            a = math.pi / 6 + i * math.pi / 3
            p = c + V(math.cos(a), math.sin(a), 0) * 0.31
            k.block("Pillar", (0.075, 0.075, 1.0), tk.frame(p + V(0, 0, 0.62), rot=(0, 0, math.degrees(a))), tk.BLUE.but(panel=0.12), "Root", on,
                    taper=(0.8, 0.8), bevel=0.008)
            k.block("PillarCap", (0.09, 0.09, 0.06), tk.frame(p + V(0, 0, 1.14), rot=(0, 0, math.degrees(a))), tk.WHITE.but(panel=0), "Root", on,
                    bevel=0.006)
            k.block("PillarFoot", (0.11, 0.11, 0.09), tk.frame(p + V(0, 0, 0.165), rot=(0, 0, math.degrees(a))), tk.WHITE.but(panel=0), "Root", on,
                    bevel=0.006)
        for zz in (0.48, 0.8):
            k.torus("Containment", c + V(0, 0, zz), UP, 0.3, 0.017, bk.glow("cyan", 1.6), "Root", on, seg=24, sides=5)
        k.torus("Crown", c + V(0, 0, 1.17), UP, 0.3, 0.035, tk.WHITE.but(panel=0), "Root", on, seg=24, sides=6)
        k.lathe("CrownCap", [(0, 1.15), (0.13, 1.15), (0.11, 1.23), (0.045, 1.27), (0, 1.27)], tk.frame(c), tk.GUNMETAL, "Root", on, sides=10)
        k.cyl("Emitter", c + V(0, 0, 1.14), c + V(0, 0, 1.11), 0.05, look=bk.glow("crysium", 1.6), bone="Root", on=on, sides=8, bevel=0.0)
        # Power trunk to the command post.
        for dz in (0.05, 0.09):
            k.cyl("Cable", c + V(-0.34, -0.08, dz), V(0.73, -0.15, DECK_Z + dz), 0.02, look=tk.RUBBER, bone="Root", on=on, sides=6, bevel=0.0)

    def crystal(self, k, look=None, on=("Base",)):
        """A Crysium cluster on the turn axis: a tall faceted prism and three leaning shards round it."""
        c = PIVOTS["Crystal"]
        L = look or bk.glow((0.72, 0.66, 1.0), 1.0, lens=0.13)
        k.lathe("Crystal", [(0, 0), (0.09, 0.1), (0.12, 0.2), (0.115, 0.5), (0.08, 0.64), (0, 0.76)], tk.frame(c, rot=(3, -2, 12)), L, "Root",
                list(on), sides=6, bevel=0.0)
        for i, (a, lean, h) in enumerate(((20, 24, 0.36), (150, 30, 0.3), (265, 20, 0.42))):
            r = math.radians(a)
            p = c + V(math.cos(r) * 0.09, math.sin(r) * 0.09, 0.02)
            d = V(math.cos(r) * math.sin(math.radians(lean)), math.sin(r) * math.sin(math.radians(lean)), math.cos(math.radians(lean)))
            k.lathe("CrystalShard", [(0, 0), (0.04, 0.05), (0.05, h * 0.6), (0, h)], tk.frame(p, z=d), L, "Root", list(on), sides=5, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ landing zone
    def pad(self, k, look=None, on=("Base",)):
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        c = PAD
        R = PAD_R
        k.lathe("PadBase", [(0, 0), (R, 0), (R, 0.04), (R - 0.03, 0.055), (0, 0.055)], tk.frame(c), L(tk.GUNMETAL), "Root", on, sides=6, start=0.0)
        surface = tk.GUNMETAL.but(paint=(0.3, 0.32, 0.36), fam="ceramic", panel=0.26, rough=0.6)
        k.lathe("PadTop", [(0, 0.05), (R - 0.05, 0.05), (R - 0.05, PAD_TOP), (0, PAD_TOP)], tk.frame(c), L(surface), "Root", on, sides=6, start=0.0)
        k.lathe("PadSkirt", [(R + 0.01, 0.0), (R + 0.01, 0.03), (R - 0.0, 0.035), (R - 0.0, 0.0)], tk.frame(c), L(tk.HAZARD), "Root", on, sides=6,
                start=0.0, bevel=0.0)
        if not intact:
            return
        z = c + V(0, 0, PAD_TOP + 0.001)
        # Markings: a white hex border, a yellow dashed touchdown circle, a white target ring and three chevrons.
        corners = [V(math.cos(i * math.pi / 3) * (R - 0.12), math.sin(i * math.pi / 3) * (R - 0.12), 0) for i in range(6)]
        for i in range(6):
            a, b = corners[i], corners[(i + 1) % 6]
            mid, d = (a + b) / 2, b - a
            k.block("PadBorder", (d.length, 0.03, 0.006), tk.frame(z + mid, rot=(0, 0, math.degrees(math.atan2(d.y, d.x)))), tk.WHITE.but(panel=0),
                    "Root", on, bevel=0.0)
        for i in range(18):
            a = 2 * math.pi * i / 18
            k.block("PadDash", (0.08, 0.025, 0.006), tk.frame(z + V(math.cos(a) * 0.5, math.sin(a) * 0.5, 0), rot=(0, 0, math.degrees(a) + 90)),
                    tk.YELLOW, "Root", on, bevel=0.0)
        ring = [(0.17, 0), (0.22, 0), (0.22, 0.006), (0.17, 0.006), (0.17, 0.0)]
        k.lathe("PadTarget", ring, tk.frame(z), tk.WHITE.but(panel=0), "Root", on, sides=20, bevel=0.0)
        for i in range(3):
            a = math.radians(90 + i * 120)
            d = V(math.cos(a), math.sin(a), 0)
            tip = d * 0.27
            chev = [(0.0, 0.0), (0.09, 0.07), (0.09, 0.11), (0.0, 0.04), (-0.09, 0.11), (-0.09, 0.07)]
            m = tk.frame(z + tip, z=UP, y=d)
            k.plate("PadChevron", [(p[0], p[1]) for p in chev], 0.006, m, tk.YELLOW, "Root", on, bevel=0.0)
        # Edge lights at the hex corners and mid edges.
        for i in range(12):
            a = i * math.pi / 6
            rr = (R - 0.06) * (1.0 if i % 2 == 0 else math.cos(math.pi / 6))
            p = c + V(math.cos(a) * rr, math.sin(a) * rr, PAD_TOP)
            k.cyl("PadLight", p, p + V(0, 0, 0.018), 0.022, look=bk.glow("yellow" if i % 2 == 0 else "base", 1.8), bone="Root", on=on, sides=8,
                  bevel=0.0)

    def padgear(self, k, look=None, on=("Base",)):
        """The landing zone's tall gear kept out of `Base_Pad`, so the pad's bounds stay its flat top."""
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        c = PAD
        for a in (math.radians(-60), math.radians(120)):
            p = c + V(math.cos(a), math.sin(a), 0) * (PAD_R + 0.08)
            k.cyl("FloodPost", p, p + V(0, 0, 0.5), 0.018, 0.013, look=L(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
            k.block("FloodHead", (0.1, 0.06, 0.06), tk.frame(p + V(0, 0, 0.52), rot=(0, 0, math.degrees(a) + 90)), L(tk.WHITE.but(panel=0)), "Root", on,
                    bevel=0.004)
            if intact:
                k.block("FloodLamp", (0.08, 0.012, 0.035), tk.frame(p + V(-math.cos(a) * 0.032, -math.sin(a) * 0.032, 0.52), rot=(0, 0, math.degrees(a) + 90)),
                        bk.glow("base", 2.0), "Root", on, bevel=0.0)
        # Wind sock on a striped pole at the pad's back corner.
        w = c + V(0.62, 0.66, 0)
        k.cyl("SockPole", w, w + V(0, 0, 0.62), 0.012, 0.009, look=L(tk.WHITE), bone="Root", on=on, sides=6, bevel=0.0)
        if intact:
            k.lathe("WindSock", [(0.04, 0), (0.026, 0.24), (0.0, 0.25)], tk.frame(w + V(0, 0, 0.58), z=V(-0.8, -0.3, -0.25)),
                    tk.HAZARD.but(rings=0.25), "Root", on, sides=8, bevel=0.0)
        # The fuel and power umbilical: a post, a boom over the pad edge and the hose down to a coupling.
        f = V(2.27, 0.86, DECK_Z)
        d = (c - f)
        d.z = 0
        d.normalize()
        yaw = math.degrees(math.atan2(d.y, d.x))
        k.block("BoomPost", (0.1, 0.1, 0.46), tk.frame(f + V(0, 0, 0.23)), L(tk.GUNMETAL.but(panel=0.1)), "Root", on, bevel=0.006)
        k.block("BoomArm", (0.42, 0.06, 0.05), tk.frame(f + d * 0.19 + V(0, 0, 0.44), rot=(0, 0, yaw)), L(tk.YELLOW), "Root", on, bevel=0.004)
        k.block("FuelBox", (0.14, 0.12, 0.12), tk.frame(f + V(0.0, 0.0, 0.06) - d * 0.1, rot=(0, 0, yaw)), L(tk.BLUE.but(panel=0)), "Root", on,
                bevel=0.006)
        if intact:
            k.cyl("Hose", f + d * 0.38 + V(0, 0, 0.42), f + d * 0.5 + V(0, 0, PAD_TOP + 0.02), 0.014, look=tk.RUBBER, bone="Root", on=on, sides=6,
                  bevel=0.0)
            k.block("FuelLight", (0.1, 0.012, 0.02), tk.frame(f + V(0, 0, 0.36) - d * 0.051, rot=(0, 0, yaw + 90)), bk.glow("yellow", 1.5), "Root", on,
                    bevel=0.0)

    # ------------------------------------------------------------------------------------------------ shuttle
    def shuttle(self, k, look=None, on=("Base",), m=None, part="whole"):
        """A stubby VTOL dropship whose plan form reads from above: an arrowhead lifting body (gunmetal belly, blue
        band, white hull) with a raised fuselage and cockpit, two glowing lift-fan rings in the wings and twin
        engines at the back. Nose to -Y, yawed SHUTTLE_YAW on the pad, skids on the pivot plane. `part`: "whole",
        or "rear" / "nose" for the wreck's broken halves."""
        L = look or (lambda x: x)
        on = list(on)
        intact = look is None
        base = m if m is not None else Matrix.Translation(PIVOTS["Shuttle"]) @ Matrix.Rotation(math.radians(SHUTTLE_YAW), 4, "Z")
        r3 = base.to_3x3()
        F = lambda o, rot=None: base @ tk.frame(o, rot=rot)
        whole = [(0, -0.66), (0.2, -0.36), (0.5, 0.22), (0.5, 0.42), (0.18, 0.5), (-0.18, 0.5), (-0.5, 0.42), (-0.5, 0.22), (-0.2, -0.36)]
        rear = [(0.32, -0.05), (0.5, 0.22), (0.5, 0.42), (0.18, 0.5), (-0.18, 0.5), (-0.5, 0.42), (-0.5, 0.22), (-0.32, -0.05)]
        nose = [(0, -0.66), (0.2, -0.36), (0.32, -0.05), (-0.32, -0.05), (-0.2, -0.36)]
        plan = {"whole": whole, "rear": rear, "nose": nose}[part]
        scaled = lambda poly, s: [(x * s, y * s) for x, y in poly]
        has_rear = part in ("whole", "rear")
        has_nose = part in ("whole", "nose")
        if part == "whole":
            for x, y in ((-0.22, -0.28), (0.22, -0.28), (-0.26, 0.3), (0.26, 0.3)):
                k.cyl("Strut", base @ V(x, y, 0.2), base @ V(x * 1.1, y, 0.015), 0.016, look=L(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
                k.lathe("Foot", [(0, 0), (0.042, 0), (0.036, 0.016), (0, 0.016)], tk.frame(base @ V(x * 1.1, y, 0.0)), L(tk.DARK), "Root", on, sides=8,
                        bevel=0.0)
        k.plate("Belly", scaled(plan, 0.9), 0.08, F((0, 0, 0.2)), L(tk.GUNMETAL.but(panel=0.12)), "Root", on, bevel=0.012)
        k.plate("HullBand", scaled(plan, 1.012), 0.03, F((0, 0, 0.252)), L(tk.BLUE.but(panel=0)), "Root", on, bevel=0.006)
        k.plate("Hull", plan, 0.06, F((0, 0, 0.295)), L(tk.WHITE.but(panel=0.14)), "Root", on, bevel=0.012)
        if has_rear:
            # The raised fuselage, its dorsal stripe, the lift fans, the wingtip hazard caps, the tail and the engines.
            y0 = -0.05 if part == "rear" else -0.42
            length = 0.5 - y0
            k.block("Fuselage", (0.32, length, 0.14), F((0, y0 + length / 2, 0.39)), L(tk.WHITE.but(panel=0.12)), "Root", on, taper=(0.72, 0.9),
                    bevel=0.014)
            k.block("Dorsal", (0.07, length * 0.8, 0.012), F((0, y0 + length * 0.45, 0.465)), L(tk.BLUE.but(panel=0)), "Root", on, bevel=0.0)
            for s in (-1, 1):
                fan = V(s * 0.31, 0.24, 0.326)
                k.lathe("FanWell", [(0, 0), (0.13, 0), (0.13, 0.008), (0, 0.008)], tk.frame(base @ fan), L(tk.DARK), "Root", on, sides=16, bevel=0.0)
                k.torus("FanRing", base @ (fan + V(0, 0, 0.008)), r3 @ UP, 0.122, 0.013, bk.glow("azure", 1.8) if intact else L(tk.GUNMETAL), "Root", on,
                        seg=20, sides=4)
                k.lathe("FanHub", [(0, 0), (0.035, 0), (0.03, 0.02), (0, 0.024)], tk.frame(base @ (fan + V(0, 0, 0.008))), L(tk.STEEL), "Root", on,
                        sides=10, bevel=0.0)
                for j in range(3):
                    a = math.radians(j * 120 + 15)
                    k.block("FanBlade", (0.2, 0.03, 0.006), F((fan.x + math.cos(a) * 0.05, fan.y + math.sin(a) * 0.05, fan.z + 0.012),
                            rot=(12, 0, math.degrees(a))), L(tk.GUNMETAL), "Root", on, bevel=0.0)
                k.block("TipCap", (0.05, 0.22, 0.08), F((s * 0.495, 0.32, 0.282)), L(tk.HAZARD), "Root", on, bevel=0.006)
                cant = math.radians(26)
                k.plate("TailFin", [(0, 0), (0.2, 0), (0.24, 0.15), (0.12, 0.15)], 0.025,
                        tk.frame(base @ V(s * 0.1, 0.3, 0.45), z=r3 @ V(math.cos(cant), 0, -s * math.sin(cant)), y=r3 @ V(s * math.sin(cant), 0, math.cos(cant))),
                        L(tk.BLUE.but(panel=0)), "Root", on, bevel=0.004)
                k.cyl("Engine", base @ V(s * 0.12, 0.46, 0.34), base @ V(s * 0.12, 0.6, 0.34), 0.058, 0.068, look=L(tk.GUNMETAL.but(rings=0.2)), bone="Root",
                      on=on, sides=12)
                if intact:
                    k.lathe("ExhaustGlow", [(0, 0), (0.05, 0), (0.05, 0.01), (0, 0.01)], tk.frame(base @ V(s * 0.12, 0.601, 0.34), z=r3 @ V(0, 1, 0)),
                            bk.glow("azure", 2.0, lens=0.04), "Root", on, sides=12, bevel=0.0)
                    k.lathe("LiftGlow", [(0, 0), (0.1, 0), (0.1, 0.008), (0, 0.008)], tk.frame(base @ V(s * 0.31, 0.24, 0.158), z=r3 @ V(0, 0, -1)),
                            bk.glow("azure", 1.3), "Root", on, sides=14, bevel=0.0)
                    for j in range(3):
                        k.block("Window", (0.012, 0.05, 0.028), F((s * 0.152, -0.1 + j * 0.12, 0.395)), bk.glow("base", 1.4), "Root", on, bevel=0.0)
            if intact:
                k.cyl("Antenna", base @ V(0.04, 0.32, 0.46), base @ V(0.04, 0.38, 0.6), 0.006, look=tk.GUNMETAL, bone="Root", on=on, sides=5, bevel=0.0)
        if has_nose:
            k.block("NoseCone", (0.26, 0.3, 0.12), F((0, -0.5, 0.37)), L(tk.WHITE.but(panel=0.08)), "Root", on, taper=(0.55, 0.45), shift=(0, 0.03),
                    bevel=0.014)
            k.block("Cockpit", (0.22, 0.2, 0.07), F((0, -0.36, 0.46), rot=(-14, 0, 0)), L(tk.GLASS), "Root", on, taper=(0.7, 0.55), bevel=0.01)
            k.block("NoseStripe", (0.2, 0.03, 0.02), F((0, -0.6, 0.33)), L(tk.HAZARD), "Root", on, bevel=0.003)
            if intact:
                k.block("CockpitGlow", (0.18, 0.012, 0.014), F((0, -0.455, 0.44), rot=(-14, 0, 0)), bk.glow("base", 1.8), "Root", on, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ damage overlay
    def damage(self, k):
        on = ["Damage"]
        rng = np.random.default_rng(37)
        z = DECK_Z
        for (x, y, zz, r, seed) in ((0.0, -0.5, z + 0.613, 0.5, 1), (0.32, -0.78, z + 0.993, 0.2, 2), (-0.5, -1.05, z + 0.003, 0.38, 3),
                                    (0.7, -1.1, z + 0.003, 0.3, 4), (-0.84, 0.92, z + 0.393, 0.17, 5), (1.58, 1.56, z + PAD_TOP + 0.003, 0.4, 6),
                                    (-1.3, -0.4, z + 0.003, 0.32, 7), (1.3, -0.5, z + 0.003, 0.3, 8), (-0.47, 1.1, z + 0.003, 0.35, 9),
                                    (2.0, 1.3, z + 0.003, 0.25, 10), (RADAR.x, RADAR.y, z + 0.403, 0.3, 11)):
            k.plate("Soot", blot(r, seed), 0.004, tk.frame((x, y, zz)), bk.burn(tk.WHITE), "Root", on, bevel=0.0)
        # Smashed ops glass across the command front, hot edges under it.
        front = CMD.y - 0.69
        for x in (-0.3, 0.25):
            bk.shards(k, "Smash", bk.rect_poly(0.32, 0.12), 0.008, tk.frame((x, front - 0.005, z + 0.47), rot=(62, 0, 0)), bk.burn(tk.GLASS), "Root", on,
                      count=3, gap=0.01, lift=0.006, tilt=6, seed=int(x * 10) + 19)
            k.block("SmashGlow", (0.26, 0.008, 0.01), tk.frame((x, front - 0.02, z + 0.41)), bk.glow("ember", 1.3), "Root", on, bevel=0.0)
        # Hex plates torn off the glacis, a breach glowing in the blast door.
        k.block("Breach", (0.16, 0.01, 0.12), tk.frame((0.06, front - 0.13, z + 0.18), rot=(0, 18, 0)), bk.glow("ember", 1.5), "Root", on, bevel=0.0)
        for i, (x, y, r) in enumerate(((-0.5, -1.5, 40), (0.62, -1.55, -25))):
            k.plate("FallenHex", bk.hex_poly(0.1), 0.025, tk.frame((x, y, 0.02), rot=(8, -6, r)), bk.scorch(tk.WHITE if i else tk.BLUE, 0.6), "Root", on,
                    bevel=0.004)
        # A torn deck plate on the prow, a bent antenna on the cupola, a toppled container, a holed tank.
        bk.shards(k, "TornPlate", bk.rect_poly(0.42, 0.3), 0.025, tk.frame((-0.6, -1.0, z + 0.03), rot=(10, -8, 15)), bk.scorch(tk.WHITE, 0.5), "Root",
                  on, count=3, gap=0.02, lift=0.04, tilt=24, seed=12)
        k.block("TornGlow", (0.3, 0.02, 0.012), tk.frame((-0.6, -0.98, z + 0.008), rot=(0, 0, 15)), bk.glow("ember", 1.4), "Root", on, bevel=0.0)
        cz = CMD.z + 0.61 + 0.38 + 0.26
        k.cyl("BentAntenna", V(0.18, -0.55, cz), V(0.38, -0.75, cz + 0.16), 0.009, look=bk.burn(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        k.block("FallenContainer", (0.25, 0.5, 0.24), tk.frame((0.18, 2.0, z + 0.12), rot=(0, 78, 24)), bk.scorch(tk.BLUE, 0.6), "Root", on, bevel=0.008)
        bk.shards(k, "TankHole", bk.rect_poly(0.14, 0.18), 0.01, tk.frame((STORE.x, 0.5 + 0.215, z + 0.36), rot=(90, 0, 0)), bk.burn(tk.BLUE), "Root",
                  on, count=3, gap=0.012, lift=0.008, tilt=10, seed=5)
        k.block("TankGlow", (0.08, 0.008, 0.1), tk.frame((STORE.x, 0.5 + 0.222, z + 0.36)), bk.glow("ember", 1.3), "Root", on, bevel=0.0)
        # A cracked bay wall and the radar hut scorched.
        bk.shards(k, "BayCrack", bk.rect_poly(0.3, 0.2), 0.015, tk.frame((BAYS.x - 0.3, BAYS.y - (2 * WELL + 3 * WALL) / 2 - 0.005, z + 0.16),
                  rot=(90, 0, 0)), bk.scorch(tk.BLUE, 0.6), "Root", on, count=3, gap=0.015, lift=0.01, tilt=14, seed=8)
        # Camera side: a breach in the blue habitat tube, a scorched back wall, a barrier knocked flat.
        hb = HAB + V(0.18, 0.27 + 0.172, 0.25)
        bk.shards(k, "HabBreach", bk.rect_poly(0.2, 0.14), 0.01, tk.frame(hb, rot=(90, 0, 0)), bk.burn(tk.BLUE), "Root", on, count=3, gap=0.014,
                  lift=0.01, tilt=12, seed=17)
        k.block("HabGlow", (0.1, 0.008, 0.05), tk.frame(hb + V(0, 0.008, 0)), bk.glow("ember", 1.4), "Root", on, bevel=0.0)
        k.plate("SootWall", blot(0.22, 23), 0.004, tk.frame((0.35, CMD.y + 0.693, z + 0.3), rot=(90, 0, 0)), bk.burn(tk.BLUE), "Root", on, bevel=0.0)
        k.block("BarrierDown", (0.55, 0.09, 0.12), tk.frame((0.62, 2.55, 0.05), rot=(78, 0, 8)), bk.scorch(tk.GUNMETAL, 0.6), "Root", on, bevel=0.008)
        for p in ((-0.2, 1.05, z + 0.006), (1.0, 1.25, z + 0.006), (-1.25, 1.55, z + 0.006), (2.05, 1.0, z + 0.006)):
            k.block("Ember", (0.07, 0.03, 0.012), tk.frame(p, rot=(0, 0, 35)), bk.glow("ember", 1.2), "Root", on, bevel=0.0)
        for i in range(14):
            p = V(rng.uniform(-2.2, 2.2), rng.uniform(-1.3, 0.2), z)
            size = rng.uniform(0.03, 0.07, 3) * (1, 1, 0.4)
            k.block("Chip", tuple(size), tk.frame(p + V(0, 0, size[2] / 2), rot=(rng.uniform(-20, 20), rng.uniform(-20, 20), rng.uniform(0, 180))),
                    bk.scorch([tk.WHITE, tk.BLUE, tk.GUNMETAL][i % 3], 0.4), "Root", on, bevel=0.003)
        for p in ((0.72, -0.4, z + 0.3), (-0.72, -0.7, z + 0.22), (CORE.x - 0.33, CORE.y, z + 0.6), (-0.47 + 0.3, 1.62 + 0.45, z + 0.25)):
            k.block("HotSeam", (0.012, 0.12, 0.08), tk.frame(p), bk.glow("ember", 1.2), "Root", on, bevel=0.0)

    # ------------------------------------------------------------------------------------------------ wreck
    def wreck(self, k):
        on = ["Wreck"]
        B = bk.burn
        z = DECK_Z
        rng = np.random.default_rng(41)
        self.deck(k, look=B, on=on)
        rear_c, rear_s, _ = REAR
        for i, (cx, cy, w, d, seed) in enumerate(((-1.4, 0.0, 2.2, 1.3, 1), (1.3, 0.0, 2.3, 1.3, 2), (-1.4, 1.6, 2.2, 1.4, 3), (1.3, 1.6, 2.3, 1.4, 4),
                                                  (0, -1.03, 1.8, 0.55, 5))):
            bk.shards(k, "DeckShard", bk.rect_poly(w, d, 0.05), 0.03, tk.frame((cx, cy, 0.215)), B(tk.WHITE), "Root", on, count=4, gap=0.03,
                      lift=0.025, tilt=5, seed=seed)
        # The command post caved in: a stump of the lower storey, the roof slab fallen in, the upper storey beside it.
        c = CMD
        k.block("Storey1", (1.42, 1.38, 0.24), tk.frame((c.x, c.y, z + 0.12)), B(tk.BLUE), "Root", on, taper=(0.96, 0.95), bevel=0.012)
        k.block("Storey1Wall", (0.5, 0.1, 0.42), tk.frame((-0.45, c.y + 0.64, z + 0.21), rot=(0, 6, 0)), B(tk.BLUE), "Root", on, bevel=0.01)
        k.block("Roof", (1.48, 1.44, 0.05), tk.frame((0.05, c.y + 0.02, z + 0.3), rot=(13, -9, 4)), B(tk.WHITE), "Root", on, bevel=0.008)
        k.block("Storey2", (0.92, 0.86, 0.34), tk.frame((0.95, c.y - 0.85, z + 0.05), rot=(0, 74, 18)), B(tk.BLUE), "Root", on, bevel=0.01)
        k.block("Glacis", (1.48, 0.12, 0.3), tk.frame((0, c.y - 0.72, z + 0.12), rot=(-40, 0, 3)), B(tk.GUNMETAL), "Root", on, bevel=0.01)
        k.lathe("CupolaCap", [(0, 0), (0.33, 0), (0.3, 0.05), (0.12, 0.09), (0, 0.09)], tk.frame((-0.75, -1.55, 0.04), rot=(70, 10, 0)), B(tk.WHITE),
                "Root", on, sides=8)
        # The radar mast toppled across the bays, its array smashed on the deck.
        foot = RADAR + V(0, 0, 0.3)
        tip = V(-0.6, -0.25, 0.32)
        for off in (V(0.07, 0.07, 0), V(-0.07, -0.07, 0), V(0.07, -0.07, 0.06), V(-0.07, 0.07, 0.06)):
            k.cyl("MastLeg", foot + off * 2.4, tip + off * 0.8, 0.018, 0.013, look=B(tk.GUNMETAL), bone="Root", on=on, sides=6, bevel=0.0)
        k.lathe("RadarHut", [(0, 0), (0.42, 0), (0.42, 0.06), (0.35, 0.08), (0.34, 0.22), (0, 0.22)], tk.frame(RADAR), B(tk.BLUE), "Root", on, sides=8)
        bk.shards(k, "ArrayShard", bk.rect_poly(0.94, 0.54), 0.06, tk.frame((-0.5, -0.05, z + 0.06), rot=(4, 3, 30)), B(tk.BLUE), "Root", on, count=4,
                  gap=0.03, lift=0.03, tilt=12, seed=21)
        k.block("ArrayFrame", (0.98, 0.05, 0.12), tk.frame((-0.3, 0.35, z + 0.05), rot=(80, 0, 22)), B(tk.WHITE), "Root", on, bevel=0.008)
        # The bays broken: wall stubs, lids torn off and flung down.
        bspan = 2 * WELL + 3 * WALL
        for i, (dx, dy, w, d, h, rot) in enumerate(((0, -0.62, bspan, WALL, 0.16, 0), (0, 0.62, bspan * 0.6, WALL, 0.24, 4), (-0.62, 0, WALL, bspan * 0.8, 0.2, 0),
                                                   (0.62, 0.15, WALL, bspan * 0.5, 0.12, -6))):
            k.block("BayStub", (w, d, h), tk.frame(BAYS + V(dx, dy, h / 2), rot=(0, 0, rot)), B(tk.BLUE), "Root", on, bevel=0.008)
        k.block("BayBase", (bspan + 0.06, bspan + 0.06, 0.07), tk.frame(BAYS + V(0, 0, 0.035)), B(tk.GUNMETAL), "Root", on, bevel=0.008)
        for i, (x, y, rx, rz) in enumerate(((-2.4, -0.5, 8, 20), (-1.1, -0.35, -12, -35), (-2.55, 1.0, 70, 80))):
            k.block("LidFallen", (0.65, 0.64, 0.035), tk.frame((x, y, z + 0.05 + 0.1 * (i == 2)), rot=(rx, 0, rz)), B(tk.WHITE), "Root", on, bevel=0.006)
        # The shuttle broke its back on the pad: the hull in two pieces, a nacelle and a wing thrown off.
        self.pad(k, look=B, on=on)
        top = PAD + V(0, 0, PAD_TOP)
        rear_m = Matrix.Translation(top + V(0.12, 0.18, -0.17)) @ Matrix.Rotation(math.radians(SHUTTLE_YAW + 16), 4, "Z") @ Matrix.Rotation(
            math.radians(-9), 4, "X") @ Matrix.Rotation(math.radians(6), 4, "Y")
        self.shuttle(k, look=B, on=on, m=rear_m, part="rear")
        nose_m = Matrix.Translation(top + V(-0.42, -0.5, -0.2)) @ Matrix.Rotation(math.radians(SHUTTLE_YAW - 38), 4, "Z") @ Matrix.Rotation(
            math.radians(14), 4, "X") @ Matrix.Rotation(math.radians(-18), 4, "Y")
        self.shuttle(k, look=B, on=on, m=nose_m, part="nose")
        for x, y, r in ((0.95, 2.1, 20), (2.4, 2.15, -40)):
            k.block("StrutFallen", (0.03, 0.2, 0.03), tk.frame((x, y, z + 0.015), rot=(0, 0, r)), B(tk.GUNMETAL), "Root", on, bevel=0.0)
        # The silo cage broken: three pillar stubs, three fallen; the crystal a dark cracked stub.
        cc = CORE
        k.lathe("CorePlinth", [(0, 0), (0.38, 0), (0.38, 0.08), (0.34, 0.12), (0, 0.12)], tk.frame(cc), B(tk.GUNMETAL), "Root", on, sides=12)
        for i in range(6):
            a = math.pi / 6 + i * math.pi / 3
            p = cc + V(math.cos(a), math.sin(a), 0) * 0.31
            if i % 2 == 0:
                k.block("PillarStub", (0.075, 0.075, 0.3 + 0.08 * i), tk.frame(p + V(0, 0, 0.15 + 0.04 * i), rot=(0, 0, math.degrees(a))), B(tk.BLUE),
                        "Root", on, bevel=0.008)
            else:
                d = V(math.cos(a), math.sin(a), 0)
                k.block("PillarFallen", (0.075, 0.075, 0.8), tk.frame(p + d * 0.42 + V(0, 0, 0.06), z=d + V(0, 0, 0.12), y=UP), B(tk.BLUE), "Root",
                        on, bevel=0.008)
        dark = tk.GLASS.but(paint=(0.12, 0.09, 0.16), emit=bk.GLOW["crysium"], glow=0.35, fam="glow")
        k.lathe("CrystalStub", [(0, 0), (0.1, 0.1), (0.13, 0.2), (0.11, 0.3), (0, 0.36)], tk.frame(cc + V(0, 0, 0.12), rot=(9, 6, 12)), dark, "Root",
                on, sides=6, bevel=0.0)
        k.lathe("CrystalShard", [(0, 0), (0.05, 0.05), (0.05, 0.18), (0, 0.3)], tk.frame(cc + V(-0.4, -0.35, 0.03), rot=(80, 0, 60)), dark, "Root",
                on, sides=5, bevel=0.0)
        # Tanks: one burst open and squat, one toppled against the barrier; containers tumbled.
        t1 = V(STORE.x, -0.04, z)
        k.lathe("TankStub", [(0, 0), (0.25, 0), (0.25, 0.05), (0.21, 0.05), (0.21, 0.26), (0.17, 0.32), (0, 0.32)], tk.frame(t1), B(tk.BLUE), "Root",
                on, sides=12)
        bk.shards(k, "TankPeel", bk.rect_poly(0.3, 0.2), 0.012, tk.frame(t1 + V(0.05, -0.24, 0.12), rot=(70, 0, 10)), B(tk.BLUE), "Root", on, count=3,
                  gap=0.02, lift=0.02, tilt=20, seed=33)
        k.cyl("TankFallen", V(2.05, 0.42, z + 0.21), V(2.45, 1.0, z + 0.2), 0.21, look=B(tk.BLUE.but(rings=0.18)), bone="Root", on=on, sides=14)
        for (x, y, zz, rx, ry, rz) in ((0.55, 1.0, 0.12, 0, 0, 25), (0.25, 1.62, 0.12, 0, 88, -15), (0.7, 2.05, 0.1, 12, 0, 70)):
            k.block("ContainerWreck", (0.25, 0.5, 0.24), tk.frame((x, y, z + zz), rot=(rx, ry, rz)), B(tk.GUNMETAL), "Root", on, bevel=0.008)
        # The habitat tubes cracked and lying, the greenhouse dome gone.
        h = HAB
        k.cyl("HabFallen", h + V(-0.5, -0.3, 0.15), h + V(0.42, -0.2, 0.12), 0.17, look=B(tk.WHITE.but(rings=0.16)), bone="Root", on=on, sides=14)
        k.cyl("HabFallen", h + V(-0.45, 0.4, 0.13), h + V(0.1, 0.16, 0.16), 0.17, look=B(tk.BLUE.but(rings=0.16)), bone="Root", on=on, sides=14)
        k.cyl("NodeStub", h, h + V(0, 0, 0.2), 0.2, look=B(tk.GUNMETAL), bone="Root", on=on, sides=16)
        # Hardpoint plinths scorched, their junctions knocked off.
        for s in (-1, 1):
            c2 = V(s * HARD_X, HARD_Y, z)
            k.block("HardPlinth", (0.56, 0.56, 0.08), tk.frame(c2 + V(0, 0, 0.04)), B(tk.GUNMETAL), "Root", on, taper=(0.94, 0.94), bevel=0.008)
            k.block("JunctionFallen", (0.24, 0.42, 0.17), tk.frame(c2 + V(s * 0.3, -0.25, 0.04), rot=(14, 70 * s, 30)), B(tk.BLUE), "Root", on, bevel=0.008)
        for i in range(22):
            p = V(rng.uniform(-2.7, 2.7), rng.uniform(-1.8, 2.5), 0.0)
            inside = abs(p.x) < 2.45 and -0.65 < p.y < 2.3 or (abs(p.x) < 0.95 and -1.3 < p.y < -0.7)
            size = rng.uniform(0.05, 0.13, 3) * (1, 1, 0.3)
            k.block("Scrap", tuple(size), tk.frame(p + V(0, 0, (z if inside else 0) + size[2] / 2),
                    rot=(rng.uniform(-25, 25), rng.uniform(-25, 25), rng.uniform(0, 180))), B([tk.WHITE, tk.BLUE, tk.GUNMETAL, tk.STEEL][i % 4]), "Root",
                    on, bevel=0.004)
        for p in ((0.05, -0.5, z + 0.29), (0.5, -0.2, z + 0.27), (-0.2, -0.1, z + 0.27), (CORE.x, CORE.y - 0.1, z + 0.2), (-0.8, 1.4, z + 0.32),
                  (1.6, 1.6, z + 0.3), (-1.7, 0.3, z + 0.2), (2.1, 0.0, z + 0.33)):
            k.block("Ember", (0.07, 0.03, 0.014), tk.frame(p, rot=(0, 0, 25)), bk.glow("ember", 1.1), "Root", on, bevel=0.0)



# ---------------------------------------------------------------------------------------------------- objects

def bake_set(kit, out_dir, bake=True):
    """`base_kit.bake_set` with a wider bake spacing (the expanded deck is 5 m across)."""
    uv = tk.unwrap_kit(kit, margin=UV_MARGIN)
    paths = {s.lower(): os.path.join(out_dir, f"{kit.name}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    if bake or not all(os.path.exists(p) for p in paths.values()):
        paths = tk.bake(kit, out_dir, size=TEX, low_size=TEX_LOW, samples=12, spacing=BAKE_SPACING)
    entries, detail = tk.detail_entries(uv["world_per_uv"])
    mat = C.game_material(kit.name + "_Mat", paths, 1.5, 0.45, entries)
    report = {"asset": kit.name, "texture_size": TEX, "low_size": TEX_LOW, "maps": ["Albedo", "Normal", "Emission", "Mask"],
              "mask": "R AO, G smoothness, B metal detail mask, A ceramic detail mask", "uv": uv, "detail": detail}
    return mat, report


def module_objects(base, mat, lod=False, label=""):
    objs = {}
    for m in MODULES:
        name = f"{NAME}_{m}" + ("_LOD1" if lod else "") + label
        o = bk.module_object(base.module[m], name, PIVOTS[m], mat, lod=lod)
        if m in ANIMATED:
            upright(o)
        objs[m] = o
    return objs


def sockets(shuttle=None):
    out = []
    for name, p in SOCKETS.items():
        e = C.link(bpy.data.objects.new(name, None))
        e.empty_display_size = 0.08
        e.location = p
        out.append(e)
    for name, p in NEW_SOCKETS.items():
        e = C.link(bpy.data.objects.new(name, None))
        e.empty_display_size = 0.08
        e.location = p
        upright(e)
        out.append(e)
    if shuttle is not None:
        # Engine sockets ride on the shuttle (children, no rotation of their own relative to it).
        base = Matrix.Translation(PIVOTS["Shuttle"]) @ Matrix.Rotation(math.radians(SHUTTLE_YAW), 4, "Z")
        for name, p in (("Socket_ShuttleLift_L", V(-0.52, 0.16, 0.2)), ("Socket_ShuttleLift_R", V(0.52, 0.16, 0.2)),
                        ("Socket_ShuttleExhaust", V(0.0, 0.6, 0.32))):
            e = C.link(bpy.data.objects.new(name, None))
            e.empty_display_size = 0.05
            e.parent = shuttle
            bpy.context.view_layer.update()
            e.matrix_world = Matrix.Translation(base @ p) @ Matrix.Rotation(math.radians(90), 4, "X")
            out.append(e)
    return out


def state_object(base, mat, state, label=""):
    return bk.module_object(base.module[state], f"{NAME}_{state}{label}", V(0, 0, 0), mat)


def reach(objs):
    """The footprint as Unity's StructureClearance measures it: the widest bounding-box corner round the pivot (XY)."""
    bpy.context.view_layer.update()
    best = 0.0
    for o in objs:
        if o.type != "MESH":
            continue
        for corner in o.bound_box:
            w = o.matrix_world @ Vector(corner)
            best = max(best, math.hypot(w.x, w.y))
    return best


def extent(objs):
    bpy.context.view_layer.update()
    xs, ys, zs = [], [], []
    for o in objs:
        if o.type != "MESH":
            continue
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            xs.append(w.x); ys.append(w.y); zs.append(w.z)
    return [round(min(xs), 3), round(max(xs), 3)], [round(min(ys), 3), round(max(ys), 3)], [round(min(zs), 3), round(max(zs), 3)]


def build_base(bake=True):
    base = Base2()
    kit = base.kit()
    mat, report = bake_set(kit, OUT, bake)
    lod0 = module_objects(base, mat)
    lod1 = module_objects(base, mat, lod=True)
    socks = sockets(lod0["Shuttle"])
    bpy.context.view_layer.update()
    bk.export_fbx(os.path.join(OUT, f"{NAME}.fbx"), list(lod0.values()) + list(lod1.values()) + socks, animated=False)
    dmg = state_object(base, mat, "Damage")
    bk.export_fbx(os.path.join(OUT, f"{NAME}_Damage.fbx"), [dmg], animated=False)
    wreck = state_object(base, mat, "Wreck")
    bk.export_fbx(os.path.join(OUT, f"{NAME}_Wreck.fbx"), [wreck], animated=False)
    report["modules"] = {m: {"object": f"{NAME}_{m}", "pivot": [round(c, 3) for c in PIVOTS[m]], "tris": C.tri_count(lod0[m]),
                             "tris_lod1": C.tri_count(lod1[m]), "upright": m in ANIMATED} for m in MODULES}
    report["tris_total"] = sum(v["tris"] for v in report["modules"].values())
    report["tris_total_lod1"] = sum(v["tris_lod1"] for v in report["modules"].values())
    report["damage_overlay"] = {"fbx": f"{NAME}_Damage.fbx", "tris": C.tri_count(dmg)}
    report["wreck"] = {"fbx": f"{NAME}_Wreck.fbx", "tris": C.tri_count(wreck)}
    report["sockets"] = {n: [round(c, 3) for c in p] for n, p in SOCKETS.items()}
    report["sockets_new"] = {n: [round(c, 3) for c in p] for n, p in NEW_SOCKETS.items()}
    report["sockets_shuttle"] = ["Socket_ShuttleLift_L", "Socket_ShuttleLift_R", "Socket_ShuttleExhaust"]
    intact = list(lod0.values())
    report["footprint"] = {"reach": round(reach(intact), 3), "reach_wreck": round(reach([wreck]), 3), "extent_xyz": extent(intact),
                           "note": "reach = widest mesh bounding-box corner round the pivot (StructureClearance.BaseFootprint); "
                                   "extent in Blender axes (-Y = road / Unity +Z)"}
    report["animated"] = {
        "Base_Dish": {"pivot": [round(c, 3) for c in PIVOTS["Dish"]], "turn": "Unity local +Y (= world up); node rotation identity",
                      "note": "pivot on the mast axis at the bearing top; turn about Y only"},
        "Base_Beacon": {"pivot": [round(c, 3) for c in PIVOTS["Beacon"]], "anim": "blink its emission (static, on the radar axis above the head)"},
        "Base_Crystal": {"pivot": [round(c, 3) for c in PIVOTS["Crystal"]], "turn": "Unity local +Y; pulses"},
        "Base_Shuttle": {"pivot": [round(c, 3) for c in PIVOTS["Shuttle"]], "anim": "translate along Unity +Y to take off; skids rest on the pivot plane",
                         "yaw_deg_baked": SHUTTLE_YAW},
        "Base_BayDoor_N": {"pivots": {f"BayDoor_{i + 1}": [round(c, 3) for c in PIVOTS[f"BayDoor_{i + 1}"]] for i in range(4)},
                           "rest": f"open, {DOOR_OPEN} degrees up on the outer wall",
                           "anim": "close by turning DOOR_OPEN degrees about the hinge (Unity local Z, the road axis), lid falling toward the "
                                   "Base_Bays centre"}}
    report["pivot"] = "base centre on the ground (LevelVisualBuilder.BasePosition); -Y (Unity +Z) faces the road"
    report["timelapse_order"] = MODULES
    report["scale"] = "final size: do not apply LevelVisualBuilder.BaseScale"
    report["axes"] = "upright parts: FBX node rotation identity (local +Y up). Other modules and the round-9 sockets keep Blender's root -90 X."
    tk.write_json(os.path.join(OUT, f"{NAME}_report.json"), report)
    C.log(NAME, report["tris_total"], report["tris_total_lod1"], report["damage_overlay"], report["wreck"], report["footprint"])
    bk.remove(list(lod0.values()) + list(lod1.values()) + socks + [dmg, wreck])
    return base, kit, mat, report


# ---------------------------------------------------------------------------------------------------- previews

def place_base(base, mat, state="intact", at=V(0, 0, 0), label="", modules=None):
    if state == "wreck":
        o = state_object(base, mat, "Wreck", label)
        o.location = at
        return [o]
    objs = []
    for m, o in module_objects(base, mat, label=label).items():
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


def import_prop(path, folder, prefix, location, yaw_deg, keep=None):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    tex = {k.lower(): os.path.join(folder, f"{prefix}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
    mat = C.game_material(prefix + "_Prev", tex, emission_strength=2.0)
    for o in new:
        o.animation_data_clear()
        if o.type == "MESH" and "LOD1" in o.name:
            o.hide_render = True
        if o.type == "MESH" and keep and not o.name.endswith(keep):
            o.hide_render = True
        if o.parent is None:
            o.location = Vector(location) + o.location
            o.rotation_euler.z += math.radians(yaw_deg)
        if o.type == "MESH":
            o.data.materials.clear()
            o.data.materials.append(mat)
    return new


def previews(base, mat, out, oldbase=None):
    os.makedirs(out, exist_ok=True)
    stage = bk.road_stage(2.0, 14, axis="y", centre=(0, -8.7))
    # Hero shot at play zoom and a closer one, with towers and a few Skitters on the road.
    placed = place_base(base, mat, label="g")
    placed += bk.tower("Gun", "T2", (1.7, -3.4, 0), 0, head_deg=200)
    placed += bk.tower("Laser", "T1", (-1.7, -4.2, 0), 0, head_deg=160)
    sk = bk.skitters([(0.3, -3.0, 0), (-0.4, -3.8, 0), (0.2, -5.0, 0), (-0.3, -6.2, 0)], yaw=0.0)
    bk.render_game(V(0, -0.6, 0.4), os.path.join(out, "base_game.png"), zoom=1.0)
    bk.render_game(V(0, 0.3, 0.5), os.path.join(out, "base_hero.png"), zoom=2.0, width=1600, height=900)
    bk.unstage(sk)
    bk.remove(placed)
    # Top view (orthographic, straight down).
    objs = place_base(base, mat, label="t")
    rig = tk.game_lights()
    cam_data = bpy.data.cameras.new("TopCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 7.6
    cam = C.link(bpy.data.objects.new("TopCam", cam_data))
    cam.location = (0, 0.3, 30)
    C.look_at(cam, Vector((0, 0.3, 0)))
    C.render(cam, os.path.join(out, "base_top.png"), 1200, 1200)
    bk.remove(rig + [cam])
    # Three-quarter views and the module close-ups.
    for name, az, el in (("base_close_back", 18, 30), ("base_close_front", 200, 26), ("base_close_side", 110, 26)):
        bk.render_close(V(0, 0.4, 0.6), os.path.join(out, name + ".png"), 10.5, azimuth=az, elevation=el, lens=50, width=1280, height=800,
                        light_scale=2.6)
    for name, target, dist, az, el in (("mod_command", CMD + V(0, -0.1, 0.5), 3.4, 205, 20), ("mod_command_back", CMD + V(0, 0.2, 0.5), 3.4, 20, 30),
                                       ("mod_radar", RADAR + V(0, 0, 1.4), 4.2, 25, 18), ("mod_radar_head", HEAD + V(0, 0, 0.3), 2.0, 200, 22),
                                       ("mod_pad", PAD + V(0, 0, 0.25), 3.4, 30, 34), ("mod_bays", BAYS + V(0, 0, 0.2), 2.8, 25, 42),
                                       ("mod_storage", V(1.75, 0.6, 0.45), 3.6, 60, 26), ("mod_habitat", HAB + V(0, 0, 0.3), 2.6, 15, 30),
                                       ("mod_hardpoint", V(HARD_X, HARD_Y, 0.4), 2.0, 140, 26)):
        bk.render_close(target, os.path.join(out, name + ".png"), dist, azimuth=az, elevation=el, lens=50, width=1100, height=800, light_scale=1.4)
    bk.remove(objs)
    # States row at play zoom and close: intact, damaged, wreck.
    placed = []
    for i, st in enumerate(("intact", "damaged", "wreck")):
        placed += place_base(base, mat, state=st, at=V(-(i - 1) * 6.2, 0, 0), label=f"s{i}")
    bk.render_game(V(0, 0.4, 0.4), os.path.join(out, "base_states_game.png"), zoom=1.15, width=1600, height=720)
    bk.remove(placed)
    for st in ("damaged", "wreck"):
        objs = place_base(base, mat, state=st, label="d" + st)
        bk.render_close(V(0, 0.4, 0.5), os.path.join(out, f"base_{st}_close.png"), 10.5, azimuth=200, elevation=28, lens=50, width=1280, height=800,
                        light_scale=2.6)
        bk.render_close(V(0, 0.4, 0.5), os.path.join(out, f"base_{st}_back.png"), 10.5, azimuth=18, elevation=30, lens=50, width=1280, height=800,
                        light_scale=2.6)
        bk.remove(objs)
    # Size comparison: the new base, a gate and a gun tower, and the round-9 base if its export was saved aside.
    placed = place_base(base, mat, label="cmp")
    gate = []
    gate_fbx = os.path.join(C.EXPORTS, "Gate", "Gate_G1.fbx")
    if os.path.exists(gate_fbx):
        gate = import_prop(gate_fbx, os.path.join(C.EXPORTS, "Gate"), "Gate", (5.2, -0.5, 0), 0, keep="G1")
    placed += bk.tower("Gun", "T2", (3.6, -1.6, 0), 0, head_deg=200)
    old = []
    if oldbase and os.path.exists(os.path.join(oldbase, "Base.fbx")):
        old = import_prop(os.path.join(oldbase, "Base.fbx"), oldbase, "Base", (-5.6, 0, 0), 0)
        for o in old:
            if o.type == "MESH" and "LOD1" in o.name:
                o.hide_render = True
    bk.render_game(V(-0.2, 0.0, 0.5), os.path.join(out, "base_size_compare.png"), zoom=1.4, width=1600, height=800)
    bk.remove(placed + gate + old)
    # Timelapse strip.
    shots = []
    steps = [["Deck"], ["Deck", "Command", "Hardpoints"], ["Deck", "Command", "Hardpoints", "Habitat", "Radar", "Dish", "Beacon"],
             MODULES[:12], MODULES[:16], MODULES]
    for i, mods in enumerate(steps):
        objs = place_base(base, mat, label=f"tl{i}", modules=mods)
        p = os.path.join(out, f"_base_tl_{i}.png")
        bk.render_close(V(0, 0.4, 0.6), p, 11.0, azimuth=200, elevation=30, lens=50, width=480, height=360, light_scale=2.6, bloom=False)
        shots.append(p)
        bk.remove(objs)
    C.contact_sheet(shots, os.path.join(out, "base_timelapse_strip.png"), 6, 400)
    for p in shots:
        os.remove(p)
    bk.remove(stage)


def draft(out):
    """Clay renders of the layout without a bake or an export (fast iteration on the shapes)."""
    base = Base2()
    base.kit()
    mat = C.principled("Clay", color=(0.55, 0.57, 0.6), rough=0.6)
    os.makedirs(out, exist_ok=True)
    objs = place_base(base, mat, label="dr")
    C.log("draft footprint", round(reach(objs), 3), extent(objs))
    bk.render_game(V(0, 0.3, 0.5), os.path.join(out, "draft_hero.png"), zoom=2.0, width=1600, height=900, bloom=False)
    bk.render_close(V(0, 0.4, 0.6), os.path.join(out, "draft_front.png"), 10.5, azimuth=200, elevation=26, lens=50, width=1280, height=800,
                    light_scale=2.6, bloom=False)
    bk.remove(objs)
    for st in ("damaged", "wreck"):
        objs = place_base(base, mat, state=st, label="dr" + st)
        bk.render_game(V(0, 0.3, 0.5), os.path.join(out, f"draft_{st}.png"), zoom=2.0, width=1600, height=900, bloom=False)
        bk.remove(objs)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = args.get("preview") or PREVIEW
    bake = not args.get("nobake")
    C.preview_world()
    if args.get("draft"):
        draft(out)
        return
    tk.metal_detail_map()
    base, kit, mat, _ = build_base(bake)
    if not args.get("norender"):
        previews(base, mat, out, args.get("oldbase"))
    C.save_blend("Base2")


if __name__ == "__main__":
    main()
