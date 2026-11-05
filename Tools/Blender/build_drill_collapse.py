"""Round 31: the colony drill rig breaking apart under Gravemaw (Remco 2026-11-05: "de toren die omvalt ziet er heel
dom uit, je wilt eigenlijk een echte kapotte boortoren zien").

`Cutscene_Drill_Wreck` (Exports/Cutscene_Drill_Wreck/) is now a rigged, fractured copy of `build_cutscene.Drill`:
the same pieces, cut at three heights, each part rigid on its own bone. Its bind pose IS the wreck (so a static use
of the FBX still shows the wreck), and its clip `Collapse` starts in the standing rig (string sunk, as after the
drill's Sink clip) and ends exactly in the bind pose:

- impact: the ground heaves up under the deck, Gravemaw's crystals punch through, the drill string is thrown out of
  the hole (it snaps: the kelly lands on the far side, the bit lands tip-first, half buried, its lower kelly bent);
- the lower derrick legs buckle at their knees, the mid-section drops and tips over its +X legs, the top snaps off
  at the work platform and slams onto the ground beyond the deck, braces tear off and fly;
- the motor house is crushed flat, its roof sheared off, the exhaust stack folds over.

The wreck is dead: every lit part of the rig (vents, screen, table ring, collar, beacon) is burnt out; a few dim
ember slivers smoulder in the torn ends. The thrown bit carries cutter teeth and lies tip-down, so it no longer
reads as a torpedo.

Run: blender --background --factory-startup --python Tools/Blender/build_drill_collapse.py --
     [--preview <dir>] [--norender] [--nobake]
`build_bosses29_props.py --only DrillWreck` calls this module's `build_wreck`.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_cutscene as BC  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import tower_kit as tk  # noqa: E402

R = math.radians
ROUND = 31
WRECK = "Cutscene_Drill_Wreck"
UP = Vector((0, 0, 1))

# Cut heights of the derrick (the brace levels of build_cutscene.Drill are DECK 0.34, 1.3, 2.2, 3.0, 3.7, TOP 4.3).
KNEE = 0.85            # the lower legs buckle here
CUT1 = 1.3             # the mid-section starts at the first brace level
CUT2 = 2.2             # the top snaps off here
GAP = 0.018            # torn gap at each cut
FALL_YAW = 22.0        # the fall, from +X turned towards the motor house (+Y)
MID_ANGLE = 72.0
MID_DROP = 0.32

# Timeline (30 fps).
FRAMES = 84
IMPACT = 3             # Gravemaw's crest hits from below: heave, string thrown
BUCKLE = (7, 20)       # legs fold at the knees
MID = (12, 36)         # mid-section tips
TOP = (17, 40)         # top folds down and hits the ground
TOP_HIT = TOP[1]
HOUSE = (30, 36)       # the falling mass crushes the motor house
STRING = (IMPACT + 1, 34)
BIT_LAND = 36
DEBRIS = 18
COLLAPSE_END = 60


def clamp01(x):
    return min(1.0, max(0.0, x))


def lin(a, b, f):
    return clamp01((f - a) / float(b - a))


def smooth(a, b, f):
    t = lin(a, b, f)
    return t * t * (3 - 2 * t)


def fall(a, b, f, bounce=0.06, settle=8):
    """Gravity: the angle grows with t^2, hits at b, rebounds by `bounce` and settles by b + settle."""
    if f <= b:
        t = lin(a, b, f)
        return t * t
    s = lin(b, b + settle, f)
    return 1.0 - bounce * math.sin(math.pi * s) * (1 - s)


def pop(a, b, f, over=0.12):
    """0..1 with an overshoot (heaved ground, crystals punching out)."""
    t = lin(a, b, f)
    return 1 - (1 - t) ** 3 + over * math.sin(math.pi * t) * (1 - t) * 2


def rot(axis, angle):
    return Matrix.Rotation(angle, 4, Vector(axis).normalized())


def about(point, m):
    p = Vector(point)
    return Matrix.Translation(p) @ m @ Matrix.Translation(-p)


def fall_dir():
    return Vector((math.cos(R(FALL_YAW)), math.sin(R(FALL_YAW)), 0))


def fall_axis():
    d = fall_dir()
    return UP.cross(d).normalized()     # rotating about it tips +Z towards +d


class Collapse(BC.Drill):
    name = WRECK

    # ---------------------------------------------------------------------------------------------------------
    # Pieces
    # ---------------------------------------------------------------------------------------------------------

    def kit(self):
        k = tk.Kit(self.name, self.variants)
        self.pieces(k)
        self.bone_list = [tk.Bone("Root", (0, 0, 0), UP, length=0.2)]
        self.sockets = {}
        self.group = {}
        self.rng = np.random.default_rng(3131)
        self.rework(k)
        self.ground_pieces(k)
        self.motions(k)
        self.dead_look(k)
        self.to_wreck(k)
        return k

    def leg_r(self, z):
        return 0.05 + (0.04 - 0.05) * (z - self.DECK) / (self.TOP - self.DECK)

    def corner(self, sx, sy, z):
        h = self.half(z)
        return Vector((sx * h, sy * h, z))

    def rework(self, k):
        on = ["Main"]
        drop = Matrix.Translation((0, 0, -self.TRAVEL))      # the string as it stands after the Sink clip
        for p in list(k.pieces):
            if p.name in ("Cable", "Leg", "Kelly", "BitTip"):
                k.pieces.remove(p)
                continue
            if p.bone in ("Block", "String"):
                p.matrix = drop @ p.matrix
        # Legs in four segments: lower (stays on the deck), knee to CUT1 (buckles), the mid and the top part.
        self.legs = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
        cuts = [self.DECK, KNEE, CUT1, CUT2, self.TOP]
        groups = ["Deck", None, "Mid", "Top"]
        for i, (sx, sy) in enumerate(self.legs):
            for s in range(4):
                z0 = cuts[s] + (GAP if s else 0.0)
                z1 = cuts[s + 1] - (GAP if s < 3 else 0.0)
                # A ragged cut: each end leans a little off the line.
                j0 = Vector((*self.rng.uniform(-0.012, 0.012, 2), 0)) if s else Vector()
                p = k.cyl("Leg", self.corner(sx, sy, z0) + j0, self.corner(sx, sy, z1), self.leg_r(z0), self.leg_r(z1),
                          look=tk.STEEL, bone="Root", on=on, sides=8, bevel=0.0)
                self.group[p] = groups[s] or f"Stub_{i + 1}"
        # The kelly in two: the top half is thrown clear, the lower half stays on the bit and bends.
        top_z = self.STRING_TOP - self.TRAVEL
        bit_z = self.BIT_TOP - self.TRAVEL
        mid_z = (top_z + bit_z) / 2
        p = k.block("KellyUp", (0.11, 0.11, top_z - mid_z - GAP), tk.frame((0, 0, (top_z + mid_z + GAP) / 2)),
                    tk.GUNMETAL.but(rings=0.12), "Root", on, bevel=0.01)
        self.group[p] = "Kelly"
        p = k.block("KellyLow", (0.11, 0.11, mid_z - bit_z), tk.frame((0, 0, (mid_z + bit_z) / 2)),
                    tk.GUNMETAL.but(rings=0.12), "Root", on, taper=(0.86, 0.86), bevel=0.01)
        self.group[p] = "KellyBent"
        self.kelly_joint = Vector((0, 0, bit_z + 0.06))
        # Cutter teeth round the shoulder of the bit (a drill head, not a smooth nose); a few are broken off.
        z0 = self.BIT_TIP - self.TRAVEL
        bit_len = self.BIT_TOP - 0.06 - self.BIT_TIP
        for j in range(12):
            if j in (3, 8):
                continue
            for ring, (zr, rr, sz) in enumerate(((z0 + bit_len - 0.16, 0.235, 0.085), (z0 + bit_len * 0.45, 0.17, 0.065))):
                a = j * 30 + ring * 15
                out = BC.polar(a, 1, 0)
                c = BC.polar(a, rr, zr)
                p = k.block("Tooth", (sz * 0.7, sz * 0.7, sz), tk.frame(c, z=(out - UP * 0.5).normalized()),
                            tk.DARK.but(fam="metal", wear=1.0), "Root", on, taper=(0.25, 0.25), bevel=0.0)
                self.group[p] = "Bit"
        # The snapped end of the kelly shows bright torn steel.
        for zz, g in ((mid_z + GAP, "Kelly"), (mid_z - 0.004, "KellyBent")):
            p = k.block("Tear", (0.118, 0.118, 0.018), tk.frame((0, 0, zz)), tk.STEEL.but(wear=1.0, rough=0.3), "Root", on,
                        taper=(0.8, 0.7), bevel=0.0)
            self.group[p] = g
        # Embers in the torn leg ends and in the crushed house (dim orange, the wreck's only light).
        ember = sk.glow("ember", 0.55)
        for i, (sx, sy) in enumerate(self.legs):
            c = self.corner(sx, sy, CUT1 - GAP)
            p = k.block("Ember", (0.045, 0.045, 0.012), tk.frame(c), ember, "Root", on, bevel=0.0)
            self.group[p] = f"Stub_{i + 1}"
        for x, z in ((0.42, 0.5), (0.78, 0.46)):
            p = k.block("Ember", (0.12, 0.012, 0.03), tk.frame((x, 0.285, z), rot=(0, self.rng.uniform(-25, 25), 0)), ember,
                        "Root", on, bevel=0.0)
            self.group[p] = "House"
        self.assign(k)

    def assign(self, k):
        deck = {"Deck", "DeckEdge", "DeckPlate", "Table", "TableGlow", "Bollard", "Panel", "PanelScreen", "Shoe"}
        block = {"Block", "BlockCheek", "Swivel", "SwivelGlow"}
        bit = {"Collar", "CollarGlow", "Bit", "Flute"}
        lattice = []
        for p in k.pieces:
            if p in self.group:
                continue
            if p.name in deck:
                g = "Deck"
            elif p.name in ("House", "Vent"):
                g = "House"
            elif p.name == "HouseRoof":
                g = "Roof"
            elif p.name in ("Stack", "StackCap"):
                g = "Stack"
            elif p.name in block:
                g = "Block"
            elif p.name in bit:
                g = "Bit"
            else:
                w = p.world()
                z = float(w[:, 2].mean())
                if z <= CUT1 + 0.03:
                    # The lowest bay's braces go with the leg nearest their upper end.
                    top = w[np.argmax(w[:, 2])]
                    i = min(range(4), key=lambda i: (top[0] - self.legs[i][0]) ** 2 + (top[1] - self.legs[i][1]) ** 2)
                    g = f"Stub_{i + 1}"
                elif z <= CUT2 + 0.03:
                    g = "Mid"
                else:
                    g = "Top"
                if p.name in ("Brace", "Rail"):
                    lattice.append(p)
            self.group[p] = g
        # Eight braces and rails tear off and fly.
        picks = self.rng.choice(len(lattice), 8, replace=False)
        self.debris = []
        for n, i in enumerate(sorted(picks)):
            p = lattice[int(i)]
            self.debris.append((p, self.group[p], f"Debris_{n + 1}"))
            self.group[p] = f"Debris_{n + 1}"

    def ground_pieces(self, k):
        on = ["Main"]
        # The heave: the ground Gravemaw pushes up under the deck (rises from below at the impact).
        p = sk.add(k, "Heave", lambda: sk.bm_mound(2.2, 0.36, seed=3133, rough=0.38), tk.frame((-0.15, 0.0, -0.02)), sk.MUD,
                   "Root", on)
        self.group[p] = "Heave"
        rubble = (((-1.7, -1.25, 0.24), (-2.2, 0.1, 0.17), (-1.0, -1.7, 0.15)),
                  ((-0.6, 1.75, 0.26), (-1.95, 1.55, 0.18), (0.9, 1.95, 0.14)),
                  ((1.35, -1.55, 0.18), (2.5, 1.25, 0.2), (3.9, -0.6, 0.16)))
        for g, group in enumerate(rubble):
            for i, (x, y, r) in enumerate(group):
                look = tk.DARK.but(paint=(0.11, 0.1, 0.1), fam="ceramic", rough=0.9) if (g + i) % 2 else sk.MUD
                p = sk.add(k, "Rubble", lambda r=r, s=3135 + g * 3 + i: sk.bm_mound(r, r * 0.8, seed=s, rings=3, sectors=7, rough=0.5),
                           tk.frame((x, y, -0.02), rot=(self.rng.uniform(-15, 15), self.rng.uniform(-15, 15), self.rng.uniform(0, 90))),
                           look, "Root", on)
                self.group[p] = f"Rubble_{g + 1}"
        self.crests = []
        for i, (x, y, ln, a) in enumerate(((-1.3, -1.05, 0.95, 200), (-1.0, -1.4, 0.62, 235), (-1.55, 0.45, 0.7, 160))):
            d = Vector((math.cos(R(a)) * 0.45, math.sin(R(a)) * 0.45, 1)).normalized()
            n0 = len(k.pieces)
            sk.crystal(k, "Crest", Vector((x, y, 0.05)), d, 0.09, ln, sk.CRYS, "Root", on, seed=3137 + i)
            for p in k.pieces[n0:]:
                self.group[p] = f"Crest_{i + 1}"
            self.crests.append((d, ln))

    # ---------------------------------------------------------------------------------------------------------
    # Motion: G[group](f) maps the standing rig onto frame f; the wreck is G at the last frame.
    # ---------------------------------------------------------------------------------------------------------

    def motions(self, k):
        d, ax = fall_dir(), fall_axis()
        self.pivots = {}

        def deck(f):
            u = pop(IMPACT, IMPACT + 11, f, over=0.25)
            jolt = 0.22 * math.sin(math.pi * lin(IMPACT, IMPACT + 8, f))
            m = rot((1, 0, 0), R(-3) * u) @ rot((0, 1, 0), R(7) * u)
            return Matrix.Translation((0, 0, 0.1 * u + jolt)) @ m

        def stub(i):
            sx, sy = self.legs[i]
            knee = self.corner(sx, sy, KNEE)
            radial = Vector((sx, sy, 0)).normalized()
            front = radial.dot(d) > 0
            b = (radial * 0.55 + d * (1.0 if front else 0.35)).normalized()
            angle = R(46 if front else 24) * (1 if front else 1)
            axis = UP.cross(b).normalized()

            def fn(f):
                return deck(f) @ about(knee, rot(axis, angle * fall(*BUCKLE, f, bounce=0.05)))
            self.pivots[f"Stub_{i + 1}"] = knee
            return fn

        h1 = d * self.half(CUT1) + Vector((0, 0, CUT1))
        h2 = d * self.half(CUT2) + Vector((0, 0, CUT2))

        def mid(f):
            u = fall(*MID, f, bounce=0.04)
            return deck(f) @ Matrix.Translation((0, 0, -MID_DROP * smooth(BUCKLE[0], BUCKLE[1] + 4, f))) @ \
                about(h1, rot(ax, R(MID_ANGLE) * u))

        top_pts = np.vstack([p.world() for p in k.pieces if self.group[p] == "Top"])

        def top_with(angle, f):
            return mid(f) @ about(h2, rot(ax, angle * fall(*TOP, f, bounce=0.05)))

        # The top's fold: the largest angle whose lowest point still rests on the ground.
        lo, hi = R(10), R(120)
        for _ in range(32):
            a = (lo + hi) / 2
            m = np.array(top_with(a, FRAMES))
            low = float((np.c_[top_pts, np.ones(len(top_pts))] @ m.T)[:, 2].min())
            lo, hi = (a, hi) if low > 0.03 else (lo, a)
        top_angle = lo
        self.top_angle = top_angle

        pb = Vector((0.6, 0.62, self.DECK))
        crush = (1.1, 0.94, 0.42)

        def house(f):
            u = fall(*HOUSE, f, bounce=0.03, settle=5)
            s = Matrix.Diagonal((1 + (crush[0] - 1) * u, 1 + (crush[1] - 1) * u, 1 + (crush[2] - 1) * u, 1))
            return deck(f) @ about(pb, s)

        roof_hinge = Vector((0.6 - 0.43, 0.62, 1.06))

        def roof(f):
            u = fall(*HOUSE, f, bounce=0.03, settle=5)
            down = Matrix.Translation((0.15 * u, 0.12 * u, -(1.06 - self.DECK) * (1 - crush[2]) * u))
            return deck(f) @ down @ about(roof_hinge, rot((0.3, 1, 0), R(-17) * u))

        stack_base = Vector((0.86, 0.82, 1.05))

        def stack(f):
            u = fall(HOUSE[0] - 2, HOUSE[1] + 6, f, bounce=0.08)
            sink = Matrix.Translation((0, 0, -(1.06 - self.DECK) * (1 - crush[2]) * smooth(*HOUSE, f)))
            return deck(f) @ sink @ about(stack_base, rot((-1, 0.5, 0), R(78) * u))

        G = {"Root": lambda f: Matrix.Identity(4), "Deck": deck, "Mid": mid,
             "Top": lambda f: top_with(top_angle, f), "House": house, "Roof": roof, "Stack": stack}
        for i in range(4):
            G[f"Stub_{i + 1}"] = stub(i)
        self.pivots.update({"Deck": Vector((0, 0, 0.3)), "Mid": h1, "Top": h2, "House": pb, "Roof": roof_hinge,
                            "Stack": stack_base})

        # Thrown parts: a ballistic arc of the centroid and a turn (with extra spins) to the landing orientation.
        def thrown(c0, c1, q0_of, q1, a, b, peak, spins=0.0, spin_axis=UP, start=None):
            def fn(f):
                if f <= a:
                    return start(f) if start else Matrix.Identity(4)
                t = lin(a, b, f)
                s0 = start(a) if start else Matrix.Identity(4)
                p0 = s0 @ c0
                c = p0.lerp(c1, t) + UP * (4 * peak * t * (1 - t))
                if f > b:   # a small skid and rock after landing
                    s = lin(b, b + 6, f)
                    c = c1 + UP * 0.05 * math.sin(math.pi * s) * (1 - s)
                q = q0_of(s0).slerp(q1, smooth(a, b, f) if f <= b else 1.0)
                spin = Quaternion(spin_axis, 2 * math.pi * spins * (1 - t)) if f <= b else Quaternion()
                return Matrix.Translation(c) @ (q @ spin).to_matrix().to_4x4() @ Matrix.Translation(-c0)
            return fn

        def centroid(group):
            pts = np.vstack([p.world() for p in k.pieces if self.group[p] == group])
            return Vector(pts.mean(axis=0))

        rest_q = (lambda s0: s0.to_quaternion())
        # Travelling block: tossed onto its side beside the deck.
        cb = centroid("Block")
        G["Block"] = thrown(cb, Vector((1.25, -1.35, 0.14)), rest_q,
                            (Quaternion(UP, R(35)) @ Quaternion((1, 0, 0), R(90))), IMPACT + 2, 28, 1.6, spins=0.5,
                            spin_axis=Vector((1, 0, 0)))
        # Upper kelly: flung out of the hole, lands on the far (-X) side across the rubble.
        ck = centroid("Kelly")
        G["Kelly"] = thrown(ck, Vector((-2.45, -0.55, 0.11)), rest_q,
                            Quaternion(UP, R(-58)) @ Quaternion((0, 1, 0), R(-84)), STRING[0], STRING[1] - 2, 3.0, spins=1.0,
                            spin_axis=Vector((0, 1, 0)))
        # Bit with the lower kelly: tip first into the heave, buried to the shoulder, leaning out.
        tip = Vector((0, 0, self.BIT_TIP - self.TRAVEL))
        collar = Vector((0, 0, self.BIT_TOP - self.TRAVEL))
        q_bit = Quaternion(UP, R(-30)) @ Quaternion((1, 0, 0), R(56))
        land = Vector((-1.85, 1.05, 0.0))
        # Place the bit so its tip is buried (about 0.3 deep) and the fluted head with its teeth shows.
        axis_w = q_bit @ (collar - tip).normalized()
        tip_at = land - axis_w * 0.42
        cbit = centroid("Bit")
        c1 = tip_at + q_bit @ (cbit - tip)
        G["Bit"] = thrown(cbit, c1, rest_q, q_bit, STRING[0], BIT_LAND, 3.6, spins=1.25, spin_axis=Vector((1, 0, 0)))
        joint = self.kelly_joint
        bend_axis = Vector((0, 1, 0))
        G["KellyBent"] = lambda f: G["Bit"](f) @ about(joint, rot(bend_axis, R(-34) * smooth(STRING[0] + 6, BIT_LAND, f)))
        self.pivots.update({"Block": cb, "Kelly": ck, "Bit": collar, "KellyBent": joint})

        # Ground: heave, rubble and crystals come up from below.
        G["Heave"] = lambda f: Matrix.Translation((0, 0, -0.5 * (1 - pop(IMPACT, IMPACT + 9, f, over=0.15))))
        self.pivots["Heave"] = Vector((-0.15, 0, 0))
        for g in range(3):
            a = IMPACT + 1 + g * 2
            G[f"Rubble_{g + 1}"] = (lambda a: lambda f: Matrix.Translation((0, 0, -0.45 * (1 - pop(a, a + 8, f)))))(a)
            self.pivots[f"Rubble_{g + 1}"] = centroid(f"Rubble_{g + 1}")
        for i, (dv, ln) in enumerate(self.crests):
            a = IMPACT + 1 + i
            G[f"Crest_{i + 1}"] = (lambda a, dv, ln: lambda f: Matrix.Translation(-dv * (ln + 0.1) * (1 - pop(a, a + 7, f, 0.18))))(a, dv, ln)
            self.pivots[f"Crest_{i + 1}"] = centroid(f"Crest_{i + 1}")

        # Debris: rides its part until it tears off, then flies out and lands flat.
        for n, (p, parent, g) in enumerate(self.debris):
            c0 = Vector(p.world().mean(axis=0))
            a = DEBRIS + 3 * n
            if parent.startswith("Stub"):
                a = BUCKLE[0] + 3 + n
            par = G[parent]
            axis_l = Vector(p.axes()[2]).normalized()
            ang = R(self.rng.uniform(0, 360))
            side = Vector((math.cos(ang), math.sin(ang), 0))
            yaw = Vector((-side.y, side.x, 0))
            dist = self.rng.uniform(1.9, 3.6)
            land = d * dist * 0.8 + side * dist * 0.6 + Vector((0.6, 0, 0))
            land.z = 0.03
            q1 = axis_l.rotation_difference(yaw)
            q1 = Quaternion(UP, R(self.rng.uniform(-30, 30))) @ q1
            G[g] = thrown(c0, land, rest_q, q1, a, a + 15, self.rng.uniform(0.7, 1.6), spins=self.rng.uniform(0.5, 1.5),
                          spin_axis=Vector(self.rng.normal(size=3)).normalized(), start=par)
            self.pivots[g] = c0
        self.G = G

    def dead_look(self, k):
        for p in k.pieces:
            g = self.group[p]
            if p.name == "Ember" or g.startswith(("Crest", "Heave", "Rubble")):
                continue
            if p.look.fam == "glow":
                p.look = bk.burn(p.look)
            elif p.name in ("Bit", "Flute", "KellyLow", "KellyUp", "Collar"):
                p.look = bk.scorch(p.look, 0.35)
            elif p.name in ("Deck", "DeckPlate", "House", "HouseRoof", "Crown", "Panel"):
                # The big light faces read clean and new at the game zoom: soot and dust over all of them.
                p.look = bk.scorch(p.look, self.rng.uniform(0.45, 0.6))
            elif self.rng.random() < 0.5:
                p.look = bk.scorch(p.look, self.rng.uniform(0.3, 0.7))

    def to_wreck(self, k):
        """Bind pose = the last frame: each piece moved by its group's final transform; one bone per group."""
        self.final = {g: fn(FRAMES) for g, fn in self.G.items()}
        for p in k.pieces:
            g = self.group[p]
            p.matrix = self.final[g] @ p.matrix
            p.bone = g
        bones = [tk.Bone("Root", (0, 0, 0), UP, length=0.2)]
        for g in self.G:
            if g == "Root":
                continue
            fm = self.final[g]
            head = fm @ self.pivots.get(g, Vector())
            loc, rq, sc = fm.decompose()
            bones.append(tk.Bone(g, head, (rq @ UP).normalized(), "Root", length=0.25))
        self.bone_list = bones

    # ---------------------------------------------------------------------------------------------------------
    # Clips
    # ---------------------------------------------------------------------------------------------------------

    def key_clip(self, arm, name, frames, frame_of):
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
        inv = {g: m.inverted() for g, m in self.final.items()}
        for f in range(frames + 1):
            src = frame_of(f)
            for pb in arm.pose.bones:
                g = pb.name
                rest = arm.data.bones[g].matrix_local
                basis = rest.inverted() @ self.G[g](src) @ inv[g] @ rest
                loc, q, sc = basis.decompose()
                pb.location = loc
                pb.rotation_quaternion = q
                pb.scale = sc
                pb.keyframe_insert("location", frame=f, group=g)
                pb.keyframe_insert("rotation_quaternion", frame=f, group=g)
                pb.keyframe_insert("scale", frame=f, group=g)
        return act

    def clips(self, arm):
        return [self.key_clip(arm, "Collapse", FRAMES, lambda f: f),
                self.key_clip(arm, "Standing", 1, lambda f: 0),
                self.key_clip(arm, "Wrecked", 1, lambda f: FRAMES)]


# =============================================================================================================
# Build
# =============================================================================================================

EVENTS = {"impact": IMPACT, "string_thrown": STRING[0], "legs_buckle": BUCKLE[0], "mid_tips": MID[0],
          "top_snaps": TOP[0], "braces_fly": DEBRIS, "house_crushed": HOUSE[0] + 4, "block_lands": 28,
          "kelly_lands": STRING[1] - 2, "bit_lands": BIT_LAND, "top_hits_ground": TOP_HIT,
          "collapse_end": COLLAPSE_END}


def build_wreck(bake=True):
    bk.clear_actions()
    tk.metal_detail_map()
    spec = Collapse()
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, WRECK)
    mat, report = bk.bake_set(kit, out_dir, 1024, 512, bake, samples=16)
    objs = bk.skinned(kit, "Main", spec.bones(), mat, WRECK, lod=False, arm_name=WRECK + "_Rig")
    arm = objs[0]
    acts = spec.clips(arm)
    bk.rest_pose(arm)
    bk.export_fbx(os.path.join(out_dir, WRECK + ".fbx"), objs, animated=True)
    report.update({
        "fbx": WRECK + ".fbx", "round": ROUND, "tris": C.tri_count(objs[1]), "lod1": "none (one per level, story decor)",
        "bones": [b.name for b in arm.data.bones],
        "size_m": [round(v, 3) for v in objs[1].dimensions],
        "pivot": "the rig deck's centre on the ground, as Cutscene_Drill: swap it in place",
        "bind_pose": "the wreck (a static use of the FBX shows the wreck)",
        "clips": {"Collapse": {"frames": FRAMES, "loop": False, "events": EVENTS,
                               "start": "frame 0 = Cutscene_Drill standing with its string sunk (after Sink), glow off",
                               "end": f"frame {COLLAPSE_END}.. {FRAMES} = the bind pose (the wreck), held"},
                  "Standing": {"frames": 1, "loop": False, "note": "the standing rig (Collapse frame 0)"},
                  "Wrecked": {"frames": 1, "loop": False, "note": "the wreck (bind pose)"}},
        "fall": {"cuts_m": [KNEE, CUT1, CUT2], "mid_angle_deg": MID_ANGLE,
                 "top_fold_deg": round(math.degrees(spec.top_angle), 1),
                 "direction": f"the top lies towards Blender +X turned {FALL_YAW:g} deg towards +Y (over the motor "
                              "house); the thrown string lands on -X, the side Gravemaw comes up"},
        "emission": "dead: no cyan left; dim ember slivers in the torn leg ends and the crushed house (ember orange "
                    "x0.55; keep Unity's WreckEmission 0.6)",
        "use": "ColonyRigView.Smash: show the wreck at the smash frame and play Collapse (role Death) from 0; it ends "
               "in the wreck pose and holds it. Dust at impact, sparks at top_hits_ground and house_crushed."})
    tk.write_json(os.path.join(out_dir, WRECK + "_report.json"), report)
    C.log(WRECK, report["tris"], len(report["bones"]))
    C.save_blend(WRECK)
    return spec, objs, {a.name: a for a in acts}


# =============================================================================================================
# Previews
# =============================================================================================================

STRIP = [0, 5, 10, 16, 22, 28, 36, FRAMES]


def strip(arm, act, cam, frames, path, w, h):
    tmp = []
    for i, f in enumerate(frames):
        C.set_action(arm, act, f)
        tmp.append(C.render(cam, os.path.join(os.path.dirname(path), f"_s{i}.png"), w, h, samples=24))
    C.contact_sheet(tmp, path, 4, w)
    for p in tmp:
        os.remove(p)


def previews(spec, objs, acts, out):
    arm = objs[0]
    C.preview_world()
    C.light_rig(scale=2.6)
    C.ground(color=(0.05, 0.05, 0.07))
    cam = C.game_camera(Vector((0.6, 0.0, 0.9)), 12.0)
    strip(arm, acts["Collapse"], cam, STRIP, os.path.join(out, "DrillCollapse_game.png"), 640, 440)
    cam2 = C.closeup_camera(Vector((0.6, 0.0, 1.4)), 15.0, azimuth=-8, elevation=7, lens=40)
    strip(arm, acts["Collapse"], cam2, STRIP, os.path.join(out, "DrillCollapse_side.png"), 640, 440)
    C.set_action(arm, acts["Wrecked"], 0)
    paths = []
    for i, (az, el, dist) in enumerate(((-35, 24, 11), (60, 22, 11), (160, 24, 11), (-90, 8, 12))):
        c = C.closeup_camera(Vector((0.6, 0, 0.6)), dist, azimuth=az, elevation=el, lens=40)
        paths.append(C.render(c, os.path.join(out, f"_w{i}.png"), 800, 520, samples=32))
    paths.append(C.render(cam, os.path.join(out, "_wg.png"), 800, 520, samples=32))
    c = C.closeup_camera(Vector((-1.9, 0.9, 0.4)), 3.2, azimuth=-30, elevation=24, lens=50)
    paths.append(C.render(c, os.path.join(out, "_wbit.png"), 800, 520, samples=32))
    C.contact_sheet(paths, os.path.join(out, "DrillWreck_after.png"), 2, 800)
    for p in paths:
        os.remove(p)


def main():
    args = C.cli_args()
    C.reset_scene()
    C.preview_world()
    spec, objs, acts = build_wreck(bake=not args.get("nobake"))
    if not args.get("norender"):
        previews(spec, objs, acts, C.preview_dir())


if __name__ == "__main__":
    main()
