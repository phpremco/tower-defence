"""Round 33 helpers: gate-crossing clips for the climbing bosses (Gate_Mount, Gate_Straddle_Loop, Gate_Dismount).

Kept apart from the shared modules (it only imports `common`). A boss's gate module (gravemaw_gate.py,
cinder_gate.py) keys the three clips with the boss's own pose functions and uses this module for:

- the gate's measured geometry (`Gate_G1..G3.fbx`, round 9) in the boss's model units: the bosses play at Unity
  creature scale `MODEL_SCALE` (0.8, `ProjectGenerator.Bosses`), the gate at 1.0, so the gate is authored 1/0.8 times
  its world size round the boss;
- `FootPlan`: planted foot positions over time with lifted swings between them (hand-timed: hooks and braces);
- `plan_steps`: a step planner for legs whose body motion is known (the foot stays planted until it would
  over-stretch or fall behind, then swings to the nearest reachable surface: ground clear of the gate, a face, a crown
  top; scrabbles while nothing is in reach);
- `monotone_samples`: a no-overshoot profile for the arch a spine rides over the gate;
- the gate sag (`TowerDisplacementView`: the beam top drops `SAG_PER_BURDEN` world metres per unit of burden) and the
  per-frame burden curve a clip reports so Unity sags the gate in sync with the belly;
- QA: `import_gate` (the real Gate FBX scaled to model units at a forward position, sagged like Unity does),
  `GateCheck` (per frame: how deep the deformed boss mesh sinks into the gate, how deep the gate's thin beam sinks
  into the boss, and how far every planted `IK_*` marker is from the nearest surface), `previews_and_check` (side and
  44-degree game-camera strips with the gate in the scene, plus the check for the report).

Coordinates: gate space is the boss's (x, up, game forward) with the gate's road centre on the ground at forward 0;
a clip whose pivot starts `d` in front of the gate adds `d` to every forward value.
"""

import math
import os

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

import common as C

# Unity: bosses play at creature scale 0.8, gates at 1.0. Every gate number below is in WORLD metres.
MODEL_SCALE = 0.8
# Gate_G1..G3 (build_gate.py; measured by ray casts on the LOD0 meshes, identical for all tiers unless noted).
BEAM_TOP = 1.171          # the beam's top between the pylons (|x| < 0.88)
BEAM_BOTTOM = 1.04        # the beam is a bar; the energy field is open under it
BEAM_HALF = 0.105         # half its depth along the road
CROWN_TOP = 1.2           # the pylon crowns' flat top (x 0.88..1.12); the beacon in the middle rises to 1.27
CROWN_X = (0.88, 1.2)     # the pylon column across the road (inner, outer)
PYLON_FACE = 0.195        # the pylon's face along the road at heights 0.45..1.0 (all tiers)
CROWN_FACE = 0.232        # the crown's face (heights 1.0..1.2)

# TowerDisplacementView (round 4b): squash 0.16 and drop 0.12 per unit burden.
SAG_SQUASH = 0.16
SAG_DROP = 0.12
SAG_PER_BURDEN = BEAM_TOP * SAG_SQUASH + SAG_DROP   # 0.307 world metres off the beam top at burden 1


def m(world):
    """World metres to the boss's model metres."""
    return world / MODEL_SCALE


def burden_for_sag(sag_model):
    """The TowerDisplacementView burden that drops the beam top by `sag_model` (model metres)."""
    return max(0.0, sag_model * MODEL_SCALE / SAG_PER_BURDEN)


def gate_facts():
    s = 1.0 / MODEL_SCALE
    return {"model_scale": MODEL_SCALE,
            "beam_top_world": BEAM_TOP, "beam_top_model": round(BEAM_TOP * s, 3),
            "crown_top_world": CROWN_TOP, "crown_top_model": round(CROWN_TOP * s, 3),
            "beam_bottom_world": BEAM_BOTTOM, "beam_half_depth_world": BEAM_HALF,
            "pylon_face_world": PYLON_FACE, "crown_face_world": CROWN_FACE, "crown_x_world": list(CROWN_X),
            "tiers": {"G1": "the reference shape; pylon foot 0.20 along the road",
                      "G2": "same beam, crowns and pylon faces as G1; buttresses reach 0.34 along the road below 0.3 "
                            "high (no foot of the clips touches them)",
                      "G3": "same beam and crown top; buttresses reach 0.52 below 0.3 high, and a spike rises to 1.46 "
                            "on the outer crown corner (x 1.12, 0.2 towards the enemy) beside the feet on the crown; "
                            "the round-33 contact check passes on G1, G2 and G3 alike"},
            "sag": f"TowerDisplacementView drops the beam top {SAG_PER_BURDEN:.3f} world per unit burden "
                   f"(squash {SAG_SQUASH}, drop {SAG_DROP}); the clips' gate_burden curves use that"}


# ---------------------------------------------------------------------------------------------------------------
# Foot plans
# ---------------------------------------------------------------------------------------------------------------

class FootPlan:
    """A foot's planted positions over time. `plants`: [(frame_land, point)] in order; between two plants the foot
    swings (lifted `lift` above the higher end, `clear` pushes the arc away along a direction) during the `swing`
    frames before the next plant lands. Before the first plant the foot is at the first point."""

    def __init__(self, plants, swing=10, lift=0.25, clear=None):
        self.plants = [(f, Vector(p)) for f, p in plants]
        self.swing = swing
        self.lift = lift
        self.clear = Vector(clear) if clear is not None else None

    def at(self, f):
        """(point, planted?) at frame f."""
        ps = self.plants
        if f <= ps[0][0]:
            return ps[0][1].copy(), True
        for i in range(1, len(ps)):
            land, p1 = ps[i]
            p0 = ps[i - 1][1]
            sw = self.swing if not isinstance(self.swing, (list, tuple)) else self.swing[i]
            start = max(land - sw, ps[i - 1][0])
            if f < start:
                return p0.copy(), True
            if f < land:
                s = (f - start) / max(land - start, 1e-6)
                e = (1 - math.cos(math.pi * s)) * 0.5
                p = p0.lerp(p1, e)
                arc = math.sin(math.pi * s)
                p.z = p0.z + (p1.z - p0.z) * e + (max(p0.z, p1.z) - (p0.z + (p1.z - p0.z) * e) + self.lift) * arc
                if self.clear is not None:
                    p = p + self.clear * arc
                return p, False
        return ps[-1][1].copy(), True


# ---------------------------------------------------------------------------------------------------------------
# Gate in the scene (QA)
# ---------------------------------------------------------------------------------------------------------------

def import_gate(tier="G2", forward=0.0, scale=1.0 / MODEL_SCALE, sag_model=0.0):
    """Imports Gate_<tier>.fbx (LOD0 only shown), scaled to model units, its road centre `forward` ahead of the pivot.
    `sag_model` squashes and drops it as TowerDisplacementView would. Returns (roots, mesh)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(C.EXPORTS, "Gate", f"Gate_{tier}.fbx"))
    new = [o for o in bpy.data.objects if o not in before]
    mesh = None
    for o in new:
        if o.type == "MESH":
            if o.name.startswith(f"Gate_{tier}") and not any(k in o.name for k in ("LOD", "Damage", "Overlay")):
                mesh = o
            else:
                o.hide_render = o.hide_viewport = True
    roots = [o for o in new if o.parent is None]
    for o in new:
        # The imported clips (Build, Collapse ...) would key the rig back to the origin on every frame_set.
        if o.animation_data is not None:
            o.animation_data_clear()
    for o in roots:
        o["base_scale"] = list(o.scale)
    set_gate(roots, forward, sag_model, scale)
    return roots, mesh


def set_gate(roots, forward, sag_model=0.0, scale=1.0 / MODEL_SCALE):
    """Moves the imported gate to `forward` and sags it by `sag_model` (model metres off the beam top) the way
    TowerDisplacementView does (squash and drop)."""
    burden = burden_for_sag(sag_model)
    for o in roots:
        b = o["base_scale"]
        o.location = Vector((0.0, -forward, -m(SAG_DROP * burden)))
        o.scale = Vector((b[0] * scale, b[1] * scale, b[2] * scale * (1 - SAG_SQUASH * burden)))
    bpy.context.view_layer.update()


def points_of(mesh):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh.evaluated_get(dg)
    me = ev.to_mesh()
    pts = [ev.matrix_world @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return pts


def bvh_of(mesh):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [ev.matrix_world @ v.co for v in me.vertices]
    polys = [list(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, polys)


def inside(bvh, p, dirs=((1, 0, 0), (0, 1, 0), (0, 0, 1))):
    """Ray parity in three directions (majority): the normal test alone misfires near the gate's edges and corners."""
    votes = 0
    for d in dirs:
        d = Vector(d)
        o, hits = p.copy(), 0
        for _ in range(64):
            loc, _, _, _ = bvh.ray_cast(o, d)
            if loc is None:
                break
            hits += 1
            o = loc + d * 1e-4
        votes += hits % 2
    return votes >= 2


class GateCheck:
    """Contacts and penetration of a boss clip against a gate (BVH) and the ground (z = 0)."""

    def __init__(self, arm, mesh, markers, ground=0.0):
        self.arm = arm
        self.mesh = mesh
        self.markers = markers
        self.ground = ground

    def _marker_points(self):
        out = {}
        for n in self.markers:
            pb = self.arm.pose.bones[n]
            out[n] = self.arm.matrix_world @ pb.head
        return out

    def run(self, act, frames, gate_bvh, plans=None, every=1, label="", gate_points=None):
        """Returns a dict: worst mesh depth into the gate / under the ground and its frame, the worst depth of a gate
        vertex inside the boss (`gate_points`: the gate's vertices; catches the thin beam passing through the body,
        which no boss vertex would show), and per marker the worst gap (planted frames only; `plans` {marker:
        f -> planted}) and the worst sink."""
        dg = bpy.context.evaluated_depsgraph_get()
        worst_gate, worst_ground, worst_in = (0.0, None), (0.0, None), (0.0, None)
        feet = {n: {"gap": (0.0, None), "sink": (0.0, None)} for n in self.markers}
        for f in frames[::every]:
            C.set_action(self.arm, act, f)
            dg.update()
            ev = self.mesh.evaluated_get(dg)
            me = ev.to_mesh()
            mw = ev.matrix_world
            for v in me.vertices:
                p = mw @ v.co
                if p.z < self.ground - worst_ground[0]:
                    worst_ground = (self.ground - p.z, f)
                if gate_bvh is not None:
                    loc, nrm, _, dist = gate_bvh.find_nearest(p, 0.4)
                    if loc is not None and (p - loc).dot(nrm) < 0 and dist > worst_gate[0] and inside(gate_bvh, p):
                        worst_gate = (dist, f, tuple(round(c, 2) for c in (p.x, -p.y, p.z)))
            if gate_points:
                body = BVHTree.FromPolygons([mw @ v.co for v in me.vertices], [list(q.vertices) for q in me.polygons])
                for g in gate_points:
                    loc, nrm, _, dist = body.find_nearest(g, 0.5)
                    if loc is not None and (g - loc).dot(nrm) < 0 and dist > worst_in[0] and inside(body, g):
                        worst_in = (dist, f, tuple(round(c, 2) for c in (g.x, -g.y, g.z)))
            ev.to_mesh_clear()
            for n, p in self._marker_points().items():
                d_ground = p.z - self.ground
                d_gate = 9.0
                into = False
                if gate_bvh is not None:
                    loc, nrm, _, dist = gate_bvh.find_nearest(p, 3.0)
                    if loc is not None:
                        d_gate = dist
                        into = (p - loc).dot(nrm) < 0 and inside(gate_bvh, p)
                gap = min(abs(d_ground), d_gate)
                sink = max(-d_ground, d_gate if into else 0.0)
                planted = True
                if plans and n in plans:
                    planted = plans[n](f)
                if planted and gap > feet[n]["gap"][0]:
                    feet[n]["gap"] = (gap, f)
                if sink > feet[n]["sink"][0]:
                    feet[n]["sink"] = (sink, f)
        out = {"mesh_into_gate": [round(worst_gate[0], 3)] + list(worst_gate[1:]),
               "mesh_under_ground": [round(worst_ground[0], 3), worst_ground[1]],
               "gate_inside_mesh": [round(worst_in[0], 3)] + list(worst_in[1:]),
               "feet": {n: {k: [round(v[0], 3), v[1]] for k, v in d.items()} for n, d in feet.items()}}
        C.log("gatecheck", label, out)
        return out


# ---------------------------------------------------------------------------------------------------------------
# Strips with the gate in the scene
# ---------------------------------------------------------------------------------------------------------------

def gate_strip(name, arm, act, out, label, frames, gate_roots, gate_forward, sag_fn=None, view="side",
               target=None, distance=15.0, columns=4, cell=420, samples=16, ortho=None):
    """`<out>/<name>_<label>_<view>.png`: frames of `act` with the gate at `gate_forward`, sagged by `sag_fn(f)`
    (model metres). view: 'side' (a low 3/4 side view) or 'game' (the 44-degree ortho game camera)."""
    t = Vector(target) if target is not None else Vector((0, -gate_forward, 0.9))
    if view == "side":
        cam = C.closeup_camera(t, distance, azimuth=-82, elevation=10, lens=50)
    elif view == "front":
        cam = C.closeup_camera(t, distance, azimuth=-25, elevation=18, lens=50)
    else:
        cam = C.game_camera(t, ortho or 9.5)
    paths = []
    for i, f in enumerate(frames):
        set_gate(gate_roots, gate_forward, sag_fn(f) if sag_fn else 0.0)
        C.set_action(arm, act, f)
        paths.append(C.render(cam, os.path.join(out, f"_{name}_{label}_{view}_{i}.png"), 640, 360, samples=samples,
                              bloom=True))
    path = os.path.join(out, f"{name}_{label}_{view}.png")
    C.contact_sheet(paths, path, columns, cell)
    for p in paths:
        os.remove(p)
    bpy.data.objects.remove(cam)
    return path


def rot_about(p, centre, axis, angle):
    return centre + Matrix.Rotation(angle, 3, axis) @ (p - centre)


# ---------------------------------------------------------------------------------------------------------------
# A step planner for short legs crossing a gate
# ---------------------------------------------------------------------------------------------------------------

def plan_steps(frames, keys, hip, natural, candidates, reach, stride=0.45, swing=8, lift=0.22, forced=None,
               tuck=None, gap=3, start=None, hold=0, bias=None):
    """Per-frame feet for legs whose body motion is already known.

    hip(key, f) -> Vector; natural(key, f) -> Vector (where the foot would rest on flat ground, carried by the body);
    candidates(key, hip, natural) -> [points] the foot may stand on (ground, crown top, pylon face ...), best first;
    reach: the leg's longest comfortable hip-to-foot distance; forced(key, f) -> point or None (the clip's end
    targets, so the last pose matches the next clip); tuck(key, f) -> where the foot hangs while nothing is in reach
    (it scrabbles there, not planted). At most one leg per side starts a step every `gap` frames unless a foot is
    over-stretched. `start`: the planted feet at frame 0 (the previous clip's last pose); `hold`: no voluntary step
    before this frame; `bias` {key: frames}: a leg waits that long before a voluntary step (breaks the mirror
    symmetry of left and right). Returns ({key: [(point, planted)] * (frames + 1)}, step log)."""
    side_of = lambda k: k[0] if isinstance(k, tuple) else k  # noqa: E731

    def choose(k, f):
        fr = forced(k, f) if forced else None
        if fr is not None:
            return fr
        h = hip(k, f)
        for c in candidates(k, h, natural(k, f)):
            if (c - h).length <= reach:
                return c
        return None

    cur = {k: (start[k].copy() if start and k in start else choose(k, 0)) for k in keys}   # planted, or None (tucked)
    out = {k: [] for k in keys}
    swing_of = {}
    last_start = {}
    first_want = {}
    log = []
    for f in range(frames + 1):
        for k in keys:
            if k in swing_of:
                continue
            h = hip(k, f)
            p = cur[k]
            fr = forced(k, f) if forced else None
            if p is None:
                wants, over = True, False
            else:
                over = (p - h).length > reach
                far = (p - natural(k, f)).length > stride
                wants = over or far or (fr is not None and (p - fr).length > 0.03)
            if not wants:
                first_want.pop(k, None)
                continue
            first_want.setdefault(k, f)
            if not over and (f < hold or f - first_want[k] < (bias or {}).get(k, 0)):
                continue
            busy = any(side_of(kk) == side_of(k) for kk in swing_of)
            recent = any(f - last_start.get(kk, -99) < gap for kk in keys if side_of(kk) == side_of(k))
            if (busy or recent) and not over:
                continue
            land = min(f + swing, frames)
            t = choose(k, land)
            start = p if p is not None else (tuck(k, f) if tuck else natural(k, f))
            if t is None:
                if p is not None:
                    cur[k] = None          # lets go and scrabbles
                    log.append((k, f, "tuck"))
                continue
            if (t - start).length < 0.03:
                continue
            swing_of[k] = (f, land, start.copy(), t.copy())
            last_start[k] = f
            first_want.pop(k, None)
            log.append((k, f, land, tuple(round(v, 2) for v in t)))
        for k in keys:
            if k in swing_of:
                s0, s1, p0, p1 = swing_of[k]
                if f >= s1:
                    cur[k] = p1
                    del swing_of[k]
                    out[k].append((p1.copy(), True))
                    continue
                s = (f - s0) / max(s1 - s0, 1)
                e = (1 - math.cos(math.pi * s)) * 0.5
                p = p0.lerp(p1, e)
                base = p0.z + (p1.z - p0.z) * e
                p.z = base + (max(p0.z, p1.z) - base + lift) * math.sin(math.pi * s)
                out[k].append((p, False))
            elif cur[k] is None:
                out[k].append(((tuck(k, f) if tuck else natural(k, f)).copy(), False))
            else:
                out[k].append((cur[k].copy(), True))
    return out, log


def monotone_samples(points, step=0.05):
    """Dense (f, z) samples of a monotone cubic (Fritsch-Carlson) through `points` (f increasing): no overshoot, so a
    profile with long flats and a tight fold stays on its control heights (Catmull-Rom loops on uneven spacing)."""
    f = [p[0] for p in points]
    z = [p[1] for p in points]
    n = len(points)
    d = [(z[i + 1] - z[i]) / (f[i + 1] - f[i]) for i in range(n - 1)]
    m = [d[0]] + [0.0 if d[i - 1] * d[i] <= 0 else (d[i - 1] + d[i]) * 0.5 for i in range(1, n - 1)] + [d[-1]]
    for i in range(n - 1):
        if d[i] == 0:
            m[i] = m[i + 1] = 0.0
            continue
        a, b = m[i] / d[i], m[i + 1] / d[i]
        s = a * a + b * b
        if s > 9:
            k = 3 / math.sqrt(s)
            m[i], m[i + 1] = k * a * d[i], k * b * d[i]
    out = []
    for i in range(n - 1):
        h = f[i + 1] - f[i]
        count = max(1, int(math.ceil(h / step)))
        for j in range(count):
            t = j / count
            h00, h10 = 2 * t ** 3 - 3 * t ** 2 + 1, t ** 3 - 2 * t ** 2 + t
            h01, h11 = -2 * t ** 3 + 3 * t ** 2, t ** 3 - t ** 2
            out.append((f[i] + t * h, h00 * z[i] + h10 * h * m[i] + h01 * z[i + 1] + h11 * h * m[i + 1]))
    out.append((f[-1], z[-1]))
    return out


def previews_and_check(boss, name, clips, planted, out, tier="G2", ortho=9.0, distance=12.0):
    """Side and 44-degree game-camera strips of a boss's gate clips with the real gate (model units, sagged in sync)
    and the contact check. clips: [(clip, gate_forward, sag_fn, camera centre forward, planted key)];
    planted: {key: {marker: [planted per frame]}}. Returns {clip: worst facts} for the report."""
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    if not any(o.name.startswith("Ground") and o.type == "MESH" for o in bpy.data.objects):
        C.ground()
    roots, gmesh = import_gate(tier, 0.0)
    markers = [mk for mk in boss.markers if mk.startswith("IK_")]
    checks = {}
    for clip, gf, sag, centre, key in clips:
        act, n = boss.new[clip], boss.frames[clip]
        frames = [int(round(n * i / 7)) for i in range(8)]
        for view in ("side", "game"):
            gate_strip(name, boss.arm, act, out, clip, frames, roots, gf, sag, view=view, target=(0, -centre, 0.8),
                       distance=distance, ortho=ortho, samples=16)
        pl = planted.get(key, {})
        worst = {"mesh_into_gate": [0.0], "feet": {}}
        for f in range(0, n + 1, 4):
            set_gate(roots, gf, sag(f))
            res = GateCheck(boss.arm, boss.mesh, markers).run(
                act, [f], bvh_of(gmesh), plans={mk: (lambda ff, mk=mk: pl[mk][ff]) for mk in pl},
                gate_points=points_of(gmesh))
            for k in ("mesh_into_gate", "gate_inside_mesh"):
                if res[k][0] > worst.setdefault(k, [0.0])[0]:
                    worst[k] = res[k]
            for mk, d in res["feet"].items():
                w = worst["feet"].setdefault(mk, {"gap": [0.0, None], "sink": [0.0, None]})
                for k in ("gap", "sink"):
                    if d[k][0] > w[k][0]:
                        w[k] = d[k]
        checks[clip] = worst
    for r in roots:
        for o in [r] + list(r.children_recursive):
            if o.name in bpy.data.objects:
                bpy.data.objects.remove(o)
    return checks


def write_check(name, checks):
    """Adds the contact check to `<name>_report.json` round28.gate.check."""
    import json
    path = os.path.join(C.EXPORTS, name, name + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["round28"].setdefault("gate", {})["check"] = checks
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("round33 gate check", name, checks)


USAGE = ("Gate_Mount from the road (gate centre gate_forward_m ahead of the pivot), then move the transform onto the "
         "gate centre (end offset, on the ground) and loop Gate_Straddle_Loop while it weighs the gate down, then "
         "Gate_Dismount over the far side and move the transform by its end offset. Drive the gate's burden from each "
         "clip's gate_burden (one value per frame, TowerDisplacementView units) and hit the gate at its weight_frames "
         "(impact_frame = the first). Climb_Up/Loop/Down stay for rocks.")
