"""Round 36: the landing capsule's deorbit booster for the opening cutscene's orbit shot (playtest note 6, 2026-11-08).

The capsule leaves the mothership's bay with this kick stage on its apex. The stage burns it down towards the planet,
then blows its separation clamps and tumbles away while the capsule goes on alone into the air. Built in the same
round-13 hard-surface kit as the capsule (tower_kit pieces, one atlas, self-bake, four maps + Low set):

- an interstage skirt that hides the capsule's chute and antenna, ringed by four separation clamps (bones Clamp_n:
  clip Deploy throws them out tumbling, the debris of the separation),
- a ribbed octagonal tank section with stringers, a hazard band, conduit raceways, two pressurant spheres, four
  RCS quads with their own small bells, a lit status strip and two strobes (sockets Light_1, Light_2),
- a thrust structure with struts, a turbopump and a gimballed engine bell with regenerative cooling bands, heat
  tint at the throat and a glowing throat (socket Nozzle_1 at the exit, +Z along the plume).

Pivot: the mount plane on the capsule's shoulder (capsule z 1.40); +Z points away from the capsule (Unity +Y).

Out: `ArtSource/Exports/Cutscene_Booster/` (FBX, maps, Low maps, report).

Run: blender --background --factory-startup --threads 6 --python Tools/Blender/build_booster36.py -- [--preview <dir>]
     [--norender] [--nobake]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import base_kit as bk  # noqa: E402
import build_cutscene as bc  # noqa: E402
import common as C  # noqa: E402
import story_kit as sk  # noqa: E402
import tower_kit as tk  # noqa: E402
from base_kit import V, UP  # noqa: E402

polar = bc.polar
seg = bc.seg

BODY_R = 0.42
SKIRT_R0, SKIRT_R1 = 0.5, 0.43
SKIRT_TOP = 0.5
TANK_TOP = 1.36
THROAT_Z = 1.52
EXIT_Z = 2.08
EXIT_R = 0.34
CLAMPS = (45, 135, 225, 315)

HULL = tk.WHITE.but(panel=0.11)
DARK_HULL = tk.GUNMETAL.but(panel=0.14)
BELL = tk.STEEL.but(rings=0.05, heat=1.0, cyl=0.3, rough=0.3)


class Booster(bc.Spec):
    name = "Cutscene_Booster"
    tex = 1024
    low = 512

    def pieces(self, k):
        on = ["Main"]
        self.skirt(k, on)
        self.tank(k, on)
        self.engine(k, on)

    # The interstage: a short cone that sits over the capsule's shoulder, with the four clamps on its foot.
    def skirt(self, k, on):
        k.lathe("Skirt", [(SKIRT_R0 - 0.03, 0.0), (SKIRT_R0, 0.0), (SKIRT_R0, 0.05), (SKIRT_R1, SKIRT_TOP), (SKIRT_R1 - 0.03, SKIRT_TOP)],
                tk.frame(), DARK_HULL, "Root", on, sides=8, start=math.pi / 8, bevel=0.006)
        k.torus("SkirtLip", V(0, 0, 0.03), UP, SKIRT_R0 + 0.004, 0.018, tk.HAZARD, "Root", on, seg=32, sides=4)
        k.torus("SkirtRing", V(0, 0, SKIRT_TOP - 0.02), UP, SKIRT_R1 + 0.005, 0.016, tk.STEEL, "Root", on, seg=32, sides=4)
        for i in range(8):
            a = i * 45 + 22.5
            k.cyl("SkirtRib", polar(a, SKIRT_R0 - 0.005, 0.06), polar(a, SKIRT_R1 + 0.005, SKIRT_TOP - 0.04), 0.012, look=tk.STEEL, bone="Root", on=on,
                  sides=6, bevel=0.0)
        for i, a in enumerate(CLAMPS):
            bone = f"Clamp_{i + 1}"
            foot = polar(a, SKIRT_R0 + 0.035, 0.07)
            out = polar(a, 1, 0)
            f = tk.frame(foot, z=out, y=UP)
            k.block("Clamp", (0.16, 0.12, 0.06), f, tk.YELLOW, bone, on, bevel=0.01)
            k.block("ClampBolt", (0.05, 0.05, 0.03), f @ tk.frame((0, 0.02, 0.04)), tk.GUNMETAL, bone, on, bevel=0.006)
            k.block("ClampCharge", (0.12, 0.025, 0.02), f @ tk.frame((0, -0.045, 0.035)), sk.glow("ember", 1.2), bone, on, bevel=0.0)
            self.bone_list.append(tk.Bone(bone, foot, out, "Root", length=0.08))

    # The tank section: octagonal, ribbed, with its plumbing, thrusters and lights on the outside.
    def tank(self, k, on):
        k.lathe("Tank", [(0, SKIRT_TOP), (BODY_R, SKIRT_TOP), (BODY_R, TANK_TOP), (0, TANK_TOP)], tk.frame(), HULL, "Root", on, sides=8,
                start=math.pi / 8, bevel=0.008)
        for z in (0.68, 0.9, 1.12):
            k.lathe("Stiffener", [(0, z - 0.012), (BODY_R + 0.014, z - 0.012), (BODY_R + 0.014, z + 0.012), (0, z + 0.012)], tk.frame(), tk.STEEL, "Root", on,
                    sides=8, start=math.pi / 8, bevel=0.003)
        k.lathe("Band", [(0, 0.76), (BODY_R + 0.012, 0.76), (BODY_R + 0.012, 0.84), (0, 0.84)], tk.frame(), tk.HAZARD, "Root", on, sides=8,
                start=math.pi / 8, bevel=0.0)
        k.lathe("BlueBand", [(0, 1.18), (BODY_R + 0.01, 1.18), (BODY_R + 0.01, 1.3), (0, 1.3)], tk.frame(), tk.BLUE.but(panel=0.08), "Root", on,
                sides=8, start=math.pi / 8, bevel=0.004)
        # Stringers down the middle of every face.
        for i in range(8):
            a = i * 45
            k.block("Stringer", (0.03, TANK_TOP - SKIRT_TOP - 0.1, 0.02), tk.frame(polar(a, BODY_R * 0.93 + 0.01, (SKIRT_TOP + TANK_TOP) / 2),
                    z=polar(a, 1, 0), y=UP), tk.STEEL, "Root", on, bevel=0.003)
        # Conduit raceways: two pipes and a cable tray down one side.
        for da, r in ((-9, 0.018), (9, 0.014)):
            k.cyl("Conduit", polar(200 + da, BODY_R + 0.03, SKIRT_TOP + 0.04), polar(200 + da, BODY_R + 0.03, TANK_TOP - 0.02), r, look=tk.GUNMETAL,
                  bone="Root", on=on, sides=8, bevel=0.0)
        k.block("CableTray", (0.07, TANK_TOP - SKIRT_TOP - 0.08, 0.025), tk.frame(polar(20, BODY_R + 0.015, (SKIRT_TOP + TANK_TOP) / 2), z=polar(20, 1, 0),
                y=UP), DARK_HULL, "Root", on, bevel=0.004)
        for z in (0.62, 0.95, 1.25):
            k.block("TrayClip", (0.09, 0.035, 0.025), tk.frame(polar(20, BODY_R + 0.025, z), z=polar(20, 1, 0), y=UP), tk.STEEL, "Root", on, bevel=0.003)
        # Pressurant spheres in cradles on two faces.
        for a in (110, 290):
            c = polar(a, BODY_R + 0.07, 1.0)
            k.lathe("Sphere", [(0, -0.09), (0.06, -0.075), (0.09, 0.0), (0.06, 0.075), (0, 0.09)], tk.frame(c), tk.WHITE.but(panel=0), "Root", on,
                    sides=12, bevel=0.0)
            k.torus("Cradle", c, polar(a, 1, 0), 0.08, 0.01, tk.GUNMETAL, "Root", on, seg=16, sides=4)
        # RCS quads: a block with four small bells pointing round the stage.
        for i, a in enumerate((65, 155, 245, 335)):
            c = polar(a, BODY_R + 0.035, 1.24)
            out = polar(a, 1, 0)
            f = tk.frame(c, z=out, y=UP)
            k.block("RcsBlock", (0.09, 0.09, 0.06), f, tk.GUNMETAL, "Root", on, bevel=0.008)
            for d in (V(1, 0, 0), V(-1, 0, 0), V(0, 1, 0), V(0, -1, 0)):
                m = f @ tk.frame((d.x * 0.05, d.y * 0.05, 0.01), z=d)
                k.lathe("RcsBell", [(0.006, 0.0), (0.014, 0.01), (0.022, 0.035), (0.016, 0.036), (0, 0.01)], m, tk.STEEL, "Root", on, sides=8,
                        bevel=0.0)
        # Status strip and the two strobes.
        f = tk.frame(polar(270, BODY_R * 0.93 + 0.006, 1.02), z=polar(270, 1, 0), y=UP)
        k.block("StatusPanel", (0.14, 0.22, 0.012), f, tk.DARK.but(panel=0), "Root", on, bevel=0.003)
        for j in range(4):
            k.block("StatusLight", (0.1, 0.018, 0.008), f @ tk.frame((0, -0.07 + j * 0.045, 0.008)), sk.glow("cyan" if j else "ember", 1.8), "Root", on,
                    bevel=0.0)
        sk.emblem(k, tk.frame(polar(90, BODY_R * 0.93 + 0.008, 1.0), z=polar(90, 1, 0), y=UP), "Root", on, scale=0.3, letters=False, prefix="Mark")
        for i, a in enumerate((0, 180)):
            p = polar(a, BODY_R + 0.03, TANK_TOP - 0.01)
            k.lathe("StrobeBase", [(0, -0.01), (0.028, -0.01), (0.028, 0.01), (0, 0.01)], tk.frame(p, z=polar(a, 1, 0) + UP * 0.6), tk.GUNMETAL, "Root",
                    on, sides=8, bevel=0.0)
            k.lathe("Strobe", [(0, 0.0), (0.018, 0.005), (0.014, 0.025), (0, 0.03)], tk.frame(p, z=polar(a, 1, 0) + UP * 0.6), sk.glow("ora_hot", 2.2),
                    "Root", on, sides=8, bevel=0.0)
            self.sockets[f"Light_{i + 1}"] = p + (polar(a, 1, 0) + UP * 0.6).normalized() * 0.03

    # The thrust structure, the turbopump and the bell.
    def engine(self, k, on):
        k.lathe("ThrustPlate", [(0, TANK_TOP), (BODY_R - 0.02, TANK_TOP), (0.3, TANK_TOP + 0.08), (0, TANK_TOP + 0.08)], tk.frame(), DARK_HULL, "Root",
                on, sides=8, start=math.pi / 8, bevel=0.006)
        for i in range(6):
            a = i * 60 + 30
            k.cyl("Strut", polar(a, 0.28, TANK_TOP + 0.06), polar(a + 25, 0.12, THROAT_Z - 0.02), 0.012, look=tk.STEEL, bone="Root", on=on, sides=6,
                  bevel=0.0)
        k.lathe("Chamber", [(0, TANK_TOP + 0.06), (0.11, TANK_TOP + 0.06), (0.12, THROAT_Z - 0.04), (0.09, THROAT_Z), (0, THROAT_Z)], tk.frame(),
                tk.GUNMETAL, "Root", on, sides=16, bevel=0.004)
        k.torus("Gimbal", V(0, 0, TANK_TOP + 0.1), UP, 0.13, 0.016, tk.YELLOW, "Root", on, seg=20, sides=4)
        pump = polar(35, 0.19, TANK_TOP + 0.13)
        k.cyl("Turbopump", pump + V(0, 0, -0.05), pump + V(0, 0, 0.06), 0.05, 0.045, look=tk.GUNMETAL, bone="Root", on=on, sides=12)
        k.cyl("PumpDuct", pump + V(0, 0, 0.04), polar(35, 0.09, THROAT_Z - 0.03), 0.018, look=tk.STEEL, bone="Root", on=on, sides=8, bevel=0.0)
        k.cyl("Exhaust", pump + V(0, 0, 0.05), pump + V(0.02, 0.02, 0.2), 0.022, 0.03, look=tk.STEEL.but(heat=1.0), bone="Root", on=on, sides=8,
              bevel=0.0)
        # The bell, its cooling bands and the hot throat inside.
        bell = []
        for j in range(9):
            u = j / 8
            bell.append((0.09 + (EXIT_R - 0.09) * (u ** 0.75), THROAT_Z + (EXIT_Z - THROAT_Z) * u))
        outer = bell + [(EXIT_R - 0.012, EXIT_Z), (0.085, THROAT_Z + 0.01)]
        k.lathe("Bell", outer, tk.frame(), BELL, "Root", on, sides=24, bevel=0.0)
        for u in (0.3, 0.6, 0.92):
            z = THROAT_Z + (EXIT_Z - THROAT_Z) * u
            r = 0.09 + (EXIT_R - 0.09) * (u ** 0.75)
            k.torus("BellBand", V(0, 0, z), UP, r + 0.004, 0.008, tk.GUNMETAL, "Root", on, seg=24, sides=4)
        k.lathe("BellGlow", [(0, THROAT_Z + 0.02), (0.08, THROAT_Z + 0.03), (0.16, THROAT_Z + 0.2), (0, THROAT_Z + 0.2)], tk.frame(), sk.glow("engine", 2.4),
                "Root", on, sides=16, bevel=0.0)
        self.sockets["Nozzle_1"] = (V(0, 0, EXIT_Z), UP)
        self.sockets["Mount"] = (V(0, 0, 0), -UP)

    def clips(self, arm):
        # The separation: each clamp is blown off its foot, out and back, tumbling.
        def deploy(t, f):
            spec = {}
            u = seg(t, 0.0, 1.0)
            for i, a in enumerate(CLAMPS):
                out = polar(a, 1, 0)
                spin_axis = out.cross(UP).normalized()
                travel = 2.6 * u * (1.0 + 0.15 * i)
                spec[f"Clamp_{i + 1}"] = {"loc": tuple(out * travel + UP * (-0.5 * u)), "rot": (spin_axis, u * (5.0 + i * 1.3))}
            return spec
        return [bk.clip(arm, "Deploy", 30, deploy)]


def render_booster(spec, kit, mat, out):
    objs = bc.rig_objects(spec, kit, mat, "P")
    arm = objs[0]
    acts = {a.name: a for a in spec.clips(arm)}
    bk.rest_pose(arm)
    tk.close_lights(1.2)
    cam = C.closeup_camera(V(0, 0, 1.0), 6.5, azimuth=-35, elevation=12, lens=45)
    C.render(cam, os.path.join(out, "booster_side.png"), 1280, 720)
    bk.remove([cam])
    cam = C.closeup_camera(V(0, 0, 1.4), 5.0, azimuth=20, elevation=-35, lens=45)
    C.render(cam, os.path.join(out, "booster_bell.png"), 960, 960)
    bk.remove([cam])
    arm.animation_data.action = acts["Deploy"]
    bpy.context.scene.frame_set(8)
    cam = C.closeup_camera(V(0, 0, 0.6), 9.0, azimuth=-35, elevation=20, lens=40)
    C.render(cam, os.path.join(out, "booster_separation.png"), 1280, 720)
    bk.remove([cam])


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    C.preview_world()
    tk.metal_detail_map()
    spec = Booster()
    kit, mat, _ = bc.build(spec, bake=not args.get("nobake"))
    if not args.get("norender"):
        spec2 = Booster()
        spec2.kit()
        render_booster(spec2, kit, mat, out)
        bk.clear_actions()
    C.save_blend("Cutscene_Booster")


if __name__ == "__main__":
    main()
