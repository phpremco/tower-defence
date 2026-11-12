"""Round 39: lifelike insect gaits for the Skitter and the Shardback (Remco 2026-11-09: "Can we make the ant walking
animation more lifelike?").

The old Walk clips moved the feet on a symmetric sine step with a 50/50 stance, the body bobbing on plain sines, and
the Skitter's clip covered less ground than Unity thought (stance 0.30 model units per loop against the catalog's
0.36), while at its speed Unity needed 5.6x playback and clamped it at 4x: its feet slid ~40% of the way. Here:

- An insect step: each foot is planted with zero ground speed (it moves back in the rig at exactly the body speed, so
  it stays put in the world), lifts straight up, swings forward on a Hermite curve whose end tangents match the
  ground speed (no skid at lift-off or touchdown), arcs a little outward and comes down a little later than it rose.
- Gaits: the Shardback walks an alternating tripod (L1 R2 L3 against R1 L2 R3); the Skitter has four walking legs
  (its fore pair are raised scythes), so it walks the diagonal pairs (L1 R2 against R1 L2). In each group the hind
  leg sets down a beat before the front one (the back-to-front wave of real insects), the duty factor is above one
  half so the groups overlap on the ground.
- Natural variance: a clip holds several gait cycles, and every single step has its own small timing, duty, lift
  and landing-spot offsets (seeded, deterministic); the loop seam is continuous.
- The body follows the feet: a dip after each group touches down, a small surge, roll and yaw towards the side of
  the stance front leg, a counter-rotating stabilised head, mandibles that flutter and twitch, the Skitter's scythes
  rocking with the diagonals and its abdomen swaying behind the thorax with a lag.
- Stride matched to Unity: the distance per loop is chosen from the EnemyDefinition speed and scale
  (Assets/_Project/Data/Enemies/<Name>.asset, read only) so Unity's playback rate (CreatureClips.Rate:
  speed / (distance x scale) x clip length, clamped 0.3..4) stays well inside its clamp; the report gets
  `walk_distance_per_loop` (it overrides the catalog's MoveDistance) and a `round39_gait` block with the measured
  foot slide before and after.

Opens ArtSource/Blend/<Name>.blend (the species script and build_attacks36.py run first), re-keys only Walk on the
full and the LOD1 rig, exports <Name>.fbx and <Name>_LOD1.fbx with every clip under the same names, merges the report
and saves the .blend. Idempotent.

Previews (ArtSource/Previews/Creatures/gait_<name>_*.png): a contact sheet of one full cycle (side and game camera)
and a before/after strip of the creature travelling over a marked ground at Unity's playback rate (the before row is
rendered with --before, on the first run, from the clip still in the .blend, and kept for later runs).

Run: blender --background --factory-startup --python Tools/Blender/build_insect_gait.py --
     [--only Skitter,Shardback] [--preview <dir>] [--norender] [--before]
"""

import json
import math
import os
import random
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import boss_mech_kit as BMK  # noqa: E402
import build_attacks36 as A36  # noqa: E402
import common as C  # noqa: E402

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
d = math.radians
FPS = 30
# BlenderArtEntry.SpeciesScale: Unity's model scale = EnemyDefinition scale x this.
SPECIES_SCALE = 1.4
# CreatureClips.MinRate / MaxRate.
UNITY_RATE = (0.3, 4.0)
# A foot counts as planted below this height (model units).
PLANTED = 0.004


# ---------------------------------------------------------------------------------------------------------------
# The step
# ---------------------------------------------------------------------------------------------------------------

def hermite(p0, p1, m0, m1, s):
    s2, s3 = s * s, s * s * s
    return (2 * s3 - 3 * s2 + 1) * p0 + (s3 - 2 * s2 + s) * m0 + (-2 * s3 + 3 * s2) * p1 + (s3 - s2) * m1


class LegSchedule:
    """The steps of one leg over a clip of `cycles` gait cycles, in clip time u (0..1).

    Each step: touchdown td, stance length ls (both in u), landing spot F (forward offset from the rest foot), lift
    and a lateral landing offset. While planted the foot moves back at the clip's ground speed D (model units per
    loop), so it does not move in the world."""

    def __init__(self, phase, cycles, duty, lift, distance, rng, shift=0.0, jitter=0.025, duty_jitter=0.02, lift_jitter=0.15,
                 land_jitter=0.012, side_jitter=0.006):
        self.D = distance
        steps = []
        for k in range(cycles):
            td = ((k + phase + rng.uniform(-jitter, jitter)) / cycles) % 1.0
            ls = (duty + rng.uniform(-duty_jitter, duty_jitter)) / cycles
            centre = shift + rng.uniform(-land_jitter, land_jitter)
            steps.append(dict(td=td, ls=ls, F=centre + distance * ls / 2, lift=lift * (1 + rng.uniform(-lift_jitter, lift_jitter)),
                              side=rng.uniform(-side_jitter, side_jitter)))
        self.steps = sorted(steps, key=lambda s: s["td"])

    def foot(self, u):
        """(forward, up, out, planted) of the foot at clip time u."""
        n = len(self.steps)
        for i, st in enumerate(self.steps):
            du = (u - st["td"]) % 1.0
            if du < st["ls"]:
                return st["F"] - self.D * du, 0.0, st["side"], True
        for i, st in enumerate(self.steps):
            nxt = self.steps[(i + 1) % n]
            lo = st["td"] + st["ls"]
            sw = (nxt["td"] - lo) % 1.0
            du = (u - lo) % 1.0
            if du <= sw:
                s = du / sw
                m = -self.D * sw
                fwd = hermite(st["F"] - self.D * st["ls"], nxt["F"], m, m, s)
                # Rises fast, comes down a little later and softer: the peak sits before mid-swing.
                lift = st["lift"] + (nxt["lift"] - st["lift"]) * s
                up = lift * math.sin(math.pi * s ** 0.8) ** 1.2
                out = st["side"] + (nxt["side"] - st["side"]) * s + 0.35 * lift * math.sin(math.pi * s)
                return fwd, up, out, False
        return 0.0, 0.0, 0.0, True


class Gait:
    def __init__(self, legs, phases, cycles, duty, lift, distance, seed, shifts=None):
        """shifts: {pair index: forward offset of that pair's stance centre} (front legs work ahead of their rest
        spot less, hind legs push from further forward, so no leg ends its stroke locked straight)."""
        rng = random.Random(seed)
        self.legs = legs
        shifts = shifts or {}
        self.schedules = {key: LegSchedule(phases[key], cycles, duty, lift, distance, rng, shifts.get(key[1], 0.0))
                          for key in legs}

    def feet(self, u):
        out = {}
        for key, lg in self.legs.items():
            fwd, up, side, _ = self.schedules[key].foot(u)
            sx = -1 if key[0] == "L" else 1
            out[key] = lg.foot + Vector((side * sx, -fwd, up))
        return out

    def planted(self, key, u):
        return self.schedules[key].foot(u)[3]


def group_phases(groups, wave):
    """Touchdown phase per leg: group g at g / len(groups); within a group the hind legs set down `wave` earlier than
    the leg ahead of them (pair index 1 = front)."""
    out = {}
    for g, keys in enumerate(groups):
        pairs = sorted({k[1] for k in keys})
        mid = (pairs[0] + pairs[-1]) / 2
        for key in keys:
            out[key] = g / len(groups) + (key[1] - mid) * wave
    return out


# ---------------------------------------------------------------------------------------------------------------
# Species
# ---------------------------------------------------------------------------------------------------------------

def skitter_walk(m, cfg, gait):
    """Diagonal pairs; the thorax dips after each pair lands, yaws towards the stance front leg and rolls a little;
    the head stays steadier than the body; the scythes rock with the diagonals; the abdomen sways behind with a lag;
    the four jaws flutter with a twitch twice per clip."""
    n = cfg["cycles"]

    def spec(u, f):
        w = 2 * math.pi * n * u
        clip = 2 * math.pi * u
        dip = math.cos(2 * w - 2 * math.pi * 0.2)
        yaw = -d(3.2) * math.sin(w - 0.35)
        twitch = max(0.0, math.sin(2 * clip + 0.7)) ** 8
        specs = m.body_specs(offset=Vector((0.004 * math.sin(w), -0.006 * math.sin(2 * w - 0.6), -0.006 * dip + 0.002)),
                             roll=d(2.2) * math.sin(w - 0.2), thorax_pitch=d(1.2) * math.sin(2 * w - 1.0) + d(1.0),
                             sway=-d(5.5) * math.sin(w - 1.1), belly=d(2.5) * math.cos(2 * w - 1.6) + d(1.5) * math.sin(clip),
                             head_yaw=-0.65 * yaw + d(3) * math.sin(clip + 0.4) + d(1.5) * math.sin(3 * clip),
                             head_pitch=d(1.5) * dip + d(2) * math.sin(2 * clip + 1.3),
                             jaw=d(5) + d(4) * (0.5 + 0.5 * math.sin(5 * clip)) + d(16) * twitch,
                             blade=d(3) + d(2) * math.sin(2 * w - 0.4), blade_spread=d(2) * math.sin(w + 0.4),
                             reach=d(3) * math.sin(2 * w + 0.6) - d(3) * twitch)
        specs["Thorax"]["rot"].insert(0, (Z, yaw))
        # The abdomen lags further behind at its tip.
        specs["Abdomen_2"]["rot"][0] = (Z, -d(6.5) * math.sin(w - 1.9))
        # Each scythe rocks with its own diagonal (the left one rises while the left fore leg swings).
        for side, sx in m.SIDES:
            rock = d(4) * math.sin(w + (0.0 if side == "L" else math.pi) - 0.9)
            specs[f"Blade_{side}_1"]["rot"].append((X, -rock))
        return m.place_legs(poser_holder["poser"], specs, gait.feet(u))
    return spec


def shardback_walk(m, cfg, gait):
    """Alternating tripod; the heavy body sinks after each tripod lands, surges, rolls onto the two-leg side and yaws
    towards the stance front leg; the plough head is held steady with a slow look; the mandibles work."""
    n = cfg["cycles"]

    def spec(u, f):
        w = 2 * math.pi * n * u
        clip = 2 * math.pi * u
        dip = math.cos(2 * w - 2 * math.pi * 0.18)
        yaw = -d(2.6) * math.sin(w - 0.3)
        twitch = max(0.0, math.sin(clip + 2.0)) ** 10
        specs = m.body_specs(offset=Vector((0.005 * math.sin(w - 0.2), -0.007 * math.sin(2 * w - 0.7), -0.008 * dip + 0.003)),
                             pitch_=d(1.2) * math.sin(2 * w - 0.9), roll=d(2.4) * math.sin(w - 0.25), yaw=yaw,
                             head=d(1.5) * dip + d(2) * math.sin(clip + 0.8), head_yaw=-0.7 * yaw + d(4) * math.sin(clip),
                             jaws=d(4) + d(5) * (0.5 + 0.5 * math.sin(3 * clip + 0.5)) + d(14) * twitch)
        return m.K3.place_legs(poser_holder["poser"], specs, m.legs(), gait.feet(u))
    return spec


SPECIES = {
    "Skitter": dict(script="build_skitter.py", body="Thorax", lod="skitter",
                    groups=[(("L", 1), ("R", 2)), (("R", 1), ("L", 2))],
                    # 3 cycles of 14 frames; 0.44 model units per cycle; stance 58%.
                    cycles=3, frames_per_cycle=14, cycle_distance=0.44, duty=0.58, lift=0.055, wave=0.03, seed=39,
                    stance_shift={1: -0.02, 2: 0.035},
                    walk=skitter_walk, old_unity_distance=0.36),
    "Shardback": dict(script="build_shardback.py", body="Body", lod="common",
                      groups=[(("L", 1), ("R", 2), ("L", 3)), (("R", 1), ("L", 2), ("R", 3))],
                      cycles=2, frames_per_cycle=24, cycle_distance=0.32, duty=0.62, lift=0.05, wave=0.025, seed=17,
                      stance_shift={1: -0.01, 3: 0.02},
                      walk=shardback_walk, old_unity_distance=None),
}

# The specs need the poser the clip is keyed with; set per species before keying.
poser_holder = {}


def enemy_data(name):
    path = os.path.join(C.REPO, "Assets", "_Project", "Data", "Enemies", name + ".asset")
    with open(path, encoding="utf-8") as fh:
        text = fh.read()
    speed = float(re.search(r"^\s*_speed:\s*([0-9.]+)", text, re.M).group(1))
    scale = float(re.search(r"^\s*_scale:\s*([0-9.]+)", text, re.M).group(1))
    return speed, scale


def unity_rate(speed, distance, scale, length):
    raw = speed / (distance * scale) * length
    return raw, min(max(raw, UNITY_RATE[0]), UNITY_RATE[1])


class Creature:
    def __init__(self, name):
        self.name = name
        self.cfg = SPECIES[name]
        self.mod = A36.load_builder(self.cfg["script"])
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects[name + "_LOD1_Rig"]
        self.lod_mesh = bpy.data.objects[name + "_LOD1"]
        with open(self.report_path(), encoding="utf-8") as fh:
            self.rep = json.load(fh)
        self.clips = list(self.rep["actions"])
        scale_bones = list(self.rep.get("scale_keyed_bones") or [])
        lod_names = {b.name for b in self.lod_arm.data.bones}
        self.poser = B.ScalePoser(self.arm, scale_bones)
        self.lod_poser = B.ScalePoser(self.lod_arm, [n for n in scale_bones if n in lod_names])
        speed, def_scale = enemy_data(name)
        self.speed = speed
        self.scale = def_scale * SPECIES_SCALE
        self.v_model = speed / self.scale
        cfg = self.cfg
        self.frames = cfg["cycles"] * cfg["frames_per_cycle"]
        self.distance = cfg["cycles"] * cfg["cycle_distance"]
        self.legs = self.mod.legs()
        self.feet_bones = {key: lg.names[-1] for key, lg in self.legs.items()}

    def report_path(self):
        return os.path.join(C.EXPORTS, self.name, self.name + "_report.json")

    # -- keying ----------------------------------------------------------------------------------------------------

    def spec(self):
        cfg = self.cfg
        phases = group_phases(cfg["groups"], cfg["wave"])
        self.gait = Gait(self.legs, phases, cfg["cycles"], cfg["duty"], cfg["lift"], self.distance, cfg["seed"],
                         cfg.get("stance_shift"))
        poser_holder["poser"] = self.poser
        return cfg["walk"](self.mod, cfg, self.gait)

    def key(self):
        for n in ("Walk", "Walk_LOD"):
            act = bpy.data.actions.get(n)
            if act is not None:
                bpy.data.actions.remove(act)
        spec = self.spec()
        self.walk = self.poser.action("Walk", self.frames, spec, loop=True)
        if self.cfg["lod"] == "skitter":
            self.walk_lod = self.mod.lod_action(self.poser, self.lod_poser, "Walk_LOD", self.frames, spec, True)
        else:
            self.walk_lod = C.lod_action(self.poser, self.lod_poser, "Walk_LOD", self.frames, spec, True,
                                         getattr(self.mod, "LOD_AIMS", {}))
        self.arm.animation_data.action = bpy.data.actions["Idle"]
        self.lod_arm.animation_data.action = bpy.data.actions["Idle_LOD"]
        C.log(self.name, "keyed Walk", self.frames)

    def actions(self, lod=False):
        return {n: bpy.data.actions[n + ("_LOD" if lod else "")] for n in self.clips}

    def export(self):
        folder = os.path.join(C.EXPORTS, self.name)
        # Round 46: the close-up detail parts (<Name>_Face/_Claws, saved in the .blend) stay in the exports.
        extra = [bpy.data.objects.get(f"{self.name}_{p}") for p in ("Face", "Claws")]
        extra_lod = [bpy.data.objects.get(f"{self.name}_{p}_LOD1") for p in ("Face", "Claws")]
        BMK.export_rig(os.path.join(folder, self.name + ".fbx"), self.arm, [self.mesh] + [o for o in extra if o],
                       self.actions())
        BMK.export_rig(os.path.join(folder, self.name + "_LOD1.fbx"), self.lod_arm,
                       [self.lod_mesh] + [o for o in extra_lod if o], self.actions(True))

    # -- measuring -------------------------------------------------------------------------------------------------

    def play_rate(self, act, distance):
        length = act.frame_range[1] / FPS
        return unity_rate(self.speed, distance, self.scale, length)

    def pose_at(self, act, frame):
        C.set_action(self.arm, act, 0)
        whole = int(math.floor(frame))
        bpy.context.scene.frame_set(whole, subframe=frame - whole)

    def slide(self, act, distance, seconds=None, dt=1 / 240):
        """Unity's playback over `seconds`: the rig travels at the creature's speed while the clip plays at Unity's
        rate. Per planted foot (below PLANTED), how far it moves over the ground. Returns the facts."""
        frames = act.frame_range[1]
        raw, rate = self.play_rate(act, distance)
        seconds = seconds or 2 * frames / FPS / rate
        tracks = {k: [] for k in self.feet_bones}
        steps = int(seconds / dt)
        for i in range(steps + 1):
            tau = i * dt
            self.pose_at(act, (rate * tau * FPS) % frames)
            travel = self.v_model * tau
            for key, bone in self.feet_bones.items():
                p = self.arm.pose.bones[bone].tail
                tracks[key].append((p.x, p.y - travel, p.z))
        slides, stance_travel, root_travel = [], 0.0, 0.0
        for key, tr in tracks.items():
            seg = []
            for p in tr + [(0, 0, 1)]:
                if p[2] < PLANTED:
                    seg.append(p)
                    continue
                if len(seg) > 2:
                    ys = [q[1] for q in seg]
                    slides.append(abs(ys[-1] - ys[0]))
                    stance_travel += abs(ys[-1] - ys[0])
                    root_travel += self.v_model * dt * (len(seg) - 1)
                seg = []
        mean = sum(slides) / max(1, len(slides))
        return {"unity_rate_wanted": round(raw, 2), "unity_rate_played": round(rate, 2),
                "planted_steps_measured": len(slides),
                "foot_slide_per_step_model_mean": round(mean, 4), "foot_slide_per_step_model_max": round(max(slides or [0]), 4),
                "foot_slide_per_step_world_mean": round(mean * self.scale, 4),
                "slip_share": round(stance_travel / root_travel, 3) if root_travel else None}

    def reach_check(self):
        """The feet the IK could not reach while planted (they would hover): max planted height in the new clip."""
        worst = 0.0
        for f in range(self.frames):
            self.pose_at(self.walk, f)
            u = f / self.frames
            for key, bone in self.feet_bones.items():
                if self.gait.planted(key, u):
                    worst = max(worst, self.arm.pose.bones[bone].tail.z)
        return round(worst, 4)

    def write_report(self, gait_facts):
        rep = self.rep
        rep["actions"] = {n: int(round(a.frame_range[1])) for n, a in self.actions().items()}
        rep["walk_distance_per_loop"] = round(self.distance, 4)
        rep["round39_gait"] = gait_facts
        with open(self.report_path(), "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=2)
        C.log("report", self.report_path())


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

class Stage:
    def __init__(self, cr):
        self.cr = cr
        for o in bpy.data.objects:
            if o.type == "MESH" and o is not cr.mesh:
                o.hide_render = True
        C.preview_world()
        self.objs = list(C.light_rig(scale=1.2))
        self.objs.append(C.ground(size=40.0))
        # Pale stripes across the road every 0.1 model units: a sliding foot shows against them.
        mark = BMK.mat("GaitMark", (0.32, 0.36, 0.42), rough=0.7)
        for k in range(-30, 31):
            f = k * 0.1
            self.objs.append(BMK.box(f"Mark{k}", -1.6, 1.6, f - 0.006, f + 0.006, 0.0, 0.0015, mark, bevel=0))
        # Framing from the creature's standing size (the Skitter is ~1.1 model units long, the Shardback ~1.2 and tall).
        cr.pose_at(bpy.data.actions["Idle"], 0)
        dg = bpy.context.evaluated_depsgraph_get()
        ev = cr.mesh.evaluated_get(dg)
        me = ev.to_mesh()
        pts = [ev.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        size = max(hi - lo)
        # Aimed low (the feet matter, not the tops of the Shardback's prisms).
        self.target = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z + 0.32 * (hi.z - lo.z)))
        self.side = BMK.side_cam(self.target, 2.3 * size, elevation=8.0, azimuth=-90.0)
        self.game = C.game_camera(self.target, 1.6 * size)
        self.travel_cam = C.closeup_camera(self.target + Vector((0, 0.12, 0)), 2.6 * size, azimuth=-62, elevation=20, lens=50)

    def cycle_sheet(self, out):
        """One full gait cycle: 8 frames from the side (two rows), then the same 8 from the game camera."""
        cr = self.cr
        fpc = cr.cfg["frames_per_cycle"]
        frames = [fpc * i / 8 for i in range(8)]
        paths = []
        for key, cam in (("side", self.side), ("game", self.game)):
            for i, f in enumerate(frames):
                cr.pose_at(cr.walk, f)
                paths.append(C.render(cam, os.path.join(out, f"_gait_{cr.name}_{key}_{i}.png"), 640, 360, samples=16))
        path = os.path.join(out, f"gait_{cr.name.lower()}_cycle.png")
        C.contact_sheet(paths, path, 4, 480)
        for p in paths:
            os.remove(p)
        return path

    def travel_row(self, act, distance, label, out, window):
        """8 moments over `window` seconds of Unity playback, the rig travelling at the creature's speed."""
        cr = self.cr
        frames = act.frame_range[1]
        _, rate = cr.play_rate(act, distance)
        paths = []
        for i in range(8):
            tau = window * i / 8
            cr.arm.location = Vector((0, -cr.v_model * tau + cr.v_model * window * 0.45, 0))
            cr.pose_at(act, (rate * tau * FPS) % frames)
            paths.append(C.render(self.travel_cam, os.path.join(out, f"_gait_{cr.name}_{label}_{i}.png"), 640, 360,
                                  samples=16))
        cr.arm.location = Vector()
        path = os.path.join(out, f"_gait_{cr.name.lower()}_{label}_row.png")
        C.contact_sheet(paths, path, 4, 480)
        for p in paths:
            os.remove(p)
        return path

    def remove(self):
        BMK.remove(self.objs + [self.side, self.game, self.travel_cam])


def window_of(cr):
    """Two new gait cycles of real time."""
    return 2 * cr.cfg["cycle_distance"] / cr.v_model


def stack(paths, out_path):
    C.contact_sheet(paths, out_path, 1, 1920)


# ---------------------------------------------------------------------------------------------------------------

def build(name, args, out):
    cr = Creature(name)
    before_json = os.path.join(out, f"gait_{name.lower()}_before.json")
    before_row = os.path.join(out, f"_gait_{name.lower()}_before_row.png")
    if args.get("before"):
        old = bpy.data.actions["Walk"]
        old_distance = cr.rep.get("walk_distance_per_loop") or cr.cfg["old_unity_distance"]
        facts = {"frames": int(round(old.frame_range[1])), "unity_distance_per_loop": old_distance,
                 **cr.slide(old, old_distance)}
        with open(before_json, "w", encoding="utf-8") as fh:
            json.dump(facts, fh, indent=2)
        if not args.get("norender"):
            st = Stage(cr)
            st.travel_row(old, old_distance, "before", out, window_of(cr))
            st.remove()
    cr.key()
    after = {"frames": cr.frames, "unity_distance_per_loop": round(cr.distance, 4), **cr.slide(cr.walk, cr.distance),
             "max_planted_foot_height_model": cr.reach_check()}
    before = None
    if os.path.exists(before_json):
        with open(before_json, encoding="utf-8") as fh:
            before = json.load(fh)
    cfg = cr.cfg
    period = cfg["cycle_distance"] / cr.v_model
    facts = {
        "gait": "alternating tripod" if len(cfg["groups"][0]) == 3 else "diagonal pairs (four walking legs)",
        "groups": [["".join(map(str, k)) for k in g] for g in cfg["groups"]],
        "hind_leads_front_by_cycle": cfg["wave"], "duty_factor": cfg["duty"], "lift_model": cfg["lift"],
        "cycles_per_loop": cfg["cycles"], "frames_per_cycle": cfg["frames_per_cycle"],
        "distance_per_cycle_model": cfg["cycle_distance"], "stance_stroke_model": round(cfg["cycle_distance"] * cfg["duty"], 3),
        "enemy_speed": cr.speed, "unity_model_scale": round(cr.scale, 3),
        "steps_per_second_per_leg": round(1 / period, 2), "stride_world": round(cfg["cycle_distance"] * cr.scale, 3),
        "after": after, "before": before,
        "note": "planted feet move back at the clip's ground speed (zero world speed); per-step timing, duty, lift and "
                "landing jitter; the report's walk_distance_per_loop replaces the catalog MoveDistance",
    }
    C.log(name, json.dumps(facts))
    cr.export()
    cr.write_report(facts)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"), compress=True)
    if not args.get("norender"):
        st = Stage(cr)
        st.cycle_sheet(out)
        after_row = st.travel_row(cr.walk, cr.distance, "after", out, window_of(cr))
        st.remove()
        if os.path.exists(before_row):
            stack([before_row, after_row], os.path.join(out, f"gait_{name.lower()}_before_after.png"))
        else:
            stack([after_row], os.path.join(out, f"gait_{name.lower()}_after.png"))


def main():
    args = C.cli_args()
    out = os.path.join(C.preview_dir(), "Creatures")
    os.makedirs(out, exist_ok=True)
    only = [n for n in (args.get("only") or ",".join(SPECIES)).split(",") if n]
    for name in only:
        build(name, args, out)


if __name__ == "__main__":
    main()
