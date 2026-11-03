"""Round 9: the three drones (repair, harvest, attack), one family (see ArtSource/DESIGN_NOTES.md, round 9).

Shared parts: the white hull shell over a blue band and a gunmetal belly, a glass visor, ducted rotors (gunmetal
duct, white lip, yellow stripe, dark blades), nav-light pips. Each drone has its own layout and tool so its role
reads at play size. Each drone is its own asset (one 512 map set, one material, + Low 256), skinned to <= 8 deform
bones with non-deforming sockets, clips Idle, Fly and Work.

Out: `Exports/Drone_<Role>/Drone_<Role>.fbx` (LOD0 + LOD1, the harvest drone also `Drone_Harvest_Cargo`), maps,
`Drone_<Role>_report.json`.

Run: blender --background --factory-startup --python Tools/Blender/build_drones.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Repair,Harvest,Attack]
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
from base_kit import V, UP, XAX, FWD  # noqa: E402

ALTITUDE = 2.2   # Drone.Altitude: the drones fly this high over the field


def duct(k, on, centre, r, bone, depth=0.05, struts=2, to=None, sides=18):
    """A ducted rotor: duct ring, white lip with a yellow stripe, a hub and three blades on the rotor bone."""
    c = V(*centre)
    ring = [(r, -depth / 2), (r + 0.022, -depth / 2), (r + 0.03, depth / 2), (r + 0.004, depth / 2), (r, -depth / 2)]
    k.lathe("Duct", ring, tk.frame(c), tk.GUNMETAL, "Body", on, sides=sides, bevel=0.0)
    k.torus("DuctLip", c + V(0, 0, depth / 2), UP, r + 0.017, 0.012, tk.WHITE.but(panel=0), "Body", on, seg=sides, sides=4)
    k.torus("DuctStripe", c + V(0, 0, depth / 2 + 0.008), UP, r + 0.017, 0.005, tk.YELLOW, "Body", on, seg=sides, sides=3, lod=False)
    k.cyl("Hub", c - V(0, 0, 0.02), c + V(0, 0, 0.012), 0.022, look=tk.DARK, bone=bone, on=on, sides=8)
    for i in range(3):
        a = i * 2 * math.pi / 3
        d = V(math.cos(a), math.sin(a), 0)
        m = tk.frame(c + d * (r * 0.52), z=d, y=UP)
        k.block("Blade", (0.035, 0.006, r * 0.9), m @ tk.frame(rot=(0, 0, 18)), tk.DARK.but(rough=0.4), bone, on, taper=(0.6, 1.0), bevel=0.0)
    if to is not None:
        # Struts from the duct to the hull.
        t = V(*to)
        for dz in ((0.0,) if struts == 1 else (-0.012, 0.012)):
            k.cyl("Strut", c + (t - c).normalized() * (r + 0.02) + V(0, 0, dz), t + V(0, 0, dz), 0.012, look=tk.GUNMETAL, bone="Body", on=on,
                  sides=6, bevel=0.0)


def nav(k, on, p, colour="base"):
    k.cyl("NavPip", V(*p) - V(0, 0, 0.004), V(*p) + V(0, 0, 0.01), 0.012, look=bk.glow(colour, 1.9), bone="Body", on=on, sides=6, bevel=0.0,
          lod=False)


class Drone:
    name = ""
    accent = "cyan"
    variants = ["Main"]

    def __init__(self):
        self.extra = []      # extra bones (name, head, dir, parent, deform)
        self.rotors = []     # (bone, centre, axis)

    def kit(self):
        k = tk.Kit("Drone_" + self.name, self.variants)
        self.pieces(k)
        return k

    def bones(self):
        b = [tk.Bone("Root", (0, 0, 0), UP), tk.Bone("Body", (0, 0, 0), UP, "Root", length=0.1)]
        for name, c, axis in self.rotors:
            b.append(tk.Bone(name, c, axis, "Body", length=0.05))
        for name, head, d, parent, deform in self.extra:
            b.append(tk.Bone(name, head, d, parent, deform=deform, length=0.05))
        return b

    def hover(self, t, pitch=0.0, bob=0.015, revs=4):
        spec = {"Body": {"loc": V(0, 0, bob * math.sin(2 * math.pi * t)),
                         "rot": (XAX, pitch + 0.03 * math.sin(2 * math.pi * t + 1.0))}}
        for i, (name, c, axis) in enumerate(self.rotors):
            spec[name] = {"rot": (axis, (1 if i % 2 == 0 else -1) * 2 * math.pi * revs * t)}
        return spec

    def clips(self, arm):
        acts = [bk.clip(arm, "Idle", 60, lambda t, f: self.hover(t, 0.0, 0.015, 4), loop=True),
                bk.clip(arm, "Fly", 60, lambda t, f: self.hover(t, math.radians(10), 0.008, 6), loop=True),
                bk.clip(arm, "Work", 60, self.work, loop=True)]
        return acts

    def work(self, t, f):
        return self.hover(t)


class Repair(Drone):
    """Round quad with four small ducts, an arm under the belly ending in a downward emitter (the beam's origin)."""
    name = "Repair"
    accent = "cyan"

    def pieces(self, k):
        on = ["Main"]
        k.lathe("Belly", [(0, -0.075), (0.1, -0.075), (0.135, -0.035), (0.14, -0.01), (0, -0.01)], tk.frame(), tk.GUNMETAL, "Body", on, sides=16)
        k.lathe("Band", [(0, -0.012), (0.145, -0.012), (0.145, 0.022), (0, 0.022)], tk.frame(), tk.BLUE.but(panel=0), "Body", on, sides=16)
        k.lathe("Shell", [(0, 0.02), (0.14, 0.02), (0.125, 0.055), (0.085, 0.08), (0, 0.088)], tk.frame(), tk.WHITE.but(panel=0.06), "Body", on, sides=16)
        k.torus("StatusRing", V(0, 0, -0.012), UP, 0.146, 0.008, bk.glow(self.accent, 1.5), "Body", on, seg=20, sides=4)
        k.block("Visor", (0.12, 0.03, 0.04), tk.frame((0, -0.13, 0.03), rot=(-20, 0, 0)), tk.GLASS, "Body", on, bevel=0.006)
        k.block("VisorGlow", (0.08, 0.01, 0.01), tk.frame((0, -0.145, 0.02), rot=(-20, 0, 0)), bk.glow(self.accent, 1.8), "Body", on, bevel=0.0)
        # The mark on top: a cyan plus in a dark hexagon.
        k.plate("MarkHex", bk.hex_poly(0.055), 0.006, tk.frame((0, 0.01, 0.09)), tk.DARK, "Body", on, bevel=0.0)
        k.block("MarkBar", (0.07, 0.018, 0.006), tk.frame((0, 0.01, 0.095)), bk.glow(self.accent, 1.6), "Body", on, bevel=0.0)
        k.block("MarkBar", (0.018, 0.07, 0.006), tk.frame((0, 0.01, 0.095)), bk.glow(self.accent, 1.6), "Body", on, bevel=0.0)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            d = V(math.cos(a), math.sin(a), 0)
            c = d * 0.27 + V(0, 0, 0.02)
            k.block("Arm", (0.25, 0.04, 0.03), tk.frame(d * 0.16 + V(0, 0, 0.005), rot=(0, 0, math.degrees(a))), tk.BLUE.but(panel=0), "Body", on,
                    bevel=0.005)
            name = f"Rotor_{i + 1}"
            duct(k, on, c, 0.09, name, sides=12)
            self.rotors.append((name, c, UP))
            nav(k, on, c + d * 0.13 + V(0, 0, 0.0), "cyan" if i % 2 else "base")
        for s in (-1, 1):
            k.cyl("Skid", (s * 0.09, -0.1, -0.11), (s * 0.09, 0.1, -0.11), 0.01, look=tk.GUNMETAL, bone="Body", on=on, sides=6, bevel=0.0)
            for y in (-0.06, 0.06):
                k.cyl("SkidLeg", (s * 0.07, y, -0.07), (s * 0.09, y, -0.11), 0.008, look=tk.GUNMETAL, bone="Body", on=on, sides=6, bevel=0.0)
        # The tool: a two-part arm with an emitter dish that looks down and forward.
        k.cyl("ArmA", (0, 0, -0.07), (0, -0.07, -0.17), 0.02, look=tk.DARK, bone="ToolArm", on=on, sides=8)
        k.lathe("Elbow", [(0, -0.02), (0.026, -0.02), (0.026, 0.02), (0, 0.02)], tk.frame((0, -0.07, -0.17), z=XAX), tk.GUNMETAL, "ToolArm", on,
                sides=10)
        e = V(0, -0.1, -0.22)
        d = V(0, -0.35, -0.94).normalized()
        k.cyl("ArmB", (0, -0.07, -0.17), e, 0.016, look=tk.DARK, bone="ToolArm", on=on, sides=8)
        k.lathe("Emitter", [(0, -0.01), (0.03, -0.01), (0.055, 0.03), (0.05, 0.04), (0, 0.025)], tk.frame(e, z=d), tk.WHITE.but(panel=0), "Emitter", on,
                sides=14, bevel=0.0)
        k.lathe("Lens", [(0, 0.02), (0.04, 0.025), (0.025, 0.04), (0, 0.045)], tk.frame(e, z=d), bk.glow(self.accent, 2.0, lens=0.04), "Emitter", on,
                sides=12, bevel=0.0)
        k.torus("EmitterRing", e + d * 0.034, d, 0.05, 0.006, tk.YELLOW, "Emitter", on, seg=14, sides=3)
        self.extra = [("ToolArm", V(0, 0, -0.07), V(0, -0.07, -0.1), "Body", True), ("Emitter", e, d, "ToolArm", True),
                      ("Beam", e + d * 0.05, d, "Emitter", False)]

    def work(self, t, f):
        spec = self.hover(t, 0.0, 0.01, 4)
        spec["ToolArm"] = {"rot": (XAX, -math.radians(28) - math.radians(4) * math.sin(4 * math.pi * t))}
        spec["Emitter"] = {"rot": (V(0, -0.35, -0.94).normalized(), 2 * math.pi * t), "scale": 1.0 + 0.06 * math.sin(8 * math.pi * t)}
        return spec


class Harvest(Drone):
    """Heavy hauler: long hull on two big side ducts, a drill ringed by three claws, a cargo canister under it."""
    name = "Harvest"
    accent = "amber"
    variants = ["Main", "Cargo"]

    def pieces(self, k):
        on = ["Main"]
        k.block("Belly", (0.2, 0.42, 0.06), tk.frame((0, 0, -0.04)), tk.GUNMETAL, "Body", on, taper=(1.1, 1.06), bevel=0.012)
        k.block("Hull", (0.24, 0.46, 0.07), tk.frame((0, 0, 0.02)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.9, 0.92), bevel=0.012)
        k.block("Shell", (0.2, 0.36, 0.04), tk.frame((0, 0.03, 0.07)), tk.WHITE.but(panel=0.08), "Body", on, taper=(0.85, 0.85), bevel=0.01)
        k.block("Visor", (0.15, 0.04, 0.05), tk.frame((0, -0.22, 0.03), rot=(-30, 0, 0)), tk.GLASS, "Body", on, bevel=0.008)
        k.block("VisorGlow", (0.1, 0.01, 0.01), tk.frame((0, -0.24, 0.015), rot=(-30, 0, 0)), bk.glow(self.accent, 1.8), "Body", on, bevel=0.0)
        k.block("Beacon", (0.04, 0.04, 0.02), tk.frame((0, 0.1, 0.1)), bk.glow(self.accent, 1.8), "Body", on, bevel=0.004)
        for s in (-1, 1):
            k.block("Boom", (0.24, 0.07, 0.05), tk.frame((s * 0.22, 0, 0.02)), tk.GUNMETAL, "Body", on, bevel=0.008)
            k.block("BoomHazard", (0.2, 0.074, 0.054), tk.frame((s * 0.22, 0, 0.02)), tk.HAZARD, "Body", on, bevel=0.004)
            c = V(s * 0.4, 0, 0.03)
            name = f"Rotor_{1 if s < 0 else 2}"
            duct(k, on, c, 0.15, name, depth=0.06)
            self.rotors.append((name, c, UP))
            nav(k, on, c + V(s * 0.19, 0, 0.0), "amber")
            k.block("Exhaust", (0.03, 0.05, 0.08), tk.frame((s * 0.06, 0.2, 0.08), rot=(-20, 0, 0)), tk.STEEL, "Body", on, bevel=0.004)
        # Drill and claws under the nose.
        dp = V(0, -0.2, -0.08)
        dd = V(0, -0.5, -0.87).normalized()
        k.cyl("DrillMount", dp + V(0, 0.05, 0.05), dp, 0.04, look=tk.DARK, bone="Body", on=on, sides=10)
        k.lathe("Drill", [(0, 0), (0.045, 0), (0.04, 0.05), (0.025, 0.1), (0, 0.15)], tk.frame(dp, z=dd), tk.STEEL.but(rings=0.025), "Drill", on,
                sides=10, bevel=0.0)
        k.torus("DrillGlow", dp + dd * 0.02, dd, 0.046, 0.008, bk.glow(self.accent, 1.6), "Drill", on, seg=14, sides=4)
        k.torus("ClawRing", dp, dd, 0.06, 0.012, tk.GUNMETAL, "Body", on, seg=14, sides=4)
        for i in range(3):
            a = math.radians(90 + 120 * i)
            side = V(math.cos(a), 0, math.sin(a))
            radial = (side - dd * side.dot(dd)).normalized()
            base = dp + radial * 0.065
            tip = base + dd * 0.13 - radial * 0.03
            name = f"Claw_{i + 1}"
            k.block("Claw", (0.026, 0.016, (tip - base).length), tk.frame((base + tip) / 2, z=tip - base, y=radial), tk.YELLOW.but(fam="metal"), name, on,
                    taper=(0.6, 0.6), bevel=0.003)
            k.block("ClawHook", (0.024, 0.04, 0.016), tk.frame(tip - radial * 0.012, z=tip - base, y=radial), tk.DARK, name, on, bevel=0.002)
            self.extra.append((name, base, tip - base, "Body", True))
            self.claws = getattr(self, "claws", []) + [(name, radial.cross(dd).normalized())]
        self.extra.append(("Mine", dp + dd * 0.16, dd, "Drill", False))
        self.extra.append(("Drill", dp, dd, "Body", True))
        # The extra list must have Drill before its children: reorder.
        self.extra.sort(key=lambda e: 0 if e[0] == "Drill" else 1)
        # Cargo cradle on the body; the canister is its own mesh (variant Cargo) so Unity can light it.
        for s in (-1, 1):
            k.block("Cradle", (0.02, 0.24, 0.07), tk.frame((s * 0.09, 0.04, -0.09)), tk.GUNMETAL, "Body", on, bevel=0.004)
        oc = ["Cargo"]
        cc = V(0, 0.04, -0.125)
        k.lathe("Canister", [(0, -0.12), (0.045, -0.115), (0.062, -0.09), (0.062, 0.09), (0.045, 0.115), (0, 0.12)], tk.frame(cc, z=V(0, 1, 0)),
                tk.GLASS, "Body", oc, sides=12, bevel=0.0)
        k.cyl("Load", cc - V(0, 0.08, 0), cc + V(0, 0.08, 0), 0.045, look=bk.glow(self.accent, 1.4), bone="Body", on=oc, sides=6, bevel=0.0)
        for y in (-0.06, 0.06):
            k.torus("CanBand", cc + V(0, y, 0), V(0, 1, 0), 0.064, 0.008, tk.WHITE.but(panel=0), "Body", oc, seg=14, sides=4)
        self.extra.append(("Cargo", cc, V(0, 1, 0), "Body", False))

    def work(self, t, f):
        spec = self.hover(t, math.radians(4), 0.006, 4)
        spec["Drill"] = {"rot": (V(0, -0.5, -0.87).normalized(), 2 * math.pi * 6 * t)}
        grip = 0.5 + 0.5 * math.sin(4 * math.pi * t)
        for name, axis in self.claws:
            spec[name] = {"rot": (axis, -0.35 * grip)}
        return spec


class Attack(Drone):
    """A dart: narrow hull, swept stub wings, wingtip lift ducts, a twin gun pod under the chin, a tail thruster."""
    name = "Attack"
    accent = "azure"

    def pieces(self, k):
        on = ["Main"]
        k.block("Belly", (0.1, 0.44, 0.04), tk.frame((0, 0.0, -0.03)), tk.GUNMETAL, "Body", on, taper=(1.0, 1.0), shift=(0, 0), bevel=0.01)
        k.block("Hull", (0.13, 0.48, 0.06), tk.frame((0, 0, 0.015)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.75, 0.9), shift=(0, 0.02), bevel=0.012)
        k.block("Spine", (0.08, 0.36, 0.035), tk.frame((0, 0.04, 0.055)), tk.WHITE.but(panel=0.08), "Body", on, taper=(0.6, 0.85), bevel=0.008)
        k.block("Nose", (0.09, 0.1, 0.06), tk.frame((0, -0.27, 0.0)), tk.WHITE.but(panel=0), "Body", on, taper=(0.4, 0.5), shift=(0, -0.02), bevel=0.008)
        k.block("Visor", (0.07, 0.08, 0.03), tk.frame((0, -0.16, 0.05), rot=(-12, 0, 0)), tk.GLASS, "Body", on, taper=(0.7, 0.8), bevel=0.006)
        k.block("Eye", (0.03, 0.012, 0.012), tk.frame((0, -0.315, 0.0)), bk.glow(self.accent, 2.0), "Body", on, bevel=0.0)
        for s in (-1, 1):
            wing = [(0.0, -0.06), (0.26, 0.06), (0.26, 0.13), (0.0, 0.12)]
            pts = [(s * x, y) for x, y in wing]
            if s < 0:
                pts = list(reversed(pts))
            k.plate("Wing", pts, 0.018, tk.frame((s * 0.05, 0.02, 0.01)), tk.WHITE.but(panel=0.07), "Body", on, bevel=0.004)
            edge = tk.frame((s * 0.18, 0.0, 0.012), rot=(0, 0, s * 25))
            k.block("WingEdge", (0.18, 0.012, 0.012), edge, bk.glow(self.accent, 1.6), "Body", on, bevel=0.0)
            k.block("Chevron", (0.06, 0.03, 0.006), tk.frame((s * 0.15, 0.09, 0.021), rot=(0, 0, s * 40)), tk.YELLOW, "Body", on, bevel=0.0)
            c = V(s * 0.33, 0.1, 0.01)
            name = f"Rotor_{1 if s < 0 else 2}"
            duct(k, on, c, 0.075, name, depth=0.045)
            self.rotors.append((name, c, UP))
            nav(k, on, c + V(s * 0.1, 0, 0), "azure")
            k.block("Fin", (0.012, 0.08, 0.07), tk.frame((s * 0.04, 0.22, 0.07), rot=(0, s * 15, 0)), tk.BLUE.but(panel=0), "Body", on, taper=(1, 0.5),
                    bevel=0.003)
        # Twin gun pod under the chin.
        k.block("Pod", (0.1, 0.16, 0.05), tk.frame((0, -0.13, -0.065)), tk.DARK, "Body", on, taper=(0.9, 0.85), bevel=0.008)
        self.muzzles = []
        for i, x in enumerate((-0.028, 0.028)):
            b0 = V(x, -0.2, -0.07)
            name = f"Gun_{i + 1}"
            k.barrel("Barrel", b0, b0 + FWD * 0.13, 0.011, 0.006, tk.GUNMETAL, name, on, sides=8)
            k.torus("MuzzleRing", b0 + FWD * 0.12, FWD, 0.013, 0.004, tk.YELLOW, name, on, seg=8, sides=3)
            self.extra.append((name, b0, FWD, "Body", True))
            self.extra.append((f"Muzzle_{i + 1}", b0 + FWD * 0.135, FWD, name, False))
        k.torus("Thruster", V(0, 0.25, 0.01), V(0, 1, 0), 0.04, 0.012, tk.GUNMETAL, "Body", on, seg=14, sides=4)
        k.cyl("ThrusterGlow", (0, 0.245, 0.01), (0, 0.26, 0.01), 0.03, look=bk.glow(self.accent, 2.0), bone="Body", on=on, sides=10, bevel=0.0)
        self.extra.append(("Thruster", V(0, 0.27, 0.01), V(0, 1, 0), "Body", False))
        self.extra.sort(key=lambda e: 0 if e[0].startswith("Gun") else 1)

    def work(self, t, f):
        spec = self.hover(t, math.radians(6), 0.006, 6)
        for i in range(2):
            ph = ((f - i * 5) % 10) / 10.0
            k = max(0.0, 1.0 - ph * 2.2) if ph > 0 else 0.0
            spec[f"Gun_{i + 1}"] = {"loc": V(0, 0.02 * k, 0)}
        return spec


SPECS = {"Repair": Repair, "Harvest": Harvest, "Attack": Attack}


def rig_objects(spec, kit, mat, label="", lod=True):
    objs = bk.skinned(kit, "Main", spec.bones(), mat, f"{kit.name}{label}", lod=lod, arm_name=f"{kit.name}_Rig{label}")
    if "Cargo" in kit.variants:
        objs.append(bk.extra_mesh(kit, "Cargo", objs[0], mat, f"{kit.name}_Cargo{label}"))
    return objs


def build(spec, bake=True):
    kit = spec.kit()
    out_dir = os.path.join(C.EXPORTS, kit.name)
    mat, report = bk.bake_set(kit, out_dir, 512, 256, bake, samples=16)
    objs = rig_objects(spec, kit, mat)
    arm = objs[0]
    acts = spec.clips(arm)
    bk.export_fbx(os.path.join(out_dir, kit.name + ".fbx"), objs, animated=True)
    report.update({"fbx": kit.name + ".fbx", "tris": C.tri_count(objs[1]), "tris_lod1": C.tri_count(objs[2]),
                   "bones": [b.name for b in arm.data.bones], "deform_bones": [b.name for b in arm.data.bones if b.use_deform],
                   "clips": {a.name: int(a.frame_range[1]) for a in acts}, "altitude": ALTITUDE,
                   "rotors": {n: [round(x, 3) for x in c] for n, c, _ in spec.rotors},
                   "pivot": "drone centre (DroneView root); faces -Y in Blender (Unity +Z)"})
    if len(objs) > 3:
        report["cargo"] = {"object": objs[3].name, "tris": C.tri_count(objs[3])}
    tk.write_json(os.path.join(out_dir, kit.name + "_report.json"), report)
    C.log(kit.name, report["tris"], report["tris_lod1"], len(report["deform_bones"]))
    bk.remove(objs)
    bk.clear_actions()
    return kit, mat


def previews(built, out):
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = []
    placed += bk.tower("Gun", "T1", (-1.4, 1.5, 0), 0)
    placed += bk.tower("Cannon", "T1", (1.4, 1.5, 0), 0)
    sk = bk.skitters([(-0.5, 0.2, 0), (0.6, -0.3, 0), (2.4, 0.1, 0)], yaw=math.radians(90))
    arms = {}
    for i, (name, (spec, kit, mat)) in enumerate(built.items()):
        objs = rig_objects(spec, kit, mat, label="P", lod=False)
        arm = objs[0]
        arm.location = (-(i - 1) * 1.3, 0.3, ALTITUDE)
        arm.rotation_euler.z = math.radians(-60)
        bk.rest_pose(arm)
        arms[name] = (spec, arm, objs)
        placed += objs
    bk.render_game(V(0, 0.4, 1.0), os.path.join(out, "drones_game.png"), zoom=1.0)
    bk.render_game(V(0, 0.4, 1.2), os.path.join(out, "drones_zoom.png"), zoom=3.0)
    bk.unstage(sk)
    # Close-ups and work strips, each drone alone.
    shots = []
    for name, (spec, arm, objs) in arms.items():
        for n2, (_, a2, o2) in arms.items():
            for o in o2:
                o.hide_render = n2 != name
        target = arm.location + V(0, 0, -0.03)
        p = os.path.join(out, f"drone_{name.lower()}_close.png")
        bk.render_close(target, p, 1.5, azimuth=200, elevation=28, lens=50, light_scale=0.6)
        shots.append(p)
        arm.animation_data.action = spec.clips(arm)[2]
        rig = tk.close_lights(0.6)
        cam = tk.closeup_camera(target, 1.5, azimuth=235, elevation=18, lens=50)
        strip = []
        for f in (0, 8, 15, 23, 30):
            bpy.context.scene.frame_set(f)
            fp = os.path.join(out, f"_drone_{name}_{f}.png")
            C.render(cam, fp, 400, 300, bloom=False)
            strip.append(fp)
        C.contact_sheet(strip, os.path.join(out, f"drone_{name.lower()}_work_strip.png"), len(strip), 360)
        for fp in strip:
            os.remove(fp)
        bk.remove([cam] + rig)
        bk.rest_pose(arm)
    C.contact_sheet(shots, os.path.join(out, "drones_close_sheet.png"), 3, 640)
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
    C.save_blend("Drones")


if __name__ == "__main__":
    main()
