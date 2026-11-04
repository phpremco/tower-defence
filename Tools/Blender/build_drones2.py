"""Round 18: three more drones of the round-9 family (see ArtSource/DESIGN_NOTES.md, round 18).

- Builder   (cutscene: welds the base together while it unfolds): a tri-rotor with two welding arms under the nose,
             a signal-yellow construction beacon on top. Sockets `Weld_L`, `Weld_R` (sparks), `Beacon`.
- Hauler    (cutscene: flies Crysium from the deposits to the base in a swarm): a light tandem lifter, one big duct
             fore and aft on a long spine, a cargo pod with an amber ore window in a grip (`Drone_Hauler_Cargo`, its
             own mesh). Kept light for swarms. Socket `Cargo`.
- Bombard   (the hero's ultimate, Pathfinder Strike: a drone bombardment): a flying wing with two embedded ducts,
             twin thrusters, a bomb bay with two doors and two pairs of small bombs on `Bomb_1/2`. Sockets
             `Drop_1..4`, `Thruster_L/R`.

Same shared parts as round 9 (white hull over a blue band and a gunmetal belly, glass visor, ducted rotors with a
white lip and yellow stripe, nav pips) via build_drones.py, imported unchanged. One 512 map set (+ Low 256) each.
Out: `Exports/Drone_<Role>/Drone_<Role>.fbx` (LOD0 + LOD1, clips Idle/Fly/Work), maps, report.

Run: blender --background --factory-startup --python Tools/Blender/build_drones2.py -- [--preview <dir>] [--norender]
     [--nobake] [--only Builder,Hauler,Bombard]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import base_kit as bk  # noqa: E402
import build_drones as bd  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import FWD, UP, XAX, V  # noqa: E402

YAX = V(0, 1, 0)
SIGNAL = (1.0, 0.74, 0.2)
WELD = (0.6, 0.85, 1.0)


class Builder(bd.Drone):
    """Tri-rotor (a Y of ducts, unlike the repair quad's X) with two welding arms and a construction beacon."""
    name = "Builder"
    accent = "yellow"

    def pieces(self, k):
        on = ["Main"]
        k.lathe("Belly", [(0, -0.07), (0.09, -0.07), (0.125, -0.03), (0.13, -0.01), (0, -0.01)], tk.frame(), tk.GUNMETAL, "Body", on, sides=12)
        k.lathe("Band", [(0, -0.012), (0.135, -0.012), (0.135, 0.02), (0, 0.02)], tk.frame(), tk.BLUE.but(panel=0), "Body", on, sides=12)
        k.lathe("Shell", [(0, 0.018), (0.13, 0.018), (0.115, 0.05), (0.07, 0.072), (0, 0.078)], tk.frame(), tk.WHITE.but(panel=0, bolts=False), "Body",
                on, sides=12)
        k.torus("HazardRing", V(0, 0, 0.004), UP, 0.137, 0.01, tk.HAZARD, "Body", on, seg=16, sides=4)
        k.block("Visor", (0.1, 0.03, 0.035), tk.frame((0, -0.12, 0.025), rot=(-20, 0, 0)), tk.GLASS, "Body", on, bevel=0.005)
        k.block("VisorGlow", (0.07, 0.01, 0.01), tk.frame((0, -0.134, 0.018), rot=(-20, 0, 0)), tk.glow(SIGNAL, 1.8), "Body", on, bevel=0.0)
        # The construction beacon: a squat amber dome in a little cage.
        k.lathe("BeaconBase", [(0, 0.074), (0.03, 0.074), (0.03, 0.088), (0, 0.088)], tk.frame(), tk.DARK, "Body", on, sides=8, bevel=0.0)
        k.lathe("Beacon", [(0, 0.086), (0.024, 0.086), (0.022, 0.104), (0.012, 0.114), (0, 0.116)], tk.frame(), tk.glow(SIGNAL, 2.0, lens=0.02), "Body", on,
                sides=8, bevel=0.0)
        for i in range(3):
            a = math.radians(90 + 120 * i)          # one duct straight back, two forward-side
            d = V(math.cos(a), math.sin(a), 0)
            c = d * 0.26 + V(0, 0, 0.015)
            k.block("Arm", (0.22, 0.04, 0.03), tk.frame(d * 0.15 + V(0, 0, 0.005), rot=(0, 0, math.degrees(a))), tk.BLUE.but(panel=0), "Body", on, bevel=0.005)
            k.block("ArmStripe", (0.12, 0.042, 0.008), tk.frame(d * 0.16 + V(0, 0, 0.022), rot=(0, 0, math.degrees(a))), tk.YELLOW, "Body", on, bevel=0.0,
                    lod=False)
            name = f"Rotor_{i + 1}"
            bd.duct(k, on, c, 0.09, name, sides=12)
            self.rotors.append((name, c, UP))
            bd.nav(k, on, c + d * 0.13, "yellow")
        # Two welding arms under the nose: shoulder, a forearm angled down and forward, a torch with a glowing tip.
        self.arms = []
        for s, name in ((-1, "Arm_L"), (1, "Arm_R")):
            sh = V(s * 0.06, -0.06, -0.07)
            el = sh + V(s * 0.02, -0.07, -0.06)
            tip = el + V(-s * 0.015, -0.06, -0.05)
            k.lathe("Shoulder", [(0, -0.018), (0.024, -0.018), (0.024, 0.018), (0, 0.018)], tk.frame(sh, z=XAX), tk.GUNMETAL, name, on, sides=8, bevel=0.0)
            k.cyl("Upper", sh, el, 0.014, look=tk.DARK, bone=name, on=on, sides=6, bevel=0.0)
            k.cyl("Fore", el, tip, 0.012, 0.009, look=tk.STEEL, bone=name, on=on, sides=6, bevel=0.0)
            k.block("ForeStripe", (0.026, 0.02, 0.016), tk.frame(el + (tip - el) * 0.35, z=tip - el), tk.HAZARD, name, on, bevel=0.0, lod=False)
            k.cyl("Torch", tip, tip + (tip - el).normalized() * 0.025, 0.01, 0.005, look=tk.DARK, bone=name, on=on, sides=6, bevel=0.0)
            k.cyl("TorchTip", tip + (tip - el).normalized() * 0.025, tip + (tip - el).normalized() * 0.032, 0.006, look=tk.glow(WELD, 2.2), bone=name,
                  on=on, sides=6, bevel=0.0)
            self.arms.append((name, sh, s))
            self.extra.append((name, sh, el - sh, "Body", True))
        for i, (name, sh, s) in enumerate(self.arms):
            el = sh + V(s * 0.02, -0.07, -0.06)
            tip = el + V(-s * 0.015, -0.06, -0.05)
            d = (tip - el).normalized()
            self.extra.append((f"Weld_{'L' if s < 0 else 'R'}", tip + d * 0.035, d, name, False))
        self.extra.append(("Beacon", V(0, 0, 0.12), UP, "Body", False))

    def work(self, t, f):
        spec = self.hover(t, math.radians(6), 0.006, 4)
        for i, (name, sh, s) in enumerate(self.arms):
            ph = 2 * math.pi * (2 * t + 0.5 * i)
            spec[name] = {"rot": (XAX, math.radians(-12) + math.radians(14) * math.sin(ph) + math.radians(2) * math.sin(ph * 7))}
        return spec


class Hauler(bd.Drone):
    """A light tandem lifter for swarms: a long spine with one big duct fore and aft, a grip and a cargo pod."""
    name = "Hauler"
    accent = "amber"
    variants = ["Main", "Cargo"]

    def pieces(self, k):
        on = ["Main"]
        k.block("Belly", (0.14, 0.34, 0.05), tk.frame((0, 0, -0.03)), tk.GUNMETAL, "Body", on, taper=(0.9, 0.95), bevel=0.01)
        k.block("Hull", (0.17, 0.38, 0.06), tk.frame((0, 0, 0.015)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.85, 0.9), bevel=0.01)
        k.block("Shell", (0.13, 0.28, 0.035), tk.frame((0, 0.0, 0.058)), tk.WHITE.but(panel=0.08), "Body", on, taper=(0.8, 0.85), bevel=0.008)
        k.block("Visor", (0.1, 0.03, 0.035), tk.frame((0, -0.19, 0.02), rot=(-30, 0, 0)), tk.GLASS, "Body", on, bevel=0.005)
        k.block("VisorGlow", (0.07, 0.01, 0.01), tk.frame((0, -0.205, 0.012), rot=(-30, 0, 0)), bk.glow("amber", 1.8), "Body", on, bevel=0.0)
        k.block("Spine", (0.03, 0.22, 0.012), tk.frame((0, 0.0, 0.08)), tk.HAZARD, "Body", on, bevel=0.0, lod=False)
        for name, y in (("Rotor_1", -0.33), ("Rotor_2", 0.33)):
            c = V(0, y, 0.035)
            k.block("Boom", (0.05, 0.17, 0.03), tk.frame((0, y * 0.62, 0.025)), tk.GUNMETAL, "Body", on, bevel=0.005)
            bd.duct(k, on, c, 0.13, name, depth=0.05, sides=14)
            self.rotors.append((name, c, UP))
            bd.nav(k, on, c + V(0.16, 0, 0), "amber")
        # The grip: four short claws round the pod, on `Grip` (opens on Work to drop the pod).
        cc = V(0, 0, -0.12)
        for sx in (-1, 1):
            for sy in (-1, 1):
                p0 = V(sx * 0.06, sy * 0.1, -0.05)
                p1 = V(sx * 0.085, sy * 0.1, -0.13)
                k.block("Claw", (0.02, 0.03, (p1 - p0).length), tk.frame((p0 + p1) / 2, z=p1 - p0, y=YAX), tk.YELLOW.but(fam="metal"), "Grip", on,
                        bevel=0.003)
        oc = ["Cargo"]
        k.block("Pod", (0.13, 0.22, 0.1), tk.frame(cc), tk.WHITE.but(panel=0.07), "Body", oc, bevel=0.012)
        k.block("PodBand", (0.134, 0.06, 0.104), tk.frame(cc), tk.BLUE.but(panel=0), "Body", oc, bevel=0.004)
        for sx in (-1, 1):
            k.block("Window", (0.008, 0.12, 0.045), tk.frame(cc + V(sx * 0.066, 0, 0.0)), bk.glow("amber", 1.5), "Body", oc, bevel=0.0)
        for sy in (-1, 1):
            k.block("PodHazard", (0.12, 0.008, 0.03), tk.frame(cc + V(0, sy * 0.111, -0.02)), tk.HAZARD, "Body", oc, bevel=0.0)
        self.extra = [("Grip", V(0, 0, -0.05), -UP, "Body", True), ("Cargo", cc, YAX, "Body", False)]

    def work(self, t, f):
        # Drop: the claws open (the grip flattens out), hold, close; the drone kicks up as the load goes.
        spec = self.hover(t, 0.0, 0.004, 4)
        u = bk.ease(t * 4) if t < 0.5 else 1 - bk.ease((t - 0.5) * 4)
        spec["Grip"] = {"scale": (1.0 + 0.5 * u, 1.0, 1.0 - 0.35 * u)}
        spec["Body"] = {"loc": V(0, 0, 0.03 * u), "rot": (XAX, -0.04 * u)}
        return spec


class Bombard(bd.Drone):
    """A flying wing: a white-topped delta round a blue hull, two embedded ducts, twin thrusters at the back, a bomb
    bay with two doors (`Bay_L/R`) and two pairs of small bombs on `Bomb_1/2`."""
    name = "Bombard"
    accent = "cyan"

    def pieces(self, k):
        on = ["Main"]
        k.block("Belly", (0.16, 0.42, 0.04), tk.frame((0, 0.02, -0.035)), tk.GUNMETAL, "Body", on, taper=(0.9, 0.9), bevel=0.01)
        k.block("Hull", (0.18, 0.5, 0.06), tk.frame((0, 0.0, 0.01)), tk.BLUE.but(panel=0.08), "Body", on, taper=(0.7, 0.85), shift=(0, 0.02), bevel=0.012)
        k.block("Spine", (0.1, 0.36, 0.035), tk.frame((0, 0.05, 0.055)), tk.WHITE.but(panel=0.08), "Body", on, taper=(0.6, 0.85), bevel=0.008)
        k.block("Nose", (0.11, 0.1, 0.05), tk.frame((0, -0.28, 0.0)), tk.WHITE.but(panel=0), "Body", on, taper=(0.4, 0.5), shift=(0, -0.02), bevel=0.008)
        k.block("Visor", (0.08, 0.1, 0.03), tk.frame((0, -0.17, 0.045), rot=(-10, 0, 0)), tk.GLASS, "Body", on, taper=(0.7, 0.8), bevel=0.006)
        k.block("Eye", (0.04, 0.012, 0.012), tk.frame((0, -0.325, 0.0)), bk.glow("cyan", 2.0), "Body", on, bevel=0.0)
        for s in (-1, 1):
            wing = [(0.0, -0.18), (0.4, 0.1), (0.4, 0.18), (0.0, 0.2)]
            pts = [(s * x, y) for x, y in wing]
            if s < 0:
                pts = list(reversed(pts))
            k.plate("Wing", pts, 0.022, tk.frame((s * 0.07, 0.02, 0.005)), tk.WHITE.but(panel=0.07), "Body", on, bevel=0.004)
            k.plate("WingUnder", pts, 0.012, tk.frame((s * 0.07, 0.02, -0.012)), tk.BLUE.but(panel=0), "Body", on, bevel=0.0, lod=False)
            k.block("WingEdge", (0.44, 0.012, 0.014), tk.frame((s * 0.27, -0.025, 0.008), rot=(0, 0, s * 35)), bk.glow("cyan", 1.5), "Body", on, bevel=0.0)
            for j in range(2):
                k.block("Chevron", (0.06, 0.03, 0.006), tk.frame((s * (0.3 + j * 0.06), 0.16, 0.019), rot=(0, 0, s * 40)), tk.YELLOW, "Body", on, bevel=0.0,
                        lod=False)
            c = V(s * 0.24, 0.05, 0.005)
            name = f"Rotor_{1 if s < 0 else 2}"
            bd.duct(k, on, c, 0.085, name, depth=0.045, sides=14)
            self.rotors.append((name, c, UP))
            bd.nav(k, on, c + V(s * 0.24, 0.08, 0), "cyan" if s > 0 else "base")
            k.torus("Thruster", V(s * 0.05, 0.26, 0.015), YAX, 0.03, 0.01, tk.GUNMETAL, "Body", on, seg=12, sides=4)
            k.cyl("ThrusterGlow", V(s * 0.05, 0.255, 0.015), V(s * 0.05, 0.268, 0.015), 0.022, look=bk.glow("cyan", 2.0), bone="Body", on=on, sides=8, bevel=0.0)
            k.block("Fin", (0.012, 0.09, 0.07), tk.frame((s * 0.1, 0.2, 0.06), rot=(0, s * 20, 0)), tk.BLUE.but(panel=0), "Body", on, taper=(1, 0.5), bevel=0.003)
        # Bomb bay: a dark recess, two doors hinged along the bay's long edges, two pairs of bombs on racks.
        k.block("Bay", (0.1, 0.24, 0.02), tk.frame((0, 0.03, -0.058)), tk.DARK, "Body", on, bevel=0.0)
        self.doors = []
        for s, name in ((-1, "Bay_L"), (1, "Bay_R")):
            hinge = V(s * 0.055, 0.03, -0.062)
            k.block("Door", (0.055, 0.24, 0.008), tk.frame(hinge + V(-s * 0.0275, 0, -0.004)), tk.WHITE.but(panel=0), name, on, bevel=0.002)
            k.block("DoorStripe", (0.012, 0.22, 0.01), tk.frame(hinge + V(-s * 0.048, 0, -0.004)), tk.HAZARD, name, on, bevel=0.0, lod=False)
            self.doors.append((name, hinge, s))
        drops = []
        for b, y in (("Bomb_1", -0.03), ("Bomb_2", 0.09)):
            for s in (-1, 1):
                c = V(s * 0.025, y, -0.075)
                k.lathe("Bomb", [(0, -0.045), (0.012, -0.04), (0.018, -0.02), (0.018, 0.025), (0.008, 0.045), (0, 0.045)], tk.frame(c, z=YAX), PLAIN, b,
                        on, sides=8, bevel=0.0)
                k.cyl("BombBand", c - YAX * 0.012, c + YAX * 0.0, 0.0185, look=tk.HAZARD, bone=b, on=on, sides=8, bevel=0.0, lod=False)
                drops.append(c)
            self.extra.append((b, V(0, y, -0.065), -UP, "Body", True))
        self.extra += [(n, h, YAX, "Body", True) for n, h, s in self.doors]
        self.extra += [(f"Drop_{i + 1}", c - UP * 0.02, -UP, "Body", False) for i, c in enumerate(drops)]
        self.extra += [("Thruster_L", V(-0.05, 0.275, 0.015), YAX, "Body", False), ("Thruster_R", V(0.05, 0.275, 0.015), YAX, "Body", False)]

    def work(self, t, f):
        # One strike per loop: the doors open (0-0.15), pair 1 drops at 0.25, pair 2 at 0.4, doors close by 0.65, the
        # bombs are back (reloaded, unseen) after 0.8.
        spec = self.hover(t, math.radians(8), 0.006, 6)
        o = bk.ease(t / 0.15) if t < 0.5 else 1 - bk.ease((t - 0.5) / 0.15)
        for n, h, s in self.doors:
            spec[n] = {"rot": (YAX, s * math.radians(80) * o)}
        for b, t0 in (("Bomb_1", 0.25), ("Bomb_2", 0.4)):
            u = (t - t0) / 0.08
            if 0 <= u < 1:
                spec[b] = {"loc": V(0, 0, -0.08 * u * u), "scale": 1.0}
            elif t0 + 0.08 <= t < 0.8:
                spec[b] = {"scale": 0.0}
        return spec


PLAIN = tk.WHITE.but(panel=0, bolts=False)
SPECS = {"Builder": Builder, "Hauler": Hauler, "Bombard": Bombard}


def previews(built, out):
    stage = bk.road_stage(2.0, 40, axis="x")
    placed = bk.tower("Gun", "T1", (-1.4, 1.5, 0), 0) + bk.tower("Cannon", "T1", (1.4, 1.5, 0), 0)
    sk = bk.skitters([(-0.5, 0.2, 0), (0.6, -0.3, 0), (2.4, 0.1, 0)], yaw=math.radians(90))
    arms = {}
    # The round-9 repair drone next to them, from its maps, for the family check.
    rspec = bd.Repair()
    rkit = rspec.kit()
    rmat, _ = bk.bake_set(rkit, os.path.join(C.EXPORTS, rkit.name), 512, 256, bake=False)
    rep = bd.rig_objects(rspec, rkit, rmat, label="Cmp", lod=False)
    rep[0].location = (2.2, 0.3, bd.ALTITUDE)
    rep[0].rotation_euler.z = math.radians(-60)
    placed += rep
    for i, (name, (spec, kit, mat)) in enumerate(built.items()):
        objs = bd.rig_objects(spec, kit, mat, label="P", lod=False)
        arm = objs[0]
        arm.location = (-(i - 1) * 1.3 - 0.4, 0.3, bd.ALTITUDE)
        arm.rotation_euler.z = math.radians(-60)
        bk.rest_pose(arm)
        arms[name] = (spec, arm, objs)
        placed += objs
    bk.render_game(V(0, 0.4, 1.0), os.path.join(out, "drones2_game.png"), zoom=1.0)
    bk.render_game(V(0, 0.4, 1.2), os.path.join(out, "drones2_zoom.png"), zoom=3.0)
    bk.unstage(sk)
    for o in rep:
        o.hide_render = True
    shots = []
    for name, (spec, arm, objs) in arms.items():
        for n2, (_, a2, o2) in arms.items():
            for o in o2:
                o.hide_render = n2 != name
        target = arm.location + V(0, 0, -0.03)
        p = os.path.join(out, f"drone_{name.lower()}_close.png")
        bk.render_close(target, p, 1.6, azimuth=200, elevation=28, lens=50, light_scale=0.6)
        shots.append(p)
        arm.animation_data.action = spec.clips(arm)[2]
        rig = tk.close_lights(0.6)
        # The bombard's bay and bombs are only seen from below.
        cam = tk.closeup_camera(target, 1.6, azimuth=235, elevation=-30 if name == "Bombard" else 12, lens=50)
        strip = []
        for f in (0, 9, 15, 24, 33, 45):
            bpy.context.scene.frame_set(f)
            fp = os.path.join(out, f"_drone2_{name}_{f}.png")
            C.render(cam, fp, 400, 300, bloom=False)
            strip.append(fp)
        C.contact_sheet(strip, os.path.join(out, f"drone_{name.lower()}_work_strip.png"), len(strip), 320)
        for fp in strip:
            os.remove(fp)
        bk.remove([cam] + rig)
        bk.rest_pose(arm)
    C.contact_sheet(shots, os.path.join(out, "drones2_close_sheet.png"), 3, 640)
    bk.remove(placed + stage)


def build(spec, bake):
    kit, mat = bd.build(spec, bake=bake)
    path = os.path.join(C.EXPORTS, kit.name, kit.name + "_report.json")
    import json
    with open(path, encoding="utf-8") as f:
        report = json.load(f)
    report["sockets"] = {n: [round(c, 3) for c in h] for n, h, d, p, df in spec.extra if not df}
    tk.write_json(path, report)
    return kit, mat


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
    C.save_blend("Drones2" if only is None else "Drones2_" + "_".join(names))


if __name__ == "__main__":
    main()
