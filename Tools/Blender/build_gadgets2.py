"""Round 11: the P2 gadgets (see ArtSource/DESIGN_NOTES.md, round 11): the Lava Ball (a thermite roller: our machine
with a molten core), the R.A.T. (Remote Assault Tank) and the carrier drone that drops it. Same workshop as round 9;
the Gadget base, the build and export come from build_gadgets.py and the ducted rotor from build_drones.py (both
imported, unchanged). Each asset has one 512 map set (+ Low 256).

Out: `Exports/Gadget_<Name>/Gadget_<Name>.fbx` (LOD0 + LOD1 skinned, clips, sockets), maps, `Gadget_<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_gadgets2.py -- [--preview <dir>] [--norender]
     [--nobake] [--only LavaBall,RAT,RATCarrier]
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
import build_drones as bd  # noqa: E402
import build_gadgets as bg  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import UP, XAX, V  # noqa: E402

LAVA = (1.0, 0.34, 0.05)
PLAIN = tk.WHITE.but(panel=0, bolts=False)
YAX = V(0, 1, 0)


def sphere_prof(r, n=6, zc=0.0):
    return [(0, zc - r)] + [(r * math.sin(math.pi * i / n), zc - r * math.cos(math.pi * i / n)) for i in range(1, n)] + [(0, zc + r)]


def _octant_patch(corners, r_out, r_in, n=4):
    """A curved shell plate covering a spherical triangle (corners: unit vectors), between radii r_in and r_out."""
    bm = bmesh.new()
    A, B, Cc = (Vector(c) for c in corners)

    def P(i, j, r):
        a, b, c = (n - i - j) / n, i / n, j / n
        return (A * a + B * b + Cc * c).normalized() * r
    outer, inner = {}, {}
    for i in range(n + 1):
        for j in range(n + 1 - i):
            outer[i, j] = bm.verts.new(P(i, j, r_out))
            inner[i, j] = bm.verts.new(P(i, j, r_in))
    tris = []
    for i in range(n):
        for j in range(n - i):
            tris.append(((i, j), (i + 1, j), (i, j + 1)))
            if i + j < n - 1:
                tris.append(((i + 1, j), (i + 1, j + 1), (i, j + 1)))
    for t in tris:
        bm.faces.new([outer[x] for x in t])
        bm.faces.new([inner[x] for x in reversed(t)])
    loop = [(i, 0) for i in range(n)] + [(n - j, j) for j in range(n)] + [(0, n - j) for j in range(n)]
    for a, b in zip(loop, loop[1:] + loop[:1]):
        bm.faces.new((outer[b], outer[a], inner[a], inner[b]))
    return bm


class LavaBall(bg.Gadget):
    """A thermite roller: eight white armour plates and three gunmetal bands round a glowing molten core."""
    name = "LavaBall"
    R = 0.27
    INSET = 0.2

    def pieces(self, k):
        on = ["Main"]
        R = self.R
        c = V(0, 0, R)
        k.lathe("Core", sphere_prof(R * 0.87, 8), tk.frame(c), tk.glow(LAVA, 1.1, lens=R * 0.87), "Ball", on, sides=16, bevel=0.0)
        for sx in (-1, 1):
            for sy in (-1, 1):
                for sz in (-1, 1):
                    corners = [V(sx, 0, 0), V(0, sy, 0), V(0, 0, sz)]
                    mid = V(sx, sy, sz).normalized()
                    inset = [(cc * (1 - self.INSET) + mid * self.INSET).normalized() for cc in corners]
                    look = PLAIN if sz > 0 else tk.BLUE.but(panel=0, bolts=False)
                    k._add("Plate", (lambda ins=inset: _octant_patch(ins, R, R - 0.028)), 0.0, tk.frame(c), look, "Ball", on)
        for ax in (XAX, YAX, UP):
            k.torus("Band", c, ax, R - 0.004, 0.017, tk.GUNMETAL, "Ball", on, seg=28, sides=4)
        for d in (XAX, -XAX, YAX, -YAX, UP, -UP):
            p = c + d * (R + 0.002)
            k.cyl("Hub", p - d * 0.02, p + d * 0.012, 0.046, 0.04, look=tk.HAZARD, bone="Ball", on=on, sides=10, bevel=0.0)
            k.cyl("HubCap", p + d * 0.012, p + d * 0.018, 0.022, look=tk.DARK, bone="Ball", on=on, sides=6, bevel=0.0, lod=False)
        self.extra = [("Ball", c, XAX, "Root", True), ("Core", c, UP, "Root", False), ("Trail", V(0, 0, 0.01), UP, "Root", False)]

    def clips(self, arm):
        R = self.R

        def drop(t, f):
            # Falls 0.6 in 8 frames, one bounce, settles by frame 20; a squash at each landing.
            if f <= 8:
                z, sq = 0.6 * (1 - (f / 8) ** 2), 1.0
            elif f <= 16:
                u = (f - 8) / 8
                z, sq = 0.09 * math.sin(math.pi * u), 1.0
            else:
                z, sq = 0.0, 1.0
            if f in (8, 9, 16):
                sq = 0.9
            return {"Ball": {"loc": V(0, 0, z), "scale": (1.04 if sq < 1 else 1.0, 1.04 if sq < 1 else 1.0, sq), "rot": (XAX, 0.6 * min(f, 20) / 20)}}

        def roll(t, f):
            return {"Ball": {"rot": (XAX, 2 * math.pi * t)}}

        def burst(t, f):
            s = 1.0 + 0.3 * math.sin(min(t * 1.6, 1.0) * math.pi / 2) if t < 0.62 else max(0.0, 1.3 * (1 - (t - 0.62) / 0.38))
            return {"Ball": {"scale": s}}
        return [bk.clip(arm, "Drop", 24, drop), bk.clip(arm, "Roll", 30, roll, loop=True), bk.clip(arm, "Burst", 12, burst)]


class RAT(bg.Gadget):
    """Remote Assault Tank: a small tracked tank with a round turret, dropped by the carrier drone."""
    name = "RAT"
    WHEELS = {"Wheel_FL": V(-0.152, -0.19, 0.068), "Wheel_FR": V(0.152, -0.19, 0.068), "Wheel_RL": V(-0.152, 0.19, 0.068),
              "Wheel_RR": V(0.152, 0.19, 0.068)}
    LUG = V(0, 0, 0.215)

    def pieces(self, k):
        on = ["Main"]
        # Tracks: a stadium belt per side (rubber, tread seams), road-wheel discs and a sprocket/idler pair on bones.
        r, half = 0.068, 0.19
        poly = []
        for i in range(7):
            a = -math.pi / 2 + math.pi * i / 6
            poly.append((half + math.cos(a) * r, r + math.sin(a) * r))
        for i in range(7):
            a = math.pi / 2 + math.pi * i / 6
            poly.append((-half + math.cos(a) * r, r + math.sin(a) * r))
        for s in (-1, 1):
            k.plate("Track", poly, 0.07, tk.frame(V(s * 0.152, 0, 0), z=XAX, y=UP), tk.RUBBER.but(panel=0.028), "Body", on, bevel=0.006)
            for y in (-0.065, 0.065):
                k.cyl("RoadWheel", V(s * 0.188, y, 0.06), V(s * 0.193, y, 0.06), 0.04, look=tk.GUNMETAL, bone="Body", on=on, sides=10, bevel=0.0)
            k.block("Skirt", (0.09, 0.42, 0.05), tk.frame(V(s * 0.155, -0.005, 0.15)), PLAIN, "Body", on, bevel=0.006)
            k.block("SkirtStripe", (0.006, 0.36, 0.018), tk.frame(V(s * 0.2, -0.005, 0.142)), tk.HAZARD, "Body", on, bevel=0.0)
            k.torus("Lug", V(s * 0.12, 0.0, 0.19), YAX, 0.02, 0.006, tk.GUNMETAL, "Body", on, seg=10, sides=4)
        for name, c in self.WHEELS.items():
            s = 1 if c.x > 0 else -1
            k.cyl("Sprocket", c + V(s * 0.03, 0, 0), c + V(s * 0.041, 0, 0), 0.056, look=tk.STEEL, bone=name, on=on, sides=12, bevel=0.002)
            k.cyl("SprocketHub", c + V(s * 0.041, 0, 0), c + V(s * 0.046, 0, 0), 0.018, look=bk.glow("cyan", 1.6), bone=name, on=on, sides=6, bevel=0.0)
            for i in range(3):
                a = 2 * math.pi * i / 3
                p = c + V(s * 0.044, math.cos(a) * 0.036, math.sin(a) * 0.036)
                k.block("Spoke", (0.006, 0.014, 0.014), tk.frame(p), tk.DARK, name, on, bevel=0.0, lod=False)
        k.block("Hull", (0.22, 0.44, 0.09), tk.frame(V(0, 0, 0.13)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.9, 0.86), bevel=0.01)
        k.block("Deck", (0.2, 0.32, 0.02), tk.frame(V(0, 0.03, 0.18)), tk.WHITE.but(panel=0.08), "Body", on, bevel=0.005)
        k.block("Glacis", (0.2, 0.02, 0.09), tk.frame(V(0, -0.21, 0.13), rot=(-50, 0, 0)), tk.WHITE.but(panel=0), "Body", on, bevel=0.005)
        k.block("Belly", (0.2, 0.4, 0.03), tk.frame(V(0, 0, 0.075)), tk.GUNMETAL, "Body", on, bevel=0.005)
        for s in (-1, 1):
            k.block("Headlight", (0.03, 0.01, 0.014), tk.frame(V(s * 0.07, -0.232, 0.142), rot=(-50, 0, 0)), bk.glow("cyan", 1.8), "Body", on, bevel=0.0)
        k.block("Engine", (0.14, 0.08, 0.03), tk.frame(V(0, 0.18, 0.19)), tk.DARK.but(panel=0.02), "Body", on, bevel=0.004)
        k.block("Tail", (0.1, 0.01, 0.014), tk.frame(V(0, 0.222, 0.15)), bk.glow("cyan", 1.4), "Body", on, bevel=0.0)
        # Turret and gun.
        tc = V(0, -0.02, 0.19)
        k.lathe("Turret", [(0, 0.19), (0.095, 0.19), (0.098, 0.215), (0.08, 0.245), (0.045, 0.258), (0, 0.26)], tk.frame(), PLAIN, "Turret", on, sides=14)
        k.lathe("TurretBand", [(0, 0.196), (0.1, 0.196), (0.1, 0.212), (0, 0.212)], tk.frame(), tk.BLUE.but(panel=0), "Turret", on, sides=14, bevel=0.002)
        k.block("Eye", (0.045, 0.01, 0.014), tk.frame(V(0, -0.088, 0.238), rot=(-30, 0, 0)), bk.glow("cyan", 2.0), "Turret", on, bevel=0.0)
        k.cyl("Antenna", (0.05, 0.05, 0.25), (0.06, 0.08, 0.36), 0.004, look=tk.GUNMETAL, bone="Turret", on=on, sides=5, bevel=0.0)
        k.cyl("AntennaTip", (0.06, 0.08, 0.36), (0.06, 0.08, 0.368), 0.01, look=bk.glow("cyan", 2.0), bone="Turret", on=on, sides=6, bevel=0.0)
        g0, gz = V(0, -0.08, 0.228), 0.228
        k.cyl("Mantlet", (-0.04, -0.08, gz), (0.04, -0.08, gz), 0.026, look=tk.GUNMETAL, bone="Gun", on=on, sides=12)
        k.barrel("Tube", g0 + V(0, -0.01, 0), V(0, -0.33, gz), 0.014, 0.008, tk.GUNMETAL, "Gun", on, sides=10)
        k.lathe("Jacket", [(0, 0), (0.022, 0), (0.022, 0.09), (0, 0.1)], tk.frame(g0 + V(0, -0.02, 0), z=V(0, -1, 0)), tk.STEEL.but(rings=0.02), "Gun", on,
                sides=10)
        k.torus("MuzzleRing", V(0, -0.315, gz), V(0, -1, 0), 0.017, 0.005, tk.YELLOW, "Gun", on, seg=10, sides=4)
        self.extra = [("Body", V(0, 0, 0.068), UP, "Root", True), ("Turret", tc, UP, "Body", True), ("Gun", g0, V(0, -1, 0), "Turret", True)]
        self.extra += [(n, c, XAX, "Body", True) for n, c in self.WHEELS.items()]
        self.extra += [("Muzzle", V(0, -0.335, gz), V(0, -1, 0), "Gun", False), ("Lug", self.LUG, UP, "Body", False)]

    def clips(self, arm):
        def drive(t, f):
            spec = {"Body": {"loc": V(0, 0, 0.003 * math.sin(4 * math.pi * t)), "rot": (XAX, math.radians(1.2) * math.sin(4 * math.pi * t))},
                    "Turret": {"rot": (UP, 0.06 * math.sin(2 * math.pi * t))}}
            for n in self.WHEELS:
                spec[n] = {"rot": (XAX, 2 * math.pi * t)}
            return spec

        def fire(t, f):
            k = bk_kick(f)
            return {"Gun": {"loc": V(0, 0.028 * k, 0)}, "Body": {"rot": (XAX, -math.radians(2.5) * k)}}

        def idle(t, f):
            return {"Turret": {"rot": (UP, 0.5 * math.sin(2 * math.pi * t))}, "Body": {"loc": V(0, 0, 0.002 * math.sin(4 * math.pi * t))}}

        def land(t, f):
            d = -0.022 * math.sin(min(1.0, t * 2.2) * math.pi) if t < 0.46 else 0.006 * math.sin((t - 0.46) / 0.54 * math.pi) * (1 - t)
            return {"Body": {"loc": V(0, 0, d)}}
        return [bk.clip(arm, "Drive", 30, drive, loop=True), bk.clip(arm, "Fire", 10, fire), bk.clip(arm, "Idle", 90, idle, loop=True),
                bk.clip(arm, "Land", 16, land)]


def bk_kick(f, n=10):
    if f <= 0:
        return 0.0
    if f == 1:
        return 1.0
    return max(0.0, 1.0 - (f - 1) / (n - 1)) ** 2


class RATCarrier(bg.Gadget):
    """The drone that drops the R.A.T.: round-9 drone family, a heavy lifter with four ducts and two hook clamps."""
    name = "RATCarrier"
    CARGO = V(0, 0, -0.2)
    DUCTS = {"Rotor_1": V(-0.3, -0.28, 0.03), "Rotor_2": V(0.3, -0.28, 0.03), "Rotor_3": V(-0.3, 0.28, 0.03), "Rotor_4": V(0.3, 0.28, 0.03)}

    def pieces(self, k):
        on = ["Main"]
        k.block("Shell", (0.26, 0.32, 0.06), tk.frame(V(0, 0, 0.05)), tk.WHITE.but(panel=0.08), "Body", on, taper=(0.8, 0.8), bevel=0.012)
        k.block("Band", (0.28, 0.34, 0.04), tk.frame(V(0, 0, 0.0)), tk.BLUE.but(panel=0.08), "Body", on, bevel=0.008)
        k.block("Belly", (0.24, 0.3, 0.05), tk.frame(V(0, 0, -0.04)), tk.GUNMETAL, "Body", on, taper=(0.85, 0.85), bevel=0.01)
        k.block("Visor", (0.14, 0.02, 0.035), tk.frame(V(0, -0.17, 0.02), rot=(-25, 0, 0)), tk.GLASS, "Body", on, bevel=0.004)
        k.block("VisorGlow", (0.08, 0.008, 0.01), tk.frame(V(0, -0.181, 0.022), rot=(-25, 0, 0)), bk.glow("azure", 1.8), "Body", on, bevel=0.0)
        k.block("Spine", (0.04, 0.24, 0.02), tk.frame(V(0, 0.01, 0.088)), tk.HAZARD, "Body", on, bevel=0.003)
        for name, c in self.DUCTS.items():
            s = V(math.copysign(0.11, c.x), math.copysign(0.13, c.y), 0.02)
            boom = c - s
            k.block("Boom", (0.04, boom.length, 0.03), tk.frame((c + s) / 2, z=UP, y=boom), tk.GUNMETAL, "Body", on, bevel=0.005)
            k.block("BoomStripe", (0.042, boom.length * 0.5, 0.008), tk.frame((c + s) / 2 + V(0, 0, 0.017), z=UP, y=boom), tk.YELLOW, "Body", on,
                    bevel=0.0, lod=False)
            bd.duct(k, on, c, 0.095, name, depth=0.05, sides=12)
            bd.nav(k, on, c + V(math.copysign(0.13, c.x), 0, 0.02), "azure")
        # Hook clamps under the belly: a hinge, an arm down to the cargo, a hook turned inward.
        self.clamps = {}
        for s, name in ((-1, "Clamp_L"), (1, "Clamp_R")):
            h = V(s * 0.1, 0, -0.06)
            k.cyl("Hinge", h - V(0, 0.05, 0), h + V(0, 0.05, 0), 0.018, look=tk.DARK, bone="Body", on=on, sides=8, bevel=0.0)
            low = V(s * 0.135, 0, -0.2)
            k.block("ClampArm", (0.026, 0.07, (low - h).length), tk.frame((h + low) / 2, z=low - h, y=YAX), tk.STEEL, name, on, bevel=0.004)
            k.block("ClampStripe", (0.028, 0.072, 0.02), tk.frame(h + (low - h) * 0.35, z=low - h, y=YAX), tk.HAZARD, name, on, bevel=0.0)
            k.block("Hook", (0.05, 0.06, 0.018), tk.frame(low + V(-s * 0.018, 0, -0.008)), tk.DARK, name, on, bevel=0.004)
            k.block("HookLight", (0.012, 0.03, 0.008), tk.frame(low + V(s * 0.01, -0.0, 0.03)), bk.glow("azure", 1.7), name, on, bevel=0.0)
            self.clamps[name] = (h, s)
        self.extra = [("Body", V(0, 0, 0), UP, "Root", True)]
        self.extra += [(n, c, UP, "Body", True) for n, c in self.DUCTS.items()]
        self.extra += [(n, h, YAX, "Body", True) for n, (h, s) in self.clamps.items()]
        self.extra.append(("Cargo", self.CARGO, UP, "Body", False))

    def hover(self, t, pitch=0.0, bob=0.012, revs=4):
        spec = {"Body": {"loc": V(0, 0, bob * math.sin(2 * math.pi * t)), "rot": (XAX, pitch + 0.025 * math.sin(2 * math.pi * t + 1.0))}}
        for i, n in enumerate(self.DUCTS):
            spec[n] = {"rot": (UP, (1 if i % 2 == 0 else -1) * 2 * math.pi * revs * t)}
        return spec

    def clips(self, arm):
        def release(t, f):
            spec = self.hover(t, 0.0, 0.0, 2)
            e = bk.ease(t * 2.5)
            for n, (h, s) in self.clamps.items():
                spec[n] = {"rot": (YAX, -s * math.radians(55) * e)}
            up = bk.ease((t - 0.25) / 0.75)
            spec["Body"] = {"loc": V(0, 0, 0.12 * up), "rot": (XAX, -0.06 * math.sin(math.pi * min(1.0, t * 1.5)))}
            return spec
        return [bk.clip(arm, "Carry", 60, lambda t, f: self.hover(t, 0.0, 0.01, 4), loop=True), bk.clip(arm, "Release", 20, release),
                bk.clip(arm, "Fly", 60, lambda t, f: self.hover(t, math.radians(10), 0.008, 6), loop=True)]


SPECS = {"LavaBall": LavaBall, "RAT": RAT, "RATCarrier": RATCarrier}
PIVOTS = {"LavaBall": "ball's contact point on the ground (the ball centre is the Ball bone at z = R)",
          "RAT": "tank centre on the ground; faces -Y in Blender (Unity +Z)",
          "RATCarrier": "drone centre (the Body bone); hang the tank so its Lug socket sits on Cargo"}


def build(spec, bake):
    kit, mat = bg.build(spec, bake=bake)
    path = os.path.join(C.EXPORTS, kit.name, kit.name + "_report.json")
    with open(path, encoding="utf-8") as f:
        report = json.load(f)
    report["pivot"] = PIVOTS[spec.name]
    report["sockets"] = {n: [round(c, 3) for c in h] for n, h, d, p, df in spec.extra if not df}
    if spec.name == "LavaBall":
        report["roll"] = {"radius": spec.R, "clip_Roll_turns": 1, "note": "drive Ball's X rotation from the distance: angle = distance / R"}
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
        fp = os.path.join(out, f"_g2_{name}_{f}.png")
        C.render(cam, fp, 400, 300, bloom=False)
        paths.append(fp)
    C.contact_sheet(paths, os.path.join(out, f"gadget_{name.lower()}_{act.name.lower()}_strip.png"), len(paths), 360)
    for fp in paths:
        os.remove(fp)
    bk.remove([cam] + rig)
    bk.rest_pose(arm)


def previews(built, out):
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    groups = {}
    if "LavaBall" in built:
        spec, kit, mat = built["LavaBall"]
        o = place(spec, kit, mat, V(-1.6, 0.15, 0), yaw=-90)
        o[0].pose.bones["Ball"].rotation_quaternion = (0.92, 0.39, 0, 0)
        groups["LavaBall"] = o
    if "RAT" in built:
        spec, kit, mat = built["RAT"]
        groups["RAT"] = place(spec, kit, mat, V(0.2, -0.35, 0), yaw=-90)
    if "RATCarrier" in built:
        spec, kit, mat = built["RATCarrier"]
        loc = V(1.9, 0.2, 1.3)
        groups["RATCarrier"] = place(spec, kit, mat, loc, yaw=-90)
        if "RAT" in built:
            rs, rk, rm = built["RAT"]
            hang = loc + V(0, 0, RATCarrier.CARGO.z - RAT.LUG.z)
            groups["RATHung"] = place(rs, rk, rm, hang, yaw=-90, label="H")
    for o in groups.values():
        placed += o
    placed += bk.tower("Gun", "T1", (-0.6, 1.6, 0))
    placed += bk.tower("Flak", "T1", (1.0, 1.6, 0))
    sk = bk.skitters([(-2.6, -0.2, 0), (-0.9, 0.35, 0), (1.3, -0.3, 0), (3.0, 0.2, 0)], yaw=math.radians(90))
    bk.render_game(V(0.2, 0.3, 0.3), os.path.join(out, "gadgets2_game.png"), zoom=1.0)
    bk.render_game(V(0.2, 0.3, 0.4), os.path.join(out, "gadgets2_zoom.png"), zoom=2.8)
    bk.unstage(sk)
    # Close-ups and clip strips, each gadget on its own.
    def solo(names):
        for n, oo in groups.items():
            for o in oo:
                o.hide_render = n not in names
        for o in placed:
            if not any(o in oo for oo in groups.values()):
                o.hide_render = True
    shots = []
    if "LavaBall" in groups:
        solo({"LavaBall"})
        arm = groups["LavaBall"][0]
        t = arm.location + V(0, 0, 0.27)
        p = os.path.join(out, "gadget_lavaball_close.png")
        bk.render_close(t, p, 1.3, azimuth=25, elevation=25, light_scale=0.5)
        shots.append(p)
        bk.render_close(t, os.path.join(out, "gadget_lavaball_macro.png"), 0.6, azimuth=40, elevation=15, lens=70, width=1280, height=720, light_scale=0.5)
        acts = built["LavaBall"][0].clips(arm)
        strip(arm, acts[0], t + V(0, 0, 0.2), out, "LavaBall", 2.0, azimuth=0, elevation=8)
        strip(arm, acts[1], t, out, "LavaBall", 1.3, azimuth=0, elevation=10)
    if "RAT" in groups:
        solo({"RAT"})
        arm = groups["RAT"][0]
        t = arm.location + V(0, 0, 0.15)
        p = os.path.join(out, "gadget_rat_close.png")
        bk.render_close(t, p, 1.2, azimuth=-35, elevation=28, light_scale=0.5)
        shots.append(p)
        bk.render_close(t + V(0.1, 0, 0.05), os.path.join(out, "gadget_rat_macro.png"), 0.55, azimuth=-60, elevation=20, lens=70, width=1280, height=720,
                        light_scale=0.5)
        acts = built["RAT"][0].clips(arm)
        strip(arm, acts[1], t, out, "RAT", 1.2, azimuth=0, elevation=12)
    if "RATCarrier" in groups:
        solo({"RATCarrier", "RATHung"})
        arm = groups["RATCarrier"][0]
        t = arm.location + V(0, 0, -0.15)
        p = os.path.join(out, "gadget_ratcarrier_close.png")
        bk.render_close(t, p, 2.0, azimuth=-30, elevation=22, light_scale=0.7)
        shots.append(p)
        acts = built["RATCarrier"][0].clips(arm)
        strip(arm, acts[1], t, out, "RATCarrier", 2.1, azimuth=0, elevation=10)
    if shots:
        C.contact_sheet(shots, os.path.join(out, "gadgets2_close_sheet.png"), len(shots), 560)
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
    C.save_blend("Gadgets2")


if __name__ == "__main__":
    main()
