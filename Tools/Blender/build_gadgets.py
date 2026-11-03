"""Round 9: gadgets and path objects (see ArtSource/DESIGN_NOTES.md, round 9): Land Mine, Chrono Field emitter,
Barrel, Air Net and Ground Drone. Same workshop as the towers: white tops, colony blue, gunmetal undersides, yellow
hazard trim, one glow per gadget. Each is its own asset with a 512 map set (+ Low 256).

Mine, Chrono and Ground Drone are skinned with clips; Barrel and Air Net are static with socket empties.
Out: `Exports/Gadget_<Name>/Gadget_<Name>.fbx`, maps, `Gadget_<Name>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_gadgets.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Mine,Chrono,Barrel,AirNet,GroundDrone]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP, XAX  # noqa: E402


class Gadget:
    name = ""
    static = False

    def __init__(self):
        self.extra = []
        self.sockets = {}

    def kit(self):
        k = tk.Kit("Gadget_" + self.name, ["Main"])
        self.pieces(k)
        return k

    def bones(self):
        return [tk.Bone("Root", (0, 0, 0), UP)] + [tk.Bone(n, h, d, p, deform=df, length=0.05) for n, h, d, p, df in self.extra]

    def clips(self, arm):
        return []


class Mine(Gadget):
    name = "Mine"

    def pieces(self, k):
        on = ["Main"]
        k.lathe("Body", [(0, 0), (0.21, 0), (0.225, 0.02), (0.205, 0.05), (0, 0.05)], tk.frame(), tk.GUNMETAL, "Root", on, sides=18)
        k.lathe("Band", [(0, 0.044), (0.196, 0.044), (0.19, 0.064), (0, 0.064)], tk.frame(), tk.BLUE.but(panel=0), "Root", on, sides=18)
        k.lathe("Hazard", [(0, 0.06), (0.185, 0.06), (0.175, 0.07), (0, 0.07)], tk.frame(), tk.HAZARD, "Root", on, sides=18, bevel=0.002)
        k.torus("ArmedRing", V(0, 0, 0.074), UP, 0.142, 0.013, bk.glow("amber", 1.15), "Ring", on, seg=20, sides=4)
        k.lathe("Cap", [(0, 0.062), (0.115, 0.062), (0.11, 0.085), (0.075, 0.104), (0, 0.11)], tk.frame(), tk.WHITE.but(panel=0, bolts=False), "Cap",
                on, sides=16)
        k.lathe("Plate", [(0, 0.108), (0.05, 0.108), (0.046, 0.116), (0, 0.118)], tk.frame(), tk.GUNMETAL, "Cap", on, sides=10, bevel=0.0)
        k.cyl("Sensor", (0, 0, 0.116), (0, 0, 0.124), 0.016, look=bk.glow("amber", 2.0), bone="Cap", on=on, sides=8, bevel=0.0)
        for i in range(6):
            a = i * math.pi / 3 + math.pi / 6
            d = V(math.cos(a), math.sin(a), 0)
            p0, p1 = d * 0.19 + V(0, 0, 0.035), d * 0.29 + V(0, 0, -0.012)
            k.block("Claw", (0.035, 0.022, (p1 - p0).length), tk.frame((p0 + p1) / 2, z=p1 - p0, y=UP), tk.DARK, "Root", on, taper=(0.5, 0.7),
                    bevel=0.003)
            k.block("ClawStripe", (0.03, 0.024, 0.02), tk.frame(p0 + (p1 - p0) * 0.25, z=p1 - p0, y=UP), tk.YELLOW, "Root", on, bevel=0.0, lod=False)
        self.extra = [("Cap", V(0, 0, 0.06), UP, "Root", True), ("Ring", V(0, 0, 0.074), UP, "Root", True),
                      ("Blast", V(0, 0, 0.1), UP, "Root", False)]

    def clips(self, arm):
        def arm_clip(t, f):
            return {"Cap": {"loc": V(0, 0, -0.03 * (1 - bk.ease(t * 1.4)))}, "Ring": {"scale": bk.pop(t, 0.5, 0.4, 0.3)}}

        def idle(t, f):
            return {"Cap": {"loc": V(0, 0, 0.002 * math.sin(2 * math.pi * t))}, "Ring": {"scale": 1.0 + 0.05 * math.sin(2 * math.pi * t)}}

        def trigger(t, f):
            return {"Cap": {"loc": V(0, 0, -0.02 * math.sin(min(1.0, t * 2) * math.pi / 2))}, "Ring": {"scale": 1.0 + 0.3 * t}}
        return [bk.clip(arm, "Arm", 24, arm_clip), bk.clip(arm, "Idle", 60, idle, loop=True), bk.clip(arm, "Trigger", 8, trigger)]


class Chrono(Gadget):
    name = "Chrono"
    TILT_A = V(0.0, -0.34, 0.94).normalized()
    TILT_B = V(0.42, 0.0, 0.91).normalized()

    def pieces(self, k):
        on = ["Main"]
        for i in range(3):
            a = math.pi / 2 + i * 2 * math.pi / 3
            d = V(math.cos(a), math.sin(a), 0)
            k.cyl("Leg", d * 0.05 + V(0, 0, 0.17), d * 0.24 + V(0, 0, 0.02), 0.018, 0.014, look=tk.GUNMETAL, bone="Root", on=on, sides=8)
            k.block("Foot", (0.08, 0.05, 0.03), tk.frame(d * 0.25 + V(0, 0, 0.015), rot=(0, 0, math.degrees(a))), tk.DARK, "Root", on, bevel=0.004)
            k.block("FootStripe", (0.082, 0.052, 0.012), tk.frame(d * 0.25 + V(0, 0, 0.026), rot=(0, 0, math.degrees(a))), tk.HAZARD, "Root", on,
                    bevel=0.002)
        k.lathe("Hub", [(0, 0.1), (0.075, 0.1), (0.08, 0.16), (0.06, 0.24), (0, 0.24)], tk.frame(), tk.GUNMETAL, "Root", on, sides=12)
        # The projector that paints the field on the road: a white flare skirt with a glowing ring under it.
        k.lathe("Projector", [(0.06, 0.2), (0.16, 0.13), (0.17, 0.12), (0.15, 0.115), (0.06, 0.17)], tk.frame(), tk.WHITE.but(panel=0, bolts=False),
                "Root", on, sides=16, bevel=0.0)
        k.torus("ProjectorGlow", V(0, 0, 0.118), UP, 0.14, 0.01, bk.glow("chrono", 1.8), "Root", on, seg=20, sides=4)
        k.cyl("Column", (0, 0, 0.24), (0, 0, 0.55), 0.038, 0.034, look=tk.BLUE.but(rings=0.06, panel=0), bone="Root", on=on, sides=12)
        k.lathe("Collar", [(0, 0.54), (0.07, 0.54), (0.075, 0.57), (0.055, 0.6), (0, 0.6)], tk.frame(), tk.WHITE.but(panel=0, bolts=False), "Root", on,
                sides=12)
        for s in (-1, 1):
            k.block("Prong", (0.03, 0.03, 0.3), tk.frame((s * 0.085, 0, 0.74), rot=(0, -s * 6, 0)), tk.WHITE.but(panel=0), "Root", on, taper=(0.6, 0.8),
                    bevel=0.005)
            k.block("ProngTip", (0.024, 0.026, 0.03), tk.frame((s * 0.1, 0, 0.9)), bk.glow("chrono", 1.4), "Root", on, bevel=0.002)
            k.block("ProngStripe", (0.032, 0.032, 0.02), tk.frame((s * 0.08, 0, 0.64), rot=(0, -s * 6, 0)), tk.YELLOW, "Root", on, bevel=0.0)
        k.lathe("Crystal", [(0, 0), (0.035, 0.04), (0.045, 0.08), (0.045, 0.2), (0.03, 0.26), (0, 0.3)], tk.frame((0, 0, 0.6), rot=(0, 0, 10)),
                bk.glow((0.55, 0.8, 1.0), 0.85, lens=0.045), "Core", on, sides=6, bevel=0.0)
        c = V(0, 0, 0.75)
        k.torus("RingA", c, self.TILT_A, 0.135, 0.011, tk.WHITE.but(panel=0), "RingA", on, seg=28, sides=5)
        for i in range(4):
            a = i * math.pi / 2
            ax = self.TILT_A
            u = ax.cross(V(1, 0, 0)).normalized()
            w = ax.cross(u)
            p = c + (u * math.cos(a) + w * math.sin(a)) * 0.135
            k.cyl("Bead", p - ax * 0.012, p + ax * 0.012, 0.016, look=bk.glow("chrono", 1.9), bone="RingA", on=on, sides=8, bevel=0.0)
        k.torus("RingB", c, self.TILT_B, 0.1, 0.009, bk.glow("chrono", 1.6), "RingB", on, seg=24, sides=4)
        self.extra = [("Core", V(0, 0, 0.6), UP, "Root", True), ("RingA", c, self.TILT_A, "Root", True), ("RingB", c, self.TILT_B, "Root", True),
                      ("Field", V(0, 0, 0.05), UP, "Root", False)]

    def clips(self, arm):
        def deploy(t, f):
            return {"Core": {"loc": V(0, 0, -0.12 * (1 - bk.ease(t * 1.5)))}, "RingA": {"scale": bk.pop(t, 0.35, 0.35)},
                    "RingB": {"scale": bk.pop(t, 0.5, 0.35)}}

        def idle(t, f):
            return {"Core": {"loc": V(0, 0, 0.01 * math.sin(2 * math.pi * t))}, "RingA": {"rot": (self.TILT_A, 2 * math.pi * t)},
                    "RingB": {"rot": (self.TILT_B, -4 * math.pi * t)}}
        return [bk.clip(arm, "Deploy", 30, deploy), bk.clip(arm, "Idle", 90, idle, loop=True)]


class Barrel(Gadget):
    name = "Barrel"
    static = True

    def pieces(self, k):
        on = ["Main"]
        k.lathe("Foot", [(0, 0), (0.21, 0), (0.21, 0.04), (0, 0.04)], tk.frame(), tk.GUNMETAL, "Root", on, sides=16)
        k.lathe("Shell", [(0, 0.04), (0.17, 0.04), (0.18, 0.06), (0.18, 0.5), (0.16, 0.53), (0, 0.53)], tk.frame(),
                tk.WHITE.but(panel=0, bolts=False, rings=0.12), "Root", on, sides=16)
        k.lathe("Band", [(0, 0.24), (0.185, 0.24), (0.185, 0.32), (0, 0.32)], tk.frame(), tk.BLUE.but(panel=0), "Root", on, sides=16)
        for z in (0.1, 0.42):
            k.lathe("Hazard", [(0, z), (0.186, z), (0.186, z + 0.05), (0, z + 0.05)], tk.frame(), tk.HAZARD, "Root", on, sides=16, bevel=0.002)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            d = V(math.cos(a), math.sin(a), 0)
            k.block("CageBar", (0.03, 0.03, 0.5), tk.frame(d * 0.195 + V(0, 0, 0.28), rot=(0, 0, math.degrees(a))), tk.GUNMETAL, "Root", on, bevel=0.004)
        k.torus("CageRing", V(0, 0, 0.53), UP, 0.19, 0.018, tk.GUNMETAL, "Root", on, seg=16, sides=4)
        for a in (0, math.pi):
            d = V(math.cos(a + 0.4), math.sin(a + 0.4), 0)
            k.block("Window", (0.06, 0.012, 0.06), tk.frame(d * 0.183 + V(0, 0, 0.28), z=d, y=UP), bk.glow("amber", 1.8), "Root", on, bevel=0.0)
        k.cyl("Valve", (0.06, 0, 0.53), (0.06, 0, 0.6), 0.03, look=tk.DARK, bone="Root", on=on, sides=8)
        k.cyl("ValveWheel", (0.06, 0, 0.6), (0.06, 0, 0.615), 0.045, look=tk.YELLOW, bone="Root", on=on, sides=10)
        k.cyl("Fuse", (-0.07, 0.02, 0.53), (-0.07, 0.02, 0.57), 0.018, look=bk.glow("amber", 2.0), bone="Root", on=on, sides=8, bevel=0.0)
        self.sockets = {"Blast": V(0, 0, 0.3), "Fuse": V(-0.07, 0.02, 0.58)}


class AirNet(Gadget):
    name = "AirNet"
    static = True

    def pieces(self, k):
        on = ["Main"]
        k.block("Sill", (0.98, 0.16, 0.05), tk.frame((0, 0, 0.025)), tk.GUNMETAL, "Root", on, taper=(1, 0.8), bevel=0.008)
        k.block("SillHazard", (0.9, 0.165, 0.02), tk.frame((0, 0, 0.03)), tk.HAZARD, "Root", on, bevel=0.002)
        socks = {}
        for s in (-1, 1):
            x = s * 0.42
            k.block("PostFoot", (0.16, 0.18, 0.1), tk.frame((x, 0, 0.05)), tk.GUNMETAL, "Root", on, bevel=0.008)
            k.cyl("Post", (x, 0, 0.1), (x, 0, 0.86), 0.04, 0.032, look=tk.BLUE.but(rings=0.08, panel=0), bone="Root", on=on, sides=10)
            k.lathe("Head", [(0, 0.86), (0.055, 0.86), (0.06, 0.9), (0.04, 0.95), (0, 0.96)], tk.frame((x, 0, 0)), tk.WHITE.but(panel=0, bolts=False),
                    "Root", on, sides=12)
            k.cyl("Beacon", (x, 0, 0.96), (x, 0, 0.985), 0.022, look=bk.glow("yellow", 2.0), bone="Root", on=on, sides=8, bevel=0.0)
            for j, z in enumerate((0.3, 0.52, 0.74)):
                k.torus("Coil", V(x, 0, z), UP, 0.05, 0.012, bk.glow("yellow", 1.1), "Root", on, seg=14, sides=4)
                k.cyl("Emitter", (x - s * 0.03, 0, z), (x - s * 0.07, 0, z), 0.018, look=tk.DARK, bone="Root", on=on, sides=8, bevel=0.0)
                socks[f"Strand_{'L' if s < 0 else 'R'}{j + 1}"] = V(x - s * 0.075, 0, z)
        self.sockets = socks


class GroundDrone(Gadget):
    name = "GroundDrone"
    WHEELS = {"Wheel_FL": V(-0.18, -0.13, 0.08), "Wheel_FR": V(0.18, -0.13, 0.08), "Wheel_RL": V(-0.18, 0.13, 0.08), "Wheel_RR": V(0.18, 0.13, 0.08)}

    def pieces(self, k):
        on = ["Main"]
        k.block("Chassis", (0.26, 0.38, 0.08), tk.frame((0, 0, 0.1)), tk.GUNMETAL, "Body", on, bevel=0.008)
        k.block("Hull", (0.28, 0.34, 0.1), tk.frame((0, 0.02, 0.18)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.85, 0.85), bevel=0.01)
        k.block("Top", (0.22, 0.26, 0.025), tk.frame((0, 0.03, 0.24)), tk.WHITE.but(panel=0.08), "Body", on, bevel=0.006)
        # The ram plough at the front (-Y): a white wedge with a hazard edge.
        k.block("Plough", (0.36, 0.03, 0.13), tk.frame((0, -0.21, 0.12), rot=(-28, 0, 0)), tk.WHITE.but(panel=0), "Body", on, taper=(0.85, 1.0), bevel=0.006)
        k.block("PloughEdge", (0.36, 0.034, 0.025), tk.frame((0, -0.24, 0.07), rot=(-28, 0, 0)), tk.HAZARD, "Body", on, bevel=0.002)
        k.block("Eye", (0.08, 0.012, 0.02), tk.frame((0, -0.16, 0.2), rot=(-20, 0, 0)), bk.glow("cyan", 2.0), "Body", on, bevel=0.0)
        k.cyl("Antenna", (0.08, 0.12, 0.25), (0.08, 0.14, 0.38), 0.006, look=tk.GUNMETAL, bone="Body", on=on, sides=6, bevel=0.0)
        k.cyl("AntennaTip", (0.08, 0.14, 0.38), (0.08, 0.14, 0.39), 0.012, look=bk.glow("cyan", 2.0), bone="Body", on=on, sides=6, bevel=0.0)
        for name, c in self.WHEELS.items():
            s = 1 if c.x > 0 else -1
            k.cyl("Tyre", c - V(s * 0.03, 0, 0), c + V(s * 0.03, 0, 0), 0.08, look=tk.RUBBER.but(rings=0.012), bone=name, on=on, sides=14)
            k.cyl("Rim", c + V(s * 0.03, 0, 0), c + V(s * 0.036, 0, 0), 0.05, look=tk.STEEL, bone=name, on=on, sides=10, bevel=0.0)
            k.cyl("RimLight", c + V(s * 0.036, 0, 0), c + V(s * 0.04, 0, 0), 0.016, look=bk.glow("cyan", 1.6), bone=name, on=on, sides=6, bevel=0.0)
        self.extra = [("Body", V(0, 0, 0.08), UP, "Root", True)] + [(n, c, XAX, "Body", True) for n, c in self.WHEELS.items()]
        self.extra.append(("Ram", V(0, -0.26, 0.1), V(0, -1, 0), "Body", False))

    def clips(self, arm):
        def drive(t, f):
            spec = {"Body": {"loc": V(0, 0, 0.004 * math.sin(4 * math.pi * t)), "rot": (XAX, math.radians(1.5) * math.sin(4 * math.pi * t))}}
            for n in self.WHEELS:
                spec[n] = {"rot": (XAX, -2 * math.pi * t)}
            return spec

        def idle(t, f):
            return {"Body": {"loc": V(0, 0, 0.003 * math.sin(2 * math.pi * t))}}
        return [bk.clip(arm, "Drive", 30, drive, loop=True), bk.clip(arm, "Idle", 60, idle, loop=True)]


SPECS = {"Mine": Mine, "Chrono": Chrono, "Barrel": Barrel, "AirNet": AirNet, "GroundDrone": GroundDrone}


def socket_empties(spec, parent=None, label=""):
    out = []
    for n, p in spec.sockets.items():
        e = C.link(bpy.data.objects.new(n + label, None))
        e.empty_display_size = 0.05
        e.location = p
        e.parent = parent
        out.append(e)
    return out


def objects(spec, kit, mat, label="", lod=True):
    if spec.static:
        o = bk.module_object(kit.of("Main"), kit.name + label, V(0, 0, 0), mat)
        objs = [o]
        if lod:
            objs.append(bk.module_object(kit.of("Main"), kit.name + "_LOD1" + label, V(0, 0, 0), mat, lod=True))
        return objs
    return bk.skinned(kit, "Main", spec.bones(), mat, kit.name + label, lod=lod, arm_name=f"{kit.name}_Rig{label}")


def build(spec, bake=True):
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, 512, 256, bake, samples=16)
    objs = objects(spec, kit, mat)
    acts = [] if spec.static else spec.clips(objs[0])
    socks = socket_empties(spec)
    bk.export_fbx(os.path.join(out_dir, kit.name + ".fbx"), objs + socks, animated=bool(acts))
    mesh0 = objs[0] if spec.static else objs[1]
    mesh1 = objs[1] if spec.static else objs[2]
    report.update({"fbx": kit.name + ".fbx", "tris": C.tri_count(mesh0), "tris_lod1": C.tri_count(mesh1),
                   "clips": {a.name: int(a.frame_range[1]) for a in acts}, "static": spec.static,
                   "sockets": {n: [round(c, 3) for c in p] for n, p in spec.sockets.items()},
                   "pivot": "centre on the ground; faces -Y in Blender (Unity +Z)"})
    if not spec.static:
        arm = objs[0]
        report["bones"] = [b.name for b in arm.data.bones]
        report["deform_bones"] = [b.name for b in arm.data.bones if b.use_deform]
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    C.log(kit.name, report["tris"], report["tris_lod1"])
    bk.remove(objs + socks)
    bk.clear_actions()
    return kit, mat


def previews(built, out):
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    layout = {"Mine": V(-1.9, 0.2, 0), "Chrono": V(-0.5, -0.1, 0), "GroundDrone": V(0.8, 0.3, 0), "Barrel": V(2.0, 1.45, 0),
              "AirNet": V(-1.2, 1.55, 0)}
    rigs = {}
    for name, (spec, kit, mat) in built.items():
        objs = objects(spec, kit, mat, label="P", lod=False)
        objs[0].location = layout.get(name, V(0, 0, 0))
        if name == "GroundDrone":
            objs[0].rotation_euler.z = math.radians(-90)
        rigs[name] = (spec, objs)
        placed += objs
    placed += bk.tower("Gun", "T1", (0.4, 1.6, 0), 0)
    placed += bk.tower("Flak", "T1", (3.2, -1.6, 0), 0)
    sk = bk.skitters([(-2.8, -0.2, 0), (0.0, 0.4, 0), (1.6, -0.3, 0)], yaw=math.radians(90))
    bk.render_game(V(0, 0.3, 0.2), os.path.join(out, "gadgets_game.png"), zoom=1.0)
    bk.render_game(V(0, 0.4, 0.2), os.path.join(out, "gadgets_zoom.png"), zoom=2.6)
    bk.unstage(sk)
    shots = []
    for name, (spec, objs) in rigs.items():
        for n2, (_, o2) in rigs.items():
            for o in o2:
                o.hide_render = n2 != name
        size = {"Mine": 1.0, "Chrono": 1.6, "Barrel": 1.4, "AirNet": 2.0, "GroundDrone": 1.2}[name]
        target = objs[0].location + V(0, 0, {"Mine": 0.05, "Chrono": 0.45, "Barrel": 0.3, "AirNet": 0.45, "GroundDrone": 0.15}[name])
        p = os.path.join(out, f"gadget_{name.lower()}_close.png")
        bk.render_close(target, p, size, azimuth=30, elevation=30, lens=50, light_scale=0.6)
        shots.append(p)
        if not spec.static:
            arm = objs[0]
            acts = spec.clips(arm)
            arm.animation_data.action = acts[0]
            rig = tk.close_lights(0.6)
            cam = tk.closeup_camera(target, size, azimuth=30, elevation=30, lens=50)
            n = int(acts[0].frame_range[1])
            strip = []
            for f in [round(n * i / 4) for i in range(5)]:
                bpy.context.scene.frame_set(f)
                fp = os.path.join(out, f"_gadget_{name}_{f}.png")
                C.render(cam, fp, 400, 300, bloom=False)
                strip.append(fp)
            C.contact_sheet(strip, os.path.join(out, f"gadget_{name.lower()}_{acts[0].name.lower()}_strip.png"), len(strip), 360)
            for fp in strip:
                os.remove(fp)
            bk.remove([cam] + rig)
            bk.rest_pose(arm)
    C.contact_sheet(shots, os.path.join(out, "gadgets_close_sheet.png"), len(shots), 480)
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
    C.save_blend("Gadgets")


if __name__ == "__main__":
    main()
