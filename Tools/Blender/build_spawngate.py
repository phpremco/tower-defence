"""Round 25: the enemy spawn gates (where the Burrowers come from), one for ground routes and one for air routes.

Replaces StructureModelFactory.EnemyGate (fleshy mound + spinning ring built from code primitives) and the spinner
ring in LevelVisualBuilder.PlaceStructure. Alien and warm (blood red, ember orange, a sickly green in the eyes), so
it never reads as our cool white-blue tech gate.

SpawnGate_Ground: a crater mound of layered flesh and chitin round a glowing throat, six fleshy lips that peel open
  when a wave comes out, five hooked spines of unequal length leaning over it (asymmetric on purpose) and a
  vertebra ring with claws that hovers over the throat and turns.
SpawnGate_Air: a floating hive pod (teardrop) with the throat on top, five lips, a tilted claw ring round its waist
  and four tendrils that dangle and sway.

Rig (Generic, root `Root`): Body, Ring, Lip_n, Spine_n (ground) / Tendril_n_0..1 (air), plus non-deforming
sockets Socket_Emerge (where a creature appears, +Z up), Socket_Spores (spore emitter), Socket_Light.
Clips: Idle (loop, 360 f: breathing, lip twitch, spine/tendril sway and one full ring turn = 30 deg/s like
GateRingSpin), Spin (loop, 360 f: only the ring), Spawn (40 f: lips peel open, throat pushes up, ring lurches).

Out: Exports/SpawnGate_<Kind>/SpawnGate_<Kind>.fbx + 512 maps (Low 256) + report; Blend/SpawnGate.blend.
Run: blender --background --factory-startup --python Tools/Blender/build_spawngate.py -- [--preview <dir>]
     [--norender] [--only Ground|Air]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import chitin as K  # noqa: E402
import common as C  # noqa: E402
import tower_kit as tk  # noqa: E402

TAU = 2 * math.pi
UP = Vector((0, 0, 1))
TEX = 512
FAMILIES = ("membrane", "chitin")

PAL = K.Palette(top=(0.3, 0.08, 0.06), side=(0.2, 0.055, 0.05), belly=(0.08, 0.03, 0.03), seam=(0.05, 0.015, 0.015),
                dark=(0.06, 0.02, 0.02), glow=(1.0, 0.42, 0.1), eye=(0.75, 1.0, 0.25),
                membrane=(0.38, 0.1, 0.07), bone=(0.62, 0.5, 0.36), dust=(0.2, 0.14, 0.11))
SPINE = K.Palette(top=(0.24, 0.12, 0.08), side=(0.16, 0.07, 0.05), belly=(0.07, 0.03, 0.03), seam=(0.04, 0.02, 0.02),
                  dark=(0.1, 0.04, 0.03), glow=(1.0, 0.45, 0.12), eye=(0.75, 1.0, 0.25), bone=(0.7, 0.6, 0.45))
GREEN = np.array((0.55, 0.95, 0.2))


def radial(a):
    return Vector((math.cos(a), math.sin(a), 0))


def tangent(a):
    return Vector((-math.sin(a), math.cos(a), 0))


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def flesh_style(centre_z, pit, outer, seed=0, pit_axis=UP, cells=70):
    """Hide of overlapping chitin scales over flesh round a throat: irregular plates with dark seams (Voronoi
    cells, never concentric rings), glowing veins that run out of the pit along some seams and fade, pustule bumps,
    a hot throat floor. `pit` is the throat radius, `outer` the radius where the veins die out."""
    rng = np.random.default_rng(seed)
    dirs = rng.normal(size=(cells, 3))
    dirs /= np.linalg.norm(dirs, axis=1, keepdims=True)
    seeds = dirs * np.array((0.9, 0.9, 0.5)) + np.array((0, 0, centre_z * 0.3))

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        r = np.hypot(p[:, 0], p[:, 1])
        ang = np.arctan2(p[:, 1], p[:, 0])
        warp = C.value_noise(p, 3.0, seed) * 1.6
        top = C.smoothstep(0.1, 0.8, n @ np.array(pit_axis))
        in_pit = (1 - C.smoothstep(pit * 0.55, pit, r)) * top
        edge = C.voronoi_edge(p, seeds)
        seam = 1 - C.smoothstep(0.0, 0.018, edge)
        plate = C.smoothstep(0.0, 0.06, edge)
        vein = 1 - C.smoothstep(0.0, 0.12, np.abs(np.sin(ang * 3.5 + warp * 1.4 + r * 4)))
        vein *= C.smoothstep(pit * 0.8, pit * 1.2, r) * (1 - C.smoothstep(pit * 1.2, outer, r))
        vein = np.maximum(vein * 0.8, seam * (1 - C.smoothstep(pit, outer * 0.75, r)) * 0.9)
        pust = C.smoothstep(0.6, 0.82, C.value_noise(p, 16, seed + 3)) * (1 - in_pit) * (1 - seam)
        s.disp[idx] = (0.01 * plate - 0.012 * seam + 0.01 * pust) - 0.13 * in_pit
        col = C.chitin_gradient(n, PAL.membrane, PAL.side, PAL.belly) * K.mottle(s, idx, 0.18, 30, seed + 4)
        col = col * (0.8 + 0.3 * plate)[:, None]
        col = C.lerp3(col, PAL.seam, seam * 0.75)
        col = C.lerp3(col, PAL.glow * 0.4, vein * 0.7)
        col = C.lerp3(col, np.array((0.42, 0.3, 0.12)), pust * 0.45)
        col = C.lerp3(col, PAL.glow * 0.5, in_pit)
        s.albedo[idx] = col
        throat = in_pit * (0.6 + 0.4 * C.smoothstep(0.3, 0.8, C.value_noise(p, 9, seed + 5)))
        s.emit[idx] = PAL.glow[None, :] * np.clip(vein * 0.65 + throat, 0, 1)[:, None] + \
            GREEN[None, :] * (pust * 0.3 * (1 - vein))[:, None]
        s.rough[idx] = 0.62 + 0.2 * seam - 0.15 * pust
        K.family(s, idx, chitin=0.6 * plate, membrane=(1 - 0.6 * plate) * (1 - in_pit), flow=r)
    return style


def root_style(seed=0):
    """Ground roots: flesh cords that sink into the soil, glowing seam along their top fading out."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, PAL.side * 0.8, PAL.belly, PAL.belly) * K.mottle(s, idx, 0.15, 40, seed)
        seam = C.smoothstep(0.75, 0.98, n[:, 2]) * (1 - C.smoothstep(0.2, 0.8, t))
        s.albedo[idx] = C.lerp3(col, PAL.seam, C.smoothstep(0.5, 1.0, t) * 0.6)
        s.emit[idx] = PAL.glow[None, :] * (seam * 0.35)[:, None]
        s.rough[idx] = 0.7
        K.family(s, idx, membrane=1.0, flow=t * C.axis_length(part))
    return style


# ---------------------------------------------------------------------------------------------------------------
# Specs
# ---------------------------------------------------------------------------------------------------------------

class Ground:
    name = "SpawnGate_Ground"
    tris = 3800
    voxel = 0.01
    ring_z = 0.52
    ring_r = 0.42
    ring_normal = UP
    lips = 6
    pit = 0.3

    def __init__(self):
        self.bones = [tk.Bone("Root", (0, 0, 0), UP, deform=False, length=0.1),
                      tk.Bone("Body", (0, 0, 0.02), UP, "Root", length=0.2),
                      tk.Bone("Ring", (0, 0, self.ring_z), self.ring_normal, "Root", length=0.12)]
        self.lip_axes = {}
        self.sway = {}
        self.sockets = {"Socket_Emerge": (0, 0, 0.12), "Socket_Spores": (0, 0, 0.24), "Socket_Light": (0, 0, 0.6)}

    def parts(self):
        b = C.PartBuilder()
        b.ellipsoid("Mound", Vector((0, 0, 0.0)), (0.82, 0.8, 0.3), bone="Body", style=flesh_style(0.2, self.pit, 0.85, 11),
                    segments=(64, 32))
        # A thick fleshy collar round the throat: the lips grow out of it.
        b.ring("Collar", Vector((0, 0, 0.2)), UP, self.pit + 0.04, 0.07, arcs=3, sides=10, chain=["Body"],
               style=flesh_style(0.2, self.pit * 0.6, 0.6, 12))
        rng = np.random.default_rng(3)
        for i in range(self.lips):
            a = TAU * i / self.lips + 0.2
            d, t = radial(a), tangent(a)
            h = 0.2 + rng.uniform(-0.03, 0.05)
            root = d * (self.pit + 0.03) + UP * 0.21
            pts = [root, d * (self.pit + 0.01) + UP * (h + 0.11), d * (self.pit - 0.07) + UP * (h + 0.17) + t * 0.025,
                   d * (self.pit - 0.17) + UP * (h + 0.14) + t * 0.02]
            name = f"Lip_{i}"
            b.tube("Lip", pts, [0.08, 0.066, 0.045, 0.01], sides=10, chain=[name], flatten=0.5,
                   style=K.tube(PAL, 1.0, PAL.membrane * 1.2, PAL.glow * 0.7, tip_glow=1.0, rings=(0.3, 0.55), glow_from=0.55))
            self.bones.append(tk.Bone(name, root, (pts[-1] - root).normalized(), "Body", length=0.08))
            self.lip_axes[name] = t
        # Five hooked spines of unequal length; the two tall ones lean in like claws over the throat.
        spec = [(0.5, 0.9, 0.085), (1.65, 0.62, 0.06), (2.6, 0.78, 0.07), (3.9, 0.55, 0.055), (5.1, 0.95, 0.09)]
        for i, (a, h, rad) in enumerate(spec):
            d = radial(a)
            root = d * 0.62 + UP * 0.12
            pts = [root, d * 0.7 + UP * (h * 0.45), d * 0.64 + UP * (h * 0.85), d * 0.48 + UP * h, d * 0.4 + UP * (h * 0.9)]
            name = f"Spine_{i}"
            b.tube("Spine", pts, [rad, rad * 0.8, rad * 0.55, rad * 0.3, 0.004], sides=10, chain=[name],
                   style=K.hook(SPINE, 1.0, glow=0.5, glow_from=0.9))
            self.bones.append(tk.Bone(name, root, UP, "Body", length=0.1))
            self.sway[name] = tangent(a)
        # Pustule eyes in a crooked cluster on one flank only.
        for j, (a, rr, z, sz) in enumerate([(4.4, 0.66, 0.17, 0.05), (4.6, 0.6, 0.21, 0.038), (4.25, 0.58, 0.22, 0.032),
                                            (2.1, 0.68, 0.14, 0.04)]):
            b.ellipsoid("Eye", radial(a) * rr + UP * z, (sz, sz, sz * 0.85), bone="Body", style=K.eye(PAL, 1.0),
                        segments=(16, 8))
        # Roots crawling into the ground.
        for i, (a, reach, bend) in enumerate([(0.45, 1.05, 0.12), (1.5, 0.92, -0.1), (2.35, 1.15, 0.16),
                                              (3.7, 0.95, -0.14), (5.6, 1.1, 0.1)]):
            d, t = radial(a), tangent(a)
            pts = [d * 0.6 + UP * 0.04, d * 0.78 + t * bend * 0.5 + UP * 0.02, d * 0.95 + t * bend - UP * 0.005,
                   d * reach + t * bend * 1.3 - UP * 0.03]
            b.tube("Root", pts, [0.075, 0.055, 0.035, 0.01], sides=8, chain=["Body"], flatten=0.4, style=root_style(i))
        self.ring_parts(b)
        return b

    def ring_parts(self, b):
        c = Vector((0, 0, self.ring_z))
        n = self.ring_normal

        def prof(th):
            return 1.0 + 0.7 * max(0.0, math.cos(th * 7)) ** 4
        b.ring("RingBand", c, n, self.ring_r, 0.036, arcs=3, sides=8, chain=["Ring"], profile=prof, ref=(1, 0, 0),
               style=K.ring_band(PAL, 1.0, tuple(c), tuple(n), beads=7, nodes=7, node_glow=1.0, joint_glow=0.6,
                                 inner_glow=0.7))
        a_ax = (Vector((1, 0, 0)) - n * n.x).normalized()
        b_ax = n.cross(a_ax)
        for i in range(7):
            th = TAU * i / 7 + TAU / 14
            d = a_ax * math.cos(th) + b_ax * math.sin(th)
            t = n.cross(d)
            root = c + d * self.ring_r
            pts = [root, root + d * 0.07 - n * 0.015 + t * 0.025, root + d * 0.12 - n * 0.06 + t * 0.04,
                   root + d * 0.13 - n * 0.12 + t * 0.035]
            b.tube("RingClaw", pts, [0.034, 0.025, 0.014, 0.003], sides=6, chain=["Ring"], style=K.hook(SPINE, 1.0, glow=0.6))

    def clips(self, arm):
        lips, sway = self.lip_axes, self.sway
        n = self.ring_normal

        def idle(t, f):
            spec = {"Ring": {"rot": (n, -TAU * t)},
                    "Body": {"scale": (1.0 + 0.012 * math.sin(TAU * 3 * t), 1.0, 1.0 + 0.012 * math.sin(TAU * 3 * t))}}
            for k, (name, axis) in enumerate(lips.items()):
                spec[name] = {"rot": (axis, math.radians(6) * math.sin(TAU * 4 * t + k * 1.3))}
            for k, (name, axis) in enumerate(sway.items()):
                spec[name] = {"rot": (axis, math.radians(2.5) * math.sin(TAU * 2 * t + k * 2.1))}
            return spec

        def spin(t, f):
            return {"Ring": {"rot": (n, -TAU * t)}}

        def spawn(t, f):
            open_ = math.sin(math.pi * min(1.0, t * 1.4)) if t < 0.72 else 0.0
            open_ = max(open_, 0.0)
            spec = {"Ring": {"rot": (n, -math.radians(90) * bk.ease(t)), "loc": n * (0.06 * math.sin(math.pi * t))},
                    "Body": {"scale": (1.0 + 0.05 * open_, 1.0, 1.0 + 0.08 * open_)}}
            for k, (name, axis) in enumerate(lips.items()):
                spec[name] = {"rot": (axis, math.radians(55) * open_ * (0.85 + 0.15 * math.sin(k * 2.0)))}
            for name, axis in sway.items():
                spec[name] = {"rot": (axis, math.radians(-4) * open_)}
            return spec
        return [bk.clip(arm, "Idle", 360, idle, loop=True), bk.clip(arm, "Spin", 360, spin, loop=True),
                bk.clip(arm, "Spawn", 40, spawn)]


def pod_deform(v):
    """A seed-pod: the lower half drawn down to a blunt point, the upper half narrowing to a neck round the throat."""
    x, y, z = v
    if z < 0:
        s = 1 + 0.55 * z
        return Vector((x * s, y * s, z * 1.35))
    s = 1 - 0.42 * z ** 1.5
    return Vector((x * s, y * s, z * 0.9))


class Air(Ground):
    name = "SpawnGate_Air"
    tris = 3600
    voxel = 0.009
    ring_z = -0.2
    ring_r = 0.52
    ring_normal = Vector((0.07, -0.12, 1.0)).normalized()
    lips = 5
    pit = 0.17

    def __init__(self):
        super().__init__()
        self.sockets = {"Socket_Emerge": (0, 0, 0.3), "Socket_Spores": (0, 0, 0.36), "Socket_Light": (0, 0, 0.05)}
        self.bones[1] = tk.Bone("Body", (0, 0, 0), UP, "Root", length=0.2)

    def parts(self):
        b = C.PartBuilder()
        top = 0.4
        b.ellipsoid("Pod", Vector((0, 0, 0)), (0.46, 0.46, 0.45), bone="Body", style=flesh_style(top, self.pit, 0.7, 21),
                    segments=(64, 32), deform=pod_deform)
        for i in range(self.lips):
            a = TAU * i / self.lips + 0.5
            d, t = radial(a), tangent(a)
            root = d * (self.pit + 0.03) + UP * (top - 0.09)
            pts = [root, d * (self.pit + 0.03) + UP * (top + 0.02), d * (self.pit - 0.04) + UP * (top + 0.08) + t * 0.02,
                   d * (self.pit - 0.12) + UP * (top + 0.06)]
            name = f"Lip_{i}"
            b.tube("Lip", pts, [0.055, 0.045, 0.03, 0.007], sides=10, chain=[name], flatten=0.55,
                   style=K.tube(PAL, 1.0, PAL.membrane, PAL.glow * 0.6, tip_glow=0.9, rings=(0.35, 0.6), glow_from=0.7))
            self.bones.append(tk.Bone(name, root, (pts[-1] - root).normalized(), "Body", length=0.08))
            self.lip_axes[name] = t
        # Curved spurs grow out of the shoulders and hook down past the ring: a pod that bites, not a pot.
        for i, (a, length) in enumerate([(0.1, 0.34), (1.35, 0.26), (2.5, 0.38), (3.8, 0.28), (5.0, 0.33)]):
            d, t = radial(a), tangent(a)
            root = d * 0.36 + UP * 0.12
            pts = [root, root + d * length * 0.45 + UP * 0.07 + t * 0.03, root + d * length * 0.85 + UP * 0.0 + t * 0.05,
                   root + d * length - UP * length * 0.45 + t * 0.06]
            b.tube("Spur", pts, [0.05, 0.036, 0.02, 0.003], sides=8, chain=["Body"],
                   style=K.hook(SPINE, 1.0, glow=0.5, glow_from=0.88))
        for j, (a, rr, z, sz) in enumerate([(2.0, 0.44, -0.02, 0.045), (2.25, 0.42, 0.06, 0.035), (1.8, 0.4, 0.07, 0.03)]):
            b.ellipsoid("Eye", radial(a) * rr + UP * z, (sz, sz, sz * 0.85), bone="Body", style=K.eye(PAL, 1.0),
                        segments=(16, 8))
        # Four tendrils hanging from the underside, two bones each, unequal lengths.
        for i, (a, length) in enumerate([(0.3, 0.75), (1.9, 0.55), (3.4, 0.85), (4.9, 0.6)]):
            d = radial(a)
            root = d * 0.12 + UP * -0.5
            mid = root + d * 0.08 - UP * length * 0.5
            tip = root + d * 0.02 - UP * length + tangent(a) * 0.06
            pts = [root, root + d * 0.06 - UP * length * 0.25, mid, mid + d * -0.02 - UP * length * 0.25, tip]
            names = [f"Tendril_{i}_0", f"Tendril_{i}_1"]
            b.tube("Tendril", pts, [0.05, 0.04, 0.03, 0.022, 0.006], sides=8, chain=names,
                   style=K.tube(PAL, 1.0, PAL.side, PAL.glow * 0.5, tip_glow=1.0, rings=(0.3, 0.55, 0.75), glow_from=0.75))
            self.bones.append(tk.Bone(names[0], root, (mid - root).normalized(), "Body", length=(mid - root).length))
            self.bones.append(tk.Bone(names[1], mid, (tip - mid).normalized(), names[0], length=(tip - mid).length))
            self.sway[names[0]] = tangent(a)
            self.sway[names[1]] = tangent(a)
        self.ring_parts(b)
        return b

    def clips(self, arm):
        acts = super().clips(arm)
        return acts


# ---------------------------------------------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------------------------------------------

def build(spec):
    builder = spec.parts()
    asset = C.build_mesh_asset(spec.name, builder, voxel=spec.voxel, target_tris=spec.tris, tex_size=TEX,
                               ao_distance=0.25, extrusion=0.03, ray=0.08, emission_strength=2.4, smooth_iterations=3,
                               symmetric=False, low_from_detail=True, families=FAMILIES, low_size=TEX // 2)
    arm = tk.build_armature(spec.name + "_Rig", spec.bones)
    socks = []
    for n, p in spec.sockets.items():
        socks.append(tk.Bone(n, p, UP, "Root", deform=False, length=0.06))
    # Rebuild with the sockets included (non-deforming bones travel in the FBX through tk.export_fbx).
    bpy.data.objects.remove(arm)
    arm = tk.build_armature(spec.name + "_Rig", spec.bones + socks)
    rigid = ("RingBand", "RingClaw")
    C.skin(asset.low, arm, builder, rigid=rigid, blend=0)
    acts = spec.clips(arm)
    arm.animation_data.action = acts[0]
    path = os.path.join(C.EXPORTS, spec.name, spec.name + ".fbx")
    tk.export_fbx(path, [arm, asset.low], animated=True)
    clip_lengths = {a.name: int(a.frame_range[1]) for a in acts}
    # Action names are global in Blender: free "Idle"/"Spin"/"Spawn" for the next gate (the FBX already has them).
    for a in acts:
        a.name = spec.name + ":" + a.name
    report = {"asset": spec.name, "round": 25, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
              "texture_size": TEX, "low_size": TEX // 2, "maps": ["Albedo", "Normal", "Emission", "Mask"],
              "mask": "R AO, G smoothness, B membrane detail, A chitin detail", "uv": asset.uv_stats,
              "detail": asset.details, "bones": [bn.name for bn in arm.data.bones],
              "deform_bones": [bn.name for bn in arm.data.bones if bn.use_deform],
              "sockets": {k: list(v) for k, v in spec.sockets.items()},
              "clips": clip_lengths,
              "pivot": "ground: centre of the mound on the ground; air: centre of the pod (hangs 1.6 above the road)",
              "dims": [round(d, 3) for d in asset.low.dimensions]}
    C.write_report(spec.name, report)
    return asset, arm, acts


def previews(built, out):
    C.preview_world()
    stage = bk.road_stage(2.0, 40, axis="y")
    for name, (asset, arm, acts) in built.items():
        for other, (a2, r2, _) in built.items():
            a2.low.hide_render = other != name
        size = 2.4
        target = Vector((0, 0, 0.35))
        bk.render_close(target, os.path.join(out, f"spawngate_{name.lower()}_close.png"), size * 1.4, azimuth=25,
                        elevation=32, lens=50, light_scale=0.8)
        # Spawn strip.
        arm.animation_data.action = next(a for a in acts if a.name.endswith(":Spawn"))
        rig = tk.close_lights(0.8)
        cam = tk.closeup_camera(target, size * 1.4, azimuth=25, elevation=32, lens=50)
        strip = []
        for f in (0, 10, 18, 26, 40):
            bpy.context.scene.frame_set(f)
            fp = os.path.join(out, f"_sg_{name}_{f}.png")
            C.render(cam, fp, 480, 360, bloom=True)
            strip.append(fp)
        C.contact_sheet(strip, os.path.join(out, f"spawngate_{name.lower()}_spawn_strip.png"), len(strip), 360)
        for fp in strip:
            os.remove(fp)
        bk.remove([cam] + rig)
        arm.animation_data.action = acts[0]
        bpy.context.scene.frame_set(0)
    for name, (asset, arm, acts) in built.items():
        asset.low.hide_render = False
        if name == "Air":
            arm.location = (1.8, 0.8, 1.6 * 1.0)
        arm.scale = (1.2, 1.2, 1.2) if name == "Ground" else (0.96, 0.96, 0.96)
    bk.render_game(Vector((0.6, 0.4, 0.5)), os.path.join(out, "spawngate_game.png"), zoom=1.6)
    bk.remove(stage)


SPECS = {"Ground": Ground, "Air": Air}


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    only = args.get("only")
    built = {}
    for key, cls in SPECS.items():
        if only and key != only:
            continue
        built[key] = build(cls())
    C.save_blend("SpawnGate" if not only else "SpawnGate_" + only)
    if not args.get("norender"):
        previews(built, out)


if __name__ == "__main__":
    main()
